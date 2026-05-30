---
baseline_commit: d0780a2
---

# Story 1.3: CommandGateway Admission Spine With Tenant Binding and Authorization

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-30. -->

## Story

As a security engineer,
I want every state-mutating command to pass through a CommandGateway that authenticates, tenant-binds, and authorizes before any aggregate load,
so that no surface can reach domain state without enforced tenant isolation and authorization.

## Acceptance Criteria

1. Given the FR81a invariant, when the gateway is built, then it runs stages in this order before any EventStore dispatch or aggregate load: `auth -> tenant-bind -> authorize`; risk-classify and approval-gate seams exist but are stubbed in this story. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3; Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns]
2. Given a command from any surface, when `tenantId` is resolved, then it is bound from authenticated Keycloak/EventStore claims only, never from request body, CLI/MCP arguments, or `IChatBotCommand` payload data. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a]
3. Given a command carrying or targeting a tenant-scoped identifier, when the target tenant differs from the bound authenticated tenant, then the command is rejected before EventStore dispatch even if the principal holds valid credentials in another tenant. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR16]
4. Given an unauthorized request, when authorization fails, then the gateway returns metadata-only redacted denial Problem Details that do not confirm whether the target resource exists, and records a metadata-only authorization-failure audit fact through the internal audit seam. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2]
5. Given the adapter boundary, when current or future adapters are compiled, then they construct only `IChatBotCommand` and cannot replicate gateway stages because `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, and `IIdempotencyStore` are internal to `Hexalith.ChatBot.Server`. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3; Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
6. Given negative authorization tests, when a cross-tenant command is submitted, then the result is fail-closed: zero domain dispatch, zero durable state mutation, and exactly one authorization-failure audit fact with tenant, actor, command type, reason code, and correlation metadata only. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR1]

## Tasks / Subtasks

- [x] Build the gateway admission spine in `Server/Gateway/` (AC: 1, 5)
  - [x] Add a `CommandGateway` or equivalent orchestrator in `src/Hexalith.ChatBot.Server/Gateway/` with explicit ordered stage execution.
  - [x] Add `Gateway/Stages/` interfaces/classes for `auth`, `tenant-bind`, `authorize`, `risk-classify`, and `approval-gate`; risk and approval must be deterministic pass-through stubs for this story.
  - [x] Add internal seam placeholders for `coarse-idempotency`, `pre-commit-audit`, and `post-commit-audit` so the gateway shape matches the architecture sequence; keep their durable behavior out of scope until Stories 1.4 and 1.5.
  - [x] Keep `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, and `IIdempotencyStore` internal to `Hexalith.ChatBot.Server`; do not add these interfaces to Contracts, Client, generated code, CLI/MCP/UI, or test fixtures intended as public surface.
  - [x] Add a narrow dispatch seam for the later EventStore write path; tests must prove it is not invoked before auth, tenant bind, and authorization pass.
- [x] Implement authenticated tenant binding (AC: 2, 3)
  - [x] Resolve the authoritative tenant from the authenticated principal/claims context only. Use `sub` for actor identity and `eventstore:tenant`-style tenant evidence where available.
  - [x] Reject missing, unauthenticated, ambiguous, malformed, or unavailable tenant context as fail-closed before command dispatch.
  - [x] Treat any tenant/project/resource identifiers present in the submitted command as comparison inputs only, never as authority.
  - [x] Reject cross-tenant target identifiers before EventStore dispatch and without querying or loading protected state.
- [x] Implement authorization denial and safe response mapping (AC: 4, 6)
  - [x] Add a ChatBot authorization result model with stable reason codes such as `authentication_denied`, `tenant_missing`, `tenant_mismatch`, `authorization_denied`, and `safe_not_found`.
  - [x] Map denials to metadata-only Problem Details aligned with Story 1.2 fields: `category`, `code`, `message`, `correlationId`, `taskId?`, `retryable`, `clientAction`, and `details.visibility`.
  - [x] Do not include tenant IDs, project names, candidate evidence, file metadata, audit detail, raw exception text, local paths, payload bytes, or command bodies in user-facing responses, test failure text, logs, or generated artifacts.
  - [x] Emit the story-local authorization-failure audit fact through the internal audit seam. This is not the durable WORM/two-phase audit implementation; Story 1.4 owns that.
