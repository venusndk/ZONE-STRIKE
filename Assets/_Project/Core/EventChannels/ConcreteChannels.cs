using UnityEngine;

namespace ZoneStrike.Core.EventChannels
{
    // Concrete, non-generic EventChannelSO<T> subclasses — Unity requires a concrete type to
    // create an asset instance, so each payload type used across the project gets one small
    // subclass here rather than a bespoke channel implementation. See EventChannelSO<T> for the
    // full architecture/optimisation/extension/networking documentation shared by all of these.

    [CreateAssetMenu(menuName = "ZoneStrike/Events/Void Channel", fileName = "Event_")]
    public sealed class VoidEventChannelSO : EventChannelSO<System.ValueTuple> { }

    [CreateAssetMenu(menuName = "ZoneStrike/Events/Match State Channel", fileName = "Event_MatchState")]
    public sealed class MatchStateEventChannelSO : EventChannelSO<MatchStateChangedPayload> { }

    [CreateAssetMenu(menuName = "ZoneStrike/Events/Player Downed Channel", fileName = "Event_PlayerDowned")]
    public sealed class PlayerDownedEventChannelSO : EventChannelSO<PlayerDownedPayload> { }

    [CreateAssetMenu(menuName = "ZoneStrike/Events/Zone Ring Channel", fileName = "Event_ZoneRing")]
    public sealed class ZoneRingEventChannelSO : EventChannelSO<ZoneRingChangedPayload> { }
}
