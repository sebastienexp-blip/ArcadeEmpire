using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>Apparition animée d'un panneau : léger zoom avec rebond et fondu (à chaque activation).</summary>
    public class UiPopIn : MonoBehaviour
    {
        public float duration = 0.22f;
        private CanvasGroup group;

        private void OnEnable()
        {
            if (group == null) { group = GetComponent<CanvasGroup>(); if (group == null) group = gameObject.AddComponent<CanvasGroup>(); }
            StopAllCoroutines();
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float back = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);   // ease-out avec léger dépassement
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, back);
                group.alpha = Mathf.SmoothStep(0f, 1f, k * 1.4f);
                yield return null;
            }
            transform.localScale = Vector3.one; group.alpha = 1f;
        }
    }

    /// <summary>« Pulsation » d'un élément (monnaie qui augmente, bouton qui attire l'œil).</summary>
    public class UiPunch : MonoBehaviour
    {
        private float t = 1f;
        public void Punch() => t = 0f;
        private void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime * 3.5f);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.18f * (1f - t * 0.5f));
        }
    }

    /// <summary>Fondu au noir plein écran (téléporteur, changement de lieu).</summary>
    public class ScreenFader : MonoBehaviour
    {
        private static ScreenFader instance;
        private Image img;

        public static ScreenFader Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("ScreenFader");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ScreenFader>();
                var c = UiKit.NewCanvas(go.transform, "FadeCanvas", 100);
                var i = new GameObject("Black", typeof(RectTransform), typeof(Image));
                i.transform.SetParent(c.transform, false);
                var r = i.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                instance.img = i.GetComponent<Image>(); instance.img.color = new Color(0.02f, 0.01f, 0.06f, 0f); instance.img.raycastTarget = false;
                return instance;
            }
        }

        /// <summary>Fond au noir, exécute l'action (téléportation), puis revient.</summary>
        public void FadeThrough(System.Action middle, float half = 0.28f) => StartCoroutine(Run(middle, half));

        private IEnumerator Run(System.Action middle, float half)
        {
            img.raycastTarget = true;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime) { SetA(t / half); yield return null; }
            SetA(1f);
            middle?.Invoke();
            yield return null; yield return null;
            for (float t = 0f; t < half * 1.4f; t += Time.unscaledDeltaTime) { SetA(1f - t / (half * 1.4f)); yield return null; }
            SetA(0f);
            img.raycastTarget = false;
        }

        private void SetA(float a) { var c = img.color; c.a = Mathf.SmoothStep(0f, 1f, a); img.color = c; }
    }

    /// <summary>Texte flottant au-dessus d'un point du monde (« +12 tickets », « +1 clé ») qui monte et s'efface.</summary>
    public class FloatingText : MonoBehaviour
    {
        private static Canvas canvas;
        private Vector3 world; private float t; private Text text; private RectTransform rt;

        public static void Show(Vector3 worldPos, string msg, Color color, int size = 34)
        {
            if (canvas == null)
            {
                var go = new GameObject("FloatingTexts");
                DontDestroyOnLoad(go);
                canvas = UiKit.NewCanvas(go.transform, "FloatCanvas", 25);
                canvas.GetComponent<GraphicRaycaster>().enabled = false;
            }
            var g = new GameObject("Float", typeof(RectTransform), typeof(Text));
            g.transform.SetParent(canvas.transform, false);
            var tx = g.GetComponent<Text>();
            tx.font = UiKit.Font; tx.text = msg; tx.fontSize = size; tx.fontStyle = FontStyle.Bold; tx.color = color;
            tx.alignment = TextAnchor.MiddleCenter; tx.horizontalOverflow = HorizontalWrapMode.Overflow; tx.raycastTarget = false;
            var o = g.AddComponent<Outline>(); o.effectColor = new Color(0.05f, 0.02f, 0.1f, 0.9f); o.effectDistance = new Vector2(2f, -2f);
            var f = g.AddComponent<FloatingText>();
            f.world = worldPos; f.text = tx; f.rt = (RectTransform)g.transform; f.rt.sizeDelta = new Vector2(600f, 60f);
        }

        private void LateUpdate()
        {
            t += Time.deltaTime;
            var cam = Camera.main;
            if (cam == null || t > 1.6f) { Destroy(gameObject); return; }
            Vector3 sp = cam.WorldToScreenPoint(world + Vector3.up * (t * 0.35f));
            if (sp.z < 0f) { text.enabled = false; return; }
            text.enabled = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, sp, null, out Vector2 lp);
            rt.anchoredPosition = lp;
            float pop = t < 0.15f ? Mathf.Lerp(0.5f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.15f) / 0.2f));
            rt.localScale = Vector3.one * pop;
            var c = text.color; c.a = t < 1.1f ? 1f : Mathf.Clamp01(1f - (t - 1.1f) / 0.5f); text.color = c;
        }
    }
}
