using ArcadeEmpire.Core;
using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Interface de jeu (Canvas UGUI construit par le code) : carte du joueur avec anneau d'XP, monnaies avec icônes,
    /// boutons illustrés (Inventaire, Boutique, Construire, Options), invite d'interaction avec touche, bandeau de machine,
    /// notifications, et boutons de caméra (tourner, zoom, changer de vue) utilisables à la souris comme au doigt.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        /// <summary>Bouton maintenu : appelle l'action à chaque image tant qu'on appuie dessus (souris ou doigt).</summary>
        public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
        {
            public System.Action<float> OnHold;
            public System.Action OnClick;
            private bool down;
            public void OnPointerDown(PointerEventData e) { down = true; OnClick?.Invoke(); }
            public void OnPointerUp(PointerEventData e) => down = false;
            public void OnPointerExit(PointerEventData e) => down = false;
            private void Update() { if (down && OnHold != null) OnHold(Time.unscaledDeltaTime); }
            private void OnDisable() => down = false;
        }

        private static readonly bool Mobile = Application.isMobilePlatform;
        private MachineInteractor interactor;
        private string toast = "";
        private float toastUntil;

        private void OnEnable() { MachineBase.AnyRewardWon += OnReward; AchievementManager.Unlocked += OnAchievement; GameSettings.Changed += ApplySettings; }
        private void OnDisable()
        {
            MachineBase.AnyRewardWon -= OnReward;
            AchievementManager.Unlocked -= OnAchievement;
            GameSettings.Changed -= ApplySettings;
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.OnLevelUp -= OnLevelUp;
            if (MissionManager.Instance != null) MissionManager.Instance.OnMissionCompleted -= OnMission;
        }

        private void Start()
        {
            interactor = FindFirstObjectByType<MachineInteractor>();
            UiKit.EnsureEventSystem();
            BuildCanvas();
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.OnLevelUp += OnLevelUp;
            if (MissionManager.Instance != null) MissionManager.Instance.OnMissionCompleted += OnMission;
            ApplySettings();
        }

        private void Show(string text, float seconds = 3.5f)
        {
            toast = text;
            toastUntil = Time.time + seconds;
        }

        private void OnReward(MachineBase machine, RewardData reward) =>
            Show($"{reward.displayName} — {ClawMachine.RarityName(reward.rarity)}");

        private void OnLevelUp(int level, long coins, int tokens) =>
            Show($"NIVEAU {level} !   +{coins} pièces" + (tokens > 0 ? $"   +{tokens} jeton premium" : ""), 4.5f);

        private void OnAchievement(string title, long coins) =>
            Show($"Succès débloqué : {title}   +{coins} pièces", 5f);

        private void OnMission(string title, long coins, long xp) =>
            Show($"Mission accomplie : {title}   +{coins} pièces, +{xp} XP", 4.5f);

        // ------------------------------------------------------------------ construction
        private Canvas canvas;
        private GameObject root, menuButtons, cameraPad, statusBox, promptBox, toastBox, keysBox, hintBox;
        private Text coinsText, ticketsText, tokensText, levelNum, xpText, nameText, statusText, hintText, camHintText, promptText, promptKey, toastText;
        private RectTransform cameraPadRect;
        private Image xpRing, xpBar, toastIcon;

        private void BuildCanvas()
        {
            canvas = UiKit.NewCanvas(transform, "HudCanvas", 10);
            root = canvas.gameObject;
            Transform r = root.transform;

            // ---- carte du joueur (haut gauche)
            RectTransform card = Box(r, "PlayerCard", new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), new Vector2(330, 92), UiTheme.Panel);
            var badge = Image(card, "Badge", UiTheme.Circle, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(76, 76), new Color(0.12f, 0.12f, 0.3f));
            xpRing = Image(badge.transform, "Ring", UiTheme.Ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84), UiTheme.Cyan);
            xpRing.type = UnityEngine.UI.Image.Type.Filled; xpRing.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360; xpRing.fillOrigin = 2; xpRing.fillClockwise = true;
            Image(badge.transform, "Star", UiTheme.Icon("star"), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(26, 26), Color.white);
            levelNum = Text(badge.transform, "Lvl", 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, Color.white, true);
            nameText = TextAt(card, "Name", 20, TextAnchor.MiddleLeft, new Vector2(100, -10), new Vector2(220, 28), UiTheme.Text);
            nameText.text = "Directeur de la salle";
            var barBack = Box(card, "XpBack", new Vector2(0, 1), new Vector2(0, 1), new Vector2(100, -46), new Vector2(214, 16), new Color(0.04f, 0.04f, 0.14f, 1f));
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barBack, false);
            xpBar = fill.GetComponent<Image>(); xpBar.sprite = UiTheme.Rounded; xpBar.type = UnityEngine.UI.Image.Type.Sliced; xpBar.color = UiTheme.Cyan;
            var fr = fill.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(0f, 1f); fr.pivot = new Vector2(0f, 0.5f); fr.offsetMin = fr.offsetMax = Vector2.zero;
            xpText = TextAt(card, "Xp", 15, TextAnchor.MiddleLeft, new Vector2(100, -64), new Vector2(220, 22), UiTheme.TextDim);

            // ---- monnaies (haut, à droite de la carte)
            coinsText = Pill(r, "coin", 0, UiTheme.Gold);
            ticketsText = Pill(r, "ticket", 1, new Color(1f, 0.62f, 0.4f));
            tokensText = Pill(r, "gem", 2, UiTheme.Cyan);

            // ---- boutons du menu (haut droite)
            menuButtons = new GameObject("MenuButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            menuButtons.transform.SetParent(r, false);
            var mr = menuButtons.GetComponent<RectTransform>();
            mr.anchorMin = mr.anchorMax = mr.pivot = new Vector2(1, 1); mr.anchoredPosition = new Vector2(-12, -12); mr.sizeDelta = new Vector2(216, 280);
            var v = menuButtons.GetComponent<VerticalLayoutGroup>();
            v.spacing = 8f; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            UiKit.AddIconButton(mr, "bag", "Inventaire", 216f, 56f, () => UIState.Request("inventory"), Mobile ? null : "I");
            UiKit.AddIconButton(mr, "shop", "Boutique", 216f, 56f, () => UIState.Request("shop"), Mobile ? null : "B");
            UiKit.AddIconButton(mr, "hammer", "Construire", 216f, 56f, () => UIState.Request("build"), Mobile ? null : "C");
            UiKit.AddIconButton(mr, "gear", "Options", 216f, 56f, () => UIState.Request("options"), Mobile ? null : "Échap");

            // ---- caméra (milieu droit)
            BuildCameraPad(r);

            // ---- bandeau de la machine utilisée (bas)
            statusBox = Box(r, "Status", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(Mobile ? -60 : 0, 24), new Vector2(Mobile ? 640 : 820, Mobile ? 64 : 58), UiTheme.Panel).gameObject;
            statusText = Text(statusBox.transform, "StatusText", 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, UiTheme.Gold, true);
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap; statusText.verticalOverflow = VerticalWrapMode.Truncate;
            statusText.resizeTextForBestFit = true; statusText.resizeTextMinSize = 13; statusText.resizeTextMaxSize = 26;
            statusText.rectTransform.offsetMin = new Vector2(10, 2); statusText.rectTransform.offsetMax = new Vector2(-10, -2);
            hintBox = Box(r, "Hint", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(Mobile ? -60 : 0, Mobile ? 96 : 90), new Vector2(Mobile ? 640 : 920, 62), new Color(0.04f, 0.03f, 0.12f, 0.72f)).gameObject;
            hintText = TextAt(hintBox.transform, "H1", 19, TextAnchor.MiddleCenter, new Vector2(0, -4), new Vector2(Mobile ? 620 : 900, 28), UiTheme.Text, true);
            hintText.horizontalOverflow = HorizontalWrapMode.Wrap; hintText.resizeTextForBestFit = true; hintText.resizeTextMinSize = 12; hintText.resizeTextMaxSize = 19;
            camHintText = TextAt(hintBox.transform, "H2", 15, TextAnchor.MiddleCenter, new Vector2(0, -32), new Vector2(900, 24), UiTheme.TextDim, true);
            camHintText.text = "Clic droit + glisser : tourner la vue    Molette : zoom    V : changer de vue    Retour arrière : recentrer";

            // ---- invite d'interaction (bas)
            promptBox = Box(r, "Prompt", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(Mobile ? -60 : 0, 40), new Vector2(Mobile ? 620 : 720, 64), UiTheme.Panel).gameObject;
            var keycap = Box(promptBox.transform, "Key", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(44, 44), new Color(0.92f, 0.95f, 1f));
            promptKey = Text(keycap, "K", 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, new Color(0.1f, 0.12f, 0.25f), true);
            promptKey.text = "E";
            promptText = TextAt(promptBox.transform, "PromptText", 24, TextAnchor.MiddleLeft, new Vector2(72, -16), new Vector2(Mobile ? 540 : 630, 34), UiTheme.Gold);
            promptText.horizontalOverflow = HorizontalWrapMode.Wrap; promptText.resizeTextForBestFit = true; promptText.resizeTextMinSize = 14; promptText.resizeTextMaxSize = 24;

            // ---- notification (haut centre)
            toastBox = Box(r, "Toast", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(900, 60), new Color(0.12f, 0.1f, 0.3f, 0.95f)).gameObject;
            toastIcon = Image(toastBox.transform, "Icon", UiTheme.Icon("trophy"), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(44, 44), Color.white);
            toastText = Text(toastBox.transform, "ToastText", 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, UiTheme.Gold, true);

            // ---- aide permanente (bas gauche)
            keysBox = new GameObject("Keys", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            keysBox.transform.SetParent(r, false);
            var kr = keysBox.GetComponent<RectTransform>();
            kr.anchorMin = kr.anchorMax = kr.pivot = new Vector2(0, 0); kr.anchoredPosition = new Vector2(12, 10); kr.sizeDelta = new Vector2(760, 34);
            var kh = keysBox.GetComponent<HorizontalLayoutGroup>();
            kh.spacing = 6f; kh.childAlignment = TextAnchor.MiddleLeft; kh.childControlWidth = true; kh.childControlHeight = true; kh.childForceExpandWidth = false; kh.childForceExpandHeight = false;
            Hint(kr, "Z Q S D", "marcher"); Hint(kr, "Maj", "courir"); Hint(kr, "Clic droit", "caméra"); Hint(kr, "E", "machine"); Hint(kr, "M", "musique");
        }

        private static void Hint(Transform parent, string key, string label)
        {
            UiKit.AddKeyCap(parent, key);
            var t = UiKit.AddText(parent, label, 15, UiTheme.TextDim, label.Length * 9f + 14f, 30f);
            t.fontStyle = FontStyle.Bold;
        }

        private void BuildCameraPad(Transform r)
        {
            cameraPad = new GameObject("CameraPad", typeof(RectTransform), typeof(Image));
            cameraPad.transform.SetParent(r, false);
            UiKit.Round(cameraPad, new Color(0.05f, 0.04f, 0.14f, 0.7f));
            var pr = cameraPad.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(1, 0.5f); pr.anchoredPosition = new Vector2(-12, -70); pr.sizeDelta = new Vector2(206, 262);
            cameraPadRect = pr;
            Image(pr, "Cam", UiTheme.Icon("camera"), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-70, -16), new Vector2(30, 30), Color.white);
            var title = TextAt(pr, "Title", 16, TextAnchor.MiddleLeft, new Vector2(60, -6), new Vector2(130, 24), UiTheme.TextDim);
            title.text = "CAMÉRA";

            // croix de rotation
            PadHold(pr, "up", new Vector2(103, -78), t => Cam(c => c.Nudge(0f, -70f * t)));
            PadHold(pr, "down", new Vector2(103, -150), t => Cam(c => c.Nudge(0f, 70f * t)));
            PadHold(pr, "left", new Vector2(45, -114), t => Cam(c => c.Nudge(-100f * t, 0f)));
            PadHold(pr, "right", new Vector2(161, -114), t => Cam(c => c.Nudge(100f * t, 0f)));
            PadHold(pr, "refresh", new Vector2(103, -114), null, () => Cam(c => c.ResetView()));
            // zoom, vue
            PadHold(pr, "minus", new Vector2(40, -218), null, () => Cam(c => c.Zoom(-1f)));
            PadHold(pr, "plus", new Vector2(103, -218), null, () => Cam(c => c.Zoom(1f)));
            PadHold(pr, "eye", new Vector2(166, -218), null, () => Cam(c => c.CycleView()));
        }

        private static void Cam(System.Action<ThirdPersonCamera> a)
        {
            if (ThirdPersonCamera.Instance != null) a(ThirdPersonCamera.Instance);
        }

        private void PadHold(RectTransform parent, string icon, Vector2 pos, System.Action<float> hold, System.Action click = null)
        {
            var go = new GameObject("Pad_" + icon, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            UiKit.Round(go, new Color(0.2f, 0.26f, 0.55f, 0.95f));
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0.5f, 0.5f); r.anchoredPosition = pos; r.sizeDelta = new Vector2(54, 54);
            var ic = new GameObject("I", typeof(RectTransform), typeof(Image));
            ic.transform.SetParent(go.transform, false);
            var img = ic.GetComponent<Image>(); img.sprite = UiTheme.Icon(icon); img.raycastTarget = false; img.preserveAspect = true;
            var ir = ic.GetComponent<RectTransform>(); ir.anchorMin = new Vector2(0.18f, 0.18f); ir.anchorMax = new Vector2(0.82f, 0.82f); ir.offsetMin = ir.offsetMax = Vector2.zero;
            var hb = go.AddComponent<HoldButton>();
            hb.OnHold = hold; hb.OnClick = click;
        }

        private void ApplySettings()
        {
            if (keysBox != null) keysBox.SetActive(GameSettings.ShowKeyHints);
        }

        // ------------------------------------------------------------------ petits constructeurs
        private static readonly Color PanelFallback = new Color(0.07f, 0.06f, 0.16f, 0.9f);

        private static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            UiKit.Round(go, color);
            var r = go.GetComponent<RectTransform>();
            bool bottom = anchor.y < 0.5f;
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = (anchor.x == 0.5f) ? new Vector2(0.5f, anchor.y) : pivot;
            r.anchoredPosition = pos; r.sizeDelta = size;
            if (!bottom && anchor.x == 0.5f) r.pivot = new Vector2(0.5f, 1f);
            return r;
        }

        private static Image Image(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = false; img.preserveAspect = true;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos; r.sizeDelta = size;
            return img;
        }

        private static Text Text(Transform parent, string name, int size, TextAnchor align, Vector2 pos, Vector2 sizeDelta, Color color, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = UiKit.Font; t.fontSize = size; t.alignment = align; t.color = color; t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            var r = go.GetComponent<RectTransform>();
            if (stretch) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
            else { r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = pos; r.sizeDelta = sizeDelta; }
            return t;
        }

        private static Text TextAt(Transform parent, string name, int size, TextAnchor align, Vector2 pos, Vector2 sizeDelta, Color color, bool centerAnchor = false)
        {
            Text t = Text(parent, name, size, align, pos, sizeDelta, color, false);
            if (centerAnchor)
            {
                var r = t.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
            }
            return t;
        }

        private Text Pill(Transform r, string icon, int index, Color tint)
        {
            RectTransform pill = Box(r, "Pill_" + icon, new Vector2(0, 1), new Vector2(0, 1), new Vector2(354f + index * 176f, -12), new Vector2(168, 44), new Color(0.05f, 0.04f, 0.14f, 0.97f));
            Image(pill, "Icon", UiTheme.Icon(icon), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(34, 34), Color.white);
            Text t = Text(pill, "Value", 22, TextAnchor.MiddleRight, Vector2.zero, Vector2.zero, tint, true);
            t.rectTransform.offsetMin = new Vector2(44, 0); t.rectTransform.offsetMax = new Vector2(-12, 0);
            return t;
        }

        // ------------------------------------------------------------------ mise à jour
        private static string Compact(long v)
        {
            if (v >= 10_000_000) return (v / 1_000_000f).ToString("0.#") + " M";
            if (v >= 100_000) return (v / 1000f).ToString("0.#") + " k";
            return v.ToString("N0");
        }

        private void LateUpdate()
        {
            if (canvas == null) return;
            bool visible = !UIState.PanelOpen;
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;

            var cm = CurrencyManager.Instance;
            var pm = ProgressionManager.Instance;
            coinsText.text = cm != null ? Compact(cm.Get(CurrencyType.Coins)) : "—";
            ticketsText.text = cm != null ? Compact(cm.Get(CurrencyType.Tickets)) : "—";
            tokensText.text = cm != null ? Compact(cm.Get(CurrencyType.PremiumTokens)) : "—";
            if (pm != null)
            {
                levelNum.text = pm.Level.ToString();
                xpText.text = pm.Level >= ProgressionManager.MaxLevel ? "Niveau maximum" : $"{pm.XpIntoLevel} / {pm.XpForNext} XP";
                xpRing.fillAmount = pm.Progress01;
                xpBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(pm.Progress01), 1f);
            }

            bool active = interactor != null && interactor.Active != null;
            bool near = interactor != null && !active && interactor.Nearest != null && interactor.Nearest.Data != null;
            bool pad = GameSettings.ShowCameraPad;
            menuButtons.SetActive(!active);
            cameraPad.SetActive(pad);
            keysBox.SetActive(!Mobile && !active && GameSettings.ShowKeyHints);
            if (cameraPadRect != null)
            {
                cameraPadRect.anchorMin = cameraPadRect.anchorMax = cameraPadRect.pivot = new Vector2(1, 1);
                cameraPadRect.anchoredPosition = active ? new Vector2(-12, -12) : new Vector2(-12, -276);
                cameraPadRect.localScale = Vector3.one * (active ? 1f : 0.75f);
            }
            statusBox.SetActive(active);
            hintBox.SetActive(active);
            camHintText.gameObject.SetActive(!Mobile && GameSettings.ShowKeyHints);
            promptBox.SetActive(near);
            if (active)
            {
                statusText.text = interactor.Active.StatusText;
                hintText.text = Mobile ? "Flèches : choisir la fente    ACTION : insérer    R : gains    X : quitter" : interactor.Active.ControlsHint;
            }
            else if (near)
            {
                var d = interactor.Nearest.Data;
                bool unlocked = interactor.Nearest.IsUnlocked;
                promptKey.text = unlocked ? "E" : "×";
                promptText.text = unlocked ? $"Jouer — {d.displayName}   ({d.costPerPlay} pièce{(d.costPerPlay > 1 ? "s" : "")})" : $"{d.displayName} — niveau {d.unlockLevel} requis";
            }
            bool showToast = Time.time < toastUntil;
            toastBox.SetActive(showToast);
            if (showToast) toastText.text = toast;
        }
    }
}
