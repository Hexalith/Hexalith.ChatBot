# Test Summary - Story 13.3

Date: 2026-06-22
Baseline commit: bfed90a
Verified at HEAD: bfed90a (working tree)

## Scope

Story 13.3 replaces the hand-rolled `<section class="chatbot-page …">` content-box wrapper in all 6
`Hexalith.ChatBot.UI` surfaces that carried the `chatbot-page` whole-class token with the
`Hexalith.Tenants.UI` reference pattern — a Fluent vertical layout container
(`<FluentStack Orientation="Orientation.Vertical">`) directly inside `FcPageLayout`. This removes the
bordered/boxed page-content chrome that fought the Fluent → FrontComposer visual chain, and empties the
Story 13.1 guard list this story owns (`PageContentBoxAllowlist` 6 → **0**).

**Content-wrapper re-composition only.** No sibling `.chatbot-section` → `FluentAccordion` regrouping
(Story 13.7), no `/operational-dashboards` data-viz/KPI tiles (Story 13.5), no `<dl>` migration
(Story 13.4, done), no `.chatbot-*` CSS deletion (Story 13.8), no static-E2E-fixture rewrite
(Story 13.9), and no backend / CommandGateway / CLI / MCP / SignalR / Dapr / EventStore change and
**no edit inside any sibling submodule**.

## Implementation-provenance note (transparency)

Unlike Stories 13.2/13.4/13.6 (whose source landed early in the ahead-of-story tree), **Story 13.3's
`chatbot-page` unwrap was genuinely un-done at baseline** — the guard's `PageContentBoxAllowlist` still
held all 6 live entries and a source scan confirmed the outer `<section class="chatbot-page …">` wrapper
survived in every file. This dev-story session made the actual source edits: 6 `.razor` wrapper swaps
plus emptying the owned guard list, then proved it green with a full Debug + Release build and the
Governance lane.

## The transform applied (mechanical, per file)

Each file: `<FcPageLayout> → <section class="chatbot-page [secondary]" aria-labelledby="…" data-chatbot-responsive-fixture="…"> → <FcPageHeader> + content → </section> → </FcPageLayout>`
became `… → <FluentStack Orientation="Orientation.Vertical" [Class="[secondary]"] aria-labelledby="…" data-chatbot-responsive-fixture="…"> → … → </FluentStack> → …`.
`FluentStack` captures unmatched attributes, so `aria-labelledby` and `data-chatbot-responsive-fixture`
splat through (Tenants `TenantAuditPage` parity). No `VerticalGap`/extra `Class` added beyond the carried
secondary class (default gap; minimal diff). `FcPageLayout`/`FcPageHeader`/inner accordion/banners/sections
left exactly as Story 13.2/13.4/13.6/13.7 left them.

| File | secondary class → `Class=` |
| --- | --- |
| `Components/Pages/GovernedOperations.razor` | (none — bare `FluentStack`) |
| `Components/Pages/OperationalDashboards.razor` | (none — bare `FluentStack`) |
| `Components/Pages/AssociationReview.razor` | `chatbot-association-review` |
| `Components/Pages/ComplianceAuditInvestigation.razor` | `compliance-audit-investigation` |
| `Components/Pages/ProjectWorkspace.razor` | `chatbot-project-workspace` |
| `Components/Governed/ChatBotProjectConversationWorkspace.razor` | `chatbot-project-conversation` |

## Per-AC verification

