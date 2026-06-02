---
baseline_commit: 1745611
---

# Story 7.2: Policy-admin scope, Tenant Policy Schema editor, and AI action policy

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As a policy administrator,
I want to configure tenant policy knobs, including AI action policy, within a closed, versioned schema with a two-person rule on sensitive changes,
so that governance behavior is tunable but never unsafe.

## Acceptance Criteria

1. Given the closed, versioned Tenant Policy Schema, when a human `policy-admin` or `tenant-admin` edits policy knobs through the governed command spine, then only product-declared knobs can be changed, values must satisfy declared type/range/enum validation, tenants cannot define new knobs, and every accepted change records actor, scope used, old value reference, new value reference, source version, timestamp, correlation, reason/justification, and policy snapshot id. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Tenant Policy Schema`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`]
2. Given a non-human actor, service client, AI actor, mailbox event, CLI automation without delegated human policy authority, or an admin role without `AdminScope.Policy`, when it attempts to mutate tenant policy, then authorization denies the request before state load and without exposing tenant policy body, project names, evidence content, file metadata, mailbox content, raw claims, headers, tokens, or audit detail. [Source: `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md#Current State To Preserve`; `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`]
3. Given a security-sensitive knob is changed, including `association.t-high`, `association.t-low`, `ai-action.low-risk-allowed`, allowlist version pin, admin permission scopes, classifier explanation toggle, or inbound-authenticity strictness, when the first policy-admin submits the change, then the system creates a pending two-person policy approval record and does not activate the new policy until a second distinct human admin with policy authority approves with a documented justification recorded in audit. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.2`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Tenant Policy Schema`]
4. Given AI action policy is configured, when the policy schema is saved and evaluated, then `ai-action.low-risk-allowed` is represented as a per-action-class map for exactly `modifies-state`, `exposes-files`, `sends-external`, `creates-tasks`, `invokes-tools`, and `acts-on-behalf`; every class defaults to `false`; opt-in is per class; approval-required behavior remains the safe default for risky actions; and `DefaultAiActionPolicyEvaluator`/`ITenantAiPolicySnapshotProvider` consume the versioned snapshot rather than a global low-risk boolean. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR41`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR52`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Tenant Policy Schema`; `src/Hexalith.ChatBot.Contracts/Enums/AiActionRiskActionClass.cs`; `src/Hexalith.ChatBot.Server/Governance/AiMediation/DefaultAiActionPolicyEvaluator.cs`]
5. Given a policy-admin uses the Tenant Configuration S5 editor, when policy data is loading, empty, edited, invalid, pending approval, saved, conflicted, stale, permission-blocked, or failed, then the UI renders validation summary before fields, associates field errors with controls, explains disabled/unavailable actions without tooltip-only dependency, focuses the summary on validation failure, reports save conflicts as policy/permission/stale-data causes, and uses the small-screen fallback for dense admin editing on phone. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Tenant Configuration`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility Floor`; `src/Hexalith.ChatBot.UI/Design/ChatBotValidationErrorContract.cs`; `src/Hexalith.ChatBot.UI/Design/ChatBotSaveConflictCause.cs`]
6. Given a policy mutation or second approval is accepted, when audit pre-commit cannot be written, the gateway fails closed and no durable policy state changes. When audit succeeds, metadata-only audit refs include admin role/scope, policy operation, changed knob ids, old/new snapshot ids or fingerprints, justification reason code, source version, and correlation. Audit refs must never include full policy JSON, project names, mailbox bodies, provider payloads, raw claims, headers, tokens, or secrets. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`; `src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs`]
7. Given acceptance coverage runs, then tests prove closed schema validation, unknown-knob denial, range/enum rejection, sensitive/non-sensitive knob classification, two-person distinct-actor activation, service-client/AI denial, policy-admin allow, non-policy-admin deny, per-class AI low-risk evaluation, stale/expired policy routing to approval, S5 validation/accessibility/recovery contracts, metadata-only audit refs, OpenAPI/generated-client drift if public contracts change, and no new gateway/audit/admission bypass. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md#Testing Notes`]

## Tasks / Subtasks

