# Test Automation Summary — Story 7.26 (Rate-limit outbound channel)

Workflow: `bmad-qa-generate-e2e-tests` · Framework: **xUnit v3** (.NET 10, compiled in-process runners) · Date: 2026-06-03

## Scope

Story 7.26 (status: `review`) ships the **outbound channel × rate-limit** cell — a single-actor,
policy-admin-gated, schema-bounded send budget enforced as the **last gate of the outbound send seam**
(`AcceptedCommandDispatcher.ExecuteApprovedOutboundDraft`, after the Disabled/Quarantined control-state switch).
QA verified the existing automated suite against the AC-10 coverage matrix, found one explicit gap, and closed it.

## AC-10 Coverage Matrix (existing tests verified)

| AC requirement | Test | Status |
|---|---|---|
| Single human policy-admin applies (no approver) + tenant-admin via FR75a union | `OutboundChannelRateLimitAuthorizationTests.RateLimitShouldRequireSingleHumanPolicyAdminWithNoApprover` | ✅ |
| Non-policy human scope / service / AI denied (even with admin claims) | same test (denied-actor loop) | ✅ |
| Out-of-bounds / undeclared budget rejected at **gateway** | `…RateLimitShouldRejectOutOfBoundsOrUndeclaredBudgetAtGateway` | ✅ |
| Out-of-bounds rejected at **aggregate** | `GovernedOperationAggregateTests.HandleOutboundChannelRateLimitShouldRejectOutOfBoundsBudget` / `…RejectInvalidMetadata` | ✅ |
| Out-of-bounds → safe-default fallback at enforcement seam (never raises cap) | `AcceptedCommandDispatcherTests.OutOfBoundsConfiguredOutboundBudgetShouldFallBackToSafeDefaultNeverRaisingTheCap` | ✅ |
| At-budget send fails closed **before** `SendAsync` (spy never invoked), reason `outbound_channel_rate_limited` | `…DispatchShouldFailClosedAtSendSeamBeforeAdapterWhenOutboundChannelAtRateLimitBudget` | ✅ |
| Under-budget send proceeds | `…DispatchShouldSendNormallyWhenOutboundChannelUnderRateLimitBudget` | ✅ |
| Sibling channel + cross-tenant isolation (NFR30) | `…DispatchShouldIsolateSiblingChannelsAndOtherTenantsForRateLimit` / `…RateLimitsShouldKeepEachChannelBudgetIndependent` | ✅ |
| Disabled→`outbound_channel_disabled`, Quarantined→`outbound_channel_quarantined` regression (control switch precedes gate) | `…DispatchShouldKeepControlStateReasonOverRateLimitGate` | ✅ |
| Trailing-window count (aged-out + future sends ignored) | `…DispatchShouldCountOnlyAdmittedSendsInsideTheTrailingWindowForRateLimit` | ✅ |
| Reason **distinct** from disabled/quarantined/adapter-unavailable/not-approved/actor+capability rate-limits | `…HandleOutboundSendShouldFailClosedWithOutboundChannelRateLimitedReasonWhenChannelRateLimited` | ✅ |
| Drafts/approvals stay inspectable — `CreateOutboundDraft` | `…DispatchShouldLeaveOutboundDraftInspectableWhenChannelRateLimitedAndNeverConsultRateLimitSeams` | ✅ |
| Drafts/approvals stay inspectable — `RequestOutboundSendApproval` + `DecideOutboundApproval` | **`…DispatchShouldLeaveOutboundApprovalRequestAndDecisionInspectableWhenChannelRateLimited`** | ➕ **Added (gap)** |
| Audit envelope: actor/scope/subject/reason/old/new/window/policy-snapshot/timestamp, `admin-scope:policy`, **no** `StateTransition`, no leakage | `CommandGatewayTests.OutboundChannelRateLimitAuditEnvelopeShouldCarryBudgetWindowAndRemainMetadataOnly` | ✅ |
| Audit-unavailable → fail closed (no durable rate-limit, no enforcement effect) | `CommandGatewayTests.OutboundChannelRateLimitPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch` | ✅ |
| Idempotent re-submit → `IsNoOp`; first submit → configure event | `…HandleOutboundChannelRateLimitShouldNoOpForIdenticalBudgetResubmit` / `…ShouldConfigureDirectlyWithoutPendingEvent` | ✅ |
| Catalog guidance: `retry-later` + `dependency-degraded`, headline ≤ 80 chars | `MessageCatalogContractTests` (OutboundChannelRateLimited assertions) | ✅ |
| Contract/serialization safe-token + bounds pin | `AdminContractTests.OutboundChannelRateLimitContractShouldSerialize…` | ✅ |
| OpenAPI → client → checksum parity | `ClientGenerationTests.GeneratedClientShouldContainOutboundChannelRateLimitContractWithSafeMetadataOnly` + checksum test | ✅ |
| Capacity-impact observation = finite integer tokens (AC6) | `…OutboundChannelCapacityImpactObservationShouldCarryFiniteIntegerBudgetCountAndThrottledFlag` | ✅ |

## Gap Discovered & Auto-Applied

**Gap:** AC-10 explicitly enumerates **three** pre-send steps that must stay un-throttled for a rate-limited
channel — `CreateOutboundDraft`, `RequestOutboundSendApproval`, `DecideOutboundApproval` — but the existing
inspectability test only exercised `CreateOutboundDraft`; the two approval steps were asserted only in a code
comment ("the approval steps that share this seam").

**Fix (test-only, no production change):** added
`AcceptedCommandDispatcherTests.DispatchShouldLeaveOutboundApprovalRequestAndDecisionInspectableWhenChannelRateLimited`,
which dispatches `RequestOutboundSendApproval` and `DecideOutboundApproval` through a dispatcher with an **at-budget
(0)** limit configured for the channel and asserts each dispatches normally, the send adapter spy is never invoked,
and the rate-limit/history seams are **never consulted** (the gate is structurally unreachable off the
`ExecuteApprovedOutboundDraft` branch). Added two command helpers `OutboundApprovalRequest` / `OutboundApprovalDecision`.

## Files Changed

- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs` — new test + 2 helpers.

## Results

- `dotnet build tests/Hexalith.ChatBot.Server.Tests` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- New test in isolation → **Total: 1, Failed: 0**.
- Full `Hexalith.ChatBot.Server.Tests` → **Total: 904, Failed: 0** (was 903; +1).
- Other suites unaffected (test-only change in Server.Tests; no contract/client change). Story-recorded green
  baselines stand: Contracts 266, Client 20, Conformance 75, Architecture 37 — all 0 failures.

## Coverage

- AC-10 enumerated checks: **22/22 covered** (21 pre-existing, 1 added).
- Outbound rate-limit feature: API (authorization, aggregate, gateway/audit, contract/client) + send-seam
  enforcement fully covered. No further gaps.

## Next Steps

- None required for Story 7.26 QA. Run the suite in CI as part of the normal gate.
