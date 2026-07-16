---
baseline_commit: 073d762
---

# Story 8.3: SLO publication and error budgets

Status: done

<!-- Validation: create-story checklist applied 2026-06-03. -->

## Story

As an operator,
I want each operational SLO published with its target, measurement window, error budget, and the alert threshold that consumes the budget — and the current error-budget burn shown in the per-tenant operational view to authorized operators only,
so that error budgets are visible, calibrated, and consumable by alerting (Story 8.4) without leaking restricted tenant detail.

## Acceptance Criteria

1. Given each operational SLO in the NFR42a catalog, when published, then it carries — at minimum — a stable **metric name**, a **target** (numeric value + unit, or an explicit `calibration-pending` token where no starter number exists), a **measurement window**, an **error budget**, the **alert threshold that consumes the budget**, a **calibration source** (the NFR/A11 origin of the target), and a **tenant scope** (platform-wide default or per-tenant override). The catalog covers, at minimum: ingestion latency, association/candidate-generation latency, ambiguous-resolution time, command-execution latency, audit projection lag, retry exhaustion rate, duplicate suppression rate, mailbox failure rate, approval-queue p95 age, AI mediation latency, correction-propagation latency, and mailbox-subscription expiry. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.3`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR42a` (line 1430); `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Operating Baselines` (lines 149-167); `src/Hexalith.ChatBot.Server/Observability/ChatBotOperationClasses.cs`]
2. Given the published catalog's initial target values, when they are set, then they come from the documented MVP defaults — **NFR24** (user-facing lookups p95 ≤ 2 s), **NFR25** (ambiguous-association candidate generation p95 ≤ 10 s), **NFR26** (CLI/MCP operation-identity return p95 ≤ 5 s), **NFR17a** (correction propagation p95 ≤ 10 min M0/M1), and the **NFR43** alert thresholds (subscription expiry within 7 days, retry exhaustion, audit projection lag > 5 min, approval items older than 2 business days) — and any SLO without a documented starter number is published with an explicit `calibration-pending` target and a `a11-pending` calibration source rather than a fabricated value, per A11 (starter values, pilot-calibrated). [Source: `...prd.md#NFR24` (line 1405); `#NFR25` (line 1406); `#NFR26` (line 1407); `#NFR17a` (line 1325); `#NFR43` (line 1431); `#A11` (line 1350); `src/Hexalith.ChatBot.Server/Projections/AuditProjectionLagEvaluator.cs` (degraded 100 / failed 1000 lag thresholds)]
3. Given the per-tenant operational view (the M2 S8 operational dashboard), when rendered, then the published SLO catalog **and** each SLO's current **error-budget burn** are surfaced as a metadata-only section alongside the existing FR67 health views, carrying only stable tokens (no raw percentiles, no restricted tenant/project detail). [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#S8` (line 535, "SLO/error-budget status from NFR42a"); `src/Hexalith.ChatBot.Contracts/Queries/OperationalDashboardContracts.cs`; `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor`]
4. Given those published SLOs and error-budget burn, when read, then they are **visible to authorized operators only** (NFR38): the read flows through the existing see-only human-admin dashboard read path (`OperationalDashboardReadPolicy` → `AdminQueueSummaryReadPolicy`), so non-human service/AI callers and unscoped principals are denied before state load with a safe reason code, and privileged diagnostic detail stays separated from user-visible status. [Source: `...prd.md#NFR38` (line 1425); `src/Hexalith.ChatBot.Server/Projections/OperationalDashboardReadPolicy.cs`; `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryReadPolicy.cs`; `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`]
5. Given current error-budget burn, when it is computed, then it is **fail-safe and deterministic**: burn is surfaced as a coarse, stable state enum (e.g. `within-budget` / `approaching` / `exhausted` / `unknown`) derived only from already-available server-side signals, and it reports `unknown` (never a fabricated `within-budget`) when the underlying metric/lag signal is unavailable — mirroring the Story 8.1/8.2 prefer-no-data-over-fabricated-health doctrine. Burn is never count-derived into a fake percentage shown as truth. [Source: `_bmad-output/implementation-artifacts/8-1-operational-dashboards-s8-s10.md#Completion Notes List` (fail-safe `Unknown`); `_bmad-output/implementation-artifacts/8-2-operational-telemetry-emission.md#Audit Projection Lag — Implementation Note`; `src/Hexalith.ChatBot.Server/Projections/OperationalDashboardProjector.cs` (BuildQueueView Unknown fail-safe)]
6. Given the published catalog, when authored, then it is **mirrored into `addendum.md` §Operating Baselines** (the NFR42a publication location, created at M2) with the same per-SLO fields, replacing the "deferred until M2" placeholder, so the documented catalog and the code-published catalog do not drift; the code remains the single source of truth and the doc reflects it. [Source: `...addendum.md#Operating Baselines` (lines 149-167, "created at M2 release", "Until M2 release, the catalog is deferred"); `...prd.md#NFR42a` (line 1430)]
7. Given the published-SLO surface is read-only observability, when implemented, then it introduces **no** new `IChatBotCommand`, no `ChatBotSpineCommandAllowlist` entry, no gateway write stage, no audit-write envelope, and no new public OpenAPI endpoint — it reuses the generic transport and the existing dashboard read path exactly as Story 8.1 did (AC9), and it must not mutate project, queue, association, participant, approval, mailbox, policy, or audit state. SLO **alerting** (firing on threshold breach) and tenant-safe alert payloads are **out of scope** — they are Story 8.4. [Source: `_bmad-output/implementation-artifacts/8-1-operational-dashboards-s8-s10.md#Completion Notes List` (AC9 — generic transport reused, no public endpoint); `_bmad-output/planning-artifacts/epics.md#Story 8.4`; `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`]
8. Given acceptance coverage runs, then tests prove: the catalog provider returns one entry per required NFR42a metric with all seven addendum fields populated and the NFR-documented initial targets present (and `calibration-pending`/`a11-pending` used wherever no starter number exists, never a fabricated number); the catalog contract validates (finite tokens, bounded, defined enums, no duplicate/ missing required SLO); the error-budget burn evaluator returns `unknown` when its signal is absent and the correct coarse state when present, deterministically; the published SLOs + burn ride the operational-dashboard overview and are denied for a non-human/unscoped principal but allowed for a see-only human admin; and the addendum §Operating Baselines table matches the code catalog (no drift). [Source: `_bmad-output/planning-artifacts/architecture.md#Observability` (line 400); `tests/Hexalith.ChatBot.Server.Tests/Observability/`; `tests/Hexalith.ChatBot.Contracts.Tests/`]