- [x] Add closed Tenant Policy Schema contracts and validators (AC: 1, 3, 4, 7)
  - [x] Add finite contract types under `src/Hexalith.ChatBot.Contracts` for policy schema version, knob id, knob type, sensitivity, validation rule, policy value, policy change set, policy snapshot metadata, and policy approval status.
  - [x] Include the M0 knob set exactly: `association.t-high` double `[0.80, 1.00]` default `0.90`; `association.t-low` double `[0.50, t-high)` default `0.60`; `attachments.unsafe-handling` enum `quarantine | block | reject-message` default `quarantine`; `ai-action.low-risk-allowed` map of the six `AiActionRiskActionClass` values to bool, all default `false`; `mailbox.routing-rules` list default empty.
  - [x] Include M1 schema entries needed by Story 7.2 where implemented or previewed: approval routing knobs, admin permission scopes, allowlist version pin, classifier explanation layer toggle, and inbound-authenticity strictness. Do not implement mailbox/provider credential editing, notification routing, retention, queues, rate limits, or command allowlist v1 beyond schema metadata needed for validation.
  - [x] Reject unknown knob ids, wrong value types, missing required map keys, extra action-class keys, unsafe free-form ids, invalid ranges, `NaN`/`Infinity`, and unsafe policy snapshot ids.
  - [x] Keep policy value payloads structured and bounded. Do not store or serialize arbitrary JSON dictionaries outside typed schema handling.
- [x] Add governed policy mutation and two-person approval commands (AC: 1, 2, 3, 6, 7)
  - [x] Add commands such as `SubmitTenantPolicyChange` and `ApproveTenantPolicyChange` or equivalent names that match existing command naming, implement `IChatBotCommand`, and carry safe metadata only: policy change id, source version, changed knob ids, old/new snapshot refs or fingerprints, justification reason code, requester/approver refs, schema version, and correlation.
  - [x] Wire the command types into `ChatBotSpineCommandAllowlist` only after gateway validation, audit, and tests are in place.
  - [x] For security-sensitive knobs, persist a pending policy approval record and require a second distinct human policy-authorized admin before activation. The requester cannot approve their own sensitive change.
  - [x] For non-sensitive knobs, allow direct activation only if the schema marks the knob non-sensitive and audit pre-commit succeeds.
  - [x] Represent activation as a new immutable policy snapshot. Supersede old snapshots; do not mutate historical snapshots or approval records.
- [x] Extend server authorization and audit without adding another gateway path (AC: 2, 3, 6, 7)
  - [x] Reuse `AdminAuthorityEvaluator.HasHumanAdminScope(..., AdminScope.Policy)` for policy mutations and approvals.
  - [x] Deny service/AI actors and non-human surfaces even if they carry `chatbot:tenant-role = tenant-admin` or `policy-admin`.
  - [x] Keep tenant identity from `ChatBotTenantBinding`; never trust tenant ids from command bodies, policy ids, route/query params, queue refs, or UI state.
  - [x] Extend `ParticipantAuthorizationStage` validation for policy mutation/approval commands with finite reason codes, required justification for sensitive changes, non-negative source version, safe ids, distinct requester/approver for second approval, and changed-knob refs drawn from the closed schema.
  - [x] Extend `AuditEnvelopeFactory.AdminEvidenceRefs` or a policy-specific helper with safe refs such as `admin-operation:submit-policy-change`, `admin-operation:approve-policy-change`, `admin-scope:policy`, `policy-knob:<safe-id>`, `policy-snapshot:<safe-id>`, `policy-change:<safe-id>`, and `reason:<safe-code>`.
  - [x] Preserve `CommandGateway` pre-commit audit fail-closed behavior. Do not write policy state directly from UI, query controllers, projections, workers, CLI, MCP, or service clients.
- [x] Integrate AI action policy with per-class low-risk configuration (AC: 4, 6, 7)
  - [x] Replace or wrap the current `TenantAiPolicySnapshot.LowRiskAllowed` global bool with a per-action-class policy shape while preserving compatibility where tests require it.
  - [x] Ensure `DefaultAiActionPolicyEvaluator` checks the request's action-class set against the snapshot map. Low-risk execution is allowed only when project authorization, fresh/valid policy, context package, effect surface, assistance kind, and every relevant action-class policy condition pass.
  - [x] Preserve routing to approval for missing project authorization, non-low-risk classification, missing context package, stale/expired/invalid policy, and any action class whose `low-risk-allowed` value is `false`.
  - [x] Keep FR41 risky actions approval-required by default. Do not add coarse global shortcuts or bypass approval gates for external send, file exposure, project mutation, task creation, tool invocation, or acting on behalf.
