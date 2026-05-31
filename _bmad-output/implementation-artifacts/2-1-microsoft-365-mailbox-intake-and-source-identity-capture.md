---
baseline_commit: a5ce13031f446f6f2b39b8247afc25261bfa1b28
---

# Story 2.1: Microsoft 365 mailbox intake and source-identity capture

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a project team,
I want one controlled M365/Exchange mailbox pattern ingested idempotently with full source identity preserved,
so that project email becomes a governed, traceable, duplicate-safe collaboration input.

## Acceptance Criteria

1. **Controlled mailbox events become governed intake records.** Given one configured controlled mailbox pattern for a tenant, when a mailbox event arrives, then a worker captures it as a project collaboration input and preserves source email identity, internet message ID, conversation/thread identity, mailbox identity, sender, recipients, timestamps, and attachment references. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.1-Microsoft-365-mailbox-intake-and-source-identity-capture]

2. **Duplicate provider delivery is suppressed by the message-intake idempotency key.** Given the message-intake operation class, when the same provider message is delivered twice, then the `tenant_id + mailbox_id + provider_message_id` idempotency key suppresses the duplicate, audits the suppression, and creates no duplicate intake record. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.1-Microsoft-365-mailbox-intake-and-source-identity-capture]

3. **Unresolved tenant scope and audit outage fail closed.** Given intake where tenant scope is unresolved or the audit writer is down, when processing runs, then no durable state is written, the intent is queued for replay, an operator-visible recoverable item exists, and the response/status is user-safe. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.1-Microsoft-365-mailbox-intake-and-source-identity-capture; _bmad-output/planning-artifacts/epics.md#NFR15a]

4. **Timestamps are UTC with source context preserved.** Given message source timestamps include provider timezone context, when intake records are persisted, then server-side timestamps are UTC `DateTimeOffset` values and source timestamp/timezone context remains available for later review without tenant-local conversion until presentation. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.1-Microsoft-365-mailbox-intake-and-source-identity-capture; _bmad-output/planning-artifacts/architecture.md#Format-Patterns]

5. **Mailbox origin and correlation are immutable.** Given the mailbox adapter/worker submits intake, when the command flows through the gateway, then `ChatBotSurfaceOrigin.Mailbox` or a clearly documented worker-origin mapping is attached at the adapter boundary, correlation propagates through command/event/audit/status, and downstream stages cannot rewrite origin. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns; src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs]

## Tasks / Subtasks

- [x] Add the contract spine for mailbox intake (AC: 1, 2, 4, 5)
  - [x] Add an imperative `IChatBotCommand` contract for message intake, for example `CaptureMailboxMessageIntake`, under `src/Hexalith.ChatBot.Contracts/Commands/`; do not use a `Command` suffix.
  - [x] Add contract-owned source identity/value records for provider message id, internet message id, conversation/thread id, mailbox id, sender, recipients, received/sent timestamps, attachment references, source timezone/context, and source schema version.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and generated client artifacts through the established contract-generation path; do not hand-edit `src/Hexalith.ChatBot.Client/Generated/*.g.cs`.
  - [x] Add contract tests for required fields, camelCase JSON, UTC timestamp serialization, ULID validation where ChatBot owns the id, and no `Guid` parsing.

- [x] Route intake through the existing CommandGateway and state-writing inventory (AC: 2, 3, 5)
  - [x] Extend `ChatBotSpineCommandAllowlist` deliberately for the new mailbox-intake command; keep the allowlist closed and test that unrelated commands remain rejected.
  - [x] Add a message-intake operation class to the coarse idempotency path so the key is exactly `tenant_id + mailbox_id + provider_message_id`, canonicalized with NFC and stable ordering before hashing.
  - [x] Keep all durable state writes behind `IAuditWriter.RecordPreCommitAsync`; use the existing `m365-mailbox-intake` path in `ChatBotStateWritingPathInventory` rather than inventing a second audit gate.
  - [x] Ensure duplicate suppression records an auditable fact/outcome without creating another intake aggregate/projection row.

