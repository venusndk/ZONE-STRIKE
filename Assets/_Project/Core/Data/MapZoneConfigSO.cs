using System;
using UnityEngine;

namespace ZoneStrike.Core.Data
{
    /// <summary>
    /// Architecture role: single source of truth for the zone shrink schedule (GDD §11). Read
    /// exclusively by <c>ZoneManager</c> on the server — clients never make zone-damage
    /// decisions locally, they only render the ring visualisation from server-replicated state.
    /// This is deliberately the one asset most likely to be re-tuned after Phase 3 Week 11's
    /// playtest (GDD §27 flags the ring timings as an unvalidated hypothesis) — because it is a
    /// single data asset and not scattered constants in code, that retuning is a data change,
    /// not a code change.
    ///
    /// Optimisation notes: the ring array is read once at match start and cached by
    /// ZoneManager; no per-frame asset lookups.
    ///
    /// Extension points: add/remove rings by resizing <see cref="Rings"/> — ZoneManager iterates
    /// the array generically and does not hardcode a ring count.
    ///
    /// Networking considerations: this asset is never itself replicated — every client and the
    /// server load the same build-time asset, so only the current ring *index* and shrink
    /// *progress* (a float 0..1) need to be sent over the network (Technical Architecture §5.3
    /// treats zone state as low-bandwidth, on-change-only replication).
    /// </summary>
    [CreateAssetMenu(menuName = "ZoneStrike/Data/Map Zone Config", fileName = "ZoneConfig_")]
    public sealed class MapZoneConfigSO : ScriptableObject
    {
        [Serializable]
        public struct RingStage
        {
            [Tooltip("Seconds the ring holds at this radius before shrinking to the next stage.")]
            [Min(0f)] public float HoldDurationSeconds;
            [Tooltip("Seconds the shrink animation/transition to the next ring takes.")]
            [Min(0f)] public float ShrinkDurationSeconds;
            [Tooltip("Damage-per-second applied to players outside the ring during and after this stage's shrink.")]
            [Min(0f)] public float DamagePerSecond;
            [Tooltip("Ring radius in meters at the END of this stage's shrink.")]
            [Min(0f)] public float EndRadiusMeters;
        }

        [SerializeField] private float _mapRadiusMeters = 225f;
        [SerializeField] private float _dropPhaseDurationSeconds = 30f;
        [SerializeField] private RingStage[] _rings;
        [SerializeField] private float _suddenDeathDamagePerSecond = 12f;
        [SerializeField] private float _suddenDeathEscalationIntervalSeconds = 30f;
        [SerializeField] private float _suddenDeathEscalationStep = 12f;
        [SerializeField] private float _hardCapSeconds = 600f; // 10:00

        public float MapRadiusMeters => _mapRadiusMeters;
        public float DropPhaseDurationSeconds => _dropPhaseDurationSeconds;
        public RingStage[] Rings => _rings;
        public float SuddenDeathDamagePerSecond => _suddenDeathDamagePerSecond;
        public float SuddenDeathEscalationIntervalSeconds => _suddenDeathEscalationIntervalSeconds;
        public float SuddenDeathEscalationStep => _suddenDeathEscalationStep;
        public float HardCapSeconds => _hardCapSeconds;
    }
}
