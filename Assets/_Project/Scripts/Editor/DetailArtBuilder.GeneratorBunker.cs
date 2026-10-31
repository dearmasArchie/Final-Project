using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        /// <summary>
        /// Interior detail for the island generator bunker (interior about 7.9 x 5.9 m, generator on the north wall, power
        /// cabinet on the east wall, doorway in the west wall): ceiling beams with caged lamps and pipe runs, steel shelves
        /// with cans and crates, a workbench, jerrycans, oil stains and a hazard border around the generator, a fire
        /// extinguisher, a high-voltage warning plate, and cables from the generator to the power cabinet. Positions are
        /// relative to the floor center (x east, z north); the doorway, the pickups and the switch stay clear.
        /// </summary>
        static string DressGeneratorBunkerInterior(Transform root)
        {
            var bunker = GameObject.Find("GeneratorBunker");
            if (bunker == null) return "no generator bunker";
            var group = bunker.transform;
            var floor = group.Find("Floor");
            if (floor == null) return "no floor";
            var old = group.Find("BunkerProps");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var props = Group(group, "BunkerProps");
            Vector3 origin = new Vector3(floor.position.x, floor.GetComponent<Renderer>().bounds.max.y, floor.position.z);   // top of the floor slab
            Quaternion rot = group.rotation;
            props.position = origin;
            Vector3 P(float x, float y, float z) => origin + rot * new Vector3(x, y, z);
            float Yaw(float yaw) => rot.eulerAngles.y + yaw;
            var rng = new System.Random(321);

            var concrete = Triplanar("Bunker_Beam", "concrete_wall_008", new Color(1.0f, 1.0f, 0.97f), 3f, 0f, 0.35f);
            var steel = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var blue = Lit("Pipe_Blue", new Color(0.12f, 0.32f, 0.55f), 0.5f, 0.45f);
            var red = Lit("Pipe_Red", new Color(0.65f, 0.1f, 0.07f), 0.4f, 0.45f);
            var wood = Lit("Bench_Wood", new Color(0.42f, 0.28f, 0.14f), 0f, 0.3f);
            var redPaint = Lit("Toolbox_Red", new Color(0.7f, 0.08f, 0.06f), 0.5f, 0.5f);
            var paper = Lit("Paper_White", new Color(0.88f, 0.86f, 0.78f), 0f, 0.15f);
            var shelfMat = ModelMat("steel_frame_shelves_01", "steel_frame_shelves_01", 0.4f, 0.6f);
            var canMat = ModelMat("metal_jerrycan", "metal_jerrycan", 0.45f, 0.5f);
            var lampMat = ModelMat("caged_hanging_light", "caged_hanging_light", 0.5f, 0.6f, emissive: true);
            var oil = PaintMaterial("OilStain", new Color(0.02f, 0.02f, 0.02f, 0.6f), 2);
            int items = 0;
            const float H = 3.2f, hx = 3.95f, hz = 2.95f;

            // Ceiling: two beams across the room with a lamp under each, two pipe runs along the long walls
            foreach (float x in new[] { -1.5f, 1.5f })
            {
                Part(props, "Beam", PrimitiveType.Cube, P(x, H - 0.16f, 0), new Vector3(0.35f, 0.32f, 2 * hz), concrete);
                Part(props, "BeamFlange", PrimitiveType.Cube, P(x, H - 0.34f, 0), new Vector3(0.6f, 0.04f, 2 * hz), steel);
                Model("caged_hanging_light", "caged_hanging_light", props, P(x, H - 0.35f, 0), Yaw(0f), 0.7f, Anchor.Top, lampMat);
            }
            foreach (float side in new[] { -1f, 1f })
                for (int k = 0; k < 2; k++)
                {
                    float y = H - 0.55f - k * 0.26f;
                    Vector3 a = P(-hx + 0.3f, y, side * (hz - 0.22f)), b = P(hx - 0.3f, y, side * (hz - 0.22f));
                    Tube(props, "Pipe", a, b, 0.08f, k == 0 ? (side < 0 ? blue : steel) : red);
                    for (int c = 0; c <= 3; c++) Part(props, "Clamp", PrimitiveType.Cube, Vector3.Lerp(a, b, c / 3f), new Vector3(0.1f, 0.2f, 0.2f), steel);
                }

            // Shelves on the north wall either side of the generator (fronts face south)
            float shelfScale = 1.1f;
            float[] boards = { 0.14f, 0.64f, 1.14f, 1.66f };
            foreach (float x in new[] { -2.95f, 2.95f })
            {
                var shelf = Model("steel_frame_shelves_01", "steel_frame_shelves_01", props, P(x, 0, hz - 0.3f), Yaw(180f), shelfScale, Anchor.Bottom, shelfMat);
                AddBoundsCollider(shelf);
                foreach (float board in boards)
                    for (int slot = -1; slot <= 1; slot += 2)
                    {
                        if (rng.NextDouble() < 0.25) continue;
                        Vector3 pos = P(x + slot * 0.3f + ((float)rng.NextDouble() - 0.5f) * 0.1f, board * shelfScale + 0.02f, hz - 0.3f);
                        bool can = rng.NextDouble() < 0.6;
                        var item = can
                            ? Model("metal_jerrycan", "metal_jerrycan", props, pos, Yaw(180f + (float)rng.NextDouble() * 30f - 15f), 0.8f, Anchor.Bottom, canMat)
                            : Model("wooden_military_crate", "wooden_military_crate", props, pos, Yaw(180f + (float)rng.NextDouble() * 20f - 10f), 0.55f, Anchor.Bottom, null);
                        if (item != null) items++;
                    }
            }

            // Workbench on the south wall, east of the pickups
            var bench = Group(props, "Workbench");
            bench.SetPositionAndRotation(P(1.2f, 0, -hz + 0.5f), Quaternion.Euler(0, Yaw(0f), 0));
            Part(bench, "Top", PrimitiveType.Cube, new Vector3(0, 0.9f, 0), new Vector3(1.9f, 0.06f, 0.8f), wood);
            Part(bench, "TopEdge", PrimitiveType.Cube, new Vector3(0, 0.865f, 0), new Vector3(1.94f, 0.02f, 0.84f), steel);
            foreach (float lx in new[] { -0.9f, 0.9f })
                foreach (float lz in new[] { -0.35f, 0.35f })
                    Part(bench, "Leg", PrimitiveType.Cube, new Vector3(lx, 0.43f, lz), new Vector3(0.06f, 0.86f, 0.06f), steel);
            Part(bench, "LowerShelf", PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(1.8f, 0.04f, 0.7f), steel);
            var col = bench.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.45f, 0);
            col.size = new Vector3(1.94f, 0.92f, 0.84f);
            Part(bench, "ToolboxBody", PrimitiveType.Cube, new Vector3(0.5f, 0.99f, 0.0f), new Vector3(0.42f, 0.18f, 0.2f), redPaint);
            Part(bench, "ToolboxLid", PrimitiveType.Cube, new Vector3(0.5f, 1.095f, 0.0f), new Vector3(0.44f, 0.03f, 0.22f), dark);
            Part(bench, "Multimeter", PrimitiveType.Cube, new Vector3(-0.3f, 0.95f, -0.1f), new Vector3(0.1f, 0.04f, 0.18f), yellow);
            Part(bench, "Spool", PrimitiveType.Cylinder, new Vector3(-0.65f, 0.99f, 0.1f), new Vector3(0.22f, 0.06f, 0.22f), dark, Quaternion.Euler(90, 0, 0));
            for (int k = 0; k < 4; k++)
                Part(bench, "Paper", PrimitiveType.Cube, new Vector3(0.0f + k * 0.01f, 0.935f + k * 0.002f, 0.18f), new Vector3(0.3f, 0.002f, 0.22f), paper, Quaternion.Euler(0, k * 8f - 10f, 0));
            var benchCan = Model("metal_jerrycan", "metal_jerrycan", props, bench.TransformPoint(new Vector3(0.82f, 0.93f, -0.1f)), Yaw(-20f), 0.7f, Anchor.Bottom, canMat);
            if (benchCan != null) items++;

            // Jerrycans on the floor in the south-east corner and a crate under the doorway-side wall
            foreach (var (x, z, yaw) in new[] { (3.45f, -2.5f, 10f), (3.0f, -2.65f, -25f), (3.5f, -1.95f, 40f) })
            {
                var can = Model("metal_jerrycan", "metal_jerrycan", props, P(x, 0, z), Yaw(yaw), 0.9f, Anchor.Bottom, canMat);
                if (can != null) { AddBoundsCollider(can); items++; }
            }

            // Floor: oil stains in front of the generator, yellow-black border around it
            foreach (var (x, z, size, roll) in new[] { (-0.5f, 0.9f, 1.3f, 20f), (1.1f, 1.2f, 0.9f, -35f), (-1.6f, 1.4f, 0.8f, 70f) })
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "OilStain";
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(props, false);
                q.transform.position = P(x, 0.012f, z);
                q.transform.rotation = rot * Quaternion.Euler(90, roll, 0);
                q.transform.localScale = new Vector3(size, size, 1f);
                q.GetComponent<Renderer>().sharedMaterial = oil;
                q.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var (x, z, sx, sz) in new[] { (0f, 0.8f, 3.9f, 0.1f), (-1.9f, 1.8f, 0.1f, 2.1f), (1.9f, 1.8f, 0.1f, 2.1f) })
                Part(props, "HazardLine", PrimitiveType.Cube, P(x, 0.012f, z), new Vector3(sx, 0.01f, sz), yellow, Quaternion.Euler(0, Yaw(0f), 0), shadows: false);

            // Fire extinguisher beside the doorway, high-voltage plate by the power cabinet
            Vector3 fe = P(-hx + 0.12f, 1.0f, 1.9f);
            Part(props, "ExtinguisherBody", PrimitiveType.Cylinder, fe, new Vector3(0.17f, 0.3f, 0.17f), redPaint);
            Part(props, "ExtinguisherTop", PrimitiveType.Cylinder, fe + Vector3.up * 0.33f, new Vector3(0.08f, 0.04f, 0.08f), dark);
            Part(props, "ExtinguisherHose", PrimitiveType.Cube, fe + new Vector3(0, 0.28f, 0) + rot * new Vector3(0.08f, 0, 0), new Vector3(0.1f, 0.03f, 0.03f), dark);
            Part(props, "ExtinguisherBracket", PrimitiveType.Cube, fe + rot * new Vector3(-0.05f, 0.1f, 0), new Vector3(0.03f, 0.08f, 0.2f), steel);
            Vector3 plateAt = P(hx - 0.02f, 2.15f, -1.2f);
            var plate = Part(props, "WarningPlate", PrimitiveType.Cube, plateAt, new Vector3(0.02f, 0.3f, 0.5f), yellow);
            plate.transform.rotation = rot;
            Label(props, "WarningText", "DANGER\nHIGH VOLTAGE", plateAt + rot * new Vector3(-0.015f, 0, 0), rot * Vector3.left, 0.7f, new Color(0.05f, 0.05f, 0.05f), 0.6f);

            // Cable from the generator to the power cabinet, along the floor and up the wall
            Vector3 g0 = P(1.55f, 0.1f, 2.0f), g1 = P(2.9f, 0.04f, 1.0f), g2 = P(3.7f, 0.04f, -0.4f), g3 = P(3.8f, 0.9f, -1.0f);
            Tube(props, "CableA", g0, g1, 0.035f, dark);
            Tube(props, "CableB", g1, g2, 0.035f, dark);
            Tube(props, "CableC", g2, g3, 0.035f, dark);

            return $"generator bunker interior: beams, lamps, pipes, 2 shelves, workbench, {items} items";
        }
    }
}
