---
baseline_commit: a2da0c5
---

# Story 7.4: Compliance-admin scope

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As a compliance administrator,
I want tenant-wide audit read access and retention configuration without workflow-mutation power,
so that compliance can oversee without operating on project items.

## Acceptance Criteria

1. Given a human `compliance-admin` or `tenant-admin` with `AdminScope.Compliance`, when they query tenant audit records through the governed audit-read surface, then they can read metadata-only audit records across the tenant by tenant, actor, command, resource, decision, reason, correlation, and time context, while service clients, AI actors, mailbox events, CLI automation without delegated human compliance authority, and admins without compliance scope are denied before detail hydration. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.4`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR56`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75f`; `src/Hexalith.ChatBot.Contracts/Enums/AdminScopes.cs`; `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`]
2. Given the audit record references a project, file, mailbox message, participant, approval, AI action, provider payload, or command outcome for which the compliance-admin lacks per-project authority, when audit detail is requested, then restricted fields are redacted or replaced by stable opaque refs, the response does not reveal the hidden resource's name/existence beyond an authorized audit fact, and a safe escalation/request-access path is offered. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.4`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Flow 7 - Compliance or support reviewer investigates a risky action`]
3. Given a compliance-admin triggers an investigation, when the request is accepted, then the system records a metadata-only investigation intent with investigation id, audit query/filter refs, reason code, requester ref, source version, correlation id, policy snapshot id, redaction decision, and escalation state; it does not mutate associations, files, approvals, queue items, mailbox configuration, tenant policy, service-client grants, command allowlists, outbound drafts, or workflow lifecycle state. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#UJ7 - Compliance/support investigates risky action`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75f`; `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md#Scope Boundaries`]
4. Given retention configuration is changed by a human compliance-admin or tenant-admin, when validation runs, then the configured retention windows are bounded by NFR49/NFR49a and the declared data classes, use finite safe tokens and UTC timestamps, preserve append-only WORM chain reconstructability, and perform retention-governed deletion by projection tombstone/key-shredding semantics rather than mutating or deleting audit-chain records. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75f`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR49`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR49a`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR53`]
5. Given a compliance read, investigation trigger, escalation request, or retention configuration write is accepted, when audit pre-commit is unavailable for audited writes the gateway fails closed and writes no durable state; when audit succeeds, metadata-only audit refs include admin role/scope, compliance operation, audit query/filter refs, investigation id, retention class/window refs, old/new retention snapshot fingerprints, reason code, source version, correlation, and policy snapshot id, but never audit envelopes, project names, evidence content, mailbox bodies, message subjects, raw headers, provider payloads, prompts, command bodies, tokens, secrets, raw claims, or unrestricted audit detail. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`; `src/Hexalith.ChatBot.Server/Audit/OperationAuditHistoryHttpResults.cs`]
6. Given compliance-admin scope, when the actor attempts to retry/requeue/quarantine/dismiss queue items, decide associations, correct associations, approve/reject AI actions or outbound sends, change mailbox configuration, change policy-admin-only knobs, assign admin roles, disable/quarantine/rate-limit governance subjects, or mutate project records, then the system denies the operation with user-safe reason codes and no resource-existence leakage; `compliance-admin` remains `SeeOnly + Compliance + AuditObligation`, not `Operate`, `Policy`, or `Mailbox`. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75f`; `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md#Current State To Preserve`; `src/Hexalith.ChatBot.Contracts/Enums/AdminScopes.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`]
7. Given public commands, queries, DTOs, or generated clients change for compliance audit search, investigation intent, escalation, or retention configuration, then the OpenAPI contract spine is updated first, `HexalithChatBotClient.g.cs` is regenerated rather than hand-edited, the generated-client checksum is refreshed, and contract/client tests prove schema parity. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md#Current State To Preserve`; `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`; `tests/fixtures/hexalith-chatbot-generated-client.sha256`]
8. Given acceptance coverage runs, then tests prove compliance-admin and tenant-admin allow, policy/mailbox/operations admin deny where compliance scope is absent, service/AI denial, safe-token and bounded-retention validation, per-project redaction and escalation behavior, metadata-only audit refs, audit-unavailable fail-closed behavior for writes, no workflow mutation by compliance-admin, S9/S5 accessibility/redaction contracts if UI changes, OpenAPI/client drift if public contracts change, and no gateway/audit/admission bypass. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR65-NFR70`]

## Tasks / Subtasks

- [x] Add compliance audit-read, investigation, and retention contracts (AC: 1, 2, 3, 4, 7, 8)
  - [x] Add finite contract types under `src/Hexalith.ChatBot.Contracts` for compliance audit query filters, metadata-only audit result rows, redaction state, investigation intent, escalation request/status, retention class/window, retention snapshot metadata, and retention validation result.
  - [x] Keep all ids, refs, filter keys, reason codes, retention class ids, snapshot ids, investigation ids, escalation ids, and fingerprints finite safe tokens. Do not model free-form audit queries as raw JSON or SQL-like strings.
  - [x] Audit result rows may include stable command/resource/correlation/status/ref tokens and UTC timestamps only. Do not include audit envelopes, command payloads, project names, mailbox subjects/bodies, evidence content, file metadata, provider payloads, prompts, outputs, raw claims, headers, tokens, secrets, or stack traces.
  - [x] Model retention by declared data class and bounded window. Include source version, effective timestamp, supersedes/superseded refs, reason code, policy snapshot id, and old/new fingerprints.
- [x] Add governed commands/queries for compliance behavior (AC: 1, 3, 4, 5, 7, 8)
  - [x] Add query contracts such as `SearchComplianceAuditRecords` / `GetComplianceAuditDetail` or equivalent names that match existing command/query naming. Reads must still pass backend authorization/redaction; UI-only filtering is insufficient.
  - [x] Add commands such as `TriggerComplianceInvestigation`, `RequestComplianceEscalation`, and `SubmitRetentionConfigurationChange` or equivalent names that implement `IChatBotCommand` for audited state-writing intents.
  - [x] Include safe metadata only: operation/investigation/retention change id, source version, query/filter refs or fingerprints, old/new retention snapshot refs/fingerprints, reason code, requester ref, escalation target ref when allowed, schema version, correlation id, and policy snapshot id.
  - [x] Wire any new state-mutating command types into `ChatBotSpineCommandAllowlist` only after authorization, validation, audit, dispatch, and tests are in place.
  - [x] Update OpenAPI, generated client, checksum, and client tests if these shapes are public.
- [x] Extend authorization and redaction through existing gateway/audit seams (AC: 1, 2, 5, 6, 8)
  - [x] Reuse `AdminAuthorityEvaluator.HasHumanAdminScope(..., AdminScope.Compliance)`; do not add duplicate role parsing, audit-reader superuser logic, or a support/debug bypass.
  - [x] Deny service/AI actors and non-human surfaces even if they carry `chatbot:tenant-role = tenant-admin` or `compliance-admin`. CLI/MCP may participate only through the same backend authorization and explicit delegated human compliance authority.
  - [x] Keep tenant identity from `ChatBotTenantBinding`; never trust tenant ids from command bodies, route/query params, audit ids, retention refs, correlation ids, or UI state.
  - [x] Extend `ParticipantAuthorizationStage` validation with safe reason codes, non-negative source versions, finite query/filter refs, investigation ids, retention snapshot refs, old/new fingerprints, and bounded retention windows.
  - [x] Extend `AuditEnvelopeFactory.AdminEvidenceRefs` or a compliance-specific helper with safe refs such as `admin-operation:search-compliance-audit`, `admin-operation:trigger-compliance-investigation`, `admin-operation:request-compliance-escalation`, `admin-operation:submit-retention-change`, `admin-scope:compliance`, `audit-query:<safe-id>`, `audit-filter:<safe-id>`, `investigation:<safe-id>`, `retention-class:<safe-id>`, `retention-window:<safe-id>`, `retention-old-fingerprint:<safe-id>`, `retention-new-fingerprint:<safe-id>`, `policy-snapshot:<safe-id>`, and `reason:<safe-code>`.
  - [x] Preserve `CommandGateway` pre-commit audit fail-closed behavior. Do not write compliance investigation or retention state directly from UI, query controllers, projections, workers, CLI, MCP, service clients, or background tasks.
- [x] Add metadata-only compliance projections/read policies (AC: 1, 2, 3, 5, 8)
  - [x] Extend or add audit-history readers/projections under `src/Hexalith.ChatBot.Server/Audit` or `src/Hexalith.ChatBot.Server/Projections` so tenant-wide compliance reads return safe audit summaries and defer detail hydration to per-project authorization/redaction checks.
  - [x] Reuse `OperationAuditHistoryHttpResults`' metadata-only discipline for audit history. If richer search is needed, add a new projection that stores/searches safe indexed fields, not raw envelopes or command payloads.
  - [x] Represent restricted detail with explicit redaction state and safe next action. The row may say detail is restricted and offer escalation; it must not name the hidden project/file/party/mailbox/evidence.
  - [x] Ensure investigation trigger records an auditable intent/handoff only. It may link to audit refs and escalation state; it must not execute replay/simulation, mutate production records, or start export/delete workflows beyond retention configuration scope in this story.
- [x] Add retention configuration validation and immutable snapshot behavior (AC: 4, 5, 8)
  - [x] Define retention class/window contracts for ChatBot-owned derived data classes: source email metadata, attachments, association records, evidence snapshots, approval records, policy snapshots, lifecycle state, workflow predecessor/successor maps, AI prompts/outputs/context, logs/support bundles where represented, and audit records.
  - [x] Enforce NFR49/NFR49a bounds: WORM audit chain records are append-only; retention-governed erasure is represented by projection tombstones and key shredding/redaction-key state, not mutation or deletion of audit-chain records.
  - [x] Preserve default MVP retention behavior from PRD data-governance tables unless a bounded tenant policy explicitly changes it.
  - [x] Treat missing, stale, invalid, or conflicting retention snapshots as fail-closed for retention writes and as redacted/degraded status for reads.
- [x] Add S9/S5 UI contracts only if this story exposes UI (AC: 2, 3, 4, 8)
  - [x] Use existing FrontComposer/Fluent UI governed components and `src/Hexalith.ChatBot.UI` patterns. Do not introduce another design system, marketing page, raw JSON audit browser, or nested-card dashboard.
  - [x] For S9 Audit Investigation, render filterable audit timelines with actor, command surface, decision, policy snapshot, correlation id, outcome, redaction state, and escalation status only where authorized.
  - [x] For S5 Tenant Configuration retention settings, reuse validation summary before fields, field-level `aria-invalid`/`aria-describedby`, reachable disabled-action explanations, focus to summary on validation failure, and safe conflict causes limited to policy/permission/stale data.
  - [x] On phone, provide read-only audit/retention summary/status, safe escalation action if practical, and a reachable explanation that dense audit analysis or retention editing requires a larger screen. Preserve draft/filter state when returning to a larger screen.
  - [x] Localize visible text through existing `ChatBotUiTextKey`, `SharedResource.resx`, and `SharedResource.fr.resx` patterns.
- [x] Add focused tests (AC: all)
  - [x] Contract tests near `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` for compliance scope mapping, audit filter/query safe tokens, investigation/retention serialization, bounded windows, retention class validation, and secret-bearing property bans.
  - [x] Server authorization tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs` for compliance-admin/tenant-admin allow, policy/mailbox/operations admin deny, service/AI deny, invalid payload deny, and no operate/policy/mailbox mutation authority.
  - [x] Gateway/audit tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` for audit-unavailable fail-closed retention/investigation writes and metadata-only compliance refs.
  - [x] Projection/read-policy tests proving tenant-wide audit summary reads are safe, per-project restricted detail is redacted, escalation is offered without leaking hidden resources, and no raw audit envelope or command payload reaches public responses.
  - [x] UI/bUnit contract tests under `tests/Hexalith.ChatBot.UI.Tests` and UI E2E tests only if S9/S5 surfaces change, covering redaction state, validation/focus behavior, disabled explanations, small-screen fallback, localization, and absence of restricted markers.
  - [x] Conformance/architecture/client tests if new public surfaces, generated client shapes, actor isolation behavior, or architectural boundaries change.

## Dev Notes

### Scope Boundaries

- Story 7.4 implements compliance-admin read/investigate/retention configuration authority. It does not implement operational queue management (7.5), notification routing (7.6), escalation policy configuration (7.7), approval queue prioritization (7.8), notification throttling/backlog observables (7.9-7.11), disable/quarantine/rate-limit controls (7.12-7.26), command allowlist v1/lifecycle completion (7.27), or full Epic 9 tamper-evident audit store/search/export/delete workflows.
- `compliance-admin` can read tenant audit records subject to redaction, trigger investigation/escalation intents, and configure retention windows within NFR49/NFR49a bounds. It cannot operate workflow items or mutate project records.
- `tenant-admin` has the union role from Story 7.1, but it is still not a project-content, mailbox-content, raw-audit, or raw-provider superuser. Per-project authority and redaction rules still apply to restricted detail.
- Compliance reads must separate summary-safe audit facts from restricted detail. A tenant-wide audit row can prove that an audited event exists, but project/file/mailbox/evidence detail requires project authority or an escalation path.
- Retention configuration is governance metadata. It must never weaken WORM audit reconstructability, delete audit-chain records, or turn evidence preservation into uncontrolled data storage.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Contracts/Enums/AdminRole.cs`, `AdminRoles.cs`, `AdminScope.cs`, and `AdminScopes.cs` - finite admin role/scope model. `ComplianceAdmin` maps to `SeeOnly`, `Compliance`, and `AuditObligation`; do not add `Operate`, `Policy`, or `Mailbox`.
- `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` - human-only admin scope evaluation. Extend authorization through this helper.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - central authorization stage for admin commands. Add compliance command/query validation here rather than authorizing in UI, projections, CLI, MCP, or service-client code.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`, `AuditMetadata.cs`, `ChatBotStateWritingPathInventory.cs`, `OperationAuditHistoryHttpResults.cs`, and `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - metadata-only audit refs, audit history projection discipline, and pre-commit fail-closed behavior.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` - add compliance state-writing commands only after validation/audit/tests are in place.
- `src/Hexalith.ChatBot.Server/Projections/AdminQueueSummaryReadPolicy.cs` and `AdminQueueSummaryProjector.cs` - examples of summary-safe admin read policy and projection behavior.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotDisabledActionReasons.cs`, `ChatBotRefusalReasonCodes.cs`, and `ChatBotAuthorizationReasonCodes.cs` - finite safe denial, escalation, and blocked-state reason text.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor`, `ChatBotStatusBanner.razor`, `ChatBotEvidenceChip.razor`, `ChatBotRiskChip.razor`, and `ChatBotTenantPolicyEditor.razor` - governed UI primitives and S5 patterns to reuse if UI changes.
- `src/Hexalith.ChatBot.UI/Design/ChatBotTenantPolicyEditorContract.cs`, `ChatBotValidationErrorContract.cs`, `ChatBotSaveConflictCause.cs`, `ChatBotRecoveryPatternContract.cs`, and `ChatBotSmallScreenFallbackContract.cs` - accessibility/recovery/fallback contracts to extend for retention settings.
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and `tests/fixtures/hexalith-chatbot-generated-client.sha256` - update/regenerate only if public contracts change.

### Current State To Preserve

- Story 7.1 fixed role overgrant issues. Preserve that `compliance-admin` has `SeeOnly + Compliance + AuditObligation` only and add tests that fail if it gains operate, policy, or mailbox authority.
- Story 7.2 established closed schema validation, known schema versions, safe token/fingerprint checks, metadata-only audit refs, two-person policy approval, and OpenAPI/generated-client workflow. Reuse the safe-token and generated-client discipline.
- Story 7.3 established mailbox-admin metadata-only patterns and confirmed raw mailbox/provider material never enters admin UI/audit refs. Compliance audit views must not reintroduce mailbox bodies, subjects, headers, Graph payloads, tokens, or provider secrets.
- `OperationAuditHistoryHttpResults` currently projects post-commit envelopes into a metadata-only audit-history response. Do not expose `AuditEnvelope` directly through compliance search.
- `CommandGateway` suppresses dispatch when pre-commit audit cannot be written. Compliance investigation and retention writes must reuse this path.
- Existing redaction rules make authorization denied and safe-not-found indistinguishable where required. Restricted audit detail must preserve this behavior.
- Existing root submodule policy applies: initialize/update only root `.gitmodules` submodules and never use recursive submodule commands.

### Architecture Guardrails

- Contracts belong in `src/Hexalith.ChatBot.Contracts`; generated client output belongs only in `src/Hexalith.ChatBot.Client/Generated`; server authorization belongs in `src/Hexalith.ChatBot.Server/Gateway/Stages` or `src/Hexalith.ChatBot.Server/Governance/Admin`; audit/read projections belong in `src/Hexalith.ChatBot.Server/Audit` or `src/Hexalith.ChatBot.Server/Projections`; UI work belongs in `src/Hexalith.ChatBot.UI`.
- Every state mutation follows `auth -> tenant-bind -> authorize -> risk-classify -> approval-gate -> coarse-idempotency -> pre-commit-audit -> EventStore execute/publish/projection -> post-commit-audit`. Do not create a compliance admin direct-write path.
- Tenant IDs come from authenticated gateway binding. Audit ids, operation ids, correlation ids, retention refs, policy snapshot ids, route/query params, and UI state are comparison inputs only, not authorization proof.
- Store stable IDs/refs and metadata. Do not persist upstream PII or secrets in compliance events/projections beyond existing audit-safe refs.
- Retention snapshots are immutable and versioned. New changes produce new versions with supersession links; prior audit/approval/workflow records continue referencing their original retention/policy snapshot refs.
- WORM audit chain mutation is out of scope and forbidden. Retention-governed erasure works by projection tombstones/key shredding/redaction records as described by NFR49a.
- If public OpenAPI/client shapes change, update the OpenAPI spine, regenerate the client, refresh the checksum, and include client tests.

### UX Guardrails

- S9 Audit Investigation and S5 Tenant Configuration are operational work surfaces. Keep them dense, structured, and scannable; no marketing page, no decorative nested cards, no raw JSON dump, and no unrestricted audit envelope viewer.
- Audit timelines show ordered events with actor, command surface, decision, policy snapshot, correlation id, outcome, redaction state, and escalation status. Restricted details use blocked/redacted states with safe next actions.
- Validation summary appears before fields; invalid retention fields carry `aria-invalid` and reference field messages via `aria-describedby`.
- Disabled controls need a reachable explanation. Tooltip-only and default non-focusable disabled buttons are insufficient.
- Save conflicts must name only safe categories: policy, permission, or stale data. Do not surface raw exceptions, query payloads, audit details, or provider data.
- On phone, dense audit analysis and retention editing can be unavailable only if read-only summary/status, safe escalation action, and a reachable explanation remain.
- Use semantic status consistently: warning for projection pending/redacted/degraded/stale evidence, danger for terminal failure/permission denied, success only after investigation/retention save is actually accepted.

### Previous Story Intelligence

- Story 7.3 added mailbox configuration and health through `AdminScope.Mailbox`, metadata-only mailbox contracts, worker config lookup, S5 UI extensions, OpenAPI/client refresh, and tests. Compliance work should mirror its metadata-only contract discipline, but must not grant mailbox configuration/content access.
- Story 7.3 review fixed omitted enum-default validation for mailbox routing/freshness and UI behavior that existed only in tests. Compliance contracts should include `Unknown`/invalid defaults where enum deserialization could otherwise create valid-looking requests, and UI behavior must land in real components if specified.
- Story 7.2 added the Tenant Policy Schema and two-person policy approval, then fixed duplicate knob handling, finite schema-version enforcement, and old/new fingerprints in audit refs. Apply analogous protections to retention snapshot ids, retention class ids, query/filter refs, investigation ids, and old/new retention fingerprints.
- Story 7.1 established bounded admin roles/scopes and audit obligation, then fixed role/scope overgrant, empty audit-obligation fields, and unsafe affected refs. Do not repeat those defects for compliance reads/investigations/retention writes.
- Epic 1 audit work established two-phase audit and fail-closed pre-commit behavior. Compliance investigation must read and reference audit; it must not become a second audit writer or raw audit bypass.

### Latest Technical Specifics

- No external version research is required for implementation. Use the repo-pinned stack and do not upgrade packages as part of this story: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, Blazor/FrontComposer, Fluent UI v5 RC, and existing OpenAPI/generated-client tooling.
- Do not change target frameworks, Aspire/Dapr topology, Fluent UI, Fluxor, NSwag/client generation tooling, MCP SDK, Graph permission posture, WORM audit backing assumptions, or submodule pointers unless a compile-time contract regeneration command requires generated client output.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` if S9/S5 UI/components/design contracts change.
  - `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` if audit investigation or retention UI workflows change.
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` if actor isolation, compliance command/query surfaces, or cross-surface behavior changes.
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` if new boundaries are introduced.
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` if OpenAPI/generated client changes.
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, metadata-only fixtures, and root-level submodule policy.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`, including Epic 7 and Story 7.4 plus adjacent Epic 7 scope stories.
- Loaded `prd_content` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`, especially FR55-FR63, FR75f, UJ7, NFR2, NFR7, NFR24, NFR36, NFR45, NFR49-NFR54, NFR65-NFR70, Data Governance Surface, and Tenant-Admin Permission Model.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway, two-phase audit, redaction, project structure, FR-to-structure mapping, audit/compliance NFRs, and testing strategy.
- Loaded `ux_content` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md`, especially S9 Audit Investigation, S5 Tenant Configuration, redacted detail, escalation, accessibility, responsive fallback, and audit timeline semantics.
- Loaded persistent project-context facts from sibling `project-context.md` files, with relevant constraints from Hexalith.EventStore, Hexalith.FrontComposer, Hexalith.Tenants, Hexalith.Folders, Hexalith.Memories, and Hexalith.Commons.
- Loaded previous story intelligence from `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md`, `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md`, and `_bmad-output/implementation-artifacts/7-3-mailbox-admin-scope-and-mailbox-configuration.md`.
- Inspected current source anchors: admin roles/scopes, admin authority evaluation, participant authorization, tenant policy contracts, audit envelope factory, operation audit history projection, command allowlist, S5 UI design contracts, admin contract tests, gateway authorization tests, and command gateway audit tests.
- Reviewed recent git history: `a2da0c5 feat(story-7.3): Add mailbox admin configuration`, `c6bcd1a feat(story-7.2): Add tenant policy schema administration`, `1745611 feat(story-7.1): Add bounded tenant admin permissions`, `2297fe9 feat(story-6.5): Disambiguate delegated and external senders`, and `fd2cadf feat(story-6.4): Inbound authenticity header inspection`.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 7 and Story 7.4 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR55-FR63, FR75f, UJ7, NFR2, NFR7, NFR24, NFR36, NFR45, NFR49-NFR54, NFR65-NFR70, Data Governance Surface.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Shared Command Pipeline, Tenant Policy Schema, Replay Isolation, Operating Baselines.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, audit model, redaction, project structure, testing strategy.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - S9 Audit Investigation, S5 Tenant Configuration, redacted detail, escalation, accessibility, responsive behavior.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - audit timeline, blocked state, semantic status, Fluent UI/FrontComposer design spine.
- `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md` - bounded admin role/scope invariants and review fixes.
- `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md` - policy schema, safe-token, OpenAPI/client, audit, and review lessons.
- `_bmad-output/implementation-artifacts/7-3-mailbox-admin-scope-and-mailbox-configuration.md` - mailbox metadata-only, S5, OpenAPI/client, and review lessons.
- `src/Hexalith.ChatBot.Contracts/Enums/AdminScopes.cs` - compliance-admin scope mapping.
- `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` - human admin scope evaluation.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - central authorization validation.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - metadata-only audit evidence refs.
- `src/Hexalith.ChatBot.Server/Audit/OperationAuditHistoryHttpResults.cs` - metadata-only audit-history response pattern.
- `src/Hexalith.ChatBot.UI/Design/ChatBotTenantPolicyEditorContract.cs` - S5 validation/recovery/small-screen contract.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-02: Implemented compliance-admin contract, authorization, audit-ref, read-policy, OpenAPI/generated-client, and test coverage.
- Validation completed:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
- 2026-06-02: Story-automator review auto-fixes applied:
  - Enforced compliance audit query filters and UTC time bounds before returning tenant-wide rows.
  - Prevented denied compliance audit search responses from echoing unsafe query/correlation refs.
  - Changed compliance investigation, escalation, and retention snapshot source versions from arbitrary strings to non-negative numeric versions.
  - Added compliance source-version/redaction/escalation evidence refs to admin audit metadata.
  - Regenerated the OpenAPI client and refreshed the generated-client checksum.
