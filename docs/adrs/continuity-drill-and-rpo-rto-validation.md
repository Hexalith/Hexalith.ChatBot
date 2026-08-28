# ADR: Continuity drill and RPO/RTO validation (Story 9.11)

- **Status:** Accepted
- **Epic / Story:** Epic 9 — Story 9.11 (Continuity drill and RPO/RTO validation)
- **Drivers:** NFR56 (RPO ≤ 15 min / RTO ≤ 4 hr recovery targets), A10 (provisional targets pending a retained hosted run locator; see Story 12.15), NFR59 (no cross-tenant leakage / no unauthorized mutation during recovery), NFR9a (tenant isolation by construction), D4 (two-phase audit, fail-open-then-reconcile), Epic 8/9 no-fabrication doctrine.

## Context

The MVP recovery targets — source records, attachments, approval history, command history, policy snapshots, and audit records meet **RPO ≤ 15 min / RTO ≤ 4 hr** — originated as **[ASSUMPTION] A10**. Story 9.11 built the M2 continuity-drill contract; Story 12.15 supplied live Aspire/DAPR drivers and a passing local diagnostic for both mandatory scenarios. The targets remain provisional until a hosted workflow retains the evidence and its run/artifact locator is recorded.

This is the **first of the three Epic 9 recovery/continuity-validation stories** (9.11 drill → 9.12 projection-rebuild validation → 9.13 scoped-outage degradation).

## Decision

Build the drill as a **validation-harness**, modeled line-for-line on the Story 9.5 `DerivedStoreIsolationProbeCoordinator` (which in turn follows 9.1/9.2/9.4): a **pure evaluator + an injectable coordinator + a structured outcome + fail-closed audit-then-deliver + a metadata-only report**, all `internal` to `.Server/Audit/`. It is explicitly **NOT** a governed-command story (no `IChatBotCommand`, no `ChatBotSpineCommandAllowlist` entry, no `ParticipantAuthorizationStage` gate, no `.Contracts` change).

### Components

- **`RecoveryTargets`** — the **single source of truth** for the RPO/RTO targets (`MaxRpo = 15 min`, `MaxRto = 4 hr`), each XML-doc'd as the A10/NFR56 **provisional** target pending a retained hosted run locator. Mirrors `AuditCompletenessMeasurement.CompletenessTargetFraction`. The `15`/`4` literals live **here only** — never re-typed elsewhere.
- **`ContinuityDrillScenarios`** — the closed set `{ eventstore-outage, m365-subscription-failure }` (both NFR56-required; the sweep runs both). An unknown/unsafe scenario biases to `unmeasurable` (fail-safe).
- **`ContinuityDrillVerdicts`** — the closed set `{ met, missed, unmeasurable }`.
- **`ContinuityDrillEvaluator`** — a **pure, deterministic** function: `met` iff `measuredRpo ≤ MaxRpo && measuredRto ≤ MaxRto && !dataLoss`, else `missed`; plus a stable bounded `Deviations` list (`rpo_exceeded` / `rto_exceeded` / `data_loss_detected`). No clock, no IO.
- **`ContinuityDrillReport`** — the metadata-only A10 recalibration **evidence artifact** (scenario, start/end, measured RPO/RTO, data-loss flag, verdict, bounded deviations, recalibration flag, follow-up ref, tenant ref, correlation id, reason code). Modeled on `AuditCompletenessMeasurement` with a fail-safe `Unmeasurable(...)` factory. `IsMiss` distinguishes an honest miss from `IsBreach` (a miss **or** unmeasurable both fail-closed-audit-then-alert).
- **`ContinuityDrillOutcome`** — the structured sweep result (`ScenariosRun`, `Met`, `Missed`, `Unmeasurable`, `Alerted`) a CI/release gate asserts against.
- **`IContinuityDrillScenarioRunner`** — the seam the coordinator consumes for measured RPO/RTO + data-loss (returns `ContinuityDrillMeasurement`). Product DI retains the inert `DeferredContinuityDrillScenarioRunner`; Story 12.15 provides the separately constructed Tier-3 live runner.
- **`ContinuityDrillCoordinator`** — run-scenario + sweep, test-tenant-by-construction guard, fail-closed audit-then-alert, deviation/follow-up/recalibration recording. Modeled directly on `DerivedStoreIsolationProbeCoordinator`.
- **`OperatorAlertKind.ContinuityDrillTargetMissed`** + **`AuditEnvelopeFactory.ContinuityDrillTargetMissed`** — the breach alert + the metadata-only pre-commit envelope (integer-second durations, boolean flags, bounded deviation tokens, safe follow-up locator; Worker origin).

### The data-loss check (RPO semantics)

RPO is the bound on tolerable data loss. The drill compares operations committed to the WORM chain (the **source of truth** — D4 fail-open-then-reconcile: the event log is authoritative and the chain is rebuilt from it on recovery) **before** the simulated outage against the set reconstructable **after** recovery. Any committed-before-outage operation missing after recovery sets `DataLossDetected = true`, forcing a non-`met` verdict. **The deep deterministic projection-rebuild equivalence proof is Story 9.12's scope** — 9.11 owns the drill / measurement / report / data-loss-check and does not duplicate 9.12's rebuild-equivalence assertion.

### `missed` ≠ stop-ship; `unmeasurable` = breach

