using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Island access and the radio tower compound: real keypad locks on every island door (the safe-room armory door, the
    /// bunkers, the service gate on a steel post), the fuse console as a proper table, a tarp canopy, sandbag cover, supply
    /// stacks, floodlights and signs. The greybox spawn markers (red WaveGate cubes) are hidden.
    /// </summary>
    public static partial class DetailArtBuilder
    {
        static float GroundY(float x, float z)
        {
            var t = Terrain.activeTerrain;
            return t != null ? t.SampleHeight(new Vector3(x, 0, z)) + t.transform.position.y : 0f;
        }

        /// <summary>Solid, visual prop: box collider fitted to the renderers, and it carves the NavMesh so enemies walk around it.</summary>
        static void SolidProp(GameObject go)
        {
            AddBoundsCollider(go);
            var b = WorldBounds(go);
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = go.transform.InverseTransformPoint(b.center);
            var local = go.transform.InverseTransformVector(b.size);
            obstacle.size = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
        }

        // ------------------------------------------------------------------ island keypads

        static string DressIslandAccess(Transform root)
        {
            var group = Group(root, "Access");
            var steel = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
            int pads = 0, doors = 0;
            foreach (var door in Object.FindObjectsByType<Door>(FindObjectsInactive.Exclude))
            {
                if (door.transform.root.name == RootName) continue;
                var cubes = new List<Transform>();
                foreach (Transform child in door.transform) if (child.name == "Keypad") cubes.Add(child);
                if (cubes.Count == 0) continue;
                doors++;
                Vector3 mid = Vector3.zero;
                foreach (var pad in cubes)
                {
                    Vector3 s = pad.lossyScale;
                    Vector3 axis = s.x < s.z ? Vector3.right : Vector3.forward;
                    float side = Mathf.Sign(Vector3.Dot(pad.position - door.transform.position, axis));
                    HideRenderer(pad.gameObject);
                    BuildKeypadVisual(group, $"{door.name}_Keypad", pad.position, axis * side, Mathf.Min(s.x, s.z), door);
                    mid += pad.position;
                    pads++;
                }
                if (door.name != "ServiceGate") continue;

                // The gate sits in a chain-link fence, so its keypads get a real post: base plate, steel column, cap, a hood on each face
                mid /= cubes.Count;
                float ground = GroundY(mid.x, mid.z);
                Part(group, "GatePostBase", PrimitiveType.Cube, new Vector3(mid.x, ground + 0.03f, mid.z), new Vector3(0.6f, 0.06f, 0.6f), steel);
                Part(group, "GatePost", PrimitiveType.Cube, new Vector3(mid.x, ground + 0.9f, mid.z), new Vector3(0.26f, 1.74f, 0.3f), steel, collider: true);
                Part(group, "GatePostCap", PrimitiveType.Cube, new Vector3(mid.x, ground + 1.79f, mid.z), new Vector3(0.32f, 0.05f, 0.36f), Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f));
                foreach (float sx in new[] { -1f, 1f })
                    Part(group, "KeypadHood", PrimitiveType.Cube, new Vector3(mid.x + sx * 0.2f, mid.y + 0.25f, mid.z), new Vector3(0.12f, 0.025f, 0.3f), steel, Quaternion.Euler(0, 0, sx * -12f));
                Label(group, "GateSign", "SERVICE GATE", new Vector3(mid.x - 0.141f, mid.y - 0.3f, mid.z), Vector3.left, 0.5f, new Color(0.9f, 0.7f, 0.1f), 0.5f);
                Label(group, "GateSignOut", "SERVICE GATE", new Vector3(mid.x + 0.141f, mid.y - 0.3f, mid.z), Vector3.right, 0.5f, new Color(0.9f, 0.7f, 0.1f), 0.5f);
            }
            return $"{pads} keypad locks on {doors} doors";
        }

        // ------------------------------------------------------------------ fuse table

        /// <summary>A real steel-framed wooden table under the fuse slots (slots sit on its top, p.y + 0.6).</summary>
        static void BuildFuseTable(Transform group, Vector3 p)
        {
            var wood = Lit("Table_Wood", new Color(0.38f, 0.26f, 0.15f), 0f, 0.3f);
            var darkWood = Lit("Table_WoodDark", new Color(0.2f, 0.14f, 0.09f), 0f, 0.25f);
            var steel = Steel("Console_Steel", new Color(0.42f, 0.46f, 0.5f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var cable = Lit("Compound_Cable", new Color(0.02f, 0.02f, 0.02f), 0f, 0.3f);
            var red = Lit("Toolbox_Red", new Color(0.7f, 0.08f, 0.06f), 0.5f, 0.5f);
            var paper = Lit("Paper_White", new Color(0.88f, 0.86f, 0.78f), 0f, 0.15f);
            var table = Group(group, "FuseTable");
            float floor = p.y - 0.56f;

            // Top: planks with a steel rim
            Part(table, "Top", PrimitiveType.Cube, p + new Vector3(0, 0.575f, 0), new Vector3(1.5f, 0.05f, 0.8f), wood);
            for (int i = 1; i < 5; i++)
                Part(table, "PlankLine", PrimitiveType.Cube, p + new Vector3(-0.75f + i * 0.3f, 0.6f, 0), new Vector3(0.008f, 0.002f, 0.78f), darkWood, shadows: false);
            Part(table, "RimFront", PrimitiveType.Cube, p + new Vector3(0, 0.575f, -0.405f), new Vector3(1.52f, 0.06f, 0.025f), steel);
            Part(table, "RimBack", PrimitiveType.Cube, p + new Vector3(0, 0.575f, 0.405f), new Vector3(1.52f, 0.06f, 0.025f), steel);
            foreach (float sx in new[] { -1f, 1f })
                Part(table, "RimSide", PrimitiveType.Cube, p + new Vector3(sx * 0.755f, 0.575f, 0), new Vector3(0.025f, 0.06f, 0.83f), steel);

            // Legs, apron and lower shelf
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    Part(table, "Leg", PrimitiveType.Cube, new Vector3(p.x + sx * 0.69f, (floor + p.y + 0.55f) / 2f, p.z + sz * 0.35f), new Vector3(0.07f, p.y + 0.55f - floor, 0.07f), steel);
                    Part(table, "Foot", PrimitiveType.Cube, new Vector3(p.x + sx * 0.69f, floor + 0.015f, p.z + sz * 0.35f), new Vector3(0.1f, 0.03f, 0.1f), dark);
                }
            foreach (float sz in new[] { -1f, 1f })
            {
                Part(table, "Apron", PrimitiveType.Cube, p + new Vector3(0, 0.5f, sz * 0.35f), new Vector3(1.3f, 0.09f, 0.03f), steel);
                Part(table, "Stretcher", PrimitiveType.Cube, p + new Vector3(0, -0.34f, sz * 0.35f), new Vector3(1.3f, 0.04f, 0.03f), steel);
            }
            foreach (float sx in new[] { -1f, 1f })
            {
                Part(table, "ApronSide", PrimitiveType.Cube, p + new Vector3(sx * 0.69f, 0.5f, 0), new Vector3(0.03f, 0.09f, 0.63f), steel);
                Part(table, "StretcherSide", PrimitiveType.Cube, p + new Vector3(sx * 0.69f, -0.34f, 0), new Vector3(0.03f, 0.04f, 0.63f), steel);
            }
            Part(table, "Shelf", PrimitiveType.Cube, p + new Vector3(0, -0.31f, 0), new Vector3(1.32f, 0.03f, 0.66f), darkWood);
            // Drawer in the front apron
            Part(table, "DrawerFace", PrimitiveType.Cube, p + new Vector3(0.3f, 0.5f, -0.372f), new Vector3(0.55f, 0.08f, 0.012f), dark);
            Part(table, "DrawerHandle", PrimitiveType.Cube, p + new Vector3(0.3f, 0.5f, -0.388f), new Vector3(0.18f, 0.015f, 0.02f), yellow);

            // Under the table: a toolbox, a cable coil and a spare ammo crate
            Part(table, "Toolbox", PrimitiveType.Cube, p + new Vector3(-0.4f, -0.21f, 0.0f), new Vector3(0.44f, 0.17f, 0.2f), red);
            Part(table, "ToolboxHandle", PrimitiveType.Cube, p + new Vector3(-0.4f, -0.1f, 0.0f), new Vector3(0.2f, 0.02f, 0.03f), dark);
            Part(table, "CableCoil", PrimitiveType.Cylinder, p + new Vector3(0.1f, -0.27f, 0.05f), new Vector3(0.34f, 0.03f, 0.34f), cable);
            Part(table, "AmmoCan", PrimitiveType.Cube, p + new Vector3(0.45f, -0.22f, 0.0f), new Vector3(0.3f, 0.17f, 0.17f), Lit("Can_Olive", new Color(0.25f, 0.28f, 0.16f), 0.4f, 0.4f));

            // On top: clipboard with papers, mug, desk lamp, handset on its cable
            Part(table, "Clipboard", PrimitiveType.Cube, p + new Vector3(0.55f, 0.605f, -0.2f), new Vector3(0.22f, 0.008f, 0.3f), wood, Quaternion.Euler(0, 15, 0), shadows: false);
            Part(table, "Papers", PrimitiveType.Cube, p + new Vector3(0.55f, 0.612f, -0.2f), new Vector3(0.19f, 0.006f, 0.26f), paper, Quaternion.Euler(0, 15, 0), shadows: false);
            Part(table, "Mug", PrimitiveType.Cylinder, p + new Vector3(-0.62f, 0.65f, -0.22f), new Vector3(0.08f, 0.05f, 0.08f), Lit("Mug_Blue", new Color(0.12f, 0.3f, 0.5f), 0f, 0.6f));
            var lampMetal = Lit("Lamp_Metal", new Color(0.2f, 0.22f, 0.2f), 0.7f, 0.5f);
            var lampGlow = Lit("Lamp_Glow", new Color(1f, 0.85f, 0.5f), 0f, 0.5f, new Color(3f, 2.2f, 1.2f));
            Part(table, "LampBase", PrimitiveType.Cylinder, p + new Vector3(-0.62f, 0.61f, 0.25f), new Vector3(0.14f, 0.01f, 0.14f), lampMetal);
            Bar(table, "LampArm", p + new Vector3(-0.62f, 0.61f, 0.25f), p + new Vector3(-0.62f, 0.95f, 0.18f), 0.015f, lampMetal);
            Bar(table, "LampArm2", p + new Vector3(-0.62f, 0.95f, 0.18f), p + new Vector3(-0.5f, 0.92f, 0.02f), 0.015f, lampMetal);
            Part(table, "LampShade", PrimitiveType.Cylinder, p + new Vector3(-0.5f, 0.88f, 0.02f), new Vector3(0.12f, 0.04f, 0.12f), lampMetal, Quaternion.Euler(20, 0, 0));
            Part(table, "LampBulb", PrimitiveType.Sphere, p + new Vector3(-0.5f, 0.86f, 0.02f), Vector3.one * 0.05f, lampGlow, shadows: false);
            PointLight(table, "TableLamp", p + new Vector3(-0.5f, 0.82f, 0.0f), new Color(1f, 0.82f, 0.55f), 1.2f, 4.5f);
            Part(table, "Handset", PrimitiveType.Cube, p + new Vector3(0.0f, 0.62f, -0.28f), new Vector3(0.12f, 0.04f, 0.06f), dark, Quaternion.Euler(0, -20, 0));
            Bar(table, "HandsetCable", p + new Vector3(0.05f, 0.62f, -0.26f), p + new Vector3(0.2f, 0.605f, 0.1f), 0.012f, cable);
        }

        // ------------------------------------------------------------------ compound extras

        /// <summary>Sandbag wall: staggered rows of flattened bags along a line, with one solid collider.</summary>
        static void Sandbags(Transform parent, Vector3 a, Vector3 b, int rows, int seed)
        {
            var bag = Lit("Sandbag", new Color(0.5f, 0.42f, 0.27f), 0f, 0.1f);
            var rng = new System.Random(seed);
            var wall = Group(parent, "SandbagWall");
            Vector3 d = b - a;
            float length = d.magnitude;
            Vector3 dir = d / length;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            int count = Mathf.Max(2, Mathf.RoundToInt(length / 0.55f));
            for (int row = 0; row < rows; row++)
                for (int i = 0; i < count + (row % 2 == 0 ? 0 : -1); i++)
                {
                    float t = (i + 0.5f + (row % 2 == 0 ? 0f : 0.5f)) / count;
                    Vector3 pos = a + d * t;
                    pos.y = GroundY(pos.x, pos.z) + 0.1f + row * 0.17f;
                    Part(wall, "Bag", PrimitiveType.Sphere, pos, new Vector3(0.72f, 0.22f, 0.44f), bag,
                        Quaternion.Euler((float)(rng.NextDouble() - 0.5) * 6f, yaw + (float)(rng.NextDouble() - 0.5) * 10f, (float)(rng.NextDouble() - 0.5) * 6f));
                }
            var col = new GameObject("Collider");
            col.transform.SetParent(wall, false);
            Vector3 mid = (a + b) * 0.5f;
            col.transform.SetPositionAndRotation(new Vector3(mid.x, GroundY(mid.x, mid.z) + (rows * 0.17f + 0.1f) / 2f, mid.z), Quaternion.Euler(0, yaw, 0));
            var box = col.AddComponent<BoxCollider>();
            box.size = new Vector3(0.45f, rows * 0.17f + 0.1f, length + 0.3f);
            var obstacle = col.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = box.size;
        }

        static void Floodlight(Transform parent, Vector3 foot, Vector3 aimAt)
        {
            var steel = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var glow = Lit("Flood_Glow", new Color(1f, 0.95f, 0.8f), 0f, 0.5f, new Color(4f, 3.6f, 2.8f));
            var g = Group(parent, "Floodlight");
            foreach (float a in new[] { 0f, 120f, 240f })
            {
                Vector3 legEnd = foot + Quaternion.Euler(0, a, 0) * new Vector3(0, 0, 0.7f);
                Bar(g, "Leg", foot + Vector3.up * 1.3f, new Vector3(legEnd.x, GroundY(legEnd.x, legEnd.z), legEnd.z), 0.04f, steel);
            }
            Vector3 top = foot + Vector3.up * 2.6f;
            Bar(g, "Mast", foot + Vector3.up * 1.2f, top, 0.07f, steel);
            var rot = Quaternion.LookRotation((aimAt - top).normalized, Vector3.up);
            Part(g, "Head", PrimitiveType.Cube, top + rot * Vector3.forward * 0.08f, new Vector3(0.62f, 0.4f, 0.16f), dark, rot);
            Part(g, "Lens", PrimitiveType.Cube, top + rot * Vector3.forward * 0.165f, new Vector3(0.54f, 0.32f, 0.01f), glow, rot, shadows: false);
            var spot = new GameObject("FloodSpot").AddComponent<Light>();
            spot.transform.SetParent(g, false);
            spot.transform.SetPositionAndRotation(top + rot * Vector3.forward * 0.2f, rot);
            spot.type = LightType.Spot;
            spot.spotAngle = 80f;
            spot.range = 22f;
            spot.intensity = 6f;
            spot.color = new Color(1f, 0.95f, 0.82f);
            spot.shadows = LightShadows.None;
        }

        static string DressCompoundExtras(Transform root, Vector3 p)
        {
            var group = Group(root, "CompoundExtras");
            var steel = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var tarp = Lit("Tarp_Olive", new Color(0.22f, 0.27f, 0.17f), 0f, 0.2f);
            var rope = Lit("Pier_Rope", new Color(0.55f, 0.45f, 0.3f), 0f, 0.1f);
            var wood = Lit("Pallet_Wood", new Color(0.45f, 0.33f, 0.2f), 0f, 0.2f);
            var concrete = Triplanar("Barrier_Concrete", "concrete_wall_008", new Color(0.8f, 0.8f, 0.78f), 2f, 0f, 0.3f);
            var canMat = ModelMat("metal_jerrycan", "metal_jerrycan", 0.45f, 0.5f);
            var crateMat = ModelMat("wooden_crate_01", "wooden_crate_01", 0.3f, 0f);
            int props = 0;
            float Gy(float x, float z) => GroundY(x, z);

            // Tarp canopy over the fuse table: four poles, a slightly sagging olive sheet, tie ropes and a lamp
            float ty = Gy(p.x, p.z);
            foreach (float sx in new[] { -2.0f, 2.0f })
                foreach (float sz in new[] { -1.5f, 1.5f })
                {
                    float gy = Gy(p.x + sx, p.z + sz);
                    Part(group, "CanopyPole", PrimitiveType.Cylinder, new Vector3(p.x + sx, gy + 1.45f, p.z + sz), new Vector3(0.08f, 1.45f, 0.08f), steel);
                    Part(group, "PoleFoot", PrimitiveType.Cylinder, new Vector3(p.x + sx, gy + 0.03f, p.z + sz), new Vector3(0.4f, 0.03f, 0.4f), dark);
                }
            Part(group, "Tarp", PrimitiveType.Cube, new Vector3(p.x, ty + 2.78f, p.z), new Vector3(4.6f, 0.04f, 3.5f), tarp, Quaternion.Euler(0, 0, 3f), shadows: true);
            Part(group, "TarpEdgeFront", PrimitiveType.Cube, new Vector3(p.x, ty + 2.62f, p.z - 1.77f), new Vector3(4.6f, 0.22f, 0.03f), tarp, Quaternion.Euler(0, 0, 3f));
            foreach (float sx in new[] { -2.0f, 2.0f })
                foreach (float sz in new[] { -1.5f, 1.5f })
                    Bar(group, "TieRope", new Vector3(p.x + sx, ty + 2.75f, p.z + sz), new Vector3(p.x + sx * 1.4f, Gy(p.x + sx * 1.4f, p.z + sz * 1.3f) + 0.05f, p.z + sz * 1.3f), 0.02f, rope);
            PointLight(group, "CanopyLamp", new Vector3(p.x, ty + 2.5f, p.z - 0.3f), new Color(1f, 0.88f, 0.65f), 1.6f, 7f);

            // Folding chair at the table
            Vector3 chair = new Vector3(p.x, ty, p.z - 1.15f);
            Part(group, "ChairSeat", PrimitiveType.Cube, chair + Vector3.up * 0.46f, new Vector3(0.42f, 0.03f, 0.4f), Lit("Stool_Seat", new Color(0.25f, 0.27f, 0.2f), 0f, 0.2f));
            Part(group, "ChairBack", PrimitiveType.Cube, chair + new Vector3(0, 0.72f, -0.19f), new Vector3(0.42f, 0.3f, 0.025f), Lit("Stool_Seat", new Color(0.25f, 0.27f, 0.2f), 0f, 0.2f));
            foreach (float sx in new[] { -0.19f, 0.19f })
                foreach (float sz in new[] { -0.17f, 0.17f })
                    Bar(group, "ChairLeg", chair + new Vector3(sx, 0.45f, sz), chair + new Vector3(sx * 1.2f, 0f, sz * 1.2f), 0.025f, steel);

            // Supply stacks: crates and jerrycans west of the table, a pallet stack by the generator
            Vector3 stack = new Vector3(p.x - 3.2f, 0, p.z - 3.6f);
            foreach (var (dx, dz, kind, yaw) in new[] { (0f, 0f, 1, 10f), (1.2f, 0.1f, 1, -6f), (0.55f, 0.05f, 1, 4f), (-1.0f, 0.4f, 0, 20f), (2.3f, -0.2f, 2, 40f), (2.6f, 0.2f, 2, 10f) })
            {
                Vector3 pos = new Vector3(stack.x + dx, Gy(stack.x + dx, stack.z + dz), stack.z + dz);
                if (kind == 0) { var c = Model("wooden_crate_01", "wooden_crate_01", group, pos, yaw, 1.1f, Anchor.Bottom, crateMat); if (c != null) { SolidProp(c); props++; } }
                else if (kind == 1) { var c = Model("wooden_military_crate", "wooden_military_crate", group, pos, yaw, 1.15f, Anchor.Bottom, null); if (c != null) { SolidProp(c); props++; } }
                else { var c = Model("metal_jerrycan", "metal_jerrycan", group, pos, yaw, 1.1f, Anchor.Bottom, canMat); if (c != null) { SolidProp(c); props++; } }
            }
            var top = Model("wooden_military_crate", "wooden_military_crate", group, new Vector3(stack.x + 0.55f, Gy(stack.x + 0.55f, stack.z + 0.05f) + 0.6f, stack.z + 0.05f), -12f, 1.0f, Anchor.Bottom, null);
            if (top != null) props++;
            Vector3 pal = new Vector3(p.x - 5.6f, 0, p.z + 0.6f);
            for (int i = 0; i < 3; i++)
            {
                float gy = Gy(pal.x, pal.z) + i * 0.16f;
                var pallet = Group(group, "Pallet");
                Part(pallet, "Boards", PrimitiveType.Cube, new Vector3(pal.x, gy + 0.13f, pal.z), new Vector3(1.2f, 0.03f, 1.0f), wood, Quaternion.Euler(0, i * 7f, 0));
                foreach (float x in new[] { -0.5f, 0f, 0.5f })
                    Part(pallet, "Runner", PrimitiveType.Cube, new Vector3(pal.x + x, gy + 0.06f, pal.z), new Vector3(0.1f, 0.1f, 1.0f), wood, Quaternion.Euler(0, i * 7f, 0));
            }
            props++;

            // Cable spool beside the generator
            Vector3 spool = new Vector3(p.x - 4.9f, Gy(p.x - 4.9f, p.z + 3.0f) + 0.45f, p.z + 3.0f);
            Part(group, "SpoolCore", PrimitiveType.Cylinder, spool, new Vector3(0.45f, 0.3f, 0.45f), Lit("Compound_Cable", new Color(0.02f, 0.02f, 0.02f), 0f, 0.3f), Quaternion.Euler(0, 0, 90));
            foreach (float sx in new[] { -0.33f, 0.33f })
                Part(group, "SpoolDisc", PrimitiveType.Cylinder, spool + new Vector3(sx, 0, 0), new Vector3(0.95f, 0.025f, 0.95f), wood, Quaternion.Euler(0, 0, 90));
            props++;

            // Sandbag cover around the approaches and a jersey-barrier chicane inside the gate
            Sandbags(group, new Vector3(p.x + 11f, 0, p.z - 4.5f), new Vector3(p.x + 14f, 0, p.z - 4.5f), 3, 1);
            Sandbags(group, new Vector3(p.x + 11f, 0, p.z + 5.5f), new Vector3(p.x + 14f, 0, p.z + 5.5f), 3, 2);
            Sandbags(group, new Vector3(p.x - 11f, 0, p.z + 6f), new Vector3(p.x - 8f, 0, p.z + 6f), 2, 3);
            Sandbags(group, new Vector3(p.x - 11f, 0, p.z + 6f), new Vector3(p.x - 11f, 0, p.z + 3f), 2, 4);
            props += 4;
            foreach (var (dx, dz, yaw) in new[] { (9.5f, -2.2f, 8f), (9.5f, 3.4f, -6f), (7.6f, 0.6f, 90f) })
            {
                Vector3 pos = new Vector3(p.x + dx, Gy(p.x + dx, p.z + dz) + 0.4f, p.z + dz);
                var block = Part(group, "JerseyBarrier", PrimitiveType.Cube, pos, new Vector3(0.5f, 0.8f, 2.2f), concrete, Quaternion.Euler(0, yaw, 0), collider: true);
                var ob = block.AddComponent<NavMeshObstacle>(); ob.carving = true;
                Part(group, "BarrierStripe", PrimitiveType.Cube, pos + Vector3.up * 0.2f, new Vector3(0.52f, 0.12f, 2.22f), yellow, Quaternion.Euler(0, yaw, 0), shadows: false);
                props++;
            }

            // Floodlights on tripods covering the gate and the dish approach
            Floodlight(group, new Vector3(p.x + 9f, Gy(p.x + 9f, p.z - 6f), p.z - 6f), new Vector3(p.x + 12f, p.y - 1f, p.z));
            Floodlight(group, new Vector3(p.x - 9f, Gy(p.x - 9f, p.z + 7f), p.z + 7f), new Vector3(p.x - 4f, p.y - 1f, p.z + 1f));

            // Signs on posts
            foreach (var (pos, facing, text, plate, fg) in new[]
            {
                (new Vector3(p.x + 10.5f, 0, p.z - 1.7f), Vector3.left, "RESTRICTED\nSIGNAL STATION", new Color(0.7f, 0.05f, 0.04f), Color.white),
                (new Vector3(p.x + 3.4f, 0, p.z + 3.4f), Vector3.back, "DANGER\nHIGH VOLTAGE", new Color(0.85f, 0.62f, 0.05f), Color.black),
                (new Vector3(p.x - 6.5f, 0, p.z - 2.4f), Vector3.forward, "GENERATOR\nKEEP CLEAR", new Color(0.85f, 0.62f, 0.05f), Color.black),
            })
            {
                float gy = Gy(pos.x, pos.z);
                Part(group, "SignPost", PrimitiveType.Cylinder, new Vector3(pos.x, gy + 0.95f, pos.z), new Vector3(0.07f, 0.95f, 0.07f), steel);
                DecSign(group, text, new Vector3(pos.x, gy + 1.65f, pos.z) + facing * 0.05f, facing, 0.7f, 1.2f, fg, plate, new Vector2(0.9f, 0.5f));
            }
            return $"{props} props, canopy, table lamp, 2 floodlights, sandbags, barriers, signs";
        }

        /// <summary>The Fuse 3 pickup stands on a plain box (Fuse3_Crate); dress it as a steel-framed wooden table with a glowing fuse cradle.</summary>
        static string DressFuseCrate(Transform root)
        {
            var crate = GameObject.Find("Fuse3_Crate");
            if (crate == null) return "no Fuse3_Crate";
            HideRenderer(crate);
            Vector3 c = crate.transform.position;
            float g = c.y - crate.transform.lossyScale.y / 2f;   // ground under the box
            var group = Group(root, "Fuse3Table");
            var wood = Lit("Table_Wood", new Color(0.38f, 0.26f, 0.15f), 0f, 0.3f);
            var darkWood = Lit("Table_WoodDark", new Color(0.2f, 0.14f, 0.09f), 0f, 0.25f);
            var steel = Steel("Console_Steel", new Color(0.42f, 0.46f, 0.5f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var paper = Lit("Paper_White", new Color(0.88f, 0.86f, 0.78f), 0f, 0.15f);
            var glow = Lit("Fuse_Cradle_Glow", new Color(0.4f, 0.9f, 1f), 0f, 0.5f, new Color(1f, 3f, 4f));
            float top = g + 0.9f;

            Part(group, "Top", PrimitiveType.Cube, new Vector3(c.x, top - 0.025f, c.z), new Vector3(1.3f, 0.05f, 0.8f), wood);
            for (int i = 1; i < 4; i++)
                Part(group, "PlankLine", PrimitiveType.Cube, new Vector3(c.x - 0.65f + i * 0.325f, top + 0.0005f, c.z), new Vector3(0.008f, 0.002f, 0.78f), darkWood, shadows: false);
            Part(group, "RimFront", PrimitiveType.Cube, new Vector3(c.x, top - 0.025f, c.z - 0.405f), new Vector3(1.32f, 0.06f, 0.025f), steel);
            Part(group, "RimBack", PrimitiveType.Cube, new Vector3(c.x, top - 0.025f, c.z + 0.405f), new Vector3(1.32f, 0.06f, 0.025f), steel);
            foreach (float sx in new[] { -1f, 1f })
            {
                Part(group, "RimSide", PrimitiveType.Cube, new Vector3(c.x + sx * 0.655f, top - 0.025f, c.z), new Vector3(0.025f, 0.06f, 0.83f), steel);
                foreach (float sz in new[] { -1f, 1f })
                {
                    Part(group, "Leg", PrimitiveType.Cube, new Vector3(c.x + sx * 0.58f, g + 0.425f, c.z + sz * 0.33f), new Vector3(0.07f, 0.85f, 0.07f), steel);
                    Part(group, "Foot", PrimitiveType.Cube, new Vector3(c.x + sx * 0.58f, g + 0.015f, c.z + sz * 0.33f), new Vector3(0.1f, 0.03f, 0.1f), dark);
                }
                Part(group, "ApronSide", PrimitiveType.Cube, new Vector3(c.x + sx * 0.58f, g + 0.8f, c.z), new Vector3(0.03f, 0.09f, 0.6f), steel);
                Part(group, "StretcherSide", PrimitiveType.Cube, new Vector3(c.x + sx * 0.58f, g + 0.22f, c.z), new Vector3(0.03f, 0.04f, 0.6f), steel);
            }
            foreach (float sz in new[] { -1f, 1f })
            {
                Part(group, "Apron", PrimitiveType.Cube, new Vector3(c.x, g + 0.8f, c.z + sz * 0.33f), new Vector3(1.2f, 0.09f, 0.03f), steel);
                Part(group, "Stretcher", PrimitiveType.Cube, new Vector3(c.x, g + 0.22f, c.z + sz * 0.33f), new Vector3(1.2f, 0.04f, 0.03f), steel);
            }
            Part(group, "Shelf", PrimitiveType.Cube, new Vector3(c.x, g + 0.25f, c.z), new Vector3(1.2f, 0.03f, 0.62f), darkWood);
            Part(group, "AmmoCan", PrimitiveType.Cube, new Vector3(c.x - 0.3f, g + 0.35f, c.z), new Vector3(0.3f, 0.17f, 0.17f), Lit("Can_Olive", new Color(0.25f, 0.28f, 0.16f), 0.4f, 0.4f));
            Part(group, "Toolbox", PrimitiveType.Cube, new Vector3(c.x + 0.3f, g + 0.36f, c.z), new Vector3(0.4f, 0.17f, 0.2f), Lit("Toolbox_Red", new Color(0.7f, 0.08f, 0.06f), 0.5f, 0.5f));

            // Fuse cradle where the pickup floats: a yellow bezel, a dark pad and a glowing ring
            Part(group, "CradleBezel", PrimitiveType.Cylinder, new Vector3(c.x, top + 0.008f, c.z), new Vector3(0.42f, 0.008f, 0.42f), yellow);
            Part(group, "CradlePad", PrimitiveType.Cylinder, new Vector3(c.x, top + 0.014f, c.z), new Vector3(0.34f, 0.006f, 0.34f), dark);
            Part(group, "CradleRing", PrimitiveType.Cylinder, new Vector3(c.x, top + 0.019f, c.z), new Vector3(0.28f, 0.003f, 0.28f), glow, shadows: false);
            // Papers, a mug and a small warning plate at the back edge
            Part(group, "Papers", PrimitiveType.Cube, new Vector3(c.x - 0.45f, top + 0.004f, c.z - 0.15f), new Vector3(0.2f, 0.006f, 0.27f), paper, Quaternion.Euler(0, 12, 0), shadows: false);
            Part(group, "Mug", PrimitiveType.Cylinder, new Vector3(c.x + 0.5f, top + 0.05f, c.z - 0.2f), new Vector3(0.08f, 0.05f, 0.08f), Lit("Mug_Blue", new Color(0.12f, 0.3f, 0.5f), 0f, 0.6f));
            DecSign(group, "FUSE 3", new Vector3(c.x, top + 0.12f, c.z + 0.36f), Vector3.back, 0.5f, 0.5f, Color.black, new Color(0.85f, 0.62f, 0.05f), new Vector2(0.4f, 0.14f));
            Bar(group, "SignLeg", new Vector3(c.x - 0.15f, top, c.z + 0.355f), new Vector3(c.x - 0.15f, top + 0.05f, c.z + 0.355f), 0.015f, steel);
            Bar(group, "SignLeg2", new Vector3(c.x + 0.15f, top, c.z + 0.355f), new Vector3(c.x + 0.15f, top + 0.05f, c.z + 0.355f), 0.015f, steel);
            return $"fuse table at ({c.x:F0},{g:F1},{c.z:F0})";
        }

        static int HideSpawnMarkers()
        {
            int hidden = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                if (!r.name.StartsWith("WaveGate") && !r.name.StartsWith("SpawnMarker") && !r.name.StartsWith("BossReinforcement")) continue;
                r.enabled = false;
                hidden++;
            }
            return hidden;
        }
    }
}
