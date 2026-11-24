using UnityEngine;

namespace ArcadeEmpire.Player
{
    /// <summary>
    /// Animation procédurale du personnage, pilotée par la vitesse réelle : cycle de marche dont la longueur de pas suit la vitesse
    /// (pas de glissement), levée du pied, bascule du bassin, torsion du buste compensée par la tête, inclinaison dans les
    /// accélérations et les virages, respiration et déhanchement à l'arrêt. Devant une machine, le personnage se tourne vers elle
    /// et lève les bras vers les commandes.
    /// </summary>
    public class PlayerVisualAnimator : MonoBehaviour
    {
        [SerializeField] private Transform armLeft, armRight, legLeft, legRight, body;
        private Transform head, foreL, foreR, shinL, shinR;
        private float idleTimer, idleAction, idleActionT = -1f;
        private PlayerController pc;
        private MachineInteractor interactor;

        private static readonly Vector3 Hip = new Vector3(0f, 0.92f, 0f);
        private float phase, amount, run01;
        private float lean, roll, twist;
        private float armRaise;
        private Vector3 prevVelocity, accel;
        private Vector3 legLBase, legRBase;

        public void Configure(Transform aL, Transform aR, Transform lL, Transform lR, Transform bodyRoot)
        {
            armLeft = aL; armRight = aR; legLeft = lL; legRight = lR; body = bodyRoot;
        }

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
            if (body != null) head = body.Find("Head");
            foreL = armLeft != null ? armLeft.Find("Forearm") : null; foreR = armRight != null ? armRight.Find("Forearm") : null;
            shinL = legLeft != null ? legLeft.Find("Shin") : null; shinR = legRight != null ? legRight.Find("Shin") : null;
            if (legLeft != null) legLBase = legLeft.localPosition;
            if (legRight != null) legRBase = legRight.localPosition;
        }

        private void Start() => interactor = FindFirstObjectByType<MachineInteractor>();

