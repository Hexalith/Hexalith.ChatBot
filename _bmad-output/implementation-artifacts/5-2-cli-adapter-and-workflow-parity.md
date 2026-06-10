---
baseline_commit: 9fd74ec
---

# Story 5.2: CLI adapter and workflow parity

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As a developer/automation builder,
I want a CLI that performs the governed workflow over the same backend,
so that I can script operations without bypassing governance.

## Acceptance Criteria

1. Given the CLI uses `System.CommandLine` and wraps `Hexalith.ChatBot.Client`, when I run CLI operations, then I can inspect unresolved/associated workflow state, associate, reject, defer, correct, retry, approve, execute, check operation status, and query audit history for the governed workflow with the same ordered candidates, evidence fields, status/reason codes, and redaction semantics as the UI. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Command and Query Contracts`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow 6 - Developer uses CLI parity`]
2. Given a CLI command submits state-changing work, when it reaches the ChatBot backend, then the adapter constructs only typed `IChatBotCommand` instances, calls `IChatBotClient.SubmitAsync(..., origin: ChatBotSurfaceOrigin.Cli)`, and never references Dapr, EventStore, `Hexalith.ChatBot.Server`, gateway stages, risk classification, approval gate, audit writer, idempotency store, projection stores, or sibling data-plane clients directly. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `src/Hexalith.ChatBot.Client/IChatBotClient.cs`; `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`]
3. Given a delayed audit projection, when a CLI command is accepted but reconciliation is not complete, then CLI output is a clear partial-success result containing operation ID, command ID, correlation ID, lifecycle state, completion status `accepted-projection-pending`, audit status, retry count, safe next actions, and terminal/failure reason fields when present; it must not print a false full-reconciliation success. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2`; `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback matrix`]
4. Given stale credentials, a tenant switch, revoked/expired/wrong-surface CLI service-client scope, unknown operation/resource IDs, or cross-tenant resource attempts, when the CLI invokes a command or query, then it fails closed with catalog-backed metadata-only output and does not reveal restricted project names, candidate evidence, file metadata, audit details, command payloads, tenant data, grant secrets, bearer tokens, raw claims, or provider payloads. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2-NFR7`; `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md#Current State To Preserve`]
5. Given the CLI supports delegated or service-client operation, when it sends commands or reads status/audit, then it uses the existing service-client grant and Keycloak/OIDC evidence foundation from Story 5.1; it does not implement local authorization, role inheritance, tenant inference from request bodies, token parsing shortcuts, or service-client grant decisions inside the CLI. [Source: `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md#Dev Notes`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsServiceClientGrantResolver.cs`; `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json`]
6. Given acceptance coverage runs, then tests prove the CLI project is in the `.slnx`, uses central package management with repo-pinned `System.CommandLine` 2.0.8, depends only on `Hexalith.ChatBot.Client` plus allowed host/service-default abstractions, maps every state-changing command to `ChatBotSurfaceOrigin.Cli`, exposes metadata-only redacted output for denials, preserves partial-success output, and is covered by adapter-boundary NetArchTest/source fitness rules. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Decisions Provided by the Platform Starter`; `Directory.Packages.props`; `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessAssemblies.cs`]

## Tasks / Subtasks

- [x] Add the CLI adapter project and tests to the solution (AC: 1, 2, 6)
  - [x] Create `src/Hexalith.ChatBot.Cli/Hexalith.ChatBot.Cli.csproj` targeting the repo default `net10.0`, with package reference to `System.CommandLine` and project reference to `src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj`; no inline package versions.
  - [x] Create `tests/Hexalith.ChatBot.Cli.Tests/Hexalith.ChatBot.Cli.Tests.csproj` with xUnit v3, Shouldly, NSubstitute as needed, and a project reference to the CLI project.
  - [x] Add both projects to `Hexalith.ChatBot.slnx`.
  - [x] Update `tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj` to reference the CLI project so `FitnessAssemblies.Adapters` can load `Hexalith.ChatBot.Cli.dll`.
  - [x] Extend `ScaffoldArchitectureTests.SolutionShouldContainRequiredSourceAndTestProjects` so the required project list includes CLI source/tests.
