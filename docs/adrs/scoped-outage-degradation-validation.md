# ADR: Scoped outage degradation validation (Story 9.13)

- **Status:** Accepted
- **Epic / Story:** Epic 9 — Story 9.13 (Scoped outage degradation validation)
- **Drivers:** NFR58 (a dependency outage degrades only the affected narrowest scope — tenant / mailbox / operation / service-client / command-surface / workflow-item), NFR59 (resilience validation proves no cross-tenant leakage, no unauthorized state mutation, no silent data loss during a degraded Graph/identity/AI/command/audit/attachment outage, each assertion producing an evidence artifact), NFR41 (narrowest-scope isolation + the 5-min incident scope/dependency recording), NFR17 (in-flight items resume from a visible recoverable state on recovery), NFR13 (idempotency — no duplicate side effects on recovery), NFR9a (tenant isolation by construction), Epic 8/9 no-fabrication doctrine, D4 (two-phase audit, fail-open-then-reconcile), NFR2/NFR42/NFR45 (metadata-only / no-leak floor).

## Context

NFR58/NFR59 assert that a dependency outage **degrades only its scope** and never leaks across tenants, mutates unauthorized state, or loses data silently. Until now that is an assumed property. Story 9.13 makes the ChatBot's **scoped-degradation guarantee provable rather than assumed**: it injects each of the six NFR59 dependency outages against a **test tenant**, proves the degradation stayed within the expected narrowest scope (NFR58) with no cross-tenant leakage / no unauthorized mutation / no silent data loss (NFR59), that in-flight items resume from a visible recoverable state with no duplicate side effects on recovery (NFR17/NFR13), and that the incident scope + dependency was recorded within the single-source 5-min `RecoveryTargets.MaxScopeRecordingLatency` budget (NFR41) — emitting a metadata-only evidence artifact the NFR58/NFR59 validation is anchored to.

This is the **third and final of the three Epic 9 recovery/continuity-validation stories** (9.11 drill → 9.12 projection-rebuild → **9.13 scoped-outage degradation**). It is the **validation that proves** the scoped-degradation behaviour holds under each NFR59 outage — it is explicitly **NOT** the runtime degradation surface. **Story 8.5 already owns the runtime scoped-degradation behaviour** (the `DependencyDegraded` `OperatorAlertKind`, the degraded-state surface rendering, the live 5-min incident recording); 9.13 adds a **distinct** `ScopedOutageDegradationBreach` validation-breach alert, not a second runtime path.

## Decision

Build the validation as a **validation-harness**, modeled line-for-line on the Story 9.12 `ProjectionRebuildValidationCoordinator` / Story 9.11 `ContinuityDrillCoordinator` (which in turn follow 9.1/9.2/9.4/9.5): a **pure evaluator + an injectable coordinator + a structured outcome + fail-closed audit-then-deliver + a metadata-only report**, all `internal` to `.Server/Audit/`. It is explicitly **NOT** a governed-command story (no `IChatBotCommand`, no `ChatBotSpineCommandAllowlist` entry, no `ParticipantAuthorizationStage` gate, no `.Contracts` change) and **NOT** a second runtime degradation path.

### Components

