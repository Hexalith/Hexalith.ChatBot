---
baseline_commit: 2297fe95b4106dddd41ddae3bcddf5984e240b61
---

# Story 7.1: Tenant-admin permission model and bounded scopes

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As a security owner,
I want a bounded tenant-admin model with see-only vs operate scopes and an audit obligation,
so that admins get the dashboards they need without a bypass to authorization or audit.

## Acceptance Criteria

1. Given admin roles are assigned, when ChatBot resolves tenant administration authority, then `tenant-admin` holds the union of FR75b-FR75g scopes and finer roles (`mailbox-admin`, `policy-admin`, `compliance-admin`, `operations-admin`) hold proper subsets. Admin assignment is a security-sensitive operation, must be audited, and must be denied for service clients and AI actors even if they carry tenant-admin-looking claims. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.1`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Tenant-Admin Permission Model (FR75a-FR75g, M1)`]
2. Given an admin has see-only scope, when they read operational queue summaries, health/status enums, or aggregate metrics across tenant projects, then the response contains only summary-safe fields and does not require per-project membership. Per-item detail, including project name, evidence content, file metadata, audit reasons, mailbox content, or candidate evidence, still requires project authority and must be redacted or omitted otherwise. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.1`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#RBAC Matrix`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`]
3. Given an admin has operate scope, when they perform queue-level operations (`retry`, `requeue`, `quarantine`, `dismiss`) on visible queue items, then the operation records admin identity, scope used, affected item refs or count, queue, reason, timestamp, and policy snapshot. The operation must not mutate project-level records such as associations, files, approvals, conversation content, or project membership. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.1`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75c`]
4. Given any admin operation is executed, including read-only dashboard access above the configured aggregation audit threshold, when audit is required, then the operation emits an audit event with admin identity, actor type, scope, subject refs, queue/resource refs, timestamp, correlation, redaction decision, and outcome. There must be no skip-audit path, and `tenant-admin` must not bypass NFR15a or NFR50/NFR50a. Audit-unavailable handling for audited mutations must fail closed through the existing gateway/audit seam. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.1`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR50`]
5. Given service clients, AI actors, mailbox events, CLI automation, or MCP tools attempt admin assignment or admin mutations, when authorization runs, then they are denied unless a future story defines an explicit delegated human flow for a non-admin operation. Service-client grants must not inherit UI roles, and CLI/MCP must use the same backend authorization and redaction behavior as UI/API. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#Service Client Permissions`; `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md#Current State To Preserve`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`]
6. Given acceptance coverage runs, then tests prove finite admin role/scope tokens, role-to-scope mapping, tenant-admin union behavior, finer-role subset behavior, service-client/AI denial for admin assignment and admin mutations, see-only summary redaction, per-item detail authorization preservation, queue-operation scope checks, metadata-only audit refs, audit-unavailable fail-closed behavior for audited mutations, and no new gateway/audit/admission bypass. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs`]

## Tasks / Subtasks

- [x] Add finite admin role/scope contracts and mappings (AC: 1, 2, 3, 5, 6)
  - [x] Add structured contract types for admin roles and scopes, using exact wire tokens: `tenant-admin`, `mailbox-admin`, `policy-admin`, `compliance-admin`, `operations-admin`, plus finite scope tokens for see-only, operate, policy, mailbox, compliance, and audit obligation behavior.
  - [x] Model `tenant-admin` as the union of every admin scope and finer roles as proper subsets. Do not represent admin authority as free-form strings beyond tolerant parsing at trust boundaries.
  - [x] Add safe metadata contracts for admin operation refs: actor/admin id, actor type, scope used, queue/resource ref, item refs or item count, reason token, policy snapshot id, timestamp/source version, and redaction state.
  - [x] Do not include project names, evidence content, file metadata, audit reasons, mailbox body/subject, provider payloads, raw claims, raw headers, bearer tokens, or secrets in summary-safe contracts.
