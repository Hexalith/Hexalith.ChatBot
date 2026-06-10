# Test Automation Summary

## Story

Story 6.3: Outbound approval gate and approval record.

## Generated Tests

### API / E2E Tests
- [x] Added `CommandGatewayApi_ShouldPauseOutboundSendForApprovalThenSubmitApprovedSendOnceWithDefaultAdapterFailClosed` in `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` to cover outbound approval request, approval decision, approved outbound send, equivalent send replay, metadata-only audit refs, public API command admission, durable EventStore submission, and the default outbound adapter fail-closed `unavailable` status.
- [x] Added `CommandGatewayApi_ShouldRejectConflictingApprovedOutboundSendWithoutSecondDurableSubmission` to cover single-shot outbound-send idempotency through `POST /api/v1/commands`, stable `idempotency_conflict_outbound_send` problem details, no second durable EventStore submission, and metadata-only conflict payloads.

### Supporting Fixes Covered By Tests
- [x] Added `idempotency_conflict_outbound_send` to the public message catalog with metadata-only detail visibility and a contract test in `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`.
- [x] Extended `CoarseIdempotencyComposer` so `DecideOutboundApproval` uses the stable `approval-decision` operation class instead of falling through to generic `command-execution`.
- [x] Adjusted the API test principal helper so story-specific `requester_authority_class` claims are not shadowed by the default contributor claim.

### Existing Supporting Tests
- [x] Existing UI E2E coverage includes the S6 outbound approval gate flow in `ProjectConversationE2ETests`.
- [x] Existing server aggregate and gateway tests cover all approval decision outcomes, append-only approval retention, denied authority/evidence cases, metadata-only audit/problem refs, and no outbound adapter call before approval.
- [x] Existing architecture tests guard UI/CLI/MCP boundaries against server outbound, gateway, audit, idempotency, and provider internals.

## Coverage
- Story 6.3 API approval/send happy path: 1/1 covered through HTTP command gateway.
- Outbound-send idempotency paths: 2/2 covered (`equivalent replay`, `conflicting duplicate`).
- Approval decision idempotency taxonomy: covered for outbound decisions through the public admission path.
- Metadata-only safeguards: covered for accepted audit refs, conflict problem details, and serialized public test artifacts.
- External side effects: covered by adapter status assertions and durable submission counts; no provider-specific payload or provider adapter data is exposed.

## Validation
- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -class Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayAdmissionApiE2ETests` - passed, 36/36.
- [x] `dotnet build tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1 /nr:false` - passed.
- [x] `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 480/480.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 1565/1565.
- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - passed, 80/80.

## Checklist Validation
- [x] Story file loaded from `_bmad-output/implementation-artifacts/6-3-outbound-approval-gate-and-approval-record.md`.
- [x] Existing implementation and tests inspected before generating new coverage.
- [x] API/E2E tests generated where the discovered Story 6.3 gap existed.
- [x] Tests use standard xUnit v3, Shouldly, `WebApplicationFactory`, and project-local fakes.
- [x] Tests cover the happy path.
- [x] Tests cover critical error/conflict cases.
- [x] All generated tests run successfully.
- [x] Tests use semantic contract/result fields and HTTP outcomes; no hardcoded waits or sleeps.
- [x] Tests have clear descriptions and are independent.
- [x] Test summary updated at `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- [x] Tests saved to the existing server and contract test projects.
- [x] Summary includes coverage metrics and validation commands.

## Next Steps

Keep the admission API E2E matrix aligned if outbound approval/send commands are later exposed through CLI or MCP surfaces.
