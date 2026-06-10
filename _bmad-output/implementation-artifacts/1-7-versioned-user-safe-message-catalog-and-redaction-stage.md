---
baseline_commit: f4378d1b
---

# Story 1.7: Versioned User-Safe Message Catalog and Redaction Stage

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-30. -->

## Story

As a UX and security owner,
I want a versioned message catalog and a swappable redaction stage,
so that every user-facing failure is safe, catalogued, and never leaks restricted detail.

## Acceptance Criteria

1. Given the message catalog, when a refusal, blocked, degraded, failed, or denied state surfaces, then the user-facing response is drawn from `src/Hexalith.ChatBot.Contracts/Messages/` with a stable code, a user-safe headline of 80 characters or less, a one-sentence reason that names no unauthorized project, file, party, audit detail, tenant data, raw payload, exception text, secret, or local path, and a safe next-action affordance. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.7; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR77]
2. Given any user-facing surface or API response, when an error/problem/status is rendered, then no raw exception text or uncatalogued string leaks to the caller; telemetry records every uncategorized user-facing state and the release-blocking expected count is zero. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.7; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR40]
3. Given the redaction stage, when responses are produced across current API output and future UI/CLI/MCP/export surfaces, then redaction is applied consistently by a swappable policy stage that defaults to a trim-safe coarse policy when no richer policy is available. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.7; Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns]
4. Given a denied or hidden resource, when authorization fails because of insufficient authority, tenant mismatch, missing tenant context, or safe-not-found behavior, then caller-visible problem details remain indistinguishable where required and do not confirm whether the target resource exists. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2; Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs]
5. Given disabled action reasons are needed by current or future surfaces, when an affordance is unavailable, then the reason is drawn only from `insufficient-authority`, `state-not-permitted`, `dependency-degraded`, `awaiting-other-actor`, or `policy-blocked`, and is not derived from raw error text. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.7; Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR76]

## Tasks / Subtasks

- [x] Add the public versioned message catalog contract under `src/Hexalith.ChatBot.Contracts/Messages/` (AC: 1, 2, 5)
  - [x] Create stable contract types for a catalog entry, catalog version, message code, headline, reason, next-action affordance, disabled-action reason, and detail visibility.
  - [x] Keep the contract low-dependency: no server, DAPR, HTTP, logging, OpenTelemetry, or generated-client dependency in `Contracts`.
  - [x] Use a first version such as `chatbot.message-catalog.v1` and make every entry additive and serialization-tolerant.
  - [x] Define the minimum M0 entries for current gateway output: `authentication_denied`, `authorization_denied`, `audit_unavailable`, `idempotency_conflict_command_execution`, and `invalid_lifecycle_transition`.
  - [x] Add forward-safe catalog entries for the FR77 state families: refusal/blocked action, dependency degraded, failed attachment, failed command, degraded mailbox, and authorization denied. These can be unused by current code but must be tested as valid catalog entries.
  - [x] Define safe next-action affordances as stable contract values. Minimum set: `authenticate`, `retry-later`, `request-access`, `escalate`, `dismiss`, `correct-request`, and `none`.
  - [x] Define disabled-action reason values exactly as `insufficient-authority`, `state-not-permitted`, `dependency-degraded`, `awaiting-other-actor`, and `policy-blocked`.
- [x] Replace hand-authored gateway problem text with catalog-backed problem details (AC: 1, 2, 4)
  - [x] Update `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs` so all titles/messages/client actions come from the catalog, not local string literals.
  - [x] Keep the current public authorization-denial collapse: `tenant_missing`, `tenant_mismatch`, `authorization_denied`, and `safe_not_found` must continue to return equivalent caller-visible problem details where the existing tests require indistinguishability.
  - [x] Preserve existing status/category behavior unless the OpenAPI contract is deliberately migrated in the same change: unauthenticated is 401, authorization denial is 403, conflicts are 409, audit unavailable is 503.
  - [x] Replace any `contact_support` style caller action for FR77 workflow states with a safe catalog action such as `escalate` or `retry-later`; if generated contracts need new enum values, update OpenAPI and regenerate the client.
  - [x] Ensure `CommandGatewayHttpResults.ToHttpResult` serializes only catalog/redacted problem models and never serializes exception detail.
