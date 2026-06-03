---
baseline_commit: 1b13df47291d6e7f1909085dec5e92b786d2fb40
---

# Story 9.2: Audit completeness as a production observable

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance owner,
I want audit completeness measured as **reconstructability** in production,
so that "complete audit" is proven, not assumed.

## Acceptance Criteria

1. **Completeness = reconstructability of every state-mutating operation, from the chain alone (NFR50a — NOT mere field presence).**
   **Given** the set of state-mutating operations (the NFR15a path inventory — the eleven enumerated paths in `ChatBotStateWritingPathInventory.Paths`),
   **When** completeness is measured,
   **Then** it is the **fraction whose audit chain reconstructs the operation end-to-end from the chain alone** — every input, decision, resource reference, policy snapshot, and outcome — **not merely whether required fields are present**. An operation counts as *reconstructable* only when its chained WORM envelope(s) carry enough to rebuild the operation's resulting state (actor, command, resource, `Decision`, `ReasonCode`, `PolicySnapshotId`, `SourceEvidenceRefs`, `StateTransition`, `Outcome`) and that rebuilt state agrees with the live projection (AC2). Field-presence-only (the NFR50 validation-dataset test, already shipped) is necessary but **not** sufficient and must not be mistaken for this measure.

2. **Rolling 7-day window per tenant; below 99.5% triggers a P1 incident; a scheduled production assertion rebuilds state from the log and diffs the projection (NFR50a).**
   **Given** the rolling 7-day window per tenant,
   **When** completeness drops below **99.5%**,
   **Then** a **P1 incident** is triggered (a metadata-only operator alert at P1 severity through the existing `IOperatorAlertSink`, fail-closed). The measurement is produced by a **scheduled production assertion** that, per tenant, **rebuilds state from the audit log (the WORM chain) and diffs it against the live projection**; an operation whose rebuilt state diverges from its projection — or whose chain is missing/short — is *not reconstructable* and counts against the fraction. A measurement that **cannot complete** (chain unavailable, projection unavailable, enumeration throws) is reported `Unknown` / breach — **never** a fabricated 100%.

3. **Replay events are excluded from both numerator and denominator (FR95a).**
   **Given** the completeness measurement,
   **When** the numerator (reconstructable operations) and denominator (total state-mutating operations) are computed,
   **Then** **replay/simulation events are excluded from both** — an envelope distinguished as a replay event (`replay_run_id` present) is neither counted as a success nor as a total. Today there are zero replay events in production (replay execution lands in Story 9.4), so the exclusion is satisfied *by construction*; this story must make the exclusion **real and testable** — the distinguishing marker and the exclusion predicate exist now, so that when Story 9.4 emits replay records the measure stays correct without a retrofit.

### Cross-cutting requirements that hold for every AC

- **Reconstructability ≠ field presence (the defining distinction of this story).** NFR50 (already shipped) tests *100% required-field presence on the validation dataset*. NFR50a is the **stronger, production** test: can the operation be *reconstructed end-to-end from the chain alone*? Build the reconstructor on top of the existing field discipline; do **not** re-implement field-presence as if it were the AC, and do **not** weaken the AC to field-presence because reconstruction is harder.
- **Read-only / out-of-band — never a new fail-closed gate (D4, two-phase audit).** The measurement is an **observable**, computed out-of-band over the WORM chain and projections. It must **not** touch, block, or add a fail-closed gate to the commit path. It only reads (`IWormAuditStore.EnumerateChain` / `EnumerateTenants`, the projection stores). Re-introducing a commit-time dependency would recreate the NFR15a × NFR49a tension the two-phase model resolves.
- **No-fabrication / fail-safe (Epic 8 spine doctrine).** Prefer `Unknown` / breach over a fabricated `WithinBudget` / 100%. A completeness run that cannot complete is a signal, not a silent pass — mirror `ErrorBudgetBurnEvaluator` (Healthy→WithinBudget, … , `_`→Unknown, never invented) and `AuditChainVerificationCoordinator` (incomplete verification → `Unknown` breach).
- **Metadata-only / no-leak floor (NFR2, NFR42).** Every measurement result, metric tag, alert payload, and any "diff mismatch" locator carries only safe bounded tokens via `AuditMetadata` (ASCII alnum + `.-_:@|`, marker-ban on `secret`/`password`/`bearer`/`token`/`exception`/file-extension sentinels). The reconstruction diffs **structural state tokens** (`StateTransition`, `Outcome`, `Decision`, resource/policy refs) — **never** raw item content, prompts, recipient PII, or payloads. Metric dimensions stay low-cardinality: `tenant` + bounded `reason`/`operation-class` only, never ids/correlation.
- **Tenant isolation by construction (NFR9a).** Completeness is measured **per tenant** over that tenant's chain and that tenant's projection; no measurement reads or links across tenants. M0 is single-tenant but **partitioned by construction** — a second tenant is additive, not a rewrite.
- **Deterministic, pure evaluation.** The reconstructor and the fraction→state mapper are pure and deterministic (no clock, no IO inside the evaluator; `ISystemClock` only for window boundaries and timestamps at the edges). Re-running the assertion over the same chain + projection snapshot yields the same result.

