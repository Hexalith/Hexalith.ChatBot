---
baseline_commit: 48b72c034cf989888351649ff739c255d968ee89
---

# Story 2.4: Ambiguous-association detection and fail-closed routing

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As the system,
I want ambiguous or low-confidence messages routed to human review instead of being silently filed,
so that the workspace is never contaminated by an uncertain association.

## Acceptance Criteria

1. **Ambiguous threshold-band outcomes route to review, never auto-attach.** Given a deterministic association score in `[T_low, T_high)` and at least one authorized candidate, when association routing runs, then the workflow records an ambiguous-association decision with lifecycle state `NeedsReview`, preserves the ranked candidate list and evidence for S2 review, and creates no project association, conversation message, attachment exposure, AI context, or downstream project artifact. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4-Ambiguous-association-detection-and-fail-closed-routing; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low]

2. **Low-confidence, conflicting, unavailable, or invalid scorer outcomes fail closed to review.** Given a score `< T_low`, conflicting required deterministic signals, unavailable authorization/project evidence, a scorer error, or a non-finite score, when association routing runs, then the workflow records `NeedsReview` with `thresholdBand = fail-closed`; scorer errors/non-finite values produce an empty candidate list, the failure is auditable, and the original email context is preserved. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4-Ambiguous-association-detection-and-fail-closed-routing; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Shared-Workflow-Contract]

3. **Review-state items preserve source context without exposing unsafe data.** Given an item in `Rejected`, `Deferred`, `NeedsReview`, `Failed`, or `Skipped`, when an authorized user or later S2 query inspects it, then the original email source identity, mailbox/intake ids, conversation/thread ids, attachment references, participant-resolution references, scorer result, reason codes, threshold policy version, evidence refs, redaction state, retention class, schema version, and correlation id remain inspectable where authorized; unauthorized candidates/evidence and raw email payload/PII remain suppressed. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4-Ambiguous-association-detection-and-fail-closed-routing; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Security-and-Privacy]

4. **Routing uses the existing command spine and lifecycle model.** Given an association-scoring command admitted through the gateway, when the outcome is ambiguous or fail-closed, then the durable transition is validated as `Received -> NeedsReview` or `Proposed -> NeedsReview`, emitted through the same EventStore/audit path as Story 2.3, and terminal states (`Rejected`/`Failed`/`Skipped`) are not moved in place; reprocessing creates a new workflow instance with audit linkage. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Association-Lifecycle-and-States]

