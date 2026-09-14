---
title: Qualification Evidence Reconciliation — Hexalith.ChatBot Architecture Spine
date: '2026-09-14'
reviewedArtifact: ../ARCHITECTURE-SPINE.md
authority: ../../../prds/prd-Hexalith.ChatBot-2026-05-28/qualification-evidence.md
reviewStage: post-remediation
result: pass
blockingFindings: 0
advisoryFindings: 0
---

# Qualification Evidence Reconciliation

## Post-remediation verdict

This review compares only the current `ARCHITECTURE-SPINE.md` (445 lines) with `qualification-evidence.md`
(70 lines). Both were read completely. Citations use `Spine:Lx` and `Evidence:Lx` for those reviewed versions.

**PASS — zero blocking findings and zero advisory findings.** All earlier B-1–B-4 and A-1–A-3 findings are
remediated. Exact current gate labels, candidate/provenance/freshness predicates, independent approval boundaries, and
unsupported/provisional semantics align. No new conflict or readiness inference was found.

A5, A6, A10, A11, and A13 remain open. This reconciliation verifies architecture-to-evidence consistency; it is not
new evidence and cannot close a gate.

## Current gate-state alignment

| Gate | Qualification evidence | Current spine | Result |
| --- | --- | --- | --- |
| A5 | `open`; no approved provider bundle and live model invocation disabled (`Evidence:L14`). | `OPEN`; no qualified contract/negative evidence across region, retention, telemetry, training/reuse, redaction, tenant binding, and disable behavior; live AI and M0/M1 onboarding remain blocked (`Spine:L430`). | **Aligned; remains open.** |
| A6 | `open`; data-class decision and production KMS, Parties, EventStore protection/erasure, backup/restore, export/delete, and surviving-metadata evidence absent (`Evidence:L15`). | `OPEN`; the same data-class, KMS/custody, Parties/EventStore, protection/erasure, legal-hold, backup/restore, export/delete, surviving-metadata, and independently witnessed runtime evidence remain absent and blocking (`Spine:L431`). | **Aligned; remains open.** |
| A13 | `open`; Conversations execution/lifetime concurrency/audit plus owner mappings and EventStore path/fencing remain unaccepted; M0 blocked (`Evidence:L16`). | `OPEN`; the indivisible owner bundle remains unaccepted, and append/assignment, mappings, atomic audit, fencing, live AI/onboarding, M0, and tamper-evidence claims remain blocked (`Spine:L432`). | **Aligned; remains open.** |
| A10 | `provisional`; no fresh hosted four-job bundle; recovery architecture remains `activation: pending`; story/diagnostic artifacts have no A10 authority (`Evidence:L17-L18`). | `OPEN / provisional`; no qualifying fresh exact-candidate hosted four-job controlled-loss/full-window evidence and no M2 production/release-candidate claim (`Spine:L433`). AD-11 preserves activation/channel separation (`Spine:L235-L254`). | **Aligned; remains provisional.** |
| A11 | Calibration incomplete; deficient rows are `unsupported` (`Evidence:L19`). Every current row is `unsupported` with exact candidate `not-selected` (`Evidence:L25-L41`). | `OPEN / unsupported`; every current row remains `unsupported`, candidate `not-selected`, and the whole M2 gate plus narrower associated claims remain blocked (`Spine:L276-L283`, `Spine:L434`). | **Aligned; remains unsupported.** |

The release section identifies itself as an open-assumption ledger rather than the complete release gate and records
the qualification artifact's current `status: evidence-gap` (`Spine:L422-L426`; `Evidence:L1-L5`).

## Candidate, provenance, and freshness predicates

### A5, A6, and A13

- AD-12 requires exact candidate revision, contract/package versions, storage/provider profile, responsible producer,
  test runner, time, result, expiry/reopen rule, and independent verification; repository/interface presence is
  non-qualifying (`Spine:L265-L267`; `Evidence:L70`).
- A5 preserves Security + Architecture approval and the full provider contract/negative-evidence categories
  (`Spine:L267`, `Spine:L430`; `Evidence:L66`).
- A6 preserves Compliance/Data Protection + Architecture approval, Parties/EventStore runtime evidence, and independent
  witnessing (`Spine:L267-L268`, `Spine:L431`; `Evidence:L67`).
- A13 is an indivisible exact-candidate owner bundle covering executable append/assignment, lifetime concurrency,
  closed authority mapping, atomic transaction ownership, actor-only ACL/ETag first-write fencing, and concurrent/fork/
  reorder/rebuild/recovery tests; named owners and Security approval remain binding (`Spine:L268-L275`;
  `Evidence:L68`).
