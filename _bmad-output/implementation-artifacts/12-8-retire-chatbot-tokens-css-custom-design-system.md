---
baseline_commit: 17975d9
---

# Story 12.8: Retire the `chatbot.tokens.css` custom design system

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-21. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a frontend engineer,
I want the ChatBot-owned token stylesheet reduced to Fluent-backed semantic aliases and layout-only CSS,
so that the UI no longer carries a parallel custom design system after the Fluent v5 component migrations.

## Acceptance Criteria

1. **The CSS primitive backlog is burned down to zero.** Given Stories 12.2-12.7 are done and `ChatBotFluentConformanceTests.RawControlMigrationBacklog` is already empty, when Story 12.8 completes, then `PrimitiveMigrationBacklog` no longer contains `wwwroot/css/chatbot.tokens.css`, the no-theme-redefinition guard passes with no temporary CSS backlog, and `CountPrimitiveDebt` reports zero for `--chatbot-type-*`, `--chatbot-radius-*`, `--chatbot-font-*`, `.chatbot-button`, heading typography declarations, foreground `color:` role declarations, and native `button/input/select/textarea` CSS selectors. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.8`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`]

2. **`chatbot.tokens.css` is reduced to semantic Fluent aliases plus layout CSS only.** Given the current 1,312-line stylesheet still contains Fluent-provided primitive styling, when reduced, then it keeps only the ChatBot semantic color aliases that map directly to Fluent v5 custom properties (`--colorNeutral*`, `--colorBrand*`, `--colorStatus*`) and layout/accessibility CSS the design system does not own (grid/flex layout, gaps, responsive wrapping, user-agent reset, visually-hidden utility, safe focusable route/skip behavior where no Fluent component owns it). It deletes static typography aliases (`--chatbot-type-*`), ChatBot font aliases, ChatBot radius aliases, Fluent primitive typography/radius/foreground declarations, native-control selectors, and button/input styling now owned by Fluent components. [Source: `Hexalith.AI.Tools/hexalith-ux-instructions.md`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`]

3. **Component markup no longer depends on removed presentation classes.** Given migrated components still pass presentation-only classes such as `chatbot-action-button`, `chatbot-governed-composer__input`, `chatbot-association-actions__input`, `chatbot-actor-badge__action`, and `chatbot-why-project-panel__close/correction` to Fluent components, when CSS primitives are removed, then those classes are either deleted or narrowed to layout-only hooks, and Fluent component parameters (`Appearance`, `Color`, `Size`, `Weight`, component choice) own the button, text, input, badge, chip, and heading presentation. Stable behavior markers must use semantic elements, ids, aria attributes, or `data-chatbot-*` markers rather than custom styling classes. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`]

4. **Semantic-token tests stop blessing custom primitives.** Given `ChatBotSemanticTokenContractTests` currently asserts spacing/radius/type/font aliases and literal `font-size: var(--chatbot-type-*)` declarations, when reframed, then it validates only the governed semantic slot set and direct Fluent-token color mappings, confirms the stylesheet is registered once through `Components/App.razor`, and asserts there are no raw hex/RGB/HSL palette values, temporary bridge wording, legacy v4/FAST tokens, or custom primitive aliases. It must not require ChatBot radius, font, type-ramp, button, or component-surface styling. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`; `src/Hexalith.ChatBot.UI/Design/ChatBotSemanticTokenContract.cs`; `src/Hexalith.ChatBot.UI/Components/App.razor`]

5. **Primitive/source-contract tests are updated to the new Fluent-owned boundary.** Given `ChatBotGovernedPrimitiveContractTests` currently asserts CSS selectors including `.chatbot-status__label`, `.chatbot-chip__cue`, and `.chatbot-governed-composer__input`, when the stylesheet is retired, then tests assert Fluent component usage, semantic status data/aria contracts, non-color status cues, and forced-colors survivability without requiring deleted custom design-system selectors. Existing behavioral contracts for actor categories, evidence states, risk classes, blocked states, status banners, governed operations, and localization remain intact. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`]

