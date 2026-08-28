---
title: 'Independent governed and WORM rebuild equivalence'
type: 'feature'
created: '2026-08-28'
status: 'in-review'
baseline_revision: efd2d9291dc491338fea2a983c0a6efeea99baa5
baseline_commit: efd2d9291dc491338fea2a983c0a6efeea99baa5
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/docs/adrs/live-recovery-validation-drivers.md'
  - '{project-root}/docs/adrs/projection-rebuild-validation.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The live projection-rebuild lane seeds and rebuilds from the same source/WORM instances, identity-projects governed history on both sides, and discards governed digests. Its equivalence verdict therefore cannot expose governed/WORM divergence or retain evidence supporting that claim (DW-49, DW-53).

**Approach:** Independently materialize pinned seed and rebuild inputs, replay governed WORM history through the production projection handler, compare the complete source-plus-governed snapshot, and retain metadata-only per-resource digests plus canonical snapshot fingerprints for independent gate replay.

## Boundaries & Constraints

**Always:** Keep the immutable source/WORM-only input boundary, replay-test tenant isolation, fresh-partition cleanup semantics, stable ordinal ordering, length-framed SHA-256 evidence, fail-closed unmeasurable/divergent verdicts, and metadata-only artifacts. Preserve remaining `RV-DURABLE-WORM`, provider-scale, hosted-evidence, and measurable-ceiling limitations.

**Block If:** The production handler cannot reconstruct a governed view solely from retained WORM metadata, or retaining mutation-sensitive evidence would require payload/PII exposure or an incompatible artifact contract without a fail-closed migration.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; share seed/rebuild object graphs or WORM stores; call `ToGovernedOperationView` from the rebuild path; re-ingest mail/Graph or query current Party/Folder/sibling state; broaden NFR57/A10 claims beyond the proven sandbox path.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Independent equivalent rebuild | Separately loaded identical pinned datasets and separately appended WORM chains | Full source and governed snapshots match; report retains equal resource digests and manifest fingerprints | No error expected |
| Governed mutation | Rebuild WORM metadata/version differs from the persisted baseline | Verdict is `divergent`, locator identifies the governed resource, retained fingerprints differ | Gate stop-ships the evidence |
| Missing/malformed evidence | Measurable projection manifest lacks canonical fingerprints or claims equivalence with unequal values | Evidence is rejected | Stable projection-specific reason code |
| Reconstruction failure | WORM record cannot be reconstructed/applied through the real handler | No fabricated equivalence; failed fresh partition remains available until evidence capture | `unmeasurable` and alert/audit path |

</intent-contract>

## Code Map

- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs` -- currently shares one `RecoveryValidationDataset.SourceRecords` and `InMemoryWormAuditStore`; independently load/validate/append seed and rebuild inputs here.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver.cs` -- keep the baseline oracle independent from the rebuild algorithm: baseline-only direct seeding may derive the expected governed view from the seed chain, while only the rebuild side may use `AuditOperationReconstructor` plus `GovernedOperationProjectionHandler`. Group WORM envelopes by `(resource id, correlation id)`, replay operations in chain order, and do not equate record count with unique governed-resource count.
- `src/Hexalith.ChatBot.Server/Projections/GovernedOperationProjectionHandler.cs` and `src/Hexalith.ChatBot.Server/Audit/AuditOperationReconstructor.cs` -- production replay/reconstruction reuse points; do not add a parallel mapper.
- `src/Hexalith.ChatBot.Server/Audit/ProjectionRebuildReport.cs` and `ProjectionRebuildValidationCoordinator.cs` -- carry safe snapshots/fingerprints from measurement into retained reports, with empty evidence on unmeasurable results.
- `src/Hexalith.ChatBot.Server/Audit/RecoveryValidationEvidenceManifest.cs` and `LiveRecoveryValidationEvidenceGate.cs` -- projection-only canonical fingerprint contract and fail-closed equality, schema, resource-count, verdict/reason, algorithm-version, malformed-input, and bounded-size checks.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSink.cs` -- serialize report digests and copy snapshot fingerprints into the projection manifest.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriverTests.cs`, `FileRecoveryValidationEvidenceSinkTests.cs`; `tests/Hexalith.ChatBot.Server.Tests/Audit/*ProjectionRebuild*Tests.cs`, `RecoveryValidationEvidenceManifestTests.cs`, `LiveRecoveryValidationEvidenceGateTests.cs`; `tests/Hexalith.ChatBot.Conformance.Tests/Audit/ProjectionRebuildLeakageScanTests.cs` -- behavioral mutation, retention, gate, and leakage proof.
- `docs/adrs/live-recovery-validation-drivers.md`, `docs/adrs/projection-rebuild-validation.md`, `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` -- retire only `RV-REBUILD-WORM` after proof; retain all other limitations and avoid ledger edits.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs` -- create independently loaded seed/rebuild datasets and WORM chains and pass only bounded baseline evidence/locators into the driver -- make live divergence reachable without reference sharing.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver.cs` -- directly seed the governed baseline without using the rebuild helper, but rebuild grouped WORM operations via `AuditOperationReconstructor` plus `GovernedOperationProjectionHandler`; include independently derived WORM structural tokens in governed digests and all safe persisted source structural fields in source digests -- eliminate common-mode derivation and same-resource mutation tautologies.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver.cs` -- distinguish WORM record, operation, and unique resource cardinalities; reject unsafe logical resource ids; detect unexpected persisted records when the store abstraction permits enumeration, otherwise constrain writes and document the exact completeness boundary -- keep valid multi-envelope histories measurable and snapshots complete for every reachable write.
- [x] `src/Hexalith.ChatBot.Server/Audit/ProjectionRebuildReport.cs`, `ProjectionRebuildValidationCoordinator.cs`, `RecoveryValidationEvidenceManifest.cs`, `LiveRecoveryValidationEvidenceGate.cs`, and `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSink.cs` -- retain both schema versions, digest lists, resource counts, fingerprint algorithm version, and canonical fingerprints; bind them to one another and to verdict/reason semantics; reject malformed/null, oversized, unsafe, or noncanonical evidence without throwing -- make independent replay complete and bounded.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriverTests.cs`, `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSinkTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Audit/ProjectionRebuildValidationCoordinatorTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Audit/ProjectionRebuildReportTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Audit/RecoveryValidationEvidenceManifestTests.cs`, `tests/Hexalith.ChatBot.Server.Tests/Audit/LiveRecoveryValidationEvidenceGateTests.cs`, `tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs`, and `tests/Hexalith.ChatBot.Conformance.Tests/Audit/ProjectionRebuildLeakageScanTests.cs` -- cover same-resource structural WORM mutation, multi-envelope operations, multiple operations per governed resource, independent baseline/rebuild runtime behavior, canonical known-answer fingerprinting, count/schema/verdict tampering, malformed null entries, size bounds, serialization compatibility, and leakage -- prove the outer behavior rather than source-text presence alone.
- [x] `docs/adrs/live-recovery-validation-drivers.md`, `docs/adrs/projection-rebuild-validation.md`, and `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` -- describe the independently proven sandbox path and remaining residuals, and remove stale comments that still assign the retired residual to unrelated mailbox-observation limits -- keep release claims accurate.

**Acceptance Criteria:**
- Given separately materialized but equivalent pinned source and WORM inputs, when the live Tier-3 rebuild runs, then the persisted full source-plus-governed snapshot is equivalent and retained report/manifest evidence contains matching canonical digests.
- Given a mutation to the rebuild-side governed/WORM structural state, when validation and independent gate replay run, then the governed resource is reported divergent and the gate stop-ships unequal retained fingerprints.
- Given the resource id and sequence remain unchanged but any safe reconstructed governed field changes, when the rebuild runs, then its governed digest and fingerprint differ from the independently seeded baseline.
- Given a governed operation spans pre/post-commit WORM envelopes or several operations target one governed resource, when the rebuild runs, then envelopes are grouped per operation, operations replay in deterministic chain order, and the dataset remains measurable.
- Given absent, unsafe, or noncanonical projection digest evidence, when artifact validation runs, then it fails closed without exposing tenant payloads.
- Given retained evidence has null digest entries, incompatible schema stamps, inconsistent resource counts, an unsupported fingerprint algorithm, an oversized snapshot, or a verdict/reason mismatch, when artifact validation runs, then it returns stable stop-ship reasons without throwing.
- Given the implementation and documentation diff, when reviewed, then the deferred-work ledger is unchanged and only `RV-REBUILD-WORM` is retired; durable WORM, provider-scale, hosted, and timing limitations remain explicit.

## Spec Change Log

### 2026-08-28 — Review repair iteration 1

- Trigger: the first implementation used the same governed replay helper for baseline and rebuild, discarded reconstructed WORM state beyond the resource id, reconstructed one envelope at a time, and retained evidence that did not bind schema/count/verdict/algorithm facts.
- Amendment: made cross-path derivation independence, operation grouping/cardinality, same-resource mutation sensitivity, complete safe structural coverage, bounded canonical evidence replay, and runtime outer-surface tests explicit in the code map, tasks, acceptance criteria, and design notes.
- Known-bad state avoided: separately allocated inputs whose governed baseline and rebuild still share one transformation path and therefore agree under a common-mode defect.
- KEEP: preserve separately loaded source datasets, separate seed/rebuild WORM stores, production-handler use on the rebuild side, complete source-plus-governed digest retention, length-framed ordinal SHA-256 fingerprints, fail-closed evidence gating, metadata-only leakage discipline, failed-partition capture/cleanup semantics, and the narrowed documentation residuals.

