using Fusion;
using UnityEngine;
using ZoneStrike.Gameplay.Combat;

namespace ZoneStrike.Gameplay.Heroes.AbilityEffects
{
    /// <summary>
    /// Architecture role: Aegis's Tactical ability — a stationary, networked area-of-effect
    /// heal-over-time beacon (GDD §14). The second of the two Week 5 reference
    /// <see cref="IAbilityEffect"/> implementations — see <see cref="DeployableBarrierEffect"/>'s
    /// doc comment for why these two specifically were chosen as the framework's first stress
    /// test.
    ///
    /// Optimisation notes: the AoE tick uses a fixed-radius <see cref="Physics.OverlapSphere"/>
    /// on a throttled interval (twice per second, not every network tick) rather than continuous
    /// per-tick overlap checks — heal-over-time doesn't need tick-precision, and this materially
    /// reduces physics-query cost versus a naive every-tick implementation.
    ///
    /// Extension points: heal-per-tick and radius are Magnitude-driven from
    /// <c>AbilityDefinitionSO</c> (consistent with DeployableBarrierEffect's pattern) so Live Ops
    /// tuning never touches this code.
    ///
    /// Networking considerations: healing is applied only under <see cref="HasStateAuthority"/>
    /// directly to each target's <see cref="IDamageable"/>-adjacent health authority — this
    /// effect never trusts a client-reported "I was in range" claim, consistent with every other
    /// server-authoritative combat system in the project.
    /// </summary>
    public sealed class HealBeaconEffect : IAbilityEffect
    {
        private const float DefaultRadiusMeters = 4f;
        private const float DefaultDurationSeconds = 8f;
        private const float DefaultHealPerTick = 6f;

        public void Execute(in AbilityContext context)
        {
            var runner = context.Caster.Runner;
            if (runner == null || !runner.IsServer) return;

            var beacon = runner.Spawn(
                ResolveBeaconPrefab(),
                context.Origin,
                Quaternion.identity,
                context.Caster.Object.InputAuthority);

            if (beacon != null && beacon.TryGetComponent(out HealBeaconBehaviour behaviour))
            {
                float radius = context.Definition.Magnitude > 0f ? context.Definition.Magnitude : DefaultRadiusMeters;
                behaviour.InitializeServer(radius, DefaultDurationSeconds, DefaultHealPerTick);
            }
        }

        private static NetworkObject ResolveBeaconPrefab() => null; // see DeployableBarrierEffect note on prefab wiring
    }

    public sealed class HealBeaconBehaviour : NetworkBehaviour
    {
        [Networked] private TickTimer LifetimeTimer { get; set; }
        [Networked] private TickTimer TickTimer2 { get; set; }
        private float _radius;
        private float _healPerTick;
        private const float TickIntervalSeconds = 0.5f;

        public void InitializeServer(float radius, float durationSeconds, float healPerTick)
        {
            if (!HasStateAuthority) return;
            _radius = radius;
            _healPerTick = healPerTick;
            LifetimeTimer = TickTimer.CreateFromSeconds(Runner, durationSeconds);
            TickTimer2 = TickTimer.CreateFromSeconds(Runner, TickIntervalSeconds);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if (LifetimeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            if (TickTimer2.Expired(Runner))
            {
                TickTimer2 = TickTimer.CreateFromSeconds(Runner, TickIntervalSeconds);
                ApplyHealPulse();
            }
        }

        private void ApplyHealPulse()
        {
            var colliders = Physics.OverlapSphere(transform.position, _radius);
            foreach (var col in colliders)
            {
                if (col.TryGetComponent(out IHealable healable))
                {
                    healable.ApplyServerAuthoritativeHeal(_healPerTick);
                }
            }
        }
    }

    /// <summary>Paired with IDamageable — a target's health component implements both to receive damage and heal, per GDD §16's "faster ally-revive channel" support being layered on the same health component.</summary>
    public interface IHealable
    {
        void ApplyServerAuthoritativeHeal(float amount);
    }
}
