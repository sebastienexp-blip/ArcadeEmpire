using UnityEngine;

namespace ArcadeEmpire.Data
{
    public enum MachineCategory
    {
        Claw,
        CoinPusher,
        Skill,
        Retro,
        Other
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

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name;
        }
    }
}
