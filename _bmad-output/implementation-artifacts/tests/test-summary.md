# Test Automation Summary - Story 3.12

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-10
**Story:** `_bmad-output/implementation-artifacts/3-12-attachment-capture-and-governed-folder-storage.md`
**Framework:** xUnit v3 + Shouldly + Microsoft.Playwright.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Adapters/Folders/FoldersFolderStoreTests.cs` now covers Folders API failure mappings for 401, 403, 409, 413, 429, and 503 responses.
- [x] Added adapter coverage for unavailable, retryable, too-large, and unauthorized mailbox content results without calling Folders.
- [x] Added negative assertions that raw Folders exception text, provider payload markers, local paths, and fabricated folder/file references do not leak through storage failure results.

### E2E Tests

- [x] Existing `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` coverage was validated for stored attachment references, pending/retryable/unavailable/unsafe/redacted states, accessibility locators, metadata-only rendering, and inert folder/file references.
- [x] Existing UI service mapping coverage was validated for stored attachment folder/file references, duplicate/retry state, AI context eligibility, and allowed actions.

## Coverage

- API/adapter cases: 9/9 focused Folders adapter tests passed, covering success, idempotent replay shape, oversize inline degradation, upstream API failures, and non-available content degradation.
- Server workflow/projection: 73/73 focused coordinator and projection tests passed for governed storage success, duplicate suppression, replay/order tolerance, correction-state suppression, redaction, tenant/project scoping, and safe degradation.
- UI service: 6/6 focused mapping tests passed for Story 3.12 attachment metadata and governed references.
- UI E2E/component fixture: 24/24 focused Project Conversation E2E tests passed for S1 rendering, semantic locators, accessible unavailable reasons, metadata-only leakage checks, and no browser-side Folders/download controls.
- Acceptance criteria coverage: 6/6 Story 3.12 ACs covered by the focused adapter, coordinator, projection, UI service, and UI E2E lanes.

## Validation

- `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false --filter "FullyQualifiedName~FoldersFolderStoreTests"` - build completed, VSTest aborted on sandbox socket permission.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.Server.Tests.Adapters.Folders.FoldersFolderStoreTests` - passed 9/9.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.Server.Tests.Lifecycle.AttachmentCaptureCoordinatorTests -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests` - passed 73/73.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests` - passed 6/6.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 24/24.

## Checklist Validation

- [x] API tests generated/validated where applicable.
- [x] E2E tests validated because Story 3.12 includes S1 UI attachment rendering.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy path governed storage and stable folder/file references.
- [x] Tests cover critical error cases: unavailable content, unauthorized content, oversized inline content, Folders authorization failure, duplicate/replay conflict, throttling, and degraded dependency.
- [x] All generated/validated tests run successfully through compiled xUnit runners.
- [x] Tests use semantic/accessibility locators in browser paths.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent and fixture-driven.
- [x] Test summary created at the workflow output path.
- [x] Tests saved to existing test directories.
- [x] Summary includes coverage metrics.
