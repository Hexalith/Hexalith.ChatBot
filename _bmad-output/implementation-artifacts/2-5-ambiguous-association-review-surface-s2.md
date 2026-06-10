---
baseline_commit: ddbb192bc75244847fb09a135791602f45893bdd
---

# Story 2.5: Ambiguous association review surface (S2)

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As an authorized reviewer,
I want a candidate-review surface with evidence, confidence, and clear decisions,
so that I can resolve ambiguity from captured evidence without re-reading the full thread.

## Acceptance Criteria

1. **Candidate evidence is reviewable and comparable.** Given an ambiguous `NeedsReview` association item, when I open Association Review (S2), then I see ranked authorized candidate rows with evidence chips, confidence band, reason codes, disabled/next-action reason codes, source metadata, and the consequence of each available decision; I can compare candidate evidence side by side without reading the full source thread. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.5-Ambiguous-association-review-surface-S2; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Project-Email-Intake-and-Association; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]

2. **Review actions expose finite states and safe consequences.** Given a candidate item, when the review surface renders actions, then I can select a candidate, enter an optional decision note locally, and see choose-candidate, reject-all, defer, and mark-needs-review controls with explicit consequences; each durable action renders as `enabled`, `disabled-with-reason`, or `not-applicable-hidden` using a finite reason set, and disabled controls remain keyboard discoverable through `aria-disabled` plus an adjacent reachable reason. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.5-Ambiguous-association-review-surface-S2; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR76-FR80; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]

3. **Long-running, blocked, stale, and terminal states are explicit.** Given candidate loading, no authorized candidates, ambiguous candidates, candidate selected, validation error, retryable intake failure, dependency degraded, quarantined, terminal failure, stale evidence, waiting, blocked, or escalation-needed states, when S2 renders, then the surface shows the state, safe next action, retry/escalation guidance where valid, and never displays a premature "done" state before projection/audit status is complete. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Recovery-and-Continuity; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Accessibility-and-Usability-Quality; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]

4. **Unauthorized candidates stay suppressed end to end.** Given hidden or unauthorized project candidates/evidence, when the list, evidence drawer, blocked state, errors, validation summaries, exports, or accessibility names render, then unauthorized project names, existence hints, evidence, raw email text, raw provider data, raw addresses, secrets, and exception text are absent; the user sees only redacted/suppressed metadata and safe next-action copy. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.5-Ambiguous-association-review-surface-S2; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Security-and-Privacy; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Voice-and-Tone]

5. **S2 meets the M0 accessibility and responsive floor.** Given desktop, tablet, and phone viewports plus forced-colors and reduced-motion preferences, when Association Review is tested, then the screen conforms to WCAG 2.2 AA scope for M0: keyboard-only operation, visible focus, unique landmarks, no hover-only critical actions, non-color status cues, live-region behavior only for new user-relevant changes, no horizontal overflow, and target sizes matching the established ChatBot UI floor. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60-NFR64; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]

## Tasks / Subtasks

- [x] Add an S2 read seam over existing association routing status (AC: 1, 3, 4)
  - [x] Extend `IChatBotClient`/`ChatBotClient` only if the generated client already exposes a routing-status endpoint; otherwise add the minimal generated-client wrapper method after updating the OpenAPI spine.
  - [x] Prefer a UI-owned `AssociationReviewService` that reads `AssociationRoutingStatus` through `IChatBotClient`; do not call Server projections, DAPR, stores, or EventStore internals from UI.
  - [x] Map generated `AssociationRoutingStatus` to UI view models with metadata-only fields: ids, lifecycle, outcome, threshold band, confidence, reason codes, authorized candidates, evidence refs, disabled-action reason codes, next-action reason codes, freshness/source/schema/correlation fields.
  - [x] Treat absent/no-authorized-candidate data as a visible review state, not as an exception path that leaks hidden candidates.