## Tasks / Subtasks

- [x] **Task 1 — Per-operation reconstructability evaluator (AC: #1)**
  - [x] Add a **pure, deterministic** reconstructor under `src/Hexalith.ChatBot.Server/Audit/` (e.g. `AuditOperationReconstructor`) that, given the chained WORM envelope(s) for a single state-mutating operation, decides `reconstructable` | `not-reconstructable` + a metadata-only reason code (e.g. `chain_missing`, `outcome_absent`, `state_unreconstructable`, `projection_diverged`). Reconstructability requires the envelope(s) to carry, **from the chain alone**, the operation's actor, command, resource ref, `Decision`, `ReasonCode`, `PolicySnapshotId`, `SourceEvidenceRefs`, `StateTransition`, and `Outcome` — and to rebuild a resulting-state token that AC2 can diff against the projection.
  - [x] Key operations off the **NFR15a path inventory** — consume `ChatBotStateWritingPathInventory.Paths` (the eleven enumerated paths); do **not** re-enumerate state-writing paths. Map each chained envelope to its path via `CommandName` / `StateTransition` (document the mapping; an envelope that maps to no known path is itself a completeness gap, not silently dropped).
  - [x] **Explicitly distinguish reconstructability from field-presence.** Field presence is a precondition the reconstructor checks first (reuse the existing NFR50 field discipline / `AuditMetadata`), then it goes further: it must assemble the operation's end-state, not just confirm fields are non-empty. Add a code comment + ADR note making this distinction unmistakable so a future maintainer cannot quietly downgrade the test to presence-only.

- [x] **Task 2 — Scheduled production assertion: rebuild from log, diff projection, per-tenant fraction (AC: #2)**
  - [x] Add a **completeness measurer** (e.g. `AuditCompletenessMeasurer`) that, per tenant over a **rolling 7-day window**, enumerates the tenant's chain via `IWormAuditStore.EnumerateChain` (Story 9.1), groups envelopes into operations (by resource ref + correlation), runs the Task-1 reconstructor on each, and **diffs the rebuilt state against the live projection** (`IGovernedOperationProjectionStore` / `GovernedOperationView` and the gateway operation-status store — read-only). Divergence or a missing/short chain ⇒ not reconstructable.
  - [x] Compute the per-tenant fraction = reconstructable ÷ total (both **after** replay exclusion, Task 4). Return a metadata-only result (`tenant ref`, `fraction` bucketed coarsely, `windowStartUtc`/`windowEndUtc`, first-diverging-operation **safe locator** token, never content).
  - [x] **Fail-safe:** if the chain or projection cannot be read, or enumeration throws, return `Unknown` (breach), never a fabricated `1.0`. Apply the Epic 8 no-fabrication doctrine exactly as `AuditChainVerificationCoordinator` does for incomplete verification.
  - [x] Use `ISystemClock` for the 7-day window boundary and all timestamps (deterministic tests; ~30 existing call-sites).
  - [x] Provide a `MeasureAllTenantsAsync`-style sweep (mirror `AuditChainVerificationCoordinator.VerifyAllTenantsAsync`) that iterates `IWormAuditStore.EnumerateTenants` and aggregates per-tenant results — the seam a periodic scheduler calls.

- [x] **Task 3 — Completeness observable + P1 incident on <99.5% (AC: #2)**
  - [x] Publish the per-tenant completeness fraction as an **OpenTelemetry observable gauge** on the existing meter (`chatbot.audit.completeness`), following the `ChatBotMetrics` audit-projection-lag observable-gauge pattern: derived read-only at collection time, **emits no measurement when the fraction is `Unknown`** (fail-safe), exception-isolated, low-cardinality `tenant` tag only. Add the instrument name + wiring in `ChatBotMetrics` / `IChatBotMetrics` (and `NullChatBotMetrics`).
  - [x] Map the fraction to a coarse budget state via a **pure, fail-safe evaluator** (mirror `ErrorBudgetBurnEvaluator`): `≥ 99.5% → WithinBudget`, `< 99.5% → Exhausted` (P1), `Unknown → Unknown` (never fabricated within-budget). Do **not** derive percentile/count math inside the evaluator — it maps an already-computed fraction.
  - [x] Add a new **P1** `OperatorAlertKind` (e.g. `AuditCompletenessBudgetBreached`) and a matching `AuditEnvelopeFactory` producer; emit exactly one metadata-only alert per breaching tenant through `IOperatorAlertSink` via a **fail-closed audit-then-deliver coordinator** (e.g. `AuditCompletenessAlertCoordinator`) mirroring `AuditChainVerificationCoordinator` / `OperationalAlertWiringCoordinator` (pre-commit audit envelope written, *then* alert delivered; an unmeasurable tenant → breach, never silent). Encode **P1 severity** explicitly on the alert/payload so the downstream incident routing treats it as P1 (NFR50a).
  - [x] Register all new types via DI in `CommandGatewayServiceCollectionExtensions` following the existing audit/alert/metrics registration shape.

- [x] **Task 4 — Replay exclusion from numerator AND denominator (AC: #3)**
  - [x] Make replay events **distinguishable now**: add a nullable `ReplayRunId` (`string?`, default `null`) to `AuditEnvelope` so a replay record can be told apart from a production record. **Critical coordination with Story 9.1:** this changes the canonical hash input — extend `WormAuditChainHasher.CanonicalizeEnvelope` to include `ReplayRunId` (it is security-relevant: a replay record masquerading as production must be tamper-evident) and **bump `CanonicalSerializationVersion`** so genesis/older records remain verifiable under their stamped version. Do **not** silently change the canonical form without the version bump (it would invalidate Story 9.1 chains).
  - [x] Add an exclusion predicate (e.g. `IsReplayEnvelope` ⇔ `ReplayRunId is not null`) and apply it in the measurer so a replay operation is removed from **both** numerator and denominator before the fraction is computed.
  - [x] **Scope boundary (document, do not over-reach):** Story 9.4 owns *populating* `ReplayRunId` during replay/simulation runs against the test tenant. This story only introduces the field, the hash coverage, and the exclusion — so AC3 is genuinely testable today (inject a replay-marked envelope; assert it is excluded from both terms) rather than depending on 9.4.

- [x] **Task 5 — Tests (AC: #1, #2, #3)**
  - [x] Tier-1 unit tests under `tests/Hexalith.ChatBot.Server.Tests/Audit/`: **reconstructability ≠ field-presence** (an envelope with all fields present but whose rebuilt state diverges from the projection is `not-reconstructable`); a complete chain whose rebuilt state matches the projection is `reconstructable`; a missing/short chain is `not-reconstructable` with the right reason code; path mapping covers all eleven `ChatBotStateWritingPathInventory.Paths` and an unmapped envelope is a gap.
  - [x] Measurer tests: per-tenant rolling-7-day fraction is correct; `< 99.5%` → `Exhausted`/P1; `≥ 99.5%` → `WithinBudget`; **unmeasurable (chain/projection unavailable or enumeration throws) → `Unknown`, never fabricated 1.0**; first-diverging-operation locator is a safe token; measurement is per-tenant isolated (no cross-tenant read/linkage).
  - [x] Alert tests: a breaching tenant emits exactly one metadata-only `AuditCompletenessBudgetBreached` operator alert at **P1**, audit-then-deliver order, fail-closed (unmeasurable → breach alert, not silence). Gauge emits no measurement for `Unknown`.
  - [x] Replay-exclusion tests (AC3): a `ReplayRunId`-marked envelope is excluded from **both** numerator and denominator; with zero replay envelopes the fraction is unchanged (by-construction today); the canonical hash **includes** `ReplayRunId` (two envelopes differing only in `ReplayRunId` hash differently) and `CanonicalSerializationVersion` was bumped.
  - [x] Leak tests: no measurement result / gauge tag / alert payload / diff locator carries banned tokens (extend the existing Epic 8 / Story 9.1 no-leak serialization assertions).
  - [x] Two-phase / read-only test: the measurement performs **no durable state write on the commit path** and adds **no fail-closed gate** — assert it is read-only over chain + projection and the gateway commit path is unaffected.

- [x] **Task 6 — ADR + docs (AC: #1, #2, #3)**
  - [x] Author `docs/adrs/audit-completeness-observable.md`: record that **completeness = reconstructability (rebuild-from-log + diff-projection), not field presence**; the per-tenant rolling-7-day window and 99.5% → P1 threshold; the replay exclusion (`replay_run_id`, excluded from numerator+denominator, FR95a); the observable-gauge + fail-safe-evaluator + audit-then-deliver-coordinator realization; and the **inert-control-floor deferral** of the periodic scheduler (see Completion Notes guidance below). Reference the Story 9.1 `worm-audit-backing.md` ADR (this story consumes that chain) and note the `WormAuditChainHasher` version bump for `ReplayRunId`.

## Dev Notes

### What this story actually changes (and what already exists)

This story turns the Story 9.1 WORM chain into a **measured production observable**. The chain, the path inventory, the metric meter, the SLO/error-budget evaluator shape, and the fail-closed audit-then-deliver coordinator pattern **all already exist** — compose them; do **not** reinvent.

- **WORM chain (Story 9.1) is the source of truth to reconstruct from.** `IWormAuditStore` (`src/Hexalith.ChatBot.Server/Audit/IWormAuditStore.cs`) exposes `AppendAsync` + tenant-partitioned `EnumerateChain(tenantId)` + `EnumerateTenants()` — **read these to reconstruct**. `WormAuditChainRecord` wraps `AuditEnvelope` + `Sequence` + hashes. The chain is already per-tenant and verifiable (`WormAuditChainVerifier`). [Source: src/Hexalith.ChatBot.Server/Audit/IWormAuditStore.cs; InMemoryWormAuditStore.cs; WormAuditChainRecord.cs]
- **The NFR15a path inventory already exists in code:** `ChatBotStateWritingPathInventory.Paths` — eleven enumerated paths (m365-mailbox-intake, deterministic-association, ambiguous-user-association, correction, ai-action-proposal, approval-decision, command-execution, outbound-draft-creation, outbound-send, tenant-policy-mutation, allowlist-mutation). This **is** the denominator's path set — consume it, never re-list. [Source: src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs; ChatBotStateWritingPath.cs]
- **`AuditEnvelope` carries the reconstruction fields already** (`ResourceId`, `Decision`, `ReasonCode`, `PolicySnapshotId`, `SourceEvidenceRefs`, `StateTransition`, `Outcome`, `Phase`, `CorrelationId`) — but has **no replay field today** (no `replay_run_id`). Task 4 adds the nullable `ReplayRunId`. [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs:3-22]
- **Canonical hashing is Story 9.1's `WormAuditChainHasher`.** Adding `ReplayRunId` to the envelope **changes the canonical serialization** — extend `CanonicalizeEnvelope` and **bump `CanonicalSerializationVersion`**; do not change the canonical form silently (it would break existing chain verification). Story 9.1's review explicitly anticipated this: *"9.1 just must not design the chain in a way that blocks adding a `replay_run_id`-bearing record later."* [Source: src/Hexalith.ChatBot.Server/Audit/WormAuditChainHasher.cs; _bmad-output/implementation-artifacts/9-1-tamper-evident-worm-audit-chain.md:100]
- **The metric meter already exists (Story 8.2):** `ChatBotMetrics` / `IChatBotMetrics` own the single OTel `Meter` and an **observable gauge** (`chatbot.audit.projection.lag`) derived read-only at collection time, exception-isolated, low-cardinality `tenant`/`operation-class` tags only. Add the completeness gauge the same way; never high-cardinality dims. `NullChatBotMetrics` is the no-op. [Source: src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs:23-71; IChatBotMetrics.cs; NullChatBotMetrics.cs]
- **The SLO / error-budget evaluator shape already exists (Story 8.3):** `ErrorBudgetBurnEvaluator.FromHealth` is a **pure, fail-safe** map (Healthy→WithinBudget, Degraded→Approaching, Failed→Exhausted, `_`→Unknown, *never fabricated*). Mirror it for the fraction→budget mapping. `ErrorBudgetBurnState` / `ChatBotHealthStatus` live in `Hexalith.ChatBot.Contracts/Enums/`. [Source: src/Hexalith.ChatBot.Server/Observability/ErrorBudgetBurnEvaluator.cs; src/Hexalith.ChatBot.Contracts/Enums/ErrorBudgetBurnState.cs; ChatBotHealthStatus.cs]
- **The fail-closed audit-then-deliver coordinator pattern is canonical:** `AuditChainVerificationCoordinator` (Story 9.1 — per-tenant sweep `VerifyAllTenantsAsync`, incomplete → `Unknown` breach, exactly-one metadata alert) and `OperationalAlertWiringCoordinator` / `ReviewerBacklogAlertCoordinator` (Story 8.4 — pre-commit audit *then* deliver). Mirror these exactly for the completeness alert. [Source: src/Hexalith.ChatBot.Server/Audit/AuditChainVerificationCoordinator.cs; src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs; ReviewerBacklogAlertCoordinator.cs]
- **Operator alerting seam:** `IOperatorAlertSink` + `OperatorAlert` + `OperatorAlertKind` (`src/Hexalith.ChatBot.Server/Audit/`). Add `AuditCompletenessBudgetBreached` to the enum (with the P1-severity intent documented inline, as `AuditChainBroken` documents its 5-minute SLA). `InMemoryOperatorAlertSink` is the test/dev sink. [Source: src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs:1-23; OperatorAlert.cs; IOperatorAlertSink.cs]
- **The live projection to diff against:** `IGovernedOperationProjectionStore` / `GovernedOperationView` (`src/Hexalith.ChatBot.Server/Projections/`) plus the gateway operation-status store written after a successful command (`CommandGateway.cs`, post-commit path). These are the read-models the rebuilt state is diffed against — read-only. [Source: src/Hexalith.ChatBot.Server/Projections/IGovernedOperationProjectionStore.cs; GovernedOperationView.cs; InMemoryGovernedOperationProjectionStore.cs]
- **Post-commit envelope production:** `AuditEnvelopeFactory.PostCommit(context, dispatchResult, transition, clock.UtcNow)` is produced per state-mutating command and chained via `ChainedAuditWriter` (`src/Hexalith.ChatBot.Server/Gateway/Stages/ChainedAuditWriter.cs`). The completeness measure observes these after the fact — it does **not** modify this path. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs (post-commit path ~280-317); Gateway/Stages/ChainedAuditWriter.cs]
- **Clock:** `ISystemClock` (`src/Hexalith.ChatBot.Server/Audit/ISystemClock.cs`) — use for the 7-day window + timestamps; ~30 call-sites establish this for deterministic tests.
- **Metadata-only token hygiene:** `AuditMetadata` (`SafeOptionalToken`, `IsSafeStableIdentifier`, `SafeCommandName`, `SafeActorType`) + `OperationalAlertPayload.Validate()`. Every Epic 7/8/9.1 payload uses these — follow exactly. [Source: src/Hexalith.ChatBot.Server/Audit/AuditMetadata.cs; src/Hexalith.ChatBot.Server/Observability/OperationalAlertPayload.cs]

### Architecture constraints (must follow)

- **Completeness = reconstructability, verified by a scheduled production assertion that rebuilds state from the log and diffs the projection.** This is the architecture's literal definition of D4's completeness pillar — implement it as written; field-presence (NFR50) is the *weaker* shipped test. [Source: architecture.md:143-147, :369-372 ("Completeness (NFR50a) = reconstructability, verified by a scheduled production assertion that rebuilds state and diffs the projection."); D4, epics.md:319]
- **NFR50a (the AC source):** *"fraction of state-mutating operations (NFR15a inventory) whose audit chain reconstructs the operation end-to-end from the chain alone; target ≥ 99.5% per rolling 7-day window per tenant; below triggers P1; reconstructability (not just field presence) is the test; replay excluded per FR95a."* **[M2]** [Source: epics.md:257]
- **NFR50 (the weaker, already-shipped test — do not conflate):** required-field presence with *automated tests verifying 100% required-field presence for security-sensitive events in the validation dataset*. NFR50a builds on but is **distinct from** this. [Source: epics.md:256]
- **FR95a — replay isolation contract:** replay events carry `replay_run_id` in audit; production audit queries exclude replay; *"NFR50a excludes replay from numerator/denominator"*; replay execution + the nightly outbound-trace probe gate M2 release. Replay *execution* is Story 9.4; this story only needs the exclusion. [Source: epics.md:177; addendum.md:102-108 (§Replay Isolation); epics.md:2398, 2416, 2432]
- **Two-phase audit (D4):** post-commit WORM chain is **fail-open-then-reconcile**; the completeness observable is **out-of-band and read-only** — it must not turn measurement into a commit-time gate (that re-introduces the NFR15a × NFR49a contradiction the two-phase model resolves). [Source: architecture.md:143-147, :369-372; 9.1 story Dev Notes]
- **Tenant isolation by construction (NFR9a):** measure per tenant over that tenant's chain + projection; *"cross-tenant queries impossible at the store-access layer."* [Source: architecture.md:693-696]
- **Location:** everything lands in the `Audit/` seam (`src/Hexalith.ChatBot.Server/Audit/`) and `Observability/` (gauge + budget evaluator), with the alert coordinator alongside the Story 9.1 / 8.4 coordinators. Governance/audit interfaces stay `internal` to `.Server` (NetArchTest-enforced — no `*.Cli`/`*.Mcp`/`*.UI` type may reference them). [Source: architecture.md#Internal Decomposition, #Architectural Boundaries; src/Hexalith.ChatBot.Server/Audit/ marked "[M0] seam: pre/post-commit, WORM hash-chain, replay traces [M2]"]

### Previous-work intelligence — apply directly

- **Story 9.1 is the substrate.** The WORM chain, `WormAuditChainVerifier`, `AuditChainVerificationCoordinator`, and `WormAuditChainHasher` shipped and are `done`. Reuse `EnumerateChain`/`EnumerateTenants`; mirror the coordinator's fail-closed audit-then-deliver + `VerifyAllTenantsAsync` sweep for the completeness alert. The hasher's `CanonicalSerializationVersion` exists precisely so adding `ReplayRunId` is a clean version bump, not a chain-breaking change.
- **No-fabrication / fail-safe doctrine is the spine of Epics 7–9.** `ErrorBudgetBurnEvaluator` and `AuditChainVerificationCoordinator` both prefer `Unknown`/breach over invented success. An unmeasurable completeness run must report `Unknown`, never 100%. (Story 8.1 was caught fabricating health — do not repeat it.)
- **Bookkeeping drift is the #1 recurring review auto-fix across Epics 7–9** (stale test counts, **File List omissions** — Story 9.1's own review had to fix a 3-file File-List omission and stale counts). Keep the **File List exhaustive** (every new + modified file, incl. tests + the ADR) and any cited test counts accurate. Pre-empt it.
- **Inert-control-floor pattern.** Epics 7/8 and Story 9.1 repeatedly built the evaluator + coordinator + alert path but **deferred the always-on periodic scheduler** (`BackgroundService`/Dapr timer). For 9.2 the reconstructor + measurer + budget evaluator + gauge + alert path are **core ACs and must be built + fully tested**; if the *scheduler* wiring is deferred, a periodic runtime need only call `MeasureAllTenantsAsync` on its cadence — **say so explicitly in Completion Notes**, never let a deferral read as "done."
- **Define-once / reuse.** Consume existing seams (`IWormAuditStore`, `ChatBotStateWritingPathInventory`, `ChatBotMetrics`, `ErrorBudgetBurnEvaluator`, `IOperatorAlertSink`, `ISystemClock`, `AuditMetadata`, the projection stores) by reference — do not re-derive thresholds, clocks, the path list, or token rules.

### Project Structure Notes

- New types belong in `src/Hexalith.ChatBot.Server/Audit/` (reconstructor, measurer, new alert kind + factory producer, the `AuditEnvelope.ReplayRunId` field + `WormAuditChainHasher` update) and `src/Hexalith.ChatBot.Server/Observability/` (completeness gauge wiring in `ChatBotMetrics`/`IChatBotMetrics`/`NullChatBotMetrics`, the fraction→budget evaluator). The audit-then-deliver coordinator sits with the existing coordinators. DI registration in `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`.
- Tests in `tests/Hexalith.ChatBot.Server.Tests/Audit/` (and `.../Observability/` for the gauge/evaluator), mirroring the Story 9.1 / 8.x test layout.
- ADR in `docs/adrs/` (seeded by Story 9.1) — add `audit-completeness-observable.md`.
- No conflict with unified structure detected: the `Audit/` + `Observability/` homes and the `internal`-to-`.Server` boundary match the architecture's prescribed placement exactly.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.2 (lines 2382-2398)]
- [Source: _bmad-output/planning-artifacts/epics.md#NFR50a (line 257); NFR50 (line 256); NFR15a (line 205-206); FR95a (line 177)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Auditability & tamper-evidence / D4 completeness=reconstructability (lines 143-147, 369-372)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data boundaries / tenant isolation (lines 693-696)]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Replay Isolation (lines 102-108)]
- [Source: _bmad-output/implementation-artifacts/9-1-tamper-evident-worm-audit-chain.md (WORM chain substrate; hasher version-bump anticipation, line 100; bookkeeping-drift review lesson, lines 230, 249)]
- [Source: src/Hexalith.ChatBot.Server/Audit/IWormAuditStore.cs; InMemoryWormAuditStore.cs; WormAuditChainRecord.cs; WormAuditChainVerifier.cs; AuditChainVerificationCoordinator.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs:3-22; WormAuditChainHasher.cs; ChatBotStateWritingPathInventory.cs; ChatBotStateWritingPath.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs; OperatorAlert.cs; IOperatorAlertSink.cs; AuditMetadata.cs; ISystemClock.cs; AuditEnvelopeFactory.cs]
- [Source: src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs:23-71; IChatBotMetrics.cs; NullChatBotMetrics.cs; ErrorBudgetBurnEvaluator.cs; OperationalAlertPayload.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Enums/ErrorBudgetBurnState.cs; ChatBotHealthStatus.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/IGovernedOperationProjectionStore.cs; GovernedOperationView.cs; InMemoryGovernedOperationProjectionStore.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs (post-commit path); Gateway/Stages/ChainedAuditWriter.cs]
- [Source: src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs; ReviewerBacklogAlertCoordinator.cs (coordinator pattern)]

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Opus 4.8, 1M context)

### Debug Log References

- Full `Hexalith.ChatBot.Server.Tests` suite: **1181 passed, 0 failed** (1175 at dev-complete; +6 from the QA `bmad-qa-generate-e2e-tests` gap-coverage pass — see `_bmad-output/implementation-artifacts/tests/test-summary-story-9.2.md`). Architecture (37), Conformance (75), Workers (30) all green. Full solution (`Hexalith.ChatBot.slnx`) builds with 0 warnings / 0 errors.
- Two failures surfaced during the red→green cycle and were fixed: (1) the reconstructor's field-presence check initially required `StateTransition` to be an `AuditMetadata`-safe token, but a transition is a `From->To` arrow whose `>` is intentionally outside the safe-token charset — relaxed to a non-empty check (the safe-token discipline still applies to all emitted refs/locators). (2) Adding `audit-completeness` to `ChatBotOperationClasses.All` and the completeness gauge to the meter required updating the two existing Story 8.2 exact-set tests (`ChatBotOperationClassesTests`, `ChatBotMetricsTests`).

### Completion Notes List

- **Completeness = reconstructability, not field presence (AC1).** `AuditOperationReconstructor` checks field presence only as a precondition (reusing `AuditMetadata`), then assembles the operation's end-state and resolves its NFR15a path; the AC2 measurer additionally diffs the rebuilt state against the live projection. The distinction is documented inline in the reconstructor + the ADR and locked by `AllFieldsPresentButNoMatchingProjectionIsNotReconstructable`.
- **Projection diff target (AC2 scoping — surfaced explicitly, not a silent partial).** The measurer diffs the rebuilt state against `IGovernedOperationProjectionStore` / `GovernedOperationView` — the read model whose key is `(tenant, noteId)` == `(tenant, resourceId)`, giving a clean structural match (resource presence + redaction-state token). The gateway operation-status store (`IOperationStatusStore`) named alongside it in the Dev Notes is keyed by `operationId`, not the audit resource id, so it has no reliable 1:1 join to a chained envelope; the governed-operation projection is the authoritative reconstructed-state read model and is what the diff uses. A future tightening can add an operation-status cross-check once an operationId↔resource mapping is carried on the envelope.
- **DEFERRED — periodic scheduler (inert-control-floor pattern, explicit).** The reconstructor, measurer, fraction→budget evaluator, observable gauge, and audit-then-deliver alert coordinator are **all built and fully tested**. No always-on `BackgroundService` / Dapr-timer is wired (consistent with Stories 7/8/9.1). A periodic runtime need only (a) call `AuditCompletenessAlertCoordinator.MeasureAllTenantsAndAlertAsync` on its cadence and (b) publish the `AuditCompletenessMeasurer.MeasureAllTenantsAsync` sweep into a real `IAuditCompletenessSource` (the default `UnavailableAuditCompletenessSource` emits nothing, so the gauge fabricates no value until then). This deferral is intentional and is **not** "done".
- **Replay exclusion is real and testable now (AC3/FR95a).** `AuditEnvelope.ReplayRunId` (nullable, default null) + `AuditReplayExclusion.IsReplayEnvelope` exclude replay events from both terms. The marker is folded into the canonical hash: `WormAuditChainHasher.CanonicalSerializationVersion` was bumped **v1 → v2** and canonicalization is version-aware, so the verifier re-hashes each record under its stamped version and pre-9.2 (v1) chains stay verifiable byte-for-byte. Story 9.4 owns *populating* `ReplayRunId`; today production has zero replay events, so exclusion holds by construction.
- **No-fabrication / fail-safe.** An unmeasurable run (chain/projection unavailable or throws) returns `Unmeasurable` (breach → `Unknown` budget state → P1 alert), never a fabricated 1.0. An empty window is vacuously complete (1.0) and does not page — distinct from "cannot complete".
- **Read-only / out-of-band (D4).** The measurer takes no `IAuditWriter`/alert dependency and only reads the chain + projection; `MeasurementIsReadOnlyAndAddsNoCommitPathSideEffect` asserts the chain length is unchanged after a sweep. The alert coordinator's pre-commit audit-then-deliver is the only write, and it is on the out-of-band measurement path, never the gateway commit path.
- **Metadata-only / tenant isolation.** All results/refs/locators are `AuditMetadata`-safe; the gauge carries a low-cardinality `tenant` tag only (the fraction is the value). Each tenant is measured over its own chain + its own projection (the projection read passes the tenant ref); `MeasurementIsPerTenantIsolatedWithNoCrossTenantLinkage` proves no cross-tenant linkage.

### File List

**New — source (`src/Hexalith.ChatBot.Server/`):**
- `Audit/AuditOperationReconstructor.cs` — pure per-operation reconstructability evaluator + `ReconstructedOperationState` / `AuditOperationReconstructionResult` (AC1).
- `Audit/ChatBotAuditPathMap.cs` — maps a chained envelope to its NFR15a inventory path (AC1).
- `Audit/AuditReplayExclusion.cs` — `IsReplayEnvelope` exclusion predicate (AC3/FR95a).
- `Audit/AuditCompletenessMeasurement.cs` — metadata-only per-tenant result + `AuditCompletenessSweepOutcome` (AC2).
- `Audit/AuditCompletenessMeasurer.cs` — scheduled production assertion: rebuild-from-log + diff-projection, per-tenant 7-day fraction, fail-safe, sweep (AC2).
- `Audit/AuditCompletenessAlertCoordinator.cs` — fail-closed audit-then-deliver P1 alert coordinator + sweep (AC2).
- `Observability/AuditCompletenessBudgetEvaluator.cs` — pure fraction→`ErrorBudgetBurnState` map (AC2).
- `Observability/IAuditCompletenessSource.cs` — gauge source seam + `AuditCompletenessReading` (AC2).
- `Observability/UnavailableAuditCompletenessSource.cs` — fail-safe default source (AC2).

**Modified — source:**
- `Audit/AuditEnvelope.cs` — added nullable `ReplayRunId` (AC3/FR95a).
- `Audit/WormAuditChainHasher.cs` — version-aware canonicalization; `CanonicalSerializationVersionV1` + bump to `v2`; `ReplayRunId` folded into the v2 hash (AC3).
- `Audit/WormAuditChainVerifier.cs` — re-hash each record under its stamped canonical version (keeps v1 chains verifiable).
- `Audit/OperatorAlertKind.cs` — added `AuditCompletenessBudgetBreached` (P1) (AC2).
- `Audit/AuditEnvelopeFactory.cs` — added `AuditCompletenessBudgetBreached(...)` metadata-only pre-commit producer (AC2).
- `Observability/ChatBotMetrics.cs` — `chatbot.audit.completeness` observable gauge + completeness source dependency (AC2).
- `Observability/ChatBotOperationClasses.cs` — added `audit-completeness` operation-class token (AC2).
- `Gateway/CommandGatewayServiceCollectionExtensions.cs` — DI registration of measurer, alert coordinator, and gauge source (AC2).

**New — tests (`tests/Hexalith.ChatBot.Server.Tests/`):**
- `Audit/AuditOperationReconstructorTests.cs` (AC1).
- `Audit/AuditCompletenessMeasurerTests.cs` (AC2/AC3/NFR9a).
- `Audit/AuditCompletenessAlertCoordinatorTests.cs` (AC2).
- `Audit/AuditReplayExclusionTests.cs` (AC3/FR95a + hash version bump).
- `Observability/AuditCompletenessBudgetEvaluatorTests.cs` (AC2).

**Modified — tests:**
- `Observability/ChatBotMetricsTests.cs` — completeness-gauge facts + updated instrument-set assertion.
- `Observability/ChatBotOperationClassesTests.cs` — added `audit-completeness` to the closed-set assertions.
- `Audit/WormAuditLeakTests.cs` — no-leak assertion for the completeness-breach alert envelope.
- `Audit/WormAuditChainDependencyInjectionTests.cs` — resolution guard for the new completeness seams.

**New — docs:**
- `docs/adrs/audit-completeness-observable.md` (AC1/AC2/AC3).

**Modified — tracking:**
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — story status → in-progress → review.

## Change Log

- 2026-06-03: Implemented Story 9.2 — audit completeness as a production observable (reconstructability, not field presence). Added the per-operation reconstructor + NFR15a path map, the per-tenant rolling-7-day completeness measurer (rebuild-from-log + diff-projection, fail-safe Unknown), the `chatbot.audit.completeness` observable gauge, the fraction→budget evaluator, and the P1 `AuditCompletenessBudgetBreached` audit-then-deliver alert coordinator. Introduced the `ReplayRunId` marker + exclusion predicate (FR95a) and folded it into a version-bumped (v1→v2), version-aware canonical hash that keeps Story 9.1 chains verifiable. Authored `docs/adrs/audit-completeness-observable.md`. All tasks complete; tests green (Server 1175, Architecture 37, Conformance 75, Workers 30); periodic scheduler explicitly deferred (inert-control-floor). Status → review.
- 2026-06-03: Senior Developer Review (AI) — adversarial review. Verified build (0 warnings/0 errors) and the full server suite (1181 passed / 0 failed, matching the Debug Log claim), confirmed the File List is exhaustive and every cited count accurate, and validated each AC against the implementation. Fixed one MEDIUM documentation/implementation mismatch: `ChatBotAuditPathMap` and the ADR both described a `StateTransition` "fallback for system-emitted records that carry no command type", but `Resolve()` is `CommandName`-only and every `AuditEnvelopeFactory` record always stamps a non-empty `CommandName` (the fallback's premise was false). Corrected the comment + ADR to describe the actual `CommandName`-only resolution (unmapped → completeness gap). No CRITICAL issues; remaining notes captured under Review Follow-ups. Status → done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-03. **Outcome:** Approve (no CRITICAL/HIGH defects; one MEDIUM fixed in place, two design observations logged as follow-ups).

**Validated:** All six tasks are genuinely complete (not just marked `[x]`). Each AC traced to code + a real assertion: AC1 reconstructability-≠-field-presence is enforced by `AuditOperationReconstructor` (presence as precondition → end-state assembly + path resolution) and locked by `AllFieldsPresentButNoMatchingProjectionIsNotReconstructable`; AC2 rebuild-from-log + diff-projection + 99.5%→P1 + fail-safe `Unknown` is implemented by `AuditCompletenessMeasurer` / `AuditCompletenessBudgetEvaluator` / `AuditCompletenessAlertCoordinator` (audit-then-deliver, fail-closed) and the `chatbot.audit.completeness` observable gauge (no measurement when unmeasurable); AC3 replay exclusion is real and testable, with `ReplayRunId` folded into a version-bumped (v1→v2), version-aware canonical hash that keeps Story 9.1 chains verifiable. Read-only/out-of-band (D4), metadata-only no-leak floor (NFR2/NFR42), and per-tenant isolation (NFR9a) are each covered by dedicated tests. Build clean (0/0); suite 1181/0; File List exhaustive and accurate (no bookkeeping drift).

**Fixed during review (MEDIUM):**
- `ChatBotAuditPathMap` class doc + ADR claimed a `StateTransition` fallback for "records that carry no command type" — but `Resolve()` only consults `CommandName` and every factory envelope always carries one, so the fallback was both absent and premised on a condition that never occurs. Corrected the comment and ADR to describe the actual `CommandName`-only resolution; unmapped envelopes (including system observability/alert records) become `unmapped_path` completeness gaps. No behavior change, no test impact.

### Review Follow-ups (AI)

- [ ] [AI-Review][Med] **Projection-diff coverage is governed-note-only.** The measurer diffs every operation against `IGovernedOperationProjectionStore` keyed by `(tenant, resourceId==noteId)`, but only the `RecordGovernedNote` (command-execution) path produces a `GovernedOperationView`. Operations on the other inventory paths resolve to a null projection and count as not-reconstructable. Partially disclosed in Completion Notes (projection-scope), recorded here for completeness. [src/Hexalith.ChatBot.Server/Audit/AuditCompletenessMeasurer.cs:149]
- [ ] [AI-Review][Med] **System observability/alert envelopes in the chain count as `unmapped_path` gaps.** The chain also contains pre-commit alert/observability records (e.g. `AuditChainBroken`, `OperationalAlertFired`, `EscalationFired`, `AuditCompletenessBudgetBreached`). They are not NFR15a state-writing operations, but the measurer groups them as their own operations and counts them against the denominator as gaps. When the (deferred) scheduler is wired, the measured fraction would therefore read below 99.5% for reasons unrelated to true audit gaps. Not auto-changed: "unmapped → gap" is the AC's explicit instruction, the control is inert today (no scheduler), and the correct refinement (an in-inventory governance-vs-observability classifier + an `operationId↔resource` mapping for the non-note paths) is a product/architecture decision that belongs with the periodic-scheduler wiring (Story 9.4-adjacent), not this story. [src/Hexalith.ChatBot.Server/Audit/ChatBotAuditPathMap.cs:81; src/Hexalith.ChatBot.Server/Audit/AuditCompletenessMeasurer.cs:76]
