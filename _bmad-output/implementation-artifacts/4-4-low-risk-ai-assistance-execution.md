---
baseline_commit: 5708167
---

# Story 4.4: Low-risk AI assistance execution

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want low-risk read-only AI assistance to run when policy allows,
so that I get help without unnecessary approval friction.

## Acceptance Criteria

1. Given an AI action proposal classified as `low-risk`, when the authenticated actor has explicit project authorization, the proposal references a valid scoped `ProjectAiContextPackage`, audit readiness is healthy, and the trusted tenant policy snapshot permits low-risk assistance for the proposal class/effect surface, then the system executes the assistance without human approval and records metadata-only AI outcome history for start, success, or failure. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.4; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR40; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR8-NFR9]
2. Given execution begins, when the AI assistance provider is invoked, then the provider receives only tenant/project-scoped, policy-authorized, redaction-aware context assembled from `ProjectAiContextPackage` plus permitted metadata/source evidence references; it must not receive cross-tenant data, unauthorized file references, raw paths, provider payloads, secrets, or content excluded by the package. [Source: _bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md#Acceptance-Criteria; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR9]
3. Given tenant policy `ai-action.low-risk-allowed` is unset, false, missing, stale, invalid, unavailable, or does not permit the proposal class/effect surface, when the low-risk action is requested, then the system does not call the AI provider and routes the proposal to approval with `safeNextAction = review-ai-action`, preserving the proposal and policy reason for Story 4.5. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.4; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Tenant-Policy-Schema]
4. Given a proposal is `approval-required`, rejected/unsupported by classification, mixed with any risky class, missing classification metadata, missing package metadata, has stale corrected context, or lacks project authorization, when execution is evaluated, then it is not executed as low-risk; it is routed to approval or refused with a catalog-backed redacted problem according to the existing classifier/refusal semantics. [Source: _bmad-output/implementation-artifacts/4-3-ai-action-risk-classification.md#Current-State-To-Preserve; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR41-FR46]
5. Given AI provider execution succeeds, when the result is projected into the project conversation, then the AI outcome row distinguishes generated assistance from source evidence, carries provenance (`provider/model version`, generated-at UTC, source evidence IDs), risk metadata, policy snapshot ID, context package ID/version/redaction state, authorized/excluded context references, audit operation ID/status, correlation ID, and a safe next action of `none`. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR27; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Conversation-and-audit-semantics]
6. Given AI provider execution fails, times out, is disabled, or returns unsafe/overbroad output, when the failure is handled, then the system records a retryable or terminal AI outcome state with user-safe message-catalog reason, no raw provider payload, no prompt/completion leakage, and no durable project mutation beyond audited ChatBot outcome state. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR17-NFR22; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR40]
7. Given equivalent execution requests are replayed, when coarse and aggregate idempotency evaluate them, then the AI provider is not called twice for the same logical low-risk assistance request and the caller observes the same operation/outcome identity. A conflicting replay returns the existing idempotency conflict behavior. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR13-NFR14]
8. Contract, gateway, policy, context packaging, provider-port, aggregate/projection, UI/service, audit, idempotency, outage, and cross-tenant leakage tests prove the low-risk execution path and the policy-false approval-routing path. [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]

## Tasks / Subtasks

