---
baseline_commit: f752df5
---

# Story 1.16: Interaction guardrails and streaming stop/cancel behavior

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a UX and safety owner,
I want critical interaction guardrails enforced by foundation components,
so that governed workflows cannot accidentally bypass review, accessibility, or state rules.

## Acceptance Criteria

1. **A reusable interaction guardrail foundation exists in the UI project.** Given Story 1.15's governed primitives, when Story 1.16 is complete, then `src/Hexalith.ChatBot.UI` exposes reusable interaction contracts/components that make the UX-DR33 banned interactions explicit and testable: no hidden auto-association, no risky AI execution from a plain send, no hover-only critical actions, no modal stacks beyond one active dialog/sheet, no infinite scroll for operational queues, and no UI bypass affordance for CLI/MCP/admin authorization. The work must extend the existing `Components/Governed/` and `Design/` foundation; it must not create feature-specific association, approval, queue, AI proposal, or audit surfaces. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.16; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction Primitives]

2. **Critical actions are reachable, labelled, and never hover-only.** Given a governed action can affect association, approval, retry, quarantine, cancellation, escalation, or command execution, when the action is rendered through the foundation primitive, then it is a real keyboard-operable control with a visible label or stable accessible label, exposes enabled/disabled/not-applicable state, and exposes a reachable disabled reason when blocked. Tooltip-only explanations, hover-only menus for critical actions, and non-focusable disabled Fluent buttons as the only reason surface are not allowed. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Keyboard and focus model; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings]

3. **Streaming Stop/Cancel is a reusable governed primitive.** Given an AI response or AI proposal generation is streaming, when streaming is active, then a stable, keyboard-reachable Stop/Cancel control is visible in a predictable focus position, does not steal focus by appearing inline, invokes a cancellable callback, announces exactly "Response stopped" politely when activated, and returns focus to either the composer or the AI proposal panel target supplied by the caller. It must be a UI primitive only; do not add a real AI provider, server streaming endpoint, or new backend cancellation command in this foundation story. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.16; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction Primitives]

4. **Shortcut safety follows WCAG 2.1.4 and is testable.** Given keyboard shortcuts are offered for operators or developers, when a text-entry control is focused, then single-character and modifier-free shortcuts are disabled by default. Shortcut definitions must be remappable or globally disableable through a "Keyboard shortcuts" preferences entry/contract, and labelled controls must remain available for business contributors. Do not wire global shortcut handlers directly in Razor pages without the guardrail contract. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.16; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction Primitives; https://www.w3.org/WAI/WCAG21/Understanding/character-key-shortcuts.html]

5. **Modal/dialog stack prevention is represented mechanically.** Given future surfaces can open dialogs, sheets, popovers, evidence drawers, or approval panels, when the foundation is complete, then there is a UI-owned interaction state/manager or contract that permits at most one active modal dialog/sheet at a time and preserves Escape/focus-return semantics for the active topmost non-destructive overlay. Evidence/review side panels may coexist as labelled complementary regions, but they must not become stacked modal dialogs. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Keyboard and focus model; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor]

6. **Queue pagination guardrail prevents infinite-scroll defaults.** Given operational queues will be implemented later, when this foundation story lands, then reusable queue/list guardrail contracts identify pagination or virtualized list behavior with stable filters as the only permitted queue loading modes. Infinite-scroll defaults are banned by contract/tests, and any sample or placeholder queue guidance must expose active filters/result count/page state rather than unbounded append-only scrolling. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction Primitives; _bmad-output/planning-artifacts/epics.md#Story 7.8]

7. **The current governed operations page may consume non-invasive guardrails without behavior drift.** Given `GovernedOperations.razor` is the current M0 UI surface, when this story updates it, then command submission still dispatches `SubmitGovernedNoteAction`, `GovernedOperationService` still submits through `IChatBotClient` with `ChatBotSurfaceOrigin.Ui`, and the page still renders operation ID, command ID, lifecycle state, completion status, audit status, safe next actions, and metadata-only audit history. Any Stop/Cancel or guarded-action sample on this page must cancel only a local UI pending/streaming placeholder or navigation/disposal token; it must not pretend to cancel an already-submitted governed command unless the backend contract supports that state. [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor; src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs; tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs]

