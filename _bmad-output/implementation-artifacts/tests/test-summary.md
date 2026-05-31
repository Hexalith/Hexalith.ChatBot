# Test Automation Summary - Story 1.21

**Story:** 1.21 - Redaction-safe off-surface affordances and recovery patterns
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-05-31
**Framework:** xUnit v3 3.2.2, Shouldly 4.3.0, Microsoft.Playwright 1.60.0
**Run method:** compiled xUnit v3 executables, matching the existing BMAD validation path for this sandbox.

## Generated Tests

### API Tests
- [x] Not applicable. Story 1.21 is scoped to UI contracts, governed primitives, and the UI E2E/static fixture lane; it adds no API endpoint or backend export surface.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - extends the governed operations fixture with English/French redaction notices, recovery copy, stable IDs/codes, active-filter summary plus result count, one-primary-action checks, canonical field order, and restricted-text negative controls.
- [x] Existing E2E/static coverage remains in place for semantic tokens, UI-origin command dispatch, live-region behavior, reduced motion, forced colors, responsive/touch behavior, localization, keyboard landmarks/focus, validation focus, and governed primitive accessibility.

### Contract Tests
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotOffSurfaceRedactionContractTests.cs` - covers off-surface affordance kinds, redacted/unauthorized non-leakage, non-openable evidence, English/French redaction microcopy, and current evidence primitive wiring.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotRecoveryPatternContractTests.cs` - covers UX-DR40 flows: association review, AI action review, queue retry, correction, and tenant configuration.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotCognitiveLoadContractTests.cs` - covers UX-DR41 canonical field order, one primary action, action grouping, summary-before-ID behavior, active-filter summary, and result count.
- [x] Auto-applied review fixes: tightened `ChatBotOffSurfaceAffordanceContract` so off-surface artifacts must include the visual redacted payload and redaction notice; tightened `ChatBotRecoveryPatternContract` for correction success/partial/blocked status, risky-action confirmation coverage, queue row-status focus, and unsafe raw failure text rejection.

## Coverage

- API endpoints: not applicable for Story 1.21.
- UI contract surfaces: off-surface redaction, recovery patterns, cognitive-load guardrails, queue active-filter summary, and evidence primitive off-surface metadata covered.
- Locales: English and French redaction/recovery/cognitive-load microcopy covered through resource lookup/localizer paths and E2E/static fixture variants.
- Non-leakage: restricted file name, project name, raw exception text, raw payload text, tenant secret text, command payloads, secrets, paths, and unrestricted audit detail covered by contract/static sentinels.
- Regression carry-forward: Stories 1.17-1.20 responsive/touch, accessibility/focus, live-region/reduced-motion, and localization lanes remain green in the focused UI and UI E2E suites.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -noColor` - passed 77/77.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor` - passed 18/18.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `git diff --check` - passed with no whitespace errors.
- [x] Senior review fix validation: focused UI build passed; UI xUnit binary passed 77/77; UI E2E build passed; UI E2E xUnit binary passed 18/18; full solution build passed 0 warnings/0 errors; `git diff --check` passed.

## Checklist

- [x] API tests generated if applicable.
- [x] E2E tests generated for UI behavior.
- [x] Tests use standard framework APIs.
- [x] Tests cover happy path and critical error cases.
- [x] Tests use semantic locators or stable fixture metadata.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created in the configured output path.
- [x] Coverage metrics and validation commands recorded.

## Notes

- Architecture tests were not rerun because no package references, project references, dependency boundaries, or architecture assertions changed in this QA pass.
- Browser execution succeeded in the E2E lane; the deterministic static fallbacks remain available for environments without Playwright browser support.
