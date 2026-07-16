---
baseline_commit: ab432966d6972d52533e01428d28b57e09efebac
---

# Story 2.6: Association decision recording, evidence preservation, and notes

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a compliance owner,
I want every association decision recorded as an event with preserved evidence and optional human rationale,
so that decisions are reconstructable and explainable later.

## Acceptance Criteria

1. **Association decisions are durable, actor-attributed domain events.** Given any association decision made by deterministic routing, an authorized reviewer, a retry/skip path, or a future correction path, when the command is accepted, then the resulting domain event records decision kind, actor, tenant, timestamp, source message/intake, association workflow id, selected/rejected/deferred target where applicable, signal/rule, confidence state, reason codes, correlation id, surface origin, and policy snapshot/version metadata. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.6-Association-decision-recording-evidence-preservation-and-notes; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Audit-Requirements; _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]

2. **Decision evidence is preserved as metadata-only, retention-governed evidence.** Given evidence used for an association decision, when the decision event is recorded, then the event and derived decision snapshot carry `confidenceScore`, `thresholdBand`, `evidenceRefs[]`, `kernelVersion`, `detectedAt`, `sourceProvenance`, `redactionState`, `retentionClass`, `schemaVersion`, and source identity needed to reconstruct the decision without storing raw email body, raw addresses, raw provider payload, secrets, or unauthorized project detail. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.6-Association-decision-recording-evidence-preservation-and-notes; _bmad-output/planning-artifacts/architecture.md#Format-Patterns; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR49-NFR54]

3. **Reviewer notes and rationale are safely persisted.** Given a reviewer resolves, rejects, defers, marks needs-review, quarantines, retries, or later corrects an association/participant workflow item, when they provide an optional note or rationale, then the note is recorded with the decision event as bounded user-authored text, redacted/sanitized for unsafe content, localizable for display, and available to permitted audit/history surfaces without leaking restricted evidence. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.6-Association-decision-recording-evidence-preservation-and-notes; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Journey-2-Business-Contributor-Resolves-an-Ambiguous-Project-Association; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]

