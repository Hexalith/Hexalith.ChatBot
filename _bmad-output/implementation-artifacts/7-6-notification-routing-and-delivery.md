---
baseline_commit: 7e7b3b7
---

# Story 7.6: Notification routing and delivery

Status: review

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As a tenant administrator,
I want review/approval/failure/degraded/quarantine/retry states to notify the right authorized users through configurable routing,
so that the people who can act are alerted to what needs attention.

## Acceptance Criteria

1. Given a workflow item enters or remains in a notify-worthy state class — exactly `review-needed`, `approval-pending`, `failure`, `degraded`, `quarantine`, or `retry` — when the routing engine evaluates it, then a notification is produced and delivered through the channel configured for that `(state-class × scope)` mapping, and each notification carries only metadata-safe fields (tenant ref, state class, item ref, queue/workflow ref, reason code, correlation id, UTC raised-at) — never project names, mailbox content, evidence, file metadata, audit reasons, provider payloads, prompts, command bodies, claims, headers, tokens, or secrets. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR72`; `src/Hexalith.ChatBot.Server/Audit/OperatorAlert.cs`; `src/Hexalith.ChatBot.Server/Audit/OperatorAlertKind.cs`]
2. Given recipient resolution runs for a produced notification, when the candidate recipient set is computed, then it is scoped to users who hold authority over the affected item: see-only/aggregate notifications may reach admins with the relevant tenant-wide scope, but any notification carrying item-specific context is delivered only to recipients with per-project (or equivalent per-resource) authority over that item, resolved through the existing authority/redaction path rather than a new bespoke check. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR72`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs`; `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`]
3. Given a recipient lacking authority over the affected item, when a notification for that item would otherwise be delivered, then that recipient never receives restricted project detail through the notification: they receive a safe metadata-only/redacted form (or no notification) with no resource-existence leakage, mirroring the user-facing redaction discipline already applied to failure responses. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs`; `src/Hexalith.ChatBot.Contracts/Enums/ComplianceAuditRedactionState.cs`]
4. Given a tenant administrator edits notification routing configuration, when the change is submitted, then routing is modeled as a closed, typed map of `(state-class × scope) → recipient-role/channel` whose state classes, scopes, recipient roles, and channels are finite enum/token sets (not free-form strings after the trust boundary), the change is accepted only through the governed command spine, and each edit records actor (requester ref), old value, new value, reason code, source version, policy snapshot id, schema version, and UTC timestamp. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR73`; `src/Hexalith.ChatBot.Contracts/Commands/SubmitMailboxConfigurationChange.cs`; `src/Hexalith.ChatBot.Contracts/Commands/SubmitTenantPolicyChange.cs`]
5. Given routing configuration values, when they are validated, then they are bounded by the Tenant Policy Schema: recipient roles must be declared admin/tenant roles, channels must be declared channel tokens, state classes must be the six declared classes, and values outside the declared types/ranges are rejected with a safe reason code — tenants cannot introduce new state classes, channels, or recipient roles. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR73`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`; `src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyContracts.cs`; `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md#Current State To Preserve`]
6. Given a routing-configuration edit reaches the gateway, when pre-commit audit is unavailable, then the gateway fails closed and writes no durable routing state. When audit succeeds, the audit envelope carries metadata-only refs: admin role/scope, routing-edit operation, affected `(state-class × scope)` map keys, old/new fingerprints, reason code, policy snapshot id, source version, correlation id, and outcome — and the same fail-closed/audit discipline applies to any durable record produced when a notification is delivered. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75g`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`; `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`]
7. Given non-human actors (service clients, AI actors, mailbox events, CLI/MCP automation without delegated human authority) or admins lacking the routing-config authority scope attempt to edit routing configuration, when authorization runs, then they are denied before state load with a safe reason code and no resource-existence leakage; routing-config edit requires a human admin holding the routing scope (`AdminScope.Policy`, held by `policy-admin` and `tenant-admin`), consistent with the Tenant-Policy-Schema-bounded nature of the routing map. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`; `src/Hexalith.ChatBot.Contracts/Enums/AdminScopes.cs`]
8. Given public commands, queries, DTOs, or generated clients change for notification routing configuration or routing read-back, then the OpenAPI contract spine is updated first, `HexalithChatBotClient.g.cs` is regenerated rather than hand-edited, the generated-client checksum is refreshed, and contract/client tests prove schema parity. If no public endpoint/schema is added (the generic command-submission transport remains the only public spine), then the OpenAPI/client/checksum are intentionally left unchanged and this is stated in completion notes. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/implementation-artifacts/7-5-operational-queue-management.md#Completion Notes List`; `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`; `tests/fixtures/hexalith-chatbot-generated-client.sha256`]
9. Given acceptance coverage runs, then tests prove: all six state classes route to their configured recipient/channel; recipient resolution is scoped to authority over the affected item; an unauthorized recipient receives only safe/redacted notification content with no existence leakage; routing-edit records actor/old/new/reason/timestamp and is schema-bounded (out-of-range values rejected); policy-admin/tenant-admin allow and mailbox-admin/compliance-admin/operations-admin/service/AI/non-human deny for routing-config edits; audit-unavailable fail-closed on routing edit; metadata-only audit/notification refs (secret-bearing fields banned); and OpenAPI/client drift only if public contracts change. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60-NFR70`; `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`; `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`]

## Tasks / Subtasks

- [x] Add finite notification routing contracts and validators (AC: 1, 4, 5, 8, 9)
  - [x] Add finite enums under `src/Hexalith.ChatBot.Contracts/Enums`: `NotificationStateClass` (review-needed, approval-pending, failure, degraded, quarantine, retry) and its wire-token companion (`NotificationStateClasses`), and `NotificationChannel` + `NotificationChannels` for the declared channel token set. Model recipient role against the existing `AdminRole`/`AdminRoles`; do not invent a parallel role enum.
  - [x] Add the routing-map contract (e.g. `NotificationRoutingContracts.cs` under `src/Hexalith.ChatBot.Contracts/Queries` and/or `Commands`): a closed, typed map of `(state-class × scope) → { recipient-role, channel }` plus snapshot metadata (snapshot id, schema version, source version, fingerprint). Map keys/values are enums/tokens only — never free-form strings after the trust boundary.
  - [x] Add the governed edit command `SubmitNotificationRoutingChange` implementing `IChatBotCommand`, mirroring `SubmitMailboxConfigurationChange`/`SubmitTenantPolicyChange` field shape: change id, source/proposed snapshot ids, `SourceVersion`, typed `ChangeSet`, `ReasonCode`, `RequesterRef`, `SchemaVersion`, `CorrelationId`, `OldFingerprint`, `NewFingerprint`. Tenant + human admin authority come from the gateway, not the payload.
  - [x] Validate: all map keys are among the six state classes × declared scopes; recipient roles are declared `AdminRole` values; channels are declared `NotificationChannel` tokens; reject empty reason/snapshot/schema/fingerprint fields, unsafe refs, non-positive/negative source versions, stale source version, and any value outside the Tenant Policy Schema's declared types/ranges.
  - [x] Keep all contracts metadata-only. Routing config addresses roles/channels/state-classes; it must not embed project content, recipient PII beyond safe role/ref tokens, or provider/channel secrets.
- [x] Add the routing/recipient-resolution engine reusing existing authority + redaction (AC: 1, 2, 3, 9)
  - [x] Add a server-side `NotificationRoutingResolver` (under `src/Hexalith.ChatBot.Server/Governance/Admin` or a sibling `Notifications` folder) that, given a notify-worthy state event, looks up the configured `(state-class × scope)` mapping and produces the recipient set + channel.
  - [x] Resolve recipient authority through the existing path — `AdminAuthorityEvaluator` for scope-based/aggregate recipients and the per-project/per-resource authority check used by `ComplianceAuditReadPolicy`/queue read policy — rather than a new bespoke authority check. An item-specific notification is delivered with item context only to recipients with per-resource authority over that item.
  - [x] For recipients without per-item authority, downgrade to a safe metadata-only/redacted notification (or suppress) using the same discipline as `CoarseUserFacingRedactionStage` and `ComplianceAuditRedactionState`. No resource-existence leakage; redaction must be indistinguishable from safe-not-found.
  - [x] Keep tenant identity from authenticated gateway binding / `ChatBotTenantBinding`; never trust tenant ids from item refs, channel config, recipient refs, or correlation ids.
- [x] Wire delivery through the existing operator-alert seam, metadata-only (AC: 1, 3, 6, 9)
  - [x] Extend the existing alert/delivery seam (`IOperatorAlertSink`, `OperatorAlert`, `OperatorAlertKind`, `InMemoryOperatorAlertSink`) or add a parallel `INotificationSink` following the same metadata-only record shape (kind/state-class, reason code, tenant ref, item/queue ref, channel, correlation id, UTC raised-at). Do not introduce a sink that carries restricted content.
  - [x] Add notification state classes to the alert-kind model if reusing `OperatorAlertKind`, or map the six state classes to delivery records explicitly. Preserve the existing operator-alert behavior used for audit-unavailable/retry-exhausted/dependency-degraded.
  - [x] If notification delivery produces durable state, route it through the command spine and reuse `CommandGateway` pre-commit fail-closed audit. Do not add a direct-write delivery path from workers, projections, UI, CLI, MCP, or service clients.
- [x] Extend authorization and audit through existing gateway seams (AC: 4, 6, 7, 9)
  - [x] Add `SubmitNotificationRoutingChange` validation to `ParticipantAuthorizationStage` requiring a human admin with `AdminScope.Policy` (held by `policy-admin` and `tenant-admin`). Deny mailbox-admin, compliance-admin, operations-admin, service clients, AI actors, and non-human surfaces even if they carry a `tenant-role` claim.
  - [x] Add reason-code constants for routing-config denial/validation to `ChatBotAuthorizationReasonCodes` and a catalog entry to `ChatBotMessageCatalog` (stable code, ≤80-char headline, one-sentence safe reason, safe next action).
  - [x] Extend `AuditEnvelopeFactory` with safe routing refs such as `admin-operation:notification-routing-edit`, `admin-scope:policy`, `notification-state-class:<token>`, `notification-channel:<token>`, `recipient-role:<token>`, `routing-snapshot:<safe-id>`, `policy-snapshot:<safe-id>`, `reason:<safe-code>`, old/new fingerprints. Emit refs, never raw routing payloads or recipient addresses.
  - [x] Add `SubmitNotificationRoutingChange` to `ChatBotSpineCommandAllowlist` only after validator, audit refs, dispatch/projection, and tests exist.
  - [x] Preserve `CommandGateway` pre-commit fail-closed: no durable routing state when audit is unavailable.
- [x] Build routing-config projection/read-back and bound it by the Tenant Policy Schema (AC: 4, 5, 9)
  - [x] Persist routing config as governed events (mirror `TenantPolicyEvents`) and project the current `(state-class × scope) → recipient/channel` snapshot with snapshot id, source version, and fingerprint for optimistic concurrency.
  - [x] Enforce schema bounds at validation and projection time: reject any persisted/queried value referencing an undeclared state class, channel, or recipient role. Read-back is summary-safe (roles/channels/state-classes), not recipient PII.
  - [x] Provide a read policy gating routing-config read-back to human admins with the relevant scope (`AdminScope.Policy`/see-only), reusing the read-policy pattern, not a new bespoke gate.
- [x] Add or extend the notification-routing admin UI surface (AC: 4, 5, 9)
  - [x] Reuse the existing governed/Fluent UI patterns and the Tenant Policy Schema editor surface from Story 7.2 (`ChatBotTenantPolicyEditorContractTests` anchor). Do not introduce a new design system, marketing page, raw JSON editor, or hover-only critical actions.
  - [x] Render the routing matrix as a bounded grid of `(state-class × scope)` rows with role/channel selectors drawn from the declared enums, an active-change summary, reason-code entry, and a submit action that flows through the governed command. Show old→new diff on submit.
  - [x] Add `ChatBotUiTextKey` entries plus `SharedResource.resx` / `SharedResource.fr.resx` strings for all visible English/French text. Keep stable machine codes, reason codes, channel/state-class/role tokens, and correlation ids untranslated.
  - [x] On submit success move focus to the success status; on validation/audit/authorization failure keep focus in the editor with the safe reason reachable. Reflow to labelled rows on small screens without dropping state class, scope, role, channel, or reason.
- [x] Update public contract spine only if routing surfaces are public (AC: 8, 9)
  - [x] If new routing query/command DTOs become public, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` first, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` (never hand-edit), refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`, and add/extend client schema parity tests.
  - [x] If routing rides the existing generic command-submission transport with no new public endpoint/schema, leave OpenAPI/client/checksum unchanged and state this explicitly in completion notes (as Story 7.5 did).
