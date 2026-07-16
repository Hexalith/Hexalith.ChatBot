---
baseline_commit: b02e63a7bb3c542ec9f7320172c5ef6e9d8c7e16
---

# Story 10.7: Cross-surface a11y / visual / parity re-verification

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-11. -->
<!-- Senior Developer Review (AI) completed on 2026-06-11; see section below. -->

## Story

As a quality owner,
I want the shell-composed and new surfaces re-verified for accessibility, visual conformance, and cross-surface parity,
so that the migration and the new chat surface do not regress the governed floor.

## Acceptance Criteria

1. **Shell-composed surfaces pass the inherited accessibility floor.** Given the shell-composed surfaces from Stories 10.1-10.5, when verification runs, then Project Workspace, selected Project Conversation, S1 conversation stream, S2 association review, S3 AI approval, S8/S10 operational queues/dashboards, S9 audit investigation, and the governed composer retain WCAG 2.2 AA-oriented contracts: unique landmarks/region names, keyboard-reachable actions, visible focus, disabled reasons reachable without tooltip-only disclosure, correct live-region behavior, stable focus after load/send/validation/project switch, no duplicate shell/provider/store ownership, and no unauthorized detail leakage. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.7; _bmad-output/planning-artifacts/epics.md#UX-DR35; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility and keyboard operation]