- [x] Implement the mailbox intake aggregate/projection path in Server (AC: 1-4)
  - [x] Add the minimal Association/Mailbox or Intake server seam under `src/Hexalith.ChatBot.Server/Association/` and/or `src/Hexalith.ChatBot.Server/Adapters/Mailbox/`, following architecture seam naming.
  - [x] Add event/rejection records with EventStore naming conventions: events past tense, rejections structured and implementing the established rejection pattern, no localized/user text in events.
  - [x] Persist only the source identity and attachment references needed by downstream association and attachment stories; do not parse/store body content beyond this story's scope.
  - [x] Stamp derived records with `tenantId`, source provenance, derivation/kernel or schema version, redaction state, retention class, and schema version.
  - [x] Surface recoverable failed/unavailable states through the existing operation-status and message-catalog patterns.

- [x] Add the narrow worker lane for M365/Graph intake (AC: 1, 3, 5)
  - [x] Create `src/Hexalith.ChatBot.Workers/` if still absent, add it to `Hexalith.ChatBot.slnx`, and keep it dependent on `Hexalith.ChatBot.Client`/contracts rather than Server internals.
  - [x] Add a ChatBot-owned mailbox adapter port for Graph notifications/delta fetches; keep concrete Microsoft Graph calls behind this port so tests can use deterministic fakes.
  - [x] For M0, support one configured tenant mailbox pattern only; do not build the Story 7 mailbox-admin configuration UI or multi-pattern policy editor.
  - [x] Worker submission must construct the typed `IChatBotCommand` and call `IChatBotClient.SubmitAsync` with mailbox/worker origin; it must not call DAPR state stores, EventStore processors, audit writers, or gateway stage interfaces directly.

- [x] Add Graph provider mapping and resilience guardrails (AC: 1, 3, 4)
  - [x] Map Graph message fields including `id`/provider message id, `internetMessageId`, `conversationId`, sender/from, recipients, `receivedDateTime`, sent/created timestamps where available, and attachment references.
  - [x] Treat Graph webhook replay, duplicate notifications, revoked permission, expired token, throttling, subscription expiry, and partial access as scoped mailbox degradation, never tenant-wide fallback access.
  - [x] Use least-privilege Graph permissions for the chosen M0 intake mode and document the configured permission in code/tests; do not broaden to write/send permissions.
  - [x] Preserve Graph/delta state tokens as opaque provider state if introduced; never parse them or expose them in user-facing errors.

