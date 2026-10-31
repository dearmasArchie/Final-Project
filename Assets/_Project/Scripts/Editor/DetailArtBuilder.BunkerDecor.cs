using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Underground dressing beyond the structure pass: restored Poly Haven air ducts and pipe kits, floor lanes and stencils,
    /// door signs, extinguishers, security cameras, cable runs, drums, crates and oil puddles in every room.
    /// Everything is visual (props that could block the player get a box collider and are skipped when the spot is taken).
    /// </summary>
    public static partial class DetailArtBuilder
    {
        // ------------------------------------------------------------------ model helpers

        /// <summary>One named part of a modular Poly Haven kit (duct section, door slats), non-uniformly scalable, centered at a world point.</summary>
        static GameObject ModelPart(string folder, string file, string partKey, Transform parent, Vector3 center, float yaw, Vector3 scale,
            Material main, bool exact = false, params (string key, Material mat)[] extra)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{folder}/{file}_2k.fbx");
            if (asset == null) { Debug.LogWarning($"[Signal Lost] Missing model {folder}/{file}"); return null; }
            var holder = new GameObject(partKey);
            holder.transform.SetParent(parent, false);
            holder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.transform.SetParent(holder.transform, false);
            string key = partKey.ToLowerInvariant();
            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                string n = r.name.ToLowerInvariant();
                bool keep = exact ? n == key : n.Contains(key);
                if (!keep) Object.DestroyImmediate(r.gameObject);
            }
            foreach (var c in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    string slotName = slots[i] != null ? slots[i].name.ToLowerInvariant() : "";
                    var chosen = main ?? slots[i];
                    foreach (var (k, mat) in extra) if (slotName.Contains(k)) chosen = mat;
                    slots[i] = chosen;
                }
                r.sharedMaterials = slots;
            }
            holder.transform.localScale = scale;
            var b = WorldBounds(holder);
            holder.transform.position += center - b.center;
            return holder;
        }

        /// <summary>Like PlaceChecked, but with extra material slots. Skipped when something solid is in the way.</summary>
        static bool DecPlace(string folder, Transform parent, Vector3 pos, float yaw, float scale, Anchor anchor, Material main,
            params (string key, Material mat)[] extra)
        {
            var go = Model(folder, folder, parent, pos, yaw, scale, anchor, main, extra);
            if (go == null) return false;
            var b = WorldBounds(go);
            Physics.SyncTransforms();
            if (Occupied(b, 0.05f)) { Object.DestroyImmediate(go); return false; }
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(b.center);
            var local = go.transform.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            Static(go);
            return true;
        }

        // ------------------------------------------------------------------ small props

        static void DecSign(Transform parent, string text, Vector3 pos, Vector3 facing, float size, float width, Color textColor, Color plate, Vector2 plateSize)
        {
            var mat = Lit("Sign_" + ColorKey(plate), plate, 0.2f, 0.4f);
            Part(parent, "SignPlate", PrimitiveType.Cube, pos - facing * 0.012f, new Vector3(plateSize.x, plateSize.y, 0.02f), mat,
                Quaternion.LookRotation(facing, Vector3.up), shadows: false);
            Label(parent, "SignText", text, pos + facing * 0.004f, facing, size, textColor, width);
        }

        static void DecExtinguisher(Transform parent, Vector3 floorPos, Vector3 facing)
        {
            var red = Lit("Extinguisher_Red", new Color(0.72f, 0.06f, 0.05f), 0.4f, 0.55f);
            var black = Lit("Switch_Grip", new Color(0.04f, 0.04f, 0.04f), 0f, 0.5f);
            var steel = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
            var g = Group(parent, "Extinguisher");
            Vector3 c = floorPos + Vector3.up * 0.95f + facing * 0.1f;
            Part(g, "Body", PrimitiveType.Cylinder, c, new Vector3(0.17f, 0.26f, 0.17f), red);
            Part(g, "Neck", PrimitiveType.Cylinder, c + Vector3.up * 0.3f, new Vector3(0.07f, 0.05f, 0.07f), steel);
            Part(g, "Handle", PrimitiveType.Cube, c + Vector3.up * 0.37f + facing * 0.03f, new Vector3(0.05f, 0.03f, 0.14f), black, Quaternion.LookRotation(facing, Vector3.up));
            Bar(g, "Hose", c + Vector3.up * 0.33f + facing * 0.04f, c + Vector3.up * 0.0f + facing * 0.1f, 0.025f, black);
            foreach (float y in new[] { -0.08f, 0.1f })
                Part(g, "Strap", PrimitiveType.Cylinder, c + Vector3.up * y, new Vector3(0.18f, 0.012f, 0.18f), black);
            Part(g, "Bracket", PrimitiveType.Cube, c - facing * 0.07f, new Vector3(0.1f, 0.5f, 0.02f), steel, Quaternion.LookRotation(facing, Vector3.up));
            DecSign(g, "FIRE", floorPos + Vector3.up * 1.75f, facing, 0.5f, 0.3f, Color.white, new Color(0.7f, 0.05f, 0.05f), new Vector2(0.28f, 0.14f));
        }

        static void DecCamera(Transform parent, Vector3 pos, Vector3 lookAt)
        {
            var body = Lit("Camera_Body", new Color(0.12f, 0.13f, 0.14f), 0.5f, 0.45f);
            var lens = Lit("Camera_Lens", new Color(0.02f, 0.03f, 0.05f), 0.8f, 0.9f);
            var led = Lit("Camera_Led", new Color(0.4f, 0f, 0f), 0f, 0.5f, new Color(4f, 0.1f, 0.05f));
            var steel = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
            var g = Group(parent, "SecurityCamera");
            var rot = Quaternion.LookRotation((lookAt - pos).normalized, Vector3.up);
            Part(g, "Mount", PrimitiveType.Cube, pos + Vector3.up * 0.12f, new Vector3(0.1f, 0.16f, 0.1f), steel);
            Part(g, "Body", PrimitiveType.Cube, pos, new Vector3(0.14f, 0.12f, 0.3f), body, rot);
            Part(g, "Hood", PrimitiveType.Cube, pos + rot * new Vector3(0, 0.075f, 0.04f), new Vector3(0.16f, 0.02f, 0.34f), body, rot);
            Part(g, "Lens", PrimitiveType.Cylinder, pos + rot * new Vector3(0, 0, 0.16f), new Vector3(0.1f, 0.03f, 0.1f), lens, rot * Quaternion.Euler(90, 0, 0));
            Part(g, "Led", PrimitiveType.Sphere, pos + rot * new Vector3(0.05f, 0.04f, 0.15f), Vector3.one * 0.018f, led, shadows: false);
        }

        /// <summary>Straight air duct run (Poly Haven kit, 1.8 m sections) with hangers up to the ceiling.</summary>
        static int DecDuctRun(Transform parent, Vector3 start, Vector3 end, float scale, float ceilingY, Material a, Material b)
        {
            Vector3 d = end - start;
            bool alongX = Mathf.Abs(d.x) > Mathf.Abs(d.z);
            float section = 1.8f * scale;
            int n = Mathf.Max(1, Mathf.RoundToInt(d.magnitude / section));
            Vector3 dir = d.normalized;
            var g = Group(parent, "DuctRun");
            for (int i = 0; i < n; i++)
            {
                Vector3 c = start + dir * (section * (i + 0.5f));
                ModelPart("modular_airduct_rectangular_01", "modular_airduct_rectangular_01", "tripple_01", g, c, alongX ? 90f : 0f, Vector3.one * scale, a,
                    false, ("parts02", b));
                if (i % 2 == 0)
                {
                    float top = c.y + 0.255f * scale;
                    var steel = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
                    foreach (float s in new[] { -0.3f, 0.3f })
                    {
                        Vector3 off = alongX ? new Vector3(0, 0, s * scale) : new Vector3(s * scale, 0, 0);
                        Bar(g, "Hanger", new Vector3(c.x, top, c.z) + off, new Vector3(c.x, ceilingY, c.z) + off, 0.025f, steel);
                    }
                }
            }
            return n;
        }

        /// <summary>Flat painted line on the floor between two points.</summary>
        static void DecFloorLine(Transform parent, Vector3 a, Vector3 b, float width, Material mat)
        {
            a.y = FloorYAt(a) + 0.008f;
            b.y = FloorYAt(b) + 0.008f;
            Bar(parent, "FloorLine", a, b, width, mat).transform.localScale = new Vector3(width, 0.006f, (b - a).magnitude);
        }

        static void DecFloorText(Transform parent, string text, Vector3 pos, float yaw, float size, Color color, float width)
        {
            var tmp = new GameObject("FloorText").AddComponent<TextMeshPro>();
            tmp.transform.SetParent(parent, false);
            tmp.transform.SetPositionAndRotation(new Vector3(pos.x, FloorYAt(pos) + 0.01f, pos.z), Quaternion.Euler(90f, yaw, 0f));
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = 6f;
            tmp.rectTransform.sizeDelta = new Vector2(width, size * 0.3f);
        }

        /// <summary>Glossy dark puddle (flattened sphere, no collider).</summary>
        static void DecPuddle(Transform parent, Vector3 pos, Vector2 size)
        {
            var mat = Lit("Oil_Puddle", new Color(0.015f, 0.02f, 0.022f), 0.2f, 0.97f);
            Part(parent, "Puddle", PrimitiveType.Sphere, new Vector3(pos.x, FloorYAt(pos) + 0.002f, pos.z), new Vector3(size.x, 0.012f, size.y), mat, shadows: false);
        }

        /// <summary>Steel drum with ribs and a coloured band (chemical drums; deliberately not red, so they never read as explosive barrels).</summary>
        static void DecDrum(Transform parent, Vector3 floorPos, Color band)
        {
            var body = Lit("Drum_Steel", new Color(0.18f, 0.24f, 0.3f), 0.6f, 0.5f);
            var bandMat = Lit("Drum_Band_" + ColorKey(band), band, 0.3f, 0.5f);
            var rib = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
            var g = Group(parent, "Drum");
            float y = FloorYAt(floorPos);
            var cyl = Part(g, "Body", PrimitiveType.Cylinder, new Vector3(floorPos.x, y + 0.45f, floorPos.z), new Vector3(0.58f, 0.45f, 0.58f), body, collider: true);
            foreach (float h in new[] { 0.2f, 0.7f })
                Part(g, "Rib", PrimitiveType.Cylinder, new Vector3(floorPos.x, y + h, floorPos.z), new Vector3(0.61f, 0.025f, 0.61f), rib);
            Part(g, "Band", PrimitiveType.Cylinder, new Vector3(floorPos.x, y + 0.45f, floorPos.z), new Vector3(0.59f, 0.06f, 0.59f), bandMat);
            Part(g, "Lid", PrimitiveType.Cylinder, new Vector3(floorPos.x, y + 0.9f, floorPos.z), new Vector3(0.5f, 0.012f, 0.5f), rib);
        }

        /// <summary>Cable tray on a wall: steel channel with a bundle of black and coloured cables.</summary>
        static void DecCableTray(Transform parent, Vector3 a, Vector3 b, Vector3 outward)
        {
            var steel = Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f));
            var black = Lit("Compound_Cable", new Color(0.02f, 0.02f, 0.02f), 0f, 0.3f);
            var colours = new[] { Lit("Cable_Red", new Color(0.55f, 0.05f, 0.04f), 0f, 0.35f), Lit("Cable_Blue", new Color(0.08f, 0.2f, 0.5f), 0f, 0.35f) };
            var g = Group(parent, "CableTray");
            Vector3 d = b - a;
            Vector3 mid = (a + b) * 0.5f + outward * 0.06f;
            var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            Part(g, "Tray", PrimitiveType.Cube, mid, new Vector3(0.3f, 0.02f, d.magnitude), steel, Quaternion.LookRotation(d.normalized, outward));
            Part(g, "Lip", PrimitiveType.Cube, mid + outward * 0.04f + Vector3.Cross(d.normalized, outward) * 0.15f, new Vector3(0.015f, 0.08f, d.magnitude), steel, rot);
            for (int i = 0; i < 4; i++)
            {
                float o = -0.09f + i * 0.06f;
                Vector3 side = Vector3.Cross(d.normalized, outward).normalized * o;
                Bar(g, "Cable", a + outward * 0.1f + side, b + outward * 0.1f + side, 0.032f, i == 1 ? colours[0] : i == 2 ? colours[1] : black);
            }
            int clamps = Mathf.Max(2, Mathf.RoundToInt(d.magnitude / 2.2f));
            for (int i = 0; i <= clamps; i++)
                Part(g, "Strap", PrimitiveType.Cube, Vector3.Lerp(a, b, i / (float)clamps) + outward * 0.1f, new Vector3(0.28f, 0.04f, 0.04f), steel, rot);
        }

        // ------------------------------------------------------------------ rooms

        static string DressBunkerDecor(Transform root)
        {
            var group = Group(root, "Decor");
            var yellowPaint = Lit("Floor_Paint_Yellow", new Color(0.82f, 0.6f, 0.04f), 0f, 0.25f);
            var whitePaint = Lit("Floor_Paint_White", new Color(0.75f, 0.75f, 0.7f), 0f, 0.25f);
            var ductA = ModelMat("modular_airduct_rectangular_01", "modular_airduct_rectangular_01_parts01", 0.4f, 0.6f);
            var ductB = ModelMat("modular_airduct_rectangular_01", "modular_airduct_rectangular_01_parts02", 0.4f, 0.6f);
            var pipeA = ModelMat("modular_industrial_pipes_01", "modular_industrial_pipes_01_group01", 0.45f, 0.6f);
            var pipeB = ModelMat("modular_industrial_pipes_01", "modular_industrial_pipes_01_group02", 0.45f, 0.6f);
            var crateMat = (Material)null;
            var canMat = ModelMat("metal_jerrycan", "metal_jerrycan", 0.45f, 0.5f);
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellowPlate = new Color(0.85f, 0.62f, 0.05f);
            var darkPlate = new Color(0.07f, 0.08f, 0.09f);
            var redPlate = new Color(0.55f, 0.05f, 0.04f);
            int ducts = 0, props = 0, signs = 0, cams = 0;
            void Prop(bool ok) { if (ok) props++; }

            // ---------------- hub (x -8..8, z 12..28)
            ducts += DecDuctRun(group, new Vector3(0, 3.22f, 13.4f), new Vector3(0, 3.22f, 26.8f), 1.4f, 4f, ductA, ductB);
            Prop(DecPlace("modular_industrial_pipes_01", group, new Vector3(7.75f, 0, 26.2f), 90f, 1.3f, Anchor.Bottom, pipeA, ("group02", pipeB)));
            Prop(DecPlace("modular_industrial_pipes_01", group, new Vector3(-7.75f, 0, 13.8f), 90f, 1.3f, Anchor.Bottom, pipeA, ("group02", pipeB)));
            // Cargo corner (north-east) and a stack by the junction box wall
            Prop(DecPlace("wooden_military_crate", group, new Vector3(6.2f, 0, 26.4f), 12f, 1.0f, Anchor.Bottom, crateMat));
            Prop(DecPlace("wooden_military_crate", group, new Vector3(6.3f, 0.55f, 26.45f), -8f, 0.9f, Anchor.Bottom, crateMat));
            Prop(DecPlace("wooden_military_crate", group, new Vector3(5.2f, 0, 27.0f), 30f, 0.9f, Anchor.Bottom, crateMat));
            Prop(DecPlace("metal_jerrycan", group, new Vector3(4.5f, 0, 27.2f), 20f, 1.2f, Anchor.Bottom, canMat));
            DecDrum(group, new Vector3(-6.7f, 0, 26.8f), new Color(0.88f, 0.66f, 0.06f)); props++;
            DecDrum(group, new Vector3(-6.0f, 0, 27.3f), new Color(0.15f, 0.4f, 0.65f)); props++;
            Prop(DecPlace("wooden_military_crate", group, new Vector3(-7.0f, 0, 14.9f), 0f, 1.0f, Anchor.Bottom, crateMat));
            Prop(DecPlace("metal_jerrycan", group, new Vector3(-6.2f, 0, 13.6f), -15f, 1.2f, Anchor.Bottom, canMat));
            DecCableTray(group, new Vector3(-7.85f, 3.0f, 12.6f), new Vector3(-7.85f, 3.0f, 17.7f), Vector3.right);
            DecCableTray(group, new Vector3(-7.85f, 3.0f, 22.3f), new Vector3(-7.85f, 3.0f, 27.4f), Vector3.right);
            DecCableTray(group, new Vector3(7.85f, 3.0f, 12.6f), new Vector3(7.85f, 3.0f, 17.7f), Vector3.left);
            DecCableTray(group, new Vector3(7.85f, 3.0f, 22.3f), new Vector3(7.85f, 3.0f, 27.4f), Vector3.left);
            // Floor lanes: a ring around the hub centre, with walkways to the two wing doors and the elevator
            for (int i = 0; i < 28; i++)
            {
                float a0 = i * Mathf.PI * 2f / 28f, a1 = (i + 0.5f) * Mathf.PI * 2f / 28f;
                DecFloorLine(group, new Vector3(Mathf.Cos(a0) * 3.2f, 0, 20f + Mathf.Sin(a0) * 3.2f), new Vector3(Mathf.Cos(a1) * 3.2f, 0, 20f + Mathf.Sin(a1) * 3.2f), 0.12f, yellowPaint);
            }
            foreach (float zz in new[] { 18.4f, 21.6f })
            {
                DecFloorLine(group, new Vector3(-7.4f, 0, zz), new Vector3(-3.6f, 0, zz), 0.1f, whitePaint);
                DecFloorLine(group, new Vector3(7.4f, 0, zz), new Vector3(3.6f, 0, zz), 0.1f, whitePaint);
            }
            foreach (float xx in new[] { -1.6f, 1.6f })
                DecFloorLine(group, new Vector3(xx, 0, 12.6f), new Vector3(xx, 0, 16.5f), 0.1f, whitePaint);
            DecFloorText(group, "WING A", new Vector3(-5.6f, 0, 20f), -90f, 1.6f, new Color(0.82f, 0.6f, 0.04f), 3f);
            DecFloorText(group, "WING B", new Vector3(5.6f, 0, 20f), 90f, 1.6f, new Color(0.82f, 0.6f, 0.04f), 3f);
            DecFloorText(group, "HUB 01", new Vector3(0, 0, 20f), 0f, 1.5f, new Color(0.75f, 0.75f, 0.7f), 3f);
            DecFloorText(group, "ELEVATOR", new Vector3(0, 0, 14.6f), 0f, 1.3f, new Color(0.75f, 0.75f, 0.7f), 4f);
            DecPuddle(group, new Vector3(-2.2f, 0, 24.5f), new Vector2(1.6f, 1.0f));
            DecPuddle(group, new Vector3(3.4f, 0, 15.3f), new Vector2(1.1f, 0.8f));
            DecPuddle(group, new Vector3(-4.9f, 0, 18.8f), new Vector2(0.7f, 0.5f));
            // Signs over the doors and the hub entrance
            DecSign(group, "WING A\nLASER CORRIDOR", new Vector3(-7.9f, 3.72f, 20f), Vector3.right, 1.0f, 2.4f, Color.black, yellowPlate, new Vector2(2.0f, 0.5f)); signs++;
            DecSign(group, "WING B\nGENERATOR FLOOR", new Vector3(7.9f, 3.72f, 20f), Vector3.left, 1.0f, 2.4f, Color.black, yellowPlate, new Vector2(2.2f, 0.5f)); signs++;
            DecSign(group, "FREIGHT ELEVATOR", new Vector3(0, 3.72f, 12.1f), Vector3.forward, 1.0f, 3f, Color.black, yellowPlate, new Vector2(2.4f, 0.3f)); signs++;
            DecSign(group, "HAZARD: ELECTRICAL", new Vector3(7.9f, 2.2f, 16.9f), Vector3.left, 0.6f, 1.3f, Color.white, redPlate, new Vector2(0.9f, 0.22f)); signs++;
            DecExtinguisher(group, new Vector3(-7.88f, 0, 16.4f), Vector3.right);
            DecExtinguisher(group, new Vector3(7.88f, 0, 23.6f), Vector3.left);
            DecCamera(group, new Vector3(-7.4f, 3.45f, 27.4f), new Vector3(0, 1.4f, 20f)); cams++;
            DecCamera(group, new Vector3(7.4f, 3.45f, 27.4f), new Vector3(0, 1.4f, 20f)); cams++;
            DecCamera(group, new Vector3(7.4f, 3.45f, 12.6f), new Vector3(0, 1.4f, 20f)); cams++;
            // Notice board on the east wall
            var board = Group(group, "NoticeBoard");
            Part(board, "Frame", PrimitiveType.Cube, new Vector3(7.96f, 1.75f, 18.0f - 4.0f + 0.0f), new Vector3(0.04f, 0.9f, 1.4f), Steel("Bunker_Steel", new Color(0.45f, 0.47f, 0.5f)));
            Part(board, "Cork", PrimitiveType.Cube, new Vector3(7.935f, 1.75f, 14.0f), new Vector3(0.02f, 0.8f, 1.3f), Lit("Cork", new Color(0.35f, 0.24f, 0.14f), 0f, 0.15f));
            var paper = Lit("Paper_White", new Color(0.88f, 0.86f, 0.78f), 0f, 0.15f);
            var rng = new System.Random(7);
            for (int i = 0; i < 6; i++)
                Part(board, "Paper", PrimitiveType.Cube, new Vector3(7.918f, 1.75f + ((float)rng.NextDouble() - 0.5f) * 0.45f, 14.0f + (i - 2.5f) * 0.2f),
                    new Vector3(0.006f, 0.22f, 0.16f), paper, Quaternion.Euler((float)(rng.NextDouble() - 0.5) * 16f, 0, 0), shadows: false);

            // ---------------- elevator corridor (z 4..12)
            ducts += DecDuctRun(group, new Vector3(0, 3.22f, 4.4f), new Vector3(0, 3.22f, 11.6f), 1.2f, 4f, ductA, ductB);
            DecSign(group, "HUB\nWING A / WING B", new Vector3(0, 3.72f, 4.1f), Vector3.back, 0.9f, 3f, Color.black, yellowPlate, new Vector2(2.4f, 0.5f)); signs++;
            DecExtinguisher(group, new Vector3(-1.88f, 0, 9.6f), Vector3.right);
            DecFloorLine(group, new Vector3(-1.3f, 0, 4.5f), new Vector3(-1.3f, 0, 11.5f), 0.1f, yellowPaint);
            DecFloorLine(group, new Vector3(1.3f, 0, 4.5f), new Vector3(1.3f, 0, 11.5f), 0.1f, yellowPaint);
            Prop(DecPlace("modular_industrial_pipes_01", group, new Vector3(1.85f, 0, 7f), 90f, 1.2f, Anchor.Bottom, pipeA, ("group02", pipeB)));

            // ---------------- Wing A laser corridor (x -30..-8, z 18..22)
            ducts += DecDuctRun(group, new Vector3(-9.6f, 3.3f, 20f), new Vector3(-28.4f, 3.3f, 20f), 1.15f, 4f, ductA, ductB);
            DecCableTray(group, new Vector3(-9.5f, 0.4f, 18.12f), new Vector3(-29.5f, 0.4f, 18.12f), Vector3.forward);
            DecCableTray(group, new Vector3(-9.5f, 0.4f, 21.88f), new Vector3(-29.5f, 0.4f, 21.88f), Vector3.back);
            DecSign(group, "DANGER\nLASER TRIPWIRES", new Vector3(-9.2f, 2.2f, 18.1f), Vector3.forward, 0.8f, 2.0f, Color.white, redPlate, new Vector2(1.5f, 0.4f)); signs++;
            DecSign(group, "WARNING\nALARM LINKED TO SECURITY", new Vector3(-9.2f, 2.2f, 21.9f), Vector3.back, 0.6f, 2.4f, Color.black, yellowPlate, new Vector2(1.9f, 0.4f)); signs++;
            DecExtinguisher(group, new Vector3(-12.6f, 0, 18.12f), Vector3.forward);
            DecCamera(group, new Vector3(-9.0f, 3.5f, 21.7f), new Vector3(-26f, 1.2f, 20f)); cams++;
            DecCamera(group, new Vector3(-29.0f, 3.5f, 21.7f), new Vector3(-12f, 1.2f, 20f)); cams++;
            DecPuddle(group, new Vector3(-12f, 0, 19.2f), new Vector2(1.4f, 0.8f));

            // ---------------- breaker room (x -42..-30, z 14..26)
            DecSign(group, "BREAKER ROOM\nAUTHORISED STAFF ONLY", new Vector3(-30.12f, 3.72f, 20f), Vector3.left, 0.8f, 3.2f, Color.black, yellowPlate, new Vector2(2.6f, 0.5f)); signs++;
            Prop(DecPlace("modular_industrial_pipes_01", group, new Vector3(-41.7f, 0, 16f), 90f, 1.3f, Anchor.Bottom, pipeA, ("group02", pipeB)));
            Prop(DecPlace("modular_industrial_pipes_01", group, new Vector3(-41.7f, 0, 24f), 90f, 1.3f, Anchor.Bottom, pipeA, ("group02", pipeB)));
            DecCableTray(group, new Vector3(-41.88f, 3.0f, 15f), new Vector3(-41.88f, 3.0f, 25f), Vector3.right);
            ducts += DecDuctRun(group, new Vector3(-31f, 3.25f, 15.4f), new Vector3(-41f, 3.25f, 15.4f), 1.2f, 4f, ductA, ductB);
            DecExtinguisher(group, new Vector3(-30.12f, 0, 15.2f), Vector3.right);
            Prop(DecPlace("wooden_military_crate", group, new Vector3(-40.8f, 0, 21.6f), 8f, 1.0f, Anchor.Bottom, crateMat));
            Prop(DecPlace("metal_jerrycan", group, new Vector3(-40.6f, 0, 19.9f), 30f, 1.2f, Anchor.Bottom, canMat));
            DecCamera(group, new Vector3(-41.4f, 3.45f, 14.6f), new Vector3(-36f, 1.2f, 20f)); cams++;
            DecFloorLine(group, new Vector3(-33f, 0, 17.6f), new Vector3(-33f, 0, 22.4f), 0.1f, yellowPaint);

            // ---------------- Wing B flooded generator (x 16..36, z 12..28)
            ducts += DecDuctRun(group, new Vector3(17.2f, 3.25f, 14f), new Vector3(34.8f, 3.25f, 14f), 1.3f, 4.0f, ductA, ductB);
            ducts += DecDuctRun(group, new Vector3(17.2f, 3.25f, 26f), new Vector3(34.8f, 3.25f, 26f), 1.3f, 4.0f, ductA, ductB);
            Prop(DecPlace("modular_industrial_pipes_01", group, new Vector3(22f, 0, 12.4f), 0f, 1.4f, Anchor.Bottom, pipeA, ("group02", pipeB)));
            Prop(DecPlace("modular_industrial_pipes_01", group, new Vector3(32.5f, 0, 12.4f), 0f, 1.4f, Anchor.Bottom, pipeA, ("group02", pipeB)));
            DecSign(group, "FLOODWAY\nELECTRICAL HAZARD", new Vector3(16.12f, 3.72f, 20f), Vector3.right, 0.9f, 3.2f, Color.white, redPlate, new Vector2(2.6f, 0.5f)); signs++;
            DecSign(group, "HIGH VOLTAGE\nKEEP OUT OF WATER", new Vector3(26f, 3.1f, 12.12f), Vector3.forward, 0.9f, 3.2f, Color.black, yellowPlate, new Vector2(2.8f, 0.5f)); signs++;
            DecSign(group, "QUARANTINE\nSEALED", new Vector3(35.9f, 3.72f, 20f), Vector3.left, 0.9f, 2.4f, Color.white, redPlate, new Vector2(1.9f, 0.5f)); signs++;
            DecCableTray(group, new Vector3(16.12f, 3.0f, 12.6f), new Vector3(16.12f, 3.0f, 17.7f), Vector3.right);
            DecCableTray(group, new Vector3(16.12f, 3.0f, 22.3f), new Vector3(16.12f, 3.0f, 27.4f), Vector3.right);
            DecCamera(group, new Vector3(17.0f, 3.5f, 27.2f), new Vector3(26f, 0.5f, 20f)); cams++;
            DecCamera(group, new Vector3(35.0f, 3.5f, 12.8f), new Vector3(26f, 0.5f, 20f)); cams++;
            DecExtinguisher(group, new Vector3(16.12f, 0.0f, 23.6f), Vector3.right);

            // ---------------- quarantine room (x 36..52, z 12..28)
            ducts += DecDuctRun(group, new Vector3(37.4f, 3.25f, 15f), new Vector3(50.6f, 3.25f, 15f), 1.25f, 4f, ductA, ductB);
            ducts += DecDuctRun(group, new Vector3(37.4f, 3.25f, 25f), new Vector3(50.6f, 3.25f, 25f), 1.25f, 4f, ductA, ductB);
            Prop(DecPlace("modular_industrial_pipes_01", group, new Vector3(51.7f, 0, 14f), 90f, 1.3f, Anchor.Bottom, pipeA, ("group02", pipeB)));
            Prop(DecPlace("modular_industrial_pipes_01", group, new Vector3(51.7f, 0, 26.2f), 90f, 1.3f, Anchor.Bottom, pipeA, ("group02", pipeB)));
            DecSign(group, "QUARANTINE ZONE", new Vector3(36.12f, 3.72f, 20f), Vector3.right, 1.0f, 3f, Color.white, redPlate, new Vector2(2.4f, 0.3f)); signs++;
            DecSign(group, "BIOHAZARD\nSAMPLES ONLY", new Vector3(44f, 2.4f, 27.88f), Vector3.back, 0.9f, 2.8f, Color.black, yellowPlate, new Vector2(2.2f, 0.5f)); signs++;
            DecFloorLine(group, new Vector3(37.2f, 0, 14.5f), new Vector3(37.2f, 0, 25.5f), 0.12f, yellowPaint);
            DecFloorLine(group, new Vector3(50.6f, 0, 14.5f), new Vector3(50.6f, 0, 25.5f), 0.12f, yellowPaint);
            DecDrum(group, new Vector3(50.5f, 0, 13.4f), new Color(0.88f, 0.66f, 0.06f)); props++;
            DecDrum(group, new Vector3(49.8f, 0, 13.0f), new Color(0.88f, 0.66f, 0.06f)); props++;
            Prop(DecPlace("wooden_military_crate", group, new Vector3(37.4f, 0, 13.4f), 10f, 1.0f, Anchor.Bottom, crateMat));
            Prop(DecPlace("wooden_military_crate", group, new Vector3(38.6f, 0, 13.0f), -5f, 0.9f, Anchor.Bottom, crateMat));
            Prop(DecPlace("metal_jerrycan", group, new Vector3(37.4f, 0, 26.8f), 40f, 1.2f, Anchor.Bottom, canMat));
            DecExtinguisher(group, new Vector3(36.12f, 0, 24f), Vector3.right);
            DecCamera(group, new Vector3(51.2f, 3.5f, 27.2f), new Vector3(44f, 1.2f, 20f)); cams++;
            DecCamera(group, new Vector3(37.0f, 3.5f, 12.8f), new Vector3(46f, 1.2f, 20f)); cams++;
            DecPuddle(group, new Vector3(41f, 0, 23.2f), new Vector2(1.5f, 1.0f));
            DecPuddle(group, new Vector3(47f, 0, 16.4f), new Vector2(1.0f, 0.7f));

            return $"duct sections={ducts}, props={props}, signs={signs}, cameras={cams}";
        }
    }
}
