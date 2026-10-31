using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Lists assets the game does not use: everything not reachable from the three build scenes, Resources, URP settings and
    /// TextMesh Pro. Writes Library/SignalLost/unused.txt ("bytes<TAB>path") for a move to _RemovedAssets. Bridge command: unused.
    /// Folders kept on purpose (builders, audio swapping, terrain painting) are skipped.
    /// </summary>
    public static class UnusedAssetTool
    {
        static readonly string[] KeepFolders =
        {
            "Assets/_Project/Audio", "Assets/_Project/UI", "Assets/_Project/Art/PolyHaven/Textures", "Assets/_Project/Art/PolyHaven/Sky",
            "Assets/_Project/Materials", "Assets/_Project/Art/Greybox", "Assets/_Project/Art/Details", "Assets/_Project/Art/Island",
            "Assets/_Project/Art/Water", "Assets/_Project/Art/Mixamo", "Assets/_Project/Art/kenney_fantasy-ui-borders",
            "Assets/_Project/Scenes", "Assets/_Recovery", "Assets/Sci-fi Pistol", "Assets/Same Gev Dudios",
        };

        [MenuItem("Tools/Signal Lost/List Unused Assets")]
        public static void Report()
        {
            var roots = new List<string>
            {
                "Assets/_Project/Scenes/MainMenu.unity", "Assets/_Project/Scenes/IslandExterior.unity", "Assets/_Project/Scenes/UndergroundFacility.unity",
                "Assets/InputSystem_Actions.inputactions",
            };
            foreach (var folder in new[] { "Assets/_Project/Resources", "Assets/Settings", "Assets/TextMesh Pro" })
                roots.AddRange(AssetDatabase.FindAssets("", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath));
            var used = new List<string>(AssetDatabase.GetDependencies(roots.Where(r => !AssetDatabase.IsValidFolder(r)).ToArray(), true));
            var usedSet = new HashSet<string>(used);

            var lines = new List<string>();
            long total = 0;
            foreach (var guid in AssetDatabase.FindAssets("", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path) || path.EndsWith(".cs") || path.EndsWith(".asmdef") || usedSet.Contains(path)) continue;
                if (KeepFolders.Any(k => path.StartsWith(k + "/") || path == k)) continue;
                if (path.StartsWith("Assets/_Project/Scripts") || path.StartsWith("Assets/_Project/Shaders") || path.StartsWith("Assets/_Project/Prefabs")
                    || path.StartsWith("Assets/_Project/Data") || path.StartsWith("Assets/_Project/Resources")) continue;
                long size = File.Exists(path) ? new FileInfo(path).Length : 0;
                total += size;
                lines.Add($"{size}\t{path}");
            }
            File.WriteAllLines("Library/SignalLost/unused.txt", lines);
            EditorCommandBridge.Log($"unused: {lines.Count} files, {total / (1024 * 1024)} MB listed in Library/SignalLost/unused.txt");
        }
    }
}