- [x] Implement a thin command model over the existing client facade (AC: 1, 2, 5)
  - [x] Create a focused CLI composition root, for example `Program.cs`, `ChatBotCliCommands.cs`, `ChatBotCliOptions.cs`, `ChatBotCliService.cs`, and `ChatBotCliOutputFormatter.cs`.
  - [x] Register commands for the MVP parity set currently available through contracts/client: inspect/list status via `GetProjectConversationAsync`, association evidence/status via `GetAssociationRoutingStatusAsync`, task review via `GetTaskIntentReviewAsync`, audit via `GetOperationAuditHistoryAsync`, status via `GetOperationStatusAsync`, and state-changing commands via typed `IChatBotCommand` records.
  - [x] Map state-changing operations to existing command records: `AssociateEmailToProject`, `RejectEmailProjectAssociation`, `DeferEmailProjectAssociation`, `CorrectEmailProjectAssociation`, `RequestFailedWorkflowRetry`, `DecideAiActionApproval`, and `ExecuteApprovedAIAction`. Do not invent alternate command DTOs or direct REST payload builders.
  - [x] Pass `origin: ChatBotSurfaceOrigin.Cli` on every `SubmitAsync` call. Reads may pass correlation/task IDs but must not fabricate tenant authority.
  - [x] Keep command names stable and script-friendly, for example `association status`, `association associate`, `association reject`, `association defer`, `association correct`, `operation retry`, `approval decide`, `ai-action execute`, `operation status`, and `operation audit`.
- [x] Preserve credential and tenant-source boundaries (AC: 4, 5)
  - [x] Treat CLI-supplied `--tenant`, if added, only as display/filter intent or configuration selection; never send tenant authority in command payloads unless that specific contract field already represents a target resource owned by the command.
  - [x] Use the generated/typed client authentication path selected by app configuration or host wiring; do not parse JWTs locally to make authorization decisions.
  - [x] Never log or print access tokens, refresh tokens, client secrets, raw JWTs, raw OAuth assertions, raw claim sets, raw command JSON, or unrestricted provider errors.
  - [x] Ensure service-client grant failures from the backend are surfaced as safe denial categories and client actions, not as stack traces or raw server payloads.
- [x] Implement deterministic output formatting for scripts and humans (AC: 1, 3, 4)
  - [x] Add a default metadata-only text/table output that includes stable IDs, state, reason code, correlation ID, safe next action, and redaction state where available.
  - [x] Add `--json` output for automation using `System.Text.Json` and the repo's shared enum/string converter patterns; do not inline ad hoc `JsonSerializerOptions` if an existing shared options factory is available.
  - [x] For operation status, explicitly distinguish `accepted-projection-pending`, `completed`, and `failed`; include `AuditStatus` (`committed` or `reconciling`) and `SafeNextActions`.
  - [x] Collapse unauthorized/not-found/cross-tenant failures into the same safe public shape the generated client/server returns; no extra CLI "helpful" lookup should confirm hidden resources.