4. **S2 actions submit through the existing command spine.** Given the Story 2.5 Association Review surface, when an authorized user chooses a candidate, rejects all, defers, or marks needs-review, then UI code submits the new first-party `IChatBotCommand` through `IChatBotClient.SubmitAsync(..., origin: ChatBotSurfaceOrigin.Ui)`, receives accepted/blocked/partial-success status, and re-queries the routing/audit status rather than mutating UI state optimistically. [Source: _bmad-output/implementation-artifacts/2-5-ambiguous-association-review-surface-s2.md#Current-Implementation-State; _bmad-output/planning-artifacts/architecture.md#CommandGateway-flow-the-spine-every-state-mutation-every-surface]

5. **Fail-closed, idempotency, lifecycle, and redaction behavior are test-proven.** Given stale evidence, expired evidence, unauthorized candidate/project, invalid lifecycle transition, duplicate decision within the idempotency window, audit writer down, command not allowlisted, or cross-tenant access, when an association decision is attempted, then no durable decision is written unless the command passes the gateway, authorization, idempotency, lifecycle, and pre-commit audit gates; the caller receives a catalog-backed safe problem/status and unauthorized resources remain suppressed. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR13-NFR15a; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class; _bmad-output/planning-artifacts/architecture.md#Format-Patterns]

## Tasks / Subtasks

- [x] Add association decision command contracts and wire schema (AC: 1, 3, 4, 5)
  - [x] Add first-party commands under `src/Hexalith.ChatBot.Contracts/Commands/`: `AssociateEmailToProject`, `RejectEmailProjectAssociation`, `DeferEmailProjectAssociation`, and `MarkEmailAssociationNeedsReview`.
  - [x] Include only stable ids/metadata: `associationId`, `intakeId`, selected `projectId` where applicable, `decisionKind`, optional bounded `decisionNote`, expected `candidateEvidenceFingerprint`/source version, and schema version. Do not put tenant authority, actor id, or surface origin in the command payload; those come from the gateway/envelope.
  - [x] Add shared decision metadata/value records if needed, but keep `Contracts` low-dependency and serialization-friendly.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit generated code.
  - [x] Add contract/OpenAPI tests for required fields, stable enum/wire tokens, JSON camelCase, and absence of raw PII/payload examples.

- [x] Add durable decision event types and aggregate handling (AC: 1, 2, 3, 5)
  - [x] Add past-tense domain events under `src/Hexalith.ChatBot.Server/Association/`, for example `MailboxEmailAssociationConfirmed`, `MailboxEmailAssociationRejected`, `MailboxEmailAssociationDeferred`, and `MailboxEmailAssociationMarkedNeedsReview`; use structured rejection events for invalid decisions.
  - [x] Reconcile deterministic auto-association with this decision model: either add the missing actor/surface/audit decision fields additively to `MailboxEmailAssociatedToProject` or emit a dedicated auto-decision event in the same aggregate transaction. Do not leave auto-association reconstructability dependent only on the scorer output.
  - [x] Each event must carry tenant, actor, `actorType` if available from envelope/claims, association workflow id, intake/source identity, decision kind, selected project id/display label only when authorized, confidence/evidence fields, note/rationale metadata, `SourceVersion`, schema version, redaction state, retention class, detected/decided UTC timestamps, and correlation id.
  - [x] Extend `GovernedOperationAggregate.Handle(...)` for decision commands as pure EventStore aggregate handlers. Business failures return `IRejectionEvent`; do not throw for stale evidence, missing candidate, duplicate decision, invalid note, or invalid lifecycle.
  - [x] Extend `GovernedOperationState` with replay state for association decision ids/kinds and source versions so duplicate decisions and terminal-state transitions are rejected deterministically.
  - [x] Preserve existing `ScoreMailboxMessageAssociation` behavior. Do not replace the deterministic scorer, existing `MailboxAssociationCandidatesGenerated`, `MailboxEmailAssociatedToProject`, or `MailboxAssociationScoringFailedClosed` events.

- [x] Wire command spine, allowlist, idempotency, and lifecycle guardrails (AC: 1, 4, 5)
  - [x] Add the new decision command types to `ChatBotSpineCommandAllowlist`.
  - [x] Extend `AcceptedCommandDispatcher` to deserialize/validate decision commands, serialize PascalCase payloads for EventStore, and route them to the correct aggregate id (`associationId`), preserving gateway correlation/task extensions.
  - [x] Extend `CoarseIdempotencyComposer` with the addendum contract: association decision key = `tenant_id + message_id/intake_id + decision_actor + decision_kind`, replay window = 24h, equivalent duplicate = reject/return "already decided" without re-execution, conflicting duplicate = safe metadata-only conflict.
  - [x] Reuse `CoarseIdempotencyOperationClass.AssociationDecision` or promote the existing anonymous `association-decision` entry to a named property to avoid string drift.
  - [x] Extend lifecycle validation so valid transitions include `NeedsReview -> Associated`, `NeedsReview -> Rejected`, `NeedsReview -> Deferred`, and `NeedsReview -> NeedsReview`; terminal states must not transition back. Correction/supersession remains Story 2.7.
  - [x] Ensure audit unavailable aborts admission, queues replay intent, emits operator alert, and writes no durable decision.

- [x] Preserve decision evidence and projection/read status (AC: 2, 3, 4)
  - [x] Extend `AssociationCandidateView`, `AssociationProjectionTranslator`, `PublishedAssociationEvent`, and `AssociationProjectionHandler` to project decision events into metadata-only decision snapshots while preserving source/candidate evidence.
  - [x] Add decision note/rationale fields to the read model only in redaction-safe form; bound notes to an explicit limit (recommended 1,024 normalized characters), reject control characters/secret-looking markers/raw provider payload markers, and keep raw or restricted text out of unauthorized query results, logs, and generated examples.
  - [x] Extend `AssociationRoutingStatus` if S2 must render accepted decisions, notes, status, audit/projection pending, or finite disabled reasons from projection state.
  - [x] Preserve `SourceVersion` order tolerance: duplicate/stale notifications are ignored, newer decision events supersede earlier candidate snapshots only by append-only projection state, never by mutating source events.
  - [x] Keep unauthorized candidates suppressed in events visible to callers, projections, audit history, accessibility labels, and export/copy/read-aloud surfaces.

- [x] Connect the Story 2.5 S2 surface to durable commands (AC: 3, 4, 5)
  - [x] Update `AssociationReviewService` with submit methods that map choose/reject/defer/mark-needs-review to the new command contracts and call `IChatBotClient.SubmitAsync(..., origin: ChatBotSurfaceOrigin.Ui)`.
  - [x] Update `AssociationReviewEffects`/reducers/actions to represent submit pending, accepted/projection-pending, audit reconciling, safe validation error, idempotency conflict, blocked, and success states.
  - [x] Update `ChatBotAssociationReviewActions` so decision controls are enabled when backend command/status permits; keep `aria-disabled` + reachable finite disabled reasons for candidate-required, evidence-expired, not-authorized, projection-pending, terminal-state, and already-decided.
  - [x] Re-query `GetAssociationRoutingStatusAsync` and operation/audit status after accepted submission; do not trust SignalR nudges or local preview state as durable decision state.
  - [x] Add/complete localized English/French text for success, blocked, note validation, already-decided, projection-pending, and audit-reconciling states.

- [x] Add audit/history and message-catalog coverage (AC: 1, 2, 3, 5)
  - [x] Ensure pre-commit and post-commit `AuditEnvelope` records expose the association decision command, resource id, decision kind, reason code, evidence refs, idempotency key hash, state transition, redaction decision, outcome, policy snapshot id, correlation id, and surface origin.
  - [x] If command-specific audit facts are needed, extend `AuditEnvelopeFactory` without logging command payloads or notes as raw text.
  - [x] Add stable message catalog codes and disabled-action reason tokens for association decision accepted, rejected, deferred, needs-review, already-decided, evidence-expired, stale-evidence, unauthorized-project-suppressed, and audit-unavailable.
  - [x] Keep `OperationAuditHistory` metadata-only; audit/history endpoints must collapse cross-tenant unknown/invalid ids to safe not-found/denied behavior.

- [x] Add focused tests and verification (AC: 1-5)
  - [x] Add contract tests for command serialization, OpenAPI schema, enum wire tokens, generated client drift, and message catalog completeness.
  - [x] Add aggregate tests for confirm/reject/defer/needs-review events, optional note/rationale, duplicate decision rejection, stale evidence rejection, invalid lifecycle rejection, metadata-only serialization, and no raw PII leakage.
  - [x] Add gateway/idempotency tests for 24h association-decision replay/conflict behavior, command allowlist admission, audit unavailable fail-closed, surface origin attribution, and safe problem details.
  - [x] Add projection tests for decision events, source-version ordering, note redaction, tenant partitioning, and routing-status disabled/next-action reason codes.
  - [x] Add UI service/effect/component tests for real submit path, note validation, accepted/projection-pending state, safe failures, localized strings, focus/error-summary behavior, and no raw exception text.
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run relevant Contracts, Client, Server, UI, UI.E2E/static, Architecture, and Conformance tests. If `dotnet test` hits sandbox VSTest socket limits, run compiled xUnit v3 test executables and record the limitation.
  - [x] Run `git diff --check`.

## Dev Notes

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Story 2.6 is the direct source and Epic 2 provides cross-story context.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` plus `addendum.md`; relevant sections cover association journeys, shared command pipeline, idempotency keys, fail-closed contract, audit requirements, data governance, and NFR13-NFR15a/NFR49-NFR54.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; the implementation must extend the ChatBot CommandGateway/EventStore spine, Association seam, Projections, and Audit seam.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`; S2 actions, notes, status feedback, disabled reasons, accessibility, and redaction behavior constrain the UI portion.
- Loaded persistent project-context facts from sibling `Hexalith.*` `_bmad-output/project-context.md` files; recurring constraints are root-level submodules only, .NET 10, central package management, EventStore CQRS/ES, DAPR/Aspire boundaries, tenant isolation, personal-data redaction, xUnit/Shouldly tests, and no generated-file hand edits.

### Source Artifact Analysis

Epic 2's value is trustworthy email-to-project association with human review when deterministic evidence is not strong enough. Story 2.6 is the compliance/audit bridge between the S2 review surface and later correction/propagation stories: decisions must be reconstructable, evidence-preserving, and explainable. [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-Project-Association; _bmad-output/planning-artifacts/epics.md#Story-2.6-Association-decision-recording-evidence-preservation-and-notes]

The product journey states that Marc confirms, rejects, defers, or escalates ambiguous associations, and the system records the decision as an auditable association event while preserving original email context. The compliance journey later needs candidate evidence, selected association, rejected alternatives, corrections, retries, deferrals, duplicate suppression, projection-pending states, and redacted detail. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Journey-2-Business-Contributor-Resolves-an-Ambiguous-Project-Association; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Journey-7-Compliance-or-Support-Reviewer-Investigates-a-Risky-Action]

