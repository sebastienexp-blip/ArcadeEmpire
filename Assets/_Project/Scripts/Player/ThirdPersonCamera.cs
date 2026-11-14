using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Player
{
    /// <summary>
    /// Caméra à la 3e personne : suit le joueur avec un amortissement, orbite (clic droit ou glisser au doigt), zoom (molette ou
    /// pincement), évite les murs, se recentre doucement derrière le joueur quand il avance, et élargit son champ de vision
    /// avec la vitesse. Devant une machine, glisse vers l'ancrage avec une optique plus serrée.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] private float distance = 4.2f;
        [SerializeField] private float minDistance = 1.8f;
        [SerializeField] private float maxDistance = 7f;
        [SerializeField] private float lookSensitivity = 0.15f;
        [SerializeField] private float touchSensitivity = 0.12f;
        [SerializeField] private float baseFov = 60f;
        [SerializeField] private float runFovBonus = 7f;
        [SerializeField] private float machineFov = 50f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private Camera cam;
        private PlayerController player;
        private float yaw, pitch = 18f;
        private float smoothYaw, smoothPitch, yawVel, pitchVel;
        private float currentDistance;
        private Vector3 pivotSmooth, pivotVel;
        private float manualLookTimer;
        private Transform overrideAnchor;
        private Vector3 overrideVel;

        // Vue machine (première personne) : décalages réglables par le joueur
        private float mYaw, mPitch, mFovZoom = 1f;
        private int mView;
        private Vector3 mOffsetSmooth;
        private static readonly Vector3[] ViewOffsets = { Vector3.zero, new Vector3(0f, -0.25f, 0.55f), new Vector3(0f, 0.45f, -0.7f) };
        public static readonly string[] ViewNames = { "Vue standard", "Vue rapprochée", "Vue surélevée" };

        public static ThirdPersonCamera Instance { get; private set; }
        public bool InMachineView => overrideAnchor != null;
        public int MachineView => mView;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void SetTarget(Transform t) => target = t;

        /// <summary>Place la caméra sur un ancrage (machine) ; null = reprendre le suivi du joueur.</summary>
        public void SetOverride(Transform anchor)
        {
            if (anchor != overrideAnchor) ResetMachineView();
            overrideAnchor = anchor;
        }

        /// <summary>Tourne la vue (degrés) : en orbite autour du joueur, ou en regard libre devant la machine.</summary>
        public void Nudge(float yawDeg, float pitchDeg)
        {
            if (overrideAnchor != null)
            {
                mYaw = Mathf.Clamp(mYaw + yawDeg, -70f, 70f);
                mPitch = Mathf.Clamp(mPitch + pitchDeg, -35f, 40f);
            }
            else AddLook(yawDeg, pitchDeg, false);
        }

        public void Zoom(float amount)
        {
            if (overrideAnchor != null) mFovZoom = Mathf.Clamp(mFovZoom - amount * 0.12f, 0.55f, 1.5f);
            else distance = Mathf.Clamp(distance - amount * 0.6f, minDistance, maxDistance);
        }

        public void CycleView()
        {
            if (overrideAnchor == null) return;
            mView = (mView + 1) % ViewOffsets.Length;
        }

        public void ResetMachineView() { mYaw = 0f; mPitch = 0f; mFovZoom = 1f; mView = 0; }
        public void ResetView() { if (overrideAnchor != null) ResetMachineView(); else { yaw = target != null ? target.eulerAngles.y : yaw; pitch = 18f; distance = 4.2f; } }

        private void Start()
        {
            cam = GetComponent<Camera>();
            cam.fieldOfView = baseFov;
            currentDistance = distance;
            if (target != null)
            {
                yaw = smoothYaw = target.eulerAngles.y;
                pivotSmooth = target.position + targetOffset;
                player = target.GetComponent<PlayerController>();
            }
            smoothPitch = pitch;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || cam == null) return;

            if (overrideAnchor != null)
            {
                ReadLookInput(dt);
                mOffsetSmooth = Vector3.Lerp(mOffsetSmooth, ViewOffsets[mView], 1f - Mathf.Exp(-6f * dt));
                Vector3 wantPos = overrideAnchor.TransformPoint(mOffsetSmooth);
                Quaternion wantRot = overrideAnchor.rotation * Quaternion.Euler(mPitch, mYaw, 0f);
                transform.position = Vector3.SmoothDamp(transform.position, wantPos, ref overrideVel, 0.3f);
                transform.rotation = Quaternion.Slerp(transform.rotation, wantRot, 1f - Mathf.Exp(-9f * dt));
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, machineFov * mFovZoom * GameSettings.FovScale, 1f - Mathf.Exp(-5f * dt));
                return;
            }
            if (target == null) return;

            ReadLookInput(dt);

            // Recentrage doux derrière le joueur quand il avance sans toucher à la caméra
            if (player != null && manualLookTimer <= 0f && player.MoveInput.y > 0.5f && Mathf.Abs(player.MoveInput.x) < 0.3f && player.CurrentSpeed > 1f)
                yaw = Mathf.MoveTowardsAngle(yaw, target.eulerAngles.y, 45f * dt);
            manualLookTimer -= dt;

            smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawVel, 0.07f);
            smoothPitch = Mathf.SmoothDamp(smoothPitch, pitch, ref pitchVel, 0.07f);

            // Le point visé suit le joueur avec un petit retard : la caméra « respire » avec le mouvement
            Vector3 desiredPivot = target.position + targetOffset;
            pivotSmooth = Vector3.SmoothDamp(pivotSmooth, desiredPivot, ref pivotVel, 0.09f);

            Quaternion rot = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
            Vector3 back = rot * Vector3.back;

            float wanted = distance;
            if (Physics.SphereCast(desiredPivot, 0.25f, back, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
                wanted = Mathf.Max(0.6f, hit.distance - 0.1f);
            // on se rapproche vite d'un obstacle, on s'en éloigne lentement : pas de saccade
            float k = wanted < currentDistance ? 22f : 5f;
            currentDistance = Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-k * dt));

            Vector3 pos = pivotSmooth + back * currentDistance;
            if (pos.y < 0.3f) pos.y = 0.3f;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pivotSmooth - pos, Vector3.up));

            // Champ de vision : plus large en courant (sensation de vitesse)
            float run01 = player != null ? Mathf.Clamp01((player.CurrentSpeed - 3.2f) / Mathf.Max(0.1f, player.RunSpeed - 3.2f)) : 0f;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, (baseFov + runFovBonus * run01) * GameSettings.FovScale, 1f - Mathf.Exp(-5f * dt));
        }

        private void ReadLookInput(float dt)
        {
            float sens = GameSettings.LookSensitivity;
            float inv = GameSettings.InvertY ? -1f : 1f;
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if ((mouse.rightButton.isPressed || mouse.middleButton.isPressed) && !PointerOverUi())
                {
                    Vector2 d = mouse.delta.ReadValue();
                    AddLook(d.x * lookSensitivity * sens, d.y * lookSensitivity * sens * inv);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f && !PointerOverUi()) Zoom(Mathf.Sign(scroll));
            }

            // Clavier : Début / Fin = tourner à gauche / droite, Page haut / bas = incliner, V = changer de vue, Retour arrière = recentrer
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                float k = 90f * dt;
                if (kb.homeKey.isPressed) Nudge(-k, 0f);
                if (kb.endKey.isPressed) Nudge(k, 0f);
                if (kb.pageUpKey.isPressed) Nudge(0f, -k * 0.6f * inv);
                if (kb.pageDownKey.isPressed) Nudge(0f, k * 0.6f * inv);
                if (kb.vKey.wasPressedThisFrame) CycleView();
                if (kb.backspaceKey.wasPressedThisFrame) ResetView();
            }

            Touchscreen ts = Touchscreen.current;
            if (ts == null) return;
            int count = 0; Vector2 a = Vector2.zero, b = Vector2.zero;
            var free = new System.Collections.Generic.List<UnityEngine.InputSystem.Controls.TouchControl>(2);
            foreach (var t in ts.touches)
            {
                if (!t.press.isPressed) continue;
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.touchId.ReadValue())) continue;   // doigt sur un bouton
                free.Add(t); count++;
                if (count == 2) break;
            }
            if (free.Count == 1) { Vector2 d = free[0].delta.ReadValue(); AddLook(d.x * touchSensitivity * sens, d.y * touchSensitivity * sens * inv); }
            else if (free.Count == 2)
            {
                a = free[0].position.ReadValue(); b = free[1].position.ReadValue();
                Vector2 da = free[0].delta.ReadValue(), db = free[1].delta.ReadValue();
                float prev = ((a - da) - (b - db)).magnitude, now = (a - b).magnitude;
                Zoom((now - prev) * 0.02f);   // pincement
            }
        }

        private static bool PointerOverUi() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void AddLook(float dx, float dy, bool dragSign = true)
        {
            if (overrideAnchor != null)
            {
                mYaw = Mathf.Clamp(mYaw + dx * 0.6f, -70f, 70f);
                mPitch = Mathf.Clamp(mPitch - dy * 0.6f, -35f, 40f);
                return;
            }
            yaw += dx;
            pitch = Mathf.Clamp(dragSign ? pitch - dy : pitch + dy, -25f, 75f);
            manualLookTimer = 2.5f;
        }
    }
}
