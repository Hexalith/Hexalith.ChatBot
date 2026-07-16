---
baseline_commit: 2d056499e3bb21b22b7ef28d73e8640514bfe6b5
---

# Story 6.4: Inbound authenticity passthrough and header inspection

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As a reviewer,
I want inbound provider authenticity verdicts and header discrepancies recorded,
so that authenticity signals inform association and risk without blocking ingestion.

## Acceptance Criteria

1. Given an inbound Microsoft 365/Exchange message, when mailbox intake runs, then ChatBot captures the provider-supplied DMARC, DKIM, SPF, and Microsoft composite-authentication verdicts from `Authentication-Results` / related internet headers as intake metadata and intake audit evidence. ChatBot must pass through these verdicts as supplied and must not perform independent DNS, DMARC, DKIM, SPF, ARC, or composite-auth re-verification. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.4`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR48a`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Inbound Message Authenticity`]
2. Given the message internet headers, when headers are parsed, then `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender` are inspected case-insensitively while preserving provider order for repeatable headers; `From` / `Sender` / `Reply-To` disagreements are recorded as finite metadata codes and safe evidence refs. Missing headers are recorded as `not-supplied`, not treated as parser failure. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.4`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR48b`]
3. Given authenticity failures, missing verdicts, malformed header values, multiple `Authentication-Results` headers, or `From` / `Sender` / `Reply-To` disagreements, when intake completes, then ingestion still creates the mailbox-intake record unless an existing intake gate fails for tenant scope, mailbox scope, source identity, idempotency, authorization, or audit availability. Authenticity anomalies feed later association/risk review metadata; they do not block ingestion in Story 6.4. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Inbound Message Authenticity`; `_bmad-output/planning-artifacts/architecture.md#Fail-closed invariant`]
4. Given the intake event is audited, projected, or surfaced to reviewers, then authenticity/header metadata remains metadata-only: no message body, subject, raw complete header block, bearer tokens, cookies, private keys, full provider payload, or unauthorized display values appear in audit refs, problem details, logs, disabled reasons, worker results, or public projection labels. [Source: `_bmad-output/planning-artifacts/architecture.md#ChatBot Audit envelope`; `_bmad-output/planning-artifacts/architecture.md#Anti-patterns`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR40`]
5. Given association scoring, AI risk classification, and reviewer conversation surfaces consume source-email metadata, then Story 6.4 makes authenticity verdicts and discrepancy codes available through stable contract/projection fields or safe evidence refs without changing auto-association thresholds, tenant `mailbox.authenticity-strictness`, on-behalf-of principal/delegate rules, or external-sender routing. Those policy decisions remain Story 6.5 unless already present and directly reusable. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.5`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR48c`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR48d`]
6. Given acceptance coverage runs, then tests prove: Graph worker/header mapping; contract/OpenAPI required and optional fields; aggregate event retention and duplicate replay behavior; metadata-only audit refs; no body/raw-header leakage; non-blocking authenticity anomalies; idempotency still keyed by `tenant_id + mailbox_id + provider_message_id`; source-email projection/reviewer visibility; and tenant/mailbox isolation for foreign mailbox/header data. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `_bmad-output/implementation-artifacts/6-3-outbound-approval-gate-and-approval-record.md#Testing Notes`]

## Tasks / Subtasks

