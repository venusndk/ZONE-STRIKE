using UnityEngine;

namespace ZoneStrike.Core.Data
{
    public enum WeaponArchetype { SMG, AssaultRifle, Shotgun, DMR, Sidearm }
    public enum WeaponRarity { Common, Rare, Epic, Legendary }

    /// <summary>
    /// Architecture role: data-only weapon stat block for one archetype/rarity combination
    /// (GDD §15). Consumed by <c>WeaponSystem</c> for both the client-predicted local fire
    /// feedback and the server's authoritative hit/damage resolution — both read from the same
    /// asset, which is what guarantees they can never disagree on what a weapon "should" do.
    ///
    /// Optimisation notes: rarity tiers are expressed as flat multipliers over the archetype
    /// base stats rather than as separate stat blocks, per GDD §15's "no new mechanics gated
    /// behind rarity" rule — keeps the data schema small and the balance surface auditable.
    ///
    /// Extension points: new archetypes go in <see cref="WeaponArchetype"/> and get a new base
    /// asset; new rarity tiers (unlikely post-launch, but supported) go in
    /// <see cref="WeaponRarity"/> with a new multiplier entry.
    ///
    /// Networking considerations: <see cref="WeaponId"/> is the only value transmitted per shot
    /// (as a lookup key); the server resolves the full stat block locally from its own loaded
    /// copy of this asset, never trusting a client-supplied damage value.
    /// </summary>
    [CreateAssetMenu(menuName = "ZoneStrike/Data/Weapon Definition", fileName = "Weapon_")]
    public sealed class WeaponDefinitionSO : ScriptableObject, IRemoteOverridable
    {
        [Header("Identity")]
        [SerializeField] private string _weaponId;
        [SerializeField] private string _displayName;
        [SerializeField] private WeaponArchetype _archetype;
        [SerializeField] private WeaponRarity _rarity;

        [Header("Base stats (Common-rarity baseline)")]
        [SerializeField, Min(0f)] private float _baseDamagePerHit = 18f;
        [SerializeField, Min(0.01f)] private float _fireRateRoundsPerSecond = 8f;
        [SerializeField, Min(1)] private int _magazineSize = 30;
        [SerializeField, Min(0f)] private float _reloadSeconds = 2.2f;
        [SerializeField, Min(0f)] private float _effectiveRangeMeters = 30f;
        [SerializeField, Min(1f)] private float _headshotMultiplier = 2f;

        [Header("Rarity scaling (flat % over base — GDD §15)")]
        [SerializeField] private float _rarityDamageMultiplier = 1f;
        [SerializeField] private float _rarityHandlingMultiplier = 1f;

        public string WeaponId => _weaponId;
        public string DisplayName => _displayName;
        public WeaponArchetype Archetype => _archetype;
        public WeaponRarity Rarity => _rarity;

        public float DamagePerHit { get; private set; }
        public float FireRateRoundsPerSecond { get; private set; }
        public int MagazineSize { get; private set; }
        public float ReloadSeconds { get; private set; }
        public float EffectiveRangeMeters { get; private set; }
        public float HeadshotMultiplier { get; private set; }

        /// <summary>Seconds between shots, derived from fire rate — used by WeaponSystem's server-side cooldown gate.</summary>
        public float FireIntervalSeconds => 1f / Mathf.Max(0.01f, FireRateRoundsPerSecond);

        private void OnEnable() => ResetToShippedDefaults();

        public void ResetToShippedDefaults()
        {
            DamagePerHit = _baseDamagePerHit * _rarityDamageMultiplier;
            FireRateRoundsPerSecond = _fireRateRoundsPerSecond;
            MagazineSize = _magazineSize;
            ReloadSeconds = _reloadSeconds / Mathf.Max(0.01f, _rarityHandlingMultiplier);
            EffectiveRangeMeters = _effectiveRangeMeters;
            HeadshotMultiplier = _headshotMultiplier;
        }

        public void ApplyRemoteOverride(string key, float value)
        {
            switch (key)
            {
                case nameof(DamagePerHit): DamagePerHit = value; break;
                case nameof(FireRateRoundsPerSecond): FireRateRoundsPerSecond = value; break;
                case nameof(ReloadSeconds): ReloadSeconds = value; break;
                case nameof(HeadshotMultiplier): HeadshotMultiplier = value; break;
                default:
                    Debug.LogWarning($"[WeaponDefinitionSO:{_weaponId}] Unknown remote override key '{key}' ignored.");
                    break;
            }
        }
    }
}
