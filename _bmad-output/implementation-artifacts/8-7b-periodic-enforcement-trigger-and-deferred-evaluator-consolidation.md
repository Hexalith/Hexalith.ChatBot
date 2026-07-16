---
baseline_commit: 2559a85
---

# Story 8.7b: Periodic enforcement trigger and deferred evaluator consolidation

Status: done

<!-- Validation: create-story checklist applied 2026-06-11. -->

## Story

As a platform-operations engineer,
I want a periodic runtime trigger driving the deferred operational evaluators and feeds,
so that notification/escalation/throttling/backlog/rubber-stamp evaluation, alert wiring, runbook sampling, audit checkpointing, and control-state freshness run continuously in production rather than existing as wired-but-untriggered code.

## Acceptance Criteria

1. **One owned periodic runtime drives the deferred evaluator set.** Given Story 8.7a's durable control-state/rate-limit projection is live, when the configured periodic runtime starts in the Server host, then a single owned trigger runs non-overlapping passes and calls the existing coordinator/evaluator seams for the deferred 7.6-7.11 notification/escalation/throttle/backlog/rubber-stamp work, the Epic 8 `OperationalAlertWiringCoordinator`, the weekly 100-item runbook diagnostic sampler, the per-tenant audit-completeness sweep, and the per-tenant audit-projection-lag checkpoint feed. It must use injected clocks, cancellation tokens, typed options, metadata-only correlation, and no wall-clock sleeps in tests. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.7b`; `src/Hexalith.ChatBot.Server/Notifications/*Coordinator.cs`; `src/Hexalith.ChatBot.Server/Audit/AuditCompletenessAlertCoordinator.cs`; `src/Hexalith.ChatBot.Server/Observability/IAuditProjectionLagSource.cs`]

2. **Scheduler inputs come from narrow tenant-safe ports, not private store scraping.** Given a trigger pass runs for a tenant, when it gathers queue items, notification candidates/deliveries, approval decision samples, notification policy snapshots, escalation policy snapshots, runbook diagnostics, audit-chain positions, and governed-projection checkpoints, then each input is read through a focused interface backed by existing projections/stores with tenant-scoped keys. The implementation must not expose private dictionaries, enumerate Dapr state-store keys ad hoc, read restricted content, or invent synthetic healthy data when an input is unavailable. [Source: `_bmad-output/planning-artifacts/architecture.md#Events & projections`; `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`; `src/Hexalith.ChatBot.Server/Projections/IGovernedOperationProjectionStore.cs`; `src/Hexalith.ChatBot.Server/Projections/IGovernedControlStateProjectionStore.cs`]

3. **8.7a control-state freshness is kept practical without weakening fail-closed behavior.** Given projection-backed providers fail closed when a `GovernedControlStateView.LastUpdatedAtUtc` is stale, when the periodic trigger refreshes control-state freshness, then active and rate-limited subjects that still match the source-of-truth control state receive a metadata-only freshness heartbeat before ordinary staleness expires, while disabled/quarantined/revocation-sensitive records still observe the 60 second revocation-sensitive bound. The refresh must not re-activate a disabled/quarantined subject, wipe an existing budget, or extend a stale/missing source-of-truth record as active/unlimited. [Source: `_bmad-output/implementation-artifacts/8-7a-durable-control-state-rate-limit-projection-and-enforcement-seam-activation.md#Review Follow-ups`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ProjectionBackedGovernedControlProviders.cs`; `src/Hexalith.ChatBot.Server/Projections/GovernedControlStateView.cs`]

4. **Audit projection lag and completeness feeds publish measured data only.** Given audit and projection checkpoints exist for a tenant, when a trigger pass completes, then `IAuditProjectionLagSource` exposes the measured `(last projected position, latest committed position, snapshot time)` and `IAuditCompletenessSource` exposes measured completeness readings from the sweep. Given checkpoints or measurements cannot be trusted, then the sources expose no reading or an unmeasurable reading that alerts fail-closed; they must never fabricate `0` lag or `1.0` completeness. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.7b`; `src/Hexalith.ChatBot.Server/Observability/UnavailableAuditProjectionLagSource.cs`; `src/Hexalith.ChatBot.Server/Observability/UnavailableAuditCompletenessSource.cs`; `src/Hexalith.ChatBot.Server/Audit/AuditCompletenessMeasurer.cs`]

