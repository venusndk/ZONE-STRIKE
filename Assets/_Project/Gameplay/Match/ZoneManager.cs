using Fusion;
using UnityEngine;
using ZoneStrike.Core.Data;
using ZoneStrike.Core.EventChannels;
using ZoneStrike.Gameplay.Combat;

namespace ZoneStrike.Gameplay.Match
{
    /// <summary>
    /// Architecture role: server-authoritative shrinking-zone simulation (GDD §11), driven
    /// entirely by <see cref="MapZoneConfigSO"/> — the single source of truth this class was
    /// specifically designed around so the Phase 3 Week 11 playtest's zone-timing findings can be
    /// applied as a data change to that asset, never a code change here.
    ///
    /// Optimisation notes: only players outside the current ring are damage-ticked (an O(n)
    /// distance check against 8 players, negligible cost); ring visual state is replicated to
    /// clients via <see cref="ZoneRingEventChannelSO"/> only on stage change, not every tick,
    /// per the low-bandwidth zone-state budget (Technical Architecture §5.3).
    ///
    /// Extension points: <see cref="MapZoneConfigSO.Rings"/> is a plain array iterated
    /// generically — adding/removing/re-timing rings for balance is purely a designer-facing
    /// data change.
    ///
    /// Networking considerations: zone damage is applied directly to each affected player's
    /// <see cref="IDamageable"/> under server authority every tick a player is outside the ring
    /// — a client cannot claim to be inside the ring to avoid damage; the server computes the
    /// distance check itself from each player's server-authoritative (reconciled) position.
    /// </summary>
    public sealed class ZoneManager : NetworkBehaviour
    {
        [SerializeField] private MapZoneConfigSO _config;
        [SerializeField] private MatchManager _matchManager;
        [SerializeField] private ZoneRingEventChannelSO _ringChangedChannel;
        [SerializeField] private Vector3 _zoneCenter;

        [Networked] private int RingIndex { get; set; }
        [Networked] private TickTimer StageTimer { get; set; }
        [Networked] private NetworkBool IsShrinking { get; set; }
        [Networked] private float CurrentRadius { get; set; }
        [Networked] private TickTimer SuddenDeathEscalationTimer { get; set; }
        [Networked] private float SuddenDeathDamagePerSecond { get; set; }

        public float DropPhaseDurationSeconds => _config != null ? _config.DropPhaseDurationSeconds : 30f;
        public float HardCapSeconds => _config != null ? _config.HardCapSeconds : 600f;
        public float CurrentDamagePerSecond => RingIndex < (_config?.Rings.Length ?? 0)
            ? _config.Rings[RingIndex].DamagePerSecond
            : SuddenDeathDamagePerSecond;

        public void ServerBeginRingSchedule()
        {
            if (!HasStateAuthority || _config == null || _config.Rings.Length == 0) return;

            RingIndex = 0;
            CurrentRadius = _config.MapRadiusMeters;
            IsShrinking = false;
            StageTimer = TickTimer.CreateFromSeconds(Runner, _config.Rings[0].HoldDurationSeconds);
            BroadcastRingState();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _config == null) return;

            if (RingIndex < _config.Rings.Length)
            {
                TickRingSchedule();
            }
            else
            {
                TickSuddenDeath();
            }

            ApplyZoneDamageToPlayersOutsideRing();
        }

        private void TickRingSchedule()
        {
            if (!StageTimer.Expired(Runner)) return;

            var stage = _config.Rings[RingIndex];

            if (!IsShrinking)
            {
                IsShrinking = true;
                StageTimer = TickTimer.CreateFromSeconds(Runner, stage.ShrinkDurationSeconds);
                BroadcastRingState();
            }
            else
            {
                CurrentRadius = stage.EndRadiusMeters;
                RingIndex++;
                IsShrinking = false;

                if (RingIndex >= _config.Rings.Length)
                {
                    // Ring schedule exhausted — final zone reached (GDD §4/§11).
                    SuddenDeathDamagePerSecond = _config.SuddenDeathDamagePerSecond;
                    SuddenDeathEscalationTimer = TickTimer.CreateFromSeconds(Runner, _config.SuddenDeathEscalationIntervalSeconds);
                    _matchManager?.ServerNotifyFinalRingReached();
                }
                else
                {
                    StageTimer = TickTimer.CreateFromSeconds(Runner, _config.Rings[RingIndex].HoldDurationSeconds);
                }

                BroadcastRingState();
            }
        }

        private void TickSuddenDeath()
        {
            if (SuddenDeathEscalationTimer.Expired(Runner))
            {
                SuddenDeathDamagePerSecond += _config.SuddenDeathEscalationStep;
                SuddenDeathEscalationTimer = TickTimer.CreateFromSeconds(Runner, _config.SuddenDeathEscalationIntervalSeconds);
                BroadcastRingState();
            }
        }

        private void ApplyZoneDamageToPlayersOutsideRing()
        {
            float damagePerSecond = CurrentDamagePerSecond;
            if (damagePerSecond <= 0f) return;

            foreach (var playerObject in Runner.ActivePlayers)
            {
                if (!Runner.TryGetPlayerObject(playerObject, out var networkObject)) continue;
                if (networkObject == null) continue;

                float distance = Vector3.Distance(networkObject.transform.position, _zoneCenter);
                if (distance <= CurrentRadius) continue;

                if (networkObject.TryGetComponent(out IDamageable damageable))
                {
                    damageable.ApplyServerAuthoritativeDamage(damagePerSecond * Runner.DeltaTime, instigatorPlayerId: -1, isHeadshot: false);
                }
            }
        }

        private void BroadcastRingState()
        {
            _ringChangedChannel?.Raise(new ZoneRingChangedPayload(RingIndex, CurrentRadius, CurrentDamagePerSecond, IsShrinking));
        }
    }
}
