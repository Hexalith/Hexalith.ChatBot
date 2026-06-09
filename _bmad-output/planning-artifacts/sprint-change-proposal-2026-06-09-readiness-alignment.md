---
title: Sprint Change Proposal - Implementation Readiness Alignment
project: Chatbot
date: 2026-06-09
status: approved
mode: Batch
trigger: "Implementation readiness report 2026-06-09 found NEEDS WORK: stale epics metadata and Epic 10 not integrated into the M0/M1/M2 sequencing model."
scope_classification: Moderate
recommended_approach: Direct Adjustment
owner: Jerome
prepared_at: 2026-06-09T21:48:59+02:00
approved_by: Jerome
approved_at: 2026-06-09T21:48:59+02:00
implementation_state: planning-artifacts-applied
source_report: "_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-09.md"
previous_related_proposal: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09.md"
---

# Sprint Change Proposal - Implementation Readiness Alignment

## 1. Issue Summary

The implementation readiness assessment completed on 2026-06-09 with status **NEEDS WORK**. The core requirements are not missing: the final extraction found 111 FR/sub-FR items, 77 NFR/sub-NFR items, and 100% FR-ID coverage in the epics. The issue is planning consistency after the approved 2026-06-09 proposal added Epic 10.

The two critical findings are:

- `epics.md` metadata and overview remain stale: frontmatter says `epicCount: 9` and `storyCount: 107`, while the body includes Epic 10 and 119 assignable stories.
- Epic 10 is appended after Epic 9 but is not explicitly integrated into the M0/M1/M2 release sequencing model.

Evidence:

- `_bmad-output/planning-artifacts/epics.md` frontmatter still has `epicCount: 9` and `storyCount: 107`.
- `_bmad-output/planning-artifacts/epics.md` Epic List says "9 epics across the 3 fixed increments."
- `_bmad-output/planning-artifacts/epics.md` contains `## Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` already contains Epic 10 backlog entries, so sprint status is not missing Epic 10, but its timestamp/comments should be refreshed when the planning baseline is corrected.

This is a documentation and backlog-baseline correction. It does not require code rollback.

## 2. Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] Skip | Trigger is a readiness assessment, not a failed implementation story. Affected artifacts are `epics.md`, the PRD addendum, architecture/ADR gating, UX discovery conventions, and sprint status comments. |
| 1.2 Core problem | [x] Done | Planning metadata and sequencing did not catch up after the approved Epic 10 change. |
| 1.3 Evidence | [x] Done | Readiness report, stale `epics.md` frontmatter/list, Epic 10 body section, and existing Epic 10 sprint-status entries. |
| 2.1 Current epic impact | [x] Done | Epics 1-9 remain valid. Epic 10 remains valid but must be placed in the release model. |
| 2.2 Epic-level changes | [x] Done | Update counts and Epic List; classify Epic 10 as an M2 release-readiness closure epic. |
| 2.3 Remaining epics | [x] Done | No epic is obsolete. Story 10.6 is not assignment-ready until the streaming transport ADR is accepted. |
| 2.4 Future epic validity | [x] Done | No new epic required. No existing epic removed. |
| 2.5 Priority/order | [x] Done | Preserve M0 -> M1 -> M2; place Epic 10 under M2 before MVP readiness sign-off. |
| 3.1 PRD conflicts | [!] Action-needed | `addendum.md` is `status: draft` while PRD/architecture/stories treat it as binding. |
| 3.2 Architecture conflicts | [!] Action-needed | Streaming transport remains an open decision before Story 10.6. |
| 3.3 UI/UX conflicts | [x] Done | Epic 10 UX elaboration exists and aligns with DESIGN/EXPERIENCE, but discovery conventions should not miss nested UX docs again. |
| 3.4 Other artifacts | [!] Action-needed | `sprint-status.yaml` already has Epic 10; refresh timestamp/comments and optionally mark Story 10.6 as blocked-by-ADR. |
| 4.1 Direct Adjustment | [x] Viable | Direct artifact edits fix the baseline without changing implementation scope. Effort Low-Medium; risk Low. |
| 4.2 Rollback | [N/A] Skip | No implementation rollback helps. |
| 4.3 MVP Review | [N/A] Skip | MVP scope was already approved by the prior Epic 10 proposal; this change only aligns readiness metadata and gates. |
| 4.4 Recommendation | [x] Done | Direct Adjustment. |
| 5.1-5.5 Proposal components | [x] Done | This document. |
| 6.1-6.2 Final review | [x] Done | Recommendations are traceable to readiness findings. |
| 6.3 User approval | [!] Action-needed | Pending Jerome approval. |
| 6.4 Sprint-status update | [!] Action-needed | Apply after approval if the team wants status comments/timestamp updated. |
| 6.5 Handoff plan | [x] Done | Product Owner/Developer for planning edits; Architect for streaming ADR; PM/Owner for addendum status. |