- [x] Extend mailbox authenticity/header contracts in `Hexalith.ChatBot.Contracts` (AC: 1, 2, 4, 5, 6)
  - [x] Add focused records/enums for provider authenticity verdicts, selected internet headers, and discrepancy codes; prefer files near `CaptureMailboxMessageIntake`, `MailboxMessageSourceIdentity`, and mailbox participant identity contracts.
  - [x] Preserve stable finite wire tokens for SPF/DKIM/DMARC/compauth outcomes and discrepancy reasons; do not localize machine tokens.
  - [x] Extend `CaptureMailboxMessageIntake` and/or `MailboxMessageSourceIdentity` with optional authenticity/header metadata without breaking existing required source identity fields.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` if the contract spine changes.
- [x] Extend the M365 mailbox worker mapping (AC: 1, 2, 3, 4, 6)
  - [x] Extend `GraphMailboxMessage` with selected `internetMessageHeaders` metadata and any provider-derived authentication verdict fields available from the Graph source.
  - [x] In `GraphMailboxIntakeWorker.ToCommand`, map only selected headers/verdicts into metadata DTOs; preserve `Mail.Read` least-privilege posture and keep body/subject out of the command.
  - [x] Inspect header names case-insensitively; keep order for repeated `Received` and `Authentication-Results`; normalize discrepancy outputs into safe finite tokens.
  - [x] Treat missing or malformed authenticity data as metadata (`not-supplied`, `malformed`, `ambiguous`, or equivalent safe token) while still submitting intake if existing scope/source checks pass.
- [x] Persist metadata-only authenticity context in the intake aggregate/event (AC: 1, 2, 3, 4, 6)
  - [x] Extend `MailboxMessageIntakeCaptured` with authenticity verdict and header-discrepancy fields, or an equivalent nested metadata snapshot.
  - [x] Update `GovernedOperationAggregate.Handle(CaptureMailboxMessageIntake, ...)` validation to require only source identity, recipients, attachments, and safe metadata shape; do not require every authenticity header to exist.
  - [x] Preserve duplicate replay behavior: `GovernedOperationState.Apply(MailboxMessageIntakeCaptured)` remains idempotent and duplicate intake still rejects as already captured.
  - [x] Keep `SourceProvenance = "m365-mailbox-intake"`, `DerivationKernelVersion` versioned, `RedactionState = "metadata_only"`, and `RetentionClass = "collaboration_input"` unless architecture explicitly changes them.
- [x] Wire audit/idempotency/projection without bypassing the existing intake path (AC: 1, 3, 4, 5, 6)
  - [x] Extend `AuditEnvelopeFactory.SourceEvidenceRefs(...)` with metadata-only authenticity refs such as verdict/discrepancy tokens, selected header names, source mailbox, provider message, and phase; never include header values that can expose addresses beyond existing authorized display rules.
  - [x] Do not change `CoarseIdempotencyComposer.ComposeMessageIntakeRecord`: message intake remains keyed by tenant, mailbox id, and provider message id, not by headers or verdicts.
  - [x] Extend `PublishedMailboxIntakeEvent`, `MailboxIntakeProjectionTranslator`, `ProjectConversationSourceEmailView`, and conversation projection tests so reviewer surfaces can display safe authenticity posture and discrepancy codes with source version ordering.
  - [x] If association/risk inputs are extended, add metadata-only refs or finite tokens only; do not change scoring thresholds, low-risk policy, or approval decisions in this story.
- [x] Add focused test coverage (AC: all)
  - [x] Contract tests for JSON/OpenAPI shape, optional/missing headers, finite enum/wire tokens, metadata-only serialization, and no raw provider payload/body leakage.
  - [x] Worker tests for `$select=internetMessageHeaders`-style selected header input, multiple `Authentication-Results`, case-insensitive names, missing headers, disagreement detection, UTC timestamp preservation, and safe recoverable submission errors.
  - [x] Aggregate tests for event retention, malformed/missing authenticity metadata not blocking intake, duplicate replay, schema versioning, and no raw header/body leakage.
  - [x] Gateway/audit/idempotency tests for metadata-only evidence refs, audit-unavailable fail-closed behavior, unchanged message-intake idempotency, and no authenticity data in problem details or worker results.
  - [x] Projection/UI tests for reviewer-visible authenticity/discrepancy metadata, source version replacement, stale replay ignore behavior, and safe fallback for unknown provenance or unknown verdict token.
  - [x] Conformance/isolation tests proving foreign mailbox notifications or foreign fetched messages do not submit or leak header/authenticity values across tenants.
  - [x] Architecture tests only if new provider/header parser boundaries are introduced; surfaces must continue to depend on `Hexalith.ChatBot.Client`, not server worker/provider internals.

## Dev Notes

### Scope Boundaries

- Story 6.4 is inbound authenticity passthrough and selected header inspection. It records provider-supplied authenticity signals and disagreement metadata; it does not decide strict association posture.
- Do not implement Story 6.5 in this story: no `mailbox.authenticity-strictness` policy editor, no external-sender routing, no fail-closed/paranoid authenticity policy, and no on-behalf-of principal/delegate disambiguation beyond storing safe metadata already supplied.
- Do not re-verify SPF/DKIM/DMARC. Microsoft 365/Exchange is the provider source of truth for this story; ChatBot records verdicts as supplied.
- Do not add a separate intake writer, direct EventStore path, direct Dapr path, or provider-specific surface dependency. Existing worker -> `IChatBotClient.SubmitAsync(..., ChatBotSurfaceOrigin.Mailbox)` -> CommandGateway -> aggregate remains the path.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs` - current mailbox intake command; currently only source identity, recipients, and attachments.
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxMessageSourceIdentity.cs` - current source identity DTO; likely home or neighbor for authenticity metadata.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxMessage.cs` - current worker message model; currently has no header/authenticity fields.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs` - current mapping from Graph source model to `CaptureMailboxMessageIntake`; preserves mailbox origin and `Mail.Read`.
- `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs` - durable intake event to extend with metadata-only authenticity/header snapshot.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - `Handle(CaptureMailboxMessageIntake, ...)` validates and emits intake event.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` - intake replay idempotency state.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs` - dispatch plan for `CaptureMailboxMessageIntake`.
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs` - message-intake idempotency record; preserve tenant + mailbox + provider message key.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - add metadata-only authenticity evidence refs.
- `src/Hexalith.ChatBot.Server/Projections/PublishedMailboxIntakeEvent.cs`, `MailboxIntakeProjectionTranslator.cs`, and `ProjectConversationSourceEmailView.cs` - source-email projection and reviewer-visible metadata path.
- `tests/Hexalith.ChatBot.Contracts.Tests/MailboxIntakeContractTests.cs`, `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`, and `tests/Hexalith.ChatBot.Conformance.Tests/M365MailboxEventActorIsolationTests.cs` - focused test homes.

