---
baseline_commit: 18d246b
---

# Story 5.1: Service-client identities and scoped grants

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized administrator,
I want least-privilege service-client identities with scoped, expiring grants,
so that CLI/MCP/worker/mailbox/AI actors operate without inheriting human roles.

## Acceptance Criteria

1. Given the Service Client Permissions model, when an administrator configures service-client access for CLI, MCP, workers, mailbox events, and AI actors, then each client has a dedicated Keycloak service-account identity, a ChatBot-owned scoped grant, an authorized command/query set, a credential expiry, and an explicit surface/actor classification; service-client authorization never inherits UI roles or project-owner membership. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.1`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR19`; `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]
2. Given a service-client token reaches the gateway, when `ClaimsAuthenticationStage`, `ClaimsTenantBindingStage`, and authorization run, then the actor is identified from safe Keycloak/OIDC claims, tenant binding still comes only from authenticated tenant claims, command tenant targets must match the bound tenant, and missing/ambiguous actor, tenant, grant, scope, expiry, or command-set evidence fails closed before idempotency, dispatch, or durable state. [Source: `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsAuthenticationStage.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsTenantBindingStage.cs`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR5-NFR7`]
3. Given a delegated flow such as `cli-automation-client` acting for a user, when it executes an allowed command or query, then source user, service client, tenant, scope, grant ID, grant expiry, OAuth grant evidence, surface origin, correlation ID, policy snapshot, and bounded command/query evidence are recorded as metadata-only audit evidence; no secret, bearer token, raw provider payload, or unrestricted claim body is logged, returned, or stored in public projections. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.1`; `src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR4`]
4. Given an expired, revoked, over-scoped, under-scoped, unknown, tenant-mismatched, or wrong-surface credential is used, when a command or query is attempted, then the operation fails closed with a catalog-backed metadata-only response that does not reveal restricted project names, file metadata, candidate evidence, audit details, command payloads, tenant data, or grant secrets, and records authorization-failure audit evidence where audit readiness is available. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.1`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow 6 - Developer uses CLI parity`; `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`]
5. Given service-client grants are cached or projected locally, when a normal grant change or explicit revocation occurs, then ordinary policy staleness is bounded to 5 minutes and explicit revocation is effective within 60 seconds; tests prove stale grants cannot broaden access and revocation-sensitive invalidation affects only the targeted tenant/service client/surface. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR6`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR30-NFR31`]
6. Given service clients or AI actors attempt to mutate security-sensitive tenant policy or admin assignments, when the command reaches authorization, then it is denied even if the client has broad service posture or a tenant-admin-looking claim; tenant-policy threshold changes and admin assignment remain human tenant-admin operations only. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Tenant Policy Schema`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`; `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs`]
7. Given this story completes, when acceptance coverage runs, then tests prove grant creation/validation, dedicated service-account realm configuration, command/query allowlist enforcement, delegated-user audit evidence, revocation/expiry fail-closed behavior, tenant mismatch denial, metadata-only errors/audit, cross-tenant isolation for service/CLI/MCP/worker/mailbox/AI personas, and no adapter bypass of the shared command spine, without implementing the production CLI adapter, MCP server, outbound send, tenant policy editor UI, or FR74 disable/quarantine/rate-limit controls. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.1`; `_bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR65-NFR70`]

## Tasks / Subtasks

