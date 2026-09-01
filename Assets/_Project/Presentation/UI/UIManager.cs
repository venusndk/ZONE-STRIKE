using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZoneStrike.Core.EventChannels;

namespace ZoneStrike.Presentation.UI
{
    // HeroSelect added in Phase 5 (docs/UI_UX_DESIGN.md §7) — the pre-match hero-pick screen was
    // always part of the GDD §14 flow but was missed from this enum when it was first written in
    // Phase 4; closing that gap here rather than letting the design doc and code silently drift.
    public enum ScreenId { Boot, Home, Locker, BattlePass, HeroSelect, Matchmaking, Loading, HUD, DeathRecap, Spectator, PostMatch, Settings }

    /// <summary>
    /// Architecture role: Presentation-layer screen-stack controller (GDD §3 game flow). Reacts
    /// to Gameplay-layer state changes exclusively via event-channel subscriptions
    /// (<see cref="_matchStateChannel"/>, <see cref="_playerDownedChannel"/>) rather than holding
    /// any reference to MatchManager/HeroAbilitySystem/etc. This is a deliberate, stricter choice
    /// for THIS system specifically (not a project-wide rule — Technical Architecture §3's
    /// dependency graph permits direct Presentation→Gameplay references, and CameraController
    /// uses one) — a screen-stack controller reacting to many different unrelated gameplay
    /// events is exactly the case where event-channel decoupling earns its keep, versus a
    /// growing list of direct manager references.
    ///
    /// Optimisation notes: screens are Addressables-loaded prefabs, instantiated once and
    /// hidden/shown via <c>SetActive</c> rather than destroyed/recreated on every navigation —
    /// protects the §10.1 UI frame-time budget (1.5ms) by avoiding repeated instantiation/layout
    /// rebuild cost, at the acceptable trade-off of holding a modest number of screen instances
    /// in memory simultaneously (bounded — this game has ~10 top-level screens per §0 MVP scope).
    ///
    /// Extension points: new screens register in <see cref="ScreenId"/> and get an Addressables
    /// key entry in <see cref="_screenAddressableKeys"/> — no changes to the stack-management
    /// logic itself are needed.
    ///
    /// Networking considerations: none directly — this class is local-only per client. It never
    /// mutates gameplay state; the Death Recap panel (GDD §9) it renders is populated from data
    /// the server already validated and sent down via RPC/event, never computed here.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private MatchStateEventChannelSO _matchStateChannel;
        [SerializeField] private PlayerDownedEventChannelSO _playerDownedChannel;
        [SerializeField] private Transform _screenRoot;
        [SerializeField] private SerializableScreenKeyMap[] _screenAddressableKeys;

        private readonly Dictionary<ScreenId, GameObject> _loadedScreens = new();
        private readonly Stack<ScreenId> _navigationStack = new();

        [System.Serializable]
        public struct SerializableScreenKeyMap
        {
            public ScreenId Id;
            public string AddressableKey;
        }

        private void OnEnable()
        {
            _matchStateChannel?.Subscribe(OnMatchStateChanged);
            _playerDownedChannel?.Subscribe(OnPlayerDowned);
        }

        private void OnDisable()
        {
            _matchStateChannel?.Unsubscribe(OnMatchStateChanged);
            _playerDownedChannel?.Unsubscribe(OnPlayerDowned);
        }

        public async UniTask ShowScreenAsync(ScreenId id, bool pushToStack = true)
        {
            if (!_loadedScreens.TryGetValue(id, out var screenInstance))
            {
                screenInstance = await LoadScreenAsync(id);
                if (screenInstance == null) return;
                _loadedScreens[id] = screenInstance;
            }

            foreach (var kvp in _loadedScreens)
            {
                kvp.Value.SetActive(kvp.Key == id);
            }

            if (pushToStack) _navigationStack.Push(id);
        }

        public void GoBack()
        {
            if (_navigationStack.Count <= 1) return;
            _navigationStack.Pop();
            var previous = _navigationStack.Peek();
            ShowScreenAsync(previous, pushToStack: false).Forget();
        }

        private async UniTask<GameObject> LoadScreenAsync(ScreenId id)
        {
            string key = ResolveAddressableKey(id);
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"[UIManager] No Addressable key configured for screen '{id}'.");
                return null;
            }

            try
            {
                var handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(key, _screenRoot);
                GameObject instance = await handle.ToUniTask();
                return instance;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManager] Failed to load screen '{id}' ({key}): {ex}");
                return null;
            }
        }

        private string ResolveAddressableKey(ScreenId id)
        {
            if (_screenAddressableKeys == null) return null;
            foreach (var entry in _screenAddressableKeys)
            {
                if (entry.Id == id) return entry.AddressableKey;
            }
            return null;
        }

        private void OnMatchStateChanged(MatchStateChangedPayload payload)
        {
            switch (payload.NewState)
            {
                case MatchState.Loading:
                    ShowScreenAsync(ScreenId.Loading).Forget();
                    break;
                case MatchState.Drop:
                case MatchState.ZoneHold:
                case MatchState.ZoneShrink:
                case MatchState.FinalZone:
                case MatchState.SuddenDeath:
                    ShowScreenAsync(ScreenId.HUD).Forget();
                    break;
                case MatchState.PostMatch:
                    ShowScreenAsync(ScreenId.PostMatch).Forget();
                    break;
            }
        }

        private void OnPlayerDowned(PlayerDownedPayload payload)
        {
            // Local-player-specific check (comparing against the local input-authority player id)
            // happens in the concrete implementation; simplified here to the architectural hook.
            ShowScreenAsync(ScreenId.DeathRecap, pushToStack: false).Forget();
        }
    }
}
