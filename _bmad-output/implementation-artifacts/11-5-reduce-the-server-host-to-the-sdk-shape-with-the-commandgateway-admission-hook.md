---
baseline_commit: 0639e97915292c727f72e4554790636dc6931ee5
---

# Story 11.5: Reduce the Server host to the SDK shape with the CommandGateway admission hook

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-19. -->
<!-- Senior Developer Review (AI) completed 2026-06-19: 1 CRITICAL finding (live double-admission on the EventStore→/process callback) → status reverted to in-progress. See "Senior Developer Review (AI)". -->
<!-- Senior Developer Review (AI) re-review 2026-06-19 (story-automator auto-fix): all prior follow-ups verified resolved (CRITICAL double-admission fixed via non-forgeable DataProtection admission marker + real dispatch→/process round-trip test that fails on re-admission). 0 CRITICAL remain → status "done". 1 new MEDIUM (DataProtection key-ring sharing for multi-replica) mitigated + tracked. Build 0 errors; Server 1677/Arch 49/Contracts 483 passing. See second "Senior Developer Review (AI)" section. -->

## Story

As a ChatBot maintainer,
I want `Hexalith.ChatBot.Server` hosted by `AddEventStoreDomainService()`/`UseEventStoreDomainService()` with the CommandGateway registered as the SDK admission-stage chain,
so that the host is the platform's, the governance is ChatBot's, and the hand-rolled `Program.cs` disappears.

## Acceptance Criteria

