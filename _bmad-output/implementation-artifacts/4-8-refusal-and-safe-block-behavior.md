---
baseline_commit: b812b4c
---

# Story 4.8: Refusal and safe-block behavior

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As a security owner,
I want unsafe AI/automation/command/mailbox requests refused with a safe, audited message,
so that boundary-crossing attempts are blocked and traceable.

## Acceptance Criteria

1. Given a request exceeds tenant policy, project authorization, sender authority, or approved command scope, when it is evaluated by the gateway, AI mediation, command execution, mailbox intake, or projection/read surface, then the request is refused or blocked before unsafe durable mutation or external effect, using a catalog-backed user-safe message and stable reason code. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.8`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR46`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR77`]
2. Given the refusal is security-sensitive, when it is blocked, then an auditable denial fact or event preserves tenant, actor, command/action, resource reference, decision, reason code, correlation ID, policy snapshot when available, source evidence refs, redaction decision, and surface origin without exposing restricted project/file/party/audit/prompt/provider detail to the caller or UI. [Source: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR50a`]
3. Given association is unresolved, actor identity is unresolved or unauthorized, corrected context is stale, required AI context is missing, evidence is expired, policy snapshot is unavailable, or approval/allowlist metadata is missing or stale, when an AI action is requested, proposed, approved, or executed, then the system refuses, asks for association resolution or additional files, routes to approval where policy allows, or records a blocked outcome instead of fabricating, widening scope, or silently proceeding. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#System Journey - Governed AI execution`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow 9 - Governed AI execution`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR16`]
4. Given a command/action is unsupported in M0, outside the current approved AI-action command allowlist, outside approved scope, or attempts M1/M2 behavior such as outbound send, arbitrary tool invocation, project creation, task automation, or broad document intelligence, when submitted from UI, service client, worker, CLI shim, MCP shim, or AI actor path, then it fails closed with equivalent redacted refusal semantics across surfaces and no idempotency admission, dispatch, sibling service call, provider call, or success projection. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR46`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a`; `_bmad-output/implementation-artifacts/4-7-allowlisted-command-execution.md#Current State To Preserve`]
5. Given a refused, blocked, denied, degraded, failed, or waiting state reaches a user-facing API response, project conversation row, S3 approval surface, blocked-state component, operation status, queue row, or support diagnostic, when it renders, then the message comes from the versioned message catalog, has a stable code, headline <=80 chars, one-sentence safe reason, finite disabled-action reason, safe next action, metadata-only visibility, English/French labels where UI text is added, and zero raw error text. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR77`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR40`; `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`]
6. Given refusal or safe-block outcomes are projected into the conversation or AI lifecycle, when events arrive out of order, duplicate, stale, or partially enriched, then append-only rows remain immutable, duplicate delivery is idempotent, stale replay cannot overwrite newer detail, lifecycle grouping still reconstructs by proposal/approval/execution/correlation IDs, and blocked outcomes remain visible as denials/refusals rather than disappearing. [Source: `_bmad-output/implementation-artifacts/4-6-ai-action-preview-and-inspection.md#Current State To Preserve`; `_bmad-output/implementation-artifacts/4-7-allowlisted-command-execution.md#Current State To Preserve`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`]
7. Given blocked or denied state is visible in the UI, when tested with keyboard, screen reader semantics, reduced motion, forced colors, English/French text, and phone/tablet widths, then the blocked explanation and next action are reachable without hover-only detail, current-user terminal/policy denials announce assertively, observed historical rows do not announce on initial load, focus stays in the relevant review panel, and text does not overlap. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback matrix`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components`]
8. Given this story completes, when acceptance coverage runs, then tests prove refusal taxonomy coverage, audit-denial recording, catalog safety, no sensitive leakage, fail-closed ordering before mutation/effect, unresolved/missing-context AI behavior, unsupported and non-allowlisted command blocking, projection idempotency, surface parity, and UI accessibility without implementing Story 4.9 correction invalidation beyond refusing stale/corrected-context inputs already represented. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.9`; `_bmad-output/planning-artifacts/architecture.md#Implementation Handoff`; `_bmad-output/implementation-artifacts/4-7-allowlisted-command-execution.md#Testing Notes`]

## Tasks / Subtasks

