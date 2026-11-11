using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>Marqueur d'une pièce du coin pusher (objet physique réutilisé via un pool).</summary>
    public class PusherCoin : MonoBehaviour
    {
        public bool Collected { get; set; }
    }
}
