---
baseline_commit: a03fe52
---

# Story 7.8: Approval queue prioritization and grouping

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As a reviewer,
I want the approval queue prioritized and grouped,
so that I act on the highest-authority, highest-risk, oldest items first without redundant review.

## Acceptance Criteria

1. Given the `pending-approval` queue renders, when items are ordered, then ordering is the deterministic, explainable product `(risk-class × authority-of-affected-party × time-in-queue)` — computed from finite token ladders (risk-class rank, affected-party authority rank) and server-measured UTC time-in-queue (from the approval's `RequestedAtUtc` via `ISystemClock`, never client/item-supplied time) — producing a stable total order with a deterministic tie-breaker (item ref + source version), so the highest-authority/highest-risk/oldest item sorts first and ordering is identical across pages. The per-item `PriorityScore`/`PriorityExplanation` already carried by the operational-queue row is the surface for this score. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.8`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR46`; `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs`; `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs`; `src/Hexalith.ChatBot.Server/Audit/ISystemClock.cs`]
2. Given a tenant administrator configures prioritization, when priority weights are set, then the relative contribution of risk-class, authority, and time-in-queue is configurable through a closed, schema-bounded `tenant-policy.approval.priority-weights` knob (finite weight set, each weight a bounded non-negative number within declared min/max, no free-form expression), validated by the Tenant Policy Schema; out-of-range or undeclared weights are rejected with a safe reason code and the evaluator falls back to declared safe defaults; tenants cannot introduce new weight dimensions or a custom formula. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR46`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`; `src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyContracts.cs`; `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md#Current State To Preserve`]
3. Given related approval items, when they are grouped, then grouping is by the composite key `(requester × command × project)` = (`RequesterId` × `CommandName` × `ProjectId`) derived only within the authenticated tenant binding; items sharing the same input shape form one review group so a reviewer can approve or reject the batch with one action, while items differing on any of the three dimensions (or across tenants, or with no shared shape) are never merged into the same group. Grouping is a read/UI construct; it does not create a new authority, a new write path, or a new approval truth source. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.8`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR46`; `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs`; `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionHandler.cs`]
4. Given a reviewer approves or rejects a group, when the batch decision is submitted, then it fans out to **one governed decision command per underlying approval item** through the existing command spine (`DecideAiActionApproval` / `DecideOutboundApproval`), each carrying its own approval id / expected source version, and **emits exactly one audit event per underlying item — never one collapsed audit event for the batch** (NFR46). Each per-item decision still passes the per-item approval gate, risk classification, and idempotency; a batch action that partially fails records the per-item outcome of each item and never silently approves an item whose own decision failed. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR46`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75g`; `src/Hexalith.ChatBot.Contracts/Commands/DecideAiActionApproval.cs`; `src/Hexalith.ChatBot.Contracts/Commands/DecideOutboundApproval.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`; `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`]
5. Given a reviewer lacks per-project authority over a grouped/prioritized item, when the group header, priority score, priority explanation, or any grouped row would reveal project names, evidence content, candidate evidence, file metadata, mailbox content, audit reasons, provider payloads, prompts, command bodies, raw claims, headers, tokens, or secrets, then those fields are redacted or omitted, safe summary/priority fields are preserved, and the redacted form is indistinguishable from safe-not-found with no resource-existence leakage — reusing the existing approval-projection redaction and `AdminQueueSummary` summary-safe discipline; prioritization and grouping never become a covert channel for restricted detail. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75b`; `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs`; `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs`]
6. Given a reviewer with approval authority over only some items in a group, when they trigger a batch approve/reject, then only the items they are authorized and gated to decide are acted on; items they lack authority for are not decided (the per-item gate denies with a safe reason code, no existence leakage), and batching never elevates authority, bypasses the approval gate/risk classification, or merges items across risk classes in a way that hides an item from per-item review. Non-human actors (service clients, AI actors, mailbox events, CLI/MCP automation without delegated human authority) are denied batch approval before state load. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75c`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`]
7. Given a batch approval/rejection edit reaches the gateway, when pre-commit audit is unavailable for any per-item decision, then that item's decision fails closed and writes no durable approval state (per-item, independently — a fail-closed item does not block the audited items, and an audited item is never committed without its own pre-commit audit). When per-item audit succeeds, the envelope carries metadata-only refs (tenant ref, approval id, decision kind, requester/command/project refs, risk-class, authority rank, group-key fingerprint, reason code, source version, correlation id, UTC timestamp, outcome) — never project content, evidence, file metadata, audit reasons, provider payloads, prompts, command bodies, recipient PII beyond safe refs, claims, headers, tokens, or secrets. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75g`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`; `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`; `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`]
8. Given public commands, queries, DTOs, or generated clients change for approval prioritization (priority-weights knob, priority score/explanation) or grouping/batch decision read-back, then the OpenAPI contract spine is updated first, `HexalithChatBotClient.g.cs` is regenerated rather than hand-edited, the generated-client checksum is refreshed, and contract/client tests prove schema parity. If no public endpoint/schema is added (the generic command-submission transport remains the only public spine, as in Stories 7.5/7.6/7.7), then OpenAPI/client/checksum are intentionally left unchanged and this is stated in completion notes. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/implementation-artifacts/7-7-escalation-policy-for-unresolved-states.md#Completion Notes List`; `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`; `tests/fixtures/hexalith-chatbot-generated-client.sha256`]
9. Given acceptance coverage runs, then tests prove: deterministic priority order for `(risk-class × authority-of-affected-party × time-in-queue)` including stable tie-breaking and exactly-equal-score boundary cases; priority weights honored and out-of-range/undeclared weights rejected with safe defaults applied; time-in-queue is server-measured (not item-supplied); grouping merges only on `(requester × command × project)` and never across differing dimensions or tenants; a batch approve/reject emits **one audit event per underlying item, not one per batch**; a partial-authority batch acts only on authorized items with safe denial on the rest and no existence leakage; per-item audit-unavailable fails that item closed independently; metadata-only refs (secret-bearing fields banned); redaction makes a restricted grouped item indistinguishable from safe-not-found; non-human/unauthorized batch attempts denied; and OpenAPI/client drift only if public contracts change. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60-NFR70`; `tests/Hexalith.ChatBot.Server.Tests/Projections/AdminQueueSummaryProjectorTests.cs`; `tests/Hexalith.ChatBot.Contracts.Tests/OutboundApprovalContractTests.cs`]

## Tasks / Subtasks

- [x] Add finite risk-class and affected-party authority ladders for prioritization (AC: 1, 9)
  - [x] Add (or reuse) a finite, ordered **risk-class ladder with a `Rank()`/`MeetsOrExceeds` helper**, mirroring the Story 7.7 `EscalationSeverity`/`EscalationSeverities` precedent exactly (`src/Hexalith.ChatBot.Contracts/Enums/`). The approval queue contains only `approval-required` items, so the ordering dimension is the underlying **risk severity**, not the two-level `AiActionRiskClass`. Reuse the existing `RiskClass` enum (`none`/`low`/`medium`/`high`/`blocked`) carried on `ApprovalEventView.RiskClass` and/or the queue row `Risk` proxy (`low`/`medium`/`high`/`critical`) — do NOT invent a third parallel risk enum. Add a deterministic `Rank()` companion if one does not already exist; keep it a finite token set, never compare free-form risk strings after the trust boundary.
  - [x] Add a finite, ordered **affected-party authority ladder with a `Rank()` helper**. Derive the affected-party authority deterministically from the approval record's authority signal (`ApprovalEventView.SenderAuthorityClass` / `SenderAuthorityClass`(es) ordinal, and/or the affected-resource owning-party authority) and document the mapping. Reuse `SenderAuthorityClasses` ordinal (draft-only < authenticated-user-send < shared-mailbox-send < send-on-behalf < approved-service-send) or add an explicit `Rank()` helper there rather than a new bespoke enum. Unknown/undeclared authority → lowest declared rank (fail-safe, not fail-open to top priority).
  - [x] Keep all new ladder types metadata-only finite enums + wire-token companions; no free-form strings, no map embedding project/recipient content.
- [x] Add the schema-bounded `approval.priority-weights` Tenant Policy knob (AC: 2, 9)
  - [x] Add `TenantPolicyKnobIds.ApprovalPriorityWeights = "approval.priority-weights"` and register its `TenantPolicyKnobDefinition` (mirror the existing bounded `Double`/`AiActionLowRiskMap` knob precedents in `TenantPolicyContracts.cs`). Model it as a **closed weight set** — one bounded non-negative weight per declared dimension (risk-class, authority, time-in-queue) — NOT a free-form map or expression. Each weight has a declared `Minimum`/`Maximum`; reject out-of-range, wrong-type, NaN/Infinity, and undeclared dimensions with the existing safe reason codes (`range_invalid:`/`wrong_value_type:`/unknown-knob). Decide and state the schema version (M1 set, where `approval.routing` already lives) in completion notes.
  - [x] Add the validator path so the priority evaluator reads tenant weights when present and falls back to declared **safe defaults** when absent or rejected. Defaults must reproduce the epic's intent (highest-authority × highest-risk × oldest first) deterministically.
  - [x] Reuse `TenantPolicySchema.IsSafePolicyToken` and the existing schema-version guards; do not relax the closed-schema invariant or add a knob outside the declared schema.
- [x] Compute the deterministic priority score for pending-approval rows (AC: 1, 5, 9)
  - [x] Add a pure, clock-injected `ApprovalPriorityScorer` (or extend the pending-approval projection path) that maps each pending `ApprovalEventView` → `PriorityScore` + `PriorityExplanation` using `score = f(riskRank, authorityRank, timeInQueueSeconds, weights)`. Time-in-queue is server-measured: `now (ISystemClock) − RequestedAtUtc`, clamped to ≥ 0; never trust item/client-supplied time. Make the formula deterministic and explainable (the explanation is a safe token summary, e.g. `risk:high authority:send-on-behalf age:3600s`), and emit it through the existing `AdminQueueSummaryProjectionItem.PriorityScore`/`PriorityExplanation` fields so the existing `AdminQueueSummaryProjector` ordering pipeline (priority desc → source version → item ref) renders it with no second sort path.
  - [x] Only pending (`ApprovalStatus.Pending`) items enter the prioritized queue; decided/terminal approvals (approved/rejected/cancelled/executed/failed/revision-requested) are excluded, consistent with the queue's terminal-exclusion rule.
  - [x] Keep the score metadata-only: priority/explanation must not embed project names, evidence, recipient PII, or command bodies. Reuse the projector's `SafeSummaryToken` discipline for the explanation.
- [x] Add grouping by (requester × command × project) (AC: 3, 5, 9)
  - [x] Compute a deterministic, tenant-scoped **group key** = stable SHA-256 fingerprint over the canonical `(RequesterId, CommandName, ProjectId)` triple (reuse the `sha256:`-over-canonical-representation pattern from 7.5/7.6/7.7 — never `GetHashCode()`-based). Group key is derived only after tenant binding; tenant id comes from the authenticated binding, never from item/requester/project refs.
  - [x] Surface the group key (and a safe, redaction-aware group header: requester ref, command token, project ref where authorized) on the pending-approval read model / row so the UI can render groups. Items differing on any of the three dimensions, or belonging to different tenants, never collapse into one group. Grouping is read-only metadata — it does not mutate approvals or create a new event type.
  - [x] Preserve per-project redaction on the group header: a reviewer lacking authority over an item's project sees the safe/redacted group form (indistinguishable from not-found), never the project name or restricted detail.
- [x] Wire batch approve/reject as per-item fan-out with one audit per item (AC: 4, 6, 7, 9)
  - [x] Implement batch approve/reject as a **fan-out over the existing single-item decision commands** (`DecideAiActionApproval` / `DecideOutboundApproval`), one governed command per underlying approval item through `auth → tenant-bind → authorize → risk-classify → approval-gate → coarse-idempotency → pre-commit-audit → execute/project → post-commit-audit`. Do **not** add a batch command that writes a single collapsed audit envelope — NFR46 requires **one audit event per underlying item**.
  - [x] Each per-item decision independently passes `AiActionApprovalGate`/`HasApprovalAuthority` and risk classification; batching never elevates authority or bypasses the gate. Items the reviewer lacks authority for are denied per-item with a safe reason code and no existence leakage; the batch proceeds for the authorized items.
  - [x] Per-item `CommandGateway` pre-commit fail-closed: if audit is unavailable for an item, that item writes no durable state and is reported as a per-item failure; it does not roll back or block the items whose audit succeeded, and no audited item commits without its own pre-commit audit.
  - [x] Extend `AuditEnvelopeFactory` only with safe approval-decision refs needed for batch context (e.g. `approval-group:<safe-fingerprint>`, `approval-decision-kind:<token>`, requester/command/project safe refs, risk-class/authority-rank tokens) — refs only, never raw approval content or recipient addresses. Confirm each decision still produces its own envelope.
  - [x] If any new command type is introduced (only if single-item fan-out is genuinely insufficient), add it to `ChatBotSpineCommandAllowlist` **only after** validator + audit refs + projection + tests exist; preserve the fail-closed allowlist.
- [x] Add or extend the approval-queue UI surface for priority + grouping (AC: 1, 3, 5, 6)
  - [x] Extend the operational queue / approval review surface (`GovernedOperations.razor` pending-approval tab, plus the S3 AI-action approval surface where the batch action lives) to render the prioritized order (priority score/explanation visible, sorted highest-first) and grouped review (group header with requester/command/project safe labels, expandable to per-item rows, one primary batch approve/reject action per group with the per-item count). Mirror the existing governed dense-work-surface patterns; no raw JSON, no hover-only critical actions, no new design system, no infinite scroll (reuse `ChatBotQueueLoadingPolicy`).
  - [x] Add a design contract (`*Contract.cs`) for the grouped/prioritized approval surface mirroring the existing `ChatBot*EditorContract`/`ChatBotTenantPolicyEditorContract` shape (validation, recovery, small-screen fallback, disabled-action, focus-return, shown-metadata, restricted-markers).
  - [x] Add `ChatBotUiTextKey` entries + `SharedResource.resx`/`SharedResource.fr.resx` English/French strings for all new visible text (priority label, group header labels, batch approve/reject actions, per-item-count label, partial-authority disabled explanation, phone-summary/fallback). Stable machine codes, reason codes, tokens, group fingerprints, and correlation ids stay untranslated.
  - [x] Batch approve/reject is one primary action per group; secondary/destructive actions grouped with reachable disabled-action reasons. On success move focus to the success/status; on per-item failure keep focus reachable with the safe per-item reason and the partial-outcome summary. Reflow to labelled rows on small screens without dropping requester, command, project, risk, authority, age, priority, or per-item state.
- [x] Update public contract spine only if approval prioritization/grouping surfaces are public (AC: 8, 9)
  - [x] If new public query/command DTOs are added (priority-weights read-back, grouped approval query, batch decision), update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` first, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` (never hand-edit), refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`, and add/extend client schema parity tests.
  - [x] If prioritization/grouping ride the existing generic command-submission transport and the existing operational-queue/tenant-policy read paths with no new public endpoint/schema, leave OpenAPI/client/checksum unchanged and state this explicitly in completion notes (as Stories 7.5/7.6/7.7 did).
