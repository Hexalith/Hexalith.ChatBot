---
baseline_commit: 6c16298
---

# Story 1.19: Live-region and reduced-motion behavior

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As an accessibility owner,
I want live-region and reduced-motion behavior standardized,
so that workflow feedback is perceivable without disorienting users.

## Acceptance Criteria

1. **State-to-feedback matrix is encoded as a UI-owned contract.** Given Stories 1.14-1.18 established the governed UI foundation, when Story 1.19 is complete, then `src/Hexalith.ChatBot.UI/Design/` exposes reusable metadata for the UX-DR35 state families: loading/cold load, current-user AI proposal ready, current-user command accepted/projection pending, current-user approval rejected, observed-for-others rejection/queue update, validation error, blocked action, retryable failure, terminal/policy failure, dependency degraded, and background update while reading history. Each contract entry must specify feedback primitive, politeness (`none`, `polite`, `assertive`), focus behavior, repeat/dedup rule, and whether inline status is required. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.19; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix]

2. **Live-region primitives apply the matrix without noisy repeats.** Given `ChatBotStatusBanner`, `ChatBotStreamingStopControl`, busy/loading regions, validation summaries, blocked states, and future proposal/approval surfaces use the foundation, when state changes occur, then the current user's proposal-ready and command-accepted/projection-pending states receive one polite announcement per stable operation/proposal key, current-user rejection receives an assertive announcement plus reachable inline reason, observed-for-others queue/history updates remain inline-only with no live announcement, historical content loaded on initial render does not announce, and re-entry/polling does not repeat the same live message. [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Conversation-and-audit-semantics; https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-live]

3. **Busy and validation behavior from Story 1.18 is completed, not duplicated.** Given `ChatBotBusyRegionContract` and `ChatBotValidationErrorContract` already exist, when this story standardizes live-region behavior, then loading uses `aria-busy="true"` on the busy region and clears it on the same node before loaded content is exposed; focus is preserved or moved to a labelled landing point; validation failures render an error summary before the affected panel, focus the summary, and mark invalid fields with `aria-invalid="true"` plus `aria-describedby` or `aria-errormessage`. Do not create a second busy or validation contract family. [Source: src/Hexalith.ChatBot.UI/Design/ChatBotBusyRegionContract.cs; src/Hexalith.ChatBot.UI/Design/ChatBotValidationErrorContract.cs; https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-busy; https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-invalid]

