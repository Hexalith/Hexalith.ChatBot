---
baseline_commit: 5667c4bbce8485265d73529b47911cbd36464bdc
---

# Story 6.1: Sender-authority classes and M365 mapping

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As a security engineer,
I want the five outbound sender-authority classes distinguished and mapped to M365 posture,
so that outbound authority is explicit and conflicts fail closed.

## Acceptance Criteria

1. Given an outbound action, when sender authority is determined, then the system distinguishes exactly these FR48 authority classes: `draft-only`, `authenticated-user send`, `shared-mailbox send`, `send-on-behalf`, and `approved service-send`; each class maps to the fixed M365/Exchange permission posture, ChatBot authorization requirement, and audit field set in the PRD addendum authority table. Tenant policy may disable a class, but must not redefine a class. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.1`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Authority class mapping (FR48 five-class taxonomy)`]
2. Given sender-authority evidence is evaluated, when the classifier sees M365/Exchange or Graph posture, tenant policy, requester identity, project authority, service-client grant, shared-mailbox membership, delegation evidence, or approval-chain evidence, then ChatBot computes authority server-side and treats provider posture as evidence only. UI/CLI/MCP arguments must never self-assert or override authority. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-02.md#Action Items`]
3. Given M365 grants send-on-behalf but tenant policy disallows it, when an outbound action is classified, then the action fails closed with stable reason `policy-blocked`, records metadata-only denial/audit facts, and leaks no mailbox, project, delegate, provider payload, token, or raw header details. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Conflict resolution rules`; `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs`]
4. Given M365 grants send-on-behalf to delegate A but the proposed requester is delegate B, when an outbound action is classified, then the action fails closed with stable reason `delegation-mismatch`, preserving requester and `principal_for` as metadata-only audit references. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Conflict resolution rules`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR48c`]
5. Given a `shared-mailbox send` is attempted by a member whose membership lapsed between policy snapshot and command execution, when classification runs, then the action fails closed with stable reason `membership-revoked`, records the membership-at-send evidence reference for audit, and does not downgrade to authenticated-user send. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Conflict resolution rules`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR48`]
6. Given `approved service-send` is attempted, when no paired approval record is present in the audit chain for the proposed outbound action, then the action fails closed with stable reason `approval-missing`; no service-client path can send outbound only because it has Graph `Mail.Send` or an existing service-client grant. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Conflict resolution rules`; `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md#Dev Notes`]
7. Given acceptance coverage runs, then tests prove all five successful mappings, all four explicit conflict reasons, metadata-only redaction for denials, service-client grant interplay for `approved service-send`, shared-mailbox membership freshness behavior, and architecture guards that surface adapters depend only on `Hexalith.ChatBot.Client` and cannot replicate classifier/gateway stages. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`; `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs`]

## Tasks / Subtasks

- [x] Define stable sender-authority contracts in `Hexalith.ChatBot.Contracts` (AC: 1, 3, 4, 5, 6)
  - [x] Add a finite enum or equivalent token mapper for the five authority classes with exact wire tokens from the addendum; keep serialization tolerant and add contract tests.
  - [x] Add finite conflict/reason tokens for `delegation-mismatch`, `membership-revoked`, and `approval-missing`; reuse existing `policy-blocked` from `ChatBotDisabledActionReasons`.
  - [x] Add a metadata-only classification result contract carrying authority class, requester, mailbox/shared mailbox/service client reference, `principal_for`, approval id, policy snapshot id, evidence freshness, audit field references, and denial reason where applicable.
  - [x] Do not include raw provider payloads, access tokens, raw claims, raw mailbox headers, message bodies, recipient display names, or project names in public contracts.
- [x] Implement the server-owned outbound authority classifier under `src/Hexalith.ChatBot.Server/Governance/Outbound/` (AC: 1, 2, 3, 4, 5, 6)
  - [x] Create small internal request/evidence models for M365 posture, tenant outbound policy, requester/project authority, shared-mailbox membership snapshot, delegation evidence, service-client grant evidence, and approval-chain evidence.
  - [x] Map success cases exactly:
    - `draft-only`: no M365 send posture; requester has project authority plus outbound-draft scope.
    - `authenticated-user send`: requester is mailbox owner, has own-mailbox `Mail.Send`, holds outbound-send scope, and no delegation is involved.
    - `shared-mailbox send`: requester is on the shared mailbox membership list at send time, has shared-mailbox send posture, holds outbound-send scope, and membership evidence is recorded.
    - `send-on-behalf`: requester is the delegate, `principal_for` is preserved, tenant policy allows it, and delegation has not been revoked since the policy snapshot.
    - `approved service-send`: service client has explicit outbound grant, the originating requester is preserved, and a paired approval record exists in the audit chain.
  - [x] Map conflict cases exactly to `policy-blocked`, `delegation-mismatch`, `membership-revoked`, and `approval-missing`; fail closed before durable outbound effects.
  - [x] Treat Graph/Exchange permissions as inputs, not as sufficient authority. Application `Mail.Send` breadth and delegated `Mail.Send.Shared` must still pass ChatBot policy, project, grant, and approval checks.
- [x] Wire denial/redaction/audit support without adding outbound send behavior (AC: 2, 3, 4, 5, 6)
  - [x] Extend message-catalog/refusal/disabled-reason tests only as needed to support stable metadata-only denial output for new authority reasons.
  - [x] Ensure audit evidence references use opaque identifiers and safe source refs such as `sender-authority:<class>`, `principal-for:<id>`, `approval:<id>`, `service-client:<id>`, and `policy-snapshot:<id>`.
  - [x] If classifier output needs to flow through gateway approval/risk code, add a narrow internal seam; do not duplicate `CommandGateway`, `IApprovalGate`, `IAuditWriter`, or service-client grant validation.
- [x] Add focused test coverage (AC: all)
  - [x] Add contract tests for authority class wire tokens, reason tokens, metadata-only serialization, and absence of secret-bearing/public payload properties.
  - [x] Add server unit tests for all five success mappings and all four conflict rules.
  - [x] Add service-client interplay tests proving `approved service-send` requires both an outbound-allowed service-client grant and paired approval-chain evidence.
  - [x] Add architecture tests if a new public/internal boundary is introduced; surface adapters must not reference `Server/Governance/Outbound`, gateway stages, Dapr, stores, or provider adapters.
  - [x] Add redaction/leakage sentinel assertions for denied authority results.
- [x] Preserve scope boundaries (AC: all)
  - [x] Do not call Microsoft Graph or Exchange Online from this story; use deterministic evidence models/fakes.
  - [x] Do not create outbound drafts, send messages, store draft content, build S6 outbound approval UI, or expose new CLI/MCP outbound commands.
  - [x] Do not implement inbound DMARC/DKIM/SPF/header inspection, on-behalf-of inbound ingestion changes, external-sender posture, tenant admin policy editor UI, or replay outbound adapters.
  - [x] Do not upgrade .NET, Aspire, Dapr, Fluent UI, System.CommandLine, ModelContextProtocol, xUnit, Shouldly, NSubstitute, or NetArchTest.

## Dev Notes

### Scope Boundaries

- Story 6.1 is the authority taxonomy/classifier foundation for later outbound draft/send stories. It should produce stable contracts, server-side classification behavior, fail-closed reason handling, and tests.
- This story must not send email, create drafts, or integrate live Microsoft Graph/Exchange calls. Provider facts are represented as deterministic evidence inputs so later adapters can supply them without changing classifier semantics.
- Do not let surface input select authority class. UI/CLI/MCP may request an outbound intent later, but authority is resolved from authenticated identity, tenant policy, project authority, provider posture, service-client grant, shared-mailbox/delegation evidence, and approval-chain evidence.
- Prefer a new internal `Governance/Outbound` area in `.Server` for classifier logic. Keep contract DTOs/enums under `.Contracts` only when they are needed across boundaries or tests.
- `send-on-behalf` and `shared-mailbox send` are distinct. Do not collapse shared mailbox membership into delegation and do not downgrade lapsed shared-mailbox membership to authenticated-user send.
- `approved service-send` is stricter than service-client grant validation. The grant proves the service client may participate; the paired approval record proves this specific outbound action may leave the boundary.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Contracts/Commands/IChatBotCommand.cs` - marker for typed state-mutating commands.
- `src/Hexalith.ChatBot.Contracts/Enums/ServiceClientClass.cs` and `src/Hexalith.ChatBot.Contracts/Identities/ServiceClientGrant.cs` - service-client grant class/evidence foundation for approved service-send.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs` - already contains `policy-blocked`; extend carefully if additional disabled-action reasons become user-visible.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotRefusalReasonCodes.cs` - already contains `sender-authority-denied`; prefer mapping classifier denials through this category rather than creating unsafe free-form errors.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs` - current fail-closed grant validation and metadata-only evidence pattern.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs` and `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionApprovalEvents.cs` - approval authority and approval-record patterns; do not reuse AI-specific types for outbound records if a separate outbound approval concept is needed later.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` - add future outbound commands here only when a command exists and is meant to enter the gateway; this story may not need a new command.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - audit source evidence refs must remain metadata-only.
- `tests/Hexalith.ChatBot.Contracts.Tests/ServiceClientGrantContractTests.cs` and `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs` - patterns for finite token, safe serialization, and message catalog tests.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs` - pattern for fail-closed authorization matrix tests.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` and `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/*` - adapter boundary and server-only governance seam tests.

### Current State To Preserve

- `CommandGateway` remains the only external write admission spine. Do not create a second outbound admission pipeline.
- Surface adapters currently depend on `Hexalith.ChatBot.Client`; architecture tests forbid UI/CLI/MCP direct references to server/gateway internals. Keep this intact.
- Service-client grants are claim-backed and fail closed for missing, ambiguous, expired, revoked, over-scoped, under-scoped, tenant-mismatch, and wrong-surface cases. Do not weaken this for service-send.
- Existing message catalog entries are metadata-only and tested for safe next actions, stable codes, and restricted-text leakage. New authority reason text must meet the same rules.
- Existing mailbox intake code is inbound metadata capture only. Do not add live outbound provider calls to workers or mailbox adapters in this story.
- The repo is `net10.0`, nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, NSubstitute, and NetArchTest. Keep package versions pinned.
- Root-level submodule policy applies: initialize/update only root `.gitmodules` submodules and never run recursive submodule commands.

### Architecture Guardrails

- Server-owned classifier logic belongs in `.Server`, likely `Governance/Outbound/`; contracts belong in `.Contracts`; generated client/OpenAPI changes are needed only if a new adapter-facing command/query contract is introduced.
- If OpenAPI changes are unavoidable, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- Use EventStore-style deterministic domain/service logic: no I/O inside classifier decisions, no Dapr/Graph calls in tests, expected denials as result values, infrastructure failures separate from business denials.
- Tenant IDs come from authenticated context/gateway binding, not request bodies.
- Diagnostics and denials are metadata-only. Never log or surface raw claims, raw provider payloads, mailbox contents, raw headers, tokens, secret material, restricted project names, or unauthorized mailbox names.
- Preserve parity-by-construction. CLI/MCP/UI can consume future contract results through the client, but must not copy classifier logic or gateway stages.

### Latest Technical Information

- Microsoft Graph has separate delegated and application permission models. Application permissions can be broad; ChatBot must still constrain authority by tenant policy, service-client grant, approval-chain evidence, and project authority. [Source: Microsoft Learn, Graph permissions overview, https://learn.microsoft.com/en-us/graph/permissions-overview]
- Microsoft Graph `Mail.Send` allows send as users and can be application or delegated; application access policies may limit mailbox access, but that does not replace ChatBot authorization. [Source: Microsoft Learn, Graph permissions reference, https://learn.microsoft.com/en-us/graph/permissions-reference?view=graph-rest-1.0#mail-send]
- Microsoft Graph `Mail.Send.Shared` is delegated, work/school only, and allows sending on behalf of others; ChatBot still must enforce the FR48 `send-on-behalf` conflict rules. [Source: Microsoft Learn, Graph permissions reference, https://learn.microsoft.com/en-us/graph/permissions-reference?view=graph-rest-1.0#mail-sendshared]
- Exchange Online distinguishes Full Access, Send As, and Send on behalf. Full Access does not allow send by itself; if both Send As and Send on behalf are present, Exchange uses Send As. ChatBot must classify the effective authority explicitly and fail closed if product policy does not allow it. [Source: Microsoft Learn, Manage permissions for recipients in Exchange Online, https://learn.microsoft.com/en-us/exchange/recipients-in-exchange-online/manage-permissions-for-recipients]
- Microsoft Graph supports sending from another user/shared mailbox, but final behavior depends on Exchange permissions and the API used. Treat provider posture as evidence, not as ChatBot approval. [Source: Microsoft Learn, Send Outlook messages from another user, https://learn.microsoft.com/en-us/graph/outlook-send-mail-from-other-user]

### Previous Story Intelligence

- Epic 5 completed service-client identities, CLI adapter parity, MCP adapter parity, and real UI/API+CLI+MCP differential conformance. Epic 6 must keep outbound authority decisions server-owned and parity-aware.
- Story 5.1 established `ServiceClientGrant`, `ServiceClientGrantEvidence`, grant validation, dedicated service-account classes, and metadata-only audit evidence. Reuse this foundation for `approved service-send`; do not add an outbound bypass for service clients.
- Story 5.4 proved adapter parity by driving production adapter code paths through `IChatBotClient`. Do not expose outbound operations through CLI/MCP until shared contracts and server-owned classification are stable.
- Epic 5 retrospective carry-forward: outbound commands cannot infer sender authority from CLI/MCP/UI arguments alone; metadata-only denial output must cover `policy-blocked`, `delegation-mismatch`, `membership-revoked`, `approval-missing`, stale credential, wrong surface, tenant mismatch, and unknown resource cases.
- Recent commits:
  - `5667c4b docs(epic-5): add retrospective`
  - `4d3ad3d test(story-5.4): Cross-surface equivalence verification`
  - `e40d6fc feat(story-5.3): MCP adapter and governed tool surface`
  - `73847b5 feat(story-5.2): CLI adapter and workflow parity`
  - `9fd74ec feat(story-5.1): Service-client identities and scoped grants`

### Project Structure Notes

- Likely new files:
  - `src/Hexalith.ChatBot.Contracts/Enums/SenderAuthorityClass.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/SenderAuthorityClasses.cs`
  - `src/Hexalith.ChatBot.Contracts/Messages/SenderAuthorityConflictReasons.cs` or a similarly finite contract type
  - `src/Hexalith.ChatBot.Server/Governance/Outbound/*`
  - `tests/Hexalith.ChatBot.Contracts.Tests/SenderAuthorityContractTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/*`
- Likely update files:
  - `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs` if the new conflict reasons become disabled-action reasons.
  - `src/Hexalith.ChatBot.Contracts/Messages/ChatBotRefusalReasonCodes.cs` only if the existing `sender-authority-denied` category is insufficient.
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` only if this story introduces a real gateway command; prefer no new command unless needed for acceptance coverage.
  - `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs` for finite reason/catalog coverage.
  - `tests/Hexalith.ChatBot.Architecture.Tests/*` if new boundaries need enforcement.
- Keep CLI-specific tests in `tests/Hexalith.ChatBot.Cli.Tests`, MCP-specific tests in `tests/Hexalith.ChatBot.Mcp.Tests`, and cross-surface parity tests in `tests/Hexalith.ChatBot.Conformance.Tests`. This story is primarily contracts/server tests unless a public contract change requires client generation.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
- Add `Client.Tests` only if OpenAPI/generated client changes are made:
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`
- Add `Conformance.Tests`, `Cli.Tests`, and `Mcp.Tests` only if the story exposes new adapter-facing outbound operations:
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Cli.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Cli.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Mcp.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Mcp.Tests -parallel none`
- Sandbox note inherited from previous stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- No Playwright is required. This story has no visible UI surface.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 6 defines outbound communication and inbound authenticity, and Story 6.1 defines FR48 sender-authority classes plus conflict rules.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR47-FR50, FR48a-FR48d, FR81a, NFR2, NFR13-NFR15, NFR48, NFR50, and outbound failure rows.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially Inbound Message Authenticity, Authority Class Mapping, Conflict Resolution Rules, Tenant Policy Schema, Shared Command Pipeline, and Idempotency Keys.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway, parity by construction, project structure, adapter boundaries, FR47-FR50 home under `Server/Governance/Outbound/` and `Adapters/Mailbox/`, metadata-only diagnostics, and test structure.
- Loaded UX design context from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`; relevant carry-forward is sender authority visibility in approval surfaces, safe denial language, and semantic consistency. No new visual surface is added.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant carry-forward: .NET SDK `10.0.300`, `net10.0`, central package management, warnings-as-errors, metadata-only diagnostics, tenant isolation, Keycloak/OIDC, pure EventStore aggregates, Dapr duplicate/order tolerance, xUnit v3/Shouldly/NSubstitute, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md`, `_bmad-output/implementation-artifacts/5-2-cli-adapter-and-workflow-parity.md`, `_bmad-output/implementation-artifacts/5-3-mcp-adapter-and-governed-tool-surface.md`, `_bmad-output/implementation-artifacts/5-4-cross-surface-equivalence-verification.md`, and `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-02.md`.
- Inspected current likely reuse/update files: service-client grant contracts and validator, message/refusal reason catalogs, AI approval gate/events, gateway allowlist, adapter boundary tests, contracts tests, and server authorization tests.
- Web research checked current Microsoft Learn pages for Graph permission overview, Graph mail permissions, Exchange delegate permissions, and Graph send-from-other-user behavior. No package/version changes are required.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 6 and Story 6.1 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR47-FR50, FR48a-FR48d, FR81a, NFR2, NFR13-NFR15, NFR48, NFR50.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Authority class mapping, conflict rules, tenant policy schema, idempotency keys, inbound authenticity.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, adapter boundaries, project structure, FR mapping, metadata-only diagnostics, tests.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - sender authority in action review, safe denial, semantic surface consistency.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - semantic consistency and actor/surface attribution.
- `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md` - service-client grant and audit foundation.
- `_bmad-output/implementation-artifacts/5-4-cross-surface-equivalence-verification.md` - parity and adapter boundary foundation.
- `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-02.md` - Epic 6 carry-forward risks and action items.
- `Directory.Build.props` - target framework, nullable, warnings-as-errors.
- `Directory.Packages.props` - central package versions.
- `src/Hexalith.ChatBot.Contracts/Enums/ServiceClientClass.cs` - existing service-client class tokens.
- `src/Hexalith.ChatBot.Contracts/Identities/ServiceClientGrant.cs` - existing grant contract.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs` - existing disabled-action reasons including `policy-blocked`.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotRefusalReasonCodes.cs` - existing `sender-authority-denied` refusal category.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs` - fail-closed grant validator pattern.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs` - approval gate pattern.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionApprovalEvents.cs` - approval event metadata pattern.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - metadata-only audit evidence references.
- `tests/Hexalith.ChatBot.Contracts.Tests/ServiceClientGrantContractTests.cs` - finite token/secret-safe serialization pattern.
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs` - message/refusal/disabled reason safety tests.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs` - fail-closed authorization matrix pattern.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` - adapter boundary and internal stage rules.
- Microsoft Learn Graph permissions overview - `https://learn.microsoft.com/en-us/graph/permissions-overview`.
- Microsoft Learn Graph permissions reference - `https://learn.microsoft.com/en-us/graph/permissions-reference?view=graph-rest-1.0`.
- Microsoft Learn Exchange recipient permissions - `https://learn.microsoft.com/en-us/exchange/recipients-in-exchange-online/manage-permissions-for-recipients`.
- Microsoft Learn Graph send from another user - `https://learn.microsoft.com/en-us/graph/outlook-send-mail-from-other-user`.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore --filter SenderAuthorityContractTests -m:1 /nr:false` hit the known VSTest `SocketException (13): Permission denied` sandbox limitation after build.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none -class Hexalith.ChatBot.Contracts.Tests.SenderAuthorityContractTests` passed: 13 tests, 0 failed.
- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` red phase failed before outbound classifier namespace and models existed; green phase passed after implementation.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -class Hexalith.ChatBot.Server.Tests.Governance.Outbound.SenderAuthorityClassifierTests` passed: 11 tests, 0 failed.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none -class Hexalith.ChatBot.Architecture.Tests.AdapterBoundaryFitnessTests -class Hexalith.ChatBot.Architecture.Tests.ScaffoldArchitectureTests` passed: 26 tests, 0 failed.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` passed: 121 tests, 0 failed.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` passed: 467 tests, 0 failed.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` passed: 37 tests, 0 failed.
- Review auto-fix validation: `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -class Hexalith.ChatBot.Server.Tests.Governance.Outbound.SenderAuthorityClassifierTests` passed: 11 tests, 0 failed.
- Review auto-fix validation: `./tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -parallel none -class Hexalith.ChatBot.IntegrationTests.Governance.Outbound.SenderAuthorityClassificationWorkflowE2ETests` passed: 13 tests, 0 failed.
- Review auto-fix validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed.
- Review auto-fix validation: `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` passed: 121 tests, 0 failed.
- Review auto-fix validation: `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` passed: 467 tests, 0 failed.
- Review auto-fix validation: `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` passed: 37 tests, 0 failed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added stable sender-authority class tokens, conflict reason tokens, and metadata-only classification result contracts with focused contract tests.
- Added an internal server-owned outbound authority classifier with deterministic evidence models for M365 posture, tenant policy, project authority, shared mailbox membership, delegation, service-client grant, and approval-chain evidence.
- Covered all five successful authority mappings, `policy-blocked`, `delegation-mismatch`, `membership-revoked`, and `approval-missing` fail-closed cases, provider-posture-only denial, service-client approval interplay, and metadata-only denial refs.
- Extended adapter boundary architecture guards so surface adapters cannot depend on `Hexalith.ChatBot.Server.Governance.Outbound` or replicate classifier/gateway stages.

### File List

- `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Contracts/Enums/SenderAuthorityClass.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/SenderAuthorityClasses.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/SenderAuthorityClassificationResult.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/SenderAuthorityConflictReasons.cs`
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassificationRequest.cs`
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassifier.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SenderAuthorityContractTests.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/Governance/Outbound/SenderAuthorityClassificationWorkflowE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/SenderAuthorityClassifierTests.cs`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-02

### Findings

- HIGH: `approved service-send` treated a missing outbound service-client grant as `approval-missing`, even when a paired approval existed. That masked the service-client authorization failure and weakened the required grant-plus-approval interplay for AC6. Fixed in `SenderAuthorityClassifier` and covered by server/integration tests.
- MEDIUM: `tests/Hexalith.ChatBot.IntegrationTests/Governance/Outbound/SenderAuthorityClassificationWorkflowE2ETests.cs` was present in git changes but absent from the story File List. Added it to the File List.

### Outcome

Approved after auto-fixes. Remaining risk is limited to future adapter integration, since this story intentionally uses deterministic evidence models and does not expose a public outbound command.

### Change Log

- 2026-06-02: Implemented story 6.1 sender-authority contracts, server-owned classifier, metadata-only denial evidence, and adapter boundary tests.
- 2026-06-02: Senior developer review auto-fixed service-client grant denial precedence and updated the story File List.
