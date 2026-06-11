---
baseline_commit: 91723ce
---

# Story 7.13: Quarantine mailbox source

Status: done

<!-- Validation: create-story checklist applied 2026-06-02. -->

## Story

As an authorized mailbox administrator,
I want to quarantine a mailbox source under a two-person rule,
so that suspicious mailbox activity is contained for review — new intake is routed to a quarantine state where reviewers inspect only safe metadata (never restricted content), while every existing workflow item stays auditable and safe recovery guidance is shown.

## Acceptance Criteria

1. Given a mailbox source requiring investigation, when an authorized human admin proposes a **quarantine** and a **second** authorized human admin approves it (FR75d two-person rule), then the mailbox source transitions to a durable **Quarantined** governance control state and **new intake from that source is routed to a quarantine (contained-for-review) state** rather than the normal intake pipeline, while **unrelated mailbox sources continue to process normally** (isolation, NFR30/NFR18). Quarantine is a security-sensitive FR74 governance control — **not** the Story 7.3 `mailbox-admin` configuration path — so it MUST go through the same submit→second-person-approve flow established for the Story 7.12 disable (`SubmitMailboxSourceDisable`→`ApproveMailboxSourceDisable`) and Story 7.2 tenant-policy mutations; a single-actor quarantine never takes effect. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.13`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR74`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`; `_bmad-output/implementation-artifacts/7-12-disable-mailbox-source.md`; `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs:165`]

2. Given the quarantine proposal and approval, when authorization runs, then the operation is permitted only for a **human** holding the **mailbox-admin** (or **tenant-admin** union) admin scope, and is **denied for service clients and AI actors** even when they carry tenant-admin-looking claims, exactly as `AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Mailbox)` already gates `SubmitMailboxSourceDisable`/`ApproveMailboxSourceDisable`. The approving admin MUST be a **different person** from the proposer: both `RequesterRef != ApproverRef` (Ordinal compare) and `RequesterActorId != approver envelope UserId` are enforced — at the gateway validation stage (`ParticipantAuthorizationStage` + `AcceptedCommandDispatcher`) **and** re-checked in the aggregate, mirroring `IsValidMailboxSourceDisableApproval` and `Handle(ApproveMailboxSourceDisable)`. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`; `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs:104`; `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs:199`; `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs`]

3. Given the quarantine is applied (approval committed), when it is recorded, then the operation records — with **no skip-audit path** (FR74, FR75g, NFR15a) — the **actor** (approver identity / actor type), **scope used** (`admin-scope:mailbox`), **subject** (the safe mailbox-source ref), **reason** (documented justification reason code), **old state** (`active`), **new state** (`quarantined`), **policy snapshot id**, and **timestamp**, via the metadata-only `AuditEnvelope` whose `StateTransition` is `"Active->Quarantined"`. The audit is written through the existing fail-closed pre-commit seam (`IAuditWriter.RecordPreCommitAsync` via `CommandGateway`): when the pre-commit audit is unavailable, **no durable quarantine state is written and no intake-routing side effect occurs** (fail closed), reusing the disable/`CommandGateway` audit-unavailable behavior. The `tenant-admin` role does **not** bypass NFR15a/NFR50a. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.13`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75g`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR15a`; `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs:1387`; `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`]

4. Given a mailbox source is Quarantined, when a Graph mailbox notification arrives for that source, then `GraphMailboxIntakeWorker.ProcessAsync` resolves the source through `IMailboxConfigurationProvider.ResolvePatternAsync` and **routes intake to the quarantine outcome** — returning `MailboxIntakeWorkerResult.Recoverable` with the finite safe reason code `"mailbox_source_quarantined"` **before** any Graph fetch or `CaptureMailboxMessageIntake` submission — so **no restricted mailbox content (body, sender/recipient addresses, attachments) is fetched or read** for the quarantined source and no normal-pipeline intake command is created. A notification for any **other (still-Active)** source for the same tenant is unaffected (isolation). The reason code maps to a sensible owner role in `MailboxIntakeWorkerResult.ResolveOwnerRole` (mailbox-admin owns release/review) and is treated as a recoverable (queue/await-admin), not a poison, outcome. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.13`; `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs:66`; `src/Hexalith.ChatBot.Workers/Mailbox/MailboxIntakeWorkerResult.cs:49`; `src/Hexalith.ChatBot.Workers/Mailbox/ControlledMailboxPattern.cs`]

5. Given a mailbox source is quarantined, when existing workflow items that originated from that source are inspected, then they **remain visible, intact, and fully auditable** — quarantine affects **only future intake**, never already-captured intake records, associations, files, approvals, conversation content, or their audit trails (NFR17 visible/recoverable states; FR75c admins cannot mutate project-level records). Quarantine is reversible by a future release/re-activate flow; this story does not delete or rewrite any prior record. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR17`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75c`; `_bmad-output/implementation-artifacts/7-12-disable-mailbox-source.md`]

6. Given an actor encounters a quarantined mailbox source (admin viewing status, or an intake routed to quarantine), when guidance is surfaced, then **safe recovery guidance** is shown from the **finite message catalog** — a safe headline (≤ 80 chars), a one-line reason from a finite set conveying "contained for review", a next-action pointing to the responsible role (review/release is a mailbox-admin/two-person action), and a `disabled-action` reason — using `ChatBotMessageCatalog` / `ChatBotMessageCodes` / `ChatBotDisabledActionReasons`, never raw error text. The guidance conveys that reviewers may inspect **safe metadata only** and never names an unauthorized project/file/party/mailbox-content/audit detail (NFR2); EN/FR via existing localization; stable machine tokens (reason codes, state tokens, correlation ids) stay untranslated. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.13`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR76`; `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs:427`]

