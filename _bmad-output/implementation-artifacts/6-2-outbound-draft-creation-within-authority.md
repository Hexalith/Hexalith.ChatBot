---
baseline_commit: 2aa9c82405917784c79037915e6e0c4cbb0d933e
---

# Story 6.2: Outbound draft creation within authority

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As an authorized contributor,
I want to create outbound project email drafts within my project and sender authority,
so that responses originate from governed project context.

## Acceptance Criteria

1. Given the Story 6.1 sender-authority classifier resolves `draft-only`, when an authorized contributor with project authority and `outbound-draft` scope creates an outbound draft, then ChatBot creates a durable in-product outbound draft record under the approved tenant/project and records sender authority as `draft-only`; the operation does not call Microsoft Graph, Exchange, SMTP, mailbox workers, or any external outbound adapter. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Authority class mapping (FR48 five-class taxonomy)`]
2. Given a draft creation request carries any M365/Exchange send posture, requests a non-`draft-only` authority class, lacks project authority, lacks `outbound-draft`, or is blocked by tenant outbound policy, when the request is evaluated, then the request fails closed before durable draft mutation with a redacted reason mapped to `policy-blocked` or `insufficient-authority` as appropriate; public problem details, audit refs, logs, and projection rows must not reveal unauthorized project, mailbox, recipient display, raw provider, token, claim, or draft body details. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR46`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`]
3. Given a draft is accepted, when the command passes through the gateway, then pre-commit audit, coarse idempotency, lifecycle validation, EventStore dispatch, post-commit audit, and operation-status projection use the existing CommandGateway path; audit evidence includes safe refs for `outbound-draft:<draft_id>`, `sender-authority:draft-only`, `requester:<id>`, `project:<id>`, `policy-snapshot:<id>`, and any safe context/recipient refs, but never the draft body or unauthorized display values. [Source: `_bmad-output/planning-artifacts/architecture.md#Process Patterns`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR50`]
4. Given the same contributor submits the same draft creation intent again, when the canonical command input is equivalent, then ChatBot returns the prior accepted draft outcome without creating a second durable draft; when the same draft id or idempotency identity is reused with materially different content, recipients, project, requester, sender authority, or context refs, ChatBot rejects with a stable idempotency conflict and leaves the existing draft unchanged. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR13`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency Keys`]
5. Given acceptance coverage runs, then tests prove: successful `draft-only` draft creation; denial for missing project authority; denial for missing `outbound-draft`; denial when M365 send posture is present; denial when tenant policy disables `draft-only`; no external adapter/Graph call; metadata-only audit and problem payloads; duplicate/equivalent replay behavior; conflicting duplicate rejection; and architecture guards that prevent UI/CLI/MCP from referencing `Server/Governance/Outbound`, gateway stages, provider adapters, or draft storage internals. [Source: `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md#Testing Notes`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]

## Tasks / Subtasks

- [x] Define outbound draft contracts in `Hexalith.ChatBot.Contracts` (AC: 1, 2, 3, 4)
  - [x] Add a typed `CreateOutboundDraft` command implementing `IChatBotCommand`; include stable identifiers for `DraftId`, `ProjectId`, requester/source actor, source conversation/message/item refs, recipient refs, policy snapshot, correlation, schema version, redaction state, retention class, and the draft content fields required to render the draft later.
  - [x] Use `SenderAuthorityClass.DraftOnly` or an equivalent finite token field for the authority result; do not let callers supply arbitrary authority strings.
  - [x] Keep recipient and context values as safe refs where possible (`recipient:<id>`, `conversation:<id>`, `source-message:<id>`, `file:<id>`). If draft subject/body content is part of the contract, mark it as governed content and keep it out of audit evidence/problem/log contracts.
  - [x] Add contract tests for JSON wire shape, default schema version, finite `draft-only` authority serialization, and absence of secret-bearing/public payload properties such as tokens, raw claims, provider payloads, raw headers, mailbox names, unauthorized project names, and recipient display names.
  - [x] If the command is exposed through the Contract Spine, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- [x] Implement server-owned draft creation under `src/Hexalith.ChatBot.Server/Governance/Outbound/` and existing EventStore operation wiring (AC: 1, 2, 3, 4)
  - [x] Reuse `SenderAuthorityClassifier.Classify(...)` with `SenderAuthorityIntent.DraftOnly`; do not duplicate draft-only authorization logic in UI/CLI/MCP or a mailbox adapter.
  - [x] Build `SenderAuthorityClassificationRequest` from authenticated actor, tenant/project authority evidence, tenant outbound policy snapshot, and explicit no-send M365 posture (`HasAnySendPosture == false`).
  - [x] Create an `OutboundDraftCreated` event and state tracking in `GovernedOperationAggregate`/`GovernedOperationState` or a narrowly named outbound aggregate if the existing aggregate becomes unclear; keep `Handle` pure and return structured rejections for business denials.
  - [x] Add draft-id/fine-idempotency checks so the same draft cannot be created twice with different semantic inputs.
  - [x] Add `CreateOutboundDraft` to `ChatBotSpineCommandAllowlist` only after gateway, audit, idempotency, lifecycle, and tests are in place.
  - [x] Extend lifecycle/idempotency operation-class handling for outbound draft creation. Do not reuse `outbound-send`; draft creation is not a single-shot send. If a new operation class is required, add it to the inventory/tests and document the key composition in this story's tests until the addendum is updated.
