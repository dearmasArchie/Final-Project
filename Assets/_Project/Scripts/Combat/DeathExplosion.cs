using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Spawns an explosion effect (a Particle Pack prefab) when this enemy's Health runs out.
    /// The effect is scaled to the enemy's size and cleans itself up.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DeathExplosion : MonoBehaviour
    {
        [SerializeField] GameObject effectPrefab;
        [SerializeField, Tooltip("Size of the effect relative to the prefab")] float scale = 1f;
        [SerializeField, Tooltip("Where the blast centers, relative to the enemy's root")] Vector3 offset = new Vector3(0f, 1f, 0f);
        [SerializeField] float lifetime = 5f;

        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
            health.Died += Explode;
        }

        void OnDestroy()
        {
            if (health != null) health.Died -= Explode;
        }

        void Explode()
        {
            if (effectPrefab == null) return;
            var effect = Instantiate(effectPrefab, transform.TransformPoint(offset), Quaternion.identity);
            effect.transform.localScale *= scale;
            Sfx.Play(effectPrefab.name.Contains("Big") ? "explosion_big" : "explosion", effect.transform.position);
            // Particle systems ignore transform scale unless they scale with the hierarchy
            foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.loop = false;
                if (!ps.isPlaying) ps.Play();
            }
            Destroy(effect, lifetime);
        }
    }
}