4. **Reduced-motion policy is encoded and applied through the existing token/CSS layer.** Given the UI renders through Fluent UI v5 and FrontComposer token inheritance, when `prefers-reduced-motion: reduce` is active, then shimmer skeletons, row insertion/reordering animation, streaming-text animation, and non-essential panel transitions are suppressed; queue row insertion/reordering preserves focus and selection; progress/status uses text such as "Scanning attachment", "Projection pending", or equivalent stable labels rather than movement; and forced-colors/status text cues remain intact. [Source: src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Reduced-motion-and-auto-scroll; https://developer.mozilla.org/en-US/docs/Web/CSS/%40media/prefers-reduced-motion; https://www.w3.org/WAI/WCAG21/Techniques/css/C39.html]

5. **Current governed operations fixture consumes the live-region foundation without behavior drift.** Given `GovernedOperations.razor` is the current M0 UI fixture, when a governed note is submitted, pending/completed/failed statuses use the standardized status/live-region contract while preserving `SubmitGovernedNoteAction`, `GovernedOperationService`, `ChatBotSurfaceOrigin.Ui`, metadata-only audit wording, operation identifiers, lifecycle state, audit status, safe next actions, visible focus order, responsive/touch behavior, and forced-colors cues. [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor; src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs; _bmad-output/implementation-artifacts/1-18-accessibility-and-focus-management-floor.md#Current-Implementation-State]

6. **Streaming Stop/Cancel behavior remains the single cancellation announcement mechanism.** Given Story 1.16 already established `ChatBotStreamingStopControl`, when live-region behavior is standardized, then Stop/Cancel remains keyboard reachable, announces "Response stopped" politely once per activation, returns focus to the composer or AI proposal panel, and does not grow into backend cancellation or a second streaming component. [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor; _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md#Completion-Notes-List]

7. **Background updates avoid forced scroll and provide a reachable new-updates affordance.** Given a future conversation stream, audit timeline, or operational queue receives updates while the user is reading older content, when the live-region matrix is applied, then the foundation requires a keyboard-reachable "new updates" affordance, no forced scroll, no historical-content announcement, and no row-motion-only cue under reduced motion. This story may provide contracts and fixture coverage only; it must not build S1 conversation stream, S8 queues, or S9 audit timeline. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Reduced-motion-and-auto-scroll]

8. **Focused tests prove the matrix and motion policy are non-vacuous.** Given later S1/S2/S3 surface stories inherit this foundation, when tests run, then they fail if the state-to-feedback matrix omits a state family, current-user success/rejection politeness is wrong, observed-for-others updates announce live, announcement dedup keys are missing, busy/validation contracts are bypassed or duplicated, reduced-motion CSS is absent, the current governed operations fixture repeats live announcements on polling/re-entry, streaming stop repeats or loses focus return, reduced-motion emulation still leaves non-essential animation enabled, package pins are upgraded, or responsive/accessibility tests from Stories 1.17-1.18 regress. Use xUnit v3, Shouldly, Playwright/static fixture patterns already present, and do not add axe-core, a new component library, or a new animation package. [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs; Directory.Packages.props]

## Tasks / Subtasks

- [x] Define live-region matrix contracts in UI-owned code (AC: 1, 2, 3, 7, 8)
  - [x] Add small typed contracts under `src/Hexalith.ChatBot.UI/Design/` for state family, feedback primitive, live-region politeness, announcement dedup key, inline-status requirement, focus target, and background-update affordance.
  - [x] Reuse `ChatBotBusyRegionContract`, `ChatBotValidationErrorContract`, `ChatBotDisabledActionContract`, and `ChatBotFocusReturnContract`; do not create parallel busy, validation, disabled-action, or focus-return contracts.
  - [x] Encode observed-for-others queue/history updates as inline-only, not `role="status"` or `role="alert"` announcements.
  - [x] Keep contracts metadata-oriented and UI-owned; no server, gateway, DAPR, audit, projection, AI provider, CLI, MCP, Workers, or generated client dependency.

- [x] Standardize status/live-region primitive behavior (AC: 2, 3, 5, 6, 8)
  - [x] Review `ChatBotStatusBanner.razor`, `ChatBotStreamingStopControl.razor`, `ChatBotBlockedState.razor`, `ChatBotGovernedAction.razor`, and `GovernedOperations.razor` before editing.
  - [x] Extend `ChatBotStatusBanner` only as needed to make live politeness explicit and matrix-driven while preserving `role="status"` for polite and `role="alert"` for current-user terminal/assertive feedback.
  - [x] Provide a stable operation/proposal announcement key or equivalent dedup contract so polling and view re-entry cannot re-announce the same message.
  - [x] Preserve `ChatBotStreamingStopControl` as the single Stop/Cancel live-region path and keep the existing "Response stopped" polite announcement and focus return.
  - [x] Do not announce historical messages, audit events, or observed-for-others queue updates on initial load.

- [x] Add reduced-motion token/CSS hooks (AC: 4, 7, 8)
  - [x] Add `@media (prefers-reduced-motion: reduce)` rules to `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
  - [x] Suppress non-essential animation/transition classes used by the governed foundation, including any skeleton/shimmer, row movement, streaming text, and panel transition hooks that exist or are introduced by this story.
  - [x] Preserve focus visibility, forced-colors behavior, status labels, and non-color cues while reducing motion.
  - [x] Ensure progress/status copy is text-based and stable; do not rely on movement, shimmer, auto-scroll, or animated streaming text as the only cue.

- [x] Integrate current governed operations as the fixture consumer (AC: 2, 5, 8)
  - [x] Update `GovernedOperations.razor` only where needed to consume the standardized status/live-region behavior.
  - [x] Preserve `SubmitGovernedNoteAction`, `GovernedOperationService`, `ChatBotSurfaceOrigin.Ui`, operation metadata visibility, metadata-only audit language, and current responsive/focus semantics.
  - [x] Ensure submission, projection-pending, audit-committed, metadata-only history, and failure statuses use the appropriate matrix entries and do not repeat announcements on repeated render/poll simulation.

- [x] Represent future background-update behavior without building feature screens (AC: 7, 8)
  - [x] Add a contract or fixture for keyboard-reachable "new updates" affordance, no forced scroll, preserved focus/selection, and inline-only status for updates belonging to other users.
  - [x] Keep this as foundation metadata/tests; do not implement conversation stream, queue row, audit timeline, association review, AI approval, or tenant configuration screens.

- [x] Add focused non-vacuous tests (AC: 1-8)
  - [x] Add or extend `tests/Hexalith.ChatBot.UI.Tests/` contract tests for matrix completeness, politeness mapping, dedup-key requirement, observed-for-others inline-only behavior, reuse of busy/validation contracts, reduced-motion policy completeness, and package pin preservation.
  - [x] Extend `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` or a sibling fixture to assert runtime `aria-live`/role behavior, no duplicate announcement on repeated render/poll, streaming stop polite single announcement/focus return, and reduced-motion media emulation.
  - [x] Preserve deterministic static fallback behavior for restricted browser/socket environments.
  - [x] Prefer role/name and explicit fixture metadata selectors. Use CSS selectors only for mechanics such as `aria-live`, active element, reduced-motion CSS, animation duration, transition duration, or stable data hooks.

- [x] Verify and document results (AC: 8)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.E2E.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.Architecture.Tests` if dependencies or project references change.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, pass/fail counts, and any browser/socket fallback behavior in this story's Dev Agent Record.

## Dev Notes

### Source Artifact Analysis

Story 1.19 is a UI foundation story. It anchors UX-DR35 and UX-DR38 after the visual, component, interaction, responsive, and focus foundations are in place. It does not ask for association review, AI approval, conversation stream, operational queues, audit timeline, tenant configuration, localization infrastructure, off-surface export/copy behavior, backend cancellation, real AI streaming, or new data-plane behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.19; _bmad-output/planning-artifacts/epics.md#Cross-cutting-acceptance-and-planning-guidance]

The UX package has no mockups by design. Binding behavior comes from `EXPERIENCE.md`: loading uses a busy region, current-user proposal/command success is announced politely once, current-user rejection is assertive with reachable reason, observed-for-others updates stay inline-only, validation uses summary focus plus field-level associations, dependency degradation is scoped, and background updates expose a non-interrupting new-updates affordance with no forced scroll. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix]

Reduced motion is not visual polish. The UX requires `prefers-reduced-motion` to suppress shimmer skeletons, row movement animation, streaming-text animation, and non-essential panel transitions. Queue insertion/reordering must preserve focus and selection, and progress must be text-based. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Reduced-motion-and-auto-scroll]

