---
baseline_commit: 1a623e98bb0e4722270f05b64a8140eaf43be4c8
---

# Story 12.6: Migrate policy/notification/escalation editors to Fluent v5

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-21. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a tenant administrator configuring governed collaboration,
I want the tenant policy, notification routing, and escalation policy editors rendered with Fluent v5 form primitives,
so that S5 configuration keeps bounded-schema validation, safe recovery, localization, and FrontComposer visual inheritance without raw HTML controls.

## Acceptance Criteria

1. **Escalation policy matrix uses Fluent form controls without changing bounded-schema semantics.** Given `ChatBotEscalationPolicyEditor`, when migrated, then every age threshold raw `<input type="number">` renders through the pinned Fluent numeric input component (`FluentNumberInput<int>` in the installed package; do not invent a `FluentNumberField` wrapper unless already present), every severity/target-role/channel raw `<select>` renders through `FluentSelect`/`FluentOption`, reason and validation fields render through `FluentTextInput`, labels render through `FluentLabel`, and `data-escalation-age-input`, `data-escalation-severity-select`, `data-escalation-role-select`, `data-escalation-channel-select`, `aria-label`, `aria-invalid`, `aria-describedby`, `EscalationPolicySchema.MaxAgeThresholdSeconds`, `SeverityTokens`, `EscalationTargetRoleTokens`, `ChannelTokens`, and bounded selector rejection remain behaviorally intact. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.6`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEscalationPolicyEditor.razor`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotEscalationPolicyEditorContractTests.cs`]

2. **Notification routing matrix uses Fluent selects and text input without weakening routing constraints.** Given `ChatBotNotificationRoutingEditor`, when migrated, then recipient-role and channel controls render as `FluentSelect`/`FluentOption`, reason and invalid-field inputs render as `FluentTextInput`, labels render as `FluentLabel`, and `data-routing-role-select`, `data-routing-channel-select`, `RecipientRoleTokens`, `ChannelTokens`, `notification-routing-validation-summary`, `aria-invalid`, `aria-describedby`, matrix rows, safe metadata, and stale-data recovery contracts remain intact. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.6`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotNotificationRoutingEditor.razor`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotNotificationRoutingEditorContractTests.cs`]

