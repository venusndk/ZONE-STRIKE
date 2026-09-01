using UnityEngine;

namespace ZoneStrike.Core.Data
{
    /// <summary>
    /// Architecture role: match-structure rules that are neither weapon/hero balance nor zone
    /// timing — squad size, Reinforcement Token count, downed/bleed-out timings (GDD §8).
    /// Read by <c>MatchManager</c> and <c>SpawnManager</c> on the server only.
    ///
    /// Optimisation notes: pure data, negligible cost.
    ///
    /// Extension points: a future ranked-mode variant (Phase 7/8) can add a second
    /// <c>MatchRulesSO</c> asset (e.g. zero Reinforcement Tokens for a hardcore ranked variant)
    /// without touching MatchManager code — the manager is parameterised entirely by this asset.
    ///
    /// Networking considerations: loaded identically by server and all clients from the build;
    /// never replicated itself, only referenced by index/id if a future mode-select needs to
    /// tell clients which ruleset is active.
    /// </summary>
    [CreateAssetMenu(menuName = "ZoneStrike/Data/Match Rules", fileName = "MatchRules_")]
    public sealed class MatchRulesSO : ScriptableObject
    {
        [SerializeField, Range(2, 4)] private int _squadSize = 4;
        [SerializeField, Range(1, 2)] private int _squadCount = 2;
        [SerializeField, Min(0)] private int _reinforcementTokensPerSquad = 1;
        [SerializeField, Min(0f)] private float _bleedOutDurationSeconds = 60f;
        [SerializeField, Min(0f)] private float _reviveChannelSeconds = 4.5f;
        [SerializeField, Min(0f)] private float _softMatchLengthSeconds = 480f; // 8:00
        [SerializeField, Min(0f)] private float _reconnectGraceWindowSeconds = 60f;

        public int SquadSize => _squadSize;
        public int SquadCount => _squadCount;
        public int TotalPlayers => _squadSize * _squadCount;
        public int ReinforcementTokensPerSquad => _reinforcementTokensPerSquad;
        public float BleedOutDurationSeconds => _bleedOutDurationSeconds;
        public float ReviveChannelSeconds => _reviveChannelSeconds;
        public float SoftMatchLengthSeconds => _softMatchLengthSeconds;
        public float ReconnectGraceWindowSeconds => _reconnectGraceWindowSeconds;
    }
}
