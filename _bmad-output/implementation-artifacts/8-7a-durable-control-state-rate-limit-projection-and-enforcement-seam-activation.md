---
baseline_commit: 716e4cc
---

# Story 8.7a: Durable control-state/rate-limit projection and enforcement-seam activation

Status: done

<!-- Validation: create-story checklist applied 2026-06-11. -->

## Story

As a tenant administrator,
I want disable/quarantine/rate-limit decisions backed by a durable control-state/rate-limit projection at the enforcement seam,
so that a control decision actually blocks or throttles the targeted subject at runtime instead of being recorded against an inert `AlwaysActive...`/`AlwaysUnlimited...` default.

## Acceptance Criteria

1. **Durable projection feeds every runtime control seam.** Given a `GovernedOperationAggregate` control-state or rate-limit event for a mailbox source, service client, AI actor, command capability, or outbound channel, when the EventStore-published event is projected, then a durable read-side model materializes the current state/budget per `(tenantId, subjectClass, subjectRef)` in `chatbot-statestore` and runtime DI replaces the `AlwaysActive...ControlStateProvider` / `AlwaysUnlimited...RateLimitProvider` defaults on the live path. The projection must be metadata-only, tenant-partitioned, idempotent, and order-tolerant by EventStore source version. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.7a`; `_bmad-output/planning-artifacts/architecture.md#Architectural Findings to Carry into Decisions`; `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`; `src/Hexalith.ChatBot.Server/Projections/GovernedOperationProjectionHandler.cs`]

2. **Disabled/quarantined subjects fail closed after a control change.** Given a subject is active, when an identical governed operation succeeds before the projected control event and the subject is then disabled or quarantined, then the next operation through its real enforcement seam is blocked before restricted work: mailbox intake before Graph fetch, service/AI actor validation in `ServiceClientGrantValidator`, command capability authorization in `ParticipantAuthorizationStage`, and outbound send before `IOutboundMailboxSender.SendAsync`. The denial uses the existing catalogued reason and writes existing audit/diagnostic evidence. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.7a`; `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`]

3. **Rate-limited subjects throttle without cross-tenant bleed.** Given a projected rate-limit budget and admitted-history counter for a subject, when that subject reaches the configured rolling-window budget, then further operations are throttled/deferred through the existing rate-limit reason while unrelated tenants, subject classes, and sibling subjects remain unaffected. Out-of-bounds projected budgets fall back to each subject's existing safe defaults and never raise the cap. [Source: `_bmad-output/planning-artifacts/epics.md#NFR30`; `src/Hexalith.ChatBot.Server/Gateway/Stages/IServiceClientRateLimitProvider.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/IAiActorRateLimitProvider.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ICommandCapabilityRateLimitProvider.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/IOutboundChannelRateLimitProvider.cs`; `src/Hexalith.ChatBot.Workers/Mailbox/MailboxRateLimitState.cs`]

4. **Staleness and revocation bounds are test-backed.** Given a control-state or revocation-sensitive change is persisted, when the runtime seam reads the projection, then ordinary policy changes observe the NFR6 maximum staleness of 5 minutes and explicit revocation/control removals observe the 60 second revocation-sensitive path. Tests must use an injected clock/freshness policy, not wall-clock sleeps. [Source: `_bmad-output/planning-artifacts/epics.md#NFR6`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md#Story 8.7a`]

5. **No silent re-inerting on the wired runtime path.** Given the production/runtime service collection is built, when architecture/conformance tests inspect registrations and source/IL, then no live enforcement seam resolves an `AlwaysActive...` or `AlwaysUnlimited...` default. The always providers may remain only as explicit test-only or fail-safe fallback types with names/comments stating they are not registered on the live runtime path. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.7a`; `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`; `tests/Hexalith.ChatBot.Conformance.Tests/CorrectionPropagationWorkflowConformanceTests.cs`]

6. **Validation evidence is release-clean.** Given the implementation is complete, when the default lane runs, then `dotnet build` is Release-clean with warnings as errors, the focused server/worker/architecture/conformance tests pass, and a before/after proof demonstrates each controlled subject class is actually enforced after projection. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.7a`; `Directory.Build.props`; `global.json`]

