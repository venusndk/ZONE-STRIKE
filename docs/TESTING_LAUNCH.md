# ZONE STRIKE — Testing & Launch
**Phase 7 of 8 — QA, Compatibility, Store Readiness & Soft Launch**
Version 0.1 (Draft for Approval) · Date: 2026-09-01
Builds on the CI pipeline already shipped ([`docs/CI_PIPELINE.md`](CI_PIPELINE.md)), the performance/memory budgets in [`docs/TECHNICAL_ARCHITECTURE.md`](TECHNICAL_ARCHITECTURE.md) §10, the security posture in §5.5, and the success-metrics baseline already defined in [`docs/GDD.md`](GDD.md) §28 — this phase turns those into an executable QA/launch program rather than restating them.

---

## 0. Scope note — sequencing against where the project actually is

This document assumes the Phase 3 MVP (12-week vertical slice) is complete and the CI pipeline's real prerequisites (Unity project scaffold, `UNITY_LICENSE`, `BudgetGate.RunAndExport`) are resolved — none of that has happened yet in this engagement (per `docs/CI_PIPELINE.md`'s honest status note). This is deliberately a **readiness plan to build toward**, not a claim that testing has started. Where a section references "the current build," read that as "the MVP build once Phase 3 actually produces one."

---

## 1. QA Strategy — the test pyramid

```
        ▲  Manual exploratory + playtest (§3, §4)     — small volume, highest judgment
       ╱ ╲    catches "does this feel right," not just "does this work"
      ╱   ╲
     ╱     ╲  PlayMode integration tests (§2)          — medium volume
    ╱       ╲    Fusion local-simulation networking logic, system interplay
   ╱         ╲
  ╱           ╲ EditMode unit tests (§2)                — largest volume, cheapest, fastest
 ╱_____________╲   pure logic: SO data, damage math, zone-ring math, ability validation
```

