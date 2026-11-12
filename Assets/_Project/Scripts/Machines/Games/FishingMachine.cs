using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Pêche : des poissons nagent en cercle dans le bassin. Le chariot (ZQSD / flèches) se place au-dessus, ESPACE plonge
    /// la ligne ; au moment où l'hameçon touche l'eau, le poisson le plus proche (dans le rayon de prise) est attrapé.
    /// Il faut anticiper le déplacement des poissons. 30 s. Petit poisson : 1 ticket, bleu : 2, gros doré : 5.
    /// </summary>
    public class FishingMachine : ArcadeGameMachine
    {
        [SerializeField] private Transform carriage;
        [SerializeField] private Transform hook;
        [SerializeField] private Transform cable;
        [SerializeField] private Transform[] fish;

        private const float RoundTime = 30f;
        private const float CarriageY = 1.95f, HookRestY = 1.45f, HookWaterY = 0.8f, FishY = 0.76f;
        private const float DipDown = 0.45f, DipUp = 0.55f;
        private const float CatchRadius = 0.11f;

        // une ligne par poisson : centre x, z, rayons x, z, vitesse angulaire (rad/s), valeur en tickets
        private static readonly float[,] Orbits =
        {
            { -0.25f, -0.20f, 0.22f, 0.18f,  0.9f, 1 },
            {  0.25f, -0.25f, 0.20f, 0.20f, -0.8f, 1 },
            { -0.20f,  0.25f, 0.24f, 0.16f, -1.0f, 1 },
            {  0.28f,  0.22f, 0.20f, 0.18f,  0.7f, 1 },
            {  0.00f,  0.00f, 0.35f, 0.30f,  0.6f, 2 },
            {  0.00f,  0.00f, 0.22f, 0.40f, -0.75f, 2 },
            { -0.05f,  0.05f, 0.42f, 0.34f,  1.2f, 5 },
            {  0.10f, -0.05f, 0.30f, 0.25f, -0.55f, 1 },
        };

        public static int FishCount => Orbits.GetLength(0);
        public static int ValueOf(int i) => (int)Orbits[i, 5];

        private Vector2 carriagePos;
        private float[] fishAngle;
        private float[] respawn;
        private float dipTime = -1f;
        private bool checkedCatch;
        private float timeLeft;
        private int tickets;
        private int caught;
        private float lastStatus;
        private readonly System.Random random = new System.Random();

        public override string ControlsHint => "ZQSD / flèches : déplacer le chariot      ESPACE : plonger l'hameçon      Échap : quitter";

        public void Setup(Transform car, Transform hookT, Transform cableT, Transform[] fishes)
        {
            carriage = car; hook = hookT; cable = cableT; fish = fishes;
        }

        protected override void Awake()
        {
            base.Awake();
            int n = FishCount;
            fishAngle = new float[n]; respawn = new float[n];
            for (int i = 0; i < n; i++) fishAngle[i] = (float)(random.NextDouble() * Mathf.PI * 2);
        }

        protected override void HandleInput(Keyboard kb)
        {
            if (!IsPlaying)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                {
                    PressButton();
                    if (!TryStartPlay()) NotEnoughCoins();
                }
                return;
            }
            if (dipTime < 0f)
            {
                Vector2 move = Vector2.zero;
                if (kb.aKey.isPressed || kb.qKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.wKey.isPressed || kb.zKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                carriagePos += move.normalized * 0.7f * Time.deltaTime;
                carriagePos.x = Mathf.Clamp(carriagePos.x, -0.55f, 0.55f);
                carriagePos.y = Mathf.Clamp(carriagePos.y, -0.55f, 0.55f);
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                {
                    PressButton();
                    dipTime = 0f; checkedCatch = false;
                    Sfx.Play(Sfx.Kind.Grab, transform.position, 0.4f);
                }
            }
        }

        protected override void OnPlayStarted()
        {
            timeLeft = RoundTime; tickets = 0; caught = 0; dipTime = -1f;
            for (int i = 0; i < FishCount; i++) respawn[i] = 0f;
        }

        protected override void OnPlayEnded()
        {
            dipTime = -1f;
            if (timeLeft <= 0f)
            {
                Payout(tickets, 0, 4 + tickets * 2);
                StatusText = $"{caught} poissons  +{tickets} tickets";
                Sfx.Play(tickets > 0 ? Sfx.Kind.Win : Sfx.Kind.Miss, transform.position, 0.7f);
            }
        }

        protected override void Tick()
        {
            // poissons
            for (int i = 0; i < FishCount && fish != null && i < fish.Length; i++)
            {
                if (fish[i] == null) continue;
                if (respawn[i] > 0f)
                {
                    respawn[i] -= Time.deltaTime;
                    fish[i].gameObject.SetActive(respawn[i] <= 0f);
                    if (respawn[i] > 0f) continue;
                }
                float w = Orbits[i, 4];
                fishAngle[i] += w * Time.deltaTime;
                float a = fishAngle[i];
                Vector3 p = new Vector3(Orbits[i, 0] + Orbits[i, 2] * Mathf.Cos(a), FishY, Orbits[i, 1] + Orbits[i, 3] * Mathf.Sin(a));
                Vector3 dir = new Vector3(-Orbits[i, 2] * Mathf.Sin(a), 0f, Orbits[i, 3] * Mathf.Cos(a)) * Mathf.Sign(w);
                fish[i].localPosition = p;
                if (dir.sqrMagnitude > 1e-6f) fish[i].localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }

            float hookY = HookRestY;
            if (IsPlaying)
            {
                timeLeft -= Time.deltaTime;
                if (dipTime >= 0f)
                {
                    dipTime += Time.deltaTime;
                    if (dipTime < DipDown) hookY = Mathf.Lerp(HookRestY, HookWaterY, dipTime / DipDown);
                    else
                    {
                        hookY = Mathf.Lerp(HookWaterY, HookRestY, Mathf.Clamp01((dipTime - DipDown) / DipUp));
                        if (!checkedCatch) { checkedCatch = true; TryCatch(); }
                        if (dipTime >= DipDown + DipUp) dipTime = -1f;
                    }
                }
                if (Time.time - lastStatus > 0.1f)
                {
                    lastStatus = Time.time;
                    StatusText = $"{Mathf.CeilToInt(Mathf.Max(0f, timeLeft))} s    poissons {caught}    tickets {tickets}";
                }
                if (timeLeft <= 0f) EndPlay();
            }

            if (carriage != null) carriage.localPosition = new Vector3(carriagePos.x, CarriageY, carriagePos.y);
            if (hook != null) hook.localPosition = new Vector3(carriagePos.x, hookY, carriagePos.y);
            if (cable != null)
            {
                float len = Mathf.Max(0.01f, CarriageY - hookY);
                cable.localPosition = new Vector3(carriagePos.x, hookY + len * 0.5f, carriagePos.y);
                cable.localScale = new Vector3(0.008f, len * 0.5f, 0.008f);
            }
        }

        private void TryCatch()
        {
            int best = -1; float bestDist = CatchRadius;
            for (int i = 0; i < FishCount && fish != null && i < fish.Length; i++)
            {
                if (fish[i] == null || respawn[i] > 0f) continue;
                Vector3 p = fish[i].localPosition;
                float d = new Vector2(p.x - carriagePos.x, p.z - carriagePos.y).magnitude;
                if (d < bestDist) { bestDist = d; best = i; }
            }
            if (best < 0) { Sfx.Play(Sfx.Kind.Miss, transform.position, 0.3f); return; }
            int value = ValueOf(best);
            tickets += value; caught++;
            respawn[best] = 3f;
            fish[best].gameObject.SetActive(false);
            Sfx.Play(value >= 5 ? Sfx.Kind.Win : Sfx.Kind.Coin, transform.position, 0.7f);
        }
    }
}
