---
baseline_commit: 73847b5
---

# Story 5.3: MCP adapter and governed tool surface

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As an AI agent/automation tool,
I want governed MCP tools over the same workflow,
so that machine actors operate through the same authorized command model.

## Acceptance Criteria

1. Given the MCP server uses repo-pinned `ModelContextProtocol` 1.4.0 and wraps `Hexalith.ChatBot.Client`, when an authorized AI/automation client invokes an MCP tool, then it can access the governed workflow operations exposed for MCP: association status/associate/reject/defer/correct, task review, operation status/audit/retry, approval decision, approved AI-action execution, and project conversation reads where the client grant permits the query. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.3`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR83`; `src/Hexalith.ChatBot.Client/IChatBotClient.cs`; `Directory.Packages.props`]
2. Given tool exposure is governed, when MCP tool descriptors are registered, then every exposed tool is explicitly tagged or described as `mcp-exposed`, maps to a bounded command/query name already authorized by service-client grants, and omits any command/query that is not intended for machine use. Unknown tool names, wrong-surface grants, revoked/expired credentials, over-scoped/under-scoped grants, tenant mismatch, and unauthorized arguments fail closed without revealing restricted resource existence. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.3`; `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md`; `src/Hexalith.ChatBot.Contracts/Identities/ServiceClientGrant.cs`; `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json`]
3. Given an MCP tool submits state-changing work, when the adapter calls the ChatBot backend, then it constructs only typed `IChatBotCommand` records and calls `IChatBotClient.SubmitAsync(..., origin: ChatBotSurfaceOrigin.Mcp)`; it must not reference Dapr, EventStore, `Hexalith.ChatBot.Server`, gateway stages, risk classification, approval gate, audit writer, idempotency store, projection stores, sibling data-plane clients, or raw `/api/v1/commands` payload builders. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `src/Hexalith.ChatBot.Client/ChatBotClient.cs`; `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`]
4. Given an unknown or unauthorized tool/argument is invoked, when MCP returns the error/result, then the response uses metadata-only safe denial shape with stable category/code/message/correlation/task/client-action/redaction visibility fields and a safe suggestion such as the nearest allowed tool name or required argument category; it never includes project names, candidate evidence, file metadata, audit internals, command payloads, bearer tokens, raw claims, provider payloads, or stack traces. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2-NFR7`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Voice and Tone`; `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`]
5. Given an MCP tool triggers long-running work or projection reconciliation, when the adapter returns, then it reports operation ID, command ID, correlation ID, lifecycle state, completion status `accepted-projection-pending` when applicable, audit status, retry count, safe next actions, terminal/failure reason fields when present, and does not claim full success while audit/projection is still reconciling. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR80`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR26`; `_bmad-output/implementation-artifacts/5-2-cli-adapter-and-workflow-parity.md#Current State To Preserve`]
6. Given acceptance coverage runs, then tests prove the MCP project and tests are registered in the `.slnx`, use central package management with repo-pinned `ModelContextProtocol` 1.4.0, depend only on `Hexalith.ChatBot.Client` plus approved host/service-default support if needed, submit every state-changing tool with `ChatBotSurfaceOrigin.Mcp`, expose metadata-only redacted tool errors, and are covered by adapter-boundary NetArchTest/source fitness rules. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`; `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessAssemblies.cs`]

## Tasks / Subtasks

- [x] Add the MCP adapter project and tests to the solution (AC: 1, 3, 6)
  - [x] Create `src/Hexalith.ChatBot.Mcp/Hexalith.ChatBot.Mcp.csproj` targeting repo default `net10.0`, with `PackageReference Include="ModelContextProtocol"` and a project reference to `src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj`; if ASP.NET Core MCP transport requires a split package in the resolved SDK, add the matching central `PackageVersion` under the `MCP` group in `Directory.Packages.props`, never an inline version.
  - [x] Create `tests/Hexalith.ChatBot.Mcp.Tests/Hexalith.ChatBot.Mcp.Tests.csproj` with xUnit v3, Shouldly, NSubstitute, and a project reference to the MCP project.
  - [x] Add both projects to `Hexalith.ChatBot.slnx`.
  - [x] Update `tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj` to reference the MCP project so `FitnessAssemblies.Adapters` loads `Hexalith.ChatBot.Mcp.dll`.
  - [x] Extend `ScaffoldArchitectureTests.SolutionShouldContainRequiredSourceAndTestProjects` and add a `ChatBotMcpAdapterMustDependOnlyOnClientFacadeAndNeverServerOrDataPlaneInternals` test mirroring the CLI rule with MCP-specific package expectations.
- [x] Implement MCP tool registration as a thin adapter over `IChatBotClient` (AC: 1, 2, 3)
  - [x] Add focused MCP composition and service files, for example `Program.cs`, `ChatBotMcpTools.cs`, `ChatBotMcpToolMetadata.cs`, `ChatBotMcpService.cs`, and `ChatBotMcpResultFormatter.cs`.
  - [x] Use the repo-pinned ModelContextProtocol ASP.NET Core hosting/DI pattern; register `IChatBotClient` through the generated client/facade path, not through raw `HttpClient` command payload construction.
  - [x] Expose a stable MVP tool set matching current contracts/client capability: association status, associate, reject, defer, correct, project conversation read, task review, operation retry, approval decide, AI action execute, operation status, and operation audit.
  - [x] Map state-changing tools to existing command records: `AssociateEmailToProject`, `RejectEmailProjectAssociation`, `DeferEmailProjectAssociation`, `CorrectEmailProjectAssociation`, `RequestFailedWorkflowRetry`, `DecideAiActionApproval`, and `ExecuteApprovedAIAction`.
  - [x] Call `IChatBotClient.SubmitAsync` with `ChatBotSurfaceOrigin.Mcp` on every state-changing tool. Read tools must use only existing `IChatBotClient` read methods.
  - [x] Keep tool names stable and machine-friendly, for example `chatbot.association.status`, `chatbot.association.associate`, `chatbot.association.reject`, `chatbot.association.defer`, `chatbot.association.correct`, `chatbot.conversation.get`, `chatbot.task.review`, `chatbot.operation.retry`, `chatbot.approval.decide`, `chatbot.ai_action.execute`, `chatbot.operation.status`, and `chatbot.operation.audit`.
- [x] Enforce `mcp-exposed` governance and argument boundary validation (AC: 2, 4)
  - [x] Add an MCP-owned allowlist/metadata table for exposed tool names, required arguments, command/query contract name, and `mcp-exposed` marker. This is adapter exposure metadata only; it must not replace `ChatBotSpineCommandAllowlist` or service-client grants.
  - [x] Validate required arguments at the MCP boundary before constructing commands; reject unknown tools, missing required arguments, ambiguous aliases, invalid enum values, unsupported command/query names, and unrecognized identifiers with safe metadata-only errors.
  - [x] Suggestions must be safe: nearest exposed tool name, required argument key, allowed enum token, or "check operation status"; never suggest hidden project names, candidate evidence, policy internals, grant contents, or audit details.
  - [x] Treat MCP-supplied tenant or project context only as target/filter input where the underlying command/query contract already accepts it; never infer tenant authority from tool arguments.
  - [x] Do not parse JWTs, service-client claims, grant IDs, or scopes locally to make authorization decisions. The backend grant validator from Story 5.1 remains authoritative.
- [x] Implement safe MCP result formatting (AC: 4, 5)
  - [x] Return structured results using `System.Text.Json` and existing generated/client contract shapes where possible; avoid one-off serializers or raw exception bodies.
  - [x] For accepted commands, include operation ID, command ID, correlation ID, task ID, lifecycle state, completion status, audit status, retry count, safe next actions, and nullable terminal/failure reason fields.
  - [x] For operation status, preserve `accepted-projection-pending`, `completed`, and `failed` as distinct states and include `AuditStatus`, `SafeNextActions`, retry count, and terminal/failure reason fields.
  - [x] For denials and validation failures, emit catalog-backed metadata only: category, code, safe message, correlation ID, task ID when present, retryable, client action, details visibility, and safe suggestion.
  - [x] Never log or return access tokens, refresh tokens, client secrets, raw JWTs, raw OAuth assertions, raw claim sets, raw MCP request payloads containing sensitive data, raw backend response bodies, unrestricted provider errors, project names on denial, file metadata on denial, candidate evidence on denial, or audit internals on denial.
- [x] Wire hosting without adding data-plane sidecars or bypass routes (AC: 1, 3, 6)
  - [x] If the MCP server is hosted as a separate Aspire project, add it to `src/Hexalith.ChatBot.AppHost/Program.cs` with a reference to the ChatBot server and Keycloak wait/reference as needed; do not attach a Dapr sidecar unless a later architecture decision explicitly requires one.
  - [x] Configure authentication using the same Keycloak/OIDC service-client posture from Story 5.1 and the existing `mcp-tool-client` realm fixture; do not reuse the public `hexalith-chatbot` UI/test client.
  - [x] Preserve the existing Dapr deny-by-default access-control policy. MCP reaches ChatBot through `IChatBotClient` over HTTP/service discovery, not Dapr pub/sub/state/service invocation.
- [x] Add MCP adapter and safety tests (AC: all)
  - [x] Unit-test every state-changing tool maps to the intended `IChatBotCommand` type and calls `SubmitAsync(..., ChatBotSurfaceOrigin.Mcp, ...)`.
  - [x] Unit-test read tools call only `IChatBotClient` read methods and never submit commands.
  - [x] Unit-test unknown tool, missing argument, unauthorized argument, wrong enum token, stale credential, revoked grant, wrong surface, tenant mismatch, and safe-not-found formatting with no restricted text.
  - [x] Unit-test accepted command and operation status result shapes for partial-success/audit-reconciling behavior.
  - [x] Add source/architecture rules so `.Mcp` cannot reference `Hexalith.ChatBot.Server`, `Gateway`, `Gateway.Stages`, `DaprClient`, EventStore contracts, `AuditEnvelope`, `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, `IIdempotencyStore`, sibling clients, projection stores, or raw command endpoint builders.
  - [x] Add or update AppHost/realm tests proving the MCP surface uses `mcp-tool-client`, `ServiceClientClass.McpTool`, and surface origin `mcp`.
  - [x] Add a focused parity fixture comparing MCP and CLI construction for at least association status, association decision, retry, approval decision, AI action execution, operation status, and operation audit. The full UI/CLI/MCP differential harness wiring remains Story 5.4.