- [x] Implement server-owned admin authorization helpers without adding a second gateway (AC: 1, 3, 5, 6)
  - [x] Reuse `ParticipantAuthorizationStage` claim constants for actor type and tenant role where possible; factor helper logic under `src/Hexalith.ChatBot.Server/Governance/Admin/` only if it keeps the gateway stage simpler.
  - [x] Deny admin assignment and admin mutations for `service` and `ai` actor types even when they carry `chatbot:tenant-role = tenant-admin`.
  - [x] Preserve the existing positive human tenant-admin path for security-sensitive threshold policy mutation, and generalize the check instead of duplicating special cases per command.
  - [x] Keep tenant identity from `ChatBotTenantBinding`; command bodies, query parameters, queue item refs, and provider metadata must not establish tenant scope.
- [x] Add bounded admin operation/query surfaces needed for Story 7.1 only (AC: 2, 3, 4, 6)
  - [x] Add minimal DTOs/commands/queries for admin queue summary reads and queue-level operations if they do not already exist; keep them summary/operation focused so Story 7.5 can build the full operational queue management workflow.
  - [x] For see-only reads, expose queue summary, health/status enum, aggregate counts/age/owner class, and safe item refs only. Per-item details must call the existing project authorization path before revealing project/evidence/file/audit detail.
  - [x] For operate scope, allow only queue-level `retry`, `requeue`, `quarantine`, and `dismiss`; do not mutate association records, project files, approval records, conversation content, tenant policy, mailbox configuration, service-client grants, command allowlists, or outbound channel state in this story.
  - [x] Keep disabled/denied action reasons finite and message-catalog backed; prefer existing `authorization_denied`, `insufficient-authority`, `policy-blocked`, and `dependency-degraded` patterns where appropriate.
- [x] Wire admin audit evidence through the existing audit seam (AC: 1, 3, 4, 6)
  - [x] Extend `AuditEnvelopeFactory` metadata refs with finite values such as `admin-role:<role>`, `admin-scope:<scope>`, `admin-operation:<operation>`, `admin-queue:<queue-ref>`, `admin-item-count:<count>`, `admin-subject:<safe-ref>`, and `policy-snapshot:<id>`.
  - [x] Preserve `ChatBotStateWritingPathInventory.RequiredAuditCommitSeam = "IAuditWriter.RecordPreCommitAsync"` and the existing pre-commit fail-closed path in `CommandGateway`.
  - [x] For audited admin mutations, prove audit writer unavailable returns typed audit unavailable, writes no durable state, queues replay intent where the gateway already does so, and emits an operator alert.
  - [x] For audited admin reads above the aggregation threshold, do not return unrestricted data if the audit obligation cannot be met; return a typed, redacted failure or no data according to existing problem-detail conventions.
- [x] Preserve scope boundaries for later Epic 7 stories (AC: all)
  - [x] Do not implement the Tenant Policy Schema editor, policy-admin two-person approval workflow, mailbox configuration UI, provider credential management, compliance investigation surface, retention editor, notification routing, escalation policy, approval prioritization, throttling/digests, backlog alerts, rubber-stamp observable, command allowlist v1, or lifecycle-completion work.
  - [x] Do not add a visual admin dashboard unless a minimal projection/query test surface requires it. UI component work belongs to later admin operational stories.
  - [x] Do not broaden service-client grants, AI actor permissions, mailbox worker privileges, Microsoft Graph permissions, or CLI/MCP command surfaces beyond shared contract coverage needed for this story.
  - [x] Do not update package versions, target frameworks, Aspire/Dapr configuration, submodule pointers, or root/nested submodules.
- [x] Add focused tests (AC: all)
  - [x] Contract tests for role/scope wire tokens, union/subset mapping, optional/missing fields, tolerant parse failures, and safe serialization without secret-bearing names.
  - [x] Server authorization tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs` proving human tenant-admin allow, finer-role allow/deny by scope, service-client/AI denial, and tenant mismatch denial.
  - [x] Gateway/audit tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` proving audited admin mutations fail closed when audit is unavailable and do not dispatch durable state.
  - [x] Projection/query tests proving see-only summaries do not leak project/evidence/file/audit detail without per-project authority.
  - [x] Architecture tests if new public/internal boundaries are introduced; UI/CLI/MCP/workers must not reference server governance internals directly.

## Dev Notes

### Scope Boundaries

