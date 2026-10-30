using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Phase 4 art pass for the bunkers: turns the shared greybox wall, floor, ceiling, concrete and metal materials
    /// into textured Poly Haven materials (world-space triplanar, so stretched cubes never smear), then dresses the
    /// underground facility with military crates and red/cyan hazard lighting. The material change also reaches the
    /// island buildings and both island bunkers, since they share those materials. Safe to re-run: the dressing lives
    /// under one "ArtPass" object that is rebuilt each time. Bridge command: art-bunker.
    /// </summary>
    public static class BunkerArtBuilder
    {
        const string BunkerScene = "Assets/_Project/Scenes/UndergroundFacility.unity";
        const string TextureFolder = "Assets/_Project/Art/PolyHaven/Textures";
        const string CratePath = "Assets/_Project/Art/PolyHaven/Models/wooden_military_crate/wooden_military_crate_2k.fbx";
        const string ShaderName = "SignalLost/Triplanar Lit";
        const string RootName = "ArtPass";

        static readonly Color Cyan = new Color(0.2f, 0.85f, 1f);
        static readonly Color Red = new Color(1f, 0.12f, 0.08f);

        [MenuItem("Tools/Signal Lost/Art Pass – Bunkers")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            int materials = TextureMaterials();

            var scene = EditorSceneManager.OpenScene(BunkerScene);
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            BrightenRoomLights();
            int lights = AddHazardLighting(root);
            int crates = PlaceCrates(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorCommandBridge.Log($"art-bunker: textured materials={materials}, hazard lights={lights}, crates={crates}");
        }

        // ------------------------------------------------------------------ materials

        static int TextureMaterials()
        {
            if (Shader.Find(ShaderName) == null) throw new System.InvalidOperationException($"Shader {ShaderName} not found.");
            int count = 0;
            // Walls, floors and ceilings: plain concrete, tint white so the texture shows its own color
            count += Texture("Assets/_Project/Art/Greybox/Greybox_Wall.mat", "concrete_wall_008", 3f, new Color(1.5f, 1.5f, 1.45f), 0f, 0.6f);
            count += Texture("Assets/_Project/Art/Greybox/Greybox_Floor.mat", "concrete_floor_worn_001", 3f, new Color(1.35f, 1.35f, 1.35f), 0f, 0.7f);
            count += Texture("Assets/_Project/Art/Greybox/Greybox_Roof.mat", "concrete_floor_worn_001", 4f, new Color(0.8f, 0.8f, 0.85f), 0f, 0.4f);
            count += Texture("Assets/_Project/Materials/Bunker_Concrete.mat", "concrete_wall_008", 3f, new Color(1.3f, 1.3f, 1.25f), 0f, 0.6f);
            // Metal: keep a hint of each material's greybox color so doors and boxes stay recognizable
            count += Texture("Assets/_Project/Art/Greybox/Greybox_Metal.mat", "metal_plate", 2f, null, 0.35f, 0.6f, brightness: 2.4f);
            count += Texture("Assets/_Project/Materials/Door_Panel.mat", "metal_plate", 2f, null, 0.35f, 0.6f, brightness: 2.4f);
            count += Texture("Assets/_Project/Materials/Crate_Metal.mat", "metal_plate", 1.5f, null, 0.35f, 0.6f, brightness: 2.4f);
            count += Texture("Assets/_Project/Materials/Breaker_Box.mat", "metal_plate", 1.5f, null, 0.35f, 0.6f, brightness: 2.4f);
            return count;
        }

        /// <param name="tint">null keeps the material's current color, lightened halfway to white. Values above 1
        /// brighten the texture (the Poly Haven concrete is about half as bright as the old flat greybox grey).</param>
        /// <param name="brightness">Extra multiplier; the metal_plate albedo is very dark.</param>
        static int Texture(string path, string texture, float tile, Color? tint, float metallic, float smoothness, float brightness = 1f)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { Debug.LogWarning($"[Signal Lost] Missing material {path}"); return 0; }

            // Remember the greybox color once, so re-running doesn't keep lightening it
            bool first = mat.shader.name != ShaderName;
            Color original = first ? (mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white)
                                   : mat.GetColor("_GreyboxColor");

            string dir = $"{TextureFolder}/{texture}";
            mat.shader = Shader.Find(ShaderName);
            mat.SetColor("_GreyboxColor", original);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{texture}_diff_2k.jpg"));
            mat.SetTexture("_BumpMap", Import($"{dir}/{texture}_nor_gl_2k.jpg", TextureImporterType.NormalMap));
            mat.SetTexture("_ARMMap", Import($"{dir}/{texture}_arm_2k.jpg", TextureImporterType.Default, linear: true));
            var color = (tint ?? Color.Lerp(original, Color.white, 0.5f)) * brightness;
            color.a = 1f;
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_TileSize", tile);
            mat.SetFloat("_MetallicScale", metallic);
            mat.SetFloat("_SmoothnessScale", smoothness);
            mat.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            return 1;
        }

        static Texture2D Import(string path, TextureImporterType type, bool linear = false)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new System.InvalidOperationException($"Missing texture {path}");
            if (importer.textureType != type || (linear && importer.sRGBTexture))
            {
                importer.textureType = type;
                if (linear) importer.sRGBTexture = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ hazard lighting

        /// <summary>
        /// Cyan "systems online" accents in the elevator room and hub, red danger lighting along the laser corridor,
        /// a flickering cyan glow over the electrified water, and a pulsing red alarm in the quarantine room.
        /// Room centers follow GreyboxBuilder.BuildBunker.
        /// </summary>
        static int AddHazardLighting(Transform root)
        {
            var group = Group(root, "HazardLighting");
            var cyanStrip = Emissive("Light_StripCyan", Cyan * 3f);
            var redStrip = Emissive("Light_StripRed", Red * 3f);
            int n = 0;

            // Elevator room and hub: cyan ceiling strips and soft fill
            Strip(group, "Elevator_Strip", new Vector3(0, 3.95f, 0), new Vector3(0.12f, 0.05f, 6f), cyanStrip);
            n += Point(group, "Elevator_Cyan", new Vector3(0, 3.4f, 1.5f), Cyan, 1.2f, 7f);
            foreach (float x in new[] { -6f, 6f })
            {
                Strip(group, $"Hub_Strip_{x}", new Vector3(x, 3.95f, 20), new Vector3(0.12f, 0.05f, 13f), cyanStrip);
                n += Point(group, $"Hub_Cyan_{x}", new Vector3(x, 3.3f, 20), Cyan, 1.4f, 9f);
            }

            // Wing A laser corridor: red strip down the middle and red pools along it
            Strip(group, "Laser_Strip", new Vector3(-19, 3.95f, 20), new Vector3(20f, 0.05f, 0.12f), redStrip);
            foreach (float x in new[] { -11f, -19f, -27f })
                n += Point(group, $"Laser_Red_{x}", new Vector3(x, 3.3f, 20), Red, 1.6f, 6f);

            // Wing B flooded generator: unstable cyan glow off the water, red warning lamps by the doors
            var water = Point(group, "Water_Glow", new Vector3(26, 0.6f, 20), Cyan, 2.2f, 12f);
            n += water;
            Flicker(group.Find("Water_Glow"), LightFlicker.Mode.Flicker, 7f, 0.25f);
            n += Point(group, "Generator_Warning_W", new Vector3(16.6f, 3.4f, 20), Red, 1.5f, 6f);
            n += Point(group, "Generator_Warning_E", new Vector3(35.4f, 3.4f, 20), Red, 1.5f, 6f);

            // Quarantine room: pulsing red alarm beacon
            n += Point(group, "Quarantine_Alarm", new Vector3(44, 3.5f, 20), Red, 2.5f, 13f);
            Flicker(group.Find("Quarantine_Alarm"), LightFlicker.Mode.Pulse, 0.8f, 0.2f);
            Strip(group, "Quarantine_Strip", new Vector3(44, 3.95f, 20), new Vector3(13f, 0.05f, 0.12f), redStrip);

            // Breaker room: a little cyan on the fuse pod
            n += Point(group, "Breaker_Cyan", new Vector3(-40, 2.6f, 20), Cyan, 1.2f, 5f);
            return n;
        }

        /// <summary>
        /// The greybox ceiling lamps (intensity 3) were tuned for flat light-grey walls; the concrete textures are
        /// about half as bright, so the lamps and the flat ambient go up to keep the rooms readable but moody.
        /// Absolute values, so re-running doesn't keep brightening.
        /// </summary>
        static void BrightenRoomLights()
        {
            var greybox = GameObject.Find("Greybox");
            if (greybox != null)
                foreach (var light in greybox.GetComponentsInChildren<Light>(true))
                    if (light.type == LightType.Point && light.name == "Light")
                    {
                        light.intensity = 14f;
                        light.range = Mathf.Max(light.range, 16f);
                        light.color = new Color(0.9f, 0.93f, 1f);
                    }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.23f, 0.27f);
        }

        static int Point(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return 1;
        }

        static void Flicker(Transform target, LightFlicker.Mode mode, float speed, float minimum)
        {
            var flicker = target.gameObject.AddComponent<LightFlicker>();
            var so = new SerializedObject(flicker);
            so.FindProperty("mode").enumValueIndex = (int)mode;
            so.FindProperty("speed").floatValue = speed;
            so.FindProperty("minimum").floatValue = minimum;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Thin glowing bar on the ceiling (no collider); bloom turns it into a light strip.</summary>
        static void Strip(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = name;
            Object.DestroyImmediate(strip.GetComponent<Collider>());
            strip.transform.SetParent(parent, false);
            strip.transform.position = position;
            strip.transform.localScale = size;
            var renderer = strip.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        static Material Emissive(string name, Color emission) =>
            Phase2ContentBuilder.Mat(name, emission.linear * 0.2f, emission);

        // ------------------------------------------------------------------ crates

        /// <summary>Stacks of military crates in room corners, skipped wherever they would touch gameplay objects.</summary>
        static int PlaceCrates(Transform root)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CratePath);
            if (model == null) { Debug.LogWarning($"[Signal Lost] Missing crate model {CratePath}"); return 0; }
            var group = Group(root, "Crates");

            // (position on the floor, yaw)
            var spots = new List<(Vector3, float)>
            {
                (new Vector3(2.8f, 0, 2.8f), 10f),        // elevator room
                (new Vector3(6.6f, 0, 26.6f), -5f),       // hub corners
                (new Vector3(6.6f, 0, 13.4f), 95f),
                (new Vector3(-6.6f, 0, 13.4f), 80f),
                (new Vector3(-31.6f, 0, 25.4f), 0f),      // breaker room
                (new Vector3(-31.6f, 0, 14.6f), 90f),
                (new Vector3(50.6f, 0, 26.6f), 15f),      // quarantine room
                (new Vector3(37.4f, 0, 13.4f), -20f),
                (new Vector3(50.6f, 0, 13.4f), 90f),
            };

            var solids = new List<Bounds>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                string n = r.name;
                if (n == "Floor" || n == "Ceiling" || n.StartsWith("Wall") || r.transform.IsChildOf(root)) continue;
                if (r.bounds.size.x > 15f || r.bounds.size.z > 15f) continue; // water surface and other room-sized things
                solids.Add(r.bounds);
            }

            int placed = 0;
            foreach (var (spot, yaw) in spots)
            {
                // Holder carries the yaw; the model keeps its own import rotation underneath
                var crate = new GameObject("MilitaryCrate");
                crate.transform.SetParent(group, false);
                crate.transform.SetPositionAndRotation(spot, Quaternion.Euler(0, yaw, 0));
                var mesh = (GameObject)PrefabUtility.InstantiatePrefab(model);
                mesh.transform.SetParent(crate.transform, false);

                var bounds = WorldBounds(crate);
                crate.transform.position += Vector3.up * (spot.y - bounds.min.y); // sit on the floor
                bounds = WorldBounds(crate);
                var padded = bounds;
                padded.Expand(new Vector3(0.6f, 0f, 0.6f));

                bool blocked = false;
                foreach (var s in solids)
                    if (s.Intersects(padded)) { blocked = true; break; }
                if (blocked) { Object.DestroyImmediate(crate); continue; }

                var box = crate.AddComponent<BoxCollider>();
                box.center = crate.transform.InverseTransformPoint(bounds.center);
                box.size = Quaternion.Inverse(crate.transform.rotation) * bounds.size;
                box.size = new Vector3(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y), Mathf.Abs(box.size.z));
                var obstacle = crate.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.center = box.center;
                obstacle.size = box.size;
                GameObjectUtility.SetStaticEditorFlags(crate, StaticEditorFlags.BatchingStatic);
                solids.Add(bounds);
                placed++;
            }
            return placed;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
