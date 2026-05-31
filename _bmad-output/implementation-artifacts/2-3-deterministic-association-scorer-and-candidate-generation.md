---
baseline_commit: c04bcfdcb6ec8768dd03e3c78f8d9b6d9555759a
---

# Story 2.3: Deterministic association scorer and candidate generation

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As the system,
I want a deterministic-signals scorer that produces a confidence score and ranked authorized candidates,
so that strong deterministic matches auto-associate and everything else gets evidence-backed candidates.

## Acceptance Criteria

1. **Deterministic scorer emits normalized confidence and ranked authorized candidates.** Given M0 deterministic signals from an ingested/resolved mailbox message, including explicit project identifier, mailbox routing rule, and conversation/thread identifier, when association scoring runs, then ChatBot produces a deterministic `[0.0,1.0]` confidence score and a stable ranked list of project candidates filtered by bound tenant and live authorization; unauthorized projects must not appear in candidates, evidence, logs, status payloads, CLI/MCP-ready contracts, or user-visible error text. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.3-Deterministic-association-scorer-and-candidate-generation; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Technical-Success]

2. **High-confidence single deterministic match auto-associates.** Given a score `>= T_high`, required deterministic evidence present, and exactly one authorized matched project, when association runs, then the message transitions from `Received`/resolved input to `Associated` for that project through the ChatBot command spine and EventStore path; deterministic evidence outranks any AI inference, and M0 must not use learned/AI signals for the decision. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.3-Deterministic-association-scorer-and-candidate-generation; _bmad-output/planning-artifacts/architecture.md#Data-Architecture]

3. **Non-auto outcomes are evidence-backed but not silently filed.** Given no candidate, multiple authorized candidates, missing required deterministic evidence, conflicting deterministic evidence, unavailable authorization evidence, stale project membership, or a non-finite/error score, when association scoring completes, then the item remains unassociated with machine-readable candidates/exclusions and a safe lifecycle outcome that downstream Story 2.4 can route to `NeedsReview`; this story must not silently attach low-confidence or ambiguous mail. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Association-Lifecycle-and-States; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low]

4. **Confidence threshold changes are security-sensitive.** Given a tenant administrator submits `association.t-high` / `association.t-low` policy values, when the update is accepted, then the path requires tenant-admin authorization through the gateway, rejects service-client and AI actors, validates schema-bounded values, audits the change, preserves a policy snapshot/version, and blocks M0 values below `T_high = 0.80` or `T_low = 0.50` unless an explicit evaluation-run reference is supplied. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.3-Deterministic-association-scorer-and-candidate-generation; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low]

