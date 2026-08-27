# ADR: Projection rebuild validation (Story 9.12)

- **Status:** Accepted
- **Epic / Story:** Epic 9 — Story 9.12 (Projection rebuild validation)
- **Drivers:** NFR57 (derived projections rebuildable from immutable source records + audit history within the 4-hr target, without mailbox re-ingestion, and deterministically equivalent to the pre-rebuild projection), architecture invariant #11 (derived-state versioning & deterministic replay), NFR49a (evidence-snapshot reproducibility), NFR9a (tenant isolation by construction), NFR59 (no cross-tenant leakage / no unauthorized mutation during recovery), D4 (two-phase audit, fail-open-then-reconcile), Epic 8/9 no-fabrication doctrine, NFR2/NFR42/NFR45 (metadata-only / no-leak floor).

## Context

NFR57 asserts that the ChatBot's **derived state can be recovered deterministically** from its immutable source-of-record — not from a mailbox re-ingestion — within the recovery-time budget. Until now that is an assumed property. Story 9.12 makes **derived-state recovery provable rather than assumed**: it drives a rebuild of a test tenant's derived projections from its immutable source records + WORM audit history, proves the rebuilt projection is **deterministically equivalent** to the pre-rebuild projection, checks the measured rebuild duration against the single-source 4-hr target, and produces a metadata-only evidence artifact the NFR57 validation is anchored to.

This is the **second of the three Epic 9 recovery/continuity-validation stories** (9.11 drill → **9.12 projection-rebuild validation** → 9.13 scoped-outage degradation). Story 9.11's data-loss check explicitly **deferred the deep projection-rebuild-equivalence proof to this story** — 9.11 owns the RPO/RTO measurement; 9.12 owns the projection-level structural equivalence.

## Decision

Build the validation as a **validation-harness**, modeled line-for-line on the Story 9.11 `ContinuityDrillCoordinator` (which in turn follows 9.1/9.2/9.4/9.5): a **pure evaluator + an injectable coordinator + a structured outcome + fail-closed audit-then-deliver + a metadata-only report**, all `internal` to `.Server/Audit/`. It is explicitly **NOT** a governed-command story (no `IChatBotCommand`, no `ChatBotSpineCommandAllowlist` entry, no `ParticipantAuthorizationStage` gate, no `.Contracts` change).

### Components