- [x] Model first-class service-client grants and evidence (AC: 1, 2, 3, 5, 7)
  - [x] Add contract/server metadata records for a service-client grant, for example `ServiceClientGrant`, `ServiceClientGrantEvidence`, and stable enums/tokens for client class (`cli-automation`, `mcp-tool`, `background-worker`, `mailbox-ingestion`, `audit-projection`, `ai-action-execution`), allowed command/query set, allowed surface origin, expiry, revocation status, and delegated user evidence.
  - [x] Keep command/query set entries as stable metadata names; do not serialize command payloads, raw OAuth token claims, bearer tokens, secrets, or provider payloads.
  - [x] Use safe stable identifiers and UTC `DateTimeOffset` fields only; follow existing `AuditMetadata` safe-token behavior and `ChatBotIdentity` ULID rules where identifiers are ChatBot-owned.
  - [x] If public inspection or client request/response shapes change, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- [x] Add Keycloak service-account identities for the required service-client classes (AC: 1, 2, 7)
  - [x] Extend `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json` with dedicated service-account clients instead of reusing `hexalith-chatbot` public/direct-access test client.
  - [x] Preserve the existing E2E user `actor-alpha` and public client behavior used by current tests.
  - [x] Emit only necessary tenant, actor-type, service-client ID, scope, grant, and expiry claims. Avoid `fullScopeAllowed` for service clients unless a test explicitly proves it cannot grant extra ChatBot authority.
  - [x] Add realm/fixture tests that assert every required service-client identity is enabled, service-account based, tenant-bound, and has no UI role inheritance.
- [x] Extend gateway authentication and authorization for service clients (AC: 1, 2, 4, 5, 6, 7)
  - [x] Reuse `ClaimsAuthenticationStage` for safe `sub` extraction, but extend actor classification so `chatbot:actor-type=service` or equivalent Keycloak service-account claims produce explicit service-client posture.
  - [x] Add a focused service-client grant resolver/validator behind an internal gateway-stage interface, keeping it inside `Hexalith.ChatBot.Server.Gateway.Stages`.
  - [x] Validate grant ID, service-client ID, tenant, surface origin, authorized command/query set, expiry, revocation state, and delegated-user requirements before idempotency and dispatch.
  - [x] Update `ParticipantAuthorizationStage` or a sibling authorization stage so service clients and AI actors cannot perform security-sensitive tenant-policy or admin-assignment operations, even with tenant-admin-looking claims.
  - [x] Add reason codes for expired, revoked, over-scoped, under-scoped, missing-grant, grant-tenant-mismatch, and wrong-surface failures; map them to existing safe problem/message catalog behavior rather than raw errors.
