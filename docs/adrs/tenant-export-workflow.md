# ADR: Tenant export workflow — a compliance-admin-gated governed export-request command, a pure data-class/redaction/authority-aware export planner reading the Story 9.7 inventory, a per-class success/failure model with stable-run-id retryability, a no-partial-exposure manifest invariant, and the NFR45/NFR50 audit evidence block

## Status

Accepted (realized by Story 9.8, FR58 / NFR45 / NFR2 / NFR17 / NFR18 / NFR50 / NFR53 / architecture cross-cutting
#13 / two-altitude idempotency Story 1.5 / UX-DR39). Built directly on the Story 9.7 data-class inventory
([data-class-inventory-and-retention-policy.md](data-class-inventory-and-retention-policy.md)), the Story 9.3
per-project no-leak compliance read surface ([audit-investigation-surface.md](audit-investigation-surface.md)), the
Story 7.4 compliance-admin retention editor, the Story 7.1 admin-scope model, and the Story 9.1/9.2 WORM audit chain
and audit-completeness floor. It is the **first consumer** of the Story 9.7 inventory — Story 9.7's "so that" was
literally "before export or deletion workflows use them," and it named Story 9.8 as the export owner.

## Context

FR58 requires that authorized admins/reviewers access **export workflows**; NFR45 requires export/support-bundle
output to preserve correlation/state/reason context **without** exposing restricted tenant/project/participant/file/
message/audit evidence; NFR2 requires that unauthorized detail is excluded/redacted and **indistinguishable from
not-found**; NFR17/NFR18 require partial-failure visibility and a retryable-vs-terminal classification; NFR50 requires
every export envelope to carry the full required-field set; architecture cross-cutting #13 (WORM-vs-erasure) forbids
ever exporting `audit-records` (and the seed marks `backups` `not-exportable` too).

Story 9.7 already shipped the **policy-definition layer**: `DataClassInventoryCatalog.Published` classifies every one
of the 13 canonical `ComplianceRetentionClassIds` with an `ExportEligibility` (`exportable` / `redacted-export` /
`not-exportable`) and a `RedactionSensitivity`. Story 9.3 already shipped the **per-project no-leak authority**
(`ComplianceAuditReadPolicy.HasPerProjectAuthority` + the `ProjectOwnerClaim` grant + the safe-not-found rendering).
Story 7.4/9.7 already shipped the **governed-command machinery** (`SubmitDataClassInventoryChange` as the structural
twin of `SubmitRetentionConfigurationChange`: allowlist line, `ParticipantAuthorizationStage` gating block +
`IsValid…`/`Read…`, `AuditEnvelopeFactory` admission entry + evidence block). NFR18 already shipped the **one retry
taxonomy** (`RetryFailurePolicy.Classify`).

Story 9.8 adds the genuinely new value — a **pure, testable export decision engine** that, per requested data class,
decides `included` / `redacted` / `excluded` keyed off the 9.7 inventory and the requester's per-project authority,
seals a no-partial-exposure manifest, and records the run through the existing audit-commit spine — wired through the
**same** machinery, forking none of it.

Like the rest of Epic 9's inert-control-floor, the **byte-producing extraction runtime** is not wired today. This
story ships the **governed export-request command + the data-class/redaction/authority-bounded plan + the per-class
success/failure model + stable-run-id retryability + the no-partial-exposure manifest invariant + NFR45/NFR50 audit
recording + validation + tests**, and **defers** (a) the live S-tagged export UI surface, (b) the storage-layer
extraction runtime that reads each derived store, produces the redacted artifact bytes, and seals/stores the
downloadable file, and (c) any export-history projection store beyond the audit chain. The request **is** governed,
the plan **is** data-class/redaction/authority bounded, and the run **is** audited — "the byte-producing runtime
isn't wired" never means "exports are unbounded or untraceable."

## Decision

1. **Consume the Story 9.7 inventory as the single source of truth (define-once).** `TenantExportPlanner.Plan` reads
   `DataClassInventory` (`DataClassInventoryCatalog.Published` or the snapshot referenced by the request) for
   `ExportEligibility` and `RedactionSensitivity`. It forks **no** second eligibility set, class-id set, or
   sensitivity set; requested class ids validate `⊆ ComplianceRetentionClassIds.All` (the extended 13-member set,
   bound off `.Count`, never a literal `13`).

2. **Closed, bounded token sets, each an `AuditMetadata`-safe token, mirroring `DataClassExportEligibilities`
   line-for-line.** `TenantExportClassDispositions` `{ included, redacted, excluded }`;
   `TenantExportExclusionReasons` `{ not-exportable, unauthorized, not-requested }`;
   `TenantExportRedactionDecisions` `{ metadata-only, redacted, none }`; `TenantExportClassStatuses`
   `{ succeeded, failed-retryable, failed-terminal }`; `TenantExportRunStatuses`
   `{ completed, partial-failure, failed }`. Callers select within the set, never invent members.

3. **A pure, testable planner — `eligibility → disposition`, with eligibility absolute over authority.** For each
   requested class: a `not-exportable` class is **always** `excluded` / `not-exportable` regardless of authority (so
   the WORM classes `audit-records` / `backups` are never produced and never carry the `unauthorized` reason); an
   `exportable` / `redacted-export` class is downgraded to `excluded` / `unauthorized` when authority is missing,
   else mapped to `included` (redaction decision `none`) or `redacted` (decision `redacted`, or `metadata-only` when
   the class's source sensitivity is `metadata-only`). The `ManifestFingerprint` is computed deterministically
   (`sha256:` over the sorted **succeeded includable** class ids only — no raw bytes).

4. **Per-project no-leak authority reuses the Story 9.3 pattern — no second authority path.** The server-only
   `TenantExportAuthorizationPolicy` gates on `AdminAuthorityEvaluator.HasHumanAdminScope(.., AdminScope.Compliance)`
   and projects the granted `ProjectOwnerClaim` refs (filtered by `AuditMetadata.IsSafeStableIdentifier`) into the
   **bounded** `TenantExportAuthorityView(bool HasComplianceScope, IReadOnlySet<string> AuthorizedProjectRefs)`. The
   pure planner consumes that view — never a `ClaimsPrincipal` — so the `.Contracts` boundary holds. An unauthorized
   project yields `excluded` / `unauthorized` and **drops** the project ref: the bounded `unauthorized` token is the
   only signal, never the resource identity (NFR2), exactly like the Story 9.3 `EscalationRequired` state.

5. **Per-class success/failure reuses the one retry taxonomy (NFR17/NFR18) — no second classifier.** The server-only
   `TenantExportFailureClassifier` maps a per-class extraction failure to `failed-retryable` / `failed-terminal` via
   `RetryFailurePolicy.Classify`. The run status is `completed` (all includable succeeded), `partial-failure`, or
   `failed`, consistent with the per-class statuses.

6. **No partial file exposed — a manifest-level invariant in `TenantExportSchema`.** The `ManifestFingerprint`
   covers **exactly** the `succeeded` includable classes; a non-`succeeded` (or excluded) class contributes **no**
   artifact (`ArtifactFingerprint` empty). `ValidateRunResult` re-derives the manifest and rejects any run whose
   manifest claims a class that is not `succeeded` includable (`export_manifest_partial_exposed`), enforces the
   eligibility-vs-disposition invariant (`export_eligibility_disposition_mismatch`), the WORM-class invariant
   (`export_worm_class_exposed`), and completeness (`export_class_unprocessed` / `export_class_duplicate`).

7. **The governed command is a structural twin of `SubmitDataClassInventoryChange`.** `SubmitTenantExportRequest`
   (with the stable `ExportRunId` idempotency/run key + `SourceVersion` version guard, the Story 1.5 two-altitude
   floor) is added to `ChatBotSpineCommandAllowlist`, gated fail-closed in `ParticipantAuthorizationStage`
   (`HasHumanAdminScope(.., Compliance)` + `IsValidTenantExportRequest` + `ReadSubmitTenantExportRequest`), and routed
   through the one CommandGateway audit-commit spine. On non-compliance scope, non-human actor, invalid command, or
   audit-writer-down it returns a typed rejection, writes **no** durable state, and exposes **no** artifact.

8. **NFR45/NFR50 audit evidence mirrors the `SubmitDataClassInventoryChange` block.** `AuditEnvelopeFactory`
   admits `SubmitTenantExportRequest` and yields `admin-operation:submit-tenant-export-request` + `admin-scope:
   compliance`, `export-run:` / `inventory-snapshot:` / `export-manifest-fingerprint:` policy refs, one
   `data-class:{id}` per requested class, and `export-scope-tenant:` / `export-scope-project:` refs — only the
   **authorized** project refs reach the committed command, so no unauthorized ref leaks. Actor / actor-type /
   timestamp / policy-snapshot / decision / reason are carried by the envelope `Create` and not duplicated.

## Consequences

- **Positive.** Tenant export is now governed, data-class-aware, redaction-aware, authority-bounded, retryable under a
  stable run id, and audited — the "authorized export requests are traceable and bounded" the story's "so that"
  demands. Every new type is metadata-only by construction and passes the no-leak + cross-tenant suites. Define-once
  holds: no second eligibility/class/sensitivity set, authority path, retry taxonomy, validation-result type, or
  allowlist. All existing 7.4 / 9.3 / 9.7 compliance, gateway, audit, and architecture tests stay green.
- **Deferred (inert-control-floor honesty).** The live S-tagged export UI surface, the storage-layer extraction
  runtime (reads each derived store, produces redacted bytes, seals/stores the downloadable artifact), and any
  export-history projection store beyond the audit chain. `TenantExportFailureClassifier` ships the classification
  seam the deferred runtime will call.
- **Boundary.** `TenantExportAuthorizationPolicy` / `TenantExportFailureClassifier` and the auth-stage
  validator/reader stay `internal` to `.Server`; the shared contracts (`TenantExport*` records,
  `SubmitTenantExportRequest`, the closed-set token classes, `TenantExportSchema`, the pure `TenantExportPlanner`)
  live in `.Contracts` with no server/gateway/`ClaimsPrincipal` dependency. The `ScaffoldArchitectureTests` legacy-
  lifecycle-literal allowlist gains `TenantExportContracts.cs` (it legitimately owns the bounded `succeeded` export
  status token, exactly like `ApprovalStatus.cs` / `AiOutcomeStatus.cs`).

## References

- Story 9.8 spec: [_bmad-output/implementation-artifacts/9-8-tenant-export-workflow.md](../../_bmad-output/implementation-artifacts/9-8-tenant-export-workflow.md)
- Story 9.7 (inventory the planner reads): [data-class-inventory-and-retention-policy.md](data-class-inventory-and-retention-policy.md)
- Story 9.3 (per-project no-leak authority): [audit-investigation-surface.md](audit-investigation-surface.md)
- `src/Hexalith.ChatBot.Contracts/Commands/TenantExportContracts.cs` (token sets, contracts, planner, schema)
- `src/Hexalith.ChatBot.Server/Audit/TenantExportAuthorizationPolicy.cs` (authority view + failure classifier)
- `src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs`, `Gateway/ChatBotSpineCommandAllowlist.cs`, `Audit/AuditEnvelopeFactory.cs`
- Architecture: WORM-vs-erasure (#13), CommandGateway flow, audit envelope min fields, pattern enforcement (Epic 9)
