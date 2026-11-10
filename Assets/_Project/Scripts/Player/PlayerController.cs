using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Player
{
    /// <summary>
    /// Déplacement du personnage à la 3e personne (PC) : ZQSD / WASD / flèches, Maj pour courir.
    /// Le mouvement est relatif à la caméra. Nécessite un CharacterController.
    /// Les contrôles tactiles (Android) seront ajoutés dans la phase mobile : la propriété MoveInput
    /// permet de les brancher sans toucher au reste.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 5.6f;
        [SerializeField] private float turnSmoothing = 14f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private Transform cameraTransform;

        /// <summary>Entrée de déplacement (x = droite, y = avant), normalisée. Écrite par le clavier ou plus tard le joystick tactile.</summary>
        public Vector2 MoveInput { get; set; }
        public bool RunInput { get; set; }
        /// <summary>Quand false (ex. devant une machine), le personnage ne bouge plus.</summary>
        public bool ControlEnabled { get; set; } = true;
        public float CurrentSpeed { get; private set; }

        private CharacterController controller;
        private float verticalVelocity;

        public void SetCamera(Transform cam) => cameraTransform = cam;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            ReadKeyboard();
            Vector3 planar = Vector3.zero;

            if (ControlEnabled && MoveInput.sqrMagnitude > 0.001f)
            {
                Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);
                Vector3 fwd = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
                Vector3 right = cam != null ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
                Vector3 dir = (fwd * MoveInput.y + right * MoveInput.x);
                if (dir.sqrMagnitude > 1f) dir.Normalize();

                float speed = RunInput ? runSpeed : walkSpeed;
                planar = dir * speed;

                Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime));
            }

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = planar + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
            CurrentSpeed = planar.magnitude;
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