- [x] Keep public contracts disciplined (AC: all)
  - [x] Prefer existing `IChatBotClient` methods and generated client types. Only change `openapi/hexalith.chatbot.v1.yaml` if a required backend read for MCP parity is truly missing.
  - [x] If OpenAPI changes are required, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
  - [x] Do not implement the full cross-surface equivalence harness, outbound sender-authority behavior, tenant policy editor UI, command allowlist v1 lifecycle, FR74 disable/quarantine/rate-limit controls, or custom AI provider/tool execution in this story.

## Dev Notes

### Scope Boundaries

- This story implements the production MCP adapter and its governed tool surface only. Story 5.4 owns the full UI/CLI/MCP differential-conformance harness wiring.
- MCP is a surface adapter, not an AI/tool governance layer. It must not duplicate authentication, tenant binding, authorization, risk classification, approval gating, command allowlist, idempotency, audit writing, grant validation, projection mutation, or sibling integration.
- `ChatBotSurfaceOrigin.Mcp` is provenance and audit attribution. It is not sufficient authorization; Story 5.1 service-client grants must authorize `ServiceClientClass.McpTool`, the `mcp` surface, and the command/query set.
- `mcp-exposed` is adapter exposure metadata. It narrows which tools the adapter offers but does not authorize by itself.
- No visual UI surface is added. UX obligations are semantic: safe language, actor/audit attribution, partial-success behavior, and no bypass claims.

