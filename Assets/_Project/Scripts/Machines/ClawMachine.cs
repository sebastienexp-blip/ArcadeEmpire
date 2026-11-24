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

        // --- accès pour le labo automatique (ClawLab)
        public Vector3 HeadLocalPos => headLocal;
        public bool LabIdle => phase == Phase.Idle;
        public bool LabHolding => held != null;
        public int LabWins { get; private set; }
        public int LabGrabs { get; private set; }
        public Vector2 PlayMin => minXZ;
        public Vector2 PlayMax => maxXZ;
        public Vector2 ChuteXZ => chuteXZ;
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
            motorA = MakeMotor("MotorXZ", ClawAudio.Get(ClawAudio.Id.Whine), 1.15f);
            motorB = MakeMotor("MotorY", ClawAudio.Get(ClawAudio.Id.Whine), 0.85f);
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
                // éclairage intérieur à LED blanches (comme une vraie vitrine), légèrement teinté et animé ; dorure quand on gagne
                Color tint = Color.Lerp(new Color(1f, 0.55f, 0.8f), new Color(0.6f, 0.85f, 1f), (i % 2 == 0) ? wave : 1f - wave);
                Color baseCol = Color.Lerp(new Color(1f, 0.97f, 0.92f), tint, phase == Phase.Moving || phase == Phase.Sequence ? 0.12f : 0.3f);
                leds[i].color = Color.Lerp(baseCol, new Color(1f, 0.85f, 0.2f), flash);
                leds[i].intensity = Mathf.Lerp(1.7f + wave * 0.4f, 3.2f, flash);
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
            if (IsInUse) StatusText = (lostMessage != null && !lastWasWin ? lostMessage + "   —   " : "") + $"ESPACE : rejouer ({data.costPerPlay} pièces)   —   Échap : quitter";
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
            Vector2 move = externalMove + ArcadeEmpire.Core.TouchInput.Move;
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

        private Vector3 physHead; private bool physHeadInit;

        private void FixedUpdate()
        {
            if (head == null) return;
            // headLocal avance à chaque image affichée ; la physique tourne à pas fixe. Sans lissage, la pince avance par à-coups
            // (immobile un pas, puis un saut) : ces secousses arrachaient le lot de la prise, surtout quand l'image ralentit.
            if (!physHeadInit) { physHead = headLocal; physHeadInit = true; }
            physHead = Vector3.Lerp(physHead, headLocal, 1f - Mathf.Exp(-22f * Time.fixedDeltaTime));
            float len = Mathf.Max(0.2f, AttachY - physHead.y);
            Vector3 off = new Vector3(Mathf.Sin(swing.x) * len, -(1f - Mathf.Cos(Mathf.Max(Mathf.Abs(swing.x), Mathf.Abs(swing.y)))) * len, Mathf.Sin(swing.y) * len);
            head.MovePosition(transform.TransformPoint(physHead + off));
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
        private void UpdateGrip(float dt) { }   // la prise est physique : voir PrizeItem (liaison qui peut casser)

        /// <summary>La liaison physique a cassé : le lot a glissé des branches.</summary>
        public void OnPrizeSlipped(PrizeItem prize, float force)
        {
            if (prize != held) return;
            held = null;
            Debug.Log($"[ClawSlip] force={force:0.00} grip={gripForce:0.00} hold={holdForce:0.00} poids={(prize.Body != null ? prize.Body.mass * 9.81f : 0f):0.00} hauteur={headLocal.y:0.000} haut={raisedY:0.000} contacts={prize.LabContacts}");
            lostMessage = headLocal.y > raisedY - 0.03f ? "Le lot a glissé en haut… la pince a lâché !" : "Le lot s'est échappé des branches !";
            StatusText = lostMessage;
            PlayLocal(ClawAudio.Id.Slip, 0.6f);
            PlayLocal(ClawAudio.Id.Fail, 0.4f);
            StartCoroutine(AnimateClaw(closedAngle, 0.2f));   // plus rien entre les branches : elles se referment
        }

        // force de serrage : pleine à la prise, réduite une fois en haut (réglage « strength at top » des vraies machines)
        private float gripForce, holdForce;

        /// <summary>Lot que les branches vont enserrer (le plus proche de l'axe), sans le prendre.</summary>
        private PrizeItem PeekPrize()
        {
            PrizeItem best = null; float bestDist = float.MaxValue;
            foreach (Collider c in Physics.OverlapSphere(holdPoint.position, 0.12f, ~0, QueryTriggerInteraction.Ignore))
            {
                PrizeItem p = c.GetComponentInParent<PrizeItem>();
                if (p == null || !p.IsFree) continue;
                Vector3 d = p.transform.TransformPoint(PrizeCenter(p)) - holdPoint.position; d.y = 0f;
                if (d.sqrMagnitude < bestDist && d.magnitude < PrizeRadius(p) + 0.03f) { bestDist = d.sqrMagnitude; best = p; }
            }
            return best;
        }

        /// <summary>Descend jusqu'à toucher le tas (détection de contact comme le « mou » du câble), puis un petit peu plus.</summary>
        private IEnumerator DescendUntilContact()
        {
            float stopAt = loweredY;
            bool touched = false;
            while (headLocal.y > stopAt + 0.0005f)
            {
                float prev = headLocal.y;
                headLocal.y = Mathf.MoveTowards(headLocal.y, stopAt, verticalSpeed * Time.deltaTime);
                vertVel = (headLocal.y - prev) / Mathf.Max(Time.deltaTime, 1e-4f);
                if (!touched && headLocal.y < raisedY - 0.08f && TouchingBelow())
                {
                    touched = true;
                    stopAt = Mathf.Max(loweredY, headLocal.y - 0.015f);   // la paume s'enfonce à peine dans les peluches
                }
                yield return null;
            }
            vertVel = 0f;
        }

        private bool TouchingBelow()
        {
            // comme une vraie pince : la descente s'arrête quand le BOÎTIER (la paume) se pose sur le tas et que le câble se détend.
            // Les branches ouvertes passent autour du lot : leurs pointes finissent plus bas que lui, prêtes à l'enserrer.
            Vector3 palm = head.position - head.transform.up * 0.07f;
            foreach (Collider c in Physics.OverlapSphere(palm, 0.045f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (clawColliders != null && System.Array.IndexOf(clawColliders, c) >= 0) continue;
                if (c.attachedRigidbody != null && c.attachedRigidbody == head) continue;
                return true;
            }
            return false;
        }

        private float prevVert;

        private IEnumerator DropSequence()
        {
            phase = Phase.Sequence;
            dropRequested = false;
            headVel = Vector2.zero;

            StatusText = "Descente…";
            ArcadeEmpire.Core.SoundBank.PlayAt("claw_drop", transform.position + Vector3.up * 1.6f, 0.55f);
            yield return DescendUntilContact();

            StatusText = "Prise…";
            PlayLocal(ClawAudio.Id.Grip, 0.6f);
            // les branches se referment AUTOUR du lot (elles s'arrêtent sur lui) ; à vide elles se ferment complètement
            yield return CloseUntilContact(0.75f);
            yield return new WaitForFixedUpdate();
            TryGrab();
            if (held == null && clawAngle > closedAngle + 0.5f) yield return AnimateClaw(closedAngle, 0.25f);   // la prise a raté : les branches glissent et se referment
            if (held != null) { yield return new WaitForSeconds(0.6f); if (held != null) held.SetBreakForce(gripForce); }   // les branches ont fini de serrer
            if (held != null)   // décollage lent du tas (la pince force à pleine puissance), puis montée normale
                yield return MoveHeadTo(new Vector3(headLocal.x, Mathf.Min(raisedY, headLocal.y + 0.06f), headLocal.z), verticalSpeed * 0.35f);

            yield return MoveHeadTo(new Vector3(headLocal.x, raisedY, headLocal.z), held != null ? verticalSpeed * 0.75f : verticalSpeed);
            if (held != null) held.SetBreakForce(holdForce);   // en haut, la pince serre moins fort (réglage des vraies machines)
            yield return new WaitForSeconds(0.35f);

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
            lastWasWin = justWon;
            justWon = false;
            EndPlay();
        }

        private bool justWon, lastWasWin;

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

        // ------------------------------------------------------------------ prise réaliste
        // Une vraie pince ne soulève un lot que si ses branches l'ENSERRENT : au moins deux branches en contact avec lui,
        // son centre de gravité à l'intérieur du cercle des branches, et les pointes passées SOUS son centre (elles le portent).
        // Toucher le bord, le dessus ou seulement le frôler avec le bout d'une branche ne suffit jamais.

        private Collider[] prongCols;

        private Collider ProngCollider(int i)
        {
            if (prongCols == null || prongCols.Length != prongs.Length)
            {
                prongCols = new Collider[prongs.Length];
                for (int k = 0; k < prongs.Length; k++) { var t = prongs[k].Find("Prong"); prongCols[k] = t != null ? t.GetComponent<Collider>() : null; }
            }
            return prongCols[i];
        }

        private Vector3 ProngTip(int i) => prongs[i].TransformPoint(0f, -0.2f, 0f);

        /// <summary>Distance de la branche i au lot (0 = elle le touche ou s'y enfonce).</summary>
        private float ProngGap(int i, PrizeItem p)
        {
            var pc = ProngCollider(i);
            float best = float.MaxValue;
            foreach (var c in p.GetComponentsInChildren<Collider>())
            {
                if (pc != null && Physics.ComputePenetration(pc, pc.transform.position, pc.transform.rotation, c, c.transform.position, c.transform.rotation, out _, out _)) return 0f;
                // segment de la branche : du milieu à la pointe
                Vector3 a = prongs[i].TransformPoint(0f, -0.1f, 0f), b = ProngTip(i);
                for (int k = 0; k <= 4; k++)
                {
                    Vector3 q = Vector3.Lerp(a, b, k / 4f);
                    best = Mathf.Min(best, Vector3.Distance(q, c.ClosestPoint(q)));
                }
            }
            return best;
        }

        /// <summary>Lots touchés par les branches (pas par le câble ni le boîtier).</summary>
        private PrizeItem ProngTouching(out int count)
        {
            count = 0;
            PrizeItem best = null; int bestN = 0;
            var seen = new System.Collections.Generic.HashSet<PrizeItem>();
            foreach (Collider c in Physics.OverlapSphere(holdPoint.position, 0.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                PrizeItem p = c.GetComponentInParent<PrizeItem>();
                if (p == null || !p.IsFree || !seen.Add(p)) continue;
                int n = 0;
                for (int i = 0; i < prongs.Length; i++) if (ProngGap(i, p) < 0.006f) n++;
                if (n > bestN) { bestN = n; best = p; }
            }
            count = bestN;
            return best;
        }

        /// <summary>Les branches se referment jusqu'à rencontrer un lot (elles s'arrêtent dessus, comme les vraies), sinon complètement.</summary>
        private IEnumerator CloseUntilContact(float duration)
        {
            float start = clawAngle, t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                clawAngle = Mathf.Lerp(start, closedAngle, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
                ProngTouching(out int n);
                if (n >= 2) { clawAngle -= 3f; yield break; }   // dernier petit serrage contre le lot
            }
            clawAngle = closedAngle;
        }

        private void TryGrab()
        {
            Vector3 axis = holdPoint.position;
            PrizeItem best = ProngTouching(out int contacts);
            if (best == null)
            {
                lostMessage = "Les branches se sont refermées dans le vide…";
                StatusText = lostMessage;
                return;
            }

            Vector3 com = best.Body != null ? best.Body.worldCenterOfMass : best.transform.TransformPoint(PrizeCenter(best));
            Bounds pb = PrizeBounds(best);
            float halfH = Mathf.Max(0.02f, pb.extents.y);

            // cercle formé par les pointes et position des pointes par rapport au centre de gravité
            float tipR = 0f; int under = 0; float wrap = 0f;
            for (int i = 0; i < prongs.Length; i++)
            {
                Vector3 tip = ProngTip(i);
                Vector3 h = tip - axis; h.y = 0f; tipR += h.magnitude / prongs.Length;
                if (ProngGap(i, best) < 0.012f)
                {
                    float below = (com.y - tip.y) / halfH;   // > 0 : la pointe est passée sous le centre
                    if (below > 0.05f) under++;
                    wrap += Mathf.Clamp01(below) / prongs.Length;
                }
            }
            Vector3 dc = com - axis; dc.y = 0f;
            float off = dc.magnitude;
            float reach = Mathf.Max(0.03f, tipR + 0.02f);

            string fail = null;
            if (contacts < 2) fail = "Une seule branche a touché le lot : il a glissé !";
            else if (off > reach * 0.85f) fail = "Raté : les branches n'ont pris que le bord du lot !";
            else if (under < 2) fail = "Les branches ont serré le dessus du lot sans passer dessous…";
            if (fail != null)
            {
                lostMessage = fail; StatusText = fail;
                PlayLocal(ClawAudio.Id.Slip, 0.5f);
                // le lot bouge un peu sous les branches (on l'a quand même touché)
                if (best.Body != null) best.Body.AddForce((dc.sqrMagnitude > 1e-6f ? dc.normalized : Vector3.forward) * 0.25f + Vector3.up * 0.4f, ForceMode.VelocityChange);
                return;
            }

            // qualité : centrage, profondeur sous le lot, 3 branches mieux que 2, masse, matière, force de la pince
            float centering = 1f - Mathf.Clamp01(off / reach);
            float mass = best.Body != null ? best.Body.mass : 0.15f;
            float massFactor = Mathf.Clamp(0.17f / mass, 0.55f, 1.3f);
            float quality = (0.35f + 0.65f * centering) * (0.45f + 0.55f * Mathf.Clamp01(wrap * 1.6f)) * (contacts >= 3 ? 1f : 0.78f)
                            * (0.6f + 0.4f * massFactor) * Slipperiness(best.reward) * EffectiveGrip * Random.Range(0.88f, 1.12f);

            float weight = mass * Physics.gravity.magnitude;
            gripForce = weight * (3.2f + 7f * quality) * EffectiveGrip;
            holdForce = weight * (1.15f + 2.7f * quality) * EffectiveGrip * Random.Range(0.8f, 1.15f);   // une prise bien enserrée tient mieux en haut
            // le lot est tenu par son centre de gravité et ramené dans l'axe de la pince (les branches le centrent en serrant)
            // (déjà entre les branches : on ne le tire que d'un tiers vers l'axe, sinon le ressort de la liaison crée une force qui la casse)
            Vector3 hold = Vector3.Lerp(com, new Vector3(axis.x, Mathf.Min(com.y, axis.y), axis.z), 0.35f);
            best.AttachJoint(head, com, gripForce * 4f, clawColliders, hold);   // le temps que les branches recentrent le lot, la prise ne casse pas
            held = best;
            LabGrabs++;
            PlayLocal(ClawAudio.Id.Grip, 0.5f, 1.2f);
            StatusText = contacts >= 3 ? "Attrapé à pleines branches !" : "Attrapé !";
        }

        private static Bounds PrizeBounds(PrizeItem p)
        {
            var b = new Bounds(p.transform.position, Vector3.zero); bool any = false;
            foreach (var c in p.GetComponentsInChildren<Collider>())
            {
                if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds);
            }
            return b;
        }

        private static Vector3 PrizeCenter(PrizeItem p)
        {
            var b = new Bounds(); bool any = false;
            foreach (var c in p.GetComponentsInChildren<Collider>())
            {
                Bounds cb = c.bounds;
                if (!any) { b = cb; any = true; } else b.Encapsulate(cb);
            }
            return any ? p.transform.InverseTransformPoint(b.center) : Vector3.zero;
        }

        private static float PrizeRadius(PrizeItem p)
        {
            var b = new Bounds(); bool any = false;
            foreach (var c in p.GetComponentsInChildren<Collider>())
            {
                Bounds cb = c.bounds;
                if (!any) { b = cb; any = true; } else b.Encapsulate(cb);
            }
            return any ? Mathf.Max(0.04f, Mathf.Min(b.extents.x, b.extents.z)) : 0.08f;
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
            LabWins++;
            GrantReward(prize.reward);
            justWon = true;
            winsThisSession++;
            flash = 1f;
            PlayLocal(ClawAudio.Id.Fanfare, 0.7f);
            Vector3 chuteWorld = transform.TransformPoint(chuteXZ.x, 0.9f, chuteXZ.y);
            ArcadeEmpire.Core.Fx.Confetti(chuteWorld + Vector3.up * 0.4f);
            ArcadeEmpire.Core.Fx.Sparkle(chuteWorld, UiCardsColor(prize.reward.rarity), 40);
            ArcadeEmpire.UI.FloatingText.Show(chuteWorld + Vector3.up * 0.8f, $"{prize.reward.displayName}  +{prize.reward.ticketValue} tickets", UiCardsColor(prize.reward.rarity), 30);
            PlayClip(PusherAudio.Get(PusherAudio.Id.Collect), transform.TransformPoint(chuteXZ.x, 0.4f, chuteXZ.y), 0.6f);
            StatusText = $"GAGNÉ : {prize.reward.displayName} ({RarityName(prize.reward.rarity)}) !   +{prize.reward.ticketValue} tickets";
            Destroy(prize.gameObject, 0.7f);
        }

        private static Color UiCardsColor(Rarity r) => ArcadeEmpire.UI.UiCards.RarityColor(r) * 1.2f;

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