        private void Update()
        {
            float dt = Time.deltaTime;
            if (pc == null || dt <= 0f) return;

            Vector3 v = pc.PlanarVelocity;
            float speed = v.magnitude;
            Vector3 a = (v - prevVelocity) / dt;
            prevVelocity = v;
            accel = Vector3.Lerp(accel, a, 1f - Mathf.Exp(-10f * dt));
            float accelForward = Vector3.Dot(accel, transform.forward);

            amount = Mathf.MoveTowards(amount, Mathf.Clamp01(speed / 1.5f), dt * 7f);
            run01 = Mathf.Clamp01((speed - 3.2f) / Mathf.Max(0.1f, pc.RunSpeed - 3.2f));

            // Longueur de pas : 0,62 m en marchant, 0,95 m en courant -> la fréquence suit la vitesse, les pieds ne glissent pas
            float step = Mathf.Lerp(0.62f, 0.95f, run01);
            phase += Mathf.PI * speed / step * dt;
            if (phase > 1000f) phase -= 1000f * 1f;

            float swing = Mathf.Lerp(24f, 48f, run01) * amount;
            float s = Mathf.Sin(phase) * swing;

            // Machine : on se tourne vers elle et on lève les bras
            bool using_ = interactor != null && interactor.Active != null;
            if (using_)
            {
                Vector3 to = interactor.Active.transform.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f && speed < 0.3f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to, Vector3.up), 360f * dt);
            }
            armRaise = Mathf.MoveTowards(armRaise, using_ ? 1f : 0f, dt * 3f);
            float t = Time.time;
            float idle = 1f - amount;

            // Jambes : balancement + levée du pied quand la jambe passe vers l'avant
            Swing(legLeft, s); Swing(legRight, -s);
            // genoux : la jambe qui revient vers l'avant plie le genou (pied levé), l'autre reste presque tendue (appui)
            float kneeMax = Mathf.Lerp(42f, 85f, run01);
            float liftL = Mathf.Max(0f, -Mathf.Cos(phase)), liftR = Mathf.Max(0f, Mathf.Cos(phase));
            float kneeIdle = 3f + Mathf.Max(0f, Mathf.Sin(t * 0.5f)) * 4f * idle;   // léger déhanché à l'arrêt
            Swing(shinL, (6f + kneeMax * liftL * liftL) * amount + kneeIdle);
            Swing(shinR, (6f + kneeMax * liftR * liftR) * amount + 3f);
            if (legLeft != null) legLeft.localPosition = legLBase;
            if (legRight != null) legRight.localPosition = legRBase;

            // Bras : balancement opposé aux jambes, plus mou à l'arrêt, levés vers la machine
            float armSwing = -s * 0.85f + Mathf.Sin(t * 1.3f) * 1.5f * idle;
            float raised = -68f + Mathf.Sin(t * 2.1f) * 2f;
            // petites actions à l'arrêt : s'étirer, regarder autour, taper du pied
            if (amount < 0.05f && !using_)
            {
                idleTimer += dt;
                if (idleActionT < 0f && idleTimer > 6f) { idleActionT = 0f; idleAction = Random.value; idleTimer = 0f; }
            }
            else { idleTimer = 0f; idleActionT = -1f; }
            float stretch = 0f, look = 0f;
            if (idleActionT >= 0f)
            {
                idleActionT += dt;
                float w = Mathf.Sin(Mathf.Clamp01(idleActionT / 2.2f) * Mathf.PI);
                if (idleAction < 0.4f) stretch = w; else look = w * (idleAction < 0.7f ? 1f : -1f);
                if (idleActionT > 2.2f) idleActionT = -1f;
            }
            float armL = Mathf.Lerp(armSwing, raised, armRaise), armR = Mathf.Lerp(-armSwing, raised, armRaise);
            armL = Mathf.Lerp(armL, -165f, stretch); armR = Mathf.Lerp(armR, -165f, stretch);
            Swing(armLeft, armL); Swing(armRight, armR);
            if (armLeft != null) armLeft.localRotation *= Quaternion.Euler(0f, 0f, -(4f + 6f * run01) * (1f - stretch));   // bras un peu écartés du corps
            if (armRight != null) armRight.localRotation *= Quaternion.Euler(0f, 0f, (4f + 6f * run01) * (1f - stretch));
            // coudes : pliés en marchant, beaucoup en courant, plus encore vers les commandes de la machine
            float elbowBase = Mathf.Lerp(12f, 18f, amount) + 55f * run01;
            float elbowL = elbowBase + Mathf.Max(0f, -s) * 0.5f, elbowR = elbowBase + Mathf.Max(0f, s) * 0.5f;
            Swing(foreL, -Mathf.Lerp(Mathf.Lerp(elbowL, 45f, armRaise), 10f, stretch));
            Swing(foreR, -Mathf.Lerp(Mathf.Lerp(elbowR, 45f, armRaise), 10f, stretch));

            // Buste : inclinaison selon l'accélération et la vitesse, roulis dans les virages, torsion du bassin
            float targetLean = Mathf.Clamp(accelForward * 0.35f, -5f, 7f) + run01 * 6f + amount * 2f;
            float targetRoll = Mathf.Clamp(-pc.YawRate * 0.03f * Mathf.Clamp01(speed / 3f), -7f, 7f);
            float targetTwist = Mathf.Sin(phase) * 5f * amount;
            float k = 1f - Mathf.Exp(-10f * dt);
            lean = Mathf.Lerp(lean, targetLean, k); roll = Mathf.Lerp(roll, targetRoll, k); twist = Mathf.Lerp(twist, targetTwist, k);

            if (body != null)
            {
                Quaternion q = Quaternion.Euler(lean, twist, roll);
                float bob = Mathf.Abs(Mathf.Sin(phase)) * Mathf.Lerp(0.025f, 0.045f, run01) * amount;
                float sway = Mathf.Sin(phase) * 0.015f * amount + Mathf.Sin(t * 0.45f) * 0.008f * idle;
                float breathe = 1f + Mathf.Sin(t * 1.7f) * 0.006f * idle;
                body.localRotation = q;
                // rotation autour des hanches (et non des pieds) pour que le corps se penche naturellement
                body.localPosition = Hip - q * Hip + new Vector3(sway, bob, 0f);
                body.localScale = new Vector3(1f, breathe, 1f);
                if (head != null)
                    head.localRotation = Quaternion.Euler(-lean * 0.6f + Mathf.Sin(t * 0.7f) * 1.2f * idle - stretch * 12f, -twist * 0.8f + Mathf.Sin(t * 0.33f) * 3f * idle + look * 55f, -roll * 0.7f);
            }
        }

        private static void Swing(Transform tr, float degrees)
        {
            if (tr != null) tr.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}
