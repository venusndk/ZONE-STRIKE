namespace ZoneStrike.Core.Data
{
    /// <summary>
    /// Architecture role: contract implemented by every tunable ScriptableObject (Technical
    /// Architecture §7) so Live Ops can layer Firebase Remote Config values on top of the
    /// shipped, designer-authored defaults at boot — without ever mutating the asset on disk
    /// and without an app resubmission for most balance changes.
    ///
    /// Optimisation notes: overrides are applied once at boot (or on an explicit Remote Config
    /// refresh), not per-frame — implementers should treat their public numeric properties as
    /// plain in-memory fields, not recompute-on-read.
    ///
    /// Extension points: implementers add one <c>case</c> per remote-tunable field to
    /// <see cref="ApplyRemoteOverride"/>; fields not listed there are intentionally not
    /// remote-tunable (a deliberate design decision, not an oversight — see each SO's own docs).
    ///
    /// Networking considerations: none directly — this is a client-local config layering
    /// mechanism, not a networked type.
    /// </summary>
    public interface IRemoteOverridable
    {
        /// <summary>Restores every remote-tunable value to the value authored in the Inspector.</summary>
        void ResetToShippedDefaults();

        /// <summary>Applies a single named Remote Config override on top of the shipped default.</summary>
        void ApplyRemoteOverride(string key, float value);
    }
}