- [x] Add adapter-boundary and parity tests (AC: all)
  - [x] Unit-test command parsing maps each CLI verb to the intended `IChatBotCommand` type or client read method.
  - [x] Unit-test every state-changing CLI path calls `IChatBotClient.SubmitAsync` with `ChatBotSurfaceOrigin.Cli`.
  - [x] Unit-test operation status formatting for `accepted-projection-pending` plus `reconciling` audit status so it reports partial success and does not print "done"/"success" as the final outcome.
  - [x] Unit-test safe denial formatting for stale credential, revoked grant, wrong surface, tenant mismatch, safe-not-found, and validation error cases with no restricted project/file/audit/secret text.
  - [x] Extend architecture/source tests so `.Cli` cannot reference `Hexalith.ChatBot.Server`, `Gateway`, `Gateway.Stages`, `DaprClient`, EventStore contracts, `AuditEnvelope`, `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, `IIdempotencyStore`, sibling clients, or projection stores.
  - [x] Add a focused client/CLI parity fixture that compares CLI output fields for association status/candidates against the same `IChatBotClient` response fields used by UI service tests.
- [x] Keep public contracts and generated client disciplined (AC: 1, 6)
  - [x] Prefer existing `IChatBotClient` methods and generated client types. Only change `openapi/hexalith.chatbot.v1.yaml` if a required backend read is truly missing.
  - [x] If OpenAPI changes are required, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
  - [x] Do not add the MCP adapter, MCP tool exposure tags, cross-surface full conformance harness, outbound sender-authority behavior, tenant policy editor UI, or FR74 disable/quarantine/rate-limit controls in this story.

## Dev Notes

### Scope Boundaries

- This story implements the production CLI adapter only. MCP remains Story 5.3, and the full cross-surface equivalence harness remains Story 5.4.
- The CLI is a surface adapter, not a governance layer. It must not duplicate authentication, tenant binding, authorization, risk classification, approval gating, idempotency, audit writing, service-client grant validation, command allowlist checks, or projection mutation.
- Story 5.1 already added service-client classes/grants, Keycloak service-account fixture clients, internal grant validation, audit evidence, and fail-closed reason codes. Reuse that server-side behavior; do not move it into CLI code.
- `ChatBotSurfaceOrigin.Cli` is provenance and audit attribution. It is not an authorization shortcut.
- The CLI should support both human/developer and automation usage, but command output must remain redacted and metadata-only under denial or restricted visibility.

### Existing Code To Reuse

- Client facade and generated contracts:
  - `src/Hexalith.ChatBot.Client/IChatBotClient.cs`
  - `src/Hexalith.ChatBot.Client/ChatBotClient.cs`
  - `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/IChatBotCommand.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ServiceClientClass.cs`
- Existing commands likely needed by CLI parity:
  - `src/Hexalith.ChatBot.Contracts/Commands/AssociateEmailToProject.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/RejectEmailProjectAssociation.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/DeferEmailProjectAssociation.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/CorrectEmailProjectAssociation.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/RequestFailedWorkflowRetry.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/DecideAiActionApproval.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/ExecuteApprovedAIAction.cs`
- Existing query/status contracts:
  - `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/OperationCompletionStatus.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/OperationAuditStatus.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationResponse.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/TaskIntentReview.cs`
- Existing UI service pattern for client-only adapters:
  - `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
  - `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- Existing architecture enforcement:
  - `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessAssemblies.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DependencyDirectionFitnessTests.cs`
- Story 5.1 service-client foundation:
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsServiceClientGrantResolver.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs`
  - `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json`

### Current State To Preserve

- `src/Hexalith.ChatBot.Cli` does not exist yet.
- `Directory.Packages.props` already pins `System.CommandLine` to `2.0.8`. Use that central package; do not add inline versions.
- `IChatBotClient` already exposes `SubmitAsync`, `GetOperationStatusAsync`, `GetOperationAuditHistoryAsync`, `GetAssociationRoutingStatusAsync`, `GetProjectConversationAsync`, and `GetTaskIntentReviewAsync`.
- No dedicated `ListUnresolvedAssociations`/queue read method was found on `IChatBotClient`. Flow 6 requires listing unresolved associations; if existing project-conversation or association-routing reads are insufficient, add a minimal contract-first query endpoint through OpenAPI/generated client and the server projection layer. Do not fake list output inside the CLI or query projection stores directly.
- `ChatBotClient.SubmitAsync` already validates command type names, normalizes correlation/task ULIDs, creates a `CommandId`, and maps `ChatBotSurfaceOrigin.Cli` to the generated wire enum.
- Server command submission already resolves surface origin from request body/header and audits it. The CLI should declare the origin through the client body path, not rely on a custom header.
- `FitnessAssemblies` already knows the future `.Cli` adapter suffix; once the architecture test project references CLI, IL boundary tests will include it.
- `ScaffoldArchitectureTests` already scans `.Cli` source paths for forbidden server/governance/idempotency/audit/Dapr/EventStore tokens.