- Review validation completed:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none`

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added finite compliance audit query/detail, investigation/escalation, retention window/snapshot, redaction, and escalation contracts with safe-token, UTC, bounded-retention, fingerprint, schema-version, and enum-default validation.
- Added human-only compliance command authorization through `AdminAuthorityEvaluator.HasHumanAdminScope(..., AdminScope.Compliance)`; service/AI/non-compliance admins deny before dispatch.
- Added compliance investigation/escalation/retention commands to the spine allowlist after authorization/audit/test coverage.
- Extended admin audit evidence refs with metadata-only compliance operation, query/filter, investigation, escalation, retention class/window, fingerprint, policy snapshot, and reason refs.
- Added metadata-only compliance audit read policy that returns tenant-wide summaries and redacts restricted details with safe request-access escalation behavior.
- Updated OpenAPI first, regenerated `HexalithChatBotClient.g.cs` through NSwag, and refreshed the generated-client checksum fixture.
- No application S9/S5 UI was exposed in this implementation slice; compliance E2E fixture coverage was present and was included in review validation.
- Review fixed audit read filtering so accepted filter keys and UTC query windows now constrain returned rows before hydration.
- Review fixed denied compliance audit search responses so unsafe query refs and correlations are replaced with `denied`.
- Review fixed compliance investigation/escalation source-version validation to require non-negative numeric source versions instead of arbitrary safe-token strings.
- Review fixed metadata-only compliance audit refs to include compliance source-version evidence for investigation/escalation and redaction/escalation evidence for escalation requests.

### File List

- src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs
- src/Hexalith.ChatBot.Contracts/Enums/ComplianceAuditRedactionState.cs
- src/Hexalith.ChatBot.Contracts/Enums/ComplianceEscalationStatus.cs
- src/Hexalith.ChatBot.Contracts/Queries/ComplianceAuditQueries.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs
- src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs
- src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs
- src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs
- tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditReadPolicyTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs
- tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs
- tests/fixtures/hexalith-chatbot-generated-client.sha256
- Hexalith.Tenants
- _bmad-output/implementation-artifacts/7-4-compliance-admin-scope.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-02

Outcome: Approved after auto-fixes. No critical issues remain.

Review inputs:

- Story status was `review`; epic/story resolved as 7.4 from `_bmad-output/implementation-artifacts/7-4-compliance-admin-scope.md`.
- Planning and architecture context used: `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/architecture.md`, PRD/UX references listed in the story, and previous Story 7.1-7.3 intelligence.
- External MCP/web documentation search was not performed because the story explicitly pinned the repo stack and stated no external version research was required; network access is restricted in this environment.

Findings fixed:

- HIGH: `ComplianceAuditReadPolicy.Search` validated query filters but ignored them, returning any safe envelope inside the limit. Fixed by applying actor, actor-type, command, resource, tenant, decision, reason, correlation, policy-snapshot, and UTC time-window constraints before row projection. Added regression coverage in `ComplianceAuditReadPolicyTests`.
- HIGH: Denied compliance audit search responses could echo an unsafe `QueryRef` from an invalid query. Fixed denied responses to replace unsafe query refs and correlations with `denied`, with regression coverage.
- HIGH: `RequestComplianceInvestigation`, `RequestComplianceEscalation`, and `RetentionSnapshotMetadata` modeled `SourceVersion` as free-form strings, allowing safe-looking non-version tokens despite the story requiring non-negative source versions. Fixed contracts/OpenAPI/generated client/tests to use numeric `long` source versions with non-negative validation.
- MEDIUM: Compliance investigation/escalation audit evidence omitted source-version refs, and escalation omitted redaction/escalation refs. Fixed `AuditEnvelopeFactory.AdminEvidenceRefs` to emit metadata-only compliance source-version, redaction, and escalation refs.
- MEDIUM: Actual git changes included `tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs` and the root `Hexalith.Tenants` submodule pointer, but the story File List did not document them. File List updated.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 153 total
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 550 total
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - passed, 17 total
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 37 total
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 75 total
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - passed, 61 total

---

Reviewer: Jerome on 2026-06-11 (story-automator re-review, Claude Opus 4.8)

Outcome: Approved. No critical or high issues. Status remains `done`.

Context: The 7.4 commit (`5dbb31a`, 2026-06-02) is now 132 commits behind HEAD; story 9.3 (`27fe87a`) later added the S9 audit-investigation endpoint/UI and extended the same compliance contracts, read policy, and audit refs. This re-review validates every 7.4 acceptance criterion and task against the current working tree, not just the original slice.

Acceptance criteria re-verified against current source:

- AC1: `AdminAuthorityEvaluator.HasHumanAdminScope` gates on `IsHumanActor` before deriving role scopes, so an AI/service actor carrying a `tenant-admin`/`compliance-admin` role claim is denied before any row hydration. Endpoint tests cover AI-actor and finer-admin-role denial.
- AC2: `ComplianceAuditReadPolicy.HasPerProjectAuthority`/`Detail` drive visibility from actual `project:` grants, collapse to `EscalationRequired` + `request-access`, and emit `redacted-ref` opaque tokens without leaking hidden resource names.
- AC3: `RequestComplianceInvestigation` is validated metadata-only in `ParticipantAuthorizationStage` and carries no state-mutation path.
- AC4: `ComplianceAdministrationSchema.ValidateRetentionChangeSet` enforces bounded windows (audit-records floor 2555 days), finite safe tokens, and UTC; retention is configuration-only (erasure execution remains Epic 9 scope).
- AC5: `ComplianceInvestigationAndRetentionWritesShouldFailClosedWhenPreCommitAuditUnavailable` proves the gateway fail-closed path; `AuditEnvelopeFactory` emits metadata-only `admin-scope:compliance`/`audit-query`/`retention-*` refs.
- AC6: `AdminScopes.ScopesForRole(ComplianceAdmin)` = `SeeOnly + Compliance + AuditObligation` only; no Operate/Policy/Mailbox.
- AC7: Client.Tests prove OpenAPI/generated-client parity and the checksum fixture.
- AC8: Denial, redaction, fail-closed, replay-exclusion, and filter lock-step coverage all present and passing.

Findings this pass:

- MEDIUM (transparency): `tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditInvestigationEndpointTests.cs` carries uncommitted additions (+2 denial tests: human-tenant-admin allow vs AI-actor deny, and mailbox/operations/policy-admin deny before returning rows). They compile and pass, strengthening AC1/AC6/AC8 coverage. They belong to story 9.3's endpoint surface, so they are intentionally not added to the 7.4 File List; left in place (review does not commit).
- LOW (quality, not changed): `ComplianceAuditReadPolicy.Search` computes `ResultFingerprint` as `sha256:<rowCount>` rather than a content hash. It is metadata-safe and format-valid; consistent with this repo's intentional foundation placeholders, so it was deliberately not "fixed" to avoid non-correctness churn against parity/serialization tests.

Re-review validation (full compiled in-process suites, all green):

- `dotnet build Hexalith.ChatBot.slnx -m:1 /nr:false` - succeeded, 0 warnings, 0 errors
- `Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 482 total
- `Hexalith.ChatBot.Server.Tests -parallel none` - passed, 1571 total
- `Hexalith.ChatBot.Client.Tests -parallel none` - passed, 34 total
- `Hexalith.ChatBot.Architecture.Tests -parallel none` - passed, 39 total
- `Hexalith.ChatBot.Conformance.Tests -parallel none` - passed, 93 total
- `Hexalith.ChatBot.UI.E2E.Tests -parallel none` - passed, 87 total

### Change Log

- 2026-06-02: Implemented compliance-admin audit read/investigation/escalation/retention contracts, authorization, metadata-only audit refs/read policy, OpenAPI/generated client parity, and focused validation tests.
- 2026-06-02: Story-automator review fixed audit query filtering, numeric source-version validation, compliance audit evidence refs, generated-client parity, and story File List completeness; status set to done.
- 2026-06-11: Story-automator re-review (repo at HEAD, 132 commits past the 7.4 slice) re-verified all 8 ACs and tasks against current source; full suite green (build 0/0; Contracts 482, Server 1571, Client 34, Architecture 39, Conformance 93, UI.E2E 87). No critical/high issues; recorded one MEDIUM transparency note (uncommitted 9.3-surface denial tests) and one LOW non-blocking quality note (count-based result fingerprint). Status unchanged: done.
