---
baseline_commit: 77e636ddf305b13016c0436c528b8383ecd3d896
---

# Story 9.10: Consent and lawful-basis metadata

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance administrator,
I want consent and lawful-basis metadata recorded where policy requires it,
so that external participant, retained content, attachment, and AI-processing records have defensible governance.

## Acceptance Criteria

1. **Where tenant policy or regulatory profile requires it, recording an external participant, retained content, an attachment, or an AI-processing event captures consent or lawful-basis metadata that is queryable for authorized compliance review (FR20, NFR55).**
   **Given** a governed subject of one of the four `ConsentSubjectKinds` (`external-participant`, `retained-content`, `attachment`, `ai-processing`) whose requirement profile marks consent/lawful-basis `required`,
   **When** the metadata is recorded via the `SubmitConsentLawfulBasisRecord` governed command,
   **Then** the run records a `ConsentLawfulBasisRecord` carrying the subject kind, an opaque project-scoped subject locator, a closed-set `ConsentLawfulBases` basis (`consent` / `contract` / `legal-obligation` / `vital-interests` / `public-task` / `legitimate-interests`), a `ConsentRecordStatuses` status (`active` / `withdrawn` / `expired` / `superseded`), a safe `BasisSource` token, and the `RedactionSensitivity` (a `DataClassRedactionSensitivities` member — **reuse**, do not fork), all **correlation-stamped** (`CorrelationId` + `PolicySnapshotId`) and reconstructable for authorized compliance review from the audit chain alone.
   - **Reuse the Story 9.7 redaction-sensitivity set + the canonical compliance token helpers as single sources of truth; do NOT re-declare either.** `RedactionSensitivity` is a `DataClassRedactionSensitivities` member; all tokens validate through `ComplianceAdministrationSchema.IsSafeComplianceToken`/`IsSafeFingerprint`/`IsUtc`. The subject locator is an `AuditMetadata.IsSafeStableIdentifier` opaque ref — **never** raw participant email, file name, or message body.
   - **Closed, bounded token sets (each value an `AuditMetadata`-safe token).** Define: **subject kind** `ConsentSubjectKinds` `{ external-participant, retained-content, attachment, ai-processing }`; **lawful basis** `ConsentLawfulBases` `{ consent, contract, legal-obligation, vital-interests, public-task, legitimate-interests }`; **record status** `ConsentRecordStatuses` `{ active, withdrawn, expired, superseded }`; **requirement disposition** `ConsentRequirementDispositions` `{ required, not-required }`; **gate decision** `ConsentGateDecisions` `{ satisfied, blocked-missing-basis }`. Each is a static class with `All` + `Contains`, mirroring `TenantExportClassDispositions`/`DataClassDeletionBehaviors` line-for-line. Callers select within the set, never invent members.

2. **Consent/lawful-basis metadata reads are subject to per-project redaction and only authorized compliance review can query them (NFR2, FR75f).**
   **Given** consent/lawful-basis metadata for a project,
   **When** it is read or queried,
   **Then** access requires the human compliance-admin scope **and** per-project authority over the subject's project; a reader lacking per-project authority gets a result **indistinguishable from safe-not-found** — restricted detail redacted, **no** subject locator, project ref, participant, file, or message identity leaked, escalation offered without revealing the hidden resource (exactly like the Story 9.3 compliance read surface).
   - **Mirror `ComplianceAuditReadPolicy.HasPerProjectAuthority` and the Story 9.8 `TenantExportAuthorizationPolicy` exactly — do NOT build a third authority path.** Add a server-side `ConsentLawfulBasisAuthorizationPolicy` that (a) gates on `AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Compliance)` and (b) computes per-project authority from `principal.FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)` filtered by `AuditMetadata.IsSafeStableIdentifier`, projecting into a **bounded** `ConsentLawfulBasisAuthorityView(bool HasComplianceScope, IReadOnlySet<string> AuthorizedProjectRefs)`. The pure `ConsentLawfulBasisRedactionPolicy.Redact(...)` (or the read projection) in `.Contracts` consumes that bounded view (never a `ClaimsPrincipal`). When a record's project scope ref is not in `AuthorizedProjectRefs` (or `!HasComplianceScope`), the read **drops the subject locator and project ref** — the redacted shape is the only signal, never the resource identity (NFR2).

3. **Recording or changing consent/lawful-basis metadata is audited with actor, basis, and timestamp (NFR50).**
   **Given** a consent/lawful-basis record is created or changed,
   **When** committed,
   **Then** the run routes through the one CommandGateway audit-commit spine and the committed envelope carries the NFR50 required fields — actor, actor type, command, resource, decision, reason, correlation, timestamp, policy-snapshot ref — plus the source-evidence refs `consent-record:{id}`, `consent-subject-kind:{kind}`, `consent-lawful-basis:{basis}`, `consent-record-status:{status}`, `consent-basis-source:{source}`, `consent-scope-project:{ref}` (authorized refs only), and the `consent-fingerprint:` ref. No consent recording/change path skips audit.

4. **When tenant policy requires consent/lawful-basis but it is absent, a governed action (e.g., AI processing or retention) fails closed pending the metadata (NFR7, FR68).**
   **Given** a subject kind whose requirement profile marks consent/lawful-basis `required`,
   **When** a governed action over that subject is attempted and no `active` lawful-basis record exists,
   **Then** the pure `ConsentGate.Evaluate(...)` returns `blocked-missing-basis` and the governed action **fails closed** — it performs no state mutation and surfaces a safe, redacted reason (NFR2) naming no restricted resource; once an `active` basis is recorded the same gate returns `satisfied` and the action may proceed. Where the subject kind is `not-required`, the gate returns `satisfied` without a basis record.
   - **Fail-closed is absolute over convenience.** An **unknown / unclassifiable** subject kind or a **missing requirement profile** biases to `required` ⇒ `blocked-missing-basis` (never silently `satisfied`). A `withdrawn` / `expired` / `superseded` record does **not** satisfy a `required` gate — only an `active` basis does.

### Cross-cutting requirements that hold for every AC

