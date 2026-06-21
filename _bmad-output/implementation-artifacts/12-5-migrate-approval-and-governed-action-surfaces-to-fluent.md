---
baseline_commit: c3232b5
---

# Story 12.5: Migrate approval and governed-action surfaces to Fluent v5

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-21. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As an authorized reviewer of approval and governed-action surfaces,
I want approval decisions, task-intent transitions, why-this-project controls, and approval queue actions rendered with Fluent v5 primitives,
so that governed action semantics, disabled reasons, redaction boundaries, and FrontComposer visual inheritance remain consistent across S3 and related review flows.

## Acceptance Criteria

1. **S3 approval decision actions use Fluent buttons without changing approval semantics.** Given `ChatBotApprovalConversationItem`, when migrated, then the pending approval actions for approve, reject, request revision, and cancel render as `FluentButton` or the existing `ChatBotGovernedAction`/`FluentButton` primitive; `SubmitApprovalDecisionAsync`, `ApprovalDecisionKind.Approve`, `ApprovalDecisionKind.Reject`, `ApprovalDecisionKind.RequestRevision`, `ApprovalDecisionKind.Cancel`, `CanApprove`, `BlockApproveAsync`, `DecisionLiveRegion`, `DecisionStatus`, `aria-describedby`, and the reachable approve disabled reason remain behaviorally identical. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.5`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR41-FR45`]

2. **Why-this-project panel controls use Fluent buttons while preserving metadata-only evidence.** Given `ChatBotWhyProjectPanel`, when migrated, then the close control and superseding-correction control render as `FluentButton`s, `OnClose`, `OnOpenAssociation`, `data-chatbot-why-project-panel="metadata-only"`, `data-chatbot-correction-link`, association ids, evidence visibility states, redaction/unavailable explanations, `ChatBotEvidenceChip`, and the `role="complementary"` accessible panel contract are preserved. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.5`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`; `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs#AssertWhyProjectPanelCoverageWithoutBrowser`]

3. **Task-intent review actions and predecessor input use Fluent form primitives.** Given `ChatBotTaskIntentReviewPanel`, when migrated, then transition actions render as `FluentButton`s or `ChatBotGovernedAction` over `FluentButton`; the predecessor label/input pair renders as `FluentLabel` plus `FluentTextInput` or another locally verified Fluent v5 text-field primitive; `role="toolbar"`, action labels, `aria-disabled`, `aria-describedby`, disabled reason spans, `TaskIntentTransitionSelectionModel`, duplicate validation (`predecessor_task_intent_required`), `role="status"`, and `aria-live="polite"` are preserved. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.5`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor`; `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs#AssertTaskIntentReviewPanelCoverageWithoutBrowser`]

4. **Shared governed-action primitive remains the single disabled-reason action wrapper.** Given `ChatBotGovernedAction`, when this story is complete, then it still wraps `FluentButton`, still exposes `data-chatbot-critical-action`, `data-chatbot-action-state`, `data-chatbot-touch-target="primary"`, `data-chatbot-stable-id`, `aria-disabled`, `aria-describedby`, focusable reason text, `ChatBotUiTextKey.WhyUnavailable`, `DisabledReason`, and `NotApplicableHidden`; confirm, defer, correct, retry, quarantine, escalate, approve, request-revision, and cancel affordances that already flow through this primitive inherit Fluent rendering and reachable disabled reasons; no raw native button, tooltip-only reason, hover-only activation, or native `Disabled` short-circuit is introduced. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs#GovernedActionPrimitiveShouldExposeReachableStateAndReasonSemantics`]

5. **Approval queue priority view remains governed and accessible while using Fluent action primitives.** Given `ChatBotApprovalQueuePriorityView`, when verified or narrowly adjusted, then grouped approval rows, priority explanation, requester/command/project metadata, per-group item count, disabled batch approve action, partial-authority reason, validation summary, small-screen fallback, and metadata-only restrictions remain intact; batch approve continues through `ChatBotGovernedActionState.DisabledWithReason` until real batch fan-out is explicitly implemented elsewhere. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR46`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotApprovalQueuePriorityContractTests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/ApprovalQueuePriorityE2ETests.cs`]

