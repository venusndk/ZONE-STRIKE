using UnityEngine;
using VContainer;
using VContainer.Unity;
using ZoneStrike.Gameplay.Match;

namespace ZoneStrike.Infrastructure.DI
{
    /// <summary>
    /// Architecture role: match-lifetime VContainer scope (Technical Architecture §3/§12),
    /// created as a child of <see cref="GameLifetimeScope"/> when GameManager transitions into a
    /// match, and explicitly disposed when the match ends and the player returns to the Home Hub.
    /// Wires the match-scoped Gameplay systems (MatchManager, ZoneManager, SpawnManager) that
    /// must not survive across matches — this is what prevents the classic mobile-game bug class
    /// of stale match state leaking into the next session.
    ///
    /// Optimisation notes: because this is a child scope, it automatically inherits every
    /// Infrastructure-layer singleton from <see cref="GameLifetimeScope"/> (Save, Settings,
    /// Network, UI, Audio) without re-registering them — avoids duplicate service instances.
    ///
    /// Extension points: per-match systems added in later phases (e.g. a killfeed aggregator,
    /// a ranked-mode scoring variant per Technical Architecture §13 assumption) register here,
    /// not in GameLifetimeScope, if their state should not outlive a single match.
    ///
    /// Networking considerations: MatchManager/ZoneManager/SpawnManager are Fusion
    /// NetworkBehaviours attached to scene/spawned NetworkObjects, not plain injected classes —
    /// they're registered via <c>RegisterComponentInHierarchy</c> so VContainer can still inject
    /// their own dependencies (event channel SO references, etc.) without taking over Fusion's
    /// own object-lifecycle ownership of them.
    ///
    /// A note on the Technical Architecture §3 dependency diagram: that diagram documents
    /// *runtime* dependency direction (Presentation/Gameplay/Networking never reach backward into
    /// Infrastructure at play time). A composition root is the sanctioned, narrow exception to
    /// that rule by definition — its entire job is wiring the full object graph, so it alone is
    /// allowed to reference every layer, including Gameplay. This is why
    /// <c>ZoneStrike.Infrastructure.asmdef</c> carries an assembly reference to
    /// <c>ZoneStrike.Gameplay.asmdef</c> scoped to this file's folder, while no Gameplay code
    /// ever references Infrastructure back.
    /// </summary>
    public sealed class MatchLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MatchManager>();
            builder.RegisterComponentInHierarchy<ZoneManager>();
            builder.RegisterComponentInHierarchy<SpawnManager>();
        }

        /// <summary>
        /// Called by GameManager when the match ends and the app returns to the Hub — disposes
        /// this scope's container, releasing every match-scoped reference so nothing from this
        /// match persists into the next one.
        /// </summary>
        public void TearDown()
        {
            Dispose();
        }
    }
}
