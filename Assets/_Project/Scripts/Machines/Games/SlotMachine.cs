using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Machine à sous à 3 rouleaux. Symboles : 0 cerise, 1 citron, 2 cloche, 3 BAR, 4 sept, 5 étoile.
    /// Le résultat est tiré selon une table de probabilités (retour moyen d'environ 89 %), puis les rouleaux
    /// s'arrêtent l'un après l'autre sur les symboles correspondants. Gains en pièces (+ tickets, XP).
    /// </summary>
    public class SlotMachine : ArcadeGameMachine
    {
        [SerializeField] private Transform[] reels;

        public override string ControlsHint => "ESPACE : lancer les rouleaux      Échap : quitter";

        // Table des résultats : poids sur 1000, gain en pièces (pour une mise de 10)
        private struct Outcome { public int weight; public int symbol; public int payout; public string label; }
        private static readonly Outcome[] Table =
        {
            new Outcome { weight = 4,   symbol = 4, payout = 500, label = "JACKPOT 777 !" },
            new Outcome { weight = 15,  symbol = 3, payout = 100, label = "Triple BAR" },
            new Outcome { weight = 20,  symbol = 5, payout = 60,  label = "Triple étoile" },
            new Outcome { weight = 30,  symbol = 2, payout = 40,  label = "Triple cloche" },
            new Outcome { weight = 40,  symbol = 1, payout = 25,  label = "Triple citron" },
            new Outcome { weight = 50,  symbol = 0, payout = 20,  label = "Triple cerise" },
            new Outcome { weight = 120, symbol = -2, payout = 8,  label = "Deux cerises" },   // -2 : paire de cerises
            new Outcome { weight = 721, symbol = -1, payout = 0,  label = "Perdu" },           // -1 : aucune combinaison
        };

        /// <summary>Retour théorique moyen pour 1 pièce misée (voir Table), utile pour l'équilibrage et les tests.</summary>
        public static float ExpectedReturnPerCoin(int stake)
        {
            float sum = 0, total = 0;
            foreach (var o in Table) { sum += o.weight * o.payout; total += o.weight; }
            return sum / total / stake;
        }

        private readonly System.Random random = new System.Random();
        private float[] angle = new float[3];
        private float[] from = new float[3];
        private float[] to = new float[3];
        private float[] stopAt = new float[3];
        private int[] state = new int[3];   // 0 arrêté, 1 tourne, 2 ralentit
        private float[] stopStart = new float[3];
        private int[] result = new int[3];
        private Outcome outcome;
        private float playTime;

        public void SetReels(Transform[] r) => reels = r;

        protected override void HandleInput(Keyboard kb)
        {
            if (!(kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return;
            PressButton();
            if (IsPlaying) return;
            if (!TryStartPlay()) NotEnoughCoins();
        }

        private Outcome Pick()
        {
            int total = 0;
            foreach (var o in Table) total += o.weight;
            int r = random.Next(total);
            foreach (var o in Table) { r -= o.weight; if (r < 0) return o; }
            return Table[Table.Length - 1];
        }

        private void BuildResult(Outcome o)
        {
            if (o.symbol >= 0) { result[0] = result[1] = result[2] = o.symbol; return; }
            if (o.symbol == -2)
            {
                int other = 1 + random.Next(5);                  // 1..5 : jamais une cerise
                int pos = random.Next(3);
                for (int i = 0; i < 3; i++) result[i] = i == pos ? other : 0;
                return;
            }
            // perdu : ni triple, ni exactement deux cerises
            do
            {
                for (int i = 0; i < 3; i++) result[i] = random.Next(6);
            } while ((result[0] == result[1] && result[1] == result[2]) || CountCherries() == 2);
        }

        private int CountCherries() { int c = 0; foreach (int s in result) if (s == 0) c++; return c; }

        protected override void OnPlayStarted()
        {
            outcome = Pick();
            BuildResult(outcome);
            playTime = 0f;
            for (int i = 0; i < 3; i++) { state[i] = 1; stopAt[i] = 1.0f + i * 0.65f; }
            StatusText = "Les rouleaux tournent...";
            Sfx.Play(Sfx.Kind.Coin, transform.position, 0.5f);
        }

        protected override void Tick()
        {
            if (reels == null) return;
            if (IsPlaying)
            {
                playTime += Time.deltaTime;
                bool allStopped = true;
                for (int i = 0; i < 3; i++)
                {
                    if (state[i] == 1)
                    {
                        angle[i] -= 900f * Time.deltaTime;
                        if (playTime >= stopAt[i])
                        {
                            // ralentit jusqu'à un angle d'arrêt situé au moins 120° plus loin
                            float baseA = -60f * result[i];
                            to[i] = baseA + 360f * Mathf.Floor((angle[i] - baseA - 120f) / 360f);
                            from[i] = angle[i];
                            stopStart[i] = playTime;
                            state[i] = 2;
                        }
                    }
                    else if (state[i] == 2)
                    {
                        float t = Mathf.Clamp01((playTime - stopStart[i]) / 0.5f);
                        angle[i] = Mathf.Lerp(from[i], to[i], 1f - (1f - t) * (1f - t));
                        if (t >= 1f) { state[i] = 0; Sfx.Play(Sfx.Kind.Drop, transform.position, 0.3f); }
                    }
                    if (state[i] != 0) allStopped = false;
                }
                if (allStopped) Finish();
            }
            for (int i = 0; i < 3 && i < reels.Length; i++)
                if (reels[i] != null) reels[i].localRotation = Quaternion.Euler(angle[i], 0f, 0f);
        }

        private void Finish()
        {
            if (outcome.payout > 0)
            {
                Payout(outcome.payout / 5, outcome.payout, 10 + outcome.payout / 5);
                StatusText = $"{outcome.label}  +{outcome.payout} pièces";
                Sfx.Play(Sfx.Kind.Win, transform.position, 0.8f);
            }
            else
            {
                Payout(0, 0, 2);
                StatusText = "Perdu... ESPACE pour rejouer";
                Sfx.Play(Sfx.Kind.Miss, transform.position, 0.5f);
            }
            EndPlay();
        }
    }
}
