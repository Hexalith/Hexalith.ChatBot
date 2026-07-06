# ADR: DomainService SDK host adoption for ChatBot

## Status

Accepted (2026-06-11, Story 11.1).

## Context

Readiness pass-2 Issue #1 identified host-layer drift between ChatBot and the platform domain-service model. ChatBot
currently owns a hand-rolled Server host instead of using the `Hexalith.EventStore.DomainService` SDK host layer as
the target domain-module foundation.

At the time this decision is recorded:

- ChatBot does not use the target DomainService SDK contracts in its Server host.
- `src/Hexalith.ChatBot.Server/Program.cs` is 1250 lines in the current working tree. The readiness report cited 1221
  lines when it was produced; the host has continued to grow since then.
- ChatBot still ships module-owned `src/Hexalith.ChatBot.AppHost`, `src/Hexalith.ChatBot.Aspire`, and
  `src/Hexalith.ChatBot.ServiceDefaults`.
- The current host owns custom command gateway, authorization, correlation, DAPR/cloud-events, projection
  subscription, health, workflow, periodic enforcement, domain-service endpoint, and inline query-route wiring.

This is not a product-scope change and introduces no new FRs or NFRs. It is platform-conformance work that preserves
the existing FR81a CommandGateway admission invariant while moving host boilerplate to the platform SDK.

## Decision

### Adopt the DomainService SDK as the target host layer

ChatBot adopts `Hexalith.EventStore.DomainService` as the target host layer. The current hand-rolled Server host is
explicitly rejected as the default direction and is not recorded as a permanent exception.

ChatBot's target Server host is `AddEventStoreDomainService(...)` plus admission-chain registration plus
`UseEventStoreDomainService()`. The Server host should contain ChatBot domain composition and the FR81a admission-chain
mount, not a parallel reimplementation of platform hosting boilerplate.

The canonical DomainService endpoints must remain available through the platform SDK shape:

- `POST /process`
- `POST /replay-state`
- `POST /query`
- `POST /project`
- `POST /admin/operational-index-metadata`

### Preserve FR81a through a platform pre-commit admission hook

The FR81a CommandGateway admission chain becomes a platform SDK pre-commit admission hook owned by Story 11.2. It is
not a ChatBot-specific bypass, not a second command pipeline, and not a weakening of the existing CommandGateway spine.

The hook must preserve fail-closed admission, the existing stage-order invariant, and the governance interfaces that
prevent command execution before admission succeeds. If the SDK lacks a capability needed by ChatBot admission,
the capability is added to `Hexalith.EventStore.DomainService` rather than bypassed in ChatBot.

### Bind future migrations to SDK contracts

Future ChatBot host migration work is bound to these SDK contracts and composition points:

- `AddEventStoreDomainService()` and `UseEventStoreDomainService()` as the Server host foundation.
- `IDomainQueryHandler` for query endpoints.
- `IDomainProjectionHandler` for projection replay/dispatch.
- `IReadModelStore` and `ReadModelWritePolicy` for read-model persistence and optimistic-concurrency writes.
- `IQueryCursorCodec` and `QueryCursorScope` for protected, scope-bound query cursors.
- `AddEventStoreDomainTelemetry` for domain telemetry.
- `AddEventStoreDomainStateStoreHealthCheck` for domain state-store health.
- `AddEventStoreDomainModule(...)` as the target composition path from the platform AppHost.

### Gate and sequence dependent work

Story 11.1 gates Stories 11.2-11.6. Stories 11.2-11.6 must not start before this ADR is accepted.

The migration order is:

`11.2 -> 11.3/11.4 -> 11.5 -> 11.6`

- 11.2 adds the platform pre-commit admission hook in `Hexalith.EventStore.DomainService`. This is platform work in the
  `Hexalith.EventStore` submodule and requires explicit submodule approval before any EventStore edit.
- 11.3 migrates ChatBot query endpoints to `IDomainQueryHandler` and `IQueryCursorCodec`.
- 11.4 migrates projections, telemetry, and health to SDK contracts.
- 11.5 reduces the ChatBot Server host to the SDK shape with the CommandGateway admission hook.
- 11.6 retires or sharply reduces module-owned `AppHost`/`Aspire`/`ServiceDefaults` through
  `AddEventStoreDomainModule(...)`.

Stories 11.3 and 11.4 are parallelizable after 11.2. Stories 11.5 and 11.6 land after Stories 8.7a and 8.7b so the host
migration does not chase a moving durable enforcement seam.