3. **Tenant policy editor uses Fluent labels/text input while preserving S5 safe recovery.** Given `ChatBotTenantPolicyEditor`, when migrated, then affected-field labels render as `FluentLabel`, invalid field inputs render as `FluentTextInput`, and `tenant-policy-validation-summary`, `data-mailbox-admin-s5`, mailbox degraded metadata, `ShownMailboxMetadata`, `permission-freshness`, `mailbox-degradation-banner`, safe next action text, `aria-invalid`, `aria-describedby`, and small-screen fallback markers are preserved. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.6`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotTenantPolicyEditorContractTests.cs`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Tenant Configuration`]

4. **Validation, focus, disabled-reason, and small-screen fallback behavior remain reachable.** Given all three editors, when validation or save-blocked states render, then the validation summary remains before fields, focus targets remain `Validation.SummaryId`, field errors remain linked by `aria-describedby`, `ChatBotStatusBanner` warning state remains localized, `ChatBotGovernedAction` disabled save actions still expose reachable reasons, and phone fallback summaries preserve draft/status information without dense editing. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotEscalationPolicyEditorContractTests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotNotificationRoutingEditorContractTests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotTenantPolicyEditorContractTests.cs`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Error recovery patterns`]

5. **EN and FR localization remain intact and no restricted content is introduced.** Given the editor source and localization tests, when migrated, then all visible labels/action text continue to flow through `ChatBotUiTextKey`/`ChatBotUiTextLocalizer`; stable machine tokens remain untranslated; French text expansion is not truncated in source fixtures; and no `projectName`, `providerPayload`, `rawClaims`, `mailboxSubject`, `recipientAddress`, `messageHeaders`, raw policy body, or unauthorized project/file/party details are introduced. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Localization`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`; editor contract tests]

6. **The Fluent conformance guard shrinks only for Story 12.6 files.** Given `ChatBotFluentConformanceTests`, when this story is complete, then `Components/Governed/ChatBotEscalationPolicyEditor.razor`, `Components/Governed/ChatBotNotificationRoutingEditor.razor`, and `Components/Governed/ChatBotTenantPolicyEditor.razor` are removed from `RawControlMigrationBacklog`; no raw lowercase `<input>` or `<select>` remains in those files; no new raw lowercase `<button>`, `<textarea>`, `<input>`, or `<select>` is introduced elsewhere; and later Story 12.7 backlog entry `Components/Pages/ComplianceAuditInvestigation.razor` remains untouched. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

7. **Focused tests and fixtures prove Fluent migration plus editor behavior.** Given source-contract, governance, localization, accessibility/focus, and editor E2E/source-fixture lanes, when updated, then tests assert required Fluent tags (`FluentLabel`, `FluentTextInput`, `FluentNumberInput`, `FluentSelect`, `FluentOption`), absence of raw controls in Story 12.6 target files, preservation of data markers and validation contracts, bounded selector tokens, metadata-only restrictions, and phone fallback behavior. Exact validation commands and results are recorded in `_bmad-output/implementation-artifacts/tests/test-summary-story-12.6.md`. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotEscalationPolicyEditorContractTests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotNotificationRoutingEditorContractTests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotTenantPolicyEditorContractTests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/EscalationPolicyEditorE2ETests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/NotificationRoutingEditorE2ETests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/TenantPolicyEditorE2ETests.cs`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md`]

8. **Scope remains a rendering-layer correction only.** Given Epic 12 constraints, when this story is complete, then there are no package upgrades, no Fluent version churn, no backend, CommandGateway, CLI, MCP, SignalR, notification/escalation runtime, tenant policy schema, audit, or EventStore behavior changes; no sibling submodule edits; no generated `obj/**/generated/HexalithFrontComposer/**` edits; no wholesale `chatbot.tokens.css` retirement; and no migration of compliance audit, operational dashboard, final CSS retirement, or cross-surface verification owned by Stories 12.7-12.9. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `Hexalith.AI.Tools/hexalith-llm-instructions.md`]

## Tasks / Subtasks

- [x] Migrate `ChatBotEscalationPolicyEditor` matrix and form fields (AC: 1, 4, 5, 6, 8)
  - [x] Replace the age-threshold raw number input with `FluentNumberInput<int>` using the existing min/max contract from `EscalationPolicySchema.MaxAgeThresholdSeconds`; preserve `data-escalation-age-input`, row-key labelling, and current value.
  - [x] Replace severity, target-role, and channel raw selects with `FluentSelect` plus `FluentOption` values sourced only from `SeverityTokens`, `EscalationTargetRoleTokens`, and `ChannelTokens`.
  - [x] Replace reason and invalid-field raw text inputs with `FluentTextInput`; preserve `id`, `aria-invalid`, `aria-describedby`, validation message ids, and safe next-action copy.
  - [x] Replace raw labels with `FluentLabel`; preserve `for`/target associations or an equivalent explicit accessible relationship accepted by the pinned Fluent component.
  - [x] Keep semantic `table`, `thead`, `tbody`, `tr`, `th`, `td`, `dl`, `dt`, `dd`, `code`, and phone fallback markup where it carries governed matrix or metadata meaning.

- [x] Migrate `ChatBotNotificationRoutingEditor` matrix and form fields (AC: 2, 4, 5, 6, 8)
  - [x] Replace recipient-role and channel raw selects with `FluentSelect` plus `FluentOption` sourced only from `RecipientRoleTokens` and `ChannelTokens`.
  - [x] Replace reason and invalid-field raw text inputs with `FluentTextInput`; preserve `id`, `aria-invalid`, `aria-describedby`, validation summary linkage, and safe stale-data recovery.
  - [x] Replace raw labels with `FluentLabel` while preserving localized labels and accessible field relationships.
  - [x] Preserve notification routing matrix row keys, metadata-only diff rows, `ChatBotGovernedAction` save behavior, and small-screen fallback.

- [x] Migrate `ChatBotTenantPolicyEditor` validation fields (AC: 3, 4, 5, 6, 8)
  - [x] Replace affected-field raw labels with `FluentLabel`.
  - [x] Replace affected-field raw inputs with `FluentTextInput`; preserve invalid state, described-by message ids, and validation summary focus behavior.
  - [x] Preserve mailbox degraded status, `data-mailbox-admin-s5`, `data-mailbox-dense-editor`, metadata-only rows, reconnect/content-read-denied governed actions, and phone fallback safe actions.
  - [x] Do not add policy editing scope beyond the existing scaffolded validation fields.

- [x] Update conformance and source-contract tests (AC: 1, 2, 3, 6, 7)
  - [x] Remove only the Story 12.6 target files from `RawControlMigrationBacklog` after raw controls are gone.
  - [x] Update `ChatBotEscalationPolicyEditorContractTests` to require `FluentNumberInput`, `FluentSelect`, `FluentOption`, `FluentTextInput`, and `FluentLabel`, while still asserting data markers, bounded tokens, validation, and no restricted markers.
  - [x] Update `ChatBotNotificationRoutingEditorContractTests` to require `FluentSelect`, `FluentOption`, `FluentTextInput`, and `FluentLabel`, while still asserting routing markers and no restricted markers.
  - [x] Update `ChatBotTenantPolicyEditorContractTests` to require `FluentTextInput` and `FluentLabel`, while still asserting mailbox degraded metadata, recovery markers, and no restricted markers.
  - [x] Keep tests raw-tag-aware and case-sensitive so names such as `FluentTextInput` and `FluentNumberInput` do not false-match raw `<input>`.

- [x] Update E2E/source fixtures for editor surfaces (AC: 4, 5, 7)
  - [x] Update `EscalationPolicyEditorE2ETests` fixtures if they hard-code native `input`/`select` selectors or CSS; prefer role/data-marker selectors that match the migrated Fluent custom elements.
  - [x] Update `NotificationRoutingEditorE2ETests` fixtures if they hard-code native `select`/`input` selectors or CSS.
  - [x] Update `TenantPolicyEditorE2ETests` fixtures if they simulate save/approval controls or invalid fields with native-only assumptions; preserve browser-path assertions.
  - [x] Record whether real Playwright/Chromium ran or whether only source-fixture fallback assertions ran.

- [x] Verify and record results (AC: all)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`, restoring first only if needed.
  - [x] Run `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false`; if VSTest sockets are denied, run the compiled xUnit v3 executable fallback and record it.
  - [x] Run focused UI tests covering `ChatBotFluentConformanceTests`, `ChatBotEscalationPolicyEditorContractTests`, `ChatBotNotificationRoutingEditorContractTests`, `ChatBotTenantPolicyEditorContractTests`, `ChatBotAccessibilityFocusContractTests`, and `ChatBotLocalizationContractTests`.
  - [x] Run affected E2E/source-fixture tests: `EscalationPolicyEditorE2ETests`, `NotificationRoutingEditorE2ETests`, and `TenantPolicyEditorE2ETests`.
  - [x] Run `git diff --check`.
  - [x] Add `_bmad-output/implementation-artifacts/tests/test-summary-story-12.6.md` with exact commands, pass/fail status, and environmental limitations.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: user `Jerome`, project `chatbot`, planning artifacts under `_bmad-output/planning-artifacts`, implementation artifacts under `_bmad-output/implementation-artifacts`, document language English.
- Loaded sprint status from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key `12-6-migrate-policy-notification-escalation-editors-to-fluent` was `backlog`; `epic-12` was already `in-progress`; Stories 12.1-12.5 were `done`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`, especially Epic 12, Story 12.6, and Epic 7 stories 7.2, 7.6, and 7.7 for policy, notification, and escalation context.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially Frontend Architecture and the ChatBot UI Fluent-only conformance rule.
- Loaded UX context from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`, `EXPERIENCE.md`, and `epic10-chat-surface-elaboration.md`.
- Loaded source hints from `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`, which introduced Epic 12 and named the Story 12.6 target files.
- Loaded PRD context through `epics.md` mappings and the PRD references for FR52, FR72-FR79, FR75d, NFR2, NFR6, NFR35, NFR46, NFR60, and UX-DR12/UX-DR40.
- Loaded previous story intelligence from `_bmad-output/implementation-artifacts/12-5-migrate-approval-and-governed-action-surfaces-to-fluent.md` and `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md`.
- Loaded persistent project-context facts from sibling `**/project-context.md` files. Relevant facts: .NET 10, `.slnx`, central package management, warnings-as-errors, xUnit v3 and Shouldly, `DiffEngine_Disabled=true` for Verify, root-level submodule-only policy, no generated-output edits, no casual package upgrades, and FrontComposer/Fluent-only UI rules.
- Inspected current target sources and focused tests under `src/Hexalith.ChatBot.UI/Components/Governed`, `tests/Hexalith.ChatBot.UI.Tests`, and `tests/Hexalith.ChatBot.UI.E2E.Tests`.

### Epic 12 Context

Epic 12 closes the component-level Fluent v5 gap left after Epic 10 adopted the FrontComposer shell while interior ChatBot surfaces still used raw HTML over `chatbot.tokens.css`. The binding rule is that every `Hexalith.ChatBot.UI` `.razor` page/component uses FrontComposer or Fluent UI v5 components, with no raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` outside the temporary migration backlog. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