The PRD scopes WCAG 2.2 AA per increment to the UI surfaces that exist in that increment. M0 surfaces are ambiguous association review, AI action approval, and project conversation view; validation must include automated checks plus keyboard-only and screen-reader review for in-scope surfaces. NFR61-NFR63 specifically require labels/focus/non-color status/error recovery and understandable status/failure/refusal/authorization messages without exposing restricted evidence or raw audit logs. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR61]

### Current Implementation State

Files likely to be updated:

- `src/Hexalith.ChatBot.UI/Design/` already owns metadata contracts for semantic tokens, governed primitives, interaction guardrails, queue loading policy, responsive tiers, touch targets, accessibility/focus, busy regions, disabled actions, and validation errors. New live-region/reduced-motion contracts should follow this one-type-per-file style.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor` currently chooses `role="status"` or `role="alert"` from `IsTerminalForCurrentUser`, but it does not expose an explicit matrix contract, live politeness parameter, or dedup key. Extend this primitive rather than creating a second status/toast component.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor` already provides a keyboard-reachable Stop control, a visually hidden polite live region, "Response stopped", and focus return through `HexalithChatBot.focusElementById`. Preserve it.
- `src/Hexalith.ChatBot.UI/Design/ChatBotBusyRegionContract.cs` and `ChatBotValidationErrorContract.cs` already encode loading and validation focus rules from Story 1.18. Story 1.19 should compose these into the matrix and test them, not replace them.
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` owns token aliases, component layout, focus rings, forced-colors cues, responsive behavior, touch sizing, visually-hidden content, and governed primitive styling. It currently needs an explicit reduced-motion media section.
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` is the current M0 page fixture. It must remain a governed-command path, keep all operation/audit metadata visible, and continue dispatching `SubmitGovernedNoteAction`.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`, `ChatBotInteractionGuardrailContractTests.cs`, and `ChatBotResponsiveTouchContractTests.cs` are the closest contract test homes.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` already covers role/label behavior, disabled reasons, Stop/Cancel focus return, busy/validation fixtures, forced colors, responsive behavior, and deterministic static fallback.

