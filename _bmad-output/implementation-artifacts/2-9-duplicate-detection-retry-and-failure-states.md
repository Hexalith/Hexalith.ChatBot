---
baseline_commit: 987c5d4ae00ccdde7c2246e3ad4330b09154b8e1
---

# Story 2.9: Duplicate detection, retry, and failure states

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-01. -->

## Story

As a reviewer,
I want duplicate deliveries suppressed and failed work retried or surfaced clearly,
so that messy mailbox conditions never corrupt project state or hide work.

## Acceptance Criteria

1. **Duplicate mailbox delivery is suppressed without duplicate project artifacts.** Given the same provider mailbox message is delivered more than once, when the duplicate reaches the command spine, then the existing `tenant_id + mailbox_id + provider_message_id` message-intake idempotency key replays the prior accepted outcome, skips EventStore dispatch, and creates no duplicate project messages, attachments, task intents, approvals, commands, notifications, association decisions, or decision audit records. A metadata-only duplicate-suppression audit/status record may be created per duplicate attempt and must link to the original operation. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.9-Duplicate-detection-retry-and-failure-states; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class; src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]

2. **Duplicate suppression carries retry-safe metadata.** Given duplicate delivery is suppressed, when operation status, audit history, or M0 operational queue data is read, then the record exposes stable metadata only: original operation id, duplicate attempt correlation id, duplicate count or attempt count, operation class, retry count, duplicate-safety note, safe next action, and audit status, without raw message body, raw addresses, Graph delta tokens, provider payload, unauthorized project names, or exception text. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR14; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR37-NFR44; src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]

3. **Retry is a first-class idempotent operation.** Given a failed mailbox, attachment, association, approval, command, or projection operation where retry is valid, when an authorized actor retries it, then retry admission uses the addendum retry idempotency key `tenant_id + failed_event_id + retry_actor`, rejects conflicting duplicate retry attempts, records the retry attempt/audit facts, and either replays the same accepted retry operation or advances the item exactly once with no duplicate artifacts. [Source: _bmad-output/planning-artifacts/epics.md#FR65; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class; _bmad-output/planning-artifacts/architecture.md#ChatBot-Idempotency-keys-two-altitudes]

4. **Retry policy distinguishes retryable from terminal failure.** Given a failure reason is observed, when status is projected, then the system classifies it with a documented retry policy containing retryable-vs-terminal classification, max attempts, exponential backoff with jitter, dead-letter criteria, responsible owner role, terminal reason code, and manual recovery action; exhausted retry attempts move to a visible terminal or escalation-needed state instead of looping silently. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR18; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR39-NFR44]

5. **Failure states are visible, recoverable, and user-safe.** Given a terminal or non-terminal failure occurs in mailbox intake, association, correction propagation, projection, or the existing command spine, when an authorized UI/API caller reads operation status or relevant review/queue detail, then the item exposes lifecycle/status, retry count, partial outputs, safe next actions, terminal reason when applicable, owner role, correlation context, and audit state using message-catalog codes; unauthorized or cross-tenant reads collapse to the existing safe not-found/redacted behavior. [Source: _bmad-output/planning-artifacts/epics.md#FR66-FR71-FR80; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns; src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs; src/Hexalith.ChatBot.Server/Program.cs]

6. **Terminal state reprocessing never mutates the terminal record in place.** Given an item is in `Rejected`, `Failed`, or `Skipped`, when authorized reprocessing is requested, then the original record remains immutable and a new workflow instance is created with `supersedes_workflow` / `superseded_by_workflow` audit links; no implementation may transition `Failed -> Received`, `Skipped -> Received`, or `Rejected -> Received` in place. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Canonical-state-definitions; src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleReprocessFactory.cs; tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs]