The command list in the PRD already names association commands: `AssociateEmailToProject`, `RejectEmailProjectAssociation`, `DeferEmailProjectAssociation`, `MarkEmailAssociationNeedsReview`, `CorrectEmailProjectAssociation`, `ReprocessEmailAssociation`, and `QuarantineEmailAssociation`. This story should implement the S2 decision commands needed now and leave correction supersession to Story 2.7 and duplicate/retry/failure orchestration to Story 2.9 unless the reusable event shape is needed for forward compatibility. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Shared-Workflow-Contract; _bmad-output/planning-artifacts/epics.md#Story-2.7-Association-correction-and-supersession; _bmad-output/planning-artifacts/epics.md#Story-2.9-Duplicate-detection-retry-and-failure-states]

NFR15a explicitly lists both deterministic association decisions and ambiguous user association decisions as fail-closed state-writing paths. For ambiguous user decisions, stale evidence, missing project authority, and audit writer down must prevent durable mutation. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a-Fail-Closed-Contract-invariant-not-behavior]

The addendum defines association-decision idempotency as `tenant_id + message_id + decision_actor + decision_kind`, a 24h replay window, same-kind equivalence, and "already decided" conflict response. Existing code has an anonymous `association-decision` operation class but no composer branch yet; the dev agent should promote and use it rather than inventing another key scheme. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class; src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs]

