---
baseline_commit: 50af02746cf42f8b0bfba200cbc5469511807df3
---

# Story 3.5: Association and correction decision rendering

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want association, correction, rejection, deferral, and review decisions represented in the project conversation,
so that human and system decisions are visible without erasing history.

## Acceptance Criteria

1. Given an association or correction decision conversation item, when it renders on S1, then the item exposes actor attribution, an actor-type label before content in the accessible name, evidence/risk/status/actor/timestamp ordering, non-color status text, reduced-motion behavior, and WCAG 2.2 AA behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.5; _bmad-output/planning-artifacts/epics.md#UX-DR41]
2. Association decisions render first-class metadata for confirmed association, rejection, deferral, and needs-review outcomes: decision kind, lifecycle state, decision actor/type, decision timestamp, surface origin, policy snapshot/version, threshold band, confidence score, evidence-reference summary, safe next action, redaction state, retention class, schema version, source version, and correlation id. [Source: src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs; src/Hexalith.ChatBot.Server/Projections/PublishedAssociationEvent.cs; _bmad-output/planning-artifacts/epics.md#FR8]
3. Correction decisions render first-class metadata for project reassignment and propagation state: correction kind, correction actor/type, corrected timestamp, prior project id, corrected project id, predecessor association id, supersedes/superseded-by association ids, correction id, workflow instance id, downstream impact status, required/completed/failed store keys, propagation progress, started/completed/estimated timestamps, responsible owner role, stale-context state, safe next action, redaction state, retention class, schema version, source version, and correlation id. [Source: src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs; src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs; _bmad-output/planning-artifacts/architecture.md#Correction-propagation-FR91a]
4. Superseded decisions are append-only history items. A newer decision or correction must not mutate, hide, or replace the prior rendered decision item; it may add explicit supersedes/superseded-by links and status labels. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.5; _bmad-output/planning-artifacts/epics.md#Story 2.7; _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
5. Decision and correction items remain metadata-only. The contract, projection, UI, logs, diagnostics, fixtures, and test output must not expose raw decision notes, raw correction rationale, raw provider source context, unauthorized project names, hidden evidence values, raw exception text, or audit-restricted payloads. Redacted and unavailable values must be visibly distinct and understandable to screen-reader users. [Source: _bmad-output/planning-artifacts/epics.md#NFR2; _bmad-output/planning-artifacts/architecture.md#Problem-error-responses; _bmad-output/planning-artifacts/architecture.md#Audit-envelope]
6. S1 preserves Stories 3.1 through 3.4 behavior: tenant/project partitioning, cursor pagination, source-email enrichment, participant and attachment materialization, source-version replay safety, stale/correction safe-next-action behavior, EN/FR localization, responsive layout, forced-colors, reduced-motion, and UI state clearing on route load/failure. [Source: _bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md#Current-State-To-Preserve; _bmad-output/implementation-artifacts/3-2-associated-email-rendering-in-the-conversation-stream.md#Current-State-To-Preserve; _bmad-output/implementation-artifacts/3-3-participant-rendering-in-the-conversation-stream.md#Current-State-To-Preserve; _bmad-output/implementation-artifacts/3-4-attachment-rendering-in-the-conversation-stream.md#Current-State-To-Preserve]
7. Contract, generated client, server projection, conformance, UI service/state/component, localization, static CSS, and UI E2E coverage prove decision rendering is localized, accessible, metadata-only, append-only, replay-safe, and cross-tenant isolated. [Source: _bmad-output/planning-artifacts/epics.md#FR22; _bmad-output/planning-artifacts/epics.md#FR25; _bmad-output/planning-artifacts/epics.md#NFR60]

## Tasks / Subtasks

- [x] Extend the S1 contract spine for decision conversation items (AC: 1, 2, 3, 4, 5, 7)
  - [x] Add additive fields to `ProjectConversationItem` and OpenAPI `ProjectConversationItem` for association/correction decision metadata. Suggested names: `decisionKind`, `decisionActorId`, `decisionActorType`, `decidedAtUtc`, `decisionNoteRedactionState`, `surfaceOrigin`, `policySnapshotVersion`, `correctionKind`, `priorProjectId`, `correctedProjectId`, `predecessorAssociationId`, `supersedesAssociationId`, `supersededByAssociationId`, `correctionRationaleRedactionState`, `correctionActorId`, `correctionActorType`, `correctedAtUtc`, `downstreamImpactStatus`, `correctionId`, `workflowInstanceId`, `requiredStoreKeys`, `completedStoreKeys`, `failedStoreKeys`, `propagationProgressNumerator`, `propagationProgressDenominator`, `propagationStartedAtUtc`, `propagationCompletedAtUtc`, `propagationEstimatedCompletionAtUtc`, `propagationStatus`, `isCorrectedContextStale`, and `responsibleOwnerRole`.
  - [x] Keep existing `ProjectConversationItemKind.SystemDecision` and `ProjectConversationActorKind.SystemDecision` wire tokens stable. Add no duplicate decision item kind unless the contract tests prove it is necessary and backward-compatible.
  - [x] Add localized display/status enum mapping where needed, but keep stable machine tokens, IDs, reason codes, lifecycle states, correlation IDs, and status codes untranslated when shown as metadata.
  - [x] Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit generated client output.
  - [x] Add contract/OpenAPI/generated-client tests proving decision fields are additive, wire values are stable, generated client exposes the new fields, and raw note/rationale/provider/evidence/audit payload fields are absent.
- [x] Materialize decisions into append-only project conversation items (AC: 2, 3, 4, 5, 6)
  - [x] Reuse `AssociationProjectionHandler`, `AssociationNotification`, `AssociationCandidateView`, `PublishedAssociationEvent`, and `AssociationProjectionTranslator`. Do not add a second association read model or transcript table.
  - [x] Change `ProjectConversationItemView.FromAssociation` or add a dedicated factory so decision/correction notifications create stable decision item IDs distinct from the source email association item. Suggested shape: `decision:{associationId}:{sourceVersion}` or an equivalent opaque, deterministic, tenant/project-scoped ID.
  - [x] Preserve the original email-derived association item as the source context item. Decision/correction notifications must add or update their own decision item and must not convert the source email item into the only current `system-decision` record.
  - [x] For duplicate delivery of the same decision source version, replace only that same decision item with equivalent metadata. For newer source versions, append a new decision/correction history item. For stale source versions, do not overwrite newer current-state fields or newer decision history.
  - [x] Preserve source-email enrichment independently: source-email source-version updates may enrich decision items where safe, but must not raise/lower association or decision source versions.
  - [x] Preserve participant and attachment materialization. A correction state update must refresh participant/attachment stale lifecycle and safe-next-action semantics without changing their independent source versions.
- [x] Represent supersession and correction propagation safely (AC: 3, 4, 5)
  - [x] Render superseded history from `PredecessorAssociationId`, `SupersedesAssociationId`, and `SupersededByAssociationId`; never collapse a superseded item into the current decision.
  - [x] If a correction references both prior and corrected projects, keep historical visibility for the prior project without leaking unauthorized corrected-project detail. The corrected project may show the correction item only when the authenticated actor is authorized for that project.
  - [x] During `Correcting` or `Correction-delayed`, show stale-context state, downstream impact status, owner role, progress, and safe next action. Do not present corrected context as current until propagation is complete.
  - [x] Render required/completed/failed store keys as machine-token metadata only. Do not expose internal exception text, audit payloads, raw projection data, or hidden sibling-service details.
  - [x] Use message-catalog/localized copy for user-facing labels and unavailable/restricted explanations. Decision notes and correction rationales may be represented only through redaction state or authorized safe snippets if an existing contract already provides them.
- [x] Update UI mapping and decision rendering components (AC: 1, 2, 3, 5, 6, 7)
  - [x] Extend `ProjectConversationItemModel` and `ProjectConversationService.MapItem` to carry decision metadata through `IChatBotClient` only.
  - [x] Add `ChatBotDecisionConversationItem.razor` or split the existing system-decision path so decision items do not overload `ChatBotEmailConversationItem` semantics. `ChatBotConversationStream.razor` should route `SystemDecision` items to the dedicated component while preserving existing email, participant, and attachment routes.
  - [x] Reuse existing governed primitives: `ChatBotActorBadge`, `ChatBotEvidenceChip`, stable `<time>` elements, definition-list metadata, localized labels, and governed layout classes.
  - [x] Actor label must lead the accessible name. Recommended shape: "System decision, <localized decision summary>, <status/lifecycle>, <timestamp>".
  - [x] Keep evidence/risk/status/actor/timestamp ordering consistent with existing S1 rows. Plain-language labels precede IDs; IDs remain available as metadata.
  - [x] Add EN/FR resource keys for decision labels, correction labels, supersession labels, propagation labels, unavailable/redacted explanations, accessible names, status copy, and metadata labels. Do not hard-code user-facing strings except stable machine tokens displayed as metadata.
- [x] Maintain S1 responsive, visual, and accessibility behavior (AC: 1, 5, 6, 7)
  - [x] Extend `chatbot.tokens.css` using existing Fluent/FrontComposer tokens only; do not introduce raw `#`, `rgb(`, or `hsl(` color literals.
  - [x] Ensure phone/tablet layouts wrap decision metadata without overlap, preserve evidence/status/actor/timestamp order, and keep any decision affordances or "Why unavailable?" explanations keyboard reachable.
  - [x] Ensure forced-colors and reduced-motion rules cover decision rows, actor badges, evidence chips, status labels, focus outlines, progress/status metadata, and unavailable/restricted explanations.
  - [x] Decision status must never be color-only. Use text labels and icon/shape/border affordances where needed.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests for DTO serialization, OpenAPI schema, generated client availability, stable `system-decision` wire tokens, and absence of raw note/rationale/provider/evidence/audit payload fields.
  - [x] Server projection tests for confirm, reject, defer, needs-review, correction accepted, correction propagation started, correction store invalidated, correction delayed, correction completed, duplicate delivery, stale replay, newer replay, source-email-before-decision, decision-before-source-email, participant/attachment preservation during correction, superseded history preservation, and tenant/project partitioning.
  - [x] Conformance/read-surface tests proving foreign, unknown, malformed, missing, ambiguous, stale, and unsafe tenant/project contexts collapse to safe denial with metadata-only bodies and no decision/evidence leakage.
  - [x] UI service/state/component tests for mapped decision metadata, actor-label accessible names, evidence/status/timestamp order, localization keys, supersession labels, redaction/unavailable distinction, no stale prior project content, and no raw colors.
  - [x] Update existing Playwright/UI.E2E fixture coverage for populated S1 stream to include association confirmed, rejected, deferred, needs-review, correction, superseded, correcting, correction-delayed, and correction-completed decision states with forced-colors and reduced-motion assertions where the harness supports them.

## Dev Notes

### Scope Boundaries

- This story is decision and correction rendering on the existing S1 project conversation stream.
- Do not implement the "why this project" evidence/provenance panel; that is Story 3.9. Story 3.5 may show evidence-reference summaries and links/IDs needed for later panel navigation, but not the full evidence drawer.
- Do not implement approval events (Story 3.6), failures/retries/blocked-state rendering (Story 3.7), AI outcomes (Story 3.8), next-action consolidation (Story 3.10), informational/actionable classification or full human-review history (Story 3.11), attachment capture/storage (Story 3.12), attachment state/authorization (Story 3.13), or AI-context packaging (Story 3.14).
- Do not change association command semantics unless an additive projection/contract field is strictly required for rendering. The durable sources remain existing association, decision, and correction events.
- Do not add a chat composer, transcript table, direct audit browser, direct EventStore reader in the UI, or direct server projection access from the UI.

### Existing Code To Reuse

- S1 contract and read surface: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`, `ProjectConversationResponse.cs`, `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`, `ProjectConversationActorKind.cs`, OpenAPI `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, and generated client output under `src/Hexalith.ChatBot.Client/Generated/`.
- Existing association/correction source shape: `src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs`, `AssociationNotification.cs`, `AssociationProjectionHandler.cs`, `AssociationProjectionTranslator.cs`, and `PublishedAssociationEvent.cs`.
- Existing correction propagation status source: `src/Hexalith.ChatBot.Server/Association/CorrectionPropagationStatuses.cs` and `CorrectionPropagationStoreKeys.cs`.
- S1 projection store and item model: `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`, `IProjectConversationProjectionStore.cs`, `InMemoryProjectConversationProjectionStore.cs`, `DaprProjectConversationProjectionStore.cs`, and `ProjectConversationPage.cs`.
- Existing S1 UI route/state/service: `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`, `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`, `ChatBotEmailConversationItem.razor`, `ChatBotParticipantConversationItem.razor`, `ChatBotAttachmentConversationItem.razor`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/*`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`.
- Governed UI primitives: `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotStatusBanner`, `ChatBotBlockedState`, localization resources, and `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Existing tests to extend: `ProjectConversationContractTests`, `OpenApiContractSpineTests`, `ClientGenerationTests`, `ProjectConversationProjectionTests`, `AssociationProjectionTests`, `CrossTenantReadSurfaceIsolationTests`, `ProjectConversationServiceTests`, `AssociationReviewComponentContractTests`, `ChatBotLocalizationContractTests`, and `ProjectConversationE2ETests`.

### Current State To Preserve

- Story 3.1 added `GET /api/v1/projects/{projectId}/conversation`, tenant/project keyed S1 projection storage, cursor pagination, authorized empty reads, safe denial for unauthorized reads, governed UI route/state, and reducers that clear previous project state on load/failure.
- Story 3.2 enriched S1 items with source-email metadata from mailbox intake while keeping raw provider `SourceContext` and email bodies out of the read contract. Source-email enrichment has its own source version and must not overwrite newer association/correction state.
- Story 3.3 added participant item rendering and pending materialization by tenant/intake. Participant source-version replacement is independent from association and source-email replacement.
- Story 3.4 added attachment item rendering and pending materialization by tenant/intake. Attachment source-version replacement is independent from association, source-email, and participant replacement.
- `ProjectConversationItemView.FromAssociation` currently emits `SystemDecision` when `DecisionKind` or `CorrectionKind` is present, but uses `AssociationId` as the item id. Story 3.5 must avoid erasing history by separating the source email item from append-only decision/correction items.
- `AssociationProjectionHandler` already merges correction propagation state and projects to `IProjectConversationProjectionStore`. Reuse that path; do not duplicate projection consumers.
- Existing worktree has an unrelated modification in `_bmad-output/story-automator/orchestration-2-20260531-161212.md`; do not touch or revert it.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. UI reads through `IChatBotClient` only; server projection internals, DAPR, EventStore, audit stores, and sibling adapters stay server-side. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- ChatBot derived records carry tenant/provenance/kernel/redaction/retention/schema/version metadata. Decision snapshots are append-only and superseded, never mutated; live mirrors are version-stamped projections. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- Tenant authority comes from authenticated claims/context and projection gates, never route/body/query values. Unknown, foreign, malformed, missing, ambiguous, stale, or unsafe tenant context must collapse to safe denial without confirming restricted resource existence. [Source: _bmad-output/planning-artifacts/epics.md#NFR2; _bmad-output/planning-artifacts/epics.md#NFR11]
- DAPR pub/sub is at-least-once and unordered. Projection handlers must tolerate duplicate and out-of-order intake, association, decision, correction, participant, and attachment events. SignalR nudges, if used, trigger re-query and are never trusted as data. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- Cursor tokens stay opaque and tenant/project scoped. Do not embed tenant, project, mailbox, association, decision, correction, evidence, audit, provider, or raw note/rationale text in cursor values.
- Use `System.Text.Json` shared options and camelCase wire names. Do not add inline `JsonSerializerOptions`, Newtonsoft.Json, or new serialization libraries.
- Lifecycle-state strings are exact and shared: `Received`, `Proposed`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Corrected`, `Correcting`, and `Correction-delayed`. Do not invent synonyms.

### UX And Accessibility Guardrails

- The UX package is binding despite having no mockups. Conversation events must use consistent actor attribution, actor-type labels before content in accessible names, and evidence/risk/status/actor/timestamp ordering. [Source: _bmad-output/planning-artifacts/epics.md#Cross-cutting-acceptance-and-planning-guidance; _bmad-output/planning-artifacts/epics.md#UX-DR41]
- Decision rows should read as governed decisions, not anonymous chat messages and not plain email rows. Use a dedicated component or equivalent split from `ChatBotEmailConversationItem`.
- Evidence, risk/status, actor, and timestamp must appear in consistent order. Plain-language labels precede raw IDs; IDs remain available as metadata.
- Conversation stream focus remains stable: Tab reaches decision/event groups and any unavailable/restricted explanations. Reduced motion suppresses non-essential item movement.
- EN/FR localization is required. Stable machine codes, IDs, lifecycle states, status codes, reason codes, and correlation IDs remain untranslated; labels and explanations are translated. Avoid concatenated strings for accessible names.

### Project Structure Notes

- Contract DTO and enum changes belong under `src/Hexalith.ChatBot.Contracts/Queries/`, `src/Hexalith.ChatBot.Contracts/Enums/`, and `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Regenerated client output belongs in `src/Hexalith.ChatBot.Client/Generated/`; do not hand-edit `.g.cs` files.
- Server projection changes belong under `src/Hexalith.ChatBot.Server/Projections/`. Association command/event behavior remains in the existing Association/Operations/Gateway paths unless strictly necessary.
- UI changes belong under `src/Hexalith.ChatBot.UI/Components/Governed/`, `Components/Pages/`, `Services/`, `State/ProjectConversation/`, `Localization/`, and `wwwroot/css/`.
- Tests mirror source boundaries under `tests/Hexalith.ChatBot.Contracts.Tests/`, `Client.Tests/`, `Server.Tests/Projections/`, `Conformance.Tests/`, `UI.Tests/`, and `UI.E2E.Tests/`.

### Previous Story Intelligence

- Story 3.4 established the newest additive item pattern for S1: extend contract/OpenAPI/generated client, materialize through the existing project conversation store, add a dedicated governed component, localize EN/FR copy, update CSS using existing tokens, and expand contract/server/UI/conformance/E2E coverage.
- Story 3.4 review fixed restricted metadata leakage and redacted/unavailable distinction. Decision rendering must not repeat the same class of bug with decision notes, correction rationale, project names, evidence values, audit details, or progress diagnostics.
- Story 3.3 review fixed raw enum display and actor badge fallback issues. Decision rendering needs localized user-facing labels; machine tokens may appear only in intended metadata fields.
- Story 3.2 review fixed stale source-email replay, association/source-email source-version conflation, and missing threshold-band metadata. Decision enrichment must keep source-version ownership separate.
- Story 3.1 review fixed DAPR stale replay handling, cross-project stale UI state, authorized empty conversation reads, and stable rendered item IDs. Decision rendering must preserve those regression targets.
- Epic 2 retrospective and architecture both warn not to invent a separate conversation model. Build on the existing contract spine and S1 projection; do not introduce a transcript table, decision-specific UI data plane, or browser-side audit data plane.
- Prior validation used compiled xUnit v3 executables because VSTest socket creation was blocked in this sandbox. Prefer that fallback if `dotnet test` fails with local socket permission errors.

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - targeted contract/client/server/UI/conformance tests touched by this story
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` if the compiled runner is available
- Use xUnit v3, Shouldly, NSubstitute, bUnit, existing Playwright fixtures, and existing CSS/static contract tests only. Do not add assertion, mocking, UI, or E2E libraries.
- Add regression tests around event ordering: source email before decision, decision before source email, participant/attachment before correction, correction before participant/attachment, duplicate decision delivery, stale decision replay after current decision, correction accepted before propagation events, propagation events out of order, and correction completed after delayed/failed store updates.
- Include negative content assertions for raw decision notes, raw correction rationale, provider source context, unauthorized project names, hidden evidence values, raw audit payloads, raw exception text, and hidden diagnostic data in API, UI, fixture, logs/test output where applicable.

### Latest Technical Notes

- Architecture pins .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC via FrontComposer, Fluxor, DAPR 1.17.x, Aspire 13.3.x, xUnit v3 3.2.x, Shouldly, NSubstitute, and Testcontainers. Do not upgrade packages for this rendering story. [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints--Dependencies]
- Dapr pub/sub documentation states at-least-once delivery semantics. Decision and correction projection code must therefore be idempotent and out-of-order tolerant. [Source: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- NuGet lists newer Fluent UI Blazor prerelease/package versions than the repo pin. Keep the repo-pinned Fluent/FrontComposer stack unless a separate dependency-upgrade story is created. [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components]
- Microsoft Learn tracks .NET 10 breaking changes separately; do not mix framework migration work into decision rendering. [Source: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10]

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3, Story 3.5, FR8, FR22, FR23, FR25, FR28, FR63, NFR2, NFR11, NFR40, NFR60, UX-DR16, UX-DR18, and UX-DR41 context.
- `_bmad-output/planning-artifacts/architecture.md` - append-only decision snapshots, correction propagation, lifecycle vocabulary, projection boundaries, DAPR/event ordering, file organization, and testing standards.
- `src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs` - current association/decision/correction projection fields already available before S1 mapping.
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs` - event-to-notification mapping for association decisions and correction propagation states.
- `src/Hexalith.ChatBot.Server/Projections/PublishedAssociationEvent.cs` - incoming association/correction event payload shape.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` - current S1 item materialization logic that must be split for append-only decisions.
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs` and `DaprProjectConversationProjectionStore.cs` - existing in-memory and DAPR-backed S1 projection stores to extend.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor` and `ChatBotEmailConversationItem.razor` - current system-decision routing/rendering to split.
- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md` - S1 shell/read surface implementation context and review fixes.
- `_bmad-output/implementation-artifacts/3-2-associated-email-rendering-in-the-conversation-stream.md` - source-email enrichment context and review fixes.
- `_bmad-output/implementation-artifacts/3-3-participant-rendering-in-the-conversation-stream.md` - participant materialization/UI pattern and review fixes.
- `_bmad-output/implementation-artifacts/3-4-attachment-rendering-in-the-conversation-stream.md` - attachment materialization/UI pattern, latest Story 3 continuity, and review fixes.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story workflow executed 2026-06-01T04:28:57+02:00.
- Source discovery loaded `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/architecture.md`, Story 3.4, Story 3.3, sprint status, current projection/UI/test files, sibling project-context files, and recent git history.
- No PRD or UX shard files were present under `_bmad-output/planning-artifacts`; Epic 3 contains the binding FR/UX references used here.
- Dev-story workflow executed 2026-06-01T04:50:08+02:00.
- `dotnet test` attempted for targeted projects but VSTest socket creation failed with `System.Net.Sockets.SocketException (13): Permission denied`; compiled xUnit v3 runners were used as the documented sandbox fallback.
- Validation completed with `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`, targeted xUnit runner suites for contract/client/server/UI/conformance/E2E, and all compiled test assemblies under `tests/**/bin/Debug/net10.0/*Tests`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story status set to ready-for-dev.
- Extended the S1 contract spine, OpenAPI schema, and generated client with additive decision/correction metadata while preserving `system-decision` item and actor wire tokens.
- Split association source-context materialization from append-only decision/correction history items using deterministic `decision:{associationId}:{sourceVersion}` item IDs.
- Added safe prior-project correction projection behavior that preserves historical visibility without projecting corrected-project display detail into the prior project.
- Added dedicated decision rendering through `ChatBotDecisionConversationItem`, UI state/service mapping, EN/FR localization, responsive/reduced-motion/forced-colors CSS, and metadata-only E2E fixture coverage for decision and correction states.
- Added/updated contract, generated-client, server projection, conformance, UI service/component/localization, and UI E2E tests; full compiled test assembly regression passed.
- Senior developer review completed 2026-06-01; auto-fixed metadata token rendering, unavailable decision explanations, and prior-project correction suppression coverage.

### Change Log

- 2026-06-01: Implemented Story 3.5 association/correction decision rendering and moved story to review.
- 2026-06-01: Senior developer review auto-fixes applied; story moved to done.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fixes. No critical issues remain.

Findings fixed:

- Medium: Decision/correction metadata reached the UI as generated C# enum names (`Associate`, `ProjectReassignment`, `Redacted`) instead of stable wire tokens required for metadata display. Fixed `ProjectConversationService` to preserve `EnumMember` wire values for decision kind, correction kind, and decision/correction redaction states, and broadened localization to support both wire and generated-token inputs.
- Medium: Decision unavailable explanation rendered only `Unavailable`, which was not a meaningful reachable explanation for screen-reader users. Added localized EN/FR decision-unavailable reason copy and rendered it with the existing `Why unavailable?` pattern.
- Low: Prior-project correction projection had no focused regression proving corrected-project detail is suppressed from historical prior-project items. Added server projection coverage for that suppression while preserving the authorized corrected-project item.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none`
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none`
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests`
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests`
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none`

### File List

- `_bmad-output/implementation-artifacts/3-5-association-and-correction-decision-rendering.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
