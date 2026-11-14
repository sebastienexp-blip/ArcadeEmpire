using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>Jeton bonus posé sur le plateau : quand il tombe dans le bac, l'effet se déclenche (pluie de pièces, gains doublés, tickets, gemme premium).</summary>
    public class PusherBonus : MonoBehaviour
    {
        public enum Kind { CoinRain, DoubleGains, TicketBurst, Gem }

        public Kind kind;
        public bool Collected { get; set; }

        public static string Label(Kind k)
        {
            switch (k)
            {
                case Kind.CoinRain: return "PLUIE DE PIÈCES";
                case Kind.DoubleGains: return "GAINS x2";
                case Kind.TicketBurst: return "BONUS TICKETS";
                default: return "GEMME PREMIUM";
            }
        }
    }
}
