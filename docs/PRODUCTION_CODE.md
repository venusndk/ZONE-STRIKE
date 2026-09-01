# ZONE STRIKE — Production Code
**Phase 4 of 8 — Core Unity Systems**
Version 0.1 (Draft for Approval) · Date: 2026-09-01
Implements the 15 systems named in the brief, realizing the architecture from [`docs/TECHNICAL_ARCHITECTURE.md`](TECHNICAL_ARCHITECTURE.md) as actual code under [`Assets/_Project/`](../Assets/_Project/), sequenced per the build order in [`docs/MVP_ROADMAP.md`](MVP_ROADMAP.md) Weeks 1–8.

---

## 0. Scope note — what "production-ready" means for this phase, honestly

Every one of the 15 systems below is real, complete, documented C# following the Phase 2 architecture (VContainer DI, Fusion dedicated-server networking, ScriptableObject data, event-channel decoupling, object pooling, UniTask async). Two honest caveats, stated up front rather than glossed over:

1. **This code has not been compiled against a live Unity project with the actual package versions installed** (no Unity Editor is available in this environment) — it is written to compile against the Fusion 2 / VContainer / UniTask / Addressables APIs as documented, and every file that touches a third-party SDK's exact call signature carries an explicit code-comment flagging it for verification once imported into a real project (e.g. `Runner.LagCompensation.RaycastAll`, `NetworkInput.Set<T>()`). This is the same "verify against the installed SDK version" caveat the Phase 2 architecture doc itself flagged for Fusion generally — Phase 4 inherits it at the call-site level.
2. **8 heroes' worth of unique ability content is not fully hand-written here.** `HeroAbilitySystem` (the named system) is complete and production-ready as a *framework*; two reference `IAbilityEffect` implementations (Havoc's barrier, Aegis's heal beacon) prove the framework end-to-end, matching exactly the Phase 3 Week 5 scope ("prove the framework on the two most different heroes before applying it to the other 6 in Week 6"). Writing all 8 heroes' full kits here would front-load Week 6's actual sprint work into this document rather than delivering the framework the brief asked for as one of the 15 named systems.

Every script below carries its own doc-comment block covering **architecture role, optimisation notes, extension points, and networking considerations** — the brief's explicit per-script requirement — rather than this document re-stating each script's contents.

---

## 1. System-by-system map

| # | Brief-named system | File(s) | Layer |
|---|---|---|---|
| 1 | Player Controller | [`Gameplay/Movement/PlayerController.cs`](../Assets/_Project/Gameplay/Movement/PlayerController.cs) | Gameplay |
| 2 | Camera | [`Presentation/Camera/CameraController.cs`](../Assets/_Project/Presentation/Camera/CameraController.cs) | Presentation |
| 3 | Input System | [`Infrastructure/Input/InputService.cs`](../Assets/_Project/Infrastructure/Input/InputService.cs) | Infrastructure |
| 4 | Weapon System | [`Gameplay/Weapons/WeaponSystem.cs`](../Assets/_Project/Gameplay/Weapons/WeaponSystem.cs) (+ `IDamageable.cs`) | Gameplay |
| 5 | Inventory | [`Gameplay/Inventory/InventorySystem.cs`](../Assets/_Project/Gameplay/Inventory/InventorySystem.cs) (+ `WorldLootItem.cs`) | Gameplay |
| 6 | Hero Ability System | [`Gameplay/Heroes/HeroAbilitySystem.cs`](../Assets/_Project/Gameplay/Heroes/HeroAbilitySystem.cs) (+ `IAbilityEffect.cs`, `AbilityEffectRegistry.cs`, `AbilityEffects/*.cs`) | Gameplay |
| 7 | Game Manager | [`Infrastructure/DI/GameManager.cs`](../Assets/_Project/Infrastructure/DI/GameManager.cs) | Infrastructure |
| 8 | Match Manager | [`Gameplay/Match/MatchManager.cs`](../Assets/_Project/Gameplay/Match/MatchManager.cs) | Gameplay |
| 9 | Zone Manager | [`Gameplay/Match/ZoneManager.cs`](../Assets/_Project/Gameplay/Match/ZoneManager.cs) | Gameplay |
| 10 | Spawn Manager | [`Gameplay/Match/SpawnManager.cs`](../Assets/_Project/Gameplay/Match/SpawnManager.cs) | Gameplay |
| 11 | UI Manager | [`Presentation/UI/UIManager.cs`](../Assets/_Project/Presentation/UI/UIManager.cs) | Presentation |
| 12 | Audio Manager | [`Presentation/Audio/AudioManager.cs`](../Assets/_Project/Presentation/Audio/AudioManager.cs) | Presentation |
| 13 | Network Manager | [`Networking/Fusion/NetworkManager.cs`](../Assets/_Project/Networking/Fusion/NetworkManager.cs) | Networking |
| 14 | Save System | [`Infrastructure/SaveSystem/SaveSystem.cs`](../Assets/_Project/Infrastructure/SaveSystem/SaveSystem.cs) (+ `ISaveSystem.cs`) | Infrastructure |
| 15 | Settings Manager | [`Infrastructure/Settings/SettingsManager.cs`](../Assets/_Project/Infrastructure/Settings/SettingsManager.cs) (+ `PerformanceGovernor`) | Infrastructure |

