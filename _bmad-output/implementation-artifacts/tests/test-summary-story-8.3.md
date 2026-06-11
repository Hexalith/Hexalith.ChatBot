# Test Automation Summary

Story: 8.3 - SLO publication and error budgets
Date: 2026-06-11

## Generated Tests

### API Tests

- [x] No new public API test was generated. Story 8.3 is explicitly read-only observability and adds no public OpenAPI endpoint, command, gateway write stage, or audit-write envelope.
- [x] Existing in-process contract/server tests remain the applicable API-level coverage for the catalog provider, validator, burn evaluator, dashboard projector, read-policy authorization gate, and addendum drift guard.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/OperationalDashboardsPublishedSlosE2ETests.cs` - Adds Story 8.3 E2E coverage for the operational-dashboard "Published SLOs / Error budgets" section.
- [x] Browser path verifies the operator-facing table renders one row per published SLO, all seven addendum fields, keyboard-reachable rows, stable `data-chatbot-slo-metric` and `data-chatbot-slo-burn` tokens, the audit-lag `approaching` burn state, A11 `calibration-pending`/`a11-pending` entries, and metadata-only output.
- [x] No-browser fallback validates the same contract against `OperationalDashboards.razor`, `OperatingBaselineContracts.cs`, and English/French localization resources, matching the existing E2E suite pattern.

## Coverage

- Published SLO catalog: 13/13 expected Story 8.3 metrics covered in the E2E fixture and source fallback.
- SLO fields: metric name, target, measurement window, error budget, alert threshold, calibration source, tenant scope, and coarse burn state are asserted.
- Burn states: `approaching` is asserted for the wired audit projection lag signal; `unknown` is asserted for calibration/no-signal SLOs.
- Safety: test asserts no restricted project/evidence/mailbox detail and no raw percentile/event-count wording in the rendered operator view.
- API endpoints: 0 new endpoints applicable by story scope.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore` - Built the E2E assembly, then aborted under VSTest with `SocketException (13): Permission denied`, which is the known sandbox socket limitation.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - Total 105, Errors 0, Failed 0, Skipped 0.

## Checklist Validation

- [x] API tests generated if applicable: no new public API exists; existing contract/server tests are the applicable API-level coverage.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Microsoft.Playwright APIs.
- [x] Happy path covered.
- [x] Critical error/safety cases covered through no-browser fallback, metadata-only assertions, calibration-pending assertions, and burn-state assertions.
- [x] Tests use semantic Playwright locators (`role` heading/table) plus stable data tokens for SLO rows.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps added.
- [x] Tests are independent and follow the existing E2E harness pattern.
- [x] Test summary created with coverage metrics.
