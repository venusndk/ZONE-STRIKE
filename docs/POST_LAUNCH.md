# ZONE STRIKE — Post-Launch
**Phase 8 of 8 — Year 1 Live Operations Roadmap**
Version 0.1 (Draft for Approval) · Date: 2026-09-01
Builds on the Battle Pass/ranked cadence already defined in [`docs/GDD.md`](GDD.md) §13/§22, the Remote Config/Addressables live-tuning architecture in [`docs/TECHNICAL_ARCHITECTURE.md`](TECHNICAL_ARCHITECTURE.md) §6/§7, and the analytics/KPI baseline in [`docs/TESTING_LAUNCH.md`](TESTING_LAUNCH.md) §9/§10 — this phase sequences 12 months of content and operations on top of that already-built foundation rather than designing a new one.

---

## 0. Scope note — the honest starting condition

This roadmap begins at **global launch**, which itself sits after the soft-launch go/no-go gate defined in Phase 7 §8 — nothing here is scheduled to happen until that gate is actually passed with real data, not on a fixed calendar regardless of results. Every content commitment below (new heroes, a second map, new modes) is sized against the same Phase 3 §1 team-size assumption carried through this whole engagement; a materially different team size should re-derate this cadence the same way Phase 3 said sprint scope should re-derate, not silently hold the same pace.

---

## 1. Year 1 Roadmap — season-by-season

Seasons run ~9 weeks (matches the Battle Pass cadence already fixed in GDD §22), giving **5 full seasons plus the start of a 6th** across a 52-week year. Content is deliberately **not evenly distributed** — each season has one headline focus, not everything at once, because Phase 3's own sprint-planning lesson (dense weeks are the ones that slip) applies just as much to live-ops cadence as it did to the original 12-week build.