- Story 7.1 is the Epic 7 foundation: bounded admin roles/scopes, no admin/debug bypass, and audit obligation. It is not the complete operational dashboard or policy editor.
- The tenant admin is explicitly not a superuser. `tenant-admin` grants the union of admin scopes, but it does not grant project-content access, project membership, evidence visibility, mailbox content visibility, or audit-detail visibility unless the actor also has the project/compliance authority required by existing paths.
- Keep see-only and operate scopes separate. Summary reads are safe aggregate operational views; operate scope is limited to queue-level lifecycle operations and cannot directly mutate project artifacts.
- Admin assignment is security-sensitive and human-only. Service clients and AI actors must not assign roles or perform admin mutations, even with broad grants or tenant-admin-looking claims.
- Audit is part of the authorization contract. Do not add an admin path that writes durable state outside `CommandGateway`, `IAuditWriter.RecordPreCommitAsync`, and existing replay/alert behavior.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - existing actor-type, tenant-role, human tenant-admin, service-client grant, project-read, and threshold-policy authorization logic.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs` - current human tenant-admin positive path and service-client denial pattern for security-sensitive tenant policy mutation.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ServiceClientGrantValidator.cs` and `src/Hexalith.ChatBot.Contracts/Identities/ServiceClientGrant.cs` - service clients must stay explicitly scoped and must not inherit human UI roles.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - append metadata-only admin evidence refs here; keep refs safe and finite.
- `src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs` - NFR15a path inventory and required audit seam; do not add a new state-writing path without inventory/test coverage.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - existing pre-commit audit unavailable behavior, replay intent, operator alert, and dispatch suppression.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotDisabledActionReasons.cs`, and `ChatBotMessageCodes.cs` - finite safe denial/problem text.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - audit-unavailable fail-closed patterns for command execution, association, correction, and mailbox intake.
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationActorMatrix.cs` - existing tenant-admin persona and cross-tenant actor isolation harness.

### Current State To Preserve

- `ParticipantAuthorizationStage` already denies service clients with tenant-admin-looking claims for `SetAssociationConfidenceThresholds`. Generalize that safety rule; do not weaken it.
- `CommandGateway` already suppresses dispatch when pre-commit audit cannot be written. Admin mutations must reuse this instead of writing directly to stores/projections.
- Surface adapters should continue to use generated client contracts/shared backend outcomes. Do not duplicate admin authorization in UI, CLI, MCP, or workers.
- Existing redaction rules make authorization denied and safe-not-found indistinguishable where required. Admin summary/detail separation must preserve this behavior.
- Existing root submodule policy applies: initialize/update only root `.gitmodules` submodules and never use recursive submodule commands.

### Architecture Guardrails

- Contracts belong in `src/Hexalith.ChatBot.Contracts`; server authorization/evaluator logic belongs in `src/Hexalith.ChatBot.Server/Gateway/Stages` or `src/Hexalith.ChatBot.Server/Governance/Admin`; audit evidence remains in `src/Hexalith.ChatBot.Server/Audit`; queue projections/views stay in `src/Hexalith.ChatBot.Server/Projections` if needed.
- Use records/enums/finite tokens, not delimited strings or ad hoc dictionaries, for role/scope/operation results.
- Tenant IDs must come from authenticated gateway binding. Never trust request body tenant ids or queue item refs as authorization proof.
- Treat missing, ambiguous, or invalid admin role/scope claims as deny-by-default. Unknown roles do not imply partial access.
- If public OpenAPI/client shapes change, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.

### Previous Story Intelligence

