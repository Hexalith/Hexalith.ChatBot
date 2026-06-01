---
baseline_commit: e3e5c58
---

# Story 4.3: AI action risk classification

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As a security owner,
I want AI action requests classified by risk with a fail-closed default,
so that risky work is never executed without approval.

## Acceptance Criteria

1. Given a proposed AI action, when it is classified, then a deterministic tag-and-heuristic classifier reads the proposed command, tenant-policy classification for that command, action effect surface, and requester authority class, and outputs exactly `low-risk` or `approval-required` with no AI/model/tool-provider dependency. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.3; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Risk-Classifier]
2. Given classification succeeds, when the proposal is persisted and projected, then the proposal carries the risk class, risk action classes, classifier version, producing input tuple, policy snapshot id, allowlist/default-risk metadata, requester authority class, reason code, redaction state, retention class, schema version, and correlation id as metadata-only fields. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR42; _bmad-output/planning-artifacts/architecture.md#Governed-AI-Mediation]
3. Given missing tags, unknown effect surface, undeclared requester authority, missing policy classification, missing allowlist/default-risk metadata, or an otherwise indeterminate tuple, when classification completes, then the result is `approval-required`, not `low-risk`; the reason code identifies the indeterminate input without leaking restricted payloads. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Risk-Classifier; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15-NFR16]
4. Given a mixed request, when any proposed sub-action is in a risky class or any tuple is indeterminate, then the proposal inherits the strictest applicable classification (`approval-required`) and includes every contributing action class in deterministic order. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.3; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Task-Intent-and-AI-Action-Mediation]
5. Given the six risky action classes (`modifies-state`, `exposes-files`, `sends-external`, `creates-tasks`, `invokes-tools`, `acts-on-behalf`), when any class is present or when the M0 command `Project.AppendConversationMessage` is proposed with its default risk metadata, then the result is `approval-required`; no command is executed and no approval decision is made in this story. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Command-Allowlist-v0v1; _bmad-output/planning-artifacts/epics.md#Story-4.5]
6. Given a read-only action whose command metadata and tenant policy classification permit low risk, when requester authority and project authorization are known, then the classifier may output `low-risk`; enforcement of `ai-action.low-risk-allowed` and any execution remains Story 4.4 scope. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.4; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR40]
7. Given a command or policy metadata explicitly marks an action as disallowed/unsupported, when a proposal is submitted, then the classifier does not invent a `denied` risk value; the path fails closed with a metadata-only rejection before any durable proposal or execution side effect. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Risk-Classifier; _bmad-output/planning-artifacts/epics.md#Story-4.8]
8. Given a reviewer later disagrees with a classifier result, when the disagreement event is recorded, then it captures classifier version, input tuple, classification, reviewer decision, resolution, proposal id, correlation id, and policy snapshot id for A9a calibration without storing raw prompt, message body, provider payload, file content, or tool arguments. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.3; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Risk-Classifier]
9. Contract, OpenAPI/generated-client, gateway-stage, aggregate/projection, UI mapping, audit metadata, A9a fixture, no-AI-dependency, fail-closed, mixed-request, disallowed, idempotency, and cross-tenant leakage tests prove the classifier and projected proposal metadata. [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]

## Tasks / Subtasks