- [x] Add focused tests (AC: all)
  - [x] Contract tests near `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` for state-class/channel tokens, routing-map closure, `SubmitNotificationRoutingChange` validation, schema-bound rejection of undeclared values, secret-bearing property bans, and OpenAPI schema parity if public.
  - [x] Server authorization tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/` for policy-admin/tenant-admin allow; mailbox-admin/compliance-admin/operations-admin deny; service/AI/non-human deny; invalid/stale payload deny; safe reason codes.
  - [x] Gateway/audit tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` for audit-unavailable fail-closed routing edit and metadata-only audit refs.
  - [x] Routing-resolver/redaction tests (new, near `tests/Hexalith.ChatBot.Server.Tests/Governance` or `Projections`) for all six state classes routing to configured recipient/channel, recipient scoping to per-item authority, unauthorized-recipient redaction with no existence leakage, and schema-bound projection.
  - [x] UI/bUnit tests under `tests/Hexalith.ChatBot.UI.Tests` (anchor: `ChatBotTenantPolicyEditorContractTests`) for the routing matrix, bounded selectors, reason entry, submit/focus behavior, small-screen reflow, localization, and absence of restricted markers.
  - [x] Conformance/architecture/client tests if new public surfaces, command/query shapes, actor isolation, or module boundaries change.

