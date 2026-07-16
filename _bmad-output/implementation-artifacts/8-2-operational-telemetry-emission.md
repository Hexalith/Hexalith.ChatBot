---
baseline_commit: f47715c
---

# Story 8.2: Operational telemetry emission

Status: done

<!-- Validation: create-story checklist applied 2026-06-03. -->

## Story

As an operator,
I want OpenTelemetry metrics emitted for every operation class with tenant and operation-class dimensions,
so that operational outcomes are measurable and metric loss is itself observable without blocking the underlying operation.

## Acceptance Criteria

1. Given operational outcomes across all operation classes, when they complete (success or failure), then OpenTelemetry metrics expose **ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag**, emitted through a single dedicated ChatBot meter registered on the always-on OpenTelemetry pipeline (not inside the trim-able dashboard read stage). [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR94` (line 1328); `_bmad-output/planning-artifacts/architecture.md#Observability` (line 400, "structured emission always-on … emission is not [trim-able]"); `src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs`]
2. Given the latency operation classes (ingestion, association, approval, command-execution), when measured, then each is recorded as a **duration histogram** (so percentile distribution is derivable) and the metric set also exposes error rate, retry rate, queue age, saturation indicators, and audit projection lag — satisfying the NFR28 distribution/rate set rather than raw counts only. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR28` (line 1409); `src/Hexalith.ChatBot.Server/Projections/AuditProjectionLagEvaluator.cs`]
3. Given every emitted metric, when published, then it carries bounded, low-cardinality dimensions: **tenant** (from the authenticated `ChatBotTenantBinding` only — never route/query/UI/correlation input) and **operation-class** (a finite stable token from the existing taxonomy, e.g. `message-intake`/`association`/`approval`/`command-execution`/`retry`/`duplicate-handling`/`audit-projection-lag`); **correlation** context (NFR34) is associated via the active trace/span (exemplar or span linkage), **not** as a metric attribute, to prevent cardinality explosion. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR34` (line 1418); `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR28`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotTenantBinding.cs`; `src/Hexalith.ChatBot.Server/Gateway/Correlation/ChatBotCorrelationContext.cs`; `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs` (`OperationClass`)]
4. Given metric dimensions and metadata, when emitted, then **no restricted tenant/project content** is used as a dimension or exemplar: no project names, evidence, file/mailbox content or headers, provider payloads, prompts, command bodies, raw claims, tokens, secrets, or PII. Tenant identity is carried as the stable bound tenant id only; correlation/operation ids appear only as trace exemplars, never as high-cardinality metric labels. Mirrors the existing metadata-only OTel invariant that ASP.NET Core instrumentation must not capture request/response bodies. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `tests/Hexalith.ChatBot.ServiceDefaults.Tests/ServiceDefaultsExtensionsTests.cs` (`OpenTelemetryShouldNotCaptureRequestOrResponseBodies`)]
5. Given a metric-pipeline failure (the emission call throws, the meter is unavailable, or the exporter is down), when it occurs, then **the underlying operation is never blocked or failed** by metric emission: emission is wrapped so it cannot throw into the operation path, and the operation completes on its normal success/failure path regardless. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR28` (metric loss does not block the operation)]
6. Given metric loss, when it happens, then the loss is **itself observable (gap detection)**: a dedicated meta-counter (e.g. `chatbot.telemetry.emission_failures`) increments on every swallowed emission failure (dimensioned by operation-class and a stable failure-reason token), so a downstream monitor can detect emission gaps rather than silently losing signal. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR28`]
7. Given the per-tenant operational exposure surface, when metrics are published, then they are emitted to the standard OpenTelemetry meter pipeline configured in `Hexalith.ChatBot.ServiceDefaults` (OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, in-process MeterProvider otherwise), with the new ChatBot meter registered via `AddMeter(...)` so it flows to the same per-tenant operational view the M2 dashboards (Story 8.1) read. No new public OpenAPI/gateway/command write path is introduced. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR94` (per-tenant operational view, M2); `src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs`; `_bmad-output/implementation-artifacts/8-1-operational-dashboards-s8-s10.md#Completion Notes List` (AC9 — generic transport reused, no public endpoint)]
8. Given the emission seam is read-only observability instrumentation, when implemented, then it adds **no** new `IChatBotCommand`, no `ChatBotSpineCommandAllowlist` entry, no gateway write stage, and no audit-write envelope, and it must not mutate project, queue, association, participant, approval, mailbox, policy, or audit state. The audit-projection-lag metric reuses `AuditProjectionLagEvaluator` read-only and surfaces only a coarse lag value — never audit envelope contents, hash-chain detail, redaction keys, or audit reasons. [Source: `_bmad-output/implementation-artifacts/8-1-operational-dashboards-s8-s10.md#Architecture Guardrails`; `src/Hexalith.ChatBot.Server/Projections/AuditProjectionLagEvaluator.cs`; `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`]
9. Given acceptance coverage runs, then tests prove: all seven metric instruments are registered on the ChatBot meter and observable via a `MeterListener`/in-memory metric reader; histograms record durations for the four latency classes and counters fire for retry-exhaustion and duplicate-suppression; the audit-projection-lag gauge reflects `AuditProjectionLagEvaluator` output; every metric carries only the bounded tenant + operation-class dimensions (and no restricted/secret-bearing dimension, asserted by a property/dimension-name ban); a forced emission failure is swallowed (operation still completes) **and** increments the gap-detection meta-counter; and the meter is wired into `ServiceDefaults` (`AddMeter`) so the MeterProvider exports it. [Source: `_bmad-output/planning-artifacts/architecture.md#Architecture Validation Results`; `tests/Hexalith.ChatBot.ServiceDefaults.Tests/ServiceDefaultsExtensionsTests.cs`]

