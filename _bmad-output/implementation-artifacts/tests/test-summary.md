# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs` - OpenAPI gateway problem examples must use catalog-backed codes, titles, messages, metadata-only visibility, and safe client actions.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs` - ProblemDetails `clientAction` enum exposes only catalog-safe hyphenated action values.
- [x] `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs` - Generated client enum values match the catalog-safe wire actions.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - Current gateway problem details must resolve caller-visible text from the message catalog.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - Uncategorized authorization reasons must record only catalog version plus safe fallback code, never raw input.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - Redaction stage strips non-catalog `detail` and `instance` values containing exception, tenant/project/file, secret, and local path sentinels.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs` - Integration harness wiring now supplies the Story 1.7 problem-details factory dependency.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - Gateway workflow tests cover authentication denial, authorization/safe-not-found indistinguishability, audit unavailable, idempotency conflict, invalid lifecycle transition, and leakage prevention.
- [x] No browser/UI E2E tests generated; Story 1.7 currently exposes contract and API/gateway surfaces only.

### Contract and Guardrail Tests
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs` - Problem examples allow the documented 503 audit-unavailable response.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs` - Existing catalog shape tests now compile and validate stable entries, headline length, one-sentence reasons, safe actions, disabled reasons, and restricted text.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` - Non-generated server problem text literals are allowed only inside the catalog resolver or redaction boundary.
- [x] `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` examples were updated to catalog-backed Story 1.7 values so the generated contract tests assert the intended behavior.

## Coverage
- API/problem responses: 5/5 current gateway problem families covered (`authentication_denied`, `authorization_denied`, `audit_unavailable`, `idempotency_conflict_command_execution`, `invalid_lifecycle_transition`).
- Message catalog: required M0 entries and forward-safe FR77 state-family entries covered by contract tests.
- Redaction: direct redaction-stage coverage plus gateway serialization leakage checks for tenant, project, file, party, audit, payload, secret, Windows path, Unix path, and raw exception sentinels.
- UI features: 0/0 covered; no Story 1.7 UI surface exists in this repository.

## Validation
- [x] Existing framework detected: xUnit v3 with Shouldly in .NET test projects.
- [x] `dotnet build tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1 /nr:false` - passed.
- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed.
- [x] `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1 /nr:false` - passed.
- [x] `dotnet build tests/Hexalith.ChatBot.Client.Tests/Hexalith.ChatBot.Client.Tests.csproj --no-restore -m:1 /nr:false` - passed.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` - 26 passed.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` - 72 passed.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` - 17 passed.
- [x] `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests` - 10 passed.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests` - 2 passed.
- [ ] `dotnet test ...` VSTest runner - blocked by sandbox socket permission (`System.Net.Sockets.SocketException (13): Permission denied`); replaced with xUnit v3 in-process test executables.

## Checklist Result
- [x] API tests generated where applicable.
- [x] E2E workflow tests generated for the non-UI gateway surface.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path and critical error cases.
- [x] Tests use direct API/gateway assertions and metadata-only contract checks.
- [x] Tests avoid hardcoded waits and sleeps.
- [x] Tests are independent and have clear descriptions.
- [x] Test summary created with coverage metrics.
- [x] Generated tests run successfully with the in-process runner.
