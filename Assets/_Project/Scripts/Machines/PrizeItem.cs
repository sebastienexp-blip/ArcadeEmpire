using ArcadeEmpire.Data;
using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Un lot dans une machine : objet physique (Rigidbody) lié à sa RewardData.
    /// Tenu par la pince, il est relié à la tête par une liaison physique souple qui casse si la prise est insuffisante ;
    /// relâché, il retombe avec la physique normale. Les chocs réels produisent un son proportionnel à la vitesse.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PrizeItem : MonoBehaviour
    {
        public RewardData reward;
        public ClawMachine Machine { get; set; }

        private Rigidbody rb;
        private Collider[] cols;

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

        private ConfigurableJoint joint;
        private Collider[] clawCols;

        /// <summary>
        /// La pince serre le lot : liaison PHYSIQUE souple (ConfigurableJoint) vers la tête de pince. Le lot garde son poids et ses
        /// collisions avec les autres lots ; la liaison casse si l'effort dépasse la force de serrage (lot mal centré, secousses,
        /// arrachement du tas, pince affaiblie en haut) — exactement comme une vraie pince qui laisse glisser.
        /// </summary>
        public void AttachJoint(Rigidbody headBody, Vector3 worldAnchor, float breakForce, Collider[] clawColliders) => AttachJoint(headBody, worldAnchor, breakForce, clawColliders, worldAnchor);

        /// <summary>Tenu par worldAnchor (centre de gravité du lot), ramené en douceur vers holdWorld (l'axe de la pince).</summary>
        public void AttachJoint(Rigidbody headBody, Vector3 worldAnchor, float breakForce, Collider[] clawColliders, Vector3 holdWorld)
        {
            IsHeld = true;
            foreach (Collider c in cols)
                foreach (Collider k in clawColliders)
                    if (c != null && k != null) Physics.IgnoreCollision(c, k, true);
            clawCols = clawColliders;
            rb.WakeUp();
            rb.useGravity = true;
            rb.linearDamping = 0.6f; rb.angularDamping = 1.5f;
            joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = headBody;
            joint.anchor = transform.InverseTransformPoint(worldAnchor);
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = headBody.transform.InverseTransformPoint(holdWorld);
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
            joint.linearLimit = new SoftJointLimit { limit = 0.012f };
            joint.linearLimitSpring = new SoftJointLimitSpring { spring = 600f, damper = 40f };   // ramené dans l'axe sans à-coup
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit = new SoftJointLimit { limit = -25f };
            joint.highAngularXLimit = new SoftJointLimit { limit = 25f };
            joint.angularYLimit = new SoftJointLimit { limit = 25f };
            joint.angularZLimit = new SoftJointLimit { limit = 25f };
            joint.angularXLimitSpring = new SoftJointLimitSpring { spring = 40f, damper = 3f };
            joint.angularYZLimitSpring = new SoftJointLimitSpring { spring = 40f, damper = 3f };
            joint.enableCollision = false;
            SetBreakForce(breakForce);
        }

        /// <summary>Force de serrage courante (la pince s'affaiblit une fois en haut, comme sur les vraies machines).</summary>
        public void SetBreakForce(float f)
        {
            if (joint == null) return;
            joint.breakForce = f;
            joint.breakTorque = f * 0.22f;
        }

        /// <summary>Labo : objets touchés pendant que le lot est tenu (pour comprendre ce qui fait lâcher la prise).</summary>
        public string LabContacts = "";

        private void OnJointBreak(float force)
        {
            joint = null;
            if (!IsHeld) return;
            IsHeld = false;
            rb.linearDamping = 0.35f; rb.angularDamping = 0.6f;
            if (clawCols != null) StartCoroutine(RestoreCollisions());
            if (Machine != null) Machine.OnPrizeSlipped(this, force);
        }

        public void Release()
        {
            if (!IsHeld) return;
            IsHeld = false;
            if (joint != null) { Destroy(joint); joint = null; }
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
            if (IsHeld && LabContacts.Length < 120) LabContacts += c.collider.name + "(" + c.impulse.magnitude.ToString("0.00") + ") ";
            if (Machine == null) return;
            float speed = c.relativeVelocity.magnitude;
            if (speed > 0.25f) Machine.OnPrizeImpact(speed, c.GetContact(0).point, c.rigidbody != null && c.rigidbody.GetComponent<PrizeItem>() != null);
        }
    }
}
