---
baseline_commit: 0fcdf27
---

# Story 12.2: Migrate governed chat composer to Fluent v5

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-21. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a user,
I want the composer rendered with Fluent v5 components,
so that it looks and behaves like the rest of Microsoft Fluent V2.

## Acceptance Criteria

1. **Composer raw controls are replaced by Fluent v5 components.** Given `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`, when the story is complete, then its three raw lowercase `<button>` controls render as `FluentButton`, its raw `<textarea>` renders as `FluentTextArea`, and its raw `<label>` renders as `FluentLabel`. The component must contain no raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` tags, and it must not use `.chatbot-button` / `.chatbot-button-primary`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.2`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`]

2. **Existing composer behavior and accessibility contracts are preserved.** Given the migrated component, when a user changes mode, types text, submits, hits validation, sees disabled/degraded/unauthorized state, or receives an accepted pending submission, then existing semantics remain: mode buttons keep `aria-pressed`, the text-entry control keeps the stable id `project-conversation-composer-input`, `aria-describedby`, `aria-invalid`, disabled state, placeholder/localized text, and keydown stop-propagation for UX-DR34; validation still renders `role="alert"` with `tabindex="-1"` and receives focus once per distinct validation error; accepted pending submissions still return focus once per distinct command id through a compile-valid Fluent component focus path. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`; `_bmad-output/planning-artifacts/epics.md#UX-DR34`; `_bmad-output/planning-artifacts/epics.md#UX-DR35`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`]

3. **The Story 12.1 guard backlog shrinks by exactly this file.** Given `ChatBotFluentConformanceTests`, when the Governance lane runs, then `Components/Governed/ChatBotGovernedComposer.razor` is removed from `RawControlMigrationBacklog`, no stale backlog entry remains, no new raw-control offender is introduced, and the remaining raw-control backlog still contains the other migration stories' files only. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md#Acceptance Criteria`; `_bmad-output/planning-artifacts/epics.md#Story 12.2`]

