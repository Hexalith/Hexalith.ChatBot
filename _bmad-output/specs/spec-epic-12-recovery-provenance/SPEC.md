---
id: SPEC-epic-12-recovery-provenance
companions:
  - provenance-contract.md
  - ../../planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md
sources:
  - ../../implementation-artifacts/12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10.md
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract for what to build, test, and validate. Source documents listed in frontmatter are for traceability; consult them only for narrative rationale this contract intentionally omits.

# Epic 12 Recovery Provenance

## Why

Story 12.15 has live recovery exercises, but story completion still lacks an active, non-circular proof that binds the exact pull-request candidate, successful destructive-run cleanup, sanitized results, attestation, and independent validation. Operations, reviewers, and release governance need that proof without confusing it with diagnostics or the separately governed operational evidence used to assess A10.

## Capabilities

- **CAP-1**
  - **intent:** Operations can execute the mandatory continuity, projection-rebuild, and scoped-outage scenarios through real sandbox boundaries.
  - **success:** Both continuity scenarios, every configured non-empty rebuild dataset, and all six scoped dependencies produce their existing fail-closed verdicts with non-zero coverage, restoration, tenant isolation, and explicit production-equivalence residuals.
- **CAP-2**
  - **intent:** An exact protected-main pull-request candidate can produce one authoritative current-run recovery completion result.
  - **success:** Planning authorizes exactly one canonical `recovery-primary` consumer, and any planning, production, timeout/no-test, restoration, cleanup, projection, attestation, validation, or publication failure keeps the transition check red.
- **CAP-3**
  - **intent:** Reviewers can prove that destructive recovery left no run-owned mutation, injected fault, or ephemeral topology state behind.
  - **success:** The sole cleanup finalizer reconciles exactly one positive, identity-matching observation for every expected scenario/mutation class and infrastructure teardown item; only a `complete` receipt can enter the canonical result and attestation chain.
- **CAP-4**
  - **intent:** Reviewers can distinguish diagnostic evidence, story-completion authority, and operational A10 evidence.
  - **success:** Closed, disjoint artifact channels prevent cross-use, raw producer TRX is never uploaded, and only a passing completion-transition report for the exact protected-main candidate carries story-completion authority.
- **CAP-5**
  - **intent:** Recovery completion runs inside a bounded, supported, and isolated execution envelope.
  - **success:** Repository version authorities, immutable artifact-action references, producer and absolute closeout deadlines, fresh hosted runners, `Testing` guards, generated secrets, production exclusion, and absence of a GitHub concurrency group are mechanically verified.
- **CAP-6**
  - **intent:** Governance can activate, reject, or defer the control without overstating completion or A10 evidence.
  - **success:** TE-2 records every activation prerequisite before Story 12.15 or TE-2 reaches `done`; A10 remains provisional until separate fresh qualifying operational evidence exists, and every deferred decision retains its revisit condition.

## Constraints

- **AD-1:** `recovery-primary` is current-run completion evidence only, with the exact canonical TRX, provenance path, locator, and at-most-one-consumer rule defined in `provenance-contract.md`.
- **AD-2:** Repository-owned planning must authorize the exact base/head and full production order before destructive setup; every phase fails closed.
- **AD-3:** Scheduled/release recovery evidence serves only the independent operational recovery/A10 gate and cannot satisfy `recovery-primary`.
- **AD-4:** Diagnostic and completion authority use separate closed, metadata-only, 30-day artifact channels; the raw producer TRX is outside every upload path.
- **AD-5:** Preserve producer-relative ordering `250 < 265 < 285` and absolute ordering `315 < 330 < 335 < 350 < 355 < 360`; no phase may borrow another phase's reserve.
- **AD-6:** Only the always-run `story-transition-evidence-integrity (pull_request)` check for pull requests targeting protected `main` may gain completion authority, and it remains inactive until external enforcement is recorded.
- **AD-7:** Cleanup observations, aggregate receipt, canonical TRX binding, exact-byte digest, and validator recheck use the versioned schemas and sole codec defined in `provenance-contract.md`.
- **AD-8:** `global.json`, the AppHost SDK/aligned package catalog, the approved Dapr CLI/runtime pair, and reviewed full artifact-action SHAs are the executable version authorities.
- **AD-9:** Destructive recovery runs only on fresh GitHub-hosted, job-local ephemeral infrastructure with generated per-run secrets, `Testing` safeguards, no production endpoints or credentials, and no GitHub concurrency group.
- Existing Story 12.15 live-driver scenario coverage, report/verdict semantics, fault-authority boundary, A10-only RPO/RTO scope, fail-closed behavior, and explicit topology residuals remain binding unless an AD above narrows their evidence authority.
- The architecture spine is authoritative and remains `activation: pending`; `status: final` settles design decisions but does not assert repository or GitHub enforcement.

## Non-goals

- Activating or changing CI workflows, repository rules, branch protection, dependencies, application code, or the architecture spine in this specification update.
- Using completion diagnostics or current-run completion artifacts to ratify A10, or using scheduled/release evidence to satisfy story completion.
- Adding retained-source completion, merge-queue enforcement, shared/self-hosted destructive infrastructure, or a second recovery consumer without a new architecture decision.
- Claiming production Graph, AKS, multi-replica, durable-storage, hosted-duration, or four-hour RTO equivalence that the named environment did not exercise.
- Automatically changing `RecoveryTargets`, evaluator thresholds, or fail-safe verdict semantics to make evidence pass.

## Success signal

An exact protected-main pull-request transition can run every required recovery exercise, prove complete cleanup, publish disjoint diagnostic and authoritative artifacts inside the fixed deadline envelope, and pass independent provenance validation; TE-2 then records the verified external activation facts. Until that demonstration exists, Story 12.15 and TE-2 remain in review and A10 remains separately provisional.