- [x] Add explicit low-risk assistance execution contracts (AC: 1, 5, 7, 8)
  - [x] Add purpose-named public contracts under `src/Hexalith.ChatBot.Contracts/` for the execution request/result, for example `ExecuteLowRiskAIAssistance`, `LowRiskAiAssistanceExecutionRecord`, and a stable assistance-kind enum/string set for M0 read-only help such as summarizing already-associated conversation context or explaining visible evidence.
  - [x] Keep execution additive to the existing `AiActionProposalRecord`; do not create a second proposal model. Reference the existing proposal ID, task intent ID, source message ID, context package ID/version, risk classification, policy snapshot ID, expected proposal source version, correlation ID, and transition/execution ID.
  - [x] If a generated assistance text field is added to the query contract, keep it bounded, redaction-stamped, provenance-linked, and distinct from source evidence. Do not add prompt, provider payload, hidden chain-of-thought, raw file content, local path, mailbox body dump, or unrestricted model output fields.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` if any public command/query shape changes.
- [x] Implement trusted low-risk policy evaluation (AC: 1, 3, 4, 8)
  - [x] Add a server-owned policy evaluator under `src/Hexalith.ChatBot.Server/Governance/AiMediation/` or `.../Governance/Approval/`, for example `IAiActionPolicyEvaluator`, that consumes trusted server/projection/policy snapshot data. It must not trust client-supplied `TenantPolicyClassification`, `CommandDefaultRisk`, `CommandMetadataSupported`, or `RiskClassification` as final authority.
  - [x] Preserve the safe default: missing, invalid, unavailable, stale, or false `ai-action.low-risk-allowed` means **route to approval**, not execute.
  - [x] Treat `approval-required`, unsupported, rejected, mixed risky-class, unknown command/effect surface, missing project authorization, or missing context package as non-executable through this path.
  - [x] Record a policy snapshot ID and reason code for both `low-risk-execute-allowed` and `low-risk-routed-to-approval`; the reason code must be stable and metadata-only.
  - [x] Do not weaken Story 4.3's fail-closed classifier. If classifier changes are needed to represent read-only M0 assistance, make them deterministic, metadata-only, and covered by regression tests.
- [x] Replace pass-through approval behavior for AI proposal/execution decisions (AC: 1, 3, 4, 8)
  - [x] Extend `ChatBotApprovalResult` beyond the current singleton approved result so the gateway can distinguish `AllowedLowRiskExecution`, `RoutedToApproval`, and `Blocked/Rejected` with a stable reason.
  - [x] Replace or wrap `PassThroughApprovalGate` for AI action proposals/executions while preserving pass-through behavior for unrelated non-AI commands until their stories require approval.
  - [x] Ensure `CommandGateway` uses the approval result before idempotency/audit/dispatch when the result is a refusal or approval route, and that it never dispatches low-risk execution when the policy gate returned approval-required.
  - [x] Preserve the existing gateway order: `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> pre-commit-audit -> dispatch`.
  - [x] Enrich `AuditEnvelopeFactory` source evidence refs with low-risk policy decision, policy snapshot, context package ID/version, execution ID, provider-port decision, and risk metadata; never include prompt or completion text in audit refs.
- [x] Add the AI assistance provider boundary without choosing a vendor package (AC: 1, 2, 6, 8)
  - [x] Add `src/Hexalith.ChatBot.Server/Adapters/AiProvider/` with an internal provider port such as `IAiAssistanceProvider`. The request object should contain only scoped context package metadata, permitted source evidence references, redaction/retention/provider-reuse settings, assistance kind, requester/project IDs, policy snapshot ID, and correlation ID.
  - [x] Provide a deterministic disabled/unavailable provider implementation for local/tests that returns a typed failure or deterministic synthetic summary. Do not add OpenAI, Azure OpenAI, Graph, or other vendor SDK dependencies in this story unless an approved provider ADR/config already exists.
  - [x] If live provider invocation is enabled by configuration, require A5 policy controls before tenant data leaves the boundary: provider reuse/training disabled unless explicitly configured, tenant-approved region/retention, metadata-only logging, secret redaction, timeout, and cancellation support.
  - [x] Do not call the provider from aggregate `Handle` methods, UI, Client, projections, CLI, or MCP. Provider invocation belongs in server orchestration behind the gateway/dispatcher boundary.
  - [x] AI provider outage must affect only the low-risk assistance operation and must not break association review, existing approval review, audit reads, retry workflows, or project conversation reads.
- [x] Execute low-risk assistance through a durable ChatBot outcome path (AC: 1, 5, 6, 7, 8)
  - [x] Add aggregate event(s) under `src/Hexalith.ChatBot.Server/Governance/AiMediation/` for low-risk execution start/success/failure, or extend existing AI outcome event flow if that is the established path. Events must be append-only, tenant/project scoped, metadata-only, and serialization-tolerant.
  - [x] Extend `GovernedOperationAggregate.Handle(...)` and `GovernedOperationState` only as needed to validate execution identity, expected source version, proposal linkage, package linkage, policy decision, and idempotency. Aggregate logic stays pure and must not perform provider calls, authorization, policy lookup, or projection reads.
  - [x] Ensure same-key replay does not call the provider again. Prefer recording provider execution under an execution ID before/with durable outcome so retries can return the prior operation identity.
  - [x] Keep M0 AI action command allowlist semantics separate: `Project.AppendConversationMessage` remains approval-required by default and is not executed in this story. Low-risk assistance records a ChatBot AI outcome, not an unapproved project-state mutation.
  - [x] Route policy-false or approval-required proposals to the existing proposal/projection path with `safeNextAction = review-ai-action`; Story 4.5 owns approval decisions.
- [x] Consume the scoped AI context package safely (AC: 1, 2, 4, 8)
  - [x] Reuse `ProjectAiContextPackage` and `DefaultProjectAiContextPackageAssembler`; do not assemble a second context package shape and do not bypass package exclusions.
  - [x] Validate package tenant/project, `PackageId`, `PackageVersion`, `PolicySnapshotId`, `RedactionDecision`, `RetentionClass`, `ProviderReuseSetting`, `SourceEvidenceReferences`, and source version before provider invocation.
  - [x] Exclude files with `pending-scan`, `unsafe`, `rejected`, `failed`, `unavailable`, `retryable`, `redacted`, `unauthorized`, `policy-denied`, or `not-yet-eligible`; do not hydrate them later through Folders or mailbox side channels.
  - [x] If file content hydration is introduced, add a separate authorized content port and prove it reads only files included in the package. Do not extend the existing `IFolderStore.StoreMailboxAttachmentAsync` storage port into a read/download API unless Folders already exposes the correct governed read contract.
  - [x] Block execution while corrected context is stale or invalidation is in progress; reuse Story 4.1/3.14 correction-readiness rules instead of re-checking raw association state.
- [x] Project and render low-risk AI outcome state using existing S1/S3 primitives (AC: 5, 6, 8)
  - [x] Extend `PublishedAiOutcomeEvent`, `AiOutcomeEventView`, `AiOutcomeProjectionTranslator`, `ProjectConversationItemView`, `ProjectConversationItem`, generated-client mapping, and `ProjectConversationService` only as needed to expose low-risk execution metadata and generated assistance safely.
  - [x] Reuse `ChatBotAiOutcomeConversationItem`, `ChatBotRiskChip`, `ChatBotEvidenceChip`, `ChatBotConversationItemStatusSummary`, localization, and current AI-summary/source-evidence distinction. Do not invent a new visual system or build Story 4.5 approval controls here.
  - [x] Success rows use `AiOutcomeKind.ExecutionSucceeded` or `OutcomeRecorded` with `AiOutcomeStatus.Succeeded`; failures use `ExecutionFailed`/`Failed` or `Refusal`/`Blocked` as appropriate. Preserve append-only history: proposal, route/decision, execution started, succeeded/failed remain distinct rows when events arrive out of order.
  - [x] Generated assistance must be labelled as AI-generated, carry provenance, and remain collapsible or clearly separated from source evidence per FR27. Source evidence remains the default inspection path.
  - [x] Add EN/FR localization keys for low-risk executed, routed to approval, provider unavailable, unsafe output blocked, and context package unavailable if not already covered by the message/UI catalogs.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract/OpenAPI/generated-client tests for new command/result fields, exact risk/status wire values, absence of tenant authority in request bodies, and no raw prompt/provider/file-content fields.
  - [x] Policy evaluator tests for allowed, false, missing, invalid, stale, unavailable, class mismatch, approval-required, unsupported, mixed-risk, missing package, and missing project authorization cases.
  - [x] Gateway tests proving approval gate behavior: low-risk+policy-allowed executes; low-risk+policy-false routes to approval without provider call; `approval-required` never executes; audit unavailable fails closed before provider invocation.
  - [x] Provider-port tests proving only scoped context package metadata/permitted content reaches the provider; cancellation/timeout/outage paths return typed failures and do not mutate project state.
  - [x] Aggregate/projection tests proving low-risk execution events are append-only, idempotent, order-tolerant, source-version checked, and projected with the correct AI outcome state.
  - [x] UI/service/component tests proving low-risk success/failure/routed-to-approval rows render risk class, policy reason, context package, provenance, safe next action, live-region behavior, EN/FR labels, and metadata-only fallback.
  - [x] Leakage/isolation tests proving prompts, completions, provider payloads, file contents/paths, excluded file refs, raw email bodies, tenant IDs in denial bodies, secrets, raw exceptions, and unauthorized project/file evidence do not appear in audit envelopes, logs, projections, UI rows, fixtures, support artifacts, or exported/copy surfaces.
  - [x] Outage tests proving AI provider outage does not break non-AI workflows or existing approval/proposal inspection.

## Dev Notes

### Scope Boundaries

- This story owns FR40: the first allowed low-risk, read-only AI assistance execution path.
- This story may add low-risk execution contracts, policy evaluator, approval-gate behavior, an internal AI provider port, low-risk execution event/outcome records, projection/UI mapping, audit metadata, message/localization keys, idempotency, and focused tests.
- This story must not implement Story 4.5 approval decisions/S3 controls, Story 4.6 detailed preview, Story 4.7 allowlisted command execution, Story 4.8 full refusal/block behavior beyond low-risk execution safety, Story 4.9 correction invalidation, outbound email, CLI/MCP parity, tenant policy editor UI, vendor-provider selection, vector/embedding store work, or `Project.AppendConversationMessage` execution.
- Keep M0 conservative: risky or unclear work routes to approval; low-risk execution is read-only assistance under explicit policy, explicit project authorization, scoped context, and audit readiness.

### Existing Code To Reuse

- Story 3.14 scoped context package:
  - `src/Hexalith.ChatBot.Contracts/Queries/ProjectAiContextPackage.cs`
  - `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/ProjectAiContextPackageAssembler.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/ProjectAiContextPackageAssemblerTests.cs`
- Story 4.3 classifier/proposal metadata:
  - `src/Hexalith.ChatBot.Contracts/Commands/ProposeAIAction.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/AiActionProposalRecord.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/AiActionRiskClassificationRecord.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/DeterministicAiActionRiskClassifier.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionRiskClassifier.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionCommandMetadataProvider.cs`
- Gateway and audit seams:
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotGatewayContext.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/IApprovalGate.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughApprovalGate.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotApprovalResult.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- Durable operation/projection/UI surfaces:
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
  - `src/Hexalith.ChatBot.Server/Projections/PublishedAiOutcomeEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeEventView.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
  - `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`

