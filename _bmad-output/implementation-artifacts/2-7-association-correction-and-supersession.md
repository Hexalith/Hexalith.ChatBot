---
baseline_commit: c8e755edab37c5afe923d7342be2a118f72074d0
---

# Story 2.7: Association correction and supersession

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As a project owner,
I want to correct a wrong association while preserving the original decision in history,
so that contaminated context is repaired accountably without erasing the record.

## Acceptance Criteria

1. **Correction is a first-party command through the existing spine.** Given an associated email workflow, when an authorized project owner submits `CorrectEmailProjectAssociation`, then UI/API code submits only the typed `IChatBotCommand` through `IChatBotClient.SubmitAsync(..., origin: ChatBotSurfaceOrigin.Ui)`, the command passes the existing `CommandGateway` stages, and no UI, query, worker, or projection path writes correction state directly. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Command-and-Query-Contracts; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Shared-Command-Pipeline-architectural-invariant-for-FR81a; _bmad-output/planning-artifacts/architecture.md#CommandGateway-flow-the-spine-every-state-mutation-every-surface]

2. **Prior association is superseded, not mutated.** Given an existing `Associated` or `Corrected` association, when correction is accepted, then the system appends a correction/supersession event that records the new project association and links to the predecessor association/decision, while preserving the original decision event, evidence, actor, timestamp, confidence state, downstream-impact summary, and audit history unchanged. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.7-Association-correction-and-supersession; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR7; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR63]

3. **Correction fails closed on authority or dependency failure.** Given the corrector lacks ownership of the source or target project, the target project is unauthorized, the projection-invalidation queue is unavailable, audit writing is unavailable, the evidence/source version is stale, or the lifecycle transition is invalid, when correction is attempted, then no durable correction is written, the idempotency admission is aborted where applicable, the caller receives a catalog-backed safe reason, and unauthorized project/detail existence is not revealed. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a-Fail-Closed-Contract-invariant-not-behavior; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR57; _bmad-output/planning-artifacts/architecture.md#Fail-closed-NFR15a]

4. **Correction evidence and rationale are metadata-only and reconstructable.** Given a correction is recorded, when audit/history or association status is queried by an authorized actor, then the correction record carries tenant, actor, actor type, source message/intake, association workflow id, prior project id, corrected project id, correction kind, reason codes, evidence refs, confidence inputs when applicable, policy snapshot/version, correlation id, surface origin, UTC timestamps, bounded/sanitized rationale, redaction state, retention class, schema version, and supersession links without raw email body, raw addresses, raw provider payload, secrets, unauthorized project names, or raw exception text. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR60; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR62; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR49-NFR54; _bmad-output/planning-artifacts/architecture.md#Derived-record-shape-every-derived-class]

5. **Correction idempotency and lifecycle are explicit.** Given duplicate or conflicting correction submissions, when the same actor corrects the same message with the same correction kind, then the coarse idempotency key `tenant_id + message_id + correction_actor + correction_kind` is used indefinitely, equivalent duplicates surface "already corrected" without re-execution, conflicting duplicates return a safe conflict, and aggregate replay prevents a second in-place correction from corrupting the event stream. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR90; src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs]

6. **Correction status is visible without completing Story 2.8 propagation.** Given a correction is accepted, when Association Review, Conversation Detail, or Audit Investigation reads the item, then the read model shows the current corrected association, the predecessor/superseded link, correction rationale when authorized, safe downstream-impact preview/status, and `Corrected` lifecycle state; it must not implement full derived-store invalidation/rebuild orchestration, DAPR Workflow, or `Correcting`/`Correction-delayed` propagation beyond contracts needed to hand off Story 2.8. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.7-Association-correction-and-supersession; _bmad-output/planning-artifacts/epics.md#Story-2.8-Correction-propagation-contract; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow-4-Project-owner-repairs-a-wrong-association]

7. **Future evidence use is policy-gated.** Given tenant policy permits recorded corrections as future association evidence in M1, when a later association score uses correction history, then the correction evidence remains machine-readable, explainable, and inspectable with why-it-influenced details; in this story, persist the policy/evidence fields and keep actual learned/future scoring behavior off unless already policy-enabled. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.7-Association-correction-and-supersession; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR96; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low]

## Tasks / Subtasks

