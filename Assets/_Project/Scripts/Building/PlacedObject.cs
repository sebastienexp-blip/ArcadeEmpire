using ArcadeEmpire.Data;
using ArcadeEmpire.Save;
using UnityEngine;

namespace ArcadeEmpire.Building
{
    /// <summary>Lie un objet posé dans la salle à sa fiche (MachineData) et à son entrée de sauvegarde.</summary>
    public class PlacedObject : MonoBehaviour
    {
        public MachineData Data { get; private set; }
        public PlacedMachineData Entry { get; private set; }

        public void Init(MachineData data, PlacedMachineData entry)
        {
            Data = data;
            Entry = entry;
        }
    }
}
