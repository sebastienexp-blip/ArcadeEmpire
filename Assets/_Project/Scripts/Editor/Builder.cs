#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArcadeEmpire.EditorTools
{
    /// <summary>Méthodes de build appelées par GitHub Actions (game-ci/unity-builder, paramètre buildMethod).</summary>
    public static class Builder
    {
        public static void BuildAndroid() => Build(BuildTarget.Android, "build/Android/ArcadeEmpire.apk", "android");
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "build/Windows/ArcadeEmpire.exe", "windows");

        /// <summary>Étape 1 de la CI : réglages qui exigent un redémarrage de l'éditeur (Input System seul). Quitte aussitôt.</summary>
        public static void Prepare()
        {
            ProjectSetup.EnableInputSystemOnly();
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        static void Build(BuildTarget target, string output, string tag)
        {
            var rep = new StringBuilder($"# Construction {tag}\n\n");
            int code = 1;
            try
            {
                Directory.CreateDirectory($"ci-out/{tag}");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                ProjectSetup.Run();

                PlayerSettings.companyName = "Arcade Empire Studio";
                PlayerSettings.productName = "Arcade Empire";
                string run = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
                if (string.IsNullOrEmpty(run) && File.Exists("ci-build-number.txt")) run = File.ReadAllText("ci-build-number.txt").Trim();
                int build = int.TryParse(run, out int n) ? n : 1;
                PlayerSettings.bundleVersion = "0.1." + build;

                if (target == BuildTarget.Android)
                {
                    var t = NamedBuildTarget.Android;
                    PlayerSettings.SetApplicationIdentifier(t, "com.arcadeempire.game");
                    PlayerSettings.SetScriptingBackend(t, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                    PlayerSettings.Android.bundleVersionCode = build;
                    PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                    EditorUserBuildSettings.buildAppBundle = false;
                    EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;   // pas de fichiers de débogage : économise le disque du serveur de build
                    EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
                }

                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                rep.AppendLine("- Scènes : " + string.Join(", ", scenes));
                var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = target,
                    options = BuildOptions.None
                });
                var s2 = r.summary;
                rep.AppendLine($"- Résultat : **{s2.result}** en {s2.totalTime.TotalMinutes:0.0} min, {s2.totalErrors} erreur(s), {s2.totalWarnings} avertissement(s)");
                foreach (var step in r.steps)
                    foreach (var m in step.messages.Where(m => m.type == LogType.Error || m.type == LogType.Exception).Take(20))
                        rep.AppendLine($"- ❌ {step.name} : {m.content.Split('\n')[0]}");
                code = s2.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e)
            {
                rep.AppendLine("- ❌ Exception : " + e.Message);
                Debug.LogException(e);
            }
            File.WriteAllText($"ci-out/{tag}/build.md", rep.ToString());
            EditorApplication.Exit(code);
        }
    }
}
#endif