- The mutation path is explicitly an A13-gated target, not current support (`Spine:L49`, `Spine:L84-L97`), matching
  the evidence artifact's unaccepted supported-write-path/fencing posture (`Evidence:L16`).

### A10

- The qualifying operational bundle must be fresh, hosted, four-job, controlled-loss, and bound to the exact M2
  candidate, evidence-policy version, run locator, producer, timestamps, freshness calculation, persisted-loss bounds,
  RTO duration, cleanup, independent validation, and stable failure reason (`Spine:L246-L251`; `Evidence:L56-L58`).
- Freshness follows the evidence policy, currently eight days (`Spine:L251-L252`). The historical 2026-08-27 bundle is
  correctly rejected as expired on 2026-09-04, pre-controlled-loss, without controlled-loss RPO evidence, and limited
  to 180 seconds (`Spine:L252-L254`; `Evidence:L43-L54`).
- Story completion, transition diagnostics, and pre-activation evidence cannot gain A10 authority
  (`Spine:L235-L245`, `Spine:L253-L254`; `Evidence:L18`, `Evidence:L58`).
- A10 still requires an RTO-capable full-window or separately retained production-shaped drill
  (`Spine:L248-L251`; `Evidence:L18`).

### A11

- The operating-baseline target catalog and qualification table pair one-to-one by stable metric name
  (`Spine:L276`; `Evidence:L21-L23`).
- `supported` requires numeric target/unit, window, error budget, alert threshold, timestamped calibration source,
  tenant scope, live signal/provenance, accountable route/receiver, burn-test result/date/immutable locator, and exact
  M2 candidate (`Spine:L276-L279`; `Evidence:L23`).
- Any missing, stale, failing, unverifiable, or candidate-mismatched value deterministically derives `unsupported`
  (`Spine:L279-L280`; `Evidence:L23`). Current rows remain `unsupported` with candidate `not-selected`
  (`Spine:L282-L283`; `Evidence:L25-L41`).

## No readiness inference

- AD-12 makes these assumptions necessary but not the complete release gate, retains strict M0 → M1 → M2 order, and
  says closing one never compensates for another failure (`Spine:L256-L264`).
- A5/A6/A13 block M0/M1; M2 revalidates them and additionally requires A10/A11 (`Spine:L260`).
- All five current rows remain visibly open and name the blocked claim (`Spine:L428-L434`).
- AD-10 calls canonical audit `100%` a target, not current qualification, and prohibits completeness/tamper-evidence
  claims while A13 remains open (`Spine:L230-L233`).
- The topology is labeled an M2 `production-shaped candidate`, not production (`Spine:L394-L403`).
- The source artifact remains `status: evidence-gap` and holds mutable revision-specific evidence
  (`Evidence:L1-L10`); the spine mirrors that status (`Spine:L424-L426`).

## Concise remediation history

The following findings were recorded against the earlier 240-line draft. They are retained as history and are no
longer active.

| Initial finding | Disposition in current candidate |
| --- | --- |
| **B-1:** A13 EventStore path called supported despite unaccepted path/fencing. | **Resolved.** Diagram and AD-2 label it an A13-gated target and enumerate current unaccepted support (`Spine:L49`, `Spine:L84-L97`). |
| **B-2:** A10 lacked exact-candidate/full provenance and eight-day freshness binding. | **Resolved.** AD-11 carries the complete bundle tuple, current freshness policy, historical invalidity, and separate RTO drill (`Spine:L235-L254`). |
| **B-3:** A11 omitted burn-test result/date/locator and one-to-one metric binding. | **Resolved.** AD-12 includes the complete support tuple and deterministic unsupported rule (`Spine:L276-L283`). |
| **B-4:** A5/A6/A13 lacked the common exact-candidate/provenance closure tuple. | **Resolved.** AD-12 binds the full tuple, non-qualifying evidence, named approvers, and indivisible A13 bundle (`Spine:L265-L275`). |
| **A-1:** Current `evidence-gap` status not surfaced. | **Resolved.** Release ledger records it explicitly (`Spine:L424-L426`). |
| **A-2:** A5 evidence categories incomplete. | **Resolved.** The A5 row enumerates the missing provider/negative-evidence set and disabled effects (`Spine:L430`). |
| **A-3:** A6 omitted Parties integration and independent witnessing. | **Resolved.** AD-12 and the A6 row name both (`Spine:L267-L268`, `Spine:L431`). |

## Final disposition

**Accepted for the qualification-evidence reconciliation gate.** Remaining blockers: none. Remaining advisories:
none. No new issue exists. A5, A6, A10, A11, and A13 remain open exactly as recorded; this reconciliation must not be
cited as gate-closing evidence.