## 3. Impact Analysis

### Epic Impact

| Epic | Impact | Required change |
| --- | --- | --- |
| Epics 1-4 | No scope change. Remain M0 vertical thesis path. | None. |
| Epics 5-7 | No scope change. Remain M1 cross-surface parity and governance. | None. |
| Epics 8-9 | No scope change. Remain M2 operations, audit, compliance, and recovery. | None. |
| Epic 10 | Valid, but structurally under-integrated. | Place in the M2 release-readiness sequence as the final surface-composition and governed chat closure epic. Mark Story 10.6 blocked until the streaming transport ADR is accepted. |

Recommended sequencing model:

- M0: Epics 1-4.
- M1: Epics 5-7.
- M2: Epics 8-10.
- Epic 10 is the M2 release-readiness closure epic for FrontComposer Shell adoption and the governed interactive chat surface. It does not weaken the M0/M1/M2 dependency order; it makes MVP readiness sign-off depend on the approved Epic 10 scope.

### Story Impact

- Stories 10.1-10.5 and 10.7 remain assignable once Epic 10 is accepted in the sequence.
- Story 10.6 must carry a planning status of blocked or "do not assign" until the AI-response streaming transport ADR is accepted.
- Story 1.1 and Story 7.27 parent planning containers should remain non-assignable. To prevent accidental assignment, their historical acceptance context should be clearly marked as parent-only/historical or moved to an appendix.
- Story 2.8 already states that it does not claim production saga readiness and links production readiness to Story 8.6. Keep that release gate visible when correcting the readiness baseline.

### Artifact Conflicts

- `epics.md`: stale metadata, stale Epic List text, stale "ChatBot naming/positioning" planning note, missing Epic 10 M2 list entry, and missing Story 10.6 assignment gate.
- `addendum.md`: frontmatter `status: draft` conflicts with its binding use by PRD, architecture, and stories.
- `architecture.md`: already identifies the streaming transport decision, but the readiness baseline should route that to an ADR before Story 10.6.
- UX package: nested UX artifacts exist and are aligned; future discovery should follow planning artifact references or a planning index so nested PRD/UX docs are not missed.
- `sprint-status.yaml`: Epic 10 entries exist; optional update is timestamp/comment only unless a blocked status convention is introduced.

### Technical Impact

No production code impact. No submodule work. No recursive submodule operation. The only implementation-impacting gate is that Story 10.6 should not start until the streaming transport decision is recorded.

## 4. Recommended Approach

**Selected path: Direct Adjustment.**

Apply targeted planning edits:

1. Update `epics.md` metadata and overview counts.
2. Integrate Epic 10 into the M2 part of the Epic List and update the dependency guidance.
3. Mark Story 10.6 as blocked until the streaming ADR is accepted.
4. Promote or explicitly approve binding PRD addendum sections.
5. Clarify non-assignable parent story containers.
6. Refresh `sprint-status.yaml` timestamp/comments if desired.
7. Add or update a planning artifact index/discovery note so nested PRD/UX files are found on future readiness checks.