## Tasks / Subtasks

- [x] Define the published-SLO contract (AC: 1, 3)
  - [x] Add `PublishedSlo` and the operating-baseline catalog records to `src/Hexalith.ChatBot.Contracts/Queries/` (a new `OperatingBaselineContracts.cs`, mirroring the shape and XML-doc rigor of `OperationalDashboardContracts.cs`). `PublishedSlo` carries exactly the addendum §Operating Baselines fields as bounded, low-cardinality safe tokens: `MetricName` (stable token, aligned to the Story 8.2 instrument names / `ChatBotOperationClasses` where one exists), `Target` (token, e.g. `p95<=2000ms` or `calibration-pending`), `MeasurementWindow` (token, e.g. `rolling-24h`), `ErrorBudget` (token, e.g. `0.1%` or `calibration-pending`), `AlertThreshold` (token, e.g. `lag>5m`), `CalibrationSource` (token, e.g. `nfr24` / `nfr43` / `a11-pending`), `TenantScope` (token, e.g. `platform-default`), and the coarse `ErrorBudgetBurnState` (see next task).
  - [x] Add an `ErrorBudgetBurnState` enum to `src/Hexalith.ChatBot.Contracts/Enums/` with `[EnumMember]` wire tokens, mirroring `ChatBotHealthStatus` / `ChatBotFreshnessState`: `WithinBudget` (`within-budget`), `Approaching` (`approaching`), `Exhausted` (`exhausted`), `Unknown` (`unknown`). Default/fail-safe is `Unknown`.
  - [x] Add a finite-token validator for the catalog (a static `OperatingBaselineContractValidator` mirroring `OperationalDashboardContractValidator`): all string fields are required safe tokens (reuse the existing `IsSafeToken` ASCII/marker-ban posture — no `secret`/`token`/`bearer`/`exception`/file-extension markers), `MetricName` unique (no duplicates), all required NFR42a metric names present (no missing), `ErrorBudgetBurnState` is a defined enum. No business logic, never inspects restricted detail.
- [x] Surface the catalog + burn on the per-tenant operational view (AC: 3, 4)
  - [x] Extend `OperationalDashboardOverview` (in `OperationalDashboardContracts.cs`) with an additive `IReadOnlyList<PublishedSlo> PublishedSlos` property so the published catalog + burn ride the **same** authorized S8 read as the FR67 health views (the dashboard IS the per-tenant operational view). Keep the change additive — do not alter the existing FR67 `Views` validation or break the Story 8.1 overview shape.
  - [x] Extend `OperationalDashboardContractValidator.Validate(OperationalDashboardOverview)` to validate each `PublishedSlo` via the new catalog validator (or call the catalog validator over `overview.PublishedSlos`). Preserve all existing overview/view validation (FR67 coverage, UTC freshness, no duplicates).
  - [x] Do **not** add a new public endpoint or a new `IChatBotClient` method: follow Story 8.1's AC9 posture (generic transport, no public OpenAPI surface). The published SLOs flow through the existing dashboard read the UI already consumes.
