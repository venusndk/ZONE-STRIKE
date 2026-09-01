using System.Collections.Generic;
using UnityEngine;
using ZoneStrike.Gameplay.Heroes.AbilityEffects;

namespace ZoneStrike.Gameplay.Heroes
{
    /// <summary>
    /// Architecture role: maps an <c>AbilityDefinitionSO.EffectId</c> string to a concrete
    /// <see cref="IAbilityEffect"/> instance. A simple static registry rather than a full DI
    /// container binding, since ability effects are stateless strategies, not services with
    /// their own dependencies — deliberately the lightest-weight solution that satisfies the
    /// extension-point requirement documented on <see cref="IAbilityEffect"/>.
    ///
    /// Extension points: new hero content (Phase 3 Weeks 5-6) registers its effect here; this is
    /// the one place new ability implementations must be wired in.
    /// </summary>
    public static class AbilityEffectRegistry
    {
        private static readonly Dictionary<string, IAbilityEffect> _effects = new()
        {
            // Illustrative launch entries — GDD §14. The full 8-hero roster's remaining effects
            // are Phase 3 Week 5-6 production work built on this same framework/pattern.
            { "deployable_barrier", new DeployableBarrierEffect() },
            { "heal_beacon", new HealBeaconEffect() }
        };

        public static bool TryResolve(string effectId, out IAbilityEffect effect)
        {
            if (string.IsNullOrEmpty(effectId))
            {
                effect = null;
                return false;
            }

            if (_effects.TryGetValue(effectId, out effect)) return true;

            Debug.LogWarning($"[AbilityEffectRegistry] No IAbilityEffect registered for effectId '{effectId}'.");
            return false;
        }
    }
}
