# Test Automation Summary — Story 9.9 (Deletion and Erasure Workflow)

**Date:** 2026-06-03
**Engineer:** QA automation (BMAD `qa-generate-e2e-tests`)
**Framework:** xUnit + Shouldly (.NET 10) — the project's existing test stack. No new framework introduced.
**Feature under test:** Story 9.9 governed deletion/erasure decision/recording/proof layer
(`SubmitDeletionErasureRequest` → `DeletionErasurePlanner` → `DeletionErasureSchema` → audit-chain erasure via the
Story 9.1 `AuditRedactionService` seam).

> Scope note: this is a .NET contracts/server/conformance solution with no browser UI surface (the live deletion/erasure
> UI is an explicit Story 9.9 deferral). "E2E" here = API/behavioral coverage of the governed command spine + the pure
> decision engine + the audit-chain erasure wiring, exercised through the same harnesses Stories 9.1/9.7/9.8 use.

## Coverage assessment (against the 5 ACs)

The feature shipped with a substantial, well-structured suite already (TenantExport-mirrored). The QA pass diffed
existing coverage against every AC, planner branch, schema invariant, and closed-set member to surface untested paths.

| AC | Theme | Pre-existing coverage | Gap found |
|----|-------|----------------------|-----------|
| AC1 | behavior→action, correlation-stamp, no chain mutation | key-shred⇒crypto-shredded, projection-tombstone⇒tombstoned, retain-immutable⇒retained, proof seal | **`hard-delete`⇒`hard-deleted` untested** (no seed class is hard-delete); **unclassifiable-class fail-closed untested** |
| AC2 | fail-closed authority + NFR2 no-leak | gateway role/actor-type denial, planner unauthorized/no-scope/authorized/any-unauthorized, no-leak serialization | none |
| AC3 | erasure never mutates chain; verifier still passes | runner appends-shreds-tombstones + `WormAuditChainVerifier` verifies | none |
| AC4 | per-class success/failure, no silent partial, idempotency | partial-failure proof scoping, retry-taxonomy classification, completeness/duplicate/proof invariants | **`failed` run status (all-failed) untested**; **planner determinism/idempotency untested** |
| AC5 | proof artifact (tombstone + key-shred) | proof entry population (runner), proof seals succeeded-destructive only | none |

## Gaps auto-applied (4 new tests)

All added to `tests/Hexalith.ChatBot.Contracts.Tests/DeletionErasureContractTests.cs` (pure planner/schema branches —
exercised with synthetic `DataClassInventory` instances, since the seed catalog classifies all 13 canonical classes and
cannot reach these branches):

- [x] `PlanShouldResolveHardDeleteBehaviorToHardDeletedAndSealNoProofEntry` — AC1: closed-set member `hard-delete`
      resolves to `hard-deleted`; the dedicated planner + `DeletionErasureSchema` behavior-vs-action branch; the subtle
      invariant that a hard-deleted class is actionable+`succeeded` yet seals **no** proof entry (proof = crypto-shred /
      tombstone confirmations only).
- [x] `PlanShouldFailClosedToRetainedForAClassMissingFromTheInventory` — AC1 fail-closed: a requested canonical class
      absent from the inventory defaults to `retain-immutable` ⇒ `retained`/`worm-retained`, never destroyed (the
      Completion-Notes "unclassifiable class fails closed to retained" invariant, previously unasserted).
- [x] `ValidateRunResultShouldAcceptAFullyFailedRunAndRejectMislabeling` — AC4: the all-actionable-classes-failed
      ⇒ `failed` run-status branch; failed class carries no proof entry; mislabeling as `completed` is rejected
      (`deletion_run_status_inconsistent`).
- [x] `PlanShouldBeDeterministicForIdempotentRetries` — AC4 / Story 1.5 idempotency floor: a same-run-id retry over
      identical inputs yields a structurally identical run + identical proof fingerprint (no duplicate-destruction signal).

## Test execution

| Suite | Before | After | Result |
|-------|--------|-------|--------|
| `Hexalith.ChatBot.Contracts.Tests` | 415 | **419** | ✅ Passed (0 failed, 0 skipped) |
| `Hexalith.ChatBot.Server.Tests` | 1361 | 1361 | unchanged by this pass (not re-run; no source touched) |
| `Hexalith.ChatBot.Conformance.Tests` | 80 | 80 | unchanged by this pass |
| `Hexalith.ChatBot.Architecture.Tests` | 39 | 39 | unchanged by this pass |

Deletion-focused slice: `--filter FullyQualifiedName~DeletionErasure` → **30 passed** (was 26).
Full `Contracts.Tests` suite → **419 passed, 0 failed**.

> Only the `Contracts.Tests` file was modified (4 added tests), so the other three suites are unchanged from the story's
> green run and were not re-executed in this QA pass.

## Next steps

- Run all four suites in CI to confirm the full baseline (1895 + 4 = 1899) stays green.
- When the deferred non-audit-store destruction runtime and the live S-tagged deletion/erasure UI land, add:
  - server-side `failed-retryable`/`failed-terminal` runtime tests driving real per-store failures through the runner;
  - browser E2E (Playwright) for the deletion/erasure admin surface and proof-query view.
