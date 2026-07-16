---
baseline_commit: f6c79ba251ec97a59835cc511271ba2c0141b444
---

# Story 3.7: Failure, retry, and blocked-state rendering

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want failures, retries, and blocked states represented in the project conversation,
so that I can see recoverable work and the next safe action.

## Acceptance Criteria

1. Given a failure, retry, or blocked-state conversation item, when it renders on S1, then the item exposes actor attribution, an actor-type label before content in the accessible name, evidence/risk/status/actor/timestamp ordering, non-color status text, reduced-motion behavior, and WCAG 2.2 AA behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.7; _bmad-output/planning-artifacts/epics.md#UX-DR41]
2. Each failure/retry/blocked item renders a catalog-backed user-facing message using `ChatBotMessageCatalogVersion.Current`, stable `ChatBotMessageCodes`, catalog headline/reason/next-action/disabled-reason/detail-visibility metadata, and localized EN/FR labels. Raw exception text, stack traces, provider diagnostics, hidden tenant/project/file/party/audit/payload text, and uncategorized messages must not reach API, UI, logs, snapshots, or E2E fixtures. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.7; _bmad-output/planning-artifacts/epics.md#Story 1.7; _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
3. Retryable states render retry count, retryability, duplicate-safety note, operation/workflow identity, correlation id, and safe next action. Terminal states render terminal reason, escalation/manual-resolution path, audit availability/status, and the Story 1.6 reprocess/new-workflow-instance rule without implying the terminal item can move backward. [Source: _bmad-output/planning-artifacts/epics.md#Story 2.9; _bmad-output/planning-artifacts/epics.md#Story 1.6; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]
4. Blocked states render a safe blocked reason and reachable explanation for authorization failure, policy block, unresolved participant, dependency degraded, evidence expired/stale, correction delayed, projection unavailable/retryable, audit unavailable, duplicate suppressed, retry exhausted, and reprocess-created states without confirming restricted resource existence. [Source: _bmad-output/planning-artifacts/epics.md#FR16; _bmad-output/planning-artifacts/epics.md#FR77; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]
5. Failure/retry/blocked items are metadata-only append-only history. A retry attempt, retry acceptance, retry exhaustion, duplicate suppression, terminal failure, or reprocess-created event must not mutate, hide, or replace the original failed/blocked event except by deterministic same-item/source-version replacement for the same event version. [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture; _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]
6. S1 preserves Stories 3.1 through 3.6 behavior: tenant/project partitioning, cursor pagination, source-email enrichment, participant/attachment materialization, association/correction decision history, approval event rendering, source-version replay safety, stale/correction safe-next-action behavior, EN/FR localization, responsive layout, forced-colors, reduced-motion, and UI state clearing on route load/failure. [Source: _bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md#Current-State-To-Preserve; _bmad-output/implementation-artifacts/3-6-approval-event-rendering.md#Current-State-To-Preserve]
7. Contract, generated client, server projection, conformance, UI service/state/component, localization, static CSS, and UI E2E coverage prove failure/retry/blocked rendering is localized, accessible, catalog-backed, metadata-only, append-only, replay-safe, raw-error-safe, and cross-tenant isolated. [Source: _bmad-output/planning-artifacts/epics.md#FR22; _bmad-output/planning-artifacts/epics.md#FR25; _bmad-output/planning-artifacts/epics.md#NFR60]

## Tasks / Subtasks

- [x] Extend the S1 contract spine for failure/retry/blocked conversation items (AC: 1, 2, 3, 4, 7)
  - [x] Add a distinct additive item kind for this story. Recommended wire token: `failure-state`; do not reuse `system-decision`, `approval-event`, `attachment`, `participant`, or `email-derived`.
  - [x] Add a distinct actor kind if needed. Recommended wire token: `system-status` or `reliability-system`; preserve all existing actor wire tokens.
  - [x] Add additive `ProjectConversationItem` fields for failure/retry/blocked metadata. Suggested names: `failureStateKind`, `failureStatus`, `messageCatalogCode`, `messageCatalogVersion`, `messageDetailVisibility`, `failureCategory`, `failureScope`, `failureReasonCode`, `blockedReason`, `retryable`, `retryCount`, `maxRetryCount`, `nextRetryAtUtc`, `lastRetryAtUtc`, `retryOperationId`, `workflowInstanceId`, `supersedesWorkflowInstanceId`, `supersededByWorkflowInstanceId`, `taskId`, `operationId`, `auditOperationId`, `auditStatus`, `clientAction`, `safeNextAction`, `duplicateSafetyState`, `duplicateSuppressionId`, `dependencyName`, `degradedUntilUtc`, `escalationTargetRole`, and `reprocessCreatedWorkflowInstanceId`.
  - [x] Add stable enum wire tokens where useful: failure state kind (`failure`, `retry-queued`, `retry-accepted`, `retry-exhausted`, `blocked`, `duplicate-suppressed`, `dependency-degraded`, `projection-retryable`, `terminal-failure`, `reprocess-created`), failure status (`retryable`, `terminal`, `blocked`, `degraded`, `resolved`, `unknown`), detail visibility (`metadata_only`), and blocked reason values aligned with `ChatBotDisabledActionReasons`.
  - [x] Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit generated client output.
  - [x] Add contract/OpenAPI/generated-client tests proving the fields are additive, wire values are stable, existing item tokens are unchanged, and no raw `exception`, `stackTrace`, `providerDiagnostic`, `payload`, `prompt`, `output`, `policyBody`, `auditEnvelope`, or hidden resource-name fields exist.
- [x] Bind rendering to the existing versioned message catalog (AC: 2, 4, 7)
  - [x] Reuse `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotMessageCodes.cs`, `ChatBotMessageCatalogVersion.cs`, `ChatBotMessageNextActions.cs`, `ChatBotDisabledActionReasons.cs`, and `ChatBotDetailVisibility.cs`; extend them only for missing finite codes required by this story.
  - [x] Existing relevant codes include `DuplicateSuppressed`, `RetryQueued`, `RetryAccepted`, `RetryExhausted`, `TerminalFailure`, `RecoverableMailboxDegradation`, `ProjectionRetryable`, `ReprocessCreated`, `AuditUnavailable`, `DependencyDegraded`, `RefusalBlockedAction`, `FailedAttachment`, `FailedCommand`, association correction propagation codes, participant authorization/resolution codes, and association evidence/candidate suppression codes.
  - [x] UI must render localized EN/FR labels and explanations from UI localization resources keyed by catalog code/next action/disabled reason; stable machine codes may appear only in intended metadata fields.
  - [x] Preserve `MessageCatalogContractTests`: headline <= 80 chars, one-sentence safe reason, finite next action, finite disabled reason, metadata-only detail visibility, and restricted-text negative assertions.
- [x] Add a projection source and materialize S1 failure-state items (AC: 2, 3, 4, 5, 6, 7)
  - [x] Introduce projection-side source/view types such as `FailureStateEventView`, `PublishedFailureStateEvent`, `FailureStateProjectionTranslator`, and/or `FailureStateProjectionHandler` under `src/Hexalith.ChatBot.Server/Projections/`.
  - [x] Keep this source metadata-only and projection-facing. Consume EventStore-stamped tenant/domain/aggregate/source-version/correlation metadata, not route/body/query tenant/project values.
  - [x] Materialize deterministic item ids, for example `failure:{operationId}:{failureStateKind}:{sourceVersion}` or `failure:{workflowInstanceId}:{messageCatalogCode}:{sourceVersion}`. Use opaque tenant/project-scoped ids; do not embed raw error text or restricted names in ids.
  - [x] Store materialized items in the existing `IProjectConversationProjectionStore`; do not add a transcript table, retry-specific browser data plane, direct EventStore reader, direct audit reader, or direct operational-queue reader from S1.
  - [x] Preserve DAPR/CloudEvent duplicate and out-of-order tolerance: duplicate source version is idempotent, stale replay cannot overwrite newer item state, and retry/terminal/reprocess events arriving before an initial failure still render safe metadata-only history and can be enriched later.
  - [x] Link failure/retry/blocked events to governed source context using stable ids only: project id, source message id, source conversation item id, association id, attachment id/file id when authorized, approval id, operation id, task id, workflow instance id, audit operation id, message catalog code, and correlation id.
- [x] Represent retry, terminal, blocked, audit, and authorization detail safely (AC: 2, 3, 4, 5, 7)
  - [x] Retryable rows show retry count, retryability, next retry time when present, duplicate-safety note, and safe next action. Do not implement the retry execution command unless an existing command already exists and is already authorized through the CommandGateway.
  - [x] Terminal rows show terminal reason code, manual-resolution/escalation path, and audit status. Do not show a retry control for terminal state; if reprocess is available, show only the new workflow/reference metadata supplied by the projection.
  - [x] Blocked rows show reachable reason text for policy, authorization, unresolved participant, stale/evidence-expired, correction delayed, projection unavailable, audit unavailable, dependency degraded, retry exhausted, and unsafe context. Tooltip-only explanations are not acceptable.
  - [x] Hide or redact restricted ids unless the projection explicitly marks them authorized. Redacted/unavailable messages must not confirm hidden project, file, participant, recipient, policy, audit, prompt, output, command payload, or provider-error existence.
  - [x] Keep failure rows append-only. A later retry/result/reprocess item may link to prior items, but it must not mutate the prior item into a new state or erase failure history.
- [x] Update UI mapping and dedicated failure-state rendering components (AC: 1, 2, 3, 4, 6, 7)
  - [x] Extend `ProjectConversationItemModel` and `ProjectConversationService.MapItem` to carry failure-state metadata through `IChatBotClient` only.
  - [x] Update `ChatBotConversationStream.razor` to route failure-state items to a dedicated `ChatBotFailureStateConversationItem.razor`; do not overload `ChatBotDecisionConversationItem`, `ChatBotApprovalConversationItem`, or `ChatBotBlockedState` directly as the row component.
  - [x] Reuse governed primitives: `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotStatusBanner`, `ChatBotBlockedState`, stable `<time>` elements, definition-list metadata, localized labels, and governed layout classes.
  - [x] Actor label must lead the accessible name. Recommended shape: "System status, <localized catalog headline>, <failure status>, <timestamp>".
  - [x] Keep evidence/risk/status/actor/timestamp ordering consistent with existing S1 rows. Plain-language labels precede IDs; IDs remain available as metadata.
  - [x] Add EN/FR resource keys for failure state kind, failure status, catalog headline/reason/next action labels, retry labels, duplicate-safety labels, blocked reason labels, terminal/escalation labels, audit availability labels, accessible names, and metadata labels. Avoid concatenated strings for accessible names.
- [x] Maintain S1 responsive, visual, and accessibility behavior (AC: 1, 4, 6, 7)
  - [x] Extend `chatbot.tokens.css` using existing Fluent/FrontComposer tokens only; do not introduce raw `#`, `rgb(`, or `hsl(` color literals.
  - [x] Ensure phone/tablet layouts wrap failure metadata without overlap, preserve evidence/risk/status/actor/timestamp order, and keep blocked/retry/audit explanations keyboard reachable.
  - [x] Ensure forced-colors and reduced-motion rules cover failure rows, blocked panels, status banners, actor badges, evidence chips, risk chips, focus outlines, and retry/terminal explanations.
  - [x] Status must never be color-only. Use localized text plus icon/shape/border affordances where needed.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests for DTO serialization, OpenAPI schema, generated client availability, failure-state wire tokens, stable existing item tokens, catalog-code fields, and absence of raw error/diagnostic/payload fields.
  - [x] Message catalog tests for any new codes: safe headline/reason, finite next action/disabled reason, metadata-only detail visibility, no restricted text, and stable catalog version.
  - [x] Server projection tests for retry queued, retry accepted, retry exhausted, duplicate suppressed, terminal failure, blocked authorization, policy blocked, audit unavailable, dependency degraded, projection retryable, reprocess created, duplicate delivery, stale replay, retry-before-failure, terminal-before-retry, source email before/after failure, participant/attachment/approval before/after failure, and tenant/project partitioning.
  - [x] Conformance/read-surface tests proving foreign, unknown, malformed, missing, ambiguous, stale, and unsafe tenant/project contexts collapse to safe denial with metadata-only bodies and no failure/audit/policy/evidence/provider leakage.
  - [x] UI service/state/component tests for mapped failure metadata, actor-label accessible names, evidence/risk/status/timestamp order, localization keys, catalog-code rendering, reachable blocked explanations, no raw colors, and no raw exception/provider diagnostics.
  - [x] Update Playwright/UI.E2E fixture coverage for populated S1 stream to include retry queued, retry accepted, retry exhausted, duplicate suppressed, terminal failure, policy blocked, audit unavailable, dependency degraded, projection retryable, and reprocess-created rows with forced-colors and reduced-motion assertions where the harness supports them.

## Dev Notes

### Scope Boundaries

- This story is failure, retry, and blocked-state rendering on the existing S1 project conversation stream.
- It may establish contract fields, projection event envelopes, server projection materialization, catalog-code mapping, and UI rendering for failure/retry/blocked events already present in the projection/event stream.
- Do not implement a retry engine, operational queue management, tenant notification routing, retry execution workflow, audit investigation surface, policy editor, approval review surface, AI action conversion, command allowlist execution, outbound communication, or attachment storage/capture workflow in this story.
- Do not implement AI outcome rendering (Story 3.8), the "why this project" evidence/provenance panel (Story 3.9), next-action consolidation (Story 3.10), informational/actionable classification or full human-review history (Story 3.11), attachment capture/storage (Story 3.12), attachment state/authorization expansion beyond existing metadata (Story 3.13), or AI-context packaging (Story 3.14).
- Do not add a chat composer, direct retry command surface, direct operational queue browser, direct audit browser, direct EventStore reader in the UI, or direct server projection access from the UI.

### Existing Code To Reuse

- S1 contract and read surface: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`, `ProjectConversationResponse.cs`, `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`, `ProjectConversationActorKind.cs`, `RiskClass.cs`, OpenAPI `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, and generated client output under `src/Hexalith.ChatBot.Client/Generated/`.
- Message catalog: `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotMessageCodes.cs`, `ChatBotMessageCatalogEntry.cs`, `ChatBotMessageCatalogVersion.cs`, `ChatBotMessageNextActions.cs`, `ChatBotDisabledActionReasons.cs`, and `ChatBotDetailVisibility.cs`.
- Existing failure/retry codes are already present in the catalog/code list: duplicate suppression, retry queued/accepted/exhausted, terminal failure, recoverable mailbox degradation, projection retryable, reprocess created, dependency degraded, failed attachment/command, audit unavailable, and blocked/refusal codes.
- S1 projection store and item model: `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`, `IProjectConversationProjectionStore.cs`, `InMemoryProjectConversationProjectionStore.cs`, `DaprProjectConversationProjectionStore.cs`, and `ProjectConversationPage.cs`.
- Existing S1 materialization patterns: `ProjectConversationItemView.FromParticipant`, `FromAttachment`, `FromAssociationDecision`, and `FromApprovalEvent`; `AssociationProjectionHandler` and `ApprovalProjectionHandler` show projection-to-conversation-store patterns.
- Existing status/reliability support: `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs`, `OperationAuditStatus.cs`, `src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs`, `IAuditHistoryReader.cs`, `OperationAuditHistoryHttpResults.cs`, `IUserFacingMessageTelemetry.cs`, and `InMemoryUserFacingMessageTelemetry.cs`.
- Existing S1 UI route/state/service: `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`, `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`, `ChatBotApprovalConversationItem.razor`, `ChatBotDecisionConversationItem.razor`, `ChatBotAttachmentConversationItem.razor`, `ChatBotBlockedState.razor`, `ChatBotStatusBanner.razor`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/*`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`.
- Governed UI primitives: `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotStatusBanner`, `ChatBotBlockedState`, localization resources, live-region feedback matrix, and `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Existing tests to extend: `MessageCatalogContractTests`, `ProjectConversationContractTests`, `OpenApiContractSpineTests`, `ClientGenerationTests`, `ProjectConversationProjectionTests`, `CrossTenantReadSurfaceIsolationTests`, `ProjectConversationServiceTests`, `ChatBotLocalizationContractTests`, `AssociationReviewComponentContractTests`, and `ProjectConversationE2ETests`.

### Current State To Preserve

- Story 3.1 added `GET /api/v1/projects/{projectId}/conversation`, tenant/project keyed S1 projection storage, cursor pagination, authorized empty reads, safe denial for unauthorized reads, governed UI route/state, and reducers that clear previous project state on load/failure.
- Story 3.2 enriched S1 items with source-email metadata from mailbox intake while keeping raw provider `SourceContext` and email bodies out of the read contract. Source-email enrichment must not overwrite newer failure-state, approval, association, participant, or attachment state.
- Story 3.3 added participant item rendering and pending materialization by tenant/intake. Participant source-version replacement is independent from failure-state replacement.
- Story 3.4 added attachment item rendering and pending materialization by tenant/intake. Attachment source-version replacement is independent from failure-state replacement.
- Story 3.5 split source email association context from append-only association/correction decision items using deterministic `decision:{associationId}:{sourceVersion}` item ids. Failure-state items must follow the same append-only history principle and must not reuse `decision:` item ids.
- Story 3.6 added dedicated approval-event contract fields, projection sources, DAPR/in-memory approval enrichment, and `ChatBotApprovalConversationItem.razor`. Failure-state rows must not reuse approval item ids, approval-specific metadata fields, or approval component semantics.
- `ProjectConversationItemView.ShouldReplace` guards item replacement by source version for the same item id. If failure events use independent source versions, tests must prove stale failure replay cannot overwrite newer failure state and cannot mutate source email, participant, attachment, association/correction, or approval state.
- `ProjectConversationItemKind` currently has `EmailDerived`, `SystemDecision`, `Participant`, `Attachment`, and `ApprovalEvent`. Story 3.7 should add a distinct failure-state kind unless implementation proves reuse is safer and contract tests prove no ambiguity.
- Existing worktree has an unrelated modification in `_bmad-output/story-automator/orchestration-2-20260531-161212.md` from prior automation history; do not touch or revert it.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. UI reads through `IChatBotClient` only; server projection internals, DAPR, EventStore, audit stores, retry workers, and sibling adapters stay server-side. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- Problem/error responses and rendered failure messages are metadata-only: `{ category, code, message, correlationId, taskId?, retryable, clientAction, details.visibility }`; user-safe text comes from the versioned message catalog. Raw error text leaking to a user is release-blocking. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- ChatBot derived records carry tenant/provenance/kernel/redaction/retention/schema/version metadata. Decision and status snapshots are append-only and superseded, never silently mutated; live mirrors are version-stamped projections. [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- Tenant authority comes from authenticated claims/context and projection gates, never route/body/query values. Unknown, foreign, malformed, missing, ambiguous, stale, or unsafe tenant context must collapse to safe denial without confirming restricted resource existence. [Source: _bmad-output/planning-artifacts/epics.md#FR16; _bmad-output/planning-artifacts/epics.md#NFR2]
- DAPR pub/sub is at-least-once and unordered. Projection handlers must tolerate duplicate and out-of-order failure, retry, blocked, approval, association, intake, participant, and attachment events. SignalR nudges, if used, trigger re-query and are never trusted as data. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- Cursor tokens stay opaque and tenant/project scoped. Do not embed tenant, project, mailbox, approval, operation, audit, evidence, policy, command payload, raw error, provider diagnostic, or hidden resource text in cursor values.
- Use `System.Text.Json` shared options and camelCase wire names. Do not add inline `JsonSerializerOptions`, Newtonsoft.Json, or new serialization libraries.
- Lifecycle-state strings are exact and shared: `Received`, `Proposed`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Corrected`, `Correcting`, and `Correction-delayed`. Do not invent synonyms for existing lifecycle fields.

### UX And Accessibility Guardrails

- The UX package is binding despite having no mockups. Failure rows must use consistent actor attribution, actor-type labels before content in accessible names, and evidence/risk/status/actor/timestamp ordering. [Source: _bmad-output/planning-artifacts/epics.md#Cross-cutting-acceptance-and-planning-guidance; _bmad-output/planning-artifacts/epics.md#UX-DR41]
- Failure/retry/blocked rows should read as governed system status events, not anonymous chat messages, association decisions, approvals, or raw exception reports. Use a dedicated component or equivalent split from existing conversation-item components.
- Retryable failure uses persistent row/panel status with retry count, duplicate-safety note, safe next action, and polite announcement. Terminal failure/policy denial uses persistent alert or blocked state with escalation/manual path; assertive announcement only when caused by the current user's action.
- Evidence, risk, status, actor, and timestamp must appear in consistent order. Plain-language labels precede raw IDs; IDs remain available as metadata.
- Conversation stream focus remains stable: Tab reaches failure-state groups and any blocked/retry/audit explanations. Reduced motion suppresses non-essential item movement.
- EN/FR localization is required. Stable machine codes, IDs, lifecycle states, status codes, reason codes, message catalog codes, operation IDs, task IDs, workflow IDs, audit operation IDs, and correlation IDs remain untranslated; labels and explanations are translated. Avoid concatenated strings for accessible names.
- Disabled, blocked, retryable, terminal, or unavailable detail must expose a reachable reason via inline text or focusable explanation. Tooltip-only explanation is not acceptable.

### Project Structure Notes

- Contract DTO and enum changes belong under `src/Hexalith.ChatBot.Contracts/Queries/`, `src/Hexalith.ChatBot.Contracts/Enums/`, `src/Hexalith.ChatBot.Contracts/Messages/`, and `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Regenerated client output belongs in `src/Hexalith.ChatBot.Client/Generated/`; do not hand-edit `.g.cs` files.
- Server failure-state projection changes belong under `src/Hexalith.ChatBot.Server/Projections/`. If later implementation needs true retry worker/domain logic, keep it out of this story unless an existing projection event already exposes the metadata needed for rendering.
- UI changes belong under `src/Hexalith.ChatBot.UI/Components/Governed/`, `Components/Pages/`, `Services/`, `State/ProjectConversation/`, `Localization/`, and `wwwroot/css/`.
- Tests mirror source boundaries under `tests/Hexalith.ChatBot.Contracts.Tests/`, `Client.Tests/`, `Server.Tests/Projections/`, `Conformance.Tests/`, `UI.Tests/`, and `UI.E2E.Tests/`.

### Previous Story Intelligence

- Story 3.6 established the latest dedicated item-kind/component pattern for S1 extension. Failure rendering should follow that shape: additive contract fields, projection-facing view/source/translator/handler, deterministic item ids, and a dedicated governed component.
- Story 3.6 review fixed restricted policy/audit identifier leakage and out-of-order Dapr enrichment. Failure rendering must not leak audit operation IDs or hidden resource references when unavailable/redacted, and Dapr/in-memory stores must behave the same for out-of-order failure/retry events.
- Story 3.5 established append-only history for association/correction decisions. Failure/retry/blocked state must preserve each event as history and link later retry/reprocess events instead of mutating the original failure row.
- Story 3.4 review fixed restricted metadata leakage and redacted/unavailable distinction. Failure rendering must not repeat the same class of bug with raw error text, provider diagnostics, hidden attachment/file names, audit details, policy details, command payloads, prompts, outputs, or source evidence values.
- Story 3.3 review fixed raw enum display and actor badge fallback issues. Failure rendering needs localized user-facing labels; machine tokens may appear only in intended metadata fields.
- Story 3.2 review fixed stale source-email replay, association/source-email source-version conflation, and missing threshold-band metadata. Failure-state enrichment must keep source-version ownership separate.
- Story 3.1 review fixed DAPR stale replay handling, cross-project stale UI state, authorized empty conversation reads, and stable rendered item IDs. Failure rendering must preserve those regression targets.
- Story 2.9 created the reliability context: duplicates, retries, terminal/non-terminal failures, and visible recoverable states. Story 3.7 should render those states in S1 but should not rebuild retry processing.
- Epic 2 retrospective and architecture both warn not to invent a separate conversation model. Build on the existing contract spine and S1 projection; do not introduce a transcript table, retry-specific UI data plane, or browser-side audit data plane.
- Prior validation used compiled xUnit v3 executables because VSTest socket creation was blocked in this sandbox. Prefer that fallback if `dotnet test` fails with local socket permission errors.

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - targeted contract/client/server/UI/conformance tests touched by this story
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` if the compiled runner is available
- Use xUnit v3, Shouldly, NSubstitute, bUnit, existing Playwright fixtures, and existing CSS/static contract tests only. Do not add assertion, mocking, UI, or E2E libraries.
- Add regression tests around event ordering: retry queued before failure, retry accepted before failure, retry exhausted before retry queued, terminal failure before retry, reprocess created after terminal failure, source email before/after failure, participant/attachment/approval before/after failure, duplicate failure delivery, stale failure replay after current failure state, audit unavailable, dependency degraded, projection retryable, policy blocked, authorization blocked, and correction delayed.
- Include negative content assertions for raw exception text, stack traces, provider diagnostics, raw prompt, raw model output, raw command payload, raw policy body, raw audit envelope, unauthorized project/file/participant/recipient names, hidden evidence values, hidden diagnostic data, local paths, and tokens in API, UI, fixture, logs/test output where applicable.

### Latest Technical Notes

- Architecture pins .NET SDK `10.0.302`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC via FrontComposer, Fluxor, DAPR 1.17.x, Aspire 13.3.x, xUnit v3 3.2.x, Shouldly, NSubstitute, and Testcontainers. Do not upgrade packages for this rendering story. [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints--Dependencies]
- Dapr pub/sub documentation confirms at-least-once delivery semantics. Failure-state projection code must therefore be idempotent and out-of-order tolerant. [Source: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- NuGet lists the repo-pinned Fluent UI Blazor package as prerelease and compatible with .NET 9.0 or higher. Keep the repo-pinned Fluent/FrontComposer stack unless a separate dependency-upgrade story is created. [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/5.0.0-rc.2-26098.1]
- Microsoft Learn tracks .NET 10 breaking changes separately; do not mix framework migration work into failure-state rendering. [Source: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10]

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3, Story 3.7, FR22, FR24, FR25, FR64-FR66, FR71, FR77, FR79, NFR2, NFR11, NFR40, NFR60, UX-DR18, UX-DR35, and UX-DR41 context.
- `_bmad-output/planning-artifacts/architecture.md` - message catalog, metadata-only problem responses, append-only derived records, projection boundaries, DAPR/event ordering, file organization, and testing standards.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - conversation detail state coverage, retryable/terminal failure feedback, blocked-state recovery, live-region behavior, reduced motion, and responsive behavior.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - blocked-state/status-banner components, semantic status colors, forced-colors constraints, and compact operational surface posture.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` and `ChatBotMessageCodes.cs` - existing versioned message catalog and failure/retry codes to reuse.
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs` - current S1 item DTO to extend additively.
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs` and `ProjectConversationActorKind.cs` - current item/actor wire tokens to preserve and extend.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` - current S1 item materialization logic and deterministic item-id helpers to extend.
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionHandler.cs` and `ApprovalEventView.cs` - latest dedicated event projection pattern from Story 3.6.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`, `ChatBotApprovalConversationItem.razor`, `ChatBotBlockedState.razor`, `ChatBotStatusBanner.razor`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs` - current S1 routing, rendering, blocked/status primitives, and UI mapping patterns to extend.
- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md` - S1 shell/read surface implementation context and review fixes.
- `_bmad-output/implementation-artifacts/3-2-associated-email-rendering-in-the-conversation-stream.md` - source-email enrichment context and review fixes.
- `_bmad-output/implementation-artifacts/3-3-participant-rendering-in-the-conversation-stream.md` - participant materialization/UI pattern and review fixes.
- `_bmad-output/implementation-artifacts/3-4-attachment-rendering-in-the-conversation-stream.md` - attachment materialization/UI pattern and redaction review fixes.
- `_bmad-output/implementation-artifacts/3-5-association-and-correction-decision-rendering.md` - append-only decision history pattern, implementation context, and review fixes.
- `_bmad-output/implementation-artifacts/3-6-approval-event-rendering.md` - latest approval event projection/UI pattern, implementation context, and review fixes.
- `_bmad-output/implementation-artifacts/2-9-duplicate-detection-retry-and-failure-states.md` - retry/failure domain context and Story 3.7 readiness note.
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-01.md` - Epic 3 warning to reuse Epic 2 metadata/source-evidence records and corrected-context readiness.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (initial implementation); Claude Opus 4.8 (dev-story validation pass, 2026-06-01)

### Debug Log References

- Create-story workflow executed 2026-06-01T06:10:56+02:00.
- Source discovery loaded `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`, `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`, sprint status, Story 3.6, current S1 projection/UI/message-catalog/test files, sibling project-context facts, recent git history, and official Dapr/NuGet/Microsoft technical references.
- Discovery results: loaded `{epics_content}` from 1 file, `{architecture_content}` from 1 file, `{prd_content}` from 1 sharded PRD file plus focused FR/NFR excerpts, `{ux_content}` from 2 sharded UX files, and `{project_context}` from sibling module project-context files with FrontComposer rules most relevant to UI.
- Dev-story validation pass (2026-06-01): `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 warnings (warnings-as-errors), 0 errors.
- Targeted compiled xUnit v3 runners (VSTest sockets blocked in sandbox): Contracts 84, Client 15, Server 259, UI 91, Conformance 56 — all passed.
- E2E `ProjectConversationE2ETests` initially 1/7 failed: shared helper `AssertDecisionMetadataAsync` hard-asserted the `"System decision,"` accessible-name prefix and was reused for the three new failure-state rows whose name correctly leads with `"System status,"`. Fixed by adding an `expectedAccessibleNamePrefix` parameter (default unchanged) and passing `"System status,"` for the failure rows. Re-ran: 7/7 passed. Product HTML was correct; only the test helper needed the fix.
- Senior developer review (AI, 2026-06-01): found and auto-fixed three issues: blocked-reason OpenAPI/generated-client enum did not cover catalog/story tokens that projection/UI can emit; failure catalog localization missed required blocked/authorization/stale/correction/recoverable mailbox codes; projection accepted unsafe free-text metadata tokens that could leak raw exception/provider/path detail into S1.
- Review validation (2026-06-01): `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed; targeted compiled xUnit v3 runners passed for `ProjectConversationContractTests` (3), `ClientGenerationTests` (14), `ProjectConversationProjectionTests` (32), `ChatBotLocalizationContractTests` + `ProjectConversationServiceTests` (12), and `ProjectConversationE2ETests` (7).

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story status set to `ready-for-dev`.
- Sprint status updated for `3-7-failure-retry-and-blocked-state-rendering`.
- ✅ Contract spine extended additively: `failure-state` item kind, `system-status` actor kind, `FailureStateKind`/`FailureStatus` enums (exact wire tokens), and ~28 additive `ProjectConversationItem` failure/retry/blocked fields. Existing item/actor wire tokens unchanged; generated client regenerated and sha256 fixture updated.
- ✅ Rendering bound to the existing versioned message catalog by reuse only — no new codes required; `FromFailureStateEvent` resolves headline/reason/next-action/disabled-reason/detail-visibility via `ChatBotMessageCatalog.Resolve` and `ChatBotMessageCatalogVersion.Current`. Catalog files unmodified, `MessageCatalogContractTests` preserved.
- ✅ Projection source added (`PublishedFailureStateEvent`, `FailureStateEventView`, `FailureStateProjectionTranslator`, `FailureStateProjectionHandler`); translator validates catalog code membership and consumes EventStore-stamped tenant/domain/aggregate/source-version/correlation metadata. Deterministic ids `failure:{operationId}:{kind}:{sourceVersion}`, stored in existing `IProjectConversationProjectionStore`; idempotent/out-of-order tolerant via existing source-version `ShouldReplace`.
- ✅ Retry/terminal/blocked/audit/authorization detail rendered safely: unauthorized audit operation ids redacted (`AuthorizedAuditOperationId`), reachable inline reason paragraphs (not tooltip-only), terminal-rule and duplicate-safety explanations, append-only history preserved.
- ✅ Dedicated `ChatBotFailureStateConversationItem.razor` routed from `ChatBotConversationStream.razor`; reuses governed primitives; actor-led accessible name via template (`FailureStateAccessible`, no concatenation); evidence/risk/status/actor/timestamp ordering; EN/FR localization keys added.
- ✅ Responsive/forced-colors/reduced-motion CSS extended with Fluent/FrontComposer tokens only (no raw color literals); status conveyed by text + affordances, never color-only.
- ✅ Validation coverage across contract, generated client, server projection (duplicate/stale/out-of-order ordering), conformance/read-surface isolation, UI service/state/component, and Playwright E2E fixtures including forced-colors and reduced-motion assertions.
- ✅ Senior review fixes applied: failure metadata tokens are now constrained to safe machine-token characters before projection, expanded blocked-reason wire coverage is regenerated into the typed client, and EN/FR failure catalog mappings now cover authorization, policy block, unresolved participant, stale/expired evidence, correction delay/audit unavailable, recoverable mailbox degradation, retry exhaustion, duplicate suppression, and reprocess-created states.

### File List

New:
- `src/Hexalith.ChatBot.Contracts/Enums/FailureStateKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/FailureStatus.cs`
- `src/Hexalith.ChatBot.Server/Projections/FailureStateEventView.cs`
- `src/Hexalith.ChatBot.Server/Projections/FailureStateProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/FailureStateProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedFailureStateEvent.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`

Modified:
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationActorKind.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
- `_bmad-output/implementation-artifacts/3-7-failure-retry-and-blocked-state-rendering.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-2-20260531-161212.md`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- HIGH: `blockedReason` in the OpenAPI/generated client accepted only a subset of finite blocked states, so valid projection values such as `retry-exhausted`, `already-decided`, and `audit-unavailable` could fail generated-client deserialization or render as unavailable. Fixed the OpenAPI enum, regenerated `HexalithChatBotClient.g.cs`, updated the hash fixture, and added contract/client assertions.
- HIGH: failure-state UI localization did not cover required catalog-backed states including `refusal_blocked_action`, `authorization_denied`, unresolved participant, stale/expired evidence, correction audit/propagation delay, and recoverable mailbox degradation. Added EN/FR resources, localizer mappings, and localization regression assertions.
- HIGH: `FailureStateProjectionTranslator` passed optional projection metadata through as arbitrary strings. Raw exception text, paths, provider diagnostics, or hidden resource names in fields such as `FailureReasonCode`, `DependencyName`, `AuditOperationId`, and `DuplicateSafetyState` could reach API/UI metadata. Added safe machine-token filtering for required and optional metadata and regression tests proving unsafe optional values are suppressed and unsafe required values are ignored.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectConversationContractTests` - passed 3/3.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests` - passed 14/14.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests` - passed 32/32.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests` - passed 12/12.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 7/7.

## Senior Developer Review (AI) — Re-review

Reviewer: Claude Opus 4.8 on 2026-06-10

Outcome: Approved after automatic fixes. No critical issues remain; committed implementation re-validated.

Scope: Adversarial re-review of the committed story 3.7 changeset (`f6c79ba..66d47e3`) plus the uncommitted working-tree delta. The committed contract/projection/UI/localization/CSS code passed adversarial validation — additive contract with no raw exception/diagnostic/payload fields, `FailureStateProjectionTranslator` safe-token + catalog-membership filtering, audit-operation-id redaction gating (`AuthorizedAuditOperationId`), append-only history via unique `failure:{operationId}:{kind}:{sourceVersion}` ids with idempotent/stale-safe `ShouldReplace`, actor-led accessible names (`"System status, …"`), evidence/risk/status/actor/timestamp ordering, reachable inline (non-tooltip) reasons, and reduced-motion/forced-colors CSS coverage. EN/FR localization parity verified (110/110) and contract-tested.

Findings fixed:

- MEDIUM (test fidelity): The uncommitted E2E scenario `BuildStory37BlockedReasonVariantsBody` rendered two `messageCatalogCode` values — `evidence_stale` and `correction_delayed` — that are not members of `ChatBotMessageCodes`/`ChatBotMessageCatalog`. The real `FailureStateProjectionTranslator` rejects any non-catalog code, so those rows could never be produced by the projection; the test asserted against an unreachable fixture. Replaced with the real catalog codes `association_stale_evidence` and `association_correction_propagation_delayed`.
- MEDIUM (contract fidelity): The same fixture used `blockedReason` machine tokens (`authorization-denied`, `evidence-stale`) absent from the OpenAPI `blockedReason` enum and from `FailureBlockedReasonLabel`, so they neither round-trip through the generated client enum nor localize (they render the "Unavailable" fallback). Replaced with reachable enum/localizer values (`insufficient-authority`, `evidence-expired`); marker arrays and the no-browser coverage assertions were updated in lockstep.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — passed (0 warnings, 0 errors).
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` — passed 24/24 (corrected blocked-reason-variant test exercises the no-browser coverage path in this sandbox).

Note: the E2E fixture edits remain uncommitted in the working tree per workflow (no commit performed).

## Change Log

- 2026-06-10 — Senior developer re-review (AI, Claude Opus 4.8): committed implementation re-validated with no critical findings. Auto-fixed uncommitted E2E fixture fidelity defects — replaced non-existent catalog codes (`evidence_stale`, `correction_delayed`) and non-enum blocked-reason tokens (`authorization-denied`, `evidence-stale`) with reachable catalog/contract values. Build green; `ProjectConversationE2ETests` 24/24. Status unchanged: done.
- 2026-06-01 — Story 3.7 implemented: failure/retry/blocked-state rendering on the S1 project conversation stream. Additive contract spine (`failure-state` kind, `system-status` actor, failure metadata fields, regenerated client), catalog-backed safe messaging by reuse, metadata-only failure-state projection source/handler with deterministic append-only item ids, dedicated governed `ChatBotFailureStateConversationItem` with EN/FR localization and accessibility, and validation coverage across contract/client/server/conformance/UI/E2E suites. Status: in-progress → review.
- 2026-06-01 — Dev-story validation pass (Claude Opus 4.8): fixed E2E helper `AssertDecisionMetadataAsync` to accept a configurable accessible-name prefix so failure rows (`"System status,"`) assert correctly; all touched suites green (Contracts 84, Client 15, Server 259, UI 91, Conformance 56, E2E 7).
- 2026-06-01 — Senior developer review (AI): fixed blocked-reason OpenAPI/generated-client coverage, failure catalog EN/FR localization gaps, and unsafe free-text projection metadata leakage. Validation green; status review → done.
