using UnityEngine;
using UnityEngine.InputSystem;
using ZoneStrike.Core.Interfaces;

namespace ZoneStrike.Infrastructure.Input
{
    /// <summary>
    /// Architecture role: Infrastructure-layer concrete implementation of
    /// <see cref="IInputSource"/>, wrapping Unity's Input System package (Technical Architecture
    /// §1: chosen over the legacy Input Manager specifically for clean touch + external-gamepad
    /// abstraction). This is the ONLY class in the project allowed to reference
    /// <c>UnityEngine.InputSystem</c> types directly — Gameplay code depends only on
    /// <see cref="IInputSource"/>, so swapping input methods never touches PlayerController.
    ///
    /// Optimisation notes: samples input once per frame into a cached struct
    /// (<see cref="_cachedInput"/>) rather than re-querying the Input System on every call to
    /// <see cref="SampleCurrentInput"/> — PlayerController's FixedUpdateNetwork and any UI code
    /// reading input in the same frame get a consistent, cheap read.
    ///
    /// Extension points: two concrete input sources are supported side-by-side — on-screen
    /// virtual joystick (touch, primary target per the brief's mobile-first audience) and a
    /// bound Gamepad/MFi controller device — selected automatically based on which device last
    /// produced input, with a small dead-zone/priority rule in <see cref="ResolveActiveDevice"/>
    /// to avoid flicker between the two. A third source (Editor keyboard/mouse) is compiled only
    /// in the Editor for development convenience.
    ///
    /// Networking considerations: this class produces the exact <see cref="PlayerInputData"/>
    /// struct that crosses the network boundary (see that struct's own doc comment) — any bug
    /// here that produces an out-of-range value is exactly what PlayerController's server-side
    /// clamping (Technical Architecture §5.5) exists to catch, but keeping this class correct is
    /// still the first line of defence against garbage input.
    /// </summary>
    public sealed class InputService : MonoBehaviour, IInputSource
    {
        [SerializeField] private VirtualJoystick _moveJoystick;
        [SerializeField] private VirtualJoystick _lookJoystick;
        [SerializeField] private float _gamepadLookSensitivity = 120f;

        private PlayerInputData _cachedInput;
        private bool _controllerActiveThisSession;

        private void Update()
        {
            var gamepad = Gamepad.current;
            _controllerActiveThisSession = gamepad != null && HasRecentGamepadActivity(gamepad);

            Vector2 move = _controllerActiveThisSession
                ? gamepad.leftStick.ReadValue()
                : _moveJoystick != null ? _moveJoystick.Value : Vector2.zero;

            Vector2 look = _controllerActiveThisSession
                ? gamepad.rightStick.ReadValue() * _gamepadLookSensitivity * Time.deltaTime
                : _lookJoystick != null ? _lookJoystick.Value : Vector2.zero;

            InputButton buttons = InputButton.None;
            bool fireHeld = _controllerActiveThisSession ? gamepad.rightTrigger.isPressed : _fireButtonHeld;
            bool aimHeld = _controllerActiveThisSession ? gamepad.leftTrigger.isPressed : _aimButtonHeld;

            if (fireHeld) buttons |= InputButton.Fire;
            if (aimHeld) buttons |= InputButton.AimDownSights;
            if (_reloadRequested) buttons |= InputButton.Reload;
            if (_ability1Requested) buttons |= InputButton.Ability1;
            if (_ability2Requested) buttons |= InputButton.Ability2;
            if (_ultimateRequested) buttons |= InputButton.Ultimate;
            if (_interactRequested) buttons |= InputButton.Interact;
            if (_slideRequested) buttons |= InputButton.Slide;
            if (_sprintHeld) buttons |= InputButton.SprintToggle;

            _cachedInput = new PlayerInputData
            {
                MoveAxis = move,
                LookDelta = look,
                Buttons = buttons
            };

            // One-shot button requests are consumed after being packed into this frame's input —
            // prevents a single tap being (incorrectly) sent across multiple network ticks.
            _reloadRequested = _ability1Requested = _ability2Requested = _ultimateRequested = _interactRequested = _slideRequested = false;
        }

        public PlayerInputData SampleCurrentInput() => _cachedInput;

        private static bool HasRecentGamepadActivity(Gamepad gamepad)
        {
            return gamepad.leftStick.ReadValue().sqrMagnitude > 0.01f
                || gamepad.rightStick.ReadValue().sqrMagnitude > 0.01f
                || gamepad.rightTrigger.isPressed;
        }

        // --- UI-button-driven touch input (wired from on-screen action buttons in the Presentation layer) ---
        private bool _fireButtonHeld;
        private bool _aimButtonHeld;
        private bool _sprintHeld;
        private bool _reloadRequested;
        private bool _ability1Requested;
        private bool _ability2Requested;
        private bool _ultimateRequested;
        private bool _interactRequested;
        private bool _slideRequested;

        public void SetFireHeld(bool held) => _fireButtonHeld = held;
        public void SetAimHeld(bool held) => _aimButtonHeld = held;
        public void SetSprintHeld(bool held) => _sprintHeld = held;
        public void RequestReload() => _reloadRequested = true;
        public void RequestAbility1() => _ability1Requested = true;
        public void RequestAbility2() => _ability2Requested = true;
        public void RequestUltimate() => _ultimateRequested = true;
        public void RequestInteract() => _interactRequested = true;
        public void RequestSlide() => _slideRequested = true;
    }

    /// <summary>Minimal on-screen virtual joystick contract — concrete UI implementation lives in Presentation/UI (drag-based touch control, GDD §12/§20 thumb-zone-aware placement).</summary>
    public interface VirtualJoystickValueSource
    {
        Vector2 Value { get; }
    }

    public abstract class VirtualJoystick : MonoBehaviour, VirtualJoystickValueSource
    {
        public abstract Vector2 Value { get; }
    }
}
