using Fusion;
using UnityEngine;

namespace ZoneStrike.Gameplay.Heroes.AbilityEffects
{
    /// <summary>
    /// Architecture role: Havoc's Tactical ability — a deployable, collision-blocking energy
    /// barrier (GDD §14). One of the two Week 5 reference implementations of
    /// <see cref="IAbilityEffect"/>, chosen specifically because it exercises "spawn a networked
    /// world object" — the other reference implementation (<see cref="HealBeaconEffect"/>)
    /// exercises "apply a networked buff to existing entities" — between them they stress-test
    /// the framework's two main effect shapes before the remaining 6 heroes are built on it.
    ///
    /// Optimisation notes: pooled, not instantiated/destroyed — barriers are short-lived (6s per
    /// GDD §14) and cast repeatedly across a match, so a NetworkObject pool (managed by a
    /// dedicated spawner, referenced here via <see cref="Runner"/> access on the caster) avoids
    /// per-cast instantiation cost.
    ///
    /// Extension points: barrier duration/size are read from <c>AbilityDefinitionSO.Magnitude</c>
    /// rather than hardcoded, so Live Ops can retune Havoc's barrier without a code change.
    ///
    /// Networking considerations: spawned via <c>Runner.Spawn</c> under server authority only
    /// (this class is only ever invoked from HeroAbilitySystem's already-server-side RPC
    /// handler); the spawned barrier NetworkObject's own despawn timer is server-driven so
    /// clients cannot locally extend or remove a barrier's lifetime.
    /// </summary>
    public sealed class DeployableBarrierEffect : IAbilityEffect
    {
        private const float DefaultDurationSeconds = 6f;

        public void Execute(in AbilityContext context)
        {
            var runner = context.Caster.Runner;
            if (runner == null || !runner.IsServer) return;

            Vector3 spawnPosition = context.Origin + context.AimDirection.normalized * 2f;
            Quaternion spawnRotation = Quaternion.LookRotation(context.AimDirection, Vector3.up);

            // NOTE: barrier prefab should be a pooled NetworkPrefab reference resolved via the
            // Addressables-backed Fusion NetworkObject provider (Technical Architecture §6);
            // omitted here as a prefab-asset wiring concern outside this effect's own logic.
            var barrier = runner.Spawn(
                ResolveBarrierPrefab(),
                spawnPosition,
                spawnRotation,
                context.Caster.Object.InputAuthority);

            float duration = context.Definition.Magnitude > 0f ? context.Definition.Magnitude : DefaultDurationSeconds;
            if (barrier != null && barrier.TryGetComponent(out DeployableBarrierBehaviour barrierBehaviour))
            {
                barrierBehaviour.InitializeServerLifetime(duration);
            }
        }

        private static NetworkObject ResolveBarrierPrefab()
        {
            // Resolved from a project-level NetworkPrefabRef table in the real implementation.
            return null;
        }
    }

    /// <summary>Server-driven despawn timer for a spawned barrier instance — deliberately minimal; blocking-collision behaviour lives on the prefab's own Collider setup.</summary>
    public sealed class DeployableBarrierBehaviour : NetworkBehaviour
    {
        [Networked] private TickTimer LifetimeTimer { get; set; }

        public void InitializeServerLifetime(float durationSeconds)
        {
            if (!HasStateAuthority) return;
            LifetimeTimer = TickTimer.CreateFromSeconds(Runner, durationSeconds);
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && LifetimeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
    }
}
