using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using ArcadeEmpire.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Panneau d'inventaire et de collection (touche I) : lots possédés par rareté, progression de la collection,
    /// revente contre des pièces, sauvegarde manuelle. Interface IMGUI provisoire (remplacée plus tard par un Canvas).
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        private bool open;
        private Vector2 scroll;
        private string message = "";
        private MachineInteractor interactor;
        private PlayerController player;
        private GUIStyle title, label, small;

        private void Start()
        {
            interactor = FindFirstObjectByType<MachineInteractor>();
            player = FindFirstObjectByType<PlayerController>();
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            bool inMachine = interactor != null && interactor.Active != null;

            if (!open && kb.iKey.wasPressedThisFrame && !inMachine && !UIState.PanelOpen) SetOpen(true);
            else if (open && (kb.iKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) SetOpen(false);
        }

        private void SetOpen(bool value)
        {
            open = value;
            UIState.PanelOpen = value;
            message = "";
            if (player != null) player.ControlEnabled = !value;
        }

        private void OnGUI()
        {
            float scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            Styles();

            if (!open)
            {
                if (interactor == null || interactor.Active == null)
                    GUI.Label(new Rect(12, 690, 420, 24), "I : inventaire     B : boutique et missions", small);
                return;
            }

            InventoryManager inv = InventoryManager.Instance;
            Rect box = new Rect(w * 0.5f - 400, 40, 800, 640);
            GUI.Box(box, GUIContent.none);
            GUILayout.BeginArea(new Rect(box.x + 20, box.y + 14, box.width - 40, box.height - 28));

            GUILayout.Label("INVENTAIRE ET COLLECTION", title);
            if (inv == null || inv.Catalog == null)
            {
                GUILayout.Label("Inventaire indisponible (lance le jeu depuis la scène Boot).", label);
                GUILayout.EndArea();
                return;
            }

            var cm = CurrencyManager.Instance;
            GUILayout.Label($"Pièces : {(cm != null ? cm.Get(CurrencyType.Coins) : 0)}     Tickets : {(cm != null ? cm.Get(CurrencyType.Tickets) : 0)}" +
                            $"     Collection : {inv.DistinctOwned} / {inv.Catalog.rewards.Count}     Objets : {inv.TotalItems}", label);

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(470));
            for (int r = 3; r >= 0; r--)
            {
                var rarity = (Rarity)r;
                GUILayout.Label($"— {ClawMachine.RarityName(rarity)} —", title);
                foreach (RewardData reward in inv.Catalog.rewards)
                {
                    if (reward == null || reward.rarity != rarity) continue;
                    int count = inv.Count(reward.id);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(count > 0 ? reward.displayName : "???", label, GUILayout.Width(280));
                    GUILayout.Label($"x{count}", label, GUILayout.Width(70));
                    GUILayout.Label($"{reward.SellValue} pièces / unité", small, GUILayout.Width(170));
                    GUI.enabled = count > 0;
                    if (GUILayout.Button("Vendre 1", GUILayout.Width(90))) Sell(inv, reward, 1);
                    if (GUILayout.Button("Tout vendre", GUILayout.Width(100))) Sell(inv, reward, count);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label(message, small);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sauvegarder", GUILayout.Width(160), GUILayout.Height(34)))
            {
                bool ok = SaveManager.Instance != null && SaveManager.Instance.SaveNow();
                message = ok ? "Partie sauvegardée." : "Sauvegarde impossible.";
            }
            if (GUILayout.Button("Fermer (I / Échap)", GUILayout.Width(180), GUILayout.Height(34))) SetOpen(false);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void Sell(InventoryManager inv, RewardData reward, int amount)
        {
            if (inv.Sell(reward, amount))
                message = $"Vendu {amount} x {reward.displayName} : +{reward.SellValue * amount} pièces.";
        }

        private void Styles()
        {
            if (label != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(1f, 0.9f, 0.3f);
            label = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            label.normal.textColor = new Color(0.9f, 0.95f, 1f);
            small = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            small.normal.textColor = new Color(0.7f, 0.75f, 0.9f);
        }
    }
}