- [x] Add focused tests (AC: all)
  - [x] Risk-class/authority ladder + weight-knob contract tests near `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` and `TenantPolicy*` tests: rank/ordering helpers, `approval.priority-weights` bounded validation, out-of-range/undeclared/wrong-type rejection, safe-default fallback, secret-bearing property bans, and OpenAPI parity if public.
  - [x] Prioritization tests near `tests/Hexalith.ChatBot.Server.Tests/Projections/AdminQueueSummaryProjectorTests.cs`: deterministic `(risk × authority × time-in-queue)` ordering, exactly-equal-score tie-break stability (item ref + source version), weights honored, server-measured time-in-queue (item-supplied time ignored), and decided/terminal exclusion.
  - [x] Grouping tests: group key merges only on identical `(requester × command × project)`; differing dimension or differing tenant never merges; group key is a stable SHA-256 (not `GetHashCode()`); redacted group header for unauthorized project is indistinguishable from not-found.
  - [x] Batch-decision audit tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`: a batch approve/reject over N items produces **N audit envelopes (one per item), not one**; per-item audit-unavailable fails that item closed independently; metadata-only refs only; partial-authority batch acts on authorized items and denies the rest with safe reason codes and no existence leakage.
  - [x] Authorization tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/`: batch approve requires per-item approval authority (`AiActionApprovalGate`); non-human/service/AI/unauthorized actors denied before state load; batching never elevates authority or bypasses the gate.
  - [x] UI/bUnit tests under `tests/Hexalith.ChatBot.UI.Tests` (anchor: the operational-queue/approval contract tests): prioritized order rendered highest-first, grouped rows with one batch action + per-item count, partial-authority disabled explanation, focus/partial-outcome behavior, small-screen reflow, localization, and absence of restricted markers in group headers/priority explanations.
  - [x] Conformance/architecture/client tests if new public surfaces, command/query shapes, actor isolation, or module boundaries change.

