---
baseline_commit: 6c292c2
---

# Story 1.15: Shared governed component primitives

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a frontend engineer,
I want shared governed component primitives,
so that feature stories compose the same project, actor, evidence, risk, blocked-state, and status language.

## Acceptance Criteria

1. **Reusable governed primitive set exists in the UI project.** Given the Story 1.14 token foundation, when Story 1.15 is complete, then `src/Hexalith.ChatBot.UI` exposes reusable behavioral components for project context header, conversation shell, actor badge, evidence chip, risk chip, blocked state, and status toast/banner. These components must compose Fluent UI v5 and ChatBot semantic tokens; they must not introduce another component library, a second design system, raw palettes, marketing cards, or feature-specific candidate/proposal/queue/audit components. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.15; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]

2. **Actor badge distinguishes all eight UX categories without color dependence.** Given actor attribution appears in conversation, audit, and approval surfaces, when `ChatBotActorBadge` renders, then it supports exactly these actor categories by stable label plus icon affordance: human user, external party, service client, AI actor, background worker, CLI, MCP, and mailbox event. It must expose an accessible name containing the actor type and resolved display label, support unresolved actors with a safe unresolved label/action affordance, and use one neutral token treatment for category identity rather than per-actor colors. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component Patterns; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]

3. **Evidence and risk chips are keyboard-operable, text-first, and redaction-safe.** Given evidence chips and risk chips appear in association, approval, audit, and queue contexts, when they render, then each chip contains visible text, a semantic status/risk label, non-color cue, and a deterministic accessible label. Evidence chips that can open supporting evidence must be keyboard reachable and must expose a disabled/unavailable reason when evidence is redacted or unauthorized. Risk chips must name the risk class in plain language and include the policy reason that caused review; color alone is never the signal. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility Floor; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]

4. **Blocked state and status feedback follow the UX state-to-feedback matrix.** Given a denial, unresolved association, quarantine, failed dependency, unsafe context, accepted command, retry queued, degraded dependency, or validation failure, when the blocked state or status toast/banner primitive renders, then it uses user-safe message text, safe next action, correct ARIA role (`status` for polite/non-terminal, `alert` only for current-user terminal failure/denial), and metadata-only stable IDs. Long-lived operational states must render inline on the affected surface; toast/banner is transition feedback only. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State Patterns; _bmad-output/planning-artifacts/epics.md#Story 1.7]

5. **Conversation shell and project context header keep safety state visible.** Given future S1/S2/S3 stories will host project conversation, association review, and AI approval, when the conversation shell renders, then it keeps authorized project identity, tenant context when relevant, current conversation/state, status, and main content landmarks visible while panels/evidence/approval content are open. It must not create a fake chat surface, hide system decisions as chat bubbles, or remove the current governed-command path from `GovernedOperations.razor`. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component Patterns]

6. **The current governed operations page consumes the primitives without changing command behavior.** Given `GovernedOperations.razor` is the current M0 UI surface, when primitives replace its local status/layout markup, then submission still dispatches `SubmitGovernedNoteAction`, `GovernedOperationService` still submits through `IChatBotClient` with `ChatBotSurfaceOrigin.Ui`, and the page still renders operation ID, command ID, lifecycle state, completion status, audit status, safe next actions, and metadata-only audit history. No UI code may reference Server, gateway internals, DAPR clients, audit writer, idempotency store, or projection store. [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor; src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs; tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs]

7. **Focused tests prove the primitives are non-vacuous.** Given future feature stories depend on these components, when tests run, then they fail if required primitives are missing, actor categories are incomplete, chips rely on color alone, blocked/status roles are wrong, unauthorized/redacted states leak restricted labels, keyboard activation/disabled-reason behavior is absent, or `GovernedOperations.razor` stops using the shared primitives for status outcomes. Use xUnit v3, Shouldly, and existing UI/E2E test patterns; add bUnit only if component-level rendering cannot be tested by current string/static fixture approaches. [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs; Directory.Packages.props]