## Review Triage Log

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 12: (high 5, medium 7, low 0)
- patch: 0
- defer: 0
- reject: 1: (high 0, medium 1, low 0)
- addressed_findings:
  - `[high]` `[bad_spec]` Baseline and rebuild shared the governed derivation helper; required a baseline-only oracle path distinct from the rebuild reconstructor/production handler.
  - `[high]` `[bad_spec]` Reconstructed decision/outcome/redaction state was discarded and the mutation test changed only the key; required same-resource structural WORM tokens in governed digests and a mutation-sensitive outer-surface test.
  - `[high]` `[bad_spec]` WORM envelopes were reconstructed individually; required `(resource id, correlation id)` operation grouping and deterministic operation replay.
  - `[high]` `[bad_spec]` Snapshot evidence did not bind retained schema versions, resource counts, and verdict/reason semantics; required those facts in the report/manifest and gate replay.
  - `[high]` `[bad_spec]` Complete-snapshot claims omitted reachable unexpected writes and substantial safe persisted source state; required complete reachable-key accounting and fuller safe structural coverage.
  - `[medium]` `[bad_spec]` WORM record count was incorrectly equated with unique governed resources; required distinct record, operation, and resource cardinalities.
  - `[medium]` `[bad_spec]` Null digest elements could throw before fail-closed validation; required non-throwing malformed-evidence rejection.
  - `[medium]` `[bad_spec]` Unsafe producer resource ids could collapse to the same fallback; required the live producer to reject them rather than authenticate equivalence.
  - `[medium]` `[bad_spec]` Snapshot lists had no explicit evidence-size bound; required bounded retained evidence validation.
  - `[medium]` `[bad_spec]` Fingerprints lacked an algorithm/version contract and known-answer vector; required both for replay stability.
  - `[medium]` `[bad_spec]` Architecture proof was source-text-only; required runtime proof of independent inputs and mutation behavior.
  - `[medium]` `[bad_spec]` Projection verdict/reason combinations were not validated; required projection-specific consistency checks.

## Design Notes

Canonical snapshot fingerprints must carry a stable algorithm/version token, sort by resource id using ordinal comparison, hash length-framed `(resource id, structural token)` pairs, and have a pinned known-answer test. Evidence must be bounded by an explicit resource ceiling and reject null/unsafe/duplicate entries without throwing. Governed WORM envelopes are grouped by `(resource id, correlation id)`; the result-bearing envelope determines the reconstructed state and the group's maximum retained sequence yields the positive source version. Several operations may target one resource. The baseline oracle must not call the rebuild helper, `AuditOperationReconstructor`, or `GovernedOperationProjectionHandler`; only the rebuild side uses those production components. Governed digests combine persisted governed-view structural fields with an independently derived safe WORM-operation-history token so mutations to decision, reason, policy snapshot, transition, outcome, redaction state, or resulting state remain detectable even when the persisted view schema does not expose them directly.

## Verification

**Commands:**
- `UseHexalithProjectReferences=true dotnet test tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj -p:HexalithEventStoreFromSource=true --filter 'FullyQualifiedName~LiveProjectionRebuildDriverTests|FullyQualifiedName~FileRecoveryValidationEvidenceSinkTests'` -- focused live-driver and retention tests pass.
- `UseHexalithProjectReferences=true dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj -p:HexalithEventStoreFromSource=true --filter 'FullyQualifiedName~ProjectionRebuild|FullyQualifiedName~RecoveryValidationEvidenceManifest|FullyQualifiedName~LiveRecoveryValidationEvidenceGate'` -- coordinator/report/manifest/gate tests pass.
- `UseHexalithProjectReferences=true dotnet test tests/Hexalith.ChatBot.Conformance.Tests/Hexalith.ChatBot.Conformance.Tests.csproj -p:HexalithEventStoreFromSource=true --filter FullyQualifiedName~ProjectionRebuildLeakageScanTests` -- retained evidence remains metadata-only.
- `UseHexalithProjectReferences=true dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj -p:HexalithEventStoreFromSource=true --filter FullyQualifiedName~LiveRecoveryValidationArchitectureTests` -- independent-path guard passes.
- `UseHexalithProjectReferences=true dotnet build Hexalith.ChatBot.slnx --no-restore -m:1` -- serialized solution build passes with warnings as errors.
