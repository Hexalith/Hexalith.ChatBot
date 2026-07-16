---
baseline_commit: d483a69
---

# Story 4.2: Task-intent review, conversion, and disposition

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized reviewer,
I want to review captured intent and either convert it to a governed action or close it,
so that only intended work proceeds.

## Acceptance Criteria

1. Given a captured task-intent record, when an authorized reviewer opens the review surface, then the system returns the complete FR35 task-intent metadata, the authorized full source message for review, source evidence offsets/references, current correction-readiness state, current task-intent state, and available transitions without relying on browser-side message parsing. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.2; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR36]
2. Given the reviewer is unauthorized, the task intent is foreign-tenant, the source message is unavailable, the corrected context is stale, or the source message is redacted/quarantined by policy, when review data is requested, then the response fails closed with safe message-catalog codes and does not confirm restricted task-intent, project, file, party, mailbox, or source-message existence. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR1; #NFR2; #NFR7; #NFR11; #NFR38-#NFR40; _bmad-output/planning-artifacts/architecture.md#Fail-Closed-NFR15a]
3. Given an actionable captured task intent, when an authorized reviewer converts it, then a durable FR41-ready AI action proposal record is created through the CommandGateway, linked to the source `task_intent_id`, `source_message_id`, requester, evidence refs, policy snapshot, correlation id, and source version, and the task intent cannot be converted again by replay or race. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR37; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys]
4. Given conversion succeeds, when the proposal is projected, then S1 renders an AI proposal/conversion event with `safeNextAction = review-ai-action` or `classify-ai-action` and metadata-only proposal details; it must not execute a command, call an AI/model/tool provider, classify final risk, request approval, or emit completed-work UI. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.3; #Story-4.5; #Story-4.7; _bmad-output/planning-artifacts/architecture.md#Governed-AI-Mediation]
5. Given conversion is attempted while source evidence is stale, terminal, duplicate, already converted, unauthorized, cross-tenant, missing audit readiness, or policy-blocked, when the command is submitted, then no proposal is created and the rejection is audited with metadata-only reason codes. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR46; #FR91a; _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]
6. Given a captured task intent is non-actionable, when an authorized reviewer dispositions it, then the reviewer can mark exactly one terminal state: `not-actionable`, `duplicate`, `already-handled`, or `out-of-scope`; the original record and source evidence remain preserved for A9a evaluation and audit. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.2; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR38]
7. Given the selected disposition is `duplicate`, when the command is accepted, then it requires and stores a predecessor task-intent id in the same tenant/project scope, rejects missing or foreign predecessors indistinguishably, and projects the duplicate link without exposing restricted source-message details. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR38; _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
8. Given conversion or disposition changes a task-intent state, when project conversation, task-intent status, audit history, or A9a evaluation data is queried, then the new state, reviewer actor, decided timestamp, reason code, predecessor/proposal link, source version, and audit operation id are visible where authorized and redacted where not authorized. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Command-and-Query-Contracts; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]
9. Contract, OpenAPI/generated-client, aggregate, projection, review-query, UI service/component, accessibility, idempotency, stale-context, duplicate-link, audit, and cross-tenant leakage tests prove the review, conversion, terminal disposition, source-message exposure, and safe rejection behavior. [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]

## Tasks / Subtasks

