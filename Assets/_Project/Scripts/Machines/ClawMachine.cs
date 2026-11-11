using System.Collections;
using ArcadeEmpire.Core;
using ArcadeEmpire.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Machine à pinces. Déroulé d'une partie : ESPACE (paiement) → déplacement de la pince (ZQSD/WASD/flèches)
    /// → ESPACE (descente) → fermeture → prise (probabilité selon la rareté et la force de la pince)
    /// → remontée → transport vers la trappe (la pince peut lâcher le lot) → ouverture → chute du lot → récompense.
    /// La tête de pince est un Rigidbody cinématique : ses branches poussent réellement les lots (physique).
    /// Les contrôles tactiles pourront piloter la machine via SetExternalMove / RequestDrop.
    /// </summary>
    public class ClawMachine : MachineBase
    {
        private enum Phase { Idle, Moving, Sequence }

        [Header("Références (assignées par le builder)")]
        [SerializeField] private Rigidbody head;
        [SerializeField] private Transform cable;
        [SerializeField] private Transform[] prongs;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private Transform joystickStick;
        [SerializeField] private Transform dropButton;

        [Header("Zone de jeu (repère local de la machine)")]
        [SerializeField] private Vector2 minXZ = new Vector2(-0.5f, -0.4f);
        [SerializeField] private Vector2 maxXZ = new Vector2(0.5f, 0.4f);
        [SerializeField] private Vector2 chuteXZ = new Vector2(-0.4f, -0.3f);
        [SerializeField] private float raisedY = 1.55f;
        [SerializeField] private float loweredY = 0.78f;
        [SerializeField] private float cableTopY = 1.72f;

        [Header("Mécanique")]
        [SerializeField] private float moveSpeed = 0.45f;
        [SerializeField] private float verticalSpeed = 0.4f;
        [SerializeField] private float returnSpeed = 0.5f;
        [SerializeField] private float openAngle = 38f;
        [SerializeField] private float closedAngle = -18f;
        [SerializeField] private float playTimeLimit = 25f;
        [Tooltip("Force de la pince (améliorable) : 1 = normale. Plus c'est haut, plus les prises réussissent et moins le lot glisse.")]
        [SerializeField] private float gripStrength = 1f;
        [SerializeField] private float grabRadius = 0.16f;

        public override string ControlsHint => "ZQSD / WASD / flèches : déplacer la pince      ESPACE : valider";
        public float TimeLeft => timeLeft;
        public bool IsMoving => phase == Phase.Moving;

        private Phase phase = Phase.Idle;
        private Vector3 headLocal;
        private float clawAngle;
        private PrizeItem held;
        private float timeLeft;
        private bool dropRequested;
        private int enterFrame;
        private Vector2 externalMove;
        private bool externalDrop;
        private Vector2 smoothedMove;
        private float buttonPress;
        private Vector3[] prongTangents;

        /// <summary>Appelée par le builder de l'éditeur pour câbler les références.</summary>
        public void Configure(MachineData machineData, Transform anchor, Rigidbody headBody, Transform cableT,
            Transform[] prongPivots, Transform hold, Transform stick, Transform button)
        {
            data = machineData;
            cameraAnchor = anchor;
            head = headBody;
            cable = cableT;
            prongs = prongPivots;
            holdPoint = hold;
            joystickStick = stick;
            dropButton = button;
        }

        public void SetExternalMove(Vector2 move) => externalMove = move;
        public void RequestDrop() => externalDrop = true;

        private void Awake()
        {
            headLocal = new Vector3(chuteXZ.x, raisedY, chuteXZ.y);
            clawAngle = openAngle;
            prongTangents = new Vector3[prongs != null ? prongs.Length : 0];
            for (int i = 0; i < prongTangents.Length; i++)
            {
                Vector3 radial = new Vector3(prongs[i].localPosition.x, 0f, prongs[i].localPosition.z).normalized;
                prongTangents[i] = new Vector3(-radial.z, 0f, radial.x);
            }
            if (head != null) head.transform.position = transform.TransformPoint(headLocal);
            ApplyVisuals(Vector2.zero);
        }

        protected override void OnPlayerEntered()
        {
            enterFrame = Time.frameCount;
            StatusText = $"ESPACE : jouer ({data.costPerPlay} pièces)   —   Échap : quitter";
        }

        protected override void OnPlayerExited() => StatusText = "";

        protected override void OnPlayStarted()
        {
            phase = Phase.Moving;
            timeLeft = playTimeLimit;
            dropRequested = false;
            Sfx.Play(Sfx.Kind.Coin, transform.position);
        }

        protected override void OnPlayEnded()
        {
            phase = Phase.Idle;
            if (IsInUse) StatusText = $"ESPACE : rejouer ({data.costPerPlay} pièces)   —   Échap : quitter";
        }

        private void Update()
        {
            Vector2 move = Vector2.zero;
            if (IsInUse) move = HandleInput();

            if (phase == Phase.Moving)
            {
                headLocal.x = Mathf.Clamp(headLocal.x + move.x * moveSpeed * Time.deltaTime, minXZ.x, maxXZ.x);
                headLocal.z = Mathf.Clamp(headLocal.z + move.y * moveSpeed * Time.deltaTime, minXZ.y, maxXZ.y);
                timeLeft -= Time.deltaTime;
                StatusText = $"Déplace la pince — ESPACE pour descendre   ({Mathf.CeilToInt(Mathf.Max(0f, timeLeft))} s)";
                if (dropRequested || timeLeft <= 0f) StartCoroutine(DropSequence());
            }
            ApplyVisuals(move);
        }

        private Vector2 HandleInput()
        {
            Keyboard kb = Keyboard.current;
            Vector2 move = externalMove;
            bool action = externalDrop;
            bool exit = false;
            externalDrop = false;

            if (kb != null)
            {
                bool left = kb.aKey.isPressed || kb.qKey.isPressed || kb.leftArrowKey.isPressed;
                bool right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                bool fwd = kb.wKey.isPressed || kb.zKey.isPressed || kb.upArrowKey.isPressed;
                bool back = kb.sKey.isPressed || kb.downArrowKey.isPressed;
                move += new Vector2((right ? 1f : 0f) - (left ? 1f : 0f), (fwd ? 1f : 0f) - (back ? 1f : 0f));
                action |= kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                exit = kb.escapeKey.wasPressedThisFrame || (kb.eKey.wasPressedThisFrame && Time.frameCount != enterFrame);
            }
            move = Vector2.ClampMagnitude(move, 1f);

            if (exit && !IsPlaying)
            {
                Exit();
                return Vector2.zero;
            }
            if (action)
            {
                buttonPress = 0.25f;
                if (phase == Phase.Idle)
                {
                    if (!TryStartPlay()) StatusText = "Pas assez de pièces !";
                }
                else if (phase == Phase.Moving)
                {
                    dropRequested = true;
                }
            }
            return phase == Phase.Moving ? move : Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (head == null) return;
            head.MovePosition(transform.TransformPoint(headLocal));
            head.MoveRotation(transform.rotation);
        }

        private void ApplyVisuals(Vector2 move)
        {
            if (prongs != null)
                for (int i = 0; i < prongs.Length; i++)
                    prongs[i].localRotation = Quaternion.AngleAxis(clawAngle, prongTangents[i]);

            if (cable != null && head != null)
            {
                Vector3 h = transform.InverseTransformPoint(head.transform.position);
                float len = Mathf.Max(0.01f, cableTopY - h.y);
                cable.localPosition = new Vector3(h.x, (cableTopY + h.y) * 0.5f, h.z);
                cable.localRotation = Quaternion.identity;
                cable.localScale = new Vector3(0.012f, len * 0.5f, 0.012f);
            }

            smoothedMove = Vector2.Lerp(smoothedMove, move, 1f - Mathf.Exp(-14f * Time.deltaTime));
            if (joystickStick != null)
                joystickStick.localRotation = Quaternion.Euler(smoothedMove.y * 18f, 0f, -smoothedMove.x * 18f);

            if (dropButton != null)
            {
                buttonPress = Mathf.Max(0f, buttonPress - Time.deltaTime);
                Vector3 p = dropButton.localPosition;
                p.y = Mathf.Lerp(p.y, buttonPress > 0f ? buttonBaseY - 0.012f : buttonBaseY, 1f - Mathf.Exp(-30f * Time.deltaTime));
                dropButton.localPosition = p;
            }
        }

        private float buttonBaseY => 0.665f;

        // --------------------------------------------------------------------------------------------
        // Séquence automatique après appui sur le bouton
        // --------------------------------------------------------------------------------------------

        private IEnumerator DropSequence()
        {
            phase = Phase.Sequence;
            dropRequested = false;

            StatusText = "Descente…";
            Sfx.Play(Sfx.Kind.Drop, transform.position);
            yield return MoveHeadTo(new Vector3(headLocal.x, loweredY, headLocal.z), verticalSpeed);

            StatusText = "Prise…";
            yield return AnimateClaw(closedAngle, 0.7f);
            TryGrab();

            yield return MoveHeadTo(new Vector3(headLocal.x, raisedY, headLocal.z), held != null ? verticalSpeed * 0.8f : verticalSpeed);
            MaybeSlip("Le lot a glissé pendant la remontée !");

            // Transport vers la trappe, avec un deuxième risque de chute à mi-chemin
            StatusText = "Direction la trappe…";
            Vector3 chute = new Vector3(chuteXZ.x, raisedY, chuteXZ.y);
            Vector3 mid = Vector3.Lerp(headLocal, chute, 0.5f);
            yield return MoveHeadTo(mid, returnSpeed);
            MaybeSlip("Le lot a glissé en route !");
            yield return MoveHeadTo(chute, returnSpeed);

            // Ouverture : le lot tombe dans la trappe
            yield return AnimateClaw(openAngle, 0.6f);
            if (held != null)
            {
                held.Release();
                held = null;
            }
            yield return new WaitForSeconds(1.6f);   // laisse le temps au lot de tomber et d'être récupéré

            if (StatusText == "Direction la trappe…" || StatusText == "Prise…" || StatusText == "Descente…")
            {
                StatusText = "Perdu… retente ta chance !";
                Sfx.Play(Sfx.Kind.Miss, transform.position);
            }
            EndPlay();
        }

        private IEnumerator MoveHeadTo(Vector3 target, float speed)
        {
            while ((headLocal - target).sqrMagnitude > 0.000004f)
            {
                headLocal = Vector3.MoveTowards(headLocal, target, speed * Time.deltaTime);
                yield return null;
            }
            headLocal = target;
        }

        private IEnumerator AnimateClaw(float targetAngle, float duration)
        {
            float start = clawAngle, t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                clawAngle = Mathf.Lerp(start, targetAngle, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            clawAngle = targetAngle;
        }

        private void TryGrab()
        {
            Collider[] hits = Physics.OverlapSphere(holdPoint.position, grabRadius, ~0, QueryTriggerInteraction.Ignore);
            PrizeItem best = null;
            float bestDist = float.MaxValue;
            foreach (Collider c in hits)
            {
                PrizeItem p = c.GetComponentInParent<PrizeItem>();
                if (p == null || !p.IsFree) continue;
                float d = (p.transform.position - holdPoint.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = p; }
            }

            if (best == null)
            {
                StatusText = "Rien sous la pince…";
                return;
            }

            float chance = Mathf.Clamp(GrabBase(best.reward) * gripStrength, 0.05f, 0.97f);
            if (Random.value <= chance)
            {
                best.Attach(holdPoint);
                held = best;
                Sfx.Play(Sfx.Kind.Grab, transform.position);
                StatusText = "Attrapé !";
            }
            else
            {
                StatusText = "Raté, le lot t'a échappé !";
                Sfx.Play(Sfx.Kind.Miss, transform.position);
            }
        }

        private void MaybeSlip(string message)
        {
            if (held == null) return;
            float chance = Mathf.Clamp01(0.14f * RarityFactor(held.reward) * Mathf.Max(0.1f, 1.6f - gripStrength));
            if (Random.value < chance)
            {
                held.Release();
                held = null;
                StatusText = message;
                Sfx.Play(Sfx.Kind.Miss, transform.position);
            }
        }

        private static float GrabBase(RewardData r)
        {
            if (r == null) return 0.7f;
            switch (r.rarity)
            {
                case Rarity.Common: return 0.85f;
                case Rarity.Rare: return 0.65f;
                case Rarity.Epic: return 0.5f;
                default: return 0.35f;
            }
        }

        private static float RarityFactor(RewardData r)
        {
            if (r == null) return 1f;
            switch (r.rarity)
            {
                case Rarity.Common: return 0.8f;
                case Rarity.Rare: return 1f;
                case Rarity.Epic: return 1.3f;
                default: return 1.7f;
            }
        }

        /// <summary>Appelée par la zone de récupération quand un lot libre tombe dans la trappe.</summary>
        public void OnPrizeCollected(PrizeItem prize)
        {
            if (prize == null || !prize.IsFree) return;
            prize.Collected = true;
            GrantReward(prize.reward);
            Sfx.Play(Sfx.Kind.Win, transform.position);
            StatusText = $"GAGNÉ : {prize.reward.displayName} ({RarityName(prize.reward.rarity)}) !   +{prize.reward.ticketValue} tickets";
            Destroy(prize.gameObject);
        }

        public static string RarityName(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return "Commun";
                case Rarity.Rare: return "Rare";
                case Rarity.Epic: return "Épique";
                default: return "Légendaire";
            }
        }
    }
}
