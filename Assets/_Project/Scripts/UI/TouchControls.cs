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
            Button(canvas.transform, "▲", "<Keyboard>/w", new Vector2(0, 0), new Vector2(150, 170), 84);
            Button(canvas.transform, "▼", "<Keyboard>/s", new Vector2(0, 0), new Vector2(150, 40), 84);
            Button(canvas.transform, "◀", "<Keyboard>/a", new Vector2(0, 0), new Vector2(50, 105), 84);
            Button(canvas.transform, "▶", "<Keyboard>/d", new Vector2(0, 0), new Vector2(250, 105), 84);

            // Actions (bas droite)
            Button(canvas.transform, "ACTION", "<Keyboard>/space", new Vector2(1, 0), new Vector2(-110, 120), 130);
            Button(canvas.transform, "E\njouer", "<Keyboard>/e", new Vector2(1, 0), new Vector2(-260, 70), 92);
            Button(canvas.transform, "Retour", "<Keyboard>/escape", new Vector2(1, 0), new Vector2(-260, 180), 80);

            // Menus (haut droite)
            Button(canvas.transform, "I", "<Keyboard>/i", new Vector2(1, 1), new Vector2(-250, -40), 56);
            Button(canvas.transform, "B", "<Keyboard>/b", new Vector2(1, 1), new Vector2(-185, -40), 56);
            Button(canvas.transform, "C", "<Keyboard>/c", new Vector2(1, 1), new Vector2(-120, -40), 56);
            Button(canvas.transform, "M", "<Keyboard>/m", new Vector2(1, 1), new Vector2(-55, -40), 56);
        }

        private static void Button(Transform parent, string label, string path, Vector2 anchor, Vector2 pos, float size)
        {
            var go = new GameObject("Touch_" + path, typeof(RectTransform), typeof(Image));
            go.SetActive(false);   // le chemin de la touche doit être réglé avant l'activation
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.1f, 0.12f, 0.3f, 0.55f);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos; r.sizeDelta = new Vector2(size, size);

            var t = new GameObject("Label", typeof(RectTransform), typeof(Text));
            t.transform.SetParent(go.transform, false);
            var tx = t.GetComponent<Text>();
            tx.font = UiKit.Font; tx.text = label; tx.fontSize = size >= 100 ? 24 : 20; tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter; tx.color = UiKit.Light; tx.raycastTarget = false;
            var tr = t.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;

            var osb = go.AddComponent<OnScreenButton>();
            osb.controlPath = path;
            go.SetActive(true);
        }
    }
}