7. Given the quarantine subject and audit refs are metadata, when the proposal/approval/quarantined events and the audit envelope are serialized, then they carry **only safe, finite tokens** — the safe mailbox-source ref, `admin-operation:mailbox-source-quarantine[-approve]` / `admin-scope:mailbox`, the reason code, old/new state tokens (`active`/`quarantined`), policy-snapshot id, correlation id, UTC timestamp — and **never** mailbox subject/body, sender/recipient addresses, provider payloads, raw GUIDs that are not already safe identifiers, project/proposal/evidence content, raw claims/headers, bearer tokens, or secrets. The subject (mailbox source) is identified by its existing safe id (`MailboxSourceRef` via `AuditMetadata.SafeOptionalToken` / `IsSafeStableIdentifier`). Tenant scope comes **only** from the authenticated gateway binding — never from the command body. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`; `src/Hexalith.ChatBot.Server/Audit/AuditMetadata.cs`; `src/Hexalith.ChatBot.Contracts/Commands/MailboxSourceControlContracts.cs`]

8. Given public commands, queries, DTOs, or generated clients change for the quarantine proposal/approval, when the contract surface is updated, then the OpenAPI contract spine (`hexalith.chatbot.v1.yaml`) is updated **first** (mirroring the `SubmitMailboxSourceDisable`/`ApproveMailboxSourceDisable` schemas at lines ~4005/4054 and extending `MailboxSourceControlState` at line ~4108 with `quarantined`), `HexalithChatBotClient.g.cs` is **regenerated** (never hand-edited), the generated-client checksum (`tests/fixtures/hexalith-chatbot-generated-client.sha256`) is refreshed, and contract/client parity tests prove schema parity — exactly as Stories 7.2/7.3/7.12 did. The new command types are added to `ChatBotSpineCommandAllowlist` **only after** validation, authorization, audit, and tests are in place (fail-closed; never widen the allowlist first). [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/implementation-artifacts/7-12-disable-mailbox-source.md`; `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs:28`; `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml:4005`]