- [x] Add correction command contract and OpenAPI schema (AC: 1, 4, 5, 7)
  - [x] Add `CorrectEmailProjectAssociation` under `src/Hexalith.ChatBot.Contracts/Commands/` as an `IChatBotCommand`.
  - [x] Include stable metadata only: `associationId`, `intakeId`, `targetProjectId`, `correctionKind`, `correctionRationale`, `predecessorAssociationId` or predecessor decision/source version, `candidateEvidenceFingerprint`/evidence refs, `sourceVersion`, and `schemaVersion`.
  - [x] Do not put tenant authority, actor id, actor type, project-owner proof, or surface origin in the command payload; those come from authenticated context, authorization, and gateway envelope.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit generated code.
  - [x] Add contract tests for command serialization, OpenAPI shape, generated-client drift, enum/wire tokens, and metadata-only examples.

- [x] Add correction/supersession domain events and aggregate handling (AC: 2, 3, 4, 5, 7)
  - [x] Add past-tense events under `src/Hexalith.ChatBot.Server/Association/`, for example `MailboxEmailAssociationCorrected` plus a structured rejection such as `MailboxAssociationCorrectionInvalidRejection`.
  - [x] Record predecessor links (`supersedes` / `superseded_by` or story-consistent names) without mutating prior decision events.
  - [x] Extend `GovernedOperationAggregate.Handle(...)` for `CorrectEmailProjectAssociation` as pure EventStore aggregate logic. Do not perform DAPR, projection, authorization, project lookup, or audit I/O inside `Handle`.
  - [x] Permit only explicit lifecycle transitions such as `Associated -> Corrected` and `Corrected -> Correcting` only if needed as a handoff to Story 2.8; reject terminal-state in-place mutation.
  - [x] Extend `GovernedOperationState` to replay current association, predecessor/supersession metadata, correction source versions, and duplicate-correction prevention.
  - [x] Reuse Story 2.6 evidence/note sanitization patterns and `AssociationDecisionSourceSnapshot`; do not create parallel raw evidence or note storage.

- [x] Wire gateway, authorization, idempotency, allowlist, and fail-closed behavior (AC: 1, 3, 5)
  - [x] Add `CorrectEmailProjectAssociation` to `ChatBotSpineCommandAllowlist`.
  - [x] Extend `AcceptedCommandDispatcher` to validate and route the correction command to the association aggregate id, serialize PascalCase payloads for EventStore, and preserve correlation/task/surface extensions.
  - [x] Extend `CoarseIdempotencyComposer` with correction key composition: `tenant_id + message_id/intake_id + correction_actor + correction_kind`, indefinite replay window, equivalence fields, and safe conflict code.
  - [x] Keep the existing `CoarseIdempotencyOperationClass` correction class; promote it to a named property if needed to avoid string drift.
  - [x] Extend authorization tests/stages so correction requires project ownership for the current and target project; unauthorized or unknown target details must collapse to a redacted denial.
  - [x] Ensure projection-invalidation queue dependency readiness is checked before durable correction if this story introduces the invalidation enqueue seam; otherwise add an explicit no-op/test seam that can fail closed and is replaced by Story 2.8.
  - [x] Ensure audit unavailable aborts admission, queues replay intent, emits operator alert, and writes no durable correction.

- [x] Extend projections, query status, audit facts, and message catalog (AC: 2, 3, 4, 6)
  - [x] Extend `PublishedAssociationEvent`, `AssociationProjectionTranslator`, `AssociationProjectionHandler`, and `AssociationCandidateView` to project correction events with predecessor/current association links.
  - [x] Extend `AssociationRoutingStatus` only with fields the UI/query surface needs: corrected project, predecessor project/decision id, correction rationale metadata, correction actor/type, correction timestamp, downstream-impact preview/status, and supersession links.
  - [x] Preserve source-version ordering: duplicate/stale notifications are ignored; correction projection is append-only derived state over immutable events.
  - [x] Extend `AuditEnvelopeFactory` only with metadata facts needed to reconstruct the correction. Do not log raw command payload, raw rationale, raw mailbox content, or unauthorized project labels.
  - [x] Add message catalog codes and disabled-action reason tokens for correction accepted, already-corrected, stale evidence, target unauthorized/suppressed, projection-invalidation unavailable, audit unavailable, policy blocked, and invalid lifecycle.

