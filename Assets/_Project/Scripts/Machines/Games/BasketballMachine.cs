using System.Collections.Generic;
using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Machines.Games
{
    /// <summary>
    /// Basket : une partie dure 30 s. Une jauge de puissance oscille en permanence ; ESPACE lance un ballon (physique réelle).
    /// La vitesse idéale est calculée pour passer au centre de l'anneau (angle fixe) ; la puissance la module de -30 % à +30 %.
    /// Un panier = le ballon traverse la zone de l'anneau en descendant : 2 tickets, +1 à partir du 3e panier d'affilée.
    /// </summary>
    public class BasketballMachine : ArcadeGameMachine
    {
        [SerializeField] private Transform launchPoint;
        [SerializeField] private Transform hoopCenter;
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private float launchAngle = 70f;

        private const float RoundTime = 30f;
        private const float ShotCooldown = 0.5f;
        private const float MeterPeriod = 1.1f;

        private readonly List<Rigidbody> balls = new List<Rigidbody>();
        private readonly List<float> spawnTimes = new List<float>();
        private float timeLeft;
        private float meter;
        private float nextShot;
        private int baskets;
        private int streak;
        private int tickets;
        private float lastStatus;

        public override string ControlsHint => "ESPACE : lancer (vise le milieu de la jauge)      Échap : quitter";

        public void Setup(Transform launch, Transform hoop, GameObject ball)
        {
            launchPoint = launch; hoopCenter = hoop; ballPrefab = ball;
        }

        /// <summary>Vitesse initiale qui amène la balle exactement au centre de l'anneau (tir parabolique, angle fixe).</summary>
        public static float IdealSpeed(Vector3 from, Vector3 to, float angleDeg)
        {
            Vector3 d = to - from;
            float horizontal = new Vector2(d.x, d.z).magnitude;
            float a = angleDeg * Mathf.Deg2Rad;
            float denom = 2f * Mathf.Cos(a) * Mathf.Cos(a) * (horizontal * Mathf.Tan(a) - d.y);
            if (denom <= 0.0001f) return 5f;
            return Mathf.Sqrt(-Physics.gravity.y * horizontal * horizontal / denom);
        }

        protected override void HandleInput(Keyboard kb)
        {
            if (!(kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return;
            PressButton();
            if (!IsPlaying) { if (!TryStartPlay()) NotEnoughCoins(); return; }
            if (Time.time >= nextShot) { Shoot(); nextShot = Time.time + ShotCooldown; }
        }

        protected override void OnPlayStarted()
        {
            timeLeft = RoundTime; baskets = 0; streak = 0; tickets = 0; nextShot = 0f;
        }

        protected override void OnPlayEnded()
        {
            if (baskets >= 0 && timeLeft <= 0f)
            {
                Payout(tickets, 0, 5 + baskets * 3);
                StatusText = $"Temps écoulé : {baskets} paniers  +{tickets} tickets";
                Sfx.Play(baskets > 0 ? Sfx.Kind.Win : Sfx.Kind.Miss, transform.position, 0.7f);
            }
        }

        private void Shoot()
        {
            if (launchPoint == null || hoopCenter == null || ballPrefab == null) return;
            float ideal = IdealSpeed(launchPoint.position, hoopCenter.position, launchAngle);
            float speed = ideal * (0.7f + 0.6f * meter);
            Vector3 flat = hoopCenter.position - launchPoint.position; flat.y = 0f; flat.Normalize();
            Vector3 dir = Quaternion.AngleAxis(-launchAngle, Vector3.Cross(Vector3.up, flat)) * flat;
            var go = Instantiate(ballPrefab, launchPoint.position, Quaternion.identity, transform);
            go.SetActive(true);
            var rb = go.GetComponent<Rigidbody>();
            rb.linearVelocity = dir * speed;
            rb.angularVelocity = Random.insideUnitSphere * 2f;
            balls.Add(rb); spawnTimes.Add(Time.time);
            Sfx.Play(Sfx.Kind.Grab, transform.position, 0.4f);
        }

        /// <summary>Appelé par la zone de l'anneau quand un ballon la traverse.</summary>
        public void RegisterBasket(Rigidbody ball)
        {
            if (!IsPlaying || ball == null || ball.linearVelocity.y >= 0f) return;
            if (ball.GetComponent<BasketBall>() is BasketBall b) { if (b.Scored) return; b.Scored = true; }
            baskets++; streak++;
            tickets += 2 + (streak >= 3 ? 1 : 0);
            Sfx.Play(Sfx.Kind.Coin, transform.position, 0.8f);
        }

        protected override void Tick()
        {
            if (IsPlaying)
            {
                timeLeft -= Time.deltaTime;
                meter = Mathf.PingPong(Time.time * 2f / MeterPeriod, 1f);
                if (Time.time - lastStatus > 0.05f)
                {
                    lastStatus = Time.time;
                    int filled = Mathf.RoundToInt(meter * 20f);
                    StatusText = $"{Mathf.CeilToInt(Mathf.Max(0f, timeLeft))} s   paniers {baskets}   [" + new string('#', filled) + new string('-', 20 - filled) + "]";
                }
                if (timeLeft <= 0f) EndPlay();
            }
            for (int i = balls.Count - 1; i >= 0; i--)
            {
                if (balls[i] == null) { balls.RemoveAt(i); spawnTimes.RemoveAt(i); continue; }
                if (Time.time - spawnTimes[i] > 3.2f)
                {
                    Destroy(balls[i].gameObject);
                    balls.RemoveAt(i); spawnTimes.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>Marque un ballon pour ne compter qu'un seul panier par tir.</summary>
    public class BasketBall : MonoBehaviour
    {
        public bool Scored;
    }

    /// <summary>Zone d'anneau : prévient la machine quand un ballon la traverse.</summary>
    public class BasketHoopZone : MonoBehaviour
    {
        [SerializeField] private BasketballMachine machine;
        public void Configure(BasketballMachine m) => machine = m;

        private void OnTriggerEnter(Collider other)
        {
            if (machine != null && other.attachedRigidbody != null && other.GetComponent<BasketBall>() != null)
                machine.RegisterBasket(other.attachedRigidbody);
        }
    }
}