### Existing Code To Reuse

- Client facade and surface provenance:
  - `src/Hexalith.ChatBot.Client/IChatBotClient.cs`
  - `src/Hexalith.ChatBot.Client/ChatBotClient.cs`
  - `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigins.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ServiceClientClass.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/ServiceClientClasses.cs`
- Existing commands likely needed by MCP parity:
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
- Existing CLI adapter pattern to mirror where useful:
  - `src/Hexalith.ChatBot.Cli/ChatBotCliCommands.cs`
  - `src/Hexalith.ChatBot.Cli/ChatBotCliService.cs`
  - `src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs`
  - `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`
- Service-client foundation:
  - `src/Hexalith.ChatBot.Contracts/Identities/ServiceClientGrant.cs`
  - `src/Hexalith.ChatBot.Contracts/Identities/ServiceClientGrantEvidence.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ClaimsServiceClientGrantResolver.cs`
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs`
  - `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json`
- Architecture/conformance enforcement:
  - `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessAssemblies.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DependencyDirectionFitnessTests.cs`
  - `tests/Hexalith.ChatBot.Conformance.Tests/Harness/SurfaceArms.cs`
  - `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationActorMatrix.cs`

### Current State To Preserve

- `src/Hexalith.ChatBot.Mcp` and `tests/Hexalith.ChatBot.Mcp.Tests` do not exist yet.
- `Directory.Packages.props` already pins `ModelContextProtocol` to `1.4.0` (the repo-wide pin advanced from 1.3.0 to 1.4.0 in a separate dependency-maintenance commit; the architecture fitness test asserts 1.4.0). Use central package management; do not add inline versions or perform a package upgrade in this story. If a separate ASP.NET Core transport package is required, pin it centrally under the existing `MCP` package group and keep it on the same repo-approved version family.
- `ChatBotSurfaceOrigin.Mcp`, generated `SurfaceOrigin.Mcp`, `ServiceClientClass.McpTool`, and the `mcp-tool-client` Keycloak fixture already exist. Reuse them; do not introduce alternate enum values, actor classes, or client IDs.
- `IChatBotClient` already exposes `SubmitAsync`, `GetOperationStatusAsync`, `GetOperationAuditHistoryAsync`, `GetAssociationRoutingStatusAsync`, `GetProjectConversationAsync`, and `GetTaskIntentReviewAsync`.
- `ChatBotClient.SubmitAsync` already maps `ChatBotSurfaceOrigin.Mcp` to the generated wire enum and normalizes command IDs, correlation IDs, task IDs, and command type names.
- Story 5.2 added CLI project/tests and proved the adapter-only pattern. The MCP implementation should mirror the successful boundaries without copying CLI text output concerns directly into MCP protocol concerns.
- `FitnessAssemblies` already includes the future `Mcp` adapter suffix. Once architecture tests reference `.Mcp`, IL-level adapter boundary rules will cover it automatically.
- The conformance harness already has shim `McpSurfaceArm` coverage. Do not replace Story 5.4's planned full harness with broad work here; add focused construction parity only.
- Existing worktree has unrelated modified `Hexalith.Tenants` submodule state and `_bmad-output/story-automator/orchestration-4-20260601-145742.md`; do not revert or include them in implementation.

### Architecture Guardrails

- MCP project references should be limited to `Hexalith.ChatBot.Client` and approved host/service-default support if hosting requires it. Do not reference `Hexalith.ChatBot.Server` or sibling clients.
- State-changing tools must build typed command records and submit through `IChatBotClient.SubmitAsync`. They must not call raw REST endpoints, Dapr, EventStore, projection stores, or gateway stages.
- Read-only tools must use `IChatBotClient` read methods. They must not query Dapr state, EventStore streams, projection stores, sibling bounded contexts, or server internals.
- Keep aggregate, gateway, risk, approval, audit, idempotency, and grant concepts out of MCP tool implementation except as returned metadata from backend responses.
- Tool names and argument names are part of an automation contract. Keep them stable, lowercase, namespaced, and explicit.
- Treat all IDs as stable metadata. Preserve client facade validation behavior and return safe MCP errors for invalid IDs.
- Preserve root submodule policy: initialize/update only root `.gitmodules` submodules and never use recursive submodule commands.

### Project Structure Notes

- Place MCP adapter code under `src/Hexalith.ChatBot.Mcp/`.
- Place MCP tests under `tests/Hexalith.ChatBot.Mcp.Tests/`.
- Keep exposure metadata in MCP adapter code, for example `ChatBotMcpToolMetadata`, not in Server gateway internals.
- Keep generic command/query contracts under `src/Hexalith.ChatBot.Contracts` only if they are truly public cross-surface contracts. MCP-only host concerns belong in `.Mcp`.
- Keep AppHost wiring in `src/Hexalith.ChatBot.AppHost/Program.cs` only if this story chooses a separate hosted MCP project. Do not modify Dapr ACLs unless tests prove a new sidecar route is intentionally required.
- Keep architecture rules in `tests/Hexalith.ChatBot.Architecture.Tests`; do not rely on code review to enforce adapter boundaries.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Mcp.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Mcp.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -parallel none` if AppHost/Keycloak/MCP hosting changes.
- Run focused server tests if service-client denial mapping, problem details, realm fixtures, or operation status behavior changes:
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
- Sandbox note inherited from previous stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Do not add Playwright; MCP has no visible UI surface.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 5 is Cross-Surface Parity and Story 5.3 requires an MCP adapter over the same governed workflow, restricted to `mcp-exposed` commands and the shared command model.
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR80-FR86, NFR2-NFR7, NFR11, NFR26, NFR32-NFR34, and NFR65-NFR70.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially `ModelContextProtocol` 1.3.x, Contract Spine, `IChatBotClient`, CommandGateway, adapter-only-through-client rule, NetArchTest boundaries, and repo-pinned `.NET 10`/xUnit/Shouldly/NSubstitute patterns.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`, especially Command Surface Reference, actor badge/audit attribution, partial success, safe denial language, and no UI bypass affordance.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant carry-forward: SDK `10.0.302`, `net10.0`, central package management, warnings-as-errors, metadata-only diagnostics, tenant isolation, Keycloak/OIDC, pure EventStore aggregates, Dapr duplicate/order tolerance, MCP/CLI wrap typed clients, xUnit v3/Shouldly/NSubstitute, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/5-2-cli-adapter-and-workflow-parity.md`; Story 5.2 completed the CLI adapter, validated the build plus CLI, Client, Architecture, and Conformance suites, and auto-fixed partial-success metadata, typed safe-denial formatting, and association evidence output.
- Loaded Story 5.1 intelligence from `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md`; the service-client foundation already includes `mcp-tool-client`, `ServiceClientClass.McpTool`, grant validation, fail-closed reason codes, and audit evidence.
- Inspected current code and tests for likely update surfaces: `IChatBotClient`, `ChatBotClient`, command/query contracts, surface/service-client enums, Keycloak realm, AppHost wiring, CLI adapter implementation/tests, `ScaffoldArchitectureTests`, adapter NetArchTest rules, `FitnessAssemblies`, conformance surface arms, `Directory.Packages.props`, and `Hexalith.ChatBot.slnx`.
- Recent git history shows Story 5.2 landed at `73847b5 feat(story-5.2): CLI adapter and workflow parity`, preceded by Story 5.1 at `9fd74ec`.
- Latest-technology web research was not required for story creation: this story should use repo-pinned `ModelContextProtocol` 1.4.0 and existing client/server contracts, with no new external API or package version decision.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 5 and Story 5.3 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR80-FR86, NFR2-NFR7, NFR11, NFR26, NFR32-NFR34, NFR65-NFR70.
- `_bmad-output/planning-artifacts/architecture.md` - ModelContextProtocol pin, Contract Spine, CommandGateway, adapter boundary, metadata-only diagnostics, project structure, and tests.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - Command Surface Reference, partial success, safe denial, actor/audit attribution.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - command surface visual/semantic consistency and actor badge categories.
- `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md` - MCP service-client grant foundation and validation notes.
- `_bmad-output/implementation-artifacts/5-2-cli-adapter-and-workflow-parity.md` - previous-story CLI adapter pattern and review fixes.
- `Directory.Packages.props` - `ModelContextProtocol` 1.4.0 and central package versions.
- `Hexalith.ChatBot.slnx` - source/test project registration.
- `src/Hexalith.ChatBot.Client/IChatBotClient.cs` - adapter-facing client facade.
- `src/Hexalith.ChatBot.Client/ChatBotClient.cs` - command submission and surface-origin mapping.
- `src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs` - closed surface-origin enum including `Mcp`.
- `src/Hexalith.ChatBot.Contracts/Enums/ServiceClientClass.cs` - service-client class enum including `McpTool`.
- `src/Hexalith.ChatBot.Contracts/Identities/ServiceClientGrant.cs` - grant command/query/surface authorization evidence.
- `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json` - service-account client fixture including `mcp-tool-client`.
- `src/Hexalith.ChatBot.Cli/ChatBotCliService.cs` - existing thin adapter service pattern.
- `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs` - adapter tests for origin attribution and safe denial formatting.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` - source/project boundary rules.
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessAssemblies.cs` - adapter assembly discovery including future MCP.
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/SurfaceArms.cs` - current UI/CLI/MCP semantic intent shims.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-10 re-validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed with 0 warnings/errors.
- 2026-06-10 re-validation: `./tests/Hexalith.ChatBot.Mcp.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Mcp.Tests -parallel none` - passed, 25 tests.
- 2026-06-10 re-validation: `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 34 tests.
- 2026-06-10 re-validation: `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 39 tests.
- 2026-06-10 re-validation: `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 87 tests.
- 2026-06-10 re-validation: `./tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -parallel none` - passed, 5 tests.
- 2026-06-10 re-validation: additional compiled regression runners passed: Aspire 2, Contracts 480, ServiceDefaults 5, Testing 41, Workers 30, UI 131, Server 1557, UI.E2E 80.
- 2026-06-10 re-validation: `./tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -parallel none` - passed, 18 tests total with 2 Tier-3 Aspire/Dapr tests skipped by environment gate.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `./tests/Hexalith.ChatBot.Mcp.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Mcp.Tests -parallel none` - passed, 25 tests.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 15 tests.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 37 tests.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 58 tests.
- `./tests/Hexalith.ChatBot.AppHost.Tests/bin/Debug/net10.0/Hexalith.ChatBot.AppHost.Tests -parallel none` - passed, 4 tests.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implemented the MCP stdio adapter using repo-pinned `ModelContextProtocol` 1.4.0 and the generated `IChatBotClient` facade path.
- Added an MCP-owned `mcp-exposed` metadata catalog, argument boundary validation, safe metadata-only denial formatting, and structured partial-success command/status results.
- Added unit, architecture, AppHost realm, and focused CLI/MCP parity coverage for governed MCP tool construction and safety behavior.
- No OpenAPI or generated client changes were required.

