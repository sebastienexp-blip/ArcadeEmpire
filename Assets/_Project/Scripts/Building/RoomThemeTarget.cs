using UnityEngine;

namespace ArcadeEmpire.Building
{
    /// <summary>Marque un élément de la salle (sol, mur, néon, lumière) dont la couleur dépend de l'ambiance choisie.</summary>
    public class RoomThemeTarget : MonoBehaviour
    {
        public enum Role { FloorA, FloorB, Wall, Ceiling, NeonA, NeonB, NeonAccent, LightA, LightB }

        public Role role;
    }
}
