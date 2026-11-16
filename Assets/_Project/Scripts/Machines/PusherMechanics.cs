using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>
    /// Mécanismes annexes du coin pusher, tous physiques (corps cinématiques) : plateau fixe qui vibre (les pièces se tassent à plat
    /// au lieu de s'empiler), deux pousseurs latéraux en opposition de phase, hélice centrale, voies « chance » éclairées au bord avant
    /// et roue bonus du mur du fond. Les mouvements ne tournent que lorsque la plaque est en marche.
    /// </summary>
    public class PusherMechanics : MonoBehaviour
    {
        public CoinPusherMachine machine;
        public Rigidbody tray, paddleL, paddleR, spinner;
        public Vector3 trayLocal, paddleLLocal, paddleRLocal, spinnerLocal;
        public Transform wheel;
        public Renderer[] laneLights;
        public Material laneOn, laneOff;
        public PusherMarquee marquee;
        public Light[] ambient;

        public const int Segments = 8;
        public const int Lanes = 5;
        public const float ShakeHz = 23f, ShakeAmp = 0.0015f, PaddleTravel = 0.14f, PaddlePeriod = 4.4f, SpinnerDegPerSec = 55f;

        public int LitLane { get; private set; } = 2;

        private float amp, phase, spinAngle, nextLane, showUntil, wheelAngle;
        private bool spinning; private float spinT, spinDur, spinFrom, spinTo; private System.Action spinDone;
        private Color[] ambientColors; private float[] ambientIntensity;

        public void Show(float seconds) => showUntil = Mathf.Max(showUntil, Time.time + seconds);

        /// <summary>Lance la roue : elle fait plusieurs tours puis s'arrête sur le segment demandé (sous le pointeur, en haut).</summary>
        public void SpinWheel(int segment, float duration, System.Action done)
        {
            if (wheel == null) { done?.Invoke(); return; }
            // le segment k occupe l'angle (k + 0,5) x 45° à partir de la droite ; le pointeur est à 90°
            float target = 90f - (segment + 0.5f) * (360f / Segments);
            while (target >= wheelAngle) target -= 360f;
            target -= 360f * 3f;
            spinFrom = wheelAngle; spinTo = target; spinT = 0f; spinDur = duration; spinDone = done; spinning = true;
        }

        private void Start()
        {
            if (ambient != null)
            {
                ambientColors = new Color[ambient.Length]; ambientIntensity = new float[ambient.Length];
                for (int i = 0; i < ambient.Length; i++) if (ambient[i] != null) { ambientColors[i] = ambient[i].color; ambientIntensity[i] = ambient[i].intensity; }
            }
            ApplyLaneLights();
        }

        private void ApplyLaneLights()
        {
            if (laneLights == null) return;
            for (int i = 0; i < laneLights.Length; i++)
                if (laneLights[i] != null) laneLights[i].sharedMaterial = i == LitLane ? laneOn : laneOff;
        }

        private void Update()
        {
            if (Time.time >= nextLane)
            {
                int n = Random.Range(0, Lanes);
                if (n == LitLane) n = (n + 1 + Random.Range(0, Lanes - 1)) % Lanes;
                LitLane = n;
                nextLane = Time.time + (machine != null && machine.TurboActive ? 0.9f : 1.6f);
                ApplyLaneLights();
            }

            if (spinning && wheel != null)
            {
                spinT += Time.deltaTime / spinDur;
                float t = Mathf.Clamp01(spinT);
                float k = 1f - Mathf.Pow(1f - t, 3f);
                wheelAngle = Mathf.Lerp(spinFrom, spinTo, k);
                wheel.localRotation = Quaternion.Euler(0f, 0f, wheelAngle);
                if (t >= 1f)
                {
                    spinning = false;
                    wheelAngle = Mathf.Repeat(wheelAngle, 360f);
                    wheel.localRotation = Quaternion.Euler(0f, 0f, wheelAngle);
                    var d = spinDone; spinDone = null; d?.Invoke();
                }
            }

            bool show = Time.time < showUntil;
            if (marquee != null) marquee.speed = show ? 18f : 7f;
            if (ambient != null && ambientColors != null)
                for (int i = 0; i < ambient.Length; i++)
                {
                    if (ambient[i] == null) continue;
                    if (show)
                    {
                        ambient[i].color = Color.HSVToRGB((Time.time * 1.2f + i * 0.35f) % 1f, 0.8f, 1f);
                        ambient[i].intensity = 4f + Mathf.Sin(Time.time * 18f + i) * 2.5f;
                    }
                    else { ambient[i].color = ambientColors[i]; ambient[i].intensity = ambientIntensity[i]; }
                }
        }

        private void FixedUpdate()
        {
            if (machine == null) return;
            float dt = Time.fixedDeltaTime;
            bool run = machine.PlateRunning;
            float turbo = machine.TurboFactor;
            amp = Mathf.MoveTowards(amp, run ? 1f : 0f, dt * 0.6f);
            if (amp > 0f) phase += dt * turbo;

            if (tray != null)
            {
                float off = run ? Mathf.Sin(Time.time * Mathf.PI * 2f * ShakeHz) * ShakeAmp : 0f;
                tray.MovePosition(transform.TransformPoint(trayLocal + Vector3.up * off));
            }
            float s = Mathf.Sin(phase * Mathf.PI * 2f / PaddlePeriod);
            if (paddleL != null) paddleL.MovePosition(transform.TransformPoint(paddleLLocal + Vector3.right * (amp * PaddleTravel * (0.5f + 0.5f * s))));
            if (paddleR != null) paddleR.MovePosition(transform.TransformPoint(paddleRLocal - Vector3.right * (amp * PaddleTravel * (0.5f - 0.5f * s))));
            if (spinner != null)
            {
                spinAngle += amp * SpinnerDegPerSec * turbo * dt;
                spinner.MovePosition(transform.TransformPoint(spinnerLocal));
                spinner.MoveRotation(transform.rotation * Quaternion.Euler(0f, spinAngle, 0f));
            }
        }
    }
}