| Season | Weeks | Headline focus | Rationale |
|---|---|---|---|
| **Launch** | 0 | Global launch (post soft-launch gate, Phase 7 §8) | — |
| **Season 1** | 1–9 | **Ranked mode goes live** + first live-ops stabilization pass | Phase 3 §0 explicitly deferred ranked matchmaking past the MVP scope — Season 1 is where that deferred work actually ships, once real launch population data exists to seed matchmaking pools (GDD §13 needs real MMR distribution, which doesn't exist pre-launch). Also the season where the still-flagged GDD hypotheses (TTK, zone timing) get their first at-scale data-driven revision, superseding the Phase 3 Week 11 internal-playtest-only tuning. |
| **Season 2** | 10–18 | **Hero #9** (new role-pairing, TBD by Season 1 balance data — likely a second Phantom or Striker, whichever role's launch pair shows the narrowest viable playstyle range in the win-rate data) + first Limited-Time Mode (LTM) | New hero cadence starts here, not at launch — deliberately, so hero #9's kit is designed against real balance data (GDD §19's win-rate bands) rather than guessed in a vacuum the way the launch 8 necessarily were. |
| **Season 3** | 19–27 | **Balance-focused season** (no new hero/map) + community-vote LTM | A season with zero major content additions is a deliberate choice, not a gap — every prior live-service post-mortem this design draws on shows a season entirely spent tightening an already-large content surface (8-9 heroes, ranked, first LTM's data) prevents exactly the kind of balance debt that erodes GDD §19's competitive-integrity promise over time. |
| **Season 4** | 28–36 | **Second map** + Hero #10 | A second map is the single most expensive content item in Year 1 — Phase 3 Week 9's single-map art pass consumed a full dedicated sprint week for one team; a second map gets its own season specifically so it isn't squeezed alongside other major work, and is sequenced after 3 live seasons of usage/performance data exist to inform what should differ from Neo City (map size, POI count, sightline balance) rather than repeating Neo City's exact formula blind. |
| **Season 5** | 37–45 | **New game mode evaluated from Season 2's LTM data** graduates to a semi-permanent rotation slot + Hero #11 | Only an LTM with strong retention/engagement data (Phase 7 §9 dashboards) earns permanent status — this is a data gate, not a calendar commitment; if no Season-2/3 LTM earned it, Season 5's slot becomes a second LTM trial instead, and that's a legitimate, expected outcome, not a roadmap failure. |
| **Season 6 (start)** | 46–52 | Year 1 retrospective content + Year 2 roadmap tease | Closes the year deliberately light on new systems, heavy on Year-1-in-review content (profile badges, veteran-player recognition) — both a genuine retention lever (GDD §25) and an honest acknowledgment that a full Year 2 plan needs Year 1's actual data before it can be written responsibly, the same "measure before committing" discipline applied throughout this whole engagement. |

---

## 2. Seasons (structure, reused every season per GDD §22)

Every season, regardless of headline focus (§1), follows the same fixed operational shape — consistency here is what makes the cadence sustainable for the team and legible for players:

- **Battle Pass:** new 50-tier track (GDD §22), premium + free rewards, populated with that season's cosmetic content from the Phase 6 art pipeline.
- **Season-start balance patch:** kit-level changes (ability numbers, hero additions/reworks) — a real app-content update via Addressables (Technical Architecture §6), not a Remote Config-only change, since kit changes typically touch VFX/animation assets too.
- **2-3 mid-season numeric-only patches:** pure Remote Config value changes (Technical Architecture §7's `IRemoteOverridable` pattern) against the GDD §19 "2 consecutive weeks outside the 45-55% win-rate band" trigger — no app update required, which is the entire point of that architecture decision paying off here, seasons after it was made.
- **One live event** (§5) mid-season.
- **Ranked season reset** (soft reset per GDD §13) aligned to the Battle Pass boundary — one season-boundary concept for the player, not two staggered ones that would be confusing.

---

## 3. New Heroes

- **Cadence:** 1 new hero roughly every other season (~3 added across Year 1, bringing the roster to 11 by Season 5) — deliberately slower than "every season," because each new hero (a) needs a full `IAbilityEffect` implementation cycle on the Phase 4 framework, (b) needs Phase 6-equivalent art/animation/VFX/SFX production, and (c) immediately expands the GDD §19 balance surface every other hero has to be checked against — a cadence faster than this risks the same "balance debt" Season 3 (§1) is explicitly designed to pay down.
- **Selection process:** each new hero's role and rough kit direction is chosen from the Combat/Balance dashboard (Phase 7 §9) — specifically, which role shows the narrowest viable playstyle range or lowest pick-rate variety, not from an undirected creative-brainstorm process alone. This keeps new-hero investment aimed at strengthening the game's actual weak points.
- **Never at the expense of the launch 8's stability:** a new hero's season-start patch never bundles a same-season nerf to more than one existing hero, specifically to keep the "what changed and why" story legible to players and community managers (§7) each season.

---

## 4. New Maps

- **Cadence:** one second map in Year 1 (Season 4, §1) — genuinely the right pace given the production cost noted in §1's Season 4 rationale; a faster map cadence is explicitly not attempted in Year 1.
- **Design brief for Map 2:** differentiate meaningfully from Neo City rather than reskin it — informed by 3 seasons of real POI heat-map data (which of Mall/Park/Rooftops sees disproportionate early-game contest, which connective zone is under-used) that doesn't exist until Season 1-3 have shipped. This is stated as a brief, not a finished design, since writing Map 2's actual GDD-equivalent section now would mean guessing at data that doesn't exist yet — the same anti-guessing discipline as every other flagged hypothesis in this engagement.
- **Map rotation system:** Map 2's arrival is also when a map-select/rotation system becomes necessary (MVP scope, Phase 3 §0, only ever needed one map) — flagged here as a real, non-trivial Gameplay/UI addition (a new pre-match screen state, matchmaking now needing to account for map preference/rotation) that Season 4's scope must explicitly include, not treat as a side effect of "just adding art."

---

## 5. New Game Modes (Limited-Time Modes)

- **Philosophy:** LTMs are time-boxed trials, not permanent commitments at launch — protects the core 4v4 identity (GDD §1.3's whole design thesis) from dilution while still giving Live Ops room to experiment, per the brief's own "limited-time modes" requirement.
- **Season 2 LTM candidate:** a "Duos" variant (2×2 instead of 4×4) — cheapest to build (reuses every system, just changes `MatchRulesSO.SquadSize`/`SquadCount`, both already-parameterized per Phase 4's data-driven design) and tests a genuinely different pace without new content production.
- **Season 3 LTM candidate:** community-vote between 2-3 lightweight rule-variant modes (e.g., faster zone-shrink "Blitz" variant, or a no-Reinforcement-Token "Hardcore" variant) — voted via an in-app poll, both a genuine design signal and a community-engagement moment (§7).
- **Graduation gate:** an LTM only earns a permanent rotation slot (Season 5, §1) if its Phase 7 §9 engagement/retention data during its trial run meets or exceeds the core mode's own baseline — never graduated on developer preference alone.

---

## 6. Balance Patches

Restates and extends GDD §19's balance philosophy into an operating cadence rather than a one-time rule:

- **Trigger:** any role/hero outside the 45-55% win-rate band for 2 consecutive weeks (GDD §19, monitored via Phase 7 §9's Combat/Balance dashboard) triggers a patch — this is a standing rule, not something re-decided each time.
- **Response speed:** a numeric-only issue (a single ability's damage value too strong) ships within the *next* mid-season Remote Config patch (§2), typically within 1-2 weeks of the trigger firing — fast, because the architecture (Technical Architecture §7) was built specifically to make this fast. A structural issue (a kit fundamentally warping the meta) waits for the next season-start patch, since it likely needs asset changes too.
- **Transparency:** every balance patch ships with public patch notes stating the *data* that motivated the change (e.g., "Havoc's barrier uptime correlated with an 8-point win-rate swing in Vanguard-heavy comps") — a community-trust practice (§7), and a direct, practical benefit of already having the Combat/Balance dashboard built (Phase 7) to draw the number from.

---

## 7. Community Management

- **Channels:** Discord (primary real-time community hub) + in-app news feed (Home Hub, [`UI_UX_DESIGN.md`](UI_UX_DESIGN.md) §4's Featured Carousel already reserved for exactly this) + patch-note publication alongside every balance patch (§6).
- **Moderation posture, restated from GDD §20/§21's age-13-17-appropriate design:** the game's own in-app surfaces (preset pings, party-only voice) are already moderation-safe by construction; Discord/external channels need active human moderation the game's own architecture can't provide, and that moderation policy should be written and staffed before launch, not improvised after an incident.
- **Player feedback loop:** a lightweight, regularly-reviewed feedback channel (Discord + in-app "Report Feedback," distinct from the Bug Report flow in Phase 7 §3) feeding into the Season 3-style "what needs fixing before we add more" prioritization, and the LTM community-vote mechanism (§5) as a structured, not just ad hoc, way for the community to influence roadmap.
- **Esports/competitive community seeding:** deliberately **not** a Year 1 initiative — ranked mode (Season 1) needs real seasons of data and a stable meta before a competitive circuit makes sense; flagged here as an explicit Year 2+ consideration rather than a Year 1 gap.

---

## 8. Analytics Strategy (ongoing operating cadence)

Extends Phase 7 §9's dashboards from "built" to "actually reviewed on a cadence":

- **Weekly:** Combat/Balance and Matchmaking-health dashboards reviewed by the live-ops/design team — this is the operational home of the §6 balance-patch trigger check, run as a standing weekly ritual, not only when someone happens to notice a problem.
- **Monthly:** Retention/Engagement and Monetisation dashboards reviewed against the Phase 7 §10 KPI targets, with any sustained miss escalated the same way a soft-launch gate miss would have been (Phase 7 §8) — the launch gate's rigor doesn't relax just because launch happened.
- **A/B testing framework:** Firebase Remote Config's A/B testing capability (brief-mandated, Technical Architecture §8) is the mechanism for testing content/UX changes (e.g., a Battle Pass pricing experiment, an onboarding-flow variant) against a control group before a full rollout — every experiment pre-registers its success metric before launch, avoiding the common trap of retroactively deciding what counts as a win.

---

## 9. Monetisation Evolution

Every evolution below operates strictly inside GDD §6/§19's immovable constraints (no pay-to-win, no loot boxes, cosmetic-only spend) — this section describes how the *breadth* of monetisation grows, never how those constraints loosen:

- **Cosmetic catalog growth:** each season's Battle Pass (§2) plus a rotating direct-purchase Store selection (GDD §22) grows the cosmetic catalog steadily — sized against the Phase 6 art pipeline's actual throughput, not an arbitrary SKU target.
- **Collaboration skins:** a plausible Year 2+ lever (common in this genre), explicitly flagged here as needing extra care given the 13-17 audience — any collab partner needs the same age-appropriateness bar as the base game, not a lower one just because it's "just cosmetic."
- **Battle Pass pricing/structure experiments:** run through the A/B framework (§8), never shipped as a silent, unexperimented change to something as trust-sensitive as pricing.
- **What does NOT evolve:** hero unlocks stay permanently free (GDD §14's launch decision extends indefinitely — a new hero added in §3 is free-to-play from day one of its release, same as the launch 8), and no loot-box-style mechanic is ever introduced regardless of monetisation pressure — both restated here explicitly as Year-1-and-beyond commitments, not launch-only promises.

---

## 10. Long-Term Scalability

- **Content vs. download-size budget:** the Addressables local/remote split (Technical Architecture §6) is exactly the mechanism that lets the catalog keep growing (§9) without breaching the ≤800MB initial-download budget — each season's new cosmetics/hero assets ship remote-only by default; only what a player actually owns or previews gets downloaded. A **content vault rotation** (older, low-engagement seasonal cosmetics moved to an on-demand-only tier, per the cache-eviction policy already designed in Technical Architecture §6) is the concrete mechanism for keeping the *remote* catalog's total footprint manageable too, not just the initial download.
- **Server fleet scaling:** regional server capacity (Technical Architecture §13, "regional servers") scales against the real concurrent-match ceiling Phase 7 §5 measures — this roadmap doesn't pre-commit to specific server counts, since that number is explicitly an output of a test this document schedules but doesn't pre-empt the result of.
- **Technical debt allocation:** one sprint-equivalent per season is reserved for refactor/tech-debt work (not new content) — a standard, professional live-service practice, stated explicitly here so it's a planned line item rather than something perpetually squeezed out by content pressure, which is how technical debt compounds in real live-service projects.
- **Looking past Year 1 (explicitly out of this roadmap's scope, named so it isn't silently forgotten):** cross-play expansion, a competitive/esports circuit (§7), and any move beyond the 8-player/2-squad format (GDD §1.3) are all plausible Year 2+ directions, but every one of them should be evaluated against Year 1's actual retention/engagement data — consistent with this entire document's operating principle of sequencing commitments after evidence, not before it.

---

## 11. Internal Design Review (Self-Validation Pass)

- **Coherence check:** every season in §1 traces to either the Battle Pass cadence already fixed in GDD §22, or a production-cost rationale grounded in a real measured Phase 3 data point (Week 9's single-map art-pass duration) — no season's content was sized arbitrarily.
- **Non-negotiable-constraint check:** re-verified §9's monetisation evolution never touches GDD §6/§19's no-pay-to-win/no-loot-box guarantees — every growth lever described is breadth (more cosmetics, more experiments), never depth into power-affecting territory.
- **Data-gate check:** confirmed every major content commitment (hero #9's role, Map 2's design brief, LTM graduation) is explicitly gated on data that doesn't exist yet rather than pre-decided — this was a deliberate check against this document's own temptation to over-specify Year 1 content that this engagement has no actual live data to justify.
- **Consistency check with prior phases:** the Duos LTM candidate (§5) was specifically chosen because it requires zero new systems (`MatchRulesSO`'s existing `SquadSize`/`SquadCount` fields, Phase 4) — checked against the actual shipped code rather than assumed to be "easy" in the abstract.
- **Gap found and resolved during this pass:** an earlier draft scheduled the second map in Season 2 (immediately after launch); reconsidered against the Phase 3 Week 9 art-pass cost and the "map design needs real usage data" principle this same document applies to hero design — moved to Season 4, after 3 live seasons of data exist, which is also when it no longer collides with Season 2's ranked-mode-adjacent stabilization focus.

### Assumptions requiring explicit confirmation
1. **Team size stays roughly constant through Year 1** (§0) — if the studio plans to scale the team post-launch, this cadence (especially the hero/map pacing in §3/§4) should be revisited rather than assumed to scale linearly with headcount.
2. **Hero #9's role/direction is explicitly undetermined** (§3) — stated as a data-driven decision for Season 1 to inform, not a gap in this document.
3. **Esports/competitive seeding is deliberately deferred past Year 1** (§7) — confirm this matches business expectations, since some stakeholders may expect earlier competitive-scene investment; flagged rather than assumed uncontroversial.

### Remaining risks (carried forward)
- Season 3's "zero major content" season is a deliberate design choice this document argues for, but is also the season most likely to face internal pressure to add content anyway — worth the business/production leadership being aligned on this rationale before Season 3 actually arrives, not re-litigated under pressure at the time.
- The LTM graduation gate (§5) means Season 5's roadmap slot is genuinely conditional — if no LTM earns it, that's flagged here as an expected, not a failure, outcome, but it does mean Season 5's actual content is not fully fixed by this document alone.

### Completeness check against the Phase 8 brief
Year 1 roadmap ✅ (§1), Seasons ✅ (§2), new heroes ✅ (§3), new maps ✅ (§4), new game modes ✅ (§5), balance patches ✅ (§6), live events ✅ (§2's per-season event slot; not a separate section since GDD §22's Battle Pass cadence and this section's event slot are the same mechanism, not two competing systems — a deliberate consolidation, not an omission), community management ✅ (§7), analytics strategy ✅ (§8), monetisation evolution ✅ (§9), long-term scalability ✅ (§10).

---

## 12. Deliverables Summary

- One Year 1 live-operations roadmap (`docs/POST_LAUNCH.md`) covering all 11 requested subsections, every major content commitment either grounded in an already-fixed cadence (Battle Pass/ranked, from GDD §13/§22) or explicitly data-gated rather than pre-decided.
- Confirmed every monetisation-evolution lever stays inside the GDD §6/§19 no-pay-to-win/no-loot-box guarantees, restated as a Year-1-and-beyond commitment, not a launch-only promise.
- Three assumptions flagged for your confirmation (§11), most consequential: whether the team-size assumption this whole engagement has carried since Phase 3 still holds post-launch.

**Approve? (Y/N)**
*(This is the final phase of the engagement. A "Y" locks this as the Phase 8 baseline and — per the Git protocol — I'll branch `feature/phase-8-post-launch`, diff-review, commit, and push, then stop for your merge, same as every prior phase. If "N," tell me which section(s) need revision.)*
