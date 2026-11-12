using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Réflexes : 6 pavés numérotés 1 à 6 (touches 1 à 6 du clavier ou du pavé numérique). Pendant 20 s un pavé s'allume ;
    /// il faut appuyer sur sa touche avant qu'il ne s'éteigne (le délai raccourcit à chaque réussite). Gain : tickets selon les touches réussies.
    /// </summary>
    public class ReflexMachine : ArcadeGameMachine
    {
        [SerializeField] private Renderer[] pads;
        [SerializeField] private Material[] onMaterials;
        [SerializeField] private Material[] offMaterials;
        [SerializeField] private Material rimMaterial;

        private const float RoundTime = 20f;
        private float timeLeft;
        private int lit = -1;
        private float litUntil;
        private float nextLight;
        private int hits;
        private int misses;
        private float window;
        private float lastStatus;
        private readonly System.Random random = new System.Random();

        public override string ControlsHint => "Touches 1 à 6 : appuie sur le pavé allumé      Échap : quitter";
        protected override string IdlePrompt => $"ESPACE : jouer ({Cost} pièces) — 20 s, touches 1 à 6";

        public void Setup(Renderer[] padRenderers, Material[] on, Material[] off, Material rim)
        {
            pads = padRenderers; onMaterials = on; offMaterials = off; rimMaterial = rim;
        }

        public static int TicketsFor(int hits) => hits / 2 + (hits >= 15 ? 5 : 0);

        protected override void HandleInput(Keyboard kb)
        {
            if (!IsPlaying)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                {
                    PressButton();
                    if (!TryStartPlay()) NotEnoughCoins();
                }
                return;
            }
            int pressed = PressedPad(kb);
            if (pressed < 0) return;
            if (pressed == lit)
            {
                hits++;
                window = Mathf.Max(0.45f, window - 0.025f);
                SetLit(-1);
                nextLight = Time.time + 0.12f;
                Sfx.Play(Sfx.Kind.Coin, transform.position, 0.35f);
            }
            else
            {
                misses++;
                Sfx.Play(Sfx.Kind.Miss, transform.position, 0.3f);
            }
        }

        private static int PressedPad(Keyboard kb)
        {
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return 0;
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return 1;
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return 2;
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) return 3;
            if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) return 4;
            if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) return 5;
            return -1;
        }

        protected override void OnPlayStarted()
        {
            timeLeft = RoundTime; hits = 0; misses = 0; window = 1.2f; lit = -1; nextLight = Time.time + 0.5f;
        }

        protected override void OnPlayEnded()
        {
            SetLit(-1);
            if (timeLeft <= 0f)
            {
                int tickets = TicketsFor(hits);
                Payout(tickets, 0, 4 + hits);
                StatusText = $"{hits} réussites  +{tickets} tickets";
                Sfx.Play(tickets > 0 ? Sfx.Kind.Win : Sfx.Kind.Miss, transform.position, 0.7f);
            }
        }

        protected override void Tick()
        {
            if (!IsPlaying) return;
            timeLeft -= Time.deltaTime;
            if (lit >= 0 && Time.time > litUntil) { misses++; SetLit(-1); nextLight = Time.time + 0.2f; }
            if (lit < 0 && Time.time >= nextLight)
            {
                int next;
                do { next = random.Next(6); } while (next == lastLit);
                SetLit(next);
                litUntil = Time.time + window;
            }
            if (Time.time - lastStatus > 0.1f)
            {
                lastStatus = Time.time;
                StatusText = $"{Mathf.CeilToInt(Mathf.Max(0f, timeLeft))} s    réussites {hits}    ratés {misses}";
            }
            if (timeLeft <= 0f) EndPlay();
        }

        private int lastLit = -1;

        private void SetLit(int index)
        {
            if (index >= 0) lastLit = index;
            lit = index;
            if (pads == null) return;
            for (int i = 0; i < pads.Length; i++)
                if (pads[i] != null)
                    pads[i].sharedMaterials = new[] { rimMaterial, i == index ? onMaterials[i] : offMaterials[i] };
        }
    }
}
