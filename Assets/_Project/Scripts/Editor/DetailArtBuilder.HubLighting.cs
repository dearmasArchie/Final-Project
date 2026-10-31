using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        /// <summary>
        /// Underground Central Hub lighting: the single blown-out ceiling light becomes a soft fill, and each of the four
        /// caged lamps gets its own warm light (with soft shadows from the beams and pillars). Colored accents mark the
        /// two wing doors and the junction box, and a low cold light fills the corners so the walls don't fall to black.
        /// </summary>
        static string DressHubLighting(Transform root)
        {
            var group = Group(root, "HubLighting");
            int lights = 0;

            // The old single point light: now just a gentle fill
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                var p = l.transform.position;
                if (l.name == "Light" && l.type == LightType.Point && Mathf.Abs(p.x) < 0.5f && Mathf.Abs(p.z - 20f) < 0.5f && l.transform.root.name != RootName)
                {
                    l.intensity = 2.2f;
                    l.range = 20f;
                    l.color = new Color(0.95f, 0.93f, 0.88f);
                }
            }

            // One warm light under each caged lamp; soft shadows from beams and pillars
            foreach (float x in new[] { -4f, 4f })
                foreach (float z in new[] { 16f, 24f })
                {
                    var light = PointLight(group, $"HubLamp_{x}_{z}", new Vector3(x, 3.3f, z), new Color(1f, 0.92f, 0.78f), 6f, 11f);
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.75f;
                    lights++;
                }

            // Door accents: red over the laser wing, cyan over the flooded wing, cyan wash on the junction box
            PointLight(group, "WingA_Warning", new Vector3(-7.3f, 3.1f, 20f), new Color(1f, 0.12f, 0.08f), 2.2f, 6f);
            PointLight(group, "WingB_Cyan", new Vector3(7.3f, 3.1f, 20f), new Color(0.2f, 0.85f, 1f), 2.0f, 6f);
            PointLight(group, "JunctionWash", new Vector3(5f, 2.4f, 13.6f), new Color(0.3f, 0.9f, 1f), 2.4f, 5f);
            lights += 3;

            // Low cool fill in the four corners
            foreach (float x in new[] { -6.5f, 6.5f })
                foreach (float z in new[] { 14f, 26f })
                {
                    PointLight(group, $"CornerFill_{x}_{z}", new Vector3(x, 1.2f, z), new Color(0.55f, 0.65f, 0.85f), 1.2f, 6f);
                    lights++;
                }
            return $"hub lighting: {lights} lights";
        }
    }
}
