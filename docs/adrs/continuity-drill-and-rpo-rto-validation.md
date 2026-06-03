# ADR: Continuity drill and RPO/RTO validation (Story 9.11)

- **Status:** Accepted
- **Epic / Story:** Epic 9 — Story 9.11 (Continuity drill and RPO/RTO validation)
- **Drivers:** NFR56 (RPO ≤ 15 min / RTO ≤ 4 hr recovery targets), A10 ([ASSUMPTION] targets pending the M2 drill), NFR59 (no cross-tenant leakage / no unauthorized mutation during recovery), NFR9a (tenant isolation by construction), D4 (two-phase audit, fail-open-then-reconcile), Epic 8/9 no-fabrication doctrine.

## Context

The MVP recovery targets — source records, attachments, approval history, command history, policy snapshots, and audit records meet **RPO ≤ 15 min / RTO ≤ 4 hr** — are an **[ASSUMPTION] per A10**, *framed but not yet proven*. Story 9.11 builds the **M2 continuity drill** that makes them **provable rather than assumed**: it runs a recovery exercise for each of the two required scenarios, measures the achieved RPO/RTO against the targets, runs a data-loss check, and produces a metadata-only evidence artifact that the A10 recalibration is anchored to.

This is the **first of the three Epic 9 recovery/continuity-validation stories** (9.11 drill → 9.12 projection-rebuild validation → 9.13 scoped-outage degradation).

## Decision

Build the drill as a **validation-harness**, modeled line-for-line on the Story 9.5 `DerivedStoreIsolationProbeCoordinator` (which in turn follows 9.1/9.2/9.4): a **pure evaluator + an injectable coordinator + a structured outcome + fail-closed audit-then-deliver + a metadata-only report**, all `internal` to `.Server/Audit/`. It is explicitly **NOT** a governed-command story (no `IChatBotCommand`, no `ChatBotSpineCommandAllowlist` entry, no `ParticipantAuthorizationStage` gate, no `.Contracts` change).

### Components

- **`RecoveryTargets`** — the **single source of truth** for the RPO/RTO targets (`MaxRpo = 15 min`, `MaxRto = 4 hr`), each XML-doc'd as the A10/NFR56 [ASSUMPTION] target. Mirrors `AuditCompletenessMeasurement.CompletenessTargetFraction`. The `15`/`4` literals live **here only** — never re-typed elsewhere.
- **`ContinuityDrillScenarios`** — the closed set `{ eventstore-outage, m365-subscription-failure }` (both NFR56-required; the sweep runs both). An unknown/unsafe scenario biases to `unmeasurable` (fail-safe).
- **`ContinuityDrillVerdicts`** — the closed set `{ met, missed, unmeasurable }`.
- **`ContinuityDrillEvaluator`** — a **pure, deterministic** function: `met` iff `measuredRpo ≤ MaxRpo && measuredRto ≤ MaxRto && !dataLoss`, else `missed`; plus a stable bounded `Deviations` list (`rpo_exceeded` / `rto_exceeded` / `data_loss_detected`). No clock, no IO.
- **`ContinuityDrillReport`** — the metadata-only A10 recalibration **evidence artifact** (scenario, start/end, measured RPO/RTO, data-loss flag, verdict, bounded deviations, recalibration flag, follow-up ref, tenant ref, correlation id, reason code). Modeled on `AuditCompletenessMeasurement` with a fail-safe `Unmeasurable(...)` factory. `IsMiss` distinguishes an honest miss from `IsBreach` (a miss **or** unmeasurable both fail-closed-audit-then-alert).
- **`ContinuityDrillOutcome`** — the structured sweep result (`ScenariosRun`, `Met`, `Missed`, `Unmeasurable`, `Alerted`) a CI/release gate asserts against.
- **`IContinuityDrillScenarioRunner`** — the seam the coordinator consumes for measured RPO/RTO + data-loss (returns `ContinuityDrillMeasurement`). The **live fault-injection runtime is M2-deferred**; the inert `DeferredContinuityDrillScenarioRunner` throws `NotSupportedException`.
- **`ContinuityDrillCoordinator`** — run-scenario + sweep, test-tenant-by-construction guard, fail-closed audit-then-alert, deviation/follow-up/recalibration recording. Modeled directly on `DerivedStoreIsolationProbeCoordinator`.
- **`OperatorAlertKind.ContinuityDrillTargetMissed`** + **`AuditEnvelopeFactory.ContinuityDrillTargetMissed`** — the breach alert + the metadata-only pre-commit envelope (integer-second durations, boolean flags, bounded deviation tokens, safe follow-up locator; Worker origin).

### The data-loss check (RPO semantics)