5. **Weekly runbook sampling produces auditable NFR44 evidence.** Given operational queue diagnostics exist, when the weekly sampler runs, then it selects exactly 100 eligible diagnostics per tenant where available, evaluates them with `RunbookDiagnosticCompletenessValidator.EvaluateSample`, and records metadata-only evidence of sampled count, complete count, defect item refs, tenant ref, correlation id, and sampled-at time. If fewer than 100 eligible items exist, the report must state the real sample size rather than padding. Any incomplete diagnostic is surfaced as a defect and routed to the existing operator-alert path without leaking project names, evidence content, file metadata, mailbox subjects, or audit reasons. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.5`; `src/Hexalith.ChatBot.Contracts/Queries/RunbookDiagnosticContracts.cs`; `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs`]

6. **Trigger failures and stalls are observable within 5 minutes.** Given the periodic trigger fails, stalls, overlaps, is disabled in a production profile, or misses its expected schedule, when the health/staleness evaluator runs, then the failure is observable through the Story 8.4 alert path within 5 minutes with scope, owner role, next safe action, and correlation id, and no evaluator silently stops. Failed evaluator calls must be isolated to the affected tenant/evaluator where possible and must not stop unrelated tenants from being evaluated in the same pass. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.7b`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR41`; `src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs`]

7. **Registration, cadence, and release evidence are explicit.** Given the story is complete, when the runtime service collection is built with the production/AppHost profile, then the periodic trigger is registered exactly once, its cadences and enablement are configured in typed options, the AppHost sets the production activation flag, and documentation/comments no longer describe the 7.6-7.11, 8.4, 8.5, audit-completeness, or audit-projection-lag callers as deferred. The default validation lane is green with focused scheduler, source, alert, runbook, and architecture tests. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md#Story 8.7 split`; `src/Hexalith.ChatBot.AppHost/Program.cs`; `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`; `Directory.Build.props`]

## Tasks / Subtasks

- [x] Add the periodic runtime composition (AC: 1, 6, 7)
  - [x] Add a focused runtime area such as `src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/` with `PeriodicEnforcementOptions`, `PeriodicEnforcementCoordinator`, `PeriodicEnforcementBackgroundService`, run status/outcome records, and a small service-collection extension.
  - [x] Register the hosted service only when a typed production flag such as `ChatBot:UsePeriodicEnforcementRuntime=true` is set; keep unit/in-process tests able to call the coordinator directly.
  - [x] Use `PeriodicTimer` or a Dapr timer binding behind the same coordinator seam. Do not introduce multiple independent timers for each evaluator.
  - [x] Prevent overlapping passes with an in-process guard; record skipped-overlap as a scheduler-health signal, not as success.
  - [x] Propagate `CancellationToken` to every coordinator/store call and use `ISystemClock` for timestamps.

- [x] Add tenant-safe scheduler input ports (AC: 1, 2, 5)
  - [x] Define narrow interfaces for tenant enumeration and per-tenant evaluator input, for example queue snapshot source, notification recipient/delivery source, approval decision sample source, notification/escalation policy snapshot source, and runbook diagnostic sample source.
  - [x] Back the in-memory implementations from existing projection stores without exposing private dictionaries directly outside the store boundary.
  - [x] Back the Dapr implementations with explicit tenant/index records maintained by the projection stores; do not enumerate arbitrary `chatbot-statestore` keys.
  - [x] Return unavailable/empty input explicitly where the product has no live source yet; do not fabricate recipients, policies, diagnostics, lag, or healthy states.

- [x] Drive the existing notification/evaluator coordinators (AC: 1, 2, 6)
  - [x] Call `EscalationEvaluationCoordinator.EvaluateAndDeliverAsync` with queue items, escalation policy snapshot, recipient candidates, tenant ref, and run correlation id.
  - [x] Call `NotificationThrottleCoordinator.EvaluateAndDeliverAsync` with already-resolved deliveries and bounded `NotificationThrottleCeilings`; preserve fail-closed audit-before-delivery semantics.
  - [x] Call `ReviewerBacklogAlertCoordinator.EvaluateAndDeliverAsync` with tenant queue items, recipient candidates, and `ReviewerBacklogThreshold`.
  - [x] Call `ApprovalRubberStampRateCoordinator.EvaluateAndRecordAsync` with approval decision samples materialized from approval projections, not raw content.
  - [x] Keep each evaluator's failures isolated and accumulated in a pass outcome so one failed evaluator does not silently suppress the rest.

