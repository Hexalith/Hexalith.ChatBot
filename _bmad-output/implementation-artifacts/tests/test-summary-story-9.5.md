# Test Automation Summary — Story 9.5 (Derived-store cross-tenant isolation)

**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-03
**Framework:** .NET 10 / xUnit v3 + Shouldly (project's existing stack — no new framework introduced)
**Mode:** Gap-closing — story 9.5 shipped with tests in `review`; this run auto-applied coverage gaps against the implementation surface.

## Context

Story 9.5 ships internal `.Server` seams (no HTTP API, no UI), so "E2E" here means full behavioural coverage of the
tenant-partition contract, the store-access seam, and the synthetic cross-tenant isolation probe. Existing tests were
already strong; this run added focused tests for untested branches of the implementation surface.

## Gaps discovered and closed (18 new tests, all passing)

### `DerivedStorePartition` — `tests/.../Projections/DerivedStorePartitionTests.cs` (+3)
- [x] `SegmentReturnsTheStableWireConstantForEveryClass` — pins the four segment wire-constants (`vector-index`,
      `embedding-store`, `prompt-context-cache`, `candidate-ranking-cache`) so an enum rename is caught as a wire break
      (the M2 live binding adopts these literally).
- [x] `SegmentThrowsOnAnUnknownDerivedStoreClass` — out-of-range enum guard.
- [x] `PartitionPrefixIsDistinctPerClassForTheSameTenant` — one tenant → four non-aliasing partition buckets.

### `InMemoryDerivedStore` / `DerivedStoreEntry` — `tests/.../Projections/DerivedStores/InMemoryDerivedStoreTests.cs` (+6)
- [x] `PutOverwritesTheEntryForTheSameTenantClassAndResource` — last-write-wins, no duplicate residue.
- [x] `PutWithANullEntryThrows` — `ArgumentNullException` guard.
- [x] `GetWithAnUnsafeTenantIdThrowsFailClosed` — **reads are fail-closed too** (only writes were previously covered).
- [x] `EnumerateResourceIdsWithAnUnsafeTenantIdThrowsFailClosed` — enumeration is fail-closed.
- [x] `PutHonorsCancellation` / `GetHonorsCancellation` — cancellation token is observed.

### `DerivedStoreIsolationVerifier` — `tests/.../Audit/DerivedStoreIsolationProbeCoordinatorTests.cs` (+3)
- [x] `VerifierFirstOffenderLocatorIsDeterministicInOwnerSentinelOrder` — with multiple leaks the locator is the first
      in **owner-sentinel** order (stable across runs), not observable order.
- [x] `VerifierThrowsOnAMissingOwnerOrIntruderTenant` (Theory, 3 cases) — argument guards.

### `DerivedStoreIsolationProbeCoordinator` — same file (+3)
- [x] `EmptyStoreSweepProbesNoPairsAndPassesTheReleaseGate` — release-gate vacuous-clean with no tenants `(0,0,0)`.
- [x] `SingleTenantStoreSweepProbesNoPairs` — one tenant forms no ordered pair.
- [x] `CleanPairProbeReturnsCleanWithoutAuditingOrAlerting` — single clean pair writes no envelope and emits no alert.

### `DerivedStoreEntry.Create` no-leak — `tests/.../Audit/DerivedStoreIsolationLeakTests.cs` (+2)
- [x] `DerivedStoreEntryCreateWithNullDigestFallsBackToASafeToken` — null digest → `redacted-ref`, never null.
- [x] `DerivedStoreEntryCreateSanitizesAnUnsafeResourceIdToTheSafeFallback` — an unsafe (`.json`-bearing) resource id
      collapses to the safe fallback rather than smuggling content into the entry.

## Coverage

| Surface | Before | After |
| --- | --- | --- |
| `DerivedStorePartition` (KeyFor/PartitionPrefix/Segment/AllClasses, fail-closed) | KeyFor/PartitionPrefix happy + unsafe | + Segment wire-constants, out-of-range, per-class prefix distinctness |
| `IDerivedStore`/`InMemoryDerivedStore` (Put/Get/Enumerate, isolation, fail-closed) | cross-tenant isolation + Put fail-closed | + read/enumerate fail-closed, overwrite, null-entry, cancellation |
| `DerivedStoreIsolationVerifier` (Clean/Breach, locator) | Clean + single-leak Breach | + determinism, argument guards |
| `DerivedStoreIsolationProbeCoordinator` (sweep, audit-then-deliver, fail-closed) | clean/leaky/throwing/audit-suppressed/residue | + empty-store, single-tenant, clean single-pair release-gate edges |
| No-leak floor (`DerivedStoreEntry`, result, outcome, envelope) | sensitive-marker + envelope refs | + null-digest & unsafe-resource-id fallbacks |

## Validation

- `dotnet test tests/Hexalith.ChatBot.Server.Tests` ⇒ **1295 passed / 0 failed** (was 1277 → +18).
- All new tests are independent (each constructs its own store/coordinator), use semantic Shouldly assertions, carry
  clear descriptions, and use no hardcoded waits/sleeps (cancellation is driven by a pre-cancelled `CancellationTokenSource`).

## Next Steps

- No new test files were created — all gaps were appended to the story's existing test files, so the story File List
  remains accurate (no bookkeeping update required).
- The deferred M2 live `IDerivedStore` (Redis-Vector/FalkorDB) binding, when wired, should re-run the same probe
  coordinator suite against the live store and add a live-backed conformance pass.
