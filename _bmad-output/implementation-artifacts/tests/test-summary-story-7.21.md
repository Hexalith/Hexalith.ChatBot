# Test Automation Summary — Story 7.21 (Disable command capability)

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-03
**Engineer:** Jerome (QA automation pass)
**Test framework:** .NET `net10.0` / xUnit v3 + Shouldly + NSubstitute (project-existing; no new framework). No browser/UI E2E layer — this feature is a server-side command-admission control with no UI surface (AC6 satisfied by catalog + reason code + audit metadata), so coverage is API/behavioral via the in-process xUnit v3 runners.

## Scope

Story 7.21 was already implemented (Status: review) with tests in place. This QA pass mapped the existing automated coverage against the AC9 enumerated test obligations, found two gaps, and auto-applied focused tests mirroring the sibling 7.18 / 7.15 patterns. No production code was changed.

## Gaps discovered & auto-applied

### Gap 1 — Dispatcher distinct-approver guard had no test (AC2, AC9)
AC2/AC9 require the FR75d distinct-approver rule to be enforced at **three** layers: the gateway validator, the **accepted-command dispatcher**, *and* the aggregate. `AcceptedCommandDispatcher.cs` was modified to add the `ApproveCommandCapabilityDisable` distinct-approver guard (prod lines 297–309), but `AcceptedCommandDispatcherTests.cs` had **no** command-capability test — exactly the recurring File-List omission the Dev Notes flagged (7.18/7.12 reviews caught the same `AcceptedCommandDispatcherTests.cs` omission). The sibling AI-actor and service-client disables each have two dispatcher tests; command-capability had zero.

**Added** (`tests/.../Gateway/Stages/AcceptedCommandDispatcherTests.cs`):
- `DispatchShouldRouteCommandCapabilityDisableApprovalToDisableChangeAggregateForDistinctApprover` — distinct approver routes to the disable-change aggregate with a PascalCase metadata-only payload.
- `DispatchShouldRejectCommandCapabilityDisableApprovalWhenApproverEqualsRequester` — `RequesterRef == ApproverRef` throws, nothing submitted to the spine.
- `WireApproveCommandCapabilityDisableCommand` camelCase wire helper.

### Gap 2 — AC5/NFR17 cross-subject immutability had no aggregate test (AC5, AC9)
AC9 requires proving "the disable does not mutate existing committed/audit records." The sibling cells got this via their quarantine cell's `Handle*QuarantineShouldNotMutatePriorCommittedRecords` test, but command-capability has no quarantine cell yet (7.22), so it had **no** cross-subject immutability test. The existing `...NoOpForAlreadyDisabledOrDuplicate` test only covers idempotency, not preservation of a prior committed disable for a *different* command type.

**Added** (`tests/.../Operations/GovernedOperationAggregateTests.cs`):
- `HandleCommandCapabilityDisableShouldNotMutatePriorCommittedOrPendingRecords` — commits a disable for command type A, leaves an unrelated pending disable for type C, then disables target type B through the two-person flow; asserts A stays committed, C stays pending (distinct approver still owed), and only B is newly disabled (per-subject isolation; NFR17/FR75c).

## Pre-existing coverage verified present (AC1–AC9)

- **Authorization (AC1, AC2)** — `CommandCapabilityDisableAuthorizationTests`: policy-admin + tenant-admin (FR75a union) allowed; mailbox/compliance/operations-only denied; service/AI denied even with admin-looking claims; `RequesterRef==ApproverRef` denied at gateway; invalid metadata-only payloads denied.
- **Self-lockout guard (AC2)** — `SelfLockoutGuardShouldRejectDisablingAnFr74GovernanceCommand`.
- **Actor-agnostic enforcement seam (AC4)** — `DisabledCommandCapabilityShouldFailClosedForEveryActorBeforeGrantValidation` (human/service/AI → `command_capability_disabled`, before grant validation via sentinel-denying validator); reads only safe tenant id + command-type name.
- **Isolation (AC4, AC5)** — `DisabledCapabilityShouldNotAffectSiblingActiveTypeOrOtherTenants`; governance-command exemption `DisabledCapabilityCheckShouldExemptFr74GovernanceCommands`.
- **Aggregate two-person (AC1, AC3)** — proposal→pending (no disable), distinct-approver activates `CommandCapabilityDisabled`, subject/version/reason mismatch rejected, idempotency.
- **Fail-closed audit + redaction (AC3, AC7)** — `CommandGatewayTests`: audit-unavailable fails closed (503, no dispatch, replay intent); `Active->Disabled` metadata-only envelope with actor/scope(`admin-scope:policy`)/subject/reason/old/new-state/policy-snapshot/timestamp; no `@`/`secret`/`oauth`/`bearer`/`project-` leakage.
- **Contracts + catalog (AC6, AC8)** — `AdminContractTests` serialization + safe-token + finite schema-version; `MessageCatalogContractTests` `command_capability_disabled` code + finite `disabled-action` reason.
- **Client parity (AC8)** — `Client.Tests` OpenAPI/checksum parity green.

## Generated/Updated Tests

### API / behavioral tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs` — +2 dispatcher distinct-approver tests + wire helper (NEW coverage).
- [x] `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs` — +1 AC5/NFR17 cross-subject immutability test (NEW coverage).

### E2E (UI) tests
- None — feature has no UI surface (server-side admission control; AC6 satisfied without a UI).

## Test Results

| Suite | Total | Failed | Note |
|---|---|---|---|
| Server.Tests | **818** | 0 | was 815; +3 from this pass |
| Contracts.Tests | 261 | 0 | regression |
| Client.Tests | 17 | 0 | OpenAPI/checksum parity |
| Conformance.Tests | 75 | 0 | regression |
| Architecture.Tests | 37 | 0 | regression |

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 Warning(s), 0 Error(s).
- `git submodule status` → no submodule pointer drift.
- The 3 new tests confirmed individually: `Total: 3, Failed: 0`.

## Coverage

- AC1–AC9 acceptance criteria: covered (all AC9 enumerated obligations now have a corresponding test).
- Two-person distinct-approver defense-in-depth: now proven at **all three** layers (gateway ✓ existing, dispatcher ✓ added, aggregate ✓ existing).

## Next Steps

- Run in CI alongside the existing Epic 7 suites.
- File List: this pass adds `AcceptedCommandDispatcherTests.cs` to the story's Modified (tests) set — update the story File List to keep it exact (the recurring omission the Dev Notes warn about).
- The durable read-side projection feeding `ICommandCapabilityControlStateProvider` remains deferred (sanctioned 7.12/7.15/7.18 deferral); add projection-level tests when that flow lands.