4. **Tests and snapshots are updated intentionally without weakening the guard intent.** Given the focused UI tests and any Verify/bUnit baselines affected by the composer markup, when updated, then expectations assert Fluent components and raw-tag absence accurately. Tests must not use case-insensitive substring checks that make `FluentTextArea` fail as if it were raw `textarea`; use raw-tag-aware checks equivalent to the governance regex where needed. [Source: `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]

5. **Scope remains a rendering-layer migration only.** Given Epic 12 constraints, when the story is complete, then there are no package upgrades, no Fluent version churn, no backend/CommandGateway/CLI/MCP/SignalR behavior changes, no edits to sibling submodules, no `chatbot.tokens.css` retirement beyond removing composer usage of Fluent-provided primitive classes, and no migration of surfaces owned by Stories 12.3-12.8. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `Hexalith.AI.Tools/hexalith-llm-instructions.md`; `Hexalith.AI.Tools/hexalith-ux-instructions.md`]

## Tasks / Subtasks

- [x] Migrate `ChatBotGovernedComposer` markup to Fluent components (AC: 1, 2)
  - [x] Replace the two mode `<button>` elements with `FluentButton` components, preserving `type="button"` behavior, `aria-pressed`, disabled state, localized labels, and `OnModeChanged` callbacks.
  - [x] Replace the submit `<button>` with `FluentButton`, using a Fluent primary appearance for the submit action and preserving disabled state, `SubmitAsync`, and submitting/submitted localized text.
  - [x] Replace the raw `<label>` with `FluentLabel`, preserving its association with `project-conversation-composer-input`.
  - [x] Replace the raw `<textarea>` with `FluentTextArea`, preserving id, `@bind`/input update behavior, placeholder, `aria-describedby`, `aria-invalid`, disabled state, and keydown stop-propagation.
  - [x] Remove composer usage of `.chatbot-button` and `.chatbot-button-primary`; keep only layout classes that the design system does not own.

- [x] Preserve focus, shortcut, validation, and governed-safety behavior (AC: 2)
  - [x] Keep `OnAfterRenderAsync` focus-once-per-distinct-state logic intact.
  - [x] Update the input focus reference to the correct Fluent v5 shape: a `FluentTextArea` component `@ref` exposes an `Element` reference; the existing raw `ElementReference _input` pattern cannot be assumed to compile unchanged after the tag migration.
  - [x] Keep the validation summary id `project-conversation-composer-error`, `role="alert"`, `tabindex="-1"`, and `_validationSummary` focus reference intact.
  - [x] Keep UX-DR34 shortcut suppression inside the text-entry control; do not move shortcut handling to a global ungoverned path.
  - [x] Keep the no-fake/freeform-textbox safety model: submission still flows through the existing `OnSubmit` callback and Project Conversation state/effects; do not add direct backend, Dapr, EventStore, CLI, MCP, or SignalR calls.

- [x] Burn down exactly the composer guard backlog entry (AC: 3)
  - [x] Remove `Components/Governed/ChatBotGovernedComposer.razor` from `RawControlMigrationBacklog` in `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`.
  - [x] Do not add a replacement allowlist entry or carve-out.
  - [x] Verify the remaining allowlist entries are untouched and still belong to Stories 12.3-12.7.

- [x] Update focused tests and snapshots (AC: 2, 4)
  - [x] Update `ChatBotAccessibilityFocusContractTests` composer expectations to include the Fluent component tags and still prove focus/shortcut floor markers.
  - [x] Update `ProjectWorkspaceRouteContractTests` and `ProjectWorkspaceE2ETests` raw-textarea assertions so they do not fail merely because `FluentTextArea` contains the substring `textarea`.
  - [x] Update any bUnit/Verify snapshots or component contract expectations affected by the generated Fluent markup intentionally.
  - [x] Keep E2E fixture-only raw textarea usages in unrelated tests out of scope unless a focused assertion must change for this story.

- [x] Verify the narrow migration (AC: all)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` or restore first if needed.
  - [x] Run `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false`.
  - [x] Run the focused UI test project or compiled xUnit v3 executable if VSTest socket creation is denied in the sandbox.
  - [x] Run any affected E2E/visual/snapshot lane needed by the changed expectations, using fixture-only fallback if browser prerequisites are unavailable.
  - [x] Run `git diff --check`.
  - [x] Add or update `_bmad-output/implementation-artifacts/tests/test-summary-story-12.2.md` with exact commands and results.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: user `Jerome`, project `chatbot`, planning artifacts under `_bmad-output/planning-artifacts`, implementation artifacts under `_bmad-output/implementation-artifacts`, output language English.
- Loaded sprint status from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key `12-2-migrate-governed-chat-composer-to-fluent` was `backlog`; `epic-12` was already `in-progress`; Story 12.1 was `done`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`. Epic 12 and Stories 12.1-12.9 are present as the approved Fluent v5 component-conformance remediation epic.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`. Relevant section is `Frontend Architecture`, especially ChatBot UI Fluent-only conformance and the governed chat surface.
- No first-level `*prd*.md` or `*ux*.md` matched the create-story discovery table. UX requirements relevant to this story are embedded in `epics.md` UX-DR1, UX-DR2, UX-DR34, and UX-DR35; architecture input lists the original UX design docs as source material.
- Loaded persistent project-context facts from eight sibling `**/project-context.md` files. Relevant facts: .NET 10, warnings-as-errors, central package versions, `.slnx`, xUnit v3 + Shouldly, no package versions in `.csproj`, root-level submodules only, no generated-output edits, metadata-only diagnostics, and FrontComposer Fluent-only governance.

### Epic 12 Context

Epic 12 closes the component-level Fluent v5 gap left after Epic 10 adopted the FrontComposer Shell but allowed interior surfaces to remain raw HTML over `chatbot.tokens.css`. The accepted remediation goal is that every `Hexalith.ChatBot.UI` `.razor` page/component renders through FrontComposer or Fluent UI v5 components, with no raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` except temporary migration backlog entries that must shrink to empty by Epic 12 completion. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]

Story 12.2 is the first post-guard migration story. It owns only `ChatBotGovernedComposer`; conversation stream/items are Story 12.3, association review is Story 12.4, approval/action surfaces are Story 12.5, policy/config editors are Story 12.6, operational/audit pages are Story 12.7, stylesheet retirement is Story 12.8, and cross-surface reverification is Story 12.9. [Source: `_bmad-output/planning-artifacts/epics.md#Stories 12.2-12.9`]

### Current Implementation State

`src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor` currently renders:

- A section with `class="chatbot-governed-composer composer-action-entry"` and `aria-labelledby="project-conversation-composer-title"`.
- A title `<h2 id="project-conversation-composer-title" class="chatbot-section-title">`.
- Optional `ChatBotStatusBanner` for disabled/submitting/pending states.
- Optional validation summary `<div id="project-conversation-composer-error" class="chatbot-validation-summary" role="alert" tabindex="-1" @ref="_validationSummary">`.
- A mode group with two raw `<button class="chatbot-button">` controls for message and ask-AI mode; each has `aria-pressed`, `disabled`, and `OnModeChanged`.
- A raw `<label class="chatbot-labelled-row" for="project-conversation-composer-input">`.
- A raw `<textarea id="project-conversation-composer-input" class="chatbot-governed-composer__input">` with `@bind:event="oninput"`, placeholder, `aria-describedby`, `aria-invalid`, `disabled`, keydown stop-propagation, and `_input` focus reference.
- A raw submit `<button class="chatbot-button chatbot-button-primary">` that calls `SubmitAsync`.

The code-behind state is small and should be preserved: `Text`, `ValidationMessage`, `StatusMessage`, `StatusCode`, `StatusKind`, `StatusFamily`, `OnAfterRenderAsync`, `SubmitAsync`, and `SuppressComposerShortcutAsync`. The focus logic intentionally focuses the validation summary once per distinct validation error and the input once per distinct accepted command id; do not simplify it into first-render-only or every-render focus. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`]

### Existing Guard and Test State

Story 12.1 added `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` with `[Trait("Category", "Governance")]`. Its `RawInteractiveControl` regex is case-sensitive and matches raw lowercase controls only: `"<(button|input|select|textarea)(\\s|/|>)"`. `RawControlMigrationBacklog` currently includes `Components/Governed/ChatBotGovernedComposer.razor`; Story 12.2 must delete that one entry after the component no longer contains raw controls. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md#Senior Developer Review`]

