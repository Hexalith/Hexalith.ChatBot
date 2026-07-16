---
baseline_commit: fd2cadfe998a6fe0f40e3eebfc8f17d9e472f6b2
---

# Story 6.5: On-behalf-of disambiguation and external-sender posture

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As a security engineer,
I want delegated-send disambiguated and external senders flagged with a strictness knob,
so that authority and external posture drive safe association decisions.

## Acceptance Criteria

1. Given an inbound Microsoft 365/Exchange message where the provider expresses delegated send, when mailbox intake records sender identity, then ChatBot records the delegate as the sender authority identity and preserves the apparent/principal identity as `principal_for`. The mapping must use provider `sender`/`from` and selected `Sender`/`From` header evidence where available, must not trust raw header claims over provider identity, and must preserve metadata-only evidence refs for audit/reviewer use. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.5`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR48c`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Inbound Message Authenticity`]
2. Given an outbound send-on-behalf action, when sender authority is classified or sent after approval, then the existing `SenderAuthorityClassifier` semantics remain symmetric with inbound: requester/delegate is the recorded authority identity, `principal_for` is preserved, tenant policy `outbound.send-on-behalf-allowed` is enforced, and `delegation-mismatch` / `policy-blocked` denials remain metadata-only. Story 6.5 may extend stored/projection metadata for symmetry, but must not replace the classifier or add a second outbound authority pipeline. [Source: `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md#Dev Notes`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Authority class mapping (FR48 five-class taxonomy)`]
3. Given an inbound sender cannot be resolved to a tenant-scoped party, when intake/association processing runs, then the message carries `external_sender = true` through command/event/projection/association-risk metadata. If the sender resolves to an internal tenant party, the flag is `false`; unresolved or ambiguous resolution must fail safe as external unless an existing tenant-party resolver supplies a stronger finite state. [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.5`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR13`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR48d`]
4. Given tenant policy `mailbox.authenticity-strictness` is `permissive`, `strict`, or `paranoid`, when association routing evaluates an external sender and/or inbound authenticity anomalies, then `permissive` may allow normal auto-association if existing deterministic evidence and thresholds pass, `strict` routes the item to `NeedsReview`, and `paranoid` fails closed before auto-association while preserving original email context. Missing, invalid, or unavailable strictness policy uses a safe default of `strict` and records a finite metadata-only reason. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Tenant Policy Schema`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Inbound Message Authenticity`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR4`]
5. Given reviewer, conversation, audit, CLI, or MCP projections expose source email posture, then they show only finite safe fields: external-sender flag, strictness policy value, delegated-send state, delegate/principal refs, authenticity verdict tokens, discrepancy tokens, lifecycle/routing outcome, source version, redaction state, and evidence refs. They must not expose raw complete headers, message body, subject, bearer tokens, provider payloads, private keys, mailbox display names beyond existing authorization, or unauthorized tenant/party/project values. [Source: `_bmad-output/planning-artifacts/architecture.md#ChatBot Audit envelope`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction patterns and controls`; `_bmad-output/implementation-artifacts/6-4-inbound-authenticity-passthrough-and-header-inspection.md#Current State To Preserve`]
6. Given acceptance coverage runs, then tests prove: provider `sender`/`from` delegated-send mapping; header/provider conflict handling; outbound send-on-behalf symmetry; tenant-party unresolved/ambiguous/external sender posture; strictness `permissive`/`strict`/`paranoid` routing; invalid strictness safe default; unchanged message-intake idempotency key `tenant_id + mailbox_id + provider_message_id`; unchanged worker foreign-mailbox fail-closed behavior; metadata-only audit/problem/log/projection output; source-version-ordered projection replacement; OpenAPI/generated client shape if contracts change; and tenant isolation for foreign party/header/authenticity values. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `_bmad-output/implementation-artifacts/6-4-inbound-authenticity-passthrough-and-header-inspection.md#Testing Notes`]

