using System.Collections.Generic;
using UnityEngine;

namespace ArcadeEmpire.Data
{
    /// <summary>
    /// Catalogue de tout ce qui peut être placé dans la salle (machines et décorations).
    /// Généré automatiquement par ProjectSetup à partir des assets MachineData du projet.
    /// </summary>
    [CreateAssetMenu(fileName = "MachineCatalog", menuName = "ArcadeEmpire/Machine Catalog")]
    public class MachineCatalog : ScriptableObject
    {
        public List<MachineData> machines = new List<MachineData>();

        public MachineData Find(string id)
        {
            foreach (MachineData m in machines)
                if (m != null && m.id == id) return m;
            return null;
        }
    }
}