## Tasks / Subtasks

- [x] Define the ChatBot metrics seam (meter + instruments + operation-class tokens) (AC: 1, 2, 3)
  - [x] Add a single dedicated meter (e.g. `Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs`) exposing a constant meter name (e.g. `ChatBotMeterName = "Hexalith.ChatBot"`, paralleling `ChatBotActivitySourceName` in `ServiceDefaults/Extensions.cs`). Use `System.Diagnostics.Metrics.Meter` with `Histogram<double>` for the four latency classes (ingestion, association, approval, command-execution), `Counter<long>` for retry-exhaustion and duplicate-suppression, and an `ObservableGauge<long>` (or up/down counter) for audit-projection-lag.
  - [x] Name instruments with stable dotted OTel-style names (e.g. `chatbot.ingestion.latency`, `chatbot.association.latency`, `chatbot.approval.latency`, `chatbot.command.execution.latency`, `chatbot.retry.exhausted`, `chatbot.duplicate.suppressed`, `chatbot.audit.projection.lag`). Latency histograms record **milliseconds** (UTC server-side timing); document the unit.
  - [x] Reuse the existing operation-class taxonomy rather than inventing new strings: operation-class tokens already exist as stable literals (`message-intake`, `association`, `approval`, `command-execution`, `retry`, `duplicate-handling`, `audit-projection-lag`) on `OperationStatus.OperationClass`, `MailboxIntakeWorkerResult.OperationClass`, and `RequestFailedWorkflowRetry`. Centralize the finite set as constants in the metrics seam (or a sibling `ChatBotOperationClasses`) and validate emitted operation-class against that finite set.
  - [x] Define a small emission API (e.g. `IChatBotMetrics` + `ChatBotMetrics` implementation) mirroring the existing `IUserFacingMessageTelemetry` / `InMemoryUserFacingMessageTelemetry` abstraction pattern in `Server/Gateway/`. Methods take the bounded dimensions only: `RecordIngestionLatency(string tenantId, double ms)`, `RecordRetryExhausted(string tenantId, string operationClass)`, etc. Do **not** accept payloads, evidence, or free-form tag bags.
- [x] Attach bounded dimensions and correlation correctly (AC: 3, 4)
  - [x] Every metric carries exactly two stable tags: `tenant` (the authenticated bound tenant id) and `operation-class` (finite token). Pull tenant from `ChatBotTenantBinding` at the emission site — never from route/query params, UI state, project ids, mailbox ids, or correlation ids.
  - [x] Do **not** add correlation id, operation id, command id, project id, or any unbounded value as a metric attribute (cardinality + NFR2). Correlation (NFR34) is satisfied by the active `Activity`/trace context already propagated via `ChatBotCorrelationMiddleware` / `ChatBotCorrelationContext`; rely on OTel trace exemplars / span linkage for metric→trace correlation, not metric labels.
  - [x] Enforce the metadata-only invariant: no project names, evidence, file/mailbox content or headers, provider payloads, prompts, command bodies, raw claims, tokens, secrets, or PII may appear in any dimension. Add a guard/validator and a test that bans secret-bearing or high-cardinality dimension names, mirroring `OpenTelemetryShouldNotCaptureRequestOrResponseBodies`.
- [x] Wrap emission to be non-blocking + gap-detectable (AC: 5, 6)
  - [x] Wrap every emission call so an exception (meter disposed, exporter failure, listener throw) is caught and swallowed — emission must never propagate into or fail the operation path. The operation completes on its normal success/failure path regardless of metric outcome.
  - [x] On any swallowed failure, increment a dedicated meta-counter `chatbot.telemetry.emission_failures` dimensioned by `operation-class` and a stable `reason` token (e.g. `emit-threw`, `meter-unavailable`). This is the gap-detection signal (NFR28). The meta-counter emission is itself best-effort and must not throw.
  - [x] Optionally emit a debug/trace log on swallowed failure (no payload), but logging is secondary to the meta-counter signal.
- [x] Instrument the operation classes at their existing completion points (AC: 1, 2)
  - [x] **Command execution latency**: record at the gateway dispatch completion seam (`CommandGateway` / `AcceptedCommandDispatcher`) — duration from accepted to terminal/dispatch result. Reuse the bound tenant from gateway context.
  - [x] **Ingestion latency**: record at the mailbox intake worker completion (`Workers/Mailbox/`, where `MailboxIntakeWorkerResult` is produced). Operation-class `message-intake`.
  - [x] **Association latency**: record at association scoring orchestration completion (`AssociationScoringOrchestrator`). Operation-class `association`.
  - [x] **Approval latency**: record at approval gate/decision completion (`AiActionApprovalGate` / approval decision path). Operation-class `approval`.
  - [x] **Retry exhaustion**: increment the retry-exhausted counter when a workflow item reaches `retry-exhausted` terminal state (the existing `retry-exhausted`/`RequestFailedWorkflowRetry` path). Operation-class `retry`.
  - [x] **Duplicate suppression**: increment the duplicate-suppressed counter where duplicate detection suppresses a provider message (the existing `duplicate-suppressed` path, Story 2.9). Operation-class `duplicate-handling`.
  - [x] **Audit projection lag**: register the observable gauge to read the coarse lag from `AuditProjectionLagEvaluator` (last-projected vs latest-committed). Surface only the coarse `LagEvents` value — never audit envelope contents, reasons, or hash-chain detail. Prefer not emitting (or emitting a sentinel) over fabricating a value when positions are unavailable.
  - [x] Keep each instrumentation call thin and at the seam boundary — do not thread metric concerns through business logic; do not change operation control flow or return shapes.
