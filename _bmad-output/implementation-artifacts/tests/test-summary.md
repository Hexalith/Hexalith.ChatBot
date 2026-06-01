# Test Automation Summary

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added project conversation API coverage proving `aiContextPackage` is materialized with NFR9 fields, included/excluded file partitioning, stable exclusion reasons, and metadata-only leakage guards.
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - Added redacted denial coverage proving unauthorized/unknown project conversation reads omit the AI-context package and do not disclose tenant, project, folder, file, or provider references.

### Server Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/ProjectAiContextPackageAssemblerTests.cs` - Added assembler coverage for `policy-denied`, `unauthorized`, and `not-yet-eligible` gates.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/ProjectAiContextPackageAssemblerTests.cs` - Added NFR9 completeness and dependency-free assembler assertions to protect no model/tool/content invocation.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/ProjectAiContextPackageAssemblerTests.cs` - Senior review added regression coverage for latest policy/retention metadata selection and source-evidence fail-closed exclusion.

### E2E Tests
- [x] API-level E2E coverage was generated through in-process `WebApplicationFactory` tests for the story 3.14 query surface.
- [x] No new UI E2E test was added because story 3.14 exposes an inspectable metadata contract through the project conversation query and does not add a UI surface.

## Coverage
- API endpoints: 1/1 applicable story 3.14 read endpoint covered (`GET /api/v1/projects/{projectId}/conversation`).
- API outcomes: authorized package, pending exclusion, included clean file, and redacted unauthorized/unknown denial covered.
- Server assembler gates: clean include, pending, unsafe, rejected, failed, unavailable, retryable, redacted, policy-denied, unauthorized, not-yet-eligible, empty package, and idempotent last-writer-wins covered.

## Validation
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- [x] `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -parallel none -class Hexalith.ChatBot.Server.Tests.Lifecycle.ProjectAiContextPackageAssemblerTests`
- [x] `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -parallel none -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldExposeAiContextPackageManifestMetadataOnly -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldOmitAiContextPackageFromRedactedDenials`
- [x] `dotnet tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests.dll -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectAiContextPackageContractTests -class Hexalith.ChatBot.Contracts.Tests.MessageCatalogContractTests`
- [x] `dotnet tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests.dll -parallel none -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests`
- [x] `dotnet tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests.dll -parallel none -class Hexalith.ChatBot.Conformance.Tests.ContractSpineOracleTests`

## Checklist Status
- [x] API tests generated where applicable.
- [x] E2E coverage generated for the applicable API read workflow.
- [x] Tests use xUnit v3, Shouldly, and existing in-process server test patterns.
- [x] Happy path and critical error/denial cases covered.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and pass with the compiled xUnit v3 runner.
