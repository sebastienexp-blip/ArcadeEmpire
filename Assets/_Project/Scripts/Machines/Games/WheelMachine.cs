using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Roue de la fortune : 12 secteurs. Le secteur gagnant est tiré selon des poids fixes au lancement, puis la roue
    /// tourne et ralentit jusqu'à s'arrêter exactement sous le repère. Gain en tickets (+ XP).
    /// Le secteur 0 commence en haut et suit le sens horaire ; la roue tourne dans le sens anti-horaire (rotation Z positive).
    /// </summary>
    public class WheelMachine : ArcadeGameMachine
    {
        [SerializeField] private Transform wheel;

        public static readonly int[] Values = { 2, 5, 2, 10, 2, 5, 25, 2, 5, 2, 10, 100 };
        private static readonly int[] Weights = { 14, 10, 14, 6, 14, 10, 3, 14, 10, 14, 6, 1 };

        private const float SpinTime = 4.8f;
        private float angle;
        private float startAngle;
        private float totalRotation;
        private float elapsed;
        private int target;

        public void SetWheel(Transform w) => wheel = w;
        public override string ControlsHint => "ESPACE : lancer la roue      Échap : quitter";

        /// <summary>Tirage pondéré d'un secteur.</summary>
        public static int PickSector(System.Random rng)
        {
            int sum = 0;
            foreach (int w in Weights) sum += w;
            int r = rng.Next(sum);
            for (int i = 0; i < Weights.Length; i++) { r -= Weights[i]; if (r < 0) return i; }
            return 0;
        }

        private readonly System.Random random = new System.Random();

        protected override void HandleInput(Keyboard kb)
        {
            if (!(kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return;
            PressButton();
            if (IsPlaying) return;
            if (!TryStartPlay()) NotEnoughCoins();
        }

        protected override void OnPlayStarted()
        {
            target = PickSector(random);
            startAngle = angle;
            float landing = (target + 0.5f) * 30f + ((float)random.NextDouble() - 0.5f) * 18f;   // reste à l'intérieur du secteur
            float baseDelta = Mathf.Repeat(landing - startAngle, 360f);
            totalRotation = 360f * (4 + random.Next(3)) + baseDelta;
            elapsed = 0f;
            StatusText = "La roue tourne...";
            Sfx.Play(Sfx.Kind.Coin, transform.position, 0.5f);
        }

        protected override void Tick()
        {
            if (wheel == null) return;
            if (IsPlaying)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / SpinTime);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                angle = startAngle + totalRotation * eased;
                if (t >= 1f) Finish();
            }
            wheel.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Finish()
        {
            int tickets = Values[target];
            Payout(tickets, 0, 5 + tickets);
            StatusText = tickets >= 25 ? $"JACKPOT ! +{tickets} tickets" : $"+{tickets} tickets";
            Sfx.Play(tickets >= 10 ? Sfx.Kind.Win : Sfx.Kind.Coin, transform.position, 0.7f);
            EndPlay();
        }
    }
}
