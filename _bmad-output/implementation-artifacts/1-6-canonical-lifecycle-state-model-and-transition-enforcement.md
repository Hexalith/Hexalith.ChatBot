---
baseline_commit: 26f6ee028a5a2c13c9775c6c3d9066b4b9fe3184
---

# Story 1.6: Canonical Lifecycle State Model and Transition Enforcement

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-30. -->

## Story

As a workflow owner,
I want a canonical lifecycle state machine with validated transitions,
so that workflow items move only through legal states and invalid transitions are rejected and audited.

## Acceptance Criteria

1. Given the exact state vocabulary, when states are defined, then `Received | Proposed | Associated | Rejected | Deferred | NeedsReview | Failed | Skipped | Corrected` plus sub-states `Correcting | Correction-delayed` exist as stable strings used verbatim across UI, CLI, MCP, audit, OpenAPI, and generated clients. Do not keep or introduce lifecycle synonyms such as `pending`, `accepted`, `running`, `succeeded`, or `cancelled` for workflow state. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.6; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Association Lifecycle and States; Source: _bmad-output/planning-artifacts/architecture.md#Naming Patterns]
2. Given an inbound or outbound transition, when attempted, then it is validated against an explicit state model before any durable state mutation, including gateway command-submission state transitions and future association/retry/correction paths. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.6; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Workflow State Contracts and Testability; Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns]
3. Given an invalid transition, when attempted, then it is rejected before mutation and recorded with the rejected transition, actor, reason, correlation context, and safe metadata-only details. If the audit writer is unavailable, the operation returns typed `AuditUnavailable`, queues the intent for replay, emits an operator alert, and writes no durable state. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.6; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15; Source: src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs]
4. Given a terminal state (`Rejected`, `Failed`, or `Skipped`), when reprocessed, then the original record remains unchanged and a new workflow instance ID is created with `supersedes` and `superseded_by` audit links. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.6; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Association Lifecycle and States; Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns]
5. Given health/status values are exposed by ChatBot, when serialized or displayed, then status enums use only stable strings `healthy`, `degraded`, `failed`, and `unknown`; they are explicit values and are never derived from counts. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.6; Source: _bmad-output/planning-artifacts/architecture.md#Naming Patterns]

## Tasks / Subtasks

- [x] Replace the current placeholder lifecycle contract with the canonical workflow vocabulary (AC: 1, 5)
  - [x] Update `src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs` so its `EnumMember` wire values are exactly the PascalCase canonical strings from AC1, in the documented order.
  - [x] Add a contract enum or equivalent stable contract type for ChatBot health/status values: `healthy`, `degraded`, `failed`, `unknown`. Avoid naming collisions with `Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus`.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` `LifecycleState` enum and examples to match the canonical strings.
  - [x] Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` through the existing NSwag target. Do not hand-edit generated client output.
  - [x] Update `tests/fixtures/hexalith-chatbot-generated-client.sha256` after regeneration.
- [x] Add the lifecycle state model under `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/` (AC: 1, 2, 4)
  - [x] Create internal model types for lifecycle states, sub-states, transition definitions, terminal-state detection, and transition validation.
  - [x] Represent valid transition edges explicitly. Minimum edges for this story: `Received -> Proposed`, `Received -> NeedsReview`, `Received -> Failed`, `Received -> Skipped`, `Proposed -> Associated`, `Proposed -> Rejected`, `Proposed -> Deferred`, `Proposed -> NeedsReview`, `Proposed -> Failed`, `Deferred -> Proposed`, `Deferred -> Rejected`, `Deferred -> NeedsReview`, `NeedsReview -> Proposed`, `NeedsReview -> Associated`, `NeedsReview -> Rejected`, `NeedsReview -> Deferred`, `Associated -> Corrected`, `Corrected -> Correcting`, `Correcting -> Corrected`, `Correcting -> Correction-delayed`, and `Correction-delayed -> Corrected`.
  - [x] Treat `Rejected`, `Failed`, and `Skipped` as terminal states for the same workflow instance. No transition may leave these states.
  - [x] Add a reprocess helper/factory contract that requires a new workflow instance ID and carries old/new audit link names `superseded_by_workflow` and `supersedes_workflow`; do not mutate terminal records in place.
  - [x] Keep the model deterministic and side-effect free; no DAPR, HTTP, clock, tenant lookup, or sibling client calls inside transition validation.
