# Test Automation Summary

Story: 7.22 - Quarantine command capability
Date: 2026-06-11

## Generated Tests

### API Tests
- [x] Existing Story 7.22 API/gateway coverage retained in `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` and `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/CommandCapabilityQuarantineAuthorizationTests.cs`: two-person approval, actor-agnostic fail-closed admission, audit fail-closed behavior, metadata-only audit envelopes, reason-code distinction, and tenant/command isolation.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/CommandCapabilityQuarantineE2ETests.cs` - Added a Story 7.22 E2E fixture for command-capability quarantine safe guidance, all-actor admission denial (`human`, `service`, `ai`), prior-artifact visibility, and policy-admin review next action.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/CommandCapabilityQuarantineE2ETests.cs` - Added a source-contract E2E guard that keeps the quarantine workflow wired across OpenAPI, regenerated client, authorization tests, aggregate tests, dispatcher tests, audit tests, catalog tests, and checksum parity.

## Coverage

- API endpoints: existing `/api/v1/commands` and gateway-stage coverage proves accepted approval/audit behavior and fail-closed refusal semantics for command-capability quarantine.
- UI/E2E surface: generated fixture covers safe recovery guidance from the finite catalog tokens, all actor classes denied for the quarantined command type, no admission side effects, and prior command/audit/approval artifacts still visible.
- Critical error cases: pre-commit audit unavailable fails closed; same requester/approver is rejected at dispatcher/aggregate anchors; quarantined reason stays distinct from disabled, allowlist, grant, and AI-actor control reasons.
- Coverage metric: Story 7.22 AC-9 obligation groups remain covered, plus the previously missing UI E2E guidance/admission fixture is now present.

## Validation

- [x] `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore --logger "console;verbosity=minimal"` - build completed, then VSTest aborted before execution with sandbox `SocketException (13): Permission denied` (known repo sandbox issue).
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.CommandCapabilityQuarantineE2ETests` - 2 total, 0 failed.
- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore` - passed with 0 warnings and 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.Client.Tests/Hexalith.ChatBot.Client.Tests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -method '*CommandCapabilityQuarantine*'` - 17 total, 0 failed.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none -method '*CommandCapabilityQuarantine*' -method '*MessageCatalog*'` - 5 total, 0 failed.
- [x] `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests` - 21 total, 0 failed.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - 100 total, 0 failed.

## Checklist Validation

- [x] API tests generated/retained where applicable.
- [x] E2E tests generated for the UI/guidance surface.
- [x] Tests use standard xUnit v3, Playwright, and Shouldly APIs.
- [x] Happy path covered: quarantine guidance and policy-admin review next action remain available.
- [x] Critical error paths covered: all actor classes fail closed; audit unavailable fails closed; same-person approval guards remain wired.
- [x] Semantic locators used in browser path.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and use a browser fallback when Playwright/Chromium is unavailable.
- [x] Summary includes coverage metrics and validation commands.
