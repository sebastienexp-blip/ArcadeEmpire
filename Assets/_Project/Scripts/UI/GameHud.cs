using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Interface de jeu : monnaies, niveau et barre d'XP, invite d'interaction, état de la machine,
    /// notifications (gain, niveau, mission). Interface Canvas (UGUI) construite par le code au démarrage.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private MachineInteractor interactor;
        private string toast = "";
        private float toastUntil;

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
            BuildCanvas();
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

        // ---- Interface Canvas (créée par le code, sans prefab) ----
        private Canvas canvas;
        private Text keysText;
        private Text coinsText, levelText, statusText, hintText, promptText, toastText;
        private Image xpFill;
        private GameObject statusBox, promptBox, toastBox, root;
        private Font font;

        private void BuildCanvas()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("HudCanvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            root = go;

            // Panneau des monnaies et du niveau (haut gauche)
            var top = Panel("TopLeft", go.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), new Vector2(380, 84));
            coinsText = Label("Coins", top.transform, 20, TextAnchor.MiddleLeft, new Vector2(14, -6), new Vector2(352, 30), new Color(1f, 0.9f, 0.35f));
            levelText = Label("Level", top.transform, 18, TextAnchor.MiddleLeft, new Vector2(14, -36), new Vector2(352, 26), new Color(0.85f, 0.95f, 1f));
            var barBack = Panel("XpBack", top.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -68), new Vector2(352, 8), new Color(0.1f, 0.1f, 0.2f, 0.95f));
            var fill = new GameObject("XpFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barBack.transform, false);
            xpFill = fill.GetComponent<Image>();
            xpFill.color = new Color(0.2f, 0.9f, 1f);
            var fr = fill.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(0f, 1f); fr.pivot = new Vector2(0f, 0.5f);
            fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero; fr.sizeDelta = new Vector2(0f, 0f);

            // Bandeau d'état de la machine utilisée (bas)
            statusBox = Panel("Status", go.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(860, 56), null, true);
            statusText = Label("StatusText", statusBox.transform, 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, new Color(1f, 0.9f, 0.3f), true);
            hintText = Label("Hint", go.transform, 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(860, 34), new Color(0.85f, 0.95f, 1f));
            var hr = hintText.rectTransform; hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0); hr.pivot = new Vector2(0.5f, 0); hr.anchoredPosition = new Vector2(0, 88);

            // Invite « E : jouer » (bas)
            promptBox = Panel("Prompt", go.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(660, 56), null, true);
            promptText = Label("PromptText", promptBox.transform, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, new Color(1f, 0.9f, 0.3f), true);

            keysText = Label("Keys", go.transform, 15, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(520, 24), new Color(0.7f, 0.75f, 0.9f));
            var kr = keysText.rectTransform; kr.anchorMin = kr.anchorMax = new Vector2(0, 0); kr.pivot = new Vector2(0, 0); kr.anchoredPosition = new Vector2(12, 8);
            keysText.text = "I : inventaire   B : boutique, missions et succès   C : construire   M : musique   - / + : volume";

            // Message temporaire (haut centre)
            toastBox = Panel("Toast", go.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(920, 56), null, true);
            toastText = Label("ToastText", toastBox.transform, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, new Color(1f, 0.9f, 0.3f), true);
        }

        private static readonly Color PanelColor = new Color(0.04f, 0.02f, 0.1f, 0.78f);

        private GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color? color = null, bool centered = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color ?? PanelColor;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = centered ? new Vector2(0.5f, anchor.y) : pivot;
            r.anchoredPosition = pos; r.sizeDelta = size;
            return go;
        }

        private Text Label(string name, Transform parent, int size, TextAnchor align, Vector2 pos, Vector2 sizeDelta, Color color, bool stretch = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = align; t.color = color; t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var r = go.GetComponent<RectTransform>();
            if (stretch) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
            else { r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = pos; r.sizeDelta = sizeDelta; }
            return t;
        }

        private void LateUpdate()
        {
            if (canvas == null) return;
            bool visible = !UIState.PanelOpen;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;

            var cm = CurrencyManager.Instance;
            var pm = ProgressionManager.Instance;
            string coins = cm != null ? cm.Get(CurrencyType.Coins).ToString() : "—";
            string tickets = cm != null ? cm.Get(CurrencyType.Tickets).ToString() : "—";
            coinsText.text = $"Pièces {coins}     Tickets {tickets}";
            if (pm != null)
            {
                string xpText = pm.Level >= ProgressionManager.MaxLevel ? "MAX" : $"{pm.XpIntoLevel} / {pm.XpForNext} XP";
                levelText.text = $"Niveau {pm.Level}     {xpText}";
                xpFill.rectTransform.sizeDelta = new Vector2(352f * pm.Progress01, 0f);
            }

            bool active = interactor != null && interactor.Active != null;
            bool near = interactor != null && !active && interactor.Nearest != null && interactor.Nearest.Data != null;
            keysText.gameObject.SetActive(!active);
            statusBox.SetActive(active);
            hintText.gameObject.SetActive(active);
            promptBox.SetActive(near);
            if (active)
            {
                statusText.text = interactor.Active.StatusText;
                hintText.text = interactor.Active.ControlsHint;
            }
            else if (near)
            {
                var d = interactor.Nearest.Data;
                promptText.text = interactor.Nearest.IsUnlocked
                    ? $"E : jouer — {d.displayName} ({d.costPerPlay} pièces)"
                    : $"{d.displayName} — niveau {d.unlockLevel} requis";
            }
            bool showToast = Time.time < toastUntil;
            toastBox.SetActive(showToast);
            if (showToast) toastText.text = toast;
        }
    }
}