- [x] Define the review, conversion, and disposition contract surface (AC: 1, 3, 6, 7, 8, 9)
  - [x] Add purpose-named contract records under `src/Hexalith.ChatBot.Contracts/Queries/` for task-intent review/status, for example `TaskIntentReview`, `TaskIntentReviewSourceMessage`, `TaskIntentAvailableTransition`, and `TaskIntentTransitionAuditSummary`.
  - [x] Add commands under `src/Hexalith.ChatBot.Contracts/Commands/`: `MarkTaskIntentDisposition` and `ProposeAIAction` unless an existing generated-contract naming constraint requires a more specific name. Keep `ProposeAIAction` aligned with the PRD command inventory.
  - [x] Include `taskIntentId`, `projectId`, `sourceMessageId`, reviewer intent/decision, expected source version, evidence refs, policy snapshot id, correlation id, optional `predecessorTaskIntentId` for duplicate, and proposal input metadata. Tenant id must come from authenticated server context, not command body.
  - [x] Add an additive `TaskIntentState.Converted` wire value (`converted`) if needed to prevent repeated conversion. Do not overload `Captured` to mean converted.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` when the public contract changes.
- [x] Add authorized task-intent review query support without leaking raw source text broadly (AC: 1, 2, 8, 9)
  - [x] Add a server-side review/status endpoint scoped by both project and task intent, for example `GET /api/v1/projects/{projectId}/task-intents/{taskIntentId}`, or the existing route pattern if the OpenAPI spine already dictates one.
  - [x] Introduce `IMailboxMessageContentSource` or an equivalent mailbox-source adapter for authorized full-source-message review. Mirror `IMailboxAttachmentContentSource` patterns: default unavailable implementation, safe reason codes, no raw provider error text.
  - [x] Keep raw/full source message content out of `TaskIntentRecord`, project conversation list items, audit envelopes, logs, status summaries, generated fixtures, and off-surface exports. The full source message may appear only in the authorized review response and UI review panel.
  - [x] Enforce tenant/project/read authorization before resolving the source message. Unknown, foreign, redacted, quarantined, unavailable, stale, or degraded source state returns safe review-unavailable output without confirming existence.
  - [x] Derive available transitions from current task-intent state, conversion-readiness, corrected-context readiness, source-message availability, authorization, and policy. Disabled controls must carry reachable disabled reasons.
- [x] Implement task-intent transition handling in the governed operation aggregate (AC: 3, 5, 6, 7, 9)
  - [x] Extend `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` so replay tracks task-intent records/state, terminal states, converted proposal links, predecessor duplicate links, source version, and transition ids. A set of ids alone is not enough for Story 4.2.
  - [x] Extend `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` with pure `Handle(...)` methods for `ProposeAIAction` and `MarkTaskIntentDisposition`, using `CommandEnvelope` for tenant/user authority where needed.
  - [x] Emit typed events/rejections under `src/Hexalith.ChatBot.Server/Governance/AiMediation/`, for example `TaskIntentConvertedToAiActionProposal`, `TaskIntentDispositionMarked`, and `TaskIntentTransitionRejected`.
  - [x] Model all business failures as domain rejections, not exceptions: missing captured intent, expected source-version mismatch, terminal state, duplicate predecessor invalid, correction-readiness stale, audit readiness unavailable, unsupported transition, or invalid metadata.
  - [x] Idempotency: equivalent replay returns the prior logical outcome; conflicting transition with the same idempotency key or already terminal/converted state rejects with a safe code and no second proposal/disposition event.
- [x] Create the FR41-ready AI action proposal record without implementing later stories (AC: 3, 4, 5, 9)
  - [x] The proposal must structurally carry `proposalId`, `taskIntentId`, `sourceMessageId`, `sourceConversationItemId` when known, requester/reviewer ids, evidence refs, intended command name or action kind, affected resource refs, recipient refs if present, policy snapshot id, source version, correlation id, redaction state, retention class, schema version, and safe next action.
  - [x] Keep risk classification fields pending/indeterminate or absent as appropriate for Story 4.3; do not implement the tag+heuristic classifier here.
  - [x] Do not invoke an AI provider, embedding provider, external tool, Folders content read beyond authorized metadata/context references, or allowlisted command execution in this story.
  - [x] If projection reuses existing AI outcome rendering, project conversion as `AiOutcomeKind.Proposal` with `AiOutcomeStatus.Proposed` and `safeNextAction` set for the next governed step. If a more specific proposal projection is added, keep it tenant-partitioned and append-only.
  - [x] Create metadata-only audit envelope data for conversion. The audit envelope includes actor, command/operation name, resource id, decision, reason code, correlation id, timestamp, policy snapshot id, source evidence refs, state transition, redaction decision, and outcome.
- [x] Project task-intent transition state into S1 and status reads (AC: 1, 4, 6, 7, 8, 9)
  - [x] Extend `TaskIntentProjectionHandler`, `TaskIntentProjectionTranslator`, `IProjectConversationProjectionStore`, `InMemoryProjectConversationProjectionStore`, and `DaprProjectConversationProjectionStore` so conversion/disposition updates are idempotent, order-tolerant, source-version guarded, and tenant/project partitioned.
  - [x] Preserve `ProjectConversationItemView.BuildDetectedIntent()` behavior from Story 4.1: captured task-intent records remain the source of true FR35 detected intent, and the previous placeholder fallback remains available for non-captured rows.
  - [x] Add status/review projection fields for available transitions, terminal state, converted proposal link, duplicate predecessor link, audit operation id/status, reviewer actor, and safe next action.
  - [x] Ensure project conversation ETags change when authorized task-intent transition metadata changes and remain stable for 304 when unchanged.
- [x] Add S1/UI review and disposition affordances using existing governed components (AC: 1, 2, 4, 6, 7, 8, 9)
  - [x] Extend `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs` and `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs` to map the new review/status contract through the generated client only.
  - [x] Add a review panel/component under `src/Hexalith.ChatBot.UI/Components/Governed/` that reuses `ChatBotConversationItemClassificationBadge`, `ChatBotConversationItemStatusSummary`, `ChatBotEvidenceChip`, risk/evidence/status primitives, localization resources, and existing redaction helpers. Do not create a new visual language.
  - [x] The panel must show FR35 metadata, full source message only for authorized review, source evidence refs, current state, reviewer-safe disabled reasons, and actions: convert, not-actionable, duplicate, already-handled, out-of-scope.
  - [x] Duplicate disposition requires a predecessor task-intent id input with validation summary. Other dispositions require only bounded reason/metadata if policy requires it; never store raw free-form rationale in audit/projection unless explicitly redacted and tested.
  - [x] Accessibility: keyboard operation, focus-on-success/error summary, reachable disabled reasons (`aria-disabled` or adjacent "Why unavailable?" affordance), unique landmark labels, WCAG 2.2 AA target sizing, live-region behavior for current-user conversion/disposition success or rejection, and reduced-motion compatibility.
- [x] Extend A9a fixture/evaluation support for review outcomes (AC: 6, 8, 9)
  - [x] Reuse `src/Hexalith.ChatBot.Testing/Fixtures/TaskIntentEvaluationCalculator.cs` and the scaffold `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json`.
  - [x] Add scaffold labels for review outcome/disposition where needed while preserving `isScaffold`. Do not claim a full A9a corpus exists.
  - [x] Preserve precision/recall metrics from Story 4.1 and add outcome-count reporting for converted, not-actionable, duplicate, already-handled, and out-of-scope records.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract/OpenAPI/generated-client tests for command/query schema, enum wire values including any new `converted` state, required fields, additive compatibility, no tenant authority in command body, and raw source message limited to the review response.
  - [x] Aggregate tests for captured-to-converted, captured-to-each-terminal-disposition, duplicate predecessor validation, terminal/converted transition rejection, source-version conflicts, idempotent replay, and metadata-only rejections.
  - [x] Projection tests for conversion/disposition state materialization, out-of-order event replay, stale source-version ignored, duplicate delivery idempotency, project conversation ETag changes, and no browser-side parsing.
  - [x] Review query/server tests for authorized full source message, unavailable source message, unauthorized/foreign/unknown indistinguishable denial, stale corrected context blocked, redacted/quarantined source blocked, and no raw provider error leakage.
  - [x] UI service/component/bUnit/E2E tests for review panel fields, actions, disabled reasons, duplicate id validation, localization EN/FR resource coverage, keyboard/focus/live-region contracts, and redaction-safe off-surface behavior.
  - [x] Isolation/leakage tests proving denial bodies, logs, projections, audit summaries, fixtures, and UI list surfaces never leak raw mail body, subject, provider payload, prompt, model output, tool args, file names/paths when unauthorized, tenant ids in denial bodies, secrets, or raw exceptions.

## Dev Notes

### Scope Boundaries

- This story owns FR36-FR38: authorized review of captured task intent, conversion into a durable AI action proposal shell, and terminal disposition.
- This story may add commands, query/status contracts, source-message review adapter, aggregate transition events, projection updates, S1 review UI, OpenAPI/client regeneration, A9a outcome reporting, and focused tests.
- This story must not implement Story 4.3 final risk classification, Story 4.4 low-risk AI execution, Story 4.5 approval decision workflow/S3, Story 4.6 detailed preview beyond proposal metadata needed here, Story 4.7 allowlisted command execution, Story 4.8 refusal behavior beyond safe transition rejections, Story 4.9 correction invalidation of AI proposals, CLI/MCP parity, outbound email, tenant policy editor UI, or model/tool invocation.
- "Full source message" is review-only authorized content. Do not persist it into task-intent records, proposal records, conversation projections, audit envelopes, logs, fixtures, or off-surface exports.

### Existing Code To Reuse

- Story 4.1 task-intent contracts and capture path:
  - `src/Hexalith.ChatBot.Contracts/Commands/CaptureTaskIntent.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentRecord.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentSourceEvidenceOffset.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/TaskIntentState.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/DeterministicTaskIntentKernel.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentIdempotency.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentReasonCodes.cs`
- Durable write and state patterns:
  - `src/Hexalith.ChatBot.Contracts/Commands/IChatBotCommand.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- Projection/read chokepoints:
  - `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionHandler.cs`
  - `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
  - `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
  - `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
  - `src/Hexalith.ChatBot.Server/Program.cs` `ToContractItem` and project-conversation endpoints
- Existing AI proposal and approval rendering shapes to reuse, not duplicate:
  - `src/Hexalith.ChatBot.Server/Projections/PublishedAiOutcomeEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- Current source-message state:
  - `CaptureMailboxMessageIntake` explicitly keeps body content out of intake; source identity and attachment refs are metadata-only.
  - There is `IMailboxAttachmentContentSource`; there is no source-message content adapter yet. Add one rather than pushing raw message content into existing metadata projections.

