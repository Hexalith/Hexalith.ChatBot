---
title: Sprint Change Proposal (Pass 2) - Chatbot Readiness Residual Hardening
project: Chatbot
date: 2026-05-30
status: applied
source_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-30.md
predecessor_proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-30.md
scope_classification: Moderate
recommended_approach: Direct Adjustment (split / inline-floor / strengthen / anchor)
epic7_decision: "Option A — keep 15 subject×action control stories, inline the floor into each (chosen by Jerome over the recommended Option B consolidation)"
mode: Batch
owner: Jerome
approved_by: Jerome
approved_at: 2026-05-30
applied_at: 2026-05-30
target_artifact: _bmad-output/planning-artifacts/epics.md
story_count_before: 100
story_count_after: 107
---

# Sprint Change Proposal (Pass 2) — Chatbot Readiness Residual Hardening

> **Sequencing note.** A first corrective pass (`sprint-change-proposal-2026-05-30.md`, `status: applied`) already fixed an earlier wave (Story 3.2 sizing split into 7 rendering stories, Epic 6 ordering, 2.8/4.9/9.6 correction-coupling). The current `epics.md` (`correctedAt: 2026-05-30`) reflected it at 100 stories. **This pass 2 addresses the residual 11 attention items** surfaced when the readiness check was re-run against the already-corrected epics. It does **not** touch the applied pass-1 record.

## 1. Issue Summary

The 2026-05-30 implementation-readiness assessment rates the planning package **NEEDS WORK before full Phase 4 implementation**. The hard things are done: **111/111 FR coverage**, **no critical blocker**, and PRD / UX / architecture / epics aligned at surface and architecture level. The residual gap is **story hardening, not product-definition repair**.

The report identified **11 attention items**: **5 Major** epic/story-quality issues, **3 Minor** concerns, **3 UX** alignment warnings.

**Issue type:** planning/backlog correction (under-specification & sizing debt). No code rollback required — no implementation artifacts exist under `_bmad-output/implementation-artifacts/`, and there is no chatbot-level `sprint-status.yaml` to reconcile.

## 2. Impact Analysis

### 2.1 Epic Impact

| Epic | Impact | Change applied |
| --- | --- | --- |
| **Epic 1** — First Safe Governed Action & Command Spine | Value-framing durability. Acceptable only as a foundation exception anchored to Story 1.9. | Added a **value-anchor invariant** to the epic header, a one-line **Anchor:** to each foundation story 1.1–1.8, and an optional-split **Planning note** to Story 1.1. (21 stories, unchanged count.) |
| **Epic 7** — Tenant Administration & Governance Policy | Largest impact. Story 7.6 over-broad; Stories 7.7–7.21 silently inherited a shared acceptance floor. | **Split 7.6 → 6 stories** (7.6–7.11); **kept all 15 control stories (Option A)**, renumbered 7.7–7.21 → **7.12–7.26** with the floor **inlined per story**; renumbered 7.22 → **7.27**. Epic 7: **22 → 27 stories**. |
| **Epic 8** — Operational Dashboards & Observability | Story 8.2 explicitly not sprint-ready. | **Split 8.2 → 3 stories** (8.2 telemetry / 8.3 SLO+budgets / 8.4 alert wiring); renumbered 8.3 → **8.5**. Epic 8: **3 → 5 stories**. |
| **Epic 9** — Tamper-Evident Audit, Compliance & Recovery | Stories 9.7–9.13 too thin (single happy-path AC) for compliance/recovery risk. | **Strengthened 9.7–9.13 in place** with negative-path + evidence ACs. No renumber. (13 stories.) |
| Epics 2–6 | No structural change. | Minor **Beneficiary:** clarification on Stories 2.3, 2.4, 3.14, 4.1, 4.7. |

**Net story-count change:** Epic 7 +5, Epic 8 +2 ⇒ **100 → 107 stories** (`storyCount: 107`).

### 2.2 Story Impact

- **Renumbered:** Epic 7 — 7.6→(7.6–7.11), 7.7–7.21→(7.12–7.26), 7.22→7.27; Epic 8 — 8.3→8.5.
- **Strengthened (content only, no renumber):** 9.7, 9.8, 9.9, 9.10, 9.11, 9.12, 9.13.
- **Annotated (one-line additions):** 1.1–1.8 (anchors), 1.1 (planning note), 2.3/2.4/3.14/4.1/4.7 (beneficiary).

### 2.3 Artifact Conflicts

- **`epics.md`** — the single artifact edited: story bodies, the FR Coverage Map line for FR74, and frontmatter `storyCount: 100 → 107`.
- **PRD / Architecture** — no conflict. (Optional one-line PRD positioning note for UX-3 left to Jerome.)
- **UX** — no spec change; 3 warnings captured as a binding cross-cutting guidance subsection in `epics.md`.
- **Readiness report & pass-1 proposal** — left untouched (historical records).

### 2.4 Technical Impact

None beyond planning. The splits do not change the architecture, the shared command pipeline (FR81a), the M0/M1/M2 sequencing, or any FR/NFR contract. Traceability is preserved because the FR Coverage Map maps FRs to **epics**, not story numbers.

