---
baseline_commit: c8b7d54
---

# Story 4.9: Correction invalidates AI action proposals

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As a project owner,
I want AI proposals that consumed corrected project context invalidated or blocked,
so that approval and execution never use stale evidence.

## Acceptance Criteria

1. Given an AI action proposal was built from association evidence, when that association is corrected, then every pending or not-yet-executed proposal, approval, and execution path that consumed the corrected evidence is marked invalidated with the correction ID, source association ID, corrected evidence state, source version, and correlation ID; the original proposal and approval rows remain append-only and are not mutated in place. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.9`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/implementation-artifacts/4-8-refusal-and-safe-block-behavior.md#Current State To Preserve`]
2. Given a proposal or approval has been invalidated by corrected context, when a reviewer tries to approve it, an AI actor tries to execute it, or replay delivers stale approval/execution events, then the action fails closed with reason `corrected-context-invalidated`, cannot create success outcomes, cannot dispatch `Project.AppendConversationMessage`, cannot call Conversations/Folders/AI providers, and records metadata-only denial/refusal/audit lifecycle evidence. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.9`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR46`; `src/Hexalith.ChatBot.Contracts/Messages/ChatBotRefusalReasonCodes.cs`]
3. Given a new AI proposal is requested after correction, when all required M0 correction propagation acknowledgements are complete (`association-routing`, `evidence-snapshot`, `operation-status`, `ai-context-readiness`), then the proposal uses the corrected evidence snapshot, records correction lineage in the proposal/approval/audit metadata, and exposes only safe metadata tokens to contracts, projections, UI, logs, and tests. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.9`; `src/Hexalith.ChatBot.Server/Association/CorrectionPropagationStoreKeys.cs`; `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/ProjectionCorrectedContextReadinessPolicy.cs`]
4. Given correction propagation is pending, delayed, incomplete, stale, or failed for any required M0 store, when a new proposal, low-risk assistance, approval decision, or approved execution would consume affected evidence, then the system blocks or routes to safe recovery rather than fabricating from stale context; visible state uses catalog-backed copy and safe next actions such as `review-source-evidence`, `retry-later`, `resolve-association`, or `none`. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Task Intent and AI Action Mediation`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow 9 - Governed AI execution`; `_bmad-output/implementation-artifacts/4-8-refusal-and-safe-block-behavior.md#Acceptance Criteria`]
5. Given an AI action needs file context, when it consumes context from Epic 3, then it uses only an authorized, current `ProjectAiContextPackage` manifest produced by Story 3.14, and the manifest/evidence references align with the corrected association source version before proposal, approval, low-risk execution, or approved execution proceeds. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.9`; `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/ProjectAiContextPackageAssembler.cs`; `_bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md`]
6. Given invalidation or corrected-lineage lifecycle rows are projected into the project conversation, AI action review surface, operational queues, or audit investigation, when events arrive duplicate, stale, or out of order, then projection remains append-only, idempotent, tenant-scoped, and reconstructable by proposal ID, approval ID, execution ID, correction ID, source message ID, operation ID, and correlation ID. [Source: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs`; `_bmad-output/implementation-artifacts/4-6-ai-action-preview-and-inspection.md#Current State To Preserve`]
7. Given invalidated proposal or approval state is visible in S3 or the project conversation, when tested with keyboard, screen reader semantics, reduced motion, forced colors, English/French text, and phone/tablet widths, then approval is unavailable with a reachable reason, terminal current-user invalidations announce assertively, historical invalidations do not announce on initial load, focus stays in the review panel, and no text overlaps or relies on hover-only detail. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback matrix`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components`]
8. Given this story completes, when acceptance coverage runs, then tests prove correction-triggered proposal invalidation, approval/execute fail-closed behavior, corrected-lineage proposal creation after propagation completion, context-package currency, audit/projection metadata-only safety, replay/idempotency, tenant isolation, cross-surface refusal parity, and UI accessibility without implementing M1 outbound send, arbitrary tools, vector reindexing beyond M0 acknowledgement semantics, or a tenant policy editor. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.9`; `_bmad-output/planning-artifacts/architecture.md#Implementation Handoff`; `_bmad-output/implementation-artifacts/4-8-refusal-and-safe-block-behavior.md#Testing Notes`]

