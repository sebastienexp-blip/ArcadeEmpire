using ArcadeEmpire.Save;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcadeEmpire.Core
{
    /// <summary>
    /// Choisit au lancement le profil graphique : "PC" (graphismes complets) ou "Mobile" (version allégée).
    /// Les deux assets URP sont référencés ici (créés automatiquement par ProjectSetup dans l'éditeur / la CI),
    /// ce qui les inclut dans le build.
    /// </summary>
    public class GraphicsQualityManager : MonoBehaviour
    {
        public enum Profile { Auto = -1, PC = 0, Mobile = 1 }

        [SerializeField] private RenderPipelineAsset pcPipeline;
        [SerializeField] private RenderPipelineAsset mobilePipeline;
        [SerializeField] private int mobileTargetFps = 60;

        public Profile Current { get; private set; }

        public void Configure(RenderPipelineAsset pc, RenderPipelineAsset mobile)
        {
            pcPipeline = pc;
            mobilePipeline = mobile;
        }

        private void Start()
        {
            int manual = SaveManager.Instance != null ? SaveManager.Instance.Data.graphicsQualityOverride : -1;
            Apply(manual == 0 ? Profile.PC : manual == 1 ? Profile.Mobile : Profile.Auto);
        }

        public void Apply(Profile profile)
        {
            if (profile == Profile.Auto)
                profile = Application.isMobilePlatform ? Profile.Mobile : Profile.PC;

            Current = profile;
            RenderPipelineAsset asset = profile == Profile.Mobile ? mobilePipeline : pcPipeline;
            if (asset != null)
                QualitySettings.renderPipeline = asset;
            else
                Debug.LogWarning($"[Graphics] Asset URP du profil {profile} non assigné.");

            if (profile == Profile.PC && QualitySettings.vSyncCount == 0)
                QualitySettings.vSyncCount = 1;   // évite de faire tourner le GPU à fond inutilement

            if (profile == Profile.Mobile)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = mobileTargetFps;
            }
        }
    }
}