## 3. Recommended Approach

**Selected path: Direct Adjustment** (Option 1), realized as *split + inline-floor + strengthen + annotate* within the existing epic structure.

**Rationale:** Effort **Medium**, Risk **Low**, timeline impact **none for M0**. Rollback is moot (nothing implemented); MVP review unnecessary (scope unchanged). Direct Adjustment preserves the strong traceability the report praised while removing the sizing/inheritance debt that blocked confident sprint assignment.

**Epic 7 control-story decision — Option A (chosen by Jerome).** The report sanctioned two fixes for the 7.7–7.21 shared-floor problem. The pass-2 *recommendation* was Option B (consolidate to 5 control-surface stories). **Jerome elected Option A:** keep all 15 subject×action stories and inline the relevant floor ACs into each. Both remove the cross-story dependency; Option A preserves the finest-grained backlog (one story per subject×action cell) at the cost of repeating the floor per story. Applied as Option A.

## 4. Detailed Change Proposals (as applied)

---

### MAJOR-2 — Split Story 7.6 into six stories (applied)

Story 7.6 had bundled notification routing, escalation, approval-queue prioritization/grouping, throttling/digest, reviewer-backlog alerting, rubber-stamp observability, **and** a shared acceptance floor for 15 later stories. Replaced with six focused stories; the shared floor was removed here and inlined into the control stories (MAJOR-1).

- **7.6 Notification routing and delivery** (FR72, FR73, NFR2)
- **7.7 Escalation policy for unresolved states** (FR73, FR59)
- **7.8 Approval queue prioritization and grouping** (NFR46)
- **7.9 Notification throttling and digest rollup** (NFR46, NFR2)
- **7.10 Reviewer backlog alerting** (NFR46, NFR2)
- **7.11 Rubber-stamp-rate observable** (NFR46, FR41)

Full Given/When/Then bodies are in `epics.md` (the applied source of truth).

---

### MAJOR-1 — Keep 15 control stories, inline the shared floor into each (Option A, applied)

Each of the 15 subject×action stories carried a single local AC, with audit/scope/two-person-rule/service-client-&-AI-actor prohibitions living only in 7.6's shared floor — so a story could be pulled into a sprint without its security floor. **Option A:** keep all 15 stories, inline the relevant floor ACs into each, remove the shared floor.

**Renumbering (each story keeps its original local AC and gains the floor ACs below):**

| Old | New | Subject × action |
| --- | --- | --- |
| 7.7 / 7.8 / 7.9 | 7.12 / 7.13 / 7.14 | mailbox source — disable / quarantine / rate-limit |
| 7.10 / 7.11 / 7.12 | 7.15 / 7.16 / 7.17 | service client — disable / quarantine / rate-limit |
| 7.13 / 7.14 / 7.15 | 7.18 / 7.19 / 7.20 | AI actor — disable / quarantine / rate-limit |
| 7.16 / 7.17 / 7.18 | 7.21 / 7.22 / 7.23 | command capability — disable / quarantine / rate-limit |
| 7.19 / 7.20 / 7.21 | 7.24 / 7.25 / 7.26 | outbound channel — disable / quarantine / rate-limit |
| 7.22 | 7.27 | command allowlist v1 & full lifecycle completion (renumber only) |

