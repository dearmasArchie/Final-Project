using System;
using System.Collections;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// The Dual-Mode Arc Pistol.
    /// Pulse: left-click (hold for auto), 4 shots/s, 20 damage, 1 ammo.
    /// Overload Beam: hold right-click 1 s and release; hitscan, 60 damage, 4 ammo, 2 s cooldown.
    /// Unusable (and hidden) until GameManager.HasArcPistol is true.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class ArcPistol : MonoBehaviour
    {
        public const int MagazineSize = 12;
        public const int MaxReserve = 48;

        /// <summary>Raised on every shot with the muzzle position. Enemies use it to hear gunfire.</summary>
        public static event Action<Vector3> Fired;

        /// <summary>Other loud events (explosions) alert enemies the same way gunshots do.</summary>
        public static void RaiseNoise(Vector3 position) => Fired?.Invoke(position);

        [SerializeField] Transform cameraPivot;
        [SerializeField] Transform muzzle;
        [SerializeField] GameObject model;
        [SerializeField] Projectile pulsePrefab;
        [SerializeField] LineRenderer beamLine;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Pulse")]
        [SerializeField] float pulseDamage = 20f;
        [SerializeField] float shotsPerSecond = 4f;
        [SerializeField] float reloadTime = 1.5f;
        [SerializeField] float maxAimDistance = 200f;

        [Header("Overload Beam")]
        [SerializeField] float overloadDamage = 60f;
        [SerializeField] float overloadChargeTime = 1f;
        [SerializeField] int overloadAmmoCost = 4;
        [SerializeField] float overloadCooldown = 2f;
        [SerializeField] float beamVisibleTime = 0.15f;

        PlayerInputReader input;
        float nextShotTime;
        float chargeTimer;
        float overloadReadyTime;
        AudioSource chargeSource;

        public int Magazine { get; private set; } = MagazineSize;
        public int Reserve { get; private set; } = MaxReserve;
        public bool IsReloading { get; private set; }
        public bool IsCharging { get; private set; }
        bool Equipped => GameManager.Instance != null && GameManager.Instance.HasArcPistol;

        void Awake() => input = GetComponent<PlayerInputReader>();

        void Start()
        {
            if (beamLine != null) beamLine.enabled = false;
            RefreshHud();
        }

        void Update()
        {
            if (model != null && model.activeSelf != Equipped) model.SetActive(Equipped);
            if (!Equipped || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked)
            {
                CancelCharge();
                return;
            }

            if (input.Reload.WasPressedThisFrame()) TryReload();
            UpdateOverload();
            if (!IsCharging && input.Fire.IsPressed() && Time.time >= nextShotTime) TryFirePulse();
        }

        // ------------------------------------------------------------------ Pulse

        void TryFirePulse()
        {
            if (IsReloading) return;
            if (Magazine <= 0)
            {
                if (Reserve == 0)
                {
                    nextShotTime = Time.time + 0.4f;
                    Sfx.Play2D("dry_fire");
                }
                TryReload();
                return;
            }

            nextShotTime = Time.time + 1f / shotsPerSecond;
            Magazine--;
            RefreshHud();

            Vector3 target = AimPoint(out _);
            var bolt = Instantiate(pulsePrefab, muzzle.position, Quaternion.LookRotation(target - muzzle.position));
            bolt.Launch(pulseDamage, DamageType.Pulse, hitMask, countForAccuracy: true);
            Sfx.Play2D("pulse_fire");
            Fired?.Invoke(muzzle.position);

            if (Magazine == 0) TryReload();
        }

        // ------------------------------------------------------------------ Overload Beam

        void UpdateOverload()
        {
            bool canCharge = !IsReloading && Time.time >= overloadReadyTime && Magazine >= overloadAmmoCost;

            if (input.AltFire.IsPressed() && canCharge)
            {
                if (!IsCharging) StartChargeSound();
                IsCharging = true;
                chargeTimer += Time.deltaTime;
                if (chargeSource != null) chargeSource.pitch = 0.7f + 0.9f * Mathf.Clamp01(chargeTimer / overloadChargeTime);
            }
            else if (IsCharging)
            {
                // Released (or became unable): fire only if fully charged; releasing early costs nothing.
                if (chargeTimer >= overloadChargeTime && canCharge) FireOverload();
                CancelCharge();
            }

            if (HUDController.Instance != null)
            {
                float charge = IsCharging ? Mathf.Clamp01(chargeTimer / overloadChargeTime) : 0f;
                HUDController.Instance.SetOverload(charge, canCharge);
            }
        }

        void FireOverload()
        {
            Magazine -= overloadAmmoCost;
            Sfx.Play2D("overload_fire");
            overloadReadyTime = Time.time + overloadCooldown;
            RefreshHud();

            Vector3 end = AimPoint(out var hit);
            if (hit.collider != null)
            {
                if (!hit.collider.TryGetComponent<IDamageable>(out var target))
                    target = hit.collider.GetComponentInParent<IDamageable>();
                target?.TakeDamage(overloadDamage, DamageType.Overload, hit.point);
                GameManager.Instance.RegisterShot(target != null);
                ImpactEffects.Sparks(hit.point, hit.normal, new Color(0.6f, 0.95f, 1f), 24);
                Sfx.Play(target != null ? "impact_hit" : "impact_surface", hit.point);
                if (target != null && HUDController.Instance != null) HUDController.Instance.ShowHitMarker(false);
            }
            else
            {
                GameManager.Instance.RegisterShot(false);
            }

            Fired?.Invoke(muzzle.position);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.06f, 0.25f);
            if (beamLine != null) StartCoroutine(ShowBeam(end));
            if (Magazine == 0) TryReload();
        }

        void StartChargeSound()
        {
            if (chargeSource == null)
            {
                chargeSource = Sfx.Loop(gameObject, "overload_charge", autoplay: false);
                if (chargeSource != null) chargeSource.spatialBlend = 0f;
            }
            if (chargeSource != null) chargeSource.Play();
        }

        void CancelCharge()
        {
            if (chargeSource != null && chargeSource.isPlaying) chargeSource.Stop();
            IsCharging = false;
            chargeTimer = 0f;
        }

        IEnumerator ShowBeam(Vector3 end)
        {
            if (beamLine.TryGetComponent<LightningLine>(out var lightning))
            {
                lightning.Show(muzzle.position, end);
                yield return new WaitForSeconds(beamVisibleTime);
                lightning.Hide();
                yield break;
            }
            beamLine.enabled = true;
            beamLine.SetPosition(0, muzzle.position);
            beamLine.SetPosition(1, end);
            yield return new WaitForSeconds(beamVisibleTime);
            beamLine.enabled = false;
        }

        // ------------------------------------------------------------------ Ammo

        void TryReload()
        {
            if (IsReloading || Magazine == MagazineSize || Reserve == 0) return;
            StartCoroutine(ReloadRoutine());
        }

        IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            CancelCharge();
            Sfx.Play2D("reload");
            yield return new WaitForSeconds(reloadTime);
            int taken = Mathf.Min(MagazineSize - Magazine, Reserve);
            Magazine += taken;
            Reserve -= taken;
            IsReloading = false;
            Sfx.Play2D("reload_done");
            RefreshHud();
        }

        /// <summary>Battery Clip pickup or ammo locker. Returns false when the reserve is already full.</summary>
        public bool AddAmmo(int amount)
        {
            if (Reserve >= MaxReserve) return false;
            Reserve = Mathf.Min(MaxReserve, Reserve + amount);
            RefreshHud();
            return true;
        }

        public bool ReserveFull => Reserve >= MaxReserve;

        /// <summary>Used when loading saved state (scene change or checkpoint).</summary>
        public void SetAmmo(int magazine, int reserve)
        {
            Magazine = Mathf.Clamp(magazine, 0, MagazineSize);
            Reserve = Mathf.Clamp(reserve, 0, MaxReserve);
            RefreshHud();
        }

        // ------------------------------------------------------------------ helpers

        Vector3 AimPoint(out RaycastHit hit)
        {
            var aimRay = new Ray(cameraPivot.position, cameraPivot.forward);
            return Physics.Raycast(aimRay, out hit, maxAimDistance, hitMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : aimRay.GetPoint(maxAimDistance);
        }

        void RefreshHud()
        {
            if (HUDController.Instance != null) HUDController.Instance.SetAmmo(Magazine, Reserve);
        }
    }
}
