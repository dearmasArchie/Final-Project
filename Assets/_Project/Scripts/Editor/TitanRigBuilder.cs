using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// The Titan model is 159 rigid mesh pieces with no skeleton, and each piece holds both legs in one mesh. This splits every
    /// piece by triangle into body, and thigh / shin / foot for each leg, and parents the leg parts under hip, knee and ankle
    /// pivots, so MechWalkBob can swing them into a real walk. Run by EnemyArtBuilder whenever the Titan model is rebuilt.
    /// Meshes are saved as sub-assets of Art/SketchFab/TitanRobot/TitanParts.asset.
    /// </summary>
    public static class TitanRigBuilder
    {
        const string MeshAssetPath = "Assets/_Project/Art/SketchFab/TitanRobot/TitanParts.asset";

        // Measured on the fitted model (prefab-root space, meters): legs fill y 0..2.25 at |x| 0.55..2, the body starts above them.
        const float LegTop = 2.25f, KneeY = 1.3f, AnkleY = 0.45f, LegInner = 0.55f, LegX = 1.05f;
        static readonly float[] PivotZ = { -0.3f, -0.5f, -0.3f };   // hip, knee, ankle

        enum Part { Body, ThighL, ShinL, FootL, ThighR, ShinR, FootR }

        public static string Rig(GameObject model, Transform prefabRoot)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(MeshAssetPath) != null) AssetDatabase.DeleteAsset(MeshAssetPath);

            // Pivots, nested hip > knee > ankle, positioned in the model's space
            var pivots = new Transform[7];
            var pivotRefs = new Dictionary<string, Transform>();
            foreach (float side in new[] { -1f, 1f })
            {
                string tag = side < 0 ? "L" : "R";
                var hip = NewPivot(model.transform, $"Leg{tag}_Hip", prefabRoot, new Vector3(side * LegX, LegTop, PivotZ[0]), null);
                var knee = NewPivot(model.transform, $"Leg{tag}_Knee", prefabRoot, new Vector3(side * LegX, KneeY, PivotZ[1]), hip);
                var ankle = NewPivot(model.transform, $"Leg{tag}_Ankle", prefabRoot, new Vector3(side * LegX, AnkleY, PivotZ[2]), knee);
                int offset = side < 0 ? 1 : 4;
                pivots[offset] = hip; pivots[offset + 1] = knee; pivots[offset + 2] = ankle;
                pivotRefs[$"{tag}Hip"] = hip; pivotRefs[$"{tag}Knee"] = knee; pivotRefs[$"{tag}Ankle"] = ankle;
            }

            bool first = true;
            int pieces = 0, split = 0, meshes = 0;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.transform.parent != model.transform || mf.sharedMesh == null) continue; // only the original pieces
                pieces++;
                var source = mf.sharedMesh;
                var groups = Classify(source, mf.transform, prefabRoot);
                if (groups.Count > 1 || !groups.ContainsKey(Part.Body)) split++;

                var originalGo = mf.gameObject;
                var materials = originalGo.GetComponent<MeshRenderer>().sharedMaterials;
                foreach (var kv in groups)
                {
                    var mesh = BuildMesh(source, kv.Value, $"{originalGo.name}_{kv.Key}");
                    if (first) { AssetDatabase.CreateAsset(mesh, MeshAssetPath); first = false; }
                    else AssetDatabase.AddObjectToAsset(mesh, MeshAssetPath);
                    meshes++;

                    if (kv.Key == Part.Body) { mf.sharedMesh = mesh; continue; }
                    var dup = Object.Instantiate(originalGo, model.transform);
                    dup.name = $"{originalGo.name}_{kv.Key}";
                    dup.GetComponent<MeshFilter>().sharedMesh = mesh;
                    dup.GetComponent<MeshRenderer>().sharedMaterials = materials;
                    dup.transform.SetPositionAndRotation(originalGo.transform.position, originalGo.transform.rotation);
                    dup.transform.localScale = originalGo.transform.localScale;
                    dup.transform.SetParent(pivots[(int)kv.Key], true);
                }
                if (!groups.ContainsKey(Part.Body)) Object.DestroyImmediate(originalGo);
            }

            // Wire the walk animation
            var bob = model.GetComponent<MechWalkBob>();
            if (bob == null) bob = model.AddComponent<MechWalkBob>();
            var so = new SerializedObject(bob);
            so.FindProperty("leftHip").objectReferenceValue = pivotRefs["LHip"];
            so.FindProperty("leftKnee").objectReferenceValue = pivotRefs["LKnee"];
            so.FindProperty("leftAnkle").objectReferenceValue = pivotRefs["LAnkle"];
            so.FindProperty("rightHip").objectReferenceValue = pivotRefs["RHip"];
            so.FindProperty("rightKnee").objectReferenceValue = pivotRefs["RKnee"];
            so.FindProperty("rightAnkle").objectReferenceValue = pivotRefs["RAnkle"];
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return $"{pieces} pieces, {split} split, {meshes} meshes, legs rigged";
        }

        static Transform NewPivot(Transform model, string name, Transform prefabRoot, Vector3 rootSpacePos, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : model, false);
            go.transform.position = prefabRoot.TransformPoint(rootSpacePos);
            go.transform.rotation = prefabRoot.rotation;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        /// <summary>Triangle indices per part, per submesh, by where each triangle's center sits in the prefab's space.</summary>
        static Dictionary<Part, List<int>[]> Classify(Mesh mesh, Transform piece, Transform prefabRoot)
        {
            var vertices = mesh.vertices;
            var toRoot = prefabRoot.worldToLocalMatrix * piece.localToWorldMatrix;
            var result = new Dictionary<Part, List<int>[]>();
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var idx = mesh.GetTriangles(sub);
                for (int i = 0; i < idx.Length; i += 3)
                {
                    Vector3 c = toRoot.MultiplyPoint3x4((vertices[idx[i]] + vertices[idx[i + 1]] + vertices[idx[i + 2]]) / 3f);
                    Part part = PartAt(c);
                    if (!result.TryGetValue(part, out var lists))
                    {
                        lists = new List<int>[mesh.subMeshCount];
                        for (int k = 0; k < lists.Length; k++) lists[k] = new List<int>();
                        result[part] = lists;
                    }
                    lists[sub].Add(idx[i]); lists[sub].Add(idx[i + 1]); lists[sub].Add(idx[i + 2]);
                }
            }
            return result;
        }

        static Part PartAt(Vector3 p)
        {
            if (p.y >= LegTop || Mathf.Abs(p.x) < LegInner) return Part.Body;
            bool left = p.x < 0f;
            if (p.y < AnkleY) return left ? Part.FootL : Part.FootR;
            if (p.y < KneeY) return left ? Part.ShinL : Part.ShinR;
            return left ? Part.ThighL : Part.ThighR;
        }

        /// <summary>A mesh with only the given triangles, with its vertices compacted.</summary>
        static Mesh BuildMesh(Mesh source, List<int>[] triangles, string name)
        {
            var map = new Dictionary<int, int>();
            var order = new List<int>();
            foreach (var list in triangles)
                foreach (int i in list)
                    if (!map.ContainsKey(i)) { map[i] = order.Count; order.Add(i); }

            var mesh = new Mesh { name = name, indexFormat = order.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            var v = source.vertices; var n = source.normals; var t = source.tangents; var uv = source.uv; var uv2 = source.uv2; var col = source.colors32;
            var nv = new Vector3[order.Count];
            var nn = n.Length == v.Length ? new Vector3[order.Count] : null;
            var nt = t.Length == v.Length ? new Vector4[order.Count] : null;
            var nuv = uv.Length == v.Length ? new Vector2[order.Count] : null;
            var nuv2 = uv2.Length == v.Length ? new Vector2[order.Count] : null;
            var ncol = col.Length == v.Length ? new Color32[order.Count] : null;
            for (int k = 0; k < order.Count; k++)
            {
                int s = order[k];
                nv[k] = v[s];
                if (nn != null) nn[k] = n[s];
                if (nt != null) nt[k] = t[s];
                if (nuv != null) nuv[k] = uv[s];
                if (nuv2 != null) nuv2[k] = uv2[s];
                if (ncol != null) ncol[k] = col[s];
            }
            mesh.vertices = nv;
            if (nn != null) mesh.normals = nn;
            if (nt != null) mesh.tangents = nt;
            if (nuv != null) mesh.uv = nuv;
            if (nuv2 != null) mesh.uv2 = nuv2;
            if (ncol != null) mesh.colors32 = ncol;
            mesh.subMeshCount = triangles.Length;
            for (int sub = 0; sub < triangles.Length; sub++)
            {
                var remapped = new int[triangles[sub].Count];
                for (int k = 0; k < remapped.Length; k++) remapped[k] = map[triangles[sub][k]];
                mesh.SetTriangles(remapped, sub);
            }
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