- **`ScopedOutageDependencies`** — the closed set `{ graph, identity, ai-provider, command-execution, audit-store, attachment-processing }`, the scenario vocabulary the sweep iterates, mirroring `ContinuityDrillScenarios`. **`graph` covers both NFR59's degraded-Graph-access and the expired-subscription lapse** — both degrade at the mailbox boundary per architecture (M365/Graph = mailbox boundary, degraded per-mailbox, never tenant-wide); there is deliberately **no** seventh `subscription-expiry` token. The literals avoid the legacy-lifecycle tokens, so no `ScaffoldArchitectureTests` allowlist entry is required.
- **`ScopedOutageScopes`** — the closed set `{ tenant, mailbox, operation, service-client, command-surface, workflow-item }`, the NFR41/NFR58 narrowest-scope axes. An observed scope outside the expected scope is the NFR58 `scope_escape` breach.
- **`ScopedOutageDegradationVerdicts`** — the closed set `{ contained, breached, unmeasurable }`, mirroring `ContinuityDrillVerdicts` / `ProjectionRebuildVerdicts`.
- **`ScopedOutageDegradationEvaluator`** — a **pure, deterministic** function: `breached` iff any serious assertion failed (cross-tenant leakage, unauthorized mutation, silent data loss, scope escape, non-recoverable in-flight, or duplicate side effect); else `contained`. Plus a deterministic `FirstBreachLocator` (the first failed assertion in the stable order) and a stable bounded `Deviations` list (`cross_tenant_leakage` / `unauthorized_mutation` / `silent_data_loss` / `scope_escape` / `inflight_not_recoverable` / `duplicate_side_effect`, then `scope_recording_exceeded` when the recording is late). No clock, no IO. The `unmeasurable` verdict and the late-recording dimension live in the coordinator, **not** the evaluator — the verdict stays binary contained/breached over the serious assertions.
- **`ScopedOutageDegradationReport`** — the metadata-only NFR58/NFR59 **validation-evidence artifact** (tenant ref, dependency, expected/observed scope, start/end, scope-recording latency, `ScopeRecordedWithinTarget`, verdict, bounded deviations, first-breach locator, correlation id, reason code). Modeled on `ProjectionRebuildReport` with a fail-safe `Unmeasurable(...)` factory. `IsScopeBreach` is the serious NFR58/NFR59 breach; `IsBreach` folds all three fail-closed dimensions (breached **or** unmeasurable **or** late scope recording).
- **`ScopedOutageDegradationMeasurement`** — what the driver returns (expected/observed scope, the three NFR59 assertions, the NFR17 positive `InflightItemsRecoverable` assertion, the NFR13 `DuplicateSideEffectDetected` check, the measured scope-recording latency, wall-clock bounds).
- **`ScopedOutageDegradationOutcome`** — the structured sweep result (`ScenariosValidated`, `Contained`, `Breached`, `ScopeRecordingExceeded`, `Unmeasurable`, `Alerted`) a CI/release gate asserts against.
- **`IScopedOutageInjectionDriver`** — the seam the coordinator consumes to inject the outage + run the assertions. Product DI retains the inert `DeferredScopedOutageInjectionDriver`; Story 12.15 provides the separately constructed Tier-3 live driver.
- **`ScopedOutageDegradationValidationCoordinator`** — run-scenario + sweep, test-tenant-by-construction guard, fail-closed audit-then-alert, deviation/first-breach recording. Modeled directly on `ProjectionRebuildValidationCoordinator`.
- **`RecoveryTargets.MaxScopeRecordingLatency`** — the new single-source 5-min NFR41 budget. It is a **deliberately separate** constant from `WormAuditChainVerifier.DetectionToAlertBudget` (also 5 min, but the NFR49a chain-break detection-to-alert budget) — different NFRs that share the default value and recalibrate independently. The `FromMinutes(5)` literal is never re-typed for this concept (grep-confirmed).
- **`OperatorAlertKind.ScopedOutageDegradationBreach`** + **`AuditEnvelopeFactory.ScopedOutageDegradationBreach`** — the validation breach alert + the metadata-only pre-commit envelope (integer-second latency, boolean flags, bounded dependency/scope/verdict/deviation tokens, safe first-breach locator; Worker origin). Distinct from the Story 8.5 runtime `DependencyDegraded` alert.

### Three distinct breach dimensions (kept distinct in the outcome)

