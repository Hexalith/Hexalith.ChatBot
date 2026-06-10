# Test Automation Summary - Story 3.14

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-10
**Story:** `_bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md`
**Framework:** xUnit v3 + Shouldly + ASP.NET Core `WebApplicationFactory`.

## Generated Tests

### API Tests

- [x] Added `ProjectConversationEndpointShouldPartitionAiContextPackageWithStableExclusionReasons` in `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`.
- [x] The test exercises the real `GET /api/v1/projects/{projectId}/conversation` read path with an in-memory projection store and authenticated project-scoped principal.
- [x] The test covers an included captured attachment plus `pending-scan`, `unsafe`, `policy-denied`, and `redacted` exclusions.
- [x] The test verifies NFR9 manifest fields, `ETag` emission, stable exclusion reason codes, redacted exclusion tokens, and package-level leakage protections.

### E2E Tests

- [x] Story 3.14 has no new S1 UI surface; the applicable end-to-end coverage is API-level query-contract coverage through the real server endpoint.
- [x] Existing UI E2E project remains untouched because the story package is inspectable through the query contract and is not rendered as a new UI dashboard.

## Coverage

- API endpoint coverage: `GET /api/v1/projects/{projectId}/conversation` now covers package happy path, mixed include/exclude partitioning, conditional-read metadata, authorization denials, and redacted/non-confirming denial behavior.
- Package assembler coverage: existing focused tests cover clean inclusion, ineligible statuses, policy/authorization/readiness gates, NFR9 completeness, source-evidence fail-closed behavior, purity/no-invocation, tenant scoping, idempotency, and last-writer-wins.
- UI coverage: not applicable for Story 3.14 because no UI surface was added.

## Validation

- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -parallel none -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldPartitionAiContextPackageWithStableExclusionReasons` - passed 1/1.
- `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -parallel none -class Hexalith.ChatBot.Server.Tests.Lifecycle.ProjectAiContextPackageAssemblerTests` - passed 15/15.
- `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -parallel none -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldExposeAiContextPackageManifestMetadataOnly -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldReturnNotModifiedForMatchingEtag -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldOmitAiContextPackageFromRedactedDenials -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldDenyAuthenticatedActorWithoutProjectScope` - passed 4/4.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E/API workflow coverage generated because Story 3.14 is exposed through the server query contract.
- [x] Tests use standard xUnit v3, Shouldly, and ASP.NET Core test-host APIs.
- [x] Tests cover happy path captured-file inclusion.
- [x] Tests cover critical error cases: pending scan, unsafe, policy-denied, redacted exclusion, authorization denials, and leakage prevention.
- [x] All generated tests run successfully through compiled xUnit runners.
- [x] Tests use endpoint-level user-visible contract assertions rather than implementation-only assertions.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent and fixture-driven.
- [x] Test summary created at the workflow output path.
- [x] Tests saved to existing test directories.
- [x] Summary includes coverage metrics.