### Current State To Preserve

- `CaptureMailboxMessageIntake` durable writes remain admitted through `CommandGateway`; no surface, worker, or adapter writes aggregate state directly.
- `GraphMailboxIntakeWorker` fails closed before Graph fetch for foreign mailbox notifications and before submit for fetched mailbox/message scope mismatch; preserve no-leak worker result behavior.
- Existing source identity validation requires provider message id, mailbox id, internet message id, conversation id, source context, sender address, non-empty recipients, and valid attachment refs. Authenticity metadata should be optional and separately validated for safe shape.
- Existing message-intake idempotency is `tenant + mailbox_id + provider_message_id` with indefinite replay window. Header changes or retry-delivered updated verdicts must not create a second intake artifact.
- Intake projection handlers are idempotent and source-version ordered. Any new projection fields must preserve older-event ignore behavior and enrich existing conversation items without replacing unrelated history.
- Root-level submodule policy applies: initialize/update only root `.gitmodules` submodules and never run recursive submodule commands.

### Architecture Guardrails

- Put public command/query DTOs in `.Contracts`; worker/provider fetch models in `.Workers/Mailbox`; aggregate events in `.Server/Association/Intake`; projection shapes in `.Server/Projections`.
- Keep parsing deterministic and local: selected header extraction, case-insensitive header-name matching, safe tokenization, no DNS/network rechecks, no AI involvement.
- Use structured DTOs/enums rather than ad hoc delimited strings for verdicts and discrepancy codes where contracts cross assembly/API boundaries.
- Record header names and safe normalized verdict/discrepancy tokens; avoid storing raw full header blocks in audit/projection/public responses. If selected header values must be retained for reviewer evidence, keep them behind redaction-aware authorized detail and test no leakage to metadata-only surfaces.
- Business denials return typed rejection/domain results; do not throw for malformed authenticity metadata that can be represented safely.
- Tenant ID comes from gateway context. Provider mailbox/message/header data never establishes tenant scope by itself.

### Latest Technical Information

