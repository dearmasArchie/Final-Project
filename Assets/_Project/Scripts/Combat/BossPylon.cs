using System;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Titan Phase 2 power pylon on the pier. Shootable (100 HP). When destroyed it goes dark;
    /// Reboot() brings it back at a fraction of its health.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class BossPylon : MonoBehaviour
    {
        [SerializeField] Renderer[] glowRenderers;
        [SerializeField] Transform tetherPoint;

        Health health;
        Collider[] colliders;
        MaterialPropertyBlock dark;

        public event Action<BossPylon> Destroyed;
        public bool IsDown => health.IsDead;
        public Transform TetherPoint => tetherPoint;

        void Awake()
        {
            health = GetComponent<Health>();
            colliders = GetComponentsInChildren<Collider>();
            dark = new MaterialPropertyBlock();
            dark.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.1f));
            dark.SetColor("_EmissionColor", Color.black);
        }

        void OnEnable() => health.Died += OnDied;
        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            foreach (var c in colliders) c.enabled = false;
            foreach (var r in glowRenderers) r.SetPropertyBlock(dark);
            ExplosionFlash.Spawn(transform.position + Vector3.up * 3f, 2f);
            Destroyed?.Invoke(this);
        }

        public void Reboot(float healthFraction)
        {
            health.SetHealth(health.Max * healthFraction);
            foreach (var c in colliders) c.enabled = true;
            foreach (var r in glowRenderers) r.SetPropertyBlock(null);
        }
    }
}