- [x] Define the AI action risk contract without overloading generic severity (AC: 1, 2, 3, 4, 8, 9)
  - [x] Add purpose-named public contract records/enums under `src/Hexalith.ChatBot.Contracts/` for AI action classification, for example `AiActionRiskClass` (`low-risk`, `approval-required`), `AiActionRiskActionClass`, `AiActionRiskInputTuple`, and `AiActionRiskClassificationRecord`.
  - [x] Do not reuse the existing generic `RiskClass` enum (`none`, `low`, `medium`, `high`, `blocked`) as the source of truth for AI action mediation unless the wire contract is made explicitly compatible with `low-risk` and `approval-required` without breaking existing approval/AI outcome rendering.
  - [x] Extend `AiActionProposalRecord` with classification metadata: risk class, risk action classes, classifier version, input tuple, policy reason code, command allowlist version/default risk, requester authority class, produced-at UTC timestamp, and indeterminate reason when applicable.
  - [x] Extend `ProposeAIAction` only with metadata the gateway needs to classify safely. Tenant id, authenticated actor, and final authority must still come from server context, not the request body.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- [x] Implement the deterministic classifier kernel in the Governance seam (AC: 1, 3, 4, 5, 6, 7, 9)
  - [x] Add the implementation under `src/Hexalith.ChatBot.Server/Governance/AiMediation/` or `src/Hexalith.ChatBot.Server/Governance/RiskClassifier/`; do not place it in UI, Client, Projections-only, Workers, or a sibling submodule.
  - [x] Make the kernel a pure deterministic function over a metadata-only input tuple. It must not call an AI provider, embedding provider, external tool, mailbox body source, Folders content source, sibling service client, or networked policy service.
  - [x] Model action classes with stable wire tokens: `modifies-state`, `exposes-files`, `sends-external`, `creates-tasks`, `invokes-tools`, `acts-on-behalf`. Map UI display labels separately to the existing risk chip language.
  - [x] Treat any risky class, mixed risky/read-only request, missing effect surface, missing policy classification, missing requester authority, missing allowlist metadata, unknown command, or unknown action class as `approval-required`.
  - [x] Preserve the addendum rule that disallowed/unsupported command metadata is rejected before classification; do not add a third risk-class output.
  - [x] Include an M0 command metadata provider for `Project.AppendConversationMessage` with allowlist version, effect surface, authority class, default risk `approval-required`, and reason code. Do not implement command execution.
- [x] Replace the pass-through gateway risk stage for AI action proposals (AC: 1, 2, 3, 5, 7, 9)
  - [x] Replace or wrap `PassThroughRiskClassifier` registration in `CommandGatewayServiceCollectionExtensions` with the deterministic classifier for AI action proposal submissions while preserving pass-through/neutral behavior for non-AI commands until their stories require classification.
  - [x] Extend `ChatBotRiskClassification` and `ChatBotGatewayContext` so the risk stage result is available to later gateway stages and audit envelope creation.
  - [x] Ensure the stage still runs in the existing order: `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> pre-commit-audit -> dispatch`.
  - [x] Fail closed with a redacted, message-catalog-backed problem when pre-classification metadata says disallowed/unsupported or when required command metadata is structurally invalid.
  - [x] Update `AuditEnvelopeFactory` so pre-commit audit source evidence includes classification metadata refs such as classifier version, risk class, action classes, policy snapshot id, and reason code, never raw payload.
- [x] Persist and project classification through the proposal shell from Story 4.2 (AC: 2, 3, 4, 5, 6, 8, 9)
  - [x] Use the single classifier kernel from the gateway path; do not create a second aggregate-only classifier with diverging logic.
  - [x] Update `AcceptedCommandDispatcher` or the proposal submission path so `ProposeAIAction` reaches `GovernedOperationAggregate` with the classification record produced by the gateway. The aggregate should validate the classification metadata is present and safe, then persist it as part of `TaskIntentConvertedToAiActionProposal`.
  - [x] Extend `GovernedOperationAggregate.Handle(ProposeAIAction)` and `GovernedOperationState` only as needed to store classification on the immutable proposal record. Existing conversion idempotency and terminal-state rejection behavior must stay intact.
  - [x] Extend `TaskIntentProjectionTranslator.TryCreateAiOutcome`, `PublishedTaskIntentEvent`, and `ProjectConversationItemView` so S1 AI proposal/outcome rows expose `AiRiskClass`, `AiRiskActionClasses`, policy reason, classifier version/input tuple refs, and `safeNextAction = review-ai-action` for approval-required proposals.
  - [x] Preserve metadata-only behavior: classification/projection/audit data must not contain raw source message bodies, subjects, prompts, completions, provider payloads, file paths/content, or tool args.
  - [x] Add a durable classifier-disagreement event/record shape for A9a calibration. If no approval decision workflow exists yet, wire the event type and tests without adding approval execution behavior.