- [x] Wire the REST command submission boundary to the gateway only if needed for the executable slice (AC: 1, 2, 4)
  - [x] If implementing `/api/v1/commands`, update `src/Hexalith.ChatBot.Server/Program.cs` or a focused registration extension so the endpoint calls the gateway and not EventStore directly.
  - [x] Preserve Story 1.2 `IChatBotClient.SubmitAsync(IChatBotCommand, ...)` as the adapter-facing API; do not make adapters pass tenant IDs or gateway-stage inputs.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and regenerate the client only if response categories or operation behavior change. No contract regeneration was required.
- [x] Add mechanical architecture guardrails (AC: 5)
  - [x] Extend `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` or add a focused architecture test proving gateway stage interfaces remain internal to Server.
  - [x] Add a guard that future `.UI`, `.Cli`, `.Mcp`, and `.Workers` projects cannot reference `Hexalith.ChatBot.Server` or the internal stage interface names.
  - [x] Add source scanning or reflection tests that fail if `tenantId` is added to client-controlled command request body schemas or the `IChatBotClient.SubmitAsync` signature.
- [x] Add focused server and conformance tests (AC: 1, 2, 3, 4, 6)
  - [x] Test stage order with a recording/fake stage set: auth before tenant bind, tenant bind before authorization, authorization before dispatch.
  - [x] Test missing authentication and missing tenant context return safe Problem Details and never call dispatch.
  - [x] Test cross-tenant target mismatch returns safe denial, emits one authorization-failure audit fact, and never calls dispatch.
  - [x] Test unauthorized and nonexistent/restricted resources are indistinguishable at the caller-visible boundary where the story exercises resource denial.
  - [x] Add/update `tests/fixtures/` conformance data for the Story 1.3 gateway negative case if the existing Story 1.2 oracle is extended. No fixture update was required because the conformance oracle was not extended.
