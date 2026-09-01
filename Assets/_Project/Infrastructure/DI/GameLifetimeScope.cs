using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using ZoneStrike.Core.Interfaces;
using ZoneStrike.Infrastructure.Input;
using ZoneStrike.Infrastructure.SaveSystem;
using ZoneStrike.Infrastructure.Settings;
using ZoneStrike.Networking.Fusion;
using ZoneStrike.Presentation.Audio;
using ZoneStrike.Presentation.UI;

namespace ZoneStrike.Infrastructure.DI
{
    /// <summary>
    /// Architecture role: the app-lifetime VContainer composition root (Technical Architecture
    /// §3/§12) — wires every Infrastructure-layer singleton (Save, Settings, Network, Audio, UI,
    /// GameManager) exactly once at boot. This is the concrete object referred to as
    /// "GameLifetimeScope" throughout the Technical Architecture document's diagrams.
    ///
    /// Optimisation notes: registrations are all singleton-lifetime (one instance for the whole
    /// app session) except where a match-scoped lifetime is explicitly needed — those live in
    /// <see cref="MatchLifetimeScope"/> instead, so returning to the Hub can cleanly dispose
    /// match-only state without tearing down the whole app (the classic mobile memory-leak this
    /// split is designed to prevent, per Technical Architecture §3).
    ///
    /// Extension points: add a new Infrastructure-layer service by registering it here once;
    /// Gameplay/Presentation code requests it via constructor or [Inject] method injection,
    /// never via a manual singleton/service-locator lookup.
    ///
    /// Networking considerations: registers <see cref="NetworkManager"/> as the sole Networking-
    /// layer entry point available to the rest of the graph.
    ///
    /// Composition-root exception (see MatchLifetimeScope's fuller note on this): this class's
    /// asmdef references every other layer, including Presentation — because its entire purpose
    /// is wiring the full object graph. That is not the same as Presentation/Gameplay code
    /// referencing Infrastructure at runtime, which remains event-only per Technical
    /// Architecture §3.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private InputService _inputServicePrefabInstance;
        [SerializeField] private NetworkManager _networkManagerPrefabInstance;
        [SerializeField] private UIManager _uiManagerInstance;
        [SerializeField] private AudioManager _audioManagerInstance;
        [SerializeField] private UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset _lowTierAsset;
        [SerializeField] private UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset _midTierAsset;
        [SerializeField] private UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset _highTierAsset;

        protected override void Configure(IContainerBuilder builder)
        {
            // Infrastructure layer
            builder.Register<ISaveSystem, SaveSystem.SaveSystem>(Lifetime.Singleton);
            builder.Register<SettingsManager>(Lifetime.Singleton)
                .WithParameter(_lowTierAsset)
                .WithParameter(_midTierAsset)
                .WithParameter(_highTierAsset);

            // Networking layer — the one place a MonoBehaviour-based service is registered as a
            // component instance rather than a plain class (Fusion's NetworkRunner integration
            // requires MonoBehaviour lifecycle).
            builder.RegisterComponent(_networkManagerPrefabInstance).As<NetworkManager>();
            builder.RegisterComponent(_inputServicePrefabInstance).As<IInputSource>();

            // Presentation layer (app-lifetime UI/Audio shells — screen CONTENT is match-scoped
            // where relevant, but the manager instances themselves persist across Hub↔Match
            // transitions to avoid destroying/recreating the Canvas hierarchy).
            builder.RegisterComponent(_uiManagerInstance);
            builder.RegisterComponent(_audioManagerInstance);

            // App-level orchestrator
            builder.Register<GameManager>(Lifetime.Singleton);

            builder.RegisterEntryPoint<GameBootstrapper>();
        }
    }

    /// <summary>Thin VContainer entry point that kicks off GameManager's async boot sequence once the container is built.</summary>
    public sealed class GameBootstrapper : IStartable
    {
        private readonly GameManager _gameManager;

        public GameBootstrapper(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Start()
        {
            _gameManager.BootAsync().Forget();
        }
    }
}