### Architecture Guardrails

- CLI project references should be limited to `Hexalith.ChatBot.Client` and any approved host/service-default support needed for configuration/HTTP client setup. Do not reference `Hexalith.ChatBot.Contracts` directly unless it arrives transitively through Client or is needed to construct typed command records; if direct Contracts reference is used, keep it low-dependency and still avoid Server.
- State-changing CLI verbs must build typed command records and submit through `IChatBotClient.SubmitAsync`. They must not call `/api/v1/commands` via raw `HttpClient` with hand-built JSON.
- Read-only CLI verbs must use `IChatBotClient` read methods. They must not query Dapr state, projection stores, EventStore, sibling bounded contexts, or server internals.
- Keep aggregate and gateway concepts out of CLI naming. The CLI should speak workflow terms, operation IDs, status, audit, association, approval, and safe next actions.
- Treat all IDs as stable metadata. Preserve ULID validation behavior from the client facade and surface validation failures as safe CLI errors.
- Preserve root submodule policy: initialize/update only root `.gitmodules` submodules and never use recursive submodule commands.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Cli.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Cli.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
- Run focused server tests if any service-client denial mapping, problem details, or operation status behavior changes:
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
- Sandbox note inherited from previous stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Do not add Playwright; this story adds a CLI adapter and should not touch visible UI surfaces.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Story 5.2 is the CLI adapter over the governed workflow and follows Story 5.1 service-client grant work.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially RBAC, Service Client Permissions, Command and Query Contracts, FR80-FR86, NFR2-NFR7, and cross-surface parity outcomes.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially System.CommandLine 2.0.x, Contract Spine, `IChatBotClient`, CommandGateway, adapter-only-through-client rule, NetArchTest boundaries, and repo-pinned `.NET 10`/xUnit/Shouldly/NSubstitute patterns.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`, especially Command Surface Reference, Flow 6 CLI parity, partial-success state, safe denial language, and audit semantics.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant carry-forward: .NET SDK `10.0.300`, `net10.0`, central package management, warnings-as-errors, metadata-only diagnostics, tenant isolation, Keycloak/OIDC, pure EventStore aggregates, Dapr duplicate/order tolerance, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md`; story 5.1 completed the service-client foundation and validated the full build plus Contracts, Client, Server, Architecture, Conformance, AppHost, Aspire, ServiceDefaults, Testing, UI, Workers, and Integration suites.
- Inspected current code and tests for likely update surfaces: `IChatBotClient`, `ChatBotClient`, command/query contracts, server operation status/audit endpoints, UI service client-only pattern, `ScaffoldArchitectureTests`, adapter NetArchTest rules, `Directory.Packages.props`, and `Hexalith.ChatBot.slnx`.
- Recent git history shows Story 5.1 landed at `9fd74ec feat(story-5.1): Service-client identities and scoped grants`, followed by Epic 4 completion and stories 4.4-4.9.
- Latest-technology web research was not required for story creation: this story should use repo-pinned `System.CommandLine` 2.0.8 and existing client/server contracts, with no new external API or package version decision.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 5 and Story 5.2 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - Service Client Permissions, Command and Query Contracts, FR80-FR86, NFR2-NFR7.
- `_bmad-output/planning-artifacts/architecture.md` - System.CommandLine pin, Contract Spine, adapter boundaries, CommandGateway, and test standards.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - Flow 6, partial success, safe denial, command-surface reference, and audit semantics.
- `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md` - previous-story service-client foundation and validation notes.
- `Directory.Packages.props` - `System.CommandLine` 2.0.8 and central package versions.
- `Hexalith.ChatBot.slnx` - source/test project registration.
- `src/Hexalith.ChatBot.Client/IChatBotClient.cs` - adapter-facing client facade.
- `src/Hexalith.ChatBot.Client/ChatBotClient.cs` - command submission and surface-origin mapping.
- `src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs` - closed surface-origin enum.
- `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs` - operation status/partial-success output shape.
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs` - existing client-only surface adapter pattern.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` - source/project boundary rules.
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessAssemblies.cs` - adapter assembly discovery including future CLI.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-02: Built the CLI adapter against repo-pinned `System.CommandLine` 2.0.8 and existing `IChatBotClient`; no OpenAPI or generated-client changes were required.
- 2026-06-02: Validation passed:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Cli.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Cli.Tests -parallel none` (16 passed)
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` (15 passed)
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` (36 passed)
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` (58 passed)
- 2026-06-10: Re-ran dev-story validation. Story was already `done`, no unchecked task or review-follow-up boxes remained, and no implementation changes were required. Validation passed:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` (0 warnings, 0 errors)
  - `./tests/Hexalith.ChatBot.Cli.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Cli.Tests -parallel none` (22 passed)
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` (34 passed)
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` (39 passed)
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` (87 passed)
  - Remaining compiled regression runners passed: AppHost (5), Aspire (2), Contracts (480), Integration (16 passed, 2 skipped Tier-3 Docker/Dapr-gated), MCP (25), Server (1557), ServiceDefaults (5), Testing (41), UI E2E (80), UI (131), Workers (30).
- 2026-06-10: Story-automator review applied safe-invoke hardening so local validation (`Required`) and decision-parse (`ParseApprovalDecision`) `ArgumentException`s — plus any `InvalidOperationException` — fail closed as redacted metadata-only denials (exit code 1) before a command is submitted, instead of propagating as unhandled exceptions. Added `ChatBotCliService.RunSafelyAsync`, a top-level `InvokeCoreAsync` exception net, and the `InvokeSafelyAsync` command wrapper. Validation passed:
  - `dotnet build Hexalith.ChatBot.slnx -m:1 /nr:false` (0 warnings, 0 errors)
  - `./tests/Hexalith.ChatBot.Cli.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Cli.Tests -parallel none` (24 passed)
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` (34 passed)
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` (39 passed)
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` (87 passed)

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added `Hexalith.ChatBot.Cli` as a thin System.CommandLine adapter over `IChatBotClient`, with state-changing commands constructing typed `IChatBotCommand` records and submitting with `ChatBotSurfaceOrigin.Cli`.
- Added deterministic text and JSON output for accepted submissions, operation status/audit, association status, project conversation, task review, and safe denial responses.
- Added CLI tests for parser-to-command mapping, origin attribution, client read routing, partial-success formatting, safe denial redaction, and association candidate output parity.
- Extended architecture tests and solution registration so the CLI adapter is built, loaded by adapter fitness tests, and checked for server/governance/data-plane boundary violations.
- 2026-06-10 validation rerun confirmed Story 5.2 remains complete with no unchecked tasks and no code changes required.
- 2026-06-10 story-automator review hardened the fail-closed path: local validation and decision-parse failures (and any `InvalidOperationException`) are now surfaced through `ChatBotCliService.RunSafelyAsync` and a top-level `InvokeCoreAsync` net as redacted metadata-only denials before submission, with two added tests (`CliInvocationRedactsTypedProblemDetailsAtReadBoundary`, `CliInvocationRedactsLocalValidationFailuresBeforeSubmittingCommands`). CLI suite is now 24 tests.