- [x] Add the Tenant Configuration S5 policy editor surface and state contracts (AC: 1, 3, 5, 7)
  - [x] Add a focused policy editor page or component under `src/Hexalith.ChatBot.UI` using existing Fluent UI/FrontComposer-compatible governed primitives. Avoid a marketing/landing page and avoid nested cards.
  - [x] Show schema sections, current snapshot metadata, pending two-person approvals, validation summary, changed-knob list, and safe conflict status. Full policy JSON should not be dumped into the UI.
  - [x] Use existing localization patterns in `ChatBotUiTextKey`, `SharedResource.resx`, and `SharedResource.fr.resx` for all visible text.
  - [x] Use `ChatBotValidationErrorContract`, `ChatBotSaveConflictCause`, `ChatBotRecoveryPatternContract.ForTenantConfiguration`, focus-return contracts, live-region deduplication, and disabled-action contracts already present in UI design tests.
  - [x] On phone, provide read-only summary/status, safe pending-approval actions if practical, and a reachable explanation that dense policy editing requires a larger screen. Preserve draft state when routing back to a larger screen.
- [x] Update public contract spine only if public shapes change (AC: 1, 4, 7)
  - [x] If new commands/queries/DTOs are externally visible, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
  - [x] Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit generated `.g.cs`.
  - [x] Refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` and run client contract tests.
- [x] Add focused tests (AC: all)
  - [x] Contract tests near `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` for schema tokens, knob ids, sensitivity, defaults, range/enum/map validation, serialization, and secret-bearing property bans.
  - [x] Server authorization tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs` for policy-admin/tenant-admin allow, operations/mailbox/compliance admin deny, service/AI deny, invalid payload deny, and two-person distinct-actor checks.
  - [x] AI policy tests near `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionPolicyEvaluatorTests.cs` for per-class map behavior and safe routing.
  - [x] Gateway/audit tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` for audit-unavailable fail-closed policy mutations and metadata-only refs.
  - [x] UI/bUnit contract tests under `tests/Hexalith.ChatBot.UI.Tests` for S5 validation summary, `aria-invalid`/`aria-describedby`, focus target, disabled explanation, save conflict cause, small-screen fallback, and localization coverage.
  - [x] Architecture/conformance tests if new public surfaces, commands, or client adapters are added.

## Dev Notes

### Scope Boundaries

- Story 7.2 implements policy-admin scope behavior, closed Tenant Policy Schema editing, sensitive-change two-person approval, and AI action policy configuration. It is not the full mailbox admin surface, operational queue management, notification routing, escalation policies, rate limiting, lifecycle completion, compliance investigation, retention/export/deletion, or command allowlist v1 workflow.
- `tenant-admin` remains the union role from Story 7.1, but it is still not a project-content or mailbox-content superuser. Policy editing must not grant visibility into project evidence, file metadata, mailbox bodies, provider payloads, or sensitive audit detail.
- Policy schema is product-owned and closed. Tenants can set declared values only; they cannot add arbitrary knobs, custom JSON, custom risk classes, or custom command permissions.
- Security-sensitive changes are not active until a distinct second human admin with policy authority approves. Audit and justification are part of the state transition, not optional UI decoration.
- No subscription-tier behavior belongs here. Policy behavior is determined by tenant policy and role/scope, not billing package.

### Existing Code To Reuse

- `src/Hexalith.ChatBot.Contracts/Enums/AdminRole.cs`, `AdminRoles.cs`, `AdminScope.cs`, `AdminScopes.cs` - finite admin role/scope model from Story 7.1; `PolicyAdmin` maps to `AdminScope.Policy`.
- `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` - human-only admin scope evaluation. Extend this path instead of adding duplicate role parsing.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - current policy-scope check for `SetAssociationConfidenceThresholds` and human-only admin mutation validation. Add policy mutation/approval command validation here.
- `src/Hexalith.ChatBot.Contracts/Commands/SetAssociationConfidenceThresholds.cs`, `AssociationThresholdPolicySnapshot.cs`, and `src/Hexalith.ChatBot.Server/Association/Scoring/AssociationThresholdPolicyValidator.cs` - existing threshold policy contract/range handling. Fold into the broader schema model without weakening `T_high`/`T_low` validation.
- `src/Hexalith.ChatBot.Contracts/Enums/AiActionRiskActionClass.cs` - exact six FR41 action-class tokens. Reuse these for the `ai-action.low-risk-allowed` map.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/DefaultAiActionPolicyEvaluator.cs` and `ITenantAiPolicySnapshotProvider.cs` - current AI policy evaluation seam. Extend it to consume versioned per-class snapshots.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`, `AuditMetadata.cs`, `ChatBotStateWritingPathInventory.cs`, and `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` - existing metadata-only audit refs and pre-commit fail-closed path.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` - add new command types only after validator/audit/test coverage exists.
- `src/Hexalith.ChatBot.UI/Design/ChatBotValidationErrorContract.cs`, `ChatBotSaveConflictCause.cs`, `ChatBotRecoveryPatternContract.cs`, `ChatBotSmallScreenFallbackContract.cs`, and governed UI primitives under `src/Hexalith.ChatBot.UI/Components/Governed/` - S5 UI should reuse the current accessibility/recovery contracts.
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and `tests/fixtures/hexalith-chatbot-generated-client.sha256` - update/regenerate only if public contracts change.

