# Test Automation Summary

Story: 8.1 - Operational dashboards (S8/S10)
Date: 2026-06-11

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Contracts.Tests/OperationalDashboardContractTests.cs` - Dashboard query/DTO validation, stable view/freshness wire tokens, bounded-staleness classification, metadata-only serialization, full FR67 view coverage, and degraded/failed scope/action requirements.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Projections/OperationalDashboardProjectorTests.cs` - Read aggregation, see-only authorization, service/AI/no-scope denial, audit-threshold fail-closed behavior, redacted detail-link states, audit projection lag health, published SLO burn state, and status-as-enum behavior.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.Tests/OperationalDashboardServiceTests.cs` - UI service boundary, `ChatBotSurfaceOrigin.Ui`, metadata-only overview, freshness states, and fail-safe published SLO catalog.
- [x] `tests/Hexalith.ChatBot.UI.Tests/OperationalDashboardsComponentContractTests.cs` - Governed UI primitives, localized visible text, non-color status, freshness labels, reachable disabled detail explanations, small-screen labelled-row contract, and metadata-only SLO section.
- [x] `tests/Hexalith.ChatBot.UI.Tests/OperationalDashboardsReducersTests.cs` - Operational dashboard Fluxor reducer loading, success, and safe failure states.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/OperationalDashboardsAccessibilityE2ETests.cs` - Playwright/fallback accessibility coverage for landmarks, keyboard-reachable rows, semantic token CSS, non-color status text, aria-live freshness announcements, and duplicate-announcement prevention.

## Coverage

- API/read contracts: dashboard query, overview DTOs, freshness policy, view tokens, safe-token validation, metadata-only serialization, and no-public-OpenAPI-change decision are covered.
- Server read behavior: six observability views plus audit projection lag, worst-health enum status, fail-safe unknown states, see-only human admin access, non-human and unscoped denial, detail redaction/escalation states, audit fail-closed read policy, and published SLO burn mapping are covered.
- UI/E2E surface: route composition, governed shell primitives, keyboard/focusable dashboard rows, aria-live freshness announcements, non-color labels, localization, responsive labelled rows, manual refresh affordance, and metadata-only SLO rendering are covered.
- Discovered gaps in this run: none requiring new test code. The existing Story 8.1 tests already satisfy the checklist and all affected lanes passed.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - Build succeeded, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - Total 482, Failed 0.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - Total 1605, Failed 0.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - Total 131, Failed 0.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - Total 104, Failed 0.

## Checklist Validation

- [x] API tests generated/validated where applicable.
- [x] E2E tests generated/validated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, bUnit/source-contract, and Playwright APIs.
- [x] Happy path covered.
- [x] Critical error behavior covered.
- [x] Tests use semantic accessible locators where UI E2E applies.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps added.
- [x] Tests are independent.
- [x] Test summary created with coverage metrics and validation commands.