- **`breached`** — a serious NFR58/NFR59 isolation/scope/recovery breach (cross-tenant leakage / unauthorized mutation / silent data loss / scope escape / non-recoverable in-flight / duplicate side effect). The **stop-ship-style** breach, analogous to a 9.4/9.5 isolation breach. A release gate asserts `Breached == 0`.
- **`ScopeRecordedWithinTarget == false`** — a monitoring-latency **miss**, analogous to the Story 9.11 RPO/RTO miss and the Story 9.12 duration overrun: a recorded deviation that is a recalibration / follow-up signal, **not** by itself an isolation breach. A contained-but-slow degradation stays `contained` with `ScopeRecordedWithinTarget == false`.
- **`unmeasurable`** — the **fail-safe** breach (no evidence produced).

`ScopedOutageDegradationOutcome` keeps `Contained` / `Breached` / `ScopeRecordingExceeded` / `Unmeasurable` distinct so a gate asserts the dimension it cares about (e.g. `Breached == 0 && Unmeasurable == 0` ⇒ every dependency outage degraded only its scope and produced evidence). Never collapse `breached` or `unmeasurable` into `contained`, and never let a late scope recording silently pass.

### Recovery-resume checks consume the existing seams (NFR17/NFR13)

NFR17's visible-recoverable-state lifecycle (`pending`/`retryable`/`failed`/`quarantined`/`needs-review`) and NFR13's idempotency (coarse request-dedup + fine event-dedup, `CoarseIdempotencyOperationClass`) already exist. The measurement reports **whether** recovery honored them (`InflightItemsRecoverable`, `DuplicateSideEffectDetected`); this story adds **no** new state machine and **no** new idempotency cache. The `pending`/`retryable` tokens appear only as documented assertion semantics, never as new hard-coded literals.

### Fail-safe over fabrication (Epic 8/9 no-fabrication doctrine)

A validation that **cannot complete** — the driver throws, the outage exercise never finishes, or the assertion results are unavailable — yields `unmeasurable` (a breach signal that fail-closed-audits-then-alerts), never a fabricated `contained`. An unknown dependency or a production-tenant target biases to `unmeasurable` **without invoking the driver**. Mirrors `ContinuityDrillReport.Unmeasurable`, `ProjectionRebuildReport.Unmeasurable`, and `ReplayIsolationStatus.Unknown`.

### Tenant isolation by construction (NFR9a/NFR59)

The validation is isolated **because it runs under a test tenant** resolved by the single authoritative `ReplayTenantPolicy.IsTestTenant` predicate, and every durable store is tenant-partitioned, so the injected outage and its recovery land only in the test tenant's partition. The coordinator **fails closed to `unmeasurable` without invoking the driver** when the target is not a test tenant, so no production-tenant durable state is mutated. There is **no** second test-tenant discriminator and **no** "inject outage on a production tenant" flag.

### Read-mostly / out-of-band (D4, two-phase audit, NFR15a)

The validation injects the outage out-of-band against the test tenant and reads the resulting state; it adds **no** commit-time gate and never mutates the audit chain. Its only writes are (i) into the test tenant's partition during the outage exercise, and (ii) the fail-closed `ScopedOutageDegradationBreach` pre-commit audit envelope on a breach.

## Deferrals (inert-control-floor honesty)

Fully built **and tested** against a scripted fake driver: `ScopedOutageDependencies` / `ScopedOutageScopes` / `ScopedOutageDegradationVerdicts`, the pure `ScopedOutageDegradationEvaluator`, the `ScopedOutageDegradationReport`/`Unmeasurable` factory + `ScopedOutageDegradationMeasurement` + `ScopedOutageDegradationOutcome`, the `ScopedOutageDegradationValidationCoordinator` (run-scenario + sweep + fail-closed audit-then-alert), the new `OperatorAlertKind`, the `AuditEnvelopeFactory` envelope, and the `RecoveryTargets.MaxScopeRecordingLatency` constant.

**Story 12.15 implementation update (partial retirement).** **Retired:** the live fault-injection deferral for `identity` and `graph`, which fault real topology boundaries — an allowlisted Keycloak resource stop/start with the token-acquisition path as the probe, and the composed Worker/provider mailbox boundary including subscription expiry.

