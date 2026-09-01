using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZoneStrike.Core.Interfaces;

namespace ZoneStrike.Infrastructure.SaveSystem
{
    /// <summary>
    /// Architecture role: local-device persistence (Technical Architecture §12 class diagram),
    /// implementing <see cref="ISaveSystem"/> so it can be swapped for an in-memory fake in
    /// EditMode tests or a future cloud-save implementation without touching any caller
    /// (GameManager, SettingsManager).
    ///
    /// Optimisation notes: file IO runs on a background thread via
    /// <see cref="UniTask.SwitchToThreadPool"/>, then hops back to the main thread only for the
    /// final Unity API touch-points — mobile storage writes can be slow/throttled and must never
    /// stall a frame (this directly protects the responsiveness goal in the GDD's player
    /// experience section).
    ///
    /// Extension points: <see cref="SaveData.SchemaVersion"/> exists specifically so a future
    /// field addition can run a migration step in <see cref="LoadAsync"/> rather than discarding
    /// old save data outright.
    ///
    /// Networking considerations: none — purely local. Exception-safety is the load-bearing
    /// concern here instead: a corrupted save file (partial write from an app kill mid-save, a
    /// storage error) must never crash the app or block boot — see the atomic
    /// write-to-temp-then-move pattern in <see cref="SaveAsync"/> and the fallback-to-defaults
    /// behaviour in <see cref="LoadAsync"/>.
    /// </summary>
    public sealed class SaveSystem : ISaveSystem
    {
        private readonly string _savePath;
        private readonly string _tempPath;

        public SaveSystem()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "save.json");
            _tempPath = _savePath + ".tmp";
        }

        public async UniTask SaveAsync(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            string json = JsonUtility.ToJson(data);

            await UniTask.SwitchToThreadPool();
            try
            {
                // Atomic write pattern: write to a temp file first, then move it over the real
                // save file. A crash/power-loss mid-write leaves the temp file corrupted but the
                // previous good save file untouched — never a half-written save.json.
                File.WriteAllText(_tempPath, json);

                if (File.Exists(_savePath)) File.Delete(_savePath);
                File.Move(_tempPath, _savePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Save write failed: {ex}");
                // Deliberately swallowed beyond logging — a failed save should not crash
                // gameplay; the player simply keeps their in-memory state for this session.
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        public async UniTask<SaveData> LoadAsync()
        {
            await UniTask.SwitchToThreadPool();
            SaveData result;

            try
            {
                if (!File.Exists(_savePath))
                {
                    result = new SaveData();
                }
                else
                {
                    string json = File.ReadAllText(_savePath);
                    result = JsonUtility.FromJson<SaveData>(json);

                    if (result == null)
                    {
                        Debug.LogWarning("[SaveSystem] Save file deserialized to null, falling back to defaults.");
                        result = new SaveData();
                    }
                    else if (result.SchemaVersion < 1)
                    {
                        result = MigrateLegacySave(result);
                    }
                }
            }
            catch (Exception ex)
            {
                // Corrupt/unreadable save file must never block boot (GameManager depends on
                // this) — fall back to defaults rather than propagating the exception.
                Debug.LogError($"[SaveSystem] Load failed, falling back to defaults: {ex}");
                result = new SaveData();
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            return result;
        }

        private static SaveData MigrateLegacySave(SaveData legacy)
        {
            legacy.SchemaVersion = 1;
            return legacy;
        }
    }
}
