using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Phase 4 art pass for the island: paints the terrain with the Poly Haven textures (grass, dirt paths, rock, sand),
    /// sets the Belfast sunset sky, matches the sun, ambient light and fog to it, and plants Conifers [BOTD] trees
    /// away from roads, paths, buildings and patrol routes. Safe to re-run: the dirt paths already painted on the
    /// terrain are kept, and the trees are replaced. Bridge command: art-island.
    /// </summary>
    public static class IslandArtBuilder
    {
        const string IslandScene = "Assets/_Project/Scenes/IslandExterior.unity";
        const string TextureFolder = "Assets/_Project/Art/PolyHaven/Textures";
        const string SkyPath = "Assets/_Project/Art/PolyHaven/Sky/belfast_sunset_puresky_2k.hdr";
        const string OutFolder = "Assets/_Project/Art/Island";
        const string TreeFolder = "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs";

        // Layer order: 0 grass, 1 dirt (the path painters write layer 1), 2 rock, 3 sand
        const int Grass = 0, Dirt = 1, Rock = 2, Sand = 3;

        /// <summary>Where the sun sits as seen from the island (compass degrees): north-northeast, behind the radio tower.</summary>
        const float SunAzimuth = 30f;
        const float MinSunElevation = 10f; // keeps shadows from stretching across the whole island
        static readonly Vector2 HillCenter = new Vector2(130, 190);

        [MenuItem("Tools/Signal Lost/Art Pass – Island")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.OpenScene(IslandScene);
            var terrain = Terrain.activeTerrain;
            if (terrain == null) throw new System.InvalidOperationException("IslandExterior has no terrain.");
            if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Island");

            PaintTerrain(terrain);
            string sky = SetSkyAndLighting();
            int trees = PlantTrees(terrain);

            EditorUtility.SetDirty(terrain.terrainData);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorCommandBridge.Log($"art-island: terrain layers={terrain.terrainData.terrainLayers.Length}, {sky}, trees={trees} (rejected shore/slope/path/compound/blocked/crowded = {lastRejects})");
        }

        // ------------------------------------------------------------------ terrain

        static void PaintTerrain(Terrain terrain)
        {
            var data = terrain.terrainData;
            int res = data.alphamapResolution;
            var old = data.GetAlphamaps(0, 0, res, res);
            int oldLayers = old.GetLength(2);

            data.terrainLayers = new[]
            {
                Layer("Grass", "aerial_grass_rock", 6f),
                Layer("Dirt", "brown_mud_leaves_01", 4f),
                Layer("Rock", "rocky_terrain_02", 8f),
                Layer("Sand", "coast_sand_rocks_02", 5f)
            };

            var maps = new float[res, res, 4];
            for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                float u = xi / (float)(res - 1), v = zi / (float)(res - 1);
                float x = u * data.size.x, z = v * data.size.z;
                float height = data.GetInterpolatedHeight(u, v);
                float slope = data.GetSteepness(u, v);
                float dirt = oldLayers > Dirt ? old[zi, xi, Dirt] : 0f;

                // Sand on the shore below the 6 m plateau; rock on steep ground and the upper hill, broken up with noise
                float noise = Mathf.PerlinNoise(x * 0.06f + 13.1f, z * 0.06f + 7.7f);
                float sand = 1f - Mathf.InverseLerp(4.6f, 5.9f, height + (noise - 0.5f) * 0.6f);
                float rock = Mathf.Max(Mathf.InverseLerp(20f, 32f, slope),
                                       Mathf.InverseLerp(16f, 24f, height) * Mathf.Lerp(0.4f, 0.9f, noise));
                rock = Mathf.Clamp01(rock) * (1f - sand);
                float grass = Mathf.Clamp01(1f - sand - rock);

                float rest = 1f - dirt;
                maps[zi, xi, Grass] = grass * rest;
                maps[zi, xi, Dirt] = dirt;
                maps[zi, xi, Rock] = rock * rest;
                maps[zi, xi, Sand] = sand * rest;
            }
            data.SetAlphamaps(0, 0, maps);
        }

        static TerrainLayer Layer(string name, string texture, float tile)
        {
            string dir = $"{TextureFolder}/{texture}";
            var layer = new TerrainLayer
            {
                diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{texture}_diff_2k.jpg"),
                normalMapTexture = NormalMap($"{dir}/{texture}_nor_gl_2k.jpg"),
                tileSize = new Vector2(tile, tile),
                metallic = 0f,
                smoothness = 0.05f,
                normalScale = 1f
            };
            if (layer.diffuseTexture == null) throw new System.InvalidOperationException($"Missing texture {dir}");
            string path = $"{OutFolder}/Terrain_{name}.terrainlayer";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(layer, path);
            return layer;
        }

        static Texture2D NormalMap(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ sky and lighting

        /// <summary>Skybox from the HDRI, sun placed on the HDRI's sun, ambient and fog sampled from the sky.</summary>
        static string SetSkyAndLighting()
        {
            var hdr = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyPath);
            if (hdr == null) throw new System.InvalidOperationException($"Missing sky {SkyPath}");

            // Read the sky at low resolution (works whatever the texture's compression or Read/Write setting)
            const int w = 512, h = 256;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            Graphics.Blit(hdr, rt);
            var readback = new Texture2D(w, h, TextureFormat.RGBAHalf, false, true);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            readback.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            readback.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            var px = readback.GetPixels();
            Object.DestroyImmediate(readback);

            // The sun is the brightest pixel in the upper half
            int sunX = 0, sunY = h / 2;
            float best = 0f;
            for (int y = h / 2; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float lum = px[y * w + x].grayscale;
                if (lum > best) { best = lum; sunX = x; sunY = y; }
            }
            Vector3 sunInSky = PanoramaDirection((sunX + 0.5f) / w, (sunY + 0.5f) / h);

            // Rotate the skybox so the sun sits at SunAzimuth (Skybox/Panoramic turns the sky sphere by _Rotation)
            Vector3 wanted = Quaternion.Euler(0, SunAzimuth, 0) * Vector3.forward;
            float rotation = 0f, bestDot = -2f;
            for (int deg = 0; deg < 360; deg++)
            {
                Vector3 shown = RotateAroundY(sunInSky, deg);
                float dot = Vector2.Dot(new Vector2(shown.x, shown.z).normalized, new Vector2(wanted.x, wanted.z));
                if (dot > bestDot) { bestDot = dot; rotation = deg; }
            }

            var skyMat = AssetDatabase.LoadAssetAtPath<Material>($"{OutFolder}/Sky_BelfastSunset.mat");
            if (skyMat == null)
            {
                skyMat = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(skyMat, $"{OutFolder}/Sky_BelfastSunset.mat");
            }
            skyMat.SetTexture("_MainTex", hdr);
            skyMat.SetFloat("_Rotation", rotation);
            skyMat.SetFloat("_Exposure", 0.65f);
            skyMat.SetFloat("_Mapping", 1f);      // latitude-longitude layout
            skyMat.SetFloat("_ImageType", 0f);    // 360 degrees
            EditorUtility.SetDirty(skyMat);
            RenderSettings.skybox = skyMat;

            // Colors around the sun (sun light), at the zenith, and along the horizon
            Color sunColor = Average(px, w, h, sunX - 6, sunX + 6, sunY - 4, sunY + 4);
            Color zenith = Average(px, w, h, 0, w - 1, h - 12, h - 1);
            Color horizon = Average(px, w, h, 0, w - 1, h / 2, h / 2 + 10);

            float elevation = Mathf.Max(MinSunElevation, Mathf.Asin(Mathf.Clamp(sunInSky.y, -1f, 1f)) * Mathf.Rad2Deg);
            Light sun = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Directional) sun = l;
            if (sun == null)
            {
                sun = new GameObject("Directional Light").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.gameObject.SetActive(true);
            // The light shines from the sun toward the island
            sun.transform.rotation = Quaternion.Euler(elevation, SunAzimuth + 180f, 0f);
            sun.color = Color.Lerp(Normalized(sunColor, 1f), Color.white, 0.3f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Normalized(zenith, 0.55f).gamma;
            RenderSettings.ambientEquatorColor = Normalized(horizon, 0.5f).gamma;
            RenderSettings.ambientGroundColor = Normalized(horizon, 0.18f).gamma;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 480f;
            RenderSettings.fogColor = Normalized(horizon, 0.6f).gamma;

            PaintOcean();
            return $"sky rotation={rotation}, sun pixel=({sunX},{sunY}) elevation={elevation:F1}, sun color={sun.color}";
        }

        /// <summary>Gives the greybox Ocean plane the water shader (ripples, depth color, foam, sunset reflection).</summary>
        static void PaintOcean()
        {
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                if (r.name == "Ocean") r.sharedMaterial = WaterArtBuilder.OceanMaterial();
        }

        /// <summary>Direction for a latitude-longitude UV, matching Unity's Skybox/Panoramic shader.</summary>
        static Vector3 PanoramaDirection(float u, float v)
        {
            float lon = (0.5f - u) * 2f * Mathf.PI;
            float lat = (1f - v) * Mathf.PI;
            return new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(lon));
        }

        /// <summary>The rotation Skybox/Panoramic applies to the sky sphere (RotateAroundYInDegrees).</summary>
        static Vector3 RotateAroundY(Vector3 v, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, s = Mathf.Sin(a), c = Mathf.Cos(a);
            return new Vector3(c * v.x - s * v.z, v.y, s * v.x + c * v.z);
        }

        static Color Average(Color[] px, int w, int h, int x0, int x1, int y0, int y1)
        {
            Color sum = Color.black;
            int n = 0;
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
            for (int x = x0; x <= x1; x++)
            {
                sum += px[y * w + ((x % w) + w) % w];
                n++;
            }
            return n > 0 ? sum / n : Color.white;
        }

        /// <summary>Keeps the hue, scales so the brightest channel equals max.</summary>
        static Color Normalized(Color c, float max)
        {
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            return m <= 0f ? new Color(max, max, max) : new Color(c.r / m * max, c.g / m * max, c.b / m * max, 1f);
        }

        // ------------------------------------------------------------------ trees

        static string lastRejects = "";

        /// <summary>
        /// Prefab variant of a Conifers [BOTD] tree with a trunk collider on the root (terrain trees only collide
        /// through a root collider; the Medium tree ships with none and the others are very thin).
        /// </summary>
        static GameObject SolidTree(string name)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{TreeFolder}/PF Conifer {name} BOTD URP.prefab");
            if (source == null) return null;
            string folder = $"{OutFolder}/Trees";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(OutFolder, "Trees");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            var bounds = new Bounds(instance.transform.position, Vector3.zero);
            foreach (var r in instance.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

            var capsule = instance.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = instance.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = name == "Small" ? 0.25f : 0.4f;
            capsule.height = Mathf.Max(2f, bounds.size.y * 0.6f);
            capsule.center = new Vector3(0, capsule.height * 0.5f, 0);

            var variant = PrefabUtility.SaveAsPrefabAsset(instance, $"{folder}/Conifer {name} Solid.prefab");
            Object.DestroyImmediate(instance);
            return variant;
        }

        static int PlantTrees(Terrain terrain)
        {
            var data = terrain.terrainData;
            var prototypes = new List<TreePrototype>();
            foreach (var name in new[] { "Tall", "Medium", "Small", "Bare" })
            {
                var prefab = SolidTree(name);
                if (prefab != null) prototypes.Add(new TreePrototype { prefab = prefab });
            }
            if (prototypes.Count == 0) throw new System.InvalidOperationException($"No conifer prefabs in {TreeFolder}");
            data.treePrototypes = prototypes.ToArray();

            var blocked = BlockedAreas(terrain);
            var nearPath = NearPath(data, radiusMeters: 5f);
            int res = data.alphamapResolution;

            var rejected = new int[6]; // shore, slope, path, compound, blocked, crowded
            var rng = new System.Random(402);
            var placed = new List<Vector2>();
            var instances = new List<TreeInstance>();
            for (int attempt = 0; attempt < 30000 && instances.Count < 480; attempt++)
            {
                float x = 26f + (float)rng.NextDouble() * 198f;
                float z = 10f + (float)rng.NextDouble() * 219f;
                float u = x / data.size.x, v = z / data.size.z;
                if (data.GetInterpolatedHeight(u, v) < 5.8f) { rejected[0]++; continue; } // beach and shore
                if (data.GetSteepness(u, v) > 32f) { rejected[1]++; continue; }
                if (nearPath[Mathf.RoundToInt(v * (res - 1)), Mathf.RoundToInt(u * (res - 1))]) { rejected[2]++; continue; }
                var p = new Vector2(x, z);
                if (Vector2.Distance(p, HillCenter) < 24f) { rejected[3]++; continue; } // tower compound and wave gates
                if (IsBlocked(blocked, p)) { rejected[4]++; continue; }
                bool crowded = false;
                foreach (var other in placed)
                    if ((other - p).sqrMagnitude < 16f) { crowded = true; break; }
                if (crowded) { rejected[5]++; continue; }

                placed.Add(p);
                float scale = 0.8f + (float)rng.NextDouble() * 0.45f;
                int kind = rng.NextDouble() < 0.08 ? prototypes.Count - 1 : rng.Next(0, Mathf.Max(1, prototypes.Count - 1));
                instances.Add(new TreeInstance
                {
                    prototypeIndex = kind,
                    position = new Vector3(u, data.GetInterpolatedHeight(u, v) / data.size.y, v),
                    widthScale = scale,
                    heightScale = scale * (0.9f + (float)rng.NextDouble() * 0.2f),
                    rotation = (float)rng.NextDouble() * Mathf.PI * 2f,
                    color = Color.white,
                    lightmapColor = Color.white
                });
            }
            data.SetTreeInstances(instances.ToArray(), snapToHeightmap: true);
            data.RefreshPrototypes();
            terrain.treeDistance = 400f;
            terrain.treeBillboardDistance = 140f;
            terrain.Flush();
            lastRejects = string.Join("/", rejected);
            return instances.Count;
        }

        /// <summary>XZ rectangles (grown by a margin) around buildings, props and patrol waypoints.</summary>
        internal static List<Rect> BlockedAreas(Terrain terrain)
        {
            var rects = new List<Rect>();
            foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (col is TerrainCollider || col.isTrigger) continue; // triggers are zones, not solid things
                var b = col.bounds;
                if (b.size.x > 60f || b.size.z > 60f) continue;                   // boundary walls
                const float margin = 6f;
                rects.Add(Rect.MinMaxRect(b.min.x - margin, b.min.z - margin, b.max.x + margin, b.max.z + margin));
            }
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                bool waypoint = t.name.StartsWith("WP") || (t.parent != null && t.parent.name.StartsWith("Waypoints"))
                                || t.GetComponent<SpawnPoint>() != null;
                if (waypoint)
                    rects.Add(new Rect(t.position.x - 5f, t.position.z - 5f, 10f, 10f));
            }
            return rects;
        }

        internal static bool IsBlocked(List<Rect> rects, Vector2 p)
        {
            foreach (var r in rects)
                if (r.Contains(p)) return true;
            return false;
        }

        /// <summary>Alphamap cells within radius of any painted dirt (roads and paths).</summary>
        internal static bool[,] NearPath(TerrainData data, float radiusMeters)
        {
            int res = data.alphamapResolution;
            var maps = data.GetAlphamaps(0, 0, res, res);
            int r = Mathf.CeilToInt(radiusMeters / (data.size.x / (res - 1)));
            var near = new bool[res, res];
            for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                if (maps[zi, xi, Dirt] < 0.05f) continue;
                for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int z = zi + dz, x = xi + dx;
                    if (z >= 0 && z < res && x >= 0 && x < res && dx * dx + dz * dz <= r * r) near[z, x] = true;
                }
            }
            return near;
        }
    }
}