- Microsoft Graph v1.0 message resources expose `internetMessageHeaders`, and the official `Get message` example retrieves them with `$select=internetMessageHeaders`; this is the adapter fetch input for selected header inspection, not a reason to fetch body content. [Source: Microsoft Learn, Get message, https://learn.microsoft.com/en-us/graph/api/message-get?view=graph-rest-1.0]
- Microsoft Graph's `message` resource includes `internetMessageHeaders`, `from`, `replyTo`, `sender`, and message identity/timestamp fields that map to the current worker source model. [Source: Microsoft Learn, message resource, https://learn.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0]
- Microsoft Defender for Office 365 documents that inbound SPF, DKIM, and DMARC results are stamped in the `Authentication-Results` header; it also documents `compauth` and reason-code fields. ChatBot should parse and record these supplied fields, not recompute them. [Source: Microsoft Learn, Anti-spam message headers, https://learn.microsoft.com/en-us/defender-office-365/message-headers-eop-mdo]
- Microsoft Learn's email-authentication troubleshooting guidance confirms the relevant header fields include `spf`, `dkim`, `dmarc`, `compauth`, and `reason`. Treat unknown/new values as safe `unknown`/`not-supplied` metadata unless the project already has a stricter finite-token policy. [Source: Microsoft Learn, Troubleshoot email authentication, https://learn.microsoft.com/en-us/defender-office-365/email-authentication-troubleshoot]

### Previous Story Intelligence

- Story 6.1 added canonical sender-authority class tokens and `SenderAuthorityClassifier`; do not reuse outbound sender-authority enums for inbound authenticity verdicts unless the meaning exactly matches.
- Story 6.1 review fixed denial reason ordering for approved service-send. Preserve precise safe reason tokens and avoid masking one anomaly with another if multiple header discrepancies exist.
- Story 6.2 added governed draft creation and reinforced source-actor binding. For inbound authenticity, continue to trust gateway/worker scope checks, not submitted provider header claims.
- Story 6.3 added outbound approval/send contracts, metadata-only audit refs, approval/sender authority projection reuse, and send-time evidence freshness checks. Reuse the metadata-only evidence-ref style; do not overload outbound approval concepts for inbound authenticity.
- Recent commits:
  - `2d05649 feat(story-6.3): Outbound approval gate and send record`
  - `e5cce4b feat(story-6.2): Governed outbound draft creation`
  - `2aa9c82 feat(story-6.1): Sender authority classes and M365 mapping`
  - `5667c4b docs(epic-5): add retrospective`
  - `4d3ad3d test(story-5.4): Cross-surface equivalence verification`

### Project Structure Notes

- Likely new files:
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticityVerdict.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticationResultSnapshot.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxHeaderInspectionSnapshot.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxHeaderDiscrepancy.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/MailboxAuthenticationVerdictKind.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/MailboxHeaderDiscrepancyKind.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/MailboxAuthenticityContractTests.cs`
- Likely update files:
  - `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxMessageSourceIdentity.cs`
  - `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
  - `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
  - `tests/fixtures/hexalith-chatbot-generated-client.sha256`
  - `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxMessage.cs`
  - `src/Hexalith.ChatBot.Workers/Mailbox/IGraphMailboxMessageSource.cs` only if the fetch contract must explicitly request selected headers.
  - `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`
  - `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs`
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
  - `src/Hexalith.ChatBot.Server/Projections/PublishedMailboxIntakeEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/MailboxIntakeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationSourceEmailView.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/MailboxIntakeContractTests.cs`
  - `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
  - `tests/Hexalith.ChatBot.Conformance.Tests/M365MailboxEventActorIsolationTests.cs`

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` if new boundaries are added.
- Add UI tests only if new reviewer-visible authenticity fields are rendered by UI components in this story.
- Sandbox note inherited from previous stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Use xUnit v3, Shouldly, NSubstitute/fakes, warnings-as-errors, central package management, `net10.0`, and no package/SDK upgrades.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 6 defines outbound communication/inbound authenticity, and Story 6.4 defines FR48a/FR48b passthrough/header inspection.
- Loaded PRD/addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`, especially FR48a-FR48d, NFR15a, NFR31, NFR40, NFR59, and the inbound authenticity addendum.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway flow, mailbox adapter boundary, fail-closed invariant, audit envelope, project structure, and metadata-only diagnostic rules.
- Loaded UX context from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`; relevant carry-forward is reviewer-visible safe status, evidence chips, source email provenance, and accessible non-color status text.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant carry-forward: .NET SDK `10.0.302`, `net10.0`, central package management, warnings-as-errors, metadata-only diagnostics, tenant isolation, pure EventStore aggregates, Dapr duplicate/order tolerance, xUnit v3/Shouldly/NSubstitute, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/6-3-outbound-approval-gate-and-approval-record.md` and recent git history for Stories 6.1-6.3.
- Inspected current likely reuse/update files: intake contracts, Graph mailbox worker/source models, aggregate intake event/state/handler, gateway dispatcher, coarse idempotency, audit evidence refs, source-email projections, worker tests, aggregate tests, projection tests, and M365 isolation conformance tests.
- Web research checked current Microsoft Learn pages for Graph message headers and Microsoft 365 authentication-result headers. No package/version changes are required.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 6 and Story 6.4 acceptance source.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR48a-FR48d, NFR15a, NFR31, NFR40, NFR59.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - inbound authenticity, idempotency keys, tenant policy knobs.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, mailbox adapter boundary, audit envelope, project structure, fail-closed and testing expectations.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - reviewer-safe status/evidence behavior and localization/accessibility rules.
- `_bmad-output/implementation-artifacts/6-3-outbound-approval-gate-and-approval-record.md` - latest previous story implementation/test intelligence.
- `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs` - current intake command.
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxMessageSourceIdentity.cs` - current source identity DTO.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxMessage.cs` - current Graph message worker model.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs` - current worker intake mapping.
- `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs` - current intake event.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - current intake aggregate handler.
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs` - message-intake idempotency.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - audit evidence refs.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationSourceEmailView.cs` - reviewer source-email metadata projection.
- Microsoft Learn Graph Get message - `https://learn.microsoft.com/en-us/graph/api/message-get?view=graph-rest-1.0`.
- Microsoft Learn Graph message resource - `https://learn.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0`.
- Microsoft Learn Anti-spam message headers - `https://learn.microsoft.com/en-us/defender-office-365/message-headers-eop-mdo`.
- Microsoft Learn Troubleshoot email authentication - `https://learn.microsoft.com/en-us/defender-office-365/email-authentication-troubleshoot`.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-02T03:43:07+02:00 - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-02T03:43:07+02:00 - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` passed: 128 total, 0 failed.
- 2026-06-02T03:43:07+02:00 - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` passed: 15 total, 0 failed.
- 2026-06-02T03:43:07+02:00 - `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none` passed: 17 total, 0 failed.
- 2026-06-02T03:43:07+02:00 - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` passed: 501 total, 0 failed.
- 2026-06-02T03:43:07+02:00 - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` passed: 66 total, 0 failed.
- 2026-06-02T03:43:07+02:00 - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` passed: 37 total, 0 failed.
- 2026-06-02T03:58:11+02:00 - Senior review auto-fix validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-02T03:58:11+02:00 - Senior review auto-fix validation: `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` passed: 128 total, 0 failed.
- 2026-06-02T03:58:11+02:00 - Senior review auto-fix validation: `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` passed: 15 total, 0 failed.
- 2026-06-02T03:58:11+02:00 - Senior review auto-fix validation: `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none` passed: 18 total, 0 failed.
- 2026-06-02T03:58:11+02:00 - Senior review auto-fix validation: `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` passed: 505 total, 0 failed.
- 2026-06-02T03:58:11+02:00 - Senior review auto-fix validation: `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` passed: 66 total, 0 failed.
- 2026-06-02T03:58:11+02:00 - Senior review auto-fix validation: `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` passed: 37 total, 0 failed.

### Completion Notes List

- Added optional metadata-only inbound authenticity/header snapshots to mailbox intake commands, durable intake events, audit refs, and source-email/project-conversation projections.
- Added deterministic worker parsing for selected M365 internet headers: SPF, DKIM, DMARC, compauth, selected header presence/order, malformed/missing states, and finite discrepancy tokens.
- Preserved existing command gateway intake path, `Mail.Read` posture, tenant/mailbox scope checks, message-intake idempotency key shape, association/risk thresholds, and Story 6.5 policy scope boundaries.
- Regenerated the OpenAPI client and refreshed the generated-client SHA-256 fixture.
- Senior review fixed repeated `Authentication-Results` parsing so later ordered headers can supply missing verdicts and conflicting repeated verdicts are recorded as `ambiguous` instead of silently using the first header.
- Senior review capped authenticity discrepancy metadata to 32 entries in the aggregate/OpenAPI shape to keep public command metadata bounded.

### File List

- `_bmad-output/implementation-artifacts/6-4-inbound-authenticity-passthrough-and-header-inspection.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticationResultSnapshot.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticityMetadata.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxHeaderInspectionSnapshot.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxSelectedHeaderSnapshot.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/MailboxAuthenticationVerdictKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/MailboxHeaderDiscrepancyKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/MailboxHeaderValueState.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/MailboxIntakeProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationSourceEmailView.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedMailboxIntakeEvent.cs`
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxInternetMessageHeader.cs`
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxMessage.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/M365MailboxEventActorIsolationTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MailboxIntakeContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-02

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- **[High][AC1/AC2/AC6] Repeated `Authentication-Results` verdict parsing dropped provider-supplied fields after the first header.** The worker selected only the first `Authentication-Results` header for SPF/DKIM/DMARC/compauth extraction, so later ordered headers could supply verdicts that were recorded as `not-supplied`; conflicting repeated values were also silently masked. Fixed by folding all ordered authentication-result headers, filling missing verdicts from later headers, and emitting `ambiguous` for conflicting repeated verdicts. Regression coverage added in `GraphMailboxIntakeWorkerTests`.
- **[Medium][AC4/AC6] Public authenticity discrepancy metadata was unbounded.** Selected header lists were capped, but `HeaderInspection.Discrepancies` had no aggregate or OpenAPI `maxItems` limit. Fixed with a 32-item limit, aggregate uniqueness validation matching the OpenAPI `uniqueItems` contract, and aggregate rejection coverage.

Review checklist summary:

- Story status was reviewable, ACs and completed tasks were checked against implementation.
- Story File List matched the source/test files changed for Story 6.4; git also contains pre-existing/story-automation artifact changes outside application source, which were not reviewed as source code.
- Architecture and planning context were loaded from local planning artifacts. External doc search was not re-run during review because network access is restricted; the story already contains the relevant Microsoft Learn references used by the implementation.
- Security review focused on metadata-only audit/projection behavior, no raw header/body leakage, tenant/mailbox isolation, and bounded public metadata shape.
- Validation passed after fixes using the build and in-process test binaries listed in Debug Log References.

Reviewer: Claude (Opus 4.8) on 2026-06-11

Outcome: Approved (re-review). No critical issues. Implementation re-validated against all six acceptance criteria; no new code fixes required.

Re-review verification:

- **AC1 (passthrough, no re-verification):** `GraphMailboxIntakeWorker` parses provider-supplied SPF/DKIM/DMARC/compauth verdicts from `Authentication-Results` via deterministic local regex only — no DNS/network re-verification anywhere in the worker.
- **AC2 (header inspection):** `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, `X-Original-Sender` inspected case-insensitively; repeatable headers keep provider order with ordinals; From/Sender/Reply-To/X-Original-Sender disagreements emit finite `MailboxHeaderDiscrepancyKind` tokens; missing headers map to `not-supplied`, never a parser failure.
- **AC3 (non-blocking):** `GovernedOperationAggregate.Handle(CaptureMailboxMessageIntake)` treats `Authenticity` as optional and validates safe shape only (bounded 32-discrepancy cap with uniqueness, ordinal/name checks) — worker-normalized malformed/missing email headers still submit; idempotency key unchanged (`tenant + mailbox_id + provider_message_id`).
- **AC4 (metadata-only):** `AuditEnvelopeFactory` source-evidence refs emit verdict tokens, discrepancy codes, and selected header **names** only — never header values, body, subject, raw header block, or tokens. STJ enums serialize as wire-name strings (`JsonEnumMemberStringConverter`), so no integer-ordinal leakage.
- **AC5 (reviewer surfacing):** Authenticity flows captured event → `PublishedMailboxIntakeEvent` (wired via `MailboxIntakeProjectionTranslator`, registered through `MapMailboxIntakeProjectionEndpoints` in `Program.cs`) → `ProjectConversationSourceEmailView` → `ProjectConversationItemView` → public `ProjectConversationItem.Authenticity`; no association/risk thresholds or Story 6.5 policy knobs changed.
- **AC6 (coverage):** Build clean (0 warnings/0 errors). In-process xUnit suites green — Workers 31, Contracts 480, Server 1565, Conformance 93 (0 failed). OpenAPI `discrepancies` `maxItems: 32 / uniqueItems` matches the aggregate cap; selected-header `maxItems: 64` matches aggregate validation; generated-client SHA-256 fixture matches the generated client.

Transparency note (LOW): the working tree carries one uncommitted, passing regression test (`RepeatedReceivedHeadersShouldPreserveProviderOrderAndInspectOriginalSenderDisagreement` in `GraphMailboxIntakeWorkerTests.cs`, already listed in the File List) plus two `_bmad-output/` automation artifacts (excluded from source review). The added test compiles and passes; no code change required.

### Change Log

- 2026-06-02 - Implemented Story 6.4 inbound authenticity passthrough/header inspection and marked ready for review.
- 2026-06-02 - Senior Developer Review auto-fixed repeated authentication-result parsing and bounded discrepancy metadata; story marked done.
- 2026-06-11 - Story-automator re-review (Claude Opus 4.8): re-validated all six ACs against current implementation; build clean and Workers/Contracts/Server/Conformance suites green; no critical issues, no code fixes required; status remains done.