2. **Light, dark, and forced-colors visual conformance is proved without a new palette.** Given all migrated and Epic 10 surfaces, when the visual gate runs, then semantic token aliases remain thin over Fluent/FrontComposer variables, no raw hex/rgb/hsl product palette is introduced, non-color status survives through text/icon/border cues, focus indicators satisfy the established focus floor, and fixtures cover light, dark, and forced-colors/high-contrast mode for status chips, banners, composer, conversation items, queue rows, and audit/operational surfaces. [Source: _bmad-output/planning-artifacts/epics.md#UX-DR3; _bmad-output/planning-artifacts/epics.md#UX-DR4; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Visual accessibility]

3. **EN+FR localization parity is intact across the changed surfaces.** Given the localized UI, when resource and fixture checks run under English and French cultures, then every `ChatBotUiTextKey` used by the Epic 10 surfaces resolves in both resources, French text expansion does not truncate critical state/action/recovery words in fixtures, and stable machine identifiers, reason codes, command names, correlation IDs, task IDs, project IDs, and policy tokens remain untranslated. [Source: _bmad-output/planning-artifacts/epics.md#UX-DR45; tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs]

4. **Epic 10 E2E gate is green for migrated and new surfaces.** Given the browser-optional Playwright-style E2E suite, when the Epic 10 a11y/visual gate runs, then existing fixtures and any new consolidated fixtures pass for shell ownership, project workspace states, conversation states, governed composer states, operational surfaces, audit surface, reduced-motion/forced-colors, and redaction-safe failure states. The fallback source assertions must remain meaningful when a browser is unavailable; do not add tests that pass vacuously because Chrome is missing. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.7; tests/Hexalith.ChatBot.UI.E2E.Tests/FrontComposerShellIntegrationE2ETests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]

5. **CLI/MCP parity remains unaffected by the composer.** Given the composer is a UI surface over the same CommandGateway/client spine, when parity checks run, then CLI and MCP state-changing/read intents still construct the same typed commands/read calls as the UI/API arm except for declared `ChatBotSurfaceOrigin`, no CLI/MCP tool gains an authorization-bypass affordance for composer-only behavior, and `RecordProjectConversationMessage` or any related conversation write remains governed through the shared client/submission path. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.7; _bmad-output/planning-artifacts/architecture.md#Parity by construction; tests/Hexalith.ChatBot.Conformance.Tests/Epic5AdapterIntentParityTests.cs; tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs; tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs]

6. **Story 10.6 backlog status is not hidden.** Given Stories 10.6a and 10.6b are still backlog at story creation time, when Story 10.7 verification is implemented, then it must not claim AI-response streaming transport, progressive response rendering, or full Stop/Cancel production verification as complete. It may verify the existing `ChatBotStreamingStopControl` primitive/localization contract and must record an explicit readiness caveat that final streaming end-to-end verification remains blocked until 10.6a ADR acceptance and 10.6b implementation. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status; _bmad-output/planning-artifacts/epics.md#Story 10.6a; _bmad-output/planning-artifacts/epics.md#Story 10.6b]

7. **Release-readiness evidence is concrete and reproducible.** Given the story completes, when reviewing Dev Agent Record and test summaries, then they list exact commands, pass/fail counts, browser availability/fallback mode, any readiness caveats, and the touched source/test files. Do not mark this story done on narrative inspection alone. [Source: .agents/skills/bmad-create-story/checklist.md; _bmad-output/planning-artifacts/epics.md#Epic 10]

## Tasks / Subtasks

- [x] Inventory the verification scope and existing coverage (AC: 1, 4, 6)
  - [x] Read and map current Story 10.1-10.5 implementation surfaces: `MainLayout.razor`, `App.razor`, `ProjectWorkspace.razor`, `ProjectConversation.razor`, `ChatBotProjectConversationWorkspace.razor`, `ChatBotGovernedComposer.razor`, S1/S2/S3 governed components, operational dashboard/queue pages, and audit investigation page.
  - [x] Confirm `10-6a-streaming-transport-adr` and `10-6b-streaming-ai-response-and-stop-cancel` are still backlog before deciding what streaming evidence can be claimed.
  - [x] Produce a small in-code or test-local matrix of surfaces/states covered by the Epic 10 gate so future removal of a surface cannot silently reduce coverage.

- [x] Extend accessibility/focus contract tests for Epic 10 surfaces (AC: 1, 3, 6)
  - [x] Extend `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs` or add a focused Epic 10 contract test covering unique shell/body/complementary labels, visible focus path, validation-summary focus, focus return to composer/proposal panel, disabled reason reachability, no hover-only critical action, and text-entry shortcut suppression.
  - [x] Include Project Workspace, selected Project Conversation, governed composer, association review, AI approval, operational dashboards/queues, and audit investigation in the contract matrix.
  - [x] Verify the existing `ChatBotStreamingStopControl` primitive only as a primitive/readiness contract; do not treat that as Story 10.6b completion.

- [x] Strengthen visual/token/forced-colors verification (AC: 2, 4)
  - [x] Extend `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs` and/or E2E fixtures to assert semantic aliases over Fluent/FrontComposer tokens, no raw `#`, `rgb(`, or `hsl(` product colors in `chatbot.tokens.css`, and no reintroduced "temporary inheritance bridge" language.
  - [x] Add or extend browser fixtures to emulate or source-prove light, dark, and forced-colors/high-contrast behavior for status chips, risk/evidence chips, banners, composer, queue rows, and audit rows.
  - [x] Preserve reduced-motion checks; do not add animation-only status for streaming, pending, or degraded states.

- [x] Consolidate the Epic 10 E2E a11y/visual gate (AC: 1, 2, 4, 6)
  - [x] Extend `FrontComposerShellIntegrationE2ETests`, `ProjectWorkspaceE2ETests`, `ProjectConversationE2ETests`, and `GovernedOperationsVisualFoundationE2ETests`, or add a new `Epic10ReleaseReadinessE2ETests.cs` that composes their fixtures without duplicating large HTML unnecessarily.
  - [x] Include browser-path assertions when Chrome is available and non-vacuous source assertions when it is not.
  - [x] Assert no duplicate `<FrontComposerShell>`, `<FluentProviders>`, `StoreInitializer`, app-owned provider tree, second `<main>`, page-level banner landmark, marketing hero, fake ungoverned textbox, or generated FrontComposer output edit.
  - [x] Assert redaction/leakage sentinels are absent from rendered/source fixtures: unauthorized project names, mailbox addresses, provider payloads, raw exception text, stack traces, hidden IDs, restricted file names, and audit internals.

- [x] Re-verify EN+FR localization and French expansion (AC: 3)
  - [x] Extend `ChatBotLocalizationContractTests` so every Epic 10 composer/workspace/shell verification label resolves in English and French.
  - [x] Add fixture checks for French text expansion in composer status/action labels, blocked/degraded states, queue/audit state labels, and safe next actions.
  - [x] Confirm stable machine tokens remain invariant under French culture.

- [x] Prove CLI/MCP parity was not changed by UI composer verification (AC: 5)
  - [x] Run the existing conformance tests for UI/API, CLI, and MCP arms and keep the surface list exactly `ui-api`, `cli`, `mcp`.
  - [x] If the conversation message command is added to a parity catalog, add it deliberately to all required arms through the shared client contract; otherwise add a regression assertion that Epic 10 UI-only composer verification does not alter CLI/MCP tool catalogs.
  - [x] Verify CLI and MCP continue to use `IChatBotClient` only and never direct Server, DAPR, EventStore, projection store, or gateway-stage internals.

- [x] Document readiness caveats and evidence (AC: 6, 7)
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary-story-10.7.md` or the established test-summary target with exact commands, pass/fail counts, and browser/fallback mode.
  - [x] State clearly that final streaming end-to-end verification remains pending until Story 10.6a/10.6b, unless those stories are completed before implementation starts.
  - [x] Do not update Epic 10 retrospective or mark Epic 10 done from this story unless sprint policy explicitly says all Epic 10 assignable stories are complete.

- [x] Run verification gates (AC: all)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run `DiffEngine_Disabled=true` with the compiled xUnit v3 in-process runner for `tests/Hexalith.ChatBot.UI.Tests`.
  - [x] Run `DiffEngine_Disabled=true` with the compiled xUnit v3 in-process runner for `tests/Hexalith.ChatBot.UI.E2E.Tests`.
  - [x] Run `tests/Hexalith.ChatBot.Conformance.Tests`, `tests/Hexalith.ChatBot.Cli.Tests`, and `tests/Hexalith.ChatBot.Mcp.Tests`.
  - [x] Run `tests/Hexalith.ChatBot.Architecture.Tests`.
  - [x] Run `git diff --check`.
  - [x] If `dotnet test` is used and VSTest socket permissions fail in the sandbox, switch to the compiled xUnit v3 runner and record the failed VSTest attempt honestly.

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`. Epic 10 is M2 release-readiness closure for FrontComposer Shell adoption and the governed interactive chat surface; Story 10.7 is the cross-surface verification story.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`. Relevant sections: fixed .NET/Blazor/FrontComposer stack, CommandGateway parity by construction, Frontend Architecture, project structure, and UI/CLI/MCP boundaries.
- Loaded `prd_content` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`; no separate PRD requirement overrides the Epic 10 acceptance criteria.
- Loaded `ux_content` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`, `EXPERIENCE.md`, and `epic10-chat-surface-elaboration.md`. Binding UX anchors are UX-DR3/4, UX-DR5-17, UX-DR32-35, UX-DR42-46.
- Loaded persistent project-context facts from 8 sibling `_bmad-output/project-context.md` files. Relevant facts: root-level submodule policy, centralized package versions, .NET SDK `10.0.302`, warnings-as-errors, metadata-only logging, FrontComposer read-only consumption, and no generated-output edits.
- Loaded previous Story 10.5. It added `ChatBotGovernedComposer`, `RecordProjectConversationMessage`, UI service/state/effects, localization keys, token styling, and focused tests. Story 10.7 should verify this work rather than redesign or reimplement it.

### Source Artifact Analysis

Epic 10's invariant is unchanged: every interactive write remains on the CommandGateway spine; risky AI requests become Epic 4 approval-required proposals, never inline execution. [Source: _bmad-output/planning-artifacts/epics.md#Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption]

Story 10.7's source ACs are intentionally broad. The implementation must turn them into an explicit release-readiness matrix for accessibility, visual conformance, localization, E2E, and CLI/MCP parity. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.7: Cross-surface a11y / visual / parity re-verification]

UX-DR4 and DESIGN.md require contrast in light/dark themes and forced-colors survival through text/icon/border, not fill alone. `chatbot.tokens.css` already aliases status colors to Fluent/FrontComposer variables and includes forced-colors coverage; Story 10.7 should broaden proof across surfaces. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Color and semantic status]

UX-DR45 requires EN+FR UI text with machine tokens left invariant. Existing `ChatBotLocalizationContractTests` already verifies complete resources and culture-sensitive formatting; extend it instead of adding ad hoc string scans only. [Source: _bmad-output/planning-artifacts/epics.md#UX-DR45; tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs]

The Epic 10 UX elaboration says Story 10.4-10.6 surfaces inherit WCAG 2.2 AA, light/dark/forced-colors, EN+FR, live-region, and focus-management rules. The same document still lists streaming transport as an open dependency. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md]

### Previous Story Intelligence

Story 10.1 completed shell ownership:

- `MainLayout.razor` owns the single `<FrontComposerShell AppTitle="Hexalith ChatBot">`.
- `App.razor` no longer renders app-owned `<FluentProviders />`; FrontComposer owns providers/store initialization.
- Startup order is `AddHexalithFrontComposerQuickstart()` then `AddHexalithDomain<ChatBotUiFrontComposerMarker>()` then `AddHexalithEventStore(...)`.

Story 10.2 and 10.3 migrated governed and operational surfaces:

- S1/S2/S3 and S8/S9/S10 surfaces render as FrontComposer body content, not nested app shells.
- Reuse `ChatBotConversationShell`, `ChatBotProjectContextHeader`, `ChatBotStatusBanner`, `ChatBotApprovalQueuePriorityView`, `ChatBotAiOutcomeConversationItem`, `ChatBotApprovalConversationItem`, and `ChatBotAiActionPreviewSections`.

Story 10.4 created Project Workspace:

- `/` is `ProjectWorkspace.razor`; selected-project and deep-link views reuse `ChatBotProjectConversationWorkspace.razor`.
- `/governed-operations` remains its own route.
- Existing workspace E2E fixtures already prove no project selected, cold load, empty, active, degraded, unauthorized/redacted, and project-switch success states.

Story 10.5 added the governed composer:

- `ChatBotGovernedComposer.razor` is placed inside `ChatBotProjectConversationWorkspace.razor` below the conversation stream.
- User messages submit through `RecordProjectConversationMessage`; ask-AI submits through the Epic 4 proposal path.
- UI state tracks mode, validation, submitting, and pending command receipt; durable transcript rows come only from server projection refresh.
- New localization keys live in `ChatBotUiTextKey`, `SharedResource.resx`, and `SharedResource.fr.resx`.
- Existing 10.5 validation passed build, UI, E2E, Contracts, Client, Server, Architecture tests, and `git diff --check`.

### Current Implementation State

Files likely to be updated:

- `tests/Hexalith.ChatBot.UI.E2E.Tests/FrontComposerShellIntegrationE2ETests.cs` - shell/provider/token runtime and source fallback checks.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs` - Project Workspace route and state fixtures.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - S1 conversation, approval, failure, AI outcome, attachment, participant, and composer fixture checks.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - governed operations, token, live-region, reduced-motion, retry/failure, and responsive visual foundation checks.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/OperationalDashboardsAccessibilityE2ETests.cs` plus adjacent operational/audit E2E tests - S8/S9/S10 a11y/freshness/status coverage.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs` - focus, landmark, validation, disabled action, shell/page contracts.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs` - primitive style/token/forced-colors contracts.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs` - EN+FR resource and phrase-level localization.
- `tests/Hexalith.ChatBot.Conformance.Tests/Epic5AdapterIntentParityTests.cs`, `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`, `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs` - parity proof.
- `_bmad-output/implementation-artifacts/tests/test-summary-story-10.7.md` or the established summary file - exact verification evidence.

Files to read before changing tests:

- `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor`
- `src/Hexalith.ChatBot.UI/Components/App.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`

### Project Structure Notes

- Verification work belongs in existing ChatBot test projects under `tests/Hexalith.ChatBot.UI.Tests`, `tests/Hexalith.ChatBot.UI.E2E.Tests`, `tests/Hexalith.ChatBot.Conformance.Tests`, `tests/Hexalith.ChatBot.Cli.Tests`, `tests/Hexalith.ChatBot.Mcp.Tests`, and `tests/Hexalith.ChatBot.Architecture.Tests`.
- UI source changes should stay in `src/Hexalith.ChatBot.UI`; only touch shared contracts/client/server code if a real parity gap is discovered and then broaden tests accordingly.
- Generated files under `obj/**/generated/HexalithFrontComposer/` and files inside `Hexalith.FrontComposer` are out of scope for this story.
- No nested submodule work is required; root-level submodule policy still applies.

### Architecture and UX Guardrails

- Do not implement Story 10.6a or 10.6b in this story. Streaming ADR, transport, progressive rendering, and production Stop/Cancel E2E remain separate work.
- Do not edit `Hexalith.FrontComposer` submodule contents, generated FrontComposer output, or nested submodules.
- Do not add or upgrade packages. `Directory.Packages.props` pins Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, Microsoft.Playwright `1.60.0`, xUnit v3 `3.2.2`, and bUnit `2.7.2`.
- Do not introduce raw color values, a one-off palette, decorative cards, marketing hero content, a second shell, or app-owned Fluent providers/store initializers.
- Do not let browser-unavailable fallbacks become false positives. Fallback assertions must inspect source/fixture contracts that would fail on real regressions.
- Do not conflate UI visual breakpoints with CLI/MCP parity. CLI and MCP are separate command surfaces over the same backend transitions, not responsive variants of the UI.
- Do not expose provider payloads, raw exception text, stack traces, restricted project/file/mailbox details, hidden identifiers, or audit internals in visual fixtures, snapshots, CLI output, MCP structured output, or test failure messages.

### Latest Technical Research

- NuGet package pages were checked on 2026-06-11 for critical local pins. The repo must stay on its pinned lane for this verification story: Fluent UI Blazor is intentionally pinned by `Directory.Packages.props`/FrontComposer, Fluxor.Blazor.Web `6.9.0`, Microsoft.Playwright `1.60.0`, and xUnit v3 `3.2.2`. Do not convert this story into a dependency upgrade. [Source: Directory.Packages.props; https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/; https://www.nuget.org/packages/Fluxor.Blazor.Web/6.9.0; https://www.nuget.org/packages/Microsoft.Playwright/1.60.0; https://www.nuget.org/packages/xunit.v3/3.2.2]

### Testing Notes

- Use xUnit v3, Shouldly, NSubstitute, bUnit/static source tests, and the existing browser-optional Playwright harness pattern.
- Set `DiffEngine_Disabled=true` for Verify/snapshot-style lanes.
- Existing E2E `BrowserHarness.TryStartAsync()` returns null when Chrome is unavailable. Preserve that behavior but ensure source fallback checks remain non-vacuous.
- Minimum validation for this story: build, UI.Tests, UI.E2E.Tests, Conformance.Tests, Cli.Tests, Mcp.Tests, Architecture.Tests, and `git diff --check`.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 10.7: Cross-surface a11y / visual / parity re-verification]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Parity by construction]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md]
- [Source: _bmad-output/implementation-artifacts/10-1-frontcomposer-shell-integration.md]
- [Source: _bmad-output/implementation-artifacts/10-2-migrate-m0-governed-surfaces-onto-shell.md]
- [Source: _bmad-output/implementation-artifacts/10-3-migrate-operational-surfaces-onto-shell.md]
- [Source: _bmad-output/implementation-artifacts/10-4-project-workspace-landing-route.md]
- [Source: _bmad-output/implementation-artifacts/10-5-governed-chat-composer.md]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/FrontComposerShellIntegrationE2ETests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs]
- [Source: tests/Hexalith.ChatBot.Conformance.Tests/Epic5AdapterIntentParityTests.cs]
- [Source: tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs]
- [Source: tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs]
- [Source: Directory.Packages.props]