### Current State To Preserve

- Story 4.1 created metadata-only durable task-intent records and projected them into `ProjectConversationDetectedIntent`. Preserve the placeholder fallback for older non-captured rows.
- `TaskIntentCaptured` currently projects into project conversation by matching source message/provider id and replacing placeholder detected intent data. Keep this path idempotent and source-version guarded.
- Existing project conversation reads enforce tenant/project authorization, safe not-found denials, stable ETags, and metadata-only AI-context package assembly. Do not weaken these paths for review.
- Existing AI outcome and approval projection code already has fields for proposal id, risk class, evidence refs, source message id, command name, approval status, audit status, and safe next action. Extend/reuse these shapes instead of creating an unrelated proposal UI contract.
- `CaptureMailboxMessageIntake` states "body content is out of scope." Story 4.2 must introduce authorized review retrieval separately; raw source text must not backflow into intake events or projections.
- Existing worktree has an unrelated modified story-automator orchestration file. Do not revert or include unrelated changes.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. Public DTOs and command/query contracts live in `.Contracts`; transition handling, content-source authorization, projections, and fail-closed checks live in `.Server`; UI consumes generated client contracts only.
- Every state mutation must enter via CommandGateway: authentication, tenant-bind, authorization, risk-classify, approval-gate, coarse idempotency, pre-commit audit, EventStore, post-commit audit. Do not mutate task-intent state from a projection, UI service, or mailbox adapter.
- Tenant id comes from authenticated/server context, never request body, route text alone, provider payload, or UI-supplied data. Route/project ids are comparison inputs only.
- Use `System.Text.Json`, JSON camelCase, additive serialization-tolerant schema evolution, UTC `DateTimeOffset`, ULID-compatible stable identifiers, central package management, and existing xUnit v3/Shouldly patterns.
- Derived records carry `tenantId`, source provenance, kernel/schema version, redaction state, retention class, source version, and correlation id. Decision/proposal snapshots are append-only and superseded, not mutated.
- Logs, traces, support artifacts, audit summaries, fixtures, and user-visible errors are metadata-only unless a dedicated authorized review response is explicitly returning full source message content. Raw provider errors, mail body/subject, prompts, completions, tool args, paths, secrets, and raw exceptions are stop-ship leakage defects.
- Root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - Targeted compiled xUnit v3 runners for Contracts, Client generation, Server aggregate/projection/review query, UI service/component, Testing fixture/evaluation, Architecture, and Conformance/isolation.
- Sandbox note inherited from Story 4.1: `dotnet test` via VSTest can fail in this environment with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 test DLLs with `-parallel none` after build.
- Add broader UI E2E only if the review panel or conversation rendering changes. Use accessible roles/labels or stable data attributes, not CSS selectors or sleeps.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 4 is "Governed AI Action Mediation" and Story 4.2 covers FR36-FR38.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR35-FR38, FR41-FR46, FR91a, NFR1-NFR22, NFR36-NFR46, NFR60-NFR64, and the command/query inventory naming `MarkTaskIntentDisposition`, `ProposeAIAction`, and `GetTaskIntentStatus`.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially tenant policy schema, shared command pipeline, and AI action proposal idempotency keys.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially Governed AI Mediation, CommandGateway flow, derived-record shape, audit envelope, modular-monolith seams, and D6 immutable decision snapshots.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`, especially Conversation Detail, AI Action Review, evidence/risk chips, approval panel/controls, source evidence behavior, accessibility/focus/live-region requirements, and responsive/touch rules.
- Loaded persistent project-context facts from sibling module `project-context.md` files. Relevant rules: .NET SDK `10.0.302`, `net10.0`, warnings-as-errors, central package management, root-level submodules only, EventStore aggregate pure `Handle/Apply`, DAPR at-least-once duplicate/order tolerance, FrontComposer/Fluent UI v5 inheritance, and metadata-only diagnostics.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/4-1-task-intent-detection-and-data-contract.md`. Story 4.1 deliberately excluded review UI, disposition controls, risk classification, approval, execution, and model/tool invocation. It also established the durable task-intent contract and projection reuse points.
- Inspected current code to confirm extension points: `TaskIntentRecord`, `TaskIntentState`, `CaptureTaskIntent`, `TaskIntentCaptured`, `TaskIntentProjectionHandler`, `ProjectConversationItemView.BuildDetectedIntent`, `GovernedOperationAggregate.Handle(CaptureTaskIntent)`, `GovernedOperationState.Apply(TaskIntentCaptured)`, AI outcome/approval projection shapes, and UI project-conversation mapping.
- Latest-technology research not required for implementation: the story is constrained to repo-pinned .NET SDK `10.0.302`, `net10.0`, Dapr/Aspire/EventStore patterns, OpenAPI/NSwag generation, Fluent UI/FrontComposer patterns, and xUnit v3. No external package upgrade or new third-party API is in scope.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.2, M0 governed AI action mediation sequence.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR35-FR38, FR41-FR46, FR91a, NFRs, command/query inventory.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, Governed AI Mediation, Data Architecture, derived-record shape, audit envelope, project structure.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - visual semantics, evidence/risk/proposal/approval components.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - IA, review flows, state patterns, accessibility floor, live-region behavior.
- `_bmad-output/implementation-artifacts/4-1-task-intent-detection-and-data-contract.md` - previous-story scope boundaries, existing code reuse list, validation notes, and implemented files.
- `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentRecord.cs` - existing durable task-intent record.
- `src/Hexalith.ChatBot.Contracts/Enums/TaskIntentState.cs` - existing state vocabulary to extend.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentCaptured.cs` - existing task-intent capture event.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - current capture handler and command aggregate pattern.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` - current replay state needing richer task-intent transition state.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` - current detected-intent, AI outcome, and approval projection mapping.
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs` - projection store contract to extend.
- `src/Hexalith.ChatBot.Server/Program.cs` - project conversation read, ETag, and contract mapping chokepoints.
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs` - generated-client-only UI mapping pattern.
- `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json` - A9a scaffold to extend without claiming full corpus coverage.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story workflow executed on 2026-06-01 with user-provided Story ID `4.2` and `#YOLO`.
- Workflow activation resolved with no prepend/append steps and persistent facts from sibling `project-context.md` files.
- Sprint status read fully; `epic-4` was already `in-progress` and `4-2-task-intent-review-conversion-and-disposition` started as `backlog`.
- Checklist validation applied during story creation; no user input requested.
- Dev-story workflow executed on 2026-06-01 from ready-for-dev to review.
- Built `src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj --no-restore -m:1 /nr:false` to regenerate and validate the OpenAPI client.
- Built `Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` after implementation.
- Ran focused xUnit v3 compiled runners for Contracts, Client, Server, UI, Testing, Architecture, and Conformance with `-parallel none`.
- Ran remaining compiled test runners for AppHost, Aspire, ServiceDefaults, Workers, Integration, and UI.E2E; Integration reported two expected Tier-3 skips gated by `HEXALITH_CHATBOT_TIER3`.
- Dev-story workflow revalidated on 2026-06-10; no unchecked Story 4.2 tasks or review follow-ups were present.
- Rebuilt `Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` and reran all 15 compiled ChatBot xUnit v3 test assemblies with `-parallel none`; build and tests passed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story file created for Story 4.2 and sprint status advanced to `ready-for-dev`.
- Validation pass focused on preventing scope bleed into risk classification, approval execution, allowlisted command execution, and raw source-message leakage.
- External package/version research was not required because implementation is constrained to repo-pinned versions and existing platform patterns.
- Added the additive task-intent review/conversion/disposition contract surface, OpenAPI route, generated client support, and UI service mappings.
- Added governed aggregate transition handling for `ProposeAIAction` and `MarkTaskIntentDisposition`, including metadata-only rejection events and idempotency checks.
- Added authorized task-intent review query support with a default fail-closed mailbox-message content source so raw source text appears only on the review surface.
- Projected converted/disposition state into S1/status reads, including metadata-only AI proposal projection with `safeNextAction = review-ai-action`.
- Added a governed review panel for source review, conversion, terminal dispositions, duplicate predecessor input, disabled reasons, and live-region status.
- Extended the A9a scaffold with review outcome counts while preserving the Story 4.1 precision/recall scaffold.
- Revalidated the completed story on 2026-06-10; no implementation changes or checkbox updates were required because all tasks were already complete.

