using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        const string FenceFolder = ModelFolder + "/modular_chainlink_fence";
        const string FenceModelPath = FenceFolder + "/modular_chainlink_fence_2k.fbx";

        /// <summary>Wire mesh needs its alpha: the Poly Haven wire diffuse and alpha are separate files, so merge them once.</summary>
        static Texture2D FenceWireTexture()
        {
            string path = $"{MaterialFolder}/FenceWire_Color.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            string diffPath = $"{FenceFolder}/textures/modular_chainlink_fence_wire_diff_2k.png";
            string alphaPath = $"{FenceFolder}/textures/modular_chainlink_fence_wire_alpha_2k.png";
            foreach (var p in new[] { diffPath, alphaPath })
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(p);
                if (!imp.isReadable) { imp.isReadable = true; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport(); }
            }
            var diff = AssetDatabase.LoadAssetAtPath<Texture2D>(diffPath);
            var alpha = AssetDatabase.LoadAssetAtPath<Texture2D>(alphaPath);
            var d = diff.GetPixels32();
            var a = alpha.GetPixels32();
            if (alpha.width != diff.width || alpha.height != diff.height) throw new System.InvalidOperationException("Fence wire diffuse and alpha differ in size");
            for (int i = 0; i < d.Length; i++) d[i].a = a[i].r;
            var merged = new Texture2D(diff.width, diff.height, TextureFormat.RGBA32, false);
            merged.SetPixels32(d);
            merged.Apply();
            System.IO.File.WriteAllBytes(path.Replace("Assets/", Application.dataPath + "/"), merged.EncodeToPNG());
            Object.DestroyImmediate(merged);
            AssetDatabase.ImportAsset(path);
            var imported = (TextureImporter)AssetImporter.GetAtPath(path);
            imported.alphaIsTransparency = true;
            imported.mipmapEnabled = true;
            imported.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D NormalTex(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material FenceWireMaterial()
        {
            string path = $"{MaterialFolder}/Fence_Wire.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", FenceWireTexture());
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", NormalTex($"{FenceFolder}/textures/modular_chainlink_fence_wire_nor_gl_2k.png"));
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cull", 0f);
            mat.SetFloat("_Metallic", 0.8f);
            mat.SetFloat("_Smoothness", 0.45f);
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material FencePostMaterial() =>
            ModelMat("modular_chainlink_fence", "modular_chainlink_fence_posts", 0.4f, 0.7f);

        /// <summary>One piece of the fence kit (a node of the FBX), pivot at holder, yawed and stretched along its length.</summary>
        static GameObject FencePiece(Transform parent, string node, Vector3 worldPos, float yaw, float lengthScale, Material wire, Material posts)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(FenceModelPath);
            Transform template = null;
            foreach (var t in asset.GetComponentsInChildren<Transform>(true)) if (t.name == node) { template = t; break; }
            if (template == null) return null;

            var holder = new GameObject(node);
            holder.transform.SetParent(parent, false);
            holder.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yaw, 0f));
            holder.transform.localScale = new Vector3(lengthScale, 1f, 1f);
            var clone = Object.Instantiate(template.gameObject);
            clone.name = "Mesh";
            clone.transform.SetParent(holder.transform, false);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = template.localRotation;
            clone.transform.localScale = template.localScale;
            foreach (var r in clone.GetComponentsInChildren<Renderer>())
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = slots[i] != null && slots[i].name.ToLowerInvariant().Contains("wire") ? wire : posts;
                r.sharedMaterials = slots;
            }
            return holder;
        }

        /// <summary>Fills a straight run (start to end, same height) with 2 m chain-link panels and posts at every joint.</summary>
        static int FenceRun(Transform parent, Vector3 start, Vector3 end, Material wire, Material posts, bool followTerrain = false)
        {
            var terrain = followTerrain ? Terrain.activeTerrain : null;
            Vector3 Grounded(Vector3 p) { if (terrain != null) p.y = terrain.SampleHeight(p) + terrain.transform.position.y - 0.05f; return p; }
            Vector3 d = end - start;
            float length = d.magnitude;
            if (length < 0.5f) return 0;
            int n = Mathf.Max(1, Mathf.RoundToInt(length / 2f));
            float step = length / n;
            float yaw = Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;                 // local +x runs along d
            Vector3 dir = d.normalized;
            for (int i = 0; i < n; i++)
            {
                // On a slope the panel sits at its lower end, the posts at their own ground height
                Vector3 p0 = Grounded(start + dir * (i * step)), p1 = Grounded(start + dir * ((i + 1) * step));
                p0.y = Mathf.Min(p0.y, p1.y);
                FencePiece(parent, "modular_chainlink_fence_double", p0, yaw, step / 2f, wire, posts);
            }
            for (int i = 0; i <= n; i++)
                FencePiece(parent, "modular_chainlink_fence_post", Grounded(start + dir * (i * step)), yaw, 1f, wire, posts);
            return n;
        }

        /// <summary>
        /// Chain-link fence around the radio tower compound, replacing the beige wall slabs (their colliders stay). The
        /// service gate gets a chain-link panel on its sliding leaf.
        /// </summary>
        static string BuildFence(Transform root)
        {
            if (!System.IO.File.Exists(FenceModelPath.Replace("Assets/", Application.dataPath + "/"))) return "fence model missing";
            var wire = FenceWireMaterial();
            var posts = FencePostMaterial();
            var group = Group(root, "ChainLinkFence");
            int panels = 0, runs = 0;

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (!t.name.StartsWith("Fence_") || t.root.name == RootName) continue;
                var r = t.GetComponent<Renderer>();
                if (r == null) continue;
                var b = r.bounds;
                r.enabled = false;
                bool alongX = b.size.x > b.size.z;
                Vector3 a = alongX ? new Vector3(b.min.x, b.min.y, b.center.z) : new Vector3(b.center.x, b.min.y, b.min.z);
                Vector3 e = alongX ? new Vector3(b.max.x, b.min.y, b.center.z) : new Vector3(b.center.x, b.min.y, b.max.z);
                panels += FenceRun(group, a, e, wire, posts, followTerrain: true);
                runs++;
            }

            // Service gate: a chain-link leaf on the sliding panel
            string gate = "no gate";
            var gateRoot = GameObject.Find("ServiceGate");
            var leaf = gateRoot != null ? gateRoot.transform.Find("Panel") : null;
            if (leaf != null && leaf.GetComponent<Renderer>() != null)
            {
                var b = leaf.GetComponent<Renderer>().bounds;
                leaf.GetComponent<Renderer>().enabled = false;
                var holder = new GameObject("ChainLinkLeaf").transform;
                holder.SetParent(leaf, false);
                var s = leaf.lossyScale;
                holder.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
                bool alongX = b.size.x > b.size.z;
                Vector3 a = alongX ? new Vector3(b.min.x, b.min.y, b.center.z) : new Vector3(b.center.x, b.min.y, b.min.z);
                Vector3 e = alongX ? new Vector3(b.max.x, b.min.y, b.center.z) : new Vector3(b.center.x, b.min.y, b.max.z);
                FenceRun(holder, a, e, wire, posts);
                // Top and bottom rails so the leaf reads as a gate frame
                Vector3 dir = (e - a).normalized;
                var steel = Steel("Fence_GateRail", new Color(0.5f, 0.52f, 0.55f));
                Bar(holder, "GateRailTop", a + Vector3.up * 2.4f, e + Vector3.up * 2.4f, 0.07f, steel);
                Bar(holder, "GateRailBottom", a + Vector3.up * 0.15f, e + Vector3.up * 0.15f, 0.07f, steel);
                gate = "gate leaf";
            }
            return $"{runs} runs, {panels} panels, {gate}";
        }
    }
}
