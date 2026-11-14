using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>Zone sous le bord avant du plateau : pièces et lots qui y tombent sont attribués au joueur.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PusherCollector : MonoBehaviour
    {
        [SerializeField] private CoinPusherMachine machine;

        public void Configure(CoinPusherMachine m) => machine = m;

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (machine == null) return;
            PusherCoin coin = other.GetComponentInParent<PusherCoin>();
            if (coin != null) { machine.OnCoinCollected(coin); return; }
            PusherBonus bonus = other.GetComponentInParent<PusherBonus>();
            if (bonus != null) { machine.OnBonusCollected(bonus); return; }
            PrizeItem prize = other.GetComponentInParent<PrizeItem>();
            if (prize != null && prize.IsFree) machine.OnPrizeCollected(prize);
        }
    }
}
