# Test Automation Summary — Story 7.25 (Quarantine outbound channel)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer role:** QA automation (test generation only)

**Stack:** .NET 10 / xUnit v3 / Shouldly / NSubstitute. This is a governed command-spine backend — there is **no browser UI**, so "E2E" here = the API/acceptance/aggregate/gateway integration suites. Tests were run with the compiled in-process xUnit v3 runners (the story's sandbox guidance: VSTest can hit `SocketException (13)`).

## Method

The feature was already implemented (story status `review`) with a substantial test set. The QA pass mapped every clause of the **AC9 test charter** (plus the "Add focused tests" task and the "Highest-value targets" testing note) to a concrete test, then auto-applied the discovered coverage gaps.

## AC9 charter → test coverage map

| AC9 charter clause | Test | Status |
| --- | --- | --- |
| Single-actor quarantine never takes effect (proposal alone) | `HandleOutboundChannelQuarantineProposalShouldCreatePendingWithoutQuarantining` | already covered |
| Distinct 2nd human policy-admin applies it | `HandleOutboundChannelQuarantineApprovalShouldRequirePendingAndDistinctSecondActor` | already covered |
| `RequesterRef==ApproverRef` **and** `RequesterActorId==UserId` rejected — gateway / dispatcher / aggregate | gateway `QuarantineApprovalShouldRequireHumanPolicyAdminAndDistinctApprover`; dispatcher `DispatchShouldRejectOutboundChannelQuarantineApprovalWhenApproverEqualsRequester`; aggregate `…ApprovalShouldRequirePendingAndDistinctSecondActor` (both ref **and** actor-id) | already covered |
| Service clients + AI actors denied (propose + approve) with admin-looking claims | `QuarantineProposalShouldRequireHumanPolicyAdmin`, `QuarantineApprovalShouldRequireHumanPolicyAdminAndDistinctApprover` | already covered |
| Non-policy human scope denied; policy-admin **and** tenant-admin (union) allowed | same two auth tests | already covered |
| Quarantined channel → `ExecuteApprovedOutboundDraft` fails closed **before** `SendAsync` (`outbound_channel_quarantined`, spy never invoked) | `DispatchShouldFailClosedAtSendSeamBeforeAdapterWhenOutboundChannelQuarantined` (`sender.SendCount == 0`) | already covered |
| Reason **distinct from** disabled / adapter_unavailable / **adapter_not_approved_mode** / **command_capability_quarantined** | `HandleOutboundSendShouldFailClosedWithOutboundChannelQuarantinedReasonWhenChannelQuarantined` | **GAP CLOSED** (added the last two `ShouldNotBe` assertions) |
| Sibling Active channel + same channel under a different tenant unaffected (isolation) | `DispatchShouldSendNormallyWhenChannelActiveOrUnderADifferentTenantForQuarantine` | already covered |
| `Disabled` channel still returns `outbound_channel_disabled` (regression, both branches off one read) | `DispatchShouldStillBlockDisabledChannelWithBlockedStatusAlongsideQuarantineBranch`; aggregate test `"blocked"→outbound_channel_disabled` | already covered |
| `CreateOutboundDraft`/`RequestOutboundSendApproval`/`DecideOutboundApproval` for quarantined channel still succeed (drafts/approvals inspectable) | `DispatchShouldLeaveOutboundDraftCreationInspectableWhenChannelQuarantinedAndNeverConsultTheChannelControl` (+ `provider.ObservedRequests.ShouldBeEmpty()` proves the control is wired ONLY into the send branch) | covered (see residual note) |
| Quarantine does **not** mutate existing committed/audit records (AC5/NFR17/FR75c) | `HandleOutboundChannelQuarantineShouldNotMutatePriorCommittedOrPendingRecords` | **GAP CLOSED** (new test) |
| Audit envelope: actor/scope/subject/reason/old/new/snapshot/timestamp + `StateTransition "Active->Quarantined"` + `admin-scope:policy` + no leakage | `OutboundChannelQuarantineAuditEnvelopeShouldCarryActiveToQuarantinedTransitionAndRemainMetadataOnly` | already covered |
| Audit-unavailable → no durable quarantine + no send-block (fail closed) | `OutboundChannelQuarantineApprovalPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch` | already covered |
| OpenAPI / generated-client / checksum parity; new `Quarantined` enum wire token | `ClientGenerationTests` quarantine-contract test; `AdminContractTests` finite-token (`"quarantined"`) test; `MessageCatalogContractTests` | already covered |

## Gaps discovered and auto-applied

### 1. Outbound-channel quarantine immutability test — NEW
`tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs` → `HandleOutboundChannelQuarantineShouldNotMutatePriorCommittedOrPendingRecords`.
AC9 explicitly requires "the quarantine does not mutate existing committed/audit records," and the sibling quarantine subjects (command-capability, AI-actor, service-client) each ship such a test — but the outbound-channel cell was missing it (the 7.24 disable template never had one). The test commits a quarantine for a different channel ref, leaves an unrelated pending quarantine for a third ref, then quarantines the target through the two-person flow and asserts the prior committed record and the unrelated pending record both survive intact (per-subject isolation; admins cannot mutate prior records).

### 2. Reason-code distinctness — EXTENDED
`HandleOutboundSendShouldFailClosedWithOutboundChannelQuarantinedReasonWhenChannelQuarantined` now also asserts `outbound_channel_quarantined` is distinct from `adapter_not_approved_mode` and `command_capability_quarantined` (AC9 names all four reasons; only `outbound_channel_disabled`/`outbound_adapter_unavailable` were previously asserted).

## Residual (intentionally not added — justified)

- **`RequestOutboundSendApproval` / `DecideOutboundApproval` inspectability** as separate dispatcher tests: AC9 enumerates all three pre-send steps, but only `CreateOutboundDraft` has a dedicated test. This is already covered **structurally** — the existing test asserts `provider.ObservedRequests.ShouldBeEmpty()`, proving the channel-control check is wired ONLY into the `ExecuteApprovedOutboundDraft` branch, so no non-send command can ever be blocked by a quarantine. Adding bespoke per-command auth-claim recipes would exceed the Story 7.24 template and risk fragile tests for zero additional behavioural coverage. Documented here rather than applied.

## Coverage / results

All suites run via compiled in-process runners, `-parallel none`:

| Suite | Total | Failed |
| --- | --- | --- |
| Server.Tests | **885** (was 884; +1 immutability test) | 0 |
| Contracts.Tests | 265 | 0 |
| Client.Tests (OpenAPI→client parity + SHA256) | 19 | 0 |
| Conformance.Tests (regression) | 75 | 0 |
| Architecture.Tests (regression) | 37 | 0 |

- Build: `dotnet build Hexalith.ChatBot.slnx` → **0 Warning(s), 0 Error(s)**.
- Submodule guard: `git submodule status` → no pointer drift.
- No `Workers.Tests` change (no worker touched — enforcement is at the gateway dispatcher's outbound send seam).

## Next steps

- Run the suites in CI (already green locally).
- When the durable read-side projection of `OutboundChannelQuarantined` into `IOutboundChannelControlStateProvider` lands (deferred, sanctioned), add an end-to-end test that quarantines via the two-person flow and then observes a real send fail closed through the live provider (today the send-seam is unit-tested in isolation via an injected fake).
