using UnityEngine;

namespace ArcadeEmpire.Data
{
    public enum Rarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    /// <summary>
    /// Une récompense (peluche, figurine, capsule…). Créer via :
    /// clic droit dans Project > Create > ArcadeEmpire > Reward.
    /// </summary>
    [CreateAssetMenu(fileName = "Reward_", menuName = "ArcadeEmpire/Reward")]
    public class RewardData : ScriptableObject
    {
        [Tooltip("Identifiant unique et stable (utilisé par la sauvegarde). Ne jamais le modifier après coup.")]
        public string id;
        public string displayName;
        public Rarity rarity = Rarity.Common;
        public Sprite icon;
        [Tooltip("Objet 3D affiché dans les machines (prefab).")]
        public GameObject prefab;
        [Tooltip("Tickets gagnés quand cette récompense est obtenue.")]
        public int ticketValue = 1;
        [Tooltip("Prix en tickets dans la boutique (0 = non vendable).")]
        public int shopTicketPrice = 0;
        public long experienceValue = 5;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name;
        }
    }
}
