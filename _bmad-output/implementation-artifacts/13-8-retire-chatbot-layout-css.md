---
baseline_commit: e9141d813b90b39fdd62083f5437492813eadf9f
---

# Story 13.8: Retire remaining .chatbot-* layout CSS

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a ChatBot frontend engineer,
I want the now-dead hand-rolled `.chatbot-*` page/shell layout CSS deleted from `chatbot.tokens.css` (the page-header band, the bordered page/content box, the command bar, the monospace definition-list, the custom skip-link, and the Epic-10 shell wrappers the FrontComposer shell now owns),
so that the stylesheet contains only the layout/accessibility CSS the design system does not own — closing Epic 13's "page-level composition" remediation — while every still-referenced `.chatbot-*` class, governed semantic, accessibility hook, non-color status cue, and EN+FR string is preserved exactly.

## Context

Epic 13 closes the page-level FrontComposer/Fluent composition gap that Epics 10 (shell) and 12 (leaf controls) left open. Stories **13.2–13.7 (all `done`)** migrated every routable page off the hand-rolled chrome:

- 13.2 → `FcPageLayout` + `FcPageHeader` (emptied `PageHeaderChromeAllowlist` + `CommandBarAllowlist`).
- 13.3 → replaced the `.chatbot-page`/`.chatbot-section` bordered boxes with `FluentStack`/`FluentCard` (emptied `PageContentBoxAllowlist`; the `.chatbot-section` **class survives** as the accordion-item landmark body, but its border rule is gone).
- 13.4 + 13.5 + 13.6 → migrated the `<dl class="chatbot-definition-list">` data dumps to `FluentDataGrid`/`FluentStack`/`FluentText` (emptied `DefinitionListAllowlist`).
- 13.7 → grouped sibling sections in `FluentAccordion`, **keeping** `<section class="chatbot-section">` wrappers (and their `chatbot-section-title` heading class) as accordion bodies.

The Story 13.1 layout-composition guard (`ChatBotLayoutCompositionConformanceTests`) confirms the result: **all five shrink-only lists are already empty** (`PageHeaderChromeAllowlist`, `PageContentBoxAllowlist`, `CommandBarAllowlist`, `DefinitionListAllowlist`, `NotYetComposedPageBacklog` = `[]`), so the razor markup has **zero** references to the banned chrome classes. The epic AC's "the guard allowlist is **empty**" is therefore already satisfied — **13.8 does not migrate any page; it deletes the now-orphaned CSS rules** and adds a CSS-side ratchet that keeps them gone.

This is a **CSS-only, rendering-layer correction**: no backend, command-spine, query, CLI, MCP, `.razor`, or `.cs` production-source change. Reference pattern: `Hexalith.Tenants.UI` ships only the layout CSS the design system does not own (no hand-rolled page/shell chrome). The real-render visual gate is **Story 13.9**.

### Authoritative dead-vs-live classification (source scan 2026-06-22, baseline `e9141d8`)

A scan of every `.chatbot-*` selector **defined** in `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` (137 distinct classes) cross-checked against **production markup refs** (`src/Hexalith.ChatBot.UI/**/*.razor` + `*.cs`, whole-token match) gives exactly **10 dead classes** (0 production-markup refs). **Re-run this scan at dev time before deleting each rule — the working tree drifts.**

**DELETE — 10 confirmed-dead selectors:**

| Class | Epic-AC named? | Why dead / who owns it now |
|---|---|---|
| `.chatbot-page-header` | ✅ yes | Replaced by `FcPageHeader` (13.2). The band that overlapped the shell top bar. |
| `.chatbot-page` (whole token) | ✅ yes | Replaced by `FcPageLayout` + `FluentStack`/`FluentCard` (13.3). The bordered content box. |
| `.chatbot-command-bar` | ✅ yes | Folded into `FcPageHeader` Actions slot / `FluentStack` (13.2). |
| `.chatbot-definition-list` (+ `dd` rule) | ✅ yes | Replaced by `FluentDataGrid`/`FluentStack`/`FluentText` (13.4/13.5/13.6). |
| `.chatbot-skip-link` (+ `:focus`/`:focus-visible`) | ✅ yes (the "custom skip-link") | The FrontComposer shell renders its own bypass-block `<a class="fc-skip-link" href="#fc-main-content">` → `#fc-main-content`. |
| `.chatbot-page-title` | coupled | The `<h1>` title **inside** the deleted `.chatbot-page-header` band (now `FcPageHeader` Heading). |
| `.chatbot-layout` | coupled | Epic-10 shell wrapper; `MainLayout.razor` now composes `<FrontComposerShell>`. |
| `.chatbot-shell-header` | coupled | Epic-10 shell chrome the FrontComposerShell replaced (`MainLayoutShouldUseFrontComposerShellAsTheSingleShellBoundary` asserts the layout no longer references it). |
| `.chatbot-shell-main` | coupled | Epic-10 shell main wrapper; the shell renders `#fc-main-content`. |
| `.chatbot-dense-row` | coupled | Orphaned dead layout class (no markup ref). |

