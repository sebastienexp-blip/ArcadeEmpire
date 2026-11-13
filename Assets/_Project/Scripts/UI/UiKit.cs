using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>Petits utilitaires UGUI : tout l'interface est construite par le code (aucun prefab).</summary>
    public static class UiKit
    {
        public static readonly Color Gold = new Color(1f, 0.9f, 0.3f);
        public static readonly Color Light = new Color(0.9f, 0.95f, 1f);
        public static readonly Color Dim = new Color(0.7f, 0.75f, 0.9f);
        public static readonly Color PanelBg = new Color(0.05f, 0.03f, 0.12f, 0.96f);
        public static readonly Color ButtonBg = new Color(0.16f, 0.2f, 0.42f, 1f);

        private static Font font;
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static Canvas NewCanvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1280f, 720f);
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        /// <summary>Fenêtre centrée, disposition verticale automatique.</summary>
        public static RectTransform Window(Transform canvas, Vector2 size)
        {
            var go = new GameObject("Window", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(canvas, false);
            go.GetComponent<Image>().color = PanelBg;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size;
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 14, 14);
            v.spacing = 6f;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return r;
        }

        public static Text AddText(Transform parent, string text, int size, Color color, float width = -1f, float height = 28f, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font; t.fontSize = size; t.color = color; t.alignment = anchor; t.text = text;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = height; le.preferredHeight = height;
            if (width > 0f) { le.minWidth = width; le.preferredWidth = width; } else le.flexibleWidth = 1f;
            return t;
        }

        public static Button AddButton(Transform parent, string label, float width, float height, UnityAction onClick, bool interactable = true)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = Color.white;
            var b = go.GetComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = ButtonBg; cb.highlightedColor = new Color(0.28f, 0.36f, 0.7f); cb.pressedColor = new Color(0.1f, 0.14f, 0.3f);
            cb.disabledColor = new Color(0.12f, 0.12f, 0.18f, 0.7f); cb.selectedColor = ButtonBg;
            b.colors = cb;
            b.interactable = interactable;
            if (onClick != null) b.onClick.AddListener(onClick);
            var le = go.GetComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = width; le.minHeight = le.preferredHeight = height;

            var tgo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(go.transform, false);
            var t = tgo.GetComponent<Text>();
            t.font = Font; t.fontSize = 17; t.alignment = TextAnchor.MiddleCenter; t.text = label;
            t.color = interactable ? Light : Dim; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var r = tgo.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            return b;
        }

        public static RectTransform AddRow(Transform parent, float height = 34f)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 8f; h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = height;
            return go.GetComponent<RectTransform>();
        }

        /// <summary>Zone défilante ; retourne le contenu (liste verticale).</summary>
        public static RectTransform AddScroll(Transform parent, float height)
        {
            var go = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = height; le.flexibleWidth = 1f;

            var cgo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            cgo.transform.SetParent(go.transform, false);
            var cr = cgo.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0f, 1f); cr.anchorMax = new Vector2(1f, 1f); cr.pivot = new Vector2(0.5f, 1f);
            cr.offsetMin = cr.offsetMax = Vector2.zero;
            var v = cgo.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(8, 8, 6, 6); v.spacing = 4f;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            cgo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = go.GetComponent<ScrollRect>();
            sr.content = cr; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;
            return cr;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i);
                c.SetParent(null);
                Object.Destroy(c.gameObject);
            }
        }
    }
}
