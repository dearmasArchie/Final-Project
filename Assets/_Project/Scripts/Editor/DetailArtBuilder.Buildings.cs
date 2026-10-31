using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        struct Building
        {
            public string name;
            public Vector2 center;     // x, z
            public Vector2 size;       // x, z
            public float height;       // wall height above the ground at the center
            public Vector2 doorWall;   // outward normal of the wall holding the door (x, z)
            public Vector2 doorAt;     // door center on the wall (x, z)
            public string label;
        }

        /// <summary>
        /// Exterior detail for the island buildings so they stop reading as plain boxes: concrete plinth and pilasters,
        /// a roof with an overhanging cap and parapet, vents and a stack on top, a steel canopy with a caged lamp over
        /// the door, a yellow door frame, concrete apron, wall cabinet with conduit, and a stenciled name plate.
        /// </summary>
        static string DressBuildings(Transform root)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return "no terrain";
            float Ground(float x, float z) => terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;

            var buildings = new[]
            {
                new Building { name = "Safe Room", center = new Vector2(90, 45), size = new Vector2(16, 9), height = 3.5f, doorWall = new Vector2(0, 1), doorAt = new Vector2(93, 49.5f), label = "SAFE ROOM" },
                new Building { name = "Elevator House", center = new Vector2(125, 110), size = new Vector2(8, 8), height = 4f, doorWall = new Vector2(0, -1), doorAt = new Vector2(125, 106), label = "FREIGHT ELEVATOR" },
                new Building { name = "Maintenance Bunker", center = new Vector2(37.36f, 131.03f), size = new Vector2(8.6f, 6.6f), height = 3.3f, doorWall = new Vector2(1, 0), doorAt = new Vector2(41.51f, 131.03f), label = "MAINTENANCE 03" },
                new Building { name = "Generator Bunker", center = new Vector2(175, 75), size = new Vector2(8.6f, 6.6f), height = 3.3f, doorWall = new Vector2(-1, 0), doorAt = new Vector2(170.85f, 75), label = "GENERATOR 01" },
            };

            var concrete = Triplanar("Building_Concrete", "concrete_wall_008", new Color(1.1f, 1.1f, 1.05f), 3f, 0f, 0.4f);
            var dark = Triplanar("Building_DarkConcrete", "concrete_wall_008", new Color(0.55f, 0.55f, 0.55f), 3f, 0f, 0.3f);
            var steel = Steel("Building_Steel", new Color(0.5f, 0.52f, 0.55f));
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var pipeBlue = Lit("Pipe_Blue", new Color(0.12f, 0.32f, 0.55f), 0.5f, 0.45f);
            var blackMat = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var powerMat = ModelMat("power_box_01", "power_box_01", 0.45f, 0.5f);
            var sconceMat = ModelMat("industrial_caged_sconce", "industrial_caged_sconce", 0.5f, 0.6f, emissive: true);

            var group = Group(root, "Buildings");
            int ribs = 0;
            foreach (var bd in buildings)
            {
                float gy = Ground(bd.center.x, bd.center.y);
                float hx = bd.size.x / 2f, hz = bd.size.y / 2f, top = gy + bd.height;
                Vector3 c = new Vector3(bd.center.x, gy, bd.center.y);
                var g = Group(group, bd.name);

                // Plinth around the base and a cornice under the roof
                foreach (var (offset, size) in new[] { (new Vector3(0, 0, hz + 0.1f), new Vector3(2 * hx + 0.4f, 0.55f, 0.35f)), (new Vector3(0, 0, -hz - 0.1f), new Vector3(2 * hx + 0.4f, 0.55f, 0.35f)),
                                                      (new Vector3(hx + 0.1f, 0, 0), new Vector3(0.35f, 0.55f, 2 * hz + 0.4f)), (new Vector3(-hx - 0.1f, 0, 0), new Vector3(0.35f, 0.55f, 2 * hz + 0.4f)) })
                    Part(g, "Plinth", PrimitiveType.Cube, c + offset + Vector3.up * 0.15f, size, dark);
                Part(g, "RoofCap", PrimitiveType.Cube, new Vector3(c.x, top + 0.04f, c.z), new Vector3(2 * hx + 0.7f, 0.28f, 2 * hz + 0.7f), concrete);
                // Parapet (leave the cap's center free for vents)
                foreach (var (offset, size) in new[] { (new Vector3(0, 0, hz + 0.3f), new Vector3(2 * hx + 0.7f, 0.4f, 0.14f)), (new Vector3(0, 0, -hz - 0.3f), new Vector3(2 * hx + 0.7f, 0.4f, 0.14f)),
                                                      (new Vector3(hx + 0.3f, 0, 0), new Vector3(0.14f, 0.4f, 2 * hz + 0.7f)), (new Vector3(-hx - 0.3f, 0, 0), new Vector3(0.14f, 0.4f, 2 * hz + 0.7f)) })
                    Part(g, "Parapet", PrimitiveType.Cube, new Vector3(c.x + offset.x, top + 0.36f, c.z + offset.z), size, concrete);

                // Pilasters on the walls (skip the door's neighborhood)
                Vector3 doorPos = new Vector3(bd.doorAt.x, gy, bd.doorAt.y);
                void Pilaster(Vector3 pos, bool alongX)
                {
                    if (Vector2.Distance(new Vector2(pos.x, pos.z), bd.doorAt) < 1.8f) return;
                    Part(g, "Pilaster", PrimitiveType.Cube, new Vector3(pos.x, gy + bd.height / 2f - 0.1f, pos.z),
                        alongX ? new Vector3(0.5f, bd.height - 0.1f, 0.34f) : new Vector3(0.34f, bd.height - 0.1f, 0.5f), concrete);
                    ribs++;
                }
                for (float x = -hx + 0.25f; x <= hx - 0.2f; x += Mathf.Max(2.5f, (2 * hx - 0.5f) / Mathf.Round((2 * hx - 0.5f) / 3.6f)))
                {
                    Pilaster(new Vector3(c.x + x, 0, c.z + hz + 0.02f), true);
                    Pilaster(new Vector3(c.x + x, 0, c.z - hz - 0.02f), true);
                }
                for (float z = -hz + 0.25f; z <= hz - 0.2f; z += Mathf.Max(2.5f, (2 * hz - 0.5f) / Mathf.Round((2 * hz - 0.5f) / 3.6f)))
                {
                    Pilaster(new Vector3(c.x + hx + 0.02f, 0, c.z + z), false);
                    Pilaster(new Vector3(c.x - hx - 0.02f, 0, c.z + z), false);
                }

                // Roof: two vent boxes with louvers, a pipe stack, and a small mast
                var rand = new System.Random(bd.name.GetHashCode());
                for (int i = 0; i < 2; i++)
                {
                    Vector3 vp = new Vector3(c.x + (i == 0 ? -1 : 1) * hx * 0.4f, top + 0.18f + 0.35f, c.z + ((float)rand.NextDouble() - 0.5f) * hz);
                    Part(g, "RoofVent", PrimitiveType.Cube, vp, new Vector3(1.0f, 0.7f, 0.8f), steel);
                    Part(g, "VentCap", PrimitiveType.Cube, vp + Vector3.up * 0.4f, new Vector3(1.15f, 0.08f, 0.95f), blackMat);
                    for (int k = 0; k < 4; k++)
                        Part(g, "VentLouver", PrimitiveType.Cube, vp + new Vector3(0, -0.2f + k * 0.12f, -0.41f), new Vector3(0.8f, 0.04f, 0.02f), blackMat);
                }
                Vector3 stack = new Vector3(c.x, top + 0.18f, c.z - hz * 0.4f);
                Tube(g, "Stack", stack, stack + Vector3.up * 1.6f, 0.17f, steel);
                Tube(g, "StackCap", stack + Vector3.up * 1.6f, stack + Vector3.up * 1.72f, 0.26f, blackMat);

                // Door: yellow frame and hazard header, canopy, caged lamp, apron
                Vector2 n2 = bd.doorWall;
                Vector3 n = new Vector3(n2.x, 0, n2.y);
                Vector3 across = Vector3.Cross(Vector3.up, n);
                var rot = Quaternion.LookRotation(n, Vector3.up);
                GameObject Oriented(string nm, Vector3 world, Vector3 localSize, Material m)
                {
                    var o = Part(g, nm, PrimitiveType.Cube, world, localSize, m);
                    o.transform.rotation = rot;
                    return o;
                }
                float dh = 2.6f;
                foreach (float s in new[] { -1f, 1f })
                    Oriented("DoorPost", doorPos + across * (s * 0.92f) + Vector3.up * (dh / 2f) + n * 0.1f, new Vector3(0.24f, dh, 0.55f), yellow);
                Oriented("DoorHeader", doorPos + Vector3.up * (dh + 0.12f) + n * 0.1f, new Vector3(2.1f, 0.26f, 0.55f), yellow);
                Oriented("Canopy", doorPos + Vector3.up * (dh + 0.55f) + n * 0.9f, new Vector3(3.0f, 0.1f, 1.7f), steel);
                foreach (float s in new[] { -1f, 1f })
                {
                    var brace = Bar(g, "CanopyBrace", doorPos + across * (s * 1.3f) + Vector3.up * (dh + 0.5f) + n * 0.15f, doorPos + across * (s * 1.3f) + Vector3.up * (dh - 0.1f) + n * 1.5f, 0.07f, steel);
                }
                Oriented("Apron", new Vector3(doorPos.x, gy - 0.1f, doorPos.z) + n * 1.4f, new Vector3(3.6f, 0.3f, 2.8f), dark);
                var lamp = Model("industrial_caged_sconce", "industrial_caged_sconce", g, doorPos + Vector3.up * (dh + 0.3f) + n * 0.5f, Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg, 0.4f, Anchor.Top, sconceMat);
                PointLight(g, "DoorLight", doorPos + Vector3.up * (dh - 0.2f) + n * 0.8f, new Color(1f, 0.82f, 0.55f), 2.5f, 9f);

                // Cabinet and conduit beside the door, name plate above it
                Vector3 side = doorPos + across * 1.7f + n * 0.4f;
                var cab = Model("power_box_01", "power_box_01", g, new Vector3(side.x, gy + 1.5f, side.z), Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg, 1.4f, Anchor.Center, powerMat);
                Tube(g, "Conduit", new Vector3(side.x, gy + 1.85f, side.z) , new Vector3(side.x, top + 0.2f, side.z) + n * 0.02f, 0.035f, blackMat);
                Vector3 sign = doorPos + Vector3.up * (dh + 0.95f) + n * 0.2f;
                Oriented("NamePlate", sign, new Vector3(2.6f, 0.34f, 0.04f), blackMat);
                Label(g, "NameText", bd.label, sign + n * 0.03f, n, 1.4f, new Color(1f, 0.82f, 0.25f), 2.5f);

                // A drain pipe down a back corner
                Vector3 corner = new Vector3(c.x - (n.x != 0 ? n.x * hx : -hx), gy, c.z - (n.z != 0 ? n.z * hz : -hz)) + new Vector3(n.x != 0 ? 0 : 0.0f, 0, 0);
                Vector3 pipeBase = new Vector3(c.x + (n.x != 0 ? -n.x * (hx + 0.12f) : hx - 0.6f), gy, c.z + (n.z != 0 ? -n.z * (hz + 0.12f) : hz + 0.12f));
                Tube(g, "DrainPipe", pipeBase, pipeBase + Vector3.up * (bd.height + 0.2f), 0.07f, pipeBlue);
            }
            return $"{buildings.Length} buildings, {ribs} pilasters";
        }

        /// <summary>Industrial generator set over the greybox generator box in the island generator bunker.</summary>
        static string DressGeneratorSet(Transform root)
        {
            var box = Find("Generator");
            if (box == null || box.GetComponent<Renderer>() == null) return "missing";
            var b = box.GetComponent<Renderer>().bounds;
            box.GetComponent<Renderer>().enabled = false;
            var exhaust = GameObject.Find("Generator_Exhaust");
            if (exhaust != null && exhaust.GetComponent<Renderer>() != null) exhaust.GetComponent<Renderer>().enabled = false;

            var olive = Triplanar("Locker_Olive", "metal_plate", new Color(1.3f, 1.7f, 1.0f), 1.5f, 0.35f, 0.45f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var steel = Steel("Plinth_Steel", new Color(0.5f, 0.53f, 0.57f));
            var gauge = Lit("Gen_Gauge", new Color(0.9f, 0.9f, 0.85f), 0f, 0.6f);
            var glow = Lit("Gen_Glow", new Color(0.05f, 0.2f, 0.05f), 0f, 0.7f, new Color(0.3f, 2.5f, 0.4f));
            var group = Group(root, "GeneratorSet");
            Vector3 c = b.center;
            float x0 = b.min.x, z0 = b.min.z, z1 = b.max.z;
            float w = b.size.x, d = b.size.z, bottom = b.min.y;
            // Skid and housing (front faces -Z, into the room)
            Part(group, "Skid", PrimitiveType.Cube, new Vector3(c.x, bottom + 0.1f, c.z), new Vector3(w + 0.2f, 0.2f, d + 0.2f), yellow);
            Part(group, "Housing", PrimitiveType.Cube, new Vector3(c.x - 0.2f, bottom + 0.2f + 0.55f, c.z), new Vector3(w * 0.72f, 1.1f, d - 0.1f), olive);
            Part(group, "Alternator", PrimitiveType.Cylinder, new Vector3(c.x + w * 0.34f, bottom + 0.2f + 0.5f, c.z), new Vector3(0.95f, 0.55f, 0.95f), steel, Quaternion.Euler(0, 0, 90));
            Part(group, "Radiator", PrimitiveType.Cube, new Vector3(c.x - w * 0.46f - 0.1f, bottom + 0.2f + 0.65f, c.z), new Vector3(0.18f, 1.2f, d - 0.15f), dark);
            for (int k = 0; k < 7; k++)
                Part(group, "Louver", PrimitiveType.Cube, new Vector3(c.x - 0.2f - w * 0.15f, bottom + 0.55f + k * 0.1f, z0 - 0.005f), new Vector3(w * 0.3f, 0.025f, 0.02f), dark);
            // Exhaust stack and fuel tank
            Tube(group, "ExhaustPipe", new Vector3(c.x + 0.7f, bottom + 1.3f, c.z + 0.2f), new Vector3(c.x + 0.7f, bottom + 2.8f, c.z + 0.2f), 0.09f, steel);
            Tube(group, "ExhaustCap", new Vector3(c.x + 0.7f, bottom + 2.8f, c.z + 0.2f), new Vector3(c.x + 0.7f, bottom + 2.92f, c.z + 0.2f), 0.14f, dark);
            Part(group, "FuelTank", PrimitiveType.Cylinder, new Vector3(c.x - 0.2f, bottom + 1.55f, c.z + 0.1f), new Vector3(0.55f, 0.55f, 0.55f), yellow, Quaternion.Euler(0, 0, 90));
            // Control panel on the front: gauges and a status display
            Part(group, "Panel", PrimitiveType.Cube, new Vector3(c.x + w * 0.2f, bottom + 0.95f, z0 - 0.02f), new Vector3(0.7f, 0.55f, 0.06f), dark);
            for (int k = 0; k < 3; k++)
            {
                var gg = Part(group, "Gauge", PrimitiveType.Cylinder, new Vector3(c.x + w * 0.2f - 0.22f + k * 0.22f, bottom + 1.05f, z0 - 0.06f), new Vector3(0.17f, 0.015f, 0.17f), gauge, Quaternion.Euler(90, 0, 0));
                Part(group, "Needle", PrimitiveType.Cube, new Vector3(c.x + w * 0.2f - 0.22f + k * 0.22f, bottom + 1.05f, z0 - 0.08f), new Vector3(0.012f, 0.07f, 0.005f), dark, Quaternion.Euler(0, 0, 25f * (k - 1)));
            }
            Part(group, "StatusLight", PrimitiveType.Cube, new Vector3(c.x + w * 0.2f, bottom + 0.82f, z0 - 0.06f), new Vector3(0.5f, 0.07f, 0.01f), glow, shadows: false);
            // Cable bundle into the wall
            Tube(group, "CableA", new Vector3(c.x + w * 0.45f, bottom + 0.4f, z0 + 0.1f), new Vector3(c.x + w * 0.45f + 0.8f, bottom + 0.1f, z0 - 0.8f), 0.05f, dark);
            return "generator set";
        }
    }
}
