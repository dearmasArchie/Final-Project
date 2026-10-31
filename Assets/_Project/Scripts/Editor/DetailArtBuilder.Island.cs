using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        /// <summary>Island scene: main power cabinet and knife switch, elevator cage, radio tower, tower compound equipment.</summary>
        static string DressIsland()
        {
            var root = FreshRoot();
            var report = new List<string>();
            report.Add("elevator " + BuildElevator(root, "ElevatorPanel_Down", Vector3.back, Vector3.back, 4f));
            report.Add("switch " + DressPowerSwitch(root));
            report.Add("code bunker " + DressCodeBunker(root));
            report.Add("tower " + BuildRadioTower(root));
            report.Add("compound " + DressCompound(root));
            report.Add("generator " + DressGeneratorSet(root));
            report.Add(DressGeneratorBunkerInterior(root));
            report.Add("buildings " + DressBuildings(root));
            report.Add("fence " + BuildFence(root));
            report.Add("access " + DressIslandAccess(root));
            report.Add("reyes " + ReyesBuilder.Dress(root));
            var reyesVisual = GameObject.Find("ReyesVisual");
            if (reyesVisual != null)
            {
                var camp = GameObject.Find("ReyesCamp");
                Model("medical_box", "medical_box", camp != null ? camp.transform : root, reyesVisual.transform.position + reyesVisual.transform.rotation * new Vector3(0.85f, 0f, 0.2f),
                    reyesVisual.transform.eulerAngles.y + 20f, 1.3f, Anchor.Bottom, ModelMat("medical_box", "medical_box", 0.3f, 0.2f));
            }
            report.Add("pier " + DressPier(root));
            report.Add("generator supplies " + DressGeneratorSupplies(root));
            report.Add("weather " + WeatherBuilder.Dress(root));
            return string.Join("; ", report);
        }

        // ------------------------------------------------------------------ main power switch

        static string DressPowerSwitch(Transform root)
        {
            var panel = Find("MainPowerSwitch");
            var center = Find("EmergencyLight");
            if (panel == null || center == null) return "missing";

            Vector3 p = panel.transform.position;
            Vector3 toRoom = center.transform.position - p;
            Vector3 f = Mathf.Abs(toRoom.x) > Mathf.Abs(toRoom.z) ? new Vector3(Mathf.Sign(toRoom.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(toRoom.z));
            float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

            panel.GetComponent<Renderer>().enabled = false;
            var group = Group(root, "PowerCabinet");
            var cabinet = Model("utility_box_01", "utility_box_01", group, new Vector3(p.x, 0, p.z), yaw, 1.5f, Anchor.Bottom,
                ModelMat("utility_box_01", "utility_box_01", 0.45f, 0.5f));
            var b = WorldBounds(cabinet);
            // Back against the wall (where the plate was), standing on the floor under the old plate
            cabinet.transform.position += f * (Vector3.Dot(b.extents, new Vector3(Mathf.Abs(f.x), 0, Mathf.Abs(f.z))) - 0.075f);
            float floorY = p.y - 1.3f;
            cabinet.transform.position += Vector3.up * (floorY - WorldBounds(cabinet).min.y);
            b = WorldBounds(cabinet);

            // Knife-switch lever: base plate, copper contacts, long arm with a black grip. The lever swings in the wall plane.
            var lever = panel.transform.Find("LeverPivot");
            var handle = lever != null ? lever.Find("Handle") : null;
            if (handle != null) handle.GetComponent<Renderer>().enabled = false;
            var mountPos = new Vector3(b.center.x, floorY + 0.8f, b.center.z) + f * (Vector3.Dot(b.extents, new Vector3(Mathf.Abs(f.x), 0, Mathf.Abs(f.z))) + 0.04f);
            var mount = Group(group, "LeverMount");
            mount.SetPositionAndRotation(mountPos, Quaternion.LookRotation(f, Vector3.up));
            var copper = Lit("Switch_Copper", new Color(0.8f, 0.45f, 0.25f), 1f, 0.6f);
            var black = Lit("Switch_Grip", new Color(0.04f, 0.04f, 0.04f), 0f, 0.5f);
            var plate = Lit("Switch_Plate", new Color(0.75f, 0.74f, 0.7f), 0.1f, 0.5f);
            Part(mount, "BasePlate", PrimitiveType.Cube, new Vector3(0, 0.15f, -0.015f), new Vector3(0.34f, 0.7f, 0.03f), plate);
            Part(mount, "ContactLow", PrimitiveType.Cube, new Vector3(0, -0.12f, 0.02f), new Vector3(0.08f, 0.08f, 0.05f), copper);
            Part(mount, "ContactHigh", PrimitiveType.Cube, new Vector3(0.0f, 0.45f, 0.02f), new Vector3(0.08f, 0.08f, 0.05f), copper);
            Part(mount, "Hinge", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.07f, 0.02f, 0.07f), copper, Quaternion.Euler(90, 0, 0));
            if (lever != null)
            {
                lever.SetParent(mount, false);
                lever.localPosition = new Vector3(0, 0, 0.03f);
                lever.localRotation = Quaternion.identity;
                lever.localScale = Vector3.one;
                foreach (Transform old in lever) if (old.name != "Handle") Object.DestroyImmediate(old.gameObject);
                Part(lever, "Arm", PrimitiveType.Cube, new Vector3(0, 0.26f, 0), new Vector3(0.05f, 0.52f, 0.02f), copper);
                Part(lever, "Grip", PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0.0f), new Vector3(0.07f, 0.12f, 0.07f), black);
                Part(lever, "GripCap", PrimitiveType.Sphere, new Vector3(0, 0.63f, 0), new Vector3(0.08f, 0.08f, 0.08f), black);
            }
            return $"cabinet at ({b.center.x:F1},{b.center.y:F1},{b.center.z:F1}) facing ({f.x:F0},{f.z:F0})";
        }

        // ------------------------------------------------------------------ radio tower

        /// <summary>Paraboloid dish, double sided, as a saved mesh asset (axis +Z is where it points).</summary>
        static Mesh DishMesh(string name, float radius, float depth)
        {
            const int rings = 8, segments = 28;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            for (int r = 0; r <= rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    float rr = radius * r / rings, a = s / (float)segments * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0f));
                    uv.Add(new Vector2(r / (float)rings, s / (float)segments));
                }
            // Dish surface: z = depth at the rim, 0 at the center, so it opens toward +Z
            for (int i = 0; i < v.Count; i++) v[i] = new Vector3(v[i].x, v[i].y, depth * (v[i].x * v[i].x + v[i].y * v[i].y) / (radius * radius));
            int count = v.Count;
            for (int i = 0; i < count; i++) { v.Add(v[i]); uv.Add(uv[i]); }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a0 = r * segments + s, a1 = r * segments + (s + 1) % segments;
                    int b0 = (r + 1) * segments + s, b1 = (r + 1) * segments + (s + 1) % segments;
                    t.AddRange(new[] { a0, b0, b1, a0, b1, a1 });                           // inner face
                    t.AddRange(new[] { a0 + count, b1 + count, b0 + count, a0 + count, a1 + count, b1 + count }); // back face
                }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string path = $"{MaterialFolder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>
        /// Red and white steel lattice tower (4 tapering legs, ring braces, X bracing), service platforms with railings,
        /// a parabolic dish, panel antennas, a top mast with pulsing red beacons, guy wires and a concrete footing. The
        /// greybox tower box keeps its collider (a solid footprint) and loses its renderer.
        /// </summary>
        static string BuildRadioTower(Transform root)
        {
            var box = Find("RadioTower");
            if (box == null) return "missing";
            var r = box.GetComponent<Renderer>();
            var b = r.bounds;
            r.enabled = false;
            float H = b.size.y, baseY = b.min.y;
            Vector2 c = new Vector2(b.center.x, b.center.z);
            var group = Group(root, "RadioTowerModel");

            var red = Triplanar("Tower_Red", "metal_plate", new Color(2.8f, 0.28f, 0.2f), 1.5f, 0.15f, 0.45f);
            var white = Triplanar("Tower_White", "metal_plate", new Color(2.5f, 2.5f, 2.4f), 1.5f, 0.15f, 0.45f);
            var steel = Steel("Tower_Steel", new Color(0.5f, 0.52f, 0.55f));
            var concrete = Triplanar("Tower_Concrete", "concrete_wall_008", new Color(1.3f, 1.3f, 1.25f), 3f, 0f, 0.4f);

            const float wBase = 1.6f, wTop = 0.55f;
            int sections = Mathf.RoundToInt(H / 2.5f);
            float dy = H / sections;
            float Half(float y) => Mathf.Lerp(wBase, wTop, y / H);
            Vector3 Corner(int i, float y) => new Vector3(c.x + ((i & 1) == 0 ? -1 : 1) * Half(y), baseY + y, c.y + ((i & 2) == 0 ? -1 : 1) * Half(y));

            for (int s = 0; s < sections; s++)
            {
                float y0 = s * dy, y1 = (s + 1) * dy;
                var paint = (s / 2) % 2 == 0 ? red : white;
                for (int i = 0; i < 4; i++) Bar(group, "Leg", Corner(i, y0), Corner(i, y1), 0.14f, paint);
                // Ring braces at the top of each section
                int[,] edges = { { 0, 1 }, { 1, 3 }, { 3, 2 }, { 2, 0 } };
                for (int e = 0; e < 4; e++)
                {
                    Bar(group, "Ring", Corner(edges[e, 0], y1), Corner(edges[e, 1], y1), 0.06f, paint);
                    // X bracing: one diagonal per face, alternating direction each section
                    bool flip = (s + e) % 2 == 0;
                    Bar(group, "Brace", Corner(flip ? edges[e, 0] : edges[e, 1], y0), Corner(flip ? edges[e, 1] : edges[e, 0], y1), 0.05f, steel);
                    Bar(group, "Brace", Corner(flip ? edges[e, 1] : edges[e, 0], y0), Corner(flip ? edges[e, 0] : edges[e, 1], y1), 0.04f, steel);
                }
            }

            // Footing
            Part(group, "Footing", PrimitiveType.Cube, new Vector3(c.x, baseY + 0.15f, c.y), new Vector3(4.2f, 0.5f, 4.2f), concrete);

            // Service platforms with railings
            foreach (float py in new[] { 12.5f, 20f })
            {
                float h = Half(py) + 0.5f;
                Part(group, "Platform", PrimitiveType.Cube, new Vector3(c.x, baseY + py, c.y), new Vector3(2 * h, 0.08f, 2 * h), steel);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 a = new Vector3(c.x + ((i & 1) == 0 ? -h : h), baseY + py, c.y + ((i & 2) == 0 ? -h : h));
                    Part(group, "RailPost", PrimitiveType.Cube, a + Vector3.up * 0.5f, new Vector3(0.05f, 1f, 0.05f), steel);
                }
                int[,] sides = { { 0, 1 }, { 1, 3 }, { 3, 2 }, { 2, 0 } };
                for (int e = 0; e < 4; e++)
                {
                    Vector3 a = new Vector3(c.x + ((sides[e, 0] & 1) == 0 ? -h : h), baseY + py + 1f, c.y + ((sides[e, 0] & 2) == 0 ? -h : h));
                    Vector3 d = new Vector3(c.x + ((sides[e, 1] & 1) == 0 ? -h : h), baseY + py + 1f, c.y + ((sides[e, 1] & 2) == 0 ? -h : h));
                    Bar(group, "Rail", a, d, 0.04f, steel);
                    Bar(group, "Rail", a + Vector3.down * 0.5f, d + Vector3.down * 0.5f, 0.03f, steel);
                }
            }

            // Parabolic dish on a short arm, facing the sea (south-east, toward the pier)
            Vector3 dishDir = new Vector3(0.7f, -0.12f, -0.7f).normalized;
            float dishY = baseY + 16.5f;
            Vector3 mount = new Vector3(c.x + dishDir.x * (Half(16.5f) + 0.2f), dishY, c.y + dishDir.z * (Half(16.5f) + 0.2f));
            var dishMat = Lit("Tower_Dish", new Color(0.82f, 0.84f, 0.85f), 0.5f, 0.5f);
            var dish = new GameObject("Dish");
            dish.transform.SetParent(group, false);
            dish.transform.SetPositionAndRotation(mount + dishDir * 0.9f, Quaternion.LookRotation(dishDir, Vector3.up));
            dish.AddComponent<MeshFilter>().sharedMesh = DishMesh("Tower_Dish", 1.3f, 0.45f);
            dish.AddComponent<MeshRenderer>().sharedMaterial = dishMat;
            Bar(group, "DishArm", new Vector3(c.x, dishY, c.y) + dishDir * 0.4f, mount + dishDir * 0.9f, 0.1f, steel);
            Bar(group, "DishFeed", mount + dishDir * 0.9f, mount + dishDir * 1.9f, 0.04f, steel);
            Part(group, "DishHorn", PrimitiveType.Cylinder, mount + dishDir * 1.9f, new Vector3(0.14f, 0.07f, 0.14f), dishMat, Quaternion.LookRotation(dishDir, Vector3.up) * Quaternion.Euler(90, 0, 0));

            // Panel antennas near the top
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad + 0.5f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 pos = new Vector3(c.x, baseY + H - 3.2f, c.y) + dir * (Half(H - 3.2f) + 0.35f);
                Part(group, "PanelAntenna", PrimitiveType.Cube, pos, new Vector3(0.28f, 1.8f, 0.08f), white, Quaternion.LookRotation(dir, Vector3.up));
                Bar(group, "PanelArm", new Vector3(c.x, pos.y, c.y) + dir * 0.2f, pos - dir * 0.04f, 0.04f, steel);
            }

            // Top mast and warning beacons
            Vector3 top = new Vector3(c.x, baseY + H, c.y);
            Part(group, "Mast", PrimitiveType.Cylinder, top + Vector3.up * 2.5f, new Vector3(0.12f, 2.5f, 0.12f), steel);
            var beaconMat = Lit("Tower_Beacon", new Color(0.5f, 0.02f, 0.02f), 0f, 0.8f, new Color(6f, 0.15f, 0.1f));
            foreach (var (pos, size) in new[] { (top + Vector3.up * 5.1f, 0.3f), (new Vector3(c.x, baseY + 12.5f + 1.2f, c.y + Half(12.5f) + 0.5f), 0.2f) })
            {
                Part(group, "Beacon", PrimitiveType.Sphere, pos, Vector3.one * size, beaconMat, shadows: false);
                var light = PointLight(group, "BeaconLight", pos, new Color(1f, 0.1f, 0.05f), 3f, 14f);
                var flicker = light.gameObject.AddComponent<LightFlicker>();
                var so = new SerializedObject(flicker);
                so.FindProperty("mode").enumValueIndex = (int)LightFlicker.Mode.Pulse;
                so.FindProperty("speed").floatValue = 0.6f;
                so.FindProperty("minimum").floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Guy wires to concrete anchors on the ground
            var terrain = Terrain.activeTerrain;
            var wire = Lit("Tower_Wire", new Color(0.12f, 0.12f, 0.13f), 0.8f, 0.4f);
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 anchor = new Vector3(c.x, 0, c.y) + dir * 9f;
                anchor.y = terrain != null ? terrain.SampleHeight(anchor) + terrain.transform.position.y : baseY;
                Bar(group, "GuyWire", new Vector3(c.x, baseY + 20f, c.y) + dir * Half(20f) * 1.4f, anchor + Vector3.up * 0.3f, 0.03f, wire);
                Part(group, "Anchor", PrimitiveType.Cube, anchor + Vector3.up * 0.15f, new Vector3(0.7f, 0.4f, 0.7f), concrete);
            }
            return $"base y {baseY:F1}, height {H:F0}, {sections} sections";
        }

        // ------------------------------------------------------------------ tower compound

        /// <summary>Fuse console as an industrial terminal, a generator and field radio feeding it.</summary>
        static string DressCompound(Transform root)
        {
            var console = Find("FuseConsole");
            if (console == null) return "missing console";
            // Phase3 replaces the greybox console; hide whichever renderers share the name
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (go.name == "FuseConsole") { var rr = go.GetComponent<Renderer>(); if (rr != null) rr.enabled = false; }
            var live = GameObject.Find("TowerCompound") != null ? GameObject.Find("TowerCompound").transform.Find("FuseConsole") : null;
            if (live != null) console = live.gameObject;

            Vector3 p = console.transform.position;
            var group = Group(root, "Terminal");
            var steel = Steel("Console_Steel", new Color(0.42f, 0.46f, 0.5f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var screen = Lit("Console_Screen", new Color(0.02f, 0.06f, 0.07f), 0f, 0.9f, Cyan * 1.4f);
            var led = Lit("Console_Led", new Color(0.1f, 0.5f, 0.1f), 0f, 0.5f, new Color(0.4f, 3f, 0.5f));

            // A real table under the fuse slots (the old box keeps its collider), with the screen riser standing on its back edge
            BuildFuseTable(group, p);
            Part(group, "Riser", PrimitiveType.Cube, p + new Vector3(0, 0.92f, 0.28f), new Vector3(1.36f, 0.62f, 0.1f), dark);
            for (int i = 0; i < 3; i++)
            {
                Part(group, "Screen", PrimitiveType.Cube, p + new Vector3(-0.42f + i * 0.42f, 0.92f, 0.225f), new Vector3(0.36f, 0.42f, 0.01f), screen);
                Part(group, "Led", PrimitiveType.Sphere, p + new Vector3(-0.42f + i * 0.42f, 1.18f, 0.225f), Vector3.one * 0.03f, led, shadows: false);
            }
            // Bezels around the three fuse slots (slots sit at x -0.4, 0, 0.4 on the top)
            for (int i = 0; i < 3; i++)
                Part(group, "SlotBezel", PrimitiveType.Cylinder, p + new Vector3(-0.4f + i * 0.4f, 0.605f, 0), new Vector3(0.3f, 0.02f, 0.3f), yellow);
            Label(group, "ConsoleText", "SIGNAL UPLINK", p + new Vector3(0, 1.32f, 0.225f), Vector3.back, 1.4f, Cyan, 1.3f);

            // Power for it: a generator, a cable, a field radio on a bench
            var terrain = Terrain.activeTerrain;
            float Ground(Vector3 v) => terrain != null ? terrain.SampleHeight(v) + terrain.transform.position.y : v.y;
            Vector3 genPos = p + new Vector3(-3.4f, 0, 1.2f);
            genPos.y = Ground(genPos);
            var gen = Model("portable_generator", "portable_generator", group, genPos, 90f, 1.7f, Anchor.Bottom,
                ModelMat("portable_generator", "portable_generator", 0.4f, 0.3f));
            var cable = Lit("Compound_Cable", new Color(0.02f, 0.02f, 0.02f), 0f, 0.3f);
            Vector3 g0 = genPos + Vector3.up * 0.4f, g1 = new Vector3(p.x - 0.7f, Ground(p) + 0.05f, p.z - 0.2f);
            Bar(group, "CableA", g0, new Vector3(g0.x + 1.5f, Ground(p) + 0.03f, g0.z - 0.6f), 0.04f, cable);
            Bar(group, "CableB", new Vector3(g0.x + 1.5f, Ground(p) + 0.03f, g0.z - 0.6f), g1, 0.04f, cable);

            Vector3 benchPos = p + new Vector3(-3.4f, 0, -1.4f);
            benchPos.y = Ground(benchPos);
            Part(group, "BenchTop", PrimitiveType.Cube, benchPos + Vector3.up * 0.75f, new Vector3(1.2f, 0.06f, 0.7f), steel);
            foreach (float x in new[] { -0.55f, 0.55f })
                foreach (float z in new[] { -0.3f, 0.3f })
                    Part(group, "BenchLeg", PrimitiveType.Cube, benchPos + new Vector3(x, 0.37f, z), new Vector3(0.05f, 0.74f, 0.05f), dark);
            Model("vintage_radio_transceiver", "vintage_radio_transceiver", group, benchPos + Vector3.up * 0.78f, 20f, 1.1f, Anchor.Bottom,
                ModelMat("vintage_radio_transceiver", "vintage_radio_transceiver", 0.4f, 0.4f));
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
                if (t.name == "AmmoLocker" && t.root.name != RootName) DressLocker(group, t, new Vector3(p.x, 0, p.z));
            string extras = DressCompoundExtras(root, p);
            string fuseTable = DressFuseCrate(root);
            int hidden = HideSpawnMarkers();
            return $"fuse table, generator, field radio, lockers, extras ({extras}), {fuseTable}, {hidden} spawn markers hidden";
        }
    }
}
