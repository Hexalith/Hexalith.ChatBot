---
baseline_commit: ab529e2
---

# Story 1.18: Accessibility and focus-management floor

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As an accessibility owner,
I want the WCAG 2.2 AA keyboard and focus-management floor established,
so that every governed surface is operable by inheritance.

## Acceptance Criteria

1. **Accessibility floor contracts exist in UI-owned code.** Given the governed UI foundation from Stories 1.14-1.17, when Story 1.18 is complete, then `src/Hexalith.ChatBot.UI/Design/` exposes reusable contracts for keyboard operation, repeated-landmark naming, visible-order focus sequence, focus return, disabled-action explanation, and busy-region focus preservation. These contracts must extend the existing design-contract pattern and must not create feature-specific S1/S2/S3 screens. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.18; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Keyboard-and-focus-model; src/Hexalith.ChatBot.UI/Design/ChatBotOverlayPolicy.cs]

2. **Current layout and shell expose stable landmark and focus-entry semantics.** Given the app shell, `ChatBotConversationShell`, and the current governed operations page render, when a keyboard or screen-reader user navigates the page, then skip-link, `main`, shell, project context, primary region, complementary region, status summary, and current surface heading are reachable in visible reading/action order. Repeated landmark roles within one surface must carry unique accessible names, and `FocusOnNavigate` must continue targeting the surface `h1`. [Source: src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor; src/Hexalith.ChatBot.UI/Components/Routes.razor; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Keyboard-and-focus-model]

3. **Disabled governed actions remain discoverable without firing.** Given `ChatBotGovernedAction` renders a disabled or unavailable approval/association/retry/correction action, when users tab to it or activate it with keyboard, then the action exposes `aria-disabled="true"`, keeps the reason reachable through `aria-describedby` or an adjacent focusable "Why unavailable?" affordance, does not use tooltip-only explanation, and does not invoke the action while disabled. Do not add the native HTML `disabled` attribute to this primitive because it would remove the control from the focus order. [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor; https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-disabled]

4. **Dialog, sheet, drawer, popover, and review-panel focus behavior is mechanical.** Given an overlay can become the active topmost interaction layer, when it opens, closes, or receives Escape, then the foundation requires single active modal dialog/sheet behavior, focus containment where modal, Escape close for non-destructive topmost overlays, and focus return to the invoking control. Evidence drawers and review panels remain labelled complementary regions unless intentionally modal. [Source: src/Hexalith.ChatBot.UI/Design/ChatBotOverlayPolicy.cs; tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs; https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/]

5. **Busy/loading and validation focus rules are represented for future surfaces.** Given future conversation, association, approval, queue, audit, and tenant-configuration surfaces load data or reject form/review submissions, when they use the foundation contracts, then `aria-busy` is set and cleared on the same busy region, focus is preserved or moved to a labelled landing point after content swaps in, newly loaded historical content does not announce, validation summaries appear before the affected panel, invalid inputs carry `aria-invalid="true"` and are associated with field messages through `aria-describedby` or `aria-errormessage`. This story should provide contract/test scaffolding only; full live-region matrix work belongs to Story 1.19. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix; https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-busy; https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-invalid]

