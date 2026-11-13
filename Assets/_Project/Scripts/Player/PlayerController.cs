using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Player
{
    /// <summary>
    /// Déplacement à la 3e personne avec une vraie inertie : accélération et freinage progressifs, rotation à vitesse limitée,
    /// gravité et adhérence au sol (escaliers, pentes), poussée douce des objets mobiles. Relatif à la caméra.
    /// Clavier : ZQSD / WASD / flèches, Maj pour courir. Tactile : les boutons simulent ces touches (TouchControls).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 5.6f;
        [SerializeField] private float acceleration = 16f;      // m/s² en accélération
        [SerializeField] private float deceleration = 22f;      // m/s² au freinage
        [SerializeField] private float turnSpeed = 720f;        // degrés par seconde
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float terminalVelocity = -30f;
        [SerializeField] private float groundStick = -3f;       // plaque le personnage au sol dans les marches
        [SerializeField] private float pushPower = 1.6f;
        [SerializeField] private Transform cameraTransform;

        /// <summary>Entrée de déplacement (x = droite, y = avant), normalisée.</summary>
        public Vector2 MoveInput { get; set; }
        public bool RunInput { get; set; }
        /// <summary>Quand false (devant une machine, menu ouvert), le personnage freine et ne bouge plus.</summary>
        public bool ControlEnabled { get; set; } = true;
        public float CurrentSpeed { get; private set; }
        public Vector3 PlanarVelocity => planarVelocity;
        public bool IsGrounded { get; private set; }
        public float RunSpeed => runSpeed;
        /// <summary>Vitesse de rotation actuelle du personnage (degrés par seconde, signée).</summary>
        public float YawRate { get; private set; }

        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalVelocity;

        public void SetCamera(Transform cam) => cameraTransform = cam;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.slopeLimit = 45f;
            controller.skinWidth = Mathf.Max(controller.skinWidth, 0.03f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            ReadKeyboard();

            Vector3 wanted = Vector3.zero;
            if (ControlEnabled && MoveInput.sqrMagnitude > 0.001f)
            {
                Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);
                Vector3 fwd = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
                Vector3 right = cam != null ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
                Vector3 dir = fwd * MoveInput.y + right * MoveInput.x;
                if (dir.sqrMagnitude > 1f) dir.Normalize();
                wanted = dir * (RunInput ? runSpeed : walkSpeed);
            }

            // Inertie : on rejoint la vitesse voulue avec une accélération limitée (plus faible en l'air)
            float rate = (wanted.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration) * (IsGrounded ? 1f : 0.3f);
            planarVelocity = Vector3.MoveTowards(planarVelocity, wanted, rate * dt);

            // Le personnage se tourne vers sa direction de déplacement, d'autant plus vite qu'il avance
            float prevYaw = transform.eulerAngles.y;
            if (planarVelocity.sqrMagnitude > 0.05f)
            {
                Quaternion target = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * dt);
            }
            YawRate = Mathf.Lerp(YawRate, Mathf.DeltaAngle(prevYaw, transform.eulerAngles.y) / dt, 1f - Mathf.Exp(-12f * dt));

            if (IsGrounded && verticalVelocity < 0f) verticalVelocity = groundStick;
            verticalVelocity = Mathf.Max(terminalVelocity, verticalVelocity + gravity * dt);

            CollisionFlags flags = controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
            IsGrounded = controller.isGrounded || (flags & CollisionFlags.Below) != 0;
            if ((flags & CollisionFlags.Sides) != 0)
            {
                // contre un mur : on retire la composante qui y pousse, pour ne pas « coller » au mur
                Vector3 actual = controller.velocity; actual.y = 0f;
                if (actual.sqrMagnitude < planarVelocity.sqrMagnitude) planarVelocity = Vector3.MoveTowards(planarVelocity, actual, 30f * dt);
            }
            CurrentSpeed = planarVelocity.magnitude;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody rb = hit.rigidbody;
            if (rb == null || rb.isKinematic || rb.mass > 25f || hit.moveDirection.y < -0.3f) return;
            Vector3 push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            rb.AddForceAtPosition(push * pushPower * Mathf.Clamp(CurrentSpeed, 0.5f, 5.6f), hit.point, ForceMode.Impulse);
        }

        private void ReadKeyboard()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            bool left = kb.aKey.isPressed || kb.qKey.isPressed || kb.leftArrowKey.isPressed;      // A (QWERTY) ou Q (AZERTY)
            bool fwd = kb.wKey.isPressed || kb.zKey.isPressed || kb.upArrowKey.isPressed;         // W (QWERTY) ou Z (AZERTY)
            bool right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
            bool back = kb.sKey.isPressed || kb.downArrowKey.isPressed;

            Vector2 input = new Vector2((right ? 1f : 0f) - (left ? 1f : 0f), (fwd ? 1f : 0f) - (back ? 1f : 0f));
            MoveInput = Vector2.ClampMagnitude(input, 1f);
            RunInput = kb.leftShiftKey.isPressed;
        }
    }
}