- [x] Wire lifecycle validation into the current gateway/audit path without bypassing the spine (AC: 2, 3)
  - [x] Update `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` so accepted command submission uses a canonical transition. The current `LifecycleState.Accepted` and audit transitions `admitted->dispatch_pending` / `dispatch_pending->accepted` are not canonical workflow states.
  - [x] Update `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` so `stateTransition` values are emitted from validated canonical lifecycle transitions, not ad hoc strings.
  - [x] Add an invalid-transition rejection path that returns metadata-only Problem Details and records the rejected transition, actor ID/type, reason code, and correlation ID.
  - [x] Preserve Story 1.4 behavior: pre-commit audit remains the fail-closed gate before dispatch; post-commit audit failure still queues reconciliation and alert instead of rolling back dispatch.
  - [x] Preserve Story 1.5 behavior: equivalent duplicate replay returns the prior outcome without dispatch or new audit; idempotency conflict returns metadata-only 409 and does not call lifecycle mutation logic.
- [x] Add tests that make invalid states and invalid transitions impossible to miss (AC: all)
  - [x] Update `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs` to assert the exact lifecycle and health/status wire names in order.
  - [x] Update `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs` to assert OpenAPI lifecycle enum values and examples use canonical strings only.
  - [x] Update `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs` for generated enum values and refreshed hash.
  - [x] Add focused `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/` tests for all valid edges, representative invalid edges, terminal-state reprocess semantics, sub-state behavior, and status enum stability.
  - [x] Extend `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` to prove gateway audit envelopes use canonical transition strings, invalid transitions fail before dispatch, and audit-writer-down on invalid transition follows the existing `AuditUnavailable` replay/alert behavior.
  - [x] Extend `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` with guards that reject non-canonical lifecycle string literals in non-generated ChatBot source, except documented test sentinels and OpenAPI examples.