- [x] Preserve the shared command spine and adapter boundaries (AC: 2, 4, 7)
  - [x] Do not add CLI/MCP production adapters in this story. Only prepare service-client identity/grant enforcement and test shims needed for acceptance.
  - [x] Ensure future CLI/MCP/worker/mailbox/AI adapters still call `IChatBotClient.SubmitAsync(...)` or the existing gateway boundary and never call `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, `IIdempotencyStore`, Dapr, EventStore, or projection stores directly.
  - [x] Keep `ChatBotSurfaceOrigin` as provenance declared at the adapter boundary; do not trust origin alone as authorization.
  - [x] Extend `AdapterBoundaryFitnessTests`, `DependencyDirectionFitnessTests`, or `ScaffoldArchitectureTests` if new projects, namespaces, or grant services create a new bypass risk.
- [x] Enrich audit evidence for service-client and delegated flows (AC: 3, 4, 7)
  - [x] Extend `AuditEnvelope`/`AuditEnvelopeFactory` or add structured `SourceEvidenceRefs` entries for service-client ID, actor type, delegated source user, grant ID, grant scope, grant expiry, OAuth grant evidence fingerprint, command/query set version, and surface origin.
  - [x] Keep audit values metadata-only and safe-tokenized. Never record secrets, access tokens, refresh tokens, client secret hashes, raw JWTs, raw claims JSON, raw OAuth assertions, or upstream provider payloads.
  - [x] Ensure authorization-failure audit facts carry service-client surface and reason code without leaking tenant/project resource existence.
  - [x] Preserve existing pre-commit fail-closed behavior: audit unavailability before a state mutation must deny and release coarse idempotency admission.
- [x] Add grant staleness and revocation behavior (AC: 4, 5, 7)
  - [x] Implement cache/projection semantics so normal service-client grant changes honor the 5-minute maximum staleness and explicit revocations honor the 60-second maximum.
  - [x] Add deterministic tests using the existing `ISystemClock`/fixed-clock patterns instead of sleeping.
  - [x] Scope revocation effects to the targeted tenant/service client/surface; unrelated tenants, mailboxes, service clients, AI actors, and command surfaces must continue to use their own authorization state.
  - [x] Fail closed when the grant store, projection, cache, or policy snapshot is unavailable or ambiguous.
- [x] Add focused acceptance coverage (AC: all)
  - [x] Contract tests for service-client grant record serialization, enum wire values, safe identifier validation, OpenAPI/generated-client drift if public shapes change, and absence of secret/token/raw-claim fields.
  - [x] Gateway-stage tests for service-client authentication, tenant binding, command tenant mismatch, grant expiry, revocation, wrong surface, under-scoped command, over-scoped credential, delegated-user evidence, and missing/ambiguous grant.
  - [x] Authorization tests proving service clients and AI actors cannot mutate `SetAssociationConfidenceThresholds` or future security-sensitive admin operations; preserve the existing human tenant-admin positive path.
  - [x] Audit tests proving delegated flow evidence includes service client, source user, tenant, scope, expiry, OAuth evidence fingerprint, surface origin, correlation, policy snapshot, and command/query set without secrets.
  - [x] Conformance/isolation tests extending the nine-actor matrix for service client, CLI, MCP, background worker, M365 event, and AI actor negative authorization paths.
  - [x] AppHost/realm tests proving service-account clients exist with least privilege and do not inherit UI roles.

### Review Follow-ups (AI)

- [ ] [AI-Review][Low] `ServiceClientGrantProjectionCache` (AC5 5-min staleness / 60-sec revocation engine) has no production caller — live grant resolution is claims-direct (`ClaimsServiceClientGrantResolver`) and never consults it. The bounded-staleness/revocation semantics are proven only by `ServiceClientGrantProjectionCacheTests`, not on any admission path. Acceptable for this claims-sourced foundation story (JWT lifetime bounds staleness), but wire the cache into resolution when a local grant projection/store lands (Story 5.4 / Epic 5 follow-on). [src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantProjectionCache.cs:7]

## Dev Notes

### Scope Boundaries

- This story creates the identity and grant enforcement foundation for Epic 5. It must not implement the production CLI adapter (Story 5.2), MCP adapter/tool server (Story 5.3), full cross-surface differential harness wiring (Story 5.4), outbound sender-authority behavior (Epic 6), tenant policy editor UI (Epic 7), or FR74 disable/quarantine/rate-limit controls.
- Service-client grants are ChatBot authorization evidence layered on top of Keycloak service-account identity. Keycloak proves the actor identity and tenant claim; ChatBot still owns command/query authorization, surface binding, expiry/revocation interpretation, audit evidence, and fail-closed outcomes.
- `ChatBotSurfaceOrigin` is immutable provenance captured at the adapter boundary and already travels into audit. It is not a security control by itself; service-client grants must authorize the origin/command/query combination.
- Service clients and AI actors cannot mutate security-sensitive tenant policy or admin assignment. Do not loosen Story 1/2/4 authorization guardrails to make automation easier.

### Existing Code To Reuse

- Gateway and admission:
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandSubmission.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/CommandSubmissionWireRequest.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsAuthenticationStage.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsTenantBindingStage.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotAuthenticatedActor.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotAuthorizationResult.cs`
- Surface/client contracts:
  - `src/Hexalith.ChatBot.Contracts/Enums/ActorType.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigins.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/IChatBotCommand.cs`
  - `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
  - `src/Hexalith.ChatBot.Client/IChatBotClient.cs`
  - `src/Hexalith.ChatBot.Client/ChatBotClient.cs`
- Audit, redaction, and safe metadata:
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditMetadata.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs`
  - `src/Hexalith.ChatBot.Contracts/Messages/ChatBotRefusalReasonCodes.cs`
- Identity fixture:
  - `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json`