**Ownership split** (matches the Phase 3 §1 assumed team roster): the QA Engineer owns manual/exploratory testing (§3) and the device matrix (§5); engineers own the automated layers (§2) as part of the Definition of Done already established in Phase 3 §1 ("meets acceptance criteria... passes CI budget gates... not just code exists"); the whole team participates in the Phase 3 Week 11 internal alpha playtest (§4's multiplayer/exploratory testing feeding directly into that same milestone, not a separate effort).

**Gate discipline:** every phase-completion in this engagement has already required "run the appropriate validation... before committing" per the git protocol — this document formalizes what that meant informally into a repeatable program the team runs continuously, not just at engagement phase boundaries.

---

## 2. Automated Testing Strategy

| Layer | Tooling | What it covers | What it deliberately does NOT cover |
|---|---|---|---|
| **EditMode unit tests** | Unity Test Framework (Technical Architecture §8) | Pure logic with no Unity runtime dependency: `WeaponDefinitionSO` rarity-scaling math, `MapZoneConfigSO` ring-timing sums (the same arithmetic self-checked in GDD §29), `MatchRulesSO`/`AbilityDefinitionSO` `IRemoteOverridable` override application, `SaveSystem`'s corrupt-save fallback behavior (Phase 4 §3 flagged this as needing exactly this kind of test) | Anything requiring real network round-trips, real rendering, or real device input — those are PlayMode/manual territory |
| **PlayMode integration tests** | Unity Test Framework + Fusion's local/shared-mode simulation (no live dedicated server needed) | Cross-system interplay: `HeroAbilitySystem`'s cooldown-bypass rejection (already named as a Phase 3 Week 5 acceptance criterion — this is where it actually gets codified as a repeatable test, not a one-time manual check), `WeaponSystem`'s server-authoritative hit validation against a simulated lag-compensation scenario, `MatchManager`'s win/loss/draw resolution across all 3 documented conditions (GDD §5) | Real-world network jitter/packet loss (that's §4's job — Fusion's local simulation is deterministic and clean, which is exactly why it can't substitute for real-network testing) |
| **CI enforcement** | `.github/workflows/build.yml` (already shipped, Phase "CI/CD") | Both layers above run on every PR; the budget-gate job enforces the Technical Architecture §2/§9/§10 performance ceilings as a merge-blocking check, not a post-hoc audit | Store submission validation, real-device thermal/battery behavior — no CI runner is a representative mobile device |
| **Coverage target** | — | 80%+ line coverage on Core (`ScriptableObject` data/validation logic) and Gameplay-layer server-authoritative decision code specifically (the classes Technical Architecture §5.5's anti-cheat table depends on); no blanket project-wide percentage target, since chasing coverage on Presentation-layer visual code produces low-value tests | Coverage is a floor for correctness-critical logic, not a proxy for "well tested" everywhere equally |

---

## 3. Manual Testing Plan

**Test case categories**, run against every build before it's promoted past internal testing (Technical Architecture §9's build-variant pipeline):

| Category | Cadence | Example scope |
|---|---|---|
| **Functional (per-system)** | Every PR touching that system, before merge | E.g., a PR touching `SpawnManager` gets a manual pass on drop-point selection + Reinforcement Token redeploy, in addition to whatever automated tests exist |
| **Regression** | Before every internal build promotion | Re-run the core match loop end-to-end (drop → loot → zone → combat → win/loss) plus a fixed checklist of previously-fixed bugs specific to this project, so a fix never silently regresses |
| **Exploratory** | Weekly, unscripted | QA Engineer plays without a script specifically looking for "the thing no test case anticipated" — e.g., stacking Aegis's Heal Beacon with a Reinforcement Token redeploy in a way that wasn't an explicit design conversation |
| **Accessibility pass** | Once per major UI change | Walks the actual [`UI_UX_DESIGN.md`](UI_UX_DESIGN.md) §20-derived checklist per screen (colorblind mode, dynamic text scale, gamepad focus nav, redundant-not-color-only signaling) — this doc's job is making sure that checklist gets *run*, not just written |

**Bug report template** (every manual/exploratory finding uses this — consistency here is what makes triage fast):

```
Title: [System] Short, specific summary (not "bug in match" — "ZoneManager: shrink-2 hold
       duration reads 45s in HUD but MapZoneConfigSO specifies 75s")
Severity: Blocker | Critical | Major | Minor | Trivial   (see table below)
Priority: P0 | P1 | P2 | P3                                (triage-assigned, may differ from severity)
Build: <git commit hash / CI build number>
Device: <model, OS version, RAM class — cross-reference §5's matrix ID if applicable>
Network condition (if multiplayer-relevant): <WiFi / 4G / simulated latency+loss profile, §4>
Steps to reproduce: 1. 2. 3. (numbered, exact — "play a match" is not a repro step)
Expected result:
Actual result:
Attachments: video/screenshot/log file (device log path per platform)
Frequency: Always | Often (~>50%) | Sometimes | Rare (once)
```

**Severity definitions** (game-specific, not generic):

| Severity | Definition | Example |
|---|---|---|
| **Blocker** | Crashes, or makes a match unplayable/uncompletable | App crash on match load; a squad can never reach the win condition due to a logic deadlock |
| **Critical** | Breaks competitive integrity or core netcode correctness | A client-side value bypasses server validation (directly against Technical Architecture §5.5); a desync causes visibly different game state between clients |
| **Major** | A system doesn't work as designed but has a workaround, or affects a non-critical-path feature | Death Recap panel shows wrong damage attribution; Battle Pass tier-claim animation doesn't play (functionally still claims the tier) |
| **Minor** | Cosmetic/polish issue with no functional impact | Icon slightly misaligned, animation timing slightly off |
| **Trivial** | Typo, minor visual inconsistency | Caption text capitalization inconsistency |

---

## 4. Multiplayer Testing

Directly targets the Technical Architecture §5 networking design and §5.5 anti-cheat table — this is where those design-time mitigations get verified as actually-working, not just architecturally sound on paper.

| Test area | Method | What it validates |
|---|---|---|
| **Latency simulation** | Client-side network condition throttling (e.g., Clumsy/network-link-conditioner tooling) at 50/100/150/200ms, both directions | Client-prediction/reconciliation stays visually smooth (no hard snapping) up to the Technical Architecture §5.2 target range; TTK (GDD §18) measured under each condition, not just on a LAN — this is the *real* version of the Phase 3 Week 4 TTK measurement, run under realistic network conditions rather than dev-team-on-the-same-network conditions |
| **Packet loss simulation** | 1%/5%/10% simulated loss | Reliable RPCs (ability casts, hit confirmations) still land correctly; verifies the Technical Architecture §5.3 "reliable RPCs only for discrete events" design actually tolerates loss gracefully |
| **Reconnect / match recovery** | Force-kill a client's network mid-match, reconnect within and after the 60s grace window (`MatchRulesSO.ReconnectGraceWindowSeconds`) | Technical Architecture §5.4's exact designed behavior: within-window reconnect resumes cleanly with a full state resync; past-window converts to `DisconnectElimination` without consuming a Reinforcement Token |
| **Exploit/cheat attempt testing** | Deliberate misuse: a client sends out-of-envelope movement input, a modified/replayed fire RPC, an ability-cast RPC while on cooldown, a claimed pickup on an already-claimed loot item | Directly re-verifies every row of Technical Architecture §5.5's anti-cheat table against the actual shipped code (Phase 4's speed-clamp, ammo/cooldown server-authority, and race-safe pickup claim logic) — this is a **security test**, run by whoever owns the security-review pass, not just a QA nice-to-have |
| **Squad/party matchmaking correctness** | Queue as solo, duo, and full 4-stack in varying combinations | GDD §13's ranked-matchmaking-fairness rule (a full 4-stack never faces a mixed solo/duo pool in ranked) — testable even pre-ranked, since casual matchmaking's party-aware pairing (Phase 3 §0 scope) shares the same underlying logic |
| **Cross-region latency** | Test from each planned server region (Technical Architecture §13 "regional servers") against its intended player geography | Confirms regional routing actually reduces latency vs. a control test against a distant region — a real measurement, not an assumption that "regional servers" alone guarantees good latency |

