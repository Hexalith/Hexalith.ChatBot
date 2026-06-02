---
baseline_commit: e5cce4bfe0f021090fd965057027b267e391b7a1
---

# Story 6.3: Outbound approval gate and approval record

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As an authorized approver,
I want outbound communication paused for approval with full record retention,
so that nothing leaves the project boundary without an audited decision.

## Acceptance Criteria

1. Given a durable Story 6.2 outbound draft exists, when an outbound send is requested from any surface, then ChatBot creates an outbound approval request and requires an approved S6 decision before any Microsoft Graph, Exchange, SMTP, mailbox worker, or outbound adapter side effect occurs. The request must surface command name, allowlist version, draft id, proposed content redaction state, recipients, sender-authority class, requester, project/context refs, policy snapshot, evidence freshness, expected post-state, and decisions `approve` / `reject` / `request-revision` / `cancel`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.3`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR42`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR49`]
2. Given an outbound approval decision is recorded, when the decision is `approve`, `reject`, `request-revision`, or `cancel`, then the approval record is append-only and preserves proposed content, approved content when approved, recipient refs, sender authority, project/context refs, requester, approver, decision outcome, authority result, policy snapshot, audit operation id/status, correlation id, redaction state, retention class, and source version. Rejections, revision requests, and cancellations must never send. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.3`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR50`; `_bmad-output/planning-artifacts/architecture.md#ChatBot Derived-record shape`]
3. Given an approved outbound send is executed, when the send command enters the CommandGateway, then sender authority is recomputed server-side from current evidence using Story 6.1 authority semantics; missing outbound-send scope, policy block, stale/expired evidence, delegation mismatch, lapsed shared-mailbox membership, missing paired approval, or adapter not-approved-mode fail closed before durable send mutation or external side effect with metadata-only denial. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Authority class mapping (FR48 five-class taxonomy)`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`]
4. Given the outbound-send operation class, when a draft send is retried, then the coarse and aggregate-level idempotency key is `tenant_id + outbound_draft_id + send_actor`; the first successful send is single-shot, and any later send for the same draft and send actor is rejected with a stable safe `already sent` outcome / `idempotency_conflict_outbound_send` without creating a second outbound record or external send. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency Keys`; `_bmad-output/planning-artifacts/epics.md#Story 6.3`]
5. Given acceptance coverage runs, then tests prove: approval request projection to S6/conversation approval surface; approve/reject/revision/cancel decision retention; approval disabled for insufficient authority and expired evidence; no external adapter call before approval; current-authority recomputation at send time; metadata-only audit/problem/log refs; single-shot outbound-send idempotency; no draft body in audit refs, problem details, disabled reasons, or logs; and architecture guards preventing UI/CLI/MCP from referencing server outbound, gateway, audit, idempotency, or provider internals. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow 8`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/implementation-artifacts/6-2-outbound-draft-creation-within-authority.md#Testing Notes`]

## Tasks / Subtasks

