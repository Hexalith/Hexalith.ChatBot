---
baseline_commit: 098cd9b
---

# Story 4.5: Approval gate and AI action approval surface (S3)

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized approver,
I want risky AI actions paused for review with a complete preview,
so that nothing risky executes until I approve it.

## Acceptance Criteria

1. Given an AI action proposal classified as `approval-required`, or containing any of the six risky action classes (`modifies-state`, `exposes-files`, `sends-external`, `creates-tasks`, `invokes-tools`, `acts-on-behalf`), when the action is proposed or routed from low-risk policy evaluation, then the system creates or preserves a durable pending approval request and does not execute the action before a permitted approval decision succeeds. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.5; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR41]
2. Given the AI Action Review surface (S3), when an authorized user opens a pending action, then it displays command name, current allowlist version, input files as tappable evidence references with redaction state, proposed recipients, sender-authority class, risk classification, the risk-producing input tuple, policy snapshot ID, expected post-state metadata, and decisions `approve`, `reject`, `request-revision`, and `cancel`. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.5; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR42]
3. Given the current actor lacks authority for the action risk class, when S3 renders, then `approve` is unavailable with a stable reason code and user-safe reason string; the unavailable state remains keyboard reachable and screen-reader announced via `aria-disabled="true"` or an adjacent focusable explanation. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.5; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]
4. Given surfaced evidence references exist, when S3 renders, then every evidence reference has a visible freshness chip (`fresh`, `stale`, or `expired`) and the chip count equals the evidence-reference count. `stale` is allowed with a warning; `expired` disables `approve` with reason `evidence-expired`. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR48]
5. Given an approver chooses `approve`, `reject`, `request-revision`, or `cancel`, when the decision is submitted, then the decision passes through the shared CommandGateway spine, records a durable approval decision event with actor, decision, rationale redaction state, policy snapshot, source evidence, idempotency key, correlation ID, and audit status, and projects the updated approval row into the project conversation. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR42; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR50]
6. Given a repeated equivalent approval decision is submitted within the replay window, when idempotency evaluates it, then the system returns the same observable decision outcome without duplicating approval, audit, projection, or downstream execution intent; a conflicting second decision is rejected with a user-safe, audited conflict. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR13-NFR14]
7. Given approval succeeds, when this story completes, then the system records approval state and a safe next action for later allowlisted execution; it must not execute `Project.AppendConversationMessage` or any other AI action command in this story. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.7; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Command-Allowlist-v0]
8. Given approval, rejection, revision request, cancellation, blocked authority, expired evidence, audit unavailable, projection pending, and AI provider outage scenarios, when tests run, then contract, gateway, aggregate/projection, UI/component, accessibility, idempotency, audit, leakage, and outage coverage prove the approval gate and S3 surface behavior without raw prompt, provider payload, file content, unauthorized evidence, secrets, tenant IDs in denial bodies, or raw exceptions leaking. [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR22-NFR40-NFR60-NFR65]

## Tasks / Subtasks

- [x] Define approval request and decision contracts without duplicating proposal models (AC: 1, 2, 5, 6, 8)
  - [x] Add purpose-named public contracts under `src/Hexalith.ChatBot.Contracts/` for approval request/decision only if the existing `ProjectConversationItem` approval fields are insufficient. Candidate command names: `RequestAiActionApproval` for a durable request and `DecideAiActionApproval` for decisions.
  - [x] Reuse `AiActionProposalRecord`, `AiActionRiskClassificationRecord`, `ApprovalDecisionKind`, `ApprovalEventKind`, `ApprovalStatus`, and `ApprovalEvidenceFreshness`; do not create a second AI proposal DTO.
  - [x] Decision command must include approval/proposal identity, decision kind, expected approval source version, redaction-stamped rationale metadata, idempotency key or command ID, and correlation context. Tenant ID, actor identity, authority, and final authorization must come from server context, not request body.
  - [x] If public command/query shape changes, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- [x] Materialize pending approval requests from proposal/routing paths (AC: 1, 2, 4, 7, 8)
  - [x] When `ProposeAIAction` persists an `approval-required` proposal, create a durable pending approval request event that projects into S3 and links to the proposal. Preserve the existing proposal row and `safeNextAction = review-ai-action`.
  - [x] When Story 4.4 policy evaluation returns `RoutedToApproval`, preserve the routed record and link it to the same approval request path instead of treating routing as execution success.
  - [x] Populate request metadata from trusted server/projection data: command name, allowlist version, AI risk class/action classes, risk input tuple, policy snapshot, requester, source message/conversation item, evidence refs, affected resources, recipient refs, sender-authority class, expected post-state redaction state, action-summary redaction state, source version, schema version, and correlation ID.
  - [x] Evidence freshness must be derived from server-side snapshot metadata using UTC timestamps. Missing, malformed, or expired freshness blocks approval; stale is allowed but flagged.
  - [x] Do not call AI providers, Folders content APIs, M365, command executors, or sibling service command clients while creating an approval request.
