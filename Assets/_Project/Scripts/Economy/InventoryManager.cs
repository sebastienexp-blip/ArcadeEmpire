using System;
using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Save;
using UnityEngine;

namespace ArcadeEmpire.Economy
{
    /// <summary>
    /// Gestion centralisée de l'inventaire. Les données vivent dans SaveData.inventory (donc sauvegardées
    /// automatiquement) ; aucun autre script ne modifie cette liste directement.
    /// À placer sur le GameObject "Managers" de la scène Boot.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private RewardCatalog catalog;

        public RewardCatalog Catalog => catalog;

        /// <summary>Déclenché à chaque modification de l'inventaire.</summary>
        public event Action OnChanged;

        // utilisé seulement si SaveManager est absent (scène lancée seule dans l'éditeur)
        private readonly List<InventoryEntry> fallback = new List<InventoryEntry>();

        private List<InventoryEntry> Entries =>
            SaveManager.Instance != null ? SaveManager.Instance.Data.inventory : fallback;

        public void Configure(RewardCatalog rewardCatalog) => catalog = rewardCatalog;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public int Count(string rewardId)
        {
            InventoryEntry e = Entries.Find(x => x.rewardId == rewardId);
            return e == null ? 0 : e.count;
        }

        public int TotalItems
        {
            get
            {
                int total = 0;
                foreach (InventoryEntry e in Entries) total += e.count;
                return total;
            }
        }

        /// <summary>Nombre de récompenses différentes déjà obtenues (progression de la collection).</summary>
        public int DistinctOwned
        {
            get
            {
                int n = 0;
                foreach (InventoryEntry e in Entries) if (e.count > 0) n++;
                return n;
            }
        }

        public void Add(RewardData reward, int amount = 1)
        {
            if (reward == null || amount <= 0) return;
            InventoryEntry e = Entries.Find(x => x.rewardId == reward.id);
            if (e == null) Entries.Add(new InventoryEntry { rewardId = reward.id, count = amount });
            else e.count += amount;
            OnChanged?.Invoke();
        }

        /// <summary>Vend des exemplaires contre des pièces (prix selon la rareté). Retourne false si stock insuffisant.</summary>
        public bool Sell(RewardData reward, int amount)
        {
            if (reward == null || amount <= 0) return false;
            InventoryEntry e = Entries.Find(x => x.rewardId == reward.id);
            if (e == null || e.count < amount) return false;

            e.count -= amount;
            if (e.count == 0) Entries.Remove(e);
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.Add(CurrencyType.Coins, reward.SellValue * amount);
            OnChanged?.Invoke();
            return true;
        }
    }
}
