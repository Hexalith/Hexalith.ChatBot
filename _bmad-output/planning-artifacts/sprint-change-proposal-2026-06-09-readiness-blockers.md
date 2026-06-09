---
title: Sprint Change Proposal - Implementation Readiness Blockers (Pass 2)
project: Chatbot
date: 2026-06-09
status: approved
mode: Batch
trigger: "Implementation readiness re-run 2026-06-09 (22:12) found NOT READY: 2 critical structural defects (CR-1 Story 10.6 blocked by unresolved streaming ADR; CR-2 Epic 7 FR74/FR75 enforcement materialization has no owning story), 5 major issues, 3 minor concerns, 2 UX warnings."
scope_classification: Moderate
recommended_approach: Direct Adjustment (Hybrid - 2 new assignable stories + targeted artifact edits)
owner: Jerome
prepared_at: 2026-06-09
approved_by: Jerome
approved_at: 2026-06-09
implementation_state: planning-artifacts-applied
source_report: "_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-09.md"
supersedes_open_items_from: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-readiness-alignment.md"
decisions:
  review_mode: Batch
  cr2_placement: "New consolidated Epic 8 story (Story 8.7)"
  cr1_approach: "Split Story 10.6 into 10.6a (ADR) + 10.6b (implementation)"
  scope: "Criticals fully resolved + majors/minors captured as planning actions"
---

# Sprint Change Proposal - Implementation Readiness Blockers (Pass 2)

## 1. Issue Summary

The 2026-06-09 implementation readiness assessment was **re-run** after the earlier `sprint-change-proposal-2026-06-09-readiness-alignment.md` (approved 21:48) corrected the stale epic metadata and Epic 10 sequencing. The deeper re-run (report timestamped 22:12) confirms the planning package is unusually strong — **111/111 FR coverage, UX aligned, testable BDD acceptance criteria** — and is **NOT READY** for only **two structural reasons**. Both share the same shape: *correct, tested logic was shipped, but its runtime activation or its enabling decision was deferred with no assignable story owning the gap.*

**CR-1 — Story 10.6 is blocked by an unresolved architecture decision.**
`architecture.md` §Frontend Architecture marks the AI-response streaming transport an **open decision** ("resolve before Story 10.6"). The readiness-alignment proposal marked Story 10.6 *blocked*, but never converted the decision into **assignable work**. A required-for-MVP-readiness epic therefore still contains a story that cannot start, with the unblocking decision floating outside the plan.

**CR-2 — Epic 7's disable/quarantine/rate-limit controls (FR74/FR75) may be inert shells.**
`architecture.md` lines 199–204 state plainly: Epic 7 wired and unit-tested the control floor over a shared `GovernedOperationAggregate`, but the control-state and rate-limit enforcement seams **read from `AlwaysActive…`/`AlwaysUnlimited…` provider defaults** and stay **inert until a durable read-side projection materializes tenant control state plus a periodic runtime trigger**. The Epic 8 retrospective made this **Action Item #1** ("create a dedicated 'wire the observability + control-floor runtime' story for early Epic 9") and **Action Item #2** (wire the periodic trigger). **Epic 9 then closed `done` without ever creating that story.** As a result, stories 7.12–7.26 are marked `done` while the actual enforcement they describe does not yet affect runtime behavior, and **no assignable story owns the materialization.**

### Evidence

- `implementation-readiness-report-2026-06-09.md` §Step 5 — CR-1, CR-2, MA-1…MA-5, MI-1…MI-3; §Step 4 — 2 UX warnings; final status **NOT READY**.
- `architecture.md:199-204` — "the floor is wired and unit-tested yet inert until a durable read-side projection materializes … remain deferred beyond Epic 8 (carried forward as Epic 8 retro action items #1–#2)."
- `architecture.md:393-397` — "Open decision — AI-response streaming transport (resolve before Story 10.6) … Decide and record an ADR."
- `epic-8-retro-2026-06-03.md` Action Items #1–#2 and §Previous Retrospective Follow-Through — Epic 7 AI#1/AI#2 ❌ **Not addressed**; deferred-runtime debt "compounds across epics and needs an owner."
- `sprint-status.yaml` — Epics 1–7 & 9 `done`; **Epic 8 `in-progress`** (only Story 8.6 `backlog`); Epic 10 `backlog`. The control floor stories 7.12–7.26 are all `done`.