- [x] Register the meter in the OpenTelemetry pipeline (AC: 1, 7)
  - [x] In `ServiceDefaults/Extensions.cs#ConfigureOpenTelemetry`, add `.AddMeter(ChatBotMetrics.ChatBotMeterName)` to the `WithMetrics(...)` builder alongside the existing AspNetCore/HttpClient/Runtime instrumentation, so the ChatBot meter exports through the same MeterProvider/OTLP path.
  - [x] Register `IChatBotMetrics` → `ChatBotMetrics` as a singleton in the Server DI composition (alongside the existing gateway telemetry singletons in `CommandGatewayServiceCollectionExtensions.cs`). The `Meter` instance lifetime is the singleton.
  - [x] Do not change exporter selection, OTLP config, ActivitySource name, target frameworks, or Aspire/Dapr topology. Reuse the existing always-on emission pipeline.
- [x] Add focused tests (AC: all)
  - [x] ServiceDefaults tests (`tests/Hexalith.ChatBot.ServiceDefaults.Tests/ServiceDefaultsExtensionsTests.cs` or a sibling): assert the ChatBot meter name is registered on the MeterProvider (`AddMeter`) and the metadata-only body-capture invariant still holds.
  - [x] Metrics-seam unit tests (new, near `tests/Hexalith.ChatBot.Server.Tests/`): use a `System.Diagnostics.Metrics.MeterListener` (or `MetricCollector<T>`) to assert each of the seven instruments records/fires; histograms capture durations; counters increment; the observable gauge reflects `AuditProjectionLagEvaluator` output; every measurement carries only the `tenant` + `operation-class` tags and no restricted/secret-bearing tag (dimension-name ban assertion).
  - [x] Non-blocking + gap-detection tests: force an emission failure (e.g. inject a throwing instrument/listener) and assert (a) the caller still completes normally and (b) `chatbot.telemetry.emission_failures` increments with the operation-class + reason dimensions.
  - [x] Instrumentation-point tests (near the existing gateway/worker/orchestrator tests): assert command-execution / ingestion / association / approval latency is recorded once per completed operation with the bound tenant, and that retry-exhaustion / duplicate-suppression counters fire on their existing terminal paths — without altering the operation result.
  - [x] Conformance/architecture tests only if module boundaries or actor isolation change (the metrics seam stays in `.Server` + `.ServiceDefaults`; no public contract surface is added).

## Dev Notes

### Scope Boundaries

- Story 8.2 delivers **OpenTelemetry metric emission** for the seven FR94 operation outcomes (ingestion/association/approval/command-execution latency, retry exhaustion, duplicate suppression, audit projection lag), with bounded tenant + operation-class dimensions, trace-based correlation, non-blocking emission, and gap-detection. Emission is on the **always-on** structured-emission layer (architecture: "structured emission always-on … emission is not [trim-able]"), distinct from the trim-able dashboard read stage delivered in Story 8.1.
- It does **not** implement: SLO publication / error budgets / alert thresholds (Story 8.3 — this story emits the raw metrics those SLOs are computed from), tenant-safe alert wiring (Story 8.4), or degraded-state operability + runbook diagnostics (Story 8.5).
- It does **not** add dashboards or change the Story 8.1 dashboard read models. The audit-projection-lag metric reuses `AuditProjectionLagEvaluator` (already built in 8.1) read-only; it does not re-derive lag.
- It does **not** add a public OpenAPI endpoint, an `IChatBotCommand`, an allowlist entry, a gateway write stage, or an audit-write envelope. It is an instrumentation seam + meter registration only.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs` — `ConfigureOpenTelemetry` already wires `WithMetrics` (AspNetCore/HttpClient/Runtime instrumentation + OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` set) and `WithTracing` (`AddSource(ChatBotActivitySourceName)`). Add `.AddMeter(ChatBotMetrics.ChatBotMeterName)` here. `ChatBotActivitySourceName = "Hexalith.ChatBot"` is the naming precedent for the meter name constant.
- `src/Hexalith.ChatBot.Server/Gateway/IUserFacingMessageTelemetry.cs` + `InMemoryUserFacingMessageTelemetry.cs` — the existing in-`.Server` telemetry abstraction pattern (interface + thread-safe in-memory impl, registered as a singleton). Mirror this shape for `IChatBotMetrics` / `ChatBotMetrics`.
- `src/Hexalith.ChatBot.Server/Projections/AuditProjectionLagEvaluator.cs` — `Evaluate(lastProjectedPosition, latestCommittedPosition, snapshotUtc, nowUtc, …)` returns `AuditProjectionLagStatus(Health, LagIndicator, LagEvents, FreshnessTimestampUtc)`. Reuse read-only for the audit-projection-lag gauge; surface only the coarse `LagEvents`, never reasons/envelope/hash-chain.
- `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs` — `OperationClass` (default `"command-execution"`) is the canonical operation-class field. `src/Hexalith.ChatBot.Workers/Mailbox/MailboxIntakeWorkerResult.cs` carries `OperationClass` (`"message-intake"`). `RequestFailedWorkflowRetry` carries the retry operation-class. These define the finite operation-class taxonomy to centralize.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotTenantBinding.cs` — authenticated bound tenant identity; the **only** source for the `tenant` metric dimension.
- `src/Hexalith.ChatBot.Server/Gateway/Correlation/` — `ChatBotCorrelationContext` (record `CorrelationId`, `TaskId`), `ChatBotCorrelationMiddleware`, `ChatBotCorrelationHttpContextExtensions`. Correlation is already propagated; rely on the active `Activity`/trace for metric correlation (exemplars), not metric labels.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` — DI composition root where the gateway/telemetry singletons are registered (`TryAddSingleton`). Register `IChatBotMetrics` here.
- Instrumentation seams: `CommandGateway.cs` / `Stages/AcceptedCommandDispatcher.cs` (command-execution latency), `Workers/Mailbox/` (ingestion), `Stages/AssociationScoringOrchestrator.cs` (association), `Stages/AiActionApprovalGate.cs` (approval), retry-exhausted terminal path (`RequestFailedWorkflowRetry` / `retry-exhausted`), duplicate-suppressed path (Story 2.9).

