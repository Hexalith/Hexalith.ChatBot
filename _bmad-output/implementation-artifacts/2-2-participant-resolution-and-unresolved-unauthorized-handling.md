---
baseline_commit: dee54233b59da62189ee18c522b78ca73f108ac9
---

# Story 2.2: Participant resolution and unresolved/unauthorized handling

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a reviewer,
I want email senders and recipients resolved to tenant-scoped parties (and unresolved ones flagged),
so that external participants contribute by email while authorization is enforced before any context is exposed.

## Acceptance Criteria

1. **Participants resolve to tenant-scoped PartyId references.** Given an ingested mailbox message, when participant resolution runs, then sender and recipient identities are resolved through a ChatBot-owned `IParticipantDirectory` adapter over Hexalith.Parties, internal and external participants are scoped to the bound tenant, and durable ChatBot records store stable `PartyId` references plus metadata-only evidence references - never upstream display-name/contact PII as authority. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.2-Participant-resolution-and-unresolved-unauthorized-handling; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Participants-Identity-and-Authorization; _bmad-output/planning-artifacts/architecture.md#Structure-Patterns]

2. **Unresolved participants become safe review items.** Given a sender or recipient cannot be resolved, when resolution completes, then the item is recorded in an unresolved participant state with safe identity evidence and the allowed review actions `link`, `create-pending`, `reject`, and `quarantine`; the surface response and projected queue state must not expose more than the source identity evidence already permitted for the reviewer. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.2-Participant-resolution-and-unresolved-unauthorized-handling; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]

3. **External email contribution does not imply portal authorization.** Given an external participant sends email to a controlled project mailbox, when the participant is resolved or remains unresolved, then the source email can remain project context input, but the external participant is not granted portal, file, command, task, approval, or outbound authority by that email alone. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.2-Participant-resolution-and-unresolved-unauthorized-handling; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Assumptions]

4. **Unresolved and unauthorized actors fail closed before exposure or mutation.** Given an unresolved or unauthorized actor, when it attempts to access files, create task requests, trigger commands, inspect restricted project context/candidates/evidence, or send outbound communication, then the action is blocked before durable mutation or restricted data exposure and returns a message-catalog response that does not confirm resource existence. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.2-Participant-resolution-and-unresolved-unauthorized-handling; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Security-and-Privacy; src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs]

5. **Resolution outcomes are auditable, replayable, and tenant-isolated.** Given participant resolution succeeds, is unresolved, is rejected, is quarantined, or is blocked by authorization, when the operation completes, then correlation, source mailbox/intake identity, participant evidence references, actor, reason code, lifecycle state, audit outcome, and tenant-partitioned projection keys are preserved for replay without logging or projecting raw PII. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns; _bmad-output/planning-artifacts/architecture.md#Format-Patterns; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Reliability-and-Data-Integrity]

## Tasks / Subtasks

- [x] Add participant-resolution contracts and OpenAPI spine entries (AC: 1-5)
  - [x] Add an imperative `IChatBotCommand` such as `ResolveMailboxMessageParticipants` under `src/Hexalith.ChatBot.Contracts/Commands/`; do not use a `Command` suffix.
  - [x] Add contract records for participant source references, resolved participant references, unresolved participant evidence, resolution status, review action availability, and safe blocked/authorization reasons.
  - [x] Model durable references with ChatBot-owned identifiers and stable `PartyId` strings; keep provider address/display name as source evidence only, never as authorization identity.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and regenerate `src/Hexalith.ChatBot.Client/Generated/*.g.cs` through the established NSwag path; do not hand-edit generated files.
  - [x] Add contract tests for required fields, camelCase JSON, status/reason enums, `PartyId` reference serialization, and absence of `Guid` parsing for ChatBot-owned ULID ids.

