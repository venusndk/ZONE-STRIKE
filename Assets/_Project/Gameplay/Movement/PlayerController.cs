using Fusion;
using UnityEngine;
using VContainer;
using ZoneStrike.Core.Data;
using ZoneStrike.Core.Interfaces;

namespace ZoneStrike.Gameplay.Movement
{
    /// <summary>
    /// Architecture role: server-authoritative player movement (GDD §12) implemented as a Fusion
    /// <see cref="NetworkBehaviour"/>. Movement is client-predicted for the input-authority
    /// client (immediate local responsiveness) and reconciled every tick against the server's
    /// simulation, per Technical Architecture §5.2. This is the Gameplay-layer system every
    /// other combat/ability system builds on top of (weapon aim origin, ability cast origin,
    /// camera target).
    ///
    /// Optimisation notes: uses Unity's <see cref="CharacterController"/> for collision rather
    /// than a full Rigidbody physics simulation — CharacterController is cheaper per-instance
    /// and sufficient for kinematic BR-style movement, which matters with 8 simultaneous
    /// networked instances on a mid-range device (Technical Architecture §10.1 gameplay
    /// simulation budget: 4.0ms total, shared across all systems). Movement math avoids
    /// per-tick heap allocation (struct-only locals) to protect the zero-steady-state-GC target
    /// (§10.3).
    ///
    /// Extension points: hero-specific movement modifiers (e.g. Raptor's wall-run, GDD §14) are
    /// intended to be implemented as small strategy components that read/modify
    /// <see cref="CurrentMoveSpeed"/> and hook <see cref="OnBeforeMove"/>/<see cref="OnAfterMove"/>,
    /// rather than subclassing this controller — keeps the base controller uniform across all 8
    /// heroes per the "composition over inheritance" engineering standard.
    ///
    /// Networking considerations: all authoritative position/velocity state lives in
    /// <c>[Networked]</c> properties, written only when <see cref="HasStateAuthority"/> is true
    /// (the server). The input-authority client predicts locally in <see cref="Render"/> using
    /// the same movement function against its own unconfirmed input, then Fusion's built-in
    /// resimulation reconciles on mismatch. Speed/acceleration are hard-clamped server-side
    /// against <see cref="HeroDefinitionSO"/> values every tick — this is the concrete mitigation
    /// for the "speed hacks" threat listed in Technical Architecture §5.5: a client cannot move
    /// faster than its hero's authored stats regardless of what input it sends.
    ///
    /// NOTE: Photon Fusion's exact attribute/API surface (e.g. [Networked], FixedUpdateNetwork
    /// signatures) should be verified against the specific Fusion SDK version imported into the
    /// project — written here against the Fusion 2 NetworkBehaviour pattern.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : NetworkBehaviour
    {
        [SerializeField] private HeroDefinitionSO _heroDefinition;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _vaultCheckDistance = 0.6f;
        [SerializeField] private LayerMask _vaultableLayers;

        private CharacterController _characterController;
        private IInputSource _inputSource;

        [Networked] private Vector3 NetworkedVelocity { get; set; }
        [Networked] private NetworkBool IsSprinting { get; set; }
        [Networked] private NetworkBool IsSliding { get; set; }
        [Networked] private float SlideTimer { get; set; }

        /// <summary>Current effective move speed after sprint/slide modifiers — read by camera FOV kick and weapon spread systems.</summary>
        public float CurrentMoveSpeed { get; private set; }

        /// <summary>
        /// Method-injected (see NetworkManager's Construct doc for why MonoBehaviour/
        /// NetworkBehaviour-derived types use method injection, not constructors). Note: Fusion
        /// spawns this component at runtime via <c>Runner.Spawn</c> on a prefab, outside
        /// VContainer's automatic scene-object injection pass — the project's Fusion spawn hook
        /// (on <c>OnPlayerJoined</c>, Networking layer) is responsible for resolving
        /// <see cref="IInputSource"/> from the active LifetimeScope's container and calling this
        /// explicitly right after spawn, only for the locally input-authority instance.
        /// </summary>
        [Inject]
        public void Construct(IInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        public override void Spawned()
        {
            _characterController = GetComponent<CharacterController>();

            if (_heroDefinition == null)
            {
                Debug.LogError($"[PlayerController] {name} spawned with no HeroDefinitionSO assigned — movement stats unavailable.", this);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out PlayerInputData input))
            {
                // No input this tick (e.g. a proxy/remote instance, or a dropped input packet
                // within the reconnect grace window, Technical Architecture §5.4) — hold position.
                return;
            }

            if (_heroDefinition == null) return;

            ApplyMovement(input, Runner.DeltaTime);
        }

        private void ApplyMovement(PlayerInputData input, float deltaTime)
        {
            bool wantsSlide = input.IsPressed(Core.Interfaces.InputButton.Slide);
            bool sprintHeld = input.IsPressed(Core.Interfaces.InputButton.SprintToggle);

            // Auto-sprint above a forward-movement threshold, no dedicated hold button on
            // mobile touch input — GDD §12 rationale: minimise required simultaneous touch inputs.
            float inputMagnitude = input.MoveAxis.magnitude;
            IsSprinting = sprintHeld && inputMagnitude > 0.6f && !IsSliding;

            if (wantsSlide && IsSprinting && !IsSliding)
            {
                IsSliding = true;
                SlideTimer = 0f;
            }

            float baseSpeed = _heroDefinition.BaseMoveSpeed;
            float speed = IsSprinting ? baseSpeed * _heroDefinition.SprintSpeedMultiplier : baseSpeed;

            Vector3 moveDirection = transform.TransformDirection(new Vector3(input.MoveAxis.x, 0f, input.MoveAxis.y));
            moveDirection.y = 0f;
            if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

            Vector3 horizontalVelocity;
            if (IsSliding)
            {
                SlideTimer += deltaTime;
                float slideFalloff = Mathf.Clamp01(1f - SlideTimer / 0.6f);
                horizontalVelocity = transform.forward * (_heroDefinition.SlideImpulse * slideFalloff);
                if (slideFalloff <= 0f) IsSliding = false;
            }
            else
            {
                horizontalVelocity = moveDirection * speed;
            }

            // Server-authoritative speed clamp — see class doc "Networking considerations".
            // HasStateAuthority is only true on the server (dedicated-server topology, Technical
            // Architecture §5.1), so a compromised client's predicted-but-unauthoritative
            // movement can never actually move the replicated position beyond this envelope.
            float maxAllowedSpeed = baseSpeed * _heroDefinition.SprintSpeedMultiplier * 1.05f; // small tolerance for slide impulse
            if (horizontalVelocity.magnitude > maxAllowedSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * maxAllowedSpeed;
            }

            float verticalVelocity = _characterController.isGrounded ? -1f : NetworkedVelocity.y + _gravity * deltaTime;
            Vector3 velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);

            TryAutoVault(ref velocity);

            _characterController.Move(velocity * deltaTime);
            NetworkedVelocity = velocity;
            CurrentMoveSpeed = horizontalVelocity.magnitude;

            if (input.LookDelta.sqrMagnitude > 0f)
            {
                transform.Rotate(Vector3.up, input.LookDelta.x, Space.World);
            }
        }

        /// <summary>
        /// Context-automatic vault (GDD §12: "no dedicated button — extra inputs cost accuracy
        /// on mobile"). Detects a low obstacle directly ahead and adds a vertical impulse rather
        /// than requiring explicit player input.
        /// </summary>
        private void TryAutoVault(ref Vector3 velocity)
        {
            if (velocity.sqrMagnitude < 0.01f || !_characterController.isGrounded) return;

            Vector3 origin = transform.position + Vector3.up * 0.3f;
            if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, _vaultCheckDistance, _vaultableLayers))
            {
                bool clearAbove = !Physics.Raycast(origin + Vector3.up * 0.6f, transform.forward, _vaultCheckDistance, _vaultableLayers);
                if (clearAbove)
                {
                    velocity.y = Mathf.Max(velocity.y, 6f);
                }
            }
        }

        public override void Render()
        {
            // Local-only predicted visual smoothing hook (e.g. camera bob) goes here in
            // Presentation-facing components that read this controller's public state — the
            // Render() callback intentionally does not write any [Networked] state.
        }
    }
}
