---
title: Sprint Change Proposal - Epic 12 Audit/Recovery Runtime, Live Recovery Driver & Memories Derived-Store Deferrals
project: Chatbot
date: 2026-07-20
status: approved
mode: Batch
trigger: "Convert the remaining audit and recovery runtime, live recovery driver, and Memories derived-store deferrals into owned stories or explicit scope waivers before release-readiness sign-off."
scope_classification: Minor
recommended_approach: Direct Adjustment - add three owned stories to Epic 12
owner: Jerome
prepared_by: Correct Course workflow
approved_by: Jerome
approved_at: 2026-07-20
implementation_state: stories-added-sprint-status-updated
handoff_status: routed-to-architect-and-developer
---

# Sprint Change Proposal - Epic 12 Audit/Recovery Runtime, Live Recovery Driver & Memories Derived-Store Deferrals

## 1. Issue Summary

`_bmad-output/implementation-artifacts/sprint-status.yaml` carries an open action item under `epic: 12`:

> "Convert the remaining audit and recovery runtime, live recovery driver, and Memories derived-store deferrals into owned stories or explicit scope waivers before release-readiness sign-off." (owner: John / Winston)

Epic 12 ("Tamper-Evident Audit, Compliance Investigation & Recovery") shows all 13 stories `done`, but every one of them documents — honestly, in its own Completion Notes — an intentional "inert-control-floor" deferral. This is not a new discovery: the **Epic 9 retrospective (2026-06-03)**, written when this epic was still numbered 9, already named all three gaps precisely and raised explicit action items (AI#1, AI#3, AI#4) to convert them into owned work. Those retro action items were never turned into backlog stories; they were eventually consolidated into today's single open action item, and nothing has closed since.

### Evidence collected

1. **Audit/recovery runtime (retro AI#1).** Stories 12.1 (WORM chain verifier), 12.2 (audit-completeness measurer), 12.4 (replay-isolation probe), 12.5 (derived-store-isolation probe), and 12.6 (correction-propagation SLO sweep) each built and fully tested a coordinator, but **no scheduler ever calls any of them**. Every story's Completion Notes states explicitly: "No always-on `BackgroundService`/Dapr-timer is wired... a scheduler need only call `X` on its cadence."
2. **Live recovery driver (retro AI#3).** Stories 12.11-12.13 (continuity drill, projection-rebuild validation, scoped-outage degradation validation) only run against scripted fakes. Their real seams — `IContinuityDrillScenarioRunner`, `IProjectionRebuildDriver`, `IScopedOutageInjectionDriver` — all have inert default implementations that throw `NotSupportedException("...M2-deferred")`. RPO ≤ 15 min / RTO ≤ 4 hr (PRD assumption A10) remain unproven against any real environment.
3. **Memories derived-store (retro AI#4).** Stories 12.5/12.6 built the tenant-partition contract (`DerivedStorePartition`, `IDerivedStore`, `IVectorReindexer`) and an in-memory default, but `grep -rn "Hexalith.Memories" --include=*.csproj .` confirms **no ChatBot project references Hexalith.Memories today**. The live Redis-Vector/FalkorDB binding does not exist.

All three are explicitly, honestly documented as *intentional M2 deferrals* — not silent gaps. But per the PRD, **M2 is part of the single MVP release** (M0 → M1 → M2, one release, not a post-MVP phase), so they cannot remain open prose indefinitely if release-readiness sign-off is to mean anything.

## 2. Correct Course Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | Done | Epic 12 action item, sprint-status.yaml:209-212. |
| 1.2 Core problem | Done | Technical limitation discovered during implementation (inert-control-floor pattern), compounded by a planning-process failure: retro action items were never converted to stories across three consecutive epics (per Epic 9 retro's own "Significant Discoveries"). |
| 1.3 Evidence | Done | Verified directly in the 13 canonical-9-prefixed story files, the Epic 9 retrospective (2026-06-03), architecture.md (Memories = M2, NFR9a), and a repo-wide `grep` confirming no live Memories reference. |
| 2.1 Current epic | Done | Epic 12 (`in-progress` after this change) can still be completed as planned — no scope reduction, just three additional stories. |
| 2.2 Epic-level change | Done | Add 3 stories (12.14, 12.15, 12.16) to existing Epic 12. No new epic; no epic removed. |
| 2.3 Remaining epics | Done | Only Epic 13 follows Epic 12; it has no dependency on the new stories' outcome. |
| 2.4 Future validity | Done | No story becomes obsolete. |
| 2.5 Priority/order | Done | New stories are additive at the end of Epic 12; no resequencing needed. |
| 3.1 PRD conflict | N/A | FR55a, NFR9a, NFR56-59, and A10 already specify exactly this work; no requirement changes. |
| 3.2 Architecture conflict | N/A | architecture.md already frames Hexalith.Memories and the Aspire/AKS deploy topology as M2 concerns; no architecture edit required. |
| 3.3 UX conflict | N/A | No user-facing surface, behavior, accessibility, or localization impact — all three stories are internal/operational. |
| 3.4 Other artifacts | Done | `epics.md` (3 new stories) and `sprint-status.yaml` (epic status + story rows + action item) updated. No CI/CD, IaC, or test-strategy document requires a separate edit. |
| 4.1 Direct Adjustment | Viable | Add 3 stories to existing epic. Effort: Medium (12.14 low, 12.15 medium given sandbox environment work, 12.16 low-medium). Risk: Low — all three are additive on already-built, already-tested seams. |
| 4.2 Rollback | Not viable | Nothing to roll back; the deferrals were intentional and correctly implemented for their own story scope. |
| 4.3 MVP Review | Not viable | No scope reduction pressure; A10 already anticipates and frames this exact recalibration path. |
| 4.4 Recommended path | Done | Direct Adjustment — add Stories 12.14, 12.15, 12.16 to Epic 12. |
| 5.1-5.5 Proposal components | Done | Sections 1-5 below. |
| 6.1 Checklist review | Done | All applicable items resolved. |
| 6.2 Accuracy review | Done | Story content cross-checked against the actual `9-*` story files' Completion Notes and the Epic 9 retro's Action Items #1/#3/#4. |
| 6.3 Explicit approval | Done | Jerome approved both the Direct Adjustment approach and targeting Story 12.15 at the existing Aspire/DAPR sandbox (2026-07-20). |
| 6.4 Sprint update | Done | `sprint-status.yaml` updated: epic-12 → `in-progress`, three new `backlog` story rows added, action item text updated to reference the new stories (status remains `open` until they reach `done`). |
| 6.5 Handoff | Done | Winston (12.14 spec + 12.15 architecture/live-driver design), Amelia (12.14/12.16 implementation), Murat (12.15 evidence validation). |

## 3. Impact Analysis

### Epic and story impact

- Epic 12 reopens to `in-progress`; no story is renumbered or removed. Three new stories (12.14-12.16) are appended.
- No other epic is affected. Epic 13 (UI conformance) has no dependency on this work.

### Architecture impact

None. The architecture document already frames the Hexalith.Memories live binding and the Aspire/AKS deployment topology as M2-scoped concerns (architecture.md: Memories row in the platform decisions table; Aspire 13.3.x AKS/Helm row noted "relevant to M2 ops"). These stories implement what was already architected, they do not change the architecture.

### Technical and repository impact

- `epics.md`: 3 new stories appended to Epic 12 (after Story 12.13, before the Epic 13 heading).
- `sprint-status.yaml`: epic-12 status flip, 3 new backlog story rows, action item text updated.
- No source code changed by this proposal — implementation is future dev work owned by the new stories.

### PRD and UX impact

None. FR55a, NFR9a, NFR56, NFR57, NFR58, NFR59, and PRD assumption A10 already specify this exact work; no requirement text changes.

## 4. Recommended Approach

**Direct Adjustment** — add three owned stories to Epic 12:

1. **Story 12.14** — Wire the M2 audit and recovery runtime scheduler (pure activation of already-built coordinators from 12.1, 12.2, 12.4, 12.5, 12.6, reusing the control-plane runtime already delivered by canonical Story 9.1 / legacy 8.7a/8.7b).
2. **Story 12.15** — Stand up live recovery/continuity fault-injection drivers (`IContinuityDrillScenarioRunner`, `IProjectionRebuildDriver`, `IScopedOutageInjectionDriver`) against the existing Aspire/DAPR sandbox topology used for Tier-3 live E2E validation, and recalibrate the A10 [ASSUMPTION] targets with real evidence. Any scenario that cannot be faithfully reproduced in that sandbox (e.g., true production-scale AKS outage) is recorded as an explicit residual follow-up, not silently closed.
3. **Story 12.16** — Bind the live Hexalith.Memories derived-store backing (Redis-Vector/FalkorDB via `IndexSchemaDefinitions`), additive on the existing `IDerivedStore`/`IVectorReindexer` seam, including the delete-seam follow-up flagged by Story 12.5's own Senior Review.

This directly satisfies the action item's ask ("owned stories") rather than a scope waiver, because all three deferrals are buildable now against infrastructure and seams that already exist — none require capability the team doesn't already have.

## 5. Detailed Change Proposals

### `epics.md` — Epic 12, append after Story 12.13

Full story text (Given/When/Then acceptance criteria, owners, and explicit cross-references to the retro action items they convert) added for:

- **Story 12.14: Wire the M2 audit and recovery runtime scheduler**
- **Story 12.15: Stand up live recovery/continuity fault-injection drivers and recalibrate A10**
- **Story 12.16: Bind the live Hexalith.Memories derived-store backing**

**Rationale:** Each story converts one of the Epic 9 retrospective's named action items (#1, #3, #4 respectively) into owned, independently-sprintable scope, consistent with the precedent set by Stories 8.7a/8.7b (which successfully converted the analogous Epic 7/8 control-floor deferral into owned, landed work).

### `sprint-status.yaml`

**Old:**
```yaml
epic-12: done
...
epic-12-retrospective: done
...
  - epic: 12
    action: "Convert the remaining audit and recovery runtime, live recovery driver, and Memories derived-store deferrals into owned stories or explicit scope waivers before release-readiness sign-off."
    owner: "John / Winston"
    status: open
```

**New:**
```yaml
epic-12: in-progress
...
12-14-wire-the-m2-audit-and-recovery-runtime-scheduler: backlog
12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10: backlog
12-16-bind-the-live-hexalith-memories-derived-store-backing: backlog
epic-12-retrospective: done
...
  - epic: 12
    action: "Converted via sprint-change-proposal-2026-07-20-epic12-recovery-deferrals.md into owned Stories 12.14 (M2 audit/recovery runtime scheduler), 12.15 (live recovery/continuity fault-injection drivers + A10 recalibration, targeting the Aspire/DAPR sandbox), and 12.16 (live Hexalith.Memories derived-store binding). Item stays open until all three reach done."
    owner: "John / Winston"
    status: open
```

**Rationale:** The epic must reopen because not all its stories are `done` anymore. The action item stays `open` (not resolved) — creating the stories satisfies "convert into owned stories," but the underlying implementation work is not yet complete.

## 6. Out-of-scope, related debt (noted, not acted on)

The same Epic 9 retrospective raised three further action items not covered by today's trigger and left untouched here:

- AI#2 — bookkeeping-drift prevention gate (owner: Amelia)
- AI#5 — missing Epic 6 retrospective / aggregate-row reconciliation (owner: Paige/Amelia)
- AI#6 — regenerate OpenAPI client for Story 12.3's (legacy 9.3) compliance-audit endpoints (owner: Paige/Amelia)

These are real, separately-tracked debt items but are outside the scope of the action item this proposal resolves ("audit and recovery runtime, live recovery driver, and Memories derived-store deferrals"). Flagged here for visibility only.

## 7. Implementation Handoff

| Recipient | Responsibility |
| --- | --- |
| Winston (Architect) | Author the ADRs and detailed technical design for Stories 12.14 and 12.15; confirm the Aspire/DAPR sandbox is suitable for 12.15's live fault-injection scenarios. |
| Amelia (Developer) | Implement Stories 12.14 and 12.16; support 12.15's driver implementation. |
| Murat (Test Architect) | Validate Story 12.15's evidence artifacts are real (not fabricated) and that the A10 recalibration is properly logged. |

**Success criteria:** Stories 12.14, 12.15, and 12.16 each reach `done` with the same evidentiary rigor already applied across Epic 12 (no bookkeeping drift, no fabricated pass, explicit Completion Notes), after which the epic-12 action item can be marked resolved and closed.

## 8. Approval and Workflow Log

Jerome explicitly approved the Direct Adjustment approach and confirmed Story 12.15 should target the existing Aspire/DAPR sandbox rather than a narrower scope waiver (2026-07-20).

| Event | Result |
| --- | --- |
| Change analysis | Completed against epics.md, sprint-status.yaml, the 13 Epic 12 story files, the Epic 9 retrospective (2026-06-03), and architecture.md. |
| Selected approach | Direct Adjustment; 3 new owned stories. |
| Scope | Minor — additive stories within an existing epic; no PRD/architecture/UX change. |
| Artifact updates | `epics.md` (Epic 12 +3 stories), `sprint-status.yaml` (epic status, 3 story rows, action item text). |
| Handoff | Winston / Amelia / Murat. |
