using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        static TMP_Text Label(Transform parent, string name, string text, Vector3 pos, Vector3 facing, float size, Color color, float width)
        {
            var tmp = new GameObject(name).AddComponent<TextMeshPro>();
            tmp.transform.SetParent(parent, false);
            tmp.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-facing, Vector3.up));
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(width, size * 0.25f);
            return tmp;
        }

        /// <summary>
        /// Steel cage freight elevator on the existing platform: yellow corner posts and roof frame, lattice sides and
        /// back with kick plates, a folded scissor gate, hazard strip at the open front, roof lamp, hoist motor and
        /// ropes, guide rails behind, and a real call panel. The platform keeps its collider (and the player's spawn on
        /// it); the three closed sides get thin colliders so the cage feels solid. front points from the platform toward
        /// the room's door; panelFacing is the wall normal at the call panel.
        /// </summary>
        static string BuildElevator(Transform root, string sceneTag, Vector3 front, Vector3 panelFacing, float ceilingHeight)
        {
            var platform = Find("ElevatorPlatform");
            var panel = Find(sceneTag);
            if (platform == null || panel == null) return "missing platform/panel";

            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var steel = Steel("Lift_Steel", new Color(0.55f, 0.57f, 0.6f));
            var darkSteel = Steel("Lift_DarkSteel", new Color(0.28f, 0.3f, 0.33f));
            var floorMat = Steel("Lift_Floor", new Color(0.4f, 0.42f, 0.45f));
            var hazard = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Greybox/Greybox_Hazard.mat") ?? yellow;

            // The platform keeps its collider; only its look changes
            platform.GetComponent<Renderer>().sharedMaterial = floorMat;
            var pb = platform.GetComponent<Renderer>().bounds;
            var car = new GameObject("FreightElevatorCar").transform;
            car.SetParent(root, false);
            car.SetPositionAndRotation(new Vector3(pb.center.x, pb.max.y, pb.center.z), Quaternion.LookRotation(front, Vector3.up));
            const float H = 2.5f, half = 1.44f;

            // Posts and roof frame
            foreach (float x in new[] { -half, half })
                foreach (float z in new[] { -half, half })
                    Part(car, "Post", PrimitiveType.Cube, new Vector3(x, H / 2f, z), new Vector3(0.12f, H, 0.12f), yellow);
            foreach (float y in new[] { H, 0.05f })
            {
                Part(car, "Beam_Front", PrimitiveType.Cube, new Vector3(0, y, half), new Vector3(2 * half, 0.1f, 0.1f), yellow);
                Part(car, "Beam_Back", PrimitiveType.Cube, new Vector3(0, y, -half), new Vector3(2 * half, 0.1f, 0.1f), yellow);
                Part(car, "Beam_Left", PrimitiveType.Cube, new Vector3(-half, y, 0), new Vector3(0.1f, 0.1f, 2 * half), yellow);
                Part(car, "Beam_Right", PrimitiveType.Cube, new Vector3(half, y, 0), new Vector3(0.1f, 0.1f, 2 * half), yellow);
            }
            Part(car, "Roof", PrimitiveType.Cube, new Vector3(0, H + 0.08f, 0), new Vector3(2 * half + 0.1f, 0.06f, 2 * half + 0.1f), darkSteel);

            // Lattice sides and back: vertical bars, three rails, solid kick plate
            for (int side = 0; side < 3; side++)
            {
                Vector3 normal = side == 0 ? Vector3.left : side == 1 ? Vector3.right : Vector3.back;
                Vector3 along = side == 2 ? Vector3.right : Vector3.forward;
                Vector3 center = normal * half;
                for (float t = -half + 0.14f; t < half - 0.05f; t += 0.2f)
                    Part(car, "Bar", PrimitiveType.Cube, center + along * t + Vector3.up * (H / 2f + 0.15f), new Vector3(0.02f, H - 0.3f, 0.02f), steel);
                foreach (float y in new[] { 0.7f, 1.3f, 1.9f })
                    Part(car, "Rail", PrimitiveType.Cube, center + Vector3.up * y, side == 2 ? new Vector3(2 * half, 0.03f, 0.03f) : new Vector3(0.03f, 0.03f, 2 * half), steel);
                Part(car, "KickPlate", PrimitiveType.Cube, center + Vector3.up * 0.3f, side == 2 ? new Vector3(2 * half, 0.5f, 0.03f) : new Vector3(0.03f, 0.5f, 2 * half), darkSteel);

                var wall = new GameObject("CageCollider").AddComponent<BoxCollider>();
                wall.transform.SetParent(car, false);
                wall.transform.localPosition = center + Vector3.up * (H / 2f);
                wall.size = side == 2 ? new Vector3(2 * half, H, 0.1f) : new Vector3(0.1f, H, 2 * half);
            }

            // Folded scissor gate on the right of the open front, hazard strip along the threshold
            for (int k = 0; k < 6; k++)
            {
                float y0 = 0.15f + k * 0.38f;
                Vector3 a = new Vector3(0.95f, y0, half + 0.03f), b = new Vector3(half, y0 + 0.38f, half + 0.03f);
                Bar(car, "Gate", car.TransformPoint(a), car.TransformPoint(b), 0.025f, steel);
                Bar(car, "Gate", car.TransformPoint(new Vector3(a.x, y0 + 0.38f, a.z)), car.TransformPoint(new Vector3(b.x, y0, b.z)), 0.025f, steel);
            }
            Part(car, "ThresholdHazard", PrimitiveType.Cube, new Vector3(0, 0.005f, half - 0.12f), new Vector3(2 * half - 0.2f, 0.015f, 0.2f), hazard);
            var sign = Part(car, "SignPlate", PrimitiveType.Cube, new Vector3(0, H - 0.2f, half + 0.01f), new Vector3(1.6f, 0.3f, 0.02f), darkSteel);
            Label(sign.transform, "SignText", "FREIGHT ELEVATOR 02", car.TransformPoint(new Vector3(0, H - 0.2f, half + 0.035f)), car.forward, 1.1f, new Color(1f, 0.8f, 0.2f), 1.5f);

            // Roof lamp (caged lamp model plus a real light), hoist motor and ropes up to the ceiling
            var lampMat = ModelMat("caged_hanging_light", "caged_hanging_light", 0.5f, 0.6f, emissive: true);
            Model("caged_hanging_light", "caged_hanging_light", car, car.TransformPoint(new Vector3(0, H - 0.02f, 0)), car.eulerAngles.y, 0.6f, Anchor.Top, lampMat);
            PointLight(car, "CarLight", car.TransformPoint(new Vector3(0, H - 0.4f, 0)), new Color(1f, 0.85f, 0.6f), 1.6f, 5f);
            float motorY = ceilingHeight - 0.3f;
            Part(car, "HoistMotor", PrimitiveType.Cube, new Vector3(0, motorY, -0.3f), new Vector3(1.1f, 0.5f, 0.8f), yellow);
            Part(car, "HoistDrum", PrimitiveType.Cylinder, new Vector3(0, motorY, 0.3f), new Vector3(0.35f, 0.5f, 0.35f), darkSteel, Quaternion.Euler(0, 0, 90f));
            foreach (float x in new[] { -0.35f, 0.35f })
                Part(car, "Rope", PrimitiveType.Cylinder, new Vector3(x, (H + motorY) / 2f, 0.3f), new Vector3(0.03f, (motorY - H) / 2f, 0.03f), darkSteel);

            // Guide rails (I-beams) behind the car, up to the ceiling
            foreach (float x in new[] { -0.9f, 0.9f })
            {
                float zb = -half - 0.2f;
                Part(car, "GuideWeb", PrimitiveType.Cube, new Vector3(x, ceilingHeight / 2f, zb), new Vector3(0.03f, ceilingHeight, 0.16f), darkSteel);
                Part(car, "GuideFlange", PrimitiveType.Cube, new Vector3(x, ceilingHeight / 2f, zb - 0.08f), new Vector3(0.14f, ceilingHeight, 0.03f), darkSteel);
                Part(car, "GuideShoe", PrimitiveType.Cube, new Vector3(x, 1.0f, zb + 0.1f), new Vector3(0.1f, 0.25f, 0.1f), yellow);
            }

            // Call panel: dark housing with a yellow frame, UP/DOWN buttons, a status window and a label
            panel.GetComponent<Renderer>().enabled = false;
            Vector3 p = panel.transform.position;
            Vector3 f = panelFacing;
            var body = Group(root, "CallPanel");
            Vector3 face = p + f * 0.115f;
            var housing = Part(body, "Housing", PrimitiveType.Cube, p + f * 0.04f, Vector3.one, darkSteel);
            housing.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            housing.transform.localScale = new Vector3(0.5f, 0.7f, 0.1f);
            var frame = Part(body, "Frame", PrimitiveType.Cube, p + f * 0.03f, Vector3.one, yellow);
            frame.transform.rotation = housing.transform.rotation;
            frame.transform.localScale = new Vector3(0.56f, 0.76f, 0.08f);
            Vector3 right = Vector3.Cross(Vector3.up, f);
            var upBtn = Lit("Lift_BtnUp", new Color(0.1f, 0.3f, 0.12f), 0f, 0.5f, new Color(0.3f, 2.4f, 0.5f));
            var downBtn = Lit("Lift_BtnDown", new Color(0.35f, 0.08f, 0.06f), 0f, 0.5f, new Color(2.6f, 0.4f, 0.3f));
            for (int i = 0; i < 2; i++)
            {
                var btn = Part(body, i == 0 ? "ButtonUp" : "ButtonDown", PrimitiveType.Cylinder, face + Vector3.up * (0.08f - i * 0.16f) + f * 0.01f, Vector3.one, i == 0 ? upBtn : downBtn);
                btn.transform.rotation = Quaternion.LookRotation(f, Vector3.up) * Quaternion.Euler(90f, 0, 0);
                btn.transform.localScale = new Vector3(0.1f, 0.015f, 0.1f);
            }
            var window = Part(body, "Window", PrimitiveType.Cube, face + Vector3.up * 0.26f, Vector3.one, Lit("Lift_Display", new Color(0.02f, 0.05f, 0.05f), 0f, 0.9f, Cyan * 0.6f));
            window.transform.rotation = housing.transform.rotation;
            window.transform.localScale = new Vector3(0.36f, 0.1f, 0.01f);
            Label(body, "PanelText", "LIFT", face + Vector3.up * 0.26f + f * 0.012f, f, 1.1f, Cyan, 0.4f);
            Label(body, "PanelHint", "UP  /  DOWN", face + Vector3.down * 0.31f + f * 0.006f, f, 0.8f, new Color(0.9f, 0.9f, 0.85f), 0.5f);

            return $"car at ({car.position.x:F1},{car.position.y:F1},{car.position.z:F1})";
        }
    }
}
