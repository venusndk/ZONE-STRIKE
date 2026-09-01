using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ZoneStrike.Infrastructure.AssetLoading
{
    /// <summary>
    /// Architecture role: thin Addressables wrapper used by GameManager to load the match-scene
    /// content bundle (Technical Architecture §6 "Core-Local" vs. remote group split) — kept as
    /// a small static utility rather than a full service, since it has no meaningful instance
    /// state of its own.
    ///
    /// Optimisation notes: always awaited via UniTask, never a blocking synchronous load — a
    /// synchronous Addressables load on the main thread during the match-loading transition
    /// would directly blow the GDD §4 ≤15s loading-stage budget on a slow storage device.
    ///
    /// Extension points: additional content groups (hero packs, seasonal cosmetics, Technical
    /// Architecture §6) get their own load methods here following the same
    /// AsyncOperationHandle→UniTask pattern, so per-hero lazy loading (only download a hero's
    /// assets on first select) is a straightforward addition.
    ///
    /// Networking considerations: none — Addressables content loading is independent of the
    /// Photon Fusion connection lifecycle; GameManager sequences them (connect, then load) but
    /// they are not coupled at this layer.
    /// </summary>
    public static class AddressableSceneLoader
    {
        private const string MatchContentLabel = "match-content-neocity";

        public static async UniTask LoadMatchContentAsync()
        {
            AsyncOperationHandle handle = Addressables.LoadAssetsAsync<GameObject>(MatchContentLabel, null);

            try
            {
                await handle.ToUniTask();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AddressableSceneLoader] Match content load failed: {ex}");
                throw; // GameManager's caller is responsible for surfacing a load-failure UI state.
            }
        }
    }
}