- [x] Add the Parties adapter seam without putting I/O in aggregates (AC: 1, 3, 5)
  - [x] Add `src/Hexalith.ChatBot.Server/Adapters/Parties/IParticipantDirectory.cs` with methods needed by resolution only, for example lookup by normalized email evidence and pending-party creation request preparation.
  - [x] Implement a narrow adapter over `Hexalith.Parties.Client.Abstractions.IPartiesQueryClient` and, only if `create-pending` is in scope for this story, `IPartiesCommandClient`; wrap the sibling client and do not expose Parties client types beyond the adapter boundary.
  - [x] Add the required `ProjectReference` to `Hexalith.Parties.Client` and/or `Hexalith.Parties.Contracts` in `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj`; do not add inline package versions.
  - [x] Canonicalize email evidence deterministically (trim, lower-case domain/local policy as documented, Unicode normalization) before lookup, but preserve the original provider value only as redaction-governed source evidence.
  - [x] Treat Parties not found, stale/unavailable projection, restricted/erased party, ambiguous match, and cross-tenant result as explicit resolution outcomes; do not collapse them into raw exceptions.

- [x] Route participant resolution through the existing command spine (AC: 1, 4, 5)
  - [x] Extend `ChatBotSpineCommandAllowlist` deliberately for the participant-resolution command and keep unrelated commands rejected.
  - [x] Extend the gateway or accepted dispatcher orchestration so participant-directory I/O occurs outside pure aggregate `Handle` methods and before/around the EventStore write in a testable server service.
  - [x] Use the existing pre-commit audit seam for all resolution state writes; if tenant binding, authorization, Parties dependency, or audit is unavailable, fail closed with no durable resolution state.
  - [x] Add or extend a coarse idempotency operation class only if the resolution command is externally replayable; if added, define the exact key from `tenant_id + intake_id + source_participant_fingerprint + resolution_kernel_version` and test canonical equivalence.
  - [x] Preserve correlation id and `ChatBotSurfaceOrigin.Mailbox`/`Worker` attribution from the Story 2.1 intake path.

- [x] Implement participant-resolution domain events/projections (AC: 1, 2, 5)
  - [x] Add past-tense events under `src/Hexalith.ChatBot.Server/Association/Participants/`, for example `MailboxParticipantResolved`, `MailboxParticipantUnresolved`, `MailboxParticipantRejected`, and `MailboxParticipantQuarantined`; rejections must be structured and implement the established rejection pattern.
  - [x] Extend the existing aggregate/state or introduce the smallest Association aggregate seam that matches EventStore discovery; keep `Handle` pure, synchronous, and free of `IParticipantDirectory`, DAPR, auth, and audit calls.
  - [x] Stamp derived records with `tenantId`, source provenance, resolution/kernel version, redaction state, retention class, source version, and schema version.
  - [x] Add tenant-partitioned projection state for resolved and unresolved participants so later stories can render conversation participants, unresolved-party queues, and association review context without re-querying Parties live for display.
  - [x] Preserve source mailbox/intake ids from `MailboxMessageIntakeCaptured` and do not parse/store email body content in this story.

- [x] Enforce unresolved/unauthorized blocking semantics (AC: 3, 4)
  - [x] Add message-catalog entries and finite disabled-action reasons for unresolved participant, unauthorized actor, and participant-directory degraded states; headlines stay under 80 characters and reasons must not name restricted parties/projects/files/audit details.
  - [x] Update `IAuthorizationStage` implementation or add a dedicated participant-authority policy service so unresolved/external email-only actors cannot access files, create task requests, trigger commands, inspect restricted context, or send outbound communication.
  - [x] Add redaction-stage tests proving problem details, operation status, queues, logs, and telemetry expose stable codes/correlation only, not raw address/display-name/project/file evidence.
  - [x] Keep external participants able to contribute by email as source context without granting portal or command authority.

- [x] Reuse existing UI/design primitives only for required state contracts (AC: 2, 4)
  - [x] If any UI contract is touched, use existing `ChatBotActorBadge`, `ChatBotBlockedState`, evidence/state chips, localization, focus, live-region, and recovery pattern contracts; do not create a new visual system.
  - [x] Unresolved actors must show safe actions and accessible labels through existing localization resources in both English and French.
  - [x] Disabled controls must be focusable with `aria-disabled="true"` and announced reasons, or have an adjacent focusable "why unavailable" affordance; tooltip-only disabled state is not sufficient.