6. **The Fluent conformance guard shrinks only for this story's fixed files.** Given `ChatBotFluentConformanceTests`, when this story is complete, then `Components/Governed/ChatBotApprovalConversationItem.razor`, `Components/Governed/ChatBotTaskIntentReviewPanel.razor`, and `Components/Governed/ChatBotWhyProjectPanel.razor` are removed from `RawControlMigrationBacklog`; no stale backlog entries remain; no new raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` tags are introduced; later-story backlog entries for policy editors and compliance audit remain untouched. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`; `_bmad-output/implementation-artifacts/12-4-migrate-association-review-surface-to-fluent.md`]

7. **Focused tests and fixtures prove Fluent migration plus governed behavior.** Given source-contract, governance, approval queue, project conversation, accessibility, and E2E/source-fixture lanes, when updated, then tests assert required Fluent tags, absence of raw native controls in Story 12.5 target files, preservation of approval decision and task-intent transition markers, why-project metadata-only behavior, approval queue partial-authority behavior, and browser/fallback safety. Exact validation commands and results are recorded in `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md`. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/ApprovalQueuePriorityE2ETests.cs`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.4.md`]

8. **Scope remains a rendering-layer correction only.** Given Epic 12 constraints, when this story is complete, then there are no package upgrades, no Fluent version churn, no backend, CommandGateway, CLI, MCP, SignalR, approval policy, task-intent domain, or approval queue fan-out behavior changes; no sibling submodule edits; no generated `obj/**/generated/HexalithFrontComposer/**` edits; no wholesale `chatbot.tokens.css` retirement; and no migration of policy editors, operational dashboards, compliance audit, or final cross-surface verification owned by Stories 12.6-12.9. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `Hexalith.AI.Tools/hexalith-llm-instructions.md`]

## Tasks / Subtasks

- [x] Migrate `ChatBotApprovalConversationItem` pending decision actions (AC: 1, 6, 8)
  - [x] Replace the four raw pending decision `<button>` elements with `FluentButton Type="ButtonType.Button"` or a narrowly adapted `ChatBotGovernedAction` usage that preserves the exact callbacks.
  - [x] Preserve approve blocking: disabled state remains advisory through `aria-disabled`, blocked approval calls `BlockApproveAsync`, sets `DecisionLiveRegion` to assertive, and announces `ResolvedApproveDisabledReason`.
  - [x] Keep reject, request revision, and cancel available for pending approvals exactly as today; do not add backend command paths or batch behavior.
  - [x] Preserve metadata rows, `ChatBotAiActionPreviewSections`, evidence freshness chips, redaction-safe policy/audit explanation paragraphs, review history, and item accessibility labels.

- [x] Migrate `ChatBotWhyProjectPanel` controls (AC: 2, 6, 8)
  - [x] Replace the close raw button with `FluentButton Type="ButtonType.Button"` and preserve `aria-label`, `CloseAsync`, and panel accessible naming.
  - [x] Replace the superseding-correction raw button with `FluentButton Type="ButtonType.Button"` and preserve `data-chatbot-correction-link`, visible correction text, and `OpenSupersedingCorrectionAsync`.
  - [x] Keep `aside role="complementary"`, `data-chatbot-why-project-panel="metadata-only"`, `dl/dt/dd`, `code`, `time`, `ol/li`, evidence visibility markers, and redaction/unavailable explanations intact.

- [x] Migrate `ChatBotTaskIntentReviewPanel` actions and duplicate input (AC: 3, 6, 8)
  - [x] Replace transition raw buttons with Fluent action primitives while preserving `role="toolbar"`, `aria-label="Task intent actions"`, per-transition disabled reasons, and `SelectTransitionAsync`.
  - [x] Replace the predecessor raw `<label>` and `<input>` with `FluentLabel` and `FluentTextInput` or a locally verified equivalent from the pinned Fluent package.
  - [x] Preserve `PredecessorInputId`, `PredecessorTaskIntentId`, duplicate validation, `aria-invalid`, `predecessor_task_intent_required`, live-region text, and `TaskIntentTransitionSelectionModel`.
  - [x] Do not reveal source message content that is currently suppressed by tests; keep task intent review metadata-only where required.

- [x] Re-verify `ChatBotGovernedAction` and approval queue usage (AC: 4, 5, 8)
  - [x] Confirm `ChatBotGovernedAction` remains the shared action wrapper over `FluentButton`; do not replace it with repeated one-off markup.
  - [x] Verify action labels/usages for confirm, defer, correct, retry, quarantine, escalate, approve, request-revision, and cancel still inherit the same Fluent rendering and disabled-reason path where they use `ChatBotGovernedAction`.
  - [x] Confirm `ChatBotApprovalQueuePriorityView` keeps `ChatBotGovernedActionState.DisabledWithReason`, partial-authority reason text, grouped rows, small-screen fallback, and safe metadata.
  - [x] Update source-contract tests only if markup changes require new non-vacuous Fluent markers; do not weaken existing disabled-reason or no-tooltip assertions.