| AC | Requirement | Evidence | Result |
| --- | --- | --- | --- |
| 1 | All 6 `chatbot-page` content-box wrappers replaced by a Fluent layout container; `PageContentBoxAllowlist` empty | `Pages_do_not_hand_roll_content_box_wrapper_except_shrinking_allowlist` PASS; `PageContentBoxAllowlist = []`; project-wide `chatbot-page` whole-token count = **0** (whole-token regex, prefix `chatbot-page-header`/`-title` excluded). Each outer `<section>` → `<FluentStack Orientation="Orientation.Vertical">` with matching `</FluentStack>`. | ✅ |
| 2 | Heading association, responsive-fixture hooks, secondary classes preserved; no new localization keys | Every `aria-labelledby` value unchanged (`governed-operations-title`, `operational-dashboards-title`, `association-review-title`, `compliance-audit-title`, `project-workspace-title`, `@HeadingId`); every `data-chatbot-responsive-fixture` preserved verbatim (incl. `audit-investigation-s9`, `@ResponsiveFixture`); 4 secondary classes moved to `Class=`, 2 files bare; no `ChatBotUiTextKey`/localization key added/removed/renamed (diff is structural only). | ✅ |
| 3 | Boxed content chrome gone; page composes as a clean full-width Fluent surface | Content no longer wrapped in the `chatbot-page` box (`display:grid; gap; max-width` from `chatbot.tokens.css`); it now flows `FcPageLayout` (FullWidth) → `FluentStack` (Vertical). Binding automated gate (guard + Debug & Release build) green. **Full real-render screenshot pass is Story 13.9** per AC3; not executed this session (see Caveats). | ✅ (structural) |
| 4 | Scope boundary preserved — sibling sections, dashboards, `<dl>`, CSS, fixtures untouched | No `.chatbot-section`/`FluentAccordion` change (e.g. `GovernedOperations`/`AssociationReview`/`ProjectWorkspace`/`ChatBotProjectConversationWorkspace` accordions left as-is); `DefinitionListAllowlist` unchanged = `["Components/Pages/OperationalDashboards.razor"]`; `PageHeaderChromeAllowlist`/`CommandBarAllowlist`/`NotYetComposedPageBacklog` stay `[]`; no `.chatbot-*` CSS deleted; static E2E fixtures not rewritten; no backend/submodule edits (`git status` shows only 6 `.razor` + the guard). | ✅ |
| 5 | Guard green + build clean | `ChatBotLayoutCompositionConformanceTests` + `ChatBotFluentConformanceTests` all green; `dotnet build Hexalith.ChatBot.slnx` 0W/0E in Debug **and** Release (`TreatWarningsAsErrors`); `git diff --check` clean; a11y landmarks (shell `<main>` + `FcPageHeader` h1, `aria-labelledby` preserved), non-color status cues, EN/FR localization preserved (no key changes). | ✅ |

## Commands

| Command | Result |
| --- | --- |
| `dotnet restore Hexalith.ChatBot.slnx` | Passed: all projects up-to-date. |
| `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` | Passed: **0 warnings, 0 errors** (TreatWarningsAsErrors). |
| `dotnet build Hexalith.ChatBot.slnx -c Release --no-restore -m:1 -nodeReuse:false` | Passed: **0 warnings, 0 errors**. |
| `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false` | Passed: **48 total, 0 failed, 0 skipped**. |
| └ `ChatBotLayoutCompositionConformanceTests` (owned `PageContentBoxAllowlist` now empty + detector-fixture pins) | Passed (incl. `Pages_do_not_hand_roll_content_box_wrapper_except_shrinking_allowlist`, `Page_content_box_matcher_flags_whole_token_only`). |
| └ `ChatBotFluentConformanceTests` (Story 12.1 regression — no raw controls / legacy v4/FAST tokens) | Passed. |
| `grep -roP "(?<=[\"'\s])chatbot-page(?=[\"'\s])" --include=*.razor src/Hexalith.ChatBot.UI/ \| wc -l` | **0** |
| `git diff --check` (working tree) | Passed: clean. |

The Governance lane total of 48 is the Story 12.1 Fluent-conformance tests + the Story 13.1
layout-composition guard (its owned `PageContentBoxAllowlist` now empty, plus the other bans and the
detector-fixture theory pins) + the Story 13.4/13.6 definition-list migration tests that also carry
`Category=Governance`.

## Accessibility note (so review does not churn)

Replacing the named `<section class="chatbot-page" aria-labelledby="…">` (an implicit `region` landmark)
with `FluentStack` (a `<div>`) intentionally drops the nested `region` role — matching `Hexalith.Tenants.UI`,
where the single content landmark is the FrontComposer shell `<main>` (`#fc-main-content`) and `FcPageHeader`
owns the route `h1`. `aria-labelledby` is kept on the `FluentStack`, preserving the programmatic name
association exactly as `TenantAuditPage` does. This is not an a11y regression.

## Caveats

- **No live `aspire run` visual pass this session.** AC3's binding automated gate is the guard + Release
  build (both green), and AC3 explicitly defers the full real-render screenshot verification to Story 13.9.
  A live Aspire/DAPR topology run is documented as flaky in this sandbox (DCP startup + a pre-existing
  403 `TenantMissing`), so the boxed-chrome removal was verified structurally (token sweep = 0,
  `FluentStack` inside `FcPageLayout` FullWidth, Tenants-reference parity) rather than by screenshot —
  the same approach Story 13.2 took. If a reviewer wants live confirmation before 13.9, run `aspire run`
  and load the 6 routes (`/governed-operations`, `/operational-dashboards`,
  `/association-review/{id}`, `/compliance-audit-investigation`, `/`, `/projects/{id}/conversation`).

## Changed Files (this dev-story session)

Source (the actual Story 13.3 implementation):

- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (`PageContentBoxAllowlist` emptied)

Docs/tracking:

- `_bmad-output/implementation-artifacts/13-3-replace-content-boxes-with-fluent-composition.md` (task checkboxes, Dev Agent Record, File List, Change Log, Status → review)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (`13-3-…` in-progress → review; `last_updated`)
- `_bmad-output/implementation-artifacts/tests/test-summary-story-13.3.md` (this file)
