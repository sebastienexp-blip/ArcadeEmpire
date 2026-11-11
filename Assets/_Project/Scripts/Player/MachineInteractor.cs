using ArcadeEmpire.Machines;
using ArcadeEmpire.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Player
{
    /// <summary>
    /// Interaction du joueur avec les machines : détecte la machine proche (devant elle), E pour s'installer,
    /// bloque le déplacement et place la caméra sur l'ancrage de la machine. Libère tout quand la machine est quittée.
    /// </summary>
    public class MachineInteractor : MonoBehaviour
    {
        [SerializeField] private float range = 2.4f;

        private PlayerController player;
        private ThirdPersonCamera cam;

        public MachineBase Nearest { get; private set; }
        public MachineBase Active { get; private set; }

        private void Start()
        {
            player = GetComponent<PlayerController>();
            cam = FindFirstObjectByType<ThirdPersonCamera>();
        }

        private void Update()
        {
            if (Active != null)
            {
                if (!Active.IsInUse) Release();
                return;
            }

            if (UIState.PanelOpen)
            {
                Nearest = null;
                return;
            }

            Nearest = FindNearest();
            Keyboard kb = Keyboard.current;
            if (Nearest != null && kb != null && kb.eKey.wasPressedThisFrame && Nearest.Enter())
            {
                Active = Nearest;
                if (player != null) player.ControlEnabled = false;
                if (cam != null) cam.SetOverride(Active.CameraAnchor);
            }
        }

        private MachineBase FindNearest()
        {
            MachineBase best = null;
            float bestDist = range;
            foreach (MachineBase m in MachineBase.All)
            {
                if (m == null || m.IsInUse) continue;
                Vector3 to = transform.position - m.transform.position;
                to.y = 0f;
                float d = to.magnitude;
                // le joueur doit se trouver du côté de la façade (la façade est à l'opposé de l'axe Z local)
                if (d < bestDist && Vector3.Dot(to.normalized, -m.transform.forward) > 0.3f)
                {
                    bestDist = d;
                    best = m;
                }
            }
            return best;
        }

        private void Release()
        {
            Active = null;
            if (player != null) player.ControlEnabled = true;
            if (cam != null) cam.SetOverride(null);
        }
    }
}