5. **Reason codes and confidence inputs are first-class contracts.** Given a candidate ranking, auto-association, threshold update, or fail-closed scorer outcome, when UI/CLI/MCP/audit/tests inspect the result, then the output exposes stable machine-readable reason codes, confidence inputs, `thresholdBand`, `evidenceRefs[]`, `kernelVersion`, `detectedAt`, source mailbox/intake identity, correlation id, and redaction/retention/schema metadata without relying on localized text or raw upstream payloads. [Source: _bmad-output/planning-artifacts/epics.md#Story-2.3-Deterministic-association-scorer-and-candidate-generation; _bmad-output/planning-artifacts/architecture.md#Format-Patterns]

## Tasks / Subtasks

- [x] Add association-scoring contracts and OpenAPI spine entries (AC: 1-5)
  - [x] Add imperative commands such as `ScoreMailboxMessageAssociation` and, if threshold policy is implemented as a command in this story, `SetAssociationConfidenceThresholds`; do not use a `Command` suffix.
  - [x] Add contract records for deterministic signal input, association candidate, evidence reference, exclusion, confidence input, threshold policy snapshot, and scorer result.
  - [x] Add stable enums or closed vocabularies for deterministic signal class, association reason code, association exclusion state, scorer outcome, and `ThresholdBand` usage; reuse existing `ThresholdBand` only if its current values match `auto|ambiguous|fail-closed` semantics without breaking existing UI localization.
  - [x] Add typed ULID identity helpers for association/scoring workflow ids if no existing identifier fits; never parse ChatBot-owned ids with `Guid`.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and regenerate `src/Hexalith.ChatBot.Client/Generated/*.g.cs` through the established NSwag path; do not hand-edit generated files.
  - [x] Add contract tests for JSON camelCase, required fields, bounded score values, deterministic ordering, reason-code stability, OpenAPI required fields, and absence of raw project/participant/mailbox PII in result contracts.

- [x] Add a ChatBot-owned Projects adapter seam for authorized candidate discovery (AC: 1, 3, 5)
  - [x] Add `src/Hexalith.ChatBot.Server/Adapters/Projects/IProjectDirectory.cs` and an implementation over existing Hexalith.Projects client/contracts or server-side query APIs; wrap sibling types behind ChatBot-owned records.
  - [x] Add required `ProjectReference`s to `Hexalith.Projects.Client` and/or `Hexalith.Projects.Contracts` via `$(HexalithProjectsRoot)` in `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj` and matching test project references as needed; no inline package versions.
  - [x] Reuse or map from existing Projects resolution vocabulary where it is already stable (`ProjectResolution`, `ResolutionCandidate`, `ResolutionExclusion`, `ReferenceState`, `ProjectReasonCode`) instead of inventing duplicate semantics; keep ChatBot's product-specific scorer/contracts in ChatBot if Projects' existing engine cannot express mailbox-routing rules.
  - [x] Query/filter only by the bound tenant from `ChatBotGatewayContext`; never trust tenant or project ids supplied in the mailbox payload.
  - [x] Treat project not found, archived, stale/unavailable projection, ambiguous project identifier, cross-tenant project, and authorization denied as explicit safe candidate/exclusion outcomes.
  - [x] Do not persist or surface unauthorized project display names/evidence. Authorized candidate display names are permitted only after live authorization succeeds for the actor/surface.

- [x] Implement the deterministic M0 association scorer kernel (AC: 1-3, 5)
  - [x] Add a pure scorer under `src/Hexalith.ChatBot.Server/Association/Scoring/` that consumes pre-fetched input evidence and returns deterministic results; it must not call DAPR, Projects, Parties, authorization, audit, clocks, or EventStore.
  - [x] Support only M0 signal classes: explicit project identifier, mailbox routing rule, and conversation/thread identifier. Do not add learned, AI, sender-history, attachment-pattern, or prior-correction scoring in M0.
  - [x] Normalize score to `[0.0,1.0]`, reject non-finite values, and derive a stable `thresholdBand` using tenant policy thresholds.
  - [x] Define deterministic ranking: score descending, required-evidence completeness, reason-code precedence, then opaque project id ordinal as final tie-breaker.
  - [x] Require deterministic evidence for auto-association; a high score without the required evidence remains a non-auto result.
  - [x] Add pure unit/property-style tests for score bounds, deterministic ordering, reason-code de-duplication, missing/conflicting signal fail-closed behavior, and zero unauthorized candidate leakage.

- [x] Route association scoring and auto-association through the existing command spine (AC: 1-5)
  - [x] Extend `ChatBotSpineCommandAllowlist` deliberately for the new first-party association/threshold commands and keep unrelated commands rejected.
  - [x] Extend `AcceptedCommandDispatcher` or add a narrow Association orchestrator so Projects adapter I/O and authorization filtering occur outside aggregate `Handle` methods before EventStore persistence.
  - [x] Preserve `ChatBotSurfaceOrigin.Mailbox`/`Worker`, correlation id, task id, intake id, source mailbox id, source conversation/thread id, and participant-resolution references from Stories 2.1 and 2.2.
  - [x] Use the existing pre-commit audit seam for auto-association and threshold changes. If tenant binding, project authorization, threshold policy, scorer, or audit is unavailable, fail closed with no association state write.
  - [x] Add or extend coarse idempotency only for externally replayable association scoring/auto-association. If added, define exact keys from `tenant_id + intake_id + scorer_kernel_version + deterministic_signal_fingerprints` and `tenant_id + policy_key + policy_version` for threshold updates.
  - [x] Keep aggregate `Handle` methods pure and synchronous. They validate payloads and emit events/rejections; they do not call `IProjectDirectory`, `IParticipantDirectory`, DAPR, auth, audit, or clocks.

- [x] Add association events, state, and projections (AC: 1-3, 5)
  - [x] Add past-tense events under `src/Hexalith.ChatBot.Server/Association/`, for example `MailboxAssociationCandidatesGenerated`, `MailboxEmailAssociatedToProject`, `MailboxAssociationScoringFailedClosed`, and threshold-policy events if implemented in ChatBot; rejections must follow `{Target}{Reason}Rejection`.
  - [x] Extend `GovernedOperationAggregate` only as much as needed, or create the smallest Association aggregate seam compatible with EventStore discovery; avoid a broad aggregate refactor.
  - [x] Stamp every event/projection with `tenantId`, source provenance, scorer/kernel version, threshold policy version, redaction state, retention class, source version, schema version, `detectedAt`, and correlation id.
  - [x] Add tenant-partitioned projection state for association candidates and auto-associated message state so Story 2.4/2.5 can render `NeedsReview` candidates without re-querying Projects live for display.
  - [x] Store stable `ProjectId` and metadata-only evidence references. Do not persist source email body, raw mailbox headers, raw participant addresses, unauthorized project names, or localized messages.

- [x] Implement threshold policy defaults and guarded updates (AC: 2, 4, 5)
  - [x] Provide M0 defaults `T_high = 0.90` and `T_low = 0.60` when no tenant policy override exists.
  - [x] Enforce `0.0 <= T_low < T_high <= 1.0` plus M0 floors `T_high >= 0.80` and `T_low >= 0.50` unless an evaluation-run reference is supplied.
  - [x] Restrict threshold mutation to tenant-admin human actors. Service clients, AI actors, unresolved participants, and email-only external parties must receive catalog-backed redacted denials.
  - [x] Record threshold changes as security-sensitive audit facts with actor, tenant, old/new values, policy version, evaluation-run reference when present, correlation id, and command surface.
  - [x] Add message-catalog codes for invalid threshold policy, unauthorized threshold update, scorer fail-closed, and unauthorized association candidate suppression; headlines stay under 80 characters and reason text must not confirm hidden resources.

- [x] Preserve UI/UX contracts without building the S2 review surface yet (AC: 1, 3, 5)
  - [x] If UI contracts or localization are touched, reuse existing `ChatBotEvidenceChip`, `ChatBotBlockedState`, `ChatBotActorBadge`, status/banner, localization, focus, live-region, and disabled-action primitives; do not create a new visual system.
  - [x] Candidate rows must expose project candidate, confidence band, evidence chips, authorization-safe state, and actions only as contracts/projection data needed by later S2 UI work.
  - [x] Disabled or blocked association/threshold actions must expose finite reason codes and reachable explanations; tooltip-only or raw exception text is not acceptable.
  - [x] Keep English/French localization behavior: stable machine codes stay untranslated, display labels and explanations translate, and confidence formatting remains locale-aware.

- [x] Add focused tests and regression evidence (AC: 1-5)
  - [x] Add Contracts tests for association commands/results/OpenAPI, threshold policy shape, reason-code serialization, and machine-readable confidence input fields.
  - [x] Add Server tests for explicit project-id match, mailbox-routing match, thread-id match, multi-signal ranking, missing evidence, conflicting evidence, no authorized candidates, stale/unavailable Projects projection, non-finite scorer result, and high-confidence single authorized auto-association.
  - [x] Add gateway tests proving scorer/Projects I/O occurs before EventStore dispatch but outside aggregates, audit unavailable fails closed, threshold mutation requires tenant-admin human actor, and service-client/AI/unresolved actors are denied safely.
  - [x] Add projection tests proving candidate/auto-association records are tenant-partitioned, idempotent/order-tolerant, metadata-only, and preserve source mailbox/intake/correlation/kernel/threshold fields.
  - [x] Add architecture tests proving no aggregate references `IProjectDirectory`, Projects clients, `IParticipantDirectory`, DAPR, auth, audit, or idempotency stores, and no UI/worker path replicates gateway stages.
  - [x] Add conformance/isolation tests for unauthorized project candidate suppression, cross-tenant project ids, redacted problem details/log payloads, and deterministic replay of scorer results.

- [x] Verify and document results (AC: 1-5)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
  - [x] Run compiled xUnit v3 binaries for touched test projects if `dotnet test` hits sandbox socket limits.
  - [x] Run at minimum Contracts, Server, Architecture, Conformance, and any UI/Workers tests touched by this story.
  - [x] Run `git diff --check`.
  - [x] Record exact commands, pass/fail counts, and known environment limitations in the Dev Agent Record.

## Dev Notes

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 2 and Story 2.3 are the primary story source.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`; association confidence, lifecycle, tenant isolation, data governance, threshold floors, and idempotency are directly relevant.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; no sharded architecture directory was present.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`; candidate row, evidence chip, blocked state, accessibility, localization, and responsive state contracts are relevant.
- Loaded persistent project-context facts from sibling `Hexalith.*` `_bmad-output/project-context.md` files; recurring constraints are .NET 10, central package versions, EventStore purity, DAPR/Aspire boundaries, tenant isolation, personal-data redaction, root-level submodules only, and xUnit/Shouldly testing.

### Source Artifact Analysis

Epic 2 moves from mailbox intake to participant resolution to deterministic project association. Story 2.3 is the first project-association story: it must produce deterministic confidence, ranked authorized candidates, and high-confidence auto-association before later stories route ambiguous outcomes, render S2 review, record all decision notes, and handle corrections. [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Email-Intake-and-Project-Association]

The PRD makes deterministic routing signals authoritative for M0: project-specific mailbox aliases/routing rules, conversation/thread identifiers, and explicit project references are valid deterministic signals; AI may rank or summarize later but must not override fail-closed rules. Association quality targets include `95% precision`, `90% recall`, and zero critical false-positive auto-associations into projects the sender is not authorized to read. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Technical-Success; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Measurable-Outcomes]

