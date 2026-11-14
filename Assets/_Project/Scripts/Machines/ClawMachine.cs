using System.Collections;
using ArcadeEmpire.Core;
using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Machine à pinces réaliste. Déroulé : ESPACE (la pièce glisse dans le monnayeur) → portique piloté au joystick avec inertie
    /// (le câble se balance) → ESPACE (descente) → fermeture des branches (collisions réelles) → la prise dépend du centrage du lot,
    /// de son poids, de sa rareté et de la force de la pince, puis s'use pendant le transport (arrêts brusques, durée) → trappe.
    /// Le pont et le chariot du portique se déplacent réellement ; sons, afficheur numérique et LED suivent la partie.
    /// </summary>
    public class ClawMachine : MachineBase
    {
        private enum Phase { Idle, Inserting, Moving, Sequence }

        [Header("Références (assignées par le builder)")]
        [SerializeField] private Rigidbody head;
        [SerializeField] private Transform cable;
        [SerializeField] private Transform[] prongs;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private Transform joystickStick;
        [SerializeField] private Transform dropButton;
        [SerializeField] private Transform bridge, carriage, insertCoin, displayAnchor;
        [SerializeField] private Light[] leds;

        [Header("Zone de jeu (repère local de la machine)")]
        [SerializeField] private Vector2 minXZ = new Vector2(-0.5f, -0.4f);
        [SerializeField] private Vector2 maxXZ = new Vector2(0.5f, 0.4f);
        [SerializeField] private Vector2 chuteXZ = new Vector2(-0.4f, -0.3f);
        [SerializeField] private float raisedY = 1.5f;
        [SerializeField] private float loweredY = 0.78f;
        [SerializeField] private float cableTopY = 1.72f;

        [Header("Mécanique")]
        [SerializeField] private float moveSpeed = 0.5f;
        [SerializeField] private float acceleration = 1.7f;
        [SerializeField] private float deceleration = 2.4f;
        [SerializeField] private float verticalSpeed = 0.42f;
        [SerializeField] private float returnSpeed = 0.5f;
        [SerializeField] private float openAngle = 38f;
        [SerializeField] private float closedAngle = -18f;
        [SerializeField] private float playTimeLimit = 25f;
        [Tooltip("Force de la pince (améliorable) : 1 = normale.")]
        [SerializeField] private float gripStrength = 1f;
        [SerializeField] private float grabRadius = 0.16f;

        public override string ControlsHint => "ZQSD / WASD / flèches : joystick      ESPACE : lancer la pièce, puis descendre la pince";
        /// <summary>Force réelle de la pince : réglage de base x bonus des améliorations achetées (+10 % par niveau).</summary>
        public float EffectiveGrip => gripStrength * (1f + 0.1f * UpgradeManager.GetLevel(data != null ? data.id : ""));
        public float TimeLeft => timeLeft;
        public bool IsMoving => phase == Phase.Moving;

        private Phase phase = Phase.Idle;
        private Vector3 headLocal;
        private Vector2 headVel, prevHeadVel, accel2;
        private float vertVel;
        private float clawAngle;
        private PrizeItem held;
        private float gripEnergy;
        private Collider[] clawColliders;
        private float timeLeft;
        private bool dropRequested;
        private int enterFrame;
        private Vector2 externalMove;
        private bool externalDrop;
        private Vector2 smoothedMove;
        private float buttonPress;
        private Vector3[] prongTangents;
        private string lostMessage;
        private int winsThisSession, lastBeep;

        // balancement du câble : pendule amorti (angles en radians autour de X et Z)
        private Vector2 swing, swingVel;

        // sons
        private AudioSource[] voices;
        private int nextVoice;
        private AudioSource motorA, motorB;
        private float lastImpact;

        // affichage
        private Text creditText, timeText, winsText;
        private long lastCredit = -1;
        private int lastTime = -1, lastWins = -1;
        private float flash;

        /// <summary>Appelée par le builder de l'éditeur pour câbler les références.</summary>
        public void Configure(MachineData machineData, Transform anchor, Rigidbody headBody, Transform cableT,
            Transform[] prongPivots, Transform hold, Transform stick, Transform button)
        {
            data = machineData;
            cameraAnchor = anchor;
            head = headBody;
            cable = cableT;
            prongs = prongPivots;
            holdPoint = hold;
            joystickStick = stick;
            dropButton = button;
        }

        /// <summary>Pièces mobiles du portique, monnayeur, afficheur et LED (builder).</summary>
        public void SetupExtras(Transform bridgeT, Transform carriageT, Transform coinVisual, Transform display, Light[] ledLights)
        {
            bridge = bridgeT; carriage = carriageT; insertCoin = coinVisual; displayAnchor = display; leds = ledLights;
        }

        public void SetExternalMove(Vector2 move) => externalMove = move;
        public void RequestDrop() => externalDrop = true;

        private void Awake()
        {
            Physics.defaultSolverIterations = Mathf.Max(Physics.defaultSolverIterations, 8);
            headLocal = new Vector3(chuteXZ.x, raisedY, chuteXZ.y);
            clawAngle = openAngle;
            prongTangents = new Vector3[prongs != null ? prongs.Length : 0];
            for (int i = 0; i < prongTangents.Length; i++)
            {
                Vector3 radial = new Vector3(prongs[i].localPosition.x, 0f, prongs[i].localPosition.z).normalized;
                prongTangents[i] = new Vector3(-radial.z, 0f, radial.x);
            }
            if (head != null)
            {
                head.transform.position = transform.TransformPoint(headLocal);
                clawColliders = head.GetComponentsInChildren<Collider>();
            }
            foreach (PrizeItem p in GetComponentsInChildren<PrizeItem>()) p.Machine = this;
            if (insertCoin != null) insertCoin.gameObject.SetActive(false);
            BuildAudio();
            if (displayAnchor != null) BuildDisplay();
            ApplyVisuals(Vector2.zero);
        }

        // ------------------------------------------------------------------ son
        private void BuildAudio()
        {
            voices = new AudioSource[6];
            for (int i = 0; i < voices.Length; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0.85f; s.minDistance = 0.8f; s.maxDistance = 12f;
                s.rolloffMode = AudioRolloffMode.Linear; s.dopplerLevel = 0f;
                voices[i] = s;
            }
            motorA = MakeMotor("MotorXZ", PusherAudio.Get(PusherAudio.Id.Motor), 1.5f);
            motorB = MakeMotor("MotorY", ClawAudio.Get(ClawAudio.Id.Whine), 1f);
        }

        private AudioSource MakeMotor(string name, AudioClip clip, float pitch)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.loop = true; s.playOnAwake = false; s.spatialBlend = 0.9f; s.minDistance = 0.8f; s.maxDistance = 9f;
            s.rolloffMode = AudioRolloffMode.Linear; s.volume = 0f; s.pitch = pitch; s.dopplerLevel = 0f;
            s.Play();
            return s;
        }

        private void PlayClip(AudioClip clip, Vector3 worldPos, float volume, float pitch = 1f)
        {
            if (voices == null || clip == null) return;
            var s = voices[nextVoice]; nextVoice = (nextVoice + 1) % voices.Length;
            s.transform.position = worldPos;
            s.pitch = pitch;
            s.PlayOneShot(clip, volume * GameAudio.EffectiveSfx);
        }

        private void PlayLocal(ClawAudio.Id id, float volume = 0.6f, float pitch = 1f) => PlayClip(ClawAudio.Get(id), transform.position + Vector3.up, volume, pitch);

        /// <summary>Choc réel d'un lot (appelé par PrizeItem) : bruit sourd proportionnel à la vitesse.</summary>
        public void OnPrizeImpact(float speed, Vector3 point, bool onAnotherPrize)
        {
            if (Time.time - lastImpact < 0.05f) return;
            lastImpact = Time.time;
            float vol = Mathf.Clamp01(speed * 0.5f) * 0.45f;
            PlayClip(onAnotherPrize ? ClawAudio.Get(ClawAudio.Id.Plush) : PusherAudio.Get(PusherAudio.Id.Thud), point, vol, Random.Range(0.85f, 1.2f));
        }

        // ------------------------------------------------------------------ afficheur
        private void BuildDisplay()
        {
            var cgo = new GameObject("DisplayCanvas", typeof(RectTransform), typeof(Canvas));
            cgo.transform.SetParent(displayAnchor, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = cgo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(640f, 100f);
            rt.localScale = Vector3.one * 0.0015f;
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(cgo.transform, false);
            bg.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 1f);
            var br = bg.GetComponent<RectTransform>(); br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            creditText = DisplayLabel(cgo.transform, 0, new Color(1f, 0.75f, 0.15f));
            timeText = DisplayLabel(cgo.transform, 1, new Color(0.3f, 0.95f, 1f));
            winsText = DisplayLabel(cgo.transform, 2, new Color(0.4f, 1f, 0.45f));
        }

        private static Text DisplayLabel(Transform parent, int column, Color color)
        {
            var go = new GameObject("Label" + column, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 32; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter; t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(column / 3f, 0f); r.anchorMax = new Vector2((column + 1) / 3f, 1f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            return t;
        }

        private void UpdateDisplay()
        {
            if (creditText == null) return;
            long credit = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Coins) : 0;
            if (credit != lastCredit) { lastCredit = credit; creditText.text = "CRÉDIT\n" + credit; }
            int t = phase == Phase.Moving ? Mathf.CeilToInt(Mathf.Max(0f, timeLeft)) : -1;
            if (t != lastTime) { lastTime = t; timeText.text = t >= 0 ? "TEMPS\n" + t : "TEMPS\n--"; }
            if (winsThisSession != lastWins) { lastWins = winsThisSession; winsText.text = "LOTS\n" + winsThisSession; }
        }

        private void UpdateLeds()
        {
            if (leds == null) return;
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 1.2f);
            float speed = IsInUse ? 3f : 1.2f;
            for (int i = 0; i < leds.Length; i++)
            {
                if (leds[i] == null) continue;
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * speed * 2f - i * 1.4f);
                Color baseCol = phase == Phase.Moving || phase == Phase.Sequence ? new Color(0.2f, 0.9f, 1f) : Color.Lerp(new Color(1f, 0.2f, 0.7f), new Color(1f, 0.85f, 0.2f), (i % 2 == 0) ? wave : 1f - wave);
                leds[i].color = Color.Lerp(baseCol, new Color(1f, 0.85f, 0.2f), flash);
                leds[i].intensity = Mathf.Lerp(0.5f + wave * 0.8f, 3f, flash);
            }
        }

        // ------------------------------------------------------------------ cycle de la partie
        protected override void OnPlayerEntered()
        {
            enterFrame = Time.frameCount;
            winsThisSession = 0;
            StatusText = $"ESPACE : insérer une pièce ({data.costPerPlay})   —   Échap : quitter";
        }

        protected override void OnPlayerExited() => StatusText = "";

        protected override void OnPlayStarted()
        {
            phase = Phase.Inserting;
            dropRequested = false;
            lostMessage = null;
            StartCoroutine(InsertRoutine());
        }

        protected override void OnPlayEnded()
        {
            phase = Phase.Idle;
            if (IsInUse) StatusText = $"ESPACE : rejouer ({data.costPerPlay} pièces)   —   Échap : quitter";
        }

        private IEnumerator InsertRoutine()
        {
            StatusText = "Pièce insérée…";
            PlayClip(PusherAudio.Get(PusherAudio.Id.Insert), transform.TransformPoint(0f, 0.4f, -1.13f), 0.6f);
            if (insertCoin != null)
            {
                insertCoin.gameObject.SetActive(true);
                Vector3 from = new Vector3(0f, 0.55f, -1.2f), to = new Vector3(0f, 0.4f, -1.14f);
                float t = 0f;
                while (t < 0.35f)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / 0.35f);
                    insertCoin.localPosition = Vector3.Lerp(from, to, u * u);
                    yield return null;
                }
                insertCoin.gameObject.SetActive(false);
            }
            else yield return new WaitForSeconds(0.3f);
            timeLeft = playTimeLimit;
            lastBeep = int.MaxValue;
            phase = Phase.Moving;
        }

        private void Update()
        {
            Vector2 move = Vector2.zero;
            if (IsInUse) move = HandleInput();

            if (phase == Phase.Moving)
            {
                Vector2 target = move * moveSpeed;
                float a = (target.sqrMagnitude > 0.0001f ? acceleration : deceleration) * Time.deltaTime;
                headVel = Vector2.MoveTowards(headVel, target, a);
                headLocal.x += headVel.x * Time.deltaTime;
                headLocal.z += headVel.y * Time.deltaTime;
                if (headLocal.x < minXZ.x || headLocal.x > maxXZ.x) { headLocal.x = Mathf.Clamp(headLocal.x, minXZ.x, maxXZ.x); headVel.x = 0f; }
                if (headLocal.z < minXZ.y || headLocal.z > maxXZ.y) { headLocal.z = Mathf.Clamp(headLocal.z, minXZ.y, maxXZ.y); headVel.y = 0f; }
                timeLeft -= Time.deltaTime;
                int secs = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
                StatusText = $"Place la pince — ESPACE pour descendre   ({secs} s)";
                if (secs <= 5 && secs < lastBeep && secs > 0) { lastBeep = secs; PlayLocal(ClawAudio.Id.Beep, 0.5f, 1f + (5 - secs) * 0.05f); }
                if (dropRequested || timeLeft <= 0f) StartCoroutine(DropSequence());
            }
            else if (phase != Phase.Sequence)
            {
                headVel = Vector2.MoveTowards(headVel, Vector2.zero, deceleration * Time.deltaTime);
            }

            UpdateSwing(Time.deltaTime);
            ApplyVisuals(move);
            UpdateDisplay();
            UpdateLeds();
            UpdateMotorSound();
            UpdateGrip(Time.deltaTime);
        }

        private Vector2 HandleInput()
        {
            Keyboard kb = Keyboard.current;
            Vector2 move = externalMove;
            bool action = externalDrop;
            bool exit = false;
            externalDrop = false;

            if (kb != null)
            {
                bool left = kb.aKey.isPressed || kb.qKey.isPressed || kb.leftArrowKey.isPressed;
                bool right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                bool fwd = kb.wKey.isPressed || kb.zKey.isPressed || kb.upArrowKey.isPressed;
                bool back = kb.sKey.isPressed || kb.downArrowKey.isPressed;
                move += new Vector2((right ? 1f : 0f) - (left ? 1f : 0f), (fwd ? 1f : 0f) - (back ? 1f : 0f));
                action |= kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                exit = kb.escapeKey.wasPressedThisFrame || (kb.eKey.wasPressedThisFrame && Time.frameCount != enterFrame);
            }
            move = Vector2.ClampMagnitude(move, 1f);

            if (exit && !IsPlaying)
            {
                Exit();
                return Vector2.zero;
            }
            if (action)
            {
                buttonPress = 0.25f;
                if (phase == Phase.Idle)
                {
                    if (!TryStartPlay()) { StatusText = "Pas assez de pièces !"; PlayLocal(ClawAudio.Id.Fail, 0.5f); }
                }
                else if (phase == Phase.Moving) dropRequested = true;
            }
            return phase == Phase.Moving ? move : Vector2.zero;
        }

        // ------------------------------------------------------------------ balancement, mouvement physique, visuels
        private void UpdateSwing(float dt)
        {
            if (dt <= 0f) return;
            Vector2 acc = (headVel - prevHeadVel) / dt;
            prevHeadVel = headVel;
            accel2 = Vector2.Lerp(accel2, acc, 1f - Mathf.Exp(-12f * dt));
            float len = Mathf.Max(0.2f, AttachY - headLocal.y);
            float g = 9.81f;
            // pendule : theta'' = -(g/L) theta - c theta' - a/L (a = accélération du point d'attache)
            // axe X du pendule <- accélération en X ; axe Z <- accélération en Z
            Vector2 a = new Vector2(accel2.x, accel2.y);
            float k = g / len, c = 2.2f;
            Vector2 acc2 = -k * swing - c * swingVel - a / len * 1.2f;
            swingVel += acc2 * dt;
            swing += swingVel * dt;
            swing = Vector2.ClampMagnitude(swing, 0.22f);
            // pendant la descente / la prise la pince est stabilisée par les guides : le balancement s'éteint
            if (phase == Phase.Sequence && headLocal.y < raisedY - 0.05f) { swing = Vector2.Lerp(swing, Vector2.zero, 1f - Mathf.Exp(-6f * dt)); swingVel *= Mathf.Exp(-6f * dt); }
        }

        private void FixedUpdate()
        {
            if (head == null) return;
            float len = Mathf.Max(0.2f, AttachY - headLocal.y);
            Vector3 off = new Vector3(Mathf.Sin(swing.x) * len, -(1f - Mathf.Cos(Mathf.Max(Mathf.Abs(swing.x), Mathf.Abs(swing.y)))) * len, Mathf.Sin(swing.y) * len);
            head.MovePosition(transform.TransformPoint(headLocal + off));
            head.MoveRotation(transform.rotation * Quaternion.Euler(-swing.y * Mathf.Rad2Deg, 0f, swing.x * Mathf.Rad2Deg));
        }

        private void ApplyVisuals(Vector2 move)
        {
            if (prongs != null)
                for (int i = 0; i < prongs.Length; i++)
                    prongs[i].localRotation = Quaternion.AngleAxis(clawAngle, prongTangents[i]);

            // portique : le pont suit Z, le chariot suit X, le câble part du chariot jusqu'à la tête
            Vector3 top = new Vector3(headLocal.x, AttachY, headLocal.z);
            if (bridge != null) bridge.localPosition = new Vector3(0f, cableTopY, headLocal.z);
            if (carriage != null) carriage.localPosition = new Vector3(headLocal.x, cableTopY - 0.055f, headLocal.z);
            if (cable != null && head != null)
            {
                Vector3 h = transform.InverseTransformPoint(head.transform.position + head.transform.up * 0.08f);
                Vector3 mid = (top + h) * 0.5f;
                Vector3 dir = top - h;
                float len = Mathf.Max(0.01f, dir.magnitude);
                cable.localPosition = mid;
                cable.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                cable.localScale = new Vector3(0.012f, len * 0.5f, 0.012f);
            }

            smoothedMove = Vector2.Lerp(smoothedMove, move, 1f - Mathf.Exp(-14f * Time.deltaTime));
            if (joystickStick != null)
                joystickStick.localRotation = Quaternion.Euler(smoothedMove.y * 18f, 0f, -smoothedMove.x * 18f);

            if (dropButton != null)
            {
                buttonPress = Mathf.Max(0f, buttonPress - Time.deltaTime);
                Vector3 p = dropButton.localPosition;
                p.y = Mathf.Lerp(p.y, buttonPress > 0f ? buttonBaseY - 0.012f : buttonBaseY, 1f - Mathf.Exp(-30f * Time.deltaTime));
                dropButton.localPosition = p;
            }
        }

        private float buttonBaseY => 0.665f;
        private float AttachY => cableTopY - 0.105f;   // point d'attache du câble sous le chariot

        private void UpdateMotorSound()
        {
            if (motorA == null) return;
            float vxz = headVel.magnitude / Mathf.Max(0.01f, moveSpeed);
            float vy = Mathf.Abs(vertVel) / Mathf.Max(0.01f, verticalSpeed);
            float eff = GameAudio.EffectiveSfx;
            motorA.volume = Mathf.MoveTowards(motorA.volume, Mathf.Clamp01(vxz) * 0.35f * eff, Time.deltaTime * 2f);
            motorA.pitch = 1.2f + vxz * 0.5f;
            motorB.volume = Mathf.MoveTowards(motorB.volume, Mathf.Clamp01(vy) * 0.3f * eff, Time.deltaTime * 2f);
            motorB.pitch = 0.9f + vy * 0.3f;
        }

        // ------------------------------------------------------------------ prise
        private void UpdateGrip(float dt)
        {
            if (held == null || dt <= 0f) return;
            // l'usure de la prise : temps + secousses (arrêts brusques)
            float jerk = accel2.magnitude + Mathf.Abs(vertVel - prevVert) / dt * 0.25f;
            prevVert = vertVel;
            gripEnergy -= dt * (1f + 0.9f * jerk) * (1f + 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 7f)) * Mathf.Clamp01(swing.magnitude * 6f));
            if (gripEnergy <= 0f)
            {
                held.Release();
                held = null;
                lostMessage = "Le lot a glissé des branches !";
                StatusText = lostMessage;
                PlayLocal(ClawAudio.Id.Slip, 0.6f);
                PlayLocal(ClawAudio.Id.Fail, 0.4f);
            }
        }

        private float prevVert;

        private IEnumerator DropSequence()
        {
            phase = Phase.Sequence;
            dropRequested = false;
            headVel = Vector2.zero;

            StatusText = "Descente…";
            yield return MoveHeadTo(new Vector3(headLocal.x, loweredY, headLocal.z), verticalSpeed);

            StatusText = "Prise…";
            PlayLocal(ClawAudio.Id.Grip, 0.6f);
            yield return AnimateClaw(closedAngle, 0.7f);
            TryGrab();

            yield return MoveHeadTo(new Vector3(headLocal.x, raisedY, headLocal.z), held != null ? verticalSpeed * 0.75f : verticalSpeed);

            StatusText = held != null ? "Direction la trappe…" : (lostMessage ?? StatusText);
            Vector3 chute = new Vector3(chuteXZ.x, raisedY, chuteXZ.y);
            yield return MoveHeadTo(chute, returnSpeed, true);

            yield return AnimateClaw(openAngle, 0.6f);
            if (held != null)
            {
                PrizeItem p = held;
                held = null;
                p.Release();
                PlayLocal(ClawAudio.Id.Grip, 0.4f, 0.8f);
            }
            yield return new WaitForSeconds(1.5f);   // laisse le temps au lot de tomber et d'être récupéré

            if (!justWon)
            {
                StatusText = lostMessage ?? "Perdu… retente ta chance !";
                PlayLocal(ClawAudio.Id.Fail, 0.5f);
            }
            justWon = false;
            EndPlay();
        }

        private bool justWon;

        private IEnumerator MoveHeadTo(Vector3 target, float speed, bool damped = false)
        {
            // profil trapézoïdal : l'arrêt est progressif, donc la prise s'use moins
            while ((headLocal - target).sqrMagnitude > 0.000004f)
            {
                float remaining = (headLocal - target).magnitude;
                float s = Mathf.Min(speed, Mathf.Max(0.08f, Mathf.Sqrt(2f * 1.2f * remaining)));
                Vector3 prev = headLocal;
                headLocal = Vector3.MoveTowards(headLocal, target, s * Time.deltaTime);
                Vector3 dv = (headLocal - prev) / Mathf.Max(Time.deltaTime, 1e-4f);
                headVel = new Vector2(dv.x, dv.z);
                vertVel = dv.y;
                yield return null;
            }
            headLocal = target;
            headVel = Vector2.zero; vertVel = 0f;
        }

        private IEnumerator AnimateClaw(float targetAngle, float duration)
        {
            float start = clawAngle, t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                clawAngle = Mathf.Lerp(start, targetAngle, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            clawAngle = targetAngle;
        }

        private void TryGrab()
        {
            Vector3 axis = holdPoint.position;
            Collider[] hits = Physics.OverlapSphere(axis, grabRadius * 1.5f, ~0, QueryTriggerInteraction.Ignore);
            PrizeItem best = null;
            float bestDist = float.MaxValue;
            foreach (Collider c in hits)
            {
                PrizeItem p = c.GetComponentInParent<PrizeItem>();
                if (p == null || !p.IsFree) continue;
                Vector3 d = p.transform.position - axis; d.y = 0f;
                if (d.sqrMagnitude < bestDist) { bestDist = d.sqrMagnitude; best = p; }
            }

            if (best == null)
            {
                lostMessage = "Rien sous la pince…";
                StatusText = lostMessage;
                return;
            }

            float centering = 1f - Mathf.Clamp01(Mathf.Sqrt(bestDist) / (grabRadius * 1.5f));
            float mass = best.Body != null ? best.Body.mass : 0.15f;
            float massFactor = Mathf.Clamp(0.17f / mass, 0.55f, 1.3f);
            float quality = (0.4f + 0.6f * centering) * massFactor * Slipperiness(best.reward) * EffectiveGrip * Random.Range(0.85f, 1.15f);

            if (quality < 0.4f)
            {
                lostMessage = centering < 0.35f ? "Raté : la pince n'a pris que le bord du lot !" : "Raté, le lot est trop lourd ou trop glissant !";
                StatusText = lostMessage;
                PlayLocal(ClawAudio.Id.Slip, 0.5f);
                return;
            }

            best.Attach(holdPoint, head.transform, clawColliders);
            held = best;
            gripEnergy = quality * 6.5f;
            prevVert = 0f;
            PlayLocal(ClawAudio.Id.Grip, 0.5f, 1.2f);
            StatusText = "Attrapé !";
        }

        private static float Slipperiness(RewardData r)
        {
            if (r == null) return 1f;
            switch (r.rarity)
            {
                case Rarity.Common: return 1f;
                case Rarity.Rare: return 0.92f;
                case Rarity.Epic: return 0.8f;
                default: return 0.66f;
            }
        }

        /// <summary>Appelée par la zone de récupération quand un lot libre tombe dans la trappe.</summary>
        public void OnPrizeCollected(PrizeItem prize)
        {
            if (prize == null || !prize.IsFree) return;
            prize.Collected = true;
            GrantReward(prize.reward);
            justWon = true;
            winsThisSession++;
            flash = 1f;
            PlayLocal(ClawAudio.Id.Fanfare, 0.7f);
            PlayClip(PusherAudio.Get(PusherAudio.Id.Collect), transform.TransformPoint(chuteXZ.x, 0.4f, chuteXZ.y), 0.6f);
            StatusText = $"GAGNÉ : {prize.reward.displayName} ({RarityName(prize.reward.rarity)}) !   +{prize.reward.ticketValue} tickets";
            Destroy(prize.gameObject, 0.7f);
        }

        public static string RarityName(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return "Commun";
                case Rarity.Rare: return "Rare";
                case Rarity.Epic: return "Épique";
                default: return "Légendaire";
            }
        }
    }
}