8. **Focused tests prove the guardrails are non-vacuous.** Given later feature stories depend on this foundation, when tests run, then they fail if guardrail constants/contracts are missing, any banned interaction is absent from the enforced list, critical actions can be hover-only, disabled reasons are unreachable, Stop/Cancel lacks keyboard/control/live-region/focus-return semantics, shortcut definitions allow single-character shortcuts in text inputs by default, modal stack policy allows more than one active dialog/sheet, queue loading defaults to infinite scroll, or `GovernedOperations.razor` loses UI-origin command behavior. Use xUnit v3, Shouldly, and existing deterministic UI/static/E2E patterns. Do not upgrade Fluent UI, Fluxor, FrontComposer, Playwright, xUnit, .NET, or add inline package versions. [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs; Directory.Packages.props]

## Tasks / Subtasks

- [x] Define governed interaction contracts in UI-owned code (AC: 1, 2, 4, 5, 6, 8)
  - [x] Add small typed contracts under `src/Hexalith.ChatBot.UI/Design/` for interaction guardrails, guarded action state, shortcut scope, overlay kind, and queue loading mode.
  - [x] Include all six UX-DR33 banned interactions as exact, enumerable, test-covered values.
  - [x] Keep contracts stable and metadata-oriented; no server, gateway, DAPR, audit, projection, AI provider, or generated client dependency.
  - [x] Preserve central package management and do not add package versions in `.csproj` files.

- [x] Add guarded action primitive(s) (AC: 1, 2, 8)
  - [x] Create a governed action primitive in `Components/Governed/` with enabled, `aria-disabled`, disabled-with-reason, and not-applicable-hidden behavior.
  - [x] Ensure critical actions are keyboard reachable and never exposed only through hover, tooltip, icon-only ambiguity, or non-focusable disabled buttons.
  - [x] Provide a reachable reason pattern: either focusable `aria-disabled="true"` plus announced reason or an adjacent focusable "Why unavailable?" affordance.
  - [x] Reuse Fluent UI v5 components and ChatBot semantic token CSS; do not introduce another component library or raw palette.

- [x] Add streaming Stop/Cancel primitive (AC: 3, 8)
  - [x] Create `ChatBotStreamingStopControl.razor` or a similarly explicit governed component under `Components/Governed/`.
  - [x] Expose parameters for `IsStreaming`, stop/cancel callback, stable ID, accessible label, live-region message, and focus-return target ID.
  - [x] Announce exactly `Response stopped` through a polite live region once per activation.
  - [x] Keep the control in a stable focusable position while streaming; do not render it as a transient inline token inside streaming text.
  - [x] Return focus to the supplied composer/proposal target when cancellation completes.
  - [x] Treat backend cancellation as out of scope unless an existing contract already supports it.

- [x] Add shortcut guardrail contract and preference entry hook (AC: 4, 8)
  - [x] Define shortcut metadata that distinguishes text-entry scope from global/operator scope.
  - [x] Default single-character and modifier-free shortcuts to disabled inside composer, search, filters, and configuration forms.
  - [x] Add a "Keyboard shortcuts" preferences entry/contract or placeholder hook that can disable/remap shortcuts globally in later settings work.
  - [x] Ensure labelled UI controls remain the primary path for business contributors.

- [x] Add overlay and queue guardrail contracts (AC: 5, 6, 8)
  - [x] Define an overlay stack policy that permits no more than one active modal dialog/sheet.
  - [x] Preserve side panels/drawers as labelled complementary regions rather than stacked modal dialogs where possible.
  - [x] Define permitted queue loading modes as pagination or virtualization with stable filters/result count/page state.
  - [x] Explicitly reject infinite scroll as a queue default in tests and guardrail labels.

- [x] Integrate only safe, low-risk guardrails into the current page if useful (AC: 7)
  - [x] Update `GovernedOperations.razor` to use the guarded action primitive for "Record governed note" if it can preserve current behavior exactly.
  - [x] Preserve `SubmitGovernedNoteAction`, Fluxor state, status banners, audit metadata rendering, and UI origin.
  - [x] Do not claim a submitted governed operation was cancelled unless existing backend state confirms that outcome.
  - [x] Keep current local status/error semantics and metadata-only audit language intact.

