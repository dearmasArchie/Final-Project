using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SignalLost.EditorTools
{
    /// <summary>Phase 5: builds the Windows player into Builds/SignalLost/ (git-ignored). Bridge command: build.</summary>
    public static class BuildTools
    {
        const string OutputDir = "Builds/SignalLost";
        const string ExeName = "ProjectOutpost_SignalLost.exe";

        [MenuItem("Tools/Signal Lost/Build Windows Player")]
        public static void BuildWindows()
        {
            EditorSceneManager_SaveAll();
            PlayerSettings.productName = "Project Outpost - Signal Lost";
            PlayerSettings.companyName = "IT 402W";
            PlayerSettings.bundleVersion = "1.0.0";

            Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/_Project/Scenes/MainMenu.unity",
                    "Assets/_Project/Scenes/IslandExterior.unity",
                    "Assets/_Project/Scenes/UndergroundFacility.unity",
                },
                locationPathName = $"{OutputDir}/{ExeName}",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            EditorCommandBridge.Log($"build: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:F0} s, " +
                                    $"errors={summary.totalErrors}, warnings={summary.totalWarnings}, output={Path.GetFullPath(options.locationPathName)}");
            if (summary.result != BuildResult.Succeeded)
                foreach (var step in report.steps)
                    foreach (var message in step.messages)
                        if (message.type == LogType.Error || message.type == LogType.Exception)
                            EditorCommandBridge.Log($"build error: {message.content}");
        }

        static void EditorSceneManager_SaveAll() => UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }
}
