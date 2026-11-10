#if UNITY_EDITOR
using System.IO;
using ArcadeEmpire.Core;
using ArcadeEmpire.Economy;
using ArcadeEmpire.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>
    /// Configuration automatique du projet (exécutée par la CI GitHub, ou via le menu ArcadeEmpire > Configurer le projet) :
    /// crée les deux assets URP (PC / Mobile) et la scène de démarrage Boot. Idempotent.
    /// </summary>
    public static class ProjectSetup
    {
        public const string RenderDir = "Assets/_Project/Settings";
        public const string ScenePath = "Assets/_Project/Scenes/Boot.unity";

        [MenuItem("ArcadeEmpire/Configurer le projet")]
        public static void Run()
        {
            var pc = EnsurePipeline("PC", true);
            var mobile = EnsurePipeline("Mobile", false);
            GraphicsSettings.defaultRenderPipeline = pc;
            QualitySettings.renderPipeline = pc;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            RoomBuilder.Build();
            EnsureBootScene(pc, mobile);
            EnableInputSystemOnly();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static UniversalRenderPipelineAsset EnsurePipeline(string name, bool pcProfile)
        {
            EnsureFolder(RenderDir);
            string rendererPath = $"{RenderDir}/Renderer_{name}.asset";
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rd == null)
            {
                rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rd, rendererPath);
            }

            string pipePath = $"{RenderDir}/URP_{name}.asset";
            var pipe = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipePath);
            if (pipe == null)
            {
                pipe = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(pipe, pipePath);
            }

            if (pcProfile)
            {
                pipe.supportsHDR = true;
                pipe.msaaSampleCount = 4;
                pipe.renderScale = 1f;
                pipe.shadowDistance = 50f;
                pipe.shadowCascadeCount = 4;
            }
            else
            {
                pipe.supportsHDR = false;
                pipe.msaaSampleCount = 1;
                pipe.renderScale = 0.8f;
                pipe.shadowDistance = 15f;
                pipe.shadowCascadeCount = 1;
            }
            EditorUtility.SetDirty(rd);
            EditorUtility.SetDirty(pipe);
            return pipe;
        }

        static void EnsureBootScene(RenderPipelineAsset pc, RenderPipelineAsset mobile)
        {
            EnsureFolder("Assets/_Project/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var managers = new GameObject("Managers");
            managers.AddComponent<CurrencyManager>();
            managers.AddComponent<SaveManager>();
            managers.AddComponent<BootLoader>();
            var gfx = managers.AddComponent<GraphicsQualityManager>();
            gfx.Configure(pc, mobile);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(RoomBuilder.ScenePath, true)
            };
        }

        /// <summary>Le jeu utilise uniquement le nouvel Input System (activeInputHandler = 1).</summary>
        static void EnableInputSystemOnly()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue != 1)
            {
                prop.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
#endif
