# Test Automation Summary

## Generated Tests

### API Tests
- [x] tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs - Command submission success now asserts canonical `Proposed` lifecycle response.
- [x] tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs - Invalid lifecycle transition returns metadata-only 409, writes rejected transition audit, and skips dispatch.
- [x] tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs - Invalid lifecycle transition with unavailable audit writer returns typed `audit_unavailable`, queues replay intent, emits alert, and skips dispatch.

### E2E Tests
- [x] tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs - Gateway workflow tests cover stage order, canonical transition audit, invalid transition rejection, invalid transition audit-unavailable handling, duplicate replay, conflict short-circuiting, and audit failure paths.
- [x] tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs - Lifecycle workflow tests cover canonical vocabulary, every valid edge, representative invalid edges, terminal reprocess semantics, and sub-state transitions.
- [x] No browser/UI E2E tests generated; Story 1.6 has no UI surface in this repository.

### Contract and Guardrail Tests
- [x] tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs - Contract enum wire names assert lifecycle and ChatBot health/status stability.
- [x] tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs - OpenAPI lifecycle and health/status schemas must use canonical values only.
- [x] tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs - Generated client lifecycle enum names and `EnumMember` wire values must match canonical order.
- [x] tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs - Non-generated source must not hard-code legacy lifecycle literals.

## Coverage
- API endpoints: command submission covered for happy path, replay, idempotency conflict, authentication denial, cross-tenant denial, audit unavailable, invalid lifecycle rejection, and invalid lifecycle audit unavailable.
- Lifecycle model: 21/21 required valid transition edges covered, plus representative invalid edges across initial, active, review, corrected, sub-state, and terminal states.
- UI features: 0/0 covered; no Story 1.6 UI exists.
- Status/health contract: contract and OpenAPI schema coverage added for `healthy`, `degraded`, `failed`, and `unknown`.

## Validation
- [x] Existing framework detected: xUnit v3 with Shouldly in .NET test projects.
- [x] `dotnet build tests/Hexalith.ChatBot.Client.Tests/Hexalith.ChatBot.Client.Tests.csproj --no-restore -m:1` - passed.
- [x] `dotnet build tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1` - passed.
- [x] `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1` - passed.
- [ ] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1` - blocked by implementation mismatch: `CommandGateway` does not contain a constructor that takes 12 arguments at `CommandGatewayTests.cs:28` and `CommandGatewayTests.cs:658`.
- [ ] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1` - blocked by the same server test compile errors.
- [ ] `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` - 18 passed, 3 failed. Failing checks show OpenAPI still has old lifecycle enum values and lacks `ChatBotHealthStatus`.
- [ ] `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests` - 7 passed, 2 failed. Failing checks show generated client still has `Pending|Accepted|Running|Succeeded|Failed|Rejected|Cancelled` instead of canonical lifecycle values.
- [ ] `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` - 15 passed, 1 failed. Failing check finds legacy lifecycle literals in `CommandGatewayHttpResults.cs` and `AuditEnvelopeFactory.cs`.

## Current Blockers
- Source implementation has not caught up to the generated Story 1.6 tests: `CommandGateway` is not wired with `ILifecycleTransitionGuard`.
- OpenAPI still exposes old lifecycle values and has no `ChatBotHealthStatus` schema.
- Generated client has not been regenerated from the canonical OpenAPI contract.
- Non-generated server source still hard-codes legacy lifecycle transition literals.

## Checklist Result
- [x] API tests generated where applicable.
- [x] E2E workflow tests generated for the non-UI gateway/lifecycle surface.
- [x] Tests use standard xUnit v3 and Shouldly patterns.
- [x] Tests cover happy path and critical error cases.
- [x] Tests use semantic HTTP assertions and direct gateway assertions.
- [x] Tests avoid hardcoded waits and order dependencies.
- [x] Tests are independent and have clear descriptions.
- [x] Summary includes coverage metrics.
- [ ] All generated tests run successfully; blocked by implementation gaps listed above.
