# Test Automation Summary - Story 1.8

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-10
**Story:** `_bmad-output/implementation-artifacts/1-8-correlation-propagation-and-long-running-operation-status.md`
**Framework:** xUnit v3 + Shouldly
**Mode:** QA automation validation and gap-fill against implemented correlation propagation and governed operation-status behavior.

## Generated Tests

### API Tests

- [x] Existing API E2E coverage in `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` covers the Story 1.8 HTTP surfaces:
  - `POST /api/v1/commands` correlation response headers, missing-header fallback, invalid-header sanitization, idempotent replay, and safe problem details.
  - `GET /api/v1/operations/{operationId}` authentication requirement, invalid-id safe denial, tenant-scoped unknown/cross-tenant collapse, projection-pending status, FR80 field shape, UTC timestamps, and metadata-only leakage assertions.
- [x] Existing API/gateway regression coverage in `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` covers accepted-operation status-store writes, idempotent replay status behavior, audit reconciliation status, and metadata-only serialization.
- [x] Existing store registration coverage in `tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/OperationStatusStoreRegistrationTests.cs` covers the DAPR store swap and tenant-partitioned operation-status keys.

### E2E Tests

- [x] Story 1.8 is a server/API contract story with no browser-only UI workflow in scope. The applicable end-to-end surface is covered through in-process HTTP API tests using `WebApplicationFactory<Program>`.
- [x] Existing supporting lanes cover correlation middleware, OpenTelemetry registration, OpenAPI/client contract drift, generated-client status enums, low-dependency contracts, UTC-only server/contract boundaries, and adapter isolation:
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/CorrelationMiddlewareTests.cs`
  - `tests/Hexalith.ChatBot.ServiceDefaults.Tests/ServiceDefaultsExtensionsTests.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
  - `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`

## Gaps Discovered And Filled

- No source test-code gap remained after review. The required cross-tenant/unknown operation-status collapse test already exists as `OperationStatusEndpointShouldCollapseCrossTenantAndUnknownOperations` and compares status, correlation header, and body for indistinguishability.
- The BMAD default test summary still described Story 1.7. Replaced it with this Story 1.8 summary and checklist validation.

## Coverage

- API endpoints: 2/2 Story 1.8 applicable endpoints covered (`POST /api/v1/commands`, `GET /api/v1/operations/{operationId}`).
- API happy paths: command accepted with correlation headers; operation status returns projection-pending FR80 metadata.
- Critical error cases: unauthenticated status read, invalid operation id, cross-tenant operation id, unknown operation id, invalid correlation/task headers, idempotency conflict, invalid lifecycle transition, and audit unavailable.
- UI E2E: 0 applicable Story 1.8 browser workflows; no Story 1.8 UI rendering was in scope.

## Test Results

- `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` - passed.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -class Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests -parallel none` - passed, 45 tests.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -class Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayTests -parallel none` - passed, 131 tests.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -class Hexalith.ChatBot.Server.Tests.Gateway.CorrelationMiddlewareTests -parallel none` - passed, 2 tests.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -class Hexalith.ChatBot.Server.Tests.Gateway.Status.OperationStatusStoreRegistrationTests -parallel none` - passed, 2 tests.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -class Hexalith.ChatBot.Contracts.Tests.OpenApiContractSpineTests -parallel none` - passed, 15 tests.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests -parallel none` - passed, 19 tests.
- `tests/Hexalith.ChatBot.ServiceDefaults.Tests/bin/Debug/net10.0/Hexalith.ChatBot.ServiceDefaults.Tests -class Hexalith.ChatBot.ServiceDefaults.Tests.ServiceDefaultsExtensionsTests -parallel none` - passed, 5 tests.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -class Hexalith.ChatBot.Architecture.Tests.ScaffoldArchitectureTests -parallel none` - passed, 25 tests.

## Checklist Validation

- [x] API tests generated or verified where applicable.
- [x] E2E coverage evaluated; browser/UI E2E is not applicable for this server/API story.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy paths.
- [x] Tests cover critical error cases.
- [x] Generated/existing Story 1.8 tests run successfully through the xUnit v3 in-process runner.
- [x] Tests use semantic HTTP/body/header assertions and stable contract values.
- [x] No hardcoded waits or sleeps.
- [x] Tests have clear descriptions.
- [x] Tests are independent.
- [x] Test summary created.
- [x] Tests saved to the appropriate test projects.
- [x] Summary includes coverage metrics.
