using Fusion;
using UnityEngine;
using ZoneStrike.Core.Data;
using ZoneStrike.Core.EventChannels;
using ZoneStrike.Core.Interfaces;
using ZoneStrike.Gameplay.Combat;

namespace ZoneStrike.Gameplay.Weapons
{
    /// <summary>
    /// Architecture role: server-authoritative weapon firing and hit resolution (GDD §15/§18),
    /// implementing the exact client-predict → server-validate → reconcile sequence documented
    /// as a diagram in Technical Architecture §12.1. This is the single most
    /// competitive-integrity-critical system in the project — every other anti-cheat mitigation
    /// (Technical Architecture §5.5) assumes this system never trusts a client-reported hit or
    /// damage value.
    ///
    /// Optimisation notes: fire requests are sent as a single reliable RPC per shot (not a
    /// per-tick networked property), keeping bandwidth in line with the Technical Architecture
    /// §5.3 budget ("reliable RPCs only for discrete events, never per-frame"). Local predicted
    /// VFX/SFX fire immediately client-side on input (via event channel, decoupled from network
    /// round-trip) so the shooter never perceives input latency, even though the authoritative
    /// hit confirmation arrives a tick or two later.
    ///
    /// Extension points: <see cref="ResolveHit"/> is intentionally the single seam a future
    /// weapon-type addition (e.g. a projectile/rocket archetype instead of hitscan) would
    /// override — the ammo/cooldown/RPC plumbing above it is archetype-agnostic.
    ///
    /// Networking considerations: hit validation uses Fusion's server-side lag-compensated
    /// history (rewinding to the shooter's perceived tick, bounded to a max window — Technical
    /// Architecture §5.2) before resolving a raycast, so a shot that visually landed on the
    /// shooter's screen is judged fairly even under real network latency. Ammo and cooldown are
    /// [Networked] properties mutated only under <see cref="HasStateAuthority"/>; a client's
    /// locally-predicted ammo decrement is cosmetic and is overwritten by the authoritative value
    /// every reconciliation (Technical Architecture §5.5 "memory editing" mitigation — editing
    /// the client-local ammo value has no authoritative effect).
    ///
    /// NOTE: written against the Fusion 2 NetworkBehaviour/RPC pattern and Fusion's
    /// LagCompensation raycast API; verify exact call signatures against the installed SDK.
    /// </summary>
    public sealed class WeaponSystem : NetworkBehaviour
    {
        [SerializeField] private WeaponDefinitionSO _equippedWeapon;
        [SerializeField] private Transform _muzzlePoint;
        [SerializeField] private LayerMask _hitLayers;
        [SerializeField] private VoidEventChannelSO _localFireFeedbackChannel;

        [Networked] private int CurrentAmmo { get; set; }
        [Networked] private TickTimer ReloadTimer { get; set; }
        [Networked] private TickTimer FireCooldown { get; set; }

        public WeaponDefinitionSO EquippedWeapon => _equippedWeapon;
        public int CurrentAmmoValue => CurrentAmmo;

        public override void Spawned()
        {
            if (HasStateAuthority && _equippedWeapon != null)
            {
                CurrentAmmo = _equippedWeapon.MagazineSize;
            }
        }

        /// <summary>
        /// Called from PlayerController's input-processing path (input-authority client only)
        /// the instant Fire is pressed — fires immediate local feedback and sends the
        /// authoritative fire request to the server in the same frame.
        /// </summary>
        public void RequestFire(Vector3 origin, Vector3 direction)
        {
            if (!HasInputAuthority) return;

            // Immediate, purely cosmetic local feedback — never affects authoritative state.
            _localFireFeedbackChannel?.Raise(default);

            RPC_RequestFire(origin, direction, Runner.Tick);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestFire(Vector3 origin, Vector3 direction, int clientTick)
        {
            if (_equippedWeapon == null) return;
            if (!FireCooldown.ExpiredOrNotRunning(Runner)) return; // server-authoritative rate-of-fire gate
            if (CurrentAmmo <= 0) return;
            if (!ReloadTimer.ExpiredOrNotRunning(Runner)) return;

            CurrentAmmo--;
            FireCooldown = TickTimer.CreateFromSeconds(Runner, _equippedWeapon.FireIntervalSeconds);

            ResolveHit(origin, direction, clientTick);
        }

        /// <summary>
        /// Server-side hit resolution using lag-compensated history. Extension point for
        /// alternate weapon archetypes (projectile-based) to override via composition.
        /// </summary>
        private void ResolveHit(Vector3 origin, Vector3 direction, int clientTick)
        {
            // Lag compensation: rewind server-side colliders to the shooter's perceived tick
            // before testing the raycast, bounded to Fusion's configured max rewind window
            // (Technical Architecture §5.2). Pseudocode-accurate to Fusion's LagCompensation API;
            // verify exact call against the installed SDK version.
            var lagCompHits = new HitCollisionInfo[8];
            int hitCount = Runner.LagCompensation.RaycastAll(
                origin, direction, _equippedWeapon.EffectiveRangeMeters,
                Object.InputAuthority, lagCompHits, _hitLayers);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = lagCompHits[i];
                if (hit.Hitbox == null) continue;

                var targetHealth = hit.Hitbox.transform.root.GetComponentInParent<IDamageable>();
                if (targetHealth == null) continue;

                bool isHeadshot = hit.Hitbox.CompareTag("Head");
                float damage = _equippedWeapon.DamagePerHit * (isHeadshot ? _equippedWeapon.HeadshotMultiplier : 1f);

                targetHealth.ApplyServerAuthoritativeDamage(damage, Object.InputAuthority.PlayerId, isHeadshot);

                RPC_NotifyHitConfirmed(isHeadshot);
                return; // hitscan: first valid hit along the ray only
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RPC_NotifyHitConfirmed(bool isHeadshot)
        {
            // Presentation layer (hit-marker UI/SFX) subscribes to this via a local event raised
            // here — kept as a direct RPC (not an event-channel round trip) since it targets only
            // the shooting client.
        }

        public void RequestReload()
        {
            if (!HasInputAuthority) return;
            RPC_RequestReload();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestReload()
        {
            if (_equippedWeapon == null || CurrentAmmo >= _equippedWeapon.MagazineSize) return;
            if (!ReloadTimer.ExpiredOrNotRunning(Runner)) return;

            ReloadTimer = TickTimer.CreateFromSeconds(Runner, _equippedWeapon.ReloadSeconds);
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && ReloadTimer.Expired(Runner) && CurrentAmmo < (_equippedWeapon?.MagazineSize ?? 0))
            {
                CurrentAmmo = _equippedWeapon.MagazineSize;
                ReloadTimer = TickTimer.None;
            }
        }

        /// <summary>Swaps the equipped weapon definition — called by InventorySystem on a validated pickup/equip.</summary>
        public void EquipWeapon(WeaponDefinitionSO weapon)
        {
            if (!HasStateAuthority) return;
            _equippedWeapon = weapon;
            CurrentAmmo = weapon.MagazineSize;
            ReloadTimer = TickTimer.None;
        }
    }
}
