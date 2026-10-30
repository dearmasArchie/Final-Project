using UnityEngine;
using UnityEngine.InputSystem;

namespace SignalLost.DebugTools
{
    /// <summary>
    /// TEMPORARY stand-in so Partner B can test scenes before Partner A's player prefab exists.
    /// WASD to move, mouse to look, Space to jump, E to interact. Delete it from the scenes
    /// once the real Player prefab is placed.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DebugPlayer : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float lookSensitivity = 0.1f;
        [SerializeField] float interactRange = 3f;
        [SerializeField] LayerMask interactMask = ~0;

        CharacterController controller;
        float pitch;
        float verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            // Look
            Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

            // Move
            Vector3 input = new Vector3(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), 0f,
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            Vector3 move = transform.TransformDirection(Vector3.ClampMagnitude(input, 1f)) * moveSpeed;

            if (controller.isGrounded)
            {
                verticalVelocity = -1f;
                if (keyboard.spaceKey.wasPressedThisFrame) verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y);
            }
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);

            UpdateInteraction(keyboard.eKey.wasPressedThisFrame);
        }

        void UpdateInteraction(bool pressed)
        {
            IInteractable target = null;
            var ray = new Ray(cameraPivot.position, cameraPivot.forward);
            if (Physics.Raycast(ray, out var hit, interactRange, interactMask, QueryTriggerInteraction.Collide))
                target = hit.collider.GetComponentInParent<IInteractable>();

            if (HUDController.Instance != null)
                HUDController.Instance.SetPrompt(target?.Prompt, target != null && target.CanInteract);

            if (pressed && target != null && target.CanInteract)
                target.Interact(gameObject);
        }
    }
}