- [x] Wire fail-closed authorization, audit, and status behavior (AC: 2, 3, 4)
  - [x] Extend `ParticipantAuthorizationStage` or a narrow outbound authorization seam so human contributors must have project authority and `outbound-draft`; service/AI actors may create drafts only when their existing grant/proposal path supplies equivalent authority and safe originating requester evidence.
  - [x] Map missing project authority to a redacted authorization denial and missing outbound-draft or tenant policy block to stable safe reasons; do not expose whether an unauthorized project, mailbox, or recipient exists.
  - [x] Extend `AuditEnvelopeFactory.SourceEvidenceRefs(...)` for `CreateOutboundDraft` with safe refs only: command id, correlation, phase, draft id, sender authority, requester, project, policy snapshot, and safe context/recipient refs.
  - [x] Ensure operation status and post-commit audit report an accepted in-product draft outcome, not a sent/outbound-delivered outcome.
- [x] Preserve strict scope boundaries (AC: all)
  - [x] Do not call Microsoft Graph, Exchange Online, SMTP, mailbox workers, or an outbound provider adapter.
  - [x] Do not send email, create an M365 mailbox draft, add S6 outbound approval UI, approve outbound sends, store approval records, or implement the Story 6.3 single-shot send idempotency key.
  - [x] Do not implement inbound DMARC/DKIM/SPF passthrough, header inspection, external-sender posture, on-behalf-of inbound changes, tenant policy editor UI, replay outbound adapters, CLI/MCP outbound commands, or notification routing.
  - [x] Do not relax adapter boundary tests or let surface projects depend on `.Server`, `Gateway`, `Governance/Outbound`, Dapr, EventStore internals, or mailbox provider adapters.
- [x] Add focused test coverage (AC: all)
  - [x] Contract tests in `tests/Hexalith.ChatBot.Contracts.Tests` for `CreateOutboundDraft` and any outbound draft DTOs/events exposed across boundaries.
  - [x] Server tests in `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/` for successful draft creation, all denial paths, metadata-only output, and classifier reuse.
  - [x] Aggregate/state tests in `tests/Hexalith.ChatBot.Server.Tests/Operations/` if `GovernedOperationAggregate` handles the new command/event.
  - [x] Gateway tests for allowlist admission, pre/post audit evidence, idempotency replay/conflict, operation status, and no durable dispatch on denied authority.
  - [x] Architecture tests in `tests/Hexalith.ChatBot.Architecture.Tests` if any new boundary or generated client exposure is introduced.
  - [x] Integration/conformance tests only if the public client/OpenAPI surface changes or a production adapter arm can submit `CreateOutboundDraft`; do not add CLI/MCP parity tests unless CLI/MCP commands are actually exposed.

## Dev Notes

### Scope Boundaries

