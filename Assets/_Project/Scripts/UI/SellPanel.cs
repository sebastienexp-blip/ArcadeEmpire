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
    /// Comptoir de rachat (petite boutique de la salle) : on revend ses lots contre des pièces.
    /// Cartes illustrées, « Vendre 1 », « Garder 1 » (vend les doubles), et un bouton global pour vendre tous les doublons.
    /// Les meubles ne se revendent pas (ils sont installés dans la maison).
    /// </summary>
    public class SellPanel : MonoBehaviour
    {
        private static readonly Color Accent = new Color(0.3f, 0.88f, 0.55f);
        private bool open;
        private string message = "";
        private PlayerController player;
        private Canvas canvas;
        private Text currencies, messageText, subtitle;
        private RectTransform content;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            UiKit.EnsureEventSystem();
            canvas = UiKit.NewCanvas(transform, "SellCanvas", 20);
            var win = UiCards.Screen(canvas.transform, "sell", "RACHAT DE LOTS", Accent, () => SetOpen(false), out currencies);
            var row = UiKit.AddRow(win, 48f);
            subtitle = UiKit.AddText(row, "", 16, UiKit.Dim, -1f, 44f);
            var all = UiKit.AddButton(row, "Vendre tous les doublons", 280f, 44f, SellAllDuplicates);
            var c = all.colors; c.normalColor = new Color(0.2f, 0.55f, 0.38f); c.selectedColor = c.normalColor; all.colors = c;
            content = UiKit.AddScroll(win, 380f);
            var le = content.parent.GetComponent<LayoutElement>(); le.flexibleHeight = 1f; le.minHeight = 200f;
            messageText = UiKit.AddText(win, "", 17, UiKit.Gold, -1f, 26f, TextAnchor.MiddleCenter, true);
            canvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
            if (!open && UIState.Consume("sell") && !UIState.PanelOpen) SetOpen(true);
            else if (open && esc) SetOpen(false);
            if (open && UIState.CloseRequested) SetOpen(false);
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
            var inv = InventoryManager.Instance;
            if (inv == null || inv.Catalog == null) { UiKit.AddText(content, "Comptoir indisponible.", 18, UiKit.Light); return; }
            var list = new List<RewardData>();
            long total = 0;
            foreach (var r in inv.Catalog.rewards)
                if (r != null && !r.furniture && inv.Count(r.id) > 0) { list.Add(r); total += r.SellValue * inv.Count(r.id); }
            list.Sort((a, b) => b.rarity != a.rarity ? b.rarity.CompareTo(a.rarity) : string.CompareOrdinal(a.displayName, b.displayName));
            subtitle.text = list.Count == 0 ? "Tu n'as aucun lot à revendre. Va gagner des peluches !" :
                $"{list.Count} lots différents — valeur totale de rachat : {total:N0} pièces. Commun 3 · Rare 12 · Épique 40 · Légendaire 150 pièces.";
            if (list.Count == 0) return;
            var grid = UiCards.Grid(content, 186f, 280f);
            UiCards.FitColumns(grid, ((RectTransform)canvas.transform).rect.width * 0.92f - 70f);
            foreach (var r in list)
            {
                int n = inv.Count(r.id);
                RewardData rw = r;
                var card = UiCards.PrizeCard(grid, r, true, $"Possédé x{n}", $"{r.SellValue} pièces / unité", "Vendre 1", true, () => { Sell(rw, 1); Rebuild(); }, $"x{n}");
                var b = UiKit.AddButton(card, n > 1 ? $"Garder 1 (+{r.SellValue * (n - 1)})" : "Dernier exemplaire", 160f, 28f, () => { Sell(rw, inv.Count(rw.id) - 1); Rebuild(); }, n > 1);
                b.GetComponentInChildren<Text>().fontSize = 14;
            }
        }

        private void Sell(RewardData r, int amount)
        {
            var inv = InventoryManager.Instance;
            if (inv == null || amount <= 0) return;
            if (inv.Sell(r, amount)) message = $"Vendu {amount} x {r.displayName} : +{r.SellValue * amount} pièces.";
        }

        private void SellAllDuplicates()
        {
            var inv = InventoryManager.Instance;
            if (inv == null || inv.Catalog == null) return;
            long gain = 0; int items = 0;
            foreach (var r in inv.Catalog.rewards)
            {
                if (r == null || r.furniture) continue;
                int extra = inv.Count(r.id) - 1;
                if (extra > 0 && inv.Sell(r, extra)) { gain += r.SellValue * extra; items += extra; }
            }
            message = items > 0 ? $"{items} doublons vendus : +{gain:N0} pièces." : "Aucun doublon à vendre.";
            Rebuild();
        }
    }
}