Unlike the 9.4/9.5 isolation probes (zero breaches = stop-ship), an RPO/RTO **miss** is an A10 [ASSUMPTION]-recalibration signal: log the deviation, set `RecalibrationFlag = true`, record a `FollowUpActionRef` — **honest evidence, not a release blocker**. The fail-safe breach here is an **`unmeasurable`** drill (no recovery evidence produced). `ContinuityDrillOutcome` keeps `Met`/`Missed`/`Unmeasurable` distinct so a gate asserts the dimension it wants (e.g. `Unmeasurable == 0` ⇒ the drills ran and produced evidence). Never collapse `missed` or `unmeasurable` into `met`.

### A10 recalibration procedure

The `ContinuityDrillReport` **is** the recalibration evidence; drill code never mutates targets. Story 12.15 supplied live Aspire/DAPR drivers for both mandatory scenarios. A genuine hosted, repository-retained run now publishes measured figures: release run `33066358280` (commit `17aa94d`, evidence `01M11EYSDMP1ZF38B7KZA1A6FA`, 2026-08-27) reports both scenarios `met` — RPO 0s (no-loss-path constant) / RTO 149.6s and 60.5s against the 900s/14400s targets. Architecture/DevOps left A10/NFR56 provisional at 15 minutes / 4 hours without changing `RecoveryTargets`: the measured RPO is still a constant on the no-loss path, and RTO is bounded by the lane's 180-second measurable-recovery ceiling, so this run does not discharge either half of A10. See `.decision-log.md` (2026-08-27 entry) for the full bundle and rationale.

Two limits bound what the cited 2026-08-27 pass can ratify. Ordinary continuity deliberately keeps `MeasuredRpo = 0s` when all committed data survives; those safety reports are not RPO proof. DW-52 adds a separate `controlled-loss-path` evidence job, not a third `ContinuityDrillScenarios` member: it rejects one known sandbox notification, proves that candidate absent, surrounds it with retained EventStore envelopes, and derives a positive RPO only from their persisted UTC timestamps. The gate independently recomputes that duration and accepts the RPO channel only when `0 < rpo <= RecoveryTargets.MaxRpo`. The mechanism is implemented and locally verified, but no hosted DW-52 artifact is cited here, so the 15-minute RPO remains provisional. Separately, the lane's 180-second restoration ceiling remains two orders of magnitude below the 4-hour RTO, so a pass proves recovery within the ceiling, not within the target.

### Tenant isolation by construction (NFR9a/NFR59)

Recovery is isolated **because the drill runs under a test tenant** resolved by the single authoritative `ReplayTenantPolicy.IsTestTenant` predicate, and every durable store is tenant-partitioned. The coordinator **asserts** (fails closed to `unmeasurable`) when the target is not a test tenant, so no production-tenant durable state is mutated. There is **no** "drill flag on a production tenant".

## Deferrals (inert-control-floor honesty)

Fully built **and tested** against a scripted fake runner: `RecoveryTargets`, the token sets, the pure `ContinuityDrillEvaluator`, the `ContinuityDrillReport`/`Unmeasurable` factory, the `ContinuityDrillCoordinator` (run + sweep + fail-closed audit-then-alert), the `ContinuityDrillOutcome` gate, the new `OperatorAlertKind`, and the `AuditEnvelopeFactory` envelope.

**Story 12.15 retirement (partial).** The Aspire/DAPR live runner and the CI/release evidence gate (release: per-commit non-cancelling concurrency) execute both mandatory scenarios through `RunAllScenariosAsync`. **Retired:** the live fault-injection deferral itself — the EventStore path uses an allowlisted real resource stop/start, and the subscription path faults and renews a topology-composed Worker/provider boundary with independent DAPR sentinel and EventStore actor-state reads. DW-52 also retires the missing *mechanism* for a non-vacuous RPO measurement through its separate controlled-loss report and gate. **Not retired:** hosted proof from that new channel and any claim about the 4-hour RTO (beyond the lane's 180-second ceiling). The scheduler deferral is retired only for the scheduled and manually dispatched lanes and the release lane — ordinary `push`/`pull_request` CI runs no recovery validation. `RV-EXT-M365` is retained because the subscription boundary is not external Graph; product DI deliberately remains inert, and production AKS/multi-replica control remains `RV-PROD-CONTROL`. Those safety/residual boundaries are not live-driver deferrals.

## Consequences

- The RPO/RTO targets now have a **provable harness** and live fault injection at a real resource boundary, while A10 ratification still requires a retained hosted evidence artifact — and, for the 4-hour RTO specifically, a lane whose restoration budget can reach it.
- A CI/release gate can assert `Unmeasurable == 0` (drills produced evidence) independently of `Missed == 0` (targets met).
- Backward-compatible: adding a coordinator + an `OperatorAlertKind` member + an envelope factory method keeps all existing Epic 1–9 audit, gateway, conformance, and architecture tests green; the scenario/verdict tokens avoid the legacy-lifecycle literals so no `ScaffoldArchitectureTests` allowlist entry is required.

## References

- Story 9.4 — `9-4-replay-and-simulation-isolation.md` (the test-tenant + deferred-driver + probe-coordinator template).
- Story 9.5 — `9-5-derived-store-cross-tenant-isolation.md` (the closest coordinator twin).
- Story 9.2 — `9-2-audit-completeness-as-a-production-observable.md` (the measurement + fail-safe twin).
- Story 9.12 — projection-rebuild validation (the deep rebuild-equivalence proof this story defers to).
- Story 9.13 — scoped-outage degradation.
- `architecture.md` (recovery framing, two-phase audit / fail-open-then-reconcile, M2 continuity drill); `epics.md` (Story 9.11, NFR56, A10).