### File List

- `_bmad-output/implementation-artifacts/4-2-task-intent-review-conversion-and-disposition.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Client/ChatBotClient.cs`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Client/IChatBotClient.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MarkTaskIntentDisposition.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/ProposeAIAction.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/TaskIntentState.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AiActionProposalRecord.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentAvailableTransition.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentRecord.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentReview.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentReviewSourceMessage.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentTransitionAuditSummary.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/IMailboxMessageContentSource.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/MailboxMessageContentResult.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentConvertedToAiActionProposal.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentDispositionMarked.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentTransitionRejected.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/UnavailableMailboxMessageContentSource.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedTaskIntentEvent.cs`
- `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TaskIntentEvaluationCalculator.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TaskIntentEvaluationReport.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedEvaluationDataset.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/TaskIntentContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.Testing.Tests/Fixtures/TenantScopedFixtureManifestTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
- `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json`

### Change Log

- 2026-06-01: Created Story 4.2 implementation context for task-intent review, conversion, and disposition.
- 2026-06-01: Implemented task-intent review, conversion proposal creation, disposition transitions, projection/status reads, UI review affordance, A9a outcome scaffold, OpenAPI/client updates, and focused validation coverage.
- 2026-06-01: Senior review auto-fixed stale review fail-closed behavior, transition tenant/requester/metadata validation, same-version replay ordering, duplicate predecessor tenant scoping, and duplicate UI predecessor selection payload.
- 2026-06-10: Revalidated Story 4.2 completion; no unchecked tasks remained, and the full compiled ChatBot regression set passed.
- 2026-06-10: Adversarial senior review re-run. Verified all 9 ACs against the live implementation and the full compiled ChatBot suite (Server 1544, Contracts 480, Client 34, UI 131, Testing 41, E2E 78, Architecture 39, Conformance 87 — 0 failures), including the newly added AC2 redacted/policy-blocked fail-closed test and AC6 terminal-disposition E2E coverage. One MEDIUM integration-completeness finding logged: the review panel is not mounted on the S1 `ProjectConversation` page and the convert/disposition write-path is absent from `ProjectConversationService`. No CRITICAL issues; status remains `done`.