## Dev Notes

### Scope Boundaries

- Story 7.6 implements (a) the routing/recipient-resolution + delivery path that notifies authorized users when the six notify-worthy state classes require attention (FR72), and (b) the governed, schema-bounded routing-configuration editor that maps `(state-class × scope) → recipient-role/channel` and records actor/old/new/timestamp (FR73).
- It does NOT implement: escalation policy for unresolved/aged states (7.7 — uses the same FR73 routing map but fires on age/severity), approval-queue prioritization/grouping (7.8), notification throttling / digest rollup (7.9), reviewer-backlog alerting (7.10), rubber-stamp-rate observable (7.11), disable/quarantine/rate-limit governance controls (7.12-7.26), command allowlist v1 / lifecycle completion (7.27), or M2 operational dashboards/SLO/alert wiring (Epic 8).
- Throttling and digest rollup are explicitly out of scope: 7.6 delivers a notification per notify-worthy state per the routing map; rollup/dedup arrives in 7.9. Keep the delivery seam simple and per-event so 7.9 can layer on it.
- Notification routing is governance metadata (which role/channel hears about which state class). It must never become a covert channel for project content, mailbox content, evidence, file metadata, audit reasons, or provider/AI payloads.

### Existing Code To Reuse

- Delivery seam: `src/Hexalith.ChatBot.Server/Audit/OperatorAlert.cs`, `OperatorAlertKind.cs`, `IOperatorAlertSink.cs`, `InMemoryOperatorAlertSink.cs` — existing metadata-only operator-alert infrastructure (Kind, ReasonCode, TenantId, CommandName, CorrelationId, RaisedAt). Extend or parallel this rather than inventing a content-bearing notification record.
- Config-edit command pattern: `src/Hexalith.ChatBot.Contracts/Commands/SubmitMailboxConfigurationChange.cs` and `SubmitTenantPolicyChange.cs` (+ `ApproveTenantPolicyChange.cs`) — snapshot-id / source-version / typed-change-set / reason-code / requester-ref / schema-version / correlation-id / old+new fingerprint shape. Mirror this exactly for `SubmitNotificationRoutingChange`.
- Policy schema model: `src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyContracts.cs`, `Enums/TenantPolicyKnobType.cs`, `TenantPolicyKnobSensitivity.cs`, `TenantPolicyApprovalStatus.cs`, and server events `src/Hexalith.ChatBot.Server/Governance/Policy/TenantPolicyEvents.cs` — the closed, versioned schema the routing map must be bounded by.
- Admin role/scope model: `src/Hexalith.ChatBot.Contracts/Enums/AdminRole.cs`, `AdminRoles.cs`, `AdminScope.cs`, `AdminScopes.cs`. Routing-config edit uses `AdminScope.Policy` (held by `policy-admin` and `tenant-admin` per `AdminScopes.ScopesForRole`). Recipient roles in the routing map are `AdminRole` values.
- Authorization + audit spine: `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` (central command authorization; already validates `SubmitTenantPolicyChange`/`SubmitMailboxConfigurationChange`), `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` (pre-commit fail-closed), `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`, `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (metadata-only refs).
- Authority + redaction (NFR2): `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` (human-only scope checks), `src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs` (per-project authority → redacted vs full + escalation pattern), `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs` + `IUserFacingRedactionStage.cs`, `src/Hexalith.ChatBot.Contracts/Enums/ComplianceAuditRedactionState.cs`. Reuse these for recipient resolution; do not write a new authority check.
- Safe text: `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`, `src/Hexalith.ChatBot.Server/Audit/AuditFailureReasonCodes.cs`.
- UI: Tenant Policy Schema editor surface from Story 7.2 (test anchor `tests/Hexalith.ChatBot.UI.Tests/ChatBotTenantPolicyEditorContractTests.cs`), governed components under `src/Hexalith.ChatBot.UI/Components/Governed/`, `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`, `SharedResource.resx`, `SharedResource.fr.resx`.
- Public contract spine (only if routing goes public): `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, `tests/fixtures/hexalith-chatbot-generated-client.sha256`.