- [x] Update conformance and focused tests (AC: 6, 7)
  - [x] Remove only the Story 12.5 target files from `RawControlMigrationBacklog` after raw controls are gone.
  - [x] Update `ChatBotAccessibilityFocusContractTests` so `ChatBotApprovalConversationItem` no longer expects `<button` and does expect `FluentButton` action markers.
  - [x] Update or add focused tests for `ChatBotTaskIntentReviewPanel` to require `FluentButton`, `FluentLabel`, `FluentTextInput`, and preserved transition/duplicate markers.
  - [x] Update or add focused tests for `ChatBotWhyProjectPanel` to require `FluentButton` and preserved metadata-only/redaction markers.
  - [x] Update `ProjectConversationE2ETests` fallback checks if they hard-code native control source expectations; keep browser assertions and redaction checks intact.
  - [x] Keep `ApprovalQueuePriorityContractTests` and `ApprovalQueuePriorityE2ETests` green; update only if this story's markup changes make existing assertions stale.

- [x] Verify and record results (AC: all)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`, restoring first only if needed.
  - [x] Run `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false`; if VSTest sockets are denied, run the compiled xUnit v3 executable fallback and record it.
  - [x] Run focused UI tests covering `ChatBotFluentConformanceTests`, `ChatBotAccessibilityFocusContractTests`, `ChatBotInteractionGuardrailContractTests`, `ChatBotLocalizationContractTests`, and `ChatBotApprovalQueuePriorityContractTests`.
  - [x] Run affected E2E/source-fixture tests in `ProjectConversationE2ETests` and `ApprovalQueuePriorityE2ETests`; use the browser path when available and explicitly record fallback-only coverage if no browser executes.
  - [x] Run `git diff --check`.
  - [x] Add `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md` with exact commands, pass/fail status, and environmental limitations.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: user `Jerome`, project `chatbot`, planning artifacts under `_bmad-output/planning-artifacts`, implementation artifacts under `_bmad-output/implementation-artifacts`, document language English.
- Loaded sprint status from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key `12-5-migrate-approval-and-governed-action-surfaces-to-fluent` was `backlog`; `epic-12` was already `in-progress`; Stories 12.1-12.4 were `done`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`, especially Epic 12 and Story 12.5.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially Frontend Architecture and the ChatBot UI Fluent-only conformance rule.
- Loaded source hints from `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`, which introduced Epic 12 and named the Story 12.5 target files.
- Loaded PRD context from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR41-FR45, FR49-FR50, FR62, NFR46, NFR48, NFR60-NFR63, and NFR65.
- Loaded previous story intelligence from `_bmad-output/implementation-artifacts/12-4-migrate-association-review-surface-to-fluent.md` and `_bmad-output/implementation-artifacts/tests/test-summary-story-12.4.md`.
- Loaded persistent project-context facts from eight sibling `**/project-context.md` files. Relevant facts: .NET 10, `.slnx`, central package management, warnings-as-errors, xUnit v3 and Shouldly, `DiffEngine_Disabled=true` for Verify, root-level submodule-only policy, no generated-output edits, no casual package upgrades, and FrontComposer/Fluent-only UI rules.
- Inspected current target sources and focused tests under `src/Hexalith.ChatBot.UI/Components/Governed`, `src/Hexalith.ChatBot.UI/Components/Pages`, `tests/Hexalith.ChatBot.UI.Tests`, and `tests/Hexalith.ChatBot.UI.E2E.Tests`.

### Epic 12 Context

Epic 12 closes the component-level Fluent v5 gap left after Epic 10 adopted the FrontComposer shell while interior ChatBot surfaces still used raw HTML over `chatbot.tokens.css`. The binding rule is that every `Hexalith.ChatBot.UI` `.razor` page/component uses FrontComposer or Fluent UI v5 components, with no raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` outside the temporary migration backlog. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

Story 12.5 owns approval and governed-action surfaces: `ChatBotApprovalConversationItem`, `ChatBotWhyProjectPanel`, `ChatBotTaskIntentReviewPanel`, `ChatBotGovernedAction`, and `ChatBotApprovalQueuePriorityView`. Story 12.6 owns policy/config editors; Story 12.7 owns operational/audit pages; Story 12.8 retires `chatbot.tokens.css`; Story 12.9 performs final cross-surface a11y/visual verification. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.5`; `_bmad-output/planning-artifacts/epics.md#Stories 12.6-12.9`]

