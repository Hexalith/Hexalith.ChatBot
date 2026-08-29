---
title: 'Isolate recovery cleanup tracking state'
type: 'bugfix'
created: '2026-08-28'
status: in-review
baseline_revision: '9fb71f24bbb9148eb6a406f889e046118c81e491'
baseline_commit: '9fb71f24bbb9148eb6a406f889e046118c81e491'
review_loop_iteration: 4
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
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/EventStoreRecoveryCleanupState.cs`, `SubscriptionRecoveryCleanupState.cs`, and `ScopedOutageRecoveryCleanupState.cs` -- encapsulate each active generation and provide a detach-and-reset operation that preserves a typed cleanup snapshot; represent every distinct valid cleanup-owned identity exposed by one producer response without overwriting or conflating an intake and candidate; distinguish an absent optional identity property from an exposed malformed/whitespace identity so later validation rejects the malformed response after retaining any valid sibling; keep one C# type per file.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs` -- detach/reset EventStore and subscription/controlled-loss tracking at each concrete cleanup entry before its first await, then use only the detached data for checks, erasure, restoration, and diagnostics; reset both observation flags with their generations. Extract and canonically validate every harness-owned note, intake, and candidate identity that a response exposes, retain each valid distinct identity before checking `submitted`, contract-required identity equality, timestamps, or later awaits, and clean every retained identity. Reject any exposed noncanonical identity rather than treating it as absent; validate equality only after both valid values have been retained. Preserve whitespace rejection before durable polling, execute both pre-fault and post-recovery witness branches, and make an incomplete diagnostic write observe caller cancellation without emitting identifiers.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs` -- apply the same detach/reset boundary at the concrete scoped-outage cleanup entry so a partial dependency cleanup cannot contaminate its successor; retain every distinct valid Graph intake/candidate identity before later response validation, reject exposed noncanonical identities, and perform contract-required equality validation only after retaining both values; provide an infrastructure-free cleanup seam that executes Graph and Identity; preserve whitespace rejection before durable polling.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoveryCleanupStateTests.cs` and only the minimum internal seams required by those tests -- invoke the real producer/cleanup methods without Aspire/DAPR startup and satisfy every row below. Trigger successor state by a named key/operation rather than a read-attempt ordinal. Build expected resource keys and durable request expectations independently from production target construction, then assert every seeded old-generation key not deliberately recreated is absent, the named recreated key makes cleanup false, every successor key remains, and required restore calls occurred. A success test must fail if a required erase/absence/durable verification loop or tenant/ref is omitted; direct state-helper tests may supplement but must not substitute for this coverage.

| Concrete surface | Required always-run evidence |
|------------------|------------------------------|
| EventStore seed and cleanup | Couple `SeedCommittedOperationAsync` failing after note allocation directly to `CleanupEventStoreScenarioAsync` while that unconfirmed projection remains absent; require the checkpoint-negative `false` result, exact stable diagnostic, independent post-cleanup absence, and retired successor state without manually materializing the note. Also cover complete `true`, control/fault negative `false` results with exact codes, fault-probe reappearance after its pre-erase check, first-awaited-operation exception, already-observed cancellation, and cancellation that arrives while the diagnostic writer is blocked. Exercise checkpoint, control-tenant, and fault-probe refs and assert every old active field is absent from successor state. |
| Subscription / controlled loss | Invoke checkpoint, both pre-fault and post-recovery witness branches, reconciliation/duplicate, and rejected-candidate producers with valid intake/candidate identities followed by `submitted=false`, contract-required mismatch, an exposed malformed identity or later timestamp, and an awaited durable-evidence failure; prove every valid distinct canonical identity remains owned before the later failure and malformed/whitespace identities never reach durable polling. Invoke `CleanupSubscriptionScenarioAsync` for complete `true`, post-restore `false`, first-erasure exception/cancellation, and restore exception. Exercise every retained identity, both sentinels, both flags, whitespace identities, and required durable tenant checks. Assert exact independently built durable tenant/ref requests, falsify each load-bearing durable loop with a named present aggregate, prove restore is attempted after first-erasure failure, independently assert all non-recreated old keys are absent on incomplete cleanup, and assert every old field is absent from successor state. |
| Scoped outage | Invoke the Graph producer with valid intake/candidate identities followed by `submitted=false`, contract-required mismatch, an exposed malformed identity, and an awaited durable-evidence failure; prove each valid identity remains owned before the later failure and whitespace never reaches durable polling. Invoke `AspireScopedOutageOperations.CleanupAsync` for Graph and Identity; cover complete `true`, incomplete `false`, first-erasure exception, and cancellation. Exercise both branch sentinels, both flags, every Graph identity, control-operation refs, and whitespace values; prove restore is attempted after first-erasure failure, independently assert all non-recreated old keys/control-note projections are absent on incomplete cleanup, falsify the Graph duplicate durable check with a named present aggregate and exact tenant/ref request, and assert every old field is absent from successor state. |

**Acceptance Criteria:**
- Given any cleanup path has captured scenario-owned state, when cleanup returns false or throws after capture, then a subsequent scenario on the same tracker observes none of the prior refs, sentinels, or flags.
- Given a checkpoint note is tracked before its durable wait and that wait never confirms, when cleanup runs, then it still attempts owned cleanup/absence verification for that note and retires the note before another scenario begins.
- Given cleanup diagnostics are produced from a failed verification, when the active generation is reset, then the diagnostic retains only its existing stable check codes and never exposes tenant or resource identifiers.
- Given caller cancellation during cleanup, when the cancellation is observed, then it propagates and the retired generation is not restored to active state.

## Spec Change Log

