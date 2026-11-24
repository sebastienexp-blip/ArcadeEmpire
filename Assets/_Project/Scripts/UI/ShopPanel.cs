using System.Collections.Generic;
using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Vitrine des lots (comptoir de la salle, touche B) : grandes cartes illustrées par la vraie vignette 3D du lot.
    /// Onglets : Lots (tickets), Meubles (pour la maison), Cadenas (gros lots : tickets + clés dorées), Améliorations, Missions.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        private static readonly string[] TabNames = { "Lots", "Meubles", "Cadenas", "Améliorations", "Missions" };
        private static readonly string[] TabIcons = { "star", "home", "key", "hammer", "mission" };
        private static readonly Color Accent = new Color(1f, 0.35f, 0.65f);

        private bool open;
        private int tab;
        private string message = "";
        private MachineInteractor interactor;
        private PlayerController player;

        private Canvas canvas;
        private Text currencies, messageText, subtitle;
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
            var win = UiCards.Screen(canvas.transform, "shop", "VITRINE DES LOTS", Accent, () => SetOpen(false), out currencies);
            tabButtons = UiCards.Tabs(win, TabNames, TabIcons, Accent, i => { tab = i; message = ""; Rebuild(); });
            subtitle = UiKit.AddText(win, "", 16, UiKit.Dim, -1f, 22f);
            content = UiKit.AddScroll(win, 380f);
            var le = content.parent.GetComponent<LayoutElement>(); le.flexibleHeight = 1f; le.minHeight = 200f;
            messageText = UiKit.AddText(win, "", 17, UiKit.Gold, -1f, 26f, TextAnchor.MiddleCenter, true);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            bool inMachine = interactor != null && interactor.Active != null;
            bool b = kb != null && kb.bKey.wasPressedThisFrame;
            bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
            if (!open && (b || UIState.Consume("shop")) && !inMachine && !UIState.PanelOpen) SetOpen(true);
            else if (open && (b || esc)) SetOpen(false);
            if (open) { currencies.text = UiCards.CurrencyLine(); messageText.text = message; }
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

        private void Rebuild()
        {
            UiKit.Clear(content);
            UiCards.HighlightTab(tabButtons, tab, Accent);
            switch (tab)
            {
                case 0: subtitle.text = "Peluches, idoles, confiseries, figurines… achetées avec les tickets gagnés aux machines."; DrawGrid(r => !r.furniture && r.keyPrice == 0); break;
                case 1: subtitle.text = "Meubles et équipements pour ta maison : ils s'installent automatiquement chez toi (téléporteur au fond de la salle)."; DrawGrid(r => r.furniture); break;
                case 2: subtitle.text = "Gros lots sous cadenas : il faut des clés dorées (balles du coin pusher, lots épiques, chance à chaque gain)."; DrawGrid(r => !r.furniture && r.keyPrice > 0); break;
                case 3: subtitle.text = "Améliorations payées en pièces."; DrawUpgrades(); break;
                default: subtitle.text = "Missions du jour et succès."; DrawMissions(); break;
            }
        }

        private void DrawGrid(System.Predicate<RewardData> filter)
        {
            var inv = InventoryManager.Instance;
            if (inv == null || inv.Catalog == null) { UiKit.AddText(content, "Vitrine indisponible (lance le jeu depuis la scène Boot).", 18, UiKit.Light); return; }
            int level = ProgressionManager.Instance != null ? ProgressionManager.Instance.Level : 1;
            var cm = CurrencyManager.Instance;
            long tickets = cm != null ? cm.Get(CurrencyType.Tickets) : 0, keys = cm != null ? cm.Get(CurrencyType.Keys) : 0;

            var list = new List<RewardData>();
            foreach (var r in inv.Catalog.rewards) if (ShopManager.IsForSale(r) && filter(r)) list.Add(r);
            list.Sort((a, b) => a.shopTicketPrice != b.shopTicketPrice ? a.shopTicketPrice.CompareTo(b.shopTicketPrice) : string.CompareOrdinal(a.displayName, b.displayName));
            if (list.Count == 0) { UiKit.AddText(content, "Rien ici pour le moment.", 18, UiKit.Light); return; }

            var grid = UiCards.Grid(content);
            UiCards.FitColumns(grid, ((RectTransform)canvas.transform).rect.width * 0.92f - 70f);
            foreach (var r in list)
            {
                int owned = inv.Count(r.id);
                string price = $"{r.shopTicketPrice} tickets" + (r.keyPrice > 0 ? $"  +  {r.keyPrice} clé{(r.keyPrice > 1 ? "s" : "")}" : "");
                RewardData rw = r;
                if (r.furniture && owned > 0) { UiCards.PrizeCard(grid, r, true, "Installé dans la maison", price, "✔ Possédé", false, null); continue; }
                bool levelOk = level >= r.ShopMinLevel;
                bool afford = tickets >= r.shopTicketPrice && keys >= r.keyPrice;
                string info = !levelOk ? $"Niveau {r.ShopMinLevel} requis" : owned > 0 ? $"Possédé x{owned}" : (r.furniture ? "Meuble" : "Nouveau !");
                UiCards.PrizeCard(grid, r, true, info, price, r.keyPrice > 0 ? "Ouvrir le cadenas" : "Acheter", levelOk && afford, () => { Buy(rw); Rebuild(); },
                    r.keyPrice > 0 ? "CADENAS" : null);
            }
        }

        private void Buy(RewardData r)
        {
            switch (ShopManager.Buy(r))
            {
                case ShopManager.Result.Ok: message = r.furniture ? $"{r.displayName} livré à la maison !" : $"Acheté : {r.displayName} — ajouté à ta collection."; ArcadeEmpire.Core.Sfx.Play(ArcadeEmpire.Core.Sfx.Kind.Win, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.5f); break;
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

            UiKit.AddText(content, "Pinces — force de prise", 22, UiKit.Gold, -1f, 32f, TextAnchor.MiddleLeft, true);
            UiKit.AddText(content, $"Niveau {lvl} / {UpgradeManager.MaxLevel}   (bonus actuel : +{lvl * 10} % de réussite de prise, moins de glissades)", 18, UiKit.Light);
            if (cost < 0) UiKit.AddText(content, "Niveau maximum atteint.", 14, UiKit.Dim);
            else
            {
                var row = UiKit.AddRow(content, 44f);
                UiKit.AddButton(row, $"Améliorer — {cost} pièces", 300f, 42f,
                    () => { message = UpgradeManager.TryUpgrade(clawId) ? "Pince améliorée !" : "Amélioration impossible."; Rebuild(); }, coins >= cost);
            }
        }

        private void DrawMissions()
        {
            var mm = MissionManager.Instance;
            if (mm == null) { UiKit.AddText(content, "Missions indisponibles.", 18, UiKit.Light); return; }
            UiKit.AddText(content, "Missions du jour (renouvelées chaque jour à minuit UTC)", 15, UiKit.Dim);
            foreach (var m in mm.Missions)
            {
                MissionManager.Parse(m.missionId, out _, out int target, out _);
                var row = UiKit.AddRow(content, 40f);
                UiKit.Round(row.gameObject, new Color(0.1f, 0.09f, 0.22f, 1f));
                row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 4, 4);
                UiKit.AddIcon(row, m.completed ? "check" : "mission", 28f);
                UiKit.AddText(row, MissionManager.Title(m.missionId), 18, UiKit.Light, 420f);
                UiKit.AddText(row, $"{Mathf.Min(m.progress, target)} / {target}", 18, m.completed ? UiTheme.Green : UiKit.Light, 90f);
                UiKit.AddText(row, $"+{MissionManager.RewardCoins(m.missionId)} pièces, +{MissionManager.RewardXp(m.missionId)} XP", 15, UiKit.Dim, 230f);
            }

            var am = AchievementManager.Instance;
            if (am == null) return;
            UiKit.AddText(content, "Succès", 22, UiKit.Gold, -1f, 36f, TextAnchor.MiddleLeft, true);
            foreach (var a in AchievementManager.All)
            {
                bool done = am.IsUnlocked(a.id);
                var row = UiKit.AddRow(content, 36f);
                UiKit.AddIcon(row, done ? "trophy" : "star", 26f, done ? Color.white : new Color(1f, 1f, 1f, 0.3f));
                UiKit.AddText(row, a.title, 18, UiKit.Light, 230f);
                UiKit.AddText(row, a.desc, 15, UiKit.Dim, 300f);
                UiKit.AddText(row, done ? "obtenu" : $"{Mathf.Min(am.Get(a.stat), a.target)} / {a.target}", 17, UiKit.Light, 90f);
                UiKit.AddText(row, $"+{a.reward} pièces", 15, UiKit.Dim, 110f);
            }
        }
    }
}