- [x] Define outbound approval/send contracts in `Hexalith.ChatBot.Contracts` (AC: 1, 2, 3, 4)
  - [x] Add typed commands such as `RequestOutboundSendApproval`, `DecideOutboundApproval`, and `ExecuteApprovedOutboundDraft` (names may vary, but keep them imperative and explicit); all implement `IChatBotCommand`.
  - [x] Include stable ids for approval id, draft id, project id, requester, approver/send actor, source conversation/message/item refs, recipient refs, context refs, policy snapshot, expected source versions, correlation id, and schema version.
  - [x] Reuse `ApprovalDecisionKind`, `ApprovalStatus`, `ApprovalEventKind`, `ApprovalEvidenceFreshness`, and `SenderAuthorityClass`; do not create parallel enum vocabularies unless an outbound-specific token is not covered.
  - [x] Add an outbound approval content snapshot DTO that can preserve proposed and approved governed content, but keep body/subject out of audit evidence, problem details, logs, disabled-action text, queue row labels, and idempotency conflict messages.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` if any command is exposed through the Contract Spine.
- [x] Implement server-owned outbound approval records under `src/Hexalith.ChatBot.Server/Governance/Outbound/` and aggregate state (AC: 1, 2, 3)
  - [x] Add outbound-specific events/rejections such as `OutboundApprovalRequested`, `OutboundApprovalDecisionRecorded`, `OutboundApprovalOutcomeRecorded`, `OutboundSendStarted/Succeeded/Rejected`, and state tracking in `GovernedOperationAggregate` / `GovernedOperationState`.
  - [x] Keep outbound approval events distinct from `AiActionApprovalRequested` / `AiActionApprovalDecisionRecorded`; reuse projection/view shapes where appropriate, not AI-specific domain events.
  - [x] Validate request/decision/source-version transitions in the aggregate; never throw for business denials, and never mutate a closed approval record. Supersede rather than mutate if revision later creates a new draft or approval.
  - [x] Preserve proposed content at request time and approved content at approval time. If `request-revision` is selected, record the decision and safe next action but do not alter or send the draft in this story.
- [x] Wire CommandGateway admission, authorization, approval gate, audit, and idempotency (AC: all)
  - [x] Add new command names to `ChatBotSpineCommandAllowlist` only after authorization, approval gate, audit refs, idempotency, aggregate handling, and tests exist.
  - [x] Add an outbound authorization/evaluator seam that recomputes sender authority with `SenderAuthorityClassifier` using `AuthenticatedUserSend`, `SharedMailboxSend`, `SendOnBehalf`, or `ApprovedServiceSend` as appropriate. Do not allow callers to self-assert authority.
  - [x] Extend `AiActionApprovalGate` or create a narrow outbound approval gate implementation behind the existing `IApprovalGate`; preserve the gateway order `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> pre-commit-audit -> dispatch -> post-commit-audit`.
  - [x] Extend `CoarseIdempotencyOperationClass` / `CoarseIdempotencyComposer` with a real `outbound-send` branch using `tenant + outboundDraftId + sendActor`; conflict code must be `idempotency_conflict_outbound_send`.
  - [x] Extend `AuditEnvelopeFactory.SourceEvidenceRefs(...)` with metadata-only outbound approval/send refs: command, correlation, phase, outbound draft, approval, sender authority, requester, approver/send actor, project, policy snapshot, recipient refs, context refs, and adapter mode. Never include subject/body or unauthorized display values.
- [x] Add outbound adapter boundary without bypassing approval (AC: 1, 3, 4)
  - [x] Introduce or extend a server-owned port under `src/Hexalith.ChatBot.Server/Adapters/Mailbox/` for approved outbound send. The port is invoked only after gateway approval, idempotency, pre-commit audit, and aggregate send acceptance have succeeded.
  - [x] Keep tests on fake/in-memory adapter behavior unless a production adapter already exists. If adding Microsoft Graph production wiring, confine Graph calls to the adapter and prove no UI/CLI/MCP/server governance code calls Graph directly.
  - [x] Validate adapter approved mode, tenant, mailbox/send identity, Graph permission posture, and authority evidence before sending. Adapter unavailable or not-approved-mode returns a typed failure and leaves the draft unsent.
  - [x] Do not implement inbound DMARC/DKIM/SPF passthrough, header inspection, external-sender posture, tenant policy editor UI, notification routing, replay outbound adapters, or M2 replay isolation in this story.
- [x] Project outbound approval to S6/conversation/audit surfaces (AC: 1, 2, 5)
  - [x] Reuse/extend `PublishedApprovalEvent`, `ApprovalProjectionHandler`, `ApprovalProjectionTranslator`, `ApprovalEventView`, and `ProjectConversationItemView.FromApprovalEvent(...)` for outbound approval request/decision/outcome materialization.
  - [x] Extend `ApprovalProjectionEndpoints` if outbound events need a non-AI published event envelope; keep projection handlers idempotent and source-version ordered.
  - [x] Extend `ChatBotApprovalConversationItem.razor` and UI text only where needed to display outbound draft id, recipients, sender authority, evidence freshness, policy snapshot visibility, expected post-state, approved/outcome status, and disabled approve reasons.
  - [x] Preserve UX requirements: disabled approve remains focusable with `aria-disabled` or adjacent reachable reason; expired evidence disables approval; blocked states are redacted and reachable; phone/tablet approval controls keep accessible target size.
- [x] Add focused test coverage (AC: all)
  - [x] Contract tests for JSON/OpenAPI shape, finite enum tokens, metadata-only public fields, governed content snapshot serialization, and absence of tokens/raw claims/provider payloads/mailbox display/body leakage.
  - [x] Server governance tests for request creation, all four decisions, authority recomputation, conflict reasons, expired/stale evidence, service-send paired approval, and metadata-only denials.
  - [x] Gateway tests for allowlist admission, approval gate order, no external adapter before approval, audit refs, idempotency replay/conflict, operation status, and no durable dispatch on denied authority.
  - [x] Aggregate tests for source-version validation, append-only records, closed-record rejection, single-shot outbound send, and no-op/rejection behavior.
  - [x] Projection/UI tests for S6 approval rendering, disabled approve reasons, evidence freshness chips, safe redaction of policy/audit refs, and outcome materialization.
  - [x] Architecture tests for adapter boundaries: UI/CLI/MCP depend on Client only; no surface references `Server/Governance/Outbound`, gateway stages, Dapr, EventStore internals, or Graph provider adapters.
  - [x] Conformance/CLI/MCP tests only if outbound approval/send commands are exposed through those surfaces in this story.

## Dev Notes

### Scope Boundaries

- Story 6.3 is the outbound approval/send gate. Story 6.2 already created the ChatBot-local draft record and explicitly left approval, approval-record retention, and send idempotency for this story.
- Nothing leaves ChatBot before approval. A request to send creates/uses an approval request; the external adapter is invoked only after an approved decision, current authority recomputation, idempotency admission, pre-commit audit, and aggregate acceptance.
- Do not treat `CreateOutboundDraft` as a send command. Draft creation remains `draft-only`, no M365 posture, and no external side effect.
- Approval records are derived decision snapshots: append-only, source-versioned, redaction-aware, retention-classed, and superseded rather than mutated.
- Preserve current AI approval behavior. Outbound approval may reuse shared approval projection and UX primitives, but must not overload AI-specific domain event types or command names.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Contracts/Commands/CreateOutboundDraft.cs` and `OutboundDraftContent.cs` - source draft contract and governed content shape.
- `src/Hexalith.ChatBot.Contracts/Enums/SenderAuthorityClass.cs` / `SenderAuthorityClasses.cs` - canonical FR48 authority tokens.
- `src/Hexalith.ChatBot.Contracts/Enums/ApprovalDecisionKind.cs`, `ApprovalStatus.cs`, `ApprovalEventKind.cs`, `ApprovalEvidenceFreshness.cs` - existing approval vocabulary.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassifier.cs` and `SenderAuthorityClassificationRequest.cs` - authority classifier for send-time recomputation.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundDraftAuthorityEvaluator.cs` - pattern for server-owned outbound evidence extraction; add a send evaluator rather than duplicating logic in adapters.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundDraftEvents.cs` - local draft events; extend carefully or split new outbound approval/send events into dedicated files.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`, `ChatBotSpineCommandAllowlist.cs`, `Stages/ParticipantAuthorizationStage.cs`, and `Stages/AiActionApprovalGate.cs` - existing governed admission path and approval authority pattern.
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs` and `CoarseIdempotencyComposer.cs` - `outbound-send` is already reserved in `All`; add the missing composer logic.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` and `ChatBotStateWritingPathInventory.cs` - state-writing path inventory already includes `outbound-send`; add metadata-only refs.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` and `GovernedOperationState.cs` - current aggregate/state for draft, approval, and execution events.
- `src/Hexalith.ChatBot.Server/Projections/PublishedApprovalEvent.cs`, `ApprovalProjectionHandler.cs`, `ApprovalProjectionTranslator.cs`, `ApprovalEventView.cs`, and `ProjectConversationItemView.FromApprovalEvent(...)` - reusable approval projection route to conversation/S6.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor` - existing approval rendering, evidence freshness, disabled approve reason, and keyboard/focus behavior.
- `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/OutboundDraftCreationTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, and `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs` - current outbound draft and gateway patterns.

### Current State To Preserve

- `CommandGateway` remains the only write admission spine. No controller, UI component, CLI command, MCP tool, service client, AI actor, worker, or mailbox adapter may write outbound approval/send state directly.
- Surface adapters depend on `Hexalith.ChatBot.Client`; architecture tests already forbid direct references to `.Server`, gateway stages, outbound internals, Dapr, EventStore internals, and provider adapters.
- `CreateOutboundDraft` stores governed draft content but audit refs and public denial paths are metadata-only.
- `OutboundDraftAuthorityEvaluator` denies draft creation when M365 send posture exists. That remains correct; send-time authority is separate and must use outbound-send scope.
- `CoarseIdempotencyOperationClass.All` already contains `outbound-send`, and `ChatBotStateWritingPathInventory.Paths` already contains `outbound-send`. Do not add a second operation name.
- Existing AI approval commands and CLI/MCP approval commands are AI-specific. Do not silently route outbound approvals through `DecideAiActionApproval` or `ExecuteApprovedAIAction`.
- Root-level submodule policy applies: initialize/update only root `.gitmodules` submodules and never run recursive submodule commands.

### Architecture Guardrails

- Contracts belong in `.Contracts`; server-owned outbound approval/send logic belongs in `.Server/Governance/Outbound/`; provider ports belong under `.Server/Adapters/Mailbox/`; aggregate handling remains in `.Server/Operations/`.
- Aggregates remain pure: no I/O, no Graph calls, no Dapr calls, no sibling client calls, no logs, and no exceptions for business denials.
- Tenant ID comes from authenticated gateway context, not command body. Actor/source identities must bind to authenticated actor or validated service-client delegated requester evidence.
- Store stable refs (`project:<id>`, `recipient:<id>`, `outbound-draft:<id>`, `approval:<id>`, `policy-snapshot:<id>`, `file:<id>`, `conversation:<id>`) instead of copying upstream display names or unauthorized PII.
- Evidence freshness is enforcement, not display-only. Expired evidence disables approval with reason `evidence-expired`; stale evidence is flagged but may be permitted only if policy allows.
- If the implementation emits outbound approval projection events, include source version and make projection handlers duplicate/replay/order tolerant.

### Latest Technical Information

- Microsoft Graph v1.0 sends an existing draft with `POST /me/messages/{id}/send` or `POST /users/{id | userPrincipalName}/messages/{id}/send`, returns `202 Accepted`, requires `Mail.Send`, and saves to Sent Items. This is adapter-only context; ChatBot approval/audit/idempotency must complete before this call. [Source: Microsoft Learn, message: send, https://learn.microsoft.com/en-us/graph/api/message-send?view=graph-rest-1.0]
- Microsoft Graph v1.0 can create provider drafts with `POST /me/messages` or `POST /users/{id|userPrincipalName}/messages`; by default this saves in Drafts, and sending is a subsequent operation. Story 6.2 did not create provider drafts, so any provider-draft creation/send pairing must remain behind the approved outbound adapter. [Source: Microsoft Learn, Create message, https://learn.microsoft.com/en-us/graph/api/user-post-messages?view=graph-rest-1.0]
- Microsoft Graph distinguishes delegated and application permissions. `Mail.Send` can send mail; `Mail.Send.Shared` is delegated send-on-behalf. These provider permissions are evidence only and do not replace ChatBot tenant policy, project authority, service-client grant, paired approval, audit, or idempotency. [Source: Microsoft Learn, permissions overview, https://learn.microsoft.com/en-us/graph/permissions-overview; Microsoft Learn, permissions reference, https://learn.microsoft.com/en-us/graph/permissions-reference?view=graph-rest-1.0]

### Previous Story Intelligence

- Story 6.1 is done and added finite sender-authority class tokens, conflict reason tokens, metadata-only authority result contracts, and the internal `SenderAuthorityClassifier`.
- Story 6.1 review fixed approved service-send ordering: missing outbound service-client grant must not be masked as approval missing. Preserve precise safe denial reason ordering.
- Story 6.2 is done and added `CreateOutboundDraft`, `OutboundDraftContent`, `OutboundDraftCreated`, draft-specific authority evaluation, gateway admission, metadata-only audit refs, and `outbound-draft-creation` idempotency.
- Story 6.2 review fixed source-actor binding and service/AI delegated requester evidence. Carry this forward to send actor and approver attribution; never trust a submitted actor id without gateway evidence.
- Recent commits:
  - `e5cce4b feat(story-6.2): Governed outbound draft creation`
  - `2aa9c82 feat(story-6.1): Sender authority classes and M365 mapping`
  - `5667c4b docs(epic-5): add retrospective`
  - `4d3ad3d test(story-5.4): Cross-surface equivalence verification`
  - `e40d6fc feat(story-5.3): MCP adapter and governed tool surface`

### Project Structure Notes

- Likely new files:
  - `src/Hexalith.ChatBot.Contracts/Commands/RequestOutboundSendApproval.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/DecideOutboundApproval.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/ExecuteApprovedOutboundDraft.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/OutboundApprovalContentSnapshot.cs`
  - `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundApprovalEvents.cs`
  - `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundSendEvents.cs`
  - `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundSendAuthorityEvaluator.cs`
  - `src/Hexalith.ChatBot.Server/Adapters/Mailbox/IOutboundMailboxSender.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/OutboundApprovalContractTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/OutboundApprovalTests.cs`
- Likely update files:
  - `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
  - `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
  - `tests/fixtures/hexalith-chatbot-generated-client.sha256`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs` or a new outbound `IApprovalGate` collaborator
  - `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionEndpoints.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Services/*Text*` / localization resources if new labels are required
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs` if new provider boundaries are added

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` if UI approval rendering changes.
- Add `Client.Tests` and `Conformance.Tests` if OpenAPI/generated client or cross-surface outbound exposure changes.
- Add `Cli.Tests` and `Mcp.Tests` only if CLI/MCP outbound approval/send commands are actually exposed.
- Sandbox note inherited from previous stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Use xUnit v3, Shouldly, NSubstitute/fakes, warnings-as-errors, central package management, `net10.0`, and no package/SDK upgrades.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 6 defines outbound communication/inbound authenticity and Story 6.3 defines S6 outbound approval, approval-record retention, and outbound-send idempotency.
- Loaded PRD/addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`, especially FR42, FR45-FR50, NFR13/NFR13a, NFR15a, NFR46-NFR50a, authority mapping, conflict rules, and outbound-send idempotency.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway flow, adapter boundaries, `Server/Governance/Outbound/`, approval records as append-only derived snapshots, audit envelope, and project structure.
- Loaded UX context from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`; relevant S6 carry-forward is Flow 8, approval panel/control behavior, evidence freshness chips, disabled approve reasons, blocked states, accessibility, and responsive approval.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant carry-forward: .NET SDK `10.0.300`, `net10.0`, central package management, warnings-as-errors, metadata-only diagnostics, tenant isolation, pure EventStore aggregates, Dapr duplicate/order tolerance, xUnit v3/Shouldly/NSubstitute, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md` and `_bmad-output/implementation-artifacts/6-2-outbound-draft-creation-within-authority.md`.
- Inspected current likely reuse/update files: outbound draft contracts/events/evaluator, sender-authority classifier/request, gateway allowlist, participant authorization, approval gate, accepted dispatcher, idempotency composer/classes, audit envelope, aggregate/state, approval projections, UI approval component, CLI approval commands, and outbound tests.
- Web research checked current Microsoft Learn pages for Graph send existing draft, Graph create draft, Graph permissions overview, and Graph mail permissions. No package/version changes are required.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 6 and Story 6.3 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR42, FR45-FR50, NFR13/NFR13a, NFR15a, NFR46-NFR50a.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - command pipeline, idempotency keys, authority class mapping, conflict rules, replay isolation.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, module boundaries, project structure, FR mapping, audit envelope, idempotency, testing expectations.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - Flow 8, approval controls, evidence freshness, blocked-state, accessibility.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - approval panel/control semantics and visual status vocabulary.
- `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md` - sender-authority classifier foundation and review learnings.
- `_bmad-output/implementation-artifacts/6-2-outbound-draft-creation-within-authority.md` - outbound draft command, server wiring, tests, and review learnings.
- `src/Hexalith.ChatBot.Contracts/Commands/CreateOutboundDraft.cs` - current draft command.
- `src/Hexalith.ChatBot.Contracts/Commands/OutboundDraftContent.cs` - governed draft content DTO.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassifier.cs` - server-owned sender-authority classifier.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundDraftAuthorityEvaluator.cs` - outbound evidence evaluator pattern.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - write admission spine.
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs` - idempotency composer.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - metadata-only audit refs.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - current command/event aggregate handling.
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs` - approval projection translation.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor` - approval UI behavior.
- Microsoft Learn Graph message send - `https://learn.microsoft.com/en-us/graph/api/message-send?view=graph-rest-1.0`.
- Microsoft Learn Graph create message - `https://learn.microsoft.com/en-us/graph/api/user-post-messages?view=graph-rest-1.0`.
- Microsoft Learn Graph permissions overview - `https://learn.microsoft.com/en-us/graph/permissions-overview`.
- Microsoft Learn Graph permissions reference - `https://learn.microsoft.com/en-us/graph/permissions-reference?view=graph-rest-1.0`.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 127/127.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 494/494.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 15/15.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 37/37.
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - passed, 97/97.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 66/66.
- UI E2E was not run because no UI rendering component was changed; existing UI approval tests and server projection tests were run instead.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - review rerun passed, 0 warnings, 0 errors.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - review rerun passed, 494/494.

