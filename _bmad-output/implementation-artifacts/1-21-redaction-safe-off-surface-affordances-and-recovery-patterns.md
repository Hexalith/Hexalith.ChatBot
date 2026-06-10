---
baseline_commit: 8a1957d
---

# Story 1.21: Redaction-safe off-surface affordances and recovery patterns

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a UX and security owner,
I want off-surface affordances and error-recovery patterns to preserve redaction and cognitive-load rules,
so that exported, copied, downloaded, and read-aloud content stays as safe as the visual surface.

## Acceptance Criteria

1. **Off-surface affordances inherit visual redaction.** Given any export, copy-to-clipboard, download-transcript, read-aloud, copy/share handoff, audit-copy, evidence-copy, or future off-surface affordance is exposed by a governed UI component, when it prepares its artifact, clipboard text, accessible name, accessible description, or read-aloud text, then it uses the same redacted display payload as the visual surface, never a hidden raw source payload, and includes a screen-reader-equivalent redaction message such as "This export is redacted; full detail requires escalation." The redaction notice must not contain redacted source text. [Source: _bmad-output/planning-artifacts/epics.md#Story-1.21-Redaction-safe-off-surface-affordances-and-recovery-patterns; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor; _bmad-output/planning-artifacts/epics.md#UX-DR39]

2. **Current governed primitives expose a reusable redaction contract.** Given `ChatBotEvidenceChip`, `ChatBotBlockedState`, `ChatBotStatusBanner`, `ChatBotGovernedAction`, `ChatBotSmallScreenFallbackContract`, and `GovernedOperations.razor` are the current M0 fixture and primitive surface, when this story is complete, then there is a UI-owned contract/model for off-surface affordances under `src/Hexalith.ChatBot.UI/Design/` or a similarly local UI namespace that records visual text, off-surface text, redaction state, disabled reason, escalation guidance, and accessible message. Redacted or unauthorized evidence remains non-openable, and any current/future off-surface action for evidence or audit history can only receive metadata-only/redacted text. [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor; src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor; src/Hexalith.ChatBot.UI/State/GovernedOperations/OperationOutcome.cs; _bmad-output/planning-artifacts/architecture.md#Redaction-and-data-governance]

3. **Error-recovery patterns are mechanically encoded.** Given UX-DR40 defines recovery behavior for association review, AI action review, queue retry, correction, and tenant configuration, when developers add those surfaces later, then they consume a typed UI contract that encodes the safe failure category, focus target, preserved selection or draft state where valid, still-valid actions, duplicate-safety/retry-count copy, affected-context preview, validation summary placement, field-message association, and save-conflict cause. The contract must prevent raw exception text, restricted project/file/party/audit detail, or unauthorized resource existence from appearing in the recovery message. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Error-recovery-patterns; _bmad-output/planning-artifacts/epics.md#UX-DR40; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR40]

4. **Cognitive-load guardrails are enforceable by contract and fixture.** Given UX-DR41 requires one primary action, stable information order, plain-language summaries before IDs, active-filter summaries, consolidated state messages, and labelled-row reflow, when this story is complete, then UI-owned contracts/tests prove these rules for current fixtures and future row/panel surfaces. Evidence/risk/status/actor/timestamp order is the canonical cross-surface order for candidate rows, proposals, queues, and audit entries; raw IDs remain available as metadata or expandable detail after the user-facing summary. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Cognitive-load-guardrails; src/Hexalith.ChatBot.UI/Design/ChatBotDenseRowCollapseContract.cs; src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingContract.cs]

5. **Localization, accessibility, and live-region behavior remain intact.** Given Story 1.20 moved governed UI text behind English/French resources, when redaction/recovery/cognitive-load messages render, then English and French resources exist for phrase-level accessible messages, stable machine identifiers remain untranslated, redaction notices are not assembled from unsafe translated fragments, and existing Story 1.18/1.19 focus, disabled-reason, live-region, and reduced-motion contracts remain green. Status updates use existing `ChatBotStateFeedbackMatrix` behavior; do not create a second announcement system. [Source: _bmad-output/implementation-artifacts/1-20-english-french-localization-infrastructure.md#Completion-Notes-List; src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs; src/Hexalith.ChatBot.UI/Design/ChatBotStateFeedbackMatrix.cs; https://www.w3.org/WAI/WCAG21/Techniques/aria/ARIA22]

6. **No backend export or browser API overreach.** Given this is an Epic 1 UI foundation story, when implementation is done, then changes stay inside `src/Hexalith.ChatBot.UI/`, `tests/Hexalith.ChatBot.UI.Tests/`, and `tests/Hexalith.ChatBot.UI.E2E.Tests/` unless a focused architecture-test assertion is required. Do not build Story 9.8 tenant export, real transcript download pipelines, backend redaction policy engines, CLI/MCP off-surface behavior, OpenAPI/generated-client changes, Server/Gateway/DAPR/EventStore changes, package upgrades, or browser-storage coupling. If clipboard behavior is added for the current UI fixture, it must be user-initiated, use a redacted string, handle unavailable browser permissions with a safe status message, and keep deterministic non-browser tests. [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries; _bmad-output/planning-artifacts/epics.md#Story-9.8-Tenant-export-workflow; https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API]

7. **Focused tests prove non-leakage and non-vacuous UX behavior.** Given later S1/S2/S3 and queue/audit/admin surfaces inherit these rules, when tests run, then they fail if off-surface text contains restricted source text while the visual state is redacted, accessible names/descriptions include redacted source text, redaction messages are missing in either English or French, recovery contracts omit a safe next action or focus target, cognitive-load ordering drifts, more than one primary action appears for one workflow item, active-filter summary/result count is missing where filters exist, dense rows drop state/reason/safe-action on phone, or Stories 1.17-1.20 responsive/accessibility/live-region/localization tests regress. Use existing xUnit v3, Shouldly, Playwright/static fixture patterns, and no new test framework. [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs; tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs; tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]

## Tasks / Subtasks

- [x] Add UI-owned off-surface redaction contracts (AC: 1, 2, 6)
  - [x] Add focused design types under `src/Hexalith.ChatBot.UI/Design/`, for example `ChatBotOffSurfaceAffordanceContract`, `ChatBotOffSurfaceRedactionState`, and `ChatBotOffSurfaceAffordanceKind`.
  - [x] Model at least these kinds: `Export`, `CopyToClipboard`, `DownloadTranscript`, `ReadAloud`, `CopyShareHandoff`, `AuditCopy`, and `EvidenceCopy`.
  - [x] Require redacted visual text, redacted off-surface text, redaction state, safe accessible name, safe accessible description, disabled reason, and escalation guidance.
  - [x] Ensure the contract cannot be marked complete when off-surface text is blank, when an accessible name/description contains restricted source text, or when redacted/unauthorized states lack a screen-reader-equivalent redaction message.

- [x] Add localized redaction and recovery microcopy keys (AC: 1, 3, 5, 7)
  - [x] Extend `ChatBotUiTextKey`, `SharedResource.resx`, and `SharedResource.fr.resx` with phrase-level keys for redacted off-surface notice, full-detail escalation guidance, copy/export unavailable reason, duplicate-safe retry copy, active-filter summary, and recovery safe-next-action messages.
  - [x] Add typed methods to `ChatBotUiTextLocalizer` only where they prevent unsafe string assembly.
  - [x] Keep resource keys stable machine identifiers and keep command/status/reason/correlation IDs untranslated.
  - [x] Do not add a JavaScript localization framework, MVC localizer APIs, or package changes.

- [x] Wire current primitives and fixture to the contracts without inventing product export scope (AC: 1, 2, 4, 6)
  - [x] Update `ChatBotEvidenceChip` contracts/tests so redacted/unauthorized evidence stays non-openable and any future off-surface action must reference a redacted affordance contract.
  - [x] Keep `GovernedOperationService.ToAuditHistoryLines` metadata-only: phase, decision/outcome, audit status, origin, and correlation are allowed; payloads, tenant/resource names, file names, raw source text, secrets, and exception text are not.
  - [x] If adding a fixture copy action in `GovernedOperations.razor`, copy only the metadata-only audit/status text plus the redaction notice, expose a localized accessible description, and fall back to a safe status message when browser clipboard access is unavailable.
  - [x] Preserve existing `SubmitGovernedNoteAction`, `ChatBotSurfaceOrigin.Ui`, live-region announcement keys, focus order, and primary action behavior.

- [x] Encode UX-DR40 recovery patterns as reusable UI contracts (AC: 3, 5, 7)
  - [x] Add a typed contract/enumeration for the five flows: association review, AI action review, queue retry, correction, and tenant configuration.
  - [x] For association review, require safe failure category, candidate selection preservation when still valid, focus to summary, and only still-valid confirm/reject/defer/escalate actions.
  - [x] For AI action review, require explicit confirmation copy for externally visible, file-exposing, project-mutating, tool-invoking, or participant-representing actions, and preserve audit-visible rejection/revision/cancel outcomes.
  - [x] For queue retry, require duplicate-safety text, retry count, focus return to row status on failed retry, and visible next safe action.
  - [x] For correction, require policy rationale when needed, affected attachments/derived AI context preview, and success/partial/blocked status without unauthorized detail.
  - [x] For tenant configuration, require validation summary before fields, field-level errors near controls, and save-conflict cause constrained to policy, permission, or stale data.

- [x] Encode UX-DR41 cognitive-load guardrails (AC: 4, 5, 7)
  - [x] Add a contract for one primary action per workflow item, with secondary and destructive actions grouped after it.
  - [x] Add a canonical ordered field list: evidence, risk, status, actor, timestamp; document how this maps to candidate rows, proposals, queues, and audit entries.
  - [x] Require plain-language summary text before raw IDs in the current `GovernedOperations.razor` fixture and any new test fixture.
  - [x] Extend `ChatBotQueueLoadingContract` or a sibling contract so active filters must render a visible summary plus result count.
  - [x] Reuse `ChatBotDenseRowCollapseContract` and `ChatBotSmallScreenFallbackContract`; do not mask overflow or drop label/state/reason/safe-action on phone.

- [x] Add focused unit/static tests (AC: 1-7)
  - [x] Add `tests/Hexalith.ChatBot.UI.Tests/ChatBotOffSurfaceRedactionContractTests.cs` or equivalent.
  - [x] Add tests that redacted/unauthorized off-surface affordances fail if raw source text appears in exported/copied/read-aloud text, accessible names, or accessible descriptions.
  - [x] Add tests that English/French resources include redaction/recovery/cognitive-load keys and that phrase-level accessible messages resolve through `ChatBotUiTextLocalizer`.
  - [x] Add tests that current governed primitives and `GovernedOperations.razor` do not expose raw exception text, payloads, tenant/resource names, file names, or unrestricted audit detail in off-surface-ready strings.
  - [x] Add tests for recovery-flow completeness and cognitive-load ordering/primary-action constraints.

- [x] Extend the existing E2E/static fixture lane (AC: 1, 4, 5, 7)
  - [x] Extend `GovernedOperationsVisualFoundationE2ETests.cs` or a sibling fixture using the existing browser/static fallback pattern.
  - [x] Assert the redaction notice is visible or screen-reader reachable for redacted off-surface actions.
  - [x] Assert English and French fixture variants keep stable IDs/codes untranslated while redaction/recovery labels localize.
  - [x] Assert one primary action per workflow item, active-filter summary plus result count where filters appear, and no phone-width overflow that hides label/state/reason/safe-action.
  - [x] Avoid CSS-only selectors except where existing tests use them for stable layout, overflow, data hooks, or accessibility metadata.

- [x] Verify and document results (AC: 5-7)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.UI.E2E.Tests`.
  - [x] Run the compiled xUnit v3 binary for `Hexalith.ChatBot.Architecture.Tests` if dependencies, project references, package files, or boundary assertions change.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, pass/fail counts, English/French coverage, browser/static fallback path, and any clipboard-permission fallback behavior in this story's Dev Agent Record.

## Dev Notes

### Source Artifact Analysis

Story 1.21 closes the last Epic 1 accessibility/localization split from the sprint change proposal. Stories 1.18-1.20 already established keyboard/focus, live-region/reduced-motion, and English/French localization foundations. This story is the redaction-safe off-surface, recovery, and cognitive-load foundation inherited by later S1/S2/S3, queue, audit, and tenant-configuration surfaces. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-30.md#Story-1.12-Accessibility-floor-and-English/French-localization; _bmad-output/planning-artifacts/epics.md#Story-1.21-Redaction-safe-off-surface-affordances-and-recovery-patterns]

The core UX requirement is explicit: off-surface affordances must apply the same redaction as the visual surface; accessible names/descriptions must not contain redacted source text; the surface must expose an equivalent message for screen-reader users that the export is redacted and full detail requires escalation. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor; _bmad-output/planning-artifacts/epics.md#UX-DR39]

The recovery and cognitive-load rules are also explicit. Recovery patterns cover association review, AI action review, queue retry, correction, and tenant configuration. Cognitive-load rules require one primary next action, consistent evidence/risk/status/actor/timestamp order, plain-language summaries before IDs, active-filter summary plus result count, consolidated state messages, and labelled-row reflow on small screens. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Error-recovery-patterns; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Cognitive-load-guardrails]

PRD NFRs reinforce the same constraints: WCAG 2.2 AA is scoped per increment; accessibility validation includes keyboard-only, screen-reader labels, focus order, non-color status indicators, and error recovery; failure/refusal/authorization messages must be understandable without exposing restricted evidence; users must identify next action without raw audit logs. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR61; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR62; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR63]

### Current Implementation State

Files likely to be updated:

- `src/Hexalith.ChatBot.UI/Design/ChatBotAccessibilityFloorContract.cs` currently enumerates keyboard operation, repeated landmark naming, visible-order focus sequence, focus return, disabled-action explanation, busy-region focus preservation, and validation error association. It does not yet enumerate off-surface redaction equivalence.
- `src/Hexalith.ChatBot.UI/Design/ChatBotDenseRowCollapseContract.cs` already preserves project, actor, risk, state, confidence, time, reason, and next action while allowing raw ID, secondary timestamp, and repeated context to collapse first.
- `src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingContract.cs` already requires `ActiveFilterDescription` and `ResultCount`; use this as the active-filter summary base rather than inventing a second queue contract.
- `src/Hexalith.ChatBot.UI/Design/ChatBotSmallScreenFallbackContract.cs` already includes a `HandoffLinkLabel`; Story 1.21 must make clear that copy/share handoff is also an off-surface affordance and must be redaction-safe.
- `src/Hexalith.ChatBot.UI/Design/ChatBotValidationErrorContract.cs` already requires summary, focus target, field-message association, and safe next action. Recovery contracts should reuse this pattern for tenant configuration and association/approval validation.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor` already treats `Unavailable`, `Redacted`, and `Unauthorized` as unavailable, sets `aria-disabled`, references a reason, and suppresses activation. Do not weaken that behavior while adding off-surface guidance.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor`, `ChatBotGovernedAction.razor`, and `ChatBotStatusBanner.razor` already resolve default safety copy through `ChatBotUiTextLocalizer`; extend localization there only if the new contracts need phrase-level messages.
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` is still the M0 governed-command fixture. It currently exposes one primary action (`Record governed note`), shows status banners, and renders metadata IDs/statuses after the status summary. Preserve this command path.
- `src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs` returns `OperationOutcome` with metadata-only audit-history lines. It must remain metadata-only; do not add payload, tenant/resource names, file names, raw source text, or exception text.
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`, `SharedResource.resx`, `SharedResource.fr.resx`, and `ChatBotUiTextLocalizer.cs` are the existing localization extension points from Story 1.20.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`, `ChatBotGovernedPrimitiveContractTests.cs`, `ChatBotResponsiveTouchContractTests.cs`, `ChatBotLocalizationContractTests.cs`, and `ChatBotLiveRegionReducedMotionContractTests.cs` are the closest static contract homes.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` already has browser and deterministic static fallback paths. Extend that style rather than adding a new E2E harness.

Preserve existing behavior:

- `GovernedOperations.razor` must still dispatch `SubmitGovernedNoteAction` through the existing primary governed action.
- `GovernedOperationService` must still use `ChatBotSurfaceOrigin.Ui` and only call `IChatBotClient`.
- `OperationOutcome` machine values remain visible and untranslated: operation ID, command ID, correlation ID, lifecycle state, completion status, audit status, safe next-action codes, and audit metadata tokens.
- Existing `ChatBotStatusBanner` live-region matrix, announcement deduplication, blocked-state matrix consumption, retryable failure politeness, and reduced-motion behavior must remain green.
- Existing Story 1.17 responsive/touch behavior must not regress: no horizontal overflow, no content masking, 44x44 primary touch targets, 24x24 dense-secondary targets, visible safe metadata, and no viewport zoom lock.
- Existing Story 1.18 accessibility/focus behavior must not regress: skip-link/main focus path, unique region names, disabled reason reachability, busy-region focus preservation, validation summary focus, and field message association.
- Existing Story 1.20 localization behavior must not regress: English/French resources, phrase-level accessible labels, stable machine identifiers, and French expansion behavior remain intact.
- UI must not reference `.Server`, gateway internals, DAPR clients, audit writer, idempotency store, projection store, mailbox, AI provider, CLI/MCP internals, Workers, or direct data-plane infrastructure.
- Do not add inline package versions or upgrade Fluent UI, Fluxor, FrontComposer, Playwright, xUnit, bUnit, or .NET.

### Previous Story Intelligence

Story 1.20 completed at baseline `8a1957d` and added UI-owned English/French localization registration, shared `.resx` resources, typed resource keys, phrase-level localizer methods, culture-aware formatting, and localization tests. Important implementation learnings for Story 1.21:

- Put new governed UI copy behind stable English/French resource keys, not inline strings scattered across components.
- Use phrase-level resource templates for accessible names/descriptions and redaction notices; do not concatenate translated fragments into safety-critical AT text.
- Keep stable machine identifiers byte-stable under French culture: IDs, status codes, reason codes, command names, lifecycle enum values, surface origin values, correlation IDs, data attributes, and audit metadata tokens.
- Do not change package pins, generated client code, backend command/audit contracts, or UI dependency boundaries.
- Final Story 1.20 validation passed build, UI tests, UI E2E tests, and `git diff --check`; architecture tests were not run because package/dependency/project-reference files did not change.

[Source: _bmad-output/implementation-artifacts/1-20-english-french-localization-infrastructure.md#Completion-Notes-List; 8a1957d]

Story 1.19 completed the state-to-feedback matrix, live-region politeness/dedup rules, and reduced-motion contract. Use those contracts for redaction/recovery status messages instead of adding another announcement path. [Source: _bmad-output/implementation-artifacts/1-19-live-region-and-reduced-motion-behavior.md#Completion-Notes-List]

Story 1.18 established keyboard/focus contracts, including disabled reasons and validation summary focus. Recovery pattern work should extend those contracts, not duplicate or weaken them. [Source: _bmad-output/implementation-artifacts/1-18-accessibility-and-focus-management-floor.md#Completion-Notes-List]

Story 1.17 established responsive/touch and dense-row collapse behavior. Cognitive-load work should use those rules and avoid clipping/truncation as a way to pass overflow tests. [Source: _bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md#Completion-Notes-List]

Recent git context:

- `8a1957d feat(story-1.20): English French localization infrastructure`
- `f55ccb3 feat(story-1.19): Live region and reduced motion behavior`
- `6c16298 feat(story-1.18): Accessibility and focus management floor`
- `ab529e2 feat(story-1.17): Responsive and touch foundation`
- `86f9dd6 feat(story-1.16): Interaction guardrails and streaming stop/cancel behavior`

Current dirty worktree entry observed during story creation: `_bmad-output/story-automator/orchestration-1-20260531-085840.md`. It is unrelated automation output and must not be reverted.

### Architecture and UX Guardrails

- UI stack is Blazor + Fluent UI v5 RC via FrontComposer, Fluxor state, REST commands/queries, and SignalR projection nudge. Do not add a second component library, CSS framework, JavaScript localization framework, native mobile layer, or MVC-localization dependency. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- Preserve dependency direction: UI depends only on Client and shared service defaults, not Server/gateway internals. [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries; src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj]
- Redaction is a swappable policy stage applied consistently across UI/CLI/MCP/export. This story should not build the backend policy stage; it must ensure the UI cannot route off-surface text around whatever redacted display payload it receives. [Source: _bmad-output/planning-artifacts/architecture.md#Authentication-and-Security; _bmad-output/planning-artifacts/architecture.md#Redaction-and-data-governance]
- FrontComposer customization requires preserving labels, keyboard reachability, focus visibility, live-region parity, reduced-motion, and forced-colors behavior. [Source: Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific-Rules]
- Stable identifiers and wire values must use ordinal/invariant handling. Sibling context guidance explicitly uses `StringComparison.Ordinal`, `StringComparer.Ordinal`, and `CultureInfo.InvariantCulture` for identifiers, ordering, hashing, and wire-stable formatting. [Source: Hexalith.Folders/_bmad-output/project-context.md#Critical-Implementation-Rules; src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs]
- Package versions are centrally managed in `Directory.Packages.props`; `.csproj` package references must not gain inline versions. [Source: Directory.Packages.props; Hexalith.FrontComposer/_bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Root-level submodule policy remains in force. Do not initialize or update nested submodules; this story should not need submodule commands. [Source: AGENTS.md; Hexalith.FrontComposer/_bmad-output/project-context.md#Development-Workflow-Rules]

### Latest Technical Notes

Web-verified on 2026-05-31: MDN documents the Clipboard API as the preferred async clipboard path over deprecated `document.execCommand`, but clipboard access is restricted to secure contexts and browser/user-activation/permission behavior differs across engines. If this story adds a fixture copy action, keep it user-initiated, graceful on failure, and deterministic in non-browser test fallback. [Source: https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API]

Web-verified on 2026-05-31: W3C WCAG 2.2 target-size guidance requires pointer targets to meet a minimum size or have sufficient spacing. Keep off-surface buttons and copy/share controls on the existing `chatbot-touch-target-primary`/dense control sizing path. [Source: https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum]

Web-verified on 2026-05-31: W3C error-identification guidance requires detected input errors to be identified and described in text. Recovery contracts should keep validation/recovery causes textual and focusable, not color-only or transient. [Source: https://www.w3.org/WAI/WCAG22/Understanding/error-identification]

Web-verified on 2026-05-31: W3C ARIA status technique notes `role="status"` provides polite status updates and recommends explicit `aria-atomic="true"` when the whole message should be announced. Reuse existing `ChatBotStatusBanner`/state-feedback behavior for redaction and recovery status. [Source: https://www.w3.org/WAI/WCAG21/Techniques/aria/ARIA22]

The repo currently pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, `Fluxor` to `6.9.0`, `Microsoft.Playwright` to `1.60.0`, `xunit.v3` to `3.2.2`, `Shouldly` to `4.3.0`, and `bunit` to `2.7.2`. Treat root package pins as authoritative; do not upgrade packages in this story. [Source: Directory.Packages.props]

### Suggested Implementation Shape

Prefer a narrow UI-owned addition:

```text
src/Hexalith.ChatBot.UI/
  Design/
    ChatBotOffSurfaceAffordanceContract.cs
    ChatBotOffSurfaceAffordanceKind.cs
    ChatBotOffSurfaceRedactionState.cs
    ChatBotRecoveryPatternContract.cs
    ChatBotRecoveryFlow.cs
    ChatBotCognitiveLoadContract.cs
    ChatBotWorkflowItemActionContract.cs
  Localization/
    ChatBotUiTextKey.cs
    ChatBotUiTextLocalizer.cs
    SharedResource.resx
    SharedResource.fr.resx
  Components/Governed/
    ChatBotEvidenceChip.razor
    ChatBotBlockedState.razor
    ChatBotStatusBanner.razor
    ChatBotGovernedAction.razor
  Components/Pages/
    GovernedOperations.razor
tests/
  Hexalith.ChatBot.UI.Tests/
    ChatBotOffSurfaceRedactionContractTests.cs
    ChatBotRecoveryPatternContractTests.cs
    ChatBotCognitiveLoadContractTests.cs
  Hexalith.ChatBot.UI.E2E.Tests/
    GovernedOperationsVisualFoundationE2ETests.cs
```

This shape is a suggestion, not a mandate. Keep one primary public type per file, file-scoped namespaces, and surrounding style. If a smaller shape fits the code better, keep it smaller.

### Testing Requirements

- Use xUnit v3 `3.2.2`, Shouldly `4.3.0`, Playwright `1.60.0`, and existing project patterns. No new assertion, browser, accessibility, component, localization, or clipboard test library.
- Prefer deterministic static tests for contract completeness and resource coverage; use Playwright/static fixture tests for rendered redaction notices, accessibility metadata, action ordering, overflow, and behavior preservation.
- Browser tests should select by role/name or explicit fixture metadata; CSS selectors are acceptable only when asserting data hooks, layout/overflow, active element, language/culture, or stable accessibility metadata.
- Test both English and French redaction/recovery/cognitive-load messages through the actual `ChatBotUiTextLocalizer` path.
- Test machine identifiers stay unchanged under French culture: operation ID, command ID, task ID, correlation ID, lifecycle state, completion status, audit status, safe-next-action code, status/reason code, command name, `data-chatbot-*` values, and enum wire values.
- Test off-surface redaction with a negative-control restricted phrase. A redacted visual state must fail if the restricted phrase appears in exported/copied/read-aloud text, accessible name, accessible description, status message, or deterministic fixture output.
- Test current metadata-only audit history lines continue to exclude command payloads, tenant/resource names, file names, secrets, raw exception text, and unrestricted audit details.
- Test recovery-flow contracts for association review, AI action review, queue retry, correction, and tenant configuration.
- Test cognitive-load constraints: one primary action, action grouping order, evidence/risk/status/actor/timestamp order, summary before IDs, active-filter summary/result count, consolidated banner/panel, and labelled-row small-screen fallback.
- Keep Story 1.17 responsive/touch tests, Story 1.18 accessibility/focus tests, Story 1.19 live-region/reduced-motion tests, and Story 1.20 localization tests green.
- Keep architecture boundary tests green if dependencies/project references change.

### Out of Scope

- Building S1 project conversation, S2 association review, S3 AI approval, operational queue rows, audit investigation timeline, tenant configuration editor, command palette, tenant export workflow, or production transcript download behavior.
- Building Story 9.8 data-class tenant export, backend retention/export/deletion workflows, WORM audit query/export logic, backend redaction-policy engine changes, or generated OpenAPI/client changes.
- Translating backend audit envelopes, OpenAPI, generated client code, command/rejection/event contracts, enum wire values, command names, reason/status codes, identifiers, correlation IDs, or log/audit metadata.
- CLI/MCP off-surface behavior, M365 integration, AI provider behavior, DAPR/EventStore changes, Server/Gateway changes, Workers, sibling submodule changes, browser storage persistence, or production data behavior.
- Adding or upgrading packages, adding a JavaScript localization framework, adding a second UI/component framework, or introducing MVC-only localizer APIs.

### Project Structure Notes

- Off-surface/recovery/cognitive-load contracts: `src/Hexalith.ChatBot.UI/Design/`.
- Existing shared label and resource helpers: `src/Hexalith.ChatBot.UI/Design/ChatBotGovernedUiText.cs`, `src/Hexalith.ChatBot.UI/Localization/`.
- Current fixture: `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`.
- Governed primitives: `src/Hexalith.ChatBot.UI/Components/Governed/`.
- Existing UI state/service seam: `src/Hexalith.ChatBot.UI/State/GovernedOperations/` and `src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs`.
- CSS/touch/responsive behavior: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Focused UI tests: `tests/Hexalith.ChatBot.UI.Tests/`.
- E2E/static fixture tests: `tests/Hexalith.ChatBot.UI.E2E.Tests/`.
- Boundary tests if dependencies change: `tests/Hexalith.ChatBot.Architecture.Tests/`.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create-Story-Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.21-Redaction-safe-off-surface-affordances-and-recovery-patterns]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Error-recovery-patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Cognitive-load-guardrails]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/review-accessibility.md#Findings]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Accessibility-and-Usability-Quality]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries]
- [Source: _bmad-output/implementation-artifacts/1-17-responsive-and-touch-foundation.md]
- [Source: _bmad-output/implementation-artifacts/1-18-accessibility-and-focus-management-floor.md]
- [Source: _bmad-output/implementation-artifacts/1-19-live-region-and-reduced-motion-behavior.md]
- [Source: _bmad-output/implementation-artifacts/1-20-english-french-localization-infrastructure.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: Hexalith.Folders/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotAccessibilityFloorContract.cs]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotDenseRowCollapseContract.cs]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingContract.cs]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotSmallScreenFallbackContract.cs]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotValidationErrorContract.cs]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]
- [Source: src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- [Source: Directory.Packages.props]
- [Source: https://developer.mozilla.org/en-US/docs/Web/API/Clipboard_API]
- [Source: https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum]
- [Source: https://www.w3.org/WAI/WCAG22/Understanding/error-identification]
- [Source: https://www.w3.org/WAI/WCAG21/Techniques/aria/ARIA22]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31T15:15+02:00 - Create-story workflow loaded skill, discover-inputs protocol, template, checklist, BMAD config, sprint status, planning artifacts, UX/PRD/architecture excerpts, previous story 1.20, current UI source/tests, project context facts, recent git history, and official MDN/W3C references.
- 2026-05-31T15:15+02:00 - Current dirty worktree entry before story creation: `_bmad-output/story-automator/orchestration-1-20260531-085840.md`; unrelated automation artifact, not reverted.
- 2026-05-31T15:15+02:00 - Checklist validation applied during story creation: tightened guardrails for off-surface redaction parity, screen-reader-equivalent redaction notices, recovery-flow completeness, cognitive-load ordering, localization, package pins, and UI-only scope.
- 2026-05-31T15:20:21+02:00 - Dev-story workflow marked story and sprint status `in-progress`; preserved existing `baseline_commit: 8a1957d`.
- 2026-05-31T15:23+02:00 - Red phase: added off-surface, recovery, cognitive-load unit/static tests; `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` failed at compile because `ChatBotOffSurfaceAffordanceContract` did not exist yet. The VSTest runner also cannot be used in this sandbox after compilation because it opens a local socket and gets `Permission denied`; validation uses the required xUnit v3 binaries.
- 2026-05-31T15:26+02:00 - Green/refactor: added UI design contracts, localized microcopy, evidence off-surface metadata hook, accessibility floor entry, queue active-filter summary property, and focused tests. `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests` passed 77/77.
- 2026-05-31T15:27+02:00 - E2E/static lane first run failed 1/18 due stale fallback assertion expecting literal `Response stopped` in component source after Story 1.20 localization; updated assertion to check `ChatBotUiTextKey.StopResponseAnnouncement` while fixture still renders the visible text.
- 2026-05-31T15:28+02:00 - Final validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed 0 warnings/0 errors; UI xUnit binary passed 77/77; UI E2E xUnit binary passed 18/18; `git diff --check` passed. Architecture xUnit binary not run because no dependencies, project references, package files, or boundary assertions changed.
- 2026-05-31T15:43+02:00 - Senior developer review auto-fixed confirmed gaps: off-surface artifacts now must include the redacted visual payload and redaction notice in both off-surface text and accessible description; evidence chips only advertise safe/state-compatible off-surface contracts; recovery contracts now encode correction success/partial/blocked status, AI risky-action confirmation coverage, queue row-status focus, and typed duplicate-safe retry localization. UI xUnit binary and UI E2E xUnit binary passed after fixes.
- 2026-05-31T15:44+02:00 - Post-review validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed 0 warnings/0 errors; `git diff --check` passed.
- 2026-06-10T06:24+02:00 - Dev-story workflow re-run found no unchecked tasks or review follow-ups. Fresh validation passed: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed 0 warnings/0 errors; `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -noColor` passed 129/129; `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor` passed 64/64; `git diff --check` passed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Discovery loaded sprint status, Epic 1 story context, architecture frontend/project-structure/redaction sections, UX accessibility/recovery/cognitive-load docs, PRD NFR60-NFR64 and NFR40 references, previous story 1.20, current UI components/design contracts/tests, package pins, recent git history, FrontComposer/Folders project context facts, and web-verified MDN/W3C references.
- Checklist validation applied: story explicitly prevents raw hidden-source export/copy/read-aloud leakage, accessible-name redaction leaks, wrong file locations, package upgrades, backend export scope creep, duplicate announcement systems, cognitive-load drift, and regressions to responsive/accessibility/live-region/localization foundations.
- Implemented UI-owned off-surface affordance contracts for export, copy, transcript download, read-aloud, copy/share handoff, audit copy, and evidence copy. Contracts require nonblank off-surface text, safe accessible name/description, disabled reason, escalation guidance, and redaction notice for redacted/unauthorized states, and fail when known restricted source markers appear.
- Senior review tightened off-surface affordance safety so copied/exported/read-aloud text must carry the visual redacted payload and accessible descriptions must include the redaction notice; unsafe or state-incompatible evidence affordance metadata is no longer advertised by the primitive.
- Added typed UX-DR40 recovery contracts for association review, AI action review, queue retry, correction, and tenant configuration, including still-valid actions, audit-visible outcomes, duplicate-safe retry copy/count, row-status focus return, affected-context preview, correction success/partial/blocked status, validation summary placement, field-message association, and constrained save-conflict causes.
- Added UX-DR41 cognitive-load contracts for exactly one primary action, primary/secondary/destructive action grouping, canonical evidence/risk/status/actor/timestamp ordering, summary-before-ID behavior, active-filter summary plus result count, and consolidated state message.
- Extended English/French resources and `ChatBotUiTextLocalizer` with phrase-level redaction, escalation, unavailable off-surface, duplicate-safe retry, active-filter, and safe-next-action recovery messages. Stable machine identifiers remain untranslated.
- Wired `ChatBotEvidenceChip` with optional off-surface affordance metadata while preserving redacted/unauthorized non-openable behavior. No product export, clipboard action, browser API, backend redaction engine, package, generated-client, or dependency changes were added.
- Extended static/unit and E2E/static fixture tests for non-leakage, English/French redaction/recovery copy, stable IDs/codes, metadata-only audit strings, recovery completeness, cognitive-load ordering, one primary action, active-filter result counts, and phone-width overflow. Browser clipboard fallback is not applicable because no clipboard behavior was added.
- 2026-06-10 dev-story re-run confirmed all task and review checkboxes already complete; no implementation deltas were required. Current validation remains green with UI tests at 129/129 and UI E2E tests at 64/64.

### File List

- `_bmad-output/implementation-artifacts/1-21-redaction-safe-off-surface-affordances-and-recovery-patterns.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor`
- `src/Hexalith.ChatBot.UI/Design/ChatBotAccessibilityFloorContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotCognitiveLoadContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotCorrectionRecoveryStatus.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotOffSurfaceAffordanceContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotOffSurfaceAffordanceKind.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotOffSurfaceRedactionState.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotRecoveryFlow.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotRecoveryPatternContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotSaveConflictCause.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotWorkflowItemActionContract.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotWorkflowItemActionKind.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotCognitiveLoadContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotOffSurfaceRedactionContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotRecoveryPatternContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31

Outcome: Approved after auto-fixes. No critical issues remain.

Findings fixed:

- HIGH - Off-surface artifact parity was incomplete: `ChatBotOffSurfaceAffordanceContract` recorded visual and off-surface text but did not require the off-surface artifact to include the redacted visual payload, so a future copy/export/read-aloud caller could pass different text while still satisfying the contract. Fixed by requiring `UsesVisualPayloadOffSurface` for completeness and adding regression coverage.
- HIGH - Redaction notices were accepted when present in either off-surface text or accessible description, but the AC requires both artifact/read-aloud safety and screen-reader-equivalent messaging. Fixed by requiring the notice in both `OffSurfaceText` and `AccessibleDescription`.
- HIGH - UX-DR40 correction recovery did not mechanically encode success, partial, or blocked correction status. Fixed with `ChatBotCorrectionRecoveryStatus` and completeness validation that rejects raw failure status.
- MEDIUM - AI action review confirmation copy was only nonblank; it did not prove coverage for external, file, project, tool, and participant risky-action classes. Fixed with explicit confirmation-term validation and tests.
- MEDIUM - Queue retry recovery did not prove focus return to row status. Fixed by requiring a row-status focus target and adding regression coverage.
- MEDIUM - `ChatBotEvidenceChip` accepted any off-surface contract metadata, including unsafe or state-incompatible contracts. Fixed by only advertising safe, state-compatible affordances.
- LOW - Duplicate-safe retry copy existed as a resource key but had no typed localizer method. Fixed with `RecoveryDuplicateSafeRetry()`.

Validation after fixes:

- `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -noColor` - passed 77/77.
- `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor` - passed 18/18.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `git diff --check` - passed.

---

Reviewer: Jerome on 2026-06-10 (story-automator adversarial re-review)

Outcome: Approved after one auto-fix. No critical issues remain; status stays done.

Findings fixed:

- MEDIUM (test quality, AC4/AC7) - `ChatBotCognitiveLoadContractTests.CurrentFixtureShouldPutPlainLanguageBeforeMachineIdentifiers` asserted `page.IndexOf("OperationStatus_Summary")` and `page.IndexOf("AuditHistory_MetadataOnly")`, but the razor resource-key tokens are `OperationStatusSummary` and `AuditHistoryMetadataOnly` (no underscore). `IndexOf` returned -1 for the mismatched tokens, so `(-1).ShouldBeLessThan(realIndex)` passed vacuously and the "summary before raw IDs" cognitive-load ordering guard did not actually verify ordering. Fixed by matching the real tokens and adding `ShouldBeGreaterThanOrEqualTo(0)` existence guards so the assertion fails if a token disappears or the order drifts. The live `GovernedOperations.razor` ordering was already correct (`OperationStatusSummary` precedes `OperationLabel`; `AuditHistoryMetadataOnly` precedes `AuditHistoryTitle`), so the corrected test passes meaningfully.

Validation after re-review fix:

- `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -noColor` - passed 129/129.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor` - passed 64/64.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `git diff --check` - passed.

Verified clean (no change required): ACs 1-7 implemented; off-surface contract requires the visual payload plus redaction notice in both off-surface text and accessible description; recovery contracts encode all five UX-DR40 flows with non-vacuous negative controls; `GovernedOperationService.ToAuditHistoryLines` stays metadata-only; English/French phrase-level keys resolve through `ChatBotUiTextLocalizer`; File List matches git reality.

### Change Log

- 2026-05-31T15:29:25+02:00 - Implemented Story 1.21 redaction-safe off-surface, recovery-pattern, and cognitive-load UI foundation; marked story ready for review after passing required validation.
- 2026-05-31T15:43:00+02:00 - Senior developer review auto-fixed off-surface redaction parity, recovery-pattern completeness, evidence affordance safety, and duplicate-safe retry localization; marked story done.
- 2026-06-10 - Story-automator adversarial re-review auto-fixed a vacuous cognitive-load ordering assertion (underscore token mismatch made `IndexOf` return -1 and pass trivially); corrected tokens with existence guards. Build 0/0, UI tests 129/129, UI E2E tests 64/64, `git diff --check` passed. Status remains done (0 critical issues).
