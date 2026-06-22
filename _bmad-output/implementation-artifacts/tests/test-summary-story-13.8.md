# Test Summary - Story 13.8

Date: 2026-06-22
Baseline commit: e9141d8

## Scope

Story 13.8 closes Epic 13's "page-level composition" remediation by **deleting the now-dead hand-rolled
`.chatbot-*` page/shell layout CSS** from `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` and adding
a CSS-side ratchet that keeps it gone. Stories 13.2–13.7 (all `done`) drove every routable page off this chrome
and emptied the five `ChatBotLayoutCompositionConformanceTests` allowlists, so these rules render nothing — this
story removes the orphaned CSS. After 13.8 the stylesheet matches the `Hexalith.Tenants.UI` posture: only the
layout/accessibility CSS the design system does not own.

**CSS-only, rendering-layer correction.** No backend / command-spine / query / CLI / MCP / SignalR / Dapr /
EventStore change, no `.razor`/`.cs` production-source edit, no E2E hand-authored fixture edit, no localization
key, no allowlist edit, no sibling-submodule change. Governed read/decision semantics, the "no fake/freeform
textbox" safety model, NFR6 landmarks/labels (the skip-link is provided by the FrontComposer shell's
`fc-skip-link` → `#fc-main-content`), UX-DR4 non-color status cues, UX-DR34 focus management, and EN+FR i18n are
preserved exactly. The live `aspire run` real-render visual gate is **Story 13.9**.

`chatbot.tokens.css`: **660 → 599 lines** (61 lines of dead chrome removed).

### 10 retired selectors (re-confirmed 0 production-markup refs at dev time, baseline `e9141d8`)

A dev-time whole-token scan of `src/Hexalith.ChatBot.UI/**/*.razor` + `*.cs`
(`rg -P "(?<![a-z0-9-])<token>(?![a-z0-9-])"`) re-confirmed 0 references for each before deletion; the two
high-risk live neighbors still have refs (`.chatbot-section` = 21, `.chatbot-section-title` = 23) and are kept.

| Selector | Epic-AC named? | Removed from | Owner now |
| --- | --- | --- | --- |
| `.chatbot-page-header` | ✅ | grid-group selector list | `FcPageHeader` (13.2) |
| `.chatbot-page` (whole token) | ✅ | grid-group list + the `max-width` rule | `FcPageLayout` + `FluentStack`/`FluentCard` (13.3) |
| `.chatbot-command-bar` | ✅ | flex-group list + `@media (max-width: 599px)` group | `FcPageHeader` Actions slot / `FluentStack` (13.2) |
| `.chatbot-definition-list` (+ `dd`) | ✅ | grid-group list, standalone `grid-template-columns` rule, `dd` rule, `@media (max-width: 599px)` rule | `FluentDataGrid`/`FluentStack`/`FluentText` (13.4/13.5/13.6) |
| `.chatbot-skip-link` (+ `:focus`/`:focus-visible`) | ✅ | standalone rule, focus rule, both forced-colors focus-list entries | FrontComposer shell `fc-skip-link` → `#fc-main-content` |
| `.chatbot-page-title` | coupled | `min-width/overflow-wrap` group + `margin:0` group | `FcPageHeader` Heading (13.2) |
| `.chatbot-layout` | coupled | standalone `min-height: 100vh` rule | `<FrontComposerShell>` (`MainLayout.razor`) |
| `.chatbot-shell-header` | coupled | flex-group list | FrontComposerShell chrome |
| `.chatbot-shell-main` | coupled | standalone rule, focus-list entry, `@media (max-width: 599px)` rule, forced-colors focus-list entry | shell `#fc-main-content` |
| `.chatbot-dense-row` | coupled | grid-group list + forced-colors border list | orphaned dead layout class |

### Live siblings preserved when a dead token left a shared comma-list

- flex group keeps `.chatbot-status`, `.chatbot-actor-badge`, `.chatbot-chip`, `.chatbot-governed-action`, … (head is now `.chatbot-status,`).
- grid group keeps `.chatbot-section`, `.chatbot-status-group`, `.chatbot-conversation-shell`, … (head is now `.chatbot-section,`).
- `max-width` rule keeps `.chatbot-conversation-shell`.
- `min-width/overflow-wrap` + `margin:0` groups keep `.chatbot-section-title`, `.chatbot-body`, ….
- focus-outline list keeps `.chatbot-conversation-shell__main:focus-within` (a live class, NOT `.chatbot-shell-main`).
- `@media (max-width: 599px)` keeps `.chatbot-governed-action`, `.chatbot-streaming-stop`, `.chatbot-status`.
- `@media (forced-colors: active)` border list keeps `.chatbot-governed-action__reason`, `.chatbot-association-candidate`, … and the focus list keeps `.chatbot-governed-action__reason:focus-visible`, `.chatbot-association-candidate:focus`, ….

