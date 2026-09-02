using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ZoneStrike.EditorTooling
{
    /// <summary>
    /// Architecture role: one-shot developer-onboarding utility. Installs every package the
    /// project's code depends on (Technical Architecture §8's package selection) via the Package
    /// Manager API rather than hand-editing `Packages/manifest.json` with guessed version
    /// numbers — Client.Add resolves each package to the latest version actually compatible
    /// with the installed Editor, which is safer than pinning a version string that might not
    /// exist for this Editor build.
    ///
    /// Usage: after a fresh clone, run via menu **ZoneStrike → Bootstrap → Install Required
    /// Packages**, or headless: `Unity.exe -batchmode -nographics -projectPath &lt;path&gt;
    /// -executeMethod ZoneStrike.EditorTooling.ProjectBootstrap.InstallPackagesHeadless`.
    ///
    /// Not a build-time dependency — lives under an `Editor/` folder so Unity excludes it from
    /// player builds automatically; safe to leave in the repo permanently as team tooling.
    ///
    /// IMPORTANT implementation note (learned the hard way — see git history): `Client.Add`
    /// returns an async request whose `IsCompleted` flag only advances when the Editor's main
    /// thread update loop is pumped. An earlier version of this script polled that flag inside a
    /// blocking `while (!request.IsCompleted) Thread.Sleep(...)` loop on the SAME main thread
    /// that -executeMethod runs on — which starves the very update pump the request depends on,
    /// deadlocking the Editor process permanently (reproduced repeatedly in headless batch mode
    /// before being diagnosed). Every package install below is driven by an
    /// <see cref="EditorApplication.update"/> callback instead, which never blocks the thread it
    /// depends on.
    ///
    /// NOTE: the VContainer/UniTask git-URL entries below are unpinned (track each project's
    /// default branch) as a pragmatic first pass — pin to a specific tag once a known-good
    /// version has been verified against this project's actual usage, for build reproducibility.
    /// </summary>
    public static class ProjectBootstrap
    {
        private static readonly string[] RegistryPackages =
        {
            "com.unity.render-pipelines.universal",
            "com.unity.inputsystem",
            "com.unity.addressables",
            "com.unity.cinemachine",
            "com.unity.textmeshpro",
        };

        private static readonly string[] GitPackages =
        {
            "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer",
            "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
        };

        private static Queue<string> _pending;
        private static List<string> _failures;
        private static bool _exitOnComplete;

        [MenuItem("ZoneStrike/Bootstrap/Install Required Packages")]
        public static void InstallPackagesMenu() => StartInstall(exitOnComplete: false);

        /// <summary>Headless entry point for `-executeMethod`. Exits the Editor process with a non-zero code if any package failed, so automation can detect failure.</summary>
        public static void InstallPackagesHeadless() => StartInstall(exitOnComplete: true);

        private static void StartInstall(bool exitOnComplete)
        {
            _pending = new Queue<string>();
            foreach (var p in RegistryPackages) _pending.Enqueue(p);
            foreach (var p in GitPackages) _pending.Enqueue(p);
            _failures = new List<string>();
            _exitOnComplete = exitOnComplete;

            InstallNext();
        }

        private static void InstallNext()
        {
            if (_pending.Count == 0)
            {
                Finish();
                return;
            }

            string packageIdOrUrl = _pending.Dequeue();
            Debug.Log($"[ProjectBootstrap] Installing {packageIdOrUrl} ...");
            var request = Client.Add(packageIdOrUrl);

            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                if (!request.IsCompleted) return;

                EditorApplication.update -= poll;

                if (request.Status == StatusCode.Success)
                {
                    Debug.Log($"[ProjectBootstrap] Installed {request.Result.packageId}");
                }
                else
                {
                    Debug.LogError($"[ProjectBootstrap] Failed to install {packageIdOrUrl}: {request.Error?.message}");
                    _failures.Add(packageIdOrUrl);
                }

                InstallNext();
            };
            EditorApplication.update += poll;
        }

        private static void Finish()
        {
            if (_failures.Count > 0)
            {
                Debug.LogError($"[ProjectBootstrap] {_failures.Count} package(s) failed to install: {string.Join(", ", _failures)}");
            }
            else
            {
                Debug.Log("[ProjectBootstrap] All required packages installed successfully.");
            }

            if (_exitOnComplete)
            {
                EditorApplication.Exit(_failures.Count > 0 ? 1 : 0);
            }
        }
    }
}
