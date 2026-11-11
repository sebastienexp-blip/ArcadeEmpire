using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Player
{
    /// <summary>
    /// Caméra à la 3e personne : suit le joueur, orbite avec le clic droit maintenu (ou la souris si le curseur
    /// est verrouillé), zoom à la molette, et se rapproche quand un mur est derrière le joueur.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float distance = 4.2f;
        [SerializeField] private float minDistance = 1.8f;
        [SerializeField] private float maxDistance = 7f;
        [SerializeField] private float lookSensitivity = 0.15f;
        [SerializeField] private float followSmoothing = 12f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private float yaw;
        private float pitch = 18f;
        private float currentDistance;

        private Transform overrideAnchor;

        public void SetTarget(Transform t) => target = t;

        /// <summary>Place la caméra sur un ancrage (machine) ; null = reprendre le suivi du joueur.</summary>
        public void SetOverride(Transform anchor) => overrideAnchor = anchor;

        private void Start()
        {
            currentDistance = distance;
            if (target != null) yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (overrideAnchor != null)
            {
                float k = 1f - Mathf.Exp(-6f * Time.deltaTime);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(transform.position, overrideAnchor.position, k),
                    Quaternion.Slerp(transform.rotation, overrideAnchor.rotation, k));
                return;
            }
            if (target == null) return;

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    yaw += d.x * lookSensitivity;
                    pitch = Mathf.Clamp(pitch - d.y * lookSensitivity, -10f, 70f);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * 0.5f, minDistance, maxDistance);
            }

            Vector3 pivot = target.position + targetOffset;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rot * Vector3.back;

            // Évite de traverser les murs : on raccourcit la distance si un obstacle est derrière le joueur
            float wanted = distance;
            if (Physics.SphereCast(pivot, 0.25f, back, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
                wanted = Mathf.Max(0.6f, hit.distance - 0.1f);

            currentDistance = Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-followSmoothing * Time.deltaTime));
            transform.SetPositionAndRotation(pivot + back * currentDistance, rot);
        }
    }
}
