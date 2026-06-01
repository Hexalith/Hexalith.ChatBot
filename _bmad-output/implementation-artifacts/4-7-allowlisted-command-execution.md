---
baseline_commit: a8f1c37
---

# Story 4.7: Allowlisted command execution

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As the system,
I want approved AI actions executed only through allowlisted governed commands,
so that AI can never invoke an un-allowlisted command.

## Acceptance Criteria

1. Given an approved AI action whose approval decision is current and has `DecisionKind = Approve`, when execution is requested, then the system executes only a command present in the current AI-action command allowlist version. For M0 this allowlist contains exactly `Project.AppendConversationMessage`, and the execution enters through the ChatBot `CommandGateway` and EventStore path, not a sibling-client shortcut. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.7`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Command Allowlist v0 (M0)`; `_bmad-output/planning-artifacts/architecture.md#Governed AI Mediation`]
2. Given an approved AI action attempts any command other than `Project.AppendConversationMessage` in M0, or the proposal command/allowlist metadata is missing, stale, unsupported, superseded, or mismatched with the approval record, when execution is attempted, then the system fails closed before durable mutation, returns a catalog-backed metadata-only rejection, records an auditable denial fact with correlation/surface/proposal/approval metadata, and does not create an idempotency admission, dispatch an EventStore command, call a sibling service, publish a success outcome, or expose hidden tenant/project/file/body/prompt/provider details. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.7`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`; `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`]
3. Given an approved `Project.AppendConversationMessage` execution passes tenant, authorization, approval, allowlist, idempotency, and pre-commit audit gates, when dispatch runs, then the accepted work is represented by a typed ChatBot command/event contract, uses trusted server-side approval/proposal metadata, preserves tenant/project/requester/correlation/policy snapshot/idempotency provenance, and routes to the bounded Conversations integration only through a ChatBot-owned adapter or EventStore command boundary outside aggregate `Handle` logic. [Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`; `_bmad-output/planning-artifacts/architecture.md#Sibling integration`; `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`]
4. Given execution succeeds, when projection handlers process the resulting events, then an append-only AI outcome row and project conversation row show `execution-started`, `execution-succeeded`, `outcome-recorded`, command name, allowlist version, approval ID, proposal ID, operation ID, audit status, correlation ID, safe next action, and permitted generated-content visibility without raw prompt text, provider payloads, file contents, raw email bodies, secrets, raw exceptions, or unauthorized resource identifiers. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.7`; `_bmad-output/implementation-artifacts/4-6-ai-action-preview-and-inspection.md#Current State To Preserve`; `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs`]
5. Given execution fails because dispatch, dependency, audit, projection, idempotency, policy, correction freshness, or sibling conversation state is unavailable or invalid, when the failure is observed, then the lifecycle records `execution-failed` with stable reason code, retryability, audit status, duplicate-safety note where applicable, and safe next action; retryable paths remain idempotent and terminal failures cannot be reclassified as success by replay or out-of-order projection delivery. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Surface state coverage`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency Keys (per operation class)`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`]
6. Given an approval is rejected, cancelled, revision-requested, expired, already decided, lacks approver authority, has expired evidence, or is invalidated by corrected context, when execution is requested, then execution is blocked with a metadata-only reason and existing approval/proposal records remain append-only and superseded rather than mutated. [Source: `_bmad-output/implementation-artifacts/4-5-approval-gate-and-ai-action-approval-surface-s3.md#Current State To Preserve`; `_bmad-output/implementation-artifacts/4-6-ai-action-preview-and-inspection.md#Architecture Guardrails`; `_bmad-output/planning-artifacts/epics.md#Story 4.9`]
7. Given S3/project conversation render the execution result, when tested with keyboard, screen reader semantics, reduced motion, forced colors, English/French text, and phone/tablet widths, then execution pending/succeeded/failed states remain reachable, status updates announce according to the UX matrix, focus does not jump while reading history, disabled or blocked actions have reachable explanations, and no text overlaps or relies on hover-only detail. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback matrix`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components`]
8. Given this story completes, when acceptance coverage runs, then tests prove allowlisted success, non-allowlisted fail-closed rejection, approval-state gating, idempotent duplicate handling, out-of-order projection safety, tenant isolation, leakage prevention, contract serialization/OpenAPI/generated client consistency if public contracts changed, architecture guardrails, and UI/accessibility behavior without implementing Story 4.8 refusal policy expansion or Story 4.9 correction invalidation beyond blocking stale/invalidated inputs already available. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.8`; `_bmad-output/planning-artifacts/epics.md#Story 4.9`; `_bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines`]

