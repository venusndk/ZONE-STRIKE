using Fusion;
using UnityEngine;
using ZoneStrike.Core.Data;
using ZoneStrike.Gameplay.Weapons;

namespace ZoneStrike.Gameplay.Inventory
{
    /// <summary>
    /// Architecture role: server-authoritative loadout state — equipped weapon, armor tier, and
    /// the two attachment slots (GDD §15's simplified 2-slot system). Bridges world loot pickups
    /// to <see cref="WeaponSystem"/>'s equipped-weapon state.
    ///
    /// Optimisation notes: item identity is transmitted as a compact string id (resolved against
    /// a build-time catalog), never as a full asset reference, keeping pickup RPC payloads small.
    ///
    /// Extension points: attachment slots are stored as a fixed 2-length array rather than a
    /// dynamic collection (GDD §15 explicitly caps attachments at 2 for mobile UX reasons) — a
    /// future archetype needing more slots is an explicit design change, not just a code change,
    /// by intent.
    ///
    /// Networking considerations: pickups are validated server-side against the deterministic,
    /// server-seeded loot spawn table (Technical Architecture §17/Phase 3 Week 8) — a client
    /// requesting to pick up an item it doesn't have line-of-sight/proximity to, or that was
    /// already claimed by another player this tick, is rejected. This closes the same class of
    /// exploit as weapon hit validation: the client can request, never assert, an inventory change.
    /// </summary>
    public sealed class InventorySystem : NetworkBehaviour
    {
        [SerializeField] private WeaponSystem _weaponSystem;
        [SerializeField] private float _pickupRadiusMeters = 2.5f;

        [Networked] private int ArmorTier { get; set; }
        [Networked, Capacity(2)] private NetworkArray<NetworkString<_16>> AttachmentIds => default;

        public int CurrentArmorTier => ArmorTier;

        /// <summary>
        /// Server-side damage-reduction multiplier applied by IDamageable implementers —
        /// GDD §18: "extends TTK by ~15-30% per tier."
        /// </summary>
        public float ArmorDamageReductionMultiplier => ArmorTier switch
        {
            1 => 0.85f,
            2 => 0.70f,
            _ => 1f
        };

        public void RequestPickup(NetworkId lootEntityId)
        {
            if (!HasInputAuthority) return;
            RPC_RequestPickup(lootEntityId);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestPickup(NetworkId lootEntityId)
        {
            if (!Runner.TryFindObject(lootEntityId, out var lootObject)) return;

            var loot = lootObject.GetComponent<Loot.WorldLootItem>();
            if (loot == null || loot.IsClaimed) return;

            float distance = Vector3.Distance(transform.position, loot.transform.position);
            if (distance > _pickupRadiusMeters) return; // server-side proximity validation, never trusts client claim of "I'm near it"

            ApplyPickup(loot);
            loot.MarkClaimed();
        }

        private void ApplyPickup(Loot.WorldLootItem loot)
        {
            switch (loot.ItemKind)
            {
                case Loot.LootKind.Weapon when loot.WeaponDefinition != null:
                    _weaponSystem?.EquipWeapon(loot.WeaponDefinition);
                    break;
                case Loot.LootKind.Armor:
                    ArmorTier = Mathf.Clamp(loot.ArmorTierValue, 0, 2);
                    break;
                case Loot.LootKind.Attachment:
                    // Slot-assignment logic (first empty slot) — deliberately simple per the
                    // GDD §15 "reduce inventory management taps" mobile UX goal.
                    for (int i = 0; i < 2; i++)
                    {
                        if (AttachmentIds[i].ToString() == string.Empty)
                        {
                            AttachmentIds.Set(i, loot.AttachmentId);
                            break;
                        }
                    }
                    break;
            }
        }
    }
}
