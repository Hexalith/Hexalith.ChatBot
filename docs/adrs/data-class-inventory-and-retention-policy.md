# ADR: Data-class inventory and retention policy — the canonical extended class set, a versioned compliance-admin-owned inventory artifact with six closed classification dimensions, a completeness invariant, and the Story 7.4-mirrored fail-closed change command

## Status

Accepted (realized by Story 9.7, NFR52 / NFR53 / NFR23 / NFR35 / NFR1 / FR75f / architecture cross-cutting #7 / #13).
Built on the Story 7.4 compliance-admin retention editor
([the `SubmitRetentionConfigurationChange` / `RetentionSnapshotMetadata` / `ComplianceRetentionClassIds` seam]) and the
Story 7.1 admin-scope model; reuses the Story 9.1/9.3 audit envelope and no-leak floor
([worm-audit-backing.md](worm-audit-backing.md), [audit-investigation-surface.md](audit-investigation-surface.md)). It is
the **policy-definition layer** that Stories 9.8 (export) and 9.9 (deletion) consume — the story's "so that" is literally
"before export or deletion workflows use them."

## Context

NFR52 (minimization) and NFR53 (data-class distinction) require that **every** ChatBot-owned data class is inventoried
with a full retention/minimization policy tuple, and that retention/export/deletion workflows can distinguish the named
classes. NFR23 requires the inventory be a **versioned artifact** (owner, version, last-reviewed date) reviewed at least
quarterly. NFR35 requires every committed config change to record actor, old/new value, timestamp, and a policy snapshot.
NFR1 / FR75f require that only `compliance-admin` (human) may edit the inventory; everything else fails closed and is
audited. Architecture cross-cutting #13 (WORM-vs-erasure) forbids hard-deleting `audit-records`.

Story 7.4 already shipped the compliance-admin **retention-window** editor: `SubmitRetentionConfigurationChange` +
`RetentionSnapshotMetadata` + the `ComplianceRetentionClassIds` class set + the fail-closed `ParticipantAuthorizationStage`
gating + the `AuditEnvelopeFactory` evidence block. Story 9.7 adds the classification dimensions retention windows alone
don't capture (owner, redaction sensitivity, deletion behavior, export eligibility, minimization rule) and the
**completeness guarantee** (every data class classified, none unclassified) — wired through the **same** machinery.

Like the rest of Epic 9's inert-control-floor, the **live storage-layer enforcement** of retention windows / deletion
behaviors is not wired today. This story ships the **inventory artifact + classification model + the compliance-admin-gated
change command + validation + NFR35 audit recording + the seed v1 catalog + the completeness/quarterly-review invariants +
tests**, and **defers** (a) the live S-tagged Data Governance editor UI surface, (b) the storage-layer retention/deletion
enforcement owned by Stories 9.8 (export) and 9.9 (deletion) + the eventual retention-sweep runtime, and (c) any
inventory-history projection store beyond the audit chain. The inventory **is** complete and the policy **is** recorded —
"the live retention sweep isn't wired" never means "data classes are unclassified."

## Decision

1. **Extend `ComplianceRetentionClassIds`, do not fork it (define-once).** The canonical data-class identity set is the
   Story 7.4 `ComplianceRetentionClassIds`. NFR53 additionally names **backups** and **evaluation datasets**, which that
   set lacked, so it is **extended** with `Backups = "backups"` and `EvaluationDatasets = "evaluation-datasets"` (now 13
   members, keyed off `All.Count`, never a literal). This is the **one** class set the retention-window editor and the
   inventory both consume — never a parallel `DataClassIds` enum. `ValidateRetentionChangeSet` already bounds
   `windows.Count <= All.Count`, so it self-adjusts; a window for either new member validates (neither is `audit-records`,
   so the `MinimumAuditRetentionWindowDays` floor does not apply).

2. **`DataClassInventory` — the versioned artifact (NFR23/NFR53).** It carries `Owner`, `Version`, `LastReviewedAtUtc`,
   `SchemaVersion`, and the full classification set. The quarterly-review clock starts at the seed `LastReviewedAtUtc`.

3. **Six closed classification dimensions (NFR52, architecture #7/#13).** Each is an `AuditMetadata`-safe bounded token:
   **owner** (an `AdminRoles` wire token), **retention class** (the `ComplianceRetentionClassIds` ref), **redaction
   sensitivity** `{ restricted, sensitive, internal, metadata-only }`, **deletion behavior**
   `{ key-shred, projection-tombstone, hard-delete, retain-immutable }`, **export eligibility**
   `{ exportable, redacted-export, not-exportable }`, and **minimization rule** (a safe-token ref). The three new
   dimensions are closed sets with a `Contains` validator mirroring `ComplianceRetentionClassIds.All` — editors select
   within the set but never invent members.

4. **`DataClassInventorySchema.Validate` — a real completeness invariant, not a comment (AC4/NFR53).** Validation returns
   invalid (`data_class_unclassified` / `data_class_duplicate`) unless the classification set is a **bijection** over the
   canonical class set, plus per-field safe-token/closed-set checks. It reuses the Story 7.4 `RetentionValidationResult`
   and `ComplianceAdministrationSchema` token helpers — no second result type, no second token validator.

5. **Architecture #13 (WORM-vs-erasure) encoded in the deletion-behavior dimension.** `audit-records` must be
   `retain-immutable` or `projection-tombstone`, never `hard-delete` (`audit_class_deletion_invalid`). The seed catalog
   classifies `audit-records` as `retain-immutable` + `not-exportable`.

6. **`SubmitDataClassInventoryChange` — a structural twin of `SubmitRetentionConfigurationChange` (NFR1/FR75f/NFR35).** It
   is added to `ChatBotSpineCommandAllowlist`; gated in `ParticipantAuthorizationStage` by
   `!HasHumanAdminScope(principal, AdminScope.Compliance) || !IsValidDataClassInventoryChange(command) ⇒ Denied`
   (human-only via `HasHumanAdminScope`'s `IsHumanActor` gate — service clients / AI actors denied); routed through the
   one CommandGateway spine (`auth → authorize → … → pre-commit-audit → execute → post-commit-audit`) so an unauthorized
   scope, invalid command, or audit-writer-down returns a typed rejection and writes **no durable state**.

7. **NFR35 change recording mirrors `RetentionSnapshotMetadata`.** `DataClassInventorySnapshotMetadata` carries the same
   shape (replacing `ChangedRetentionClassIds` with `ChangedDataClassIds`, `ScopeUsed = AdminScope.Compliance`). The
   `AuditEnvelopeFactory.SourceEvidenceRefs` block yields `admin-operation:submit-data-class-inventory-change`,
   `admin-scope:compliance`, the change-id / snapshot / old+new fingerprint refs, and per changed classification a
   `data-class:` / `owner-role:` / `retention-class:` / `redaction-sensitivity:` / `deletion-behavior:` /
   `export-eligibility:` ref. The NFR35 actor / timestamp / policy-snapshot are carried by the envelope's
   `ActorId` / `Timestamp` / `PolicySnapshotId` — confirmed, not duplicated.

8. **`DataClassInventoryCatalog.Published` — the as-shipped seed v1 artifact (AC4).** It classifies **every** member of
   the extended class set, mirroring `OperatingBaselineCatalog.Published`: immutable, deterministic, token-only, a fixed
   seed review date (no `UtcNow`). Mailbox-owned source classes (`source-email-metadata`, `attachments`) carry the
   `mailbox-admin` owner; the rest default to `compliance-admin`.

## Metadata-only / no-leak (NFR2, NFR42)

Every emitted token — snapshot refs, fingerprints, class ids, classification dimension values, minimization-rule ref — is
an `AuditMetadata`-safe bounded token; fingerprints are `sha256:` digests, never raw inventory values. The serialization
no-leak suite and a cross-tenant scan (reusing `CrossTenantLeakageScanner` / `CrossTenantLeakageCorpus`) cover
`DataClassInventory`, `DataClassClassification`, `DataClassInventorySnapshotMetadata`, and `SubmitDataClassInventoryChange`.

## Boundary (NetArchTest-enforced)

The shared contracts (`DataClass*` records, `SubmitDataClassInventoryChange`, the closed-set ids/enums, the schema, the
seed catalog) live in `.Contracts/Commands` exactly like `ComplianceAdministrationContracts.cs` and carry no
server/gateway dependency; the new gating/validator/reader and audit-evidence logic stay `internal` to `.Server`. The
existing `DependencyDirectionFitnessTests` / `AdapterBoundaryFitnessTests` generic rules enforce this for the new types
without bespoke assertions.

## Consequences

- Stories 9.8 (export) and 9.9 (deletion) key their per-class behavior on this inventory rather than re-deriving policy;
  the export-eligibility and deletion-behavior dimensions are their direct inputs.
- The completeness invariant is a load-bearing test: any future ChatBot-owned data class must be added to the canonical
  set **and** classified in the seed catalog, or the bijection assertion fails closed.
- Deferred work (live editor UI, storage-layer enforcement, inventory-history projection) is additive against these
  contracts — never a rewrite.

## References

- `_bmad-output/implementation-artifacts/9-7-data-class-inventory-and-retention-policy.md` (this story)
- `_bmad-output/implementation-artifacts/9-3-audit-query-and-compliance-investigation-surface-s9.md` (compliance surface)
- Story 7.4 (compliance-admin scope + retention config), Story 7.1 (admin-scope model)
- [worm-audit-backing.md](worm-audit-backing.md), [audit-investigation-surface.md](audit-investigation-surface.md),
  [audit-completeness-observable.md](audit-completeness-observable.md),
  [correction-driven-vector-reindexing.md](correction-driven-vector-reindexing.md) (Epic 9 ADRs)
- `_bmad-output/planning-artifacts/architecture.md` (#7 redaction & data governance 148–149; #13 WORM-vs-erasure 167–169)
