#if UNITY_EDITOR
using System.IO;
using ArcadeEmpire.Core;
using ArcadeEmpire.Data;
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
            managers.AddComponent<InventoryManager>().Configure(EnsureCatalog());
            managers.AddComponent<ProgressionManager>();
            managers.AddComponent<MissionManager>();
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

        /// <summary>Catalogue de toutes les MachineData (machines et décorations), triées par catégorie puis par prix.</summary>
        public static MachineCatalog EnsureMachineCatalog()
        {
            EnsureFolder("Assets/_Project/Data");
            const string path = "Assets/_Project/Data/MachineCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<MachineCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MachineCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            catalog.machines.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:MachineData"))
            {
                var m = AssetDatabase.LoadAssetAtPath<MachineData>(AssetDatabase.GUIDToAssetPath(guid));
                if (m != null) catalog.machines.Add(m);
            }
            catalog.machines.Sort((a, b) => a.category != b.category ? a.category.CompareTo(b.category) : a.purchasePrice.CompareTo(b.purchasePrice));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>Catalogue de toutes les RewardData du projet, triées par rareté puis par nom.</summary>
        static RewardCatalog EnsureCatalog()
        {
            EnsureFolder("Assets/_Project/Data");
            const string path = "Assets/_Project/Data/RewardCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<RewardCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RewardCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            catalog.rewards.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:RewardData"))
            {
                var r = AssetDatabase.LoadAssetAtPath<RewardData>(AssetDatabase.GUIDToAssetPath(guid));
                if (r != null) catalog.rewards.Add(r);
            }
            catalog.rewards.Sort((a, b) => a.rarity != b.rarity ? a.rarity.CompareTo(b.rarity) : string.CompareOrdinal(a.displayName, b.displayName));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>Le jeu utilise uniquement le nouvel Input System (activeInputHandler = 1).</summary>
        public static void EnableInputSystemOnly()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue != 1)
            {
                prop.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
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