9. Given acceptance coverage runs, then tests prove: a single-actor quarantine never takes effect (proposal alone does not route intake); a distinct second human approver applies the quarantine; `RequesterRef == ApproverRef` and `RequesterActorId == approver UserId` are both rejected (gateway **and** aggregate); service clients and AI actors are denied for both proposal and approval even with tenant-admin-looking claims; a non-mailbox/non-tenant-admin scope is denied; a quarantined source routes `GraphMailboxIntakeWorker` intake to `Recoverable("mailbox_source_quarantined")` **before** fetch/submit (no restricted content read) while a sibling Active source is unaffected (isolation); quarantine does not mutate existing intake/association/audit records; the quarantine audit envelope carries actor/scope/subject/reason/old-state/new-state/policy-snapshot/timestamp with `StateTransition "Active->Quarantined"` and **no** mailbox-content/PII/`@`/`secret`/project leakage; audit-unavailable → no durable quarantine + no intake-routing side effect (fail closed); and OpenAPI/client/checksum parity holds. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy`; `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/MailboxSourceDisableAuthorizationTests.cs`; `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`; `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`]

## Tasks / Subtasks

- [x] Extend the control-state model with `Quarantined` and define the two-person command shape (AC: 1, 7)
  - [x] Add `Quarantined` to `src/Hexalith.ChatBot.Contracts/Enums/MailboxSourceControlState.cs` (`[EnumMember(Value = "quarantined")]`). The enum was explicitly shaped in 7.12 to receive this value; do not reorder existing members (`Active`, `Disabled`) — append `Quarantined` to preserve wire/serialization stability and any ordering tests.
  - [x] Decide and state (completion notes) whether to keep **subject-specific** quarantine commands (`SubmitMailboxSourceQuarantine`/`ApproveMailboxSourceQuarantine`, recommended — mirrors the 7.12 disable pair, lowest risk) or generalize to a subject-state control command pair shared by disable+quarantine. **Recommended:** subject-specific for 7.13, with a note that 7.15–7.26 may generalize if duplication across the five subject classes grows.

- [x] Add the quarantine proposal + approval command contracts (AC: 1, 2, 7, 8)
  - [x] Add `SubmitMailboxSourceQuarantine` and `ApproveMailboxSourceQuarantine` records implementing `IChatBotCommand` in `src/Hexalith.ChatBot.Contracts/Commands/MailboxSourceControlContracts.cs` (beside the disable pair). Carry only safe metadata mirroring `SubmitMailboxSourceDisable`/`ApproveMailboxSourceDisable`: a `QuarantineChangeId`, `MailboxSourceRef`, `ReasonCode`, `PolicySnapshotId`, `OldState`/`NewState` (`MailboxSourceControlState`), `SourceVersion`, `RequesterRef` (+ `ApproverRef` on approve), `SchemaVersion`, `CorrelationId`. No mailbox content, addresses, or secrets. Reuse `MailboxSourceControlSchemaVersions.V1` (or add a `Quarantine` schema-version constant if you keep the version set separable — state the choice).
  - [x] Reuse safe-token / finite-enum discipline; no free-form strings beyond tolerant parse at trust boundaries.

- [x] Add aggregate handlers with two-person enforcement (AC: 1, 2, 3, 5, 9)
  - [x] In `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs`, add `Handle(SubmitMailboxSourceQuarantine, state, envelope)` → validate via a new `IsValidMailboxSourceQuarantine`, dedupe (pending key on `QuarantineChangeId` **and** already-quarantined `QuarantinedMailboxSources[ref]` → `DomainResult.NoOp()`), then emit `MailboxSourceQuarantinePendingApproval` (record requester actor id + requester ref + subject + reason + old/new state + `SourceVersion + 1`). Mirror `Handle(SubmitMailboxSourceDisable)` (lines 165–197) exactly.
  - [x] Add `Handle(ApproveMailboxSourceQuarantine, state, envelope)` → look up the pending approval; **reject** unless `pending.SourceVersion == command.SourceVersion` AND subject/reason match AND `pending.RequesterRef != command.ApproverRef` (Ordinal) AND `pending.RequesterActorId != envelope.UserId`. On success emit `MailboxSourceQuarantined` carrying actor (approver), scope, subject, reason, old state (`Active`), new state (`Quarantined`), policy snapshot, timestamp, `SourceVersion + 1`. Mirror `Handle(ApproveMailboxSourceDisable)` (lines 199–240).
  - [x] Add `IsValidMailboxSourceQuarantine` / `IsValidMailboxSourceQuarantineApproval` private validators and a `RejectMailboxSourceQuarantine` helper mirroring lines 2855–2965; structured rejection reason codes `invalid_mailbox_source_quarantine`, `invalid_mailbox_source_quarantine_approval`, `mailbox_source_quarantine_unavailable`, `mailbox_source_quarantine_approval_scope_invalid`.
  - [x] Add the quarantine events to `src/Hexalith.ChatBot.Server/Governance/Mailbox/MailboxSourceControlEvents.cs`: `MailboxSourceQuarantinePendingApproval`, `MailboxSourceQuarantined`, `MailboxSourceQuarantineRejected` (mirror the disable triplet; `MailboxSourceQuarantined : IEventPayload`, `MailboxSourceQuarantineRejected : IRejectionEvent`).
  - [x] Add the pending-approval + quarantined dictionaries to `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` mirroring `_mailboxSourceDisablePendingApprovals` / `_disabledMailboxSources` (lines 43–44, 105–107, 323–335): `_mailboxSourceQuarantinePendingApprovals` (add-on-pending / remove-on-activate) and `_quarantinedMailboxSources` (set-on-activate), with public `IReadOnlyDictionary` accessors and `Apply` methods.

- [x] Authorize both commands (human + mailbox scope + distinct approver) (AC: 2, 9)
  - [x] In `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`, add per-command branches beside the disable block (lines 104–115): `SubmitMailboxSourceQuarantine` and `ApproveMailboxSourceQuarantine` require `AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox)` AND pass new `IsValidMailboxSourceQuarantine` / `IsValidMailboxSourceQuarantineApproval` validators (mirror `IsValidMailboxSourceDisable*` at lines 336–410: safe tokens for all refs, `SourceVersion >= 0`, non-empty `ReasonCode`/`PolicySnapshotId`, and `RequesterRef != ApproverRef` Ordinal on approval; add the `ReadSubmit*`/`ReadApprove*` deserialization helpers). Service/AI actors are denied via `HasHumanAdminScope`'s human-actor gate.
  - [x] In `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`, add the dispatcher-level distinct-approver guard for `ApproveMailboxSourceQuarantine` mirroring the disable guard (lines 213–219): reject when `RequesterRef == ApproverRef`.
  - [x] Use `ChatBotAuthorizationReasonCodes.AuthorizationDenied` on denial; keep authorization-denied indistinguishable from safe-not-found where required.

- [x] Extend the audit factory with the quarantine envelope (AC: 3, 7, 9)
  - [x] In `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`, add a quarantine ref block mirroring the disable block (lines 1387–1436): `admin-operation:mailbox-source-quarantine` (and `...-quarantine-approve` for approval), `admin-scope:mailbox`, `mailbox-source-quarantine-change:<id>` (via `PolicyEvidenceRefs(element, "quarantineChangeId", ...)`), `mailbox-source:<safe-ref>`, `policy-snapshot:<id>`, `reason:<code>`, `mailbox-source-old-state:active`, `mailbox-source-new-state:quarantined`, `admin-subject:<approver>` (approval only), and the source-version ref. Also add the quarantine command names to the admin-evidence command-type guard near lines 1123–1124.
  - [x] Add the `Active->Quarantined` lifecycle transition: register `Quarantined` in `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleStates.cs` (const + `All` list, append — do not reorder), add the `(Active, Quarantined)` transition to `LifecycleTransitionValidator.cs` (line ~29), and map `ApproveMailboxSourceQuarantine => new(LifecycleStates.Active, LifecycleStates.Quarantined)` in `CommandSubmissionLifecycleTransitionGuard.cs` (line ~20).
  - [x] Preserve `ChatBotStateWritingPathInventory` — the quarantine writes durable state **only** through `CommandGateway` + `IAuditWriter.RecordPreCommitAsync`; do not add a new state-writing path. Confirm audit-unavailable fails closed (no quarantine, no intake routing).

- [x] Add the command types to the spine allowlist — last, after validation/audit/tests (AC: 1, 8)
  - [x] Add `nameof(SubmitMailboxSourceQuarantine)` and `nameof(ApproveMailboxSourceQuarantine)` to `ChatBotSpineCommandAllowlist.AllowedCommandTypes` (beside the disable entries, line 28–29). Do **not** widen the allowlist before authorization + validation + audit + tests exist (the recurring Epic 7 review defect).

- [x] Route quarantined-source intake at the mailbox worker (AC: 4, 5, 9)
  - [x] In `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs`, add a quarantine branch beside the disable check (lines 66–71): when `pattern.ControlState == MailboxSourceControlState.Quarantined`, return `MailboxIntakeWorkerResult.Recoverable("mailbox_source_quarantined")` **before** `_source.FetchMessageAsync` and before building `CaptureMailboxMessageIntake` — so **no restricted content is fetched**. `ControlledMailboxPattern.ControlState` already carries the state (no signature change needed; the enum value is new).
  - [x] Add `mailbox_source_quarantined` to `MailboxIntakeWorkerResult.ResolveOwnerRole` (line 49–52) → `"mailbox-admin"` (same family as `mailbox_source_disabled`); classify as recoverable/await-admin, not poison. Confirm the worker never reads body/addresses/attachments for a quarantined source. Quarantine must not touch already-captured intake/association records.

- [x] Add safe recovery guidance to the message catalog (AC: 6)
  - [x] Add `ChatBotMessageCodes.MailboxSourceQuarantined = "mailbox_source_quarantined"` (beside `MailboxSourceDisabled`, line 65) and a finite catalog entry in `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` (beside the `MailboxSourceDisabled` entry, lines 427–433): safe headline ≤ 80 chars (e.g. `"Mailbox source quarantined."`), a one-line reason conveying "new mail is held for review; reviewers see safe metadata only", next-action pointing to mailbox-admin review/release, `ChatBotDisabledActionReasons.DisabledAction`, `ChatBotDetailVisibility.MetadataOnly`. Reuse the existing `disabled-action` reason (no new reason constant required) and state that choice.

- [x] Update public contract spine + regenerate client (AC: 8, 9)
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` **first**: add `SubmitMailboxSourceQuarantine` + `ApproveMailboxSourceQuarantine` schemas (copy the disable schemas at lines ~4005/4054, rename `disableChangeId`→`quarantineChangeId`) and add `quarantined` to the `MailboxSourceControlState` enum (line ~4108). Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` via the Client MSBuild target (never hand-edit), refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`, and extend client parity tests. Follow the exact workflow Story 7.12 used.

- [x] Optional S5 admin status surface (AC: 6) — _(decide; minimal if added)_
  - [x] If a quarantined-source indicator is added to the S5 tenant configuration / queue view, reuse the existing admin contract + `ChatBotUiTextKey` + `SharedResource.resx`/`.fr.resx`, keep tokens/reason-codes untranslated, and state it. Otherwise state no UI surface added (AC6 satisfied by the catalog + worker reason code, consistent with 7.12).