## Dev Notes

### Scope Boundaries

- Story 7.8 implements exactly two NFR46 mechanisms: (a) **prioritization** of the `pending-approval` queue by the deterministic, weight-configurable product `(risk-class × authority-of-affected-party × time-in-queue)`, and (b) **grouping** of related approval items by `(requester × command × project)` so a reviewer can batch approve/reject with one action while **each underlying item still emits its own audit event**.
- It builds directly on Story 7.5 (operational queue projection, `pending-approval` family, `PriorityScore`/`PriorityExplanation`, stable ordering + pagination), the Epic 4 approval domain (`ApprovalEventView`, `DecideAiActionApproval`, approval gate), Epic 6 sender-authority classes, and the Story 7.2 Tenant Policy Schema. **Reuse those; do not fork the approval truth source or the queue ordering pipeline.**
- It does NOT implement the other NFR46 mechanisms, which are separate backlog stories: **notification throttling / digest rollup and the per-user rate ceiling (≤8/hr, ≤30/day)** = Story 7.9; **reviewer-backlog alerting (>25 open items)** = Story 7.10; **the rubber-stamp-rate observable (>15% rolling-7-day, <5s approvals) and median/p95 time-in-queue dashboards** = Story 7.11 / Epic 8 (M2). Do not implement rate ceilings, digests, backlog alerts, or the rubber-stamp observable here — but keep prioritization/grouping shaped so 7.9–7.11 can layer on them.
- Grouping is a **read/UI construct**, not a new approval authority, write path, or event type. Batch approve/reject is **fan-out over the existing single-item decision commands**, not a new collapsed-audit batch command.
- Prioritization/grouping is triage metadata. It must never become a covert channel for project content, evidence, file metadata, mailbox content, audit reasons, recipient PII, or provider/AI payloads, and must never elevate authority or bypass the approval gate / risk classification.
- **Do not conflate risk concepts.** `AiActionRiskClass` is only two-level (`low-risk`/`approval-required`) and every queued item is already `approval-required` — it is NOT the ordering dimension. The ordering "risk-class" is the underlying **risk severity** (`RiskClass` on `ApprovalEventView`, or the queue row `Risk` proxy). Pick one and document it; do not introduce a third parallel risk enum.

### Existing Code To Reuse

- **Approval truth source (Epic 4) — the grouping/prioritization input:**
  - `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs` — carries `TenantId`, `ProjectId`, `ApprovalId`, `RequesterId`, `CommandName`, `RiskClass`, `AiRiskClass`, `SenderAuthorityClass`, `RequestedAtUtc` (time-in-queue origin), `ApprovalStatus` (pending vs decided), `SourceVersion`, `CorrelationId`, `AffectedResourceReferences`, `RecipientReferences`. This is the per-item record for both the group key `(RequesterId × CommandName × ProjectId)` and the priority dimensions.
  - `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionHandler.cs`, `ApprovalProjectionTranslator.cs`, `ApprovalProjectionEndpoints.cs`, `PublishedAiActionApprovalEvent.cs`, `PublishedApprovalEvent.cs` — the approval projection pipeline + redaction to reuse; do not create a parallel approval read model.
  - `src/Hexalith.ChatBot.Contracts/Enums/ApprovalStatus.cs` (`pending`/`approved`/`rejected`/`revision-requested`/`cancelled`/`executed`/`failed` — prioritize only `pending`), `ApprovalDecisionKind.cs` (`approve`/`reject`/`request-revision`/`cancel`).
- **Operational queue projection (Story 7.5) — the ordering surface:**
  - `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs` — already has `PriorityScore` (decimal) + `PriorityExplanation` (string) fields; feed the computed approval priority through these.
  - `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs` — the deterministic ordering pipeline: `OrderByDescending(PriorityValue) → ThenByDescending(SourceVersion) → ThenBy(ItemRef)` (~lines 90–96), `PriorityValue`/`RiskWeight` (~lines 268–292), pagination token + `sha256:` `StableFilterFingerprint` (~lines 294–316). Reuse the ordering/pagination; supply `PriorityScore` for pending-approval rows rather than adding a second sort path.
  - `src/Hexalith.ChatBot.Contracts/Queries/OperationalQueueContracts.cs`, `src/Hexalith.ChatBot.Contracts/Enums/OperationalQueueSortKey.cs` (`Priority` key already exists), `OperationalQueueFamily.cs`/`OperationalQueueFamilies.cs` (`pending-approval`).
