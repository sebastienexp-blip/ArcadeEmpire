using System;
using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Save;
using UnityEngine;

namespace ArcadeEmpire.Economy
{
    /// <summary>
    /// Succès permanents. Compteurs (parties, lots gagnés, machines différentes essayées, niveau) alimentés par les événements
    /// des machines ; chaque succès débloqué verse une récompense en pièces. État dans SaveData.stats / SaveData.achievements.
    /// Se crée tout seul au démarrage.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }
        public static event Action<string, long> Unlocked;   // (titre, pièces)

        public class Def
        {
            public string id, title, desc, stat; public int target; public long reward;
            public Def(string id, string title, string desc, string stat, int target, long reward)
            { this.id = id; this.title = title; this.desc = desc; this.stat = stat; this.target = target; this.reward = reward; }
        }

        public static readonly Def[] All =
        {
            new Def("first_play", "Premier essai", "Lance une première partie", "plays", 1, 50),
            new Def("plays_50", "Habitué", "Joue 50 parties", "plays", 50, 300),
            new Def("plays_500", "Accro à l'arcade", "Joue 500 parties", "plays", 500, 2500),
            new Def("prize_1", "Premier lot", "Gagne un lot à la pince", "prizes", 1, 100),
            new Def("prize_25", "Collectionneur", "Gagne 25 lots à la pince", "prizes", 25, 1000),
            new Def("machines_4", "Curieux", "Essaie 4 machines différentes", "machines", 4, 400),
            new Def("machines_8", "Touche-à-tout", "Essaie 8 machines différentes", "machines", 8, 1500),
            new Def("machines_12", "Roi de l'arcade", "Essaie 12 machines différentes", "machines", 12, 5000),
            new Def("level_10", "Gérant confirmé", "Atteins le niveau 10", "level", 10, 800),
            new Def("level_50", "Légende", "Atteins le niveau 50", "level", 50, 10000),
        };

        private SaveData Data => SaveManager.Instance != null ? SaveManager.Instance.Data : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (Instance != null) return;
            var go = new GameObject("AchievementManager");
            DontDestroyOnLoad(go);
            go.AddComponent<AchievementManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable() => MachineBase.AnyPlayStarted += OnPlay;
        private void OnDisable() { MachineBase.AnyPlayStarted -= OnPlay; MachineBase.AnyRewardWon -= OnReward; }

        private float levelTimer;

        private void Start() { MachineBase.AnyRewardWon += OnReward; }

        private void Update()
        {
            levelTimer += Time.unscaledDeltaTime;
            if (levelTimer < 2f) return;
            levelTimer = 0f;
            if (ProgressionManager.Instance != null) SetMax("level", ProgressionManager.Instance.Level);
        }

        public int Get(string stat)
        {
            var d = Data; if (d == null) return 0;
            var e = d.stats.Find(s => s.key == stat);
            return e != null ? e.value : 0;
        }

        public bool IsUnlocked(string id) => Data != null && Data.achievements.Contains(id);

        private void OnPlay(MachineBase machine)
        {
            Add("plays", 1);
            var d = Data;
            if (d != null && machine != null && machine.Data != null)
            {
                string key = "played:" + machine.Data.id;
                if (!d.achievements.Contains(key)) { d.achievements.Add(key); Add("machines", 1); }
            }
        }

        private void OnReward(MachineBase machine, RewardData reward) => Add("prizes", 1);

        private void Add(string stat, int amount)
        {
            var d = Data; if (d == null) return;
            var e = d.stats.Find(s => s.key == stat);
            if (e == null) { e = new StatEntry { key = stat }; d.stats.Add(e); }
            e.value += amount;
            Check();
        }

        private void SetMax(string stat, int value)
        {
            var d = Data; if (d == null) return;
            var e = d.stats.Find(s => s.key == stat);
            if (e == null) { e = new StatEntry { key = stat }; d.stats.Add(e); }
            if (value <= e.value) return;
            e.value = value;
            Check();
        }

        private void Check()
        {
            var d = Data; if (d == null) return;
            foreach (var def in All)
            {
                if (d.achievements.Contains(def.id) || Get(def.stat) < def.target) continue;
                d.achievements.Add(def.id);
                if (CurrencyManager.Instance != null) CurrencyManager.Instance.Add(CurrencyType.Coins, def.reward);
                Unlocked?.Invoke(def.title, def.reward);
            }
        }
    }
}
