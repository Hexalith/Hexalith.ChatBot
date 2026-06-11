# Test Automation Summary

Story: 7.21 - Disable command capability
Date: 2026-06-11

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` - Added API-level coverage for the command-capability disable two-person flow through `/api/v1/commands`: policy-admin proposal, distinct approval, dispatch, pre/post audit envelopes, idempotency records, and metadata-only responses.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` - Added API-level coverage that a disabled command capability fails closed before dispatch with `command_capability_disabled`, records a metadata-only authorization failure fact, creates no audit envelopes, and creates no idempotency side effects.

### E2E Tests
- [x] `CommandGatewayAdmissionApiE2ETests` now covers Story 7.21 through the in-memory `WebApplicationFactory<Program>` command-admission pipeline, including authentication claims, tenant binding, policy-admin authorization, command-capability state provider injection, audit writer behavior, idempotency, dispatch suppression, problem response redaction, and the HTTP `/api/v1/commands` surface.

## Coverage

- API endpoints: `/api/v1/commands` Story 7.21 paths covered for accepted disable proposal, accepted distinct approval, and disabled-capability refusal.
- UI features: no browser UI surface was applicable; the command-admission API E2E harness is the relevant user-facing workflow layer for this story.
- Critical E2E gap closed: command-capability disable now has HTTP API coverage matching the existing mailbox/service-client/AI control API patterns.
- Existing lower-level coverage retained: gateway authorization, aggregate two-person enforcement, dispatcher distinct-approver guard, audit fail-closed behavior, metadata-only contract serialization, message catalog, lifecycle transition, and OpenAPI/generated-client parity were already present.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -method Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayAdmissionApiE2ETests.CommandGatewayApi_ShouldAcceptCommandCapabilityDisableFlowThenFailClosedForDisabledCapability` - 1 total, 0 failed.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -class Hexalith.ChatBot.Server.Tests.Gateway.Stages.CommandCapabilityDisableAuthorizationTests` - 7 total, 0 failed.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - 1600 total, 0 failed.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E/API-admission tests generated for the implemented surface.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Happy path covered: two-person disable proposal and approval through HTTP.
- [x] Critical error path covered: disabled command-capability denial before dispatch/side effects.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and use in-memory fakes.
- [x] Summary includes coverage metrics and validation commands.
