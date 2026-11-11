using UnityEngine;

namespace ArcadeEmpire.Data
{
    public enum MachineCategory
    {
        Claw,
        CoinPusher,
        Skill,
        Retro,
        Other,
        Decoration
    }

    /// <summary>
    /// Fiche d'une machine d'arcade. Créer via :
    /// clic droit dans Project > Create > ArcadeEmpire > Machine.
    /// </summary>
    [CreateAssetMenu(fileName = "Machine_", menuName = "ArcadeEmpire/Machine")]
    public class MachineData : ScriptableObject
    {
        [Tooltip("Identifiant unique et stable (utilisé par la sauvegarde).")]
        public string id;
        public string displayName;
        public MachineCategory category = MachineCategory.Claw;
        public GameObject prefab;
        [Tooltip("Pièces nécessaires pour une partie.")]
        public long costPerPlay = 5;
        [Tooltip("Prix d'achat en pièces.")]
        public long purchasePrice = 500;
        [Tooltip("Niveau joueur requis pour débloquer la machine.")]
        public int unlockLevel = 1;

        [Header("Construction")]
        [Tooltip("Emprise au sol (largeur x profondeur, en mètres), espace de jeu devant la machine compris.")]
        public Vector2 footprintSize = new Vector2(1.5f, 1.5f);
        [Tooltip("Décalage de l'emprise le long de l'axe Z local (négatif = vers l'avant de la machine).")]
        public float footprintCenterZ = 0f;

        /// <summary>Prix de revente : la moitié du prix d'achat.</summary>
        public long SellPrice => purchasePrice / 2;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name;
        }
    }
}