## Tasks / Subtasks

- [x] Extend mailbox authority/authenticity contracts without duplicating Story 6.4 metadata (AC: 1, 3, 5, 6)
  - [x] Add focused contract records/enums near `MailboxAuthenticityMetadata`, `MailboxMessageSourceIdentity`, and `MailboxParticipantIdentity` for delegated sender posture: delegate authority identity, `principal_for`, provider evidence state, external-sender flag, and strictness policy snapshot.
  - [x] Keep finite wire tokens for strictness (`permissive`, `strict`, `paranoid`), delegated-send state (`not-delegated`, `delegated`, `ambiguous`, `not-supplied`, or equivalent), and routing reason tokens; do not localize machine tokens.
  - [x] Prefer additive fields on existing metadata snapshots instead of replacing `MailboxAuthenticityMetadata` or `MailboxHeaderInspectionSnapshot`; preserve old optional fields for compatibility.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` if the public contract spine changes.
- [x] Extend M365 mailbox worker mapping for delegated-send posture (AC: 1, 3, 5, 6)
  - [x] Use `GraphMailboxMessage.From`, `GraphMailboxMessage.Sender`, `GraphMailboxMessage.ReplyTo`, and selected `From`/`Sender`/`Reply-To`/`X-Original-Sender` headers already fetched by Story 6.4; do not request body/subject or broader Graph permissions.
  - [x] Treat provider `sender` different from provider `from` as the primary delegated-send signal. Record `sender` as delegate/authority and `from` as `principal_for`; use header discrepancies only as evidence/discrepancy refs, not as authority override.
  - [x] When provider identity and selected header evidence conflict, record a finite `ambiguous`/discrepancy reason and keep ingestion non-blocking; downstream strictness/risk controls decide routing.
  - [x] Preserve `GraphMailboxIntakeWorker.LeastPrivilegeGraphPermission = "Mail.Read"`, foreign mailbox notification fail-closed before fetch, fetched mailbox/message scope mismatch fail-closed before submit, and safe recoverable submission results.
- [x] Add tenant-party/external-sender posture inputs to association processing (AC: 3, 4, 6)
  - [x] Reuse existing tenant-party resolution concepts from Epic 2/participants where available; do not create a parallel party directory or trust email domain suffix alone as tenant-party proof.
  - [x] Extend `AssociationScoringInput`, `ScoreMailboxMessageAssociation`, and/or a narrow policy snapshot DTO with source-party resolution posture, external-sender flag, and `mailbox.authenticity-strictness`.
  - [x] Add finite association reason codes for external-sender strict routing/fail-closed and authenticity policy unavailable/invalid if existing `AssociationReasonCode` values are insufficient.
  - [x] Keep deterministic scoring weights and existing threshold validation intact; strictness is a routing guard over the existing result, not a hidden score boost or threshold rewrite.
- [x] Apply strictness routing in the aggregate/scoring path (AC: 4, 6)
  - [x] For `permissive`, allow existing deterministic association behavior to proceed, while retaining external/authenticity metadata in result/audit/projection fields.
  - [x] For `strict`, route external-sender and/or high-risk authenticity posture to `NeedsReview` even when the deterministic score would otherwise auto-associate.
  - [x] For `paranoid`, produce a fail-closed association outcome before auto-association for external-sender posture, preserving original email context and safe evidence refs.
  - [x] Missing/invalid strictness must use safe default `strict`; do not let policy absence silently downgrade to `permissive`.
  - [x] Preserve duplicate scoring/idempotency semantics: a second association id rejects as already recorded; message-intake idempotency remains keyed only by tenant, mailbox id, and provider message id.
- [x] Preserve and extend outbound send-on-behalf symmetry (AC: 2, 5, 6)
  - [x] Reuse `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassifier.cs` and `SenderAuthorityClassificationRequest.cs`; do not duplicate send-on-behalf classification in adapters, UI, CLI, MCP, or aggregate helper code.
  - [x] Ensure outbound approval/send events and projections preserve `principal_for` when `SenderAuthorityClass.SendOnBehalf` is selected and that send-time recomputation still rejects `delegation-mismatch` and `policy-blocked`.
  - [x] If public outbound projection fields are missing, add metadata-only delegate/principal refs without including raw Graph claims, mailbox display names, provider payloads, or message content.
- [x] Extend audit and projections with safe posture metadata (AC: 1, 3, 4, 5, 6)
  - [x] Extend `AuditEnvelopeFactory.SourceEvidenceRefs(...)` with finite refs such as `external-sender:true`, `authenticity-strictness:<value>`, `delegated-send:<state>`, `principal-for:<id>`, `delegate:<id>`, `party-resolution:<state>`, and `routing:<outcome>`.
  - [x] Extend `PublishedMailboxIntakeEvent`, `MailboxIntakeProjectionTranslator`, `ProjectConversationSourceEmailView`, `ProjectConversationItemView`, and contract query models so reviewer surfaces can display safe posture with source-version ordering.
  - [x] Keep projection handlers idempotent and source-version ordered; stale replays must not remove newer authenticity/delegation/external-sender fields.
  - [x] Add UI/localization tests only if the visible conversation/review component adds labels; labels must be concise, non-color-only, and accessible.
- [x] Add focused test coverage (AC: all)
  - [x] Contract/OpenAPI tests for delegated sender posture, `principal_for`, external-sender flag, strictness tokens, optional/missing fields, generated-client shape, and metadata-only serialization.
  - [x] Worker tests for provider `sender`/`from` delegated-send mapping, selected header conflicts, missing sender/from, malformed headers, repeated `Authentication-Results`, no body/subject forwarding, and unchanged foreign mailbox fail-closed paths.
  - [x] Aggregate/scorer tests for `permissive`, `strict`, `paranoid`, invalid policy defaulting to `strict`, existing threshold preservation, `NeedsReview` routing, fail-closed routing, duplicate scoring rejection, and original-context preservation.
  - [x] Outbound governance tests for send-on-behalf symmetry, `principal_for` retention, send-time authority recomputation, delegation mismatch, policy-blocked denial, and no second authority classifier path.
  - [x] Audit/projection tests for safe evidence refs, source-version replacement, no raw headers/body/provider payloads/problem-detail leakage, and reviewer-visible finite posture fields.
  - [x] Conformance/isolation tests proving foreign mailbox notifications, foreign fetched messages, and foreign party-resolution metadata do not submit, associate, or leak cross-tenant sender/header/authenticity data.
  - [x] Architecture tests if new public/internal boundaries are introduced; UI/CLI/MCP must continue to depend on `Hexalith.ChatBot.Client`, not server worker/provider/gateway/scoring internals.

## Dev Notes

### Scope Boundaries

- Story 6.5 completes FR48c/FR48d by making delegated sender identity and external-sender posture actionable in association/risk routing.
- Story 6.4 already records provider authenticity verdicts and selected header discrepancy tokens. Reuse those snapshots; do not create a second raw-header parser or re-verify SPF/DKIM/DMARC.
- Do not implement the full Tenant Policy Schema editor from Epic 7. This story may add the strictness policy snapshot/contract and server-side behavior needed to consume `mailbox.authenticity-strictness`; admin mutation UI belongs to Story 7.2.
- Do not change Microsoft Graph live permission posture. Inbound worker remains `Mail.Read`; outbound send behavior remains behind the approved adapter from Story 6.3.
- Do not change association thresholds `association.t-high` / `association.t-low` or introduce domain-based shortcuts. Strictness controls routing of external/authenticity posture, not score math.
- Do not add a new command spine or direct EventStore/Dapr write path. Worker/surface adapters continue through `IChatBotClient.SubmitAsync(..., ChatBotSurfaceOrigin.Mailbox)` and `CommandGateway`.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs` - current mailbox intake command with optional authenticity metadata.
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticityMetadata.cs`, `MailboxAuthenticationResultSnapshot.cs`, `MailboxHeaderInspectionSnapshot.cs`, and `MailboxSelectedHeaderSnapshot.cs` - Story 6.4 metadata-only authenticity/header model.
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxMessageSourceIdentity.cs` and `MailboxParticipantIdentity.cs` - current source identity and sender DTOs; likely homes or neighbors for delegate/principal refs.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxMessage.cs` - already carries provider `From`, `Sender`, `ReplyTo`, and `InternetMessageHeaders`.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs` - current M365 mapping and selected header parser; extend here instead of adding a parallel worker.
- `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs` - durable intake event to extend additively with posture metadata.
- `src/Hexalith.ChatBot.Server/Association/Scoring/DeterministicAssociationScorer.cs`, `AssociationScoringInput.cs`, and `AssociationThresholdPolicyValidator.cs` - existing deterministic scorer and threshold validation.
- `src/Hexalith.ChatBot.Contracts/Commands/ScoreMailboxMessageAssociation.cs`, `AssociationScoringResult.cs`, `AssociationReasonCode.cs`, and `AssociationThresholdPolicySnapshot.cs` - public association scoring contract surface.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - current handlers for mailbox intake, association scoring, and outbound operations.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassifier.cs` and `SenderAuthorityClassificationRequest.cs` - canonical outbound authority classifier, including send-on-behalf semantics.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - metadata-only audit evidence refs.
- `src/Hexalith.ChatBot.Server/Projections/PublishedMailboxIntakeEvent.cs`, `MailboxIntakeProjectionTranslator.cs`, `ProjectConversationSourceEmailView.cs`, and `ProjectConversationItemView.cs` - current projection path for reviewer-visible source-email metadata.
- `tests/Hexalith.ChatBot.Contracts.Tests/MailboxIntakeContractTests.cs`, `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Operations/AssociationAggregateTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`, and `tests/Hexalith.ChatBot.Conformance.Tests/M365MailboxEventActorIsolationTests.cs` - focused test homes.

### Current State To Preserve

- `CaptureMailboxMessageIntake` writes remain admitted through `CommandGateway`; no worker/provider/surface direct writes.
- `GraphMailboxIntakeWorker` currently records authenticity metadata even for malformed/missing headers and still submits if existing scope/source checks pass. Keep anomalies as metadata unless strictness/routing later blocks association.
- Message-intake idempotency remains `tenant + mailbox_id + provider_message_id` with indefinite replay. Header changes, delegate metadata changes, or strictness policy changes must not create a second intake artifact.
- Association scoring currently routes non-auto outcomes to `NeedsReview` and represents fail-closed association as `MailboxAssociationScoringFailedClosed` with lifecycle `NeedsReview`. Preserve original email context for rejected/deferred/failed/skipped/needs-review states.
- `SenderAuthorityClassifier` already maps send-on-behalf to requester/delegate plus `principal_for`; preserve denial reason ordering from Story 6.1 so `delegation-mismatch`, `membership-revoked`, `approval-missing`, and `policy-blocked` remain precise.
- `PublishedMailboxIntakeEvent` and project-conversation projections are idempotent and source-version ordered. New fields must not break stale replay ignore behavior.
- Root-level submodule policy applies: initialize/update only root `.gitmodules` submodules and never run recursive submodule commands.

### Architecture Guardrails

- Public command/query DTOs belong in `.Contracts`; worker/provider fetch models belong in `.Workers/Mailbox`; durable intake events belong in `.Server/Association/Intake`; association scoring stays in `.Server/Association/Scoring`; outbound authority stays in `.Server/Governance/Outbound`; projection shapes stay in `.Server/Projections`.
- Use structured records/enums and finite tokens. Avoid delimited strings, free-form policy values, domain-name heuristics, or raw provider/header payloads in contracts.
- Tenant ID comes from gateway/envelope context. Provider mailbox/message/header/sender values never establish tenant scope by themselves.
- Business denials and routing decisions return typed result/rejection/event outcomes. Do not throw for malformed delegated-send/authenticity metadata that can be represented safely.
- Keep diagnostics metadata-only across audit refs, logs, problem details, disabled reasons, worker results, projection labels, CLI/MCP output, and UI labels.
- If a strictness policy source is unavailable, fail safe to `strict` for routing and record a finite reason; do not silently auto-associate external senders.

### Latest Technical Information

- Microsoft Graph message resources expose `from`, `sender`, `replyTo`, and `internetMessageHeaders`; selected internet headers remain the correct adapter input for Story 6.5. [Source: Microsoft Learn, message resource, `https://learn.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0`]
- Microsoft Graph documents `internetMessageHeader` as name/value header pairs. ChatBot should keep using selected names and finite metadata states instead of retaining complete raw header blocks. [Source: Microsoft Learn, internetMessageHeader resource, `https://learn.microsoft.com/en-us/graph/api/resources/internetmessageheader?view=graph-rest-1.0`]
- Microsoft Graph send-from-another-user behavior distinguishes `sender` as the actual sending user and `from` as the represented mailbox/person for send-on-behalf. This matches the required delegate authority plus `principal_for` mapping. [Source: Microsoft Learn, Send Outlook messages from another user, `https://learn.microsoft.com/en-us/graph/outlook-send-mail-from-other-user`]
- Microsoft 365 Defender documentation says authentication results are stamped in `Authentication-Results` and include SPF, DKIM, DMARC, `compauth`, and reason fields. Story 6.5 should consume Story 6.4's supplied verdict tokens and must not recompute DNS/authentication checks. [Source: Microsoft Learn, Anti-spam message headers, `https://learn.microsoft.com/en-us/defender-office-365/message-headers-eop-mdo`; Troubleshoot email authentication, `https://learn.microsoft.com/en-us/defender-office-365/email-authentication-troubleshoot`]

