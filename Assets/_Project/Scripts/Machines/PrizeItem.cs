using ArcadeEmpire.Data;
using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Un lot dans une machine : objet physique (Rigidbody) lié à sa RewardData.
    /// Tenu par la pince, il est entraîné par la force de prise (vitesse dirigée vers la pince, collisions avec les autres lots conservées) ;
    /// relâché, il retombe avec la physique normale. Les chocs réels produisent un son proportionnel à la vitesse.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PrizeItem : MonoBehaviour
    {
        public RewardData reward;
        public ClawMachine Machine { get; set; }

        private Rigidbody rb;
        private Collider[] cols;
        private Vector3 heldLocal;
        private Transform holder;

        public Rigidbody Body => rb;
        public bool IsHeld { get; private set; }
        public bool Collected { get; set; }
        public bool IsFree => !IsHeld && !Collected;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cols = GetComponentsInChildren<Collider>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.sleepThreshold = 0.05f;
        }

        /// <summary>La pince serre le lot : il suit le point de prise, sans gravité, en gardant sa position relative.</summary>
        public void Attach(Transform holdPoint, Transform head, Collider[] clawColliders)
        {
            IsHeld = true;
            holder = head;
            heldLocal = head.InverseTransformPoint(rb.position);
            rb.useGravity = false;
            rb.linearDamping = 4f; rb.angularDamping = 6f;
            foreach (Collider c in cols)
                foreach (Collider k in clawColliders)
                    if (c != null && k != null) Physics.IgnoreCollision(c, k, true);
            clawCols = clawColliders;
        }

        private Collider[] clawCols;

        private void FixedUpdate()
        {
            if (!IsHeld || holder == null) return;
            Vector3 err = holder.TransformPoint(heldLocal) - rb.position;
            Vector3 v = err / Time.fixedDeltaTime * 0.45f;
            rb.linearVelocity = Vector3.ClampMagnitude(v, 7f);
        }

        public void Release()
        {
            if (!IsHeld) return;
            IsHeld = false;
            holder = null;
            rb.useGravity = true;
            rb.linearDamping = 0.35f; rb.angularDamping = 0.6f;
            rb.angularVelocity += Random.insideUnitSphere * 1.2f;
            rb.WakeUp();
            if (clawCols != null) StartCoroutine(RestoreCollisions());
        }

        private System.Collections.IEnumerator RestoreCollisions()
        {
            yield return new WaitForSeconds(0.5f);   // le temps de se dégager des branches
            foreach (Collider c in cols)
                foreach (Collider k in clawCols)
                    if (c != null && k != null) Physics.IgnoreCollision(c, k, false);
            clawCols = null;
        }

        private void OnCollisionEnter(Collision c)
        {
            if (Machine == null) return;
            float speed = c.relativeVelocity.magnitude;
            if (speed > 0.25f) Machine.OnPrizeImpact(speed, c.GetContact(0).point, c.rigidbody != null && c.rigidbody.GetComponent<PrizeItem>() != null);
        }
    }
}