RPO is the bound on tolerable data loss. The drill compares operations committed to the WORM chain (the **source of truth** — D4 fail-open-then-reconcile: the event log is authoritative and the chain is rebuilt from it on recovery) **before** the simulated outage against the set reconstructable **after** recovery. Any committed-before-outage operation missing after recovery sets `DataLossDetected = true`, forcing a non-`met` verdict. **The deep deterministic projection-rebuild equivalence proof is Story 9.12's scope** — 9.11 owns the drill / measurement / report / data-loss-check and does not duplicate 9.12's rebuild-equivalence assertion.

### `missed` ≠ stop-ship; `unmeasurable` = breach

Unlike the 9.4/9.5 isolation probes (zero breaches = stop-ship), an RPO/RTO **miss** is an A10 [ASSUMPTION]-recalibration signal: log the deviation, set `RecalibrationFlag = true`, record a `FollowUpActionRef` — **honest evidence, not a release blocker**. The fail-safe breach here is an **`unmeasurable`** drill (no recovery evidence produced). `ContinuityDrillOutcome` keeps `Met`/`Missed`/`Unmeasurable` distinct so a gate asserts the dimension it wants (e.g. `Unmeasurable == 0` ⇒ the drills ran and produced evidence). Never collapse `missed` or `unmeasurable` into `met`.

### A10 recalibration procedure

The `ContinuityDrillReport` **is** the recalibration evidence. The actual edit of the architecture/PRD A10 [ASSUMPTION] marker (NFR56) is a **documented human/ADR follow-up**, captured here — it is **not** a value the drill code mutates at runtime. On a miss, the recorded `FollowUpActionRef` (`continuity-recalibration:{scenario}`) makes the recalibration obligation explicit and auditable; an operator/architect reviews the drill evidence and, if warranted, edits `RecoveryTargets` and the A10 marker in a follow-up change.

### Tenant isolation by construction (NFR9a/NFR59)

Recovery is isolated **because the drill runs under a test tenant** resolved by the single authoritative `ReplayTenantPolicy.IsTestTenant` predicate, and every durable store is tenant-partitioned. The coordinator **asserts** (fails closed to `unmeasurable`) when the target is not a test tenant, so no production-tenant durable state is mutated. There is **no** "drill flag on a production tenant".

## Deferrals (inert-control-floor honesty)

Fully built **and tested** against a scripted fake runner: `RecoveryTargets`, the token sets, the pure `ContinuityDrillEvaluator`, the `ContinuityDrillReport`/`Unmeasurable` factory, the `ContinuityDrillCoordinator` (run + sweep + fail-closed audit-then-alert), the `ContinuityDrillOutcome` gate, the new `OperatorAlertKind`, and the `AuditEnvelopeFactory` envelope.

**Deferred** (documented seams, exactly like Story 9.4's deferred replay driver + 9.1/9.2/9.4/9.5's deferred periodic scheduler):

1. The **live fault-injection runtime** that actually downs a real EventStore / lapses a real M365 Graph subscription against a deployed AKS/Aspire environment — modeled behind `IContinuityDrillScenarioRunner` (inert default throws `NotSupportedException`).
2. The **periodic scheduler / release-gate wiring** — no always-on `BackgroundService`; a scheduler/gate need only call `RunAllScenariosAsync` on its cadence and supply the real runner.

**The deferral never reads as "recovery is unproven."** The drill harness, the measurement semantics, the data-loss check, and the fail-closed evidence path are all real and tested against the scenario-runner seam.

## Consequences

- The RPO/RTO targets are now backed by a **provable harness** rather than an unvalidated assumption; the A10 recalibration has a concrete evidence artifact.
- A CI/release gate can assert `Unmeasurable == 0` (drills produced evidence) independently of `Missed == 0` (targets met).
- Backward-compatible: adding a coordinator + an `OperatorAlertKind` member + an envelope factory method keeps all existing Epic 1–9 audit, gateway, conformance, and architecture tests green; the scenario/verdict tokens avoid the legacy-lifecycle literals so no `ScaffoldArchitectureTests` allowlist entry is required.

## References

- Story 9.4 — `9-4-replay-and-simulation-isolation.md` (the test-tenant + deferred-driver + probe-coordinator template).
- Story 9.5 — `9-5-derived-store-cross-tenant-isolation.md` (the closest coordinator twin).
- Story 9.2 — `9-2-audit-completeness-as-a-production-observable.md` (the measurement + fail-safe twin).
- Story 9.12 — projection-rebuild validation (the deep rebuild-equivalence proof this story defers to).
- Story 9.13 — scoped-outage degradation.
- `architecture.md` (recovery framing, two-phase audit / fail-open-then-reconcile, M2 continuity drill); `epics.md` (Story 9.11, NFR56, A10).