- [x] Add focused tests and evidence (AC: 1-5)
  - [x] Add Tier 1 aggregate/contract tests for successful capture, duplicate provider message suppression, UTC timestamp preservation, source identity preservation, and structured rejection events.
  - [x] Add gateway tests proving the mailbox-intake command uses the new operation class, aborts idempotency on pre-commit audit failure, queues replay intent, and emits an operator alert.
  - [x] Add architecture tests proving Workers and adapters do not reference `IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, `IIdempotencyStore`, Server gateway internals, DAPR clients, or EventStore processors directly.
  - [x] Add cross-tenant negative tests for the M365 event actor path: foreign tenant/mailbox ids fail closed with redacted problem/status and no candidate/evidence/resource leakage.
  - [x] Add deterministic fake-Graph worker tests for created notification, duplicate notification, missing tenant scope, audit unavailable, throttled/retryable fetch, and revoked/expired credential paths.

- [x] Verify and document results (AC: 1-5)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run compiled xUnit v3 binaries for touched test projects; prefer the compiled runner path because default VSTest sockets are blocked in this sandbox.
  - [x] Run `tests/Hexalith.ChatBot.Architecture.Tests`, `tests/Hexalith.ChatBot.Contracts.Tests`, `tests/Hexalith.ChatBot.Server.Tests`, and the new/updated worker tests.
  - [x] Run relevant conformance/isolation tests that cover M365 event actor and cross-tenant leakage.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, pass/fail counts, and any known environment limitations in the Dev Agent Record.

## Dev Notes

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 2 and Story 2.1 are the source of this story.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; no sharded architecture directory was present.
- No whole or sharded PRD/UX files matching the workflow input patterns were present under `_bmad-output/planning-artifacts`; UX requirements are embedded in `epics.md`.
- Loaded persistent project-context facts from sibling `Hexalith.*` `_bmad-output/project-context.md` files; the recurring constraints are .NET 10, central package versions, EventStore purity, DAPR/Aspire boundaries, tenant isolation, and xUnit/Shouldly testing.

### Source Artifact Analysis

Epic 2 starts the email intake and association flow. Story 2.1 is intentionally narrow: capture one controlled M365/Exchange mailbox pattern as a governed input, preserve source identity, make duplicate provider delivery idempotent, and fail closed when tenant binding or audit is unavailable. Participant resolution, scoring, candidate generation, ambiguity review, association decisions, correction, and retry/failure breadth are later Epic 2 stories. [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-and-Project-Association]

The existing Epic 1 foundation must be reused. `CommandGateway` already performs authentication, tenant binding, authorization, risk/approval seams, coarse idempotency, lifecycle validation, pre-commit audit, EventStore dispatch, post-commit audit, replay intent queuing, operator alerting, and operation-status updates. Story 2.1 must extend that path, not create a mailbox side channel. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; _bmad-output/implementation-artifacts/epic-1-retro-2026-05-31.md#Epic-2-Preview]

The state-writing path inventory already names `m365-mailbox-intake`. Treat that as the canonical fail-closed path for this story. Do not add a separate intake audit seam, and do not let the worker write durable state directly. [Source: src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs; _bmad-output/planning-artifacts/architecture.md#Process-Patterns]

There is currently no `src/Hexalith.ChatBot.Workers/` project in the workspace. The architecture explicitly expects `Hexalith.ChatBot.Workers` for mailbox ingestion and retry, so this story should create the narrow worker project and add it to the solution if still absent at implementation time. [Source: _bmad-output/planning-artifacts/architecture.md#Complete-Project-Directory-Structure]

### Current Implementation State

Relevant existing files likely to be updated:

- `src/Hexalith.ChatBot.Contracts/Commands/IChatBotCommand.cs` is the marker all state-mutating commands must implement.
- `src/Hexalith.ChatBot.Contracts/Commands/RecordGovernedNote.cs` is the only current concrete command and demonstrates command naming/no content leakage.
- `src/Hexalith.ChatBot.Client/ChatBotClient.cs` resolves command type names, creates ULID command/correlation ids, maps `ChatBotSurfaceOrigin`, and submits through generated transport.
- `src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs` already includes `Worker` and `Mailbox`.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` currently admits only `RecordGovernedNote`; mailbox intake will be rejected until this is extended.
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs` currently composes only command-execution idempotency from tenant, operation class, command name, command-input hash, and actor id. Story 2.1 needs a distinct message-intake operation class/key.
- `src/Hexalith.ChatBot.Server/Audit/AuditReplayIntent.cs`, `InMemoryAuditReplayIntentQueue`, and `InMemoryOperatorAlertSink` already support the fail-closed audit-unavailable path.
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs` is the existing operation-status shape for accepted/projection-pending/audit-reconciling states.
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` already includes lifecycle states and `SurfaceOrigin` values; update it through the established contract path.
- Tests to extend include `tests/Hexalith.ChatBot.Contracts.Tests`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, `tests/Hexalith.ChatBot.Architecture.Tests`, and `tests/Hexalith.ChatBot.Conformance.Tests`.

Preserve existing behavior:

- Do not weaken `RecordGovernedNote` walking-skeleton tests or remove it from the allowlist.
- Do not change generated client code by hand.
- Do not put tenant id in command body as trusted authority; tenant id comes from authenticated claims/binding. Any provider mailbox tenant hint is evidence only until the gateway binds scope.
- Do not log or surface body content, raw exception text, access tokens, provider delta tokens, sender/recipient detail in errors, or unauthorized project/candidate detail.
- Do not initialize nested submodules or run recursive submodule commands.

### Architecture Guardrails

- Runtime stack: .NET `10.0.300`, C# latest/net10.0, DAPR `1.17.9`, Aspire `13.3.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, central package management. Do not add inline package versions. [Source: global.json; Directory.Packages.props; _bmad-output/planning-artifacts/architecture.md#Selected-Starter]
- Write model: EventStore CQRS/ES, persist-then-publish, pure `Handle`/`Apply`, rejections-as-events, ULIDs, and `{tenant}:chatbot:{aggregateId}` identity. Aggregates/projections live in `.Server` only. [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- Gateway invariant: every state mutation flows through `CommandGateway`; adapters construct typed `IChatBotCommand` and call `IChatBotClient.SubmitAsync`; surface/worker adapters must not replicate gateway stages. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- Tenant isolation: tenant id comes from Keycloak/authenticated claims through tenant binding, never from the request body. Cross-tenant identifiers must fail closed without confirming resource existence. [Source: _bmad-output/planning-artifacts/architecture.md#Authentication-and-Security]
- Derived records carry `tenantId`, `sourceProvenance`, `derivationKernelVersion`, `redactionState`, `retentionClass`, and `schemaVersion`. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- Correlation id propagates across mailbox intake, association, file handling, approval, AI mediation, command execution, audit, and UI/CLI/MCP/workers. Logs and traces are metadata-only. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]
- Lifecycle strings are exact and shared: `Received`, `Proposed`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Corrected`, plus `Correcting` and `Correction-delayed`. [Source: src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml#LifecycleState]

### Latest Technical Notes

Web-verified on 2026-05-31 from Microsoft Learn: Microsoft Graph message resources expose the fields this story needs, including `id`, `internetMessageId`, `conversationId`, recipients, timestamps, and attachments. Use `$select` to fetch only required fields. [Source: https://learn.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0]

Web-verified on 2026-05-31 from Microsoft Learn: Graph delta query for messages is per mail folder, returns opaque `@odata.nextLink`/`@odata.deltaLink` tokens, supports `$select`, `$top`, `$expand`, and limited filtering/order by received time. Treat tokens as opaque provider state. [Source: https://learn.microsoft.com/en-us/graph/delta-query-messages]

Web-verified on 2026-05-31 from Microsoft Learn: Graph change notifications support Outlook messages on `/users/{id}/messages` and `/users/{id}/mailFolders/{id}/messages`; the service should fetch changed objects for basic notifications and must tolerate replay/duplicates. [Source: https://learn.microsoft.com/en-us/graph/change-notifications-overview]

Web-verified on 2026-05-31 from Microsoft Learn: Outlook change notifications require mail read permission for message subscriptions; shared/delegated shared permissions do not support change-notification subscriptions for another mailbox. Prefer the least privilege viable for M0 and constrain app access to the controlled mailbox. [Source: https://learn.microsoft.com/en-us/graph/outlook-change-notifications-overview; https://learn.microsoft.com/en-us/graph/permissions-reference?view=graph-rest-1.0]

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Contracts/
    Commands/CaptureMailboxMessageIntake.cs
    Commands/MailboxMessageSourceIdentity.cs
    Commands/MailboxRecipientIdentity.cs
    Commands/MailboxAttachmentReference.cs
    Events/MailboxMessageIntakeCaptured.cs
    Rejections/MailboxMessageIntakeDuplicateSuppressedRejection.cs
    Rejections/MailboxMessageIntakeTenantUnresolvedRejection.cs
    openapi/hexalith.chatbot.v1.yaml
  Hexalith.ChatBot.Server/
    Association/Intake/
    Adapters/Mailbox/
    Gateway/Idempotency/
  Hexalith.ChatBot.Workers/
    Mailbox/
tests/
  Hexalith.ChatBot.Contracts.Tests/
  Hexalith.ChatBot.Server.Tests/
  Hexalith.ChatBot.Workers.Tests/
  Hexalith.ChatBot.Architecture.Tests/
  Hexalith.ChatBot.Conformance.Tests/
```

Keep the shape smaller if the existing code supports a narrower change, but preserve the dependency direction: Contracts <- Client <- Workers, and Server owns aggregates, gateway internals, audit, projections, and adapters.

### Out of Scope

- Participant resolution through Parties, unresolved participant review actions, and external-sender posture decisions; those start in Story 2.2 and Epic 6.
- Deterministic project scoring, candidate generation, thresholds, ambiguous association routing, S2 UI, association decisions, correction/supersession, correction propagation, and retry/failure queues beyond what is needed to expose a recoverable intake failure.
- Attachment storage in Folders, conversation rendering in S1, AI context packaging, task intent detection, approval flows, outbound send/draft creation, mailbox-admin configuration UI, multi-mailbox policy editing, service-client administration, and operational dashboards.
- Rich notification encryption, production subscription renewal management, tenant-wide mailbox discovery, outbound M365 send permissions, body indexing, attachment download/storage, and raw mailbox content display.
- Package upgrades, new UI/component frameworks, recursive submodule initialization, direct Redis/Graph/EventStore writes from the worker, or bypassing the `IChatBotClient`/CommandGateway path.

## Project Structure Notes

- `src/Hexalith.ChatBot.Workers/` is expected by architecture but absent in the current workspace; creating it is part of this story if still absent.
- Mailbox provider integration belongs behind `src/Hexalith.ChatBot.Server/Adapters/Mailbox/` and/or worker-owned provider ports, not inside aggregate logic.
- Server aggregate/projection/intake logic belongs under `src/Hexalith.ChatBot.Server/Association/` or an adjacent architecture-approved seam, not a broad type bucket.
- Contract additions belong under `src/Hexalith.ChatBot.Contracts/` and `Contracts/openapi/`; generated client files are regenerated, never hand-edited.
- Tests must mirror source boundaries under `tests/Hexalith.ChatBot.*.Tests/`.
- Runtime topology uses EventStore actor state `statestore`, ChatBot derived state `chatbot-statestore`, and Redis pub/sub `chatbot-pubsub`; local mTLS-off DAPR uses `accesscontrol.local.yaml`, production stays deny-by-default. [Source: _bmad-output/implementation-artifacts/epic-1-retro-2026-05-31.md#Significant-Discoveries]

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.1-Microsoft-365-mailbox-intake-and-source-identity-capture]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-and-Project-Association]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#API-and-Communication-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns-and-Consistency-Rules]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries]
- [Source: _bmad-output/implementation-artifacts/epic-1-retro-2026-05-31.md#Epic-2-Preview]
- [Source: _bmad-output/story-automator/learnings.md]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs]
- [Source: src/Hexalith.ChatBot.Client/ChatBotClient.cs]
- [Source: src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml]
- [Source: Directory.Packages.props]
- [Source: global.json]
- [Source: https://learn.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0]
- [Source: https://learn.microsoft.com/en-us/graph/delta-query-messages]
- [Source: https://learn.microsoft.com/en-us/graph/change-notifications-overview]
- [Source: https://learn.microsoft.com/en-us/graph/outlook-change-notifications-overview]
- [Source: https://learn.microsoft.com/en-us/graph/permissions-reference?view=graph-rest-1.0]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `python3 _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-dev-story --key workflow` -> pass; no activation prepend/append steps.
- `dotnet restore src/Hexalith.ChatBot.Workers/Hexalith.ChatBot.Workers.csproj /p:RestoreUseStaticGraphEvaluation=true -v:normal` -> pass; plain restore failed silently in this sandbox for projects referencing the generated client, static graph restore produced assets.
- `dotnet restore tests/Hexalith.ChatBot.Workers.Tests/Hexalith.ChatBot.Workers.Tests.csproj /p:RestoreUseStaticGraphEvaluation=true -v:minimal` -> pass.
- `dotnet restore tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj /p:RestoreUseStaticGraphEvaluation=true -v:minimal` -> pass.
- `dotnet restore tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj /p:RestoreUseStaticGraphEvaluation=true -v:minimal` -> pass.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` -> pass, 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` -> pass, 69 total, 0 failed.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` -> pass, 118 total, 0 failed.
- `tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests` -> pass, 9 total, 0 failed.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` -> pass, 33 total, 0 failed.
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests` -> pass, 53 total, 0 failed.
- `git diff --check` -> pass.
- Review workflow: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` -> pass, 0 warnings, 0 errors.
- Review workflow: `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` -> pass, 120 total, 0 failed.
- Review workflow: `tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests` -> pass, 15 total, 0 failed.
- Review workflow: `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` -> pass, 69 total, 0 failed.
- Review workflow: `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests` -> pass, 54 total, 0 failed.
- Review workflow: `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` -> pass, 33 total, 0 failed.
- Review workflow: `git diff --check` -> pass.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-05-31.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added mailbox-intake contract records, OpenAPI schemas, generated client artifacts, and message catalog entry for message-intake idempotency conflicts.
- Routed `CaptureMailboxMessageIntake` through the closed spine allowlist, existing CommandGateway, message-intake coarse idempotency key, pre-commit audit fail-closed path, EventStore dispatch, and metadata-only audit suppression for provider duplicates.
- Added server intake event/rejection records under `Association/Intake` and extended the existing aggregate/state to persist source identity, UTC timestamps, attachment references, source provenance, redaction state, retention class, and schema version without body content.
- Added `Hexalith.ChatBot.Workers` with a deterministic Graph mailbox source port, one-pattern controlled mailbox worker, opaque provider state handling, least-privilege `Mail.Read` documentation, and `IChatBotClient.SubmitAsync` mailbox-origin submission.
- Added focused contract, gateway, aggregate, architecture, conformance, and fake-Graph worker tests covering acceptance criteria and fail-closed paths.
- Senior review fixed unresolved mailbox tenant-scope handling so it now queues a replay intent and emits an operator alert before returning a redacted problem.
- Senior review fixed worker-scope validation so fetched Graph messages must match the controlled mailbox and notification provider message id before submission.
- Senior review fixed worker submission-failure handling so gateway 401/403/503 responses return safe recoverable worker results without leaking provider state.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31

Outcome: Approved after automatic fixes. Critical issues remaining: 0. High/medium issues fixed: 3. Action items created: 0.

Findings fixed:

- HIGH: Unresolved tenant scope for mailbox intake returned a safe 403 but did not queue a replay intent or emit an operator-visible item, contrary to AC3. Fixed in `CommandGateway` by queueing `PreCommitOperationReplay` and emitting `TenantScopeUnresolved` for mailbox-intake tenant-missing failures before denial.
- HIGH: The worker trusted the fetched Graph message identity without verifying it still matched the controlled mailbox notification. A misbound source could submit a foreign mailbox/provider message. Fixed by requiring fetched `MailboxId` and `ProviderMessageId` to match the notification and configured pattern before command submission.
- MEDIUM: Worker submission failures from the gateway, including authorization/tenant-scope and audit-unavailable responses, bubbled as API exceptions instead of returning a safe recoverable mailbox result. Fixed by mapping generated 401/403/503 problem responses to sanitized recoverable worker results.

Validation checklist:

- Story file loaded; status was `review` before review and is now `done`.
- Epic/story id resolved as `2.1`; architecture and story references reviewed from `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/epics.md`, and the story Dev Notes.
- Tech stack confirmed as .NET 10, xUnit v3, Shouldly, DAPR/Aspire boundaries, central package management.
- Acceptance criteria and completed tasks cross-checked against contract, gateway, aggregate, worker, architecture, conformance, and test files.
- Git/file-list discrepancy checked against current status and baseline diff; review-added files are included below.
- External doc refresh was not re-run during review; the story already captured Microsoft Learn Graph references and the review changes did not alter external Graph API semantics.
- Tests and security review focused on tenant scope, mailbox scope, provider identity, audit outage, duplicate suppression, timestamp UTC behavior, and worker dependency boundaries.
- Sprint status synced to `done`.

### File List

- Hexalith.ChatBot.slnx
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs
- src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs
- src/Hexalith.ChatBot.Contracts/Commands/MailboxAttachmentReference.cs
- src/Hexalith.ChatBot.Contracts/Commands/MailboxMessageSourceIdentity.cs
- src/Hexalith.ChatBot.Contracts/Commands/MailboxParticipantIdentity.cs
- src/Hexalith.ChatBot.Contracts/Commands/MailboxRecipientIdentity.cs
- src/Hexalith.ChatBot.Contracts/Identities/MailboxMessageIntakeId.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeAlreadyCapturedRejection.cs
- src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs
- src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeInvalidRejection.cs
- src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs
- src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs
- src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs
- src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs
- src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs
- src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs
- src/Hexalith.ChatBot.Workers/Hexalith.ChatBot.Workers.csproj
- src/Hexalith.ChatBot.Workers/Mailbox/ControlledMailboxPattern.cs
- src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxAttachment.cs
- src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxFetchResult.cs
- src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxFetchResultKind.cs
- src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs
- src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxMessage.cs
- src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxNotification.cs
- src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxParticipant.cs
- src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxRecipient.cs
- src/Hexalith.ChatBot.Workers/Mailbox/IGraphMailboxMessageSource.cs
- src/Hexalith.ChatBot.Workers/Mailbox/MailboxIntakeWorkerResult.cs
- src/Hexalith.ChatBot.Workers/Mailbox/MailboxIntakeWorkerResultKind.cs
- tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj
- tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs
- tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj
- tests/Hexalith.ChatBot.Conformance.Tests/M365MailboxEventActorIsolationTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/MailboxIntakeContractTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs
- tests/Hexalith.ChatBot.Workers.Tests/Hexalith.ChatBot.Workers.Tests.csproj
- tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs

### Change Log

- 2026-05-31: Implemented Microsoft 365 mailbox intake contract, gateway/idempotency routing, server intake event path, worker Graph adapter lane, and focused validation tests.
- 2026-05-31: Senior review auto-fixed mailbox tenant-scope replay/alert handling, worker fetched-message scope validation, recoverable gateway submission handling, and related tests; story marked done.
