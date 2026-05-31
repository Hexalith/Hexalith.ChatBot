---
baseline_commit: f55ccb3
---

# Story 1.20: English/French localization infrastructure

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a localization owner,
I want English and French localization infrastructure established,
so that governed surfaces can be bilingual without losing contract-stable identifiers.

## Acceptance Criteria

1. **Localization is registered as a UI-owned foundation.** Given the ChatBot UI is Blazor + Fluent UI v5 via FrontComposer, when the UI host starts, then localization services are registered, only English and French cultures are supported for this story, English remains the fallback/default, request/user culture selection is applied before Razor component rendering, and the UI exposes a small typed culture/localization contract under `src/Hexalith.ChatBot.UI/Design/` or `src/Hexalith.ChatBot.UI/Localization/`. Do not add a second UI framework, MVC `IViewLocalizer`, `IHtmlLocalizer`, JavaScript localization framework, or package upgrade. [Source: _bmad-output/planning-artifacts/epics.md#Story-1.20-English-French-localization-infrastructure; _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture; https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0]

2. **Stable machine identifiers remain untranslated.** Given ChatBot relies on stable codes across UI, CLI, MCP, audit, contracts, and tests, when English or French is active, then machine codes, status codes, reason codes, command names, lifecycle enum values, surface origin values, correlation IDs, command IDs, task IDs, operation IDs, and audit metadata tokens remain byte-stable and untranslated. Display labels and explanations may be localized; identifiers use ordinal/invariant formatting and comparisons. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Localization; _bmad-output/planning-artifacts/architecture.md#Naming-Patterns; src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs; src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs]

3. **Governed UI text moves behind explicit localization keys.** Given `ChatBotGovernedUiText` currently hardcodes shared labels for actor categories, evidence states, risk classes, feedback kinds, blocked reasons, and interaction guardrails, when this story is complete, then those labels resolve through an English/French localization layer with stable keys and complete resource coverage for both cultures. The implementation must avoid string concatenation for accessible names and state descriptions by using phrase-level templates that include the whole accessible label or description. [Source: src/Hexalith.ChatBot.UI/Design/ChatBotGovernedUiText.cs; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Localization]

4. **Existing governed primitives consume localized labels without behavior drift.** Given Stories 1.14-1.19 created the governed UI foundation, when the culture changes between English and French, then `ChatBotProjectContextHeader`, `ChatBotConversationShell`, `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotBlockedState`, `ChatBotStatusBanner`, `ChatBotGovernedAction`, `ChatBotStreamingStopControl`, and `GovernedOperations.razor` show localized display labels and accessible names while preserving semantic token slots, live-region politeness, announcement dedup keys, focus return, disabled-reason reachability, forced-colors cues, touch sizing, and current governed-command behavior. [Source: _bmad-output/implementation-artifacts/1-19-live-region-and-reduced-motion-behavior.md#Completion-Notes-List; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor; src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]

5. **Locale-aware display formatting is explicit and bounded.** Given UX-DR45 requires localized dates, times, numbers, confidence bands, pluralization, and actor labels, when UI display helpers format user-facing values, then they use the active UI culture for display only and use invariant/ordinal formatting for wire, audit, IDs, generated client values, tests that assert machine contracts, and CSS/data attributes. Confidence bands and actor labels must have English/French display labels without changing enum names or serialized values. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Localization; src/Hexalith.ChatBot.Contracts/Enums/ThresholdBand.cs; src/Hexalith.ChatBot.Contracts/Enums/ActorType.cs; src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs]

6. **French expansion cannot hide safety-critical state or actions.** Given Story 1.17 established responsive/touch and dense-row collapse contracts, when French labels are rendered in buttons, chips, status banners, labelled rows, and dense-row fixture content, then critical words for actor, risk, state, confidence, next action, and safe recovery reason wrap, use an approved short label, or move to labelled detail before truncation. Raw IDs, secondary timestamps, low-priority metadata, and repeated context may collapse first. Do not add clipping rules that hide text or make overflow checks pass by masking content. [Source: src/Hexalith.ChatBot.UI/Design/ChatBotDenseRowCollapseContract.cs; src/Hexalith.ChatBot.UI/Design/ChatBotSmallScreenFallbackContract.cs; _bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md#Completion-Notes-List; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings]

7. **Focused tests prove localization is non-vacuous.** Given later S1/S2/S3 surfaces inherit this foundation, when tests run, then they fail if English/French resource keys drift, a resource is missing in either culture, stable identifiers are localized, accessible labels are built from unsafe concatenation, date/number/confidence/actor formatting ignores culture, French text expansion hides critical labels, package pins change, or Stories 1.17-1.19 responsive/accessibility/live-region/reduced-motion tests regress. Use existing xUnit v3, Shouldly, Playwright/static fixture patterns, and do not introduce a new assertion, accessibility, localization, or UI test framework. [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs; tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs; tests/Hexalith.ChatBot.UI.Tests/ChatBotLiveRegionReducedMotionContractTests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]

8. **Architecture boundaries stay intact.** Given localization is a UI foundation story, when implemented, then changes stay in `src/Hexalith.ChatBot.UI/`, `tests/Hexalith.ChatBot.UI.Tests/`, and `tests/Hexalith.ChatBot.UI.E2E.Tests/` unless a focused contract-test adjustment is needed. Do not edit Server, Gateway, DAPR, EventStore, generated client, OpenAPI, audit/idempotency seams, CLI, MCP, Workers, sibling submodules, or `Directory.Packages.props` package versions. [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries; Hexalith.FrontComposer/_bmad-output/project-context.md#Critical-Dont-Miss-Rules; Directory.Packages.props]

## Tasks / Subtasks

- [x] Register supported cultures and localization services in the UI host (AC: 1, 5, 8)
  - [x] Update `src/Hexalith.ChatBot.UI/Program.cs` to add localization services and request localization before `MapRazorComponents`.
  - [x] Define supported cultures as English and French only for this story, with English fallback/default.
  - [x] Keep culture handling UI-owned; do not modify backend command, audit, generated client, CLI, MCP, or DAPR code.
  - [x] Add `_Imports.razor` or namespace imports only as needed for the chosen localizer pattern.

- [x] Add UI localization resources and typed keys (AC: 2, 3, 5)
  - [x] Create a small localization area such as `src/Hexalith.ChatBot.UI/Localization/` with a dummy shared resource type, English/default `.resx`, and French `.fr.resx`.
  - [x] Add stable resource keys for shared governed labels, accessible label templates, state/status labels, actor/risk/evidence labels, disabled/reason phrasing, and current governed-operations fixture text.
  - [x] Make resource keys stable machine keys, not translated display text.
  - [x] Use phrase-level resource templates for accessible labels/descriptions such as actor badges, risk chips, evidence chips, status labels, disabled reasons, and blocked-state next actions.

- [x] Refactor governed text resolution without changing component behavior (AC: 2, 3, 4)
  - [x] Replace hardcoded shared labels in `ChatBotGovernedUiText` with localization-aware resolution while preserving existing method intent and stable slot values.
  - [x] Update `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotBlockedState`, `ChatBotStatusBanner`, `ChatBotGovernedAction`, `ChatBotStreamingStopControl`, `ChatBotProjectContextHeader`, and `GovernedOperations.razor` to consume localized labels/templates.
  - [x] Preserve data attributes, `StableId`, `AnnouncementKey`, enum names, `data-chatbot-status`, `data-chatbot-feedback-state`, `aria-live`, roles, and existing focus semantics.
  - [x] Keep visible machine codes such as `CompletionStatus`, `AuditStatus`, operation ID, command ID, lifecycle state, safe-next-action codes, and audit history metadata untranslated.

- [x] Add locale-aware display formatting contracts (AC: 2, 5)
  - [x] Add a UI-owned formatting contract/helper for display dates, times, numbers, confidence values/bands, pluralized counts, and actor labels.
  - [x] Assert display formatting uses the active `CultureInfo.CurrentCulture`/`CurrentUICulture`.
  - [x] Assert wire-stable values, generated client formatting, IDs, enum serialization, CSS hooks, and audit metadata continue to use invariant/ordinal behavior.
  - [x] Do not change generated client code or contract enum wire values.

- [x] Protect French expansion and responsive behavior (AC: 4, 6, 7)
  - [x] Update existing CSS only where needed to allow longer French labels to wrap inside buttons, chips, status banners, labelled rows, and dense fixtures.
  - [x] Reuse `ChatBotDenseRowCollapseContract`, `ChatBotSmallScreenFallbackContract`, responsive/touch tokens, and existing `overflow-wrap` patterns.
  - [x] Do not introduce `overflow-x: clip`, fixed one-line truncation, or hidden overflow on safety-critical labels.
  - [x] Keep forced-colors, focus rings, reduced-motion hooks, and touch target sizes intact.

- [x] Add focused localization tests (AC: 1-8)
  - [x] Add `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs` or equivalent for supported cultures, resource completeness, fallback, no missing keys, stable machine identifiers, no inline package version changes, and invariant/ordinal identifier formatting.
  - [x] Add tests that `ChatBotGovernedUiText` and governed primitives resolve different English/French display labels while stable slots/codes remain unchanged.
  - [x] Add tests that accessible labels/descriptions are phrase-level resources and are not assembled from individual translated fragments.
  - [x] Add tests for culture-aware date, number, confidence, pluralization, and actor label formatting.
  - [x] Extend `GovernedOperationsVisualFoundationE2ETests.cs` or a sibling fixture to render English and French fixture content, assert localized visible/accessibility labels, assert unchanged operation IDs/status codes/audit metadata, and assert no horizontal overflow at desktop/tablet/phone widths.
  - [x] Preserve deterministic static fallback behavior for restricted browser/socket environments.

- [x] Verify and document results (AC: 7, 8)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.E2E.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.Architecture.Tests` if dependencies or project references change.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, pass/fail counts, culture used by tests, and any browser/socket fallback behavior in this story's Dev Agent Record.

## Dev Notes

### Source Artifact Analysis

Story 1.20 is the localization foundation story for UX-DR45. It is not a feature surface story. It prepares English/French infrastructure inherited by later S1 project conversation, S2 association review, and S3 AI action approval surfaces. It must preserve the M0 UI foundation from Stories 1.14-1.19 and keep localization out of backend/audit/command contracts. [Source: _bmad-output/planning-artifacts/epics.md#Story-1.20-English-French-localization-infrastructure; _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]

The UX localization contract is specific: English and French UI text are supported; stable machine codes, status codes, reason codes, command names, and correlation IDs remain untranslated; display labels and explanations are translated; dates/times/numbers/confidence bands/pluralization/actor labels use locale-aware formatting; accessible names/state descriptions avoid concatenated strings; French expansion must not truncate critical state/action words. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Localization]

The PRD accessibility floor still applies. Localization must not weaken keyboard operation, labels, focus order, non-color status indicators, understandable status/failure/refusal/authorization messages, next-action clarity, or source-evidence/AI-summary distinction. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Accessibility-and-Usability-Quality]

### Current Implementation State

Files likely to be updated:

- `src/Hexalith.ChatBot.UI/Program.cs` currently registers Razor components, Fluent UI, Fluxor, typed client services, `GovernedOperationService`, and `ChatBotAnnouncementDeduplicationState`. It does not register localization services or request localization.
- `src/Hexalith.ChatBot.UI/Components/_Imports.razor` imports UI component/design/service namespaces but no localization namespace.
- `src/Hexalith.ChatBot.UI/Design/ChatBotGovernedUiText.cs` is the central hardcoded label helper for actor categories, evidence states, risk classes, feedback kinds, blocked reasons, and interaction guardrails. This is the preferred place to introduce localized governed text, not by spreading switch expressions through components.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor` builds fallback accessible labels using interpolation. Story 1.20 should move that fallback phrase to a localized template while preserving `data-chatbot-*`, `aria-live`, dedup, and role behavior.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`, `ChatBotEvidenceChip.razor`, `ChatBotRiskChip.razor`, `ChatBotBlockedState.razor`, and `ChatBotGovernedAction.razor` currently build accessible labels/reasons from fragments. Replace with phrase-level localizer calls.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor` currently owns "Stop response generation" and "Response stopped". Preserve it as the single Stop/Cancel announcement mechanism while localizing the visible and accessible text.
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` is still the current M0 fixture. It contains many English labels; localize display text but leave operation/status/audit codes visible and untranslated.
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` owns wrapping, responsive, reduced-motion, forced-colors, touch target, and governed primitive styles. French expansion fixes belong here if styles need adjustment.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs`, `ChatBotAccessibilityFocusContractTests.cs`, `ChatBotLiveRegionReducedMotionContractTests.cs`, and `ChatBotGovernedPrimitiveContractTests.cs` are the closest static contract test homes.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` already has browser and deterministic static fallback paths. Extend that style rather than adding a new E2E harness.

Preserve existing behavior:

- `GovernedOperations.razor` must still dispatch `SubmitGovernedNoteAction`.
- `GovernedOperationService` must still use `ChatBotSurfaceOrigin.Ui`.
- `OperationOutcome.CompletionStatus`, `AuditStatus`, `SafeNextActions`, IDs, and audit-history metadata are stable code/metadata strings and must remain untranslated.
- Existing status banner live-region matrix, announcement deduplication, blocked-state matrix consumption, retryable failure politeness, and reduced-motion behavior must remain green.
- Existing Story 1.17 responsive/touch behavior must not regress: no horizontal overflow, no content masking, 44x44 primary touch targets, 24x24 dense-secondary targets, visible safe metadata, and no viewport zoom lock.
- Existing Story 1.18 accessibility/focus behavior must not regress: skip-link/main focus path, unique region names, disabled reason reachability, busy-region focus preservation, validation summary focus, and field message association.
- UI must not reference `.Server`, gateway internals, DAPR clients, audit writer, idempotency store, projection store, mailbox, AI provider, CLI/MCP internals, Workers, or direct data-plane infrastructure.
- Do not add inline package versions or upgrade Fluent UI, Fluxor, FrontComposer, Playwright, xUnit, bUnit, or .NET.

### Previous Story Intelligence

Story 1.19 completed at baseline `f55ccb3` and added a UI-owned state-to-feedback matrix, live-region politeness/dedup contracts, reduced-motion contract, `ChatBotAnnouncementDeduplicationState`, and focused UI/E2E tests. Important implementation learnings for Story 1.20:

- `ChatBotStatusBanner` suppresses repeated live roles for stable announcement keys while preserving visible inline status. Localization must not change announcement keys or derive them from translated strings.
- `ChatBotBlockedState` consumes `ChatBotStateFeedbackMatrix`; do not reintroduce ad hoc role/politeness logic while localizing blocked-state copy.
- Retryable governed-note submission failures intentionally use polite live behavior with danger styling and retry availability. Do not localize by changing the state family.
- The final Story 1.19 validation passed `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`, `Hexalith.ChatBot.UI.Tests` 52/52, `Hexalith.ChatBot.UI.E2E.Tests` 15/15, and `git diff --check`; browser path was available.

[Source: _bmad-output/implementation-artifacts/1-19-live-region-and-reduced-motion-behavior.md#Completion-Notes-List; f55ccb3]

Story 1.18 established the accessibility/focus contracts. Story 1.20 should reuse them and add localization-specific requirements, not duplicate busy/validation/disabled/focus contracts. [Source: _bmad-output/implementation-artifacts/1-18-accessibility-and-focus-management-floor.md#Completion-Notes-List]

Story 1.17 established responsive/touch and dense-row collapse behavior. French expansion work should harden those contracts, not bypass them with clipping/truncation. [Source: _bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md#Completion-Notes-List]

Recent git context:

- `f55ccb3 feat(story-1.19): Live region and reduced motion behavior`
- `6c16298 feat(story-1.18): Accessibility and focus management floor`
- `ab529e2 feat(story-1.17): Responsive and touch foundation`
- `86f9dd6 feat(story-1.16): Interaction guardrails and streaming stop/cancel behavior`
- `f752df5 feat: Update orchestration status and steps for story 1.15`

Current dirty worktree entry observed during story creation: `_bmad-output/story-automator/orchestration-1-20260531-085840.md`. It is unrelated automation output and must not be reverted.

### Architecture and UX Guardrails

- UI stack is Blazor + Fluent UI v5 RC via FrontComposer, Fluxor state, REST commands/queries, and SignalR projection nudge. Do not add a second component library, CSS framework, JavaScript localization framework, native mobile layer, or MVC-localization dependency. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- Preserve dependency direction: UI depends only on Client and shared service defaults, not Server/gateway internals. [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries; src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj]
- FrontComposer customization requires preserving labels, keyboard reachability, focus visibility, live-region parity, reduced-motion, and forced-colors behavior. [Source: Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific-Rules]
- Stable machine values should use ordinal/invariant handling. Sibling context guidance explicitly uses `StringComparison.Ordinal`, `StringComparer.Ordinal`, and `CultureInfo.InvariantCulture` for identifiers, ordering, hashing, and wire-stable formatting; Story 1.20 should apply that split between display and machine values. [Source: Hexalith.Folders/_bmad-output/project-context.md#Critical-Implementation-Rules; src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs]
- Package versions are centrally managed in `Directory.Packages.props`; `.csproj` package references must not gain inline versions. [Source: Directory.Packages.props; Hexalith.FrontComposer/_bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Root-level submodule policy remains in force. Do not initialize or update nested submodules; this story should not need submodule commands. [Source: AGENTS.md; Hexalith.FrontComposer/_bmad-output/project-context.md#Development-Workflow-Rules]

### Latest Technical Notes

Web-verified on 2026-05-31: Microsoft Learn for ASP.NET Core Blazor globalization/localization says Blazor provides number/date formatting for globalization and uses the .NET Resources system for localization. `IStringLocalizer` and `IStringLocalizer<T>` are supported in Blazor apps; `IHtmlLocalizer` and `IViewLocalizer` are MVC features and are not supported in Blazor apps. Use the supported Blazor pattern. [Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0]

Web-verified on 2026-05-31: Microsoft Learn for ASP.NET Core localization documents `.resx` resource files and culture-specific resource naming using ISO language codes. Use a resource convention that is testable in this repo, and do not mix incompatible resource-location patterns. [Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0]

Web-verified on 2026-05-31: Microsoft Learn troubleshooting guidance calls out resource naming/embedded resource issues as common localization failures. Add tests that prove the English and French resources are discoverable through the exact localizer path used by the UI. [Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/troubleshoot-aspnet-core-localization?view=aspnetcore-10.0]

The repo currently pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, `Fluxor` to `6.9.0`, `Microsoft.Playwright` to `1.60.0`, `xunit.v3` to `3.2.2`, and `bunit` to `2.7.2`. Treat root package pins as authoritative; do not upgrade packages in this story. [Source: Directory.Packages.props]

### Suggested Implementation Shape

Prefer a narrow UI-owned addition:

```text
src/Hexalith.ChatBot.UI/
  Localization/
    SharedResource.cs
    SharedResource.resx
    SharedResource.fr.resx
    ChatBotUiTextKey.cs
    ChatBotUiTextLocalizer.cs
    ChatBotSupportedCulture.cs
    ChatBotCultureFormatting.cs
  Design/
    ChatBotLocalizationContract.cs
    ChatBotLocalizedTextExpansionContract.cs
  Components/Governed/
    ChatBotActorBadge.razor
    ChatBotEvidenceChip.razor
    ChatBotRiskChip.razor
    ChatBotBlockedState.razor
    ChatBotStatusBanner.razor
    ChatBotGovernedAction.razor
    ChatBotStreamingStopControl.razor
  Components/Pages/
    GovernedOperations.razor
  Program.cs
tests/
  Hexalith.ChatBot.UI.Tests/
    ChatBotLocalizationContractTests.cs
  Hexalith.ChatBot.UI.E2E.Tests/
    GovernedOperationsVisualFoundationE2ETests.cs
```

This shape is a suggestion, not a mandate. Keep one primary public type per file, file-scoped namespaces, and surrounding style. If a smaller shape fits the code better, keep it smaller.

### Testing Requirements

- Use xUnit v3 `3.2.2`, Shouldly `4.3.0`, Playwright `1.60.0`, and existing project patterns. No new assertion, browser, accessibility, component, or localization test library.
- Prefer deterministic component/static tests for resource/key/formatting coverage and Playwright/static fixture tests for rendered English/French labels, accessible names, overflow, and behavior preservation.
- Browser tests should select by role/name or explicit fixture metadata; CSS selectors are acceptable only when asserting `lang`, `dir`, data hooks, active element, overflow, animation/transition duration, or stable accessibility metadata.
- Test both `en`/English and `fr`/French resources through the actual `IStringLocalizer<T>` or UI localizer service used by components.
- Test missing keys fail loudly in contract tests; do not allow key names to render silently as production labels.
- Test machine identifiers stay unchanged under French culture: operation ID, command ID, task ID, correlation ID, lifecycle state, completion status, audit status, safe-next-action code, status/reason code, command name, `data-chatbot-*` values, and enum wire values.
- Test current user-facing labels change between English and French: actor category labels, evidence labels, risk labels, feedback kind labels, blocked reason labels, disabled reason phrase, Stop/Cancel announcement, status banner accessible labels, and governed-operations page labels.
- Test locale-aware formatting for dates/times/numbers/confidence/pluralization with explicit cultures.
- Test accessible names/descriptions use phrase-level resource templates, not unsafe concatenation of translated fragments.
- Test French expansion at desktop/tablet/phone widths and verify critical labels remain visible or moved into labelled row detail.
- Keep Story 1.17 responsive/touch tests, Story 1.18 accessibility/focus tests, and Story 1.19 live-region/reduced-motion tests green.
- Keep architecture boundary tests green if dependencies/project references change.

### Out of Scope

- Building S1 project conversation, S2 association review, S3 AI approval, queue rows, audit timeline, tenant configuration, command palette, export/copy/download/read-aloud affordances, or Story 1.21 redaction-safe recovery patterns.
- Translating backend audit envelopes, OpenAPI, generated client code, command/rejection/event contracts, enum wire values, command names, reason/status codes, identifiers, correlation IDs, or log/audit metadata.
- Adding tenant/user language preference persistence beyond the minimal culture-selection plumbing needed to prove the infrastructure.
- CLI/MCP localization, machine-surface documentation localization, M365 integration, AI provider behavior, DAPR/EventStore changes, backend validation messages, or production data behavior.
- Adding or upgrading packages, adding a JavaScript localization framework, adding a second UI/component framework, or introducing MVC-only localizer APIs.

### Project Structure Notes

- UI localization resources/services: `src/Hexalith.ChatBot.UI/Localization/`.
- Existing shared label helper: `src/Hexalith.ChatBot.UI/Design/ChatBotGovernedUiText.cs`.
- Optional localization contracts: `src/Hexalith.ChatBot.UI/Design/`.
- UI host registration: `src/Hexalith.ChatBot.UI/Program.cs`.
- Razor imports: `src/Hexalith.ChatBot.UI/Components/_Imports.razor`.
- Current fixture: `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`.
- Governed primitives: `src/Hexalith.ChatBot.UI/Components/Governed/`.
- CSS/text expansion: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Focused UI tests: `tests/Hexalith.ChatBot.UI.Tests/`.
- E2E/static fixture tests: `tests/Hexalith.ChatBot.UI.E2E.Tests/`.
- Boundary tests if dependencies change: `tests/Hexalith.ChatBot.Architecture.Tests/`.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create-Story-Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.20-English-French-localization-infrastructure]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Accessibility-and-Usability-Quality]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Localization]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings]
- [Source: _bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md]
- [Source: _bmad-output/implementation-artifacts/1-18-accessibility-and-focus-management-floor.md]
- [Source: _bmad-output/implementation-artifacts/1-19-live-region-and-reduced-motion-behavior.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.UI/Program.cs]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotGovernedUiText.cs]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotLiveRegionReducedMotionContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- [Source: Directory.Packages.props]
- [Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0]
- [Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0]
- [Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/troubleshoot-aspnet-core-localization?view=aspnetcore-10.0]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31T14:39+02:00 - Create-story workflow loaded skill, discover-inputs protocol, template, checklist, BMAD config, sprint status, planning artifacts, prior story 1.19, current UI source/tests, project context facts, recent git history, and official Microsoft localization docs.
- 2026-05-31T14:39+02:00 - Current dirty worktree entry before story creation: `_bmad-output/story-automator/orchestration-1-20260531-085840.md`; unrelated automation artifact, not reverted.
- 2026-05-31T14:39+02:00 - Checklist validation applied during story creation: tightened guardrails for stable identifiers, phrase-level accessible labels, French expansion, existing live-region/focus behavior, package pins, and UI-only scope.
- 2026-05-31T14:45+02:00 - Dev-story workflow moved story 1.20 from `ready-for-dev` to `in-progress` in sprint status; preserved existing `baseline_commit: f55ccb3`.
- 2026-05-31T14:55+02:00 - Initial `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` failed on a CA1062 null-validation issue in `ChatBotUiTextLocalizer.Get`; added explicit `ArgumentNullException.ThrowIfNull(arguments)`.
- 2026-05-31T14:56+02:00 - Second build failed on a test namespace reference for `RequestLocalizationOptions`; changed the test to infer the returned options type.
- 2026-05-31T14:57+02:00 - `Hexalith.ChatBot.UI.Tests` first run failed because test DI lacked logging for `ResourceManagerStringLocalizerFactory` and one old literal-English source assertion remained; added `AddLogging()` and updated the assertion to the localization key.
- 2026-05-31T14:58+02:00 - `IStringLocalizer<SharedResource>` initially missed resources when `ResourcesPath = "Localization"` was combined with colocated resources; removed the path override so runtime localizer and `SharedResource.ResourceManager` use the same base name.
- 2026-05-31T14:59+02:00 - Final validation passed: build 0 warnings/0 errors; UI tests 61/61; UI E2E tests 16/16; `git diff --check` clean. Architecture tests were not run because package/dependency/project-reference files did not change.
- 2026-05-31T15:20+02:00 - Senior Developer Review found and auto-fixed localized landmark drift and English-only safety-critical primitive defaults; validation passed build 0 warnings/0 errors, UI tests 63/63, UI E2E tests 17/17, and `git diff --check`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Discovery loaded sprint status, Epic 1 story context, architecture frontend/project-structure sections, UX localization/accessibility docs, PRD NFR60-NFR64 references, previous story 1.19, current UI components/tests, package pins, recent git history, FrontComposer and sibling project context facts, and web-verified Microsoft localization references.
- Checklist validation applied: story explicitly prevents translating stable machine identifiers, unsafe accessible-name concatenation, wrong file locations, package upgrades, backend scope creep, French text clipping, and regressions to responsive/accessibility/live-region/reduced-motion foundations.
- Implemented UI-owned English/French localization registration, supported-culture contract, shared `.resx` resources, typed resource keys, phrase-level localizer, and display-only culture formatter.
- Refactored governed primitives and `GovernedOperations.razor` to consume localized labels/templates while preserving stable slots, IDs, announcement keys, live-region metadata, focus behavior, and visible machine codes.
- Added CSS wrapping hardening for actor/chip/blocked-heading labels so longer French safety-critical text can wrap without clipping.
- Added focused localization contract tests for culture registration, resource completeness, actual `IStringLocalizer<SharedResource>` discovery, phrase-level accessible labels, culture-aware formatting, stable machine identifiers, package pins, and component source contracts.
- Extended the governed operations E2E/static fixture lane to exercise English and French fixture text, stable machine metadata, and phone-width overflow checks with deterministic fallback.
- Validation results: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed; `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` passed 61/61; `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` passed 16/16 using the browser path; `git diff --check` passed. Architecture test binary was not run because no package, dependency, or project-reference files changed.
- Senior Developer Review auto-fixes restored the governed command path main landmark through a page-specific localization key and moved default risk, blocked-state, safe-next-action, and disabled-reason safety copy behind English/French resources.
- Review validation results: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed; `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` passed 63/63; `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` passed 17/17; `git diff --check` passed. Architecture test binary was not run because no package, dependency, or project-reference files changed.

### File List

- `_bmad-output/implementation-artifacts/1-20-english-french-localization-infrastructure.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260531-085840.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectContextHeader.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Components/_Imports.razor`
- `src/Hexalith.ChatBot.UI/Design/ChatBotGovernedUiText.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotCultureFormatter.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotSupportedCultures.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Program.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`

### Senior Developer Review (AI)

Reviewer: Codex on 2026-05-31

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- HIGH: `GovernedOperations.razor` localized the main landmark with the generic conversation-detail key, which changed the existing "Governed command path" focus/landmark contract. Added `GovernedCommandPath` resources and updated the page and accessibility contract test to preserve the surface-specific landmark.
- HIGH: `ChatBotRiskChip`, `ChatBotBlockedState`, and `ChatBotGovernedAction` still rendered English default safety-critical reason/next-action text under French culture when callers used defaults. Added stable English/French resource keys and resolved those defaults through `ChatBotUiTextLocalizer`.
- MEDIUM: Localization tests did not prove safety-critical primitive defaults were localized. Added focused assertions for the new French resources and component source usage.
- MEDIUM: The story File List omitted changed automation/test-summary artifacts. Updated the File List for transparency; application-source review still excluded `_bmad-output/` content per workflow scope.

Checklist validation:

- Acceptance Criteria 1-8 cross-checked against UI source, resources, tests, and git changes.
- File List reconciled against git status; `_bmad-output/` artifacts treated as documentation/automation, not application source.
- Architecture boundaries stayed within UI/UI tests/E2E tests; no package pins, generated client, backend, CLI, MCP, DAPR, or submodule changes.
- Microsoft localization references were captured during story creation; no additional network lookup was required for this local implementation review.

### Change Log

- 2026-05-31: Implemented Story 1.20 English/French localization foundation, localized governed UI primitives/page text through stable resource keys, added culture-aware display formatting contracts, protected French wrapping, and added focused UI/E2E localization tests.
- 2026-05-31: Senior Developer Review auto-fixed localized landmark preservation and French/default safety-critical primitive text coverage; status moved to done.