## Tasks / Subtasks

- [x] Add explicit corrected-context invalidation domain events and state (AC: 1, 2, 6, 8)
  - [x] Add a focused event shape under `src/Hexalith.ChatBot.Server/Governance/AiMediation/`, for example `AiActionProposalInvalidatedByCorrection`, carrying proposal ID, approval ID when known, task intent ID, source message ID, source conversation item ID, requester ID, project ID, association ID, correction ID, corrected project/evidence metadata, source version, correlation ID, redaction state, and retention class.
  - [x] Extend `GovernedOperationState` to track invalidated proposal IDs and correction lineage without mutating original proposal or approval records.
  - [x] Apply invalidation events idempotently: duplicate same correction/proposal input is no-op; conflicting duplicate input fails closed with metadata-only reason.
  - [x] Do not add I/O, projection lookup, Dapr calls, AI provider calls, or sibling-service calls inside `GovernedOperationAggregate.Handle`.
- [x] Wire correction propagation to AI proposal invalidation (AC: 1, 3, 4, 6)
  - [x] Reuse existing association correction events: `MailboxEmailAssociationCorrected`, `MailboxAssociationCorrectionStoreInvalidated`, and `MailboxAssociationCorrectionPropagationCompleted`.
  - [x] Treat `CorrectionPropagationStoreKeys.AiContextReadiness` and `EvidenceSnapshot` as the M0 stores that gate AI proposal reuse; do not invent a second readiness model.
  - [x] Add the minimal coordinator/projection bridge needed to emit invalidation lifecycle events for affected proposals after correction is observed. Prefer an event/projection handler over a direct cross-seam service call.
  - [x] If no existing projection can find affected proposals by source message/evidence reference, add a tenant-scoped metadata index in the projection layer only; keep aggregate state pure.
- [x] Extend proposal and approval contracts with correction lineage where needed (AC: 1, 3, 5, 8)
  - [x] Extend `AiActionProposalRecord` and/or `ProposeAIAction` metadata to carry safe correction lineage tokens such as `CorrectionLineageId`, `AssociationId`, `EvidenceSnapshotSourceVersion`, and `ContextPackageId/Version` if current `ProposalInputMetadata` is insufficient or too ambiguous.
  - [x] Preserve existing `TaskIntentRecord.CorrectionLineageId`; new proposal lineage should derive from captured task intent and corrected evidence readiness rather than client-provided truth.
  - [x] If public contract/OpenAPI shapes change, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
  - [x] Keep all lineage values safe metadata tokens. Never surface raw email body, file content, unrestricted filenames/paths, raw correction rationale, tenant secrets, provider payloads, prompts, completions, or raw exception text.
- [x] Enforce invalidated proposal blocking in approval and execution paths (AC: 2, 4, 8)
  - [x] Update `DecideAiActionApproval` handling so approval of an invalidated proposal rejects with `corrected-context-invalidated` or a catalog-backed approval-disabled reason before recording an approved decision.
  - [x] Preserve Story 4.7/4.8 execution behavior: `ExecuteApprovedAIAction` already rejects `CorrectedContextReady == false` with `ChatBotRefusalReasonCodes.CorrectedContextInvalidated`; extend it to consult invalidated proposal state/lineage, not only the command flag.
  - [x] Ensure invalidated execution produces `ApprovedAiActionExecutionRejected`, not a success or thrown expected-business exception.
  - [x] Do not weaken `ChatBotSpineCommandAllowlist` or `ApprovedAiActionCommandAllowlist`; M0 approved AI execution remains exactly `Project.AppendConversationMessage`.
