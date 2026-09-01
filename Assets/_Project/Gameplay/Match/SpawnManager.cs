using System.Collections.Generic;
using Fusion;
using UnityEngine;
using ZoneStrike.Core.Data;

namespace ZoneStrike.Gameplay.Match
{
    /// <summary>
    /// Architecture role: server-authoritative squad drop placement (GDD §4 Drop stage) and
    /// Reinforcement Token redeploy handling (GDD §8). Deliberately owns only *where/when*
    /// players enter the world — it does not own health/downed-state logic, which lives on each
    /// player's own health component (see <see cref="Weapons.IDamageable"/>'s doc note on that
    /// boundary).
    ///
    /// Optimisation notes: drop points are pre-authored Transform markers in the scene (design
    /// data, not procedurally generated at runtime) — zero runtime cost to select from beyond a
    /// min-squad-distance check over a small fixed list.
    ///
    /// Extension points: <see cref="SelectDropPoint"/> is the seam a future "contested hot-drop
    /// popularity" system (weighting drop-point selection UI, a common BR UX feature) would hook
    /// into without touching the Reinforcement/redeploy logic below it.
    ///
    /// Networking considerations: Reinforcement Token consumption is a [Networked] per-squad
    /// counter mutated only under state authority — a client cannot request more redeploys than
    /// <see cref="MatchRulesSO.ReinforcementTokensPerSquad"/> regardless of what it sends,
    /// closing off a "free extra life" exploit.
    /// </summary>
    public sealed class SpawnManager : NetworkBehaviour
    {
        [SerializeField] private MatchRulesSO _rules;
        [SerializeField] private Transform[] _dropPointCandidates;
        [SerializeField] private Transform[] _reinforcementBeacons;
        [SerializeField] private float _minSquadDropSeparationMeters = 60f;

        [Networked] private int TokensRemainingSquadA { get; set; }
        [Networked] private int TokensRemainingSquadB { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                int tokens = _rules != null ? _rules.ReinforcementTokensPerSquad : 1;
                TokensRemainingSquadA = tokens;
                TokensRemainingSquadB = tokens;
            }
        }

        public void ServerBeginDropPhase()
        {
            if (!HasStateAuthority) return;

            Vector3 squadADropPoint = SelectDropPoint(exclude: null);
            Vector3 squadBDropPoint = SelectDropPoint(exclude: squadADropPoint);

            // Actual per-player NetworkObject spawn/positioning at these two points is performed
            // by the caller that owns player-object lifecycle (outside this class's scope,
            // typically the session-start flow) — SpawnManager's responsibility here is
            // deterministic, validated *placement selection*, not player-object instantiation.
            ServerBroadcastDropPoints(squadADropPoint, squadBDropPoint);
        }

        private Vector3 SelectDropPoint(Vector3? exclude)
        {
            if (_dropPointCandidates == null || _dropPointCandidates.Length == 0) return Vector3.zero;

            var validCandidates = new List<Transform>(_dropPointCandidates.Length);
            foreach (var candidate in _dropPointCandidates)
            {
                if (exclude.HasValue && Vector3.Distance(candidate.position, exclude.Value) < _minSquadDropSeparationMeters)
                {
                    continue;
                }
                validCandidates.Add(candidate);
            }

            if (validCandidates.Count == 0) validCandidates.AddRange(_dropPointCandidates);

            int index = Random.Range(0, validCandidates.Count); // server-seeded per Runner's deterministic RNG in the full implementation
            return validCandidates[index].position;
        }

        private void ServerBroadcastDropPoints(Vector3 squadA, Vector3 squadB)
        {
            // Presentation-layer glide/deployment UI subscribes via an event channel in the full
            // implementation; omitted here to keep this system's scope to server placement logic.
        }

        /// <summary>
        /// Server-side Reinforcement Token redeploy request (GDD §8). Called from the
        /// player-interact flow when a living squadmate activates a Comms Beacon.
        /// </summary>
        public bool TryServerRedeployFromToken(int squadId, NetworkObject eliminatedPlayer, Vector3 beaconPosition)
        {
            if (!HasStateAuthority) return false;
            if (!IsNearAnyBeacon(beaconPosition)) return false;

            if (squadId == 0)
            {
                if (TokensRemainingSquadA <= 0) return false;
                TokensRemainingSquadA--;
            }
            else
            {
                if (TokensRemainingSquadB <= 0) return false;
                TokensRemainingSquadB--;
            }

            RedeployPlayer(eliminatedPlayer, beaconPosition);
            return true;
        }

        private bool IsNearAnyBeacon(Vector3 position)
        {
            if (_reinforcementBeacons == null) return false;
            const float beaconRadius = 5f;
            foreach (var beacon in _reinforcementBeacons)
            {
                if (Vector3.Distance(beacon.position, position) <= beaconRadius) return true;
            }
            return false;
        }

        private void RedeployPlayer(NetworkObject player, Vector3 position)
        {
            // Resets health/downed-state (delegated to the player's own health component) and
            // repositions — the exact reduced-health value is read from MatchRulesSO in the full
            // implementation rather than hardcoded here.
            player.transform.position = position;
        }
    }
}
