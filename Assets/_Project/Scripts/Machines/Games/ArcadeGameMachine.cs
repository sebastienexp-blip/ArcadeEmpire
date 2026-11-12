using ArcadeEmpire.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Base des machines à une touche (roue, machine à sous, marteau, basket, réflexes) : s'installer (E),
    /// quitter (Échap ou E), bouton animé, aide à l'écran. La mécanique est dans chaque sous-classe.
    /// </summary>
    public abstract class ArcadeGameMachine : MachineBase
    {
        [SerializeField] protected Transform dropButton;

        private int enterFrame;
        private float buttonPress;
        private Vector3 buttonBase;
        private bool hasButtonBase;

        protected int Cost => data != null ? (int)data.costPerPlay : 0;
        protected virtual string IdlePrompt => $"ESPACE : jouer ({Cost} pièces)";

        public void Configure(MachineData machineData, Transform anchor, Transform button)
        {
            data = machineData;
            cameraAnchor = anchor;
            dropButton = button;
        }

        protected virtual void Awake()
        {
            if (dropButton != null) { buttonBase = dropButton.localPosition; hasButtonBase = true; }
        }

        protected override void OnPlayerEntered()
        {
            enterFrame = Time.frameCount;
            StatusText = IdlePrompt;
        }

        protected override void OnPlayerExited() => StatusText = "";

        protected void PressButton() => buttonPress = 0.2f;

        protected virtual void Update()
        {
            if (IsInUse)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    bool exit = kb.escapeKey.wasPressedThisFrame || (kb.eKey.wasPressedThisFrame && Time.frameCount != enterFrame);
                    if (exit) { Exit(); return; }
                    HandleInput(kb);
                }
            }
            Tick();

            if (dropButton != null && hasButtonBase)
            {
                buttonPress = Mathf.Max(0f, buttonPress - Time.deltaTime);
                Vector3 p = dropButton.localPosition;
                p.y = Mathf.Lerp(p.y, buttonPress > 0f ? buttonBase.y - 0.012f : buttonBase.y, 1f - Mathf.Exp(-30f * Time.deltaTime));
                dropButton.localPosition = p;
            }
        }

        /// <summary>Entrées clavier pendant l'utilisation.</summary>
        protected abstract void HandleInput(Keyboard kb);

        /// <summary>Animation / logique exécutée à chaque image (même hors utilisation).</summary>
        protected virtual void Tick() { }

        protected override void OnPlayEnded() { }

        /// <summary>Message « fonds insuffisants » commun.</summary>
        protected void NotEnoughCoins() => StatusText = $"Il faut {Cost} pièces pour jouer.";
    }
}
