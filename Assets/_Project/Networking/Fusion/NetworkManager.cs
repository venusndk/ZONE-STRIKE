using System;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using VContainer;
using ZoneStrike.Core.Interfaces;

namespace ZoneStrike.Networking.Fusion
{
    /// <summary>
    /// Architecture role: the ONLY class outside the Networking assembly boundary that
    /// Infrastructure/Gameplay are allowed to depend on for connection lifecycle — wraps Photon
    /// Fusion's <see cref="NetworkRunner"/> in dedicated-server topology (Technical Architecture
    /// §5.1) behind a small async API (<see cref="ConnectToMatchAsync"/>/<see cref="Disconnect"/>).
    /// This is the "NetworkManagerWrapper" named in the Technical Architecture §12 class diagram.
    ///
    /// Optimisation notes: implements <see cref="INetworkRunnerCallbacks"/> directly rather than
    /// via a separate listener-forwarding component, avoiding an extra indirection layer on the
    /// hot input-polling path (<see cref="OnInput"/> is called every local simulation tick).
    ///
    /// Extension points: <see cref="HandleReconnect"/> is the seam for the Technical Architecture
    /// §5.4 reconnect/match-recovery flow — a full implementation extends this method to attempt
    /// resuming an existing session (via Fusion's reconnect token) before falling back to
    /// treating the player as eliminated once <see cref="MatchRulesSO.ReconnectGraceWindowSeconds"/>
    /// elapses (that elapsed-time bookkeeping itself lives server-side in MatchManager/SpawnManager,
    /// not here — this class only reports connection-state transitions).
    ///
    /// Networking considerations: this class IS the networking layer's boundary — every method
    /// on it either starts/stops a Fusion session or forwards a Fusion callback outward via a
    /// narrow, project-specific event, so nothing above this layer needs to know Fusion exists.
    /// Player input is sampled from the injected <see cref="IInputSource"/> and packed into
    /// <see cref="PlayerInputData"/> inside <see cref="OnInput"/> — this is the single point
    /// where local input becomes network traffic.
    ///
    /// NOTE: written against the Fusion 2 NetworkRunner/INetworkRunnerCallbacks API surface;
    /// verify exact method signatures against the installed Fusion SDK version.
    /// </summary>
    public sealed class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkRunner _runnerPrefab;
        [SerializeField] private string _regionPreference = "auto";

        private IInputSource _inputSource;
        private NetworkRunner _activeRunner;
        private UniTaskCompletionSource<bool> _connectCompletionSource;

        public event Action OnDisconnectedFromServer;
        public event Action<PlayerRef> OnPlayerJoined;
        public event Action<PlayerRef> OnPlayerLeft;

        public bool IsConnected => _activeRunner != null && _activeRunner.IsRunning;

        /// <summary>
        /// VContainer method injection, not a constructor — Unity, not the DI container, is
        /// responsible for constructing MonoBehaviour instances (they're attached to a
        /// GameObject, never `new`'d directly), so VContainer's <see cref="InjectAttribute"/>
        /// method-injection convention is used instead of the constructor injection seen on this
        /// project's plain-C# services (e.g. GameManager).
        /// </summary>
        [Inject]
        public void Construct(IInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        public async UniTask<bool> ConnectToMatchAsync()
        {
            if (IsConnected) return true;

            _activeRunner = Instantiate(_runnerPrefab);
            _activeRunner.AddCallbacks(this);

            _connectCompletionSource = new UniTaskCompletionSource<bool>();

            try
            {
                var result = await _activeRunner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Client, // dedicated-server topology (Technical Architecture §5.1): this build connects as a Client to a separately-deployed Server build
                    SessionName = null,          // resolved by the matchmaking service (outside this class's scope) before calling ConnectToMatchAsync
                    PlayerCount = 8,
                    CustomLobbyName = _regionPreference
                });

                return result.Ok;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Connect failed: {ex}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (_activeRunner == null) return;
            _activeRunner.Shutdown();
            _activeRunner = null;
        }

        // --- INetworkRunnerCallbacks: only the members relevant to this project's needs are
        // implemented meaningfully; the rest are required by the interface but intentionally
        // no-op for MVP scope (Phase 3 §0). ---

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (_inputSource == null) return;
            input.Set(_inputSource.SampleCurrentInput());
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) => OnPlayerJoined?.Invoke(player);
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) => OnPlayerLeft?.Invoke(player);

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            // Technical Architecture §5.4: a disconnect does not immediately eliminate the
            // player — a 60s reconnect grace window is owned server-side (MatchRulesSO); this
            // client-side callback only surfaces the UI-facing "reconnecting..." state.
            HandleReconnect(reason);
        }

        private void HandleReconnect(NetDisconnectReason reason)
        {
            OnDisconnectedFromServer?.Invoke();
            // A full implementation attempts NetworkRunner.StartGame with the same session name
            // and a stored reconnect token here, retried with backoff, before giving up.
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            _connectCompletionSource?.TrySetResult(false);
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}
