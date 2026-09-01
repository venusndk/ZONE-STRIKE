using System;
using UnityEngine;

namespace ZoneStrike.Core.EventChannels
{
    /// <summary>
    /// Architecture role: generic base for ScriptableObject-based event channels (Technical
    /// Architecture §3). A channel is a decoupling seam — Gameplay-layer systems raise events by
    /// calling <see cref="Raise"/> on a channel asset reference, and Presentation-layer systems
    /// (UI, Audio, VFX) subscribe to the same asset reference. Note this is a stricter, opt-in
    /// pattern for *reactive* Presentation systems (UIManager, AudioManager) specifically — the
    /// Technical Architecture §3 dependency graph's solid `Presentation --> Gameplay` arrow
    /// permits direct references too (see CameraController, which legitimately holds a direct
    /// PlayerController reference because a camera rig's job intrinsically requires one). The
    /// one direction that is never allowed, and IS enforced by the asmdef graph, is the reverse:
    /// Gameplay code must never reference Presentation (no `Canvas`/UI Toolkit/Cinemachine types
    /// in Gameplay assemblies) — Gameplay only ever raises an event and never knows who, if
    /// anyone, is listening.
    ///
    /// Optimisation notes: subscriber list uses a plain C# event (not UnityEvent) to avoid
    /// UnityEvent's reflection-based invocation overhead on a per-match-event frequency; channels
    /// are fired dozens of times per match, not per frame, so this is a correctness/clarity
    /// choice more than a hot-path optimisation, but it costs nothing to do properly.
    ///
    /// Extension points: derive a concrete non-generic subclass per payload type (Unity cannot
    /// create asset instances of an open generic type) — see <c>PlayerEventChannelSO</c>,
    /// <c>MatchStateEventChannelSO</c>, <c>VoidEventChannelSO</c> for the pattern.
    ///
    /// Networking considerations: event channels are a purely local (client-side or
    /// server-side-in-process) pub/sub mechanism — they are never a networking transport. A
    /// networked event (e.g. "player downed") is raised locally by the receiving side's own
    /// NetworkBehaviour callback (e.g. a Fusion <c>[Networked]</c> property's OnChanged hook or
    /// an RPC handler), which then raises the local channel to notify Presentation.
    /// </summary>
    /// <typeparam name="T">Event payload type. Use a lightweight struct where possible to avoid GC.</typeparam>
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        private event Action<T> _listeners;

        /// <summary>Number of currently-subscribed listeners — exposed for QA/debug tooling to catch leaked subscriptions.</summary>
        public int ListenerCount { get; private set; }

        public void Raise(T payload)
        {
            _listeners?.Invoke(payload);
        }

        public void Subscribe(Action<T> listener)
        {
            _listeners += listener;
            ListenerCount++;
        }

        public void Unsubscribe(Action<T> listener)
        {
            _listeners -= listener;
            ListenerCount = Mathf.Max(0, ListenerCount - 1);
        }

        /// <summary>
        /// Editor/domain-reload safety: ScriptableObject event channels can retain stale
        /// listener references across Play Mode sessions in the Editor. Called by GameManager on
        /// boot to guarantee a clean slate — without this, a channel-based architecture is prone
        /// to a well-known Unity gotcha of duplicate/leaked subscriptions between play sessions.
        /// </summary>
        public void ClearAllListeners()
        {
            _listeners = null;
            ListenerCount = 0;
        }
    }
}
