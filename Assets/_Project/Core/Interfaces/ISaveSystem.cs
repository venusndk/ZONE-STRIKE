using Cysharp.Threading.Tasks;

namespace ZoneStrike.Core.Interfaces
{
    /// <summary>
    /// Architecture role: Infrastructure-layer contract for local persistence, consumed by
    /// GameManager and SettingsManager. Abstracted behind an interface (rather than a concrete
    /// class reference) so EditMode unit tests can inject an in-memory fake instead of touching
    /// disk (Technical Architecture §8 testing package choice).
    ///
    /// Optimisation notes: both methods are async (UniTask) — file IO must never block the main
    /// thread on mobile, where a slow/throttled storage write can otherwise cause a visible
    /// frame hitch.
    ///
    /// Extension points: a cloud-save implementation (future Live Ops feature) can implement
    /// this same interface and be swapped in via the DI container without touching any caller.
    ///
    /// Networking considerations: none — this is local device persistence only. Cross-device
    /// sync, if added later, is a distinct feature layered on top, not a replacement for this
    /// interface.
    /// </summary>
    public interface ISaveSystem
    {
        UniTask SaveAsync(SaveData data);
        UniTask<SaveData> LoadAsync();
    }

    /// <summary>
    /// Root persisted data shape. Kept intentionally small and flat for MVP scope (Phase 3 §0) —
    /// cosmetic inventory, mastery progress, and battle pass state are added here incrementally
    /// as those systems come online post-MVP, not speculatively pre-built.
    /// </summary>
    [System.Serializable]
    public sealed class SaveData
    {
        public int SchemaVersion = 1;
        public string PlayerId;
        public GraphicsQualityTier GraphicsTier = GraphicsQualityTier.Auto;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public bool ColorblindMode;
        public bool AutoSprintEnabled = true;
        public bool AutoVaultEnabled = true;
    }

    public enum GraphicsQualityTier { Auto, Low, Mid, High }
}