- [x] Require current authorized context packages for file-consuming actions (AC: 3, 4, 5, 8)
  - [x] Reuse `DefaultProjectAiContextPackageAssembler` and `ProjectAiContextPackage` instead of adding a separate file-context manifest.
  - [x] Verify proposed/approved/low-risk/execute paths compare package source version and evidence references against corrected association state before proceeding.
  - [x] Exclude pending scan, unsafe, rejected, unavailable, unauthorized, redacted, stale, or policy-denied files using existing package exclusion semantics.
  - [x] If the context package is unavailable or stale, use `context-package-unavailable`, `missing-required-context`, or `corrected-context-invalidated` consistently with the message catalog.
- [x] Project invalidation and lineage into conversation, S3, and audit surfaces (AC: 1, 2, 6, 7, 8)
  - [x] Extend `PublishedAiActionExecutionEvent` and `ApprovedAiActionOutcomeProjectionTranslator` only if existing rejected execution mapping cannot represent proactive proposal invalidation.
  - [x] Prefer existing `AiOutcomeKind.CorrectedContextInvalidated` and `AiOutcomeStatus.Invalidated`; add enum/contract fields only if they are required to distinguish invalidation from generic refusal.
  - [x] Reuse `AiOutcomeProjectionTranslator`, `ProjectConversationItemView`, `ChatBotApprovalConversationItem.razor`, `ChatBotAiOutcomeConversationItem.razor`, `ChatBotBlockedState.razor`, and review-history components.
  - [x] Surface invalidation as metadata-only evidence with correction ID, safe next action, disabled approval reason, audit status, and lineage. Do not hide the old proposal row; mark it superseded/invalidated through append-only history.
  - [x] Add EN/FR localization only for missing labels or reason strings; verify long French labels fit phone/tablet layouts.
- [x] Add focused acceptance coverage (AC: all)
  - [x] Contract tests for new event/record fields, enum wire values, OpenAPI/generated client hash if changed, metadata-only serialization, and safe token validation.
  - [x] Aggregate tests for proposal invalidation, approval-after-invalidation rejection, execution-after-invalidation rejection, duplicate invalidation replay, conflicting duplicate invalidation, and corrected-lineage proposal creation after propagation completion.
  - [x] Projection tests for invalidated AI outcome rows, correction lineage, out-of-order delivery, duplicate replay, stale replay, tenant isolation, lifecycle review-history reconstruction, and leakage sentinels.
  - [x] Gateway/dispatcher tests proving invalidated approval/execution does not create idempotency success, dispatch EventStore commands, call `IConversationWriter`, call Folders, or call an AI provider.
  - [x] Conformance tests proving equivalent redacted refusal semantics across UI/service/CLI/MCP/AI adapter shims except audited origin.
  - [x] UI/bUnit/E2E tests for invalidated approval rendering, disabled approve reason, reachable blocked explanation, live-region behavior, focus retention, EN/FR labels, forced-colors, reduced-motion, phone/tablet no-overlap, and no sensitive strings in markup.

## Dev Notes

### Scope Boundaries

- This story owns the remaining Story 4.9 behavior: proactive invalidation of existing AI proposals/approvals that consumed corrected association evidence, plus corrected-lineage recording for new proposals after M0/M1 readiness completes.
- This story may add small metadata fields, domain events, projection translation, message/localization entries, and tests needed for invalidation and lineage.
- This story must not implement M1 outbound draft/send, arbitrary tool invocation, vector reindex completion beyond respecting M0/M1 acknowledgement state, tenant policy editor UI, broad document intelligence, autonomous task management, or production CLI/MCP adapters.
- Existing Story 4.8 safe-blocking already added `corrected-context-invalidated`; reuse it. Do not create a parallel refusal taxonomy.
- Existing Story 4.7 approved execution already gates `ExecuteApprovedAIAction` and has a separate AI-action command allowlist. Keep that separation intact.