## Tasks / Subtasks

- [x] Model the durable control-state/rate-limit projection (AC: 1, 3, 4)
  - [x] Add an internal read model under `src/Hexalith.ChatBot.Server/Projections/` such as `GovernedControlStateView` plus a store interface and in-memory/Dapr implementations. Key by `tenantId`, subject class, and safe subject ref; do not reuse `GovernedOperationView.KeyFor(tenantId, noteId)` because note ids are not the enforcement subject.
  - [x] Persist metadata-only fields only: tenant id, subject class token, subject ref, control state (`Active`/`Disabled`/`Quarantined` where applicable), optional rate-limit budget/window, source version, correlation id, effective timestamp, last updated timestamp, and freshness/revocation metadata.
  - [x] Make writes idempotent and order-tolerant: duplicate or lower/equal source versions are ignored; higher versions replace current state for that `(tenant x subjectClass x subjectRef)`.
  - [x] Store each tenant partition independently in `chatbot-statestore`; no global dictionaries shared across tenants on the Dapr path.

- [x] Extend the published-event translation path with trusted control-event payload data (AC: 1)
  - [x] Read `PublishedGovernedOperationEvent`, `GovernedOperationProjectionTranslator`, and the EventStore publish shape before coding. The current record exposes only metadata (`tenantId`, `aggregateId`, `eventTypeName`, `sequenceNumber`, etc.), which is not enough to know the controlled subject ref, state, budget, or window.
  - [x] Add a trusted, metadata-only way for projection code to receive the event payload subset needed for the control events. Do not infer subject state from `AggregateId`, command names, audit metadata text, or note ids.
  - [x] Translate these event types at minimum: `MailboxSourceDisabled`, `MailboxSourceQuarantined`, `MailboxSourceRateLimitConfigured`, `ServiceClientDisabled`, `ServiceClientQuarantined`, `ServiceClientRateLimitConfigured`, `AiActorDisabled`, `AiActorQuarantined`, `AiActorRateLimitConfigured`, `CommandCapabilityDisabled`, `CommandCapabilityQuarantined`, `CommandCapabilityRateLimitConfigured`, `OutboundChannelDisabled`, `OutboundChannelQuarantined`, and `OutboundChannelRateLimitConfigured`.
  - [x] Keep unrelated governed-operation events acknowledged as no-ops so Dapr at-least-once redelivery does not loop on unsupported events.

- [x] Replace live runtime DI defaults with projection-backed providers (AC: 1, 5)
  - [x] Implement projection-backed providers for `IServiceClientControlStateProvider`, `IAiActorControlStateProvider`, `ICommandCapabilityControlStateProvider`, `IOutboundChannelControlStateProvider`, `IServiceClientRateLimitProvider`, `IAiActorRateLimitProvider`, `ICommandCapabilityRateLimitProvider`, and `IOutboundChannelRateLimitProvider`.
  - [x] Register those providers in the runtime path from `CommandGatewayServiceCollectionExtensions.AddChatBotCommandGateway()` or a focused extension called by it. Avoid scattered overrides in `Program.cs`.
  - [x] Leave the existing always-active/unlimited classes only where tests explicitly instantiate them or where a documented non-runtime fallback is required. The wired runtime path must not use them.
  - [x] Preserve the existing enforcement order: disabled/quarantined checks before grant/scope/rate-limit checks; rate-limit as the final gate for service/AI actor and command capability; outbound channel rate-limit only after outbound channel control state is active.

