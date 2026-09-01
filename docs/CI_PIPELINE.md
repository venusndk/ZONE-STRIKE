# CI/CD Pipeline — Status Note

Implements the pipeline designed in [`TECHNICAL_ARCHITECTURE.md`](TECHNICAL_ARCHITECTURE.md) §9: [`.github/workflows/build.yml`](../.github/workflows/build.yml) (PR checks + release build/deploy) and [`.github/scripts/check_budget.py`](../.github/scripts/check_budget.py) (the performance-budget gate from §2/§9/§10).

## Why every PR shows green right now — read this before assuming "green = tested"

A `check-prerequisites` job runs first and detects whether the `UNITY_LICENSE` repo secret is
set. Every Unity-dependent job (tests, budget gate, the three platform builds, deploy) is gated
on that: if the secret is present, the real job runs and its pass/fail is genuine; if it's
absent, a matching `*-skipped-no-license` job runs instead, succeeds, and posts an explicit
`::notice::` explaining why. This is a **deliberate, standard CI pattern** for a secret this
workflow has no way to provision for itself (see below) — it is not the same thing as the code
actually being tested. Once `UNITY_LICENSE` is added, these jobs stop skipping and start running
for real, with real results.

**Only you can close this gap** — `UNITY_LICENSE` requires your Unity account's license
activation, added via GitHub repo Settings → Secrets and variables → Actions. This cannot be
delegated to an assistant working in this repo, and shouldn't be — it's a credential, and pasting
it into a chat session would be the wrong way to provision it regardless. See game-ci's own
[unity-builder documentation](https://github.com/game-ci/unity-builder) for how to generate an
activation file (`.ulf`) for a personal license, or use `UNITY_EMAIL`/`UNITY_PASSWORD`/
`UNITY_SERIAL` instead for a Plus/Pro seat.

## What's actually verified vs. what's still pending

**Verified in this session:**
- `check_budget.py` runs correctly and does what it claims — tested locally against both a passing report (exit 0) and a deliberately budget-violating report (exit 1, naming the specific failed metrics: draw calls and steady-state GC allocation).
- `build.yml`'s YAML is syntactically valid (parsed successfully with PyYAML).

**Not yet true, flagged rather than assumed away:**
- The workflow has **not run successfully on GitHub Actions yet**, and will fail at the Unity license-activation step until two prerequisites are done:
  1. **Unity project scaffold.** This repo currently has `Assets/_Project/*.cs` source (Phase 4) but no `ProjectSettings/`, no `Packages/manifest.json` with URP/Fusion/VContainer/UniTask/Addressables/Cinemachine/Input System actually installed — i.e., no bootable Unity project yet. That's an Editor-side task (open Unity, create the project, install packages, save ProjectSettings) that hasn't happened in this text-only environment.
  2. **Secrets provisioning.** `UNITY_LICENSE`, Android keystore secrets, Apple signing secrets, and Fastlane credentials are not set in the GitHub repo's Actions secrets — these are account-level tasks for you, and correctly do not belong in source control (see `.gitignore`'s explicit secrets section).
- `ZoneStrike.Editor.BudgetGate.RunAndExport` — the Unity Editor method the `budget-gate` job calls to actually produce a `budget-report.json` from a real scene — does not exist yet. `check_budget.py` (the consumer of that report) is real and tested; the producer is a tracked follow-up, not yet built.
- Branch protection on `main` requiring the PR-check jobs to pass before merge is a GitHub repo *setting*, not something this workflow file can configure — recommend turning it on once the pipeline has run green at least once.

## Suggested next real milestone
Phase 3 Week 1's own acceptance criteria — a real Unity project connecting two Fusion clients to a dedicated server build — is the same milestone that would make this pipeline actually runnable, since it requires the same project scaffold. Treat "scaffold the Unity project" as the shared unblocking task for both.
