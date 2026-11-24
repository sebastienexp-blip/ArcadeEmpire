using System.Collections.Generic;
using UnityEngine;

namespace ArcadeEmpire.Core
{
    /// <summary>
    /// Sons réalistes générés avec ElevenLabs (Assets/_Project/Resources/Audio/*.ogg), chargés par nom.
    /// Si un fichier manque, les sons générés par le code (Sfx, ClawAudio, PusherAudio) prennent le relais.
    /// </summary>
    public static class SoundBank
    {
        private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        private static AudioSource ui;

        public static AudioClip Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (cache.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            cache[name] = c;
            return c;
        }

        /// <summary>Son d'interface (non spatialisé) : clics, menus, achats, clés, niveau…</summary>
        public static void Play2D(string name, float volume = 0.7f, float pitch = 1f)
        {
            var clip = Get(name);
            if (clip == null) return;
            if (ui == null)
            {
                var go = new GameObject("UiAudio");
                Object.DontDestroyOnLoad(go);
                ui = go.AddComponent<AudioSource>();
                ui.spatialBlend = 0f; ui.playOnAwake = false; ui.ignoreListenerPause = true;
            }
            ui.pitch = pitch;
            ui.PlayOneShot(clip, volume * GameAudio.EffectiveSfx);
        }

        /// <summary>Son placé dans le monde (s'atténue avec la distance).</summary>
        public static void PlayAt(string name, Vector3 pos, float volume = 0.8f, float pitch = 1f)
        {
            var clip = Get(name);
            if (clip == null) return;
            var go = new GameObject("Sfx_" + name);
            go.transform.position = pos;
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.spatialBlend = 0.85f; s.minDistance = 1.2f; s.maxDistance = 18f; s.rolloffMode = AudioRolloffMode.Linear;
            s.pitch = pitch; s.volume = volume * GameAudio.EffectiveSfx; s.dopplerLevel = 0f;
            s.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }
    }
}
