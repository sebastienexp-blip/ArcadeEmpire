using System.Collections.Generic;
using UnityEngine;

namespace ArcadeEmpire.Data
{
    /// <summary>
    /// Catalogue de toutes les récompenses du jeu (collection, boutique, inventaire).
    /// Généré automatiquement par ProjectSetup à partir des assets RewardData du projet.
    /// </summary>
    [CreateAssetMenu(fileName = "RewardCatalog", menuName = "ArcadeEmpire/Reward Catalog")]
    public class RewardCatalog : ScriptableObject
    {
        public List<RewardData> rewards = new List<RewardData>();

        public RewardData Find(string id)
        {
            foreach (RewardData r in rewards)
                if (r != null && r.id == id) return r;
            return null;
        }
    }
}