## Dev Agent Record

### Agent Model Used

Codex (GPT-5)

### Debug Log References

- Loaded BMAD dev-story workflow and checklist, project config, sprint status, story 10.7, and persistent project-context files before editing.
- Inventoried shell ownership, project workspace/conversation, governed composer, association review, approval, operations, dashboards, audit investigation, localization resources, tokens, and streaming stop primitive source.
- Confirmed `10-6a-streaming-transport-adr` and `10-6b-streaming-ai-response-and-stop-cancel` remained backlog during implementation.
- `dotnet test` VSTest lanes failed before execution with sandbox socket permission denied; switched to the compiled xUnit v3 in-process runner and recorded the fallback.

### Completion Notes List

- Added Epic 10 accessibility/focus source matrix coverage across workspace, conversation, governed composer, S2/S3/S8/S9/S10 surfaces, and streaming stop primitive readiness.
- Added visual/token forced-colors contracts for the shared chatbot token stylesheet, preserving semantic aliases, non-color cues, reduced-motion coverage, and no temporary inheritance bridge language.
- Added EN+FR expansion coverage for critical state/action/recovery text across composer, workspace, queues, dashboards, audit, association review, approval, and streaming stop labels.
- Added Epic 10 release-readiness E2E gate that composes existing browser fixtures, asserts non-vacuous browser-unavailable fallbacks, pins shell ownership/leakage sentinels, and keeps streaming verification primitive-only until 10.6a/10.6b.
- Added CLI/MCP regression assertions proving composer verification did not add bypass affordances or direct server/Dapr/EventStore/projection-store dependencies.
- Wrote reproducible test summary with exact commands, pass/fail counts, Chrome availability, VSTest sandbox failure, and streaming readiness caveat.

