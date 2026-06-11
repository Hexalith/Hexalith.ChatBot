# Test Automation Summary

Story: 8.6 - Hosted Dapr Workflow production binding and saga readiness validation
Date: 2026-06-11

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - Added direct gateway coverage proving correction admission fails closed when the hosted workflow runtime is unavailable.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` - Added HTTP API E2E coverage proving workflow-runtime unavailability returns metadata-only `association_correction_workflow_unavailable`, does not submit to EventStore, does not schedule workflow work, does not write audit envelopes, and does not create idempotency records.
- [x] Updated correction-dependency reason mapping so workflow-runtime, projection, audit, and dependency degraded statuses resolve through catalog-backed safe codes instead of collapsing workflow outages into projection outages.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` - API E2E covers the correction UI spine happy path plus workflow-runtime outage denial.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` - Existing Tier-3 hosted Dapr workflow health smoke remains in place and self-skips when Docker/Dapr opt-in prerequisites are absent.

## Coverage

- Correction admission dependency failures: workflow runtime and projection dependency are separately covered.
- Fail-closed guarantees: EventStore submission, hosted workflow scheduling, audit envelopes, and coarse idempotency records remain untouched on workflow-runtime outage.
- Metadata-only diagnostics: API response asserts catalog code, retry action, metadata-only visibility, and absence of tenant/project/rationale/raw exception detail.
- Story 8.6 topology and boundary checks remain covered by AppHost, Aspire, Architecture, Conformance, Server, and Integration lanes.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - Build succeeded, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - Total 1614, Errors 0, Failed 0, Skipped 0.
- [x] `./tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -parallel none` - Total 6, Errors 0, Failed 0, Skipped 0.
- [x] `./tests/Hexalith.ChatBot.Aspire.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Aspire.Tests -parallel none` - Total 3, Errors 0, Failed 0, Skipped 0.
- [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - Total 40, Errors 0, Failed 0, Skipped 0.
- [x] `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - Total 96, Errors 0, Failed 0, Skipped 0.
- [x] `./tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -parallel none` - Total 20, Errors 0, Failed 0, Skipped 3. Tier-3 Dapr/Docker workflow smoke skipped because `HEXALITH_CHATBOT_TIER3=1` plus Docker/Dapr prerequisites were not present.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E/API workflow tests generated for the correction admission surface.
- [x] Tests use standard xUnit v3, WebApplicationFactory, and Shouldly APIs.
- [x] Happy path remains covered for correction acceptance and hosted workflow scheduling.
- [x] Critical error case covered for workflow-runtime outage.
- [x] Tests use HTTP/API-level assertions and no hardcoded waits.
- [x] Tests have clear behavior descriptions.
- [x] Tests are independent and have no order dependency.
- [x] Test summary created with coverage metrics.
