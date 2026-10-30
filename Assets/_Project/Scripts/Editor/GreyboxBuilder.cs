using System;
using System.Collections.Generic;
using SignalLost.DebugTools;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Generates the Phase 1 greybox for both gameplay scenes: island terrain and blockout buildings,
    /// the bunker rooms, spawn points, elevator portals, the HUD, a debug player, and a baked NavMesh.
    /// Everything generated lives under a root object named "Greybox". Re-running deletes and rebuilds it,
    /// so move hand-made work out of "Greybox" (or stop using the tool) once you start editing the layout.
    /// Layout and distances follow GameConcept.md, section 7.
    /// </summary>
    public static class GreyboxBuilder
    {
        const string RootName = "Greybox";
        const string ScenesFolder = "Assets/_Project/Scenes";
        const string DataFolder = "Assets/_Project/Art/Greybox";

        const float WallThickness = 0.3f;
        const float DoorHeight = 2.6f;

        static Material floorMat, wallMat, roofMat, interactMat, hazardMat, waterMat, metalMat;

        // ------------------------------------------------------------------ menu

        [MenuItem("Tools/Signal Lost/Build Greybox (Both Scenes)")]
        public static void BuildFromMenu() => BuildBoth(interactive: true);

        /// <summary>interactive = false skips the dialogs and saves open scenes automatically (command bridge).</summary>
        public static void BuildBoth(bool interactive)
        {
            if (interactive)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (!ConfirmRebuild()) return;
            }
            else
            {
                EditorSceneManager.SaveOpenScenes();
            }
            if (!EnsureHud()) throw new InvalidOperationException("HUD prefab could not be built (TextMeshPro resources missing?).");

            BuildIsland();
            BuildBunker();
            EditorSceneManager.OpenScene($"{ScenesFolder}/IslandExterior.unity");
            Debug.Log("[Signal Lost] Greybox built for IslandExterior and UndergroundFacility.");
        }

        static bool ConfirmRebuild() => EditorUtility.DisplayDialog(
            "Build greybox?",
            "This opens IslandExterior and UndergroundFacility and rebuilds the \"Greybox\" object in each. " +
            "Anything inside an existing \"Greybox\" object is deleted.",
            "Build", "Cancel");

        static bool EnsureHud() =>
            AssetDatabase.LoadAssetAtPath<GameObject>(HUDPrefabBuilder.PrefabPath) != null || HUDPrefabBuilder.Build() != null;

        // ------------------------------------------------------------------ island

        static readonly Vector2 HillCenter = new Vector2(130, 190);

        static void BuildIsland()
        {
            var scene = EditorSceneManager.OpenScene($"{ScenesFolder}/IslandExterior.unity");
            var root = ResetRoot();
            CreateMaterials();

            var terrain = CreateTerrain(root.transform);
            float Ground(float x, float z) => terrain.SampleHeight(new Vector3(x, 0, z));
            Vector3 At(float x, float z, float yOffset = 0f) => new Vector3(x, Ground(x, z) + yOffset, z);

            // Water around the island (visual only; the boundary walls stop the player)
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Ocean";
            water.transform.SetParent(root.transform);
            water.transform.position = new Vector3(125, 4f, 125);
            water.transform.localScale = new Vector3(40, 1, 40);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.GetComponent<Renderer>().sharedMaterial = waterMat;

            // Zone 1: safe room + armory (south)
            var safeRoom = Group(root.transform, "Zone1_SafeRoom");
            float safeY = Ground(90, 45);
            Room(safeRoom, "SafeRoom", new Vector3(90, safeY, 45), new Vector3(16, 3.5f, 9),
                north: 3f, south: -1, east: -1, west: -1);
            Wall(safeRoom, "ArmoryPartition", new Vector3(86, safeY, 40.5f), new Vector3(86, safeY, 49.5f), 3.5f,
                doorCenter: 4.5f, doorWidth: 1.6f);
            Spawn(safeRoom, "SafeRoom", new Vector3(93, safeY, 43), 0f, isDefault: true);

            // Zone 1: pier, boss arena, extraction boat
            var pier = Group(root.transform, "Zone1_Pier");
            Box(pier, "PierDeck", new Vector3(140, 6.1f, 22), new Vector3(24, 0.4f, 30), floorMat);
            Box(pier, "Container_A", new Vector3(134, 7.6f, 26), new Vector3(2.5f, 2.6f, 6), metalMat);
            Box(pier, "Container_B", new Vector3(146, 7.6f, 18), new Vector3(6, 2.6f, 2.5f), metalMat);
            Box(pier, "Container_C", new Vector3(137, 7.6f, 13), new Vector3(2.5f, 2.6f, 6), metalMat);
            Box(pier, "Pylon_West", new Vector3(131, 9.3f, 10), new Vector3(1, 6, 1), hazardMat);
            Box(pier, "Pylon_East", new Vector3(149, 9.3f, 10), new Vector3(1, 6, 1), hazardMat);
            Box(pier, "ExtractionBoat", new Vector3(155.5f, 4.8f, 20), new Vector3(3, 2, 8), interactMat);

            // Zone 2: freight elevator house (center)
            var elevator = Group(root.transform, "Zone2_ElevatorHouse");
            float elevY = Ground(125, 110);
            Room(elevator, "ElevatorHouse", new Vector3(125, elevY, 110), new Vector3(8, 4, 8),
                north: -1, south: 0f, east: -1, west: -1);
            Box(elevator, "ElevatorPlatform", new Vector3(125, elevY + 0.1f, 111.5f), new Vector3(3, 0.2f, 3), interactMat);
            Portal(elevator, "ElevatorPanel_Down", new Vector3(127.3f, elevY + 1.3f, 113.5f),
                "Ride Elevator Down", "UndergroundFacility", "ElevatorBottom");
            Spawn(elevator, "ElevatorTop", new Vector3(125, elevY, 109), 180f);

            // Zone 3: radio tower compound (north hilltop)
            var tower = Group(root.transform, "Zone3_RadioTower");
            float towerY = Ground(HillCenter.x, HillCenter.y);
            Fence(tower, new Vector3(HillCenter.x, 0, HillCenter.y), 26f, terrain);
            Box(tower, "RadioTower", new Vector3(HillCenter.x, towerY + 12.5f, HillCenter.y + 4), new Vector3(3, 25, 3), metalMat);
            Box(tower, "FuseConsole", At(HillCenter.x - 3, HillCenter.y, 0.6f), new Vector3(1.2f, 1.2f, 0.6f), interactMat);
            Marker(tower, "WaveGate_1", At(HillCenter.x - 18, HillCenter.y));
            Marker(tower, "WaveGate_2", At(HillCenter.x, HillCenter.y + 18));
            Marker(tower, "WaveGate_3", At(HillCenter.x + 14, HillCenter.y + 14));

            // Play-area boundary
            var bounds = Group(root.transform, "Boundary");
            InvisibleWall(bounds, "North", new Vector3(125, 25, 235), new Vector3(230, 50, 1));
            InvisibleWall(bounds, "South", new Vector3(125, 25, 4), new Vector3(230, 50, 1));
            InvisibleWall(bounds, "East", new Vector3(230, 25, 120), new Vector3(1, 50, 232));
            InvisibleWall(bounds, "West", new Vector3(20, 25, 120), new Vector3(1, 50, 232));

            AddSceneEssentials(root, new Vector3(93, safeY, 43), dark: false);
            BakeNavMesh(root, "IslandExterior");
            EditorSceneManager.SaveScene(scene);
        }

        static Terrain CreateTerrain(Transform parent)
        {
            EnsureFolder(DataFolder);
            var data = new TerrainData { heightmapResolution = 257, alphamapResolution = 256 };
            data.size = new Vector3(250, 60, 250);

            int res = data.heightmapResolution;
            var heights = new float[res, res];
            for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                float x = xi / (float)(res - 1) * 250f, z = zi / (float)(res - 1) * 250f;
                heights[zi, xi] = IslandHeight(x, z) / data.size.y;
            }
            data.SetHeights(0, 0, heights);

            data.terrainLayers = new[]
            {
                SolidLayer("Grass", new Color(0.33f, 0.45f, 0.25f)),
                SolidLayer("Dirt", new Color(0.45f, 0.36f, 0.25f))
            };
            PaintRoads(data);

            CreateOrReplace(data, $"{DataFolder}/IslandTerrain.asset");

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "IslandTerrain";
            go.transform.SetParent(parent);
            go.layer = LayerMask.NameToLayer("Ground");
            return go.GetComponent<Terrain>();
        }

        /// <summary>Flat land at 6 m, sloping into the sea near the edges, with a plateaued hill for the radio tower.</summary>
        static float IslandHeight(float x, float z)
        {
            float r = Vector2.Distance(new Vector2(x, z), new Vector2(125, 125)) / 110f;
            float land = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1.05f, r));

            float d2 = (new Vector2(x, z) - HillCenter).sqrMagnitude;
            float hill = Mathf.Clamp01(Mathf.Exp(-d2 / (2f * 25f * 25f)) * 1.3f);

            return (6f + 22f * hill) * land;
        }

        static readonly Vector2[][] Roads =
        {
            // Safe room -> elevator house
            new[] { new Vector2(93, 50), new Vector2(110, 80), new Vector2(125, 105) },
            // Elevator house -> tower compound (hill trail)
            new[] { new Vector2(125, 114), new Vector2(120, 150), new Vector2(130, 176) },
            // Tower compound -> pier (service road, opens after the upload)
            new[] { new Vector2(144, 190), new Vector2(175, 150), new Vector2(165, 90), new Vector2(145, 45), new Vector2(140, 38) }
        };

        static void PaintRoads(TerrainData data)
        {
            int res = data.alphamapResolution;
            var maps = new float[res, res, 2];
            for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                var p = new Vector2(xi / (float)(res - 1) * 250f, zi / (float)(res - 1) * 250f);
                float dist = float.MaxValue;
                foreach (var road in Roads)
                    for (int i = 0; i < road.Length - 1; i++)
                        dist = Mathf.Min(dist, DistanceToSegment(p, road[i], road[i + 1]));

                float dirt = 1f - Mathf.InverseLerp(2.5f, 3.5f, dist);
                maps[zi, xi, 0] = 1f - dirt;
                maps[zi, xi, 1] = dirt;
            }
            data.SetAlphamaps(0, 0, maps);
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + t * ab);
        }

        static TerrainLayer SolidLayer(string name, Color color)
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            CreateOrReplace(tex, $"{DataFolder}/{name}Color.asset");

            var layer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(2, 2) };
            CreateOrReplace(layer, $"{DataFolder}/{name}.terrainlayer");
            return layer;
        }

        static void Fence(Transform parent, Vector3 center, float size, Terrain terrain)
        {
            float h = 2.5f, half = size / 2f;
            float y = terrain.SampleHeight(center);
            var c = new Vector3(center.x, y, center.z);
            // Gates: south (hill trail) and east (service road)
            Wall(parent, "Fence_S", c + new Vector3(-half, 0, -half), c + new Vector3(half, 0, -half), h, size / 2f, 4f);
            Wall(parent, "Fence_N", c + new Vector3(-half, 0, half), c + new Vector3(half, 0, half), h);
            Wall(parent, "Fence_E", c + new Vector3(half, 0, -half), c + new Vector3(half, 0, half), h, size / 2f, 4f);
            Wall(parent, "Fence_W", c + new Vector3(-half, 0, -half), c + new Vector3(-half, 0, half), h);
        }

        // ------------------------------------------------------------------ bunker

        static void BuildBunker()
        {
            var scene = EditorSceneManager.OpenScene($"{ScenesFolder}/UndergroundFacility.unity");
            var root = ResetRoot();
            CreateMaterials();
            const float H = 4f;

            // Elevator room and corridor to the hub
            var elevator = Group(root.transform, "ElevatorRoom");
            BunkerRoom(elevator, "ElevatorRoom", new Vector3(0, 0, 0), new Vector3(8, H, 8), north: 0f);
            Box(elevator, "ElevatorPlatform", new Vector3(0, 0.1f, -1.5f), new Vector3(3, 0.2f, 3), interactMat);
            Portal(elevator, "ElevatorPanel_Up", new Vector3(2.3f, 1.3f, -3.5f),
                "Ride Elevator Up", "IslandExterior", "ElevatorTop");
            Spawn(elevator, "ElevatorBottom", new Vector3(0, 0, -1), 0f);
            Corridor(elevator, "Corridor_ToHub", new Vector3(0, 0, 8), new Vector3(4, H, 8), alongX: false);

            // Central hub
            var hub = Group(root.transform, "CentralHub");
            BunkerRoom(hub, "Hub", new Vector3(0, 0, 20), new Vector3(16, H, 16), south: 0f, east: 0f, west: 0f);
            Box(hub, "AmmoLocker", new Vector3(-6.5f, 1f, 26.5f), new Vector3(2, 2, 0.8f), interactMat);

            // Wing A: laser corridor + breaker room (west)
            var wingA = Group(root.transform, "WingA");
            Corridor(wingA, "LaserCorridor", new Vector3(-19, 0, 20), new Vector3(22, H, 4), alongX: true);
            BunkerRoom(wingA, "BreakerRoom", new Vector3(-36, 0, 20), new Vector3(12, H, 12), east: 0f);
            Box(wingA, "Fuse1_Pod", new Vector3(-40, 0.8f, 20), new Vector3(1.2f, 1.6f, 1.2f), interactMat);

            // Wing B: flooded generator + quarantine room (east)
            var wingB = Group(root.transform, "WingB");
            Corridor(wingB, "Corridor_ToGenerator", new Vector3(12, 0, 20), new Vector3(8, H, 4), alongX: true);
            BunkerRoom(wingB, "FloodedGenerator", new Vector3(26, -0.3f, 20), new Vector3(20, H + 0.3f, 16),
                west: 0f, east: 0f);
            var water = Box(wingB, "ElectrifiedWater", new Vector3(26, -0.1f, 20), new Vector3(19.4f, 0.02f, 15.4f), waterMat);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            BunkerRoom(wingB, "QuarantineRoom", new Vector3(44, 0, 20), new Vector3(16, H, 16), west: 0f);
            Box(wingB, "Fuse2_Pedestal", new Vector3(48, 0.6f, 20), new Vector3(1, 1.2f, 1), interactMat);

            AddSceneEssentials(root, new Vector3(0, 0, -1), dark: true);
            BakeNavMesh(root, "UndergroundFacility");
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Floor, ceiling, walls and a light. A side value >= 0 is the door's offset from that wall's center.</summary>
        static void BunkerRoom(Transform parent, string name, Vector3 floorCenter, Vector3 size,
            float north = -1, float south = -1, float east = -1, float west = -1)
        {
            var group = Group(parent, name);
            Box(group, "Floor", floorCenter + Vector3.down * 0.1f, new Vector3(size.x, 0.2f, size.z), floorMat);
            Box(group, "Ceiling", floorCenter + Vector3.up * (size.y + 0.1f), new Vector3(size.x, 0.2f, size.z), roofMat);
            Room(group, "Walls", floorCenter, size, north, south, east, west, doorWidth: 4f, doorHeight: 3.2f);
            AddLight(group, floorCenter + Vector3.up * (size.y - 0.5f), Mathf.Max(size.x, size.z));
        }

        static void Corridor(Transform parent, string name, Vector3 floorCenter, Vector3 size, bool alongX)
        {
            var group = Group(parent, name);
            Box(group, "Floor", floorCenter + Vector3.down * 0.1f, new Vector3(size.x, 0.2f, size.z), floorMat);
            Box(group, "Ceiling", floorCenter + Vector3.up * (size.y + 0.1f), new Vector3(size.x, 0.2f, size.z), roofMat);
            float hx = size.x / 2f, hz = size.z / 2f;
            if (alongX)
            {
                Wall(group, "Wall_N", floorCenter + new Vector3(-hx, 0, hz), floorCenter + new Vector3(hx, 0, hz), size.y);
                Wall(group, "Wall_S", floorCenter + new Vector3(-hx, 0, -hz), floorCenter + new Vector3(hx, 0, -hz), size.y);
            }
            else
            {
                Wall(group, "Wall_E", floorCenter + new Vector3(hx, 0, -hz), floorCenter + new Vector3(hx, 0, hz), size.y);
                Wall(group, "Wall_W", floorCenter + new Vector3(-hx, 0, -hz), floorCenter + new Vector3(-hx, 0, hz), size.y);
            }
            AddLight(group, floorCenter + Vector3.up * (size.y - 0.5f), Mathf.Max(size.x, size.z));
        }

        // ------------------------------------------------------------------ shared pieces

        static GameObject ResetRoot()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            foreach (var name in new[] { "HUD", "DebugPlayer", "Player" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }
            return new GameObject(RootName);
        }

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        /// <summary>
        /// Axis-aligned wall from a to b (both at floor level). doorCenter is the distance from a
        /// to the middle of the doorway; negative means no door.
        /// </summary>
        static void Wall(Transform parent, string name, Vector3 a, Vector3 b, float height,
            float doorCenter = -1f, float doorWidth = 1.6f, float doorHeight = DoorHeight)
        {
            Vector3 dir = (b - a).normalized;
            float length = Vector3.Distance(a, b);
            bool alongX = Mathf.Abs(dir.x) > 0.5f;

            void Segment(string segName, float from, float to, float bottom, float top)
            {
                if (to - from < 0.01f || top - bottom < 0.01f) return;
                Vector3 center = a + dir * ((from + to) / 2f) + Vector3.up * ((bottom + top) / 2f);
                Vector3 size = alongX
                    ? new Vector3(to - from, top - bottom, WallThickness)
                    : new Vector3(WallThickness, top - bottom, to - from);
                Box(parent, segName, center, size, wallMat);
            }

            if (doorCenter < 0f)
            {
                Segment(name, 0f, length, 0f, height);
                return;
            }
            float d0 = doorCenter - doorWidth / 2f, d1 = doorCenter + doorWidth / 2f;
            Segment(name + "_Left", 0f, d0, 0f, height);
            Segment(name + "_Right", d1, length, 0f, height);
            Segment(name + "_Lintel", d0, d1, doorHeight, height);
        }

        /// <summary>Four walls around a floor center. A side value >= 0 is the door's offset from that wall's center.</summary>
        static void Room(Transform parent, string name, Vector3 floorCenter, Vector3 size,
            float north = -1, float south = -1, float east = -1, float west = -1,
            float doorWidth = 1.6f, float doorHeight = DoorHeight)
        {
            var group = Group(parent, name);
            float hx = size.x / 2f, hz = size.z / 2f, h = size.y;
            Vector3 sw = floorCenter + new Vector3(-hx, 0, -hz), se = floorCenter + new Vector3(hx, 0, -hz);
            Vector3 nw = floorCenter + new Vector3(-hx, 0, hz), ne = floorCenter + new Vector3(hx, 0, hz);

            float Door(float offset, float wallLength) => offset < 0f ? -1f : wallLength / 2f + offset;
            Wall(group, "Wall_N", nw, ne, h, Door(north, size.x), doorWidth, doorHeight);
            Wall(group, "Wall_S", sw, se, h, Door(south, size.x), doorWidth, doorHeight);
            Wall(group, "Wall_E", se, ne, h, Door(east, size.z), doorWidth, doorHeight);
            Wall(group, "Wall_W", sw, nw, h, Door(west, size.z), doorWidth, doorHeight);
        }

        static void Spawn(Transform parent, string id, Vector3 position, float yaw, bool isDefault = false)
        {
            var go = new GameObject($"Spawn_{id}");
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var spawn = go.AddComponent<SpawnPoint>();
            var so = new SerializedObject(spawn);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("isDefault").boolValue = isDefault;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Portal(Transform parent, string name, Vector3 position, string prompt, string scene, string spawnId)
        {
            var go = Box(parent, name, position, new Vector3(0.5f, 0.7f, 0.2f), interactMat);
            go.isStatic = false;
            go.layer = LayerMask.NameToLayer("Interactable");
            go.tag = "Interactable";
            var portal = go.AddComponent<ScenePortal>();
            var so = new SerializedObject(portal);
            so.FindProperty("prompt").stringValue = prompt;
            so.FindProperty("targetScene").stringValue = scene;
            so.FindProperty("targetSpawnId").stringValue = spawnId;
            // Requirements stay at None until the Arc Pistol pickup and fuses exist (Phase 2).
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Marker(Transform parent, string name, Vector3 position)
        {
            var go = Box(parent, name, position + Vector3.up * 0.5f, Vector3.one, hazardMat);
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        static void InvisibleWall(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject($"Boundary_{name}");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
            go.isStatic = true;
        }

        static void AddLight(Transform parent, Vector3 position, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range * 0.9f;
            light.intensity = 3f;
            light.color = new Color(0.85f, 0.9f, 1f);
        }

        /// <summary>HUD, debug player, and lighting. Removes the default camera because the debug player has one.</summary>
        static void AddSceneEssentials(GameObject root, Vector3 playerPosition, bool dark)
        {
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUDPrefabBuilder.PrefabPath);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            hud.name = "HUD";

            var mainCam = GameObject.Find("Main Camera");
            if (mainCam != null) Object.DestroyImmediate(mainCam);
            // Use Partner A's Player prefab once it exists; otherwise the temporary DebugPlayer.
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabBuilder.PlayerPath);
            if (playerPrefab != null)
                ((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab)).transform.position = playerPosition;
            else
                CreateDebugPlayer(playerPosition);

            Light sun = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Directional) sun = l;
            if (sun != null)
            {
                sun.gameObject.SetActive(!dark);
                sun.transform.rotation = Quaternion.Euler(12f, 210f, 0f); // low dusk sun
                sun.color = new Color(1f, 0.72f, 0.5f);
            }
            if (dark)
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.11f);
            }
        }

        static void CreateDebugPlayer(Vector3 position)
        {
            var player = new GameObject("DebugPlayer");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Player");
            player.transform.position = position;

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0, 0.9f, 0);
            controller.stepOffset = 0.4f;

            var cam = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            cam.transform.SetParent(player.transform, false);
            cam.transform.localPosition = new Vector3(0, 1.6f, 0);

            var debug = player.AddComponent<DebugPlayer>();
            var so = new SerializedObject(debug);
            so.FindProperty("cameraPivot").objectReferenceValue = cam.transform;
            so.FindProperty("interactMask").intValue = ~(1 << LayerMask.NameToLayer("Player"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BakeNavMesh(GameObject root, string sceneName)
        {
            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~(1 << LayerMask.NameToLayer("Player"));
            surface.BuildNavMesh();

            CreateOrReplace(surface.navMeshData, $"{DataFolder}/NavMesh_{sceneName}.asset");
        }

        // ------------------------------------------------------------------ materials

        static void CreateMaterials()
        {
            EnsureFolder(DataFolder);
            floorMat = Mat("Greybox_Floor", new Color(0.45f, 0.45f, 0.45f));
            wallMat = Mat("Greybox_Wall", new Color(0.7f, 0.7f, 0.7f));
            roofMat = Mat("Greybox_Roof", new Color(0.3f, 0.3f, 0.32f));
            metalMat = Mat("Greybox_Metal", new Color(0.35f, 0.4f, 0.45f));
            interactMat = Mat("Greybox_Interactable", new Color(1f, 0.8f, 0.1f));
            hazardMat = Mat("Greybox_Hazard", new Color(0.9f, 0.15f, 0.1f));
            waterMat = Mat("Greybox_Water", new Color(0.1f, 0.3f, 0.45f, 0.75f), transparent: true);
        }

        static Material Mat(string name, Color color, bool transparent = false)
        {
            var path = $"{DataFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f); // Transparent
                mat.SetFloat("_Blend", 0f);   // Alpha
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetFloat("_ZWrite", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void CreateOrReplace(Object asset, string path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