### Previous Story Intelligence

Story 2.5 intentionally did not create durable decision commands/events. It built S2 over `AssociationRoutingStatus`, added local candidate selection and optional decision-note entry, and rendered choose/reject/defer/mark-needs-review actions disabled/local-preview while waiting for Story 2.6. This story should turn those controls into real submissions. [Source: _bmad-output/implementation-artifacts/2-5-ambiguous-association-review-surface-s2.md#Out-of-Scope; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor]

Actionable 2.5 patterns to preserve:

- UI reads through `IChatBotClient` and `AssociationReviewService`; it must not call Server projections, DAPR, EventStore, or stores directly.
- S2 evidence display suppresses restricted/redacted references; keep this behavior when adding notes, status, and audit links.
- Decision action disabled reasons preserve specific finite reasons before falling back to command-unavailable.
- `SignalR` nudges, if later wired, only trigger re-query. Do not trust nudge payloads as decision source of truth.
- Recent git history confirms the dependency chain: `ab43296 feat(story-2.5)`, `ddbb192 feat(story-2.4)`, `48b72c0 feat(story-2.3)`, `c04bcfd feat(story-2.2)`, `dee5423 feat(story-2.1)`.

### Current Implementation State

Existing association scoring contracts are `ScoreMailboxMessageAssociation`, `AssociationScoringResult`, `AssociationCandidate`, `AssociationEvidenceReference`, and `AssociationRoutingStatus`. They already carry confidence score, threshold band, reason codes, evidence refs, kernel version, detected timestamp, redaction state, retention class, schema version, source ids, and correlation id. Reuse these shapes for decision evidence; do not create parallel confidence/evidence models. [Source: src/Hexalith.ChatBot.Contracts/Commands/ScoreMailboxMessageAssociation.cs; src/Hexalith.ChatBot.Contracts/Commands/AssociationScoringResult.cs; src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs]

Existing scoring events are `MailboxEmailAssociatedToProject` for auto-association, `MailboxAssociationCandidatesGenerated` for review candidates, and `MailboxAssociationScoringFailedClosed` for fail-closed routing. These events are scoring/routing outcomes, not human decision events. Story 2.6 should add decision events rather than overloading these prior events or pretending S2 decisions are scorer results. [Source: src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociatedToProject.cs; src/Hexalith.ChatBot.Server/Association/MailboxAssociationCandidatesGenerated.cs; src/Hexalith.ChatBot.Server/Association/MailboxAssociationScoringFailedClosed.cs]

Do not miss the auto-association path. `MailboxEmailAssociatedToProject` currently lacks explicit actor/surface-origin decision fields, while the story and PRD require every auto-association and user-selected association to be reconstructable as a decision. The implementation must add that metadata without breaking existing scorer semantics or generated contract compatibility. [Source: src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociatedToProject.cs; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Technical-Success]

`GovernedOperationAggregate` is currently the scanned EventStore aggregate and already handles mailbox intake, participant resolution, association scoring, and threshold policy mutation. `Handle(...)` methods are pure, return `DomainResult`, and use structured `IRejectionEvent` for business failures. Add decision handlers here unless the implementation first introduces an architecture-approved association aggregate scanned by Server. [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs; src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs]

The gateway flow is already implemented as `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> lifecycle-validation -> pre-commit-audit -> dispatch -> post-commit-audit`. Do not bypass it from UI or server endpoints. Add decision commands to the allowlist, dispatcher, idempotency composer, and tests. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs; src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs]

