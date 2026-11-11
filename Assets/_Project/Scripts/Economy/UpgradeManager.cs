using ArcadeEmpire.Save;

namespace ArcadeEmpire.Economy
{
    /// <summary>
    /// Améliorations achetables avec des pièces, par machine (niveau stocké dans SaveData.upgrades).
    /// Effet actuel : +10 % de force de prise par niveau sur la machine à pinces.
    /// </summary>
    public static class UpgradeManager
    {
        public const int MaxLevel = 5;

        public static int GetLevel(string machineId)
        {
            if (SaveManager.Instance == null || string.IsNullOrEmpty(machineId)) return 0;
            UpgradeEntry e = SaveManager.Instance.Data.upgrades.Find(x => x.machineId == machineId);
            return e == null ? 0 : e.level;
        }

        /// <summary>Prix du prochain niveau en pièces, ou -1 si le niveau maximum est atteint.</summary>
        public static long NextCost(string machineId)
        {
            int level = GetLevel(machineId);
            return level >= MaxLevel ? -1 : 150L * (level + 1);
        }

        public static bool TryUpgrade(string machineId)
        {
            long cost = NextCost(machineId);
            if (cost < 0 || CurrencyManager.Instance == null || SaveManager.Instance == null) return false;
            if (!CurrencyManager.Instance.TrySpend(CurrencyType.Coins, cost)) return false;

            var list = SaveManager.Instance.Data.upgrades;
            UpgradeEntry e = list.Find(x => x.machineId == machineId);
            if (e == null) list.Add(new UpgradeEntry { machineId = machineId, level = 1 });
            else e.level++;
            return true;
        }
    }
}