Story 12.6 owns the S5 policy/configuration editors: `ChatBotEscalationPolicyEditor`, `ChatBotNotificationRoutingEditor`, and `ChatBotTenantPolicyEditor`. Story 12.7 owns operational/audit pages; Story 12.8 owns final `chatbot.tokens.css` retirement; Story 12.9 performs final cross-surface a11y/visual verification. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.6`; `_bmad-output/planning-artifacts/epics.md#Stories 12.7-12.9`]

### Current Implementation State

`ChatBotEscalationPolicyEditor` currently contains raw controls in the escalation matrix and form grid: one number input per row for age threshold, three select columns for severity/target role/channel, a change-reason text input, and validation-field text inputs plus labels. Preserve the matrix table semantics, row keys, `data-escalation-*` markers, schema-bound tokens, validation summary placement, safe next action, disabled save reason, and phone fallback. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEscalationPolicyEditor.razor`]

`ChatBotNotificationRoutingEditor` currently contains raw selects for recipient role/channel, a change-reason text input, validation-field text inputs, and labels. Preserve routing matrix table semantics, `data-routing-*` markers, token lists, validation summary placement, disabled save reason, metadata-only rows, and phone fallback. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotNotificationRoutingEditor.razor`]

`ChatBotTenantPolicyEditor` currently contains raw labels and inputs only in the affected-field validation loop. It also owns mailbox degraded state, mailbox admin metadata-only sections, reconnect/content-read-denied governed actions, and phone fallback summaries. Do not broaden the editor into a real policy mutation form beyond the existing scaffold. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor`]

`ChatBotFluentConformanceTests` currently lists exactly the Story 12.6 target files plus `Components/Pages/ComplianceAuditInvestigation.razor` in `RawControlMigrationBacklog`. A stale-entry assertion fails once a backlog file no longer contains raw controls, so the guard must shrink in the same implementation. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]

### Fluent v5 Component Notes

The local package pin is binding: `Directory.Packages.props` pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, and `_Imports.razor` already imports `Microsoft.FluentUI.AspNetCore.Components`. Do not add package references or change versions. [Source: `Directory.Packages.props`; `src/Hexalith.ChatBot.UI/Components/_Imports.razor`; `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj`]

The Epic 12 shorthand says `FluentTextField`/`FluentNumberField`, but the installed package docs expose `FluentTextInput` and `FluentNumberInput<TValue>` for this RC. Use the actual local API names unless compile-time inspection proves the project has an existing alias/wrapper. Do not introduce a wrapper solely to match the epic shorthand. [Source: local NuGet package docs under `~/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml`]

Local patterns already available:

- `ChatBotGovernedComposer` and `ChatBotAssociationReviewActions` show accepted `FluentLabel`, `FluentTextArea`, and `FluentTextInput` patterns with explicit `Id`, value binding, and `aria-*` preservation. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`]
- Story 12.4 established the conservative migration pattern: keep semantic HTML where it carries contract meaning and replace only the presentation/control primitive. [Source: `_bmad-output/implementation-artifacts/12-4-migrate-association-review-surface-to-fluent.md`]
- Story 12.5 found that browser-path E2E can fail where source fixtures pass; when using Fluent custom elements, verify real Playwright behavior if Chromium is available. [Source: `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md`]

