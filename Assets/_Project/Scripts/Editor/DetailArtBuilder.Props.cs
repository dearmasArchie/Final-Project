using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        /// <summary>
        /// Military ammo locker over a greybox box (renderer hidden, collider and scripts stay): olive steel body, two
        /// doors with louvers and handle bars, a stencil plate, hazard top band and feet. The front faces toward the point.
        /// </summary>
        static void DressLocker(Transform root, Transform locker, Vector3 toward)
        {
            var r = locker.GetComponent<Renderer>();
            if (r == null) return;
            var b = r.bounds;
            r.enabled = false;
            bool thinX = b.size.x < b.size.z;
            Vector3 f = thinX ? new Vector3(Mathf.Sign(toward.x - b.center.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(toward.z - b.center.z));
            Vector3 right = Vector3.Cross(Vector3.up, f);
            float width = thinX ? b.size.z : b.size.x, depth = thinX ? b.size.x : b.size.z, height = b.size.y;
            Vector3 c = b.center;

            var olive = Triplanar("Locker_Olive", "metal_plate", new Color(1.3f, 1.7f, 1.0f), 1.5f, 0.35f, 0.45f);
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var plate = Lit("Locker_Plate", new Color(0.85f, 0.82f, 0.7f), 0.1f, 0.4f);
            var group = Group(root, "AmmoLockerModel");
            var rot = Quaternion.LookRotation(f, Vector3.up);

            GameObject Box(string n, Vector3 offset, Vector3 size, Material m)
            {
                var go = Part(group, n, PrimitiveType.Cube, c + right * offset.x + Vector3.up * offset.y + f * offset.z, size, m);
                go.transform.rotation = rot;
                return go;
            }
            Box("Body", new Vector3(0, -0.04f, 0), new Vector3(width - 0.04f, height - 0.14f, depth - 0.04f), olive);
            Box("Feet", new Vector3(0, -height / 2f + 0.05f, 0), new Vector3(width, 0.1f, depth), dark);
            Box("TopBand", new Vector3(0, height / 2f - 0.05f, 0), new Vector3(width, 0.1f, depth), yellow);
            float faceZ = depth / 2f + 0.01f;
            for (int d = 0; d < 2; d++)
            {
                float x = (d == 0 ? -1f : 1f) * width / 4f;
                Box("Door", new Vector3(x, -0.04f, faceZ), new Vector3(width / 2f - 0.06f, height - 0.3f, 0.03f), olive);
                Box("Handle", new Vector3(x + (d == 0 ? 1 : -1) * (width / 4f - 0.14f), -0.04f, faceZ + 0.04f), new Vector3(0.04f, 0.5f, 0.04f), dark);
                for (int k = 0; k < 8; k++)
                    Box("Louver", new Vector3(x, height / 2f - 0.5f - k * 0.075f, faceZ + 0.02f), new Vector3(width / 2f - 0.3f, 0.02f, 0.02f), dark);
            }
            Box("StencilPlate", new Vector3(0, -0.5f, faceZ + 0.02f), new Vector3(0.6f, 0.2f, 0.01f), plate);
            Label(group, "StencilText", "AMMO", c + Vector3.up * -0.5f + f * (depth / 2f + 0.035f), f, 1.2f, new Color(0.1f, 0.1f, 0.08f), 0.55f);
        }

        /// <summary>Steel plinth over a greybox pedestal: panelled body, hazard band, and a glowing cyan cap ring.</summary>
        static void DressPedestal(Transform root, Transform pedestal)
        {
            var r = pedestal.GetComponent<Renderer>();
            if (r == null) return;
            var b = r.bounds;
            r.enabled = false;
            var steel = Steel("Plinth_Steel", new Color(0.5f, 0.53f, 0.57f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var hazard = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Greybox/Greybox_Hazard.mat");
            var ring = Lit("Plinth_Ring", new Color(0.05f, 0.2f, 0.25f), 0f, 0.8f, Cyan * 3.5f);
            var group = Group(root, pedestal.name + "Model");
            Vector3 c = b.center;
            float top = b.max.y, bottom = b.min.y;
            Part(group, "Body", PrimitiveType.Cube, new Vector3(c.x, (top + bottom) / 2f, c.z), new Vector3(b.size.x - 0.1f, b.size.y - 0.1f, b.size.z - 0.1f), steel);
            Part(group, "Base", PrimitiveType.Cube, new Vector3(c.x, bottom + 0.06f, c.z), new Vector3(b.size.x, 0.12f, b.size.z), dark);
            if (hazard != null) Part(group, "HazardBand", PrimitiveType.Cube, new Vector3(c.x, bottom + 0.22f, c.z), new Vector3(b.size.x + 0.02f, 0.1f, b.size.z + 0.02f), hazard);
            Part(group, "Cap", PrimitiveType.Cube, new Vector3(c.x, top - 0.03f, c.z), new Vector3(b.size.x + 0.08f, 0.06f, b.size.z + 0.08f), dark);
            Part(group, "GlowRing", PrimitiveType.Cube, new Vector3(c.x, top - 0.14f, c.z), new Vector3(b.size.x + 0.03f, 0.04f, b.size.z + 0.03f), ring, shadows: false);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 pos = c + new Vector3(dir.x * b.size.x / 2f, 0, dir.z * b.size.z / 2f);
                Part(group, "Panel", PrimitiveType.Cube, new Vector3(pos.x, (top + bottom) / 2f + 0.05f, pos.z) + dir * 0.005f,
                    Mathf.Abs(dir.x) > 0.5f ? new Vector3(0.015f, b.size.y * 0.5f, b.size.z * 0.7f) : new Vector3(b.size.x * 0.7f, b.size.y * 0.5f, 0.015f), dark);
            }
        }

        /// <summary>Steel frame, hood, conduits and a warning label around the hub junction box face (wire tiles stay as they are).</summary>
        static void DressJunctionBox(Transform root, Transform backing)
        {
            var r = backing.GetComponent<Renderer>();
            if (r == null) return;
            var b = r.bounds;
            var steel = Steel("Plinth_Steel", new Color(0.5f, 0.53f, 0.57f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var group = Group(root, "JunctionBoxFrame");
            // The backing is a flat face on the south wall of the hub; its front points +Z
            Vector3 c = b.center + Vector3.back * 0f;
            float w = Mathf.Max(b.size.x, b.size.z), h = b.size.y;
            bool faceZ = b.size.z < b.size.x;
            Vector3 front = faceZ ? Vector3.forward : Vector3.right * (backing.forward.x >= 0 ? 1 : -1);
            Vector3 right = Vector3.Cross(Vector3.up, front);
            var rot = Quaternion.LookRotation(front, Vector3.up);
            void Frame(string n, Vector3 offset, Vector3 size, Material m)
            {
                var go = Part(group, n, PrimitiveType.Cube, c + right * offset.x + Vector3.up * offset.y + front * offset.z, size, m);
                go.transform.rotation = rot;
            }
            const float t = 0.14f;
            Frame("Top", new Vector3(0, h / 2f + t / 2f - 0.01f, 0.05f), new Vector3(w + 2 * t, t, 0.2f), steel);
            Frame("Bottom", new Vector3(0, -h / 2f - t / 2f + 0.01f, 0.05f), new Vector3(w + 2 * t, t, 0.2f), steel);
            Frame("Left", new Vector3(-w / 2f - t / 2f + 0.01f, 0, 0.05f), new Vector3(t, h + 2 * t, 0.2f), steel);
            Frame("Right", new Vector3(w / 2f + t / 2f - 0.01f, 0, 0.05f), new Vector3(t, h + 2 * t, 0.2f), steel);
            Frame("Hood", new Vector3(0, h / 2f + t + 0.04f, 0.1f), new Vector3(w + 0.5f, 0.08f, 0.35f), dark);
            for (int i = 0; i < 4; i++)
                Frame("Bolt", new Vector3((i % 2 == 0 ? -1 : 1) * (w / 2f + t / 2f), (i < 2 ? -1 : 1) * (h / 2f + t / 2f), 0.16f), new Vector3(0.05f, 0.05f, 0.03f), dark);
            Frame("WarnStripe", new Vector3(0, -h / 2f - t - 0.1f, 0.03f), new Vector3(w + 2 * t, 0.12f, 0.03f), yellow);
            foreach (float x in new[] { -w / 3f, w / 3f })
            {
                // Conduit up to the ceiling (ceiling inner face at 4 m)
                float topY = b.max.y + t + 0.12f;
                var a = c + right * x + Vector3.up * (topY - c.y) + front * 0.1f;
                Tube(group, "Conduit", a, new Vector3(a.x, 4f, a.z), 0.06f, dark);
            }
        }

        /// <summary>Hub lockers, junction box frame and the two fuse pedestals (called from BunkerEquipment).</summary>
        static string DressBunkerProps(Transform root)
        {
            var group = Group(root, "Props");
            int lockers = 0, pedestals = 0, frames = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (t.root.name == RootName) continue;
                if (t.name == "AmmoLocker") { DressLocker(group, t, new Vector3(0, 0, 20)); lockers++; }
                else if (t.name == "Fuse1_Pod" || t.name == "Fuse2_Pedestal") { DressPedestal(group, t); pedestals++; }
                else if (t.name == "JunctionBoxBacking") { DressJunctionBox(group, t); frames++; }
            }
            return $"lockers={lockers}, pedestals={pedestals}, junction frames={frames}";
        }
    }
}
