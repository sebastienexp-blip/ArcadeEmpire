using System.Collections.Generic;
using ArcadeEmpire.Core;
using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Coin pusher. Le joueur vise (gauche/droite) et insère des pièces (ESPACE, 1 pièce chacune) : la pièce glisse hors du rail,
    /// tombe sur le plateau et c'est la physique Unity qui fait tout. Une plaque pousseuse cinématique (déplacée en FixedUpdate,
    /// profil à vitesse quasi constante avec accélérations aux extrémités) pousse les pièces ; celles qui franchissent le bord
    /// tombent dans le bac. Les gains s'accumulent dans le bac (compteur animé) et se récupèrent avec R (ou en quittant la machine).
    /// Les pièces sont recyclées via un pool ; le plateau est limité en nombre de pièces. Bruitages liés aux vrais chocs.
    /// </summary>
    public class CoinPusherMachine : MachineBase
    {
        [Header("Références (assignées par le builder)")]
        [SerializeField] private Rigidbody pusher;
        [SerializeField] private Transform aimIndicator;
        [SerializeField] private Transform dropButton;
        [SerializeField] private Transform coinTemplate;
        [SerializeField] private Transform coinRoot;
        [SerializeField] private Transform displayAnchor;
        [SerializeField] private Light[] ledLights;

        [Header("Géométrie (repère local)")]
        [SerializeField] private float aimMinX = -0.5f;
        [SerializeField] private float aimMaxX = 0.5f;
        [SerializeField] private float dropY = 1.45f;
        [SerializeField] private float dropZ = 0.27f;
        [SerializeField] private float plateY = 0.965f;
        [SerializeField] private float plateRetractedZ = 0.39f;   // course de 20 cm : la plaque garde un tapis de pièces (comme une vraie)
        [SerializeField] private float plateExtendedZ = 0.17f;
        [Tooltip("Inclinaison de la plaque vers l'avant (degrés) : les pièces tombées dessus glissent vers le plateau inférieur.")]
        [SerializeField] private float plateTilt = 0f;
        [SerializeField] private float killY = 0.3f;

        [Header("Réglages")]
        [SerializeField] private float aimSpeed = 0.6f;
        [SerializeField] private float pushPeriod = 2.2f;
        [SerializeField] private float dropCooldown = 0.3f;
        [SerializeField] private int maxCoinsOnTray = 700;

        public override string ControlsHint => "←/→ : choisir la fente      1-4 : insérer dans la fente      ESPACE : insérer      R : gains      Échap : quitter";

        /// <summary>Les 4 fentes d'insertion (x local), comme sur les vraies machines : on choisit où tombe la pièce.</summary>
        public static readonly float[] SlotX = { -0.42f, -0.14f, 0.14f, 0.42f };
        private int selectedSlot = 1;
        private GameObject slotUi;

        private struct Pending { public PusherCoin coin; public float time; }

        private readonly List<PusherCoin> active = new List<PusherCoin>();
        private readonly Stack<PusherCoin> pool = new Stack<PusherCoin>();
        private readonly List<Pending> inserting = new List<Pending>();   // pièces qui glissent hors du rail
        private readonly List<Pending> inBin = new List<Pending>();       // pièces tombées dans le bac, recyclées peu après

        private float aimX, pushPhase, nextDropTime, lastSfx, buttonPress, killTimer, flash, lastImpact;
        private int enterFrame, lastHalf, pending, inserted;
        private float displayedPending;
        private float prevPusherZ;
        private long lastCredit = -1;
        private int lastPendingShown = -1, lastInserted = -1, lastMult = 1;

        private AudioSource[] voices; private int nextVoice;
        private AudioSource motor;
        private Text creditText, insertedText, gainsText;

        private const float InsertSlide = 0.16f;
        private const float RunSecondsPerCoin = 14f;
        private const float BinDelay = 0.7f;

        public int CoinsOnTray => active.Count;
        // --- accès pour le labo automatique (PusherLab)
        public IReadOnlyList<PusherCoin> ActiveCoins => active;
        public int TotalCollected { get; private set; }
        public int InsertedCount => inserted;
        public float PlateFrontZ => pusher != null ? transform.InverseTransformPoint(pusher.position).z - 0.31f : 0f;
        public void LabSpinWheel() => StartWheel();
        public int GainMultiplier => Time.time < tripleUntil ? 3 : (Time.time < doubleUntil ? 2 : 1);
        private float doubleUntil, tripleUntil, turboUntil;
        public bool PlateRunning => Time.time < runUntil;
        public bool TurboActive => Time.time < turboUntil;
        public float TurboFactor => Time.time < turboUntil ? 1.7f : 1f;

        /// <summary>Les 8 segments de la roue bonus (dans l'ordre du dessin).</summary>
        public enum Outcome { CoinRain, Gains3, Tickets, Gem, BillRain, Turbo, Jackpot, FreeCoins }
        private PusherMechanics mech;
        private Transform[] billTemplates;
        private bool wheelBusy, ballWheel;
        private Transform liftTube;
        // jackpot progressif : +1 à chaque pièce insérée, gagné en entier sur la case JACKPOT de la roue
        private const int PotBase = 200;
        public int JackpotPot { get; private set; } = PotBase;
        private int lastPotShown = -1;

        /// <summary>Mécanismes annexes (vibreur, pousseurs, hélice, voies, roue) et modèles de billets, assignés par le builder.</summary>
        public void SetupMechanics(PusherMechanics m, Transform[] bills, Transform lift = null)
        {
            mech = m; billTemplates = bills; liftTube = lift;
            if (m != null) m.machine = this;
        }
        public int PendingWinnings => pending;

        public void Configure(MachineData machineData, Transform anchor, Rigidbody pusherBody, Transform aim,
            Transform button, Transform template, Transform root)
        {
            data = machineData;
            cameraAnchor = anchor;
            pusher = pusherBody;
            aimIndicator = aim;
            dropButton = button;
            coinTemplate = template;
            coinRoot = root;
        }

        /// <summary>Affichage numérique (ancrage) et éclairages LED animés, assignés par le builder.</summary>
        public void SetupExtras(Transform display, Light[] leds)
        {
            displayAnchor = display;
            ledLights = leds;
        }

        private void Awake()
        {
            Physics.defaultSolverIterations = Mathf.Max(Physics.defaultSolverIterations, 8);
            Time.fixedDeltaTime = Mathf.Min(Time.fixedDeltaTime, 1f / 60f);   // pas de physique fin : pièces fines, pas de traversée
            if (coinRoot != null)
                foreach (PusherCoin c in coinRoot.GetComponentsInChildren<PusherCoin>(false))
                {
                    c.Machine = this;
                    active.Add(c);
                    var rb = c.GetComponent<Rigidbody>();
                    if (rb != null) rb.Sleep();   // les pièces posées au départ ne s'agitent pas avant la première poussée
                }
            if (coinRoot != null)
                foreach (PusherBonus b in coinRoot.GetComponentsInChildren<PusherBonus>(false)) b.GetComponent<Rigidbody>().Sleep();
            if (pusher != null)
            {
                pusher.transform.SetPositionAndRotation(transform.TransformPoint(0f, plateY, plateRetractedZ), transform.rotation * Quaternion.Euler(-plateTilt, 0f, 0f));
                pusher.transform.position = transform.TransformPoint(0f, plateY, plateExtendedZ);
                prevPusherZ = plateExtendedZ;
                pushPhase = 0.5f; lastHalf = 1;   // au repos la plaque est sortie
            }
            BuildAudio();
            if (displayAnchor != null) BuildDisplay();
        }

        // ------------------------------------------------------------------ son
        private void BuildAudio()
        {
            voices = new AudioSource[8];
            for (int i = 0; i < voices.Length; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0.85f; s.minDistance = 0.8f; s.maxDistance = 12f;
                s.rolloffMode = AudioRolloffMode.Linear; s.dopplerLevel = 0f;
                voices[i] = s;
            }
            var mgo = new GameObject("Motor");
            mgo.transform.SetParent(transform, false);
            motor = mgo.AddComponent<AudioSource>();
            motor.clip = PusherAudio.Get(PusherAudio.Id.Motor);
            motor.loop = true; motor.playOnAwake = false; motor.spatialBlend = 0.9f; motor.minDistance = 0.8f; motor.maxDistance = 10f;
            motor.rolloffMode = AudioRolloffMode.Linear; motor.volume = 0f; motor.dopplerLevel = 0f;
            motor.Play();
        }

        private void PlayVoice(PusherAudio.Id id, Vector3 worldPos, float volume, float pitch = 1f)
        {
            if (voices == null) return;
            var s = voices[nextVoice]; nextVoice = (nextVoice + 1) % voices.Length;
            s.transform.position = worldPos;
            s.pitch = pitch;
            s.PlayOneShot(PusherAudio.Get(id), volume * GameAudio.EffectiveSfx);
        }

        /// <summary>Choc réel entre une pièce et autre chose (appelé par PusherCoin) : cliquetis métallique ou bruit sourd.</summary>
        public void OnCoinImpact(float speed, Vector3 point, bool onAnotherBody)
        {
            if (Time.time - lastImpact < 0.035f) return;
            lastImpact = Time.time;
            float vol = Mathf.Clamp01(speed * 0.45f) * 0.35f;
            PlayVoice(onAnotherBody ? PusherAudio.Id.Clink : PusherAudio.Id.Thud, point, vol, Random.Range(0.88f, 1.18f));
        }

        // ------------------------------------------------------------------ affichage numérique
        private void BuildDisplay()
        {
            var cgo = new GameObject("DisplayCanvas", typeof(RectTransform), typeof(Canvas));
            cgo.transform.SetParent(displayAnchor, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = cgo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(640f, 110f);
            rt.localScale = Vector3.one * 0.0015f;
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(cgo.transform, false);
            bg.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 1f);
            var br = bg.GetComponent<RectTransform>(); br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            creditText = DisplayLabel(cgo.transform, 0, new Color(1f, 0.75f, 0.15f));
            insertedText = DisplayLabel(cgo.transform, 1, new Color(0.3f, 0.95f, 1f));
            gainsText = DisplayLabel(cgo.transform, 2, new Color(0.4f, 1f, 0.45f));
        }

        private static Text DisplayLabel(Transform parent, int column, Color color)
        {
            var go = new GameObject("Label" + column, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 34; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter; t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(column / 3f, 0f); r.anchorMax = new Vector2((column + 1) / 3f, 1f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            return t;
        }

        private void UpdateDisplay()
        {
            if (creditText == null) return;
            displayedPending = Mathf.MoveTowards(displayedPending, pending, Mathf.Max(6f, Mathf.Abs(pending - displayedPending) * 5f) * Time.deltaTime);
            int shown = Mathf.RoundToInt(displayedPending);
            long credit = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Coins) : 0;
            if (credit != lastCredit) { lastCredit = credit; creditText.text = "CRÉDIT\n" + credit; }
            if (JackpotPot != lastPotShown) { lastPotShown = JackpotPot; insertedText.text = "JACKPOT\n" + JackpotPot; }
            int mult = GainMultiplier;
            if (shown != lastPendingShown || mult != lastMult) { lastPendingShown = shown; lastMult = mult; gainsText.text = (mult > 1 ? "GAINS x" + mult + "\n" : "GAINS\n") + shown; }
        }

        private void UpdateLeds()
        {
            if (ledLights == null) return;
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 1.6f);
            float speed = IsInUse ? 0.35f : 0.08f;
            for (int i = 0; i < ledLights.Length; i++)
            {
                if (ledLights[i] == null) continue;
                Color c = Color.HSVToRGB((Time.time * speed + i * 0.22f) % 1f, 0.85f, 1f);
                ledLights[i].color = Color.Lerp(c, new Color(1f, 0.85f, 0.2f), flash);
                ledLights[i].intensity = Mathf.Lerp(0.8f, 3.5f, flash) * (IsInUse ? 1f : 0.6f);
            }
        }

        // ------------------------------------------------------------------ cycle de la machine
        protected override void OnPlayerEntered()
        {
            enterFrame = Time.frameCount;
            if (slotUi == null) BuildSlotUi();
            RefreshStatus();
        }

        protected override void OnPlayerExited()
        {
            if (slotUi != null) slotUi.SetActive(false);
            CollectWinnings();   // les gains du bac sont toujours récupérés en quittant
            StatusText = "";
        }

        protected override void OnPlayStarted()
        {
            runUntil = Mathf.Min(Mathf.Max(runUntil, Time.time) + RunSecondsPerCoin, Time.time + 60f);
            SpawnCoin();
            inserted++;
            JackpotPot++;
            if (inserted % 20 == 0) StartWheel();   // tous les 20 pièces insérées, la roue bonus se lance
            PlayVoice(PusherAudio.Id.Insert, transform.TransformPoint(aimX, dropY, dropZ), 0.55f);
            flash = Mathf.Max(flash, 0.25f);
            EndPlay();   // chaque pièce insérée est une « partie » instantanée
        }

        private void Update()
        {
            if (IsInUse) HandleInput();
            if (slotUi != null) slotUi.SetActive(IsInUse && !UIState.PanelOpen);
            aimX = Mathf.Lerp(aimX, SlotX[selectedSlot], 1f - Mathf.Exp(-14f * Time.deltaTime));

            if (aimIndicator != null)
            {
                Vector3 p = aimIndicator.localPosition;
                p.x = aimX;
                aimIndicator.localPosition = p;
            }
            if (dropButton != null)
            {
                buttonPress = Mathf.Max(0f, buttonPress - Time.deltaTime);
                Vector3 p = dropButton.localPosition;
                p.y = Mathf.Lerp(p.y, buttonPress > 0f ? 0.653f : 0.665f, 1f - Mathf.Exp(-30f * Time.deltaTime));
                dropButton.localPosition = p;
            }

            if (IsInUse && Time.frameCount % 20 == 0) RefreshStatus();
            UpdateInserting();
            UpdateBin();
            UpdateDisplay();
            UpdateLeds();

            // filet de sécurité : une pièce tombée hors de la machine est recyclée sans gain
            killTimer += Time.deltaTime;
            if (killTimer > 0.5f)
            {
                killTimer = 0f;
                for (int i = active.Count - 1; i >= 0; i--)
                    if (active[i] != null && transform.InverseTransformPoint(active[i].transform.position).y < killY)
                        Recycle(active[i]);
            }
            UnjamChute();
        }

        // Anti-blocage : une pièce immobile dans le couloir de chute (au-dessus du plateau) reçoit une petite secousse, comme le
        // tapotement d'un technicien ; de plus en plus forte si elle reste coincée.
        private float unjamTimer;
        private void UnjamChute()
        {
            unjamTimer += Time.deltaTime;
            if (unjamTimer < 0.25f) return;
            float dt = unjamTimer; unjamTimer = 0f;
            for (int i = 0; i < active.Count; i++)
            {
                PusherCoin c = active[i];
                if (c == null || c.Body == null || c.Body.isKinematic) continue;
                Vector3 lp = transform.InverseTransformPoint(c.transform.position);
                // pièce debout (tranche vers le bas) posée sur la plaque dans le couloir : on la fait basculer à plat vers l'avant
                bool standing = Mathf.Abs(Vector3.Dot(c.transform.up, transform.forward)) > 0.6f;
                if (standing && lp.y > 1.0f && lp.y < 1.14f && lp.z > 0.2f && lp.z < 0.34f)
                {
                    c.TopplePending += dt;
                    if (c.TopplePending > 1.2f)
                    {
                        c.Body.WakeUp();
                        c.Body.AddForce(transform.TransformDirection(new Vector3(Random.Range(-0.15f, 0.15f), 0.35f, -0.9f)), ForceMode.VelocityChange);
                        c.TopplePending = 0.6f;
                    }
                }
                else c.TopplePending = 0f;
                bool inChute = lp.y > 1.13f && lp.z > 0.25f && lp.z < 0.32f;
                if (!inChute || c.Body.linearVelocity.sqrMagnitude > 0.0064f) { c.StuckTime = 0f; continue; }
                c.StuckTime += dt;
                if (c.StuckTime > 0.9f)
                {
                    float power = Mathf.Min(0.5f + c.StuckTime * 0.35f, 1.8f);
                    float side = Random.value < 0.5f ? -1f : 1f;
                    c.Body.WakeUp();
                    c.Body.AddForce(transform.TransformDirection(new Vector3(side * power, 0.25f, 0f)), ForceMode.VelocityChange);
                    c.Body.AddTorque(transform.TransformDirection(new Vector3(0f, 0f, -side * power * 2f)), ForceMode.VelocityChange);
                    c.StuckTime = 0.5f;
                }
            }
        }

        private void HandleInput()
        {
            int flick = ArcadeEmpire.Core.TouchInput.ConsumeFlick();
            if (flick < 0) selectedSlot = Mathf.Max(0, selectedSlot - 1);
            if (flick > 0) selectedSlot = Mathf.Min(SlotX.Length - 1, selectedSlot + 1);
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            if (kb.aKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) selectedSlot = Mathf.Max(0, selectedSlot - 1);
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) selectedSlot = Mathf.Min(SlotX.Length - 1, selectedSlot + 1);
            if (kb.digit1Key.wasPressedThisFrame) { InsertAt(0); return; }
            if (kb.digit2Key.wasPressedThisFrame) { InsertAt(1); return; }
            if (kb.digit3Key.wasPressedThisFrame) { InsertAt(2); return; }
            if (kb.digit4Key.wasPressedThisFrame) { InsertAt(3); return; }

            bool exit = kb.escapeKey.wasPressedThisFrame || (kb.eKey.wasPressedThisFrame && Time.frameCount != enterFrame);
            if (exit) { Exit(); return; }

            if (kb.rKey.wasPressedThisFrame) { CollectWinnings(); return; }

            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) InsertAt(selectedSlot);
        }

        /// <summary>Insère une pièce dans la fente choisie (clavier 1-4, boutons tactiles ou ESPACE sur la fente sélectionnée).</summary>
        public void InsertAt(int slot)
        {
            if (!IsInUse) return;
            selectedSlot = Mathf.Clamp(slot, 0, SlotX.Length - 1);
            buttonPress = 0.2f;
            if (Time.time < nextDropTime) return;
            aimX = SlotX[selectedSlot];
            if (TryStartPlay()) nextDropTime = Time.time + dropCooldown;
            else StatusText = "Plus de pièces !";
        }

        private void BuildSlotUi()
        {
            var canvas = UiKit.NewCanvas(transform, "PusherSlotsUi", 12);
            slotUi = canvas.gameObject;
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(slotUi.transform, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f); r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(Application.isMobilePlatform ? -60f : 0f, 168f); r.sizeDelta = new Vector2(440f, 64f);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 10f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            for (int i = 0; i < SlotX.Length; i++)
            {
                int slot = i;
                UiKit.AddIconButton(r, "down", (i + 1).ToString(), 100f, 64f, () => InsertAt(slot));
            }
        }

        // ------------------------------------------------------------------ plaque pousseuse (physique, FixedUpdate)
        private void FixedUpdate()
        {
            if (pusher == null) return;
            float dt = Time.fixedDeltaTime;
            float prev = pushPhase;
            bool running = Time.time < runUntil;
            if (running) pushPhase += dt * TurboFactor / pushPeriod;
            else if (Mathf.Repeat(pushPhase - 0.5f, 1f) > 0.0001f)
            {
                // à l'arrêt, la plaque termine sa course et s'immobilise sortie (ses pièces sont alors devant le capot)
                pushPhase += dt / pushPeriod;
                if (Mathf.Floor(pushPhase - 0.5f) > Mathf.Floor(prev - 0.5f)) pushPhase = Mathf.Floor(pushPhase - 0.5f) + 0.5f;
            }

            float t = pushPhase % 1f;
            float tri = t < 0.5f ? t * 2f : 2f - 2f * t;                       // 0 -> 1 -> 0
            float k = Mathf.Lerp(tri, tri * tri * (3f - 2f * tri), 0.65f);    // vitesse quasi constante, douce aux extrémités
            float z = Mathf.Lerp(plateRetractedZ, plateExtendedZ, k);
            pusher.MovePosition(transform.TransformPoint(0f, plateY, z));
            pusher.MoveRotation(transform.rotation * Quaternion.Euler(-plateTilt, 0f, 0f));

            // bruit sec aux inversions de course, ronronnement du moteur selon la vitesse
            int half = Mathf.FloorToInt(pushPhase * 2f);
            if (half != lastHalf) { lastHalf = half; if (running) PlayVoice(PusherAudio.Id.Clank, transform.TransformPoint(0f, plateY, z), 0.5f); }
            float speed = Mathf.Abs(z - prevPusherZ) / dt;
            prevPusherZ = z;
            if (motor != null)
            {
                motor.volume = Mathf.Lerp(motor.volume, Mathf.Clamp01(speed * 0.5f) * 0.12f * GameAudio.EffectiveSfx, 0.2f);
                motor.pitch = 0.8f + Mathf.Clamp01(speed * 0.6f) * 0.4f;
            }
        }

        // ------------------------------------------------------------------ pièces
        private void SpawnCoin() => SpawnCoin(aimX);

        private void SpawnCoin(float x)
        {
            PusherCoin coin;
            if (pool.Count > 0) coin = pool.Pop();
            else
            {
                coin = Instantiate(coinTemplate.gameObject, coinRoot).GetComponent<PusherCoin>();
                coin.Machine = this;
            }

            coin.Collected = false;
            var rb = coin.GetComponent<Rigidbody>();
            rb.isKinematic = true;   // tenue dans la fente pendant l'animation d'insertion
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            coin.transform.SetPositionAndRotation(
                transform.TransformPoint(Mathf.Clamp(x + Random.Range(-0.02f, 0.02f), aimMinX, aimMaxX), dropY + 0.07f, dropZ),
                transform.rotation * Quaternion.Euler(90f + Random.Range(-4f, 4f), Random.Range(-6f, 6f), Random.Range(-4f, 4f)));
            coin.gameObject.SetActive(true);
            active.Add(coin);
            inserting.Add(new Pending { coin = coin, time = Time.time });
            RefreshStatus();
        }

        private void UpdateInserting()
        {
            for (int i = inserting.Count - 1; i >= 0; i--)
            {
                Pending p = inserting[i];
                if (p.coin == null) { inserting.RemoveAt(i); continue; }
                float u = Mathf.Clamp01((Time.time - p.time) / InsertSlide);
                Vector3 pos = p.coin.transform.position;
                Vector3 target = transform.TransformPoint(0f, dropY, 0f);
                // glisse verticalement hors de la fente (ne touche pas à x et z : la visée reste respectée)
                float y = Mathf.Lerp(transform.TransformPoint(0f, dropY + 0.07f, 0f).y, target.y, u * u);
                p.coin.transform.position = new Vector3(pos.x, y, pos.z);
                if (u >= 1f)
                {
                    var rb = p.coin.Body != null ? p.coin.Body : p.coin.GetComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.linearVelocity = transform.TransformDirection(new Vector3(Random.Range(-0.12f, 0.12f), 0f, 0f));   // chute le long du mur du fond, légère dérive aléatoire
                    rb.angularVelocity = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.6f, 0.6f));
                    inserting.RemoveAt(i);
                }
            }
        }

        private void UpdateBin()
        {
            for (int i = inBin.Count - 1; i >= 0; i--)
            {
                if (Time.time - inBin[i].time < BinDelay) continue;
                if (inBin[i].coin != null) Recycle(inBin[i].coin);
                inBin.RemoveAt(i);
            }
        }

        private void Recycle(PusherCoin coin)
        {
            active.Remove(coin);
            coin.gameObject.SetActive(false);
            pool.Push(coin);
        }

        /// <summary>Une pièce est tombée dans le bac : +1 gain à récupérer. La pièce reste visible dans le bac un instant.</summary>
        public void OnCoinCollected(PusherCoin coin) => OnCoinCollected(coin, -1);

        public void OnCoinCollected(PusherCoin coin, int lane)
        {
            if (coin == null || coin.Collected) return;
            coin.Collected = true;
            inBin.Add(new Pending { coin = coin, time = Time.time });
            TotalCollected++;
            int gain = GainMultiplier;
            if (lane >= 0 && mech != null && lane == mech.LitLane)
            {
                gain *= 3;   // voie chance éclairée : gains x3
                StatusText = "VOIE CHANCE x3 !"; bonusMessageUntil = Time.time + 1.2f;
                flash = 1f;
            }
            pending += gain;
            flash = Mathf.Max(flash, 0.7f);
            if (Time.time - lastSfx > 0.07f)
            {
                lastSfx = Time.time;
                PlayVoice(PusherAudio.Id.Collect, coin.transform.position, 0.5f, Random.Range(0.95f, 1.1f));
            }
            RefreshStatus();
        }

        /// <summary>Verse les gains du bac dans le solde du joueur (R, ou automatiquement en quittant la machine).</summary>
        public void CollectWinnings()
        {
            if (pending <= 0) return;
            int n = pending;
            pending = 0;
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.Add(CurrencyType.Coins, n);
                CurrencyManager.Instance.Add(CurrencyType.Experience, n);
            }
            RaiseCoinsWon(this, n);
            PlayVoice(PusherAudio.Id.Payout, transform.TransformPoint(0f, 0.7f, -0.8f), Mathf.Clamp(0.4f + n * 0.03f, 0.4f, 0.9f));
            flash = 1f;
            RefreshStatus();
        }

        /// <summary>Un lot spécial (jeton) est tombé : récompense, tickets et XP.</summary>
        public void OnPrizeCollected(PrizeItem prize)
        {
            if (prize == null || !prize.IsFree) return;
            prize.Collected = true;
            GrantReward(prize.reward);
            Sfx.Play(Sfx.Kind.Win, transform.position);
            flash = 1f;
            StatusText = $"GAGNÉ : {prize.reward.displayName} ({ClawMachine.RarityName(prize.reward.rarity)}) !   +{prize.reward.ticketValue} tickets";
            Destroy(prize.gameObject);
        }

        /// <summary>Un jeton bonus est tombé dans le bac : déclenche son effet.</summary>
        public void OnBonusCollected(PusherBonus bonus)
        {
            if (bonus == null || bonus.Collected) return;
            bonus.Collected = true;
            flash = 1f;
            PlayVoice(PusherAudio.Id.Payout, bonus.transform.position, 0.9f);
            var cm = CurrencyManager.Instance;
            switch (bonus.kind)
            {
                case PusherBonus.Kind.CoinRain:
                    runUntil = Mathf.Max(runUntil, Time.time + 12f);
                    StartCoroutine(CeilingRain(24, 0.09f));
                    break;
                case PusherBonus.Kind.Wheel:
                    StartWheel();
                    break;
                case PusherBonus.Kind.Cash:
                    pending += bonus.value * GainMultiplier;
                    if (cm != null) cm.Add(CurrencyType.Experience, 8);
                    break;
                case PusherBonus.Kind.DoubleGains:
                    doubleUntil = Time.time + 30f;
                    break;
                case PusherBonus.Kind.TicketBurst:
                    if (cm != null) { cm.Add(CurrencyType.Tickets, 25); cm.Add(CurrencyType.Experience, 30); }
                    break;
                default:
                    if (cm != null) { cm.Add(CurrencyType.PremiumTokens, 1); cm.Add(CurrencyType.Experience, 60); }
                    break;
            }
            StatusText = "BONUS : " + PusherBonus.Label(bonus.kind) + " !";
            bonusMessageUntil = Time.time + 3.5f;
            Destroy(bonus.gameObject, 0.6f);
        }

        private float bonusMessageUntil;
        private float runUntil;   // la plaque ne bouge que si des pièces ont été insérées récemment

        // ------------------------------------------------------------------ roue bonus et effets spectaculaires
        private static readonly int[] WheelWeights = { 18, 14, 14, 6, 16, 12, 4, 16 };
        private static readonly int[] BallWheelWeights = { 15, 14, 10, 10, 15, 10, 16, 10 };   // balle jackpot : bien plus de chances de jackpot

        private int queuedBallSpins;
        private System.Collections.IEnumerator DelayedBallSpin() { yield return new WaitForSeconds(2f); StartWheel(true); }

        private void StartWheel() => StartWheel(false);

        private void StartWheel(bool fromBall)
        {
            if (mech == null) return;
            if (wheelBusy) { if (fromBall) queuedBallSpins++; return; }
            wheelBusy = true;
            int[] weights = fromBall ? BallWheelWeights : WheelWeights;
            int total = 0; foreach (int w in weights) total += w;
            int r = Random.Range(0, total), seg = 0;
            for (; seg < weights.Length - 1; seg++) { if (r < weights[seg]) break; r -= weights[seg]; }
            StatusText = "ROUE BONUS : ça tourne...";
            bonusMessageUntil = Time.time + 6f;
            mech.Show(5f);
            flash = 1f;
            PlayVoice(PusherAudio.Id.Payout, transform.TransformPoint(0f, 1.3f, 0.3f), 0.8f);
            mech.SpinWheel(seg, 4.5f, () => { wheelBusy = false; ApplyOutcome((Outcome)seg); if (queuedBallSpins > 0) { queuedBallSpins--; StartCoroutine(DelayedBallSpin()); } });
        }

        private void ApplyOutcome(Outcome o)
        {
            var cm = CurrencyManager.Instance;
            string msg;
            switch (o)
            {
                case Outcome.CoinRain: runUntil = Mathf.Max(runUntil, Time.time + 14f); StartCoroutine(CeilingRain(30, 0.08f)); msg = "PLUIE DE PIÈCES !"; break;
                case Outcome.Gains3: tripleUntil = Time.time + 30f; msg = "GAINS x3 pendant 30 s !"; break;
                case Outcome.Tickets: if (cm != null) { cm.Add(CurrencyType.Tickets, 40); cm.Add(CurrencyType.Experience, 30); } msg = "+40 TICKETS !"; break;
                case Outcome.Gem: if (cm != null) { cm.Add(CurrencyType.PremiumTokens, 1); cm.Add(CurrencyType.Keys, 1); cm.Add(CurrencyType.Experience, 60); } msg = "GEMME + CLÉ DORÉE !"; break;
                case Outcome.BillRain: runUntil = Mathf.Max(runUntil, Time.time + 14f); StartCoroutine(BillRain(10)); msg = "PLUIE DE BILLETS !"; break;
                case Outcome.Turbo: turboUntil = Time.time + 20f; runUntil = Mathf.Max(runUntil, Time.time + 20f); msg = "TURBO : la plaque s'emballe !"; break;
                case Outcome.Jackpot:
                    runUntil = Mathf.Max(runUntil, Time.time + 25f); tripleUntil = Time.time + 20f;
                    pending += JackpotPot;
                    msg = $"JACKPOT !!! +{JackpotPot} pièces, pluie géante et gains x3";
                    JackpotPot = PotBase;
                    StartCoroutine(CeilingRain(60, 0.05f)); StartCoroutine(BillRain(8)); mech.Show(10f);
                    break;
                default: pending += 20 * GainMultiplier; msg = "+20 PIÈCES OFFERTES !"; break;
            }
            StatusText = "ROUE : " + msg;
            bonusMessageUntil = Time.time + 4.5f;
            flash = 1f;
            mech.Show(3f);
            PlayVoice(PusherAudio.Id.Payout, transform.TransformPoint(0f, 1.2f, 0f), 1f);
        }

        /// <summary>Une balle jackpot est tombée : ascenseur, roue « spéciale balle », puis la balle retombe sur la plaque.</summary>
        public void OnBallCollected(PusherBall ball)
        {
            if (ball == null || ball.InLift) return;
            ball.InLift = true;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Add(CurrencyType.Keys, 1);   // balle jackpot = 1 clé dorée
            flash = 1f;
            StatusText = "BALLE JACKPOT !";
            bonusMessageUntil = Time.time + 3f;
            if (mech != null) mech.Show(4f);
            PlayVoice(PusherAudio.Id.Payout, ball.transform.position, 1f, 1.2f);
            StartCoroutine(BallLift(ball));
        }

        private System.Collections.IEnumerator BallLift(PusherBall ball)
        {
            yield return new WaitForSeconds(0.5f);
            ball.Body.isKinematic = true;
            if (ball.Col != null) ball.Col.enabled = false;
            Vector3 tube = liftTube != null ? transform.InverseTransformPoint(liftTube.position) : new Vector3(0.6f, 1.2f, -0.5f);
            Vector3[] path =
            {
                transform.InverseTransformPoint(ball.transform.position),
                new Vector3(tube.x, 0.93f, tube.z),
                new Vector3(tube.x, 1.44f, tube.z),
                new Vector3(Random.Range(-0.45f, 0.45f), 1.42f, Random.Range(-0.02f, 0.12f)),
            };
            float[] durations = { 0.5f, 1.6f, 0.7f };
            for (int k = 0; k < durations.Length; k++)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / durations[k];
                    float e = Mathf.SmoothStep(0f, 1f, t);
                    ball.transform.position = transform.TransformPoint(Vector3.Lerp(path[k], path[k + 1], e));
                    ball.transform.Rotate(Vector3.right, 540f * Time.deltaTime, Space.Self);
                    yield return null;
                }
                if (k == 1) { StartWheel(true); PlayVoice(PusherAudio.Id.Insert, ball.transform.position, 0.8f, 0.7f); }
            }
            if (ball.Col != null) ball.Col.enabled = true;
            ball.Body.isKinematic = false;
            ball.Body.linearVelocity = Vector3.down * 0.3f;
            ball.InLift = false;
            runUntil = Mathf.Max(runUntil, Time.time + 10f);
        }

        /// <summary>Pluie de pièces qui tombent du plafond, partout sur le plateau (retournées au hasard).</summary>
        private System.Collections.IEnumerator CeilingRain(int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnCeilingCoin();
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnCeilingCoin()
        {
            PusherCoin coin;
            if (pool.Count > 0) coin = pool.Pop();
            else { coin = Instantiate(coinTemplate.gameObject, coinRoot).GetComponent<PusherCoin>(); coin.Machine = this; }
            coin.Collected = false;
            var rb = coin.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.down * 0.5f;
            rb.angularVelocity = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-3f, 3f));
            coin.transform.SetPositionAndRotation(
                transform.TransformPoint(Random.Range(-0.55f, 0.55f), 1.42f, Random.Range(-0.1f, 0.17f)),
                transform.rotation * Random.rotation);
            coin.gameObject.SetActive(true);
            active.Add(coin);
        }

        private System.Collections.IEnumerator BillRain(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnBill();
                yield return new WaitForSeconds(0.2f);
            }
        }

        private void SpawnBill()
        {
            if (billTemplates == null || billTemplates.Length == 0) return;
            int k = Random.value < 0.1f ? billTemplates.Length - 1 : Random.Range(0, Mathf.Max(1, billTemplates.Length - 1));
            var go = Instantiate(billTemplates[k].gameObject, coinRoot);
            go.transform.SetPositionAndRotation(
                transform.TransformPoint(Random.Range(-0.5f, 0.5f), 1.42f, Random.Range(-0.1f, 0.17f)),
                transform.rotation * Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f)));
            go.SetActive(true);
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.angularVelocity = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), Random.Range(-2f, 2f));
        }

        private void RefreshStatus()
        {
            if (!IsInUse || Time.time < bonusMessageUntil) return;
            string bonus = GainMultiplier > 1 ? $"   —   GAINS x{GainMultiplier} ({Mathf.CeilToInt(Mathf.Max(doubleUntil, tripleUntil) - Time.time)} s)" : "";
            string run = Time.time < runUntil ? $"plaque en marche : {Mathf.CeilToInt(runUntil - Time.time)} s" : "plaque à l'arrêt : insère une pièce";
            StatusText = $"Gains dans le bac : {pending} (R pour récupérer)   —   {run}{bonus}";
        }
    }
}
