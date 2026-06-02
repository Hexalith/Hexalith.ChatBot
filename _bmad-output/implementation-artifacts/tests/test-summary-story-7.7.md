# Test Automation Summary — Story 7.7 (Escalation policy for unresolved states)

**Story:** 7.7 - Escalation policy for unresolved states
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-02
**Author:** QA automation engineer
**Framework:** xUnit v3 + Shouldly + NSubstitute (.NET 10, `net10.0`), compiled in-process runners (`-parallel none`, per the story sandbox note — `dotnet test`/VSTest can hit `SocketException (13)`).
**Mode:** Auto-apply all discovered gaps in tests.

## Scope

Story 7.7 reuses the Story 7.6 routing/delivery spine end-to-end. The feature ships on the generic
command-submission transport with **no new public HTTP endpoint/schema** (AC8 — OpenAPI/generated
client intentionally unchanged), so there is **no public REST surface to generate API status-code
tests against**. Coverage is the existing in-process layered suites: contracts, server
evaluator/coordinator, gateway authorization/audit, projection/read-policy, and the UI
design-contract / component bUnit tests.

## Pre-existing coverage (verified against AC9)

All AC9-enumerated behaviors already had at least one passing test:

- Age-over escalates / under-both does not / severity-at-or-over escalates regardless of age — `EscalationPolicyEvaluatorTests`
- Terminal & resolved items never escalate; strictly-greater age boundary; at-or-above severity boundary — `EscalationPolicyEvaluatorTests`
- Server-measured UTC age (never item-supplied) — `EscalationPolicyEvaluatorTests`
- Routes to configured target via the routing engine; unauthorized target → redacted, no existence leakage — `EscalationPolicyEvaluatorTests`
- Schema-invalid policy → fail-closed (no escalations) — `EscalationPolicyEvaluatorTests`
- Per-fired-escalation metadata-only audit with FR59 correlation context; audit-unavailable → fail-closed, deliver nothing — `EscalationEvaluationCoordinatorTests`
- Edit authorization: policy-admin/tenant-admin allow; mailbox/compliance/operations-admin, service, AI deny; invalid/stale payloads deny — `EscalationPolicyAuthorizationTests`
- Edit fail-closed when pre-commit audit unavailable; metadata-only audit refs — `CommandGatewayTests`
- Schema-bound snapshot projection + read-back gated to `AdminScope.Policy` — `EscalationPolicyProjectorTests`
- Contract closure / `MaxEntries` / severity ladder / secret-bearing property bans — `EscalationPolicyContractTests`
- Matrix UI bounded selectors, numeric age, localization, no restricted markers — `ChatBotEscalationPolicyEditorContractTests`

## Gaps discovered and auto-applied

### Gap 1 — Evaluator coverage of the four other escalatable state classes + mapping rules
AC1 names **five** escalatable state classes; every evaluator test only exercised `Failure`. The
`EscalationStateClassMap` queue-family mapping, quarantine-dominance, and health-promotion logic was
untested at the evaluator level. Added 4 tests to
`tests/Hexalith.ChatBot.Server.Tests/Notifications/EscalationPolicyEvaluatorTests.cs`:

- `PendingApprovalItemShouldEscalateAgainstTheApprovalPendingEntry`
- `AmbiguousAssociationItemShouldEscalateAgainstTheReviewNeededEntry`
- `QuarantineSignalShouldDominateTheQueueFamilyMapping` (quarantine status overrides the family map)
- `DegradedHealthShouldPromoteARetryableItemToTheDegradedEntry` (retry → degraded health promotion)

### Gap 2 — Schema rejection of an undeclared escalation-target `AdminRole`
AC5 requires escalation-target roles to be declared `AdminRole` values; the contract test exercised
out-of-range state-class/severity/channel but not the target role (whose validator emits
`escalation_policy_target_role_invalid`). Added an assertion block to
`EscalationMapShouldRejectUndeclaredValuesAndOutOfRangeAge` in
`tests/Hexalith.ChatBot.Contracts.Tests/EscalationPolicyContractTests.cs`.

## Coverage

- AC9 enumerated acceptance behaviors: 12/12 covered (depth increased on AC1 state-class breadth and AC5 role validation).
- Escalatable state classes exercised end-to-end in the evaluator: **5/5 (was 1/5)**.
- Schema-bounded value-rejection dimensions: state-class, scope, severity, **target-role (new)**, channel, age-range, duplicate-key, max-entries.
- Public API endpoints: 0/0 — no public REST surface added (generic command transport, AC8).

## Validation

- `dotnet build Hexalith.ChatBot.slnx` (full solution): **succeeded, 0 warnings, 0 errors**.
- `Hexalith.ChatBot.Contracts.Tests -parallel none`: **Total 192, Failed 0**.
- `Hexalith.ChatBot.Server.Tests -parallel none`: **Total 592, Failed 0** (+4 new evaluator tests vs the 588 baseline).

## Files changed (test-only)

- `tests/Hexalith.ChatBot.Server.Tests/Notifications/EscalationPolicyEvaluatorTests.cs` (+4 facts, +1 `ClassPolicy` helper)
- `tests/Hexalith.ChatBot.Contracts.Tests/EscalationPolicyContractTests.cs` (+1 assertion block)

## Next Steps

- Run the full suite matrix in CI (UI/Conformance/Architecture unaffected — test-only changes in two files).
- When the periodic Dapr-timer/workflow trigger for the escalation coordinator is bound (currently
  deferred per the story's completion notes), add a runtime-binding integration test that drives the
  live evaluate→deliver→audit cycle.