### Current State To Preserve

- Story 7.1 fixed admin role/scope overgrant. Routing-config edit must NOT widen scope: only `AdminScope.Policy` holders (`policy-admin`, `tenant-admin`) edit routing. `operations-admin`, `mailbox-admin`, `compliance-admin` must not gain routing-edit power.
- `ParticipantAuthorizationStage` already gates `SubmitTenantPolicyChange`/`SubmitMailboxConfigurationChange` on human admin scope with safe refs and reason codes. Follow the identical shape for `SubmitNotificationRoutingChange`; do not relax existing checks.
- `OperatorAlert`/`IOperatorAlertSink` is intentionally metadata-only (no content). New notification delivery must keep this metadata-only invariant.
- `CommandGateway` suppresses dispatch when pre-commit audit cannot be written. Routing edits (and any durable delivery record) reuse this fail-closed path.
- `AuditEnvelopeFactory` emits safe refs only. Extend refs; never emit raw routing maps, recipient addresses, or channel secrets.
- `ChatBotSpineCommandAllowlist` is a fail-closed allowlist — add `SubmitNotificationRoutingChange` only after validation/audit/tests exist.
- Existing root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Architecture Guardrails

- Contracts in `src/Hexalith.ChatBot.Contracts`; generated client only in `src/Hexalith.ChatBot.Client/Generated`; server authorization in `src/Hexalith.ChatBot.Server/Gateway/Stages` or `Governance/Admin`; routing engine/projection in `src/Hexalith.ChatBot.Server` (Governance or Projections); audit refs in `src/Hexalith.ChatBot.Server/Audit`; UI in `src/Hexalith.ChatBot.UI`.
- Every routing-config mutation follows `auth → tenant-bind → authorize → risk-classify → approval-gate → coarse-idempotency → pre-commit-audit → EventStore execute/publish/projection → post-commit-audit`. No direct-write routing path.
- Tenant id comes from authenticated gateway binding; item refs, recipient refs, channel config, route/query params, UI state, and correlation ids are comparison inputs only.
- Use typed records/enums and finite token validators. No raw JSON routing blobs, SQL-like filters, delimited fields, or user-provided expressions.
- The routing map is closed and schema-bounded: state classes, channels, and recipient roles are finite declared sets. Tenants pick values within the schema; they cannot define new keys.
- Time/age fields use server-side UTC; tenant-local formatting only at presentation boundaries.
- NFR2 is the spine of this story: a recipient lacking authority over an item must be unable to distinguish "redacted" from "does not exist." Reuse the compliance/queue redaction discipline; do not hand-roll.

