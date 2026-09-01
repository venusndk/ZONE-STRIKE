using System;
using UnityEngine;

namespace ZoneStrike.Core.Data
{
    /// <summary>
    /// Architecture role: static, designer-authored definition of a single hero's identity and
    /// base stats. One asset per launch hero (GDD §14 — 8 total). Consumed by
    /// <c>HeroAbilitySystem</c> (ability references), <c>PlayerController</c> (movement stats),
    /// and Presentation-layer hero-select UI. Never mutated at runtime — see
    /// <see cref="IRemoteOverridable"/> for how Live Ops tuning is layered on top without
    /// touching this asset.
    ///
    /// Optimisation notes: referenced by value (ScriptableObject asset reference), never cloned;
    /// keep this data-only — no MonoBehaviour logic belongs here, per the Core layer's
    /// "no UnityEngine.UI / no Fusion" dependency rule (Technical Architecture §3).
    ///
    /// Extension points: add new roles to <see cref="HeroRole"/>; add new heroes by creating new
    /// assets, never by adding fields conditional on hero identity (keep this schema uniform).
    ///
    /// Networking considerations: only <see cref="HeroId"/> is ever sent over the network
    /// (as a lookup key into an Addressables-loaded catalog); the asset itself is never
    /// serialized into a network packet.
    /// </summary>
    [CreateAssetMenu(menuName = "ZoneStrike/Data/Hero Definition", fileName = "Hero_")]
    public sealed class HeroDefinitionSO : ScriptableObject, IRemoteOverridable
    {
        [Header("Identity")]
        [SerializeField] private string _heroId = Guid.NewGuid().ToString("N");
        [SerializeField] private string _displayName;
        [SerializeField] private HeroRole _role;

        [Header("Movement (base values — GDD §12)")]
        [SerializeField, Min(0f)] private float _baseMoveSpeed = 5.5f;
        [SerializeField, Min(0f)] private float _sprintSpeedMultiplier = 1.35f;
        [SerializeField, Min(0f)] private float _slideImpulse = 6f;

        [Header("Combat (base values)")]
        [SerializeField, Min(1f)] private float _baseHealth = 100f;

        [Header("Abilities")]
        [SerializeField] private AbilityDefinitionSO _tacticalAbility;
        [SerializeField] private AbilityDefinitionSO _ultimateAbility;
        [SerializeField] private AbilityDefinitionSO _passiveAbility;

        public string HeroId => _heroId;
        public string DisplayName => _displayName;
        public HeroRole Role => _role;
        public float BaseMoveSpeed { get; private set; }
        public float SprintSpeedMultiplier { get; private set; }
        public float SlideImpulse { get; private set; }
        public float BaseHealth { get; private set; }
        public AbilityDefinitionSO TacticalAbility => _tacticalAbility;
        public AbilityDefinitionSO UltimateAbility => _ultimateAbility;
        public AbilityDefinitionSO PassiveAbility => _passiveAbility;

        private void OnEnable() => ResetToShippedDefaults();

        /// <inheritdoc />
        public void ResetToShippedDefaults()
        {
            BaseMoveSpeed = _baseMoveSpeed;
            SprintSpeedMultiplier = _sprintSpeedMultiplier;
            SlideImpulse = _slideImpulse;
            BaseHealth = _baseHealth;
        }

        /// <inheritdoc />
        public void ApplyRemoteOverride(string key, float value)
        {
            switch (key)
            {
                case nameof(BaseMoveSpeed): BaseMoveSpeed = value; break;
                case nameof(SprintSpeedMultiplier): SprintSpeedMultiplier = value; break;
                case nameof(SlideImpulse): SlideImpulse = value; break;
                case nameof(BaseHealth): BaseHealth = value; break;
                default:
                    Debug.LogWarning($"[HeroDefinitionSO:{_heroId}] Unknown remote override key '{key}' ignored.");
                    break;
            }
        }
    }

    public enum HeroRole
    {
        Vanguard,
        Striker,
        Phantom,
        Medic
    }
}
