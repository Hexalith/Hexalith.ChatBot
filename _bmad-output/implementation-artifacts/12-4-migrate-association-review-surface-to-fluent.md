---
baseline_commit: 6336421
---

# Story 12.4: Migrate association review surface to Fluent v5

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-21. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As an authorized association reviewer,
I want the S2 association review surface rendered with Fluent v5 components,
so that candidate selection, evidence comparison, notes, correction rationale, and safe actions inherit the FrontComposer visual system without losing governed decision semantics.

## Acceptance Criteria

1. **Association review actions use Fluent form primitives without changing command semantics.** Given `ChatBotAssociationReviewActions`, when migrated, then both raw lowercase `<textarea>` fields are replaced with `FluentTextArea`, their visible labels use `FluentLabel`, decision-note and correction-rationale updates still dispatch through `OnDecisionNoteChanged` and `OnCorrectionRationaleChanged`, validation remains visible through `ChatBotStatusBanner`, and all `ChatBotGovernedAction` preview/correction callbacks, disabled-with-reason states, consequence text, correction status, propagation progress, safe-next-action, owner-role, and policy/audit/projection blocked reasons are preserved. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.4`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`]

2. **Candidate row selection moves from raw button to Fluent while preserving radio behavior.** Given `ChatBotAssociationCandidateRow`, when migrated, then the raw lowercase `<button>` is replaced by `FluentButton` (or an explicitly proven Fluent v5 selection primitive), while preserving `type`/button behavior, `role="radio"`, `aria-checked`, accessible candidate label, `data-chatbot-association-candidate`, `data-chatbot-selected`, rank, display label, confidence formatting, reason codes, `ChatBotEvidenceChip` rendering, restricted-evidence text, and `OnSelected` dispatch of the selected project id. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.4`; `_bmad-output/planning-artifacts/epics.md#UX-DR25`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor`]

3. **Evidence comparison and page surfaces use Fluent surface/layout/text primitives without hiding metadata.** Given `ChatBotAssociationEvidenceComparison` and `Pages/AssociationReview`, when migrated, then surface and layout-only wrappers use `FluentCard`, `FluentStack`, and `FluentText` where appropriate; page headings, candidate section, empty/blocked state, selected-candidate comparison, and source-metadata panel remain reachable; `data-chatbot-responsive-fixture="association-review"`, `data-chatbot-association-comparison="true"`, `ChatBotConversationShell`, `ChatBotProjectContextHeader`, `ChatBotStatusBanner`, `ChatBotBlockedState`, `ChatBotAssociationCandidateRow`, `ChatBotAssociationReviewActions`, and `ChatBotAssociationEvidenceComparison` stay wired exactly as the S2 surface expects. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.4`; `_bmad-output/planning-artifacts/epics.md#Story 2.5`; `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor`]

4. **S2 governed behavior, fail-closed redaction, and accessibility are preserved.** Given ambiguous, fail-closed, no-authorized-candidate, selected-candidate, validation-error, terminal, correction-pending, correction-blocked, correction-delayed, and projection-pending states, when the migrated surface renders, then no ambiguous item auto-attaches, unauthorized candidate projects and raw evidence remain suppressed, failure text remains metadata-only, disabled association/correction controls expose reachable reasons, validation errors use the existing summary/banner path, focus order follows candidate selection -> evidence comparison -> action form, status meaning is not color-only, and machine codes/ids remain visible as code where they are currently visible. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.5`; `_bmad-output/planning-artifacts/epics.md#UX-DR7`; `_bmad-output/planning-artifacts/epics.md#UX-DR24-UX-DR25`; `_bmad-output/planning-artifacts/epics.md#UX-DR35-UX-DR45`; `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`]

5. **The Fluent conformance guard shrinks only for this story's fixed files.** Given `ChatBotFluentConformanceTests`, when this story is complete, then `Components/Governed/ChatBotAssociationCandidateRow.razor` and `Components/Governed/ChatBotAssociationReviewActions.razor` are removed from `RawControlMigrationBacklog`, no stale backlog entries remain, no new raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` tags are introduced, and remaining backlog entries for Story 12.5-12.7 files stay untouched. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`; `_bmad-output/implementation-artifacts/12-3-migrate-conversation-stream-and-items-to-fluent.md`]

