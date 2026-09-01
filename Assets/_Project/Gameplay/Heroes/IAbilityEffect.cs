using UnityEngine;
using ZoneStrike.Core.Data;

namespace ZoneStrike.Gameplay.Heroes
{
    /// <summary>
    /// Architecture role: strategy-pattern contract for a single ability's actual gameplay
    /// effect, resolved by <see cref="HeroAbilitySystem"/> from an <see cref="AbilityDefinitionSO"/>'s
    /// <c>EffectId</c> via <see cref="AbilityEffectRegistry"/>. This is the extension point that
    /// lets 8 structurally different hero kits (GDD §14) share one generic cooldown/resource/
    /// networking framework (HeroAbilitySystem) without HeroAbilitySystem knowing anything about
    /// any specific hero — composition over inheritance per the engineering standards.
    ///
    /// Optimisation notes: effects are resolved once via a small dictionary lookup at cast time,
    /// not polymorphic dispatch through a long inheritance chain — keeps the framework flat and
    /// cheap even as the hero roster grows post-launch (Phase 8 new-hero cadence).
    ///
    /// Extension points: THIS is the extension point — implement this interface once per unique
    /// ability effect (not once per hero; a shared effect like "deployable barrier" could in
    /// principle be reused by a future hero with a re-skinned version of the same mechanic).
    ///
    /// Networking considerations: <see cref="Execute"/> is only ever called server-side (from
    /// HeroAbilitySystem's RPC handler, which already validated cooldown/resource cost) — an
    /// implementation should treat itself as already-authorized and focus purely on the
    /// gameplay effect (spawning a networked object, applying a networked buff, etc.), never
    /// re-deriving whether the cast is "allowed."
    /// </summary>
    public interface IAbilityEffect
    {
        void Execute(in AbilityContext context);
    }

    /// <summary>Everything an ability effect needs to act — passed by value (struct) to avoid per-cast allocation.</summary>
    public readonly struct AbilityContext
    {
        public readonly HeroAbilitySystem Caster;
        public readonly AbilityDefinitionSO Definition;
        public readonly Vector3 Origin;
        public readonly Vector3 AimDirection;

        public AbilityContext(HeroAbilitySystem caster, AbilityDefinitionSO definition, Vector3 origin, Vector3 aimDirection)
        {
            Caster = caster;
            Definition = definition;
            Origin = origin;
            AimDirection = aimDirection;
        }
    }
}