Projection state currently stores metadata-only association routing in `AssociationCandidateView`, writes tenant-partitioned keys through `IAssociationProjectionStore`, and ignores duplicate/stale notifications by `SourceVersion`. Extend it to reflect decisions and notes without replacing the tenant/source-version guard. [Source: src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs; src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs; src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs]

S2 currently uses `AssociationReviewService`, `AssociationReviewEffects`, `AssociationReviewState`, and `ChatBotAssociationReviewActions`. The component already has a textarea for decision notes and finite disabled reasons; the implementation must make submit durable and preserve accessibility/localization behavior. [Source: src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs; src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewModels.cs; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor]

### Architecture Guardrails

- Runtime stack: .NET SDK `10.0.300`, `net10.0`, central package management, DAPR `1.17.9`, Aspire `13.3.x`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, NSwag `14.7.1`, xUnit v3 `3.2.2`, Shouldly, bUnit, and Playwright. Do not add inline package versions or upgrade packages for this story. [Source: global.json; Directory.Packages.props]
- Contract changes start in `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and typed contract records; generated client files are regenerated only. [Source: _bmad-output/planning-artifacts/architecture.md#Unified-Project-Structure; src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs]
- Every external write enters through `IChatBotClient.SubmitAsync` and the `CommandGateway`. UI/CLI/MCP must not replicate auth, authorization, idempotency, audit, or lifecycle stages. [Source: _bmad-output/planning-artifacts/architecture.md#CommandGateway-flow-the-spine-every-state-mutation-every-surface]
- EventStore owns write envelopes; domain events carry payload fields only. Aggregate `Handle` methods remain pure and never perform DAPR, HTTP, store, authorization, or audit I/O. [Source: Hexalith.EventStore/_bmad-output/project-context.md; src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- Evidence and audit are metadata-only. Never store or display raw mailbox body, raw addresses, raw provider payloads, local paths, secrets, unauthorized project names, unauthorized evidence, or raw exception text. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR49-NFR54]
- Use exact lifecycle strings and enum wire tokens. Do not invent `Pending`, `Done`, `Resolved`, or localized machine tokens. [Source: src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs; tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs]
- Audit writer unavailable is fail-closed for this story. Pre-commit failure means abort idempotency admission, queue replay intent, alert operator, return `AuditUnavailable`, and write no durable decision. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a-Fail-Closed-Contract-invariant-not-behavior]
- Submodules: initialize/update only root-level submodules declared in the repository root `.gitmodules`; never use recursive submodule commands. [Source: AGENTS.md#Git-Submodules]

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Contracts/
    Commands/
      AssociateEmailToProject.cs                 # NEW
      RejectEmailProjectAssociation.cs           # NEW
      DeferEmailProjectAssociation.cs            # NEW
      MarkEmailAssociationNeedsReview.cs         # NEW
      AssociationDecisionEvidenceSnapshot.cs     # NEW if useful
    Messages/
      ChatBotMessageCodes.cs                     # UPDATE stable codes
      ChatBotDisabledActionReasons.cs            # UPDATE finite reason tokens
    Queries/
      AssociationRoutingStatus.cs                # UPDATE only if read status needs decision/note fields
    openapi/hexalith.chatbot.v1.yaml             # UPDATE
  Hexalith.ChatBot.Client/
    Generated/HexalithChatBotClient.g.cs         # REGENERATE only
  Hexalith.ChatBot.Server/
    Association/
      MailboxEmailAssociationConfirmed.cs        # NEW
      MailboxEmailAssociationRejected.cs         # NEW
      MailboxEmailAssociationDeferred.cs         # NEW
      MailboxEmailAssociationMarkedNeedsReview.cs# NEW
      MailboxAssociationDecisionInvalidRejection.cs # NEW
    Operations/
      GovernedOperationAggregate.cs              # UPDATE
      GovernedOperationState.cs                  # UPDATE
    Gateway/
      ChatBotSpineCommandAllowlist.cs            # UPDATE
      Idempotency/CoarseIdempotencyComposer.cs   # UPDATE
      Idempotency/CoarseIdempotencyOperationClass.cs # UPDATE
      Stages/AcceptedCommandDispatcher.cs        # UPDATE
    Lifecycle/StateModel/                        # UPDATE transition definitions/tests if needed
    Projections/
      AssociationCandidateView.cs                # UPDATE
      AssociationProjectionTranslator.cs         # UPDATE
      PublishedAssociationEvent.cs               # UPDATE
  Hexalith.ChatBot.UI/
    Services/AssociationReviewService.cs         # UPDATE submit methods
    State/AssociationReview/*                    # UPDATE submit lifecycle
    Components/Governed/ChatBotAssociationReviewActions.razor # UPDATE enable/submit states
    Localization/*.resx                          # UPDATE EN/FR resources
tests/
  Hexalith.ChatBot.Contracts.Tests/
  Hexalith.ChatBot.Client.Tests/
  Hexalith.ChatBot.Server.Tests/
  Hexalith.ChatBot.UI.Tests/
  Hexalith.ChatBot.UI.E2E.Tests/
  Hexalith.ChatBot.Architecture.Tests/
  Hexalith.ChatBot.Conformance.Tests/
```

