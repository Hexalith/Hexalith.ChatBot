# Test Automation Summary — Story 7.13 (Quarantine mailbox source)

Workflow: `bmad-qa-generate-e2e-tests` · Date: 2026-06-02 · Engineer: Jerome (QA automation)
Framework: .NET `net10.0` / xUnit v3 + Shouldly + NSubstitute (compiled in-process runners, `-parallel none`).
Note: this is a backend governance feature — there is no browser UI surface (the S5 admin status surface was deliberately deferred per the story), so "E2E" coverage is the gateway→aggregate→worker integration tests the project already uses, not Playwright/Cypress.

## Approach

Story 7.13 was already in `review` with a dev-authored test suite. This workflow performed a **gap analysis of the existing coverage against AC9's enumerated requirements** and the story's three-layer two-person rule, then auto-applied the one discovered gap.

## Coverage map (AC9 requirements → existing tests)

| AC9 requirement | Covered by | Status |
| --- | --- | --- |
| Single-actor quarantine never takes effect (proposal alone) | `GovernedOperationAggregateTests.HandleMailboxSourceQuarantineProposalShouldCreatePendingWithoutQuarantining` | ✅ existing |
| Distinct second human approver applies | `…ApprovalShouldRequirePendingAndDistinctSecondActor` | ✅ existing |
| `RequesterRef == ApproverRef` rejected — **gateway** | `MailboxSourceQuarantineAuthorizationTests.QuarantineApprovalShouldRequireHumanMailboxScopeAndDistinctApprover` (`selfApproval`) | ✅ existing |
| `RequesterRef == ApproverRef` rejected — **aggregate** | `…ApprovalShouldRequirePendingAndDistinctSecondActor` (`selfApprovalByRef`) | ✅ existing |
| `RequesterActorId == approver UserId` rejected — aggregate | `…ApprovalShouldRequirePendingAndDistinctSecondActor` (`selfApprovalByActor`) | ✅ existing |
| `RequesterRef == ApproverRef` rejected — **dispatcher (3rd layer)** | `AcceptedCommandDispatcherTests.DispatchShouldRejectMailboxSourceQuarantineApprovalWhenApproverEqualsRequester` | ➕ **added** |
| Distinct approver routes to quarantine-change aggregate (dispatcher happy path) | `AcceptedCommandDispatcherTests.DispatchShouldRouteMailboxSourceQuarantineApprovalToQuarantineChangeAggregateForDistinctApprover` | ➕ **added** |
| Service clients / AI actors denied — proposal **and** approval | `MailboxSourceQuarantineAuthorizationTests` (both Fact methods) | ✅ existing |
| Non-mailbox/non-tenant-admin scope denied | `…ShouldRequireHumanMailboxScope` (policy/compliance/operations-admin) | ✅ existing |
| Quarantined source routes `Recoverable("mailbox_source_quarantined")` before fetch/submit (no content read) | `GraphMailboxIntakeWorkerTests.QuarantinedMailboxSourceShouldRouteIntakeBeforeFetchWhileSiblingActiveSourceIsUnaffected` (asserts `FetchCount == 0`, no submission) | ✅ existing |
| Sibling Active source unaffected (isolation) | same worker test (second `ProcessAsync`) | ✅ existing |
| Owner role = mailbox-admin, recoverable/await-admin (not poison, no auto-retry) | same worker test (`OwnerRole`, `NextRetryAt == null`, `SafeNextAction == "escalate"`) | ✅ existing |
| Audit envelope carries actor/scope/subject/reason/old-state/new-state/policy-snapshot/timestamp + `StateTransition "Active->Quarantined"` | `CommandGatewayTests.MailboxSourceQuarantineAuditEnvelopeShouldCarryActiveToQuarantinedTransitionAndRemainMetadataOnly` | ✅ existing |
| No mailbox-content / PII / `@` / `secret` / `project-` leakage | same gateway test + `AdminContractTests.MailboxSourceQuarantineContractsShouldSerializeFiniteStateTokensAndMetadataOnlyFields` | ✅ existing |
| Audit-unavailable → no durable quarantine + no intake-routing side effect (fail closed) | `CommandGatewayTests.MailboxSourceQuarantineApprovalPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch` | ✅ existing |
| Subject/version/reason mismatch + unknown pending rejected; duplicate / already-quarantined → NoOp | `…RejectSubjectVersionOrReasonMismatch`, `…NoOpForDuplicateOrAlreadyQuarantined` | ✅ existing |
| New `Quarantined` state + `(Active, Quarantined)` transition vocabulary | `LifecycleStateModelTests` (`"Quarantined"`, `InlineData("Active","Quarantined")`) | ✅ existing |
| Wire tokens / message-catalog code | `AdminContractTests`, `MessageCatalogContractTests.…ShouldContain(ChatBotMessageCodes.MailboxSourceQuarantined)` | ✅ existing |
| OpenAPI / generated-client / checksum parity | `Hexalith.ChatBot.Client.Tests` (17 tests, checksum fixture) | ✅ existing |

## Gap discovered and applied

The story's Dev Notes require the FR75d two-person rule to be enforced **three** times — gateway validation, the `AcceptedCommandDispatcher` guard, **and** the aggregate. The dispatcher guard for `ApproveMailboxSourceQuarantine` exists in production code (`AcceptedCommandDispatcher.cs:246`, rejects `RequesterRef == ApproverRef`) but had **no test** — the dispatcher layer was the only one of the three left unverified (the disable equivalent is also untested, but that is out of 7.13 scope).

Two tests added to `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`:

- `DispatchShouldRouteMailboxSourceQuarantineApprovalToQuarantineChangeAggregateForDistinctApprover` — a distinct approver routes to the `QuarantineChangeId` aggregate with a PascalCase payload (happy path).
- `DispatchShouldRejectMailboxSourceQuarantineApprovalWhenApproverEqualsRequester` — same-person approval throws and **nothing is submitted to the spine** (closes the third-layer gap).

## Results

- `dotnet build tests/Hexalith.ChatBot.Server.Tests` → Build succeeded, 0 Warning(s), 0 Error(s).
- `Hexalith.ChatBot.Server.Tests -parallel none` → **Total 701, Failed 0** (was 699; +2 added). All green.

## Next steps

- Optional follow-up (out of 7.13 scope): add the symmetric dispatcher-guard test for `ApproveMailboxSourceDisable` (Story 7.12), which is likewise untested at the dispatcher layer.
- No production code changed; only the two new tests in `AcceptedCommandDispatcherTests.cs` were added.