### Previous Story Intelligence

- Story 6.1 added canonical sender-authority classes, conflict reason tokens, and `SenderAuthorityClassifier`. Do not duplicate this classifier for outbound symmetry.
- Story 6.1 review fixed denial reason ordering for approved service-send. Preserve precise safe reasons; do not collapse delegated mismatch into generic policy-blocked when the mismatch is known.
- Story 6.2 reinforced source-actor binding. Continue to use gateway/worker scope evidence rather than submitted sender/header claims.
- Story 6.3 added outbound approval/send contracts and send-time authority recomputation. Story 6.5 must keep send-on-behalf symmetry compatible with approved outbound send records.
- Story 6.4 added provider-supplied authenticity/header snapshots, finite discrepancy tokens, metadata-only audit/projection refs, and no raw header/body leakage. Extend those models instead of introducing a new authenticity layer.
- Story 6.4 review fixed repeated `Authentication-Results` parsing and capped discrepancy metadata to 32 entries. Preserve bounded shapes for any new posture arrays/maps.
- Recent commits:
  - `fd2cadf feat(story-6.4): Inbound authenticity header inspection`
  - `2d05649 feat(story-6.3): Outbound approval gate and send record`
  - `e5cce4b feat(story-6.2): Governed outbound draft creation`
  - `2aa9c82 feat(story-6.1): Sender authority classes and M365 mapping`
  - `5667c4b docs(epic-5): add retrospective`