- [x] Add focused non-vacuous tests (AC: 1-8)
  - [x] Extend `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs` or add a sibling contract test file for interaction guardrails.
  - [x] Test exact banned-interaction coverage and fail on missing values.
  - [x] Test guarded action markup for keyboard role, disabled reason, `aria-disabled`, and no hover-only dependency.
  - [x] Test Stop/Cancel markup and behavior for stable position, keyboard reachability, polite `status` live region, exact `Response stopped` text, and focus-return target.
  - [x] Test shortcut defaults reject single-character/modifier-free shortcuts in text inputs and expose remap/disable metadata.
  - [x] Test overlay policy rejects more than one active modal dialog/sheet.
  - [x] Test queue policy rejects infinite scroll and requires pagination/virtualization metadata.
  - [x] Extend E2E/static fixtures only where runtime browser coverage adds value; keep the existing deterministic fallback for restricted local browser/socket environments.

- [x] Verify and document results (AC: 8)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.E2E.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.Architecture.Tests` if dependencies or project references change.
  - [x] Record exact commands, pass/fail counts, and any sandbox browser/socket fallback behavior in this story's Dev Agent Record.

## Dev Notes

### Source Artifact Analysis

Story 1.16 is a foundation guardrail story, not an AI feature story. Its source epic limits scope to enforcing interaction bans, adding a reusable streaming Stop/Cancel behavior, and governing keyboard shortcuts. It does not ask for a real AI stream, proposal generation, association workflow, operational queue, or backend cancellation contract. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.16]

The UX spine has no mockups by design. Binding behavior comes from `EXPERIENCE.md` interaction, keyboard/focus, live-region, reduced-motion, and accessibility rules. The absence of mockups is not permission to invent a consumer-chat interface or bypass the existing governed command path. [Source: _bmad-output/planning-artifacts/epics.md#Cross-cutting acceptance & planning guidance; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction Primitives]

UX-DR33's banned interactions are the central contract for this story: no hidden auto-association when ambiguous, no risky AI execution from plain send, no hover-only critical actions, no stacked active dialogs/sheets, no infinite-scroll queues, and no bypass affordance for CLI/MCP/admin authorization. These should become typed, testable guardrails instead of prose-only comments. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction Primitives]

The accessibility review explains why Stop/Cancel and shortcut governance matter: streaming interruption must be keyboard reachable, polite on cancellation, and stable for focus; single-character shortcuts are a WCAG 2.1.4 risk in composers/search/filter/config inputs. These findings were incorporated into `EXPERIENCE.md` and are now story requirements. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings]

### Current Implementation State

Files likely to be updated:

- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` currently uses `ChatBotConversationShell`, `ChatBotProjectContextHeader`, and `ChatBotStatusBanner`; it dispatches `SubmitGovernedNoteAction` from a primary `FluentButton`; it displays pending/error/outcome state, operation ID, command ID, lifecycle state, completion status, audit status, safe next actions, and metadata-only audit history. This story may replace the submit button wrapper with a guarded action primitive, but command behavior and rendered outcome facts must remain unchanged.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor` currently maps non-terminal feedback to `role="status"` and terminal current-user feedback to `role="alert"`. The Stop/Cancel live-region announcement should follow the non-terminal `status` pattern, not create a noisy alert.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor` currently exposes project context, main region, and optional complementary panel with labels. Overlay/dialog rules should preserve this region model and avoid turning evidence/review panels into stacked modal dialogs.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor` already carries reason and safe next action. Reuse its user-safe next-action pattern for disabled/blocked interaction reasons instead of rendering raw exceptions or restricted detail.
- `src/Hexalith.ChatBot.UI/Design/ChatBotGovernedUiText.cs` owns stable labels for actor, evidence, risk, feedback, and blocked reason. Add new interaction/shortcut/overlay/queue labels here only if they are stable governed UI text; avoid scattering duplicated strings in components/tests.
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` owns token aliases, component layout, focus rings, forced-colors behavior, and responsive shell rules. Add compact classes for new guardrail/streaming controls here, using existing token aliases and forced-colors cues.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs` is the closest contract-test home. It already asserts primitive files, exact enum values, non-color cues, redaction-safe labels, and `GovernedOperations.razor` primitive usage.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` is the closest runtime/static fixture pattern. It uses role/name selectors and a deterministic fallback when the sandbox blocks local browser/socket startup.

Preserve existing behavior:

- `GovernedOperations.razor` must still dispatch `SubmitGovernedNoteAction`.
- `GovernedOperationService` must still use `ChatBotSurfaceOrigin.Ui`.
- UI must not reference `.Server`, gateway stages, DAPR clients, audit writer, idempotency store, projection store, mailbox, AI provider, or direct data-plane infrastructure.
- Existing status banners must keep metadata-only wording and stable IDs; do not surface raw error text, unauthorized resource names, candidate evidence, or restricted audit detail.

### Previous Story Intelligence

Story 1.15 completed at baseline `f3f0e97` and introduced shared governed primitives. Carry forward these lessons:

- Keep foundation components small and reusable. Feature-specific candidate rows, approval panels, queue rows, evidence drawers, and audit timelines belong to later stories.
- Tests should be non-vacuous and exact: enum/category coverage, component file existence, role behavior, redaction-safe text, forced-colors cues, and no color-only meaning.
- Static/fixture E2E fallback is intentional for restricted environments. Preserve it rather than making the main guardrail tests depend on local browser/socket permissions.
- Use ChatBot-owned wrappers over Fluent UI and token aliases. Do not copy FrontComposer internals or introduce another design system.
- Current page behavior should remain stable unless this story explicitly requires a foundation-level guardrail.

[Source: _bmad-output/implementation-artifacts/1-15-shared-governed-component-primitives.md#Completion Notes List; f3f0e97]

Recent git context:

- `f752df5 feat: Update orchestration status and steps for story 1.15`
- `f3f0e97 feat(story-1.15): Shared governed component primitives`
- `6c292c2 feat(story-1.14): Visual inheritance and semantic token foundation`

Current dirty worktree entry observed during story creation: `_bmad-output/story-automator/orchestration-1-20260531-085840.md`. It is unrelated automation output and must not be reverted.

### Architecture and UX Guardrails

- UI stack is Blazor + Fluent UI v5 RC via FrontComposer, Fluxor state, REST commands/queries, and SignalR projection nudge. Do not add a second component library or new global JS shortcut framework for this story. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- M0 UI surfaces are S1 project conversation view, S2 ambiguous association review, and S3 AI action approval. The current page is only the first governed-command path; do not create a fake chat subsystem or wire risky action execution from a plain send. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- CLI/MCP/admin parity must not become a UI bypass affordance. UI, CLI, and MCP are governed clients over the same Contract Spine; every external write enters via the CommandGateway. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
- All action controls must expose role, label, state, disabled reason, and keyboard operation. Focus order follows visible order, repeated landmarks need unique labels, and Escape must not discard unsaved work without explicit confirmation. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility Floor]
- Status/live-region discipline matters. Use `role="status"` for polite non-terminal feedback such as "Response stopped"; reserve `role="alert"` for current-user terminal failure/denial. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State, feedback & live regions; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor]
- Do not force-scroll while the user is reading history. Streaming text animation must be suppressible with reduced motion and progress must have non-motion text. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Reduced motion and auto-scroll]

### Latest Technical Notes

The repo currently pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, `Fluxor` to `6.9.0`, `Microsoft.Playwright` to `1.60.0`, `xunit.v3` to `3.2.2`, and `bunit` to `2.7.2`. Treat root package pins as authoritative; do not upgrade packages in this story. [Source: Directory.Packages.props]