### Existing Code To Reuse

- Correction and readiness:
  - `src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociationCorrected.cs`
  - `src/Hexalith.ChatBot.Server/Association/MailboxAssociationCorrectionStoreInvalidated.cs`
  - `src/Hexalith.ChatBot.Server/Association/MailboxAssociationCorrectionPropagationCompleted.cs`
  - `src/Hexalith.ChatBot.Server/Association/CorrectionPropagationStoreKeys.cs`
  - `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/ProjectionCorrectedContextReadinessPolicy.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/CorrectedContextReadinessPolicyTests.cs`
- AI mediation contracts and aggregate:
  - `src/Hexalith.ChatBot.Contracts/Commands/ProposeAIAction.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/ExecuteApprovedAIAction.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentRecord.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/AiActionProposalRecord.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionApprovalEvents.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/ApprovedAiActionExecutionEvents.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- Refusal, gateway, allowlist, and dispatch:
  - `src/Hexalith.ChatBot.Contracts/Messages/ChatBotRefusalReasonCodes.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/ApprovedAiActionCommandAllowlist.cs`
  - `src/Hexalith.ChatBot.Server/Adapters/Conversations/IConversationWriter.cs`
- Context package and projection/UI:
  - `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/ProjectAiContextPackageAssembler.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/ProjectAiContextPackage.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/AiOutcomeKind.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/AiOutcomeStatus.cs`
  - `src/Hexalith.ChatBot.Server/Projections/PublishedAiActionExecutionEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor`
- Existing tests to extend:
  - `tests/Hexalith.ChatBot.Contracts.Tests/TaskIntentContractTests.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Operations/CorrectionPropagationAggregateTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
  - `tests/Hexalith.ChatBot.Conformance.Tests/RejectionIntentParityTests.cs`
  - `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`

### Current State To Preserve

- Story 4.8 added `ChatBotRefusalReasonCodes.CorrectedContextInvalidated` mapped to `AssociationAiContextBlocked`, and `ApprovedAiActionOutcomeProjectionTranslator.FromRejected()` already projects rejected approved execution as blocked/refusal lifecycle rows.
- `ExecuteApprovedAIAction` currently rejects `CorrectedContextReady == false` before approval/execution success. Story 4.9 must strengthen this with aggregate/projection invalidation state so callers cannot bypass by setting the flag incorrectly.
- `TaskIntentRecord` already has `CorrectionLineageId`; `AiActionProposalRecord` does not yet expose explicit correction lineage. Prefer minimal field additions over overloading opaque dictionaries if the lineage becomes part of public inspection.
- `ProposeAIAction` builds `AiActionProposalRecord` and `AiActionApprovalRequested` from captured task intent state. New proposal lineage must be based on trusted server/correction state, not client-supplied truth.
- `GovernedOperationState` tracks approvals, decisions, task intents, approved executions, and correction propagation state. Add invalidation tracking carefully so replay remains deterministic and append-only.
- `ProjectionCorrectedContextReadinessPolicy` blocks until the association projection has current source version, complete downstream impact status, and no stale corrected context flag. Reuse that rule.
- `DefaultProjectAiContextPackageAssembler` already produces metadata-only `ProjectAiContextPackage` manifests from tenant/project-scoped conversation items and excludes redacted, unauthorized, unsafe, pending, failed, unavailable, retryable, or policy-denied files.
- Existing UI components already render approval evidence freshness, disabled approve reasons, AI outcomes, blocked states, review history, safe next actions, and metadata-only previews. Reuse them before adding UI primitives.
- Existing worktree has unrelated modified `Hexalith.Tenants` submodule state and `_bmad-output/story-automator/orchestration-4-20260601-145742.md`; do not revert or include them.

### Architecture Guardrails

