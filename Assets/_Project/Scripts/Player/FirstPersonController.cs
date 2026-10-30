using UnityEngine;

namespace SignalLost
{
    /// <summary>Walking, sprinting with stamina, jumping, gravity, and mouse look.</summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public class FirstPersonController : MonoBehaviour
    {
        public const string SensitivityKey = "MouseSensitivity";

        [SerializeField] Transform cameraPivot;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float sprintSpeed = 7f;
        [SerializeField] float jumpHeight = 1.1f;
        [SerializeField] float gravity = -20f;

        [Header("Stamina")]
        [SerializeField] float maxStamina = 100f;
        [SerializeField] float sprintDrainPerSecond = 25f;
        [SerializeField] float regenPerSecond = 18f;
        [SerializeField] float regenDelay = 1f;

        [Header("Look")]
        [Tooltip("Degrees per mouse pixel at sensitivity 1. The Settings menu scales this.")]
        [SerializeField] float baseLookSpeed = 0.1f;

        CharacterController controller;
        PlayerInputReader input;
        float pitch;
        float verticalVelocity;
        float stamina;
        float regenTimer;

        public float Stamina01 => stamina / maxStamina;
        public bool IsSprinting { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            stamina = maxStamina;
        }

        void OnEnable() => SetCursorLocked(true);

        void Update()
        {
            if (input.Pause.WasPressedThisFrame()) SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
            if (Time.timeScale == 0f) return;

            if (Cursor.lockState == CursorLockMode.Locked) UpdateLook();
            UpdateMovement();
            UpdateStamina();

            if (HUDController.Instance != null) HUDController.Instance.SetStamina(Stamina01);
        }

        void UpdateLook()
        {
            float sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
            Vector2 look = input.Look.ReadValue<Vector2>() * baseLookSpeed * sensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        void UpdateMovement()
        {
            Vector2 move2 = Vector2.ClampMagnitude(input.Move.ReadValue<Vector2>(), 1f);
            bool wantsSprint = input.Sprint.IsPressed() && move2.y > 0.1f;
            IsSprinting = wantsSprint && stamina > 0f;

            float speed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 move = transform.TransformDirection(new Vector3(move2.x, 0f, move2.y)) * speed;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f; // keeps the controller pressed onto slopes
                if (input.Jump.WasPressedThisFrame()) verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }

        void UpdateStamina()
        {
            if (IsSprinting)
            {
                stamina = Mathf.Max(0f, stamina - sprintDrainPerSecond * Time.deltaTime);
                regenTimer = regenDelay;
            }
            else if (regenTimer > 0f)
            {
                regenTimer -= Time.deltaTime;
            }
            else
            {
                stamina = Mathf.Min(maxStamina, stamina + regenPerSecond * Time.deltaTime);
            }
        }

        /// <summary>Menus call this with false to free the mouse, and true when returning to the game.</summary>
        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