6. **No UX, accessibility, or safety semantics regress while CSS is removed.** Given Epic 12 is rendering-layer remediation only, when Story 12.8 completes, then no backend, CommandGateway, CLI, MCP, EventStore, projection, SignalR, package pin, or submodule content changes are made; the governed chat no-fake-freeform safety model, EN+FR localization, focus management, disabled/unavailable explanations, metadata-only audit display, non-color status labels/icons/borders, light/dark/forced-colors requirements, and responsive/touch affordances are preserved. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `Hexalith.AI.Tools/hexalith-ux-instructions.md`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`]

7. **Verification proves the CSS retirement and leaves Story 12.9 as a re-verification story, not cleanup.** Given Story 12.9 lands last, when Story 12.8 completes, then the focused build/governance/source-contract lanes are green, `rg` confirms no forbidden primitive aliases/selectors remain in `chatbot.tokens.css`, no raw interactive controls exist in `.razor`, and a story-specific test summary records commands/results. Do not perform the full cross-surface Playwright a11y/visual re-verification owned by Story 12.9, but keep all affected existing E2E/source fixtures compiling and honest about browser skips. [Source: `_bmad-output/implementation-artifacts/12-7-migrate-operational-and-audit-pages-to-fluent.md`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.7.md`; `_bmad-output/planning-artifacts/epics.md#Story 12.9`]

## Tasks / Subtasks

- [x] Reduce `chatbot.tokens.css` to allowed content only (AC: 1, 2, 6)
  - [x] Preserve the direct semantic Fluent color aliases required by `ChatBotSemanticTokenContract.Slots`: neutral, brand, info, warning, danger, success.
  - [x] Keep only layout/accessibility CSS the design system does not own: grid/flex composition, gaps/wrapping, responsive layout, UA reset, skip/visually-hidden behavior, overflow handling, and focused route-level exceptions where no Fluent component owns the behavior.
  - [x] Delete `--chatbot-type-*`, `--chatbot-font-*`, and `--chatbot-radius-*` declarations.
  - [x] Delete custom typography declarations (`font-size`, `font-weight`, `line-height`) where Fluent `FluentText`/component params or browser semantic headings should own presentation.
  - [x] Delete custom foreground `color:` role styling where Fluent component `Color`, semantic Fluent tokens, badge/chip color, or inherited theme color should own it.
  - [x] Delete native `button/input/select/textarea` selectors and any styling of Fluent-provided button/input primitives.

- [x] Remove or narrow presentation-only component classes (AC: 2, 3, 6)
  - [x] Inspect every class hit from `rg -n "chatbot-action-button|chatbot-governed-composer__input|chatbot-association-actions__input|chatbot-actor-badge__action|chatbot-why-project-panel__(close|correction)|chatbot-project-picker__link" src/Hexalith.ChatBot.UI/Components tests/Hexalith.ChatBot.UI.Tests`.
  - [x] Remove `Class=` hooks from Fluent controls when they exist only for ChatBot primitive styling; keep Fluent `Appearance`, `Color`, `Size`, `Weight`, `Id`, `aria-*`, and `data-chatbot-*` contracts.
  - [x] For raw `<a>` navigation links (allowed by the guard), keep only minimal layout/accessibility styling if Fluent/FrontComposer provides no equivalent link/navigation component for that location.
  - [x] Do not introduce raw lowercase `<button>/<input>/<select>/<textarea>` while removing classes.

- [x] Reframe `ChatBotSemanticTokenContractTests` (AC: 4)
  - [x] Remove `ExpectedSpacingAndRadiusAliases` and typography alias assertions.
  - [x] Keep semantic slot ordering/meaning checks and direct Fluent color mapping checks.
  - [x] Add/keep negative checks for raw color literals, legacy v4/FAST tokens, temporary bridge wording, and custom primitive aliases.
  - [x] Keep `AppShouldRegisterTokenStylesheetAndDelegateProvidersToFrontComposerShell` and `MainLayoutShouldUseFrontComposerShellAsTheSingleShellBoundary` green.