Effort: Low-Medium. Risk: Low. Timeline impact: one planning pass plus one architecture ADR for Story 10.6.

## 5. Detailed Change Proposals

### 5.1 `epics.md` Frontmatter

OLD:

```yaml
epicCount: 9
storyCount: 107
correctedAt: "2026-05-30"
```

NEW:

```yaml
epicCount: 10
storyCount: 119
correctedAt: "2026-05-30"
readinessAlignedAt: "2026-06-09"
```

Rationale: Aligns metadata with the readiness report's detected body structure: 10 epics and 119 assignable stories.

### 5.2 `epics.md` Overview

OLD:

```text
The MVP is a single release delivered in three dependency-ordered increments: M0 (vertical thesis path, UI-only) -> M1 (cross-surface parity + full governance) -> M2 (operations, recovery, continuity). Architecture and epics must preserve the M0 -> M1 -> M2 dependency order.
```

NEW:

```text
The MVP is a single release delivered in three dependency-ordered increments: M0 (vertical thesis path, UI-only) -> M1 (cross-surface parity + full governance) -> M2 (operations, recovery, continuity, and release-readiness surface closure). Architecture and epics must preserve the M0 -> M1 -> M2 dependency order. Epic 10 is the approved M2 release-readiness closure epic for FrontComposer Shell adoption and the governed interactive chat surface; MVP readiness sign-off requires it to close.
```

Rationale: Places Epic 10 inside the release model without changing the original dependency direction.

### 5.3 `epics.md` Epic List

OLD:

```text
9 epics across the 3 fixed increments (M0 -> M1 -> M2). Dependency flow is strictly forward ...
```

NEW:

```text
10 epics across the 3 fixed increments (M0 -> M1 -> M2). Dependency flow is strictly forward. Epics 1-4 deliver M0, Epics 5-7 deliver M1, and Epics 8-10 deliver M2. Epic 10 is the approved release-readiness closure epic added on 2026-06-09; it is not an appendix and must close before MVP readiness sign-off.
```

Add under `Increment M2` after Epic 9:

```text
### Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption
Deliver the approved FrontComposer Shell adoption and governed interactive chat surface as the final M2 release-readiness closure. Every write remains on the CommandGateway spine; risky requests become Epic 4 proposals; Story 10.6 is blocked until the streaming transport ADR is accepted.
**FRs covered:** Extends FR21, FR40-FR46, FR81, FR81a, FR85, FR86, and the UX-DR5/UX-DR16/UX-DR17/UX-DR32 surface requirements without adding a new governance path.
```

Rationale: Epic 10 becomes visible in the same list used for sequencing and sprint planning.

### 5.4 `epics.md` Cross-Cutting Planning Guidance

OLD:

```text
- "ChatBot" naming/positioning. M0 is a project-conversation view plus review/approval surfaces - not a native chat surface. UX and architecture intentionally avoid a fake chat surface for M0; pilot communication must keep this expectation explicit.
```

NEW:

```text
- "ChatBot" naming/positioning. M0 remains a project-conversation view plus review/approval surfaces, not a native chat surface. The approved governed interactive chat surface is delivered by Epic 10 before MVP readiness sign-off. UX and architecture continue to forbid a fake or ungoverned chat textbox: every message is admitted through CommandGateway, and risky requests become governed AI-action proposals.
```

Rationale: Preserves the M0 limitation while reflecting the approved Epic 10 change.

### 5.5 Story 10.6 Assignment Gate

OLD:

```text
### Story 10.6: Streaming AI response + Stop/Cancel (UX-DR32)
```

NEW:

```text
### Story 10.6: Streaming AI response + Stop/Cancel (UX-DR32)

**Planning status:** blocked until the AI-response streaming transport ADR is accepted. Do not assign this story before the architecture decision records whether the implementation extends SignalR projection-nudge or introduces a dedicated streaming channel, while preserving the "never trust payload" and fail-closed posture.
```

Rationale: Converts the readiness warning into an explicit assignment guard.