### Current Implementation State

`ChatBotApprovalConversationItem` is partially Fluent already: the outer item uses `FluentCard`, `FluentStack`, `FluentText`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, and Fluent evidence freshness chip buttons. The remaining raw controls are the pending decision buttons for approve, reject, request revision, and cancel. Preserve `ApproveAsync`, `BlockApproveAsync`, `SubmitDecisionAsync`, `CanApprove`, `ResolvedApproveDisabledReason`, `ApproveReasonId`, `DecisionStatus`, and `DecisionLiveRegion`. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`]

`ChatBotWhyProjectPanel` currently renders two raw controls: a close button and a superseding-correction button. The panel is metadata-only and carries association ids, source/correlation/schema metadata, evidence visibility markers, redaction/unavailable explanations, correction propagation text, and evidence chips. Preserve semantic `aside`, `header`, `section`, `dl/dt/dd`, `ol/li`, `code`, and `time` markup while migrating controls. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`]

`ChatBotTaskIntentReviewPanel` currently renders one raw button per available transition, one raw predecessor `<label>`, and one raw predecessor `<input>`. It owns transition selection, duplicate-predecessor validation, disabled reasons, and the polite live region. The source message body is conditionally displayed only when already present in the authorized review model; do not broaden what this surface reveals. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor`]

`ChatBotGovernedAction` already wraps `FluentButton` and exposes disabled-with-reason semantics. Treat it as the shared primitive to preserve, not a target for rewrites unless focused tests prove a small fix is needed. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs`]

`ChatBotApprovalQueuePriorityView` currently uses `ChatBotGovernedAction` for disabled batch approval and preserves grouped priority rows, validation summary, safe metadata, partial-authority reason, and phone fallback. Do not implement real batch approval in this rendering story. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotApprovalQueuePriorityContractTests.cs`]

### Fluent v5 Component Notes

The local package pin is binding: `Directory.Packages.props` pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, and `_Imports.razor` already imports `Microsoft.FluentUI.AspNetCore.Components`. Do not add package references or change versions. [Source: `Directory.Packages.props`; `src/Hexalith.ChatBot.UI/Components/_Imports.razor`; `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj`]

Local patterns already available:

- `ChatBotGovernedComposer` shows the accepted `FluentLabel` plus `FluentTextArea` pattern with `Id`, `Value`, `ValueChanged`, `Immediate="true"`, and explicit `aria-*` attributes. Use the same binding style when a Fluent text field needs two-way state. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`]
- Story 12.4 established the conservative migration pattern: keep semantic HTML where it carries contract meaning and wrap or replace only the presentation/control primitive. [Source: `_bmad-output/implementation-artifacts/12-4-migrate-association-review-surface-to-fluent.md`]
- `ChatBotGovernedAction` is the canonical reusable action wrapper for critical governed actions with disabled reasons. Prefer reusing it where the target action semantics match; otherwise use `FluentButton Type="ButtonType.Button"` directly with explicit `aria-*` preservation. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`]

Latest official context checked during story creation:

- The Microsoft Fluent UI Blazor README identifies `Microsoft.FluentUI.AspNetCore.Components` as the Razor component package for Blazor apps using Fluent Design, points to `www.fluentui-blazor.net` for component docs/demo, and shows `_Imports.razor` usage with `@using Microsoft.FluentUI.AspNetCore.Components`. This repo already has that setup, so this story should use the locally pinned API surface and not introduce setup churn. [Source: `https://github.com/microsoft/fluentui-blazor`; `https://www.fluentui-blazor.net/`]
- The local NuGet XML for `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` confirms `FluentButton`, `FluentLabel`, `FluentTextInput`, `FluentTextArea`, `FluentCard`, `FluentStack`, `FluentText`, and `FluentDataGrid` exist in the installed `net10.0` package. [Source: local NuGet package docs under `~/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/`]

### Architecture and UX Guardrails

