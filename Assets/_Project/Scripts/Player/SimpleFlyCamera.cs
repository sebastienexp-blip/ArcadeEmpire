using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Player
{
    /// <summary>
    /// Caméra temporaire pour visiter la salle (remplacée par le personnage à la Phase 3).
    /// ZQSD / WASD / flèches : se déplacer ; clic droit maintenu + souris : regarder ;
    /// Maj : courir ; Espace / Ctrl : monter / descendre.
    /// </summary>
    public class SimpleFlyCamera : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float sprintMultiplier = 2.5f;
        [SerializeField] private float lookSensitivity = 0.12f;

        private float yaw;
        private float pitch;

        private void Start()
        {
            Vector3 e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null) return;

            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue();
                yaw += d.x * lookSensitivity;
                pitch = Mathf.Clamp(pitch - d.y * lookSensitivity, -85f, 85f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            bool left = kb.aKey.isPressed || kb.qKey.isPressed || kb.leftArrowKey.isPressed;      // A (QWERTY) ou Q (AZERTY)
            bool fwd = kb.wKey.isPressed || kb.zKey.isPressed || kb.upArrowKey.isPressed;         // W (QWERTY) ou Z (AZERTY)
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (left ? 1f : 0f);
            float z = (fwd ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            float y = (kb.spaceKey.isPressed ? 1f : 0f) - (kb.leftCtrlKey.isPressed ? 1f : 0f);

            float speed = moveSpeed * (kb.leftShiftKey.isPressed ? sprintMultiplier : 1f);
            Vector3 move = transform.right * x + transform.forward * z + Vector3.up * y;
            transform.position += move * (speed * Time.deltaTime);
        }
    }
}
