---
baseline_commit: 5dbb31a
---

# Story 7.5: Operational queue management

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As an authorized operator,
I want to view, claim/assign, and prioritize operational queues,
so that review work is triaged efficiently across the tenant.

## Acceptance Criteria

1. Given a human `operations-admin` or `tenant-admin` with `AdminScope.Operate`, when they open operational queue management, then they can view tenant-wide queue rows for ambiguous-association, unresolved-participant, pending-approval, failed-ingestion, failed-attachment, and retryable-operation queues, with each row showing state, age, risk, confidence, assignee, next action, retry count, terminal/non-terminal status, health, freshness timestamp, owner role, and safe item refs without requiring per-project membership. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.5`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR67`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR69`; `src/Hexalith.ChatBot.Contracts/Queries/AdminQueueSummary.cs`]
2. Given the operator lacks per-project authority for a queue item, when item detail or restricted columns would reveal project names, evidence content, candidate evidence, file metadata, mailbox content, audit reasons, provider payloads, prompts, command bodies, raw claims, headers, tokens, or secrets, then the response redacts or omits those fields, preserves safe summary fields, and offers a safe request-access/escalation or open-detail-disabled state without resource-existence leakage. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75b`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md#Current State To Preserve`; `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs`]
3. Given a review item needs human resolution, when an authorized operator claims or assigns it, then the operation is accepted only through the governed command spine, records assignee/reviewer refs, prior assignee, item refs or count, queue ref, reason code, source version, policy snapshot id, correlation id, and UTC timestamp, and does not mutate project-level records such as associations, participants, files, approvals, conversation content, mailbox configuration, tenant policy, service-client grants, command allowlists, outbound drafts, or audit chains. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.5`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR70`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75c`; `src/Hexalith.ChatBot.Contracts/Commands/ExecuteAdminQueueOperation.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`]
4. Given an operator performs retry, requeue, quarantine, dismiss, claim, assign, or priority adjustment on visible queue items, when pre-commit audit is unavailable, then the gateway fails closed and writes no durable queue state. When audit succeeds, metadata-only audit refs include admin role/scope, queue operation, queue ref, affected item refs or item count, assignee/reviewer refs where applicable, reason code, source version, policy snapshot id, redaction decision, correlation, and outcome. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75g`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`; `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`]
5. Given queue filters, sorting, pagination, and prioritization are used, when the operator queries a queue, then server-side filtering supports age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action; sorting/prioritization is deterministic and stable across pages; default page size is no greater than 100; pagination or virtualized-list-with-stable-filters is used; infinite scroll is not used. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.5`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR78`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR27`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Banned or constrained interactions`; `src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingPolicy.cs`]
6. Given queue items are degraded, stale, waiting, blocked, escalation-needed, failed, retryable, terminal, or completed, when the queue surface renders, then each row has one primary next action, finite disabled-action reasons, message-catalog-backed safe status text, runbook-ready diagnostics at least for correlation id, tenant id, mailbox id when applicable, workflow item id, current state, last transition, retry count, failure reason, and next safe action, and completed items are removed or archived without losing audit reconstructability. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR76`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR77`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR39`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR44`; `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`]
7. Given non-human actors, service clients, AI actors, mailbox events, CLI/MCP automation without delegated human operate authority, or admins without `AdminScope.Operate` attempt queue operations, when authorization runs, then they are denied before state load with safe reason codes and no resource-existence leakage. `mailbox-admin`, `policy-admin`, and `compliance-admin` remain unable to operate queues. [Source: `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md#Senior Developer Review (AI)`; `src/Hexalith.ChatBot.Contracts/Enums/AdminScopes.cs`; `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`]
8. Given public commands, queries, DTOs, or generated clients change for operational queue search, row/detail, claim/assign, prioritization, or queue operations, then the OpenAPI contract spine is updated first, `HexalithChatBotClient.g.cs` is regenerated rather than hand-edited, the generated-client checksum is refreshed, and contract/client tests prove schema parity. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md#Current State To Preserve`; `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`; `tests/fixtures/hexalith-chatbot-generated-client.sha256`]
9. Given acceptance coverage runs, then tests prove all six queue families render and filter, server-side pagination defaults and caps at 100, deterministic priority ordering, claim/assign validation, operations-admin/tenant-admin allow, mailbox/policy/compliance admin deny, service/AI denial, per-project redaction, metadata-only audit refs, audit-unavailable fail-closed behavior, queue UI accessibility/no-infinite-scroll behavior, OpenAPI/client drift if public contracts change, and no gateway/audit/admission bypass. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR65-NFR70`]

## Tasks / Subtasks

- [x] Expand operational queue contracts and validators (AC: 1, 2, 5, 6, 8, 9)
  - [x] Extend or replace the thin `AdminQueueSummary` contract with typed, metadata-only operational queue contracts under `src/Hexalith.ChatBot.Contracts/Queries` for queue family, row id/ref, state, age, risk, confidence, assignee, next action, retry count, terminal status, health, freshness, owner role, disabled-action reasons, safe diagnostics, redaction state, page cursor/token, sort, filter, and priority score/explanation.
  - [x] Add finite queue-family values for exactly `ambiguous-association`, `unresolved-participant`, `pending-approval`, `failed-ingestion`, `failed-attachment`, and `retryable-operation`. Do not model queue family as arbitrary free-form strings after trust-boundary parsing.
  - [x] Add server-side query contracts such as `SearchOperationalQueueItems` / `GetOperationalQueueItemDetail` or equivalent names. Include `PageSize`, `PageToken`, stable sort keys, and filter fields for age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action.
  - [x] Validate page size as `1..100`, default to `<=100`, reject unsafe tokens/refs, require UTC time bounds, and make sort/prioritization deterministic with a stable tie-breaker such as item ref plus source version.
  - [x] Keep public contracts metadata-only by default. Restricted detail must be a separate authorized hydration step, not fields leaked through the summary row.
- [x] Add governed claim/assign and priority-operation commands (AC: 3, 4, 7, 8, 9)
  - [x] Extend `ExecuteAdminQueueOperation` only if it remains clean; otherwise add focused commands such as `AssignOperationalQueueItem`, `ClaimOperationalQueueItem`, and `PrioritizeOperationalQueueItems` that implement `IChatBotCommand` and use the same metadata-only discipline.
  - [x] Add finite operations for claim/assign/priority only after deciding whether they belong in `AdminQueueOperation`; preserve existing `retry`, `requeue`, `quarantine`, and `dismiss` semantics from Story 7.1.
  - [x] Require safe metadata: operation id, queue family/ref, item refs or positive item count, assignee/reviewer refs, previous assignee when known, reason code, policy snapshot id, source version, redaction state, correlation id, and UTC command timestamp where the contract owns it.
  - [x] Reject empty reason/policy/redaction/source-version fields, unsafe item refs, mismatched item ref count, unsupported queue family, stale source version, and claim/assign attempts against terminal or completed items.
  - [x] Wire new command types into `ChatBotSpineCommandAllowlist` only after authorization, validation, audit refs, dispatch/projection behavior, and tests exist.
- [x] Extend authorization and audit through existing gateway seams (AC: 3, 4, 7, 9)
  - [x] Reuse `AdminAuthorityEvaluator.HasHumanAdminScope(..., AdminScope.Operate)` for all queue operations. Do not add duplicate role parsing or a queue-operator superuser path.
  - [x] Preserve denial for service/AI actors and non-human surfaces even if they carry `chatbot:tenant-role = tenant-admin` or `operations-admin`.
  - [x] Keep tenant identity from `ChatBotTenantBinding`; never trust tenant ids from queue refs, item refs, route/query params, UI state, page tokens, mailbox ids, project ids, or correlation ids.
  - [x] Extend `ParticipantAuthorizationStage.IsValidAdminQueueOperation` or neighboring validation for claim/assign/priority fields with finite reason codes, safe refs, non-negative source versions, positive affected counts, allowed state transitions, and terminal-state denial.
  - [x] Extend `AuditEnvelopeFactory.AdminEvidenceRefs` with safe refs such as `admin-operation:claim`, `admin-operation:assign`, `admin-operation:prioritize`, `admin-scope:operate`, `admin-queue:<safe-id>`, `queue-family:<safe-id>`, `admin-subject:<safe-item-ref>`, `queue-assignee:<safe-ref>`, `queue-previous-assignee:<safe-ref>`, `admin-item-count:<count>`, `policy-snapshot:<safe-id>`, and `reason:<safe-code>`.
  - [x] Preserve `CommandGateway` pre-commit audit fail-closed behavior. Do not write queue assignment or priority state directly from UI, query controllers, projections, workers, CLI, MCP, service clients, or background tasks.
- [x] Build operational queue projections/read policies from existing sources (AC: 1, 2, 5, 6, 9)
  - [x] Extend `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs`, `AdminQueueSummaryProjector.cs`, and `AdminQueueSummaryReadPolicy.cs` or add sibling read models so all six queue families can be projected from existing workflow state, mailbox health, association, participant, approval, attachment, retry, duplicate, authorization, AI, command, and audit-projection status sources.
  - [x] Preserve summary-safe reads from Story 7.1: queue depth/age/owner/status are tenant-wide safe; project names, evidence, candidate detail, mailbox bodies/subjects/headers, file metadata, audit reasons, provider payloads, prompts, command bodies, raw claims, tokens, and secrets are not.
  - [x] Add per-item redaction state and disabled-action reasons. Restricted detail should render safe placeholders and next actions, not disappear in ways that make authorization failures distinguishable from safe-not-found.
  - [x] Add claim/assignment and priority projection state with optimistic concurrency via source version. Conflicting claims should fail with a safe stale-data reason and leave focus on the row/status in UI.
  - [x] Ensure retry/requeue/quarantine/dismiss remain queue-level lifecycle operations. They may update queue item state or schedule retry/requeue intent, but must not directly decide associations, mutate participants, approve/reject AI actions, store/delete files, change mailbox config, or edit tenant policy.
- [x] Add or extend the operational queue UI surface (AC: 1, 2, 5, 6, 9)
  - [x] Reuse existing FrontComposer/Fluent UI governed components and `src/Hexalith.ChatBot.UI` patterns. Do not introduce another design system, a marketing page, raw JSON queue browser, nested-card dashboard, or hover-only critical actions.
  - [x] Render a dense operational work surface with queue family tabs or segmented controls, filter controls, sort/prioritization controls, result count, pagination, queue rows, row status, one primary next action, secondary/destructive grouped actions, and a safe detail panel/drawer where authorized.
  - [x] Use `ChatBotQueueLoadingPolicy`: pagination or virtualized list with stable filters is allowed; infinite scroll is not.
  - [x] Use `ChatBotBlockedState`, `ChatBotStatusBanner`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, and existing localization patterns where applicable. Add `ChatBotUiTextKey`, `SharedResource.resx`, and `SharedResource.fr.resx` entries for any visible text.
  - [x] On action success, move focus to row/surface status; on validation/audit/authorization failure, keep focus in the row or review panel with the safe reason reachable. Retry controls must state duplicate-safety and retry count.
  - [x] On phone, provide readable queue summary/status, filters summary, row metadata, and safe actions if practical; dense triage may degrade to read-only summary only if a reachable explanation and path to full workflow remain.
- [x] Update public contract spine if queue surfaces are public (AC: 8, 9)
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` before generated client output when new queue query/command DTOs are public.
  - [x] Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit the generated `.g.cs`.
  - [x] Refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` and add/extend client schema parity tests.
- [x] Add focused tests (AC: all)
  - [x] Contract tests near `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` for queue family tokens, row/search/page/filter/sort/priority validation, claim/assign command validation, secret-bearing property bans, and OpenAPI schema parity if public.
  - [x] Server authorization tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs` for operations-admin/tenant-admin allow, mailbox/policy/compliance admin deny, service/AI/non-human deny, invalid payload deny, stale/terminal item deny, and safe reason codes.
  - [x] Gateway/audit tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` for audit-unavailable fail-closed queue claim/assign/priority and metadata-only audit refs.
  - [x] Projection/read-policy tests near `tests/Hexalith.ChatBot.Server.Tests/Projections/AdminQueueSummaryProjectorTests.cs` for all six queue families, summary-safe redaction, per-project detail denial, stable priority ordering, pagination, server-side filters, and bounded page size.
  - [x] UI/bUnit tests under `tests/Hexalith.ChatBot.UI.Tests` for queue tabs/filters/sort/page controls, no infinite scroll default, focus/status behavior, disabled explanations, row reflow on small screens, localization, and absence of restricted markers.
  - [x] Conformance/architecture/client tests if new public surfaces, command/query shapes, actor isolation behavior, or module boundaries change.

## Dev Notes

### Scope Boundaries

- Story 7.5 implements operational queue viewing, filtering/sorting/pagination/prioritization, and claim/assign workflow for review items. It also extends queue-level operations only as needed for triage.
- It does not implement notification routing/delivery (7.6), escalation policy configuration (7.7), approval queue grouping/batch policy (7.8), notification throttling/digests/backlog observables (7.9-7.11), disable/quarantine/rate-limit governance controls (7.12-7.26), command allowlist v1/lifecycle completion (7.27), or M2 operational dashboards/SLO publication (Epic 8).
- `operations-admin` can see tenant-wide summary-safe queue information and operate queue lifecycle/assignment metadata. It is not a project-content, mailbox-content, file-content, raw-audit, policy, mailbox, or compliance superuser.
- Per-item detail that reveals project/evidence/file/mailbox/audit content must still pass the existing project/compliance authorization and redaction paths.
- Claim/assign/prioritize changes are queue metadata. They must not become hidden association, participant, file, approval, mailbox, policy, outbound, or audit mutations.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Contracts/Enums/AdminRole.cs`, `AdminRoles.cs`, `AdminScope.cs`, and `AdminScopes.cs` - finite admin role/scope model. `OperationsAdmin` maps to `SeeOnly`, `Operate`, and `AuditObligation`; preserve that subset.
- `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` - human-only admin scope evaluation. Use this for operate-scope checks.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - central command authorization and existing `ExecuteAdminQueueOperation` validation. Extend here rather than authorizing in UI, projections, CLI, MCP, workers, or service-client code.
- `src/Hexalith.ChatBot.Contracts/Commands/ExecuteAdminQueueOperation.cs`, `AdminQueueOperation.cs`, `AdminQueueOperations.cs`, and `src/Hexalith.ChatBot.Contracts/Identities/AdminOperationReference.cs` - existing queue-operation and audit-ref foundation.
- `src/Hexalith.ChatBot.Contracts/Queries/AdminQueueSummary.cs` and `GetAdminQueueSummary.cs` - current summary-safe queue read contracts. Expand carefully or add sibling contracts without weakening existing redaction.
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs`, `AdminQueueSummaryProjector.cs`, `AdminQueueSummaryReadDecision.cs`, and `AdminQueueSummaryReadPolicy.cs` - current tenant-wide summary projection and audit-threshold read policy.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`, `AuditMetadata.cs`, `ChatBotStateWritingPathInventory.cs`, and `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - metadata-only audit refs and pre-commit fail-closed behavior.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` - add new command types only after validator/audit/test coverage exists.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotDisabledActionReasons.cs`, `ChatBotMessageCodes.cs`, and `ChatBotAuthorizationReasonCodes.cs` - finite safe status, disabled-action, denial, and next-action text.
- `src/Hexalith.ChatBot.UI/State/GovernedOperations/GovernedOperationsState.cs`, `src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingPolicy.cs`, `ChatBotQueueLoadingContract.cs`, and governed UI primitives under `src/Hexalith.ChatBot.UI/Components/Governed/` - UI state/policy and component anchors for queue operation surfaces.
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and `tests/fixtures/hexalith-chatbot-generated-client.sha256` - update/regenerate only if public queue DTOs change.

### Current State To Preserve

- Story 7.1 fixed role/scope overgrant: only `tenant-admin` and `operations-admin` can operate queues. `mailbox-admin`, `policy-admin`, and `compliance-admin` must not gain `AdminScope.Operate`.
- `ParticipantAuthorizationStage.IsValidAdminQueueOperation` currently requires `ScopeUsed == AdminScope.Operate`, safe operation/queue/policy/redaction tokens, allowed reason codes, non-negative source version, positive item count, safe item refs, and matching ref count when refs are supplied. Do not relax these checks for new operations.
- `AdminQueueSummaryProjector` intentionally strips project/evidence/file/audit/mailbox/candidate fields from summary output. New queue rows must keep this metadata-only default.
- `AdminQueueSummaryReadPolicy` allows human see-only admin summary reads without project membership and fails closed above the audit threshold when audit is unavailable. Preserve this behavior for richer queue reads.
- `AuditEnvelopeFactory.AdminEvidenceRefs` already emits safe admin role/scope/queue/item-count/item refs for `ExecuteAdminQueueOperation`. Extend refs rather than emitting raw queue row/detail payloads.
- `CommandGateway` suppresses dispatch when pre-commit audit cannot be written. Queue claim/assign/priority operations must reuse this path.
- Existing root submodule policy applies: initialize/update only root `.gitmodules` submodules and never use recursive submodule commands.

### Architecture Guardrails

- Contracts belong in `src/Hexalith.ChatBot.Contracts`; generated client output belongs only in `src/Hexalith.ChatBot.Client/Generated`; server authorization belongs in `src/Hexalith.ChatBot.Server/Gateway/Stages` or `src/Hexalith.ChatBot.Server/Governance/Admin`; queue projections/read policies belong in `src/Hexalith.ChatBot.Server/Projections`; audit refs stay in `src/Hexalith.ChatBot.Server/Audit`; UI work belongs in `src/Hexalith.ChatBot.UI`.
- Every queue state mutation follows `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> pre-commit-audit -> EventStore execute/publish/projection -> post-commit-audit`. Do not add a queue direct-write path.
- Tenant IDs come from authenticated gateway binding. Queue refs, item refs, page tokens, project filters, mailbox filters, route/query params, UI state, and correlation ids are comparison inputs only.
- Use typed records/enums and finite token validators. Avoid raw JSON filters, SQL-like query strings, delimited blob fields, and user-provided sort expressions.
- Prioritization must be deterministic, explainable, and bounded. Priority is triage metadata, not authority to bypass approval, association, mailbox, project, or audit checks.
- Time and age fields use server-side UTC timestamps; tenant-local formatting happens only at presentation boundaries.
- If public OpenAPI/client shapes change, update the OpenAPI spine, regenerate the client, refresh the checksum, and include client tests.

### UX Guardrails

- Operational queue management is a dense admin work surface. Keep it scannable and utilitarian; no landing page, no decorative nested cards, no raw JSON browser, and no hover-only critical actions.
- Queue rows show evidence/risk/status/actor/timestamp in consistent order with other review surfaces. Plain-language summaries precede raw IDs; IDs remain available as metadata.
- Each row has one primary next action. Secondary/destructive actions are grouped after the primary decision and require reachable disabled-action explanations when unavailable.
- Filters show a visible active-filter summary and result count. Pagination controls must be keyboard reachable and labelled.
- No infinite scroll for operational queues. Use pagination or virtualized list behavior with stable filters.
- Status updates for claim/assign/retry/requeue/quarantine/dismiss move focus to success status or error summary. Rejected/blocked actions keep focus in the row or panel with the safe reason reachable.
- Dense tables reflow to labelled rows on small screens without dropping actor, risk, state, confidence, next action, retry count, safe recovery reason, or labels.
- English and French visible text must use existing localization patterns. Stable machine codes, reason codes, command names, and correlation IDs remain untranslated.

### Previous Story Intelligence

- Story 7.1 established the bounded admin role/scope model, the initial admin queue summary/operation contracts, metadata-only admin refs, and fail-closed audit behavior. Its review fixed overgrant, empty audit-obligation fields, and unsafe affected refs; do not repeat those defects for claim/assign/priority.
- Story 7.2 established closed schema validation, known schema versions, safe token/fingerprint checks, OpenAPI/client regeneration, S5 UI accessibility, and metadata-only audit refs. Reuse its public-contract workflow if queue DTOs are public.
- Story 7.3 established metadata-only mailbox configuration/health and protected provider material. Queue filters and rows may include safe mailbox refs/status only, never mailbox content or raw provider data.
- Story 7.4 established compliance audit reads and per-project redaction/escalation. Operational queue detail must not bypass those redaction and escalation rules.
- Earlier Epic 2-6 stories established duplicate/retry/failure states, association review, approval review, AI action outcomes, outbound approval, and cross-surface parity. Operational queues should aggregate those existing workflow states instead of creating a separate workflow truth source.

### Latest Technical Specifics

- No external version research is required for implementation. Use the repo-pinned stack and do not upgrade packages as part of this story: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, Blazor/FrontComposer, Fluent UI v5 RC, Fluxor, and existing OpenAPI/generated-client tooling.
- Do not change target frameworks, Aspire/Dapr topology, Fluent UI, Fluxor, NSwag/client generation tooling, MCP SDK, Graph permission posture, WORM audit backing assumptions, or submodule pointers unless a compile-time contract regeneration command requires generated client output.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` if queue UI/components/design contracts change.
  - `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` if the operational queue workflow is exposed end to end.
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` if actor isolation, queue command/query surfaces, or cross-surface behavior changes.
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` if new boundaries are introduced.
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` if OpenAPI/generated client changes.
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, metadata-only fixtures, and root-level submodule policy.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`, including Epic 7, Story 7.5, and adjacent Story 7.1-7.8 queue/admin context.
- Loaded `prd_content` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`, especially FR67-FR80, FR75b/FR75c/FR75g, NFR24, NFR27, NFR37-NFR44, NFR60-NFR70, Shared Command Pipeline, and Idempotency Keys.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway, two-phase audit, fail-closed invariant, projections/queue mapping, project structure, redaction, integration boundaries, and testing strategy.
- Loaded `ux_content` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`, especially Operational Queues, queue rows, no infinite scroll, filter/sort behavior, focus model, cognitive-load guardrails, responsive behavior, semantic status, and FrontComposer/Fluent UI design spine.
- Loaded persistent project-context facts from sibling `project-context.md` files, with relevant constraints from Hexalith.EventStore, Hexalith.FrontComposer, Hexalith.Tenants, Hexalith.Folders, Hexalith.Memories, and Hexalith.Commons.
- Loaded previous story intelligence from `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md`, `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md`, `_bmad-output/implementation-artifacts/7-3-mailbox-admin-scope-and-mailbox-configuration.md`, and `_bmad-output/implementation-artifacts/7-4-compliance-admin-scope.md`.
- Inspected current source anchors: admin roles/scopes, admin authority evaluation, participant authorization, queue operation contracts, admin queue summary contracts, admin queue projections/read policy, audit envelope factory, queue loading UI policy, governed operations state, admin contract tests, gateway authorization tests, command gateway audit tests, and admin queue projection tests.
- Reviewed recent git history: `5dbb31a feat(story-7.4): Add compliance admin scope`, `a2da0c5 feat(story-7.3): Add mailbox admin configuration`, `c6bcd1a feat(story-7.2): Add tenant policy schema administration`, `1745611 feat(story-7.1): Add bounded tenant admin permissions`, and `2297fe9 feat(story-6.5): Disambiguate delegated and external senders`.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 7 and Story 7.5 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR67-FR80, FR75b/FR75c/FR75g, NFR24, NFR27, NFR37-NFR44, NFR60-NFR70.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Shared Command Pipeline and Idempotency Keys.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, audit model, projections, project structure, redaction, testing strategy.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - Operational Queues, queue rows, no infinite scroll, focus, responsive behavior.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - FrontComposer/Fluent UI design spine, semantic status, queue row component.
- `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md` - bounded admin roles/scopes, queue summary/operation foundation, review fixes.
- `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md` - safe-token, OpenAPI/client, audit, and UI accessibility patterns.
- `_bmad-output/implementation-artifacts/7-3-mailbox-admin-scope-and-mailbox-configuration.md` - mailbox metadata-only and provider-leakage protections.
- `_bmad-output/implementation-artifacts/7-4-compliance-admin-scope.md` - compliance read/redaction/escalation and audit-read patterns.
- `src/Hexalith.ChatBot.Contracts/Commands/ExecuteAdminQueueOperation.cs` - current queue operation command.
- `src/Hexalith.ChatBot.Contracts/Enums/AdminScopes.cs` - operations-admin scope mapping.
- `src/Hexalith.ChatBot.Contracts/Queries/AdminQueueSummary.cs` - current summary-safe queue response.
- `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` - human admin scope evaluation.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - central authorization and queue operation validation.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - metadata-only admin audit refs.
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs` - summary-safe queue projection.
- `src/Hexalith.ChatBot.UI/Design/ChatBotQueueLoadingPolicy.cs` - no-infinite-scroll queue loading policy.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-02T08:00:43+02:00 - Build and validation completed:
  `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`;
  Contracts, Server, UI, UI.E2E, Conformance, Architecture, AppHost, Aspire, CLI, Client, MCP, ServiceDefaults, Testing, Workers, and Integration xUnit v3 runners all passed with `-parallel none`.
  Integration runner reported two expected Tier-3 Aspire E2E skips gated by Docker/DAPR environment flags.
- 2026-06-02T08:15:07+02:00 - Senior review auto-fixes completed:
  `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`;
  Contracts, Server, UI, UI.E2E, Conformance, Architecture, and Client xUnit v3 runners all passed with `-parallel none`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added finite operational queue family and sort contracts plus metadata-only search, detail, row, diagnostics, paging, filter, and validation contracts.
- Extended `ExecuteAdminQueueOperation` compatibly for claim/assign/prioritize queue metadata while preserving existing retry/requeue/quarantine/dismiss construction and allowlist behavior.
- Extended gateway validation through `ParticipantAuthorizationStage` for human `AdminScope.Operate`, safe refs, UTC command timestamps, reason codes, non-negative source versions, positive affected counts, and terminal/completed denial.
- Extended metadata-only admin audit refs for queue family, assignee/reviewer/previous assignee, policy snapshot, reason, redaction, source version, item refs, and item count.
- Extended queue projection models to emit all six queue families, deterministic priority ordering, bounded paging, server-side filters, metadata-only rows, redaction state, disabled action reasons, diagnostics, and safe restricted detail placeholders.
- Added the governed operational queue UI surface using existing Fluent/governed components, pagination, family controls, visible filters/sort/result count, safe rows, primary/secondary actions, disabled detail reasons, duplicate-safety status, and English/French localization.
- OpenAPI/generated client/checksum were intentionally unchanged because this repository currently has no public admin queue endpoint/schema in `hexalith.chatbot.v1.yaml`; the generic command submission endpoint remains the public transport spine.

### Senior Developer Review (AI)

Review date: 2026-06-02T08:15:07+02:00

Outcome: approved after automatic fixes. No critical issues remain.

Fixed findings:

- [HIGH] `AdminQueueSummaryProjector.Search` validated `PageToken` but did not apply it, so page 2 repeated page 1 and AC 5 pagination was only partially implemented. Fixed by applying the token after deterministic ordering, returning an empty page for unknown safe tokens, preserving total filtered count, and adding a regression assertion for the second page.
- [MEDIUM] `StableFilterFingerprint` used `OperationalQueueFilter.GetHashCode()`, which is process-dependent and not a stable filter identity. Fixed by hashing a deterministic canonical representation with SHA-256 and asserting the stable prefix in projection tests.
- [MEDIUM] Git/story File List drift: `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`, `Hexalith.Tenants`, `_bmad-output/implementation-artifacts/tests/test-summary.md`, and `_bmad-output/story-automator/orchestration-4-20260601-145742.md` had git changes but were not listed. Fixed by adding them to the File List.

Checklist notes:

- Story status was reviewable before review and is now done.
- Story context, planning artifacts, architecture, UX references, previous Story 7.1-7.4 intelligence, and repo-pinned stack notes were available in the story and planning artifacts. No external version research was required because implementation used the pinned repo stack.
- ACs, completed tasks, File List, tests, code quality, authorization, redaction, audit, pagination, and public-client claims were cross-checked against implementation and tests.
- No OpenAPI/generated-client update was required because no public admin queue endpoint/schema was added; client and architecture tests passed.

### File List

- `_bmad-output/implementation-artifacts/7-5-operational-queue-management.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-4-20260601-145742.md`
- `Hexalith.Tenants`
- `src/Hexalith.ChatBot.Contracts/Commands/ExecuteAdminQueueOperation.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminQueueOperation.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminQueueOperations.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/OperationalQueueFamilies.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/OperationalQueueFamily.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/OperationalQueueSortKey.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/OperationalQueueContracts.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs`
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Components/_Imports.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AdminQueueSummaryProjectorTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotInteractionGuardrailContractTests.cs`

### Change Log

- 2026-06-02T08:00:43+02:00 - Implemented operational queue management contracts, queue operation validation/audit/projection behavior, governed UI surface, and focused acceptance coverage. Story moved to review.
- 2026-06-02T08:15:07+02:00 - Senior review auto-fixed operational queue pagination token handling, deterministic filter fingerprinting, projection regression coverage, and File List drift. Story moved to done.