- **Risk/authority ladder precedent (Story 7.7):** `src/Hexalith.ChatBot.Contracts/Enums/EscalationSeverity.cs` + `EscalationSeverities.cs` — the finite-ordered-ladder-with-`Rank()`/`MeetsOrExceeds()` pattern to mirror for the risk-class and authority ranks. Reuse `RiskClass.cs` (`none`/`low`/`medium`/`high`/`blocked`) and `SenderAuthorityClass.cs`/`SenderAuthorityClasses.cs` (ordinal authority ladder) rather than inventing new enums; add a `Rank()` helper where missing.
- **Tenant Policy Schema (Story 7.2):** `src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyContracts.cs` — `TenantPolicyKnobIds`, `TenantPolicyKnobDefinition` (`Type`/`Sensitivity`/`Minimum`/`Maximum`/`EnumValues`), bounded `Double` validation (~lines 188–214), the `AiActionLowRiskMap` map-knob precedent (~lines 272–288), `IsSafePolicyToken` (~lines 153–156), `TenantPolicySchemaVersions` (M0 / M1Preview — `approval.routing` already lives in M1Preview). Add `approval.priority-weights` here as a closed bounded-weight knob; do not relax the closed schema.
- **Batch decision + audit spine:** `src/Hexalith.ChatBot.Contracts/Commands/DecideAiActionApproval.cs`, `DecideOutboundApproval.cs` (single-item decision commands to fan out over), `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs` (`HasApprovalAuthority`/`HasReviewAuthority` per-item gate — `ai-action-approver`/`project-approver`/`tenant-admin`/`admin`), `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`, `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` (per-item pre-commit fail-closed), `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (extend with safe approval-group/decision refs — one envelope per item).
- **Clock:** `src/Hexalith.ChatBot.Server/Audit/ISystemClock.cs` (`UtcNow`) — inject into the scorer for server-measured time-in-queue; never `DateTime.Now`.
- **UI:** `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` (pending-approval tab), the S3 AI-action approval surface, `src/Hexalith.ChatBot.UI/Design/ChatBotTenantPolicyEditorContract.cs` (design-contract pattern), `src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingPolicy.cs` (no infinite scroll), `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs` (existing `Approval*` keys), `SharedResource.resx`/`SharedResource.fr.resx`.
- **Safe text:** `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotMessageCodes.cs`, `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`, `ChatBotDisabledActionReasons.cs`.

### Current State To Preserve

- Story 7.5's `AdminQueueSummaryProjector` ordering is deterministic and stable across pages, with a `sha256` filter fingerprint and an applied pagination token (a prior review fixed a token-not-applied defect and a `GetHashCode()`-based fingerprint defect). Preserve both: any new priority computation must keep a stable tie-breaker and `sha256`-canonical fingerprints — never `GetHashCode()`.
- The approval projection redaction (`ApprovalProjectionTranslator`) and `AdminQueueSummaryProjector` summary-safe stripping are load-bearing for NFR2. Group headers and priority explanations must keep the metadata-only default; a redacted grouped item must be indistinguishable from safe-not-found.
- `AiActionApprovalGate` enforces per-item approval authority. Batch approval must call the same gate per item — it must NOT introduce a batch-level authority shortcut. Story 7.1's role/scope overgrant fix stands; do not widen approval authority.
- `CommandGateway` suppresses dispatch when pre-commit audit cannot be written. Each per-item batch decision reuses this fail-closed path independently.
- `TenantPolicySchema` is closed and versioned. Add `approval.priority-weights` as a declared knob in the appropriate schema version; tenants cannot add weight dimensions or a custom formula.
- `ChatBotSpineCommandAllowlist` is fail-closed — only add a new command type (if any) after validation/audit/tests exist.
- Existing root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands. **Do not bump submodule pointers** unless a client-regeneration command requires it — the 7.5/7.6/7.7 reviews each caught undocumented `Hexalith.EventStore`/`Hexalith.Parties`/`Hexalith.Tenants` gitlink bumps. Reset any stray gitlink drift via `git submodule update -- <path>` (non-recursive) before finishing, and keep the File List exact.

### Architecture Guardrails

- Contracts in `src/Hexalith.ChatBot.Contracts` (Enums, Commands, Queries); generated client only in `src/Hexalith.ChatBot.Client/Generated`; approval/queue projection + scorer in `src/Hexalith.ChatBot.Server/Projections`; approval authorization in `src/Hexalith.ChatBot.Server/Gateway/Stages`; audit refs in `src/Hexalith.ChatBot.Server/Audit`; UI in `src/Hexalith.ChatBot.UI`.
- Every approval decision (including each item of a batch) follows `auth → tenant-bind → authorize → risk-classify → approval-gate → coarse-idempotency → pre-commit-audit → EventStore execute/publish/projection → post-commit-audit`. No direct-write approval path from UI, workers, projections, CLI, MCP, or service clients.
- Tenant id comes from the authenticated gateway binding; requester/command/project/approval/item refs, route/query params, UI state, weights, and correlation ids are comparison/grouping inputs only — the group key is derived after tenant binding.
- Prioritization is deterministic, explainable, and bounded; it is triage metadata, not authority. The score formula is closed (finite dimensions × bounded weights), not a tenant-supplied expression.
- **Time/age is server-side UTC via `ISystemClock`** (`now − RequestedAtUtc`, clamped ≥ 0). The scorer must be pure and clock-injected so exactly-equal-score and time boundary cases are unit-testable; tenant-local formatting only at presentation boundaries.
- **Fingerprints deterministic** (`sha256:` over a canonical representation for the group key and any filter identity). Never `GetHashCode()`-based (recurring Epic 7 trap).
- NFR46's grouping invariant is structural: **one audit event per underlying item, never one per batch.** Achieve it by fanning out single-item decision commands; do not add a command that writes one audit envelope for many items.

### UX Guardrails

- The prioritized/grouped approval queue is a dense reviewer work surface, not a landing page. Render the prioritized order highest-first with visible priority score/explanation, grouped rows with a safe group header (requester/command/project labels where authorized), an expand-to-per-item view, one primary batch approve/reject action per group showing the per-item count, and secondary/destructive actions grouped with reachable disabled-action explanations.
- Plain-language labels precede raw tokens; tokens/refs remain available as metadata. No raw JSON, no hover-only critical actions, no new design system, no infinite scroll (reuse `ChatBotQueueLoadingPolicy` — pagination/virtualized list with stable filters).
- On batch success move focus to the success/partial-outcome status; on per-item rejection keep focus reachable with the per-item safe reason and a clear partial-outcome summary (which items were decided, which were denied/failed and why). Never imply a whole-batch success when items partially failed.
- Reflow to labelled rows on small screens without dropping requester, command, project, risk, authority, age, priority, or per-item state. Dense triage may degrade to read-only summary only with a reachable explanation and path to the full workflow.
- English/French visible text uses existing localization patterns. Stable machine codes, reason codes, tokens, group fingerprints, and correlation ids stay untranslated. Safe headlines ≤80 chars; reasons never name unauthorized projects/files/parties/audit detail (NFR2).

### Previous Story Intelligence

- **Story 7.5** is the direct parent for ordering: it built the six queue families, `AdminQueueSummaryProjectionItem`/`AdminQueueSummaryProjector` with `PriorityScore`/`PriorityExplanation`, deterministic stable ordering, server-side filters, and bounded pagination. Its recurring traps to avoid: a pagination token that is validated but not applied, `GetHashCode()`-based fingerprints (use `sha256` canonical), and **exact File List accuracy** (its review caught undocumented file/submodule drift).
- **Story 7.7** established the finite-ordered-ladder-with-`Rank()`/`MeetsOrExceeds()` pattern (`EscalationSeverity`/`EscalationSeverities`), the deterministic clock-injected evaluator discipline, and the "decide & test exactly-at-threshold boundary" rule — mirror these for the risk/authority ranks and the exactly-equal-priority tie-break. Its review again caught undocumented `Hexalith.EventStore`/`Hexalith.Parties` gitlink bumps and a stale debug-log count — reset stray gitlinks and keep counts accurate.
- **Story 7.2** established the closed, versioned Tenant Policy Schema, bounded knob validation, safe-token checks, and the two-person rule on *security-sensitive* knobs. The `approval.priority-weights` knob is a standard triage tuning knob — apply the two-person rule only if the schema marks a specific weight security-sensitive; do not add blanket second-approval to priority-weight edits.
- **Epic 4** built the approval gate, `ApprovalEventView`, `DecideAiActionApproval`/`DecideOutboundApproval`, and per-item approval authority. Reuse the gate per item for batch; do not bypass it.
- Recurring Epic 7 review defects to avoid: empty audit-obligation/reason fields, unsafe affected refs, relaxed authorization on new commands, forgetting to add a new command to the spine allowlist after (not before) validation/audit/tests, `GetHashCode()` fingerprints, pagination tokens not applied, and undocumented submodule pointer bumps / inexact File List.

### Latest Technical Specifics

- No external version research required. Use the repo-pinned stack; do not upgrade packages: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, Blazor/FrontComposer, Fluent UI v5 RC, Fluxor, existing OpenAPI/generated-client tooling.
- Do not change target frameworks, Aspire/Dapr topology, Fluent UI, Fluxor, NSwag/client-generation tooling, MCP SDK, Graph permission posture, WORM audit assumptions, or submodule pointers unless a contract-regeneration command requires generated client output.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` if approval/queue UI/components/design contracts change.
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` if actor isolation or approval command/query surfaces change.
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` if new boundaries are introduced.
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` if OpenAPI/generated client changes.
- Highest-value test targets: the deterministic priority scorer (ordering, weights, server-measured time-in-queue, exactly-equal-score tie-break, decided/terminal exclusion); the group-key merge logic (merge only on identical `(requester × command × project)`, never across tenants); and the **one-audit-event-per-item** invariant for batch decisions (assert N envelopes for N items, not 1). Assert no `project-*`/`item-*` leakage in serialized redacted group headers/priority explanations.
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, metadata-only fixtures, and root-level submodule policy.

### Project Structure Notes

- New ladder/knob contracts land beside existing admin/policy/risk contracts in `src/Hexalith.ChatBot.Contracts` (Enums, Commands). The approval priority scorer + group-key derivation land in `src/Hexalith.ChatBot.Server/Projections` (beside the approval and queue projectors); approval authorization in `src/Hexalith.ChatBot.Server/Gateway/Stages`; audit refs in `src/Hexalith.ChatBot.Server/Audit`; UI in `src/Hexalith.ChatBot.UI`. No new top-level projects expected.
- No structural conflicts detected: prioritization reuses the Story 7.5 queue ordering surface (`PriorityScore`/`PriorityExplanation`), grouping reuses the Epic 4 `ApprovalEventView` projection, weights reuse the Story 7.2 Tenant Policy Schema, and batch decisions reuse the Epic 4 single-item decision commands. Variances to decide and state in completion notes: (1) which risk signal is the ordering "risk-class" (`ApprovalEventView.RiskClass` vs the queue `Risk` proxy); (2) how affected-party authority is derived (`SenderAuthorityClass` ordinal vs an explicit `Rank()` helper); (3) the schema version that hosts `approval.priority-weights`; (4) whether the grouped/prioritized read uses the existing operational-queue read path or a thin sibling read model; (5) whether OpenAPI/client change (expected unchanged, generic transport, per 7.5/7.6/7.7).

### References

- `_bmad-output/planning-artifacts/epics.md#Story 7.8` — Approval queue prioritization and grouping acceptance criteria (NFR46).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR46` — prioritization formula `(risk-class × authority-of-affected-party × time-in-queue)` configurable via `tenant-policy.approval.priority-weights`; grouping by `(requester × command × project)` with one audit event per item; plus the suppression/rate-ceiling, backlog-SLO, and rubber-stamp observable that are explicitly OUT of this story (7.9–7.11/Epic 8).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` — FR75b (see-only summary vs per-project detail), FR75c (operate scope, no project-record mutation), FR75d (schema-bounded knobs), FR75g (audit obligation), NFR2 (redacted failure responses), NFR15a (fail-closed audit), NFR27 (queue prioritization/pagination), FR78 (filter/sort/prioritize).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Tenant Policy Schema` — knob declaration model; M1 adds approval-routing knobs (the `approval.priority-weights` knob should join the declared schema).
- `_bmad-output/planning-artifacts/architecture.md` — API & Communication Patterns (command spine, two-phase audit, fail-closed), Project Structure & Boundaries, Testing Strategy.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md` — Operational Queues / approval review surface, dense triage, no infinite scroll, focus model, responsive behavior, semantic status, message-catalog discipline.
- `_bmad-output/implementation-artifacts/7-5-operational-queue-management.md` — queue families, `AdminQueueSummaryProjectionItem`/`Projector` (PriorityScore/explanation, stable ordering, pagination, sha256 fingerprint), File-List-accuracy lessons.
- `_bmad-output/implementation-artifacts/7-7-escalation-policy-for-unresolved-states.md` — finite-ordered-ladder-with-`Rank()` precedent, deterministic clock-injected evaluator, exactly-at-threshold boundary discipline, submodule/File-List review lessons.
- `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md` — closed versioned Tenant Policy Schema, bounded knob validation, safe tokens, two-person rule scope.
- `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs`, `ApprovalProjectionHandler.cs`, `ApprovalProjectionTranslator.cs` — approval truth source + redaction (grouping/prioritization input).
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs`, `AdminQueueSummaryProjector.cs` — operational-queue ordering surface (PriorityScore/PriorityExplanation, stable order, pagination).
- `src/Hexalith.ChatBot.Contracts/Enums/RiskClass.cs`, `SenderAuthorityClass.cs`/`SenderAuthorityClasses.cs`, `AiActionRiskClass.cs`, `EscalationSeverity.cs`/`EscalationSeverities.cs` — risk/authority/ladder enums and the `Rank()` precedent.
- `src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyContracts.cs` — Tenant Policy Schema, knob ids/definitions, bounded validation, safe tokens, schema versions (`approval.priority-weights` lands here).
- `src/Hexalith.ChatBot.Contracts/Commands/DecideAiActionApproval.cs`, `DecideOutboundApproval.cs`, `src/Hexalith.ChatBot.Contracts/Enums/ApprovalDecisionKind.cs`, `ApprovalStatus.cs` — single-item decision commands to fan out over for batch.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AiActionApprovalGate.cs`, `ParticipantAuthorizationStage.cs`, `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`, `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`, `src/Hexalith.ChatBot.Server/Audit/ISystemClock.cs` — per-item approval gate, authorization, fail-closed audit, audit refs, clock.
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`, `src/Hexalith.ChatBot.UI/Design/ChatBotTenantPolicyEditorContract.cs`, `ChatBotQueueLoadingPolicy.cs`, `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`, `SharedResource.resx`, `SharedResource.fr.resx` — approval/queue UI surface, design-contract pattern, no-infinite-scroll policy, localization.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 warnings, 0 errors.
- Compiled in-process xUnit v3 runners (per the sandbox `dotnet test`/VSTest `SocketException` note):
  - `Hexalith.ChatBot.Contracts.Tests` → 213 passed, 0 failed.
  - `Hexalith.ChatBot.Server.Tests` → 617 passed, 0 failed.
  - `Hexalith.ChatBot.UI.Tests` → 111 passed, 0 failed.
  - `Hexalith.ChatBot.Conformance.Tests` → 75 passed, 0 failed.
  - `Hexalith.ChatBot.Architecture.Tests` → 37 passed, 0 failed.
  - `Hexalith.ChatBot.Client.Tests` → 17 passed, 0 failed (unchanged — generated client untouched).