- [x] Build the catalog provider with NFR-documented initial values (AC: 1, 2, 6)
  - [x] Add an `OperatingBaselineCatalogProvider` (in `src/Hexalith.ChatBot.Server/Observability/` next to the Story 8.2 metrics seam, or `Server/Projections/` next to the dashboard projector) that returns the finite published-SLO catalog. Centralize the SLO metric-name token set as constants and align them to the Story 8.2 instrument names where one exists (`chatbot.ingestion.latency`, `chatbot.association.latency`, `chatbot.approval.latency`, `chatbot.command.execution.latency`, `chatbot.retry.exhausted`, `chatbot.duplicate.suppressed`, `chatbot.audit.projection.lag`) and use a stable `metric-pending` marker for catalog entries not yet emitted by an 8.2 instrument (mailbox failure rate, AI mediation latency, correction propagation, approval-queue p95 age, subscription expiry).
  - [x] Populate initial targets from the documented MVP defaults only: NFR24 → user-facing lookup / command-execution p95 `2000ms`; NFR25 → association/candidate-generation p95 `10000ms`; NFR26 → CLI/MCP operation-identity p95 `5000ms`; NFR17a → correction-propagation p95 `10m` (M0/M1); NFR43 → audit-projection-lag alert `>5m` (with the evaluator's degraded-100/failed-1000 event thresholds as the budget bands), approval-queue age alert `>2-business-days`, subscription-expiry alert `<=7d`, retry-exhaustion alert `on-exhaustion`. For every SLO without a documented starter number (e.g. mailbox-failure-rate, duplicate-suppression-rate, AI-mediation-latency, ingestion latency target), publish `Target = calibration-pending`, `CalibrationSource = a11-pending` — **never fabricate a number** (A11; mirrors the 8.1/8.2 no-fabricated-value lesson).
  - [x] Measurement windows and error budgets use stable bounded tokens (e.g. `rolling-24h`, `rolling-7d`, `0.1%`, `calibration-pending`). Keep the whole set finite and validate it against the catalog validator at provider construction (or via a test).
- [x] Implement the fail-safe error-budget burn evaluator (AC: 5)
  - [x] Add an `ErrorBudgetBurnEvaluator` (static, deterministic — mirror `AuditProjectionLagEvaluator`) that maps an available server-side signal to a coarse `ErrorBudgetBurnState`. Derive burn only from already-available signals (e.g. the `AuditProjectionLagStatus` health for the audit-projection-lag SLO; absence of a queryable metric backend → `Unknown`). Do **not** query OTel histograms directly or fabricate percentile math in this story.
  - [x] Return `ErrorBudgetBurnState.Unknown` whenever the underlying signal is unavailable (no metric backend, no lag reading) — never a fabricated `WithinBudget`. Map a present-and-healthy signal to `WithinBudget`, degraded/approaching-threshold to `Approaching`, and breached/failed to `Exhausted`. Keep the mapping a pure function (no clock/IO beyond the passed-in `nowUtc`/status) so it is deterministic and unit-testable.
  - [x] Wire the evaluator output into each `PublishedSlo` inside the dashboard projector (`OperationalDashboardProjector.Create`), so each published SLO carries its current coarse burn state. SLOs whose signal is not yet wired carry `Unknown` (honest no-data), consistent with the AI-outcome view's `Unknown` default.
  - [x] Surface only the coarse state token — never a raw percentile, raw event count, or restricted detail.
- [x] Render the published SLOs in the dashboard UI (AC: 3, 4)
  - [x] Extend `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor` (and the Fluxor `State/OperationalDashboards/*` if a projection is needed) with a metadata-only "Published SLOs / Error budgets" section iterating `overview.PublishedSlos`, rendering each SLO's metric name, target, window, error budget, alert threshold, calibration source, tenant scope, and the coarse burn state — reusing the existing `ChatBotStatusBanner`/`chatbot-table` patterns and the established WCAG 2.2 AA structure (Story 8.1, NFR60). Use stable `data-` tokens and the existing `UiText`/localization keys pattern (English + French).
  - [x] Update `src/Hexalith.ChatBot.UI/Services/OperationalDashboardService.cs` placeholder so its fail-safe overview also carries an (empty or `Unknown`-burn) `PublishedSlos` list until the server read is wired, keeping the UI buildable and the contract satisfied.
  - [x] Keep the authorization story unchanged: the whole overview is already see-only-operator-gated; do not add a separate SLO authorization path (NFR38 is satisfied by reuse).
- [x] Mirror the catalog into the addendum (AC: 6)
  - [x] Replace the "deferred until M2" placeholder in `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Operating Baselines` with a table of the published catalog (one row per SLO, the seven fields), matching the code provider exactly. Note in the doc that the code provider is the single source of truth and that numeric targets marked `calibration-pending` are filled after the A11 baseline run.
- [x] Add focused tests (AC: 8)
  - [x] Catalog-provider tests (`tests/Hexalith.ChatBot.Server.Tests/Observability/`): one entry per required NFR42a metric; every entry has all seven fields; NFR-documented targets present (`2000ms`/`10000ms`/`5000ms`/`10m`/`>5m`/`>2-business-days`/`<=7d`); `calibration-pending` + `a11-pending` used (and asserted) wherever no starter number exists; the whole catalog passes the catalog validator.
  - [x] Contract-validator tests (`tests/Hexalith.ChatBot.Contracts.Tests/`): the catalog validator rejects unsafe/secret-bearing tokens, duplicate metric names, missing required SLOs, and undefined `ErrorBudgetBurnState`; the extended `OperationalDashboardContractValidator` still enforces FR67 view coverage and now also validates `PublishedSlos`.
  - [x] Burn-evaluator tests: `Unknown` when the signal is absent; `WithinBudget`/`Approaching`/`Exhausted` for healthy/degraded/failed signals; pure-function determinism (same inputs → same output).
  - [x] Dashboard-projector tests (extend the existing `OperationalDashboardProjector` tests): the overview now carries `PublishedSlos` with per-SLO burn; SLOs without a wired signal carry `Unknown`; the overview still validates.
  - [x] Authorization tests (reuse the existing dashboard read-policy tests): a see-only human admin is allowed to read the overview incl. published SLOs; a non-human service/AI principal and an unscoped principal are denied before state load with a safe reason (NFR38).
  - [x] Addendum-drift test (or a catalog-export assertion): assert the addendum §Operating Baselines rows match the code catalog metric-name set (catch doc/code drift — the exact 8.1/7.5 File-List/claims discipline).
  - [x] UI tests (`tests/Hexalith.ChatBot.UI.Tests/`) if the bUnit dashboard test exists: the SLO section renders one row per published SLO with the coarse burn token and no restricted detail.
  - [x] Architecture/conformance suites only if module boundaries change (they should not — the catalog lives in `.Server` + `.Contracts`, no public contract/actor-isolation change).

## Dev Notes

### Scope Boundaries

- Story 8.3 delivers the **published SLO catalog** (target, measurement window, error budget, alert threshold, calibration source, tenant scope per SLO — the addendum §Operating Baselines fields) and the **current error-budget burn** surfaced on the per-tenant operational view to authorized operators only. It publishes the *definitions and current coarse burn*; it does **not** compute real percentile distributions from OTel histograms (that backend math is a calibration/A11 concern beyond this story's fail-safe coarse mapping).
- It **builds on**: Story 8.1 (the operational dashboard read surface, read policy, freshness policy, health/freshness enums) and Story 8.2 (the metric instrument names + operation-class taxonomy + `AuditProjectionLagEvaluator`). It reuses those — it does not re-derive lag, re-emit metrics, or re-implement the dashboard read.
- It does **not** implement: alert **firing** on threshold breach, tenant-safe alert payloads, or owner-role routing — that is **Story 8.4** (tenant-safe alert wiring). 8.3 publishes the thresholds 8.4 will consume. Degraded-state operability + runbook diagnostics are **Story 8.5**.
- It does **not** add a public OpenAPI endpoint, an `IChatBotCommand`, an allowlist entry, a gateway write stage, or an audit-write envelope. It is a read-only catalog provider + an additive field on the existing dashboard overview + a UI section + a doc mirror.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Contracts/Queries/OperationalDashboardContracts.cs` — the **pattern to mirror** for the new SLO contract: a query record, a result record, a per-row record, and a static finite-token validator (`IsSafeToken`, `ContainsSensitiveMarker` marker-ban list: `secret`/`password`/`bearer`/`token`/`exception`/`.txt`/`.json`/`.xml`). Extend `OperationalDashboardOverview` here with the additive `PublishedSlos` list and extend its validator.
- `src/Hexalith.ChatBot.Contracts/Enums/ChatBotHealthStatus.cs`, `ChatBotFreshnessState.cs`, `DashboardObservabilityView.cs` (+ `DashboardObservabilityViews.cs`) — the `[EnumMember]` wire-token enum + `All`/`ToWireValue` helper pattern to mirror for `ErrorBudgetBurnState`.
- `src/Hexalith.ChatBot.Server/Projections/OperationalDashboardProjector.cs` — `Create(queueItems, auditLag, aiOutcome, nowUtc, correlationId, aggregationLimit)`; aggregates the metadata-only overview, uses `ChatBotHealthStatus.Unknown` fail-safe for empty sources, `SafeSummaryToken`, `OperationalDashboardFreshnessPolicy.Classify`. Wire the published-SLO catalog + per-SLO burn in here.
- `src/Hexalith.ChatBot.Server/Projections/OperationalDashboardReadPolicy.cs` → `AdminQueueSummaryReadPolicy.Evaluate(principal, aggregationCount, auditThreshold, auditAvailable)` → `AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.SeeOnly)` (human-actor-only + see-only scope; fail-closed when audit unavailable above threshold). This **is** the NFR38 gate — reuse verbatim, add no new authorization path.
- `src/Hexalith.ChatBot.Server/Observability/` (Story 8.2): `ChatBotMetrics.cs` (instrument-name constants `IngestionLatencyInstrumentName` = `chatbot.ingestion.latency`, etc.), `ChatBotOperationClasses.cs` (the finite token set `message-intake`/`association`/`approval`/`command-execution`/`retry`/`duplicate-handling`/`audit-projection-lag` + `IsKnown`/`All`). Align SLO `MetricName` tokens to these.
- `src/Hexalith.ChatBot.Server/Projections/AuditProjectionLagEvaluator.cs` — `Evaluate(...) → AuditProjectionLagStatus(Health, LagIndicator, LagEvents, FreshnessTimestampUtc)`; constants `IndicatorUnknown`/`IndicatorCurrent`/`IndicatorLagging`/`IndicatorCriticalLag`, `DefaultDegradedLagThreshold = 100`, `DefaultFailedLagThreshold = 1000`. This is the **template for `ErrorBudgetBurnEvaluator`** (pure static fail-safe mapping) and the source signal for the audit-projection-lag SLO's burn.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` (~lines 165-166) — DI composition where `IAuditProjectionLagSource`/`IChatBotMetrics` singletons register. Register any new provider here if it needs DI (prefer a pure static provider/evaluator if no state is required).
- `src/Hexalith.ChatBot.Server/Notifications/ReviewerBacklogAlertCoordinator.cs` / `ReviewerBacklogEvaluator` — the existing **threshold → evaluate** precedent (Story 7.10). Note it for Story 8.4's alert wiring; for 8.3 only the *publication* of the threshold is in scope.
- UI: `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor` (the `chatbot-table` / `ChatBotStatusBanner` / `UiText[ChatBotUiTextKey.*]` render pattern, `data-chatbot-*` stable tokens), `Services/OperationalDashboardService.cs` (fail-safe placeholder overview), `State/OperationalDashboards/*` (Fluxor Actions/Effects/Reducers/State/Feature). Extend the placeholder to carry `PublishedSlos`.

### Current State To Preserve

- **Fail-safe doctrine (8.1/8.2):** prefer `Unknown`/no-data over a fabricated value. The error-budget burn must report `Unknown` when its signal is absent; the catalog must publish `calibration-pending`/`a11-pending` (never a made-up number) where no starter value exists. Reviewers flagged fabricated-health and File-List/claims drift in 8.1/7.5 — keep claims honest and the doc/code in sync.
- **Metadata-only / summary-safe invariant:** the dashboard surface reads no project/evidence/file/mailbox/audit detail. SLO tokens are bounded, low-cardinality, ASCII-safe, marker-banned — no raw percentiles, event counts, secrets, or PII as published values.
- **Authorization floor (NFR38):** the see-only human-admin gate stays on the existing read path; non-human/unscoped callers are denied before state load. Do not introduce a second authorization path or weaken the fail-closed-when-audit-unavailable behavior.
- **No write path (AC7):** add no `IChatBotCommand`, no `ChatBotSpineCommandAllowlist` entry, no gateway write stage, no audit-write envelope, no public OpenAPI endpoint. Reuse generic transport (8.1 AC9). Module boundaries (NetArchTest): UI/CLI/MCP depend only on `IChatBotClient`; the catalog provider/evaluator stay internal to `.Server`; the contract records stay in `.Contracts`.
- **Additive contract change:** extending `OperationalDashboardOverview` with `PublishedSlos` must not break the Story 8.1 FR67 view validation, the existing UI render, or the freshness/health enums. Keep it additive and keep the existing tests green.
- **Stack & topology:** do not change target frameworks (`net10.0`, SDK `10.0.302`), central package management, exporter/OTLP config, the `Hexalith.ChatBot` meter/activity-source names, or Aspire/Dapr topology. Root submodule policy: initialize/update only root `.gitmodules` submodules; never recursive submodule commands.

### Architecture Guardrails

- New SLO contract + `ErrorBudgetBurnState` enum live in `.Contracts` (`Queries/OperatingBaselineContracts.cs`, `Enums/ErrorBudgetBurnState.cs`). The catalog provider + burn evaluator live in `.Server` (`Observability/` or `Projections/`). No new public contract on `IChatBotClient`, no generated-client change.
- This story is read-only/observability: it surfaces on the **trim-able dashboard read stage** (Story 8.1 layer), not the always-on emission layer — published SLO definitions are static catalog reads, distinct from the always-on metric emission of Story 8.2. (Architecture: "structured emission always-on; dashboards trim-able".)
- Burn evaluation is a **pure deterministic mapping** from an already-computed status to a coarse enum — no new IO, no clock beyond passed-in `nowUtc`, no OTel histogram querying. Keep it the same shape as `AuditProjectionLagEvaluator`.
- The catalog is a **finite, bounded** set of stable tokens validated by the contract validator. No raw JSON, no user-provided keys, no high-cardinality values.

### Error-Budget Burn — Implementation Note

NFR42a requires each SLO to publish its error budget and the alert threshold that consumes it; AC5 requires the *current burn* to be visible. Real burn = (observed bad-event rate ÷ error budget) over the window, which needs the OTel metric backend queried — out of scope here. Resolution for this story: surface a **coarse** burn state derived from signals already in-process. The only fully-wired signal today is `AuditProjectionLagStatus` (Story 8.2/8.1): map its `Health` to burn for the audit-projection-lag SLO (`Healthy→WithinBudget`, `Degraded→Approaching`, `Failed→Exhausted`, `Unknown→Unknown`). Every other SLO whose live signal is not yet wired publishes burn `Unknown` (honest no-data) — exactly the AI-outcome-view `Unknown` default posture. Document this in completion notes; full per-SLO burn from queried histograms is a follow-up (calibration/A11), not a fabricated value now.

### SLO Catalog Initial Values — Implementation Note

Publish initial targets **only** from documented MVP defaults; mark everything else `calibration-pending`/`a11-pending`:

| SLO (metric name) | Target (initial) | Window | Alert threshold | Calibration source |
| --- | --- | --- | --- | --- |
| command-execution / user-facing lookup latency | `p95<=2000ms` | `rolling-24h` | budget-burn | `nfr24` |
| association / candidate-generation latency | `p95<=10000ms` | `rolling-24h` | budget-burn | `nfr25` |
| ambiguous-resolution time | `calibration-pending` | `rolling-7d` | budget-burn | `a11-pending` |
| cli/mcp operation-identity latency | `p95<=5000ms` | `rolling-24h` | budget-burn | `nfr26` |
| audit-projection-lag | `<=5m` | `rolling-24h` | `lag>5m` (degraded@100ev / failed@1000ev) | `nfr43` |
| retry-exhaustion rate | `on-exhaustion` | `rolling-24h` | `any-exhaustion` | `nfr43` |
| duplicate-suppression rate | `calibration-pending` | `rolling-24h` | spike-baseline | `a11-pending` |
| mailbox-failure rate | `calibration-pending` | `rolling-24h` | budget-burn | `a11-pending` |
| approval-queue p95 age | `<=2-business-days` | `rolling-7d` | `age>2-business-days` | `nfr43` |
| ai-mediation latency | `calibration-pending` | `rolling-24h` | budget-burn | `a11-pending` |
| correction-propagation latency | `p95<=10m` | `rolling-24h` | budget-burn | `nfr17a` |
| mailbox-subscription expiry | `<=7d` | `rolling-7d` | `expiry<=7d` | `nfr43` |

Tokens are illustrative — the dev agent finalizes the exact stable token spelling, but each must be ASCII-safe, marker-free, and validated. `calibration-pending`/`a11-pending` are mandatory wherever the PRD gives no starter number (do not invent). Mirror this exact table into addendum §Operating Baselines (AC6).

### Previous Story Intelligence

- **Story 8.2 (operational telemetry emission, just completed, baseline `073d762`)** built the `Hexalith.ChatBot` meter, the seven FR94 instruments, `ChatBotOperationClasses` (the finite operation-class token set), `IAuditProjectionLagSource`/`UnavailableAuditProjectionLagSource` (fail-safe empty-readings default), and registered `IChatBotMetrics`/`IAuditProjectionLagSource` as singletons. Align SLO metric-name tokens to its instrument names; reuse its operation-class taxonomy; do **not** re-emit metrics. Its review re-confirmed the fail-safe (no fabricated value) and File-List-honesty lessons.
- **Story 8.1 (operational dashboards)** built `OperationalDashboardProjector`, `OperationalDashboardReadPolicy`, `OperationalDashboardFreshnessPolicy`, `AuditProjectionLagEvaluator`, and the `ChatBotHealthStatus`/`ChatBotFreshnessState`/`DashboardObservabilityView` enums; its AC9 decision reused the generic transport with **no public OpenAPI endpoint** — follow the same posture. Its review flagged: prefer `Unknown`/no-data over fabricated health (apply to burn + targets), and keep the File List exact / make only honest claims.
- **Story 7.5 / 7.10 (operational queue management / reviewer-backlog alerting)** established `AdminQueueSummaryProjectionItem`, the operational-queue families the dashboard maps, the see-only read policy, and the threshold→evaluate→audit→deliver alert pattern (`ReviewerBacklogAlertCoordinator`). Review lessons: stable enum tokens, deterministic values (no process-dependent hashing), exact File List.

### Latest Technical Specifics

- No external version research required. Use the repo-pinned stack: .NET SDK `10.0.302`, `net10.0`, central package management (no inline versions), xUnit v3, Shouldly, NSubstitute, Fluxor (UI), Fluent UI Blazor. Do not upgrade packages or change target frameworks, exporter config, the meter/activity-source names, or Aspire/Dapr topology / submodule pointers.
- Tests use compiled in-process xUnit v3 runners (VSTest can fail with `SocketException (13): Permission denied` in this sandbox). Observability tests in `tests/Hexalith.ChatBot.Server.Tests/Observability/` use `System.Diagnostics.Metrics.MeterListener` + Shouldly — but this story's units (catalog provider, burn evaluator, validators) are plain pure-function tests; no `MeterListener` needed unless a new instrument is added (it should not be).

### Testing Notes

- Minimum validation before dev handoff (build, then compiled in-process xUnit v3 runners):
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` (SLO contract + `ErrorBudgetBurnState` + validators; extended dashboard-overview validation)
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` (catalog provider, burn evaluator, dashboard projector `PublishedSlos`, read-policy authorization)
  - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` (SLO dashboard section render, if a bUnit dashboard test exists)
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` and `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` (only need to stay green — module boundaries / actor isolation should be unchanged)
- Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, metadata-only fixtures, Allman braces, English + French localization, and root-level submodule policy.

### Project Structure Notes

- New contract: `src/Hexalith.ChatBot.Contracts/Queries/OperatingBaselineContracts.cs` (`PublishedSlo`, catalog result + `OperatingBaselineContractValidator`); new enum `src/Hexalith.ChatBot.Contracts/Enums/ErrorBudgetBurnState.cs` (mirror the `[EnumMember]` + `All`/`ToWireValue` helper pattern).
- Modified contract: `src/Hexalith.ChatBot.Contracts/Queries/OperationalDashboardContracts.cs` (additive `PublishedSlos` on `OperationalDashboardOverview` + validator extension).
- New server: `src/Hexalith.ChatBot.Server/Observability/OperatingBaselineCatalogProvider.cs` + `ErrorBudgetBurnEvaluator.cs` (pure static, mirror `AuditProjectionLagEvaluator`).
- Modified server: `src/Hexalith.ChatBot.Server/Projections/OperationalDashboardProjector.cs` (populate `PublishedSlos` + per-SLO burn); DI registration in `Gateway/CommandGatewayServiceCollectionExtensions.cs` only if the provider needs DI.
- Modified UI: `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor`, `Services/OperationalDashboardService.cs`, `State/OperationalDashboards/*` as needed, plus the `ChatBotUiTextKey` localization entries (en + fr).
- Doc: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` §Operating Baselines (replace placeholder with the catalog table).
- Tests mirror source boundaries: `tests/Hexalith.ChatBot.Contracts.Tests`, `tests/Hexalith.ChatBot.Server.Tests/Observability/`, `tests/Hexalith.ChatBot.UI.Tests`.

### References

- `_bmad-output/planning-artifacts/epics.md#Epic 8` / `#Story 8.3` — source acceptance criteria (published SLO fields; per-tenant operational view; authorized operators only).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR42a` (line 1430) — SLOs published in the per-tenant operational view (M2) and addendum §Operating Baselines; each SLO has target, measurement window, error budget, alert threshold; initial values per NFR24–NFR27/NFR43; A11 calibration.
- `...prd.md#NFR24` (line 1405), `#NFR25` (1406), `#NFR26` (1407), `#NFR17a` (1325), `#NFR43` (1431) — the documented initial targets/thresholds.
- `...prd.md#NFR38` (line 1425) — user-visible status separated from privileged diagnostic detail, exposed per authorization level.
- `...prd.md#A11` (line 1350) — starter values calibrated against a 2–4 week pilot baseline; final targets after the baseline window.
- `...prd.md#S8` (line 535) — S8 operational dashboards include SLO/error-budget status from NFR42a.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Operating Baselines` (lines 149-167) — the per-SLO recorded fields + "created at M2" + "deferred until M2" placeholder this story replaces.
- `_bmad-output/planning-artifacts/architecture.md#Observability` (lines 400-401) — OpenTelemetry; structured emission always-on (dashboards trim-able); published SLOs (M2).
- `_bmad-output/implementation-artifacts/8-1-operational-dashboards-s8-s10.md` — dashboard read surface, read policy, freshness policy, enums, AC9 no-public-endpoint, fail-safe `Unknown`, File-List honesty.
- `_bmad-output/implementation-artifacts/8-2-operational-telemetry-emission.md` — meter instrument names, `ChatBotOperationClasses`, `AuditProjectionLagEvaluator`, fail-safe / no-fabricated-value doctrine.
- Source anchors: `Contracts/Queries/OperationalDashboardContracts.cs`, `Contracts/Enums/{ChatBotHealthStatus,ChatBotFreshnessState,DashboardObservabilityView}.cs`, `Server/Projections/{OperationalDashboardProjector,OperationalDashboardReadPolicy,OperationalDashboardFreshnessPolicy,AuditProjectionLagEvaluator,AdminQueueSummaryReadPolicy}.cs`, `Server/Governance/Admin/AdminAuthorityEvaluator.cs`, `Server/Observability/{ChatBotMetrics,ChatBotOperationClasses}.cs`, `Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`, `UI/Components/Pages/OperationalDashboards.razor`, `UI/Services/OperationalDashboardService.cs`, `UI/State/OperationalDashboards/*`.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md` (Epic 8 overview + Story 8.1–8.5 for scope boundaries; Story 8.3 acceptance criteria).
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md` (Observability decision — published SLOs M2, always-on emission vs trim-able dashboards).
- Loaded `prd_content` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` (NFR42a, NFR24–NFR27, NFR17a, NFR38, NFR43, A11, S8) and `addendum.md` §Operating Baselines (per-SLO fields + M2 placeholder).
- Loaded persistent project-context facts from sibling submodule `project-context.md` files (Commons, Memories, Folders, EventStore, Projects, Conversations, FrontComposer).
- Previous in-epic stories: Story 8.2 (telemetry emission) and Story 8.1 (operational dashboards) — loaded fully for the metric instrument names / operation-class taxonomy / `AuditProjectionLagEvaluator`, the dashboard read surface / read policy / freshness policy / enums, AC9 no-public-endpoint posture, and the fail-safe / File-List-honesty review lessons. Reviewed git history (`073d762` story-8.2 … `f47715c` story-8.1 …).
- Inspected current source: `OperationalDashboardContracts.cs` (contract + validator pattern, `IsSafeToken`/marker-ban), `OperationalDashboardProjector.cs` (overview aggregation, `Unknown` fail-safe), `OperationalDashboardReadPolicy.cs`/`AdminQueueSummaryReadPolicy.cs`/`AdminAuthorityEvaluator.cs`/`AdminScopes.cs` (NFR38 see-only human-admin gate), `OperationalDashboardFreshnessPolicy.cs` (5-min fresh / 15-min expiry), the Story 8.2 `Observability/*` seam (instrument-name constants, `ChatBotOperationClasses`, `AuditProjectionLagEvaluator` thresholds/indicators), the dashboard enums (`ChatBotHealthStatus`/`ChatBotFreshnessState`/`DashboardObservabilityView(s)`), and the UI render path (`OperationalDashboards.razor`, `OperationalDashboardService.cs`, Fluxor `State/OperationalDashboards/*`). Verified **no** existing SLO/ErrorBudget/Baseline construct in `src/` (only the static lag thresholds and the Story 7.x backlog/retry alert thresholds) — confirming this is net-new catalog + burn surfacing built on 8.1/8.2.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Opus 4.8, 1M context)

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 warnings, 0 errors.
- In-process xUnit v3 runners (`-parallel none`): Contracts.Tests **298**, Server.Tests **1018**, UI.Tests **120**, Architecture.Tests **37**, Conformance.Tests **75** — all passed, 0 failed, 0 skipped. No regressions; module boundaries / actor isolation unchanged.

### Completion Notes List

- **Published-SLO contract (AC1/AC3).** Added `PublishedSlo` (seven addendum fields as bounded ASCII-safe tokens + coarse `ErrorBudgetBurnState`) and `OperatingBaselineContractValidator` in `Contracts/Queries/OperatingBaselineContracts.cs`, plus the `ErrorBudgetBurnState` enum (+`ErrorBudgetBurnStates` wire helper) mirroring the `ChatBotHealthStatus`/`ChatBotFreshnessState` `[EnumMember]` pattern. `OperationalDashboardOverview` extended **additively** with a trailing optional `IReadOnlyList<PublishedSlo>? PublishedSlos = null` so all Story 8.1 call sites and tests stay green; the overview validator now validates a present catalog and permits its absence.
- **Catalog provider + burn evaluator (AC1/AC2/AC5/AC6).** The canonical catalog (13 SLOs covering the 12 AC1 metrics + the NFR26 cli/mcp operation-identity SLO) lives in `Contracts` as `OperatingBaselineCatalog.Published` — the **single source of truth** — because the UI depends only on the client/contract surface and never on `.Server`; a thin `Server/Observability/OperatingBaselineCatalogProvider` is the projector seam onto it. `ErrorBudgetBurnEvaluator.FromHealth` is a pure deterministic mapping (`Healthy→WithinBudget`, `Degraded→Approaching`, `Failed→Exhausted`, `Unknown/undefined→Unknown`), mirroring `AuditProjectionLagEvaluator`.
- **Deliberate token-spelling decisions (honest, reviewer-relevant).** (1) The `IsSafeToken` charset bans `<`, `>`, `=`, `%`, so the illustrative Dev-Notes tokens (`p95<=2000ms`, `lag>5m`, `0.1%`) were spelled ASCII-safe: `p95-le-2000ms`, `lag-gt-5m`, etc. (`le`=≤, `gt`=&gt;). (2) The Dev-Notes "`metric-pending` marker for not-yet-emitted SLOs" was **not** used as a `MetricName`, because the validator requires unique metric names and a shared `metric-pending` would violate it; instead every SLO carries a unique stable dotted `MetricName` (aligned to the Story 8.2 instrument name where one exists, e.g. `chatbot.ingestion.latency`). (3) Error-budget **fractions** are undocumented in the PRD, so they are published `calibration-pending` (A11) rather than a fabricated `0.1%`; only the audit-projection-lag budget bands (`degraded-100ev-failed-1000ev`) are documented.
- **Fail-safe burn (AC5).** Only the audit-projection-lag SLO has a live signal today; the projector layers its burn from the live `AuditProjectionLagStatus.Health`. Every other SLO carries the catalog's default `Unknown` burn (honest no-data, never a fabricated within-budget) — the AI-outcome-view `Unknown` posture. No OTel histogram is queried and no percentile math is fabricated.
- **Authorization (AC4) reused verbatim.** Published SLOs ride the **same** `OperationalDashboardOverview` through the existing `OperationalDashboardReadPolicy → AdminQueueSummaryReadPolicy → AdminAuthorityEvaluator` see-only human-admin gate. No new authorization path; the existing read-policy tests still deny non-human/unscoped principals and fail closed when audit is unavailable.
- **No write path (AC7).** No new `IChatBotCommand`, `ChatBotSpineCommandAllowlist` entry, gateway write stage, audit-write envelope, or public OpenAPI endpoint. Read-only catalog data + an additive overview field + a UI section + a doc mirror. Architecture/Conformance suites stayed green (no module-boundary change).
- **Doc mirror + drift guard (AC6/AC8).** Addendum §Operating Baselines now publishes the catalog table (replacing the "deferred until M2" placeholder) and names the code as single source of truth; `OperatingBaselineAddendumDriftTests` asserts the addendum metric-name set matches `OperatingBaselineCatalog.Published` (no doc/code drift).
- **Burn/alerting scope.** SLO **alerting** on threshold breach and tenant-safe alert payloads remain out of scope (Story 8.4); 8.3 only *publishes* the thresholds 8.4 will consume.

### File List

**Added**
- `src/Hexalith.ChatBot.Contracts/Enums/ErrorBudgetBurnState.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ErrorBudgetBurnStates.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/OperatingBaselineContracts.cs` (`PublishedSlo`, `OperatingBaselineMetrics`, `OperatingBaselineCatalog`, `OperatingBaselineContractValidator`)
- `src/Hexalith.ChatBot.Server/Observability/OperatingBaselineCatalogProvider.cs`
- `src/Hexalith.ChatBot.Server/Observability/ErrorBudgetBurnEvaluator.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OperatingBaselineContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OperatingBaselineAddendumDriftTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/OperatingBaselineCatalogProviderTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/ErrorBudgetBurnEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/OperationalDashboardsPublishedSlosE2ETests.cs` (QA-automation pass — browser + no-browser fallback E2E coverage of the published-SLO/error-budget dashboard section)

**Modified**
- `src/Hexalith.ChatBot.Contracts/Queries/OperationalDashboardContracts.cs` (additive `PublishedSlos` on `OperationalDashboardOverview` + validator extension)
- `src/Hexalith.ChatBot.Server/Projections/OperationalDashboardProjector.cs` (populate `PublishedSlos` + per-SLO burn)
- `src/Hexalith.ChatBot.UI/Services/OperationalDashboardService.cs` (placeholder carries the catalog with Unknown burn)
- `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor` (metadata-only "Published SLOs / Error budgets" section + burn helpers)
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs` (new SLO/burn keys + `All` registration)
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx` (en strings)
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx` (fr strings)
- `tests/Hexalith.ChatBot.Server.Tests/Projections/OperationalDashboardProjectorTests.cs` (PublishedSlos + burn coverage)
- `tests/Hexalith.ChatBot.UI.Tests/OperationalDashboardServiceTests.cs` (catalog rider coverage)
- `tests/Hexalith.ChatBot.UI.Tests/OperationalDashboardsComponentContractTests.cs` (SLO section render contract)
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` (§Operating Baselines catalog table — replaces the deferred placeholder)

### Change Log

- 2026-06-03 — Implemented Story 8.3 (SLO publication and error budgets): published-SLO contract + `ErrorBudgetBurnState` enum + validator; canonical `OperatingBaselineCatalog` (13 SLOs, NFR-documented starters + `calibration-pending`/`a11-pending`); fail-safe `ErrorBudgetBurnEvaluator`; projector wires `PublishedSlos` + per-SLO coarse burn onto the authorized S8 overview; UI "Published SLOs / Error budgets" section (en+fr); addendum §Operating Baselines mirror + drift guard. All ACs satisfied; full in-process test suites green. Status → review.
- 2026-06-03 — Senior Developer Review (AI), auto-fix. Verified all 8 ACs against implementation and re-ran build + all 5 suites (Contracts 298, Server 1018, UI 120, Architecture 37, Conformance 75 — all green, 0 failed). 0 CRITICAL / 0 HIGH / 0 MEDIUM. Fixed 2 LOW: (1) corrected stale Debug-Log test counts (289/1011 → 298/1018); (2) reordered `OperatingBaselineContractValidator.Validate(IReadOnlyList<PublishedSlo>)` so a null catalog element is recorded as `slo_invalid` rather than throwing past the dead `slo is not null` guard, with a covering test. Status → done.
- 2026-06-11 — QA-automation E2E pass + review (AI), auto-fix. Added `tests/Hexalith.ChatBot.UI.E2E.Tests/OperationalDashboardsPublishedSlosE2ETests.cs` (Story 8.3 published-SLO/error-budget dashboard section): a Playwright browser path asserting the metadata-only table renders one row per published SLO with all seven addendum fields, keyboard-reachable rows, stable `data-chatbot-slo-metric`/`data-chatbot-slo-burn` tokens, the audit-lag `approaching` burn, A11 `calibration-pending`/`a11-pending` entries, and no restricted/raw detail; plus a no-browser fallback asserting the same contract against `OperationalDashboards.razor`, `OperatingBaselineContracts.cs`, and the en/fr localization resources. Re-verified: `dotnet build Hexalith.ChatBot.slnx` 0 warnings/0 errors; compiled in-process xUnit v3 `Hexalith.ChatBot.UI.E2E.Tests -parallel none` → Total 105, Errors 0, Failed 0, Skipped 0 (browser path executed — chrome present). Adversarially cross-checked the test's 13 expected metric names and target/burn tokens against the live `OperatingBaselineCatalog.Published`, the razor markers, and both resx files — all match. 0 CRITICAL / 0 HIGH / 0 MEDIUM. 1 LOW: the new E2E test was missing from the File List — added above. Status stays done; sprint-status `8-3 = done` unchanged.

### Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-03. **Outcome:** Approve (auto-fix applied).

**Scope verified:** read all 9 added + 12 modified files; cross-referenced git working tree vs File List; validated each AC against code; ran `dotnet build` (0 warnings/0 errors) and all five in-process xUnit v3 suites (Contracts 298, Server 1018, UI 120, Architecture 37, Conformance 75 — all green).

**AC outcomes:** AC1 catalog of 13 SLOs covers all 12 required NFR42a metrics + NFR26 operation-identity, seven fields each — IMPLEMENTED. AC2 NFR24/25/26/17a starters present, NFR43 alert thresholds published, `calibration-pending`/`a11-pending` everywhere no starter exists — IMPLEMENTED. AC3 metadata-only "Published SLOs / Error budgets" UI section rides the FR67 overview — IMPLEMENTED. AC4 reuses `OperationalDashboardReadPolicy → AdminQueueSummaryReadPolicy` NFR38 gate, no new auth path — IMPLEMENTED. AC5 `ErrorBudgetBurnEvaluator.FromHealth` pure/deterministic, fail-safe `Unknown`, only audit-lag SLO wired — IMPLEMENTED. AC6 addendum §Operating Baselines table mirrors the code catalog with a metric-name + per-field drift guard — IMPLEMENTED. AC7 no `IChatBotCommand`/allowlist/gateway-write/audit-write/public endpoint; Architecture+Conformance green — IMPLEMENTED. AC8 catalog-provider, contract-validator, burn-evaluator, projector, authorization, addendum-drift, and UI-contract tests all present and real — IMPLEMENTED.

**Tasks audit:** every `[x]` task confirmed against code/tests; no false completions.

**File List vs git:** matches. The `Hexalith.Conversations`/`Hexalith.Parties` submodule pointer modifications are pre-existing working-tree drift unrelated to 8.3 — correctly excluded and untouched.

**Findings:** 0 CRITICAL / 0 HIGH / 0 MEDIUM. 2 LOW, both auto-fixed (see Change Log): stale debug-log counts; dead null-guard in the catalog validator (now records `slo_invalid` + covering test).

**Honest deviations confirmed sound:** ASCII-safe token spellings (`p95-le-2000ms`, `lag-gt-5m`) because `IsSafeToken` bans `<>=%`; unique dotted `MetricName`s instead of a shared `metric-pending` (validator requires uniqueness); error-budget fractions published `calibration-pending` rather than a fabricated `0.1%`. The dashboard component test is a source-text contract (Story 8.1 pattern — no bUnit render harness exists), which is the correct established approach.