### Completion Notes List

- Added outbound approval request, decision, send execution, and content snapshot contracts with OpenAPI/client regeneration.
- Added server-owned outbound approval/send events, aggregate state transitions, metadata-only rejections, approval retention, closed-record protection, and single-shot send behavior.
- Wired CommandGateway allowlist, participant authorization, approval gate policy, outbound-send coarse idempotency, metadata-only audit refs, send-time authority recomputation, and a server mailbox adapter port with an unavailable fail-closed default.
- Reused the shared approval projection/conversation item surface and added outbound-specific projection coverage for draft refs, recipients, sender authority, freshness, policy visibility, and metadata-only redaction.
- Added focused contract, aggregate, gateway, projection, client-generation, UI, conformance, and architecture validation coverage.
- Review fixed outbound send approval-scope validation so execution must match the approved command name, allowlist version, policy snapshot, source refs, sender authority, recipients, and context refs.
- Review fixed send-time evidence enforcement so stale or expired current outbound evidence is denied before idempotency, audit, dispatch, or durable mutation.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-02.

Outcome: Approved after auto-fixes.

Findings fixed:

- HIGH: `ExecuteApprovedOutboundDraft` did not fully bind the send command to the paired approval request. The aggregate now rejects mismatched policy snapshot, command name, allowlist version, source refs, sender authority, recipients, or context refs before any send record is accepted.
- HIGH: outbound send accepted stale command evidence unless the value was explicitly expired. The aggregate now requires fresh send evidence, and the gateway authorization path denies stale/expired current evidence from trusted server-side claims before idempotency, audit, or dispatch.