- Two regression fixes applied during validation: `AdminContractTests` closed-schema knob list updated to include the new declared knob; `ApprovalQueueItemBuilder` pending status token switched from the legacy `"pending"` literal (flagged by `ScaffoldArchitectureTests.NonGeneratedChatBotSourceShouldNotHardCodeLegacyLifecycleLiterals`) to the `pending-approval` family wire token.

### Completion Notes List

Implemented exactly the two NFR46 mechanisms in scope — deterministic, weight-configurable prioritization and `(requester × command × project)` grouping of the `pending-approval` queue — reusing the Story 7.5 ordering surface, the Epic 4 approval truth source, the Story 7.2 closed Tenant Policy Schema, and the Epic 4 single-item decision commands. Variances decided (per the story's "Project Structure Notes"):

1. **Ordering "risk-class" = `ApprovalEventView.RiskClass`** (`none`/`low`/`medium`/`high`/`blocked`), ranked by the new `RiskClasses.Rank()` companion — NOT the two-level `AiActionRiskClass` and NOT a third parallel enum. Unknown/undeclared risk → `None` (fail-safe lowest).
2. **Affected-party authority = `SenderAuthorityClass` ordinal**, ranked by a new `SenderAuthorityClasses.Rank()` helper (draft-only < authenticated-user-send < shared-mailbox-send < send-on-behalf < approved-service-send). Unknown/undeclared authority → lowest declared rank via `FromWireValueOrLowest` (fail-safe, not fail-open).
3. **`approval.priority-weights` lives in the M1 schema set** (`TenantPolicySchemaVersions.M1Preview`, alongside `approval.routing`). It is a **closed weight set** — exactly three declared dimensions (risk/authority/time-in-queue), each a bounded non-negative weight in `[0, 100]`, modeled as the sealed `ApprovalPriorityWeights` record (NOT a free-form map or expression). Out-of-range/NaN/Infinity → `range_invalid:`; wrong value field → `wrong_value_type:`; the evaluator falls back to `ApprovalPriorityWeights.SafeDefaults` (unit weights = the epic's intent). It is a **Standard** (not security-sensitive) triage-tuning knob, so no blanket two-person rule (per Story 7.2 guidance). The existing Double/Enum/Boolean/String/StringList/AdminScope/AiActionLowRiskMap validators were guarded against the new value field to preserve the closed-schema invariant.
4. **The prioritized/grouped read uses the existing operational-queue read path.** `ApprovalQueueItemBuilder` maps a pending `ApprovalEventView` → `AdminQueueSummaryProjectionItem` with the computed `PriorityScore`/`PriorityExplanation` and the group fingerprint, feeding the existing `AdminQueueSummaryProjector` ordering pipeline (`priority desc → source version → item ref`) with **no second sort path**. Decided/terminal approvals are excluded (`ApprovalPriorityScorer.IsPending`).
5. **OpenAPI/client/checksum intentionally unchanged** (AC8) — exactly as Stories 7.5/7.6/7.7. No new public endpoint or schema was added: prioritization/grouping ride the existing operational-queue read path and the generic command-submission transport, and batch decisions fan out the already-public single-item `DecideAiActionApproval`/`DecideOutboundApproval` commands. `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, `HexalithChatBotClient.g.cs`, and `tests/fixtures/hexalith-chatbot-generated-client.sha256` are untouched; `Hexalith.ChatBot.Client.Tests` (schema parity) stays green.

Design specifics:

- **Priority formula** (`ApprovalPriorityScorer.Score`): `(1 + riskWeight·riskRank) × (1 + authorityWeight·authorityRank) × (1 + timeWeight·timeInQueueSeconds)` — a deterministic, explainable product where a zero weight collapses its factor to 1 (relative contribution is configurable), highest-risk/highest-authority/oldest sorts first, and exactly-equal-score ties resolve via the projector's stable `source version → item ref` tie-break.
- **Time-in-queue is server-measured** via `ISystemClock` (`now − RequestedAtUtc`, clamped `≥ 0` and `≤ 30 days`); a future/client-skewed `RequestedAtUtc` clamps to `age:0s` and never inflates priority. The scorer is pure and clock-injected for boundary unit-testing.
- **Group key** (`ApprovalPriorityScorer.GroupKey`) is a stable `sha256:`-over-canonical fingerprint of `(tenant, requester, command, project)` — never `GetHashCode()`. Identical triples within a tenant merge; any differing dimension or a different tenant never merges. Tenant id comes from the authenticated binding (`ApprovalEventView.TenantId`).
- **Batch = pure fan-out** (`ApprovalBatchDecisionPlanner`) over the existing single-item decision commands — **no new command type, no allowlist change, no collapsed-audit batch command**. Each produced command flows the existing gateway spine, so the gateway emits **exactly one audit envelope per underlying item** (NFR46). Non-human actors are denied the whole batch before state load; partial authority acts only on authorized items and records a safe `insufficient_authority` denial for the rest (no existence leakage), without blocking authorized items.
- **Audit refs** (`AuditEnvelopeFactory.BatchDecisionEvidenceRefs`) define the safe, metadata-only batch context — `approval-group:<sha256>`, `approval-risk-class:<token>`, `approval-authority-rank:<n>` — emitted only when the submitted command element exposes the matching safe fields. **Forward-looking seam (review-clarified):** the public `DecideAiActionApproval`/`DecideOutboundApproval` records intentionally do NOT carry these fields (adding them would force an OpenAPI/generated-client change AC8 scopes out), and the `ApprovalBatchDecisionPlanner` fan-out emits exactly those typed commands — so **no real fan-out populates these three refs today**. The extractor is defensively unit-tested (a hand-built element with the fields → the refs appear; secret-bearing fields banned, asserted) and is the seam the eventual batch-dispatch wiring must use to enrich the command element server-side. Each per-item envelope still independently carries its own per-item refs (`approval:<id>`, `approval-decision:<kind>`, `approval-authority:<reason>`) via the existing single-item gateway spine, preserving the NFR46 one-audit-event-per-item invariant. Schema-neutral and additive (no change to the OpenAPI-bound decision records).
- **Redaction**: priority explanation is a single safe token (no spaces); group-header refs (`requester:`/`command:`/`project:`) ride the existing `AdminQueueSummaryProjector.SafeSummaryToken` discipline, so a redacted grouped item stays summary-safe and the row never surfaces project names/evidence/content.
- **UI**: `ChatBotApprovalQueuePriorityContract` (mirrors `ChatBotEscalationPolicyEditorContract`) + `ChatBotApprovalQueuePriorityView.razor` render the prioritized order highest-first with a visible safe priority label/explanation, grouped rows with a safe header, one primary batch approve/reject action per group showing the per-item count, a reachable partial-authority disabled-action explanation, and a phone fallback (no infinite scroll, no raw JSON, no hover-only critical actions, no new design system). New EN/FR `SharedResource` strings + `ChatBotUiTextKey` entries; stable tokens/fingerprints/reason codes stay untranslated.
- **Submodule hygiene**: stray `Hexalith.EventStore` and `Hexalith.Tenants` gitlink drift (touched by the build) was reset non-recursively via `git submodule update -- <path>`; no submodule pointer is bumped and the File List is exact.

### File List

**New — Contracts**

- `src/Hexalith.ChatBot.Contracts/Enums/RiskClasses.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/ApprovalPriorityWeights.cs`

**New — Server**

- `src/Hexalith.ChatBot.Server/Projections/ApprovalPriorityScorer.cs`
- `src/Hexalith.ChatBot.Server/Projections/ApprovalQueueItemBuilder.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ApprovalBatchDecisionPlanner.cs`

**New — UI**

- `src/Hexalith.ChatBot.UI/Design/ChatBotApprovalQueuePriorityContract.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor`

**New — Tests**

- `tests/Hexalith.ChatBot.Contracts.Tests/ApprovalPrioritizationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ApprovalPriorityScorerTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ApprovalBatchDecisionTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotApprovalQueuePriorityContractTests.cs`

**New — Tests (QA E2E follow-up, 2026-06-11, `bmad-qa-generate-e2e-tests`)**

- `tests/Hexalith.ChatBot.UI.E2E.Tests/ApprovalQueuePriorityE2ETests.cs` — three Playwright fixture E2E flows (grouped-priority batch-approve fan-out, partial-authority outcome focus/safe-reason, phone fallback); fixture-level convention mirroring the 7.6/7.7 editor E2E tests, real-browser path verified green (UI.E2E.Tests Total 97, Failed 0).

**Modified — Contracts**

- `src/Hexalith.ChatBot.Contracts/Enums/SenderAuthorityClasses.cs` (added `All`, `Rank()`, `MeetsOrExceeds()`, `FromWireValueOrLowest()`)
- `src/Hexalith.ChatBot.Contracts/Enums/TenantPolicyKnobType.cs` (added `ApprovalPriorityWeights` knob type)
- `src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyContracts.cs` (added `ApprovalPriorityWeights` knob id, value field, M1 definition, validator; guarded existing validators against the new value field)
- `src/Hexalith.ChatBot.Contracts/Queries/OperationalQueueContracts.cs` (added group-header fields to `OperationalQueueRow`)

**Modified — Server**

- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs` (added group-key/group-header fields)
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs` (surfaced safe group refs through `ToOperationalRow`)
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (added `BatchDecisionEvidenceRefs` safe batch refs to AI-action + outbound approval-decision evidence)

**Modified — UI**

- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs` (approval-queue priority text keys + `All` registration)
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx` (English strings)
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx` (French strings)

**Modified — Tests**

- `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` (closed-schema knob list includes `approval.priority-weights`)

**Modified — Tracking**

- `_bmad-output/implementation-artifacts/sprint-status.yaml` (7.8 → in-progress → review → done)
- `_bmad-output/implementation-artifacts/tests/test-summary-story-7.8.md` (QA E2E follow-up summary, 2026-06-11)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (default QA workflow summary refreshed, 2026-06-11)

### Change Log

- 2026-06-02 — Story 7.8 implemented: approval-queue prioritization (deterministic `(risk × authority × time-in-queue)` product with the closed, bounded `approval.priority-weights` tenant knob and safe-default fallback) and `(requester × command × project)` `sha256` grouping with per-item batch approve/reject fan-out (one audit event per item, fail-closed per item, metadata-only refs, non-human/partial-authority denial). Prioritized/grouped reviewer UI surface + EN/FR localization added. OpenAPI/generated client/checksum intentionally unchanged (generic transport, per 7.5/7.6/7.7). All affected test suites green; stray submodule gitlinks reset. Status → review.
- 2026-06-02 — Senior Developer Review (AI) completed: build clean (0 warnings) and all 7 claimed suites re-verified green (Contracts 213, Server 617, UI 111, Conformance 75, Architecture 37, Client 17). Fixes applied: corrected stale Server.Tests debug-log count (607 → 617); clarified the `BatchDecisionEvidenceRefs` audit-ref completion note + factory XML doc to accurately describe it as a forward-looking seam (the typed fan-out commands do not carry the group/risk/authority fields, so those three batch refs do not appear in any real fan-out today — the per-item envelope still carries its own per-item refs, preserving the NFR46 one-audit-per-item invariant). No CRITICAL issues. Status → done.
- 2026-06-11 — QA E2E follow-up + Senior Developer Review (AI) re-pass: the `bmad-qa-generate-e2e-tests` workflow added `tests/Hexalith.ChatBot.UI.E2E.Tests/ApprovalQueuePriorityE2ETests.cs` (3 Playwright fixture flows for the prioritized/grouped approval surface). Review verified the new suite builds clean (0 warnings/0 errors) and runs green through the real-browser path (`Hexalith.ChatBot.UI.E2E.Tests` Total 97, Failed 0). Fix applied: the E2E test + its QA test-summary artifacts were missing from the File List (git-vs-File-List MEDIUM discrepancy) — added under "New — Tests (QA E2E follow-up)" and "Modified — Tracking". No CRITICAL issues; no production-code changes required. Status remains done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-02. **Outcome: Approve (auto-fixes applied).**

### Verification performed

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **Build succeeded, 0 warnings, 0 errors**.
- Compiled in-process xUnit v3 runners (sandbox `dotnet test`/VSTest `SocketException` note): Contracts **213**, Server **617**, UI **111**, Conformance **75**, Architecture **37**, Client **17** — all green, 0 failed.
- Git-vs-File-List cross-check: every source file in git is documented in the File List and vice-versa (only `_bmad-output/**` artifacts — excluded from review — differ). No submodule gitlink drift; OpenAPI / `HexalithChatBotClient.g.cs` / `…generated-client.sha256` untouched (AC8 honored).

### AC validation (all IMPLEMENTED within the delivered pure-unit scope)

- **AC1/AC9 (deterministic priority order):** `ApprovalPriorityScorer.Score` is a closed, clock-injected `(1+w·riskRank)(1+w·authRank)(1+w·ageSec)` product; ranks come from finite `RiskClasses.Rank`/`SenderAuthorityClasses.Rank` ladders; time-in-queue is server-measured (`now − RequestedAtUtc`, clamped `[0, 30d]`). Tests cover ordering, exactly-equal-score tie-break (source version → item ref), future-skew→0, ancient→clamp, terminal exclusion. ✔
- **AC2/AC9 (priority-weights knob):** `approval.priority-weights` declared in M1Preview as a `Standard` closed `ApprovalPriorityWeights` record (3 bounded `[0,100]` dimensions); `ValidateApprovalPriorityWeights` rejects wrong-type/out-of-range/NaN/Infinity and every existing validator is guarded against the new value field (closed-schema invariant). Evaluator falls back to `SafeDefaults`. ✔
- **AC3/AC5 (grouping + redaction):** group key is a stable tenant-scoped `sha256:` fingerprint over `(tenant,requester,command,project)` (never `GetHashCode()`); merges only on identical triples, never across tenants; explanation/group refs ride the projector's `SafeSummaryToken` discipline; no-leak tests assert. ✔
- **AC4/AC6/AC7 (batch fan-out, per-item audit, authority):** `ApprovalBatchDecisionPlanner` is pure fan-out to typed single-item `DecideAiActionApproval`/`DecideOutboundApproval` (no batch command, no allowlist change), each carrying its own approval id + expected source version; non-human actors denied before state load; partial authority acts only on authorized items with a safe `insufficient_authority` denial. ✔
- **AC8 (contract spine):** generic transport reused, OpenAPI/client/checksum intentionally unchanged. ✔

### Findings

- **[Med][Fixed] Stale debug-log count** — Story claimed Server.Tests = 607; actual = 617 (the recurring "stale debug-log count" class). Corrected.
- **[Med][Fixed] Overstated batch audit-ref claim** — `BatchDecisionEvidenceRefs` reads `groupKeyFingerprint`/`riskClass`/`authorityRank` from the public command body, but the typed fan-out commands the planner emits never carry them (and can't without an AC8 contract change), so AC7's three batch refs cannot appear in any real fan-out; the audit test only proves the extractor when a hand-built element supplies the fields. Reclassified in the completion note + factory XML doc as a forward-looking, defensively-tested seam; the per-item envelope still carries its own per-item refs, so the NFR46 one-audit-per-item invariant holds. Safe tested code retained (no contract drift introduced).
- **[Low][Noted] Standalone UI component not mounted** — `ChatBotApprovalQueuePriorityView` is a standalone design-contract surface (static `CreateDefault()`), not wired into `GovernedOperations.razor`; the subtask wording said "extend GovernedOperations.razor". This matches the accepted repo convention — the prior-story `ChatBotNotificationRoutingEditor` (7.6) and `ChatBotEscalationPolicyEditor` (7.7) are likewise standalone and unmounted — so it is recorded as a convention note, not a regression. Live host wiring of the scorer/builder/planner into projection + gateway dispatch is deferred the same way Story 7.5's `AdminQueueSummaryProjector` is (zero production callers in `src/`).
- **[Low][Noted] Unused localized keys** — `ApprovalQueuePriorityExplanationLabel` and `ApprovalQueuePriorityBatchRejectAction` are declared and localized (EN/FR) but not rendered by the current view (approve-only). Harmless; left for the batch-reject action when the surface is wired live.

### Reviewer note

The delivered units are correct, deterministic, redaction-safe, and well-tested. The principal residual is integration: prioritization/grouping/batch are pure units pending host wiring (consistent with 7.5–7.7). The audit-ref seam should be the first thing exercised end-to-end when batch dispatch is wired, to make AC7's group/risk/authority refs actually appear on each per-item envelope.

---

### Review pass — 2026-06-11 (QA E2E follow-up). Outcome: Approve (auto-fixes applied).

**Trigger:** the `bmad-qa-generate-e2e-tests` workflow added a new story-7.8 E2E suite after the original 2026-06-02 review. This pass adversarially validates that new artifact and reconciles it with the story doc. The original implementation (commit `676bae0`) is committed, frozen, and an ancestor of HEAD (7.7 builds on it); it was not re-opened.

**Verification performed**

- `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/…csproj --no-restore -m:1 /nr:false` → **Build succeeded, 0 warnings, 0 errors**.
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` → **Total 97, Failed 0, Skipped 0**. `google-chrome` was present, so the three new tests executed through the **real Playwright browser path**, not only the no-browser fixture fallback — the QA summary's claim is independently confirmed.
- Git-vs-File-List cross-check: only two untracked source/artifact files exist (`ApprovalQueuePriorityE2ETests.cs`, `test-summary-story-7.8.md`); no submodule gitlink drift; OpenAPI/`HexalithChatBotClient.g.cs`/checksum still untouched (AC8 honored).
- Spot re-read of `ApprovalPriorityScorer`, `ApprovalQueueItemBuilder`, `ApprovalBatchDecisionPlanner`, `RiskClasses`, `ApprovalPriorityWeights`: unchanged since the 2026-06-02 review and consistent with the documented design.

**New-artifact (E2E test) review**

- The suite mirrors the repo's established E2E convention exactly (per-file nested `BrowserHarness`, `ReadProjectFile`/`ProjectPath` CSS-token fixture, `AssertMetadataOnly`, no-browser fixture fallback, semantic accessible locators, no sleeps) — matching the 7.6 `NotificationRoutingEditorE2ETests` and 7.7 `EscalationPolicyEditorE2ETests`.
- It faithfully encodes the NFR46 UX contract: highest-first priority order (`Critical`/`High`/`Low`), one batch approve action per group, per-item fan-out to `DecideAiActionApproval` (2 commands from 3 items with 1 `insufficient_authority` denial), `auditEnvelopeCount = accepted-item count`, status-focus recovery, safe partial-authority reason reachable, and phone fallback hiding dense controls — all with metadata-only assertions.

**Findings**

- **[Med][Fixed] File List omitted the new E2E test + QA summaries** — the QA follow-up artifacts were real, story-7.8-specific changes absent from the File List (the recurring Epic 7 "inexact File List" class). Added a clearly-provenance-labelled "New — Tests (QA E2E follow-up)" entry and the two `tests/` summaries under "Modified — Tracking"; Change Log updated.
- **[Low][Noted] E2E is fixture-level, not the real component** — like every other E2E suite here, it asserts against a hand-built HTML/JS fixture, so it validates the intended UX contract rather than the live `ChatBotApprovalQueuePriorityView.razor` or the real `ApprovalBatchDecisionPlanner`. The QA summary's "Next Steps" already records the host-integration gap; convention-consistent, not a regression.
- **[Low][Noted] `ApprovalBatchDecisionFanOut` is a UI-event marker, not a server command** — the fixture's `window.__lastApprovalBatch.commandType` labels the batch *gesture*; the per-item commands are correctly the typed `DecideAiActionApproval`. There is intentionally no such server command type (AC4 / completion note 5: pure fan-out, no batch command, no allowlist change). Harmless JS-local label; flagged so the live wiring doesn't mistake it for a real command.

No CRITICAL issues; no production-code changes required. Status remains **done**.

_Reviewer: Jérôme Piquot on 2026-06-11._
