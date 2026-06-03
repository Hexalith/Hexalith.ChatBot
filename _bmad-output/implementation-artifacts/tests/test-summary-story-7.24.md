# Test Automation Summary — Story 7.24 (Disable outbound channel)

**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-03
**Engineer role:** QA automation (test generation only — no code review/story validation)
**Test framework:** xUnit v3 + Shouldly + NSubstitute (compiled in-process runners; `.NET 10.0.300`, `net10.0`)

## Method

Mapped each AC9-enumerated test requirement (22 behaviours) against the five test files touched by the
story, then auto-applied tests for the two coverage gaps found. Story 7.24 was already delivered with
broad coverage (it mirrors the Story 7.21 disable cell); this pass closed the remaining gaps.

## Coverage map (AC9 → existing tests)

20 of 22 AC9 behaviours were already covered before this pass, including: single-actor no-op,
distinct-second-approver activation, same-person rejection at gateway / dispatcher / aggregate,
service-client + AI denial (propose & approve), non-policy-scope denial, policy-admin + tenant-admin
allow, send-seam fail-closed before `IOutboundMailboxSender.SendAsync` with `outbound_channel_disabled`,
sibling-Active and cross-tenant isolation, `Active->Disabled` audit envelope + `admin-scope:policy`,
audit redaction, audit-unavailable fail-closed, idempotency, subject/version/reason mismatch rejection,
and the catalog finite-reason entry.

## Gaps found and closed (auto-applied)

### Gap 1 — AC5/AC13: drafts/approvals remain inspectable when the channel is Disabled
No test proved that the disable affects **only** the send step. The disabled-channel check is wired
solely into the `ExecuteApprovedOutboundDraft` branch, so `CreateOutboundDraft` /
`RequestOutboundSendApproval` / `DecideOutboundApproval` must continue to dispatch normally.

- **New test:** `DispatchShouldLeaveOutboundDraftCreationInspectableWhenChannelDisabledAndNeverConsultTheChannelControl`
  in `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
- Dispatches `CreateOutboundDraft` for a tenant+channel that is **Disabled** in the injected
  `IOutboundChannelControlStateProvider`; asserts the draft is submitted normally **and** the
  channel-control provider is **never consulted** (`ObservedRequests` empty) — proving the block is
  local to the send step.
- Added two helpers (`OutboundDraft`, `OutboundDraftContext`) using the proven draft-only authority
  claim recipe (project ownership + `outbound-draft` project scope + `draft-only` tenant policy).

### Gap 2 — AC8/AC18: generated-client type-level parity for the two new public commands
The global checksum-freshness test proves the regenerated client matches the checked-in file, but there
was no type-level assertion that the two new public commands and the control-state enum actually surface
on the generated client (the established pattern for mailbox/compliance contracts).

- **New test:** `GeneratedClientShouldContainOutboundChannelDisableContractsWithSafeMetadataOnly`
  in `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- Asserts `SubmitOutboundChannelDisable` / `ApproveOutboundChannelDisable` expose the safe metadata
  fields (`OutboundChannelRef`, `DisableChangeId`, `OldState`/`NewState` typed as
  `OutboundChannelControlState`, `ApproverRef`); that `OutboundChannelControlState` is append-only
  `Active(0)`→`Disabled(1)` with wire values `active`/`disabled`; and that **no** leaky payload field
  (recipient/sender/address/body/subject/content/token/secret/OAuth/credential/prompt/audit-envelope)
  appears on either command.

## Generated Tests

### API / behaviour tests
- [x] `AcceptedCommandDispatcherTests.cs` — `DispatchShouldLeaveOutboundDraftCreationInspectableWhenChannelDisabledAndNeverConsultTheChannelControl` (AC5/AC13)

### Contract / parity tests
- [x] `ClientGenerationTests.cs` — `GeneratedClientShouldContainOutboundChannelDisableContractsWithSafeMetadataOnly` (AC8/AC18)

## Coverage

- AC9 behaviours: **22 / 22** covered (20 pre-existing + 2 added this pass)
- No UI/E2E surface added (consistent with the story decision — AC6 satisfied by catalog + reason code + audit metadata)

## Test run results (compiled in-process xUnit v3 runners, `-parallel none`)

| Suite | Total | Failed | Notes |
|-------|-------|--------|-------|
| Server.Tests | 868 | 0 | +1 (AC13 dispatcher test) |
| Client.Tests | 18 | 0 | +1 (AC18 parity test) |
| Contracts.Tests | 264 | 0 | regression |
| Conformance.Tests | 75 | 0 | regression |
| Architecture.Tests | 37 | 0 | regression |

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 Warning(s), 0 Error(s).
- `git submodule status` → no submodule pointer drift.
- Generated client unchanged (no OpenAPI change in this pass) → checksum freshness test stays green.

## Next Steps

- Run suites in CI.
- When the durable read-side projection of `OutboundChannelDisabled` lands (deferred per the sanctioned
  7.12/7.15/7.18/7.21 read-side deferral), add an integration test that drives a real disable through the
  two-person flow and observes the send-seam block end-to-end (currently the disabled state is injected).
