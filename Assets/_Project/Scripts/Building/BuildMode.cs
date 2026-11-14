using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using ArcadeEmpire.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Building
{
    /// <summary>
    /// Mode construction (touche C) : vue du dessus, achat/placement des machines et décorations (clic gauche),
    /// sélection et déplacement d'un objet posé, rotation (R), revente (Suppr / X), choix de l'ambiance.
    /// Interface IMGUI provisoire. Un aperçu au sol (vert = valide, rouge = invalide) suit la souris.
    /// </summary>
    public class BuildMode : MonoBehaviour
    {
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Vector3 topViewPosition = new Vector3(0f, 10.5f, -6.2f);
        [SerializeField] private Vector3 topViewLookAt = new Vector3(0f, 0f, -0.6f);

        private static readonly string[] Tabs = { "Machines", "Décors", "Ambiances" };

        private bool active;
        private int tab;
        private Vector2 scroll;
        private string message = "";
        private float rotation;
        private MachineData pending;
        private PlacedObject selected;
        private Vector3 cursor;
        private bool cursorValid;
        private GameObject ghost;
        private Material ghostInstance;
        private Transform topAnchor;
        private Rect panelRect;

        private BuildingManager mgr;
        private RoomThemeManager themes;
        private PlayerController player;
        private MachineInteractor interactor;
        private ThirdPersonCamera cam;
        private GUIStyle title, label, small;

        public void Configure(Material ghost) => ghostMaterial = ghost;

        private void Start()
        {
            mgr = BuildingManager.Instance;
            themes = RoomThemeManager.Instance;
            player = FindFirstObjectByType<PlayerController>();
            interactor = FindFirstObjectByType<MachineInteractor>();
            cam = FindFirstObjectByType<ThirdPersonCamera>();

            topAnchor = new GameObject("BuildCameraAnchor").transform;
            topAnchor.SetPositionAndRotation(topViewPosition, Quaternion.LookRotation(topViewLookAt - topViewPosition));

            ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "BuildGhost";
            Destroy(ghost.GetComponent<Collider>());
            if (ghostMaterial != null)
            {
                ghostInstance = new Material(ghostMaterial);
                ghost.GetComponent<MeshRenderer>().sharedMaterial = ghostInstance;
            }
            ghost.SetActive(false);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null || mouse == null || mgr == null) return;

            if (!active)
            {
                bool inMachine = interactor != null && interactor.Active != null;
                if ((kb.cKey.wasPressedThisFrame || UIState.Consume("build")) && !inMachine && !UIState.PanelOpen) SetActive(true);
                return;
            }

            // Échap / clic droit : annuler la sélection, sinon quitter le mode
            if (kb.escapeKey.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                if (pending != null || selected != null) { pending = null; selected = null; message = ""; }
                else SetActive(false);
                return;
            }
            if (kb.cKey.wasPressedThisFrame && pending == null && selected == null) { SetActive(false); return; }

            if (kb.rKey.wasPressedThisFrame) rotation = (rotation + 90f) % 360f;
            if ((kb.deleteKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame) && selected != null)
            {
                long refund = mgr.Sell(selected);
                message = $"Revendu : +{refund} pièces.";
                selected = null;
            }

            UpdateCursor(mouse);
            UpdateGhost();

            if (mouse.leftButton.wasPressedThisFrame && !MouseOverPanel(mouse)) OnClick();
        }

        private void SetActive(bool value)
        {
            active = value;
            UIState.PanelOpen = value;
            pending = null;
            selected = null;
            message = "";
            if (player != null) player.ControlEnabled = !value;
            if (cam != null) cam.SetOverride(value ? topAnchor : null);
            if (themes != null) themes.SetCeilingVisible(!value);
            if (!value && ghost != null) ghost.SetActive(false);
        }

        private void UpdateCursor(Mouse mouse)
        {
            Camera c = Camera.main;
            if (c == null) return;
            Ray ray = c.ScreenPointToRay(mouse.position.ReadValue());
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d))
                cursor = BuildingManager.Snap(ray.GetPoint(d));
        }

        private MachineData Current => pending != null ? pending : (selected != null ? selected.Data : null);

        private void UpdateGhost()
        {
            MachineData md = Current;
            if (md == null) { ghost.SetActive(false); return; }

            float rot = rotation;
            cursorValid = mgr.IsValid(md, cursor, rot, selected);
            if (pending != null) cursorValid &= mgr.MeetsLevel(md) && mgr.CanAfford(md);

            BuildingManager.GetRect(md, cursor, rot, out Vector2 center, out Vector2 size);
            ghost.transform.position = new Vector3(center.x, 0.06f, center.y);
            ghost.transform.localScale = new Vector3(size.x, 0.1f, size.y);
            if (ghostInstance != null)
                ghostInstance.SetColor("_BaseColor", cursorValid ? new Color(0.2f, 1f, 0.4f, 0.45f) : new Color(1f, 0.2f, 0.2f, 0.45f));
            ghost.SetActive(true);
        }

        private void OnClick()
        {
            if (pending != null)
            {
                switch (mgr.Place(pending, cursor, rotation))
                {
                    case BuildingManager.PlaceResult.Ok:
                        message = $"{pending.displayName} posé (−{pending.purchasePrice} pièces).";
                        if (!mgr.CanAfford(pending)) pending = null;
                        break;
                    case BuildingManager.PlaceResult.NotEnoughCoins: message = "Pas assez de pièces."; break;
                    case BuildingManager.PlaceResult.LevelTooLow: message = $"Niveau {pending.unlockLevel} requis."; break;
                    case BuildingManager.PlaceResult.Invalid: message = "Emplacement impossible (occupé ou hors de la salle)."; break;
                    default: message = "Objet indisponible."; break;
                }
                return;
            }

            // Clic sur un objet posé : le sélectionner (ou le déplacer si un autre est déjà sélectionné et que la cible est libre)
            PlacedObject hit = PickPlaced();
            if (hit != null && hit != selected)
            {
                selected = hit;
                rotation = hit.Entry.rotationY;
                message = $"Sélectionné : {hit.Data.displayName}. Clic : déplacer — R : pivoter — Suppr : vendre.";
                return;
            }
            if (selected != null)
            {
                if (mgr.Move(selected, cursor, rotation)) { message = "Déplacé."; selected = null; }
                else message = "Emplacement impossible (occupé ou hors de la salle).";
            }
        }

        private PlacedObject PickPlaced()
        {
            Camera c = Camera.main;
            if (c == null || Mouse.current == null) return null;
            Ray ray = c.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<PlacedObject>();
            return null;
        }

        private bool MouseOverPanel(Mouse mouse)
        {
            float s = Screen.height / 720f;
            Vector2 p = mouse.position.ReadValue();
            return panelRect.Contains(new Vector2(p.x / s, (Screen.height - p.y) / s));
        }

        // --- Interface ---------------------------------------------------------------------------------

        private void OnGUI()
        {
            if (!active || mgr == null) return;
            float scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            Styles();

            panelRect = new Rect(10, 10, 360, 700);
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(20, 18, 340, 684));

            var cm = CurrencyManager.Instance;
            var pm = ProgressionManager.Instance;
            GUILayout.Label("MODE CONSTRUCTION", title);
            GUILayout.Label($"Pièces : {(cm != null ? cm.Get(CurrencyType.Coins) : 0)}     Niveau {(pm != null ? pm.Level : 1)}", label);

            int newTab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(30));
            if (newTab != tab) { tab = newTab; scroll = Vector2.zero; }

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(420));
            if (tab == 2) DrawThemes();
            else DrawItems(tab == 1);
            GUILayout.EndScrollView();

            if (selected != null)
            {
                GUILayout.Label($"Sélection : {selected.Data.displayName}", label);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Pivoter (R)", GUILayout.Height(30))) rotation = (rotation + 90f) % 360f;
                if (GUILayout.Button($"Vendre +{selected.Data.SellPrice}", GUILayout.Height(30)))
                {
                    message = $"Revendu : +{mgr.Sell(selected)} pièces.";
                    selected = null;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(message, small);
            if (GUILayout.Button("Quitter (C / Échap)", GUILayout.Height(32))) SetActive(false);
            GUILayout.EndArea();

            GUI.Label(new Rect(w * 0.5f - 400, 686, 800, 28),
                "Clic : poser / sélectionner    R : pivoter    Suppr : vendre    Clic droit / Échap : annuler    C : quitter", small);
        }

        private void DrawItems(bool decorations)
        {
            if (mgr.Catalog == null) { GUILayout.Label("Catalogue indisponible.", label); return; }
            foreach (MachineData md in mgr.Catalog.machines)
            {
                if (md == null || (md.category == MachineCategory.Decoration) != decorations) continue;
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(md.displayName, label);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{md.purchasePrice} pièces", small, GUILayout.Width(110));
                if (!mgr.MeetsLevel(md)) GUILayout.Label($"Niveau {md.unlockLevel} requis", small);
                else
                {
                    GUI.enabled = mgr.CanAfford(md);
                    if (GUILayout.Button(pending == md ? "En cours…" : "Acheter et placer", GUILayout.Height(26)))
                    {
                        pending = md;
                        selected = null;
                        rotation = 0f;
                        message = "Clique dans la salle pour poser. R : pivoter.";
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        private void DrawThemes()
        {
            if (themes == null) { GUILayout.Label("Ambiances indisponibles.", label); return; }
            foreach (RoomTheme t in RoomThemes.All)
            {
                bool unlocked = themes.IsUnlocked(t.id);
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(t.name + (themes.CurrentId == t.id ? "   (actuelle)" : ""), label);
                GUILayout.BeginHorizontal();
                if (unlocked)
                {
                    GUI.enabled = themes.CurrentId != t.id;
                    if (GUILayout.Button("Appliquer", GUILayout.Height(26))) { themes.Apply(t.id); message = $"Ambiance : {t.name}."; }
                    GUI.enabled = true;
                }
                else
                {
                    GUILayout.Label($"{t.price} pièces — niveau {t.minLevel}", small, GUILayout.Width(170));
                    if (GUILayout.Button("Débloquer", GUILayout.Height(26)))
                    {
                        switch (themes.Unlock(t.id))
                        {
                            case RoomThemeManager.Result.Ok: themes.Apply(t.id); message = $"Débloquée : {t.name}."; break;
                            case RoomThemeManager.Result.LevelTooLow: message = $"Niveau {t.minLevel} requis."; break;
                            default: message = "Pas assez de pièces."; break;
                        }
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
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
            small.normal.textColor = new Color(0.8f, 0.85f, 1f);
        }
    }
}