### File List

- `Hexalith.ChatBot.slnx`
- `_bmad-output/implementation-artifacts/5-2-cli-adapter-and-workflow-parity.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Cli/ChatBotCliCommands.cs`
- `src/Hexalith.ChatBot.Cli/ChatBotCliOptions.cs`
- `src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs`
- `src/Hexalith.ChatBot.Cli/ChatBotCliService.cs`
- `src/Hexalith.ChatBot.Cli/Hexalith.ChatBot.Cli.csproj`
- `src/Hexalith.ChatBot.Cli/Program.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`
- `tests/Hexalith.ChatBot.Cli.Tests/Hexalith.ChatBot.Cli.Tests.csproj`

### Senior Developer Review (AI)

Reviewer: Codex on 2026-06-02

Outcome: Approved after auto-fixes. No critical issues remain.

Findings fixed:

- [HIGH] Accepted command output reported `accepted-projection-pending` but did not include the full required partial-success metadata (`operationId`, audit status, retry count, and terminal/failure fields). Fixed `ChatBotCliOutputFormatter.FormatCommandAccepted` and pinned with CLI JSON tests.
- [HIGH] Safe denial formatting collapsed typed backend `ProblemDetails` into generic HTTP buckets, losing catalog code/category/client action metadata. Fixed the formatter to consume `HexalithChatBotApiException<ProblemDetails>` safely without printing raw response bodies.
- [MEDIUM] Association text output omitted reason-code, candidate evidence, and redaction/visibility/freshness metadata that the UI service preserves. Fixed default text output and added coverage for ordered candidate evidence metadata.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` (passed)
- `./tests/Hexalith.ChatBot.Cli.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Cli.Tests -parallel none` (22 passed)
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` (15 passed)
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` (36 passed)
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` (58 passed)

