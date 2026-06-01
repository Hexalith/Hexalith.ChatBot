# Test Automation Summary

**Story:** 3.5 - Association and correction decision rendering
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-01
**Framework:** xUnit v3, Shouldly, Microsoft.Playwright
**Run method:** `dotnet test` builds the assembly but aborts in this sandbox at VSTest socket startup; validation used the compiled xUnit v3 executable.

## Generated Tests

### API Tests

- [x] Existing Story 3.5 contract/API coverage is present in `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`, and `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs` for additive decision/correction DTO fields, stable `system-decision` wire tokens, generated-client availability, OpenAPI shape, and raw note/rationale/provider/evidence/audit field exclusion.
- [x] Existing Story 3.5 server/conformance coverage is present in `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` and `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs` for append-only decision materialization, duplicate/stale replay handling, superseded history, tenant/project partitioning, and metadata-only safe denial.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - expanded populated S1 stream coverage for confirmed, rejected, deferred, needs-review, correction-delayed, correcting, and correction-completed system decision items.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added decision/correction metadata assertions for accessible names, focusability, evidence/confidence/status/actor/timestamp ordering, localized labels, decision actor/type, correction actor/type, policy/surface metadata, supersedes/superseded-by links, propagation progress, stale context, redaction/unavailable distinction, retention/schema/source version, and correlation IDs.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - extended forced-colors, reduced-motion, and phone-layout coverage to decision rows and focusable decision unavailable explanations.

## Coverage

- API endpoints: 1/1 Story 3.5 read endpoint covered (`GET /api/v1/projects/{projectId}/conversation`) by existing contract/server/conformance tests.
- UI states: 4/4 S1 E2E states covered: loading, populated stream, empty, unauthorized/redacted.
- Decision states: 4/4 association decision outcomes covered in E2E fixture: confirmed, rejected, deferred, needs-review.
- Correction states: 3/3 propagation states covered in E2E fixture: correction-delayed, correcting, corrected.
- Critical safety cases: metadata-only rendering, append-only decision item IDs, supersession visibility, redacted versus unavailable decision metadata, no raw decision notes, no raw correction rationale, no hidden evidence values, no raw provider payload, no audit payload, and no raw exception text.

## Validation

- [x] `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore --filter ProjectConversationE2ETests -m:1 /nr:false` - attempted; assembly built, then VSTest aborted before executing tests due to sandbox `SocketException (13): Permission denied`.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 7/7.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none` - passed 37/37.

## Checklist

- [x] API tests generated or already present where applicable.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases.
- [x] Tests use semantic roles, labels, accessible names, and stable metadata selectors.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created in the configured output path.
- [x] Tests saved to the appropriate test project.
- [x] Summary includes coverage metrics and validation commands.