- [x] Add a server-side redaction stage that can be reused beyond the gateway (AC: 1, 2, 3, 4)
  - [x] Add a focused server seam, for example `src/Hexalith.ChatBot.Server/Gateway/Redaction/` or `src/Hexalith.ChatBot.Server/Governance/Redaction/`, with an internal interface such as `IUserFacingRedactionStage`.
  - [x] Implement the default coarse policy as metadata-only: it permits stable codes, status/category, correlation ID, task ID, retryability, safe next action, and catalog text; it strips or suppresses command payloads, exception text, tenant/project/file/party names, audit details, secrets, and local paths.
  - [x] Keep the stage deterministic and side-effect free. No DAPR, HTTP, clocks, sibling clients, tenant lookups, AI calls, or logging inside the redaction decision itself.
  - [x] Register the stage in `CommandGatewayServiceCollectionExtensions`.
  - [x] Stamp audit envelopes with the redaction decision already used by the response path. Preserve the existing `metadata_only` audit value unless the story intentionally migrates it to a richer stable contract value.
- [x] Update OpenAPI and generated client contracts only as needed, then regenerate (AC: 1, 2, 5)
  - [x] If message catalog and safe-action values are public wire contracts, add their schemas to `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
  - [x] Preserve the existing RFC 9457-style `ProblemDetails` shape: `{ category, code, message, correlationId, taskId?, retryable, clientAction, details.visibility }`.
  - [x] Update synthetic problem examples so all messages come from the catalog and remain metadata-only.
  - [x] Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` through the existing NSwag target. Do not hand-edit generated output.
  - [x] Refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` if generated output changes.
- [x] Add telemetry for uncategorized user-facing states without leaking payloads (AC: 2)
  - [x] Add a metadata-only counter or in-memory observable seam for uncatalogued message resolution. It must record counts/tags such as catalog version and safe fallback code, not raw input text.
  - [x] Route unknown reason codes through a cataloged safe fallback and increment the uncategorized counter.
  - [x] Do not log or export the unknown raw message/reason/payload as part of this story.
- [x] Add regression tests that make leakage and uncatalogued output hard to reintroduce (AC: all)
  - [x] Add contract tests for catalog entry completeness: stable code pattern, headline length <= 80, one-sentence reason, safe next action, disabled-action reason finite set, and catalog version stability.
  - [x] Extend `ProblemDetailsContractTests` to assert OpenAPI examples use catalog codes/actions and remain synthetic, metadata-only, and free of restricted words, paths, payload markers, secrets, and raw exception text.
  - [x] Extend gateway tests to prove authorization, audit-unavailable, idempotency-conflict, and invalid-lifecycle-transition responses are catalog-backed and redacted.
  - [x] Add adversarial redaction tests with tenant IDs, project names, file names, party names, audit detail, command payload sentinels, secrets, local Unix/Windows paths, and raw exception phrases.
  - [x] Add an architecture test that rejects new non-generated `ProblemDetails` text literals outside the catalog and the narrow catalog resolver/redaction implementation.
  - [x] Preserve existing Story 1.6 tests proving invalid lifecycle transitions fail before dispatch and authorization-denied/safe-not-found responses are indistinguishable.
- [x] Verify locally (AC: all)
  - [x] Run `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false`.
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests`.
  - [x] Run `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests` if OpenAPI/generated client changes.
  - [x] Run `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests`.
  - [x] Run `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests`.
  - [x] If VSTest or tooling is blocked in the sandbox, record the exact command, error, and replacement in-process command in the Dev Agent Record.

## Dev Notes

### Implementation Intent

Story 1.7 turns user-facing failures from scattered strings into a durable contract. The current code already uses metadata-only `ProblemDetails`, but the safe text is hand-written in `ChatBotProblemDetailsFactory` and `CommandGatewayHttpResults` has no catalog or redaction stage between internal failures and wire output. This story should make the catalog the only source of caller-visible failure copy and make redaction an explicit server stage before the API, UI, CLI, MCP, and export surfaces grow around it. [Source: src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs; Source: src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs; Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns]