### Architecture and UX Guardrails

- UI may depend on ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts. It must not reference Server, Gateway internals, Dapr, EventStore internals, audit, idempotency, projection internals, CLI, or MCP. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- UX-DR1 requires component-level Fluent inheritance and build-enforced conformance; raw `<a>` nav links are allowed, but raw lowercase interactive controls are not. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`]
- UX-DR2 bans recreating Fluent-provided primitives in hand CSS. Custom CSS should be layout-only unless Story 12.8 is explicitly retiring token debt. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`]
- Tenant configuration validation summaries appear before fields, field-level errors remain near controls, and save conflicts explain policy, mailbox permission, or stale-data causes. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Error recovery patterns`]
- Disabled or unavailable actions must have reachable explanation; tooltip-only explanations are insufficient. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component Patterns`]
- Notification recipients lacking authority must not receive restricted project detail, and escalation edits must record bounded configuration/audit context. This UI story must preserve those metadata-only constraints and must not add detail exposure. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6`; `_bmad-output/planning-artifacts/epics.md#Story 7.7`]

### File Structure Requirements

Primary implementation locations:

- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEscalationPolicyEditor.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotNotificationRoutingEditor.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotEscalationPolicyEditorContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotNotificationRoutingEditorContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotTenantPolicyEditorContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/EscalationPolicyEditorE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/NotificationRoutingEditorE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/TenantPolicyEditorE2ETests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.6.md`

Secondary files only if focused tests prove they need source/fixture expectation updates:

- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`

