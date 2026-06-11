---
baseline_commit: 94847ae
---

# Story 7.12: Disable mailbox source

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As an authorized mailbox administrator,
I want to disable a misbehaving mailbox source under a two-person rule,
so that unsafe or invalid mailbox activity stops without affecting unrelated sources, while every existing workflow item stays auditable and safe recovery guidance is shown.

## Acceptance Criteria

1. Given a mailbox source producing unsafe or invalid activity, when an authorized human admin proposes a **disable** and a **second** authorized human admin approves it (FR75d two-person rule), then the mailbox source transitions to a durable **Disabled** governance control state and **future intake from that mailbox source is blocked** at the mailbox intake worker, while **unrelated mailbox sources continue to process normally** (isolation, NFR30/NFR18). The disable is a security-sensitive FR74 governance control — **not** the Story 7.3 `mailbox-admin` configuration path — so it MUST go through the same submit→second-person-approve flow established for security-sensitive tenant policy mutations (`SubmitTenantPolicyChange`→`ApproveTenantPolicyChange`); a single-actor disable never takes effect. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.12`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR74`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`; `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`]

2. Given the disable proposal and approval, when authorization runs, then the operation is permitted only for a **human** holding the **mailbox-admin** (or **tenant-admin** union) admin scope, and is **denied for service clients and AI actors** even when they carry tenant-admin-looking claims, exactly as `AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Mailbox)` already enforces for `SubmitMailboxConfigurationChange`. The approving admin MUST be a **different person** from the proposer: both `RequesterRef != ApproverRef` (safe-token compare) and `RequesterActorId != approver envelope UserId` are enforced — at the gateway validation stage **and** re-checked in the aggregate, mirroring `ParticipantAuthorizationStage.IsValidTenantPolicyApproval` and `GovernedOperationAggregate.Handle(ApproveTenantPolicyChange)`. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75e`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`; `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`]

3. Given the disable is applied (approval committed), when it is recorded, then the operation records — with **no skip-audit path** (FR74, FR75g, NFR15a) — the **actor** (admin identity / actor type), **scope used**, **subject** (the safe mailbox-source ref), **reason** (documented justification reason code), **old state**, **new state**, **policy snapshot id**, and **timestamp**, via a metadata-only `AuditEnvelope` whose `StateTransition` is `"Active->Disabled"`. The audit is written through the existing fail-closed pre-commit seam (`IAuditWriter.RecordPreCommitAsync`): when the pre-commit audit is unavailable, **no durable disable state is written and no intake-blocking side effect occurs** (fail closed), reusing `CommandGateway` audit-unavailable behavior. The `tenant-admin` role does **not** bypass NFR15a/NFR50a. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.12`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75g`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`; `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`; `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`]

4. Given a mailbox source is Disabled, when a Graph mailbox notification arrives for that source, then `GraphMailboxIntakeWorker.ProcessAsync` resolves the source through `IMailboxConfigurationProvider.ResolvePatternAsync` and **blocks intake** — returning `MailboxIntakeWorkerResult.Recoverable` with a finite safe reason code (e.g. `"mailbox_source_disabled"`) **before** any Graph fetch or `CaptureMailboxMessageIntake` submission — so no new intake command is created for the disabled source. A notification for any **other (still-Active)** source for the same tenant is unaffected. The reason code maps to a sensible owner role in `MailboxIntakeWorkerResult.ResolveOwnerRole` (mailbox-admin owns re-enablement) and is treated as a recoverable (queue/retry-or-await-admin), not a poison, outcome. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.12`; `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`; `src/Hexalith.ChatBot.Workers/Mailbox/MailboxIntakeWorkerResult.cs`; `src/Hexalith.ChatBot.Workers/Mailbox/IMailboxConfigurationProvider.cs`]

5. Given a mailbox source is disabled, when existing workflow items that originated from that source are inspected, then they **remain visible, intact, and fully auditable** — disable affects **only future intake**, never already-captured intake records, associations, files, approvals, conversation content, or their audit trails (NFR17 visible/recoverable states; FR75c admins cannot mutate project-level records). Disable is reversible by a future re-enable flow; this story does not delete or rewrite any prior record. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR17`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75c`; `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md`]

6. Given an actor encounters a disabled mailbox source (admin viewing status, or an intake blocked), when guidance is surfaced, then **safe recovery guidance** is shown from the **finite message catalog** — a safe headline (≤ 80 chars), a one-line reason from a finite set, a next-action pointing to the responsible role (re-enable is a mailbox-admin/two-person action), and a `disabled-action` reason — using `ChatBotMessageCatalog` / `ChatBotMessageCodes` / `ChatBotDisabledActionReasons`, never raw error text. No surface names an unauthorized project/file/party/mailbox-content/audit detail (NFR2); EN/FR via existing localization; stable machine tokens (reason codes, state tokens, correlation ids) stay untranslated. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.12`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR76`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`]

7. Given the disable subject and audit refs are metadata, when the proposal/approval/disabled events and the audit envelope are serialized, then they carry **only safe, finite tokens** — the safe mailbox-source ref, `admin-operation:mailbox-source-disable` / `admin-scope:mailbox`, the reason code, old/new state tokens, policy-snapshot id, correlation id, UTC timestamp — and **never** mailbox subject/body, sender/recipient addresses, provider payloads, raw GUIDs that are not already safe identifiers, project/proposal/evidence content, raw claims/headers, bearer tokens, or secrets. The subject (mailbox source) is identified by its existing safe id (`MonitoredMailboxPattern.PatternRef` / `MailboxId` via `AuditMetadata.SafeOptionalToken` / `IsSafeStableIdentifier`). Tenant scope comes **only** from the authenticated gateway binding — never from the command body. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `src/Hexalith.ChatBot.Server/Audit/AuditMetadata.cs`; `src/Hexalith.ChatBot.Contracts/Commands/MailboxConfigurationContracts.cs`]