**KEEP — still-live `.chatbot-*` classes (do NOT delete; partial list of the high-risk ones):**
- `.chatbot-section` (21 refs) and `.chatbot-section-title` (23 refs) — **kept by 13.7** as `FluentAccordion` item bodies/headings. **`.chatbot-page` is a prefix of `.chatbot-page-header`/`.chatbot-page-title`; `.chatbot-section` is unrelated and stays.**
- The conversation-item families (`.chatbot-email-conversation-item*`, `.chatbot-decision-*`, `.chatbot-participant-*`, `.chatbot-attachment-*`, `.chatbot-approval-*`, `.chatbot-failure-*`, `.chatbot-ai-outcome-*`), `.chatbot-conversation-shell*`, `.chatbot-conversation-stream*`, `.chatbot-conversation-status-summary*`, `.chatbot-conversation-classification*`, `.chatbot-conversation-review-history*`.
- `.chatbot-status`/`.chatbot-status__label`/`.chatbot-status-group`, `.chatbot-chip*`, `.chatbot-actor-badge*`, `.chatbot-governed-action*`, `.chatbot-governed-composer*`, `.chatbot-streaming-stop*`, `.chatbot-blocked-state*`, `.chatbot-validation-summary`, `.chatbot-association-*`, `.chatbot-why-project-panel*`, `.chatbot-ai-action-preview*`, `.chatbot-project-*`, `.chatbot-labelled-row*`, `.chatbot-audit-list`.
- A11y/layout hooks: `.chatbot-visually-hidden`, `.chatbot-touch-target*`, the `--chatbot-color-*` semantic aliases, `--chatbot-responsive-*`/touch vars, and the `[hidden]`/`html,body` UA resets.

**Scope fence — do NOT touch (belongs to another story):**
- **Stale E2E hand-authored HTML fixtures.** `tests/Hexalith.ChatBot.UI.E2E.Tests/*E2ETests.cs` embed pre-13.2 HTML strings (e.g. `<a class="chatbot-skip-link" …>`, `<section class="chatbot-page">`, `<dl class="chatbot-definition-list">`) and `querySelector(".chatbot-shell-main")`. These are the hand-authored fixtures the Epic-13 correction note flags; **replacing them with real renders is Story 13.9**, not 13.8. They do **not** read `chatbot.tokens.css`, so deleting the CSS does not break them — leave every E2E fixture untouched.
- The razor-side allowlists in `ChatBotLayoutCompositionConformanceTests` (already empty — 13.2–13.6 own them). Verify-only; do not edit.

## Acceptance Criteria

1. **(Epic-AC dead layout CSS deleted)** **Given** Stories 13.2–13.7 complete and all five layout-composition allowlists already empty, **When** `chatbot.tokens.css` is reduced, **Then** these confirmed-dead selectors are deleted in full: `.chatbot-page-header`, `.chatbot-page` (matched as a **whole class token**, never the `.chatbot-page-*` prefixes that are also being removed), `.chatbot-command-bar`, `.chatbot-definition-list` **including its `.chatbot-definition-list dd` descendant rule**, and the custom `.chatbot-skip-link` **including its `:focus`/`:focus-visible` rules** — the FrontComposer shell's `fc-skip-link`→`#fc-main-content` and `FcPageHeader`/`FcPageLayout` provide these affordances. Each removal is re-confirmed to have **0 production-markup references** (`src/Hexalith.ChatBot.UI/**/*.razor` + `*.cs`) at dev time before deletion.

2. **(Coupled dead shell/header chrome retired)** **Then** the dead chrome the FrontComposerShell / `FcPageHeader` / `FluentStack` now own is also deleted — `.chatbot-page-title`, `.chatbot-layout`, `.chatbot-shell-header`, `.chatbot-shell-main`, and the orphaned `.chatbot-dense-row` — each re-confirmed 0 production-markup refs; and **`chatbot.tokens.css` now contains only layout/accessibility CSS the design system does not own** (flex/grid, gaps, UA resets, responsive vars, and the focus / `forced-colors` / `prefers-reduced-motion` blocks for live classes).

3. **(Live classes preserved — no regression)** **Then** every still-referenced `.chatbot-*` class is left intact — explicitly `.chatbot-section`/`.chatbot-section-title` (the 13.7 accordion bodies/headings), the conversation-item/status/chip/actor-badge/association/why-panel/ai-action-preview/governed-composer/project families, `.chatbot-visually-hidden`, `.chatbot-touch-target*`, the `--chatbot-color-*` semantic aliases, `--chatbot-responsive-*`/touch CSS vars, the `[hidden]`/`html,body` resets, and all `@media (forced-colors / prefers-reduced-motion / min|max-width)` rules for live selectors. **When a dead selector is removed from a shared comma-separated selector list, its live siblings are preserved** (e.g. removing `.chatbot-command-bar,` from the flex group keeps `.chatbot-status`, `.chatbot-actor-badge`, …; removing `.chatbot-page,`/`.chatbot-page-header,` from the grid group keeps `.chatbot-section`, `.chatbot-status-group`, …; removing `.chatbot-page-title,` keeps `.chatbot-section-title`, `.chatbot-body`, …).

4. **(CSS-side retirement guard — non-vacuous, build-blocking)** **Then** a new `[Fact]` carrying `[Trait("Category", "Governance")]` (added to `ChatBotSemanticTokenContractTests` or a sibling `Story13LayoutCssRetirementTests`, mirroring the razor-side `ChatBotLayoutCompositionConformanceTests` ratchet) reads `chatbot.tokens.css` and asserts **none of the 10 retired selectors appear** — using a **selector/word-boundary match** so live prefixes (`.chatbot-section-title`, `.chatbot-page` must not false-match `.chatbot-section`, etc.) are not caught — and asserts the scan is **non-vacuous** (the file is found and the live anchors `--chatbot-color-neutral-background`, `.chatbot-status__label`, and `@media (forced-colors: active)` are still present). This makes the deletion build-enforced and prevents silent reintroduction.