### Project Structure Notes

- Likely new files:
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxDelegatedSenderSnapshot.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxExternalSenderPosture.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticityStrictnessPolicySnapshot.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/MailboxDelegatedSenderState.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/MailboxAuthenticityStrictness.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/MailboxExternalSenderPostureContractTests.cs`
- Likely update files:
  - `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticityMetadata.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxHeaderInspectionSnapshot.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxMessageSourceIdentity.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/ScoreMailboxMessageAssociation.cs`
  - `src/Hexalith.ChatBot.Contracts/Commands/AssociationScoringResult.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/AssociationReasonCode.cs`
  - `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
  - `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
  - `tests/fixtures/hexalith-chatbot-generated-client.sha256`
  - `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`
  - `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxMessage.cs` only if the current provider model lacks a needed field.
  - `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs`
  - `src/Hexalith.ChatBot.Server/Association/Scoring/AssociationScoringInput.cs`
  - `src/Hexalith.ChatBot.Server/Association/Scoring/DeterministicAssociationScorer.cs`
  - `src/Hexalith.ChatBot.Server/Association/Scoring/AssociationThresholdPolicyValidator.cs` only if policy validation is extended, not for threshold changes.
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
  - `src/Hexalith.ChatBot.Server/Projections/PublishedMailboxIntakeEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/MailboxIntakeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationSourceEmailView.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
  - `tests/Hexalith.ChatBot.Contracts.Tests/MailboxIntakeContractTests.cs`
  - `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Operations/AssociationAggregateTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Governance/Outbound/SenderAuthorityClassifierTests.cs`
  - `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
  - `tests/Hexalith.ChatBot.Conformance.Tests/M365MailboxEventActorIsolationTests.cs`
  - `tests/Hexalith.ChatBot.Architecture.Tests/*` if new boundaries are added.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` if OpenAPI/generated client changes.
  - `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` if new boundaries are introduced.
  - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` if visible conversation/review labels change.
- Sandbox note inherited from previous stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Use xUnit v3, Shouldly, NSubstitute/fakes, warnings-as-errors, central package management, `net10.0`, and no package/SDK upgrades.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 6 defines outbound communication/inbound authenticity and Story 6.5 defines FR48c/FR48d delegated-send and external-sender posture.
- Loaded PRD/addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`, especially FR13, FR48-FR48d, Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys, Inbound Message Authenticity, and Authority Class Mapping.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway invariants, mailbox adapter boundary, fail-closed invariant, audit envelope, project structure, metadata-only diagnostics, and testing strategy.
- Loaded UX context from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`; relevant carry-forward is safe actor/source status, external-party actor badges, evidence chips, review routing, accessible non-color-only states, and no invented fake chat behavior.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant carry-forward: .NET SDK `10.0.302`, `net10.0`, central package management, warnings-as-errors, metadata-only diagnostics, tenant isolation, pure EventStore aggregates, Dapr duplicate/order tolerance, xUnit v3/Shouldly/NSubstitute, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md`, `6-2-outbound-draft-creation-within-authority.md`, `6-3-outbound-approval-gate-and-approval-record.md`, and `6-4-inbound-authenticity-passthrough-and-header-inspection.md`.
- Inspected current likely reuse/update files: intake contracts, Graph mailbox worker/source model, authenticity metadata snapshots, aggregate intake/scoring handlers, deterministic association scorer, sender authority classifier, audit evidence refs, source-email projections, worker tests, aggregate tests, outbound classifier tests, projection tests, and M365 isolation conformance tests.
- Web research checked current Microsoft Learn pages for Graph message properties, internet message headers, send-on-behalf `sender`/`from` behavior, and Microsoft 365 authentication-result headers. No package/version changes are required.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 6 and Story 6.5 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR13, FR48-FR48d, FR4, FR10, FR16-FR18, NFR13a, NFR15a.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys, Inbound Message Authenticity, Authority Class Mapping.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, mailbox adapter boundary, audit envelope, fail-closed/metadata-only diagnostics, project structure, testing expectations.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - actor badges, source status, association review, external communication review, accessibility.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - actor/source/risk visual vocabulary.
- `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md` - sender-authority classifier foundation and review learnings.
- `_bmad-output/implementation-artifacts/6-3-outbound-approval-gate-and-approval-record.md` - outbound approval/send and send-time authority recomputation.
- `_bmad-output/implementation-artifacts/6-4-inbound-authenticity-passthrough-and-header-inspection.md` - latest previous story, authenticity metadata pipeline, tests, and review learnings.
- `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs` - current intake command.
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticityMetadata.cs` - current authenticity metadata root.
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxHeaderInspectionSnapshot.cs` - current selected header metadata.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxMessage.cs` - current Graph message worker model.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs` - current worker intake mapping.
- `src/Hexalith.ChatBot.Server/Association/Scoring/DeterministicAssociationScorer.cs` - current deterministic association scorer.
- `src/Hexalith.ChatBot.Server/Governance/Outbound/SenderAuthorityClassifier.cs` - current outbound authority classifier.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` - current intake, scoring, and outbound aggregate handlers.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - audit evidence refs.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationSourceEmailView.cs` - source-email metadata projection.
- Microsoft Learn Graph message resource - `https://learn.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0`.
- Microsoft Learn Graph internetMessageHeader resource - `https://learn.microsoft.com/en-us/graph/api/resources/internetmessageheader?view=graph-rest-1.0`.
- Microsoft Learn Send Outlook messages from another user - `https://learn.microsoft.com/en-us/graph/outlook-send-mail-from-other-user`.
- Microsoft Learn Anti-spam message headers - `https://learn.microsoft.com/en-us/defender-office-365/message-headers-eop-mdo`.
- Microsoft Learn Troubleshoot email authentication - `https://learn.microsoft.com/en-us/defender-office-365/email-authentication-troubleshoot`.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 129 tests.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 15 tests.
- `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none` - passed, 20 tests.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 517 tests.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 66 tests.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 37 tests.
- `git diff --check` - passed.

### Completion Notes List

- Added delegated sender, external sender, and authenticity strictness posture contracts and OpenAPI/client generation updates.
- Extended Graph mailbox intake mapping to prefer provider `sender`/`from` for delegated-send authority while recording selected-header conflicts as finite metadata.
- Threaded external sender and strictness policy through association scoring, events, audit evidence refs, association routing status, and project-conversation source-email projections.
- Applied strictness routing as a guard over deterministic scoring: permissive preserves existing behavior, strict routes external auto-associations to review, paranoid fails closed, and missing/invalid policy defaults to strict.
- Review fixed strictness routing so inbound authenticity anomalies also route/fail closed under the tenant strictness policy instead of only external-sender posture.
- Review hardened aggregate validation for contradictory delegated-sender and external-sender posture snapshots.
- Preserved outbound send-on-behalf classifier ownership by carrying optional authority classification metadata through approval events instead of adding a second classifier path.

### File List

- `src/Hexalith.ChatBot.Contracts/Commands/AssociationScoringResult.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticityMetadata.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxAuthenticityStrictnessPolicySnapshot.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxDelegatedSenderSnapshot.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxExternalSenderPosture.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxMessageSourceIdentity.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/RequestOutboundSendApproval.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/ScoreMailboxMessageAssociation.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationReasonCode.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/MailboxAuthenticityStrictness.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/MailboxDelegatedSenderState.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/MailboxPartyResolutionState.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxAssociationCandidatesGenerated.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxAssociationScoringFailedClosed.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociatedToProject.cs`
- `src/Hexalith.ChatBot.Server/Association/Scoring/AssociationScoringInput.cs`
- `src/Hexalith.ChatBot.Server/Association/Scoring/DeterministicAssociationScorer.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AssociationScoringOrchestrator.cs`
- `src/Hexalith.ChatBot.Server/Governance/Outbound/OutboundApprovalEvents.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationNotification.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/MailboxIntakeProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationSourceEmailView.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAssociationEvent.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedMailboxIntakeEvent.cs`
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/AssociationContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MailboxIntakeContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Association/Scoring/DeterministicAssociationScorerTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Senior Developer Review (AI)

Reviewed on 2026-06-02 by GPT-5 Codex.

Findings auto-fixed:

- HIGH: AC4/AC6 claimed strictness routing covered inbound authenticity anomalies, but `DeterministicAssociationScorer` only evaluated `ExternalSender`. Added optional `Authenticity` scoring input, finite `authenticity-strict-review` / `authenticity-paranoid-fail-closed` reason codes, OpenAPI/generated-client updates, and tests proving `permissive` / `strict` / `paranoid` behavior for authenticity anomalies.
- MEDIUM: Intake aggregate accepted contradictory delegated-sender snapshots, such as `delegated` without `principal_for` or `not-delegated` with a principal. Hardened aggregate validation and added regression tests.
- MEDIUM: Intake aggregate accepted contradictory external-sender snapshots, such as `external_sender = true` with `resolved-internal` or `external_sender = false` without an internal party ref. Hardened aggregate validation and added regression tests.
- MEDIUM: Story File List omitted modified test files discovered through git reality. Updated File List for the aggregate, projection, and API tests.
- LOW: Git reality also includes non-source automation artifact updates and a clean root-level `Hexalith.Tenants` submodule pointer change. These were documented as out of the source review scope and left intact.

Outcome: Approved after auto-fixes. No CRITICAL issues remain.

Re-reviewed on 2026-06-11 by Jérôme Piquot (story-automator review).

Scope note: the repository is many stories ahead of Story 6.5 (HEAD builds on Epics 7-9), so the baseline-to-HEAD diff is dominated by later work and the recorded test counts read low. The review validated Story 6.5's six acceptance criteria against the current implementation rather than against the raw diff.

Findings:

- AC1-AC6 re-verified as implemented and wired: worker `sender`/`from` delegated-send mapping with header-conflict `ambiguous` handling; external-sender fail-safe posture threaded source identity → `MailboxMessageIntakeCaptured` → projections; strictness routing wired `AssociationScoringOrchestrator` → `DeterministicAssociationScorer` → association events, with missing/invalid policy defaulting to `strict` and coverage for both external-sender and inbound-authenticity anomalies; outbound send-on-behalf symmetry preserved by carrying optional `AuthorityResult` through approval/send events without a second classifier; projections expose metadata-only posture (no raw header values, body, or subject). The prior review's auto-fix claims (authenticity-anomaly routing, contradictory-posture validation) were confirmed present in code.
- MEDIUM (auto-fixed): `DeterministicAssociationScorer.ApplyStrictness` emitted the PascalCase C# enum identifier (`AuthenticityStrictnessPolicyUnavailable` / `AuthenticityStrictnessPolicyInvalid`) for `RoutingReason` in the no-risk + missing/invalid-policy branch, instead of the finite kebab-case wire token used by every other branch. This violated the finite-token guardrail (AC4/AC5). Replaced `strictness.Reason?.ToString()` with a `StrictnessPolicyReasonToken` mapping that returns `authenticity-strictness-policy-unavailable` / `authenticity-strictness-policy-invalid` (and `null` when the policy is valid, preserving existing behavior). No test or fixture depended on the prior value.

Verification: `dotnet build tests/Hexalith.ChatBot.Server.Tests` succeeded with 0 warnings / 0 errors; `Hexalith.ChatBot.Server.Tests` compiled runner passed 1565 tests, 0 failed.

Outcome: Approved. No CRITICAL issues remain; status stays `done`.

### Change Log

- 2026-06-02: Implemented Story 6.5 delegated-send disambiguation, external-sender posture, strictness routing, safe projections/audit metadata, and focused validation coverage.
- 2026-06-02: Senior review auto-fixed authenticity-anomaly strictness routing, posture consistency validation, OpenAPI/generated client drift, and story File List completeness; marked story done.
- 2026-06-11: Story-automator re-review auto-fixed non-finite `RoutingReason` token in `DeterministicAssociationScorer` (no-risk + missing/invalid strictness policy branch now emits a finite kebab wire token); Server.Tests green (1565 passed). Status remains done.