## Tasks / Subtasks

- [x] Define the approved AI-action execution contract and keep it separate from the existing spine allowlist (AC: 1, 2, 3, 8)
  - [x] Add the narrow command/event shapes needed for approved AI execution, preferably under `src/Hexalith.ChatBot.Contracts/Commands` and `src/Hexalith.ChatBot.Server/Governance/AiMediation`, following existing `ProposeAIAction`, `ExecuteLowRiskAIAssistance`, and `DecideAiActionApproval` metadata patterns.
  - [x] Represent the M0 AI-action command allowlist as a distinct server-side allowlist with exactly `Project.AppendConversationMessage`; do not overload `ChatBotSpineCommandAllowlist`, which is explicitly orthogonal to the AI-action execution allowlist.
  - [x] Carry only trusted metadata: tenant/project/requester/proposal/approval/source message/source conversation item/command name/allowlist version/policy snapshot/correlation/idempotency/action summary redaction. Do not accept client-provided tenant authority, approval truth, policy truth, or generated raw content as trust-bearing.
  - [x] If a public command/query/OpenAPI shape changes, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- [x] Gate execution against current approval, proposal, allowlist, and correction freshness state (AC: 1, 2, 5, 6, 8)
  - [x] Extend `GovernedOperationAggregate` and `GovernedOperationState` or a focused governance aggregate path so execution requires an existing approval request plus current `AiActionApprovalDecisionRecorded` with `DecisionKind.Approve`, expected approval source version, matching proposal/source message/project, non-expired evidence, and non-invalidated corrected context where known.
  - [x] Reject `Reject`, `RequestRevision`, `Cancel`, missing decision, duplicate conflicting decision, stale expected version, command mismatch, allowlist-version mismatch, unsupported command metadata, and invalidated context with structured `IRejectionEvent` payloads and stable reason codes.
  - [x] Keep business-rule violations as `DomainResult.Rejection([...])`; do not throw from aggregate `Handle` paths for expected approval/allowlist failures.
  - [x] Ensure duplicate equivalent execution returns no extra durable effect or the prior outcome according to the command execution idempotency rule; conflicting duplicate input must fail closed.
- [x] Dispatch the single M0 command through the existing gateway/EventStore spine (AC: 1, 3, 5, 8)
  - [x] Extend `AcceptedCommandDispatcher` only after the `CommandGateway` stages have admitted the typed execution command. Preserve the current stage order: auth, tenant-bind, authorize, risk-classify, approval-gate, coarse-idempotency, lifecycle validation, pre-commit audit, dispatch, post-commit audit.
  - [x] For `Project.AppendConversationMessage`, invoke the bounded Conversations integration through a ChatBot-owned adapter port such as `IConversationWriter` under `src/Hexalith.ChatBot.Server/Adapters/Conversations/`, or through the sibling EventStore command boundary if that is the established local pattern. Do not call Conversations from aggregate logic.
  - [x] Use PascalCase serialization when forwarding command payloads into EventStore, matching the existing dispatcher behavior required by `EventStoreAggregate`.
  - [x] Propagate `correlationId`, task/proposal/approval/operation IDs, actor type, source surface, idempotency metadata, and policy snapshot into audit/status/projection metadata.
  - [x] On dispatch/provider/dependency failure, release or complete coarse idempotency consistently with existing gateway semantics, queue the correct audit replay intent when needed, and return a catalog-backed redacted problem.