## Scope fences honored (NOT this story)

- **Stale E2E hand-authored HTML fixtures** (`tests/Hexalith.ChatBot.UI.E2E.Tests/*E2ETests.cs` embed pre-13.2
  `chatbot-page`/`chatbot-skip-link`/`chatbot-shell-main`/`chatbot-definition-list` strings and
  `querySelector(".chatbot-shell-main")`) → **Story 13.9** real-render replacement. They never load
  `chatbot.tokens.css`, so the CSS deletion does not break them; left untouched (0 edits).
- The five `ChatBotLayoutCompositionConformanceTests` razor-side allowlists + `NotYetComposedPageBacklog`
  (already `[]`; 13.2–13.6 own them) → verify-only, left exactly as-is.
- `.chatbot-section`/`.chatbot-section-title` accordion bodies/headings (13.7) → **kept** (live refs).

## Commands

| Command | Result |
| --- | --- |
| `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` | Passed: **0 warnings, 0 errors** (Release, `TreatWarningsAsErrors`). |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --filter "Category=Governance" --no-build -c Release -m:1 -nodeReuse:false` | Passed: **60** total, 0 failed, 0 skipped (incl. the new `Story13LayoutCssRetirementTests`, `ChatBotLayoutCompositionConformanceTests`, `ChatBotFluentConformanceTests`). |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --filter "FullyQualifiedName~Story13LayoutCssRetirementTests" --no-build …` | Passed: **12** total (1 `[Fact]` + 11 detector `[Theory]` cases), 0 failed, 0 skipped. |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --no-build -c Release -m:1 -nodeReuse:false` (full project, regression) | Passed: **227** total, 0 failed, 0 skipped (incl. `ChatBotSemanticTokenContractTests` — the only test reading the CSS — `StylesheetShouldContainForcedColorsAndNonColorStatusCues`, `StylesheetShouldMapSemanticColorsOnlyToFluentOrFrontComposerVariables`, `StylesheetShouldNotDeclareChatBotPrimitiveAliasesOrPaletteValues`, `MainLayoutShouldUseFrontComposerShellAsTheSingleShellBoundary`). |
| `DiffEngine_Disabled=true dotnet test …UI.E2E.Tests… --no-build -c Release -m:1 -nodeReuse:false` (real browser) | Passed: **136** total, 0 failed, **0 skipped** in **22 s** — Chromium present (Google Chrome 148.0.7778.215), so the no-browser string fallback did **not** trigger (memory `chatbot-e2e-nobrowser-fallback-trap`); the real-browser path executed. |
| `rg -P "(?<![a-z0-9-])<token>(?![a-z0-9-])" src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` (each of the 10) | **0** occurrences for every retired selector; live anchors (`--chatbot-color-neutral-background`, `.chatbot-status__label`, `@media (forced-colors: active)`, `CanvasText`, `Highlight`, `border-inline-start`) all present. |
| `git diff --check` | Passed: clean. |
| `git diff --name-only` (this story's changes) | `chatbot.tokens.css`, `Story13LayoutCssRetirementTests.cs`, story doc, this evidence doc, `sprint-status.yaml` — **no** `.razor`, **no** production `.cs`, **no** `*E2ETests.cs` fixture, **no** localization, **no** submodule. |

## Conformance guard (AC4)

`Story13LayoutCssRetirementTests.StylesheetShouldNotContainRetiredLayoutChromeSelectors`
(`[Trait("Category", "Governance")]`) reads `chatbot.tokens.css` and asserts: (a) **non-vacuous** — the file is
found, is non-trivial, and still carries its live anchors (`--chatbot-color-neutral-background`,
`.chatbot-status__label`, `@media (forced-colors: active)`, `.chatbot-section,`, `.chatbot-section-title,`,
`CanvasText`, `Highlight`) before checking absence; (b) each of the 10 retired selectors is **absent** using a
selector word-boundary matcher `\.<token>(?![a-z0-9-])`, so a live prefix neighbor is never a false pass/fail
(`.chatbot-page` does not match `.chatbot-section`/`.chatbot-page-header`; `.chatbot-page-title` does not match
the live `.chatbot-section-title`). 11 detector-fixture `[Theory]` pins prove the boundary logic both ways
(dead token matched; live neighbor not matched). The razor-side `ChatBotLayoutCompositionConformanceTests`
allowlists (all `[]`) are verified-only, not edited.

## Notes

Story 13.9 owns the live `aspire run` real-render cross-surface re-verification gate (and the stale E2E-fixture
replacement); this story's truth signal is the build-blocking CSS source-scan guard above plus the unchanged
Governance/semantic-contract lanes and the real-browser E2E suite.