- Existing tests to extend:
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/ChatBotSurfaceOriginsTests.cs`
  - `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DependencyDirectionFitnessTests.cs`
  - `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantActorMatrixTests.cs`
  - `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationActorMatrix.cs`

### Current State To Preserve

- `hexalith-realm.json` currently has a single public `hexalith-chatbot` client with direct access grants for E2E and `serviceAccountsEnabled: false`; Story 5.1 should add dedicated service-account clients without breaking that test client or `actor-alpha`.
- `ClaimsAuthenticationStage` currently authenticates any principal with a safe `sub`. It does not yet classify service clients, check grant IDs, verify grant expiry, or bind authorized command/query sets.
- `ClaimsTenantBindingStage` already enforces exactly one safe `eventstore:tenant`/`tenant` claim and rejects command tenant targets outside that tenant. Preserve this behavior; do not accept tenant IDs from request bodies as authority.
- `ParticipantAuthorizationStage` already prevents `SetAssociationConfidenceThresholds` unless the actor is a human tenant admin. Preserve and generalize this guardrail for service-client/AI actor restrictions.
- `ChatBotSpineCommandAllowlist` is the gateway command-type guard. Do not treat service-client grants as a replacement for the spine allowlist; a command must satisfy both the hard spine allowlist and the service-client grant.
- `IChatBotClient.SubmitAsync` already maps `ChatBotSurfaceOrigin` to the generated wire request. Future adapters should use this path; do not introduce adapter-specific gateway stage calls.
- `AuditEnvelope` currently records tenant, actor, actor type, command, resource, decision, reason, correlation, policy snapshot, source evidence refs, idempotency, state transition, redaction decision, outcome, phase, schema version, predecessor hash, and surface origin. Story 5.1 should enrich this without making audit payloads secret-bearing.
- Existing worktree has unrelated modified `Hexalith.Tenants` submodule state and `_bmad-output/story-automator/orchestration-4-20260601-145742.md`; do not revert or include them in the implementation.

### Architecture Guardrails

- Every state mutation must route through `CommandGateway`; UI/CLI/MCP/service/AI adapters submit typed commands through the client/gateway and must not replicate authentication, tenant binding, authorization, risk, approval, audit, idempotency, grant validation, or allowlist logic.
- Gateway stage interfaces remain internal to `.Server`; adapter assemblies must not reference `Hexalith.ChatBot.Server.Gateway` or `.Stages`.
- Aggregates remain pure: no Keycloak, grant-store, cache, Dapr, logging, authorization, policy lookup, sibling client, AI provider, or async work inside aggregate `Handle`.
- Rejections for expected security/business failures are structured denial results/events, not thrown exceptions.
- Tenant isolation is fail-closed and claim-bound. Cross-tenant identifiers in command payloads are rejected even when the credential is valid for another tenant.
- Public responses, audit evidence, logs, traces, fixtures, and support artifacts are metadata-only. Never expose access tokens, refresh tokens, client secrets, raw JWTs, raw OAuth assertions, raw claim sets, command payloads, project names in denial bodies, file metadata, audit internals, prompts, completions, provider payloads, or raw exception text.
- Use repo-pinned stack only: .NET SDK `10.0.302`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, NetArchTest, existing Keycloak/Aspire/Dapr patterns. Do not add inline package versions or upgrade dependencies.
- Root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Project Structure Notes

- Place public grant/evidence contracts only under `src/Hexalith.ChatBot.Contracts/Commands`, `Queries`, `Enums`, or a focused `Identities`/metadata folder if they cross the client/server boundary.
- Place grant validation, cache/projection abstractions, and authorization stage logic under `src/Hexalith.ChatBot.Server/Gateway/Stages/` or a focused internal gateway subfolder.
- Keep Keycloak fixture changes in `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json`.
- Keep architecture rules in `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/`; do not enforce source boundaries only through code review.
- Mirror tests by boundary under `tests/Hexalith.ChatBot.Contracts.Tests`, `Client.Tests`, `Server.Tests/Gateway/Stages`, `Architecture.Tests`, and `Conformance.Tests`.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
  - Add AppHost/realm-focused validation if Keycloak realm parsing or service-account fixture tests live outside the suites above.
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Do not add Playwright unless a visible UI surface is touched; this story should be backend/contract/AppHost/conformance focused.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 5 is "Cross-Surface Parity - CLI & MCP" and Story 5.1 is the service-client identity/grant foundation for later CLI/MCP parity.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR19, FR55, FR57, FR59, FR68, FR74-FR86, NFR2, NFR4-NFR8, NFR30-NFR31, and NFR65-NFR70.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys, and approved service-send authority mapping.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially Keycloak identity, tenant binding, CommandGateway ordering, adapter-only-through-client rule, audit envelope, metadata-only diagnostics, project structure, NetArchTest/conformance enforcement, and testing standards.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`, especially actor badges, audit timeline fields, Command Surface Reference, Flow 6 CLI parity, redacted denial language, and the stale credential / tenant switch / revoked service-client scope fail-closed rule. No new visual UI surface is implemented by this story.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant rules: .NET SDK `10.0.302`, `net10.0`, warnings-as-errors, central package management, metadata-only diagnostics, tenant isolation, pure EventStore aggregates, Dapr duplicate/order tolerance, Keycloak readiness for security tests, FrontComposer/Fluent UI inheritance when UI is touched, Shouldly/NSubstitute/xUnit patterns, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/4-9-correction-invalidates-ai-action-proposals.md`; carry-forward is to reuse the command spine, safe refusal/message catalog behavior, metadata-only audit/projection patterns, compiled xUnit v3 validation style, and avoid unrelated `Hexalith.Tenants`/story-automator worktree changes.
- Inspected current code and tests for likely update surfaces: Keycloak realm fixture, `ClaimsAuthenticationStage`, `ClaimsTenantBindingStage`, `ParticipantAuthorizationStage`, `ChatBotAuthorizationReasonCodes`, `ChatBotSpineCommandAllowlist`, `ChatBotCommandSubmission`, `CommandSubmissionWireRequest`, `AuditEnvelope`, `AuditEnvelopeFactory`, `ActorType`, `ChatBotSurfaceOrigin`, `IChatBotClient`, gateway tests, architecture fitness tests, and conformance actor matrix.
- Recent git history shows Epic 4 completion: `18d246b docs(epic-4): add retrospective`, `33287c2 feat(story-4.9): Correction invalidates AI action proposals`, `c8b7d54 feat(story-4.8): Refusal and safe block behavior`, `b812b4c feat(story-4.7): Allowlisted AI command execution`, and `a8f1c37 feat(story-4.6): AI action preview and inspection`.
- Latest-technology web research was not required for story creation: this story adds no new external package, protocol, provider API, or framework surface and should use repo-pinned Keycloak/OIDC, .NET, Aspire, Dapr, xUnit, Shouldly, NSubstitute, and NetArchTest patterns.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 5 and Story 5.1 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR19, FR55, FR57, FR59, FR68, FR74-FR86, NFR2, NFR4-NFR8, NFR30-NFR31, NFR65-NFR70.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys, approved service-send authority mapping.
- `_bmad-output/planning-artifacts/architecture.md` - Keycloak identity, tenant binding, CommandGateway, adapter boundary, audit, metadata-only diagnostics, project structure, and tests.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - actor badge and audit timeline semantics for service clients, CLI, MCP, workers, mailbox events, and AI actors.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - Command Surface Reference, Flow 6 CLI parity, redacted denials, and audit semantics.
- `_bmad-output/implementation-artifacts/4-9-correction-invalidates-ai-action-proposals.md` - prior-story implementation intelligence and validation style.
- `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json` - current realm/client/user fixture.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsAuthenticationStage.cs` - current safe `sub` authentication.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsTenantBindingStage.cs` - current claim-bound tenant binding and command-target tenant validation.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - current participant/admin authorization guardrails.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - current gateway ordering and fail-closed behavior.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs` - current audit envelope shape.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - current audit evidence extraction and safe metadata behavior.
- `src/Hexalith.ChatBot.Contracts/Enums/ActorType.cs` - current actor-type wire contract.
- `src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs` - current surface-origin wire contract.
- `src/Hexalith.ChatBot.Client/IChatBotClient.cs` - current typed client boundary future adapters must wrap.
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationActorMatrix.cs` - current nine-actor matrix.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - 108 passed.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - 15 passed.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - 456 passed.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - 35 passed.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - 58 passed.
- `./tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -parallel none` - 4 passed.
- Additional in-process regression suites passed: Aspire.Tests 2, ServiceDefaults.Tests 3, Testing.Tests 41, UI.Tests 97, Workers.Tests 15.
- `./tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -parallel none` - 2 passed, 2 skipped by expected Tier-3 infrastructure opt-in gate.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implemented first-class service-client grant metadata contracts, service-client class wire tokens, internal claim-backed grant resolution/validation, explicit service/AI actor classification, service-client fail-closed reason codes, and service-client grant evidence propagation into `AuditEnvelopeFactory`.
- Added dedicated Keycloak service-account fixture clients for CLI, MCP, background worker, mailbox ingestion, audit projection, and AI action execution with least-privilege posture and bounded tenant/grant/scope/expiry/command claims; preserved the existing public `hexalith-chatbot` E2E client and `actor-alpha`.
- Added deterministic coverage for grant serialization, secret-field absence, realm least privilege, service-client authorization failures, delegated audit evidence, cache staleness, targeted revocation invalidation, and existing conformance/architecture boundaries.
- No OpenAPI/generated-client update was required: the story added contracts and internal gateway enforcement but did not change public command submission or inspection HTTP shapes. `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` and `tests/fixtures/hexalith-chatbot-generated-client.sha256` have no diff.
- Senior review auto-fix closed two service-client identity binding gaps: service-account posture now overrides conflicting human-looking actor-type claims, and grant validation now requires grant service-client ID to match the authenticated service-client identity.
- Definition of Done: PASS. All story tasks/subtasks are complete, acceptance criteria are covered by tests, and story status is `done`.

### File List

- `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json`
- `src/Hexalith.ChatBot.Contracts/Enums/ServiceClientClass.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ServiceClientClasses.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/ServiceClientGrant.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/ServiceClientGrantEvidence.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotGatewayContext.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotAuthenticatedActor.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotAuthenticationResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ChatBotAuthorizationResult.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsAuthenticationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsServiceClientGrantResolver.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IServiceClientGrantResolver.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IServiceClientGrantValidator.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantProjectionCache.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantResolution.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs`
- `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ServiceClientGrantContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantProjectionCacheTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` (end-to-end wire-dispatch coverage proving the service-client grant flows through the shared command spine and fails closed before idempotency/dispatch/audit; added during 2026-06-10 review, currently uncommitted in the working tree)

### Change Log

- 2026-06-01: Added service-client grant contracts, Keycloak service-account fixture clients, internal gateway grant validation, metadata-only audit evidence, staleness/revocation cache semantics, and focused acceptance coverage for Story 5.1.
- 2026-06-01: Senior developer review fixed service-client actor-type spoofing and grant service-client mismatch fail-closed gaps; refreshed required validation suites.
- 2026-06-10: Story-automator adversarial re-review (Jérôme Piquot). Verified build (0 warnings / 0 errors) and all required suites green (Server 1557, Contracts 480, Architecture 39, Conformance 87, AppHost 5). Documented previously-undocumented end-to-end wire-dispatch coverage in `CommandGatewayAdmissionApiE2ETests.cs` (File List) and recorded the AC5 projection-cache wiring follow-up. No CRITICAL issues; status remains `done`.

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fix.

Findings fixed:

- HIGH: `ClaimsAuthenticationStage` trusted a safe `chatbot:actor-type=human` claim even when service-client identity evidence was present, allowing a service-account token to avoid service-client grant validation. Fixed by deriving service/AI posture from resolved service-client identity before accepting human-looking actor-type claims.
- HIGH: `ServiceClientGrantValidator` did not compare the resolved grant service-client ID with `ChatBotAuthenticatedActor.ServiceClientId`, so a grant claim set for one service client could authorize a different authenticated service-client identity. Fixed by failing closed when the grant and authenticated service-client identity differ.

Regression coverage added:

- `ClaimsAuthenticationStageShouldNotLetServiceAccountClaimHumanPosture`
- `ServiceClientGrantShouldMatchAuthenticatedServiceClientIdentity`

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - 108 passed.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - 15 passed.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - 456 passed.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - 35 passed.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - 58 passed.
- `./tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -parallel none` - 4 passed.

---

Reviewer: Jérôme Piquot on 2026-06-10 (story-automator adversarial re-review)

Outcome: Approved. No CRITICAL issues; status remains `done`.

Scope verified: read every File List source file and cross-checked all 7 acceptance criteria against the implementation (not against the story's own claims).

AC validation:

- AC1 — IMPLEMENTED. `ServiceClientGrant`/`ServiceClientGrantEvidence` records, `ServiceClientClass` enum + wire tokens, and six dedicated Keycloak service-account clients (`cli-automation-client`, `mcp-tool-client`, `background-worker-client`, `mailbox-ingestion-client`, `audit-projection-client`, `ai-action-execution-client`), each `publicClient:false`, `directAccessGrantsEnabled:false`, `serviceAccountsEnabled:true`, `fullScopeAllowed:false`. Public `hexalith-chatbot` E2E client and `actor-alpha` preserved.
- AC2 — IMPLEMENTED. `ClaimsAuthenticationStage` derives service/AI posture from resolved service-client identity before trusting human-looking actor-type claims; `ServiceClientGrantValidator` fails closed (id/tenant/surface/expiry/revocation/over-/under-scope) inside `ParticipantAuthorizationStage`, before idempotency, dispatch, and durable state.
- AC3 — IMPLEMENTED. `AuditEnvelopeFactory.ServiceClientGrantEvidenceRefs` emits service-client/actor-type/grant/scope/expiry/command-set/surface/class/delegated-user/oauth-fingerprint as `SafeOptionalToken` metadata only; OAuth evidence is a fingerprint, never a token.
- AC4 — IMPLEMENTED. Service-client failures map to safe snake_case reason codes and the redacted catalog response (E2E asserts `category=authorization_denied`, `visibility=metadata_only`, and no tenant/resource/oauth/secret in the body).
- AC5 — PARTIAL (LOW). `ServiceClientGrantProjectionCache` implements 5-min staleness / 60-sec revocation with tenant|client|surface|grant isolation and deterministic clock-based tests, but is not wired into the live claims-direct resolution path. Tracked as a Review Follow-up.
- AC6 — IMPLEMENTED. `SetAssociationConfidenceThresholds` (and tenant-policy/admin commands) require `HasHumanAdminScope`; service/AI actors are denied even with tenant-admin-looking claims.
- AC7 — IMPLEMENTED. Acceptance coverage across Contracts/Server/Architecture/Conformance/AppHost suites, plus the added end-to-end spine wire-dispatch tests.

Findings:

- MEDIUM (fixed — documentation): `CommandGatewayAdmissionApiE2ETests.cs` added two end-to-end tests (`...ShouldAcceptServiceClientGrantThroughSharedCommandSpine`, `...ShouldFailClosedServiceClientGrantErrorsBeforeDurableWork`) proving the grant path runs through the shared command spine and fails closed before idempotency/dispatch/audit — legitimate AC2/AC7 coverage that was missing from the File List. Added to the File List and Change Log. The file remains uncommitted in the working tree (committing is left to the story-automator commit step / the author).
- LOW (tracked): AC5 projection cache not on the live path — see Review Follow-ups (AI).

Validation (compiled in-process xUnit v3 runners, this review):

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `Hexalith.ChatBot.Server.Tests -parallel none` - 1557 passed (includes the added E2E grant tests).
- `Hexalith.ChatBot.Contracts.Tests -parallel none` - 480 passed.
- `Hexalith.ChatBot.Architecture.Tests -parallel none` - 39 passed.
- `Hexalith.ChatBot.Conformance.Tests -parallel none` - 87 passed.
- `Hexalith.ChatBot.AppHost.Tests -parallel none` - 5 passed.
