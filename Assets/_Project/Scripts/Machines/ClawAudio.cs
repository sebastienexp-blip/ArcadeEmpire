using System;
using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>Sons de la machine à pinces générés par le code : moteurs du portique, claquement des branches, peluche qui tombe, bip du chrono, jingle de victoire.</summary>
    public static class ClawAudio
    {
        public enum Id { Whine, Grip, Slip, Plush, Beep, Fanfare, Fail }

        private const int Rate = 44100;
        private static readonly AudioClip[] cache = new AudioClip[7];

        public static AudioClip Get(Id id)
        {
            int i = (int)id;
            if (cache[i] != null) return cache[i];
            var rng = new System.Random(300 + i);
            switch (id)
            {
                case Id.Whine:   // petit moteur de treuil : boucle de 1 s, fréquences entières
                    cache[i] = Make("claw_whine", 1f, t => (Mathf.Sin(6.2832f * 310f * t) * 0.35f + Mathf.Sin(6.2832f * 620f * t) * 0.18f + Mathf.Sin(6.2832f * 41f * t) * 0.25f) * (0.8f + 0.2f * Mathf.Sin(6.2832f * 7f * t)) * 0.5f);
                    break;
                case Id.Grip:    // branches qui se referment : choc métallique sec
                    cache[i] = Make("claw_grip", 0.22f, t => (Mathf.Sin(6.2832f * 1250f * t) * 0.5f + Mathf.Sin(6.2832f * 2900f * t) * 0.3f) * Mathf.Exp(-t * 28f) + ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 70f) * 0.35f);
                    break;
                case Id.Slip:    // le lot glisse : frottement qui s'éteint
                    cache[i] = Make("claw_slip", 0.35f, t => ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 9f) * 0.25f * Mathf.Sin(6.2832f * 28f * t));
                    break;
                case Id.Plush:   // peluche sur peluche : bruit sourd et doux
                    cache[i] = Make("claw_plush", 0.16f, t => (Mathf.Sin(6.2832f * (110f - 160f * t) * t) * 0.7f + ((float)rng.NextDouble() * 2f - 1f) * 0.1f) * Mathf.Exp(-t * 30f));
                    break;
                case Id.Beep:    // chrono
                    cache[i] = Make("claw_beep", 0.14f, t => Mathf.Sin(6.2832f * 1480f * t) * Mathf.Exp(-t * 18f) * 0.3f);
                    break;
                case Id.Fanfare: // victoire : arpège majeur
                    cache[i] = Make("claw_fanfare", 1.0f, t =>
                    {
                        float[] n = { 523f, 659f, 784f, 1047f, 1319f };
                        int k = Mathf.Min(4, (int)(t / 0.12f));
                        float l = t - k * 0.12f;
                        float v = Mathf.Sin(6.2832f * n[k] * t) * Mathf.Exp(-l * 6f) * 0.35f;
                        if (t > 0.6f) v += (Mathf.Sin(6.2832f * 1047f * t) + Mathf.Sin(6.2832f * 1319f * t) + Mathf.Sin(6.2832f * 1568f * t)) * Mathf.Exp(-(t - 0.6f) * 5f) * 0.12f;
                        return v;
                    });
                    break;
                default:         // échec : deux notes descendantes
                    cache[i] = Make("claw_fail", 0.6f, t => Mathf.Sin(6.2832f * (t < 0.25f ? 330f : 247f) * t) * Mathf.Exp(-(t < 0.25f ? t : t - 0.25f) * 9f) * 0.3f);
                    break;
            }
            return cache[i];
        }

        private static AudioClip Make(string name, float duration, Func<float, float> sample)
        {
            int n = Mathf.CeilToInt(Rate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