### 5.6 `addendum.md` Status

OLD:

```yaml
status: draft
```

Recommended NEW:

```yaml
status: approved
approvedAt: "2026-06-09"
approvalScope: "Binding PRD implementation context for confidence thresholds, risk classifier, command allowlists, tenant policy schema, shared command pipeline, idempotency keys, replay isolation, ID evolution, inbound authenticity, and operating baselines."
```

Alternative if full approval is not intended:

```yaml
status: approved-sections
approvedAt: "2026-06-09"
approvedSections:
  - Confidence Thresholds
  - Risk Classifier
  - Command Allowlist v0
  - Command Allowlist v1
  - Tenant Policy Schema
  - Shared Command Pipeline
  - Idempotency Keys
  - Replay Isolation
  - ID Evolution Contract
  - Inbound Message Authenticity
  - Operating Baselines
```

Rationale: Stories already rely on these sections as binding. The metadata should match actual use.

### 5.7 Parent Planning Containers

Story 1.1 and Story 7.27 already say they are parent planning containers. Strengthen the marker:

```text
**Planning status:** parent planning container only. Do not create a sprint story from this heading. Child stories are the assignable units. Historical acceptance context below is non-assignable evidence only.
```

Rationale: Prevents automated story selection from treating parent containers as assignable work.

### 5.8 Sprint Status

Current state: `_bmad-output/implementation-artifacts/sprint-status.yaml` already includes:

```yaml
  epic-10: backlog
  10-1-frontcomposer-shell-integration: backlog
  10-2-migrate-m0-governed-surfaces-onto-shell: backlog
  10-3-migrate-operational-surfaces-onto-shell: backlog
  10-4-project-workspace-landing-route: backlog
  10-5-governed-chat-composer: backlog
  10-6-streaming-ai-response-and-stop-cancel: backlog
  10-7-cross-surface-a11y-visual-parity-reverification: backlog
```

Recommended update:

- Refresh `last_updated` after applying planning edits.
- Add a comment above Story 10.6: `# Blocked until the AI-response streaming transport ADR is accepted.`

Rationale: No missing sprint-status entries remain, but the readiness gate should be visible where story creation starts.

### 5.9 Planning Artifact Discovery

Add a planning artifact index or discovery note under `_bmad-output/planning-artifacts/` that lists:

- PRD: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- PRD addendum: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Epics: `_bmad-output/planning-artifacts/epics.md`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/m1-m2-surface-elaboration.md`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md`

Rationale: Future readiness checks should not begin from a false missing-document state because nested PRD/UX docs lack `index.md` sharding.

## 6. Implementation Handoff

**Scope classification:** Moderate.

| Recipient | Responsibility |
| --- | --- |
| Product Owner / Developer | Apply `epics.md` count, Epic List, planning guidance, parent-container, and Story 10.6 gate edits. Refresh `sprint-status.yaml` comments/timestamp if desired. |
| Product Manager / Owner | Approve `addendum.md` status or decide section-scoped approval. |
| Architect | Create or approve the AI-response streaming transport ADR before Story 10.6 is assigned. |
| QA/Test Architect | Re-run readiness after the planning edits and confirm the critical count/sequencing findings are closed. |

## 7. Success Criteria

- `epics.md` frontmatter reports `epicCount: 10` and `storyCount: 119`.
- Epic List says 10 epics and places Epic 10 in the M2 release-readiness sequence.
- Story 10.6 has an explicit "blocked until streaming ADR" planning status.
- `addendum.md` no longer presents binding implementation context as unqualified draft material.
- Parent planning containers remain visibly non-assignable.
- Future readiness discovery finds nested PRD and UX artifacts without corrective rediscovery.
- A follow-up readiness check reports no critical metadata/sequencing findings.

## 8. Approval and Routing

Approval status: **approved by Jerome on 2026-06-09**.

Routing complete: direct planning-artifact adjustment applied. No code implementation should start for Story 10.6 until the streaming ADR is accepted.
