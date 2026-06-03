# ADR: Audit completeness as a production observable — reconstructability, not field presence

## Status

Accepted (realized by Story 9.2, NFR50a / FR95a). Builds directly on the Story 9.1 WORM chain
([worm-audit-backing.md](worm-audit-backing.md)) and the Epic 8 observability spine.

## Context

The architecture defines the D4 completeness pillar precisely: *"Completeness (NFR50a) = reconstructability, verified by
a scheduled production assertion that rebuilds state from the log and diffs the projection."* This is a **stronger**
test than NFR50 (already shipped), which only verifies **100% required-field presence** for security-sensitive events on
a validation dataset. Field presence is necessary but **not sufficient**: a record can have every field populated and
still fail to let you rebuild the operation's resulting state, or disagree with what the system actually projected.

The risk this ADR guards against is a future maintainer quietly downgrading "can we *reconstruct* the operation?" to
"are the fields *present*?" — which would make "complete audit" an assumption again, not a measured fact.

Three further constraints shape the design:

- **FR95a (replay isolation):** replay/simulation events must be excluded from both the numerator and the denominator
  of the completeness fraction. Replay *execution* lands in Story 9.4; Story 9.2 must make the exclusion **real and
  testable now** so 9.4 needs no retrofit.
- **D4 two-phase audit:** the post-commit WORM chain is fail-open-then-reconcile. The completeness measure must stay an
  **out-of-band, read-only observable** — never a commit-time gate — or it re-introduces the NFR15a × NFR49a tension the
  two-phase model resolves.
- **Epic 8 no-fabrication spine:** a measurement that cannot complete must report `Unknown`/breach, never a fabricated
  100%.

## Decision

### Completeness = reconstructability (rebuild-from-log + diff-projection), NOT field presence

- `AuditOperationReconstructor` is a pure, deterministic per-operation evaluator. Field presence (reusing the existing
  `AuditMetadata` discipline) is checked only as a **precondition**; the evaluator then goes further — it maps the
  envelope to its NFR15a path and **assembles the operation's resulting end-state** (resource, decision, transition,
  outcome, and a structural projection token). An operation with all fields present but no assemblable end-state, an
  unknown path, or (in the measurer) a projection that diverges is `NotReconstructable`. The distinction is documented
  inline in the reconstructor and locked by a test (`AllFieldsPresentButNoMatchingProjectionIsNotReconstructable`).
- `AuditCompletenessMeasurer` is the **scheduled production assertion**: per tenant, over a rolling **7-day** window, it
  enumerates the tenant's WORM chain (`IWormAuditStore.EnumerateChain`), groups envelopes into operations (by resource
  ref + correlation), runs the reconstructor on each, and **diffs the rebuilt state against the live governed-operation
  projection** (`IGovernedOperationProjectionStore`, read-only). A missing/short chain, an unmapped path, or a
  projection that is absent or structurally divergent counts the operation as **not reconstructable**. The fraction is
  `reconstructable ÷ total`.

### Denominator = the NFR15a path inventory, consumed by reference

`ChatBotAuditPathMap` maps each envelope to one of the eleven `ChatBotStateWritingPathInventory.Paths` via the
`CommandName` the `AuditEnvelopeFactory` stamps on every record. The path list is **never re-listed** — it is consumed
by reference. An envelope that maps to **no** known path — including the system-emitted observability/alert records,
which are not NFR15a state-writing operations — is surfaced as a completeness gap (`unmapped_path`), never silently
dropped from the denominator, so a path that stops emitting cannot masquerade as "100% complete".

### Per-tenant rolling-7-day window; 99.5% → P1; fail-safe Unknown

- `AuditCompletenessBudgetEvaluator` is a pure, fail-safe map mirroring `ErrorBudgetBurnEvaluator`: measurable and
  ≥ 99.5% → `WithinBudget`; measurable and < 99.5% → `Exhausted` (a **P1** breach); unmeasurable → `Unknown` (never a
  fabricated within-budget). It maps an already-computed fraction — no percentile/count math inside the evaluator.
- A run that **cannot complete** (chain/projection unavailable, enumeration/diff throws) returns
  `AuditCompletenessMeasurement.Unmeasurable` — a breach, never a fabricated 1.0 — exactly as
  `AuditChainVerificationCoordinator` treats an incomplete verification. A completed window with **zero** in-scope
  operations is genuinely vacuously complete (1.0), which is distinct from "cannot complete" and must not page anyone.

### Observable gauge + audit-then-deliver P1 alert

- `chatbot.audit.completeness` is an OpenTelemetry **observable gauge** on the existing `ChatBotMetrics` meter, derived
  read-only at collection time from `IAuditCompletenessSource`, emitting **no measurement** when a tenant's fraction is
  unmeasurable (fail-safe), exception-isolated, with a low-cardinality `tenant` tag only (the fraction is the value,
  never a dimension). The default source (`UnavailableAuditCompletenessSource`) publishes nothing until a periodic sweep
  feeds it.
