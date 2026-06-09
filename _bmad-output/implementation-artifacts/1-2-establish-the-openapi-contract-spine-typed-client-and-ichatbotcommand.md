---
baseline_commit: 09da357
---

# Story 1.2: Establish the OpenAPI Contract Spine, Typed Client, and IChatBotCommand

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-30. -->

## Story

As an adapter developer,
I want a single OpenAPI 3.1 Contract Spine with a generated typed client and an `IChatBotCommand` marker,
so that UI/CLI/MCP adapters bind to one contract source and cross-surface parity is structural.

## Acceptance Criteria

1. Given the Contract Spine decision D7, when the spine is created, then `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` is the single contract source, and the typed client in `src/Hexalith.ChatBot.Client/Generated/` is NSwag-generated from it and never hand-edited. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2; Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure]
2. Given the client surface, when an adapter submits a command, then it constructs only a typed `IChatBotCommand` and calls `IChatBotClient.SubmitAsync(...)`; adapters must not construct EventStore envelopes, call Dapr directly, or replicate any CommandGateway stage. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a]
3. Given shared contract types, when defined, then `LifecycleState`, `RiskClass`, `ActorType`, `ThresholdBand` enums and ULID-based identity helpers exist in `Contracts`; identifiers parse via `Ulid.TryParse` and never `Guid.TryParse`. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2; Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure]
4. Given an operation failure, when a problem response is returned, then it is RFC 9457 metadata-only with `{ category, code, message, correlationId, taskId?, retryable, clientAction, details.visibility }`, and no restricted project names, file metadata, candidate evidence, audit details, tenant data, payloads, secrets, or local paths. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR77; Source: RFC 9457 Problem Details for HTTP APIs]
5. Given contract naming conventions, when contract tests run, then commands are imperative with no `Command` suffix, events are past-tense with no `Event` suffix, and rejections follow `{Target}{Reason}Rejection : IRejectionEvent` with structured payloads only: IDs, enums, counts, booleans, timestamps, and bounded metadata, never localized display text. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2; Source: _bmad-output/planning-artifacts/architecture.md#Naming Patterns]
6. Given generated artifacts can drift, when `dotnet build Hexalith.ChatBot.slnx --no-restore` and focused client/contract tests run, then they prove the checked-in generated client matches the current Contract Spine and generation configuration, generated files contain safe provenance only, and generation does not depend on timestamps, absolute paths, network calls, Dapr, Aspire, Keycloak, Redis, production secrets, or nested submodule initialization. [Source: Hexalith.Folders/_bmad-output/implementation-artifacts/1-12-wire-nswag-sdk-generation-with-idempotency-helpers.md#Acceptance Criteria; Source: Hexalith.Folders/_bmad-output/project-context.md#Testing Rules]

## Tasks / Subtasks

- [x] Create the ChatBot Contract Spine foundation (AC: 1, 3, 4)
  - [x] Add `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` as OpenAPI `3.1.0` with `info`, `/api/v1` server guidance, OIDC/bearer security, shared headers, shared responses, shared schemas, and synthetic examples only.
  - [x] Encode the minimal command-submission contract needed for `IChatBotClient.SubmitAsync(...)` without implementing server runtime behavior, CommandGateway stages, domain aggregates, mailbox intake, UI, CLI, MCP, or workers.
  - [x] Include reusable schemas for `AcceptedCommand`, `ProblemDetails`, `ProblemDetailsDetails`, command submission request/response shapes, `LifecycleState`, `RiskClass`, `ActorType`, and `ThresholdBand`.
  - [x] Keep tenant authority out of request payloads, query parameters, and client-controlled headers; tenant authority comes from authenticated context and EventStore envelope later.
  - [x] Use OpenAPI specification extensions only with `x-hexalith-*` names when the spine needs Hexalith metadata; do not introduce sidecar-only contract metadata for adapter behavior.
- [x] Add shared contract C# types (AC: 2, 3, 4, 5)
  - [x] Add `src/Hexalith.ChatBot.Contracts/Commands/IChatBotCommand.cs` as the marker interface for state-mutating ChatBot commands.
  - [x] Add enum files under `src/Hexalith.ChatBot.Contracts/Enums/` for `LifecycleState`, `RiskClass`, `ActorType`, and `ThresholdBand` using stable wire names aligned to the OpenAPI schema.
  - [x] Add ULID identity helpers under `src/Hexalith.ChatBot.Contracts/Identities/`, for example `ChatBotIdentity` or focused value objects for command/task/correlation identifiers. They must validate with `Ulid.TryParse` and must not use `Guid.TryParse`.
  - [x] Add problem-response DTOs or records under `src/Hexalith.ChatBot.Contracts/Problems/` only if they are needed outside generated DTOs; keep behavior and serialization settings minimal and contract-centered.
  - [x] If a reference to `IRejectionEvent` is needed for tests, use the existing EventStore contract reference only in tests or a low-risk contract reference; do not add EventStore server/runtime dependencies to `Contracts`.
- [x] Wire deterministic NSwag client generation (AC: 1, 2, 6)
  - [x] Add `src/Hexalith.ChatBot.Client/nswag.json` with the Contract Spine file as its only input.
  - [x] Update `src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj` to generate into `src/Hexalith.ChatBot.Client/Generated/` before compile, using `NSwag.MSBuild` from central package management and no inline package versions.
  - [x] Use the Folders generation posture: `injectHttpClient=true`, `disposeHttpClient=false`, `generateSyncMethods=false`, `generateClientInterfaces=true`, `useBaseUrl=false`, stable namespace `Hexalith.ChatBot.Client.Generated`, LF generated output, and no environment-specific base URL.
  - [x] Keep generated files marked as generated and do not hand-edit them. Any hand-written facade, DI registration, options, or extensions belong outside `Generated/`.
  - [x] Add or update a hand-written `IChatBotClient` facade with `SubmitAsync(IChatBotCommand command, ...)` outside `Generated/`; it may wrap generated transport shapes but must not know Dapr, EventStore envelopes, or CommandGateway internals.
- [x] Add contract and client guardrail tests (AC: 1, 2, 3, 4, 5, 6)
  - [x] Add OpenAPI foundation tests under `tests/Hexalith.ChatBot.Contracts.Tests/` that parse the YAML, assert OpenAPI `3.1.0`, assert required shared schemas/headers/responses, resolve local `$ref` targets, and reject tenant authority fields in client-controlled inputs.
  - [x] Add tests proving `ProblemDetails` includes the RFC fields plus Hexalith fields and that examples are metadata-only and synthetic.
  - [x] Add tests proving identifiers use ULID parsing and that `Guid.TryParse` does not appear in ChatBot identity helper source.
  - [x] Add contract naming tests for imperative commands, past-tense events, and `{Target}{Reason}Rejection : IRejectionEvent` structured payloads.
  - [x] Replace the Story 1.1 conformance placeholder with a real oracle/fixture scaffold that records the current command-submission operation, expected adapter input shape, and metadata-only failure categories.
  - [x] Add client generation tests under `tests/Hexalith.ChatBot.Client.Tests/` that assert `nswag.json` points only to `../../src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, generated output exists under `Generated/`, the generated client compiles, and stale generated output is detectable by content hash or isolated regeneration.
- [x] Preserve architecture and repository guardrails (AC: 2, 5, 6)
  - [x] Keep `Contracts <- Client <- Server` dependency direction. Do not make `Contracts` reference `Server`, `Aspire`, `AppHost`, UI, CLI, MCP, Dapr runtime, or EventStore server/runtime packages.
  - [x] Do not modify sibling submodules. Use `Hexalith.Folders` and other siblings as read-only references only.
  - [x] Do not initialize or update nested submodules. If a root-level submodule is missing for local validation, use only non-recursive root-level commands such as `git submodule update --init`.
  - [x] Keep package versions centralized in `Directory.Packages.props`; project files must not contain inline `Version` attributes.
  - [x] Keep diagnostics, test failure messages, generated provenance, docs examples, and problem responses metadata-only.
- [x] Verify locally (AC: 1, 5, 6)
  - [x] Run `dotnet restore Hexalith.ChatBot.slnx`.
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore`.
  - [x] Run focused contract/client/conformance tests affected by this story.
  - [x] Record exact blockers in the Dev Agent Record if NSwag generation, restore, build, or tests are blocked by sandbox/runtime prerequisites rather than code defects.

## Dev Notes

### Implementation Intent

This story turns the Story 1.1 scaffold into the first real contract-centered slice. It does not implement the CommandGateway, domain command execution, idempotency persistence, audit writing, state transition enforcement, mailbox intake, association scoring, UI, CLI, MCP, workers, or governed AI. The deliverable is the contract source, generated client pipeline, marker types, and tests that make later adapter work bind to one spine. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2; Source: _bmad-output/planning-artifacts/architecture.md#Architecture implementation sequence]

### Current Files To Update

- `Directory.Packages.props`: already contains `NSwag.MSBuild 14.7.1`, `Newtonsoft.Json 13.0.4`, `Microsoft.AspNetCore.OpenApi`, `YamlDotNet`, xUnit v3, Shouldly, and related packages. Add package versions here only if a missing package is truly required. [Source: Directory.Packages.props; Source: Hexalith.Folders/Directory.Packages.props]
- `src/Hexalith.ChatBot.Contracts/Hexalith.ChatBot.Contracts.csproj`: currently has no project references and only `InternalsVisibleTo` for tests. Keep it low-dependency and behavior-free. [Source: src/Hexalith.ChatBot.Contracts/Hexalith.ChatBot.Contracts.csproj]
- `src/Hexalith.ChatBot.Contracts/ChatBotModuleInfo.cs`: currently exposes `ModuleName` and `DaprAppId`. Preserve these stable bootstrap identifiers. [Source: src/Hexalith.ChatBot.Contracts/ChatBotModuleInfo.cs]
- `src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj`: currently references Contracts only. This is the right place for NSwag generation targets and generated-client dependencies. [Source: src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj]
- `src/Hexalith.ChatBot.Client/ChatBotClientDescriptor.cs`: current descriptor uses contract identifiers. Preserve it or evolve it into the hand-written client registration/facade without pushing transport behavior into Contracts. [Source: src/Hexalith.ChatBot.Client/ChatBotClientDescriptor.cs]
- `tests/Hexalith.ChatBot.Conformance.Tests/ContractSpineOraclePlaceholderTests.cs` and `tests/fixtures/story-1-2-contract-spine-oracle.placeholder.json`: placeholders from Story 1.1. Replace or extend them with real story-owned contract-spine evidence. [Source: tests/Hexalith.ChatBot.Conformance.Tests/ContractSpineOraclePlaceholderTests.cs; Source: tests/fixtures/story-1-2-contract-spine-oracle.placeholder.json]
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`: already enforces solution shape, dependency direction, no inline package versions, CI root submodule policy, and no recursive submodule commands. Extend only if this story creates new boundary risks. [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]

### Canonical References

- Use `Hexalith.Folders` as the structural reference for Contract Spine and generation: `src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml`, `src/Hexalith.Folders.Client/nswag.json`, `src/Hexalith.Folders.Client/Generated/`, and the client generation tests. [Source: Hexalith.Folders/src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml; Source: Hexalith.Folders/src/Hexalith.Folders.Client/nswag.json; Source: Hexalith.Folders/tests/Hexalith.Folders.Client.Tests/ClientGenerationTests.cs]
- Borrow the Folders guardrail, not necessarily the full idempotency-helper generator. ChatBot Story 1.2 needs generated client freshness and command submission shape; two-altitude idempotency is Story 1.5. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.5; Source: Hexalith.Folders/_bmad-output/implementation-artifacts/1-12-wire-nswag-sdk-generation-with-idempotency-helpers.md#Scope Boundaries]
- Use `Hexalith.Conversations` as the domain-boundary reminder: store stable upstream IDs, do not duplicate Projects/Parties/Folders authority, and keep durable events free of upstream personal data. [Source: Hexalith.Conversations/_bmad-output/project-context.md#Critical Implementation Rules]

### Contract Spine Requirements

- The Contract Spine is OpenAPI `3.1.0`, despite the OpenAPI site now listing newer versions. Architecture explicitly selects OpenAPI 3.1, and the sibling Folders NSwag pipeline is already proven against that version. Do not upgrade the contract version in this story. [Source: _bmad-output/planning-artifacts/architecture.md#Contract Spine; Source: OpenAPI Specification v3.1.0]
- The OpenAPI 3.1 spec permits `x-` specification extensions and aligns Schema Object behavior with JSON Schema 2020-12. Use this for Hexalith metadata rather than duplicating contract semantics in docs only. [Source: OpenAPI Specification v3.1.0]
- Problem responses should preserve RFC 9457 standard members where applicable (`type`, `title`, `status`, `detail`, `instance`) and add the Hexalith canonical metadata fields required by the story. [Source: RFC 9457 Problem Details for HTTP APIs; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR77]
- The first operation shape should be the smallest useful command-submission contract for adapter parity. It should express `IChatBotCommand` submission and accepted/status metadata, not execute a real domain command. Story 1.9 owns the first governed command expressed through this contract. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2; Source: _bmad-output/planning-artifacts/epics.md#Story 1.9]

### Generated Client Requirements

- Generate under `src/Hexalith.ChatBot.Client/Generated/`, mark files as generated, and keep manual code outside that directory.
- Use NSwag from the existing central package version. The NSwag C# client generator supports generated C# clients/DTOs from an OpenAPI document and supports injected `HttpClient` with `UseBaseUrl=false`; match the Folders configuration unless a build failure proves a narrower adjustment is needed. [Source: NSwag CSharpClientGenerator documentation; Source: Hexalith.Folders/src/Hexalith.Folders.Client/nswag.json]
- `IChatBotClient.SubmitAsync(IChatBotCommand command, ...)` is the hand-written adapter-facing surface. Generated REST client types may be lower-level transport details, but UI/CLI/MCP later must call the facade and must not construct Dapr calls or EventStore envelopes directly. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a; Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
- Generated output must be deterministic and stale output must fail a focused test or build target. Avoid tests that compare raw generated diffs in public logs; report repository-relative paths, content hashes, and operation IDs only. [Source: Hexalith.Folders/_bmad-output/implementation-artifacts/1-12-wire-nswag-sdk-generation-with-idempotency-helpers.md#Generation Requirements]

### Contract Type Requirements

- `IChatBotCommand` is a marker only. It must not include gateway stage methods, authorization hooks, EventStore envelope metadata, Dapr types, or transport-specific properties.
- Enums and identity helpers live in `Contracts`, not `Client` or `Server`, because adapters need shared compile-time shape before the CommandGateway exists.
- ULID helpers must use `Ulid.TryParse`. Do not accept GUID-shaped shortcuts and do not add `Guid.TryParse` in identity helpers or tests.
- Rejection naming tests should scan only hand-written contract source and avoid generated/obj/bin directories. A future rejection must implement `IRejectionEvent` and carry structured payload values only.

### Previous Story Intelligence

- Story 1.1 scaffolded the solution, central package management, deny-by-default DAPR policy, AppHost/Aspire topology, root-level submodule guardrails, architecture tests, and conformance placeholders. It explicitly left the OpenAPI Contract Spine, generated client, and `IChatBotCommand` to Story 1.2. [Source: _bmad-output/implementation-artifacts/1-1-scaffold-the-buildable-hexalith-chatbot-module.md#Out of Scope for Story 1.1]
- Story 1.1 validation passed restore and no-restore build. Full `dotnet test` was green in normal execution, but a later sandbox run required direct xUnit v3 runners because VSTest TCP listener permissions were blocked. If this repeats, record the sandbox blocker and run the direct focused test executable when possible. [Source: _bmad-output/implementation-artifacts/1-1-scaffold-the-buildable-hexalith-chatbot-module.md#Debug Log References]
- Recent commit `09da357 feat(story-1.1): Scaffold the buildable Hexalith.ChatBot module` is the baseline for this story. The only unrelated dirty file observed during story creation was `_bmad-output/story-automator/orchestration-1-20260530-160445.md`; do not revert it. [Source: git log --oneline -5; Source: git status --short]

### Testing Requirements

- Tests must be deterministic and offline. They must not require Aspire, Dapr sidecars, Keycloak, Redis, provider credentials, tenant seed data, production secrets, live network calls, or nested submodule initialization. [Source: Hexalith.Folders/_bmad-output/project-context.md#Testing Rules]
- Use xUnit v3 and Shouldly. Prefer structured YAML parsing through `YamlDotNet.RepresentationModel` over ad hoc grep when validating OpenAPI. [Source: Hexalith.Folders/_bmad-output/project-context.md#Testing Rules]
- Good focused tests for this story: OpenAPI root/component/ref validation, metadata-only examples, no tenant-authority fields, generated-client config shape, generated output freshness, `IChatBotClient` facade signature, `IChatBotCommand` marker shape, ULID-only identity parsing, command/event/rejection naming, and no generated-file manual edits.
- Build verification is required with `dotnet restore Hexalith.ChatBot.slnx` and `dotnet build Hexalith.ChatBot.slnx --no-restore`. Run focused tests after build.

### Out of Scope

- Do not implement `CommandGateway`, gateway stages, risk classifier, approval gate, audit writer, idempotency store, EventStore aggregate dispatch, domain processors, or runtime command handling. Story 1.3 and later foundation stories own those.
- Do not add UI, CLI, MCP, workers, mailbox intake, association scoring, approval workflows, AI mediation, attachment handling, or operational dashboards.
- Do not copy the full Folders idempotency-helper generator unless the story needs it for a minimal command-submission contract. Full two-altitude idempotency belongs to Story 1.5.
- Do not modify sibling submodules, generated files by hand, nested submodule metadata, or root `.gitmodules`.
- Do not introduce TypeSpec, OpenAPI Generator, Kiota, Swashbuckle code-first source of truth, ABP, Clean Architecture templates, or a second UI/client stack.

### Project Structure Notes

- Alignment: `Contracts/openapi/` owns the canonical OpenAPI file; `Client/Generated/` owns generated NSwag output; hand-written `IChatBotClient` and DI/registration code stay outside `Generated/`; contract tests live under `tests/Hexalith.ChatBot.Contracts.Tests`; client generation tests live under `tests/Hexalith.ChatBot.Client.Tests`; conformance fixtures live under `tests/fixtures/`.
- Detected variance: architecture text places the `IChatBotCommand` marker under `Identities/` in one tree comment, but command marker ownership is clearer under `Contracts/Commands/`. If the developer chooses `Identities/` to match the tree exactly, keep the namespace and tests explicit so adapters find it.
- Detected conflict: OpenAPI currently lists newer specification versions, but this project and Folders reference are pinned to OpenAPI 3.1 for NSwag compatibility and architecture consistency. Treat an upgrade as a future architecture decision, not a local implementation choice.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md#Discover Inputs Protocol]
- [Source: .agents/skills/bmad-create-story/template.md#Story Template]
- [Source: .agents/skills/bmad-create-story/checklist.md#Story Context Quality Competition Prompt]
- [Source: AGENTS.md#Git Submodules]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/implementation-artifacts/1-1-scaffold-the-buildable-hexalith-chatbot-module.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2]
- [Source: _bmad-output/planning-artifacts/architecture.md#Contract Spine]
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR77]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Shared Command Pipeline]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Command Surface Reference]
- [Source: Hexalith.Folders/_bmad-output/project-context.md#Project Context for AI Agents]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md#Project Context for AI Agents]
- [Source: Hexalith.Folders/src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml]
- [Source: Hexalith.Folders/src/Hexalith.Folders.Client/nswag.json]
- [Source: Hexalith.Folders/tests/Hexalith.Folders.Client.Tests/ClientGenerationTests.cs]
- [Source: Hexalith.Folders/_bmad-output/implementation-artifacts/1-12-wire-nswag-sdk-generation-with-idempotency-helpers.md]
- [Source: OpenAPI Specification v3.1.0: https://spec.openapis.org/oas/v3.1.0.html]
- [Source: RFC 9457 Problem Details for HTTP APIs: https://www.rfc-editor.org/rfc/rfc9457]
- [Source: NSwag CSharpClientGenerator documentation: https://github.com/RicoSuter/NSwag/wiki/CSharpClientGenerator]

## Dev Agent Record

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-30

Outcome: Approve after auto-fix. No critical issues remain.

Findings fixed:
- HIGH: `ChatBotClient.SubmitAsync` forwarded caller-supplied correlation/task headers without enforcing the ULID contract. Fixed by normalizing supplied IDs through the shared identity helpers and rejecting invalid values before transport.
- HIGH: `ChatBotClient.SubmitAsync` accepted `IChatBotCommand` implementations whose type names ended in `Command`, allowing adapters to violate the contract naming rule. Fixed by rejecting invalid command type names before transport and adding an OpenAPI `not: { pattern: "Command$" }` guard.
- MEDIUM: NSwag generated optional command/problem DTO fields as non-null/defaulted properties, which could silently emit absent adapter metadata such as actor/risk/threshold values. Fixed with nullable optional-property generation and regenerated the checked-in client.
- MEDIUM: Story tasks and File List lagged the implemented client generation, conformance, and fixture changes. Fixed by marking verified tasks complete and expanding the File List.

Verification notes:
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1` passed.
- Focused xUnit v3 in-process runners passed for Contracts, Client, and Conformance.
- Default VSTest execution remains blocked by sandbox TCP listener permissions.
- Default parallel `dotnet build Hexalith.ChatBot.slnx --no-restore` remains blocked by AppHost Aspire SDK resolver behavior, while serial solution build passes.

---

Reviewer: Claude Opus 4.8 on 2026-06-10

Outcome: Approve. No critical or high-severity issues remain.

Findings fixed:
- MEDIUM: A new Story-1.2 client test, `tests/Hexalith.ChatBot.Client.Tests/CommandSubmissionTransportTests.cs`, existed in the working tree (untracked) but was absent from the story File List — a documentation/transparency gap. Added it to the File List. The test is the only lane that exercises the generated transport `SubmitCommandAsync` end-to-end over HTTP (asserting `POST /api/v1/commands`, correlation/task headers, typed request body shape, accepted-response parsing, and RFC 9457 metadata-only problem parsing for 400/401/403/409/500/503), complementing `ClientGenerationTests` which uses a fake `IClient`.

AC validation (all verified against implementation):
- AC1 (single OpenAPI spine + NSwag-generated, never hand-edited): `nswag.json` points only at `../../src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`; generated output carries `<auto-generated>`/NSwag provenance; freshness hash fixture matches the checked-in client byte-for-byte.
- AC2 (typed `IChatBotCommand` + `IChatBotClient.SubmitAsync`, no envelope/Dapr/gateway in adapters): facade present; `FacadeShouldExposeTypedCommandAndStatusMethodsOnly` asserts no Dapr/EventStore parameter types.
- AC3 (enums + ULID identity helpers, `Ulid.TryParse` not `Guid.TryParse`): `ChatBotIdentity` uses `Ulid.TryParse`; repo-wide grep confirms no `Guid.TryParse` in hand-written ChatBot source.
- AC4 (RFC 9457 metadata-only problem responses): generated `ProblemDetails*` enums match the catalog; transport tests assert `visibility=metadata_only` and absence of tenant/payload/secret/local-path content.
- AC5 (naming conventions): `ChatBotClient.ResolveCommandType` rejects `Command`-suffixed type names; contract naming tests pass.
- AC6 (build + tests prove freshness, safe provenance, deterministic): generation/freshness/provenance tests pass.

Verification notes:
- `dotnet build … -m:1 --no-restore` succeeded for Contracts and Client test projects (0 warnings, 0 errors).
- Direct xUnit v3 in-process runners passed: Contracts 454, Client 30 (includes the 7 newly-documented transport tests), Conformance 84; 0 failed.
- Generated-client SHA-256 equals `tests/fixtures/hexalith-chatbot-generated-client.sha256` (`ef40fb3f…`).
- The Conformance project's full `dotnet build` remains blocked by the previously-recorded environmental NuGet vulnerability-audit `NU1900` errors in sibling submodules (no network in sandbox); this is not a code defect and the Conformance lane passes via the direct runner.

### Change Log

- 2026-05-30: Senior review fixes applied for facade metadata validation, command naming enforcement, optional generated DTO nullability, generated client hash freshness, story File List, and sprint status sync.
- 2026-06-09: Re-ran BMAD dev-story validation for already-complete Story 1.2; all tasks remained checked and direct xUnit v3 regression runners passed.
- 2026-06-10: Story-automator adversarial review (Claude Opus 4.8). Auto-fixed one MEDIUM File List gap by documenting `CommandSubmissionTransportTests.cs`. Re-verified all six ACs and re-ran focused lanes (Contracts 454, Client 30, Conformance 84; 0 failed). Status remains `done` (0 critical issues).

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Activation customization resolved with no prepend or append steps and persistent fact glob `file:{project-root}/**/project-context.md`.
- Input discovery loaded: sprint status, epics, architecture, PRD/addendum, UX DESIGN/EXPERIENCE, Story 1.1, root project files, current scaffold source/tests, Folders/Conversations/EventStore/Commons project contexts, Folders OpenAPI and NSwag generation references, and recent git status/log.
- Web research checked primary sources for OpenAPI 3.1 specification extensions, RFC 9457 Problem Details, and NSwag C# client generation.
- Checklist validation applied during story creation: added explicit current-file update notes, anti-reinvention guidance, generated-file ownership, metadata-only diagnostics, ULID-only identity requirement, submodule guardrails, and focused testing requirements.
- 2026-05-30: `dotnet test tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore` built the test project but VSTest aborted with sandbox TCP listener `SocketException (13): Permission denied`; focused execution used the xUnit v3 in-process runner instead.
- 2026-05-30: `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -noColor` passed: 10 total, 0 failed.
- 2026-05-30: `dotnet build tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore` and xUnit v3 in-process runner passed for shared contract type coverage: 16 total, 0 failed.
- 2026-05-30: Senior Developer Review found and auto-fixed: facade accepted invalid ULID header metadata; facade allowed `Command`-suffixed command type names; NSwag generated optional DTO fields as non-null/defaulted values; story task/file-list bookkeeping lagged actual implementation.
- 2026-05-30: `dotnet restore Hexalith.ChatBot.slnx` exited 1 without console diagnostics under default parallel restore; `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` and project-level restore for the changed client graph succeeded. `dotnet build Hexalith.ChatBot.slnx --no-restore` exits 1 without normal console diagnostics under default parallel solution build; diagnostic log points at `Aspire.AppHost.Sdk` resolver behavior in the AppHost project. `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1` passed.
- 2026-05-30: VSTest remains blocked by sandbox TCP listener permissions; focused xUnit v3 in-process runners passed: Contracts 19 total, Client 8 total, Conformance 3 total.
- 2026-06-09: BMAD dev-story activation resolved with no prepend or append steps and persistent project-context facts loaded from sibling module `project-context.md` files. Story and sprint status were already `done`; no unchecked Story 1.2 tasks/subtasks were present.
- 2026-06-09: `dotnet restore Hexalith.ChatBot.slnx` failed because NuGet vulnerability-audit lookup could not reach `https://api.nuget.org/v3/index.json` in the network-restricted sandbox, producing `NU1900` warnings-as-errors in sibling submodule projects.
- 2026-06-09: `dotnet build Hexalith.ChatBot.slnx --no-restore` failed with no diagnostics; `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1` compiled ChatBot projects and test assemblies but failed overall on the same network-blocked NuGet audit `NU1900` errors in sibling submodule projects.
- 2026-06-09: `dotnet test` for focused Story 1.2 projects remained blocked by VSTest TCP listener permission errors and parallel MSBuild node socket/pipe restrictions. Direct xUnit v3 runners passed for focused Story 1.2 lanes: Contracts 454 total, Client 23 total, Conformance 84 total; 0 failed.
- 2026-06-09: Direct xUnit v3 regression runners passed across all built ChatBot test assemblies: 2441 total, 0 failed, 2 skipped Tier-3 Aspire/DAPR tests requiring `HEXALITH_CHATBOT_TIER3=1`, Docker, and DAPR runtime.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story created as ready-for-dev with implementation scope limited to Contract Spine, generated typed client, `IChatBotCommand`, shared contract types, and guardrail tests.
- Added the OpenAPI 3.1 Contract Spine with a minimal command-submission operation, shared problem metadata, shared headers/responses, reusable enums, synthetic examples, and `x-hexalith-*` extension metadata only.
- Added `IChatBotCommand`, stable wire-name enums, and ULID identity helpers for command, correlation, and task identifiers; no problem DTOs or EventStore references were needed outside generated transport types.
- Wired deterministic NSwag generation for `Hexalith.ChatBot.Client.Generated`, with nullable optional DTO fields and a checked generated-client hash fixture.
- Added the hand-written `IChatBotClient` facade; it validates adapter-supplied ULID metadata and rejects `Command`-suffixed command type names before invoking the generated transport client.
- Replaced the Story 1.1 conformance placeholder with a real Story 1.2 oracle fixture and tests for the command-submission contract and metadata-only failure categories.
- Senior review completed with no critical issues remaining; story and sprint status moved to done.
- 2026-06-09 validation rerun found no unchecked Story 1.2 tasks to implement and no code changes were required; direct xUnit v3 regression validation passed with only environment-gated Tier-3 skips.

### File List

- `_bmad-output/implementation-artifacts/1-2-establish-the-openapi-contract-spine-typed-client-and-ichatbotcommand.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `Directory.Packages.props`
- `src/Hexalith.ChatBot.Contracts/Hexalith.ChatBot.Contracts.csproj`
- `src/Hexalith.ChatBot.Contracts/Commands/IChatBotCommand.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ActorType.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/RiskClass.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ThresholdBand.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/ChatBotCommandId.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/ChatBotCorrelationId.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/ChatBotIdentity.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/ChatBotTaskId.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj`
- `src/Hexalith.ChatBot.Client/nswag.json`
- `src/Hexalith.ChatBot.Client/IChatBotClient.cs`
- `src/Hexalith.ChatBot.Client/ChatBotClient.cs`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Client.Tests/CommandSubmissionTransportTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj`
- `tests/Hexalith.ChatBot.Conformance.Tests/ContractSpineOracleTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
- `tests/fixtures/story-1-2-contract-spine-oracle.json`
- `tests/Hexalith.ChatBot.Conformance.Tests/ContractSpineOraclePlaceholderTests.cs` (removed)
- `tests/fixtures/story-1-2-contract-spine-oracle.placeholder.json` (removed)
