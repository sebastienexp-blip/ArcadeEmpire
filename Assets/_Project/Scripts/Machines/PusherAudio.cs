using System;
using UnityEngine;

namespace ArcadeEmpire.Machines
{
    /// <summary>Sons du coin pusher générés par le code : cliquetis de pièces, bruit sourd, insertion, gain, claquement de la plaque, moteur.</summary>
    public static class PusherAudio
    {
        public enum Id { Clink, Thud, Insert, Collect, Payout, Clank, Motor }

        private const int Rate = 44100;
        private static readonly AudioClip[] cache = new AudioClip[7];

        public static AudioClip Get(Id id)
        {
            int i = (int)id;
            if (cache[i] != null) return cache[i];
            string file = id == Id.Insert ? "coin_insert" : id == Id.Payout ? "coin_drop" : id == Id.Motor ? "pusher_motor" : null;
            var real = ArcadeEmpire.Core.SoundBank.Get(file);
            if (real != null) return cache[i] = real;
            var rng = new System.Random(100 + i);
            switch (id)
            {
                case Id.Clink:   // deux pièces métalliques : modes inharmoniques qui s'éteignent vite
                    cache[i] = Make("pc_clink", 0.22f, t => (Mathf.Sin(6.2832f * 2930f * t) + 0.7f * Mathf.Sin(6.2832f * 4170f * t) + 0.5f * Mathf.Sin(6.2832f * 6380f * t)) * Mathf.Exp(-t * 34f) * 0.28f);
                    break;
                case Id.Thud:    // pièce sur le feutre ou le bois
                    cache[i] = Make("pc_thud", 0.14f, t => (Mathf.Sin(6.2832f * (170f - 300f * t) * t) * 0.8f + ((float)rng.NextDouble() * 2f - 1f) * 0.25f) * Mathf.Exp(-t * 38f));
                    break;
                case Id.Insert:  // pièce qui glisse dans la fente puis tinte
                    cache[i] = Make("pc_insert", 0.4f, t => t < 0.1f ? ((float)rng.NextDouble() * 2f - 1f) * 0.12f * (t / 0.1f)
                        : (Mathf.Sin(6.2832f * 1760f * t) + 0.5f * Mathf.Sin(6.2832f * 2637f * t)) * Mathf.Exp(-(t - 0.1f) * 12f) * 0.3f);
                    break;
                case Id.Collect: // pièce gagnée tombant dans le bac
                    cache[i] = Make("pc_collect", 0.3f, t => (Mathf.Sin(6.2832f * 1319f * t) + 0.6f * Mathf.Sin(6.2832f * 1760f * t)) * Mathf.Exp(-t * 11f) * 0.3f);
                    break;
                case Id.Payout:  // gains versés : arpège montant
                    cache[i] = Make("pc_payout", 0.6f, t =>
                    {
                        float[] n = { 659f, 880f, 1175f, 1568f };
                        int k = Mathf.Min(3, (int)(t / 0.1f));
                        float l = t - k * 0.1f;
                        return Mathf.Sin(6.2832f * n[k] * t) * Mathf.Exp(-l * 8f) * 0.4f;
                    });
                    break;
                case Id.Clank:   // la plaque change de sens : choc mécanique
                    cache[i] = Make("pc_clank", 0.28f, t => (Mathf.Sin(6.2832f * 95f * t) * Mathf.Exp(-t * 18f) * 0.7f + Mathf.Sin(6.2832f * 410f * t) * Mathf.Exp(-t * 30f) * 0.3f
                        + ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 90f) * 0.3f));
                    break;
                default:         // moteur : boucle de 1 s (fréquences entières : se répète sans coupure)
                    cache[i] = Make("pc_motor", 1f, t => (Mathf.Sin(6.2832f * 55f * t) * 0.6f + Mathf.Sin(6.2832f * 110f * t) * 0.3f + Mathf.Sin(6.2832f * 165f * t) * 0.12f + Mathf.Sin(6.2832f * 23f * t) * 0.2f) * 0.5f);
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
