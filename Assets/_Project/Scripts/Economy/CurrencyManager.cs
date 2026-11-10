using System;
using UnityEngine;

namespace ArcadeEmpire.Economy
{
    /// <summary>Les quatre monnaies du jeu.</summary>
    public enum CurrencyType
    {
        Coins = 0,          // Pièces : jouer aux machines, améliorer la salle
        Tickets = 1,        // Tickets : acheter des lots à la boutique
        PremiumTokens = 2,  // Jetons premium : obtenus uniquement en progression
        Experience = 3      // Points d'expérience
    }

    /// <summary>
    /// Gestion centralisée des monnaies. Aucune autre classe ne modifie directement un solde.
    /// À placer sur un GameObject "Managers" de la scène de démarrage.
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        /// <summary>Déclenché à chaque changement : (monnaie, nouveau solde).</summary>
        public event Action<CurrencyType, long> OnBalanceChanged;

        [Header("Soldes de départ (nouvelle partie)")]
        [SerializeField] private long startingCoins = 100;

        private readonly long[] balances = new long[Enum.GetValues(typeof(CurrencyType)).Length];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            balances[(int)CurrencyType.Coins] = startingCoins;
        }

        public long Get(CurrencyType type) => balances[(int)type];

        public bool CanAfford(CurrencyType type, long amount) => amount >= 0 && balances[(int)type] >= amount;

        public void Add(CurrencyType type, long amount)
        {
            if (amount <= 0) return;
            long current = balances[(int)type];
            // Protection contre le dépassement de capacité
            balances[(int)type] = amount > long.MaxValue - current ? long.MaxValue : current + amount;
            OnBalanceChanged?.Invoke(type, balances[(int)type]);
        }

        /// <summary>Retire le montant si le solde suffit. Retourne false sinon (rien n'est modifié).</summary>
        public bool TrySpend(CurrencyType type, long amount)
        {
            if (amount < 0 || !CanAfford(type, amount)) return false;
            balances[(int)type] -= amount;
            OnBalanceChanged?.Invoke(type, balances[(int)type]);
            return true;
        }

        /// <summary>Remplace tous les soldes (chargement d'une sauvegarde).</summary>
        public void LoadBalances(long coins, long tickets, long premiumTokens, long experience)
        {
            balances[(int)CurrencyType.Coins] = Math.Max(0, coins);
            balances[(int)CurrencyType.Tickets] = Math.Max(0, tickets);
            balances[(int)CurrencyType.PremiumTokens] = Math.Max(0, premiumTokens);
            balances[(int)CurrencyType.Experience] = Math.Max(0, experience);
            foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
                OnBalanceChanged?.Invoke(type, balances[(int)type]);
        }
    }
}
