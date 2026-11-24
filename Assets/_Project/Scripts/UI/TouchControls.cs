using ArcadeEmpire.Core;
using ArcadeEmpire.Machines;
using ArcadeEmpire.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace ArcadeEmpire.UI
{
    /// <summary>
    /// Commandes tactiles (mobile) : joystick analogique flottant à gauche, boutons d'action ronds à droite, façon jeu console.
    ///  - Joystick : marcher (poussé à fond = courir) ; dans une machine, il déplace la pince / choisit la fente.
    ///  - Gros bouton A : action (lâcher la pince, insérer une pièce).
    ///  - Bouton contextuel : « Jouer », « Vitrine », « Revendre », « Maison »… selon ce qui est devant le joueur.
    ///  - Retour (quitter la machine), Gains (coin pusher), Courir (bascule).
    /// Le reste de l'écran sert à tourner la caméra (glisser) et zoomer (pincer).
    /// Les boutons simulent les touches du clavier (OnScreenButton) : tout le jeu fonctionne sans code spécifique.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static bool ShowOnPc = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Application.isMobilePlatform && !ShowOnPc) return;
            if (FindFirstObjectByType<TouchControls>() != null) return;
            var go = new GameObject("TouchControls");
            DontDestroyOnLoad(go);
            go.AddComponent<TouchControls>();
        }

        private GameObject root, useBtn, backBtn, gainsBtn, runBtn, actionBtn;
        private Text useLabel, actionLabel;
        private Image useIcon, runImg;
        private MachineInteractor interactor;

        private static readonly Color BtnBg = new Color(0.08f, 0.07f, 0.2f, 0.62f);
        private static readonly Color RingCol = new Color(1f, 1f, 1f, 0.35f);

        private void Start()
        {
            UiKit.EnsureEventSystem();
            var canvas = UiKit.NewCanvas(transform, "TouchCanvas", 30);
            root = canvas.gameObject;
            var r = canvas.transform;

            // ---- joystick (zone d'appui : tout le quart bas gauche ; la base apparaît sous le doigt)
            var zone = new GameObject("JoystickZone", typeof(RectTransform), typeof(Image));
            zone.transform.SetParent(r, false);
            var zr = zone.GetComponent<RectTransform>();
            zr.anchorMin = Vector2.zero; zr.anchorMax = new Vector2(0.4f, 0.62f); zr.offsetMin = zr.offsetMax = Vector2.zero;
            zone.GetComponent<Image>().color = new Color(0, 0, 0, 0);   // invisible mais capte le doigt
            var stick = zone.AddComponent<VirtualJoystick>();
            stick.Build(zr);

            // ---- boutons d'action (bas droite)
            actionBtn = Round(r, "A", "<Keyboard>/space", new Vector2(-120, 130), 150, new Color(1f, 0.32f, 0.55f, 0.85f), out actionLabel, null);
            useBtn = Round(r, "JOUER", "<Keyboard>/e", new Vector2(-290, 92), 112, new Color(0.25f, 0.75f, 1f, 0.85f), out useLabel, "hand");
            useIcon = useBtn.transform.Find("Icon")?.GetComponent<Image>();
            backBtn = Round(r, "RETOUR", "<Keyboard>/escape", new Vector2(-290, 92), 104, new Color(0.9f, 0.3f, 0.3f, 0.85f), out _, "close");
            gainsBtn = Round(r, "GAINS", "<Keyboard>/r", new Vector2(-150, 290), 96, new Color(1f, 0.78f, 0.2f, 0.85f), out _, "coin");
            runBtn = Round(r, "COURIR", null, new Vector2(-280, 250), 88, BtnBg, out _, "run");
            runImg = runBtn.GetComponent<Image>();
            runBtn.AddComponent<Button>().onClick.AddListener(() => TouchInput.RunToggle = !TouchInput.RunToggle);
        }

        private void Update()
        {
            if (root == null) return;
            if (interactor == null) interactor = FindFirstObjectByType<MachineInteractor>();
            bool panel = UIState.PanelOpen;
            if (root.activeSelf == panel) root.SetActive(!panel);
            if (panel) { TouchInput.Feed(Vector2.zero); return; }

            MachineBase active = interactor != null ? interactor.Active : null;
            bool inMachine = active != null;
            string ctx = null, icon = "hand";
            if (!inMachine && interactor != null)
            {
                if (interactor.Nearest != null) { ctx = "JOUER"; icon = "play"; }
                else if (interactor.NearestPoint != null)
                {
                    switch (interactor.NearestPoint.kind)
                    {
                        case ArcadeEmpire.Building.InteractPoint.Kind.Showcase: ctx = "VITRINE"; icon = "shop"; break;
                        case ArcadeEmpire.Building.InteractPoint.Kind.Sell: ctx = "REVENDRE"; icon = "sell"; break;
                        case ArcadeEmpire.Building.InteractPoint.Kind.GoHome: ctx = "MAISON"; icon = "home"; break;
                        case ArcadeEmpire.Building.InteractPoint.Kind.GoArcade: ctx = "SALLE"; icon = "door"; break;
                        default: ctx = "OUVRIR"; icon = "bag"; break;
                    }
                }
            }
            useBtn.SetActive(ctx != null);
            if (ctx != null) { useLabel.text = ctx; if (useIcon != null) useIcon.sprite = UiTheme.Icon(icon); }
            backBtn.SetActive(inMachine);
            actionBtn.SetActive(inMachine);
            gainsBtn.SetActive(active is CoinPusherMachine);
            runBtn.SetActive(!inMachine);
            if (actionLabel != null) actionLabel.text = active is CoinPusherMachine ? "PIÈCE" : active is ClawMachine ? "PINCE" : "A";
            if (runImg != null) runImg.color = TouchInput.RunToggle ? new Color(0.3f, 0.88f, 0.5f, 0.9f) : BtnBg;
        }

        /// <summary>Bouton rond : disque coloré, anneau clair, icône et libellé. path = touche simulée (null = bouton UGUI simple).</summary>
        private static GameObject Round(Transform parent, string label, string path, Vector2 pos, float size, Color color, out Text text, string icon)
        {
            var go = new GameObject("Touch_" + label, typeof(RectTransform), typeof(Image));
            go.SetActive(false);   // le chemin de la touche doit être réglé avant l'activation
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.sprite = UiTheme.Circle; img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(size, size);
            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(go.transform, false);
            var ri = ring.GetComponent<Image>(); ri.sprite = UiTheme.Ring; ri.color = RingCol; ri.raycastTarget = false;
            var rr = ring.GetComponent<RectTransform>(); rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.offsetMin = rr.offsetMax = Vector2.zero;
            if (icon != null)
            {
                var ig = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                ig.transform.SetParent(go.transform, false);
                var ii = ig.GetComponent<Image>(); ii.sprite = UiTheme.Icon(icon); ii.raycastTarget = false; ii.preserveAspect = true;
                var ir = ig.GetComponent<RectTransform>(); ir.anchorMin = new Vector2(0.27f, 0.36f); ir.anchorMax = new Vector2(0.73f, 0.82f); ir.offsetMin = ir.offsetMax = Vector2.zero;
            }
            var t = new GameObject("Label", typeof(RectTransform), typeof(Text));
            t.transform.SetParent(go.transform, false);
            text = t.GetComponent<Text>();
            text.font = UiKit.Font; text.text = label; text.fontStyle = FontStyle.Bold; text.color = Color.white; text.raycastTarget = false;
            text.alignment = icon != null ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter;
            text.fontSize = icon != null ? Mathf.RoundToInt(size * 0.15f) : Mathf.RoundToInt(size * 0.3f);
            var sh = t.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(1.5f, -1.5f);
            var tr = t.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = icon != null ? new Vector2(4, size * 0.1f) : Vector2.zero; tr.offsetMax = Vector2.zero;
            go.AddComponent<TouchStyle>();
            if (path != null) { var o = go.AddComponent<OnScreenButton>(); o.controlPath = path; go.AddComponent<PressScale>(); }
            go.SetActive(true);
            return go;
        }

        /// <summary>Petit effet d'enfoncement du bouton.</summary>
        private sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public void OnPointerDown(PointerEventData e) { transform.localScale = Vector3.one * 0.9f; ArcadeEmpire.Core.SoundBank.Play2D("ui_click", 0.35f); }
            public void OnPointerUp(PointerEventData e) => transform.localScale = Vector3.one;
            private void OnDisable() => transform.localScale = Vector3.one;
        }

        /// <summary>Applique la taille et l'opacité choisies dans les options.</summary>
        private sealed class TouchStyle : MonoBehaviour
        {
            private Vector2 baseSize; private Vector2 basePos; private float baseAlpha; private bool init;
            private void OnEnable() { GameSettings.Changed += Apply; Apply(); }
            private void OnDisable() => GameSettings.Changed -= Apply;
            private void Apply()
            {
                var r = (RectTransform)transform;
                var img = GetComponent<Image>();
                if (!init) { baseSize = r.sizeDelta; basePos = r.anchoredPosition; baseAlpha = img.color.a; init = true; }
                r.sizeDelta = baseSize * GameSettings.TouchScale; r.anchoredPosition = basePos * GameSettings.TouchScale;
                var c = img.color; c.a = baseAlpha * Mathf.Clamp01(GameSettings.TouchOpacity / 0.55f); img.color = c;
            }
        }
    }

    /// <summary>
    /// Joystick virtuel flottant : la base se place là où le doigt se pose (dans sa zone), le bouton suit le doigt dans un rayon fixe.
    /// Écrit la direction dans TouchInput (analogique), revient au centre au relâchement.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform zone, baseRt, knob;
        private Image baseImg, knobImg;
        private Vector2 restPos = new Vector2(190f, 190f);
        private float radius = 95f;
        private bool held;
        private Vector2 value;

        public void Build(RectTransform z)
        {
            zone = z;
            var b = new GameObject("Base", typeof(RectTransform), typeof(Image));
            b.transform.SetParent(z, false);
            baseRt = b.GetComponent<RectTransform>();
            baseRt.anchorMin = baseRt.anchorMax = Vector2.zero; baseRt.pivot = new Vector2(0.5f, 0.5f); baseRt.sizeDelta = new Vector2(230, 230);
            baseImg = b.GetComponent<Image>(); baseImg.sprite = UiTheme.Circle; baseImg.color = new Color(0.06f, 0.05f, 0.16f, 0.45f); baseImg.raycastTarget = false;
            var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(b.transform, false);
            var ri = ring.GetComponent<Image>(); ri.sprite = UiTheme.Ring; ri.color = new Color(0.4f, 0.9f, 1f, 0.55f); ri.raycastTarget = false;
            var rr = ring.GetComponent<RectTransform>(); rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.offsetMin = rr.offsetMax = Vector2.zero;
            foreach (var (icon, pos) in new[] { ("up", new Vector2(0, 82)), ("down", new Vector2(0, -82)), ("left", new Vector2(-82, 0)), ("right", new Vector2(82, 0)) })
            {
                var a = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
                a.transform.SetParent(b.transform, false);
                var ai = a.GetComponent<Image>(); ai.sprite = UiTheme.Icon(icon); ai.color = new Color(1f, 1f, 1f, 0.4f); ai.raycastTarget = false;
                var ar = a.GetComponent<RectTransform>(); ar.sizeDelta = new Vector2(30, 30); ar.anchoredPosition = pos;
            }
            var k = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            k.transform.SetParent(b.transform, false);
            knob = k.GetComponent<RectTransform>(); knob.sizeDelta = new Vector2(104, 104);
            knobImg = k.GetComponent<Image>(); knobImg.sprite = UiTheme.Circle; knobImg.color = new Color(0.85f, 0.95f, 1f, 0.75f); knobImg.raycastTarget = false;
            var kr = new GameObject("KnobRing", typeof(RectTransform), typeof(Image));
            kr.transform.SetParent(k.transform, false);
            var kri = kr.GetComponent<Image>(); kri.sprite = UiTheme.Ring; kri.color = new Color(0.25f, 0.9f, 1f, 0.9f); kri.raycastTarget = false;
            var krr = kr.GetComponent<RectTransform>(); krr.anchorMin = Vector2.zero; krr.anchorMax = Vector2.one; krr.offsetMin = krr.offsetMax = Vector2.zero;
            baseRt.anchoredPosition = restPos;
        }

        private Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out Vector2 p);
            return p - zone.rect.min;   // origine en bas à gauche de la zone
        }

        public void OnPointerDown(PointerEventData e)
        {
            held = true;
            Vector2 p = Local(e);
            p.x = Mathf.Max(p.x, 120f); p.y = Mathf.Max(p.y, 120f);
            baseRt.anchoredPosition = p;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!held) return;
            Vector2 d = Local(e) - baseRt.anchoredPosition;
            float s = GameSettings.TouchScale;
            Vector2 c = Vector2.ClampMagnitude(d, radius * s);
            knob.anchoredPosition = c / s;   // le bouton est enfant de la base (déjà mise à l'échelle)
            Vector2 v = c / (radius * s);
            value = v.magnitude < 0.12f ? Vector2.zero : v;   // zone morte
        }

        public void OnPointerUp(PointerEventData e)
        {
            held = false; value = Vector2.zero; knob.anchoredPosition = Vector2.zero;
            baseRt.anchoredPosition = restPos;
        }

        private void OnDisable() { held = false; value = Vector2.zero; TouchInput.Feed(Vector2.zero); }

        private void Update()
        {
            TouchInput.Feed(value);
            float a = held ? 0.75f : 0.4f;
            var c = baseImg.color; c.a = Mathf.Lerp(c.a, held ? 0.6f : 0.35f, 0.2f); baseImg.color = c;
            var kc = knobImg.color; kc.a = a; knobImg.color = kc;
            baseRt.localScale = Vector3.one * GameSettings.TouchScale;
        }
    }
}