### Current State To Preserve

- The OpenTelemetry **metadata-only invariant**: ASP.NET Core instrumentation must not capture request/response bodies (`ServiceDefaultsExtensionsTests.OpenTelemetryShouldNotCaptureRequestOrResponseBodies`). Extend the same posture to metric dimensions — no payload/PII/secret-bearing tags, ever.
- `ChatBotActivitySourceName = "Hexalith.ChatBot"` and the existing OTLP/exporter selection are stable. Do not rename the activity source, change exporter config, or alter the tracing pipeline.
- Tenant identity comes from authenticated `ChatBotTenantBinding` only; route/query params, UI state, project ids, mailbox ids, and correlation ids are never tenant sources (Story 8.1 / 7.5 invariant).
- The safety floor (tenant isolation, authorization, fail-closed gate, audit-of-the-command, gateway spine) must not be perturbed. Instrumentation is a passive read-only side-channel; it must not change operation control flow, ordering, or results — and must never block or fail an operation.
- Architecture: **structured emission is always-on and not trim-able** (unlike dashboards). Place the metrics seam in the always-on Server/ServiceDefaults layer, not behind a dashboard read policy.
- Reviewers found count-vs-enum, non-deterministic hashing, and File-List-drift defects in 7.5/8.1 — keep the File List exact, use the stable operation-class enum tokens, and avoid any non-deterministic dimension values.
- Root submodule policy: initialize/update only root `.gitmodules` submodules; never recursive submodule commands.

### Architecture Guardrails

- Metrics seam lives in `src/Hexalith.ChatBot.Server/Observability/` (or `Server/Telemetry/`) + meter registration in `src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs`. No new public contract in `.Contracts`, no generated-client change.
- This story is read-only/observability: add **no** `IChatBotCommand`, no `ChatBotSpineCommandAllowlist` entry, no gateway write stage, no audit-write envelope. Module boundaries (NetArchTest): UI/CLI/MCP depend only on `IChatBotClient`; the metrics seam is internal to `.Server`/`.ServiceDefaults` and must not leak across seams.
- Use `System.Diagnostics.Metrics` (`Meter`, `Histogram<double>`, `Counter<long>`, `ObservableGauge`) — the OTel-native .NET metrics API the existing `WithMetrics` pipeline consumes via `AddMeter`. Do not introduce a parallel metrics library or a bespoke counter store for the exported metrics (the in-memory `IUserFacingMessageTelemetry` style is fine for the gap-detection bookkeeping but the *exported* metrics must be real `Meter` instruments).
- Metric dimensions are finite, bounded, typed tokens only. No raw JSON tags, no user-provided dimension keys, no correlation/operation ids as labels (cardinality + NFR2).
- Latency uses server-side UTC timing (e.g. `Stopwatch`/timestamp delta at the seam); record milliseconds. Time/age formatting stays server-side; no tenant-local formatting at emission.
- Emission must be exception-isolated: never let a metric call throw into the operation path (AC5). Gap detection via a dedicated meta-counter (AC6).

### Cardinality & Correlation — Implementation Note

NFR34 requires correlation context to flow across surfaces; NFR28 requires distribution/rate metrics. The tension: correlation/operation ids are high-cardinality and must **not** be metric attributes (they would explode the time-series count and risk leaking identifiers, NFR2). Resolution: emit metrics with only `tenant` + `operation-class` as attributes, and satisfy correlation via **OTel trace exemplars / active-`Activity` linkage** — the correlation id already rides the trace context (`ChatBotCorrelationMiddleware`), so a metric measurement taken inside an active span is automatically correlatable to its trace without a high-cardinality label. Document this decision in the completion notes. If exemplars are not configured in the current exporter, record the metric inside the active `Activity` scope so span/trace linkage is preserved, and note exemplar wiring as a follow-up rather than adding a correlation-id label.