7. **Duplicate, retry, and failure observability is measurable and tenant-scoped.** Given duplicate suppression, retry exhaustion, or mailbox failure occurs, when operational data is emitted or projected, then metrics/status carry tenant, operation class, mailbox or workflow item id, correlation id, reason code, retry count, and freshness timestamp; data stays tenant-partitioned, metadata-only, and ready for FR67 M0 queue rendering and M2 OpenTelemetry dashboards. [Source: _bmad-output/planning-artifacts/epics.md#FR67-FR94; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR37-NFR43; _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]

## Tasks / Subtasks

- [x] Extend retry and duplicate contracts without bypassing the spine (AC: 1, 3, 6)
  - [x] Add a narrowly scoped retry command under `src/Hexalith.ChatBot.Contracts/Commands/`, for example `RetryFailedWorkflowOperation`, carrying `RetryId`, `FailedEventId`, `FailedOperationClass`, `FailureReasonCode`, `ExpectedFailedSourceVersion`, and optional metadata-only note/rationale.
  - [x] Add a reprocess command only if terminal-state reprocessing cannot reuse the retry command cleanly; it must carry both old and new workflow instance ids and the reprocess audit-link reason.
  - [x] Keep tenant, actor, authority, correlation, and surface origin outside command payloads; those remain gateway/envelope concerns.
  - [x] Update `ChatBotSpineCommandAllowlist` and OpenAPI/generated client only for public commands/status fields needed by this story.
  - [x] Do not add attachment, approval, notification, AI action, or projection-specific bespoke retry commands unless a generic typed command cannot preserve the required invariants.

- [x] Implement retry idempotency and duplicate-suppression metadata (AC: 1, 2, 3)
  - [x] Extend `CoarseIdempotencyOperationClass` and `CoarseIdempotencyComposer` so retry uses `tenant_id + failed_event_id + retry_actor` with an indefinite replay window and conflict code `idempotency_conflict_retry`.
  - [x] Preserve existing message-intake duplicate behavior: same provider message replays prior outcome and does not dispatch another `CaptureMailboxMessageIntake`.
  - [x] Add a duplicate-suppression status/audit model that links duplicate attempts to the original operation id and captures duplicate count/attempt count without treating suppression as another project decision.
  - [x] Ensure idempotent replay updates only safe status freshness/attempt metadata and never downgrades `AuditReconciling` to `AuditCommitted`.
  - [x] Keep canonical normalization behavior from `CoarseIdempotencyCanonicalizer`; do not compare raw JSON or provider payload strings directly.

- [x] Add retry/failure lifecycle and policy services (AC: 3, 4, 5, 6)
  - [x] Add a `Lifecycle/Retry/` or `Lifecycle/FailureHandling/` folder for retry policy types, reason-code classification, attempt state, backoff/jitter calculation, and dead-letter/exhaustion decisions.
  - [x] Use exact lifecycle tokens `Failed`, `Skipped`, `Rejected`, `NeedsReview`, `Correction-delayed`, and the existing status strings; do not invent machine states such as `Errored`, `Duplicate`, `RetryingForever`, or localized state tokens.
  - [x] Model non-terminal retryable states through operation/queue status and safe next actions; only use terminal lifecycle states when the item requires new workflow-instance reprocessing.
  - [x] Use `LifecycleReprocessFactory` for terminal-state reprocess links; add aggregate/gateway guards proving terminal states never transition back in place.
  - [x] Emit operator alerts for retry exhaustion and dependency degradation through the existing `IOperatorAlertSink`; do not introduce a second alert abstraction.

- [x] Extend operation status, audit history, and projections (AC: 2, 4, 5, 7)
  - [x] Extend `OperationStatus`, `OperationStatusRecord`, `OperationStatusHttpResults`, and OpenAPI schema with retry/failure fields needed by FR80: operation class, retry count, max attempts, next retry time, duplicate-safety note, owner role, failure reason code, terminal reason, partial outputs, and audit state.
  - [x] Update `InMemoryOperationStatusStore` and any DAPR-backed production swap required for status durability; do not leave live topology with only volatile retry/failure status if the status is user-visible.
  - [x] Extend `AuditEnvelopeFactory` and message catalog entries for duplicate suppressed, retry queued, retry accepted, retry exhausted, terminal failure, recoverable mailbox degradation, projection retryable, and reprocess-created.
  - [x] Keep audit/status payloads metadata-only; raw Graph problem text, raw email addresses, provider payloads, and exception messages must stay out of user-visible JSON and UI text.
  - [x] Preserve tenant-scoped safe reads in `Program.cs` operation and audit-history endpoints: invalid ULID, unknown operation, and cross-tenant operation must remain indistinguishable.

- [x] Wire existing mailbox and gateway failure sources into visible recoverable states (AC: 4, 5, 7)
  - [x] Update `GraphMailboxIntakeWorker` and `MailboxIntakeWorkerResult` to carry retry policy metadata for recoverable Graph failures and recoverable gateway submission failures while preserving current no-payload/no-delta-token guarantees.
  - [x] Ensure Graph failures such as throttling, subscription expiry, token expiry, partial access, permission revoked, scope mismatch, and message-scope mismatch map to finite message-catalog reason codes with retryability classification.
  - [x] Update gateway dispatch/audit failure handling so recoverable failures create or update visible operation status where appropriate while still failing closed before durable state when required.
  - [x] If a failure occurs before tenant binding, keep the existing unresolved-scope replay intent and operator alert behavior; do not expose tenant-specific detail.
  - [x] Keep duplicate mailbox delivery suppression at the gateway/idempotency altitude; do not move provider duplicate detection into the worker as the source of truth.

- [x] Update UI surfaces through existing components (AC: 2, 5, 7)
  - [x] Extend the governed operation/status UI and Association Review status rendering for retryable failure, terminal failure, duplicate suppressed, retry queued, and reprocess-created states using `ChatBotStatusBanner`, `ChatBotBlockedState`, existing Fluxor patterns, and localization.
  - [x] Show retry count, terminal/non-terminal status, safe next action, owner role, and duplicate-safety note where applicable.
  - [x] Retry/repair actions must be keyboard-operable; disabled actions must be focusable with `aria-disabled="true"` plus an announced reason or an adjacent focusable explanation.
  - [x] Add English and French localization; visible strings must come from the UI localization/message catalog path, not raw reason codes or exceptions.
  - [x] Do not create an operational dashboard, full queue-management surface, CLI, or MCP adapter in this story; preserve contract-ready fields for those later surfaces.

- [x] Add focused verification (AC: 1-7)
  - [x] Gateway/idempotency tests for duplicate mailbox replay, duplicate suppression metadata, retry key composition, retry replay, retry conflict, audit unavailable fail-closed, dispatch unavailable status, and no duplicate dispatch.
  - [x] Aggregate/lifecycle tests for terminal reprocess links, invalid terminal in-place transitions, retry accepted from retryable failure, retry exhaustion, duplicate retry rejection/replay, and metadata-only events.
  - [x] Worker tests for retry metadata and safe reason codes across Graph throttling, subscription expiry, token expiry, permission revoked, scope mismatch, gateway 403/503, and opaque delta-token redaction.
  - [x] Projection/status tests for retry count, next retry time, duplicate count/attempt count, terminal reason, stale/out-of-order notifications, tenant partitioning, and audit reconciling replay preservation.
  - [x] UI/bUnit tests for retryable failure, terminal failure, duplicate suppressed, retry queued, disabled-action reason reachability, focus/announcement behavior, and English/French localized text.
  - [x] Architecture/conformance tests proving retry state mutation still enters through `IChatBotClient.SubmitAsync`/`CommandGateway`, UI/worker do not reference gateway internals, and aggregate handlers do no I/O.
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run relevant Server, Contracts, Client, Workers, UI, Architecture, Conformance, and Integration tests. If `dotnet test` hits sandbox VSTest socket limits, run compiled xUnit v3 test executables and record the limitation.
  - [x] Run `git diff --check`.

## Dev Notes

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Story 2.9 is the target and Stories 2.1-2.8 provide direct email intake, association, decision, correction, and propagation context.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`; relevant sections are canonical state definitions, FR64-FR80, FR90-FR94, NFR13-NFR22, NFR37-NFR44, NFR65-NFR66, and addendum §Idempotency Keys.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; the core constraints are two-altitude idempotency, single CommandGateway mutation spine, fail-closed audit seam, exact lifecycle vocabulary, tenant-partitioned projections, metadata-only audit/status, and terminal-state reprocess links.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`; retryable failure, terminal failure, duplicate suppressed, retry queued, and queue-row retry count are explicit UX states.
- Loaded previous story context from `_bmad-output/implementation-artifacts/2-8-correction-propagation-contract.md`; Story 2.8 completed correction propagation events, projection/status fields, operator alerts, and DAPR state-store persistence gaps.
- Loaded persistent project-context facts from sibling `Hexalith.*` `_bmad-output/project-context.md` files; recurring rules are .NET 10, central package management, EventStore CQRS/ES, DAPR/Aspire boundaries, tenant isolation, metadata-only redaction, xUnit v3/Shouldly tests, no generated-file hand edits, and root-level submodule initialization only.

### Source Artifact Analysis

Story 2.9 closes Epic 2's reliability floor. It must preserve the already-built association/correction pipeline while making duplicate delivery, retry, and failure status observable and idempotent. FR64/NFR14 are stricter than "do not duplicate intake": duplicate mailbox delivery must not create duplicate messages, attachments, task intents, approvals, commands, notifications, outbound emails, or audit decisions. A duplicate-suppression audit/status fact is allowed and expected, but it is not a second project decision. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.9-Duplicate-detection-retry-and-failure-states; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR14]

The addendum defines the binding idempotency table. Message intake is keyed by `tenant_id + mailbox_id + provider_message_id` with an indefinite replay window; retry is keyed by `tenant_id + failed_event_id + retry_actor` with an indefinite replay window. Existing code already implements the message-intake key and includes a `retry` operation class in `CoarseIdempotencyOperationClass.All`, but retry composition/admission is not wired to a concrete command yet. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class; src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs; src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs]