6. **Tests and fixtures are updated intentionally.** Given focused UI, governance, source-contract, and affected E2E lanes, when updated, then tests assert the required Fluent tags in Story 12.4 files, preserve the existing S2 semantics and redaction assertions, update browser/fixture markup that still hard-codes raw candidate buttons or note textareas, keep raw-tag checks case-sensitive so `FluentTextArea` does not false-fail, and record exact validation commands/results in `_bmad-output/implementation-artifacts/tests/test-summary-story-12.4.md`. [Source: `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`]

7. **Scope remains a rendering-layer correction only.** Given Epic 12 constraints, when this story is complete, then there are no package upgrades, no Fluent version churn, no backend / CommandGateway / CLI / MCP / SignalR behavior changes, no sibling submodule edits, no generated `obj/**/generated/HexalithFrontComposer/**` edits, no wholesale `chatbot.tokens.css` retirement beyond narrow obsolete S2 primitive usage if actually removed, and no migration of approval buttons, why-this-project controls, task-intent review, policy editors, operational/audit pages, or final cross-surface verification owned by Stories 12.5-12.9. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `Hexalith.AI.Tools/hexalith-llm-instructions.md`]

## Tasks / Subtasks

- [x] Migrate `ChatBotAssociationReviewActions` form fields and action layout (AC: 1, 4, 7)
  - [x] Replace the decision-note raw `<label>`/`<textarea>` with `FluentLabel` plus `FluentTextArea` using the existing composer pattern: `Id`, `Value`, `ValueChanged` or equivalent event bridge, `Immediate="true"` when needed, and explicit `aria-*` attributes.
  - [x] Replace the correction-rationale raw `<label>`/`<textarea>` with `FluentLabel` plus `FluentTextArea`, preserving correction lifecycle validation, safe-next-action text, correction status banner, and propagation progress display.
  - [x] Keep `ChatBotGovernedAction` for preview and correction submission; do not inline raw buttons or bypass its disabled-with-reason behavior.
  - [x] Preserve the finite disabled-reason mapping, including `candidate-required`, `evidence-expired`, `not-authorized`, `projection-pending`, `already-decided`, `already-corrected`, `audit-unavailable`, `corrected-context-stale`, `correction-delayed`, `policy-blocked`, `projection-invalidation-unavailable`, `stale-evidence`, `target-unauthorized`, and `terminal-state`.

- [x] Migrate `ChatBotAssociationCandidateRow` selection control (AC: 2, 4, 5)
  - [x] Replace the raw candidate `<button>` with `FluentButton Type="ButtonType.Button"` unless a better local Fluent v5 selection primitive is proven against the same source-contract tests.
  - [x] Preserve `role="radio"`, `aria-checked="@IsSelectedText"`, `aria-label="@AccessibleLabel"`, `data-chatbot-association-candidate`, `data-chatbot-selected`, `Candidate.ProjectId`, and `SelectAsync`.
  - [x] Keep the candidate row's visible rank, display label, confidence, reason code order, and evidence-chip list visible and screen-reader reachable.
  - [x] Do not use color or selected styling as the only selection cue; `aria-checked` and visible selected state must remain testable.

- [x] Migrate S2 page and comparison surface composition (AC: 3, 4, 7)
  - [x] Use `FluentStack` for layout-only wrappers in `Pages/AssociationReview` and `ChatBotAssociationEvidenceComparison` where semantics are not carried by the wrapper.
  - [x] Use `FluentCard` for selected-candidate/evidence comparison surfaces while preserving `section`, `article`, `dl/dt/dd`, `code`, and existing `aria-labelledby` / `aria-label` semantics where they carry contract meaning.
  - [x] Use `FluentText` for page/section titles and visible status/metadata text where it does not break heading ids, `FocusOnNavigate`, or landmark references.
  - [x] Keep `ChatBotConversationShell` as body content under the FrontComposer shell; do not introduce `<main>`, `<FrontComposerShell>`, app-owned providers, or a second store initializer in the page.
  - [x] Keep `ChatBotBlockedState` for no-authorized-candidate/fail-closed states and keep source metadata redaction-safe.

