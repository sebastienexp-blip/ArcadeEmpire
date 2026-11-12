using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Testeur de force : après paiement, une jauge de puissance oscille ; ESPACE frappe. La hauteur atteinte par le
    /// disque dépend de la puissance réelle au moment du coup (0 à 10 voyants, cloche au maximum). Gain en tickets.
    /// </summary>
    public class HammerMachine : ArcadeGameMachine
    {
        [SerializeField] private Transform puck;
        [SerializeField] private float puckBottomY = 0.62f;
        [SerializeField] private float puckTopY = 2.3f;
        [SerializeField] private Renderer[] lamps;
        [SerializeField] private Material lampOn;
        [SerializeField] private Material lampOff;

        public static readonly int[] TicketsByLevel = { 0, 0, 1, 1, 1, 2, 2, 3, 5, 8, 15 };

        private const float MeterPeriod = 1.3f;   // aller-retour complet
        private enum Phase { Idle, Aiming, Rising, Falling, Result }
        private Phase phase;
        private float phaseTime;
        private float meter;
        private float power;
        private float puckHeight;      // 0..1
        private float lastStatus;

        public override string ControlsHint => "ESPACE : lancer / frapper au bon moment      Échap : quitter";

        public void Setup(Transform puckT, Renderer[] lampRenderers, Material on, Material off, float bottomY, float topY)
        {
            puck = puckT; lamps = lampRenderers; lampOn = on; lampOff = off; puckBottomY = bottomY; puckTopY = topY;
        }

        protected override void HandleInput(Keyboard kb)
        {
            if (!(kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return;
            PressButton();
            if (phase == Phase.Aiming) { Strike(); return; }
            if (IsPlaying) return;
            if (!TryStartPlay()) NotEnoughCoins();
        }

        protected override void OnPlayStarted()
        {
            phase = Phase.Aiming;
            phaseTime = 0f;
            StatusText = "Frappe quand la jauge est au maximum !";
        }

        protected override void OnPlayEnded()
        {
            phase = Phase.Idle;
            puckHeight = 0f;
            SetLamps(0);
        }

        private void Strike()
        {
            power = meter;
            phase = Phase.Rising;
            phaseTime = 0f;
            Sfx.Play(Sfx.Kind.Grab, transform.position, 0.8f);
        }

        private static int LevelOf(float p) => Mathf.Clamp(Mathf.RoundToInt(p * 10f), 0, 10);

        protected override void Tick()
        {
            switch (phase)
            {
                case Phase.Aiming:
                    phaseTime += Time.deltaTime;
                    meter = Mathf.PingPong(phaseTime * 2f / MeterPeriod, 1f);
                    if (Time.time - lastStatus > 0.05f)
                    {
                        lastStatus = Time.time;
                        int filled = Mathf.RoundToInt(meter * 20f);
                        StatusText = "[" + new string('#', filled) + new string('-', 20 - filled) + "]  ESPACE !";
                    }
                    break;
                case Phase.Rising:
                {
                    phaseTime += Time.deltaTime;
                    float t = Mathf.Clamp01(phaseTime / (0.35f + 0.9f * power));
                    puckHeight = power * (1f - (1f - t) * (1f - t));
                    SetLamps(LevelOf(puckHeight));
                    if (t >= 1f) { phase = Phase.Falling; phaseTime = 0f; Result(); }
                    break;
                }
                case Phase.Falling:
                    phaseTime += Time.deltaTime;
                    puckHeight = Mathf.Max(0f, puckHeight - Time.deltaTime * 0.6f);
                    if (phaseTime > 2.2f) EndPlay();
                    break;
            }
            if (puck != null)
            {
                Vector3 p = puck.localPosition;
                p.y = Mathf.Lerp(puckBottomY, puckTopY, puckHeight);
                puck.localPosition = p;
            }
        }

        private void Result()
        {
            int level = LevelOf(power);
            int tickets = TicketsByLevel[level];
            Payout(tickets, 0, 4 + level);
            StatusText = level >= 10 ? $"CLOCHE ! +{tickets} tickets" : $"Niveau {level}/10  +{tickets} tickets";
            Sfx.Play(level >= 9 ? Sfx.Kind.Win : (level >= 4 ? Sfx.Kind.Coin : Sfx.Kind.Miss), transform.position, 0.8f);
        }

        private void SetLamps(int level)
        {
            if (lamps == null) return;
            for (int i = 0; i < lamps.Length; i++)
                if (lamps[i] != null) lamps[i].sharedMaterial = i < level ? lampOn : lampOff;
        }
    }
}