---

## 5. Stress Testing

| Test | Method | Pass criteria |
|---|---|---|
| **Server concurrent-match load** | Ramp synthetic match sessions on the dedicated-server fleet (Technical Architecture §5.1) until degradation | Document the actual concurrent-match ceiling per server instance/region *before* soft launch (§8) — this number directly feeds the soft-launch region-sizing decision, not a guess |
| **Client long-session soak test** | Run 15+ consecutive matches back-to-back on a representative device without restarting the app (Phase 3 Week 11's device profiling pass extended in duration) | Memory stays within the Technical Architecture §10.2 1.8GB alert ceiling across the whole session — this is the specific test that validates `MatchLifetimeScope`'s teardown-on-return-to-hub design (Phase 4) actually prevents the "stale match state leaks into the next session" bug class it was built to prevent, rather than just asserting the architecture should work |
| **Matchmaking queue burst** | Simulate a sudden spike in concurrent queuing players (e.g., a marketing push or launch-day spike) | Queue times degrade gracefully (linearly, with clear "estimated wait" UI feedback per [`UI_UX_DESIGN.md`](UI_UX_DESIGN.md) §8) rather than the matchmaking service failing outright |
| **Addressables remote-content load under poor connectivity** | Throttle to a slow/lossy connection profile during first-time hero/cosmetic download (Technical Architecture §6) | Load failures are recoverable (retry, not stuck), and the ≤45s cold-start-to-first-match KPI (GDD §3) is re-validated under this condition specifically, not just on a good connection |

---

## 6. Device Compatibility Matrix

Anchored to the GDD's explicit target hardware floor (3GB RAM) through a genuine high end, spanning both platforms — representative real devices, not abstract tiers alone, since App Store/Play Console crash reports will name real models.

| Tier | Android | iOS | RAM class | Quality tier (Technical Architecture §2) |
|---|---|---|---|---|
| **Floor** | Samsung Galaxy A10s / A20, Xiaomi Redmi 9A | iPhone SE (2nd gen), iPhone 7 | 3GB | Low (30 FPS locked) |
| **Mid** | Samsung Galaxy A54, Google Pixel 6a | iPhone 12, iPhone SE (3rd gen) | 4–6GB | Mid (60 FPS) |
| **High** | Samsung Galaxy S23, Google Pixel 8 | iPhone 14/15 | 8GB+ | High (60 FPS, headroom) |

**Per-tier pass criteria:** Floor tier must hold the Technical Architecture §2 Low-tier locked 30 FPS with the `PerformanceGovernor` (Phase 4) never needing to intervene further (i.e., Low tier is genuinely the floor, not itself unstable); Mid/High must sustain 60 FPS per §10.1's frame-time budget with the Governor engaging only under real thermal throttling, not routinely. **GPU driver path check:** both the Vulkan (primary) and OpenGL ES 3.1 (fallback) paths from Technical Architecture §1 are explicitly tested on at least 2 Floor-tier Android devices each, since driver inconsistency at the low end was already flagged as a known risk in Technical Architecture §13.

---

## 7. Store Optimisation

### 7.1 App Store (iOS)
- **Title:** "Zone Strike" (name recognition) — **Subtitle:** keyword-carrying, e.g. "4v4 Hero Battle Royale" (matches actual search intent for the genre, honestly describes the product per the "set expectations correctly" note already flagged in GDD §27's risk register about the 8-player format reading as "not real BR" to some players).
- **Keywords field:** hero shooter, battle royale, multiplayer, squad, 4v4, mobile esports, team shooter — chosen against actual App Store Connect keyword-competition research at submission time, not guessed here.
- **Screenshots (first 3 are the ones that convert):** #1 a mid-fight action shot with clear UI (proves "this is a real game, not a mockup"), #2 hero-roster showcase (sells character variety), #3 the zone-shrink moment (sells the BR tension fantasy). Preview video: ≤30s, cold open on gameplay within the first 2 seconds (App Store preview-video drop-off is steep after that).
- **Category:** Games → Action, secondary Games → Adventure is not appropriate — should be a straightforward Action/Multiplayer categorization, avoiding a miscategorization that would hurt discoverability among the actual target audience.
- **Age rating:** targets the stated 13–17 audience — complete Apple's age-rating questionnaire honestly against the actual content (no loot boxes, preset-ping-only default comms, per GDD §6/§21) rather than under- or over-declaring.

### 7.2 Google Play
- **Short description (80 char limit):** leads with the core hook, not the brand name alone — e.g. "8-minute 4v4 hero battle royale. Drop, loot, survive the zone."
- **Long description:** structured with the core loop (GDD §2) in the first 2 lines (Play Store truncates aggressively before "read more"), feature bullets below, avoiding keyword-stuffing (Play's algorithm penalizes it more directly than App Store's).
- **Feature graphic:** static, high-impact hero-squad key art (matches the Phase 6 §11 AI-art-prompt-seeded key art direction) — this is Play's most-viewed asset before a tap, deserves the most iteration.
- **Pre-registration campaign:** Play Store's pre-registration feature, timed to open ~4-6 weeks before soft launch (§8) specifically to build a day-one cohort for the soft-launch KPI measurement window rather than a slow organic trickle that would take longer to reach statistical confidence.
- **Play Console pre-launch report:** run automatically against the device lab Google provides — treat any crash/ANR it surfaces as a Blocker-severity bug regardless of how it was found, per §3's severity table.

---

## 8. Soft Launch Strategy

- **Region selection:** 1-2 tier-2 English-speaking markets with a mobile-gaming audience broadly representative of the global target demographic but smaller/cheaper to reach at scale (e.g., Philippines and Canada are common industry choices for exactly this reason) — final selection should also weigh where the planned regional dedicated-server footprint (Technical Architecture §13) already has low-latency coverage, since soft launch should test the *real* server experience, not an artificially-advantaged one.
- **Duration:** 4-6 weeks minimum — long enough to measure real D7/D30 retention (GDD §28), not just D1, since D1 alone is a poor predictor of whether the core loop actually retains players past the novelty window.
- **Go/no-go gate for global launch:** the GDD §28 success-metrics baseline (D1≥35%, D7≥15%, D30≥6%, session length 25-40min, match completion ≥95%, crash-free ≥99.5%) is the literal go/no-go checklist — global launch does not proceed on a majority-met basis; every metric is reviewed, with D7/D30 retention and crash-free rate treated as the two hardest gates (retention because it's the core product bet from GDD §1.3's design thesis; stability because a bad launch-week crash rate does lasting App Store/Play Store rating damage that's expensive to recover from).
- **Balance/tuning latitude:** soft launch is explicitly the first real chance to validate the GDD's still-flagged hypotheses (TTK, zone timing, Reinforcement Token feel) against a real, non-internal population at scale — Remote Config-driven tuning (Technical Architecture §7) should be used actively during this window, not treated as a frozen build.
- **Rollback plan:** the dedicated-server/client version-compatibility approach (Technical Architecture, versioned Addressables catalogs) should be confirmed capable of a fast content-only rollback (Remote Config revert, Addressables catalog revert) without an app-store resubmission, for exactly the scenario where soft-launch data reveals a bad tuning change needs reverting quickly.

---

## 9. Analytics Dashboards

Built on Firebase Analytics (Technical Architecture §8, already the chosen SDK) — dashboards are grouped by the question they answer, not just by raw event dump, so a non-technical stakeholder (Producer, Live Ops) can act on them directly.

| Dashboard | Key events/metrics | Question it answers |
|---|---|---|
| **Acquisition** | `first_open`, install source/campaign attribution, `tutorial_complete` (if a tutorial exists) | Are the right players finding the game, and do they get through onboarding? |
| **Retention/Engagement** | `session_start`/`session_end`, `match_start`/`match_end`, D1/D7/D30 cohort retention (GDD §28) | Is the core loop bringing players back? — the single most important dashboard given GDD §1.3's design thesis rests on this |
| **Combat/Balance** | `hero_selected`, `elimination` (with weapon/ability attribution), `match_result`, per-hero win-rate rollup | Feeds directly into GDD §19's "≥45%/≤55% win-rate band" balance-patch trigger — this dashboard is a live implementation of that GDD rule, not a separate concern |
| **Monetisation** | `iap_purchase`, `battle_pass_purchased`, `battle_pass_tier_claimed`, ARPDAU, premium-conversion rate | Tracks against GDD §22's 3-6% premium-conversion target, explicitly never cross-referenced against the Balance dashboard for power-tuning decisions (GDD §19's non-negotiable no-pay-to-win guardrail, enforced organizationally by keeping these dashboards' *purposes* separate even though the underlying Firebase project is shared) |
| **Performance/Stability** | Crash-free session rate (Firebase Crashlytics), average FPS by device tier, `PerformanceGovernor` intervention rate (Phase 4 — how often it actually has to step down quality) | Tracks against GDD §28's ≥99.5% crash-free and ≥55 average-FPS targets; the Governor-intervention metric specifically tells the team whether the Low-tier floor (§6) is actually holding or silently failing more devices than expected |
| **Matchmaking health** | Average queue time, queue-cancel rate, party-composition mix (solo/duo/4-stack ratio) | Feeds the §5 stress-testing follow-up in live operation — a live version of the same queue-burst concern, monitored continuously post-launch |

---

## 10. Retention KPIs

Restates and operationalizes the GDD §28 baseline as the concrete numbers this phase's dashboards (§9) and soft-launch gate (§8) are built to measure — not a new, separate KPI set:

| KPI | Target (GDD §28) | Primary dashboard | Alert threshold |
|---|---|---|---|
| D1 retention | ≥35% | Retention/Engagement | Alert if <30% for 3 consecutive days |
| D7 retention | ≥15% | Retention/Engagement | Alert if <12% (soft-launch go/no-go gate, §8) |
| D30 retention | ≥6% | Retention/Engagement | Reviewed at soft-launch window close, not daily-alerted (too slow-moving for daily alerting to be meaningful) |
| Avg. session length | 25-40 min | Retention/Engagement | Alert if <20min sustained (signals the core loop isn't holding attention even within a session) |
| Match completion rate | ≥95% | Performance/Stability | Alert if <90% (proxy for both netcode reliability and rage-quit rate) |
| Crash-free session rate | ≥99.5% | Performance/Stability | Alert if <99% on any single device tier (§6) |
| Cold-start-to-first-match | ≤45s | Performance/Stability | Alert if p90 exceeds 60s |
| Battle Pass premium conversion | 3-6% of MAU | Monetisation | Reviewed weekly, never used to adjust Balance dashboard decisions (§9's stated firewall) |

---

## 11. Internal Design Review (Self-Validation Pass)

- **Coherence check:** every KPI in §10 traces to the GDD §28 baseline already established in Phase 1 — no new, uncoordinated metric set was introduced; §9's dashboards are the concrete implementation of §10's numbers, not a parallel reporting scheme.
- **Anti-cheat re-verification check:** §4's exploit-testing row was checked line-by-line against every mitigation named in Technical Architecture §5.5, confirming each has a corresponding *test*, not just a corresponding *design claim* — this closes the loop between "we designed against this threat" (Phase 2) and "we verified the shipped code actually resists it" (this phase).
- **Consistency check:** the bug-report template and severity table (§3) were checked against real project vocabulary (e.g., "Critical" is defined in terms of this game's specific competitive-integrity/netcode concerns, not a generic severity glossary) so triage decisions stay grounded in what actually matters for this product.
- **Sequencing check:** confirmed this document assumes, and states explicitly (§0), that it is a readiness plan for a build that doesn't fully exist yet in this engagement — avoids the document silently implying testing has already happened.
- **Gap found and resolved during this pass:** an earlier draft's soft-launch go/no-go gate (§8) was written as "meet a majority of the GDD §28 targets"; reconsidered and tightened to "every metric reviewed, with retention and crash-free treated as the two hardest gates" — a majority-based gate would have let a genuinely broken core loop (bad retention) slip through if enough secondary metrics looked fine, which contradicts GDD §1.3's own framing of retention as the central product bet.

### Assumptions requiring explicit confirmation
1. **Soft-launch region selection** (Philippines/Canada suggested as examples) needs your/business-side confirmation — final choice should also weigh actual planned server-region footprint and marketing budget, both outside this document's scope.
2. **Server concurrent-match ceiling (§5)** is explicitly unmeasured — this document specifies the test, not the result; running it requires the actual dedicated-server infrastructure to exist first.
3. **Category/age-rating declarations (§7)** should get a final legal/compliance review pass before submission, consistent with the age-13-17-compliance flag already carried since GDD §27.

### Remaining risks (carried forward)
- The device compatibility matrix (§6) names specific real devices as of this document's writing; device availability/relevance shifts over time and the list should be refreshed close to actual QA execution, not treated as permanently fixed.
- Soft-launch's 4-6 week duration is a industry-standard starting assumption, not derived from this specific game's data (there is none yet) — worth revisiting once the first cohort's early D7 trend is visible, rather than rigidly waiting the full window if early signal is unambiguous either way.

### Completeness check against the Phase 7 brief
QA strategy ✅ (§1), automated testing strategy ✅ (§2), manual testing plan ✅ (§3), multiplayer testing ✅ (§4), stress testing ✅ (§5), device compatibility matrix ✅ (§6), bug report templates ✅ (§3), App Store optimisation ✅ (§7.1), Google Play optimisation ✅ (§7.2), soft launch strategy ✅ (§8), analytics dashboards ✅ (§9), retention KPIs ✅ (§10).

---

## 12. Deliverables Summary

- One testing/launch readiness program (`docs/TESTING_LAUNCH.md`) covering all 12 requested subsections, every KPI and test traceable to a Phase 1/2/4 constraint rather than freestanding.
- A bug-report template and severity table calibrated to this project's actual competitive-integrity/netcode concerns, not a generic QA glossary.
- Three assumptions flagged for your confirmation (§11), most consequential: soft-launch region selection needs a business-side decision this document can't make alone.

**Approve? (Y/N)**
*(A "Y" locks this as the Phase 7 baseline and — per the Git protocol — I'll branch `feature/phase-7-testing-launch`, diff-review, commit, and push, then stop for your merge. If "N," tell me which section(s) need revision.)*