Tests likely affected:

- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs` has a "Governed composer focus and shortcut floor" source-contract row for `ChatBotGovernedComposer.razor`.
- `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs` currently asserts `workspace.ShouldNotContain("textarea", Case.Insensitive)`. That assertion will falsely fail on `FluentTextArea`; replace it with a raw-tag-aware assertion.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs` has fixture assertions using `ShouldNotContain("textarea", Case.Insensitive)` and a browser assertion `Locator("textarea").CountAsync() == 0`. Keep the browser assertion if the rendered DOM still has no raw exposed textarea; update fixture/string assertions so they do not block `FluentTextArea`.

### Fluent v5 Component Notes

The local repo pin is binding. `Directory.Packages.props` pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, and `_Imports.razor` already imports `Microsoft.FluentUI.AspNetCore.Components`; do not add package references or version attributes. [Source: `Directory.Packages.props`; `src/Hexalith.ChatBot.UI/Components/_Imports.razor`; `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj`]

The pinned package's net10.0 XML docs confirm:

- `FluentButton` exposes `Type`, `Disabled`, `DisabledFocusable`, `Appearance`, `Loading`, `Title`, `Label`, and `StopPropagation`; `ButtonAppearance.Primary` emphasizes a primary action, and `ButtonAppearance.Outline`/`Default` are available for lower-emphasis mode buttons. [Source: local NuGet XML `microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml`]
- `FluentTextArea` is the multiline text-entry component and exposes `Placeholder`, `AutoResize`, `Size`, `Width`, `Height`, `Resize`, `Spellcheck`, and `ChangeAfterKeyPress`; keep the existing binding semantics rather than introducing a custom JS path. [Source: same local NuGet XML]
- `FluentLabel` is the label component for an input component and exposes `ChildContent`, `Disabled`, `Size`, `Weight`, and `Tooltip`. [Source: same local NuGet XML]
- `FluentTextArea` implements the Fluent component element-base contract and exposes an `Element` property for the rendered element. Do not keep `@ref` typed as raw `ElementReference` on the component tag if that fails to compile; use a `FluentTextArea` component reference plus its rendered element, or another locally proven Fluent focus path. [Source: same local NuGet XML]

