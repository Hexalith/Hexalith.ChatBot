# Test Automation Summary

**Story:** 3.4 - Attachment rendering in the conversation stream
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-01
**Framework:** xUnit v3, Shouldly, Microsoft.Playwright
**Run method:** `dotnet test` aborts in this sandbox at VSTest socket startup; validation used the compiled xUnit v3 executable.

## Generated Tests

### API Tests

- [x] Existing Story 3.4 contract/API coverage is present in `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs` and `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs` for additive attachment DTO fields, stable attachment wire tokens, generated-client availability, OpenAPI shape, and raw attachment/source-provider field exclusion.
- [x] Existing Story 3.4 server/conformance coverage is present in `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` and `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs` for materialization ordering, stale replay, tenant/project partitioning, and metadata-only safe denial.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - expanded populated S1 stream coverage from one pending attachment to six attachment display states: authorized/captured, pending-scan, unavailable, redacted, duplicate/retryable, and unsafe.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added `ProjectConversationAttachmentItemsShouldExposeStateMetadataAndReachableUnavailableReasons` for attachment accessible-name discovery, evidence/status/actor/timestamp ordering, ordered metadata labels, file/folder references when authorized, duplicate/retry state, AI eligibility, redaction distinction, focusable unavailable reasons, and unsafe filename suppression.
- [x] Existing loading, empty, unauthorized/redacted, forced-colors, reduced-motion, and phone-layout E2E paths remain covered.

## Coverage

- API endpoints: 1/1 Story 3.4 read endpoint covered (`GET /api/v1/projects/{projectId}/conversation`) by existing contract/server/conformance tests.
- UI states: 4/4 S1 E2E states covered: loading, populated stream, empty, unauthorized/redacted.
- Attachment states: 6/6 required Story 3.4 display states covered in E2E fixture: authorized/captured, pending-scan, unavailable, redacted, duplicate/retryable, unsafe.
- Attachment metadata: provider attachment id, display name/redacted/unavailable label, content type, size, capture/storage/scan statuses, duplicate state, retry state, AI eligibility, file/folder references when authorized, mailbox, conversation/thread, association id, lifecycle, redaction state, safe next action, and correlation id.
- Critical safety cases: metadata-only rendering, reachable unavailable reasons, no raw attachment content, no malware scan detail, no unauthorized file/folder names, no raw provider payload, no raw exception text, no hidden diagnostic text.

## Validation

- [x] `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - attempted; aborted before executing tests due to sandbox VSTest `SocketException (13): Permission denied`.
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