- [x] Add focused tests and regression evidence (AC: 1-5)
  - [x] Add Tier 1 contract tests for resolution command/schema/status/reason serialization and OpenAPI required fields.
  - [x] Add server tests for resolved internal party, resolved external party, unresolved participant, ambiguous/duplicate party match, restricted/erased party, Parties unavailable, tenant mismatch, and audit unavailable.
  - [x] Add gateway tests proving authorization blocks unresolved/unauthorized actors before restricted data exposure or durable mutation and returns catalog-backed redacted problem details.
  - [x] Add architecture tests proving no aggregate calls `IParticipantDirectory` or Parties clients, no adapter references server gateway stages, and no UI/worker path references `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, or `IIdempotencyStore`.
  - [x] Add conformance/isolation tests for cross-tenant participant evidence, unauthorized candidate/file/context leakage, and mailbox-event actor behavior.

- [x] Verify and document results (AC: 1-5)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run compiled xUnit v3 binaries for touched test projects; prefer compiled runners in this sandbox because VSTest sockets have been unreliable.
  - [x] Run at minimum Contracts, Server, Architecture, Conformance, and any UI/Workers tests touched by this story.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, pass/fail counts, and known environment limitations in the Dev Agent Record.

## Dev Notes

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 2 and Story 2.2 are the primary story source.
- Loaded `{prd_content}` selectively from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`; FR13-FR20, FR76-FR77, NFR2, NFR11, NFR13-NFR15a, and assumption A7 are relevant.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; no sharded architecture directory was present.
- Loaded `{ux_content}` selectively from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`, `EXPERIENCE.md`, and accessibility/review reports; unresolved actor, blocked state, evidence, disabled-action, redaction, and accessibility patterns are relevant.
- Loaded persistent project-context facts from sibling `Hexalith.*` `_bmad-output/project-context.md` files; the recurring constraints are .NET 10, central package versions, EventStore purity, DAPR/Aspire boundaries, tenant isolation, personal-data redaction, and xUnit/Shouldly testing.

### Source Artifact Analysis

Epic 2 starts with mailbox intake, then participant resolution, then deterministic association and review. Story 2.2 is the bridge between source mailbox metadata and later association/conversation work: it resolves sender/recipient identities to tenant-scoped PartyId references, records unresolved review states, and blocks unresolved/unauthorized actors before files, tasks, commands, context, or outbound communication are exposed. [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-and-Project-Association]

The PRD keeps participant identity and authorization separate. FR13-FR15 permit external participants to contribute context through email, but FR16-FR17 require authorization before exposing candidates, files, conversations, approvals, commands, audit details, task requests, or outbound communication. A7 explicitly says external participants do not need portal access in MVP; do not turn email identity into portal authority. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Participants-Identity-and-Authorization; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Assumptions]

Architecture requires ChatBot-owned sibling adapters such as `IParticipantDirectory` over Parties. Aggregates/projections store stable IDs only and never call sibling clients from pure domain logic. The participant directory belongs under `src/Hexalith.ChatBot.Server/Adapters/Parties/`; Association owns the resolution events and derived state. [Source: _bmad-output/planning-artifacts/architecture.md#Structure-Patterns; _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries]

UX has no full mockup for this story, but its spine is binding where UI state contracts are touched. Actor badges must represent unresolved actors and all actor categories with stable labels/icons, unresolved party states expose safe actions, blocked states explain denial/quarantine/unsafe context with redacted details, and disabled actions must expose focusable reasons rather than tooltip-only explanations. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]

### Previous Story Intelligence

Story 2.1 is complete and established the mailbox source-identity path. It added `CaptureMailboxMessageIntake`, mailbox source/recipient/attachment contracts, `MailboxMessageIntakeId`, the `m365-mailbox-intake` EventStore path, message-intake idempotency, Graph worker models, and tests proving gateway routing, duplicate suppression, audit unavailable, missing tenant scope, and worker mailbox-scope validation. Story 2.2 must extend this path rather than reintroduce mailbox ingestion. [Source: _bmad-output/implementation-artifacts/2-1-microsoft-365-mailbox-intake-and-source-identity-capture.md]

Relevant 2.1 files to extend or preserve:

- `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs` carries `IntakeId`, `Source`, `Recipients`, and `Attachments`; use its source participant records as input evidence.
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxParticipantIdentity.cs` and `MailboxRecipientIdentity.cs` carry provider address/display name. Treat those values as source evidence and personal data, not as authorization identity.
- `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs` records source identity with provenance, kernel, redaction, retention, and schema fields. Participant resolution should preserve links back to this event/intake id.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` is currently doing mailbox intake handling in the walking-skeleton aggregate. If Story 2.2 grows the Association seam, avoid a broad refactor unless it is needed to keep EventStore discovery and tests coherent.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughAuthorizationStage.cs` currently allows all authenticated/bound submissions. Story 2.2 is the first place participant authorization blocking may need a real policy seam.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` already has safe auth/audit/idempotency/dependency messages. Add catalog codes for unresolved/unauthorized participant states rather than returning raw text.

Story 2.1 senior review fixed three issues that should not regress: missing-tenant mailbox intake now queues replay and emits an operator alert, the worker verifies fetched mailbox/provider ids match the controlled notification, and gateway 401/403/503 responses map to sanitized recoverable worker results. Keep those tests green. [Source: _bmad-output/implementation-artifacts/2-1-microsoft-365-mailbox-intake-and-source-identity-capture.md#Senior-Developer-Review-AI]

### Current Implementation State

No `IParticipantDirectory` exists yet. `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj` references EventStore, Tenants contracts, Client, and ServiceDefaults, but not Parties. `Hexalith.Parties.Client` exposes `IPartiesQueryClient.SearchPartiesAsync`, `GetPartyAsync`, and command APIs for party/contact-channel creation/update. `PartyDetail` and `ContactChannel.Value` contain `[PersonalData]`, so ChatBot must not persist or log those values beyond redaction-governed source evidence and stable `PartyId` references. [Source: src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj; Hexalith.Parties/src/Hexalith.Parties.Client/Abstractions/IPartiesQueryClient.cs; Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/PartyDetail.cs; Hexalith.Parties/src/Hexalith.Parties.Contracts/ValueObjects/ContactChannel.cs]

`ActorType` currently has `Human`, `Ai`, `Service`, and `System`. UX and architecture name richer categories such as external party, background worker, CLI, MCP, and mailbox event. Do not overload this enum casually without checking API/OpenAPI compatibility and downstream tests; if actor-category display is needed, prefer an additive contract shape with stable strings. [Source: src/Hexalith.ChatBot.Contracts/Enums/ActorType.cs; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]

### Architecture Guardrails

- Runtime stack: .NET SDK `10.0.302`, `net10.0`, DAPR `1.17.9`, Aspire `13.3.x`, Fluent UI `5.0.0-rc.3`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, central package management. Do not add inline package versions or casual package upgrades. [Source: global.json; Directory.Packages.props]
- Every state mutation goes through `CommandGateway`; surface/worker adapters construct typed `IChatBotCommand` and call `IChatBotClient.SubmitAsync`. Do not let Parties adapters, UI, workers, CLI, or MCP replicate gateway stages. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- Aggregate `Handle` methods are pure: no I/O, DAPR, authorization, audit, or sibling calls. Participant lookup belongs in a server orchestration/adapter service before durable EventStore events are emitted. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- Store stable sibling IDs (`PartyId`) in events/projections. Do not store upstream PII, raw exception text, raw provider tokens, or unauthorized resource details in logs, telemetry, status, problem details, queues, or test artifacts. [Source: _bmad-output/planning-artifacts/architecture.md#Structure-Patterns; Hexalith.Parties/_bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Cross-tenant identifiers and cross-tenant Parties results fail closed without confirming resource existence. Reads/projections should show redacted blocked/unresolved states, not leak candidate, party, file, project, or audit detail. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Security-and-Privacy]
- Derived records carry `tenantId`, `sourceProvenance`, `derivationKernelVersion`, `redactionState`, `retentionClass`, and `schemaVersion`; projection handlers are idempotent and order-tolerant. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- Lifecycle strings are exact: `Received`, `Proposed`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Corrected`, plus `Correcting` and `Correction-delayed`. Use these terms verbatim when participant resolution joins the lifecycle model. [Source: _bmad-output/planning-artifacts/architecture.md#Naming-Patterns]

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Contracts/
    Commands/ResolveMailboxMessageParticipants.cs
    Commands/MailboxParticipantSourceReference.cs
    Commands/ResolvedMailboxParticipantReference.cs
    Commands/UnresolvedMailboxParticipantEvidence.cs
    Enums/ParticipantResolutionStatus.cs
    Messages/ChatBotMessageCodes.cs
    Messages/ChatBotMessageCatalog.cs
    openapi/hexalith.chatbot.v1.yaml
  Hexalith.ChatBot.Server/
    Adapters/Parties/
      IParticipantDirectory.cs
      PartiesParticipantDirectory.cs
      ParticipantDirectoryResult.cs
    Association/Participants/
      MailboxParticipantResolved.cs
      MailboxParticipantUnresolved.cs
      MailboxParticipantRejected.cs
      MailboxParticipantQuarantined.cs
    Gateway/Stages/
      ParticipantAuthorizationPolicy.cs
    Projections/
      ParticipantResolutionView.cs
tests/
  Hexalith.ChatBot.Contracts.Tests/
  Hexalith.ChatBot.Server.Tests/
  Hexalith.ChatBot.Architecture.Tests/
  Hexalith.ChatBot.Conformance.Tests/
  Hexalith.ChatBot.UI.Tests/          # only if UI contracts/localization are touched
```

Keep this shape smaller if the existing code supports a narrower change. The important boundaries are: Contracts remain low-dependency, Server owns Parties adapter/orchestration/domain/projections, UI/Workers stay client-bound, and generated client files are regenerated from OpenAPI.

### Out of Scope

- Deterministic project scoring, candidate generation, thresholds, ambiguous association routing, association confirmation/rejection/defer decisions, S2 full review surface, corrections, correction propagation, retry/duplicate state breadth, and operational queue management beyond the unresolved participant state needed here.
- Granting external participants portal access, service-client grants, mailbox participation policy editor, consent/lawful-basis policy UI, outbound draft/send authority, sender-authority modeling, or inbound authenticity posture.
- Attachment download/storage/scanning, conversation rendering, task-intent conversion, AI proposal/execution, command allowlist expansion beyond participant resolution, CLI/MCP shipped surfaces, and production dashboards.
- Refactoring the whole walking-skeleton aggregate into final Association aggregates unless this story needs it for correctness.
- Package upgrades, new UI frameworks, recursive submodule initialization, direct DAPR/Redis/EventStore/Parties writes from workers/UI, or bypassing `IChatBotClient`/`CommandGateway`.

### Latest Technical Notes

No new external API version research is required for this story. Participant resolution uses the repository-pinned stack and sibling Hexalith.Parties client/contracts already present in the workspace. Story 2.1 already captured the current Microsoft Graph mailbox fields; 2.2 consumes the resulting source identity records and should not add new Graph scope or parsing behavior.

## Project Structure Notes

- `src/Hexalith.ChatBot.Server/Adapters/Parties/` is the expected home for `IParticipantDirectory`; it is absent today.
- `Hexalith.Parties` is a root-level sibling module. Do not edit Parties unless the implementation proves the adapter cannot meet this story with existing client/contracts.
- `src/Hexalith.ChatBot.Server/Association/Intake/` owns intake events from Story 2.1. Participant-resolution events should live in a sibling Association subfolder such as `Association/Participants/`.
- Contract additions belong under `src/Hexalith.ChatBot.Contracts/` and `Contracts/openapi/`; generated client files are regenerated, never hand-edited.
- Tests must mirror source boundaries under `tests/Hexalith.ChatBot.*.Tests/`.
- Do not initialize nested submodules or run recursive submodule commands.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/implementation-artifacts/2-1-microsoft-365-mailbox-intake-and-source-identity-capture.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.2-Participant-resolution-and-unresolved-unauthorized-handling]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Participants-Identity-and-Authorization]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Security-and-Privacy]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Reliability-and-Data-Integrity]
- [Source: _bmad-output/planning-artifacts/architecture.md#Structure-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]
- [Source: Hexalith.Parties/_bmad-output/project-context.md]
- [Source: Hexalith.Parties/src/Hexalith.Parties.Client/Abstractions/IPartiesQueryClient.cs]
- [Source: Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/PartyDetail.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/MailboxParticipantIdentity.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/MailboxRecipientIdentity.cs]
- [Source: src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/PassThroughAuthorizationStage.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs]
- [Source: Directory.Packages.props]
- [Source: global.json]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31T19:22:14+02:00 - `dotnet build src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj --no-restore -m:1 /nr:false` passed after adding root-level EventStore path support to the referenced Parties client/contracts projects.
- 2026-05-31T19:22:14+02:00 - `dotnet test tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1 /nr:false` and equivalent Architecture run aborted with VSTest `SocketException (13): Permission denied`; reran via compiled xUnit v3 binaries as required by the story.
- 2026-05-31T19:22:14+02:00 - `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` passed: 72 total, 0 failed.
- 2026-05-31T19:22:14+02:00 - `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false && tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` passed: 132 total, 0 failed.
- 2026-05-31T19:22:14+02:00 - `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` passed: 35 total, 0 failed.
- 2026-05-31T19:22:14+02:00 - `dotnet build tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj --no-restore -m:1 /nr:false && tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests` passed: 54 total, 0 failed.
- 2026-05-31T19:22:14+02:00 - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- 2026-05-31T19:22:14+02:00 - `git diff --check` passed.
- 2026-05-31T19:35:04+02:00 - Senior developer review found and auto-fixed four issues: incomplete unresolved review actions, ambiguous Parties `TotalCount` handling, malformed participant-resolution source payload hardening, and missing correlation/source mailbox fields in participant projection state.
- 2026-05-31T19:35:04+02:00 - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- 2026-05-31T19:35:04+02:00 - `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` passed: 72 total, 0 failed.
- 2026-05-31T19:35:04+02:00 - `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` passed: 144 total, 0 failed.
- 2026-05-31T19:35:04+02:00 - `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` passed: 35 total, 0 failed.
- 2026-05-31T19:35:04+02:00 - `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests` passed: 54 total, 0 failed.
- 2026-05-31T19:35:04+02:00 - `git diff --check` passed.
- 2026-06-10 - Story-automator re-review (adversarial). Verified AC1-5 and every `[x]` task against the current source surface (codebase has since evolved through later epics; participant-resolution feature remains intact and integrated).
- 2026-06-10 - `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors (compiles the previously-undocumented `CommandGatewayAdmissionApiE2ETests.cs` participant-resolution E2E tests).
- 2026-06-10 - `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` passed: 1516 total, 0 failed (includes participant-resolution + participant-authority E2E coverage).
- 2026-06-10 - `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` passed: 480 total, 0 failed.
- 2026-06-10 - `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` passed: 39 total, 0 failed (`ParticipantDirectoryShouldStayOutOfAggregatesAndGatewayStagesShouldStayOutOfAdapter` still green).
- 2026-06-10 - `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests` passed: 87 total, 0 failed.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-05-31.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added participant-resolution command contracts, ULID identity, status/reason/action enums, message-catalog codes, OpenAPI schemas, and regenerated the NSwag client.
- Added a ChatBot-owned Parties adapter seam with deterministic email canonicalization and explicit fail-closed outcomes for not found, ambiguity, stale/degraded projections, restricted/erased parties, unavailable directory, invalid evidence, and tenant mismatch.
- Routed participant resolution through the existing gateway/dispatcher spine; participant-directory I/O occurs before EventStore dispatch and aggregate `Handle` methods remain pure.
- Added participant-resolution events, structured rejections, aggregate state tracking, tenant-partitioned projection state, and idempotent projection handling without storing provider address/display-name PII in durable events/projections.
- Added participant-authority blocking and catalog-backed redacted responses for unresolved, unauthorized, email-only, and participant-directory-degraded actors before idempotency, audit envelopes, dispatch, or operation-status mutation.
- No UI implementation files were touched; UI/design primitive obligations remain satisfied by not introducing new visual contracts in this story.
- Senior developer review auto-fixed all confirmed high/medium issues and found no remaining critical issues.

### File List

- Hexalith.Parties/src/Hexalith.Parties.Client/Hexalith.Parties.Client.csproj
- Hexalith.Parties/src/Hexalith.Parties.Contracts/Hexalith.Parties.Contracts.csproj
- _bmad-output/implementation-artifacts/2-2-participant-resolution-and-unresolved-unauthorized-handling.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs
- src/Hexalith.ChatBot.Contracts/Commands/MailboxParticipantSourceReference.cs
- src/Hexalith.ChatBot.Contracts/Commands/ResolveMailboxMessageParticipants.cs
- src/Hexalith.ChatBot.Contracts/Commands/ResolvedMailboxParticipantReference.cs
- src/Hexalith.ChatBot.Contracts/Commands/UnresolvedMailboxParticipantEvidence.cs
- src/Hexalith.ChatBot.Contracts/Enums/ParticipantResolutionBlockedReason.cs
- src/Hexalith.ChatBot.Contracts/Enums/ParticipantResolutionStatus.cs
- src/Hexalith.ChatBot.Contracts/Enums/ParticipantReviewAction.cs
- src/Hexalith.ChatBot.Contracts/Identities/ParticipantResolutionId.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Server/Adapters/Parties/IParticipantDirectory.cs
- src/Hexalith.ChatBot.Server/Adapters/Parties/PartiesParticipantDirectory.cs
- src/Hexalith.ChatBot.Server/Adapters/Parties/UnavailableParticipantDirectory.cs
- src/Hexalith.ChatBot.Server/Association/Participants/MailboxParticipantQuarantined.cs
- src/Hexalith.ChatBot.Server/Association/Participants/MailboxParticipantRejected.cs
- src/Hexalith.ChatBot.Server/Association/Participants/MailboxParticipantResolutionInvalidRejection.cs
- src/Hexalith.ChatBot.Server/Association/Participants/MailboxParticipantResolved.cs
- src/Hexalith.ChatBot.Server/Association/Participants/MailboxParticipantUnresolved.cs
- src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs
- src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs
- src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs
- src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs
- src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs
- src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/IParticipantResolutionOrchestrator.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantResolutionOrchestrator.cs
- src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs
- src/Hexalith.ChatBot.Server/Projections/IParticipantResolutionProjectionStore.cs
- src/Hexalith.ChatBot.Server/Projections/InMemoryParticipantResolutionProjectionStore.cs
- src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionNotification.cs
- src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionProjectionHandler.cs
- src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionProjectionTranslator.cs
- src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionView.cs
- src/Hexalith.ChatBot.Server/Projections/PublishedParticipantResolutionEvent.cs
- tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/ParticipantResolutionContractTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Adapters/ParticipantDirectoryTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj
- tests/Hexalith.ChatBot.Server.Tests/Operations/ParticipantResolutionAggregateTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Projections/ParticipantResolutionProjectionTests.cs

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31.

### Findings Fixed

- High: unresolved participant states did not always expose the required safe review actions `link`, `create-pending`, `reject`, and `quarantine`; fixed the participant-directory unresolved result factory and adapter regression tests.
- High: ambiguous Parties searches could be resolved incorrectly when the page contained one item but `TotalCount` showed more than one match; fixed adapter ambiguity handling and added regression coverage.
- Medium: malformed participant-resolution commands with missing source participants/source mailbox identity could fault outside the intended dispatcher fail-closed path; hardened coarse idempotency input handling and dispatcher validation.
- Medium: participant-resolution projection state did not preserve correlation id and source mailbox identity; added both to the notification/view path and projection tests.

### Review Outcome

All confirmed issues were auto-fixed. Acceptance Criteria 1-5 and completed tasks were rechecked against the implementation surface. No critical issues remain, so the story is approved and marked `done`.

### Change Log

- 2026-05-31: Implemented Story 2.2 participant resolution, unresolved-state handling, authorization blocking, projection state, and regression tests. Story status set to review.
- 2026-05-31: Senior developer review auto-fixed participant-resolution review-action, ambiguity, fail-closed validation, and projection traceability gaps. Story status set to done.
- 2026-06-10: Story-automator re-review. Documented the previously-untracked participant-resolution E2E test file in the File List. Re-verified ACs/tasks and full suite (Contracts 480, Server 1516, Architecture 39, Conformance 87 — all green). No critical issues; story remains done.

---

## Senior Developer Review (AI) — 2026-06-10 re-review

Reviewer: Jérôme Piquot (story-automator adversarial review) on 2026-06-10.

### Method

Validated story claims against the *actual* current source surface — not the prior review notes. The chatbot codebase has evolved well past Story 2.2 (the gateway, `AcceptedCommandDispatcher`, and `ParticipantAuthorizationStage` now also carry epic 7–9 governance commands), so the review confirmed the participant-resolution feature is still intact, correct, and integrated rather than re-deriving the original diff.

### Acceptance Criteria — all confirmed implemented

- **AC1** — `PartiesParticipantDirectory` resolves sender/recipient evidence to tenant-scoped `PartyId` references through `IParticipantDirectory` over `IPartiesQueryClient` (tenant-scoped via `caseId` + `X-Hexalith-Tenant-Id`); `MailboxParticipantResolved` stores stable `PartyId` + metadata-only evidence refs, never upstream display-name/contact PII.
- **AC2** — `ParticipantDirectoryResolution.FromUnresolved` always exposes the four safe review actions `Link`, `CreatePending`, `Reject`, `Quarantine`; unresolved evidence carries only source-identity-level fields.
- **AC3 / AC4** — `ParticipantAuthorizationStage` denies `email-only`/`unauthorized`/`unresolved`/`directory-degraded` participant authorities with catalog-backed reason codes *before* dispatch/durable mutation and without confirming resource existence.
- **AC5** — Notification/view + idempotency carry correlation id, source mailbox/intake ids, evidence references, reason code, tenant-partitioned projection keys, and provenance/kernel/redaction/retention/schema stamps; coarse idempotency key = `tenant + intake + sorted participant evidence fingerprint + kernel version`.

### Findings

- **MEDIUM (documentation / transparency) — fixed.** `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` contained uncommitted participant-resolution + participant-authority E2E tests but was absent from the File List. Added it to the File List; the tests compile and pass. (Working tree left uncommitted — committing is the orchestrator's responsibility.)
- **LOW (defense-in-depth) — noted, not changed.** `PartiesParticipantDirectory.TryReadTenantFromPartyId` assumes a `tenant:type:id` `PartyId` shape that the `Hexalith.Parties` contract does not guarantee; on other shapes the *secondary* cross-tenant guard silently no-ops. Not a security gap — the primary tenant isolation is the tenant-scoped query (`caseId` + tenant header), which always applies. Left unchanged because tightening the parse could raise false `TenantMismatch` rejections against legitimately-scoped results.

### Review Outcome

Build clean (0/0). All four touched suites green (Contracts 480, Server 1516, Architecture 39, Conformance 87). No CRITICAL or HIGH issues. Story remains **done**.