### Architecture and UX Guardrails

- The UI adapter may reference ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts. It must not reference Server, Gateway internals, Dapr, EventStore internals, audit, idempotency, projection internals, CLI, or MCP. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- UX-DR1 requires component-level, build-enforced visual inheritance: ChatBot `.razor` components use FrontComposer or Fluent UI v5 components; raw lowercase interactive controls fail the build; raw `<a>` links are allowed. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`]
- UX-DR2 bans hand-authored recreation of Fluent-provided primitives and legacy v4/FAST tokens. This story should remove composer dependency on `.chatbot-button` classes, but it must not retire `chatbot.tokens.css` wholesale; that belongs to Story 12.8. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR2`; `_bmad-output/planning-artifacts/epics.md#Story 12.8`]
- UX-DR34 requires single-character/modifier-free shortcuts disabled by default inside text-entry controls. The existing `@onkeydown:stopPropagation="true"` and `SuppressComposerShortcutAsync` marker exist for this floor; preserve the intent when moving to `FluentTextArea`. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR34`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`]
- UX-DR35 requires validation errors to show an error summary before the panel with `aria-invalid` + `aria-describedby` and focus to summary. Preserve the existing validation summary and described-by relationship. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR35`]

### File Structure Requirements

Likely implementation locations:

- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs` if fixture/source assertions are affected
- Verify snapshot files under the existing UI test layout, if the changed component has snapshots
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.2.md`

Avoid these locations:

- Do not edit `Hexalith.FrontComposer`, `Hexalith.EventStore`, `Hexalith.Tenants`, or any sibling submodule.
- Do not edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Do not move the composer into a new subsystem or create new backend endpoints.
- Do not modify package pins in `Directory.Packages.props` or add inline package versions.

### Testing Standards

- Use xUnit v3 and Shouldly; avoid raw `Assert.*`.
- Keep `DiffEngine_Disabled=true` when running Verify-backed tests.
- Prefer focused test updates that prove raw-tag absence and Fluent component presence. Do not weaken tests to substring-only "no textarea" checks that would also reject `FluentTextArea`.
- Run the Governance trait first because it directly proves the allowlist burn-down, then run the affected UI test project and any affected E2E/fixture lane.
- If `dotnet test` fails before test execution because the sandbox denies VSTest socket creation, build the test project and run the compiled xUnit v3 executable from `bin/<Configuration>/net10.0`, recording that limitation honestly in the test summary.

### Previous Story Intelligence

Story 12.1 is done and added the guard that makes this story measurable. It also documented these lessons:

- The raw-control backlog is shrink-only; stale entries fail intentionally.
- The guard is case-sensitive so PascalCase Fluent tags do not count as raw controls.
- The guard reports offenders by relative path and distinct raw tag names.
- The CSS primitive backlog remains temporary migration debt for Story 12.8; component migration stories should avoid adding new primitive CSS.
- VSTest socket creation may fail in this sandbox; compiled xUnit v3 executable fallback was used successfully for Story 12.1.
- A separate cross-tenant conformance failure was observed during Story 12.1 solution fallback runs and documented as unrelated to UI governance. Do not claim this story caused or fixed that backend issue. [Source: `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.1.md`]

### Git Intelligence

Recent commit titles:

- `0fcdf27 feat(story-12.1): Fluent-only governance guard`
- `75d29a5 chore: Update lastUpdated timestamps and story statuses in sprint artifacts`
- `245075e chore: sync Epic 10 retrospective follow-up`
- `75b07eb feat(story-10.6b): Streaming AI response and Stop/Cancel`
- `23615f8 chore: Update subproject commit reference and lastUpdated timestamp in orchestration document`

The relevant pattern is that Epic 12 is now enforced by code, not prose. Story 12.2 must make the guard fail if the composer migration is incomplete and pass only when the composer backlog entry is deleted because the raw controls are gone.

### Regression Traps to Avoid

- Do not leave `Components/Governed/ChatBotGovernedComposer.razor` in `RawControlMigrationBacklog`; the stale-entry assertion should force deletion.
- Do not replace raw controls with custom wrapper components unless they are already FrontComposer/Fluent components or a clearly justified local component that itself renders Fluent primitives.
- Do not keep `.chatbot-button` on migrated buttons; that is a Fluent-provided primitive class and belongs to the Story 12.8 retirement path.
- Do not remove focus refs or change focus behavior just because the component tag changes.
- Do not lose `aria-pressed`; `FluentButton` must receive it as an additional attribute if there is no first-class selected/toggle parameter used locally.
- Do not convert the validation summary into a transient toast or hide it inside a Fluent component that breaks `role="alert"` / `tabindex="-1"` focus.
- Do not introduce a case-insensitive `textarea` substring assertion in tests; it will reject the required `FluentTextArea` tag.
- Do not run or advise recursive submodule initialization.

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
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR34`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR35`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: ChatBot UI Fluent v5 Component Conformance Remediation`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.2: Migrate governed chat composer -> Fluent v5`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]
- [Source: `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`]
- [Source: `_bmad-output/implementation-artifacts/tests/test-summary-story-12.1.md`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/_Imports.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs`]
- [Source: `Directory.Packages.props`]
- [Source: local NuGet package docs for `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`]
- [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`]
- [Source: `Hexalith.EventStore/_bmad-output/project-context.md`]
- [Source: `Hexalith.Conversations/_bmad-output/project-context.md`]
- [Source: `Hexalith.Projects/_bmad-output/project-context.md`]
- [Source: `Hexalith.Folders/_bmad-output/project-context.md`]
- [Source: `Hexalith.Parties/_bmad-output/project-context.md`]
- [Source: `Hexalith.Tenants/_bmad-output/project-context.md`]
- [Source: `Hexalith.Memories/_bmad-output/project-context.md`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.2.md`

### Completion Notes List

- Migrated `ChatBotGovernedComposer` mode, submit, label, and multiline text-entry controls to Fluent v5 components.
- Preserved composer state flow through `OnSubmit`, localized labels/messages, validation summary semantics, disabled/degraded/unauthorized status rendering, `aria-pressed`, `aria-describedby`, `aria-invalid`, and text-entry stop-propagation.
- Updated pending-submission focus return to use the Fluent `FluentTextArea.Element.FocusAsync()` path.
- Removed only the composer raw-control backlog entry; the remaining raw-control backlog stays scoped to later Epic 12 migration stories.
- Updated focused source/E2E contract tests to assert Fluent tags and raw-tag-aware textarea checks.
- Verified with solution build, governance, full UI tests, affected E2E methods, and broad non-integration xUnit fallback. The conformance suite still has the known unrelated cross-tenant failure documented by Story 12.1.

### File List

- `_bmad-output/implementation-artifacts/12-2-migrate-governed-chat-composer-to-fluent.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.2.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`

### Change Log

- 2026-06-21: Migrated governed chat composer to Fluent v5 controls, removed its raw-control guard backlog entry, updated focused UI/E2E contracts, and recorded validation results.
- 2026-06-21: Senior Developer Review (AI) — auto-fix applied. Removed dead `SuppressComposerShortcutAsync` method left over from the textarea→FluentTextArea migration; the `@onkeydown:stopPropagation="true"` directive is the single correct UX-DR34 mechanism on a Fluent component. Re-verified build + governance + full UI + focused/affected E2E. Status moved to done.

## Senior Developer Review (AI)

**Reviewer:** Jerome — 2026-06-21
**Outcome:** Approve (1 Medium auto-fixed, 0 Critical, 0 High remaining)

### Scope verified

- AC1 (raw controls replaced): PASS. Source contains no raw `<button>`/`<input>`/`<select>`/`<textarea>` and no `.chatbot-button`/`.chatbot-button-primary`; mode + submit buttons are `FluentButton`, label is `FluentLabel`, text entry is `FluentTextArea`.
- AC2 (behavior/a11y preserved): PASS. Stable id `project-conversation-composer-input`, `aria-describedby`, `aria-invalid`, `aria-pressed`, disabled state, placeholder/localized text preserved; validation summary keeps `role="alert"` + `tabindex="-1"`; focus-once-per-distinct-state logic intact; pending-focus uses a compile-valid `FluentTextArea.Element.FocusAsync()` path.
- AC3 (guard backlog shrinks by exactly this file): PASS. `Components/Governed/ChatBotGovernedComposer.razor` removed from `RawControlMigrationBacklog`; no carve-out added; other stories' entries untouched. Governance trait: 6 passed.
- AC4 (tests updated without weakening guard): PASS. Case-insensitive `textarea` substring checks replaced with raw-tag-aware regex (`<textarea(\s|/|>)`) in `ProjectWorkspaceRouteContractTests` and `ProjectWorkspaceE2ETests`; source/E2E contracts now assert Fluent tags and case-sensitive raw-tag absence (`FluentTextArea` no longer false-fails).
- AC5 (rendering-layer only): PASS. Only the composer `.razor` and focused tests changed; no package pins, backend, gateway, CLI, MCP, SignalR, or submodule edits.

### Findings

- **[Medium][FIXED] Dead code in a governance-sensitive component.** The textarea→`FluentTextArea` migration dropped the `@onkeydown="SuppressComposerShortcutAsync"` handler but left the now-unreferenced `SuppressComposerShortcutAsync` method (verified zero references repo-wide). Removed it. Confirmed the `@onkeydown:stopPropagation="true"` directive is the correct and sufficient UX-DR34 mechanism on a Fluent component: on a component the directive compiles to an `onkeydown` parameter that `FluentTextArea` splats to the rendered element, and re-adding an explicit handler fails to compile with `RZ10010` (duplicate `onkeydown` parameter) — proving the modifier alone is the single intended path here.
- **[Low][Noted] E2E fixture/component fidelity.** The hand-authored E2E fixture renders the submit button as `<fluent-button ... type="submit">` while the real component uses `ButtonType.Button` (renders `type="button"`). Fixture-only; no app impact. Left as-is to avoid churn in passing assertions.
- **[Low][Noted] FluentLabel `for=` association.** Real-render association depends on `FluentLabel` emitting a native `<label for>`; the fixture hedges with `aria-labelledby`. Not directly verifiable under the project's fixture-based (no-bUnit) E2E strategy. Pre-existing strategy limitation, not introduced by this story.

### Verification commands (re-run during review)

- `dotnet build src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj` — Passed, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj` — Passed, 0 warnings, 0 errors.
- `Hexalith.ChatBot.UI.Tests -trait "Category=Governance"` — 6 passed, 0 failed.
- `Hexalith.ChatBot.UI.Tests` (full) — 167 passed, 0 failed.
- `Hexalith.ChatBot.UI.E2E.Tests -method ...ProjectConversationGovernedComposerShouldExposeSubmissionStatesAndSuppressTextEntryShortcuts` — 1 passed (real Chromium path executed; ~1.4s, browser-only `ClickAsync`/`Locator(...).CountAsync()` assertions ran).
- `Hexalith.ChatBot.UI.E2E.Tests -method ...ProjectWorkspace*` (2 affected) — 2 passed, 0 failed.
- `git diff --check` — clean.
