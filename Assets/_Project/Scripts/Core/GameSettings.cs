using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ArcadeEmpire.Core
{
    /// <summary>
    /// Réglages du joueur (graphismes, caméra, commandes, interface), enregistrés dans les préférences et appliqués au lancement.
    /// Le volume est géré par GameAudio.
    /// </summary>
    public static class GameSettings
    {
        private const string P = "ae_set_";

        public static int ResolutionIndex { get => GetI("res", -1); set => SetI("res", value); }
        public static bool Fullscreen { get => GetI("fs", 1) == 1; set => SetI("fs", value ? 1 : 0); }
        public static int VSync { get => GetI("vsync", 1); set => SetI("vsync", value); }
        public static int TargetFps { get => GetI("fps", 60); set => SetI("fps", value); }
        public static float RenderScale { get => GetF("rs", 1f); set => SetF("rs", Mathf.Clamp(value, 0.5f, 1.5f)); }
        public static int Shadows { get => GetI("shadow", 2); set => SetI("shadow", value); }      // 0 off, 1 faibles, 2 normales, 3 hautes
        public static int Antialiasing { get => GetI("msaa", 2); set => SetI("msaa", value); }      // 0, 1=2x, 2=4x, 3=8x
        public static float FovScale { get => GetF("fov", 1f); set => SetF("fov", Mathf.Clamp(value, 0.8f, 1.3f)); }
        public static float LookSensitivity { get => GetF("sens", 1f); set => SetF("sens", Mathf.Clamp(value, 0.2f, 3f)); }
        public static bool InvertY { get => GetI("invy", 0) == 1; set => SetI("invy", value ? 1 : 0); }
        public static float UiScale { get => GetF("ui", 1f); set => SetF("ui", Mathf.Clamp(value, 0.8f, 1.3f)); }
        public static bool ShowKeyHints { get => GetI("hints", 1) == 1; set => SetI("hints", value ? 1 : 0); }
        public static bool ShowCameraPad { get => GetI("campad", UnityEngine.Application.isMobilePlatform ? 0 : 1) == 1; set => SetI("campad", value ? 1 : 0); }
        public static float TouchScale { get => GetF("touch", 1f); set => SetF("touch", Mathf.Clamp(value, 0.7f, 1.5f)); }
        public static float TouchOpacity { get => GetF("touchop", 0.6f); set => SetF("touchop", Mathf.Clamp(value, 0.2f, 1f)); }
        public static bool AutoSave { get => GetI("autosave", 1) == 1; set => SetI("autosave", value ? 1 : 0); }

        public static event System.Action Changed;
        public static void NotifyChanged() => Changed?.Invoke();

        public static readonly int[] FpsChoices = { 30, 60, 90, 120, 0 };   // 0 = illimité

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot() => ApplyAll();

        public static Resolution[] Resolutions()
        {
            var list = new System.Collections.Generic.List<Resolution>();
            foreach (Resolution r in Screen.resolutions)
            {
                if (r.height < 480) continue;
                if (list.Count > 0 && list[list.Count - 1].width == r.width && list[list.Count - 1].height == r.height) continue;
                list.Add(r);
            }
            return list.ToArray();
        }

        public static void ApplyAll()
        {
            ApplyScreen();
            ApplyGraphics();
            Changed?.Invoke();
        }

        public static void ApplyScreen()
        {
            if (Application.isMobilePlatform) return;
            Resolution[] rs = Resolutions();
            int i = ResolutionIndex;
            if (i >= 0 && i < rs.Length)
                Screen.SetResolution(rs[i].width, rs[i].height, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            else
                Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        public static void ApplyGraphics()
        {
            if (!Application.isMobilePlatform) QualitySettings.vSyncCount = VSync;
            Application.targetFrameRate = Application.isMobilePlatform ? (TargetFps == 0 ? 60 : Mathf.Min(TargetFps, 60)) : (VSync > 0 ? -1 : TargetFps == 0 ? -1 : TargetFps);

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = RenderScale;
                int[] msaa = { 1, 2, 4, 8 };
                urp.msaaSampleCount = msaa[Mathf.Clamp(Antialiasing, 0, 3)];
                float[] dist = { 0f, 18f, 35f, 60f };
                urp.shadowDistance = Mathf.Max(0.1f, dist[Mathf.Clamp(Shadows, 0, 3)]);
            }
        }

        public static void ResetToDefaults()
        {
            string[] keys = { "res", "fs", "vsync", "fps", "rs", "shadow", "msaa", "fov", "sens", "invy", "ui", "hints", "campad", "touch", "touchop", "autosave" };
            foreach (string k in keys) PlayerPrefs.DeleteKey(P + k);
            PlayerPrefs.Save();
            GameAudio.Master = 1f; GameAudio.SfxVolume = 1f; GameAudio.MusicVolume = 0.5f;
            ApplyAll();
        }

        private static int GetI(string k, int d) => PlayerPrefs.GetInt(P + k, d);
        private static void SetI(string k, int v) { PlayerPrefs.SetInt(P + k, v); PlayerPrefs.Save(); }
        private static float GetF(string k, float d) => PlayerPrefs.GetFloat(P + k, d);
        private static void SetF(string k, float v) { PlayerPrefs.SetFloat(P + k, v); PlayerPrefs.Save(); }
    }
}
