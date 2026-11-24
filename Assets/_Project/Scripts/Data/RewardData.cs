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
        [Tooltip("Clés dorées nécessaires (lots sous cadenas de la vitrine). 0 = pas de cadenas.")]
        public int keyPrice = 0;
        [Tooltip("Meuble de la maison (s'installe à son emplacement dans la maison une fois obtenu).")]
        public bool furniture;
        public long experienceValue = 5;

        /// <summary>Prix de revente en pièces, selon la rareté.</summary>
        public long SellValue
        {
            get
            {
                switch (rarity)
                {
                    case Rarity.Common: return 3;
                    case Rarity.Rare: return 12;
                    case Rarity.Epic: return 40;
                    default: return 150;
                }
            }
        }

        /// <summary>Niveau du joueur requis pour acheter ce lot en boutique, selon la rareté.</summary>
        public int ShopMinLevel
        {
            get
            {
                switch (rarity)
                {
                    case Rarity.Common: return 1;
                    case Rarity.Rare: return 3;
                    case Rarity.Epic: return 8;
                    default: return 15;
                }
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name;
        }
    }
}
