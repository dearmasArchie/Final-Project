using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Underground interactables: the sliding doors get Poly Haven roller-shutter slats, the keypads and the maintenance
    /// terminal become real wall units, and the Wing B junction levers and pump get a lever and handwheel. The greybox
    /// shapes keep their colliders and scripts and only lose their renderer.
    /// </summary>
    public static partial class DetailArtBuilder
    {
        static string DressBunkerInteractables(Transform root)
        {
            var group = Group(root, "Interactables");
            int doors = DressDoors(group, out int pads);
            int terminals = DressMaintenanceTerminal(group);
            int levers = DressJunctionLevers(group);
            return $"doors={doors}, keypads={pads}, terminals={terminals}, junction levers={levers}";
        }

        // ------------------------------------------------------------------ doors and keypads

        static int DressDoors(Transform group, out int keypads)
        {
            keypads = 0;
            var shutter = ModelMat("rollershutter_door", "rollershutter_door", 0.5f, 0.65f);
            shutter.SetColor("_BaseColor", new Color(0.62f, 0.66f, 0.7f));
            int count = 0;
            foreach (var name in new[] { "Door_WingA", "Door_WingB", "Door_BreakerRoom", "Door_Quarantine" })
            {
                var door = GameObject.Find(name);
                if (door == null) continue;
                var panel = door.transform.Find("Panel");
                if (panel == null) continue;
                HideRenderer(panel.gameObject);

                var old = panel.Find("ShutterVisual");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var holder = new GameObject("ShutterVisual").transform;
                holder.SetParent(panel, false);
                Vector3 ps = panel.lossyScale;
                holder.localScale = new Vector3(1f / ps.x, 1f / ps.y, 1f / ps.z); // undo the cube's stretch, so tiles keep true proportions

                bool alongZ = ps.z > ps.x;
                float width = alongZ ? ps.z : ps.x, depth = Mathf.Max(alongZ ? ps.x : ps.z, 0.12f);
                const int tiles = 3;
                float tileW = width / tiles;
                for (int i = 0; i < tiles; i++)
                {
                    float offset = (i - (tiles - 1) / 2f) * tileW;
                    Vector3 center = panel.position + (alongZ ? new Vector3(0, 0, offset) : new Vector3(offset, 0, 0));
                    var tile = ModelPart("rollershutter_door", "rollershutter_door", "rollershutter_door", holder, center, alongZ ? 90f : 0f,
                        new Vector3(tileW / 1.08f, ps.y / 2.4f, depth / 0.3f), shutter, exact: true);
                    if (tile != null) tile.name = "ShutterTile";
                }

                // Hazard stripe along the bottom edge and a yellow lock plate at handle height, so the door reads as a door
                count++;

                var pads = new List<Transform>();
                foreach (Transform child in door.transform) if (child.name == "Keypad") pads.Add(child);
                foreach (var pad in pads)
                {
                    Vector3 s = pad.lossyScale;
                    Vector3 axis = s.x < s.z ? Vector3.right : Vector3.forward;
                    float side = Mathf.Sign(Vector3.Dot(pad.position - door.transform.position, axis));
                    HideRenderer(pad.gameObject);
                    BuildKeypadVisual(group, $"{name}_Keypad", pad.position, axis * side, Mathf.Min(s.x, s.z), door.GetComponent<Door>());
                    keypads++;
                }
            }

            var breaker = GameObject.Find("BreakerRoomKeypad");
            if (breaker != null)
            {
                HideRenderer(breaker);
                Vector3 s = breaker.transform.lossyScale;
                Vector3 axis = s.x < s.z ? Vector3.right : Vector3.forward;
                float side = Mathf.Sign(Vector3.Dot(new Vector3(breaker.transform.position.x, 0, 20f) - new Vector3(0, 0, breaker.transform.position.z), axis));
                if (axis == Vector3.forward) side = Mathf.Sign(20f - breaker.transform.position.z);
                var breakerDoor = GameObject.Find("Door_BreakerRoom");
                BuildKeypadVisual(group, "BreakerRoomKeypad_Visual", breaker.transform.position, axis * side, Mathf.Min(s.x, s.z),
                    breakerDoor != null ? breakerDoor.GetComponent<Door>() : null);
                keypads++;
            }
            return count;
        }

        /// <summary>Wall keypad: steel housing, dark face plate, amber display, 3x4 button caps with digits and a status lamp.</summary>
        static void BuildKeypadVisual(Transform parent, string name, Vector3 center, Vector3 facing, float thickness, Door door = null)
        {
            var housing = Steel("Keypad_Housing", new Color(0.3f, 0.32f, 0.35f));
            var face = Lit("Keypad_Face", new Color(0.05f, 0.055f, 0.06f), 0.6f, 0.55f);
            var cap = Lit("Keypad_Cap", new Color(0.62f, 0.63f, 0.6f), 0.1f, 0.4f);
            var screen = Lit("Keypad_Screen", new Color(0.08f, 0.04f, 0.0f), 0f, 0.9f, new Color(1f, 0.55f, 0.08f) * 1.3f);
            var lamp = Lit("Keypad_Lamp", new Color(0.4f, 0.2f, 0f), 0f, 0.5f, new Color(3f, 1.2f, 0f));

            var mount = Group(parent, name);
            mount.SetPositionAndRotation(center - facing * (thickness / 2f), Quaternion.LookRotation(facing, Vector3.up));
            Part(mount, "Housing", PrimitiveType.Cube, new Vector3(0, 0, 0.035f), new Vector3(0.25f, 0.37f, 0.07f), housing);
            Part(mount, "FacePlate", PrimitiveType.Cube, new Vector3(0, 0, 0.073f), new Vector3(0.22f, 0.33f, 0.012f), face);
            Part(mount, "Display", PrimitiveType.Cube, new Vector3(0, 0.115f, 0.08f), new Vector3(0.16f, 0.055f, 0.006f), screen);
            var lampPart = Part(mount, "Lamp", PrimitiveType.Sphere, new Vector3(0.09f, 0.152f, 0.082f), Vector3.one * 0.016f, lamp, shadows: false);
            if (door != null)
            {
                // Live lock state: the display says LOCKED / UNLOCKED and the lamp follows (red / green)
                var status = Label(mount, "Status", "LOCKED", mount.TransformPoint(new Vector3(0, 0.115f, 0.0865f)), facing, 0.3f, new Color(1f, 0.12f, 0.08f), 0.15f);
                var status_lamp = lampPart.AddComponent<LockStatusLamp>();
                var so = new SerializedObject(status_lamp);
                so.FindProperty("door").objectReferenceValue = door;
                so.FindProperty("lamp").objectReferenceValue = lampPart.GetComponent<Renderer>();
                so.FindProperty("label").objectReferenceValue = status;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            string digits = "123456789*0#";
            for (int i = 0; i < 12; i++)
            {
                float x = (i % 3 - 1) * 0.065f, y = 0.05f - (i / 3) * 0.05f;
                Part(mount, "Key", PrimitiveType.Cube, new Vector3(x, y, 0.082f), new Vector3(0.05f, 0.035f, 0.012f), cap, shadows: false);
                var t = Label(mount, "Digit", digits[i].ToString(), mount.TransformPoint(new Vector3(x, y, 0.0885f)), facing, 0.32f, new Color(0.05f, 0.05f, 0.05f), 0.05f);
                t.fontStyle = FontStyles.Bold;
            }
        }

        // ------------------------------------------------------------------ maintenance terminal

        static int DressMaintenanceTerminal(Transform group)
        {
            var terminal = GameObject.Find("MaintenanceTerminal");
            if (terminal == null) return 0;
            HideRenderer(terminal);
            Vector3 s = terminal.transform.lossyScale;
            Vector3 axis = s.x < s.z ? Vector3.right : Vector3.forward;
            Vector3 facing = axis * (axis == Vector3.forward ? Mathf.Sign(20f - terminal.transform.position.z) : Mathf.Sign(-terminal.transform.position.x));
            float thickness = Mathf.Min(s.x, s.z);

            var steel = Steel("Terminal_Steel", new Color(0.36f, 0.4f, 0.43f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var screen = Lit("Terminal_Screen", new Color(0.02f, 0.07f, 0.08f), 0f, 0.9f, Cyan * 1.2f);
            var keys = Lit("Terminal_Keys", new Color(0.55f, 0.57f, 0.55f), 0.1f, 0.4f);
            var cable = Lit("Compound_Cable", new Color(0.02f, 0.02f, 0.02f), 0f, 0.3f);
            var green = Lit("Console_Led", new Color(0.1f, 0.5f, 0.1f), 0f, 0.5f, new Color(0.4f, 3f, 0.5f));
            var amber = Lit("Terminal_Amber", new Color(0.4f, 0.2f, 0f), 0f, 0.5f, new Color(3f, 1.2f, 0f));

            var mount = Group(group, "MaintenanceTerminal_Visual");
            mount.SetPositionAndRotation(terminal.transform.position - facing * (thickness / 2f), Quaternion.LookRotation(facing, Vector3.up));
            Part(mount, "Housing", PrimitiveType.Cube, new Vector3(0, 0, 0.09f), new Vector3(0.86f, 0.64f, 0.18f), steel);
            Part(mount, "Hood", PrimitiveType.Cube, new Vector3(0, 0.34f, 0.1f), new Vector3(0.9f, 0.04f, 0.22f), dark);
            Part(mount, "Bezel", PrimitiveType.Cube, new Vector3(0, 0.04f, 0.185f), new Vector3(0.72f, 0.48f, 0.02f), dark);
            Part(mount, "Screen", PrimitiveType.Cube, new Vector3(0, 0.04f, 0.197f), new Vector3(0.64f, 0.4f, 0.006f), screen);
            var log = Label(mount, "LogText", "MAINTENANCE LOG\n> BREAKER SEQUENCE\n> LASER GATES: ARMED",
                mount.TransformPoint(new Vector3(0, 0.04f, 0.204f)), facing, 0.5f, Cyan, 0.6f);
            log.fontStyle = FontStyles.Normal;
            Part(mount, "Shelf", PrimitiveType.Cube, new Vector3(0, -0.34f, 0.2f), new Vector3(0.74f, 0.035f, 0.24f), dark);
            for (int row = 0; row < 3; row++)
                Part(mount, "KeyRow", PrimitiveType.Cube, new Vector3(0, -0.318f, 0.14f + row * 0.065f), new Vector3(0.64f, 0.012f, 0.045f), keys, shadows: false);
            Part(mount, "LedGreen", PrimitiveType.Sphere, new Vector3(-0.3f, -0.235f, 0.196f), Vector3.one * 0.02f, green, shadows: false);
            Part(mount, "LedAmber", PrimitiveType.Sphere, new Vector3(-0.255f, -0.235f, 0.196f), Vector3.one * 0.02f, amber, shadows: false);
            Part(mount, "Caution", PrimitiveType.Cube, new Vector3(0.3f, -0.235f, 0.193f), new Vector3(0.14f, 0.04f, 0.004f), yellow, shadows: false);
            // Cable from the side of the housing to the floor
            Vector3 a = mount.TransformPoint(new Vector3(0.43f, -0.2f, 0.09f));
            Bar(mount, "CableA", a, a + facing * 0.12f + Vector3.down * 0.15f, 0.03f, cable);
            Bar(mount, "CableB", a + facing * 0.12f + Vector3.down * 0.15f, new Vector3(a.x, FloorYAt(a) + 0.02f, a.z) + facing * 0.3f, 0.03f, cable);
            var title = Label(mount, "Title", "MAINTENANCE", mount.TransformPoint(new Vector3(0, 0.46f, 0.01f)), facing, 0.6f, new Color(0.9f, 0.7f, 0.1f), 0.8f);
            title.characterSpacing = 8f;
            return 1;
        }

        static float FloorYAt(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, p.y + 0.3f, p.z), Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return 0f;
        }

        // ------------------------------------------------------------------ Wing B junction levers and pump

        static int DressJunctionLevers(Transform group)
        {
            var copper = Lit("Switch_Copper", new Color(0.8f, 0.45f, 0.25f), 1f, 0.6f);
            var black = Lit("Switch_Grip", new Color(0.04f, 0.04f, 0.04f), 0f, 0.5f);
            var plateMat = Lit("Junction_PlateLit", new Color(0.62f, 0.64f, 0.68f), 0.1f, 0.4f);
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            int count = 0;
            foreach (var n in new[] { "A", "B", "C" })
            {
                var box = GameObject.Find($"JunctionLever_{n}");
                if (box == null) continue;
                HideRenderer(box);
                var wing = box.transform.parent;

                // The handle pivot moves out from under the stretched box, so the arm swings without shearing
                var pivot = box.transform.Find("HandlePivot") ?? wing.Find($"HandlePivot_{n}");
                if (pivot == null) continue;
                pivot.name = $"HandlePivot_{n}";
                pivot.SetParent(wing, true);
                pivot.localScale = Vector3.one;
                for (int i = pivot.childCount - 1; i >= 0; i--) Object.DestroyImmediate(pivot.GetChild(i).gameObject);

                Vector3 facing = Vector3.back;
                Vector3 p = box.transform.position;
                var mount = Group(group, $"JunctionLever_{n}_Visual");
                // Base plate sits on the wall, at the back face of the old box
                mount.SetPositionAndRotation(p - facing * 0.075f, Quaternion.LookRotation(facing, Vector3.up));
                Part(mount, "Plate", PrimitiveType.Cube, new Vector3(0, 0, 0.015f), new Vector3(0.36f, 0.5f, 0.03f), plateMat);
                Part(mount, "Slot", PrimitiveType.Cube, new Vector3(0, 0, 0.032f), new Vector3(0.05f, 0.4f, 0.01f), dark);
                foreach (float y in new[] { -0.2f, 0.2f })
                    Part(mount, "Stop", PrimitiveType.Cube, new Vector3(0, y, 0.04f), new Vector3(0.09f, 0.035f, 0.03f), copper);
                foreach (var sx in new[] { -1f, 1f })
                    foreach (var sy in new[] { -1f, 1f })
                        Part(mount, "Screw", PrimitiveType.Cylinder, new Vector3(sx * 0.15f, sy * 0.21f, 0.034f), new Vector3(0.025f, 0.006f, 0.025f), copper, Quaternion.Euler(90, 0, 0), shadows: false);
                Part(mount, "WarnStripe", PrimitiveType.Cube, new Vector3(0, -0.235f, 0.034f), new Vector3(0.34f, 0.025f, 0.004f), yellow, shadows: false);

                // Lever: arm points out toward the player; the pivot's rotation lifts or lowers it (see PipeLever.SetDown)
                pivot.position = p + facing * 0.005f;
                pivot.rotation = Quaternion.identity;
                Part(mount, "Post", PrimitiveType.Cylinder, new Vector3(0, 0, 0.055f), new Vector3(0.04f, 0.025f, 0.04f), copper, Quaternion.Euler(90, 0, 0), shadows: false);
                Part(pivot, "Hub", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.08f, 0.02f, 0.08f), copper, Quaternion.Euler(0, 0, 90), shadows: false);
                Part(pivot, "Arm", PrimitiveType.Cube, new Vector3(0, 0, -0.17f), new Vector3(0.035f, 0.035f, 0.34f), copper);
                Part(pivot, "Grip", PrimitiveType.Cylinder, new Vector3(0, 0, -0.4f), new Vector3(0.065f, 0.07f, 0.065f), black, Quaternion.Euler(90, 0, 0));
                Part(pivot, "GripCap", PrimitiveType.Sphere, new Vector3(0, 0, -0.47f), Vector3.one * 0.07f, black);
                count++;
            }

            var pump = GameObject.Find("PumpHandle");
            if (pump != null)
            {
                HideRenderer(pump);
                var red = Lit("Pump_Red", new Color(0.75f, 0.08f, 0.05f), 0.5f, 0.45f, new Color(0.5f, 0.03f, 0f));
                var steel = Steel("Terminal_Steel", new Color(0.36f, 0.4f, 0.43f));
                Vector3 c = pump.transform.position + new Vector3(0, 0.1f, -0.04f);
                var wheel = Group(group, "PumpWheel_Visual");
                wheel.SetPositionAndRotation(c, Quaternion.identity);
                Part(wheel, "Stub", PrimitiveType.Cylinder, new Vector3(0, 0, 0.12f), new Vector3(0.1f, 0.14f, 0.1f), steel, Quaternion.Euler(90, 0, 0));
                Part(wheel, "Flange", PrimitiveType.Cylinder, new Vector3(0, 0, 0.02f), new Vector3(0.2f, 0.015f, 0.2f), steel, Quaternion.Euler(90, 0, 0));
                Part(wheel, "Hub", PrimitiveType.Cylinder, new Vector3(0, 0, -0.04f), new Vector3(0.08f, 0.03f, 0.08f), red, Quaternion.Euler(90, 0, 0));
                for (int i = 0; i < 16; i++)
                {
                    float a = i * Mathf.PI * 2f / 16f;
                    Part(wheel, "Rim", PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 0.2f, Mathf.Sin(a) * 0.2f, -0.04f), new Vector3(0.09f, 0.035f, 0.035f), red,
                        Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f));
                }
                for (int i = 0; i < 4; i++)
                    Part(wheel, "Spoke", PrimitiveType.Cube, new Vector3(0, 0, -0.04f), new Vector3(0.4f, 0.03f, 0.03f), red, Quaternion.Euler(0, 0, i * 45f));
            }
            return count;
        }
    }
}