- [x] Implement S2 Fluxor state and effects (AC: 1, 3, 4)
  - [x] Add a focused Association Review state slice rather than overloading the trivial `GovernedOperations` note-submission state.
  - [x] Model loading, loaded, selected candidate, validation error, submit pending, submit accepted/projection-pending, submit failed, blocked, unauthorized/redacted, stale/degraded, terminal, and empty/no-authorized-candidates states.
  - [x] Effects must collapse unknown/transport failures to stable safe codes, rethrow cancellation, and preserve server-provided catalog/problem codes without raw exception text.
  - [x] SignalR nudges, if wired, must only trigger re-query; never trust nudge payloads as S2 display data.

- [x] Build Association Review UI components from existing governed primitives (AC: 1-5)
  - [x] Add components under `src/Hexalith.ChatBot.UI/Components/Governed/` or a narrow `AssociationReview/` subfolder if the surface needs multiple files; keep component naming `ChatBotAssociation...`.
  - [x] Reuse `ChatBotConversationShell`, `ChatBotProjectContextHeader`, `ChatBotEvidenceChip`, `ChatBotGovernedAction`, `ChatBotStatusBanner`, `ChatBotBlockedState`, actor/status design contracts, and `chatbot.tokens.css`.
  - [x] Candidate rows must be buttons/radio/selection controls with stable accessible names and selected state; evidence chips must remain text-first and keyboard-operable.
  - [x] Provide a side-by-side evidence comparison panel/drawer for the selected candidate(s) using authorized metadata only; redacted evidence remains understandable without exposing hidden detail.
  - [x] Show confidence band and reason codes as text labels, not color alone. Use current `AssociationThresholdBand`, `AssociationScoringOutcome`, `LifecycleState`, `AssociationReasonCode`, and evidence refs; do not invent parallel enums.
  - [x] Add explicit consequence copy per action, for example association will attach to one project, reject-all keeps the item unresolved/terminal per backend state, defer keeps it visible, mark-needs-review keeps it in review.

- [x] Render review actions safely without owning Story 2.6 backend recording (AC: 2, 3)
  - [x] Candidate selection and optional decision-note entry must work locally on the S2 surface so reviewers can inspect consequences and validation before submission.
  - [x] If Story 2.6 decision commands are not present yet, render confirm/reject/defer/mark-needs-review/decision-note affordances as disabled-with-reason or local validation previews, with finite reason codes such as `decision-command-not-available`, `candidate-required`, `evidence-expired`, `not-authorized`, `terminal-state`, `projection-pending`.
  - [x] If minimal decision commands already exist by implementation time, submit them through `IChatBotClient.SubmitAsync(..., origin: ChatBotSurfaceOrigin.Ui)` and read status/audit back through the client before reporting completion.
  - [x] Do not create durable decision-recording commands/events in this UI story unless required to make existing S2 action controls function; Story 2.6 owns association decision recording, evidence preservation breadth, and notes.

- [x] Add localized, user-safe S2 text (AC: 1-5)
  - [x] Add stable keys to `ChatBotUiTextKey`, `SharedResource.resx`, and `SharedResource.fr.resx` for Association Review title, state labels, action labels, action consequences, disabled reasons, evidence comparison labels, safe next actions, loading/empty/blocked states, validation summaries, and screen-reader labels.
  - [x] English and French resources must remain complete and must not contain restricted sample project names, raw email snippets, paths, secrets, or raw exception wording.
  - [x] Keep microcopy factual: "This message needs project review", "3 candidate projects. Confidence is close", "Evidence restricted", not marketing or chatbot-like copy.

- [x] Extend CSS and responsive behavior within the existing token system (AC: 1, 5)
  - [x] Add S2 row/list/panel classes to `wwwroot/css/chatbot.tokens.css` using only existing `--chatbot-*`, Fluent, and FrontComposer custom properties; do not add raw hex/rgb/hsl colors.
  - [x] Candidate lists must keep stable dimensions and avoid layout shift when evidence/status/action labels change.
  - [x] Phone/tablet layouts must collapse comparison details below or into the complementary panel without losing source metadata, selected candidate, disabled reasons, or safe next action.
  - [x] Forced-colors must preserve text/icon/border cues; reduced motion must suppress row reordering, skeleton shimmer, and panel transitions while preserving text progress status.

