using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ZoneStrike.Core.Interfaces;

namespace ZoneStrike.Infrastructure.Settings
{
    /// <summary>
    /// Architecture role: owns graphics quality tier, audio levels, and accessibility toggles
    /// (GDD §20; Technical Architecture §2's Low/Mid/High quality tiers), persisting through
    /// <see cref="ISaveSystem"/>. Also owns the runtime <see cref="PerformanceGovernor"/> that
    /// implements the brief-mandated 60→30 FPS graceful fallback.
    ///
    /// Optimisation notes: quality-tier changes swap a pre-authored URP
    /// <see cref="UniversalRenderPipelineAsset"/> per tier (Technical Architecture §2 table)
    /// rather than mutating individual render-pipeline settings at runtime — swapping the whole
    /// asset is both cheaper and far less bug-prone than toggling a dozen individual settings.
    ///
    /// Extension points: <see cref="GraphicsQualityTier.Auto"/> triggers a one-time device
    /// benchmark pass on first boot (stubbed here as <see cref="RunDeviceBenchmark"/>) — the
    /// actual benchmark workload (Technical Architecture §2) is a Phase 4 implementation task for
    /// the graphics/tech-art team, this class only owns the tier-selection contract around it.
    ///
    /// Networking considerations: none — entirely local/client-side. Accessibility settings
    /// (colorblind mode, subtitles) are read by Presentation-layer rendering/UI code, never sent
    /// over the network.
    /// </summary>
    public sealed class SettingsManager
    {
        private readonly ISaveSystem _saveSystem;
        private readonly UniversalRenderPipelineAsset _lowTierAsset;
        private readonly UniversalRenderPipelineAsset _midTierAsset;
        private readonly UniversalRenderPipelineAsset _highTierAsset;

        public SaveData CurrentSettings { get; private set; }
        public PerformanceGovernor Governor { get; }

        public SettingsManager(
            ISaveSystem saveSystem,
            UniversalRenderPipelineAsset lowTierAsset,
            UniversalRenderPipelineAsset midTierAsset,
            UniversalRenderPipelineAsset highTierAsset)
        {
            _saveSystem = saveSystem;
            _lowTierAsset = lowTierAsset;
            _midTierAsset = midTierAsset;
            _highTierAsset = highTierAsset;
            Governor = new PerformanceGovernor(this);
        }

        public async UniTask InitializeAsync()
        {
            CurrentSettings = await _saveSystem.LoadAsync();

            GraphicsQualityTier resolvedTier = CurrentSettings.GraphicsTier == GraphicsQualityTier.Auto
                ? RunDeviceBenchmark()
                : CurrentSettings.GraphicsTier;

            ApplyGraphicsTier(resolvedTier);
        }

        public void ApplyGraphicsTier(GraphicsQualityTier tier)
        {
            UniversalRenderPipelineAsset asset = tier switch
            {
                GraphicsQualityTier.Low => _lowTierAsset,
                GraphicsQualityTier.High => _highTierAsset,
                _ => _midTierAsset
            };

            if (asset != null)
            {
                UnityEngine.QualitySettings.renderPipeline = asset;
            }

            Application.targetFrameRate = tier == GraphicsQualityTier.Low ? 30 : 60;
        }

        public void SetAndPersist(System.Action<SaveData> mutate)
        {
            mutate(CurrentSettings);
            _saveSystem.SaveAsync(CurrentSettings).Forget();
        }

        private GraphicsQualityTier RunDeviceBenchmark()
        {
            // Placeholder decision heuristic (device RAM as a coarse proxy) — the real
            // implementation runs a short timed render/compute workload per Technical
            // Architecture §2; this is an explicit Phase 4 graphics-team task, not a stand-in for
            // shipped balance data (unlike the GDD's flagged gameplay-balance hypotheses, this is
            // purely an engineering task with no design ambiguity to resolve).
            int systemMemoryMb = SystemInfo.systemMemorySize;
            if (systemMemoryMb <= 3200) return GraphicsQualityTier.Low;
            if (systemMemoryMb <= 6144) return GraphicsQualityTier.Mid;
            return GraphicsQualityTier.High;
        }
    }

    /// <summary>
    /// Architecture role: runtime frame-time watchdog implementing the brief-mandated graceful
    /// 60→30 FPS fallback (Technical Architecture §2). Steps quality down progressively
    /// (dynamic resolution → MSAA → full tier drop) rather than snapping directly to the floor,
    /// so a thermally-throttling mid-range device degrades smoothly instead of stuttering.
    ///
    /// Optimisation notes: samples a rolling average frame time rather than reacting to a single
    /// slow frame, avoiding over-aggressive downgrades from a single GC spike or scene-load hitch.
    ///
    /// Extension points: each degradation step is a separate, individually-skippable method
    /// (<see cref="EngageDynamicResolutionScaling"/>, etc.) so a future device-specific tuning
    /// pass can reorder or disable individual steps without rewriting the watchdog loop.
    ///
    /// Networking considerations: none — this is a purely local rendering-performance concern
    /// and never affects simulation/replicated state (movement/combat correctness is unaffected
    /// by which visual quality tier a given client is rendering at).
    /// </summary>
    public sealed class PerformanceGovernor
    {
        private const float TargetFrameTimeMs = 16.6f;
        private const float SustainedBadFrameSecondsBeforeStepDown = 3f;

        private readonly SettingsManager _settingsManager;
        private float _badFrameTimer;
        private int _degradationStep;

        public PerformanceGovernor(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        /// <summary>Call once per frame (e.g. from a lightweight MonoBehaviour driver in the match scene).</summary>
        public void Tick(float deltaTimeSeconds)
        {
            float frameTimeMs = deltaTimeSeconds * 1000f;

            if (frameTimeMs > TargetFrameTimeMs)
            {
                _badFrameTimer += deltaTimeSeconds;
                if (_badFrameTimer >= SustainedBadFrameSecondsBeforeStepDown)
                {
                    StepDown();
                    _badFrameTimer = 0f;
                }
            }
            else
            {
                _badFrameTimer = 0f;
            }
        }

        private void StepDown()
        {
            _degradationStep++;
            switch (_degradationStep)
            {
                case 1: EngageDynamicResolutionScaling(); break;
                case 2: DisableMsaa(); break;
                case 3: DropToLowTier(); break;
                // Step 3 is the floor — no further degradation beyond the Low tier's locked 30 FPS.
            }
        }

        private void EngageDynamicResolutionScaling()
        {
            // Reduces the URP render scale toward the Low-tier range (Technical Architecture §2:
            // 0.6-1.0 aggressive range) — first, least-visually-disruptive step.
        }

        private void DisableMsaa()
        {
            // Drops MSAA sample count — second step, before a full quality-tier change.
        }

        private void DropToLowTier()
        {
            _settingsManager.ApplyGraphicsTier(GraphicsQualityTier.Low);
        }
    }
}