- [x] Record and project execution lifecycle outcomes (AC: 4, 5, 6, 8)
  - [x] Add events or projection translation for `execution-started`, `execution-succeeded`, `execution-failed`, and `outcome-recorded` if existing `PublishedAiOutcomeEvent`/`AiOutcomeEventView` fields cannot represent the execution without ambiguity.
  - [x] Reuse `AiOutcomeProjectionTranslator`, `AiOutcomeProjectionHandler`, `ProjectConversationItemView`, `BuildReviewHistory()`, `ChatBotAiOutcomeConversationItem.razor`, and `ChatBotConversationItemReviewHistory.razor` before creating parallel read models or UI components.
  - [x] Preserve append-only projection rows. Duplicate delivery must be idempotent, stale replay must not overwrite newer rows, and out-of-order execution events must still reconstruct by proposal/approval/operation/correlation identifiers.
  - [x] Keep generated AI content separate from source evidence. If generated detail is unavailable or redacted, render metadata-only reason codes instead of provider payload or raw command output.
- [x] Preserve S3/project conversation UX for pending, success, and failure states (AC: 4, 5, 7)
  - [x] Reuse the Story 4.6 preview and inspection UI: `ChatBotAiActionPreviewSections.razor`, `ChatBotApprovalConversationItem.razor`, `ChatBotAiOutcomeConversationItem.razor`, status summary, review history, evidence/risk chips, and blocked-state primitives.
  - [x] Add EN/FR localization only for missing execution labels, reason codes, safe next actions, retryability, and audit/projection pending text.
  - [x] Ensure current user's execution pending/success announces politely once, current user's rejected/blocked/terminal failure announces assertively, historical rows do not announce on initial load, and background updates expose a keyboard-reachable new-updates affordance rather than forced scroll.
  - [x] Maintain forced-colors, reduced-motion, mobile/tablet no-overlap, non-color status meaning, and reachable disabled explanations.