8. **Build and regression gates stay green.** Given this story touches UI primitives only, when implementation is complete, then `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` succeeds; compiled xUnit v3 binaries for `Hexalith.ChatBot.UI.Tests`, `Hexalith.ChatBot.UI.E2E.Tests`, and `Hexalith.ChatBot.Architecture.Tests` are green. Broader Server/Conformance/Integration tests are required only if service behavior, app host wiring, OpenAPI/client boundaries, or governed-command behavior changes. No Fluent UI, Fluxor, FrontComposer, Playwright, xUnit, or .NET package upgrades are introduced. [Source: _bmad-output/implementation-artifacts/1-14-visual-inheritance-and-semantic-token-foundation.md#Completion Notes List; Directory.Packages.props]

## Tasks / Subtasks

- [x] Define the governed primitive component contracts (AC: 1, 2, 3, 4, 5)
  - [x] Create components under `src/Hexalith.ChatBot.UI/Components/Governed/` or a similarly explicit UI-owned folder.
  - [x] Keep contracts simple, typed, and reusable: prefer small parameter records/enums for component state instead of passing unstructured strings everywhere.
  - [x] Do not create feature-specific components in this story: candidate row, AI proposal/approval panel, queue row, and audit timeline stay in their feature stories.
  - [x] Reuse the Story 1.14 token aliases and CSS variables. Add component CSS only for layout/behavior gaps that Fluent/FrontComposer do not already cover.

- [x] Build `ChatBotProjectContextHeader` and `ChatBotConversationShell` (AC: 1, 5)
  - [x] Header shows authorized project identity, optional tenant context, current conversation/state, and safe status.
  - [x] Shell exposes landmarks/slots for project context, main conversation/detail, and optional complementary panel content with unique labels when roles repeat.
  - [x] Keep workflow state visible when child panels are rendered.
  - [x] Avoid fake chat affordances in this foundation story; the current page remains an operational governed-command surface.

- [x] Build `ChatBotActorBadge` with exact actor category coverage (AC: 2)
  - [x] Support exactly: human user, external party, service client, AI actor, background worker, CLI, MCP, mailbox event.
  - [x] Render stable visible labels and an icon affordance for each category. Prefer Fluent/FrontComposer icon/component patterns already available in the dependency graph; do not add a new icon package unless unavoidable and centrally versioned.
  - [x] Use accessible names that include actor category plus resolved label/client/source when authorized.
  - [x] Add unresolved actor state with safe text and optional recovery affordance, without leaking unavailable party/project details.
  - [x] Differentiate categories by label/icon, not color.

- [x] Build `ChatBotEvidenceChip` and `ChatBotRiskChip` (AC: 3)
  - [x] Evidence chip supports evidence reason text, semantic state, optional supporting-evidence activation, and unavailable/redacted reason.
  - [x] Keyboard activation for actionable chips must match mouse activation.
  - [x] Risk chip supports risk class plus policy reason for review. Use the six risky action classes named in UX/architecture context when sample data is needed: externally visible, file-exposing, project-mutating, tool-invoking, task-creating, participant-representing.
  - [x] Ensure all chip states include visible text and border/icon/label cues for forced-colors users.

- [x] Build `ChatBotBlockedState` and `ChatBotStatusBanner`/toast primitive (AC: 4)
  - [x] Blocked state covers denial, unresolved association, quarantine, failed dependency, and unsafe context with user-safe reason and safe next action.
  - [x] Status primitive supports info, warning, danger, and success semantics from Story 1.14 and uses the correct ARIA role based on feedback type.
  - [x] Keep persistent states inline; do not implement a global notification stack beyond the transition primitive needed by this story.
  - [x] Draw user-facing language from safe message patterns. Do not render raw exception text, unauthorized names, restricted file metadata, candidate evidence, or sensitive audit details.

- [x] Replace local governed-operations status/layout markup with shared primitives (AC: 5, 6)
  - [x] Update `GovernedOperations.razor` to consume the project context header/shell and status primitives where they apply.
  - [x] Preserve the current command submission flow and outcome fields exactly.
  - [x] Preserve one `<FluentProviders />` registration in `App.razor`.
  - [x] Keep `Program.cs` service registrations and UI project references unchanged unless the implementation proves a minimal FrontComposer reference is necessary.

- [x] Add focused tests and fixtures (AC: 2, 3, 4, 6, 7, 8)
  - [x] Add UI tests that prove the primitive files/contracts exist and expose exact required actor categories.
  - [x] Add render/static tests for actor badge labels, accessible names, unresolved state, and no per-category color dependence.
  - [x] Add tests for evidence/risk chip text labels, keyboard affordance or button role, disabled/unavailable reason, and forced-colors-safe cues.
  - [x] Add tests for blocked/status roles (`status` vs `alert`), safe next action, and metadata-only messages.
  - [x] Update existing E2E/static fixture coverage so the governed operations page uses shared primitives and still declares UI origin.
  - [x] Keep architecture boundary tests green; expand them if component dependencies add project references.

- [x] Verify and document results (AC: 7, 8)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.E2E.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.Architecture.Tests`.
  - [x] Record exact commands, pass/fail counts, and any sandbox browser/socket fallback behavior in this story's Dev Agent Record.

## Dev Notes

### Source Artifact Analysis

Story 1.15 follows Story 1.14 and turns the token foundation into reusable governed UI primitives. The epic's exact scope is project context header, conversation shell, actor badge, evidence chip, risk chip, blocked state, and status toast/banner. It explicitly excludes candidate row, AI proposal/approval panels, queue row, and audit timeline, which belong to later feature stories. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.15]

The UX package has no mockups by design. Binding behavior comes from the component, state, feedback, accessibility, responsive, and cognitive-load tables. The absence of mockups is not permission to invent a custom ChatBot visual language or a chat-like surface. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Foundation; _bmad-output/planning-artifacts/epics.md#Cross-cutting acceptance & planning guidance]

The product posture remains a dense operational SaaS workspace. Components should be compact, scannable, and evidence/status-first. Avoid marketing empty states, decorative card stacks, large pill-heavy layouts, raw palettes, and broad custom CSS. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Brand & Style; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Do's and Don'ts]

### Current Implementation State

Current UI files likely to be updated:

- `src/Hexalith.ChatBot.UI/Components/App.razor` registers `css/chatbot.tokens.css` and one `<FluentProviders />`.
- `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor` provides the current tokenized shell header, skip link, and `main` region.
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` renders the current governed-command page with local status wrappers that should move behind shared primitives.
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` owns the Story 1.14 token aliases, layout classes, status classes, and forced-colors behavior.
- `src/Hexalith.ChatBot.UI/Design/ChatBotSemanticTokenContract.cs` defines the six semantic slots and meanings.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs` and `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` are the closest test patterns to extend.

The UI adapter must remain a client-only surface. It may depend on `Hexalith.ChatBot.Client`, `Hexalith.ChatBot.ServiceDefaults`, Fluent UI, Fluxor, and approved FrontComposer UI pieces if introduced deliberately. It must not reference `.Server`, gateway stages, DAPR clients, audit writer, idempotency store, or projection store. [Source: src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj; _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]

### FrontComposer Reuse Intelligence

FrontComposer should be treated as a reference and possible runtime component source, not copied wholesale:

- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor` is the local pattern for a thin Fluent badge wrapper with visible label text, `role="status"`, contextual `aria-label`, and semantic slot data attributes.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/SlotAppearanceTable.cs` uses a frozen exhaustive mapping from semantic slots to Fluent `BadgeColor`/`BadgeAppearance`.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` is the shell pattern for skip links, Fluent layout, providers, theme/density watchers, projection status summaries, and slot-based header/navigation/content.

Do not edit files inside `Hexalith.FrontComposer` unless the task is explicitly expanded. Do not copy its internals into ChatBot; use its public components if the dependency shape is clean, otherwise mirror the pattern with small ChatBot-owned wrappers. [Source: _bmad-output/implementation-artifacts/1-14-visual-inheritance-and-semantic-token-foundation.md#FrontComposer Reuse Intelligence]

### Previous Story Intelligence

Story 1.14 completed at baseline `6c292c2` and intentionally used a thin ChatBot-owned token alias layer rather than the full FrontComposer shell wrapper. Carry forward these lessons:

- The Story 1.14 token contract is now the local source for semantic colors and should not be duplicated.
- Non-vacuity matters. Tests must prove exact actor/category coverage, role behavior, redaction-safe text, and no color-only status.
- Negative controls matter. Tests should fail on missing categories, raw colors where token aliases are expected, tooltip-only disabled reasons, and raw exception/user-restricted details.
- Browser tests may fall back to deterministic static fixture assertions when the sandbox blocks local sockets or Chrome launch. Preserve this pattern rather than making restricted environments fail for infrastructure reasons.
- Keep UI behavior unchanged unless the story explicitly requires behavior changes. Story 1.15 should mostly extract and reuse primitives around the current M0 page.

[Source: _bmad-output/implementation-artifacts/1-14-visual-inheritance-and-semantic-token-foundation.md#Completion Notes List; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]

### Git Intelligence

Recent commits:

- `6c292c2 feat(story-1.14): Visual inheritance and semantic token foundation` added the token contract, CSS aliases, governed operations tokenization, UI/E2E token tests, and marked story 1.14 done.
- `911c4fe feat(story-1.13): Tenant-Scoped Fixture and Evaluation Scaffold` added fixture/evaluation infrastructure and reinforced non-vacuous guardrail testing.
- `21fd712` added UI launch settings and removed obsolete orchestration output.
- `209c569` updated subproject commits and orchestration for Story 1.14.

Current dirty worktree entry observed during story creation: `_bmad-output/story-automator/orchestration-1-20260531-085840.md`. It is unrelated automation output and must not be reverted by the dev agent.

### Architecture and UX Guardrails

- UI stack is Blazor + Fluent UI v5 RC via FrontComposer, Fluxor state, REST commands/queries, and SignalR projection nudge. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- M0 UI surfaces are S1 project conversation view, S2 ambiguous association review, and S3 AI action approval. The conversation view is a read projection that future chat/action input can write into through the same CommandGateway; do not create a separate chat subsystem. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- Components must preserve WCAG 2.2 AA behavior: keyboard operation, focus order, reachable disabled reasons, non-color status, live-region discipline, forced-colors compatibility, and redacted screen-reader-equivalent messaging. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility Floor]
- Stable machine codes, state names, command names, correlation IDs, and operation IDs stay metadata/monospace and are not user-facing prose to localize later. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility Floor]
- Error and denial language must not reveal unauthorized project names, file metadata, candidate evidence, or sensitive audit details. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Voice and Tone]

### Latest Technical Notes

The repo currently pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`; NuGet shows that RC3 package was published on May 19, 2026, while the latest stable NuGet listing is still `4.14.2`. Treat the root pin as authoritative and do not upgrade Fluent UI in this story. [Source: Directory.Packages.props; https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components]

Microsoft Fluent design-token guidance supports semantic aliases over component-library tokens rather than hard-coded product palettes. Story 1.15 should keep using ChatBot aliases over Fluent/FrontComposer custom properties. [Source: https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/design-tokens]

Microsoft high-contrast guidance points to `forced-colors` handling and system color keywords. Product-owned status/chip wrappers must keep explicit forced-colors behavior when meaning depends on wrapper styling. [Source: https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/high-contrast]

### Suggested Implementation Shape

Prefer a narrow component folder:

```text
src/Hexalith.ChatBot.UI/
  Components/Governed/
    ChatBotProjectContextHeader.razor
    ChatBotConversationShell.razor
    ChatBotActorBadge.razor
    ChatBotEvidenceChip.razor
    ChatBotRiskChip.razor
    ChatBotBlockedState.razor
    ChatBotStatusBanner.razor
  Design/
    ChatBotActorCategory.cs
    ChatBotFeedbackKind.cs
    ChatBotRiskActionClass.cs
```

This shape is a suggestion, not a mandate. Keep one type per file and file-scoped namespaces for C# helpers. Razor component names should make the governed purpose obvious and avoid generic names such as `Badge`, `Chip`, or `Alert`.

### Testing Requirements

- Use xUnit v3 `3.2.2`, Shouldly `4.3.0`, Playwright `1.60.0`, and existing project patterns. No new assertion library.
- Prefer deterministic component/static tests for exact contract coverage. Use bUnit only if current static/E2E patterns cannot prove the component behavior.
- If Playwright is used, use role/name or `data-testid` selectors for behavior. Do not depend on CSS class selectors for user behavior except where testing CSS guardrails directly.
- Tests should include negative controls for leakage: unauthorized/redacted states may name safe categories, reason codes, and recovery actions, but must not include restricted project/file/party/audit details in visible text or accessible labels.

### Out of Scope

- Feature-specific candidate row, AI proposal/approval panel, queue row, evidence drawer, attachment row, audit timeline, or tenant configuration components.
- Interaction guardrails and streaming Stop/Cancel behavior from Story 1.16.
- Responsive/touch foundation from Story 1.17.
- Full accessibility/focus-management floor from Story 1.18 beyond what these primitives must expose.
- Live-region/reduced-motion matrix expansion from Story 1.19 beyond the feedback roles required here.
- English/French localization infrastructure from Story 1.20.
- Redaction-safe copy/export/download/read-aloud affordances from Story 1.21 beyond redaction-safe text and labels in these primitives.
- Server, gateway, DAPR, audit/idempotency, OpenAPI, generated client, M365, attachment, approval, AI, CLI, MCP, Workers, or production data behavior.
- Upgrading Fluent UI, Fluxor, FrontComposer, .NET, Playwright, xUnit, or adding inline package versions.

### Project Structure Notes

- New shared primitives: `src/Hexalith.ChatBot.UI/Components/Governed/` unless the implementation finds an existing better UI-owned folder.
- Token contract to reuse: `src/Hexalith.ChatBot.UI/Design/ChatBotSemanticTokenContract.cs`.
- Token CSS to reuse/extend carefully: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Current page to migrate: `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`.
- Current layout/provider registration: `src/Hexalith.ChatBot.UI/Components/App.razor` and `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor`.
- Focused UI tests: `tests/Hexalith.ChatBot.UI.Tests/`.
- E2E/browser-contract tests: `tests/Hexalith.ChatBot.UI.E2E.Tests/`.
- Boundary tests: `tests/Hexalith.ChatBot.Architecture.Tests/`.
- FrontComposer reference patterns: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/` and `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/`.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: First Safe Governed Action & Command Spine]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.15: Shared governed component primitives]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility Floor]
- [Source: _bmad-output/implementation-artifacts/1-14-visual-inheritance-and-semantic-token-foundation.md]
- [Source: src/Hexalith.ChatBot.UI/Components/App.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]
- [Source: src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotSemanticTokenContract.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges/SlotAppearanceTable.cs]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor]
- [Source: Directory.Packages.props]
- [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components]
- [Source: https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/design-tokens]
- [Source: https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/high-contrast]

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- Red phase: `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` failed before implementation with missing `ChatBotActorCategory`, `ChatBotRiskActionClass`, and `ChatBotGovernedUiText`.
- Sandbox note: `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore --no-build` aborted because VSTest attempted to open a local socket and received `System.Net.Sockets.SocketException (13): Permission denied`; compiled xUnit v3 binaries were used for validation.
- Build gate: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- UI test gate: `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` passed, total 20, failed 0, skipped 0.
- UI E2E/static gate: `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` passed, total 5, failed 0, skipped 0. The suite retains the existing deterministic static fallback for restricted browser startup; no E2E gate failure occurred.
- Architecture gate: `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` passed, total 33, failed 0, skipped 0.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added governed UI primitives for project context, shell layout, actor attribution, evidence/risk chips, blocked states, and status feedback using Story 1.14 ChatBot semantic tokens.
- Added typed primitive contracts for actor categories, evidence states, risk classes, feedback kinds, and blocked reasons, with stable labels and non-color cue text.
- Migrated `GovernedOperations.razor` to compose `ChatBotConversationShell`, `ChatBotProjectContextHeader`, and `ChatBotStatusBanner` while preserving `SubmitGovernedNoteAction`, outcome fields, metadata-only audit history, and `ChatBotSurfaceOrigin.Ui`.
- Added focused UI contract tests plus E2E/static assertions proving exact actor/risk coverage, actor accessible names, unresolved actor actions, keyboard/unavailable chip affordances, status/blocked roles, shared primitive usage, redaction-safe fixture text, and architecture boundary compliance.
- Senior review auto-fixed primitive accessibility/composition issues and kept the build, UI, E2E/static, and architecture gates green.

### File List

- _bmad-output/implementation-artifacts/1-15-shared-governed-component-primitives.md
- _bmad-output/implementation-artifacts/tests/test-summary.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectContextHeader.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor
- src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor
- src/Hexalith.ChatBot.UI/Components/_Imports.razor
- src/Hexalith.ChatBot.UI/Design/ChatBotActorCategory.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotBlockedReason.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotEvidenceState.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotFeedbackKind.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotGovernedUiText.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotRiskActionClass.cs
- src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css
- tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs
- tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs
- tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs

## Senior Developer Review (AI)

Reviewer: Codex (GPT-5) on 2026-05-31

Outcome: Approved after automatic fixes. No CRITICAL issues remain.

### Review Inputs

- Story status verified as `review` before review.
- Acceptance criteria, completed tasks, Dev Agent Record, File List, and Change Log were checked against implementation.
- Architecture guidance loaded from `_bmad-output/planning-artifacts/architecture.md`.
- MCP resources were checked; none were available. Web fallback references used for Fluent token and forced-colors behavior:
  - `https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/design-tokens`
  - `https://learn.microsoft.com/en-us/fluent-ui/web-components/design-system/high-contrast`
- Git comparison found the story source/test file list aligned with the implemented story files. Existing root submodule pointer changes and story-automator orchestration output were observed but not treated as application-source review targets for this story.

### Findings Fixed

- [MEDIUM] `ChatBotEvidenceChip` mixed a native `<button>` with a manual `@onkeydown` activation path, which can double-submit Enter/Space activations in real browsers. Fixed by relying on native button keyboard behavior and keeping unavailable/redacted activation guarded in `ActivateAsync`.
- [MEDIUM] `ChatBotActorBadge` unresolved-action buttons had only generic visible text, making multiple unresolved actors ambiguous to assistive technology. Fixed with an action-specific accessible label containing actor category and safe unresolved display label.
- [MEDIUM] Several primitive non-color cues were plain spans, leaving the "compose Fluent UI v5" requirement weaker than claimed. Fixed by using `FluentBadge` for actor/evidence/risk/blocked/status cues while preserving ChatBot semantic token classes.
- [LOW] Static tests still asserted the old manual keydown implementation and did not lock exact evidence/blocked enum coverage. Updated UI and E2E/static tests to cover the corrected contracts.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` - passed, total 20, failed 0, skipped 0.
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` - passed, total 5, failed 0, skipped 0.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` - passed, total 33, failed 0, skipped 0.

### Change Log

- 2026-05-31: Implemented shared governed component primitives, migrated governed operations status/layout composition, and added focused UI/E2E/static primitive guardrail tests.
- 2026-05-31: Senior review auto-fixed evidence keyboard activation semantics, unresolved actor action labels, Fluent badge composition for primitive cues, and focused contract assertions; marked story done.
