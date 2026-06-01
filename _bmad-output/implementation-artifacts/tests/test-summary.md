# Test Automation Summary

**Story:** 3.9 - Why this project evidence and provenance panel
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-01
**Framework:** xUnit v3, Shouldly, Microsoft.Playwright
**Run method:** compiled xUnit v3 executable after `dotnet test` hit the known VSTest socket restriction in this sandbox.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationHttpHost.cs` - added seeded association routing-status records for own-tenant and foreign-tenant read-surface isolation checks.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs` - added routing-status API coverage for foreign, unknown, malformed, missing-tenant, ambiguous-tenant, stale-tenant, and unsafe-tenant contexts collapsing to indistinguishable safe denial.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs` - added positive owner/own controls proving metadata-only routing-status responses expose safe evidence tokens while excluding raw `decisionNote` and `correctionRationale` fields.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added why-panel workflow coverage for opening from the email row and the association decision row using semantic button locators.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added ordered panel metadata assertions for signal class, matched value, confidence, threshold policy, scorer/kernel version, actor, timestamp, source provenance, source version, correlation id, redaction state, schema version, safe next action, and authorized evidence rows.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added redacted evidence, focus reachability, superseding correction navigation, close action, forced-colors, reduced-motion, phone-width layout, and hidden-resource negative assertions.

## Coverage

- API endpoints: `GET /api/v1/associations/{associationId}/routing-status` covered for happy path, owner/own positive controls, foreign/unknown/malformed safe denial, and tenant-context failure denial.
- UI states: S1 populated stream now covers why-panel open from email and decision rows, redacted evidence, correction link navigation, and close behavior.
- Critical safety cases: metadata-only evidence, no raw decision note, no raw correction rationale, no hidden project/participant/file names, no raw mailbox body/provider/policy/audit/prompt/output/tool payload text, distinct redacted evidence state, and accessible complementary panel labels.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `dotnet test tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj --no-build --no-restore -m:1 /nr:false` - VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- [x] `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-build --no-restore -m:1 /nr:false` - VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none -class Hexalith.ChatBot.Conformance.Tests.CrossTenantReadSurfaceIsolationTests` - passed 10/10.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 9/9.

## Checklist

- [x] API tests generated where applicable.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy path.
- [x] Tests cover 1-2 critical error cases.
- [x] Tests use semantic roles, labels, accessible names, and stable metadata selectors.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created in the configured output path.
- [x] Tests saved to the appropriate test projects.
- [x] Summary includes coverage metrics and validation commands.