### File List

- `_bmad-output/implementation-artifacts/10-7-cross-surface-a11y-visual-parity-reverification.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-10.7.md`
- `tests/Hexalith.ChatBot.Conformance.Tests/Epic5AdapterIntentParityTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/Epic10ReleaseReadinessE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`

### Change Log

- 2026-06-11: Implemented Story 10.7 verification-only coverage and evidence artifacts; moved story to review.
- 2026-06-11: Senior Developer Review (AI) — adversarial re-verification. Rebuilt clean (0 warnings/errors) and re-ran all six gates via the compiled xUnit v3 in-process runner: UI.Tests 148/0, UI.E2E.Tests 122/0 (real Chromium path, ~50s), Conformance 97/0, Cli 24/0, Mcp 30/0, Architecture 41/0. Fixed one LOW test-quality defect (dead/tautological assertion in `Epic10ReleaseReadinessE2ETests.BrowserUnavailableFallbacksShouldAssertSourceContractsInsteadOfReturningVacuously`). Status → done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-11
**Outcome:** Approve (auto-fix applied)

### Scope of verification

Adversarial re-verification of every claim in this story against actual implementation and a live test run, not narrative inspection:

- **File List vs git reality:** matches. The only changed file not listed is `_bmad-output/story-automator/orchestration-1-20260609-212026.md` (automator bookkeeping, out of review scope).
- **All 6 ACs traced to executing tests** that genuinely run and pass; every `ShouldContain` marker in the new source-grep contract tests was confirmed to exist in its target source/test file (accessibility surface markers, E2E gate-row method names, leakage sentinels, CSS selectors, localization keys, CLI/MCP facade strings).
- **AC4 anti-vacuous floor:** Chrome 148 present; the full `UI.E2E.Tests` suite ran the real browser path (~50s, 122/0), so the "0 failed" result is not a no-browser false positive.
- **AC5 CLI/MCP parity:** positive (`IChatBotClient`, `GetProjectConversationAsync`) and negative (`RecordProjectConversationMessage`, `SubmitProjectConversationComposer`, `Hexalith.ChatBot.Server`, `Dapr.`, `EventStore`, `ProjectionStore`) assertions verified against real CLI/MCP source.
- **AC2 visual floor:** `chatbot.tokens.css` contains zero raw hex/`rgb(`/`hsl(` product colors; forced-colors and reduced-motion media queries present; `#` hex is additionally guarded by the pre-existing test at `ChatBotGovernedPrimitiveContractTests.cs:163`.
- **AC3 French expansion:** the four asserted words resolve to real fr.resx values (`indisponible`→QueueDetailUnavailable, `accès`→ProjectSwitchSuccess, `dégrad`→ComposerDegradedReason, `arrêtée`→StopResponseAnnouncement).
- **AC6 streaming caveat:** 10-6a/10-6b confirmed still `backlog`; streaming verification kept primitive-only.

