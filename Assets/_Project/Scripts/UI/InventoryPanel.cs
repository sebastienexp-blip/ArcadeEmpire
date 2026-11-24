using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Player;
using ArcadeEmpire.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Collection (touche I) : album de tous les lots du jeu en cartes illustrées (silhouette pour ceux pas encore gagnés),
    /// onglets par rareté et meubles de la maison. La revente se fait au comptoir de rachat de la salle.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        private static readonly string[] TabNames = { "Tout", "Légendaire", "Épique", "Rare", "Commun", "Maison" };
        private static readonly Color Accent = new Color(0.3f, 0.75f, 1f);
        private bool open;
        private int tab;
        private string message = "";
        private MachineInteractor interactor;
        private PlayerController player;

        private Canvas canvas;
        private Text currencies, infoText, messageText;
        private RectTransform content;
        private Button[] tabButtons;

        private void Start()
        {
            interactor = FindFirstObjectByType<MachineInteractor>();
            player = FindFirstObjectByType<PlayerController>();
            UiKit.EnsureEventSystem();
            canvas = UiKit.NewCanvas(transform, "InventoryCanvas", 20);
            var win = UiCards.Screen(canvas.transform, "bag", "MA COLLECTION", Accent, () => SetOpen(false), out currencies);
            tabButtons = UiCards.Tabs(win, TabNames, null, Accent, i => { tab = i; Rebuild(); });
            foreach (var b in tabButtons) b.GetComponent<LayoutElement>().preferredWidth = 150f;
            var row = UiKit.AddRow(win, 44f);
            infoText = UiKit.AddText(row, "", 17, UiKit.Light, -1f, 40f);
            UiKit.AddButton(row, "Sauvegarder", 180f, 40f, () =>
            {
                bool ok = SaveManager.Instance != null && SaveManager.Instance.SaveNow();
                message = ok ? "Partie sauvegardée." : "Sauvegarde impossible.";
            });
            content = UiKit.AddScroll(win, 380f);
            var le = content.parent.GetComponent<LayoutElement>(); le.flexibleHeight = 1f; le.minHeight = 200f;
            messageText = UiKit.AddText(win, "", 16, UiKit.Gold, -1f, 24f, TextAnchor.MiddleCenter, true);
            canvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            bool inMachine = interactor != null && interactor.Active != null;
            bool i = kb != null && kb.iKey.wasPressedThisFrame;
            bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
            if (!open && (i || UIState.Consume("inventory")) && !inMachine && !UIState.PanelOpen) SetOpen(true);
            else if (open && (i || esc)) SetOpen(false);
            if (open) { currencies.text = UiCards.CurrencyLine(); messageText.text = message; }
        }

        private void SetOpen(bool value)
        {
            open = value;
            UIState.PanelOpen = value;
            message = "";
            if (player != null) player.ControlEnabled = !value;
            canvas.gameObject.SetActive(value);
            if (value) Rebuild();
        }

        private void Rebuild()
        {
            UiKit.Clear(content);
            UiCards.HighlightTab(tabButtons, tab, Accent);
            var inv = InventoryManager.Instance;
            if (inv == null || inv.Catalog == null) { infoText.text = "Collection indisponible (lance le jeu depuis la scène Boot)."; return; }
            int lots = 0, lotsOwned = 0, furn = 0, furnOwned = 0;
            foreach (var r in inv.Catalog.rewards)
            {
                if (r == null) continue;
                bool own = inv.Count(r.id) > 0;
                if (r.furniture) { furn++; if (own) furnOwned++; } else { lots++; if (own) lotsOwned++; }
            }
            infoText.text = $"Lots découverts : {lotsOwned} / {lots}     Meubles de la maison : {furnOwned} / {furn}     Objets : {inv.TotalItems}";

            var grid = UiCards.Grid(content, 170f, 230f);
            UiCards.FitColumns(grid, ((RectTransform)canvas.transform).rect.width * 0.92f - 70f);
            for (int rr = 3; rr >= 0; rr--)
                foreach (var r in inv.Catalog.rewards)
                {
                    if (r == null || (int)r.rarity != rr) continue;
                    if (tab == 5 ? !r.furniture : r.furniture) continue;
                    if (tab >= 1 && tab <= 4 && (int)r.rarity != 4 - tab) continue;
                    int n = inv.Count(r.id);
                    string info = n > 0 ? (r.furniture ? "Installé" : $"x{n}") : (r.furniture ? "Vitrine des lots" : "Pas encore gagné");
                    UiCards.PrizeCard(grid, r, n > 0, info, null, null, false, null, n > 1 ? $"x{n}" : null);
                }
        }
    }
}