- [x] Add focused acceptance coverage (AC: all)
  - [x] Contract tests for any new command/event/query records, enum wire values, stable reason codes, safe metadata tokens, serialization round trips, OpenAPI/generated client hash if public schema changes, and message catalog entries.
  - [x] Aggregate tests for approved success, missing/rejected/revision/cancelled/expired/stale approval rejection, non-allowlisted command rejection, command/allowlist mismatch, correction-stale blocking, duplicate equivalent replay, and conflicting duplicate rejection.
  - [x] Gateway/dispatcher tests proving non-allowlisted AI execution fails before durable mutation, allowed M0 execution reaches the dispatcher only after admission, no sibling call occurs before pre-commit audit, EventStore payload casing is correct, and dependency failures are redacted.
  - [x] Projection tests for execution-started/succeeded/failed/outcome-recorded rows, out-of-order delivery, duplicate replay, stale replay, tenant isolation, lifecycle review-history reconstruction, and metadata-only leakage sentinels.
  - [x] Architecture/conformance tests proving UI/CLI/MCP/service/AI adapters cannot replicate `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, `IIdempotencyStore`, or AI-action allowlist logic, and rejection parity remains identical across surface shims except audited origin.
  - [x] UI/bUnit and E2E tests where needed for execution pending/success/failure rendering, disabled reasons, live-region behavior, focus, EN/FR labels, forced-colors, reduced-motion, phone/tablet no-overlap, and no sensitive strings in rendered markup.

## Dev Notes

### Scope Boundaries

- This story owns FR43 for M0: execute approved AI actions only through the versioned AI-action command allowlist, with M0 exactly `Project.AppendConversationMessage`.
- This story may add the narrow approved-execution command, execution lifecycle events, an AI-action execution allowlist, dispatcher/adapter wiring, projection mapping, UI labels/states, and tests.
- This story must not expand M1 command allowlist behavior, implement outbound email/draft/send, create task management, invoke arbitrary tools, build CLI/MCP production adapters, add a tenant policy editor, implement broad Story 4.8 refusal taxonomy, or complete Story 4.9 correction invalidation beyond blocking execution when stale/invalidated context is already represented.
- The existing `ChatBotSpineCommandAllowlist` is not the AI-action allowlist. It admits first-party ChatBot commands to the gateway. Story 4.7 needs the separate allowlist that governs what an approved AI action may execute.

### Existing Code To Reuse

- Gateway and admission:
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ISpineCommandAllowlist.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Status/IOperationStatusStore.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- AI mediation and approval:
  - `src/Hexalith.ChatBot.Contracts/Commands/ProposeAIAction.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/DecideAiActionApproval.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/ExecuteLowRiskAIAssistance.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionCommandMetadataProvider.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionApprovalEvents.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/LowRiskAiAssistanceExecutionEvents.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- Projection and UI:
  - `src/Hexalith.ChatBot.Server/Projections/PublishedAiOutcomeEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionHandler.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/AiOutcomeKind.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/AiOutcomeStatus.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor`
  - `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
  - `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
  - `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- Existing tests to extend:
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
  - `tests/Hexalith.ChatBot.Conformance.Tests/RejectionIntentParityTests.cs`
  - `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`

### Current State To Preserve

- Story 4.6 added `ChatBotAiActionPreviewSections.razor` and wired preview sections into approval and AI outcome rows. Reuse this metadata-only preview instead of adding a second execution preview model.
- `AiActionCommandMetadataProvider` already recognizes `Project.AppendConversationMessage` as approval-required with `ai-action-command-allowlist.m0`; keep naming/versioning consistent unless deliberately normalizing the older `ai-action-allowlist.m0` fixture strings in the same change.
- `DecideAiActionApproval` already records approved decisions with safe next action `execute-approved-ai-action`; execution should consume that state instead of inventing a separate approval truth.
- `CommandGateway` already fails closed on the spine allowlist before idempotency admission and dispatch, writes a redacted authorization-failure fact, and has regression tests proving no payload/tenant leakage.
- `AcceptedCommandDispatcher` already centralizes trusted enrichment and PascalCase EventStore payload forwarding. Add execution routing there or behind a focused helper rather than creating another dispatcher.
- `GovernedOperationAggregate` stores approval requests/decisions and low-risk execution IDs in `GovernedOperationState`. Extend this state carefully so append-only approval records remain immutable.
- `AiOutcomeProjectionTests` already cover append-only AI outcome rows, all outcome kinds, low-risk execution rows, out-of-order delivery, duplicate replay, stale replay, and lifecycle review-history identifiers. Build on these tests for approved command execution.
- Existing worktree has unrelated modified `Hexalith.Tenants` submodule state and `_bmad-output/story-automator/orchestration-4-20260601-145742.md`; do not revert or include them.

### Architecture Guardrails

- Every state mutation must route through `CommandGateway`; no UI/service/client/helper path may execute approved AI commands directly.
- `Project.AppendConversationMessage` is an AI-action command name, not necessarily a C# ChatBot command type. The implementation needs a typed ChatBot execution command that the gateway admits, then a server-side execution step that maps the approved AI action to the Conversations boundary.
- Keep dependency direction `Contracts <- Client <- Server` and UI consuming generated client/service models. Do not put Server-only governance interfaces into Contracts, Client, UI, CLI, or MCP.
- Aggregates remain pure: no I/O, Dapr, authorization, policy, logging, AI provider, or Conversations client calls inside `GovernedOperationAggregate.Handle`.
- Tenant identity and actor authority come from authenticated server context and EventStore envelopes. Request bodies may carry resource IDs for validation, but never as final tenant/authorization truth.
- Rejections are structured event payloads and user-visible text comes from the message catalog. Raw exception text, raw prompt/completion/provider payload, file content, raw email body, unrestricted policy data, tenant IDs in denial bodies, and secrets must not leak into API responses, projections, UI, logs, fixtures, or support artifacts.
- Use two-altitude idempotency: coarse gateway admission and fine aggregate/EventStore idempotency. For command execution, compose keys from tenant, command name, canonical command input hash, and requester as defined by the PRD addendum.
- Post-commit audit may reconcile later, but pre-commit audit is fail-closed. A path that cannot write pre-commit audit must not dispatch `Project.AppendConversationMessage`.
- Use repo-pinned stack only: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, System.Text.Json, xUnit v3, Shouldly, NSubstitute, bUnit/Playwright where needed, Fluent UI v5 RC through existing FrontComposer patterns. Do not add inline package versions or upgrade dependencies.
- Root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` if UI/browser behavior changed.
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Add Playwright only for behavior not provable by component/service tests, especially responsive no-overlap, forced-colors, reduced-motion, focus, and live-region behavior.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 4 is "Governed AI Action Mediation" and Story 4.7 follows Story 4.6 preview/inspection.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially glossary definitions, Functional Acceptance Guidance, FR43, NFR15a, FR81a, and the governed AI execution journey.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially Risk Classifier, Command Allowlist v0/v1, Shared Command Pipeline, Tenant Policy Schema, and Idempotency Keys.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway order, governed AI mediation, sibling integration rules, fail-closed/audit/idempotency guardrails, project structure, and testing standards.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`, especially AI Action Review, audit timeline, state-to-feedback matrix, focus/live-region behavior, and mobile/forced-colors constraints.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant rules: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, metadata-only diagnostics, tenant isolation, pure EventStore aggregates, Dapr duplicate/order tolerance, FrontComposer/Fluent UI inheritance, Shouldly/NSubstitute/xUnit patterns, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/4-6-ai-action-preview-and-inspection.md`; key carry-forward is to reuse existing preview/inspection metadata and lifecycle review history instead of creating parallel UI/projection models.
- Inspected current code and tests for likely update surfaces: `ChatBotSpineCommandAllowlist`, `CommandGateway`, `AiActionApprovalGate`, `AcceptedCommandDispatcher`, `AiActionCommandMetadataProvider`, approval/low-risk events, `GovernedOperationAggregate`, `GovernedOperationState`, `PublishedAiOutcomeEvent`, `AiOutcomeProjectionTranslator`, `ProjectConversationItemView`, approval/AI outcome UI components, gateway/dispatcher/aggregate/projection/conformance/E2E tests.
- Recent git history shows Story 4.6, 4.5, 4.4, 4.3, and 4.2 commits. The immediate baseline is `a8f1c37 feat(story-4.6): AI action preview and inspection`.
- Latest-technology web research was not required for story creation: this story adds no new external package, model, protocol, or framework and should use repo-pinned versions plus local code patterns.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.7 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - glossary, Functional Acceptance Guidance, FR43/NFR15a/FR81a, governed AI execution journey.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Risk Classifier, Command Allowlist v0/v1, Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, governed AI mediation, structure, sibling integration, fail-closed/audit/idempotency/testing guardrails.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - AI proposal panel, approval panel, audit timeline, blocked state, component rules.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - AI Action Review, governed AI execution flow, state-to-feedback matrix, focus/live-region behavior.
- `_bmad-output/implementation-artifacts/4-5-approval-gate-and-ai-action-approval-surface-s3.md` - approval gate implementation context and projection learning.
- `_bmad-output/implementation-artifacts/4-6-ai-action-preview-and-inspection.md` - previous story scope, preview/inspection implementation notes, validation evidence, and file list.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - current gateway stage order and fail-closed spine allowlist behavior.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` - current ChatBot spine allowlist, explicitly orthogonal to AI-action execution allowlist.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs` - trusted command enrichment and EventStore dispatch.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionCommandMetadataProvider.cs` - current M0 command metadata for `Project.AppendConversationMessage`.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - task-intent, proposal, low-risk execution, and approval decision aggregate behavior.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` - approval request/decision and low-risk execution state.
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs` - metadata-only AI outcome projection mapping.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - existing fail-closed allowlist and audit-origin coverage.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs` - existing dispatcher payload and enrichment coverage.
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs` - existing approval and low-risk execution aggregate coverage.
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs` - existing AI outcome lifecycle projection coverage.
- `tests/Hexalith.ChatBot.Conformance.Tests/RejectionIntentParityTests.cs` - existing rejection parity expectations.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 99 tests.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 15 tests.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 428 tests.
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - passed, 97 tests.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 35 tests.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 58 tests.
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - passed, 49 tests.