- **Define-once / reuse — do NOT reinvent.** Consume by reference: the Story 9.7 `DataClassRedactionSensitivities` (the **one** sensitivity set) and the canonical `ComplianceRetentionClassIds` spine; the Story 7.4 `RetentionValidationResult` (the **one** validation-result type) + `ComplianceAdministrationSchema` (`IsSafeComplianceToken`, `IsSafeFingerprint`, `IsUtc`); the Story 9.8 `SubmitTenantExportRequest` / Story 9.9 `SubmitDeletionErasureRequest` shapes as the **structural template** for the governed-command/planner/schema/authority/audit pattern; the `AdminAuthorityEvaluator.HasHumanAdminScope(…, AdminScope.Compliance)` gate; the `ParticipantAuthorizationStage` per-command `Submit…` gating + `IsValid…`/`Read…` pattern; the `ComplianceAuditReadPolicy.HasPerProjectAuthority` per-project no-leak pattern + `ParticipantAuthorizationStage.ProjectOwnerClaim` + `AuditMetadata.IsSafeStableIdentifier`; the `ChatBotSpineCommandAllowlist`; the `AuditEnvelopeFactory.SourceEvidenceRefs` per-command block + `PolicyEvidenceRefs`/`SafeObjectArrayRefs`/`SafeRefArray` helpers; the `DataClassInventoryCatalog.Published` **seed-catalog shape** as the template for `ConsentRequirementMatrix.Published`. **Do not** build a second authority path, a second policy-snapshot record, a second sensitivity set, a second validation-result type, or a second token validator.
- **Fail-closed + audit-everything floor (NFR1, NFR7, NFR15a, FR68, FR75f, FR75g).** `SubmitConsentLawfulBasisRecord` is a state-writing compliance path: it routes through the one CommandGateway spine (`auth → authorize → … → pre-commit-audit → execute → post-commit-audit`); on non-compliance scope, non-human actor (service client / AI actor), invalid command, or audit-writer-down it returns a typed rejection and writes **no** durable state. No consent path skips audit. The **`ConsentGate`** fail-closed bias (AC4) is the same fail-closed posture applied to *consumption* of the metadata.
- **Metadata-only / no-leak (NFR2, NFR42, NFR45).** Every emitted token (record id, subject kind, lawful basis, status, basis source, sensitivity, project scope ref, fingerprint) is an `AuditMetadata`-safe bounded token; fingerprints are `sha256:` digests; the subject locator is an opaque safe stable identifier, never raw PII; an unauthorized-project read emits no project/subject ref. Extend the no-leak + cross-tenant serialization suites to every new type.
- **Boundary (NetArchTest-enforced).** New `.Server` internals (the authorization policy, the read-redaction wiring, the auth-stage validator/reader, the `ConsentGateEvaluator` server seam) stay `internal` to `.Server`; the shared contracts (`Consent*` records, `SubmitConsentLawfulBasisRecord`, the five closed-set token classes, `ConsentLawfulBasisSchema`, `ConsentGate`, `ConsentRequirementPolicy`, `ConsentRequirementMatrix`, `ConsentLawfulBasisAuthorityView`) live in `.Contracts` exactly like `TenantExportContracts.cs`/`DataClassInventoryContracts.cs` and carry no server/gateway/`ClaimsPrincipal` dependency. No `.UI`/`.Cli`/`.Mcp` references a `.Server.Gateway` type.
- **Inert-control-floor honesty.** This story ships the **consent/lawful-basis governed command + the `ConsentLawfulBasisRecord` model + the closed token sets + the `ConsentRequirementMatrix.Published` seed + the pure `ConsentRequirementPolicy`/`ConsentGate` decision functions + the per-project no-leak read redaction + the `ConsentLawfulBasisSchema` invariants + NFR50 audit recording + validation + tests + ADR** — the governed *decision/recording/gating* layer that makes "defensible consent governance" real. **Deferred** (state explicitly in Completion Notes): the live S-tagged consent-metadata UI surface; the live wiring of `ConsentGate` into the **actual** AI-processing (`ProposeAIAction`) and retention **execution** call sites (modeled as a documented `ConsentGateEvaluator` deferral hook); and the tenant-policy-knob *override* that lets a tenant additionally require basis beyond the regulatory-profile default (the `Published` matrix + pure evaluator ship now; the live policy-snapshot → `ConsentRequirementProfile` mapper is the seam). Never let "the gate isn't yet wired into every AI/retention call site" read as "consent is ungoverned/unaudited" — the recording **is** governed, the requirement **is** evaluated, the gate decision **is** real and tested, and every record **is** audited.

## Tasks / Subtasks