- [x] Update the no-theme-redefinition governance guard (AC: 1, 2, 7)
  - [x] Remove `wwwroot/css/chatbot.tokens.css` from `PrimitiveMigrationBacklog` after the stylesheet has zero primitive debt.
  - [x] Keep detector fixture tests for primitive debt so future regressions fail.
  - [x] Keep the raw-control guard backlog empty.
  - [x] Ensure failure messages stay metadata-only and report relative paths/counts, not full source dumps.

- [x] Update source-contract and fixture tests affected by CSS class removal (AC: 3, 5, 6)
  - [x] Update `ChatBotGovernedPrimitiveContractTests` to assert Fluent component boundaries and semantic data/aria contracts instead of deleted CSS selectors.
  - [x] Update any source/E2E fixture strings in `tests/Hexalith.ChatBot.UI.E2E.Tests` or UI tests that still require deleted typography/helper classes only as style proof.
  - [x] Preserve behavior assertions for actor categories, evidence/risk state labels, blocked/status live-region behavior, metadata-only audit content, localization keys, and stable `data-chatbot-*` markers.

- [x] Verify and record results (AC: all)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` (restore first only if needed).
  - [x] Run focused UI tests via compiled xUnit v3 executable if VSTest sockets are denied:
        `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -class Hexalith.ChatBot.UI.Tests.ChatBotFluentConformanceTests`
  - [x] Run focused semantic/primitive/source-contract lanes:
        `-class Hexalith.ChatBot.UI.Tests.ChatBotSemanticTokenContractTests`,
        `-class Hexalith.ChatBot.UI.Tests.ChatBotGovernedPrimitiveContractTests`,
        `-class Hexalith.ChatBot.UI.Tests.ChatBotAccessibilityFocusContractTests`,
        `-class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests`.
  - [x] Run any affected E2E fixture class only if source changes touch its fixtures; otherwise record why Story 12.9 owns full browser visual/a11y re-verification.
  - [x] Run `rg -n -- "--chatbot-type-|--chatbot-font-|--chatbot-radius-|\\.chatbot-button|\\b(button|input|select|textarea)([:.#\\s,{>+~\\[]|$)" src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` and expect no forbidden matches.
  - [x] Run `git diff --check`.
  - [x] Add `_bmad-output/implementation-artifacts/tests/test-summary-story-12.8.md` with exact commands, pass/fail counts, and any environmental limitations.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: user `Jerome`, project `chatbot`, planning artifacts under `_bmad-output/planning-artifacts`, implementation artifacts under `_bmad-output/implementation-artifacts`, document language English.
- Loaded sprint status from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key `12-8-retire-chatbot-tokens-css-custom-design-system` was `backlog`; `epic-12` was already `in-progress`; Stories 12.1-12.7 were `done`; Story 12.9 remains `backlog`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; relevant sections are UX-DR1/UX-DR2, Epic 10 correction note, Epic 12, and Story 12.8.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`; relevant section is `Frontend Architecture`, especially ChatBot UI Fluent-only conformance, no-theme-redefinition, empty allowlist target, and Fluent UI v5 pin.
- Loaded deeper UX artifacts from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md` because the discovery table's first-level UX glob does not reach the nested folder. Binding UX rule: Fluent UI v5 -> FrontComposer -> DESIGN.md -> EXPERIENCE.md; no custom ChatBot design system.
- Loaded `sprint-change-proposal-2026-06-19.md`, which introduced Epic 12 and identified `chatbot.tokens.css` as the temporary Story 1.14 token-alias bridge that had grown into a forbidden parallel design system.
- Loaded previous story intelligence from `12-7-migrate-operational-and-audit-pages-to-fluent.md` and `tests/test-summary-story-12.7.md`.
- Loaded persistent project-context facts from sibling `**/project-context.md` files. Relevant cross-cutting rules: .NET 10, `.slnx`, central package management, warnings-as-errors, xUnit v3 + Shouldly, `DiffEngine_Disabled=true` for Verify, root-level submodule-only policy, no generated-output edits, no casual package upgrades, metadata-only diagnostics, and FrontComposer/Fluent-only UI rules.

### Epic 12 Context

Epic 12 closes the component-level Fluent v5 conformance gap left after Epic 10 adopted the FrontComposer shell while interior surfaces still used raw HTML and a custom CSS token layer. Stories 12.2-12.7 migrated raw interactive controls and left the raw-control backlog empty. Story 12.8 is the CSS retirement step; Story 12.9 lands last for cross-surface a11y/visual re-verification. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]

This is a rendering-layer correction only. The dev agent must not alter backend command/admission behavior, EventStore, projections, CLI, MCP, package pins, submodule contents, or the governed "no fake/freeform textbox" safety model. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

### Current State of Files to Update

- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` is currently 1,312 lines. It still carries semantic color aliases plus extensive custom typography, font, radius, button/input/control, surface, foreground, status, chip, card, forced-colors, and reduced-motion styling. The guard currently counts this file as exact primitive debt: 11 `--chatbot-type-*` aliases, 3 `--chatbot-radius-*` aliases, 5 `--chatbot-font-*` aliases, 51 heading typography declarations, 32 foreground color declarations, and 4 native control CSS selectors. [Source: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` has an empty `RawControlMigrationBacklog`, so the raw-control side of Epic 12 is complete. Its `PrimitiveMigrationBacklog` still allows the CSS debt by exact count and must be emptied in this story.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs` still validates custom spacing/radius/type/font aliases and literal font-size declarations. That is the main test that blesses the custom CSS primitive layer and must be reframed.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs` still asserts CSS selectors such as `.chatbot-status__label`, `.chatbot-chip__cue`, and `.chatbot-governed-composer__input`; update it to verify contracts without requiring deleted custom styling.
- `src/Hexalith.ChatBot.UI/Components/App.razor` still registers `css/chatbot.tokens.css`; keep the registration if the file still owns semantic aliases/layout utilities. `MainLayout.razor` is already a single `<FrontComposerShell>`.

### Likely Markup Hotspots

- `ChatBotApprovalConversationItem.razor` passes `Class="chatbot-action-button"` / `chatbot-action-button--primary` to `FluentButton`. Prefer Fluent `Appearance` without custom button styling.
- `ChatBotGovernedComposer.razor` and `ChatBotAssociationReviewActions.razor` pass custom input classes to `FluentTextArea`. Preserve ids, labels, aria, validation, focus behavior, and bindings, but do not restyle Fluent inputs through ChatBot CSS.
- `ChatBotActorBadge.razor` and `ChatBotWhyProjectPanel.razor` pass custom action classes to `FluentButton`. Prefer Fluent `Appearance`, accessible labels, and semantic data markers.
- Many pages/components still use `chatbot-page-title`, `chatbot-section-title`, `chatbot-body`, `chatbot-metadata`, and `chatbot-code`. Where those classes only provide type/color primitives, use `FluentText` parameters or semantic HTML plus inherited Fluent/FrontComposer styling. If a class remains, it must not carry forbidden primitive CSS.
- Raw `<a>` links are allowed by the guard. `ProjectWorkspace.razor` uses `chatbot-project-picker__link`; keep only minimal layout/focus styling if no Fluent/FrontComposer navigation component fits.

### Architecture and UX Guardrails

- UI may depend on ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts only. Do not reference Server, gateway internals, Dapr clients, EventStore internals, audit/idempotency/projection internals, CLI, or MCP. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Use FrontComposer or Fluent UI v5 components for UI primitives. Raw lowercase `<button>/<input>/<select>/<textarea>` remain prohibited; raw `<a>` nav links are allowed. [Source: `Hexalith.AI.Tools/hexalith-ux-instructions.md`; `_bmad-output/planning-artifacts/epics.md#UX-DR1`]
- Express typography, color, and spacing through Fluent v5 component parameters or Fluent 2 design tokens. Hand-authored CSS must not recreate Fluent-provided button styling, heading ramps, foreground roles, or use legacy v4/FAST tokens. [Source: `Hexalith.AI.Tools/hexalith-ux-instructions.md`; `_bmad-output/planning-artifacts/epics.md#UX-DR2`]
- Forced-colors and non-color status meaning must survive through icon/text labels/borders, not background fill alone. Do not remove labels/aria/data markers that make status meaning perceivable. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR4`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`]
- Page-like surfaces with two or more sibling titled content sections should use `FluentAccordion`; do not make accordion/layout changes in this story unless CSS retirement exposes a directly related defect. [Source: `Hexalith.AI.Tools/hexalith-ux-instructions.md`]

