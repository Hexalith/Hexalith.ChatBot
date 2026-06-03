# ADR: Deletion and erasure workflow — a compliance-admin-gated governed deletion/erasure-request command, a pure deletion-behavior/authority-aware planner reading the Story 9.7 inventory, a per-class success/failure model with stable-run-id retryability, a no-silent-partial-deletion schema invariant, an `ErasureProofArtifact`, and the WORM-safe audit-chain erasure delegated to the existing Story 9.1 `AuditRedactionService`

## Status

Accepted (realized by Story 9.9, FR58 / NFR1 / NFR2 / NFR17 / NFR18 / NFR49 / NFR49a / NFR50 / NFR53 / architecture
cross-cutting #13 / two-altitude idempotency Story 1.5). Built directly on the Story 9.7 data-class inventory
([data-class-inventory-and-retention-policy.md](data-class-inventory-and-retention-policy.md)), the Story 9.8 tenant
export workflow ([tenant-export-workflow.md](tenant-export-workflow.md)) as the structural template, the Story 9.1
WORM-safe erasure mechanism ([worm-audit-backing.md](worm-audit-backing.md)), the Story 9.3 per-project no-leak
compliance read surface, the Story 7.4 compliance-admin retention editor, and the Story 7.1 admin-scope model. It is
the **second consumer** of the Story 9.7 inventory — Story 9.7's "so that" was literally "before export **or deletion**
workflows use them" — and the **direct sibling of Story 9.8 export** (export → destruction).

## Context

GDPR right-to-erasure must be honored **without** mutating the immutable, tamper-evident audit chain. FR58 requires
that authorized admins/reviewers access **deletion workflows**; NFR1 requires non-authorized deletion/erasure to fail
closed at the gateway, destroy nothing, and be audited; NFR2 requires unauthorized detail to be excluded and
**indistinguishable from not-found**; NFR17/NFR18 require partial-failure visibility and a retryable-vs-terminal
classification; NFR49/NFR49a forbid storage-layer deletion of audit records — erasure of audited subjects is a
projection **tombstone + key-shred** over an append-only chain, and nightly per-tenant chain verification must still
pass; NFR50 requires every deletion/erasure envelope to carry the full required-field set; NFR53 requires the workflow
to distinguish the named data classes and a completed erasure to produce a proof artifact queryable for compliance;
architecture cross-cutting #13 (WORM-vs-erasure) requires `audit-records` to stay `retain-immutable` and never be
destroyed.

The danger is that deletion is the **most destructive operation in the system**. The framing risk is twofold: building
a *second* crypto-shred/tombstone path (the worst possible define-once violation, since Story 9.1 already built the one
mechanism), or letting an ambiguous/unauthorized case escalate to destruction.

## Decision

Ship the **governed decision/recording/proof layer** as an almost-entirely-additive contracts + pure-decision-engine +
governed-command + gateway-gating + audit-evidence story built on mature seams — a structural twin of Story 9.8 export.

1. **`SubmitDeletionErasureRequest` governed command** — a line-for-line structural twin of `SubmitTenantExportRequest`:
   allowlisted in `ChatBotSpineCommandAllowlist`, gated at `ParticipantAuthorizationStage` by
   `HasHumanAdminScope(.., AdminScope.Compliance)` + a typed `IsValidDeletionErasureRequest`/`ReadSubmitDeletionErasureRequest`,
   routed through the one CommandGateway audit-commit spine, fail-closed (no durable write, **no destruction**) on
   unauthorized scope / invalid command / audit-writer-down. `DeletionRunId` is the stable idempotency/run key
   (Story 1.5 two-altitude floor): a re-submission carrying the same `DeletionRunId` and a matching `SourceVersion` is
   version-guarded and idempotent — no duplicate destruction.

2. **Five closed token sets** (each an `AuditMetadata`-safe bounded token, mirroring `TenantExportClassDispositions`):
   `DeletionErasureClassActions` `{ crypto-shredded, tombstoned, hard-deleted, retained }`;
   `DeletionErasureExclusionReasons` `{ worm-retained, unauthorized, not-requested }`;
   `DeletionErasureClassStatuses` `{ succeeded, failed-retryable, failed-terminal }`;
   `DeletionErasureRunStatuses` `{ completed, partial-failure, failed }`;
   `DeletionErasureModes` `{ deletion, erasure }`.

3. **The `DeletionErasurePlanner` pure decision engine** — for each requested class it reads the **Story 9.7
   `DataClassInventoryCatalog.Published`** `DeletionBehavior` (no second behavior/class set) and decides the action:
   `key-shred`⇒`crypto-shredded`; `projection-tombstone`⇒`tombstoned`; `hard-delete`⇒`hard-deleted`;
   `retain-immutable`⇒`retained`/`worm-retained`. It then applies a **bounded** `DeletionErasureAuthorityView` (never a
   `ClaimsPrincipal`): a project ref absent from the authorized set forces `retained`/`unauthorized` and **drops the
   project ref** (NFR2). Two fail-closed invariants make destruction safe:
   - **WORM behavior is absolute over authority** — a `retain-immutable` class is *always* `retained`/`worm-retained`,
     never `unauthorized`, so `audit-records` are never mislabeled.
   - **Unauthorized is never destructive** — an `unauthorized` class is `retained`, never `crypto-shredded`/
     `tombstoned`/`hard-deleted`. An unclassifiable class fails closed to `retained`.

4. **The `DeletionErasureSchema` invariants** (reusing the Story 7.4 `RetentionValidationResult` + the
   `ComplianceAdministrationSchema.IsSafe*`/`IsUtc` token helpers — no second result type or validator):
   behavior-vs-action (`deletion_behavior_action_mismatch`), WORM-class (`deletion_worm_class_destroyed`),
   no-silent-partial — every requested class appears in `ClassResults` exactly once
   (`deletion_class_unprocessed`/`deletion_class_duplicate`) and a non-`succeeded` class contributes **no** proof entry
   (`deletion_proof_partial_exposed`), the proof fingerprint covers exactly the carried confirmation set, and
   run-status consistency.

5. **The per-class success/failure model reuses `RetryFailurePolicy.Classify`** (the one retry taxonomy):
   `DeletionErasureFailureClassifier` maps a per-class destruction-failure reason to `failed-retryable` /
   `failed-terminal` — no second retryable-vs-terminal classifier.

6. **WORM-vs-erasure resolution — orchestrate, don't rebuild.** `audit-records` is never destroyed; erasure over the
   audit chain runs strictly through the **existing Story 9.1 `AuditRedactionService`** (append redaction record →
   `IKmsRedactionKeyStore.Shred` → `IRedactionProjectionStore.Tombstone`). The thin server-internal
   `DeletionErasureRunner` calls that seam and turns the returned `AuditRedactionRegistration` into a populated
   `ErasureProofEntry` (tenant-scoped subject locator + `tombstoned`, safe KMS key handle + `shredded`). After erasure
   the `WormAuditChainVerifier` still verifies end-to-end — the chain grows or stays; it never shrinks or mutates.

7. **`ErasureProofArtifact` (NFR53)** — a metadata-only proof carrying, per successfully-erased subject/class, the
   tombstone + per-store key-shred confirmations, the `DeletionRunId`/`CorrelationId`, and a deterministic `sha256:`
   `ProofFingerprint` over the confirmation set. A class that did not reach `succeeded` contributes no entry. The proof
   is queryable for compliance through the run's audit envelope (the `deletion-proof:`/`erasure-tombstone:`/
   `erasure-key-shred:` evidence refs make it reconstructable from the chain).