### Findings

| # | Severity | Finding | Disposition |
| --- | --- | --- | --- |
| 1 | LOW | `BrowserUnavailableFallbacksShouldAssertSourceContractsInsteadOfReturningVacuously` contained a dead assertion: `body.ShouldNotContain("return;\n        }")`. The regex captures the fallback body only up to the first `return;`, so the body can never contain `return;` — the assertion could never fail. Ironic in a test whose purpose is to forbid vacuous assertions. | **Fixed** — replaced with the live, stronger guard `body.ShouldContain("WithoutBrowser")`, verified true for all 73 fallback blocks across the 6 referenced E2E files, so a bare `return;` fallback would now fail. |
| 2 | LOW | `StreamingVerificationShouldRemainPrimitiveOnlyUntilStory106IsImplemented` couples to the exact `sprint-status.yaml` text `10-6a-...: backlog`, so it will break when 10.6a legitimately advances. | **Kept by design** — this is an intentional AC6 tripwire that forces re-examination of streaming claims when 10.6 status changes; "fixing" it would weaken the guard the story requires. Noted for the 10.6 implementer. |
| 3 | LOW | `source.ShouldContain("ShouldContain")` in the same test is a weak proxy. | **Kept** — redundant but harmless; the per-fallback `ShouldContain("Assert")`/`ShouldContain("WithoutBrowser")` checks carry the real weight. |

No CRITICAL, HIGH, or MEDIUM findings. No tasks marked `[x]` were found incomplete; no AC was missing or partial.
