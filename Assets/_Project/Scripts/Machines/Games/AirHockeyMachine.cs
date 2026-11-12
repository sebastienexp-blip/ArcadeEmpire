using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Air hockey contre une IA. Physique 2D simple et stable (sous-pas) : rebonds sur les bandes, collision élastique
    /// palet/maillet qui transmet la vitesse du maillet, buts de 0,34 m. Premier à 5 buts ou meilleur score au bout de 2 min.
    /// Victoire : 10 tickets, égalité : 4, défaite : 1. L'IA a une vitesse limitée et une erreur de visée : elle est battable.
    /// </summary>
    public class AirHockeyMachine : ArcadeGameMachine
    {
        [SerializeField] private Transform puck;
        [SerializeField] private Transform playerMallet;
        [SerializeField] private Transform aiMallet;
        [SerializeField] private float surfaceY = 0.801f;

        private const float HalfW = 0.55f, HalfL = 0.65f, GoalHalf = 0.17f;
        private const float PuckR = 0.04f, MalletR = 0.06f, MaxPuckSpeed = 3.4f;
        private const int Substeps = 4;
        private const float MatchTime = 120f;
        private const int GoalsToWin = 5;

        private Vector2 puckPos, puckVel, pPos, aPos, pVel, aVel;
        private int scorePlayer, scoreAi;
        private float timeLeft;
        private float serveTimer;
        private float aiOffsetX, aiOffsetTimer;
        private float lastStatus;
        private readonly System.Random random = new System.Random();

        public override string ControlsHint => "ZQSD / flèches : déplacer le maillet      Échap : quitter";

        public void Setup(Transform puckT, Transform playerM, Transform aiM, float y)
        {
            puck = puckT; playerMallet = playerM; aiMallet = aiM; surfaceY = y;
        }

        private void Start() => ResetPositions(0);

        protected override void HandleInput(Keyboard kb)
        {
            if (IsPlaying) return;
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            {
                PressButton();
                if (!TryStartPlay()) NotEnoughCoins();
            }
        }

        protected override void OnPlayStarted()
        {
            scorePlayer = 0; scoreAi = 0; timeLeft = MatchTime;
            ResetPositions(0);
            serveTimer = 1.0f;
            Sfx.Play(Sfx.Kind.Coin, transform.position, 0.5f);
        }

        protected override void OnPlayEnded()
        {
            if (timeLeft <= 0f || scorePlayer >= GoalsToWin || scoreAi >= GoalsToWin)
            {
                int tickets; int xp; string msg;
                if (scorePlayer > scoreAi) { tickets = 10; xp = 20; msg = "VICTOIRE !"; Sfx.Play(Sfx.Kind.Win, transform.position, 0.8f); }
                else if (scorePlayer == scoreAi) { tickets = 4; xp = 8; msg = "Égalité"; Sfx.Play(Sfx.Kind.Coin, transform.position, 0.6f); }
                else { tickets = 1; xp = 3; msg = "Défaite"; Sfx.Play(Sfx.Kind.Miss, transform.position, 0.6f); }
                Payout(tickets, 0, xp);
                StatusText = $"{msg}  {scorePlayer} - {scoreAi}  +{tickets} tickets";
            }
        }

        private void ResetPositions(int servingSide)
        {
            pPos = new Vector2(0f, -HalfL + 0.18f);
            aPos = new Vector2(0f, HalfL - 0.18f);
            pVel = aVel = Vector2.zero;
            // servingSide : -1 palet côté joueur, +1 côté IA, 0 centre
            puckPos = new Vector2(0f, servingSide * 0.3f);
            puckVel = Vector2.zero;
        }

        protected override void Tick()
        {
            if (IsPlaying)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.033f);
                timeLeft -= dt;
                if (serveTimer > 0f)
                {
                    serveTimer -= dt;
                    if (serveTimer <= 0f && puckVel.sqrMagnitude < 0.0001f)
                        puckVel = new Vector2(((float)random.NextDouble() - 0.5f) * 0.6f, (random.Next(2) == 0 ? -1f : 1f) * 0.8f);
                }
                float sub = dt / Substeps;
                for (int i = 0; i < Substeps; i++) Step(sub);

                if (Time.time - lastStatus > 0.1f)
                {
                    lastStatus = Time.time;
                    int t = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
                    StatusText = $"Toi {scorePlayer} - {scoreAi} IA     {t / 60}:{t % 60:00}";
                }
                if (timeLeft <= 0f || scorePlayer >= GoalsToWin || scoreAi >= GoalsToWin) EndPlay();
            }
            ApplyVisuals();
        }

        private void Step(float dt)
        {
            // maillet du joueur
            Keyboard kb = Keyboard.current;
            Vector2 move = Vector2.zero;
            if (kb != null && IsInUse)
            {
                if (kb.aKey.isPressed || kb.qKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.wKey.isPressed || kb.zKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            }
            Vector2 prevP = pPos;
            pPos += move.normalized * 1.8f * dt;
            pPos.x = Mathf.Clamp(pPos.x, -HalfW + MalletR, HalfW - MalletR);
            pPos.y = Mathf.Clamp(pPos.y, -HalfL + MalletR, -0.03f - MalletR);
            pVel = (pPos - prevP) / dt;

            // IA : suit le palet avec une vitesse limitée et une erreur de visée
            aiOffsetTimer -= dt;
            if (aiOffsetTimer <= 0f) { aiOffsetTimer = 0.4f; aiOffsetX = ((float)random.NextDouble() - 0.5f) * 0.14f; }
            Vector2 target;
            if (puckPos.y > 0.02f && puckVel.y < 1.2f) target = new Vector2(puckPos.x + aiOffsetX, puckPos.y + PuckR + MalletR * 0.4f);   // attaque
            else target = new Vector2(puckPos.x * 0.7f + aiOffsetX, HalfL - 0.2f);                                                         // défense
            target.x = Mathf.Clamp(target.x, -HalfW + MalletR, HalfW - MalletR);
            target.y = Mathf.Clamp(target.y, 0.03f + MalletR, HalfL - MalletR);
            Vector2 prevA = aPos;
            aPos = Vector2.MoveTowards(aPos, target, 1.1f * dt);
            aVel = (aPos - prevA) / dt;

            // palet
            puckPos += puckVel * dt;
            puckVel *= 1f - 0.3f * dt;
            CollideMallet(pPos, pVel);
            CollideMallet(aPos, aVel);
            float speed = puckVel.magnitude;
            if (speed > MaxPuckSpeed) puckVel *= MaxPuckSpeed / speed;

            // bandes latérales
            if (puckPos.x < -HalfW + PuckR) { puckPos.x = -HalfW + PuckR; puckVel.x = Mathf.Abs(puckVel.x) * 0.95f; }
            if (puckPos.x > HalfW - PuckR) { puckPos.x = HalfW - PuckR; puckVel.x = -Mathf.Abs(puckVel.x) * 0.95f; }

            // extrémités : but ou rebond
            bool inGoal = Mathf.Abs(puckPos.x) < GoalHalf;
            if (puckPos.y > HalfL - PuckR)
            {
                if (inGoal && puckPos.y > HalfL) Goal(true);
                else if (!inGoal) { puckPos.y = HalfL - PuckR; puckVel.y = -Mathf.Abs(puckVel.y) * 0.95f; }
            }
            else if (puckPos.y < -HalfL + PuckR)
            {
                if (inGoal && puckPos.y < -HalfL) Goal(false);
                else if (!inGoal) { puckPos.y = -HalfL + PuckR; puckVel.y = Mathf.Abs(puckVel.y) * 0.95f; }
            }
        }

        private void CollideMallet(Vector2 mallet, Vector2 malletVel)
        {
            Vector2 d = puckPos - mallet;
            float dist = d.magnitude;
            float min = PuckR + MalletR;
            if (dist >= min || dist < 0.0001f) return;
            Vector2 n = d / dist;
            puckPos = mallet + n * min;
            Vector2 rel = puckVel - malletVel;
            float vn = Vector2.Dot(rel, n);
            if (vn < 0f) puckVel -= (1f + 0.9f) * vn * n;
            if (puckVel.magnitude < 0.5f) puckVel = n * 0.5f;   // jamais collé au maillet
            Sfx.Play(Sfx.Kind.Drop, transform.position, 0.25f);
        }

        private void Goal(bool playerScored)
        {
            if (playerScored) scorePlayer++; else scoreAi++;
            Sfx.Play(playerScored ? Sfx.Kind.Coin : Sfx.Kind.Miss, transform.position, 0.6f);
            ResetPositions(playerScored ? 1 : -1);   // le palet est servi du côté de celui qui a encaissé
            serveTimer = 1.0f;
        }

        private void ApplyVisuals()
        {
            if (puck != null) puck.localPosition = new Vector3(puckPos.x, surfaceY, puckPos.y);
            if (playerMallet != null) playerMallet.localPosition = new Vector3(pPos.x, surfaceY, pPos.y);
            if (aiMallet != null) aiMallet.localPosition = new Vector3(aPos.x, surfaceY, aPos.y);
        }
    }
}