The architecture is explicit that every state mutation from UI, worker, CLI, MCP, service client, or AI actor must enter the same command spine: authentication, tenant binding, authorization, risk classification, approval gate, coarse idempotency, pre-commit audit, EventStore fine idempotency, projection, and post-commit audit. Retry must be added as another spine command, not a worker-side direct call into EventStore or a projection-store mutation. [Source: _bmad-output/planning-artifacts/architecture.md#ChatBot-CommandGateway-flow-the-spine-every-state-mutation-every-surface]

Canonical terminal semantics are already implemented: `Rejected`, `Failed`, and `Skipped` are terminal; reprocessing creates a new workflow instance and audit links instead of mutating the old item. `LifecycleTransitionValidator` intentionally has no `Failed -> Received`, `Skipped -> Received`, or `Rejected -> Received` transition. Do not loosen this validator to make retry easier. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Canonical-state-definitions; src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionValidator.cs; src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleReprocessFactory.cs]

UX requirements are concrete: retryable failure shows retry action, retry count, reason, and duplicate-safety note; terminal failure shows reason, escalation/manual recovery path, and audit availability; operational queue rows carry state, age, next action, assignee, confidence/risk, and terminal/non-terminal status. This story should extend existing status/review surfaces, not create a new dashboard. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]