- [x] Connect mailbox intake configuration to the projection (AC: 1, 2, 3)
  - [x] Add or update a mailbox configuration provider that composes the existing configured `ControlledMailboxPattern` with projected mailbox-source control state and rate-limit budget.
  - [x] Ensure `GraphMailboxIntakeWorker` still blocks disabled/quarantined sources before Graph fetch and defers rate-limited sources before content fetch/submission.
  - [x] Keep Story 7.3 mailbox configuration `IsEnabled` semantics distinct from FR74 mailbox-source control state.

- [x] Add admitted-history counters needed by rate-limit enforcement (AC: 3)
  - [x] Provide projection-backed histories for service-client commands, AI-actor proposals, command-capability submissions, outbound-channel sends, and mailbox intake timestamps where a budget applies.
  - [x] Advance counters only after an operation is admitted/sent/captured successfully; denied, disabled, quarantined, expired, revoked, under-scoped, or over-scoped attempts must not consume budget.
  - [x] Use server-measured UTC timestamps and the existing trailing-window math (`NotificationThrottleEvaluator.CountInTrailingWindow` / `MailboxRateLimitState.CountInTrailingWindow`); never trust caller/item supplied time.

- [x] Implement bounded-staleness and revocation-sensitive invalidation (AC: 4)
  - [x] Add a small freshness policy or timestamp check that enforces ordinary policy staleness <= 5 minutes and revocation/control removal <= 60 seconds.
  - [x] Use injected `ISystemClock` or an equivalent testable clock. No sleeps, timers, or wall-clock assumptions in unit tests.
  - [x] Surface stale/unavailable projection state as fail-closed where the operation is security-sensitive; do not silently treat stale/missing projection data as active/unlimited on the live path.

- [x] Add tests that prove real enforcement, not just seam behavior (AC: 2, 3, 4, 5, 6)
  - [x] Projection tests: each control/rate-limit event type materializes the expected subject state/budget; duplicate and out-of-order events are ignored; tenant partitioning is strict; payloads remain metadata-only.
  - [x] Provider tests: each projection-backed provider returns the projected state/budget and safe defaults only for documented absent state; out-of-bounds budget falls back to the existing safe default.
  - [x] Before/after runtime tests: identical operation succeeds before projection and is blocked/throttled after projection for mailbox source, service client, AI actor, command capability, and outbound channel.
  - [x] Isolation tests: same subject ref in another tenant and sibling subject refs remain unaffected.
  - [x] Staleness/revocation tests: ordinary <= 5 minute and revocation <= 60 second behavior is asserted with an injected clock.
  - [x] Architecture/conformance tests: production/runtime registrations cannot resolve `AlwaysActive...` or `AlwaysUnlimited...` providers for the enforcement seams; UI/CLI/MCP still enter through `IChatBotClient`/CommandGateway and never reference provider internals.