The addendum defines M0 scoring inputs and thresholds: explicit project identifier, mailbox-routing-rule match, and conversation/thread-identifier match only; default `T_high = 0.90`, `T_low = 0.60`; non-finite/error scorer output fails closed to `NeedsReview` with an empty candidate list and audited failure. Threshold updates are security-sensitive and cannot be performed by service clients or AI actors. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low]

Architecture places the scorer in `Server/Association/Scoring/`, wraps Projects through `Adapters/Projects/IProjectDirectory`, and requires every proposal/candidate to carry `confidenceScore`, `thresholdBand`, `evidenceRefs[]`, `kernelVersion`, `detectedAt`, and correction outcome when applicable. Surface adapters must still submit typed `IChatBotCommand` through `IChatBotClient`; no adapter may perform scoring, authorization, or audit writes independently. [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture; _bmad-output/planning-artifacts/architecture.md#Structure-Patterns; _bmad-output/planning-artifacts/architecture.md#Process-Patterns]

UX has no visual mockup for this story, but its state contracts are binding if projection/UI contracts are touched. Association Review expects ranked authorized candidates with confidence and evidence; unauthorized candidates are suppressed; blocked states explain denial without confirming restricted resources; action controls expose disabled reasons and remain keyboard/a11y compliant. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]

