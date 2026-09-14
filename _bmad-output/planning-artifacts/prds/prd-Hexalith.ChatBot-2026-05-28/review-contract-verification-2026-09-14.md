---
title: "Targeted PRD contract verification"
status: complete
created: "2026-09-14"
reviewer: "targeted-contract-verifier"
verdict: fail
counts:
  critical: 0
  high: 3
  medium: 1
  low: 0
  checks_passed: 9
  checks_partial: 1
  checks_failed: 3
---

# Targeted Contract Verification — 2026-09-14

## Verdict

**FAIL — 0 Critical, 3 High, 1 Medium. Nine of thirteen requested closures pass, one is partial, and three fail.**

The updated artifacts close most of the prior safety-contract defects: the two-command deny-by-default AI allowlist, atomic audit durability, first-store isolation, M0 authenticity, separate classifiers and canonical `NeedsReview` mapping, stable idempotency, M1 governed chat/WCAG, Conversations ownership, and recovery evidence boundary are materially coherent. Re-finalization remains blocked by an internally non-executable lifecycle, a source manifest that no longer matches the checked-out dependency baseline, and a phase-gate contradiction that places the A6 pre-pilot decision only in M2 evidence.

## Scope and Method

Reviewed as one contract set:

- `prd.md`
- `addendum.md`
- `source-manifest.md`
- `qualification-evidence.md`
- `.memlog.md`

The source-lineage check additionally compared declared SHA-256 values and repository/submodule revisions with the current checkout. No canonical artifact was edited.

## Closure Matrix

| Requested closure | Result | Evidence and verification |
| --- | --- | --- |
| Non-downgradable approval | **PARTIAL** | PRD FR41/FR52, Glossary, SM-C3, NFR46, addendum Risk Classifier and Tenant Policy Schema all state that the six boundary-crossing effects cannot be downgraded. One undefined `allowed override path` remains in addendum line 47; see M1. |
| Exact two-command AI allowlist | **PASS** | Addendum lines 62–73 is deny-by-default and names exactly `Project.AppendConversationMessage` and `ChatBot.ExecuteLowRiskAssistance`; PRD A8 and memlog line 23 repeat the exact membership and change controls. The broader operation catalog is explicitly not the AI allowlist. |
| Atomic audit durability | **PASS** | Addendum lines 109–111 and PRD FR81a/NFR15a atomically commit the domain event, durable idempotency record, policy/approval references, and canonical audit envelope. Projection is explicitly rebuildable post-commit state; `AuditUnavailable` writes no domain/idempotency state. |
| First-store isolation | **PASS** | PRD Data Governance Surface line 551, M0/M1/M2 gate bullets, FR55a, NFR9a, NFR59, NFR65, and memlog line 25 all require native-store/API isolation proof in the first increment that introduces a store, before pilot/machine-surface/multi-tenant exposure. |
| M0 authenticity | **PASS** | M0 scope line 261, FR48a–FR48d, addendum lines 149–156, and the policy-schema `mailbox.authenticity-strictness` row require verdict passthrough, header discrepancy capture, delegated sender evidence, external-sender posture, `strict` default, and `NeedsReview`/block behavior. MVP has no permissive mode. |
| Separated classifiers and canonical `NeedsReview` mapping | **PASS** | Addendum defines independently versioned `AssociationScorer`, `TaskIntentDetector`, and categorical `ActionRiskClassifier`; PRD FR26/FR35/FR39 and A9a preserve separate outputs and qualification partitions. Every non-qualifying scorer outcome maps to `NeedsReview`; `Deferred`/`Rejected` require explicit human decisions. No legacy shared-kernel or alternate below-`T_low` disposition remains. |
| Stable idempotency | **PASS** | Addendum lines 120–131, PRD FR90, NFR13/NFR13a, operation contracts, lifecycle matrix, and memlog line 29 use lifetime-stable `operation_id`, authoritative `decision_slot_id`, expected revisions, typed conflict outcomes, and bounded hashes only for non-mutating proposal suppression. No legacy 60-second mutation key remains. |
| Executable lifecycle | **FAIL** | A detailed matrix now exists, but its graph, command catalog, and rows do not round-trip. See H1. |
| M1 governed chat and WCAG | **PASS** | M1 scope lines 287/291, Journey 1, Vision, surface S1a, FR28a–FR28f, command catalog, traceability, NFR60, and memlog line 26 consistently place the FrontComposer/CommandGateway governed composer in M1 with admission, attribution, streaming, stop/cancel, stable retry, typed failure, mandatory proposal conversion, audit, and WCAG 2.2 AA. |
| Conversations ownership | **PASS** | Canonical Context Ownership assigns conversation identity/messages/append/history to Hexalith.Conversations, Projects only references conversation IDs within Project boundaries, EventStore owns durable command/event/audit persistence, and the manifest pins Conversations. The legacy command prefix is explicitly identified and governed by the material-change protocol. |
| Recovery evidence boundary, NFR54a, and NFR65a | **PASS** | Addendum lines 214–232 separates current-run completion from retained operational/A10 evidence; PRD NFR54a and NFR65a enforce metadata-only publication, exact-candidate/current-run/single-producer binding, independent validation, and fail-closed stages. PRD A10 and `qualification-evidence.md` correctly say no fresh qualifying hosted four-job bundle exists. |
| Source lineage | **FAIL** | All declared direct-input hashes match, and the two pinned source commits contain their paths. However, the manifest's workspace/dependency baseline no longer matches the checkout committed with the update. See H2. |
| A6/A10/A11 ownership | **FAIL** | The named owners and open evidence states are individually explicit, and A10/A11 are correctly stop-ship when unsupported. A6's owner is correct, but its pre-pilot timing conflicts with the authoritative increment gate. See H3. |

