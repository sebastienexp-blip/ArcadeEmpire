using ArcadeEmpire.Core;
using ArcadeEmpire.Player;
using ArcadeEmpire.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Menu pause + options complet : Graphismes, Audio, Commandes et caméra, Interface, Partie. Échap l'ouvre quand rien d'autre n'est ouvert
    /// (et le ferme), ou via le bouton « Options » du HUD. Le temps est figé tant qu'il est ouvert. Tous les réglages sont enregistrés et appliqués tout de suite.
    /// </summary>
    public class OptionsMenu : MonoBehaviour
    {
        private static readonly string[] Tabs = { "Graphismes", "Audio", "Commandes", "Interface", "Partie" };
        private static readonly string[] TabIcons = { "monitor", "speaker", "gamepad", "eye", "save" };

        private bool open;
        private int tab;
        private Canvas canvas;
        private RectTransform content;
        private Text titleText, messageText;
        private Button[] tabButtons;
        private MachineInteractor interactor;
        private PlayerController player;
        private bool confirmReset;
        private float prevScale = 1f;

        private void Start()
        {
            interactor = FindFirstObjectByType<MachineInteractor>();
            player = FindFirstObjectByType<PlayerController>();
            UiKit.EnsureEventSystem();
            canvas = UiKit.NewCanvas(transform, "OptionsCanvas", 40);

            // voile sombre
            var veil = new GameObject("Veil", typeof(RectTransform), typeof(Image));
            veil.transform.SetParent(canvas.transform, false);
            veil.GetComponent<Image>().color = new Color(0f, 0f, 0.03f, 0.65f);
            var vr = veil.GetComponent<RectTransform>(); vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = vr.offsetMax = Vector2.zero;

            RectTransform win = UiKit.Window(canvas.transform, new Vector2(980f, 660f));
            var head = UiKit.AddRow(win, 56f);
            UiKit.AddIcon(head, "gear", 44f);
            titleText = UiKit.AddText(head, "OPTIONS", 30, UiKit.Gold, -1f, 52f, TextAnchor.MiddleLeft, true);
            UiKit.AddButton(head, "", 52f, 44f, () => SetOpen(false));
            UiKit.SetButtonIcon(head.GetChild(head.childCount - 1).GetComponent<Button>(), "close");

            var tabRow = UiKit.AddRow(win, 54f);
            tabButtons = new Button[Tabs.Length];
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                tabButtons[i] = UiKit.AddIconButton(tabRow, TabIcons[i], Tabs[i], 180f, 52f, () => { tab = index; confirmReset = false; messageText.text = ""; Rebuild(); });
            }
            content = UiKit.AddScroll(win, 420f);
            messageText = UiKit.AddText(win, "", 16, UiKit.Dim, -1f, 26f, TextAnchor.MiddleCenter);
            var bottom = UiKit.AddRow(win, 50f);
            UiKit.AddIconButton(bottom, "play", "Reprendre", 200f, 48f, () => SetOpen(false), "Échap");
            UiKit.AddIconButton(bottom, "refresh", "Valeurs par défaut", 260f, 48f, () => { GameSettings.ResetToDefaults(); messageText.text = "Réglages remis par défaut."; Rebuild(); });
            canvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            bool inMachine = interactor != null && interactor.Active != null;
            if (!open)
            {
                bool escape = kb != null && kb.escapeKey.wasPressedThisFrame && !inMachine && !UIState.PanelOpen && Time.frameCount != UIState.LastClosedFrame;
                if (escape || UIState.Consume("options")) SetOpen(true);
            }
            else if (kb != null && kb.escapeKey.wasPressedThisFrame) SetOpen(false);
        }

        private void SetOpen(bool value)
        {
            if (value == open) return;
            open = value;
            UIState.PanelOpen = value;
            UIState.Paused = value;
            if (value) { prevScale = Time.timeScale > 0f ? Time.timeScale : 1f; Time.timeScale = 0f; AudioListener.pause = false; }
            else { Time.timeScale = prevScale; if (SaveManager.Instance != null) SaveManager.Instance.SaveNow(); }
            if (player != null && !(interactor != null && interactor.Active != null)) player.ControlEnabled = !value;
            canvas.gameObject.SetActive(value);
            confirmReset = false;
            if (value) { messageText.text = ""; Rebuild(); }
        }

        private void OnDisable() { if (open) { Time.timeScale = prevScale; UIState.PanelOpen = false; UIState.Paused = false; open = false; } }

        private void Rebuild()
        {
            titleText.text = "OPTIONS — " + Tabs[tab].ToUpperInvariant();
            for (int i = 0; i < tabButtons.Length; i++)
                tabButtons[i].GetComponent<Image>().color = i == tab ? new Color(0.3f, 0.42f, 0.9f) : UiKit.ButtonBg;
            UiKit.Clear(content);
            switch (tab)
            {
                case 0: Graphics(); break;
                case 1: Audio(); break;
                case 2: Controls(); break;
                case 3: Interface(); break;
                default: Game(); break;
            }
        }

        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + " %";

        private void Graphics()
        {
            Transform c = content;
            UiKit.AddHeader(c, "monitor", "Affichage");
            if (!Application.isMobilePlatform)
            {
                Resolution[] rs = GameSettings.Resolutions();
                if (rs.Length > 0)
                {
                    var names = new string[rs.Length];
                    int cur = rs.Length - 1;
                    for (int i = 0; i < rs.Length; i++)
                    {
                        names[i] = $"{rs[i].width} × {rs[i].height}";
                        if (rs[i].width == Screen.width && rs[i].height == Screen.height) cur = i;
                    }
                    if (GameSettings.ResolutionIndex >= 0 && GameSettings.ResolutionIndex < rs.Length) cur = GameSettings.ResolutionIndex;
                    UiKit.AddSelector(c, "Résolution", names, cur, i => { GameSettings.ResolutionIndex = i; GameSettings.ApplyScreen(); });
                }
                UiKit.AddToggle(c, "Plein écran", GameSettings.Fullscreen, v => { GameSettings.Fullscreen = v; GameSettings.ApplyScreen(); });
                UiKit.AddSelector(c, "Synchronisation verticale", new[] { "Désactivée", "Activée (60)", "Moitié" }, Mathf.Clamp(GameSettings.VSync, 0, 2), i => { GameSettings.VSync = i; GameSettings.ApplyGraphics(); });
            }
            var fpsNames = new[] { "30 FPS", "60 FPS", "90 FPS", "120 FPS", "Illimité" };
            int fi = System.Array.IndexOf(GameSettings.FpsChoices, GameSettings.TargetFps);
            UiKit.AddSelector(c, "Limite d'images / s", fpsNames, Mathf.Max(0, fi), i => { GameSettings.TargetFps = GameSettings.FpsChoices[i]; GameSettings.ApplyGraphics(); });

            UiKit.AddHeader(c, "bolt", "Qualité");
            UiKit.AddSlider(c, "Résolution du rendu", 0.5f, 1.5f, GameSettings.RenderScale, v => { GameSettings.RenderScale = v; GameSettings.ApplyGraphics(); }, Pct);
            UiKit.AddSelector(c, "Ombres", new[] { "Aucune", "Faibles", "Normales", "Hautes" }, GameSettings.Shadows, i => { GameSettings.Shadows = i; GameSettings.ApplyGraphics(); });
            UiKit.AddSelector(c, "Anti-crénelage", new[] { "Aucun", "MSAA 2x", "MSAA 4x", "MSAA 8x" }, GameSettings.Antialiasing, i => { GameSettings.Antialiasing = i; GameSettings.ApplyGraphics(); });
            UiKit.AddText(c, "Astuce : baissez la résolution du rendu et les ombres si le jeu saccade sur votre appareil.", 15, UiKit.Dim, -1f, 40f);
        }

        private void Audio()
        {
            Transform c = content;
            UiKit.AddHeader(c, "speaker", "Volumes");
            UiKit.AddSlider(c, "Volume général", 0f, 1f, GameAudio.Master, v => GameAudio.Master = v, Pct);
            UiKit.AddSlider(c, "Effets sonores", 0f, 1f, GameAudio.SfxVolume, v => GameAudio.SfxVolume = v, Pct);
            UiKit.AddSlider(c, "Musique d'ambiance", 0f, 1f, GameAudio.MusicVolume, v => GameAudio.MusicVolume = v, Pct);
            UiKit.AddButton(c, "Tester un effet sonore", 260f, 40f, () => Sfx.Play(Sfx.Kind.Coin, Camera.main != null ? Camera.main.transform.position : Vector3.zero));
            UiKit.AddText(c, "La touche M coupe / remet la musique, les touches - et + règlent le volume général.", 15, UiKit.Dim, -1f, 40f);
        }

        private void Controls()
        {
            Transform c = content;
            UiKit.AddHeader(c, "camera", "Caméra");
            UiKit.AddSlider(c, "Sensibilité de rotation", 0.2f, 3f, GameSettings.LookSensitivity, v => GameSettings.LookSensitivity = v, v => v.ToString("0.0") + "x");
            UiKit.AddToggle(c, "Inverser l'axe vertical", GameSettings.InvertY, v => GameSettings.InvertY = v);
            UiKit.AddSlider(c, "Champ de vision", 0.8f, 1.3f, GameSettings.FovScale, v => { GameSettings.FovScale = v; }, v => Pct(v));
            UiKit.AddToggle(c, "Boutons de caméra à l'écran", GameSettings.ShowCameraPad, v => { GameSettings.ShowCameraPad = v; GameSettings.NotifyChanged(); });

            UiKit.AddHeader(c, "gamepad", "Touches");
            string[][] keys =
            {
                new[] { "Z Q S D  /  W A S D  /  flèches", "Se déplacer" },
                new[] { "Maj", "Courir" },
                new[] { "Clic droit + glisser", "Tourner la caméra (aussi : Début / Fin / Page haut / Page bas)" },
                new[] { "Molette", "Zoom (marche) ou champ de vision (machine)" },
                new[] { "V", "Changer de vue devant une machine" },
                new[] { "Retour arrière", "Recentrer la caméra" },
                new[] { "E", "S'installer / quitter une machine" },
                new[] { "Espace", "Action principale de la machine" },
                new[] { "R", "Récupérer les gains (pousse-pièces)" },
                new[] { "I  /  B  /  C", "Inventaire  /  Boutique  /  Construction" },
                new[] { "M  /  -  /  +", "Musique  /  volume" },
                new[] { "Échap", "Quitter la machine, fermer, ou ouvrir ce menu" },
            };
            foreach (var k in keys)
            {
                RectTransform row = UiKit.AddRow(c, 36f);
                UiKit.AddKeyCap(row, k[0]);
                UiKit.AddText(row, k[1], 16, UiKit.Light, -1f, 34f);
            }
            {
                UiKit.AddHeader(c, "touch", "Commandes tactiles");
                UiKit.AddSlider(c, "Taille des boutons", 0.7f, 1.5f, GameSettings.TouchScale, v => { GameSettings.TouchScale = v; GameSettings.NotifyChanged(); }, Pct);
                UiKit.AddSlider(c, "Opacité des boutons", 0.2f, 1f, GameSettings.TouchOpacity, v => { GameSettings.TouchOpacity = v; GameSettings.NotifyChanged(); }, Pct);
            }
        }

        private void Interface()
        {
            Transform c = content;
            UiKit.AddHeader(c, "eye", "Interface");
            UiKit.AddSlider(c, "Taille de l'interface", 0.8f, 1.3f, GameSettings.UiScale, v => { GameSettings.UiScale = v; GameSettings.NotifyChanged(); }, Pct);
            UiKit.AddToggle(c, "Afficher les aides de touches", GameSettings.ShowKeyHints, v => { GameSettings.ShowKeyHints = v; GameSettings.NotifyChanged(); });
        }

        private void Game()
        {
            Transform c = content;
            UiKit.AddHeader(c, "save", "Sauvegarde");
            UiKit.AddToggle(c, "Sauvegarde automatique (toutes les minutes)", GameSettings.AutoSave, v => GameSettings.AutoSave = v);
            UiKit.AddIconButton(c, "save", "Sauvegarder maintenant", 360f, 50f, () =>
            {
                bool ok = SaveManager.Instance != null && SaveManager.Instance.SaveNow();
                messageText.text = ok ? "Partie sauvegardée." : "Sauvegarde impossible.";
            });
            UiKit.AddHeader(c, "door", "Partie");
            if (!Application.isMobilePlatform)
                UiKit.AddIconButton(c, "door", "Quitter le jeu", 360f, 50f, () =>
                {
                    if (SaveManager.Instance != null) SaveManager.Instance.SaveNow();
                    Time.timeScale = 1f;
                    Application.Quit();
                });
            if (!confirmReset)
                UiKit.AddIconButton(c, "refresh", "Effacer la partie…", 360f, 50f, () => { confirmReset = true; Rebuild(); }, null, UiTheme.Red);
            else
            {
                UiKit.AddText(c, "Tout sera effacé : pièces, niveau, inventaire, construction. Cette action est définitive.", 17, UiTheme.Red, -1f, 52f);
                RectTransform row = UiKit.AddRow(c, 50f);
                UiKit.AddIconButton(row, "check", "Oui, tout effacer", 260f, 48f, () =>
                {
                    if (SaveManager.Instance != null) SaveManager.Instance.ResetAll();
                    Time.timeScale = 1f; UIState.PanelOpen = false; UIState.Paused = false; open = false;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }, null, UiTheme.Red);
                UiKit.AddIconButton(row, "close", "Annuler", 200f, 48f, () => { confirmReset = false; Rebuild(); });
            }
            UiKit.AddText(c, "ARCADE EMPIRE — version de développement", 14, UiKit.Dim, -1f, 30f, TextAnchor.MiddleCenter);
        }
    }
}