**Supporting types** (not separately named in the brief, but required for the above to be real, wired, compilable code rather than isolated snippets): 5 ScriptableObject data definitions (`Core/Data/*.cs`), the event-channel framework (`Core/EventChannels/*.cs`), core interfaces (`Core/Interfaces/*.cs`), and the two VContainer composition roots (`Infrastructure/DI/GameLifetimeScope.cs`, `MatchLifetimeScope.cs`) that actually wire all 15 systems together per the Phase 2 §12 class diagram — a "production code" deliverable that didn't include the wiring would not be something a team could immediately build on, per the brief's own bar.

**Assembly definitions** (`ZoneStrike.Core.asmdef`, `.Gameplay`, `.Networking`, `.Presentation`, `.Infrastructure`) are included so the Phase 2 §3 layering rule is an enforced compile-time boundary, not just a diagram.

---

## 2. Cross-cutting patterns demonstrated

- **Server authority, everywhere:** every mutation of gameplay-critical state (`WeaponSystem`'s ammo/hit resolution, `HeroAbilitySystem`'s cooldowns, `ZoneManager`'s damage, `SpawnManager`'s Reinforcement Tokens) is gated behind `HasStateAuthority`, with client requests arriving only as RPCs the server independently validates — the concrete realization of every anti-cheat mitigation table row in Technical Architecture §5.5.
- **Data-driven balance:** every numeric value a designer would want to retune (weapon damage, ability cooldowns, zone timings, match rules) lives in a ScriptableObject, several implementing `IRemoteOverridable` so Live Ops can layer Firebase Remote Config on top without a code change — directly realizing Technical Architecture §7.
- **Event-channel decoupling where it earns its keep:** `UIManager` and `AudioManager` react to Gameplay state purely through `EventChannelSO<T>` subscriptions; `CameraController` uses a direct reference instead, because a camera rig's job intrinsically requires one. Both patterns are valid per the Phase 2 dependency graph (`Presentation → Gameplay` is a permitted solid arrow) — this phase deliberately demonstrates both rather than forcing one pattern where it doesn't fit, and the doc comments on each file explain which is used and why.
- **Composition over inheritance:** `HeroAbilitySystem` is one class shared by all 8 heroes; per-hero behavior is injected via `IAbilityEffect` strategy objects, not a growing inheritance hierarchy — directly satisfying the brief's engineering standard.

---

## 3. Internal Design Review (Self-Validation Pass)

- **Coherence check against Phase 2:** every system's networking behavior matches the Technical Architecture §12.1 sequence diagram exactly (client-predicted local feedback → RPC to server → server-side lag-compensated validation → confirm/reconcile) — `WeaponSystem.RequestFire`/`RPC_RequestFire`/`ResolveHit` is a direct implementation of that diagram, not a reinterpretation of it.
- **Coherence check against Phase 1:** `MatchManager`'s win/loss/draw logic implements GDD §5's three conditions exactly (squad elimination, hard-cap survivor comparison, simultaneous-KO draw); `SpawnManager`'s Reinforcement Token logic implements GDD §8's "one squad-shared token" rule as a server-validated counter, not a client-trusted flag.
- **Self-caught and fixed during this pass (documented rather than silently corrected):**
  1. `NetworkManager` and `PlayerController` initially used constructor-based dependency injection despite being `MonoBehaviour`/`NetworkBehaviour`-derived types, which Unity never constructs via `new` — fixed to VContainer's `[Inject]` method-injection pattern, with a doc-comment explaining why the pattern differs from the plain-C# services (`GameManager`, `SettingsManager`) that do use constructor injection.
  2. `SpawnManager.TryServerRedeployFromToken` originally contained a broken/dead `ref` expression attempting to alias a Fusion `[Networked]` property, left over from an earlier draft — removed in favor of the straightforward per-squad branch that was already present below it.
  3. Several Presentation-layer doc comments overstated "Presentation never references Gameplay directly" as an absolute project rule, which directly contradicted `CameraController`'s (correct, architecture-permitted) direct reference to `PlayerController`. Corrected the wording across `EventChannelSO.cs`, `UIManager.cs`, and `AudioManager.cs` to state the actual rule precisely: event-channel decoupling is a deliberate per-system choice for reactive UI/Audio, not a blanket ban, and the asmdef graph now reflects this (`ZoneStrike.Presentation.asmdef` references `ZoneStrike.Gameplay`).
  4. `MatchLifetimeScope`/`GameLifetimeScope` (Infrastructure) need to reference Gameplay/Presentation types to wire them via VContainer, which superficially looks like a layering violation against Technical Architecture §3's diagram — documented explicitly as the standard, narrow "composition root" exception (a DI root's job is whole-graph wiring; this is distinct from runtime code reaching backward across layers, which remains disallowed).