- UI may depend on ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts. It must not reference Server, Gateway internals, Dapr, EventStore internals, audit, idempotency, projection internals, CLI, or MCP. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- UX-DR1 requires component-level Fluent inheritance and build-enforced conformance; raw `<a>` nav links are allowed, but raw lowercase interactive controls are not. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`]
- UX-DR2 bans recreating Fluent-provided primitives in hand CSS. Custom CSS should be layout-only unless Story 12.8 is explicitly retiring token debt. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`]
- Approval surfaces must preserve proposed command, input/evidence, recipients/sender authority where present, risk classification, policy snapshot, expected post-state, approval decisions, authority-based disabled approval, evidence freshness, and metadata-only safe display. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR41-FR45`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR48`]
- Approval queue grouping and prioritization are safety requirements, not visual decoration: order by risk, affected-party authority, and age; group by requester, command, and project; partial authority must remain visible and safe. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR46`]
- Accessibility validation must include keyboard-only review flows, screen-reader labels, focus order, non-color status indicators, error recovery, and next-action clarity for approval workflows. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60-NFR63`]

### File Structure Requirements

Primary implementation locations:

- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotApprovalQueuePriorityContractTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ApprovalQueuePriorityE2ETests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md`

Secondary files only if focused tests prove they need source/fixture expectation updates:

- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`

Avoid these locations:

- Do not edit `Hexalith.FrontComposer`, `Hexalith.EventStore`, `Hexalith.Tenants`, `Hexalith.Parties`, `Hexalith.Memories`, or other sibling submodules.
- Do not edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Do not modify package pins in `Directory.Packages.props` or add inline package versions.
- Do not move approval, task-intent, or queue behavior into backend, CLI, MCP, SignalR, service, effect, or reducer changes unless an existing compile break proves a narrow UI-facing contract mismatch.

### Previous Story Intelligence

Story 12.4 established the migration and validation pattern for this story:

