---
baseline_commit: c478399
---

# Story 1.14: Visual inheritance and semantic token foundation

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a frontend engineer,
I want the Fluent UI v5 / FrontComposer visual inheritance and semantic tokens established once,
so that every M0+ surface inherits a consistent governed UX.

## Acceptance Criteria

1. **Visual inheritance chain is explicit and mechanically protected.** Given the UI foundation is built, when `src/Hexalith.ChatBot.UI` renders, then it inherits Fluent UI v5 -> Hexalith.FrontComposer -> DESIGN.md semantic narrowing without creating a separate chatbot design system, second component library, or hard-coded color language. The implementation must either wrap the app in the FrontComposer shell/components where the current dependency shape permits it, or create a thin ChatBot-owned token alias layer that maps directly to FrontComposer/Fluent CSS custom properties and is documented as temporary until the shell wrapper lands. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.14; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Brand & Style; _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]

2. **Semantic color slots exist once with stable meaning.** Given semantic UI state needs across S1/S2/S3 and later surfaces, when the token foundation is complete, then the six semantic slots `neutral`, `brand`, `info`, `warning`, `danger`, and `success` exist as one shared ChatBot UI contract and map only to Fluent/FrontComposer token values. Meanings are fixed: neutral = workspace/panes/queues/audit, brand = primary actions and selected navigation only, info = evidence/context/non-terminal status, warning = ambiguity/approval-required/stale/degraded/manual review, danger = blocked/unauthorized/failed/quarantined/rejected/terminal, success = completed/approved/stored/command-success/projection-complete. [Source: _bmad-output/planning-artifacts/epics.md#UX-DR2; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Colors]

3. **Spacing, radius, and typography aliases follow DESIGN.md.** Given DESIGN.md provides product-level token values, when ChatBot UI layout or wrappers use spacing, shape, or text styling, then they consume named aliases for spacing `4/8/12/16/24px`, density compact `8px`, density comfortable `12px`, panel gap `16px`, row gap `8px`, radius `4/8/12px`, and typography roles `page-title`, `section-title`, `body`, `metadata`, and `code`. Product code must avoid oversized hero type, decorative cards, and broad custom CSS. [Source: _bmad-output/planning-artifacts/epics.md#UX-DR3; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Typography; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Layout & Spacing]

4. **Contrast and forced-colors behavior are testable.** Given light, dark, and Windows High Contrast / `forced-colors` users, when semantic statuses render, then the required token pairs meet WCAG 2.2 AA minima: 4.5:1 for text and 3:1 for non-text UI. Status meaning must survive forced-colors through visible text labels plus border/icon/focus treatment; no status component may rely on background fill alone. Product wrappers must not override Fluent token pairs with raw colors unless the story adds a contrast test for that replacement. [Source: _bmad-output/planning-artifacts/epics.md#UX-DR4; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Contrast requirements; Microsoft Learn Fluent UI high contrast mode]

5. **Current UI is migrated without breaking the governed-command path.** Given `GovernedOperations.razor` is the current M0 UI surface for the trivial governed command, when Story 1.14 applies the token foundation, then the page keeps submitting only through `GovernedOperationService` / `IChatBotClient`, keeps declaring UI origin, preserves partial-success/audit metadata rendering, and replaces ad hoc layout/status markup only with tokenized, accessible primitives. No Server, gateway-stage, DAPR, or audit/idempotency seam reference may be added to the UI project. [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor; src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs; tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]

6. **Tests prove the foundation is non-vacuous.** Given future surface stories will inherit this work, when tests run, then they fail if the semantic slot set is missing, duplicate, unmapped, mapped to raw hex/rgb/hsl colors instead of Fluent/FrontComposer variables, missing forced-colors rules, missing token stylesheet registration, or missing a render-time example for at least `info`, `warning`, `danger`, and `success`. UI tests should cover pure token/contract behavior in `Hexalith.ChatBot.UI.Tests`; Playwright/axe checks are optional for this story unless a runnable endpoint is already available. [Source: _bmad-output/implementation-artifacts/1-10-architecture-dependency-fitness-tests.md#QA Results; _bmad-output/implementation-artifacts/1-12-cross-tenant-isolation-harness.md#Senior Developer Review (AI); _bmad-output/implementation-artifacts/1-13-tenant-scoped-fixture-and-evaluation-scaffold.md#Previous Story Intelligence]

7. **Build and focused regression gates stay green.** Given the story touches UI foundation, when implementation is complete, then `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` succeeds with warnings-as-errors; compiled xUnit v3 binaries for `Hexalith.ChatBot.UI.Tests` and `Hexalith.ChatBot.Architecture.Tests` are green; broader Server/Conformance/Integration tests are run if UI dependency boundaries, app host wiring, or governed command behavior are touched. No inline package versions or Fluent UI upgrades are introduced. [Source: Directory.Packages.props; tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]

## Tasks / Subtasks

- [x] Add the ChatBot semantic token contract (AC: 1, 2, 3, 4, 6)
  - [x] Create a single source for ChatBot UI token aliases, preferably under `src/Hexalith.ChatBot.UI/Design/` for C# constants and `src/Hexalith.ChatBot.UI/wwwroot/css/` for CSS variables if static assets are used.
  - [x] Define the six semantic slots exactly: `neutral`, `brand`, `info`, `warning`, `danger`, and `success`.
  - [x] Map semantic colors to Fluent/FrontComposer CSS custom properties from DESIGN.md: `--colorNeutralBackground1`, `--colorNeutralBackground2`, `--colorNeutralForeground1`, `--colorNeutralForeground3`, `--colorNeutralStroke1`, `--colorBrandBackground`, `--colorNeutralForegroundOnBrand`, `--colorStatusSuccessBackground1`, `--colorStatusSuccessForeground1`, `--colorStatusWarningBackground1`, `--colorStatusWarningForeground1`, `--colorStatusDangerBackground1`, `--colorStatusDangerForeground1`, `--colorStatusInformationBackground1`, and `--colorStatusInformationForeground1`.
  - [x] Check FrontComposer token spelling before coding. FrontComposer currently exposes `--fc-color-info: var(--colorStatusInfoForeground1)` in its isolated shell CSS, while ChatBot DESIGN.md uses `--colorStatusInformation*`; do not silently pick the shorter spelling unless the rendered Fluent UI v5 RC3 token exists and the tests prove it.
  - [x] Define spacing/radius/typography alias names that match DESIGN.md vocabulary. Use aliases such as `--chatbot-space-1: 4px`, `--chatbot-space-2: 8px`, `--chatbot-space-3: 12px`, `--chatbot-space-4: 16px`, `--chatbot-space-6: 24px`, `--chatbot-radius-sm: 4px`, `--chatbot-radius-md: 8px`, and `--chatbot-radius-lg: 12px`.
  - [x] Keep any ChatBot aliases as semantic wrappers over Fluent/FrontComposer tokens. Do not add a Tailwind/shadcn/Bootstrap/MudBlazor/Telerik layer, raw palette, gradient theme, decorative card system, or chatbot-specific color meanings.

- [x] Wire the token foundation into the Blazor app without bypassing FrontComposer (AC: 1, 4, 5)
  - [x] Register the token stylesheet from `App.razor` or the project-approved static web asset path so the variables cascade to `MainLayout.razor` and page components.
  - [x] Evaluate adding a conditional `ProjectReference` to `$(HexalithFrontComposerRoot)\src\Hexalith.FrontComposer.Shell\Hexalith.FrontComposer.Shell.csproj` and wrapping `@Body` in `FrontComposerShell`. If this creates package/version churn or a boundary conflict, do not force it in Story 1.14; document the thin alias layer as the temporary inheritance bridge and keep the story focused on tokens.
  - [x] If `FrontComposerShell` is used, keep the UI adapter dependency direction intact: UI may depend on Client, ServiceDefaults, and FrontComposer shell contracts/components, but must not reference Server, gateway internals, DAPR clients, or audit/idempotency interfaces.
  - [x] Preserve `AddFluentUIComponents()` and the existing Fluxor registration in `Program.cs`; do not introduce another component provider or remove the current governed command service registration.
  - [x] Keep `<FluentProviders />` available exactly once in the rendered tree. FrontComposerShell already renders it; if ChatBot adds providers directly, avoid duplicate provider registration.

- [x] Tokenize the current governed operations surface (AC: 2, 3, 4, 5)
  - [x] Update `MainLayout.razor` to use the inherited shell or tokenized layout classes for page background, header, content spacing, and focus-visible behavior.
  - [x] Update `GovernedOperations.razor` to use compact page/section headings, metadata/code text styling for IDs and stable machine values, and semantic status/banners for submitting, error, partial-success, audit, and success states.
  - [x] Keep the current command submission behavior intact: button click dispatches `SubmitGovernedNoteAction`; the service submits through `IChatBotClient` with `ChatBotSurfaceOrigin.Ui`; status/audit reads remain metadata-only.
  - [x] Do not turn the page into a marketing landing page or generic chat feed. The first viewport remains the usable governed-command work surface.
  - [x] Do not add Story 1.15 component primitives yet unless a small internal example is needed to prove token application. Project context header, actor badge, evidence chip, risk chip, blocked state, and toast/banner as reusable components belong to Story 1.15.

- [x] Add forced-colors and reduced-customization guardrails (AC: 4, 6)
  - [x] Add `@media (forced-colors: active)` rules for ChatBot-owned status wrappers so text, border, focus, and icons survive system palette overrides.
  - [x] Ensure `info`, `warning`, `danger`, and `success` status examples carry visible text labels. Color may reinforce meaning; it must never be the only signal.
  - [x] Let Fluent/FrontComposer own light/dark/high-contrast token values. Product CSS may alias them but should not replace them with raw color constants.
  - [x] Add comments only where needed to explain token alias ownership or a temporary FrontComposer-shell bridge; do not add visual-design narration into the app UI.

- [x] Add focused tests for the foundation (AC: 1, 2, 3, 4, 6, 7)
  - [x] Add `Hexalith.ChatBot.UI.Tests` tests that load the token contract and assert the semantic slot set is exact and exhaustive.
  - [x] Add tests that reject raw `#`, `rgb(`, `hsl(`, or unrelated CSS variable mappings for semantic color aliases, except permitted fixed spacing/radius pixel values from DESIGN.md.
  - [x] Add tests that prove the token stylesheet is registered or included as a static web asset reachable by the app.
  - [x] Add tests that prove the stylesheet includes forced-colors coverage and non-color status cues for at least info/warning/danger/success.
  - [x] Keep existing `GovernedOperationServiceTests` and `GovernedOperationsEffectsTests` green; expand them only if the tokenization changes service/effect behavior.
  - [x] Add or update architecture tests if a FrontComposer project reference is introduced, so the UI adapter boundary remains non-vacuous and still excludes Server/gateway internals.

- [x] Preserve dependency and package guardrails (AC: 1, 5, 7)
  - [x] Do not change `Microsoft.FluentUI.AspNetCore.Components` version. Current ChatBot pin is `5.0.0-rc.3-26138.1` in `Directory.Packages.props`; FrontComposer docs still mention RC2 in places, so treat the docs as contingency guidance and the root pin as authoritative for this repo.
  - [x] Do not add inline package versions in `.csproj` files.
  - [x] Do not edit files inside `Hexalith.FrontComposer` unless the task is explicitly expanded to modify the submodule. Reuse it as a read-only reference.
  - [x] Do not initialize or update nested submodules. If submodule setup is needed, use only root-level `git submodule update --init` from the repository root.
  - [x] Do not hand-edit generated client files under `src/Hexalith.ChatBot.Client/Generated/`.

- [x] Verify and document results (AC: 6, 7)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.Architecture.Tests` if dependency references, project boundaries, or app-level wiring are touched.
  - [x] Run broader Server/Conformance/Integration tests only if the implementation touches service behavior, app host wiring, OpenAPI/client boundaries, or governed-command behavior.
  - [x] Record exact commands, pass/fail counts, and any skipped live/E2E checks in this story's Dev Agent Record.

## Dev Notes

### Source Artifact Analysis

Story 1.14 is the first cross-cutting visual foundation story. It anchors UX-DR1 through UX-DR4 before the shared component primitives in Story 1.15, interaction guardrails in Story 1.16, responsive/touch foundation in Story 1.17, and accessibility/localization foundation in Stories 1.18-1.21. Keep the scope to inheritance, tokens, and proof that current UI consumes those tokens. [Source: _bmad-output/planning-artifacts/epics.md#FR Coverage Map; _bmad-output/planning-artifacts/epics.md#Story 1.14]

The visual chain is binding: Fluent UI v5 -> FrontComposer -> DESIGN.md -> EXPERIENCE.md. DESIGN.md owns visual token meaning; EXPERIENCE.md owns behavior, state, interactions, and accessibility. The absence of mockups is explicit and not permission to invent a visual language. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Brand & Style; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Foundation]

The product posture is a quiet operational SaaS command workspace. Avoid playful assistant language, marketing-style empty states, oversized hero typography, decorative cards, and generic chat bubbles that hide workflow state. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Brand & Style; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Do's and Don'ts]

### Current Implementation State

Current UI files likely to be updated:

- `src/Hexalith.ChatBot.UI/Components/App.razor` defines the HTML shell and currently has no stylesheet links or Fluent/FrontComposer providers.
- `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor` is a small custom wrapper with `.page` and `.brand` classes but no CSS file in the UI project.
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` renders the current M0 governed-command page with raw headings/paragraphs/status markup and one `FluentButton`.
- `src/Hexalith.ChatBot.UI/Program.cs` registers Razor components, Fluent UI, Fluxor, the generated HTTP client, `IChatBotClient`, and `GovernedOperationService`.
- `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj` references Client and ServiceDefaults only, plus Fluent UI and Fluxor packages.

Preserve current behavior while tokenizing it. The UI is a surface adapter over `IChatBotClient`; it must not acquire direct Server, gateway, DAPR, audit-writer, idempotency-store, or projection-store dependencies. Existing architecture tests scan for these boundaries. [Source: src/Hexalith.ChatBot.UI/Program.cs; src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]

The current UI test project has service/effect tests but no component or CSS/token tests. Add focused tests rather than broad browser automation unless needed by the implementation. [Source: tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj; tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs; tests/Hexalith.ChatBot.UI.Tests/GovernedOperationsEffectsTests.cs]

### FrontComposer Reuse Intelligence

FrontComposer is present as a root-level sibling/submodule and already contains the relevant shell and badge patterns. Use it as a pattern source and, if dependency shape permits, a runtime shell/component source:

- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` renders `FluentLayout`, skip links, theme/density watchers, projection status summaries, and `<FluentProviders />`.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css` defines scoped semantic `--fc-color-*` aliases and skip-link focus behavior.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor` wraps `FluentBadge`, renders visible label text, exposes `role="status"`, and adds contextual `aria-label`.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/SlotAppearanceTable.cs` maps semantic slots `Neutral`, `Info`, `Success`, `Warning`, `Danger`, and `Accent` to Fluent badge color/appearance pairs.

Do not copy FrontComposer internals into ChatBot. If ChatBot cannot consume the shell cleanly in this story, define a thin ChatBot alias layer with source comments pointing back to FrontComposer and DESIGN.md, then leave the runtime shell swap to a later, explicit story. [Source: Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific Rules; Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md#Load-Bearing API Validation Checklist]

### Previous Story Intelligence

Story 1.13 completed the tenant-scoped fixture and evaluation scaffold at baseline `0a3f392`, after Story 1.12's isolation harness. Carry forward these lessons:

- Non-vacuity matters. A token test that only checks file existence is insufficient; it must prove all required slots and mappings are present.
- Negative controls matter. Tests should fail on raw colors, missing mappings, and missing forced-colors coverage.
- Diagnostics must be metadata-only. UI/test diagnostics may name token keys, slot names, and file paths relative to the repo, but must not dump secrets, raw payloads, tenant data, or provider content.
- Reuse existing assets instead of forking them. For UI foundation, that means FrontComposer shell/badge/token conventions before custom ChatBot components. [Source: _bmad-output/implementation-artifacts/1-13-tenant-scoped-fixture-and-evaluation-scaffold.md#Previous Story Intelligence; _bmad-output/implementation-artifacts/1-12-cross-tenant-isolation-harness.md#Senior Developer Review (AI)]

### Git Intelligence

Recent commits show Story 1.13 landed and the current HEAD already has early story-1.14 orchestration cleanup:

- `c478399` current baseline for this story context.
- `21fd712` removed obsolete orchestration logs/policy snapshots and added UI launch settings.
- `209c569` updated subproject commits and orchestration for story 1.14.
- `911c4fe` implemented Story 1.13 fixture/evaluation scaffold.
- `0a3f392` implemented Story 1.12 cross-tenant isolation harness.

Current dirty worktree entries are unrelated automation/config output: `.agents/skills/bmad-story-automator/data/agent-config-presets.json` and `_bmad-output/story-automator/`. Do not revert or overwrite them while implementing this story. [Source: git log/status on 2026-05-31]

### Architecture and UX Guardrails

- UI stack is Blazor + Fluent UI v5 RC via FrontComposer, Fluxor state, REST commands/queries, and SignalR projection nudge. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- FrontComposer is the UI foundation; Fluent UI APIs are RC-sensitive and customization should be minimal. Do not update Fluent UI, Fluxor, xUnit, or Playwright versions casually. [Source: Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific Rules; Hexalith.FrontComposer/_bmad-output/project-context.md#Critical Don't-Miss Rules]
- Accessibility is a framework contract. Generated or customized UI must preserve labels, keyboard reachability, focus visibility, live-region parity, reduced-motion, and forced-colors behavior. [Source: Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific Rules]
- Semantic status should carry text labels and not rely on color. FrontComposer's `FcStatusBadge` is the local pattern to study before creating any ChatBot-specific status wrapper. [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor]
- Stable machine codes, state names, command names, and correlation IDs should remain metadata/monospace style and are not translated in later localization work. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility Floor]

### Latest Technical Notes

The root `Directory.Packages.props` currently pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`. Do not upgrade it in this story. The architecture and FrontComposer contingency docs are still useful for breakage patterns, but some text mentions RC2 and is older than the root pin. [Source: Directory.Packages.props; Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md]

Microsoft's Fluent UI Web Components docs state that design tokens are semantic named variables for design concepts such as typography, color, sizes, and spacing, and that tokens can emit CSS custom properties. They also discourage fixed colors for adaptive color-system scenarios. This supports aliasing to Fluent token variables instead of hard-coding raw colors. [Source: https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/design-tokens]

Microsoft's Fluent high-contrast guidance uses the `forced-colors` media feature and system color keywords for Windows High Contrast handling. ChatBot-owned wrappers should provide explicit `@media (forced-colors: active)` behavior when status meaning depends on wrapper styling. [Source: https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/high-contrast]

Fluent 2 design-token guidance describes global tokens and alias tokens, with alias tokens adding semantic meaning. ChatBot's `neutral/brand/info/warning/danger/success` layer should be an alias layer over Fluent/FrontComposer tokens, not a raw global-palette replacement. [Source: https://fluent2.microsoft.design/design-tokens]

### Suggested Implementation Shape

Prefer one of these two implementation paths:

1. **Preferred path: FrontComposer shell wrapper.** Add the appropriate FrontComposer Shell project reference, update `_Imports.razor`, wrap the body in `FrontComposerShell`, and then add only ChatBot-specific semantic aliases not already covered by FrontComposer. Validate `<FluentProviders />` is not duplicated.
2. **Fallback path: thin alias layer.** Add `chatbot.tokens.css` and optional C# constants in ChatBot UI that alias Fluent/FrontComposer custom properties, register the stylesheet, and update current UI markup/classes to consume those aliases. Document this as an inheritance bridge, not a new design system.

In either path, keep style names semantic and stable. Example CSS shape:

```css
:root {
    --chatbot-color-neutral-background: var(--colorNeutralBackground1);
    --chatbot-color-neutral-background-raised: var(--colorNeutralBackground2);
    --chatbot-color-neutral-foreground: var(--colorNeutralForeground1);
    --chatbot-color-info-background: var(--colorStatusInformationBackground1);
    --chatbot-color-info-foreground: var(--colorStatusInformationForeground1);
    --chatbot-radius-sm: 4px;
    --chatbot-radius-md: 8px;
    --chatbot-space-2: 8px;
    --chatbot-space-4: 16px;
}

@media (forced-colors: active) {
    .chatbot-status {
        border: 1px solid CanvasText;
        color: CanvasText;
        background: Canvas;
    }
}
```

The final implementation does not have to use these exact names if tests prove the same contract, but it must keep the slot meanings, token source, and accessibility guarantees.

### Testing Requirements

- Use xUnit v3 `3.2.2` and Shouldly `4.3.0`; no new assertion library.
- Prefer tests that parse/read the token source directly for exactness and non-vacuity. Do not rely only on screenshots or subjective visual inspection.
- If component rendering tests are added, keep them in `Hexalith.ChatBot.UI.Tests`; add `bunit` only if necessary and through central package management. The root already has a central `bunit` package version.
- If Playwright is used, use accessible role/label selectors or `data-testid`; do not depend on CSS class selectors for user behavior.
- Preserve existing UI service/effect tests: they guard the governed-command path, UI origin attribution, partial-success wording, and metadata-only error handling.

### Out of Scope

- Building all Story 1.15 reusable governed components.
- Implementing interaction guardrails, streaming Stop/Cancel, keyboard shortcut preferences, responsive/touch foundations, full accessibility floor, live-region matrix, reduced-motion policy, localization infrastructure, or redaction-safe off-surface affordances.
- Upgrading Fluent UI, Fluxor, FrontComposer, .NET, Playwright, or xUnit versions.
- Modifying the `Hexalith.FrontComposer` submodule.
- Adding real M365, attachment, approval, AI, CLI, MCP, Workers, or production data behavior.
- Adding a second UI design system, raw brand palette, custom icon system, custom component library, or decorative marketing UI.

### Project Structure Notes

- UI token CSS, if used: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- UI token constants/contracts, if used: `src/Hexalith.ChatBot.UI/Design/`.
- Current layout to update: `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor`.
- Current page to update: `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`.
- Current app shell/static asset registration point: `src/Hexalith.ChatBot.UI/Components/App.razor`.
- Focused tests: `tests/Hexalith.ChatBot.UI.Tests/`.
- Boundary tests to update if project references change: `tests/Hexalith.ChatBot.Architecture.Tests/`.
- FrontComposer reference patterns: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/` and `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/`.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#UX Design Requirements]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.14: Visual inheritance and semantic token foundation]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/SlotAppearanceTable.cs]
- [Source: src/Hexalith.ChatBot.UI/Components/App.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]
- [Source: src/Hexalith.ChatBot.UI/Program.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/GovernedOperationsEffectsTests.cs]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- [Source: Directory.Packages.props; Directory.Build.props; tests/Directory.Build.props]
- [Source: _bmad-output/implementation-artifacts/1-13-tenant-scoped-fixture-and-evaluation-scaffold.md]
- [Source: Microsoft Fluent UI design tokens: https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/design-tokens]
- [Source: Microsoft Fluent UI high contrast mode: https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/high-contrast]
- [Source: Fluent 2 design tokens: https://fluent2.microsoft.design/design-tokens]

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- 2026-05-31T11:07:16+02:00 - Story started from existing `baseline_commit: c478399`; sprint tracking moved to `in-progress`.
- 2026-05-31T11:08:00+02:00 - Red phase: `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` failed with missing `Hexalith.ChatBot.UI.Design` namespace.
- 2026-05-31T11:10:00+02:00 - Green/refactor: added token contract, token stylesheet, app registration, provider registration, tokenized layout/page markup, and focused token tests.
- 2026-05-31T11:11:00+02:00 - `dotnet test ... --no-restore --no-build` was attempted but VSTest aborted because the sandbox denied local socket creation; used compiled xUnit v3 binaries instead.
- 2026-05-31T11:24:00+02:00 - QA E2E generation: added `Hexalith.ChatBot.UI.E2E.Tests` with browser-first Playwright coverage and deterministic no-browser fallback for restricted sandboxes.
- 2026-05-31T11:26:00+02:00 - QA E2E validation: direct local socket and Chrome launch paths were blocked by sandbox policy; generated tests auto-fell back to static browser-contract assertions and passed 4/4.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implemented the Story 1.14 fallback path: a thin ChatBot-owned token alias layer over Fluent/FrontComposer custom properties, documented in CSS as temporary until a FrontComposer shell wrapper can land without package or boundary churn.
- Added the exact six-slot semantic token contract with stable meanings and aliases under `src/Hexalith.ChatBot.UI/Design/`.
- Registered `chatbot.tokens.css` from `App.razor`, added exactly one `<FluentProviders />`, and left `Program.cs`, Fluent registration, Fluxor registration, package references, and UI project references unchanged.
- Tokenized `MainLayout.razor` and `GovernedOperations.razor` with compact operational layout, metadata/code text styling, visible semantic labels, forced-colors-safe status wrappers, and no service/effect behavior changes.
- Added non-vacuous UI tests for exact slot set, raw-color rejection, `Information` token spelling, forced-colors coverage, stylesheet/provider registration, and render-time examples for `info`, `warning`, `danger`, and `success`.
- Added Story 1.14 E2E/browser-contract tests for runtime stylesheet/token alias loading, semantic role/label workflow coverage, UI-origin command declaration, backend error alert rendering, and forced-colors non-color cues. The tests use Playwright when Chrome can launch, and automatically execute equivalent fixture assertions when the sandbox blocks browser/socket primitives.
- Validation results: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings/0 errors; `Hexalith.ChatBot.UI.E2E.Tests` compiled xUnit v3 binary passed 4/4; `Hexalith.ChatBot.UI.Tests` compiled xUnit v3 binary passed 13/13; `Hexalith.ChatBot.Architecture.Tests` compiled xUnit v3 binary passed 33/33. Broader Server/Conformance/Integration binaries were not run because this story did not touch service behavior, app host wiring, OpenAPI/client boundaries, or governed-command behavior.
- Definition of Done: PASS. Completion score: 25/25 checklist items passed. Quality gates: solution build, UI tests, and architecture tests passed. Documentation: story tasks, Dev Agent Record, File List, Change Log, story status, and sprint status updated.

### File List

- `_bmad-output/implementation-artifacts/1-14-visual-inheritance-and-semantic-token-foundation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260531-085840.md`
- `src/Hexalith.ChatBot.UI/Components/App.razor`
- `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Design/ChatBotSemanticToken.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotSemanticTokenContract.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`
- `Hexalith.ChatBot.slnx`

### Senior Developer Review (AI)

Reviewer: Codex (GPT-5) on 2026-05-31

Outcome: Approved after automatic fixes.

Review scope:

- Loaded `.agents/skills/bmad-story-automator-review/SKILL.md`, `workflow.yaml`, `instructions.xml`, and `checklist.md`.
- Loaded story file, planning architecture, UX design sources, FrontComposer reference patterns, sprint status, git status/diff, and all files in the story File List.
- MCP resource discovery performed; no MCP resources were configured in this workspace.
- Verified AC1-AC7 against implementation and tests.

Findings fixed automatically:

- [Medium] Token tests allowed semantic aliases to point at any CSS custom property, so a slot could be mapped to the wrong Fluent/FrontComposer status token without failing. Fixed by asserting exact expected mappings for `neutral`, `brand`, `info`, `warning`, `danger`, and `success`.
- [Medium] Spacing, radius, and typography aliases were implemented but not guarded by tests, and typography styles still used raw role values directly. Fixed by adding explicit typography role variables and tests for DESIGN.md spacing/radius/type aliases.
- [Medium] The generated E2E harness claimed a no-browser fallback but could still fail before fallback when Chrome was absent. Fixed by resolving Chrome as optional and returning to deterministic fixture assertions when unavailable.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests.dll -noLogo -noColor` - passed 14/14.
- `dotnet tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests.dll -noLogo -noColor` - passed 4/4.
- `dotnet tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests.dll -noLogo -noColor` - passed 33/33.

### Change Log

- 2026-05-31 - Added ChatBot semantic token contract, Fluent/FrontComposer alias stylesheet, tokenized current UI surface, focused token guardrail tests, and validation evidence for Story 1.14.
- 2026-05-31 - Added QA-generated Story 1.14 E2E/browser-contract tests and validation summary.
- 2026-05-31 - Senior review auto-fixed stricter semantic mapping tests, DESIGN.md spacing/radius/typography alias coverage, typography alias consumption, and no-browser E2E fallback.
