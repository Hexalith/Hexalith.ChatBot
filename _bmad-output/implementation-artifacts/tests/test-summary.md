# Test Automation Summary

**Story:** 3.6 - Approval event rendering
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-01
**Framework:** xUnit v3, Shouldly, Microsoft.Playwright
**Run method:** compiled xUnit v3 executable, matching the story's sandbox fallback guidance.

## Generated Tests

### API Tests

- [x] Existing Story 3.6 contract/API coverage is present in `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`, and `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs` for additive approval DTO fields, stable approval wire tokens, generated-client availability, OpenAPI shape, and raw prompt/output/payload/rationale/policy/audit field exclusion.
- [x] Existing Story 3.6 server/conformance coverage is present in `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` and related conformance tests for metadata-only approval projection, append-only IDs, duplicate/stale replay handling, out-of-order approval events, supersession links, and tenant/project partitioning.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - expanded populated S1 stream approval coverage for requested, approved, rejected, request-revision, cancelled, projection-pending, executed, failed, expired-evidence, unavailable policy, and unavailable audit states.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added approval metadata assertions for actor-leading accessible names, focusability, evidence/risk/status/actor/timestamp order, decision actor/type, authority result, disabled reason, rationale redaction state, audit operation/status, supersedes/superseded-by links, safe next action, and no false `Done` claim while projection/audit is pending.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - extended forced-colors, reduced-motion, phone-layout, and focusable unavailable-explanation assertions to approval rows.

## Coverage

- API endpoints: 1/1 Story 3.6 read endpoint covered (`GET /api/v1/projects/{projectId}/conversation`) by existing contract/server/conformance tests.
- UI states: 4/4 S1 E2E states covered: loading, populated stream, empty, unauthorized/redacted.
- Approval request states: 1/1 requested/pending case covered, including expired evidence and policy-unavailable explanation.
- Approval decision states: 4/4 decision outcomes covered in E2E fixture: approve, reject, request-revision, cancel.
- Approval outcome states: 3/3 governed result states covered in E2E fixture: accepted/projection-pending, executed, failed.
- Critical safety cases: metadata-only rendering, append-only approval item IDs, status text not color-only, reachable policy/audit unavailable explanations, no raw prompt/output/command payload/rationale/policy body/audit envelope, and no `Done` claim for projection-pending approval outcomes.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 7/7.

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