8. Given public commands, queries, DTOs, or generated clients change for the disable proposal/approval, when the contract surface is updated, then the OpenAPI contract spine is updated **first**, `HexalithChatBotClient.g.cs` is **regenerated** (never hand-edited), the generated-client checksum (`tests/fixtures/hexalith-chatbot-generated-client.sha256`) is refreshed, and contract/client parity tests prove schema parity — exactly as Stories 7.2/7.3 did for their public admin commands. The new command types are added to `ChatBotSpineCommandAllowlist` **only after** validation, authorization, audit, and tests are in place (fail-closed; never widen the allowlist first). [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/implementation-artifacts/7-3-mailbox-admin-scope-and-mailbox-configuration.md`; `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`; `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`]

9. Given acceptance coverage runs, then tests prove: a single-actor disable never takes effect (proposal alone does not block intake); a distinct second human approver applies the disable; `RequesterRef == ApproverRef` and `RequesterActorId == approver UserId` are both rejected (gateway **and** aggregate); service clients and AI actors are denied for both proposal and approval even with tenant-admin-looking claims; a non-mailbox/non-tenant-admin scope is denied; a disabled source blocks `GraphMailboxIntakeWorker` intake before fetch/submit with the `mailbox_source_disabled` recoverable reason while a sibling Active source is unaffected (isolation); disable does not mutate existing intake/association/audit records; the disable audit envelope carries actor/scope/subject/reason/old-state/new-state/policy-snapshot/timestamp with `StateTransition "Active->Disabled"` and **no** mailbox-content/PII/`@`/`secret`/project leakage; audit-unavailable → no durable disable + no intake-blocking side effect (fail closed); and OpenAPI/client/checksum parity holds. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs`; `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`; `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`]

## Tasks / Subtasks

- [x] Decide & document the disable control-state model and the two-person command shape (AC: 1, 7)
  - [x] **Recommended:** add a finite governance control-state enum (e.g. `MailboxSourceControlState { Active, Disabled }` in `src/Hexalith.ChatBot.Contracts/Enums/`) **distinct from** the Story 7.3 `MonitoredMailboxPattern.IsEnabled` config flag, so the FR74 security-sensitive (two-person) disable cannot be silently set/cleared via the `mailbox-admin` non-two-person `SubmitMailboxConfigurationChange` path. Shape the enum/event so Story 7.13 can add `Quarantined` and 7.15–7.26 can reuse the per-(subject × action) control pattern. State this decision (and the alternative: overloading `IsEnabled`, rejected because it bypasses the two-person rule) in completion notes.
  - [x] Define the disable as a **submit→approve** pair mirroring `SubmitTenantPolicyChange`/`ApproveTenantPolicyChange`. Decide and state whether to name them subject-specific (`SubmitMailboxSourceDisable`/`ApproveMailboxSourceDisable`) or generic subject-state controls; recommended: subject-specific for 7.12, with a note that 7.13+ generalize if duplication grows.

- [x] Add the disable proposal + approval command contracts (AC: 1, 2, 7, 8)
  - [x] Add `SubmitMailboxSourceDisable` and `ApproveMailboxSourceDisable` records implementing `IChatBotCommand` in `src/Hexalith.ChatBot.Contracts/Commands/`. Carry only safe metadata: a disable-change id, the safe mailbox-source ref (`PatternRef`/`MailboxId`), `ReasonCode` (documented justification), `PolicySnapshotId`, old/new state tokens, `SourceVersion`, `CorrelationId`, and on approve `RequesterRef` + `ApproverRef` + the pending disable-change id — mirror `ApproveTenantPolicyChange`'s field set. No mailbox content, addresses, or secrets.
  - [x] Reuse safe-token / finite-enum discipline; no free-form strings beyond tolerant parse at trust boundaries.

- [x] Add aggregate handlers with two-person enforcement (AC: 1, 2, 3, 5, 9)
  - [x] In `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`, add `Handle(SubmitMailboxSourceDisable, state, envelope)` → emit a `MailboxSourceDisablePendingApproval` event (record requester ref + requester actor id + subject + reason + old/new state), keyed by the disable-change id, refusing duplicates exactly as `Handle(SubmitTenantPolicyChange)` does.
  - [x] Add `Handle(ApproveMailboxSourceDisable, state, envelope)` → look up the pending approval; **reject** unless `pending.RequesterRef != command.ApproverRef` AND `pending.RequesterActorId != envelope.UserId` AND the subject/version/reason match (mirror `Handle(ApproveTenantPolicyChange)` lines 131–143). On success emit `MailboxSourceDisabled` (the activated control-state event) carrying actor, scope, subject, reason, old state (`Active`), new state (`Disabled`), policy snapshot, timestamp.
  - [x] Add the pending-approval dictionary + apply methods to `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` mirroring `_tenantPolicyPendingApprovals` (add-on-pending, remove-on-activate). Add the disable events to `src/Hexalith.ChatBot.Server/Governance/` (new `MailboxSourceControlEvents.cs` beside `Policy/TenantPolicyEvents.cs`, or the mailbox governance folder).

