# Test Automation Summary — Story 9.6 (Correction-driven vector reindexing)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer role:** QA automation (test generation only) · Jerome

**Framework detected:** .NET 10 / xUnit v3 + Shouldly + NetArchTest — the project's existing stack (no JS/Playwright present, none introduced). No UI surface; this is a server-side feature, so "E2E" coverage is API/behavioral/conformance level (in-memory seams exercised through their public contracts).

**Mode:** Gap-fill against an already-implemented feature in `review` status. Audited every acceptance path against the existing tests, found genuine coverage gaps, and auto-applied tests for each. **No source code was changed — tests only.**

## What this run did

Story 9.6 ships seam-first deliverables (the live Hexalith.Memories Redis-Vector/FalkorDB binding is a documented M2 deferral). The dev story already shipped broad coverage. This pass mapped existing tests against AC1–AC2 + the story's own Task checklist and added tests **only** where a real, achievable gap existed.

## Gaps discovered and filled

### A. AC1 / Task 3 — the version-guard authority had no dedicated unit test (**new file**)
`InMemoryVectorReindexLedger` is the single order-tolerant last-writer-wins authority the whole idempotency property rests on, yet it was only exercised *indirectly* through the reindexer. The `<=` boundary, per-class and per-tenant partition independence, and fail-closed-on-unsafe-tenant were unasserted directly — a regression in the ledger's boundary (`<` vs `<=`) or partition keying could slip through.
- **Added** new file `tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStores/InMemoryVectorReindexLedgerTests.cs` (7 tests): fresh advance; equal-version no-op (the `<=` boundary); older no-op; strictly-newer advance; each `DerivedStoreClass` an independent partition; each tenant an independent partition; unsafe tenant id throws (fail-closed).

### B. AC2 — the reindexer's own SLO-breach computation was untested
Reindexer tests used an on-time clock; only the *activity* test stubbed `SloBreached = true`. `InMemoryVectorReindexer` computing `SloBreached` from a clock past the 60-min M2 deadline (NFR17a) was never exercised end-to-end.
- **Added** `AReindexThatCompletesPastTheM2DeadlineReportsSloBreached` — clock at start+61 min ⇒ `SloBreached`, `DeadlineUtc == start+60`, no failure reason (a late-but-complete reindex, not a failure).
- File: `tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStores/VectorReindexerTests.cs`

### C. AC1 — count accuracy only verified for a single affected resource id
`EntriesInvalidated`/`EntriesRebuilt` is an AC requirement, but every test used one resource id; the multi-resource loop scaling counts across all four classes was untested.
- **Added** `MultipleAffectedResourceIdsScaleTheInvalidatedAndRebuiltCountsAcrossEveryClass` — 2 ids × 4 classes ⇒ 8 invalidated + 8 rebuilt, both ids present with the corrected digest.
- File: `VectorReindexerTests.cs`

### E. AC1 — empty affected-resource-id list edge untested
- **Added** `AnEmptyAffectedResourceIdListAdvancesTheGuardButInvalidatesAndRebuildsNothing` — 0/0 counts, `VersionGuardSkipped == false` (the guard still advanced), and a subsequent re-delivery is a no-op.
- File: `VectorReindexerTests.cs`

### D. AC2 — coordinator delay path untested for a vector-reindex *hard failure*
The coordinator was tested for an SLO breach and an M0 store failure, but not for a vector-reindex hard failure (`vector_reindex_failed`) driving the delay and propagating its reason code onto both the alert and the P2 audit envelope.
- **Added** `AVectorReindexHardFailureMarksDelayedWithTheVectorReindexFailedReasonCode` — `failed`/`vector_reindex_failed` ⇒ `DelayMailbox…` command last, single `CorrectionDelayed` alert with that reason code, P2 envelope carrying `correction-propagation-reason:vector_reindex_failed`.
- File: `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/CorrectionPropagationCoordinatorTests.cs`

## Generated tests

| Layer | File | New tests |
|---|---|---|
| Version-guard ledger (AC1) | `InMemoryVectorReindexLedgerTests` (**new file**) | +7 |
| ReindexVectors operation (AC1/AC2) | `VectorReindexerTests` | +3 |
| Coordinator delay path (AC2) | `CorrectionPropagationCoordinatorTests` | +1 |

## Coverage

| Acceptance criterion | Status |
|---|---|
| AC1 — invalidate + rebuild, idempotent, version-guarded, through the `IDerivedStore` seam | Covered (reindexer happy path/idempotent/older-skip/foreign-tenant/fail-closed, `InvalidateAsync` delete seam, **+ direct ledger contract, multi-resource counts, empty list**) |
| AC2 — SLO breach ⇒ `correction-delayed` + owner + next action + P2 audit-then-deliver, fail-closed | Covered (SLO deadlines/boundary, M2 scope, SLO-breach delay audit-before-alert, failed-audit suppression, activity reason-code mapping, **+ reindexer breach computation, hard-failure delay path**) |
| Cross-cutting — no-leak, cross-tenant conformance, internal-to-`.Server` boundary | Covered by the dev story (`DerivedStoreIsolationLeakTests`, `CorrectionVectorReindexCrossTenantIsolationTests`, `DerivedStoreIsolationBoundaryFitnessTests`); re-verified green |

## Test run results (after changes)

| Project | Before | After | Delta |
|---|---|---|---|
| `Hexalith.ChatBot.Server.Tests` | 1323 | **1334** | +11 |
| Story-scoped filter (VectorReindex/SLO/Coordinator/InMemoryDerivedStore/IsolationLeak) | 50 | **61** | +11 |

All run suites: **Passed — 0 failed, 0 skipped.** (Conformance + Architecture suites unmodified by this pass; the dev story records them green.)

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API/behavioral tests generated · [x] E2E covered by existing coordinator/conformance tests (no UI surface — live Memories binding is a documented M2 deferral)
- [x] Tests use standard framework APIs (xUnit v3 / Shouldly) · [x] Happy path · [x] Critical error/negative cases (fail-closed throw, hard-failure delay, unsafe-tenant)
- [x] All generated tests run successfully (1334 total, 0 failed) · [x] Semantic assertions, no hardcoded waits/sleeps (time via injected `ISystemClock`/`FixedClock`) · [x] Clear descriptions · [x] Tests independent (fresh in-memory store/ledger per fact, no order dependency)
- [x] Summary created · [x] Tests saved to appropriate directories · [x] Coverage metrics included

## Next steps

- Run the new tests in CI alongside the existing Epic 9 suites (no new test project — all land in the existing `Hexalith.ChatBot.Server.Tests` csproj).
- When the live Hexalith.Memories Redis-Vector/FalkorDB `IVectorReindexer` binding lands (deferred M2), add a Conformance-tier test against the real `IndexSchemaDefinitions` partition and an async/long-running reindex + periodic SLO-sweep test (both out of scope here per the inert-control-floor deferral).
