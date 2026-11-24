using UnityEngine;
using UnityEngine.UI;
using ArcadeEmpire.UI;

namespace ArcadeEmpire.Machines
{
    /// <summary>Rend un bonus bien visible : étiquette flottante qui regarde la caméra, légère lévitation du texte et pulsation lumineuse.</summary>
    public class PusherBonusVisual : MonoBehaviour
    {
        public string customText;
        private Transform label;
        private Material pulse;
        private Color baseEmission;
        private PusherBonus bonus;

        private void Start()
        {
            bonus = GetComponent<PusherBonus>();
            var go = new GameObject("BonusLabel");
            go.transform.SetParent(transform.parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(260f, 70f);
            go.transform.localScale = Vector3.one * 0.00085f;
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var brt = bg.GetComponent<RectTransform>(); brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
            var rend = GetComponent<Renderer>();
            Color c = rend != null ? rend.sharedMaterial.GetColor("_BaseColor") : Color.yellow;
            bg.GetComponent<Image>().color = new Color(0.03f, 0.02f, 0.1f, 0.82f);
            var t = new GameObject("Text", typeof(RectTransform), typeof(Text));
            t.transform.SetParent(go.transform, false);
            var trt = t.GetComponent<RectTransform>(); trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tx = t.GetComponent<Text>();
            tx.font = UiKit.Font; tx.fontSize = 44; tx.fontStyle = FontStyle.Bold; tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.Lerp(c, Color.white, 0.35f); tx.text = !string.IsNullOrEmpty(customText) ? customText : PusherBonus.ShortLabel(bonus != null ? bonus.kind : PusherBonus.Kind.Gem, bonus != null ? bonus.value : 0);
            tx.raycastTarget = false;
            label = go.transform;
            if (rend != null)
            {
                pulse = rend.material;   // instance propre : on fait pulser l'émission
                pulse.EnableKeyword("_EMISSION");
                baseEmission = c;
            }
        }

        private void LateUpdate()
        {
            if (label == null) return;
            label.position = transform.position + Vector3.up * (0.085f + Mathf.Sin(Time.time * 2.4f + transform.position.x * 5f) * 0.008f);
            var cam = Camera.main;
            if (cam != null) label.rotation = Quaternion.LookRotation(label.position - cam.transform.position, Vector3.up);
            if (pulse != null) pulse.SetColor("_EmissionColor", baseEmission * (1.2f + Mathf.Sin(Time.time * 4f) * 0.9f));
        }

        private void OnDestroy() { if (label != null) Destroy(label.gameObject); if (pulse != null) Destroy(pulse); }
    }
}