## High Findings

### H1 — The lifecycle matrix still cannot serve as one executable contract

**Location:** `prd.md` lines 454–494, 781–818, and 839–855.

**Evidence:**

- The summary graph says `Received -> Proposed -> ...`, while the authoritative matrix sends `Received` directly to `Associated` or `NeedsReview`; no matrix row has `Proposed` as a destination.
- Matrix commands `SkipEmailAssociation` and `ResumeEmailAssociationReview` do not exist in the canonical command catalog.
- Catalog commands `AssociateEmailToProject`, `MarkEmailAssociationNeedsReview`, `ReprocessEmailAssociation`, and `QuarantineEmailAssociation` have no lifecycle row even though they act on this workflow.
- The `Correction-delayed` definition says it returns to `Corrected`, but the matrix has no source row, command/actor, guard, or audit event for that transition.
- The combined concurrency/audit/successor column does not consistently name an audit event for every row, so an implementation cannot mechanically derive the complete transition contract.

**Impact:** UI, CLI, MCP, persistence, and tests can implement different reachable states and command names while claiming conformance. The `Proposed` state is unreachable under the authoritative matrix, and terminal reprocessing/quarantine/resume behavior is not tied to the catalog.

**Required closure:** Make the authoritative matrix and command catalog bijective for lifecycle-changing commands. Choose whether proposal generation persists `Proposed` or directly resolves to `Associated`/`NeedsReview`, then remove the competing graph. Add explicit rows for skip, resume, reprocess, quarantine, and `Correction-delayed -> Corrected`, with canonical command ID, actor, guard, destination, terminal/successor rule, expected revision, and audit event.

### H2 — Source lineage is reproducible for the old base but stale for the current checkout

**Location:** `source-manifest.md` lines 6, 11, 25–39; `prd.md` lines 24–34 and 101–105.

**Evidence:**

- Direct-input hashes for the product brief, architecture spine, orientation extract, two reviews, and validation report all match the manifest.
- The manifest records workspace revision `f0ba70ed9c76a88d0204e1d228562b476742c1b5`; current root HEAD is `76f355a038c4abdb3b9fdb3fb836c25053a18fb0`.
- The manifest says Hexalith.Parties is `bfc15cc15b6556c5f97ae3d06bdefdd809d3b666`; the current checked-out/gitlink revision is `14d249fde316b0002aec84351d7a7cdf953d1d30`.
- The manifest says Hexalith.EventStore is `4502913cafbb6151a17544923b5b1ba76ea5e5ec`; the current checked-out/gitlink revision is `7579b858ecd30f0273bec3ab3e8c88f93232f0a5`.
- The current EventStore range adds/changes public payload-protection and erasure contracts, making the drift material to A6 and the PRD's canonical audit/data-protection boundary, not merely editorial dependency churn.