### Audit Projection Lag — Implementation Note

The audit-projection-lag metric must reuse `AuditProjectionLagEvaluator` (built in Story 8.1) read-only. Register an `ObservableGauge<long>` whose callback reads the current coarse `LagEvents` (last-projected vs latest-committed positions). Surface **only** the coarse lag value; never the `LagIndicator` text mapped from reasons, audit envelope contents, hash-chain detail, or redaction keys. When checkpoint positions are unavailable (the evaluator returns `Unknown` with `LagEvents == null`), prefer **not reporting** a measurement (or report a sentinel that downstream treats as "no data") over fabricating `0`/`healthy` — consistent with the evaluator's fail-safe `Unknown` doctrine.

### Previous Story Intelligence

- **Story 8.1 (operational dashboards, just completed)** built `AuditProjectionLagEvaluator`, `OperationalDashboardProjector`, `OperationalDashboardReadPolicy`, `OperationalDashboardFreshnessPolicy`, and the `ChatBotHealthStatus`/`ChatBotFreshnessState` enums. Its AC9 decision reused the generic transport with **no public OpenAPI endpoint** — follow the same posture (no public contract added) and record it in completion notes. Its review flagged: (1) do not present fabricated health as real (prefer `Unknown`/no-data over fabricated values) — apply the same fail-safe doctrine to the audit-lag gauge; (2) keep the File List exact (8.1 had a false "reaches the spine via `IChatBotClient`" claim corrected in review — make only honest claims about what is wired).
- **Story 7.5 (operational queue management)** established the operational-queue contracts and review lessons: a validated-but-unapplied pagination token, a process-dependent `GetHashCode()` fingerprint (replaced with deterministic SHA-256), and File-List drift. For metrics: use stable enum tokens, avoid non-deterministic dimension values, and keep the File List honest.
- **Story 1.8 (correlation propagation)** established `ChatBotCorrelationMiddleware`/`ChatBotCorrelationContext` and long-running-operation status — correlation already flows on the trace; reuse it, do not re-implement.
- The existing `IUserFacingMessageTelemetry`/`InMemoryUserFacingMessageTelemetry` (gateway message-catalog telemetry) is the in-repo precedent for a thread-safe, singleton, metadata-only telemetry abstraction — mirror its shape and registration.

### Latest Technical Specifics

- No external version research required. Use the repo-pinned stack and do not upgrade packages: .NET SDK `10.0.300`, `net10.0`, central package management (no inline versions), `System.Diagnostics.Metrics` + the OpenTelemetry packages already referenced by `ServiceDefaults` (`OpenTelemetry.Metrics`, `OpenTelemetry.Trace`, `OpenTelemetry.Logs`, AspNetCore/HttpClient/Runtime instrumentation), xUnit v3, Shouldly, NSubstitute.
- Do not change target frameworks, Aspire/Dapr topology, exporter config, `OTEL_EXPORTER_OTLP_ENDPOINT` handling, the activity source name, or submodule pointers.
- Prefer `System.Diagnostics.Metrics.Meter` instruments consumed by the existing `WithMetrics().AddMeter(...)` pipeline. For tests, use `MeterListener` (BCL) or `OpenTelemetry`'s in-memory/`MetricCollector<T>` reader to observe instruments deterministically without an exporter.

### Testing Notes

- Minimum validation before dev handoff (build then compiled in-process xUnit v3 runners; prefer compiled runners over `dotnet test` — VSTest can fail with `SocketException (13): Permission denied` in this sandbox):
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.ServiceDefaults.Tests/bin/Debug/net10.0/Hexalith.ChatBot.ServiceDefaults.Tests -parallel none` (meter registration / metadata-only invariant)
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` (metrics seam, instrumentation points, non-blocking + gap-detection)
  - `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none` (ingestion latency instrumentation, if the worker seam is touched)
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` (if any operation-class token constants land in `.Contracts`)
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` if module boundaries change
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` if actor isolation/query surfaces change
- Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, metadata-only fixtures, Allman braces, and root-level submodule policy.

### Project Structure Notes

- New metrics seam: `src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs` (+ `IChatBotMetrics.cs`, optional `ChatBotOperationClasses.cs` for the finite token set). Mirrors the `Gateway/IUserFacingMessageTelemetry.cs` pattern.
- Meter registration: extend `src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs#ConfigureOpenTelemetry` with `.AddMeter(...)`.
- DI registration: `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` (singleton).
- Instrumentation call sites: `Gateway/CommandGateway.cs` / `Gateway/Stages/AcceptedCommandDispatcher.cs`, `Workers/Mailbox/`, `Gateway/Stages/AssociationScoringOrchestrator.cs`, `Gateway/Stages/AiActionApprovalGate.cs`, retry-exhausted and duplicate-suppressed terminal paths.
- Audit-lag gauge: reads `src/Hexalith.ChatBot.Server/Projections/AuditProjectionLagEvaluator.cs` (read-only).
- Tests mirror source boundaries: `tests/Hexalith.ChatBot.ServiceDefaults.Tests`, `tests/Hexalith.ChatBot.Server.Tests`, `tests/Hexalith.ChatBot.Workers.Tests`.