### Current State To Preserve

- Story 7.1 already implemented bounded admin roles/scopes and fixed a prior overgrant so `mailbox-admin` does not have `AdminScope.Operate`. Do not regress those mappings.
- `ParticipantAuthorizationStage` already allows human `policy-admin` and `tenant-admin` for `SetAssociationConfidenceThresholds`, and denies service/AI actors with tenant-admin-looking claims. Generalize this invariant to all policy mutations.
- `DefaultAiActionPolicyEvaluator` currently routes unsafe or unavailable low-risk policy states to approval. Keep this fail-safe behavior while changing from a global low-risk flag to a per-class map.
- `AuditEnvelopeFactory` emits safe metadata refs for admin operations. Preserve the safe-token discipline and extend refs rather than emitting raw policy bodies or JSON diffs.
- `CommandGateway` suppresses dispatch when pre-commit audit cannot be written. Policy mutation and activation must reuse this path.
- UI currently has governed primitives, localization, validation/recovery contracts, responsive/touch contracts, and live-region deduplication. Use them for S5; do not introduce a second design system or bypass localization.
- Existing root submodule policy applies: initialize/update only root `.gitmodules` submodules and never use recursive submodule commands.

### Architecture Guardrails

- Contracts belong in `src/Hexalith.ChatBot.Contracts`; generated client output belongs only in `src/Hexalith.ChatBot.Client/Generated`; server authorization/schema evaluation belongs in `src/Hexalith.ChatBot.Server/Gateway/Stages` or `src/Hexalith.ChatBot.Server/Governance/Policy`; audit refs stay in `src/Hexalith.ChatBot.Server/Audit`; UI S5 work belongs in `src/Hexalith.ChatBot.UI`.
- Use records/enums/finite token helpers, not free-form strings or arbitrary dictionaries, for schema ids, sensitivity, approval status, reason codes, action classes, strictness values, and unsafe-attachment handling.
- Tenant IDs must come from authenticated gateway binding. Request bodies, policy ids, route/query params, UI state, and snapshot ids are not authorization proof.
- Treat missing, stale, expired, invalid, or unauthorized policy snapshots as route-to-approval or fail-closed according to the operation. Never broaden access because policy is unavailable.
- Policy snapshots are immutable historical records. New changes produce new versions with supersession links; old approvals and snapshots remain reconstructable.
- If public OpenAPI/client shapes change, update the OpenAPI spine, regenerate the client, refresh the checksum, and include the client tests in validation.

### UX Guardrails

- S5 Tenant Configuration is a work surface for admins, not a marketing page. Keep it dense, structured, and scannable.
- Validation summary appears before fields; invalid fields carry `aria-invalid` and reference field messages via `aria-describedby`.
- Disabled controls need a reachable explanation. Tooltip-only and default non-focusable disabled buttons are insufficient.
- Save conflicts must name only safe categories: policy, permission, or stale data. Do not surface raw exceptions.
- On phone, dense editing can be unavailable only if a read-only summary/status, safe available actions, and a reachable explanation remain.
- Use semantic status consistently: warning for approval-required/stale evidence, danger for terminal failure/policy denial, success only after save/approval is actually accepted.

