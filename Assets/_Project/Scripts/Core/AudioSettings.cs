using UnityEngine;

namespace ArcadeEmpire.Core
{
    /// <summary>Volumes (général, effets, musique) enregistrés dans les préférences du joueur.</summary>
    public static class GameAudio
    {
        private const string KMaster = "ae_vol_master", KSfx = "ae_vol_sfx", KMusic = "ae_vol_music";

        public static float Master { get => Get(KMaster, 1f); set => Set(KMaster, value); }
        public static float SfxVolume { get => Get(KSfx, 1f); set => Set(KSfx, value); }
        public static float MusicVolume { get => Get(KMusic, 0.5f); set => Set(KMusic, value); }

        public static float EffectiveSfx => Master * SfxVolume;
        public static float EffectiveMusic => Master * MusicVolume;

        private static float Get(string key, float def) => Mathf.Clamp01(PlayerPrefs.GetFloat(key, def));
        private static void Set(string key, float v) { PlayerPrefs.SetFloat(key, Mathf.Clamp01(v)); PlayerPrefs.Save(); }
    }
}