Avoid these locations:

- Do not edit `Hexalith.FrontComposer`, `Hexalith.EventStore`, `Hexalith.Tenants`, `Hexalith.Parties`, `Hexalith.Memories`, or other sibling submodules.
- Do not edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Do not modify package pins in `Directory.Packages.props` or add inline package versions.
- Do not move policy, notification, escalation, mailbox, or validation behavior into backend, CLI, MCP, SignalR, service, effect, or reducer changes unless an existing compile break proves a narrow UI-facing contract mismatch.

### Previous Story Intelligence

Story 12.5 established and reinforced the migration/validation pattern for this story:

- Use the local Fluent package pin; do not upgrade packages.
- Use PascalCase Fluent component tags so the case-sensitive governance regex does not false-match raw controls.
- Remove stale raw-control backlog entries only after source no longer contains raw controls.
- Keep source-contract tests raw-tag-aware rather than case-insensitive substring checks that reject names such as `FluentTextInput`.
- Preserve semantic HTML (`article`, `section`, `aside`, `ol/li`, `dl/dt/dd`, `code`, `time`, and for this story `table`/`th`/`td`) when semantics are part of governed contracts; use Fluent components for controls and layout/text primitives around them.
- VSTest socket creation may fail in this sandbox; prior stories used the compiled xUnit v3 executable fallback successfully.
- Browser E2E fallback can mask real custom-element failures. Story 12.5 review found real Chromium failures not caught by string fallback, then fixed them. Prefer browser-path coverage for migrated Fluent inputs/selects.
- Record exact commands and results in the per-story test summary.
- Do not claim this UI story caused or fixed unrelated backend or submodule drift. [Source: `_bmad-output/implementation-artifacts/12-5-migrate-approval-and-governed-action-surfaces-to-fluent.md`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md`]

### Git Intelligence

Recent relevant commits:

- `1a623e9 feat(story-12.5): Migrate approval and governed action surfaces to Fluent`
- `c3232b5 feat(story-12.4): Migrate association review surface to Fluent`
- `6336421 feat(story-12.3): Migrate conversation stream and items to Fluent`
- `6266d0c chore: Update subproject commits for FrontComposer and Memories`
- `09aa92b fix(tests): Enhance cross-tenant leakage scans in isolation tests`

Story 12.5 modified approval/task-intent/why-project target files, focused source-contract tests, E2E fixtures, the conformance backlog, and per-story test summary. The important pattern is to make real source changes, update focused tests, then shrink the conformance backlog rather than weakening the guard. [Source: `git log -5 --oneline`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md`]

