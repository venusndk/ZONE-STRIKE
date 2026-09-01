namespace ZoneStrike.Core.EventChannels
{
    public enum MatchState
    {
        Lobby,
        Loading,
        Drop,
        ZoneHold,
        ZoneShrink,
        FinalZone,
        SuddenDeath,
        PostMatch
    }

    /// <summary>Payload for <see cref="MatchStateEventChannelSO"/> — kept as a small readonly struct to avoid per-event GC allocation.</summary>
    public readonly struct MatchStateChangedPayload
    {
        public readonly MatchState PreviousState;
        public readonly MatchState NewState;
        public readonly float ServerTimeSeconds;

        public MatchStateChangedPayload(MatchState previous, MatchState next, float serverTime)
        {
            PreviousState = previous;
            NewState = next;
            ServerTimeSeconds = serverTime;
        }
    }

    public enum DownedReason { CombatDamage, ZoneDamage }

    /// <summary>Payload for <see cref="PlayerDownedEventChannelSO"/>.</summary>
    public readonly struct PlayerDownedPayload
    {
        public readonly int PlayerRefId;
        public readonly int SquadId;
        public readonly DownedReason Reason;
        public readonly int InstigatorPlayerRefId;

        public PlayerDownedPayload(int playerRefId, int squadId, DownedReason reason, int instigatorPlayerRefId)
        {
            PlayerRefId = playerRefId;
            SquadId = squadId;
            Reason = reason;
            InstigatorPlayerRefId = instigatorPlayerRefId;
        }
    }

    /// <summary>Payload for <see cref="ZoneRingEventChannelSO"/>.</summary>
    public readonly struct ZoneRingChangedPayload
    {
        public readonly int RingIndex;
        public readonly float CurrentRadiusMeters;
        public readonly float DamagePerSecond;
        public readonly bool IsShrinking;

        public ZoneRingChangedPayload(int ringIndex, float currentRadiusMeters, float damagePerSecond, bool isShrinking)
        {
            RingIndex = ringIndex;
            CurrentRadiusMeters = currentRadiusMeters;
            DamagePerSecond = damagePerSecond;
            IsShrinking = isShrinking;
        }
    }
}