- Every state mutation must route through `CommandGateway`; UI/CLI/MCP/service/AI adapters submit typed commands through the client/gateway and must not replicate authorization, risk, approval, audit, idempotency, allowlist, correction-readiness, or refusal logic.
- Association, Governance, Lifecycle, Projections, and Audit communicate across seams by events/projections. Do not introduce direct cross-seam reach into internals to find or mutate proposals.
- Aggregates remain pure: no I/O, Dapr, logging, authorization, policy lookup, sibling client calls, AI provider calls, Folders reads, Conversations writes, or async inside `GovernedOperationAggregate.Handle`.
- Rejections for expected business-rule failures are structured domain results/events, not exceptions.
- Pre-commit audit is fail-closed. Invalidated approval/execution paths must not continue when audit readiness cannot be verified.
- Public responses and projections are metadata-only. Raw prompt/completion/provider payloads, file contents, raw email bodies, restricted filenames/paths, tenant IDs in denial bodies, raw policy body, raw audit detail, secrets, raw correction rationale, and raw exception text must not appear in API responses, UI, logs, fixtures, support artifacts, or tests.
- Use repo-pinned stack only: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, bUnit/Playwright where needed, Fluent UI v5 RC through existing FrontComposer patterns. Do not add inline package versions or upgrade dependencies.
- Root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Project Structure Notes

- Place new AI mediation event records under `src/Hexalith.ChatBot.Server/Governance/AiMediation/`.
- Keep public contracts under `src/Hexalith.ChatBot.Contracts/Commands`, `src/Hexalith.ChatBot.Contracts/Queries`, or `src/Hexalith.ChatBot.Contracts/Enums` only when the UI/API needs the data.
- Keep projection translation under `src/Hexalith.ChatBot.Server/Projections/`; do not put projection-only indexes in the aggregate.
- Mirror test locations under `tests/Hexalith.ChatBot.*.Tests/` by source boundary.
- If UI copy changes, update `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx` and `SharedResource.fr.resx` together.

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

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 4 is "Governed AI Action Mediation" and Story 4.9 closes correction invalidation after Stories 4.7 and 4.8 added allowlisted execution and safe-block behavior.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR33, FR35-FR46, FR55, FR57, FR59, denied/unsupported risk behavior, and governed AI execution journeys.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway order, event-only seam boundaries, governed AI mediation structure, message catalog requirements, audit envelope, fail-closed/idempotency/correlation guardrails, and testing standards.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`, especially corrected association state, AI Action Review, blocked state, audit timeline, Flow 8/9, state-to-feedback matrix, focus/live-region behavior, and mobile/forced-colors constraints.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant rules: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, metadata-only diagnostics, tenant isolation, pure EventStore aggregates, Dapr duplicate/order tolerance, FrontComposer/Fluent UI inheritance, Shouldly/NSubstitute/xUnit patterns, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/4-8-refusal-and-safe-block-behavior.md`; key carry-forward is to reuse `corrected-context-invalidated`, existing approved-execution rejection projection, blocked-state UI, and catalog-backed refusal semantics.
- Inspected current code and tests for likely update surfaces: correction events/readiness policy, `ProjectAiContextPackageAssembler`, `ProposeAIAction`, `ExecuteApprovedAIAction`, `AiActionProposalRecord`, `TaskIntentRecord`, approval events, approved-execution events, `GovernedOperationAggregate`, `GovernedOperationState`, AI outcome projection translators, approval/outcome/blocked UI components, gateway/aggregate/projection/conformance/E2E tests.
- Recent git history shows Story 4.8, 4.7, 4.6, 4.5, and 4.4 commits. The immediate baseline is `c8b7d54 feat(story-4.8): Refusal and safe block behavior`.
- Latest-technology web research was not required for story creation: this story adds no new external package, model, protocol, or framework and should use repo-pinned versions plus local code patterns.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.9 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR33, FR35-FR46, FR55, FR57, FR59, denied/unsupported risk behavior, governed AI execution.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, event-only seams, governed AI mediation, audit, fail-closed/idempotency/testing guardrails.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - approval panel, audit timeline, blocked state, status semantics, component rules.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - corrected association state, Flow 8/9, state-to-feedback matrix, focus/live-region behavior.
- `_bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md` - authorized current AI context package foundation.
- `_bmad-output/implementation-artifacts/4-7-allowlisted-command-execution.md` - approved execution allowlist, aggregate, dispatcher, projection learning.
- `_bmad-output/implementation-artifacts/4-8-refusal-and-safe-block-behavior.md` - refusal taxonomy, safe-block behavior, current-state preservation notes.
- `src/Hexalith.ChatBot.Server/Association/CorrectionPropagationStoreKeys.cs` - required correction propagation stores.
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/ProjectionCorrectedContextReadinessPolicy.cs` - corrected context readiness rule.
- `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/ProjectAiContextPackageAssembler.cs` - authorized current context package assembly.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotRefusalReasonCodes.cs` - existing `corrected-context-invalidated` reason code.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - proposal, approval, low-risk, and approved-execution aggregate behavior.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` - current task-intent, approval, execution, and correction propagation state.
- `src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs` - rejected approved-execution projection mapping.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor` - existing approval UI with disabled reason handling.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor` - existing AI lifecycle outcome UI.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor` - reusable blocked-state UI.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Built solution with warnings-as-errors and ran compiled xUnit v3 in-process runners for Contracts, Client, Server, UI, Architecture, Conformance, UI.E2E, AppHost, Aspire, ServiceDefaults, Testing, and Workers.

