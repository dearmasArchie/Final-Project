using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Phase 4 art pass for props: puts the Poly Haven crate, barrel and medical box models on the SupplyCrate,
    /// ExplosiveBarrel and Medkit prefabs (the greybox shape's collider and the gameplay scripts stay; only its mesh is
    /// hidden), and scatters boulders and mossy rocks along the coast, the hill trail and the open ground. Rocks carve
    /// the NavMesh so enemies walk around them. Safe to re-run; Phase2ContentBuilder also calls DressPrefabs after it
    /// rebuilds the prefabs. Bridge command: art-props.
    /// </summary>
    public static class PropArtBuilder
    {
        const string ModelFolder = "Assets/_Project/Art/PolyHaven/Models";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string MaterialFolder = "Assets/_Project/Art/Props";
        const string IslandScene = "Assets/_Project/Scenes/IslandExterior.unity";
        const string ModelName = "Model";
        const string RockRootName = "ArtPass_Rocks";

        [MenuItem("Tools/Signal Lost/Art Pass – Props and Rocks")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            string props = DressPrefabs();

            var scene = EditorSceneManager.OpenScene(IslandScene);
            int rocks = PlaceRocks(Terrain.activeTerrain);
            string puddles = PlacePuddles(Terrain.activeTerrain, 7);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorCommandBridge.Log($"art-props: {props}, rocks={rocks}, puddles={puddles}");
        }

        // ------------------------------------------------------------------ puddles

        const string PuddleRootName = "ArtPass_Puddles";
        const string PuddleFolder = "Assets/_Project/Art/Island/Puddles";

        /// <summary>
        /// Rain puddles on flat stretches of the dirt roads and paths: irregular flat water shapes that follow the
        /// ground, glossy so they mirror the sunset sky. No collider (the player walks through them). Spaced at least
        /// 25 m apart and kept clear of buildings and rocks.
        /// </summary>
        static string PlacePuddles(Terrain terrain, int count)
        {
            var old = GameObject.Find(PuddleRootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(PuddleRootName).transform;
            if (!AssetDatabase.IsValidFolder(PuddleFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art/Island", "Puddles");

            var data = terrain.terrainData;
            int res = data.alphamapResolution;
            var maps = data.GetAlphamaps(0, 0, res, res);
            var blocked = IslandArtBuilder.BlockedAreas(terrain);
            var rocks = GameObject.Find(RockRootName);
            var material = PuddleMaterial();
            var rng = new System.Random(77);
            var placed = new List<Vector2>();
            var where = new List<string>();

            for (int attempt = 0; attempt < 6000 && placed.Count < count; attempt++)
            {
                float x = 30f + (float)rng.NextDouble() * 190f;
                float z = 15f + (float)rng.NextDouble() * 210f;
                float u = x / data.size.x, v = z / data.size.z;
                int ax = Mathf.RoundToInt(u * (res - 1)), az = Mathf.RoundToInt(v * (res - 1));
                if (maps[az, ax, 1] < 0.6f) continue;                            // on a dirt road or path
                if (data.GetSteepness(u, v) > 4f) continue;                      // water only sits on flat ground
                var p = new Vector2(x, z);
                if (IslandArtBuilder.IsBlocked(blocked, p)) continue;
                bool crowded = false;
                foreach (var other in placed)
                    if ((other - p).sqrMagnitude < 25f * 25f) { crowded = true; break; }
                if (crowded) continue;
                if (rocks != null)
                    foreach (Transform rock in rocks.transform)
                        if ((new Vector2(rock.position.x, rock.position.z) - p).sqrMagnitude < 9f) { crowded = true; break; }
                if (crowded) continue;

                float size = Mathf.Lerp(0.9f, 1.8f, (float)rng.NextDouble());
                var mesh = PuddleMesh(terrain, p, size, rng, $"Puddle_{placed.Count}");
                var go = new GameObject($"Puddle_{placed.Count}");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                placed.Add(p);
                where.Add($"({x:0},{z:0})");
            }
            return $"{placed.Count} at {string.Join(" ", where)}";
        }

        /// <summary>
        /// A wobbly disc (noise on the edge radius) in world space, each vertex lifted 2 cm above the terrain so it
        /// follows small bumps without z-fighting. Saved as an asset so the scene keeps it.
        /// </summary>
        static Mesh PuddleMesh(Terrain terrain, Vector2 center, float radius, System.Random rng, string name)
        {
            const int segments = 28, rings = 3;
            float seed = (float)rng.NextDouble() * 100f;
            float stretch = Mathf.Lerp(1f, 1.7f, (float)rng.NextDouble());   // oval puddles
            float angle = (float)rng.NextDouble() * Mathf.PI;
            var rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            Vector3 Point(float r, float a)
            {
                float edge = 1f + 0.28f * (Mathf.PerlinNoise(seed + Mathf.Cos(a) * 1.3f, seed + Mathf.Sin(a) * 1.3f) - 0.5f) * 2f;
                var local = rotation * new Vector3(Mathf.Cos(a) * r * edge * stretch, 0f, Mathf.Sin(a) * r * edge);
                var world = new Vector3(center.x + local.x, 0f, center.y + local.z);
                world.y = terrain.SampleHeight(world) + terrain.transform.position.y + 0.02f;
                return world;
            }

            // Center vertex, then rings out to the edge
            var c = new Vector3(center.x, 0f, center.y);
            c.y = terrain.SampleHeight(c) + terrain.transform.position.y + 0.02f;
            vertices.Add(c);
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int ring = 1; ring <= rings; ring++)
                for (int s = 0; s < segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    vertices.Add(Point(radius * ring / rings, a));
                    uvs.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f * ring / rings, 0.5f + Mathf.Sin(a) * 0.5f * ring / rings));
                }
            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;
                triangles.AddRange(new[] { 0, 1 + next, 1 + s });
                for (int ring = 1; ring < rings; ring++)
                {
                    int a0 = 1 + (ring - 1) * segments + s, a1 = 1 + (ring - 1) * segments + next;
                    int b0 = 1 + ring * segments + s, b1 = 1 + ring * segments + next;
                    triangles.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string path = $"{PuddleFolder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>Dark, near-mirror water: mostly reflection of the sky, a little of the mud colour underneath.</summary>
        static Material PuddleMaterial()
        {
            string path = $"{PuddleFolder}/Puddle_Water.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(0.06f, 0.07f, 0.07f));
            mat.SetFloat("_Smoothness", 0.97f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_EnvironmentReflections", 1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ prefabs

        /// <summary>Puts the models on the three prop prefabs. Returns a short report.</summary>
        public static string DressPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Props");

            var crateMat = ModelMaterial("Prop_WoodenCrate", "wooden_crate_01", "wooden_crate_01_diff_2k.jpg", "wooden_crate_01_nor_gl_2k.exr", 0.25f);
            var barrelMat = ModelMaterial("Prop_ExplosiveBarrel", "Barrel_01", "Barrel_01_explosive_diff_2k.png", "Barrel_01_explosive_nor_gl_2k.png", 0.45f, metallic: 0.6f);
            var medMat = ModelMaterial("Prop_MedicalBox", "medical_box", "medical_box_diff_2k.jpg", "medical_box_nor_gl_2k.exr", 0.4f);

            var crate = Dress($"{PrefabFolder}/Props/SupplyCrate.prefab", "wooden_crate_01/wooden_crate_01_2k.fbx", crateMat, "Body");
            var barrel = Dress($"{PrefabFolder}/Props/ExplosiveBarrel.prefab", "Barrel_01/Barrel_01_2k.fbx", barrelMat, "Body", "HazardStripe");
            var med = Dress($"{PrefabFolder}/Pickups/Medkit.prefab", "medical_box/medical_box_2k.fbx", medMat, null);
            return $"crate={crate}, barrel={barrel}, medkit={med}";
        }

        /// <summary>
        /// Hides the greybox renderers (bodyName child, or the root when null, plus any extra children), and fits the
        /// model inside the greybox shape's bounds: same footprint center, resting on the same bottom.
        /// </summary>
        static string Dress(string prefabPath, string modelPath, Material material, string bodyName, params string[] alsoHide)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{modelPath}");
            if (model == null) return "missing model";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var old = root.transform.Find(ModelName);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var body = bodyName == null ? root.transform : root.transform.Find(bodyName);
                var bodyRenderer = body.GetComponent<Renderer>();
                bodyRenderer.enabled = true; // bounds are only valid on an enabled renderer
                var target = bodyRenderer.bounds;
                bodyRenderer.enabled = false;
                foreach (var name in alsoHide)
                {
                    var extra = root.transform.Find(name);
                    if (extra != null) extra.GetComponent<Renderer>().enabled = false;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.name = ModelName;
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                foreach (var col in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
                foreach (var r in instance.GetComponentsInChildren<Renderer>())
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = material;
                    r.sharedMaterials = mats;
                }

                // Uniform world scale that fits inside the greybox shape; undo any (non-uniform) root scale
                var b = Bounds(instance);
                float s = Mathf.Min(target.size.x / b.size.x, target.size.y / b.size.y, target.size.z / b.size.z);
                var parentScale = root.transform.lossyScale;
                var local = instance.transform.localScale * s;
                instance.transform.localScale = new Vector3(local.x / parentScale.x, local.y / parentScale.y, local.z / parentScale.z);

                b = Bounds(instance);
                instance.transform.position += new Vector3(target.center.x - b.center.x, target.min.y - b.min.y, target.center.z - b.center.z);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return $"scale {s:F2}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>URP Lit material from a Poly Haven model's albedo and OpenGL normal map.</summary>
        static Material ModelMaterial(string name, string folder, string diffuse, string normal, float smoothness, float metallic = 0f)
        {
            string dir = $"{ModelFolder}/{folder}/textures";
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{diffuse}"));
            mat.SetColor("_BaseColor", Color.white);
            var normalPath = $"{dir}/{normal}";
            var importer = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ rocks

        struct RockMesh { public Mesh mesh; public Material material; public float size; }

        /// <summary>
        /// Boulders and mossy rocks: a ring along the shore, clusters beside the hill trail, and a few on open ground.
        /// Kept off roads and paths, away from buildings, patrol points and trees.
        /// </summary>
        static int PlaceRocks(Terrain terrain)
        {
            var old = GameObject.Find(RockRootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RockRootName).transform;

            var meshes = new List<RockMesh>();
            AddRockMeshes(meshes, "boulder_01/boulder_01_2k.fbx",
                ModelMaterial("Rock_Boulder", "boulder_01", "boulder_01_diff_2k.jpg", "boulder_01_nor_gl_2k.exr", 0.15f));
            AddRockMeshes(meshes, "rock_moss_set_01/rock_moss_set_01_2k.fbx",
                ModelMaterial("Rock_MossSet", "rock_moss_set_01", "rock_moss_set_01_diff_2k.jpg", "rock_moss_set_01_nor_gl_2k.exr", 0.1f));
            if (meshes.Count == 0) return 0;

            var data = terrain.terrainData;
            var blocked = IslandArtBuilder.BlockedAreas(terrain);
            var onPath = IslandArtBuilder.NearPath(data, 3f);
            var nearPath = IslandArtBuilder.NearPath(data, 9f);
            int res = data.alphamapResolution;

            var taken = new List<Vector2>();
            foreach (var tree in data.treeInstances) taken.Add(new Vector2(tree.position.x * data.size.x, tree.position.z * data.size.z));
            int treeCount = taken.Count;

            var rng = new System.Random(4021);
            int placed = 0;
            // (zone, how many, size range in meters)
            placed += Scatter(root, terrain, meshes, rng, blocked, onPath, nearPath, res, taken, treeCount, Zone.Shore, 40, 1.2f, 3.2f);
            placed += Scatter(root, terrain, meshes, rng, blocked, onPath, nearPath, res, taken, treeCount, Zone.Trail, 24, 0.6f, 1.8f);
            placed += Scatter(root, terrain, meshes, rng, blocked, onPath, nearPath, res, taken, treeCount, Zone.Open, 16, 0.5f, 1.5f);
            return placed;
        }

        enum Zone { Shore, Trail, Open }

        static int Scatter(Transform root, Terrain terrain, List<RockMesh> meshes, System.Random rng, List<Rect> blocked,
            bool[,] onPath, bool[,] nearPath, int res, List<Vector2> taken, int treeCount, Zone zone, int count, float minSize, float maxSize)
        {
            var data = terrain.terrainData;
            int placed = 0;
            for (int attempt = 0; attempt < count * 60 && placed < count; attempt++)
            {
                float x = 24f + (float)rng.NextDouble() * 202f;
                float z = 8f + (float)rng.NextDouble() * 223f;
                float u = x / data.size.x, v = z / data.size.z;
                float h = data.GetInterpolatedHeight(u, v);
                int ax = Mathf.RoundToInt(u * (res - 1)), az = Mathf.RoundToInt(v * (res - 1));

                bool ok = zone switch
                {
                    Zone.Shore => h > 3.6f && h < 5.9f,
                    Zone.Trail => h > 8f && nearPath[az, ax] && !onPath[az, ax],
                    _ => h >= 5.9f && h < 8f && !nearPath[az, ax]
                };
                if (!ok || onPath[az, ax] || data.GetSteepness(u, v) > 35f) continue;
                var p = new Vector2(x, z);
                if (IslandArtBuilder.IsBlocked(blocked, p)) continue;

                float size = Mathf.Lerp(minSize, maxSize, (float)rng.NextDouble());
                bool crowded = false;
                for (int i = 0; i < taken.Count; i++)
                {
                    float gap = i < treeCount ? 1.5f + size * 0.5f : size + 1.5f;
                    if ((taken[i] - p).sqrMagnitude < gap * gap) { crowded = true; break; }
                }
                if (crowded) continue;

                var rock = meshes[rng.Next(meshes.Count)];
                var go = new GameObject($"Rock_{zone}_{placed}");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = rock.mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = rock.material;
                go.AddComponent<MeshCollider>().sharedMesh = rock.mesh;
                go.layer = LayerMask.NameToLayer("Ground");

                float scale = size / rock.size;
                go.transform.localScale = Vector3.one * scale * Mathf.Lerp(0.85f, 1.15f, (float)rng.NextDouble());
                // Lay the rock on its flattest side (the mesh's shortest axis points up), then a random yaw and slight tilt
                var ms = rock.mesh.bounds.size;
                var flat = ms.x <= ms.y && ms.x <= ms.z ? Quaternion.Euler(0, 0, 90) : ms.z <= ms.y ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;
                go.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 12f - 6f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 12f - 6f) * flat;
                // Sink a quarter of the rock into the ground so it doesn't float on slopes (bounds measured at the origin)
                var bounds = go.GetComponent<MeshRenderer>().bounds;
                go.transform.position = new Vector3(x, h - bounds.min.y - bounds.size.y * 0.25f, z);

                var local = rock.mesh.bounds;
                var obstacle = go.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                obstacle.center = local.center;
                obstacle.size = local.size;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);

                taken.Add(p);
                placed++;
            }
            return placed;
        }

        /// <summary>Every mesh in the FBX, with its largest dimension in meters (model import scale applied).</summary>
        static void AddRockMeshes(List<RockMesh> list, string modelPath, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{modelPath}");
            if (model == null) { Debug.LogWarning($"[Signal Lost] Missing rock model {modelPath}"); return; }
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null) continue;
                var s = mesh.bounds.size;
                float size = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
                if (size > 0.01f) list.Add(new RockMesh { mesh = mesh, material = material, size = size });
            }
        }
    }
}