### Current State To Preserve

- Story 4.3 records low-risk/approval-required classification metadata on proposals. Do not create a second classifier or trust caller-supplied classification as final authority.
- `PassThroughApprovalGate` currently approves everything. Story 4.4 should narrow only the AI low-risk/proposal path and preserve non-AI behavior until later stories own those gates.
- `Project.AppendConversationMessage` is the M0 AI action execution allowlist command and remains approval-required by default. This story must not execute it without approval.
- The scoped context package is metadata-only, redaction-aware, tenant/project scoped, and excludes unsafe/pending/redacted/unauthorized files. Do not read around it from Folders, mailbox, projection, or local paths.
- Existing projections are append-only/order-tolerant. Low-risk execution outcomes should follow the same pattern and not mutate prior proposal rows.
- Existing worktree has an unrelated modified `_bmad-output/story-automator/orchestration-4-20260601-145742.md`; do not revert or include it.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. Public wire contracts live in `.Contracts`; policy evaluation, provider port, gateway orchestration, aggregate validation, projections, and audit metadata live in `.Server`; UI consumes generated client contracts only.
- Every state mutation must enter through `CommandGateway`. UI/Client/provider adapters must not authorize, classify risk, evaluate low-risk policy, write audit, or persist outcomes directly.
- Governance stage interfaces remain internal to `.Server`; architecture tests must continue to reject UI/CLI/MCP references to `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, and `IIdempotencyStore`.
- Aggregate `Handle` methods stay pure: no provider calls, no Dapr, no Folders reads, no policy lookups, no authorization, no logging, no async.
- Tenant ID and actor authority come from authenticated server context, never from command body, provider response, UI, or generated client payload.
- Use repo-pinned stack only: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, Dapr/Aspire pins, xUnit v3, Shouldly, NSubstitute. Do not add package versions inline or casually upgrade vendor SDKs.
- Logs, traces, audit envelopes, support artifacts, fixtures, UI rows, and error bodies are metadata-only unless a public display field is explicitly redaction-stamped and policy-approved. Prompt/completion/provider payload/file-content leakage is a stop-ship defect.
- Root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Previous Story Intelligence

- Story 4.3 added deterministic classifier contracts and fixed two review findings: unknown command metadata spoofing and unknown action-class values now fail closed. Do not reintroduce caller-controlled command metadata as a trusted source for low-risk execution.
- Story 4.3 intentionally left `ai-action.low-risk-allowed` enforcement and execution to this story. Its `AiActionCommandMetadataProvider` currently knows `Project.AppendConversationMessage` as approval-required/default risky; add read-only assistance metadata deliberately if needed and test the exact wire tokens.
- Story 3.14 follow-up review fixed explicit project-scope read authorization, redacted/unauthorized package evidence filtering, and ETag/304 behavior. Consume the authorized package/read surface; do not reimplement weaker project-read authorization.
- Validation sandbox gotcha from prior stories: `dotnet test` via VSTest can fail with `SocketException (13): Permission denied`. Prefer compiled in-process xUnit v3 runners after build.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - Targeted compiled xUnit v3 runners for Contracts/Client generation, Server gateway/policy/provider/aggregate/projection/context package, UI service/component, Architecture, Conformance/isolation, and outage/leakage tests with `-parallel none`.
- Add broader server and conformance runs if the story touches shared gateway admission, OpenAPI, project conversation read models, or cross-tenant authorization.
- Do not rely on live AI-provider credentials in blocking tests. Use deterministic provider fakes and explicit outage fakes.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 4 is "Governed AI Action Mediation" and Story 4.4 follows Story 4.3 classifier work by executing only allowed low-risk read-only assistance.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR27, FR40-FR46, NFR8-NFR11, NFR13-NFR17, NFR21-NFR22, NFR40, and NFR58-NFR59.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially Risk Classifier, Tenant Policy Schema, Shared Command Pipeline, Command Allowlist v0/v1, and Idempotency Keys.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway order, Governed AI Mediation, project structure, provider boundary, metadata-only logging, derived-record shape, and testing guardrails.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`, especially AI proposal/outcome panels, risk/evidence chips, source-evidence vs AI-summary distinction, live-region behavior, disabled-control explanation, EN/FR localization, and responsive/accessibility constraints.
- Loaded persistent project-context facts from sibling module `project-context.md` files. Relevant rules: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, root-level submodules only, pure EventStore aggregate handlers, metadata-only diagnostics, tenant isolation, Dapr duplicate/order tolerance, and FrontComposer/Fluent UI inheritance.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/4-3-ai-action-risk-classification.md` and `_bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md`.
- Inspected current code to confirm extension points: `ProjectAiContextPackage`, `ProjectAiContextPackageAssembler`, `ProposeAIAction`, `AiActionProposalRecord`, `DeterministicAiActionRiskClassifier`, `AiActionRiskClassifier`, `PassThroughApprovalGate`, `ChatBotApprovalResult`, `CommandGateway`, `AcceptedCommandDispatcher`, `AuditEnvelopeFactory`, `GovernedOperationAggregate`, `PublishedAiOutcomeEvent`, `AiOutcomeProjectionTranslator`, `ProjectConversationItemView`, `ChatBotAiOutcomeConversationItem`, and related tests.
- Latest-technology research not required for story creation: no external provider, model SDK, cloud API, or package upgrade is selected by this story. Implementation should use repo-pinned versions and local provider ports/fakes unless a separate provider ADR approves live integration.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.4 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR27, FR40-FR46, NFR8-NFR17, NFR21-NFR22, NFR40, NFR58-NFR59.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Risk Classifier, Tenant Policy Schema, Shared Command Pipeline, Command Allowlist, Idempotency Keys.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, Governed AI Mediation, provider boundary, project structure, fail-closed/audit/testing guardrails.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - AI proposal/outcome, approval, risk chip, evidence chip, and blocked state component semantics.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - AI proposal/outcome state handling, live regions, keyboard/focus, source evidence vs AI summary, localization, and responsive constraints.
- `_bmad-output/implementation-artifacts/4-3-ai-action-risk-classification.md` - classifier metadata, code reuse list, scope boundaries, review fixes, validation notes.
- `_bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md` - scoped AI context package contract, assembler, authorization/redaction, review fixes.
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectAiContextPackage.cs` - context package manifest to consume.
- `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/ProjectAiContextPackageAssembler.cs` - deterministic package assembler and exclusion reason rules.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - gateway order and fail-closed admission behavior to preserve.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughApprovalGate.cs` - current approval gate stub to replace/wrap.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotApprovalResult.cs` - result shape to extend.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionRiskClassifier.cs` - deterministic classifier kernel.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionCommandMetadataProvider.cs` - current M0 command metadata.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - pure aggregate validation/event emission for new outcome events.
- `src/Hexalith.ChatBot.Server/Projections/PublishedAiOutcomeEvent.cs` - AI outcome projection input shape.
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs` - AI outcome sanitization/projection rules.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor` - existing rendered AI outcome row to reuse.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj --no-restore -m:1 /nr:false` - passed; regenerated generated client after OpenAPI change.
- `dotnet build src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj --no-restore -m:1 /nr:false` - passed.
- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Governance.AiMediation.AiActionPolicyEvaluatorTests -class Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayTests -class Hexalith.ChatBot.Server.Tests.Gateway.Stages.AcceptedCommandDispatcherTests -class Hexalith.ChatBot.Server.Tests.Operations.GovernedOperationAggregateTests -class Hexalith.ChatBot.Server.Tests.Projections.AiOutcomeProjectionTests -class Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests` - passed, 161 tests.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none -class Hexalith.ChatBot.Contracts.Tests.OpenApiContractSpineTests` - passed, 13 tests.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests` - passed, 14 tests.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -parallel none` - passed, 35 tests.
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none -class Hexalith.ChatBot.Conformance.Tests.CrossTenantLeakageScannerTests -class Hexalith.ChatBot.Conformance.Tests.CrossTenantReadSurfaceIsolationTests` - passed, 15 tests.
- `git diff --check` - passed after generated-client whitespace cleanup and senior review fixes.