## Senior Developer Review (AI)

Reviewer: Codex on 2026-06-01

Outcome: Approved after auto-fixes. No critical issues remain.

### Findings Fixed

- [HIGH] Stale corrected context still returned authorized full source message from the review endpoint. Fixed by returning safe unavailable review output before source content resolution when `ConversionReadinessBlocked` is true.
- [HIGH] `ProposeAIAction` and `MarkTaskIntentDisposition` did not bind transition handling back to the captured record tenant and requester, and allowed unsafe metadata in proposal/disposition fields. Fixed tenant, requester, optional metadata, evidence/resource/recipient, policy snapshot, predecessor, and reason-code validation.
- [HIGH] Same-source-version replay could overwrite a converted/dispositioned task intent back to `captured` in aggregate and projection state. Fixed task-intent replacement ranking so terminal/converted states win over captured replays at the same source version.
- [HIGH] Duplicate disposition predecessor validation checked project scope but not tenant scope. Fixed predecessor validation to require same tenant and project.
- [MEDIUM] The UI review panel collected duplicate predecessor input but only emitted the transition string, so callers could not submit the required predecessor id. Fixed the callback payload to include `PredecessorTaskIntentId`.
- [MEDIUM] File List missed UI review test files changed by the implementation/review. Updated the File List.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -parallel none`
- `dotnet tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests.dll -parallel none`
- `dotnet tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests.dll -parallel none`
- `dotnet tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests.dll -parallel none`
- `dotnet tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests.dll -parallel none`

---

Reviewer: Claude (Opus 4.8) on 2026-06-10

Outcome: Approved with one tracked follow-up. No CRITICAL issues; status remains `done`.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — succeeded, 0 warnings/0 errors.
- Compiled xUnit v3 runners (`-parallel none`), all green: Server 1544, Contracts 480, Client 34, UI 131, Testing 41, UI.E2E 78, Architecture 39, Conformance 87 — 2808 tests, 0 failures.

### AC verification (all confirmed against the live implementation)

- AC1/AC2: `GET /api/v1/projects/{projectId}/task-intents/{taskIntentId}` returns full FR35 metadata + authorized source message + evidence + transitions + audit summary server-side, and fails closed (`Available=false`, `Record=null`, `SourceMessage=null`) for unauthorized/foreign/unknown (indistinguishable `SafeNotFound`/`task_intent_unavailable`), stale (`ConversionReadinessBlocked` resolved before source content), unavailable, redacted, and policy-blocked source. New `TaskIntentReviewEndpointShouldFailClosedWhenSourceIsRedactedOrQuarantinedByPolicy` proves no raw payload/tenant/party leakage.
- AC3/AC4/AC5: `ProposeAIAction` is pure, transition-idempotent, requester/tenant/project/source-version/state guarded (`ValidateCapturedRecord` rejects already-converted, terminal, stale, mismatch), and projects a metadata-only `AiOutcomeKind.Proposal`/`Proposed` with `safeNextAction = review-ai-action`; no provider/tool/command execution.
- AC6/AC7: `MarkTaskIntentDisposition` restricts to exactly the four terminal states; duplicate requires a predecessor in the same tenant **and** project, rejecting missing/foreign indistinguishably (`task_intent_duplicate_predecessor_unavailable`); source record/evidence preserved.
- AC8: state/actor/decided-at/reason/predecessor+proposal links/source-version/audit-operation-id surfaced via review + audit summary + projection fields; redacted where unauthorized.
- AC9: full contract/aggregate/projection/review-query/UI/isolation suites pass, including Architecture + Conformance leakage guardrails.

### Findings

- [MEDIUM] S1 UI integration incomplete. `ChatBotTaskIntentReviewPanel.razor`, its `TaskIntentReviewModel` mapping, and `ProjectConversationService.GetTaskIntentReviewAsync` exist and are contract-tested, but the panel is consumed by **zero** pages — `ProjectConversation.razor` neither calls `GetTaskIntentReviewAsync` nor mounts the panel — and `ProjectConversationService` has no convert/disposition write method, so the panel's `OnTransitionSelected` callback has no destination (contrast Story 4.5, which added `SubmitApprovalDecisionAsync` and wired `ChatBotApprovalConversationItem` into the conversation stream). The review/disposition affordance is therefore not user-reachable in the running app. Server-side ACs are fully met and tested, so this does not block the story; it is logged as a follow-up because mounting placement and intent-selection UX on S1 is a design decision (see Review Follow-ups).

### Review Follow-ups (AI)

- [ ] [AI-Review][MEDIUM] Mount `ChatBotTaskIntentReviewPanel` on the S1 `ProjectConversation` surface and add `SubmitProposeAIActionAsync`/`SubmitTaskIntentDispositionAsync` to `ProjectConversationService` (route through the generic `IChatBotClient.SubmitAsync`, mirroring `SubmitApprovalDecisionAsync`), with page wiring tests. [src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor; src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs]
