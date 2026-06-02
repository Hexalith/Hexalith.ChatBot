# Test Automation Summary - Story 6.3

**Story:** 6.3 - Outbound approval gate and approval record
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-02
**Framework:** xUnit v3 in-process runners, Shouldly, and Microsoft.Playwright for UI E2E.

## Generated Tests

### API Tests

- [x] Existing `tests/Hexalith.ChatBot.Contracts.Tests/OutboundApprovalContractTests.cs` - outbound approval/send command JSON shape, schema versions, canonical approval/authority tokens, metadata-only public fields, and no provider payload/display leakage.
- [x] Existing `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - CommandGateway allowlist/admission, approval gate order, no adapter call before approval, audit refs, idempotency replay/conflict, status behavior, and denied-authority fail closed behavior.
- [x] Existing `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs` - outbound approval request, approve/reject/request-revision/cancel retention, append-only transitions, expired evidence denial, approved send, non-approve never-send outcomes, and single-shot outbound-send idempotency.
- [x] Existing `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` - outbound approval projection materializes S6/conversation approval metadata.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added `OutboundApprovalGateShouldPauseSendUntilApprovalAndRetainMetadataOnlyDecisions`, covering outbound approval request metadata, no send before approval, approved send enablement, reject/revision/cancel non-send decisions, blocked approval reason reachability, and metadata-only redaction.

## Coverage

- API/contract coverage: outbound approval request, decision, send command, governed content snapshot, OpenAPI/client spine, and public redaction constraints.
- Gateway/server coverage: approval required before adapter side effect, send-time authority recomputation, metadata-only audit/problem refs, fail-closed authority/evidence states, and outbound-send idempotency.
- UI/E2E coverage: S6 approval surface renders command name, allowlist version, draft id, redaction state, recipients, sender authority, requester, project/context refs, policy snapshot, evidence freshness, expected post-state, and all decisions.
- Critical error cases: expired evidence, insufficient authority, non-approve decisions never send, and pre-approval send attempt leaves adapter call count at zero.
- Architecture coverage: existing fitness tests prevent UI/CLI/MCP from depending on server outbound, gateway, Dapr/EventStore internals, or provider adapter internals.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - 127 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - 494 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - 97 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - 52 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - 37 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - 15 passed, 0 failed.
- [x] `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - 66 passed, 0 failed.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated where UI exists.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases.
- [x] Tests use semantic/accessibility locators.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and run without order dependency.
- [x] Test summary created with coverage metrics.

## Discovered Gaps Applied

- Added missing Story 6.3 browser-level coverage for the outbound approval gate and approval record surface.
- Existing API/server tests already covered the approval/send mechanics, so no additional API gap was found.