- [x] Add focused tests (AC: all)
  - [x] Authorization tests: add `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/MailboxSourceQuarantineAuthorizationTests.cs` mirroring `MailboxSourceDisableAuthorizationTests.cs`: human mailbox-admin/tenant-admin allowed for propose+approve; policy/compliance/operations scope denied; service-client + AI denied (even with tenant-admin claims); distinct-approver enforced; `RequesterRef==ApproverRef` denied at gateway **and** dispatcher.
  - [x] Aggregate tests (extend `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`): proposal alone produces pending (no quarantine); approval by same requester/actor rejected; distinct approver activates `MailboxSourceQuarantined`; subject/version/reason mismatch rejected; duplicate / already-quarantined → NoOp.
  - [x] Gateway/audit tests (extend `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`): audit-unavailable on the approval fails closed (no durable quarantine, no intake-routing side effect); quarantine envelope has actor/scope/subject/reason/old/new-state/policy-snapshot/timestamp + `StateTransition "Active->Quarantined"`; redaction — no mailbox content/PII/`@`/`secret`/project refs.
  - [x] Worker tests (extend `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`): quarantined source → `Recoverable("mailbox_source_quarantined")` before fetch/submit (assert `FetchMessageAsync` not called); sibling Active source unaffected (isolation); existing records untouched; owner role = mailbox-admin.
  - [x] Contract tests (extend `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` and `MessageCatalogContractTests.cs`): wire tokens/serialization for the two commands + the new `Quarantined` enum value, safe-token validation, no secret-bearing names; message-catalog finite-reason set still valid.
  - [x] Client/Conformance/Architecture/Lifecycle regression: client parity (OpenAPI/checksum), `LifecycleStateModelTests` updated for the added `Quarantined` state, conformance/architecture boundaries if new public/internal seams added. Record green counts.

## Dev Notes

### Scope Boundaries

