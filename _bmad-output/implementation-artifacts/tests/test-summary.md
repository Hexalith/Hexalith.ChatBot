# Test Automation Summary - Story 1.21

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-10
**Story:** `_bmad-output/implementation-artifacts/1-21-redaction-safe-off-surface-affordances-and-recovery-patterns.md`
**Framework:** xUnit v3 + Shouldly with Microsoft.Playwright browser checks and deterministic no-browser fallback assertions.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotOffSurfaceRedactionContractTests.cs` - verifies the UI-owned off-surface affordance contract, redacted/unauthorized non-leakage, current evidence primitive integration, and English/French phrase-level microcopy.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotRecoveryPatternContractTests.cs` - verifies UX-DR40 recovery contracts for association review, AI action review, queue retry, correction, and tenant configuration.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotCognitiveLoadContractTests.cs` - verifies UX-DR41 one-primary-action, canonical field order, active-filter summary/result-count, and summary-before-ID contracts.
- [x] `tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs` - verifies metadata-only audit history lines exclude payloads, tenant/resource names, file names, secrets, raw exception text, and unsafe source text.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - verifies rendered/static Story 1.21 fixture behavior for redaction notices, recovery copy, stable English/French machine metadata, one primary action, active-filter summary/result count, phone-width overflow, and off-surface artifact attributes.

## Gaps Discovered And Filled

- Gap: the E2E/static fixture proved the redaction notice was visible or reachable, but did not prove the off-surface artifact metadata itself inherited the redacted visual payload and excluded restricted source markers.
- Fix: extended `GovernedOperationsShouldExposeRedactionRecoveryAndCognitiveLoadFixture` to assert `AuditCopy` artifact attributes contain `Audit metadata only`, `audit:Committed`, `origin:Ui`, the stable correlation token, and the localized redaction notice while excluding `restricted-file.txt`, `Secret Project`, and `raw exception`.
- Fix: extended the deterministic no-browser fallback fixture assertions for the same English/French off-surface text and accessible-description attributes.

## Coverage

- API endpoints: 0 applicable / 0 added for this UI foundation story.
- UI contract areas: 7/7 Story 1.21 acceptance areas covered by focused xUnit contract tests and rendered/static E2E fixture checks.
- Critical error cases: restricted source text in off-surface text, accessible name, accessible description, redaction notice, disabled reason, recovery messages, field associations, audit lines, and rendered fixture attributes; missing redaction notice/escalation guidance; unsafe raw exception/payload text; missing recovery focus target/safe next action; invalid save conflict cause; duplicate primary actions; missing active-filter summary/result count; phone overflow.

## Test Results

- `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore --no-build` - blocked by sandbox VSTest socket permission (`System.Net.Sockets.SocketException (13): Permission denied`).
- `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore --no-build` - blocked by sandbox VSTest socket permission (`System.Net.Sockets.SocketException (13): Permission denied`).
- `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` - passed, Total 129, Errors 0, Failed 0, Skipped 0.
- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` - passed, Total 64, Errors 0, Failed 0, Skipped 0.
- `git diff --check` - passed with no whitespace errors.
- `python3 _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-qa-generate-e2e-tests --key workflow.on_complete` - returned an empty completion hook.

## Checklist Validation

- [x] API tests generated or verified where applicable.
- [x] E2E tests generated or extended for the UI fixture.
- [x] Tests use standard framework APIs: xUnit v3, Shouldly, and Microsoft.Playwright.
- [x] Tests cover happy paths: redacted off-surface artifacts, recovery guidance, cognitive-load ordering, localization, and rendered fixture behavior.
- [x] Tests cover critical error cases: restricted text leakage, missing redaction guidance, unsafe raw failure text, invalid recovery contracts, duplicate primary actions, missing filter counts, and responsive overflow.
- [x] All generated/verified tests run successfully through compiled xUnit v3 binaries.
- [x] Tests use semantic locators and accessibility roles/labels where browser execution is available.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent.
- [x] Test summary created at the workflow output path.
- [x] Tests saved to appropriate test projects/directories.
- [x] Summary includes coverage metrics.