- Story 5.1 established service-client identities and proved service clients/AI actors cannot mutate security-sensitive tenant policy or admin-assignment-style operations. Preserve that invariant.
- Story 5.4 established parity-by-construction through `IChatBotClient`; admin behavior must not split across UI/CLI/MCP implementations.
- Stories 6.1-6.5 added outbound authority, approval, authenticity, and external-sender governance while preserving metadata-only diagnostics. Admin audit refs must follow the same safe token discipline.
- Story 6.5 added public contract/OpenAPI/client update patterns and generated-client checksum handling. Reuse that workflow only if public admin DTOs change.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` if actor isolation or cross-surface admin behavior changes.
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` if new boundaries are introduced.
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` if OpenAPI/generated client changes.
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Keep xUnit v3, Shouldly, NSubstitute/fakes, central package management, nullable, warnings-as-errors, and `net10.0`.

### Discovery Results

- Loaded Story 7.1 source acceptance criteria from `_bmad-output/planning-artifacts/epics.md`.
- Loaded PRD role/scope detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially RBAC Matrix, Service Client Permissions, FR75a-FR75g, NFR15a, NFR50, and NFR50a.
- Loaded architecture guidance from `_bmad-output/planning-artifacts/architecture.md`, especially bounded admin/governance mapping, fail-closed invariant, audit model, command/gateway boundaries, and testing strategy.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md`, `_bmad-output/implementation-artifacts/6-1-sender-authority-classes-and-m365-mapping.md`, and `_bmad-output/implementation-artifacts/6-5-on-behalf-of-disambiguation-and-external-sender-posture.md`.
- Inspected current implementation anchors: `ParticipantAuthorizationStage`, `AssociationThresholdAuthorizationTests`, `AuditEnvelopeFactory`, `ChatBotStateWritingPathInventory`, service-client grant validation, message catalog, and gateway audit-unavailable tests.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 7 and Story 7.1 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - RBAC Matrix, Service Client Permissions, FR75a-FR75g, NFR2, NFR15a, NFR50, NFR50a.
- `_bmad-output/planning-artifacts/architecture.md` - admin/governance mapping, fail-closed invariant, audit model, project structure, testing strategy.
- `_bmad-output/implementation-artifacts/5-1-service-client-identities-and-scoped-grants.md` - service-client/admin-mutation denial invariant.
- `_bmad-output/implementation-artifacts/6-5-on-behalf-of-disambiguation-and-external-sender-posture.md` - latest contract/OpenAPI/client/audit metadata pattern.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - current role/actor authorization stage.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - audit evidence ref expansion point.
- `src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs` - NFR15a path inventory.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs` - tenant-admin human and service-client denial pattern.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - pre-commit audit unavailable fail-closed tests.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 147 tests.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 525 tests.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 66 tests.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 37 tests.
- Code review auto-fix validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- Code review auto-fix validation: `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 147 tests.
- Code review auto-fix validation: `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 527 tests.
- Code review auto-fix validation: `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 75 tests.
- Code review auto-fix validation: `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 37 tests.
- 2026-06-11 dev-story validation rerun: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- 2026-06-11 dev-story validation rerun: contracts/server/conformance/architecture in-process xUnit runners passed: 480, 1565, 93, and 39 tests respectively.
- 2026-06-11 full compiled ChatBot regression rerun passed: 2580 total tests, 2578 passed, 2 Tier-3 Aspire/DAPR tests skipped by guard, 0 failed.

### Completion Notes List

- Added finite admin role, scope, and queue-operation contracts with tolerant wire parsing and tenant-admin union/finer-role subset mapping.
- Added metadata-only admin operation, assignment, and queue summary contracts that omit project names, evidence content, file metadata, audit detail, mailbox content, raw claims, headers, and secrets.
- Centralized server admin authority evaluation under `Governance/Admin`, reusing existing gateway claims and denying service/AI actors with tenant-admin-looking claims for admin assignment and admin mutations.
- Generalized threshold policy authorization to policy-scope human admins while preserving tenant-admin positive behavior.
- Added bounded queue operation authorization for human operate-scope admins only; no UI, policy editor, mailbox configuration, lifecycle-completion, or broader adapter/service-client permissions were added.
- Extended audit metadata refs for admin roles/scopes/operations/queue/item-count/subjects/policy snapshots through the existing `AuditEnvelopeFactory` and verified pre-commit audit-unavailable fail-closed behavior.
- Added summary-safe queue read projection/policy coverage, including see-only reads without project membership and fail-closed audit-threshold behavior when audit is unavailable.
- 2026-06-11 dev-story rerun found no unchecked tasks or implementation gaps; story and sprint tracker were already `done`, so no source changes or checkbox updates were required.

### Senior Developer Review (AI)

**Review outcome:** Approved after automatic fixes. No critical issues remain.

**Findings fixed:**

- [HIGH] `mailbox-admin` was granted `AdminScope.Operate`, which allowed queue-level retry/requeue/quarantine/dismiss operations even though the bounded role is mailbox configuration only. Removed operate scope from `mailbox-admin` and added contract/authorization tests proving only tenant-admin and operations-admin can execute queue operations.
- [HIGH] Admin assignment and queue mutation validation accepted empty audit-obligation fields (`ReasonCode`, `PolicySnapshotId`, `RedactionState`, and negative source versions). Tightened gateway validation so audited admin mutations cannot enter the command spine without required audit metadata.
- [HIGH] Queue mutations could be admitted with no affected refs/count or with unsafe item refs that the audit layer would omit. The gateway now requires positive affected count, matching item refs when provided, safe metadata tokens, and finite queue reason codes.

**Validation notes:**

- Story acceptance criteria were cross-checked against `AdminRoles`, `AdminScopes`, `AdminAuthorityEvaluator`, `ParticipantAuthorizationStage`, `AuditEnvelopeFactory`, `AdminQueueSummaryProjector`, `AdminQueueSummaryReadPolicy`, and focused tests.
- Git/story discrepancy review found unrelated dirty `Hexalith.Tenants`; it was left untouched per run guard.
- MCP documentation search was not applicable; review relied on local PRD/epics/architecture primary artifacts and source.

#### 2026-06-11 adversarial re-review (story-automator)

**Review outcome:** Approved. No critical, high, or medium issues. Status remains `done`. No source changes were required.

**Scope note:** The repository is ~132 commits ahead of Story 7.1's commit (`1745611`); the 7.1 surfaces have since been extended by Stories 7.5/7.8/8.x. The review validated the *current* state of the 7.1-owned code against the 7.1 acceptance criteria. Low story-doc test counts (147/525/66/37) are expected and not a finding — current compiled counts are far higher.

**Acceptance-criteria validation (all confirmed against code + tests):**

- AC1 — `AdminScopes.ScopesForRole` makes `tenant-admin` the union of all scopes and each finer role a proper subset (`mailbox/policy/compliance/operations-admin` each = `{see-only, <domain>, audit-obligation}`); `AdminAuthorityEvaluator.HasHumanTenantAdmin`/`HasHumanAdminScope` gate every admin path on the `human` actor-type claim, so service/AI actors carrying `tenant-admin`-looking claims are denied. Proven by `AdminContractTests.TenantAdminShouldBeUnionAndFinerRolesShouldBeProperSubsets` and `AssociationThresholdAuthorizationTests.AdminAssignmentShouldRequireHumanTenantAdmin`.
- AC2 — `AdminQueueSummaryProjector` emits only summary-safe fields (queue ref, health, status/owner-class buckets, safe item refs) and never reads project/evidence/file/audit/mailbox fields off the projection item; `AdminQueueSummaryReadPolicy.Evaluate` allows see-only reads without project membership. Proven by `AdminQueueSummaryProjectorTests` (JSON leak assertions) and `ReadPolicyShouldAllowHumanSeeOnlyAdminWithoutProjectMembership`.
- AC3 — `ExecuteAdminQueueOperation` is gated on `HasHumanAdminScope(Operate)` (only tenant-admin/operations-admin); `IsValidAdminQueueOperation` requires `ScopeUsed == Operate`, positive `ItemCount`, matching/safe item refs, finite reason code, and safe policy-snapshot/redaction tokens; `AdminEvidenceRefs` records admin identity, scope, operation, queue, item-count, subject refs, policy snapshot, reason, redaction, and source version. Proven by `AdminQueueOperationShouldRequireHumanOperateScope` and `AdminQueueOperationAuditRefsShouldRemainMetadataOnly`.
- AC4 — admin mutations flow through the existing `CommandGateway` pre-commit `IAuditWriter.RecordPreCommitAsync` seam; audit-unavailable fails closed, dispatches no durable state, and queues replay intent. Proven by `AdminQueueOperationPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch`. For above-threshold reads, `AdminQueueSummaryReadPolicy` returns `audit_unavailable` when the obligation cannot be met (`ReadPolicyShouldFailClosedAboveAuditThresholdWhenAuditUnavailable`).
- AC5 — every admin command branch denies non-human actors via the human-actor gate; service-client grants do not inherit UI roles; CLI/MCP/UI share the same backend authorization. Proven by `TenantAdminPermissionConformanceTests` (9 tests) and `ReadPolicyShouldDenyServiceAndAiActorsWithTenantAdminLookingClaims`.
- AC6 — contract, authorization, projection, and gateway/audit tests cover the role/scope tokens, union/subset mapping, tolerant parse-failure deny-by-default (`JsonEnumMemberStringConverter` throws on unknown tokens → deserialize returns null → denied), and metadata-only audit refs.

**Verified findings:**

- [LOW — not auto-fixed by design] Orphaned see-only summary surface. `Contracts/Queries/GetAdminQueueSummary.cs` and `AdminQueueSummaryProjector.Create` (plus the `AdminQueueSummary`/`AdminQueueSummaryBucket`/`AdminQueueSummaryItemRef` records) are referenced only by tests — no query handler/dispatch, and they are absent from the public OpenAPI. Story 7.5 superseded this path with `AdminQueueSummaryProjector.Search` → `OperationalQueueSearchResult` + `AdminQueueSummaryReadPolicy.Evaluate`, which **is** wired (used by the Story 8.1 operational dashboard) and carries the same redaction discipline. Not removed: `AdminQueueSummaryProjector.Create`'s test (`SeeOnlySummaryShouldOmitProjectEvidenceFileAuditAndMailboxDetail`) is the canonical AC2/AC6 see-only-redaction proof for this story, so deleting it would weaken Story 7.1's own acceptance coverage for zero functional gain. Recommended as a deliberate cleanup decision for a future operational-queue story rather than an in-review edit. (Sub-note: in the orphaned `Create` path, `WorstHealth` maps an all-`Unknown` item set to `Healthy`; immaterial because the live `Search` path reports per-row health directly.)

**Validation evidence (current tree):**

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — passed, 0 warnings, 0 errors.
- `AdminContractTests` — 47 passed. `AdminQueueSummaryProjectorTests` + `AssociationThresholdAuthorizationTests` — 18 passed. `CommandGatewayTests` — 131 passed. `TenantAdminPermissionConformanceTests` — 9 passed. 0 failed.
- Git/story discrepancy review: all 7.1 File List source files are committed and clean; the only working-tree change to the story is the prior dev-story rerun note. Unrelated dirty files (`GovernedOperationsVisualFoundationE2ETests.cs`, `_bmad-output` docs) belong to later stories and were left untouched per run guard.

### File List

- `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-4-20260601-145742.md`
- `src/Hexalith.ChatBot.Contracts/Commands/AssignTenantAdminRole.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/ExecuteAdminQueueOperation.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminQueueOperation.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminQueueOperations.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminRole.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminRoles.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminScope.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AdminScopes.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/AdminOperationReference.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AdminQueueSummary.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/GetAdminQueueSummary.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjectionItem.cs`
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryProjector.cs`
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryReadDecision.cs`
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryReadPolicy.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/TenantAdminPermissionConformanceTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AdminQueueSummaryProjectorTests.cs`

### Change Log

- 2026-06-02: Implemented bounded tenant-admin role/scope contracts, admin authorization, admin audit refs, queue operation/read summary surfaces, and focused validation tests for Story 7.1.
- 2026-06-02: Senior developer review auto-fixed role/scope overgrant, admin mutation audit-field validation, safe metadata token validation, and affected-item validation; story marked done after focused build/contracts/server/conformance/architecture tests passed.
- 2026-06-11: Re-ran dev-story validation for Story 7.1; all tasks were already checked, all configured compiled ChatBot tests passed, and no implementation changes were required.
- 2026-06-11: Story-automator adversarial re-review. Cross-checked all six ACs against current code and focused tests (build clean; AdminContractTests 47, projector+authorization 18, CommandGatewayTests 131, conformance 9 — 0 failed). No critical/high/medium issues; one LOW observation (orphaned see-only `GetAdminQueueSummary`/`AdminQueueSummaryProjector.Create` surface superseded by Story 7.5, intentionally retained as the AC2 redaction proof). Status remains `done`; no source changes.