- [x] Add tests and verification (AC: 1-5)
  - [x] Add UI service/effect tests for routing-status reads, selected-candidate state, safe failure codes, cancellation rethrow, and redaction/no raw data in errors.
  - [x] Add contract tests proving S2 components use shared primitives, finite action states/reason codes, `aria-disabled` disabled reasons, unique landmarks, keyboard paths, and no hover-only critical controls.
  - [x] Add localization tests proving all S2 keys exist in English and French.
  - [x] Add E2E/Playwright coverage or static fallback tests matching existing `tests/Hexalith.ChatBot.UI.E2E.Tests` style for desktop/tablet/phone, forced-colors, reduced-motion, no horizontal overflow, candidate selection, evidence comparison, and blocked/redacted states.
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run the relevant UI, client/contracts if touched, and E2E tests. If `dotnet test` hits sandbox socket limits, run the compiled xUnit v3 test executables and record the limitation.
  - [x] Run `git diff --check`.

## Dev Notes

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 2 and Story 2.5 are the primary story source.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` plus `addendum.md`; relevant sections cover Journey 2, FR3-FR12, FR76-FR80, NFR60-NFR64, lifecycle states, fail-closed safety, and UI scope.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; S2 lives in the Blazor/FrontComposer UI over the Contract Spine, with all writes through `IChatBotClient` and CommandGateway.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`; Association Review states, component behavior, accessibility floor, semantic tokens, and voice/tone constrain this story.
- Loaded persistent project-context facts from sibling `Hexalith.*` `_bmad-output/project-context.md` files; recurring constraints are .NET 10, central package management, EventStore purity, DAPR/Aspire boundaries, tenant isolation, personal-data redaction, root-level submodules only, xUnit/Shouldly tests, and no generated-file hand edits.

### Source Artifact Analysis

Epic 2 requires ambiguous or low-confidence email-to-project association to stop at human review. Story 2.4 already routes `[T_low, T_high)`, low-confidence, conflicting evidence, scorer errors, and non-finite scores to explicit `NeedsReview` records with metadata preserved and no auto-attachment. Story 2.5 is the first S2 surface that consumes that state. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4-Ambiguous-association-detection-and-fail-closed-routing; _bmad-output/planning-artifacts/epics.md#Story-2.5-Ambiguous-association-review-surface-S2]

The user journey is trust-centered: the reviewer should resolve ambiguity from ranked evidence without manually re-reading mailbox history. Candidate evidence includes party match, thread references, project alias/id, subject/body signals, attachment metadata, prior associations, conversation participants, prior corrections, and reason labels, but only when authorized. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Journey-2-Business-Contributor-Resolves-an-Ambiguous-Project-Association]

The canonical lifecycle is `Received -> Proposed -> Associated | Rejected | Deferred | NeedsReview | Failed | Skipped`; `NeedsReview` and `Deferred` remain active review states, while `Rejected`, `Failed`, and `Skipped` have terminal rules. S2 must display these states honestly and avoid inventing its own lifecycle vocabulary. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Journey-3-External-Party-Sends-Project-Context-Into-Hexalith]

M0 UI scope explicitly includes S2 and the WCAG 2.2 AA floor: keyboard operation, focus order, non-color indicators, live-region behavior, reduced motion, redacted/unauthorized screen-reader-safe states, and no unsafe off-surface/export leakage. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60-NFR64; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]

### Previous Story Intelligence

Story 2.4 is complete and should be extended, not replaced. It added explicit `LifecycleState` to association routing events/projections, a metadata-only `AssociationRoutingStatus` query contract/OpenAPI schema, generated client updates, safe message catalog codes, and tests proving ambiguous/fail-closed outcomes become `NeedsReview`. [Source: _bmad-output/implementation-artifacts/2-4-ambiguous-association-detection-and-fail-closed-routing.md]

Actionable 2.4 patterns to preserve:

