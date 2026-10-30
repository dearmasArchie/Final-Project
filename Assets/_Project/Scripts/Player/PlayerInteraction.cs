using UnityEngine;

namespace SignalLost
{
    /// <summary>Looks for an IInteractable under the crosshair (3 m), shows its prompt, and uses it on E.</summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;
        [SerializeField] float range = 3f;
        [SerializeField] LayerMask mask = ~0;

        PlayerInputReader input;

        public IInteractable Current { get; private set; }

        void Awake() => input = GetComponent<PlayerInputReader>();

        void Update()
        {
            Current = null;
            var ray = new Ray(cameraPivot.position, cameraPivot.forward);
            if (Physics.Raycast(ray, out var hit, range, mask, QueryTriggerInteraction.Collide))
                Current = hit.collider.GetComponentInParent<IInteractable>();

            if (HUDController.Instance != null)
                HUDController.Instance.SetPrompt(Current?.Prompt, Current != null && Current.CanInteract);

            if (Time.timeScale > 0f && input.Interact.WasPressedThisFrame() && Current != null && Current.CanInteract)
            {
                Sfx.Play2D("interact");
                Current.Interact(gameObject);
            }
        }

        void OnDisable()
        {
            if (HUDController.Instance != null) HUDController.Instance.SetPrompt(null);
        }
    }
}
