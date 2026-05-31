---
baseline_commit: 86f9dd6
---

# Story 1.17: Responsive and touch foundation

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a frontend engineer,
I want responsive and touch foundations established,
so that desktop work, tablet review, and phone triage use the same governed patterns.

## Acceptance Criteria

1. **Responsive foundation contracts exist in UI-owned code.** Given Story 1.16's governed UI foundation, when Story 1.17 is complete, then `src/Hexalith.ChatBot.UI` exposes reusable responsive contracts/tokens/classes for desktop, tablet, and phone behavior. Desktop remains the primary full-workflow surface, tablet may stack conversation/detail/panels while keeping association and approval flows complete, and phone supports triage without hiding safety-critical state. This must extend the existing `Design/`, `Components/Governed/`, and `wwwroot/css/chatbot.tokens.css` foundation; it must not create feature-specific S1/S2/S3 screens. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.17; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Responsive & Platform]

2. **Current shell and governed primitives reflow predictably across desktop, tablet, and phone widths.** Given `ChatBotConversationShell`, `ChatBotProjectContextHeader`, status banners, chips, actor badges, blocked states, guarded actions, and streaming Stop/Cancel controls render on different viewport widths, when the layout crosses desktop, tablet, and phone test widths, then content uses constrained grids/flex wrapping, preserves readable order, avoids horizontal page overflow, and keeps project identity, state, reason, safe action, and status visible. Existing desktop behavior must remain stable, including the two-column shell at wider widths. [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor; src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Responsive behavior]

3. **Small-screen fallback is represented mechanically.** Given a future workflow is too dense for phone, when a responsive contract marks it as phone-limited, then the foundation must preserve a read-only summary, current status, safe approve/reject/defer/confirm actions where applicable, copy/share handoff link metadata, and "open on larger screen" guidance. Dense editing/admin-only controls must be disabled or hidden only through governed state with a reachable explanation; tooltip-only explanations are not allowed. Draft or filter state preservation must be represented as required metadata for later routing/handoff work. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Responsive & Platform; _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md#Completion Notes List]

4. **Touch target rules are explicit and test-covered.** Given approval, association, filter, attachment, timeline, search, drawer-close, destructive, guarded action, and streaming Stop/Cancel controls render for phone/tablet, when touch sizing rules apply, then primary touch targets are at least `44x44` CSS px where layout allows, and compact dense-row secondary controls are at least `24x24` CSS px or have equivalent spacing. Destructive and approval controls must not rely on compact-only sizing on phone/tablet. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.17; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings; https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html]

5. **Collapsed dense rows keep safety labels.** Given a future candidate row, approval row, queue row, attachment row, or audit timeline item collapses from table/dense layout into a phone-sized row, when it renders through the foundation contracts, then visible labels for project, actor, risk, state, confidence, time, reason, and next action are preserved or explicitly marked as must-move-to-detail. Raw IDs and secondary timestamps may collapse first; safety-critical status/action/reason content must not disappear. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Cognitive-load guardrails; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Localization]

6. **The current governed operations page consumes the foundation without behavior drift.** Given `GovernedOperations.razor` is the current M0 UI surface, when this story updates it, then it still dispatches `SubmitGovernedNoteAction`, `GovernedOperationService` still submits through `IChatBotClient` with `ChatBotSurfaceOrigin.Ui`, and operation ID, command ID, lifecycle state, completion status, audit status, safe next actions, and metadata-only audit history remain visible at desktop, tablet, and phone widths. Responsive changes must not create a fake chat surface, backend cancellation behavior, association workflow, approval workflow, or queue workflow. [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor; src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs; _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]