- [x] Define the refusal taxonomy and map it to existing catalog semantics (AC: 1, 3, 5, 8)
  - [x] Add or normalize stable reason codes for the M0 refusal cases: `tenant-policy-exceeded`, `project-authorization-denied`, `sender-authority-denied`, `approved-command-scope-exceeded`, `command-not-allowlisted`, `unsupported-action`, `unresolved-association`, `unresolved-participant`, `missing-required-context`, `context-package-unavailable`, `evidence-expired`, `policy-snapshot-unavailable`, `approval-state-invalid`, `corrected-context-invalidated`, and `dependency-degraded`.
  - [x] Keep code names safe metadata tokens. Do not encode tenant IDs, project IDs, filenames, email addresses, raw policy text, raw prompt fragments, Graph payloads, or exception messages in user-facing reason codes.
  - [x] Reuse existing catalog entries where the user-facing meaning is identical: `refusal_blocked_action`, `authorization_denied`, `unresolved_participant`, `participant_directory_degraded`, `project_ai_context_package_unavailable`, `audit_unavailable`, `failed_command`, and `dependency_degraded`.
  - [x] Add catalog entries only when the current copy would make the next action ambiguous. Any new entry must pass `MessageCatalogContractTests`: stable code, headline <=80 chars, one-sentence reason, finite disabled reason, safe next action, metadata-only visibility.
- [x] Enforce fail-closed refusal before unsafe mutation or external effect (AC: 1, 2, 4, 8)
  - [x] Extend the existing `CommandGateway`, `ParticipantAuthorizationStage`, `DeterministicAiActionRiskClassifier`, `AiActionApprovalGate`, `AcceptedCommandDispatcher`, and approved-execution aggregate path rather than adding a parallel refusal pipeline.
  - [x] For gateway-level refusals before idempotency or dispatch, preserve the Story 4.7 behavior: no coarse idempotency record, no dispatcher call, no sibling service call, no provider call, and a catalog-backed `ProblemDetails`.
  - [x] For post-admission business-rule refusals inside `GovernedOperationAggregate`, return structured `IRejectionEvent` payloads such as `ApprovedAiActionExecutionRejected`; do not throw for expected policy/scope/context failures.
  - [x] For dispatch/dependency failures, keep the pre-commit-audit contract: abort or release idempotency as existing gateway semantics require, queue replay/alert only for audit/dispatch failure paths that already own that behavior, and return redacted catalog-backed responses.
  - [x] Do not weaken `ChatBotSpineCommandAllowlist` or `ApprovedAiActionCommandAllowlist`. M0 approved AI execution remains exactly `Project.AppendConversationMessage`.
- [x] Record auditable denial facts and project blocked outcomes where appropriate (AC: 2, 6, 8)
  - [x] Reuse `ChatBotAuthorizationFailureAuditFact` and `AuditEnvelopeFactory` for gateway/security denials where the operation never reaches the aggregate.
  - [x] If aggregate-level AI refusal needs durable lifecycle visibility, add focused refusal/blocked event shapes under `src/Hexalith.ChatBot.Server/Governance/AiMediation/` and translate them through existing AI outcome projection paths.
  - [x] Extend `PublishedAiActionExecutionEvent` and `ApprovedAiActionOutcomeProjectionTranslator` only if existing `ApprovedAiActionExecutionRejected` cannot be observed by projections. Keep projection payloads metadata-only.
  - [x] AI lifecycle rows should use existing `AiOutcomeKind` tokens where possible: `denial`, `refusal`, `execution-failed`, `corrected-context-invalidated`, and `outcome-recorded`; add enum/contract changes only if a distinct existing token cannot represent the state.
  - [x] Preserve append-only and superseded-not-mutated semantics for proposals, approvals, outcomes, and blocked rows.
- [x] Refuse missing or unsafe AI context without fabrication (AC: 3, 5, 6, 8)
  - [x] Use existing corrected-context readiness checks, task-intent reason codes, participant authority claims, project authorization checks, context-package metadata, evidence freshness, and approval-state data instead of asking an AI provider to infer safety.
  - [x] Ensure unresolved association, unresolved participant, missing source evidence, missing context package, stale corrected context, expired evidence, and unavailable policy snapshot produce deterministic safe next actions such as `request-access`, `retry-later`, `correct-request`, `escalate`, or `none`.
  - [x] Do not call an AI provider, M365 outbound path, Folders raw content path, arbitrary tool path, or Conversations writer when refusal preconditions are not met.
  - [x] If a mixed request includes a denied part, deny the whole request unless the denied portion can be safely separated and audited with no scope widening.