- Story 6.2 creates a governed ChatBot-local outbound draft record. It does not create a draft in Microsoft 365 and does not send anything outside ChatBot.
- `draft-only` means no M365 send posture and no external outbound side effect. If Graph/Exchange permissions are present in the evidence, the classifier must deny draft-only rather than treating that as extra authority.
- Draft creation may store governed draft content so it can be previewed/revised/approved later. That content must be data-minimized, redaction-aware, retention-classed, and excluded from audit refs, problem details, logs, and disabled-action text.
- Do not implement the Story 6.3 S6 outbound send approval gate in this local draft story. If existing AI/action policy classifies a draft creation request as approval-required because it uses AI output, file context, or external-recipient preview content, preserve that policy path; the boundary rule is that no external send can execute until Story 6.3.
- If an AI proposal produces the draft content, keep the existing AI proposal/approval semantics intact. This story should not create an AI bypass around `ProposeAIAction`, `ExecuteLowRiskAIAssistance`, or `ExecuteApprovedAIAction`.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Contracts/Enums/SenderAuthorityClass.cs` and `SenderAuthorityClasses.cs` - canonical five authority classes and wire tokens.
- `src/Hexalith.ChatBot.Contracts/Identities/SenderAuthorityClassificationResult.cs` - metadata-only classification result shape; reuse/extend rather than creating a second authority result DTO.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs` - finite disabled reasons including `policy-blocked`, `insufficient-authority`, and `not-authorized`.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotRefusalReasonCodes.cs` - use `sender-authority-denied` / `project-authorization-denied` categories instead of free-form refusal text.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassifier.cs` - server-owned authority classifier; its `DraftOnly` path already checks tenant policy, no send posture, project authority, and `outbound-draft`.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassificationRequest.cs` - evidence model for tenant policy, M365 posture, and project authority.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - only write admission path: auth, tenant-bind, authorize, allowlist, risk, approval, idempotency, lifecycle, pre-commit audit, dispatch, post-commit audit.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` - add `CreateOutboundDraft` here only when the command is ready to enter the governed spine.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - current human/project/service authorization patterns; extend narrowly for `outbound-draft`.
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyCanonicalizer.cs` and `CoarseIdempotencyOperationClass.cs` - canonical input hashing and operation-class pattern.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - central metadata-only audit evidence extraction.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` and `GovernedOperationState.cs` - current EventStore aggregate/state for ChatBot governed operation events. Add outbound draft handling here only if it remains cohesive.
- `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/SenderAuthorityClassifierTests.cs` and `tests/Hexalith.ChatBot.IntegrationTests/Governance/Outbound/SenderAuthorityClassificationWorkflowE2ETests.cs` - patterns for `draft-only` success and metadata-only authority output.
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs` and `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` - adapter boundary guards already include `Server.Governance.Outbound`.

### Current State To Preserve

- `CommandGateway` remains the only external write admission spine. Do not create a direct controller, worker, provider, UI, CLI, or MCP path that writes drafts outside the gateway.
- Surface adapters depend on `Hexalith.ChatBot.Client`; architecture tests forbid direct references to server/gateway/outbound internals. Keep this intact.
- Story 6.1 established that authority is server-computed from evidence. Do not let a caller self-assert `draft-only` without server evidence validation.
- Existing audit and message-catalog behavior is metadata-only and redacted. Do not add draft content to `AuditEnvelope.SourceEvidenceRefs`, problem details, disabled reasons, operation-status partial outputs, or logs.
- The existing state-writing path inventory already contains `outbound-send`; do not misuse that code for draft creation. Add a draft-specific operation class if implementation needs idempotency/status separation.
- Root-level submodule policy applies: initialize/update only root `.gitmodules` submodules and never run recursive submodule commands.

### Architecture Guardrails

- Contracts belong in `.Contracts`; server-owned draft orchestration belongs in `.Server/Governance/Outbound/`; EventStore command handling belongs in `.Server/Operations/` if the current aggregate owns it.
- Aggregates/handlers must be pure: no I/O, no Graph calls, no Dapr calls, no sibling client calls, no logging of payloads, and no exceptions for business denials.
- Tenant ID must come from gateway/authenticated context, not from request body authority.
- Use stable IDs and safe refs. Store source project/conversation/file/recipient references by ID; do not copy sibling context display names or personal data into events unless the contract explicitly marks the data as authorized draft content.
- If OpenAPI changes are made, regenerate the generated client and fixture hash in the same change. Never hand-edit generated client files except through regeneration.
- Keep `draft-only` and `outbound-send` separate. Sending a draft and approval-record retention are Story 6.3.

### Latest Technical Information

- Microsoft Graph v1.0 supports creating a new message draft with `POST /me/messages` or `POST /users/{id | userPrincipalName}/messages`; by default this saves a provider draft in the Drafts folder. That is future adapter context only and must not be invoked in Story 6.2. [Source: Microsoft Learn, Create message, https://learn.microsoft.com/en-us/graph/api/user-post-messages?view=graph-rest-1.0]
- Microsoft Graph v1.0 sends an existing draft with `POST /me/messages/{id}/send` or `POST /users/{id | userPrincipalName}/messages/{id}/send` and returns `202 Accepted`; that belongs to Story 6.3+ outbound-send work, not local draft creation. [Source: Microsoft Learn, message: send, https://learn.microsoft.com/en-us/graph/api/message-send?view=graph-rest-1.0]
- Microsoft Graph mail permission docs distinguish draft creation/update permissions from send permissions. ChatBot must still enforce its own tenant policy, project authority, sender authority, audit, and approval rules rather than treating Graph permission as sufficient. [Source: Microsoft Learn, Graph permissions reference, https://learn.microsoft.com/en-us/graph/permissions-reference?view=graph-rest-1.0]

### Previous Story Intelligence

- Story 6.1 is done and added stable sender-authority class tokens, conflict reason tokens, metadata-only authority result contracts, and an internal `SenderAuthorityClassifier`.
- The classifier already proves `draft-only` success with `project-authority:outbound-draft` and denies provider posture without project authority. Build on those tests instead of duplicating logic.
- Story 6.1 review fixed service-send approval/grant ordering; carry that lesson forward: authority denials must preserve the most precise safe reason without leaking sensitive evidence.
- Epic 5 completed CLI/MCP parity and service-client grant validation. Do not expose outbound draft CLI/MCP commands until shared contracts and conformance coverage exist.
- Recent commits:
  - `2aa9c82 feat(story-6.1): Sender authority classes and M365 mapping`
  - `5667c4b docs(epic-5): add retrospective`
  - `4d3ad3d test(story-5.4): Cross-surface equivalence verification`
  - `e40d6fc feat(story-5.3): MCP adapter and governed tool surface`
  - `73847b5 feat(story-5.2): CLI adapter and workflow parity`

### Project Structure Notes

- Likely new files:
  - `src/Hexalith.ChatBot.Contracts/Commands/CreateOutboundDraft.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/OutboundDraftRecord.cs` or `src/Hexalith.ChatBot.Contracts/Identities/OutboundDraft*` if a boundary DTO is needed.
  - `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundDraftCreation*`
  - `src/Hexalith.ChatBot.Server/Operations/OutboundDraftCreated.cs`
  - `src/Hexalith.ChatBot.Server/Operations/OutboundDraftCreationRejected.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/OutboundDraftContractTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/OutboundDraftCreationTests.cs`
- Likely update files:
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
  - `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and generated client/fixture only if the command is exposed through the public command spine.
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/*` if boundary coverage changes.
- Detected conflict/variance: the PRD addendum has an `Outbound send` idempotency class but no explicit `Outbound draft creation` class. Do not silently fold drafts into `outbound-send`; tests should make the draft-specific behavior explicit and the PRD/addendum should be updated by PM/architecture later.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
- Add `Client.Tests` if OpenAPI/generated client changes are made.
- Add `Conformance.Tests`, `Cli.Tests`, and `Mcp.Tests` only if the story exposes real adapter-facing outbound draft operations through those surfaces.
- Sandbox note inherited from previous stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- No Playwright is required unless a visible draft UI is added, which is out of scope for this story.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 6 defines outbound communication and inbound authenticity, and Story 6.2 defines local outbound draft creation within `draft-only` authority.
- Loaded PRD/addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`, especially FR46-FR50, NFR2, NFR13/NFR13a, NFR15a, NFR48, NFR50, authority class mapping, and idempotency keys.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway, module boundaries, `Server/Governance/Outbound/`, adapter boundaries, metadata-only diagnostics, audit envelope, and tests.
- Loaded UX context from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`; relevant carry-forward is safe blocked-state language, sender authority in approval contexts, and no tooltip-only disabled reasoning. No visible UI surface is in scope.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant carry-forward: .NET SDK `10.0.300`, `net10.0`, central package management, warnings-as-errors, metadata-only diagnostics, tenant isolation, EventStore aggregate purity, Dapr duplicate/order tolerance, xUnit v3/Shouldly/NSubstitute, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md` and recent git history.
- Inspected current likely reuse/update files: sender-authority contracts/classifier, command allowlist, participant authorization stage, idempotency classes, audit envelope factory, governed operation aggregate/state, message/refusal reason catalogs, and adapter boundary tests.
- Web research checked current Microsoft Learn pages for Graph draft creation, Graph draft send, and Graph mail permissions. No package/version changes are required.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 6 and Story 6.2 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR46-FR50, NFR2, NFR13/NFR13a, NFR15a, NFR48, NFR50.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - authority class mapping, conflict rules, idempotency keys, inbound authenticity.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, adapter boundaries, project structure, FR mapping, metadata-only diagnostics, tests.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - blocked states, approval accessibility, safe denial, semantic surface consistency.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - approval panel semantics and status colors.
- `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md` - sender-authority classifier foundation and previous story learnings.
- `src/Hexalith.ChatBot.Contracts/Enums/SenderAuthorityClass.cs` - authority class enum.
- `src/Hexalith.ChatBot.Contracts/Identities/SenderAuthorityClassificationResult.cs` - metadata-only authority result.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassifier.cs` - server-owned authority classifier.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassificationRequest.cs` - authority evidence model.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - write admission spine.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` - governed command allowlist.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - participant/project authorization stage.
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs` - operation-class pattern.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - metadata-only audit refs.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - current command/event handling aggregate.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` - current aggregate state tracking.
- `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/SenderAuthorityClassifierTests.cs` - draft-only authority tests.
- `tests/Hexalith.ChatBot.IntegrationTests/Governance/Outbound/SenderAuthorityClassificationWorkflowE2ETests.cs` - metadata-only authority boundary tests.
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs` - adapter outbound/gateway boundary tests.
- Microsoft Learn Graph create message - `https://learn.microsoft.com/en-us/graph/api/user-post-messages?view=graph-rest-1.0`.
- Microsoft Learn Graph send existing draft - `https://learn.microsoft.com/en-us/graph/api/message-send?view=graph-rest-1.0`.
- Microsoft Learn Graph permissions reference - `https://learn.microsoft.com/en-us/graph/permissions-reference?view=graph-rest-1.0`.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - 124 passed.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - 482 passed.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - 37 passed.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - 15 passed.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - 66 passed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implemented `CreateOutboundDraft` and `OutboundDraftContent` contracts with finite `draft-only` sender authority, governed content, safe refs, OpenAPI/client generation, and generated-client hash refresh.
- Added server-owned outbound draft authority evaluation reusing `SenderAuthorityClassifier.Classify(..., DraftOnly)`, fail-closed participant authorization, no-send M365 posture handling, metadata-only audit refs, and draft-specific idempotency operation class.
- Added pure aggregate handling and state tracking for `OutboundDraftCreated`, including equivalent replay as no-op and semantic duplicate conflict rejection without any Microsoft Graph, Exchange, SMTP, mailbox worker, provider adapter, UI, CLI, or MCP draft path.
- Added focused contract, outbound governance, aggregate, gateway, architecture, client, and conformance coverage for successful draft creation, denial paths, metadata-only audit/problem behavior, idempotency replay/conflict, and boundary preservation.
- Senior review fixed outbound draft source-actor binding so durable state and audit evidence cannot be attributed to an untrusted submitted actor, and added service/AI delegated-requester evidence enforcement before idempotency, audit, or dispatch.

### File List

- _bmad-output/implementation-artifacts/6-2-outbound-draft-creation-within-authority.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs
- src/Hexalith.ChatBot.Contracts/Commands/CreateOutboundDraft.cs
- src/Hexalith.ChatBot.Contracts/Commands/OutboundDraftContent.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs
- src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs
- src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs
- src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs
- src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs
- src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundDraftAuthorityEvaluator.cs
- src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundDraftEvents.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs
- tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/OutboundDraftContractTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/OutboundDraftCreationTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs
- tests/fixtures/hexalith-chatbot-generated-client.sha256

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-02

### Outcome

Approved after automatic fixes. Story status set to `done`; sprint status synced to `done`.

### Findings Fixed

- [HIGH] Outbound draft authorization accepted a submitted `SourceActorId` that did not match the authenticated actor, allowing durable state and audit evidence to attribute a draft to an untrusted source actor. Fixed by requiring `CreateOutboundDraft.SourceActorId == ChatBotAuthenticatedActor.ActorId` before idempotency, audit, or dispatch.
- [HIGH] Service/AI draft creation did not require validated delegated requester evidence, despite the story requirement that service/AI actors create drafts only through equivalent authority and safe originating requester evidence. Fixed by requiring validated service-client grant evidence with `DelegatedUserId == RequesterId` for service/AI actors.
- [MEDIUM] Gateway tests previously used a mismatched source-actor fixture on the outbound draft happy path, so they could not detect the attribution bug. Fixed the fixture and added denial/allowance coverage for human source-actor mismatch and service delegated requester evidence.

### Validation

- Reference check: existing story references cover Microsoft Graph draft/create/send behavior; review confirmed the implementation remains ChatBot-local and does not add Graph, Exchange, SMTP, mailbox worker, or outbound provider calls.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - 124 passed.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - 482 passed.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - 37 passed.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - 15 passed.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - 66 passed.

---

Reviewer: Jerome (story-automator adversarial review) on 2026-06-11

### Outcome

Approved. No CRITICAL or HIGH issues. Every acceptance criterion is implemented and exercised by passing tests; every `[x]` task corresponds to real, verified code. One MEDIUM documentation discrepancy auto-fixed (File List was missing the gateway E2E coverage file).

### Findings Fixed

- [MEDIUM] Git-vs-story File List discrepancy: `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` carries the Story 6.2 gateway end-to-end coverage (spine happy path with no external send; the four authority-gap denial cases mapped to `insufficient-authority`/`policy-blocked` with no durable mutation; and equivalent-replay plus conflicting-duplicate idempotency) but was not listed under Dev Agent Record → File List. Added it to the File List.

### Validation

- Re-verified AC1-AC5 against the actual implementation: `CreateOutboundDraft`/`OutboundDraftContent` contracts (finite `draft-only`, safe refs, governed content); `OutboundDraftAuthorityEvaluator` reuse of `SenderAuthorityClassifier`; pure aggregate `Handle(CreateOutboundDraft)` with equivalent-replay no-op and semantic-conflict rejection; metadata-only `OutboundDraftEvidenceRefs` (no subject/body in audit refs); allowlist admission; draft-specific `OutboundDraftCreation` idempotency class; fail-closed `ParticipantAuthorizationStage` source-actor binding and service/AI delegated-requester evidence. No Microsoft Graph/Exchange/SMTP/mailbox/provider adapter call path; no CLI/MCP/UI reference to the outbound draft surface.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed (0 warnings, 0 errors).
- `Hexalith.ChatBot.Contracts.Tests -parallel none` - 480 passed.
- `Hexalith.ChatBot.Server.Tests -parallel none` - 1563 passed.
- `Hexalith.ChatBot.Architecture.Tests -parallel none` - 39 passed.
- `Hexalith.ChatBot.Client.Tests -parallel none` - 34 passed.
- `Hexalith.ChatBot.Conformance.Tests -parallel none` - 93 passed.

## Change Log

- 2026-06-02: Implemented governed outbound draft creation within draft-only authority; added contract/OpenAPI/client, gateway authorization/audit/idempotency, aggregate state/event handling, and focused validation coverage.
- 2026-06-02: Senior review auto-fixed outbound draft source-actor binding and service/AI delegated requester evidence enforcement; added gateway regression coverage and marked story done.
- 2026-06-11: Story-automator adversarial review re-validated AC1-AC5 against current implementation (build clean; 480 contract, 1563 server, 39 architecture, 34 client, 93 conformance tests passing); auto-fixed File List to document the gateway E2E coverage file; status remains done.