### Completion Notes List

- Added metadata-only correction invalidation command/event and aggregate state for invalidated AI proposals, including idempotent replay and conflicting duplicate rejection.
- Extended proposal records with safe correction lineage/context package tokens and derived proposal lineage from captured task intent plus safe metadata.
- Enforced fail-closed behavior for invalidated proposals in approval, low-risk assistance, and approved execution paths using `corrected-context-invalidated`.
- Projected proactive correction invalidations as append-only `CorrectedContextInvalidated` AI outcome rows with safe association/correction/evidence metadata only.
- Updated approval UI/localization so `corrected-context-invalidated` disables approval with EN/FR catalog-backed copy.
- Added focused aggregate, projection, and localization tests for invalidation, fail-closed approval/execution, idempotent replay, metadata-only projection, and EN/FR disabled reasons.
- Senior review fixed command-spine reachability for proposal invalidation, correction-driven proposal indexing/fan-out, and aggregate lineage checks so unrelated proposals cannot be invalidated by association/source-version mismatch.

### Senior Developer Review (AI)

Reviewer: Codex on 2026-06-01

Outcome: Approved after auto-fixes.

Findings fixed:

- HIGH: `MarkAiActionProposalInvalidatedByCorrection` was not admitted by `ChatBotSpineCommandAllowlist` and was not routed by `AcceptedCommandDispatcher`, so the new command could not reliably reach the source-message aggregate through the normal spine.
- HIGH: proposal invalidation accepted a matching project/source message but did not verify the proposal's recorded `AssociationId` and `EvidenceSnapshotSourceVersion`, allowing an unrelated correction to invalidate the wrong proposal.
- HIGH: correction projection had no bridge/index to discover affected AI proposals when an association correction arrived, so proactive correction-triggered invalidation was not actually wired.
- MEDIUM: story File List omitted changed implementation/test files: `_bmad-output/implementation-artifacts/tests/test-summary.md` and `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none`

Checklist validation: story, local architecture/standards, tech stack, acceptance criteria, File List, tests, code quality, and security/metadata-only constraints reviewed. External MCP/web documentation lookup was not applicable because the review added no new external package, protocol, provider API, or framework surface. Status and sprint tracking were updated after fixes.

---

Reviewer: Claude (Story Automator re-review) on 2026-06-10