**Not retired, and re-opened by the round-4 review:**

1. **`ai-provider`, `command-execution`, `audit-store`, `attachment-processing`** — these call sandbox types shaped like the Server contracts, registered as concrete singletons and invoked directly. Only `command-execution` degrades ChatBot code; the real consumers (`AcceptedCommandDispatcher` for AI, `ChatBotCommandAdmissionPipeline`'s fail-closed `RecordPreCommitAsync` gate for audit) are bypassed. The `audit-store` contract — governed mutation fails closed when audit evidence is unavailable — is therefore not exercised at all.
2. **The three NFR59 assertions and the NFR13 duplicate check, for all four sandbox-exercised dependencies** — the faulted branch never records an effect and the unfaulted branch always records exactly one, over a correlation-id set both calls share, so unauthorized-mutation, silent-loss and duplicate-effect cannot evaluate `true`; nothing in the sandbox can write a second tenant, so cross-tenant leakage cannot either.
3. **NFR58 observed scope, for all six** — the sandbox monitor's scope table is a byte-identical copy of the driver's expectation table and feeds `ObservedScope` on every path, so `ExpectedScope == ObservedScope` unconditionally and the evaluator's `scope_escape` deviation is unreachable.
4. **NFR41 scope-recording latency, for all six** — both time bounds are minted inside one process a channel hop apart, and for `identity`/`graph` the one honest timestamp is overwritten by an on-demand stamp. No latency figure is published; missing monitoring evidence is `unmeasurable`, not a sub-millisecond latency.

The serialized CI/release gate invokes all six through `RunAllScenariosAsync` on the scheduled, manually dispatched and release lanes only — ordinary `push`/`pull_request` CI runs no recovery validation. Product DI remains inert; external Graph, durable WORM, production control, product composition, provider/traffic scale, and the lane's measurable recovery ceiling remain explicit residuals.

## Consequences

- Scoped degradation is now backed by a **provable harness** rather than an unvalidated assumption; the NFR58/NFR59 validation has a concrete per-scenario evidence artifact (dependency + expected/observed scope + the three assertion outcomes + verdict).
- A CI/release gate can assert `Breached == 0 && Unmeasurable == 0` (every dependency outage degraded only its scope and produced evidence) independently of `ScopeRecordingExceeded == 0` (within the 5-min NFR41 budget).
- Backward-compatible: adding a coordinator + an `OperatorAlertKind` member + an envelope factory method + a `RecoveryTargets` constant keeps all existing Epic 1–9 audit, gateway, conformance, and architecture tests green; the dependency/scope/verdict tokens avoid the legacy-lifecycle literals so no `ScaffoldArchitectureTests` allowlist entry is required.

## References

- Story 9.11 — `9-11-continuity-drill-and-rpo-rto-validation.md` (the continuity coordinator twin and the `RecoveryTargets` owner).
- Story 9.12 — `9-12-projection-rebuild-validation.md` (the closest coordinator twin; the `RecoveryTargets` owner and the within-target-boolean template).
- Story 9.4 — `9-4-replay-and-simulation-isolation.md` (the test-tenant + deferred-driver template).
- Story 9.5 — `9-5-derived-store-cross-tenant-isolation.md` (the active-isolation-breach + audit-then-deliver + sweep template).
- Story 8.5 — the **runtime** scoped-degradation behaviour this story validates (`DependencyDegraded`, the degraded-state surface, the live 5-min incident recording) — a distinct path, not re-implemented here.
- `architecture.md` (recovery / scoped outage degradation, M365/Graph mailbox boundary degraded per-mailbox no tenant-wide, status sub-states, pattern enforcement, Workers [M2] resilience validation); `epics.md` (Story 9.13, NFR58, NFR59, NFR41, NFR17, NFR13).
