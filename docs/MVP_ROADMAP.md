# ZONE STRIKE — MVP Roadmap
**Phase 3 of 8 — 12-Week Sprint Plan**
Version 0.1 (Draft for Approval) · Date: 2026-09-01
Builds on [`docs/GDD.md`](GDD.md) (design) and [`docs/TECHNICAL_ARCHITECTURE.md`](TECHNICAL_ARCHITECTURE.md) (engineering) — this plan sequences the work those two documents describe into an executable order.

---

## 0. MVP Scope — what these 12 weeks build (and explicitly don't)

The brief asks for a 12-week plan; a *complete* AAA live-service game (ranked ladder, full 50-tier Battle Pass content, live IAP, App/Play Store submission) is not honestly achievable in 12 weeks by a normal-sized team, and pretending otherwise would make this plan fiction rather than something "a professional Unity team could immediately begin implementing" (the brief's own stated bar). So this roadmap targets a concrete, defensible milestone: **a feature-complete internal vertical slice** — the full 4v4 core loop, all 8 heroes, the full Neo City map, working at the Phase 2 performance budgets, ready for the internal Alpha playtest that validates the Phase 1 GDD's flagged balance hypotheses (TTK, zone timers, Reinforcement Token feel).

| In scope for this 12-week MVP | Deferred to later phases (Phase 7/8 territory) |
|---|---|
| Full 4v4 core loop (drop → loot → zone → combat → win/loss) on Neo City | Ranked ladder / MMR (needs a live population to tune against) |
| All 8 launch heroes, all 5 weapon archetypes + rarity tiers | Full 50-tier Battle Pass *content* (art/cosmetics pipeline) — the tier/UI scaffold is built, populated later |
| Casual matchmaking (skill-agnostic, party-aware) | Live real-money IAP (needs store-account/legal setup, Phase 7) |
| Respawn/Reinforcement Token, Spectator System | Live Ops tooling (A/B testing, seasonal event system) |
| Core HUD, menus, matchmaking flow | Full device compatibility matrix certification (Phase 7) |
| Photon Fusion dedicated-server netcode, anti-cheat mitigations from Phase 2 | Third-party security audit (scheduled Phase 7) |
| Adaptive music/SFX, base VFX pass | Marketing/ASO assets, store listing polish |

This scope split is itself a design decision worth flagging: it means Weeks 1–12 produce a build good enough to **playtest and greenlight**, not a build ready to **ship**. Phases 4–8 in this engagement continue directly from this milestone rather than restarting a new 12-week clock.

---

## 1. Sprint Cadence & Team Assumption

- **Sprint length:** 1 week (matches the brief's "every week must include..." structure) with a Friday review/demo.
- **Assumed core team (for planning realism, not prescriptive):** 1 Technical Director/Lead, 2 Gameplay Engineers, 1 Network Engineer, 1 UI Engineer, 1 Environment Artist, 1 Character Artist, 1 Animator, 1 VFX Artist, 1 Audio Designer, 1 QA Engineer, 1 Producer. Flagged as an assumption (§13) — the plan's per-week backlog is sized against this roster; a smaller team stretches the timeline proportionally rather than the plan being wrong.
- **Definition of Done (applies every week):** merges to `main` via the Phase 1/2 git protocol (feature branch → CI-gated PR → human review/merge), meets that week's acceptance criteria, and passes the Phase 2 §9 CI budget gates (draw calls, GC alloc, build size trend) before being considered complete — not just "code exists."
- **Note on greybox art:** weeks 1–6 use greybox/whitebox placeholder geometry (primitive capsules, blockout rooms) for programmer-testing purposes. This is standard AAA production practice, not the "placeholder content" the brief's Golden Rules forbid — that rule targets shipped/documented deliverables pretending to be finished; greyboxing is scaffolding that gets replaced on a scheduled week (Week 9 for the map, Weeks 5–6 for hero silhouettes moving to real art in Week 9's polish pass), tracked explicitly below so it never silently ships.

---

## Week 1 — Foundation & Netcode Bring-Up

**Objectives:** stand up the Unity project exactly per the Phase 2 architecture spec, and prove the highest-risk unknown first — that Photon Fusion dedicated-server connect/predict/reconcile actually works end-to-end — before any gameplay is built on top of it.

| Discipline | Backlog |
|---|---|
| Design | Finalize Week 1–4 numeric defaults (movement speed/accel) as `MovementConfigSO` starting values, sourced from GDD §12 intent |
| Programming | Project scaffold matching Phase 2 §4 folder/asmdef structure; `GameLifetimeScope` (VContainer) boot composition; Photon Fusion project provisioned (Day 0 task, blocks everything else); headless server build target wired into CI; `NetworkManagerWrapper` connects 2 clients to 1 dedicated server session; basic `PlayerController` movement replicated with client prediction + server reconciliation visible under artificial latency |
| Art | None (greybox capsule placeholders only, per §1 note) |
| Animation | None |
| VFX | None |
| Audio | None |
| UI | Debug-only overlay (connection state, ping, tick rate) for engineering use |
| QA | Manual 2-device connect/move/latency-injection smoke test; log a baseline perf capture |
| Optimisation | Confirm Phase 2 §9 CI budget-gate checks actually run and fail correctly on a deliberately-broken test scene (validates the gate itself works) |
| Review | Friday: demo 2 clients moving in sync on a dedicated server build |

**Acceptance criteria:** two clients connect to a headless dedicated-server build over the internet (not just localhost), see each other's predicted+reconciled movement, and a deliberate 150ms artificial-latency test shows correction without a hard visual snap.

**Risks & mitigation:** Photon account/billing provisioning delay → assign as the literal first task Monday morning, escalate same-day if blocked. Fusion's learning curve for the team → budget a half-day Fusion sample-project walkthrough before writing project code.

**Completion checklist:** ☐ repo scaffold matches Phase 2 folder spec ☐ CI pipeline green on an empty scene ☐ 2-client dedicated-server connect/move/reconcile demoed ☐ PR merged per git protocol.

---

## Week 2 — Movement & Camera Feel

**Objectives:** turn "basic replicated movement" into the actual GDD §12 movement kit — the thing players will spend the whole match doing, so it gets its own dedicated week before combat is layered on.

| Discipline | Backlog |
|---|---|
| Design | Tune sprint-auto-trigger threshold, slide momentum curve, vault detection radius; define the touch-input dead-zone/sensitivity curve for the twin-stick scheme |
| Programming | Sprint/slide/context-vault implementation; Cinemachine third-person combat camera rig; Input System action maps for touch (dual virtual stick) + MFi/Android gamepad, with the input-abstraction layer from Phase 2 §1 |
| Art | Greybox vault/traversal test geometry (stairs, ledges, low walls) |
| Animation | Locomotion blend tree (idle/walk/run/slide) on a placeholder rig, root-motion-off (fully code-driven per Phase 2 mobile-perf stance) |
| VFX | Footstep dust (pooled, budget-tested against the §10 VFX frame-time allocation) |
| Audio | Placeholder footstep/slide SFX hooked to `AudioManager` stub |
| UI | Virtual joystick + action-button touch UI (thumb-zone-aware per GDD §20) |
| QA | Manual traversal test pass across all greybox geometry; touch-input usability pass on 2 physical device sizes |
| Optimisation | Profile movement code path for the §10.3 zero-steady-state-GC target; fix any per-frame allocations found |
| Review | Friday: playable movement demo, both touch and gamepad input |

**Acceptance criteria:** a player can sprint/slide/vault fluidly on both touch and gamepad input with server-reconciled positions staying visually stable; movement code shows 0B/frame steady-state GC alloc in the Profiler.

**Risks & mitigation:** touch-input feel is subjective and hard to nail in one week → treat Week 2's touch tuning as a first pass, not final; schedule a dedicated feel-tuning revisit in Week 11 once real playtesters (not just the dev team) provide feedback.

**Completion checklist:** ☐ all movement verbs implemented and networked ☐ dual input schemes functional ☐ 0B/frame GC confirmed in Profiler ☐ PR merged.

---

## Week 3 — Weapon System & Server-Authoritative Hit Registration

**Objectives:** implement the hardest correctness-critical system early — hit registration — while the team is small-surface-area enough to get it right, per Phase 2 §5.2/§5.5's lag-compensated, server-validated design.

| Discipline | Backlog |
|---|---|
| Design | `WeaponDefinitionSO` schema finalized (damage, fire rate, falloff, rarity multipliers) for the 5 archetypes (GDD §15); first-pass numeric values for AR and SMG only this week |
| Programming | `WeaponSystem`: client-predicted fire (immediate local feedback) + server-side lag-compensated rewind hit validation per Phase 2 §12.1 sequence; ammo/reload state server-authoritative; hit-confirmation RPC → damage application |
| Art | Greybox AR + SMG models (silhouette-correct, not final-textured) |
| Animation | Fire/reload animation stubs on placeholder rig |
| VFX | Muzzle flash, impact VFX (pooled) for the 2 weapons |
| Audio | Placeholder fire/reload/impact SFX |
| UI | Basic crosshair, ammo counter |
| QA | Hit-registration accuracy test: fire at a stationary + moving target under simulated 50/100/150ms latency, log hit-confirm accuracy |
| Optimisation | Validate weapon-fire RPC traffic against the Phase 2 §5.3 bandwidth budget (≤8KB/s down target) |
| Review | Friday: 1v1 skirmish demo with AR/SMG, hits visibly confirming correctly under latency |

**Acceptance criteria:** server-validated hits register correctly (no "shot behind cover" desync) for both weapons under up to 150ms simulated latency; no ammo/health value is ever client-authoritative (verified by a QA test that edits client-local values and confirms the server overrides them, directly proving the Phase 2 §5.5 memory-editing mitigation).

**Risks & mitigation:** lag-comp rewind is genuinely one of the harder netcode problems → if Fusion's built-in tooling doesn't cover it out of the box, escalate to the Network Engineer as a focused spike immediately rather than letting it silently slip into Week 4.

**Completion checklist:** ☐ 2 weapon archetypes fully networked and server-validated ☐ latency test results logged ☐ client-memory-edit test proves server authority ☐ PR merged.

---

## Week 4 — Combat Loop Complete: Remaining Weapons, Armor, First TTK Pass

**Objectives:** finish the weapon roster and get a real (not theoretical) first read on the GDD §18 TTK target.

| Discipline | Backlog |
|---|---|
| Design | Numeric tuning for Shotgun, DMR, Sidearm archetypes; armor tier damage-reduction values (§18: ~15-30%/tier) |
| Programming | Remaining 3 weapon archetypes on the Week 3 framework (should be fast — framework, not new systems); armor pickup/apply logic; rarity-tier stat scaling (flat % per §15) |
| Art | Greybox Shotgun/DMR/Sidearm models |
| Animation | Fire/reload variants per archetype (shotgun pump, DMR bolt-action feel, sidearm quick-draw) |
| VFX | Archetype-appropriate muzzle/impact VFX |
| Audio | Distinct fire/reload SFX per archetype (critical for the GDD §24 audio-as-competitive-system goal — even placeholder SFX should be spatially distinct starting now) |
| UI | Weapon-switch UI, armor indicator |
| QA | **First internal TTK measurement pass**: controlled 1v1s across all 5 weapons/3 rarities, log actual time-to-kill against the 1.8–2.2s (Common/Rare) / 1.0–1.3s (headshot) targets from GDD §18 |
| Optimisation | Draw-call check with 5 weapon models + VFX active simultaneously against Phase 2 §2 budget |
| Review | Friday: TTK data review — first real validate/revise decision point for a GDD hypothesis |

**Acceptance criteria:** all 5 weapon archetypes fully implemented and networked; logged TTK data exists (even if it doesn't perfectly match the target yet — that's what this measurement is for) with a documented delta vs. the GDD target and a proposed tuning adjustment if needed.

**Risks & mitigation:** if measured TTK is significantly off-target, resist the urge to "fix it live" mid-sprint with ad hoc number changes — log the delta, apply a considered adjustment to the `WeaponDefinitionSO` values, and re-measure next available QA pass (Week 11's full playtest is the real validation gate; Week 4 is an early-warning signal, not final balance).

**Completion checklist:** ☐ all 5 archetypes + 4 rarity tiers implemented ☐ armor system functional ☐ TTK data logged and compared to GDD target ☐ PR merged.

---

## Week 5 — Hero Ability System Framework + First 2 Heroes

**Objectives:** build the ability system as a reusable framework (not hero-specific spaghetti), proven correct with one Vanguard and one Medic — the two roles most structurally different from each other (shield/mobility vs. heal/revive), so the framework is stress-tested by variety immediately.

| Discipline | Backlog |
|---|---|
| Design | `AbilityDefinitionSO` schema (cooldowns, resource costs, VFX/SFX hooks); full kit spec for Havoc (Vanguard) and Aegis (Medic) per GDD §14 |
| Programming | `HeroAbilitySystem`: server-authoritative ability cast validation (cooldown/resource checks) + client-predicted cast feedback, per Phase 2's networking pattern extended from weapons to abilities; Havoc's deployable barrier (collision-blocking networked object) and Ground Slam (AoE); Aegis's Heal Beacon (networked AoE HoT) and faster-revive-channel modifier |
| Art | Greybox silhouettes for Havoc/Aegis (distinct read from each other and from generic soldiers, per GDD §23 readability rule) |
| Animation | Ability-cast animation stubs |
| VFX | Barrier deploy/break VFX, slam AoE VFX, heal-beacon pulse VFX — each with a readable pre-cast tell (GDD §19 counterplay-first rule) |
| Audio | Ability cast/impact SFX, distinct per hero |
| UI | Ability-cooldown HUD icons (2 slots + ultimate) |
| QA | Verify the "no ability can solo-delete a revive window" constraint (GDD §16) holds for both heroes' current numbers; verify ability-cast RPCs can't be spammed past server-validated cooldowns (a client-side cooldown bypass attempt should be rejected) |
| Optimisation | Networked-ability RPC traffic check against bandwidth budget with abilities added to weapon fire traffic |
| Review | Friday: Havoc vs. Aegis 1v1 demo showing the ability framework working end-to-end |

**Acceptance criteria:** both heroes fully playable with server-validated abilities; a deliberate client-side cooldown-bypass QA test is rejected by the server (proves the framework's authority model, not just this hero's).

**Risks & mitigation:** building the framework generically enough for 8 very different kits in one week's foundation is the real risk → intentionally chose the two most different roles (Vanguard/Medic) to surface framework gaps now rather than in Week 6 when 6 more heroes are being built on top of it.

**Completion checklist:** ☐ ability framework implemented ☐ Havoc + Aegis fully playable and networked ☐ cooldown-bypass QA test passed ☐ PR merged.

---

## Week 6 — Remaining 6 Heroes: Full Roster Playable

**Objectives:** with the framework proven, implement Titan, Blaze, Nova, Ghost, Raptor, Circuit — this week is framework-application, not framework-design, which is why it can absorb 6 heroes where Week 5 only did 2.

| Discipline | Backlog |
|---|---|
| Design | Kit specs for remaining 6 heroes per GDD §14; comp-warning UI logic spec (GDD §14 composition rule) |
| Programming | Titan (aura + grapple-pull), Blaze (incendiary + fire-rate ramp), Nova (marksman passive + dash), Ghost (blink + partial-cloak), Raptor (personal grapple + wall-run), Circuit (recon drone + crawl-speed passive) — all on the Week 5 framework |
| Art | Greybox silhouettes for remaining 6, each checked against the others for silhouette-distinctness (readability rule) |
| Animation | Cast/traversal animation stubs (Raptor's wall-run and Ghost's blink need custom locomotion states) |
| VFX | Per-hero ability VFX with readable tells |
| Audio | Per-hero ability SFX |
| UI | Full 8-hero select screen; ability-cooldown HUD generalized for all kits |
| QA | Full 8-hero regression pass: every ability server-validated, no hero breaks the "no solo-wipe a revive window" rule, comp-warning UI triggers correctly on duplicate-hero/no-Medic squads |
| Optimisation | Full-roster VFX/audio budget check (worst case: 4 different abilities active simultaneously in one team fight) |
| Review | Friday: full 8-player, 8-hero match demo (first time all 8 players + all 8 heroes exist simultaneously) |

**Acceptance criteria:** all 8 heroes fully implemented, networked, and pass the QA regression pass above; an 8-player test match runs without a framework-level crash or desync.

**Risks & mitigation:** 6 heroes in 1 week is the single densest week in this plan → if any hero's ability doesn't cleanly fit the framework (e.g., Raptor's wall-run touching movement-system code, not just ability code), flag immediately and pull in a second engineer rather than letting one hero block the week; Ghost's partial-cloak specifically needs a security review (a "reveal true position to server, hide from other clients" pattern) — treat as the week's technical-risk hero.

**Completion checklist:** ☐ all 8 heroes implemented ☐ full regression pass green ☐ 8-player/8-hero match demoed without crash ☐ PR merged.

---

## Week 7 — Match, Zone & Spawn Systems

**Objectives:** wrap the now-complete combat sandbox in the actual battle royale structure — drop, shrinking zone, win/loss — per GDD §4/§5/§11.

| Discipline | Backlog |
|---|---|
| Design | Finalize `MapZoneConfigSO` starting values from GDD §11's ring/damage table; `MatchRulesSO` (Reinforcement Token count, win/loss/draw conditions) |
| Programming | `MatchManager` (lifecycle per GDD §4 table, server-authoritative), `ZoneManager` (5-ring shrink + escalating damage + sudden-death overtime), `SpawnManager` (drop-phase glide, squad landing-zone selection) |
| Art | Zone-ring visual shader (readable, colorblind-safe per GDD §20) |
| Animation | Glide/drop animation |
| VFX | Zone-wall damage-tick VFX |
| Audio | Zone-shrink warning stinger, drop-phase wind/glide audio |
| UI | Zone-timer/minimap-ring HUD, drop-phase deployment UI |
| QA | Full-match timing test: verify actual elapsed time against the GDD §4/§11 target (8:00 soft, 10:00 hard cap) across multiple test matches; verify win/loss/draw resolution logic against all 3 conditions (elimination, final-zone-hold, simultaneous-KO draw) |
| Optimisation | Zone-state replication bandwidth check (should be near-zero per Phase 2 §5.3 — infrequent, on-change only) |
| Review | Friday: full match start-to-finish demo including a sudden-death overtime case |

**Acceptance criteria:** a complete match runs from drop to win/loss/draw resolution, hitting the 8-10 minute window in QA timing tests; all 3 end conditions have been triggered and verified at least once.

**Risks & mitigation:** zone timing math was validated arithmetically in the GDD's own self-review (§29) but never run in a real build — if actual test-match timing drifts from the documented ~9:05 total, treat it as confirming §27's flagged risk (unvalidated hypothesis) and adjust `MapZoneConfigSO`, not code.

**Completion checklist:** ☐ MatchManager/ZoneManager/SpawnManager implemented ☐ full match timing verified in real builds ☐ all 3 end conditions tested ☐ PR merged.

---

## Week 8 — Loot, Inventory, Respawn & Spectator Systems

**Objectives:** close the remaining core-loop gaps — the actual loot economy players scavenge through, and what happens when a player dies — per GDD §8/§9/§17.

| Discipline | Backlog |
|---|---|
| Design | Loot spawn-weight table per zone (GDD §17: Mall/Park/Rooftops/connective-zone weighting); deterministic-seed loot generation spec |
| Programming | `InventorySystem` (pickup/equip/2-slot attachments per GDD §15); deterministic server-seeded loot spawner; Downed-state/bleed-out/revive/finish logic; single squad-shared Reinforcement Token redeploy at Comms Beacons; Spectator free-cam + preset-ping system (GDD §9) |
| Art | Loot crate/pickup greybox models, death-drop container |
| Animation | Downed-state crawl animation, revive-channel animation |
| VFX | Pickup glow (rarity-color-coded), revive-channel VFX |
| Audio | Pickup/equip SFX, Downed-state audio cue, revive-complete stinger |
| UI | Inventory/loadout HUD, Downed-state UI (bleed-out timer, revive prompt), Death Recap panel (GDD §9), Spectator UI with preset pings |
| QA | Loot-seed determinism test (same match seed → same loot layout, needed for the anti-cheat validation tie-in from Phase 2 §17); full Downed→revive→Reinforcement-Token→redeploy flow test |
| Optimisation | Loot-pickup pooling check (many crates on a compact map — verify no per-pickup GC allocation) |
| Review | Friday: full death→spectate→revive→reinforcement flow demo |

**Acceptance criteria:** loot spawns deterministically from the server seed; a full squad-death-and-recovery cycle (Downed → revive attempt OR bleed-out → Reinforcement Token redeploy) works end-to-end; Spectator mode is fully functional with working preset pings and a rejoin-queue button.

**Risks & mitigation:** the Reinforcement Token is explicitly flagged in GDD §27 as needing feel-validation (does it undercut BR stakes?) → Week 8 only needs it *functionally correct*; the *feel* judgment is deferred to Week 11's playtest, not decided here.

**Completion checklist:** ☐ inventory/loot systems implemented ☐ deterministic seeding verified ☐ full death/revive/reinforcement flow tested ☐ spectator system functional ☐ PR merged.

---

## Week 9 — Neo City: Greybox → Art Pass

**Objectives:** this is the week the map (and hero silhouettes) graduate from greybox to real art direction (GDD §23), now that every gameplay system that depends on map geometry (loot placement, zone rings, vault points, sightlines) has been proven correct against the greybox version.

| Discipline | Backlog |
|---|---|
| Design | Finalize loot-density placement against real geometry (may shift slightly from the greybox pass — expected, not a regression) |
| Programming | Navmesh/traversal-point (vault/grapple/zipline) re-baking against final geometry; any collision-shape fixes the art pass introduces |
| Art | **Full Neo City art pass**: Mall (multi-floor indoor), Park (open-field/long-sightline), Rooftops (vertical/zipline network), Transit Hub, Alley Row, Parking Structure — per GDD §10's palette/readability direction; enemy-silhouette-vs-background contrast explicitly checked per POI (GDD §23 competitive-fairness rule) |
| Animation | N/A this week (heroes stay on placeholder rig; animation polish is Week 10-11 territory, not blocking on map art) |
| VFX | POI ambient/neon lighting VFX (Mall magenta/teal, Park amber, Rooftops cold blue-white per GDD §23) |
| Audio | Zone-ambient audio beds per POI (supports the GDD §24 "learn which POI you're near by ambient sound/color" navigation aid) |
| UI | Minimap art pass matching final map layout |
| QA | Full sightline/readability QA pass: walk every POI checking enemy-model contrast against final backgrounds in both light-heavy (Park) and dark-indoor (Mall) conditions |
| Optimisation | **Critical week for the draw-call/polygon budget (Phase 2 §2/§10)** — first real art assets means first real perf risk; profile immediately as art lands, not at the end of the week |
| Review | Friday: full art-directed Neo City walkthrough demo |

**Acceptance criteria:** Neo City fully art-passed across all 6 named zones; sightline/readability QA finds no enemy-silhouette-blending failures; draw-call count with final art stays within the Phase 2 §2 budget (≤120 Low tier / ≤180 Mid/High) — if it doesn't, this week does not close until it does, since perf budget violations compound badly if allowed to ship past this gate.

**Risks & mitigation:** art passes are the single most likely week to blow the performance budget → the optimisation backlog item explicitly runs *concurrently* with art landing (not after), so a budget-busting asset is caught and revised same-day, not discovered in Week 11.

**Completion checklist:** ☐ all 6 zones art-passed ☐ readability QA passed ☐ draw-call budget confirmed met with final art ☐ PR merged.

---

## Week 10 — UI/UX Integration & Audio Pass

**Objectives:** connect the Home Hub / matchmaking / menu flow (built in isolation so far as debug UI) into the real player-facing flow, and bring audio up from placeholder to direction-accurate.

| Discipline | Backlog |
|---|---|
| Design | Finalize Home Hub navigation flow (GDD §3) for MVP scope (Play/Locker/Social visible; Battle Pass/Store shown as scaffolded-but-content-light per §0 scope split) |
| Programming | Screen-controller implementation for Home Hub, matchmaking queue flow, post-match summary, party system (up to 4, per GDD §21) |
| Art | Final HUD/menu art pass (in-match HUD elements finalized from Week 2-8's functional placeholders) |
| Animation | Hero locomotion/combat animation pass moves from placeholder to final-quality (this is the week hero animation catches up, now that all 8 kits are functionally proven) |
| VFX | Menu/transition VFX polish |
| Audio | **Full adaptive music implementation** (GDD §24: ambient loot-phase stem → rising shrink-phase layer → combat stem, crossfaded); final SFX pass replacing all placeholder audio from Weeks 3-8; hero ability barks |
| UI | Full HUD finalized, matchmaking/party UI, post-match summary screen, Death Recap final art |
| QA | Full menu-flow QA (cold-start → match → post-match → back to hub, per GDD §3's ≤45s cold-start-to-match KPI — first real measurement) |
| Optimisation | UI draw-call/batching check (Phase 2 §10.1 UI frame-time budget: 1.5ms) |
| Review | Friday: full cold-start-to-post-match demo, first time the whole player-facing flow exists end-to-end |

**Acceptance criteria:** a player can go from app cold-start through Home Hub, matchmaking, a full match, and post-match summary without hitting a debug/placeholder screen; cold-start-to-first-match timing measured (compare against the ≤45s GDD §3 target, log delta if missed).

**Risks & mitigation:** this is the first week UI, art, and audio all land together — integration bugs (wrong SFX trigger, HUD element misaligned on a new device aspect ratio) are expected → QA time is weighted toward integration testing, not new-feature testing, this week.

**Completion checklist:** ☐ full player-facing flow implemented ☐ adaptive audio functional ☐ cold-start timing measured ☐ PR merged.

---

## Week 11 — Optimization Pass, Device Matrix & Internal Alpha Playtest

**Objectives:** this is the week the Phase 1 GDD's flagged hypotheses (§27/§29) get real data, and the Phase 2 performance budgets get validated against actual mid-range hardware, not just the dev team's development machines.

| Discipline | Backlog |
|---|---|
| Design | Own the playtest data review session; prepare the specific questions the playtest needs to answer (does the Reinforcement Token feel right? is TTK too fast/slow? does the zone pace feel tense or draggy?) |
| Programming | Fix issues surfaced by profiling (GC allocation regressions, draw-call creep, any desync found under real network conditions rather than simulated latency) |
| Art | Any asset-budget trims needed based on Week 11's profiling data |
| Animation | Final animation polish pass based on playtest feedback on "game feel" |
| VFX | Any VFX-budget trims needed |
| Audio | Mix pass based on playtest feedback (directional-audio clarity is a stated competitive requirement, GDD §24 — verify it's actually working for real players, not just the dev team who already know where sounds should come from) |
| UI | Fixes for any UI usability issues surfaced by playtesters unfamiliar with the game |
| QA | **Device compatibility spot-check** (not the full Phase 7 matrix, but a representative 3GB-RAM-class Android device + one iOS device, confirming the 60→30 FPS `PerformanceGovernor` fallback actually triggers correctly under thermal load); **Internal Alpha Playtest**: multiple full matches with real (internal) players, not just the dev team, capturing TTK/zone-timing/Reinforcement-Token feedback data against the GDD's flagged hypotheses |
| Optimisation | Full profiling pass against every Phase 2 §10 budget (frame time, memory, GC) on the representative device; this is the week those budgets are either confirmed met or a documented, prioritized punch-list is created |
| Review | Friday: playtest data review — this is the actual validate/revise decision point the GDD's §27 Risk Analysis called for |

**Acceptance criteria:** at least 3 full internal matches completed with data logged against every flagged GDD hypothesis (TTK, zone pacing, Reinforcement Token feel); representative-device profiling data exists against every Phase 2 §10 budget line item, with any misses documented as a prioritized Week 12 punch-list rather than left implicit.

**Risks & mitigation:** this week is where 10 weeks of assumptions meet real data, so it's the week most likely to surface an uncomfortable finding (e.g., TTK feels wrong, or memory budget is blown on the real device) → that's the week's actual job, not a failure of it; the plan explicitly reserves Week 12 to act on exactly this kind of finding rather than treating Week 11 as a rubber-stamp.

**Completion checklist:** ☐ device compatibility spot-check complete ☐ full profiling pass against all Phase 2 budgets ☐ internal alpha playtest data logged against all flagged GDD hypotheses ☐ prioritized Week 12 punch-list produced ☐ PR merged (fixes only, not new features this week).

---

## Week 12 — Stabilization, Tuning & MVP Sign-Off

**Objectives:** close out Week 11's punch-list, apply data-driven tuning (not guesswork) to the flagged balance hypotheses, and formally evaluate the vertical slice against this document's own MVP definition.

| Discipline | Backlog |
|---|---|
| Design | Apply TTK/zone-timer/Reinforcement-Token adjustments to the relevant ScriptableObjects based on Week 11's logged data (data-driven config change, no code change needed — proving the Phase 2 §7 architecture decision pays off here) |
| Programming | Bug-fixing against Week 11's punch-list; no new features this week (deliberate scope freeze) |
| Art | Final polish pass on any assets flagged in Week 11 |
| Animation | Final polish pass |
| VFX | Final polish pass |
| Audio | Final mix pass |
| UI | Final usability fixes |
| QA | Full regression pass across every system built in Weeks 1-11; re-run the Week 11 device profiling pass to confirm fixes actually resolved flagged issues (not just that new issues weren't introduced) |
| Optimisation | Final confirmation against every Phase 2 §10 budget — this is the gate, not a suggestion |
| Review | **MVP Sign-Off Review**: formal walkthrough of this document's §0 scope table, confirming every "in scope" item is present and functional |

**Acceptance criteria:** every item in §0's "In scope for this 12-week MVP" table is implemented, networked, and passes QA regression; every Phase 2 §10 performance budget is met on the representative mid-range device (not just simulated); every GDD-flagged balance hypothesis has been measured at least once and tuned accordingly (even if further tuning continues into Phase 4+ — the point is that it's now data-driven, not speculative).

**Risks & mitigation:** the temptation to add "just one more feature" in the final week is a classic scope-creep risk → the scope freeze is explicit and deliberate; anything not in §0's in-scope table that comes up during Week 12 goes on a backlog for Phase 4+, not into this week.

**Completion checklist:** ☐ Week 11 punch-list closed ☐ full regression pass green ☐ all performance budgets confirmed met on real hardware ☐ all GDD hypotheses measured and tuned ☐ §0 scope table fully satisfied ☐ MVP Sign-Off Review held ☐ PR merged.

---

## 13. Internal Design Review (Self-Validation Pass)

- **Coherence check:** the 12-week sequence was checked for dependency ordering — no week requires a system that a later week builds (e.g., Week 7's ZoneManager correctly comes after Week 4's combat loop is complete, since zone damage needs a working health/damage system; Week 9's art pass correctly comes after Week 6's full hero roster, since silhouette-distinctness art decisions need all 8 kits to compare against).
- **Systems interplay simulated:** traced the build order end-to-end — Weeks 1-2 (netcode+movement) → 3-4 (combat) → 5-6 (heroes) → 7-8 (match structure+loot+death) → 9 (art) → 10 (UI/audio) → 11 (validate) → 12 (tune+sign off) — each week's "Acceptance Criteria" becomes the next relevant week's assumed-working foundation; no week silently depends on something not yet built.
- **Balance/hypothesis-validation check:** every numeric hypothesis flagged as unvalidated in GDD §27/§29 (TTK, zone timers, Reinforcement Token) has an explicit measurement point in this plan (Week 4 early-warning, Week 11 real validation) rather than being left untested until Phase 7.
- **Performance-budget check:** every Phase 2 §10 budget has an explicit test point (Week 1 CI-gate validation, Week 9 art-landing profiling, Week 11 full device profiling, Week 12 final confirmation) — budgets are checked continuously, not just once at the end.
- **Scope-discipline check:** §0's in/out scope table is referenced as the literal Week 12 sign-off criteria, closing the loop between "what we said we'd build" and "what we're declaring done" — this is what keeps a 12-week plan from silently becoming a 20-week plan.
- **Gap found and resolved during this pass:** an earlier draft had the art pass (Neo City) in Week 5, before the hero roster was complete — moved to Week 9, since silhouette-vs-background readability decisions (GDD §23) genuinely need all 8 heroes to check contrast against, not just the first 2.

### Assumptions requiring explicit confirmation
1. **The team-size/role assumption in §1** — if the actual available team is smaller, this plan's weekly scope should be re-derated proportionally rather than compressed into the same 12 weeks.
2. **MVP scope split (§0)** — confirm that deferring ranked mode, live IAP, and full Battle Pass content past this 12-week window (into Phase 7/8 territory) matches your expectations, rather than assuming "MVP" meant "everything in the GDD."
3. Photon Fusion account/billing (Week 1 Day-0 blocker) needs to actually be provisioned before Week 1 can start on schedule — an administrative task, flagged so it isn't discovered as a Week 1 delay.

### Remaining risks (carried forward)
- Week 6 (6 heroes in one week) and Week 9 (art-landing perf risk) are this plan's two highest-density weeks — if either slips, the recommended recovery is compressing Week 12's polish scope rather than cutting Week 11's validation (validation data is what makes every later phase's decisions defensible; polish is more recoverable later).
- Touch-input feel (Week 2) only gets one dedicated tuning pass before Week 11's real playtest — flagged as a known "first pass, not final" item, consistent with GDD §27's own risk register.

### Completeness check against the Phase 3 brief
12-week sprint plan ✅, and every week includes: objectives ✅, backlog ✅, design ✅, programming ✅, art ✅, animation ✅, VFX ✅, audio ✅, UI ✅, QA ✅, optimisation ✅, review ✅, acceptance criteria ✅, risks ✅, mitigation ✅, completion checklist ✅.

---

## 14. Deliverables Summary

- One 12-week execution plan (`docs/MVP_ROADMAP.md`) sequencing Phase 1 (GDD) and Phase 2 (architecture) into a buildable order, with an explicit MVP scope boundary (§0) so "done" is a checkable fact, not a feeling.
- Every GDD-flagged balance hypothesis has a named measurement week (Week 4 early signal, Week 11 real validation).
- Every Phase 2 performance budget has a named validation point (Weeks 1, 9, 11, 12).
- Three assumptions flagged for your confirmation (§13), most consequential: the MVP scope split actually matching your expectations.

**Approve? (Y/N)**
*(A "Y" locks this as the Phase 3 baseline and — per the Git protocol — I'll branch `feature/phase-3-mvp-roadmap`, diff-review, commit, and push, then stop for your merge. If "N," tell me which week(s) or scope call need revision.)*