The catalog belongs in `Contracts/Messages/` because message codes, safe headlines, reasons, next actions, disabled reasons, and redaction visibility are shared public semantics. The resolver/redaction policy belongs in `.Server` because the server decides what internal reason is safe to show and must keep gateway stages internal. [Source: _bmad-output/planning-artifacts/architecture.md#Source Tree; Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]

### Current Files To Update

- `src/Hexalith.ChatBot.Contracts/Messages/`: create this folder and add the versioned catalog contracts and catalog entries. The architecture already reserves this path for the FR77 catalog. [Source: _bmad-output/planning-artifacts/architecture.md#Source Tree]
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`: currently hard-codes user-facing titles/messages for auth denial, audit unavailable, idempotency conflict, and invalid lifecycle transition. Replace this with catalog resolution and redaction. [Source: src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs]
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs`: currently maps generated `ProblemDetails` to wire output. Keep it as a serialization boundary, but ensure it only receives already-redacted, catalog-backed problem details. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs]
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`: current denial paths call the problem factory directly after auth, tenant-binding, authorization, idempotency, lifecycle, and audit failures. Preserve stage order while adding catalog/redaction behavior. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`: register any catalog resolver/redaction stage here. Keep gateway stage interfaces internal. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs]
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`: currently stamps `RedactionDecision` as `metadata_only`. Keep audit metadata-only and align it with the redaction stage decision. [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`: update if public safe actions, catalog entries, or problem examples change; regenerate client output afterward. [Source: src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml]
- `tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, and `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`: extend these rather than creating duplicate test styles. [Source: tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs; Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs; Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]

### Architecture Guardrails

- Do not build a second command pipeline. Catalog resolution and redaction are stages/helpers inside the existing gateway output path, not alternate admission or dispatch paths. [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Handoff]
- Do not let adapters, future UI, CLI, MCP, workers, or export code invent local messages. They should consume catalog codes, safe text, and redaction decisions from shared contracts or client responses. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR32]
- Do not weaken the Story 1.3 and Story 1.6 security behavior. Tenant mismatch, safe-not-found, and authorization denial must not reveal whether a resource exists; invalid lifecycle transitions still fail before dispatch. [Source: _bmad-output/implementation-artifacts/1-3-commandgateway-admission-spine-with-tenant-binding-and-authorization.md; Source: _bmad-output/implementation-artifacts/1-6-canonical-lifecycle-state-model-and-transition-enforcement.md]
- Do not put raw command payloads, exception text, project/file/party names, candidate evidence, audit details, tenant data, secrets, tokens, local paths, or upstream PII into problem details, logs, traces, catalog entries, telemetry, test snapshots, or generated examples. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2; Source: Hexalith.EventStore/_bmad-output/project-context.md#Critical Implementation Rules]
- Keep the default redaction policy deterministic and side-effect free. Richer tenant policy can be added later, but this M0 stage must be trim-safe to `metadata_only`. [Source: _bmad-output/planning-artifacts/architecture.md#Redaction and data governance]
- Preserve two-phase audit semantics: pre-commit audit failure blocks mutation; post-commit audit failure queues reconciliation and alerts but keeps accepted dispatch. [Source: _bmad-output/implementation-artifacts/1-4-fail-closed-audit-commit-seam-with-pre-and-post-commit-audit-emission.md]
- Preserve two-altitude idempotency. Catalog/redaction work must not change equivalent replay or conflict semantics. [Source: _bmad-output/implementation-artifacts/1-5-two-altitude-idempotency.md]
- Use only existing platform dependencies: .NET 10, `System.Text.Json`, OpenAPI/NSwag, xUnit v3, Shouldly, DAPR/Aspire where already wired. Do not add inline package versions or a new serializer. [Source: Directory.Build.props; Source: Directory.Packages.props]

### UX And Accessibility Notes

- Microcopy must be factual, specific, and safe. Blocked states explain denial, unresolved association, quarantine, failed dependency, or unsafe context with a safe next action and redacted details. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Voice and Tone; Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Components]
- Disabled controls must not rely on tooltip-only explanations. Future UI should render reachable reasons using the finite disabled-action set from this story. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction and Focus]
- Export, copy, download, read-aloud, and future off-surface affordances must apply the same redaction as the visual surface and must expose an equivalent screen-reader-safe redaction message. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Off-surface Affordances]
- English/French localization infrastructure lands in Story 1.20. For this story, keep catalog values stable and localizable later; do not hard-code UI-only assumptions into message codes. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.20]

### Previous Story Intelligence

- Story 1.6 completed canonical lifecycle states and invalid-transition rejection. Reuse `LifecycleTransitionReasonCodes.InvalidTransition` as an internal reason source, but resolve caller text through the message catalog. [Source: _bmad-output/implementation-artifacts/1-6-canonical-lifecycle-state-model-and-transition-enforcement.md#Completion Notes List]
- Story 1.6 added tests that already assert invalid transitions return metadata-only 409 responses without dispatch. Extend these tests for catalog-backed copy and leakage checks rather than loosening them. [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs]
- Story 1.5 established coarse idempotency replay and conflict branching. Equivalent duplicate replay should not generate new catalog telemetry, audit, or dispatch. Conflict should return the cataloged idempotency problem and remain metadata-only. [Source: _bmad-output/implementation-artifacts/1-5-two-altitude-idempotency.md#Completion Notes List]
- Story 1.4 established fail-closed pre-commit audit and post-commit reconciliation. `audit_unavailable` must stay retryable and safe while revealing no operational internals beyond correlation/task metadata. [Source: _bmad-output/implementation-artifacts/1-4-fail-closed-audit-commit-seam-with-pre-and-post-commit-audit-emission.md#Architecture Guardrails]
- Story 1.3 established tenant binding from authenticated claims and caller-indistinguishable denial behavior. Do not expose tenant IDs from request bodies or authorization facts in problem text. [Source: _bmad-output/implementation-artifacts/1-3-commandgateway-admission-spine-with-tenant-binding-and-authorization.md#Senior Developer Review (AI)]
- Current dirty worktree observed during story creation: `_bmad-output/story-automator/orchestration-1-20260530-160445.md` is unrelated. Do not revert or overwrite it. [Source: git status --short]

### Testing Requirements

- Use xUnit v3 and Shouldly. Avoid raw `Assert.*` and avoid adding a new mocking/assertion library. [Source: Directory.Packages.props; Source: Hexalith.Tenants/_bmad-output/project-context.md#Critical Implementation Rules]
- Add negative leakage tests, not only happy-path catalog tests. Include tenant, project, file, party, audit, payload, secret, Windows path, Unix path, and raw exception sentinels.
- Contract tests should validate catalog shape and OpenAPI examples. Server tests should validate actual gateway behavior. Architecture tests should stop future hand-written problem strings outside the catalog path.
- If generated client changes, update the generated-client hash and run client tests. Hand-editing `Generated/HexalithChatBotClient.g.cs` is a defect.

### Out of Scope

- Do not implement Story 1.8 correlation/status-query behavior beyond preserving current correlation and task fields.
- Do not implement Story 1.9's first governed command or any mailbox intake, association scorer, approval queue, correction propagation, retry worker, UI, CLI, or MCP adapter.
- Do not implement Story 1.20 localization infrastructure; make the catalog localizable later, but keep this story focused on stable safe message contracts.
- Do not implement Epic 9 WORM audit-chain persistence or GDPR key-shredding mechanics.
- Do not modify sibling bounded contexts or EventStore internals unless a compile error requires a minimal adapter-facing update.
- Do not initialize nested submodules, run recursive submodule commands, add inline package versions, or hand-edit generated client files.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md#Discover Inputs Protocol]
- [Source: .agents/skills/bmad-create-story/template.md#Story Template]
- [Source: .agents/skills/bmad-create-story/checklist.md#Story Context Quality Competition Prompt]
- [Source: AGENTS.md#Git Submodules]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.7]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR76]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR77]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR40]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR44]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR70]
- [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Source Tree]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation Handoff]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Voice and Tone]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction and Focus]
- [Source: _bmad-output/implementation-artifacts/1-3-commandgateway-admission-spine-with-tenant-binding-and-authorization.md]
- [Source: _bmad-output/implementation-artifacts/1-4-fail-closed-audit-commit-seam-with-pre-and-post-commit-audit-emission.md]
- [Source: _bmad-output/implementation-artifacts/1-5-two-altitude-idempotency.md]
- [Source: _bmad-output/implementation-artifacts/1-6-canonical-lifecycle-state-model-and-transition-enforcement.md]
- [Source: src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]
- [Source: src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml]
- [Source: tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- [Source: Directory.Packages.props]
- [Source: Directory.Build.props]
- [Source: git log --oneline -5]
- [Source: git status --short]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Activation customization resolved with no prepend or append steps, persistent fact glob `file:{project-root}/**/project-context.md`, and no `on_complete` terminal instruction.
- Input discovery loaded sprint status, epics, architecture, PRD/addendum references, UX design/experience references, Story 1.6 intelligence, project-context facts from sibling modules, current ChatBot gateway/problem-details/audit/OpenAPI source and tests, and recent git status/log.
- Web research was not required because this story does not upgrade version-sensitive infrastructure; implementation constraints and package versions are pinned locally in `global.json`, `Directory.Build.props`, `Directory.Packages.props`, architecture, and project-context files.
- Checklist validation applied during story creation: added explicit current-file analysis, catalog-backed problem-details requirements, redaction-stage requirements, finite disabled-action reason coverage, current gateway regression preservation, and adversarial leakage tests.
- Senior review MCP resource lookup returned no configured resources; review used local pinned project documentation and source references.

### Completion Notes List

- Added the versioned `chatbot.message-catalog.v1` contract, stable message codes, safe next-action values, finite disabled-action reasons, and metadata-only visibility.
- Replaced current gateway denial/conflict/audit/lifecycle problem copy with catalog-backed problem details behind a redaction stage.
- Added metadata-only telemetry for uncategorized authorization reason fallback without recording raw reason text.
- Updated OpenAPI problem examples and generated client action enum to use catalog-safe hyphenated actions and remove `contact_support`.
- Added regression coverage for catalog shape, OpenAPI examples, generated client action values, gateway catalog resolution, leakage prevention, redaction behavior, and architecture guardrails.
- Senior review auto-fixed wire-action/catalog mismatches, redaction mutation, telemetry DI observability, story File List/status gaps, and regenerated client freshness.

### File List

- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDetailVisibility.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalogEntry.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalogVersion.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageNextActions.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayHttpResults.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/IChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/IUserFacingMessageTelemetry.cs`
- `src/Hexalith.ChatBot.Server/Gateway/InMemoryUserFacingMessageTelemetry.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Redaction/IUserFacingRedactionStage.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProblemDetailsContractTests.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/IdempotencyStateStoreIntegrationTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-30

### Review Findings

- HIGH: OpenAPI/generated client still exposed legacy `contact_support`/underscore client actions, so the public wire contract did not match the catalog-safe values required by AC1/AC5. Fixed by changing `ProblemDetails.clientAction` to `authenticate`, `retry-later`, `request-access`, `escalate`, `dismiss`, `correct-request`, and `none`, then regenerating the client and hash.
- HIGH: Authorization-denied catalog entry used `request-access`, but `ChatBotProblemDetailsFactory` collapsed it to `None`, so caller-visible next actions were not actually drawn from the catalog. Fixed by mapping all catalog next-action constants to generated client enum values.
- MEDIUM: `CoarseUserFacingRedactionStage.Apply` mutated the input `ProblemDetails`, which weakened the side-effect-free redaction-stage requirement. Fixed by returning a new redacted problem instance and adding a regression assertion.
- MEDIUM: `CommandGatewayHttpResults` hard-coded `metadata_only` instead of serializing the redacted problem model's visibility. Fixed by mapping the redacted visibility from `ProblemDetails.Details`.
- MEDIUM: `InMemoryUserFacingMessageTelemetry` was registered only as `IUserFacingMessageTelemetry`, making the in-memory observable seam hard to inspect through DI. Fixed by registering the concrete singleton and mapping the interface to it.
- MEDIUM: Story status, task checkboxes, and File List did not reflect the implemented source/test changes. Fixed in this story record.

### Review Outcome

Approved after auto-fixes. No critical issues remain.

### Validation

- `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` - passed.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` - 26 passed.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests` - 10 passed.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` - 72 passed.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` - 17 passed.
- `tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests` - 2 passed.

## Senior Developer Review (AI) — Re-review

Reviewer: Jerome on 2026-06-10

### Scope and Method

Adversarial re-review of the committed Story 1.7 implementation (`da6ebe6`, now an ancestor of `main@6409240`) against its five Acceptance Criteria and task claims. Because 40+ later stories (7.x/8.x/9.x) have since extended the catalog and gateway, every claim was validated against the **current** live source and the full test tree, not only the original diff. Evidence: clean `dotnet build` (0 warnings/0 errors) and green suites at HEAD — Contracts 480, Client 30, Server 1510, Architecture 39, Integration 16 (+2 skipped).

### AC Validation (all confirmed implemented)

- AC1/AC5 — `ChatBotMessageCatalog` resolves stable codes to safe headlines, one-sentence reasons, finite safe next-actions, and finite disabled-action reasons; enforced by `MessageCatalogContractTests` (headline ≤ 80, one-sentence reason, no restricted text/paths/secrets/exception/payload markers, finite reason sets).
- AC2 — `CoarseUserFacingRedactionStage.Apply` nulls `Detail`/`Instance` and stamps `metadata_only`; `IUserFacingMessageTelemetry`/`InMemoryUserFacingMessageTelemetry` record uncategorized fallbacks without raw text; `ScaffoldArchitectureTests.ServerProblemDetailsTextShouldStayInsideCatalogResolverOrRedactionBoundary` blocks new non-catalog problem-text literals across the whole Server project.
- AC3 — Redaction is a swappable `IUserFacingRedactionStage` seam defaulting to the trim-safe coarse policy, registered in `CommandGatewayServiceCollectionExtensions`.
- AC4 — `ChatBotProblemDetailsFactory.CreateAuthorizationProblem` collapses `tenant_missing`/`tenant_mismatch`/`authorization_denied`/`safe_not_found` to one identical `authorization_denied` catalog entry; Story 1.3/1.6 indistinguishability tests remain green.

### Findings

- LOW (fixed): `src/Hexalith.ChatBot.Server/Gateway/Redaction/UserFacingRedactionDecision.cs` was born-dead — introduced in `da6ebe6` and never referenced in any commit since (the response/audit paths use the `CoarseUserFacingRedactionStage.MetadataOnlyDecision` string constant instead). Removed the vestigial record; build and all suites stay green. File List updated.
- OBSERVATION (no change): a strengthened, real-pipeline E2E leakage test — `CommandGatewayApi_ShouldReturnCatalogBackedRedactedProblemsForStory17States` in `CommandGatewayAdmissionApiE2ETests.cs` — drives the HTTP gateway through all six Story 1.7 denial states (401 auth, 403 authz-via-tenant-mismatch, 403 refusal-blocked, 409 idempotency, 409 invalid-lifecycle, 503 audit-unavailable) and asserts each is catalog-backed, redacted, and free of `tenant-*`/`payload-sentinel`/`restricted-project`/`/tmp/...`/`C:\` markers. Validated as passing and preserved; added to the File List.

No CRITICAL, HIGH, or MEDIUM issues found. The catalog/redaction/telemetry contract holds up under adversarial review.

### Re-review Outcome

Approved. Zero critical issues remain; status stays **done**.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — passed (0 warnings, 0 errors), before and after the dead-code removal.
- `Hexalith.ChatBot.Contracts.Tests` — 480 passed.
- `Hexalith.ChatBot.Client.Tests` — 30 passed.
- `Hexalith.ChatBot.Server.Tests` — 1510 passed (includes the new Story 1.7 E2E leakage test).
- `Hexalith.ChatBot.Architecture.Tests` — 39 passed.
- `Hexalith.ChatBot.IntegrationTests` — 16 passed, 2 skipped.

## Change Log

- 2026-05-30: Implemented Story 1.7 message catalog, redaction stage, catalog-backed gateway problem details, OpenAPI/client updates, telemetry seam, and regression tests.
- 2026-05-30: Senior review auto-fixed public client-action wire values, catalog action mapping, redaction mutation, telemetry DI observability, and story tracking metadata.
- 2026-06-10: Adversarial re-review against live HEAD — removed the born-dead `UserFacingRedactionDecision` record (LOW, auto-fixed), validated the new catalog-backed/redacted Story 1.7 E2E leakage test, and reconfirmed all ACs with a clean build and full green suites. Status remains done.
