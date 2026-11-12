using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Stacker rétro : grille 7 colonnes x 12 rangées. Un bloc de 3 cases va et vient sur la rangée courante ;
    /// ESPACE le pose. Seules les cases posées au-dessus de cases de la rangée précédente sont conservées.
    /// Plus aucune case = fin de partie. Gain selon la hauteur atteinte (12 rangées = jackpot).
    /// </summary>
    public class StackerMachine : ArcadeGameMachine
    {
        [SerializeField] private Transform[] cells;   // rangée * 7 + colonne

        public const int Cols = 7, Rows = 12;
        public static readonly int[] TicketsByRows = { 0, 0, 1, 1, 2, 3, 4, 6, 8, 11, 15, 20, 30 };

        private bool[,] placed;
        private int row, pos, dir, width, prevLeft, prevWidth;
        private float stepTimer;
        private bool over;
        private float overTimer;

        public override string ControlsHint => "ESPACE : poser le bloc      Échap : quitter";

        public void Setup(Transform[] grid) => cells = grid;

        protected override void Awake() { base.Awake(); placed = new bool[Rows, Cols]; Refresh(); }

        protected override void HandleInput(Keyboard kb)
        {
            bool press = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
            if (!press) return;
            PressButton();
            if (!IsPlaying)
            {
                if (!TryStartPlay()) NotEnoughCoins();
                return;
            }
            if (over) return;
            Drop();
        }

        protected override void OnPlayStarted()
        {
            placed = new bool[Rows, Cols];
            row = 0; width = 3; pos = 0; dir = 1; prevLeft = 0; prevWidth = Cols; over = false; stepTimer = 0f;
            StatusText = "Rangée 1 / 12";
        }

        protected override void OnPlayEnded() { }

        private float StepTime => Mathf.Max(0.05f, 0.2f - row * 0.013f);

        private void Drop()
        {
            if (row == 0) { prevLeft = pos; prevWidth = width; }
            else
            {
                int left = Mathf.Max(pos, prevLeft), right = Mathf.Min(pos + width - 1, prevLeft + prevWidth - 1);
                if (right < left) { Finish(); return; }
                pos = left; width = right - left + 1;
                prevLeft = left; prevWidth = width;
            }
            for (int c = pos; c < pos + width; c++) placed[row, c] = true;
            Sfx.Play(Sfx.Kind.Drop, transform.position, 0.4f);
            row++;
            if (row >= Rows) { Finish(); return; }
            if (row == 4 || row == 8) width = Mathf.Min(width, row == 4 ? 2 : 1);   // la largeur maximale diminue en montant
            pos = Mathf.Clamp(pos, 0, Cols - width); dir = 1; stepTimer = 0f;
            StatusText = $"Rangée {row + 1} / {Rows}";
        }

        private void Finish()
        {
            over = true; overTimer = 1.5f;
            int tickets = TicketsByRows[Mathf.Clamp(row, 0, Rows)];
            Payout(tickets, 0, 3 + row * 2);
            StatusText = row >= Rows ? $"JACKPOT !  +{tickets} tickets" : $"{row} rangées  +{tickets} tickets";
            Sfx.Play(row >= Rows ? Sfx.Kind.Win : (tickets > 0 ? Sfx.Kind.Coin : Sfx.Kind.Miss), transform.position, 0.7f);
        }

        protected override void Tick()
        {
            if (placed == null) return;
            if (IsPlaying && !over)
            {
                stepTimer += Time.deltaTime;
                while (stepTimer >= StepTime)
                {
                    stepTimer -= StepTime;
                    pos += dir;
                    if (pos < 0) { pos = 1; dir = 1; }
                    else if (pos + width > Cols) { pos = Cols - width - 1; dir = -1; if (pos < 0) pos = 0; }
                }
            }
            if (over)
            {
                overTimer -= Time.deltaTime;
                if (overTimer <= 0f) { over = false; EndPlay(); }
            }
            Refresh();
        }

        private void Refresh()
        {
            if (cells == null) return;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    int i = r * Cols + c;
                    if (i >= cells.Length || cells[i] == null) continue;
                    bool on = placed != null && placed[r, c];
                    if (!on && IsPlaying && !over && r == row && c >= pos && c < pos + width) on = true;
                    cells[i].gameObject.SetActive(on);
                }
        }
    }
}
