using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Water pass: the island sea and the electrified Wing B floodwater both use the SignalLost/Water shader
    /// (ripples, depth color, refraction, shoreline foam, sky reflection; Wing B adds electric arcs).
    /// Safe to re-run. Also called by the island art pass and by phase2. Bridge command: water.
    /// </summary>
    public static class WaterArtBuilder
    {
        const string IslandScene = "Assets/_Project/Scenes/IslandExterior.unity";
        const string BunkerScene = "Assets/_Project/Scenes/UndergroundFacility.unity";
        const string Folder = "Assets/_Project/Art/Water";

        [MenuItem("Tools/Signal Lost/Art Pass – Water")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();

            var island = EditorSceneManager.OpenScene(IslandScene);
            int seas = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r.name != "Ocean") continue;
                r.sharedMaterial = OceanMaterial();
                seas++;
            }
            EditorSceneManager.MarkSceneDirty(island);
            EditorSceneManager.SaveScene(island);

            var bunker = EditorSceneManager.OpenScene(BunkerScene);
            int floods = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r.name != "WaterSurface") continue;
                r.sharedMaterial = FloodMaterial();
                floods++;
            }
            EditorSceneManager.MarkSceneDirty(bunker);
            EditorSceneManager.SaveScene(bunker);

            EditorSceneManager.OpenScene(IslandScene);
            AssetDatabase.SaveAssets();
            EditorCommandBridge.Log($"water: ocean surfaces={seas}, flood surfaces={floods}");
        }

        public static Material OceanMaterial()
        {
            var mat = LoadOrCreate("Ocean");
            mat.SetColor("_ShallowColor", new Color(0.16f, 0.5f, 0.5f));
            mat.SetColor("_DeepColor", new Color(0.015f, 0.08f, 0.13f));
            mat.SetVector("_Absorption", new Vector4(1.0f, 0.45f, 0.35f, 0f));
            mat.SetFloat("_DepthMax", 10f);
            mat.SetFloat("_DepthBias", 2f);
            mat.SetFloat("_SunScatter", 0.3f);
            mat.SetFloat("_Smoothness", 0.96f);
            mat.SetFloat("_ReflectionStrength", 0.85f);
            mat.SetFloat("_WaveScale", 0.12f);
            mat.SetFloat("_WaveSpeed", 0.35f);
            mat.SetFloat("_NormalStrength", 1.3f);
            mat.SetFloat("_Refraction", 0.04f);
            mat.SetFloat("_FoamWidth", 0.3f);
            mat.SetFloat("_FoamAmount", 0.85f);
            mat.SetFloat("_CrestFoam", 0.25f);
            mat.SetFloat("_FarFade", 140f);
            mat.SetColor("_SparkColor", Color.clear);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material FloodMaterial()
        {
            var mat = LoadOrCreate("FloodWater");
            mat.SetColor("_ShallowColor", new Color(0.1f, 0.18f, 0.16f));
            mat.SetColor("_DeepColor", new Color(0.02f, 0.06f, 0.07f));
            mat.SetVector("_Absorption", new Vector4(2.5f, 1.4f, 1.6f, 0f));
            mat.SetFloat("_DepthMax", 1f);
            mat.SetFloat("_DepthBias", 0.3f);
            mat.SetFloat("_SunScatter", 0f);
            mat.SetFloat("_Smoothness", 0.93f);
            mat.SetFloat("_ReflectionStrength", 1f);
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); // no sky underground: only lamp highlights, never the room walls mirrored
            mat.SetFloat("_WaveScale", 0.7f);
            mat.SetFloat("_WaveSpeed", 0.5f);
            mat.SetFloat("_NormalStrength", 1.2f);
            mat.SetFloat("_Refraction", 0.02f);
            mat.SetFloat("_FoamWidth", 0.25f);
            mat.SetFloat("_FoamAmount", 0.7f);
            mat.SetFloat("_CrestFoam", 0f);
            mat.SetFloat("_FarFade", 40f);
            mat.SetColor("_SparkColor", new Color(0.15f, 0.75f, 1.3f, 1f));
            mat.SetFloat("_SparkScale", 0.9f);
            mat.SetFloat("_SparkSpeed", 1.2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material LoadOrCreate(string name)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Water");
            string path = $"{Folder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("SignalLost/Water"));
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = Shader.Find("SignalLost/Water");
            return mat;
        }
    }
}
