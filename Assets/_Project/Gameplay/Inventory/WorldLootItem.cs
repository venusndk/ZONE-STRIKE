using Fusion;
using UnityEngine;
using ZoneStrike.Core.Data;

namespace ZoneStrike.Gameplay.Inventory.Loot
{
    public enum LootKind { Weapon, Armor, Attachment, AbilityCharge }

    /// <summary>
    /// Architecture role: a single networked world-space loot pickup instance, spawned by
    /// SpawnManager's deterministic, server-seeded loot table (GDD §17, Technical Architecture
    /// §17) at match start. Claimed exactly once, by whichever <c>InventorySystem</c> RPC wins
    /// the server-side race for a given tick — see <see cref="MarkClaimed"/>.
    ///
    /// Optimisation notes: pooled by SpawnManager rather than instantiated/destroyed per pickup
    /// (an 8-minute match with a compact map, GDD §10, has a bounded, small loot-crate count —
    /// pooling keeps re-spawning between matches free of per-item instantiation cost).
    ///
    /// Extension points: <see cref="LootKind"/> covers MVP scope (GDD §15/§17); a future
    /// consumable-item kind (e.g. a one-time mobility charge) extends this enum and
    /// InventorySystem's switch statement, following the same pattern.
    ///
    /// Networking considerations: <see cref="IsClaimed"/> is a [Networked] bool so the claim
    /// race is resolved identically for every client — the first server-processed
    /// RPC_RequestPickup (InventorySystem) wins, and every later request against an
    /// already-claimed item is rejected, preventing a duplicate-pickup exploit under latency.
    /// </summary>
    public sealed class WorldLootItem : NetworkBehaviour
    {
        [SerializeField] private LootKind _itemKind;
        [SerializeField] private WeaponDefinitionSO _weaponDefinition;
        [SerializeField] private int _armorTierValue;
        [SerializeField] private string _attachmentId;

        [Networked] public NetworkBool IsClaimed { get; private set; }

        public LootKind ItemKind => _itemKind;
        public WeaponDefinitionSO WeaponDefinition => _weaponDefinition;
        public int ArmorTierValue => _armorTierValue;
        public string AttachmentId => _attachmentId;

        /// <summary>Server-only: marks this pickup consumed. Callers must already hold state authority (enforced by the RPC target attribute on the caller).</summary>
        public void MarkClaimed()
        {
            if (!HasStateAuthority) return;
            IsClaimed = true;
        }

        /// <summary>Called by SpawnManager when returning this instance to the pool between matches.</summary>
        public void ResetForPool()
        {
            if (!HasStateAuthority) return;
            IsClaimed = false;
        }
    }
}
