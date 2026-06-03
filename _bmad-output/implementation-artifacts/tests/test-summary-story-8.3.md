# Test Automation Summary — Story 8.3 (SLO publication and error budgets)

**Workflow:** bmad-qa-generate-e2e-tests · **Date:** 2026-06-03 · **Engineer role:** QA automation
**Framework:** xUnit v3 + Shouldly + NSubstitute (in-process runners, `-parallel none`) — the project's existing stack.
**Mode:** gap-fill — Story 8.3 shipped with tests; this run audited AC coverage and auto-applied the discovered gaps.

## Scope

Story 8.3 is read-only/observability (no HTTP endpoint, no UI E2E driver), so "E2E/automated tests" here means
unit + contract + component-contract tests in the existing framework. The pre-existing suites already covered the
catalog/validator/burn-evaluator/projector/UI-render happy paths. This run added coverage for the AC-mandated
fields and authorization composition that were not yet asserted.

## Discovered Gaps → Tests Added (auto-applied)

### Server — `tests/Hexalith.ChatBot.Server.Tests/Observability/OperatingBaselineCatalogProviderTests.cs`
- [x] **AC2** `Nfr43SlosShouldPublishTheDocumentedAlertThresholdAndMeasurementWindow` — the NFR43 alert thresholds
  (`lag-gt-5m`, `any-exhaustion`, `age-gt-2-business-days`, `expiry-le-7d`) and each SLO's measurement window were
  never asserted (only target + calibration source were). Now covered for all four NFR43 SLOs.
- [x] **AC2** `AuditProjectionLagShouldPublishTheDocumentedErrorBudgetBandsWhileOthersStayCalibrationPending` —
  asserts the only documented error budget (`degraded-100ev-failed-1000ev`) and that every other SLO stays
  `calibration-pending` (no fabricated budget fraction).
- [x] **AC1** `EverySloShouldPublishThePlatformDefaultTenantScope` — the tenant-scope field was unasserted.

### Server — `tests/Hexalith.ChatBot.Server.Tests/Projections/OperationalDashboardProjectorTests.cs`
- [x] **AC4/AC8** `PublishedSlosShouldRideTheGatedOverviewAllowedForHumanSeeOnlyAndDeniedForNonHumanAndUnscoped` —
  the read-policy gate and the SLO-bearing overview were tested separately; this composes them, proving the
  published SLOs ride the NFR38-gated overview (allowed for see-only human admin; denied for service/AI/unscoped).

### Contracts — `tests/Hexalith.ChatBot.Contracts.Tests/OperatingBaselineContractTests.cs`
- [x] **AC5/AC8** `TryFromWireValueShouldRejectUnknownTokensAndFailSafeToUnknown` (null/blank/garbage → false + Unknown).
- [x] **AC5** `ToWireValueShouldThrowForAnUndefinedBurnStateRatherThanEmitAFabricatedToken`.
- [x] **AC5** `AllShouldEnumerateExactlyTheDefinedBurnStates`.

### Contracts — `tests/Hexalith.ChatBot.Contracts.Tests/OperatingBaselineAddendumDriftTests.cs`
- [x] **AC6/AC8** `AddendumOperatingBaselinesRowsShouldMirrorEveryPublishedFieldNotJustTheMetricName` — the existing
  drift guard checked only the metric-name set; this catches silent VALUE drift (target/window/budget/threshold/
  calibration-source/tenant-scope) between the doc table and the code catalog.

## Coverage (AC8 acceptance-test obligations)

| AC8 obligation | Status |
| --- | --- |
| Catalog: one entry per required metric, all 7 fields, NFR targets, `calibration-pending`/`a11-pending` | ✅ pre-existing + **AC2/AC1 fields added** |
| Catalog contract validates (safe tokens, bounded, defined enum, no dup/missing) | ✅ pre-existing |
| Burn evaluator: `unknown` when absent, correct coarse state, deterministic | ✅ pre-existing + **enum robustness added** |
| Published SLOs + burn ride the overview | ✅ pre-existing (projector) |
| Denied for non-human/unscoped, allowed for see-only human admin | ✅ **composed gate test added** |
| Addendum table matches code catalog (no drift) | ✅ name guard + **per-field guard added** |

## Results

| Suite | Total | Failed | Skipped | Δ new |
| --- | --- | --- | --- | --- |
| Contracts.Tests | 297 | 0 | 0 | +8 |
| Server.Tests | 1018 | 0 | 0 | +7 |
| UI.Tests | 120 | 0 | 0 | 0 (regression check) |

Build: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → succeeded, 0 warnings, 0 errors.

## Next Steps

- Story 8.4 will add alert **firing** on threshold breach — the alert-threshold tokens this run pinned down are the
  contract 8.4 consumes; keep these threshold assertions as the regression anchor when 8.4 wires firing.
- When the A11 baseline run fills `calibration-pending` targets, the new field-level drift guard will enforce that
  the addendum and the code catalog are updated together.
