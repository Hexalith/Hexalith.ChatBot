# Test Automation Summary

Story: 7.27 - Command allowlist v1 and full lifecycle completion
Date: 2026-06-11

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` - Added HTTP admission API coverage proving the v1-only `ChatBot.ExecuteLowRiskAssistance` command is accepted through `ExecuteApprovedAIAction` when the request is pinned to `ai-action-command-allowlist.v1`, with metadata-only audit refs and no raw payload leakage.

### E2E Tests

- [x] Existing `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` and `tests/Hexalith.ChatBot.UI.E2E.Tests/DuplicateRetryFailureStatesE2ETests.cs` continue to cover user-visible AI action, duplicate suppression, out-of-scope disposition, terminal state, and metadata-only lifecycle surfaces.

## Coverage

- API endpoints/workflows: allowlist v1 positive admission path added at the HTTP command gateway; existing tests cover non-allowlisted denial, M0 preservation, wrong-version rejection at the allowlist/aggregate seams, metadata completeness, lifecycle skip triggers, and cross-actor isolation parity.
- UI/E2E surfaces: no new UI fixture was required for this backend governance story; existing browser-backed fixtures were validated.
- Critical error cases: unsupported/non-allowlisted AI command denial, v1 member requested at M0 at lower seams, invalid lifecycle transition rejection, duplicate suppression to `Skipped`, disabled/quarantined/rate-limited CLI/MCP/service-client denials, and metadata leakage checks are covered.
- Gaps closed in this run: 1 discovered API E2E gap auto-applied.

## Validation

- [x] `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` - up to date.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - Build succeeded, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - Total 1605, Failed 0.
- [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - Total 39, Failed 0.
- [x] `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - Total 93, Failed 0.
- [x] `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - Total 482, Failed 0.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - Total 104, Failed 0.

## Checklist Validation

- [x] API tests generated/updated where applicable.
- [x] E2E tests generated or validated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, ASP.NET test host, and Playwright APIs.
- [x] Happy path covered.
- [x] Critical error behavior covered by existing and generated tests.
- [x] Tests use semantic accessible locators where UI E2E applies.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps added.
- [x] Tests are independent.
- [x] Test summary created with coverage metrics and validation commands.
