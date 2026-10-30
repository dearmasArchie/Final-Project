using System.Collections;
using UnityEngine;

namespace SignalLost.DebugTools
{
    /// <summary>Shooting target for testing: flashes when hit, logs damage, and respawns after dying.</summary>
    [RequireComponent(typeof(Health))]
    public class TestDummy : MonoBehaviour
    {
        [SerializeField] float respawnDelay = 2f;
        [SerializeField] Color hitColor = Color.white;

        Health health;
        Renderer[] renderers;
        Collider[] colliders;
        MaterialPropertyBlock block;

        void Awake()
        {
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            block = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void OnDamaged(float amount, DamageType type, Vector3 point)
        {
            Debug.Log($"[TestDummy] {name} took {amount} {type} damage ({health.Current}/{health.Max} left)");
            StartCoroutine(Flash());
        }

        void OnDied() => StartCoroutine(Respawn());

        IEnumerator Flash()
        {
            block.SetColor("_BaseColor", hitColor);
            foreach (var r in renderers) r.SetPropertyBlock(block);
            yield return new WaitForSeconds(0.08f);
            foreach (var r in renderers) r.SetPropertyBlock(null);
        }

        IEnumerator Respawn()
        {
            SetVisible(false);
            yield return new WaitForSeconds(respawnDelay);
            health.ResetHealth();
            SetVisible(true);
        }

        void SetVisible(bool visible)
        {
            foreach (var r in renderers) r.enabled = visible;
            foreach (var c in colliders) c.enabled = visible;
        }
    }
}