- [x] Verify locally (AC: all)
  - [x] Run `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false`.
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1`.
  - [x] Run `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests`.
  - [x] Run `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests`.
  - [x] Run `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests`.
  - [x] Run `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests`.
  - [x] If VSTest or generated-client tooling is blocked in the sandbox, record the exact command, error, and replacement in-process command in the Dev Agent Record.

## Dev Notes

### Implementation Intent

Story 1.6 turns lifecycle state from a placeholder response enum into a load-bearing contract and server-side state model. The current code has `LifecycleState` values `pending`, `accepted`, `running`, `succeeded`, `failed`, `rejected`, and `cancelled`, and the command endpoint returns `"accepted"` after gateway dispatch. That is incompatible with the story and architecture requirement that workflow lifecycle states use exact strings `Received`, `Proposed`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Corrected`, `Correcting`, and `Correction-delayed`. [Source: src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs; Source: src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml; Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; Source: _bmad-output/planning-artifacts/architecture.md#Naming Patterns]

Use this story to establish the canonical state model and transition validator now. Later stories for mailbox intake, association decisions, retries, approval, correction, and command execution must reuse this model instead of inventing local state machines. The model belongs in `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/`; public string vocabulary belongs in `Contracts` and OpenAPI. [Source: _bmad-output/planning-artifacts/architecture.md#Structure Patterns; Source: _bmad-output/planning-artifacts/architecture.md#Requirements Structure Mapping]

### Current Files To Update

- `src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs`: currently exposes non-canonical lowercase workflow values. Replace with exact canonical `EnumMember` values. [Source: src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs]
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`: currently defines the old lifecycle enum and `AcceptedCommand` example with `lifecycleState: accepted`. Update schema and examples. [Source: src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml]
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`: generated by NSwag before compile. Regenerate only; do not hand-edit. [Source: src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj; Source: src/Hexalith.ChatBot.Client/nswag.json]
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`: refresh after generated client changes. [Source: tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs]
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`: currently sets generated-client `LifecycleState.Accepted`; update to a canonical initial workflow state and validate transition before returning. Preserve auth, tenant-bind, authorize, risk, approval, idempotency, pre-commit audit, dispatch, post-commit audit order. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs]
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`: currently emits ad hoc `stateTransition` strings. Replace with validated canonical transition values and rejected-transition details. [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs`: review whether rejected transition details need structured fields or safe source-evidence refs. Keep envelope metadata-only and serialization tolerant. [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs]
- `src/Hexalith.ChatBot.Server/Program.cs`: `/health/chatbot` currently returns only module identity. Add explicit stable status only if the story exposes ChatBot health/status values here; do not derive status from counts. [Source: src/Hexalith.ChatBot.Server/Program.cs]
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`, `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, and `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`: update existing assertions and add lifecycle guardrails. [Source: tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs; Source: tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs; Source: tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs; Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs; Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]

### Architecture Guardrails

- Do not build a second command pipeline. Lifecycle validation is a reusable state-model stage/guard inside the existing CommandGateway/EventStore flow, not a new adapter path or alternate dispatcher. [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Shared Command Pipeline]
- Do not let adapters, CLI, MCP, workers, or future UI code replicate lifecycle validation. Surface adapters translate to typed commands and call the client; the server model validates transitions. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
- Invalid transitions are normal domain rejections, not exceptions. Return structured rejection/problem data and keep durable mutation blocked. If implemented inside EventStore aggregate logic later, use `DomainResult.Rejection([new ...Rejection(...)])` and `IRejectionEvent`, never thrown business-rule exceptions. [Source: Hexalith.EventStore/_bmad-output/project-context.md#Critical Implementation Rules; Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs]
- Keep lifecycle validation pure and deterministic. No I/O, DAPR, HTTP, clock, tenant projection, or sibling client calls inside transition checks. [Source: Hexalith.EventStore/_bmad-output/project-context.md#Critical Implementation Rules]
- Keep all transition and rejection output metadata-only. Do not write raw command payloads, exception text, project/file/party names, local paths, secrets, or unauthorized resource hints into audit envelopes, logs, Problem Details, or status bodies. [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns; Source: Hexalith.EventStore/_bmad-output/project-context.md#Critical Implementation Rules]
- Preserve two-phase audit semantics. Pre-commit audit is fail-closed. Post-commit WORM audit failure is reconcile-from-event-log and alert, not rollback. [Source: _bmad-output/implementation-artifacts/1-4-fail-closed-audit-commit-seam-with-pre-and-post-commit-audit-emission.md#Architecture Guardrails]
- Preserve two-altitude idempotency. Lifecycle validation must not replace coarse gateway request dedup or EventStore fine idempotency. [Source: _bmad-output/implementation-artifacts/1-5-two-altitude-idempotency.md#Architecture Guardrails]
- Do not add inline package versions, new serializers, or new dependencies. Use the current pinned .NET 10, NSwag, xUnit v3, Shouldly, DAPR, and Aspire versions from central package management. [Source: Directory.Packages.props; Source: Directory.Build.props]

### Project Structure Notes

- Alignment: lifecycle model code belongs under `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/`, matching the architecture source tree. If the folder does not exist yet, create it. [Source: _bmad-output/planning-artifacts/architecture.md#Source Tree]
- Alignment: public wire vocabulary belongs in `src/Hexalith.ChatBot.Contracts` and OpenAPI; generated client output belongs under `src/Hexalith.ChatBot.Client/Generated/` and is refreshed by NSwag. [Source: src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj]
- Alignment: deterministic unit tests belong in `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/` and gateway regression tests stay in `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`. [Source: _bmad-output/planning-artifacts/architecture.md#File Organization Patterns]
- Detected conflict: architecture prose says inherited C# style may be Allman, but existing ChatBot source uses K&R braces. Match surrounding ChatBot files. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; Source: Hexalith.Tenants/_bmad-output/project-context.md#Critical Implementation Rules]
- Detected conflict: current OpenAPI/client contract uses `LifecycleState` for the command-submission response. Treat this as a public contract migration in this story and update all generated/test fixtures in the same change.

### Previous Story Intelligence

- Story 1.5 completed coarse idempotency at commit `26f6ee0`; it added the real `IIdempotencyStore`, `DaprCoarseIdempotencyStore`, canonicalizer, and replay/conflict branching. Preserve equivalent duplicate replay and conflict behavior while adding lifecycle validation. [Source: _bmad-output/implementation-artifacts/1-5-two-altitude-idempotency.md#Completion Notes List; Source: git log --oneline -5]
- Story 1.4 completed fail-closed pre-commit audit and post-commit reconciliation. Invalid lifecycle transitions must use this audit seam rather than adding an unaudited rejection path. [Source: _bmad-output/implementation-artifacts/1-4-fail-closed-audit-commit-seam-with-pre-and-post-commit-audit-emission.md#Architecture Guardrails]
- Story 1.3 established tenant binding from authenticated claims only. Lifecycle records, audit, and rejected-transition data must use `ChatBotGatewayContext.TenantBinding`, not request-body tenant values. [Source: _bmad-output/implementation-artifacts/1-3-commandgateway-admission-spine-with-tenant-binding-and-authorization.md#Senior Developer Review (AI)]
- Known local validation issue from prior stories: default VSTest can fail in this sandbox due to TCP listener permissions. Use focused xUnit v3 in-process test executables when that repeats and record exact blockers. [Source: _bmad-output/implementation-artifacts/1-5-two-altitude-idempotency.md#Testing Requirements]
- Current dirty worktree observed during story creation: `_bmad-output/story-automator/orchestration-1-20260530-160445.md` is unrelated. Do not revert or overwrite it. [Source: git status --short]

### Testing Requirements

- Use xUnit v3 and Shouldly. Prefer deterministic Tier 1 tests for the lifecycle model. Do not introduce a mocking framework for pure state-machine tests. [Source: Directory.Packages.props; Source: _bmad-output/planning-artifacts/architecture.md#Implementation Handoff]
- Lifecycle tests must cover both positive and negative edges. Include at least one invalid edge from each state class: initial, active, review, corrected, sub-state, and terminal.
- Invalid transition tests must assert negative behavior, not only returned status: no dispatcher call, no durable state mutation, audit/replay/alert behavior matches pre-commit fail-closed expectations, and serialized responses contain no tenant/resource/payload sentinels.
- Contract tests must reject old lifecycle strings anywhere they remain load-bearing: `pending`, `accepted`, `running`, `succeeded`, `cancelled`.
- Architecture tests should scan non-generated ChatBot source for hard-coded lifecycle strings and require use of the canonical model/constants. Allow generated files and explicit test sentinel strings only.
- OpenAPI and client generation tests must be updated together; failing to refresh the generated-client hash is an implementation defect, not a test issue.

### Out of Scope

- Do not implement Story 1.7 message catalog/redaction stage, Story 1.8 correlation/status query behavior, or Story 1.9's first governed command.
- Do not implement full mailbox intake, association scorer, approval queues, correction propagation workflow, retry worker, UI surfaces, CLI, or MCP adapters.
- Do not implement Epic 9 WORM audit chain persistence, production audit-completeness observables, replay isolation, or projection rebuild validation.
- Do not modify sibling bounded contexts or EventStore internals unless a compile error requires a minimal adapter-facing update.
- Do not initialize nested submodules, run recursive submodule commands, add inline package versions, or hand-edit generated client files.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md#Discover Inputs Protocol]
- [Source: .agents/skills/bmad-create-story/template.md#Story Template]
- [Source: .agents/skills/bmad-create-story/checklist.md#Story Context Quality Competition Prompt]
- [Source: AGENTS.md#Git Submodules]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.6]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Association Lifecycle and States]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Workflow State Contracts and Testability]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Shared Command Pipeline]
- [Source: _bmad-output/planning-artifacts/architecture.md#Naming Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Structure Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Process Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Source Tree]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Key State UX Matrix]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]
- [Source: _bmad-output/implementation-artifacts/1-5-two-altitude-idempotency.md]
- [Source: _bmad-output/implementation-artifacts/1-4-fail-closed-audit-commit-seam-with-pre-and-post-commit-audit-emission.md]
- [Source: src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs]
- [Source: src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml]
- [Source: src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj]
- [Source: src/Hexalith.ChatBot.Client/nswag.json]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs]
- [Source: tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs]
- [Source: tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- [Source: Directory.Packages.props]
- [Source: Directory.Build.props]
- [Source: git log --oneline -5]
- [Source: git status --short]

## Change Log

- 2026-05-30: Implemented canonical lifecycle state vocabulary, state model, gateway validation, audit transition enforcement, generated client refresh, health status contract, and regression tests.
- 2026-05-30: Senior Developer Review (AI) found and auto-fixed missing gateway/audit wiring, stale OpenAPI/generated client lifecycle values, missing DI registration, stale story metadata, and one over-broad contract test assertion.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Activation customization resolved with no prepend or append steps, persistent fact glob `file:{project-root}/**/project-context.md`, and no `on_complete` terminal instruction.
- Input discovery loaded sprint status, epics, architecture, PRD/addendum lifecycle sections, UX state guidance, Story 1.5 and Story 1.4 intelligence, project-context facts from sibling modules, current ChatBot contracts/gateway/audit source and tests, and recent git status/log.
- Web research was not required because this story does not upgrade version-sensitive infrastructure; the repository pins SDK/package versions and implementation constraints in `global.json`, `Directory.Packages.props`, and project-context files.
- Checklist validation applied during story creation: identified current non-canonical lifecycle values, required OpenAPI/generated-client migration, added lifecycle state-model location, added invalid-transition fail-closed audit behavior, included terminal reprocess semantics, and added tests/architecture guards to prevent lifecycle synonyms.
- Senior Developer Review (AI) loaded story, sprint status, project planning artifacts, implementation files, git diff/status, and validation checklist. MCP/web documentation search was not needed because no external API or version-sensitive behavior was changed.
- Generated client refreshed with `dotnet msbuild src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj /t:GenerateHexalithChatBotClient /p:Configuration=Debug -m:1 /nr:false`.

### Completion Notes List

- Canonical lifecycle contract values are exposed through contracts, OpenAPI, generated client output, and command responses.
- Added stable ChatBot health status contract values and explicit `/health/chatbot` status output.
- Added pure lifecycle state model, explicit valid transition edges, terminal-state detection, and reprocess plan factory.
- Wired command submission through `ILifecycleTransitionGuard`; valid submissions audit `Received->Proposed`, invalid transitions return metadata-only 409 before dispatch, and audit-unavailable invalid transitions return typed `AuditUnavailable` with replay intent and alert.
- Preserved two-phase audit behavior, duplicate replay, idempotency conflict behavior, and metadata-only response hygiene.
- Added contract, client generation, lifecycle model, gateway, HTTP bootstrap, integration, and architecture guardrail coverage.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-30

Outcome: Approved after automatic fixes. Story status set to `done`; sprint status synced to `done`.

Issues found and fixed:

- HIGH: `CommandGateway` still returned generated `LifecycleState.Accepted` and did not call the lifecycle transition guard on the real path.
- HIGH: `AuditEnvelopeFactory` still emitted ad hoc `admitted->dispatch_pending` and `dispatch_pending->accepted` transitions instead of canonical validated transitions.
- HIGH: OpenAPI and generated client still exposed legacy lifecycle values (`pending`, `accepted`, `running`, `succeeded`, `cancelled`).
- HIGH: Invalid lifecycle transition handling existed only in tests; production DI did not register `ILifecycleTransitionGuard` and production gateway had no rejection branch.
- MEDIUM: Story metadata and File List were stale; the story remained `ready-for-dev` even though implementation existed.
- MEDIUM: One contract test rejected any `- accepted` substring and failed on legitimate `acceptedAt`/description text; tightened it to exact enum/example lifecycle values.

Verification:

- PASS: `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false`
- PASS: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- PASS: `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests`
- PASS: `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests`
- PASS: `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests`
- PASS: `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests`
- PASS: `tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests`

### File List

- _bmad-output/implementation-artifacts/1-6-canonical-lifecycle-state-model-and-transition-enforcement.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs
- src/Hexalith.ChatBot.Contracts/Enums/ChatBotHealthStatus.cs
- src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs
- src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs
- src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs
- src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs
- src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/CommandSubmissionLifecycleTransitionGuard.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/ILifecycleTransitionGuard.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleReprocessFactory.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleReprocessPlan.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleStates.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleSubStates.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTerminalStates.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionDefinition.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionReasonCodes.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionValidation.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionValidator.cs
- src/Hexalith.ChatBot.Server/Program.cs
- tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs
- tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs
- tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs
- tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs
- tests/fixtures/hexalith-chatbot-generated-client.sha256
