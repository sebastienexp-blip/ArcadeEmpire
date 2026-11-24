using UnityEngine;

namespace ArcadeEmpire.Core
{
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