- [x] Implement decision recording through the CommandGateway spine (AC: 3, 5, 6, 8)
  - [x] Add a gateway-admitted decision command and route it through the existing order: `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> pre-commit-audit -> dispatch`.
  - [x] Extend `AiActionApprovalGate` or add an approval-specific authority evaluator so `approve` requires authority for the risk class/action class, evidence freshness is not expired, approval is pending, audit readiness is healthy, and policy snapshot is trusted.
  - [x] `reject`, `request-revision`, and `cancel` still require project/review authority and audit readiness, but must not require command-execution authority.
  - [x] Extend `CoarseIdempotencyComposer` for approval decisions using the addendum contract: `tenant_id + ai_action_id + decision_actor + decision_kind`, 24h window, same decision replay, conflicting decision rejection.
  - [x] Enrich `AuditEnvelopeFactory` with approval decision metadata: approval ID, proposal ID, decision kind, risk class/action classes, policy snapshot ID, evidence refs/freshness, authority result, disabled reason when present, redaction decisions, and resulting safe next action. Keep audit refs metadata-only.
- [x] Persist approval state in the governed operation aggregate (AC: 1, 5, 6, 7, 8)
  - [x] Add append-only approval events under `src/Hexalith.ChatBot.Server/Governance/AiMediation/` or a focused `Governance/Approval/` folder, for example `AiActionApprovalRequested` and `AiActionApprovalDecisionRecorded`.
  - [x] Extend `GovernedOperationAggregate.Handle(...)` and `GovernedOperationState` only as needed to validate pending state, expected source version, proposal linkage, decision actor, decision kind, authority result, evidence freshness, and idempotency.
  - [x] Aggregate logic stays pure: no authorization lookups, no policy reads, no Dapr, no AI/provider calls, no audit writes, no projection reads, no async.
  - [x] Approved state records permission for later Story 4.7 execution and safe next action such as `execute-approved-ai-action`; it must not dispatch or execute the allowlisted command in this story.
  - [x] Rejection, revision request, and cancellation are terminal for the current approval record unless future policy explicitly creates a new superseding approval. Preserve original records with `supersedes`/`superseded_by` links when later stories add revision/correction flows.
