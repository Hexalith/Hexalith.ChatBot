# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Existing API coverage verifies health/alive, SDK domain-service routes, command submission, rejection, and compatibility endpoint behavior after the host reduction.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs` - Existing API-host guardrails verify the server remains on `AddEventStoreDomainService(...)`, `UseEventStoreDomainService()`, SDK telemetry, state-store health checks, and the DataProtection key-ring boundary.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.IntegrationTests/ScaffoldTopologySmokeTests.cs` - Added story 11.6 launch-path coverage proving Tier-3 tests use the retained thin local AppHost shim as a non-resource Aspire project reference.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/ScaffoldTopologySmokeTests.cs` - Added story 11.6 topology coverage for dedicated Dapr state/pubsub resources, workflow state isolation, local access-control posture, sidecar readiness preconditions, and `chatbot-ui` staying HTTP-only.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` - Existing opt-in Tier-3 E2E coverage still proves unauthenticated fail-closed behavior, tenant-bound command flow, cross-origin UI/CLI/MCP parity, Dapr sidecar readiness, and correction-propagation workflow health.

## Coverage
- Story 11.6 acceptance criteria: 7/7 covered by AppHost topology tests, architecture anti-regrowth tests, server/UI host tests, and Tier-3 Aspire/Dapr E2E tests.
- API endpoints: `/process`, `/query`, `/project`, `/replay-state`, `/admin/operational-index-metadata`, `/health/chatbot`, `/health/chatbot/workflows`, `/health/chatbot/periodic-enforcement`, `/alive`, and compatibility routes are covered by server bootstrap and architecture tests.
- E2E workflows: Tier-3 Aspire/Dapr tests cover 3/3 required workflows: governed command flow, cross-origin parity, and workflow runtime health.
- Critical error cases: unauthenticated fail-closed command submission, denied/absent Dapr read probe, missing Dapr access-control config, production DataProtection key-ring guard, and removed hosting project regrowth are covered.

## Validation
- [x] `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --no-restore -m:1 -nodeReuse:false` - passed; one pre-existing `StackExchange.Redis` version-conflict warning surfaced from `Hexalith.Tenants`.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -noLogo -noColor -parallel none -class Hexalith.ChatBot.IntegrationTests.ScaffoldTopologySmokeTests` - passed, 3 total, 0 failed, 0 skipped.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -noLogo -noColor -parallel none -class Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests` - ran 3 total, all skipped because Docker/Dapr Tier-3 prerequisites were unavailable.
- [x] `dotnet build tests/Hexalith.ChatBot.AppHost.Tests/Hexalith.ChatBot.AppHost.Tests.csproj --no-restore -m:1 -nodeReuse:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -noLogo -noColor -parallel none` - passed, 9 total, 0 failed, 0 skipped.
- [x] `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1 -nodeReuse:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.Architecture.Tests.ScaffoldArchitectureTests` - passed, 28 total, 0 failed, 0 skipped.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.Architecture.Tests.DomainServiceSdkHostAdoptionAdrTests` - passed, 9 total, 0 failed, 0 skipped.
- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 -nodeReuse:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests` - passed, 69 total, 0 failed, 0 skipped.
- [x] `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 -nodeReuse:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.UI.Tests.AssociationReviewComponentContractTests` - passed, 7 total, 0 failed, 0 skipped.
- [x] `git diff --check` - passed.

## Next Steps
- Run the opt-in Tier-3 Aspire/Dapr E2E on a prepared host with Docker, Dapr CLI/runtime, placement, scheduler, and Keycloak prerequisites when infrastructure is available.