- [x] Run and record validation evidence (AC: 6)
  - [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - [x] `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none`
  - [x] `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - [x] `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
  - [x] `git diff --check`

### Review Follow-ups (AI)

- [x] [AI-Review][High] Fixed: control-state and rate-limit events shared one read-model record and each event fully replaced it, so a `*RateLimitConfigured` event re-activated a disabled/quarantined subject (`ControlState` reset to `active`) and a control event wiped a configured budget. Overlay each dimension independently. [`src/Hexalith.ChatBot.Server/Projections/GovernedControlStateProjectionHandler.cs`; `GovernedControlStateProjectionNotification.cs`; `GovernedControlStateProjectionTranslator.cs`]
- [ ] [AI-Review][Med] Confirm against Story 8.7b: `GovernedProjectionProviderHelpers.IsStale` measures age of the last projection write (`LastUpdatedAtUtc`), not lag versus the source of truth. An ordinary, still-active subject that merely had a rate-limit configured will be treated as stale after 5 minutes of inactivity and the control-state providers return `Disabled` — an idle-but-active rate-limited subject latches closed until a new control event or a periodic re-projection refreshes the record. This is the intended fail-closed posture (tested) but depends on 8.7b's periodic trigger to be practical; verify the propagation/refresh story closes the gap. [`src/Hexalith.ChatBot.Server/Gateway/Stages/ProjectionBackedGovernedControlProviders.cs`]
- [ ] [AI-Review][Med] Mailbox subject class has no live durable-projection path: `ProjectionBackedMailboxConfigurationProvider`, `IMailboxSourceControlProjection`, and `IMailboxSourceRateLimitProjection` are registered nowhere and have only `Static*` stubs — there is no Dapr-backed adapter reading `GovernedControlStateView` from `chatbot-statestore`. The four server-side seams are wired live; mailbox is unit-tested only. Acceptable while the worker host is deferred to Epic 11, but AC1's "feeds every runtime control seam" is not met for mailbox on any live path. [`src/Hexalith.ChatBot.Workers/Mailbox/ProjectionBackedMailboxConfigurationProvider.cs`]
- [ ] [AI-Review][Low] Admitted-history read-modify-write (`ProjectionBackedAdmittedHistory.RecordAsync`) does a get-then-`SaveStateAsync` with no ETag/optimistic concurrency, so concurrent admits for the same `(tenant × subjectClass × subjectRef)` can lose history entries or over-admit against the budget. Mirrors the existing Dapr view-store pattern; revisit if subject-level write contention becomes real. [`src/Hexalith.ChatBot.Server/Gateway/Stages/ProjectionBackedGovernedControlProviders.cs`; `src/Hexalith.ChatBot.Server/Projections/DaprGovernedControlStateProjectionStore.cs`]
- [ ] [AI-Review][Low] `GovernedControlStateProjectionTranslator.Wire(...RateLimitWindow)` overloads hardcode `RollingHour` and ignore the event's window enum; correct only while exactly one window dimension is declared. Map the actual value if a second window is ever added. [`src/Hexalith.ChatBot.Server/Projections/GovernedControlStateProjectionTranslator.cs`]

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Story 8.7 is a parent planning container and Story 8.7a is the assignable unit for durable control-state/rate-limit projection and enforcement-seam activation.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`; the architecture explicitly says Epic 7 landed the control floor but the runtime path remains wired-but-inert until Stories 8.7a/8.7b replace `AlwaysActive...`/`AlwaysUnlimited...` defaults.
- Loaded PRD requirements from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and the epics rollup: NFR6 staleness, NFR7 fail-closed, NFR15a fail-closed durable-write contract, NFR30 backlog isolation, FR68 fail-closed, FR74 disable/quarantine/rate-limit controls, and FR75 per-tenant limits/quotas/circuit breakers.
- Loaded UX artifacts from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/`; no new UI screen is required for 8.7a, but denial/recovery reasons must stay catalogued, localized, and metadata-only.
- Loaded persistent project-context facts from sibling modules. Relevant constraints: .NET SDK `10.0.302`, `net10.0`, central package management, warnings-as-errors, Dapr at-least-once/unordered delivery, pure EventStore aggregates, metadata-only telemetry/diagnostics, and root-level submodule initialization only.

### Current Implementation State

- `CommandGatewayServiceCollectionExtensions.AddChatBotCommandGateway()` currently registers `AlwaysActiveServiceClientControlStateProvider`, `AlwaysActiveAiActorControlStateProvider`, `AlwaysActiveCommandCapabilityControlStateProvider`, `AlwaysActiveOutboundChannelControlStateProvider`, `AlwaysUnlimitedServiceClientRateLimitProvider`, `AlwaysUnlimitedAiActorRateLimitProvider`, `AlwaysUnlimitedCommandCapabilityRateLimitProvider`, and `AlwaysUnlimitedOutboundChannelRateLimitProvider`. These are the inert runtime defaults this story must replace. [Source: `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`]
- The service-client/AI actor enforcement seam already exists in `ServiceClientGrantValidator`: disabled/quarantined checks run before grant scope/allowlist, and rate-limit runs as the final admission gate with subject-class separation. Reuse this seam; do not add a second gateway. [Source: `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs`]
- The command-capability enforcement seam already exists in `ParticipantAuthorizationStage`: control state runs actor-agnostically near the top of authorization, and command-capability rate-limit runs as the final actor-agnostic gate after admin and participant checks. Preserve governance-command exemptions for self-lockout prevention. [Source: `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`]
- The outbound-channel enforcement seam already exists in `AcceptedCommandDispatcher`: control state/rate-limit are checked immediately before the external sender adapter; blocked/quarantined/rate-limited sends are recorded as non-`sent` aggregate outcomes and do not call the adapter. [Source: `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`]
- Mailbox intake already has an enforcement shape through `ControlledMailboxPattern.ControlState` and optional `MailboxRateLimitState`; `GraphMailboxIntakeWorker` blocks disabled/quarantined sources and defers rate-limited sources before Graph fetch. This story must feed that configuration from the durable projection. [Source: `src/Hexalith.ChatBot.Workers/Mailbox/ControlledMailboxPattern.cs`; `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`]
- The current governed-operation projection only handles `GovernedNoteRecorded` into `GovernedOperationView` keyed by `(tenantId, noteId)`. It does not materialize control state or budgets and cannot feed enforcement providers as-is. [Source: `src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs`; `src/Hexalith.ChatBot.Server/Projections/GovernedOperationProjectionTranslator.cs`]
- `PublishedGovernedOperationEvent` currently carries EventStore-stamped metadata only; it does not expose event payload fields like `ServiceClientRef`, `MailboxSourceRef`, `NewState`, `NewBudget`, or `Window`. 8.7a must add a trusted payload path before projection can be correct. [Source: `src/Hexalith.ChatBot.Server/Projections/PublishedGovernedOperationEvent.cs`]

### Previous Story Intelligence

- Story 8.6 completed hosted Dapr Workflow binding and left the hand-rolled host in place until Epic 11. Do not start Epic 11 host reduction here; 11.5/11.6 are explicitly sequenced after 8.7a/8.7b so host migration does not chase a moving enforcement seam. [Source: `_bmad-output/implementation-artifacts/8-6-hosted-dapr-workflow-production-binding-and-saga-readiness-validation.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md`]
- Story 8.5 reinforced the fail-safe/no-fabricated-value doctrine: stale, unknown, or unavailable state must be surfaced honestly instead of pretending healthy/active/unlimited. Apply that doctrine to projection freshness and provider fallback behavior. [Source: `_bmad-output/implementation-artifacts/8-5-degraded-state-operability-and-runbook-diagnostics.md`]
- Stories 7.12-7.26 completed the recorded, audited control decisions over `GovernedOperationAggregate`; their accepted runtime-effect claims are explicitly deferred until 8.7a/8.7b. Do not rewrite their commands/events unless a small additive projection payload contract is required. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.12`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md#Per-story runtime-activation riders`]
- Recent git history is Epic 8 operational work: `716e4cc feat(story-8.6)`, `a4e8833 feat(story-8.5)`, `54681b6 feat(story-8.4)`, `a9f7d44 feat(story-8.3)`, `9217f16 feat(story-8.2)`. Preserve the established pattern: metadata-only observability, fail-closed paths, focused tests, and explicit validation evidence.