### Previous Story Intelligence

Story 2.2 is complete and should be extended, not bypassed. It added `ResolveMailboxMessageParticipants`, `ParticipantResolutionId`, `IParticipantDirectory`, `ParticipantResolutionOrchestrator`, participant events/projections, participant authorization blocking, catalog-backed redacted denials, and tests for malformed payloads, ambiguous Parties results, projection traceability, and unresolved-review actions. [Source: _bmad-output/implementation-artifacts/2-2-participant-resolution-and-unresolved-unauthorized-handling.md]

Relevant 2.2 patterns to preserve:

- `AcceptedCommandDispatcher` deserializes camelCase wire payloads with web options, validates source identity, calls the orchestrator, then submits PascalCase payloads to EventStore. Association scoring should follow the same pattern if it needs pre-EventStore I/O.
- `ParticipantResolutionOrchestrator` performs sibling directory I/O outside aggregates and returns a fully enriched command payload. Association scoring should use the same orchestration shape with an `IProjectDirectory`.
- `GovernedOperationAggregate.Handle(ResolveMailboxMessageParticipants, ...)` validates payloads and emits metadata-only events; it does not call adapters, clocks, audit, or DAPR. Association handlers must stay pure.
- Participant projections preserve source mailbox id, intake id, correlation id, kernel version, redaction state, retention class, and schema version. Association projections should carry the same traceability plus threshold policy/version.
- Review fixed four issues in 2.2: unresolved review actions, ambiguous Parties `TotalCount`, malformed payload fail-closed validation, and projection correlation/source mailbox preservation. Add tests that prevent analogous association-scoring regressions.

Recent git history confirms Story 2.2 landed in commit `c04bcfd feat(story-2.2): Participant resolution and unresolved/unauthorized handling` after Story 2.1 `dee5423 feat(story-2.1): Microsoft 365 mailbox intake and source-identity capture`. Build on those committed paths. [Source: git log --oneline -5]

### Current Implementation State

No `IProjectDirectory` exists yet. `Directory.Build.props` already defines `$(HexalithProjectsRoot)`, but `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj` currently references EventStore, Parties, Tenants, Client, and ServiceDefaults only. Story 2.3 likely needs Projects client/contracts project references in Server and affected tests. [Source: Directory.Build.props; src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj]