- `AuditCompletenessAlertCoordinator` mirrors `AuditChainVerificationCoordinator`: on a breach (Exhausted **or**
  Unknown) it writes the metadata-only `AuditEnvelopeFactory.AuditCompletenessBudgetBreached` **pre-commit** envelope,
  then emits exactly one `OperatorAlertKind.AuditCompletenessBudgetBreached` operator alert. The alert/payload encodes
  **P1 severity explicitly** (`audit-completeness-severity:p1`). Fail-closed: if the pre-commit audit is unavailable, no
  alert is delivered; an unmeasurable tenant still alerts (breach, never silence).

### Replay exclusion (FR95a) — real and testable now

- `AuditEnvelope` gains a nullable `ReplayRunId` (default `null`). `AuditReplayExclusion.IsReplayEnvelope` is the
  exclusion predicate; the measurer removes replay envelopes **before** grouping, so they count toward neither term.
- The marker is **security-relevant** — a replay record masquerading as production must be tamper-evident — so it is
  folded into the canonical hash. `WormAuditChainHasher.CanonicalSerializationVersion` was **bumped from v1 to v2** and
  canonicalization is **version-aware**: v1 reproduces the original Story 9.1 field set (no `ReplayRunId`) and the
  verifier re-hashes each record under the version it was stamped with, so pre-9.2 chains stay verifiable byte-for-byte.
  The bump is deliberate — silently changing the canonical form would have invalidated every Story 9.1 chain.
- Story 9.2 introduces only the field, the hash coverage, and the exclusion. Story 9.4 owns **populating** `ReplayRunId`
  during replay runs against the test tenant. Today there are zero replay events in production, so the exclusion holds
  by construction; the test `ReplayEnvelopesAreExcludedFromBothNumeratorAndDenominator` proves it is nonetheless real.

### Metadata-only / no-leak floor (NFR2/NFR42) and tenant isolation (NFR9a)

Every measurement result, gauge tag, alert payload, and diff locator carries only `AuditMetadata`-safe bounded tokens
(the reconstruction diffs **structural** state tokens — transition, outcome, decision, resource/policy refs — never raw
item content). Each tenant is measured over its own chain and its own projection; the projection read passes the tenant
ref, so no measurement observes or links another tenant's records.

## Consequences

- "Complete audit" becomes a measured production observable, not an assumption: a per-tenant gauge plus a fail-closed P1
  incident when reconstructability drops below 99.5% or cannot be measured.
- The Story 9.1 chain is now consumed as the reconstruction source of truth; the v1→v2 canonical bump is a clean,
  version-aware change that leaves existing chains verifiable.
- **The periodic scheduler is deferred** (consistent with the Epic 7/8/9.1 inert-control-floor pattern): the
  reconstructor, measurer, budget evaluator, gauge, and alert coordinator are **fully built and tested**, but no
  always-on `BackgroundService` / Dapr-timer is wired. A periodic runtime need only call
  `AuditCompletenessAlertCoordinator.MeasureAllTenantsAndAlertAsync` on its cadence and publish the
  `AuditCompletenessMeasurer.MeasureAllTenantsAsync` sweep into `IAuditCompletenessSource`. This deferral is **explicit,
  not a silent skip** — see the story Completion Notes.

## Alternatives Considered

- **Measure field presence and call it completeness.** Rejected: that is NFR50 (already shipped), strictly weaker than
  NFR50a. Reconstructability — rebuild-from-log + diff-projection — is the architecture's literal definition.
- **Make the measurement a commit-time gate.** Rejected: it would turn an out-of-band observable into a fail-closed
  dependency on the commit path, re-creating the NFR15a × NFR49a tension the two-phase audit model resolves.
- **Add `ReplayRunId` without bumping the canonical version.** Rejected: silently changing the canonical form would
  invalidate every Story 9.1 chain. The version bump + version-aware canonicalization keeps old chains verifiable.
- **Report 100% when a run cannot complete.** Rejected: violates the Epic 8 no-fabrication doctrine — an unmeasurable
  run is a breach signal (`Unknown`), never a fabricated pass.

## Verification

- `AuditOperationReconstructorTests` — complete mapped chain reconstructs; unmapped command is a gap; missing
  outcome/field is not reconstructable; empty operation is chain-missing; all eleven inventory paths are mapped.
- `AuditCompletenessMeasurerTests` — all-fields-present-but-no-projection is not reconstructable (reconstructability ≠
  field presence); structural projection mismatch diverges; unmeasurable/projection-failure fails closed to a breach;
  empty window is vacuously complete; replay excluded from both terms; per-tenant isolation; read-only (no append).
- `AuditCompletenessBudgetEvaluatorTests` — fraction maps across the 99.5% threshold; unmeasurable → Unknown.
- `AuditCompletenessAlertCoordinatorTests` — below-target audits-then-emits exactly one P1 alert; within-budget emits
  nothing; unmeasurable fails closed to a breach alert; fail-closed audit suppresses the alert; sweep counts breaches.
- `ChatBotMetricsTests` — the completeness gauge reflects the measured fraction with a tenant tag only, emits nothing
  when unmeasurable, and swallows + gap-counts a source failure.
- `AuditReplayExclusionTests` — the predicate; the canonical version bump (v2); `ReplayRunId` is covered by the v2 hash
  (two envelopes differing only in the marker hash differently); a v1-stamped record still verifies after the bump.
- `WormAuditLeakTests` — the completeness-breach alert envelope carries no banned markers.
- `WormAuditChainDependencyInjectionTests` — the measurer, alert coordinator, and gauge source all resolve.