- [x] Keep CSS changes narrow and layout-only (AC: 3, 5, 7)
  - [x] Remove obsolete S2 native-control selectors only when the Fluent migration truly makes them stale, then update the CSS primitive backlog count in `ChatBotFluentConformanceTests`.
  - [x] Do not add `--chatbot-type-*`, `--chatbot-radius-*`, `--chatbot-font-*`, heading type-ramp declarations, foreground `color:` roles, `.chatbot-button`, legacy v4/FAST tokens, or new native `button/input/select/textarea` CSS selectors.
  - [x] Keep responsive, forced-colors, reduced-motion, and touch-target behavior intact for `.chatbot-association-review` and candidate rows.

- [x] Update focused tests and fixtures (AC: 5, 6)
  - [x] Update `ChatBotFluentConformanceTests.RawControlMigrationBacklog` by removing only `ChatBotAssociationCandidateRow.razor` and `ChatBotAssociationReviewActions.razor` after their raw controls are gone.
  - [x] Update `AssociationReviewComponentContractTests` to require `FluentButton`, `FluentLabel`, `FluentTextArea`, and relevant Fluent surface primitives in the S2 files while preserving existing S2 contract markers.
  - [x] Update `ChatBotAccessibilityFocusContractTests` to make Story 12.4 Fluent tags and preservation markers non-vacuous.
  - [x] Update `GovernedOperationsVisualFoundationE2ETests` source/fixture expectations that currently include raw S2 candidate `<button>` and note `<textarea>` markup; keep the fail-closed/redaction assertions.
  - [x] Update any affected `ProjectWorkspaceRouteContractTests` / raw-textarea source checks only if S2 file movement changes their expectations.

- [x] Verify and record results (AC: all)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`, restoring first only if needed.
  - [x] Run `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false`; if VSTest sockets are denied, run the compiled xUnit v3 executable fallback and record it.
  - [x] Run the focused UI tests covering `AssociationReviewComponentContractTests` and `ChatBotAccessibilityFocusContractTests`, using the compiled xUnit v3 executable fallback if needed.
  - [x] Run affected E2E/source-contract tests in `GovernedOperationsVisualFoundationE2ETests`; use browser fallback only if Playwright prerequisites are unavailable and record the limitation.
  - [x] Run `git diff --check`.
  - [x] Add `_bmad-output/implementation-artifacts/tests/test-summary-story-12.4.md` with exact commands, pass/fail status, and any environmental limitations.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: user `Jerome`, project `chatbot`, planning artifacts under `_bmad-output/planning-artifacts`, implementation artifacts under `_bmad-output/implementation-artifacts`, document language English.
- Loaded sprint status from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key `12-4-migrate-association-review-surface-to-fluent` was `backlog`; `epic-12` was already `in-progress`; Stories 12.1, 12.2, and 12.3 were `done`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`, especially Epic 12, Story 12.4, Story 2.5 S2 association review, UX-DR1/UX-DR2, UX-DR7, UX-DR24/UX-DR25, UX-DR35-UX-DR45.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially Frontend Architecture, ChatBot UI Fluent-only conformance, and Project Structure & Boundaries.
- Loaded source hints from `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`, which introduced Epic 12 and named the Story 12.4 target files.
- Loaded previous story intelligence from `_bmad-output/implementation-artifacts/12-3-migrate-conversation-stream-and-items-to-fluent.md` and recent commit `6336421 feat(story-12.3): Migrate conversation stream and items to Fluent`.
- Loaded persistent project-context facts from eight sibling `**/project-context.md` files. Relevant facts: .NET 10, `.slnx`, central package management, warnings-as-errors, xUnit v3 + Shouldly, `DiffEngine_Disabled=true` for Verify, no generated-output edits, no casual package upgrades, and root-level submodule-only policy.
- Inspected current target sources and focused tests under `src/Hexalith.ChatBot.UI/Components/Governed`, `src/Hexalith.ChatBot.UI/Components/Pages`, `tests/Hexalith.ChatBot.UI.Tests`, and `tests/Hexalith.ChatBot.UI.E2E.Tests`.

### Epic 12 Context