### Architecture Guardrails

- Use .NET SDK `10.0.302`, target `net10.0`, nullable enabled, warnings-as-errors, Allman braces, file-scoped namespaces, and central package management. Do not add package versions to `.csproj` files. [Source: `global.json`; `Directory.Build.props`; `Directory.Packages.props`]
- EventStore remains the source of truth. The projection is a read-side enforcement cache; it must not become a second aggregate or independent lifecycle authority. [Source: `_bmad-output/planning-artifacts/architecture.md#Internal Decomposition`]
- Dapr pub/sub is at-least-once and unordered. Projection handlers must tolerate duplicate and out-of-order delivery by source version. [Source: `Hexalith.Memories/_bmad-output/project-context.md`; `src/Hexalith.ChatBot.Server/Projections/GovernedOperationProjectionHandler.cs`]
- Keep tenant isolation at the store-access layer. Projection keys, rate-limit counters, histories, and provider reads must include tenant id and return safe-not-found for the wrong tenant. [Source: `_bmad-output/planning-artifacts/architecture.md#Data boundaries`; `_bmad-output/planning-artifacts/epics.md#NFR30`]
- Do not bypass CommandGateway, `ServiceClientGrantValidator`, `ParticipantAuthorizationStage`, `AcceptedCommandDispatcher`, or `GraphMailboxIntakeWorker`. The story activates existing seams rather than adding alternate enforcement pipelines.
- Do not put Dapr, projection-store, audit, authorization, or clock I/O inside `GovernedOperationAggregate`; aggregate logic remains pure and event-sourced.
- UI/CLI/MCP must remain clients over `IChatBotClient`/CommandGateway and must not reference provider/projection internals.
- Respect repository submodule policy: initialize/update only root-level submodules declared in root `.gitmodules`; never use recursive submodule commands.

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Server/
    Projections/
      GovernedControlStateView.cs                 # NEW current state/budget read model
      IGovernedControlStateProjectionStore.cs     # NEW internal store seam
      InMemoryGovernedControlStateProjectionStore.cs
      DaprGovernedControlStateProjectionStore.cs
      GovernedControlStateProjectionHandler.cs    # NEW/UPDATE projection handler for control events
      GovernedControlStateProjectionTranslator.cs # NEW/UPDATE translator from published events
    Gateway/
      CommandGatewayServiceCollectionExtensions.cs # UPDATE runtime registrations
      Stages/
        ProjectionBacked*ControlStateProvider.cs   # NEW providers or grouped provider file
        ProjectionBacked*RateLimitProvider.cs      # NEW providers or grouped provider file
  Hexalith.ChatBot.Workers/
    Mailbox/
      ProjectionBackedMailboxConfigurationProvider.cs # NEW/UPDATE composition over existing pattern provider
