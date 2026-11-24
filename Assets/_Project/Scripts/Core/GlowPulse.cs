using UnityEngine;

namespace ArcadeEmpire.Core
{
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
}
