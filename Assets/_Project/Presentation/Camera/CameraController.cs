using Cinemachine;
using UnityEngine;
using ZoneStrike.Gameplay.Movement;

namespace ZoneStrike.Presentation.Camera
{
    /// <summary>
    /// Architecture role: Presentation-layer, local-only third-person combat camera (GDD §12),
    /// built on Cinemachine per the Technical Architecture §8 package selection. Deliberately
    /// has zero networking awareness — it is spawned and enabled only on the input-authority
    /// client for its own <see cref="PlayerController"/>, never on remote/proxy player
    /// instances, so it never needs a [Networked] property of its own.
    ///
    /// Optimisation notes: uses a single persistent CinemachineVirtualCamera rather than
    /// blending between multiple cameras per state (spectator mode swaps priority to a separate,
    /// pre-existing spectator vcam instead of instantiating one) — avoids GC churn from runtime
    /// vcam creation/destruction during a match.
    ///
    /// Extension points: <see cref="ApplyRecoilKick"/> is the explicit hook WeaponSystem calls
    /// into on a confirmed shot — new weapon feel (e.g. a heavier DMR kick) is tuned via
    /// WeaponDefinitionSO-driven parameters passed into this method, not by this class knowing
    /// about weapon types.
    ///
    /// Networking considerations: none directly. Reads <see cref="PlayerController"/>'s public,
    /// already-reconciled transform each frame — it never reads or writes [Networked] state
    /// itself, keeping camera jitter fixes a purely local/visual concern that can't affect
    /// simulation correctness.
    /// </summary>
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera _combatCamera;
        [SerializeField] private CinemachineVirtualCamera _spectatorCamera;
        [SerializeField] private float _recoilRecoverySpeed = 8f;
        [SerializeField] private float _touchLookSensitivity = 0.15f;

        private PlayerController _target;
        private Vector3 _recoilOffset;

        public void BindToLocalPlayer(PlayerController target)
        {
            _target = target;
            if (_combatCamera != null)
            {
                _combatCamera.Follow = target.transform;
                _combatCamera.LookAt = target.transform;
                _combatCamera.Priority = 10;
            }
            if (_spectatorCamera != null)
            {
                _spectatorCamera.Priority = 0;
            }
        }

        /// <summary>Called by UIManager/spectator flow when the local player is eliminated (GDD §9).</summary>
        public void EnterSpectatorMode(Transform spectateTarget)
        {
            if (_spectatorCamera == null) return;
            _spectatorCamera.Follow = spectateTarget;
            _spectatorCamera.LookAt = spectateTarget;
            _spectatorCamera.Priority = 10;
            if (_combatCamera != null) _combatCamera.Priority = 0;
        }

        /// <summary>Extension point for WeaponSystem: apply a small, self-recovering camera kick on a confirmed shot.</summary>
        public void ApplyRecoilKick(float verticalKick, float horizontalKick)
        {
            _recoilOffset += new Vector3(horizontalKick, verticalKick, 0f);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Recoil recovers toward zero every frame — purely cosmetic, never affects the
            // networked aim direction sent to the server via PlayerInputData.
            _recoilOffset = Vector3.Lerp(_recoilOffset, Vector3.zero, Time.deltaTime * _recoilRecoverySpeed);
        }

        /// <summary>Touch-drag look input, converted to a look-delta suitable for InputService to feed into PlayerInputData.</summary>
        public Vector2 ConvertTouchDragToLookDelta(Vector2 rawDragDelta) => rawDragDelta * _touchLookSensitivity;
    }
}