### Previous Story Intelligence

Story 2.8's handoff matters because retry/failure status must not break correction propagation:

- Correction propagation events, commands, and status fields now exist under `src/Hexalith.ChatBot.Server/Association/` and `Lifecycle/Workflows/`.
- `AssociationCandidateView` and `AssociationRoutingStatus` now carry propagation status, required/completed/failed stores, progress, ETA, owner role, safe next action, and stale corrected-context state.
- `DefaultAssociationCorrectionDependencyReadiness`, `DaprCorrectionPropagationCoordinator`, `ProjectionCorrectedContextReadinessPolicy`, and DAPR association projection persistence were added.
- `OperationStatusRecord` still has `RetryCount` but no policy/max/next-retry/owner/duplicate-safety fields; retry is still mostly implied rather than first-class.
- Recent git history: `987c5d4 feat(story-2.8): Correction propagation contract`, `34799bd feat(story-2.7): Association correction and supersession`, `c8e755e feat(story-2.6): Association decision recording, evidence preservation, and notes`, `ab43296 feat(story-2.5): Ambiguous association review surface (S2)`, `ddbb192 feat(story-2.4): Ambiguous-association detection and fail-closed routing`.

### Current Implementation State

`GraphMailboxIntakeWorker` fetches a Graph message, verifies mailbox/provider scope, translates it to `CaptureMailboxMessageIntake`, and submits through `IChatBotClient.SubmitAsync(..., ChatBotSurfaceOrigin.Mailbox)`. It already treats Graph throttling, subscription expiry, token expiry, partial access, permission revoked, scope mismatch, and gateway 401/403/503 as recoverable worker results with safe reason codes. It does not persist retry count, next retry time, duplicate-safety note, or terminal/exhausted state. [Source: src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs; tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs]

`CommandGateway` already handles duplicate mailbox provider delivery correctly at the dispatch boundary: `ReplayPriorOutcome` for `message-intake` writes `AuditEnvelopeFactory.DuplicateMailboxIntakeSuppressed(...)`, skips dispatcher dispatch, preserves existing operation status when present, and returns the original accepted command id. Extend this path; do not replace it with worker-local duplicate detection. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs#DuplicateMailboxProviderDeliveryShouldReplayPriorOutcomeAuditSuppressionAndSkipDispatch]

