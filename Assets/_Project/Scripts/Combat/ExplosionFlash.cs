using UnityEngine;

namespace SignalLost
{
    /// <summary>Greybox explosion effect: a growing, fading fireball and a light flash. Replaced by the Particle Pack in Phase 4.</summary>
    public class ExplosionFlash : MonoBehaviour
    {
        const float Duration = 0.35f;

        float radius;
        float age;
        Light flash;
        Material material;

        public static void Spawn(Vector3 position, float radius)
        {
            Sfx.Play(radius >= 4f ? "explosion_big" : radius >= 1.5f ? "explosion" : "spark", position);
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ExplosionFlash";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;

            var effect = go.AddComponent<ExplosionFlash>();
            effect.radius = radius;
            effect.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            effect.material.SetColor("_BaseColor", new Color(1f, 0.55f, 0.1f));
            go.GetComponent<Renderer>().sharedMaterial = effect.material;

            effect.flash = go.AddComponent<Light>();
            effect.flash.color = new Color(1f, 0.6f, 0.2f);
            effect.flash.range = radius * 3f;
            effect.flash.intensity = 8f;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / Duration;
            transform.localScale = Vector3.one * Mathf.Lerp(0.5f, radius * 1.4f, t);
            flash.intensity = Mathf.Lerp(8f, 0f, t);
            if (t >= 1f) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
