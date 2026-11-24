using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Boutique, améliorations et missions du jour (touche B). Interface Canvas (UGUI).
    /// Onglets : Boutique (lots contre tickets), Améliorations (pièces), Missions (suivi en direct).
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        private static readonly string[] Tabs = { "Boutique", "Améliorations", "Missions" };

        private bool open;
        private int tab;
        private string message = "";
        private MachineInteractor interactor;
        private PlayerController player;

        private Canvas canvas;
        private Text infoText, messageText;
        private RectTransform content;
        private Button[] tabButtons;

        private void Start()
        {
            interactor = FindFirstObjectByType<MachineInteractor>();
            player = FindFirstObjectByType<PlayerController>();
            BuildUi();
            canvas.gameObject.SetActive(false);
        }

        private void BuildUi()
        {
            UiKit.EnsureEventSystem();
            canvas = UiKit.NewCanvas(transform, "ShopCanvas", 20);
            var win = UiKit.Window(canvas.transform, new Vector2(800, 640));
            UiKit.AddText(win, "BOUTIQUE, AMÉLIORATIONS ET MISSIONS", 22, UiKit.Gold, -1f, 32f, TextAnchor.MiddleLeft, true);
            infoText = UiKit.AddText(win, "", 17, UiKit.Light, -1f, 28f);
            var tabs = UiKit.AddRow(win, 36f);
            tabButtons = new Button[Tabs.Length];
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                tabButtons[i] = UiKit.AddButton(tabs, Tabs[i], 160f, 34f, () => { tab = index; message = ""; Rebuild(); });
            }
            content = UiKit.AddScroll(win, 400f);
            messageText = UiKit.AddText(win, "", 15, UiKit.Dim, -1f, 24f);
            var bottom = UiKit.AddRow(win, 38f);
            UiKit.AddButton(bottom, "Fermer (B / Échap)", 210f, 36f, () => SetOpen(false));
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) { if (!open && UIState.Consume("shop") && !UIState.PanelOpen) SetOpen(true); return; }
            bool inMachine = interactor != null && interactor.Active != null;

            if (!open && (kb.bKey.wasPressedThisFrame || UIState.Consume("shop")) && !inMachine && !UIState.PanelOpen) SetOpen(true);
            else if (open && (kb.bKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) SetOpen(false);
            if (open) RefreshHeader();
        }

        private void SetOpen(bool value)
        {
            open = value;
            UIState.PanelOpen = value;
            message = "";
            if (value && MissionManager.Instance != null) MissionManager.Instance.EnsureToday();
            if (player != null) player.ControlEnabled = !value;
            canvas.gameObject.SetActive(value);
            if (value) Rebuild();
        }

        private void RefreshHeader()
        {
            var cm = CurrencyManager.Instance;
            var pm = ProgressionManager.Instance;
            infoText.text = $"Niveau {(pm != null ? pm.Level : 1)}     Pièces : {(cm != null ? cm.Get(CurrencyType.Coins) : 0)}" +
                            $"     Tickets : {(cm != null ? cm.Get(CurrencyType.Tickets) : 0)}     Jetons premium : {(cm != null ? cm.Get(CurrencyType.PremiumTokens) : 0)}";
            messageText.text = message;
        }

        private void Rebuild()
        {
            UiKit.Clear(content);
            if (tab == 0) DrawShop();
            else if (tab == 1) DrawUpgrades();
            else DrawMissions();
            RefreshHeader();
        }

        private void DrawShop()
        {
            var inv = InventoryManager.Instance;
            if (inv == null || inv.Catalog == null) { UiKit.AddText(content, "Boutique indisponible.", 18, UiKit.Light); return; }
            int level = ProgressionManager.Instance != null ? ProgressionManager.Instance.Level : 1;
            long tickets = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Tickets) : 0;

            foreach (RewardData r in inv.Catalog.rewards)
            {
                if (!ShopManager.IsForSale(r)) continue;
                var row = UiKit.AddRow(content);
                UiKit.AddText(row, r.displayName, 18, UiKit.Light, 250f);
                UiKit.AddText(row, ClawMachine.RarityName(r.rarity), 14, UiKit.Dim, 100f);
                UiKit.AddText(row, $"{r.shopTicketPrice} tickets", 18, UiKit.Light, 120f);
                if (level < r.ShopMinLevel) UiKit.AddText(row, $"Niveau {r.ShopMinLevel} requis", 14, UiKit.Dim, 140f);
                else
                {
                    RewardData reward = r;
                    UiKit.AddButton(row, "Acheter", 100f, 30f, () => { Buy(reward); Rebuild(); }, tickets >= r.shopTicketPrice);
                }
            }
            UiKit.AddText(content, "Les tickets se gagnent en remportant des lots aux machines.", 14, UiKit.Dim);
        }

        private void Buy(RewardData r)
        {
            switch (ShopManager.Buy(r))
            {
                case ShopManager.Result.Ok: message = $"Acheté : {r.displayName} (ajouté à l'inventaire)."; break;
                case ShopManager.Result.LevelTooLow: message = "Niveau insuffisant."; break;
                case ShopManager.Result.NotEnoughTickets: message = "Pas assez de tickets."; break;
                case ShopManager.Result.NotEnoughKeys: message = "Il faut plus de clés dorées pour ouvrir ce cadenas."; break;
                default: message = "Achat impossible."; break;
            }
        }

        private void DrawUpgrades()
        {
            const string clawId = "claw_classic";
            int lvl = UpgradeManager.GetLevel(clawId);
            long cost = UpgradeManager.NextCost(clawId);
            long coins = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Coins) : 0;

            UiKit.AddText(content, "Pince classique — force de prise", 22, UiKit.Gold, -1f, 32f, TextAnchor.MiddleLeft, true);
            UiKit.AddText(content, $"Niveau {lvl} / {UpgradeManager.MaxLevel}   (bonus actuel : +{lvl * 10} % de réussite de prise, moins de glissades)", 18, UiKit.Light);
            if (cost < 0) UiKit.AddText(content, "Niveau maximum atteint.", 14, UiKit.Dim);
            else
            {
                var row = UiKit.AddRow(content, 38f);
                UiKit.AddButton(row, $"Améliorer — {cost} pièces", 260f, 36f,
                    () => { message = UpgradeManager.TryUpgrade(clawId) ? "Pince améliorée !" : "Amélioration impossible."; Rebuild(); }, coins >= cost);
            }
        }

        private void DrawMissions()
        {
            var mm = MissionManager.Instance;
            if (mm == null) { UiKit.AddText(content, "Missions indisponibles.", 18, UiKit.Light); return; }
            UiKit.AddText(content, "Missions du jour (renouvelées chaque jour à minuit UTC)", 14, UiKit.Dim);
            foreach (var m in mm.Missions)
            {
                MissionManager.Parse(m.missionId, out _, out int target, out _);
                var row = UiKit.AddRow(content, 30f);
                UiKit.AddText(row, (m.completed ? "✔ " : "• ") + MissionManager.Title(m.missionId), 18, UiKit.Light, 420f);
                UiKit.AddText(row, $"{Mathf.Min(m.progress, target)} / {target}", 18, UiKit.Light, 90f);
                UiKit.AddText(row, $"+{MissionManager.RewardCoins(m.missionId)} pièces, +{MissionManager.RewardXp(m.missionId)} XP", 14, UiKit.Dim, 200f);
            }

            var am = AchievementManager.Instance;
            if (am == null) return;
            UiKit.AddText(content, "Succès", 22, UiKit.Gold, -1f, 36f, TextAnchor.MiddleLeft, true);
            foreach (var a in AchievementManager.All)
            {
                bool done = am.IsUnlocked(a.id);
                var row = UiKit.AddRow(content, 30f);
                UiKit.AddText(row, (done ? "✔ " : "• ") + a.title, 18, UiKit.Light, 230f);
                UiKit.AddText(row, a.desc, 14, UiKit.Dim, 290f);
                UiKit.AddText(row, done ? "obtenu" : $"{Mathf.Min(am.Get(a.stat), a.target)} / {a.target}", 17, UiKit.Light, 90f);
                UiKit.AddText(row, $"+{a.reward} pièces", 14, UiKit.Dim, 100f);
            }
        }
    }
}
