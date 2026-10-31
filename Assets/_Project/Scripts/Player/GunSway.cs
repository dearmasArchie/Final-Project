using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Keeps the first-person gun alive: a step bob while walking (stronger and lowered into a carry pose while sprinting),
    /// slow breathing when standing, a springy recoil kick on every shot, a lag behind mouse look, a dip when landing, a tilt
    /// during reload and a tremble while the Overload Beam charges. Moves only this transform (a child of the camera), so
    /// where shots actually go never changes. Added to the gun model by DetailArtBuilder.DressPistol.
    /// </summary>
    public class GunSway : MonoBehaviour
    {
        [Header("Walk bob")]
        [SerializeField, Tooltip("Bob cycles (one left + one right step) per meter walked, in radians")] float strideRadiansPerMeter = 1.4f;
        [SerializeField] float walkBobSide = 0.011f;
        [SerializeField] float walkBobUp = 0.009f;
        [SerializeField] float sprintBobScale = 1.7f;
        [SerializeField] Vector3 sprintOffset = new Vector3(-0.06f, -0.05f, -0.02f);
        [SerializeField] Vector3 sprintTilt = new Vector3(14f, -18f, -6f);

        [Header("Recoil")]
        [SerializeField, Tooltip("Spring stiffness; the lower the damping against it, the more the gun bounces after a kick")] float spring = 230f;
        [SerializeField] float damping = 20f;
        [SerializeField] Vector3 pulseKick = new Vector3(0f, 0.2f, -1.15f);
        [SerializeField] float pulseKickPitch = 75f;

        [Header("Look sway")]
        [SerializeField] float lookSway = 0.00035f;
        [SerializeField] float lookSwayMax = 0.03f;

        ArcPistol pistol;
        CharacterController controller;
        FirstPersonController movement;
        PlayerInputReader input;

        Vector3 restPosition;
        Quaternion restRotation;
        Vector3 kickPos, kickPosVel, kickRot, kickRotVel;
        Vector2 lookOffset;
        float phase, moveBlend, sprintBlend, reloadBlend, chargeBlend;
        bool wasGrounded = true, wasReloading;
        float lastFallSpeed;

        /// <summary>The offset from the rest pose this frame (for tests).</summary>
        public Vector3 CurrentOffset { get; private set; }

        void Awake()
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            pistol = GetComponentInParent<ArcPistol>();
            controller = GetComponentInParent<CharacterController>();
            movement = GetComponentInParent<FirstPersonController>();
            input = GetComponentInParent<PlayerInputReader>();
        }

        void OnEnable()
        {
            if (pistol != null) pistol.ShotFired += OnShot;
            kickPos = kickPosVel = kickRot = kickRotVel = Vector3.zero;
            wasReloading = pistol != null && pistol.IsReloading;
        }

        void OnDisable()
        {
            if (pistol != null) pistol.ShotFired -= OnShot;
        }

        void OnShot(bool overload)
        {
            float k = overload ? 2.3f : 1f;
            kickPosVel += pulseKick * k;
            kickRotVel += new Vector3(-pulseKickPitch * k, Random.Range(-18f, 18f) * k, Random.Range(-22f, 22f) * k);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused

            Vector3 velocity = controller != null ? controller.velocity : Vector3.zero;
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            bool grounded = controller == null || controller.isGrounded;

            moveBlend = Mathf.MoveTowards(moveBlend, grounded ? Mathf.Clamp01(speed / 4.5f) : 0f, dt * 6f);
            bool sprinting = movement != null && movement.IsSprinting && speed > 0.5f && grounded;
            sprintBlend = Mathf.MoveTowards(sprintBlend, sprinting ? 1f : 0f, dt * 5f);
            phase += dt * speed * strideRadiansPerMeter;

            // Landing: a dip proportional to how fast the player was falling
            if (!grounded) lastFallSpeed = Mathf.Max(lastFallSpeed, -velocity.y);
            if (grounded && !wasGrounded)
            {
                kickPosVel += new Vector3(0f, -Mathf.Min(lastFallSpeed, 12f) * 0.035f, 0f);
                kickRotVel += new Vector3(Mathf.Min(lastFallSpeed, 12f) * 5f, 0f, 0f);
                lastFallSpeed = 0f;
            }
            wasGrounded = grounded;

            // Springs for recoil and landing: slightly underdamped, so they bounce once or twice and settle
            Step(ref kickPos, ref kickPosVel, dt);
            Step(ref kickRot, ref kickRotVel, dt);

            // Walk bob (a figure-eight) with a roll toward the stepping side
            float bobScale = moveBlend * Mathf.Lerp(1f, sprintBobScale, sprintBlend);
            Vector3 pos = new Vector3(Mathf.Sin(phase) * walkBobSide, Mathf.Sin(phase * 2f) * walkBobUp, Mathf.Sin(phase * 2f + 1f) * 0.005f) * bobScale;
            Vector3 rot = new Vector3(Mathf.Sin(phase * 2f) * 0.9f, Mathf.Sin(phase) * 0.8f, -Mathf.Sin(phase) * 1.8f) * bobScale;

            // Breathing while standing
            float still = 1f - moveBlend;
            pos.y += Mathf.Sin(Time.time * 1.7f) * 0.0025f * still;
            rot.z += Mathf.Sin(Time.time * 1.1f) * 0.4f * still;

            // Sprint carry pose
            pos += sprintOffset * sprintBlend;
            rot += sprintTilt * sprintBlend;

            // Lag behind the mouse
            if (input != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = input.Look.ReadValue<Vector2>();
                Vector2 target = new Vector2(Mathf.Clamp(-look.x * lookSway, -lookSwayMax, lookSwayMax), Mathf.Clamp(-look.y * lookSway, -lookSwayMax, lookSwayMax));
                lookOffset = Vector2.Lerp(lookOffset, target, 1f - Mathf.Exp(-14f * dt));
            }
            else lookOffset = Vector2.Lerp(lookOffset, Vector2.zero, 1f - Mathf.Exp(-14f * dt));
            pos += new Vector3(lookOffset.x, lookOffset.y, 0f);
            rot += new Vector3(-lookOffset.y * 120f, lookOffset.x * 120f, 0f);

            // Reload: tip the gun down and aside, with a snap back at the end
            bool reloading = pistol != null && pistol.IsReloading;
            reloadBlend = Mathf.MoveTowards(reloadBlend, reloading ? 1f : 0f, dt * 5f);
            float eased = Mathf.SmoothStep(0f, 1f, reloadBlend);
            pos += new Vector3(-0.02f, -0.1f, -0.03f) * eased + new Vector3(0f, Mathf.Sin(Time.time * 16f) * 0.004f, 0f) * eased;
            rot += new Vector3(32f, 14f, -22f) * eased;
            if (wasReloading && !reloading)
            {
                kickPosVel += new Vector3(0f, 0.15f, -0.55f);
                kickRotVel += new Vector3(-60f, 0f, 15f);
            }
            wasReloading = reloading;

            // Charging the Overload Beam: pull back a little and tremble
            chargeBlend = Mathf.MoveTowards(chargeBlend, pistol != null && pistol.IsCharging ? 1f : 0f, dt * 4f);
            pos += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * 0.0018f * chargeBlend + new Vector3(0f, 0f, -0.02f) * chargeBlend;

            pos += kickPos;
            rot += kickRot;
            CurrentOffset = pos;
            transform.localPosition = restPosition + pos;
            transform.localRotation = restRotation * Quaternion.Euler(rot);
        }

        void Step(ref Vector3 value, ref Vector3 velocity, float dt)
        {
            // Sub-step so a low frame rate can't blow the spring up
            int steps = Mathf.CeilToInt(dt / 0.008f);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                velocity += (-spring * value - damping * velocity) * h;
                value += velocity * h;
            }
        }
    }
}
