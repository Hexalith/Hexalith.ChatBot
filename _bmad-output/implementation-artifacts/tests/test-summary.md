# Test Automation Summary

**Story:** 2.2 - Participant resolution and unresolved/unauthorized handling
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-05-31
**Framework:** xUnit v3 3.2.2 and Shouldly 4.3.0
**Run method:** compiled xUnit v3 executables, matching the story sandbox guidance.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs` - participant-resolution command dispatch now exercises the API/gateway-to-EventStore path, verifies tenant-bound orchestration occurs before EventStore submission, and asserts the submitted payload contains resolved and unresolved participant outcomes.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - participant authorization now covers unresolved, email-only, explicitly unauthorized, and directory-degraded actor authorities; each blocks before dispatch, idempotency, durable mutation, and normal audit envelopes while returning catalog-backed metadata-only problem details.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Adapters/ParticipantDirectoryTests.cs` - participant directory coverage now includes invalid evidence, stale/rebuilding/degraded/unavailable Parties projections, and transient Parties failures mapped to safe unresolved outcomes.

### E2E Tests

- [x] UI E2E is not applicable for story 2.2 because no UI implementation files were touched.
- [x] The command-path dispatch test provides the story's end-to-end server lane from camelCase API wire payload through participant-resolution orchestration to the EventStore submission request.
- [x] Existing conformance tests continue to cover mailbox-event actor isolation and cross-tenant read/mutation leakage behavior around unauthorized actors.

## Coverage

- API/gateway participant-resolution paths: admitted command dispatch, tenant-bound participant orchestration, PascalCase EventStore payload forwarding, unresolved/unauthorized/email-only/directory-degraded actor denial.
- Directory adapter outcomes: resolved party, not found, ambiguous match, tenant mismatch, restricted party, erased party, invalid evidence, degraded projection, unavailable directory.
- Contract and schema paths: existing story 2.2 contract tests cover required OpenAPI fields, enum wire values, camelCase JSON, PartyId reference serialization, and ULID-only resolution IDs.
- UI features: not applicable for story 2.2; no new visual surface was introduced.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` - passed 142/142.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` - passed 72/72.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` - passed 35/35.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests` - passed 54/54.

## Checklist

- [x] API tests generated where applicable.
- [x] E2E/server command-path tests generated; UI E2E not applicable.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases.
- [x] Tests use clear descriptions and semantic command/domain assertions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created in the configured output path.
- [x] Coverage metrics and validation commands recorded.
