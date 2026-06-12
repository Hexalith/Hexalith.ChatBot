---
baseline_commit: 67dfd57bc16ef261601d061dda88c92a61ccfabe
---

# Story 11.4: Migrate projections, telemetry, and health to SDK contracts

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-12. -->

## Story

As a ChatBot maintainer,
I want projections on `IDomainProjectionHandler`, read models on `IReadModelStore` + `ReadModelWritePolicy`, and telemetry/health on the SDK helpers,
so that no per-domain projection, telemetry, or health plumbing is re-implemented in the domain.

## Acceptance Criteria

1. **Projection dispatch uses the DomainService SDK.** Given the current ChatBot projection handlers and subscription adapters, when Story 11.4 completes, then projection replay/dispatch is implemented through one or more `IDomainProjectionHandler` implementations discovered by `AddEventStoreDomainService(...)` and served by the SDK `/project` dispatcher. Existing DAPR-published ChatBot event deliveries remain behavior-compatible until Story 11.5/11.6 complete the host and topology reduction. [Source: _bmad-output/planning-artifacts/epics.md#Story 11.4; docs/adrs/domainservice-sdk-host-adoption.md#Bind future migrations to SDK contracts; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainProjectionHandler.cs]

2. **Projection behavior stays idempotent, order-tolerant, and tenant-partitioned.** Given duplicate, stale, out-of-order, cross-tenant, or unsupported published events, when the migrated projection path processes them, then existing last-writer-wins/source-version checks, metadata-only no-op acknowledgements, tenant-scoped keys, and safe projection outcomes remain unchanged, proven by the existing projection tests plus new SDK-dispatch parity coverage. [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationProjectionHandler.cs; src/Hexalith.ChatBot.Server/Projections/GovernedControlStateProjectionHandler.cs; src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs; tests/Hexalith.ChatBot.Server.Tests/Projections]

3. **Read-model persistence uses the platform store and write policy.** Given persisted ChatBot read models currently write through DAPR-specific stores such as `DaprGovernedOperationViewStore`, `DaprGovernedControlStateProjectionStore`, `DaprAssociationProjectionStore`, and `DaprProjectConversationProjectionStore`, when they are migrated, then durable writes flow through `IReadModelStore` and `ReadModelWritePolicy` using the existing `chatbot-statestore` component, preserving current key shapes and optimistic/idempotent merge semantics. No ChatBot-local DAPR read-model wrapper may remain for read models that the SDK store can serve. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs; Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs; src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs#AddChatBotDaprStateStores]

4. **Telemetry and health use SDK conventions.** Given ChatBot currently registers `Hexalith.ChatBot` `ActivitySource`/`Meter` names through `Hexalith.ChatBot.ServiceDefaults` and exposes ChatBot health endpoints in `Program.cs`, when domain telemetry/health are migrated, then domain instrumentation is registered through `AddEventStoreDomainTelemetry("chatbot")` and DAPR state-store health through `AddEventStoreDomainStateStoreHealthCheck("chatbot", stateStoreName: "chatbot-statestore", ...)`, while existing public `/health`, `/alive`, `/health/chatbot`, `/health/chatbot/workflows`, and `/health/chatbot/periodic-enforcement` behavior remains functionally equivalent for current operators/tests. [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainTelemetry.cs; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DaprStateStoreHealthCheck.cs; src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs; src/Hexalith.ChatBot.Server/Program.cs]

5. **Scope stays inside Story 11.4.** Given Story 11.4 is the projection/read-model/telemetry/health migration, when implementation completes, then it does not reduce `Program.cs` to the final 2-line host, does not register the CommandGateway as the SDK admission chain, does not retire `AppHost`/`Aspire`/`ServiceDefaults`, and does not redo Story 11.3 query/cursor work. Those are already completed or owned by Stories 11.5/11.6. [Source: docs/adrs/domainservice-sdk-host-adoption.md#Gate and sequence dependent work; _bmad-output/implementation-artifacts/11-3-migrate-chatbot-query-endpoints-to-idomainqueryhandler-and-iquerycursorcodec.md#Acceptance Criteria]

## Tasks / Subtasks

- [x] Inventory and classify current projection routes and handlers (AC: 1, 2)
  - [x] Read all `src/Hexalith.ChatBot.Server/Projections/*ProjectionEndpoints.cs` files currently mapped from `Program.cs`: governed operations/control state, mailbox intake, association, participant resolution, AI outcome, task intent, and approval.
  - [x] Map each endpoint route to its translator, handler, stores touched, and tests that prove duplicate/stale/out-of-order behavior.
  - [x] Decide which behavior becomes SDK `/project` full-replay dispatch and which event-subscription adapter must remain temporarily for public DAPR pub/sub compatibility until Story 11.5/11.6.
  - [x] Keep unsupported events acknowledged as metadata-only no-ops so DAPR at-least-once delivery does not loop.

- [x] Add SDK projection handlers under the existing Server projection area (AC: 1, 2)
  - [x] Implement `IDomainProjectionHandler` classes for ChatBot projection domains using `Domain => "chatbot"` unless the SDK request domain model requires a narrower domain token already emitted by EventStore.
  - [x] Use `ProjectionRequest.TenantId`, `AggregateId`, and `Events` from the SDK contract; do not read tenant, source version, event type, payload, or correlation from caller-supplied untrusted fields when verified EventStore metadata is available.
  - [x] Return `ProjectionResponse` with the existing projection type/state shape expected by EventStore's projection actor and current tests.
  - [x] Register/discover handlers through the existing `builder.AddEventStoreDomainService(typeof(GovernedOperationAggregate).Assembly)` path; do not manually build a parallel projection dispatcher in ChatBot.

- [x] Migrate durable read-model stores to `IReadModelStore` (AC: 2, 3)
  - [x] Register the platform DAPR read-model store through the SDK registration extension and keep the `chatbot-statestore` component name stable.
  - [x] Replace ChatBot-local DAPR get/save wrappers for governed operation views, governed control state views, association views, project conversation indexes/items, and operation status where the SDK store can serve the same keys.
  - [x] Use `ReadModelWritePolicy.UpdateAsync`, `ApplyEventsAsync`, or `MergeAsync` for read-modify-write paths so ETag conflicts retry instead of losing concurrent updates.
  - [x] Preserve existing key builders such as `GovernedOperationView.KeyFor(...)`, `AssociationCandidateView.KeyFor(...)`, `GovernedControlStateView.KeyFor(...)`, and project-conversation index keys. Do not introduce global or cross-tenant keys.
  - [x] Preserve source-version ordering rules: duplicate and lower/equal versions are ignored unless the existing handler intentionally allows equal-version correction-complete overlays.

- [x] Replace ChatBot-specific domain telemetry/health plumbing with SDK helpers (AC: 4)
  - [x] Call `builder.AddEventStoreDomainTelemetry("chatbot")` after domain-service registration so SDK convention names are registered with OpenTelemetry.
  - [x] Add `AddEventStoreDomainStateStoreHealthCheck("chatbot", stateStoreName: "chatbot-statestore", tags: ...)` on the health-check builder used by the Server host.
  - [x] Keep `IChatBotMetrics`/`ChatBotMetrics` only for product/business metrics that are not covered by the SDK domain telemetry convention; do not keep a duplicate per-domain `ActivitySource`/`Meter` solely for host plumbing.
  - [x] Preserve the current operator-facing health routes and status payloads until Story 11.5 decides whether the SDK host fully owns route mapping.

- [x] Remove or narrow hand-rolled projection/health code without breaking public adapters (AC: 1, 4, 5)
  - [x] Remove `Map*ProjectionEndpoints(...)` route mappings from `Program.cs` only after their behavior is covered by SDK `/project` dispatch or retained as thin compatibility subscription adapters.
  - [x] Keep `/api/v1/commands`, `/process`, Story 11.3's `/query` compatibility path, CloudEvents, subscription handler, auth, correlation, workflow health, and periodic-enforcement health unchanged unless directly required for the 11.4 migration.
  - [x] Do not edit `src/Hexalith.ChatBot.AppHost`, `src/Hexalith.ChatBot.Aspire`, or `src/Hexalith.ChatBot.ServiceDefaults` to retire them; Story 11.6 owns that cleanup.
  - [x] Do not modify `Hexalith.EventStore/src/**` unless a missing SDK capability is proven and explicit submodule approval is obtained.

- [x] Add parity, SDK-discovery, and anti-regrowth tests (AC: 1-5)
  - [x] Extend `tests/Hexalith.ChatBot.Server.Tests/Projections/*` to exercise migrated projection handlers through `DomainProjectionDispatcher.Project(...)` or the SDK `/project` endpoint shape.
  - [x] Keep existing projection tests for governed operations, control state, association, participant resolution, AI outcome, task intent, approval, and project conversation read models green.
  - [x] Add read-model store tests using an in-memory `IReadModelStore` or SDK testing fake to prove `ReadModelWritePolicy` conflict/idempotency behavior on ChatBot keys.
  - [x] Extend `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` to prove health endpoints and SDK telemetry/health registrations are present without losing existing status payloads.
  - [x] Extend `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs` or a focused architecture test to prevent regrowth of ChatBot-local DAPR read-model wrappers and per-domain telemetry/health classes after migration.

- [x] Run focused verification (AC: 1-5)
  - [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`
  - [x] `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 -nodeReuse:false` or, if VSTest socket setup is blocked in this sandbox, build the project and run its xUnit v3 in-process runner directly.
  - [x] `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1 -nodeReuse:false` or the xUnit v3 in-process runner equivalent.
  - [x] `dotnet test tests/Hexalith.ChatBot.ServiceDefaults.Tests/Hexalith.ChatBot.ServiceDefaults.Tests.csproj --no-restore -m:1 -nodeReuse:false` if `Hexalith.ChatBot.ServiceDefaults` changes.
  - [x] `git diff --check`

## Dev Notes

### Discovery Results

- Loaded `sprint_status` from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key is `11-4-migrate-projections-telemetry-and-health-to-sdk-contracts`, currently `backlog`; `epic-11` is already `in-progress`; Stories 11.1, 11.2, and 11.3 are `done`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`. Story 11.4 is the projection/read-model/telemetry/health migration in Epic 11 and is parallelizable with Story 11.3 after Story 11.2.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`. D8 binds ChatBot to `Hexalith.EventStore.DomainService`, `IDomainProjectionHandler`, `IReadModelStore` + `ReadModelWritePolicy`, and SDK telemetry/health helpers.
- Loaded planning context from `_bmad-output/planning-artifacts/index.md`, `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md`, and `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-09-pass-2.md`.
- Loaded the accepted ADR at `docs/adrs/domainservice-sdk-host-adoption.md`. It gates Stories 11.2-11.6 and explicitly makes Story 11.4 responsible for projection/read-model/telemetry/health migration.
- Loaded previous story files `11-1-host-reuse-adr-domainservice-sdk-adoption-decision-record.md`, `11-2-platform-pre-commit-admission-hook-in-the-domainservice-sdk.md`, and `11-3-migrate-chatbot-query-endpoints-to-idomainqueryhandler-and-iquerycursorcodec.md`.
- Loaded persistent project-context facts from sibling `_bmad-output/project-context.md` files, especially `Hexalith.EventStore/_bmad-output/project-context.md`: .NET 10, warnings-as-errors, central package versions, xUnit v3 + Shouldly, `ConfigureAwait(false)`, `.slnx` only, and root-level-only submodule handling.
- No direct PRD or UX files matched the configured create-story input patterns in `_bmad-output/planning-artifacts`; this backend/platform migration must preserve existing UI/client/API behavior but introduces no new screen.

### Epic 11 Context

Epic 11 closes readiness pass-2 Issue #1: ChatBot had a hand-rolled Server host, module-owned hosting projects, and no DomainService SDK contract usage. Story 11.3 has already introduced `AddEventStoreDomainService(...)`, `IDomainQueryHandler`, and `IQueryCursorCodec` into ChatBot. Story 11.4 is the next surface migration: move projections/read models/telemetry/health to SDK contracts without performing the final host reduction. [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-09-pass-2.md#Step 5; _bmad-output/implementation-artifacts/11-3-migrate-chatbot-query-endpoints-to-idomainqueryhandler-and-iquerycursorcodec.md]

Binding sequence:

- 11.1: accepted ADR at `docs/adrs/domainservice-sdk-host-adoption.md` - done.
- 11.2: platform pre-commit admission hook in `Hexalith.EventStore.DomainService` - done.
- 11.3: query endpoints to `IDomainQueryHandler` and `IQueryCursorCodec` - done.
- 11.4: projections/read models/telemetry/health to SDK contracts - this story.
- 11.5: reduce Server host to SDK shape and register the CommandGateway admission hook.
- 11.6: retire or sharply reduce module-owned `AppHost`/`Aspire`/`ServiceDefaults`.

Do not pull 11.5/11.6 into this story. `Program.cs` may still keep compatibility adapters after 11.4, but projection/read-model/telemetry/health implementation should no longer be ChatBot-specific host plumbing where the SDK provides the contract.

### Current State to Modify

`src/Hexalith.ChatBot.Server/Program.cs` currently:

- Calls `builder.AddEventStoreDomainService(typeof(GovernedOperationAggregate).Assembly)` for SDK query/projection handler discovery.
- Calls `builder.Services.AddChatBotCommandGateway()` and conditionally `AddChatBotDaprStateStores()`.
- Maps default endpoints, `/health/chatbot`, `/health/chatbot/workflows`, and `/health/chatbot/periodic-enforcement`.
- Maps local `/query` through `DomainQueryDispatcher.ExecuteAsync(...)` from Story 11.3.
- Maps projection subscriber endpoints with `MapGovernedOperationProjectionEndpoints`, `MapMailboxIntakeProjectionEndpoints`, `MapAssociationProjectionEndpoints`, `MapParticipantResolutionProjectionEndpoints`, `MapAiOutcomeProjectionEndpoints`, `MapTaskIntentProjectionEndpoints`, and `MapApprovalProjectionEndpoints`.

Current endpoint adapters under `src/Hexalith.ChatBot.Server/Projections/*ProjectionEndpoints.cs` accept DAPR-published event envelopes from `chatbot-pubsub` and call local handlers. They use `WithTopic(...)`, so removal without an equivalent subscription path would break the live event delivery topology. Migrate deliberately: SDK `/project` dispatch must be introduced without losing DAPR pub/sub compatibility required by the current AppHost until 11.5/11.6. [Source: src/Hexalith.ChatBot.Server/Program.cs; src/Hexalith.ChatBot.Server/Projections/GovernedOperationProjectionEndpoints.cs; src/Hexalith.ChatBot.Server/Projections/AssociationProjectionEndpoints.cs; src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionEndpoints.cs]

Current production read-model stores are direct DAPR wrappers over `chatbot-statestore`, registered only when `ChatBot:UseDaprStateStores=true`:

- `DaprGovernedOperationViewStore : IGovernedOperationProjectionStore`
- `DaprGovernedControlStateProjectionStore : IGovernedControlStateProjectionStore`
- `DaprAssociationProjectionStore : IAssociationProjectionStore`
- `DaprProjectConversationProjectionStore : IProjectConversationProjectionStore`
- `DaprOperationStatusStore : IOperationStatusStore`

These wrappers mostly perform direct `GetStateAsync`/`SaveStateAsync` calls. Story 11.4 should move durable read-model persistence to the SDK's `IReadModelStore`; use `ReadModelWritePolicy` for read-modify-write paths so concurrent updates do not regress to lost updates. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs#AddChatBotDaprStateStores; src/Hexalith.ChatBot.Server/Projections/DaprGovernedOperationViewStore.cs; src/Hexalith.ChatBot.Server/Projections/DaprAssociationProjectionStore.cs; src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs]

### SDK Projection, Read-Model, Telemetry, and Health Contracts

`IDomainProjectionHandler` has two members: `Domain` and `ProjectionResponse Project(ProjectionRequest request)`. `AddEventStoreDomainService(...)` discovers implementations from the supplied domain assembly and registers them. `DomainProjectionDispatcher.Project(...)` selects the handler by case-insensitive domain match, and the SDK maps `POST /project` unless a route is already mapped. The SDK projection handler is stateless full-replay: it receives a `ProjectionRequest` and returns a rebuilt `ProjectionResponse`; persisted multi-key read models are separate SDK `IReadModelStore` work, not hidden inside this interface. [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainProjectionHandler.cs; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DomainProjectionDispatcher.cs; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs]

`IReadModelStore` exposes `GetAsync`, `SaveAsync`, and ETag-aware `TrySaveAsync`. `ReadModelWritePolicy` provides `UpdateAsync`, `ApplyEventsAsync`, and `MergeAsync`, with a default retry budget of 3 and conflict/exhaustion diagnostics. The update/merge delegate must be idempotent because it can run more than once on conflict retry. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs; Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs]

`AddEventStoreDomainTelemetry("chatbot")` registers an `EventStoreDomainDiagnostics` singleton and wires convention-named source/meter names (`Hexalith.EventStore.Domain.chatbot`) into OpenTelemetry. `AddEventStoreDomainStateStoreHealthCheck("chatbot", stateStoreName: "chatbot-statestore")` registers a DAPR state-store health probe named by SDK convention (`dapr-statestore-chatbot`). [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainTelemetry.cs; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DaprStateStoreHealthCheck.cs]

### Behavior That Must Be Preserved

- DAPR pub/sub delivery is at-least-once and unordered. Duplicate and stale projection events must remain idempotent no-ops.
- Tenant isolation remains key-based and handler-enforced. Never let a projected event update another tenant's key or global state.
- Existing source-version ordering remains intact. Do not downgrade a completed correction or higher-version control state with an older event.
- Metadata-only posture remains intact: do not log or expose raw mailbox bodies, provider payloads, command bodies, bearer tokens, tenant-secret material, stack traces, or raw projection payload text.
- `GovernedControlStateView` overlays control-state and rate-limit dimensions independently; a rate-limit event must not reactivate a disabled/quarantined subject, and a control-state event must not wipe the budget.
- Project conversation enrichment and indexes must continue to materialize source email, participant, and attachment context as current stores do.
- Audit projection lag and completeness sources must keep honest unavailable/unknown behavior; do not fabricate healthy readings during the migration.
- Existing health endpoint payloads and status codes used by tests/operators remain compatible until the final SDK host reduction story explicitly changes ownership.

### Previous Story Intelligence

Story 11.1 established the accepted ADR and the only retained hand-rolled exception: a thin local-development umbrella AppHost, not a production host bypass. Story 11.4 must continue moving host-owned infrastructure to SDK contracts and should not add new permanent exceptions. [Source: _bmad-output/implementation-artifacts/11-1-host-reuse-adr-domainservice-sdk-adoption-decision-record.md]

Story 11.2 added the platform pre-commit admission hook and left ChatBot consumption for Story 11.5. The SDK endpoint set remained canonical: `/process`, `/replay-state`, `/query`, `/project`, and `/admin/operational-index-metadata`. Story 11.4 should normally avoid EventStore source changes because the projection/read-model/telemetry/health contracts already exist locally. [Source: _bmad-output/implementation-artifacts/11-2-platform-pre-commit-admission-hook-in-the-domainservice-sdk.md; Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs]

Story 11.3 already migrated read/query routes to `IDomainQueryHandler` and project-conversation cursors to `IQueryCursorCodec`. It explicitly left projection endpoint classes, telemetry, health, final host reduction, CommandGateway admission registration, and AppHost/Aspire/ServiceDefaults retirement out of scope for Story 11.4-11.6. Do not recreate the removed local cursor codec or move read/query logic back into `Program.cs`. [Source: _bmad-output/implementation-artifacts/11-3-migrate-chatbot-query-endpoints-to-idomainqueryhandler-and-iquerycursorcodec.md]

Story 8.7a/8.7b created the durable control-state/rate-limit projection and the periodic enforcement runtime. Relevant follow-ups for 11.4: preserve `GovernedControlStateView` freshness behavior, keep mailbox projection gaps visible rather than fabricated healthy, and use `ReadModelWritePolicy` to improve DAPR read-modify-write paths where concurrent admits/projection updates could otherwise lose data. [Source: _bmad-output/implementation-artifacts/8-7a-durable-control-state-rate-limit-projection-and-enforcement-seam-activation.md#Review Follow-ups; _bmad-output/implementation-artifacts/8-7b-periodic-enforcement-trigger-and-deferred-evaluator-consolidation.md#Review Follow-ups]

### Git Intelligence

Recent relevant commits:

- `67dfd57 feat(story-11.3): Migrate ChatBot query endpoints to IDomainQueryHandler + IQueryCursorCodec`
- `ad4d11c feat(story-11.2): Platform pre-commit admission hook in DomainService SDK`
- `d2505e5 feat(story-11.1): Host-reuse ADR DomainService SDK adoption decision record`
- `8607d52 feat(story-8.7b): Periodic enforcement trigger and deferred evaluator consolidation`

Actionable relevance: recent work used narrow migrations, compatibility adapters, parity tests, and anti-regrowth tests. Continue that pattern for projections/read models/telemetry/health instead of broad host rewrites.

### Project Structure Notes

Likely implementation locations:

- `src/Hexalith.ChatBot.Server/Program.cs` - reduce projection/health/telemetry hand wiring only where SDK contracts take over; keep compatibility adapters and routes that are still required before 11.5.
- `src/Hexalith.ChatBot.Server/Projections/*.cs` - add SDK projection handlers and migrate stores without moving projection domain logic out of the projection area.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` - register SDK `IReadModelStore`, replace DAPR read-model registrations, and keep runtime composition centralized.
- `src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs` - retain only product/business metrics; avoid duplicate per-domain host telemetry when SDK diagnostics cover the same role.
- `src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs` - change only if necessary to remove duplicate ChatBot-specific domain source/meter registration while preserving service-default behavior.
- `tests/Hexalith.ChatBot.Server.Tests/Projections/*` - projection behavior parity and read-model write policy tests.
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - SDK discovery, health, and route compatibility tests.
- `tests/Hexalith.ChatBot.Architecture.Tests/*` - anti-regrowth tests for local projection/health/telemetry plumbing.

Avoid changing these unless a test proves it is necessary:

- `Hexalith.EventStore/src/**` - platform contracts already exist; any SDK source edit requires explicit submodule approval.
- `src/Hexalith.ChatBot.AppHost`, `src/Hexalith.ChatBot.Aspire`, `src/Hexalith.ChatBot.ServiceDefaults` retirement work - Story 11.6 owns composition cleanup.
- CommandGateway admission registration and final `/process` ownership - Story 11.5 owns SDK admission-chain consumption.
- Story 11.3 query/cursor handlers under `src/Hexalith.ChatBot.Server/Queries/*` unless a projection test reveals a direct integration defect.

### Testing Standards

- Use xUnit v3 + Shouldly. Avoid raw `Assert.*`.
- Keep tests in existing projects and styles. Use `WebApplicationFactory<Program>` for HTTP/health compatibility and direct SDK dispatcher tests for projection handler behavior.
- Add `ConfigureAwait(false)` on every awaited call in production code.
- Do not add package versions to project files; versions belong in `Directory.Packages.props`.
- Do not initialize nested submodules or use recursive submodule commands.
- VSTest may fail in this sandbox because it opens a TCP listener. If that happens, build the test project and run the xUnit v3 executable from `bin/<Configuration>/net10.0/` directly, recording the limitation.

### Latest Technical Information

No external web research was used. The relevant technical truth is the checked-out `Hexalith.EventStore` SDK source and local planning artifacts. The SDK contracts named above are present locally as of baseline commit `67dfd57bc16ef261601d061dda88c92a61ccfabe`.

### Validation Notes

Checklist pass completed against `.agents/skills/bmad-create-story/checklist.md`:

- Reinvention prevention: story directs reuse of SDK `IDomainProjectionHandler`, `IReadModelStore`, `ReadModelWritePolicy`, `AddEventStoreDomainTelemetry`, and `AddEventStoreDomainStateStoreHealthCheck` rather than new ChatBot-local host plumbing.
- Wrong-location prevention: projection code stays under `.Server/Projections`, runtime composition stays centralized, and final host/AppHost retirement stays out of scope.
- Regression prevention: story preserves DAPR pub/sub compatibility, public health endpoints, source-version idempotency, tenant-partitioned keys, metadata-only logging, no-fabricated health/lag posture, and Story 11.3 query/cursor results.
- Critical gap called out: SDK `/project` full-replay dispatch and current DAPR pub/sub subscriber routes are not identical surfaces; implementation must keep compatibility adapters until 11.5/11.6 complete topology changes.
- LLM optimization: tasks are grouped by concrete code areas and acceptance criteria, with explicit files to read and files to avoid.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml]
- [Source: _bmad-output/implementation-artifacts/11-1-host-reuse-adr-domainservice-sdk-adoption-decision-record.md]
- [Source: _bmad-output/implementation-artifacts/11-2-platform-pre-commit-admission-hook-in-the-domainservice-sdk.md]
- [Source: _bmad-output/implementation-artifacts/11-3-migrate-chatbot-query-endpoints-to-idomainqueryhandler-and-iquerycursorcodec.md]
- [Source: _bmad-output/implementation-artifacts/8-7a-durable-control-state-rate-limit-projection-and-enforcement-seam-activation.md]
- [Source: _bmad-output/implementation-artifacts/8-7b-periodic-enforcement-trigger-and-deferred-evaluator-consolidation.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 11: Minimal Technical Layer - DomainService SDK Host Adoption]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 11.4: Migrate projections, telemetry, and health to SDK contracts]
- [Source: _bmad-output/planning-artifacts/architecture.md#Host-Layer Reuse (D8)]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-09-pass-2.md]
- [Source: docs/adrs/domainservice-sdk-host-adoption.md]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/IDomainProjectionHandler.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DomainProjectionDispatcher.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainServiceExtensions.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/EventStoreDomainTelemetry.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.DomainService/DaprStateStoreHealthCheck.cs]
- [Source: src/Hexalith.ChatBot.Server/Program.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationProjectionEndpoints.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationProjectionEndpoints.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionEndpoints.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/DaprGovernedOperationViewStore.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/DaprAssociationProjectionStore.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs]
- [Source: src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-12T11:07:21+02:00 - Created Story 11.4 context artifact from BMAD create-story workflow inputs.
- Validation check: story includes required sections, Story 11.4 ACs, SDK projection/read-model/telemetry/health contract guidance, current code inventory, prior story intelligence, scope boundaries, project structure notes, and testing requirements.
- Validation check: sprint status updated to `ready-for-dev` for `11-4-migrate-projections-telemetry-and-health-to-sdk-contracts`.
- 2026-06-12T11:13:42+02:00 - BMAD dev-story workflow activated; story and sprint status moved to `in-progress` with existing `baseline_commit` preserved.
- 2026-06-12T11:25:11+02:00 - Implemented SDK projection handler, SDK read-model stores, telemetry/health registration, and focused parity/anti-regrowth tests.
- Verification: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` passed.
- Verification: `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 -nodeReuse:false` was blocked by VSTest socket permissions; xUnit v3 in-process runner `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` passed 1661/1661.
- Verification: xUnit v3 in-process runner `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` passed 48/48.
- Verification: xUnit v3 in-process runner `tests/Hexalith.ChatBot.ServiceDefaults.Tests/bin/Debug/net10.0/Hexalith.ChatBot.ServiceDefaults.Tests` passed 5/5.
- Verification: `git diff --check` passed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Converted Epic 11.4 planning text into implementation-ready acceptance criteria and tasks with concrete SDK contracts and ChatBot file targets.
- Added explicit warning that SDK `/project` dispatch and current DAPR pub/sub subscriber routes are different runtime surfaces; implementation must preserve compatibility until later Epic 11 topology stories.
- Added anti-regrowth, behavior-parity, and verification guidance aligned with Story 11.3 patterns.
- Added `ChatBotDomainProjectionHandler` as the SDK-discovered `IDomainProjectionHandler` for `chatbot`, reusing existing projection translators/handlers and keeping unsupported events as metadata-only no-op acknowledgements.
- Replaced ChatBot projection DAPR wrappers with SDK `IReadModelStore` adapters using `ReadModelWritePolicy`, while keeping stable `chatbot-statestore` keys and existing in-memory test defaults.
- Registered SDK domain telemetry and DAPR state-store health check helpers, preserving existing public health routes and retaining `ChatBotMetrics` for product/business metrics.
- Added server, projection-store, service-defaults, and architecture tests for SDK dispatch, read-model write-policy retry behavior, telemetry/health registration, and anti-regrowth.

### File List

- `_bmad-output/implementation-artifacts/11-4-migrate-projections-telemetry-and-health-to-sdk-contracts.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/DaprOperationStatusStore.cs` (deleted)
- `src/Hexalith.ChatBot.Server/Gateway/Status/ReadModelOperationStatusStore.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/ChatBotDomainProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/ChatBotReadModelStoreNames.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprAssociationProjectionStore.cs` (deleted)
- `src/Hexalith.ChatBot.Server/Projections/DaprGovernedControlStateProjectionStore.cs` (deleted)
- `src/Hexalith.ChatBot.Server/Projections/DaprGovernedOperationViewStore.cs` (deleted)
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs` (deleted)
- `src/Hexalith.ChatBot.Server/Projections/InMemoryGovernedOperationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ReadModelAssociationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ReadModelGovernedControlStateProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ReadModelGovernedOperationViewStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ReadModelProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.ServiceDefaults/Extensions.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/DomainServiceSdkHostAdoptionAdrTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/OperationStatusStoreRegistrationTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ReadModelProjectionStorePolicyTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.ServiceDefaults.Tests/ServiceDefaultsExtensionsTests.cs`

### Change Log

| Date | Version | Description | Author |
| --- | --- | --- | --- |
| 2026-06-12 | 0.1 | Created Story 11.4 developer context artifact and marked story ready for dev. | GPT-5 Codex |
| 2026-06-12 | 1.0 | Migrated projections, read-model persistence, telemetry, and state-store health to SDK contracts; added parity and anti-regrowth tests. | GPT-5 Codex |
| 2026-06-12 | 1.1 | Senior Developer Review (AI): auto-fixed control-state freshness-refresh downgrade and governed-operation lost-update under optimistic concurrency; extended anti-regrowth guard to Gateway/Status; added downgrade regression test. Status → done. | Claude Opus 4.8 |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (adversarial automated review) · **Date:** 2026-06-12 · **Outcome:** Approve (after auto-fix)

Scope reviewed: full File List against git reality (matches; the two extra `_bmad-output/` files are automation artifacts excluded from review), all five ACs, the new SDK projection handler, the five `ReadModel*` stores (incl. a method-by-method parity diff of the 1224-line deleted `DaprProjectConversationProjectionStore` vs its replacement — PARITY OK), Program.cs/CommandGateway/ServiceDefaults wiring, telemetry/health registration, and the four changed/added test files. Build clean (0 warnings, warnings-as-errors); Server 1666/1666, Architecture 48/48, ServiceDefaults 5/5; `git diff --check` clean.

Findings (all verified; HIGH/MEDIUM auto-fixed per request):

- **[MEDIUM — FIXED] Control-state freshness refresh could downgrade a newer record.** `ReadModelGovernedControlStateProjectionStore.TryRefreshFreshnessAsync` passed a `ReadModelWritePolicy.UpdateAsync` delegate whose else-branch returned the pre-loop `current` snapshot. If a concurrent control-state event advanced the persisted record between the outer read and the policy's retry-safe read, the delegate overwrote the newer `latest` with the stale `current` — a version downgrade that could reactivate a disabled/quarantined subject (violates AC2 + the "rate-limit must not reactivate a disabled subject" Dev Note, and defeats the story's stated reason for adopting `ReadModelWritePolicy`). Fix: yield to `latest ?? current` so a diverged concurrent update is preserved. Added regression test `ControlStateFreshnessRefreshShouldNotDowngradeConcurrentlyAdvancedRecord`.
- **[MEDIUM — FIXED] Governed-operation read-model write lost updates under concurrency.** `ReadModelGovernedOperationViewStore.SaveAsync` used `_ => view`, ignoring the loaded value, so an ETag-conflict retry re-applied the stale `view` over a newer concurrent write — inconsistent with the sibling association/control-state stores, which guard with `current.SourceVersion > view.SourceVersion ? current : view`. Fix: added the same source-version guard (handler already drops `>=`, so equal-version overlay is unaffected). Existing retry test still green.
- **[LOW — FIXED] Anti-regrowth test missed the `Gateway/Status` location.** `StoryElevenFour_...` only scanned `src/.../Projections` for `Dapr*ProjectionStore.cs`/`Dapr*ViewStore.cs`; the deleted `DaprOperationStatusStore` lived in `src/.../Gateway/Status`, so a DAPR status-store wrapper could regrow there untracked. Fix: extended the test to also assert no `Dapr*Store.cs` under `Gateway/Status`.
- **[LOW — noted, no fix] `ChatBotDomainProjectionHandler.Project` is sync-over-async** (`GetAwaiter().GetResult()`), blocking a thread per `/project` call across multiple state-store round-trips. Imposed by the synchronous `IDomainProjectionHandler.Project` SDK contract (changing it is a submodule edit, out of scope). The `/project` path is dormant until Stories 11.5/11.6 wire the SDK host; acceptable for now.
- **[LOW — noted, no fix] `/project` unified dispatch is first-match-wins** vs the live DAPR topology's all-subscriptions-process model. Verified the seven handlers gate on distinct event types (translator-gated handlers run first; the task-intent/approval/ai-outcome trio gate internally and run last), and no concrete overlapping event type was found — but no parity test asserts cross-handler non-overlap. The path is dormant and idempotent. Recommend a future cross-handler `/project` parity test (carry into 11.5).

Non-issues confirmed during review (documented to prevent re-flagging): read-model value/key serialization parity holds (both old and new stores serialize through the same DAPR client/STJ); removing `AddSource("Hexalith.ChatBot")` is safe (no code emits to that ActivitySource; the SDK domain source replaces it); `/health` is a static string in `MapDefaultEndpoints`, so the newly-registered state-store health check is inert-but-registered by design (route ownership deferred to 11.5) and does not regress `/health`; and `MapChatBotDomainServiceEndpoints` maps only `/process`, so the manual `/project` map does not double-register against the SDK's `MapEventStoreDomainService`.