- [x] Surface refusals through existing UI and read-model primitives (AC: 5, 6, 7)
  - [x] Reuse `ChatBotBlockedState.razor`, `ChatBotAiOutcomeConversationItem.razor`, `ChatBotApprovalConversationItem.razor`, `ChatBotConversationItemStatusSummary.razor`, and `ChatBotConversationItemReviewHistory.razor` before creating new UI components.
  - [x] Extend `ProjectConversationItemView`, `AiOutcomeProjectionTranslator`, `ApprovedAiActionOutcomeProjectionTranslator`, and UI service models only as needed for refusal reason, disabled reason, safe next action, audit status, proposal/approval/execution IDs, and correlation.
  - [x] Keep blocked-state copy reachable inline. Do not rely on tooltip-only explanations or color-only danger styling.
  - [x] Add EN/FR localization keys if new UI labels or reason text are introduced; verify long French strings fit on phone/tablet and forced-colors.
  - [x] Preserve current focus/live-region behavior: current-user terminal denials assertive, command/projection pending polite, historical or observed rows inline only.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests for any new message codes, disabled reasons, next actions, enum wire tokens, public query fields, OpenAPI/generated-client hash if public contracts change, and metadata-only serialization.
  - [x] Gateway tests for tenant-policy/project-auth/sender-authority/command-scope/non-allowlisted/unsupported refusals proving redacted problem details, audit-denial facts where required, no idempotency admission, no dispatch, no sibling/provider calls, and no sensitive strings.
  - [x] Aggregate tests for approval unavailable, non-approved decisions, stale/expired evidence, command/allowlist mismatch, corrected-context invalidated, missing context, equivalent replay, and conflicting duplicate refusal semantics.
  - [x] Projection tests for refusal/denial/blocked AI outcome rows, out-of-order delivery, duplicate replay, stale replay, tenant isolation, lifecycle review-history reconstruction, and leakage sentinels.
  - [x] Conformance tests proving refusal parity across UI/CLI/MCP/service/AI adapter shims except audited origin.
  - [x] UI/bUnit/E2E tests for blocked state, S3/project-conversation refusal rendering, reachable disabled reasons, live-region politeness, focus retention, EN/FR labels, forced-colors, reduced-motion, phone/tablet no-overlap, and no raw restricted data in rendered markup.

## Dev Notes

### Scope Boundaries

- This story owns FR46 for M0: refuse or block unsafe AI, automation, command, or mailbox requests that exceed tenant policy, project authorization, sender authority, or approved command scope.
- This story may add refusal taxonomy constants, message catalog entries, focused aggregate rejection events, projection translation for blocked AI lifecycle rows, UI/localization for blocked outcomes, and tests.
- This story must not implement Story 4.9 correction invalidation broadly, M1 outbound draft/send, sender-authority mapping enforcement beyond safe refusal placeholders, tenant policy editor UI, command allowlist mutation, arbitrary tools, autonomous project/task automation, broad document intelligence, or production CLI/MCP adapters.
- Sender-authority refusal is an M0 safe-block contract, not a full M1 outbound authority implementation. If the path is not supported yet, block with metadata-only `unsupported-action` or `sender-authority-denied` semantics and no outbound side effect.
- The correct implementation path is to extend existing gateway/aggregate/projection/UI primitives. A second refusal pipeline, separate message catalog, parallel blocked-state component, or direct UI policy evaluator would violate the architecture.

### Existing Code To Reuse

- Message catalog and redacted API problems:
  - `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
  - `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
  - `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs`
  - `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageNextActions.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- Gateway, risk, approval, idempotency, and audit:
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationFailureAuditFact.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/DeterministicAiActionRiskClassifier.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
- AI mediation and approved execution:
  - `src/Hexalith.ChatBot.Contracts/Commands/ExecuteApprovedAIAction.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/ApprovedAiActionExecutionRecord.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionCommandMetadataProvider.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/ApprovedAiActionCommandAllowlist.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/ApprovedAiActionExecutionEvents.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentReasonCodes.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- Context, correction, projection, and UI:
  - `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/ProjectionCorrectedContextReadinessPolicy.cs`
  - `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/ProjectAiContextPackageAssembler.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/PublishedAiActionExecutionEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor`
