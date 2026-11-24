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
}