- [x] Reuse existing UI risk/proposal primitives instead of adding a visual language (AC: 2, 4, 8, 9)
  - [x] Extend generated-client mapping in `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs` and project-conversation models so AI proposal rows carry the classification details.
  - [x] Reuse `ChatBotAiOutcomeConversationItem`, `ChatBotApprovalConversationItem`, `ChatBotRiskChip`, existing localization keys, and `ChatBotGovernedUiText`. Add only missing EN/FR resource keys needed for `low-risk`, `approval-required`, classifier version, and policy reason.
  - [x] Risk chips must include text labels and policy reason, not color alone. Preserve keyboard/focus/live-region rules already established for proposal-ready state.
  - [x] Do not add S3 approval controls beyond displaying classification metadata and the existing `review-ai-action` next action. Approval decisions remain Story 4.5.
- [x] Extend A9a fixture/evaluation support for risk classification (AC: 3, 4, 8, 9)
  - [x] Reuse `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedEvaluationDataset.cs` and the existing scaffold fixture; add classification labels/outcomes for `risky-ai-candidate`, mixed request, read-only low-risk candidate, and indeterminate metadata.
  - [x] Preserve `isScaffold` truth and do not claim the full A9a corpus exists.
  - [x] Add calibration outcome counts for classifier disagreements without requiring Story 4.5 approval workflow to be complete.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract/OpenAPI/generated-client tests for exact wire values (`low-risk`, `approval-required`, the six action-class tokens), required metadata fields, additive compatibility, and no tenant authority in command body.
  - [x] Kernel unit tests for M0 `Project.AppendConversationMessage`, read-only low-risk candidate, each of the six risky classes, mixed requests, unknown effect surface, missing authority, missing policy, disallowed metadata, and deterministic ordering.
  - [x] Gateway tests proving risk-classify runs before approval/idempotency/audit/dispatch, records classification on context, fails closed on disallowed/invalid metadata, and does not call AI/tool/network dependencies.
  - [x] Aggregate/projection tests proving proposal conversion persists and projects the classification metadata, idempotent replay preserves the same classification, conflicting transitions reject safely, and S1 ETags change when proposal classification metadata changes.
  - [x] UI/service/component tests proving risk chip/proposal rows render risk class, action classes, policy reason, safe next action, EN/FR labels, accessible names, and metadata-only fallback.
  - [x] Isolation/leakage tests proving denial bodies, logs, audit envelopes, projections, UI rows, fixtures, and support artifacts do not leak raw mail content, subject, provider payload, prompts, completions, tool args, file paths/content, tenant ids in denial bodies, secrets, or raw exceptions.

## Dev Notes

### Scope Boundaries

- This story owns FR39 risk classification and the classifier metadata required by later low-risk execution, approval, preview, allowlisted execution, refusal, and correction-invalidation stories.
- This story may add classifier contracts, OpenAPI/generated-client changes, deterministic classifier kernel, gateway risk-stage behavior, proposal record enrichment, projection/UI mapping, audit metadata, disagreement event shape, fixture support, and focused tests.
- This story must not implement Story 4.4 low-risk AI execution, Story 4.5 approval decision workflow/S3 controls, Story 4.6 detailed preview beyond classification metadata, Story 4.7 allowlisted command execution, Story 4.8 full refusal/block behavior beyond pre-classification safe rejection, Story 4.9 correction invalidation of AI proposals, outbound email, CLI/MCP parity, tenant policy editor UI, or any model/tool invocation.
- The classifier is M0 deterministic. Do not call an LLM or external service to decide risk.

### Existing Code To Reuse

