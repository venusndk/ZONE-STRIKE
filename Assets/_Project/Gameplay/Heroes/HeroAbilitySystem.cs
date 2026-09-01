using Fusion;
using UnityEngine;
using ZoneStrike.Core.Data;

namespace ZoneStrike.Gameplay.Heroes
{
    /// <summary>
    /// Architecture role: generic, hero-agnostic ability-cast framework (GDD §16, Technical
    /// Architecture §14 "extension points"). Owns cooldown/resource validation and networking for
    /// up to 3 ability slots (Tactical, Ultimate, Passive per GDD §16); the actual per-hero
    /// gameplay effect is delegated to an <see cref="IAbilityEffect"/> resolved via
    /// <see cref="AbilityEffectRegistry"/>. This is the framework proven in Phase 3 Week 5 on two
    /// structurally different heroes (Havoc/Aegis) before the remaining 6 are built on top of it
    /// in Week 6.
    ///
    /// Optimisation notes: cooldown state uses Fusion's <see cref="TickTimer"/> (a compact
    /// struct, not a MonoBehaviour Coroutine or a manually-ticked float) — zero per-frame
    /// allocation, consistent with the zero-steady-state-GC target.
    ///
    /// Extension points: this class should never need a hero-specific code branch — if a future
    /// hero's ability doesn't fit the "cooldown + resource cost + single Execute() call" shape,
    /// that is a signal to extend <see cref="AbilityContext"/> or <see cref="IAbilityEffect"/>,
    /// not to special-case this class (see the Phase 3 Week 6 risk note on Raptor/Ghost needing
    /// closer integration with movement/visibility systems as the harder validation case for this
    /// principle).
    ///
    /// Networking considerations: cast requests are validated server-side against cooldown AND
    /// resource cost before <see cref="IAbilityEffect.Execute"/> ever runs — directly enforces
    /// Technical Architecture §5.5's anti-cheat requirement that a client cannot bypass a
    /// cooldown by spamming a local input; a QA regression test for exactly this exploit attempt
    /// is called out explicitly in Phase 3 Week 5's acceptance criteria.
    /// </summary>
    public sealed class HeroAbilitySystem : NetworkBehaviour
    {
        [SerializeField] private HeroDefinitionSO _heroDefinition;

        [Networked] private TickTimer TacticalCooldown { get; set; }
        [Networked] private TickTimer UltimateCooldown { get; set; }
        [Networked] private float UltimateCharge01 { get; set; }

        public HeroDefinitionSO HeroDefinition => _heroDefinition;
        public float UltimateChargeNormalized => UltimateCharge01;
        public bool IsTacticalReady => TacticalCooldown.ExpiredOrNotRunning(Runner);
        public bool IsUltimateReady => UltimateCharge01 >= 1f && UltimateCooldown.ExpiredOrNotRunning(Runner);

        /// <summary>Called from PlayerController's input-processing path on the input-authority client.</summary>
        public void RequestCast(AbilitySlot slot, Vector3 origin, Vector3 aimDirection)
        {
            if (!HasInputAuthority) return;
            RPC_RequestCast(slot, origin, aimDirection);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestCast(AbilitySlot slot, Vector3 origin, Vector3 aimDirection)
        {
            AbilityDefinitionSO definition = ResolveDefinition(slot);
            if (definition == null) return;

            if (!ValidateCastAllowed(slot, definition)) return;

            ApplyCooldown(slot, definition);

            if (!AbilityEffectRegistry.TryResolve(definition.EffectId, out var effect)) return;

            var context = new AbilityContext(this, definition, origin, aimDirection);
            effect.Execute(in context);

            RPC_NotifyCastConfirmed(slot);
        }

        private bool ValidateCastAllowed(AbilitySlot slot, AbilityDefinitionSO definition)
        {
            return slot switch
            {
                AbilitySlot.Tactical => IsTacticalReady,
                AbilitySlot.Ultimate => IsUltimateReady,
                _ => false
            };
        }

        private void ApplyCooldown(AbilitySlot slot, AbilityDefinitionSO definition)
        {
            switch (slot)
            {
                case AbilitySlot.Tactical:
                    TacticalCooldown = TickTimer.CreateFromSeconds(Runner, definition.CooldownSeconds);
                    break;
                case AbilitySlot.Ultimate:
                    UltimateCooldown = TickTimer.CreateFromSeconds(Runner, definition.CooldownSeconds);
                    UltimateCharge01 = 0f;
                    break;
            }
        }

        private AbilityDefinitionSO ResolveDefinition(AbilitySlot slot) => slot switch
        {
            AbilitySlot.Tactical => _heroDefinition?.TacticalAbility,
            AbilitySlot.Ultimate => _heroDefinition?.UltimateAbility,
            _ => null
        };

        /// <summary>Called by damage-dealing/receiving systems to build ultimate charge (GDD §16: "charges via damage dealt/taken + time").</summary>
        public void AddUltimateCharge(float amount)
        {
            if (!HasStateAuthority) return;
            UltimateCharge01 = Mathf.Clamp01(UltimateCharge01 + amount);
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && UltimateCharge01 < 1f)
            {
                // Small passive time-based charge component, per GDD §16 ("...+ time").
                UltimateCharge01 = Mathf.Clamp01(UltimateCharge01 + Runner.DeltaTime / 90f);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RPC_NotifyCastConfirmed(AbilitySlot slot)
        {
            // Presentation layer (ability-icon HUD flash) hook — local-only notification.
        }
    }

    public enum AbilitySlot { Tactical, Ultimate, Passive }
}
