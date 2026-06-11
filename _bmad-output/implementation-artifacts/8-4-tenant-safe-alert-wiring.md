---
baseline_commit: edd55f9
---

# Story 8.4: Tenant-safe alert wiring

Status: done

<!-- Validation: create-story checklist applied 2026-06-03. -->

## Story

As an operator,
I want the five NFR43 default alert thresholds wired to a deterministic, fail-closed evaluator+coordinator that fires a metadata-only, tenant-safe notification to the correct owner role when a threshold is breached,
so that breaches page the right owner without leaking restricted tenant/project detail.

## Acceptance Criteria

1. Given the **audit-projection-lag** alert threshold (`lag-gt-5m`, SLO catalog metric `chatbot.audit.projection.lag`, story 8.3), when the `AuditProjectionLagEvaluator` returns `Degraded` or `Failed` for any tenant, then an alert fires carrying affected scope (tenant-ref token), owner role (`operations-admin`), next safe action (`review-audit-projection-lag`), and reason code (`audit_projection_lag_breached`); the alert is denied for a non-human/unscoped principal and delivered via `INotificationSink` only after a successful pre-commit audit; no audit record is written and no delivery occurs when the audit writer is unavailable (fail-closed, NFR15a). [Source: `epics.md#Story 8.4`; `prd.md#NFR43` (line 1431); `src/Hexalith.ChatBot.Server/Projections/AuditProjectionLagEvaluator.cs`; `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogAlertCoordinator.cs`]

2. Given the **retry-exhaustion** alert threshold (`any-exhaustion`, SLO catalog metric `chatbot.retry.exhausted`, story 8.3), when `IChatBotMetrics.RecordRetryExhausted(tenantId)` is called (a workflow item reached the retry-exhausted terminal state), then an alert fires for that tenant carrying affected scope, owner role (`operations-admin`), next safe action (`review-failed-queue`), and reason code (`retry_exhaustion_threshold_exceeded`); the alert follows the same fail-closed pre-commit-audit-then-deliver pattern (AC1). [Source: `epics.md#Story 8.4`; `prd.md#NFR43`; `src/Hexalith.ChatBot.Server/Observability/IChatBotMetrics.cs`; `src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs` (RetryExhaustedInstrumentName)]

3. Given the **approval-queue age** alert threshold (`age>2-business-days`, SLO catalog metric `chatbot.approval.queue.age`, story 8.3), when a scan of `AdminQueueSummaryProjectionItem` for the `PendingApproval` queue family finds any non-terminal item with `AgeSeconds` ≥ 172,800 (48 h ≈ 2 business days, server-measured UTC, same clamp-to-zero logic as `ReviewerBacklogEvaluator.EffectiveAgeSeconds`), then a single aggregate alert fires per tenant carrying affected scope, owner role (`operations-admin`), next safe action (`review-approval-queue`), and reason code (`approval_queue_age_threshold_exceeded`); alert is deduplicated per tenant (at most one alert per evaluation pass regardless of how many items exceed the threshold); the fail-closed pre-commit-audit-then-deliver pattern applies (AC1). [Source: `epics.md#Story 8.4`; `prd.md#NFR43`; `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogEvaluator.cs`; `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs`]

4. Given the **mailbox-subscription-expiry** alert threshold (`expiry-le-7d`, SLO catalog metric `chatbot.mailbox.subscription.expiry`, story 8.3), when a mailbox source reports `MailboxDegradationReasonCode.GraphSubscriptionExpired` in the queue snapshot (i.e., an `AdminQueueSummaryProjectionItem` in the `FailedIngestion` queue family with a degradation reason containing `subscription-expired` or equivalent), then an alert fires per affected mailbox/tenant carrying affected scope (tenant ref + mailbox ref as a safe metadata token, never project detail), owner role (`mailbox-admin`), next safe action (`renew-graph-subscription`), and reason code (`subscription_expiry_threshold_exceeded`); the fail-closed pattern applies (AC1). [Source: `epics.md#Story 8.4`; `prd.md#NFR43`; `src/Hexalith.ChatBot.Contracts/Enums/MailboxDegradationReasonCode.cs` (GraphSubscriptionExpired)]

5. Given the **authorization-failure spike** alert threshold (`above-tenant-baseline`, SLO catalog metric implied by NFR43 "authorization-failure spikes above the tenant baseline"), when a rolling window of recent authorization-failure audit records for a tenant exceeds a configured baseline threshold (default: 10 failures in the rolling 10-minute window, expressed as safe constants `DefaultAuthFailureWindowSeconds = 600` / `DefaultAuthFailureBaselineCount = 10`), then an alert fires for that tenant carrying affected scope, owner role (`tenant-admin`), next safe action (`investigate-authorization-failures`), and reason code (`authorization_failure_spike_detected`); the alert count is the aggregate failure count (an integer, never a percentile), no per-actor/project/command detail leaks into the payload (NFR2); the fail-closed pattern applies (AC1). [Source: `epics.md#Story 8.4`; `prd.md#NFR43`; `prd.md#NFR2`; `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationFailureAuditFact.cs`]

6. Given any fired alert payload, when emitted to the `INotificationSink`, then it carries **only**: a stable `AffectedScope` token (tenant ref, optionally a safe mailbox ref, never a project name, evidence snippet, file path, actor PII, or audit detail), `OwnerRole` (one of `operations-admin`/`mailbox-admin`/`tenant-admin`, from the existing `AdminRole` enum), `NextSafeAction` (a bounded safe token from a closed enum, never raw error text), `ReasonCode` (a stable underscore-separated token), `AlertKind` (an enum), correlation ID, and fired-at UTC timestamp; the payload never carries restricted tenant data, project names, file metadata, candidate evidence, authorization claim detail, or secrets (NFR42, NFR2). [Source: `prd.md#NFR42` (line 1429); `prd.md#NFR2`; `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogAlert.cs`]

