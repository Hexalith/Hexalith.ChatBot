# Test Automation Summary

## Story

Story 7.4: Compliance-admin scope.

## Generated Tests

### API / Endpoint Tests

- [x] Added `SearchShouldAllowHumanTenantAdminAndDenyAiActorBeforeReturningRows` in `tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditInvestigationEndpointTests.cs`.
- [x] Added `SearchWithoutComplianceScopeShouldDenyFinerAdminRolesBeforeReturningRows` in `tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditInvestigationEndpointTests.cs`.
- [x] New endpoint coverage proves human `tenant-admin` can read compliance audit rows through the governed HTTP path.
- [x] New endpoint coverage proves AI actors and valid non-compliance admin roles (`mailbox-admin`, `operations-admin`, `policy-admin`) are denied before rows or tenant identifiers are returned.

### E2E / UI Tests

- [x] Existing `tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs` covers S9 metadata-only audit timeline, safe escalation, investigation trigger, denied workflow mutation, retention validation/focus behavior, safe retention snapshot submission, and phone fallback.
- [x] Existing UI E2E tests use Playwright role/label locators with browserless fixture fallback and assert absence of restricted content markers.

## Coverage

- API endpoints: compliance audit search/detail endpoint coverage includes tenant row filtering, replay exclusion, tenant-admin allow, compliance-admin allow, service/AI denial, non-compliance admin denial, unauthenticated denial, unresolved tenant denial, unsafe filter denial, metadata-only response checks, per-project restricted detail, and no WORM-chain mutation on reads.
- UI workflows: S9 audit investigation and S5 retention configuration fixture coverage includes happy paths, critical validation errors, semantic locators, focus management, disabled-action explanation, safe escalation, small-screen read-only fallback, and metadata-only/no restricted marker assertions.
- Supporting tests: contracts, server gateway/read policy, client generation, architecture, conformance, and UI contract suites already cover safe tokens, bounded retention windows, audit-unavailable fail-closed writes, metadata-only audit refs, OpenAPI/client parity, role/scope mapping, redaction, and gateway/audit/admission boundaries.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 1571/1571.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - passed, 87/87.
- [x] `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 482/482.
- [x] `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 34/34.
- [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 39/39.
- [x] `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 93/93.
- [x] `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - passed, 131/131.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E tests generated where UI exists.
- [x] Tests use standard xUnit v3, Shouldly, ASP.NET TestServer/WebApplicationFactory, and Playwright APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases: AI denial, non-compliance admin denial, service denial, unsafe filters, restricted detail, invalid retention, and metadata leakage.
- [x] All generated tests run successfully.
- [x] Tests use semantic, accessible locators where UI is involved.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent.
- [x] Test summary created.
- [x] Tests saved to the appropriate existing test directories.
- [x] Summary includes coverage metrics.

## Next Steps

- None for Story 7.4 test automation.
