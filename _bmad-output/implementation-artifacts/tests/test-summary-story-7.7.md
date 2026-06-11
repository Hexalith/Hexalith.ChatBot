# Test Automation Summary - Story 7.7 (Escalation policy for unresolved states)

**Story:** 7.7 - Escalation policy for unresolved states
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-11
**Author:** QA automation engineer
**Framework:** xUnit v3 + Shouldly + Playwright fixtures (.NET 10, `net10.0`), compiled in-process runners (`-parallel none`, per the story sandbox note).
**Mode:** Auto-apply all discovered gaps in tests.

## Scope

Story 7.7 reuses the Story 7.6 routing/delivery spine end-to-end. The feature ships on the generic
command-submission transport with **no new public HTTP endpoint/schema** (AC8 - OpenAPI/generated
client intentionally unchanged), so there is **no public REST surface to generate separate API
status-code tests against**. Coverage is the existing in-process layered suites plus the new UI E2E
workflow tests.

## Pre-existing Coverage Verified Against AC9

- Age-over escalates / under-both does not / severity-at-or-over escalates regardless of age - `EscalationPolicyEvaluatorTests`
- Terminal & resolved items never escalate; strictly-greater age boundary; at-or-above severity boundary - `EscalationPolicyEvaluatorTests`
- Server-measured UTC age, never item-supplied age - `EscalationPolicyEvaluatorTests`
- All five escalatable state classes and mapping rules - `EscalationPolicyEvaluatorTests`
- Routes to configured target via the routing engine; unauthorized target receives redacted content with no existence leakage - `EscalationPolicyEvaluatorTests`
- Schema-invalid policy produces no escalations fail-closed - `EscalationPolicyEvaluatorTests`
- Per-fired-escalation metadata-only audit with FR59 correlation context; audit-unavailable fail-closed and deliver nothing - `EscalationEvaluationCoordinatorTests`
- Edit authorization: policy-admin/tenant-admin allow; mailbox/compliance/operations-admin, service, AI deny; invalid/stale payloads deny - `EscalationPolicyAuthorizationTests`
- Edit fail-closed when pre-commit audit unavailable; metadata-only audit refs - `CommandGatewayTests`
- Schema-bound snapshot projection + read-back gated to `AdminScope.Policy` - `EscalationPolicyProjectorTests`
- Contract closure / `MaxEntries` / severity ladder / target-role rejection / secret-bearing property bans - `EscalationPolicyContractTests`
- Matrix UI bounded selectors, numeric age, localization, no restricted markers - `ChatBotEscalationPolicyEditorContractTests`

## Gaps Discovered And Auto-Applied

### Gap 1 - Missing escalation policy editor E2E workflow coverage

Story 7.7 had UI component/design-contract tests but no `UI.E2E.Tests` coverage equivalent to Story
7.6's notification routing editor. Added `tests/Hexalith.ChatBot.UI.E2E.Tests/EscalationPolicyEditorE2ETests.cs` with:

- `EscalationPolicyEditor_MatrixEdit_SubmitsMetadataOnlyGovernedCommand`
- `EscalationPolicyEditor_ValidationFailure_FocusesSummaryAndBlocksDurableWrite`
- `EscalationPolicyEditor_PhoneFallback_PreservesSummaryAndSafeSubmitAction`

The tests use semantic Playwright locators, bounded fixture controls, fixture fallback when no browser
is available, and metadata-only assertions.

## Coverage

- AC9 enumerated acceptance behaviors: 12/12 covered.
- Escalatable state classes exercised in the evaluator: 5/5.
- UI E2E escalation editor workflows: 3/3 expected flows covered (happy path, validation failure, phone fallback).
- Schema-bounded value-rejection dimensions: state-class, scope, severity, target-role, channel, age-range, duplicate-key, max-entries.
- Public API endpoints: 0/0 - no new public REST surface added (generic command transport, AC8).

## Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`: **succeeded, 0 warnings, 0 errors**.
- `Hexalith.ChatBot.UI.E2E.Tests -parallel none`: **Total 94, Failed 0**.
- `Hexalith.ChatBot.Contracts.Tests -parallel none`: **Total 482, Failed 0**.
- `Hexalith.ChatBot.Server.Tests -parallel none`: **Total 1583, Failed 0**.
- `Hexalith.ChatBot.UI.Tests -parallel none`: **Total 131, Failed 0**.
- `Hexalith.ChatBot.Conformance.Tests -parallel none`: **Total 93, Failed 0**.
- `Hexalith.ChatBot.Architecture.Tests -parallel none`: **Total 39, Failed 0**.
- `Hexalith.ChatBot.Client.Tests -parallel none`: **Total 34, Failed 0**.

## Files Changed

- `tests/Hexalith.ChatBot.UI.E2E.Tests/EscalationPolicyEditorE2ETests.cs` - new E2E tests for the escalation policy editor.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` - default workflow summary updated.
- `_bmad-output/implementation-artifacts/tests/test-summary-story-7.7.md` - story-specific summary updated.

## Checklist Validation

- [x] API tests generated if applicable; no new public API gap was found beyond existing Story 7.7 command/gateway coverage.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover the happy path.
- [x] Tests cover critical error cases: validation failure, durable write suppression, focus recovery, phone fallback, and restricted-marker absence.
- [x] All generated tests run successfully with the in-process xUnit runner.
- [x] Tests use semantic, accessible locators.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent.
- [x] Test summary created.
- [x] Tests saved to the appropriate existing test directory.
- [x] Summary includes coverage metrics.

## Next Steps

- When the deferred Dapr-timer/workflow runtime trigger is bound for the escalation coordinator, add a runtime-binding integration test that drives the live evaluate-to-deliver-to-audit cycle.