- Story 4.2 proposal shell:
  - `src/Hexalith.ChatBot.Contracts/Commands/ProposeAIAction.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/AiActionProposalRecord.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentConvertedToAiActionProposal.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- Gateway risk stage stubs to replace carefully:
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/IRiskClassifier.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotRiskClassification.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughRiskClassifier.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotGatewayContext.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- Current risk/proposal projection and UI shapes:
  - `src/Hexalith.ChatBot.Server/Projections/PublishedTaskIntentEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeEventView.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor`
  - `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- Existing UI token language already has user-facing risk action labels: externally visible/file exposing/project mutating/tool invoking/task creating/participant representing. Keep wire tokens aligned to PRD/addendum and map display labels at UI boundaries.

### Current State To Preserve

- Story 4.2 converts captured task intent into a durable `AiActionProposalRecord` and projects it as an AI outcome proposal with `safeNextAction = review-ai-action`; do not regress review/disposition behavior, idempotency, or terminal-state rejection.
- `AiActionProposalRecord` currently has no classification fields. Extend it additively; do not create a second unrelated proposal DTO.
- `RiskClass` currently serializes generic values `none`, `low`, `medium`, `high`, and `blocked`. The AI action risk contract must produce `low-risk` and `approval-required`; avoid breaking existing generic approval/AI outcome consumers.
- `PassThroughRiskClassifier` currently returns an empty `ChatBotRiskClassification.PassThrough`; Story 4.3 should make AI proposal classification real while preserving the gateway stage order and non-AI command behavior.
- `ChatBotSpineCommandAllowlist` is the CommandGateway admission allowlist and is separate from the AI action execution allowlist in the PRD addendum. Do not conflate them; update only where proposal submission genuinely requires admission.
- Existing projections and stores are idempotent and source-version/order tolerant. Classification metadata must follow the same rules.
- Existing worktree has an unrelated modified story-automator orchestration file. Do not revert or include it.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. Public DTOs and wire enums live in `.Contracts`; the classifier kernel, gateway stage, aggregate validation, projections, and audit metadata live in `.Server`; UI consumes generated client contracts only.
- Every state mutation must enter via CommandGateway. The risk classifier is a gateway stage and must not be replicated in UI/CLI/MCP adapters.
- Keep governance stage interfaces internal to `.Server`; architecture tests must continue to reject UI/CLI/MCP references to `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, and `IIdempotencyStore`.
- Tenant id, authenticated actor, and final requester authority are server-derived. Do not trust tenant/authority/classification fields supplied by UI or provider payload without gateway validation.
- Use `System.Text.Json`, camelCase wire schema, additive serialization-tolerant evolution, UTC `DateTimeOffset`, stable IDs, central package management, and repo-pinned package versions.
- Do not add package upgrades for this story. Relevant pinned versions include SDK `10.0.300`, `net10.0`, Dapr `1.17.9`, Aspire `13.3.x`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, ModelContextProtocol `1.3.0`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, and Playwright `1.60.0`.
- Logs, traces, audit envelopes, support artifacts, fixture output, and user-visible errors are metadata-only. Raw source messages, email subjects, prompts, model outputs, file contents/paths, provider payloads, tool args, secrets, and raw exceptions are stop-ship leakage defects.
- Root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - Compiled xUnit v3 runners for Contracts, Client generation, Server gateway/kernel/aggregate/projection, UI service/component, Testing fixture/evaluation, Architecture, and Conformance/isolation with `-parallel none`.
- Sandbox note inherited from previous stories: `dotnet test` via VSTest can fail in this environment with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 test DLLs after build.
- Add UI E2E only if rendered proposal/risk rows change in a way not covered by bUnit/service tests. Use accessible roles/labels or stable data attributes, not CSS selectors or sleeps.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 4 is "Governed AI Action Mediation" and Story 4.3 owns FR39 classifier behavior between Story 4.2 proposal conversion and Story 4.4/4.5 execution/approval paths.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR39-FR46, FR61, FR81a, FR91a, A8, A9a, NFR16, NFR22, NFR46-NFR48, and the high-risk acceptance scenario matrix for AI mediation.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially Risk Classifier, Command Allowlist v0/v1, Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys, and Inbound Message Authenticity.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway order, Governed AI Mediation, fail-closed behavior, modular-monolith seams, project structure, internal gateway stage interfaces, and testing standards.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`, especially risk chip, AI proposal panel, approval panel, AI proposal ready state, live-region/accessibility requirements, and EN/FR localization constraints.
- Loaded persistent project-context facts from sibling module `project-context.md` files. Relevant rules: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, root-level submodules only, pure EventStore aggregate handlers, metadata-only diagnostics, FrontComposer/Fluent UI inheritance, and DAPR duplicate/order tolerance.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/4-2-task-intent-review-conversion-and-disposition.md`. Story 4.2 deliberately left final risk classification pending, created `AiActionProposalRecord`, and projected conversion as a metadata-only AI outcome proposal.
- Inspected current code to confirm extension points: `AiActionProposalRecord`, `ProposeAIAction`, `TaskIntentConvertedToAiActionProposal`, `IRiskClassifier`, `ChatBotRiskClassification`, `PassThroughRiskClassifier`, `ChatBotGatewayContext`, `CommandGateway`, `AuditEnvelopeFactory`, `TaskIntentProjectionTranslator`, `ProjectConversationItemView`, `ChatBotAiOutcomeConversationItem`, `ChatBotRiskChip`, and relevant tests.
- Latest-technology research not required for implementation: no new external package, cloud API, model API, or third-party framework is in scope. The story is constrained to repo-pinned versions and existing platform patterns.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.3 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR39-FR46, A9a, NFR16, NFR22, NFR46-NFR48, FR81a.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Risk Classifier, Command Allowlist v0/v1, Tenant Policy Schema, Shared Command Pipeline.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, Governed AI Mediation, project structure, fail-closed/audit/testing guardrails.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - risk chip, proposal/approval components, visual semantics.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - AI Action Review, proposal ready state, risk chip behavior, accessibility/live-region requirements.
- `_bmad-output/implementation-artifacts/4-2-task-intent-review-conversion-and-disposition.md` - previous-story code reuse list, current proposal shell, validation notes, and scope boundaries.
- `src/Hexalith.ChatBot.Contracts/Queries/AiActionProposalRecord.cs` - proposal record to extend additively.
- `src/Hexalith.ChatBot.Contracts/Commands/ProposeAIAction.cs` - proposal conversion command to classify safely.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IRiskClassifier.cs` - gateway risk stage seam.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughRiskClassifier.cs` - current stub to replace/wrap.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotGatewayContext.cs` - context to carry classification into later stages.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - admission order to preserve.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - pre/post-commit audit metadata to enrich.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - current conversion logic to validate/store classification.
- `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionTranslator.cs` - projection translator to carry classification into AI outcome rows.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor` - existing rendered proposal row to reuse.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor` - existing accessible risk chip to reuse.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Dev-story workflow executed on 2026-06-01 for `_bmad-output/implementation-artifacts/4-3-ai-action-risk-classification.md`.
- Workflow activation resolved with no prepend/append steps and persistent facts loaded from sibling `project-context.md` files.
- Implemented a deterministic metadata-only classifier under `src/Hexalith.ChatBot.Server/Governance/AiMediation/` and wired it into the gateway risk stage for `ProposeAIAction`.
- Standard `dotnet test Hexalith.ChatBot.slnx --no-build -m:1 /nr:false` was attempted and aborted by VSTest socket binding in this sandbox with `SocketException (13): Permission denied`.

