using UnityEngine;

namespace ArcadeEmpire.Core
{
    /// <summary>
    /// Entrées tactiles analogiques partagées (joystick virtuel) : lues par le personnage et par les machines.
    /// </summary>
    public static class TouchInput
    {
        /// <summary>Direction du joystick (x = droite, y = avant), longueur 0..1.</summary>
        public static Vector2 Move;
        /// <summary>Course : joystick poussé à fond ou bouton « courir » actif.</summary>
        public static bool Run;
        public static bool RunToggle;

        private static int flick, flickFrame = -10;
        private static bool flickArmed = true;

        /// <summary>Appelé par le joystick à chaque image : détecte les « coups » gauche / droite (choix de la fente du coin pusher).</summary>
        public static void Feed(Vector2 v)
        {
            Move = v;
            Run = RunToggle || v.magnitude > 0.92f;
            if (flickArmed && Mathf.Abs(v.x) > 0.6f && Mathf.Abs(v.x) > Mathf.Abs(v.y))
            {
                flick = v.x > 0 ? 1 : -1; flickFrame = Time.frameCount; flickArmed = false;
            }
            if (Mathf.Abs(v.x) < 0.3f) flickArmed = true;
        }

        /// <summary>-1 / +1 une seule fois par coup de joystick vers la gauche / la droite, sinon 0.</summary>
        public static int ConsumeFlick()
        {
            if (flick == 0 || Time.frameCount - flickFrame > 2) { flick = 0; return 0; }
            int f = flick; flick = 0; return f;
        }
    }
}
