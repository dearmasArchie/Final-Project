using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// One-click Phase 0 setup: folders, the three scenes in Build Settings, tags, layers,
    /// and (after confirmation) removal of the URP template sample files.
    /// Safe to run more than once: anything that already exists is skipped.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        // Runs the setup automatically once, when the project scenes don't exist yet.
        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>($"{ScenesFolder}/IslandExterior.unity") == null)
                    Run();
            };
        }

        const string Root = "Assets/_Project";
        const string ScenesFolder = Root + "/Scenes";

        static readonly string[] Folders =
        {
            "Scripts", "Scenes", "Prefabs", "Materials", "Art", "Audio", "UI"
        };

        // Order matters: index 0 is the first scene the build opens.
        static readonly string[] SceneNames = { "MainMenu", "IslandExterior", "UndergroundFacility" };

        static readonly string[] Tags = { "Enemy", "WeakPoint", "Interactable", "Ground" };
        static readonly string[] Layers = { "Player", "Enemy", "Interactable", "Ground" };

        static readonly string[] TemplateLeftovers =
        {
            "Assets/Scenes/SampleScene.unity", "Assets/Scenes", "Assets/TutorialInfo", "Assets/Readme.asset"
        };

        [MenuItem("Signal Lost/Run Phase 0 Setup")]
        [MenuItem("Tools/Signal Lost/Run Phase 0 Setup")]
        public static void Run()
        {
            Debug.Log("[Signal Lost] Running Phase 0 setup...");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Signal Lost] Setup cancelled at the save prompt.");
                return;
            }

            CreateFolders();
            CreateScenesAndBuildSettings();
            AddTags();
            AddLayers();

            // Open a project scene first so the sample scene is not open while it is deleted.
            EditorSceneManager.OpenScene($"{ScenesFolder}/IslandExterior.unity");
            RemoveTemplateLeftovers();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Signal Lost] Phase 0 setup complete.");
        }

        static void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "_Project");
            foreach (var folder in Folders)
                if (!AssetDatabase.IsValidFolder($"{Root}/{folder}"))
                    AssetDatabase.CreateFolder(Root, folder);
        }

        static void CreateScenesAndBuildSettings()
        {
            foreach (var name in SceneNames)
            {
                var path = $"{ScenesFolder}/{name}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) continue;

                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
            }

            EditorBuildSettings.scenes = SceneNames
                .Select(name => new EditorBuildSettingsScene($"{ScenesFolder}/{name}.unity", true))
                .ToArray();
        }

        static SerializedObject LoadTagManager() =>
            new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        static void AddTags()
        {
            var tagManager = LoadTagManager();
            var tagsProp = tagManager.FindProperty("tags");

            foreach (var tag in Tags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) exists = true;
                if (exists) continue;

                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            }
            tagManager.ApplyModifiedProperties();
        }

        static void AddLayers()
        {
            var tagManager = LoadTagManager();
            var layersProp = tagManager.FindProperty("layers");

            foreach (var layer in Layers)
            {
                if (LayerMask.NameToLayer(layer) != -1) continue;

                // Layers 0-7 are reserved by Unity; use the first empty user layer.
                for (int i = 8; i < layersProp.arraySize; i++)
                {
                    var element = layersProp.GetArrayElementAtIndex(i);
                    if (!string.IsNullOrEmpty(element.stringValue)) continue;
                    element.stringValue = layer;
                    break;
                }
            }
            tagManager.ApplyModifiedProperties();
        }

        static void RemoveTemplateLeftovers()
        {
            var existing = TemplateLeftovers.Where(p => AssetDatabase.LoadMainAssetAtPath(p) != null).ToArray();
            if (existing.Length == 0) return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Remove template files?",
                "Delete these URP template files? They are not used by Signal Lost.\n\n" + string.Join("\n", existing),
                "Delete", "Keep");
            if (!confirmed) return;

            foreach (var path in existing)
                AssetDatabase.DeleteAsset(path);
        }
    }
}