8. **NFR45/NFR50 audit evidence block** — `AuditEnvelopeFactory.SourceEvidenceRefs` yields
   `admin-operation:submit-deletion-erasure-request`, `admin-scope:compliance`, `deletion-run:`, `inventory-snapshot:`,
   `deletion-proof:`, `deletion-mode:`, one `data-class:{id}` per requested class, and `deletion-scope-tenant:` /
   `deletion-scope-project:` (only **authorized** project refs reach the committed command, so no unauthorized ref
   leaks). Actor/actor-type/timestamp/policy-snapshot/decision/reason are carried by the envelope.

### Boundary

The shared contracts (`DeletionErasure*` records, `SubmitDeletionErasureRequest`, the five closed-set token classes,
`DeletionErasureSchema`, `DeletionErasurePlanner`, `ErasureProofArtifact`) live in `.Contracts` with no
server/gateway/`ClaimsPrincipal` dependency, exactly like `TenantExportContracts.cs`. The `.Server` internals — the
`DeletionErasureAuthorizationPolicy`, the `DeletionErasureFailureClassifier`, the `DeletionErasureRunner`, and the
auth-stage validator/reader — stay `internal` to `.Server`. NetArchTest fitness (`DependencyDirectionFitnessTests` /
`AdapterBoundaryFitnessTests`) enforces it; `DeletionErasureContracts.cs` is allowlisted in
`ScaffoldArchitectureTests.NonGeneratedChatBotSourceShouldNotHardCodeLegacyLifecycleLiterals` for the bounded
`"succeeded"` status token (the `ApprovalStatus.cs` / `TenantExportContracts.cs` precedent).

## Consequences

- **Deletion/erasure is governed, deletion-behavior-aware, authority-bounded, and audit-defensible** without any second
  authority path, crypto-shred/tombstone mechanism, policy-snapshot record, retry taxonomy, class-id/behavior set, or
  validation-result type. A reviewer diffs the command/allowlist line/gating block/validator/reader/audit block against
  the Story 9.8 export originals line-for-line.
- **GDPR obligations are met without mutating the immutable audit history** — audit-chain erasure delegates to the
  Story 9.1 `AuditRedactionService`, and the nightly `WormAuditChainVerifier` still verifies after erasure.

### Deferrals (explicit)

This story ships the governed decision/recording/proof layer. **Deferred:** (1) the live S-tagged deletion/erasure UI
surface; (2) the storage-layer **non-audit-store destruction runtime** that reaches into each non-audit derived store
(vector indexes, embedding/cache stores, attachment folders, projection stores) and crypto-shreds/tombstones/
hard-deletes its bytes — modeled as documented deferral hooks on `DeletionErasureRunner`. The **audit-chain erasure
path is NOT deferred** — Story 9.1 already implements it and this story wires the workflow to it. The request **is**
governed, the plan **is** behavior/authority bounded, the audit-chain erasure **is** real, and the run **is** audited
with a proof artifact: a deferral never means "deletion is unbounded or untraceable."

## References

- [tenant-export-workflow.md](tenant-export-workflow.md) — the closest structural template (export → destruction sibling)
- [data-class-inventory-and-retention-policy.md](data-class-inventory-and-retention-policy.md) — Story 9.7 inventory + `DataClassDeletionBehaviors`
- [worm-audit-backing.md](worm-audit-backing.md) — Story 9.1 WORM chain + `AuditRedactionService` erasure mechanism
- `_bmad-output/implementation-artifacts/9-9-deletion-and-erasure-workflow.md`
- `_bmad-output/implementation-artifacts/9-3-audit-query-and-compliance-investigation-surface-s9.md` — per-project no-leak