Current working tree note at story creation: there are pre-existing modified submodule pointers (`Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Parties`, `Hexalith.Tenants`) and an unrelated story-automator orchestration artifact. Do not revert them, and do not include submodule pointer changes in Story 12.6 unless the user explicitly asks. [Source: `git status --short` on 2026-06-21]

### Testing Standards

- Use xUnit v3 and Shouldly; avoid raw `Assert.*`.
- Keep `DiffEngine_Disabled=true` when running Verify-backed tests.
- Build with `.slnx`; do not create or use `.sln`.
- Prefer focused UI/governance/E2E project commands over broad solution-level test runs.
- Keep tests non-vacuous: assert target files exist, Fluent tags are present, raw controls are gone, data markers remain, and metadata-only restrictions remain.
- If browser prerequisites are unavailable, use existing fixture fallback and record the limitation honestly.

### Regression Traps to Avoid

- Do not chase non-existent `FluentTextField`/`FluentNumberField` APIs if the installed RC exposes `FluentTextInput`/`FluentNumberInput<TValue>`.
- Do not replace bounded token selects with free-text or unbounded options.
- Do not remove `data-escalation-*`, `data-routing-*`, `data-validation-placement`, `data-small-screen-fallback`, or `data-mailbox-admin-s5` markers; tests and E2E fixtures use them as contracts.
- Do not remove `aria-label`, `aria-invalid`, `aria-describedby`, validation message ids, reachable disabled-reason paragraphs, or validation-summary focus behavior.
- Do not make save actions natively disabled in a way that hides current `ChatBotGovernedAction` reachable reason behavior.
- Do not leak policy bodies, mailbox subjects, raw claims, provider payloads, recipient addresses, message headers, project names, or restricted evidence in fixtures or UI text.
- Do not convert matrix tables to generic card walls unless tests and accessibility contracts prove row/column labels survive; preserve the table semantics by default.
- Do not make phone fallback editable; it remains read-only summary plus safe actions/recovery.
- Do not migrate compliance audit, operational dashboards, final CSS retirement, or cross-surface re-verification in this story.
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
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.6: Migrate policy/notification/escalation editors -> Fluent v5`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.2: Policy-admin scope, Tenant Policy Schema editor, and AI action policy`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6: Notification routing and delivery`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.7: Escalation policy for unresolved states`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`]
- [Source: `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`]
- [Source: `_bmad-output/implementation-artifacts/12-4-migrate-association-review-surface-to-fluent.md`]
- [Source: `_bmad-output/implementation-artifacts/12-5-migrate-approval-and-governed-action-surfaces-to-fluent.md`]
- [Source: `_bmad-output/implementation-artifacts/tests/test-summary-story-12.5.md`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEscalationPolicyEditor.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotNotificationRoutingEditor.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotEscalationPolicyEditorContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotNotificationRoutingEditorContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotTenantPolicyEditorContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/EscalationPolicyEditorE2ETests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/NotificationRoutingEditorE2ETests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/TenantPolicyEditorE2ETests.cs`]
- [Source: `Directory.Packages.props`]
- [Source: `src/Hexalith.ChatBot.UI/Components/_Imports.razor`]
- [Source: local NuGet package docs for `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`]
- [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`]
- [Source: `Hexalith.EventStore/_bmad-output/project-context.md`]
- [Source: `Hexalith.Conversations/_bmad-output/project-context.md`]
- [Source: `Hexalith.Projects/_bmad-output/project-context.md`]
- [Source: `Hexalith.Folders/_bmad-output/project-context.md`]
- [Source: `Hexalith.Parties/_bmad-output/project-context.md`]
- [Source: `Hexalith.Tenants/_bmad-output/project-context.md`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red phase: Added source-contract assertions for Fluent tags/raw control absence, then confirmed the current implementation failed through the xUnit fallback.
- VSTest limitation: `dotnet test ... --filter "Category=Governance"` aborted before test execution with `System.Net.Sockets.SocketException (13): Permission denied`; used the compiled xUnit v3 executable fallback for governance and focused UI lanes.
- Validation results recorded in `_bmad-output/implementation-artifacts/tests/test-summary-story-12.6.md`.

### Completion Notes List

- Migrated `ChatBotEscalationPolicyEditor` raw age, selector, reason, and validation controls to `FluentNumberInput<int>`, `FluentSelect`, `FluentOption`, `FluentTextInput`, and `FluentLabel` while preserving table semantics, bounded tokens, data markers, aria contracts, validation summary placement, disabled save reason, and phone fallback.
- Migrated `ChatBotNotificationRoutingEditor` raw selector, reason, and validation controls to Fluent v5 equivalents while preserving routing tokens, stale-data recovery contracts, matrix metadata, and small-screen fallback.
- Migrated `ChatBotTenantPolicyEditor` affected-field validation labels/inputs to Fluent v5 while preserving S5 mailbox degraded metadata, governed actions, validation focus behavior, and recovery text.
- Shrank `RawControlMigrationBacklog` only for the three Story 12.6 target files; `Components/Pages/ComplianceAuditInvestigation.razor` remains in backlog for Story 12.7.
- Updated focused source-contract and E2E/source-fixture tests to assert Fluent tags, raw-control absence, stable data markers, and metadata-only restrictions.

### File List

- `_bmad-output/implementation-artifacts/12-6-migrate-policy-notification-escalation-editors-to-fluent.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.6.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEscalationPolicyEditor.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotNotificationRoutingEditor.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/EscalationPolicyEditorE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/NotificationRoutingEditorE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/TenantPolicyEditorE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotEscalationPolicyEditorContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotNotificationRoutingEditorContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotTenantPolicyEditorContractTests.cs`

## Change Log

- 2026-06-21: Migrated policy, notification routing, and escalation editor raw controls to Fluent v5 primitives; tightened source/E2E contract tests; shrank the raw-control conformance backlog for Story 12.6; recorded validation results.
- 2026-06-21: Senior Developer Review (AI) — auto-fixed 1 HIGH (masked E2E browser-path failure) and 1 MEDIUM (accessible-name regression on migrated text inputs); status set to done. UI 170/170, editor E2E 20/20 (real Chrome), full E2E 127/127, build 0 warnings.

## Senior Developer Review (AI)

Reviewer: Jerome — 2026-06-21 — Outcome: Approve (issues fixed automatically)

Adversarial review validated every File List claim against git, rebuilt the UI with warnings-as-errors, verified the pinned Fluent RC component API from the installed package XML + scoped CSS + the Fluent MCP, and re-ran the focused/full UI and E2E suites on the real Chrome browser path.

### Findings and resolutions

- **[HIGH — FIXED] Masked E2E browser-path failure; inaccurate verification record.** `EscalationPolicyEditor_MatrixEdit_SubmitsMetadataOnlyGovernedCommand` failed deterministically (3/3) on real Chrome (read stale age `3600` vs filled `1800`). Root cause: the age `<fluent-number-input>` fill used bare `FillAsync`, which never updates the `value` attribute the fixture read-back (`controlValue`) checks before `textContent`; the selects already used an attribute-setting helper but the number input did not. The story's recorded "20 passed (real Chrome)" reflected the no-browser string fallback masking the failure (confirmed: forcing the Chrome path off makes the same lane report 4/4) — the exact Story 12.5 / `chatbot-e2e-nobrowser-fallback-trap` pattern. Fix: added `SetFluentNumberInputValueAsync` (mirrors `SetFluentSelectValueAsync`) and used it for the age field. Now genuinely 20/20 on real Chrome. [`tests/Hexalith.ChatBot.UI.E2E.Tests/EscalationPolicyEditorE2ETests.cs`]
- **[MEDIUM — FIXED] Accessible-name regression on migrated text inputs (AC1/AC2/AC3/AC4).** v5 `FluentLabel` renders a `<fluent-label>` custom element (verified: no `For` parameter in package XML/MCP; scoped CSS `fluent-field[label-position=before]>fluent-label[slot=label]`), so the splatted `for="@fieldId"` is inert — it only associates on a native `<label>`. The migrated reason/affected-field `FluentTextInput`s carried no `Label`/`AriaLabel`/`aria-label`, so they lost the accessible name the original `<label for>` + `<input id>` provided (the matrix number/select controls were unaffected — they kept explicit `aria-label`). The dev's own E2E fixtures revealed the gap by adding `aria-label="Reason code"` that the real components lacked. Fix: added `aria-label` (reusing the same `ChatBotUiTextKey` each `FluentLabel` renders, so the name always matches the visible label) to all five migrated text inputs, consistent with the in-file matrix controls. [`ChatBotEscalationPolicyEditor.razor`, `ChatBotNotificationRoutingEditor.razor`, `ChatBotTenantPolicyEditor.razor`]
- **[LOW — noted, pre-existing] Reason input `Value` hardcoded to its field-id literal** (`Value="escalation-change-reason"` / `"routing-change-reason"`). Carried over unchanged from the pre-migration scaffold; the editors are static `CreateDefault()` scaffolds, so no behavioral effect. Out of scope for this rendering-layer story.
- **[LOW — noted] E2E fixtures remain decoupled from the real components** (hand-authored HTML, not bUnit rendering). The MEDIUM fix narrows the gap by aligning the components' accessible naming with the fixtures'. This is the established repo test strategy ([`chatbot-ui-no-bunit-test-strategy`]); no structural change made.

### Verification

- `dotnet build tests/Hexalith.ChatBot.UI.Tests/...csproj` and `...UI.E2E.Tests...csproj`: succeeded, 0 warnings / 0 errors (warnings-as-errors on).
- Focused UI lanes (`ChatBotFluentConformanceTests`, the 3 editor contract tests, `ChatBotAccessibilityFocusContractTests`, `ChatBotLocalizationContractTests`): 40/40.
- Full `Hexalith.ChatBot.UI.Tests`: 170/170. Full `Hexalith.ChatBot.UI.E2E.Tests` on real Chrome 148: 127/127 (21s wall — browser path, not fallback). Editor E2E (3 classes): 20/20.
- `git diff --check`: clean. Backlog correctly retains only `Components/Pages/ComplianceAuditInvestigation.razor` (Story 12.7); package pins unchanged; no submodule/backend/generated edits introduced by the review.
