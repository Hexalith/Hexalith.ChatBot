# Test Summary - Story 13.2

Date: 2026-06-22
Baseline commit: 21be905
Verified at HEAD: d344a98

## Scope

Story 13.2 is the first Epic 13 migration story: it adopts FrontComposer `FcPageLayout` + `FcPageHeader`
across all 6 routable `Hexalith.ChatBot.UI` pages (plus the shared conversation workspace), folds the
page-level command bars into the `FcPageHeader` `Actions` slot, converts the inner toolbars to Fluent
layout primitives (`FluentStack`), and empties the three Story 13.1 guard lists this story owns
(`PageHeaderChromeAllowlist`, `CommandBarAllowlist`, `NotYetComposedPageBacklog`). This fixes the
page-title band overlapping the FrontComposer shell top bar on every route.

**Rendering-layer re-composition only.** No `.chatbot-page`/`.chatbot-section` content-box removal
(Story 13.3), no `.chatbot-*` CSS deletion (Story 13.8), no static-E2E-fixture rewrite (Story 13.9),
and no backend / CommandGateway / CLI / MCP / SignalR / Dapr / EventStore change and **no edit inside
any sibling submodule**.

## Implementation-provenance note (transparency)

This story's source changes were already present in the committed tree when this dev-story session ran:
the page migrations and the guard-list emptying landed (out of story order) bundled into commit
`b310462` (`feat(story-13.4): …`) and the guard file was finalized in `d344a98` (`feat(story-13.1): …`),
both ancestors of the current `HEAD`. The repo is several Epic-13 stories ahead of this story's
`backlog→ready-for-dev` baseline (`21be905`), so 13.4/13.6/13.7 surfaces are also already migrated in the
same files. This session **verified** that every Story 13.2 task/AC is genuinely satisfied by the
committed implementation (structural read of all 8 files + the guard) and **proved** it green with a
full build + the Governance lane, then finalized the story docs. No new source edits were required.

## Per-AC verification

| AC | Requirement | Evidence | Result |
| --- | --- | --- | --- |
| 1 | All 6 `@page` routes compose `FcPageLayout` + `FcPageHeader`; backlog empty | `Route_pages_compose_frontcomposer_layout_and_header_except_not_yet_composed_backlog` PASS; `NotYetComposedPageBacklog = []`; `ProjectConversation` composes via the delegation-aware `DelegatesToComposedWorkspace` helper (renders `<ChatBotProjectConversationWorkspace>`, which owns the header). `@using Hexalith.FrontComposer.Contracts.Rendering` present in `Components/_Imports.razor`; `Mode="FcPageLayoutMode.FullWidth"` used. | ✅ |
| 2 | Every `<header class="chatbot-page-header">` replaced by `<FcPageHeader>`; allowlist empty | `Pages_do_not_hand_roll_page_header_chrome_except_shrinking_allowlist` PASS; `PageHeaderChromeAllowlist = []`; project-wide `chatbot-page-header` token count = **0**. Eyebrow→`Eyebrow`, h1→`Heading`+`HeadingId`, body→`Description` mapping applied per page; route heading `id`s preserved (`aria-labelledby` still resolves). | ✅ |
| 3 | Every `chatbot-command-bar` token removed from the 4 files; allowlist empty | `Pages_do_not_hand_roll_command_bar_except_shrinking_allowlist` PASS; `CommandBarAllowlist = []`; project-wide `chatbot-command-bar` token count = **0**. Page-level bars → `FcPageHeader` `Actions`; inner toolbars → `FluentStack Orientation="Horizontal" Wrap="true"` (queue-family group keeps `role="group"`/`aria-label`); `ChatBotAssociationReviewActions` keeps only `chatbot-association-actions__bar`. | ✅ |
| 4 | Shell-overlap bug fixed on every route | Structural root-cause confirmed by source: `FcPageHeader` renders `<header role="presentation">` (hard-coded `LandmarkRole = "presentation"` in `FcPageHeader.razor.cs`) — purely-visual chrome inside the shell's single content landmark, so it no longer competes with `FrontComposerShell`'s 48px top bar; `MainLayout` wraps `@Body` in `<FrontComposerShell>`; `HeadingId` preserved. Matches the `Hexalith.Tenants.UI` reference (no overlap). Binding automated gate for this story = guard + Release build (both green). **Full live real-render screenshot pass is Story 13.9** per AC4; not executed this session (see Caveats). | ✅ (structural) |
| 5 | Scope boundary preserved | `PageContentBoxAllowlist` unchanged (**6** files); `DefinitionListAllowlist` untouched by 13.2; no `.chatbot-*` CSS deleted; static E2E fixtures not rewritten; no backend / submodule edits. (Note: `DefinitionListAllowlist` already reads 1 entry in the ahead-of-story repo because Stories 13.4/13.6 ran earlier — 13.2 did not modify it.) | ✅ |
| 6 | Guard green + build clean | `ChatBotLayoutCompositionConformanceTests` + `ChatBotFluentConformanceTests` all green; `dotnet build Hexalith.ChatBot.slnx` 0W/0E (`TreatWarningsAsErrors`); `git diff --check` clean; a11y landmarks + heading targets + non-color cues + EN/FR localization preserved (no localization-key changes). | ✅ |

