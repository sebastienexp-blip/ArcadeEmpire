using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Stand de tir : 25 s, 9 cibles qui se lèvent au hasard. Le viseur se déplace au clavier (ZQSD / flèches), ESPACE tire.
    /// Cible normale +1, cible dorée +3 (brève), bombe -2 (à éviter). Tickets = score / 2.
    /// Le test de touche est un calcul de distance entre le viseur et le centre de la cible levée.
    /// </summary>
    public class ShootingMachine : ArcadeGameMachine
    {
        [SerializeField] private Transform crosshair;
        [SerializeField] private Transform[] targets;      // pivots (bas de la cible)
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Material[] faceMaterials;  // 0 normale, 1 dorée, 2 bombe

        private const float RoundTime = 25f;
        private const float HitRadius = 0.2f;
        private const float TargetCenterOffsetY = 0.17f;
        private const float PlaneZ = 0.95f;

        private enum TType { Normal, Gold, Bomb }
        private TType[] types;
        private float[] angle;       // 0 = levée, 90 = cachée
        private float[] upTimer;     // temps restant levée (>0 = demandée levée)
        private bool[] hitFlag;

        private Vector2 aim = new Vector2(0f, 1.5f);
        private float timeLeft;
        private float nextSpawn;
        private float nextShot;
        private int score;
        private float lastStatus;
        private readonly System.Random random = new System.Random();

        public override string ControlsHint => "ZQSD / flèches : viser      ESPACE : tirer      Échap : quitter";

        public void Setup(Transform cross, Transform[] pivots, Renderer[] renderers, Material[] faces)
        {
            crosshair = cross; targets = pivots; targetRenderers = renderers; faceMaterials = faces;
        }


        protected override void Awake()
        {
            base.Awake();
            int n = targets != null ? targets.Length : 0;
            types = new TType[n]; angle = new float[n]; upTimer = new float[n]; hitFlag = new bool[n];
            for (int i = 0; i < n; i++) angle[i] = 90f;
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
            Vector2 move = Vector2.zero;
            if (kb.aKey.isPressed || kb.qKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            if (kb.wKey.isPressed || kb.zKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            aim += move.normalized * 1.1f * Time.deltaTime;
            aim.x = Mathf.Clamp(aim.x, -0.65f, 0.65f);
            aim.y = Mathf.Clamp(aim.y, 0.8f, 2.3f);

            if ((kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) && Time.time >= nextShot)
            {
                nextShot = Time.time + 0.25f;
                PressButton();
                Fire();
            }
        }

        private void Fire()
        {
            Sfx.Play(Sfx.Kind.Grab, transform.position, 0.5f);
            for (int i = 0; i < targets.Length; i++)
            {
                if (angle[i] > 35f || hitFlag[i]) continue;   // seulement les cibles bien levées
                Vector3 c = targets[i].localPosition;
                Vector2 center = new Vector2(c.x, c.y + TargetCenterOffsetY);
                if ((center - aim).magnitude > HitRadius) continue;
                hitFlag[i] = true; upTimer[i] = 0f;
                int gain = types[i] == TType.Gold ? 3 : (types[i] == TType.Bomb ? -2 : 1);
                score = Mathf.Max(0, score + gain);
                Sfx.Play(gain > 0 ? Sfx.Kind.Coin : Sfx.Kind.Miss, transform.position, 0.6f);
                break;   // un tir = une cible
            }
        }

        protected override void OnPlayStarted()
        {
            timeLeft = RoundTime; score = 0; nextSpawn = Time.time + 0.5f; nextShot = 0f;
            for (int i = 0; i < targets.Length; i++) { upTimer[i] = 0f; hitFlag[i] = false; }
        }

        protected override void OnPlayEnded()
        {
            for (int i = 0; i < targets.Length; i++) { upTimer[i] = 0f; hitFlag[i] = false; }
            if (timeLeft <= 0f)
            {
                int tickets = score / 2;
                Payout(tickets, 0, 4 + score);
                StatusText = $"Score {score}  +{tickets} tickets";
                Sfx.Play(tickets > 0 ? Sfx.Kind.Win : Sfx.Kind.Miss, transform.position, 0.7f);
            }
        }

        protected override void Tick()
        {
            if (targets == null) return;
            if (IsPlaying)
            {
                timeLeft -= Time.deltaTime;
                if (Time.time >= nextSpawn) Spawn();
                if (Time.time - lastStatus > 0.1f)
                {
                    lastStatus = Time.time;
                    StatusText = $"{Mathf.CeilToInt(Mathf.Max(0f, timeLeft))} s    score {score}";
                }
                if (timeLeft <= 0f) EndPlay();
            }
            for (int i = 0; i < targets.Length; i++)
            {
                if (upTimer[i] > 0f) upTimer[i] -= Time.deltaTime;
                float goal = upTimer[i] > 0f && !hitFlag[i] ? 0f : 90f;
                angle[i] = Mathf.MoveTowards(angle[i], goal, 420f * Time.deltaTime);
                if (angle[i] >= 90f) hitFlag[i] = false;
                targets[i].localRotation = Quaternion.Euler(angle[i], 0f, 0f);
            }
            if (crosshair != null) crosshair.localPosition = new Vector3(aim.x, aim.y, PlaneZ - 0.05f);
        }

        private void Spawn()
        {
            nextSpawn = Time.time + Mathf.Lerp(0.5f, 0.8f, (float)random.NextDouble());
            int start = random.Next(targets.Length);
            for (int k = 0; k < targets.Length; k++)
            {
                int i = (start + k) % targets.Length;
                if (angle[i] < 90f || upTimer[i] > 0f) continue;
                double r = random.NextDouble();
                types[i] = r < 0.70 ? TType.Normal : (r < 0.82 ? TType.Gold : TType.Bomb);
                upTimer[i] = types[i] == TType.Gold ? 0.9f : (types[i] == TType.Bomb ? 1.5f : 1.3f);
                hitFlag[i] = false;
                if (targetRenderers != null && i < targetRenderers.Length && targetRenderers[i] != null && faceMaterials != null)
                {
                    var mats = targetRenderers[i].sharedMaterials;
                    mats[0] = faceMaterials[(int)types[i]];
                    targetRenderers[i].sharedMaterials = mats;
                }
                return;
            }
        }
    }
}
