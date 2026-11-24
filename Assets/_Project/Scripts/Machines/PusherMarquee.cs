using UnityEngine;
using UnityEngine.UI;
using ArcadeEmpire.UI;

namespace ArcadeEmpire.Machines
{
    /// <summary>Guirlande d'ampoules du coin pusher : les ampoules s'allument à tour de rôle (chenillard), plus vite après un gain.</summary>
    public class PusherMarquee : MonoBehaviour
    {
        public Renderer[] bulbs;
        public Material on, off;
        public float speed = 7f;
        public int period = 3;

        private void Update()
        {
            if (bulbs == null || on == null || off == null) return;
            int step = Mathf.FloorToInt(Time.time * speed);
            for (int i = 0; i < bulbs.Length; i++)
                if (bulbs[i] != null) bulbs[i].sharedMaterial = ((i + step) % period == 0) ? on : off;
        }
    }
}