### Previous Story Intelligence

- Story 12.7 left the raw-control backlog empty and explicitly left `chatbot.tokens.css` untouched for Story 12.8. Do not reintroduce raw controls while deleting CSS.
- Story 12.7 review found a false test claim caused by browser-path issues; do not claim E2E browser coverage unless the compiled runner actually executed the real browser with zero skipped tests. If browser cannot launch, record a visible skip/limitation rather than a silent string-fixture fallback.
- Epic 12 stories use the local Fluent package pin `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; do not upgrade packages and do not chase absent/renamed Fluent APIs.
- Use PascalCase Fluent component tags so the case-sensitive governance regex does not false-match raw HTML controls.

### Git Intelligence

Recent relevant commits show the Epic 12 sequence:

- `17975d9 feat(story-12.7): Migrate operational and audit pages to Fluent`
- `5d618e3 feat(story-12.6): Migrate policy notification escalation editors to Fluent`
- `1a623e9 feat(story-12.5): Migrate approval and governed action surfaces to Fluent`
- `c3232b5 feat(story-12.4): Migrate association review surface to Fluent`
- `6336421 feat(story-12.3): Migrate conversation stream and items to Fluent`

Working-tree note at story creation: there are pre-existing modified submodule pointers (`Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Parties`, `Hexalith.Tenants`) and an unrelated story-automator orchestration artifact. Do not revert them, and do not include submodule pointer changes in Story 12.8 unless the user explicitly asks.

### Testing Standards

- xUnit v3 + Shouldly; avoid raw `Assert.*`.
- Keep `DiffEngine_Disabled=true` for Verify-backed lanes. Build with `.slnx`; never create/use `.sln`.
- VSTest may fail in this environment due socket permissions; use the compiled xUnit v3 executable fallback under `tests/.../bin/Debug/net10.0/`.
- Keep tests non-vacuous: assert files exist, raw controls are absent, CSS primitive debt is zero, semantic color mappings still point directly at Fluent tokens, and guard backlogs are empty.
- Run `git diff --check` before handoff.

### Latest Technical Information

No external version research is needed for this story because the architecture makes the local pins binding: Fluent UI v5 remains `5.0.0-rc.3-26138.1`, with no version churn in Epic 12. Use installed package behavior and existing in-repo Fluent usages as the source of truth. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj`; `Directory.Packages.props`]