- [x] Verify locally (AC: all)
  - [x] Run `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false`.
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1`.
  - [x] Run focused xUnit v3 in-process test executables for Architecture, Server, Contracts/Client only if touched, and Conformance only if fixtures changed.
  - [x] If default `dotnet test` is blocked by the known sandbox VSTest TCP listener issue, record the blocker and run the in-process xUnit v3 runners as Story 1.2 did.

## Dev Notes

### Implementation Intent

Story 1.3 creates the CommandGateway admission spine and proves the first safety boundary: no state-mutating command reaches domain dispatch unless authentication, tenant binding, and authorization have already passed. It should not implement business command handling, Association, mailbox intake, UI/CLI/MCP adapters, durable WORM audit, two-altitude idempotency behavior, lifecycle transition enforcement, or the first governed command. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3; Source: _bmad-output/planning-artifacts/architecture.md#Decision Impact Analysis]

The correct shape is an admission component in `Hexalith.ChatBot.Server` that sits in front of EventStore's existing write path. Do not create a second aggregate/event pipeline. The gateway may create or prepare EventStore `CommandEnvelope`/submit data only after the gateway stages succeed. [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns; Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs]

### Current Files To Update

- `src/Hexalith.ChatBot.Server/Program.cs`: currently only maps default health endpoints and `/health/chatbot`. If this story wires `/api/v1/commands`, keep endpoint code thin and delegate to gateway/registration classes. [Source: src/Hexalith.ChatBot.Server/Program.cs]
- `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj`: currently references Client, ServiceDefaults, EventStore.Contracts, and Tenants.Contracts. Keep package versions centralized and do not add inline package versions. [Source: src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj]
- `src/Hexalith.ChatBot.Server/ChatBotPlatformReferences.cs`: confirms EventStore and Tenants contract references are already available; extend only if tests need a stable platform reference. [Source: src/Hexalith.ChatBot.Server/ChatBotPlatformReferences.cs]
- `src/Hexalith.ChatBot.Client/IChatBotClient.cs` and `src/Hexalith.ChatBot.Client/ChatBotClient.cs`: Story 1.2 already exposes `SubmitAsync(IChatBotCommand, correlationId?, taskId?)` and generates ULID command/correlation/task IDs. Preserve this as the adapter boundary; do not add tenant parameters. [Source: src/Hexalith.ChatBot.Client/IChatBotClient.cs; Source: src/Hexalith.ChatBot.Client/ChatBotClient.cs]
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`: currently defines OpenAPI 3.1 `POST /api/v1/commands` and the Story 1.2 metadata-only Problem Details shape. Update only when server behavior requires contract-visible response categories. [Source: src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml]
- `tests/Hexalith.ChatBot.Server.Tests/`: currently contains bootstrap/health and platform-reference tests. Add gateway tests here; keep them offline and deterministic. [Source: tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs; Source: tests/Hexalith.ChatBot.Server.Tests/PlatformReferenceTests.cs]
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`: already enforces dependency direction and future adapter boundaries; extend it for gateway stage internality. [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- `tests/Hexalith.ChatBot.Conformance.Tests/ContractSpineOracleTests.cs` and `tests/fixtures/story-1-2-contract-spine-oracle.json`: update or add a Story 1.3 fixture only if the conformance oracle grows beyond Story 1.2's command submission contract. [Source: tests/Hexalith.ChatBot.Conformance.Tests/ContractSpineOracleTests.cs]

### Architecture Guardrails

- Stage order is load-bearing: `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> pre-commit-audit -> [EventStore] -> post-commit-audit`. This story proves only the first three behavioral stages and adds the future seams; Story 1.4 owns fail-closed audit behavior, Story 1.5 owns idempotency behavior, and Story 1.9 owns the first real governed command. [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns]
- Tenant authority must come from authenticated context. Command body, request body, headers supplied by callers, CLI arguments, and MCP tool arguments are comparison inputs only. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a; Source: Hexalith.Folders/_bmad-output/project-context.md#Critical Don't-Miss Rules]
- Authorization must happen before aggregate load, actor state access, projection detail read, domain dispatch, or protected-resource existence check. Safe denials must not distinguish unauthorized from nonexistent restricted resources unless the caller is authorized to know. [Source: Hexalith.EventStore/_bmad-output/project-context.md#Critical Don't-Miss Rules; Source: Hexalith.Folders/src/Hexalith.Folders.Server/FolderAuthorizationDenialMapper.cs]
- The EventStore `AuthorizationBehavior` has a prevalidated gateway authorization context hook. If this story dispatches into EventStore during an HTTP request, use the existing platform pattern deliberately so EventStore validation is consistent and not bypassed. [Source: Hexalith.EventStore/src/Hexalith.EventStore/Pipeline/AuthorizationBehavior.cs]
- Governance interfaces stay `internal` to `.Server`; stage replication should be mechanically impossible for adapters. Tests should fail on forbidden references, not rely on reviewer memory. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]

### Previous Story Intelligence

- Story 1.2 is complete at baseline commit `d0780a2`. It added the OpenAPI 3.1 Contract Spine, generated NSwag client, `IChatBotCommand`, shared enums, ULID identity helpers, metadata-only Problem Details, and conformance fixtures. Build passed with serial solution build; focused xUnit v3 in-process runners passed. [Source: _bmad-output/implementation-artifacts/1-2-establish-the-openapi-contract-spine-typed-client-and-ichatbotcommand.md#Dev Agent Record]
- Story 1.2's senior review fixed invalid ULID header acceptance, invalid `Command`-suffixed command type names, generated optional DTO nullability, and stale generated-client freshness coverage. Preserve those guardrails. [Source: _bmad-output/implementation-artifacts/1-2-establish-the-openapi-contract-spine-typed-client-and-ichatbotcommand.md#Senior Developer Review (AI)]
- Known local validation issue: default VSTest execution can fail in the sandbox with a TCP listener permission error. Use focused xUnit v3 in-process runners when that repeats, and record the exact command/blocker. [Source: _bmad-output/implementation-artifacts/1-2-establish-the-openapi-contract-spine-typed-client-and-ichatbotcommand.md#Debug Log References]
- Current dirty worktree observed during story creation: `_bmad-output/story-automator/orchestration-1-20260530-160445.md` is unrelated. Do not revert or overwrite it. [Source: git status --short]

### Testing Requirements

- Use xUnit v3 and Shouldly. Keep tests deterministic and offline; no live Keycloak, Dapr sidecars, Aspire runtime, Redis, production secrets, network calls, provider credentials, or nested submodule initialization. [Source: Hexalith.Folders/_bmad-output/project-context.md#Testing Rules]
- Gateway tests should use fakes/recorders for stages, authorization decisions, audit sink, and dispatch. The important assertion is ordering and non-dispatch on failure.
- Negative authorization tests must prove zero protected-state touch. A fake dispatcher should count calls; if a fake aggregate loader exists, it should remain at zero on authentication, tenant binding, and authorization failures.
- Redaction tests should scan serialized Problem Details and audit facts for sentinel strings such as tenant IDs from the wrong tenant, project names, file names, payload content, raw exception text, local paths, and command body JSON.
- Architecture tests should remain pure repository/source tests and avoid relying on `.NET` internals that make `internal` visibility hard to inspect after `InternalsVisibleTo`.

### Out of Scope

- Do not implement the durable fail-closed audit-commit seam, WORM audit chain, audit replay queue, or post-commit audit reconciliation. Story 1.4 owns that.
- Do not implement coarse request-dedup or fine EventStore idempotency behavior beyond internal seams needed for compile-time gateway shape. Story 1.5 owns idempotency behavior.
- Do not implement lifecycle state machine, state transition enforcement, message catalog/redaction stage, correlation/status query, Association, mailbox intake, task intent, AI mediation, UI, CLI, MCP, workers, or the first real allowlisted command.
- Do not modify sibling submodules, generated client files by hand, nested submodule metadata, `.gitmodules`, or recursive submodule workflow commands.
- Do not add tenant authority to the OpenAPI request body, client facade signature, command payload contract, generated client facade, CLI/MCP arguments, or test helper defaults.

### Project Structure Notes

- Alignment: place gateway implementation under `src/Hexalith.ChatBot.Server/Gateway/` and stage seams under `src/Hexalith.ChatBot.Server/Gateway/Stages/`, matching the architecture tree. Server tests belong under `tests/Hexalith.ChatBot.Server.Tests/`; boundary tests belong under `tests/Hexalith.ChatBot.Architecture.Tests/`.
- Detected variance: architecture sequence says the CommandGateway story has all nine seams, while Epic Story 1.3 focuses behavior on auth, tenant bind, and authorization. Resolve by adding future seams as internal pass-through/stub boundaries but leaving durable audit and idempotency behavior to Stories 1.4 and 1.5.
- Detected conflict risk: the Story 1.3 AC requires an authorization-failure audit record, while Story 1.4 owns the fail-closed audit seam. For Story 1.3, produce a metadata-only audit fact through an internal testable seam; do not claim WORM/durable audit completeness.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md#Discover Inputs Protocol]
- [Source: .agents/skills/bmad-create-story/template.md#Story Template]
- [Source: .agents/skills/bmad-create-story/checklist.md#Story Context Quality Competition Prompt]
- [Source: AGENTS.md#Git Submodules]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/implementation-artifacts/1-2-establish-the-openapi-contract-spine-typed-client-and-ichatbotcommand.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#API & Communication Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR16]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR57]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR1]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Shared Command Pipeline]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md#Critical Don't-Miss Rules]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore/Pipeline/AuthorizationBehavior.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore/Authorization/ClaimsTenantValidator.cs]
- [Source: Hexalith.Folders/_bmad-output/project-context.md#Critical Don't-Miss Rules]
- [Source: Hexalith.Folders/src/Hexalith.Folders.Server/FolderAuthorizationDenialMapper.cs]
- [Source: Hexalith.Folders/tests/Hexalith.Folders.Server.Tests/FoldersDomainServiceRequestHandlerTests.cs]
- [Source: src/Hexalith.ChatBot.Client/IChatBotClient.cs]
- [Source: src/Hexalith.ChatBot.Client/ChatBotClient.cs]
- [Source: src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml]
- [Source: src/Hexalith.ChatBot.Server/Program.cs]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- [Source: git log --oneline -5]
- [Source: git status --short]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Activation customization resolved with no prepend or append steps and persistent fact glob `file:{project-root}/**/project-context.md`.
- Input discovery loaded sprint status, epics, architecture, PRD/addendum, UX file inventory, Story 1.2, project-context facts from EventStore, Tenants, Folders, Conversations, Commons, Parties, Projects, Memories, and FrontComposer, current ChatBot source/tests, EventStore authorization references, Folders safe-denial references, and recent git status/log.
- Web research was not required for this story because the project pins the relevant stack locally and the story explicitly instructs developers not to upgrade version-sensitive dependencies; local architecture already records the chosen versions and compatibility posture.
- Checklist validation applied during story creation: clarified Story 1.3 vs Story 1.4/1.5 boundaries, added current-file update notes, added anti-reinvention guidance from EventStore/Folders, added safe-denial and cross-tenant tests, and added internal-stage architecture guardrails.

### Completion Notes List

- Implemented the `CommandGateway` admission spine with ordered auth, tenant-bind, authorize, risk-classify, approval-gate, coarse-idempotency, pre-commit-audit, dispatch, and post-commit-audit seams.
- Added authenticated claims-based tenant binding using `eventstore:tenant`/tenant claim evidence and fail-closed handling for missing, ambiguous, malformed, and cross-tenant contexts.
- Added safe metadata-only authorization denial mapping and a story-local authorization-failure audit fact through the internal audit seam.
- Wired `/api/v1/commands` through the gateway, preserving adapter-facing `IChatBotClient.SubmitAsync(IChatBotCommand, ...)` semantics.
- Added architecture and server tests covering internal gateway seams, tenant-authority exclusion at the adapter boundary, stage ordering, fail-closed non-dispatch, redacted denials, and audit-fact emission.
- Senior review auto-fixed tenant-scoped identifier mismatch detection and strict ULID metadata normalization at the HTTP/identity boundary.

### File List

- `_bmad-output/implementation-artifacts/1-3-commandgateway-admission-spine-with-tenant-binding-and-authorization.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Contracts/Identities/ChatBotIdentity.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationFailureAuditFact.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandSubmission.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotDispatchResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotGatewayContext.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotGatewayResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandSubmissionWireRequest.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotApprovalResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotAuthenticatedActor.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotAuthenticationResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotAuthorizationResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotRiskClassification.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotTenantBinding.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotTenantBindingResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsAuthenticationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsTenantBindingStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IApprovalGate.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IAuditWriter.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IAuthenticationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ICommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IIdempotencyStore.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IRiskClassifier.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ITenantBindingStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/InMemoryAuditWriter.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughApprovalGate.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughIdempotencyStore.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughRiskClassifier.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-30

### Review Findings

- HIGH fixed: `ClaimsTenantBindingStage` only compared explicit `tenantId` fields, so tenant-scoped EventStore-style identifiers such as `tenant-beta:projects:project-1` could reach authorization/dispatch under a `tenant-alpha` principal. Added tenant extraction from identifier properties and a fail-closed regression test proving zero dispatch and one authorization-failure audit fact.
- HIGH fixed: `/api/v1/commands` accepted and echoed non-canonical correlation/task header text in safe Problem Details. Hardened shared ULID normalization to require canonical 26-character Crockford ULID text and normalized server metadata before gateway submission.
- MEDIUM fixed: story status, task checkboxes, and File List were stale from story creation and did not document the actual implementation. Updated this story record and synced sprint status.

### Validation

- `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` passed.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1` passed with 0 warnings and 0 errors.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` passed: 19 total, 0 failed.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` passed: 10 total, 0 failed.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` passed: 19 total, 0 failed.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests` passed: 8 total, 0 failed.
- MCP resource search was performed during review; no MCP documentation resources were configured for this workspace. Local architecture/story references were used.

### Review Outcome

Approved after auto-fixes. No critical issues remain.