- [x] Authorize both commands (human + mailbox scope + distinct approver) (AC: 2, 9)
  - [x] In `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`, add per-command checks: `SubmitMailboxSourceDisable` and `ApproveMailboxSourceDisable` require `AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox)` (tenant-admin holds the union) and pass a new `IsValidMailboxSourceDisable` / `IsValidMailboxSourceDisableApproval` validator. Mirror `IsValidTenantPolicyApproval`: safe tokens for all refs, `SourceVersion >= 0`, non-empty `ReasonCode`/`PolicySnapshotId`, and `RequesterRef != ApproverRef` (Ordinal). Deny service/AI actors (covered by `HasHumanAdminScope`'s `IsHumanActor`).
  - [x] Use `ChatBotAuthorizationReasonCodes.AuthorizationDenied` on denial; keep authorization-denied indistinguishable from safe-not-found where required.

- [x] Extend the audit factory with the disable envelope (AC: 3, 7, 9)
  - [x] Add `AuditEnvelopeFactory` ref emission for `SubmitMailboxSourceDisable` / `ApproveMailboxSourceDisable` mirroring the `SubmitTenantPolicyChange`/`ApproveTenantPolicyChange`/`mailbox-config-change` ref blocks (~lines 1240–1335): `admin-operation:mailbox-source-disable` (and `...-disable-approve` for the approval), `admin-scope:mailbox`, `mailbox-source:<SafeOptionalToken>`, `policy-snapshot:<id>`, reason code, and the `StateTransition: "Active->Disabled"` on the committed disable. Use `AuditMetadata.SafeOptionalToken`/`IsSafeStableIdentifier`; never mailbox content/addresses/secrets.
  - [x] Preserve `ChatBotStateWritingPathInventory` — the disable writes durable state **only** through `CommandGateway` + `IAuditWriter.RecordPreCommitAsync`; do not add a new state-writing path. Confirm audit-unavailable fails closed (no disable, no intake block).

- [x] Add the command types to the spine allowlist — last, after validation/audit/tests (AC: 1, 8)
  - [x] Add `nameof(SubmitMailboxSourceDisable)` and `nameof(ApproveMailboxSourceDisable)` to `ChatBotSpineCommandAllowlist.AllowedCommandTypes`. Do **not** widen the allowlist before authorization + validation + audit + tests exist (the recurring Epic 7 review defect).

- [x] Enforce the disabled state at mailbox intake (AC: 4, 5, 9)
  - [x] Make the disabled control state visible to the worker through `IMailboxConfigurationProvider.ResolvePatternAsync`: the production resolver must reflect the `MailboxSourceDisabled` governance state so a disabled source resolves as **blocked** (return `null`/disabled marker). Decide and state whether to (a) have the resolver return `null` for disabled (reusing the existing `pattern is null` branch but with a distinct reason) or (b) extend `ControlledMailboxPattern`/the worker with an explicit disabled check. Recommended: an explicit disabled signal so the reason code is `mailbox_source_disabled` (not `mailbox_scope_mismatch`).
  - [x] In `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`, block disabled sources **before** `_source.FetchMessageAsync` and before building `CaptureMailboxMessageIntake`, returning `MailboxIntakeWorkerResult.Recoverable("mailbox_source_disabled")`.
  - [x] Add `mailbox_source_disabled` to `MailboxIntakeWorkerResult` (`ResolveOwnerRole` → mailbox-admin; classify retryable/await-admin appropriately, not poison). Disable must not touch already-captured intake/association records.

- [x] Add safe recovery guidance to the message catalog (AC: 6)
  - [x] Add a finite catalog entry in `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` (+ `ChatBotMessageCodes.cs`, `ChatBotDisabledActionReasons.cs`, and a refusal/owner reason code if needed) for the disabled-mailbox-source state: safe headline ≤ 80 chars, one-line reason, next-action pointing to mailbox-admin re-enable, `MetadataOnly` visibility. Reuse `DependencyDegraded`/`DegradedMailbox` patterns if they fit; otherwise add `mailbox-source-disabled`.

- [x] Update public contract spine + regenerate client (AC: 8, 9)
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` for the two new commands **first**, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` (never hand-edit), refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`, and extend client parity tests. Follow the exact workflow Stories 7.2/7.3 used for their public admin commands.

- [x] Optional S5 admin status surface (AC: 6) — _(decide; minimal if added)_
  - [x] If a disabled-source indicator is added to the S5 tenant configuration view, reuse `ChatBotTenantPolicyEditor` / its contract and `ChatBotUiTextKey` + `SharedResource.resx`/`.fr.resx`, keep tokens/reason-codes untranslated, and state it. Otherwise state no UI surface added (AC6 satisfied by the catalog + worker reason code).

- [x] Add focused tests (AC: all)
  - [x] Authorization tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs`: human mailbox-admin/tenant-admin allowed for propose+approve; policy/compliance/operations scope denied; service-client + AI denied (even with tenant-admin claims); distinct-approver enforced; `RequesterRef==ApproverRef` denied.
  - [x] Aggregate tests: proposal alone produces pending (no disable); approval by same requester/actor rejected; distinct approver activates `MailboxSourceDisabled`; subject/version/reason mismatch rejected.
  - [x] Gateway/audit tests near `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`: audit-unavailable on the approval fails closed (no durable disable, no intake-block side effect); disable envelope has actor/scope/subject/reason/old/new-state/policy-snapshot/timestamp + `StateTransition "Active->Disabled"`; redaction — no mailbox content/PII/`@`/`secret`/project refs.
  - [x] Worker tests in `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`: disabled source → `Recoverable("mailbox_source_disabled")` before fetch/submit; sibling Active source unaffected (isolation); existing records untouched.
  - [x] Contract tests in `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`: wire tokens/serialization for the two commands + control-state enum, safe-token validation, no secret-bearing names.
  - [x] Client/Conformance/Architecture regression: client parity (OpenAPI/checksum), actor-isolation conformance if changed, architecture boundaries if new public/internal seams added. Record green counts.

## Dev Notes

### Scope Boundaries

- Story 7.12 is the **first** of the FR74 enforcement series (7.12–7.26: five subject classes — mailbox / service-client / AI-actor / command-capability / outbound — × three actions — disable / quarantine / rate-limit). It implements exactly **one** cell: **mailbox source × disable**. Disable (and quarantine, 7.13) are **security-sensitive** admin operations gated by the **FR75d two-person rule**; rate-limit (7.14) is a standard policy mutation. Shape the propose→approve + control-state + intake-enforcement pattern so 7.13–7.26 reuse it, but **do not** implement quarantine, rate-limit, or any other subject here. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR74` (decomposition guidance)]
- This is **NOT** the Story 7.3 mailbox-configuration path. Story 7.3's `SubmitMailboxConfigurationChange` (mailbox-admin, single-actor) configures patterns/routing/connections and carries a per-pattern `IsEnabled` flag. The FR74 disable is a **separate, two-person, security-sensitive governance control** with documented justification recorded in audit. **Do not** route disable through `SubmitMailboxConfigurationChange`; doing so would bypass the two-person rule.
- The disable affects **future intake only**. It must never delete, rewrite, or hide already-captured intake records, associations, files, approvals, conversation content, or their audit trails (NFR17, FR75c). It is reversible by a future re-enable flow (out of scope here).
- Tenant identity is always from the **authenticated gateway binding**, never the command body. The subject (mailbox source) and all audit refs are **metadata-only, safe tokens** (NFR2).

### Existing Code To Reuse — mirror, do not fork

- **Two-person rule (FR75d) — the exact pattern to copy (Story 7.2):**
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` — `Handle(SubmitTenantPolicyChange)` (~lines 60–118: `requiresApproval` → emit `TenantPolicyChangePendingApproval`, dedupe by change id) and `Handle(ApproveTenantPolicyChange)` (~lines 120–162: look up pending, **reject** when `pending.RequesterRef == command.ApproverRef` OR `pending.RequesterActorId == envelope.UserId` OR subject/version mismatch, else activate). **This is the canonical two-person enforcement** — mirror it for disable.
  - `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` — `_tenantPolicyPendingApprovals` dictionary + add-on-pending / remove-on-activate apply methods (~lines 40, 98, 303–313). Mirror for `_mailboxSourceDisablePendingApprovals`.
  - `src/Hexalith.ChatBot.Server/Governance/Policy/TenantPolicyEvents.cs` — `TenantPolicyChangePendingApproval` (carries `RequesterRef`, `RequesterActorId`) and `TenantPolicySnapshotActivated` (carries `ApprovalStatus`). Model `MailboxSourceDisablePendingApproval` + `MailboxSourceDisabled` the same way.
  - `src/Hexalith.ChatBot.Contracts/Commands/ApproveTenantPolicyChange.cs` and `SubmitTenantPolicyChange.cs` — the public command field sets (change id, refs, `ReasonCode`, `PolicySnapshotId`, `RequesterRef`/`ApproverRef`, `SourceVersion`, `CorrelationId`, `SchemaVersion`) to mirror.
  - `ParticipantAuthorizationStage.IsValidTenantPolicyApproval` (~lines 287–303) — the gateway-level distinct-approver + safe-token validation to mirror (`!string.Equals(RequesterRef, ApproverRef, Ordinal)`).
- **Admin authorization + actor restriction (Story 7.1):**
  - `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` — `HasHumanAdminScope(principal, AdminScope.Mailbox)` / `HasHumanTenantAdmin`; `IsHumanActor` rejects `service`/`ai` actor types. Use for both commands.
  - `src/Hexalith.ChatBot.Contracts/Enums/AdminRole.cs`, `AdminRoles.cs`, `AdminScope.cs`, `AdminScopes.cs` — finite role/scope tokens + `ScopesForRole`/`ToWireValue`. `AdminScope.Mailbox` is the scope; `tenant-admin` holds the union.
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` — the per-command switch + `IsValid*` validators + claim constants (`ActorTypeClaim`, `HumanActorValue`/`ServiceActorValue`/`AiActorValue`); add the two new commands beside the `SubmitMailboxConfigurationChange` block (~lines 97–102, 305–320).
- **Mailbox model + intake (Story 7.3 / Epic 2):**
  - `src/Hexalith.ChatBot.Contracts/Commands/MailboxConfigurationContracts.cs` — `MonitoredMailboxPattern(MailboxId, SourceContext, ProviderConnectionRef, bool IsEnabled, string PatternRef)`. `PatternRef`/`MailboxId` are the safe subject ids for the disable. (Note the config `IsEnabled` is distinct from the FR74 disable control state — see Scope Boundaries.)
  - `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs` — `ProcessAsync` resolves `ControlledMailboxPattern` via `IMailboxConfigurationProvider.ResolvePatternAsync` (~lines 58–64), returns `MailboxIntakeWorkerResult.Recoverable(...)` on block, then fetch → scope match → `CaptureMailboxMessageIntake`. Insert the disabled-source block before fetch.
  - `src/Hexalith.ChatBot.Workers/Mailbox/IMailboxConfigurationProvider.cs`, `ControlledMailboxPattern.cs` (worker-side `(MailboxId, SourceContext)`), `MailboxIntakeWorkerResult.cs` (`Recoverable(reasonCode)` + `ResolveOwnerRole` + retryable classification ~lines 26–50).
- **Audit (no skip path, FR75g / NFR15a):**
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` — the per-command ref blocks for `SubmitTenantPolicyChange`/`ApproveTenantPolicyChange` (~1240–1283) and `mailbox-config-change` (~1318–1335) are the templates; `StateTransition` precedents (`"Unresolved->Escalated"`, `"Open->BacklogAlerted"`). Add `"Active->Disabled"`.
  - `src/Hexalith.ChatBot.Server/Audit/AuditMetadata.cs` — `SafeOptionalToken` / `IsSafeStableIdentifier` for subject + refs.
  - `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` — pre-commit audit-unavailable → suppress dispatch / fail closed; reuse, do not re-implement.
  - `src/Hexalith.ChatBot.Server/Audit/ChatBotStateWritingPathInventory.cs` — NFR15a required seam; no new state-writing path.
- **Spine allowlist + safe text:**
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` — add the two commands (last).
  - `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotMessageCodes.cs`, `ChatBotDisabledActionReasons.cs`, `ChatBotRefusalReasonCodes.cs` (e.g. `DependencyDegraded`) — finite safe recovery text.
- **Public contract workflow:** `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` → regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` → refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256` (Stories 7.2/7.3 precedent).

### Current State To Preserve

- The two-person rule is enforced **twice** (gateway validation AND aggregate) for tenant policy; preserve the defense-in-depth for disable — do not rely on the gateway check alone.
- `AdminAuthorityEvaluator` already denies service/AI actors with tenant-admin-looking claims; **generalize, don't weaken**. Disable must be human + mailbox/tenant-admin only.
- `GraphMailboxIntakeWorker` already returns `Recoverable` (not poison) for scope mismatches and treats intake-block as queue/retry; the disabled-source block must be a sibling recoverable outcome — never a silent drop and never a crash.
- `CommandGateway` suppresses dispatch when pre-commit audit is unavailable; disable must reuse this — no durable disable state, no intake-blocking effect, when audit is down.
- Disable changes **only future intake**; already-captured `CaptureMailboxMessageIntake` records, associations, and audit trails are immutable and must stay readable/auditable (NFR17).
- Existing root submodule policy applies: initialize/update only root `.gitmodules` submodules; **never** recursive submodule commands. **Do not bump submodule pointers** — Stories 7.1/7.5–7.10 reviews each caught undocumented gitlink bumps (`Hexalith.EventStore`/`Hexalith.FrontComposer`/`Hexalith.Tenants`/`Hexalith.Parties`) that broke the build. Reset stray gitlink drift via `git submodule update -- <path>` (non-recursive) before finishing; keep the File List exact.

### Architecture Guardrails

- Contracts (commands, enums, DTOs) → `src/Hexalith.ChatBot.Contracts`. Aggregate handlers/state/events → `src/Hexalith.ChatBot.Server/Operations` + `src/Hexalith.ChatBot.Server/Governance`. Authorization → `Gateway/Stages` + `Governance/Admin`. Audit refs → `src/Hexalith.ChatBot.Server/Audit`. Intake enforcement → `src/Hexalith.ChatBot.Workers/Mailbox`. No new top-level project.
- Use records/enums/finite tokens, never delimited strings or ad-hoc dictionaries, for state/role/scope/reason.
- Tenant id from authenticated binding only. Deny-by-default on missing/ambiguous/invalid admin role/scope claims; unknown roles imply no access.
- The disable control state is a **fixed FR74 governance control**, not a tenant-tunable knob; it is set only through the two-person submit→approve path, never the mailbox-config path.
- Every committed disable is preceded by a fail-closed pre-commit audit; on audit-unavailable, no durable/observable side effect. No state-write that bypasses audit.

### Previous Story Intelligence

- **Story 7.2** (policy-admin / Tenant Policy Schema editor) shipped the **two-person rule**: `SubmitTenantPolicyChange`→`TenantPolicyChangePendingApproval`→`ApproveTenantPolicyChange`→`TenantPolicySnapshotActivated`, with distinct-approver enforced at gateway (`IsValidTenantPolicyApproval`) and aggregate (`Handle(ApproveTenantPolicyChange)` rejecting `RequesterRef==ApproverRef` / `RequesterActorId==envelope.UserId`). **This is the disable pattern.**
- **Story 7.3** (mailbox-admin scope) shipped `MonitoredMailboxPattern` (with config `IsEnabled`), `SubmitMailboxConfigurationChange`, the `AdminScope.Mailbox` authorization, the `AuditEnvelopeFactory` mailbox refs, and the `IMailboxConfigurationProvider` worker seam — the mailbox model + intake resolution to build on. It also established the OpenAPI→client→checksum workflow for public admin commands.
- **Story 7.1** (permission model) shipped `AdminRole`/`AdminScope`/`AdminRoles`/`AdminScopes`, `AdminAuthorityEvaluator` (human-only, service/AI denied), and the metadata-only admin audit refs — the authorization + audit foundation.
- Recurring Epic 7 review defects to avoid: empty audit-obligation/reason fields; unsafe affected refs; relaxed authorization; **adding a command to the spine allowlist before validation/audit/tests**; counters/state that trust client-supplied tenant ids; undocumented submodule pointer bumps; inexact File List / stale debug-log counts; mailbox content/PII leaking into audit refs.

### Latest Technical Specifics

- No external version research required. Repo-pinned stack; do not upgrade packages: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, Blazor/FrontComposer, Fluent UI v5 RC, Fluxor, existing OpenAPI/NSwag generated-client tooling.
- Do not change target frameworks, Aspire/Dapr topology, Fluent UI, Fluxor, NSwag/client-generation tooling, MCP SDK, Graph permission posture, WORM audit assumptions, or submodule pointers — except the OpenAPI regeneration the new public commands require (AC8).
- Use Ordinal string comparison for all ref/id equality (matches existing two-person checks). No `GetHashCode()` fingerprints for identity.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` (OpenAPI/generated-client parity — required, public commands change).
  - `./tests/Hexalith.ChatBot.Conformance.Tests/...` / `Architecture.Tests` as regression (or if actor-isolation / boundaries change). Record green counts.
- Highest-value targets: the two-person enforcement (single-actor no-op, distinct-approver applies, same-person rejected at gateway **and** aggregate, service/AI denied); the intake-block isolation (disabled source blocked before fetch/submit, sibling Active source unaffected, existing records untouched); the fail-closed audit (audit-unavailable → no disable + no block); and the metadata-only redaction (no mailbox content/PII/`@`/`secret`/project refs in events/envelope).
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer the compiled in-process xUnit v3 runners after build.
- Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, metadata-only fixtures, and root-level submodule policy.

### Project Structure Notes

- New: control-state enum (`Contracts/Enums/`), two command records (`Contracts/Commands/`), the disable events (`Server/Governance/`), aggregate handlers + state (`Server/Operations/`), authorization validators (`Gateway/Stages/`), audit refs (`Server/Audit/AuditEnvelopeFactory.cs`), spine allowlist entries, worker block + reason code (`Workers/Mailbox/`), message-catalog entry (`Contracts/Messages/`), OpenAPI + regenerated client + checksum.
- **Variances to decide and state in completion notes:** (1) dedicated `MailboxSourceControlState` enum vs overloading config `IsEnabled` — recommended: dedicated state to avoid the mailbox-admin two-person bypass; (2) subject-specific (`SubmitMailboxSourceDisable`) vs generic subject-state-control command pair — recommended: subject-specific for 7.12; (3) how the worker learns of the disabled state (resolver returns null/disabled vs explicit worker check) — recommended: explicit disabled signal so the reason code is `mailbox_source_disabled`; (4) whether an S5 admin status surface is added (recommended: deferred, catalog + worker reason code satisfy AC6); (5) the durable read-side that lets the worker's `IMailboxConfigurationProvider` observe the `MailboxSourceDisabled` event (the projection wiring may be partly deferred like the 7.6–7.11 runtime callers — if so, state it and ensure the evaluator/aggregate + worker contract are testable in isolation).

### References

- `_bmad-output/planning-artifacts/epics.md#Story 7.12` — Disable mailbox source: intake blocked, existing items auditable, safe recovery; audit of actor/scope/subject/reason/old-state/new-state/policy-snapshot/timestamp with no skip-audit (FR74, FR75g); two-person rule (FR75d); not performable by service clients or AI actors.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR74` — disable/quarantine/rate-limit for mailbox/service-client/AI-actor/command-capability/outbound; decomposition guidance (per-(subject × action); disable+quarantine security-sensitive two-person, rate-limit standard).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`, `#FR75e`, `#FR75g` — two-person rule + documented justification in audit; mailbox-admin scope; audit obligation on every admin action, no skip-audit, tenant-admin no bypass.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`, `#NFR15a`, `#NFR17`, `#FR76` — redaction; fail-closed audit; visible/recoverable states; disabled-action reasons + next-step guidance.
- `_bmad-output/planning-artifacts/architecture.md` — API & Communication Patterns (command spine, two-phase audit, fail-closed), Project Structure & Boundaries, Testing Strategy.
- `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md` — the two-person rule submit→approve precedent.
- `_bmad-output/implementation-artifacts/7-3-mailbox-admin-scope-and-mailbox-configuration.md` — mailbox model, `SubmitMailboxConfigurationChange`, `AdminScope.Mailbox`, intake resolver seam, OpenAPI/client workflow.
- `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md` — admin roles/scopes, `AdminAuthorityEvaluator`, metadata-only admin audit refs.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`, `GovernedOperationState.cs`; `src/Hexalith.ChatBot.Server/Governance/Policy/TenantPolicyEvents.cs` — two-person handlers/state/events to mirror.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`; `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` — authorization + distinct-approver validation.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`, `AuditMetadata.cs`; `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`, `ChatBotStateWritingPathInventory.cs` — audit refs + fail-closed seam.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`, `IMailboxConfigurationProvider.cs`, `ControlledMailboxPattern.cs`, `MailboxIntakeWorkerResult.cs` — intake enforcement seam.
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxConfigurationContracts.cs`; `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`; `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` — mailbox subject ids, spine allowlist, safe recovery text.
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`; `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; `tests/fixtures/hexalith-chatbot-generated-client.sha256` — public contract regeneration.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AssociationThresholdAuthorizationTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` — test anchors to extend.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — claude-opus-4-8[1m]

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx -m:1 /nr:false` → Build succeeded, 0 Warning(s), 0 Error(s) (warnings-as-errors on). The Client MSBuild target regenerated `HexalithChatBotClient.g.cs` from the updated OpenAPI spine.
- Compiled in-process xUnit v3 runners (per the inherited VSTest `SocketException` sandbox note), `-parallel none`:
  - `Hexalith.ChatBot.Contracts.Tests` → Total: 252, Failed: 0.
  - `Hexalith.ChatBot.Server.Tests` → Total: 688, Failed: 0.
  - `Hexalith.ChatBot.Workers.Tests` → Total: 23, Failed: 0.
  - `Hexalith.ChatBot.Client.Tests` → Total: 17, Failed: 0 (OpenAPI/generated-client/checksum parity green).
  - `Hexalith.ChatBot.Conformance.Tests` → Total: 75, Failed: 0 (regression).
  - `Hexalith.ChatBot.Architecture.Tests` → Total: 37, Failed: 0 (regression).
- Generated-client checksum fixture refreshed to `f93d79a8a686f676b1b24f3cc8aee88922f681012363c0a800aba02002fb91e5` after the OpenAPI change (two new command schemas + `MailboxSourceControlState`).
- Two pre-existing tests updated for the grown vocabulary, not new failures: `LifecycleStateModelTests.StateVocabularyShouldBeStableAndOrdered` (added `Active`/`Disabled`) and `MessageCatalogContractTests` finite-reason set (added `disabled-action`).
- `git submodule status` → no gitlink drift; no submodule pointers bumped.

### Completion Notes List

Implemented the first FR74 enforcement cell — **mailbox source × disable** — under the FR75d two-person rule, mirroring the Story 7.2 tenant-policy submit→approve pattern. Variances decided (per Project Structure Notes):

1. **Dedicated control-state enum** `MailboxSourceControlState { Active, Disabled }` (`Contracts/Enums/`), distinct from the Story 7.3 `MonitoredMailboxPattern.IsEnabled` config flag. Rejected the alternative of overloading `IsEnabled` because the mailbox-admin single-actor `SubmitMailboxConfigurationChange` path would then silently bypass the two-person rule. Enum shaped so 7.13 can add `Quarantined` and 7.15–7.26 reuse the per-(subject × action) control pattern.
2. **Subject-specific command pair** `SubmitMailboxSourceDisable` / `ApproveMailboxSourceDisable` (recommended for 7.12). If duplication grows across 7.13+ these can generalize to subject-state controls.
3. **Worker disabled signal is explicit**: `ControlledMailboxPattern` gained `MailboxSourceControlState ControlState = Active` (default keeps existing constructor calls). The worker blocks a disabled source with the distinct reason `mailbox_source_disabled` (not the generic `mailbox_scope_mismatch`), before any Graph fetch or `CaptureMailboxMessageIntake` submission.
4. **No S5 admin status surface added** (deferred). AC6 is satisfied by the finite `ChatBotMessageCatalog` entry (`mailbox_source_disabled` → headline ≤ 80, one-line reason, `request-access` next-action pointing at mailbox-admin re-enablement, new `disabled-action` reason) plus the worker reason code. EN/FR display localization remains at the (deferred) UI layer; machine tokens stay untranslated.
5. **Durable read-side projection deferred**, consistent with the 7.6–7.11 deferred runtime callers: no production `IMailboxConfigurationProvider` observes `MailboxSourceDisabled` yet (the only resolvers are the worker's `StaticMailboxConfigurationProvider` and the test fakes). The aggregate + event + worker contract are fully testable in isolation, which the new tests exercise; wiring the projection that feeds the worker's resolver from the `MailboxSourceDisabled` event is left for a later layering stage.

Defense-in-depth two-person enforcement: distinct approver (`RequesterRef != ApproverRef` Ordinal **and** `RequesterActorId != envelope.UserId`) is checked at the gateway (`ParticipantAuthorizationStage`) **and** re-checked in the aggregate (`Handle(ApproveMailboxSourceDisable)`); a single-actor disable only ever produces a pending approval, never a `MailboxSourceDisabled`. Service/AI actors are denied via `AdminAuthorityEvaluator.HasHumanAdminScope(.., AdminScope.Mailbox)` even with tenant-admin-looking claims.

Audit: `AuditEnvelopeFactory.AdminEvidenceRefs` emits `admin-operation:mailbox-source-disable[-approve]`, `admin-scope:mailbox`, `mailbox-source-disable-change:<id>`, `mailbox-source:<safe-ref>`, `policy-snapshot:<id>`, `reason:<code>`, `mailbox-source-old-state:active`, `mailbox-source-new-state:disabled`, and `admin-subject:<approver>` (approval only). The committed disable carries `StateTransition "Active->Disabled"` via a new `Active→Disabled` lifecycle transition mapped for `ApproveMailboxSourceDisable` in `CommandSubmissionLifecycleTransitionGuard`. Disable writes durable state only through `CommandGateway` + `IAuditWriter.RecordPreCommitAsync`; the audit-unavailable → fail-closed (no durable disable, no intake-block side effect) behavior is reused, not re-implemented, and proven by a gateway test.

Spine allowlist widened **last**, after authorization + validation + audit + tests were in place (the recurring Epic 7 review defect). Public contract surface updated OpenAPI-spine-first, generated client regenerated (never hand-edited), checksum fixture refreshed, parity tests green.

### File List

New:
- `src/Hexalith.ChatBot.Contracts/Enums/MailboxSourceControlState.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/MailboxSourceControlContracts.cs`
- `src/Hexalith.ChatBot.Server/Governance/Mailbox/MailboxSourceControlEvents.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/MailboxSourceDisableAuthorizationTests.cs`

Modified:
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotDisabledActionReasons.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` (regenerated)
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleStates.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionValidator.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/CommandSubmissionLifecycleTransitionGuard.cs`
- `src/Hexalith.ChatBot.Workers/Mailbox/ControlledMailboxPattern.cs`
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`
- `src/Hexalith.ChatBot.Workers/Mailbox/MailboxIntakeWorkerResult.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
- `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs`
- `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-02 (autonomous story-automator review, adversarial + auto-fix)

**Outcome:** ✅ Approved — Status → `done`. 0 Critical, 0 High, 0 Medium findings requiring a code change. No fixes applied (none warranted).

**Verification performed (every claim re-checked against git reality, not the story prose):**

- **File List vs git:** exact match — all 4 new files (`MailboxSourceControlState.cs`, `MailboxSourceControlContracts.cs`, `Governance/Mailbox/MailboxSourceControlEvents.cs`, `MailboxSourceDisableAuthorizationTests.cs`) and 23 modified files present; no undocumented changes; no files claimed without git evidence.
- **Submodules:** `git submodule status` shows no gitlink drift; no pointers bumped (the recurring Epic 7 defect — clean here).
- **Build:** `dotnet build Hexalith.ChatBot.slnx` → **0 Warning(s), 0 Error(s)** (warnings-as-errors on).
- **Tests (compiled in-process xUnit runners, `-parallel none`) — all claimed counts reproduced exactly:** Contracts 252/0, Server 688/0, Workers 23/0, Client 17/0, Conformance 75/0, Architecture 37/0.
- **Generated client + checksum:** generated client contains the two new commands; `sha256sum` of the actual `HexalithChatBotClient.g.cs` (`f93d79a8…fb91e5`) **matches** the committed fixture exactly; OpenAPI spine carries `SubmitMailboxSourceDisable`/`ApproveMailboxSourceDisable`/`MailboxSourceControlState`.
- **AC1 (two-person disable):** submit→pending→distinct-approver→`MailboxSourceDisabled`; single-actor never disables (verified in aggregate handler + test). ✅
- **AC2 (authorization):** `HasHumanAdminScope(.., AdminScope.Mailbox)` gates both commands; service/AI denied even with tenant-admin claims; distinct-approver enforced at **gateway** (`ParticipantAuthorizationStage` + dispatcher guard) **and** aggregate (`RequesterRef != ApproverRef` Ordinal **and** `RequesterActorId != envelope.UserId`). ✅
- **AC3 (fail-closed audit):** `AuditEnvelopeFactory` emits actor/scope/subject/reason/old-state/new-state/policy-snapshot/version + `StateTransition "Active->Disabled"`; pre-commit-audit-unavailable → no dispatch, 503, replay intent (gateway test proves no durable disable + no side effect). ✅
- **AC4/AC5 (intake block before fetch / existing records intact):** worker blocks `Disabled` source with `Recoverable("mailbox_source_disabled")` before `FetchMessageAsync`/`CaptureMailboxMessageIntake`; sibling Active source unaffected; owner = mailbox-admin; not poison. ✅ *(in isolation — see deferral below).*
- **AC6 (safe guidance):** finite catalog entry `mailbox_source_disabled` (headline ≤ 80 chars, one-line reason, `request-access` next-action, `disabled-action` reason, `MetadataOnly`). ✅
- **AC7 (metadata-only redaction):** contract + audit serialization tests assert no `@`/`secret`/subject/body/token/project leakage; subject carried as safe ref only. ✅
- **AC8 (contract spine workflow):** OpenAPI-first → regenerated client (never hand-edited) → refreshed checksum → parity tests; allowlist widened **last**. ✅
- **AC9 (acceptance coverage):** all enumerated cases have real, asserting tests (not placeholders).

**Findings (all informational — no fix applied):**

- 🟢 **LOW (accepted, documented deferral):** No production `IMailboxConfigurationProvider` projects the `MailboxSourceDisabled` event yet (only the worker's `StaticMailboxConfigurationProvider` + test fakes set `ControlState`), so an approved disable does not block live intake until the read-side projection is wired. This is **explicitly sanctioned** by Project Structure Notes variance (5) ("the projection wiring may be partly deferred like the 7.6–7.11 runtime callers — if so, state it and ensure the evaluator/aggregate + worker contract are testable in isolation") and stated in Completion Note 5; the aggregate+event+worker contract are proven in isolation. No action — tracked for a later layering stage.
- 🟢 **LOW (no fix — mirrors the established pattern):** Submit-time dedup keys on `DisableChangeId` and on already-`DisabledMailboxSources[ref]`, but two *distinct* `DisableChangeId`s targeting the same still-Active source can both produce pending approvals concurrently. Harmless (a second approval idempotently re-records the disable; new proposals are blocked once disabled) and consistent with the mirrored `SubmitTenantPolicyChange` behavior; changing it would diverge from the canonical pattern.
- 🟢 **LOW (cosmetic):** Catalog next-action is `request-access` while the worker's `SafeNextAction` is `escalate` for the same reason code — both are safe finite tokens pointing at admin involvement; no leakage, no functional impact.

### Senior Developer Review (AI) — 2026-06-11 (QA-augmentation re-review)

**Reviewer:** Jérôme Piquot — 2026-06-11 (autonomous story-automator review, adversarial + auto-fix)

**Trigger:** A `bmad-qa-generate-e2e-tests` pass added story-specific coverage for the mailbox-source disable contracts; this re-review validates those additions against git reality.

**Outcome:** ✅ Approved — Status stays `done`. 0 Critical, 0 High. 1 Medium auto-fixed (File List). 3 informational LOW (no fix).

**Working-tree changes reviewed (uncommitted, beyond the committed `91723ce feat(story-7.12)`):**

- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs` — adds `GeneratedClientShouldContainMailboxSourceDisableContractsWithSafeMetadataOnly` (OpenAPI→client parity + metadata-only property/blocked-fragment assertions for `SubmitMailboxSourceDisable`/`ApproveMailboxSourceDisable`/`MailboxSourceControlState`).
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` — adds `CommandGatewayApi_ShouldAcceptMailboxSourceDisableApprovalThroughUiSpine` (human mailbox-admin approval accepted, PreCommit+PostCommit audit, `StateTransition "Active->Disabled"`, `admin-operation:mailbox-source-disable-approve`/`admin-scope:mailbox` refs, no tenant/`@`/`secret` leakage) and `CommandGatewayApi_ShouldDenyMailboxSourceDisableApprovalFromServiceActorWithTenantAdminClaim` (service actor with tenant-admin claim → 403, no dispatch, no audit, no idempotency record).

**Verification performed (git reality, not story prose):**

- **Build:** `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **0 Warning(s), 0 Error(s)** (warnings-as-errors on). The new tests compile against the current (ahead-of-7.12) codebase.
- **Tests (compiled in-process xUnit v3 runners, `-parallel none`):** Client `35/0` (incl. the new parity test, verified by name), Server `1591/0` (incl. both new E2E tests, verified by name `*MailboxSourceDisableApproval*` → 5/0), Contracts `482/0`, Workers `31/0`. All green. (Counts run higher than the original story-doc figures because the repo is many stories ahead — expected, not a regression.)
- **Repo-is-ahead reconciliation:** `MailboxSourceControlState` now legitimately carries `{ Active, Disabled, Quarantined }` (Quarantined added by Story 7.13) in both `Contracts/Enums/MailboxSourceControlState.cs` and the generated client; the new `ClientGenerationTests` assertion `["Active","Disabled","Quarantined"]` matches **current** reality. Not reverted to 7.12's `{Active,Disabled}` snapshot — doing so would break the current build.
- **Implementation spot-check (unchanged, committed):** aggregate `Handle(ApproveMailboxSourceDisable)` re-rejects same-person (`pending.RequesterRef==command.ApproverRef || pending.RequesterActorId==envelope.UserId`, Ordinal) before emitting `MailboxSourceDisabled` (defense-in-depth ✓); worker blocks `Disabled` with `Recoverable("mailbox_source_disabled")` **before** `FetchMessageAsync` ✓; both commands in `ChatBotSpineCommandAllowlist` ✓.
- **Submodules:** `git submodule status` → no gitlink drift; no pointers bumped.

**Findings:**

- 🟡 **MEDIUM (auto-fixed):** The two QA-added test files were not listed in the story File List. Added `CommandGatewayAdmissionApiE2ETests.cs` and `ClientGenerationTests.cs` under Modified. No code change required.
- 🟢 **LOW (informational):** Story-doc Debug-Log test counts (Client 17, Server 688, Contracts 252, Workers 23) read low versus actual (35/1591/482/31) because the repo is many stories ahead of 7.12's snapshot. Expected; not corrected in the historical Debug Log.
- 🟢 **LOW (carried, story-sanctioned):** No production `IMailboxConfigurationProvider` projects `MailboxSourceDisabled` yet (projection wiring deferred per Project Structure Notes variance 5 / Completion Note 5). Contract proven in isolation.
- 🟢 **LOW (carried, cosmetic):** Catalog next-action `request-access` vs worker `SafeNextAction` `escalate` for the same reason code — both safe finite tokens, no leakage.

## Change Log

- 2026-06-11 — Senior Developer Review (AI): QA-augmentation re-review of the added client-parity + gateway-admission E2E tests for mailbox-source disable. Build clean; Client 35/0, Server 1591/0, Contracts 482/0, Workers 31/0; both new E2E tests + new parity test green by name; no submodule drift. 0 Critical/High; 1 Medium auto-fixed (File List updated with the two test files); 3 informational LOW. Status remains `done`.
- 2026-06-02 — Senior Developer Review (AI): adversarial review + verification — File List exact, no submodule drift, clean build, all 6 test suites green at claimed counts, generated-client checksum verified against the actual file. 0 Critical/High/Medium; 3 informational LOW notes (top one a story-sanctioned projection deferral). Approved; Status review → done.
- 2026-06-02 — Story 7.12 implemented: FR74/FR75d two-person mailbox-source disable (control-state enum, submit→approve commands, aggregate handlers + state + events, gateway authorization with distinct-approver enforcement, fail-closed audit envelope with `Active->Disabled` transition, intake-worker block before fetch, safe-recovery message-catalog entry, OpenAPI + regenerated client + refreshed checksum, focused tests across all ACs). Status: ready-for-dev → in-progress → review.