Story 11.1 is decision work only. It does not implement the platform hook, migrate endpoints or projections, reduce
`Program.cs`, or remove `AppHost`/`Aspire`/`ServiceDefaults`.

## Consequences

- ChatBot host reuse is a dated, accepted architecture decision instead of implicit drift.
- The platform SDK owns the reusable domain-host surface; ChatBot owns domain behavior and the admission-chain
  registration needed to preserve FR81a.
- Any migration pressure that reveals missing SDK capability becomes platform work, not ChatBot-specific host
  expansion.
- Architecture D8, Epic 11, and this ADR share the same migration order and scope boundaries.
- Story 11.6 exercised the exception boundary: standalone ChatBot `.Aspire` and `.ServiceDefaults` were retired, but
  a thin local-development AppHost remains because `AddEventStoreDomainModule(...)` does not yet express ChatBot's
  dedicated read-model, workflow, and pub/sub resources without an additional platform composition capability.

## Exception Boundary

The only allowed retained hand-rolled exception is a thin local-development umbrella AppHost if it is still needed to
run the multi-sibling ChatBot topology during the migration.

- Date: 2026-06-11.
- Justification: local developers may still need one umbrella entry point to compose ChatBot with EventStore, Tenants,
  FrontComposer, and other sibling modules until the platform AppHost can own the same topology through
  `AddEventStoreDomainModule(...)`.
- Owner: ChatBot platform architecture owner.
- Expected scope: local developer orchestration only. It is not a production domain-hosting pattern and must not keep
  the current full module-owned `AppHost`/`Aspire`/`ServiceDefaults` ownership alive by default.
- Retirement/review trigger: Story 11.6, or earlier if the platform AppHost can compose the ChatBot sibling topology
  without the umbrella AppHost.

Story 11.6 review outcome (2026-06-19):

- `src/Hexalith.ChatBot.Aspire` and `src/Hexalith.ChatBot.ServiceDefaults` were removed as standalone projects.
- `src/Hexalith.ChatBot.AppHost` remains as a local-development umbrella only; its Dapr component wiring is internal to
  the AppHost shim and is not a reusable domain-hosting package.
- The retained shim exists because the current platform `AddEventStoreDomainModule(...)` path supports shared
  EventStore resources or isolated zero-infrastructure resources, but not ChatBot's dedicated
  `chatbot-statestore`, `chatbot-workflow-statestore`, and `chatbot-pubsub` topology.
- Production/multi-replica readiness for the DataProtection-backed admission marker and query cursor key ring is
  guarded by `ChatBot:DataProtection:KeyRingPath`; production without that path must explicitly set
  `ChatBot:DataProtection:SingleReplicaOnly=true`.

No exception may become a production-domain-hosting bypass, preserve the current full hand-rolled host indefinitely, or
weaken the FR81a pre-commit admission chain.

## Alternatives Considered

- **Keep the current hand-rolled host as the default.** Rejected: it preserves the readiness pass-2 host-reuse gap,
  allows platform and ChatBot host behavior to drift, and keeps boilerplate in the domain module.
- **Record permanent ChatBot exceptions for host ownership.** Rejected: the approved direction is full DomainService
  SDK adoption. Exceptions must be small, dated, owned, scoped, and retired or reviewed.
- **Implement a ChatBot-specific admission bypass.** Rejected: it would weaken FR81a and create a second command path.
  The admission chain belongs at the platform SDK pre-commit hook.
- **Start 11.3-11.6 before the platform hook exists.** Rejected: migrations would either bypass admission or chase an
  unstable seam. Story 11.2 must establish the platform hook first.

## Verification

Story 11.1 verification is evidence-based and scoped to this decision:

- `rg -n "Status|Accepted|Hexalith.EventStore.DomainService|pre-commit admission|IDomainQueryHandler|IDomainProjectionHandler|IReadModelStore|ReadModelWritePolicy|IQueryCursorCodec|QueryCursorScope|AddEventStoreDomainTelemetry|AddEventStoreDomainStateStoreHealthCheck|AddEventStoreDomainModule|11.2 -> 11.3/11.4 -> 11.5 -> 11.6" docs/adrs/domainservice-sdk-host-adoption.md`
- `rg -n "domainservice-sdk-host-adoption.md|docs/adrs/domainservice-sdk-host-adoption.md" _bmad-output/planning-artifacts/architecture.md`
- `git diff --name-only -- references/Hexalith.EventStore` must be empty for Story 11.1.
- `git diff --check`