- [x] Refresh 8.7a control-state projection freshness (AC: 3)
  - [x] Extend `IGovernedControlStateProjectionStore` with a tenant/subject enumeration or refresh method implemented by both in-memory and Dapr stores.
  - [x] Refresh `LastUpdatedAtUtc` only when the stored record still matches the source-of-truth control event/version or another trusted projection checkpoint.
  - [x] Preserve both dimensions independently: control state and rate-limit budget/window must not wipe each other during refresh.
  - [x] Add tests for idle active/rate-limited subjects staying usable after heartbeat, disabled/quarantined subjects staying blocked, revocation-sensitive records respecting 60 seconds, and stale/untrusted records remaining fail-closed.

- [x] Replace unavailable audit feeds with measured sources (AC: 4)
  - [x] Add a sweep-backed `IAuditCompletenessSource` implementation updated from `AuditCompletenessAlertCoordinator.MeasureAllTenantsAndAlertAsync` or the underlying measurements.
  - [x] Add a checkpoint-backed `IAuditProjectionLagSource` implementation that exposes per-tenant measured positions from the governed-operation projection/audit checkpoint feed.
  - [x] Update DI so production/runtime no longer resolves `UnavailableAuditProjectionLagSource` or `UnavailableAuditCompletenessSource` when the periodic runtime is enabled.
  - [x] Ensure source read APIs are metadata-only and low-cardinality: tenant id, position numbers/fraction, and snapshot time only.

- [x] Implement the weekly runbook sampler (AC: 5)
  - [x] Add a deterministic, testable sampler over eligible `OperationalQueueDiagnostics` produced through the operational queue projection path.
  - [x] Use a seeded random or deterministic weekly partition key so tests are repeatable while production selection changes by tenant/week.
  - [x] Evaluate with `RunbookDiagnosticCompletenessValidator.EvaluateSample`; record the report through metadata-only audit/operator evidence.
  - [x] Alert on defects via the existing operator-alert path; do not add a UI-only or log-only reporting path.

- [x] Add scheduler health and missed-cadence observability (AC: 6)
  - [x] Track last-start, last-success, last-failure, last-duration, skipped-overlap count, and per-evaluator failure counts in a tenant/evaluator-safe status model.
  - [x] Expose a read-only health/status endpoint or health-check contribution suitable for tests and operators.
  - [x] Route missed schedule/stalled pass/failure signals through the Story 8.4 alert path with metadata-only payloads and owner role `operations-admin` unless the existing alert mapping has a more specific owner.
  - [x] Add a test proving an overdue trigger creates an alert within the 5 minute NFR41 bound using an injected clock, not sleeps.

- [x] Register production activation and remove stale deferral comments (AC: 7)
  - [x] Update `CommandGatewayServiceCollectionExtensions` or a focused extension called by it so scheduler services and measured sources are registered in one place.
  - [x] Update `src/Hexalith.ChatBot.Server/Program.cs` to bind scheduler options and add the hosted service based on configuration.
  - [x] Update `src/Hexalith.ChatBot.AppHost/Program.cs` to set the periodic runtime activation flag alongside the existing Dapr state-store/workflow flags.
  - [x] Replace comments that say the 7.6-7.11, 8.4, 8.5, audit-completeness, and audit-projection-lag callers are deferred with precise comments naming the new runtime trigger.

- [x] Add validation coverage (AC: 1-7)
  - [x] Unit tests for non-overlap, cancellation, per-tenant isolation, evaluator failure isolation, status model updates, and scheduler missed-cadence alerting.
  - [x] Source tests for measured audit lag/completeness feeds and no-fabricated healthy readings.
  - [x] Runbook sampler tests for 100-item, fewer-than-100, defect, metadata-only, and deterministic weekly selection cases.
  - [x] DI/architecture tests proving production activation resolves the scheduler and measured sources exactly once and does not resolve unavailable sources on the live path.
  - [x] Regression tests covering the 8.7a freshness follow-up for idle active/rate-limited subjects.

