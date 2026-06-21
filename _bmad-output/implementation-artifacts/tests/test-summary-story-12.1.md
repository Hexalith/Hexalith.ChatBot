# Test Summary - Story 12.1

Date: 2026-06-21
Baseline commit: 245075e

## Scope

Story 12.1 adds build-blocking governance for `Hexalith.ChatBot.UI` Fluent v5 conformance. The guard source-scans ChatBot `.razor` files for raw interactive controls, ratchets the exact 12-file raw-control migration backlog, and source-scans `.css` plus `.razor` content for legacy Fluent v4/FAST tokens and ChatBot-owned primitive CSS redefinitions.

No component migration, CSS retirement, package upgrade, backend behavior, CLI, MCP, SignalR, or sibling submodule edits were performed.

## Generated Tests

- [x] Added QA detector fixture coverage in `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` for raw-control matching boundaries.
- [x] Added QA detector fixture coverage for legacy Fluent v4/FAST token matching without false positives on Fluent 2 tokens.
- [x] Added QA detector fixture coverage for ChatBot primitive CSS debt counting and layout-only CSS false-positive avoidance.

## Commands

| Command | Result |
| --- | --- |
| `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` | Passed: build succeeded, 0 warnings, 0 errors. |
| `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 -nodeReuse:false` | Passed: build succeeded, 0 warnings, 0 errors. |
| `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false` | Failed before test execution in sandbox: VSTest socket permission denied. |
| `DiffEngine_Disabled=true ./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -trait "Category=Governance" -noLogo` | Passed: 6 total, 0 failed, 0 skipped. |
| `DiffEngine_Disabled=true ./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo` | Passed: 167 total, 0 failed, 0 skipped. |
| `git diff --check` | Passed. |
| `DiffEngine_Disabled=true dotnet test Hexalith.ChatBot.slnx --no-build --no-restore -m:1 -nodeReuse:false` | Failed before test execution in sandbox: VSTest socket permission denied for every test project. |
| Configured solution test executables via xUnit v3 in-process runners | 2814 total, 2810 passed, 1 failed, 3 skipped. The single failure is listed below and is unrelated to Story 12.1 UI-source governance. |

## Regression Note

`Hexalith.ChatBot.Conformance.Tests.CrossTenantReadSurfaceIsolationTests.ProjectConversationForeignRecordShouldExistForItsOwnerYetBeDeniedToTheBoundCaller` fails independently with `-parallel none`:

`Cross-tenant leakage: persona 'owner' leaked a 'tenant'-class sentinel ('tenant-beta') through the 'project-conversation-owner-200' channel.`

This story changed only UI governance tests and BMAD tracking/evidence files, so the failure is recorded as an existing non-story conformance issue rather than a regression caused by Story 12.1.

## Changed Files

- `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.1.md`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