6. **Existing streaming Stop/Cancel and shortcut guardrails are preserved.** Given Story 1.16 already established keyboard-reachable Stop/Cancel, polite cancellation announcement, focus return, and shortcut governance, when Story 1.18 changes focus/accessibility code, then `ChatBotStreamingStopControl`, `chatbot.focus.js`, and shortcut contracts still satisfy those behaviors and are not duplicated into a second mechanism. [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor; src/Hexalith.ChatBot.UI/wwwroot/js/chatbot.focus.js; src/Hexalith.ChatBot.UI/Design/ChatBotShortcutDefinition.cs; _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md#Completion-Notes-List]

7. **Focused tests prove the floor is non-vacuous.** Given later S1/S2/S3 surface stories depend on this story, when tests run, then they fail if keyboard-operation contracts are missing, repeated landmarks are not uniquely named, focus-return policy no longer covers dialogs/sheets/drawers/review panels, disabled actions can fire or become tooltip-only, busy-region focus preservation is absent, validation error association is absent, `GovernedOperations.razor` loses its current focus/landmark path, or package versions are upgraded. Use xUnit v3, Shouldly, and the existing Playwright/static fixture pattern; do not add axe-core, a new UI library, or new package pins in this story. [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs; Directory.Packages.props]

## Tasks / Subtasks

- [x] Define UI-owned accessibility and focus contracts (AC: 1, 4, 5, 7)
  - [x] Add small typed contracts under `src/Hexalith.ChatBot.UI/Design/` for keyboard operation, landmark uniqueness, visible-order focus sequence, focus return, busy-region behavior, and validation error association.
  - [x] Extend `ChatBotOverlayPolicy` only if needed to make modal containment, Escape close, focus return, and complementary-region behavior explicit and testable.
  - [x] Keep contracts metadata-oriented and UI-owned; no server, gateway, DAPR, audit, projection, AI provider, CLI, MCP, or generated client dependency.
  - [x] Do not duplicate Story 1.16's shortcut or streaming-stop contracts.

- [x] Harden current shell and governed primitives where the foundation belongs (AC: 2, 3, 6)
  - [x] Review `MainLayout.razor`, `Routes.razor`, `ChatBotConversationShell.razor`, `ChatBotProjectContextHeader.razor`, `ChatBotGovernedAction.razor`, `ChatBotStatusBanner.razor`, and `GovernedOperations.razor` before editing.
  - [x] Preserve skip-link to `#chatbot-main-content`, `main tabindex="-1"`, `FocusOnNavigate Selector="h1"`, project context labelling, and the current governed command path.
  - [x] Ensure `ChatBotConversationShell` can guarantee unique labels when main and complementary/review regions coexist.
  - [x] Preserve `ChatBotGovernedAction`'s `aria-disabled`, `aria-describedby`, focusable reason, no `title`, and no native `Disabled` attribute behavior.
  - [x] Preserve `ChatBotStreamingStopControl`'s keyboard-reachable stop, polite "Response stopped" announcement, and focus return.

- [x] Represent busy/loading and validation focus rules without building feature screens (AC: 5, 7)
  - [x] Provide a contract for busy regions: stable region id/label, `aria-busy` lifecycle, focus preservation or labelled landing target, and no historical-content announcement.
  - [x] Provide a contract for validation failures: summary id/label, focus target, affected field ids, `aria-invalid`, message association, and safe next action.
  - [x] Do not implement association review, AI approval, queue, audit, tenant settings, or localization forms in this story.

- [x] Add focused non-vacuous tests (AC: 1-7)
  - [x] Add or extend `tests/Hexalith.ChatBot.UI.Tests/` contract tests for all new design contracts and for package pin preservation.
  - [x] Extend `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` or a sibling fixture to prove keyboard tab order, skip-link/main focus, unique region names, disabled action reason reachability, and disabled non-activation.
  - [x] Keep browser tests deterministic with the existing `BrowserHarness.TryStartAsync()` fallback path for restricted browser/socket environments.
  - [x] Prefer accessible role/label selectors. Use CSS selectors only for mechanics such as active element, data contract hooks, or overflow/focus-ring checks.

- [x] Verify and document results (AC: 7)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.E2E.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.Architecture.Tests` if dependencies or project references change.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, pass/fail counts, and any browser/socket fallback behavior in this story's Dev Agent Record.

## Dev Notes

### Source Artifact Analysis

Story 1.18 is a UI foundation story. It anchors the accessibility and focus-management floor before later S1/S2/S3 feature surfaces are built. It does not ask for association review, AI approval, operational queues, audit investigation, tenant configuration, localization infrastructure, export/copy affordances, live-region matrix expansion, backend changes, or new component libraries. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.18; _bmad-output/planning-artifacts/epics.md#Cross-cutting-acceptance-and-planning-guidance]

The PRD scopes WCAG 2.2 AA per increment to the UI surfaces that exist in that increment. Validation must include automated checks plus keyboard-only and screen-reader review of each in-scope surface before release. NFR61 specifically calls out keyboard-only review flows, screen-reader labels, focus order, non-color status indicators, and error recovery for ambiguous association and approval workflows. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR61]

The UX package has no mockups by design. Binding requirements come from `EXPERIENCE.md`: keyboard operation is required for all workflows; repeated landmarks need unique `aria-label`s; dialogs/sheets trap and restore focus; focus order follows visible order; disabled/unavailable actions need reachable explanations; skeleton/busy regions clear `aria-busy` on the same node and preserve/move focus intentionally; validation uses summary focus plus field-level error association. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Keyboard-and-focus-model; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix]

Accessibility is also a FrontComposer framework contract: generated/customized UI must preserve labels, keyboard reachability, focus visibility, live-region parity, reduced-motion, and forced-colors behavior. This story should reuse Fluent UI/FrontComposer behavior and local ChatBot wrappers rather than add a parallel accessibility framework. [Source: Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific-Rules; _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]

### Current Implementation State

Files likely to be updated:

- `src/Hexalith.ChatBot.UI/Design/` already owns metadata contracts for semantic tokens, governed primitives, interaction guardrails, overlay policy, shortcut policy, responsive tiers, touch targets, dense-row retention, and small-screen fallback. New accessibility/focus contracts should follow this one-type-per-file style.
- `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor` currently provides a skip link, shell header, and `<main id="chatbot-main-content" class="chatbot-shell-main" tabindex="-1">`. Preserve the skip target and focusability.
- `src/Hexalith.ChatBot.UI/Components/Routes.razor` uses `<FocusOnNavigate RouteData="@routeData" Selector="h1" />`. Preserve heading-focused navigation.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor` exposes the shell, main region, and optional complementary panel. It currently defaults to `MainLabel="Governed conversation detail"` and `ComplementaryLabel="Governed complementary panel"`. Story 1.18 should make repeated region naming explicit without turning complementary panels into stacked modals.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor` already uses `aria-disabled`, `aria-describedby`, a focusable "Why unavailable?" reason, no `title`, and no native `Disabled` attribute. Preserve that behavior and add tests rather than replacing it.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor` already provides a keyboard-reachable Stop control while streaming, a polite live region, and focus return via `HexalithChatBot.focusElementById`. Preserve it; broader live-region standardization belongs to Story 1.19.
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` is the only current M0 page fixture. It must remain the governed-command path and still dispatch `SubmitGovernedNoteAction`.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs`, `ChatBotGovernedPrimitiveContractTests.cs`, and `ChatBotResponsiveTouchContractTests.cs` are the closest static/contract test homes.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` already tests accessible roles/labels, disabled action reachability, Stop/Cancel focus return, forced colors, responsive behavior, and deterministic browser fallback.

Preserve existing behavior:

- `GovernedOperations.razor` must still dispatch `SubmitGovernedNoteAction`.
- `GovernedOperationService` must still use `ChatBotSurfaceOrigin.Ui`.
- Existing status banners must keep metadata-only wording and stable IDs.
- Existing responsive/touch behavior from Story 1.17 must not regress: no horizontal overflow, visible safe metadata, 44x44 primary touch targets, 24x24 dense-secondary targets, no viewport zoom lock.
- UI must not reference `.Server`, gateway internals, DAPR clients, audit writer, idempotency store, projection store, mailbox, AI provider, CLI/MCP internals, or direct data-plane infrastructure.
- Do not add inline package versions or upgrade Fluent UI, Fluxor, FrontComposer, Playwright, xUnit, bUnit, or .NET.

### Previous Story Intelligence

Story 1.17 completed at baseline `ab529e2` and added reusable responsive/touch contracts, CSS hooks, current-page fixture consumption, and focused contract/E2E coverage. Important implementation learnings for Story 1.18:

- Story 1.17 removed `overflow-x: clip` from `.chatbot-shell-main` because it could hide clipped responsive content while document-level overflow assertions still passed. Story 1.18 focus tests should likewise assert focused elements remain reachable/visible rather than only checking page-level markers.
- `ChatBotSmallScreenFallbackContract.IsComplete` was hardened against null `SafeActions`; follow that defensive public-contract style for any new contract with collections.
- E2E tests should preserve deterministic fallback behavior when local browser/socket permissions are restricted.
- File lists and story documentation must include generated BMAD artifacts touched by the workflow.

[Source: _bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md#Senior-Developer-Review-AI; ab529e2]

Story 1.16 introduced the interaction guardrails that Story 1.18 must build on:

- `ChatBotGovernedAction` is the existing home for disabled-with-reason behavior. Do not create a second disabled-action primitive.
- `ChatBotStreamingStopControl` is the existing home for Stop/Cancel keyboard reachability, polite "Response stopped" announcement, and focus return. Do not add backend cancellation.
- `ChatBotOverlayPolicy` already represents no stacked active modal dialogs/sheets and focus-return requirements for overlays.
- `ChatBotShortcutDefinition` and `ChatBotShortcutPreferenceContract` already encode WCAG 2.1.4 shortcut governance for composer/search/filter/configuration text entry.

[Source: _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md#Completion-Notes-List; src/Hexalith.ChatBot.UI/Design/ChatBotOverlayPolicy.cs; src/Hexalith.ChatBot.UI/Design/ChatBotShortcutDefinition.cs]

Recent git context:

- `ab529e2 feat(story-1.17): Responsive and touch foundation`
- `86f9dd6 feat(story-1.16): Interaction guardrails and streaming stop/cancel behavior`
- `f752df5 feat: Update orchestration status and steps for story 1.15`
- `f3f0e97 feat(story-1.15): Shared governed component primitives`
- `6c292c2 feat(story-1.14): Visual inheritance and semantic token foundation`

Current dirty worktree entry observed during story creation: `_bmad-output/story-automator/orchestration-1-20260531-085840.md`. It is unrelated automation output and must not be reverted.

### Architecture and UX Guardrails

- UI stack is Blazor + Fluent UI v5 RC via FrontComposer, Fluxor state, REST commands/queries, and SignalR projection nudge. Do not add a second component library, CSS framework, JavaScript widget library, or native mobile layer. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- M0 UI surfaces are S1 project conversation view, S2 ambiguous association review, and S3 AI action approval. This story prepares shared accessibility/focus primitives for those surfaces; it does not build those surfaces. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- Preserve the visual inheritance chain: Fluent UI v5 -> FrontComposer -> `DESIGN.md` -> `EXPERIENCE.md`. Use existing token aliases, library focus affordances, forced-colors cues, and governed wrapper CSS; do not invent raw CSS color/focus systems. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Brand-and-Style; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Colors]
- Accessibility floor is contractual: role, label, state, disabled reason, keyboard operation, focus order, live-region discipline, reduced motion, and redaction-safe screen-reader messaging must not be normalized away. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]
- Story 1.19 owns live-region and reduced-motion standardization. Story 1.18 may preserve existing live-region behaviors and represent busy/validation focus contracts, but it should not attempt the full state-to-feedback matrix implementation. [Source: _bmad-output/planning-artifacts/epics.md#Story-1.19-Live-region-and-reduced-motion-behavior]

### Latest Technical Notes

The repo currently pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, `Fluxor` to `6.9.0`, `Microsoft.Playwright` to `1.60.0`, `xunit.v3` to `3.2.2`, and `bunit` to `2.7.2`. Treat root package pins as authoritative; do not upgrade packages in this story. [Source: Directory.Packages.props]

WCAG 2.2 AA includes keyboard and focus requirements relevant to this story, including Focus Not Obscured (Minimum) and Target Size (Minimum). The project already applies stricter product focus/touch rules in UX and design docs; implement those local rules where they exceed the baseline. [Source: https://www.w3.org/WAI/WCAG22/Understanding/focus-not-obscured-minimum.html; https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum]

For disabled-but-discoverable governed actions, `aria-disabled="true"` communicates disabled state without removing the element from the focus order. Unlike native `disabled`, it does not suppress behavior automatically, so event handlers must still fail closed when disabled. [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-disabled]

For modal dialogs, WAI-ARIA APG expects keyboard focus to stay inside the modal dialog while active and to return to the invoking element when the dialog closes. Existing `ChatBotOverlayPolicy` is the right place to encode that requirement mechanically for future governed overlays. [Source: https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/]

For loading/busy regions, `aria-busy` should remain true until updates complete, then be set false so assistive technologies do not announce incomplete updates. For validation, invalid controls should expose `aria-invalid` and link to explanatory messages through ARIA association. [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-busy; https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-invalid]

### Suggested Implementation Shape

Prefer a narrow addition to the existing governed foundation:

```text
src/Hexalith.ChatBot.UI/
  Design/
    ChatBotAccessibilityFloorContract.cs
    ChatBotKeyboardOperationContract.cs
    ChatBotLandmarkContract.cs
    ChatBotFocusReturnContract.cs
    ChatBotBusyRegionContract.cs
    ChatBotValidationErrorContract.cs
  Components/Governed/
    ChatBotConversationShell.razor
    ChatBotGovernedAction.razor
  Components/Layout/
    MainLayout.razor
tests/
  Hexalith.ChatBot.UI.Tests/
    ChatBotAccessibilityFocusContractTests.cs
  Hexalith.ChatBot.UI.E2E.Tests/
    GovernedOperationsVisualFoundationE2ETests.cs
```

This shape is a suggestion, not a mandate. Keep one primary public type per file and file-scoped namespaces for C# helpers. Names should make the governed/accessibility purpose obvious and avoid generic names such as `Focus`, `Region`, `Dialog`, or `Validation`.

### Testing Requirements

- Use xUnit v3 `3.2.2`, Shouldly `4.3.0`, Playwright `1.60.0`, and existing project patterns. No new assertion, browser, accessibility, or component library.
- Prefer deterministic component/static tests for contract coverage and Playwright fixture tests for real keyboard/focus behavior.
- Browser tests should select by role/name or explicit fixture metadata; CSS selectors are acceptable only when asserting active element, contract hooks, or focus/overflow mechanics.
- Test keyboard tab flow through the current governed operations fixture: skip link, main content, page heading/focus target, governed action, status summary, disabled reason where applicable.
- Test that repeated region/landmark roles in fixture markup have unique accessible names.
- Test disabled actions expose `aria-disabled="true"`, expose a reachable reason, do not use `title`, do not use native disabled, and do not invoke handlers when activated.
- Test overlay policy still rejects stacked modal dialog/sheet activation and still requires Escape/focus return for dialog, sheet, popover, evidence drawer, and review panel.
- Test busy-region and validation contracts fail when ids/labels/reasons/message associations are missing or collection properties are null.
- Keep responsive/touch tests from Story 1.17 green.
- Keep architecture boundary tests green if dependencies/project references change.

### Out of Scope

- Feature-specific S1 conversation stream, S2 association review, S3 AI approval panel, candidate row, approval panel, queue row, attachment row, audit timeline, tenant settings UI, command palette UI, localization forms, export/copy/read-aloud affordances, or off-surface redaction implementation.
- Full Story 1.19 live-region and reduced-motion matrix implementation beyond preserving existing Stop/Cancel and status semantics.
- Story 1.20 English/French localization infrastructure.
- Story 1.21 redaction-safe off-surface affordances and recovery patterns.
- Backend commands, gateway stages, DAPR, EventStore, audit/idempotency, OpenAPI/client generation, M365, attachments, AI provider streaming, real cancellation, CLI, MCP, Workers, or production data behavior.
- Upgrading Fluent UI, Fluxor, FrontComposer, .NET, Playwright, xUnit, bUnit, or adding inline package versions.

### Project Structure Notes

- New accessibility/focus contracts: `src/Hexalith.ChatBot.UI/Design/`.
- Existing app shell and focus entry: `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor` and `src/Hexalith.ChatBot.UI/Components/Routes.razor`.
- Governed shell/primitives: `src/Hexalith.ChatBot.UI/Components/Governed/`.
- Current page fixture: `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`.
- Existing focus-return helper: `src/Hexalith.ChatBot.UI/wwwroot/js/chatbot.focus.js`.
- Current submission behavior to preserve: `src/Hexalith.ChatBot.UI/State/GovernedOperations/` and `src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs`.
- Focused UI tests: `tests/Hexalith.ChatBot.UI.Tests/`.
- E2E/static fixture tests: `tests/Hexalith.ChatBot.UI.E2E.Tests/`.
- Boundary tests if dependencies change: `tests/Hexalith.ChatBot.Architecture.Tests/`.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create-Story-Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.18-Accessibility-and-focus-management-floor]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR61]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Brand-and-Style]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Colors]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Keyboard-and-focus-model]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings]
- [Source: _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md]
- [Source: _bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.UI/Components/App.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Routes.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotOverlayPolicy.cs]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotShortcutDefinition.cs]
- [Source: src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs]
- [Source: src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css]
- [Source: src/Hexalith.ChatBot.UI/wwwroot/js/chatbot.focus.js]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- [Source: Directory.Packages.props]
- [Source: https://www.w3.org/WAI/WCAG22/Understanding/focus-not-obscured-minimum.html]
- [Source: https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum]
- [Source: https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/]
- [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-disabled]
- [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-busy]
- [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-invalid]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31T13:39:08+02:00 - Marked sprint status in-progress; preserved existing `baseline_commit: ab529e2`.
- 2026-05-31T13:41:00+02:00 - Red phase: `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` failed at compile because the new accessibility/focus contracts and overlay policy methods did not exist yet.
- 2026-05-31T13:43:00+02:00 - `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` compiled, then aborted in VSTest socket setup with `System.Net.Sockets.SocketException (13): Permission denied`; used compiled xUnit v3 binaries for execution per story instructions.
- 2026-05-31T13:43:00+02:00 - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` passed: Total 39, Failed 0, Skipped 0.
- 2026-05-31T13:43:00+02:00 - `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- 2026-05-31T13:43:00+02:00 - `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` passed: Total 10, Failed 0, Skipped 0. Chrome was available at `/usr/bin/google-chrome`, so browser assertions ran instead of static fallback.
- 2026-05-31T13:44:00+02:00 - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- 2026-05-31T13:44:00+02:00 - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` passed: Total 33, Failed 0, Skipped 0.
- 2026-05-31T13:44:00+02:00 - `git diff --check` passed with no whitespace errors.
- 2026-05-31T13:59:04+02:00 - Senior review auto-fix added the missing disabled-action explanation contract, tightened visible-order/keyboard fixture checks, and documented changed BMAD artifacts.
- 2026-05-31T13:59:04+02:00 - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- 2026-05-31T13:59:04+02:00 - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` passed: Total 40, Failed 0, Skipped 0.
- 2026-05-31T13:59:04+02:00 - `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` passed: Total 12, Failed 0, Skipped 0. Browser startup was unavailable in this review run, so deterministic static fallback assertions ran.
- 2026-05-31T13:59:04+02:00 - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` passed: Total 33, Failed 0, Skipped 0.
- 2026-05-31T13:59:04+02:00 - `git diff --check` passed with no whitespace errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Discovery loaded sprint status, Epic 1 story context, architecture frontend/project-structure sections, UX design/accessibility docs, relevant PRD NFR references, previous stories 1.16/1.17, current UI components/tests, package pins, recent git history, and official W3C/MDN accessibility references.
- Checklist validation applied: story explicitly prevents duplicate primitives, wrong file locations, package upgrades, backend scope creep, and vague accessibility implementation.
- Added UI-owned accessibility/focus contracts for keyboard operation, repeated landmark names, visible focus order, focus return, disabled-action explanation, busy-region focus preservation, and validation error association.
- Extended overlay policy with explicit focus containment, Escape close, and focus-return queries while preserving existing modal stacking and complementary-region behavior.
- Hardened `ChatBotConversationShell` to resolve stable shell/main/complementary labels and expose complementary panels as labelled complementary regions; `GovernedOperations` now renders a non-modal review-context complementary panel.
- Preserved Story 1.16 governed action and streaming stop behavior; tests continue to assert `aria-disabled`, reachable disabled reasons, no native disabled attribute, no tooltip-only behavior, polite stop announcement, and focus return.
- Added focused UI contract tests and E2E/browser fixture coverage for skip-link/main focus, heading/region/complementary/status reachability, unique landmark names, disabled non-activation, busy/validation contracts, overlay focus policy, and package pin preservation.
- Senior review fixed the missing typed disabled-action explanation contract required by AC1 and hardened Story 1.18 E2E/static focus checks to prove visible DOM order plus keyboard progression from skipped main content to the governed action.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31

Outcome: Approved after auto-fixes. No critical issues remain.

Findings fixed automatically:

- [HIGH] AC1 claimed a reusable disabled-action explanation contract, but no typed UI-owned design contract existed for that behavior. Fixed by adding `ChatBotDisabledActionContract` and non-vacuous contract coverage.
- [MEDIUM] The Story 1.18 focus-path fixture test checked landmark presence and manual focus, but did not prove visible DOM order or keyboard progression after skip-link activation. Fixed by asserting skip/main/shell/project/primary/complementary order and Tab progression to the governed action.
- [MEDIUM] The story File List omitted changed BMAD review/test-summary artifacts and the review-added disabled-action contract. Fixed by updating the File List.

Review validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` passed 40/40.
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` passed 12/12.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` passed 33/33.
- `git diff --check` passed.

### File List

- `_bmad-output/implementation-artifacts/1-18-accessibility-and-focus-management-floor.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260531-085840.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Design/ChatBotAccessibilityFloorContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotAccessibilityRequirement.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotBusyRegionContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotDisabledActionContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotFocusReturnContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotFocusSequenceContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotKeyboardOperationContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotLandmarkContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotOverlayPolicy.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotValidationErrorContract.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`

### Change Log

- 2026-05-31 - Implemented Story 1.18 accessibility and focus-management floor; added typed UI contracts, shell/complementary-region semantics, focused contract/E2E coverage, and validation evidence.
- 2026-05-31 - Senior review auto-fixed disabled-action contract coverage, strengthened focus-path fixture validation, documented review findings, and marked the story done.
