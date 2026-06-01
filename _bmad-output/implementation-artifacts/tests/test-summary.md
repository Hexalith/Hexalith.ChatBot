# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added project conversation API coverage proving captured task-intent records are exposed through `detectedIntent` as metadata-only FR35 fields.
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added leakage guards proving task intent IDs, safe offset tokens, raw mail body text, provider payloads, prompts, and tool arguments are not exposed by the query response.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - Extended the project conversation E2E classification fixture to render captured task-intent source evidence, message code, safe next action, and redaction state.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs` - Extended UI service mapping assertions for detected-intent source evidence IDs, message code, and redaction state.

## Coverage
- API endpoints: 1/1 applicable Story 4.1 read endpoint covered (`GET /api/v1/projects/{projectId}/conversation`).
- API behavior: captured task-intent happy path plus metadata-only leakage prevention covered.
- UI behavior: captured detected-intent rendering and client-side contract mapping covered.
- Critical error cases: fail-closed task-intent kernel and projection rejection/replay cases were already covered by existing Story 4.1 server tests; this workflow added the missing query/UI exposure checks.

## Validation
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - 357 passed.
- [x] `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - 93 passed.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - 44 passed.

## Checklist Status
- [x] API tests generated where applicable.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard project APIs: xUnit v3, Shouldly, WebApplicationFactory, and Playwright semantic locators.
- [x] Happy path and critical metadata-only leakage cases covered.
- [x] No hardcoded waits or sleeps added.
- [x] Tests are independent and pass with the compiled xUnit v3 runner.