Preserve existing behavior:

- `GovernedOperations.razor` must still dispatch `SubmitGovernedNoteAction`.
- `GovernedOperationService` must still use `ChatBotSurfaceOrigin.Ui`.
- Existing status banners must keep metadata-only wording, stable ids, visible semantic labels, and non-color cues.
- Existing Story 1.17 responsive/touch behavior must not regress: no horizontal overflow, visible safe metadata, 44x44 primary touch targets, 24x24 dense-secondary targets, and no viewport zoom lock.
- Existing Story 1.18 focus behavior must not regress: skip-link/main focus path, unique region names, disabled reason reachability, busy-region focus preservation, validation summary focus, and field message association.
- UI must not reference `.Server`, gateway internals, DAPR clients, audit writer, idempotency store, projection store, mailbox, AI provider, CLI/MCP internals, Workers, or direct data-plane infrastructure.
- Do not add inline package versions or upgrade Fluent UI, Fluxor, FrontComposer, Playwright, xUnit, bUnit, or .NET.

### Previous Story Intelligence

Story 1.18 completed at baseline `6c16298` and added reusable accessibility/focus contracts, shell/complementary-region semantics, busy-region and validation contracts, disabled-action explanation coverage, and focused contract/E2E tests. Important implementation learnings for Story 1.19:

- Story 1.18 explicitly left the full live-region matrix to Story 1.19. Do not treat its busy/validation contracts as the whole matrix.
- `ChatBotDisabledActionContract`, `ChatBotBusyRegionContract`, and `ChatBotValidationErrorContract` are now established public design contracts; reuse them in matrix entries.
- The current E2E suite can run browser assertions when Chrome/socket access is available and deterministic static fallbacks when restricted. Preserve that pattern.
- Senior review added missing disabled-action contract coverage and hardened visible-order/keyboard fixture checks. Story 1.19 tests should be equally non-vacuous: prove roles, `aria-live`/alert behavior, dedup, focus return, and reduced-motion mechanics rather than only checking files exist.