Epic 12 closes the component-level Fluent v5 gap left after Epic 10 adopted the FrontComposer shell while interior ChatBot surfaces still used raw/custom HTML over `chatbot.tokens.css`. The binding rule is that every `Hexalith.ChatBot.UI` `.razor` page/component uses FrontComposer or Fluent UI v5 components, with no raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` outside the temporary migration backlog. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]

Story 12.4 owns the S2 association review surface only: `ChatBotAssociationReviewActions`, `ChatBotAssociationCandidateRow`, `ChatBotAssociationEvidenceComparison`, and `Pages/AssociationReview`. Story 12.5 owns approval and governed-action surfaces including `ChatBotApprovalConversationItem`, `ChatBotWhyProjectPanel`, and `ChatBotTaskIntentReviewPanel`; Story 12.6 owns policy/config editors; Story 12.7 owns operational/audit pages; Story 12.8 retires `chatbot.tokens.css`; Story 12.9 re-runs cross-surface a11y/visual verification. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.4`; `_bmad-output/planning-artifacts/epics.md#Stories 12.5-12.9`]

### Current Implementation State

`ChatBotAssociationReviewActions` currently renders:

- Outer `section.chatbot-association-actions` labelled by `association-actions-title`.
- Raw decision note `<label>` + `<textarea class="chatbot-textarea">`, bound through `value="@DecisionNote"` and `@oninput="UpdateNote"`.
- `ChatBotStatusBanner` for validation errors.
- A command bar of `ChatBotGovernedAction` entries for choose candidate, reject all, defer, and mark needs-review, each with consequence text.
- Optional correction section with correction status banner, affected-context/downstream-impact/progress/owner/safe-next-action metadata, raw correction-rationale `<label>` + `<textarea>`, and `ChatBotGovernedAction` submit button.
- Disabled reason selection through `SpecificDisabledReasonPriority`, `ResolveDisabledReasonCode`, `ResolveCorrectionDisabledReasonCode`, and `DisabledReasonText`. Do not simplify this mapping during markup migration. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`]

`ChatBotAssociationCandidateRow` currently renders one raw lowercase `<button>` with `role="radio"`, `aria-checked`, `aria-label`, candidate data attributes, rank/title/meta/reason spans, evidence chips, and `@onclick="SelectAsync"`. This is the Story 12.4 raw-control backlog offender. Preserve the radio-like contract even if the rendered control becomes a Fluent button. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]

`ChatBotAssociationEvidenceComparison` currently renders a labelled comparison section, an empty `ChatBotStatusBanner` when no candidate is selected, and a selected-candidate `article` with project id, confidence, reason codes, and evidence chips. Preserve `data-chatbot-association-comparison="true"` and the metadata-only evidence behavior. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor`]

`Pages/AssociationReview` currently renders through `ChatBotConversationShell`, `ChatBotProjectContextHeader`, S2 page header, loading/error/status banners, `ChatBotBlockedState` for no authorized candidates, a `role="radiogroup"` candidate list, `ChatBotAssociationReviewActions`, evidence comparison, and source metadata. It dispatches Fluxor actions for load/select/note/rationale/preview/correction submit. This story must not change `AssociationReviewService`, effects, reducers, models, or command contracts unless a test requires a purely UI-facing fixture update. [Source: `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`; `src/Hexalith.ChatBot.UI/State/AssociationReview/*`; `src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs`]

### Fluent v5 Component Notes

The local package pin is binding: `Directory.Packages.props` pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, and `_Imports.razor` already imports `Microsoft.FluentUI.AspNetCore.Components`. Do not add package references or change versions. [Source: `Directory.Packages.props`; `src/Hexalith.ChatBot.UI/Components/_Imports.razor`; `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj`]

Local patterns already available:

- `ChatBotGovernedComposer` uses `FluentLabel` plus `FluentTextArea` with `Id`, `Value`, `ValueChanged`, `Immediate="true"`, and explicit `aria-*` attributes. Reuse this pattern for decision note and correction rationale instead of inventing raw event plumbing. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`]
- Story 12.3 conversation items use `FluentCard`, `FluentStack`, and `FluentText` while preserving semantic `article`, `section`, `ol/li`, `dl/dt/dd`, `code`, and `time` markup where those semantics are part of the contract. Use the same conservative conversion style here. [Source: `_bmad-output/implementation-artifacts/12-3-migrate-conversation-stream-and-items-to-fluent.md`; `src/Hexalith.ChatBot.UI/Components/Governed/*ConversationItem.razor`]
- `ChatBotGovernedAction` already wraps `FluentButton` and preserves disabled-with-reason behavior; keep it for S2 decision/correction actions rather than duplicating action-control logic. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`]

Latest official context checked during story creation:

- The Microsoft Fluent UI Blazor README describes `Microsoft.FluentUI.AspNetCore.Components` as the package for Fluent Design System Razor components for Blazor, with the demo/docs at `www.fluentui-blazor.net`. It also shows `AddFluentUIComponents()` and `_Imports.razor` usage, which this repo already has. This story must therefore use the locally pinned API surface and not introduce setup churn. [Source: `https://github.com/microsoft/fluentui-blazor`; `https://www.fluentui-blazor.net/`]
- The local NuGet XML for the pinned package confirms `FluentButton`, `FluentLabel`, `FluentTextArea`, `FluentCard`, `FluentStack`, and `FluentText` exist in the installed `net10.0` package. [Source: local NuGet package docs for `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`]

### Architecture and UX Guardrails

- UI may depend on ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts. It must not reference Server, Gateway internals, Dapr, EventStore internals, audit, idempotency, projection internals, CLI, or MCP. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- UX-DR1 requires component-level Fluent inheritance and build-enforced conformance; raw `<a>` nav links are allowed, but raw lowercase interactive controls are not. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`]
- UX-DR2 bans recreating Fluent-provided primitives in hand CSS. Custom CSS should be layout-only unless Story 12.8 is explicitly retiring token debt. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR2`]
- UX-DR7 defines Association Review as a fail-closed S2 surface for resolving ambiguous/failed email-to-project association with candidate evidence and no hidden auto-attach. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR7`]
- UX-DR24 requires disabled approval/association/correction controls to expose reachable reasons through `aria-disabled` plus announced reason or an adjacent focusable explanation. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR24`]
- UX-DR25 requires candidate rows to expose candidate project, confidence band, evidence chips, unavailable/unauthorized suppression, and actions. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR25`]
- UX-DR35-UX-DR45 require stable live-region behavior, keyboard/focus accessibility, reduced motion, redaction-safe off-surface affordances, association error recovery, cognitive-load ordering, responsive/touch support, and EN/FR localization. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR35-UX-DR45`]

### File Structure Requirements

Primary implementation locations:

- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.4.md`

Secondary files only if focused tests prove they need source/fixture expectation updates:

- `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`

Avoid these locations:

- Do not edit `Hexalith.FrontComposer`, `Hexalith.EventStore`, `Hexalith.Tenants`, `Hexalith.Parties`, or other sibling submodules.
- Do not edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Do not modify package pins in `Directory.Packages.props` or add inline package versions.
- Do not move association review behavior into backend, CLI, MCP, SignalR, service, effect, or reducer changes unless a UI test exposes an existing compile break.

### Previous Story Intelligence

Story 12.3 established the pattern for this migration:

- Use the local Fluent package pin; do not upgrade packages.
- Use PascalCase Fluent component tags so the case-sensitive governance regex does not false-match raw controls.
- Remove stale raw-control backlog entries only after the source no longer contains raw controls.
- Keep source-contract tests raw-tag-aware rather than case-insensitive substring checks that reject names such as `FluentTextArea`.
- Preserve semantic HTML (`article`, `section`, `ol/li`, `dl/dt/dd`, `code`, `time`) when semantics are part of governed contracts; use Fluent components around or inside them for surface/layout/text primitives.
- VSTest socket creation may fail in this sandbox; prior stories used compiled xUnit v3 executable fallback successfully.
- Record exact commands and results in the per-story test summary.
- Do not claim this UI story caused or fixed unrelated backend or submodule drift. [Source: `_bmad-output/implementation-artifacts/12-3-migrate-conversation-stream-and-items-to-fluent.md`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.3.md`]

### Git Intelligence

Recent relevant commits:

- `6336421 feat(story-12.3): Migrate conversation stream and items to Fluent`
- `6266d0c chore: Update subproject commits for FrontComposer and Memories`
- `09aa92b fix(tests): Enhance cross-tenant leakage scans in isolation tests`
- `6fb7edc feat(story-12.2): Migrate governed chat composer to Fluent`
- `0fcdf27 feat(story-12.1): Fluent-only governance guard`

The key implementation pattern is that Epic 12 migration stories shrink conformance debt through real source changes and source-contract tests. Do not weaken `ChatBotFluentConformanceTests`; make the target files pass and then remove their allowlist entries.

Current working tree note at story creation: there are pre-existing modified submodule pointers (`Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Parties`) and unrelated BMAD/test-summary artifacts. Do not revert them, and do not include submodule pointer changes in Story 12.4 unless the user explicitly asks. [Source: `git status --short` on 2026-06-21]

### Testing Standards

- Use xUnit v3 and Shouldly; avoid raw `Assert.*`.
- Keep `DiffEngine_Disabled=true` when running Verify-backed tests.
- Build with `.slnx`; do not create or use `.sln`.
- Prefer focused UI/governance/E2E project commands over broad solution-level test runs.
- Keep tests non-vacuous: assert target files exist, Fluent tags are present, raw controls are gone, and critical S2 markers remain.
- If browser prerequisites are unavailable, use existing fixture fallback and record the limitation honestly.

### Regression Traps to Avoid

- Do not turn S2 into a generic form and drop the fail-closed association-review workflow.
- Do not lose `role="radiogroup"`, `role="radio"`, `aria-checked`, candidate accessible labels, or candidate data attributes.
- Do not remove `ChatBotEvidenceChip` or leak unauthorized evidence text while converting candidate/comparison markup.
- Do not hide validation, disabled reasons, correction blocked reasons, or propagation progress behind tooltip-only behavior.
- Do not bypass `ChatBotGovernedAction` for association decisions or correction submission.
- Do not replace visible machine ids/codes with hidden-only content; preserve `code` where currently used.
- Do not add native-control CSS or primitive Fluent recreation while removing raw controls.
- Do not modify approval/governed-action controls owned by Story 12.5.
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
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.4: Migrate association review surface -> Fluent v5`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.5: Ambiguous association review surface (S2)`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]
- [Source: `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`]
- [Source: `_bmad-output/implementation-artifacts/12-2-migrate-governed-chat-composer-to-fluent.md`]
- [Source: `_bmad-output/implementation-artifacts/12-3-migrate-conversation-stream-and-items-to-fluent.md`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`]
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

- 2026-06-21: Loaded BMAD dev-story workflow, Hexalith repo instructions, UX instructions, sprint status, project-context facts, and Story 12.4 context. Preserved existing `baseline_commit: 6336421`.

### Implementation Plan

- Migrate S2 association review native controls to Fluent v5 primitives first, preserving command callback/event semantics.
- Convert S2 page/comparison layout wrappers conservatively with Fluent layout/surface/text components while keeping semantic `section`, `article`, `dl`, `code`, and landmark attributes.
- Tighten focused source-contract tests and E2E fixtures so Story 12.4 Fluent tags are required and raw S2 controls are not reintroduced.

### Completion Notes List

- Migrated S2 association review actions from raw labels/textareas to `FluentLabel` and `FluentTextArea` while preserving decision-note and correction-rationale callbacks, validation banners, correction status/progress metadata, and `ChatBotGovernedAction` submission controls.
- Migrated association candidate selection from a raw button to `FluentButton Type="ButtonType.Button"` while preserving radio semantics, candidate data attributes, accessible labels, visible ranking/confidence/reasons, evidence chips, and selected-state cues.
- Added Fluent surface/layout/text primitives to the S2 page, source metadata, and evidence comparison while preserving `ChatBotConversationShell`, `ChatBotProjectContextHeader`, `ChatBotBlockedState`, semantic sections/articles/definition lists/code, and redaction-safe metadata.
- Shrank the raw-control conformance backlog for the two Story 12.4 files and updated focused UI/E2E tests and fixtures to require the new Fluent tags.
- Validation completed: solution build passed; VSTest was blocked by sandbox socket permissions, so compiled xUnit v3 fallback was used successfully for governance, focused UI, full UI, and affected E2E/source-contract lanes. See `_bmad-output/implementation-artifacts/tests/test-summary-story-12.4.md`.

### File List

- `_bmad-output/implementation-artifacts/12-4-migrate-association-review-surface-to-fluent.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.4.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`

### Change Log

- 2026-06-21: Migrated Story 12.4 S2 association review surface to Fluent v5 primitives, updated focused source/E2E contract coverage, recorded validation results, and moved story to review.
- 2026-06-21: Adversarial code review (AI). Found 5 E2E tests failing on the real browser path (the dev test-summary's "39 total, 0 failed" was produced under the silent no-browser fallback) plus a confirmed `aria-invalid` accessibility defect. Auto-fixed all findings; re-validated with the browser actually running. Moved story to done.

## Senior Developer Review (AI)

**Reviewer:** Jerome — 2026-06-21
**Outcome:** Approve (after auto-fixes). 0 CRITICAL issues remain.

### Context

`/usr/bin/google-chrome` is present in this sandbox, so `GovernedOperationsVisualFoundationE2ETests` runs the **real Playwright browser path** (~19s wall clock). The dev's `test-summary-story-12.4.md` recorded "GovernedOperationsVisualFoundationE2ETests … 39 total, 0 failed", but that run took the silent string-only no-browser fallback (`BrowserHarness.TryStartAsync` returns null when no chrome executable resolves), so the browser-only assertions never executed.

### Findings and fixes

- **[CRITICAL → FIXED] 4 E2E tests failed on the browser path: `fill()` on `<fluent-text-area>` is impossible.** The fixtures migrated the note/rationale fields to `<fluent-text-area>` (per AC6) but the JS-less harness can't make an undefined custom element fillable, and the simulation scripts read `.value` (a custom element has none). Tests: `AssociationReviewShouldSubmitDecisionThroughUiCommandSpineAndRefreshStatus`, `AssociationReviewShouldSubmitCorrectionThroughUiCommandSpineAndShowPartialStatus`, `AssociationReviewShouldShowSafeIdempotencyConflictWithoutLeakingDecisionPayload`, `AssociationReviewShouldShowSafeCorrectionConflictWithoutLeakingPayload`.
  - Fix: added `role="textbox"` + `contenteditable="true"` to the filled `<fluent-text-area>` fixtures (mirroring the merged Story 12.3 `ProjectConversationE2ETests` composer precedent) and changed the reader scripts to read `value ?? textContent`.
- **[CRITICAL → FIXED] Reflow test failed: candidate row overflows at 800px.** `.chatbot-association-candidate` sets `width:100%` + padding + border; a native `<button>` is `border-box` via the UA stylesheet, but the migrated `<fluent-button>` host defaults to `content-box`, so it overflowed. Test: `AssociationReviewShouldReflowAcrossDesktopTabletAndPhoneWithoutUnsafeOverflow`.
  - Fix: added `box-sizing: border-box` to `.chatbot-association-candidate` (layout-only; does not change any tracked conformance-backlog count).
- **[CRITICAL → FIXED] False test claim.** The two idempotency/correction conflict tests typed the sentinel `"raw provider payload"` into the field and asserted whole-body `innerText` excludes it — which only worked because a real `<textarea>` keeps its value out of `innerText`. With a contenteditable field the user's own retained input legitimately appears, so the assertion was a false positive. Re-scoped the no-leak check to the conflict feedback region (`#association-submit-feedback` / `#association-correction-feedback`) — where an actual payload echo would surface — while keeping whole-body checks for the server-only sentinels (`restricted@example.com`, `Secret Project`).
- **[MEDIUM → FIXED] `aria-invalid` bound to a .NET `bool`.** The migration added `aria-invalid="@(!string.IsNullOrWhiteSpace(ValidationErrorCode))"` and `aria-invalid="@IsCorrectionValidationError"`. Per Microsoft Blazor guidance, ARIA state attributes must be string `"true"`/`"false"`; with a `bool`, Blazor **drops the attribute entirely when false** instead of rendering `aria-invalid="false"`. Replaced with string helpers `DecisionNoteInvalidText` / `CorrectionRationaleInvalidText` (matching the repo's existing `aria-checked="@IsSelectedText"` convention). Fixtures already hard-coded the explicit string, which is why no test caught it.

### Verification (browser path actually executed)

- `dotnet build Hexalith.ChatBot.slnx` → 0 warnings, 0 errors.
- Full `Hexalith.ChatBot.UI.Tests` (compiled xUnit v3 runner): **169 total, 0 failed** (includes Governance + both contract test classes).
- Full `Hexalith.ChatBot.UI.E2E.Tests` (compiled runner, `/usr/bin/google-chrome` present): **124 total, 0 failed**, ~21s wall clock confirming the real browser path ran. `GovernedOperationsVisualFoundationE2ETests` alone: 39/39.
- `git diff --check` → clean. Raw `<button>/<textarea>` count in the two migrated files: 0.

### AC status

AC1–AC7 verified satisfied after fixes. Scope remained a rendering-layer correction (CSS layout-only, fixtures/tests, and the story's own `.razor`); no backend / package / submodule changes.