- **Review iteration 1 (2026-08-29):** Three independent reviewers found that the green cleanup tests called the state helpers directly and never invoked the concrete cleanup entry points. Amended the execution task to require always-run tests of all three concrete methods, including complete, incomplete, exception, cancellation, successor-generation, pre-commit absence, flag, and diagnostic behavior without Aspire/DAPR startup. The prior helper-only state was invalid because moving detachment after the first await would still pass every added regression. KEEP: typed per-domain active generations, detachment before the first await, detached-snapshot-only cleanup, stable allowlisted metadata-only diagnostics, pre-wait EventStore checkpoint tracking, caller-cancellation propagation, append-only aggregates, and harness-owned read-model erasure. Also preserve non-whitespace guards for optional identities so cleanup never polls durable state with a whitespace key.
- **Review iteration 2 (2026-08-29):** Review found that iteration 1 required outcomes only across the portfolio, allowing each concrete surface and the scoped Identity branch to omit load-bearing failure paths. Replaced the broad test task with a per-surface matrix covering real producer timing, complete/incomplete/exception/cancellation cleanup, every tracked auxiliary ref/sentinel/flag, both scoped branches, and successor isolation. The known-bad state used direct state injection for the unconfirmed checkpoint, observed subscription/scoped reset only after earlier awaits, omitted valid controlled-loss identities and EventStore auxiliary refs, could not execute Identity cleanup through its seam, and triggered a successor by brittle read ordinal. KEEP: the iteration-1 design and constraints, cleanup-only internal constructors/delegates, real concrete-entry tests, in-memory conditional-erasure callbacks, metadata-only diagnostic writer seam, zero-live-infrastructure execution, whitespace guards, and zero-warning focused build.
- **Review iteration 3 (2026-08-29):** Review found that producer responses can expose distinct valid intake and candidate identities before later submission/evidence validation fails, while the state model retained only success-path identities. It also found circular verification: production and tests reused the same target list, so omitting a target could leave both erase and assertion green. Amended state/producer tasks for canonical multi-identity capture before all later validation/awaits and amended the matrix to require independent per-key absence, negative control/fault diagnostics, fault-probe reappearance, complete successor-field assertions, partial-submission/mismatch producer cases, and scoped whitespace coverage. The known-bad state could lose candidate identities, skip auxiliary verification branches, or leave old keys while reporting complete. KEEP: all iteration-2 constraints, the 18-case concrete matrix structure, real producer calls, Graph/Identity theories, named callbacks, infrastructure-free seam types, in-memory named-key hooks, typed state, detached-only cleanup, and focused zero-warning build.
- **Review iteration 4 (2026-08-29):** Review found that valid sibling identities were retained but exposed malformed companion identities could be silently ignored, several submitted producers omitted contract-required equality checks, and the 18-case matrix remained green if pre-wait absence, awaited producer failure, durable tenant/ref verification, restoration after first-erasure failure, or most incomplete-path erasures were broken. Amended the producer tasks to reject every exposed noncanonical identity and validate equality only after retaining all valid identities. Amended the matrix to couple the real pre-wait failure to absent cleanup, execute both witness branches and awaited failures, assert exact durable requests with named present-aggregate negatives, observe restoration after erase failure, assert every non-recreated old key on false results, and propagate cancellation while diagnostic output is blocked. The known-bad state could accept malformed/mismatched responses, report green from vacuous always-404 durable handlers, or leave unrelated old keys while its sole recreated-key assertion still passed. KEEP: every prior change-log constraint; typed multi-identity generations; detach/reset before the first await; detached-snapshot-only cleanup; stable metadata-only diagnostics; independent literal key builders; named successor/reappearance/failure triggers; infrastructure-free real producer and cleanup methods; the 10-method/18-case matrix structure; complete Graph/Identity theories; fault-probe post-erase reappearance coverage; exact negative diagnostic codes; all successor-field assertions; and the zero-warning focused build.

## Review Triage Log

## Design Notes

The isolation boundary is a generation handoff: capture all fields needed by the current cleanup, replace active state with a fresh empty generation before the first await, and never consult active fields again during that cleanup. This preserves diagnostic/erase inputs locally while making failure incapable of leaking them into the next serial scenario.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Release` -- expected: zero warnings and errors.
- `./tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net10.0/Hexalith.ChatBot.IntegrationTests -class Hexalith.ChatBot.IntegrationTests.Recovery.AspireRecoveryCleanupStateTests` -- expected: all focused cleanup-state regressions pass with no skips.
- `./tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net10.0/Hexalith.ChatBot.IntegrationTests` -- expected: all always-run integration tests pass; live Tier-3-only tests may self-skip when their explicit environment gate is absent.

**Results (2026-08-29):**
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Release --no-restore --no-dependencies -m:1 -p:UseHexalithProjectReferences=true` -- passed with zero warnings and errors using installed SDK 10.0.400 from a temporary working directory; validates the changed integration-test project against the already-built source-reference dependencies.
- Focused cleanup-state executable command -- passed 18/18 with zero skips.
- Prescribed Release build from the repository root -- blocked before evaluation because `global.json` requests SDK 10.0.302 while only SDK 10.0.400 is installed.
- Equivalent absolute-project Release build from a temporary working directory -- blocked before compiling the integration-test project by the package-mode `Hexalith.Memories.Contracts` dependency lacking the `V1.DerivedStores` contracts used by `Hexalith.ChatBot.Server` (21 compiler errors, zero warnings).
- Full always-run executable -- 337 total, 18 failed, 6 skipped; failures are outside the cleanup-state class and include stale dependency API loading, current AppHost endpoint configuration, and an existing projection-rebuild write-count assertion.
