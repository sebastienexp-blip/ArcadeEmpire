using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using UnityEngine;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Interface minimale (IMGUI) : monnaies, niveau et barre d'XP, invite d'interaction, état de la machine,
    /// notifications (gain, niveau, mission). Provisoire : remplacée par un Canvas en phase de finition.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private MachineInteractor interactor;
        private string toast = "";
        private float toastUntil;
        private GUIStyle label, big, box;

        private void OnEnable() { MachineBase.AnyRewardWon += OnReward; AchievementManager.Unlocked += OnAchievement; }
        private void OnDisable()
        {
            MachineBase.AnyRewardWon -= OnReward;
            AchievementManager.Unlocked -= OnAchievement;
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.OnLevelUp -= OnLevelUp;
            if (MissionManager.Instance != null) MissionManager.Instance.OnMissionCompleted -= OnMission;
        }

        private void Start()
        {
            interactor = FindFirstObjectByType<MachineInteractor>();
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.OnLevelUp += OnLevelUp;
            if (MissionManager.Instance != null) MissionManager.Instance.OnMissionCompleted += OnMission;
        }

        private void Show(string text, float seconds = 3.5f)
        {
            toast = text;
            toastUntil = Time.time + seconds;
        }

        private void OnReward(MachineBase machine, RewardData reward) =>
            Show($"★ {reward.displayName} — {ClawMachine.RarityName(reward.rarity)} ★");

        private void OnLevelUp(int level, long coins, int tokens) =>
            Show($"NIVEAU {level} !   +{coins} pièces" + (tokens > 0 ? $"   +{tokens} jeton premium" : ""), 4.5f);

        private void OnAchievement(string title, long coins) =>
            Show($"Succès débloqué : {title}   +{coins} pièces", 5f);

        private void OnMission(string title, long coins, long xp) =>
            Show($"Mission accomplie : {title}   +{coins} pièces, +{xp} XP", 4.5f);

        private void OnGUI()
        {
            if (UIState.PanelOpen) return;
            float scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            Styles();

            // Monnaies et niveau
            var cm = CurrencyManager.Instance;
            var pm = ProgressionManager.Instance;
            string coins = cm != null ? cm.Get(CurrencyType.Coins).ToString() : "—";
            string tickets = cm != null ? cm.Get(CurrencyType.Tickets).ToString() : "—";
            GUI.Box(new Rect(12, 12, 360, 78), GUIContent.none, box);
            GUI.Label(new Rect(22, 14, 340, 30), $"Pièces {coins}     Tickets {tickets}", label);
            if (pm != null)
            {
                string xpText = pm.Level >= ProgressionManager.MaxLevel ? "MAX" : $"{pm.XpIntoLevel} / {pm.XpForNext} XP";
                GUI.Label(new Rect(22, 42, 340, 28), $"Niveau {pm.Level}     {xpText}", label);
                Color old = GUI.color;
                GUI.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
                GUI.DrawTexture(new Rect(22, 72, 336, 8), Texture2D.whiteTexture);
                GUI.color = new Color(0.2f, 0.9f, 1f);
                GUI.DrawTexture(new Rect(22, 72, 336 * pm.Progress01, 8), Texture2D.whiteTexture);
                GUI.color = old;
            }

            if (interactor != null)
            {
                if (interactor.Active != null)
                {
                    GUI.Box(new Rect(w * 0.5f - 420, 640, 840, 54), GUIContent.none, box);
                    GUI.Label(new Rect(w * 0.5f - 410, 646, 820, 44), interactor.Active.StatusText, big);
                    GUI.Label(new Rect(w * 0.5f - 410, 600, 820, 36), interactor.Active.ControlsHint, label);
                }
                else if (interactor.Nearest != null && interactor.Nearest.Data != null)
                {
                    var d = interactor.Nearest.Data;
                    GUI.Box(new Rect(w * 0.5f - 300, 640, 600, 54), GUIContent.none, box);
                    string prompt = interactor.Nearest.IsUnlocked
                        ? $"E : jouer — {d.displayName} ({d.costPerPlay} pièces)"
                        : $"{d.displayName} — niveau {d.unlockLevel} requis";
                    GUI.Label(new Rect(w * 0.5f - 290, 646, 580, 44), prompt, big);
                }
            }

            if (Time.time < toastUntil)
                GUI.Label(new Rect(w * 0.5f - 450, 110, 900, 60), toast, big);
        }

        private void Styles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            label.normal.textColor = new Color(0.85f, 0.95f, 1f);
            big = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = new Color(1f, 0.9f, 0.3f);
            box = new GUIStyle(GUI.skin.box);
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.04f, 0.02f, 0.1f, 0.75f));
            tex.Apply();
            box.normal.background = tex;
        }
    }
}
