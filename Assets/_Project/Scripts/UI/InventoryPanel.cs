using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using ArcadeEmpire.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Panneau d'inventaire et de collection (touche I) : lots possédés par rareté, progression de la collection,
    /// revente contre des pièces, sauvegarde manuelle. Interface Canvas (UGUI).
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        private bool open;
        private string message = "";
        private MachineInteractor interactor;
        private PlayerController player;

        private Canvas canvas;
        private Text infoText, messageText;
        private RectTransform content;

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
            canvas = UiKit.NewCanvas(transform, "InventoryCanvas", 20);
            var win = UiKit.Window(canvas.transform, new Vector2(800, 640));
            UiKit.AddText(win, "INVENTAIRE ET COLLECTION", 22, UiKit.Gold, -1f, 32f, TextAnchor.MiddleLeft, true);
            infoText = UiKit.AddText(win, "", 17, UiKit.Light, -1f, 28f);
            content = UiKit.AddScroll(win, 450f);
            messageText = UiKit.AddText(win, "", 15, UiKit.Dim, -1f, 24f);
            var bottom = UiKit.AddRow(win, 38f);
            UiKit.AddButton(bottom, "Sauvegarder", 160f, 36f, () =>
            {
                bool ok = SaveManager.Instance != null && SaveManager.Instance.SaveNow();
                message = ok ? "Partie sauvegardée." : "Sauvegarde impossible.";
            });
            UiKit.AddButton(bottom, "Fermer (I / Échap)", 190f, 36f, () => SetOpen(false));
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            bool inMachine = interactor != null && interactor.Active != null;

            if (!open && kb.iKey.wasPressedThisFrame && !inMachine && !UIState.PanelOpen) SetOpen(true);
            else if (open && (kb.iKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) SetOpen(false);
            if (open) RefreshHeader();
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

        private void RefreshHeader()
        {
            InventoryManager inv = InventoryManager.Instance;
            var cm = CurrencyManager.Instance;
            if (inv == null || inv.Catalog == null) { infoText.text = "Inventaire indisponible (lance le jeu depuis la scène Boot)."; return; }
            infoText.text = $"Pièces : {(cm != null ? cm.Get(CurrencyType.Coins) : 0)}   Tickets : {(cm != null ? cm.Get(CurrencyType.Tickets) : 0)}" +
                            $"   Collection : {inv.DistinctOwned} / {inv.Catalog.rewards.Count}   Objets : {inv.TotalItems}";
            messageText.text = message;
        }

        private void Rebuild()
        {
            UiKit.Clear(content);
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null || inv.Catalog == null) { RefreshHeader(); return; }
            for (int r = 3; r >= 0; r--)
            {
                var rarity = (Rarity)r;
                UiKit.AddText(content, $"— {ClawMachine.RarityName(rarity)} —", 22, UiKit.Gold, -1f, 34f, TextAnchor.MiddleLeft, true);
                foreach (RewardData reward in inv.Catalog.rewards)
                {
                    if (reward == null || reward.rarity != rarity) continue;
                    int count = inv.Count(reward.id);
                    RewardData rw = reward;
                    var row = UiKit.AddRow(content, 32f);
                    UiKit.AddText(row, count > 0 ? reward.displayName : "???", 18, UiKit.Light, 270f);
                    UiKit.AddText(row, $"x{count}", 18, UiKit.Light, 60f);
                    UiKit.AddText(row, $"{reward.SellValue} pièces / unité", 14, UiKit.Dim, 160f);
                    UiKit.AddButton(row, "Vendre 1", 90f, 28f, () => { Sell(inv, rw, 1); Rebuild(); }, count > 0);
                    UiKit.AddButton(row, "Tout vendre", 100f, 28f, () => { Sell(inv, rw, inv.Count(rw.id)); Rebuild(); }, count > 0);
                }
            }
            RefreshHeader();
        }

        private void Sell(InventoryManager inv, RewardData reward, int amount)
        {
            if (inv.Sell(reward, amount))
                message = $"Vendu {amount} x {reward.displayName} : +{reward.SellValue * amount} pièces.";
        }
    }
}
