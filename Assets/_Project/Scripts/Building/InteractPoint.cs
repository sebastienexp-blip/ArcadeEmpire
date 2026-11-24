using System.Collections.Generic;
using ArcadeEmpire.Player;
using ArcadeEmpire.UI;
using UnityEngine;

namespace ArcadeEmpire.Building
{
    /// <summary>
    /// Point d'interaction du décor (comptoir de la vitrine, boutique de revente, téléporteur maison/salle, coffre à cadenas).
    /// Le joueur s'approche : l'invite s'affiche ; bouton d'action (E) : l'action se déclenche.
    /// </summary>
    public class InteractPoint : MonoBehaviour
    {
        public enum Kind { Showcase, Sell, GoHome, GoArcade, Inventory }

        public Kind kind;
        public string label = "Interagir";
        public Transform destination;
        public float range = 1.9f;

        public static readonly List<InteractPoint> All = new List<InteractPoint>();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public void Use(PlayerController player)
        {
            switch (kind)
            {
                case Kind.Showcase: UIState.Request("shop"); break;
                case Kind.Sell: UIState.Request("sell"); break;
                case Kind.Inventory: UIState.Request("inventory"); break;
                case Kind.GoHome:
                case Kind.GoArcade:
                    if (player != null && destination != null)
                    {
                        Color c = kind == Kind.GoHome ? new Color(0.5f, 0.75f, 1f) : new Color(1f, 0.5f, 0.8f);
                        ArcadeEmpire.Core.Fx.TeleportBurst(player.transform.position, c);
                        Transform dst = destination;
                        ScreenFader.Instance.FadeThrough(() =>
                        {
                            player.TeleportTo(dst.position, dst.rotation);
                            ArcadeEmpire.Core.Fx.TeleportBurst(dst.position, c);
                        });
                    }
                    break;
            }
        }

        public static InteractPoint FindNearest(Vector3 pos, out float dist)
        {
            InteractPoint best = null; dist = float.MaxValue;
            foreach (var p in All)
            {
                if (p == null) continue;
                Vector3 d = pos - p.transform.position; d.y = 0f;
                float m = d.magnitude;
                if (m < p.range && m < dist) { dist = m; best = p; }
            }
            return best;
        }
    }
}
