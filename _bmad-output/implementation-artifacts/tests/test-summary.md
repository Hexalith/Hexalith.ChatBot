# Test Automation Summary

**Story:** 3.12 - Attachment capture and governed-folder storage
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-01
**Framework:** xUnit v3, Shouldly, Microsoft.Playwright
**Run method:** `dotnet test` build followed by compiled xUnit v3 executables because VSTest socket creation is blocked in this sandbox.

## Generated Tests

### API Tests

- [x] No new public API endpoint or query-contract fields were introduced by this QA pass. Existing story 3.12 server/projection tests remain the API-adjacent coverage for storage outcomes and S1 query projection behavior.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added stored-attachment coverage proving governed `File reference` and `Folder reference` render as metadata only.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added inert-surface assertions: no links, buttons, download affordances, browser-side Folders API URLs, or folder/file query parameters.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added degraded/retryable/unsafe storage assertions proving failed or unavailable states do not expose folder/file references.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs` - added UI service mapping coverage for stored attachment status, folder/file IDs, duplicate state, retry state, AI eligibility, and allowed-action metadata.

## Coverage

- API endpoints: no new endpoint generated; existing S1 project conversation read contract remains the exposed surface.
- UI features: stored attachment references, pending/retryable/unavailable/unsafe storage states, metadata-only rendering, accessible article selection, and no interactive Folders/file action surface are covered.
- Critical safety cases: raw attachment content, base64/bytes, provider payload/source context, Graph delta tokens, local attachment paths, raw exceptions, unauthorized folder/file names, and malware scan details remain excluded from UI/test fixture output.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` - compiled, then VSTest aborted with sandbox `SocketException (13): Permission denied`.
- [x] `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - compiled, then VSTest aborted with sandbox `SocketException (13): Permission denied`.
- [x] `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests` - passed 3/3.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 14/14.

## Checklist

- [x] API tests generated if applicable.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical degraded/retryable/unavailable cases.
- [x] Tests use semantic roles, accessible names, and stable metadata selectors.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created in the configured output path.
- [x] Tests saved to the appropriate test projects.
- [x] Summary includes coverage metrics and validation commands.