### UX Guardrails

- The routing editor is a dense governance config surface, not a landing page. Render a bounded `(state-class × scope)` matrix with role/channel selectors from declared enums, visible active-change summary, reason-code entry, and a governed submit with old→new diff.
- Plain-language labels precede raw tokens; tokens remain available as metadata. One primary submit action; destructive/secondary actions grouped with reachable disabled-action explanations.
- On submit success move focus to success status; on rejection keep focus in the editor with the safe reason reachable. Reflow to labelled rows on small screens without dropping state class, scope, role, channel, or reason.
- English/French visible text uses existing localization patterns. Stable machine codes, reason codes, channel/state-class/role tokens, and correlation ids stay untranslated.
- Notifications themselves are operational signals, not marketing — safe headline ≤80 chars, one-sentence reason that never names unauthorized projects/files/parties/audit detail (NFR2), with a safe next action.

### Previous Story Intelligence

- Story 7.1 established the bounded admin role/scope model (`tenant-admin` = union of FR75b–FR75g; finer roles are proper subsets), metadata-only admin refs, and fail-closed audit. Reuse the scope model; do not reintroduce overgrant.
- Story 7.2 established the closed, versioned Tenant Policy Schema, two-person rule on sensitive knobs, safe token/fingerprint checks, snapshot-based edit commands (`SubmitTenantPolicyChange`/`ApproveTenantPolicyChange`), the S5 schema-editor UI, and metadata-only audit refs. Routing config is a schema-bounded knob set — reuse this edit/validation/UI pattern. Note: routing role/channel mapping is a standard policy mutation; apply the two-person rule only to map entries the schema flags security-sensitive (don't add a blanket second-approval to all routing edits unless the schema requires it).
- Story 7.3 established metadata-only mailbox config/health and protected provider material via `SubmitMailboxConfigurationChange` — the closest structural template for `SubmitNotificationRoutingChange`.
- Story 7.4 established compliance audit reads with per-project redaction/escalation (`ComplianceAuditReadPolicy`, `ComplianceAuditRedactionState`) — the authority/redaction pattern recipient resolution must reuse for NFR2.
- Story 7.5 established the six operational queue families and the operator-facing surface; its review fixes (pagination token actually applied, deterministic/process-independent fingerprinting via SHA-256, File List accuracy) are recurring traps — make routing fingerprints deterministic (SHA-256 over a canonical representation, not `GetHashCode()`), and keep the File List exact.
- Recurring Epic 7 review defects to avoid: empty audit-obligation/reason fields, unsafe affected refs, relaxed authorization on new commands, and forgetting to add the new command to the spine allowlist after (not before) validation/audit/tests.

### Latest Technical Specifics

- No external version research required. Use the repo-pinned stack; do not upgrade packages: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, Blazor/FrontComposer, Fluent UI v5 RC, Fluxor, existing OpenAPI/generated-client tooling.
- Do not change target frameworks, Aspire/Dapr topology, Fluent UI, Fluxor, NSwag/client generation tooling, MCP SDK, Graph permission posture, WORM audit assumptions, or submodule pointers unless a contract regeneration command requires generated client output.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` if routing UI/components/design contracts change.
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` if actor isolation or routing command/query surfaces change.
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` if new boundaries are introduced.
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` if OpenAPI/generated client changes.
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, metadata-only fixtures, and root-level submodule policy.

### Project Structure Notes

- New contracts land beside existing admin/policy/mailbox config contracts in `src/Hexalith.ChatBot.Contracts` (Enums, Commands, Queries). New server routing/resolver/projection code lands in `src/Hexalith.ChatBot.Server` under `Governance` (Admin or a new `Notifications` sibling), `Projections`, and `Audit`. UI lands in `src/Hexalith.ChatBot.UI`. No new top-level projects expected.
- No structural conflicts detected: routing config follows the same governed-edit shape as policy (7.2) and mailbox (7.3) config, and delivery reuses the existing operator-alert seam. Variance to watch: decide deliberately whether to extend `OperatorAlertKind`/`IOperatorAlertSink` in-place or add a parallel `INotificationSink`; either is acceptable if metadata-only and fail-closed, but state the choice in completion notes.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 7.6` — Notification routing and delivery acceptance criteria (FR72, FR73, NFR2).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` — FR72 (notify on attention-needed states), FR73 (configure routing/escalation), FR75d (policy scope, schema-bounded knobs, two-person rule), FR75g (audit obligation), NFR2 (redacted failure responses), NFR15a (fail-closed audit).
- `_bmad-output/planning-artifacts/architecture.md` — API & Communication Patterns (command spine, two-phase audit, fail-closed), Testing Strategy.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` and `DESIGN.md` — governed config surface, semantic status, focus model, responsive behavior, message-catalog discipline.
- `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md` — closed schema, snapshot-edit command, two-person rule, S5 editor, audit refs.
- `_bmad-output/implementation-artifacts/7-3-mailbox-admin-scope-and-mailbox-configuration.md` — `SubmitMailboxConfigurationChange` config-edit template and provider-leakage protections.
- `_bmad-output/implementation-artifacts/7-4-compliance-admin-scope.md` — per-project authority + redaction/escalation (NFR2) pattern.
- `_bmad-output/implementation-artifacts/7-5-operational-queue-management.md` — six state-class families, deterministic SHA-256 fingerprint lesson, File List accuracy lesson.
- `src/Hexalith.ChatBot.Server/Audit/OperatorAlert.cs`, `OperatorAlertKind.cs`, `IOperatorAlertSink.cs` — metadata-only delivery seam.
- `src/Hexalith.ChatBot.Contracts/Commands/SubmitMailboxConfigurationChange.cs`, `SubmitTenantPolicyChange.cs` — governed config-edit command shape.
- `src/Hexalith.ChatBot.Contracts/Enums/AdminRole.cs`, `AdminScopes.cs` — roles/scopes; `AdminScope.Policy` for routing edit.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`, `CommandGateway.cs`, `ChatBotSpineCommandAllowlist.cs`, `Audit/AuditEnvelopeFactory.cs` — authorization, fail-closed audit, allowlist, audit refs.
- `src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs`, `Gateway/Redaction/CoarseUserFacingRedactionStage.cs`, `Contracts/Enums/ComplianceAuditRedactionState.cs` — NFR2 authority/redaction reuse.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (1M context)

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — succeeded, 0 warnings / 0 errors.
- Contracts.Tests: 178 passed. Server.Tests: 565 passed. UI.Tests: 103 passed. Conformance.Tests: 75 passed. Architecture.Tests: 37 passed. Client.Tests: 17 passed (generated-client checksum unchanged).

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Implemented finite notification routing contracts: `NotificationStateClass`/`NotificationStateClasses` (six declared classes), `NotificationChannel`/`NotificationChannels`, the closed `(state-class × scope) → { recipient-role, channel }` map (`NotificationRoutingContracts`), the governed `SubmitNotificationRoutingChange` command (mirroring `SubmitMailboxConfigurationChange`), schema/validators, and the summary-safe read-back query contract. Recipient role reuses `AdminRole`; scope reuses `AdminScope`. No new parallel role enum.
- FR72 engine: `NotificationRoutingResolver` resolves the recipient set + channel for a notify-worthy state event, reusing `AdminAuthorityEvaluator` (added `HasHumanRole`) for scope/role audience and the existing project-owner per-resource authority check used across the gateway/queue/compliance read policies. Recipients without per-item authority are downgraded to a metadata-only/redacted delivery indistinguishable from safe-not-found (NFR2); aggregate/see-only events never carry item context. Schema-invalid maps fail closed (no deliveries).
- Delivery seam choice: added a **parallel `INotificationSink`** (`NotificationDelivery`, `InMemoryNotificationSink`) rather than overloading `OperatorAlertKind`, keeping the existing operator-alert behavior untouched. The new record is metadata-only (state-class/channel/role/scope/refs/reason/correlation/UTC) with no restricted content or recipient addresses. Per-event (no rollup) so Story 7.9 can layer digest/throttle on top. Both sinks registered in DI alongside `IOperatorAlertSink`.
- Authorization/audit: `ParticipantAuthorizationStage` now gates `SubmitNotificationRoutingChange` on a human admin with `AdminScope.Policy` (held by `policy-admin`/`tenant-admin`) plus full metadata-safe payload validation; denials use the new `notification_routing_unauthorized` reason (added to `ChatBotAuthorizationReasonCodes`, `ChatBotMessageCodes`, and `ChatBotMessageCatalog`). `AuditEnvelopeFactory` emits metadata-only refs (`admin-operation:notification-routing-edit`, `admin-scope:policy`, `notification-state-class:*`, `notification-channel:*`, `recipient-role:*`, `routing-snapshot:*`, old/new fingerprints). Command added to `ChatBotSpineCommandAllowlist` after validator/audit/tests existed. `CommandGateway` pre-commit fail-closed is preserved (verified by test).
- Read-back: `NotificationRoutingSnapshotProjector` builds the summary-safe `(state-class × scope) → role/channel` snapshot bounded by the routing schema (undeclared entries dropped; invalid snapshot → empty + `sha256:denied`). `NotificationRoutingReadPolicy` gates read-back to human `AdminScope.Policy` holders, reusing the existing read-policy pattern.
- UI: `ChatBotNotificationRoutingEditor.razor` + `ChatBotNotificationRoutingEditorContract` mirror the Story 7.2 Tenant Policy Schema editor surface — a bounded `(state-class × scope)` matrix with role/channel selectors drawn from the declared enums, validation summary, reason-code entry, governed submit with old→new diff, labelled-row small-screen reflow, and English/French `SharedResource` strings. No raw JSON editor, no hover-only critical actions, no restricted markers.
- **AC8 — no public contract drift:** notification routing rides the existing generic command-submission transport and the metadata-only read-back; no new public endpoint/schema was added, so `hexalith.chatbot.v1.yaml`, `HexalithChatBotClient.g.cs`, and `hexalith-chatbot-generated-client.sha256` are intentionally left unchanged (Client.Tests still pass). This mirrors Story 7.5.
- Routing fingerprints are validated as deterministic `sha256:` tokens (reusing the Story 7.3 mailbox fingerprint guard); no `GetHashCode()`-based fingerprints introduced.

### File List

- `_bmad-output/implementation-artifacts/7-6-notification-routing-and-delivery.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Contracts/Enums/NotificationStateClass.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/NotificationStateClasses.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/NotificationChannel.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/NotificationChannels.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/NotificationRoutingContracts.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/SubmitNotificationRoutingChange.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/NotificationRoutingContracts.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
- `src/Hexalith.ChatBot.Server/Notifications/NotificationContentVisibility.cs`
- `src/Hexalith.ChatBot.Server/Notifications/NotificationStateEvent.cs`
- `src/Hexalith.ChatBot.Server/Notifications/NotificationDelivery.cs`
- `src/Hexalith.ChatBot.Server/Notifications/INotificationSink.cs`
- `src/Hexalith.ChatBot.Server/Notifications/InMemoryNotificationSink.cs`
- `src/Hexalith.ChatBot.Server/Notifications/NotificationRoutingResolver.cs`
- `src/Hexalith.ChatBot.Server/Projections/NotificationRoutingSnapshotProjector.cs`
- `src/Hexalith.ChatBot.Server/Projections/NotificationRoutingReadPolicy.cs`
- `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotAuthorizationReasonCodes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.UI/Design/ChatBotNotificationRoutingEditorContract.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotNotificationRoutingEditor.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `tests/Hexalith.ChatBot.Contracts.Tests/NotificationRoutingContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/NotificationRoutingAuthorizationTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Notifications/NotificationRoutingResolverTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/NotificationRoutingProjectorTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotNotificationRoutingEditorContractTests.cs`

### Change Log

- 2026-06-02 - Implemented Story 7.6 notification routing and delivery: finite routing contracts, governed schema-bounded routing-config command + authorization/audit, FR72 routing/recipient-resolution engine with NFR2 redaction, parallel metadata-only notification sink, summary-safe read-back projector/read policy, and the governed routing matrix editor UI with localization. Added focused acceptance coverage across contracts/authorization/gateway-audit/resolver/projection/UI. All suites green; OpenAPI/generated client intentionally unchanged (generic transport). Story moved to review.
