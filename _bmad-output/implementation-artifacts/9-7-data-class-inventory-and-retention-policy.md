---
baseline_commit: 07ad6f444c100cfbab5de5c17a91d36c28d27e81
---

# Story 9.7: Data-class inventory and retention policy

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance administrator,
I want each ChatBot-owned data class inventoried with retention policy,
so that retention and minimization rules are explicit before export or deletion workflows use them.

## Acceptance Criteria

1. **Every ChatBot-owned data class is inventoried with the full classification tuple (owner, retention class, redaction sensitivity, deletion behavior, export eligibility, minimization rule) (NFR52, NFR53).**
   **Given** the ChatBot-owned data classes (source email, metadata, attachments, derived projections, AI prompts/outputs, approvals, policy snapshots, logs, backups, evaluation datasets, audit records),
   **When** the retention policy is defined,
   **Then** each class carries a `DataClassClassification` with **owner** (a responsible admin role), **retention class** (the existing Story 7.4 `ComplianceRetentionClassIds` ref), **redaction sensitivity**, **deletion behavior**, **export eligibility**, and **minimization rule** (NFR52) — and a single canonical `DataClassInventory` artifact holds the complete set.
   - **Reuse the Story 7.4 retention-class spine; do NOT introduce a second drifting class-id set.** The canonical data-class identity set is `ComplianceRetentionClassIds` (`source-email-metadata`, `attachments`, `association-records`, `evidence-snapshots`, `approval-records`, `policy-snapshots`, `lifecycle-state`, `workflow-link-maps`, `ai-prompts-outputs-context`, `logs-support-bundles`, `audit-records`). NFR53 additionally names **backups** and **evaluation datasets**, which that set lacks — extend `ComplianceRetentionClassIds` with `Backups = "backups"` and `EvaluationDatasets = "evaluation-datasets"` (the **one** define-once class set the retention-window editor and the inventory both consume) rather than creating a parallel `DataClassIds` enum. Update the existing `ComplianceRetentionClassIds.All` consumers/tests for the two new members (see Dev Notes "Cross-cutting impact").
   - **The classification dimensions are closed, bounded token sets** (each value an `AuditMetadata`-safe token). Recommended closed sets grounded in NFR52/NFR53 and architecture cross-cutting #7/#13: **redaction sensitivity** `{ restricted, sensitive, internal, metadata-only }`; **deletion behavior** `{ key-shred, projection-tombstone, hard-delete, retain-immutable }` (audit records ⇒ `retain-immutable`/`projection-tombstone`, never `hard-delete` — WORM-vs-erasure architecture #13); **export eligibility** `{ exportable, redacted-export, not-exportable }`; **owner** an `AdminRole` wire token; **minimization rule** a bounded safe-token ref describing the NFR52 minimization constraint. Define each as a closed set with a `Try…/Is…` validator mirroring `ComplianceRetentionClassIds.All` — tenants/editors may select within the set but never invent members.

2. **An actor without compliance-admin scope is denied (fail-closed) and the denial is audited (NFR1, FR75f).**
   **Given** an actor without `compliance-admin` scope,
   **When** they attempt to edit the data-class inventory or a retention class,
   **Then** the operation **fails closed** at the `ParticipantAuthorizationStage` (`AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Compliance)` is false ⇒ `Denied`) and **no durable state is written**, and the rejection is audited exactly like the Story 7.4 compliance commands.
   - **Mirror the existing `SubmitRetentionConfigurationChange` gating line-for-line.** The new `SubmitDataClassInventoryChange` governed command is gated in `ParticipantAuthorizationStage.AuthorizeAsync` by `!HasHumanAdminScope(actor.Principal, AdminScope.Compliance) || !IsValidDataClassInventoryChange(command) ⇒ Denied(AuthorizationDenied)`. Human-only is enforced by `HasHumanAdminScope`'s `IsHumanActor` gate (service clients / AI actors denied). Add the command name to `ChatBotSpineCommandAllowlist` so it is admitted at all (an un-allowlisted command is rejected upstream, satisfying fail-closed, but the AC requires the command to *exist* and be reachable only by compliance-admin).

3. **Every committed change to the inventory / a retention class records actor, old/new value, timestamp, and a policy snapshot (NFR35).**
   **Given** any change to a retention class (or a data-class classification),
   **When** it is committed,
   **Then** it records **actor** (`RequesterRef` + the audit envelope `ActorId`), **old/new value** (`OldInventorySnapshotFingerprint` / `NewInventorySnapshotFingerprint`, `sha256:` fingerprints — never raw values), **timestamp** (`EffectiveAtUtc`, UTC), and a **policy snapshot** (`DataClassInventorySnapshotMetadata` + the audit envelope `PolicySnapshotId`), via the same pre-/post-commit audit path the Story 7.4 `SubmitRetentionConfigurationChange` uses.
   - **Mirror `RetentionSnapshotMetadata` exactly.** The new `DataClassInventorySnapshotMetadata` carries `SnapshotId`, `SchemaVersion`, `SupersedesSnapshotId`/`SupersededBySnapshotId`, `SourceChangeId`, `ActorRef`, `ScopeUsed = AdminScope.Compliance`, `ChangedDataClassIds`, `SourceVersion`, `EffectiveAtUtc`, `CorrelationId`, `ReasonCode`, `PolicySnapshotId`, `OldSnapshotFingerprint`, `NewSnapshotFingerprint`. Add the `AuditEnvelopeFactory.SourceEvidenceRefs` block for `SubmitDataClassInventoryChange` (mirror lines 2849–2889): `admin-operation:submit-data-class-inventory-change`, `admin-scope:compliance`, the change-id / snapshot / old+new fingerprint refs, and one `data-class:{id}` + `classification:{dim}:{value}` ref per changed class.

