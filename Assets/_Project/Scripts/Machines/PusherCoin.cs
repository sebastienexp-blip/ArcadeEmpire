using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>Pièce du coin pusher : objet physique réutilisé via un pool. Signale ses chocs à la machine (bruitages).</summary>
    public class PusherCoin : MonoBehaviour
    {
        public bool Collected { get; set; }
        public float StuckTime { get; set; }
        public CoinPusherMachine Machine { get; set; }
        public Rigidbody Body { get; private set; }

        private void Awake() => Body = GetComponent<Rigidbody>();

        private void OnCollisionEnter(Collision c)
        {
            if (Machine == null) return;
            float speed = c.relativeVelocity.magnitude;
            if (speed < 0.3f) return;
            Machine.OnCoinImpact(speed, c.GetContact(0).point, c.rigidbody != null);
        }
    }
}
