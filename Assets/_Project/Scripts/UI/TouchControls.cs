using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Commandes tactiles (mobile uniquement) : croix directionnelle et boutons d'action. Chaque bouton simule une touche du clavier
    /// (OnScreenButton), donc tout le jeu existant fonctionne sans modification : déplacement ZQSD, E pour utiliser une machine,
    /// ESPACE pour l'action, Échap pour revenir, I / B / C / M pour les menus.
    /// Se crée tout seul sur Android et iOS. Sur PC, mettre ShowOnPc à true pour tester.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static bool ShowOnPc = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Application.isMobilePlatform && !ShowOnPc) return;
            if (FindFirstObjectByType<TouchControls>() != null) return;
            var go = new GameObject("TouchControls");
            DontDestroyOnLoad(go);
            go.AddComponent<TouchControls>();
        }

        private void Start()
        {
            UiKit.EnsureEventSystem();
            var canvas = UiKit.NewCanvas(transform, "TouchCanvas", 30);

            // Croix directionnelle (bas gauche)
            Button(canvas.transform, "icon:up", "<Keyboard>/w", new Vector2(0, 0), new Vector2(150, 170), 84);
            Button(canvas.transform, "icon:down", "<Keyboard>/s", new Vector2(0, 0), new Vector2(150, 40), 84);
            Button(canvas.transform, "icon:left", "<Keyboard>/a", new Vector2(0, 0), new Vector2(50, 105), 84);
            Button(canvas.transform, "icon:right", "<Keyboard>/d", new Vector2(0, 0), new Vector2(250, 105), 84);

            // Actions (bas droite)
            Button(canvas.transform, "ACTION", "<Keyboard>/space", new Vector2(1, 0), new Vector2(-110, 105), 130);
            Button(canvas.transform, "E\njouer", "<Keyboard>/e", new Vector2(1, 0), new Vector2(-260, 70), 92);
            Button(canvas.transform, "R\ngains", "<Keyboard>/r", new Vector2(1, 0), new Vector2(-380, 70), 80);
            Button(canvas.transform, "icon:close", "<Keyboard>/escape", new Vector2(1, 0), new Vector2(-260, 180), 80);

        }

        /// <summary>Applique la taille et l'opacité choisies dans les options.</summary>
        private sealed class TouchStyle : MonoBehaviour
        {
            private Vector2 baseSize; private Vector2 basePos; private bool init;
            private void OnEnable() { GameSettings.Changed += Apply; Apply(); }
            private void OnDisable() => GameSettings.Changed -= Apply;
            private void Apply()
            {
                var r = (RectTransform)transform;
                if (!init) { baseSize = r.sizeDelta; basePos = r.anchoredPosition; init = true; }
                r.sizeDelta = baseSize * GameSettings.TouchScale; r.anchoredPosition = basePos * GameSettings.TouchScale;
                var img = GetComponent<Image>(); var c = img.color; c.a = GameSettings.TouchOpacity; img.color = c;
            }
        }

        private static void Button(Transform parent, string label, string path, Vector2 anchor, Vector2 pos, float size)
        {
            var go = new GameObject("Touch_" + path, typeof(RectTransform), typeof(Image));
            go.SetActive(false);   // le chemin de la touche doit être réglé avant l'activation
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = UiTheme.Circle; img.color = new Color(0.1f, 0.12f, 0.3f, 0.55f);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos; r.sizeDelta = new Vector2(size, size);

            go.AddComponent<TouchStyle>();
            if (label.StartsWith("icon:"))
            {
                var ig = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                ig.transform.SetParent(go.transform, false);
                var ii = ig.GetComponent<Image>(); ii.sprite = UiTheme.Icon(label.Substring(5)); ii.raycastTarget = false;
                var ir = ig.GetComponent<RectTransform>(); ir.anchorMin = new Vector2(0.2f, 0.2f); ir.anchorMax = new Vector2(0.8f, 0.8f); ir.offsetMin = ir.offsetMax = Vector2.zero;
                var o = go.AddComponent<OnScreenButton>(); o.controlPath = path; go.SetActive(true);
                return;
            }
            var t = new GameObject("Label", typeof(RectTransform), typeof(Text));
            t.transform.SetParent(go.transform, false);
            var tx = t.GetComponent<Text>();
            tx.font = UiKit.Font; tx.text = label; tx.fontSize = size >= 100 ? 24 : 17; tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter; tx.color = UiKit.Light; tx.raycastTarget = false;
            var tr = t.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;

            var osb = go.AddComponent<OnScreenButton>();
            osb.controlPath = path;
            go.SetActive(true);
        }
    }
}
