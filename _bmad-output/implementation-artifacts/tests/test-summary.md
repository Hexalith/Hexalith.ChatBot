# Test Automation Summary - Story 7.2

**Story:** 7.2 - Policy-admin scope, Tenant Policy Schema editor, and AI action policy
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-02
**Framework:** xUnit v3 in-process runners, Shouldly, and Microsoft Playwright static browser fixtures.

## Generated Tests

### API Tests

- [x] Existing contract/API coverage in `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` covers the closed Tenant Policy Schema, declared knob ids, sensitivity, defaults, range/enum/map validation, command contracts, serialization, and secret-bearing property bans.
- [x] Existing server authorization coverage in `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs` covers policy-admin/tenant-admin allow, service/AI denial, non-policy-admin denial, invalid closed-schema payload denial, unsafe metadata denial, and distinct-requester/approver checks.
- [x] Existing gateway/API coverage in `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` covers policy mutation admission through the command spine, audit-unavailable fail-closed behavior, metadata-only refs, and public command submission outcomes.
- [x] Existing AI policy coverage in `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionPolicyEvaluatorTests.cs` covers per-action-class low-risk behavior, stale/expired/invalid policy routing, and safe approval-required defaults.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/TenantPolicyEditorE2ETests.cs` - added browser-level S5 Tenant Policy editor fixture coverage for validation failure, pending two-person approval, phone fallback, save conflict recovery, and metadata-only rendering.
- [x] Existing `tests/Hexalith.ChatBot.UI.Tests/ChatBotTenantPolicyEditorContractTests.cs` keeps component/static contract coverage for localization, validation summary placement, `aria-invalid`, `aria-describedby`, disabled action explanations, and small-screen fallback markers.
- [x] Existing conformance tests continue to cover command gateway and cross-surface authorization boundaries for actor isolation and policy command surfaces.

## Coverage

- API/contracts: closed schema validation, unknown-knob denial, range/enum rejection, sensitivity classification, AI low-risk map defaults, command contract drift, OpenAPI/client drift, and metadata-only serialization.
- Authorization/gateway: human policy-admin and tenant-admin allow, non-policy-admin deny, service-client/AI deny, invalid payload deny before dispatch, distinct second-admin approval, audit pre-commit fail-closed behavior, and safe audit refs.
- AI policy: per-class low-risk evaluation, all six action classes, safe defaults, stale/expired/invalid policy routing, and approval-required routing for risky or disabled classes.
- UI/E2E: S5 validation summary before fields, focus to summary on validation failure, semantic field associations, disabled save reason, pending two-person approval state, safe conflict cause, phone read-only fallback, preserved draft marker, and restricted text redaction.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - passed, 56 tests.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 149 tests.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 536 tests.
- [x] `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - passed, 99 tests.
- [x] `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 15 tests.
- [x] `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 75 tests.
- [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 37 tests.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated for the S5 UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy paths: policy-admin command acceptance, metadata-only audit, AI per-class allowed routing, and pending approval visibility.
- [x] Tests cover critical error cases: unknown/invalid knobs, service/AI denial, non-policy-admin denial, self-approval denial, audit unavailable, stale-data conflict, invalid UI fields, and phone dense-editing fallback.
- [x] Tests use semantic accessible locators and field associations.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and run without order dependency.
- [x] Test summary created with coverage metrics.

## Discovered Gaps Applied

- Added missing browser-level E2E coverage for the Tenant Configuration S5 policy editor. The new tests verify validation summary placement and focus, `aria-invalid`/`aria-describedby`, per-class AI action controls, disabled save explanation, pending two-person approval metadata, safe conflict recovery, phone fallback behavior, draft preservation, and restricted text redaction.
- Story-automator review added regression coverage for duplicate policy knob ids, finite policy schema versions, aggregate rejection of unknown schema versions, and metadata-only old/new value fingerprint audit refs.