- Existing tests to extend:
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
  - `tests/Hexalith.ChatBot.Conformance.Tests/RejectionIntentParityTests.cs`
  - `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`

### Current State To Preserve

- Story 4.7 added `ExecuteApprovedAIAction`, `ApprovedAiActionExecutionRecord`, a distinct `ApprovedAiActionCommandAllowlist`, approved-execution events, dispatcher integration, and projection translation. Extend these rather than creating a second approved-execution or refusal model.
- `CommandGateway` currently rejects non-spine-allowlisted commands after authorization and before risk/idempotency/dispatch with `refusal_blocked_action`, records an authorization-failure fact, and skips idempotency/dispatch.
- `DeterministicAiActionRiskClassifier` and gateway tests already prove unsupported approved AI execution commands fail closed before idempotency. Preserve this ordering.
- `ParticipantAuthorizationStage` already blocks unresolved, email-only, unauthorized, or directory-degraded participants and denies low-risk/approved AI execution when project read authority is absent.
- `GovernedOperationAggregate.Handle(ExecuteApprovedAIAction)` already rejects non-allowlisted command metadata, corrected-context invalidation, duplicate conflicts, unavailable approval, non-approved decisions, and non-fresh approval evidence with structured `ApprovedAiActionExecutionRejected` events.
- `AcceptedCommandDispatcher` prepares `Project.AppendConversationMessage` metadata via `IConversationWriter` only after gateway admission and pre-commit audit. Refusals must not call the writer.
- `ChatBotMessageCatalog` already includes `refusal_blocked_action`, `authorization_denied`, `unresolved_participant`, `participant_directory_degraded`, `project_ai_context_package_unavailable`, `audit_unavailable`, `failed_command`, and `dependency_degraded`. Prefer these where precise enough.
- `ChatBotBlockedState.razor` already renders reason, next action, live-region metadata, and accessible label. Reuse it for user-visible blocked states.
- Existing worktree has unrelated modified `Hexalith.Tenants` submodule state and `_bmad-output/story-automator/orchestration-4-20260601-145742.md`; do not revert or include them.

### Architecture Guardrails

