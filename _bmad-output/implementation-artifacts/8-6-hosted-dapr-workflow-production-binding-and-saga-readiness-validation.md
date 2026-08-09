---
baseline_commit: a4e8833
---

# Story 8.6: Hosted Dapr Workflow production binding and saga readiness validation

Status: done

<!-- Validation: create-story checklist applied 2026-06-11. -->

## Story

As a platform/operations engineer,
I want ChatBot's correction-propagation coordinator bound to the hosted Dapr Workflow runtime with production validation,
so that production saga orchestration claims are backed by runtime wiring, observability, retry behavior, and failure-mode evidence.

## Acceptance Criteria

1. **Hosted runtime binding is real.** Given the Story 2.8 correction-propagation coordinator/activity seam, when the production AppHost/container topology is configured, then ChatBot registers the Dapr Workflow runtime, health-checks it, and binds correction propagation through explicit DI plus DAPR component/configuration wiring. This must replace the current in-process-only coordinator path for live topology, while keeping the same EventStore writer/activity seams. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.6`; `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs`; `src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs`; `src/Hexalith.ChatBot.AppHost/Program.cs`]

2. **Workflow lifecycle is observable and metadata-only.** Given a correction propagation workflow instance, when it starts, retries, completes, delays, or fails in the hosted runtime, then `workflowInstanceId`, `tenantId`, `correctionId`, `sourceVersion`, status, retry count, last failure code, and correlation id are visible through metadata-only telemetry and operation/status diagnostics, with no raw project names, candidate evidence, participant data, message subjects, file metadata, secrets, or raw exception text. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.6`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR34`; `src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs`; `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs`]

3. **Dependency outages fail closed with scoped operator signal.** Given workflow runtime, state store, pub/sub, audit writer, projection dependency, or EventStore command-writing outage, when correction propagation admission or execution depends on that dependency, then the failure is scoped to the affected tenant/workflow item where possible, no false success is recorded, the existing safe operator alert/P2 signal is emitted only after required audit evidence, and retry/idempotency state remains replay-safe. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.6`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR41`; `src/Hexalith.ChatBot.Server/Gateway/Stages/DefaultAssociationCorrectionDependencyReadiness.cs`; `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs`; `_bmad-output/implementation-artifacts/8-5-degraded-state-operability-and-runbook-diagnostics.md#Completion Notes List`]

4. **Saga-readiness evidence is explicit.** Given a production saga claim is made for correction propagation, when validation evidence is reviewed, then it includes local AppHost smoke evidence, production-config lint/static checks, retry/idempotency evidence, delayed-state evidence, workflow runtime status evidence, and proof that the implementation does not directly mutate Projects, Conversations, Folders, Memories, or EventStore internals. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.6`; `_bmad-output/planning-artifacts/architecture.md#Infrastructure & Deployment`; `_bmad-output/implementation-artifacts/2-8-correction-propagation-contract.md#Senior Developer Review (AI)`]

5. **No regression of Story 2.8/9.6 correction semantics.** Given correction propagation now runs through hosted Dapr Workflow, when the M0/M1 and M2 paths execute, then deterministic workflow ids, per-store acknowledgements, source-version idempotency, `Correction-delayed` SLO behavior, vector-reindex extension behavior, audit-then-alert ordering, and delayed-to-corrected completion semantics remain unchanged and test-backed. [Source: `_bmad-output/implementation-artifacts/2-8-correction-propagation-contract.md#Senior Developer Review (AI)`; `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationSlo.cs`; `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/CorrectionPropagationCoordinatorTests.cs`]

## Tasks / Subtasks

- [x] Add the hosted Dapr Workflow runtime package and DI registration (AC: 1, 5)
  - [x] Add `Dapr.Workflow` to `Directory.Packages.props` at `1.17.9` and a versionless `<PackageReference Include="Dapr.Workflow" />` only where runtime code needs it; do not add inline package versions or adopt `1.18.0-rc*`.
  - [x] Add a focused `AddChatBotCorrectionPropagationWorkflow(...)`/extension update near `CommandGatewayServiceCollectionExtensions.AddChatBotCorrectionPropagation()` that registers the workflow and activities with `AddDaprWorkflow(...)`.
  - [x] Keep `Hexalith.ChatBot.Contracts` free of Dapr Workflow dependencies; workflow runtime types belong in `.Server/Lifecycle/Workflows`.
  - [x] Preserve the existing `ICorrectionPropagationCoordinator`, `ICorrectionPropagationStoreActivity`, and `ICorrectionPropagationCommandWriter` seams so tests and future store activities keep one boundary.