- Story 7.13 is the **second** cell of the FR74 enforcement series (7.12–7.26: five subject classes — mailbox / service-client / AI-actor / command-capability / outbound — × three actions — disable / quarantine / rate-limit). It implements exactly **one** cell: **mailbox source × quarantine**. Disable (7.12) and quarantine (7.13) are **security-sensitive** admin operations gated by the **FR75d two-person rule**; rate-limit (7.14) is a standard policy mutation. Reuse the 7.12 propose→approve + control-state + intake-routing pattern; **do not** implement rate-limit, any other subject class, or the future quarantine-release/re-activate flow here. [Source: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR74`]
- **Quarantine ≠ Disable — the one meaningful semantic difference.** Disable (7.12) **blocks** future intake. Quarantine **contains new intake for review**: the epic says "new intake is routed to quarantine state and reviewers can inspect safe metadata without reading restricted content." At the worker, both block the normal pipeline **before fetch** (so quarantine, like disable, never reads restricted content). The distinction this story implements is the **reason code, owner/next-action guidance, and audit state token** (`quarantined` vs `disabled`), plus a distinct `Active->Quarantined` lifecycle transition. The full **reviewer quarantine-inspection surface** (an S-surface listing held items with safe metadata) is a larger build — see the deferral note below — recommended **deferred**, consistent with 7.12's deferred read-side projection.
- This is **NOT** the Story 7.3 mailbox-configuration path. Story 7.3's `SubmitMailboxConfigurationChange` (mailbox-admin, single-actor) configures patterns/routing/connections and carries a per-pattern `IsEnabled` flag. The FR74 quarantine is a **separate, two-person, security-sensitive governance control** with documented justification recorded in audit. **Do not** route quarantine through `SubmitMailboxConfigurationChange`; doing so bypasses the two-person rule.
- Quarantine affects **future intake only**. It must never delete, rewrite, or hide already-captured intake records, associations, files, approvals, conversation content, or their audit trails (NFR17, FR75c). It is reversible by a future release flow (out of scope here).
- Tenant identity is always from the **authenticated gateway binding**, never the command body. The subject (mailbox source) and all audit refs are **metadata-only, safe tokens** (NFR2).

### Existing Code To Reuse — mirror the 7.12 disable, do not fork

- **The whole quarantine cell is the disable cell with `Quarantined` in place of `Disabled`.** Story 7.12 (commit `91723ce`) shipped every seam you need; copy each and rename. Canonical anchors:
  - Enum: `src/Hexalith.ChatBot.Contracts/Enums/MailboxSourceControlState.cs` — append `Quarantined` (the comment already anticipates it).
  - Commands: `src/Hexalith.ChatBot.Contracts/Commands/MailboxSourceControlContracts.cs` — `SubmitMailboxSourceDisable`/`ApproveMailboxSourceDisable` field sets to mirror.
  - Aggregate: `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` — `Handle(SubmitMailboxSourceDisable)` (165–197), `Handle(ApproveMailboxSourceDisable)` (199–240), validators `IsValidMailboxSourceDisable*` (2855–2880), `RejectMailboxSourceDisable` (2954–2965). The distinct-approver reject condition is at lines 215–222 (`pending.RequesterRef == command.ApproverRef` OR `pending.RequesterActorId == envelope.UserId` OR subject/version/reason mismatch).
  - State: `src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs` — `_mailboxSourceDisablePendingApprovals` / `_disabledMailboxSources` (43–44, 105–107) + `Apply(MailboxSourceDisablePendingApproval)` / `Apply(MailboxSourceDisabled)` (323–335).
  - Events: `src/Hexalith.ChatBot.Server/Governance/Mailbox/MailboxSourceControlEvents.cs` — `MailboxSourceDisablePendingApproval` / `MailboxSourceDisabled` / `MailboxSourceDisableRejected` triplet to mirror.
  - Authorization: `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` — disable branches (104–115), validators + deserialization helpers (336–410). Dispatcher distinct-approver guard: `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs` (199–219).
  - Audit: `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` — disable ref block (1387–1436) + command-type guard (1123–1124). Use `AuditMetadata.SafeOptionalToken` / `IsSafeStableIdentifier` (do not invent new ref formats).
  - Lifecycle: `LifecycleStates.cs` (Active/Disabled at 16–17, 32–33), `LifecycleTransitionValidator.cs` (`(Active, Disabled)` at 29), `CommandSubmissionLifecycleTransitionGuard.cs` (`ApproveMailboxSourceDisable => (Active, Disabled)` at 20).
  - Worker: `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs` — the `ControlState == Disabled` block (66–71) sits **after** `ResolvePatternAsync` and **before** `FetchMessageAsync`; insert the `Quarantined` branch there. `MailboxIntakeWorkerResult.cs` `ResolveOwnerRole` (49–52). `ControlledMailboxPattern.cs` already carries `ControlState` (no signature change).
  - Spine allowlist: `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` (disable entries at 28–29).
  - Catalog: `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs` (`MailboxSourceDisabled` at 65), `ChatBotMessageCatalog.cs` (entry at 427–433), `ChatBotDisabledActionReasons.cs` (`DisabledAction` — reuse).
  - Public contract workflow: `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` (disable schemas ~4005/4054, `MailboxSourceControlState` ~4108) → regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` → refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
  - Test anchors: `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/MailboxSourceDisableAuthorizationTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` + `MessageCatalogContractTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs`.

### Current State To Preserve

- The two-person rule is enforced **three** times for disable — gateway validation (`ParticipantAuthorizationStage`), the `AcceptedCommandDispatcher` guard, **and** the aggregate (`Handle(ApproveMailboxSourceDisable)`). Preserve all three layers for quarantine; do not rely on the gateway check alone.
- `AdminAuthorityEvaluator` already denies service/AI actors with tenant-admin-looking claims; **generalize, don't weaken**. Quarantine must be human + mailbox/tenant-admin only.
- `GraphMailboxIntakeWorker` returns `Recoverable` (not poison) for scope mismatches and the disabled block, before fetch; the quarantine block must be a sibling recoverable outcome — never a silent drop, never a crash, and **never a content fetch**.
- `CommandGateway` suppresses dispatch when pre-commit audit is unavailable; quarantine must reuse this — no durable quarantine state, no intake-routing effect, when audit is down.
- Quarantine changes **only future intake**; already-captured `CaptureMailboxMessageIntake` records, associations, and audit trails are immutable and must stay readable/auditable (NFR17).
- **Submodules:** Existing root submodule policy applies — initialize/update only root `.gitmodules` submodules; **never** recursive submodule commands. **Do not bump submodule pointers** — Stories 7.1/7.5–7.10 reviews each caught undocumented gitlink bumps that broke the build. Reset stray gitlink drift via `git submodule update -- <path>` (non-recursive) before finishing; keep the File List exact. (See [[story-automator-session-monitoring]] submodule guard.)
- **Append-only enum/state ordering:** `MailboxSourceControlState` and `LifecycleStates.All` have stability/ordering tests (`LifecycleStateModelTests.StateVocabularyShouldBeStableAndOrdered` had to be updated when `Disabled` was added). Append `Quarantined` at the end and update those tests deliberately.

### Architecture Guardrails

- Contracts (commands, enums, DTOs) → `src/Hexalith.ChatBot.Contracts`. Aggregate handlers/state/events → `src/Hexalith.ChatBot.Server/Operations` + `src/Hexalith.ChatBot.Server/Governance/Mailbox`. Authorization → `Gateway/Stages` + `Governance/Admin`. Audit refs → `src/Hexalith.ChatBot.Server/Audit`. Lifecycle → `Server/Lifecycle/StateModel`. Intake routing → `src/Hexalith.ChatBot.Workers/Mailbox`. No new top-level project.
- Use records/enums/finite tokens, never delimited strings or ad-hoc dictionaries, for state/role/scope/reason.
- Tenant id from authenticated binding only. Deny-by-default on missing/ambiguous/invalid admin role/scope claims; unknown roles imply no access.
- The quarantine control state is a **fixed FR74 governance control**, not a tenant-tunable knob; it is set only through the two-person submit→approve path, never the mailbox-config path.
- Every committed quarantine is preceded by a fail-closed pre-commit audit; on audit-unavailable, no durable/observable side effect. No state-write that bypasses audit.

### Previous Story Intelligence

- **Story 7.12** (disable mailbox source, commit `91723ce`) is the direct template — it shipped the `MailboxSourceControlState` enum (shaped to accept `Quarantined`), the `Submit/ApproveMailboxSourceDisable` command pair, the aggregate handlers + state dictionaries + events, three-layer two-person enforcement (gateway + dispatcher + aggregate), the `AuditEnvelopeFactory` ref block with `StateTransition "Active->Disabled"`, the `Active→Disabled` lifecycle transition, the worker pre-fetch block with `Recoverable("mailbox_source_disabled")`, the message-catalog entry, and the OpenAPI→client→checksum workflow. **Copy each seam and substitute Quarantine/Quarantined.** Its review (2026-06-02) approved with 0 Critical/High/Medium and explicitly sanctioned deferring the **read-side projection** that makes the worker's `IMailboxConfigurationProvider` observe the control-state event (only the worker's `StaticMailboxConfigurationProvider` + test fakes set `ControlState` today).
- **Story 7.2** shipped the underlying two-person tenant-policy pattern (`SubmitTenantPolicyChange`→`ApproveTenantPolicyChange`); 7.12 already adapted it for mailbox-source — you adapt 7.12, not 7.2.
- **Story 7.1** shipped `AdminRole`/`AdminScope`/`AdminAuthorityEvaluator` (human-only, service/AI denied) — the authorization foundation reused unchanged.
- Recurring Epic 7 review defects to avoid: empty audit-obligation/reason fields; unsafe affected refs; relaxed authorization; **adding a command to the spine allowlist before validation/audit/tests**; counters/state that trust client-supplied tenant ids; undocumented submodule pointer bumps; inexact File List / stale debug-log counts; mailbox content/PII leaking into audit refs. (See [[tier3-live-dapr-run]] for the live E2E posture if you exercise the worker beyond unit tests.)

### Latest Technical Specifics

- No external version research required. Repo-pinned stack; do not upgrade packages: .NET SDK `10.0.300`, `net10.0`, central package management, xUnit v3, Shouldly, NSubstitute, Blazor/FrontComposer, Fluent UI v5 RC, Fluxor, existing OpenAPI/NSwag generated-client tooling.
- Do not change target frameworks, Aspire/Dapr topology, Fluent UI, Fluxor, NSwag/client-generation tooling, MCP SDK, Graph permission posture, WORM audit assumptions, or submodule pointers — except the OpenAPI regeneration the new public commands require (AC8).
- Use Ordinal string comparison for all ref/id equality (matches existing two-person checks). No `GetHashCode()` fingerprints for identity.
- Note: `quarantine`/`Quarantine` already exist as `NotificationStateClass.Quarantine` and `AdminQueueOperation.Quarantine` tokens — these are **unrelated** to the new `MailboxSourceControlState.Quarantined` control state; do not conflate or reuse them for the FR74 control.

### Testing Notes

