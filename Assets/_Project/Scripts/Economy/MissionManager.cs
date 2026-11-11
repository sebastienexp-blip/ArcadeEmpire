using System;
using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Save;
using UnityEngine;

namespace ArcadeEmpire.Economy
{
    /// <summary>
    /// Missions quotidiennes : 3 missions tirées chaque jour (UTC) parmi 4 types, progression suivie en temps réel
    /// via les événements des machines, récompense (pièces + XP) versée automatiquement à l'accomplissement.
    /// État stocké dans SaveData.missions (id « type:objectif ») et SaveData.missionsDate.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public event Action<string, long, long> OnMissionCompleted;   // (titre, pièces, XP)

        private static readonly string[] Types = { "play_claw", "win_prizes", "push_coins", "win_rare" };
        private static readonly int[][] Targets =
        {
            new[] { 3, 5, 8 },
            new[] { 2, 3, 5 },
            new[] { 10, 20, 40 },
            new[] { 1, 1, 2 },
        };

        private float checkTimer;

        public List<MissionProgress> Missions => SaveManager.Instance != null ? SaveManager.Instance.Data.missions : new List<MissionProgress>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            MachineBase.AnyPlayStarted += OnPlay;
            MachineBase.AnyRewardWon += OnReward;
            MachineBase.AnyCoinsWon += OnCoins;
        }

        private void OnDisable()
        {
            MachineBase.AnyPlayStarted -= OnPlay;
            MachineBase.AnyRewardWon -= OnReward;
            MachineBase.AnyCoinsWon -= OnCoins;
        }

        private void Start() => EnsureToday();

        private void Update()
        {
            checkTimer += Time.unscaledDeltaTime;
            if (checkTimer > 30f)
            {
                checkTimer = 0f;
                EnsureToday();
            }
        }

        /// <summary>Renouvelle les missions si le jour (UTC) a changé.</summary>
        public void EnsureToday()
        {
            if (SaveManager.Instance == null) return;
            SaveData data = SaveManager.Instance.Data;
            DateTime now = DateTime.UtcNow;
            string today = now.ToString("yyyy-MM-dd");
            if (data.missionsDate == today && data.missions.Count > 0) return;

            data.missionsDate = today;
            data.missions.Clear();

            var rng = new System.Random(now.Year * 10000 + now.Month * 100 + now.Day);
            var order = new List<int> { 0, 1, 2, 3 };
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            for (int k = 0; k < 3; k++)
            {
                int type = order[k];
                int idx = rng.Next(3);
                data.missions.Add(new MissionProgress { missionId = $"{Types[type]}:{Targets[type][idx]}:{idx}", progress = 0, completed = false });
            }
        }

        // --- Lecture d'une mission --------------------------------------------------------------

        public static void Parse(string id, out string type, out int target, out int tier)
        {
            string[] parts = id.Split(':');
            type = parts[0];
            target = parts.Length > 1 ? int.Parse(parts[1]) : 1;
            tier = parts.Length > 2 ? int.Parse(parts[2]) : 0;
        }

        public static string Title(string id)
        {
            Parse(id, out string type, out int target, out _);
            switch (type)
            {
                case "play_claw": return $"Joue {target} parties à la pince";
                case "win_prizes": return $"Gagne {target} lots";
                case "push_coins": return $"Fais tomber {target} pièces au coin pusher";
                default: return target <= 1 ? "Gagne un lot rare ou mieux" : $"Gagne {target} lots rares ou mieux";
            }
        }

        public static long RewardCoins(string id) { Parse(id, out _, out _, out int tier); return 30 + 20 * tier; }
        public static long RewardXp(string id) { Parse(id, out _, out _, out int tier); return 25 + 25 * tier; }

        // --- Suivi de progression -----------------------------------------------------------------

        private void OnPlay(MachineBase machine)
        {
            if (machine.Data != null && machine.Data.category == MachineCategory.Claw) Report("play_claw", 1);
        }

        private void OnReward(MachineBase machine, RewardData reward)
        {
            Report("win_prizes", 1);
            if (reward.rarity >= Rarity.Rare) Report("win_rare", 1);
        }

        private void OnCoins(MachineBase machine, int amount) => Report("push_coins", amount);

        private void Report(string type, int amount)
        {
            EnsureToday();
            foreach (MissionProgress m in Missions)
            {
                if (m.completed) continue;
                Parse(m.missionId, out string t, out int target, out _);
                if (t != type) continue;

                m.progress += amount;
                if (m.progress < target) continue;

                m.progress = target;
                m.completed = true;
                long coins = RewardCoins(m.missionId), xp = RewardXp(m.missionId);
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.Add(CurrencyType.Coins, coins);
                    CurrencyManager.Instance.Add(CurrencyType.Experience, xp);
                }
                OnMissionCompleted?.Invoke(Title(m.missionId), coins, xp);
            }
        }
    }
}