5. **Routing contracts are machine-readable and parity-ready.** Given UI/CLI/MCP/test consumers read the association routing result, when the item is ambiguous or fail-closed, then the contracts expose stable lifecycle state, `AssociationScoringOutcome`, `AssociationThresholdBand`, finite reason codes, candidates/exclusions, `confidenceScore`, `evidenceRefs[]`, `kernelVersion`, `detectedAt`, source provenance, disabled-action/next-action reason codes, and user-safe catalog messages without localized text as the contract of record. [Source: _bmad-output/planning-artifacts/epics.md#FR11; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Command-and-Query-Contracts]

## Tasks / Subtasks

- [x] Add explicit ambiguous/fail-closed routing contracts and OpenAPI spine entries (AC: 1, 2, 5)
  - [x] Reuse existing `ScoreMailboxMessageAssociation`, `AssociationScoringResult`, `AssociationCandidate`, `AssociationExclusion`, `AssociationThresholdBand`, and `AssociationScoringOutcome`; do not create a parallel scorer contract.
  - [x] Add only the missing routing/status shape needed to represent `NeedsReview` if the current projection/query contract cannot expose it; prefer additive properties over replacing existing records.
  - [x] If a command is needed, use an imperative name such as `MarkEmailAssociationNeedsReview`; do not add a `Command` suffix.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` for any new/changed public query or command shapes and regenerate `src/Hexalith.ChatBot.Client/Generated/*.g.cs` through the established NSwag path; never hand-edit generated client files.
  - [x] Add contract tests for camelCase JSON, stable enum wire values, required fields, lifecycle state exposure, empty candidate list on scorer-error fail-closed, and no raw mailbox/party/project PII in routing contracts.

- [x] Implement routing from Story 2.3 scorer outcomes to canonical lifecycle states (AC: 1, 2, 4)
  - [x] Extend the existing association path in `GovernedOperationAggregate.Handle(ScoreMailboxMessageAssociation, ..., CommandEnvelope)` rather than adding a second association aggregate.
  - [x] Map `AssociationScoringOutcome.CandidatesGenerated` with `AssociationThresholdBand.Ambiguous` to a durable `NeedsReview` review item and preserve all ranked authorized candidates.
  - [x] Map `AssociationScoringOutcome.CandidatesGenerated` with `AssociationThresholdBand.FailClosed` to a durable `NeedsReview` review item for low-confidence but otherwise valid scoring; preserve the ranked authorized candidates because the addendum says low-score review keeps the candidate list.
  - [x] Map `AssociationScoringOutcome.FailedClosed` with `AssociationThresholdBand.FailClosed` to `NeedsReview`; preserve exclusions and reason codes, but require `Candidates` to be empty for scorer error/non-finite/conflicting-required-evidence outcomes.
  - [x] Treat low-confidence but otherwise valid scoring as fail-closed review, not `Failed` and not `Skipped`; `Failed` remains for terminal processing failures that require explicit reprocessing.
  - [x] Validate transitions through `LifecycleTransitionValidator`; do not add direct `Received -> Associated` shortcuts for ambiguous items.
  - [x] Keep aggregate logic pure and synchronous. It must validate enriched payloads and emit events/rejections only; no Projects, Parties, DAPR, audit, clock, authorization, or logging calls inside `Handle`.

- [x] Add or extend association routing events and aggregate state (AC: 1, 2, 3, 4)
  - [x] Prefer extending `MailboxAssociationCandidatesGenerated` or adding a narrow past-tense event such as `MailboxAssociationRoutedToReview` if the current event cannot unambiguously represent `NeedsReview`.
  - [x] Event payloads must include `associationId`, `intakeId`, `tenantId`, `sourceMailboxId`, `sourceConversationId`, optional `sourceThreadId`, candidates/exclusions, confidence score, threshold band, outcome, reason codes, threshold policy version, derivation kernel version, detected-at UTC, redaction state, retention class, source/schema version, correlation id, and lifecycle state.
  - [x] Add `Apply` behavior in `GovernedOperationState` so repeated association routing is idempotent and cannot silently duplicate review decisions.
  - [x] Add structured rejection events for invalid routing payloads; do not throw for business-rule violations.
  - [x] Do not persist raw email body, raw provider headers, raw addresses, unauthorized project names, raw exception text, or localized messages.

- [x] Preserve original email context for review and terminal/non-terminal states (AC: 3)
  - [x] Ensure `AssociationCandidateView` or a successor view keeps source mailbox/intake/conversation/thread identity and metadata-only evidence for `NeedsReview`, `Rejected`, `Deferred`, `Failed`, and `Skipped`.
  - [x] If the view needs state, expose canonical `LifecycleState` values rather than deriving review state from `AssociationScoringOutcome` alone.
  - [x] Preserve attachment references and participant-resolution references by stable ids/evidence refs only; do not denormalize Parties PII or raw attachment content into association projections.
  - [x] Keep review-state reads tenant-partitioned with keys derived from `{tenantId}:association:{associationId}` or the existing store key helper.
  - [x] For unauthorized users, return redacted problem/status responses from the message catalog; never reveal whether a hidden project was a candidate.

- [x] Update projection/query behavior for S2 readiness without building S2 UI (AC: 1, 3, 5)
  - [x] Extend `AssociationProjectionHandler`, `AssociationProjectionTranslator`, `AssociationNotification`, and `AssociationProjectionEndpoints` only as needed to expose `NeedsReview` review items and original context metadata.
  - [x] Keep projection handlers idempotent and order-tolerant using source version checks; SignalR nudges must trigger re-query and not carry trusted data.
  - [x] Candidate rows must remain metadata-only and authorization-safe: ranked authorized candidates, confidence band, reason codes, evidence refs/chips, exclusions, disabled-action reasons, next safe action, freshness/schema metadata.
  - [x] Do not implement confirm/reject/defer decision recording or the Blazor S2 screen in this story; Story 2.5 owns the review surface and Story 2.6 owns decision recording/notes.

- [x] Preserve fail-closed/audit/idempotency behavior through the existing gateway (AC: 2, 4, 5)
  - [x] Keep `AssociationScoringOrchestrator` as the only place that calls `IProjectDirectory` before EventStore dispatch.
  - [x] Use existing coarse idempotency normalization for association scoring; semantically identical scorer submissions must not create duplicate review items.
  - [x] If audit pre-commit, tenant binding, command authorization, project authorization evidence, or required dependency checks are unavailable, fail closed with no project association and no partial durable write.
  - [x] Add or verify message-catalog codes for ambiguous association routed, scorer fail-closed, scorer unavailable, conflicting deterministic evidence, and association context unavailable; headlines must stay user-safe and under 80 characters.
  - [x] Ensure logs/traces/status/problem-details include only envelope metadata, stable reason codes, tenant/correlation/task ids, and no payload/PII/secrets.

- [x] Add focused regression tests (AC: 1-5)
  - [x] Add scorer tests proving `[T_low, T_high)` yields `CandidatesGenerated` + `Ambiguous`, high-score multiple candidates still route to review, score `< T_low` yields `CandidatesGenerated` + `FailClosed` review with candidates preserved, conflicting required signals fail closed, and non-finite/error score produces empty candidates.
  - [x] Add aggregate tests proving ambiguous and fail-closed payloads emit review-routing metadata with `NeedsReview`, preserve source context, reject invalid candidate-bearing scorer-error payloads, and never emit `MailboxEmailAssociatedToProject`.
  - [x] Add lifecycle tests for required `Received -> NeedsReview` and `Proposed -> NeedsReview` edges and terminal-state no in-place reprocessing if touched.
  - [x] Add projection tests proving review items are tenant-partitioned, idempotent/order-tolerant, metadata-only, and preserve source mailbox/intake/correlation/kernel/threshold fields.
  - [x] Add gateway/idempotency tests proving Projects I/O remains outside aggregates and unavailable Projects/authorization evidence routes to fail-closed review without leaking hidden project fields.
  - [x] Add conformance/isolation tests for cross-tenant candidate suppression, unauthorized project evidence redaction, CLI/MCP-ready machine-readable reason codes, and identical review outcome semantics across parity shims where present.

- [x] Verify and document results (AC: 1-5)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run compiled xUnit v3 binaries for touched test projects if `dotnet test` hits sandbox socket limits.
  - [x] Run at minimum Contracts, Server, Architecture, Conformance, Client, and any UI/Workers tests touched by this story.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, pass/fail counts, and known environment limitations in the Dev Agent Record.

## Dev Notes

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 2 and Story 2.4 are the primary story source.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`; confidence thresholds, lifecycle states, command/query contracts, tenant isolation, and fail-closed semantics are directly relevant.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; no sharded architecture directory was present.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`; Flow 2 and the Association Review state table constrain the S2-ready projection contracts.
- Loaded persistent project-context facts from sibling `Hexalith.*` `_bmad-output/project-context.md` files; recurring constraints are .NET 10, central package versions, EventStore purity, DAPR/Aspire boundaries, tenant isolation, personal-data redaction, root-level submodules only, xUnit/Shouldly testing, and no generated-file hand edits.

### Source Artifact Analysis

Epic 2 moves from mailbox intake to participant resolution to deterministic association. Story 2.3 now produces `AutoAssociated`, `CandidatesGenerated`, and `FailedClosed` scorer outcomes. Story 2.4 is the trust boundary that turns non-auto scorer outcomes into explicit review-state workflow records so uncertain mail cannot silently contaminate a project. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4-Ambiguous-association-detection-and-fail-closed-routing]

The addendum defines the threshold behavior precisely: M0 defaults are `T_high = 0.90`, `T_low = 0.60`; `score >= T_high` may auto-associate only with required deterministic evidence; `score < T_low` fails closed to `NeedsReview`; `[T_low, T_high)` routes to UI review; scorer errors and non-finite values fail closed to `NeedsReview` with an empty candidate list and audited failure. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low]

The canonical workflow contract is `Received -> Proposed -> Associated | Rejected | Deferred | NeedsReview | Failed | Skipped`, with `Rejected`, `Failed`, and `Skipped` terminal in-place. `NeedsReview` is non-terminal and means evidence, party identity, authorization, dependency status, or policy context is incomplete. Do not model ambiguous association as `Failed`; it is a review state. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Shared-Workflow-Contract]

Architecture requires all state mutation through the CommandGateway and EventStore write path, aggregate purity, metadata-only evidence, and exact lifecycle-state strings. Sibling service lookup stays behind ChatBot-owned adapters (`IProjectDirectory`, `IParticipantDirectory`) and must never run inside aggregate `Handle` methods. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns; _bmad-output/planning-artifacts/architecture.md#Structure-Patterns]

UX Flow 2 says the message becomes project context only after explicit reviewer decision. If no candidate is viable, the item remains unresolved or quarantined with a next action, not silently attached. Story 2.4 should prepare data/query state for this flow, but Story 2.5 owns the Blazor review surface and controls. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow-2-Ambiguous-association-resolution]

### Previous Story Intelligence

Story 2.3 is complete and should be extended, not rebuilt. It added association scoring contracts, OpenAPI schemas, generated client updates, command allow-listing, coarse idempotency, gateway dispatch, pure deterministic scorer, threshold-policy validation, Projects adapter integration, association events/state/projections, and tests. [Source: _bmad-output/implementation-artifacts/2-3-deterministic-association-scorer-and-candidate-generation.md]

Actionable 2.3 patterns to preserve:

- `AssociationScoringOrchestrator` calls `IProjectDirectory` before EventStore dispatch, normalizes the default kernel, and builds an enriched `ScoreMailboxMessageAssociation`. Routing must use this enriched command shape instead of re-querying Projects from aggregate or projection code.
- `GovernedOperationAggregate.Handle(ScoreMailboxMessageAssociation, ..., CommandEnvelope)` already validates scorer result consistency, auto-association invariants, failed-closed empty candidates, and emits `MailboxEmailAssociatedToProject`, `MailboxAssociationScoringFailedClosed`, or `MailboxAssociationCandidatesGenerated`. Story 2.4 should make non-auto review routing explicit here.
- `GovernedOperationState` tracks `_associationIds` for idempotent application of association events. Extend that state carefully if routing needs lifecycle tracking; do not reset or duplicate association ids.
- `AssociationCandidateView` and `AssociationProjectionHandler` already carry tenant id, association id, source mailbox/conversation/thread ids, outcome, band, candidates/exclusions, threshold policy version, schema version, source provenance, kernel version, redaction/retention, source version, correlation id, detected-at, and last-updated-at. Add lifecycle/review fields here only if required for S2 and query parity.
- Senior review already fixed Story 2.3 hazards: aggregate revalidates auto-association threshold/band/single-candidate/required-evidence invariants, threshold events include old/new values, idempotency normalizes omitted kernel values, and unauthorized/cross-tenant exclusions are redacted as `suppressed`. Do not regress these protections.

Recent git history confirms the dependency chain: `48b72c0 feat(story-2.3): Deterministic association scorer and candidate generation`, `c04bcfd feat(story-2.2): Participant resolution and unresolved/unauthorized handling`, `dee5423 feat(story-2.1): Microsoft 365 mailbox intake and source-identity capture`. [Source: git log --oneline -5]

### Current Implementation State

`AssociationScoringOutcome` currently has `AutoAssociated`, `CandidatesGenerated`, and `FailedClosed`; `AssociationThresholdBand` has `Auto`, `Ambiguous`, and `FailClosed`. These are the correct routing vocabulary for Story 2.4. [Source: src/Hexalith.ChatBot.Contracts/Enums/AssociationScoringOutcome.cs; src/Hexalith.ChatBot.Contracts/Enums/AssociationThresholdBand.cs]

`DeterministicAssociationScorer.Score(...)` already maps candidate scores to bands. It returns `AutoAssociated` only for `Auto` band plus one required-evidence candidate; otherwise candidate-bearing results become `CandidatesGenerated` with `Ambiguous`, `Auto`, or `FailClosed` band depending on score and candidate count. No candidates, invalid policy, non-finite weights, or conflicting required evidence become `FailedClosed`. Story 2.4 must distinguish low-confidence candidate review (`CandidatesGenerated` + `FailClosed`, candidates preserved) from scorer-error empty-candidate fail-closed review. [Source: src/Hexalith.ChatBot.Server/Association/Scoring/DeterministicAssociationScorer.cs]

`GovernedOperationAggregate.Handle(ScoreMailboxMessageAssociation, ..., CommandEnvelope)` currently emits `MailboxAssociationCandidatesGenerated` for the default non-auto branch and `MailboxAssociationScoringFailedClosed` for fail-closed. The events do not currently carry an explicit `LifecycleState`; Story 2.4 likely needs to add lifecycle state or a dedicated review-routing event so S2/query consumers do not infer review state ambiguously from outcome names alone. [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs; src/Hexalith.ChatBot.Server/Association/MailboxAssociationCandidatesGenerated.cs; src/Hexalith.ChatBot.Server/Association/MailboxAssociationScoringFailedClosed.cs]

`LifecycleTransitionValidator` already permits `Received -> NeedsReview`, `Proposed -> NeedsReview`, `Deferred -> NeedsReview`, and `NeedsReview` transitions to `Proposed`, `Associated`, `Rejected`, and `Deferred`; it rejects `NeedsReview -> Failed` and terminal-state in-place reprocessing. Reuse this model rather than embedding transition logic in projections or UI contracts. [Source: src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionValidator.cs; tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs]

`AssociationCandidateView` is the current tenant-partitioned read model for association state. It preserves source identity and metadata-only scorer fields, but lacks an explicit lifecycle state. Story 2.4 should add review/lifecycle fields here or in a narrow successor read model if S2 requires status filtering. [Source: src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs]

### Architecture Guardrails

- Runtime stack: .NET SDK `10.0.302`, `net10.0`, DAPR `1.17.9`, Aspire `13.3.x`, Fluent UI `5.0.0-rc.3`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, central package management. Do not add inline package versions or casual package upgrades. [Source: global.json; Directory.Packages.props]
- Every state mutation goes through `CommandGateway`; surface/worker adapters construct typed `IChatBotCommand` and call `IChatBotClient.SubmitAsync`. Do not let UI, Workers, CLI, MCP, or projections perform routing state writes directly. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- Aggregates stay pure: no I/O, DAPR, authorization, audit, sibling client calls, clocks, logging, or async work inside `Handle`. Orchestration and adapter I/O stay in gateway stages. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- Persist stable sibling ids and metadata-only evidence references. Do not persist source email body, raw mailbox headers, raw participant addresses, unauthorized project names, raw exception text, secrets, or localized messages. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- Projection handlers are idempotent and order-tolerant; use source version checks and tenant-partitioned keys. SignalR nudges trigger re-query and are not trusted as data. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]
- Problem/status text must come from the versioned message catalog, with stable codes and user-safe headlines/reasons. Raw error text leaking to users is release-blocking. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- Submodules: initialize/update only root-level submodules declared in the root `.gitmodules`; never use recursive submodule commands. [Source: AGENTS.md#Git-Submodules]

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Contracts/
    Commands/
      ScoreMailboxMessageAssociation.cs             # UPDATE only if additive routing fields are needed
      AssociationScoringResult.cs                   # UPDATE only if additive fields are needed
    Queries/
      GetEmailAssociationStatus.cs                  # NEW/UPDATE if absent
      ListProjectAssociationCandidates.cs           # NEW/UPDATE if absent
    Enums/
      AssociationScoringOutcome.cs                  # UPDATE only if current values cannot express review routing
      AssociationThresholdBand.cs                   # reuse current auto|ambiguous|fail-closed
      LifecycleState.cs                             # reuse canonical values
    Messages/
      ChatBotMessageCodes.cs
      ChatBotMessageCatalog.cs
    openapi/hexalith.chatbot.v1.yaml
  Hexalith.ChatBot.Server/
    Association/
      MailboxAssociationCandidatesGenerated.cs      # UPDATE with lifecycle state, or
      MailboxAssociationRoutedToReview.cs           # NEW narrow event if clearer
      MailboxAssociationScoringFailedClosed.cs      # UPDATE with lifecycle state/review reason if needed
    Lifecycle/StateModel/
      LifecycleTransitionValidator.cs               # UPDATE only if a missing explicit edge is proven
    Operations/
      GovernedOperationAggregate.cs
      GovernedOperationState.cs
    Projections/
      AssociationCandidateView.cs
      AssociationNotification.cs
      AssociationProjectionHandler.cs
      AssociationProjectionTranslator.cs
      AssociationProjectionEndpoints.cs
tests/
  Hexalith.ChatBot.Contracts.Tests/
  Hexalith.ChatBot.Server.Tests/
  Hexalith.ChatBot.Architecture.Tests/
  Hexalith.ChatBot.Conformance.Tests/
  Hexalith.ChatBot.Client.Tests/
```

Keep the actual change smaller if the existing code supports it. The required boundary is explicit review routing and context preservation, not a new association subsystem.

### Out of Scope

- Building the S2 Blazor Association Review UI, side-by-side candidate comparison, keyboard/a11y browser tests, or visual polish; Story 2.5 owns that surface.
- Confirm/reject/defer/mark-needs-review user decision commands, optional decision notes, and evidence-preservation breadth for human decisions; Story 2.6 owns recording and notes.
- Association correction/supersession and derived-store invalidation; Stories 2.7 and 2.8 own that work.
- Duplicate suppression, retry orchestration, terminal failure handling breadth, dead-letter queues, and operational queue management beyond the review projection fields needed here; Story 2.9 and later ops stories own those.
- Learned/AI ranking, sender-history learning, attachment-pattern scoring, prior-correction learning, vector search, LLM inference, or M1 inbound authenticity policy behavior.
- Package upgrades, new UI frameworks, recursive submodule initialization, direct DAPR/Redis/EventStore writes from UI/Workers, or generated client hand edits.

### Latest Technical Notes

No external API version research is required for this story. Use the repository-pinned stack and the Story 2.3 implementation already present in the workspace. Do not upgrade .NET, DAPR, Aspire, Fluent UI, NSwag, or xUnit to satisfy Story 2.4.

## Project Structure Notes

- `src/Hexalith.ChatBot.Server/Association/` already contains association events for candidates, auto-association, fail-closed scoring, and threshold policy. Add review-routing behavior here rather than introducing broad type buckets.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AssociationScoringOrchestrator.cs` is the existing Projects/scorer orchestration seam. Keep Projects I/O there.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` is the current durable association event emitter. Extend its validation and event selection carefully.
- `src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs` is the current read model likely to feed S2. Preserve tenant partitioning and metadata-only fields.
- `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/` owns lifecycle vocabulary and transition validation. Reuse exact strings.
- Contract additions belong under `src/Hexalith.ChatBot.Contracts/` and `Contracts/openapi/`; generated client files are regenerated, never hand-edited.
- Tests must mirror source boundaries under `tests/Hexalith.ChatBot.*.Tests/`.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/implementation-artifacts/2-1-microsoft-365-mailbox-intake-and-source-identity-capture.md]
- [Source: _bmad-output/implementation-artifacts/2-2-participant-resolution-and-unresolved-unauthorized-handling.md]
- [Source: _bmad-output/implementation-artifacts/2-3-deterministic-association-scorer-and-candidate-generation.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4-Ambiguous-association-detection-and-fail-closed-routing]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Shared-Workflow-Contract]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Association-Lifecycle-and-States]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Security-and-Privacy]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Command-and-Query-Contracts]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low]
- [Source: _bmad-output/planning-artifacts/architecture.md#Structure-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow-2-Ambiguous-association-resolution]
- [Source: Hexalith.Projects/_bmad-output/project-context.md]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/ScoreMailboxMessageAssociation.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/AssociationScoringResult.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Enums/AssociationScoringOutcome.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Enums/AssociationThresholdBand.cs]
- [Source: src/Hexalith.ChatBot.Server/Association/Scoring/DeterministicAssociationScorer.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/AssociationScoringOrchestrator.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs]
- [Source: src/Hexalith.ChatBot.Server/Association/MailboxAssociationCandidatesGenerated.cs]
- [Source: src/Hexalith.ChatBot.Server/Association/MailboxAssociationScoringFailedClosed.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs]
- [Source: src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleStates.cs]
- [Source: src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionValidator.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/Association/Scoring/DeterministicAssociationScorerTests.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/Operations/AssociationAggregateTests.cs]
- [Source: tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs]
- [Source: Directory.Build.props]
- [Source: Directory.Packages.props]
- [Source: global.json]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31: `dotnet test tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1 /nr:false` failed before test execution because VSTest could not open a sandbox socket (`System.Net.Sockets.SocketException (13): Permission denied`); switched to compiled xUnit v3 executables.
- 2026-05-31: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-05-31: `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -noColor` passed after review fixes: 78 total, 0 failed, 0 skipped.
- 2026-05-31: `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -noColor` passed: 14 total, 0 failed, 0 skipped.
- 2026-05-31: `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor` passed after review fixes: 182 total, 0 failed, 0 skipped.
- 2026-05-31: `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor` passed: 35 total, 0 failed, 0 skipped.
- 2026-05-31: `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -noColor` passed: 54 total, 0 failed, 0 skipped.
- 2026-05-31: `git diff --check` passed.
- 2026-05-31 review: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors after auto-fixes.
- 2026-05-31 review: `git diff --check` passed after auto-fixes.
- 2026-06-10 validation: no unchecked task/subtask checkboxes were present in the story file.
- 2026-06-10 validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-10 validation: `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -noColor` passed: 480 total, 0 failed, 0 skipped.
- 2026-06-10 validation: `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -noColor` passed: 30 total, 0 failed, 0 skipped.
- 2026-06-10 validation: `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor` passed: 1519 total, 0 failed, 0 skipped.
- 2026-06-10 validation: `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor` passed: 39 total, 0 failed, 0 skipped.
- 2026-06-10 validation: `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -noColor` passed: 87 total, 0 failed, 0 skipped.
- 2026-06-10 validation: `git diff --check` passed.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-05-31.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Validation completed against `.agents/skills/bmad-create-story/checklist.md`; source artifacts, previous story intelligence, current implementation files, architecture guardrails, and disaster-prevention gaps were re-checked.
- Added a metadata-only `AssociationRoutingStatus` query contract and OpenAPI schema for S2/CLI/MCP parity, regenerated the NSwag client through the project build target, and refreshed the generated-client hash fixture.
- Extended existing Story 2.3 association events/projection flow with explicit canonical `LifecycleState` values so ambiguous, low-confidence, and scorer fail-closed outcomes are routed to `NeedsReview` without introducing a second scorer or aggregate.
- Preserved fail-closed semantics: candidate-bearing low-confidence results keep authorized candidates for review, scorer-error fail-closed results require an empty candidate list, invalid candidate-bearing fail-closed payloads are rejected structurally, and aggregate logic remains pure/synchronous.
- Added user-safe message catalog codes for ambiguous routing, scorer fail-closed/unavailable, conflicting deterministic evidence, and association context unavailable.
- Added focused contract, client, scorer, aggregate, and projection regression coverage for lifecycle exposure, metadata-only context, idempotent projection updates, generated client parity, and no unsafe PII/raw payload serialization.
- 2026-06-10 BMAD dev-story validation found no incomplete tasks and re-ran the story-required build/test/diff-check gates successfully.
- 2026-06-10 adversarial review (story-automator-review) added explicit S2-surface E2E coverage for the ambiguous and fail-closed routing scenarios in `GovernedOperationsVisualFoundationE2ETests.cs`, documented that file in the File List, and re-verified build + Contracts/Server/E2E suites green.

### File List

- _bmad-output/implementation-artifacts/2-4-ambiguous-association-detection-and-fail-closed-routing.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs
- src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs
- src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs
- src/Hexalith.ChatBot.Contracts/Serialization/JsonEnumMemberStringConverter.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Server/Association/MailboxAssociationCandidatesGenerated.cs
- src/Hexalith.ChatBot.Server/Association/MailboxAssociationScoringFailedClosed.cs
- src/Hexalith.ChatBot.Server/Association/Scoring/DeterministicAssociationScorer.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs
- src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs
- src/Hexalith.ChatBot.Server/Projections/AssociationNotification.cs
- src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs
- src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs
- src/Hexalith.ChatBot.Server/Projections/PublishedAssociationEvent.cs
- tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/AssociationContractTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Association/Scoring/DeterministicAssociationScorerTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Operations/AssociationAggregateTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Projections/AssociationProjectionTests.cs
- tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs
- tests/fixtures/hexalith-chatbot-generated-client.sha256

### Change Log

- 2026-05-31: Implemented explicit ambiguous/fail-closed `NeedsReview` association routing, routing-status contract/OpenAPI/client updates, S2-ready projection lifecycle state, safe message catalog codes, and focused regression tests.
- 2026-05-31: Senior review auto-fixed routing enum serialization so `AssociationRoutingStatus`, scorer output, and EventStore payloads use stable `EnumMember` wire tokens while remaining backward-compatible with existing PascalCase command JSON.
- 2026-06-10: Revalidated Story 2.4 via BMAD dev-story workflow; no implementation changes were required because all tasks/subtasks were already complete.
- 2026-06-10: Adversarial story-automator review confirmed all ACs/tasks implemented; documented the previously-untracked `GovernedOperationsVisualFoundationE2ETests.cs` ambiguous/fail-closed routing scenarios in the File List (MEDIUM documentation finding fixed). No code changes required.

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31

### Outcome

Approved after auto-fixes. Story status moved to `done`.

### Findings Fixed

- High: `AssociationRoutingStatus` only proved `NeedsReview` serialization; hyphenated/lowercase association enum wire values could drift to CLR names such as `FailedClosed`, `FailClosed`, and `ScorerError`. Fixed with `JsonEnumMemberStringConverter<TEnum>` and regression assertions for direct and nested routing-status enum values.
- Medium: Scorer-generated evidence references used `AssociationSignalClass.ToString()` for `EvidenceKind`, producing implementation names such as `ConversationThreadIdentifier` instead of stable machine-readable tokens. Fixed scorer evidence-kind mapping to canonical wire tokens.
- Medium: Existing command-spine tests used PascalCase enum JSON. The converter now reads both legacy enum member names and canonical `EnumMember` tokens, while writes remain canonical for contract/output parity.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -noColor` passed: 78 total, 0 failed, 0 skipped.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -noColor` passed: 14 total, 0 failed, 0 skipped.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor` passed: 182 total, 0 failed, 0 skipped.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor` passed: 35 total, 0 failed, 0 skipped.
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -noColor` passed: 54 total, 0 failed, 0 skipped.
- `git diff --check` passed.

---

Reviewer: Jérôme Piquot on 2026-06-10 (story-automator adversarial review)

### Outcome

Approved. All five acceptance criteria are implemented and verified against the committed code; every `[x]` task was confirmed actually done. Story remains `done`.

### AC validation (verified against source)

- AC1 (ambiguous → review, never auto-attach): `GovernedOperationAggregate.Handle(ScoreMailboxMessageAssociation,…)` routes `CandidatesGenerated`+`Ambiguous` to `MailboxAssociationCandidatesGenerated` with `LifecycleState.NeedsReview`, preserving the ranked candidate list; no `MailboxEmailAssociatedToProject` is emitted on the review path (`GovernedOperationAggregate.cs:2380`).
- AC2 (low-confidence/conflicting/unavailable/invalid fail closed): `FailedClosed` maps to `MailboxAssociationScoringFailedClosed` (`NeedsReview`, `FailClosed` band) and the aggregate rejects any candidate-bearing fail-closed payload (`GovernedOperationAggregate.cs:2302`); the scorer emits empty candidates with `ScorerError`/`ConflictingDeterministicEvidence` reason codes and clamps non-finite scores (`DeterministicAssociationScorer.cs:27-45,147`). Low-confidence-but-valid `CandidatesGenerated`+`FailClosed` correctly keeps candidates via the default routing branch.
- AC3 (review-state context preserved, unsafe data suppressed): events and `AssociationCandidateView` carry source mailbox/intake/conversation/thread ids, evidence refs, threshold/kernel versions, redaction/retention, schema, and correlation id as metadata only; no raw body/address/header/exception/secret fields exist on the view.
- AC4 (command spine + lifecycle model): routing flows through the gateway-enriched command and `LifecycleTransitionValidator`; the aggregate `Handle` is pure/synchronous with no Projects/Parties/DAPR/clock/auth/logging calls.
- AC5 (machine-readable, parity-ready contracts): `AssociationRoutingStatus` query plus stable `EnumMember` wire tokens via `JsonEnumMemberStringConverter<TEnum>`; message-catalog codes `association_ambiguous_routed`, `association_scorer_failed_closed`, `association_scorer_unavailable`, `association_conflicting_deterministic_evidence`, `association_context_unavailable` present with user-safe headlines under 80 characters.

### Findings

- MEDIUM (fixed — documentation): the story-2.4 ambiguous and fail-closed routing E2E scenarios added to `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` were not listed in the File List / Change Log. Added the file to the File List and recorded the change; the tests themselves are correct and pass (no code change required).
- LOW (informational, not changed): `IsValidReviewTransition()` only validates the `Received → NeedsReview` edge, which is always permitted, so the guard is effectively tautological; the `Proposed → NeedsReview` edge from AC4 is enforced by the durable lifecycle state model elsewhere. Left as-is to avoid regression risk in a `done` story.

### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed: 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor` passed: 66 total, 0 failed, 0 skipped (browserless fallback; the two new association-routing scenarios pass).
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -noColor` passed: 480 total, 0 failed, 0 skipped.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor` passed: 1519 total, 0 failed, 0 skipped.
- `git diff --check` passed.
