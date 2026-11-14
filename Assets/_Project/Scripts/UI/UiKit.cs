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
            go.AddComponent<UiScaleApplier>();
            return c;
        }

        /// <summary>Applique le réglage « taille de l'interface » aux canvas.</summary>
        public sealed class UiScaleApplier : MonoBehaviour
        {
            private CanvasScaler scaler;
            private void OnEnable() { scaler = GetComponent<CanvasScaler>(); ArcadeEmpire.Core.GameSettings.Changed += Apply; Apply(); }
            private void OnDisable() => ArcadeEmpire.Core.GameSettings.Changed -= Apply;
            private void Apply() { if (scaler != null) scaler.referenceResolution = new Vector2(1280f, 720f) / ArcadeEmpire.Core.GameSettings.UiScale; }
        }

        public static Image Round(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.sprite = UiTheme.Rounded; img.type = Image.Type.Sliced; img.color = color;
            return img;
        }

        /// <summary>Icône dessinée (voir UiTheme.Icon) placée dans un parent, taille fixe dans une disposition.</summary>
        public static Image AddIcon(Transform parent, string icon, float size, Color? tint = null)
        {
            var go = new GameObject("Icon_" + icon, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = UiTheme.Icon(icon); img.color = tint ?? Color.white; img.raycastTarget = false; img.preserveAspect = true;
            var le = go.GetComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = size; le.minHeight = le.preferredHeight = size;
            return img;
        }

        /// <summary>Bouton avec une grande icône et un libellé (et une touche facultative).</summary>
        public static Button AddIconButton(Transform parent, string icon, string label, float width, float height, UnityAction onClick, string keyCap = null, Color? accent = null)
        {
            Button b = AddButton(parent, "", width, height, onClick);
            Transform lbl = b.transform.Find("Label");
            if (lbl != null) Object.Destroy(lbl.gameObject);
            var h = b.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 10, 6, 6); h.spacing = 10f; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            AddIcon(h.transform, icon, Mathf.Min(height - 12f, 44f), accent);
            Text t = AddText(h.transform, label, 18, Light, -1f, height - 12f, TextAnchor.MiddleLeft, true);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (!string.IsNullOrEmpty(keyCap)) AddKeyCap(h.transform, keyCap);
            return b;
        }

        public static void AddKeyCap(Transform parent, string key)
        {
            var go = new GameObject("Key", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            Round(go, new Color(0.9f, 0.93f, 1f, 0.95f));
            var le = go.GetComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = Mathf.Max(30f, 16f + key.Length * 13f); le.minHeight = le.preferredHeight = 30f;
            var tgo = new GameObject("T", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(go.transform, false);
            var t = tgo.GetComponent<Text>();
            t.font = Font; t.text = key; t.fontSize = 16; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter; t.color = new Color(0.1f, 0.12f, 0.25f); t.raycastTarget = false;
            var r = tgo.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        }

        /// <summary>Titre de section : trait coloré + texte.</summary>
        public static void AddHeader(Transform parent, string icon, string text)
        {
            RectTransform row = AddRow(parent, 44f);
            if (icon != null) AddIcon(row, icon, 34f);
            AddText(row, text, 22, Gold, -1f, 40f, TextAnchor.MiddleLeft, true);
        }

        /// <summary>Ligne « libellé ... curseur  valeur ». Retourne le curseur.</summary>
        public static Slider AddSlider(Transform parent, string label, float min, float max, float value, UnityAction<float> onChange, System.Func<float, string> format = null, bool whole = false)
        {
            RectTransform row = AddRow(parent, 44f);
            AddText(row, label, 18, Light, 300f, 40f);
            var sgo = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            sgo.transform.SetParent(row, false);
            var le = sgo.GetComponent<LayoutElement>(); le.minWidth = le.preferredWidth = 260f; le.minHeight = le.preferredHeight = 30f; le.flexibleWidth = 1f;
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sgo.transform, false);
            Round(bg, new Color(0.1f, 0.12f, 0.26f, 1f));
            var br = bg.GetComponent<RectTransform>(); br.anchorMin = new Vector2(0f, 0.5f); br.anchorMax = new Vector2(1f, 0.5f); br.sizeDelta = new Vector2(0f, 12f);
            var fa = new GameObject("FillArea", typeof(RectTransform));
            fa.transform.SetParent(sgo.transform, false);
            var far = fa.GetComponent<RectTransform>(); far.anchorMin = new Vector2(0f, 0.5f); far.anchorMax = new Vector2(1f, 0.5f); far.sizeDelta = new Vector2(-16f, 12f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fa.transform, false);
            Round(fill, UiTheme.Cyan);
            var ha = new GameObject("HandleArea", typeof(RectTransform));
            ha.transform.SetParent(sgo.transform, false);
            var har = ha.GetComponent<RectTransform>(); har.anchorMin = Vector2.zero; har.anchorMax = Vector2.one; har.offsetMin = new Vector2(8f, 0f); har.offsetMax = new Vector2(-8f, 0f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(ha.transform, false);
            var himg = handle.GetComponent<Image>(); himg.sprite = UiTheme.Circle; himg.color = Color.white;
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(26f, 26f);
            var sl = sgo.GetComponent<Slider>();
            sl.fillRect = fill.GetComponent<RectTransform>(); sl.handleRect = handle.GetComponent<RectTransform>(); sl.targetGraphic = himg;
            sl.minValue = min; sl.maxValue = max; sl.wholeNumbers = whole; sl.value = value;
            Text val = AddText(row, format != null ? format(value) : value.ToString("0.00"), 18, Gold, 90f, 40f, TextAnchor.MiddleRight, true);
            sl.onValueChanged.AddListener(v => { val.text = format != null ? format(v) : v.ToString("0.00"); onChange?.Invoke(v); });
            return sl;
        }

        /// <summary>Interrupteur Oui / Non.</summary>
        public static void AddToggle(Transform parent, string label, bool value, UnityAction<bool> onChange)
        {
            RectTransform row = AddRow(parent, 44f);
            AddText(row, label, 18, Light, -1f, 40f);
            bool state = value;
            Button b = null;
            Image knob = null;
            Text txt = null;
            void Paint()
            {
                b.GetComponent<Image>().color = state ? new Color(0.15f, 0.55f, 0.35f) : new Color(0.3f, 0.15f, 0.2f);
                txt.text = state ? "OUI" : "NON";
                if (knob != null) knob.sprite = UiTheme.Icon(state ? "check" : "close");
            }
            b = AddButton(row, "", 120f, 36f, () => { state = !state; Paint(); onChange?.Invoke(state); });
            b.targetGraphic = null;
            Transform lbl = b.transform.Find("Label");
            txt = lbl.GetComponent<Text>(); txt.fontStyle = FontStyle.Bold;
            Paint();
        }

        /// <summary>Sélecteur « ◀ choix ▶ » pour une liste de valeurs.</summary>
        public static void AddSelector(Transform parent, string label, string[] options, int index, UnityAction<int> onChange)
        {
            RectTransform row = AddRow(parent, 44f);
            AddText(row, label, 18, Light, -1f, 40f);
            int cur = Mathf.Clamp(index, 0, Mathf.Max(0, options.Length - 1));
            Text txt = null;
            Button l = AddButton(row, "", 44f, 36f, null);
            SetButtonIcon(l, "left");
            txt = AddText(row, options.Length > 0 ? options[cur] : "-", 18, Gold, 190f, 36f, TextAnchor.MiddleCenter, true);
            Button r = AddButton(row, "", 44f, 36f, null);
            SetButtonIcon(r, "right");
            l.onClick.AddListener(() => { cur = (cur + options.Length - 1) % options.Length; txt.text = options[cur]; onChange?.Invoke(cur); });
            r.onClick.AddListener(() => { cur = (cur + 1) % options.Length; txt.text = options[cur]; onChange?.Invoke(cur); });
        }

        public static void SetButtonIcon(Button b, string icon)
        {
            Transform lbl = b.transform.Find("Label");
            if (lbl != null) Object.Destroy(lbl.gameObject);
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(b.transform, false);
            var img = go.GetComponent<Image>(); img.sprite = UiTheme.Icon(icon); img.raycastTarget = false; img.preserveAspect = true;
            var r = go.GetComponent<RectTransform>(); r.anchorMin = new Vector2(0.15f, 0.15f); r.anchorMax = new Vector2(0.85f, 0.85f); r.offsetMin = r.offsetMax = Vector2.zero;
        }

        /// <summary>Fenêtre centrée, disposition verticale automatique.</summary>
        public static RectTransform Window(Transform canvas, Vector2 size)
        {
            var go = new GameObject("Window", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(canvas, false);
            Round(go, PanelBg);
            var outline = go.AddComponent<Outline>(); outline.effectColor = new Color(0.4f, 0.55f, 1f, 0.55f); outline.effectDistance = new Vector2(2f, -2f);
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
            var img = go.GetComponent<Image>(); img.sprite = UiTheme.Rounded; img.type = Image.Type.Sliced; img.color = Color.white;
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
            Round(go, new Color(0f, 0f, 0.05f, 0.35f));
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
