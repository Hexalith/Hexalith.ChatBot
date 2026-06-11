# Test Automation Summary - Story 7.24 (Disable outbound channel)

**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-11
**Engineer role:** QA automation (test generation only)
**Test framework:** xUnit v3 + Shouldly + NSubstitute (compiled in-process runners; .NET 10)

## Method

Mapped story 7.24 acceptance coverage against the current server, contract, client, conformance, and architecture test suites. A previous QA pass had already closed draft inspectability and generated-client parity gaps. This run found one remaining explicit AC5 gap after later story work expanded nearby coverage patterns.

## Gap Found And Closed

### AC5: approval request and decision remain inspectable when the channel is Disabled

Existing coverage proved:
- `ExecuteApprovedOutboundDraft` fails closed before the outbound adapter when the channel is Disabled.
- `CreateOutboundDraft` remains inspectable and does not consult the outbound-channel control provider.

Missing direct coverage:
- `RequestOutboundSendApproval` and `DecideOutboundApproval` also remain inspectable while the outbound channel is Disabled.

Generated test:
- `DispatchShouldLeaveOutboundApprovalRequestAndDecisionInspectableWhenChannelDisabled`
  in `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`

The test dispatches both approval steps with a disabled `adapter:mailbox-outbound` configured in the injected `IOutboundChannelControlStateProvider`, then asserts:
- each command dispatches normally to EventStore;
- `IOutboundMailboxSender` is never invoked;
- the disabled-channel provider is never consulted.

This proves the disable affects only the send/execute step and keeps pending drafts/approvals inspectable.

## Generated Tests

### API / Behavior Tests
- [x] `AcceptedCommandDispatcherTests.cs` - `DispatchShouldLeaveOutboundApprovalRequestAndDecisionInspectableWhenChannelDisabled` (AC5)

### Contract / Parity Tests
- [x] Existing contract/client parity coverage retained for `SubmitOutboundChannelDisable`, `ApproveOutboundChannelDisable`, `OutboundChannelControlState`, and generated-client checksum freshness.

## Coverage

- AC5 inspectability: `CreateOutboundDraft`, `RequestOutboundSendApproval`, and `DecideOutboundApproval` now have direct disabled-channel coverage.
- Send-seam fail-closed: existing coverage verifies disabled sends stop before `IOutboundMailboxSender.SendAsync` with the finite disabled reason.
- Authorization/two-person/audit/metadata/client parity: existing story coverage remains in place and passed in regression suites.
- Browser UI: not applicable for this story; no S5 admin surface exists for 7.24.

## Validation

| Suite | Total | Failed |
|-------|------:|-------:|
| Server.Tests | 1603 | 0 |
| Contracts.Tests | 482 | 0 |
| Client.Tests | 36 | 0 |
| Conformance.Tests | 93 | 0 |
| Architecture.Tests | 39 | 0 |

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - Build succeeded, 0 warnings, 0 errors.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated at the applicable gateway/dispatcher surface.
- [x] Tests use standard xUnit v3 APIs and Shouldly assertions.
- [x] Tests cover the happy path for inspectable approval request/decision steps under a Disabled channel.
- [x] Critical error cases remain covered by existing authorization, two-person, audit-unavailable, aggregate, and send-seam tests.
- [x] Tests use stable command/gateway assertions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Summary includes coverage metrics and validation commands.