### Out of Scope

- Story 2.7 correction supersession, previous-decision superseded links, derived-context invalidation, and future-correction learning beyond reusable event/evidence/note shapes needed here.
- Story 2.8 correction propagation contracts and rebuild orchestration.
- Story 2.9 duplicate mailbox delivery, retry orchestration, retry queues, terminal/non-terminal failure backend handling, and dead-letter behavior beyond association-decision idempotency and safe decision states.
- CLI/MCP adapters. Preserve command/query parity-compatible contracts, but M1 surfaces implement CLI/MCP.
- New WORM backing technology. M0 uses existing pre/post audit envelope behavior; M2 adds full hash-chain backing. Do not block this story on choosing a storage product.
- New project/party/folder/conversation writes. Association decision recording may reference stable ids; sibling writes and attachment relinking belong to later stories unless already supported through approved adapter ports.
- Package upgrades, new UI framework, direct projection-store reads from UI, direct EventStore/DAPR calls from UI, generated-client hand edits, or recursive submodule initialization.

### Latest Technical Notes

No external version research is required for this story. Use the repository-pinned stack and current architecture decisions; this work is contract/domain/gateway/projection/UI wiring over existing .NET, DAPR, Aspire, Fluent UI, Fluxor, NSwag, and xUnit versions. Do not upgrade dependencies to satisfy Story 2.6.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/implementation-artifacts/2-5-ambiguous-association-review-surface-s2.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-Project-Association]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.6-Association-decision-recording-evidence-preservation-and-notes]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Journey-2-Business-Contributor-Resolves-an-Ambiguous-Project-Association]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Shared-Workflow-Contract]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR13-NFR15a]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR49-NFR54]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Shared-Command-Pipeline-architectural-invariant-for-FR81a]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class]
- [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Unified-Project-Structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Key-Flows]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: Hexalith.Projects/_bmad-output/project-context.md]
- [Source: Hexalith.Parties/_bmad-output/project-context.md]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/ScoreMailboxMessageAssociation.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs]
- [Source: src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor]
- [Source: src/Hexalith.ChatBot.Client/IChatBotClient.cs]
- [Source: Directory.Packages.props]
- [Source: global.json]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed.
- `dotnet test ... --no-build` was attempted for Contracts, Client, Server, and UI test projects, but VSTest failed in this sandbox with `System.Net.Sockets.SocketException (13): Permission denied`.
- Ran compiled xUnit v3 test executables as the sandbox-compatible validation path: Contracts 79/79, Client 14/14, Server 186/186, UI 86/86, Architecture 35/35, Conformance 54/54, UI.E2E 21/21, Workers 15/15, Integration 4/4 with 2 skipped infrastructure-dependent tests.
- `git diff --check` passed.
- Senior review auto-fix validation on 2026-05-31: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed.
- Senior review auto-fix validation on 2026-05-31: compiled xUnit v3 executables passed for Contracts 79/79, Client 14/14, Server 191/191, UI 86/86, Architecture 35/35, Conformance 54/54, UI.E2E 23/23, Workers 15/15, Integration 4/4 with 2 skipped infrastructure-dependent tests.
- Senior review auto-fix validation on 2026-05-31: `git diff --check` passed.
- Senior review auto-fix validation on 2026-06-10: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- Senior review auto-fix validation on 2026-06-10: compiled xUnit v3 executables passed with Chromium present for UI.E2E 68/68 (including `AssociationDecisionRecordingE2ETests` 2/2, previously 1 failed under a real browser), Server 213/213, UI 13/13, and Contracts 14/14 for the association decision suites.
- Senior review auto-fix validation on 2026-06-10: `git diff --check` passed.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-05-31.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added first-party association decision commands, decision kind enum, OpenAPI schema updates, regenerated typed client output, and message catalog/disabled reason tokens.
- Added metadata-only decision event handling, aggregate replay state, decision source snapshots, lifecycle/idempotency/allowlist/dispatcher wiring, and audit-envelope decision facts.
- Extended association projections and routing status with decision snapshots, source-version tolerance, and safe note metadata.
- Connected the Story 2.5 review surface to durable command submission through `IChatBotClient.SubmitAsync(..., origin: ChatBotSurfaceOrigin.Ui)` with pending, refreshed, and safe failure state handling.
- Added focused contract, server, projection, UI service/effect/component, E2E/static, architecture, conformance, worker, and integration validation coverage.