### Completion Notes List

- Added AI action risk wire enums/records with `low-risk`, `approval-required`, and the six stable action-class tokens without reusing the generic `RiskClass` source of truth.
- Added a pure deterministic classifier with fail-closed indeterminate handling, M0 `Project.AppendConversationMessage` metadata, unsupported/disallowed rejection, strictest mixed-request behavior, and a metadata-only classifier-disagreement event shape.
- Replaced pass-through gateway behavior for AI action proposals with deterministic classification, context propagation, fail-closed rejection before durable work, audit evidence refs, and dispatcher injection of the gateway-produced classification.
- Persisted and projected classification metadata through proposal records, AI outcome projections, project conversation items, generated client mappings, UI models, and EN/FR localized UI labels.
- Extended A9a scaffold fixtures with risky, mixed, low-risk, indeterminate, and classifier-disagreement calibration outcomes while preserving scaffold truth.
- Validation passed via full solution build and focused in-process xUnit v3 runners; no model/tool/network dependency was added.

### File List

- `_bmad-output/implementation-artifacts/4-3-ai-action-risk-classification.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/ProposeAIAction.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AiActionRiskActionClass.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AiActionRiskClass.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AiActionProposalRecord.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AiActionRiskClassificationRecord.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AiActionRiskInputTuple.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotGatewayContext.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotRiskClassification.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/DeterministicAiActionRiskClassifier.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughRiskClassifier.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionCommandMetadataProvider.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionRiskClassifier.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionRiskClassifierDisagreementRecorded.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeEventView.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAiOutcomeEvent.cs`
- `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedEvaluationDataset.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionRiskClassifierTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
- `tests/Hexalith.ChatBot.Testing.Tests/Fixtures/TenantScopedFixtureManifestTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
- `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes.

Checklist notes:
- Story status was `review` at review start; epic/story resolved as 4.3.
- Architecture and story context were available from `_bmad-output/planning-artifacts/architecture.md` and the story references. MCP resources were checked; none were configured, and no web fallback was needed because this review did not introduce or depend on a new external API/package.
- File List was reconciled against git changes. `_bmad-output/` runtime artifacts were excluded from code review per workflow.

Findings fixed:
- HIGH: Unknown AI action commands could use caller-supplied low-risk metadata strongly enough to pass as `low-risk`. Fixed `DeterministicAiActionRiskClassifier` so gateway command metadata support is trusted only when a server-side metadata provider knows the command; spoofed unknown commands now fail closed before durable work.
- HIGH: An out-of-range `AiActionRiskActionClass` could throw during deterministic ordering/serialization instead of returning `approval-required`. Fixed the classifier to detect unknown action classes, return `indeterminate_unknown_action_class`, and sanitize returned action-class metadata.
- MEDIUM: Story File List omitted the changed UI E2E fixture/test file. Added `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` to the File List.

Fixes applied:
- Added unknown-action-class fail-closed handling and serialization-safe classifier output.
- Added aggregate validation for known AI risk action classes before persisting proposal classification metadata.
- Added gateway regression coverage proving spoofed low-risk metadata for an unknown command is rejected before dispatch, audit, or idempotency admission.
- Added classifier regression coverage proving unknown action classes do not leak invalid enum values.

### Change Log

- 2026-06-01: Implemented Story 4.3 deterministic AI action risk classification, metadata propagation, UI mapping, fixture support, tests, and review handoff tracking.
- 2026-06-01: Senior developer review fixed unknown-command metadata spoofing, unknown action-class fail-closed behavior, aggregate classification validation, and File List reconciliation.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Governance.AiMediation.AiActionRiskClassifierTests -class Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayTests -class Hexalith.ChatBot.Server.Tests.Operations.GovernedOperationAggregateTests -class Hexalith.ChatBot.Server.Tests.Projections.AiOutcomeProjectionTests -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests` - passed, 158 tests.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests` - passed, 14 tests.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectConversationContractTests` - passed, 6 tests.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests` - passed, 4 tests.
- `tests/Hexalith.ChatBot.Testing.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Testing.Tests -noLogo -parallel none -class Hexalith.ChatBot.Testing.Tests.Fixtures.TenantScopedFixtureManifestTests` - passed, 40 tests.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -parallel none` - passed, 35 tests.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed, 15 tests.
- `dotnet test Hexalith.ChatBot.slnx --no-build -m:1 /nr:false` - attempted; VSTest aborted in sandbox with `System.Net.Sockets.SocketException (13): Permission denied`.