1. **Server host uses the DomainService SDK shape.** Given Stories 11.1-11.4 and Stories 8.7a/8.7b are complete, when Story 11.5 is implemented, then `src/Hexalith.ChatBot.Server/Program.cs` is reduced to SDK host setup, ChatBot admission-chain registration, runtime feature flags that cannot live elsewhere, `app.UseEventStoreDomainService()`, and `app.Run()`; target size is at or below about 50 lines. `Program.cs` must not map custom `/process`, `/query`, `/project`, projection subscription endpoints, health endpoints already owned by the SDK, or inline public read/command route bodies. [Source: _bmad-output/planning-artifacts/epics.md#Story 11.5; _bmad-output/planning-artifacts/architecture.md#Host-Layer Reuse (D8); docs/adrs/domainservice-sdk-host-adoption.md]

2. **CommandGateway admission runs as the SDK pre-commit chain.** Given the FR81a invariant, when `POST /process` enters `DomainServiceRequestRouter.ProcessAsync(...)`, then ChatBot admission executes through one or more registered `IDomainServiceAdmissionStage` implementations before the keyed `IDomainProcessor` runs. The preserved order is `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> pre-commit-audit`; execution stops at the first rejection; accepted commands proceed to the SDK processor exactly once. [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainServiceAdmissionStage.cs; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DomainServiceRequestRouter.cs; src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]

3. **Admission rejections remain fail-closed, typed, and metadata-only.** Given authentication, tenant binding, authorization, risk, approval, allowlist, lifecycle, idempotency, or pre-commit audit denies a command, when the SDK hook returns, then the result is a `DomainServiceAdmissionResult.Rejected(...)` carrying one or more `IRejectionEvent` payloads, not HTTP `ProblemDetails`, raw exceptions, or payload-bearing diagnostics. Denied commands must not call `IDomainProcessor`, dispatch to EventStore, write post-commit audit, or leave a dangling coarse-idempotency admission. [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DomainServiceAdmissionResult.cs; Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainServiceWireResult.cs; src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]

4. **Existing caller behavior is preserved or explicitly moved behind SDK-compatible adapters.** Given existing UI/CLI/MCP/client callers and tests still exercise public command/read compatibility routes, when host code is reduced, then those behaviors remain green either through SDK canonical endpoints (`/process`, `/query`, `/project`, `/replay-state`, `/admin/operational-index-metadata`) or through focused ChatBot route-adapter extension(s) outside `Program.cs` with behavior-parity tests. No API route may silently disappear, change redaction/problem shape, bypass tenant isolation, or lose correlation/task/surface-origin/replay metadata without an approved contract artifact. [Source: _bmad-output/implementation-artifacts/11-3-migrate-chatbot-query-endpoints-to-idomainqueryhandler-and-iquerycursorcodec.md; _bmad-output/implementation-artifacts/11-4-migrate-projections-telemetry-and-health-to-sdk-contracts.md; tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs]

5. **Hand-rolled DomainService host plumbing is removed and mechanically prevented from regrowing.** Given the SDK maps canonical DomainService endpoints, when Story 11.5 completes, then `src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceEndpoints.cs` and `src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceRequestHandler.cs` are deleted or made obsolete by the SDK route, manual `/query` and `/project` maps are removed, and architecture tests forbid inline query endpoint mapping in `Program.cs`, custom domain-service `/process` handlers, per-domain telemetry/health classes, and host wiring beyond SDK calls plus admission registration. [Source: _bmad-output/planning-artifacts/architecture.md#Host-Layer Reuse (D8); tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs]

6. **Scope stays inside Story 11.5.** Given Story 11.6 owns composition cleanup, when Story 11.5 completes, then it does not remove `src/Hexalith.ChatBot.AppHost`, `src/Hexalith.ChatBot.Aspire`, or `src/Hexalith.ChatBot.ServiceDefaults`, does not implement `AddEventStoreDomainModule(...)` composition, and does not edit `Hexalith.EventStore/src/**` unless a missing SDK capability is proven and explicit submodule approval is obtained. [Source: _bmad-output/planning-artifacts/epics.md#Story 11.6; docs/adrs/domainservice-sdk-host-adoption.md#Exception Boundary]

## Tasks / Subtasks

- [x] Establish the final Server host boundary (AC: 1, 5, 6)
  - [x] Read `Program.cs` end to end and classify every remaining line as SDK host setup, ChatBot admission registration, runtime feature toggle, compatibility route adapter, or removable host plumbing.
  - [x] Move non-host service/route composition into focused extension methods under existing Server ownership areas (for example `Registration`, `Gateway`, `Queries`, `Projections`, or `Operations`) only when behavior tests prove it still belongs after 11.5.
  - [x] Reduce `Program.cs` to the target shape: `WebApplication.CreateBuilder(args)`, `AddEventStoreDomainService(typeof(GovernedOperationAggregate).Assembly)`, ChatBot command/admission registration, unavoidable feature flags, `Build`, `UseEventStoreDomainService()`, and `Run`.
  - [x] Do not keep direct `MapChatBotDomainServiceEndpoints`, manual `/query`, manual `/project`, `MapDefaultEndpoints`, or ChatBot health route mappings in `Program.cs` when the SDK owns them.

- [x] Implement the ChatBot SDK admission adapter without wrapping `CommandGateway.SubmitAsync` as-is (AC: 2, 3)
  - [x] Create one cohesive `IDomainServiceAdmissionStage` adapter or a set of ordered stage adapters that reuses the existing stage services (`IAuthenticationStage`, `ITenantBindingStage`, `IAuthorizationStage`, `IRiskClassifier`, `IApprovalGate`, `IIdempotencyStore`, `IAuditWriter`, lifecycle guard, alert/replay seams) but stops before durable dispatch.
  - [x] Do not call `CommandGateway.SubmitAsync(...)` from the SDK admission stage; it dispatches to EventStore and returns HTTP-oriented `ChatBotGatewayResult`, which would recurse and produce the wrong rejection surface.
  - [x] Build the admission context from `DomainServiceAdmissionContext.Command` (`CommandEnvelope`) and safe extension metadata. Preserve tenant, user, command id, correlation id, task id, surface origin, replay run id, service-client grant evidence, and command payload handling needed by the existing stages.
  - [x] If a `ClaimsPrincipal` is still required by existing stage contracts, introduce a narrow internal adapter that reconstructs only trusted, metadata-safe claims from `CommandEnvelope`/validated EventStore metadata. Do not trust caller payload fields or arbitrary extension keys as authority.
  - [x] Map each denial to typed `IRejectionEvent` payloads with safe reason codes. Reuse existing ChatBot rejection events where semantically correct; add a small explicit admission rejection event only if no existing event can represent gateway-stage denial without leaking data.

- [x] Preserve FR81a stage order and side effects (AC: 2, 3)
  - [x] Keep order exactly: authenticate, tenant-bind, authorize, risk-classify, approval-gate, command allowlist if not already included in authorization, coarse idempotency, lifecycle validation, pre-commit audit.
  - [x] Preserve fail-closed pre-commit audit behavior: audit unavailable returns a typed rejection, queues the existing replay intent, emits the existing operator alert, and aborts any opened coarse-idempotency admission.
  - [x] Preserve duplicate replay behavior: equivalent duplicate commands must return the prior logical result or a typed no-op/rejection posture consistent with current idempotency tests; conflicts must not dispatch.
  - [x] Preserve metadata-only audit and logging. Do not include raw command payloads, mailbox bodies, provider payloads, bearer tokens, tenant secrets, stack traces, or decoded JWT payloads in rejection events, logs, traces, or tests.

- [x] Remove custom DomainService endpoint plumbing (AC: 1, 5)
  - [x] Delete or retire `src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceEndpoints.cs` and `src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceRequestHandler.cs` after the SDK `/process` path is covered.
  - [x] Remove manual `/query` and `/project` mappings from `Program.cs`; rely on `UseEventStoreDomainService()` / `MapEventStoreDomainService()` for SDK-discovered `IDomainQueryHandler` and `IDomainProjectionHandler` dispatch.
  - [x] Keep projection pub/sub compatibility only if current topology still requires it before Story 11.6, and move that mapping out of `Program.cs` behind a clearly named compatibility extension with tests documenting the temporary boundary.
  - [x] Keep public command/read compatibility adapters only if current generated client/UI/CLI/MCP tests still require them, and move them out of `Program.cs`; do not reintroduce query logic or command admission logic inline.

- [x] Update registrations and references (AC: 1, 2, 5)
  - [x] Register the ChatBot admission stage(s) through `AddEventStoreDomainAdmissionStage(...)` in deterministic order after `AddEventStoreDomainService(...)`.
  - [x] Keep `AddEventStoreDomainTelemetry("chatbot")`, `AddEventStoreDomainStateStoreHealthCheck(...)`, `IReadModelStore`, `ReadModelWritePolicy`, `IQueryCursorCodec`, query handlers, and projection handlers from Stories 11.3/11.4 intact.
  - [x] Drop direct `Hexalith.EventStore.Client` / `Contracts` references from `Hexalith.ChatBot.Server` only if the compiler and tests prove they are transitively supplied and no source file needs them directly.
  - [x] Do not edit EventStore submodule source for this story unless the local SDK hook is missing required capability; if that happens, stop and obtain explicit submodule approval before any EventStore write.

- [x] Add parity, fail-closed, and anti-regrowth tests (AC: 1-6)
  - [x] Extend `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` or focused gateway tests to prove SDK `/process` runs ChatBot admission before the domain processor for accepted, unauthenticated, cross-tenant, not-allowlisted, lifecycle-invalid, idempotency-conflict, duplicate, and audit-unavailable paths.
  - [x] Add a test proving a rejecting admission stage produces `DomainServiceWireResult.IsRejection == true` with typed rejection payloads and does not invoke the processor.
  - [x] Keep existing differential-conformance, cross-tenant isolation, fail-closed audit, idempotency, query, projection, health, and operation-status tests green.
  - [x] Extend `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs` or adjacent fitness tests to prevent custom `/process` endpoint classes, manual `MapPost("/query")`/`MapPost("/project")`, inline `MapGet` read bodies, per-domain telemetry/health classes, and large `Program.cs` regrowth.
  - [x] Add route ownership tests that assert the SDK canonical endpoints are available exactly once and that any remaining temporary compatibility routes are mapped outside `Program.cs`.

- [x] Run focused verification (AC: 1-6)
  - [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`
  - [x] `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 -nodeReuse:false` or, if VSTest socket setup is blocked, build and run the xUnit v3 in-process runner directly.
  - [x] `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1 -nodeReuse:false` or the xUnit v3 in-process runner equivalent.
  - [x] `dotnet test tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1 -nodeReuse:false` if command/rejection/public contract artifacts change.
  - [x] `git diff --check`

### Review Follow-ups (AI)

- [x] [AI-Review][CRITICAL] Resolve double-admission on the EventStore→`/process` callback for gateway-submitted commands. The generated client posts to `/api/v1/commands` (`src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs:283`) → `CommandGateway.SubmitAsync` admits (#1) → `AcceptedCommandDispatcher` → `IEventStoreGatewayClient.SubmitCommandAsync` → live EventStore actor → DAPR-invokes ChatBot `/process` → `DomainServiceRequestRouter.ProcessAsync` runs `ChatBotDomainServiceAdmissionStage` (#2) unconditionally. Admission #2 reconstructs a principal from envelope extensions (`ChatBotDomainServiceAdmissionStage.PrincipalFromEnvelope`) that `AcceptedCommandDispatcher.BuildExtensions` (`AcceptedCommandDispatcher.cs:1104`) never populates with tenant-role/project-owner/participant-authority/service-client-grant claims → authorize denies; or, if authorize passes, the in-flight coarse-idempotency record (`InMemoryCoarseIdempotencyStore.cs:49`) makes #2 wait on #1's outcome that #1 cannot record until dispatch returns → request-scoped deadlock. Net: live HTTP command path is broken (AC2 "exactly once", AC3 fail-closed/no double side-effects, AC4 caller-behavior preserved). Choose one: (a) make `CommandGateway` a thin translator that no longer pre-admits and carries trusted server-derived authority into the envelope so `/process` is the single admission point (reworks post-commit audit / operation-status ownership), or (b) introduce a trusted EventStore-internal origin marker so the admission stage short-circuits already-admitted dispatch calls (needs EventStore SDK capability → submodule approval per AC6). Do NOT use a forgeable extension flag.
- [x] [AI-Review][HIGH] Add a behavior-parity test that exercises the real dispatch→`/process` round trip. `AcceptingEventStoreGatewayClient` (`ServerBootstrapApiTests.cs:3083`) returns success without invoking `/process`, so the double-admission path is never executed in tests. A test that routes `SubmitCommandAsync` back through the `/process` admission stage (or an integration test against a live EventStore) is required to prove AC4 parity for the gateway path.
- [x] [AI-Review][MEDIUM] `ChatBotDomainServiceAdmissionStage.RecordSdkAcceptedOutcomeAsync` records the coarse-idempotency outcome (hardcoded `LifecycleState.Proposed`, `AcceptedAt = clock.UtcNow`) BEFORE the SDK domain processor runs. If the processor rejects/throws, a retry replays a false "accepted" prior outcome and never re-processes. The HTTP gateway records the outcome only AFTER successful dispatch (`CommandGateway.cs:123`). Reconcile (e.g., an SDK post-process hook, or do not record an accepted outcome from the admission stage).
- [x] [AI-Review][LOW] Replace ad-hoc rejection reason-code string literals `"duplicate_replay_prior_outcome"` and `"invalid_command_payload"` (`ChatBotDomainServiceAdmissionStage.cs:46,86`) with named constants in the reason-code catalog (`ChatBotAuthorizationReasonCodes`/sibling) for consistency and to keep wire reason codes finite/auditable.
- [x] [AI-Review][LOW] Confirm duplicate-replay posture divergence is intended: HTTP gateway returns `AcceptedResult(priorOutcome)` (idempotent success) while `/process` returns `IsRejection == true`. Permitted by AC3 ("typed no-op/rejection posture") but document the contract so callers handle the two surfaces consistently.
- [x] [AI-Review][LOW] Strengthen anti-regrowth fitness assertions: `program.ShouldNotContain("MapPost(\n    \"/query\"")` / `"MapPost(\n    \"/project\"")` (`DomainServiceSdkHostAdoptionAdrTests.cs`) depend on exact whitespace and would pass silently if the code were reformatted onto one line. Prefer a whitespace-insensitive check (the existing `ShouldNotContain("\"/query\"")` is the robust form).
- [x] [AI-Review][MEDIUM] (re-review) The admission marker — the entire CRITICAL fix — relies on `IDataProtector.Unprotect` succeeding on whatever replica handles the EventStore→`/process` callback. `Program.cs` registered `AddDataProtection()` with no stable application name and no persisted/shared key ring, and neither `ServiceDefaults` nor the EventStore SDK configures one, so a multi-replica (or restarted) deployment would fail to validate the marker → admission re-runs → the original double-admission defect resurfaces. Single-instance dev/test and the in-process round-trip test share an in-process ring and mask this. **Applied:** added `SetApplicationName("Hexalith.ChatBot")` and an explicit comment at the DataProtection registration documenting the requirement (`Program.cs`). **Residual (deployment / Story 11.6 composition):** wire a shared, persisted key store (Dapr/Redis/blob) before scaling `Hexalith.ChatBot.Server` beyond one replica — out of host-reduction scope and must not be chosen blindly here (AC6 keeps composition in Story 11.6).

## Dev Notes

### Discovery Results

- Loaded workflow skill files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- Loaded BMad config from `_bmad/bmm/config.yaml`: planning artifacts are in `_bmad-output/planning-artifacts`; implementation artifacts are in `_bmad-output/implementation-artifacts`; output language is English.
- Loaded `sprint_status` from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key is `11-5-reduce-the-server-host-to-the-sdk-shape-with-the-commandgateway-admission-hook`, currently `backlog`; `epic-11` is `in-progress`; Stories 11.1-11.4 and prerequisite Stories 8.7a/8.7b are `done`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`. Story 11.5 is the final Server-host reduction before Story 11.6 composition cleanup.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`. D8 requires ChatBot to run on `Hexalith.EventStore.DomainService` and calls for Story 11.5 anti-regrowth tests.
- Loaded the accepted ADR at `docs/adrs/domainservice-sdk-host-adoption.md`. It gates Stories 11.2-11.6 and allows only a thin local-dev AppHost exception for Story 11.6 to review.
- Loaded previous story file `_bmad-output/implementation-artifacts/11-4-migrate-projections-telemetry-and-health-to-sdk-contracts.md`, plus Story 11.2/11.3 context for the SDK hook and query/cursor migrations.
- Loaded persistent project-context facts from sibling `**/project-context.md` files. Relevant common rules: .NET 10, warnings-as-errors, central package versions, xUnit v3 + Shouldly, `ConfigureAwait(false)` in production awaits, `.slnx` only, no recursive submodules, and no unsolicited submodule edits.
- No direct PRD or UX file matched the configured create-story input patterns. This is a backend/platform-host migration; UI behavior must remain compatible but no new screen is introduced.

### Epic 11 Context

Epic 11 closes readiness pass-2 Issue #1: ChatBot had a large hand-rolled host, module-owned hosting projects, and no DomainService SDK contract usage. Stories 11.1-11.4 already established the ADR, added the SDK pre-commit hook, migrated query/cursor dispatch, and migrated projection/read-model/telemetry/health contracts. Story 11.5 is where ChatBot consumes the hook and reduces the Server host. Story 11.6 then moves AppHost/Aspire/ServiceDefaults composition to `AddEventStoreDomainModule(...)`.

Binding sequence now satisfied for 11.5:

- 11.1: accepted ADR at `docs/adrs/domainservice-sdk-host-adoption.md` - done.
- 11.2: platform pre-commit admission hook in `Hexalith.EventStore.DomainService` - done.
- 11.3: query endpoints to `IDomainQueryHandler` and `IQueryCursorCodec` - done.
- 11.4: projections/read models/telemetry/health to SDK contracts - done.
- 8.7a/8.7b: durable control-state/rate-limit projection and periodic enforcement runtime - done.
- 11.5: reduce Server host and register the CommandGateway admission hook - this story.
- 11.6: retire or sharply reduce module-owned hosting composition - next story.

### Current State to Modify

`src/Hexalith.ChatBot.Server/Program.cs` still owns substantial host wiring:

- Calls both `builder.AddServiceDefaults(useOtlpExporterWhenConfigured: false)` and `builder.AddEventStoreDomainService(...)`; the SDK already calls service defaults.
- Registers ChatBot command gateway, telemetry, health, Data Protection query cursor codec, optional workflow/periodic enforcement runtimes, and optional DAPR read-model stores.
- Applies JWT authentication, correlation middleware, CloudEvents, DAPR subscribe handler, default endpoints, ChatBot health endpoints, public `POST /api/v1/commands`, custom `MapChatBotDomainServiceEndpoints()`, manual `/query`, manual `/project`, seven projection subscription endpoint groups, and eight public read/compatibility routes.
- Contains helper methods for read-query dispatch, project conversation HTTP result mapping, denial mapping, command id normalization, surface origin, replay run id, and query JSON options.

The reduction target is not just "move lines around." The dev must distinguish actual host boilerplate from domain behavior that still needs a home. Keep domain behavior, but remove Server host ownership of SDK-provided routes and ensure any temporary compatibility adapter has tests and a documented owner.

### SDK Admission Contract

Story 11.2 added the platform hook:

- `IDomainServiceAdmissionStage` exposes `Name` and `EvaluateAsync(DomainServiceAdmissionContext, CancellationToken)`.
- `DomainServiceAdmissionContext` carries the full `DomainServiceRequest`, exposes `CommandEnvelope Command`, and exposes `CurrentState`.
- `DomainServiceAdmissionResult.Accepted()` allows the SDK to continue to the keyed `IDomainProcessor`.
- `DomainServiceAdmissionResult.Rejected(IReadOnlyList<IRejectionEvent>)` returns a typed domain rejection through `DomainResult.Rejection(...)` and `DomainServiceWireResult.FromDomainResult(...)`.
- `AddEventStoreDomainAdmissionStage(...)` registers ordered stages; order is DI registration order and execution stops at first rejection.
- `DomainServiceRequestRouter.ProcessAsync(...)` runs the optional stage chain before resolving `IDomainProcessor`. With no stages registered, existing SDK consumers keep the old behavior.

The hook has no `HttpContext` or original `ClaimsPrincipal`. ChatBot admission must derive authority from trusted EventStore/DomainService metadata and validated extension fields, not from arbitrary command payload fields. This is the main migration risk.

### CommandGateway Migration Guardrails

Do not wrap `CommandGateway.SubmitAsync(...)` directly as an SDK admission stage. The current `CommandGateway` does too much for the hook:

- It starts from HTTP-facing `ChatBotCommandSubmission` with `ClaimsPrincipal`.
- It returns `ChatBotGatewayResult` / HTTP `ProblemDetails`, not typed `IRejectionEvent`.
- It calls `ICommandDispatcher.DispatchAsync(...)`, which submits to EventStore. Calling it from `/process` would recurse or double-dispatch.
- It records post-commit audit and operation status after dispatch; SDK admission must stop before durable processor execution.

The implementation should extract or introduce an internal admission service that reuses existing stage services and side-effect seams but returns an SDK-compatible admission result. A good shape is a focused `ChatBotDomainServiceAdmissionStage` or `ChatBotCommandAdmissionPipeline` that produces:

- Accepted: all FR81a gates passed, coarse-idempotency/pre-commit audit state is safe for the SDK processor to run.
- Rejected: typed, metadata-only `IRejectionEvent` list; any opened coarse-idempotency admission has been aborted where current behavior would abort; operator alert/replay intent side effects are preserved for audit-unavailable paths.

### Current Custom DomainService Route

`src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceEndpoints.cs` maps `POST /process` manually to `ChatBotDomainServiceRequestHandler`.

`src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceRequestHandler.cs` resolves the single `IDomainProcessor`, calls `ProcessAsync(request.Command, request.CurrentState)`, and returns `DomainServiceWireResult`. Its comments explicitly assume CommandGateway already ran before EventStore invoked `/process`. Story 11.5 makes that assumption false by moving admission into the SDK `/process` hook. Delete or retire this custom route once `UseEventStoreDomainService()` owns `/process`.

### Previous Story Intelligence

Story 11.2:

- Added the exact SDK hook needed here. No EventStore source edit should be needed for normal 11.5 work.
- Verified canonical SDK endpoints stayed stable: `/process`, `/replay-state`, `/query`, `/project`, and `/admin/operational-index-metadata`.
- Left ChatBot-specific FR81a consumption to Story 11.5.

Story 11.3:

- Migrated read logic to `IDomainQueryHandler` and cursors to `IQueryCursorCodec`.
- Left `/process`, projection subscription endpoints, public command route, health routes, and final host reduction for later stories.
- Warned not to recreate removed cursor logic or move read/query logic back into `Program.cs`.

Story 11.4:

- Migrated projections to `IDomainProjectionHandler`, read models to `IReadModelStore`/`ReadModelWritePolicy`, and telemetry/health to SDK helpers.
- Left projection pub/sub compatibility adapters because current AppHost topology may still deliver DAPR pub/sub events until 11.5/11.6 finish topology changes.
- Senior review noted `/project` unified dispatch is first-match-wins while the live DAPR topology processes all subscriptions. 11.5 should keep or explicitly test this when moving projection route ownership.

### Git Intelligence

Recent commits at story creation time:

- `0639e97 BMAD 6.8.0`
- `e5849ee feat: implement Epic 12 for ChatBot UI Fluent v5 component conformance and governance guard`
- `8bde798 feat: update shared Hexalith LLM instructions and submodule commits`
- `1e91a66 feat: integrate Fluent UI components and update OpenTelemetry configuration`
- `3d588f8 chore: update dependencies and submodules`

Actionable relevance: the working tree already has unrelated changes in `_bmad-output/story-automator/orchestration-1-20260609-212026.md`; do not touch or revert them. The current `HEAD` is `0639e97915292c727f72e4554790636dc6931ee5`.

### Project Structure Notes

Likely implementation locations:

- `src/Hexalith.ChatBot.Server/Program.cs` - reduce to SDK host shape and remove manual endpoint mapping.
- `src/Hexalith.ChatBot.Server/Gateway/*` - introduce the SDK admission adapter/pipeline and keep governance interfaces internal.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - refactor only if needed to share pre-dispatch admission logic; do not call it recursively from the SDK hook.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` - register admission stage(s), existing services, optional runtime features, DAPR read-model store swap, and compatibility services.
- `src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceEndpoints.cs` and `ChatBotDomainServiceRequestHandler.cs` - delete or retire after SDK `/process` owns dispatch.
- `src/Hexalith.ChatBot.Server/Queries/*` and `src/Hexalith.ChatBot.Server/Projections/*` - should remain SDK-discovered; avoid moving logic back into host code.
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` and `tests/Hexalith.ChatBot.Server.Tests/Gateway/*` - behavior parity and SDK admission tests.
- `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs` or adjacent fitness tests - anti-regrowth enforcement.

Avoid changing these unless a test proves it is necessary:

- `Hexalith.EventStore/src/**` - the SDK hook exists; submodule edits require explicit approval.
- `src/Hexalith.ChatBot.AppHost`, `src/Hexalith.ChatBot.Aspire`, `src/Hexalith.ChatBot.ServiceDefaults` - Story 11.6 owns composition retirement.
- Story 11.3 query handlers/cursor codec and Story 11.4 projection/read-model stores unless host reduction exposes a direct integration defect.

### Testing Standards

- Use xUnit v3 + Shouldly. Avoid raw `Assert.*`.
- Keep tests in existing projects and styles. `ServerBootstrapApiTests` already uses `WebApplicationFactory<Program>` and replaces `IEventStoreGatewayClient`/`ICommandDispatcher` for in-process admission behavior; adapt that style carefully for SDK `/process`.
- Production awaits need `ConfigureAwait(false)`. Existing test files often use `ConfigureAwait(true)`; follow the local test convention when editing tests.
- Do not add package versions to `.csproj`; versions belong in `Directory.Packages.props`.
- Do not initialize nested submodules and do not use recursive submodule commands.
- VSTest may fail in this sandbox because it opens a TCP listener. If that happens, build the test project and run the xUnit v3 executable from `bin/<Configuration>/net10.0/` directly, recording the limitation.

### Latest Technical Information

No external web research was used. The relevant technical truth is the checked-out local `Hexalith.EventStore` SDK source and local planning artifacts. Network access is restricted in this environment.

### Validation Notes

Checklist pass completed against `.agents/skills/bmad-create-story/checklist.md`:

- Reinvention prevention: story directs reuse of `IDomainServiceAdmissionStage`, `AddEventStoreDomainAdmissionStage`, `UseEventStoreDomainService`, `IDomainQueryHandler`, `IDomainProjectionHandler`, SDK telemetry/health, and existing ChatBot stage services instead of inventing a second host pipeline.
- Wrong-location prevention: story moves host wiring out of `Program.cs`, keeps governance under `.Server/Gateway`, and leaves AppHost/Aspire/ServiceDefaults retirement for Story 11.6.
- Regression prevention: story calls out public route compatibility, tenant isolation, redaction, correlation/task/surface-origin/replay metadata, idempotency, fail-closed audit, and projection pub/sub compatibility.
- Critical trap called out: do not wrap `CommandGateway.SubmitAsync(...)` from the SDK hook because it dispatches to EventStore and returns HTTP results rather than typed admission rejections.
- LLM optimization: acceptance criteria and tasks are grouped by concrete files, contracts, and failure modes.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: Hexalith.AI.Tools/hexalith-llm-instructions.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml]
- [Source: _bmad-output/implementation-artifacts/11-2-platform-pre-commit-admission-hook-in-the-domainservice-sdk.md]
- [Source: _bmad-output/implementation-artifacts/11-3-migrate-chatbot-query-endpoints-to-idomainqueryhandler-and-iquerycursorcodec.md]
- [Source: _bmad-output/implementation-artifacts/11-4-migrate-projections-telemetry-and-health-to-sdk-contracts.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 11: Minimal Technical Layer - DomainService SDK Host Adoption]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 11.5: Reduce the Server host to the SDK shape with the CommandGateway admission hook]
- [Source: _bmad-output/planning-artifacts/architecture.md#Host-Layer Reuse (D8)]
- [Source: docs/adrs/domainservice-sdk-host-adoption.md]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainServiceAdmissionStage.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DomainServiceAdmissionContext.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DomainServiceAdmissionResult.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainAdmissionServiceCollectionExtensions.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DomainServiceRequestRouter.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs]
- [Source: src/Hexalith.ChatBot.Server/Program.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceEndpoints.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceRequestHandler.cs]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-19T16:47:06+02:00 - Created Story 11.5 context artifact from BMAD create-story workflow inputs.
- 2026-06-19T17:35:00+02:00 - Story moved to `in-progress`; baseline commit preserved.
- `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 -nodeReuse:false` was attempted first and blocked by the sandbox VSTest TCP socket permission error.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` passed. The build still reports the pre-existing `Hexalith.Tenants` StackExchange.Redis version conflict warning.
- xUnit v3 in-process validation passed:
  - `Hexalith.ChatBot.Server.Tests`: 1668 passed, 0 failed.
  - `Hexalith.ChatBot.Architecture.Tests`: 49 passed, 0 failed.
  - `Hexalith.ChatBot.Contracts.Tests`: 483 passed, 0 failed.
- `git diff --check` passed.
- 2026-06-19T18:20:00+02:00 - Review follow-up implementation: protected already-admitted marker added for gateway→EventStore→SDK `/process` round trips; `GovernedOperationAggregate` explicitly registered under the ChatBot EventStore domain; SDK admission no longer records a pre-processor accepted idempotency outcome.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` passed after review follow-up changes. The build still reports the pre-existing `Hexalith.Tenants` StackExchange.Redis version conflict warning.
- VSTest remained blocked by the sandbox TCP socket permission error for focused test projects; xUnit v3 in-process validation passed:
  - `Hexalith.ChatBot.Server.Tests`: 1676 passed, 0 failed.
  - `Hexalith.ChatBot.Architecture.Tests`: 49 passed, 0 failed.
  - `Hexalith.ChatBot.Contracts.Tests`: 483 passed, 0 failed.

### Completion Notes List

- Reduced `Program.cs` to the DomainService SDK host shape with ChatBot feature toggles, SDK telemetry/health registration, `UseEventStoreDomainService()`, compatibility endpoint extensions, and `Run()`.
- Extracted reusable pre-dispatch admission into `ChatBotCommandAdmissionPipeline` and registered `ChatBotDomainServiceAdmissionStage` through `AddEventStoreDomainAdmissionStage(...)`.
- Preserved public command/read/projection compatibility routes outside `Program.cs`, deleted the custom DomainService `/process` endpoint plumbing, and kept SDK `/process`, `/query`, `/project`, `/replay-state`, and operational metadata ownership.
- Added SDK `/process` admission tests for accepted and rejected paths, plus architecture anti-regrowth coverage for manual DomainService host plumbing.
- Adjusted legacy health/liveness tests to the SDK service-defaults/compatibility split: SDK readiness can include the DAPR state-store probe, while `/health/chatbot` remains the lightweight ChatBot compatibility health route.
- Resolved the review double-admission defect with a protected, server-generated admission marker carried by `AcceptedCommandDispatcher`; the SDK admission stage accepts only a valid marker bound to the command envelope, so gateway-submitted commands do not re-run admission on the EventStore callback.
- Added a real in-process gateway→EventStore→SDK `/process` round-trip test that fails if the callback re-admits or cannot resolve the SDK processor.
- Removed the SDK admission stage's pre-processor accepted-outcome write; successful direct `/process` admission now aborts the coarse pending record because the current SDK has no post-process hook.
- Registered `GovernedOperationAggregate` explicitly under the ChatBot EventStore domain so SDK keyed dispatch matches `AcceptedCommandDispatcher` submissions.
- Promoted SDK admission rejection reason literals to `ChatBotAuthorizationReasonCodes` constants and hardened the anti-regrowth fitness checks against whitespace-only rewrites.

### File List

- `_bmad-output/implementation-artifacts/11-5-reduce-the-server-host-to-the-sdk-shape-with-the-commandgateway-admission-hook.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAdmissionMarker.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotCompatibilityEndpointExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotDomainServiceAdmissionStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceEndpoints.cs` (deleted)
- `src/Hexalith.ChatBot.Server/Operations/ChatBotDomainServiceRequestHandler.cs` (deleted)
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`

### Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-06-19 | 0.1 | Created Story 11.5 developer context artifact and marked story ready for dev. | GPT-5 Codex |
| 2026-06-19 | 1.0 | Implemented SDK host reduction and CommandGateway admission-stage migration; story ready for review. | GPT-5 Codex |
| 2026-06-19 | 1.1 | Senior Developer Review (AI): 1 CRITICAL (live double-admission on EventStore→/process callback) + 1 HIGH (parity test masks the path) + 4 MEDIUM/LOW. Status → in-progress. | Jérôme Piquot |
| 2026-06-19 | 1.2 | Addressed all Senior Developer Review follow-ups: protected gateway admission marker, real dispatch-to-SDK round-trip test, SDK accepted-outcome reconciliation, reason-code constants, and whitespace-insensitive anti-regrowth checks. Status → review. | GPT-5 Codex |
| 2026-06-19 | 1.3 | Senior Developer Review (AI) re-review (story-automator auto-fix): verified all prior follow-ups resolved; 0 CRITICAL remain → status "done". Auto-fixed 1 new MEDIUM (DataProtection key-ring sharing: `SetApplicationName` + documented multi-replica requirement) and documented the duplicate-replay posture divergence in code. Build 0 errors; Server 1677 / Architecture 49 / Contracts 483 passing. | Jérôme Piquot |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-19 · **Outcome:** Changes Requested (1 CRITICAL)

### Summary

The host reduction itself is clean and meets the structural ACs: `Program.cs` is ~67 lines on the SDK shape (`AddEventStoreDomainService` / `UseEventStoreDomainService`), the custom `/process` plumbing is deleted, manual `/query` and `/project` maps are gone, compatibility routes are moved out of `Program.cs`, and the admission pipeline was extracted cleanly (`ChatBotCommandAdmissionPipeline`) and shared between `CommandGateway` and the new `ChatBotDomainServiceAdmissionStage`. The risk/approval rejection reason-code mapping to `CommandNotAllowlisted` is preserved (matches the prior HTTP behavior via `CommandGateway.Denied`). Architecture/anti-regrowth tests were added.

However, the migration introduces a **CRITICAL live-topology defect** that the test suite cannot observe because it stubs the EventStore gateway with a non-round-tripping fake. The SDK admission hook now runs on the EventStore→`/process` callback, but the gateway path (which all real callers use) still pre-admits and then dispatches into that same callback — so admission runs twice for every gateway-submitted command, and the second run fails. Story status reverted to **in-progress**.

### Findings

**🔴 CRITICAL — Double-admission breaks the live HTTP command path (AC2, AC3, AC4)**

- Evidence chain (all confirmed):
  - Real callers POST to `/api/v1/commands` — generated client `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs:283`.
  - `ChatBotCompatibilityEndpointExtensions.MapChatBotCompatibilityEndpoints` routes that to `CommandGateway.SubmitAsync` → `admission.AdmitAsync` (admission #1, full HTTP `ClaimsPrincipal`) → `AcceptedCommandDispatcher.DispatchAsync` → `IEventStoreGatewayClient.SubmitCommandAsync`.
  - SubmitCommandAsync → EventStore `/api/v1/commands` controller → `AggregateActor` → `DaprDomainServiceInvoker.InvokeAsync` → DAPR-invokes the ChatBot app's `/process` (Hexalith.EventStore submodule, verified by trace). `DomainServiceRequestRouter.ProcessAsync` runs every registered `IDomainServiceAdmissionStage` **unconditionally — no origin marker exists**.
  - So `ChatBotDomainServiceAdmissionStage.EvaluateAsync` runs again (admission #2) on a `CommandEnvelope` whose `Extensions` were built by `AcceptedCommandDispatcher.BuildExtensions` (`AcceptedCommandDispatcher.cs:1104`) — which sets only `surfaceOrigin`, `decidedAt`, `actorType`, `taskId`. `PrincipalFromEnvelope` therefore reconstructs a principal **missing** tenant-role/project-owner/participant-authority/service-client-grant claims → `IAuthorizationStage` denies. If authorization happens to pass, the pipeline reaches `IIdempotencyStore.RecordAdmissionAsync`, which for an in-flight same-equivalence duplicate **awaits the prior outcome** (`InMemoryCoarseIdempotencyStore.cs:49`) that admission #1 cannot record until its dispatch returns → request-scoped deadlock.
  - Net effect in the live DAPR topology: every command submitted through the gateway either is rejected at `/process` (no events persisted; surfaced to the caller as success-with-no-effect or dispatch-unavailable) or deadlocks until timeout. This violates AC2 ("accepted commands proceed to the SDK processor exactly once"), AC3 (fail-closed, no double side-effects / double pre-commit audit), and AC4 ("existing caller behavior is preserved … behavior-parity tests").
- Why tests are green: `ServerBootstrapApiTests.AuthenticatedFactory` substitutes `AcceptingEventStoreGatewayClient` (`ServerBootstrapApiTests.cs:3083`), which returns success **without** invoking `/process`. The double-admission path is never executed in any test.
- Not auto-fixed: the correct remedies are architectural and out of safe review-time scope — (a) demote `CommandGateway` to a thin translator so `/process` is the single admission point (requires carrying trusted server-derived authority into the envelope and relocating post-commit audit / operation-status), or (b) add a trusted EventStore-internal origin marker so the stage short-circuits already-admitted dispatch (needs an EventStore SDK capability → submodule approval per AC6). A forgeable "skip-admission" extension flag is explicitly disallowed by the story's "do not trust arbitrary extension keys as authority" guardrail.

**🟡 HIGH — Behavior-parity tests do not exercise the real dispatch→`/process` round trip (AC4)**

The moved gateway behavior is only validated against a fake that short-circuits EventStore. AC4 requires behavior-parity tests for the relocated behavior; the current suite cannot detect the CRITICAL above. A round-trip (or live integration) test is required.

**🟡 MEDIUM — Idempotency outcome recorded before the SDK processor runs**

`ChatBotDomainServiceAdmissionStage.RecordSdkAcceptedOutcomeAsync` records the coarse-idempotency outcome (hardcoded `LifecycleState.Proposed`) on admission-accept, before the keyed `IDomainProcessor` executes. The HTTP gateway records only after a successful dispatch (`CommandGateway.cs:123`). A processor failure after accept would leave a false "accepted" outcome that a retry replays instead of re-processing — a fail-closed regression on the canonical path.

**🟢 LOW — Ad-hoc rejection reason codes**

`"duplicate_replay_prior_outcome"` and `"invalid_command_payload"` (`ChatBotDomainServiceAdmissionStage.cs:46,86`) are string literals outside the `ChatBotAuthorizationReasonCodes` catalog. Promote to named constants to keep wire reason codes finite and auditable.

**🟢 LOW — Duplicate-replay posture divergence**

HTTP gateway returns idempotent success (`AcceptedResult(priorOutcome)`); `/process` returns `IsRejection == true`. Permitted by AC3 but should be documented as a contract so callers handle both surfaces.

**🟢 LOW — Whitespace-brittle anti-regrowth assertions**

`ShouldNotContain("MapPost(\n    \"/query\"")` / `"MapPost(\n    \"/project\"")` depend on exact formatting and would pass silently after a reformat; rely on the whitespace-insensitive `ShouldNotContain("\"/query\"")` form instead.

### Git vs Story File List

No discrepancies — every file in the Dev Agent Record File List matches `git status`. (`_bmad-output/implementation-artifacts/tests/test-summary.md` and the `orchestration-*.md` changes are pre-existing/unrelated per the story's Git Intelligence and are out of review scope.)

### Verification

- Build/test results recorded by dev (1668/49/483 passing) are credible for the in-process suite but do **not** cover the CRITICAL path (stubbed gateway). No new tests were run by this review.

_Reviewer: Jérôme Piquot on 2026-06-19_

## Senior Developer Review (AI) — Re-review

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-19 (story-automator auto-fix pass) · **Outcome:** Approve (0 CRITICAL)

### Summary

Re-ran the adversarial review against the actual implementation after the dev addressed the prior follow-ups. **All six prior follow-ups are genuinely resolved and independently verified** — not just claimed:

- **CRITICAL (double-admission) — FIXED.** `ChatBotDomainServiceAdmissionStage.EvaluateAsync` short-circuits to `Accepted()` only when `IChatBotAdmissionMarker.IsValid(envelope)` passes. The marker is an `IDataProtector`-protected (authenticated-encryption) token created in `AcceptedCommandDispatcher.BuildExtensions` (`AcceptedCommandDispatcher.cs:1126-1132`) and bound to the envelope identity (command id, tenant, domain, aggregate id, command type, correlation id). It is **not** a forgeable extension flag (satisfies the story guardrail and AC6 — no EventStore submodule edit). Gateway-submitted commands carry a valid marker, so the EventStore→`/process` callback skips re-admission; a non-gateway `/process` caller cannot forge one and gets full fail-closed admission.
- **HIGH (parity test) — FIXED.** `CommandEndpointShouldRoundTripThroughSdkProcessWithoutSecondAdmission` (`ServerBootstrapApiTests.cs:665`) routes `SubmitCommandAsync` back through the real `DomainServiceRequestRouter.ProcessAsync` (`RoundTrippingEventStoreGatewayClient`, `:3154`). It asserts `Accepted`, `PreCommit == 1`, `PostCommit == 1`, `idempotencyStore.RecordCount == 1` — all of which would fail (or deadlock/throw) if admission re-ran on the callback. This is the test the prior HIGH demanded.
- **MEDIUM (pre-processor idempotency outcome) — FIXED.** The stage no longer calls `RecordSdkAcceptedOutcomeAsync`; on a direct-`/process` accept it aborts the coarse-idempotency admission (no false "accepted" prior outcome). Asserted by `DomainServiceSdkHostAdoptionAdrTests` (`admissionStage.ShouldNotContain("RecordSdkAcceptedOutcomeAsync")`).
- **LOW (reason-code constants) — FIXED.** `DuplicateReplayPriorOutcome` / `InvalidCommandPayload` are now `ChatBotAuthorizationReasonCodes` constants.
- **LOW (whitespace-brittle assertions) — FIXED.** Anti-regrowth checks use a `WhitespaceInsensitive(...)` helper.
- **LOW (duplicate-replay posture) — DOCUMENTED** (a code comment was added in this pass at `ChatBotDomainServiceAdmissionStage.cs`).

### New finding (auto-fixed this pass)

**🟡 MEDIUM — DataProtection key ring must be shared for the admission marker across instances.** The marker (the entire CRITICAL fix) is created on the gateway instance and validated on whichever replica handles the EventStore→`/process` callback. `Program.cs` registered `AddDataProtection()` with no stable application name and no persisted/shared key ring; neither `ServiceDefaults` nor the SDK configures one. A multi-replica (or restarted) deployment would fail `Unprotect` on the second instance → admission re-runs → the original double-admission defect resurfaces. Single-instance dev/test and the in-process round-trip test share an in-process ring and mask this. **Auto-fixed:** added `SetApplicationName("Hexalith.ChatBot")` and an explicit documenting comment (`Program.cs`). **Residual:** a shared, persisted key store (Dapr/Redis/blob) must be wired before scaling beyond one replica — a deployment / Story 11.6 composition concern, intentionally not chosen here (AC6).

### Git vs Story File List

No discrepancies. The two files touched this pass (`Program.cs`, `ChatBotDomainServiceAdmissionStage.cs`) were already in the File List. `_bmad-output/**` story-automator artifacts (`test-summary.md`, `orchestration-*.md`) are pre-existing/out of review scope.

### Verification (run by this review)

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` → 0 Errors (1 pre-existing Tenants/StackExchange.Redis warning).
- xUnit v3 in-process runners (VSTest TCP socket blocked in sandbox): `Hexalith.ChatBot.Server.Tests` 1677 passed / 0 failed; `Hexalith.ChatBot.Architecture.Tests` 49 / 0; `Hexalith.ChatBot.Contracts.Tests` 483 / 0.
- `git diff --check` → clean.

_Reviewer: Jérôme Piquot on 2026-06-19_
