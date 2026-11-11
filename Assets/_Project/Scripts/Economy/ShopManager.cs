using ArcadeEmpire.Data;

namespace ArcadeEmpire.Economy
{
    /// <summary>Boutique : achat de lots avec des tickets (prix = RewardData.shopTicketPrice, niveau minimum selon la rareté).</summary>
    public static class ShopManager
    {
        public enum Result { Ok, NotForSale, LevelTooLow, NotEnoughTickets, Unavailable }

        public static bool IsForSale(RewardData r) => r != null && r.shopTicketPrice > 0;

        public static Result Buy(RewardData reward)
        {
            if (!IsForSale(reward)) return Result.NotForSale;
            if (CurrencyManager.Instance == null || InventoryManager.Instance == null) return Result.Unavailable;
            int level = ProgressionManager.Instance != null ? ProgressionManager.Instance.Level : 1;
            if (level < reward.ShopMinLevel) return Result.LevelTooLow;
            if (!CurrencyManager.Instance.TrySpend(CurrencyType.Tickets, reward.shopTicketPrice)) return Result.NotEnoughTickets;

            InventoryManager.Instance.Add(reward, 1);
            return Result.Ok;
        }
    }
}
