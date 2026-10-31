using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        /// <summary>
        /// Furniture for the maintenance bunker: two steel shelves on the east wall stocked with cans and crates, a
        /// workbench with tools, a desk against the west wall with a field radio, a stool and a crate stack. Positions
        /// are relative to the bunker floor (x east, z north) and keep the doorway, the light switch and every wall clue
        /// (code digits, decoys, wall message) clear.
        /// </summary>
        static string DressCodeBunkerFurniture(Transform bunker, Vector3 origin, Quaternion rot)
        {
            var old = bunker.Find("BunkerProps");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var props = Group(bunker, "BunkerProps");
            props.position = origin;
            Vector3 P(float x, float y, float z) => origin + rot * new Vector3(x, y, z);
            float Yaw(float yaw) => rot.eulerAngles.y + yaw;
            var rng = new System.Random(99);

            var shelfMat = ModelMat("steel_frame_shelves_01", "steel_frame_shelves_01", 0.4f, 0.6f);
            var canMat = ModelMat("metal_jerrycan", "metal_jerrycan", 0.45f, 0.5f);
            var crateMat = ModelMat("wooden_crate_01", "wooden_crate_01", 0.3f, 0f);
            var radioMat = ModelMat("vintage_radio_transceiver", "vintage_radio_transceiver", 0.4f, 0.4f);
            var wood = Lit("Bench_Wood", new Color(0.42f, 0.28f, 0.14f), 0f, 0.3f);
            var steel = Steel("Bench_Steel", new Color(0.4f, 0.42f, 0.45f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var red = Lit("Toolbox_Red", new Color(0.7f, 0.08f, 0.06f), 0.5f, 0.5f);
            var paper = Lit("Paper_White", new Color(0.88f, 0.86f, 0.78f), 0f, 0.15f);
            var cloth = Lit("Stool_Seat", new Color(0.25f, 0.27f, 0.2f), 0f, 0.2f);
            int items = 0;

            // Shelves on the east wall, fronts facing west; board heights follow the model (0.14, 0.64, 1.14, 1.66)
            float shelfScale = 1.1f;
            float[] boards = { 0.14f, 0.64f, 1.14f, 1.66f };
            foreach (float z in new[] { 2.3f, -2.2f })
            {
                var shelf = Model("steel_frame_shelves_01", "steel_frame_shelves_01", props, P(3.72f, 0, z), Yaw(-90f), shelfScale, Anchor.Bottom, shelfMat);
                AddBoundsCollider(shelf);
                for (int level = 0; level < boards.Length; level++)
                    for (int slot = -1; slot <= 1; slot += 2)
                    {
                        if (rng.NextDouble() < 0.25) continue;
                        float y = boards[level] * shelfScale + 0.02f;
                        Vector3 pos = P(3.72f, y, z + slot * 0.3f + ((float)rng.NextDouble() - 0.5f) * 0.1f);
                        bool can = rng.NextDouble() < 0.55;
                        var item = can
                            ? Model("metal_jerrycan", "metal_jerrycan", props, pos, Yaw(-90f + (float)rng.NextDouble() * 30f - 15f), 0.8f, Anchor.Bottom, canMat)
                            : Model("wooden_military_crate", "wooden_military_crate", props, pos, Yaw(-90f + (float)rng.NextDouble() * 20f - 10f), 0.55f, Anchor.Bottom, null);
                        if (item != null) items++;
                    }
            }

            // Workbench parallel to the north wall: plank top, steel legs and under-shelf, tools on it
            var bench = Group(props, "Workbench");
            bench.SetPositionAndRotation(P(-1.2f, 0, 1.7f), Quaternion.Euler(0, Yaw(0f), 0));
            Part(bench, "Top", PrimitiveType.Cube, new Vector3(0, 0.9f, 0), new Vector3(1.9f, 0.06f, 0.8f), wood);
            Part(bench, "TopEdge", PrimitiveType.Cube, new Vector3(0, 0.865f, 0), new Vector3(1.94f, 0.02f, 0.84f), steel);
            foreach (float lx in new[] { -0.9f, 0.9f })
                foreach (float lz in new[] { -0.35f, 0.35f })
                    Part(bench, "Leg", PrimitiveType.Cube, new Vector3(lx, 0.43f, lz), new Vector3(0.06f, 0.86f, 0.06f), steel);
            Part(bench, "LowerShelf", PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(1.8f, 0.04f, 0.7f), steel);
            var benchCol = bench.gameObject.AddComponent<BoxCollider>();
            benchCol.center = new Vector3(0, 0.45f, 0);
            benchCol.size = new Vector3(1.94f, 0.92f, 0.84f);
            Part(bench, "ToolboxBody", PrimitiveType.Cube, new Vector3(-0.6f, 0.99f, 0.05f), new Vector3(0.42f, 0.18f, 0.2f), red);
            Part(bench, "ToolboxLid", PrimitiveType.Cube, new Vector3(-0.6f, 1.095f, 0.05f), new Vector3(0.44f, 0.03f, 0.22f), dark);
            Part(bench, "ToolboxHandle", PrimitiveType.Cube, new Vector3(-0.6f, 1.13f, 0.05f), new Vector3(0.2f, 0.025f, 0.03f), steel);
            Part(bench, "Vise", PrimitiveType.Cube, new Vector3(0.78f, 0.97f, 0.28f), new Vector3(0.14f, 0.12f, 0.2f), dark);
            Part(bench, "ViseJaw", PrimitiveType.Cube, new Vector3(0.78f, 1.05f, 0.2f), new Vector3(0.12f, 0.06f, 0.04f), steel);
            for (int k = 0; k < 5; k++)
                Part(bench, "Paper", PrimitiveType.Cube, new Vector3(0.1f + k * 0.01f, 0.935f + k * 0.002f, 0.1f), new Vector3(0.3f, 0.002f, 0.22f), paper, Quaternion.Euler(0, k * 7f - 12f, 0));
            Part(bench, "Mug", PrimitiveType.Cylinder, new Vector3(0.35f, 0.975f, -0.2f), new Vector3(0.08f, 0.045f, 0.08f), steel);
            var can0 = Model("metal_jerrycan", "metal_jerrycan", props, bench.TransformPoint(new Vector3(-0.2f, 0.93f, -0.15f)), Yaw(25f), 0.7f, Anchor.Bottom, canMat);
            if (can0 != null) items++;

            // Desk against the west wall with the field radio
            var desk = Group(props, "Desk");
            desk.SetPositionAndRotation(P(-3.6f, 0, 0.7f), Quaternion.Euler(0, Yaw(90f), 0));
            Part(desk, "Top", PrimitiveType.Cube, new Vector3(0, 0.76f, 0), new Vector3(1.5f, 0.05f, 0.7f), wood);
            foreach (float lx in new[] { -0.7f, 0.7f })
                foreach (float lz in new[] { -0.3f, 0.3f })
                    Part(desk, "Leg", PrimitiveType.Cube, new Vector3(lx, 0.37f, lz), new Vector3(0.05f, 0.74f, 0.05f), steel);
            Part(desk, "Drawers", PrimitiveType.Cube, new Vector3(0.45f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0.6f), steel);
            for (int k = 0; k < 2; k++)
                Part(desk, "DrawerHandle", PrimitiveType.Cube, new Vector3(0.45f, 0.4f + k * 0.2f, -0.31f), new Vector3(0.14f, 0.025f, 0.02f), dark);
            var deskCol = desk.gameObject.AddComponent<BoxCollider>();
            deskCol.center = new Vector3(0, 0.4f, 0);
            deskCol.size = new Vector3(1.5f, 0.8f, 0.7f);
            for (int k = 0; k < 4; k++)
                Part(desk, "Paper", PrimitiveType.Cube, new Vector3(-0.35f + k * 0.01f, 0.787f + k * 0.002f, 0.1f), new Vector3(0.22f, 0.002f, 0.3f), paper, Quaternion.Euler(0, k * 9f - 10f, 0));
            var radio = Model("vintage_radio_transceiver", "vintage_radio_transceiver", props, desk.TransformPoint(new Vector3(0.1f, 0.785f, -0.1f)), Yaw(90f), 0.9f, Anchor.Bottom, radioMat);
            if (radio != null) items++;

            // Stool by the bench, and a crate stack in the south-east corner
            var stool = Group(props, "Stool");
            stool.position = P(-0.5f, 0, 0.75f);
            Part(stool, "Seat", PrimitiveType.Cylinder, new Vector3(0, 0.46f, 0), new Vector3(0.36f, 0.03f, 0.36f), cloth);
            foreach (float a in new[] { 0f, 120f, 240f })
                Bar(stool, "Leg", stool.position + Quaternion.Euler(0, a, 0) * new Vector3(0.14f, 0.44f, 0), stool.position + Quaternion.Euler(0, a, 0) * new Vector3(0.2f, 0f, 0), 0.035f, steel);
            var stackA = Model("wooden_military_crate", "wooden_military_crate", props, P(3.4f, 0, -2.5f), Yaw(8f), 0.9f, Anchor.Bottom, null);
            if (stackA != null)
            {
                AddBoundsCollider(stackA);
                var b = WorldBounds(stackA);
                var top = Model("wooden_crate_01", "wooden_crate_01", props, new Vector3(b.center.x, b.max.y, b.center.z), Yaw(-14f), 0.6f, Anchor.Bottom, crateMat);
                if (top != null) items++;
                items++;
            }
            // The plain brown supply box becomes a few real crates (its collider stays)
            var supply = bunker.Find("SupplyShelf");
            if (supply != null && supply.GetComponent<Renderer>() != null)
            {
                supply.GetComponent<Renderer>().enabled = false;
                var sb2 = supply.GetComponent<Renderer>().bounds;
                var a = Model("wooden_military_crate", "wooden_military_crate", props, new Vector3(sb2.center.x, sb2.min.y, sb2.center.z - 0.3f), Yaw(90f), 1.25f, Anchor.Bottom, null);
                var b2 = Model("wooden_military_crate", "wooden_military_crate", props, new Vector3(sb2.center.x, sb2.min.y, sb2.center.z + 0.3f), Yaw(84f), 1.25f, Anchor.Bottom, null);
                if (a != null)
                {
                    var ab = WorldBounds(a);
                    Model("wooden_crate_01", "wooden_crate_01", props, new Vector3(ab.center.x, ab.max.y, ab.center.z), Yaw(20f), 0.7f, Anchor.Bottom, crateMat);
                    items += 3;
                }
            }
            return $"furniture: 2 shelves, workbench, desk, stool, {items} items";
        }

        /// <summary>Box collider that matches a placed model's rendered bounds.</summary>
        static void AddBoundsCollider(GameObject go)
        {
            var b = WorldBounds(go);
            var col = go.AddComponent<BoxCollider>();
            col.center = go.transform.InverseTransformPoint(b.center);
            var s = go.transform.InverseTransformVector(b.size);
            col.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }
    }
}
