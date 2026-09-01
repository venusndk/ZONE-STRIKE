using System;

namespace ZoneStrike.Core.Interfaces
{
    /// <summary>
    /// Architecture role: the single struct that crosses the client→server boundary every
    /// network tick as Photon Fusion's polled input (fed to Fusion via
    /// <c>NetworkRunner.GetInput&lt;PlayerInputData&gt;()</c> inside PlayerController's
    /// FixedUpdateNetwork). Deliberately the *only* channel through which a client can affect
    /// its own player's simulated state — everything else the server treats as untrusted display
    /// data (Technical Architecture §5.5 anti-cheat: "all gameplay-critical values live
    /// server-side").
    ///
    /// Optimisation notes: kept as a flat, blittable struct of primitives (no reference types,
    /// no collections) — Fusion serializes networked input every tick, so its size directly sets
    /// the input-channel bandwidth cost; button state is packed into a bitmask rather than one
    /// bool per action to keep this small.
    ///
    /// Extension points: add a new action by adding a bit to <see cref="InputButton"/> and a
    /// corresponding helper on <see cref="PlayerInputData"/> — do not add new struct fields for
    /// simple on/off actions, only for continuous values (the two Vector2s already present).
    ///
    /// Networking considerations: this struct IS the network protocol for player input — any
    /// change to its layout is a wire-format change and must be coordinated across client/server
    /// builds deployed together (never partially roll out a client vs. server input-struct change).
    /// </summary>
    [Flags]
    public enum InputButton : ushort
    {
        None = 0,
        Fire = 1 << 0,
        AimDownSights = 1 << 1,
        Reload = 1 << 2,
        Ability1 = 1 << 3,
        Ability2 = 1 << 4,
        Ultimate = 1 << 5,
        Interact = 1 << 6,
        Slide = 1 << 7,
        SprintToggle = 1 << 8
    }

    public struct PlayerInputData
    {
        public UnityEngine.Vector2 MoveAxis;
        public UnityEngine.Vector2 LookDelta;
        public InputButton Buttons;

        public readonly bool IsPressed(InputButton button) => (Buttons & button) != 0;
    }

    /// <summary>
    /// Architecture role: Presentation/Infrastructure-layer abstraction over the concrete input
    /// method (touch virtual sticks vs. MFi/Android gamepad vs. Editor keyboard-mouse for
    /// development) — implemented by <c>InputService</c>. Gameplay code (PlayerController) never
    /// references the Input System package directly; it asks this interface for the current
    /// frame's <see cref="PlayerInputData"/>, keeping input-method-specific code entirely out of
    /// the Gameplay layer per the Technical Architecture §3 layering rule.
    /// </summary>
    public interface IInputSource
    {
        PlayerInputData SampleCurrentInput();
    }
}