4. **The inventory is a versioned artifact (owner, version, last-reviewed date) reviewed at least quarterly, with every ChatBot-owned data class classified and none left unclassified (NFR23, NFR53).**
   **Given** the data-class inventory,
   **When** it is published,
   **Then** it is a **versioned artifact** carrying `Owner`, `Version`, `LastReviewedAtUtc`, and `SchemaVersion`, with a **quarterly-review** obligation per NFR23 — and a **completeness invariant** asserts **every** member of the canonical data-class set has exactly one classification and **none is unclassified** (NFR53). Ship the as-shipped default inventory (`DataClassInventoryCatalog.Published`, the seed v1 artifact) classifying all classes, mirroring `OperatingBaselineCatalog.Published`.
   - **Completeness is a real, testable assertion, not a comment.** A `DataClassInventorySchema.Validate(inventory)` returns invalid (`data_class_unclassified` / `data_class_duplicate`) unless the classification set is a bijection over the canonical class set. A test enumerates `ComplianceRetentionClassIds.All` (extended) and asserts the seed catalog classifies each exactly once.

### Cross-cutting requirements that hold for every AC

- **Define-once / reuse — do NOT reinvent.** Consume by reference: the Story 7.4 `ComplianceRetentionClassIds` (extend it — the single canonical class set), `RetentionSnapshotMetadata` (the snapshot shape to mirror), `ComplianceAdministrationSchema` (`IsSafeComplianceToken`, `IsSafeFingerprint`, `IsUtc`, `RetentionValidationResult`, `ComplianceAdministrationSchemaVersions`); the `AdminAuthorityEvaluator.HasHumanAdminScope(…, AdminScope.Compliance)` gate; the `ParticipantAuthorizationStage` per-command `Submit…` gating + `IsValid…`/`Read…` validator pattern; the `ChatBotSpineCommandAllowlist`; the `AuditEnvelopeFactory.SourceEvidenceRefs` per-command block + `PolicyEvidenceRefs`/`SafeObjectArrayRefs` helpers; the `OperatingBaselineCatalog.Published` immutable-seed-catalog shape; the `AdminContractTests` round-trip test style. **Do not** build a second admin-authorization path, a second policy-snapshot record, or a second class-id set.
- **Fail-closed + audit-everything floor (NFR1, NFR15a, FR75g).** The inventory-change command is a state-writing path: it routes through the one CommandGateway spine (`auth → authorize → … → pre-commit-audit → execute → post-commit-audit`); on unauthorized scope, invalid command, or audit-writer-down it returns a typed rejection and writes **no durable state**. No admin operation has a skip-audit path.
- **Metadata-only / no-leak (NFR2, NFR42).** Every emitted token (snapshot refs, fingerprints, class ids, classification dimension values, minimization-rule ref) is an `AuditMetadata`-safe bounded token. Fingerprints are `sha256:` digests, never raw inventory values. Inventory artifacts and audit refs carry **no** email/attachment/prompt content. Extend the no-leak serialization suites to every new type.
- **WORM / two-phase audit untouched (D4, NFR49a).** This story emits audit through the **existing** pre-/post-commit path; it adds **no** new commit-time gate, never mutates the chain, and the `audit-records` data class's deletion behavior is `retain-immutable`/`projection-tombstone` (architecture cross-cutting #13), never `hard-delete`.
- **Boundary (NetArchTest-enforced).** New `.Server` internals (validators, the inventory store/seed if server-side, the auth-stage validator) stay `internal` to `.Server`; the shared contracts (`DataClass*` records, `SubmitDataClassInventoryChange`, the closed-set enums/ids) live in `.Contracts` exactly like `ComplianceAdministrationContracts.cs` and carry no server/gateway dependency. No `.UI`/`.Cli`/`.Mcp` references a `.Server.Gateway` type.
- **Inert-control-floor honesty.** This story ships the **inventory artifact + its classification model + the compliance-admin-gated change command + validation + NFR35 audit recording + the seed v1 catalog + completeness/quarterly-review invariants + tests + ADR** — the governance *definition* layer "before export or deletion workflows use them" (the story's own "so that"). **Deferred** (state explicitly in Completion Notes): the live S-tagged Data Governance editor UI surface, the actual storage-layer *enforcement* of retention windows / deletion behaviors (owned by Stories 9.8 export and 9.9 deletion + the eventual retention-sweep runtime), and any inventory-history projection store beyond the audit chain. Never let "the live retention sweep isn't wired" read as "data classes are unclassified" — the inventory **is** complete and the policy **is** recorded.

## Tasks / Subtasks

- [x] **Task 1 — Extend the canonical data-class set + closed classification dimensions (AC: #1, #4)**
  - [x] In `src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs`, extend `ComplianceRetentionClassIds` with `Backups = "backups"` and `EvaluationDatasets = "evaluation-datasets"`, adding both to `All`. This is the **single** define-once class set shared by the Story 7.4 retention-window editor and this story's inventory. Update any test asserting the exact membership/count of `ComplianceRetentionClassIds.All` (see Cross-cutting impact in Dev Notes) and re-confirm `ValidateRetentionChangeSet` still bounds `windows.Count <= All.Count`.
  - [x] Add the closed classification-dimension token sets (mirror `ComplianceRetentionClassIds`' static-class + `All` + `Contains` shape) in a new `DataClassInventoryContracts.cs` (same `.Contracts/Commands` folder): `DataClassRedactionSensitivities` `{ restricted, sensitive, internal, metadata-only }`; `DataClassDeletionBehaviors` `{ key-shred, projection-tombstone, hard-delete, retain-immutable }`; `DataClassExportEligibilities` `{ exportable, redacted-export, not-exportable }`. Owner is an `AdminRole` wire token (reuse `AdminRoles.TryFromWireValue`); minimization rule is a bounded `IsSafeComplianceToken` ref.

- [x] **Task 2 — Inventory + classification + snapshot contracts (AC: #1, #3, #4)**
  - [x] In `DataClassInventoryContracts.cs` add `public sealed record DataClassClassification(string DataClassId, string OwnerRole, string RetentionClassId, string RedactionSensitivity, string DeletionBehavior, string ExportEligibility, string MinimizationRuleRef)`.
  - [x] Add `public sealed record DataClassInventory(string Owner, string Version, DateTimeOffset LastReviewedAtUtc, string SchemaVersion, IReadOnlyList<DataClassClassification> Classifications)`.
  - [x] Add `public sealed record DataClassInventorySnapshotMetadata(...)` mirroring `RetentionSnapshotMetadata` field-for-field, replacing `ChangedRetentionClassIds` with `ChangedDataClassIds` and using `ScopeUsed = AdminScope.Compliance`.
  - [x] Add `public sealed record DataClassInventoryChangeSet(IReadOnlyList<DataClassClassification> Classifications)` and the governed command `public sealed record SubmitDataClassInventoryChange(string InventoryChangeId, string SourceInventorySnapshotId, string ProposedInventorySnapshotId, long SourceVersion, DataClassInventoryChangeSet ChangeSet, string ReasonCode, string RequesterRef, string SchemaVersion, string CorrelationId, string PolicySnapshotId, string OldInventorySnapshotFingerprint, string NewInventorySnapshotFingerprint, DateTimeOffset EffectiveAtUtc) : IChatBotCommand` (mirror `SubmitRetentionConfigurationChange`).
  - [x] Add `DataClassInventorySchemaVersions` (`V1 = "data-class-inventory-schema.v1"`, `All`, `IsKnown`) mirroring `ComplianceAdministrationSchemaVersions`.

- [x] **Task 3 — `DataClassInventorySchema` validation + completeness invariant (AC: #1, #4)**
  - [x] Add `public static class DataClassInventorySchema` in `DataClassInventoryContracts.cs` with `RetentionValidationResult ValidateChangeSet(DataClassInventoryChangeSet?)` and `RetentionValidationResult Validate(DataClassInventory?)`: each classification's `DataClassId ∈ ComplianceRetentionClassIds.All`; `OwnerRole` parses via `AdminRoles.TryFromWireValue`; `RetentionClassId ∈ ComplianceRetentionClassIds.All`; `RedactionSensitivity ∈ DataClassRedactionSensitivities.All`; `DeletionBehavior ∈ DataClassDeletionBehaviors.All`; `ExportEligibility ∈ DataClassExportEligibilities.All`; `MinimizationRuleRef` is a safe compliance token; **completeness** — the classification set is a bijection over `ComplianceRetentionClassIds.All` (`data_class_unclassified` if any class missing, `data_class_duplicate` if any repeated). Enforce the architecture #13 invariant: `audit-records` deletion behavior must be `retain-immutable` or `projection-tombstone`, never `hard-delete` (`audit_class_deletion_invalid`).
  - [x] Reuse `RetentionValidationResult` (do not add a new result type). Reuse `ComplianceAdministrationSchema.IsSafeComplianceToken`/`IsSafeFingerprint`/`IsUtc`.

- [x] **Task 4 — Seed v1 inventory catalog (AC: #4)**
  - [x] Add `public static class DataClassInventoryCatalog` with `Published` (the as-shipped v1 `DataClassInventory`) classifying **every** member of the extended `ComplianceRetentionClassIds.All`, mirroring `OperatingBaselineCatalog.Published`. Use defensible defaults: `audit-records` ⇒ `retain-immutable` + `not-exportable`; `ai-prompts-outputs-context`/`source-email-metadata`/`attachments` ⇒ `key-shred` + `restricted`; `policy-snapshots` ⇒ `projection-tombstone`; `evidence-snapshots`/`association-records`/`lifecycle-state`/`workflow-link-maps` ⇒ `projection-tombstone`; `logs-support-bundles`/`backups` ⇒ `key-shred`/`metadata-only`; `evaluation-datasets` ⇒ `redacted-export`. `LastReviewedAtUtc` is a fixed seed UTC constant (no `DateTimeOffset.UtcNow` — keep the catalog deterministic/immutable like `OperatingBaselineCatalog`). Owner role per class is `compliance-admin` unless a finer owner is clearly correct (e.g. mailbox classes ⇒ `mailbox-admin`).
  - [x] Assert in a test that `DataClassInventorySchema.Validate(DataClassInventoryCatalog.Published).IsValid` and that it covers `ComplianceRetentionClassIds.All` exactly.

- [x] **Task 5 — Allowlist + fail-closed compliance-admin gating (AC: #2)**
  - [x] Add `nameof(SubmitDataClassInventoryChange)` to `ChatBotSpineCommandAllowlist` (alongside `SubmitRetentionConfigurationChange`).
  - [x] In `ParticipantAuthorizationStage`, add the gating block mirroring the `SubmitRetentionConfigurationChange` block (lines 451–453): `!HasHumanAdminScope(actor.Principal, AdminScope.Compliance) || !IsValidDataClassInventoryChange(command) ⇒ Denied(AuthorizationDenied)`. Add the private `IsValidDataClassInventoryChange(object?)` validator mirroring `IsValidRetentionConfigurationChange` (lines 1892–1908) — safe tokens for all id/ref fields, `DataClassInventorySchemaVersions.IsKnown`, `IsSafeFingerprint` on old/new fingerprints, `IsUtc(EffectiveAtUtc)`, `DataClassInventorySchema.ValidateChangeSet(change.ChangeSet).IsValid` — and the `ReadSubmitDataClassInventoryChange(object?)` typed/`JsonElement` reader mirroring `ReadSubmitRetentionConfigurationChange` (lines 2080–2093).

- [x] **Task 6 — Audit source-evidence refs for the inventory change (AC: #2, #3)**
  - [x] In `AuditEnvelopeFactory` `SourceEvidenceRefs`, add the `SubmitDataClassInventoryChange` block mirroring the `SubmitRetentionConfigurationChange` block (lines 2849–2889): yield `admin-operation:submit-data-class-inventory-change`, `admin-scope:compliance`; `PolicyEvidenceRefs` for `inventoryChangeId` (`inventory-change`), `sourceInventorySnapshotId`/`proposedInventorySnapshotId` (`inventory-snapshot`), `oldInventorySnapshotFingerprint` (`inventory-old-fingerprint`), `newInventorySnapshotFingerprint` (`inventory-new-fingerprint`); and from `changeSet.classifications` (via `SafeObjectArrayRefs`) one `data-class:{dataClassId}`, `owner-role:{ownerRole}`, `retention-class:{retentionClassId}`, `redaction-sensitivity:{…}`, `deletion-behavior:{…}`, `export-eligibility:{…}` per changed classification. The NFR35 actor/timestamp/policy-snapshot are already carried by the envelope's `ActorId`/`Timestamp`/`PolicySnapshotId` — confirm, do not duplicate.

- [x] **Task 7 — Tests: contracts, fail-closed gateway, audit, completeness, no-leak, boundary (AC: #1–#4)**
  - [x] **Contracts** (`tests/Hexalith.ChatBot.Contracts.Tests/`, mirror `AdminContractTests`): round-trip + closed-set membership for the three new dimension sets; `DataClassInventorySchema.Validate`/`ValidateChangeSet` accept the seed catalog and reject (a) a missing class (`data_class_unclassified`), (b) a duplicate, (c) an unknown dimension token, (d) `audit-records` with `hard-delete` (`audit_class_deletion_invalid`); the two new `ComplianceRetentionClassIds` members round-trip and `ValidateRetentionChangeSet` accepts a window for `backups`/`evaluation-datasets`.
  - [x] **Completeness** (Task 4 test): seed catalog is a bijection over the extended class set.
  - [x] **Fail-closed gateway** (`tests/Hexalith.ChatBot.Server.Tests/Gateway/`): a non-compliance-admin (and a service-client/AI actor) submitting `SubmitDataClassInventoryChange` is `Denied(AuthorizationDenied)` with no durable write and an audited rejection; a compliance-admin with a valid change passes authorization. Reuse the existing compliance-command gateway test harness/fixtures used for `SubmitRetentionConfigurationChange`.
  - [x] **Audit evidence refs** (`tests/Hexalith.ChatBot.Server.Tests/Audit/`): the envelope for a `SubmitDataClassInventoryChange` carries `admin-scope:compliance`, the change/snapshot/fingerprint refs, and per-class `data-class:`/`retention-class:`/dimension refs; NFR35 actor/old-fingerprint/new-fingerprint/timestamp/policy-snapshot all present.
  - [x] **No-leak**: extend the serialization no-leak suite to `DataClassInventory`, `DataClassClassification`, `DataClassInventorySnapshotMetadata`, `SubmitDataClassInventoryChange` (only safe tokens/fingerprints; no raw content); add a cross-tenant scan (reuse `CrossTenantLeakageScanner`/`CrossTenantLeakageCorpus`) asserting no foreign-tenant token survives serialization of these types.
  - [x] **Boundary fitness** (`tests/Hexalith.ChatBot.Architecture.Tests/Fitness/`): new `.Server` internals stay `internal` to `.Server`; the `.Contracts` types carry no `.Server`/gateway dependency; no `.UI`/`.Cli`/`.Mcp` reference to `.Server.Gateway`.

- [x] **Task 8 — ADR + docs (AC: #1–#4)**
  - [x] Author `docs/adrs/data-class-inventory-and-retention-policy.md`: the canonical extended `ComplianceRetentionClassIds` set (why extend, not fork); the `DataClassInventory` versioned artifact + the six classification dimensions and their closed sets; the completeness (none-unclassified, NFR53) + quarterly-review (NFR23) invariants; the compliance-admin fail-closed gating (NFR1/FR75f) reusing the Story 7.4 path; the NFR35 change recording reusing the `RetentionSnapshotMetadata` shape + audit evidence refs; the architecture #13 WORM-vs-erasure constraint on `audit-records` deletion behavior; and the **deferrals** (live editor UI surface, storage-layer retention/deletion enforcement owned by 9.8/9.9, inventory-history projection). Cross-reference `_bmad-output/implementation-artifacts/9-3-…` (compliance surface), Story 7.4 (compliance-admin scope + retention config), and `docs/adrs/` Epic 9 ADRs.

## Dev Notes

### What this story actually changes (and what already exists)

Story 9.7 makes the ChatBot's **data governance explicit**: it inventories **every** ChatBot-owned data class with a full retention/minimization policy tuple, as a versioned, compliance-admin-owned, quarterly-reviewed artifact (NFR52/NFR53/NFR23), with fail-closed editing (NFR1/FR75f) and NFR35 change recording. It is the **policy-definition layer that Stories 9.8 (export) and 9.9 (deletion) consume** — the story's "so that" is literally "before export or deletion workflows use them." It is **almost entirely an additive contracts + validation + governed-command + audit-evidence story built on top of two mature Story 7.4 seams** — reuse, do not reinvent.

**The single most important framing for the dev agent:** Story 7.4 already shipped the compliance-admin **retention-window** editor (`SubmitRetentionConfigurationChange` + `RetentionSnapshotMetadata` + `ComplianceRetentionClassIds` + the fail-closed gating + the audit evidence block). Story 9.7 **adds the classification dimensions retention windows alone don't capture** (owner, redaction sensitivity, deletion behavior, export eligibility, minimization rule) and the **completeness guarantee** (every data class classified, none unclassified), wired through the **same** governed-command/auth/audit machinery. Build `SubmitDataClassInventoryChange` as a structural twin of `SubmitRetentionConfigurationChange`. A reviewer will diff your command, your auth-stage gating block, your validator, and your audit evidence block against the retention-change originals line-for-line.

**Already exists — consume by reference:**

- **The compliance-admin retention editor is solved (Story 7.4).** `ComplianceRetentionClassIds` (11 ids), `RetentionWindow`/`RetentionConfigurationChangeSet`/`RetentionSnapshotMetadata`, `SubmitRetentionConfigurationChange : IChatBotCommand`, and `ComplianceAdministrationSchema.ValidateRetentionChangeSet`/`IsSafeComplianceToken`/`IsSafeFingerprint`/`IsUtc` + `ComplianceAdministrationSchemaVersions`. **Extend `ComplianceRetentionClassIds`** (add `backups`, `evaluation-datasets`) — it is the one canonical class set; **mirror** `RetentionSnapshotMetadata` for the inventory snapshot. [Source: src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs:16-46 (class ids), 100-175 (retention records + command), 177-303 (schema)]
- **The fail-closed compliance gating is solved.** `ParticipantAuthorizationStage` gates `SubmitRetentionConfigurationChange` at lines 451-453 via `AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Compliance)` + `IsValidRetentionConfigurationChange` (1892-1908), with the typed/`JsonElement` reader `ReadSubmitRetentionConfigurationChange` (2080-2093). Mirror all three for the inventory command. [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs:451-453, 1892-1908, 2080-2093]
- **The admin-scope model is solved (Story 7.1).** `AdminScope.Compliance`, `AdminRole.ComplianceAdmin → { SeeOnly, Compliance, AuditObligation }`, `AdminAuthorityEvaluator.HasHumanAdminScope` (human-only, claim-based, fail-closed). [Source: src/Hexalith.ChatBot.Contracts/Enums/AdminScope.cs; AdminScopes.cs:66-71 (ComplianceAdmin mapping); AdminRole.cs; AdminRoles.cs; src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs:10-23]
- **The audit evidence-ref machinery is solved.** `AuditEnvelopeFactory.SourceEvidenceRefs` builds metadata-only refs per command; the `SubmitRetentionConfigurationChange` block (2849-2889) is the template. Helpers `PolicyEvidenceRefs`, `SafeObjectArrayRefs`, `SafeAdminSubjectRefs`, `AuditMetadata.SafeOptionalToken`. NFR35 actor/timestamp/policy-snapshot are carried by the envelope itself. [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs:2849-2889, 777-828 (Create), 1583-1585 (compliance-command admission), AuditMetadata.cs]
- **The allowlist is solved.** `ChatBotSpineCommandAllowlist` lists the compliance commands at 56-58; add the new one. [Source: src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs:54-58]
- **The immutable versioned-artifact / seed-catalog shape is solved (Story 8.3).** `OperatingBaselineContracts`/`OperatingBaselineCatalog.Published` — an immutable, token-only, schema-versioned closed set with a `Required` membership assertion. Mirror it for `DataClassInventoryCatalog.Published`; keep it deterministic (no `UtcNow`). [Source: src/Hexalith.ChatBot.Contracts/Queries/OperatingBaselineContracts.cs]
- **The contracts test style is solved.** `AdminContractTests` — `[Theory]`/`[InlineData]` wire round-trips + `Shouldly`. Mirror for the new dimension sets + schema. [Source: tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs]
- **The no-leak + cross-tenant scan harness is solved.** `CrossTenantLeakageScanner`/`CrossTenantLeakageCorpus` (Conformance), the `*LeakTests` (Server.Tests/Audit). Extend to the new types. [Source: tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageScanner.cs, CrossTenantLeakageCorpus.cs; tests/Hexalith.ChatBot.Server.Tests/Audit/WormAuditLeakTests.cs]

**What you are adding (the real deliverables):** (1) two new members on the canonical `ComplianceRetentionClassIds` set (`backups`, `evaluation-datasets`); (2) three closed classification-dimension token sets; (3) `DataClassClassification` / `DataClassInventory` / `DataClassInventoryChangeSet` / `DataClassInventorySnapshotMetadata` contracts + `SubmitDataClassInventoryChange` command + `DataClassInventorySchemaVersions`; (4) `DataClassInventorySchema` validation incl. the none-unclassified completeness invariant + the architecture-#13 audit-records deletion constraint; (5) `DataClassInventoryCatalog.Published` seed v1 artifact; (6) allowlist entry + `ParticipantAuthorizationStage` fail-closed compliance gating + validator + reader; (7) the `AuditEnvelopeFactory` evidence block; (8) tests + ADR.

### Architecture constraints (must follow)

- **NFR52 (minimization) / NFR53 (data-class distinction).** Each class minimized to authorized-workflow/audit/retention need; retention/export/deletion workflows must distinguish the named classes. The inventory is the authoritative classification all of Epic 9's data-lifecycle stories key on. [Source: epics.md:259-260; architecture.md:148-149 (#7 redaction & data governance)]
- **NFR1 / FR75f (fail-closed compliance scope).** Only `compliance-admin` (or `tenant-admin` via the FR75a scope union) may edit the inventory/retention; everything else fails closed and is audited. Compliance-admin "configures retention within NFR49a bounds" and "cannot operate on workflow items." [Source: epics.md:132 (FR75f), 1882-1896 (Story 7.4); architecture.md governance]
- **NFR35 (config change recording).** Actor, old/new value, timestamp, policy snapshot; versioned; authority-expanding/destructive changes require a new version, not a rollback overwrite (the snapshot supersession chain carries this). [Source: epics.md:233]
- **NFR23 (versioned, quarterly-reviewed baselines).** The inventory records owner, version, last-reviewed date and is reviewed at least quarterly. [Source: epics.md:218]
- **Architecture cross-cutting #13 (WORM-vs-erasure).** `audit-records` cannot be hard-deleted — erasure is projection-tombstone + key-shred over an immutable chain. The deletion-behavior dimension must encode this; `audit-records ⇒ retain-immutable`/`projection-tombstone`. [Source: architecture.md:167-169; Story 9.1 Dev Notes]
- **Derived-record stamping (#7).** Every derived class already carries `retentionClass`/`redactionState` (`GovernedOperationView.RetentionClass`, etc.); the inventory is the *catalog of policy* for those stamps, not a re-implementation of stamping. [Source: architecture.md:507-509, 572; src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs]
- **CommandGateway spine / fail-closed (NFR15a).** The inventory-change command is one of the state-writing paths; it routes through the single audit-commit seam and writes no partial state on rejection. [Source: architecture.md:542-551]
- **Boundary (NetArchTest-enforced).** Contracts have no server dependency; server internals `internal`; adapters never replicate stages. [Source: architecture.md:577-580; tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs]

### Cross-cutting impact — extending `ComplianceRetentionClassIds`

Adding `Backups`/`EvaluationDatasets` to `ComplianceRetentionClassIds.All` is the one change that ripples beyond this story's new files. Before finishing:
- **`ComplianceAdministrationSchema.ValidateRetentionChangeSet`** bounds `windows.Count <= ComplianceRetentionClassIds.All.Count` — still correct, now allows two more windows. Confirm a window for `backups`/`evaluation-datasets` validates (neither is `audit-records`, so the `MinimumAuditRetentionWindowDays` floor does not apply).
- **No current test hard-codes `ComplianceRetentionClassIds.All.Count == 11`** (verified at baseline — the only `.Count` use is the self-adjusting bound in `ValidateRetentionChangeSet`, and existing usages reference named members like `AuditRecords`/`SourceEmailMetadata`). So the membership extension is low-ripple. Still: if your own new completeness test or any future assertion references the set size, key it off `ComplianceRetentionClassIds.All.Count` (now 13), never a literal — a stale literal count is the classic Epic 7–9 review auto-fix.

### Previous-work intelligence — apply directly

- **Mirror the Story 7.4 retention-change command structurally; reviewers diff line-for-line.** Command shape, allowlist line, auth-stage gating block, `IsValid…`/`Read…` validator, audit evidence block — all twins of the retention-change originals.
- **Define-once is enforced (the 9.4/9.5/9.6 lesson).** ONE class-id set (`ComplianceRetentionClassIds`, extended — never a parallel `DataClassIds`); ONE policy-snapshot shape (mirror `RetentionSnapshotMetadata`); ONE validation-result type (`RetentionValidationResult`); ONE admin-authorization gate (`HasHumanAdminScope`). Never inline a second of any of these.
- **Bookkeeping drift is the #1 recurring review auto-fix across Epics 7–9** (stale test counts, File List omissions — 9.1–9.6 reviews each fixed these). Keep the **File List exhaustive** (every new + modified source/test/ADR, including `ComplianceAdministrationContracts.cs` and the updated count tests) and every cited test count accurate against the live run.
- **Inert-control-floor honesty (the 9.4/9.5/9.6 deferral discipline).** Ship the inventory definition + classification + change command + validation + NFR35 audit + seed catalog + completeness/quarterly invariants. **Defer** the live editor UI surface, the storage-layer retention/deletion enforcement (9.8/9.9 own it), and the inventory-history projection. **State deferrals explicitly in Completion Notes**; never let a deferral read as "data classes unclassified."
- **No-leak first.** Inventory artifacts and audit refs are metadata-only by construction — safe tokens + `sha256:` fingerprints, never raw inventory values. Every serialized type passes the no-leak suite.
- **Backward-compatibility with Story 7.4 is non-negotiable.** Extending `ComplianceRetentionClassIds` and adding a sibling compliance command must keep all existing 7.4 / 9.3 compliance tests green.

### Project Structure Notes

- **Contracts (all shared types — no server dependency):**
  - `src/Hexalith.ChatBot.Contracts/Commands/DataClassInventoryContracts.cs` (**new:** the dimension sets, `DataClassClassification`, `DataClassInventory`, `DataClassInventoryChangeSet`, `DataClassInventorySnapshotMetadata`, `SubmitDataClassInventoryChange`, `DataClassInventorySchemaVersions`, `DataClassInventorySchema`, `DataClassInventoryCatalog`).
  - `src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs` (**modified:** `+ Backups`, `+ EvaluationDatasets` on `ComplianceRetentionClassIds`).
- **Server (gating + audit only — no new aggregate; compliance commands are spine-governed):**
  - `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` (**modified:** gating block + `IsValidDataClassInventoryChange` + `ReadSubmitDataClassInventoryChange`).
  - `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` (**modified:** `+ SubmitDataClassInventoryChange`).
  - `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (**modified:** `SubmitDataClassInventoryChange` evidence block + admission-list entry at ~1583-1585).
- **Tests:** `tests/Hexalith.ChatBot.Contracts.Tests/` (dimension/schema/completeness/round-trip + updated `ComplianceRetentionClassIds` count), `tests/Hexalith.ChatBot.Server.Tests/Gateway/` (fail-closed), `tests/Hexalith.ChatBot.Server.Tests/Audit/` (evidence refs + no-leak), `tests/Hexalith.ChatBot.Conformance.Tests/` (cross-tenant scan), `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/` (boundary).
- **Docs:** `docs/adrs/data-class-inventory-and-retention-policy.md`.
- No new top-level project; no conflict with the unified structure — contracts in `.Contracts/Commands`, gating/audit in `.Server`, exactly like Story 7.4 compliance administration. The live editor surface, when built, is an additive S-tagged UI consuming these contracts.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.7 (lines 2470-2490); Story 7.4 (1882-1896); Story 7.1 (1824-1844); Story 7.2 (1846-1864); Epic 9 (2358-2360); NFR52/NFR53 (259-260); NFR23 (218); NFR35 (233); FR75f (132); FR75a/FR75g (127, 1834-1844)]
- [Source: _bmad-output/planning-artifacts/architecture.md#redaction & data governance (#7, 148-149); WORM-vs-erasure (#13, 167-169); derived-record stamping (507-509, 572); audit envelope min fields (522-525); CommandGateway flow (542-551); pattern enforcement (577-585)]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs:16-46 (ComplianceRetentionClassIds), 100-175 (RetentionWindow/ChangeSet/SnapshotMetadata/SubmitRetentionConfigurationChange), 177-303 (ComplianceAdministrationSchema)]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyContracts.cs (TenantPolicySnapshotMetadata/ChangeSet shape — NFR35 reference); src/Hexalith.ChatBot.Contracts/Queries/OperatingBaselineContracts.cs (seed-catalog shape)]
- [Source: src/Hexalith.ChatBot.Contracts/Enums/AdminScope.cs; AdminScopes.cs:66-71; AdminRole.cs; AdminRoles.cs]
- [Source: src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs:10-23; src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs:13-14 (compliance-scope gate)]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs:451-453 (retention gating), 1892-1908 (IsValidRetentionConfigurationChange), 2080-2093 (reader)]
- [Source: src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs:54-58; src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs:2849-2889 (retention evidence block), 1583-1585 (compliance admission), 777-828 (Create); AuditMetadata.cs]
- [Source: src/Hexalith.ChatBot.Server/Projections/GovernedOperationView.cs (RetentionClass/RedactionState stamping)]
- [Source: tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs (contracts test style); tests/Hexalith.ChatBot.Conformance.Tests/Harness/CrossTenantLeakageScanner.cs, CrossTenantLeakageCorpus.cs; tests/Hexalith.ChatBot.Server.Tests/Audit/WormAuditLeakTests.cs; tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs]
- [Source: _bmad-output/implementation-artifacts/9-3-audit-query-and-compliance-investigation-surface-s9.md; 9-6-correction-driven-vector-reindexing.md (Epic 9 deferral/define-once discipline)]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8[1m])

### Debug Log References

- Initial contracts no-leak test asserted `json.ShouldNotContain("prompt")`, which is a false positive: the canonical
  class id `ai-prompts-outputs-context` legitimately contains the substring "prompt". Removed that substring check (the
  no-leak floor bans raw prompt CONTENT, not the bounded class-id token) in both the contracts test and the gateway
  audit-refs test; replaced with a clarifying comment.
- `DataClassInventoryAuthorizationTests.cs` initially missed `using Hexalith.ChatBot.Contracts.Enums;`, so
  `ChatBotSurfaceOrigin` did not resolve (CS0103). Added the using (mirroring `EscalationPolicyAuthorizationTests`).

### Completion Notes List

Implemented Story 9.7 as an additive contracts + gateway-gating + audit-evidence story built on the Story 7.4 compliance
seam — `SubmitDataClassInventoryChange` is a structural twin of `SubmitRetentionConfigurationChange` (command shape,
allowlist line, auth-stage gating block, `IsValid…`/`Read…` validator, audit evidence block).

- **Define-once:** extended the single canonical `ComplianceRetentionClassIds` set with `Backups`/`EvaluationDatasets`
  (now 13 members, keyed off `All.Count`) rather than forking a parallel `DataClassIds`. Reused
  `RetentionValidationResult`, `ComplianceAdministrationSchema` token helpers, `AdminAuthorityEvaluator.HasHumanAdminScope`,
  the `ParticipantAuthorizationStage` gating pattern, the `ChatBotSpineCommandAllowlist`, and the `AuditEnvelopeFactory`
  evidence-ref helpers. `DataClassInventorySnapshotMetadata` mirrors `RetentionSnapshotMetadata` field-for-field.
- **Completeness is a real assertion:** `DataClassInventorySchema.Validate` returns `data_class_unclassified` /
  `data_class_duplicate` unless the classification set is a bijection over the extended canonical set; the seed catalog is
  asserted to cover `ComplianceRetentionClassIds.All` exactly once. The architecture #13 WORM-vs-erasure constraint is
  enforced (`audit-records` may never be `hard-delete` ⇒ `audit_class_deletion_invalid`).
- **Fail-closed + audit-everything:** the new command is gated human-compliance-admin-only at
  `ParticipantAuthorizationStage`; service clients / AI actors / other admin roles are `Denied(AuthorizationDenied)` with
  no durable write; an audit-writer-down pre-commit returns 503 `AuditUnavailable` with no dispatch and an audited
  rejection carrying `admin-scope:compliance`. Per-changed-class evidence refs (`data-class:` / `owner-role:` /
  `retention-class:` / `redaction-sensitivity:` / `deletion-behavior:` / `export-eligibility:`) are emitted; NFR35
  actor/timestamp/policy-snapshot are carried by the envelope and confirmed not duplicated.
- **No-leak / boundary:** all new types serialize to safe tokens + `sha256:` fingerprints only; the contracts no-leak
  suite + a Conformance cross-tenant scan (reusing `CrossTenantLeakageScanner`/`CrossTenantLeakageCorpus`) cover them. The
  new `.Contracts` types carry no server dependency and no new public `.Server` type was added (gating/validator/reader are
  private; the allowlist is already `internal`), so the existing generic `DependencyDirectionFitnessTests` /
  `AdapterBoundaryFitnessTests` enforce the boundary for the new types without bespoke assertions (all 39 fitness tests
  green).

**Deferred (explicitly, per the inert-control-floor discipline):** (1) the live S-tagged Data Governance editor UI surface;
(2) the storage-layer *enforcement* of retention windows / deletion behaviors — owned by Story 9.8 (export) and Story 9.9
(deletion) plus the eventual retention-sweep runtime; (3) any inventory-history projection store beyond the audit chain.
The inventory **is** complete and the policy **is** recorded — no data class is unclassified.

**Tests run (all green):** Contracts 361, Server 1341, Architecture 39, Conformance 78, Client 23. New tests:
`DataClassInventoryContractTests` (22 — includes 4 branch-coverage cases added in QA automation), `DataClassInventoryAuthorizationTests` (2), `CommandGatewayTests` +2 (fail-closed
on audit-down + per-class evidence refs), `DataClassInventoryLeakageScanTests` (1). Build clean (0 warnings).

### File List

**Source (modified):**
- `src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs` (`+ Backups`, `+ EvaluationDatasets` on `ComplianceRetentionClassIds`)
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotSpineCommandAllowlist.cs` (`+ SubmitDataClassInventoryChange`)
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs` (gating block + `IsValidDataClassInventoryChange` + `ReadSubmitDataClassInventoryChange`)
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs` (admission-list entry + `SubmitDataClassInventoryChange` source-evidence block)

**Source (new):**
- `src/Hexalith.ChatBot.Contracts/Commands/DataClassInventoryContracts.cs` (dimension sets, `DataClassClassification`, `DataClassInventory`, `DataClassInventorySnapshotMetadata`, `DataClassInventoryChangeSet`, `SubmitDataClassInventoryChange`, `DataClassInventorySchemaVersions`, `DataClassInventorySchema`, `DataClassInventoryCatalog`)

**Tests (new):**
- `tests/Hexalith.ChatBot.Contracts.Tests/DataClassInventoryContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/DataClassInventoryAuthorizationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/DataClassInventoryLeakageScanTests.cs`

**Tests (modified):**
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` (`DataClassInventoryChangeCommand` factory + fail-closed-on-audit-down + per-class audit-evidence tests)

**Docs (new):**
- `docs/adrs/data-class-inventory-and-retention-policy.md`

**Tracking (modified):**
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (9-7 → in-progress → review)

## Change Log

- 2026-06-03 — Story 9.7 implemented. Extended the canonical `ComplianceRetentionClassIds` set (`+ backups`,
  `+ evaluation-datasets`); added the `DataClassInventory` versioned artifact with six closed classification dimensions,
  `DataClassInventorySchema` completeness + WORM-vs-erasure invariants, the `DataClassInventoryCatalog.Published` seed v1
  catalog, and the compliance-admin-gated `SubmitDataClassInventoryChange` governed command (allowlist + fail-closed
  `ParticipantAuthorizationStage` gating + validator/reader + `AuditEnvelopeFactory` source-evidence block, mirroring the
  Story 7.4 retention-change machinery). Added contracts/gateway/audit/no-leak tests + ADR. All tests green
  (Contracts 361, Server 1341, Architecture 39, Conformance 78, Client 23). Status → review.
- 2026-06-03 — Automated review (story-automator). Re-ran the affected suites: Contracts 361, Server 1341,
  Architecture 39, Conformance 78 — all green, 0 warnings. ACs #1–#4 and Tasks 1–8 verified against the
  implementation; File List confirmed exhaustive vs git. Corrected stale Dev Agent Record test counts
  (Contracts 357→361, new contract tests 18→22) to match the live run. No CRITICAL issues. Status → done.
