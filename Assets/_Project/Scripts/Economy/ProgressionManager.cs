using System;
using ArcadeEmpire.Save;
using UnityEngine;

namespace ArcadeEmpire.Economy
{
    /// <summary>
    /// Niveau du joueur (1 à 100) calculé à partir de l'XP total (monnaie Experience du CurrencyManager).
    /// Coût du niveau L → L+1 : 40 + 12·L + 0,6·L² (≈ 260 000 XP au total pour atteindre le niveau 100).
    /// Chaque niveau gagné donne des pièces ; tous les 5 niveaux, un jeton premium en plus.
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        public const int MaxLevel = 100;

        public static ProgressionManager Instance { get; private set; }

        /// <summary>Déclenché à chaque niveau gagné : (niveau, pièces offertes, jetons premium offerts).</summary>
        public event Action<int, long, int> OnLevelUp;

        public int Level { get; private set; } = 1;

        private static readonly long[] cumulative = BuildTable();

        private static long[] BuildTable()
        {
            // cumulative[L] = XP total nécessaire pour atteindre le niveau L
            var t = new long[MaxLevel + 1];
            for (int l = 2; l <= MaxLevel; l++) t[l] = t[l - 1] + CostToNext(l - 1);
            return t;
        }

        public static long CostToNext(int level) => (long)(40 + 12 * level + 0.6 * level * level);

        public static int LevelFromXp(long xp)
        {
            int level = 1;
            while (level < MaxLevel && xp >= cumulative[level + 1]) level++;
            return level;
        }

        public static long XpForLevel(int level) => cumulative[Mathf.Clamp(level, 1, MaxLevel)];

        public long Xp => CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Experience) : 0;
        public long XpIntoLevel => Level >= MaxLevel ? 0 : Xp - cumulative[Level];
        public long XpForNext => Level >= MaxLevel ? 0 : CostToNext(Level);
        public float Progress01 => Level >= MaxLevel ? 1f : Mathf.Clamp01(XpIntoLevel / (float)XpForNext);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnBalanceChanged += OnBalanceChanged;
            Recompute();
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnBalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(CurrencyType type, long balance)
        {
            if (type == CurrencyType.Experience) Recompute();
        }

        private void Recompute()
        {
            int target = LevelFromXp(Xp);
            int rewarded = SaveManager.Instance != null ? Mathf.Max(1, SaveManager.Instance.Data.playerLevel) : Level;

            // niveaux gagnés depuis la dernière récompense enregistrée (plusieurs d'un coup possible)
            while (rewarded < target)
            {
                rewarded++;
                long coins = 20 + 5 * rewarded;
                int tokens = rewarded % 5 == 0 ? 1 : 0;
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.Add(CurrencyType.Coins, coins);
                    if (tokens > 0) CurrencyManager.Instance.Add(CurrencyType.PremiumTokens, tokens);
                }
                OnLevelUp?.Invoke(rewarded, coins, tokens);
            }

            Level = target;
            if (SaveManager.Instance != null) SaveManager.Instance.Data.playerLevel = Mathf.Max(SaveManager.Instance.Data.playerLevel, target);
        }
    }
}