- Use `AssociationRoutingStatus` as the S2 read model. It already carries association/source ids, lifecycle, outcome, threshold band, confidence score, reason codes, candidates, exclusions, evidence refs, kernel/version/provenance/redaction/retention/correlation, disabled action reasons, and next action reasons.
- Preserve enum wire-token discipline added in review. Do not serialize UI-visible or generated-client data using ad hoc enum names that drift from `EnumMember`/OpenAPI tokens.
- Treat candidate-bearing low-confidence fail-closed results differently from scorer-error fail-closed results: low-confidence can preserve authorized candidates for review; scorer-error/non-finite/conflicting-required-evidence keeps candidates empty and uses safe reason codes.
- Do not add another scorer, association aggregate, projection store, or lifecycle state model. S2 consumes projection/query state.
- Recent git history confirms the dependency chain: `ddbb192 feat(story-2.4): Ambiguous-association detection and fail-closed routing`, `48b72c0 feat(story-2.3): Deterministic association scorer and candidate generation`, `c04bcfd feat(story-2.2): Participant resolution and unresolved/unauthorized handling`, `dee5423 feat(story-2.1): Microsoft 365 mailbox intake and source-identity capture`.

### Current Implementation State

UI currently has a governed operations page and primitives from Epic 1: `ChatBotConversationShell`, `ChatBotProjectContextHeader`, `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotBlockedState`, `ChatBotStatusBanner`, `ChatBotGovernedAction`, localization keys/resources, Fluxor `GovernedOperations` state, and `GovernedOperationService`. These prove the UI-origin command pattern, metadata-only audit/status rendering, accessibility/focus contracts, responsive/touch contracts, live-region/reduced-motion contracts, localization, and off-surface redaction. [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor; src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs; tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs]

`IChatBotClient` currently exposes command submission, operation status, and operation audit history. It does not yet expose a typed association routing-status read helper even though the generated client contains `AssociationRoutingStatus`. S2 likely needs either an additive `IChatBotClient` wrapper over the generated endpoint or a minimal OpenAPI/client update if the endpoint is not generated. Keep this in `Hexalith.ChatBot.Client`; UI must not import Server projection types. [Source: src/Hexalith.ChatBot.Client/IChatBotClient.cs; src/Hexalith.ChatBot.Client/ChatBotClient.cs; src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs]

`AssociationCandidateView` is the server projection shape and `AssociationProjectionHandler` is idempotent/order-tolerant by `SourceVersion`. This is useful context only; S2 should read through the contract/client surface, not directly from `AssociationProjectionHandler` or `IAssociationProjectionStore`. [Source: src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs; src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs]

The CSS token file already defines semantic colors, status/badge/chip/action/blocked-state classes, responsive shell behavior, reduced-motion suppression, and forced-colors handling. S2 styles should extend these classes/tokens; no raw colors or one-off UI system. [Source: src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Colors]

### Architecture Guardrails