### Regression Traps to Avoid

- Do not leave `PrimitiveMigrationBacklog` with zero-count or stale entries; Story 12.8 should remove the temporary backlog, not normalize a permanent exception.
- Do not keep `ChatBotSemanticTokenContractTests` asserting radius/font/type aliases after deleting them.
- Do not delete the semantic slot contract (`neutral`, `brand`, `info`, `warning`, `danger`, `success`) or change their meanings.
- Do not replace removed typography classes with raw inline styles or new CSS classes that recreate the same primitive debt under new names.
- Do not remove reachable disabled/unavailable explanations (`aria-describedby`, adjacent reason text) when deleting styling classes.
- Do not remove `role`, `aria-live`, `aria-busy`, `data-chatbot-*`, `data-compliance-*`, or stable ids that tests and assistive behavior rely on.
- Do not use raw hex/RGB/HSL colors or legacy v4/FAST tokens.
- Do not modify sibling submodules, generated `obj/**/generated/HexalithFrontComposer/**`, package pins, backend code, CLI/MCP, EventStore, or Dapr/Aspire topology.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`]
- [Source: `.agents/skills/bmad-create-story/discover-inputs.md`]
- [Source: `.agents/skills/bmad-create-story/template.md`]
- [Source: `.agents/skills/bmad-create-story/checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`]
- [Source: `Hexalith.AI.Tools/hexalith-ux-instructions.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR2`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: ChatBot UI Fluent v5 Component Conformance Remediation`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.8: Retire the chatbot.tokens.css custom design system`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`]
- [Source: `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`]
- [Source: `_bmad-output/implementation-artifacts/12-7-migrate-operational-and-audit-pages-to-fluent.md`]
- [Source: `_bmad-output/implementation-artifacts/tests/test-summary-story-12.7.md`]
- [Source: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs`]
- [Source: `src/Hexalith.ChatBot.UI/Components/App.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`]
- [Source: `Directory.Packages.props`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-21: Red phase confirmed after reframing tests. `ChatBotFluentConformanceTests` failed on `chatbot.tokens.css` primitive debt and `ChatBotSemanticTokenContractTests` failed on `--chatbot-type-*` before CSS retirement.
- 2026-06-21: Full `dotnet test Hexalith.ChatBot.slnx --no-build -m:1` attempted and blocked by VSTest socket permission errors; direct xUnit v3 executable fallback was used.
- 2026-06-21: Full browser/a11y visual re-verification intentionally left to Story 12.9; affected E2E fixture classes touched by this story were executed.
- 2026-06-21: QA automation pass added Story 12.8 E2E source/fixture coverage for retired presentation hooks and Fluent semantic replacement contracts.

### Completion Notes List

- Retired `chatbot.tokens.css` from a custom token/design-system layer to Fluent-backed semantic color aliases plus layout, overflow, focus, reduced-motion, and forced-colors hooks.
- Removed presentation-only `Class=` hooks from Fluent buttons and text areas while preserving ids, aria attributes, data markers, bindings, and Fluent `Appearance`/`Color` ownership.
- Emptied the CSS primitive migration backlog and reframed semantic/source-contract tests so they reject custom primitives instead of blessing them.
- Updated affected E2E fixture strings to stop carrying retired helper classes.
- Added Story 12.8-specific E2E regression coverage so retired presentation classes cannot return as test hooks or production UI hooks.
- Verification evidence is recorded in `_bmad-output/implementation-artifacts/tests/test-summary-story-12.8.md`.

### File List

- `_bmad-output/implementation-artifacts/12-8-retire-chatbot-tokens-css-custom-design-system.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.8.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/Story12CssRetirementE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`

### Change Log

- 2026-06-21: Retired ChatBot custom CSS primitives, removed presentation-only Fluent control class hooks, updated governance/source-contract/E2E fixture tests, and recorded Story 12.8 verification results.
- 2026-06-21: Added QA-generated Story 12.8 E2E regression tests for retired presentation hooks and updated BMAD test automation summaries.
- 2026-06-21: Senior Developer Review (AI) found and fixed two CSS-retirement regressions (`[hidden]` panels no longer hid; touch targets fell below 44px/24px), corrected the inaccurate "0 failed" E2E claims, re-verified full UI + E2E suites green, and set status to done.

## Senior Developer Review (AI)

**Reviewer:** Jerome — 2026-06-21
**Outcome:** Changes Requested → remediated in-session (auto-fix) → **Approved / done**
**Mode:** Adversarial review; every affected test lane re-run against the real Chromium browser (not the string-only fallback) and against the rebuilt solution.

### Summary

The CSS retirement itself (AC1–AC5, AC7) was implemented correctly: the stylesheet is reduced to Fluent-backed semantic color aliases plus layout/accessibility CSS, the primitive backlog is emptied, the governance/semantic/source-contract tests are honestly reframed, the build is clean, and the `rg` guards report no forbidden primitives or raw controls. However, AC6 ("no UX, accessibility, or safety semantics regress") was **violated**: two real accessibility regressions slipped through because the dev-recorded test summary claimed two browser E2E lanes had "0 failed" when each actually had one genuine failure (the exact false-claim pattern flagged in the Story 12.7 review).

### Findings

| # | Severity | AC | Status |
|---|----------|----|--------|
| 1 | CRITICAL | AC6 | Fixed |
| 2 | CRITICAL | AC6 | Fixed |
| 3 | HIGH (false verification claim) | AC7 | Fixed (doc corrected) |

**Finding 1 — `hidden`-toggled panels no longer hide (CRITICAL, fixed).**
The retirement deleted `.chatbot-why-project-panel[hidden] { display: none }` but left `.chatbot-why-project-panel` in the `display: grid` group. An author `display` declaration overrides the user-agent `[hidden] { display:none }` rule, so any panel toggled via the HTML `hidden` attribute stayed visible. This broke `ProjectConversationWhyProjectPanelShouldOpenFromEmailAndDecisionRowsAndRemainMetadataOnly` and is a production focus/UX defect for every `hidden`-toggled, display-grouped element. **Fix:** added a single user-agent reset `[hidden] { display: none !important; }` in `chatbot.tokens.css` (AC2-permitted UA reset; conformance-safe — no primitive/native-control/color pattern).

**Finding 2 — touch targets collapse below WCAG 2.5.5 minimums (CRITICAL, fixed).**
The retirement deleted `.chatbot-governed-action button` / `.chatbot-streaming-stop button` (44px primary) and `.chatbot-actor-badge__action` (24px dense) sizing because they were native-control selectors, but never re-applied a conformance-safe replacement — so the buttons rendered at ~21px. This broke `TouchTargetsShouldMeetPrimaryAndDenseMinimumsAtPhoneAndTabletWidths` and degraded real touch affordances. **Fix:** applied the already-defined `.chatbot-touch-target-primary` / `.chatbot-touch-target-dense-secondary` utility classes (layout-only hooks, AC3-permitted) to the governed-action, streaming-stop, and actor-badge buttons in production (`ChatBotGovernedAction.razor`, `ChatBotStreamingStopControl.razor`, `ChatBotActorBadge.razor`) and in the affected E2E fixtures.

**Finding 3 — inaccurate verification evidence (HIGH, doc corrected).**
`test-summary-story-12.8.md` recorded `ProjectConversationE2ETests` and `GovernedOperationsVisualFoundationE2ETests` as "0 failed" when each had one real failure. Corrected the summary and added a "Senior Review Remediation" section documenting the regressions, fixes, and re-verification.

### Re-verification (post-fix)

- Build: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` → 0 warnings, 0 errors.
- `Hexalith.ChatBot.UI.Tests` (full): 170 passed, 0 failed, 0 skipped.
- `Hexalith.ChatBot.UI.E2E.Tests` (full suite, real Chromium): 130 passed, 0 failed, 0 skipped.
- Guard/semantic/primitive lanes (6 / 8 / 7) green; `rg` forbidden-primitive + raw-control checks empty; `git diff --check` clean.

### Notes / non-blocking observations

- `--chatbot-color-neutral-stroke-strong` aliases to `--colorNeutralStroke1`, the same Fluent token as `--chatbot-color-neutral-stroke`; a "strong" stroke arguably wants a distinct Fluent token. It is a direct Fluent mapping (passes contract) and visually out of scope here — left as-is. (LOW)
- The pre-existing submodule pointer changes and the story-automator orchestration artifact remain untouched per the story's own scope guard.
