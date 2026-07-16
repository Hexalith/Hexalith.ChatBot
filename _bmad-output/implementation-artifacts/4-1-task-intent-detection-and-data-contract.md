---
baseline_commit: a471d8f
---

# Story 4.1: Task-intent detection and data contract

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As the system,
I want candidate task/action intent detected from authorized conversation actors with source evidence,
so that actionable requests are captured for governed review.

## Acceptance Criteria

1. Given an authorized project conversation actor, when a message implies a task or action, then the system captures a durable task-intent record preserving source message evidence with `tenant_id`, `project_id`, `source_message_id`, `requester_party_id`, `detected_intent_summary` (280 characters max), `detected_action_kind`, `source_evidence_offsets`, `kernel_version`, `confidence_score` in `[0,1]`, `detected_at`, and `state`. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.1; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Task-Intent-and-AI-Action-Mediation]
2. Given detection runs on an email-derived project conversation item, when tenant scope, project authorization, source message state, requester party identity, audit readiness, or required projected evidence is missing, stale, redacted, corrected, or cross-tenant, then no task-intent record is captured and the failure is represented as a safe redacted rejection or non-actionable classification without confirming restricted resource existence. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR1; #NFR2; #NFR7; #NFR11; _bmad-output/planning-artifacts/architecture.md#Fail-Closed-NFR15a]
3. Given a captured record, when it is exposed through the project conversation/query contract, then S1 can render the FR35-shaped detected-intent metadata from the captured task-intent record without browser-side text parsing, model/tool invocation, raw message body exposure, prompt/output exposure, or duplicate ad hoc detected-intent DTOs. [Source: _bmad-output/implementation-artifacts/3-11-informational-actionable-classification-ai-summary-distinction-and-review-history.md#Existing-Code-To-Reuse; src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationDetectedIntent.cs]
4. Given duplicate or replayed mailbox/conversation events, when detection is reprocessed, then task-intent capture is idempotent by stable tenant/project/source-message/kernel/evidence identity and converges to one observable record per equivalent detection; changed evidence or kernel version creates a superseding record rather than mutating historical evidence. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR13; #NFR14; _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
5. Given association correction or derived-context invalidation affects a source message that produced task intent, when the corrected-context readiness flag is not current, then open task-intent records using stale evidence are blocked from Epic 4 conversion and stamped with correction lineage/readiness metadata; closed historical records remain immutable. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR91a; _bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md#Current-State-To-Preserve]
6. Given the A9a evaluation dataset or fixture scaffold, when detection quality is measured, then tests compute precision and recall for task/action intent labels and preserve the M0 target as precision >= 80% and recall >= 75%, with M1 ratchet values documented as precision >= 90% and recall >= 85%; the story must not claim the full A9a corpus is present when only the Story 1.13 scaffold exists. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.1; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#A9a; tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json]
7. Contract, OpenAPI/generated-client, server kernel/capture/projection, idempotency, correction-readiness, redaction/isolation, and focused evaluation tests prove the data contract, capture behavior, safe exposure, replay convergence, leakage prevention, and precision/recall reporting. [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]

## Tasks / Subtasks

- [x] Define the task-intent contract surface without duplicating S1 detected-intent DTOs (AC: 1, 3, 7)
  - [x] Add purpose-named contract records under `src/Hexalith.ChatBot.Contracts/Queries/` and/or `Commands/` for the durable task-intent record and capture payload, for example `TaskIntentRecord`, `TaskIntentSourceEvidenceOffset`, and `CaptureTaskIntent`.
  - [x] Reuse `ProjectConversationDetectedActionKind` for `detected_action_kind` unless a stronger reason appears; it already serializes as `request-information`, `request-action`, `request-decision`, and `inform-only`.
  - [x] Define a stable task-intent state vocabulary that covers the initial captured/rejected/blocked posture and leaves room for Story 4.2 terminal states: `not-actionable`, `duplicate`, `already-handled`, and `out-of-scope`. Do not implement conversion/disposition behavior in this story.
  - [x] Include source-evidence offsets as metadata-only offset ranges/tokens. Do not persist raw email body, subject, HTML, provider payload, prompt text, tool arguments, or file content in the task-intent record.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` through the existing generation flow, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` if the public contract changes.
- [x] Add the deterministic task-intent detection/capture kernel in the Governance AI-mediation seam (AC: 1, 2, 4, 5)
  - [x] Add implementation under `src/Hexalith.ChatBot.Server/Governance/AiMediation/` (create the folder if absent) rather than UI, Client, Projections-only, or a sibling submodule.
  - [x] The M0 kernel should be deterministic metadata/rule based over already authorized project conversation input. It must not call an AI provider, embedding provider, external tool, Folders content read, or broad document-intelligence service.
  - [x] Inputs must include tenant id from authenticated/server context, project id, source message id/conversation item id, requester party id, safe source evidence refs/offsets, redaction state, retention class, source version, correlation id, policy snapshot id if available, and corrected-context readiness.
  - [x] Output must include the FR35 data contract fields, stable reason/message code, kernel version, schema version, source provenance, redaction state, retention class, source version, and correlation id.
  - [x] Capture must fail closed when tenant scope, requester party, source message authorization, project authorization, audit readiness, or corrected-context readiness is unresolved. Prefer a typed rejection record/message-catalog code over exceptions.
- [x] Persist and project task-intent records through existing ChatBot patterns (AC: 1, 3, 4, 5, 7)
  - [x] If capture writes durable state, route it through the CommandGateway as an `IChatBotCommand`; do not let a projection, UI service, or worker mutate durable state directly.
  - [x] Add EventStore payload/rejection types under `src/Hexalith.ChatBot.Server/Governance/AiMediation/` or an adjacent aggregate location consistent with `Operations/` and `Association/`; model business-rule failures as rejections, not thrown exceptions.
  - [x] Add projection/view support so the project conversation read can surface captured task intent. Extend `ProjectConversationItemView` and `Program.cs` `ToContractItem` as the S1 mapping chokepoints.
  - [x] Replace the current placeholder `BuildDetectedIntent()` derivation for true captured task-intent rows with captured record data, while preserving the safe fallback for existing non-Epic-4 classification rows where no task-intent record exists yet.
  - [x] Ensure task-intent projection keys include tenant and project and are source-version stamped, idempotent, duplicate-safe, and order-tolerant.
- [x] Add idempotency, correction-readiness, and audit metadata (AC: 2, 4, 5, 7)
  - [x] Compose the capture idempotency key from normalized tenant id, project id, source message id, requester party id, detection kernel version, detected action kind, and normalized evidence offsets/references.
  - [x] Duplicate/replayed equivalent detections must return the same observable record id/status. Changed evidence or a new kernel version should produce a superseding record with `supersedes`/`superseded_by` links rather than editing old evidence.
  - [x] Link task-intent records to association/correction lineage where available. If `IsCorrectedContextStale` or correction propagation says context is not current, block conversion-readiness and expose a safe next action such as `wait-for-correction-propagation`.
  - [x] Include audit-envelope metadata fields required by architecture: actor, command/operation name, resource id, decision, reason code, correlation id, timestamp, policy snapshot id, source evidence refs, state transition, redaction decision, and outcome.
- [x] Extend evaluation fixture support and quality reporting (AC: 6, 7)
  - [x] Reuse `Hexalith.ChatBot.Testing` fixture loader/validator patterns and the existing A9a scaffold at `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json`.
  - [x] Add task-intent labels/cases only as scaffold data unless a real A9a corpus is provided; preserve the `isScaffold` truth and do not make release-quality claims from scaffold data.
  - [x] Add a deterministic precision/recall calculator over labeled fixture cases. Tests must assert the M0 target constants are present and that the report clearly distinguishes measured scaffold values from release-gate values.
  - [x] Include adversarial cases for cross-tenant reference, unauthorized project, attachment-only message, informational-only message, risky AI candidate, duplicate message, and corrected/stale evidence.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract/OpenAPI/generated-client tests for exact wire values, required fields, summary length limit, confidence range, UTC `detected_at`, source evidence offset serialization, additive compatibility, and no raw payload fields.
  - [x] Server kernel tests for positive detection, informational-only non-capture, unsupported-but-manual safe handling, missing requester party, missing tenant/project authorization, redacted source, stale corrected context, missing audit readiness, confidence clamp/rejection, and message-catalog reason codes.
  - [x] Projection/read tests for captured intent appearing in `ProjectConversationDetectedIntent`, placeholder fallback preserved for non-captured rows, ETag changes on task-intent projection, 304 stability when unchanged, and no browser-side text parsing requirement.
  - [x] Idempotency/order tests for duplicate mailbox delivery, replayed conversation event, out-of-order correction/readiness events, new kernel version supersession, and stale event rejection.
  - [x] Isolation/leakage tests proving unauthorized, foreign-tenant, nonexistent, redacted, and malformed source contexts produce indistinguishable safe output and never leak raw mail body, subject, provider payload, prompt, tool args, tenant ids in denial bodies, file names/paths, secrets, or raw exceptions.
  - [x] Evaluation tests proving precision/recall computation, scaffold-vs-corpus distinction, required task-intent label coverage, and preservation of A9a M0/M1 target constants.

## Dev Notes

### Scope Boundaries

- This story owns FR35 task-intent detection and the durable data contract needed by Story 4.2 review/conversion. It may add contract records/enums, OpenAPI/generated-client updates, a deterministic server kernel, capture command/event/rejection/projection support, S1 query exposure, fixture/evaluation reporting, and focused tests.
- This story must not implement Story 4.2 review UI or disposition controls, Story 4.3 risk classification, Story 4.4 low-risk AI execution, Story 4.5 S3 approval surface, Story 4.6 preview/inspection, Story 4.7 allowlisted command execution, Story 4.8 refusal behavior beyond safe detection rejections, Story 4.9 invalidation of AI action proposals, outbound email, CLI/MCP parity, tenant policy editor UI, or model/tool invocation.
- Detection is M0 deterministic. Do not call an LLM to detect task intent in this story. If later stories add AI assistance, they must consume this captured record and governed AI-context package, not bypass it.

### Existing Code To Reuse

- Existing detected-intent display contract: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationDetectedIntent.cs`.
- Existing detected action enum: `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationDetectedActionKind.cs`.
- S1 item contract and reserved detected-intent field: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`.
- Server projection mapping chokepoints: `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` and `src/Hexalith.ChatBot.Server/Program.cs` `ToContractItem`.
- Current placeholder detector: `ProjectConversationItemView.BuildDetectedIntent()` derives intent from safe next action for display only. Keep this as fallback, but captured task-intent records must become the source for true FR35 records.
- Existing AI-context package producer from Story 3.14: `src/Hexalith.ChatBot.Contracts/Queries/ProjectAiContextPackage.cs`, `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/ProjectAiContextPackageAssembler.cs`, and `IProjectConversationProjectionStore.ReadAiContextPackageItemsAsync`.
- Gateway and durable write patterns: `src/Hexalith.ChatBot.Contracts/Commands/IChatBotCommand.cs`, `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`, `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`, `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`, and association rejection/event patterns under `src/Hexalith.ChatBot.Server/Association/`.
- Evaluation fixture scaffold: `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json`, `src/Hexalith.ChatBot.Testing/Fixtures/`, and `tests/Hexalith.ChatBot.Testing.Tests/Fixtures/TenantScopedFixtureManifestTests.cs`.

### Current State To Preserve

- Story 3.11 intentionally added `ProjectConversationDetectedIntent` as a read/display placeholder and explicitly did not implement FR35 task-intent capture. Do not treat the placeholder summary (`intent:...`) as a durable task-intent record.
- Story 3.14 now exposes an inspectable, metadata-only AI-context package. Story 4.1 may reference package readiness metadata, but must not invoke models/tools or consume file content.
- Project conversation reads already enforce tenant/project read scope, safe not-found denials, stable ETags, 304 conditional reads, and metadata-only AI-context package assembly in `Program.cs`. Preserve these behaviors when adding task-intent projection data.
- DAPR/EventStore delivery is at-least-once and unordered. Projection handlers and stores must remain idempotent, source-version stamped, and order-tolerant.
- Existing worktree already has unrelated `Hexalith.Tenants` submodule changes and untracked story-automator output. Do not revert or include them as part of this story.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. Public DTOs live in `.Contracts`; the kernel, capture orchestration, projections, and authorization live in `.Server`. UI maps generated client contracts only.
- Every state mutation must enter via CommandGateway: `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> pre-commit-audit -> EventStore -> post-commit-audit`. Do not create a side-channel write from a worker/projection.
- Tenant id comes from authenticated/server context, not request body, route, query string, provider payload, or UI-supplied data. Unknown, foreign, stale, revoked, redacted, or degraded context fails closed.
- Use `System.Text.Json`, JSON camelCase, additive serialization-tolerant schema evolution, UTC `DateTimeOffset`, ULID-compatible stable identifiers, and central package management. Do not add package upgrades for this story.
- Logs, traces, support artifacts, fixture output, and user-visible errors must be metadata-only and message-catalog backed. Raw mail content, subject lines, provider payloads, prompts, completions, tool args, file names/paths, tenant names, secrets, and raw exceptions are stop-ship leakage defects.
- Root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - Targeted compiled xUnit v3 runners for Contracts, Client generation, Server kernel/projection, Testing fixture/evaluation, Architecture, and Conformance/isolation.
- Sandbox note inherited from Story 3.14: `dotnet test` via VSTest can fail here with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 test DLLs with `-parallel none` for targeted validation after build.
- Add broad validation only if the public contract or S1 read path changes: Contracts, Client, Server, Conformance, UI service/model tests, and E2E only if UI rendering changes.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`, including the full Epic 4 story sequence and Story 4.1 FR35 acceptance criteria.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR35-FR38, A9a, NFR1-NFR15, NFR21, and the command/query contract inventory naming `CaptureTaskIntent`, `MarkTaskIntentDisposition`, and `GetTaskIntentStatus`.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially risk classifier constraints, command allowlist boundaries, tenant policy schema, shared command pipeline, and idempotency-key rules.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially Governed AI Mediation, CommandGateway flow, fail-closed rules, derived-record shape, audit envelope, project structure, FR35-FR46 location under `Server/Governance/{AiMediation,RiskClassifier,Approval,Allowlist}`, and A9a gate semantics.
- Loaded previous-story intelligence from Story 3.11 and Story 3.14. Story 3.11 created display-only detected-intent fields and warned not to implement task conversion; Story 3.14 created the metadata-only AI-context package Epic 4 will consume.
- Inspected source to confirm reuse points: `ProjectConversationDetectedIntent`, `ProjectConversationDetectedActionKind`, `ProjectConversationItem.DetectedIntent`, `ProjectConversationItemView.BuildDetectedIntent`, `Program.cs` project conversation read/mapping, CommandGateway service registration, and the A9a scaffold fixture.
- Latest-technology research not required: this story is constrained to repo-pinned .NET SDK `10.0.302`, `net10.0`, Dapr/Aspire/EventStore patterns, OpenAPI/NSwag generation, and xUnit v3; no external package upgrade or new third-party API is part of scope.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.1, FR coverage map, cross-cutting UX/architecture inheritance.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR35-FR38, A9a, NFR1-NFR15, NFR21, command/query inventory.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Risk Classifier, Command Allowlist v0/v1, Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys.
- `_bmad-output/planning-artifacts/architecture.md` - Governed AI Mediation, CommandGateway flow, data architecture, audit envelope, project structure/boundaries, testing standards.
- `_bmad-output/implementation-artifacts/3-11-informational-actionable-classification-ai-summary-distinction-and-review-history.md` - existing detected-intent display contract and explicit FR35 boundary.
- `_bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md` - AI-context package manifest, correction/readiness learnings, validation notes.
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationDetectedIntent.cs` - existing S1 detected-intent DTO.
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationDetectedActionKind.cs` - existing action-kind wire enum.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` - current placeholder classification/detected-intent mapping.
- `src/Hexalith.ChatBot.Server/Program.cs` - project conversation read, ETag handling, AI-context package assembly, and `ToContractItem` mapping.
- `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json` - current A9a fixture scaffold.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story workflow executed on 2026-06-01 with user-provided Story ID `4.1` and `#YOLO`.
- Workflow activation resolved with no prepend/append steps and persistent facts from sibling `project-context.md` files.
- Sprint status read fully; `epic-4` and `4-1-task-intent-detection-and-data-contract` started as `backlog`.
- Checklist validation applied during story creation; no user input requested.
- Dev-story workflow executed on 2026-06-01. Existing `baseline_commit: a471d8f` preserved.
- Build regenerated NSwag client from `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and refreshed generated-client SHA fixture.
- Validation used compiled xUnit v3 runners because repository notes warn `dotnet test`/VSTest can fail in this sandbox.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story file created for Story 4.1 and sprint status advanced to `ready-for-dev`; Epic 4 advanced to `in-progress`.
- Validation pass confirmed no user input was required and no external package/version research was needed for this repo-pinned story.
- Added metadata-only durable task-intent contracts, state vocabulary, source-evidence offsets, and `CaptureTaskIntent` command while reusing `ProjectConversationDetectedActionKind`.
- Added deterministic M0 task-intent kernel under `Server/Governance/AiMediation`, including fail-closed rejection/block codes, stable idempotency keys, correction-readiness blocking, and EventStore payload/rejection types.
- Extended project conversation projections so captured task-intent records populate `ProjectConversationDetectedIntent`; preserved the existing placeholder fallback for non-captured rows.
- Extended the A9a scaffold with task-intent expected/predicted labels and precision/recall reporting, preserving `isScaffold` and M0/M1 target constants.
- Validation passed: solution build, Contracts, Client, Server, Testing, Architecture, Conformance, UI.Tests, Workers.Tests, Aspire.Tests, AppHost.Tests, and ServiceDefaults.Tests compiled xUnit v3 runs.

### File List

- `_bmad-output/implementation-artifacts/4-1-task-intent-detection-and-data-contract.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/CaptureTaskIntent.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/TaskIntentState.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentRecord.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentSourceEvidenceOffset.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/DeterministicTaskIntentKernel.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentCaptureRejected.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentCaptured.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentDetectionRequest.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentDetectionResult.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentIdempotency.cs`
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/TaskIntentReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedTaskIntentEvent.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionEndpoints.cs`
- `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TaskIntentEvaluationCalculator.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TaskIntentEvaluationReport.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedEvaluationDataset.cs`
- `src/Hexalith.ChatBot.Testing/Fixtures/TenantScopedFixtureConstants.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/TaskIntentContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/DeterministicTaskIntentKernelTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.Testing.Tests/Fixtures/TenantScopedFixtureManifestTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
- `tests/fixtures/story-1-13-tenant-scoped-evaluation-dataset.json`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes.

Findings fixed:

- HIGH: `TaskIntentCaptured` durable events had no projection subscriber, so successful captures could be persisted without flowing into the project conversation read model required by AC3 and AC7. Added `TaskIntentProjectionHandler`, endpoint registration, and envelope translator with metadata validation.
- HIGH: `DeterministicTaskIntentKernel` could capture a task-intent record with no source evidence offsets, violating AC1/AC2 evidence preservation and fail-closed behavior. Added `task_intent_source_evidence_unresolved` rejection and test coverage.
- MEDIUM: Equivalent duplicate `CaptureTaskIntent` commands returned a domain rejection instead of an idempotent no-op, which made replay behavior observable as a failure rather than convergence. Changed duplicate handling to `DomainResult.NoOp()` and added aggregate coverage.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -class Hexalith.ChatBot.Server.Tests.Governance.AiMediation.DeterministicTaskIntentKernelTests -class Hexalith.ChatBot.Server.Tests.Operations.GovernedOperationAggregateTests -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests`
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
- `tests/Hexalith.ChatBot.Testing.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Testing.Tests -parallel none`
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -class Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests`
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none`
- `git diff --check`

---

Reviewer: Jerome on 2026-06-10 (story-automator adversarial re-review)

Outcome: Approved. No critical or high findings; one medium test-completeness gap auto-fixed.

Context: Story 4.1 (commit `d483a69`) is the Epic 4 foundation; 131 later commits (4.2-7.27, then the 3.x UI stories) build on it and remain green. All seven ACs were re-validated against the current code, and the story File List matches the 4.1 commit exactly (no git-vs-story discrepancies in source files; the only uncommitted source change is an additional isolation test already listed in the File List).

Findings fixed:

- MEDIUM: The Task 5 subtask "Server kernel tests for ... missing requester party, redacted source, confidence clamp/rejection" was marked `[x]`, but those kernel fail-closed branches had no kernel-level test (they were only exercised indirectly). Kernel logic was correct; coverage was missing. Added `MissingRequesterPartyShouldFailClosedWithoutCapture`, `RedactedOrUnavailableSourceShouldFailClosedWithoutCapture` (redacted/unavailable), and `OutOfRangeConfidenceShouldFailClosedWithoutCapture` (>1, <0, NaN, +Inf) to `DeterministicTaskIntentKernelTests`, covering `MissingRequesterParty`, `RedactedSource`, and `InvalidConfidence`.

Findings verified, no fix required:

- AC1-AC7 confirmed implemented: metadata-only `TaskIntentRecord` with the full FR35 field set; fail-closed kernel + capture handler with tenant id sourced from the authenticated envelope (not the request body); 280-char summary limit and `[0,1]` confidence enforced on the command path; SHA-256 idempotency key with kernel-version supersession and duplicate `NoOp` convergence; correction-readiness `Block` exposing `wait-for-correction-propagation`; projection translator with metadata-token validation and summary-length/confidence guards; deterministic precision/recall calculator preserving M0 (>=0.80 / >=0.75) and M1 (>=0.90 / >=0.85) targets plus `isScaffold` truth.
- Retained the working-tree isolation test `ProjectConversationEndpointShouldOmitDetectedIntentWhenTaskIntentCaptureFailsClosed` (redacted/non-actionable source omits `detectedIntent` and leaks no raw payload); it passes.

Validation (compiled xUnit v3, `-parallel none`):

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — Build succeeded, 0 Warning(s), 0 Error(s).
- Server.Tests: 251 passed; Contracts.Tests: 480 passed; Testing.Tests: 41 passed; Architecture.Tests: 39 passed; Conformance.Tests: 87 passed. 0 failures across all suites.

### Change Log

- 2026-06-01: Implemented Story 4.1 task-intent contract, deterministic capture kernel, projection integration, idempotency/correction-readiness metadata, evaluation scaffold reporting, OpenAPI/client regeneration, and validation coverage.
- 2026-06-01: Senior developer review auto-fixed task-intent projection delivery, evidence fail-closed behavior, and duplicate capture replay semantics; story approved.
- 2026-06-10: Story-automator adversarial re-review (Jerome). Auto-fixed a medium kernel test-coverage gap by adding missing-requester-party, redacted/unavailable-source, and out-of-range-confidence fail-closed kernel tests. Re-validated all ACs; build clean and targeted Server/Contracts/Testing/Architecture/Conformance suites green. Status remains done.
