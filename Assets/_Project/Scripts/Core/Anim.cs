using UnityEngine;

namespace ArcadeEmpire.Core
{
    /// <summary>Plateau tournant (lots exposés en vitrine).</summary>
    public class Turntable : MonoBehaviour
    {
        public float degreesPerSecond = 35f;
        public float bob = 0.01f;
        private Vector3 basePos; private float phase;
        private void Start() { basePos = transform.localPosition; phase = Random.value * 6.28f; }
        private void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
            if (bob > 0f) transform.localPosition = basePos + Vector3.up * (Mathf.Sin(Time.time * 1.6f + phase) * bob);
        }
    }

    /// <summary>Lumière ou objet lumineux qui « respire » (téléporteur, néons).</summary>
    public class GlowPulse : MonoBehaviour
    {
        public float speed = 2f, amount = 0.35f;
        public float spin;   // rotation (degrés/s) facultative
        private Light lightComp; private float baseIntensity; private Vector3 baseScale;
        private void Start() { lightComp = GetComponent<Light>(); if (lightComp != null) baseIntensity = lightComp.intensity; baseScale = transform.localScale; }
        private void Update()
        {
            float k = 1f + Mathf.Sin(Time.time * speed) * amount;
            if (lightComp != null) lightComp.intensity = baseIntensity * k;
            else transform.localScale = new Vector3(baseScale.x * (1f + (k - 1f) * 0.15f), baseScale.y, baseScale.z * (1f + (k - 1f) * 0.15f));
            if (spin != 0f) transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.Self);
        }
    }

    /// <summary>Apparition avec rebond (meuble qui vient d'être livré dans la maison).</summary>
    public class PopAppear : MonoBehaviour
    {
        private float t; private Vector3 target;
        private void OnEnable() { target = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale; t = 0f; transform.localScale = target * 0.01f; }
        private void Update()
        {
            t += Time.deltaTime / 0.45f;
            if (t >= 1f) { transform.localScale = target; enabled = false; return; }
            float back = 1f + 2.4f * Mathf.Pow(t - 1f, 3f) + 1.4f * Mathf.Pow(t - 1f, 2f);
            transform.localScale = target * Mathf.Max(0.01f, back);
        }
    }
}