- [x] Add S4 correction UI/service behavior using existing S2 patterns (AC: 1, 3, 4, 6)
  - [x] Extend `AssociationReviewService` or a narrowly named correction service to submit `CorrectEmailProjectAssociation` through `IChatBotClient.SubmitAsync`.
  - [x] Add or extend Fluxor actions/effects/reducers/models for correction pending, accepted/projection-pending, blocked, partial status, safe validation error, idempotency conflict, and refreshed status.
  - [x] Add correction controls to the appropriate Association Review/Conversation Detail context without creating a separate design system or decorative layout.
  - [x] Correction controls must be enabled, `disabled-with-reason`, or hidden when not applicable; disabled correction controls must remain focusable with `aria-disabled="true"` and an announced reason, or have an adjacent focusable explanation.
  - [x] Show correction rationale, affected-context preview, downstream-impact summary, and safe next action. Suppress unauthorized target project names/evidence and use the existing `ChatBotRecoveryPatternContract.ForCorrection` semantics.
  - [x] Add English/French localized text for correction success, partial success, blocked target, stale evidence, already corrected, projection pending, and validation errors.

- [x] Add focused tests and verification (AC: 1-7)
  - [x] Contracts/OpenAPI tests for correction command schema, generated client hash, stable tokens, and metadata-only examples.
  - [x] Aggregate tests for accepted correction, predecessor preserved, source-version stale rejection, duplicate correction rejection, invalid lifecycle rejection, rationale sanitization, and no raw PII leakage.
  - [x] Gateway/idempotency tests for allowlist admission, correction key/equivalence/conflict behavior, project-owner authorization, audit unavailable fail-closed, projection-invalidation unavailable fail-closed, and safe problem details.
  - [x] Projection/query tests for correction event translation, supersession links, source-version ordering, tenant partitioning, downstream-impact status, and unauthorized target suppression.
  - [x] UI service/effect/component tests for correction submit path, disabled reason accessibility, validation summary focus, localized messages, and safe blocked/partial states.
  - [x] Architecture/conformance tests proving UI uses Client only and no adapter replicates gateway stages.
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run relevant Contracts, Client, Server, UI, UI.E2E/static, Architecture, Conformance, Workers, and Integration tests. If `dotnet test` hits sandbox VSTest socket limits, run compiled xUnit v3 test executables and record the limitation.
  - [x] Run `git diff --check`.

## Dev Notes

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 2 and Story 2.7 are the direct source, with Stories 2.6, 2.8, and 2.9 defining adjacent boundaries.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`; relevant sections cover `CorrectEmailProjectAssociation`, correction idempotency, fail-closed correction conditions, FR7/FR8/FR60/FR62/FR63/FR96, and NFR13/NFR15a/NFR49-NFR54/NFR70.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; correction must extend the CommandGateway/EventStore spine, Association seam, Projections, Audit, lifecycle model, and metadata-only derived records.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`; S4 correction behavior is spine-only, with no mockup. The UX contract requires affected-context preview, rationale where policy demands it, focus-reachable disabled reasons, success/partial/blocked status, and no restricted target leakage.
- Loaded persistent project-context facts from sibling `Hexalith.*` `_bmad-output/project-context.md` files; recurring constraints are .NET 10, central package management, EventStore CQRS/ES, DAPR/Aspire boundaries, tenant isolation, personal-data redaction, xUnit/Shouldly tests, no generated-file hand edits, and root-level submodule initialization only.

### Source Artifact Analysis

Epic 2's value is trustworthy email-to-project association with repair paths when the original association is wrong. Story 2.7 is the accountable repair record: it records the corrected association and supersedes the prior decision while preserving the original event/audit history. Full propagation/rebuild of derived stores is Story 2.8. [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-Project-Association; _bmad-output/planning-artifacts/epics.md#Story-2.7-Association-correction-and-supersession; _bmad-output/planning-artifacts/epics.md#Story-2.8-Correction-propagation-contract]

The PRD names `CorrectEmailProjectAssociation` in the canonical command catalog and defines UJ4 as "Project owner corrects wrong association". The validation focus is correction, derived-context invalidation, and audit reconstructability; for this story, implement correction/supersession and a clear contract/status handoff, not the complete propagation workflow. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Command-and-Query-Contracts; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Traceability-Overview]