This is a backlog/plan correction (add the two missing owning stories + align the artifacts that describe them). It is **not** a code rollback: the Epic 7 control logic and Epic 8 emission/contract layers are correct and should be kept.

## 2. Checklist Results (Change Navigation)

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is a readiness re-run, not a failed story. Impacted artifacts: `epics.md`, `architecture.md`, `sprint-status.yaml`, `docs/adrs/`. |
| 1.2 Core problem | [x] Done | Issue type: *misrepresented completeness* — tested logic shipped with runtime activation / enabling decision deferred and unowned. |
| 1.3 Evidence | [x] Done | Readiness report Step 5; architecture.md:199-204 & 393-397; Epic 8 retro AI#1/#2; sprint-status. |
| 2.1 Current epic completable? | [x] Done | Epic 8 (`in-progress`) can absorb the enforcement-activation story cleanly; Epic 10 (`backlog`) can absorb the 10.6 split. No `done` epic must reopen. |
| 2.2 Epic-level changes | [x] Done | No new epic; no epic removed. Add 1 story to Epic 8; split 1 Epic 10 story into 2. |
| 2.3 Remaining epics impacted | [x] Done | Epic 9's "production observable" claims (9.2) depend on the same runtime loop — Story 8.7 retroactively backs them. Note this as a release-gate, not a re-open. |
| 2.4 Obsolete / new epics | [x] Done | None obsolete. Gap is two stories, not an epic. |
| 2.5 Re-sequence / priority | [x] Done | Story 8.7 should land before MVP readiness sign-off (it backs FR74/FR75 + 9.2). Story 10.6b stays gated behind 10.6a. M0→M1→M2 order unchanged. |
| 3.1 PRD conflicts | [N/A] | No PRD conflict; FR74/FR75/FR67 unchanged. The gap is enforcement *activation*, not requirement definition. |
| 3.2 Architecture conflicts | [!] Action | `architecture.md` describes the inert floor + open streaming decision but assigns neither to an owning story. Edit to name Story 8.7 and Story 10.6a as owners. |
| 3.3 UI/UX conflicts | [x] Done | UX (UX-DR32) requires the streaming Stop/Cancel that 10.6b delivers; resolved by making the transport ADR (10.6a) assignable. 2 UX warnings already covered by cross-cutting guidance (line 593-595). |
| 3.4 Other artifacts | [!] Action | `sprint-status.yaml` add Story 8.7; split 10.6 → 10.6a/10.6b. `docs/adrs/` gains the streaming ADR target (10.6a deliverable). |
| 4.1 Direct Adjustment | [x] Viable | Add 2 stories + targeted edits. Effort Low-Medium; risk Low. **Selected.** |
| 4.2 Rollback | [N/A] | No code rollback helps; the shipped logic is correct. |
| 4.3 MVP Review | [N/A] | MVP scope unchanged; this restores honesty about runtime activation, not scope. |
| 4.4 Recommendation | [x] Done | Direct Adjustment (Hybrid: 2 new assignable stories + artifact edits). |
| 5.1-5.5 Proposal components | [x] Done | This document. |
| 6.1-6.2 Final review | [x] Done | Recommendations trace 1:1 to readiness findings. |
| 6.3 User approval | [!] Action | Pending Jerome approval. |
| 6.4 Sprint-status update | [!] Action | Apply on approval (§5.3). |
| 6.5 Handoff plan | [x] Done | Architect (ADR + 8.7 design), Developer (8.7 impl + 10.6b), Test Architect (enforcement-proof tests + readiness re-run). |

## 3. Impact Analysis

### Epic Impact

