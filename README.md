# Zone Strike

4v4 fast-paced hero battle royale for Android/iOS. Unity 6 LTS, Photon Fusion (dedicated-server), URP mobile. Full design-through-post-launch documentation and initial production code below.

## Documentation index

| # | Document | Covers |
|---|---|---|
| 1 | [`docs/GDD.md`](docs/GDD.md) | Game Design Document — core loop, economy, heroes/weapons/abilities, Neo City map, combat/TTK targets, monetisation, retention, risk analysis |
| 2 | [`docs/TECHNICAL_ARCHITECTURE.md`](docs/TECHNICAL_ARCHITECTURE.md) | Engineering spec — Unity/URP config, layered architecture (VContainer DI), Photon Fusion networking design, Addressables, performance/memory budgets, CI/CD design |
| 3 | [`docs/MVP_ROADMAP.md`](docs/MVP_ROADMAP.md) | 12-week sprint plan to a feature-complete internal vertical slice |
| 4 | [`docs/PRODUCTION_CODE.md`](docs/PRODUCTION_CODE.md) | Maps the 15 core systems to their source under [`Assets/_Project/`](Assets/_Project/) |
| 5 | [`docs/UI_UX_DESIGN.md`](docs/UI_UX_DESIGN.md) | Every screen — layout, hierarchy, navigation, accessibility, component library |
| 6 | [`docs/ART_AUDIO.md`](docs/ART_AUDIO.md) | Asset inventory, polygon/texture budgets, palette, VFX/animation/audio direction |
| 7 | [`docs/TESTING_LAUNCH.md`](docs/TESTING_LAUNCH.md) | QA strategy, device matrix, store optimisation, soft-launch gate, KPIs |
| 8 | [`docs/POST_LAUNCH.md`](docs/POST_LAUNCH.md) | Year 1 live-ops roadmap — seasons, new heroes/maps/modes, balance cadence |
| — | [`docs/CI_PIPELINE.md`](docs/CI_PIPELINE.md) | CI/CD pipeline status — what's verified vs. still pending |

Each document ends with its own self-review, flagged assumptions, and risk register — this README pulls the highest-priority items from all of them into one place below rather than duplicating each doc's full detail.

## Honest project status

Documentation for all 8 phases is complete and cross-checked against itself (each phase was built on, and re-verified against, the ones before it). **The game itself is not yet buildable or running.** Specifically:

- **No Unity project is scaffolded yet.** `Assets/_Project/` holds real, documented C# source (Phase 4) written against the Phase 2 architecture, but there is no `ProjectSettings/` or `Packages/manifest.json` — i.e., no bootable Unity project. This is the single blocking task for everything below it.
- **The Phase 4 code has never compiled.** No Unity Editor was available while it was written; every call into Fusion/VContainer/UniTask's exact API surface is flagged in-code for verification once a real project exists.
- **CI is real but not fully exercised.** [`.github/workflows/build.yml`](.github/workflows/build.yml) runs and its `check_budget.py` gate is tested — but the Unity-dependent jobs (tests, budget gate, platform builds) skip cleanly rather than run, pending the `UNITY_LICENSE` secret and the project scaffold above. `BudgetGate.RunAndExport`, the Editor-side script that job depends on, doesn't exist yet either.
- **A `PlayerHealth`/downed-state component is assumed by several Phase 4 systems** (`WeaponSystem`, `ZoneManager`, the heal-beacon ability effect) but wasn't itself one of the 15 named systems, so it isn't built yet.

**Next real milestone:** [`docs/MVP_ROADMAP.md`](docs/MVP_ROADMAP.md) Week 1 — scaffold the project and get two Fusion clients connected to a real dedicated-server build. That single milestone unblocks the CI pipeline, the `PlayerHealth` gap, and real compilation of everything in Phase 4 at once.

## Open items requiring your decision

Pulled from every phase's self-review, ranked by how much downstream work depends on each:

1. **The 8-player (2×4 squad) reading of "4v4 Battle Royale"** (GDD §1.3) — the single most load-bearing assumption in the whole project. Netcode bandwidth budgets, the MVP sprint plan, and Year 1's map-production pacing are all built on it. Worth one final explicit confirmation now that the full picture exists.
2. **Provisioning tasks only you can do:** a Photon Fusion account (MVP Week 1 Day-0 blocker), a Firebase project (blocks the Infrastructure layer), and the `UNITY_LICENSE` GitHub Actions secret (blocks CI actually running) — none of these can be delegated into this repo by an assistant; they're account-level actions.
3. **Team-size assumption** — every sprint/season pacing estimate (Phase 3 §1, carried through Phase 8) is sized against one assumed team roster. A different real team size should re-derate the cadence, not silently keep the same pace.
4. **Font sign-off** (Rajdhani/Inter for UI, Archivo Black for world signage) — needs confirming before Phase 6's Week 9 environment art pass bakes signage into shared textures, which is harder to undo after.
5. **AI-generated concept art policy** (Art & Audio §11) — confirm your studio is comfortable with Midjourney/Leonardo for concept/mood-board exploration specifically (not final assets) before the pipeline adopts it.
6. **Soft-launch region selection** (Testing & Launch §8) — needs a business-side call weighing marketing budget and planned server-region footprint, both outside any single doc's scope.
7. **Esports/competitive seeding deferred past Year 1** (Post-Launch §7) — flagged in case stakeholder expectations differ.

Everything else flagged across the 8 documents is either lower-stakes or explicitly and deliberately data-gated (e.g., Post-Launch hero #9's role, Map 2's design brief) rather than something needing a decision now.
