namespace ArcadeEmpire.UI
{
    /// <summary>État partagé de l'interface : un seul panneau plein écran ouvert à la fois, et demandes d'ouverture venant des boutons.</summary>
    public static class UIState
    {
        private static bool panelOpen;
        public static int LastClosedFrame = -10;

        public static bool PanelOpen
        {
            get => panelOpen;
            set { if (panelOpen && !value) LastClosedFrame = UnityEngine.Time.frameCount; panelOpen = value; }
        }

        /// <summary>À appeler quand quelque chose vient de se fermer avec Échap (machine quittée) : le menu pause ne doit pas s'ouvrir dans la même image.</summary>
        public static void MarkClosed() => LastClosedFrame = UnityEngine.Time.frameCount;
        /// <summary>Vrai quand le menu pause / options est ouvert (le temps est figé).</summary>
        public static bool Paused;

        private static string request;
        private static int requestFrame;

        /// <summary>Demande l'ouverture d'un panneau ("inventory", "shop", "build", "options"), par exemple depuis un bouton du HUD.</summary>
        public static void Request(string panel) { request = panel; requestFrame = UnityEngine.Time.frameCount; }

        /// <summary>Retourne vrai (une seule fois) si ce panneau a été demandé.</summary>
        public static bool Consume(string panel)
        {
            if (request != panel) return false;
            if (UnityEngine.Time.frameCount - requestFrame > 3) { request = null; return false; }
            request = null;
            return true;
        }
    }
}