### References

- `_bmad-output/planning-artifacts/epics.md#Epic 8` / `#Story 8.2` — source acceptance criteria (FR94, NFR28, NFR34; metric loss observable + non-blocking).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR94` (line 1328) — measurable operational outcomes (ingestion/association/approval/command latency, retry exhaustion, duplicate suppression, audit projection lag); OTel metrics to the tenant operational dashboard in M2.
- `...prd.md#NFR28` (line 1409) — latency metrics include percentile distribution, error rate, retry rate, queue age, saturation indicators, audit projection lag; metric loss does not block the operation.
- `...prd.md#NFR34` (line 1418) — correlation context across all surfaces (mailbox, file, association, approval, command, AI, audit, UI/API, CLI, MCP, workers, webhooks).
- `...prd.md#NFR42a` (line 1430) — SLOs computed from these metrics published in M2 (Story 8.3 consumer).
- `...prd.md#NFR2` — no restricted tenant/project detail leakage (applies to metric dimensions/exemplars).
- `_bmad-output/planning-artifacts/architecture.md#Observability` (lines 150, 400-401) — OpenTelemetry; per-class latency/queue/lag metrics; **structured emission always-on (dashboards trim-able, emission is not)**; published SLOs (M2).
- `_bmad-output/implementation-artifacts/8-1-operational-dashboards-s8-s10.md` — `AuditProjectionLagEvaluator` reuse, fail-safe `Unknown` doctrine, AC9 no-public-endpoint posture, File-List-honesty review lesson.
- `_bmad-output/implementation-artifacts/7-5-operational-queue-management.md` — operational-queue contracts; deterministic-value / File-List review lessons.
- Source anchors: `ServiceDefaults/Extensions.cs` (`ConfigureOpenTelemetry`, `ChatBotActivitySourceName`), `Gateway/IUserFacingMessageTelemetry.cs` + `InMemoryUserFacingMessageTelemetry.cs`, `Projections/AuditProjectionLagEvaluator.cs`, `Contracts/Queries/OperationStatus.cs` (`OperationClass`), `Workers/Mailbox/MailboxIntakeWorkerResult.cs`, `Gateway/Stages/ChatBotTenantBinding.cs`, `Gateway/Correlation/ChatBotCorrelationContext.cs`, `tests/Hexalith.ChatBot.ServiceDefaults.Tests/ServiceDefaultsExtensionsTests.cs`.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md` (Epic 8 overview + Story 8.1–8.5 for scope boundaries; Story 8.2 acceptance criteria).
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md` (Observability decision — OpenTelemetry, per-class metrics, always-on emission vs trim-able dashboards; ServiceDefaults home).
- Loaded `prd_content` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` (FR94, NFR28, NFR34, NFR42a, NFR2, NFR24–NFR27).
- Loaded persistent project-context facts from sibling `project-context.md` files (Commons, Memories, Folders, EventStore, Projects, Conversations, FrontComposer).
- Previous in-epic story: Story 8.1 (operational dashboards) — loaded fully for `AuditProjectionLagEvaluator` reuse, fail-safe doctrine, AC9 posture, and review lessons. Reviewed git history (`f47715c` story-8.1 … `526c7a0` story-7.27 …).
- Inspected current source: `ServiceDefaults/Extensions.cs` (existing `WithMetrics`/`WithTracing`/OTLP pipeline, `ChatBotActivitySourceName`), `IUserFacingMessageTelemetry`/`InMemoryUserFacingMessageTelemetry` (telemetry abstraction precedent), `AuditProjectionLagEvaluator`, `OperationStatus.OperationClass` + operation-class literal taxonomy, `ChatBotCorrelationContext`/correlation middleware, gateway stages and DI composition, `ServiceDefaultsExtensionsTests` (metadata-only OTel invariant + MeterProvider registration test pattern). Verified **no** existing dedicated ChatBot meter/`AddMeter` for domain metrics (only host/AspNetCore/HttpClient/Runtime instrumentation) and **no** existing operation-class metric emission.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Opus 4.8, 1M context)

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 Warning(s), 0 Error(s) (warnings-as-errors clean).
- `./tests/Hexalith.ChatBot.ServiceDefaults.Tests/...` → Total: 4, Failed: 0 (meter-name + AddMeter/OTLP pipeline; metadata-only invariant preserved).
- `./tests/Hexalith.ChatBot.Server.Tests/...` → Total: 970, Failed: 0 (metrics seam, instrumentation points, non-blocking + gap-detection).
- `./tests/Hexalith.ChatBot.Workers.Tests/...` → Total: 30, Failed: 0 (no intake regression).
- `./tests/Hexalith.ChatBot.Architecture.Tests/...` → Total: 37, Failed: 0; `./tests/Hexalith.ChatBot.Conformance.Tests/...` → Total: 75, Failed: 0 (module boundaries / actor isolation intact).

### Completion Notes List

- **Metrics seam (AC1–AC3).** Added a single dedicated `Meter` named `Hexalith.ChatBot` (`ChatBotMetrics`, `src/Hexalith.ChatBot.Server/Observability/`) with four duration histograms (`chatbot.ingestion.latency`, `chatbot.association.latency`, `chatbot.approval.latency`, `chatbot.command.execution.latency`, unit `ms`), two counters (`chatbot.retry.exhausted`, `chatbot.duplicate.suppressed`), one observable gauge (`chatbot.audit.projection.lag`), and the gap-detection meta-counter (`chatbot.telemetry.emission_failures`). The finite operation-class taxonomy is centralised in `ChatBotOperationClasses` (the same stable literals already used by `OperationStatus`/`MailboxIntakeWorkerResult`/retry/duplicate paths). The emission API `IChatBotMetrics` takes only the bound tenant id (+ ms for latencies); operation-class is fixed per method, so no free-form tag bag, payload, or evidence can be passed.
- **Bounded dimensions + correlation (AC3/AC4).** Every operational measurement carries exactly `tenant` (from `ChatBotTenantBinding` only) and `operation-class` (finite token). No correlation/operation/command/project id is ever a metric label — correlation (NFR34) rides the active `Activity`/trace per the existing `ChatBotCorrelationMiddleware`; exemplar wiring is a follow-up, not a label. A dimension-name ban test asserts the only tag keys ever emitted are `tenant` + `operation-class` (meta-counter uses `operation-class` + a stable `reason`), mirroring `OpenTelemetryShouldNotCaptureRequestOrResponseBodies`.
- **Non-blocking + gap-detection (AC5/AC6).** Every emission is wrapped (`SafeEmit`/observable-gauge guard) so a throwing instrument/listener/exporter/lag-source can never propagate into the operation path; the swallowed failure increments `chatbot.telemetry.emission_failures` with `operation-class` + `reason` (`emit-threw`, `tenant-unavailable`, `lag-source-threw`). A missing/blank bound tenant is treated as a gap (`tenant-unavailable`), never fabricated into an identity. The meta-counter increment is itself best-effort.
- **Instrumentation points (AC1/AC2).** Command-execution latency and association/approval latency are recorded at their existing gateway-stage completion seams (`AcceptedCommandDispatcher.DispatchAsync`, `AssociationScoringOrchestrator.ScoreAsync`, `AiActionApprovalGate.EvaluateAsync`); retry-exhaustion increments on the `IsExhausted` terminal branch of `RetryFailureAlertEmitter`; duplicate-suppression increments on the mailbox-intake replay branch of `CommandGateway`. Each seam takes `IChatBotMetrics` as an **optional** constructor dependency defaulting to `NullChatBotMetrics.Instance`, so existing call sites/tests are unchanged and no control flow, ordering, or return shape is altered.
- **Ingestion-latency seam — deliberate, documented deviation.** The task names the mailbox intake worker (`Workers/Mailbox/`) as the ingestion completion point, but `Hexalith.ChatBot.Workers` does **not** reference `Hexalith.ChatBot.Server` (it depends only on `.Client`/`.Contracts`), so the internal `.Server` metrics seam is not visible there. To respect the architecture guardrail ("metrics seam stays in `.Server`/`.ServiceDefaults`; no leak across seams") rather than introduce a new cross-project contract, ingestion latency (operation-class `message-intake`) is recorded at the in-bounds gateway dispatch of `CaptureMailboxMessageIntake` inside `AcceptedCommandDispatcher` — the durable ingestion completion the worker drives. The worker→gateway hop is excluded; emitting a real `message-intake` latency from the always-on `.Server` seam satisfies AC1/AC2. Honest claim per the 8.1/7.5 File-List/claims discipline.
- **Audit-projection-lag gauge (AC8 / 8.1 fail-safe doctrine).** The observable gauge derives the coarse lag read-only via `AuditProjectionLagEvaluator` from an `IAuditProjectionLagSource`. The production default (`UnavailableAuditProjectionLagSource`) returns no readings, so the gauge emits **nothing** rather than a fabricated `0` until a real per-tenant audit checkpoint feed is wired (a follow-up swap, consistent with Story 8.1 AC9's no-public-endpoint posture and the prefer-no-data-over-fabricated-health lesson). When positions are present the gauge surfaces only the coarse `LagEvents` — never `LagIndicator` text, envelope contents, reasons, or hash-chain detail.
- **Meter registration (AC1/AC7).** `ServiceDefaults/Extensions.cs#ConfigureOpenTelemetry` adds `.AddMeter(ChatBotMeterName)` to the existing always-on `WithMetrics` pipeline (OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` set, in-process MeterProvider otherwise). The meter-name constant lives in `ServiceDefaults` (Server → ServiceDefaults is the allowed reference direction; the reverse would invert the layering); `ChatBotMetrics` creates its `Meter` from that same constant so the seam and the AddMeter allowlist stay in lockstep. `IChatBotMetrics`/`IAuditProjectionLagSource` are registered as singletons in `CommandGatewayServiceCollectionExtensions`. No exporter selection, ActivitySource name, target framework, Aspire/Dapr topology, or submodule pointer changed.
- **Read-only / no-write guarantee (AC8).** No `IChatBotCommand`, `ChatBotSpineCommandAllowlist` entry, gateway write stage, audit-write envelope, or public OpenAPI surface added; the seam is passive observability only. The optional `chatbot.telemetry.emission_failures` debug/trace log (a *secondary* signal per the subtask) was intentionally omitted — the meta-counter is the gap-detection signal.