**Impact:** The PRD calls `source-manifest.md` the refreshed repository baseline and says material changes trigger a five-business-day re-check, but the same committed update moved two pinned dependencies beyond the recorded baseline. A reviewer cannot tell whether the canonical contract was checked against the current EventStore security surface.

**Required closure:** Either refresh `workspaceRevision` and the Parties/EventStore rows to the current checkout and perform the material-change re-check, or explicitly model both `reviewedBaseRevision` and `currentRevision` plus a visible pending-recheck state. Record the re-check outcome in `.memlog.md`; do not describe the old values as the current checked-out baseline.

### H3 — A6 is a pre-pilot blocker but appears only in the M2 gate

**Location:** `prd.md` gate table lines 250–254, A6 line 1393, Compliance Requirements line 934, and NFR49a line 1496; `.memlog.md` line 36.

**Evidence:** A6, NFR49a, the compliance section, and memlog all say the Compliance/Data Protection + Architecture data-class decision must resolve before pilot onboarding and before any GDPR-satisfaction claim. The authoritative increment table does not list A6 in M0 or M1 mandatory evidence; it lists `A6 privacy decision` only in M2. M0 is nevertheless a controlled pilot preview and M1 an approved pilot.

**Impact:** A reader following the gate table can admit pilot users before the requirement that explicitly blocks pilot onboarding is satisfied. This is a phase-boundary contradiction even though the owner itself is named correctly.

**Required closure:** Put the A6 data-class approval and its Compliance/Data Protection + Architecture ownership into the M0 pre-pilot gate (and inherit/revalidate it in later gates). Include the absence or invalidation of that approval in the M0/M1 disable condition. Keep A10 in M2 and A11 split across its stated baseline/increment checks.

## Medium Finding

### M1 — An undefined approval “override path” weakens otherwise exact non-downgradability

**Location:** `addendum.md` line 47.

**Evidence:** The reviewer-disagreement bullet refers to approving an `approval-required` action “outside the allowed override path,” but no override path is defined. Everywhere else, the six boundary-crossing effects are structurally non-downgradable; normal authorized approval is execution authorization, not a classifier override.

**Impact:** A downstream design may invent a reclassification or bypass path that the rest of the contract forbids.

**Required closure:** Replace the phrase with the intended case. If it means normal approval, say so and do not call it disagreement. If it means reclassification, state that no such path exists for the six mandatory effect classes; only product-versioned read-only/no-external-effect subtype changes can alter low-risk eligibility.

## Ownership and Open-Evidence Audit

| Item | Owner contract | Gate state | Verification |
| --- | --- | --- | --- |
| A6 | Compliance / Data Protection + Architecture | Open pre-pilot blocker | Owner and required data-class decision are exact; gate placement is inconsistent (H3). |
| A10 | Architecture / DevOps; qualification artifact further assigns hosted runs to DevOps, independent validation to Test Architect, candidate/policy binding to System Architect, and release claim to Product Lead | Open M2 stop-ship | Correctly provisional; no fresh qualifying hosted evidence is claimed. |
| A11 | Product Lead; M2 gate additionally involves System Architect, DevOps, and Test Architect for technical proof/approval | Open calibration gate | Starter metrics and unsupported SLO rows are explicit; unsupported rows block the associated production claim. |

## Gate Recommendation

Do not restore `status: final` yet. Resolve H1–H3 and re-run this targeted verifier. M1 should be removed in the same edit because it is small but directly touches the product's central approval invariant. A10/A11 evidence gaps may remain open only with their existing explicit stop-ship semantics; they are not documentation defects by themselves.
