using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Course d'arcade : voiture vue de l'arrière sur 3 voies, la route défile sur l'écran (décalage de texture),
    /// des voitures rivales arrivent en perspective simulée (échelle et position selon la profondeur t, 0 = horizon, 1 = bas de l'écran).
    /// A/D ou flèches : changer de voie. 40 s. Esquive = +1 ; collision : -3 s et ralentissement.
    /// Tickets = esquives / 3, +4 si aucune collision.
    /// </summary>
    public class RaceMachine : ArcadeGameMachine
    {
        [SerializeField] private Renderer screen;
        [SerializeField] private Transform playerCar;
        [SerializeField] private Transform[] rivals;

        private const float RoundTime = 40f, PlayerT = 0.88f, YTop = 2.0f, YBottom = 1.2f, LaneW = 0.175f, CarZ = -0.34f;
        private const float ScreenCenterX = 0f;

        private float laneF = 1f;
        private int lane = 1;
        private float[] rt;
        private int[] rl;
        private bool[] active;
        private float timeLeft, speed, nextSpawn, scroll, flash;
        private int dodged, crashes;
        private float lastStatus;
        private Material screenMat;
        private readonly System.Random random = new System.Random();

        public override string ControlsHint => "A / D ou flèches : changer de voie      Échap : quitter";

        public void Setup(Renderer screenRenderer, Transform player, Transform[] rivalCars)
        {
            screen = screenRenderer; playerCar = player; rivals = rivalCars;
        }

        protected override void Awake()
        {
            base.Awake();
            int n = rivals != null ? rivals.Length : 0;
            rt = new float[n]; rl = new int[n]; active = new bool[n];
            if (screen != null)
            {
                var mats = screen.materials;   // instances : on ne modifie pas l'asset
                for (int i = 0; i < mats.Length; i++) if (mats[i].name.StartsWith("Race_Screen")) screenMat = mats[i];
            }
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
            if (kb.aKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) lane = Mathf.Max(0, lane - 1);
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) lane = Mathf.Min(2, lane + 1);
        }

        protected override void OnPlayStarted()
        {
            timeLeft = RoundTime; speed = 0.5f; dodged = 0; crashes = 0; lane = 1; laneF = 1f; flash = 0f;
            nextSpawn = Time.time + 0.6f;
            for (int i = 0; i < active.Length; i++) active[i] = false;
        }

        protected override void OnPlayEnded()
        {
            for (int i = 0; i < active.Length; i++) active[i] = false;
            if (timeLeft <= 0f)
            {
                int tickets = dodged / 3 + (crashes == 0 ? 4 : 0);
                Payout(tickets, 0, 4 + dodged);
                StatusText = $"{dodged} esquives, {crashes} collisions  +{tickets} tickets";
                Sfx.Play(tickets > 0 ? Sfx.Kind.Win : Sfx.Kind.Miss, transform.position, 0.7f);
            }
        }

        private static Vector3 Place(float lanePos, float t, out float scale)
        {
            scale = Mathf.Lerp(0.35f, 1.25f, t);
            float x = (lanePos - 1f) * LaneW * Mathf.Lerp(0.3f, 1f, t) + ScreenCenterX;
            return new Vector3(x, Mathf.Lerp(YTop, YBottom, t), CarZ);
        }

        protected override void Tick()
        {
            if (rivals == null) return;
            laneF = Mathf.MoveTowards(laneF, lane, 6f * Time.deltaTime);
            if (IsPlaying)
            {
                float dt = Time.deltaTime;
                timeLeft -= dt;
                speed = Mathf.Min(1.4f, speed + 0.012f * dt * 5f);
                if (flash > 0f) flash -= dt;
                scroll += speed * dt * 1.2f;
                if (Time.time >= nextSpawn) Spawn();
                for (int i = 0; i < rivals.Length; i++)
                {
                    if (!active[i]) continue;
                    rt[i] += speed * dt * (0.25f + 0.75f * rt[i] + 0.15f);
                    if (rt[i] >= 1.1f) { active[i] = false; dodged++; Sfx.Play(Sfx.Kind.Drop, transform.position, 0.15f); continue; }
                    if (Mathf.Abs(rt[i] - PlayerT) < 0.07f && Mathf.Abs(rl[i] - laneF) < 0.7f)
                    {
                        active[i] = false; crashes++;
                        timeLeft -= 3f; speed *= 0.6f; flash = 0.4f;
                        Sfx.Play(Sfx.Kind.Miss, transform.position, 0.6f);
                    }
                }
                if (Time.time - lastStatus > 0.1f)
                {
                    lastStatus = Time.time;
                    StatusText = $"{Mathf.CeilToInt(Mathf.Max(0f, timeLeft))} s    esquives {dodged}    collisions {crashes}";
                }
                if (timeLeft <= 0f) EndPlay();
            }
            else scroll += 0.15f * Time.deltaTime;   // l'écran défile doucement en attente

            if (screenMat != null)
            {
                var off = new Vector2(0f, scroll);
                if (screenMat.HasProperty("_BaseMap")) { screenMat.SetTextureScale("_BaseMap", new Vector2(1f, 2f)); screenMat.SetTextureOffset("_BaseMap", off); }
                if (screenMat.HasProperty("_EmissionMap")) { screenMat.SetTextureScale("_EmissionMap", new Vector2(1f, 2f)); screenMat.SetTextureOffset("_EmissionMap", off); }
            }
            if (playerCar != null)
            {
                Vector3 p = Place(laneF, PlayerT, out float s);
                playerCar.localPosition = p;
                playerCar.localScale = Vector3.one * s;
                playerCar.localRotation = Quaternion.Euler(0f, 0f, flash > 0f ? Mathf.Sin(Time.time * 60f) * 8f : 0f);
            }
            for (int i = 0; i < rivals.Length; i++)
            {
                if (rivals[i] == null) continue;
                rivals[i].gameObject.SetActive(active[i]);
                if (!active[i]) continue;
                rivals[i].localPosition = Place(rl[i], rt[i], out float s);
                rivals[i].localScale = Vector3.one * s;
            }
        }

        private void Spawn()
        {
            nextSpawn = Time.time + Mathf.Max(0.35f, 1.1f - speed * 0.5f) * (0.8f + 0.4f * (float)random.NextDouble());
            for (int i = 0; i < rivals.Length; i++)
            {
                if (active[i]) continue;
                active[i] = true; rt[i] = 0f; rl[i] = random.Next(3);
                return;
            }
        }
    }
}
