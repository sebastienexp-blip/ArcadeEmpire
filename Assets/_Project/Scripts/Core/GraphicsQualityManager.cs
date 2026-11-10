using ArcadeEmpire.Save;
using UnityEngine;

namespace ArcadeEmpire.Core
{
    /// <summary>
    /// Choisit automatiquement le niveau de qualité : "PC" (graphismes complets) ou "Mobile"
    /// (version allégée). Les niveaux doivent exister dans Project Settings > Quality, avec
    /// exactement ces noms, chacun relié à son propre asset URP (voir README).
    /// </summary>
    public class GraphicsQualityManager : MonoBehaviour
    {
        public const string PcLevelName = "PC";
        public const string MobileLevelName = "Mobile";

        [SerializeField] private int mobileTargetFps = 60;

        private void Start()
        {
            int manual = SaveManager.Instance != null ? SaveManager.Instance.Data.graphicsQualityOverride : -1;
            if (manual >= 0 && manual < QualitySettings.names.Length)
                Apply(manual);
            else
                ApplyAuto();
        }

        public void ApplyAuto()
        {
            string wanted = Application.isMobilePlatform ? MobileLevelName : PcLevelName;
            int index = System.Array.IndexOf(QualitySettings.names, wanted);
            if (index < 0)
            {
                Debug.LogWarning($"[Graphics] Niveau de qualité \"{wanted}\" introuvable dans Project Settings > Quality.");
                return;
            }
            Apply(index);
        }

        public void Apply(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex, true);
            if (Application.isMobilePlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = mobileTargetFps;
            }
        }
    }
}