### File List

**Added (src):**

- `src/Hexalith.ChatBot.Server/Observability/ChatBotOperationClasses.cs`
- `src/Hexalith.ChatBot.Server/Observability/IChatBotMetrics.cs`
- `src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs`
- `src/Hexalith.ChatBot.Server/Observability/NullChatBotMetrics.cs`
- `src/Hexalith.ChatBot.Server/Observability/IAuditProjectionLagSource.cs`
- `src/Hexalith.ChatBot.Server/Observability/UnavailableAuditProjectionLagSource.cs`

**Modified (src):**

- `src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs` (add `ChatBotMeterName` const + `.AddMeter(ChatBotMeterName)`)
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` (register `IChatBotMetrics`/`IAuditProjectionLagSource` singletons)
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs` (command-execution / ingestion latency)
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AssociationScoringOrchestrator.cs` (association latency)
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs` (approval latency)
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` (duplicate-suppression counter)
- `src/Hexalith.ChatBot.Server/Lifecycle/Retry/RetryFailureAlertEmitter.cs` (retry-exhaustion counter)

**Added (tests):**

- `tests/Hexalith.ChatBot.Server.Tests/Observability/ChatBotMetricsTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/ChatBotOperationClassesTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/RecordingChatBotMetrics.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AiActionApprovalGateMetricsTests.cs`

**Modified (tests):**

- `tests/Hexalith.ChatBot.ServiceDefaults.Tests/ServiceDefaultsExtensionsTests.cs` (meter-name + AddMeter/OTLP pipeline test)
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs` (command-execution + ingestion latency tests)
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationScoringOrchestratorTests.cs` (association latency test)
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/RetryPolicyTests.cs` (retry-exhaustion metric tests)
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` (duplicate-suppression metric test + `Gateway(...)` metrics param)

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-03. **Outcome:** Approved after auto-fixes. No CRITICAL findings; all tasks marked `[x]` verified against implementation and every AC traced to code + tests.

**Findings fixed automatically (3):**

- **[HIGH] Command-execution/ingestion latency was recorded only on the success path.** `AcceptedCommandDispatcher.DispatchAsync` recorded latency immediately before `return`, so a throwing `BuildPlanAsync`/`SubmitCommandAsync` emitted no measurement — violating AC1 ("when they complete **(success or failure)**") and inconsistent with the approval (`AiActionApprovalGate`) and association (`AssociationScoringOrchestrator`) seams, which both record via `try/finally`. Fixed by wrapping the dispatch body in `try/finally` so latency is recorded on every completion path while the exception propagates unchanged. Added `DispatchShouldRecordCommandExecutionLatencyEvenWhenTheDispatchThrows` (mirrors the existing approval-gate throw test).
- **[MEDIUM] File List drift.** `tests/Hexalith.ChatBot.Server.Tests/Observability/ChatBotOperationClassesTests.cs` was present but absent from the File List — the exact 8.1/7.5 review lesson the story called out. Added to the File List.
- **[MEDIUM] ServiceDefaults `AddMeter` test did not actually verify `AddMeter`.** The prior `ChatBotMeterNameShouldBeStableAndWiredIntoTheMetricsPipeline` only asserted the constant value and that a `MeterProvider` resolves — removing `.AddMeter(ChatBotMeterName)` would NOT have failed it, yet AC9 requires *proving* the meter is wired so the provider exports it. Replaced with `ChatBotMeterIsWiredIntoTheMetricsPipelineSoItsInstrumentsAreCollected`, which attaches an in-memory `BaseExportingMetricReader`, publishes a probe instrument on a `Meter("Hexalith.ChatBot")`, force-flushes, and asserts the meter's measurement is collected. **Verified via mutation:** with `.AddMeter(...)` removed the new test fails; restored, it passes. Kept a separate `ChatBotMetricsPipelineBuildsWithTheOtlpExporter` smoke test to preserve the original OTLP-build coverage.

**Post-fix validation (compiled in-process xUnit v3 runners):** build `0 Warning(s), 0 Error(s)`; ServiceDefaults `Total: 5, Failed: 0`; Server `Total: 987, Failed: 0`; Workers `Total: 30, Failed: 0`; Architecture `Total: 37, Failed: 0`; Conformance `Total: 75, Failed: 0`.

## Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-06-03 | 0.2 | Senior Developer Review (AI): auto-fixed 1 HIGH (command-execution/ingestion latency now recorded on the failure path via `try/finally`, with a throw-path test) and 2 MEDIUM (File-List drift corrected; ServiceDefaults `AddMeter` test strengthened to actually prove the meter is collected, mutation-verified). All suites green. Status → done. | Jérôme Piquot (Review) |
| 2026-06-03 | 0.1 | Story 8.2 implemented: always-on ChatBot OpenTelemetry meter with seven FR94 operational instruments (ingestion/association/approval/command-execution latency histograms, retry-exhaustion + duplicate-suppression counters, audit-projection-lag observable gauge), bounded tenant + operation-class dimensions, trace-based correlation, non-blocking exception-isolated emission with a `chatbot.telemetry.emission_failures` gap-detection meta-counter, meter registered on the ServiceDefaults metrics pipeline via `AddMeter`, instrumentation wired at the existing gateway/orchestrator/retry seams, fail-safe audit-lag gauge (no fabricated value). Status → review. | Amelia (Dev Agent) |
