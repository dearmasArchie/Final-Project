using UnityEngine;

namespace SignalLost
{
    /// <summary>Small spark bursts at hit points (greybox stand-in until the Particle Pack effects are added).</summary>
    public static class ImpactEffects
    {
        static Material material;

        public static void Sparks(Vector3 point, Vector3 normal, Color color, int count = 10)
        {
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                material.SetFloat("_Surface", 1f);   // transparent
                material.SetFloat("_Blend", 2f);     // additive
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }

            var go = new GameObject("Sparks");
            go.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal.sqrMagnitude > 0.01f ? normal : Vector3.up));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = color;
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.02f;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            ps.Play();
        }
    }
}