[Source: _bmad-output/implementation-artifacts/1-18-accessibility-and-focus-management-floor.md#Completion-Notes-List; 6c16298]

Story 1.17 established responsive/touch constraints that Story 1.19 must preserve:

- Do not add `overflow-x: clip` or similar clipping that can hide content while page-level overflow checks pass.
- Keep phone/tablet controls touch-sized and safety-critical labels visible.
- Reduced-motion changes must not break forced-colors, focus rings, wrapping, or small-screen labelled rows.

[Source: _bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md#Senior-Developer-Review-AI; ab529e2]

Story 1.16 established interaction and streaming guardrails:

- `ChatBotGovernedAction` is the existing home for enabled/disabled/not-applicable critical actions with reachable reasons. Do not create a new guarded-action primitive for rejection/blocked reasons.
- `ChatBotStreamingStopControl` is the existing home for Stop/Cancel keyboard reachability, the polite "Response stopped" announcement, and focus return. Do not add real backend cancellation.
- The senior review fixed live-region repeat behavior for Stop/Cancel; do not regress that by re-announcing on every render.

[Source: _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md#Completion-Notes-List; 86f9dd6]

Recent git context:

- `6c16298 feat(story-1.18): Accessibility and focus management floor`
- `ab529e2 feat(story-1.17): Responsive and touch foundation`
- `86f9dd6 feat(story-1.16): Interaction guardrails and streaming stop/cancel behavior`
- `f752df5 feat: Update orchestration status and steps for story 1.15`
- `f3f0e97 feat(story-1.15): Shared governed component primitives`

Current dirty worktree entry observed during story creation: `_bmad-output/story-automator/orchestration-1-20260531-085840.md`. It is unrelated automation output and must not be reverted.

### Architecture and UX Guardrails

- UI stack is Blazor + Fluent UI v5 RC via FrontComposer, Fluxor state, REST commands/queries, and SignalR projection nudge. Do not add a second component library, CSS framework, JavaScript widget library, animation library, or native mobile layer. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- M0 UI surfaces are S1 project conversation view, S2 ambiguous association review, and S3 AI action approval. This story prepares shared feedback/motion primitives for those surfaces; it does not build those surfaces. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- Preserve the visual inheritance chain: Fluent UI v5 -> FrontComposer -> `DESIGN.md` -> `EXPERIENCE.md`. Use existing token aliases, spacing/radius/type scales, focus rings, forced-colors cues, governed wrapper CSS, and status labels. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components; Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific-Rules]
- Accessibility is contractual in FrontComposer customization: generated/customized UI must preserve labels, keyboard reachability, focus visibility, live-region parity, reduced-motion, and forced-colors behavior. [Source: Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific-Rules]
- Status toasts/banners are transition feedback only; persistent operational states must live on the relevant surface. Do not turn transient live announcements into the only state record. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix]

### Latest Technical Notes

The repo currently pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, `Fluxor` to `6.9.0`, `Microsoft.Playwright` to `1.60.0`, `xunit.v3` to `3.2.2`, and `bunit` to `2.7.2`. Treat root package pins as authoritative; do not upgrade packages in this story. [Source: Directory.Packages.props]

Web-verified on 2026-05-31: MDN documents `aria-live="polite"` as low-priority notification that generally waits for a graceful opportunity, and `aria-live="assertive"` as urgent notification. Use the project matrix to decide when each is allowed; do not use assertive for routine background or observed-for-others updates. [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-live]

Web-verified on 2026-05-31: MDN documents `aria-busy` as a way to indicate an element is being modified, commonly in live-region scenarios; the local UX rule is stricter and requires clearing `aria-busy` on the same node before content swap is treated as settled. [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-busy; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix]

Web-verified on 2026-05-31: MDN documents `prefers-reduced-motion` as a media feature for detecting a user request to minimize non-essential motion; W3C technique C39 describes using the media query to prevent motion that can trigger vestibular disorders. Implement the local UX rule through `@media (prefers-reduced-motion: reduce)` and stable status text. [Source: https://developer.mozilla.org/en-US/docs/Web/CSS/%40media/prefers-reduced-motion; https://www.w3.org/WAI/WCAG21/Techniques/css/C39.html]

### Suggested Implementation Shape

Prefer a narrow addition to the existing governed foundation:

```text
src/Hexalith.ChatBot.UI/
  Design/
    ChatBotFeedbackStateFamily.cs
    ChatBotFeedbackPrimitive.cs
    ChatBotLiveRegionPoliteness.cs
    ChatBotStateFeedbackContract.cs
    ChatBotStateFeedbackMatrix.cs
    ChatBotReducedMotionContract.cs
    ChatBotBackgroundUpdateContract.cs
  Components/Governed/
    ChatBotStatusBanner.razor
    ChatBotStreamingStopControl.razor
  Components/Pages/
    GovernedOperations.razor
  wwwroot/css/
    chatbot.tokens.css
tests/
  Hexalith.ChatBot.UI.Tests/
    ChatBotLiveRegionReducedMotionContractTests.cs
  Hexalith.ChatBot.UI.E2E.Tests/
    GovernedOperationsVisualFoundationE2ETests.cs
```

This shape is a suggestion, not a mandate. Keep one primary public type per file and file-scoped namespaces for C# helpers. Names should make the governed/accessibility purpose obvious and avoid generic names such as `Notification`, `Animation`, `Region`, or `Message`.

### Testing Requirements

- Use xUnit v3 `3.2.2`, Shouldly `4.3.0`, Playwright `1.60.0`, and existing project patterns. No new assertion, browser, accessibility, component, or animation library.
- Prefer deterministic component/static tests for contract coverage and Playwright fixture tests for actual role/live-region/focus/motion behavior.
- Browser tests should select by role/name or explicit fixture metadata; CSS selectors are acceptable only when asserting `aria-live`, `aria-busy`, active element, animation/transition duration, reduced-motion media behavior, or stable data hooks.
- Test every matrix state family has exactly one policy entry and no unknown default.
- Test current-user proposal-ready and command-accepted/projection-pending map to one polite announcement with a stable dedup key.
- Test current-user approval rejected maps to assertive feedback plus reachable inline reason.
- Test observed-for-others updates and initial historical content map to inline-only/no live announcement.
- Test busy-region entries reuse `ChatBotBusyRegionContract` and validation entries reuse `ChatBotValidationErrorContract`.
- Test `ChatBotStatusBanner` has explicit live behavior and still preserves semantic status labels, metadata-only text, and forced-colors cues.
- Test reduced-motion through static CSS assertions and, when browser is available, Playwright reduced-motion emulation.
- Keep Story 1.17 responsive/touch tests and Story 1.18 accessibility/focus tests green.
- Keep architecture boundary tests green if dependencies/project references change.

### Out of Scope

- Feature-specific S1 conversation stream, S2 association review, S3 AI approval panel, candidate row, approval panel, queue row, attachment row, audit timeline, tenant settings UI, command palette UI, localization forms, export/copy/download/read-aloud affordances, or off-surface redaction implementation.
- Backend commands, gateway stages, DAPR, EventStore, audit/idempotency, OpenAPI/client generation, M365, attachments, AI provider streaming, real cancellation, CLI, MCP, Workers, or production data behavior.
- Replacing Story 1.16 `ChatBotStreamingStopControl`, Story 1.17 responsive/touch contracts, or Story 1.18 focus/busy/validation contracts.
- Adding axe-core, a new UI/component library, a new animation library, new package pins, or package upgrades.
- Story 1.20 English/French localization infrastructure.
- Story 1.21 redaction-safe off-surface affordances and recovery patterns.

### Project Structure Notes

- New feedback/motion contracts: `src/Hexalith.ChatBot.UI/Design/`.
- Status/live primitive: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor`.
- Existing Stop/Cancel live region: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor`.
- Existing focus-return helper: `src/Hexalith.ChatBot.UI/wwwroot/js/chatbot.focus.js`.
- CSS/token work: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Current page fixture: `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`.
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
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.19-Live-region-and-reduced-motion-behavior]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR61]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Reduced-motion-and-auto-scroll]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings]
- [Source: _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md]
- [Source: _bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md]
- [Source: _bmad-output/implementation-artifacts/1-18-accessibility-and-focus-management-floor.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotBusyRegionContract.cs]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotValidationErrorContract.cs]
- [Source: src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css]
- [Source: src/Hexalith.ChatBot.UI/wwwroot/js/chatbot.focus.js]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- [Source: Directory.Packages.props]
- [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-live]
- [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-busy]
- [Source: https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Reference/Attributes/aria-invalid]
- [Source: https://developer.mozilla.org/en-US/docs/Web/CSS/%40media/prefers-reduced-motion]
- [Source: https://www.w3.org/WAI/WCAG21/Techniques/css/C39.html]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31T14:10:11+02:00 - Marked story and sprint status `in-progress`; preserved existing `baseline_commit: 6c16298`.
- 2026-05-31T14:11-14:14+02:00 - Added UI-owned live-region matrix, reduced-motion/background-update contracts, `ChatBotStatusBanner` live metadata, governed operations matrix consumption, reduced-motion CSS, and focused UI/E2E tests.
- 2026-05-31T14:15+02:00 - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- 2026-05-31T14:15+02:00 - `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` first run failed 48/49 due an existing static guardrail expecting the legacy terminal role fallback expression in `ChatBotStatusBanner`.
- 2026-05-31T14:15+02:00 - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` passed: 14/14, browser path used; no restricted browser/socket fallback.
- 2026-05-31T14:16+02:00 - Restored the terminal fallback shape while keeping matrix-driven politeness.
- 2026-05-31T14:17+02:00 - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- 2026-05-31T14:17+02:00 - `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` passed: 49/49.
- 2026-05-31T14:17+02:00 - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` passed: 14/14, browser path used; no restricted browser/socket fallback.
- 2026-05-31T14:18+02:00 - Final chained validation passed: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` (0 warnings, 0 errors), `Hexalith.ChatBot.UI.Tests` (49/49), `Hexalith.ChatBot.UI.E2E.Tests` (14/14), and `git diff --check` (no whitespace errors).
- 2026-05-31T14:18+02:00 - `Hexalith.ChatBot.Architecture.Tests` compiled in the solution build but its binary was not executed because no dependencies or project references changed.
- 2026-05-31T14:23+02:00 - Senior Developer Review (AI) loaded the story, workflow/checklist, sprint status, git status/diff, planning references, implementation files, UI tests, E2E fixtures, and source contract files. MCP resource lookup returned no configured resources; external web fallback was not needed because the story already carried the dated MDN/W3C references and no new external API behavior was changed.
- 2026-05-31T14:30+02:00 - Auto-fixed review findings: added per-circuit live-announcement deduplication, routed blocked states through the feedback matrix, and mapped retryable submit failures to polite retryable status instead of assertive dependency degradation.
- 2026-05-31T14:31+02:00 - Review validation failed before test expectation cleanup: `Hexalith.ChatBot.UI.Tests` 51/52 due legacy terminal-alert assertion; `Hexalith.ChatBot.UI.E2E.Tests` 14/15 due fallback fixture expecting `role="alert"` for retryable failure.
- 2026-05-31T14:34+02:00 - Final review validation passed: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` (0 warnings, 0 errors), `Hexalith.ChatBot.UI.Tests` (52/52), `Hexalith.ChatBot.UI.E2E.Tests` (15/15), and `git diff --check` (no whitespace errors). Browser path used for the E2E run; no restricted browser/socket fallback.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Discovery loaded sprint status, Epic 1 story context, architecture frontend/project-structure sections, UX design/accessibility docs, relevant PRD NFR references, previous stories 1.16-1.18, current UI components/tests, package pins, recent git history, FrontComposer project context, and web-verified MDN/W3C live-region/reduced-motion references.
- Checklist validation applied: story explicitly prevents duplicate busy/validation/disabled/focus primitives, wrong file locations, package upgrades, backend scope creep, noisy announcements, motion-only status cues, and vague accessibility implementation.
- Added the UX-DR35 state-to-feedback matrix as UI-owned typed design metadata, including feedback primitive, politeness, focus behavior, dedup/repeat policy, stable key source, inline-status requirement, existing-contract composition, background-update affordance metadata, and reduced-motion policy metadata.
- Extended `ChatBotStatusBanner` with explicit matrix-driven live behavior, `aria-live`, stable announcement keys, repeat-rule metadata, and inline-only support while preserving legacy terminal alert/status fallback behavior.
- Updated `GovernedOperations.razor` to consume the matrix for submitting, projection pending/complete, audit committed, metadata-only history, and failure states without changing `SubmitGovernedNoteAction`, operation metadata, audit wording, responsive/focus semantics, or service behavior.
- Added reduced-motion CSS for governed shimmer/skeleton, row motion, streaming text, panel transition, status, governed action, and streaming stop hooks while preserving forced-colors and focus behavior.
- Added focused xUnit and Playwright/static fixture coverage for matrix completeness, politeness mapping, dedup keys, inline-only observed/history updates, busy/validation reuse, reduced-motion policy, package pins, runtime `aria-live`/role behavior, repeated render dedup metadata, streaming stop single announcement/focus return, and reduced-motion emulation.
- Senior Developer Review (AI) fixed three verified issues: the status banner now has scoped announcement-key suppression instead of metadata-only dedup, `ChatBotBlockedState` consumes the matrix instead of ad hoc roles, and retryable governed-note submission failures announce politely as retryable status while keeping visible danger styling and retry availability.

### File List

- `_bmad-output/implementation-artifacts/1-19-live-region-and-reduced-motion-behavior.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Program.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotAnnouncementDedupRule.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotBackgroundUpdateContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotFeedbackFocusBehavior.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotFeedbackPrimitive.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotFeedbackStateFamily.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotLiveRegionPoliteness.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotReducedMotionContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotStateFeedbackContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotStateFeedbackMatrix.cs`
- `src/Hexalith.ChatBot.UI/Services/ChatBotAnnouncementDeduplicationState.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLiveRegionReducedMotionContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31

Outcome: Approved after automatic fixes. Story status set to `done`; sprint status synced to `done`.

Findings fixed:

- **[High][AC2, AC8] Stable announcement keys were metadata-only, so polling/re-entry could still reinsert the same live-region message.** Fixed by adding scoped `ChatBotAnnouncementDeduplicationState`, registering it in UI DI, and making `ChatBotStatusBanner` suppress repeated stable-key live roles while preserving visible inline status and metadata.
- **[High][AC2] `ChatBotBlockedState` bypassed the feedback matrix with ad hoc status/alert role selection.** Fixed by deriving `BlockedAction` versus `TerminalPolicyFailure` from the matrix and exposing matrix live, repeat-rule, and announcement-key metadata on the primitive.
- **[Medium][AC5] Retryable governed-note submission failures were mapped to `DependencyDegraded` and forced assertive alert behavior despite offering "try again."** Fixed by mapping the fixture failure to `RetryableFailure` with polite live behavior while preserving danger styling and retry availability.

Git/story File List discrepancies noted:

- `_bmad-output/implementation-artifacts/tests/test-summary.md` and `_bmad-output/story-automator/orchestration-1-20260531-085840.md` are modified automation artifacts but are not application source and were excluded from code review per workflow scope.
- The review added source/test files and updated this File List accordingly.

### Change Log

- 2026-05-31 - Created Story 1.19 live-region and reduced-motion behavior context and marked it ready for dev.
- 2026-05-31 - Implemented UI-owned live-region matrix, reduced-motion/background-update contracts, governed operations consumption, and focused contract/E2E validation.
- 2026-05-31 - Senior Developer Review (AI) auto-fixed live-announcement deduplication, blocked-state matrix consumption, and retryable failure politeness; status set to done.