### QA Results

- 2026-06-10: QA generate E2E/tests gap pass added MCP descriptor discovery semantics coverage and catalog-to-attributed-method argument parity coverage; refreshed the workflow test summary. Validation passed: solution build 0 warnings/errors; MCP 27/27, Client 34/34, Architecture 39/39, Conformance 87/87, and AppHost 5/5.
- 2026-06-02: QA automation gap pass added focused MCP coverage for exact tool-catalog contract allowlisting, malformed numeric/list argument fail-closed behavior before command submission, and typed backend `ProblemDetails` metadata-only safe denial formatting.
- 2026-06-02: Senior developer review auto-fixed MCP direct-invocation JSON object/list argument validation so non-text identifiers and non-string list members fail closed before command submission without echoing restricted payload content.
- Validation passed: solution build 0 warnings/errors; MCP 25/25, Client 15/15, Architecture 37/37, Conformance 58/58, and AppHost 4/4.

### Senior Developer Review (AI)

- Reviewer: Claude (story-automator adversarial review) on 2026-06-10.
- Outcome: Approved after auto-fix; no critical issues remain. Re-validated build (0 warnings/errors) and runners: MCP 30/30, Architecture 39/39, Client 34/34, Conformance 87/87, AppHost 5/5.
- Finding fixed (HIGH): Read-tool results serialized the generated DTOs with `System.Text.Json`, which ignores their Newtonsoft `StringEnumConverter`/`[EnumMember]` attributes, so `chatbot.association.status`, `chatbot.conversation.get`, `chatbot.task.review`, and `chatbot.operation.audit` emitted raw integer enum ordinals (e.g. `redactionState:0`, `lifecycleState:5`). This diverged from the governed wire-name strings `FormatOperationStatus` already produces and made the redaction-posture signal opaque and version-brittle. Added an `EnumMember`-honoring `JsonConverterFactory` to `ChatBotMcpResultFormatter.JsonOptions` so every read surface now emits stable wire names (`redactionState:"metadata_only"`, `lifecycleState:"NeedsReview"`, `reasonCodes:["explicit-project-identifier-matched"]`). Regression test: `ReadToolResultsEmitGovernedWireNameEnumsNotRawOrdinals`.
- Finding fixed (MEDIUM): `ChatBotMcpService.InvokeAsync` only caught a fixed allowlist of exception types (`IsSafeClientFailure`), so transport/timeout faults (`HttpRequestException`, `TaskCanceledException`) — which the generated client does not wrap — escaped raw to the MCP host, bypassing the AC4 metadata-only safe-denial contract and risking endpoint-detail leakage. Broadened the catch to route every non-cancellation failure through `FormatSafeDenial` (which never echoes the exception message) while rethrowing cooperative cancellation. Removed the now-dead `IsSafeClientFailure`. Regression tests: `TransportAndUnexpectedFailuresBecomeMetadataOnlySafeDenials`, `CooperativeCancellationPropagatesAndIsNotMaskedAsDenial`.
- Finding fixed (MEDIUM, documentation): Story AC/Dev-Notes/Completion-Notes claimed repo-pinned `ModelContextProtocol` 1.3.0, but the repo, the build, and the architecture fitness test all pin 1.4.0 (advanced in a separate dependency-maintenance commit). Reconciled the story narrative to 1.4.0; no package change made (downgrade is out of scope and the attribute usage aligns with 1.4.0).
- Reviewer: GPT-5 Codex on 2026-06-02.
- Outcome: Approved after auto-fix; no critical issues remain.
- Finding fixed: MCP boundary validation accepted arbitrary JSON object/array values for text/list arguments on direct service invocation paths, allowing malformed payloads to reach command construction instead of failing closed. Fixed `ChatBotMcpService` to validate each supplied argument shape before command construction and added regression coverage in `ChatBotMcpServiceTests`.
- MCP documentation check: Official MCP C# SDK docs/search confirmed the repo-pinned `ModelContextProtocol` package remains the official C# SDK path and supports stdio server transport/tool descriptors used by this adapter.
- Git/story alignment: Story File List matches the reviewed source/test/docs surface. Git also contains unrelated pre-existing modified submodule/orchestration artifacts that were not part of this review.
- Validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`; MCP 25/25; Client 15/15; Architecture 37/37; Conformance 58/58; AppHost 4/4.

### File List

- Hexalith.ChatBot.slnx
- src/Hexalith.ChatBot.Mcp/Hexalith.ChatBot.Mcp.csproj
- src/Hexalith.ChatBot.Mcp/Program.cs
- src/Hexalith.ChatBot.Mcp/ChatBotMcpInvocation.cs
- src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs
- src/Hexalith.ChatBot.Mcp/ChatBotMcpService.cs
- src/Hexalith.ChatBot.Mcp/ChatBotMcpToolMetadata.cs
- src/Hexalith.ChatBot.Mcp/ChatBotMcpTools.cs
- src/Hexalith.ChatBot.Mcp/McpToolDeniedException.cs
- tests/Hexalith.ChatBot.Mcp.Tests/Hexalith.ChatBot.Mcp.Tests.csproj
- tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs
- tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj
- tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs
- tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs
- _bmad-output/implementation-artifacts/5-3-mcp-adapter-and-governed-tool-surface.md
- _bmad-output/implementation-artifacts/tests/test-summary.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Change Log

- 2026-06-10: Adversarial review auto-fix — read-tool enum serialization now emits governed `EnumMember` wire names (was raw integer ordinals); transport/unexpected failures now route through the metadata-only safe-denial contract instead of escaping raw; reconciled `ModelContextProtocol` version references from 1.3.0 to the actual 1.4.0 pin. Added three MCP regression tests (read enum wire-names, transport-failure safe denial, cancellation propagation). MCP 30/30, Architecture 39/39, Client 34/34, Conformance 87/87, AppHost 5/5.
- 2026-06-10: QA generate E2E/tests pass added MCP descriptor semantics and argument-contract parity tests; refreshed test summary and validation evidence.
- 2026-06-02: Added the governed MCP adapter, MCP test project, solution registration, architecture/AppHost guardrails, and focused MCP/CLI parity and safety tests.
- 2026-06-02: QA automation pass added MCP catalog allowlist, invalid argument fail-closed, and typed safe-denial metadata tests; refreshed test summary and validation evidence.
- 2026-06-02: Senior developer review fixed direct MCP JSON argument shape validation and added regression coverage; story marked done.