- [x] Project approval requests and decisions into project conversation and S3 (AC: 2, 3, 4, 5, 8)
  - [x] Reuse `PublishedApprovalEvent`, `ApprovalProjectionTranslator`, `ApprovalEventView`, `IProjectConversationProjectionStore.UpsertApprovalEventAsync`, and `ProjectConversationItemView.FromApprovalEvent`.
  - [x] Keep projection order-tolerant: decision/outcome events that arrive before the request must later be enriched by the request context through `ApprovalEventView.WithRequestContext`.
  - [x] Ensure `ProjectConversationItem` and `ProjectConversationItemModel` carry all required FR42 fields, including AI risk class/action classes, risk input tuple, and per-evidence freshness/redaction. Add only additive fields if an existing field cannot represent the data safely.
  - [x] Do not lossy-map AI action risk (`low-risk`, `approval-required`, and the six action-class tokens) into the older generic `RiskClass` values. If the current approval projection cannot carry the AI-specific tokens, add explicit approval AI-risk fields and map display labels at the UI boundary.
  - [x] Reuse `ChatBotApprovalConversationItem`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotConversationItemStatusSummary`, `ChatBotConversationItemReviewHistory`, localization, and existing conversation stream routing. Do not create a new visual language or nested-card approval panel.
  - [x] Add S3 action controls to the approval item or a focused child component using familiar buttons/icons where available. Disabled approve must be focusable with `aria-disabled="true"` or have an adjacent focusable reason affordance. Tooltip-only disabled reasons are not acceptable.
  - [x] Add EN/FR localization keys for approval blocked by authority, evidence expired, stale evidence warning, approval recorded, rejected, revision requested, cancelled, duplicate decision, conflicting decision, audit unavailable, and projection pending if missing.
- [x] Preserve frontend UX, accessibility, and responsive behavior (AC: 2, 3, 4, 8)
  - [x] S3 must keep project context, proposal metadata, evidence, risk, policy, expected post-state, and controls visible as one review unit.
  - [x] Evidence/risk chips must use text labels and semantic status, not color alone. Per-evidence chips must be tappable/keyboard-activatable when evidence is permitted and must explain redaction when detail is restricted.
  - [x] Approval, rejection, revision, and cancellation flows move focus to success status or error summary. Blocked actions keep focus in the review panel with the reason reachable.
  - [x] Live-region behavior follows UX rules: current user's rejected/blocked decision is assertive; projection pending/partial success is polite; historical rows do not announce on initial load.
  - [x] Phone/tablet layout preserves read-only summary, risk/state/actor/next action, and safe approve/reject/revise/cancel controls with at least WCAG 2.2 AA target sizing.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract/OpenAPI/generated-client tests for decision wire tokens, approval fields, required metadata, source-version/idempotency behavior, and absence of tenant/authority in request bodies.
  - [x] Gateway tests proving approval decisions run through the shared spine, authority failures disable/block approval, expired evidence blocks approval, audit unavailable fails closed before durable state, and non-AI paths keep existing behavior.
  - [x] Aggregate tests proving request creation, decision recording, same-decision replay, conflicting decision rejection, terminal state handling, source-version validation, and append-only/supersession behavior.
  - [x] Projection tests proving request/decision/outcome order tolerance, request-context enrichment, safe policy/audit redaction, per-evidence freshness chip count, and project/tenant partitioning.
  - [x] UI/service/component tests proving S3 renders FR42 fields, disabled approve reason, keyboard focus, live-region state, EN/FR labels, stale/expired freshness, and responsive no-overlap behavior.
  - [x] Leakage/isolation tests proving prompts, provider payloads, generated content not explicitly redaction-stamped, file contents/paths, unauthorized evidence, raw email bodies, tenant IDs in denial bodies, secrets, raw exceptions, and restricted policy/audit details do not appear in audit envelopes, logs, projections, UI rows, fixtures, or support artifacts.
  - [x] Outage tests proving approval review/decision and audit lookup do not require live AI provider availability.

## Dev Notes

### Scope Boundaries

- This story owns FR41/FR42 for M0: pending risky AI approval requests, S3 review, authority/freshness blocking, and durable decisions.
- This story may add approval request/decision commands, approval events, aggregate state, gateway authority/freshness approval logic, coarse idempotency for approval decisions, audit metadata, projection/UI mapping, S3 controls, localization, and tests.
- This story must not implement Story 4.6 deep preview/inspection beyond the FR42 fields required on S3, Story 4.7 allowlisted command execution, Story 4.8 refusal/block behavior beyond approval-specific blocks, Story 4.9 correction invalidation, outbound send, CLI/MCP parity, tenant policy editor UI, live AI provider integration, or any new command outside the M0 approval path.
- Approval is a gate and a durable decision, not execution. `approve` makes later execution eligible; it does not execute `Project.AppendConversationMessage`.

### Existing Code To Reuse

- Proposal/risk/low-risk routing:
  - `src/Hexalith.ChatBot.Contracts/Commands/ProposeAIAction.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/ExecuteLowRiskAIAssistance.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/AiActionProposalRecord.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/AiActionRiskClassificationRecord.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/DeterministicAiActionRiskClassifier.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotApprovalResult.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- Approval projection/UI scaffolding already exists and should be extended:
  - `src/Hexalith.ChatBot.Contracts/Enums/ApprovalDecisionKind.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ApprovalEventKind.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ApprovalStatus.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ApprovalEvidenceFreshness.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
  - `src/Hexalith.ChatBot.Server/Projections/PublishedApprovalEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs`
  - `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
  - `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- Shared gateway/audit/idempotency seams:
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotGatewayContext.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`

### Current State To Preserve