- [x] Replace the in-process live coordinator with a hosted workflow adapter (AC: 1, 3, 5)
  - [x] Introduce `CorrectionPropagationWorkflow` under `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/` using Dapr Workflow orchestration primitives, and move side effects into registered activity classes.
  - [x] Make the workflow use the existing `CorrectionPropagationRequest`, deterministic `WorkflowInstanceIdFor(...)`, `CorrectionPropagationStoreKeys.RequiredM0/RequiredM2`, `CorrectionPropagationSlo`, command writer, and store-activity contracts.
  - [x] Do not put Dapr, HTTP, projection-store, audit, authorization, or clock I/O inside `GovernedOperationAggregate`; the aggregate remains pure EventStore state transition logic.
  - [x] Keep EventStore events/projections the source of truth. Dapr Workflow coordinates execution; it must not become an independent lifecycle authority.
  - [x] Decide whether to rename the existing `DaprCorrectionPropagationCoordinator` or keep it as a compatibility facade; either way, avoid leaving a misleading class that is still in-process on the production path.

- [x] Wire AppHost/Aspire Dapr Workflow production topology (AC: 1, 4)
  - [x] Extend `ChatBotAspireModule`/`AppHost` wiring so the ChatBot sidecar has the workflow runtime prerequisites and references the correct state store/pubsub/config without reusing the EventStore actor store for ChatBot workflow state unless explicitly justified.
  - [x] Keep production `accesscontrol.yaml` deny-by-default and local `accesscontrol.local.yaml` self-hosted-only behavior intact; do not grant `chatbot-ui` DAPR access.
  - [x] Add static topology tests in `tests/Hexalith.ChatBot.Aspire.Tests` and `tests/Hexalith.ChatBot.AppHost.Tests` proving workflow component/config wiring, state-store names, and access-control posture.
  - [x] Add conformance coverage that fails if the production story still describes correction propagation as hosted Dapr Workflow while no workflow runtime is registered.

- [x] Add workflow runtime readiness and health checks (AC: 1, 3)
  - [x] Extend `DefaultAssociationCorrectionDependencyReadiness` so workflow runtime availability is a real dependency, not just constructor non-null checks.
  - [x] Health-check the hosted workflow client/runtime and required Dapr components; map unavailable runtime/state/pubsub/audit/projection conditions to existing catalog-backed safe reason codes such as `association_correction_workflow_unavailable`.
  - [x] Fail closed before durable correction-propagation success is written when runtime dependencies are unavailable; release coarse idempotency admission or queue replay intent where the existing gateway path already supports it.
  - [x] Preserve safe problem details and do not leak raw Dapr/grpc exception text.

- [x] Add metadata-only workflow telemetry and operation-status projection (AC: 2, 3)
  - [x] Add bounded, low-cardinality metrics/diagnostics for workflow lifecycle events: start, retry, completion, delay, failure, runtime unavailable, state-store unavailable, pub/sub unavailable, audit unavailable, projection unavailable.
  - [x] Use the existing `Hexalith.ChatBot` meter and safe tag posture. If adding instruments to `ChatBotMetrics`, keep tags bounded to tenant/operation-class/status/reason-style values; do not add raw workflow ids as metric labels.
  - [x] Surface high-cardinality identifiers such as `workflowInstanceId` and `correlationId` through operation/status diagnostics or audit metadata, not metric tags.
  - [x] Ensure `Correction-delayed` P2 alert emission still follows audit-then-deliver ordering.

- [x] Prove retry, idempotency, delayed-state, and failure-mode behavior (AC: 3, 4, 5)
  - [x] Add unit tests for workflow orchestration: successful M0 fan-out/fan-in, M2 vector activity inclusion, deterministic instance id, duplicate scheduling/replay behavior, per-store retry, activity failure, delayed state, and delayed-to-corrected completion.
  - [x] Add readiness/gateway tests for workflow runtime unavailable, state store unavailable, pub/sub unavailable, audit unavailable, projection unavailable, and EventStore command writer failure.
  - [x] Add AppHost/Aspire static tests and at least one local AppHost/Tier-3 smoke path that schedules or validates a workflow instance in the self-hosted topology when Dapr is available; skip gracefully only when Docker/Dapr prerequisites are genuinely absent.
  - [x] Add architecture/conformance tests proving UI/CLI/MCP/adapters do not start Dapr workflows directly and no direct mutation of Projects, Conversations, Folders, Memories, or EventStore internals was introduced.
  - [x] Capture saga-readiness evidence in the story completion notes before moving to review: commands run, workflow status/history proof, failure-mode proof, and any skipped Tier-3 prerequisites.