- **Anti-cheat mitigation check:** re-verified every Technical Architecture §5.5 threat row has a corresponding code-level control in this phase's systems — speed hacks (`PlayerController`'s server-side speed clamp), memory editing (ammo/health/cooldowns are all `[Networked]`, server-owned), replay/duplicate-claim (loot pickup and Reinforcement Token both use server-validated, race-safe claim logic).
- **Gap identified, not yet closed (flagged rather than papered over):** a dedicated `PlayerHealth`/downed-state component (implementing `IDamageable`/`IHealable`, owning the bleed-out/revive timers from GDD §8) is referenced throughout this phase's code (`WeaponSystem`, `ZoneManager`, `HealBeaconEffect` all call into it) but is not itself one of the 15 brief-named systems, so it was not built as a standalone deliverable here. It is explicitly called out as a Phase 3 Week 3-4/Week 8 implementation task in code comments (`IDamageable.cs`) so it isn't lost as an implicit dependency.

### Assumptions requiring explicit confirmation
1. **Fusion 2 API surface** used throughout (`[Networked]`, `[Rpc(...)]`, `TickTimer`, `Runner.LagCompensation.RaycastAll`, `NetworkInput.Set<T>()`) should be verified against the exact SDK version once it's imported into a real Unity project — flagged per-file, not a blanket unknown.
2. **The `PlayerHealth`/downed-state component gap** above should be scheduled explicitly in the next sprint work (Phase 3 Weeks 3-4/8), since multiple Phase 4 systems already assume its `IDamageable`/`IHealable` contract exists.
3. **NetworkPrefabRef wiring** for spawned objects (ability-effect prefabs in `DeployableBarrierEffect`/`HealBeaconEffect`, loot pickups) is left as an explicit `null`/TODO-equivalent in two small resolver methods — prefab-asset assignment is an Editor/content task, not a code-logic task, and was scoped out of this phase accordingly.

### Remaining risks (carried forward)
- Uncompiled code carries residual risk of small API-signature mismatches against the actual installed Fusion/VContainer/UniTask package versions — the Phase 3 Week 1 acceptance criteria (two clients connecting on a real dedicated-server build) is exactly the gate that will surface and resolve these before they compound.
- The `PlayerHealth` gap (above) blocks a fully working vertical slice until built — already scheduled in the existing Phase 3 roadmap, not a new risk, but worth restating here since this phase's code makes the dependency concrete.

### Completeness check against the Phase 4 brief
All 15 named systems ✅ implemented. Every script carries documentation ✅, architecture explanation ✅, optimisation notes ✅, extension points ✅, and networking considerations ✅ in its top-of-file doc comment, per the brief's explicit per-script requirement.

---

## 4. Deliverables Summary

- 34 C# files under `Assets/_Project/`, organized per the Phase 2 folder structure, implementing all 15 brief-named systems plus the supporting data/event/DI infrastructure needed for them to be real, wired code.
- 5 assembly definitions enforcing the Phase 2 layering rules as compile-time boundaries.
- 4 self-caught and corrected issues documented in §3 rather than silently fixed.
- 3 assumptions and 2 carried-forward risks flagged for your visibility, most consequential: the unbuilt `PlayerHealth` component that several systems here already depend on.

**Approve? (Y/N)**
*(A "Y" locks this as the Phase 4 baseline and — per the Git protocol — I'll branch `feature/phase-4-production-code`, diff-review, commit, and push, then stop for your merge. If "N," tell me which system(s) need revision.)*