NFR15a explicitly lists `Correction` as a fail-closed state-writing path. Its fail-closed conditions are corrector lacks project ownership, projection-invalidation queue down, and audit writer down. Do not treat projection invalidation as an afterthought if the code introduces a queue/enqueue seam; durable correction must not happen when that required dependency is unavailable. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a-Fail-Closed-Contract-invariant-not-behavior]

The addendum defines correction idempotency separately from association decisions: key = `tenant_id + message_id + correction_actor + correction_kind`, replay window = indefinite, equivalence = same correction kind, conflict response = reject/surface "already corrected". Story 2.6 already implemented association-decision idempotency; extend the composer rather than inventing a new key scheme. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class]

FR96 allows recorded corrections to feed future association evidence only when policy permits and the evidence remains explainable. M0 scoring is deterministic-only; learned/prior-correction history enters M1. This story should persist first-class correction evidence and policy metadata but should not silently alter M0 scorer behavior. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR96]

### Previous Story Intelligence

Story 2.6 completed first-party association decision commands/events and explicitly left correction supersession, previous-decision superseded links, derived-context invalidation, and future-correction learning out of scope. Reuse its patterns rather than replacing them. [Source: _bmad-output/implementation-artifacts/2-6-association-decision-recording-evidence-preservation-and-notes.md#Out-of-Scope]

Actionable 2.6 patterns to preserve:

- Decision commands are typed `IChatBotCommand` records in `Contracts/Commands`, listed in OpenAPI, submitted through `IChatBotClient.SubmitAsync`, allowlisted in `ChatBotSpineCommandAllowlist`, routed by `AcceptedCommandDispatcher`, and executed by `GovernedOperationAggregate`.
- Aggregate handlers are pure and return `DomainResult.Rejection([...])` for business failures; never throw for stale evidence, duplicate decisions, invalid lifecycle, missing candidate, or invalid notes.
- Evidence snapshots are metadata-only and carry `confidenceScore`, `thresholdBand`, `evidenceRefs[]`, `kernelVersion`, `detectedAt`, `sourceProvenance`, `redactionState`, `retentionClass`, and `schemaVersion`.
- Optional notes/rationales are bounded to 1,024 normalized characters and reject control characters plus unsafe markers such as secrets, raw provider payloads, email-like markers, and local paths.
- Projection handlers ignore duplicate/stale notifications by `SourceVersion`; SignalR nudges only trigger re-query.
- Recent git history confirms the current baseline: `c8e755e feat(story-2.6)`, `ab43296 feat(story-2.5)`, `ddbb192 feat(story-2.4)`, `48b72c0 feat(story-2.3)`, `c04bcfd feat(story-2.2)`.

### Current Implementation State

There is no `CorrectEmailProjectAssociation` contract or correction event yet. The only correction-related implementation is foundation support: lifecycle vocabulary includes `Corrected`, `Correcting`, and `Correction-delayed`; the state model currently accepts `Associated -> Corrected`, `Corrected -> Correcting`, `Correcting -> Corrected`, `Correcting -> Correction-delayed`, and `Correction-delayed -> Corrected`; `CoarseIdempotencyOperationClass.All` already contains an anonymous `correction` class with indefinite replay window and `idempotency_conflict_correction`. [Source: src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs; tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs; src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs]

Existing association decision contracts are `AssociateEmailToProject`, `RejectEmailProjectAssociation`, `DeferEmailProjectAssociation`, and `MarkEmailAssociationNeedsReview`; the generated client and OpenAPI were updated in Story 2.6. Correction should mirror this shape where appropriate but must include predecessor/supersession and target project correction semantics. [Source: src/Hexalith.ChatBot.Contracts/Commands/AssociateEmailToProject.cs; src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml]

Existing decision events are `MailboxEmailAssociationConfirmed`, `MailboxEmailAssociationRejected`, `MailboxEmailAssociationDeferred`, and `MailboxEmailAssociationMarkedNeedsReview`. `MailboxEmailAssociationConfirmed` carries actor, tenant, source ids, selected project, candidate ids, evidence refs, confidence inputs, confidence score, threshold band, reason codes, policy/kernel metadata, `DetectedAt`, `DecidedAt`, source provenance, redaction/retention, `SourceVersion`, schema version, correlation id, surface origin, note, note redaction state, and policy snapshot version. Correction events should extend that metadata set with predecessor/current/supersession links instead of inventing a smaller audit shape. [Source: src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociationConfirmed.cs]

`GovernedOperationState` tracks `AssociationDecisionSource`, `LastAssociationDecisionSourceVersion`, and `AssociationLifecycleState`; after a confirmed decision it sets lifecycle to `Associated` but does not currently retain the selected project id/display label or a supersession chain. Story 2.7 must add replay state needed for correction validation and projection. [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs]

`GovernedOperationAggregate.ValidateDecision(...)` currently requires `NeedsReview`; correction must not reuse that method unchanged, because correction starts from `Associated` or a later corrected state. Reuse the note/evidence helper patterns, but implement correction-specific validation. [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]

`AcceptedCommandDispatcher` has a dedicated association-decision branch and serializes PascalCase payloads for EventStore. Add correction routing there; otherwise the fallback may route using `CommandId` and bypass aggregate replay for the association workflow. [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs]

`AssociationProjectionTranslator` and `AssociationProjectionHandler` already translate scoring/decision events into `AssociationCandidateView` and ignore stale source versions. Add correction event translation there; do not create a second projection store unless the implementation proves the existing association view cannot represent supersession links. [Source: src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs; src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs; src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs]

The UI currently has S2 association review service/effects/actions and a reusable correction recovery contract. Use `AssociationReviewService` only if the correction affordance naturally lives in the same page; otherwise add a narrow service that still depends only on `IChatBotClient`. Existing `ChatBotRecoveryPatternContract.ForCorrection(...)` requires policy rationale, affected-context preview, safe next action, and `Success|Partial|Blocked` status, and rejects raw failure/source text. [Source: src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs; src/Hexalith.ChatBot.UI/Design/ChatBotRecoveryPatternContract.cs]

### Architecture Guardrails

- Runtime stack: .NET SDK `10.0.302`, `net10.0`, central package management, DAPR `1.17.9`, Aspire `13.3.x`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, NSwag `14.7.1`, xUnit v3 `3.2.2`, Shouldly, bUnit, and Playwright. Do not add inline package versions or upgrade packages for this story. [Source: global.json; Directory.Packages.props]
- Contract changes start in `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and typed contract records; generated client files are regenerated only. [Source: _bmad-output/planning-artifacts/architecture.md#Unified-Project-Structure; src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs]
- Every external state mutation enters through `IChatBotClient.SubmitAsync` and the `CommandGateway`. UI/CLI/MCP must not replicate auth, authorization, idempotency, audit, lifecycle, projection invalidation, or correction policy checks. [Source: _bmad-output/planning-artifacts/architecture.md#CommandGateway-flow-the-spine-every-state-mutation-every-surface]
- EventStore owns write envelopes; domain events carry payload fields only. Aggregate `Handle` methods remain pure and never perform DAPR, HTTP, store, authorization, projection invalidation, or audit I/O. [Source: Hexalith.EventStore/_bmad-output/project-context.md; src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- Evidence, audit, problem details, projections, UI messages, logs, and tests are metadata-only. Never store or display raw mailbox body, raw addresses, raw provider payloads, local paths, secrets, unauthorized project names, unauthorized evidence, or raw exception text. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR49-NFR54]
- Use exact lifecycle strings and enum wire tokens. Do not invent `PendingCorrection`, `Resolved`, `Undo`, or localized machine tokens. [Source: src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs; tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs]
- Audit writer unavailable is fail-closed for correction. Pre-commit failure means abort idempotency admission, queue replay intent, alert operator, return `AuditUnavailable`, and write no durable correction. [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a-Fail-Closed-Contract-invariant-not-behavior]
- Submodules: initialize/update only root-level submodules declared in the repository root `.gitmodules`; never use recursive submodule commands. [Source: AGENTS.md#Git-Submodules]

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Contracts/
    Commands/
      CorrectEmailProjectAssociation.cs              # NEW
      AssociationCorrectionKind.cs or enum           # NEW if useful; stable wire tokens
    Queries/
      AssociationRoutingStatus.cs                    # UPDATE with correction/supersession fields only if needed
    Messages/
      ChatBotMessageCatalog.cs                       # UPDATE safe correction messages
      ChatBotMessageCodes.cs                         # UPDATE stable codes
      ChatBotDisabledActionReasons.cs                # UPDATE finite reasons
    openapi/hexalith.chatbot.v1.yaml                 # UPDATE
  Hexalith.ChatBot.Client/
    Generated/HexalithChatBotClient.g.cs             # REGENERATE only
  Hexalith.ChatBot.Server/
    Association/
      MailboxEmailAssociationCorrected.cs            # NEW
      MailboxAssociationCorrectionInvalidRejection.cs# NEW
    Operations/
      GovernedOperationAggregate.cs                  # UPDATE correction Handle/validation
      GovernedOperationState.cs                      # UPDATE selected/current/supersession replay state
    Gateway/
      ChatBotSpineCommandAllowlist.cs                # UPDATE
      Idempotency/CoarseIdempotencyComposer.cs       # UPDATE correction record
      Idempotency/CoarseIdempotencyOperationClass.cs # UPDATE named Correction property if useful
      Stages/AcceptedCommandDispatcher.cs            # UPDATE correction dispatch plan
    Lifecycle/StateModel/                            # UPDATE only if current transitions are insufficient
    Audit/AuditEnvelopeFactory.cs                    # UPDATE metadata-only correction facts
    Projections/
      PublishedAssociationEvent.cs                   # UPDATE correction fields
      AssociationProjectionTranslator.cs             # UPDATE correction event
      AssociationProjectionHandler.cs                # UPDATE if view mapping changes
      AssociationCandidateView.cs                    # UPDATE correction/supersession fields
  Hexalith.ChatBot.UI/
    Services/AssociationReviewService.cs or Correction service # UPDATE/NEW via Client only
    State/AssociationReview/* or Correction/*        # UPDATE/NEW correction lifecycle
    Components/Governed/*                            # UPDATE correction controls/status
    Localization/*.resx                              # UPDATE EN/FR text
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

- Story 2.8 full correction propagation: DAPR Workflow coordinator, invalidation/rebuild of every M0 derived store, per-store acknowledgements, progress estimates, SLO breach to `Correction-delayed`, P2 incident creation, and AI-context blocking until all stores acknowledge.
- Story 2.9 duplicate mailbox delivery, retry orchestration, retry queues, terminal/non-terminal failure backend handling, and dead-letter behavior beyond correction idempotency/conflict handling.
- M1/M2 learned or AI-assisted association ranking. Persist correction evidence/policy metadata now, but do not make prior corrections influence M0 scoring unless an explicit tenant policy and deterministic explainability path already exist.
- CLI/MCP adapters. Preserve parity-compatible contracts and command semantics; actual M1 surfaces implement CLI/MCP.
- New WORM backing technology. Use existing pre/post audit envelope behavior; M2 adds full hash-chain backing.
- Direct writes to Projects, Conversations, Folders, or Memories. Correction may reference stable ids and preview downstream impact; actual conversation/attachment/vector propagation belongs to later stories unless already available through approved adapter ports.
- Package upgrades, new UI framework, direct projection-store reads from UI, direct EventStore/DAPR calls from UI, generated-client hand edits, or recursive submodule initialization.

### Latest Technical Notes

No external version research is required for this story. Use the repository-pinned stack and current architecture decisions; this work is contract/domain/gateway/idempotency/projection/UI wiring over existing .NET, DAPR, Aspire, Fluent UI, Fluxor, NSwag, and xUnit versions. Do not upgrade dependencies to satisfy Story 2.7.

### Validation Notes

Checklist pass completed against `.agents/skills/bmad-create-story/checklist.md`:

- Reinvention prevention: story directs reuse of Story 2.6 command, evidence, note, aggregate, gateway, idempotency, projection, UI, and test patterns.
- Wrong-location prevention: source and test paths are enumerated and aligned to architecture boundaries.
- Regression prevention: prior decision immutability, metadata-only audit/evidence, fail-closed behavior, source-version ordering, generated-client regeneration, and UI accessibility constraints are explicit.
- Scope control: Story 2.8 propagation, Story 2.9 retry/duplicate orchestration, M1 learned scoring, CLI/MCP adapters, WORM backing, and sibling-context mutation are out of scope.
- LLM optimization: acceptance criteria and tasks use direct implementation language, with concrete command/event/class names and source references.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/implementation-artifacts/2-6-association-decision-recording-evidence-preservation-and-notes.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-Project-Association]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.7-Association-correction-and-supersession]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.8-Correction-propagation-contract]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Command-and-Query-Contracts]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Traceability-Overview]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR7]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR60]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR62]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR63]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR96]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR13-NFR15a]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR49-NFR54]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR70]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Shared-Command-Pipeline-architectural-invariant-for-FR81a]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low]
- [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Unified-Project-Structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow-4-Project-owner-repairs-a-wrong-association]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Error-Recovery-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: Hexalith.Projects/_bmad-output/project-context.md]
- [Source: Hexalith.Parties/_bmad-output/project-context.md]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md]
- [Source: Hexalith.Folders/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/AssociateEmailToProject.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs]
- [Source: src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociationConfirmed.cs]
- [Source: src/Hexalith.ChatBot.Server/Association/AssociationDecisionSourceSnapshot.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/PublishedAssociationEvent.cs]
- [Source: src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotRecoveryPatternContract.cs]
- [Source: src/Hexalith.ChatBot.UI/Design/ChatBotCorrectionRecoveryStatus.cs]
- [Source: Directory.Packages.props]
- [Source: global.json]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31: Resolved BMAD dev-story workflow customization; no workflow prepend/append steps or completion hooks were configured.
- 2026-05-31: Red phase: `dotnet test tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1 /nr:false` failed as expected before `CorrectEmailProjectAssociation` / `AssociationCorrectionKind` existed.
- 2026-05-31: VSTest sandbox limitation: `dotnet test` later failed to start the VSTest socket server with `System.Net.Sockets.SocketException (13): Permission denied`; final validation used built xUnit v3 executables.
- 2026-05-31: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-05-31: xUnit executable validation passed: Contracts 80, Client 14, Server 197, UI 87, UI.E2E 23, Architecture 35, Conformance 54, Workers 15, AppHost 3, Aspire 2, ServiceDefaults 3, Testing 37.
- 2026-05-31: Integration executable passed with 4 total, 2 skipped because Tier-3 Aspire E2E requires Docker/DAPR and `HEXALITH_CHATBOT_TIER3=1`.
- 2026-05-31: `git diff --check` passed.
- 2026-05-31: Senior developer review auto-fixed correction source-project authorization, projection-invalidation readiness fail-closed seam, correction evidence fingerprint validation, and corrected-project projection mapping. Revalidated with `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`, xUnit executables for Contracts 80, Client 14, Server 201, UI 87, UI.E2E 26, Architecture 35, Conformance 54, and `git diff --check`.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-05-31.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added first-party correction command contract, stable correction kind wire tokens, OpenAPI schema, generated client updates, and metadata-only contract/message catalog coverage.
- Added correction/supersession domain events, aggregate validation/replay state, idempotency composition, allowlist/dispatcher routing, authorization, lifecycle/problem details, and audit metadata facts.
- Extended association projection/query status with corrected project, predecessor/supersession links, rationale metadata, downstream-impact preview/status, and source-version ordering behavior.
- Added Association Review correction service/state/effect/component wiring through `IChatBotClient.SubmitAsync(..., origin: ChatBotSurfaceOrigin.Ui)` with focus-reachable disabled reasons and English/French correction text.
- Story 2.8 propagation remains out of scope; downstream impact is surfaced as preview/pending status only.
- Senior developer review hardened correction admission so correction commands carry an opaque prior project id for source ownership proof, require ownership of both prior and target projects, expose a no-op projection-invalidation readiness seam that can fail closed, reject correction commands with unknown evidence fingerprints, and preserve the corrected project id when projection events publish `correctedProjectId` without `projectId`.

### File List

- `_bmad-output/implementation-artifacts/2-7-association-correction-and-supersession.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/CorrectEmailProjectAssociation.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationCorrectionKind.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Association/MailboxAssociationCorrectionInvalidRejection.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociationCorrected.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IAssociationCorrectionDependencyReadiness.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/NoOpAssociationCorrectionDependencyReadiness.cs` (Story 2.7 fail-closed no-op seam; superseded in the working tree by `DefaultAssociationCorrectionDependencyReadiness.cs`/`StaticAssociationCorrectionDependencyReadiness.cs` once Story 2.8 propagation landed)
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/CommandSubmissionLifecycleTransitionGuard.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationNotification.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAssociationEvent.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewActions.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewEffects.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewFeature.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewModels.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewReducers.cs`
- `src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewState.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/AssociationContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/AssociationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AssociationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Senior Developer Review (AI)

Outcome: Approved after auto-fixes.

Findings fixed:

- HIGH - Correction authorization checked only the target project, so an actor owning the target but not the source project could submit a correction. Fixed by adding `PriorProjectId` to the typed command/OpenAPI/client, requiring ownership of both prior and target projects in `ParticipantAuthorizationStage`, and adding metadata-only denial tests.
- HIGH - Correction accepted arbitrary `CandidateEvidenceFingerprint` values as long as `SourceVersion` matched. Fixed by validating the fingerprint against the replayed association evidence before appending `MailboxEmailAssociationCorrected`.
- HIGH - Projection dependency readiness was represented only in UI disabled text, not as a fail-closed backend seam. Fixed with `IAssociationCorrectionDependencyReadiness` and a no-op default implementation that can fail closed before idempotency/audit/dispatch when unavailable.
- MEDIUM - Correction projection could lose the current project when EventStore publishes `correctedProjectId` without a generic `projectId`. Fixed `AssociationProjectionTranslator` to map correction events to the corrected project and added projection coverage.

Checklist validation:

- Story loaded and status verified as reviewable.
- Acceptance criteria, completed tasks, File List, source files, tests, security, and fail-closed paths reviewed.
- MCP/web research not performed; the story explicitly pins the repository stack and requires no external version research.
- Review notes appended, status updated to done, and sprint status synced.

#### Re-review 2026-06-10 (story-automator adversarial review)

Outcome: Approved. No CRITICAL or HIGH issues. All 7 acceptance criteria verified against the current working tree; all tasks marked `[x]` are genuinely implemented.

Verification evidence:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 Warning(s), 0 Error(s).
- xUnit v3 executables (the documented VSTest sandbox socket limit still applies, so compiled runners were used): Contracts 480, Client 34, Architecture 39, Conformance 87, Server 1525, UI 130, UI.E2E 68 — all passed, 0 failed/0 errored. (Counts exceed the 2026-05-31 figures because the working tree now carries the full Epic 1–11 implementation, not only Story 2.7.)
- `git diff --check` clean.
- AC1 spine-only path confirmed (`CorrectEmailProjectAssociation` → `IChatBotClient.SubmitAsync(..., origin: Ui)` → `ChatBotSpineCommandAllowlist` → `AcceptedCommandDispatcher` → pure `GovernedOperationAggregate.Handle`; Architecture/Conformance suites enforce UI-via-Client-only).
- AC2 supersession confirmed in `MailboxEmailAssociationCorrected` (predecessor/supersedes links, source snapshot preserved, no prior-event mutation).
- AC3 fail-closed confirmed: payload/lifecycle/evidence/staleness rejections in the aggregate, dual prior+target project ownership in `ParticipantAuthorizationStage.CanCorrectAssociation`, and `AssociationCorrectionProjectionUnavailable` denial when readiness is down.
- AC4 metadata-only event/audit shape confirmed (sanitized rationale, redaction/retention, supersession links; no raw PII).
- AC5 idempotency confirmed in `CoarseIdempotencyComposer.ComposeAssociationCorrectionRecord` (coarse key `tenant + intake + actor + correctionKind`, indefinite window, distinct equivalence hash for conflict detection).
- AC6/AC7 confirmed in `AssociationProjectionTranslator` (corrected project, superseded links, rationale, downstream-impact preview, `Corrected` lifecycle) with M0 scorer behavior unchanged.

Findings (documentation/transparency only — auto-fixed in this story record, no code change required):

- MEDIUM (transparency) — Two Story 2.7 correction E2E tests added to `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` (metadata-only forward + projection-dependency fail-closed) were uncommitted and absent from the File List. Added to the File List above; both tests pass.
- MEDIUM (File List accuracy) — File List referenced `NoOpAssociationCorrectionDependencyReadiness.cs`, which no longer exists in the working tree (superseded by `Default`/`Static` readiness implementations when Story 2.8 propagation landed). Annotated in the File List above.
- LOW (process) — Story entered this review already at Status `done` (not `review`); this is a confirmatory re-review, so status is unchanged.

### Change Log

- 2026-05-31: Implemented Story 2.7 correction and supersession workflow; moved story to review.
- 2026-05-31: Senior developer review auto-fixed correction authorization/evidence/projection-readiness issues; moved story to done.
- 2026-06-10: Story-automator adversarial re-review — build clean, full xUnit v3 suite green, all 7 ACs verified; reconciled File List (added uncommitted correction E2E test, annotated superseded `NoOp` readiness seam). No code changes; status remains done.
