using ArcadeEmpire.Data;
using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Un lot dans une machine : objet physique (Rigidbody) lié à sa RewardData.
    /// Tenu par la pince, il est désactivé physiquement ; relâché, il retombe avec la physique normale.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PrizeItem : MonoBehaviour
    {
        public RewardData reward;

        private Rigidbody rb;

        public bool IsHeld { get; private set; }
        public bool Collected { get; set; }
        public bool IsFree => !IsHeld && !Collected;

        private void Awake() => rb = GetComponent<Rigidbody>();

        public void Attach(Transform holdPoint)
        {
            IsHeld = true;
            rb.isKinematic = true;
            rb.detectCollisions = false;   // ne pousse pas les autres lots et ne déclenche pas la zone de récupération
            transform.SetParent(holdPoint, true);
        }

        public void Release()
        {
            if (!IsHeld) return;
            IsHeld = false;
            transform.SetParent(null, true);
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Random.insideUnitSphere * 1.5f;   // léger basculement à la chute
            rb.WakeUp();
        }
    }
}
