using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Boutique, améliorations et missions du jour (touche B). Interface IMGUI provisoire.
    /// Onglets : Boutique (lots contre tickets), Améliorations (pièces), Missions (suivi en direct).
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        private static readonly string[] Tabs = { "Boutique", "Améliorations", "Missions" };

        private bool open;
        private int tab;
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

            if (!open && kb.bKey.wasPressedThisFrame && !inMachine && !UIState.PanelOpen) SetOpen(true);
            else if (open && (kb.bKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) SetOpen(false);
        }

        private void SetOpen(bool value)
        {
            open = value;
            UIState.PanelOpen = value;
            message = "";
            if (value && MissionManager.Instance != null) MissionManager.Instance.EnsureToday();
            if (player != null) player.ControlEnabled = !value;
        }

        private void OnGUI()
        {
            if (!open) return;
            float scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            Styles();

            Rect box = new Rect(w * 0.5f - 400, 40, 800, 640);
            GUI.Box(box, GUIContent.none);
            GUILayout.BeginArea(new Rect(box.x + 20, box.y + 14, box.width - 40, box.height - 28));

            var cm = CurrencyManager.Instance;
            var pm = ProgressionManager.Instance;
            GUILayout.Label("BOUTIQUE, AMÉLIORATIONS ET MISSIONS", title);
            GUILayout.Label($"Niveau {(pm != null ? pm.Level : 1)}     Pièces : {(cm != null ? cm.Get(CurrencyType.Coins) : 0)}" +
                            $"     Tickets : {(cm != null ? cm.Get(CurrencyType.Tickets) : 0)}     Jetons premium : {(cm != null ? cm.Get(CurrencyType.PremiumTokens) : 0)}", label);

            int newTab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(32));
            if (newTab != tab) { tab = newTab; message = ""; scroll = Vector2.zero; }

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(420));
            if (tab == 0) DrawShop();
            else if (tab == 1) DrawUpgrades();
            else DrawMissions();
            GUILayout.EndScrollView();

            GUILayout.Label(message, small);
            if (GUILayout.Button("Fermer (B / Échap)", GUILayout.Width(200), GUILayout.Height(34))) SetOpen(false);
            GUILayout.EndArea();
        }

        private void DrawShop()
        {
            var inv = InventoryManager.Instance;
            if (inv == null || inv.Catalog == null) { GUILayout.Label("Boutique indisponible.", label); return; }
            int level = ProgressionManager.Instance != null ? ProgressionManager.Instance.Level : 1;
            long tickets = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Tickets) : 0;

            foreach (RewardData r in inv.Catalog.rewards)
            {
                if (!ShopManager.IsForSale(r)) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label(r.displayName, label, GUILayout.Width(250));
                GUILayout.Label(ClawMachine.RarityName(r.rarity), small, GUILayout.Width(100));
                GUILayout.Label($"{r.shopTicketPrice} tickets", label, GUILayout.Width(120));
                if (level < r.ShopMinLevel) GUILayout.Label($"Niveau {r.ShopMinLevel} requis", small, GUILayout.Width(140));
                else
                {
                    GUI.enabled = tickets >= r.shopTicketPrice;
                    if (GUILayout.Button("Acheter", GUILayout.Width(100))) Buy(r);
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(8);
            GUILayout.Label("Les tickets se gagnent en remportant des lots aux machines.", small);
        }

        private void Buy(RewardData r)
        {
            switch (ShopManager.Buy(r))
            {
                case ShopManager.Result.Ok: message = $"Acheté : {r.displayName} (ajouté à l'inventaire)."; break;
                case ShopManager.Result.LevelTooLow: message = "Niveau insuffisant."; break;
                case ShopManager.Result.NotEnoughTickets: message = "Pas assez de tickets."; break;
                default: message = "Achat impossible."; break;
            }
        }

        private void DrawUpgrades()
        {
            const string clawId = "claw_classic";
            int lvl = UpgradeManager.GetLevel(clawId);
            long cost = UpgradeManager.NextCost(clawId);
            long coins = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Coins) : 0;

            GUILayout.Label("Pince classique — force de prise", title);
            GUILayout.Label($"Niveau {lvl} / {UpgradeManager.MaxLevel}   (bonus actuel : +{lvl * 10} % de réussite de prise, moins de glissades)", label);
            if (cost < 0) GUILayout.Label("Niveau maximum atteint.", small);
            else
            {
                GUI.enabled = coins >= cost;
                if (GUILayout.Button($"Améliorer — {cost} pièces", GUILayout.Width(260), GUILayout.Height(36)))
                    message = UpgradeManager.TryUpgrade(clawId) ? "Pince améliorée !" : "Amélioration impossible.";
                GUI.enabled = true;
            }
        }

        private void DrawMissions()
        {
            var mm = MissionManager.Instance;
            if (mm == null) { GUILayout.Label("Missions indisponibles.", label); return; }
            GUILayout.Label("Missions du jour (renouvelées chaque jour à minuit UTC)", small);
            foreach (var m in mm.Missions)
            {
                MissionManager.Parse(m.missionId, out _, out int target, out _);
                GUILayout.BeginHorizontal();
                GUILayout.Label((m.completed ? "✔ " : "• ") + MissionManager.Title(m.missionId), label, GUILayout.Width(420));
                GUILayout.Label($"{Mathf.Min(m.progress, target)} / {target}", label, GUILayout.Width(90));
                GUILayout.Label($"+{MissionManager.RewardCoins(m.missionId)} pièces, +{MissionManager.RewardXp(m.missionId)} XP", small, GUILayout.Width(200));
                GUILayout.EndHorizontal();
            }

            var am = AchievementManager.Instance;
            if (am == null) return;
            GUILayout.Space(14);
            GUILayout.Label("Succès", title);
            foreach (var a in AchievementManager.All)
            {
                bool done = am.IsUnlocked(a.id);
                GUILayout.BeginHorizontal();
                GUILayout.Label((done ? "✔ " : "• ") + a.title, label, GUILayout.Width(260));
                GUILayout.Label(a.desc, small, GUILayout.Width(300));
                GUILayout.Label(done ? "obtenu" : $"{Mathf.Min(am.Get(a.stat), a.target)} / {a.target}", label, GUILayout.Width(110));
                GUILayout.Label($"+{a.reward} pièces", small, GUILayout.Width(110));
                GUILayout.EndHorizontal();
            }
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