- Minimum validation before dev handoff (public commands change → Client parity required):
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` (OpenAPI/generated-client parity — required).
  - `./tests/Hexalith.ChatBot.Conformance.Tests/...` / `Architecture.Tests` as regression. Record green counts.
- Highest-value targets: the three-layer two-person enforcement (single-actor no-op, distinct-approver applies, same-person rejected at gateway, dispatcher, **and** aggregate, service/AI denied); the intake-routing isolation (quarantined source routed before fetch with no content read, sibling Active source unaffected, existing records untouched); the fail-closed audit (audit-unavailable → no quarantine + no routing); and the metadata-only redaction (no mailbox content/PII/`@`/`secret`/project refs in events/envelope).
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer the compiled in-process xUnit v3 runners after build.
- Keep nullable, warnings-as-errors, central package management, xUnit v3, Shouldly, metadata-only fixtures, and root-level submodule policy.

### Project Structure Notes

- New: quarantine command records (`Contracts/Commands/MailboxSourceControlContracts.cs` — extend existing file), quarantine events (`Server/Governance/Mailbox/MailboxSourceControlEvents.cs` — extend), aggregate handlers + validators + state dictionaries, authorization validators + dispatcher guard, audit ref block, lifecycle state + transition, spine allowlist entries, worker quarantine branch + reason code, message-catalog entry, OpenAPI + regenerated client + checksum, plus a new `MailboxSourceQuarantineAuthorizationTests.cs`.
- **Variances to decide and state in completion notes:** (1) subject-specific (`SubmitMailboxSourceQuarantine`) vs generic subject-state-control command pair shared with disable — recommended: subject-specific for 7.13; (2) reuse `MailboxSourceControlSchemaVersions.V1` vs a dedicated quarantine schema-version constant — recommended: reuse V1 unless contract tests require separation; (3) reuse the `disabled-action` catalog reason vs a new `quarantined`-flavored reason constant — recommended: reuse `disabled-action` (finite set already covers "contained"); (4) whether an S5 admin/queue status surface or a reviewer quarantine-inspection surface is added now — **recommended: deferred** (catalog + worker reason code satisfy AC6; the full reviewer "inspect safe metadata" surface is a later layering stage, mirroring 7.12's deferred read-side projection); (5) the durable read-side that lets the worker's `IMailboxConfigurationProvider` observe `MailboxSourceQuarantined` — recommended: **deferred** like 7.12 (only `StaticMailboxConfigurationProvider` + test fakes set `ControlState`); ensure the aggregate/event/worker contract are testable in isolation and state the deferral explicitly in completion notes.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 7.13` — Quarantine mailbox source: new intake routed to quarantine state, reviewers inspect safe metadata without restricted content; audit of actor/scope/subject/reason/old-state/new-state/policy-snapshot/timestamp with no skip-audit (FR74, FR75g); two-person rule (FR75d); not performable by service clients or AI actors.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR74` — disable/quarantine/rate-limit for mailbox/service-client/AI-actor/command-capability/outbound; decomposition guidance (disable+quarantine security-sensitive two-person, rate-limit standard).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR75d`, `#FR75e`, `#FR75g` — two-person rule + documented justification in audit; mailbox-admin scope; audit obligation on every admin action, no skip-audit, tenant-admin no bypass.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2`, `#NFR15a`, `#NFR17`, `#FR76` — redaction; fail-closed audit; visible/recoverable states; disabled-action reasons + next-step guidance.
- `_bmad-output/planning-artifacts/architecture.md` — API & Communication Patterns (command spine, two-phase audit, fail-closed), Project Structure & Boundaries, Testing Strategy.
- `_bmad-output/implementation-artifacts/7-12-disable-mailbox-source.md` — the direct template: control-state enum, submit→approve commands, aggregate/state/events, three-layer two-person enforcement, audit refs + `Active->Disabled` transition, worker pre-fetch block, catalog entry, OpenAPI/client workflow, and the sanctioned read-side-projection deferral.
- `_bmad-output/implementation-artifacts/7-2-policy-admin-scope-tenant-policy-schema-editor-and-ai-action-policy.md` — the underlying two-person submit→approve precedent.
- `_bmad-output/implementation-artifacts/7-1-tenant-admin-permission-model-and-bounded-scopes.md` — admin roles/scopes, `AdminAuthorityEvaluator`, metadata-only admin audit refs.
- `src/Hexalith.ChatBot.Contracts/Enums/MailboxSourceControlState.cs`; `src/Hexalith.ChatBot.Contracts/Commands/MailboxSourceControlContracts.cs` — enum + disable command pair to extend.
- `src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs` (165–240, 2855–2965), `GovernedOperationState.cs` (43–44, 105–107, 323–335); `src/Hexalith.ChatBot.Server/Governance/Mailbox/MailboxSourceControlEvents.cs` — two-person handlers/state/events to mirror.
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` (104–115, 336–410); `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs` (199–219); `src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs` — authorization + distinct-approver validation.
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (1123–1124, 1387–1436), `AuditMetadata.cs`; `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`, `ChatBotStateWritingPathInventory.cs` — audit refs + fail-closed seam.
- `src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleStates.cs`, `LifecycleTransitionValidator.cs`, `CommandSubmissionLifecycleTransitionGuard.cs` — add `Quarantined` + `Active->Quarantined`.
- `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs` (58–71), `ControlledMailboxPattern.cs`, `MailboxIntakeWorkerResult.cs` (49–52), `IMailboxConfigurationProvider.cs` — intake-routing seam.
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` (28–29); `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` (427–433), `ChatBotMessageCodes.cs` (65), `ChatBotDisabledActionReasons.cs` — spine allowlist, safe recovery text.
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` (4005/4054/4108); `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; `tests/fixtures/hexalith-chatbot-generated-client.sha256` — public contract regeneration.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/MailboxSourceDisableAuthorizationTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs` + `MessageCatalogContractTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs` — test anchors to extend.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Opus 4.8, 1M context)

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 Warning(s), 0 Error(s). The Client MSBuild NSwag target regenerated `HexalithChatBotClient.g.cs` from the updated OpenAPI spine on build.
- Compiled in-process xUnit v3 runners (`-parallel none`), all green:
  - Contracts.Tests — Total 253, Failed 0
  - Client.Tests — Total 17, Failed 0 (OpenAPI/generated-client checksum parity)
  - Server.Tests — Total 701, Failed 0
  - Workers.Tests — Total 24, Failed 0
  - Conformance.Tests — Total 75, Failed 0 (regression)
  - Architecture.Tests — Total 37, Failed 0 (regression)
- Generated-client SHA256 refreshed: `f93d79a8…` → `8f2b9d08…` in `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- `git submodule status` — no submodule pointer drift introduced.

