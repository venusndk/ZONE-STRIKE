namespace ZoneStrike.Gameplay.Combat
{
    /// <summary>
    /// Architecture role: thin contract implemented by any networked entity that can take
    /// damage (currently just player health, extensible to future destructible cover/objects).
    /// Lets WeaponSystem and ZoneManager apply damage without a hard reference to a specific
    /// "PlayerHealth" type — composition over inheritance per the engineering standards.
    ///
    /// Optimisation notes: single method, no allocation.
    ///
    /// Extension points: implement on any new damageable entity type; no changes needed to
    /// callers (WeaponSystem, ZoneManager) when new implementers are added.
    ///
    /// Networking considerations: implementations MUST apply damage only when running with
    /// state authority (the server) — this interface itself does not enforce that, so every
    /// implementer's doc comment should restate it explicitly (see PlayerHealth, Phase 4 note:
    /// health/downed-state ownership currently lives inside SpawnManager's revive/reinforcement
    /// flow per GDD §8; a dedicated PlayerHealth component is a Week 3-4 implementation task per
    /// the Phase 3 sprint plan, this interface is the seam it will plug into).
    /// </summary>
    public interface IDamageable
    {
        void ApplyServerAuthoritativeDamage(float amount, int instigatorPlayerId, bool isHeadshot);
    }
}
