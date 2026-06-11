# Test Automation Summary

## Story

Story 7.2: Policy-admin scope, Tenant Policy Schema editor, and AI action policy.

## Generated Tests

### API / Contract Tests
- [x] Added Tenant Policy Schema sensitivity classification coverage in `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`.
- [x] Added policy enum, string-list, and admin-scope shape rejection coverage in `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`.
- [x] Added aggregate coverage proving every declared schema knob follows its declared sensitivity: security-sensitive knobs create pending two-person approval and standard knobs activate directly in `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`.
- [x] Added AI action policy coverage for unavailable snapshots and unknown action-class tokens routing to approval in `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionPolicyEvaluatorTests.cs`.

### E2E Tests
- [x] Added `TenantPolicyEditorPermissionBlockedShouldExplainDisabledSaveWithoutPolicyBody` in `tests/Hexalith.ChatBot.UI.E2E.Tests/TenantPolicyEditorE2ETests.cs`.
- [x] The new E2E coverage asserts permission-blocked recovery copy, disabled-save `aria-describedby`, metadata-only output, and absence of raw claim/header details.
- [x] The test follows the existing Playwright semantic-locator path and the browserless fixture assertion fallback.

## Coverage

- API/contract endpoints and contracts: Story 7.2 policy schema validation, closed knob classification, enum/range/map/string-list/admin-scope rejection, and AI policy approval routing are covered.
- Server workflows: policy-admin/tenant-admin authorization, service/AI denial, two-person approval, non-sensitive direct activation, audit fail-closed behavior, and metadata-only audit refs are covered by existing and added server tests.
- UI workflows: Tenant Configuration S5 validation, pending approval, conflict recovery, permission-blocked recovery, phone fallback, mailbox metadata, disabled-action explanation, semantic locators, and metadata-only rendering are covered.
- Public contract drift: existing OpenAPI/client drift tests remain in the contract/client suites; no public contract or generated client changes were made in this workflow.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 482/482.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 1567/1567.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - passed, 83/83.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated where UI exists.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy paths for schema-sensitive approval routing and permission-blocked UI recovery.
- [x] Tests cover critical error cases: enum rejection, unsafe string-list rejection, duplicate admin-scope rejection, unavailable AI policy, unknown action class, and metadata-only permission denial.
- [x] All generated tests run successfully.
- [x] Tests use semantic, accessible locators.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent.
- [x] Test summary created.
- [x] Tests saved to the appropriate existing test directories.
- [x] Summary includes coverage metrics.

## Next Steps

- None for Story 7.2 test automation.
