using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>Bonus posé sur le plateau : quand il tombe dans le bac, l'effet se déclenche (pluie de pièces, gains doublés, tickets, gemme, faux billet en plastique).</summary>
    public class PusherBonus : MonoBehaviour
    {
        public enum Kind { CoinRain, DoubleGains, TicketBurst, Gem, Cash }

        public Kind kind;
        /// <summary>Valeur en pièces d'un billet (Kind.Cash).</summary>
        public int value = 20;
        public bool Collected { get; set; }

        public static string Label(Kind k)
        {
            switch (k)
            {
                case Kind.CoinRain: return "PLUIE DE PIÈCES";
                case Kind.DoubleGains: return "GAINS x2";
                case Kind.TicketBurst: return "BONUS TICKETS";
                case Kind.Cash: return "BILLET";
                default: return "GEMME PREMIUM";
            }
        }

        public static string ShortLabel(Kind k, int value)
        {
            switch (k)
            {
                case Kind.CoinRain: return "PLUIE";
                case Kind.DoubleGains: return "GAINS x2";
                case Kind.TicketBurst: return "TICKETS";
                case Kind.Cash: return "+" + value;
                default: return "GEMME";
            }
        }
    }
}
