# Test Automation Summary

## Generated Tests

### API Tests
- [x] Not applicable: Story 12.1 adds a build-blocking UI source-governance lane, not an HTTP/API surface.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` - Existing source-governance E2E analogue validates the implemented ChatBot UI Fluent-only guard against the real `.razor` and `.css` source tree.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` - Added QA detector fixtures for raw-control matching boundaries, legacy Fluent v4/FAST token matching, and ChatBot primitive CSS debt counting.

## Coverage
- Raw interactive controls: real-source scan covers the exact 12-file temporary backlog, stale backlog entries, missing backlog paths, and offenders outside the backlog.
- Theme primitive governance: real-source scan covers legacy Fluent tokens, primitive debt outside the backlog, stale CSS debt, and exact-count backlog drift.
- Critical detector cases: lowercase raw controls are flagged; Fluent/PascalCase components, `inputmode`, raw navigation links, Fluent 2 tokens, and layout-only CSS are not false positives.

## Validation
- [x] `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 -nodeReuse:false` - passed, 0 warnings, 0 errors.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -trait "Category=Governance" -noLogo` - passed, 6 total, 0 failed, 0 skipped.
- [x] `DiffEngine_Disabled=true ./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo` - passed, 167 total, 0 failed, 0 skipped.
- [x] `git diff --check` - passed.

## Notes
- No browser Playwright fixture is applicable because the story feature is a build-blocking source governance guard, not a runtime browser workflow.
- Previous Story 12.1 evidence recorded VSTest sandbox socket failures; this run used the repo's xUnit v3 executable fallback.