### Change Log

- 2026-05-31: Implemented association decision recording with evidence preservation, bounded safe notes, command-spine submission, projections/read status, audit metadata, and regression coverage for Story 2.6.
- 2026-05-31: Senior Developer Review auto-fixed decision projection evidence reconstruction, deterministic auto-association decision metadata timestamps, and association-decision idempotency conflict classification/equivalence.
- 2026-06-10: Senior Developer Review auto-fixed a broken generated E2E test (`AssociationDecisionRecordingE2ETests`) whose fail-closed case clicked an `aria-disabled` action, causing a Playwright actionability timeout under Chromium; switched to the repository's `aria-disabled` no-op assertion convention and recorded the new E2E file in the File List.

### File List

- `_bmad-output/implementation-artifacts/2-6-association-decision-recording-evidence-preservation-and-notes.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-2-20260531-161212.md`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/AssociateEmailToProject.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/DeferEmailProjectAssociation.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MarkEmailAssociationNeedsReview.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/RejectEmailProjectAssociation.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationDecisionKind.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Association/AssociationDecisionSourceSnapshot.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxAssociationDecisionInvalidRejection.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociatedToProject.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociationConfirmed.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociationDeferred.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociationMarkedNeedsReview.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociationRejected.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `src/Hexalith.ChatBot.Server/Gateway/IChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/CommandSubmissionLifecycleTransitionGuard.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionValidator.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationNotification.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAssociationEvent.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`
- `src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewActions.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewModels.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/AssociationContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/AssociationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AssociationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/AssociationDecisionRecordingE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewEffectsTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

## Senior Developer Review (AI)

### Review Date

2026-05-31

### Outcome

Approved after automatic fixes. No critical issues remain.

### Findings Fixed

1. HIGH - Decision projection did not match real decision event payloads. `AssociationProjectionTranslator` expected decision notifications to carry the prior `Candidates` collection, but the new decision events carry `candidateProjectIds`, `evidenceRefs`, and `confidenceInputs`. Fixed by extending `PublishedAssociationEvent` and reconstructing metadata-only candidate/evidence snapshots from decision payloads.
2. HIGH - Deterministic auto-association decision events could retain a default decision timestamp and incomplete actor/surface metadata in direct aggregate execution. Fixed by stamping auto-association events from the command envelope and scorer detection time fallback.
3. HIGH - Association decision idempotency treated a same-kind decision against a different project as equivalent replay. Fixed by keeping the required coarse key while adding command-equivalence fields and returning operation-specific safe conflict codes.

### Review Checklist

- Story file loaded from `_bmad-output/implementation-artifacts/2-6-association-decision-recording-evidence-preservation-and-notes.md`.
- Story status verified as reviewable before review and updated to `done` after fixes.
- Acceptance Criteria cross-checked against implementation and tests.
- File List reviewed and updated for review-touched files and pre-existing changed artifacts.
- Code quality, security, idempotency, projection, and metadata-only evidence handling reviewed on changed source files.
- MCP/web doc search not performed: story notes explicitly state no external version research is required and repository-pinned stack applies.
- Sprint status synced to `done`.

### Review Date (2026-06-10 follow-up)

2026-06-10

### Outcome

Approved after automatic fix. No critical issues remain. This pass reviewed the QA-generated E2E artifact added by the current story-automator run against the already-committed Story 2.6 implementation.

### Findings Fixed

1. HIGH - The newly generated `AssociationDecisionRecordingE2ETests.AssociationDecision_FailClosedStates_BlockDurableWriteAndSuppressRestrictedEvidence` E2E test called `ClickAsync()` on the `aria-disabled="true"` "Choose candidate" action. Playwright treats `aria-disabled` controls as not-enabled, so the click waited for actionability and timed out (~30s), failing the test whenever Chromium is available; it only "passed" via the no-browser fallback path used in the QA summary. Fixed by asserting `aria-disabled === "true"` (the repository E2E no-op convention used in `GovernedOperationsVisualFoundationE2ETests`) while preserving the no-command and `Durable decision writes: 0` assertions. Verified UI.E2E now passes 68/68 with a real browser.
2. MEDIUM - The new `tests/Hexalith.ChatBot.UI.E2E.Tests/AssociationDecisionRecordingE2ETests.cs` file was not recorded in the story Dev Agent Record → File List. Fixed by adding it to the File List.

### Findings Verified (no change required)

- AC1/AC2: The four decision events (`MailboxEmailAssociationConfirmed/Rejected/Deferred/MarkedNeedsReview`) carry tenant, actor, actorType, source identity, decision kind, candidate ids, metadata-only evidence refs, confidence/threshold band, reason codes, kernel/policy versions, detected/decided UTC timestamps, redaction state, retention class, source version, schema version, correlation id, and surface origin; no raw body/addresses/payload are present.
- AC3: Notes are bounded and sanitized server-side in `GovernedOperationAggregate.TrySanitizeDecisionNote` (1,024-char limit, control-character rejection, secret/raw-payload marker rejection) and normalized in `AssociationReviewService.NormalizeNote`.
- AC4: `AssociationReviewService.SubmitDecisionAsync` submits the first-party command through `IChatBotClient.SubmitAsync(..., origin: ChatBotSurfaceOrigin.Ui)` and re-queries `GetAssociationRoutingStatusAsync`.
- AC5: `ValidateDecision` fails closed on invalid payload, missing/stale evidence, already-decided, and non-`NeedsReview` lifecycle; `CoarseIdempotencyComposer.ComposeAssociationDecisionRecord` uses the addendum coarse key (tenant + intake + actor + kind) with a 24h window and a separate equivalence hash for conflict classification.