## Commands

| Command | Result |
| --- | --- |
| `dotnet restore Hexalith.ChatBot.slnx` | Passed: all projects up-to-date. |
| `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` | Passed: build succeeded, **0 warnings, 0 errors** (TreatWarningsAsErrors). |
| `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false` | Passed: **48 total, 0 failed, 0 skipped**. |
| └ `ChatBotLayoutCompositionConformanceTests` (the 3 owned lists now empty + detector-fixture pins) | Passed (incl. `Route_pages_compose_…backlog`, `Pages_do_not_hand_roll_page_header_chrome…`, `Pages_do_not_hand_roll_command_bar…`). |
| └ `ChatBotFluentConformanceTests` (Story 12.1 regression — no raw controls / legacy v4/FAST tokens reintroduced) | Passed (6 tests). |
| `grep -rho "chatbot-page-header" src/Hexalith.ChatBot.UI --include=*.razor \| wc -l` | **0** |
| `grep -rho "chatbot-command-bar" src/Hexalith.ChatBot.UI --include=*.razor \| wc -l` | **0** |
| `git diff --check` (working tree) and `git diff --check 21be905 HEAD -- <8 UI files + guard>` | Passed: clean. |

The Governance lane total of 48 is the 6 Story 12.1 Fluent-conformance tests + the Story 13.1 layout-composition
guard (3 owned bans now empty, plus content-box/definition-list bans and the detector-fixture theory pins) +
the Story 13.4/13.6 `Story13DefinitionListMigrationTests` that also carry `Category=Governance`.

## Overlap-fix mechanism (AC4 detail)

The hand-rolled `<header class="chatbot-page-header">` band rendered the route title inside the shell
`@Body` in a way that collided with `FrontComposerShell`'s own top bar. `FcPageHeader` instead emits
`<header role="presentation">` (stripping the implicit `banner` role so the shell header stays the sole
banner) and carries no positioning, so the title band renders as ordinary content inside the shell's
single `#fc-main-content` `main` landmark — the exact non-overlapping behavior of `Hexalith.Tenants.UI`.
The `.chatbot-page-header` CSS class itself only set `display:grid; gap` (no positioning), so its removal
is safe; the fix is structural (compose through the primitive), not a CSS tweak.

## Caveats

- **No live `aspire run` visual pass this session.** AC4's binding automated gate is the guard + Release
  build (both green), and AC4 explicitly defers the full real-render screenshot verification to Story 13.9.
  A live Aspire/DAPR topology run is documented as flaky in this sandbox (DCP startup + a pre-existing
  403 `TenantMissing`), so the overlap fix was verified structurally (FcPageHeader `role="presentation"`
  inside the shell content landmark + Tenants-reference parity) rather than by screenshot. If a reviewer
  wants live confirmation before 13.9, run `aspire run` and load all 6 routes.

## Changed Files (this dev-story session — docs/tracking only)

- `_bmad-output/implementation-artifacts/13-2-adopt-fcpagelayout-fcpageheader-across-pages.md` (task checkboxes, Dev Agent Record, File List, Change Log, Status → review)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (`13-2-…` in-progress → review; `last_updated`)
- `_bmad-output/implementation-artifacts/tests/test-summary-story-13.2.md` (this file)

## Source files implementing Story 13.2 (already committed at/under HEAD)

- `src/Hexalith.ChatBot.UI/Components/_Imports.razor` (added `@using Hexalith.FrontComposer.Contracts.Rendering`)
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (3 owned lists emptied; `DelegatesToComposedWorkspace` delegation-aware require-compose helper)
</content>
</invoke>