5. **(Existing guard/contract lanes stay green)** **Then** the five `ChatBotLayoutCompositionConformanceTests` allowlists + `NotYetComposedPageBacklog` remain **empty** (verified, not edited); `ChatBotSemanticTokenContractTests` stays green — specifically `StylesheetShouldContainForcedColorsAndNonColorStatusCues` (which requires `.chatbot-status__label`, `.chatbot-conversation-status-summary`, `.chatbot-conversation-status-summary__health`, `data-chatbot-health="failed"`, `border-inline-start`, `border:`, `outline:`, `CanvasText`, `Highlight`), `StylesheetShouldMapSemanticColorsOnlyToFluentOrFrontComposerVariables`, `StylesheetShouldNotDeclareChatBotPrimitiveAliasesOrPaletteValues`, and `MainLayoutShouldUseFrontComposerShellAsTheSingleShellBoundary`; and `ChatBotFluentConformanceTests` stays green (no raw controls, no legacy v4/FAST tokens in the `.css` lane).

6. **(Scope fence + safety + build invariants)** **Then** the diff touches **only** `chatbot.tokens.css`, the new/updated guard test, this story doc, the evidence doc, and `sprint-status.yaml` — **no** `.razor`/`.cs` production source, **no** E2E hand-authored fixture (the stale `chatbot-page`/`chatbot-skip-link`/`chatbot-shell-main` strings in `*E2ETests.cs` are Story 13.9's real-render replacement), **no** localization resource, **no** allowlist edit, **no** sibling-submodule change. Rendering-layer-only: governed read/decision semantics, NFR6 a11y landmarks/labels, UX-DR4 non-color status cues, UX-DR34 focus management, EN+FR i18n, and the "no fake/freeform textbox" safety model are preserved exactly. The Release build is clean (`TreatWarningsAsErrors`, 0/0), the UI Governance lane + full `Hexalith.ChatBot.UI.Tests` regression + the real-browser `Hexalith.ChatBot.UI.E2E.Tests` suite are green (confirm `Skipped: 0` so the Chromium path actually executed — memory `chatbot-e2e-nobrowser-fallback-trap`).

## Tasks / Subtasks

- [x] **Task 1 — Re-confirm the dead-class set (AC: 1, 2, 3)**
  - [x] Re-run the authoritative scan against the live tree: for each of the 10 candidate classes (`chatbot-page-header`, `chatbot-page`, `chatbot-command-bar`, `chatbot-definition-list`, `chatbot-skip-link`, `chatbot-page-title`, `chatbot-layout`, `chatbot-shell-header`, `chatbot-shell-main`, `chatbot-dense-row`) confirm **0 whole-token references** in `src/Hexalith.ChatBot.UI/**/*.razor` + `*.cs` (e.g. `rg -P "(?<![a-z0-9-])<token>(?![a-z0-9-])" src/Hexalith.ChatBot.UI --glob '*.razor' --glob '*.cs'`). If any candidate has gained a reference, **keep it** and document the deviation; do not delete a live class.
  - [x] Confirm `.chatbot-section` and `.chatbot-section-title` still HAVE references (they must — 13.7 accordion bodies) so you do not accidentally delete them. `.chatbot-page` must be removed only as a whole token, never the `.chatbot-section`/`-section-title` neighbors.
- [x] **Task 2 — Delete the dead layout/shell chrome from `chatbot.tokens.css` (AC: 1, 2, 3)**
  - [x] `.chatbot-layout` — delete the standalone `min-height: 100vh` rule.
  - [x] `.chatbot-shell-header`, `.chatbot-command-bar` — remove both tokens from the flex-group selector list (`display: flex; align-items: center; …`), preserving the live siblings (`.chatbot-status`, `.chatbot-actor-badge`, `.chatbot-chip`, `.chatbot-governed-action`, …).
  - [x] `.chatbot-page`, `.chatbot-page-header` — remove both tokens from the grid-group selector list (`display: grid; gap; min-width:0`), preserving the live siblings (`.chatbot-section`, `.chatbot-status-group`, `.chatbot-conversation-shell`, `.chatbot-definition-list`† — see next, …); and delete the `.chatbot-page, .chatbot-conversation-shell { max-width: … }` rule's `.chatbot-page,` selector (keep `.chatbot-conversation-shell`).
  - [x] `.chatbot-shell-main` — delete its standalone box-sizing/padding rule, its `:focus-visible` entry in the focus-outline selector list, its standalone `@media (max-width: 599px)` padding-inline rule, and its `:focus-visible` entry in the `@media (forced-colors: active)` focus list.
  - [x] `.chatbot-page-title` — remove from the `min-width:0; overflow-wrap:anywhere` group **and** the `margin:0` group, preserving `.chatbot-section-title`, `.chatbot-body`, etc.
  - [x] `.chatbot-definition-list` — remove from the grid-group list, delete the standalone `grid-template-columns` rule **and** the `.chatbot-definition-list dd { … }` rule, and remove the `.chatbot-definition-list { grid-template-columns: minmax(0,1fr) }` entry inside `@media (max-width: 599px)`.
  - [x] `.chatbot-dense-row` — remove from the grid-group list (line ~92) and from the `@media (forced-colors: active)` list (line ~565).
  - [x] `.chatbot-skip-link` — delete the standalone rule and the `.chatbot-skip-link:focus, .chatbot-skip-link:focus-visible` rule, and remove the two `.chatbot-skip-link:focus`/`:focus-visible` entries from the `@media (forced-colors: active)` focus list.
  - [x] After each edit, ensure no selector list is left with a dangling/trailing comma or an empty `{}` rule, and that the live `forced-colors` / `prefers-reduced-motion` blocks still carry their live-class selectors and the required `CanvasText`/`Highlight`/`border-inline-start` tokens.
- [x] **Task 3 — Add the CSS-side retirement guard (AC: 4)**
  - [x] Add a `[Fact] [Trait("Category", "Governance")]` (in `ChatBotSemanticTokenContractTests.cs` reusing its `ReadProjectFile` helper, or a new `Story13LayoutCssRetirementTests.cs` mirroring the `ChatBotLayoutCompositionConformanceTests` boundary-regex style) named e.g. `StylesheetShouldNotContainRetiredLayoutChromeSelectors`. Read `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`; assert it is non-empty and contains the live anchors (`--chatbot-color-neutral-background`, `.chatbot-status__label`, `@media (forced-colors: active)`); then assert each of the 10 retired selectors is **absent** using a selector-boundary match (`\.chatbot-page(?![a-z0-9-])`, `\.chatbot-page-header(?![a-z0-9-])`, etc.) so a live prefix neighbor (`.chatbot-section-title`) is never the cause of a false pass or fail. Assert the scan is non-vacuous.
  - [x] Do **not** edit the `ChatBotLayoutCompositionConformanceTests` allowlists/backlog (all empty; 13.2–13.6 own them) — only verify they stay empty.
- [x] **Task 4 — Verify & record evidence (AC: 1–6)**
  - [x] Build: `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` → 0 Warning / 0 Error.
  - [x] Governance lane: `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-build -c Release -m:1 -nodeReuse:false` → all green (incl. the new CSS-retirement `[Fact]`, `ChatBotLayoutCompositionConformanceTests`, `ChatBotFluentConformanceTests`).
  - [x] Full `Hexalith.ChatBot.UI.Tests` regression (incl. `ChatBotSemanticTokenContractTests`) → green.
  - [x] Real-browser E2E: `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-build -c Release -m:1 -nodeReuse:false` → green with **`Skipped: 0`** (confirm the Chromium/Chrome path executed, not the no-browser string fallback — memory `chatbot-e2e-nobrowser-fallback-trap`).
  - [x] `git diff --name-only` → only `chatbot.tokens.css`, the guard test, this story doc, the evidence doc, and `sprint-status.yaml`. **No `.razor`, no `.cs` production source, no `*E2ETests.cs` fixture, no localization, no submodule.** `git diff --check` → clean.
  - [x] Write `_bmad-output/implementation-artifacts/tests/test-summary-story-13.8.md` (metadata-only, same shape as `test-summary-story-13.7.md`), listing the 10 retired selectors and the build/test gate results. Live `aspire run` real-render visual proof is Story 13.9's gate, not this story.

## Dev Notes

### Current state of `chatbot.tokens.css` (read before editing; line numbers are baseline `e9141d8` and will drift)

The file (~660 lines) is already the reduced post-12.8 stylesheet: a `:root` block of `--chatbot-color-*` Fluent-aliases + `--chatbot-responsive-*`/touch vars; UA resets (`html,body`, `[hidden]`); two big comma-separated layout groups (a flex group and a grid group); per-class layout rules; status/border rules; focus-outline lists; and `@media` blocks for `min/max-width`, `prefers-reduced-motion`, and `forced-colors`. The 10 dead classes are interleaved across these — the surgical removals are enumerated in Task 2. Anchor every edit by **content/selector**, not line number.

Key adjacency traps:
- **`.chatbot-page` is a prefix.** A naive text delete of `chatbot-page` would corrupt `chatbot-page-header`/`chatbot-page-title` (both also being deleted) — fine — but a careless regex could also touch unrelated tokens; use whole-token boundaries. `.chatbot-section`/`.chatbot-section-title` are NOT prefixed by `.chatbot-page` and must survive.
- **Shared selector lists.** `.chatbot-command-bar`/`.chatbot-shell-header` (flex group), `.chatbot-page`/`.chatbot-page-header`/`.chatbot-definition-list`/`.chatbot-dense-row` (grid group), `.chatbot-page-title` (two min-width/margin groups), `.chatbot-shell-main`/`.chatbot-skip-link` (focus + forced-colors lists) each sit in a comma list with **live** siblings — remove only the dead token, keep the list and its live members.
- **`forced-colors` / `reduced-motion` blocks must keep their live anchors.** `StylesheetShouldContainForcedColorsAndNonColorStatusCues` asserts `.chatbot-status__label`, `.chatbot-conversation-status-summary`, `.chatbot-conversation-status-summary__health`, `data-chatbot-health="failed"`, `border-inline-start`, `border:`, `outline:`, `CanvasText`, `Highlight` all remain — do not collapse those blocks.

### Why deletion is test-safe (verified)

- **CSS deletion does not change rendered DOM.** Production `.razor`/`.cs` already have **0** references to the 10 dead classes, so no rendered element carries them; removing the rules changes nothing the E2E DOM scans observe.
- **The only test that reads `chatbot.tokens.css`** is `ChatBotSemanticTokenContractTests` — its assertions require live selectors/tokens to be **present** (untouched) and dead shell classes to be **absent from the layout razor** (`MainLayoutShouldUseFrontComposerShellAsTheSingleShellBoundary` → `layout.ShouldNotContain("chatbot-layout"/"chatbot-shell-header"/"chatbot-shell-main")`, which read `MainLayout.razor`, not the CSS).
- **The E2E suites embed their own hand-authored HTML fixtures** (still containing the old classes) and `querySelector` against those strings — they never load the CSS file. They are stale-by-design until **Story 13.9** re-verifies real renders; **out of scope here**.

### The skip-link is not lost (NFR6 / WCAG bypass-blocks)

`MainLayout.razor` is two lines: `<FrontComposerShell AppTitle="Hexalith ChatBot">@Body</FrontComposerShell>`. The FrontComposer shell renders its **own** bypass-block — `<a class="fc-skip-link" href="#fc-main-content">…skip to content…</a>` followed by the `#fc-main-content` `tabindex="-1"` landmark (FrontComposer Story 3-1/3-2). Deleting `.chatbot-skip-link` removes only the dead ChatBot styling; the keyboard "skip to main content" affordance is preserved by the shell. The `ChatBotFocusSequenceContract` record (`SkipLinkTargetId: "chatbot-main-content"`) asserted by `ChatBotAccessibilityFocusContractTests` is **design metadata**, not the CSS class — unaffected by this deletion.

### Reference pattern

`Hexalith.Tenants/src/Hexalith.Tenants.UI` ships only the layout CSS the design system does not own — it has no hand-rolled page-header/page-box/command-bar/skip-link CSS, composing entirely through FrontComposer + Fluent. After 13.8, `chatbot.tokens.css` matches that posture: Fluent-aliased color slots, responsive/touch vars, UA resets, and layout/a11y rules for the live `.chatbot-*` component families only.

### Latest technical information (pins — do not change)

- Fluent UI Blazor pinned `5.0.0-rc.3-26138.1`; FrontComposer pinned. No `Directory.Packages.props`/package-version edit. This story changes **no** component usage — it only deletes dead CSS and adds one source-scan `[Fact]`. [Source: `Directory.Packages.props`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`]
- ChatBot UI has **zero `RenderComponent<`** (no bUnit). Correctness here is verified by the CSS source-scan guard + the unchanged Governance/contract lanes + real-browser E2E + Story 13.9's real-render gate — not a rendering unit test. [Source: memory `chatbot-ui-no-bunit-test-strategy`]

### Regression traps (the review will check these)

- **Do not delete a live class.** `.chatbot-section` (21 refs) and `.chatbot-section-title` (23 refs) survive (13.7 accordion bodies/headings); the conversation/status/chip/association/why-panel families, `.chatbot-visually-hidden`, `.chatbot-touch-target*`, and the `--chatbot-color-*` aliases all stay. Re-scan before deleting; if a "dead" candidate gained a ref, keep it.
- **Preserve list siblings.** Removing a dead token from a comma-separated selector list must not drop its live neighbors or leave a trailing comma / empty rule.
- **Keep the forced-colors + reduced-motion + responsive blocks valid** with their live-class selectors and required tokens (`CanvasText`, `Highlight`, `border-inline-start`, `outline:`), or `StylesheetShouldContainForcedColorsAndNonColorStatusCues` fails.
- **Do not touch E2E fixtures.** The stale `chatbot-page`/`chatbot-page-header`/`chatbot-definition-list`/`chatbot-skip-link`/`chatbot-shell-main` strings in `tests/Hexalith.ChatBot.UI.E2E.Tests/*E2ETests.cs` are Story 13.9's real-render replacement — editing them here is scope creep and risks breaking the green E2E lane.
- **Do not edit the razor-side allowlists.** `ChatBotLayoutCompositionConformanceTests`'s five lists + `NotYetComposedPageBacklog` are already empty — verify-only.
- **No raw controls / no legacy tokens.** `ChatBotFluentConformanceTests` still runs its `.css` lane (bans `--type-ramp-*`/`--neutral-*`/`--accent-*`/`--palette-*`/`--design-unit`); deleting dead `.chatbot-*` rules introduces none.

### Architecture & boundary guardrails

- This story touches only `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` + one UI test file + two BMAD docs + `sprint-status.yaml`. The adapter boundary (UI → Client / ServiceDefaults / FrontComposer Shell/Contracts only) is untouched. [Source: `architecture.md#Frontend Architecture` (L411); `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`]
- Rendering-layer correction only: governed semantics, NFR6 a11y landmarks/labels, UX-DR4 non-color cues, UX-DR34 focus, EN+FR i18n, and the "no fake/freeform textbox" model are preserved exactly. Epic 13 completion requires the layout-composition allowlists at **empty** (already true) and the hand-rolled chrome CSS retired (this story). [Source: `epics.md#Epic 13`/`#Story 13.8`; `hexalith-ux-instructions.md`]

### File structure

- **Edit (CSS):** `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- **Edit/New (test):** the CSS-retirement guard `[Fact]` — extend `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs` or add `tests/Hexalith.ChatBot.UI.Tests/Story13LayoutCssRetirementTests.cs`.
- **New (evidence):** `_bmad-output/implementation-artifacts/tests/test-summary-story-13.8.md`.
- **Do NOT edit:** any `.razor`, any `.cs` production source, any `tests/Hexalith.ChatBot.UI.E2E.Tests/*E2ETests.cs` fixture, `ChatBotLayoutCompositionConformanceTests.cs` allowlists, `ChatBotUiTextKey.cs`/`.resx`, generated `obj/**`, or any sibling submodule.

### Testing standards

- xUnit v3 + Shouldly; the new guard carries `[Trait("Category", "Governance")]` and must be non-vacuous (assert the CSS file is found + live anchors present before asserting dead-selector absence). Run the UI Governance filter, the full UI.Tests regression, the slnx Release build, then the real-browser E2E (confirm `Skipped: 0`), then `git diff --check` + `git diff --name-only`. Failure messages stay metadata-only. [Source: `Hexalith.AI.Tools/CLAUDE.md#Testing Standards`; memories `chatbot-ui-no-bunit-test-strategy`, `chatbot-e2e-nobrowser-fallback-trap`]

### Previous story intelligence

- **Story 13.7 (done)** kept `<section class="chatbot-section">` + `chatbot-section-title` as `FluentAccordion` item bodies/headings and explicitly deferred "deleting any `chatbot.tokens.css` rule (incl. `.chatbot-section`, the skip-link) → **Story 13.8**." So `.chatbot-section`/`-section-title` are **live and must stay**; the skip-link is **this story's** deletion. 13.7's review valued documenting every coupled-test/file change in the File List + evidence doc — do the same. [Source: `13-7-group-sections-in-fluent-accordion.md`]
- **Stories 13.2/13.3/13.4/13.5/13.6 (done)** drove the razor markup off the chrome classes and emptied the five layout-composition allowlists — which is exactly why the 10 classes are now dead CSS. Build on their result; do not revert their `FcPageLayout`/`FcPageHeader`/`FluentStack`/`FluentDataGrid`/`FluentAccordion` composition. [Source: `13-2-…md`, `13-3-…md`, `13-4-…md`, `13-5-…md`, `13-6-…md`; `ChatBotLayoutCompositionConformanceTests.cs` (all lists `[]`)]
- **Memory `chatbot-epic13-guard-seed-count-variance`**: Epic 13 source-scan counts are authoritative over the proposal prose; this story's 10-class deletion set comes from a live source scan, not a prose count. Re-scan before deleting. **Memory `chatbot-ui-fluent-component-divergence`**: prior re-verification trusted hand-authored fixtures not the live app — the stale E2E fixtures here are exactly that, and they are Story 13.9's problem, not 13.8's.

### Git intelligence

- Recent commits: `e9141d8 feat(story-13.7): Group sibling sections in FluentAccordion` (baseline/HEAD), `b15711d feat(story-13.5)`, `0f3bfc8 feat(story-13.3)`, `bfed90a feat(story-13.2)`, `d344a98 feat(story-13.1)`. Working tree is clean except the story-automator orchestration doc. Note: per 13.7's review, some Epic-13 source landed in mixed/misleadingly-titled commits — re-read the live `chatbot.tokens.css` and tests at dev time; anchor edits by content. Commit as `feat(story-13.8): retire remaining chatbot layout CSS`. Keep this story's production change (the CSS deletion) in its **own scoped commit** (no bundled submodule bumps / unrelated story artifacts) — the 13.7 review flagged bundled commits as a MEDIUM finding. [Source: `git log`; `git status`; `13-7-…md` Senior Developer Review]

### Project structure notes

- Aligns with the established ChatBot UI test layout (source-scan Governance guards in `tests/Hexalith.ChatBot.UI.Tests`, no bUnit; real-browser E2E; evidence under `_bmad-output/implementation-artifacts/tests/`). Unlike 13.2–13.6 (which shrank allowlists) and 13.7 (which added an accordion-required list), 13.8 is the **CSS cleanup + CSS-side ratchet** story — it deletes dead rules and adds one guard asserting they stay gone. The five razor-side allowlists are already empty and stay so. No new projects/packages/localization keys. Real-render visual proof is Story 13.9.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, `checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`; `Hexalith.AI.Tools/hexalith-ux-instructions.md`; `Hexalith.AI.Tools/CLAUDE.md` (UI/UX + testing + submodule rules)]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml` (`13-8-retire-chatbot-layout-css`)]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13`; `#Story 13.8`; `#Story 13.3`; `#Story 13.7`; `#Story 13.9`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-22.md` (Epic 13 defect table; reference pattern `Hexalith.Tenants.UI`)]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture` (L411 Epic 13 layout composition)]
- [Source: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` (the file under edit)]
- [Source: `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor` (two-line `<FrontComposerShell>`); `Components/App.razor` (`<link … css/chatbot.tokens.css>`); `Design/ChatBotFocusSequenceContract.cs` (skip-link metadata)]
- [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` (`fc-skip-link` → `#fc-main-content` bypass-block)]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (5 empty allowlists); `ChatBotSemanticTokenContractTests.cs` (reads the CSS; forced-colors/semantic-mapping/primitive-ban asserts); `ChatBotFluentConformanceTests.cs` (`.css` legacy-token lane); `Story13DefinitionListMigrationTests.cs`, `Story13AccordionMigrationTests.cs` (sibling-guard precedent)]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/*E2ETests.cs` (stale hand-authored fixtures — Story 13.9 scope, NOT this story)]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.UI` (reference posture — only design-system-unowned layout CSS)]
- [Source: `_bmad-output/implementation-artifacts/13-7-group-sections-in-fluent-accordion.md`; `tests/test-summary-story-13.7.md` (evidence-doc shape)]
- [Source: memories `chatbot-ui-fluent-component-divergence`, `chatbot-ui-no-bunit-test-strategy`, `chatbot-epic13-guard-seed-count-variance`, `chatbot-e2e-nobrowser-fallback-trap`]

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-22 · **Outcome: ✅ Approved (→ done)**

Adversarial review of Story 13.8. Every story claim was independently re-verified against the live tree and the gates were **re-run, not trusted** from the evidence doc.

**Verified findings**

- **CSS deletion is exact and complete.** `git diff` on `chatbot.tokens.css` is surgical (660 → 599 lines); a selector-boundary scan (`\.<token>(?![a-z0-9-])`) finds **0** residue for all 10 retired selectors, and a whole-token scan finds **0** production-markup refs in `src/Hexalith.ChatBot.UI/**/*.{razor,cs}` for each. No dangling/double commas, no empty `{}` rules.
- **Live siblings preserved (AC3).** `.chatbot-section` (21 refs) and `.chatbot-section-title` (23 refs) survive; every shared comma-list kept its live head/members. All `ChatBotSemanticTokenContractTests` anchors present in the CSS (`.chatbot-status__label`, `.chatbot-conversation-status-summary__health`, `data-chatbot-health="failed"`, `border-inline-start`, `border:`, `outline:`, `CanvasText`, `Highlight`).
- **AC2 "only design-system-unowned CSS" holds.** Swept the remaining utility classes (`chatbot-shimmer`/`-skeleton`/`-row-motion`/`-streaming-text`/`-panel-transition`) — all still live (1 ref each); no other dead layout class lurks outside the 10.
- **Guard is non-vacuous & build-blocking (AC4).** `Story13LayoutCssRetirementTests` reads the on-disk CSS via repo-root traversal, proves 7 live anchors before asserting absence, and 11 detector `[Theory]` pins prove the boundary catches dead tokens but never live prefix neighbors.
- **Gates re-run independently:** Release build **0/0**; Governance lane **60/0/0**; new guard **12/0/0**; full `Hexalith.ChatBot.UI.Tests` **227/0/0**; real-browser E2E **136/0/0, Skipped: 0** in **22 s** (Chromium path executed — not the no-browser string fallback; memory `chatbot-e2e-nobrowser-fallback-trap`). All match the evidence doc.
- **Scope fence held (AC6).** E2E hand-authored fixtures untouched; the five `ChatBotLayoutCompositionConformanceTests` allowlists + `NotYetComposedPageBacklog` remain `[]` (verify-only); no `.razor`/`.cs`/localization/allowlist edit.

**Severity tally: 0 Critical · 0 High · 0 Medium · 2 Low (non-blocking).**

- **LOW (commit hygiene — not a 13.8 defect):** the working tree also carries **pre-existing** out-of-scope changes (`Hexalith.EventStore`/`Hexalith.Timesheets` submodule pointers, `orchestration-13-…md`) that were already dirty at session start. They are correctly untouched, but must be **excluded from the `feat(story-13.8)` commit** — the 13.7 review flagged bundled commits as MEDIUM, and memory `story-automator-session-monitoring` mandates a submodule guard before commit-story.
- **LOW (informational):** the guard's `RequiredLiveAnchors` couples non-vacuity to the trailing-comma form (`.chatbot-section,`). This is deliberate (it proves the shared-list head survived) and currently green — noted, no change needed.

No code changes were required; the implementation is approved as-is.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context)

### Debug Log References

- Re-ran the authoritative whole-token scan (`rg -P "(?<![a-z0-9-])<token>(?![a-z0-9-])" src/Hexalith.ChatBot.UI --glob '*.razor' --glob '*.cs'`) for all 10 candidates → **0** production-markup refs each; live neighbors confirmed present (`.chatbot-section` = 21, `.chatbot-section-title` = 23). No candidate gained a reference; full 10-class deletion set proceeds.
- Mapped every CSS occurrence of each dead token before editing; deleted them surgically by content anchor (not line number). Post-edit re-scan of `chatbot.tokens.css` → **0** occurrences for all 10; no dangling commas, no empty `{}` rules; live anchors (`--chatbot-color-neutral-background`, `.chatbot-status__label`, `@media (forced-colors: active)`, `CanvasText`, `Highlight`, `border-inline-start`) all retained.
- `chatbot.tokens.css`: 660 → 599 lines.
- Build/test gates (Release, `-m:1 -nodeReuse:false`): slnx build **0/0**; UI.Tests Governance lane **60/0/0**; new `Story13LayoutCssRetirementTests` **12/0/0**; full UI.Tests regression **227/0/0**; real-browser E2E **136/0/0, Skipped: 0** in 22 s (Google Chrome 148 — real path, no no-browser fallback). `git diff --check` clean.

### Completion Notes List

- **AC1/AC2 — dead chrome deleted.** Removed all 10 confirmed-dead selectors from `chatbot.tokens.css`: the 5 epic-AC-named (`.chatbot-page-header`, whole-token `.chatbot-page` incl. its `max-width` rule, `.chatbot-command-bar`, `.chatbot-definition-list` incl. its `dd` rule and the `@media (max-width: 599px)` entry, `.chatbot-skip-link` incl. `:focus`/`:focus-visible` and both forced-colors focus-list entries) plus the 5 coupled (`.chatbot-page-title`, `.chatbot-layout`, `.chatbot-shell-header`, `.chatbot-shell-main` incl. its focus + 599px + forced-colors entries, `.chatbot-dense-row` incl. its forced-colors entry). The stylesheet now holds only design-system-unowned layout/a11y CSS, matching the `Hexalith.Tenants.UI` posture.
- **AC3 — live classes preserved.** Every dead token removed from a shared comma-list left its live siblings intact (flex group head → `.chatbot-status,`; grid group head → `.chatbot-section,`; `max-width` → `.chatbot-conversation-shell`; min-width/margin groups keep `.chatbot-section-title`/`.chatbot-body`; focus list keeps the live `.chatbot-conversation-shell__main:focus-within`; 599px keeps governed-action/streaming-stop/status; forced-colors blocks keep their live selectors + `CanvasText`/`Highlight`/`border-inline-start`). `ChatBotSemanticTokenContractTests` (the only CSS reader) stays green.
- **AC4 — CSS-side retirement guard.** Added `tests/Hexalith.ChatBot.UI.Tests/Story13LayoutCssRetirementTests.cs` — a `[Fact] [Trait("Category","Governance")]` that reads the CSS, proves non-vacuity (file found + 7 live anchors present), then asserts all 10 retired selectors absent via a `\.<token>(?![a-z0-9-])` selector-boundary matcher; 11 detector `[Theory]` pins prove the boundary catches the dead token but never a live prefix neighbor (`.chatbot-page`≠`.chatbot-section`/`.chatbot-page-header`; `.chatbot-page-title`≠`.chatbot-section-title`).
- **AC5 — existing lanes green (verify-only).** The five `ChatBotLayoutCompositionConformanceTests` allowlists + `NotYetComposedPageBacklog` remain `[]` (not edited); `ChatBotSemanticTokenContractTests` and `ChatBotFluentConformanceTests` (`.css` legacy-token lane) stay green.
- **AC6 — scope fence + build invariants.** Diff touches only `chatbot.tokens.css`, the new guard test, this story doc, the evidence doc, and `sprint-status.yaml`. No `.razor`/`.cs` production source, no `*E2ETests.cs` fixture (the stale `chatbot-page`/`chatbot-skip-link`/`chatbot-shell-main` strings are Story 13.9's real-render replacement and never load the CSS), no localization, no allowlist edit, no sibling-submodule change. Release build clean (`TreatWarningsAsErrors`, 0/0); Governance + full UI.Tests + real-browser E2E (`Skipped: 0`) all green. The pre-existing `Hexalith.EventStore`/`Hexalith.Timesheets` submodule-pointer and story-automator orchestration-doc changes were already in the working tree at the baseline and are out of this story's scope (untouched).

### File List

- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` (modified — deleted the 10 dead `.chatbot-*` layout/shell chrome selectors and their standalone/`@media`/focus entries; 660 → 599 lines)
- `tests/Hexalith.ChatBot.UI.Tests/Story13LayoutCssRetirementTests.cs` (new — CSS-side retirement Governance guard + detector-fixture pins)
- `_bmad-output/implementation-artifacts/tests/test-summary-story-13.8.md` (new — evidence doc)
- `_bmad-output/implementation-artifacts/13-8-retire-chatbot-layout-css.md` (this story doc — Tasks/Subtasks, Dev Agent Record, Status)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status: `13-8-retire-chatbot-layout-css` → `review`)

### Change Log

| Date | Change |
| --- | --- |
| 2026-06-22 | Senior Developer Review (AI) — **Approved (→ done)**. Adversarial review re-verified all claims against the live tree and re-ran every gate independently: CSS diff surgical (660→599), 0 dead-token residue, 0 production refs for all 10 retired selectors, live siblings (`.chatbot-section` 21 / `-section-title` 23) + all semantic anchors intact, AC2 "only design-system-unowned CSS" confirmed (no hidden dead classes). Gates: build 0/0; Governance 60/0/0; new guard 12/0/0; full UI.Tests 227/0/0; real-browser E2E 136/0/0 Skipped:0 (22 s, Chromium path). 0 Critical/High/Medium; 2 Low (pre-existing out-of-scope submodule/orchestration changes must stay out of the commit; trailing-comma anchor is intentional). Status → done; sprint-status synced. |
| 2026-06-22 | Implemented Story 13.8 (→ review): deleted all 10 confirmed-dead `.chatbot-*` page/shell layout selectors from `chatbot.tokens.css` (660 → 599 lines), preserving every live sibling in shared comma-lists and the forced-colors/reduced-motion/responsive anchors; added `Story13LayoutCssRetirementTests` (CSS-side Governance ratchet + 11 detector pins) asserting the 10 selectors stay absent via selector-boundary regex; verified the five razor-side allowlists stay `[]`. Gates green: slnx Release build 0/0; UI.Tests Governance 60/0/0; full UI.Tests 227/0/0; real-browser E2E 136/0/0 (Skipped: 0, Chrome 148); `git diff --check` clean; scope fence held (no `.razor`/`.cs`/E2E-fixture/localization/allowlist/submodule edit). |
| 2026-06-22 | Created Story 13.8 (ready-for-dev): retire the now-dead `.chatbot-*` layout/shell chrome from `chatbot.tokens.css`. Authoritative source scan classifies 10 dead selectors (5 epic-AC-named — `chatbot-page-header`, `chatbot-page`, `chatbot-command-bar`, `chatbot-definition-list`, `chatbot-skip-link` — plus 5 coupled dead chrome — `chatbot-page-title`, `chatbot-layout`, `chatbot-shell-header`, `chatbot-shell-main`, `chatbot-dense-row`) vs. live classes to preserve (`chatbot-section`/`-section-title` + the conversation/status/association families). Established that the five layout-composition allowlists are already empty (epic "allowlist empty" pre-satisfied), the FrontComposer shell provides the skip-link, CSS deletion is test-safe (no DOM change; stale E2E fixtures are Story 13.9 scope), and added a CSS-side retirement guard requirement. Flagged the live-sibling-preservation, forced-colors-anchor, E2E-fixture, and own-scoped-commit traps. |
