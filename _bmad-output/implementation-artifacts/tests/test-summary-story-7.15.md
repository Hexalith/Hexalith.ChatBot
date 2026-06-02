# Test Automation Summary — Story 7.15 (Disable service client)

**Workflow:** bmad-qa-generate-e2e-tests · **Date:** 2026-06-02 · **Engineer:** QA automation (Jerome)
**Framework:** xUnit v3 + Shouldly + NSubstitute (.NET `net10.0`) — the project's existing stack. No new framework introduced.
**Mode:** QA-only (no code review / story validation). Gaps auto-applied.

## Scope

Story 7.15 ships the **service-client × disable** cell of the FR74 series (two-person disable, fail-closed admission seam, `Active->Disabled` audit envelope). The implementation arrived in `review` with a large existing suite. This run mapped existing coverage against the AC9-enumerated proofs and closed the one genuine gap.

## Coverage Map — AC9 enumerated proofs vs. existing tests

| AC9 proof | Status before | Test |
|---|---|---|
| Single-actor disable never takes effect (proposal alone) | ✅ covered | `GovernedOperationAggregateTests.HandleServiceClientDisableProposalShouldCreatePendingWithoutDisabling` |
| Distinct second human tenant-admin applies the disable | ✅ covered | `...ShouldRequirePendingAndDistinctSecondActor` |
| `RequesterRef == ApproverRef` rejected — **gateway** | ✅ covered | `ServiceClientDisableAuthorizationTests.DisableApprovalShouldRequireHumanTenantAdminAndDistinctApprover` |
| `RequesterRef == ApproverRef` AND `RequesterActorId == UserId` rejected — **aggregate** | ✅ covered | `...ShouldRequirePendingAndDistinctSecondActor` (`selfApprovalByRef` + `selfApprovalByActor`) |
| Same rejection — **dispatcher** | ❌ **GAP** | **added** (see below) |
| Service-client + AI actors denied (propose + approve) with tenant-admin-looking claims | ✅ covered | `DisableProposalShouldRequireHumanTenantAdmin`, `DisableApproval...` |
| Non-tenant-admin scope (mailbox/policy/compliance/operations) denied | ✅ covered | `DisableProposalShouldRequireHumanTenantAdmin` |
| Disabled client fails closed at `ServiceClientGrantValidator` with `service_client_disabled` **before** grant-scope checks; distinct from `service_client_grant_revoked` | ✅ covered | `ServiceClientGrantAuthorizationTests.DisabledServiceClientShouldFailClosedBeforeGrantScopeChecks` |
| Sibling Active service client unaffected (isolation) | ✅ covered | `ActiveSiblingServiceClientShouldBeUnaffectedByDisabledPeer` |
| Audit envelope: actor/scope/subject/reason/old/new/policy-snapshot/timestamp + `StateTransition "Active->Disabled"`, no credential/OAuth/`@`/`secret`/project leakage | ✅ covered | `CommandGatewayTests.ServiceClientDisableAuditEnvelopeShouldCarryActiveToDisabledTransitionAndRemainMetadataOnly` |
| Audit-unavailable → no durable disable + no admission-block (fail closed) | ✅ covered | `ServiceClientDisableApprovalPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch` |
| OpenAPI / generated-client / checksum parity | ✅ covered | `Hexalith.ChatBot.Client.Tests` (checksum + parity) |

## Gap Found & Auto-Applied

**Gap:** AC2/AC9 require the FR75d two-person rule proven at the **accepted-command dispatcher** as well as the gateway and aggregate. The dispatcher guard for `ApproveServiceClientDisable` (`AcceptedCommandDispatcher.cs:241-253`) had **zero direct test coverage** — only the mailbox-source *quarantine* dispatcher guard was tested. (The 7.12 mailbox *disable* dispatcher path was likewise untested, relying on the quarantine test; for 7.15 AC9 is explicit per-story, so the proof was added.)

**Applied** in `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs` (mirroring the quarantine route/reject pair):

- `DispatchShouldRouteServiceClientDisableApprovalToDisableChangeAggregateForDistinctApprover` — a distinct approver routes to the disable-change aggregate (`AggregateId == "service-client-disable-001"`), forwarded payload PascalCase.
- `DispatchShouldRejectServiceClientDisableApprovalWhenApproverEqualsRequester` — `RequesterRef == ApproverRef` throws `InvalidOperationException("The service-client disable approval command is missing valid approval metadata.")` and submits nothing to the spine.
- `WireApproveServiceClientDisableCommand(...)` camelCase wire helper.

## Gaps Considered but **not** added (parity / scope)

- **Proposal-side (`SubmitServiceClientDisable`) gateway audit envelope** — the `admin-operation:service-client-disable` (non-approve) token has no gateway assertion, but the 7.12 mailbox precedent tests only the approval (`-approve`) envelope; the proposal token is covered by `AuditEnvelopeFactory` + `AdminContractTests` serialization. Adding one would exceed precedent parity. Skipped.
- **Read-side projection / query-gateway wiring** — deferred by the story (sanctioned 7.12/7.13 deferral). The validator seam is unit-tested in isolation via an injected `IServiceClientControlStateProvider` fake. Out of scope.

## Validation

```
dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false  → Build succeeded, 0 Warning(s), 0 Error(s)
Hexalith.ChatBot.Server.Tests -parallel none                    → Total: 721, Failed: 0  (was 719; +2 new dispatcher tests)
```

Only added tests touched (no contract/public-surface change) → Contracts/Client/Conformance/Architecture suites unaffected and compiled clean. No submodule / gitlink drift.

## Checklist (./checklist.md)

- [x] API tests generated/extended (dispatcher distinct-approver, two-person rule layer 3)
- [x] E2E (UI) tests — n/a (no UI surface; AC6 satisfied by catalog + reason code + audit metadata, S5 deferred per story)
- [x] Tests use standard framework APIs (xUnit v3 / Shouldly)
- [x] Happy path covered (distinct-approver routes through)
- [x] Critical error case covered (same-person approval rejected, nothing dispatched)
- [x] All generated tests run successfully (721/721 green)
- [x] Proper, semantic assertions; clear descriptions
- [x] No hardcoded waits/sleeps
- [x] Tests are independent (no order dependency; `-parallel none` green)
- [x] Summary created with coverage map
```
```