### Completion Notes List

- Implemented the **mailbox-source × quarantine** FR74 cell by mirroring the Story 7.12 disable cell seam-for-seam, substituting `Quarantine`/`Quarantined` for `Disable`/`Disabled`. No fork of the disable logic; the two cells run side by side.
- **Variance decisions (as required by Dev Notes):**
  1. **Subject-specific commands** — kept `SubmitMailboxSourceQuarantine`/`ApproveMailboxSourceQuarantine` (mirrors the 7.12 disable pair, lowest risk). 7.15–7.26 may generalize to a shared subject-state control command pair if duplication across the five subject classes grows.
  2. **Schema version** — reused `MailboxSourceControlSchemaVersions.V1` (the command shape is identical to disable); no dedicated quarantine schema-version constant added.
  3. **Catalog reason** — reused the existing `disabled-action` `ChatBotDisabledActionReasons` constant (the finite set already conveys "contained"); no new reason constant.
  4. **S5 admin/queue status surface** — **deferred** (no UI surface added). AC6 is satisfied by the `ChatBotMessageCatalog` entry + the worker `mailbox_source_quarantined` reason code, consistent with 7.12.
  5. **Durable read-side projection** (worker's `IMailboxConfigurationProvider` observing `MailboxSourceQuarantined`) — **deferred** like 7.12; today only `StaticMailboxConfigurationProvider` + test fakes set `ControlState`. The aggregate/event/worker contract is tested in isolation.
- **Three-layer two-person enforcement** preserved for quarantine: gateway validation (`ParticipantAuthorizationStage` — `RequesterRef != ApproverRef` Ordinal + human mailbox/tenant-admin scope), the `AcceptedCommandDispatcher` distinct-approver guard, **and** the aggregate (`Handle(ApproveMailboxSourceQuarantine)` rejects same `RequesterRef`/`RequesterActorId`, plus subject/version/reason mismatch). Service clients and AI actors are denied via `AdminAuthorityEvaluator.HasHumanAdminScope`'s human-actor gate.
- **Fail-closed audit** reused unchanged: the quarantine commits durable state only through `CommandGateway` + `IAuditWriter.RecordPreCommitAsync`; audit-unavailable → no durable quarantine and no intake-routing side effect (verified by `MailboxSourceQuarantineApprovalPreCommitAuditUnavailableShouldFailClosedAndNeverDispatch`). No new state-writing path added.
- **Audit envelope** carries actor/scope/subject/reason/old-state(`active`)/new-state(`quarantined`)/policy-snapshot/source-version/timestamp with `StateTransition "Active->Quarantined"` (derived from the new `(Active, Quarantined)` lifecycle transition + `ApproveMailboxSourceQuarantine` submission-guard mapping). Redaction asserted: no `@`/`secret`/mailbox content/`project-` leakage.
- **Worker routing**: the `Quarantined` branch sits beside the `Disabled` branch — after `ResolvePatternAsync`, **before** `FetchMessageAsync` and `CaptureMailboxMessageIntake` — returning `Recoverable("mailbox_source_quarantined")` so no restricted content is fetched. Owner role = `mailbox-admin`; recoverable/await-admin (no auto-retry, no poison). Sibling Active source unaffected (isolation verified).
- **Append-only ordering** honored: `Quarantined` appended to `MailboxSourceControlState`, `LifecycleStates.All`; `LifecycleStateModelTests.StateVocabularyShouldBeStableAndOrdered` updated deliberately.
- **Spine allowlist widened last** — `SubmitMailboxSourceQuarantine`/`ApproveMailboxSourceQuarantine` added to `ChatBotSpineCommandAllowlist` only after validation + authorization + audit + tests were in place (avoiding the recurring Epic 7 review defect).
- **OpenAPI-first**: the contract spine was updated first (quarantine schemas + `quarantined` enum value), the generated client was regenerated via the MSBuild NSwag target (never hand-edited), and the checksum fixture refreshed; client parity test green.

### File List

- src/Hexalith.ChatBot.Contracts/Enums/MailboxSourceControlState.cs
- src/Hexalith.ChatBot.Contracts/Commands/MailboxSourceControlContracts.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs
- src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs
- src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml
- src/Hexalith.ChatBot.Server/Governance/Mailbox/MailboxSourceControlEvents.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs
- src/Hexalith.ChatBot.Server/Operations/GovernedOperationState.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs
- src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs
- src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs
- src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleStates.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/LifecycleTransitionValidator.cs
- src/Hexalith.ChatBot.Server/Lifecycle/StateModel/CommandSubmissionLifecycleTransitionGuard.cs
- src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs
- src/Hexalith.ChatBot.Workers/Mailbox/MailboxIntakeWorkerResult.cs
- src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs (regenerated by MSBuild NSwag target)
- tests/fixtures/hexalith-chatbot-generated-client.sha256
- tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/MailboxSourceQuarantineAuthorizationTests.cs (new)
- tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs
- tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs
- tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs
- tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs
- tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (story-automator adversarial review) · **Date:** 2026-06-02 · **Outcome:** Approve (auto-fixed)

**Scope:** Validated all story claims against actual implementation, seam-by-seam against the Story 7.12 disable template. Build clean (0 warnings / 0 errors, client regenerated by the NSwag MSBuild target). All suites re-run green: Contracts 253, Client 17 (OpenAPI/checksum parity), Server 701, Workers 24, Conformance 75, Architecture 37. No submodule pointer drift.

**Acceptance Criteria — all 9 verified IMPLEMENTED:**
- AC1/AC2 two-person rule enforced in all three layers (gateway `ParticipantAuthorizationStage`, `AcceptedCommandDispatcher` guard, aggregate `Handle(ApproveMailboxSourceQuarantine)` re-check of `RequesterRef`/`RequesterActorId`); human + mailbox/tenant-admin only; service/AI denied — proven by `MailboxSourceQuarantineAuthorizationTests` + dispatcher tests.
- AC3/AC7 audit envelope carries actor/scope(`admin-scope:mailbox`)/subject/reason/old-state(`active`)/new-state(`quarantined`)/policy-snapshot/source-version/timestamp with `StateTransition "Active->Quarantined"`; redaction asserted (no `@`/`secret`/`project-`); fail-closed (503 + `DispatchCount == 0` + pre-commit replay) verified.
- AC4/AC5 worker routes `Quarantined` to `Recoverable("mailbox_source_quarantined")` **before** `FetchMessageAsync`/`CaptureMailboxMessageIntake` (`FetchCount == 0`); sibling Active source unaffected (`FetchCount == 1`, Submitted); owner role `mailbox-admin`.
- AC6 finite catalog entry (headline 27 chars ≤ 80, metadata-only, `disabled-action` reason); AC8 OpenAPI-first + regenerated client + refreshed checksum + parity test green; AC9 full acceptance matrix present and green.

**Findings (0 Critical · 0 High · 1 Medium · 1 Low) — all auto-fixed:**
- **[Medium] Inexact File List** (recurring Epic 7 defect): `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs` was modified (2 new quarantine dispatcher tests) but omitted from the Dev Agent Record → File List. **Fixed:** added to File List.
- **[Low] Stale debug-log count:** Debug Log claimed Server.Tests Total 699; actual is 701 (the +2 undocumented dispatcher tests). **Fixed:** corrected to 701.

No code defects found: the variance decisions (subject-specific commands, V1 schema reuse, `disabled-action` reuse, deferred S5 surface, deferred read-side projection) are stated and consistent with the 7.12 precedent. Append-only enum/state ordering honored; spine allowlist widened last.

---

**Reviewer:** Jérôme Piquot (story-automator adversarial review) · **Date:** 2026-06-11 · **Outcome:** Approve (auto-fixed)

**Scope:** Re-review of Story 7.13 after the story-automator QA/automate step added a public-spine HTTP admission E2E test for the quarantine two-person flow (`CommandGatewayAdmissionApiE2ETests.CommandGatewayApi_ShouldAcceptMailboxSourceQuarantineTwoPersonFlowThroughUiSpine`). Re-validated all 9 ACs seam-by-seam against the now-committed implementation (the repo is several stories ahead of 7.13). Build clean (Server.Tests project, 0 warnings / 0 errors; client regenerated by the NSwag MSBuild target on build). Suites re-run green via the compiled in-process xUnit v3 runners: **Server 1592** (+1 vs the 7.12-cycle 1591, the new quarantine E2E test), **Contracts 482**, **Client 35** (OpenAPI/generated-client checksum parity), **Workers 31**. No submodule pointer drift.

**Acceptance Criteria — all 9 re-verified IMPLEMENTED against real code:**
- AC1/AC2 two-person rule enforced in all three layers — gateway `ParticipantAuthorizationStage` (`HasHumanAdminScope(..., AdminScope.Mailbox)` + `IsValidMailboxSourceQuarantine[Approval]`), `AcceptedCommandDispatcher` distinct-approver guard (`RequesterRef == ApproverRef` rejected), and aggregate `Handle(ApproveMailboxSourceQuarantine)` (rejects `pending.RequesterRef == command.ApproverRef` Ordinal **and** `pending.RequesterActorId == envelope.UserId`, plus subject/version/reason mismatch); human + mailbox/tenant-admin only, service/AI denied.
- AC3/AC7 audit envelope carries `admin-operation:mailbox-source-quarantine[-approve]`, `admin-scope:mailbox`, safe `mailbox-source` ref, reason, `mailbox-source-old-state:active` / `-new-state:quarantined`, policy-snapshot, source-version, timestamp, `admin-subject:<approver>` on approval, with `StateTransition "Active->Quarantined"`; the new E2E asserts these refs at the HTTP boundary plus metadata-only redaction (no tenant id, mailbox-source detail, `@`, or `secret` in response bodies).
- AC4/AC5 worker routes `Quarantined` → `Recoverable("mailbox_source_quarantined")` **before** `FetchMessageAsync`/`CaptureMailboxMessageIntake`; owner role `mailbox-admin`; sibling Active source unaffected.
- AC6 finite catalog entry (headline 26 chars ≤ 80, `MetadataOnly`, `disabled-action` reason); AC8 OpenAPI-first + regenerated client + checksum parity (Client 35 green); AC9 acceptance matrix green and now extended with the HTTP admission E2E.

**Findings (0 Critical · 0 High · 1 Medium · 0 Low) — auto-fixed:**
- **[Medium] Inexact File List** (recurring Epic 7 defect): the QA/automate step modified `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` (new quarantine two-person admission E2E + generalized `MailboxSourceControlSubmissionRequest` helper carrying explicit command/task ids while preserving disable-test behavior) but it was absent from the Dev Agent Record → File List. **Fixed:** added to File List.

QA test quality verified — real assertions, not placeholders: distinct command/task ids so coarse idempotency does not collapse the pair; correct two-person version convention (submit v4 → pending v5 → approve v5); pre/post-commit audit phase sequence asserted; command-name, state-transition, evidence-ref, and redaction assertions all concrete. No code defects. The historical 2026-06-02 Debug Log green counts (Server 701, Contracts 253, etc.) are intentionally left unchanged — they were accurate at dev time; the repo being several stories ahead makes them read low, which is expected, not a defect.

## Change Log

| Date       | Version | Description                                                                                          | Author |
| ---------- | ------- | ---------------------------------------------------------------------------------------------------- | ------ |
| 2026-06-02 | 0.1     | Implemented Story 7.13 quarantine mailbox source (FR74/FR75d): contracts, aggregate two-person handlers, authorization + dispatcher guard, audit envelope + `Active->Quarantined` lifecycle, worker intake routing, message catalog, OpenAPI/client regeneration, and focused tests. All suites green. Status → review. | Amelia (Dev) |
| 2026-06-02 | 0.2     | Adversarial code review (story-automator). All 9 ACs verified implemented against real code; build clean (0 warn/0 err); suites re-run green (Contracts 253, Client 17, Server 701, Workers 24, Conformance 75, Architecture 37). 0 Critical, 0 High, 1 Medium + 1 Low — both documentation-only — auto-fixed: File List was missing `AcceptedCommandDispatcherTests.cs`; stale Server.Tests count (699→701). No submodule drift. Status → done. | Senior Reviewer (AI) |
| 2026-06-11 | 0.3     | Re-review after story-automator QA step added an HTTP admission E2E for the quarantine two-person flow (`CommandGatewayAdmissionApiE2ETests`). All 9 ACs re-verified against committed code; build clean; suites green (Server 1592, Contracts 482, Client 35, Workers 31). 0 Critical, 0 High, 1 Medium (recurring inexact-File-List defect) — auto-fixed: added `CommandGatewayAdmissionApiE2ETests.cs` to File List. QA test quality confirmed (real assertions, distinct ids, correct version convention, redaction). No submodule drift. Status remains done. | Senior Reviewer (AI) |
</content>
</invoke>
