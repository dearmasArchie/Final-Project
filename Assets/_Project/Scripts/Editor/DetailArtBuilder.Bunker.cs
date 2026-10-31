using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        /// <summary>Underground scene: elevator cage, then structure, pipes, lamps, door frames and equipment in every room.</summary>
        static string DressBunker()
        {
            var root = FreshRoot();
            Physics.SyncTransforms();
            var report = new List<string>();
            report.Add("elevator " + BuildElevator(root, "ElevatorPanel_Up", Vector3.forward, Vector3.forward, 4f));
            report.Add("structure " + BunkerStructure(root));
            report.Add("equipment " + BunkerEquipment(root));
            report.Add(DressHubLighting(root));
            return string.Join("; ", report);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Round pipe or rod between two points.</summary>
        static GameObject Tube(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            Vector3 d = b - a;
            go.transform.position = (a + b) * 0.5f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            go.transform.localScale = new Vector3(radius * 2f, d.magnitude * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>True when something other than the room shell (walls, floors, ceilings) or this pass is inside the box.</summary>
        static bool Occupied(Bounds b, float margin = 0.1f)
        {
            b.Expand(margin * 2f);
            foreach (var c in Physics.OverlapBox(b.center, b.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                string n = c.name;
                if (n == "Floor" || n == "Ceiling" || n.StartsWith("Wall") || n == "CageCollider") continue;
                if (c.transform.root.name == RootName || c.GetComponentInParent<Transform>().name == RootName) continue;
                return true;
            }
            return false;
        }

        /// <summary>Places a model if its spot is free; returns whether it was kept.</summary>
        static bool PlaceChecked(string folder, string file, Transform parent, Vector3 pos, float yaw, float scale, Anchor anchor, Material main)
        {
            var go = Model(folder, file, parent, pos, yaw, scale, anchor, main);
            if (go == null) return false;
            var b = WorldBounds(go);
            // Colliders appear only after a sync; the model itself has none, so test its box against the scene
            Physics.SyncTransforms();
            if (Occupied(b, 0.05f)) { Object.DestroyImmediate(go); return false; }
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(b.center);
            var local = go.transform.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            Static(go);
            return true;
        }

        // ------------------------------------------------------------------ structure

        struct RoomInfo { public string name; public Vector3 center; public Vector2 size; public float floorY; public float height; }

        static string BunkerStructure(Transform root)
        {
            var concrete = Triplanar("Bunker_Beam", "concrete_wall_008", new Color(1.0f, 1.0f, 0.97f), 3f, 0f, 0.35f);
            var steel = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var blue = Lit("Pipe_Blue", new Color(0.12f, 0.32f, 0.55f), 0.5f, 0.45f);
            var grey = Lit("Pipe_Grey", new Color(0.32f, 0.34f, 0.36f), 0.7f, 0.4f);
            var red = Lit("Pipe_Red", new Color(0.65f, 0.1f, 0.07f), 0.4f, 0.45f);
            var hazard = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Greybox/Greybox_Hazard.mat") ?? yellow;
            var lampMat = ModelMat("caged_hanging_light", "caged_hanging_light", 0.5f, 0.6f, emissive: true);

            var rooms = new[]
            {
                new RoomInfo { name = "ElevatorRoom", center = new Vector3(0, 0, 0), size = new Vector2(8, 8), floorY = 0, height = 4 },
                new RoomInfo { name = "Hub", center = new Vector3(0, 0, 20), size = new Vector2(16, 16), floorY = 0, height = 4 },
                new RoomInfo { name = "BreakerRoom", center = new Vector3(-36, 0, 20), size = new Vector2(12, 12), floorY = 0, height = 4 },
                new RoomInfo { name = "FloodedGenerator", center = new Vector3(26, -0.3f, 20), size = new Vector2(20, 16), floorY = -0.3f, height = 4.3f },
                new RoomInfo { name = "QuarantineRoom", center = new Vector3(44, 0, 20), size = new Vector2(16, 16), floorY = 0, height = 4 },
            };

            int beams = 0, pillars = 0, pipes = 0, lamps = 0, frames = 0;
            var group = Group(root, "Structure");

            foreach (var r in rooms)
            {
                float top = r.floorY + r.height;
                float hx = r.size.x / 2f, hz = r.size.y / 2f;
                bool alongX = r.size.x >= r.size.y;

                // Ceiling beams across the short direction every 4 m
                float length = alongX ? r.size.x : r.size.y;
                for (float t = -length / 2f + 2f; t < length / 2f - 0.5f; t += 4f)
                {
                    var pos = r.center + (alongX ? new Vector3(t, 0, 0) : new Vector3(0, 0, t));
                    var size = alongX ? new Vector3(0.35f, 0.32f, r.size.y) : new Vector3(r.size.x, 0.32f, 0.35f);
                    Part(group, "Beam", PrimitiveType.Cube, new Vector3(pos.x, top - 0.16f, pos.z), size, concrete);
                    Part(group, "BeamFlange", PrimitiveType.Cube, new Vector3(pos.x, top - 0.34f, pos.z),
                        alongX ? new Vector3(0.6f, 0.04f, r.size.y) : new Vector3(r.size.x, 0.04f, 0.6f), steel);
                    beams++;
                }

                // Corner pillars on steel plinths, with a yellow band
                foreach (float sx in new[] { -1f, 1f })
                    foreach (float sz in new[] { -1f, 1f })
                    {
                        var pos = new Vector3(r.center.x + sx * (hx - 0.24f), r.floorY, r.center.z + sz * (hz - 0.24f));
                        var pillar = Part(group, "Pillar", PrimitiveType.Cube, pos + Vector3.up * (r.height / 2f), new Vector3(0.48f, r.height, 0.48f), concrete, collider: true);
                        Part(group, "Plinth", PrimitiveType.Cube, pos + Vector3.up * 0.1f, new Vector3(0.58f, 0.2f, 0.58f), steel);
                        Part(group, "PillarBand", PrimitiveType.Cube, pos + Vector3.up * 1.1f, new Vector3(0.5f, 0.18f, 0.5f), hazard);
                        pillars++;
                    }

                // Two pipe runs along the longer walls, above door height, with clamps and a valve
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 a, b;
                    if (alongX) { a = new Vector3(r.center.x - hx + 0.5f, 0, r.center.z + side * (hz - 0.22f)); b = new Vector3(r.center.x + hx - 0.5f, 0, a.z); }
                    else { a = new Vector3(r.center.x + side * (hx - 0.22f), 0, r.center.z - hz + 0.5f); b = new Vector3(a.x, 0, r.center.z + hz - 0.5f); }
                    for (int k = 0; k < 2; k++)
                    {
                        float y = top - 0.55f - k * 0.26f;
                        Vector3 pa = new Vector3(a.x, y, a.z), pb = new Vector3(b.x, y, b.z);
                        Tube(group, "Pipe", pa, pb, 0.08f, k == 0 ? (side < 0 ? blue : grey) : red);
                        pipes++;
                        int clamps = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(pa, pb) / 2.5f));
                        for (int c = 0; c <= clamps; c++)
                        {
                            Vector3 cp = Vector3.Lerp(pa, pb, c / (float)clamps);
                            Part(group, "Clamp", PrimitiveType.Cube, cp, alongX ? new Vector3(0.1f, 0.2f, 0.2f) : new Vector3(0.2f, 0.2f, 0.1f), steel);
                        }
                        if (k == 0)
                        {
                            Vector3 mid = Vector3.Lerp(pa, pb, 0.4f);
                            Tube(group, "Flange", mid - (pb - pa).normalized * 0.06f, mid + (pb - pa).normalized * 0.06f, 0.115f, steel);
                            Part(group, "ValveWheel", PrimitiveType.Cylinder, mid + Vector3.up * 0.2f, new Vector3(0.22f, 0.012f, 0.22f), red);
                            Tube(group, "ValveStem", mid, mid + Vector3.up * 0.2f, 0.015f, steel);
                        }
                    }
                }
            }

            // Caged lamps on the ceiling (light comes from the room lights; the model gives them a body)
            var lampSpots = new List<(Vector3, float)>();
            foreach (var r in rooms)
            {
                float top = r.floorY + r.height;
                var step = new Vector2(r.size.x > 10 ? r.size.x / 4f : r.size.x / 4f, r.size.y > 10 ? r.size.y / 4f : r.size.y / 4f);
                if (r.size.x <= 8) lampSpots.Add((new Vector3(r.center.x, top, r.center.z + 2f), 0f));
                else
                    foreach (float sx in r.size.x > 12 ? new[] { -1f, 1f } : new[] { -1f })
                        foreach (float sz in new[] { -1f, 1f })
                            lampSpots.Add((new Vector3(r.center.x + sx * step.x, top, r.center.z + sz * step.y), 90f));
            }
            // Corridors: laser corridor (red alarm look kept by the hazard lights), hub and generator links
            foreach (float x in new[] { -26f, -19f, -12f }) lampSpots.Add((new Vector3(x, 4f, 20f), 90f));
            lampSpots.Add((new Vector3(0, 4f, 8f), 0f));
            lampSpots.Add((new Vector3(12f, 4f, 20f), 90f));
            foreach (var (pos, yaw) in lampSpots)
            {
                Model("caged_hanging_light", "caged_hanging_light", group, pos, yaw, 0.7f, Anchor.Top, lampMat);
                lamps++;
            }

            // Door frames: yellow posts and a hazard header around every opening (4 m x 3.2 m)
            var openings = new List<(Vector3 pos, bool wallAlongX)>
            {
                (new Vector3(0, 0, 4), true), (new Vector3(0, 0, 12), true),
                (new Vector3(-30, 0, 20), false), (new Vector3(-8, 0, 20), false), (new Vector3(8, 0, 20), false),
                (new Vector3(16, -0.3f, 20), false), (new Vector3(36, -0.3f, 20), false),
            };
            foreach (var (pos, wallAlongX) in openings)
            {
                Vector3 across = wallAlongX ? Vector3.right : Vector3.forward;   // direction along the wall
                Vector3 thick = wallAlongX ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1);
                Vector3 postSize = wallAlongX ? new Vector3(0.3f, 3.5f, 0.55f) : new Vector3(0.55f, 3.5f, 0.3f);
                foreach (float s in new[] { -1f, 1f })
                    Part(group, "FramePost", PrimitiveType.Cube, pos + across * (s * 2.12f) + Vector3.up * 1.75f, postSize, yellow);
                Part(group, "FrameHeader", PrimitiveType.Cube, pos + Vector3.up * 3.35f, wallAlongX ? new Vector3(4.55f, 0.3f, 0.6f) : new Vector3(0.6f, 0.3f, 4.55f), hazard);
                frames++;
            }
            return $"beams={beams}, pillars={pillars}, pipes={pipes}, lamps={lamps}, door frames={frames}";
        }

        // ------------------------------------------------------------------ equipment

        static string BunkerEquipment(Transform root)
        {
            var group = Group(root, "Equipment");
            int placed = 0, skipped = 0;
            void Try(bool ok) { if (ok) placed++; else skipped++; }

            var shelfMat = ModelMat("steel_frame_shelves_01", "steel_frame_shelves_01", 0.4f, 0.6f);
            var powerMat = ModelMat("power_box_01", "power_box_01", 0.45f, 0.5f);
            var utilityMat = ModelMat("utility_box_01", "utility_box_01", 0.45f, 0.5f);
            var genMat = ModelMat("portable_generator", "portable_generator", 0.4f, 0.3f);

            // Shelves against walls (front faces into the room)
            Try(PlaceChecked("steel_frame_shelves_01", "steel_frame_shelves_01", group, new Vector3(-5f, 0, 12.45f), 0f, 1.25f, Anchor.Bottom, shelfMat));
            Try(PlaceChecked("steel_frame_shelves_01", "steel_frame_shelves_01", group, new Vector3(5f, 0, 12.45f), 0f, 1.25f, Anchor.Bottom, shelfMat));
            Try(PlaceChecked("steel_frame_shelves_01", "steel_frame_shelves_01", group, new Vector3(-38f, 0, 25.6f), 180f, 1.25f, Anchor.Bottom, shelfMat));
            Try(PlaceChecked("steel_frame_shelves_01", "steel_frame_shelves_01", group, new Vector3(-34f, 0, 25.6f), 180f, 1.25f, Anchor.Bottom, shelfMat));
            Try(PlaceChecked("steel_frame_shelves_01", "steel_frame_shelves_01", group, new Vector3(41f, 0, 27.6f), 180f, 1.25f, Anchor.Bottom, shelfMat));
            Try(PlaceChecked("steel_frame_shelves_01", "steel_frame_shelves_01", group, new Vector3(1.5f, 0, -3.55f), 0f, 1.25f, Anchor.Bottom, shelfMat));

            // Power cabinets beside doors and by the hub
            Try(PlaceChecked("power_box_01", "power_box_01", group, new Vector3(7.6f, 1.5f, 15.5f), -90f, 1.6f, Anchor.Center, powerMat));
            Try(PlaceChecked("power_box_01", "power_box_01", group, new Vector3(-7.6f, 1.5f, 24.5f), 90f, 1.6f, Anchor.Center, powerMat));
            Try(PlaceChecked("utility_box_01", "utility_box_01", group, new Vector3(-3.5f, 0, 27.65f), 180f, 1.4f, Anchor.Bottom, utilityMat));
            Try(PlaceChecked("utility_box_01", "utility_box_01", group, new Vector3(-29.65f, 0, 17f), 90f, 1.3f, Anchor.Bottom, utilityMat));
            Try(PlaceChecked("utility_box_01", "utility_box_01", group, new Vector3(35.65f, 0, 26f), 90f, 1.3f, Anchor.Bottom, utilityMat));
            Try(PlaceChecked("power_box_01", "power_box_01", group, new Vector3(-34f, 1.6f, 14.35f), 0f, 1.6f, Anchor.Center, powerMat));

            // Generator set in the quarantine room corner, a leftover from when the wing was staffed
            Try(PlaceChecked("portable_generator", "portable_generator", group, new Vector3(51.6f, 0, 23f), -90f, 1.8f, Anchor.Bottom, genMat));
            return $"placed={placed}, skipped (spot taken)={skipped}, {DressBunkerProps(root)}";
        }
    }
}