### Completion Notes List

- Added `ExecuteApprovedAIAction` and `ApprovedAiActionExecutionRecord` metadata-only contracts for approved AI action execution, with OpenAPI and generated client updates.
- Added a distinct server-side approved AI-action command allowlist for M0 containing only `Project.AppendConversationMessage`; the existing ChatBot spine allowlist remains separate and only admits the typed ChatBot command into the gateway.
- Extended gateway risk classification, authorization, idempotency, audit evidence, and dispatch handling so approved execution is admitted only through the existing CommandGateway path after pre-commit audit.
- Added a ChatBot-owned conversations adapter port and metadata-only M0 writer, then routed allowed approved execution through `AcceptedCommandDispatcher` before EventStore submission with PascalCase payloads.
- Extended `GovernedOperationAggregate` and state to require current approval request plus approved decision, matching proposal/source/project/allowlist metadata, fresh evidence, corrected context readiness, and equivalent replay idempotency.
- Added approved-execution lifecycle events and projection translation for execution-started, execution-succeeded/failed, and outcome-recorded AI outcome rows using existing project conversation projection models.
- Review auto-fix: changed the M0 conversations adapter path to prepare metadata-only append results before EventStore submission rather than implying a pre-persistence sibling mutation.
- Review auto-fix: wired EventStore-published approved execution domain events into the AI outcome projection endpoint, including success-to-outcome-recorded materialization and requester/source metadata preservation.
- Review auto-fix: aligned approved execution projection metadata with story expectations for `approved-ai-action-executed`, `wait-for-command-outcome`, and retryable `retry-later` failures.
- Preserved existing S3/project conversation UX by reusing current AI outcome/review-history surfaces and localization; no UI component changes were required.