### Completion Notes List

- Added public low-risk AI assistance execution contracts, OpenAPI schema entries, generated client types, and generated-client hash coverage while keeping the command body metadata-only.
- Replaced the pass-through AI approval path for low-risk assistance with a server-owned policy evaluator and approval gate that executes only when trusted policy allows and routes policy-false/unavailable cases to approval before idempotency, audit, dispatch, or provider invocation.
- Added an internal AI assistance provider port plus deterministic disabled-provider implementation; dispatcher invokes it only after gateway approval and submits metadata-only execution records to the durable operation path.
- Added low-risk execution idempotency, audit source refs, aggregate events/state handling, projection translation, and tests for policy, gateway, provider boundary, aggregate idempotency, projection, contracts, generated client, architecture, and conformance isolation.
- Preserved current Story 4.5 ownership by routing non-executable low-risk proposals to `review-ai-action` and not executing `Project.AppendConversationMessage`.
- Senior review auto-fixed project authorization enforcement, policy-false approval routing durability, low-risk coarse idempotency stability, safe-next-action validation, and risk metadata projection gaps.

### File List

- `_bmad-output/implementation-artifacts/4-4-low-risk-ai-assistance-execution.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Contracts/Commands/ExecuteLowRiskAIAssistance.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/LowRiskAiAssistanceContracts.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Server/Adapters/AiProvider/DisabledAiAssistanceProvider.cs`
- `src/Hexalith.ChatBot.Server/Adapters/AiProvider/IAiAssistanceProvider.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotGatewayContext.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotApprovalResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/DeterministicAiActionRiskClassifier.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionCommandMetadataProvider.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionPolicyDecision.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/DefaultAiActionPolicyEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/IAiActionPolicyEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/ITenantAiPolicySnapshotProvider.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/LowRiskAiAssistanceExecutionEvents.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/UnavailableTenantAiPolicySnapshotProvider.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/LowRiskAiOutcomeProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAiActionExecutionEvent.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionPolicyEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

## Senior Developer Review (AI)

Reviewer: Codex GPT-5
Date: 2026-06-01
Outcome: Approved after auto-fixes.

Findings fixed:

- High: `ExecuteLowRiskAIAssistance` authorization did not require an explicit project grant before provider execution.
- High: policy-false low-risk requests were rejected as forbidden instead of being durably routed to approval with audit/projection state.
- High: low-risk coarse idempotency included `ExecutionId`, allowing the same logical request to invoke the provider again under a changed execution id.
- Medium: aggregate validation allowed success outcomes with a non-`none` safe next action.
- Medium: low-risk AI outcome projection omitted risk metadata needed by downstream consumers.

Checklist result: all critical checklist items passed after fixes. Final validation passed with server test build, focused server behavioral suite, architecture tests, solution build, conformance tests from the implementation pass, and `git diff --check`.

---

Reviewer: Claude (story-automator adversarial review)
Date: 2026-06-10
Outcome: Changes Requested → auto-fixed → Approved.

Findings fixed:

- **Critical (AC5/AC6/AC8): low-risk AI outcome events were never projected into the project conversation read model.** `LowRiskAiOutcomeProjectionTranslator` had zero `src/` callers; `PublishedAiActionExecutionEvent` carried only the approved-action slots; `AiOutcomeProjectionHandler` had no dispatch branch for the four low-risk events; and `LowRiskAiAssistanceExecutionSucceeded/Failed/RoutedToApproval` did not even carry `ProjectId`. So in the live DAPR pub/sub path every low-risk outcome was silently `Ignored`, never reaching the S1 conversation — even though the task "Project and render low-risk AI outcome state" was marked `[x]`. The projection tests passed only because they invoked the translator directly, bypassing the real `eventTypeName`/typed-slot dispatch. Fixed by mirroring the approved-action wiring: added `ProjectId/RequesterId/SourceMessageId/SourceConversationItemId/AuthorizedContextReferences/ExcludedContextReasons` to the three completion events (populated in the aggregate), added the four low-risk slots to `PublishedAiActionExecutionEvent`, added `LowRiskAiOutcomeProjectionTranslator.TryCreatePublishedEvents`, and invoked it from `AiOutcomeProjectionHandler` when the approved translator yields nothing.
- **Medium (AC5): excluded context references were dropped from low-risk outcome rows.** `ExcludedContextReasons` is wired end-to-end (`PublishedAiOutcomeEvent` → `AiOutcomeEventView` → `ProjectConversationItemView.AiExcludedContextReasons` → UI label `AiOutcomeExcludedContextLabel`) and set by the main `AiOutcomeProjectionTranslator`, but `LowRiskAiOutcomeProjectionTranslator.FromCompleted` never set it, so the UI's excluded-context chips were always empty for low-risk rows. Fixed by threading the excluded/authorized references onto the completion events and setting them in `FromCompleted`.

Findings noted (not auto-fixed):

- **Low (transparency):** the File List omitted `PublishedAiActionExecutionEvent.cs` and `AiOutcomeProjectionHandler.cs` (now added). The working tree also carries an undocumented but passing 4.4 E2E test `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`.
- **Process/provenance:** this review ran against carried-over code from a prior full build (commit `098cd9b`); the current branch tip is the rebuilt story 4.3. Build and all suites are green, so the implementation is functionally intact, but the 4.4 code was not freshly re-implemented on top of the rebuilt 4.1–4.3.

Validation after fixes: solution build `Hexalith.ChatBot.slnx` 0 warnings / 0 errors; full server suite 1549 passed; `AiOutcomeProjectionTests` 29 passed (incl. new live wire-dispatch tests for low-risk success and routed-to-approval); architecture 39 passed; conformance leakage/isolation 15 passed. No public contract (OpenAPI / generated client) changed — all edits are `.Server`-internal.

## Change Log

- 2026-06-01: Implemented metadata-only low-risk AI assistance execution path with trusted policy gating, provider boundary, durable outcome events/projection, idempotency/audit coverage, OpenAPI/generated-client updates, and focused validation tests.
- 2026-06-01: Senior review auto-fixed authorization, routed-approval durability, idempotency, safe-next-action validation, and projection metadata issues; marked story done after validation.
- 2026-06-10: Adversarial story-automator review auto-fixed a critical projection-wiring gap (low-risk AI outcomes never reached the S1 conversation read model in the live pub/sub path) and the AC5 excluded-context-references omission; added live wire-dispatch projection tests. All suites green; status remains done.