`AuditEnvelopeFactory.DuplicateMailboxIntakeSuppressed(...)` currently records `decision = suppress`, `reasonCode = duplicate_provider_message`, `stateTransition = Received->Skipped`, and `outcome = duplicate_suppressed`. This is useful but too narrow for AC2: status/audit needs original operation linkage, attempt count, operation class, retry count, duplicate-safety note, and safe next action without raw provider data. [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]

`OperationStatus`, `OperationStatusRecord`, and the OpenAPI status schema already include operation id, command id, correlation id, lifecycle state, `RetryCount`, completion status, audit status, partial outputs, safe next actions, terminal reason, accepted time, and last-updated time. Current records are created as accepted/projection-pending with `RetryCount = 0`; there is no first-class failed/retryable/exhausted status update path yet. [Source: src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs; src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml]

`MailboxMessageIntakeCaptured` stores source identity and attachment references only, with metadata-only provenance/redaction/retention fields. Duplicate suppression must not create another one of these events for the same provider message. Aggregate altitude fine idempotency only rejects duplicate use of the same intake aggregate id; provider duplicate suppression is correctly handled at gateway coarse idempotency altitude. [Source: src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs; src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeAlreadyCapturedRejection.cs; src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]

### Architecture Guardrails

- Runtime stack: .NET SDK `10.0.300`, `net10.0`, central package management, DAPR `1.17.9`, Aspire `13.3.x`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, NSwag `14.7.1`, xUnit v3 `3.2.2`, Shouldly, bUnit, and Playwright. Do not add inline package versions or broad dependency upgrades. [Source: global.json; Directory.Packages.props]
- Keep Contracts low-dependency. Retry command/query contracts belong in `Hexalith.ChatBot.Contracts`; gateway idempotency, retry policy, audit writing, operation status stores, and projections belong in Server/Workers/UI as appropriate. [Source: _bmad-output/planning-artifacts/architecture.md#Structure-Patterns]
- EventStore owns envelope metadata and aggregate rehydration. Aggregate `Handle` methods must remain pure and never perform DAPR, HTTP, mailbox, projection-store, audit, authorization, clock, or retry-scheduler I/O. [Source: Hexalith.EventStore/_bmad-output/project-context.md; src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- DAPR pub/sub and projection handlers are at-least-once and unordered; every retry/failure/duplicate projection must be idempotent and order-tolerant by source version or stable attempt id. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]
- User-visible failures must use RFC 9457 problem shape or message-catalog/localization entries. Raw exception text, Graph error text, raw addresses, provider payload, and unauthorized resource names in UI/status/audit responses are release-blocking. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR40]
- Do not hand-edit generated client files. If OpenAPI changes, update source/OpenAPI and regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, then update the generated-client fixture hash. [Source: src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs; tests/fixtures/hexalith-chatbot-generated-client.sha256]
- Submodules: initialize/update only root-level submodules declared in repository root `.gitmodules`; never use recursive submodule commands. [Source: AGENTS.md#Git-Submodules]

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Contracts/
    Commands/
      RetryFailedWorkflowOperation.cs                  # NEW, if generic retry command is chosen
      ReprocessTerminalWorkflowOperation.cs            # NEW only if terminal reprocess needs separate contract
    Queries/
      OperationStatus.cs                               # UPDATE retry/failure/duplicate metadata
      OperationStatusPartialOutputs.cs                 # UPDATE if partial outputs gain retry facts
    Messages/
      ChatBotMessageCodes.cs                           # UPDATE retry/duplicate/failure codes
      ChatBotMessageCatalog.cs                         # UPDATE user-safe messages
      ChatBotMessageNextActions.cs                     # UPDATE retry/reprocess/escalate if needed
    openapi/hexalith.chatbot.v1.yaml                   # UPDATE if public contract changes
  Hexalith.ChatBot.Client/
    Generated/HexalithChatBotClient.g.cs               # REGENERATE only if OpenAPI changes
  Hexalith.ChatBot.Server/
    Gateway/
      CommandGateway.cs                                # UPDATE duplicate replay/status and retry admission behavior
      ChatBotSpineCommandAllowlist.cs                  # UPDATE for retry/reprocess commands
      Idempotency/
        CoarseIdempotencyOperationClass.cs             # UPDATE retry constant if needed
        CoarseIdempotencyComposer.cs                   # UPDATE retry key composition
      Status/
        OperationStatusRecord.cs                       # UPDATE retry/failure/duplicate metadata
        OperationStatusHttpResults.cs                  # UPDATE wire model
        IOperationStatusStore.cs                       # UPDATE only if query/update semantics require it
    Lifecycle/
      Retry/
        RetryPolicy.cs                                 # NEW classification/backoff contract
        RetryAttemptState.cs                           # NEW metadata-only attempt state
        RetryFailureReasonCodes.cs                     # NEW finite reason codes if not catalog-only
      StateModel/
        LifecycleReprocessFactory.cs                   # UPDATE only additively if more link metadata needed
    Audit/
      AuditEnvelopeFactory.cs                          # UPDATE duplicate/retry/reprocess facts
      OperatorAlertKind.cs / OperatorAlert.cs          # UPDATE retry exhaustion/degraded alerts if needed
    Operations/
      GovernedOperationAggregate.cs                    # UPDATE pure handlers for retry/reprocess lifecycle only
      GovernedOperationState.cs                        # UPDATE replay state for retry/reprocess only if aggregate-owned
    Projections/
      AssociationProjectionHandler.cs                  # UPDATE only for association/correction failure visibility
      GovernedOperationProjectionHandler.cs            # UPDATE only if operation views carry retry/failure facts
  Hexalith.ChatBot.Workers/
    Mailbox/
      GraphMailboxIntakeWorker.cs                      # UPDATE recoverable/terminal retry metadata
      MailboxIntakeWorkerResult.cs                     # UPDATE finite retry/status fields
  Hexalith.ChatBot.UI/
    Services/GovernedOperationService.cs               # UPDATE status mapping
    State/GovernedOperations/*                         # UPDATE retry/failure outcome state
    Components/Pages/GovernedOperations.razor          # UPDATE existing status surface
    Components/Governed/*                              # UPDATE only existing reusable components
    Localization/*.resx                                # UPDATE EN/FR
tests/
  Hexalith.ChatBot.Contracts.Tests/
  Hexalith.ChatBot.Server.Tests/
  Hexalith.ChatBot.Workers.Tests/
  Hexalith.ChatBot.UI.Tests/
  Hexalith.ChatBot.Architecture.Tests/
  Hexalith.ChatBot.Conformance.Tests/
  Hexalith.ChatBot.IntegrationTests/
```

### Out of Scope

- Full Epic 3 conversation stream rendering of failure/retry events; keep contracts/status ready for Story 3.7.
- Full operational queue management, claim/assign/filter/sort, notification routing, escalation policy configuration, or M2 dashboards.
- CLI/MCP retry adapters; preserve parity-compatible commands and status contracts for M1.
- New attachment capture/storage, approval engine, AI proposal engine, command execution catalog, outbound send, or notification subsystem implementation.
- Changing the provider-message duplicate key or moving duplicate detection source-of-truth into the Graph worker.
- Mutating terminal `Rejected`/`Failed`/`Skipped` records in place to simplify retry.
- Direct writes into Projects, Conversations, Folders, Memories, or vector stores; use ChatBot-owned adapter/workflow seams only.
- Package upgrades to DAPR/Aspire/Fluent UI/Fluxor/xUnit/Playwright.
- Recursive submodule initialization or nested submodule changes.

### Latest Technical Notes

- No new external library is required for this story. Use the repository-pinned stack in `Directory.Packages.props`; retry policy and status modeling should be plain .NET/domain code.
- If generated OpenAPI/client code changes, use the repository's existing NSwag path and update the generated-client checksum fixture instead of editing generated code by hand.

### Validation Notes

Checklist pass completed against `.agents/skills/bmad-create-story/checklist.md`:

- Reinvention prevention: story directs reuse of existing `CommandGateway`, coarse idempotency store/composer, EventStore aggregate/state model, `LifecycleReprocessFactory`, operation status endpoint, audit envelope factory, mailbox worker, message catalog, and existing UI status components.
- Wrong-location prevention: contracts, gateway, lifecycle retry policy, audit/status, worker, UI, OpenAPI/generated client, and test paths are enumerated.
- Regression prevention: duplicate mailbox replay must keep skipping EventStore dispatch; terminal states must remain immutable; audit reconciling replay must not downgrade; tenant-safe reads and metadata-only responses stay explicit.
- Scope control: full dashboards, CLI/MCP adapters, attachments/approval/AI/outbound subsystems, sibling direct writes, package upgrades, and recursive submodules are out of scope.
- LLM optimization: ACs and tasks use concrete class/file names, stable state tokens, current implementation facts, and direct constraints.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/implementation-artifacts/2-8-correction-propagation-contract.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-Project-Association]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.9-Duplicate-detection-retry-and-failure-states]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Canonical-state-definitions]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR64-FR80]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR13-NFR22]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR37-NFR44]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class]
- [Source: _bmad-output/planning-artifacts/architecture.md#ChatBot-CommandGateway-flow-the-spine-every-state-mutation-every-surface]
- [Source: _bmad-output/planning-artifacts/architecture.md#ChatBot-Idempotency-keys-two-altitudes]
- [Source: _bmad-output/planning-artifacts/architecture.md#ChatBot-Lifecycle-transitions]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs]
- [Source: src/Hexalith.ChatBot.Workers/Mailbox/MailboxIntakeWorkerResult.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Idempotency/InMemoryCoarseIdempotencyStore.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]
- [Source: src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionValidator.cs]
- [Source: src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleReprocessFactory.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs]
- [Source: src/Hexalith.ChatBot.Server/Program.cs]
- [Source: src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs]
- [Source: tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs]
- [Source: Directory.Packages.props]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: VSTest/dotnet test hit sandbox socket binding limits; validated compiled xUnit v3 test executables directly.
- 2026-06-01: Regenerated NSwag client from OpenAPI and updated generated-client checksum fixture.
- 2026-06-01: Added DAPR-backed operation status store swap and retry/dependency operator alert emission after checklist audit.
- 2026-06-01: Senior review found retry command dispatch/aggregate handling and file-list/test-coverage gaps; auto-fixed and reran direct xUnit validation.

### Completion Notes List

- Story context created by bmad-create-story workflow.
- Source artifacts, previous story intelligence, current implementation files, and checklist validation were reviewed.
- Added generic retry command admission through the existing command spine using actor + failed event idempotency.
- Extended operation status/OpenAPI/client metadata for retry/failure/duplicate-safe rendering while preserving tenant-scoped reads.
- Added duplicate replay status freshness/attempt metadata without re-dispatching mailbox intake or downgrading audit reconciling state.
- Added retry/failure policy classification with max attempts, bounded exponential backoff, owner roles, terminal/exhaustion metadata, and worker recovery metadata.
- Added retry exhaustion/dependency degradation alerts through the existing operator alert sink and a DAPR-backed production operation-status store swap.
- Extended message catalog entries and existing governed operation UI status rendering/localization for retry/duplicate/failure metadata.
- Senior review auto-fix made retry executable on the real EventStore dispatcher/aggregate path, not only through gateway tests with a recording dispatcher.
- Verified with compiled xUnit v3 binaries because VSTest cannot bind its local socket in this sandbox.

### File List

- _bmad-output/implementation-artifacts/2-9-duplicate-detection-retry-and-failure-states.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs
- src/Hexalith.ChatBot.Contracts/Commands/RequestFailedWorkflowRetry.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs
- src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs
- src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs
- src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs
- src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs
- src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs
- src/Hexalith.ChatBot.Server/Gateway/Status/DaprOperationStatusStore.cs
- src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs
- src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs
- src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs
- src/Hexalith.ChatBot.Server/Lifecycle/Retry/RetryFailureAlertEmitter.cs
- src/Hexalith.ChatBot.Server/Lifecycle/Retry/RetryFailurePolicy.cs
- src/Hexalith.ChatBot.Server/Lifecycle/Retry/RetryPolicyDecision.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs
- src/Hexalith.ChatBot.Server/Operations/WorkflowRetryInvalidRejection.cs
- src/Hexalith.ChatBot.Server/Operations/WorkflowRetryRequested.cs
- src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor
- src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs
- src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx
- src/Hexalith.ChatBot.UI/Localization/SharedResource.resx
- src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs
- src/Hexalith.ChatBot.UI/State/GovernedOperations/OperationOutcome.cs
- src/Hexalith.ChatBot.Workers/Mailbox/MailboxIntakeWorkerResult.cs
- tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/OperationStatusStoreRegistrationTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Lifecycle/RetryPolicyTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs
- tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs
- tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs
- tests/Hexalith.ChatBot.UI.E2E.Tests/DuplicateRetryFailureStatesE2ETests.cs
- tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs
- tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs
- tests/fixtures/hexalith-chatbot-generated-client.sha256

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approve after auto-fix. No critical issues remain.

Findings fixed:

- [HIGH] Retry was allowlisted and idempotency-aware, but the real `AcceptedCommandDispatcher` still used the generic fallback path and there was no aggregate handler for `RequestFailedWorkflowRetry`. That meant the retry path was only proven with `RecordingDispatcher` gateway tests and could fail or serialize incorrectly on the EventStore path. Fixed by routing retry explicitly, adding metadata-only `WorkflowRetryRequested` / `WorkflowRetryInvalidRejection`, and adding aggregate reflection coverage.
- [MEDIUM] Story File List was incomplete relative to git reality, missing the API/E2E tests and the new dispatcher/aggregate files touched by review. Updated the File List so implementation artifacts match the actual changed surface.
- [MEDIUM] Test coverage did not directly prove retry dispatch used PascalCase payloads that the EventStore aggregate engine can deserialize. Added `AcceptedCommandDispatcherTests` and `GovernedOperationAggregateTests` coverage for the retry command path.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed.
- `dotnet test ...` was attempted and hit the known VSTest socket permission limit.
- Direct xUnit v3 executables passed for full Server, Contracts, Client, Workers, UI, Architecture, Conformance, Integration, and UI E2E suites.

Reviewer: Jérôme Piquot (Claude story-automator review) on 2026-06-10

Outcome: Approve after auto-fix. No critical issues remain.

Validation re-run: build clean (0 warnings / 0 errors); 2384 tests passed, 0 failed
(Server 1525, Contracts 480, UI 130, UI E2E 75, Conformance 87, Architecture 39, Workers 30, Integration 18 with 2 environment-skipped); `git diff --check` clean. AC1-AC7 re-validated against implementation: retry coarse key `tenant_id + failed_event_id + retry_actor` with indefinite window and `idempotency_conflict_retry`; duplicate mailbox replay skips dispatch and preserves audit-reconciling; aggregate retry handler is pure with ULID + fine-idempotency guards; terminal immutability and reprocess links intact; status/audit payloads metadata-only; EN/FR localization complete.

Findings fixed:

- [MEDIUM] `RetryFailurePolicy.BackoffDelay` computed jitter with `Math.Abs(StringComparer.Ordinal.GetHashCode(reasonCode) + retryCount)`. `Math.Abs(int.MinValue)` throws `OverflowException`, and a full-range string hash can reach `int.MinValue`, so the retry-policy classification — the very failure-handling path that must stay robust — could crash on rare input. Fixed with overflow-safe long-modulo jitter in `[0, 16]`; `RetryPolicyTests` still pass.
- [MEDIUM] The story-2.9 E2E suite `tests/Hexalith.ChatBot.UI.E2E.Tests/DuplicateRetryFailureStatesE2ETests.cs` (duplicate suppression, retry admission, terminal reprocess; 4 tests passing under headless Chrome) was present in the working tree but uncommitted and absent from the File List. Added it to the File List and staged the file so it is tracked.

Observations (non-blocking, not changed):

- [LOW] `MailboxIntakeWorkerResult.Recoverable` sets a flat `NextRetryAt = +60s` rather than the policy's exponential backoff; AC4's "exponential backoff with jitter" is satisfied at the Server status-projection altitude (`RetryFailurePolicy`), and the worker value is only an initial hint.
- [LOW] AC2's "duplicate attempt correlation id" is carried by the `DuplicateMailboxIntakeSuppressed` audit envelope and surfaced in the UI fixture, but is not a first-class field on `OperationStatus`.

### Change Log

- 2026-06-01: Implemented duplicate suppression, retry admission/status metadata, retry policy, worker recovery metadata, UI status rendering, OpenAPI/client updates, and focused verification for Story 2.9.
- 2026-06-01: Closed checklist audit gaps for operator alerts and DAPR-backed operation status durability; reran solution build and executable test suites.
- 2026-06-01: Senior review auto-fixed retry dispatcher/aggregate handling, added direct retry-path tests, updated File List, and approved the story.
- 2026-06-10: story-automator review auto-fixed `Math.Abs(int.MinValue)` overflow risk in retry backoff jitter, documented + staged the previously-untracked `DuplicateRetryFailureStatesE2ETests.cs`, re-validated build and 2384 tests, and re-approved the story.
