using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Runs Signal Lost editor tools from command files, so they work even when custom menus don't show.
    /// Drop an empty file named &lt;command&gt;.cmd into Library/SignalLost/Commands and the Editor runs it
    /// within about a second (not during Play mode). Results go to Library/SignalLost/bridge.log.
    /// Commands: setup, import-tmp, hud, greybox, player, swap-player, sandbox, phase2, phase3, postfx, bunker-path,
    /// art-island, art-bunker, refresh, smoketest.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorCommandBridge
    {
        const string Folder = "Library/SignalLost";
        static readonly string CommandFolder = Path.Combine(Folder, "Commands");
        static readonly string LogPath = Path.Combine(Folder, "bridge.log");
        const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        static double nextCheck;

        static EditorCommandBridge()
        {
            Directory.CreateDirectory(CommandFolder);
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            foreach (var file in Directory.GetFiles(CommandFolder, "*.cmd"))
            {
                File.Delete(file);
                Run(Path.GetFileNameWithoutExtension(file));
                return; // one command per tick; the next runs a second later
            }
        }

        static void Run(string command)
        {
            Log($"start {command}");
            try
            {
                switch (command)
                {
                    case "setup": ProjectSetup.Run(); break;
                    case "import-tmp":
                        AssetDatabase.importPackageCompleted += OnTmpImported;
                        AssetDatabase.ImportPackage(TmpPackage, false);
                        return; // logged when the import finishes
                    case "hud": HUDPrefabBuilder.Build(); break;
                    case "greybox": GreyboxBuilder.BuildBoth(interactive: false); break;
                    case "player": PlayerPrefabBuilder.BuildPlayer(); break;
                    case "swap-player": PlayerPrefabBuilder.SwapIntoGameplayScenes(); break;
                    case "sandbox": PlayerPrefabBuilder.BuildSandbox(); break;
                    case "phase2": Phase2ContentBuilder.Build(); break;
                    case "phase3": Phase3ContentBuilder.Build(); break;
                    case "postfx": PostProcessBuilder.Build(); break;
                    case "bunker-path":
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/IslandExterior.unity");
                        Phase2ContentBuilder.RepaintBunkerPathToCurrentBunker();
                        break;
                    case "art-island": IslandArtBuilder.Build(); break;
                    case "art-bunker": BunkerArtBuilder.Build(); break;
                    case "refresh": AssetDatabase.Refresh(); break; // picks up new scripts without focusing Unity
                    case "smoketest": PlaySmokeTest.Run(); return; // reports when finished
                    default: Log($"unknown command {command}"); return;
                }
                Log($"done {command}");
            }
            catch (Exception e)
            {
                Log($"FAILED {command}: {e}");
                Debug.LogException(e);
            }
        }

        static void OnTmpImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            Log($"done import-tmp ({packageName})");
        }

        internal static void Log(string message) =>
            File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
    }
}