- Story 4.3 made AI risk classification deterministic and fail-closed. Do not trust caller-supplied command metadata, risk metadata, authority, or policy as final authority.
- Story 4.4 replaced pass-through approval behavior for `ExecuteLowRiskAIAssistance` and introduced `AllowedLowRiskExecution`, `RoutedToApproval`, and `Blocked`. Preserve low-risk allowed execution and policy-false routing; connect routed approval to the S3 approval request path.
- Existing approval projection/UI shapes are metadata-only and already include many FR42 fields. Extend these instead of building a parallel approval read model.
- Existing approval projection fields include generic `RiskClass? ApprovalRiskClass`; Story 4.5 must preserve AI-specific `approval-required` and action-class wire tokens. Add explicit additive fields if needed rather than forcing a lossy generic risk conversion.
- `PublishedApprovalEvent` / `ApprovalEventView.WithRequestContext` already support order-tolerant request/decision enrichment. Keep that behavior for out-of-order projection delivery.
- `Project.AppendConversationMessage` is the single M0 AI action command and remains approval-required by default. Do not execute it in this story.
- Existing worktree has unrelated modified `_bmad-output/story-automator/orchestration-4-20260601-145742.md`; do not revert or include it.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. Public contracts live in `.Contracts`; gateway, approval authority/freshness checks, aggregate events, projections, audit metadata, and policy logic live in `.Server`; UI consumes generated client contracts and service models.
- Every state-mutating approval decision must enter through `CommandGateway`. UI components and services must not authorize, classify risk, evaluate approval authority, write audit, or persist decisions directly.
- Governance stage interfaces remain internal to `.Server`; architecture tests must continue to reject UI/CLI/MCP references to `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, and `IIdempotencyStore`.
- Aggregate `Handle` methods stay pure and return rejections for business failures. No provider calls, network calls, Dapr, Folders reads, authorization, policy lookup, logging, or async inside aggregate logic.
- Tenant ID and actor identity come from authenticated server context and command envelope. Do not put tenant authority, actor roles, or final approval authority in client-submitted bodies.
- Use repo-pinned stack only: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, Dapr/Aspire pins, Fluent UI/FrontComposer inheritance, xUnit v3, Shouldly, and NSubstitute. Do not add package versions inline or casually upgrade dependencies.
- Logs, traces, audit envelopes, support artifacts, fixtures, UI rows, and error bodies are metadata-only unless a public display field is explicitly redaction-stamped and policy-approved. Prompt, completion, provider payload, file-content, raw email, unauthorized evidence, and raw exception leakage is release-blocking.
- Root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - Targeted compiled xUnit v3 runners for Contracts/OpenAPI/Client generation, Server gateway/approval/aggregate/projection/idempotency/audit, UI service/component, Architecture, Conformance/isolation, and outage/leakage tests with `-parallel none`.
- Sandbox note inherited from previous stories: `dotnet test` via VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Add Playwright UI E2E only if S3 interactive controls or responsive layout cannot be proven by component/service tests. Use accessible roles/labels or stable data attributes, not CSS selectors or sleeps.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 4 is "Governed AI Action Mediation" and Story 4.5 owns approval gate/S3 between Story 4.4 low-risk routing and Story 4.7 allowlisted execution.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR41-FR46, FR81a, NFR13-NFR16, NFR22, NFR34, NFR40, NFR46-NFR48, NFR50, NFR60-NFR65.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially Command Allowlist v0, Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys, and risk-classifier reviewer-disagreement rules.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway order, governed AI mediation, project structure, fail-closed/audit/idempotency guardrails, internal stage interfaces, and testing standards.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`, especially AI Action Review, approval panel/controls, evidence/risk chips, disabled-control explanation, live-region behavior, keyboard/focus, responsive/touch, and EN/FR localization constraints.
- Loaded persistent project-context facts from sibling module `project-context.md` files. Relevant rules: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, root-level submodules only, pure EventStore aggregate handlers, metadata-only diagnostics, tenant isolation, Dapr duplicate/order tolerance, and FrontComposer/Fluent UI inheritance.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/4-4-low-risk-ai-assistance-execution.md` and `_bmad-output/implementation-artifacts/4-3-ai-action-risk-classification.md`.
- Inspected current code to confirm extension points: `AiActionApprovalGate`, `ChatBotApprovalResult`, `AcceptedCommandDispatcher`, `GovernedOperationAggregate`, `GovernedOperationState`, `PublishedApprovalEvent`, `ApprovalProjectionTranslator`, `ApprovalEventView`, `InMemoryProjectConversationProjectionStore`, `ProjectConversationItemView`, `ProjectConversationItem`, `ChatBotApprovalConversationItem`, `ProjectConversationService`, and relevant tests.
- Latest-technology research not required for story creation: no external provider, cloud API, third-party framework, or package upgrade is selected by this story. Implementation should use repo-pinned versions and local server-owned seams.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.5 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR41-FR46, FR81a, NFR13-NFR16, NFR22, NFR40, NFR46-NFR48, NFR50, NFR60-NFR65.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Command Allowlist v0, Shared Command Pipeline, Idempotency Keys, Tenant Policy Schema.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, Governed AI Mediation, project structure, fail-closed/audit/idempotency/testing guardrails.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - approval panel/controls, evidence/risk chips, visual semantics.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - AI Action Review, interaction states, disabled-control accessibility, focus/live-region behavior, responsive constraints.
- `_bmad-output/implementation-artifacts/4-4-low-risk-ai-assistance-execution.md` - previous-story routing and approval-gate result changes.
- `_bmad-output/implementation-artifacts/4-3-ai-action-risk-classification.md` - classifier metadata, code reuse list, and review fixes.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs` - current low-risk policy approval gate to extend.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotApprovalResult.cs` - current approval result shape.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs` - dispatch extension point for admitted commands.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - pure aggregate to validate/persist approval events.
- `src/Hexalith.ChatBot.Server/Projections/PublishedApprovalEvent.cs` - approval projection input shape.
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs` - approval event translator.
- `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs` - order-tolerant approval view and request context enrichment.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` - approval conversation item materialization.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor` - existing S3 approval row to reuse/extend.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- 2026-06-01: `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 97 tests.
- 2026-06-01: `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 15 tests.
- 2026-06-01: `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 414 tests.
- 2026-06-01: `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - passed, 94 tests.
- 2026-06-01: `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 35 tests.
- 2026-06-01: `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 58 tests.
- 2026-06-01: Review auto-fix validation `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- 2026-06-01: Review auto-fix validation `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 415 tests.
- 2026-06-01: Review auto-fix validation `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 97 tests.
- 2026-06-01: Review auto-fix validation `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - passed, 95 tests.
- 2026-06-01: Review auto-fix validation `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 35 tests.
- 2026-06-01: Review auto-fix validation `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 58 tests.

### Completion Notes List

- Added the metadata-only `DecideAiActionApproval` public contract, OpenAPI schema, generated client surface, and generated-client hash fixture.
- Materialized durable `AiActionApprovalRequested` events from proposal and low-risk routed-to-approval paths without executing downstream AI action commands.
- Added pure aggregate decision handling for approve/reject/request-revision/cancel, including expected source-version checks, expired-evidence blocking for approve, terminal-state handling, and safe next actions for later execution.
- Routed approval decisions through the shared CommandGateway spine with authority gating, coarse idempotency composition, audit metadata, and dispatch into governed operation state.
- Extended approval projections and the project conversation read model with AI-specific risk class/action/input tuple fields while preserving existing generic approval fields.
- Extended the S3 approval conversation item with FR42 metadata, per-evidence freshness chips, accessible decision controls, localized disabled reasons, and service submission through the generated client.
- Review auto-fix: added chatbot-domain approval projection ingestion for aggregate `AiActionApprovalRequested` and `AiActionApprovalDecisionRecorded` events, including DAPR endpoint registration, DI registration, request/decision translation, and regression coverage.

### Change Log

- 2026-06-01: Implemented Story 4.5 approval request/decision contracts, gateway admission, aggregate persistence, audit/idempotency metadata, projection/UI S3 surface, localization, generated client updates, and focused validation coverage.
- 2026-06-01: Senior Developer Review auto-fix added aggregate approval-event projection into S3 and set story status to done after green validation.

### File List

- `_bmad-output/implementation-artifacts/4-5-approval-gate-and-ai-action-approval-surface-s3.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Contracts/Commands/DecideAiActionApproval.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotApprovalResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionApprovalEvents.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionEndpoints.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAiActionApprovalEvent.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedApprovalEvent.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

## Senior Developer Review (AI)

### Review Date

2026-06-01

### Reviewer

GPT-5 Codex

### Findings

- [x] [HIGH] Durable aggregate approval events were not wired into the S3/project-conversation projection path. `AiActionApprovalRequested` and `AiActionApprovalDecisionRecorded` were persisted by the aggregate, but only the synthetic `PublishedApprovalEvent` shape was projected, so approval requests/decisions produced by the real chatbot EventStore domain could fail to appear in S3. Fixed by adding chatbot-domain approval projection ingestion, endpoint and DI registration, and regression coverage. Evidence: `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs`, `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionEndpoints.cs`, `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`.

### Review Notes

- File List and git-changed files were cross-checked. New review-added files are now listed above.
- Acceptance criteria and completed tasks were re-checked against contract, gateway, aggregate, projection, UI/service, localization, and validation coverage.
- No CRITICAL issues remain after the auto-fix. Re-examined for additional verified issues per workflow; no further actionable defects were confirmed.

### Outcome

Approved after auto-fix. Story status set to `done`.
