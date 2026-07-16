---
baseline_commit: 554a2bd3821c6040cb85d7d4d2163a1ab43d6907
---

# Story 3.6: Approval event rendering

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want approval requests, decisions, and outcomes represented in the project conversation,
so that approval history is visible alongside the work it governed.

## Acceptance Criteria

1. Given an approval conversation item, when it renders on S1, then the item exposes actor attribution, an actor-type label before content in the accessible name, evidence/risk/status/actor/timestamp ordering, non-color status text, reduced-motion behavior, and WCAG 2.2 AA behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.6; _bmad-output/planning-artifacts/epics.md#UX-DR41]
2. Approval requests render first-class metadata for the governed work under review: approval id, proposal/action id, source conversation item or source message id, requester party/user id, requester actor type, requested timestamp, command name, command allowlist version, action summary redaction state, affected resource references, recipient references when present, sender-authority class when present, risk classification, risk action classes, policy snapshot id, expected post-state summary redaction state, evidence-reference ids, evidence freshness states, current approval status, safe next action, redaction state, retention class, schema version, source version, and correlation id. [Source: _bmad-output/planning-artifacts/epics.md#FR41; _bmad-output/planning-artifacts/epics.md#FR42; _bmad-output/planning-artifacts/epics.md#NFR48]
3. Approval decisions render first-class metadata for approve, reject, request-revision, and cancel outcomes: decision kind, decision actor id/type, decided timestamp, authority result, disabled/denial reason where applicable, decision rationale redaction state, policy snapshot id, audit operation id/status, supersedes/superseded-by approval ids when a later approval record replaces an open request, safe next action, and correlation id. [Source: _bmad-output/planning-artifacts/epics.md#FR42; _bmad-output/planning-artifacts/epics.md#FR55; _bmad-output/planning-artifacts/epics.md#FR62]
4. Approval outcomes render first-class metadata for the governed result without exposing payloads: executed command name, resulting operation id, command outcome status, post-commit audit status, projected conversation outcome reference when available, failure/refusal code when present, retryability token when present, and safe next action. The item must not claim "Done" when the command is accepted but projection/audit is pending. [Source: _bmad-output/planning-artifacts/epics.md#FR24; _bmad-output/planning-artifacts/epics.md#FR44; _bmad-output/planning-artifacts/epics.md#UX-DR30]
5. Approval status links or references only authorized policy snapshot and audit detail. Users lacking authority see redacted/unavailable policy and audit references that do not confirm restricted project, file, participant, recipient, command payload, prompt, output, or audit detail existence. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.6; _bmad-output/planning-artifacts/epics.md#FR16; _bmad-output/planning-artifacts/epics.md#FR57]
6. Approval items are append-only history. A later decision or outcome must not mutate, hide, or replace the original request event; it may add explicit links between request, decision, outcome, supersedes, and superseded-by records. Duplicate delivery of the same source version is idempotent; newer source versions append or replace only the same deterministic item for that version. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns; _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]
7. S1 preserves Stories 3.1 through 3.5 behavior: tenant/project partitioning, cursor pagination, source-email enrichment, participant/attachment materialization, association and correction decision history, source-version replay safety, stale/correction safe-next-action behavior, EN/FR localization, responsive layout, forced-colors, reduced-motion, and UI state clearing on route load/failure. [Source: _bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md#Current-State-To-Preserve; _bmad-output/implementation-artifacts/3-5-association-and-correction-decision-rendering.md#Current-State-To-Preserve]
8. Contract, generated client, server projection, conformance, UI service/state/component, localization, static CSS, and UI E2E coverage prove approval rendering is localized, accessible, metadata-only, append-only, replay-safe, policy/audit redaction-safe, and cross-tenant isolated. [Source: _bmad-output/planning-artifacts/epics.md#FR22; _bmad-output/planning-artifacts/epics.md#FR25; _bmad-output/planning-artifacts/epics.md#NFR60]

## Tasks / Subtasks

- [x] Extend the S1 contract spine for approval conversation items (AC: 1, 2, 3, 4, 5, 8)
  - [x] Add an additive item kind for approval rendering, recommended wire token `approval-event`; add an actor kind such as `approval` or `approval-system` if needed. Preserve existing `email-derived`, `system-decision`, `participant`, and `attachment` wire tokens.
  - [x] Add additive `ProjectConversationItem` fields for approval request/decision/outcome metadata. Suggested names: `approvalId`, `approvalEventKind`, `approvalStatus`, `approvalDecisionKind`, `approvalRequesterId`, `approvalRequesterActorType`, `approvalRequestedAtUtc`, `approvalDecisionActorId`, `approvalDecisionActorType`, `approvalDecidedAtUtc`, `approvalOutcomeAtUtc`, `approvalProposalId`, `approvalSourceMessageId`, `approvalSourceConversationItemId`, `approvalCommandName`, `approvalCommandAllowlistVersion`, `approvalRiskClass`, `approvalRiskActionClasses`, `approvalPolicySnapshotId`, `approvalPolicySnapshotVisibility`, `approvalEvidenceReferences`, `approvalEvidenceFreshnessStates`, `approvalAffectedResourceReferences`, `approvalRecipientReferences`, `approvalSenderAuthorityClass`, `approvalExpectedPostStateRedactionState`, `approvalActionSummaryRedactionState`, `approvalDecisionRationaleRedactionState`, `approvalAuthorityResult`, `approvalDisabledReason`, `approvalAuditOperationId`, `approvalAuditStatus`, `approvalCommandOutcomeStatus`, `approvalProjectedOutcomeItemId`, `approvalFailureCode`, `approvalRetryability`, `supersedesApprovalId`, and `supersededByApprovalId`.
  - [x] Add stable enum wire tokens where the contract benefits from enums: request/decision/outcome event kind, pending/approved/rejected/revision-requested/cancelled/executed/failed approval status, approve/reject/request-revision/cancel decision kind, and evidence freshness `fresh`/`stale`/`expired`.
  - [x] Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit generated client output.
  - [x] Add contract/OpenAPI/generated-client tests proving approval fields are additive, wire values are stable, generated client exposes the fields, and raw prompt/output/action payload/rationale/audit/policy body fields are absent.
- [x] Add an approval-event projection source and materialize S1 approval items (AC: 2, 3, 4, 5, 6, 7)
  - [x] Introduce a projection-side approval event view/source such as `PublishedApprovalEvent`, `ApprovalProjectionTranslator`, `ApprovalNotification`, and/or `ApprovalCandidateView` under `src/Hexalith.ChatBot.Server/Projections/` unless an existing approval projection exists by implementation time.
  - [x] Keep this source metadata-only and projection-facing. It must consume EventStore-stamped tenant/domain/aggregate/source-version/correlation metadata, not request-body tenant/project values.
  - [x] Materialize deterministic item ids, for example `approval:{approvalId}:{eventKind}:{sourceVersion}` or an equivalent opaque tenant/project-scoped shape. Request, decision, and outcome events must remain distinct history items.
  - [x] Store approval items in the existing `IProjectConversationProjectionStore`; do not add a transcript table, approval-specific browser data plane, direct audit reader in the UI, or direct EventStore stream reader from S1.
  - [x] Preserve DAPR/CloudEvent duplicate and out-of-order tolerance: duplicate source version is idempotent, stale replay cannot overwrite newer item state, and a decision/outcome arriving before the request still renders a safe metadata-only event and is later enriched when request metadata arrives.
  - [x] Link approval events to the governed source context using stable ids only: project id, approval id, proposal/action id, source message id, source conversation item id, operation id, and audit operation id. Do not copy raw source email text, prompts, generated output, decision rationale, policy bodies, command payloads, or audit envelopes into S1.
- [x] Represent policy snapshot, audit detail, evidence freshness, and authority safely (AC: 2, 3, 4, 5, 8)
  - [x] Show policy snapshot id/version and audit operation/status only when the read model marks them authorized. Otherwise render localized redacted/unavailable explanations without confirming hidden resources.
  - [x] Render evidence references as metadata IDs plus freshness states; `expired` evidence must be visible as an approval-blocking condition, but the full approval review action stays in S3/Epic 4.
  - [x] Render `approve` authority failures as metadata and disabled-reason tokens only. Use finite reason strings where possible, including existing disabled-action reasons such as `insufficient-authority`, `state-not-permitted`, `dependency-degraded`, `awaiting-other-actor`, `policy-blocked`, and `evidence-expired`.
  - [x] Do not implement S3 approval review controls, approval gate execution, AI action conversion, command allowlist execution, outbound sends, or policy editor behavior in this story. This story only renders approval events already present in the projection/event stream.
- [x] Update UI mapping and approval rendering components (AC: 1, 2, 3, 4, 5, 7, 8)
  - [x] Extend `ProjectConversationItemModel` and `ProjectConversationService.MapItem` to carry approval metadata through `IChatBotClient` only.
  - [x] Update `ChatBotConversationStream.razor` to route approval items to a dedicated `ChatBotApprovalConversationItem.razor`; do not overload `ChatBotDecisionConversationItem` or `ChatBotEmailConversationItem`.
  - [x] Reuse existing governed primitives: `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, stable `<time>` elements, definition-list metadata, localized labels, and governed layout classes.
  - [x] Actor label must lead the accessible name. Recommended shape: "Approval event, <localized approval event summary>, <approval status>, <timestamp>".
  - [x] Keep evidence/risk/status/actor/timestamp ordering consistent with existing S1 rows. Plain-language labels precede IDs; IDs remain available as metadata.
  - [x] Add EN/FR resource keys for approval event labels, approval status labels, decision labels, policy/audit visibility labels, risk labels, evidence freshness labels, authority/disabled reason labels, redacted/unavailable explanations, accessible names, and metadata labels. Do not hard-code user-facing strings except stable machine tokens displayed as metadata.
- [x] Maintain S1 responsive, visual, and accessibility behavior (AC: 1, 5, 7, 8)
  - [x] Extend `chatbot.tokens.css` using existing Fluent/FrontComposer tokens only; do not introduce raw `#`, `rgb(`, or `hsl(` color literals.
  - [x] Ensure phone/tablet layouts wrap approval metadata without overlap, preserve evidence/risk/status/actor/timestamp order, and keep policy/audit unavailable explanations keyboard reachable.
  - [x] Ensure forced-colors and reduced-motion rules cover approval rows, actor badges, evidence chips, risk chips, status labels, focus outlines, and unavailable/restricted explanations.
  - [x] Approval status must never be color-only. Use localized text plus icon/shape/border affordances where needed.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests for DTO serialization, OpenAPI schema, generated client availability, approval wire tokens, stable existing item tokens, and absence of raw prompt/output/command payload/policy body/audit envelope/rationale fields.
  - [x] Server projection tests for request, approved, rejected, request-revision, cancelled, command-accepted/projection-pending, command-executed, command-failed, duplicate delivery, stale replay, decision-before-request, outcome-before-decision, expired evidence, unavailable policy snapshot, unavailable audit detail, superseded approval history, and tenant/project partitioning.
  - [x] Conformance/read-surface tests proving foreign, unknown, malformed, missing, ambiguous, stale, and unsafe tenant/project contexts collapse to safe denial with metadata-only bodies and no approval/policy/evidence/audit leakage.
  - [x] UI service/state/component tests for mapped approval metadata, actor-label accessible names, evidence/risk/status/timestamp order, localization keys, policy/audit unavailable distinction, no raw colors, and no raw payload/rationale/audit text.
  - [x] Update existing Playwright/UI.E2E fixture coverage for populated S1 stream to include approval requested, approved, rejected, request-revision, cancelled, expired-evidence, command accepted with projection pending, executed outcome, failed outcome, unavailable policy snapshot, and unavailable audit detail states with forced-colors and reduced-motion assertions where the harness supports them.

## Dev Notes

### Scope Boundaries

- This story is approval event rendering on the existing S1 project conversation stream.
- It may establish contract fields, projection event envelopes, server projection materialization, and UI rendering for approval request/decision/outcome events so Epic 4 can later produce real governed approval records into the same shape.
- Do not implement the S3 approval review surface, approval gate workflow, AI action risk classifier, task-intent conversion, low-risk AI execution, allowlisted command execution, outbound communication, or policy editor behavior. Those belong to Epic 4 and later epics.
- Do not implement the "why this project" evidence/provenance panel (Story 3.9), failures/retries/blocked-state rendering (Story 3.7), AI outcome rendering (Story 3.8), next-action consolidation (Story 3.10), informational/actionable classification or full human-review history (Story 3.11), attachment capture/storage (Story 3.12), attachment state/authorization (Story 3.13), or AI-context packaging (Story 3.14).
- Do not add a chat composer, approval action form, direct audit browser, direct EventStore reader in the UI, or direct server projection access from the UI.

### Existing Code To Reuse

- S1 contract and read surface: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`, `ProjectConversationResponse.cs`, `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`, `ProjectConversationActorKind.cs`, `RiskClass.cs`, OpenAPI `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, and generated client output under `src/Hexalith.ChatBot.Client/Generated/`.
- S1 projection store and item model: `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`, `IProjectConversationProjectionStore.cs`, `InMemoryProjectConversationProjectionStore.cs`, `DaprProjectConversationProjectionStore.cs`, and `ProjectConversationPage.cs`.
- Existing S1 materialization patterns: `ProjectConversationItemView.FromParticipant`, `FromAttachment`, and `FromAssociationDecision`; `AssociationProjectionHandler` shows how to project source context plus append-only decision history into the same conversation store.
- Existing approval-related spine scaffolding: `src/Hexalith.ChatBot.Server/Gateway/Stages/IApprovalGate.cs`, `PassThroughApprovalGate.cs`, `ChatBotApprovalResult.cs`, `ChatBotStateWritingPathInventory` path `approval-decision`, and `CoarseIdempotencyOperationClass` entry `approval-decision`. These are not an approval domain model yet; use them only as guardrail context.
- Existing audit/status references: `src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs`, `IAuditHistoryReader.cs`, `OperationAuditHistoryHttpResults.cs`, `src/Hexalith.ChatBot.Contracts/Queries/OperationAuditStatus.cs`, and `OperationStatus.cs`.
- Existing S1 UI route/state/service: `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`, `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`, `ChatBotDecisionConversationItem.razor`, `ChatBotAttachmentConversationItem.razor`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/*`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`.
- Governed UI primitives: `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotStatusBanner`, `ChatBotBlockedState`, localization resources, and `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Existing tests to extend: `ProjectConversationContractTests`, `OpenApiContractSpineTests`, `ClientGenerationTests`, `ProjectConversationProjectionTests`, `CrossTenantReadSurfaceIsolationTests`, `ProjectConversationServiceTests`, `AssociationReviewComponentContractTests`, `ChatBotLocalizationContractTests`, and `ProjectConversationE2ETests`.

### Current State To Preserve

- Story 3.1 added `GET /api/v1/projects/{projectId}/conversation`, tenant/project keyed S1 projection storage, cursor pagination, authorized empty reads, safe denial for unauthorized reads, governed UI route/state, and reducers that clear previous project state on load/failure.
- Story 3.2 enriched S1 items with source-email metadata from mailbox intake while keeping raw provider `SourceContext` and email bodies out of the read contract. Source-email enrichment has its own source version and must not overwrite newer approval, association, participant, or attachment state.
- Story 3.3 added participant item rendering and pending materialization by tenant/intake. Participant source-version replacement is independent from association, source-email, attachment, and approval replacement.
- Story 3.4 added attachment item rendering and pending materialization by tenant/intake. Attachment source-version replacement is independent from approval replacement.
- Story 3.5 split source email association context from append-only association/correction decision items using deterministic `decision:{associationId}:{sourceVersion}` item ids. Approval items must follow the same append-only history principle and must not reuse `decision:` item ids.
- `ProjectConversationItemView.ShouldReplace` currently guards item replacement by source version for the same item id. If approval events use independent source versions, tests must prove stale approval replay cannot overwrite newer approval state and cannot mutate source email, participant, attachment, or association/correction state.
- `ProjectConversationItemKind` currently has `EmailDerived`, `SystemDecision`, `Participant`, and `Attachment`. Story 3.6 should add a distinct approval item kind unless implementation proves reuse is safer and contract tests prove no ambiguity.
- Existing worktree has an unrelated modification in `_bmad-output/story-automator/orchestration-2-20260531-161212.md`; do not touch or revert it.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. UI reads through `IChatBotClient` only; server projection internals, DAPR, EventStore, audit stores, approval gate internals, and sibling adapters stay server-side. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- ChatBot derived records carry tenant/provenance/kernel/redaction/retention/schema/version metadata. Approval records are decision snapshots: append-only and superseded, never mutated; live mirrors are version-stamped projections. [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- Tenant authority comes from authenticated claims/context and projection gates, never route/body/query values. Unknown, foreign, malformed, missing, ambiguous, stale, or unsafe tenant context must collapse to safe denial without confirming restricted resource existence. [Source: _bmad-output/planning-artifacts/epics.md#FR16; _bmad-output/planning-artifacts/epics.md#NFR2]
- DAPR pub/sub is at-least-once and unordered. Projection handlers must tolerate duplicate and out-of-order approval, association, intake, participant, and attachment events. SignalR nudges, if used, trigger re-query and are never trusted as data. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- Cursor tokens stay opaque and tenant/project scoped. Do not embed tenant, project, mailbox, approval, proposal, operation, audit, evidence, policy, command payload, prompt, output, or rationale text in cursor values.
- Use `System.Text.Json` shared options and camelCase wire names. Do not add inline `JsonSerializerOptions`, Newtonsoft.Json, or new serialization libraries.
- Lifecycle-state strings are exact and shared: `Received`, `Proposed`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Corrected`, `Correcting`, and `Correction-delayed`. Do not invent synonyms for existing lifecycle fields.

### UX And Accessibility Guardrails

- The UX package is binding despite having no mockups. Approval rows must use consistent actor attribution, actor-type labels before content in accessible names, and evidence/risk/status/actor/timestamp ordering. [Source: _bmad-output/planning-artifacts/epics.md#Cross-cutting-acceptance-and-planning-guidance; _bmad-output/planning-artifacts/epics.md#UX-DR41]
- Approval rows should read as governed approval events, not anonymous chat messages and not association decisions. Use a dedicated component or equivalent split from `ChatBotDecisionConversationItem`.
- Evidence, risk, status, actor, and timestamp must appear in consistent order. Plain-language labels precede raw IDs; IDs remain available as metadata.
- Conversation stream focus remains stable: Tab reaches approval event groups and any policy/audit unavailable explanations. Reduced motion suppresses non-essential item movement.
- EN/FR localization is required. Stable machine codes, IDs, lifecycle states, status codes, reason codes, command names, policy snapshot IDs, operation IDs, and correlation IDs remain untranslated; labels and explanations are translated. Avoid concatenated strings for accessible names.
- Disabled or unavailable approval-related detail must expose a reachable reason via inline text or focusable explanation. Tooltip-only explanation is not acceptable.

### Project Structure Notes

- Contract DTO and enum changes belong under `src/Hexalith.ChatBot.Contracts/Queries/`, `src/Hexalith.ChatBot.Contracts/Enums/`, and `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Regenerated client output belongs in `src/Hexalith.ChatBot.Client/Generated/`; do not hand-edit `.g.cs` files.
- Server approval projection changes belong under `src/Hexalith.ChatBot.Server/Projections/`. If later implementation needs a true approval aggregate or command model, keep it under `src/Hexalith.ChatBot.Server/Gateway/Stages/` only for the existing gate seam or under the future Governance/Approval seam, but do not expand that domain beyond what S1 rendering needs.
- UI changes belong under `src/Hexalith.ChatBot.UI/Components/Governed/`, `Components/Pages/`, `Services/`, `State/ProjectConversation/`, `Localization/`, and `wwwroot/css/`.
- Tests mirror source boundaries under `tests/Hexalith.ChatBot.Contracts.Tests/`, `Client.Tests/`, `Server.Tests/Projections/`, `Conformance.Tests/`, `UI.Tests/`, and `UI.E2E.Tests/`.

### Previous Story Intelligence

- Story 3.5 established the latest append-only S1 history pattern: split source context from event history, use deterministic per-event item ids, preserve source-version ownership, route through the existing project conversation store, and add a dedicated governed component. Approval rendering should reuse that shape.
- Story 3.5 review fixed metadata token rendering, unavailable explanations, and prior-project correction detail suppression. Approval rendering must preserve stable wire tokens for metadata and provide meaningful reachable explanations for unavailable policy/audit detail.
- Story 3.4 review fixed restricted metadata leakage and redacted/unavailable distinction. Approval rendering must not repeat the same class of bug with policy snapshot bodies, audit details, prompt/output text, command payloads, decision rationale, recipients, affected resources, or evidence values.
- Story 3.3 review fixed raw enum display and actor badge fallback issues. Approval rendering needs localized user-facing labels; machine tokens may appear only in intended metadata fields.
- Story 3.2 review fixed stale source-email replay, association/source-email source-version conflation, and missing threshold-band metadata. Approval event enrichment must keep source-version ownership separate.
- Story 3.1 review fixed DAPR stale replay handling, cross-project stale UI state, authorized empty conversation reads, and stable rendered item IDs. Approval rendering must preserve those regression targets.
- Epic 2 retrospective and architecture both warn not to invent a separate conversation model. Build on the existing contract spine and S1 projection; do not introduce a transcript table, approval-specific UI data plane, or browser-side audit data plane.
- Prior validation used compiled xUnit v3 executables because VSTest socket creation was blocked in this sandbox. Prefer that fallback if `dotnet test` fails with local socket permission errors.

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - targeted contract/client/server/UI/conformance tests touched by this story
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` if the compiled runner is available
- Use xUnit v3, Shouldly, NSubstitute, bUnit, existing Playwright fixtures, and existing CSS/static contract tests only. Do not add assertion, mocking, UI, or E2E libraries.
- Add regression tests around event ordering: approval decision before request, approval outcome before decision, request before source email, source email before request, participant/attachment before approval, approval before participant/attachment, duplicate approval event delivery, stale approval replay after current approval state, superseded approval request, expired evidence, unavailable policy snapshot, unavailable audit detail, and command accepted while projection/audit remains pending.
- Include negative content assertions for raw approval rationale, raw prompt, raw model output, raw command payload, raw policy body, raw audit envelope, unauthorized project/file/participant/recipient names, hidden evidence values, raw exception text, and hidden diagnostic data in API, UI, fixture, logs/test output where applicable.

### Latest Technical Notes

- Architecture pins .NET SDK `10.0.302`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC via FrontComposer, Fluxor, DAPR 1.17.x, Aspire 13.3.x, xUnit v3 3.2.x, Shouldly, NSubstitute, and Testcontainers. Do not upgrade packages for this rendering story. [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints--Dependencies]
- Dapr pub/sub documentation confirms at-least-once delivery semantics. Approval projection code must therefore be idempotent and out-of-order tolerant. [Source: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- NuGet lists a newer Fluent UI Blazor prerelease/package line than the repo pin. Keep the repo-pinned Fluent/FrontComposer stack unless a separate dependency-upgrade story is created. [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components]
- Microsoft Learn tracks .NET 10 breaking changes separately; do not mix framework migration work into approval rendering. [Source: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10]

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3, Story 3.6, FR22, FR24, FR28, FR41-FR45, FR55, FR57, FR62, NFR2, NFR11, NFR40, NFR48, NFR60, UX-DR18, UX-DR35, and UX-DR41 context.
- `_bmad-output/planning-artifacts/architecture.md` - append-only decision snapshots, projection boundaries, DAPR/event ordering, file organization, CommandGateway approval-gate seam, audit/status contracts, and testing standards.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - conversation detail, approval state patterns, approval/audit semantics, focus model, state-to-feedback matrix, reduced motion, responsive behavior, and redaction requirements.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - Fluent/FrontComposer inheritance, semantic status colors, risk/evidence/approval component tokens, forced-colors constraints, and compact operational surface posture.
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs` - current S1 item DTO to extend additively.
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs` and `ProjectConversationActorKind.cs` - current item/actor wire tokens to preserve and extend.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` - current S1 item materialization logic and deterministic item-id helpers to extend.
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs` - current pattern for writing source context plus append-only history into `IProjectConversationProjectionStore`.
- `src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs` and `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs` - existing approval-decision audit/idempotency guardrail entries.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`, `ChatBotDecisionConversationItem.razor`, `ChatBotRiskChip.razor`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs` - current S1 routing, rendering, risk chip, and UI mapping patterns to extend.
- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md` - S1 shell/read surface implementation context and review fixes.
- `_bmad-output/implementation-artifacts/3-2-associated-email-rendering-in-the-conversation-stream.md` - source-email enrichment context and review fixes.
- `_bmad-output/implementation-artifacts/3-3-participant-rendering-in-the-conversation-stream.md` - participant materialization/UI pattern and review fixes.
- `_bmad-output/implementation-artifacts/3-4-attachment-rendering-in-the-conversation-stream.md` - attachment materialization/UI pattern and redaction review fixes.
- `_bmad-output/implementation-artifacts/3-5-association-and-correction-decision-rendering.md` - latest append-only decision history pattern, implementation context, and review fixes.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story workflow executed 2026-06-01T05:21:48+02:00.
- Source discovery loaded `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md`, sprint status, Story 3.5, Story 3.4, current S1 projection/UI/test files, sibling project-context files, recent git history, and official Dapr/NuGet/Microsoft technical references.
- Discovery results: loaded `{epics_content}` from 1 file, `{architecture_content}` from 1 file, `{prd_content}` from 1 product-brief/fallback file, and `{ux_content}` from 2 sharded UX files: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`.
- Dev-story workflow executed 2026-06-01T05:27:59+02:00 through 2026-06-01T05:46:14+02:00.
- Regenerated `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` from the updated OpenAPI contract.
- Validation used xUnit v3 compiled test executables because `dotnet test` failed in this sandbox with VSTest socket permission error.
- Validation passed: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
- Validation passed: all compiled `tests/*.Tests/bin/Debug/net10.0/*.Tests` xUnit executables with `-noLogo -parallel none`.
- Dev-story validation rerun 2026-06-10T13:40:45+02:00. No unchecked Story 3.6 tasks/subtasks remained; story and sprint status were already `done`, so no implementation or checkbox changes were required.
- Validation passed: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
- Validation passed: compiled xUnit story validation runners for contracts/OpenAPI, generated client, server projections, conformance, UI, and `ProjectConversationE2ETests`.
- Validation passed: remaining compiled ChatBot regression runners. Tier-3 Aspire integration tests were discovered and skipped by their fixture because `HEXALITH_CHATBOT_TIER3=1` and the required Docker/DAPR runtime were not enabled.

### Completion Notes List

- Added additive approval-event contract fields, approval enums, OpenAPI schema, stable wire token tests, and regenerated client output.
- Added metadata-only approval projection view/source/translator/handler plus conversation-store materialization with deterministic `approval:{approvalId}:{eventKind}:{sourceVersion}` item ids, idempotent same-version replacement, and request-context enrichment for out-of-order decisions/outcomes.
- Added dedicated S1 approval UI model mapping and `ChatBotApprovalConversationItem.razor` using governed actor/evidence/risk primitives, localized labels, reachable policy/audit unavailable explanations, forced-colors, and reduced-motion coverage.
- Added EN/FR localization, UI service/component/static coverage, expanded S1 E2E fixture coverage, and regression tests for metadata-only, append-only, replay-safe, cross-tenant-safe approval rendering.
- Re-ran Story 3.6 dev-story validation on 2026-06-10; no unchecked tasks remained and no implementation changes were needed.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes. Critical issues remaining: 0.

Findings fixed:

- [HIGH] Redacted/unavailable policy snapshot references could still expose `approvalPolicySnapshotId`, and unavailable audit detail could still expose `approvalAuditOperationId`. Fixed read-model materialization to suppress those identifiers unless policy visibility is `authorized` or audit status is available, and added UI defense-in-depth for the same rule.
- [HIGH] The Dapr-backed projection store did not match the in-memory store for out-of-order approval enrichment; a decision/outcome received before the request would not be re-materialized with request metadata when the request arrived later. Fixed Dapr approval request/event indexing and re-enrichment.
- [MEDIUM] E2E fixture expectations allowed restricted policy/audit identifiers in redacted/unavailable approval rows, so the regression surface could bless leakage. Updated fixture output and expectations to require unavailable explanations without the restricted IDs.

Validation:

- Passed: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- Passed: targeted compiled xUnit runs for server projection, UI service/localization, contracts/OpenAPI, generated client, and project conversation E2E tests.
- Passed: all executable compiled `tests/*.Tests/bin/Debug/net10.0/*.Tests` xUnit test assemblies with `-noLogo -parallel none`.

Reviewer: Jerome (story-automator review) on 2026-06-10

Outcome: Approved. Critical issues remaining: 0. No code fixes required.

Scope reviewed: the committed Story 3.6 implementation (present in HEAD tree via `f6c79ba`) plus the uncommitted review-cycle delta in `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` and the regenerated `tests/test-summary.md`.

Validation evidence:

- Passed: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` (0 warnings, 0 errors).
- Passed: `ProjectConversationContractTests` + `OpenApiContractSpineTests` (21/21).
- Passed: `ClientGenerationTests` (19/19).
- Passed: `ProjectConversationProjectionTests` (59/59).
- Passed: `ProjectConversationServiceTests` + `ChatBotLocalizationContractTests` (20/20).
- Passed: `CrossTenantReadSurfaceIsolationTests` (10/10).
- Passed: `ProjectConversationE2ETests` compiled runner (23/23).

Findings confirmed clean (claims validated against reality):

- AC2/AC3/AC4 metadata fields are additive on `ProjectConversationItem`; no raw prompt/output/command-payload/policy-body/audit-envelope/rationale fields exist on the contract.
- AC5 redaction is enforced at two layers: `ProjectConversationItemView.AuthorizedPolicySnapshotId` drops the policy snapshot id unless visibility is `authorized`, and `AuthorizedAuditOperationId` drops the audit operation id when audit status is `redacted`/`unavailable`; `ChatBotApprovalConversationItem.razor` repeats the same suppression as defense-in-depth.
- AC6 append-only/replay safety: `ApprovalEventView.WithRequestContext` enriches out-of-order decision/outcome events; projection tests cover decision-before-request, outcome-before-decision, duplicate delivery, and stale replay.
- AC7/AC8: EN/FR localization is at parity (827/827 resource entries, 82/82 approval keys, zero missing in either direction).
- File List is accurate: every listed source file is committed in the HEAD tree.
- Uncommitted E2E delta closes an AC2 request-metadata fixture gap (adds source message, requester actor type, affected resources, recipients, sender authority, expected post-state, action-summary state, redaction state, retention class, schema version, source version, correlation id) and is internally consistent and passing.

Low-severity observations (no fix applied; intentional design):

- [LOW] The E2E populated-stream fixture is a hand-curated, story-scoped HTML string fed via `SetContentAsync`; it intentionally omits later-story component sections (approval action buttons, AI preview/classification/review-history). Live component rendering is covered by bUnit UI.Tests, so the E2E layer is not the live-rendering authority.
- [LOW] In a headless sandbox (no Playwright browser) the test takes the `AssertPopulatedWithoutBrowser` substring path; the browser path is the authoritative semantic/accessibility check.

### File List

- `_bmad-output/implementation-artifacts/3-6-approval-event-rendering.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ApprovalDecisionKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ApprovalEventKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ApprovalEvidenceFreshness.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ApprovalStatus.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationActorKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/RiskClass.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedApprovalEvent.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Change Log

- 2026-06-01: Implemented Story 3.6 approval event rendering end-to-end and moved story to review.
- 2026-06-01: Senior review auto-fixed approval policy/audit redaction leaks, Dapr out-of-order approval enrichment, regression fixture expectations, and moved story to done.
- 2026-06-10: Re-ran dev-story validation for Story 3.6; no unchecked tasks remained, and build plus compiled regression tests passed.
- 2026-06-10: Story-automator adversarial review pass. Verified all 8 ACs and all `[x]` tasks against committed source; build + contract/client/server/UI/conformance/E2E suites all green; redaction suppression and EN/FR parity confirmed. 0 critical/high/medium defects; no code fixes required. Status remains `done`.