| Epic | Impact | Required change |
| --- | --- | --- |
| Epics 1-6, 9 | None. `done`, valid. | None. |
| Epic 7 | Control floor (7.12-7.26) stays `done` and correct, but its enforcement is **inert** until activation lands. | No reopen. Annotate FR74/FR75 coverage to point enforcement *activation* at the new Story 8.7. |
| Epic 8 (`in-progress`) | Natural home for the runtime-activation work; matches Epic 8 retro AI#1/#2. | **Add Story 8.7 — Control-plane runtime activation.** |
| Epic 9 | `done`; 9.2 "audit-completeness production observable" assumes a live runtime loop that Story 8.7 supplies. | No reopen; record Story 8.7 as the runtime backing for 9.2/9.13 (release-gate note). |
| Epic 10 (`backlog`) | Story 10.6 blocked by unresolved ADR. | **Split Story 10.6 → 10.6a (streaming-transport ADR) + 10.6b (progressive render + Stop/Cancel).** |

### Story Impact

- **New Story 8.7** (Epic 8, M2): durable control-state/rate-limit read-side projection + periodic runtime trigger, replacing the `AlwaysActive…`/`AlwaysUnlimited…` defaults, with tests proving a disabled/quarantined/rate-limited subject is actually blocked or throttled. Consolidates the cross-epic deferred-runtime backlog (Epic 7 AI#1/#2, Epic 8 AI#1/#2: 7.6-7.11 evaluator triggers, 8.4 alert coordinator, 8.5 weekly runbook sampler, per-tenant audit-checkpoint feed).
- **New Story 10.6a** (Epic 10, M2): assignable streaming-transport ADR/spike → `docs/adrs/ai-response-streaming-transport.md`. Architect-owned, no production code.
- **New Story 10.6b** (Epic 10, M2): the former Story 10.6 implementation (UX-DR32 progressive render + always-reachable Stop/Cancel), **blocked until 10.6a's ADR is accepted**.
- Story count: **119 → 121**.

### Artifact Conflicts

- `epics.md`: frontmatter `storyCount`; FR74/FR75 coverage-map annotations; Epic List Epic 8 entry; Epic 8 body (+ Story 8.7); Epic 10 body (10.6 → 10.6a/10.6b).
- `architecture.md`: control-floor note (199-204) and streaming open-decision note (393-397) — name the owning stories.
- `sprint-status.yaml`: add `8-7-…`; split `10-6-…` into `10-6a-…` + `10-6b-…`.
- `docs/adrs/`: ADR target named (created by Story 10.6a, not by this proposal).

### Technical Impact

No production code change in this proposal — it adds plan items and aligns descriptions. **No submodule operations** (all edits are under `_bmad-output/` and `docs/`; per project policy, no submodule init/update is performed). The only runtime-affecting gate: Story 10.6b must not start until 10.6a's ADR is accepted, and FR74/FR75 enforcement is not "production-real" until Story 8.7 passes.

## 4. Recommended Approach

**Selected path: Direct Adjustment (Hybrid).** Add the two missing assignable stories and align the artifacts that describe them. Effort **Low-Medium**, risk **Low**, timeline impact: one architecture ADR (10.6a) + one runtime-activation implementation story (8.7) before MVP readiness sign-off.

Rejected: **Rollback** (the shipped Epic 7/8 logic is correct — nothing to revert); **MVP Review** (scope is unchanged; the fix restores honesty about runtime activation).

## 5. Detailed Change Proposals

### 5.1 `epics.md` Edits

**5.1.1 — Frontmatter story count**

OLD:
```yaml
storyCount: 119
correctedAt: "2026-05-30"
readinessAlignedAt: "2026-06-09"
```
NEW:
```yaml
storyCount: 121
correctedAt: "2026-05-30"
readinessAlignedAt: "2026-06-09"
readinessBlockersResolvedAt: "2026-06-09"
```
Rationale: +1 for Story 8.7, +1 for splitting 10.6 into 10.6a/10.6b.

**5.1.2 — FR Coverage Map FR74/FR75 enforcement-activation annotation**

OLD:
```text
- FR74: Epic 7 — Disable/quarantine/rate-limit sources (15 subject×action stories, shared control floor inlined per story)
- FR75: Epic 7 — Per-tenant rate limits/quotas/circuit breakers
```
NEW:
```text
- FR74: Epic 7 — Disable/quarantine/rate-limit sources (15 subject×action stories, shared control floor inlined per story); **runtime enforcement activation owned by Story 8.7** (control floor is wired+tested in Epic 7 but inert until 8.7 materializes the durable control-state projection + periodic trigger).
- FR75: Epic 7 — Per-tenant rate limits/quotas/circuit breakers; **runtime enforcement activation owned by Story 8.7** (same inert-until-activated condition).
```
Rationale: Closes CR-2's "appears covered while enforcement is not implementation-ready" by naming the owning story without reopening Epic 7.

**5.1.3 — Epic List, Epic 8 entry**

OLD:
```text
### Epic 8: Operational Dashboards & Observability
Make the system operable in production: operational dashboards for mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, and audit lag; published SLOs with error budgets and alert thresholds; and measurable operational outcome metrics across all operation classes via OpenTelemetry.
**FRs covered:** FR67 (full dashboards S8/S10), FR94.
```
NEW:
```text
### Epic 8: Operational Dashboards & Observability
Make the system operable in production: operational dashboards for mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, and audit lag; published SLOs with error budgets and alert thresholds; measurable operational outcome metrics across all operation classes via OpenTelemetry; and the control-plane runtime activation (Story 8.7) that materializes the durable control-state/rate-limit projection + periodic trigger so the Epic 7 control floor and the deferred evaluators are live at runtime, not inert.
**FRs covered:** FR67 (full dashboards S8/S10), FR94 (+ runtime enforcement activation for the FR74/FR75 control floor; closes the cross-epic deferred-runtime debt from Epic 7/8 retros).
```

**5.1.4 — Epic 8 body: add Story 8.7 (after Story 8.6, before the `---` closing Epic 8)**

NEW (insert):
```text
### Story 8.7: Control-plane runtime activation — durable control-state/rate-limit projection and periodic enforcement trigger

> Resolves readiness finding CR-2. Closes Epic 7 retro AI#1/#2 and Epic 8 retro AI#1/#2 (the cross-epic deferred-runtime backlog with no owning story). The Epic 7 control floor over `GovernedOperationAggregate` and the Epic 8 emission/contract/projector layers stay as-is; this story makes them live at runtime.

As a tenant administrator and platform-operations engineer,
I want disable/quarantine/rate-limit decisions and the deferred operational evaluators backed by a durable control-state projection and a periodic runtime trigger,
So that an admin's control decision actually blocks or throttles the targeted subject at runtime instead of being recorded against an inert `AlwaysActive…`/`AlwaysUnlimited…` default.

**Acceptance Criteria:**

**Given** a `GovernedOperationAggregate` control-state change (disable/quarantine/rate-limit on a mailbox source, service client, AI actor, command capability, or outbound channel)
**When** it is recorded
**Then** a durable read-side control-state/rate-limit projection materializes the current control state per (tenant × subject) and replaces the `AlwaysActive…ControlStateProvider` / `AlwaysUnlimited…RateLimitProvider` defaults at the enforcement seam (FR74, FR75).

**Given** a disabled or quarantined subject
**When** that subject next attempts a governed operation (intake / command / outbound) through the CommandGateway
**Then** admission fails closed with the catalogued user-safe reason and an audit event — proven by a test where the identical operation succeeds before the control change and is blocked after (FR74, FR68, NFR7, NFR15a).

**Given** a rate-limited subject
**When** it exceeds the configured per-tenant limit
**Then** further operations are throttled/deferred per the limit while unrelated tenants and subjects are unaffected — proven by test (FR75, NFR30).

**Given** the periodic runtime trigger (Dapr-timer / `BackgroundService`)
**When** it runs
**Then** it drives the deferred 7.6–7.11 notification/escalation/throttle/backlog/rubber-stamp evaluators, the Epic-8 `OperationalAlertWiringCoordinator` (8.4) and the weekly 100-item runbook sampler (8.5), and the per-tenant audit-checkpoint feed for the audit-projection-lag gauge (FR67, FR72, FR73, NFR43, NFR44, NFR50a).

**Given** a control-state or revocation change
**When** the projection updates
**Then** bounded staleness and revocation-sensitive invalidation hold per NFR6 (≤ 5 min ordinary, ≤ 60 s revocation), verified by a revocation test.

**Given** the activation lands
**When** architecture-fitness / conformance tests run
**Then** a mechanical test asserts no enforcement seam reads an `AlwaysActive…`/`AlwaysUnlimited…` default on the wired runtime path (guard against silent re-inerting); the build is Release-clean (TreatWarningsAsErrors) and the default test lane is green.

**Release gate:** Epic 9 Story 9.2 (audit-completeness production observable) and Story 9.13 (scoped-outage validation) assume this live runtime loop; Story 8.7 is the runtime backing for those claims and should land before MVP readiness sign-off.
```

**5.1.5 — Epic 10 body: replace Story 10.6 with Story 10.6a + Story 10.6b**

OLD (current Story 10.6 block):
```text
### Story 10.6: Streaming AI response + Stop/Cancel (UX-DR32)

**Planning status:** blocked until the AI-response streaming transport ADR is accepted. Do not assign this story before the architecture decision records whether the implementation extends SignalR projection-nudge or introduces a dedicated streaming channel, while preserving the "never trust payload" and fail-closed posture.

As a user,
I want AI responses to stream with an always-reachable Stop/Cancel control,
So that I can interrupt generation safely.
… (existing ACs) …
```
NEW:
```text
### Story 10.6a: AI-response streaming transport ADR (resolves CR-1 blocker)

As a frontend/solution architect,
I want an accepted ADR that fixes the AI-response streaming transport,
So that Story 10.6b implements progressive rendering on a decided, safe transport instead of an open question.

**Acceptance Criteria:**

**Given** the open decision in `architecture.md` §Frontend Architecture
**When** the ADR is authored
**Then** it records the chosen transport — extend the SignalR projection-nudge model vs introduce a dedicated streaming channel — with rationale, the rejected alternative, and consequences, saved as `docs/adrs/ai-response-streaming-transport.md`.

**Given** the chosen transport
**When** evaluated against the safety floor
**Then** the ADR demonstrates it preserves "never trust payload" (re-query/verify against server state, never trust pushed payload) and the fail-closed posture, and introduces no ungoverned write path that bypasses CommandGateway.

**Given** the ADR is accepted
**When** Story 10.6b is planned
**Then** 10.6b's transport acceptance criterion references this ADR and 10.6b is unblocked.

> Deliverable is the accepted ADR (no production code). This is the assignable decision work that CR-1 requires; it converts the prior "blocked" marker into owned work.

### Story 10.6b: Streaming AI response + Stop/Cancel (UX-DR32)

**Planning status:** blocked until Story 10.6a's AI-response streaming transport ADR is accepted. Do not assign before the ADR records the transport (SignalR projection-nudge extension vs dedicated streaming channel) while preserving "never trust payload" and fail-closed posture.

As a user,
I want AI responses to stream with an always-reachable Stop/Cancel control,
So that I can interrupt generation safely.

**Acceptance Criteria:**

**Given** an AI proposal/response, **When** it generates, **Then** it renders progressively and a Stop/Cancel control is always keyboard-reachable in a stable focus position (no focus-stealing appear/disappear).

**Given** the Stop/Cancel control, **When** activated, **Then** it announces "Response stopped" politely via a live region and returns focus to the composer or proposal panel; reduced-motion is respected.

**Given** the streaming path, **When** implemented, **Then** the transport conforms to the ADR accepted in Story 10.6a (`docs/adrs/ai-response-streaming-transport.md`) and preserves "never trust payload" + fail-closed.
```
Rationale: Splits the unresolved ADR from the UX implementation (CR-1's recommended fix), so the ADR is assignable and 10.6b has a concrete dependency rather than a floating blocker.

### 5.2 `architecture.md` Edits

**5.2.1 — Control-floor note (lines 199-204)**

OLD:
```text
… so the floor is wired and unit-tested yet inert until a durable read-side projection materializes tenant
control state. This materialization was originally targeted for Epic 8, but Epic 8 delivered only the
operational-metric pipeline and read-only observability surfaces; the control-state/rate-limit projection
and the periodic runtime trigger remain deferred beyond Epic 8 (carried forward as Epic 8 retro action items
#1–#2).
```
NEW:
```text
… so the floor is wired and unit-tested yet inert until a durable read-side projection materializes tenant
control state. This materialization is now owned by **Story 8.7 (Control-plane runtime activation)**, which
delivers the durable control-state/rate-limit read-side projection plus the periodic runtime trigger,
replaces the `AlwaysActive…`/`AlwaysUnlimited…` defaults, and proves disabled/quarantined/rate-limited
subjects are actually blocked or throttled. Story 8.7 also consolidates the deferred 7.6–7.11 evaluator,
8.4 alert-coordinator, 8.5 runbook-sampler, and audit-checkpoint triggers (Epic 7 retro AI#1/#2 and Epic 8
retro AI#1/#2). Until Story 8.7 lands, the floor remains wired-but-inert.
```

**5.2.2 — Streaming open-decision note (lines 393-397)**

OLD:
```text
- **Open decision — AI-response streaming transport (resolve before Story 10.6):** the current spine carries
  SignalR projection-nudge only (re-query on nudge, never trust payload). UX-DR32 requires progressive AI
  response rendering with an always-reachable Stop/Cancel. Decide and record an ADR: extend the SignalR
  projection-nudge model vs introduce a dedicated streaming channel. Must not weaken the "never trust payload"
  or fail-closed posture.
```
NEW:
```text
- **Open decision — AI-response streaming transport (owned by Story 10.6a; resolve before Story 10.6b):** the
  current spine carries SignalR projection-nudge only (re-query on nudge, never trust payload). UX-DR32 requires
  progressive AI response rendering with an always-reachable Stop/Cancel. **Story 10.6a** decides and records the
  ADR at `docs/adrs/ai-response-streaming-transport.md`: extend the SignalR projection-nudge model vs introduce a
  dedicated streaming channel. **Story 10.6b** implements against the accepted ADR. The decision must not weaken
  the "never trust payload" or fail-closed posture.
```

### 5.3 `sprint-status.yaml` Edits

**5.3.1 — Add Story 8.7 under Epic 8** (after `8-6-…: backlog`)

NEW:
```yaml
  # Story 8.7 added by sprint-change-proposal-2026-06-09-readiness-blockers (CR-2: control-plane runtime activation;
  # closes Epic 7 retro AI#1/#2 + Epic 8 retro AI#1/#2). FR74/FR75 enforcement is not production-real until 8.7 passes.
  8-7-control-plane-runtime-activation: backlog
```

**5.3.2 — Split Story 10.6** — replace:
```yaml
  # Blocked until the AI-response streaming transport ADR is accepted.
  10-6-streaming-ai-response-and-stop-cancel: backlog
```
with:
```yaml
  # Story 10.6 split by sprint-change-proposal-2026-06-09-readiness-blockers (CR-1): 10.6a ADR + 10.6b implementation.
  10-6a-streaming-transport-adr: backlog
  # 10.6b is blocked until the 10.6a AI-response streaming transport ADR is accepted.
  10-6b-streaming-ai-response-and-stop-cancel: backlog
```

**5.3.3 — Refresh `last_updated`** to the proposal application timestamp.

### 5.4 Majors, Minors & UX Warnings — Disposition

| Finding | Disposition |
| --- | --- |
| **MA-1** Epic 1 technical-foundation framing | Accepted as-is (greenfield starter). Sprint-planning discipline: every Epic 1 foundation story must name its link to the Story 1.9 value anchor. No artifact edit. |
| **MA-2** Epic 10 mixes capability + migration | Already mitigated — Epic 10 header (line 2798-2800) frames it explicitly as the M2 release-readiness closure with governed-chat value. Keep; no split. |
| **MA-3** Story 2.8 → 8.6 saga boundary | **Already resolved in `epics.md`** — Story 2.8 carries an explicit Ownership note deferring production-saga readiness to Story 8.6. Add a one-line `sprint-status.yaml` comment above `8-6-…` making the gate visible at story-creation. |
| **MA-4** ADR prerequisites not all assignable | Streaming ADR → now Story 10.6a (assignable). Production saga/Dapr Workflow → Story 8.6 (assignable, `backlog`). WORM backing + audit two-phase ADRs already exist in `docs/adrs/`. Remaining (M365/Graph specifics, schema evolution/upcasting) belong to already-`done` epics; record as a planning note that any future re-open cites the ADR as an acceptance prerequisite. No new story required now. |
| **MA-5** Spine-only UX import discipline | Already covered by cross-cutting guidance (`epics.md:593-595`). Sprint-planning checklist item: every UI/surface story cites the relevant UX-DR + surface-state table + accessibility floor + M1/M2 gate row. No artifact edit. |
| **MI-1** Parent containers (1.1, 7.27) | **Already resolved** — both carry the strengthened "parent planning container only" marker. Verify they stay out of sprint-status candidate lists. |
| **MI-2** Technical story titles | Sprint-story discipline: preserve the user/operator/security outcome in story-file titles. No epic edit. |
| **MI-3** Epic 10 inline GWT formatting | New Stories 10.6a/10.6b are authored in full multi-line GWT (partial fix). Action: expand 10.1-10.5 ACs to multi-line GWT at story-creation time. |
| **UX-1** Spine-only acceptance burden | Covered by MA-5 discipline (line 593). No edit. |
| **UX-2** M1/M2 surface-assignment gate | Covered by cross-cutting guidance (line 594) — every S4/S6/S7/S8/S9/S10 story imports the `m1-m2-surface-elaboration.md` row before assignment. No edit. |

## 6. Implementation Handoff

**Scope classification: Moderate** (backlog reorganization: 2 new assignable stories + targeted artifact edits).

| Recipient | Responsibility |
| --- | --- |
| Product Owner / Developer | Apply §5.1 (`epics.md`) and §5.3 (`sprint-status.yaml`) edits. |
| Solution Architect (Winston) | Author `docs/adrs/ai-response-streaming-transport.md` (Story 10.6a) and own the Story 8.7 control-plane runtime-activation design. Apply §5.2 (`architecture.md`) edits. |
| Developer (Amelia) | Implement Story 8.7 (projection + periodic trigger + seam replacement) and Story 10.6b (against the accepted ADR). |
| Test Architect (Murat) | Author the enforcement-proof tests for Story 8.7 (block-after-disable, throttle-after-limit, no-`AlwaysActive…`-on-wired-path guard) and re-run `bmad-check-implementation-readiness` to confirm CR-1 and CR-2 are closed. |

## 7. Success Criteria

- `epics.md` frontmatter reports `storyCount: 121` with `readinessBlockersResolvedAt: "2026-06-09"`.
- Story 8.7 exists in Epic 8 with enforcement-proof ACs; FR74/FR75 coverage-map entries name Story 8.7 as the enforcement-activation owner.
- Story 10.6 is split into assignable 10.6a (ADR) + blocked-on-10.6a 10.6b; `docs/adrs/ai-response-streaming-transport.md` is named as the 10.6a deliverable.
- `architecture.md` no longer describes the inert control floor or the streaming decision as ownerless.
- `sprint-status.yaml` carries `8-7-…`, `10-6a-…`, `10-6b-…`, refreshed `last_updated`, and the 2.8→8.6 gate comment.
- A follow-up `bmad-check-implementation-readiness` run reports **no critical findings** (CR-1 and CR-2 closed); majors/minors are dispositioned as planning actions above.

## 8. Approval and Routing

Approval status: **pending Jerome approval.**

On approval: Product Owner/Developer applies §5.1/§5.3, Architect applies §5.2 and is queued for the 10.6a ADR + 8.7 design. No code implementation starts for Story 10.6b until the 10.6a ADR is accepted; FR74/FR75 enforcement is not treated as production-real until Story 8.7 passes.
