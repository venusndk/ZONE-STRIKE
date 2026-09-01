using Cysharp.Threading.Tasks;
using UnityEngine;
using ZoneStrike.Core.EventChannels;
using ZoneStrike.Core.Interfaces;
using ZoneStrike.Infrastructure.AssetLoading;
using ZoneStrike.Networking.Fusion;

namespace ZoneStrike.Infrastructure.DI
{
    public enum AppState { Boot, Hub, Matchmaking, Loading, InMatch, PostMatch }

    /// <summary>
    /// Architecture role: top-level, non-networked application state machine (Technical
    /// Architecture §3/§12 class diagram) — the root of the composition graph, wired by
    /// <c>GameLifetimeScope</c> at boot. Owns transitions between the Home Hub and a match
    /// (spinning up/tearing down <c>MatchLifetimeScope</c>, Technical Architecture §3), and is
    /// the single place SaveSystem/SettingsManager are loaded once for the whole app lifetime.
    ///
    /// Optimisation notes: match-scene content loads via Addressables + UniTask
    /// (<see cref="TransitionToMatchAsync"/>) rather than a blocking <c>SceneManager.LoadScene</c>
    /// call, keeping the loading-screen UI responsive and matching the ≤15s loading-stage budget
    /// from the GDD §4 match lifecycle table.
    ///
    /// Extension points: <see cref="AppState"/> is intentionally small (6 states) for MVP scope
    /// (Phase 3 §0); a full Ranked/Casual mode-select branch point is a Phase 7/8 addition to
    /// <see cref="TransitionToMatchmakingAsync"/>, not a restructuring of this class.
    ///
    /// Networking considerations: this class itself never touches Photon Fusion directly — it
    /// delegates connection lifecycle to <c>NetworkManager</c> (Networking layer) via DI,
    /// preserving the layering rule that only the Networking layer references Fusion types.
    /// </summary>
    public sealed class GameManager
    {
        private readonly ISaveSystem _saveSystem;
        private readonly NetworkManager _networkManager;
        private readonly VoidEventChannelSO _returnToHubRequestedChannel;

        public AppState CurrentState { get; private set; } = AppState.Boot;
        public SaveData ActiveSaveData { get; private set; }

        public GameManager(
            ISaveSystem saveSystem,
            NetworkManager networkManager,
            VoidEventChannelSO returnToHubRequestedChannel)
        {
            _saveSystem = saveSystem;
            _networkManager = networkManager;
            _returnToHubRequestedChannel = returnToHubRequestedChannel;
        }

        public async UniTask BootAsync()
        {
            CurrentState = AppState.Boot;

            // Null-safe/exception-safe per the engineering standards: a corrupt or missing save
            // must never block boot — LoadAsync's own implementation (SaveSystem) already falls
            // back to defaults internally, but this call site still guards defensively.
            try
            {
                ActiveSaveData = await _saveSystem.LoadAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManager] Save load failed unexpectedly, continuing with defaults: {ex}");
                ActiveSaveData = new SaveData();
            }

            _returnToHubRequestedChannel?.Subscribe(_ => ReturnToHubRequested());

            CurrentState = AppState.Hub;
        }

        public async UniTask TransitionToMatchmakingAsync()
        {
            if (CurrentState != AppState.Hub) return;

            CurrentState = AppState.Matchmaking;
            bool connected = await _networkManager.ConnectToMatchAsync();

            if (!connected)
            {
                Debug.LogWarning("[GameManager] Matchmaking connection failed, returning to Hub.");
                CurrentState = AppState.Hub;
                return;
            }

            await TransitionToMatchAsync();
        }

        private async UniTask TransitionToMatchAsync()
        {
            CurrentState = AppState.Loading;

            // Match-scene/content bundle load — Addressables handle awaited via UniTask, per
            // Technical Architecture §6/§11 async-loading standard (never a blocking load on
            // the main thread).
            await AddressableSceneLoader.LoadMatchContentAsync();

            CurrentState = AppState.InMatch;
        }

        public void NotifyMatchEnded()
        {
            CurrentState = AppState.PostMatch;
        }

        private void ReturnToHubRequested()
        {
            _networkManager.Disconnect();
            CurrentState = AppState.Hub;
        }
    }
}