No ChatBot association scorer, association candidate contracts, threshold policy contracts, or association projection exist yet. Existing association source files are intake and participant-resolution focused. Keep new files under `Server/Association/Scoring/`, `Server/Association/Evidence/`, `Server/Association/` events, `Server/Adapters/Projects/`, and `Server/Projections/` rather than adding broad type buckets. [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries; src/Hexalith.ChatBot.Server/Association/]

Hexalith.Projects already has a pure `ProjectResolutionEngine` and contracts such as `ProjectResolution`, `ResolutionCandidate`, `ResolutionExclusion`, `ReferenceState`, and project resolution trace projections. Treat these as reusable vocabulary or mapping targets where possible, but do not call Projects domain logic directly from a ChatBot aggregate and do not let ChatBot persist Projects-owned source-of-truth records. [Source: Hexalith.Projects/src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs; Hexalith.Projects/src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs; Hexalith.Projects/_bmad-output/project-context.md]

`ThresholdBand` already exists in ChatBot contracts and is used by UI localization tests. Verify its current values before reusing it for association bands; do not break existing localized formatting or unrelated risk/status display. [Source: src/Hexalith.ChatBot.Contracts/Enums/ThresholdBand.cs; tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs]

### Architecture Guardrails

- Runtime stack: .NET SDK `10.0.300`, `net10.0`, DAPR `1.17.9`, Aspire `13.3.x`, Fluent UI `5.0.0-rc.3`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, central package management. Do not add inline package versions or casual package upgrades. [Source: global.json; Directory.Packages.props]
- Every state mutation goes through `CommandGateway`; surface/worker adapters construct typed `IChatBotCommand` and call `IChatBotClient.SubmitAsync`. Do not let workers, UI, Projects adapters, CLI, or MCP replicate scoring authorization, threshold checks, audit, or idempotency. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- Aggregates stay pure: no I/O, DAPR, authorization, audit, sibling client calls, or wall-clock reads inside `Handle`. Sibling lookup and live authorization filtering belong in server orchestration/adapter services before durable events are emitted. [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- Store stable sibling ids (`ProjectId`, `PartyId`) and metadata-only evidence references. Do not store upstream PII, raw exception text, raw provider tokens, raw mailbox body/header values, unauthorized project details, or localized text in events/projections/logs/status/problem details. [Source: _bmad-output/planning-artifacts/architecture.md#Structure-Patterns; Hexalith.Projects/_bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Derived records carry `tenantId`, `sourceProvenance`, `derivationKernelVersion`, `redactionState`, `retentionClass`, and `schemaVersion`; projection handlers are idempotent and order-tolerant. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- Lifecycle strings are exact: `Received`, `Proposed`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Corrected`, plus `Correcting` and `Correction-delayed`. Candidate generation normally maps to `Proposed`; auto-association maps to `Associated`; ambiguous/fail-closed routing is completed by Story 2.4. [Source: _bmad-output/planning-artifacts/architecture.md#Naming-Patterns]
- Submodules: initialize/update only root-level submodules declared in root `.gitmodules`; never use recursive submodule commands. [Source: AGENTS.md#Git-Submodules]

### Suggested Implementation Shape

```text
src/
  Hexalith.ChatBot.Contracts/
    Commands/ScoreMailboxMessageAssociation.cs
    Commands/SetAssociationConfidenceThresholds.cs       # if threshold mutation lives here
    Commands/AssociationDeterministicSignal.cs
    Commands/AssociationCandidate.cs
    Commands/AssociationEvidenceReference.cs
    Commands/AssociationScoringResult.cs
    Enums/AssociationReasonCode.cs
    Enums/AssociationSignalClass.cs
    Enums/AssociationScoringOutcome.cs
    Identities/AssociationWorkflowId.cs
    Messages/ChatBotMessageCodes.cs
    Messages/ChatBotMessageCatalog.cs
    openapi/hexalith.chatbot.v1.yaml
  Hexalith.ChatBot.Server/
    Adapters/Projects/
      IProjectDirectory.cs
      ProjectsProjectDirectory.cs
      ProjectDirectoryResult.cs
    Association/
      Evidence/
      Scoring/
        DeterministicAssociationScorer.cs
        AssociationThresholdPolicy.cs
      MailboxAssociationCandidatesGenerated.cs
      MailboxEmailAssociatedToProject.cs
      MailboxAssociationScoringFailedClosed.cs
    Gateway/Stages/
      IAssociationScoringOrchestrator.cs
      AssociationScoringOrchestrator.cs
    Projections/
      AssociationCandidateView.cs
      AssociationCandidateProjectionHandler.cs
tests/
  Hexalith.ChatBot.Contracts.Tests/
  Hexalith.ChatBot.Server.Tests/
  Hexalith.ChatBot.Architecture.Tests/
  Hexalith.ChatBot.Conformance.Tests/
  Hexalith.ChatBot.UI.Tests/          # only if UI contracts/localization are touched
```

Keep this shape smaller if the existing code supports a narrower change. The essential boundaries are: Contracts remain low-dependency, Server owns Projects adapter/orchestration/scoring/events/projections, UI/Workers stay client-bound, and generated client files are regenerated from OpenAPI.

### Out of Scope

- Full S2 Association Review UI implementation; Story 2.5 owns the review surface.
- Human choose/reject-all/defer/needs-review decision recording, optional reviewer notes, evidence-retention breadth, and all decision history; Story 2.6 owns the full decision-recording scope.
- Association correction, supersession, correction propagation, duplicate/retry/failure-state breadth, and operational queue management beyond projection state needed by this story.
- Learned/AI candidate ranking, sender-history learning, attachment-pattern scoring, prior-correction learning, vector search, or LLM inference. These are not M0 deterministic scorer inputs.
- Granting external participants portal authority, changing Parties resolution semantics, changing Projects source-of-truth ownership, or bypassing live authorization.
- Package upgrades, new UI frameworks, recursive submodule initialization, direct DAPR/Redis/EventStore/Projects writes from workers/UI, or bypassing `IChatBotClient`/`CommandGateway`.

### Latest Technical Notes

No external API version research is required for this story. The implementation should use the repository-pinned stack and sibling Projects/Parties contracts already present in the workspace. Do not upgrade .NET, DAPR, Aspire, Fluent UI, NSwag, or xUnit to satisfy this story.

## Project Structure Notes

- `src/Hexalith.ChatBot.Server/Adapters/Projects/` is the expected home for `IProjectDirectory`; it is absent today.
- `src/Hexalith.ChatBot.Server/Association/Scoring/` is the expected home for the pure deterministic scorer; it is absent today.
- `src/Hexalith.ChatBot.Server/Association/Intake/` and `Association/Participants/` already own source intake and participant-resolution events. Association candidate/auto-association events should live in sibling Association folders, not in UI/Workers.
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
- [Source: _bmad-output/implementation-artifacts/2-2-participant-resolution-and-unresolved-unauthorized-handling.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.3-Deterministic-association-scorer-and-candidate-generation]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Technical-Success]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Association-Lifecycle-and-States]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Security-and-Privacy]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Data-Governance-Surface]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Confidence-Thresholds-T_high-T_low]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Idempotency-Keys-per-operation-class]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Naming-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Structure-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Process-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Project-Structure-and-Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-Patterns]
- [Source: Hexalith.Projects/_bmad-output/project-context.md]
- [Source: Hexalith.Projects/src/Hexalith.Projects/Resolution/ProjectResolutionEngine.cs]
- [Source: Hexalith.Projects/src/Hexalith.Projects.Contracts/Models/ProjectResolution.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/ResolveMailboxMessageParticipants.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantResolutionOrchestrator.cs]
- [Source: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionView.cs]
- [Source: Directory.Build.props]
- [Source: Directory.Packages.props]
- [Source: global.json]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `python3 _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-dev-story --key workflow` -> no activation overrides; persistent project-context facts loaded.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` -> PASS, 0 warnings, 0 errors.
- `dotnet test Hexalith.ChatBot.slnx --no-build --no-restore -m:1 /nr:false` -> FAIL in this sandbox before executing tests; VSTest cannot open its TCP listener (`System.Net.Sockets.SocketException (13): Permission denied`).
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -noColor` -> PASS, 13 total, 0 failed.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -noColor` -> PASS, 76 total, 0 failed.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor` -> PASS, 157 total, 0 failed.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor` -> PASS, 35 total, 0 failed.
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -noColor` -> PASS, 54 total, 0 failed.
- `git diff --check` -> PASS.
- `dotnet build Hexalith.Projects/src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj --no-restore -m:1 /nr:false` -> FAIL, 97 compile errors from generated `Hexalith.Projects.Contracts.Ui.*` files missing `Fluxor` types; this only reproduces under `--no-restore` before `Fluxor.Blazor.Web` is restored.
- `dotnet build Hexalith.Projects/src/Hexalith.Projects.Contracts/Hexalith.Projects.Contracts.csproj -m:1 /nr:false` (with restore) -> PASS, 0 errors; the prior blocker was purely a no-restore artifact, so the Projects `ProjectReference` is now viable.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` (after restore via per-project build) -> PASS, 0 warnings, 0 errors, all projects including IntegrationTests.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor` -> PASS, 167 total (10 new `ProjectsProjectDirectoryTests`), 0 failed.
- `tests/Hexalith.ChatBot.Contracts.Tests/...` -> PASS, 76 total, 0 failed; `Architecture` -> PASS, 35; `Conformance` -> PASS, 54; `Client` -> PASS, 13.
- `git diff --check` (final) -> PASS.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-05-31.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implemented association scoring contracts, OpenAPI schemas, generated client updates, command allow-listing, idempotency keys, gateway dispatch, pure deterministic scorer, association events/state/projections, threshold-policy validation, and focused regression tests.
- Completed the remaining Projects adapter seam: replaced the stub `ProjectsProjectDirectory` with a live anti-corruption adapter over the Hexalith.Projects typed server query API (`IClient`). It authorizes every claimed project id via `GetProjectAsync`, resolves conversation/thread signals via `ResolveProjectFromConversationAsync`, and maps the Projects resolution/lifecycle vocabulary (`ProjectResolution`/`ResolutionCandidate`/`ResolutionExclusion`/`ResolutionExclusionReferenceState`/`ProjectLifecycleState`) onto ChatBot-owned candidate/exclusion records so no generated Projects DTO escapes the adapter boundary.
- The earlier `ProjectReference` blocker was diagnosed as a no-restore artifact only: `Hexalith.Projects.Contracts` builds cleanly with restore, so `Hexalith.Projects.Client` is now referenced from `Hexalith.ChatBot.Server` and `Hexalith.ChatBot.Server.Tests` via `$(HexalithProjectsRoot)`.
- Tenant safety: authority is server-derived by the Projects service; the adapter never trusts payload tenant/project ids and additionally fail-closes any opaque id that encodes a foreign tenant prefix. Not-found, archived, stale/unavailable projection, ambiguous, cross-tenant, and authorization-denied are surfaced as metadata-only exclusions; transport/projection unavailability fails closed (`IsAvailable = false`) so the gateway writes no association state. Mirroring Story 2.2's `PartiesParticipantDirectory`, the fail-closed `UnavailableProjectDirectory` stays the registered M0 DI default (live HTTP/token wiring is an integration concern).
- Added 10 `ProjectsProjectDirectoryTests` covering authorized explicit match, not-found, archived, stale, unauthorized (no name leak), cross-tenant suppression (no Projects call), conversation-resolved candidate, ambiguous conversation exclusion, transport fail-closed, and the no-claim available-but-empty path.
- Fixed a namespace collision the new reference introduced: in `TrivialGovernedCommandAspireE2eTests`, the Aspire-generated global `Projects` metadata class is now disambiguated as `global::Projects.Hexalith_ChatBot_AppHost` (the new `Hexalith.Projects` namespace otherwise shadowed it).
- Definition of Done: PASS. All tasks/subtasks are `[x]`, all acceptance criteria are satisfied, and the touched Contracts/Server/Architecture/Conformance/Client suites are green with no regressions.
- Senior review auto-fixes completed on 2026-05-31: aggregate auto-association now revalidates `confidence >= T_high`, `thresholdBand == Auto`, single required-evidence candidate, result/source/correlation/kernel consistency, and failed-closed empty-candidate shape before emitting durable events.
- Senior review added previous threshold values and previous policy version to `AssociationConfidenceThresholdsChanged`, preserves current threshold policy in aggregate state, normalizes association-scoring idempotency to the default kernel when submissions omit it, and redacts unauthorized/cross-tenant exclusion project/evidence fields as `suppressed`.
- Review validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` PASS; compiled xUnit binaries PASS for Server (173), Contracts (76), Architecture (35), Conformance (54), and Client (13); `git diff --check` PASS. `dotnet test Hexalith.ChatBot.slnx --no-build --no-restore -m:1 /nr:false` still aborts in this sandbox because VSTest cannot open its TCP listener (`SocketException (13): Permission denied`).

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31

Outcome: Approved after automatic fixes. Status moved to `done`; no CRITICAL issues remain.

Findings fixed:

- HIGH: `GovernedOperationAggregate` trusted an enriched `AutoAssociated` result without independently enforcing the policy threshold and auto band. Fixed by validating confidence, band, single authorized required-evidence candidate, source/correlation/kernel consistency, and failed-closed candidate shape before durable event emission.
- HIGH: threshold-policy change events did not carry the old threshold values required for security-sensitive audit. Fixed by storing current threshold policy in aggregate state and emitting previous `T_high`, previous `T_low`, and previous policy version alongside the new values.
- MEDIUM: association-scoring coarse idempotency keyed an omitted scorer kernel as empty instead of the M0 default kernel, so semantically identical submissions could hash differently. Fixed by normalizing empty kernel values to `association-deterministic.kernel.m0.v1`.
- MEDIUM: authorization-denied and cross-tenant project exclusions could echo the supplied project id/evidence token. Fixed by redacting suppressed exclusion ids and evidence fields to `suppressed`.

Checklist validation:

- Story file loaded and status verified as reviewable before fixes.
- Epic/story resolved as 2.3.
- Architecture loaded from `_bmad-output/planning-artifacts/architecture.md`; no root project-context file was present. MCP resource search returned no configured resources.
- Acceptance criteria, completed tasks, File List, and git changes were cross-checked.
- Code quality, security, and test quality reviewed for the story implementation surface.
- Review notes, Change Log, story status, and sprint status updated.

### File List

- `_bmad-output/implementation-artifacts/2-3-deterministic-association-scorer-and-candidate-generation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/AssociationCandidate.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/AssociationConfidenceInput.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/AssociationDeterministicSignal.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/AssociationEvidenceReference.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/AssociationExclusion.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/AssociationScoringResult.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/AssociationThresholdPolicySnapshot.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/ScoreMailboxMessageAssociation.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/SetAssociationConfidenceThresholds.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationExclusionState.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationReasonCode.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationScoringOutcome.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationSignalClass.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationThresholdBand.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/AssociationWorkflowId.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Adapters/Projects/IProjectDirectory.cs`
- `src/Hexalith.ChatBot.Server/Adapters/Projects/ProjectsProjectDirectory.cs`
- `src/Hexalith.ChatBot.Server/Adapters/Projects/UnavailableProjectDirectory.cs`
- `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj`
- `src/Hexalith.ChatBot.Server/Association/AssociationConfidenceThresholdsChanged.cs`
- `src/Hexalith.ChatBot.Server/Association/AssociationThresholdPolicyInvalidRejection.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxAssociationCandidatesGenerated.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxAssociationInvalidRejection.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxAssociationScoringFailedClosed.cs`
- `src/Hexalith.ChatBot.Server/Association/MailboxEmailAssociatedToProject.cs`
- `src/Hexalith.ChatBot.Server/Association/Scoring/AssociationScoringComputation.cs`
- `src/Hexalith.ChatBot.Server/Association/Scoring/AssociationScoringInput.cs`
- `src/Hexalith.ChatBot.Server/Association/Scoring/AssociationThresholdPolicyValidator.cs`
- `src/Hexalith.ChatBot.Server/Association/Scoring/DeterministicAssociationScorer.cs`
- `src/Hexalith.ChatBot.Server/Association/Scoring/ProjectAssociationCandidateEvidence.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyComposer.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseIdempotencyOperationClass.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AssociationScoringOrchestrator.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/IAssociationScoringOrchestrator.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationNotification.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionEndpoints.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/IAssociationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryAssociationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAssociationEvent.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/AssociationContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Adapters/Projects/ProjectsProjectDirectoryTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Association/Scoring/DeterministicAssociationScorerTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj`
- `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/AssociationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AssociationProjectionTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Change Log

- Added association scoring wire contracts and regenerated the typed ChatBot client from OpenAPI.
- Added deterministic M0 scoring, threshold validation/defaults, gateway dispatch, idempotency, authorization denial mapping, aggregate events, and tenant-partitioned projection support.
- Added focused contract/server/architecture/conformance/client validation for scorer behavior, threshold authorization, projection redaction/idempotency, generated client drift, and aggregate purity.
- Left live Projects adapter integration open because the sibling Projects contracts/client cannot currently be referenced in a no-restore build.
- Closed the Projects adapter seam: referenced `Hexalith.Projects.Client` from Server and Server.Tests, and implemented a live `ProjectsProjectDirectory` over the Projects typed query API that authorizes claimed project ids, maps Projects resolution/lifecycle vocabulary to ChatBot candidates/exclusions, enforces tenant isolation, and fails closed on unavailability; added 10 adapter tests and disambiguated the Aspire `global::Projects` metadata class in the integration E2E test.
- Senior developer review auto-fixed aggregate auto-association invariants, threshold-policy old/new audit fields, association-scoring idempotency kernel normalization, and unauthorized/cross-tenant exclusion redaction; added focused regression coverage.