- Every state mutation must route through `CommandGateway`; UI/CLI/MCP/service/AI adapters submit typed commands through the client/gateway and must not replicate authorization, risk, approval, audit, idempotency, allowlist, or refusal logic.
- Pre-commit audit is fail-closed. No state-mutating path may continue when audit readiness cannot be verified.
- Aggregates remain pure: no I/O, Dapr, logging, authorization, policy lookup, sibling client calls, AI provider calls, or async inside `GovernedOperationAggregate.Handle`.
- Rejections for expected business-rule failures are structured domain results, not exceptions. Exceptions are only for programmer/infrastructure failures already handled by gateway redaction.
- Public responses and projections are metadata-only. Raw prompt/completion/provider payloads, file contents, raw email bodies, restricted filenames/paths, tenant IDs in denial bodies, raw policy body, raw audit detail, secrets, and raw exception text must not appear in API responses, UI, logs, fixtures, support artifacts, or tests.
- Reuse `System.Text.Json` and repo-pinned stack only: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, bUnit/Playwright where needed, Fluent UI v5 RC through existing FrontComposer patterns. Do not add inline package versions or upgrade dependencies.
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

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 4 is "Governed AI Action Mediation" and Story 4.8 follows allowlisted execution (4.7) and precedes correction invalidation (4.9).
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR46, FR77, FR81a, NFR15a, NFR16, NFR40, NFR44, NFR48, NFR50a, NFR62, and governed AI execution journeys.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially command allowlist, shared command pipeline, tenant policy schema, idempotency keys, and inbound/sender authority mapping notes.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway order, governed AI mediation, problem/error response rules, audit envelope, fail-closed/idempotency/correlation guardrails, project structure, and testing standards.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`, especially blocked state, AI action review, Flow 8/9, state-to-feedback matrix, focus/live-region behavior, and mobile/forced-colors constraints.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant rules: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, metadata-only diagnostics, tenant isolation, pure EventStore aggregates, Dapr duplicate/order tolerance, FrontComposer/Fluent UI inheritance, Shouldly/NSubstitute/xUnit patterns, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/4-7-allowlisted-command-execution.md`; key carry-forward is to extend the existing approved-execution allowlist, dispatcher, aggregate rejection, and AI outcome projection paths rather than adding parallel refusal code.
- Inspected current code and tests for likely update surfaces: message catalog files, `ChatBotProblemDetailsFactory`, `CommandGateway`, `ParticipantAuthorizationStage`, `AiActionApprovalGate`, `DeterministicAiActionRiskClassifier`, `AcceptedCommandDispatcher`, `ApprovedAiActionCommandAllowlist`, approved-execution events/contracts, `GovernedOperationAggregate`, `GovernedOperationState`, AI outcome projection translators, blocked-state/approval/outcome UI components, gateway/aggregate/projection/conformance/E2E tests.
- Recent git history shows Story 4.7, 4.6, 4.5, 4.4, and 4.3 commits. The immediate baseline is `b812b4c feat(story-4.7): Allowlisted AI command execution`.
- Latest-technology web research was not required for story creation: this story adds no new external package, model, protocol, or framework and should use repo-pinned versions plus local code patterns.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.8 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR46, FR77, FR81a, NFR15a, NFR16, NFR40, NFR44, NFR48, NFR50a, NFR62, governed AI execution journeys.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - command allowlist, shared command pipeline, tenant policy schema, idempotency, authority mapping.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, governed AI mediation, message catalog requirements, audit envelope, fail-closed/audit/idempotency/testing guardrails.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - blocked state, approval panel, audit timeline, component rules.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - Flow 8/9, state-to-feedback matrix, focus/live-region behavior, interaction constraints.
- `_bmad-output/implementation-artifacts/4-6-ai-action-preview-and-inspection.md` - lifecycle inspection and blocked/metadata-only projection learning.
- `_bmad-output/implementation-artifacts/4-7-allowlisted-command-execution.md` - approved execution implementation context, allowlist separation, dispatcher/projection learning, validation evidence.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` - versioned message catalog.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - current gateway ordering and fail-closed refusal behavior.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - unresolved/unauthorized participant and project-read blocking.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs` - approved execution writer boundary after gateway admission.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/ApprovedAiActionCommandAllowlist.cs` - current M0 approved AI command allowlist.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - approved-execution aggregate rejection behavior.
- `src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs` - approved execution lifecycle projection mapping.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor` - reusable blocked-state UI.
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs` - catalog safety contract.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - existing fail-closed gateway and approved execution tests.
- `tests/Hexalith.ChatBot.Conformance.Tests/RejectionIntentParityTests.cs` - existing cross-surface redacted rejection parity.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01T22:14:28+02:00 - Marked story 4.8 in progress in sprint status; preserved existing `baseline_commit: b812b4c`.
- 2026-06-01T22:22:37+02:00 - Validation complete. Build and compiled xUnit runners passed; integration runner reported expected gated Tier-3 skips.
- 2026-06-01T22:33:34+02:00 - Senior Developer Review complete. Fixed incomplete File List documentation and synced sprint status to done.

### Completion Notes List

- Added finite M0 refusal taxonomy constants and catalog mapping without introducing new catalog entries or public enum wire tokens.
- Normalized unsupported AI action and approved-execution aggregate rejection reasons to stable hyphenated refusal tokens.
- Extended approved-execution rejection events with metadata-only lifecycle context and translated them into existing `AiOutcomeKind.Refusal` blocked rows.
- Preserved fail-closed gateway ordering before idempotency/dispatch and reused existing authorization failure audit facts, problem-details redaction, blocked-state UI, and projection primitives.
- Added/updated contract, risk classifier, aggregate, and projection tests for taxonomy coverage, safe metadata-only rejections, and projected refusal rows.
- Senior Developer Review found no remaining critical/high implementation issues after validation. One medium documentation issue was fixed: the File List omitted the changed Story 4.8 E2E test and test summary artifact.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved. Status set to done.

Findings:

- [x] [AI-Review][Medium] Story File List was incomplete relative to git changes: `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` and `_bmad-output/implementation-artifacts/tests/test-summary.md` were changed but not documented. Fixed by adding both files to the File List.

Validation performed:

- Story status was reviewable (`review`) before this review.
- Acceptance Criteria and completed tasks were cross-checked against the changed implementation and focused tests.
- Git/story discrepancies were reviewed. Unrelated existing changes in `Hexalith.Tenants` and `_bmad-output/story-automator/orchestration-4-20260601-145742.md` were intentionally not included or reverted per the story note.
- MCP doc search was attempted; no MCP resources are configured in this session. No web fallback was required because the story uses only repo-pinned .NET/project APIs and adds no external dependency.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` passed: 100 passed.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` passed: 15 passed.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` passed: 430 passed.
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` passed: 97 passed.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` passed: 35 passed.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` passed: 58 passed.
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` passed: 50 passed.