- [x] Run and record validation evidence (AC: 7)
  - [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - [x] `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
  - [x] `git diff --check`

### Review Follow-ups (AI)

- [x] [AI-Review][Medium] Make the NFR44 runbook sweep genuinely *weekly* — it ran on every cadence tick (default 1 min), so identical defect alerts re-fired every minute. Now gated once per ISO week per tenant. [`src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/PeriodicEnforcementRuntime.cs` `RunRunbookSamplerAsync`]
- [x] [AI-Review][Medium] Record positive AC5 evidence (sampled/complete/defect counts, swept-at, correlation) for the weekly sweep, not only a single defect ref on defect runs; surfaced metadata-only (counts only, tenant-free) via the `/health/chatbot/periodic-enforcement` status, and the per-tenant defect alert now carries the sampled/complete/defect counts. [`PeriodicEnforcementRuntime.cs` `PeriodicEnforcementRunbookEvidence` / `IPeriodicEnforcementStatusStore.RecordRunbookSweep`]
- [x] [AI-Review][Medium] Story File List omitted the dev-modified `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` (the periodic-enforcement health-endpoint test) and `test-summary-story-8.7b.md`; File List corrected.
- [x] [AI-Review][Low] Strengthen the AC6/NFR41 missed-cadence test to prove the 5-minute boundary with an injected clock (silent within bound, alert once overdue), not only the never-run case.
- [ ] [AI-Review][Low] `IAuditProjectionLagSource` is wired to `CheckpointBackedAuditProjectionLagSource`, but its only `IAuditProjectionCheckpointSource` is `UnavailableAuditProjectionCheckpointSource` (returns `[]`) because `IGovernedOperationProjectionStore` exposes no projected/committed position surface. This is the honest "no live source yet" fail-safe (AC4 unavailable branch), not a fabrication — but a real measured checkpoint feed is still pending and should land when the governed-operation projection tracks positions. [`src/Hexalith.ChatBot.Server/Observability/CheckpointBackedAuditProjectionLagSource.cs`]

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Story 8.7 is a non-assignable parent container and Story 8.7b is the assignable child for periodic enforcement trigger and deferred evaluator consolidation.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`; it explicitly states Stories 8.7a/8.7b own control-plane runtime activation and that 8.7b consolidates the deferred Epic 7 evaluator, Epic 8 alert/runbook, and audit-checkpoint triggers.
- Loaded PRD requirements from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`: FR67, FR72, FR73, FR74, FR75, NFR41, NFR43, NFR44, NFR50a, and related fail-closed/metadata-only constraints.
- Loaded UX artifacts from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/`; this story has no new UI screen, but alerts/runbook evidence must remain safe, role-owned, redaction-aware, and accessible through existing operational surfaces.
- Loaded persistent project-context facts from sibling modules. Relevant constraints: .NET SDK `10.0.302`, `net10.0`, central package management, warnings-as-errors, Dapr at-least-once/unordered delivery, metadata-only telemetry/audit, no recursive submodule commands, and no incidental submodule edits.

### Current Implementation State

- Story 8.7a is complete at commit `2559a85`. Runtime DI now uses projection-backed control-state and rate-limit providers for service client, AI actor, command capability, and outbound channel seams. The providers fail closed when projected records are stale. [Source: `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ProjectionBackedGovernedControlProviders.cs`]
- The 8.7a review left a medium follow-up that belongs here: `GovernedProjectionProviderHelpers.IsStale` measures age from `LastUpdatedAtUtc`, so an idle active/rate-limited record becomes fail-closed after 5 minutes unless the periodic trigger refreshes trusted projection freshness. Treat this as in scope for 8.7b. [Source: `_bmad-output/implementation-artifacts/8-7a-durable-control-state-rate-limit-projection-and-enforcement-seam-activation.md#Review Follow-ups`]
- `EscalationEvaluationCoordinator`, `NotificationThrottleCoordinator`, `ReviewerBacklogAlertCoordinator`, `ApprovalRubberStampRateCoordinator`, `OperationalAlertWiringCoordinator`, `AuditCompletenessAlertCoordinator`, and `AuditCompletenessMeasurer` are already deterministic coordinator/evaluator seams. Their comments explicitly say no always-on `BackgroundService` exists and the runtime caller is deferred. Reuse these classes instead of rewriting their evaluator math. [Source: `src/Hexalith.ChatBot.Server/Notifications/EscalationEvaluationCoordinator.cs`; `src/Hexalith.ChatBot.Server/Notifications/NotificationThrottleCoordinator.cs`; `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogAlertCoordinator.cs`; `src/Hexalith.ChatBot.Server/Notifications/ApprovalRubberStampRateCoordinator.cs`; `src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs`; `src/Hexalith.ChatBot.Server/Audit/AuditCompletenessAlertCoordinator.cs`]
- `IAuditProjectionLagSource` and `IAuditCompletenessSource` currently default to unavailable sources that emit no readings to avoid fabricated healthy metrics. 8.7b must swap measured sources in the live runtime path, while keeping unavailable/no-data behavior when measurements cannot be trusted. [Source: `src/Hexalith.ChatBot.Server/Observability/UnavailableAuditProjectionLagSource.cs`; `src/Hexalith.ChatBot.Server/Observability/UnavailableAuditCompletenessSource.cs`; `src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs`]
- `RunbookDiagnosticCompletenessValidator` exists in Contracts and is the canonical NFR44 validator. `AdminQueueSummaryProjector` already produces `OperationalQueueDiagnostics` from `AdminQueueSummaryProjectionItem`, but there is no dedicated weekly sampler/runtime caller yet. [Source: `src/Hexalith.ChatBot.Contracts/Queries/RunbookDiagnosticContracts.cs`; `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs`]
- `IProjectConversationProjectionStore`, `IGovernedOperationProjectionStore`, and `IGovernedControlStateProjectionStore` are point-read/write oriented today. Adding scheduler input should be done through narrow interfaces or store extensions with explicit tenant/index records; do not leak private in-memory dictionaries or depend on Dapr key enumeration. [Source: `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`; `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`; `src/Hexalith.ChatBot.Server/Projections/DaprGovernedControlStateProjectionStore.cs`]

### Previous Story Intelligence

- Story 8.7a established the projection and provider surface. Build on `GovernedControlStateView`, `IGovernedControlStateProjectionStore`, `DaprGovernedControlStateProjectionStore`, and `ProjectionBackedGovernedControlProviders`; do not reintroduce `AlwaysActive...` or `AlwaysUnlimited...` providers on the live path. [Source: `_bmad-output/implementation-artifacts/8-7a-durable-control-state-rate-limit-projection-and-enforcement-seam-activation.md`]
- Story 8.6 completed hosted Dapr Workflow binding for correction propagation. Do not start Epic 11 host migration or restructure the hand-rolled Server host here; Epic 11 is explicitly sequenced after 8.7a/8.7b. [Source: `_bmad-output/implementation-artifacts/8-6-hosted-dapr-workflow-production-binding-and-saga-readiness-validation.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md`]
- Story 8.5 reinforced the no-fabricated-value doctrine for degraded state: stale, unknown, or unavailable operational data must surface honestly instead of pretending healthy. Apply that doctrine to scheduler status, audit lag, completeness, and runbook sampling. [Source: `_bmad-output/implementation-artifacts/8-5-degraded-state-operability-and-runbook-diagnostics.md`]
- Epic 7 retrospective follow-up says 8.7b must introduce a periodic runtime trigger for deferred notification, escalation, throttle, reviewer-backlog, rubber-stamp, alert wiring, runbook sampling, and audit-checkpoint feeds. [Source: `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-11.md`; `_bmad-output/implementation-artifacts/epic-8-retro-2026-06-03.md`]
- Recent git history is Epic 8 operational work: `2559a85 feat(story-8.7a)`, `716e4cc feat(story-8.6)`, `a4e8833 feat(story-8.5)`, `54681b6 feat(story-8.4)`, `a9f7d44 feat(story-8.3)`. Preserve the established pattern: metadata-only observability, fail-closed paths, focused tests, and explicit validation evidence.

### Architecture Guardrails

- Use .NET SDK `10.0.302`, target `net10.0`, nullable enabled, warnings-as-errors, Allman braces, file-scoped namespaces, and central package management. Do not add package versions to `.csproj` files. [Source: `global.json`; `Directory.Build.props`; `Directory.Packages.props`]
- Use one runtime trigger/coordinator seam. Do not scatter multiple independent `BackgroundService` timers across notification, alert, runbook, and audit areas.
- The trigger calls existing coordinators; it must not bypass fail-closed audit-before-delivery semantics already implemented inside those coordinators.
- Dapr pub/sub and projection delivery are at-least-once/unordered. Any scheduler-maintained checkpoint, index, or status model must be idempotent and tenant-partitioned.
- Projection freshness refresh is not a second source of truth. It may update `LastUpdatedAtUtc` only from trusted current projected/source state; it must not infer active/unlimited from absence.
- Store keys, indexes, scheduler status, runbook samples, audit readings, and alerts must include tenant scope and return safe no-data for the wrong tenant.
- UI/CLI/MCP must remain clients over existing surfaces. This story should not add new mutation bypasses or direct provider/projection internals to UI/CLI/MCP.
- Respect repository submodule policy: initialize/update only root-level submodules declared in root `.gitmodules`; never use recursive submodule commands.

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Server/
    Operations/
      PeriodicEnforcement/
        PeriodicEnforcementOptions.cs
        PeriodicEnforcementCoordinator.cs
        PeriodicEnforcementBackgroundService.cs
        PeriodicEnforcementRunStatus.cs
        PeriodicEnforcementServiceCollectionExtensions.cs
    Observability/
      SweepBackedAuditCompletenessSource.cs
      CheckpointBackedAuditProjectionLagSource.cs
    Projections/
      IOperationalEvaluatorInputSource.cs
      IRunbookDiagnosticSampleSource.cs
      IAuditProjectionCheckpointSource.cs
      ... focused in-memory/Dapr implementations or store extensions
tests/
  Hexalith.ChatBot.Server.Tests/Operations/PeriodicEnforcement/
  Hexalith.ChatBot.Server.Tests/Observability/
  Hexalith.ChatBot.Server.Tests/Projections/
  Hexalith.ChatBot.Architecture.Tests/
```

### Project Structure Notes

- Periodic runtime composition belongs in `.Server` because all existing coordinators, stores, audit, and observability sources are server-side.
- Keep pure evaluators in `Notifications`, `Observability`, and `Audit` unchanged except for comment updates or small source-seam additions. New scheduling orchestration should live in one focused operations/runtime area.
- Dapr-backed read models should keep using `chatbot-statestore` and explicit index records. Avoid any design that depends on scanning Redis/Dapr keys.
- Tests should extend existing `Server.Tests`, `Architecture.Tests`, and `Conformance.Tests`; no new test project is expected.
- AppHost changes are limited to activation/configuration flags. Do not retire AppHost/Aspire/ServiceDefaults or adopt the DomainService SDK here; that is Epic 11.

### Out of Scope

- Rewriting Story 8.7a projection/provider logic beyond freshness enumeration/heartbeat support required by this story.
- Epic 11 host migration / DomainService SDK adoption.
- New UI screens or new UX flows.
- New notification policy semantics, escalation policy semantics, rate-limit formulas, or new approval risk math.
- New external alert provider integration beyond existing metadata-only operator/notification alert seams.
- Direct reads of restricted mailbox/project/file/message content.
- Package upgrades, target framework changes, recursive submodule initialization, or generated-client hand edits.

### Latest Technical Specifics

- No external package/version research is required. Use the repo-pinned stack: .NET SDK `10.0.302`, `net10.0`, Dapr `1.17.9`, Aspire `13.4.3`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, and NetArchTest.eNhancedEdition `1.4.5`. [Source: `Directory.Packages.props`]
- Prefer `BackgroundService` plus `PeriodicTimer` unless the implementation chooses an existing Dapr timer binding behind the same coordinator seam. Tests must exercise the coordinator directly with injected clocks and cancellation rather than sleeping.
- If Dapr state-store writes are added for scheduler status/checkpoints, follow the existing `Dapr*Store` pattern and use `.ConfigureAwait(false)` on awaited library/server calls.

### Validation Notes

Checklist pass completed against `.agents/skills/bmad-create-story/checklist.md`:

- Reinvention prevention: story directs reuse of existing coordinator/evaluator seams and existing projection stores rather than new evaluator math or parallel gateways.
- Wrong-location prevention: runtime orchestration belongs under `.Server/Operations/PeriodicEnforcement`; measured sources under `.Server/Observability`; source/store additions under `.Server/Projections`.
- Regression prevention: preserves fail-closed audit-before-delivery behavior, tenant partitioning, metadata-only alerts/metrics, no fabricated healthy readings, and 8.7a stale-state fail-closed posture.
- Critical gap called out: 8.7a stale `LastUpdatedAtUtc` fail-closed behavior needs a trusted periodic freshness heartbeat so active/rate-limited subjects do not latch closed after idle periods.
- Scope control: Epic 11 host migration, UI screens, policy semantics, package upgrades, and generated-client edits are out of scope.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`]
- [Source: `.agents/skills/bmad-create-story/discover-inputs.md`]
- [Source: `.agents/skills/bmad-create-story/template.md`]
- [Source: `.agents/skills/bmad-create-story/checklist.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml#development_status`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.7b`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Findings to Carry into Decisions`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-readiness-blockers.md#CR-2`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md#Story 8.7 split`]
- [Source: `_bmad-output/implementation-artifacts/8-7a-durable-control-state-rate-limit-projection-and-enforcement-seam-activation.md#Review Follow-ups`]
- [Source: `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-11.md#Action Items`]
- [Source: `_bmad-output/implementation-artifacts/epic-8-retro-2026-06-03.md#Action Items`]
- [Source: `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Gateway/Stages/ProjectionBackedGovernedControlProviders.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Notifications/EscalationEvaluationCoordinator.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Notifications/NotificationThrottleCoordinator.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogAlertCoordinator.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Notifications/ApprovalRubberStampRateCoordinator.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Audit/AuditCompletenessAlertCoordinator.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Observability/IAuditProjectionLagSource.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Observability/IAuditCompletenessSource.cs`]
- [Source: `src/Hexalith.ChatBot.Contracts/Queries/RunbookDiagnosticContracts.cs`]
- [Source: `Directory.Packages.props`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-11T16:07:01+02:00 - Implemented periodic enforcement runtime, measured audit sources, tenant-safe projection inputs, control-state heartbeat refresh, runbook sampling, scheduler health/status, production activation, and validation tests.
- 2026-06-11T16:11:03+02:00 - Validation passed after clock-injected scheduler input cleanup: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
- 2026-06-11T16:11:03+02:00 - Validation passed after clock-injected scheduler input cleanup: `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` (1645 passed).
- 2026-06-11T16:11:03+02:00 - Validation passed after clock-injected scheduler input cleanup: `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` (41 passed).
- 2026-06-11T16:11:03+02:00 - Validation passed after clock-injected scheduler input cleanup: `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` (96 passed).
- 2026-06-11T16:11:03+02:00 - Validation passed after clock-injected scheduler input cleanup: `git diff --check`.

### Completion Notes List

- Story context created by bmad-create-story workflow.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added a single owned `PeriodicEnforcementCoordinator` and optional `PeriodicEnforcementBackgroundService` gated by `ChatBot:UsePeriodicEnforcementRuntime=true`; tests call the coordinator directly with injected clocks/cancellation.
- Added tenant-safe scheduler input ports backed by projection-store read methods and Dapr explicit tenant/project/control-state indexes; no private dictionaries are exposed outside store boundaries and Dapr code does not enumerate arbitrary state keys.
- The periodic coordinator now drives escalation, throttle/digest, reviewer backlog, rubber-stamp, operational alert wiring, runbook sampling, audit completeness, audit projection lag feed publication, and trusted control-state freshness heartbeats with isolated evaluator failures.
- Added measured audit completeness and audit projection lag sources; unavailable/null checkpoints do not fabricate healthy readings.
- Added deterministic weekly runbook sampling over operational diagnostics with real sample size, metadata-only defect alerting, and repeatable tenant/week selection.
- Added scheduler status/health tracking and a read-only `/health/chatbot/periodic-enforcement` endpoint; missed cadence/stall/overlap signals route to the existing operator-alert sink with operations-admin ownership metadata.
- Registered production activation in Server and AppHost, and updated comments that previously described the in-scope runtime callers as deferred.

### File List

- `_bmad-output/implementation-artifacts/8-7b-periodic-enforcement-trigger-and-deferred-evaluator-consolidation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.AppHost/Program.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditCompletenessAlertCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditCompletenessMeasurer.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Notifications/ApprovalDecisionSample.cs`
- `src/Hexalith.ChatBot.Server/Notifications/ApprovalRubberStampRateCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Notifications/EscalationEvaluationCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Notifications/NotificationDigest.cs`
- `src/Hexalith.ChatBot.Server/Notifications/NotificationThrottleCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogAlertCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Observability/CheckpointBackedAuditProjectionLagSource.cs`
- `src/Hexalith.ChatBot.Server/Observability/IAuditCompletenessSource.cs`
- `src/Hexalith.ChatBot.Server/Observability/SweepBackedAuditCompletenessSource.cs`
- `src/Hexalith.ChatBot.Server/Observability/UnavailableAuditCompletenessSource.cs`
- `src/Hexalith.ChatBot.Server/Observability/UnavailableAuditProjectionLagSource.cs`
- `src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/PeriodicEnforcementRuntime.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprGovernedControlStateProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IGovernedControlStateProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryGovernedControlStateProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/MeasuredAuditSourceTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/PeriodicEnforcement/PeriodicEnforcementCoordinatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/PeriodicEnforcement/PeriodicEnforcementDependencyInjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-8.7b.md`

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-11. Outcome: **Approve (with auto-fixes applied).**

### Scope

Adversarial review of the full File List against ACs 1-7 and the git working tree (baseline `2559a85`). Re-ran the default validation lane after fixes.

### What was verified as correct

- **AC1/AC6:** one owned `PeriodicEnforcementCoordinator` + opt-in `PeriodicEnforcementBackgroundService` (gated on `ChatBot:UsePeriodicEnforcementRuntime=true`), non-overlap via `Interlocked` guard with a skipped-overlap health signal + alert, `CancellationToken`/`ISystemClock` throughout, per-evaluator failure isolation (`RunEvaluatorAsync` swallows non-cancellation faults and accumulates counts).
- **AC2:** scheduler inputs flow through narrow ports backed by projection-store read methods; Dapr stores use explicit maintained tenant/project/subject index records (`UpsertTenantProjectIndexAsync` is called on approval upsert), not ad-hoc `chatbot-statestore` key enumeration. Empty/unavailable inputs are returned explicitly (no fabricated recipients/policies).
- **AC3:** `RefreshControlStateAsync` heartbeats only non-revocation-sensitive `active` records past the 4-min pre-stale window; `TryRefreshFreshnessAsync` bumps `LastUpdatedAtUtc` only when the stored record still matches the trusted state (version + control-state + rate-limit budget/window + revocation flag), preserving the rate-limit dimension. Disabled/quarantined/revocation-sensitive records stay fail-closed. Verified by `RunOnceAsyncShouldRefreshTrustedActiveControlStateWithoutWipingRateLimitBudget`.
- **AC4:** `SweepBackedAuditCompletenessSource` publishes measured readings carrying `IsMeasurable`; `CheckpointBackedAuditProjectionLagSource` filters checkpoints with null positions (no fabricated 0 lag). DI swaps the `Unavailable*` defaults to the measured sources only when the runtime is enabled (`PeriodicEnforcementDependencyInjectionTests`).
- **AC7:** registered once via `AddChatBotPeriodicEnforcement` (in the gateway core) + `AddChatBotPeriodicEnforcementHostedService` (flag-gated in `Program.cs`); AppHost sets `ChatBot__UsePeriodicEnforcementRuntime=true`; deferral comments across the notification/audit coordinators were replaced with the new runtime-caller language.

### Findings and resolutions (all High/Medium auto-fixed)

1. **[Medium → fixed] AC5 cadence:** the "weekly" runbook sampler executed on every enforcement pass (default 1-min cadence). Only the *selection seed* rotated weekly; execution and defect-alerting did not — a defect would have paged operators every minute. Added a tenant-partitioned once-per-ISO-week execution guard.
2. **[Medium → fixed] AC5 evidence:** no positive metadata-only evidence of the sweep (sampled/complete counts, swept-at) was recorded; only a defect run emitted an alert carrying a single defect ref. Added tenant-free aggregate evidence (`PeriodicEnforcementRunbookEvidence`) surfaced via the existing `/health/chatbot/periodic-enforcement` status, and enriched the per-tenant defect alert locator with sampled/complete/defect counts.
3. **[Medium → fixed] File List:** dev-modified `ServerBootstrapApiTests.cs` (the new health-endpoint test) and `test-summary-story-8.7b.md` were missing from the File List; added.
4. **[Low → fixed] AC6 test depth:** the missed-cadence test only proved the never-run case; added a clock-driven boundary test (silent within 5 min, alert once overdue).
5. **[Low → open] AC4 lag feed:** the audit-projection-lag path has no live measured checkpoint source (only the `Unavailable` placeholder) because the governed-operation projection exposes no position surface. This is the honest fail-safe (no fabrication), tracked as a Review Follow-up.

### Validation (post-fix)

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → 0 warnings, 0 errors.
- `Hexalith.ChatBot.Server.Tests -parallel none` → 1650 passed (+2 new: weekly-sweep/evidence + missed-cadence boundary).
- `Hexalith.ChatBot.Architecture.Tests -parallel none` → 41 passed.
- `Hexalith.ChatBot.Conformance.Tests -parallel none` → 96 passed.
- `git diff --check` → clean.

## Change Log

- 2026-06-11: Implemented Story 8.7b periodic enforcement runtime, tenant-safe scheduler inputs, measured audit feeds, control-state freshness heartbeat, weekly runbook sampler, scheduler health/status, production activation, and validation coverage.
- 2026-06-11: Senior Developer Review (AI) — auto-fixed AC5 weekly cadence (once-per-ISO-week-per-tenant gate), AC5 metadata-only sweep evidence + defect-alert counts, File List, and AC6 boundary test; validation re-run green (Server 1650 / Architecture 41 / Conformance 96). Status → done.