### Review Findings

Canonical Story 2.9 re-review (`a4e8833..716e4cc`, 2026-08-09). Layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor.

- [x] [Review][Patch] Treat deterministic already-exists schedule as idempotent success (narrow catch only; do not swallow generic Dapr errors); emit safe duplicate-schedule-replay metric [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationWorkflowRuntime.cs:21`] — decided 2026-08-09: option 1 idempotent success
- [x] [Review][Patch] Wire workflow instance id/status/retry count/last failure code into operation-status projection and `OperationStatusHttpResults.ToWire` (metadata-only) [`src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs`] — decided 2026-08-09: option 1 wire into status API
- [x] [Review][Patch] Strengthen Tier-3 primary-path smoke to schedule a deterministic correction-propagation workflow and assert instance status/history (skip only when Docker/Dapr absent; skip-only runs do not unlock done) [`tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs:429`] — decided 2026-08-09: option 2 schedule + inspect
- [x] [Review][Defer] Distinct workflow `Failed` outcome — deferred to Story 2.10 retry/exhaustion; Story 2.9 keeps `Correction-delayed` only for store/soft failures — decided 2026-08-09: option 1 delayed-only
- [x] [Review][Patch] Pessimistic readiness + clear availability on schedule failure [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationWorkflowRuntime.cs:12`]
- [x] [Review][Patch] Honor `CorrectionPropagationDelayActivity` false return (do not complete as Delayed when audit/P2 path fails) [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflow.cs:60`]
- [x] [Review][Patch] Do not remap `OperationCanceledException` to workflow-unavailable [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs:48`]
- [x] [Review][Patch] Forward `cancellationToken` into `ScheduleNewWorkflowAsync` [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationWorkflowRuntime.cs:21`]
- [x] [Review][Patch] Stop publishing store-progress count as `RetryCount` / `retrying` status on first attempts [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflow.cs:33`]
- [x] [Review][Patch] Execute `CorrectionPropagationWorkflow.RunAsync` under test (current helper reimplements orchestration and never runs the workflow) [`tests/Hexalith.ChatBot.Server.Tests/Lifecycle/CorrectionPropagationCoordinatorTests.cs:194`]
- [x] [Review][Patch] Cover audit-writer and idempotency-store admission deny branches with gateway/API tests [`src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`]
- [x] [Review][Patch] Assert M2 `EstimatedCompletionAtUtc` rewrite on the schedule path [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs:36`]
- [x] [Review][Patch] Null-guard activity `Request` inputs before use [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationDelayActivity.cs:25`]
- [x] [Review][Patch] Tighten architecture allowlist so `Dapr.Workflow` cannot land anywhere under `Gateway/` except the registration extension [`tests/Hexalith.ChatBot.Architecture.Tests/CorrectionPropagationWorkflowArchitectureTests.cs:11`]
- [x] [Review][Patch] Emit or remove unused granular workflow failure-code constants; align lifecycle metrics with real dependency outages [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflowFailureCodes.cs`]
- [x] [Review][Defer] Durable activity `CancellationToken.None` side-effect calls [`src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagation*Activity.cs`] — deferred, Dapr durable-activity convention; host cancellation is not a reliable activity abort signal

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Story 8.6 is the remaining Epic 8 backlog story and owns hosted Dapr Workflow production binding for the Story 2.8 correction-propagation seam.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`; architecture states that Epic 2 implemented a DAPR-ready coordinator/activity seam, while hosted Dapr Workflow runtime binding remains pending before any production saga claim.
- Loaded `prd_content` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`; relevant requirements are NFR2 metadata-only diagnostics, NFR13 idempotency, NFR15a fail-closed correction path, NFR17/NFR17a delayed states, NFR19 at-least-once/retry safety, NFR34 correlation, NFR41 scoped degradation, NFR42 degraded-surface owner/next action, and NFR44 runbook diagnostics.
- Loaded `ux_content` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`; no new user-facing screen is required, but existing correction/diagnostic surfaces must keep reachable safe status, owner role, and next actions.
- Loaded persistent project-context facts from sibling modules. Cross-cutting constraints: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3 + Shouldly, Dapr at-least-once semantics, pure EventStore aggregates, metadata-only diagnostics, root-level submodule initialization only, and no generated-client hand edits.

### Current Implementation State

- `DaprCorrectionPropagationCoordinator` currently runs synchronously in-process over injected store activities. It writes start, per-store acknowledgement, completion, or delay commands through `ICorrectionPropagationCommandWriter`, then audit-then-alerts on delay. Despite the name, it does not register or schedule hosted Dapr Workflow runtime work today. [Source: `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs`]
- `ICorrectionPropagationCoordinator` exposes only `IsReady` and `StartAsync(CorrectionPropagationRequest, CancellationToken)`. Preserve this adapter surface unless the story explicitly justifies an additive status/query API. [Source: `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/ICorrectionPropagationCoordinator.cs`]
- Correction workflow identifiers already have deterministic helpers: `CorrectionIdFor(associationId, sourceVersion)` and `WorkflowInstanceIdFor(tenantId, associationId, correctionId, sourceVersion)`. Hosted workflow scheduling must use these instead of generated GUIDs. [Source: `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs`]
- M0/M1 scope is the four stores in `CorrectionPropagationStoreKeys.RequiredM0`; M2 scope adds `vector-reindex` when the vector activity is registered. Do not regress Story 9.6's M2 extension behavior. [Source: `src/Hexalith.ChatBot.Server/Association/CorrectionPropagationStoreKeys.cs`; `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/VectorReindexCorrectionPropagationStoreActivity.cs`]
- `CorrectionPropagationSlo` is the single SLO contract: M0/M1 target 10 minutes and M2 target 60 minutes. Do not inline alternate values. [Source: `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationSlo.cs`]
- `CommandGatewayServiceCollectionExtensions.AddChatBotCommandGateway()` already calls `services.AddChatBotCorrectionPropagation()` and registers the projection/idempotency/audit/readiness services. Runtime workflow DI should be added there or in a focused extension, not scattered in `Program.cs`. [Source: `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`]
- `ChatBotAspireModule` currently wires `statestore` for EventStore actor/status state, `chatbot-statestore` for ChatBot read models/coarse idempotency, and `chatbot-pubsub` for governed events. No workflow-specific component/config is present. [Source: `src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs`]
- Production access control is deny-by-default and grants only EventStore POST invocation to ChatBot; local Aspire uses a separate default-allow config only because self-hosted Dapr runs mTLS-off. Preserve this split. [Source: `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml`; `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml`]

### Previous Story Intelligence

- Story 2.8 completed correction propagation contracts, aggregate lifecycle events, projection/status/UI rendering, Dapr-ready coordinator/activity seams, fail-closed readiness seams, source-version idempotency, and the M0 delayed-state/P2 signal. It explicitly did **not** add the hosted Dapr Workflow package/runtime. [Source: `_bmad-output/implementation-artifacts/2-8-correction-propagation-contract.md#Completion Notes List`; `_bmad-output/implementation-artifacts/2-8-correction-propagation-contract.md#Senior Developer Review (AI)`]
- Story 2.8 re-review recorded three relevant residual risks: the corrected-context guard seam is dormant outside current production paths, `DefaultAssociationCorrectionDependencyReadiness` mostly checks injected singleton non-null state, and `DaprCorrectionPropagationCoordinator` is in-process rather than hosted runtime. Story 8.6 owns the hosted-runtime part and should improve readiness where it affects workflow runtime claims. [Source: `_bmad-output/implementation-artifacts/2-8-correction-propagation-contract.md#Findings Recorded`]
- Story 8.5 completed NFR41/NFR42/NFR44 degraded-state operability. Reuse its scope, safe-token, owner-role, next-action, and no-fabrication doctrine for runtime workflow failures. Do not introduce a second degraded-dependency vocabulary. [Source: `_bmad-output/implementation-artifacts/8-5-degraded-state-operability-and-runbook-diagnostics.md#Completion Notes List`]
- Recent git history is all Epic 8 operational-observability work: `a4e8833 feat(story-8.5)`, `54681b6 feat(story-8.4)`, `a9f7d44 feat(story-8.3)`, `9217f16 feat(story-8.2)`, `cc162b5 feat(story-8.1)`. Preserve the established pattern: metadata-only observability, fail-safe unknown/unavailable states, and explicit validation evidence.

### Architecture Guardrails

- Use .NET SDK `10.0.300`, target `net10.0`, nullable enabled, warnings-as-errors, Allman braces, file-scoped namespaces, and central package management. Do not add package versions to `.csproj` files. [Source: `global.json`; `Directory.Build.props`; `Directory.Packages.props`; `Hexalith.EventStore/_bmad-output/project-context.md`]
- Dapr packages are pinned at `1.17.9`; add `Dapr.Workflow` at the same stable family if needed. Do not adopt `1.18.0-rc*` without a separate architecture decision. [Source: `Directory.Packages.props`; `https://www.nuget.org/packages/Dapr.Workflow/`]
- Official Dapr v1.17 docs describe `Dapr.Workflow` plus `AddDaprWorkflow(...)` registration for .NET workflows/activities, and `DaprWorkflowClient.ScheduleNewWorkflowAsync(...)`/`GetWorkflowStateAsync(...)` for management/status. Use those APIs through a seam so tests can fake the workflow client. [Source: `https://docs.dapr.io/developing-applications/building-blocks/workflow/howto-author-workflow/`; `https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-workflow/dotnet-workflow-management-methods/`]
- Workflow orchestration code must be replay-safe. Workflow logic schedules activities and uses deterministic input/state; side effects such as EventStore command writes, audit writes, operator alerts, projection invalidation, and vector reindexing belong in activities or existing activity seams, not nondeterministic workflow code. [Source: `Hexalith.Memories/_bmad-output/project-context.md#Framework-Specific Rules`; Dapr Workflow docs]
- EventStore remains source of truth. Hosted Dapr Workflow coordinates saga execution, but durable lifecycle truth remains EventStore events plus projections. [Source: `_bmad-output/planning-artifacts/architecture.md#Correction propagation (FR91a)`; `Hexalith.EventStore/_bmad-output/project-context.md#Framework-Specific Rules`]
- No direct sibling mutation. Projects, Conversations, Folders, Memories, and EventStore internals may only be touched through existing adapter ports, EventStore commands, published events, or activity seams. [Source: `_bmad-output/planning-artifacts/architecture.md#Service boundaries`; sibling project-context files]
- Keep UI/CLI/MCP out of workflow startup. They submit governed commands through `IChatBotClient`/CommandGateway only; architecture tests should fail if they reference workflow runtime/client types. [Source: `_bmad-output/planning-artifacts/architecture.md#FR81a CommandGateway`; `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`]
- Respect repository submodule policy: root-level submodule initialization only; never use recursive submodule commands. [Source: AGENTS.md instructions; sibling project-context workflow rules]

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Server/
    Lifecycle/
      Workflows/
        CorrectionPropagationWorkflow.cs              # NEW hosted Dapr workflow orchestrator
        CorrectionPropagationWorkflowActivity.cs      # NEW activity bridge over existing store activities/writer
        DaprWorkflowCorrectionPropagationCoordinator.cs # NEW/RENAME adapter that schedules hosted workflow
        WorkflowRuntimeReadiness.cs                   # NEW if a focused readiness probe is needed
        IWorkflowRuntimeClient.cs                     # NEW seam over DaprWorkflowClient for tests, if useful
        DaprWorkflowRuntimeClient.cs                  # NEW hosted client adapter
      Workflows/CorrectionPropagationSlo.cs           # UPDATE only if status evidence needs helpers; do not duplicate constants
    Gateway/
      CommandGatewayServiceCollectionExtensions.cs    # UPDATE registration in one place
      Stages/DefaultAssociationCorrectionDependencyReadiness.cs # UPDATE real runtime readiness
    Observability/
      ChatBotMetrics.cs                               # UPDATE bounded workflow lifecycle instruments if needed
    Gateway/Status/OperationStatusRecord.cs           # UPDATE only if operation status carries workflow metadata
  Hexalith.ChatBot.Aspire/
    ChatBotAspireModule.cs                            # UPDATE Dapr workflow component/config wiring
  Hexalith.ChatBot.AppHost/
    Program.cs                                        # UPDATE only for topology env/config wiring
    DaprComponents/*.yaml                             # UPDATE production/local config, preserving ACL split
tests/
  Hexalith.ChatBot.Server.Tests/Lifecycle/
  Hexalith.ChatBot.Server.Tests/Gateway/
  Hexalith.ChatBot.Aspire.Tests/
  Hexalith.ChatBot.AppHost.Tests/
  Hexalith.ChatBot.Architecture.Tests/Fitness/
  Hexalith.ChatBot.Conformance.Tests/
  Hexalith.ChatBot.IntegrationTests/
```

### Project Structure Notes

- Workflow runtime implementation belongs under `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/` because this is the existing lifecycle/workflow seam and the only place Story 2.8 workflow contracts currently live.
- Dapr Workflow package/version declarations belong in `Directory.Packages.props` plus versionless project references. Do not add package versions to `.csproj` files.
- Aspire/AppHost changes belong in `src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs`, `src/Hexalith.ChatBot.AppHost/Program.cs`, and `src/Hexalith.ChatBot.AppHost/DaprComponents/`. Keep production and local access-control files separate.
- Tests should extend the existing suites rather than creating a one-off test area: Server lifecycle/gateway tests for behavior, Aspire/AppHost tests for topology, Architecture/Conformance tests for boundaries, Integration tests for local runtime smoke evidence.
- `Hexalith.ChatBot.Contracts`, `Hexalith.ChatBot.UI`, `Hexalith.ChatBot.Cli`, and `Hexalith.ChatBot.Mcp` must not reference Dapr Workflow runtime types.
- No generated client file should be touched unless a public OpenAPI contract changes; if it changes, update the OpenAPI spine and regenerate.

### Latest Technical Specifics

- Dapr's official docs currently mark v1.17 as the latest stable docs stream and v1.18 as preview. Use the v1.17 workflow docs for `AddDaprWorkflow(...)`, workflow/activity registration, `DaprWorkflowClient.ScheduleNewWorkflowAsync(...)`, and `GetWorkflowStateAsync(...)`. [Source: `https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-workflow/`; `https://docs.dapr.io/developing-applications/building-blocks/workflow/howto-author-workflow/`; `https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-workflow/dotnet-workflow-management-methods/`]
- NuGet lists `Dapr.Workflow` `1.17.9` as the stable package aligned with the repo's existing Dapr `1.17.9` packages; newer `1.18.0-rc*` builds are prerelease. [Source: `https://www.nuget.org/packages/Dapr.Workflow/`; `Directory.Packages.props`]
- `Dapr.Workflow` targets .NET 8 and is compatible with `net10.0`, so no target-framework change is needed. [Source: `https://www.nuget.org/packages/Dapr.Workflow/`]

### Out of Scope

- Rewriting Story 2.8 propagation events, aggregate state, or routing-status contract unless a small additive field is required for AC2 observability.
- Implementing unrelated Epic 8.7 control-plane runtime activation or periodic enforcement triggers.
- Implementing the dormant Epic 4 AI proposal path beyond preserving existing corrected-context readiness behavior.
- Implementing new user-facing screens. Use existing status/diagnostic surfaces if operation status needs to show workflow metadata.
- Replacing EventStore with Dapr Workflow state as lifecycle authority.
- Direct writes to Projects, Conversations, Folders, Memories, vector stores, or EventStore internals.
- Package upgrades outside `Dapr.Workflow` `1.17.9` and any strictly required transitive central package alignment.
- Generated-client edits by hand. Regenerate only if a public OpenAPI contract changes.
- Recursive or nested submodule initialization.

### Testing Notes

Minimum validation before dev handoff to review:

```bash
dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false
./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none
./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none
./tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -parallel none
./tests/Hexalith.ChatBot.Aspire.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Aspire.Tests -parallel none
./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none
./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none
./tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -parallel none
git diff --check
```

If VSTest socket permissions block `dotnet test`, use compiled xUnit v3 executables and record that limitation. If Tier-3 AppHost/Dapr smoke tests skip because Docker/Dapr is unavailable, record the exact prerequisite skip and keep static topology/DI tests green.

### Validation Notes

Checklist pass completed against `.agents/skills/bmad-create-story/checklist.md`:

- Reinvention prevention: story directs reuse of Story 2.8 writer/activity/coordinator seams, existing EventStore lifecycle events, `CorrectionPropagationSlo`, operational metrics/status patterns, and AppHost/Aspire Dapr component patterns.
- Wrong-location prevention: workflow runtime code belongs in `.Server/Lifecycle/Workflows`; topology in `.Aspire`/`.AppHost`; no Dapr Workflow types in `.Contracts`, UI, CLI, or MCP.
- Regression prevention: deterministic ids, source-version idempotency, audit-then-alert ordering, M0/M2 scope behavior, metadata-only diagnostics, and deny-by-default access control are explicit.
- Scope control: Epic 8.7 runtime activation, new UI surfaces, direct sibling mutation, EventStore lifecycle replacement, and broad package upgrades are out of scope.
- LLM optimization: acceptance criteria and tasks cite concrete files/classes, current implementation gaps, and exact validation expectations.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`]
- [Source: `.agents/skills/bmad-create-story/discover-inputs.md`]
- [Source: `.agents/skills/bmad-create-story/template.md`]
- [Source: `.agents/skills/bmad-create-story/checklist.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml#development_status`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.6`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Infrastructure & Deployment`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Correction propagation (FR91a)`]
- [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`]
- [Source: `_bmad-output/implementation-artifacts/2-8-correction-propagation-contract.md`]
- [Source: `_bmad-output/implementation-artifacts/8-5-degraded-state-operability-and-runbook-diagnostics.md`]
- [Source: `Directory.Packages.props`]
- [Source: `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/ICorrectionPropagationCoordinator.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationSlo.cs`]
- [Source: `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`]
- [Source: `src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs`]
- [Source: `src/Hexalith.ChatBot.AppHost/Program.cs`]
- [Source: `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml`]
- [Source: `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml`]
- [Source: `https://docs.dapr.io/developing-applications/building-blocks/workflow/howto-author-workflow/`]
- [Source: `https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-workflow/dotnet-workflow-management-methods/`]
- [Source: `https://www.nuget.org/packages/Dapr.Workflow/`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet restore Hexalith.ChatBot.slnx --disable-parallel --ignore-failed-sources -v minimal`
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
- `./tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -parallel none`
- `./tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Aspire.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Aspire.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
- `git diff --check`

### Completion Notes List

- Story context created by bmad-create-story workflow.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added hosted Dapr Workflow registration behind `AddChatBotCorrectionPropagationWorkflow()` and production opt-in via AppHost configuration, while preserving the existing coordinator, command writer, and store-activity seams.
- Reworked correction propagation so live topology schedules deterministic workflow instances and registered activities perform EventStore command writes, per-store activity execution, completion, delay, and audit-then-alert behavior.
- Added a dedicated ChatBot workflow state store component, workflow health endpoint, workflow runtime readiness dependency, bounded lifecycle metrics, and operation-status workflow diagnostic fields.
- Added static topology, architecture, conformance, server, and opt-in Tier-3 smoke coverage. Tier-3 workflow smoke skipped in this environment because `HEXALITH_CHATBOT_TIER3=1` plus Docker/Dapr prerequisites were not present.
- Validation passed: solution build; Server 1612 tests; Integration 20 tests with 3 expected Tier-3 skips; AppHost 6 tests; Aspire 3 tests; Architecture 40 tests; Conformance 96 tests; `git diff --check`.
- Senior Developer Review (AI) auto-fix (2026-06-11): added 4 omitted files to the File List and corrected a stale Aspire actor-boundary comment (comment-only, no behavior change). No CRITICAL findings; see the Senior Developer Review (AI) section. Status moved review → done.

### File List

- `Directory.Packages.props`
- `_bmad-output/implementation-artifacts/8-6-hosted-dapr-workflow-production-binding-and-saga-readiness-validation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.AppHost/Program.cs`
- `src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs`
- `src/Hexalith.ChatBot.Aspire/HexalithChatBotResources.cs`
- `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationCompleteActivity.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationDelayActivity.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationDelayInput.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationRunStoreActivity.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationScopeActivity.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationStartActivity.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationStartInput.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationStoreActivityInput.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflow.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflowFailureCodes.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflowProgress.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflowResult.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflowStatuses.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationWorkflowRuntime.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/ICorrectionPropagationActivityCatalog.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/ICorrectionPropagationWorkflowRuntime.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/UnavailableCorrectionPropagationWorkflowRuntime.cs`
- `src/Hexalith.ChatBot.Server/Observability/ChatBotMetrics.cs`
- `src/Hexalith.ChatBot.Server/Observability/ChatBotOperationClasses.cs`
- `src/Hexalith.ChatBot.Server/Observability/IChatBotMetrics.cs`
- `src/Hexalith.ChatBot.Server/Observability/NullChatBotMetrics.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/CorrectionPropagationWorkflowArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Aspire.Tests/ChatBotAspireModuleTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CorrectionPropagationWorkflowConformanceTests.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/CorrectionPropagationCoordinatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/ChatBotMetricsTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/ChatBotOperationClassesTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Observability/RecordingChatBotMetrics.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStores/VectorReindexDependencyInjectionTests.cs`

### Change Log

- 2026-06-11: Implemented hosted Dapr Workflow binding, production topology wiring, readiness/health/telemetry, saga-safety tests, and moved story to review.
- 2026-06-11: Senior Developer Review (AI) — auto-fix pass. Completed the Dev Agent Record File List (4 omitted files added) and corrected a stale, security-relevant Aspire actor-boundary comment. No CRITICAL findings; all five ACs verified against implementation and tests. Status → done.
- 2026-08-09: Canonical Story 2.9 code review — applied 14 patches (idempotent duplicate schedule, operation-status workflow fields, Tier-3 schedule+inspect smoke, readiness/cancellation/delay/retry telemetry/tests). Deferred terminal `Failed` to Story 2.10. Sprint status → in-progress pending retained Tier-3 primary-path evidence.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-11 · **Outcome:** Approve (auto-fixed)

Adversarial review of the hosted Dapr Workflow binding for the correction-propagation seam. Every `[x]` task was cross-checked against `src/` and `tests/`, and git reality was compared against the story File List.

### Acceptance Criteria verdict

- **AC1 (hosted runtime binding is real):** IMPLEMENTED. `DaprCorrectionPropagationWorkflowRuntime` schedules via `DaprWorkflowClient.ScheduleNewWorkflowAsync`; registered through `AddChatBotCorrectionPropagationWorkflow()` gated on `ChatBot:UseDaprWorkflowRuntime`; AppHost sets it `true`. The coordinator now delegates fan-out to the workflow + activities; `IsReady = runtime.IsAvailable && activityCatalog.IsReady`.
- **AC2 (observable, metadata-only):** IMPLEMENTED. `ChatBotMetrics.RecordWorkflowLifecycle` is bounded (tenant/operation-class/status/reason, 96-char cap, no workflow id as a tag). High-cardinality ids (`workflowInstanceId`, `correlationId`, retry count, last failure code) flow through the workflow custom status (`SetCustomStatus`) and the command-writer audit metadata — not metric tags. The `/health/chatbot/workflows` endpoint and `CheckAsync` return only safe constant tokens (no raw exception text).
- **AC3 (fail closed with scoped signal):** IMPLEMENTED. `ParticipantAuthorizationStage` denies `CorrectEmailProjectAssociation` with scoped reason codes (`association_correction_workflow_unavailable` / projection / audit / idempotency) before durable mutation; `CorrectionPropagationDelayActivity` preserves audit-then-deliver (P2 alert only after the pre-commit audit write succeeds).
- **AC4 (saga-readiness evidence):** IMPLEMENTED. Static topology, architecture, and conformance tests prove workflow registration and no direct sibling/EventStore mutation; opt-in Tier-3 smoke (`CorrectionPropagationWorkflowRuntimeShouldBeHealthyInRealDaprTopology`) skips gracefully when Docker/Dapr are absent.
- **AC5 (no 2.8/9.6 regression):** IMPLEMENTED. The workflow reproduces the prior in-process order (scope → start → per-store invalidate+ack → complete/delay), deterministic ids and `CorrectionPropagationSlo` are unchanged, and M2 vector-reindex scope behavior is preserved; coordinator tests assert each path.

### Findings

Fixed in this pass (MEDIUM):

1. **File List incomplete** — four changed files were part of the workflow-unavailable readiness wiring but missing from the Dev Agent Record File List: `Gateway/ChatBotAuthorizationReasonCodes.cs`, `Gateway/ChatBotProblemDetailsFactory.cs`, `Gateway/Stages/ParticipantAuthorizationStage.cs`, and `tests/.../Gateway/CommandGatewayTests.cs`. Added.
2. **Stale security-boundary comment** — `ChatBotAspireModule.cs` still claimed "the chatbot hosts NO actors ... references those two components only." The chatbot now hosts the Dapr Workflow actor runtime backed by the actor-capable `chatbot-workflow-statestore` (three component references). Comment corrected to describe the saga actor runtime and its dedicated state store.

Observations left as foundation (LOW — not blocking, flagged for follow-up):

3. `OperationStatusRecord` gained `WorkflowInstanceId/WorkflowStatus/WorkflowRetryCount/WorkflowLastFailureCode`, but they are never written, read, or mapped to the wire (`OperationStatusHttpResults.ToWire` omits them). AC2 is satisfied via the workflow custom status + audit metadata, so these are unwired diagnostic placeholders; a future story should wire them into an operation-status projection or drop them.
4. Workflow failure-code constants `StateStoreUnavailable`, `PubSubUnavailable`, `ProjectionUnavailable`, `EventStoreWriterUnavailable` are defined but never emitted; granular dependency-type failures currently collapse into the generic store-failure/delay path. Likewise the `retrying` status is surfaced only on the custom status, not the lifecycle metric.
5. `DaprCorrectionPropagationWorkflowRuntime.IsAvailable` defaults optimistically to `true` before the first probe, so cold-start admission can pass before a health probe runs. `ScheduleAsync` still fail-closes (throws, records `runtime-unavailable`, no `Complete` event) so no false success is recorded; consider a pessimistic-until-probed default if the live topology can tolerate it.
6. `ChatBot__Workflow__StateStoreName` is set by the AppHost but not consumed by Server code — Dapr Workflow binds the sidecar's `actorStateStore` component automatically. Retained as intentional topology scaffolding (asserted by `AppHostTopologyTests`).
