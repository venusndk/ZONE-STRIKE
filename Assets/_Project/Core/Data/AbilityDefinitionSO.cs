using UnityEngine;

namespace ZoneStrike.Core.Data
{
    /// <summary>
    /// Architecture role: designer-authored, data-only definition of one hero ability
    /// (tactical, ultimate, or passive per GDD §16). Consumed exclusively by
    /// <c>HeroAbilitySystem</c>, which resolves <see cref="EffectId"/> to a concrete
    /// <see cref="ZoneStrike.Gameplay.Heroes.IAbilityEffect"/> implementation via the
    /// ability-effect registry — this SO never contains gameplay logic itself (data/behaviour
    /// separation, so a designer can retune numbers without an engineer touching code).
    ///
    /// Optimisation notes: pure data, no per-frame cost. VFX/SFX references are Addressables
    /// asset references, not direct hard references, so an unused hero's assets are never
    /// pulled into the initial download (Technical Architecture §6).
    ///
    /// Extension points: add a new ability by authoring a new asset with a new
    /// <see cref="EffectId"/> and registering a matching <c>IAbilityEffect</c> — no changes to
    /// this class are needed for new ability content.
    ///
    /// Networking considerations: <see cref="EffectId"/> and numeric fields are what the server
    /// validates a cast against (cooldown, resource cost); VFX/SFX fields are presentation-only
    /// and are never referenced by server-authoritative code paths.
    /// </summary>
    [CreateAssetMenu(menuName = "ZoneStrike/Data/Ability Definition", fileName = "Ability_")]
    public sealed class AbilityDefinitionSO : ScriptableObject, IRemoteOverridable
    {
        [Header("Identity")]
        [SerializeField] private string _abilityId;
        [SerializeField] private string _displayName;
        [Tooltip("Key resolved by the ability-effect registry to a concrete IAbilityEffect implementation.")]
        [SerializeField] private string _effectId;

        [Header("Balance (base values)")]
        [SerializeField, Min(0f)] private float _cooldownSeconds = 15f;
        [SerializeField, Min(0f)] private float _resourceCost;
        [SerializeField, Min(0f)] private float _magnitude = 1f;
        [Tooltip("Seconds between cast input and effect resolving — the GDD §19 counterplay 'readable tell' window.")]
        [SerializeField, Min(0f)] private float _telegraphSeconds = 0.3f;

        [Header("Presentation (Addressables keys, resolved by Presentation layer only)")]
        [SerializeField] private string _castVfxAddress;
        [SerializeField] private string _castSfxAddress;

        public string AbilityId => _abilityId;
        public string DisplayName => _displayName;
        public string EffectId => _effectId;
        public float CooldownSeconds { get; private set; }
        public float ResourceCost { get; private set; }
        public float Magnitude { get; private set; }
        public float TelegraphSeconds { get; private set; }
        public string CastVfxAddress => _castVfxAddress;
        public string CastSfxAddress => _castSfxAddress;

        private void OnEnable() => ResetToShippedDefaults();

        public void ResetToShippedDefaults()
        {
            CooldownSeconds = _cooldownSeconds;
            ResourceCost = _resourceCost;
            Magnitude = _magnitude;
            TelegraphSeconds = _telegraphSeconds;
        }

        public void ApplyRemoteOverride(string key, float value)
        {
            switch (key)
            {
                case nameof(CooldownSeconds): CooldownSeconds = value; break;
                case nameof(ResourceCost): ResourceCost = value; break;
                case nameof(Magnitude): Magnitude = value; break;
                case nameof(TelegraphSeconds): TelegraphSeconds = value; break;
                default:
                    Debug.LogWarning($"[AbilityDefinitionSO:{_abilityId}] Unknown remote override key '{key}' ignored.");
                    break;
            }
        }
    }
}
