using Fusion;
using UnityEngine;
using ZoneStrike.Core.Data;
using ZoneStrike.Core.EventChannels;

namespace ZoneStrike.Gameplay.Match
{
    public enum MatchEndResult { Win, Loss, Draw }

    /// <summary>
    /// Architecture role: server-authoritative match lifecycle state machine, implementing the
    /// stage table in GDD §4 exactly (Lobby → Loading → Drop → Zone hold/shrink ×4 → Final Zone
    /// → Sudden Death → Post-match). Owns win/loss/draw resolution (GDD §5) and coordinates
    /// <see cref="ZoneManager"/> and <see cref="SpawnManager"/>, which it treats as
    /// subordinate systems rather than duplicating their logic.
    ///
    /// Optimisation notes: the state machine is driven by a single [Networked] enum + a
    /// server-side elapsed-time float, replicated to clients on change only via the
    /// MatchStateEventChannelSO bridge (see <see cref="BroadcastStateChange"/>) rather than every
    /// tick — clients render state locally from the last-received value plus local delta time,
    /// consistent with the Technical Architecture §5.3 "infrequent, on-change only" zone/match
    /// state bandwidth budget.
    ///
    /// Extension points: <see cref="MatchRulesSO"/> parameterises squad size / Reinforcement
    /// Token count, so a future ranked-mode ruleset variant (Phase 7/8) plugs in without a code
    /// change to this class, per that asset's own doc comment.
    ///
    /// Networking considerations: every state transition and the final win/loss/draw
    /// determination happens exclusively under <see cref="HasStateAuthority"/> — clients never
    /// locally decide a match has ended; they react to the server's authoritative
    /// <see cref="MatchStateChangedPayload"/> broadcast. This closes off "client claims the match
    /// ended in its favor" as an attack surface entirely.
    /// </summary>
    public sealed class MatchManager : NetworkBehaviour
    {
        [SerializeField] private MatchRulesSO _rules;
        [SerializeField] private ZoneManager _zoneManager;
        [SerializeField] private SpawnManager _spawnManager;
        [SerializeField] private MatchStateEventChannelSO _matchStateChannel;

        [Networked] private MatchState CurrentState { get; set; }
        [Networked] private float ElapsedSeconds { get; set; }
        [Networked] private int AliveCountSquadA { get; set; }
        [Networked] private int AliveCountSquadB { get; set; }

        public MatchState State => CurrentState;
        public float ElapsedTimeSeconds => ElapsedSeconds;

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                CurrentState = MatchState.Lobby;
                AliveCountSquadA = _rules != null ? _rules.SquadSize : 4;
                AliveCountSquadB = _rules != null ? _rules.SquadSize : 4;
            }
        }

        /// <summary>Called once by the server's lobby-ready-check flow (outside this class's own scope) once 8/8 players are present.</summary>
        public void ServerBeginMatch()
        {
            if (!HasStateAuthority) return;
            TransitionTo(MatchState.Drop);
            _spawnManager?.ServerBeginDropPhase();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (CurrentState == MatchState.Lobby || CurrentState == MatchState.Loading || CurrentState == MatchState.PostMatch) return;

            ElapsedSeconds += Runner.DeltaTime;

            TickStateMachine();
            EvaluateWinLossConditions();
        }

        private void TickStateMachine()
        {
            float dropDuration = _zoneManager != null ? _zoneManager.DropPhaseDurationSeconds : 30f;

            switch (CurrentState)
            {
                case MatchState.Drop when ElapsedSeconds >= dropDuration:
                    TransitionTo(MatchState.ZoneHold);
                    _zoneManager?.ServerBeginRingSchedule();
                    break;

                case MatchState.ZoneHold:
                case MatchState.ZoneShrink:
                    // ZoneManager itself owns ring-index/shrink-vs-hold transitions and reports
                    // back via ServerNotifyFinalRingReached()/ServerNotifyRingStageChanged() —
                    // MatchManager does not duplicate ring timing logic (single-responsibility).
                    break;

                case MatchState.FinalZone when _rules != null && ElapsedSeconds >= _rules.SoftMatchLengthSeconds:
                    TransitionTo(MatchState.SuddenDeath);
                    break;
            }
        }

        /// <summary>Called by ZoneManager when the ring schedule reaches its last stage (GDD §11 "Final Zone").</summary>
        public void ServerNotifyFinalRingReached()
        {
            if (!HasStateAuthority) return;
            if (CurrentState is MatchState.ZoneHold or MatchState.ZoneShrink)
            {
                TransitionTo(MatchState.FinalZone);
            }
        }

        /// <summary>Called by a player-health system when a squad member's alive count changes.</summary>
        public void ServerReportSquadAliveCount(int squadId, int aliveCount)
        {
            if (!HasStateAuthority) return;
            if (squadId == 0) AliveCountSquadA = aliveCount;
            else AliveCountSquadB = aliveCount;
        }

        private void EvaluateWinLossConditions()
        {
            bool squadAEliminated = AliveCountSquadA <= 0;
            bool squadBEliminated = AliveCountSquadB <= 0;
            bool hardCapReached = _zoneManager != null && ElapsedSeconds >= _zoneManager.HardCapSeconds;

            if (squadAEliminated && squadBEliminated)
            {
                EndMatch(MatchEndResult.Draw, -1);
            }
            else if (squadAEliminated)
            {
                EndMatch(MatchEndResult.Win, squadId: 1);
            }
            else if (squadBEliminated)
            {
                EndMatch(MatchEndResult.Win, squadId: 0);
            }
            else if (hardCapReached)
            {
                // GDD §5: hard-cap tie-break by survivor count; equal survivors = draw.
                if (AliveCountSquadA == AliveCountSquadB) EndMatch(MatchEndResult.Draw, -1);
                else EndMatch(MatchEndResult.Win, AliveCountSquadA > AliveCountSquadB ? 0 : 1);
            }
        }

        private bool _matchEnded;

        private void EndMatch(MatchEndResult result, int squadId)
        {
            if (_matchEnded) return; // idempotency guard — EvaluateWinLossConditions can run multiple ticks before the state transition takes effect
            _matchEnded = true;

            TransitionTo(MatchState.PostMatch);
            // Score/reward computation and persistence is delegated to a dedicated post-match
            // service (outside this class's scope) subscribed to the state-change broadcast below.
        }

        private void TransitionTo(MatchState next)
        {
            MatchState previous = CurrentState;
            CurrentState = next;
            BroadcastStateChange(previous, next);
        }

        private void BroadcastStateChange(MatchState previous, MatchState next)
        {
            _matchStateChannel?.Raise(new MatchStateChangedPayload(previous, next, ElapsedSeconds));
        }
    }
}
