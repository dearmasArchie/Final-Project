using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SignalLost.EditorTools
{
    /// <summary>Places the StormController in the island scene with its rain material. Run by the island detail pass (art-details).</summary>
    public static class WeatherBuilder
    {
        const string Folder = "Assets/_Project/Art/Weather";
        const string RainMaterialPath = Folder + "/Rain_Streak.mat";

        public static string Dress(Transform root)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Weather");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(RainMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, RainMaterialPath);
            }
            // Transparent, alpha blended, no depth write: faint streaks
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);

            var go = new GameObject("Storm");
            go.transform.SetParent(root, false);
            var storm = go.AddComponent<StormController>();
            var so = new SerializedObject(storm);
            so.FindProperty("rainMaterial").objectReferenceValue = mat;
            so.ApplyModifiedPropertiesWithoutUndo();
            return "storm controller (rain starts when the signal upload completes)";
        }
    }
}