Outcome: Approved after auto-fix. No CRITICAL or HIGH issues; 1 MEDIUM auto-fixed.

Git reality cross-check: story File List matches the actual story-4.9 change set (commit `33287c2`, parent baseline `c8b7d54`) exactly — 29 source/test files, no undisclosed changes, no false-claim files. Build is clean (warnings-as-errors, 0/0) and the validated suites pass: Server 1554, Contracts 480, UI 131, Conformance 87, all green.

Findings fixed:

- MEDIUM (test gap / projection-wiring): The AC6 invalidation→projection path was only proven by calling `ApprovedAiActionOutcomeProjectionTranslator.FromInvalidated(...)` directly. Every sibling event kind (`Started`, `Succeeded`, `Rejected`, low-risk variants) has an HTTP-endpoint wire test that posts a `PublishedAiActionExecutionEvent` to `AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute`, but the new `Invalidated` field and the `TryCreatePublishedEvents` Invalidated branch (`ApprovedAiActionOutcomeProjectionTranslator.cs`) had no end-to-end coverage, leaving the production correction-invalidation projection unproven. Added `ProjectionEndpointShouldApplyAiActionProposalInvalidatedByCorrectionDomainEvent` in `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`, which posts the invalidation envelope through the real subscriber endpoint and asserts the append-only `CorrectedContextInvalidated`/`Invalidated` row, `corrected-context-invalidated` failure/outcome codes, approval id, `review-source-evidence` safe next action, and association/correction context references. The test passes, empirically confirming the wire path is correctly wired (the finding was a coverage gap, not a code defect).

Verification of the end-to-end invalidation chain (no defects found): proposal indexed on publish (`TaskIntentProjectionHandler` → `UpsertAiActionProposalAsync`); association correction fan-out (`AssociationProjectionHandler` → `IAiActionProposalInvalidationCoordinator.InvalidateAsync` → deterministic `MarkAiActionProposalInvalidatedByCorrection` command, idempotent via `{correctionId}:{proposalId}` message id); spine admission (`ChatBotSpineCommandAllowlist`) and routing (`AcceptedCommandDispatcher`) to the source-message aggregate; aggregate guards proposal lineage (`AssociationId`, `EvidenceSnapshotSourceVersion`, project, requester) before invalidating and fails closed on approval/low-risk/approved-execution when invalidated; pure aggregate `Handle` (no I/O); metadata-only command/event/projection tokens; EN/FR disabled-reason parity and approve-button disablement.

Checklist validation: story, architecture/standards, tech stack, acceptance criteria, File List vs git, tests mapped to ACs, code quality, and security/metadata-only constraints reviewed. Story status remains `done` (0 CRITICAL remaining); sprint tracking already `done`.

### File List

- `_bmad-output/implementation-artifacts/4-9-correction-invalidates-ai-action-proposals.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Contracts/Commands/MarkAiActionProposalInvalidatedByCorrection.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageNextActions.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AiActionProposalRecord.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionProposalInvalidatedByCorrection.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiActionProposalInvalidationCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IAiActionProposalInvalidationCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAiActionExecutionEvent.cs`
- `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionHandler.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`

### Change Log

- 2026-06-01: Implemented Story 4.9 corrected-context invalidation for governed AI proposals, approvals, execution blocking, projection, UI disabled reason copy, and focused regression coverage.
- 2026-06-01: Senior review auto-fixed invalidation command spine routing, proposal lineage validation, correction-triggered proposal invalidation fan-out, and File List completeness.
- 2026-06-10: Story Automator re-review auto-fixed a projection-wiring test gap by adding an end-to-end HTTP-endpoint wire test for the corrected-context invalidation projection (`AiOutcomeProjectionTests.ProjectionEndpointShouldApplyAiActionProposalInvalidatedByCorrectionDomainEvent`); confirmed File List vs git parity and green build/test suites (Server/Contracts/UI/Conformance). Status remains done.