Validation checklist:

- Story file loaded and status verified as reviewable.
- Acceptance criteria, completed tasks, File List, and changed source/test files reviewed.
- Architecture/project context and story technical references checked from the story and planning artifacts.
- MCP/doc search was not needed for the code fixes; no external package or provider API behavior changed.
- Tests mapped to fixes: aggregate approval-scope mismatch, aggregate stale/expired send evidence, gateway stale/expired current evidence denial.
- Outcome: no critical issues remain after fixes; story status set to done.

### File List

- `_bmad-output/implementation-artifacts/6-3-outbound-approval-gate-and-approval-record.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Contracts/Commands/DecideOutboundApproval.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/ExecuteApprovedOutboundDraft.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/OutboundApprovalContentSnapshot.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/RequestOutboundSendApproval.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Server/Adapters/Mailbox/IOutboundMailboxSender.cs`
- `src/Hexalith.ChatBot.Server/Adapters/Mailbox/UnavailableOutboundMailboxSender.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundApprovalEvents.cs`
- `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundSendAuthorityEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OutboundApprovalContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

## Change Log

- 2026-06-02: Implemented story 6.3 outbound approval gate, approval record retention, send-time authority recomputation, outbound-send idempotency, adapter boundary, projection coverage, and validation updates.
- 2026-06-02: Senior Developer Review auto-fixed outbound paired-approval scope validation and stale/expired send-evidence enforcement; review validation passed.
