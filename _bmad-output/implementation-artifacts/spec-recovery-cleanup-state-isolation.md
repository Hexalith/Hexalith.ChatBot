---
title: 'Isolate recovery cleanup tracking state'
type: 'bugfix'
created: '2026-08-28'
status: ready-for-dev
baseline_revision: '9fb71f24bbb9148eb6a406f889e046118c81e491'
baseline_commit: '9fb71f24bbb9148eb6a406f889e046118c81e491'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/planning-artifacts/architecture.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Recovery operations retain note, intake, sentinel, and observation state whenever cleanup is incomplete, so later scenarios on the same operations instance can verify or erase stale resources. A checkpoint note is intentionally tracked before its durable wait confirms, but that unconfirmed reference currently remains attached after cleanup reports it missing.

**Approach:** Detach each scenario's tracked cleanup state before cleanup performs any awaited work, reset the active generation immediately, and complete verification, erasure, restoration, and diagnostics from the detached snapshot. Add always-run operations-level regressions proving an incomplete cleanup cannot contaminate the next scenario and an unconfirmed pre-commit note is handled and retired.

## Boundaries & Constraints

**Always:** Cover EventStore continuity, subscription/controlled-loss, and scoped-outage tracking; keep cleanup working from a stable snapshot after the active generation is reset; retire flags with the refs that produced them; preserve stable metadata-only diagnostics and existing cleanup return/exception semantics; propagate caller cancellation; keep EventStore aggregates append-only and erase only harness-owned read models.

**Block If:** Correctness would require widening cleanup authority beyond harness-owned read models, changing public recovery interfaces/evidence schemas, or changing A10/NFR verdict thresholds.

**Never:** Edit the deferred-work ledger; weaken presence, absence, tenant-isolation, or durable-state checks; omit the deliberately pre-wait checkpoint ref; log tenant/resource identifiers or payloads; initialize nested submodules; require Aspire/DAPR startup for the new regression tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Complete cleanup | One scenario owns confirmed refs and observations | Cleanup uses one detached snapshot, reports complete, and leaves the active generation empty | No error expected |
| Partial cleanup then next scenario | Verification reports incomplete or an erase path fails after state was captured | Old refs remain available to the current cleanup/diagnostic path but are absent from the next scenario's active state | Preserve the existing false result or exception while preventing cross-scenario reuse |
| Pre-commit checkpoint failure | A note ref is recorded, but projection/durable confirmation never completes | Cleanup treats the ref as owned, handles its absence, retires it, and the next scenario starts clean | Caller cancellation propagates; ordinary absence remains cleanup evidence, not a leaked generation |

</intent-contract>

## Code Map

- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs:83` -- continuity and controlled-loss tracking fields; seed records `_checkpointNoteRefs` before submit/projection/durable waits at lines 154-173.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs:557` -- EventStore cleanup emits metadata-only `incompleteChecks`, but resets refs only inside `if (complete)` at lines 708-715.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs:1169` -- subscription/controlled-loss cleanup shares intake and sentinel fields, resets them only on success, and does not reset `_subscriptionFaultLeftStateUnchanged`.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs:49` -- scoped-outage sentinel/intake/control-note state is reused across the six serial scenarios.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs:550` -- scoped cleanup builds targets from live fields and clears them only when every verification succeeds.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:225` -- one operations instance is reused for both continuity scenarios and then controlled loss, making generation isolation load-bearing.
- `src/Hexalith.ChatBot.Server/Audit/ContinuityDrillCoordinator.cs:122` -- continuity scenarios execute serially; ordering must not become an implicit cleanup mechanism.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/InMemoryRecoveryReadModelStore.cs` -- reusable ETag/read-model failure seams for always-run cleanup regressions.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/EventStoreRecoveryCleanupState.cs`, `SubscriptionRecoveryCleanupState.cs`, and `ScopedOutageRecoveryCleanupState.cs` -- encapsulate each active generation and provide a detach-and-reset operation that preserves a typed cleanup snapshot; keep one C# type per file.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs` -- detach/reset EventStore and subscription/controlled-loss tracking at cleanup entry, then use only the detached data for checks, erasure, restoration, and diagnostics; reset both observation flags with their generations.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs` -- apply the same detach/reset boundary to scoped-outage state so a partial dependency cleanup cannot contaminate its successor.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoveryCleanupStateTests.cs` -- cover a partial cleanup followed by a new scenario, an unconfirmed pre-commit checkpoint ref, paired observation-flag reset, and stable detached diagnostic inputs.

**Acceptance Criteria:**
- Given any cleanup path has captured scenario-owned state, when cleanup returns false or throws after capture, then a subsequent scenario on the same tracker observes none of the prior refs, sentinels, or flags.
- Given a checkpoint note is tracked before its durable wait and that wait never confirms, when cleanup runs, then it still attempts owned cleanup/absence verification for that note and retires the note before another scenario begins.
- Given cleanup diagnostics are produced from a failed verification, when the active generation is reset, then the diagnostic retains only its existing stable check codes and never exposes tenant or resource identifiers.
- Given caller cancellation during cleanup, when the cancellation is observed, then it propagates and the retired generation is not restored to active state.

## Spec Change Log

## Review Triage Log

## Design Notes

The isolation boundary is a generation handoff: capture all fields needed by the current cleanup, replace active state with a fresh empty generation before the first await, and never consult active fields again during that cleanup. This preserves diagnostic/erase inputs locally while making failure incapable of leaking them into the next serial scenario.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Release` -- expected: zero warnings and errors.
- `./tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net10.0/Hexalith.ChatBot.IntegrationTests -class Hexalith.ChatBot.IntegrationTests.Recovery.AspireRecoveryCleanupStateTests` -- expected: all focused cleanup-state regressions pass with no skips.
- `./tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net10.0/Hexalith.ChatBot.IntegrationTests` -- expected: all always-run integration tests pass; live Tier-3-only tests may self-skip when their explicit environment gate is absent.

**Results (2026-08-28):**
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Release --no-restore --no-dependencies -m:1 -p:UseHexalithProjectReferences=true` -- passed with zero warnings and errors; validates the changed integration-test project against the already-built source-reference dependencies.
- Focused cleanup-state executable command -- passed 7/7 with zero skips.
- Prescribed Release build -- blocked before compiling the integration-test project by the package-mode `Hexalith.Memories.Contracts` dependency lacking the `V1.DerivedStores` contracts used by `Hexalith.ChatBot.Server` (21 compiler errors, zero warnings).
- Full always-run executable -- 325 total, 18 failed, 6 skipped; failures are outside the cleanup-state class and include stale dependency API loading, current AppHost endpoint configuration, and an existing projection-rebuild write-count assertion.