- **`RecoveryTargets.MaxRto`** — reused as the **single source of truth** for the 4-hr rebuild-duration target. NFR57's "rebuild within the 4-hr target" bound is the same 4-hr recovery time (Story 9.11), so the coordinator compares the measured rebuild duration against `RecoveryTargets.MaxRto`; a one-line XML-doc cross-reference records the NFR57 consumption. The `4`/`FromHours(4)` literal is **never re-typed** for this story (grep-confirmed).
- **`ProjectionRebuildVerdicts`** — the closed set `{ equivalent, divergent, unmeasurable }`, mirroring `ContinuityDrillVerdicts`. The literals deliberately avoid the legacy-lifecycle tokens, so no `ScaffoldArchitectureTests` allowlist entry is required.
- **`ProjectionResourceDigest`** — the metadata-only per-resource structural digest (`ResourceId`, `StructuralStateToken`) the evaluator diffs. `Create(...)` sanitizes both fields via `AuditMetadata.SafeOptionalToken` (mirrors `DerivedStoreEntry.Create`). The `StructuralStateToken` is the **same structural-digest discipline** as `AuditOperationReconstructor.ReconstructedOperationState.ResultingStateToken` and the `GovernedOperationView` structural fields (`SchemaVersion`, `SourceProvenance`, `DerivationKernelVersion`, `RedactionState`, `RetentionClass`, `SourceVersion`) — never raw item content.
- **`ProjectionRebuildEquivalenceEvaluator`** — a **pure, deterministic** function: `equivalent` iff the two projection schema versions are ordinally equal, both snapshots cover the same resource-key set, and every per-resource `StructuralStateToken` matches; else `divergent`. Plus a deterministic `FirstDivergingResourceLocator` (in the pre-rebuild snapshot's stable order) and a stable bounded `Deviations` list (`projection_diverged` / `rebuild_duration_exceeded`). No clock, no IO; comparison is order-independent on the resource-key set.
- **`ProjectionRebuildReport`** — the metadata-only NFR57 **validation-evidence artifact** (tenant ref, dataset ref, start/end, measured rebuild duration, `DurationWithinTarget`, verdict, resources-compared count, bounded deviations, first-diverging locator, projection schema version, correlation id, reason code). Modeled on `ContinuityDrillReport` with a fail-safe `Unmeasurable(...)` factory. `IsDivergent` is the serious determinism breach; `IsBreach` folds all three fail-closed dimensions (divergent **or** unmeasurable **or** duration overrun).
- **`ProjectionRebuildMeasurement`** — what the driver returns (wall-clock bounds, measured duration, pre-rebuild + rebuilt structural snapshots, two stamped schema versions).
- **`ProjectionRebuildOutcome`** — the structured sweep result (`TenantsValidated`, `Equivalent`, `Divergent`, `DurationExceeded`, `Unmeasurable`, `Alerted`) a CI/release gate asserts against.
- **`IProjectionRebuildDriver`** — the seam the coordinator consumes for the snapshots + measured duration. Product DI retains the inert `DeferredProjectionRebuildDriver`; Story 12.15 provides the separately constructed Tier-3 live rebuild driver (store round-trip real, equivalence verdict re-opened — see below).
- **`ProjectionRebuildValidationCoordinator`** — run-validation + sweep, test-tenant-by-construction guard, fail-closed audit-then-alert, deviation/first-diverging recording. Modeled directly on `ContinuityDrillCoordinator`.
- **`OperatorAlertKind.ProjectionRebuildValidationFailed`** + **`AuditEnvelopeFactory.ProjectionRebuildValidationFailed`** — the breach alert + the metadata-only pre-commit envelope (integer-second duration, boolean flags, bounded deviation tokens, safe first-diverging locator; Worker origin).

### Rebuilds from the immutable source-of-record, not from mailboxes (the defining NFR57 property)

The rebuild input is (a) the tenant's **immutable source records** (`ProjectConversationSourceEmailView` — the retained `source-email-metadata` data class, Story 9.7) and (b) the tenant's **WORM audit history** (`IWormAuditStore.EnumerateChain`, the D4 source of truth). It does **not** call Graph, does **not** re-fetch any mailbox, and does **not** re-query *current* upstream Party/Folder/sibling-context data — that **as-of** resolution is what invariant #11 requires, and re-querying current data would make the rebuild diverge from the original as-of state. The seam contract documents this; the test fake honors it.

### Determinism is the core proof (invariant #11 — derived-state versioning & deterministic replay)

Equivalence is proven by a **structural-snapshot diff + schema-version match**, the projection-level generalization of Story 9.2's reconstruct-and-diff discipline. The rebuild stamps the **projection schema version** in its output snapshot; a schema-version mismatch between the pre-rebuild and rebuilt snapshots is **divergence** (the event-upcasting / schema-churn failure mode). A `divergent` verdict means the rebuild is non-deterministic — it makes evidence snapshots / approval records non-reproducible, the NFR49a breach.

### Three distinct breach dimensions (kept distinct in the outcome)

- **`divergent`** — a non-deterministic rebuild. The **serious** breach: it undermines NFR49a evidence reproducibility (invariant #11), analogous to a 9.4/9.5 isolation breach. A release gate that cares about determinism asserts `Divergent == 0`.
- **`DurationWithinTarget == false`** — a recovery-time **miss**, analogous to the Story 9.11 RPO/RTO miss: a recorded deviation that is a recalibration / follow-up signal, **not** by itself a determinism failure. A deterministic-but-slow rebuild is `equivalent` with `DurationWithinTarget == false`.
- **`unmeasurable`** — the **fail-safe** breach (no evidence produced).

`ProjectionRebuildOutcome` keeps `Equivalent` / `Divergent` / `DurationExceeded` / `Unmeasurable` distinct so a gate asserts the dimension it cares about (e.g. `Divergent == 0 && Unmeasurable == 0` ⇒ rebuilds are deterministic and produced evidence). Never collapse `divergent` or `unmeasurable` into `equivalent`, and never let a duration overrun silently pass.

### Fail-safe over fabrication (Epic 8/9 no-fabrication doctrine)

A validation that **cannot complete** — the driver throws, the rebuild never finishes, or the snapshots/durations are unavailable — yields `unmeasurable` (a breach signal that fail-closed-audits-then-alerts), never a fabricated `equivalent`/within-target. Mirrors `AuditCompletenessMeasurement.Unmeasurable`, `ContinuityDrillReport.Unmeasurable`, and `ReplayIsolationStatus.Unknown`.

### Tenant isolation by construction (NFR9a/NFR59)

The rebuild is isolated **because the validation runs under a test tenant** resolved by the single authoritative `ReplayTenantPolicy.IsTestTenant` predicate, and every durable store (`IWormAuditStore.EnumerateChain(tenantId)`, the source-record projection, the derived/projection store) is tenant-partitioned. The coordinator **fails closed to `unmeasurable` without invoking the driver** when the target is not a test tenant, so no production-tenant durable projection state is mutated. There is **no** "rebuild flag on a production tenant".

### Read-mostly / out-of-band (D4, two-phase audit, NFR15a)

The validation rebuilds out-of-band and reads the WORM chain / source records; it adds **no** commit-time gate and never mutates the audit chain. Its only writes are (i) into the test tenant's projection partition during the rebuild exercise, and (ii) the fail-closed `ProjectionRebuildValidationFailed` pre-commit audit envelope on a breach. The validation proves the D4 "chain/state rebuilt from the log on recovery" property — it does not weaken it.

## Deferrals (inert-control-floor honesty)

Fully built **and tested** against a scripted fake driver: `ProjectionRebuildVerdicts`, `ProjectionResourceDigest`, the pure `ProjectionRebuildEquivalenceEvaluator`, the `ProjectionRebuildReport`/`Unmeasurable` factory + `ProjectionRebuildMeasurement` + `ProjectionRebuildOutcome`, the `ProjectionRebuildValidationCoordinator` (run + sweep + fail-closed audit-then-alert), the new `OperatorAlertKind`, and the `AuditEnvelopeFactory` envelope.

**Story 12.15 implementation update (partial retirement).** The Tier-3 driver reads a separately seeded persisted baseline, writes and reads back a distinct DAPR partition through the production projection-store abstractions, and performs ETag cleanup. **Retired:** the "no real store round-trip" half of the deferral, and source-email rebuild through the real `AssociationProjectionHandler` (reconstructed captured events — not an identity copy of the projected view). **Not retired:** governed/WORM projections remain identity-written (`RV-REBUILD-WORM`); full NFR57 coverage for audit-derived projections is residual. A measured rebuild duration is now published from a retained hosted locator — release run `33066358280` (commit `17aa94d`, evidence `01M11EYSDMP1ZF38B7KZA1A6FA`, 2026-08-27) reports `equivalent`, 1 resource compared, duration 0.012s against the 14400s NFR57 target. The lane's 180-second restoration ceiling also sits far below the 4-hour NFR57 target (`RV-MEASURABLE-CEILING`), so no run from it can demonstrate a duration miss of the 4-hour budget, and this run does not discharge NFR57.

The CI/release gate (release: per-commit non-cancelling concurrency) invokes `RunAllAsync` on the scheduled, manually dispatched and release lanes only — ordinary `push`/`pull_request` CI runs no recovery validation. Product DI deliberately remains inert. Durable production WORM and production-volume evidence remain `RV-DURABLE-WORM` / `RV-PROVIDER-SCALE`, and the ceiling gap is `RV-MEASURABLE-CEILING`.

## Consequences

- Derived-state recovery is now backed by a **provable harness** rather than an unvalidated assumption; the NFR57 validation has a concrete evidence artifact (dataset + duration-vs-target + diff result).
- A CI/release gate can assert `Divergent == 0 && Unmeasurable == 0` (deterministic rebuilds that produced evidence) independently of `DurationExceeded == 0` (within the 4-hr target).
- Backward-compatible: adding a coordinator + an `OperatorAlertKind` member + an envelope factory method keeps all existing Epic 1–9 audit, gateway, conformance, and architecture tests green; the verdict tokens avoid the legacy-lifecycle literals so no `ScaffoldArchitectureTests` allowlist entry is required.

## References

- Story 9.11 — `9-11-continuity-drill-and-rpo-rto-validation.md` (the closest coordinator twin and the `RecoveryTargets` owner; the drill's data-loss check defers the deep rebuild-equivalence proof to this story).
- Story 9.4 — `9-4-replay-and-simulation-isolation.md` (the test-tenant + deferred-driver template).
- Story 9.2 — `9-2-audit-completeness-as-a-production-observable.md` (the reconstruct-and-diff structural-digest twin).
- Story 9.5 — `9-5-derived-store-cross-tenant-isolation.md` (the audit-then-deliver + sweep template).
- Story 9.13 — scoped-outage degradation.
- `architecture.md` (recovery framing, rebuild-and-diff completeness, invariant #11 derived-state versioning & deterministic replay, two-phase audit / fail-open-then-reconcile, Workers [M2] projection rebuild); `epics.md` (Story 9.12, NFR57).