7. **Responsive metadata and viewport behavior remain web-native and accessible.** Given the app runs as responsive Blazor/FrontComposer web, when rendered on narrow screens, then the existing viewport meta remains present, layout adapts through CSS/contracts, and the implementation does not disable user zoom, introduce native-mobile assumptions, or treat CLI/MCP as visual breakpoints. [Source: src/Hexalith.ChatBot.UI/Components/App.razor; _bmad-output/planning-artifacts/architecture.md#Frontend Architecture; https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/meta/name/viewport]

8. **Focused tests prove the responsive/touch foundation is non-vacuous.** Given later S1/S2/S3 stories depend on this foundation, when tests run, then they fail if responsive contracts are missing, breakpoint tiers are absent or unordered, phone fallback metadata omits summary/status/safe actions/handoff guidance/reachable reason, touch target constants regress below `44x44` or `24x24`, destructive/approval actions can use compact-only sizing on phone/tablet, collapsed dense rows can drop safety-critical labels, `GovernedOperations.razor` overflows or loses metadata at mobile fixture widths, or package versions are upgraded. Use xUnit v3, Shouldly, Playwright/static fixture patterns already present, and do not add a new UI library. [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs; Directory.Packages.props]

## Tasks / Subtasks

- [x] Define responsive and touch contracts in UI-owned code (AC: 1, 3, 4, 5, 8)
  - [x] Add small typed contracts under `src/Hexalith.ChatBot.UI/Design/` for viewport tier, responsive surface capability, small-screen fallback metadata, touch target class, and dense-row label retention.
  - [x] Encode desktop/tablet/phone semantics from UX-DR42 without making CLI/MCP visual breakpoints.
  - [x] Encode primary `44x44` and dense-secondary `24x24` touch target thresholds as named constants; include destructive/approval phone/tablet restrictions.
  - [x] Keep contracts metadata-oriented and UI-owned; no server, gateway, DAPR, audit, projection, AI provider, CLI, MCP, or generated client dependency.

- [x] Extend token CSS for responsive shell and touch sizing (AC: 1, 2, 4, 5, 7, 8)
  - [x] Add semantic custom properties/classes in `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` for responsive max widths, panel widths, touch target sizes, dense target spacing, and small-screen labelled-row behavior.
  - [x] Preserve current Fluent UI/FrontComposer token inheritance and existing forced-colors cues; do not introduce raw color palettes or a second design system.
  - [x] Ensure the shell, header, definition lists, command bar, status group, guarded action, and streaming Stop/Cancel can wrap or stack without horizontal document overflow.
  - [x] Keep `meta name="viewport" content="width=device-width, initial-scale=1.0"` in `App.razor`; do not add `user-scalable=no`, `maximum-scale`, or native app viewport assumptions.

- [x] Update governed shell/primitives only where foundation behavior belongs (AC: 2, 4, 5, 6)
  - [x] Update `ChatBotConversationShell.razor` only if additional tier metadata or CSS hooks are needed; preserve labelled project context, main region, and complementary panel semantics.
  - [x] Update `ChatBotProjectContextHeader`, chips, actor badge, blocked state, guarded action, and streaming Stop/Cancel styling only to meet responsive/touch rules; keep their existing accessible names, roles, and guardrail behavior.
  - [x] Ensure disabled-with-reason and blocked-state explanations remain reachable on small screens.
  - [x] Do not add feature-specific candidate rows, approval panels, queue rows, audit timelines, tenant settings, or phone-only business workflows.

- [x] Represent small-screen fallback and labelled-row behavior (AC: 3, 5, 8)
  - [x] Provide a reusable contract for phone fallback content: read-only summary, status, safe actions, handoff/copy/share metadata, larger-screen guidance, preserved draft/filter state marker, and reachable explanation.
  - [x] Provide a reusable contract or helper for dense row collapse that marks safety-critical fields as must-keep/must-move-to-detail.
  - [x] Ensure raw IDs, secondary timestamps, and repeated context are the first collapse candidates, not actor/risk/state/confidence/reason/next action.

- [x] Integrate the current governed operations page as the first fixture consumer (AC: 2, 6, 7)
  - [x] Update `GovernedOperations.razor` markup/classes only as needed to prove responsive behavior while preserving `SubmitGovernedNoteAction`.
  - [x] Preserve `GovernedOperationService` and `ChatBotSurfaceOrigin.Ui`; this story should not alter command submission, Fluxor effects/reducers, service contracts, or backend behavior.
  - [x] Verify operation metadata and audit history remain visible and readable at desktop, tablet, and phone fixture widths.

- [x] Add focused non-vacuous tests (AC: 1-8)
  - [x] Add or extend `tests/Hexalith.ChatBot.UI.Tests/` contract tests for viewport tiers, fallback metadata, touch target constants, dense-row required labels, package pin preservation, and no CLI/MCP-as-breakpoint behavior.
  - [x] Extend `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` or add a sibling fixture test to render desktop/tablet/phone widths and assert no horizontal document overflow, visible safe metadata, reachable disabled reason, and minimum touch target dimensions.
  - [x] Use accessible role/label selectors or explicit fixture metadata for user behavior; avoid CSS class selectors except when testing CSS contract mechanics directly.
  - [x] Preserve deterministic browser fallback behavior for restricted local browser/socket environments.

- [x] Verify and document results (AC: 8)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.E2E.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.Architecture.Tests` if dependencies or project references change.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, pass/fail counts, and any browser/socket fallback behavior in this story's Dev Agent Record.

## Dev Notes

### Source Artifact Analysis

Story 1.17 is a UI foundation story. It anchors UX-DR42 through UX-DR44 before later S1/S2/S3 feature screens are built. It does not ask for association review, AI approval, operational queues, audit investigation, tenant configuration, native mobile, CLI/MCP UI breakpoints, or backend changes. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.17; _bmad-output/planning-artifacts/epics.md#Cross-cutting acceptance & planning guidance]

The UX package has no mockups by design. Binding requirements come from `EXPERIENCE.md`: desktop/laptop is the primary full-workflow surface; tablet may stack while keeping review flows complete; phone supports reading, status lookup, simple AI request, and safe approve/reject/defer/confirm triage. When phone is too dense, the fallback must keep summary, status, safe actions, handoff guidance, reachable explanations, and state preservation. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Responsive & Platform]

Touch sizing is not optional polish. The UX and accessibility review both call out `44x44` CSS px for phone/tablet primary controls where layout allows and `24x24` CSS px or equivalent spacing for compact dense controls. Approval and destructive controls cannot shrink to compact-only sizing on phone/tablet. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings; https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html]

The responsive foundation must protect safety-critical information during collapse. Dense rows should reflow to labelled rows on small screens without dropping labels, state, reason, safe action, actor, risk, confidence, or next action. Localization guidance already identifies raw IDs, secondary timestamps, low-priority metadata, and repeated project/tenant context as the first collapse candidates; actor, risk, state, confidence, next action, and safe recovery reason are must-keep or must-move-to-detail. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Cognitive-load guardrails; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Localization]

### Current Implementation State

Files likely to be updated:

- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` currently owns semantic token aliases, shell/page/header/status/chip/action layout, forced-colors cues, and initial responsive rules. It already has a wide-shell two-column rule at `min-width: 900px` and a project-context header one-column rule at `max-width: 760px`. This story should extend that foundation with explicit touch sizing and stronger responsive behavior rather than replacing it.
- `src/Hexalith.ChatBot.UI/Components/App.razor` already contains `<meta name="viewport" content="width=device-width, initial-scale=1.0" />` and loads `css/chatbot.tokens.css`. Keep both. Do not disable zoom.
- `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor` currently provides the skip link, header, and main content shell. Any responsive update must preserve the skip link and `main` target.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor` exposes project context, main region, and optional complementary panel. Preserve these labelled regions; do not turn the complementary panel into a modal stack.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor` and `ChatBotStreamingStopControl.razor` are the first critical-action/touch candidates. Preserve their current accessible labels, reachable disabled reason behavior, and Stop/Cancel live-region/focus-return semantics.
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` is the only current M0 page fixture. It must remain a governed-command path and keep all operation/audit metadata visible at small widths.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs` and `ChatBotInteractionGuardrailContractTests.cs` are the closest static/contract test homes.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` already uses Playwright with deterministic fallback and role/name selectors. Extend this pattern for width/touch checks.

Preserve existing behavior:

- `GovernedOperations.razor` must still dispatch `SubmitGovernedNoteAction`.
- `GovernedOperationService` must still use `ChatBotSurfaceOrigin.Ui`.
- Existing status banners must keep metadata-only wording and stable IDs.
- UI must not reference `.Server`, gateway internals, DAPR clients, audit writer, idempotency store, projection store, mailbox, AI provider, CLI/MCP internals, or direct data-plane infrastructure.
- Do not add inline package versions or upgrade Fluent UI, Fluxor, FrontComposer, Playwright, xUnit, bUnit, or .NET.

### Previous Story Intelligence

Story 1.16 completed at baseline `86f9dd6` and introduced reusable interaction guardrails:

- `ChatBotGovernedAction` provides critical-action enabled/disabled/not-applicable behavior with reachable disabled reasons. Story 1.17 should add touch sizing and wrap/reflow behavior around it, not duplicate critical-action logic.
- `ChatBotStreamingStopControl` provides UI-only Stop/Cancel with a polite `Response stopped` announcement and focus return. Story 1.17 should ensure it remains stable and touch-sized on phone/tablet, not add backend cancellation.
- `ChatBotInteractionGuardrailContract`, overlay policy, shortcut contract, and queue loading policy are already metadata-oriented. Follow that style for responsive/touch contracts.
- Tests should remain non-vacuous and exact: constants/enums, file existence, role behavior, no hover-only dependency, forced-colors cues, deterministic E2E fallback, and page integration.
- The senior review fixed broken `aria-describedby`, live-region repeat announcements, overlay Escape/focus-return coverage, and file list accuracy. Do not regress these behaviors.

[Source: _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md#Completion Notes List; 86f9dd6]

Recent git context:

- `86f9dd6 feat(story-1.16): Interaction guardrails and streaming stop/cancel behavior`
- `f752df5 feat: Update orchestration status and steps for story 1.15`
- `f3f0e97 feat(story-1.15): Shared governed component primitives`
- `6c292c2 feat(story-1.14): Visual inheritance and semantic token foundation`

Current dirty worktree entry observed during story creation: `_bmad-output/story-automator/orchestration-1-20260531-085840.md`. It is unrelated automation output and must not be reverted.

### Architecture and UX Guardrails

- UI stack is Blazor + Fluent UI v5 RC via FrontComposer, Fluxor state, REST commands/queries, and SignalR projection nudge. Do not add a second component library, CSS framework, JavaScript layout framework, or native mobile layer. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- M0 UI surfaces are S1 project conversation view, S2 ambiguous association review, and S3 AI action approval. This story prepares shared layout/touch primitives for those surfaces; it does not build those surfaces. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- CLI/MCP are separate governed command surfaces with equivalent backend state transitions. Responsive UI work must not describe them as mobile/tablet breakpoints or create UI bypass affordances. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Responsive & Platform]
- Preserve the visual inheritance chain: Fluent UI v5 -> FrontComposer -> `DESIGN.md` -> `EXPERIENCE.md`. Use existing token aliases and spacing/radius/type scales: 4/8/12/16/24 px spacing, compact 8, comfortable 12, panel gap 16, row gap 8, radius sm 4/md 8/lg 12. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Visual posture; _bmad-output/planning-artifacts/epics.md#UX Design Requirements]
- Accessibility remains contractual: labels, keyboard reachability, focus visibility, live-region behavior, reduced motion, and forced-colors cues must not be normalized away while adapting layout. [Source: Hexalith.FrontComposer/_bmad-output/project-context.md#Critical Implementation Rules]

### Latest Technical Notes

The repo currently pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, `Fluxor` to `6.9.0`, `Microsoft.Playwright` to `1.60.0`, `xunit.v3` to `3.2.2`, and `bunit` to `2.7.2`. Treat root package pins as authoritative; do not upgrade packages in this story. [Source: Directory.Packages.props]

WCAG 2.2 SC 2.5.8 defines the AA pointer target floor as at least `24x24` CSS px, with spacing exceptions. ChatBot's UX uses a stricter product rule for phone/tablet primary controls: `44x44` CSS px where layout allows, while dense secondary controls must still satisfy the WCAG AA floor or equivalent spacing. [Source: https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Responsive & Platform]

The existing viewport meta is the right web-native responsive baseline. Keep `width=device-width, initial-scale=1.0`; do not add zoom-disabling values. [Source: https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/meta/name/viewport; src/Hexalith.ChatBot.UI/Components/App.razor]

### Suggested Implementation Shape

Prefer a narrow addition to the existing governed foundation:

```text
src/Hexalith.ChatBot.UI/
  Design/
    ChatBotViewportTier.cs
    ChatBotResponsiveSurfaceCapability.cs
    ChatBotSmallScreenFallbackContract.cs
    ChatBotTouchTarget.cs
    ChatBotDenseRowField.cs
    ChatBotDenseRowCollapseContract.cs
  wwwroot/css/
    chatbot.tokens.css
tests/
  Hexalith.ChatBot.UI.Tests/
    ChatBotResponsiveTouchContractTests.cs
  Hexalith.ChatBot.UI.E2E.Tests/
    GovernedOperationsVisualFoundationE2ETests.cs
```

This shape is a suggestion, not a mandate. Keep one type per file and file-scoped namespaces for C# helpers. Names should make the governed/responsive purpose obvious and avoid generic names such as `Breakpoint`, `Mobile`, `Row`, or `Layout`.

### Testing Requirements

- Use xUnit v3 `3.2.2`, Shouldly `4.3.0`, Playwright `1.60.0`, and existing project patterns. No new assertion library.
- Prefer deterministic component/static tests for contract coverage and Playwright fixture tests for actual viewport/touch/layout behavior.
- Browser tests should select by role/name or explicit fixture metadata; CSS class selectors are acceptable only when measuring CSS contract mechanics such as touch target dimensions or overflow.
- Test at least three fixture widths that represent desktop, tablet, and phone. The exact pixel values may be implementation-owned, but they must be named and ordered in the contract tests.
- Test no horizontal document overflow on the current governed operations fixture at phone width.
- Test minimum dimensions or equivalent spacing for primary and dense touch targets.
- Test that `App.razor` keeps the viewport meta and does not disable zoom.
- Keep architecture boundary tests green if dependencies/project references change.

### Out of Scope

- Native mobile app behavior, service workers, install prompts, mobile OS integrations, or viewport zoom disabling.
- Feature-specific S1 conversation stream, S2 association review, S3 AI approval panel, candidate row, approval panel, queue row, attachment row, audit timeline, tenant settings UI, command palette, export/copy/download/read-aloud affordances, or localization infrastructure.
- Backend commands, gateway stages, DAPR, EventStore, audit/idempotency, OpenAPI/client generation, M365, attachments, AI provider streaming, real cancellation, CLI, MCP, Workers, or production data behavior.
- Full Story 1.18 focus-management floor beyond preserving existing labels/reachable reasons while resizing.
- Story 1.19 live-region/reduced-motion matrix expansion beyond preserving Story 1.16's Stop/Cancel semantics and existing forced-colors/reduced-motion cues.
- Story 1.20 English/French localization infrastructure beyond preserving room for text expansion and avoiding truncation of critical state/action words.
- Story 1.21 off-surface redaction affordances.
- Upgrading Fluent UI, Fluxor, FrontComposer, .NET, Playwright, xUnit, bUnit, or adding inline package versions.

### Project Structure Notes

- New responsive/touch contracts: `src/Hexalith.ChatBot.UI/Design/`.
- CSS/token work: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Existing app viewport and CSS load: `src/Hexalith.ChatBot.UI/Components/App.razor`.
- Layout shell: `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor`.
- Governed shell/primitives: `src/Hexalith.ChatBot.UI/Components/Governed/`.
- Current page fixture: `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`.
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
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.17: Responsive and touch foundation]
- [Source: _bmad-output/planning-artifacts/epics.md#UX Design Requirements]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Visual posture]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Responsive & Platform]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Cognitive-load guardrails]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Localization]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings]
- [Source: _bmad-output/implementation-artifacts/1-16-interaction-guardrails-and-streaming-stop-cancel-behavior.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.UI/Components/App.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]
- [Source: src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs]
- [Source: src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- [Source: Directory.Packages.props]
- [Source: https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html]
- [Source: https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/meta/name/viewport]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red phase: `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` failed at compile because the new responsive/touch contract types did not exist yet.
- VSTest runner limitation: after implementation, `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` built successfully but aborted in this sandbox with `System.Net.Sockets.SocketException (13): Permission denied`; switched to the compiled xUnit v3 binaries required by this story.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -noColor` passed: 32 total, 0 failed, 0 skipped.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor` passed: 9 total, 0 failed, 0 skipped. Chrome was present, so Playwright browser tests ran; static fallback remains preserved for restricted environments.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor` passed: 33 total, 0 failed, 0 skipped.
- `git diff --check` passed with no whitespace errors.

### Completion Notes List

- Added UI-owned responsive contracts for phone/tablet/desktop tiers, surface capability metadata, phone-limited fallback metadata, touch target rules, and dense-row label retention.
- Extended `chatbot.tokens.css` with responsive width/touch variables, touch target classes, labelled-row/dense-row hooks, wrapping behavior, phone/tablet/desktop media rules, and preserved forced-colors styling without adding a new palette or design system.
- Added touch metadata to governed action and streaming stop primitives and updated `GovernedOperations.razor` as the first fixture consumer while preserving `SubmitGovernedNoteAction` and `ChatBotSurfaceOrigin.Ui` behavior.
- Added focused UI contract tests and E2E fixture coverage for ordered tiers, no CLI/MCP breakpoints, fallback completeness, touch target thresholds/restrictions, dense row retention, package pins, viewport meta preservation, no horizontal overflow, visible safe metadata, reachable disabled reasons, and touch target dimensions.

### File List

- `_bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Design/ChatBotDenseRowCollapseContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotDenseRowField.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotDenseRowFieldRetention.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotResponsiveActionKind.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotResponsiveSurfaceCapability.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotResponsiveSurfaceCapabilityContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotSmallScreenFallbackContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotTouchTarget.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotTouchTargetClass.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotViewportTier.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs`

### Senior Developer Review (AI)

- Reviewer: Codex on 2026-05-31.
- Outcome: approved after automatic fixes; no critical issues remain.
- Finding fixed (Medium): `.chatbot-shell-main` used `overflow-x: clip`, which could hide clipped responsive content while the E2E overflow assertion still passed at the document level. Removed clipping and asserted overflow on `documentElement`, `body`, `.chatbot-shell-main`, and the governed operations responsive fixture.
- Finding fixed (Medium): `ChatBotSmallScreenFallbackContract.IsComplete` threw when a public contract instance carried a null `SafeActions` list. It now treats null safe-action metadata as incomplete, with contract coverage.
- Finding fixed (Low): Story File List omitted the modified `_bmad-output/implementation-artifacts/tests/test-summary.md`; the File List now reflects the actual related documentation change.
- Validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- Validation: `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -noColor` passed 32/32.
- Validation: `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor` passed 9/9.
- Validation: `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor` passed 33/33.
- Validation: `git diff --check` passed.

### Change Log

- 2026-05-31: Implemented responsive/touch foundation contracts, CSS hooks, governed operations fixture consumption, and focused contract/E2E coverage; marked story ready for review.
- 2026-05-31: Senior review removed responsive overflow clipping, hardened fallback metadata completeness, updated File List documentation, and marked story done.
