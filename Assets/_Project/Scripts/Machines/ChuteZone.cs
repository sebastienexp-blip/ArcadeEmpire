using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>Zone de récupération sous la trappe : un lot libre qui y entre est attribué au joueur.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ChuteZone : MonoBehaviour
    {
        [SerializeField] private ClawMachine machine;

        public void Configure(ClawMachine m) => machine = m;

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other) => Check(other);
        private void OnTriggerStay(Collider other) => Check(other);

        private void Check(Collider other)
        {
            if (machine == null) return;
            PrizeItem prize = other.GetComponentInParent<PrizeItem>();
            if (prize != null && prize.IsFree) machine.OnPrizeCollected(prize);
        }
    }
}