- [x] **Task 1 — Closed token sets + schema version (AC: #1, #4)**
  - [x] Create `src/Hexalith.ChatBot.Contracts/Commands/ConsentLawfulBasisContracts.cs`. Add `ConsentLawfulBasisSchemaVersions` (`V1 = "consent-lawful-basis-schema.v1"`, `All`, `IsKnown`) mirroring `TenantExportSchemaVersions`.
  - [x] Add the five closed token sets, each mirroring `TenantExportClassDispositions` (static class + `const` members + `All` `IReadOnlySet<string>` + `Contains`): `ConsentSubjectKinds` `{ external-participant, retained-content, attachment, ai-processing }`; `ConsentLawfulBases` `{ consent, contract, legal-obligation, vital-interests, public-task, legitimate-interests }`; `ConsentRecordStatuses` `{ active, withdrawn, expired, superseded }`; `ConsentRequirementDispositions` `{ required, not-required }`; `ConsentGateDecisions` `{ satisfied, blocked-missing-basis }`.
  - [x] **Token-collision note:** these tokens deliberately avoid the legacy-lifecycle literals (`pending`/`accepted`/`running`/`succeeded`/`cancelled`), so `ScaffoldArchitectureTests.NonGeneratedChatBotSourceShouldNotHardCodeLegacyLifecycleLiterals` will **not** flag `ConsentLawfulBasisContracts.cs` and **no** allowlist entry is needed. If you introduce any of those literal tokens, add the file to that test's allowlist with an explanatory comment (the `TenantExportContracts.cs`/`DeletionErasureContracts.cs` precedent) — but the intent is to not need it.

- [x] **Task 2 — Record / requirement / authority contracts + the governed command (AC: #1, #2, #3)**
  - [x] Add `public sealed record ConsentLawfulBasisRecord(string RecordId, string SubjectKind, string SubjectLocator, string ProjectScopeRef, string LawfulBasis, string RecordStatus, string BasisSource, string RedactionSensitivity, DateTimeOffset RecordedAtUtc, string RecordFingerprint)` — **metadata-only** (safe opaque `SubjectLocator`; `RedactionSensitivity` ∈ `DataClassRedactionSensitivities`).
  - [x] Add `public sealed record ConsentRequirementProfile(IReadOnlyDictionary<string, string> DispositionsBySubjectKind)` — the **bounded** requirement value the pure `ConsentRequirementPolicy`/`ConsentGate` consume (each value ∈ `ConsentRequirementDispositions`; keys ⊆ `ConsentSubjectKinds.All`). The server seam builds this from the published matrix merged with any tenant override (override wiring deferred — see Task 7).
  - [x] Add `public sealed record ConsentLawfulBasisAuthorityView(bool HasComplianceScope, IReadOnlySet<string> AuthorizedProjectRefs)` — the **bounded** authority value the pure read-redaction consumes (no `ClaimsPrincipal` in `.Contracts`), mirroring `TenantExportAuthorityView`.
  - [x] Add `public sealed record ConsentLawfulBasisSnapshotMetadata(...)` mirroring `TenantExportSnapshotMetadata` field-for-field (`SnapshotId`, `SchemaVersion`, `SupersedesSnapshotId`, `SupersededBySnapshotId`, `SourceChangeId`, `ActorRef`, `ScopeUsed = AdminScope.Compliance`, `RecordedSubjectKinds` (replaces `ExportedDataClassIds`), `SourceVersion`, `EffectiveAtUtc`, `CorrelationId`, `ReasonCode`, `PolicySnapshotId`, `OldSnapshotFingerprint`, `NewSnapshotFingerprint`).
  - [x] Add the governed command `public sealed record SubmitConsentLawfulBasisRecord(string RecordId, long SourceVersion, string SubjectKind, string SubjectLocator, string ProjectScopeRef, string LawfulBasis, string RecordStatus, string BasisSource, string RedactionSensitivity, string ReasonCode, string RequesterRef, string SchemaVersion, string CorrelationId, string PolicySnapshotId, string RecordFingerprint, DateTimeOffset EffectiveAtUtc) : IChatBotCommand` (mirror `SubmitTenantExportRequest`/`SubmitDeletionErasureRequest`; `RecordId` is the stable idempotency/run key — Story 1.5 two-altitude floor).

- [x] **Task 3 — `ConsentRequirementMatrix.Published` seed + pure `ConsentRequirementPolicy` + `ConsentGate` (AC: #1, #4)**
  - [x] Add `public static class ConsentRequirementMatrix` mirroring `DataClassInventoryCatalog` — an immutable, deterministic, token-only `Published` seed (no `UtcNow`) declaring the default **regulatory-profile** disposition per `ConsentSubjectKinds` member. Seed defaults: `external-participant ⇒ required`, `ai-processing ⇒ required`, `retained-content ⇒ required`, `attachment ⇒ required` (bias to `required`; a future profile edit may relax a kind). Expose it as a `ConsentRequirementProfile`.
  - [x] Add `public static class ConsentRequirementPolicy` with `string Evaluate(string subjectKind, ConsentRequirementProfile profile)` ⇒ a `ConsentRequirementDispositions` token — a **pure** function: look up `subjectKind` in `profile.DispositionsBySubjectKind`; an **unknown subject kind or a missing/empty profile entry biases to `required`** (fail-closed, AC4).
  - [x] Add `public static class ConsentGate` with `string Evaluate(string subjectKind, string requirementDisposition, string? activeRecordStatus)` ⇒ a `ConsentGateDecisions` token — pure: `not-required ⇒ satisfied`; `required` **and** `activeRecordStatus == ConsentRecordStatuses.Active ⇒ satisfied`; `required` with a `null` / `withdrawn` / `expired` / `superseded` status ⇒ `blocked-missing-basis`; an **unknown disposition** biases to `blocked-missing-basis`. This is the AC4 fail-closed decision function — make it a **real, testable function, not a comment.**

- [x] **Task 4 — `ConsentLawfulBasisSchema` validation + read redaction (AC: #1, #2, #3)**
  - [x] Add `public static class ConsentLawfulBasisSchema` reusing `RetentionValidationResult`: `ValidateRecord(ConsentLawfulBasisRecord?)` — `SubjectKind` ∈ `ConsentSubjectKinds`; `LawfulBasis` ∈ `ConsentLawfulBases`; `RecordStatus` ∈ `ConsentRecordStatuses`; `RedactionSensitivity` ∈ `DataClassRedactionSensitivities`; `RecordId`/`SubjectLocator`/`ProjectScopeRef`/`BasisSource` safe tokens (`IsSafeComplianceToken`; `SubjectLocator` additionally `AuditMetadata.IsSafeStableIdentifier`); `RecordFingerprint` an `IsSafeFingerprint`; `RecordedAtUtc` `IsUtc`. Add `ValidateRequirementProfile(ConsentRequirementProfile?)` — keys ⊆ `ConsentSubjectKinds.All`, values ∈ `ConsentRequirementDispositions`, and **every** `ConsentSubjectKinds` member present (a bijection — no subject kind left undeclared, mirroring the Story 9.7 inventory-completeness invariant). Reuse `ComplianceAdministrationSchema.IsSafe*`/`IsUtc` — do **not** add new token validators.
  - [x] Add `public static class ConsentLawfulBasisRedactionPolicy` (pure, `.Contracts`) with `ConsentLawfulBasisRecord Redact(ConsentLawfulBasisRecord record, ConsentLawfulBasisAuthorityView authority)` — when `!authority.HasComplianceScope` or the record's `ProjectScopeRef ∉ authority.AuthorizedProjectRefs`, return a redacted record with **empty** `SubjectLocator` and `ProjectScopeRef` (and a `metadata-only` sensitivity), so an unauthorized read is indistinguishable from safe-not-found (AC2, NFR2). The server `ConsentLawfulBasisAuthorizationPolicy` (Task 5) supplies the bounded view.

- [x] **Task 5 — Allowlist + fail-closed compliance-admin gating + per-project authority (AC: #2, #3)**
  - [x] Add `nameof(SubmitConsentLawfulBasisRecord)` to `ChatBotSpineCommandAllowlist` (after `nameof(SubmitDeletionErasureRequest)`, line 61).
  - [x] In `ParticipantAuthorizationStage.AuthorizeAsync`, add the gating block mirroring the `SubmitDeletionErasureRequest` block (lines 472–477): `!HasHumanAdminScope(actor.Principal, AdminScope.Compliance) || !IsValidConsentLawfulBasisRecord(command) ⇒ Denied(AuthorizationDenied)`. Add `private static bool IsValidConsentLawfulBasisRecord(object?)` mirroring `IsValidDeletionErasureRequest` (line 1968): `SourceVersion >= 0`; `IsSafeComplianceToken` on `RecordId`/`SubjectLocator`/`ProjectScopeRef`/`BasisSource`/`ReasonCode`/`RequesterRef`/`CorrelationId`/`PolicySnapshotId`; `ConsentSubjectKinds.Contains(SubjectKind)`; `ConsentLawfulBases.Contains(LawfulBasis)`; `ConsentRecordStatuses.Contains(RecordStatus)`; `DataClassRedactionSensitivities.Contains(RedactionSensitivity)`; `ConsentLawfulBasisSchemaVersions.IsKnown(SchemaVersion)`; `IsSafeFingerprint(RecordFingerprint)`; `IsUtc(EffectiveAtUtc)`. Add the `ReadSubmitConsentLawfulBasisRecord(object?)` typed/`JsonElement` reader mirroring `ReadSubmitDeletionErasureRequest` (line 2232).
  - [x] Add `internal static class ConsentLawfulBasisAuthorizationPolicy` in `src/Hexalith.ChatBot.Server/Audit/` (next to `TenantExportAuthorizationPolicy`/`DeletionErasureAuthorizationPolicy`). Add `bool CanRecordConsentLawfulBasis(ClaimsPrincipal)` ⇒ `AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Compliance)` and `ConsentLawfulBasisAuthorityView AuthorityFor(ClaimsPrincipal)` projecting `principal.FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)` filtered by `AuditMetadata.IsSafeStableIdentifier` into the bounded view — mirroring `TenantExportAuthorizationPolicy.AuthorityFor` line-for-line. This is the **only** place a `ClaimsPrincipal` touches the consent decision; the redaction policy stays pure.

- [x] **Task 6 — Audit source-evidence refs for the consent record (AC: #1, #3)**
  - [x] Add `nameof(SubmitConsentLawfulBasisRecord)` to the compliance-command **admission list** in `AuditEnvelopeFactory` (after `nameof(SubmitDeletionErasureRequest)`, line 1588).
  - [x] In `AuditEnvelopeFactory.SourceEvidenceRefs`, add the `SubmitConsentLawfulBasisRecord` block mirroring the `SubmitDeletionErasureRequest` block (lines 3004+): yield `admin-operation:submit-consent-lawful-basis-record`, `admin-scope:compliance`; `PolicyEvidenceRefs(element, "recordId", "consent-record")`, `PolicyEvidenceRefs(element, "subjectKind", "consent-subject-kind")`, `PolicyEvidenceRefs(element, "lawfulBasis", "consent-lawful-basis")`, `PolicyEvidenceRefs(element, "recordStatus", "consent-record-status")`, `PolicyEvidenceRefs(element, "basisSource", "consent-basis-source")`, `PolicyEvidenceRefs(element, "projectScopeRef", "consent-scope-project")`, `PolicyEvidenceRefs(element, "recordFingerprint", "consent-fingerprint")`. **Do NOT** emit the raw `subjectLocator` as a ref — the opaque locator stays out of the evidence stream (only the `consent-record:` id + `consent-scope-project:` ref localize the record). Confirm the NFR50 actor/actor-type/timestamp/policy-snapshot/decision/reason are carried by the envelope `Create`, not duplicated.

- [x] **Task 7 — `ConsentGateEvaluator` server seam (AC: #4) — decision real, live wiring deferred**
  - [x] Add `internal static class ConsentGateEvaluator` in `src/Hexalith.ChatBot.Server/Audit/` (next to `DeletionErasureRunner`). Add `string EvaluateForGovernedAction(string subjectKind, ConsentRequirementProfile profile, string? activeRecordStatus)` that composes `ConsentRequirementPolicy.Evaluate` then `ConsentGate.Evaluate`, returning a `ConsentGateDecisions` token — the **server-callable** fail-closed decision the AI-processing/retention paths will consult. **Do NOT** wire it into the live `ProposeAIAction` / retention execution call sites in this story — model those as documented deferral hooks (a commented seam + an XML-doc note), exactly as Story 9.9's `DeletionErasureRunner.DestroyNonAuditStoreSubjectAsync` modeled the non-audit-store destruction runtime. The pure `ConsentGate` (Task 3) + its contract test (Task 8) make AC4 real at the decision layer; the live fan-out is the deferred runtime.
  - [x] Also add the deferred `ConsentRequirementProfileMapper` seam (server-internal) that will build a tenant-overridden `ConsentRequirementProfile` from a referenced tenant-policy snapshot — for now it returns `ConsentRequirementMatrix.Published` (the regulatory-profile default) with a documented note that the live policy-snapshot merge is M2-deferred.

- [x] **Task 8 — Tests: contracts/gate, fail-closed gateway, audit, read-redaction, no-leak, boundary (AC: #1–#4)**
  - [x] **Contracts** (`tests/Hexalith.ChatBot.Contracts.Tests/ConsentLawfulBasisContractTests.cs`, mirror `TenantExportContractTests`): round-trip + closed-set membership for the five new token sets; `ConsentRequirementMatrix.Published` is a complete bijection over `ConsentSubjectKinds.All`; `ConsentRequirementPolicy.Evaluate` ⇒ `required` for a known-required kind, `required` for an **unknown** kind (fail-closed), and the seeded value otherwise; `ConsentGate.Evaluate` ⇒ `satisfied` when `not-required`, `satisfied` when `required`+`active`, `blocked-missing-basis` when `required`+(`null`/`withdrawn`/`expired`/`superseded`), and `blocked-missing-basis` for an unknown disposition (**AC4 fail-closed**); `ConsentLawfulBasisSchema.ValidateRecord`/`ValidateRequirementProfile` accept valid inputs and reject (a) an invalid subject kind, (b) an invalid lawful basis, (c) an invalid record status, (d) a non-`DataClassRedactionSensitivities` sensitivity, (e) a bad fingerprint, (f) a requirement profile missing a subject kind (`consent_requirement_profile_incomplete`); `ConsentLawfulBasisRedactionPolicy.Redact` drops the subject locator + project ref for an unauthorized project and preserves them for an authorized one (**AC2**).
  - [x] **Fail-closed gateway** (`tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ConsentLawfulBasisAuthorizationTests.cs`, mirror `DeletionErasureAuthorizationTests`): a non-compliance-admin, a service-client, and an AI actor submitting `SubmitConsentLawfulBasisRecord` are each `Denied(AuthorizationDenied)` with **no durable write**; a human compliance-admin with a valid command passes authorization. Reuse the compliance-command gateway harness/fixtures.
  - [x] **Gateway audit + no-leak** (`tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`, extend): add a `ConsentLawfulBasisRecordCommand()` factory; assert (a) audit-writer-down at pre-commit ⇒ 503 `AuditUnavailable`, no dispatch, audited rejection carrying `admin-scope:compliance`; (b) the committed envelope carries `consent-record:`, `consent-subject-kind:`, `consent-lawful-basis:`, `consent-record-status:`, `consent-basis-source:`, `consent-scope-project:`, and `consent-fingerprint:` refs (**AC3**); (c) **no-leak** — the raw `subjectLocator` never appears in any evidence ref.
  - [x] **Cross-tenant no-leak** (`tests/Hexalith.ChatBot.Conformance.Tests/ConsentLawfulBasisLeakageScanTests.cs`, mirror `DeletionErasureLeakageScanTests`): reuse `CrossTenantLeakageScanner`/`CrossTenantLeakageCorpus` to assert no foreign-tenant token survives serialization of `SubmitConsentLawfulBasisRecord`, `ConsentLawfulBasisRecord`, `ConsentLawfulBasisSnapshotMetadata`, and the redacted record. **Use a neutral, non-sentinel scope token** (e.g. `tenant-consent-owner`) — `tenant-alpha` is the Story 1.12 corpus `boundTenant` sentinel (the 9.8/9.9 leakage-scan gotcha).
  - [x] **Boundary fitness** (`tests/Hexalith.ChatBot.Architecture.Tests/Fitness/`): confirm `DependencyDirectionFitnessTests`/`AdapterBoundaryFitnessTests` still pass — the new `.Contracts` types (incl. the pure `ConsentGate`/`ConsentRequirementPolicy`/`ConsentLawfulBasisRedactionPolicy`) carry no server dependency and `ConsentLawfulBasisAuthorizationPolicy`/`ConsentGateEvaluator`/the validator/reader are `internal`. Confirm `ScaffoldArchitectureTests.NonGeneratedChatBotSourceShouldNotHardCodeLegacyLifecycleLiterals` still passes **without** a new allowlist entry (the token sets avoid the legacy literals by design — see Task 1).

- [x] **Task 9 — ADR + docs (AC: #1–#4)**
  - [x] Author `docs/adrs/consent-and-lawful-basis-metadata.md`: the `SubmitConsentLawfulBasisRecord` governed command as a structural twin of `SubmitTenantExportRequest`/`SubmitDeletionErasureRequest`; the five closed token sets; the `ConsentRequirementMatrix.Published` regulatory-profile seed (define-once — modeled on `DataClassInventoryCatalog.Published`); the pure `ConsentRequirementPolicy`/`ConsentGate` fail-closed decision functions (AC4, NFR7/FR68); the per-project no-leak read redaction reuse (AC2, NFR2); the NFR50 audit evidence block (AC3); and the **deferrals** (live consent UI surface, live `ConsentGate` wiring into AI-processing/retention call sites via `ConsentGateEvaluator`, and the tenant-policy-override → `ConsentRequirementProfile` mapper). Cross-reference Story 9.9 (`9-9-deletion-and-erasure-workflow.md`), Story 9.8 (`9-8-tenant-export-workflow.md`), Story 9.7 (`9-7-data-class-inventory-and-retention-policy.md`), Story 9.3 (per-project no-leak), and Story 7.4 (compliance scope).

## Dev Notes

### What this story actually changes (and what already exists)

Story 9.10 makes the ChatBot's **consent and lawful-basis metadata governed, requirement-aware, fail-closed, and audit-defensible**: an authorized compliance-admin submits `SubmitConsentLawfulBasisRecord`, the gateway records a `ConsentLawfulBasisRecord` (subject kind, opaque locator, GDPR lawful basis, status, source, sensitivity) through the **existing** audit-commit spine, a pure `ConsentRequirementPolicy` decides whether a subject kind requires a basis (from the `ConsentRequirementMatrix.Published` regulatory-profile seed), a pure `ConsentGate` decides `satisfied` vs `blocked-missing-basis` for any governed action over that subject, and a per-project no-leak read redaction protects reads. It is the **direct sibling of Stories 9.7/9.8/9.9** — the fourth member of the additive compliance-governed-command family on the mature CommandGateway/audit/admin-scope seams. Like them, it is **almost entirely additive contracts + pure decision functions + a governed command + gateway-gating + audit-evidence built on mature seams** — reuse, do not reinvent.

**The single most important framing for the dev agent:** there are **three** structural templates to mirror, and a reviewer will diff against them line-for-line:
1. **`SubmitDeletionErasureRequest` (Story 9.9) / `SubmitTenantExportRequest` (Story 9.8)** — the governed compliance command shape, the allowlist line, the `ParticipantAuthorizationStage` gating block + `IsValid…`/`Read…`, the `AuditEnvelopeFactory` admission entry + evidence block, the closed-set token classes, the snapshot metadata, the `…AuthorizationPolicy` per-project authority. `SubmitConsentLawfulBasisRecord` is the structural twin (deletion → consent). **Read `9-9-deletion-and-erasure-workflow.md`, `TenantExportContracts.cs`, and the `ParticipantAuthorizationStage` deletion block first.**
2. **`DataClassInventoryCatalog.Published` (Story 9.7)** — the immutable, deterministic, token-only **seed-catalog** shape (no `UtcNow`, fixed values). `ConsentRequirementMatrix.Published` mirrors it for the per-subject-kind regulatory-profile requirement.
3. **`ComplianceAuditReadPolicy.HasPerProjectAuthority` (Story 9.3) / `TenantExportAuthorizationPolicy` (Story 9.8)** — the per-project, no-leak authority evaluation. `ConsentLawfulBasisAuthorizationPolicy` mirrors them; the redaction policy is the read-side analog.

The genuinely **new** value is (a) the `ConsentRequirementMatrix` + pure `ConsentRequirementPolicy.Evaluate` (subject kind → required/not-required, **unknown ⇒ required**), and (b) the pure `ConsentGate.Evaluate` (requirement + active-status → `satisfied`/`blocked-missing-basis`, the AC4 fail-closed gate). Make both **real, testable decision functions, not comments.**

**Already exists — consume by reference:**

- **The governed compliance command + gating + audit + authority template is solved (Stories 9.7/9.8/9.9).** `SubmitTenantExportRequest` / `SubmitDeletionErasureRequest` + the closed token sets + the snapshot metadata + the `…AuthorizationPolicy` + the allowlist line (60/61) + the gating block (`ParticipantAuthorizationStage` 463–477) + validator (`IsValidDeletionErasureRequest` 1968) + reader (`ReadSubmitDeletionErasureRequest` 2232) + audit admission (`AuditEnvelopeFactory` 1586–1588) + evidence block (3004+). Mirror **all** of it. [Source: src/Hexalith.ChatBot.Contracts/Commands/TenantExportContracts.cs; _bmad-output/implementation-artifacts/9-9-deletion-and-erasure-workflow.md]
- **The redaction-sensitivity set is solved (Story 9.7).** `DataClassRedactionSensitivities` `{ restricted, sensitive, internal, metadata-only }`. `RedactionSensitivity` is a member — never fork a second sensitivity set. [Source: src/Hexalith.ChatBot.Contracts/Commands/DataClassInventoryContracts.cs:24-36]
- **The seed-catalog shape is solved (Story 9.7).** `DataClassInventoryCatalog.Published` — immutable, deterministic, token-only, fixed seed date, no `UtcNow`. `ConsentRequirementMatrix.Published` mirrors this construction. [Source: src/Hexalith.ChatBot.Contracts/Commands/DataClassInventoryContracts.cs:265-354]
- **The validation-result + token helpers are solved (Story 7.4).** `RetentionValidationResult` (the one result type); `ComplianceAdministrationSchema.IsSafeComplianceToken`/`IsSafeFingerprint`/`IsUtc`. Reuse — do not add a second result type or token validator. [Source: src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs:133-141, 185-311]
- **The admin-scope model is solved (Story 7.1).** `AdminScope.Compliance`; `AdminAuthorityEvaluator.HasHumanAdminScope` (human-only, claim-based, fail-closed — service clients / AI actors denied). [Source: src/Hexalith.ChatBot.Contracts/Enums/AdminScope.cs:22-23; src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs]
- **The per-project no-leak authority is solved (Story 9.3 / 9.8).** `ComplianceAuditReadPolicy.HasPerProjectAuthority` + `TenantExportAuthorizationPolicy.AuthorityFor`; `ParticipantAuthorizationStage.ProjectOwnerClaim` is the grant claim; `AuditMetadata.IsSafeStableIdentifier` filters. Mirror the grant-extraction; the consent read-redaction is the analog of the export `unauthorized` exclusion — fail-closed-to-redacted. [Source: src/Hexalith.ChatBot.Server/Audit/TenantExportAuthorizationPolicy.cs:18-34; src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs:24-45]
- **The audit evidence-ref machinery is solved.** `AuditEnvelopeFactory.SourceEvidenceRefs` per-command blocks (the `SubmitDeletionErasureRequest` block 3004+ is the closest template); the compliance admission list (1586-1588); helper `PolicyEvidenceRefs`. NFR50 actor/timestamp/policy-snapshot/decision/reason carried by the envelope `Create`. [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs:1586-1588, 3004-3050; AuditMetadata.cs]
- **The contracts + gateway-auth + no-leak test styles are solved.** `TenantExportContractTests`; `DeletionErasureAuthorizationTests`; `DeletionErasureLeakageScanTests` + `CrossTenantLeakageScanner`/`CrossTenantLeakageCorpus`; `CommandGatewayTests` (audit-down + evidence refs). Mirror each. [Source: tests/Hexalith.ChatBot.Contracts.Tests/TenantExportContractTests.cs; tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/DeletionErasureAuthorizationTests.cs; tests/Hexalith.ChatBot.Conformance.Tests/DeletionErasureLeakageScanTests.cs]

**What you are adding (the real deliverables):** (1) the five closed token sets + `ConsentLawfulBasisSchemaVersions`; (2) `ConsentLawfulBasisRecord` / `ConsentRequirementProfile` / `ConsentLawfulBasisAuthorityView` / `ConsentLawfulBasisSnapshotMetadata` contracts + `SubmitConsentLawfulBasisRecord` command; (3) `ConsentRequirementMatrix.Published` seed + the pure `ConsentRequirementPolicy.Evaluate` + `ConsentGate.Evaluate` (the AC4 fail-closed decisions) + `ConsentLawfulBasisSchema` validation + `ConsentLawfulBasisRedactionPolicy` (the AC2 read redaction); (4) allowlist entry + `ParticipantAuthorizationStage` fail-closed compliance gating + validator + reader; (5) the `ConsentLawfulBasisAuthorizationPolicy` per-project no-leak authority view; (6) the `AuditEnvelopeFactory` admission entry + evidence block; (7) the `ConsentGateEvaluator` server seam (decision real; live AI/retention wiring deferred) + the deferred `ConsentRequirementProfileMapper`; (8) tests + ADR.

### Architecture constraints (must follow)

- **FR20 / NFR55 (consent & lawful-basis metadata).** Where tenant policy/regulatory profile requires, the system records consent or lawful-basis metadata for external participants, retained content, attachments, and AI processing. The governed-command + record model + requirement matrix + audit layer is the M2 foundation; the live UI surface and the live gate-into-execution wiring are the deferred surfaces. [Source: epics.md:58 (FR20), 262 (NFR55), 451, 2540-2562]
- **NFR7 / FR68 (fail closed).** Security-sensitive operations fail closed when policy evaluation or required command validation is unavailable; the `ConsentGate` fails closed (`blocked-missing-basis`) when a required basis is absent, unknown, or non-`active`. [Source: epics.md:141 (FR68), 192 (NFR7), 2560-2562]
- **NFR1 (fail-closed + audited).** Non-authorized consent recording fails closed at the gateway, writes nothing, and is audited. [Source: epics.md (NFR1); Story 9.7/9.9 AC2 precedent]
- **NFR2 / FR75f (no-leak / per-project redaction).** Consent reads are redacted per project; unauthorized detail is excluded and indistinguishable from not-found; only the human compliance scope can query. [Source: epics.md:132 (FR75f), 187 (NFR2), 2552-2554; src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs:24-45]
- **NFR50 (audit required-field presence).** Every consent recording/change envelope carries tenant, actor, actor type, command, resource, decision, reason, correlation, timestamp, policy-snapshot ref, and the source-evidence refs (subject kind, lawful basis, status, source, project scope, fingerprint). [Source: epics.md:256, 712; 2556-2558]
- **CommandGateway spine / fail-closed (NFR15a).** The consent command is a state-writing path through the single audit-commit seam; no partial state on rejection. [Source: architecture.md:542-551]
- **Two-altitude idempotency (Story 1.5).** The stable `RecordId` + `SourceVersion` version-guard makes re-submission idempotent. [Source: epics.md (Story 1.5)]
- **Boundary (NetArchTest-enforced).** Contracts (incl. the pure gate/requirement/redaction functions) have no server/`ClaimsPrincipal` dependency; server internals `internal`; adapters never replicate stages. [Source: architecture.md:577-585; tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs]

### Define-once map (the one canonical seam per concern)

| Concern | The ONE canonical thing | Do NOT add |
|---|---|---|
| Redaction sensitivity | `DataClassRedactionSensitivities` | a second sensitivity set |
| Validation result | `RetentionValidationResult` | a second result type |
| Token validators | `ComplianceAdministrationSchema.IsSafe*`/`IsUtc` + `AuditMetadata.IsSafeStableIdentifier` | new token validators |
| Admin authority gate | `AdminAuthorityEvaluator.HasHumanAdminScope` | a second auth path |
| Per-project authority | `ComplianceAuditReadPolicy.HasPerProjectAuthority` / `TenantExportAuthorizationPolicy` pattern | a second project-grant reader |
| Audit evidence | `AuditEnvelopeFactory.SourceEvidenceRefs` block + `PolicyEvidenceRefs` | a second evidence path |
| Seed-catalog shape | `DataClassInventoryCatalog.Published` | a divergent seed construction |
| Governed-command/snapshot/authority template | Story 9.8/9.9 `TenantExport*`/`DeletionErasure*` | a divergent shape |
| Allowlist | `ChatBotSpineCommandAllowlist` | a second allowlist |

### Previous-work intelligence — apply directly

- **Mirror `SubmitDeletionErasureRequest` structurally; reviewers diff line-for-line.** Command shape, allowlist line (61), auth-stage gating block (472-477), `IsValid…`/`Read…` (1968/2232), audit admission entry (1588) + evidence block (3004+), the closed-set token classes, the snapshot metadata, the authorization policy — all twins of the 9.9/9.8 originals. Stories 9.7/9.8/9.9 each went **Approve, 0 critical** with only bookkeeping auto-fixes — match that bar.
- **Fail-closed bias is the 9.10-specific risk.** Consent governance protects data subjects: an **unknown subject kind**, a **missing requirement entry**, or a **non-`active` basis** must bias to `required` / `blocked-missing-basis`, never silently `satisfied`. The Story 9.9 review explicitly validated "bias every ambiguous case toward [the safe state]" — the same invariant applies here.
- **Subject locator is opaque — never leak PII.** The `SubjectLocator` is an `AuditMetadata.IsSafeStableIdentifier` opaque ref; it is **excluded** from the audit evidence stream (only `consent-record:` id + `consent-scope-project:` ref localize it) and dropped entirely on an unauthorized read (AC2). Do not emit raw participant email / file name / message body anywhere.
- **Token sets deliberately avoid legacy-lifecycle literals.** `active`/`withdrawn`/`expired`/`superseded`, `satisfied`/`blocked-missing-basis`, `required`/`not-required` avoid `pending`/`accepted`/`running`/`succeeded`/`cancelled`, so — unlike 9.8/9.9 — **no** `ScaffoldArchitectureTests` allowlist entry is needed. Keep it that way; if you must add a colliding token, follow the `TenantExportContracts.cs`/`DeletionErasureContracts.cs` allowlist precedent.
- **Leakage-scan sentinel gotcha (the 9.8/9.9 lesson).** `tenant-alpha` is the Story 1.12 cross-tenant corpus `boundTenant` sentinel — use a neutral non-sentinel scope token (e.g. `tenant-consent-owner`) in the leakage test.
- **Bookkeeping drift is the #1 recurring review auto-fix across Epics 7–9** (stale test counts, File List omissions). Keep the **File List exhaustive** (every new + modified source/test/ADR, including `ChatBotSpineCommandAllowlist.cs`, `ParticipantAuthorizationStage.cs`, `AuditEnvelopeFactory.cs`, and any `tests/.../test-summary-story-9.10.md` artifact) and every cited test count accurate against the live run.
- **Inert-control-floor honesty.** Ship the governed command + record model + requirement matrix + pure `ConsentRequirementPolicy`/`ConsentGate` + read redaction + NFR50 audit + tests + ADR. **Defer** the live UI surface, the live `ConsentGate` wiring into `ProposeAIAction`/retention execution (the `ConsentGateEvaluator` hook), and the tenant-policy-override → `ConsentRequirementProfile` mapper. **State deferrals explicitly in Completion Notes**; never let a deferral read as "consent is ungoverned/unaudited." The pure gate decision is real and tested (AC4 holds at the decision layer).
- **Backward-compatibility is non-negotiable.** Adding a sibling compliance command + a seed matrix must keep all existing 7.4 / 9.3 / 9.7 / 9.8 / 9.9 compliance, gateway, audit, and architecture tests green.

### Project Structure Notes

- **Contracts (all shared types incl. the pure gate/requirement/redaction — no server / `ClaimsPrincipal` dependency):**
  - `src/Hexalith.ChatBot.Contracts/Commands/ConsentLawfulBasisContracts.cs` (**new:** the five token sets, `ConsentLawfulBasisSchemaVersions`, `ConsentLawfulBasisRecord`, `ConsentRequirementProfile`, `ConsentLawfulBasisAuthorityView`, `ConsentLawfulBasisSnapshotMetadata`, `SubmitConsentLawfulBasisRecord`, `ConsentRequirementMatrix`, `ConsentRequirementPolicy`, `ConsentGate`, `ConsentLawfulBasisSchema`, `ConsentLawfulBasisRedactionPolicy`).
- **Server (gating + authority + audit + gate-orchestration only — no new aggregate; compliance commands are spine-governed):**
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` (**modified:** gating block + `IsValidConsentLawfulBasisRecord` + `ReadSubmitConsentLawfulBasisRecord`).
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` (**modified:** `+ SubmitConsentLawfulBasisRecord`).
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (**modified:** admission entry ~1588 + `SubmitConsentLawfulBasisRecord` evidence block ~after 3050).
  - `src/Hexalith.ChatBot.Server/Audit/ConsentLawfulBasisAuthorizationPolicy.cs` (**new:** `CanRecordConsentLawfulBasis` + `AuthorityFor` building the bounded `ConsentLawfulBasisAuthorityView`).
  - `src/Hexalith.ChatBot.Server/Audit/ConsentGateEvaluator.cs` (**new:** the server-callable fail-closed gate seam composing `ConsentRequirementPolicy`+`ConsentGate`; `ConsentRequirementProfileMapper` deferral hook for the tenant-policy override).
- **Tests:** `tests/Hexalith.ChatBot.Contracts.Tests/ConsentLawfulBasisContractTests.cs` (**new**), `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ConsentLawfulBasisAuthorizationTests.cs` (**new**), `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` (**modified:** consent factory + audit-down + evidence + no-leak), `tests/Hexalith.ChatBot.Conformance.Tests/ConsentLawfulBasisLeakageScanTests.cs` (**new**), `tests/Hexalith.ChatBot.Architecture.Tests/` (confirm green — **no** new `ScaffoldArchitectureTests` allowlist entry expected).
- **Docs:** `docs/adrs/consent-and-lawful-basis-metadata.md` (**new**).
- No new top-level project; no conflict with the unified structure — contracts in `.Contracts/Commands`, gating/authority/audit/gate-orchestration in `.Server`, exactly like Stories 9.7/9.8/9.9. The live consent surface, when built, is an additive S-tagged UI + a Worker that consults `ConsentGateEvaluator` at the AI-processing/retention call sites through its deferral hooks.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.10 (lines 2540-2562); Story 9.9 (2514-2538); Story 9.8 (2492-2512); Story 9.7 (2470-2490); Story 9.3 (2400-2416); Epic 9 (2358-2360); FR20 (58, 451); FR68 (141, 504); FR75f (132); NFR1; NFR2 (187); NFR7 (192); NFR50 (256, 712); NFR55 (262)]
- [Source: _bmad-output/planning-artifacts/architecture.md#CommandGateway flow (542-551); audit envelope min fields (712); pattern enforcement (577-585); Epic 9 (581-582)]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/TenantExportContracts.cs (the structural template — token sets, snapshot metadata, planner/schema, command); src/Hexalith.ChatBot.Contracts/Commands/DataClassInventoryContracts.cs:24-36 (DataClassRedactionSensitivities), 265-354 (DataClassInventoryCatalog.Published seed shape)]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs:133-141 (RetentionValidationResult), 185-311 (ComplianceAdministrationSchema validators)]
- [Source: src/Hexalith.ChatBot.Server/Audit/TenantExportAuthorizationPolicy.cs:18-34 (per-project no-leak authority twin); src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs:24-45 (HasPerProjectAuthority); ProjectOwnerClaim; AuditMetadata.IsSafeStableIdentifier]
- [Source: src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs; src/Hexalith.ChatBot.Contracts/Enums/AdminScope.cs:22-23]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs:472-477 (deletion gating block), 1968 (IsValidDeletionErasureRequest), 2232 (ReadSubmitDeletionErasureRequest)]
- [Source: src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs:58-61]
- [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs:1586-1588 (compliance admission), 3004-3050 (deletion evidence block); PolicyEvidenceRefs/SafeRefArray; AuditMetadata.cs]
- [Source: tests/Hexalith.ChatBot.Contracts.Tests/TenantExportContractTests.cs; tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/DeletionErasureAuthorizationTests.cs; tests/Hexalith.ChatBot.Conformance.Tests/DeletionErasureLeakageScanTests.cs; tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs:495-518 (legacy-lifecycle-literal allowlist — NOT expected to need a new entry)]
- [Source: _bmad-output/implementation-artifacts/9-9-deletion-and-erasure-workflow.md (the closest structural template + the leakage-sentinel and legacy-literal Debug Log lessons); 9-8-tenant-export-workflow.md; 9-7-data-class-inventory-and-retention-policy.md]

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context)

### Debug Log References

- `dotnet build src/Hexalith.ChatBot.Contracts` → succeeded, 0 warnings.
- `dotnet build src/Hexalith.ChatBot.Server` → succeeded, 0 warnings.
- Token sets deliberately avoid the legacy-lifecycle literals (`pending`/`accepted`/`running`/`succeeded`/`cancelled`),
  so `ScaffoldArchitectureTests.NonGeneratedChatBotSourceShouldNotHardCodeLegacyLifecycleLiterals` passes with **no** new
  allowlist entry (confirmed: Architecture.Tests 39/39 green).
- Leakage scan uses the neutral non-sentinel scope token `tenant-consent-owner` (not the Story 1.12 corpus
  `tenant-alpha` boundary) — `ConsentLawfulBasisLeakageScanTests` green.

### Completion Notes List

Implemented all 9 tasks. Story 9.10 is the fourth member of the additive compliance-governed-command family
(9.7 inventory → 9.8 export → 9.9 deletion → 9.10 consent), built as a structural twin of `SubmitDeletionErasureRequest`.

**Shipped (the governed decision/recording/gating layer — AC1–AC4 real and tested):**
- `SubmitConsentLawfulBasisRecord` governed command + `ConsentLawfulBasisRecord` metadata-only model (opaque
  `AuditMetadata.IsSafeStableIdentifier` subject locator, `DataClassRedactionSensitivities` reuse).
- Five closed token sets (`ConsentSubjectKinds`, `ConsentLawfulBases`, `ConsentRecordStatuses`,
  `ConsentRequirementDispositions`, `ConsentGateDecisions`) + `ConsentLawfulBasisSchemaVersions`.
- `ConsentRequirementMatrix.Published` deterministic regulatory-profile seed (every kind biased to `required`).
- Pure `ConsentRequirementPolicy.Evaluate` (unknown kind / missing entry ⇒ `required`) + `ConsentGate.Evaluate`
  (`required`+`active` ⇒ `satisfied`; everything else ⇒ `blocked-missing-basis`; unknown disposition ⇒ blocked) — the
  AC4 fail-closed decision functions, real and testable.
- `ConsentLawfulBasisSchema` validation (reuses `RetentionValidationResult` + `ComplianceAdministrationSchema`; profile
  bijection over `ConsentSubjectKinds.All` ⇒ `consent_requirement_profile_incomplete`).
- `ConsentLawfulBasisRedactionPolicy.Redact` per-project no-leak read redaction (drops subject locator + project ref
  for unauthorized reads → indistinguishable from safe-not-found, AC2/NFR2).
- Allowlist entry + `ParticipantAuthorizationStage` fail-closed compliance gating + `IsValidConsentLawfulBasisRecord` +
  `ReadSubmitConsentLawfulBasisRecord`; `ConsentLawfulBasisAuthorizationPolicy` per-project authority view (mirrors
  `DeletionErasureAuthorizationPolicy` line-for-line — the only `ClaimsPrincipal` touchpoint).
- `AuditEnvelopeFactory` admission entry + evidence block (`consent-record:`, `consent-subject-kind:`,
  `consent-lawful-basis:`, `consent-record-status:`, `consent-basis-source:`, `consent-scope-project:`,
  `consent-fingerprint:`); the opaque `subjectLocator` is **never** emitted as a ref (AC3/NFR2).
- `ConsentGateEvaluator` server-callable fail-closed gate seam.

**Deferred (stated explicitly per inert-control-floor honesty — never reads as "consent is ungoverned/unaudited"):**
- the live S-tagged consent-metadata UI surface;
- the live wiring of `ConsentGate` into the actual `ProposeAIAction` / retention **execution** call sites — modeled as
  the documented `ConsentGateEvaluator.EvaluateForGovernedAction` hook (twin of Story 9.9's
  `DeletionErasureRunner.DestroyNonAuditStoreSubjectAsync`);
- the tenant-policy-knob override → `ConsentRequirementProfile` mapper — modeled as the documented
  `ConsentRequirementProfileMapper.ProfileFor` seam (today returns `ConsentRequirementMatrix.Published` unchanged).

**Test results (all green — counts reflect the QA `qa-generate-e2e-tests` pass; see `tests/test-summary-story-9.10.md`):**
- `Hexalith.ChatBot.Contracts.Tests` — 454 passed (35 new in `ConsentLawfulBasisContractTests`).
- `Hexalith.ChatBot.Server.Tests` — 1376 passed (15 new: `ConsentLawfulBasisAuthorizationTests`, `CommandGatewayTests` ×2, plus the QA additions `ConsentLawfulBasisAuthorizationPolicyTests` ×3 and `ConsentGateEvaluatorTests` ×8).
- `Hexalith.ChatBot.Conformance.Tests` — 81 passed (1 new: `ConsentLawfulBasisLeakageScanTests`).
- `Hexalith.ChatBot.Architecture.Tests` — 39 passed (boundary fitness + scaffold legacy-literal check; no new allowlist entry).
- New baseline across the four suites: **1950** — all green (re-verified during review on 2026-06-03).

### File List

**New (source):**
- `src/Hexalith.ChatBot.Contracts/Commands/ConsentLawfulBasisContracts.cs`
- `src/Hexalith.ChatBot.Server/Audit/ConsentLawfulBasisAuthorizationPolicy.cs`
- `src/Hexalith.ChatBot.Server/Audit/ConsentGateEvaluator.cs`

**Modified (source):**
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`

**New (tests):**
- `tests/Hexalith.ChatBot.Contracts.Tests/ConsentLawfulBasisContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ConsentLawfulBasisAuthorizationTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Audit/ConsentLawfulBasisAuthorizationPolicyTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Audit/ConsentGateEvaluatorTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/ConsentLawfulBasisLeakageScanTests.cs`

**Modified (tests):**
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`

**New (docs):**
- `docs/adrs/consent-and-lawful-basis-metadata.md`

**New (test artifacts):**
- `_bmad-output/implementation-artifacts/tests/test-summary-story-9.10.md`

**Modified (tracking):**
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/9-10-consent-and-lawful-basis-metadata.md`

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-03 · **Outcome:** Approve (0 critical, 0 high)

Adversarial review diffed every claimed task and AC against the actual implementation and git reality, and re-ran all four suites.

- **AC1–AC4 — all IMPLEMENTED and verified.** Five closed token sets + `ConsentLawfulBasisRecord`/`SubmitConsentLawfulBasisRecord`; `ConsentRequirementMatrix.Published` complete bijection; pure `ConsentRequirementPolicy.Evaluate` (unknown/missing ⇒ `required`) and `ConsentGate.Evaluate` (only `required`+`active` ⇒ `satisfied`, else `blocked-missing-basis`) — both real, fail-closed, tested. Per-project no-leak `ConsentLawfulBasisRedactionPolicy` + `ConsentLawfulBasisAuthorizationPolicy` (sole `ClaimsPrincipal` touchpoint). NFR50 evidence block emits `consent-record/-subject-kind/-lawful-basis/-record-status/-basis-source/-scope-project/-fingerprint`; the opaque `SubjectLocator` is provably never emitted (gateway test asserts absence in serialized envelopes). Allowlist + auth-stage gating + `IsValid…`/`Read…` mirror the 9.9 deletion twin line-for-line.
- **Re-verified test reality:** Contracts 454 · Server 1376 · Conformance 81 · Architecture 39 (baseline 1950) — all green. Boundary fitness + `ScaffoldArchitectureTests` legacy-literal check pass with no new allowlist entry, as designed.

**Findings (all MEDIUM/LOW bookkeeping — auto-fixed during review; no code defects):**
- [MEDIUM, fixed] File List omitted two QA-added test files (`ConsentLawfulBasisAuthorizationPolicyTests.cs`, `ConsentGateEvaluatorTests.cs`) and the `test-summary-story-9.10.md` artifact — now listed.
- [MEDIUM, fixed] Completion Notes test counts were stale (claimed 451/1365 pre-QA; actual 454/1376) — now corrected.
- [LOW, accepted] `ConsentGate.Evaluate(requirementDisposition, activeRecordStatus)` drops the `subjectKind` param Task 3's signature listed. The gate decision needs only the disposition (which already encodes the subject kind via `ConsentRequirementPolicy`), so the parameter would be unused; the simplification is correct and fully covered by tests. No change made — re-adding a dead parameter would reduce quality.

## Change Log

| Date       | Version | Description                                                                                   | Author |
|------------|---------|-----------------------------------------------------------------------------------------------|--------|
| 2026-06-03 | 1.0     | Implemented Story 9.10 — consent/lawful-basis governed command, five closed token sets, `ConsentRequirementMatrix.Published` seed, pure `ConsentRequirementPolicy`/`ConsentGate` fail-closed decisions, per-project no-leak read redaction, NFR50 audit evidence, `ConsentGateEvaluator` deferral seam, tests, and ADR. | Amelia (Dev Agent) |
| 2026-06-03 | 1.1     | Senior Developer Review (AI) — Approve, 0 critical. Auto-fixed bookkeeping drift: File List now lists the two QA-added test files + the test-summary artifact; Completion Notes test counts corrected to the post-QA baseline (Contracts 454, Server 1376, total 1950, all re-run green). | Jérôme Piquot (Review) |