- Use the local Fluent package pin; do not upgrade packages.
- Use PascalCase Fluent component tags so the case-sensitive governance regex does not false-match raw controls.
- Remove stale raw-control backlog entries only after the source no longer contains raw controls.
- Keep source-contract tests raw-tag-aware rather than case-insensitive substring checks that reject names such as `FluentTextArea`.
- Preserve semantic HTML (`article`, `section`, `aside`, `ol/li`, `dl/dt/dd`, `code`, `time`) when semantics are part of governed contracts; use Fluent components for controls and layout/text primitives around them.
- VSTest socket creation may fail in this sandbox; prior stories used the compiled xUnit v3 executable fallback successfully.
- Browser E2E fallback can silently skip real Playwright assertions when no browser resolves; record whether real browser path or fallback-only path ran.
- Real browser testing in Story 12.4 found Fluent custom-element issues that string fixtures missed; when controls become Fluent web components, prefer a browser path for at least affected approval/task-intent flows.
- Record exact commands and results in the per-story test summary.
- Do not claim this UI story caused or fixed unrelated backend or submodule drift. [Source: `_bmad-output/implementation-artifacts/12-4-migrate-association-review-surface-to-fluent.md`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.4.md`]

### Git Intelligence

Recent relevant commits:

- `c3232b5 feat(story-12.4): Migrate association review surface to Fluent`
- `6336421 feat(story-12.3): Migrate conversation stream and items to Fluent`
- `6266d0c chore: Update subproject commits for FrontComposer and Memories`
- `09aa92b fix(tests): Enhance cross-tenant leakage scans in isolation tests`
- `6fb7edc feat(story-12.2): Migrate governed chat composer to Fluent`

Story 12.4 modified the association review target files, `chatbot.tokens.css`, `GovernedOperationsVisualFoundationE2ETests`, `AssociationReviewComponentContractTests`, `ChatBotAccessibilityFocusContractTests`, and `ChatBotFluentConformanceTests`. The key pattern is to make real source changes, update focused contract tests, and then shrink the conformance backlog rather than weakening the guard. [Source: `git show --stat --name-only c3232b5`]

Current working tree note at story creation: there are pre-existing modified submodule pointers (`Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Parties`) and unrelated BMAD/test-summary artifacts. Do not revert them, and do not include submodule pointer changes in Story 12.5 unless the user explicitly asks. [Source: `git status --short` on 2026-06-21]

### Testing Standards

- Use xUnit v3 and Shouldly; avoid raw `Assert.*`.
- Keep `DiffEngine_Disabled=true` when running Verify-backed tests.
- Build with `.slnx`; do not create or use `.sln`.
- Prefer focused UI/governance/E2E project commands over broad solution-level test runs.
- Keep tests non-vacuous: assert target files exist, Fluent tags are present, raw controls are gone, and critical approval/task-intent/why-project markers remain.
- If browser prerequisites are unavailable, use existing fixture fallback and record the limitation honestly.

### Regression Traps to Avoid

- Do not turn approval or task-intent review into a generic ungoverned form.
- Do not call approved AI action execution directly from the approval button; approval records the decision and downstream command execution remains governed.
- Do not make `approve` natively disabled in a way that prevents the current `BlockApproveAsync` reason path and assertive live-region message.
- Do not hide disabled reasons behind tooltip-only behavior or color-only styling.
- Do not remove `aria-describedby`, `aria-disabled`, visible reason text, or focusable explanation paths.
- Do not leak raw provider payloads, raw prompts, tenant-beta identifiers, restricted project names, full outbound email bodies, or unauthorized evidence while updating fixtures.
- Do not remove `ChatBotAiActionPreviewSections`, `ChatBotConversationItemReviewHistory`, evidence freshness chips, status summary, or metadata rows from approval items.
- Do not remove why-project evidence visibility/redaction markers or correction propagation metadata.
- Do not change task-intent transition names, duplicate predecessor validation, or source message visibility behavior.
- Do not weaken `ChatBotGovernedAction`; it should remain reusable for governed controls on later surfaces.
- Do not migrate policy editors, compliance audit, operational dashboards, or final cross-surface verification in this story.
- Do not retire `chatbot.tokens.css` wholesale; Story 12.8 owns final retirement.
- Do not run recursive submodule initialization.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`]
- [Source: `.agents/skills/bmad-create-story/discover-inputs.md`]
- [Source: `.agents/skills/bmad-create-story/template.md`]
- [Source: `.agents/skills/bmad-create-story/checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: ChatBot UI Fluent v5 Component Conformance Remediation`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.5: Migrate approval & governed-action surfaces -> Fluent v5`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`]
- [Source: `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`]
- [Source: `_bmad-output/implementation-artifacts/12-2-migrate-governed-chat-composer-to-fluent.md`]
- [Source: `_bmad-output/implementation-artifacts/12-3-migrate-conversation-stream-and-items-to-fluent.md`]
- [Source: `_bmad-output/implementation-artifacts/12-4-migrate-association-review-surface-to-fluent.md`]
- [Source: `_bmad-output/implementation-artifacts/tests/test-summary-story-12.4.md`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotApprovalQueuePriorityContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/ApprovalQueuePriorityE2ETests.cs`]
- [Source: `Directory.Packages.props`]
- [Source: `src/Hexalith.ChatBot.UI/Components/_Imports.razor`]
- [Source: local NuGet package docs for `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`]
- [Source: `https://github.com/microsoft/fluentui-blazor`]
- [Source: `https://www.fluentui-blazor.net/`]
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

- 2026-06-21: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` passed with 0 warnings and 0 errors.
- 2026-06-21: `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false` was blocked by VSTest socket permission denial before execution.
- 2026-06-21: `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -trait "Category=Governance"` passed: 6 total, 0 failed.
- 2026-06-21: Focused UI test classes passed: 51 total, 0 failed.
- 2026-06-21: Affected `ProjectConversationE2ETests` and `ApprovalQueuePriorityE2ETests` classes passed: 35 total, 0 failed.
- 2026-06-21: Full `Hexalith.ChatBot.UI.Tests` xUnit executable passed: 170 total, 0 failed.
- 2026-06-21: Full `Hexalith.ChatBot.UI.E2E.Tests` xUnit executable passed: 124 total, 0 failed.
- 2026-06-21: `git diff --check` passed.

### Completion Notes List

- Replaced approval pending decision raw buttons with `FluentButton` while preserving approve advisory disabled semantics, `BlockApproveAsync`, decision callbacks, live region behavior, and metadata-only approval content.
- Replaced why-project close and superseding-correction raw buttons with `FluentButton` while preserving complementary panel metadata, correction link markers, evidence visibility, and redaction explanations.
- Replaced task-intent transition raw buttons and predecessor raw label/input with `FluentButton`, `FluentLabel`, and `FluentTextInput` while preserving transition selection, duplicate predecessor validation, disabled reason spans, and metadata-only source visibility.
- Re-verified `ChatBotGovernedAction` and approval queue behavior through existing focused contract and E2E/source-fixture lanes; no queue behavior or batch fan-out implementation was changed.
- Shrank the Fluent conformance raw-control backlog only for the three Story 12.5 target files and added non-vacuous source-contract assertions for the migrated Fluent markers.
- 2026-06-21 (review fix): The new approval-queue and task-intent E2E tests passed only on the no-browser fallback path; on the real browser path they failed (Playwright actionability). Fixed: force-click the advisory-disabled governed batch action (`ClickAsync(Force=true)`) so the "no fan-out command" assertion is genuinely exercised, and gave the simulated `<fluent-text-field>` predecessor input a visible bounding box so `FillAsync` succeeds. Re-verified the full E2E suite on the real browser path (124 total, 0 failed) and full UI suite (170 total, 0 failed).

### File List

- `_bmad-output/implementation-artifacts/12-5-migrate-approval-and-governed-action-surfaces-to-fluent.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ApprovalQueuePriorityE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`

### Change Log

- 2026-06-21: Migrated approval, why-project, and task-intent governed-action surfaces from raw controls to Fluent v5 primitives; updated conformance/focused tests and recorded validation evidence.
- 2026-06-21: Adversarial code review (story-automator). Fixed three browser-path E2E failures masked by the no-browser fallback, completed the File List, and corrected the test summaries to reflect real browser-path results. No CRITICAL issues remain; status set to done.

## Senior Developer Review (AI)

**Reviewer:** Jerome — 2026-06-21
**Outcome:** Approved after auto-fixes (0 CRITICAL remaining).

### Scope verified

- Independently rebuilt `Hexalith.ChatBot.slnx` (0 warnings, 0 errors).
- Re-ran the focused governance/contract lanes (6 Governance + 51 focused) and the full suites: `Hexalith.ChatBot.UI.Tests` 170/0 and `Hexalith.ChatBot.UI.E2E.Tests` 124/0 — the E2E run executed the **real browser path** (Chromium present; ~22s with live Playwright interaction, not the string fallback).
- Confirmed AC1–AC8: the four approval decision buttons, why-project close/correction controls, and task-intent transitions/predecessor input render as Fluent v5 primitives with `aria-disabled`/`aria-describedby`/reachable disabled reasons preserved; `ApproveAsync`→`BlockApproveAsync` advisory-disabled path and assertive live region intact; `ChatBotGovernedAction` and `ChatBotApprovalQueuePriorityView` (genuinely `DisabledWithReason`, no fan-out) unchanged; conformance backlog shrank only for the three target files and the four remaining entries still contain raw controls (not stale).

### Findings and resolutions

1. **[CRITICAL — FIXED] Recorded E2E results were false on the browser path.** Three E2E tests passed only via the no-browser fallback and **failed on the real browser path** (the documented fallback-masking trap): two in `ApprovalQueuePriorityE2ETests` called `ClickAsync()` on an `aria-disabled="true"` `<fluent-button>` (Playwright actionability → 30s timeout), and `ProjectConversationE2ETests.TaskIntentReviewPanelShouldExposeReviewConversionAndDispositionWorkflow` called `FillAsync` on a zero-box `<fluent-text-field>` ("element is not visible"). Fixed by force-clicking the advisory-disabled governed action (which correctly models a reachable activation that records no batch command) and by giving the simulated predecessor text field a visible bounding box. Re-verified green on the browser path.
2. **[MEDIUM — FIXED] Incomplete File List.** `tests/Hexalith.ChatBot.UI.E2E.Tests/ApprovalQueuePriorityE2ETests.cs` and `_bmad-output/implementation-artifacts/tests/test-summary.md` were modified by this story but absent from the File List; both added.
3. **[MEDIUM — FIXED] Inaccurate test evidence.** `test-summary-story-12.5.md` recorded "0 failed" for the browser-path E2E lanes that actually failed; corrected to document the fallback masking, the failures found, the fixes, and the re-verified browser-path results.
4. **[LOW — noted, not changed] Fixture/render fidelity.** The task-intent E2E fixture simulates the input as `<fluent-text-field>` while the migrated component renders `FluentTextInput` (Fluent v5 web element). This is inherent to the no-bUnit static-fixture strategy (the fixture is hand-authored DOM, never the real render) and is immaterial to the role/label-driven assertions; left as-is to avoid introducing an unverified element name.
