using UnityEngine;
using UnityEngine.InputSystem;

namespace SignalLost
{
    /// <summary>
    /// All player input actions in one place (keyboard + mouse, with gamepad bindings).
    /// Other player scripts read from here instead of touching devices directly.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public InputAction Move { get; private set; }
        public InputAction Look { get; private set; }
        public InputAction Jump { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Fire { get; private set; }
        public InputAction AltFire { get; private set; }
        public InputAction Reload { get; private set; }
        public InputAction Interact { get; private set; }
        public InputAction Pause { get; private set; }

        InputAction[] all;

        void Awake()
        {
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            Look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=15,y=15)");

            Jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            Jump.AddBinding("<Gamepad>/buttonSouth");
            Sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            Sprint.AddBinding("<Gamepad>/leftStickPress");
            Fire = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            Fire.AddBinding("<Gamepad>/rightTrigger");
            AltFire = new InputAction("AltFire", InputActionType.Button, "<Mouse>/rightButton");
            AltFire.AddBinding("<Gamepad>/leftTrigger");
            Reload = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
            Reload.AddBinding("<Gamepad>/buttonWest");
            Interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            Interact.AddBinding("<Gamepad>/buttonNorth");
            Pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            Pause.AddBinding("<Gamepad>/start");

            all = new[] { Move, Look, Jump, Sprint, Fire, AltFire, Reload, Interact, Pause };
        }

        void OnEnable()
        {
            foreach (var action in all) action.Enable();
        }

        void OnDisable()
        {
            foreach (var action in all) action.Disable();
        }

        void OnDestroy()
        {
            foreach (var action in all) action.Dispose();
        }
    }
}
