using System.Collections.Generic;
using ArcadeEmpire.Core;
using ArcadeEmpire.Data;
using ArcadeEmpire.Economy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Coin pusher. Le joueur vise (gauche/droite) et lâche des pièces (ESPACE, 1 pièce chacune) sur un plateau
    /// où un poussoir cinématique avance et recule. Les pièces et lots poussés par-dessus le bord avant tombent
    /// dans la zone de récupération : chaque pièce rapporte 1 pièce, chaque lot donne sa récompense.
    /// Physique réelle (Rigidbody, collisions) ; les pièces sont recyclées via un pool.
    /// Le poussoir ne bouge que pendant l'utilisation, pour qu'on ne gagne rien sans jouer.
    /// </summary>
    public class CoinPusherMachine : MachineBase
    {
        [Header("Références (assignées par le builder)")]
        [SerializeField] private Rigidbody pusher;
        [SerializeField] private Transform aimIndicator;
        [SerializeField] private Transform dropButton;
        [SerializeField] private Transform coinTemplate;
        [SerializeField] private Transform coinRoot;

        [Header("Géométrie (repère local)")]
        [SerializeField] private float aimMinX = -0.5f;
        [SerializeField] private float aimMaxX = 0.5f;
        [SerializeField] private float dropY = 1.45f;
        [SerializeField] private float dropZ = 0.25f;
        [SerializeField] private float plateY = 0.96f;
        [SerializeField] private float plateRetractedZ = 0.70f;
        [SerializeField] private float plateExtendedZ = 0.20f;
        [SerializeField] private float killY = 0.3f;

        [Header("Réglages")]
        [SerializeField] private float aimSpeed = 0.6f;
        [SerializeField] private float pushPeriod = 3.2f;
        [SerializeField] private float dropCooldown = 0.3f;

        public override string ControlsHint => "Q/D ou ←/→ : viser      ESPACE : lâcher une pièce (1 pièce)";

        private readonly List<PusherCoin> active = new List<PusherCoin>();
        private readonly Stack<PusherCoin> pool = new Stack<PusherCoin>();
        private float aimX;
        private float pushTime;
        private float nextDropTime;
        private float lastSfx;
        private float buttonPress;
        private int enterFrame;
        private int wonThisSession;
        private float killTimer;

        public int CoinsOnTray => active.Count;

        public void Configure(MachineData machineData, Transform anchor, Rigidbody pusherBody, Transform aim,
            Transform button, Transform template, Transform root)
        {
            data = machineData;
            cameraAnchor = anchor;
            pusher = pusherBody;
            aimIndicator = aim;
            dropButton = button;
            coinTemplate = template;
            coinRoot = root;
        }

        private void Awake()
        {
            if (coinRoot != null)
                foreach (PusherCoin c in coinRoot.GetComponentsInChildren<PusherCoin>(false))
                    active.Add(c);
            if (pusher != null) pusher.transform.position = transform.TransformPoint(0f, plateY, plateRetractedZ);
        }

        protected override void OnPlayerEntered()
        {
            enterFrame = Time.frameCount;
            wonThisSession = 0;
            RefreshStatus();
        }

        protected override void OnPlayerExited() => StatusText = "";

        protected override void OnPlayStarted()
        {
            SpawnCoin();
            Sfx.Play(Sfx.Kind.Coin, transform.position, 0.5f);
            EndPlay();   // chaque pièce lâchée est une « partie » instantanée
        }

        private void Update()
        {
            if (IsInUse) HandleInput();

            // visuels : indicateur de visée, bouton
            if (aimIndicator != null)
            {
                Vector3 p = aimIndicator.localPosition;
                p.x = aimX;
                aimIndicator.localPosition = p;
            }
            if (dropButton != null)
            {
                buttonPress = Mathf.Max(0f, buttonPress - Time.deltaTime);
                Vector3 p = dropButton.localPosition;
                p.y = Mathf.Lerp(p.y, buttonPress > 0f ? 0.653f : 0.665f, 1f - Mathf.Exp(-30f * Time.deltaTime));
                dropButton.localPosition = p;
            }

            if (IsInUse) pushTime += Time.deltaTime;

            // filet de sécurité : une pièce tombée hors de la machine est recyclée sans gain
            killTimer += Time.deltaTime;
            if (killTimer > 0.5f)
            {
                killTimer = 0f;
                for (int i = active.Count - 1; i >= 0; i--)
                    if (active[i] != null && transform.InverseTransformPoint(active[i].transform.position).y < killY)
                        Recycle(active[i]);
            }
        }

        private void HandleInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            bool left = kb.aKey.isPressed || kb.qKey.isPressed || kb.leftArrowKey.isPressed;
            bool right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
            aimX = Mathf.Clamp(aimX + ((right ? 1f : 0f) - (left ? 1f : 0f)) * aimSpeed * Time.deltaTime, aimMinX, aimMaxX);

            bool exit = kb.escapeKey.wasPressedThisFrame || (kb.eKey.wasPressedThisFrame && Time.frameCount != enterFrame);
            if (exit)
            {
                Exit();
                return;
            }

            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            {
                buttonPress = 0.2f;
                if (Time.time < nextDropTime) return;
                if (TryStartPlay()) nextDropTime = Time.time + dropCooldown;
                else StatusText = "Plus de pièces !";
            }
        }

        private void FixedUpdate()
        {
            if (pusher == null) return;
            float k = (1f - Mathf.Cos(2f * Mathf.PI * pushTime / pushPeriod)) * 0.5f;   // 0 = rentré, 1 = sorti
            float z = Mathf.Lerp(plateRetractedZ, plateExtendedZ, k);
            pusher.MovePosition(transform.TransformPoint(0f, plateY, z));
            pusher.MoveRotation(transform.rotation);
        }

        private void SpawnCoin()
        {
            PusherCoin coin;
            if (pool.Count > 0) coin = pool.Pop();
            else coin = Instantiate(coinTemplate.gameObject, coinRoot).GetComponent<PusherCoin>();

            coin.Collected = false;
            coin.transform.SetPositionAndRotation(
                transform.TransformPoint(aimX, dropY, dropZ),
                transform.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            var rb = coin.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            coin.gameObject.SetActive(true);
            active.Add(coin);
            RefreshStatus();
        }

        private void Recycle(PusherCoin coin)
        {
            active.Remove(coin);
            coin.gameObject.SetActive(false);
            pool.Push(coin);
        }

        /// <summary>Une pièce est tombée dans la zone de récupération : +1 pièce et +1 XP.</summary>
        public void OnCoinCollected(PusherCoin coin)
        {
            if (coin == null || coin.Collected) return;
            coin.Collected = true;
            Recycle(coin);
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.Add(CurrencyType.Coins, 1);
                CurrencyManager.Instance.Add(CurrencyType.Experience, 1);
            }
            wonThisSession++;
            if (Time.time - lastSfx > 0.08f)
            {
                lastSfx = Time.time;
                Sfx.Play(Sfx.Kind.Coin, transform.position, 0.4f);
            }
            RefreshStatus();
        }

        /// <summary>Un lot spécial (jeton) est tombé : récompense, tickets et XP.</summary>
        public void OnPrizeCollected(PrizeItem prize)
        {
            if (prize == null || !prize.IsFree) return;
            prize.Collected = true;
            GrantReward(prize.reward);
            Sfx.Play(Sfx.Kind.Win, transform.position);
            StatusText = $"GAGNÉ : {prize.reward.displayName} ({ClawMachine.RarityName(prize.reward.rarity)}) !   +{prize.reward.ticketValue} tickets";
            Destroy(prize.gameObject);
        }

        private void RefreshStatus()
        {
            if (IsInUse)
                StatusText = $"Pièces gagnées : {wonThisSession}   —   sur le plateau : {active.Count}   —   Échap : quitter";
        }
    }
}