**Inlined floor (appended to each control story's existing AC):**

- *All actions:* `**And** the operation records actor, scope, subject (<subject>), reason, old state, new state, policy snapshot, and timestamp, with no skip-audit path (FR74, FR75g).`
- *Disable / Quarantine:* `**And** <action> follows the FR75d two-person rule and cannot be performed by service clients or AI actors.`
- *Rate-limit:* `**And** the rate limit is a standard policy mutation bounded by the Tenant Policy Schema and protects unrelated tenants/sources from degradation where isolation is possible (FR75, NFR30).`

**FR Coverage Map edit (line ~510):**

- OLD: `- FR74: Epic 7 — Disable/quarantine/rate-limit sources (15 subject×action sub-stories)`
- NEW: `- FR74: Epic 7 — Disable/quarantine/rate-limit sources (15 subject×action stories, shared control floor inlined per story)`

---

### MAJOR-4 — Split Story 8.2 into three sprint-ready stories (applied)

Story 8.2 carried an explicit planning note to split. Replaced with:

- **8.2 Operational telemetry emission** (FR94, NFR28, NFR34)
- **8.3 SLO publication and error budgets** (NFR42a, NFR24–27/NFR43, A11, NFR38)
- **8.4 Tenant-safe alert wiring** (NFR43, NFR42, NFR2)

Current Story 8.3 ("Degraded-state operability and runbook diagnostics") renumbered to **8.5**. Full bodies in `epics.md`.

---

### MAJOR-3 — Strengthen Stories 9.7–9.13 with negative-path + evidence ACs (applied, in place)

Each story kept its original AC and gained authorization-failure / redaction / audit / partial-failure / retry-terminal / tenant-isolation / evidence-artifact ACs as applicable:

- **9.7** Data-class inventory: non-admin edit fails closed + audited; retention-class change records actor/old/new/snapshot; versioned, quarterly-reviewed, all classes classified.
- **9.8** Export: unauthorized detail excluded/redacted without revealing hidden resource; partial-failure per-class + retryable run ID; audit record per run.
- **9.9** Deletion/erasure: non-authorized fails closed + audited; audit chain never mutated (nightly verify passes); visible retryable/terminal on failure; erasure proof artifact.
- **9.10** Consent metadata: per-project redaction + authorized-only query; change audited; fail-closed when policy requires but metadata absent.
- **9.11** Continuity drill: no cross-tenant leakage/mutation; drill report as A10 evidence; deviation flagged + follow-up on RPO/RTO miss.
- **9.12** Projection rebuild: deterministic-equivalence diff; tenant-scoped no cross-tenant I/O; validation report.
- **9.13** Scoped outage: no-leak/no-mutation/no-loss assertions each produce an evidence artifact; clean recovery from recoverable state; incident scope recorded ≤ 5 min.

---

### MAJOR-5 — Preserve the Epic 1 value anchor (applied)

- **Epic 1 header:** added a binding **Value-anchor invariant** — Story 1.9 is the value proof; foundation stories 1.1–1.8 must each unblock or guard it; an unlinked foundation story is out of scope.
- **Stories 1.1–1.8:** added a one-line **Anchor:** stating how each unblocks/protects Story 1.9.

---

### MINOR-1 — Beneficiary made explicit in "As the system" stories (applied)

Added a one-line **Beneficiary:** under the goal of Stories 2.3, 2.4, 3.14, 4.1, 4.7 (actor framing unchanged).

### MINOR-2 — Story 1.1 optional-split planning note (applied)

Added a **Planning note** under Story 1.1: split into (1.1a) scaffold + root config + EventStore submodule + build-green and (1.1b) Aspire/DAPR topology + CI if estimation exceeds one story; starter-template stays in 1.1a.

### MINOR-3 — Technical-leaning titles (guidance, no structural edit)

Epic identifiers ("Command Spine", "CLI & MCP") kept (stable cross-references; clear value statements). Captured as binding planning guidance instead: story titles drafted in sprint planning must keep the user/operator/security outcome explicit.

### UX-1/2/3 — Cross-cutting acceptance & planning guidance subsection (applied)

Added a **Cross-cutting acceptance & planning guidance** subsection at the end of the Epic List capturing: UX spine-only tables are binding; later surfaces S4/S6/S7/S8–S10/S9 need elaboration before their increments; "ChatBot" naming ≠ native chat surface for M0; outcome-framed titles.

### Frontmatter edit (applied)

`epics.md` frontmatter `storyCount: 100` → `storyCount: 107`; `correctedAt: "2026-05-30"` retained.

## 5. Implementation Handoff

### Scope classification: **Moderate** (backlog reorganization) — completed in this session

Story splits and renumbering within Epics 7 and 8 constitute backlog reorganization; no PRD/architecture change, MVP unchanged, no resequencing. The edits were applied directly to `epics.md` after Jerome's approval.

### Renumbering reference

**Epic 7 (22 → 27):** 7.1–7.5 unchanged · 7.6 → 7.6–7.11 (split into 6) · 7.7–7.21 → 7.12–7.26 (15 kept, floor inlined) · 7.22 → 7.27.
**Epic 8 (3 → 5):** 8.1 unchanged · 8.2 → 8.2–8.4 (split into 3) · 8.3 → 8.5.
**Epic 9:** 9.7–9.13 strengthened in place (no renumber).

### Routing & responsibilities

| Role | Responsibility |
| --- | --- |
| **Product Owner (Jerome)** | Approved this proposal and Option A. Optional: add the PRD positioning note (UX-3). |
| **Developer agent (this session)** | Applied all edits to `epics.md`; verified numbering (107 stories, no gaps/dupes) and content additions. |
| **(Deferred) Sprint planning** | When `bmad-sprint-planning` is first run for Chatbot, generate `sprint-status.yaml` from the corrected epics (107 stories). No existing sprint-status to migrate. |

### Success criteria — verification result

1. ✅ `epics.md` reflects all edits; `storyCount: 107`.
2. ✅ 111/111 FR coverage preserved (FR Coverage Map is epic-level; only the FR74 line text changed).
3. ✅ No story depends on a shared acceptance floor in another story (7.6/7.7–7.21 inheritance removed; floor inlined into 15 control stories).
4. ✅ Stories 9.7–9.13 each carry negative-path + evidence ACs.
5. ✅ Story 8.2 is no longer a multi-concern story; Epic 1's value anchor is explicit (8 anchors + invariant).
6. ✅ The applied pass-1 proposal and the readiness report remain untouched.

**Verification (post-apply):** total story headers = 107; Epic 7 = 7.1–7.27, Epic 8 = 8.1–8.5 (sequential, no duplicates/gaps); 8 `Anchor:` lines; 5 `Beneficiary:` lines; 15 inlined control-floor ACs; 0 occurrences of the old shared-floor heading.