Reviewer: Jerome on 2026-06-10 (story-automator adversarial review)

Outcome: Approved. No critical or high issues remain.

Scope reviewed: uncommitted working-tree changes to `src/Hexalith.ChatBot.Cli/ChatBotCliCommands.cs`, `src/Hexalith.ChatBot.Cli/ChatBotCliService.cs`, and `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs` (safe-invoke hardening).

Assessment:

- All 6 acceptance criteria remain implemented and verified by tests plus the passing architecture/boundary suite. The change strengthens AC4 (fail-closed, metadata-only redaction) for local validation and decision-parse failures.
- Adapter boundary (AC2/AC5) re-checked: no forbidden Server/Gateway/Dapr/EventStore/risk/approval/audit/idempotency/projection tokens in CLI source; the `Program.cs` `HttpClient` is composition-root transport for the generated client only.

Findings:

- [MEDIUM] Hardening changes were undocumented in the story (Change Log/Completion Notes/Dev Agent Record), and the prior 2026-06-10 entry claimed "no implementation changes were required." Fixed by documenting the change and its validation evidence.
- [LOW] Validation evidence reported "Cli.Tests (22 passed)"; the suite is now 24 with two added redaction tests. Fixed by recording the 24-test count.
- [LOW][Observation, not changed] Safe-denial exceptions are now caught at three layers (per-service-method `catch when(IsSafeClientFailure)`, `RunSafelyAsync`, and the top-level `InvokeCoreAsync` net). This is correct defense-in-depth; the inner per-method catches are partly redundant with `RunSafelyAsync`. Left intact to avoid regressing tested behavior for no functional gain.

Validation (working tree):

- `dotnet build Hexalith.ChatBot.slnx -m:1 /nr:false` (0 warnings, 0 errors)
- `./tests/Hexalith.ChatBot.Cli.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Cli.Tests -parallel none` (24 passed)
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` (34 passed)
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` (39 passed)
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` (87 passed)

### Change Log

- 2026-06-02: Implemented Story 5.2 CLI adapter and workflow parity over the existing client facade; added CLI, tests, architecture boundary checks, solution registration, and validation evidence.
- 2026-06-02: Senior developer review auto-fixed CLI partial-success metadata, catalog-backed safe-denial formatting, association evidence/reason text output, and added focused tests; story approved.
- 2026-06-10: Validation-only dev-story rerun confirmed all Story 5.2 tasks remain complete and regression tests pass.
- 2026-06-10: Story-automator review hardened the CLI fail-closed path (local validation/decision-parse and `InvalidOperationException` now produce redacted metadata-only denials before submission via `RunSafelyAsync` plus a top-level invoke net), added two redaction tests (CLI suite now 24), and documented the previously-undocumented working-tree changes. Approved; no critical issues.