- Runtime stack: .NET SDK `10.0.300`, `net10.0`, central package management, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, Playwright `1.60.0`, xUnit v3 `3.2.2`, bUnit `2.7.2`, Shouldly. Do not add inline package versions or upgrade packages for this story. [Source: Directory.Packages.props; tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs#PackagePinsShouldRemainUnchangedForAccessibilityFloor]
- UI/CLI/MCP connect through `IChatBotClient` and the Contract Spine. No direct DAPR, EventStore, state-store, projection-store, mailbox, Projects, Parties, or Folders calls from UI. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- Every external write enters through CommandGateway with `ChatBotSurfaceOrigin.Ui`; first-party UI code declares origin at the client boundary so audit attribution remains correct. [Source: src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs]
- Problem/status text comes from message catalogs/localization, with stable codes. UI must never show raw exception text, raw payloads, raw project names for unauthorized resources, raw email content, local paths, secrets, or localized text as machine contract values. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- Accessibility is contractual. Disabled critical actions use `aria-disabled`, remain discoverable, suppress activation, and reference reachable reason text; historical content does not announce on initial load. [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs; tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- Submodules: initialize/update only root-level submodules declared in the repository root `.gitmodules`; never use recursive submodule commands. [Source: AGENTS.md#Git-Submodules]

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Client/
    IChatBotClient.cs                              # UPDATE if routing-status helper needed
    ChatBotClient.cs                               # UPDATE wrapper over generated endpoint only
  Hexalith.ChatBot.Contracts/
    openapi/hexalith.chatbot.v1.yaml              # UPDATE only if missing generated read endpoint
  Hexalith.ChatBot.Client/Generated/
    HexalithChatBotClient.g.cs                     # REGENERATE only, never hand-edit
  Hexalith.ChatBot.UI/
    Services/
      AssociationReviewService.cs                 # NEW
    State/AssociationReview/
      AssociationReviewActions.cs                 # NEW
      AssociationReviewEffects.cs                 # NEW
      AssociationReviewFeature.cs                 # NEW
      AssociationReviewReducers.cs                # NEW
      AssociationReviewState.cs                   # NEW
      AssociationReviewViewModels.cs              # NEW if helpful
    Components/Pages/
      AssociationReview.razor                     # NEW route/page for S2
    Components/Governed/
      ChatBotAssociationCandidateRow.razor        # NEW
      ChatBotAssociationEvidenceComparison.razor  # NEW
      ChatBotAssociationReviewActions.razor       # NEW if shared composition helps
    Localization/
      ChatBotUiTextKey.cs                         # UPDATE
      SharedResource.resx                         # UPDATE
      SharedResource.fr.resx                      # UPDATE
    wwwroot/css/chatbot.tokens.css                # UPDATE token-backed S2 classes
tests/
  Hexalith.ChatBot.UI.Tests/
  Hexalith.ChatBot.UI.E2E.Tests/
  Hexalith.ChatBot.Client.Tests/                  # if IChatBotClient wrapper changes
  Hexalith.ChatBot.Contracts.Tests/               # if OpenAPI/contract changes
```

Keep the change smaller if generated/client read support already exists. The required deliverable is a usable, accessible S2 review surface over existing routing status, not a backend association subsystem.

### Out of Scope

- Durable association decision recording, evidence-preservation breadth, human rationale persistence, and note events unless existing Story 2.6 contracts are already available; Story 2.6 owns these.
- Association correction/supersession and derived-store invalidation; Stories 2.7 and 2.8 own them.
- Duplicate detection, retry orchestration, terminal failure backend handling, dead-letter queues, and operational queue management beyond S2 display states; Story 2.9 and later ops stories own them.
- CLI/MCP parity surfaces; S2 must preserve machine-readable parity data but CLI/MCP adapters are M1.
- New UI framework, custom design system, package upgrades, generated-client hand edits, direct projection-store reads from UI, raw mailbox/project/party data display, or recursive submodule initialization.

### Latest Technical Notes

No external API version research is required for this story. Use the repository-pinned stack and existing Story 1 UI primitives plus Story 2.4 association routing contracts. Do not upgrade .NET, DAPR, Aspire, Fluent UI, Fluxor, Playwright, NSwag, xUnit, or bUnit to satisfy Story 2.5.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/implementation-artifacts/2-4-ambiguous-association-detection-and-fail-closed-routing.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-Project-Association]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.5-Ambiguous-association-review-surface-S2]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Journey-2-Business-Contributor-Resolves-an-Ambiguous-Project-Association]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Project-Email-Intake-and-Association]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60-NFR64]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements-Structure-Mapping]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Information-Architecture]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Colors]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: Hexalith.Projects/_bmad-output/project-context.md]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs]
- [Source: src/Hexalith.ChatBot.Client/IChatBotClient.cs]
- [Source: src/Hexalith.ChatBot.Client/ChatBotClient.cs]
- [Source: src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs]
- [Source: src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor]
- [Source: src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs]
- [Source: src/Hexalith.ChatBot.UI/State/GovernedOperations/GovernedOperationsEffects.cs]
- [Source: src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs]
- [Source: src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotRecoveryPatternContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs]
- [Source: Directory.Packages.props]
- [Source: global.json]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31: `dotnet test` for UI, client, and contracts test projects failed under sandboxed VSTest socket startup (`SocketException 13`); reran the compiled xUnit v3 test executables successfully as directed by the story.
- 2026-05-31: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed.
- 2026-05-31: Compiled xUnit v3 executables passed for UI (84), Client (14), Contracts (78), UI.E2E (18), Server (182), AppHost (3), Architecture (35), Aspire (2), Conformance (54), ServiceDefaults (3), Testing (37), Workers (15), and Integration (4 total, 2 skipped by Tier-3 infrastructure guard).
- 2026-05-31: `git diff --check` passed.
- 2026-05-31: Senior review reran `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`; passed.
- 2026-05-31: Senior review confirmed `dotnet test` still fails under sandboxed VSTest socket startup (`SocketException 13`) for UI, Client, and UI.E2E projects; reran compiled xUnit v3 executables instead.
- 2026-05-31: Senior review compiled xUnit v3 executables passed for UI (85), Client (14), and UI.E2E (21).
- 2026-05-31: Senior review reran `git diff --check`; passed.
- 2026-06-10: Dev-story rerun found no unchecked tasks or review follow-ups; story and sprint status were already `done`.
- 2026-06-10: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-10: `dotnet test` for the UI test project still cannot start VSTest under the sandbox (`SocketException 13`); used compiled xUnit v3 executables for validation.
- 2026-06-10: Compiled xUnit v3 executables passed for all ChatBot test projects: UI (129), Client (30), Contracts (480), UI.E2E (66), AppHost (5), Architecture (39), Aspire (2), CLI (22), Conformance (87), Integration (18 total, 2 skipped by infrastructure guards), MCP (25), Server (1519), ServiceDefaults (5), Testing (41), and Workers (30).
- 2026-06-10: `git diff --check` passed.
- 2026-06-10: Senior review (adversarial) reran `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`; passed with 0 warnings and 0 errors.
- 2026-06-10: Senior review compiled xUnit v3 executables passed for the touched suites: Client (34/34, including the previously undocumented `AssociationRoutingStatusTransportTests`), Server `AssociationProjectionTests` class (15/15, including the previously undocumented routing-status endpoint 200/401/403 cases), and UI (130/130, including the new redaction regression test).

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-05-31.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added the S2 association routing-status read endpoint to the OpenAPI spine, regenerated the client, and exposed the typed `IChatBotClient.GetAssociationRoutingStatusAsync` facade.
- Added `AssociationReviewService`, a focused Fluxor state/effects/reducers slice, and the `/association-review/{AssociationId}` Blazor surface over metadata-only routing status.
- Added governed S2 components for candidate rows, evidence comparison, and local decision-action preview. Story 2.6 durable decision commands are intentionally not created; actions render disabled-with-reason/local validation using finite reason codes.
- Added English/French S2 resources and token-backed responsive/forced-colors/reduced-motion CSS.
- Added UI service/effect/component contract tests, client wrapper coverage, and static E2E-compatible guard coverage. Relevant xUnit executable suites pass.
- 2026-06-10 rerun was validation-only: no open implementation tasks existed, no task checkboxes changed, and completed `done` status was preserved.

### File List

- _bmad-output/implementation-artifacts/2-5-ambiguous-association-review-surface-s2.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.Client/ChatBotClient.cs
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs
- src/Hexalith.ChatBot.Client/IChatBotClient.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Server/Program.cs
- src/Hexalith.ChatBot.UI/Components/_Imports.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor
- src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor
- src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs
- src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx
- src/Hexalith.ChatBot.UI/Localization/SharedResource.resx
- src/Hexalith.ChatBot.UI/Program.cs
- src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs
- src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewActions.cs
- src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs
- src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewFeature.cs
- src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewModels.cs
- src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs
- src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewState.cs
- src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css
- tests/Hexalith.ChatBot.Client.Tests/AssociationRoutingStatusTransportTests.cs
- tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs
- tests/Hexalith.ChatBot.Conformance.Tests/M365MailboxEventActorIsolationTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Projections/AssociationProjectionTests.cs
- tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs
- tests/Hexalith.ChatBot.UI.Tests/AssociationReviewEffectsTests.cs
- tests/Hexalith.ChatBot.UI.Tests/AssociationReviewServiceTests.cs
- tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs
- tests/Hexalith.ChatBot.UI.Tests/GovernedOperationsEffectsTests.cs
- tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs
- tests/fixtures/hexalith-chatbot-generated-client.sha256

### Change Log

- 2026-05-31: Implemented Story 2.5 S2 association review read seam, UI state, governed components, localization, responsive CSS, and verification coverage; status moved to review.
- 2026-05-31: Senior review fixed restricted-evidence display suppression and finite disabled-reason preservation; status moved to done.
- 2026-06-10: Validation-only dev-story rerun confirmed no unchecked tasks remained; status preserved as done.
- 2026-06-10: Senior review (adversarial) fixed UI evidence-state classification to honor server-authoritative redaction/visibility/freshness states, added a redaction regression test, and documented two previously undocumented test files in the File List; status preserved as done.

## Senior Developer Review (AI)

Reviewer: Jerome on 2026-05-31

Outcome: Approved after automatic fixes.

### Findings and Fixes

- HIGH: Restricted/redacted association evidence could still render the evidence reference text in S2 evidence chips. Fixed `AssociationReviewService` to classify restricted/redacted/unavailable evidence from safe metadata and updated candidate/comparison chips to display localized restricted-evidence copy instead of references for non-available evidence.
- MEDIUM: Review action disabled reasons collapsed to `decision-command-not-available`, hiding more specific finite reasons such as `evidence-expired`, `not-authorized`, and `projection-pending`. Fixed `ChatBotAssociationReviewActions` to preserve specific finite disabled reasons before falling back to the Story 2.6 command-unavailable state.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed.
- `dotnet test` for UI, Client, and UI.E2E projects could not run because VSTest socket startup is blocked in the sandbox (`SocketException 13`).
- Compiled xUnit v3 executables passed: UI 85/85, Client 14/14, UI.E2E 21/21.
- `git diff --check` passed.

Reviewer: Jerome on 2026-06-10

Outcome: Approved after automatic fixes. No CRITICAL issues; ACs 1-5 verified against the committed implementation and the uncommitted routing-status test additions, all of which build and pass.

### Findings and Fixes

- MEDIUM (AC4 redaction defense-in-depth): `AssociationReviewService.ResolveEvidenceState` classified evidence purely by substring-sniffing the evidence kind/reference text and ignored the routing-status contract's authoritative `VisibilityState`/`RedactionState`/`FreshnessState` enums. Excluded candidates are emitted by the server as `Redacted`, yet exclusion-state names such as `NotFound`, `Archived`, `Ambiguous`, `TenantMismatch`, `Conflict`, and `InvalidReference` contain none of the sniffed keywords, so any rendered evidence reference carrying those states would have surfaced its reference text instead of redacted copy. Fixed `ResolveEvidenceState` to honor the server-authoritative structured states first and fail closed, keeping the keyword check only as a secondary safety net. Added `ServiceShouldHonorServerRedactionStateEvenWhenEvidenceTextHasNoRestrictionKeyword` to lock in the behavior.
- MEDIUM (transparency / File List completeness): Two source test files exercising Story 2.5's routing-status seam were present in the working tree but absent from the File List — `tests/Hexalith.ChatBot.Client.Tests/AssociationRoutingStatusTransportTests.cs` (generated-client transport + metadata-only problem parsing) and the new routing-status endpoint cases in `tests/Hexalith.ChatBot.Server.Tests/Projections/AssociationProjectionTests.cs` (authorized 200, invalid/unknown 403 safe-not-found, unauthenticated 401). Added both to the File List and recorded them here. Both suites build and pass.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed (0 warnings, 0 errors).
- `dotnet test` still cannot start VSTest under the sandbox (`SocketException 13`); used compiled xUnit v3 executables instead.
- Compiled xUnit v3 executables passed: UI 130/130, Client 34/34, Server `AssociationProjectionTests` 15/15.
