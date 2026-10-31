using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        const string PierFolder = MaterialFolder + "/Pier";

        // ------------------------------------------------------------------ supplies (ammo refiller and medkits)

        /// <summary>An interactive ammo locker with the military locker look, standing on the ground at pos, front toward a point.</summary>
        static GameObject PlaceAmmoLocker(Transform parent, Vector3 groundPos, Vector3 toward, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "AmmoLocker";
            go.layer = LayerMask.NameToLayer("Interactable");
            go.transform.SetParent(parent, false);
            go.transform.position = groundPos + Vector3.up * 1f;
            go.transform.localScale = new Vector3(0.8f, 2f, 2f);
            go.AddComponent<AmmoLocker>();
            DressLocker(parent, go.transform, toward);
            return go;
        }

        static GameObject PlaceMedkit(Transform parent, Vector3 groundPos, string id)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Pickups/Medkit.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = groundPos + Vector3.up * 0.4f;
            var so = new SerializedObject(go.GetComponent<Pickup>());
            so.FindProperty("persistentId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.name = id;
            return go;
        }

        // ------------------------------------------------------------------ textures

        /// <summary>Weathered deck planks, 8 per tile with dark gaps, grain streaks, nails and per-plank color variation.</summary>
        static Texture2D PlankTexture()
        {
            if (!AssetDatabase.IsValidFolder(PierFolder)) AssetDatabase.CreateFolder(MaterialFolder, "Pier");
            string path = $"{PierFolder}/Planks.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int N = 512, planks = 8;
            var rng = new System.Random(5);
            var plankTint = new float[planks];
            for (int i = 0; i < planks; i++) plankTint[i] = 0.75f + 0.35f * (float)rng.NextDouble();
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float v = y / (float)N;
                    int plank = Mathf.Min(planks - 1, (int)(v * planks));
                    float local = v * planks - plank;
                    float grain = Mathf.PerlinNoise(x * 0.012f + plank * 7f, y * 0.45f) * 0.5f + Mathf.PerlinNoise(x * 0.06f + plank, y * 1.6f) * 0.25f;
                    float stain = Mathf.PerlinNoise(x * 0.01f + 20f, y * 0.01f + 3f);
                    float shade = plankTint[plank] * (0.55f + grain * 0.8f) * (0.75f + stain * 0.4f);
                    float gap = local < 0.035f || local > 0.965f ? 0.25f : 1f;
                    // End joints: each plank has one butt joint at its own offset
                    float joint = Mathf.Abs(x / (float)N - (0.15f + plank * 0.37f) % 1f);
                    if (joint < 0.004f) gap = Mathf.Min(gap, 0.3f);
                    var c = new Color(0.46f, 0.36f, 0.25f) * shade * gap;
                    c.a = 1f;
                    px[y * N + x] = c;
                }
            // Nails near the plank ends
            for (int p = 0; p < planks; p++)
                foreach (float xf in new[] { 0.06f, 0.5f, 0.94f })
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int cx = (int)(xf * N) + dx, cy = (int)((p + 0.5f) / planks * N) + dy;
                            if (dx * dx + dy * dy <= 5) px[cy * N + cx] = new Color32(40, 38, 36, 255);
                        }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true);
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(path.Replace("Assets/", Application.dataPath + "/"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material PlankMaterial()
        {
            if (!AssetDatabase.IsValidFolder(PierFolder)) AssetDatabase.CreateFolder(MaterialFolder, "Pier");
            string path = $"{PierFolder}/Pier_Planks.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find(TriplanarShader)); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", PlankTexture());
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_GreyboxColor", Color.white);
            mat.SetFloat("_TileSize", 4f);
            mat.SetFloat("_MetallicScale", 0f);
            mat.SetFloat("_SmoothnessScale", 0.25f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ the pier

        /// <summary>
        /// Pier dressing: plank deck, fascia and piles, railings on the open edges, mooring bollards near the boat, lamp
        /// posts, painted shipping containers, a harbour shed, crates, drums and rope, and the ammo locker and medkits.
        /// Existing pier shapes keep their colliders (deck, containers); the boss pylons are untouched.
        /// </summary>
        static string DressPier(Transform root)
        {
            var deck = Find("PierDeck");
            if (deck == null) return "no pier";
            var db = deck.GetComponent<Renderer>().bounds;
            deck.GetComponent<Renderer>().sharedMaterial = PlankMaterial();
            float top = db.max.y;
            float x0 = db.min.x, x1 = db.max.x, z0 = db.min.z, z1 = db.max.z;
            var g = Group(root, "Pier");
            var rng = new System.Random(77);

            var wood = Lit("Pier_Wood", new Color(0.3f, 0.2f, 0.12f), 0f, 0.2f);
            var darkWood = Lit("Pier_DarkWood", new Color(0.16f, 0.11f, 0.07f), 0f, 0.2f);
            var steel = Steel("Pier_Steel", new Color(0.45f, 0.48f, 0.5f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var rope = Lit("Pier_Rope", new Color(0.55f, 0.45f, 0.3f), 0f, 0.1f);
            var sconceMat = ModelMat("industrial_caged_sconce", "industrial_caged_sconce", 0.5f, 0.6f, emissive: true);
            var canMat = ModelMat("metal_jerrycan", "metal_jerrycan", 0.45f, 0.5f);
            var crateMat = ModelMat("wooden_crate_01", "wooden_crate_01", 0.3f, 0f);

            // Fascia boards around the deck edge and piles under it
            Part(g, "FasciaW", PrimitiveType.Cube, new Vector3(x0 + 0.03f, top - 0.32f, (z0 + z1) / 2f), new Vector3(0.1f, 0.5f, z1 - z0), darkWood);
            Part(g, "FasciaE", PrimitiveType.Cube, new Vector3(x1 - 0.03f, top - 0.32f, (z0 + z1) / 2f), new Vector3(0.1f, 0.5f, z1 - z0), darkWood);
            Part(g, "FasciaS", PrimitiveType.Cube, new Vector3((x0 + x1) / 2f, top - 0.32f, z0 + 0.03f), new Vector3(x1 - x0, 0.5f, 0.1f), darkWood);
            int piles = 0;
            for (float z = z0 + 0.4f; z <= z1 - 0.3f; z += 4f)
                foreach (float x in new[] { x0 + 0.4f, x1 - 0.4f })
                { Tube(g, "Pile", new Vector3(x, top - 3.2f, z), new Vector3(x, top - 0.1f, z), 0.2f, wood); piles++; }
            for (float x = x0 + 4f; x < x1 - 2f; x += 4f)
            { Tube(g, "Pile", new Vector3(x, top - 3.2f, z0 + 0.4f), new Vector3(x, top - 0.1f, z0 + 0.4f), 0.2f, wood); piles++; }

            // Railings on the west edge and the south end (collidable), a low rope rail on the east edge either side of the boat gap
            int railPosts = 0;
            void Rail(Vector3 a, Vector3 b, bool solid)
            {
                float len = Vector3.Distance(a, b);
                int posts = Mathf.Max(2, Mathf.RoundToInt(len / 3f) + 1);
                for (int i = 0; i < posts; i++)
                {
                    Vector3 p = Vector3.Lerp(a, b, i / (float)(posts - 1));
                    Part(g, "RailPost", PrimitiveType.Cube, p + Vector3.up * 0.55f, new Vector3(0.09f, 1.1f, 0.09f), steel);
                    railPosts++;
                }
                var top1 = Bar(g, "RailTop", a + Vector3.up * 1.05f, b + Vector3.up * 1.05f, 0.06f, steel);
                Bar(g, "RailMid", a + Vector3.up * 0.55f, b + Vector3.up * 0.55f, 0.04f, steel);
                Bar(g, "RailLow", a + Vector3.up * 0.2f, b + Vector3.up * 0.2f, 0.04f, steel);
                if (solid)
                {
                    var col = new GameObject("RailCollider").AddComponent<BoxCollider>();
                    col.transform.SetParent(g, false);
                    col.transform.position = (a + b) * 0.5f + Vector3.up * 0.7f;
                    col.transform.rotation = Quaternion.LookRotation((b - a).normalized);
                    col.size = new Vector3(0.12f, 1.4f, len);
                }
            }
            Rail(new Vector3(x0 + 0.12f, top, z0 + 0.12f), new Vector3(x0 + 0.12f, top, z1 - 4.5f), true);
            Rail(new Vector3(x0 + 0.12f, top, z0 + 0.12f), new Vector3(x1 - 0.12f, top, z0 + 0.12f), true);
            Rail(new Vector3(x1 - 0.12f, top, z0 + 0.12f), new Vector3(x1 - 0.12f, top, 15.5f), true);
            Rail(new Vector3(x1 - 0.12f, top, 24.5f), new Vector3(x1 - 0.12f, top, z1 - 6f), true);

            // Mooring bollards, rope coils and a life ring station by the boat
            for (float z = 16.5f; z <= 24f; z += 3.7f)
            {
                Tube(g, "Bollard", new Vector3(x1 - 0.5f, top, z), new Vector3(x1 - 0.5f, top + 0.42f, z), 0.12f, dark);
                Tube(g, "BollardHead", new Vector3(x1 - 0.5f, top + 0.4f, z), new Vector3(x1 - 0.5f, top + 0.5f, z), 0.17f, dark);
            }
            for (int k = 0; k < 4; k++)
                Part(g, "RopeCoil", PrimitiveType.Cylinder, new Vector3(x1 - 1.4f, top + 0.04f + k * 0.07f, 19.6f), new Vector3(0.65f - k * 0.04f, 0.035f, 0.65f - k * 0.04f), rope);
            var ring = Lit("Pier_LifeRing", new Color(0.9f, 0.4f, 0.1f), 0f, 0.4f);
            Part(g, "LifeRingPost", PrimitiveType.Cube, new Vector3(x1 - 0.5f, top + 0.9f, 28.5f), new Vector3(0.08f, 1.8f, 0.08f), steel);
            Part(g, "LifeRing", PrimitiveType.Cylinder, new Vector3(x1 - 0.5f, top + 1.45f, 28.5f - 0.12f), new Vector3(0.6f, 0.06f, 0.6f), ring, Quaternion.Euler(90, 0, 0));
            Part(g, "LifeRingHole", PrimitiveType.Cylinder, new Vector3(x1 - 0.5f, top + 1.45f, 28.5f - 0.15f), new Vector3(0.3f, 0.07f, 0.3f), dark, Quaternion.Euler(90, 0, 0));

            // Lamp posts with caged lamps and warm light
            foreach (var (x, z) in new[] { (x0 + 1.2f, 33f), (x1 - 1.2f, 33f), (x0 + 1.2f, 12f) })
            {
                Tube(g, "LampPole", new Vector3(x, top, z), new Vector3(x, top + 4.2f, z), 0.07f, steel);
                Bar(g, "LampArm", new Vector3(x, top + 4.15f, z), new Vector3(x + (x < 140f ? 0.9f : -0.9f), top + 4.15f, z), 0.06f, steel);
                Model("industrial_caged_sconce", "industrial_caged_sconce", g, new Vector3(x + (x < 140f ? 0.9f : -0.9f), top + 4.1f, z), 0f, 0.5f, Anchor.Top, sconceMat);
                PointLight(g, "LampLight", new Vector3(x + (x < 140f ? 0.9f : -0.9f), top + 3.6f, z), new Color(1f, 0.82f, 0.55f), 3f, 12f);
            }

            // Shipping containers: corrugated painted bodies over the old boxes (their colliders stay)
            string[] names = { "Container_A", "Container_B", "Container_C" };
            Color[] paints = { new Color(0.2f, 0.45f, 1.1f), new Color(1.5f, 0.45f, 0.25f), new Color(0.35f, 0.9f, 0.55f) };
            for (int i = 0; i < names.Length; i++)
            {
                var box = Find(names[i]);
                if (box != null) DressContainer(g, box.transform, paints[i], i);
            }

            // Harbour shed at the landward end: corrugated walls, pitched roof, door and window
            Vector3 shedC = new Vector3(131.8f, top, 34.6f);
            DressShed(g, shedC);

            // Crates, jerrycans and a net pile along the edges
            int props = 0;
            foreach (var (x, z, kind) in new[] { (x0 + 1.1f, 20.5f, 0), (x0 + 1.0f, 22.2f, 1), (x1 - 1.3f, 8.8f, 0), (x1 - 2.2f, 9.2f, 2), (x0 + 3.3f, 8.6f, 1), (x0 + 1.2f, 28.5f, 2) })
            {
                GameObject item = null;
                if (kind == 0) item = Model("wooden_crate_01", "wooden_crate_01", g, new Vector3(x, top, z), (float)rng.NextDouble() * 60f, 1f, Anchor.Bottom, crateMat);
                else if (kind == 1) item = Model("wooden_military_crate", "wooden_military_crate", g, new Vector3(x, top, z), (float)rng.NextDouble() * 60f, 1.2f, Anchor.Bottom, null);
                else item = Model("metal_jerrycan", "metal_jerrycan", g, new Vector3(x, top, z), (float)rng.NextDouble() * 360f, 1f, Anchor.Bottom, canMat);
                if (item != null) { AddBoundsCollider(item); props++; }
            }
            var net = Lit("Pier_Net", new Color(0.22f, 0.3f, 0.25f), 0f, 0.1f);
            Part(g, "NetPile", PrimitiveType.Sphere, new Vector3(x1 - 1.5f, top + 0.12f, 31f), new Vector3(1.3f, 0.35f, 0.9f), net);

            // Ammo refiller and medkits near the landward end (cover from the Titan behind the shed and the containers)
            var supplies = Group(g, "Supplies");
            PlaceAmmoLocker(supplies, new Vector3(x1 - 0.9f, top, 35.2f), new Vector3(x0, top, 30f), "Pier");
            PlaceMedkit(supplies, new Vector3(x1 - 1.2f, top, 31.8f), "Pier_Medkit1");
            PlaceMedkit(supplies, new Vector3(x0 + 1.0f, top, 31.2f), "Pier_Medkit2");

            return $"deck planks, {piles} piles, {railPosts} rail posts, 3 containers, shed, {props} props, ammo locker + 2 medkits";
        }

        /// <summary>Corrugated shipping container look over a greybox box: painted body, ribs, door end with lock rods.</summary>
        static void DressContainer(Transform parent, Transform box, Color paint, int seed)
        {
            var r = box.GetComponent<Renderer>();
            var b = r.bounds;
            r.enabled = false;
            var g = Group(parent, box.name + "Model");
            var body = Triplanar($"Container_{seed}", "metal_plate", paint, 1.5f, 0.3f, 0.5f);
            var rib = Triplanar($"ContainerRib_{seed}", "metal_plate", paint * 0.85f, 1.5f, 0.3f, 0.5f);
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var steel = Steel("Pier_Steel", new Color(0.45f, 0.48f, 0.5f));
            bool alongZ = b.size.z >= b.size.x;
            float len = alongZ ? b.size.z : b.size.x, wid = alongZ ? b.size.x : b.size.z, h = b.size.y;
            Quaternion rot = alongZ ? Quaternion.identity : Quaternion.Euler(0, 90, 0);         // local z = long axis
            Vector3 c = b.center;
            Vector3 L(float side, float up, float along) => c + rot * new Vector3(side, up, along);
            GameObject Cube(string n, Vector3 pos, Vector3 size, Material m)
            {
                var go = Part(g, n, PrimitiveType.Cube, pos, size, m);
                go.transform.rotation = rot;
                Static(go);
                return go;
            }
            Cube("Body", c, new Vector3(wid - 0.06f, h - 0.1f, len - 0.04f), body);
            for (float a = -len / 2f + 0.3f; a < len / 2f - 0.25f; a += 0.3f)
                foreach (float s in new[] { -1f, 1f })
                    Cube("Rib", L(s * (wid / 2f - 0.005f), 0f, a), new Vector3(0.07f, h - 0.3f, 0.14f), rib);
            // Roof ribs and corner castings
            for (float a = -len / 2f + 0.3f; a < len / 2f - 0.25f; a += 0.6f)
                Cube("RoofRib", L(0, h / 2f - 0.03f, a), new Vector3(wid - 0.2f, 0.06f, 0.14f), rib);
            foreach (float s in new[] { -1f, 1f })
                foreach (float e in new[] { -1f, 1f })
                    foreach (float v in new[] { -1f, 1f })
                        Cube("Casting", L(s * (wid / 2f - 0.08f), v * (h / 2f - 0.08f), e * (len / 2f - 0.08f)), new Vector3(0.18f, 0.18f, 0.18f), dark);
            // Door end (the +local z end): two leaves with lock rods and handles
            foreach (float s in new[] { -1f, 1f })
            {
                Cube("DoorLeaf", L(s * wid / 4f, 0f, len / 2f + 0.01f), new Vector3(wid / 2f - 0.1f, h - 0.3f, 0.03f), rib);
                for (int k = 0; k < 2; k++)
                    Cube("LockRod", L(s * (wid / 4f + (s > 0 ? -0.15f : 0.15f) * (k == 0 ? 1 : -1)) , 0f, len / 2f + 0.045f), new Vector3(0.04f, h - 0.35f, 0.03f), steel);
                Cube("Handle", L(s * 0.12f, 0f, len / 2f + 0.07f), new Vector3(0.04f, 0.3f, 0.05f), dark);
            }
            Label(g, "ContainerId", $"OVR {4120 + seed * 37:0000} 7", L(wid / 2f + 0.02f, h / 2f - 0.35f, -len / 2f + 1.3f), rot * Vector3.right, 0.9f, new Color(0.92f, 0.92f, 0.88f), 1.5f);
        }

        /// <summary>A small harbour shed: concrete footing, corrugated walls, pitched roof, a door and a lit window. Solid box collider.</summary>
        static void DressShed(Transform parent, Vector3 floor)
        {
            var g = Group(parent, "HarbourShed");
            var wall = Triplanar("Shed_Walls", "metal_plate", new Color(1.1f, 1.3f, 1.2f), 1.5f, 0.2f, 0.45f);
            var roof = Triplanar("Shed_Roof", "metal_plate", new Color(0.8f, 0.4f, 0.3f), 1.5f, 0.3f, 0.45f);
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var glow = Lit("Shed_Window", new Color(0.8f, 0.6f, 0.3f), 0f, 0.8f, new Color(2.2f, 1.5f, 0.6f));
            var wood = Lit("Pier_DarkWood", new Color(0.16f, 0.11f, 0.07f), 0f, 0.2f);
            const float w = 3.2f, d = 2.6f, h = 2.7f;
            var body = Part(g, "Walls", PrimitiveType.Cube, floor + new Vector3(0, h / 2f, 0), new Vector3(w, h, d), wall, collider: true);
            for (float x = -w / 2f + 0.2f; x < w / 2f; x += 0.3f)
                foreach (float s in new[] { -1f, 1f })
                    Part(g, "Rib", PrimitiveType.Cube, floor + new Vector3(x, h / 2f, s * (d / 2f + 0.02f)), new Vector3(0.06f, h - 0.2f, 0.05f), wall);
            // Pitched roof: two slabs meeting at the ridge (ridge along x)
            foreach (float s in new[] { -1f, 1f })
            {
                var slab = Part(g, "Roof", PrimitiveType.Cube, floor + new Vector3(0, h + 0.35f, s * 0.75f), new Vector3(w + 0.4f, 0.08f, d / 2f + 0.5f), roof, Quaternion.Euler(s * 17f, 0, 0));
                slab.transform.position = floor + new Vector3(0, h + 0.38f, s * 0.78f);
            }
            // Door and window on the east face (toward the pier)
            Part(g, "Door", PrimitiveType.Cube, floor + new Vector3(w / 2f + 0.03f, 1.0f, 0.5f), new Vector3(0.06f, 2.0f, 1.0f), wood);
            Part(g, "DoorHandle", PrimitiveType.Cube, floor + new Vector3(w / 2f + 0.08f, 1.0f, 0.15f), new Vector3(0.05f, 0.05f, 0.18f), dark);
            Part(g, "WindowFrame", PrimitiveType.Cube, floor + new Vector3(w / 2f + 0.025f, 1.6f, -0.7f), new Vector3(0.05f, 0.7f, 0.9f), dark);
            Part(g, "Window", PrimitiveType.Cube, floor + new Vector3(w / 2f + 0.05f, 1.6f, -0.7f), new Vector3(0.04f, 0.58f, 0.78f), glow, shadows: false);
            PointLight(g, "WindowLight", floor + new Vector3(w / 2f + 1.2f, 1.7f, -0.7f), new Color(1f, 0.75f, 0.4f), 2f, 6f);
            Label(g, "ShedSign", "HARBOUR OFFICE", floor + new Vector3(w / 2f + 0.06f, 2.35f, 0f), Vector3.right, 0.9f, new Color(0.95f, 0.85f, 0.4f), 2f);
        }

        /// <summary>Ammo locker and medkit outside the generator bunker's doorway (west side).</summary>
        static string DressGeneratorSupplies(Transform root)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return "no terrain";
            float Ground(float x, float z) => terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;
            var g = Group(root, "GeneratorSupplies");
            PlaceAmmoLocker(g, new Vector3(169.4f, Ground(169.4f, 79.3f), 79.3f), new Vector3(150f, 0, 79f), "Generator");
            PlaceMedkit(g, new Vector3(169.3f, Ground(169.3f, 70.9f), 70.9f), "Gen_Medkit_Outside");
            return "ammo locker + medkit outside";
        }
    }
}