### File List

- `_bmad-output/implementation-artifacts/4-7-allowlisted-command-execution.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/ExecuteApprovedAIAction.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ApprovedAiActionExecutionRecord.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Adapters/Conversations/ApprovedAiConversationAppendRequest.cs`
- `src/Hexalith.ChatBot.Server/Adapters/Conversations/ConversationAppendResult.cs`
- `src/Hexalith.ChatBot.Server/Adapters/Conversations/IConversationWriter.cs`
- `src/Hexalith.ChatBot.Server/Adapters/Conversations/MetadataOnlyConversationWriter.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/DeterministicAiActionRiskClassifier.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/ApprovedAiActionCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/ApprovedAiActionExecutionEvents.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/IApprovedAiActionCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionEndpoints.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAiActionExecutionEvent.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/TaskIntentContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/ApprovedAiActionCommandAllowlistTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Change Log

- 2026-06-01: Implemented allowlisted approved AI command execution for M0 and marked story ready for review.
- 2026-06-01: Senior developer review auto-fixed approved-execution adapter semantics, runtime projection wiring, lifecycle metadata alignment, and marked story done.

## Senior Developer Review (AI)

### Review Outcome

Approved after auto-fix. Story claims were validated against the implementation, changed files, and acceptance coverage. No critical issues remain.

### Findings Fixed

- HIGH: Approved execution dispatch prepared the EventStore payload by calling an `AppendConversationMessageAsync` adapter before the aggregate approval/proposal gates could reject. Fixed by making the M0 adapter path explicitly prepare metadata-only append results before EventStore submission.
- HIGH: Approved execution projection was only covered through direct translator calls; the runtime Dapr/EventStore projection endpoint could not consume approved execution domain events. Fixed by adding `PublishedAiActionExecutionEvent`, endpoint dispatch from raw JSON, and handler support for started/succeeded/failed approved execution events.
- HIGH: Terminal approved execution events did not carry project/requester/source metadata needed by runtime projection. Fixed by extending succeeded/failed events with project, requester, source message, and optional source conversation item metadata.
- MEDIUM: Projection metadata used `approved-command-executed` and `none` for started next action, diverging from story/UI expectations. Fixed to emit `approved-ai-action-executed` and `wait-for-command-outcome`.
- MEDIUM: Retryable approved execution failures could not use `retry-later` because aggregate validation only accepted `review-ai-action`. Fixed by allowing `retry-later` for failed approved execution records.
- MEDIUM: `ProjectConversationE2ETests.cs` was changed but missing from the story File List. Fixed by adding it.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 99 tests.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 15 tests.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 428 tests.
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - passed, 97 tests.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 35 tests.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 58 tests.
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - passed, 49 tests.
