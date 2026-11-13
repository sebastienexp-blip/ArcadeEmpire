using ArcadeEmpire.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeEmpire.Core
{
    /// <summary>
    /// Musique d'ambiance de la salle : boucle de 16 s générée par le code (basse, arpège, batterie légère sur la
    /// progression Am - F - C - G). Se crée toute seule au démarrage. Touches : M musique on/off, - et + volume général.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        private const int Rate = 32000;
        private const float Bpm = 118f;

        private AudioSource source;
        private bool muted;
        private float hudUntil;
        private string hud = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<MusicPlayer>() != null) return;
            var go = new GameObject("MusicPlayer");
            DontDestroyOnLoad(go);
            go.AddComponent<MusicPlayer>();
        }

        private void Start()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = BuildLoop();
            source.loop = true;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.volume = GameAudio.EffectiveMusic * 0.6f;
            source.Play();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.mKey.wasPressedThisFrame) { muted = !muted; Show(muted ? "Musique coupée" : "Musique activée"); }
                if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) { GameAudio.Master -= 0.1f; Show($"Volume {Mathf.RoundToInt(GameAudio.Master * 100)} %"); }
                if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) { GameAudio.Master += 0.1f; Show($"Volume {Mathf.RoundToInt(GameAudio.Master * 100)} %"); }
            }
            if (source != null) source.volume = muted ? 0f : GameAudio.EffectiveMusic * 0.6f;
        }

        private void Show(string text) { hud = text; hudUntil = Time.unscaledTime + 1.5f; }

        private void OnGUI()
        {
            if (Time.unscaledTime > hudUntil) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(Screen.width - 320, 10, 310, 30), hud, style);
        }

        private static AudioClip BuildLoop()
        {
            float beat = 60f / Bpm;
            int bars = 8;
            float length = bars * 4 * beat;
            int n = Mathf.RoundToInt(length * Rate);
            var data = new float[n];

            // fondamentales (Hz) et tierces/quintes pour Am, F, C, G
            float[][] chords =
            {
                new[] { 110.00f, 261.63f, 329.63f, 440.00f },   // Am : A2, C4, E4, A4
                new[] { 87.31f, 261.63f, 349.23f, 440.00f },    // F : F2, C4, F4, A4
                new[] { 130.81f, 261.63f, 329.63f, 392.00f },   // C : C3, C4, E4, G4
                new[] { 98.00f, 246.94f, 293.66f, 392.00f },    // G : G2, B3, D4, G4
            };

            int stepsPerBar = 16;
            float step = beat / 4f;
            var rng = new System.Random(7);
            for (int s = 0; s < bars * stepsPerBar; s++)
            {
                float[] ch = chords[(s / (stepsPerBar * 2)) % 4];
                float t0 = s * step;
                int start = Mathf.RoundToInt(t0 * Rate);
                int stepInBeat = s % 4;

                // arpège (onde carrée douce) sur chaque double croche
                float[] arp = { ch[1], ch[2], ch[3], ch[2] };
                AddNote(data, start, step * 0.9f, arp[(s / 1) % 4], 0.07f, true, 9f);
                // basse sur chaque croche
                if (s % 2 == 0) AddNote(data, start, step * 1.8f, ch[0], 0.16f, false, 6f);
                // grosse caisse sur chaque temps
                if (stepInBeat == 0) AddKick(data, start, 0.22f);
                // charleston sur les contretemps
                if (stepInBeat == 2) AddHat(data, start, 0.03f, rng);
                // caisse claire sur 2 et 4
                if (stepInBeat == 0 && (s / 4) % 2 == 1) AddHat(data, start, 0.10f, rng, 0.07f);
            }

            var clip = AudioClip.Create("music_loop", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void AddNote(float[] data, int start, float dur, float freq, float amp, bool square, float decay)
        {
            int len = Mathf.RoundToInt(dur * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                if (idx >= data.Length) break;
                float t = i / (float)Rate;
                float ph = (t * freq) % 1f;
                float wave = square ? (ph < 0.5f ? 1f : -1f) * 0.6f + Mathf.Sin(2f * Mathf.PI * ph) * 0.4f : Mathf.Sin(2f * Mathf.PI * ph) + 0.3f * Mathf.Sin(4f * Mathf.PI * ph);
                float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-t * decay);
                data[idx] += wave * env * amp;
            }
        }

        private static void AddKick(float[] data, int start, float dur)
        {
            int len = Mathf.RoundToInt(dur * Rate);
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                if (idx >= data.Length) break;
                float t = i / (float)Rate;
                float f = 45f + 110f * Mathf.Exp(-t * 28f);
                phase += f / Rate;
                data[idx] += Mathf.Sin(2f * Mathf.PI * phase) * Mathf.Exp(-t * 14f) * 0.3f;
            }
        }

        private static void AddHat(float[] data, int start, float dur, System.Random rng, float amp = 0.05f)
        {
            int len = Mathf.RoundToInt(dur * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                if (idx >= data.Length) break;
                float t = i / (float)Rate;
                data[idx] += ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * (dur > 0.05f ? 25f : 90f)) * amp;
            }
        }
    }
}
