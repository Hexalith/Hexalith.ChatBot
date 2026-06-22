# Test Summary - Story 13.1

Date: 2026-06-22
Baseline commit: 21be905

## Scope

Story 13.1 adds build-blocking governance for the `Hexalith.ChatBot.UI` FrontComposer layout-composition rule (Epic 13, guard-first step). A new sibling guard `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` source-scans ChatBot `.razor` files and:

- bans hand-rolled page chrome (`chatbot-page-header`, the whole-token `chatbot-page` content box, and `chatbot-command-bar` in both `class=`/`Class=`) via per-pattern shrink-only allowlists;
- bans `<dl class="chatbot-definition-list">` primary-data dumps via a shrink-only allowlist (bare semantic `<dl>` is NOT banned);
- requires every routable `@page` to compose through FrontComposer `FcPageLayout` + `FcPageHeader`, with a shrink-only not-yet-composed backlog;
- pins the detector regex logic with crafted-markup fixtures (AC5).

Each allowlist/backlog carries the same three ratchets as Story 12.1: a missing-path assertion, an offender-outside-allowlist assertion, and a stale-entry assertion, so every list can only shrink toward empty (Stories 13.2–13.8 burn them down).

**Governance-only.** No page migration, no `FcPageLayout`/`FcPageHeader` adoption, no `.chatbot-*` CSS deletion, no `<dl>`→`FluentDataGrid` migration, no package upgrade, and no backend/CommandGateway/CLI/MCP/SignalR or sibling-submodule edits were performed.

## Seeded allowlist/backlog counts (authoritative 2026-06-22 source scan)

| Banned pattern / rule | Seeded files | Variance vs planning prose |
| --- | --- | --- |
| `chatbot-page-header` (page-title band) | 6 | matches prose ("6") |
| `chatbot-page` content box (whole token) | 6 | prose said "2"; 4 more use it in a multi-class list (`class="chatbot-page chatbot-project-workspace"` etc.), so the guard matches the whole class token |
| `chatbot-command-bar` (`class=`/`Class=`) | 4 | prose said "3"; `ChatBotAssociationReviewActions.razor` also applies it via Blazor `Class=` on a `FluentStack` |
| `chatbot-definition-list` (`<dl>` dumps) | 25 | matches prose ("25") |
| `@page` not-yet-composed backlog | 6 | the 6 routable routes; none compose yet (`FcPageLayout`=0, `FcPageHeader`=0 project-wide) |

The two upward variances (chatbot-page 2→6, command-bar 3→4) are reconciliations of under-counted multi-class hits from the authoritative source scan, **not** scope creep. The Epic-13 end state is unchanged: every allowlist reaches empty by Story 13.8.

## Commands

| Command | Result |
| --- | --- |
| `dotnet restore tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj` | Passed: all projects up-to-date. |
| `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 -nodeReuse:false` | Passed: build succeeded, 0 warnings, 0 errors (TreatWarningsAsErrors). |
| `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore --no-build -m:1 -nodeReuse:false` | Passed: 26 total, 0 failed, 0 skipped. |
| `... --filter "FullyQualifiedName~ChatBotLayoutCompositionConformanceTests"` (new 13.1 guard) | Passed: 20 total, 0 failed, 0 skipped. |
| `... --filter "FullyQualifiedName~ChatBotFluentConformanceTests"` (existing 12.1 guard regression) | Passed: 6 total, 0 failed, 0 skipped. |
| `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` | Passed: build succeeded, 0 warnings, 0 errors (TreatWarningsAsErrors). |
| `git diff --check` | Passed: clean. |

Note: unlike the Story 12.1 run, the VSTest socket was available in this sandbox, so `dotnet test --filter "Category=Governance"` executed directly (no compiled-host fallback was required). The full Governance lane total of 26 is the 6 existing Story 12.1 tests plus the 20 new Story 13.1 tests.

## Non-vacuity / mutation evidence

- The new guard asserts `Directory.Exists(uiRoot)` and `razorFiles.ShouldNotBeEmpty()` before evaluating offenders.
- The passing stale-entry ratchets demonstrate the scan genuinely matched every seeded offender (a vacuous scan would have reported all allowlist entries as stale and failed).
- Mutation check: temporarily removing `Components/Pages/ProjectConversation.razor` from the not-yet-composed backlog made `Route_pages_compose_frontcomposer_layout_and_header_except_not_yet_composed_backlog` **fail** with the expected message (`Routes that neither compose nor are backlogged: Components/Pages/ProjectConversation.razor`), confirming the require-compose ratchet bites. The mutation was reverted and the lane returned to 26/26 green.

## Out-of-scope observations (recorded, not changed)

- `ProjectConversation.razor` declares `<PageTitle>` directly and delegates its chrome to `<ChatBotProjectConversationWorkspace>`. The Tenants guard bans direct `<PageTitle>`/`<h1>` on route pages, but that ban is **not** in this story's AC and was deliberately not added (it would pre-empt Story 13.2's migration decision). Noted only.

## Changed Files

- `_bmad-output/implementation-artifacts/13-1-frontcomposer-layout-composition-guard.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-13.1.md`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs`