#### Re-review (story-automator) — Jérôme Piquot on 2026-06-10

Outcome: Approved. Status remains done (0 critical/high issues).

Adversarial re-review against the current tree. All eight acceptance criteria were re-validated against the committed implementation, and the full validation set was re-run on the current code: build clean (0 warnings/0 errors); Contracts 480, Server 1553, Conformance 87, Architecture 39, UI 131, Client 34, UI.E2E 80 — 0 failed, 0 skipped.

Findings:

- [x] [AI-Review][Medium] The new fail-closed `PolicySnapshotUnavailable` branch in `GovernedOperationAggregate.Handle(ExecuteApprovedAIAction)` (refuses when neither the command nor the approval request carries a policy snapshot — AC3) had no dedicated test; the 4.8 aggregate-test changes only renamed reason-code assertions on pre-existing branches. Fixed by adding `HandleApprovedAiActionExecutionShouldRejectWhenPolicySnapshotUnavailable` and threading an optional `policySnapshotId` (default unchanged) through the `ApprovalRequest`/`ApprovedExecutionState` test helpers. New test passes; full suite green.

Verified (no change required):

- Projection wiring is real, not dead code: the `ApprovedAiActionExecutionRejected` → `FromRejected` → `AiOutcomeKind.Refusal/Blocked` path is exercised end-to-end through the live Dapr subscriber endpoint by `AiOutcomeProjectionTests.ProjectionEndpointShouldApplyApprovedAiActionExecutionRejectionDomainEvent`, mirroring the shipped Started/Succeeded/Failed/Invalidated branches.
- Refusal taxonomy is contract-tested (`MessageCatalogContractTests.RefusalReasonTaxonomyShouldBeFiniteSafeAndCatalogBacked`); every `ChatBotRefusalReasonCodes` token resolves to an existing, safe catalog entry.
- Reason-code value change `command_not_allowlisted` → `command-not-allowlisted` is safe: `ChatBotAuthorizationReasonCodes` is internal, API problem bodies use the catalog code, and conformance parity references the symbol.
- No-leakage is covered at the surface that matters: `AiOutcomeProjectionTests.ShouldRedactPolicyAndAuditIdentifiers...` and `...ShouldSuppressUnsafeOptionalTokensAndNeverLeakPromptOrProviderText`. Aggregate-level `SafeRejectionToken` is redundant defense-in-depth behind payload validation.

Observations (not fixed; out of story scope):

- `tests/.../Gateway/CommandGatewayAdmissionApiE2ETests.cs` was originally introduced by the 4.7 re-commit, but the current metadata-only denial fact test is a Story 4.8 automation addition and is included in this story's File List.
- `tests/.../Audit/ChainedAuditWriterTests.cs` uses the legacy underscore literal `"command_not_allowlisted"` as arbitrary sample data; cosmetic, non-breaking, outside the 4.8 surface.

### File List

- `_bmad-output/implementation-artifacts/4-8-refusal-and-safe-block-behavior.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotRefusalReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionRiskClassifier.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/ApprovedAiActionExecutionEvents.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovedAiActionOutcomeProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAiActionExecutionEvent.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionRiskClassifierTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`

### Change Log

- 2026-06-01 - Implemented refusal taxonomy, normalized safe-block reason codes, projected approved-execution rejections, and added focused validation coverage. Status: review.
- 2026-06-01 - Senior Developer Review approved the implementation, fixed File List documentation, and synced story/sprint status. Status: done.
- 2026-06-10 - Story-automator re-review: re-validated all ACs and re-ran the full suite (green). Added a missing focused test for the PolicySnapshotUnavailable fail-closed branch. No critical/high issues; status remains done.
