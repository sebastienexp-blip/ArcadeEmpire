using System.Collections;
using ArcadeEmpire.Economy;
using UnityEngine;

namespace ArcadeEmpire.Save
{
    /// <summary>
    /// Charge la partie au démarrage, sauvegarde automatiquement et à la mise en pause/fermeture
    /// (important sur Android). Dépend de CurrencyManager (même GameObject "Managers").
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [SerializeField] private float autoSaveIntervalSeconds = 60f;

        /// <summary>Données en mémoire. Les futurs systèmes (inventaire, machines…) y écrivent.</summary>
        public SaveData Data { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Data = SaveSystem.Load();
        }

        private void Start()
        {
            // Start (et non Awake) : CurrencyManager.Instance est garanti disponible.
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.LoadBalances(Data.coins, Data.tickets, Data.premiumTokens, Data.experience);
            }
            else
            {
                Debug.LogError("[SaveManager] CurrencyManager introuvable : ajoutez-le au même GameObject.");
            }
            StartCoroutine(AutoSaveRoutine());
        }

        /// <summary>Sauvegarde manuelle (bouton "Sauvegarder" du menu).</summary>
        public bool SaveNow()
        {
            if (CurrencyManager.Instance != null)
            {
                var c = CurrencyManager.Instance;
                Data.coins = c.Get(CurrencyType.Coins);
                Data.tickets = c.Get(CurrencyType.Tickets);
                Data.premiumTokens = c.Get(CurrencyType.PremiumTokens);
                Data.experience = c.Get(CurrencyType.Experience);
            }
            return SaveSystem.Save(Data);
        }

        private IEnumerator AutoSaveRoutine()
        {
            var wait = new WaitForSecondsRealtime(Mathf.Max(10f, autoSaveIntervalSeconds));
            while (true)
            {
                yield return wait;
                SaveNow();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && Data != null) SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (Data != null) SaveNow();
        }
    }
}