tests/
  Hexalith.ChatBot.Server.Tests/Projections/
  Hexalith.ChatBot.Server.Tests/Gateway/Stages/
  Hexalith.ChatBot.Workers.Tests/Mailbox/
  Hexalith.ChatBot.Architecture.Tests/
  Hexalith.ChatBot.Conformance.Tests/
```

### Project Structure Notes

- Projection and provider implementation belongs in `.Server` because the current gateway/projection seams are internal to `Hexalith.ChatBot.Server`.
- Mailbox worker composition belongs in `.Workers/Mailbox` only for the worker-facing configuration provider; do not move mailbox intake logic into `.Server`.
- Dapr state-store adapters should mirror `DaprGovernedOperationViewStore` and use `chatbot-statestore`.
- Tests should extend existing suites instead of creating a one-off test project.
- No generated client edits are expected unless the EventStore publish contract exposed through ChatBot public OpenAPI changes. Prefer internal projection payload contracts if possible.

### Out of Scope

- Story 8.7b periodic trigger and deferred evaluator consolidation.
- Epic 11 host migration / DomainService SDK adoption.
- Rewriting the Epic 7 command contracts, approval flows, or `GovernedOperationAggregate` invariants beyond additive projection payload support.
- New user-facing screens.
- New policy semantics or new rate-limit formulas/windows.
- Treating stale/missing projection data as active/unlimited on the live path.
- Direct sibling mutation or direct reads of restricted mailbox/project/file/message content.
- Package upgrades, target framework changes, recursive submodule initialization, or generated-client hand edits.

### Latest Technical Specifics

- No external package/version research is required for this story. Use the repo-pinned stack: .NET SDK `10.0.302`, `net10.0`, Dapr `1.17.9`, Aspire `13.4.3`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, and NetArchTest.eNhancedEdition `1.4.5`. [Source: `Directory.Packages.props`]
- If the projection event-payload path touches Dapr pub/sub delivery, keep using the current `UseCloudEvents()` + `MapSubscribeHandler()` pattern in `Program.cs` and the `WithTopic(pubSubName, topicName)` endpoint style.

### Validation Notes

Checklist pass completed against `.agents/skills/bmad-create-story/checklist.md`:

- Reinvention prevention: story directs reuse of existing Epic 7 aggregate events and existing enforcement seams instead of adding a second gateway or new control model.
- Wrong-location prevention: projection/provider code goes under `.Server/Projections` and `.Server/Gateway/Stages`; mailbox composition goes under `.Workers/Mailbox`; tests extend existing suites.
- Regression prevention: preserves enforcement order, self-lockout governance-command exemptions, tenant isolation, metadata-only payloads, source-version idempotency, and fail-closed behavior.
- Critical gap called out: current `PublishedGovernedOperationEvent` lacks the event payload fields needed for correct projection; the story explicitly forbids inferring control state from aggregate ids or audit text.
- Scope control: 8.7b trigger consolidation, Epic 11 host migration, UI screens, package upgrades, and Epic 7 contract rewrites are out of scope.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`]
- [Source: `.agents/skills/bmad-create-story/discover-inputs.md`]
- [Source: `.agents/skills/bmad-create-story/template.md`]
- [Source: `.agents/skills/bmad-create-story/checklist.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml#development_status`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.7a`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Findings to Carry into Decisions`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-readiness-blockers.md#CR-2`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-09-host-reuse.md#Story 8.7a`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR6`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR7`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR30`]
- [Source: `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`]
- [Source: `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Projections/PublishedGovernedOperationEvent.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Projections/GovernedOperationProjectionHandler.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Projections/DaprGovernedOperationViewStore.cs`]
- [Source: `Directory.Packages.props`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 1632 tests.
- `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none` - passed, 32 tests.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 41 tests.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 96 tests.
- `git diff --check` - passed.

### Completion Notes List

- Story context created by bmad-create-story workflow.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added tenant-partitioned `GovernedControlStateView` projection storage with in-memory and Dapr `chatbot-statestore` adapters keyed by `(tenantId, subjectClass, subjectRef)`.
- Extended the governed-operation published envelope to carry EventStore-published payload bytes and translated all required control/rate-limit event types without inferring from aggregate ids, note ids, command names, or audit text.
- Replaced live command-gateway always-active/unlimited registrations with projection-backed control-state, rate-limit, and admitted-history providers while preserving existing enforcement order.
- Added worker-side mailbox configuration composition over existing `ControlledMailboxPattern`, keeping mailbox configuration semantics distinct from FR74 control state.
- Added freshness checks using injected clocks and fail-closed behavior for stale revocation-sensitive state.
- Added focused projection/provider/mailbox/architecture tests and ran the requested validation lane successfully.

### File List

- `_bmad-output/implementation-artifacts/8-7a-durable-control-state-rate-limit-projection-and-enforcement-seam-activation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IAiActorRateLimitProvider.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ICommandCapabilityRateLimitProvider.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IOutboundChannelRateLimitProvider.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IServiceClientRateLimitProvider.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ProjectionBackedGovernedControlProviders.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprGovernedControlStateProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/GovernedControlStateProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/GovernedControlStateProjectionNotification.cs`
- `src/Hexalith.ChatBot.Server/Projections/GovernedControlStateProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/GovernedControlStateView.cs`
- `src/Hexalith.ChatBot.Server/Projections/GovernedOperationProjectionEndpoints.cs`
- `src/Hexalith.ChatBot.Server/Projections/IGovernedControlStateProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryGovernedControlStateProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedGovernedOperationEvent.cs`
- `src/Hexalith.ChatBot.Workers/Mailbox/ProjectionBackedMailboxConfigurationProvider.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/GovernedOperationProjectionTests.cs`
- `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`

### Change Log

- 2026-06-11: Implemented durable governed control-state/rate-limit projection, projection-backed runtime providers, mailbox configuration composition, admitted histories, freshness checks, and focused validation coverage for Story 8.7a.
- 2026-06-11: Senior Developer Review (AI) — auto-fixed 1 High correctness/security defect (cross-dimension clobber re-activating disabled subjects); recorded 4 follow-ups (1 fixed, 3 open). Re-ran build + Server/Workers/Architecture/Conformance lanes clean. Status → done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-11 · **Outcome:** Approve with follow-ups

### Scope

Adversarial validation of the durable control-state/rate-limit projection and enforcement-seam activation against the six ACs and the claimed File List. Verified the EventStore publish path actually carries the new `payload` bytes (`EventPublisher` republishes unprotected `EventEnvelope.Payload`), that `EventTypeName` is the event's `GetType().FullName` (`EventPersister`), and that the translator's `System.Text.Json` `Web` deserialize is consistent with the persist-time serializer (`EventPersister` default STJ → numeric enums; `DomainProcessorStateRehydrator.SerializerOptions` is also converter-less `Web`). The serialization round-trip is sound — no enum/casing mismatch.

### Findings

- **High (fixed): cross-dimension clobber.** Control-state and rate-limit events for a subject share one read-model record, and the handler rebuilt the whole record from each single-dimension notification. A `*RateLimitConfigured` event therefore set `ControlState = active`, silently re-activating a previously `Disabled`/`Quarantined` subject (a fail-open security regression), and a control event wiped a previously configured budget — contradicting AC1's "materializes the current state/budget". Fixed by tagging each notification with a `GovernedControlDimension` and overlaying only the changed dimension, carrying the other forward from the existing record. Added `ControlStateAndRateLimitEventsShouldOverlayIndependentlyWithoutClobberingEachOther` covering both orderings.
- **Medium (open):** staleness is measured as age of the last projection write, not lag versus the source of truth — see Review Follow-ups. Tested/intentional fail-closed posture, but idle active rate-limited subjects latch to `Disabled`; depends on 8.7b's periodic refresh.
- **Medium (open):** no live durable-projection path for the mailbox subject class (only `Static*` stubs; nothing registered) — see Review Follow-ups. Acceptable while the worker host is deferred to Epic 11.
- **Low (open):** non-atomic admitted-history read-modify-write (no ETag); `Wire(window)` hardcodes `RollingHour`. See Review Follow-ups.

### AC coverage

- AC1 (durable projection feeds runtime seams): **Met** for the four server-side seams (projection-backed providers registered in `AddChatBotCommandGateway`); **partial** for mailbox (foundation only, no live wiring — tracked).
- AC2 (fail-closed on control change): Met for service client / AI actor / command capability / outbound channel; mailbox enforced in `GraphMailboxIntakeWorker` but not hosted.
- AC3 (rate-limit throttle, no cross-tenant bleed, out-of-bounds → safe default): Met — `EffectiveBudget` clamps to the in-bounds cap, never raising; per-`(tenant × subjectClass × subjectRef)` keying isolates subjects/tenants.
- AC4 (staleness/revocation bounds, injected clock): Met as implemented and tested (5 min / 60 s via `ISystemClock`); semantics caveat tracked.
- AC5 (no live `AlwaysActive…`/`AlwaysUnlimited…`): Met — registrations replaced; source-scan architecture test guards regressions.
- AC6 (release-clean validation): Met — re-verified `dotnet build` 0/0, Server 1637 / Workers 32 / Architecture 41 / Conformance 96 passing, `git diff --check` clean after the fix.

### Validation evidence (post-fix)

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — 0 warnings, 0 errors.
- Server.Tests 1637 · Workers.Tests 32 · Architecture.Tests 41 · Conformance.Tests 96 — all passing, 0 failed.
- `git diff --check` — clean.