WCAG 2.1 Success Criterion 2.1.4 requires character key shortcuts to be turn-off-able, remappable, or active only on focus. The ChatBot UX contract chooses the stricter local rule: single-character/modifier-free shortcuts are disabled by default inside text-entry controls and globally remappable/disableable. [Source: https://www.w3.org/WAI/WCAG21/Understanding/character-key-shortcuts.html; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction Primitives]

### Suggested Implementation Shape

Prefer a narrow addition to the existing governed foundation:

```text
src/Hexalith.ChatBot.UI/
  Components/Governed/
    ChatBotGovernedAction.razor
    ChatBotStreamingStopControl.razor
  Design/
    ChatBotInteractionGuardrail.cs
    ChatBotGovernedActionState.cs
    ChatBotShortcutScope.cs
    ChatBotShortcutDefinition.cs
    ChatBotOverlayKind.cs
    ChatBotOverlayPolicy.cs
    ChatBotQueueLoadingMode.cs
```

This shape is a suggestion, not a mandate. Keep one type per file and file-scoped namespaces for C# helpers. Razor component names should make the governed purpose obvious and avoid generic names such as `Action`, `CancelButton`, `ModalManager`, or `Shortcut`.

### Testing Requirements

- Use xUnit v3 `3.2.2`, Shouldly `4.3.0`, Playwright `1.60.0`, and existing project patterns. No new assertion library.
- Prefer deterministic component/static tests for guardrail coverage. Use bUnit only if current static/E2E patterns cannot prove component behavior.
- Browser tests, if used, should select by role/name or `data-testid`; do not depend on CSS class selectors for user behavior except where testing CSS guardrails directly.
- Include negative controls for each banned interaction. A future developer should not be able to remove "no infinite scroll" or "no risky action from plain send" without a failing test.
- Test text-entry shortcut rules with explicit examples: composer, search field, filter input, and configuration form.
- Test Stop/Cancel as both visible and absent states: absent/inert when not streaming; stable and focusable while streaming; one polite `Response stopped` announcement after activation.
- Keep architecture boundary tests green if dependencies/project references change.

### Out of Scope

- Real AI provider streaming, server-side stream cancellation, proposal generation, task-intent detection, risk classification, approval execution, or EventStore cancellation commands.
- Feature-specific association review rows, AI proposal/approval panel, queue rows, evidence drawer, attachment row, audit timeline, tenant settings UI, or command palette implementation.
- Responsive/touch foundation from Story 1.17 beyond preserving stable layout for the new controls.
- Full accessibility/focus-management floor from Story 1.18 beyond the specific keyboard/reason/focus-return rules required here.
- Live-region/reduced-motion matrix expansion from Story 1.19 beyond the `Response stopped` polite announcement and no streaming text animation dependency.
- English/French localization infrastructure from Story 1.20 beyond avoiding concatenated accessible names and keeping stable codes untranslated.
- Redaction-safe export/copy/download/read-aloud affordances from Story 1.21.
- Server, gateway, DAPR, audit/idempotency, OpenAPI/client generation, M365, attachments, approval, CLI, MCP, Workers, or production data behavior.
- Upgrading Fluent UI, Fluxor, FrontComposer, .NET, Playwright, xUnit, bUnit, or adding inline package versions.

### Project Structure Notes

- New shared interaction primitives: `src/Hexalith.ChatBot.UI/Components/Governed/`.
- New guardrail contracts: `src/Hexalith.ChatBot.UI/Design/`.
- Token CSS to reuse/extend carefully: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Current page that may consume guarded action: `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`.
- Current submission behavior to preserve: `src/Hexalith.ChatBot.UI/State/GovernedOperations/` and `src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs`.
- Focused UI tests: `tests/Hexalith.ChatBot.UI.Tests/`.
- E2E/static fixture tests: `tests/Hexalith.ChatBot.UI.E2E.Tests/`.
- Boundary tests if dependencies change: `tests/Hexalith.ChatBot.Architecture.Tests/`.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: First Safe Governed Action & Command Spine]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.16: Interaction guardrails and streaming stop/cancel behavior]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction Primitives]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Keyboard and focus model]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Reduced motion and auto-scroll]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility Floor]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings]
- [Source: _bmad-output/implementation-artifacts/1-15-shared-governed-component-primitives.md]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]
- [Source: src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs]
- [Source: src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- [Source: Directory.Packages.props]
- [Source: https://www.w3.org/WAI/WCAG21/Understanding/character-key-shortcuts.html]

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- 2026-05-31T12:44: Red phase: `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` failed at compile time because the new guardrail contracts/components were intentionally missing.
- 2026-05-31T12:45: `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` built the project but VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`; validation used the compiled xUnit v3 binaries as required by this story.
- 2026-05-31T12:47: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-05-31T12:46: `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -reporter default -noLogo -noColor` passed: Total 26, Errors 0, Failed 0, Skipped 0, Not Run 0.
- 2026-05-31T12:47: `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -reporter default -noLogo -noColor` passed: Total 5, Errors 0, Failed 0, Skipped 0, Not Run 0. No browser/socket fallback was reported by the test output.
- 2026-05-31T12:47: `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -reporter default -noLogo -noColor` passed: Total 33, Errors 0, Failed 0, Skipped 0, Not Run 0.
- 2026-05-31T12:47: `git diff --check` passed with no whitespace errors.
- 2026-05-31T12:59: Senior review auto-fix validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-05-31T12:59: Senior review auto-fix validation: `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -reporter default -noLogo -noColor` passed: Total 26, Errors 0, Failed 0, Skipped 0, Not Run 0.
- 2026-05-31T12:59: Senior review auto-fix validation: `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -reporter default -noLogo -noColor` passed: Total 7, Errors 0, Failed 0, Skipped 0, Not Run 0.
- 2026-05-31T12:59: Senior review auto-fix validation: `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -reporter default -noLogo -noColor` passed: Total 33, Errors 0, Failed 0, Skipped 0, Not Run 0.
- 2026-05-31T12:59: Senior review auto-fix validation: `git diff --check` passed with no whitespace errors.
- 2026-06-10T00:00: Follow-up adversarial review auto-fix validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-10T00:00: Follow-up adversarial review auto-fix validation: `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -reporter default -noLogo -noColor` passed: Total 129, Errors 0, Failed 0, Skipped 0, Not Run 0 (was 128 before adding the streaming-stop announcement value assertion).
- 2026-06-10T00:00: Follow-up adversarial review auto-fix validation: `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -reporter default -noLogo -noColor` passed: Total 64, Errors 0, Failed 0, Skipped 0, Not Run 0.
- 2026-06-10T00:00: Follow-up adversarial review auto-fix validation: `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -reporter default -noLogo -noColor` passed: Total 39, Errors 0, Failed 0, Skipped 0, Not Run 0.
- 2026-06-10T00:00: Follow-up adversarial review auto-fix validation: `git diff --check` passed with no whitespace errors.

### Completion Notes List

- Added metadata-only UI guardrail contracts for UX-DR33 banned interactions, guarded action state, shortcut scope/default safety, shortcut preferences, overlay stack policy, and bounded queue loading policy.
- Added `ChatBotGovernedAction` for critical actions with focusable `aria-disabled` behavior, reachable disabled reason text, and not-applicable-hidden rendering.
- Added `ChatBotStreamingStopControl` as a reusable UI-only Stop/Cancel primitive with a polite `Response stopped` live-region announcement and focus return via a small ChatBot-owned focus helper.
- Updated `GovernedOperations.razor` to use the guarded action primitive for `Record governed note` while preserving `SubmitGovernedNoteAction`, existing Fluxor state/status rendering, metadata-only audit language, and UI-origin service behavior.
- Added focused non-vacuous guardrail tests covering exact banned interactions, guarded action markup, streaming Stop/Cancel semantics, shortcut safety defaults, modal stack prevention, queue loading bans, and the current page integration.
- Senior review auto-fixed enabled-action `aria-describedby` output, repeated Stop/Cancel live-region announcement behavior, overlay Escape/focus-return contract coverage, and missing E2E File List documentation.
- 2026-06-10 follow-up adversarial review auto-fixed a non-vacuous-test gap: AC3 requires the streaming Stop/Cancel control to announce exactly `Response stopped`, but no test pinned the resolved `StopResponse_Announcement` resource value (the E2E fixture hard-codes the string in its own script, the unit test only asserted the key name, and the resource-completeness test only checks for non-empty). Added EN/FR value assertions for the Stop announcement and labels through the `ChatBotUiTextLocalizer` render-time path so the exact text is now enforced.

### File List

- _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.UI/Components/App.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor
- src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor
- src/Hexalith.ChatBot.UI/Design/ChatBotGovernedActionState.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotGovernedUiText.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotInteractionGuardrail.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotInteractionGuardrailContract.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotOverlayKind.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotOverlayPolicy.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingContract.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingMode.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingPolicy.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotShortcutDefinition.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotShortcutPreferenceContract.cs
- src/Hexalith.ChatBot.UI/Design/ChatBotShortcutScope.cs
- src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css
- src/Hexalith.ChatBot.UI/wwwroot/js/chatbot.focus.js
- tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs
- tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs
- tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs

### Change Log

- 2026-05-31: Implemented Story 1.16 interaction guardrail foundation, streaming Stop/Cancel primitive, safe governed action integration, and focused validation tests.
- 2026-05-31: Senior review auto-fixed reachable-description, live-region repeat announcement, overlay policy semantics, and File List documentation gaps; marked story done.
- 2026-06-10: Follow-up adversarial review auto-fixed a non-vacuous-test gap by pinning the exact `Response stopped` streaming announcement (and EN/FR Stop labels) in `ChatBotLocalizationContractTests`; no CRITICAL issues, status remains done.

## Senior Developer Review (AI)

Reviewer: Codex (GPT-5) on 2026-05-31

Outcome: Approved after automatic fixes. No CRITICAL issues remain.

### Review Inputs

- Story status verified as `review` before review.
- Acceptance criteria, completed tasks, Dev Agent Record, File List, and Change Log were checked against implementation.
- Architecture guidance loaded from `_bmad-output/planning-artifacts/architecture.md`.
- MCP resources were checked; none were available. Web fallback used the official W3C WCAG 2.1.4 Character Key Shortcuts page: `https://www.w3.org/WAI/WCAG21/Understanding/character-key-shortcuts.html`.
- Git comparison found one source documentation discrepancy: `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` was modified but missing from the story File List. Existing story-automator/test-summary output under `_bmad-output/` was observed but not treated as application-source review scope.

### Findings Fixed

- [HIGH] Enabled `ChatBotGovernedAction` controls always emitted `aria-describedby` pointing at a disabled-reason element that only exists for `DisabledWithReason`, creating a broken accessibility reference for enabled critical actions. Fixed by emitting the reference only when the reachable reason is rendered.
- [HIGH] `ChatBotOverlayPolicy` mechanically prevented stacked modals but did not represent the Escape/focus-return semantics required by AC5. Fixed by adding explicit modal, complementary-region, and Escape/focus-return policy methods with tests.
- [MEDIUM] `ChatBotStreamingStopControl` could leave the live region text unchanged across repeated streaming sessions, so a later activation might not produce a fresh polite announcement. Fixed by clearing the live region before each successful Stop/Cancel activation and preserving the exact `Response stopped` message.
- [MEDIUM] The story File List omitted the modified E2E/static guardrail fixture file. Fixed by adding it to the File List.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -reporter default -noLogo -noColor` - passed, total 26, failed 0, skipped 0.
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -reporter default -noLogo -noColor` - passed, total 7, failed 0, skipped 0.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -reporter default -noLogo -noColor` - passed, total 33, failed 0, skipped 0.
- `git diff --check` - passed.

## Senior Developer Review (AI) — Follow-up

Reviewer: Claude (story-automator review) on 2026-06-10

Outcome: Approved after one automatic fix. No CRITICAL issues remain.

### Review Inputs

- Story status verified as `done`; implementation re-validated against all eight acceptance criteria and every completed task.
- Git reality check: the Story 1.16 work is committed (commit `86f9dd6`, parent `f752df5`) and present in the working tree; there are no uncommitted source changes and the File List matches the committed change set. The only working-tree changes were `_bmad-output/` automation artifacts (excluded from review scope).
- Verified the four fixes claimed by the 2026-05-31 review are genuinely present: enabled-action `aria-describedby` suppression, `ChatBotOverlayPolicy` Escape/focus-return methods, `ChatBotStreamingStopControl` clear-before-announce, and the E2E File List entry.
- Confirmed every render-time localization key used by the new components/page resolves in both `SharedResource.resx` and `SharedResource.fr.resx` (the localizer throws on a missing key), and that all CSS classes the new components rely on exist (including the focus-visible style on the focusable disabled-reason and the visually-hidden live region).

### Findings Fixed

- [MEDIUM][AC3, AC8] The streaming Stop/Cancel announcement requirement was tested vacuously. AC3 mandates the control announce **exactly** `Response stopped`, but no test pinned the resolved `StopResponse_Announcement` value: the E2E fixture hard-codes the string in its own inline script, the unit test only asserted the key name via a static file read, and the resource-completeness test only asserts non-empty. The exact text could have drifted with every test still green. Fixed by adding `StreamingStopControlAnnouncementTextShouldResolveExactlyInEnglishAndFrench` to `ChatBotLocalizationContractTests`, which asserts the resolved EN/FR values for the announcement and the visible/accessible Stop labels through the same `ChatBotUiTextLocalizer` path the component uses (`Response stopped` / `Réponse arrêtée`).

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -reporter default -noLogo -noColor` - passed, total 129, failed 0, skipped 0.
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -reporter default -noLogo -noColor` - passed, total 64, failed 0, skipped 0.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -reporter default -noLogo -noColor` - passed, total 39, failed 0, skipped 0.
- `git diff --check` - passed.
