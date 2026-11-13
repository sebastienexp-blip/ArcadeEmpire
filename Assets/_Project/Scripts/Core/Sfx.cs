using System;
using UnityEngine;

namespace ArcadeEmpire.Core
{
    /// <summary>
    /// Effets sonores générés par le code (aucun fichier audio importé à ce stade) : pièce, prise, chute, victoire.
    /// Volume réglé par GameAudio (touches - et + pour le volume général).
    /// </summary>
    public static class Sfx
    {
        public enum Kind { Coin, Grab, Drop, Win, Miss }

        private const int Rate = 44100;
        private static readonly AudioClip[] cache = new AudioClip[5];

        public static void Play(Kind kind, Vector3 position, float volume = 0.7f)
        {
            AudioClip clip = Get(kind);
            if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume * GameAudio.EffectiveSfx);
        }

        private static AudioClip Get(Kind kind)
        {
            int i = (int)kind;
            if (cache[i] != null) return cache[i];
            switch (kind)
            {
                case Kind.Coin:
                    cache[i] = Make("sfx_coin", 0.3f, t => Mathf.Sin(2f * Mathf.PI * (t < 0.08f ? 880f : 1320f) * t) * Mathf.Exp(-t * 12f) * 0.6f);
                    break;
                case Kind.Grab:
                    cache[i] = Make("sfx_grab", 0.2f, t => Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 20f) * 0.8f);
                    break;
                case Kind.Drop:
                    cache[i] = Make("sfx_drop", 0.3f, t => Mathf.Sin(2f * Mathf.PI * (700f * t - 800f * t * t)) * (1f - t / 0.3f) * 0.4f);
                    break;
                case Kind.Win:
                    cache[i] = Make("sfx_win", 0.55f, t =>
                    {
                        float[] notes = { 523f, 659f, 784f, 1047f };
                        int n = Mathf.Min(3, (int)(t / 0.12f));
                        float local = t - n * 0.12f;
                        return Mathf.Sin(2f * Mathf.PI * notes[n] * t) * Mathf.Exp(-local * 9f) * 0.5f;
                    });
                    break;
                default:
                    cache[i] = Make("sfx_miss", 0.35f, t => Mathf.Sin(2f * Mathf.PI * (300f - 400f * t) * t) * (1f - t / 0.35f) * 0.5f);
                    break;
            }
            return cache[i];
        }

        private static AudioClip Make(string name, float duration, Func<float, float> sample)
        {
            int n = Mathf.CeilToInt(Rate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = sample(i / (float)Rate);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