7. Given a fired alert, when its pre-commit audit record is written, then it is a **metadata-only** envelope following the `AuditEnvelopeFactory.ReviewerBacklogAlertFired` pattern: `CommandName` = `OperationalAlertFired`, `StateTransition` = `Open->Alerted`, `Decision` = `alert`, resource ref = the safe alert-kind token, refs contain only safe aggregate tokens (tenant-ref, alert-kind, owner-role, reason-code, correlation-id), no restricted content, written pre-commit via `IAuditWriter.RecordPreCommitAsync`, fails closed on `AuditUnavailable` (no delivery, counter incremented), one envelope per fired alert. [Source: `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (ReviewerBacklogAlertFired pattern); `prd.md#NFR15a`]

8. Given an evaluation pass over all five threshold classes, when all evaluators complete, then they are non-invasive: evaluators access only already-available in-process signals (the `IAuditProjectionLagSource` lag reading, the `AdminQueueSummaryProjectionItem` queue snapshot, the auth-failure rolling count from a dedicated in-process counter, and the retry-exhaustion hook on `IChatBotMetrics`) — no OTel histogram query, no external API call, no blocking IO on the operation path; the evaluation is side-effect-free (pure static evaluators) and the coordinator handles all stateful delivery and auditing. [Source: `prd.md#NFR43` (non-invasive); `src/Hexalith.ChatBot.Server/Projections/AuditProjectionLagEvaluator.cs`; `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogEvaluator.cs` (pure static pattern)]

9. Given acceptance coverage runs, then tests prove: each evaluator fires for the correct threshold condition and suppresses for sub-threshold inputs, with deterministic output given injected clock and snapshots; the coordinator audits before delivery, skips delivery on `AuditUnavailable`, and counts suppressions accurately; the alert payload validator rejects any payload carrying a safe-token violation (marker-banned text, secret-bearing token, high-cardinality field); authorization tests confirm that a see-only human-admin is the intended alert recipient and that non-human/unscoped principals cannot trigger or receive the alert delivery path; and the five alert kinds are each covered by at least one positive and one negative test. [Source: `tests/Hexalith.ChatBot.Server.Tests/Notifications/ReviewerBacklogAlertCoordinatorTests.cs` (test pattern); `prd.md#NFR43`]

## Tasks / Subtasks

- [x] Extend `OperatorAlertKind` with 4 new alert kinds (AC: 1, 2, 3, 4, 5)
  - [x] Add to `src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs`: `AuditProjectionLagBreached`, `SubscriptionExpiryImminent`, `ApprovalQueueAgeBreached`, `AuthorizationFailureSpike` (keep existing `RetryExhausted` — it already matches AC2's firing condition)
- [x] Define the shared tenant-safe operational alert payload (AC: 6)
  - [x] Add `src/Hexalith.ChatBot.Server/Observability/OperationalAlertPayload.cs`: a `sealed record OperationalAlertPayload(OperatorAlertKind AlertKind, string AffectedScope, string OwnerRole, string NextSafeAction, string ReasonCode, string TenantRef, string CorrelationId, DateTimeOffset FiredAtUtc)` — metadata-only, all string fields are required safe tokens (apply the existing `AuditMetadata.IsSafeStableIdentifier` / marker-ban posture from `OperatingBaselineContractValidator.IsSafeToken`); add a static `Validate(OperationalAlertPayload)` that checks each field and returns a list of validation errors (mirror `OperatingBaselineContractValidator.Validate`)
  - [x] Add `OperationalAlertOutcome` record: `(int Fired, int Delivered, int AuditUnavailable)` — mirrors `ReviewerBacklogAlertOutcome`
- [x] Build the audit-projection-lag alert evaluator (AC: 1)
  - [x] Add `src/Hexalith.ChatBot.Server/Observability/AuditProjectionLagAlertEvaluator.cs`: pure static evaluator accepting `AuditProjectionLagStatus` (already computed by `AuditProjectionLagEvaluator.Evaluate`) and returning a fired `OperationalAlertPayload?`; fires when `status.Health` is `Degraded` or `Failed`; returns `null` when `Healthy` or `Unknown`; mirrors the pure static shape of `ReviewerBacklogEvaluator` and `ErrorBudgetBurnEvaluator`; no clock beyond the passed-in status
- [x] Build the retry-exhaustion alert evaluator + hook (AC: 2)
  - [x] Add `src/Hexalith.ChatBot.Server/Observability/RetryExhaustionAlertEvaluator.cs`: pure static evaluator accepting a boolean `exhaustionOccurred` flag and a `string tenantId`, returning `OperationalAlertPayload?`; fires when the flag is true; the flag is set by the alert wiring coordinator's hook on `IChatBotMetrics.RecordRetryExhausted`
  - [x] The coordinator hooks into the `RecordRetryExhausted` path via a new `IRetryExhaustionAlertSource` interface (mirroring `IAuditProjectionLagSource`): the existing `ChatBotMetrics.RecordRetryExhausted` implementation calls the source to signal exhaustion; `InMemoryRetryExhaustionAlertSource` implements it as a simple in-process flag (thread-safe `volatile bool`) that the coordinator reads and clears each evaluation pass
- [x] Build the approval-queue age alert evaluator (AC: 3)
  - [x] Add `src/Hexalith.ChatBot.Server/Observability/ApprovalQueueAgeAlertEvaluator.cs`: pure static evaluator accepting `IReadOnlyList<AdminQueueSummaryProjectionItem>`, `DateTimeOffset nowUtc`, `int thresholdSeconds = 172800`, returning `OperationalAlertPayload?`; fires when any non-terminal PendingApproval item exceeds the threshold (same `IsOpen`/`EffectiveAgeSeconds` logic as `ReviewerBacklogEvaluator`, reuse the same `TerminalStatusTokens` pattern); returns a single aggregate `OperationalAlertPayload` (not per-item — one alert per tenant per pass); returns `null` when no item exceeds the threshold
- [x] Build the subscription-expiry alert evaluator (AC: 4)
  - [x] Add `src/Hexalith.ChatBot.Server/Observability/SubscriptionExpiryAlertEvaluator.cs`: pure static evaluator accepting `IReadOnlyList<AdminQueueSummaryProjectionItem>` and `string tenantRef`, returning `IReadOnlyList<OperationalAlertPayload>`; scans for `FailedIngestion`-family items whose `ReasonCode` (normalized: lowercase, hyphens removed) contains `graphsubscriptionexpired` or a stable token set (constant: `SubscriptionExpiredTokens`); fires one alert per distinct affected `MailboxRef` token (safe aggregate ref); `AffectedScope` = `$"tenant:{tenantRef} mailbox:{item.MailboxRef ?? "unknown"}"` — always a metadata token, never a project ref
- [x] Build the authorization-failure spike evaluator + in-process counter (AC: 5)
  - [x] Add `src/Hexalith.ChatBot.Server/Gateway/IAuthorizationFailureCounter.cs`: `internal interface IAuthorizationFailureCounter { void Record(string tenantId, DateTimeOffset timestamp); IReadOnlyList<AuthorizationFailureReading> ReadAndReset(); }` with `AuthorizationFailureReading(string TenantId, int FailureCount, DateTimeOffset WindowStartUtc)`; `InMemoryAuthorizationFailureCounter` uses a `ConcurrentDictionary<string, List<DateTimeOffset>>` per tenant, prunes entries outside the rolling window on `ReadAndReset`
  - [x] Add `src/Hexalith.ChatBot.Server/Observability/AuthorizationFailureSpikeEvaluator.cs`: pure static evaluator accepting `IReadOnlyList<AuthorizationFailureReading>` and threshold constants (`DefaultAuthFailureWindowSeconds = 600`, `DefaultAuthFailureBaselineCount = 10`), returning `IReadOnlyList<OperationalAlertPayload>`; fires one alert per tenant whose `FailureCount` strictly exceeds the threshold; payload `AffectedScope` = `$"tenant:{reading.TenantId}"` (never actor/command detail); `ReasonCode` = `authorization_failure_spike_detected`
  - [x] Wire `IAuthorizationFailureCounter.Record` into `ParticipantAuthorizationStage` (or the existing gateway authorization rejection path `ChatBotAuthorizationFailureAuditFact`) to capture each authorization denial: the stage already emits `ChatBotAuthorizationFailureAuditFact`, so hook into the post-audit step to call `counter.Record(tenantId, now)` without blocking the operation path (fire-and-forget ValueTask pattern)
- [x] Build the unified operational alert wiring coordinator (AC: 1–8)
  - [x] Add `src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs`: injectable coordinator (mirrors `ReviewerBacklogAlertCoordinator` class structure) with constructor parameters `(INotificationSink notificationSink, IAuditWriter auditWriter, IAuditProjectionLagSource lagSource, IRetryExhaustionAlertSource retrySource, IAuthorizationFailureCounter authFailureCounter, ISystemClock clock)`; exposes `EvaluateAndDeliverAsync(IReadOnlyList<AdminQueueSummaryProjectionItem> queueItems, IReadOnlyList<NotificationRecipientCandidate> candidates, string tenantRef, string correlationId, CancellationToken ct)` returning `OperationalAlertOutcome`; internally calls all five evaluators, collects fired `OperationalAlertPayload` items, then for each: writes the pre-commit audit envelope via `AuditEnvelopeFactory.OperationalAlertFired`, delivers via `notificationSink.DeliverAsync` only if audit succeeds (fail-closed), counts `AuditUnavailable` if not
  - [x] The `NotificationStateEvent` constructed for each alert uses `tenantRef` (the authenticated binding, never request body), `NotificationStateClass.OperationsAlert` (add to `NotificationStateClass` if missing, or reuse the closest existing class), `ItemProjectRef = null` (aggregate, no per-project leakage, same as `ReviewerBacklogEvaluator` → `MetadataRedacted`)
  - [x] Routing: each alert kind routes to the correct `AdminRole` via a routing entry matching its `OwnerRole`: `audit-projection-lag`/`retry-exhaustion`/`approval-queue-age` → `AdminRole.OperationsAdmin`; `subscription-expiry` → `AdminRole.MailboxAdmin`; `auth-failure-spike` → `AdminRole.TenantAdmin`
- [x] Add `AuditEnvelopeFactory.OperationalAlertFired` method (AC: 7)
  - [x] Extend `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` with a new `OperationalAlertFired(OperationalAlertPayload alert, DateTimeOffset timestamp)` factory method mirroring `ReviewerBacklogAlertFired`: `CommandName = "OperationalAlertFired"`, `StateTransition = "Open->Alerted"`, `Decision = "alert"`, refs = bounded safe tokens only (alert-kind wire value, reason-code, owner-role, affected-scope, correlation-id), `AuditCommitPhase.PreCommit`, `CoarseUserFacingRedactionStage.MetadataOnlyDecision`
- [x] Register new services in DI (AC: 8)
  - [x] In `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`: register `IRetryExhaustionAlertSource`/`InMemoryRetryExhaustionAlertSource` and `IAuthorizationFailureCounter`/`InMemoryAuthorizationFailureCounter` as singletons (mirroring the `IAuditProjectionLagSource`/`UnavailableAuditProjectionLagSource` pattern); register `OperationalAlertWiringCoordinator` as a scoped or singleton (consistent with `ReviewerBacklogAlertCoordinator` registration pattern)
- [x] Add focused tests (AC: 9)
  - [x] `tests/Hexalith.ChatBot.Server.Tests/Observability/AuditProjectionLagAlertEvaluatorTests.cs`: fires for `Degraded`/`Failed`; suppresses for `Healthy`/`Unknown`; payload fields are valid safe tokens
  - [x] `tests/Hexalith.ChatBot.Server.Tests/Observability/RetryExhaustionAlertEvaluatorTests.cs`: fires when flag is true; suppresses when false; payload fields valid
  - [x] `tests/Hexalith.ChatBot.Server.Tests/Observability/ApprovalQueueAgeAlertEvaluatorTests.cs`: fires for a single item over threshold; fires single aggregate (not per-item) for multiple items over threshold; suppresses when all items are below threshold or terminal; threshold boundary (exactly at threshold → no fire, strictly over → fire)
  - [x] `tests/Hexalith.ChatBot.Server.Tests/Observability/SubscriptionExpiryAlertEvaluatorTests.cs`: fires per distinct mailbox for `GraphSubscriptionExpired` items; suppresses for other degradation reasons; `AffectedScope` is a safe metadata token never containing project detail
  - [x] `tests/Hexalith.ChatBot.Server.Tests/Observability/AuthorizationFailureSpikeEvaluatorTests.cs`: fires when count strictly exceeds baseline; suppresses at exactly baseline; rolling-window pruning (events older than window are excluded); deterministic given same inputs; payload carries no actor/command/project detail
  - [x] `tests/Hexalith.ChatBot.Server.Tests/Notifications/OperationalAlertWiringCoordinatorTests.cs`: each evaluator's fired alert goes through pre-commit audit + deliver; coordinator returns `AuditUnavailable` count when audit writer fails; multiple alerts in one pass each get their own audit envelope; delivered payload fields pass `OperationalAlertPayload.Validate`
  - [x] `tests/Hexalith.ChatBot.Server.Tests/Observability/OperationalAlertPayloadValidatorTests.cs`: rejects marker-banned tokens (`secret`, `password`, `bearer`, `token`, `exception`, `.txt`, `.json`, `.xml`), high-cardinality values (simulated long project name), empty/whitespace fields; accepts all five alert kind payloads with their fixed safe tokens
  - [x] Architecture/conformance suites: only if module boundaries change (they should not — all new types stay in `.Server`)

## Dev Notes

### Scope Boundaries

- Story 8.4 delivers the **alert firing mechanism** for the five NFR43 default thresholds (subscription expiry, retry exhaustion, audit projection lag, approval queue age, authorization failure spikes). It consumes the SLO thresholds published by Story 8.3 conceptually — the same threshold values from `OperatingBaselineCatalog.Published.AlertThreshold` tokens are the human-readable names of the conditions the evaluators check, but the evaluators implement the logic directly from known constants, not by parsing the string tokens.
- It **builds on**: Story 8.1 (dashboard read surface, `AdminQueueSummaryProjectionItem`, `AuditProjectionLagEvaluator`), Story 8.2 (`IChatBotMetrics`/`ChatBotMetrics`, `IAuditProjectionLagSource`), Story 8.3 (SLO catalog + alert threshold token definitions), Story 7.10 (`ReviewerBacklogAlertCoordinator`/`ReviewerBacklogEvaluator` — **the primary pattern to follow**).
- It does **not** implement: Story 8.5 (degraded-state runbook diagnostics), a runtime scheduler/periodic trigger (the coordinator is injectable and called by the existing timer/Dapr-actor runtime — same deferral posture as `ReviewerBacklogAlertCoordinator`), M2 OTel metric backend querying, external alertmanager/PagerDuty integration (the delivery is via `INotificationSink`, same in-app channel as reviewer backlog alerts).
- It does **not** add: a public OpenAPI endpoint, `IChatBotCommand`, `ChatBotSpineCommandAllowlist` entry, gateway write stage, or post-commit WORM audit envelope. All alert audit envelopes are pre-commit only (AC7).

### Existing Code To Reuse — Critical

**Primary pattern: `ReviewerBacklogAlertCoordinator` (Story 7.10)**
- `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogAlertCoordinator.cs` — the exact orchestration pattern: `EvaluateAndDeliverAsync`, fail-closed pre-commit audit, `INotificationSink.DeliverAsync`, `AuditUnavailable` counter. Copy this structure exactly for `OperationalAlertWiringCoordinator`.
- `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogEvaluator.cs` — pure static evaluator pattern: no wall-clock, injected `ISystemClock`, static readonly terminal-status set, deterministic given inputs. Copy for each new evaluator.
- `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogAlert.cs` — the alert record. Replace with `OperationalAlertPayload` which is shared across all five alert kinds.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` → `ReviewerBacklogAlertFired` — the metadata-only pre-commit envelope factory. Add `OperationalAlertFired` following the exact same ref-list and field pattern.

**Evaluator signals:**
- `src/Hexalith.ChatBot.Server/Observability/IAuditProjectionLagSource.cs` + `AuditProjectionLagEvaluator.cs` — existing lag evaluation. `AuditProjectionLagAlertEvaluator` calls `AuditProjectionLagEvaluator.Evaluate` with readings from the registered `IAuditProjectionLagSource.ReadCurrent()` and fires when Health is `Degraded`/`Failed`.
- `src/Hexalith.ChatBot.Server/Observability/IChatBotMetrics.cs` → `RecordRetryExhausted(string tenantId)` — already called wherever a retry-exhausted terminal state is reached. The new `IRetryExhaustionAlertSource` sits alongside this: the concrete `ChatBotMetrics.RecordRetryExhausted` implementation also calls `_retryExhaustionSource.Signal(tenantId)` after the OTel counter increment.
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs` — the queue projection item. Both `ApprovalQueueAgeAlertEvaluator` and `SubscriptionExpiryAlertEvaluator` consume `IReadOnlyList<AdminQueueSummaryProjectionItem>` exactly as `ReviewerBacklogEvaluator` does.
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs` fields: `QueueFamily` (`OperationalQueueFamily` enum), `Status`, `AgeSeconds`, `IsTerminal`, `AssigneeRef`, `FreshnessTimestampUtc`, `MailboxRef`, `ReasonCode`. Use `FreshnessTimestampUtc` for server-measured age (same as `EffectiveAgeSeconds` in `ReviewerBacklogEvaluator`).
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationFailureAuditFact.cs` — already records `TenantId`, `ActorId`, `CommandType`, `ReasonCode`, `CorrelationId`, `TaskId`, `SurfaceOrigin` per authorization failure. The new `IAuthorizationFailureCounter.Record(tenantId, timestamp)` is called from the same code path that emits this fact, using only the `tenantId` — never `ActorId`, `CommandType`, or `ReasonCode` (those stay in audit only, not in the spike alert payload per NFR2).

**Notification routing:**
- `src/Hexalith.ChatBot.Server/Notifications/NotificationRoutingResolver.cs` — resolves a `NotificationStateEvent` + routing to `IReadOnlyList<NotificationDelivery>`. The coordinator constructs a `NotificationStateEvent` per alert with `ItemProjectRef = null` (aggregate, MetadataRedacted form, identical to reviewer-backlog pattern).
- `src/Hexalith.ChatBot.Server/Notifications/NotificationStateEvent.cs` — `(TenantRef, StateClass, ItemRef, QueueRef, ReasonCode, CorrelationId, Timestamp, ItemProjectRef?)`.
- `src/Hexalith.ChatBot.Contracts/Enums/AdminRole.cs` + `AdminScope.cs` — the existing bounded admin role/scope enums. `OperationsAdmin`, `MailboxAdmin`, `TenantAdmin` are the three roles used (verify the exact enum member names in `AdminRoles.cs`).
- `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` — `HasHumanAdminScope(principal, AdminScope.SeeOnly)` — the recipient must hold see-only human-admin scope; the resolver handles this already.

**Safe token validation:**
- `src/Hexalith.ChatBot.Contracts/Queries/OperatingBaselineContracts.cs` → `OperatingBaselineContractValidator.IsSafeToken(string)` — the ASCII/marker-ban posture (`secret`/`password`/`bearer`/`token`/`exception`/`.txt`/`.json`/`.xml` banned). Reuse or copy for `OperationalAlertPayload.Validate`.
- The `AffectedScope` field uses the format `"tenant:{tenantRef}"` or `"tenant:{tenantRef} mailbox:{mailboxRef}"` — both components must individually pass `IsSafeToken`. The `tenantRef` comes from the authenticated binding only, never from request body.

**DI registration:**
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` — lines 149-166 register `INotificationSink`, `IAuditProjectionLagSource`, `IChatBotMetrics` as singletons. Add `IRetryExhaustionAlertSource`/`InMemoryRetryExhaustionAlertSource` and `IAuthorizationFailureCounter`/`InMemoryAuthorizationFailureCounter` as singletons in the same pattern.

### Current State To Preserve

- **Fail-closed doctrine (8.1/8.2/8.3):** never fabricate a healthy/safe value; `Unknown`/no-data beats a made-up status. Apply this to evaluators: when the lag source returns no readings, no lag alert fires (cannot distinguish healthy from unavailable lag signal). When the auth-failure counter returns an empty reading, no spike alert fires.
- **Metadata-only / summary-safe invariant:** the alert payload carries no project/file/evidence/participant/message/audit content. `ItemProjectRef = null` in the `NotificationStateEvent` → the `NotificationRoutingResolver` yields `MetadataRedacted` visibility for all five alert kinds (mirrors the reviewer-backlog aggregate alert).
- **No write path:** no `IChatBotCommand`, no `ChatBotSpineCommandAllowlist` entry, no gateway write stage, no public OpenAPI endpoint. Alert audit envelopes are pre-commit only.
- **Non-invasive:** evaluators are called by the coordinator, not injected into the hot operation path. The only exception is the `IRetryExhaustionAlertSource.Signal()` call inside `ChatBotMetrics.RecordRetryExhausted` — this must be a non-throwing, fire-and-forget call that never propagates exceptions into the metric-recording path (the same exception-isolation posture as `ChatBotMetrics` OTel emissions: swallow + gap-count).
- **Stack & topology:** do not change target frameworks (`net10.0`, SDK `10.0.300`), central package management, exporter/OTLP config, the `Hexalith.ChatBot` meter/activity-source names, or Aspire/Dapr topology. Root submodule policy: initialize/update only root `.gitmodules` submodules; never recursive submodule commands.
- **Additive change:** `OperatorAlertKind` is an `internal enum` — adding new members is additive and non-breaking inside `.Server`. No client/contract enum is changed.

### Architecture Guardrails

- All new evaluators and the `OperationalAlertPayload` live in `src/Hexalith.ChatBot.Server/Observability/` (aligned with `ErrorBudgetBurnEvaluator` and `AuditProjectionLagEvaluator`).
- `OperationalAlertWiringCoordinator` lives in `src/Hexalith.ChatBot.Server/Notifications/` (alongside `ReviewerBacklogAlertCoordinator`).
- `IAuthorizationFailureCounter` and its in-memory implementation live in `src/Hexalith.ChatBot.Server/Gateway/` (alongside `ChatBotAuthorizationFailureAuditFact` — it is a gateway seam).
- `IRetryExhaustionAlertSource` and its in-memory implementation live in `src/Hexalith.ChatBot.Server/Observability/` (alongside `IAuditProjectionLagSource`).
- No new types in `.Contracts` — the alert payload is an internal `.Server` record (not a public contract), because alert delivery is server-internal (the `INotificationSink` delivers in-app; no public client shape is added).
- NetArchTest: UI/CLI/MCP depend only on `IChatBotClient`; the new types are internal to `.Server`; no `*.UI`/`*.Cli`/`*.Mcp` references to new alert types.

### Alert Payload Constants (Safe Tokens)

All of these must pass `IsSafeToken` (ASCII-safe, no banned markers):

| Alert Kind | ReasonCode | OwnerRole | NextSafeAction | AffectedScope format |
|---|---|---|---|---|
| `AuditProjectionLagBreached` | `audit_projection_lag_breached` | `operations-admin` | `review-audit-projection-lag` | `tenant:{tenantRef}` |
| `RetryExhausted` | `retry_exhaustion_threshold_exceeded` | `operations-admin` | `review-failed-queue` | `tenant:{tenantRef}` |
| `ApprovalQueueAgeBreached` | `approval_queue_age_threshold_exceeded` | `operations-admin` | `review-approval-queue` | `tenant:{tenantRef}` |
| `SubscriptionExpiryImminent` | `subscription_expiry_threshold_exceeded` | `mailbox-admin` | `renew-graph-subscription` | `tenant:{tenantRef} mailbox:{mailboxRef}` |
| `AuthorizationFailureSpike` | `authorization_failure_spike_detected` | `tenant-admin` | `investigate-authorization-failures` | `tenant:{tenantRef}` |

Underscore-separated reason codes are the wire form; all other fields are hyphen-separated safe tokens. No spaces, no uppercase, no special characters beyond hyphens and colons in the scope format.

### Approval Queue Age Threshold

The 2-business-day threshold expressed as seconds:
- `public const int BusinessDayAlertThresholdSeconds = 172800;` (48 hours = 2 × 24 h)
- This is a conservative approximation: 2 business days maps to 48 calendar hours as a safe UTC approximation (mirrors NFR43's "approval items older than 2 business days")
- Injected via constructor parameter for testability; constant is the default

### Authorization Failure Spike — Rolling Window

```csharp
public const int DefaultAuthFailureWindowSeconds = 600;  // 10-minute rolling window
public const int DefaultAuthFailureBaselineCount = 10;   // fires when strictly > 10 in window
```

`InMemoryAuthorizationFailureCounter`:
- Per-tenant `List<DateTimeOffset>` behind a lock
- `Record`: add timestamp; prune entries older than `now - WindowSeconds`
- `ReadAndReset`: take a snapshot, prune old entries, return per-tenant counts, do NOT clear the list (sliding window, not a tumbling window that resets — the evaluator decides whether to fire per pass based on the current count; clearing would miss a sustained spike)
- Thread-safe: use `lock` or `ConcurrentDictionary` + explicit locking on the per-tenant list

### Previous Story Intelligence

- **Story 8.3 (SLO publication, baseline `edd55f9`)** published the SLO catalog with alert threshold tokens (`lag-gt-5m`, `expiry-le-7d`, `any-exhaustion`, `age-gt-2-business-days`). Story 8.4 **wires the actual firing** for these thresholds. The catalog's `AlertThreshold` strings are human-readable; the evaluators implement the numeric conditions directly from constants, not by parsing tokens.
- **Story 8.3 review lessons:** `IsSafeToken` bans `<`, `>`, `=`, `%` — use `le`/`gt`/`eq` in tokens (already adopted in 8.3). `calibration-pending`/`a11-pending` for unconfirmed values. File-List honesty: only list files that were actually changed; no false completions.
- **Story 8.2 (telemetry emission)** wired `IChatBotMetrics.RecordRetryExhausted(tenantId)` as the counter that the retry-exhaustion alert evaluator hooks into. The `ChatBotMetrics.RecordRetryExhausted` implementation already does the OTel counter increment; add the `IRetryExhaustionAlertSource.Signal(tenantId)` call **after** the counter increment (not before, to preserve the existing ordering invariant).
- **Story 8.1 (dashboards)** built `AdminQueueSummaryProjectionItem` and the `FailedIngestion`/`PendingApproval` queue families. The subscription-expiry evaluator consumes `FailedIngestion` items; the approval-queue age evaluator consumes `PendingApproval` items — both from the same snapshot the dashboard already reads.
- **Story 7.10 (reviewer-backlog alerting)** is the structural twin of this story. Read `ReviewerBacklogAlertCoordinator` and `ReviewerBacklogEvaluator` fully before implementing. The exact discipline (fail-closed pre-commit, `AuditUnavailable` counter, `NotificationRoutingResolver.Resolve`, `NotificationContentVisibility.MetadataRedacted` via null `ItemProjectRef`) must be replicated.
- **Review lessons from 7.5/7.10:** stable enum tokens, deterministic values (no process-dependent hashing), exact File List, honest completion claims.

### Latest Technical Specifics

- No external version research required. Use repo-pinned stack: .NET SDK `10.0.300`, `net10.0`, central package management (no inline versions), xUnit v3, Shouldly, NSubstitute. Do not upgrade packages or change target frameworks, exporter config, meter/activity-source names, or Aspire/Dapr topology.
- Tests use compiled in-process xUnit v3 runners (`-parallel none`). New evaluator tests are plain pure-function tests (no `MeterListener`, no DI container). Coordinator tests follow `ReviewerBacklogAlertCoordinatorTests.cs` pattern: NSubstitute mocks for `IAuditWriter` + `INotificationSink`.
- `OperationalQueueFamily` enum values and `AdminRole`/`AdminScope` member names must be verified against the current source before use. Do not assume member names from the git log summary — read the enum files directly.

### Testing Notes

Minimum validation before dev handoff:
```
dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false
./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none
./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none
./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none
```
`Hexalith.ChatBot.Contracts.Tests` and `Hexalith.ChatBot.UI.Tests` should stay green (no contract or UI changes expected).

Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, NSubstitute, metadata-only fixtures, Allman braces, and root-level submodule policy.

### Project Structure Notes

**New files:**
- `src/Hexalith.ChatBot.Server/Observability/OperationalAlertPayload.cs` (`OperationalAlertPayload` record + `OperationalAlertOutcome` record + `Validate` static method)
- `src/Hexalith.ChatBot.Server/Observability/AuditProjectionLagAlertEvaluator.cs` (pure static)
- `src/Hexalith.ChatBot.Server/Observability/RetryExhaustionAlertEvaluator.cs` (pure static)
- `src/Hexalith.ChatBot.Server/Observability/IRetryExhaustionAlertSource.cs` (interface)
- `src/Hexalith.ChatBot.Server/Observability/InMemoryRetryExhaustionAlertSource.cs` (in-memory, thread-safe)
- `src/Hexalith.ChatBot.Server/Observability/ApprovalQueueAgeAlertEvaluator.cs` (pure static)
- `src/Hexalith.ChatBot.Server/Observability/SubscriptionExpiryAlertEvaluator.cs` (pure static)
- `src/Hexalith.ChatBot.Server/Observability/AuthorizationFailureSpikeEvaluator.cs` (pure static)
- `src/Hexalith.ChatBot.Server/Gateway/IAuthorizationFailureCounter.cs` (interface + `AuthorizationFailureReading` record)
- `src/Hexalith.ChatBot.Server/Gateway/InMemoryAuthorizationFailureCounter.cs` (in-memory, thread-safe, sliding window)
- `src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/AuditProjectionLagAlertEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/RetryExhaustionAlertEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/ApprovalQueueAgeAlertEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/SubscriptionExpiryAlertEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/AuthorizationFailureSpikeEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/OperationalAlertPayloadValidatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Notifications/OperationalAlertWiringCoordinatorTests.cs`

**Modified files:**
- `src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs` (add 4 new kinds: `AuditProjectionLagBreached`, `SubscriptionExpiryImminent`, `ApprovalQueueAgeBreached`, `AuthorizationFailureSpike`)
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (add `OperationalAlertFired` factory method)
- `src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs` (wire `IRetryExhaustionAlertSource.Signal` inside `RecordRetryExhausted` — non-throwing)
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` (register 2 new singletons + coordinator)
- `src/Hexalith.ChatBot.Server/Gateway/ParticipantAuthorizationStage.cs` (or the authorization rejection site: call `IAuthorizationFailureCounter.Record(tenantId, now)` in the fail-closed path after emitting `ChatBotAuthorizationFailureAuditFact`)

### References

- `_bmad-output/planning-artifacts/epics.md#Story 8.4` — primary acceptance criteria source
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR43` (line 1431) — the five default alert thresholds
- `...prd.md#NFR42` (line 1429) — alert payload must carry affected scope, owner role, next safe action; no restricted detail
- `...prd.md#NFR2` — no restricted project names, file metadata, candidate evidence, audit details in alert payload
- `...prd.md#NFR15a` — fail-closed: no delivery when audit writer unavailable
- `_bmad-output/implementation-artifacts/8-3-slo-publication-and-error-budgets.md` — SLO catalog (alert threshold tokens), `ErrorBudgetBurnEvaluator` pattern, fail-safe doctrine, File-List honesty lessons
- `_bmad-output/implementation-artifacts/8-1-operational-dashboards-s8-s10.md` — `AdminQueueSummaryProjectionItem`, `AuditProjectionLagEvaluator`, `OperationalQueueFamily`, dashboard read policy
- `_bmad-output/implementation-artifacts/8-2-operational-telemetry-emission.md` — `IChatBotMetrics.RecordRetryExhausted`, `IAuditProjectionLagSource`, `ChatBotOperationClasses`
- Source anchors: `Server/Notifications/ReviewerBacklogAlertCoordinator.cs`, `Server/Notifications/ReviewerBacklogEvaluator.cs`, `Server/Notifications/ReviewerBacklogAlert.cs`, `Server/Audit/{OperatorAlertKind,OperatorAlert,IOperatorAlertSink,AuditEnvelopeFactory}.cs`, `Server/Projections/AuditProjectionLagEvaluator.cs`, `Server/Observability/{IAuditProjectionLagSource,IChatBotMetrics,ChatBotMetrics,ErrorBudgetBurnEvaluator,OperatingBaselineCatalogProvider}.cs`, `Server/Gateway/{ChatBotAuthorizationFailureAuditFact,CommandGatewayServiceCollectionExtensions}.cs`, `Contracts/Queries/{OperatingBaselineContracts,OperationalDashboardContracts}.cs`, `Contracts/Enums/MailboxDegradationReasonCode.cs`

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md` (Epic 8 overview + Story 8.1–8.5 for scope boundaries; Story 8.4 acceptance criteria)
- Loaded `prd_content` (NFR43 alert thresholds, NFR42 alert payload, NFR2 tenant-safe redaction, NFR15a fail-closed)
- Loaded previous in-epic stories: Story 8.3 (`edd55f9`) — SLO catalog + alert threshold tokens + fail-safe doctrine + review lessons; Story 8.2 — `IChatBotMetrics`/`IAuditProjectionLagSource`; Story 8.1 — `AdminQueueSummaryProjectionItem`/`AuditProjectionLagEvaluator`
- Loaded Story 7.10 (`ReviewerBacklogAlertCoordinator`/`ReviewerBacklogEvaluator`) as the primary structural pattern
- Inspected current source: `OperatorAlertKind.cs`, `OperatorAlert.cs`, `IOperatorAlertSink.cs`, `InMemoryOperatorAlertSink.cs`, `AuditEnvelopeFactory.cs` (ReviewerBacklogAlertFired pattern), `ReviewerBacklogAlertCoordinator.cs`, `ReviewerBacklogEvaluator.cs`, `ReviewerBacklogAlert.cs`, `AuditProjectionLagEvaluator.cs`, `IAuditProjectionLagSource.cs`, `ChatBotMetrics.cs` (RetryExhaustedInstrumentName/RecordRetryExhausted), `ChatBotAuthorizationFailureAuditFact.cs`, `MailboxDegradationReasonCode.cs`, `AdminQueueSummaryProjectionItem.cs`, `OperatingBaselineContracts.cs` (13-SLO catalog + IsSafeToken validator), `ErrorBudgetBurnEvaluator.cs`, `CommandGatewayServiceCollectionExtensions.cs` (DI registrations lines 149-166), `NotificationRoutingResolver.cs`, `NotificationStateEvent.cs`, `NotificationContentVisibility.cs`
- Reviewed git history: recent commits `edd55f9` (8.3), `073d762` (8.2), `f47715c` (8.1) — confirming fail-safe/no-fabricated-value doctrine and file-list honesty as recurring review lessons

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m]

### Debug Log References

- `dotnet build src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj` — fixed CA1822 (made `OperationalAlertWiringCoordinator.ResolveDeliveries` static); then clean.
- `dotnet build tests/Hexalith.ChatBot.Server.Tests` — fixed CS0050/CS0051 (internal `OperationalAlertPayload` cannot appear in a public `[MemberData]`/`[Theory]` signature; replaced with a single `[Fact]` iterating the five payloads); then clean.
- `Hexalith.ChatBot.Server.Tests` — Total 1066, Failed 0.
- `Hexalith.ChatBot.Architecture.Tests` — Total 37, Failed 0.
- `Hexalith.ChatBot.Conformance.Tests` — Total 75, Failed 0.
- `Hexalith.ChatBot.Contracts.Tests` — Total 298, Failed 0; `Hexalith.ChatBot.UI.Tests` — Total 120, Failed 0 (no regressions).

### Completion Notes List

- Delivered the five NFR43 default alert thresholds as pure static evaluators feeding one fail-closed
  `OperationalAlertWiringCoordinator` that audits pre-commit, then delivers via `INotificationSink` only on audit
  success (AC1–AC9), exactly mirroring the Story 7.10 `ReviewerBacklogAlertCoordinator` discipline.
- One shared metadata-only `OperationalAlertPayload` carries only safe tokens; `OperationalAlertPayload.Validate`
  reuses the operational-dashboard ASCII/marker-ban posture (`OperationalDashboardContractValidator.IsRequiredSafeToken`)
  and validates each space-separated `AffectedScope` component individually so the `tenant:{ref} mailbox:{ref}` form
  stays safe (NFR2/NFR42).
- **Deviation (field name):** AC4/the task bullet referenced an `AdminQueueSummaryProjectionItem.ReasonCode` field that
  does not exist; the degradation reason is carried by `FailureState`. `SubscriptionExpiryAlertEvaluator` reads
  `FailureState` (normalized: lowercase, hyphens removed; matches `graphsubscriptionexpired`/`subscriptionexpired`).
- **Deviation (threshold semantics):** AC3 states "≥ 172,800" (inclusive) while the test bullet said "exactly at
  threshold → no fire". The AC is authoritative — `ApprovalQueueAgeAlertEvaluator` fires at-or-over the threshold; the
  boundary test asserts fire at exactly the threshold and suppress just under it.
- **Deviation (retry source shape):** the task suggested a "volatile bool"; implemented a per-tenant thread-safe set
  (`IRetryExhaustionAlertSource.ReadAndClear(string tenantId)`) so concurrent per-tenant passes never consume each
  other's signals (correctness over the literal hint). `ChatBotMetrics.RecordRetryExhausted` signals it after the OTel
  counter increment, non-throwing/exception-isolated.
- **Deviation (auth wiring site):** the task named `ParticipantAuthorizationStage`, but the
  `ChatBotAuthorizationFailureAuditFact` is actually emitted in `CommandGateway` (the central `DenyAsync` plus the
  allowlist-denial site). Hooked `IAuthorizationFailureCounter.Record(tenantId, clock.UtcNow)` at both post-audit sites
  (tenant only, never actor/command/reason — NFR2), via a new optional ctor param so existing constructions are
  unaffected.
- `InMemoryAuthorizationFailureCounter` is a sliding (not tumbling) rolling window: `ReadAndReset` prunes out-of-window
  events and retains in-window ones so a sustained spike keeps being reported; injected `ISystemClock` keeps pruning
  deterministic under test.
- `AuditEnvelopeFactory.OperationalAlertFired` writes a pre-commit, metadata-only envelope (CommandName
  `OperationalAlertFired`, `Open->Alerted`, decision `alert`, `MetadataOnlyDecision`), folding the single scope space to
  the safe `|` token so all refs stay safe tokens.
- No `.Contracts` types added; `NotificationStateClass` reused (Degraded / Retry / ApprovalPending) — no contract enum
  change. All new types are internal to `.Server`.

### File List

**New:**
- `src/Hexalith.ChatBot.Server/Observability/OperationalAlertPayload.cs`
- `src/Hexalith.ChatBot.Server/Observability/AuditProjectionLagAlertEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Observability/RetryExhaustionAlertEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Observability/IRetryExhaustionAlertSource.cs`
- `src/Hexalith.ChatBot.Server/Observability/InMemoryRetryExhaustionAlertSource.cs`
- `src/Hexalith.ChatBot.Server/Observability/ApprovalQueueAgeAlertEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Observability/SubscriptionExpiryAlertEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Observability/AuthorizationFailureSpikeEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Gateway/IAuthorizationFailureCounter.cs`
- `src/Hexalith.ChatBot.Server/Gateway/InMemoryAuthorizationFailureCounter.cs`
- `src/Hexalith.ChatBot.Server/Notifications/OperationalAlertWiringCoordinator.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/AuditProjectionLagAlertEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/RetryExhaustionAlertEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/ApprovalQueueAgeAlertEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/SubscriptionExpiryAlertEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/AuthorizationFailureSpikeEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/OperationalAlertPayloadValidatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Notifications/OperationalAlertWiringCoordinatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Audit/AuditEnvelopeFactoryOperationalAlertTests.cs`

**Modified:**
- `src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/ChatBotMetricsTests.cs`

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-03 · **Outcome:** Approve (auto-fixed)

**Method:** Adversarial validation of every File-List file against the 9 ACs and every `[x]` task, cross-referenced with git reality, plus a full local re-run of build + Server/Architecture/Conformance suites.

**Verification evidence (independently re-run):**
- `dotnet build Hexalith.ChatBot.slnx` — Build succeeded, 0 Warning(s), 0 Error(s) (warnings-as-errors honoured).
- `Hexalith.ChatBot.Server.Tests` — Total **1066**, Failed 0.
- `Hexalith.ChatBot.Architecture.Tests` — Total **37**, Failed 0.
- `Hexalith.ChatBot.Conformance.Tests` — Total **75**, Failed 0.

**AC validation:** AC1–AC9 all IMPLEMENTED. Fail-closed pre-commit-audit-then-deliver (AC1/AC7/NFR15a) is correctly replicated from `ReviewerBacklogAlertCoordinator`; the metadata-only `OperationalAlertPayload` + `Validate` reuse the dashboard ASCII/marker-ban posture and split-validate the `tenant:{ref} mailbox:{ref}` scope per component (AC6/NFR2/NFR42); each of the five evaluators has positive + negative coverage; non-human/unscoped recipients are denied delivery while alerts still audit (AC9).

**Findings:**
- 🔴 CRITICAL: none. Every `[x]` task is genuinely implemented and test-backed.
- 🟡 MEDIUM (fixed): File List omitted three changed test files — `Audit/AuditEnvelopeFactoryOperationalAlertTests.cs` (new), `Gateway/CommandGatewayTests.cs` (modified), `Observability/ChatBotMetricsTests.cs` (modified). Added to the File List (File-List-honesty lesson from 8.3).
- 🟢 LOW (fixed): stale Debug Log count corrected 1059 → 1066.
- 🟢 LOW (noted, no change): `IAuthorizationFailureCounter.ReadAndReset` is a sliding window and does not reset — intentional, documented in XML doc + completion notes, and matches the task-specified interface name; renaming a spec-named seam carries more risk than the well-documented name.

**Documented deviations reviewed and accepted:** `FailureState` (not `ReasonCode`) as the degradation-reason field; AC3 inclusive `≥` threshold (AC authoritative over the test bullet); per-tenant retry source instead of a `volatile bool`; auth-counter wired at the two `CommandGateway` denial sites (not `ParticipantAuthorizationStage`). All are correct and audit-safe.

---

### Re-review (AI) — 2026-06-11

**Reviewer:** Jérôme Piquot · **Outcome:** Approve (auto-fixed) · **Trigger:** story-automator review re-run with an uncommitted working-tree change to `Notifications/OperationalAlertWiringCoordinatorTests.cs`.

**Method:** Re-validated all nine ACs against the committed implementation (commit `4f46efd`, confirmed ancestor of HEAD) plus the uncommitted test delta; cross-checked the File List against the actual commit tree; full local re-run of build + Server/Architecture/Conformance suites.

**Verification evidence (independently re-run, repo now many stories ahead of 8.4):**
- `dotnet build Hexalith.ChatBot.slnx` — Build succeeded, 0 Warning(s), 0 Error(s) (warnings-as-errors honoured).
- `Hexalith.ChatBot.Server.Tests` — Total **1607**, Failed 0.
- `Hexalith.ChatBot.Architecture.Tests` — Total **39**, Failed 0.
- `Hexalith.ChatBot.Conformance.Tests` — Total **93**, Failed 0.

**File-List honesty:** the committed source/test set for `4f46efd` (16 source + 10 test files) matches the story File List exactly — no undocumented files. The uncommitted change adds two tests to the already-listed `OperationalAlertWiringCoordinatorTests.cs` (no new file).

**Working-tree test delta reviewed (kept):** two strong new coordinator tests — `AllFiveAlertsRouteOnlyToExpectedHumanOwnerRolesAsMetadataRedactedDeliveries` (each of the five alert kinds routes to exactly its expected `AdminRole`/recipient as a `MetadataRedacted`, see-only in-app delivery; a `policy-admin` candidate receives nothing; payload JSON carries no `project-`/`@`/`secret`/`TopSecret`) and `UnscopedHumanPrincipalCannotReceiveDeliveryButAlertsStillAudit` (an unscoped human principal yields Fired 5 / Delivered 0 / 5 audit envelopes — AC1/AC9 deny-but-still-audit). Both pass; they tighten AC6/AC9 coverage and are retained.

**Findings:**
- 🔴 CRITICAL: none. Every `[x]` task remains genuinely implemented and test-backed; all five evaluators are actually invoked by `OperationalAlertWiringCoordinator.CollectAlerts` (not merely unit-tested in isolation).
- 🟡 MEDIUM: none.
- 🟢 LOW (fixed): `OperationalAlertPayload.Validate` split `AffectedScope` twice and carried an unreachable `.Length == 0` clause after the `IsNullOrWhiteSpace` guard. Refactored to split once into `scopeComponents` and order the empty/component checks correctly — behaviour-preserving (Server suite re-run green).

**Outcome:** Approve. No critical/high issues; Status remains `done`.

## Change Log

| Date | Change |
|---|---|
| 2026-06-03 | Story 8.4 implemented: five NFR43 tenant-safe alert evaluators + fail-closed `OperationalAlertWiringCoordinator`, shared metadata-only `OperationalAlertPayload` + validator, retry-exhaustion source, authorization-failure rolling-window counter, pre-commit `OperationalAlertFired` audit envelope, DI registrations, and full unit/coordinator test coverage. All Server (1059), Architecture (37), Conformance (75), Contracts (298), UI (120) tests green. Status → review. |
| 2026-06-03 | Senior Developer Review (AI): adversarial review + local re-verification (Server 1066, Architecture 37, Conformance 75; build clean). 0 critical, 0 high. Auto-fixed File List (3 undocumented test files) and stale Debug Log test count (1059 → 1066). Status → done. |
| 2026-06-11 | Re-review (AI): adversarial re-validation against committed `4f46efd` + uncommitted coordinator-test delta; full re-verification (Server 1607, Architecture 39, Conformance 93; build clean, warnings-as-errors). 0 critical, 0 high. Auto-fixed 1 LOW (`OperationalAlertPayload.Validate` redundant double-split + dead `.Length == 0` clause). File List matches commit tree exactly; two new coordinator tests retained. Status remains done. |
