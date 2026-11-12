using UnityEngine;

namespace ArcadeEmpire.Player
{
    /// <summary>
    /// Animation procédurale du personnage : balancement des bras et des jambes selon la vitesse réelle
    /// du CharacterController, léger rebond du buste. Aucune animation importée n'est nécessaire.
    /// </summary>
    public class PlayerVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Transform armLeft, armRight, legLeft, legRight, body;
        [SerializeField] private float swingDegrees = 38f;
        [SerializeField] private float stepsPerMeter = 2.4f;

        private CharacterController controller;
        private float phase;
        private float amount;
        private Vector3 bodyBase;

        public void Configure(Transform aL, Transform aR, Transform lL, Transform lR, Transform bodyRoot)
        {
            armLeft = aL; armRight = aR; legLeft = lL; legRight = lR; body = bodyRoot;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (body != null) bodyBase = body.localPosition;
        }

        private void Update()
        {
            if (controller == null) return;
            Vector3 v = controller.velocity; v.y = 0f;
            float speed = v.magnitude;
            amount = Mathf.MoveTowards(amount, Mathf.Clamp01(speed / 2.5f), Time.deltaTime * 6f);
            phase += speed * stepsPerMeter * Time.deltaTime;
            float s = Mathf.Sin(phase) * swingDegrees * amount;
            Swing(legLeft, s); Swing(legRight, -s);
            Swing(armLeft, -s * 0.8f); Swing(armRight, s * 0.8f);
            if (body != null) body.localPosition = bodyBase + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * 0.025f * amount);
        }

        private static void Swing(Transform t, float degrees)
        {
            if (t != null) t.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}
