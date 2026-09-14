# Architecture Spine Finalize Review — Good-Spine Rubric

## Final closure rerun — 2026-09-14

This review covers the latest `ARCHITECTURE-SPINE.md` after protected-main target scoping and removal of GitHub concurrency. It **supersedes every earlier 2026-09-14 verdict and finding in this file**.

## Scope and method

- Re-read the latest spine and authoritative adjacent memlog.
- Rechecked Story 12.15, TE-2, the sprint status, policy, workflow, developer guide, companion architecture/ADR, deferred ledger, and the prior validation findings.
- Reapplied the good-spine tests for epic-level divergence, enforceability, Deferred safety, brownfield truth versus pending activation, technology currency, source reconciliation, operations/environment, and ownership.
- Reran `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24`: zero findings.

## Final verdict

**PASS — no Critical or High findings remain.** The spine now supplies a coherent epic-level consistency contract and truthfully keeps implementation and external enforcement under `activation: pending`. Three non-blocking editorial reconciliations remain suitable for direct finalize-time cleanup.

## Critical and High findings

None.

## Closure of the previous blockers

### Protected-candidate identity — closed

AD-6 now reserves `story-transition-evidence-integrity (pull_request)` exclusively for pull requests whose base ref is protected `main`; every other PR base and event receives a globally distinct, non-authoritative identity. A same-head PR against another base can no longer emit the required protected-main check. No-transition is an executed planner verdict rather than a skipped-success check, direct protected-main pushes are disallowed, and merge-queue identity remains safely excluded until separately designed.

### Required-run concurrency — closed

AD-9 now prohibits a GitHub concurrency group on the required job and records the GitHub pending-run cancellation reason. Fresh GitHub-hosted runners, job-local ephemeral topology/state, generated per-run secrets, Testing guards, production exclusion, and workflow-run-bound resource/observation identities make parallel jobs independent. Shared/self-hosted infrastructure requires a new decision and a non-canceling external lease.

### Cleanup and attestation integrity — closed

AD-7 now fixes one observation schema, exact current-run identity fields, policy keys, terminal outcomes and postconditions, exact-set receipt completeness, the sole TRX codec, exact root attributes, digest grammar, sanitizer/validator reuse, receipt reload, and exact-byte hash recomputation. A structurally present or stale observation set cannot produce an authoritative complete receipt.

### Artifact authority and closeout — closed

AD-3/AD-4 keep scheduled/release A10 evidence, transition diagnostics, and TE-2 completion authority disjoint. AD-5 gives teardown/finalization and both uploads separate absolute floors with always-run behavior, while raw TRX and internal cleanup state remain outside the authoritative evidence surface.

## Non-blocking finalize-time tail

### FC-001 — Medium — Separate reports from observations in the diagram

The diagram still draws `O[Closed cleanup observations] -->|allow-listed reports| G`, although AD-4 permits reports—not internal observations—in `recovery-primary-diagnostics`. Add a distinct scenario-report node feeding `G` and terminate `O` only at restoration/finalization. Either use the canonical receipt path in both artifacts or label the diagnostic receipt as a byte-identical copy of the canonical exact bytes.

**Disposition:** autofix.

### FC-002 — Medium — Deny no-transition completion authority explicitly

AD-6 defines a successful explicit no-transition verdict, while AD-4 says only that a passing report has completion authority. Narrow the latter to a passing `completion-transition` report for the exact protected-main base/head candidate; no-transition, diagnostic, rejection, skipped, and neutral outcomes have no completion authority.

**Disposition:** autofix.

### FC-003 — Low — Reconcile two stale summaries

The activation precondition names `330/335/350/360` but AD-5 binds `330/335/350/355/360`. The capability map also says destructive-run isolation lives in a “concurrency group” although AD-9 now prohibits one. Carry minute 355 into the activation check and replace that map entry with the absence of a GitHub concurrency group/cross-run isolation policy.

**Disposition:** autofix.

## Good-spine checklist

| Dimension | Result | Assessment |
| --- | --- | --- |
| Epic-level divergence coverage | **Pass** | Current-run production, artifact authority, completion identity, cleanup truth, versions, time budgets, and destructive-run isolation are fixed. |
| Rule enforceability | **Pass** | Every AD has executable predicates or an explicit activation verification seam; the earlier identity and concurrency counterexamples are closed. |
| Deferred safety | **Pass** | Retained completion, merge queues, hosted-duration calibration, long-lived artifact identity, DW-124, and A10 authenticity are bounded with revisit conditions. |
| Named technology currency | **Pass** | The dated .NET, Aspire, Dapr, and artifact-action authorities remain current and immutably identified where executable code crosses the trust boundary. |
| Brownfield truth | **Pass** | Current code/docs/external rules remain visibly non-conformant and are listed as activation work; `status: final` is not confused with active enforcement. |
| Source reconciliation | **Pass with editorial tail** | Story 12.15 safety, TE-2 status/ownership, A10 separation, and all prior validation decisions land; FC-001/FC-002 are wording/diagram reconciliation only. |
| Operational/environmental envelope | **Pass** | Runner, topology, tenant, secrets, production exclusion, parallelism, deadlines, retention, and branch enforcement are decided or explicitly deferred. |
| Ownership | **Pass** | Amelia, Murat, Winston, and TE-2 as the durable activation record are explicit. |
| Inherited parent spine | **N/A** | No parent architecture spine with inherited AD identifiers was supplied. |

## Final gate disposition

The semantic rubric gate passes with no Critical or High findings. Apply FC-001 through FC-003 as clear finalize-time corrections, rerun deterministic lint, and retain `activation: pending` until repository implementation, companion documents, hosted verification, TE-2's activation record, and protected-main enforcement match the spine.