### Previous Story Intelligence

- Story 7.1 established `tenant-admin` as a union role and finer roles as proper subsets. `PolicyAdmin` has `AdminScope.Policy` and audit obligation, but not operate scope.
- Story 7.1 review fixed three defects that this story must not repeat: role/scope overgrant, accepting empty audit-obligation fields, and accepting unsafe affected refs. Apply the same strict validation to policy change ids, knob refs, reason codes, snapshot ids, and justification fields.
- Story 6.5 established the OpenAPI/generated-client/checksum workflow and metadata-only audit refs for policy-like strictness settings. Reuse that workflow if Story 7.2 changes public contracts.
- Epic 4/6 AI and outbound stories established that risky AI or boundary-crossing behavior must route through proposal/approval instead of direct execution. Policy editing must tune this behavior only through explicit schema snapshots.

### Latest Technical Specifics

- No external version research is required for implementation. Use the repo-pinned stack and do not upgrade packages as part of this story: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, Blazor/FrontComposer, and Fluent UI v5 RC as already pinned in the repo and architecture.
- Do not change target frameworks, Aspire/Dapr topology, Fluent UI, Fluxor, NSwag/client generation tooling, MCP SDK, or submodule pointers unless a compile-time contract regeneration command requires generated client output.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` if S5 UI/components/design contracts change.
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` if actor isolation, policy command surfaces, or cross-surface behavior changes.
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` if new boundaries are introduced.
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` if OpenAPI/generated client changes.
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, and metadata-only test fixtures.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`, including Epic 7 and Story 7.2.
- Loaded `prd_content` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` and `addendum.md`, especially FR41, FR52, FR75d, RBAC, Tenant Policy Schema, NFR48, and two-person rule language.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially CommandGateway, audit, project structure, FR-to-structure mapping, and testing strategy.
- Loaded `ux_content` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`, `DESIGN.md`, and `review-accessibility.md`, especially S5 Tenant Configuration, validation, focus, responsive, and disabled-control rules.
- Loaded persistent project-context facts from sibling `project-context.md` files, with relevant constraints from `Hexalith.Tenants`, `Hexalith.FrontComposer`, and `Hexalith.EventStore`.
- Loaded previous story intelligence from `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md`.
- Inspected current source anchors: admin roles/scopes, admin authority evaluation, participant authorization, association threshold policy, AI action policy evaluator, audit envelope factory, command allowlist, UI validation/recovery contracts, localization, and existing admin/AI policy tests.
- Reviewed recent git history: `1745611 feat(story-7.1): Add bounded tenant admin permissions`, `2297fe9 feat(story-6.5): Disambiguate delegated and external senders`, and preceding outbound/authenticity commits.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 7 and Story 7.2 source acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR41, FR52, FR75d, RBAC Matrix, Service Client Permissions, NFR48.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Tenant Policy Schema and Shared Command Pipeline.
- `_bmad-output/planning-artifacts/architecture.md` - CommandGateway, audit model, project structure, testing strategy.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - S5 Tenant Configuration, accessibility, recovery, responsive behavior.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - Fluent UI/FrontComposer design spine and semantic status rules.
- `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md` - current admin scope implementation context and review fixes.
- `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` - policy admin scope evaluation.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` - gateway authorization validation.
- `src/Hexalith.ChatBot.Server/Governance/AiMediation/DefaultAiActionPolicyEvaluator.cs` - AI policy evaluation seam.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` - metadata-only audit evidence refs.
- `src/Hexalith.ChatBot.UI/Design/ChatBotValidationErrorContract.cs` - S5 validation summary and field association contract.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-02: Resolved dev-story workflow customization and loaded project/story context.
- 2026-06-02: Ran `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` after implementation and after OpenAPI/client regeneration.
- 2026-06-02: Ran compiled xUnit v3 in-process runners for Contracts, Server, UI, Conformance, Architecture, and Client test projects with `-parallel none`.
- 2026-06-02: Ran story-automator review workflow, applied automatic fixes, and reran build plus focused compiled xUnit runners including UI E2E.

### Completion Notes List

- Added a closed, versioned Tenant Policy Schema contract set with finite knob ids, value shapes, sensitivity classification, M0 defaults, M1 preview metadata, and validation for unknown ids, ranges, enum values, AI action-class maps, safe ids, and bounded payloads.
- Added governed `SubmitTenantPolicyChange` and `ApproveTenantPolicyChange` command contracts, OpenAPI schemas, generated client refresh, and generated-client checksum update.
- Extended gateway policy authorization to require human `AdminScope.Policy`, deny service/AI actors, validate closed-schema knob refs and safe audit metadata before dispatch, and preserve tenant binding as gateway-owned context.
- Routed policy commands through the existing `CommandGateway`/`AcceptedCommandDispatcher`/`GovernedOperationAggregate` path; sensitive changes create pending two-person approvals and non-sensitive changes activate immutable snapshots.
- Extended metadata-only audit refs for policy submit/approve operations without including full policy JSON, project names, mailbox bodies, provider payloads, raw claims, headers, tokens, or secrets.
- Replaced coarse AI low-risk evaluation with per-action-class policy checks while preserving compatibility for existing global `LowRiskAllowed` snapshots.
- Added Tenant Configuration S5 policy editor component/contract coverage for validation summary placement, field error association, disabled-action explanation, safe conflict causes, and phone fallback.
- Review fixes applied: duplicate policy knob ids now return validation errors instead of throwing; policy schema versions are finite at authorization and aggregate validation; policy audit refs now include old/new value fingerprints.
- Validation passed: build; Contracts.Tests; Server.Tests; UI.Tests; UI.E2E.Tests; Conformance.Tests; Architecture.Tests; Client.Tests.

### File List

- _bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs
- src/Hexalith.ChatBot.Contracts/Commands/ApproveTenantPolicyChange.cs
- src/Hexalith.ChatBot.Contracts/Commands/SubmitTenantPolicyChange.cs
- src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyContracts.cs
- src/Hexalith.ChatBot.Contracts/Enums/TenantPolicyApprovalStatus.cs
- src/Hexalith.ChatBot.Contracts/Enums/TenantPolicyKnobSensitivity.cs
- src/Hexalith.ChatBot.Contracts/Enums/TenantPolicyKnobType.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs
- src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs
- src/Hexalith.ChatBot.Server/Governance/AiMediation/DefaultAiActionPolicyEvaluator.cs
- src/Hexalith.ChatBot.Server/Governance/AiMediation/ITenantAiPolicySnapshotProvider.cs
- src/Hexalith.ChatBot.Server/Governance/Policy/TenantPolicyEvents.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs
- src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor
- src/Hexalith.ChatBot.UI/Design/ChatBotTenantPolicyEditorContract.cs
- tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionPolicyEvaluatorTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs
- tests/Hexalith.ChatBot.UI.E2E.Tests/TenantPolicyEditorE2ETests.cs
- tests/Hexalith.ChatBot.UI.Tests/ChatBotTenantPolicyEditorContractTests.cs
- tests/fixtures/hexalith-chatbot-generated-client.sha256

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-02

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- HIGH: Duplicate tenant policy knob ids could throw in `TenantPolicySchema.Validate` before returning a clean validation denial. Fixed by building the comparison map after duplicate detection and added contract/gateway coverage.
- HIGH: Policy commands accepted any safe-looking schema version string, weakening the closed versioned schema contract. Fixed by adding `TenantPolicySchemaVersions.IsKnown` and enforcing it in gateway authorization and aggregate validation.
- MEDIUM: Policy submit audit refs did not include old/new value fingerprints required for reconstructable metadata-only audit. Fixed by emitting `policy-old-fingerprint:*` and `policy-new-fingerprint:*` refs and adding gateway audit assertions.
- MEDIUM: Story File List omitted `tests/Hexalith.ChatBot.UI.E2E.Tests/TenantPolicyEditorE2ETests.cs`; added it after validating the compiled UI E2E runner.

Git/story discrepancy noted:

- `Hexalith.Tenants` has an existing root submodule pointer change in the worktree. It was not reviewed as application source for Story 7.2 and was left unchanged per repository safety rules.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none`
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`

### Change Log

- 2026-06-02: Implemented Story 7.2 policy-admin scope, closed Tenant Policy Schema, two-person policy approval, per-class AI action policy, S5 policy editor contracts, OpenAPI/client refresh, and focused tests.
- 2026-06-02: Story-automator review applied schema-validation, schema-version, audit-fingerprint, and story File List fixes; status moved to done.
