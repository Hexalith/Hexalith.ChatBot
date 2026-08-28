# Deferred Work

### DW-1: [MEDIUM · PR integration] Exact-head story evidence does not test the GitHub pull-request merge result.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-24, chunk E docs/ADRs/CI)"), 2026-08-26
location: CI architecture
severity: medium
reason: **[MEDIUM · PR integration] Exact-head story evidence does not test the GitHub pull-request merge result.** The evidence producers intentionally check out `github.event.pull_request.head.sha` so provenance binds the proposed source tree, but this replaced the former default merge-ref checkout. **Release-claim impact:** a PR may pass exact-head evidence while failing when merged with the current base. **Owner:** CI architecture. **Closure evidence:** a separate merge-ref integration job (or an equivalent isolated merge validation) that does not contaminate exact-head provenance.
status: done 2026-08-27
resolution: resolved by sweep bundle dw-release-workflow-safety
resolution-undo: 62b87e5b2b0f5cd98de7e31b41ffb34f03070488e12da505081d09776a9959b0 2026-08-27 7374617475733a206f70656e

### DW-2: [MEDIUM · release ordering] Per-commit live-recovery concurrency can release newer and older commits out of order.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-24, chunk E docs/ADRs/CI)"), 2026-08-26
location: release architecture
severity: medium
reason: **[MEDIUM · release ordering] Per-commit live-recovery concurrency can release newer and older commits out of order.** The story deliberately accepts concurrent 5.5-hour validation jobs so GitHub cannot cancel an older pending required check, but `semantic-release` itself has no safe ordering mechanism after those jobs converge at different times. **Release-claim impact:** an older workflow may attempt publication after a newer commit has already released. **Owner:** release architecture. **Closure evidence:** a release-order design that preserves a verdict for every commit without GitHub's one-pending-run cancellation behavior.
status: done 2026-08-27
resolution: resolved by sweep bundle dw-release-workflow-safety
resolution-undo: 62b87e5b2b0f5cd98de7e31b41ffb34f03070488e12da505081d09776a9959b0 2026-08-27 7374617475733a206f70656e
decision: 2026-08-27 Guard latest head — Before publication, fetch main and publish only when the validated SHA is still the newest releasable head; treat an older included SHA as superseded.
decision: 2026-08-26 Guard latest head — Before publication, fetch main and publish only when the validated SHA is still the newest releasable head; treat an older included SHA as superseded.

### DW-3: [LOW · code hygiene] Duplicated read-model-key helpers (`IntakeReadModelKeys`, `AttachmentIndexKeyFor`, `AreIntakeReadModelsAbsentAsync`, `RemainsIntakeReadModelsAbsentAsync`) independently added to both `AspireRecoverySandboxOperations.cs` and `AspireScopedOutageOperations.cs`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
location: AspireRecoverySandboxOperations.cs
severity: low
reason: **[LOW · code hygiene]** Duplicated read-model-key helpers (`IntakeReadModelKeys`, `AttachmentIndexKeyFor`, `AreIntakeReadModelsAbsentAsync`, `RemainsIntakeReadModelsAbsentAsync`) independently added to both `AspireRecoverySandboxOperations.cs` and `AspireScopedOutageOperations.cs`. **Release-claim impact:** none directly; a future read-model key-shape change must be made in two places by hand. **Owner:** ops-harness code hygiene. **Closure evidence:** shared helper extraction.
status: open

### DW-4: [LOW · design invariant] Graph scoped-outage lane hardcodes `RecoveryPhase` notification identity at every call site with no `CheckpointPhase` concept.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
location: ops-harness design guard
severity: low
reason: **[LOW · design invariant]** Graph scoped-outage lane hardcodes `RecoveryPhase` notification identity at every call site with no `CheckpointPhase` concept. **Release-claim impact:** none today — Graph never issues a pre-fault commit notification — but nothing enforces that invariant against a future change. **Owner:** ops-harness design guard. **Closure evidence:** an assertion or comment tying the omission to the absence of a Graph pre-fault checkpoint.
status: done 2026-08-28
resolution: already resolved: Commit 23030e35; tests/Hexalith.ChatBot.RecoverySandbox/RecoveryNotificationIdentity.cs:26-33 permits GraphLane only with RecoveryPhase, while RecoverySandboxContractTests.cs:190-197 rejects unknown lane/phase combinations.

### DW-5: [MEDIUM · cleanup integrity] Cleanup methods only null tracked note/intake refs when `complete=true`, leaving stale refs after a partial cleanup failure; no test exercises "cleanup fails, then the next scenario runs".

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
location: ops-harness cleanup integrity
severity: medium
reason: **[MEDIUM · cleanup integrity]** Cleanup methods only null tracked note/intake refs when `complete=true`, leaving stale refs after a partial cleanup failure; no test exercises "cleanup fails, then the next scenario runs". **Release-claim impact:** possible cross-scenario state contamination after an already-failed run. **Owner:** ops-harness cleanup integrity. **Closure evidence:** a cross-scenario isolation test after an induced cleanup failure.
status: open

### DW-6: [LOW · cleanup integrity] `_checkpointNoteRefs.Add(noteRef)` records a note before its durable-commit wait confirms — intentional per this diff's "preserve cleanup metadata before first mutation" fix, but cleanup's handling of a never-actually-committed ref isn't explicitly tested.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
location: ops-harness cleanup integrity
severity: low
reason: **[LOW · cleanup integrity]** `_checkpointNoteRefs.Add(noteRef)` records a note before its durable-commit wait confirms — intentional per this diff's "preserve cleanup metadata before first mutation" fix, but cleanup's handling of a never-actually-committed ref isn't explicitly tested. **Release-claim impact:** unconfirmed, likely benign. **Owner:** ops-harness cleanup integrity. **Closure evidence:** a test inducing a mid-loop seeding failure and asserting cleanup handles the unconfirmed ref gracefully.
status: open

### DW-7: [MEDIUM · verification strategy] Most of this diff's Aspire ops-harness rewrite (fault/observe/recover/cleanup logic in both `Aspire*Operations` classes) is reachable only through `LiveContinuityAspireE2eTests`, gated behind `HEXALITH_CHATBOT_TIER3=1` and not rerun in this pass — unlike the pieces extracted into static/internal helpers (e.g. `StopReachedDependencyBoundary`), which got real always-run unit tests.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
location: Story 12.15 verification strategy
severity: medium
reason: **[MEDIUM · verification strategy]** Most of this diff's Aspire ops-harness rewrite (fault/observe/recover/cleanup logic in both `Aspire*Operations` classes) is reachable only through `LiveContinuityAspireE2eTests`, gated behind `HEXALITH_CHATBOT_TIER3=1` and not rerun in this pass — unlike the pieces extracted into static/internal helpers (e.g. `StopReachedDependencyBoundary`), which got real always-run unit tests. **Release-claim impact:** today's remediation claims ("N patches applied and verified") rest on build/non-live-suite success, not on the live-path behavior actually changing. **Owner:** Story 12.15 verification strategy. **Closure evidence:** extract more branch-level decision logic into testable static helpers, or rerun the live Tier-3 lane before claiming these fixes proven.
status: done 2026-08-27
resolution: already resolved: Commit ffb62664cd0282d9ac70fad519f190c49e0d7cb9 records the genuine hosted release run 33066358280/evidence 01M11EYSDMP1ZF38B7KZA1A6FA; docs/adrs/continuity-drill-and-rpo-rto-validation.md:39 cites that retained run.

### DW-8: [MEDIUM · NFR41] Identity/Graph scope stamps still minted via post-hoc `/scope-observation`. Reconfirmed on ops-harness chunk.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
location: existing NFR41 / evidence-integrity residual
severity: medium
reason: **[MEDIUM · NFR41]** Identity/Graph scope stamps still minted via post-hoc `/scope-observation`. Reconfirmed on ops-harness chunk. **Release-claim impact:** NFR41 figures remain provisional (sandbox self-timing). **Owner:** existing NFR41 / evidence-integrity residual. **Closure evidence:** honest product monitoring clocks or explicit unmeasurable.
status: open

### DW-9: [MEDIUM · RPO bound] Continuity `LastCommittedAtUtc` is harness `UtcNow` after projection wait, not an EventStore commit stamp. Entangled with green-path Zero-RPO residual.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
location: Story 12.15 RPO measurement honesty
severity: medium
reason: **[MEDIUM · RPO bound]** Continuity `LastCommittedAtUtc` is harness `UtcNow` after projection wait, not an EventStore commit stamp. Entangled with green-path Zero-RPO residual. **Release-claim impact:** loss-path RPO bounds inherit wall-clock distortion. **Owner:** Story 12.15 RPO measurement honesty. **Closure evidence:** durable commit stamp or documented non-citability of harness-clock bounds.
status: open

### DW-10: [LOW · contract tests] Ops-harness contract tests omit route/lane/composer guards.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
location: Story 12.15 tests-contracts or recovery-sandbox follow-up
severity: low
reason: **[LOW · contract tests]** Ops-harness contract tests omit route/lane/composer guards. **Release-claim impact:** regression net for `RecoverySandboxRoute` / topology path is thin. **Owner:** Story 12.15 tests-contracts or recovery-sandbox follow-up. **Closure evidence:** focused negatives for lane headers, flat restore rejection, composer path resolution.
status: open

### DW-11: [LOW · topology path] `RecoverySandboxTopologyComposer` uses brittle relative `../../tests/...` from AppHost.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
location: AppHost/topology hygiene
severity: low
reason: **[LOW · topology path]** `RecoverySandboxTopologyComposer` uses brittle relative `../../tests/...` from AppHost. **Release-claim impact:** AppHost moves break composition silently. **Owner:** AppHost/topology hygiene. **Closure evidence:** project-reference or content-rooted path.
status: open

### DW-12: [LOW · test identity] Recovery validator passwords duplicated in token provider and Identity probe.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
location: ops-harness test-identity consolidation
severity: low
reason: **[LOW · test identity]** Recovery validator passwords duplicated in token provider and Identity probe. **Release-claim impact:** credential rotation desyncs Identity observation from minting. **Owner:** ops-harness test-identity consolidation. **Closure evidence:** single shared test identity source.
status: open

### DW-13: [HIGH · `RV-REBUILD-WORM`] Projection rebuild still identity-writes WORM/governed views and excludes them via `SourceDigestsOnly`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
location: existing `RV-REBUILD-WORM`
severity: high
reason: **[HIGH · `RV-REBUILD-WORM`] Projection rebuild still identity-writes WORM/governed views and excludes them via `SourceDigestsOnly`.** Reconfirmed on live-drivers chunk. **Release-claim impact:** do not claim full immutable-source+WORM rebuild equivalence. **Owner:** existing `RV-REBUILD-WORM`. **Closure evidence:** per existing residual.
status: done 2026-08-28
resolution: already resolved: Commit b57f02e1; tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver.cs:336-365 reconstructs WORM operations through GovernedOperationProjectionHandler, and docs/adrs/projection-rebuild-validation.md:62 retires RV-REBUILD-WORM for the bounded sandbox path.

### DW-14: [HIGH · `RV-REBUILD-WORM`] Governed/WORM rebuild cannot diverge.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
location: existing `RV-REBUILD-WORM`
severity: high
reason: **[HIGH · `RV-REBUILD-WORM`] Governed/WORM rebuild cannot diverge.** Source-email reconstruction now runs through the real `AssociationProjectionHandler`, so that part can diverge; governed/WORM views still identity-write from the same records on both sides. **Release-claim impact:** full immutable-source-plus-WORM divergence remains unreachable on the live lane. **Owner:** existing `RV-REBUILD-WORM`. **Closure evidence:** per existing residual.
status: done 2026-08-28
resolution: already resolved: Commit b57f02e1; tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver.cs:430-474 independently rebuilds governed snapshots from grouped WORM history, and LiveProjectionRebuildDriverTests.cs:213-244 proves structural mutation changes the digest.

### DW-15: [HIGH · `RV-MEASURABLE-CEILING`] E2E `RestorationTimeout = 3 minutes` vs A10/NFR56 `MaxRto` 4 hours.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
location: Architecture/DevOps
severity: high
reason: **[HIGH · `RV-MEASURABLE-CEILING`] E2E `RestorationTimeout = 3 minutes` vs A10/NFR56 `MaxRto` 4 hours.** Reconfirmed. **Release-claim impact:** do not cite this lane as confirming RTO ≤ 4 hr. **Owner:** Architecture/DevOps. **Closure evidence:** per existing entry.
status: open

### DW-16: [MEDIUM · NFR41] Scoped-outage missing/degenerate monitoring → driver unmeasurable conversion still open. Ops reject bad stamps; driver path to audited unmeasurable remains.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
location: Story 12.15 evidence-integrity
severity: medium
reason: **[MEDIUM · NFR41]** Scoped-outage missing/degenerate monitoring → driver unmeasurable conversion still open.** Ops reject bad stamps; driver path to audited unmeasurable remains. **Release-claim impact:** NFR41 figures remain provisional. **Owner:** Story 12.15 evidence-integrity. **Closure evidence:** per existing ledger entry.
status: done 2026-08-27
resolution: already resolved: tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs:260-263,300-303 rejects degenerate stamps, and src/Hexalith.ChatBot.Server/Audit/ScopedOutageDegradationValidationCoordinator.cs:169-205 converts non-cancellation driver failures into retained audited unmeasurable reports.

### DW-17: [MEDIUM · RPO semantics] Green-path RPO hard-coded `TimeSpan.Zero` when no loss detected.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
location: governance / A10
severity: medium
reason: **[MEDIUM · RPO semantics] Green-path RPO hard-coded `TimeSpan.Zero` when no loss detected.** Intentional documented classic-RPO empty loss window. **Release-claim impact:** do not cite green-path Zero as A10 budget exercise. **Owner:** governance / A10. **Closure evidence:** loss-path measurement + hosted locator, or documented non-citability.
status: open

### DW-18: [MEDIUM · dataset] ~~`DatasetVolume: 6` vs actual compared resources.~~ Closed 2026-08-04.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
location: n/a
severity: medium
reason: **[MEDIUM · dataset] ~~`DatasetVolume: 6` vs actual compared resources.~~ Closed 2026-08-04.** Manifests now separate configured corpus volume from per-scenario exercised volume; the canonical closure record and remaining scale limitation are detailed in the `RV-PROVIDER-SCALE` entry below.
status: done 2026-08-26
resolution: already resolved: src/Hexalith.ChatBot.Server/Audit/RecoveryValidationEvidenceManifest.cs:37-44 distinguishes configured and exercised volumes; tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSink.cs:56-60,88-89,142-146 emits 0 for non-dataset lanes and measured rebuild coverage.

### DW-19: [LOW · rebuild] `ReconstructCapturedEvent` placeholders outside structural digest tuple.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
location: `RV-REBUILD-WORM` / rebuild residual
severity: low
reason: **[LOW · rebuild] `ReconstructCapturedEvent` placeholders outside structural digest tuple.** Claim-narrowed. **Release-claim impact:** handler regressions outside digest fields stay invisible. **Owner:** `RV-REBUILD-WORM` / rebuild residual. **Closure evidence:** richer round-trip or explicit non-claim.
status: done 2026-08-28
resolution: already resolved: Commit b57f02e1; tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver.cs:369-398 limits placeholders to data never retained by the metadata-only view, while :477-515 digests all safely retained source and governed structure.

### DW-20: [MEDIUM · gate producer] In-process gate evaluation remains smoke while gate trusts producer summary fields.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
location: existing gate producer-trust residual
severity: medium
reason: **[MEDIUM · gate producer] In-process gate evaluation remains smoke while gate trusts producer summary fields.** Reconfirmed. **Release-claim impact:** buggy producer can still influence completion grading. **Owner:** existing gate producer-trust residual. **Closure evidence:** per existing entry.
status: open

### DW-21: [MEDIUM · evidence sink] Double-fail `RetainAsync` returns unmeasurable with no disk artifact.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk server)"), 2026-08-26
location: Story 12.15 evidence sink
severity: medium
reason: **[MEDIUM · evidence sink] Double-fail `RetainAsync` returns unmeasurable with no disk artifact.** Caller gets `EvidenceRetentionFailedDeviation`; gate sees `missing_evidence`. **Release-claim impact:** retention-failure reason is not reconstructable from artifacts alone. **Owner:** Story 12.15 evidence sink. **Closure evidence:** best-effort side channel, or accept and document that missing_evidence covers total sink loss.
status: done 2026-08-28
resolution: resolved by sweep bundle dw-retention-failure-marker
resolution-undo: b71f090ec811d8068dd7424e412d83c32baf6450975c0a62f208b21297d18f7d 2026-08-28 7374617475733a206f70656e
decision: 2026-08-27 Write fallback marker — Emit a bounded metadata-only retention-failure sentinel through an independent workflow-owned path and teach the gate to distinguish total sink loss.
decision: 2026-08-26 Write fallback marker — Emit a bounded metadata-only retention-failure sentinel through an independent workflow-owned path and teach the gate to distinguish total sink loss.

### DW-22: [LOW · options] `Enabled = false` skips environment/tenant/secret/path validation.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk server)"), 2026-08-26
location: Story 12.15 options hygiene
severity: low
reason: **[LOW · options] `Enabled = false` skips environment/tenant/secret/path validation.** Dormant Production-shaped overlays are not rejected until enablement. **Release-claim impact:** config mistakes surface only when the lane is armed. **Owner:** Story 12.15 options hygiene. **Closure evidence:** validate sandbox shape even when disabled, or document intentional skip.
status: open
decision: 2026-08-27 Validate dormant shape — Validate non-secret sandbox, environment, tenant, and path shape while disabled, retain activation-only secret checks, and add disabled-overlay tests.
decision: 2026-08-27 Validate dormant shape — Validate non-secret sandbox, environment, tenant, and path shape while disabled, retain activation-only secret checks, and add disabled-overlay tests.

### DW-23: [LOW · sanitization] `IsSafeArtifactLocator` rejects substrings `token`/`secret`/`password`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk server)"), 2026-08-26
location: evidence manifest sanitization
severity: low
reason: **[LOW · sanitization] `IsSafeArtifactLocator` rejects substrings `token`/`secret`/`password`.** Valid artifact URIs with those substrings fail closed. **Release-claim impact:** rare false stop-ship on locator naming. **Owner:** evidence manifest sanitization. **Closure evidence:** path-segment allowlist or documented substring policy.
status: open
decision: 2026-08-27 Use segment-aware policy — Parse the artifact URI, reject bounded sensitive path segments or keys, accept benign substring occurrences, and add positive and negative tests.
decision: 2026-08-27 Use segment-aware policy — Parse the artifact URI, reject bounded sensitive path segments or keys, accept benign substring occurrences, and add positive and negative tests.

### DW-24: [LOW · gate fixtures] Gate fixtures publish `rpo: 0` under `met` without asserting non-citability of constant-zero RPO.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 4+5+5b)"), 2026-08-26
location: Story 12.15 governance / RPO residual
severity: low
reason: **[LOW · gate fixtures] Gate fixtures publish `rpo: 0` under `met` without asserting non-citability of constant-zero RPO.** **Release-claim impact:** unit fixtures mirror the no-loss shape; they do not launder A10. **Owner:** Story 12.15 governance / RPO residual. **Closure evidence:** existing no-loss RPO residual closure, or fixtures that mark Zero-RPO as non-citable.
status: open

### DW-25: [LOW · architecture methodology] `AppHostTopologyTests` StrictEnum/sidecar assertions are source-substring greps.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 4+5+5b)"), 2026-08-26
location: architecture-test hardening
severity: low
reason: **[LOW · architecture methodology] `AppHostTopologyTests` StrictEnum/sidecar assertions are source-substring greps.** **Release-claim impact:** formatting-only edits can fail CI; comments can falsely pass. **Owner:** architecture-test hardening. **Closure evidence:** behaviour-level guards where feasible.
status: open

### DW-26: [LOW · ReplayTenantPolicy] Control-name hole `replay-test:tenant-alpha` → physical `tenant-alpha` remains untested in SweepVocabularyTests.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 4+5+5b)"), 2026-08-26
location: Server tenant policy / live-recovery topology
severity: low
reason: **[LOW · ReplayTenantPolicy] Control-name hole `replay-test:tenant-alpha` → physical `tenant-alpha` remains untested in SweepVocabularyTests.** **Release-claim impact:** none while topology hard-codes `replay-test:recovery-validation`; risk if a future caller-influenced label is added. **Owner:** Server tenant policy / live-recovery topology. **Closure evidence:** explicit exclusion test or policy deny-list for control tenants.
status: done 2026-08-26
resolution: already resolved: tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoverySandboxTopologyComposer.cs:41-46 rejects control-tenant aliases, exercised for replay-test:tenant-alpha/beta at RecoveryValidationTopologyContractTests.cs:165-190.

### DW-27: [MEDIUM · hygiene D12] ~~EventStore gitlink disclosure was stale.~~ Closed 2026-08-24.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 4+5+5b)"), 2026-08-26
location: n/a
severity: medium
reason: **[MEDIUM · hygiene D12] ~~EventStore gitlink disclosure was stale.~~ Closed 2026-08-24.** The story now records the current superproject gitlink `6b9b349aded823473a14ceb67e26805a3fb40fd8`; no submodule checkout, update, or staging was performed in the remediation pass. (Re-opened and re-closed 2026-08-25: the first closure recorded `da52e2c8…`, which `bab0218` had already superseded in the same commit that wrote the claim.)
status: done 2026-08-26
resolution: already resolved: _bmad-output/implementation-artifacts/12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10.md:2009 explicitly reclassifies recorded gitlink SHAs as point-in-time observations rather than standing current-head claims.

### DW-28: [LOW · platform] Windows skips symlink-ancestry / immutable digest / executable-bit gate tests.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3cd)"), 2026-08-26
location: Story 12.15 follow-up or Windows runner job
severity: low
reason: **[LOW · platform] Windows skips symlink-ancestry / immutable digest / executable-bit gate tests.** **Release-claim impact:** those fail-closed paths are unverified on Windows CI hosts. **Owner:** Story 12.15 follow-up or Windows runner job. **Closure evidence:** compensating Windows coverage or documented Linux-only gate CI.
status: open

### DW-29: [LOW · hardening] Unbounded JSON/TRX/scope file reads and JSON depth.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3cd)"), 2026-08-26
location: StoryEvidenceGate hardening
severity: low
reason: **[LOW · hardening] Unbounded JSON/TRX/scope file reads and JSON depth.** **Release-claim impact:** pathological inputs can OOM/crash instead of stable fail-closed. **Owner:** StoryEvidenceGate hardening. **Closure evidence:** size/depth caps with negative tests.
status: open

### DW-30: [LOW · reason taxonomy] Broad reuse of `scope_digest_mismatch`/`status_mismatch` for I/O and CLI errors via subjects.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3cd)"), 2026-08-26
location: TE-2/policy if reason set expands
severity: low
reason: **[LOW · reason taxonomy] Broad reuse of `scope_digest_mismatch`/`status_mismatch` for I/O and CLI errors via subjects.** **Release-claim impact:** dashboards keyed only on reason codes blur failure classes. **Owner:** TE-2/policy if reason set expands. **Closure evidence:** new stable reasons or documented subject convention.
status: open

### DW-31: [LOW · test harness] `GateFixture.FindPolicy` parent-walk discovery.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3cd)"), 2026-08-26
location: gate test harness
severity: low
reason: **[LOW · test harness] `GateFixture.FindPolicy` parent-walk discovery.** **Release-claim impact:** opaque failure if test output leaves the repo tree. **Owner:** gate test harness. **Closure evidence:** embed/copy policy as a test asset or pin via env.
status: open

### DW-32: [MEDIUM · story-evidence bash] Fail-closed bash paths and event bounds lack executable negative coverage.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3ab)"), 2026-08-26
location: extracted script tests or follow-up
severity: medium
reason: **[MEDIUM · story-evidence bash] Fail-closed bash paths and event bounds lack executable negative coverage.** **Partial close 2026-08-24 (chunk E):** the workflow now rejects an unavailable non-zero push base, uses an empty range for schedule/manual events, recursively discovers test projects, and fails on zero discovery; policy and architecture guards pin those branches. **Still open:** executable PR/normal-push/zero-base/unavailable-base/checkout-mismatch and unsafe-locator tests rather than source-text assertions. **Owner:** extracted script tests or follow-up. **Closure evidence:** BATS/script unit tests for the remaining bash guards.
status: open

### DW-33: [LOW · story-evidence policy] `aspire-dapr` pathPatterns are repo-wide `*Dapr*`/`*Aspire*` globs.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3ab)"), 2026-08-26
location: tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs
severity: low
reason: **[LOW · story-evidence policy] `aspire-dapr` pathPatterns are repo-wide `*Dapr*`/`*Aspire*` globs.** ~~Open~~ **Closed 2026-08-03 (chunk 3cd):** narrowed to `src/Hexalith.ChatBot.AppHost/**` + `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs`; validator pin + architecture assertions updated; Server `*Dapr*` no longer triggers aspire-dapr primary.
status: done 2026-08-26
resolution: already resolved: story-evidence-policy.json:148-152 now limits aspire-dapr paths to src/Hexalith.ChatBot.AppHost/** and TrivialGovernedCommandAspireE2ETests.cs.

### DW-34: [LOW · architecture methodology] ADR prose and substring greps remain the Task 8 methodology.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3ab)"), 2026-08-26
location: architecture-test hardening (follow-on)
severity: low
reason: **[LOW · architecture methodology] ADR prose and substring greps remain the Task 8 methodology.** **Release-claim impact:** doc rewording can fail CI while aliased authority leaks pass. **Owner:** architecture-test hardening (follow-on). **Closure evidence:** behaviour-level guards where feasible.
status: open

### DW-35: [LOW · live-recovery checkout] Scheduled live-recovery jobs omit exact-head / producer-head sidecars.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3ab)"), 2026-08-26
location: Story 12.15 workflows hygiene if parity is required
severity: low
reason: **[LOW · live-recovery checkout] Scheduled live-recovery jobs omit exact-head / producer-head sidecars.** Gate still pins `REQUIRED_COMMIT` to `github.sha`. **Release-claim impact:** weaker provenance ceremony than story-evidence lane. **Owner:** Story 12.15 workflows hygiene if parity is required. **Closure evidence:** same head-binding sidecars as build/topology or documented intentional difference.
status: done 2026-08-27
resolution: already resolved: tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryRepositoryCommitResolver.cs:15-24 binds evidence to GITHUB_SHA or exact HEAD, and src/Hexalith.ChatBot.Server/Audit/LiveRecoveryValidationEvidenceGate.cs:161-165 rejects a manifest that differs from the workflow-pinned commit.

### DW-36: [MEDIUM · gate producer summary] Gate still trusts producer-written `LatestAttemptCompletedSuccessfully` and `AlertsDeliveredByJob`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 2)"), 2026-08-26
location: attempt.summary.json
severity: medium
reason: **[MEDIUM · gate producer summary] Gate still trusts producer-written `LatestAttemptCompletedSuccessfully` and `AlertsDeliveredByJob`.** Known round-5 design: the out-of-process gate job still reads those fields from `attempt.summary.json`. **Release-claim impact:** a buggy producer can still influence completion/alert grading for those two inputs. **Owner:** Story 12.15 workflows/CI (group 3) if further independence is required. **Closure evidence:** derive completion/alerts from retained manifests alone, or accept and document the residual.
status: open

### DW-37: [LOW · AppHost cleanup] Temp realm cleanup is only on `AppDomain.ProcessExit`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 2)"), 2026-08-26
location: AppHost hygiene
severity: low
reason: **[LOW · AppHost cleanup] Temp realm cleanup is only on `AppDomain.ProcessExit`.** Kill/crash can leave the secret-bearing realm under the shared temp root. **Release-claim impact:** local shared-host residual only. **Owner:** AppHost hygiene. **Closure evidence:** `IHostApplicationLifetime` / dispose path that always deletes the generated directory.
status: open

### DW-38: [LOW · sandbox fixture] Committed Keycloak realm includes `recovery-validator` / `recovery-validator-pass`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 2)"), 2026-08-26
location: AppHost/Keycloak fixture hygiene if credentials must rotate per run
severity: low
reason: **[LOW · sandbox fixture] Committed Keycloak realm includes `recovery-validator` / `recovery-validator-pass`.** Same class as other local realm test users. **Release-claim impact:** none for production (sandbox realm only). **Owner:** AppHost/Keycloak fixture hygiene if credentials must rotate per run. **Closure evidence:** generated-at-runtime password or documented fixture residual in Completion Notes.
status: open

### DW-39: [MEDIUM · `RV-PROVIDER-SCALE`] ~~`datasetVolume: 6` still overstates materialized rebuild inputs.~~ Closed 2026-08-04.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1d)"), 2026-08-26
location: n/a
severity: medium
reason: **[MEDIUM · `RV-PROVIDER-SCALE`] ~~`datasetVolume: 6` still overstates materialized rebuild inputs.~~ Closed 2026-08-04.** Manifests now separate `configuredDatasetVolume` (the six-record corpus provenance anchored by release policy) from per-scenario `datasetVolume`: continuity and scoped-outage evidence report zero because they do not consult the corpus, while projection rebuild reports its compared-resource count. The gate rejects non-zero dataset volume for non-dataset jobs and a rebuild volume that differs from its measured coverage. The broader provider/scale residual remains open below.
status: done 2026-08-26
resolution: already resolved: src/Hexalith.ChatBot.Server/Audit/LiveRecoveryValidationEvidenceGate.cs:399-409 verifies rebuild volume against measured coverage and requires zero for non-dataset jobs.

### DW-40: [MEDIUM · `RV-EVIDENCE-KINDS`] Missing logs/traces/metrics/state-end-state evidence kinds remain a platform residual.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1d)"), 2026-08-26
location: Architecture/DevOps with observability
severity: medium
reason: **[MEDIUM · `RV-EVIDENCE-KINDS`] Missing logs/traces/metrics/state-end-state evidence kinds remain a platform residual.** Chunk 1d owes only stamping `RV-EVIDENCE-KINDS` on manifests (patch); producing the artifacts stays deferred. **Release-claim impact:** as on the existing `RV-EVIDENCE-KINDS` entry. **Owner:** Architecture/DevOps with observability. **Closure evidence:** per existing residual.
status: open

### DW-41: [LOW · Aspire/E2E options] `EventStoreDurableStateProbe` uses fixed 30s/250ms/10s bounds unbound from `LiveRecoveryValidationOptions.RestorationTimeout`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1d)"), 2026-08-26
location: Story 12.15 Aspire/E2E options wiring
severity: low
reason: **[LOW · Aspire/E2E options] `EventStoreDurableStateProbe` uses fixed 30s/250ms/10s bounds unbound from `LiveRecoveryValidationOptions.RestorationTimeout`.** **Release-claim impact:** harness and probe can disagree on materialization deadlines. **Owner:** Story 12.15 Aspire/E2E options wiring. **Closure evidence:** shared bound or explicit probe options from the live options object.
status: open

### DW-42: [LOW · continuity end-state] Actor-state probe only checks `events:1` + `MailboxMessageIntakeCaptured` suffix.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1d)"), 2026-08-26
location: continuity Aspire ops / end-state assertions
severity: low
reason: **[LOW · continuity end-state] Actor-state probe only checks `events:1` + `MailboxMessageIntakeCaptured` suffix.** Higher sequences / alternate event type names remain unproven by this helper. **Release-claim impact:** committed state without event `1` can look absent. **Owner:** continuity Aspire ops / end-state assertions. **Closure evidence:** sequence-aware probe or consumer assertions covering the witnessed event stream.
status: done 2026-08-28
resolution: already resolved: Commit 23030e35; tests/Hexalith.ChatBot.IntegrationTests/Recovery/EventStoreDurableStateProbeTests.cs:181-200 proves the authoritative first event remains valid when metadata advances to sequence 2, while EventStoreDurableStateProbe.cs:277-303 validates metadata/event coherence.

### DW-43: [MEDIUM · `RV-PROVIDER-SCALE`] NFR59 unauthorized-mutation / cross-tenant flags remain structurally unreachable for the four sandbox deps.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1c)"), 2026-08-26
location: provider-integration / `RV-PROVIDER-SCALE`
severity: medium
reason: **[MEDIUM · `RV-PROVIDER-SCALE`] NFR59 unauthorized-mutation / cross-tenant flags remain structurally unreachable for the four sandbox deps.** Fault/commit pairs are mutually exclusive by stub design; foreign-tenant effect writes are blocked by `Authorized`. Already covered by chunk 1b decision 2. **Release-claim impact:** do not treat sandbox Contained NFR59 flags as product isolation proof. **Owner:** provider-integration / `RV-PROVIDER-SCALE`. **Closure evidence:** per existing residual.
status: open

### DW-44: [MEDIUM · `RV-PROVIDER-SCALE`] NFR58 `scope_escape` remains unreachable on the four-dep happy path.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1c)"), 2026-08-26
location: `RV-PROVIDER-SCALE`
severity: medium
reason: **[MEDIUM · `RV-PROVIDER-SCALE`] NFR58 `scope_escape` remains unreachable on the four-dep happy path.** Exercises emit the canonical signal; the monitor maps that signal to the ExpectedScope value. **Release-claim impact:** sandbox Contained must not be cited as product scope-containment proof. **Owner:** `RV-PROVIDER-SCALE`. **Closure evidence:** per existing residual.
status: open

### DW-45: [MEDIUM · chunk 1a/1b carry] NFR41 stamps are sandbox self-timing (control POST / `UtcNow` + 200 ms poll), not product monitoring evidence.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1c)"), 2026-08-26
location: Story 12.15 evidence-integrity (driver + monitor)
severity: medium
reason: **[MEDIUM · chunk 1a/1b carry] NFR41 stamps are sandbox self-timing (control POST / `UtcNow` + 200 ms poll), not product monitoring evidence.** Continues the open driver/monitor honesty residual. **Release-claim impact:** NFR41 figures remain provisional. **Owner:** Story 12.15 evidence-integrity (driver + monitor). **Closure evidence:** honest observation clocks or explicit unmeasurable when product monitoring is absent.
status: open

### DW-46: [LOW · Aspire continuity ops] Subscription simulator restore does not reset absolute processing counters.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1c)"), 2026-08-26
location: continuity Aspire ops / remaining 1b follow-ups
severity: low
reason: **[LOW · Aspire continuity ops] Subscription simulator restore does not reset absolute processing counters.** Consumer must use deltas/checkpoints; not closed by sandbox Restore alone. **Release-claim impact:** absolute-counter reconciliation can still fabricate duplicates across retries if ops regress. **Owner:** continuity Aspire ops / remaining 1b follow-ups. **Closure evidence:** ops tests that assert delta-based reconciliation across restore cycles.
status: open

### DW-47: [MEDIUM · `RV-PROVIDER-SCALE` · decision 2] Four generic scoped deps remain sandbox-contract Contained.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1b)"), 2026-08-26
location: performance/capacity and provider-integration owners
severity: medium
reason: **[MEDIUM · `RV-PROVIDER-SCALE` · decision 2] Four generic scoped deps remain sandbox-contract Contained.** Administrator chose option 2 (2026-08-03): keep provisional-live sandbox evidence; claim narrowing recorded on the existing `RV-PROVIDER-SCALE` entry (see 2026-08-03 update). **Release-claim impact:** as on that entry. **Owner:** performance/capacity and provider-integration owners. **Closure evidence:** product-composed client paths for the four tokens under the same gate.
status: open

### DW-48: [HIGH · `RV-MEASURABLE-CEILING`] Tier-3 E2E sets `RestorationTimeout = 3 minutes` while A10/NFR56 `MaxRto` remains 4 hours.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1b)"), 2026-08-26
location: Architecture/DevOps
severity: high
reason: **[HIGH · `RV-MEASURABLE-CEILING`] Tier-3 E2E sets `RestorationTimeout = 3 minutes` while A10/NFR56 `MaxRto` remains 4 hours.** Already tracked as `RV-MEASURABLE-CEILING`; reconfirmed in chunk 1b against `LiveContinuityAspireE2eTests`. A recovery between 3m and 4h becomes `unmeasurable`, never a measurable miss. **Release-claim impact:** do not cite this lane as confirming RTO ≤ 4 hr. **Owner:** Architecture/DevOps. **Closure evidence:** per existing `RV-MEASURABLE-CEILING` entry.
status: open

### DW-49: [HIGH · `RV-REBUILD-WORM`] Live E2E still passes the same `SourceRecords` instance to seed and rebuild driver.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1b)"), 2026-08-26
location: Story 12.15 rebuild residual / `RV-REBUILD-WORM`
severity: high
reason: **[HIGH · `RV-REBUILD-WORM`] Live E2E still passes the same `SourceRecords` instance to seed and rebuild driver.** Claim-narrowed in chunk 1a; divergence remains unreachable on the live lane. **Release-claim impact:** do not claim non-tautological WORM/full rebuild equivalence from this wiring. **Owner:** Story 12.15 rebuild residual / `RV-REBUILD-WORM`. **Closure evidence:** per existing residual.
status: done 2026-08-28
resolution: already resolved: Commit b57f02e1; tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:294-326 independently loads distinct seed and rebuild datasets, WORM stores, and SourceRecords collections.

### DW-50: [MEDIUM · workflows chunk / group 3] Independent `live-recovery-evidence-gate` job is skipped when validation fails.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1b)"), 2026-08-26
location: n/a
severity: medium
reason: **[MEDIUM · workflows chunk / group 3] Independent `live-recovery-evidence-gate` job is skipped when validation fails.** ~~Open~~ **Closed 2026-08-03 (chunk 3ab):** both workflows now use `if: always() && needs.live-recovery-validation.result != 'skipped'`; architecture pins the condition. Prior text: `needs: live-recovery-validation` without failure-path `if` meant a Contained-breach that fails the producing test never reached gate replay.
status: done 2026-08-26
resolution: already resolved: .github/workflows/ci.yml:574 and .github/workflows/release.yml:197 run the evidence gate with `if: always() && needs.live-recovery-validation.result != 'skipped'`.

### DW-51: [MEDIUM · chunk 1b/1c] Missing/degenerate scope-monitoring evidence cannot be fail-closed from the live scoped-outage driver alone.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-02, chunk 1a)"), 2026-08-26
location: Story 12.15 remaining evidence-integrity work (driver + 1c)
severity: medium
reason: **[MEDIUM · chunk 1b/1c] Missing/degenerate scope-monitoring evidence cannot be fail-closed from the live scoped-outage driver alone.** `LiveScopedOutageInjectionDriver` always computes latency from the two stamps on `ScopedOutageFaultObservation`; Task 5 requires missing monitoring to be `unmeasurable`, not zero-ms. **Update 2026-08-03 (chunk 1b):** Aspire ops now reject missing/default/`Tenant`-fallback and degenerate (`recordedAtUtc <= observedAtUtc`) stamps with `InvalidOperationException`, so fabricated zero-ms latency can no longer leave ObserveFault. Remaining gap: the driver/coordinator path that converts that throw into an audited `unmeasurable` (vs a hard scenario failure), plus RecoverySandbox monitor edge cases (chunk 1c). **Release-claim impact:** NFR41 figures remain provisional until driver unmeasurable conversion and monitor honesty are closed. **Owner:** Story 12.15 remaining evidence-integrity work (driver + 1c). **Closure evidence:** ops/monitor path that can omit or invalidate stamps, plus a driver/coordinator path that converts that into `unmeasurable` with a unit/live fixture.
status: done 2026-08-28
resolution: already resolved: tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs:827-854 rejects missing or degenerate monitoring stamps, and src/Hexalith.ChatBot.Server/Audit/ScopedOutageDegradationValidationCoordinator.cs:193-228 retains non-cancellation failures as audited unmeasurable reports.

### DW-52: [HIGH · governance + residual] No-loss continuity RPO stays `TimeSpan.Zero` by decision (2026-08-02 option 2).

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-02, chunk 1a)"), 2026-08-26
location: Story 12.15 governance chunk (ADR/PRD/addendum/Completion Notes wording) + Architecture for any future loss-injection drill
severity: high
reason: **[HIGH · governance + residual] No-loss continuity RPO stays `TimeSpan.Zero` by decision (2026-08-02 option 2).** Passing-run `measuredRpo: 0s` must not be cited as A10 confirmation of RPO ≤ 15 minutes. **Release-claim impact:** A10 RPO half remains unproven by green sandbox runs until a loss-path measurement or a separately owned drill is retained. **Owner:** Story 12.15 governance chunk (ADR/PRD/addendum/Completion Notes wording) + Architecture for any future loss-injection drill. **Closure evidence:** documents stop citing green-run `0s` as A10 proof; optional retained loss-path RPO evidence against the 15-minute budget.
status: done 2026-08-28
resolution: resolved by sweep bundle dw-loss-path-rpo-evidence
resolution-undo: 4be3e8d5ebe7beed69bd12b04996eb96a1f527879240e2ec3555232d1c620697 2026-08-28 7374617475733a206f70656e
decision: 2026-08-27 Build loss-path drill — Add a controlled retained loss-injection scenario with durable commit bounds and gate evidence against the 15-minute target.
decision: 2026-08-26 Build loss-path drill — Add a controlled retained loss-injection scenario with durable commit bounds and gate evidence against the 15-minute target.

### DW-53: [HIGH · `RV-REBUILD-WORM` · WORM/governed rebuild fidelity] Projection rebuild claim-narrowing (2026-08-02 option 2) leaves WORM/governed resources off the proven equivalence path.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-02, chunk 1a)"), 2026-08-26
location: Story 12.15 remaining rebuild work, coordinated with `RV-DURABLE-WORM`
severity: high
reason: **[HIGH · `RV-REBUILD-WORM` · WORM/governed rebuild fidelity] Projection rebuild claim-narrowing (2026-08-02 option 2) leaves WORM/governed resources off the proven equivalence path.** Only `AssociationProjectionHandler` source-email rebuild is claimed; WORM still identity-writes via `ToGovernedOperationView` on both seed and rebuild. **Release-claim impact:** do not claim full immutable-source+WORM rebuild equivalence or NFR57 coverage for audit-derived projections. **Owner:** Story 12.15 remaining rebuild work, coordinated with `RV-DURABLE-WORM`. **Closure evidence:** a rebuild path for governed/WORM projections that can diverge, with retained digests and tests proving non-tautological equivalence.
status: done 2026-08-28
resolution: already resolved: Commit b57f02e1; tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver.cs:337-366 reconstructs WORM operations through GovernedOperationProjectionHandler, while LiveProjectionRebuildDriverTests.cs:213-248 proves a structural mutation produces a divergent governed digest.
decision: 2026-08-27 Build independent rebuild — Implement an independently derived governed and WORM rebuild path that can diverge, include its digests, and add mutation-sensitive tests.
decision: 2026-08-26 Build independent rebuild — Implement an independently derived governed and WORM rebuild path that can diverge, include its digests, and add mutation-sensitive tests.

### DW-54: [HIGH · `RV-EXT-M365` · external M365/Graph fidelity] The live subscription and Graph scenarios use the topology-composed Worker/provider simulator, not Microsoft Graph.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: M365 integration / production validation owner
severity: high
reason: **[HIGH · `RV-EXT-M365` · external M365/Graph fidelity] The live subscription and Graph scenarios use the topology-composed Worker/provider simulator, not Microsoft Graph.** The Story 12.15 implementation exercises `GraphMailboxIntakeWorker`, expired-subscription recovery, independent DAPR read-model sentinels, and EventStore actor-state end-state assertions. Retained hosted evidence for this simulator-backed exercise now exists — see release run `33066358280` / evidence `01M11EYSDMP1ZF38B7KZA1A6FA` (2026-08-27) in `.decision-log.md` — but it does not, and cannot, prove production Graph behavior; this residual is about fidelity to the real Microsoft Graph service, not about whether a hosted run occurred. **Release-claim impact:** A10 remains provisional; no sandbox result may claim external-M365 disaster-recovery equivalence. **Owner:** M365 integration / production validation owner. **Closure evidence:** a retained production-shaped pre-production drill against a real Graph test tenant that exercises subscription expiry/renewal, webhook/reconciliation, throttling, permissions, and end-state assertions through the same fail-closed evidence gate.
status: open
decision: 2026-08-27 Build Graph drill — Create an approved test-tenant drill covering subscription expiry, renewal, webhook reconciliation, throttling, permissions, and retained end-state evidence.
decision: 2026-08-26 Build Graph drill — Create an approved test-tenant drill covering subscription expiry, renewal, webhook reconciliation, throttling, permissions, and retained end-state evidence.

### DW-55: [HIGH · `RV-DURABLE-WORM` · durable audit-storage fidelity] Projection rebuild and audit-store fault validation still use `InMemoryWormAuditStore` or a test-hosted `IAuditWriter`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: durable WORM/audit infrastructure owner, coordinated with Story 12.16 where applicable
severity: high
reason: **[HIGH · `RV-DURABLE-WORM` · durable audit-storage fidelity] Projection rebuild and audit-store fault validation still use `InMemoryWormAuditStore` or a test-hosted `IAuditWriter`.** The repaired rebuild executes real persisted projection writes/read-back/cleanup, but it does not prove durable WORM/KMS/storage recovery or product audit composition. **Release-claim impact:** do not claim durable audit-chain disaster recovery or storage/KMS outage coverage. **Owner:** durable WORM/audit infrastructure owner, coordinated with Story 12.16 where applicable. **Closure evidence:** retained live evidence from the production binding showing append/read failure, fail-closed mutation behavior, recovery, chain verification, rebuild equivalence, and cleanup against durable storage.
status: open
decision: 2026-08-27 Build durable binding — Bind an approved durable WORM and KMS store and retain failure, recovery, chain-verification, rebuild, and cleanup evidence.
decision: 2026-08-26 Build durable binding — Bind an approved durable WORM and KMS store and retain failure, recovery, chain-verification, rebuild, and cleanup evidence.

### DW-56: [HIGH · `RV-PROD-CONTROL` · production orchestration and replica coordination] The destructive scheduler runs isolated GitHub/Aspire test jobs, not a production AKS/multi-replica control plane.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: deployment/platform reliability owner
severity: high
reason: **[HIGH · `RV-PROD-CONTROL` · production orchestration and replica coordination] The destructive scheduler runs isolated GitHub/Aspire test jobs, not a production AKS/multi-replica control plane.** The scheduled lane is serialized within its own concurrency group; release validation is deliberately concurrent per commit so required checks are not cancelled. The run proves allowlisted Aspire resource commands and restoration in a single local topology. **Release-claim impact:** do not claim Kubernetes/platform recovery automation, distributed lease safety, replica coordination, or production resource-control authorization. **Owner:** deployment/platform reliability owner. **Closure evidence:** an approved pre-production platform drill with least-privilege resource authority, distributed lease/non-overlap proof, multi-replica behavior, bounded restoration, health/end-state verification, and retained gate evidence.
status: open
decision: 2026-08-27 Build platform drill — Add an approved pre-production multi-replica drill with least-privilege controls, distributed non-overlap, restoration, health, and retained evidence.
decision: 2026-08-26 Build platform drill — Add an approved pre-production multi-replica drill with least-privilege controls, distributed non-overlap, restoration, health, and retained evidence.

### DW-57: [MEDIUM · `RV-PROVIDER-SCALE` · provider, product-composition, and workload scale] Local provider/component contract implementations and the configured six-record baseline do not represent product DI composition, production volume, latency, replicas, quotas, or regional failure modes.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: performance/capacity and provider-integration owners
severity: medium
reason: **[MEDIUM · `RV-PROVIDER-SCALE` · provider, product-composition, and workload scale] Local provider/component contract implementations and the configured six-record baseline do not represent product DI composition, production volume, latency, replicas, quotas, or regional failure modes.** Story 12.15 proves bounded contract behavior only. The baseline is **not** fully materialized: of its six categories, only `sourceRecords` and `wormAuditRecords` become real entities that are seeded, projected or compared; `governedCommands`, `approvals`, `policySnapshots` and `attachmentMetadata` are parsed into inert records that are never observed. Evidence no longer presents the configured corpus as exercised coverage: `configuredDatasetVolume` carries the six-record provenance, continuity and scoped-outage manifests publish `datasetVolume: 0`, and projection rebuild publishes the actual compared-resource count. Retained hosted evidence of this same bounded, non-production-shaped contract behavior now exists — see release run `33066358280` / evidence `01M11EYSDMP1ZF38B7KZA1A6FA` (2026-08-27) in `.decision-log.md` — but it is hosted proof of the same local-provider/sandbox-scale limitations described below, not a step toward production-shaped coverage; that gap is unaffected. **Update 2026-08-02:** the round-2 evidence-integrity remediation genuinely materialized `attachmentMetadata` — `attachment-processing` now runs through the real `AttachmentCaptureCoordinator` against a seeded projection candidate — though the candidate is exercise-seeded rather than sourced from the dataset file's own attachment record. `governedCommands`, `approvals`, and `policySnapshots` remain unmaterialized as described above. **Update 2026-08-03 (chunk 1b decision 2):** Contained for `ai-provider`, `command-execution`, `audit-store`, and `attachment-processing` is **sandbox-contract** evidence only — Aspire ops `fault`/`process`/`restore` the recovery-sandbox and trust that JSON (including NFR41 stamps and safety flags); ChatBot's real dependency clients are not the faulted seam. Independent control succeeding during `audit-store` is expected under this harness shape and must not be read as product audit fail-closed proof. **Update 2026-08-04:** the manifest/gate split closes the evidence-labeling defect only; it does not materialize the remaining categories or close the scale residual. **Release-claim impact:** local provider/rebuild timings cannot be advertised as production performance or capacity evidence; do not claim product DI composition, real AI-provider outage behavior, product command-execution path, or ChatBot audit-store fail-closed mutation from the Tier-3 sandbox Contained sweep for those four tokens. **Owner:** performance/capacity and provider-integration owners. **Closure evidence:** retained load-profile evidence at an approved production-shaped dataset and replica count, including product-composed provider paths (ChatBot client seams for the four tokens), quota/latency/failure modes, target measurements, isolation/end-state assertions, and the same manifest provenance.
status: open
decision: 2026-08-27 Build production-shaped lane — Exercise product-composed clients at an approved dataset, replica count, quota, latency, and regional failure profile with retained end-state evidence.
decision: 2026-08-26 Build production-shaped lane — Exercise product-composed clients at an approved dataset, replica count, quota, latency, and regional failure profile with retained end-state evidence.

### DW-58: [HIGH · `RV-MEASURABLE-CEILING` · the lane cannot reach the targets it is cited for] The Tier-3 restoration budget is 180 seconds against a 4-hour RTO (A10/NFR56) and a 4-hour rebuild target (NFR57).

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: src/Hexalith.ChatBot.Server/Audit/LiveRecoveryValidationOptions.cs
severity: high
reason: **[HIGH · `RV-MEASURABLE-CEILING` · the lane cannot reach the targets it is cited for] The Tier-3 restoration budget is 180 seconds against a 4-hour RTO (A10/NFR56) and a 4-hour rebuild target (NFR57).** `LiveRecoveryValidationOptions.Validate()` places no lower bound on `RestorationTimeout` relative to `MaxRto`, so any genuine recovery between 3 minutes and 4 hours converts to `unmeasurable`, never `missed`. Round 5 removed the companion `PerScenarioTimeout >= MaxRto` rule, which claimed to "permit measurement through the recovery target" while being unsatisfiable — nine serial scenarios at 4 hours each cannot fit a sub-`RunnerBudget` workflow — and replaced it with a serial fair-share bound; it deliberately did **not** add a `RestorationTimeout` floor, because any such number is invented (see the round-2 note that removed an earlier attempt). Each manifest publishes `MeasurableRecoveryCeilingSeconds` and the gate emits a non-blocking `{job}:{key}:target_exceeds_measurable_ceiling` claim limitation, so the limit is disclosed rather than hidden — but it is not enforced at ratification. A shorter budget is a deliberate trade: a 4-hour restoration window is impractical on every scheduled run. **Release-claim impact:** a pass from this lane is evidence for recovery within 180 seconds only; it must never be cited as confirming RTO ≤ 4 hr or rebuild ≤ 4 hr, and A10 ratification from it is bounded accordingly. **Owner:** Architecture/DevOps, with performance/capacity. **Closure evidence:** either a lane whose restoration budget reaches `RecoveryTargets.MaxRto` with retained evidence of a measurable miss and pass either side of the boundary, or a separately evidenced pre-production drill that measures the full window, plus a ratification rule that rejects a candidate artifact carrying the claim-limitation token for the target being ratified. [`src/Hexalith.ChatBot.Server/Audit/LiveRecoveryValidationOptions.cs`, `LiveRecoveryValidationEvidenceGate.cs`]
status: open
decision: 2026-08-27 Build full-window evidence — Add a separately scheduled full-window pre-production lane and reject ratification artifacts whose measurable ceiling is below the claimed target.
decision: 2026-08-26 Build full-window evidence — Add a separately scheduled full-window pre-production lane and reject ratification artifacts whose measurable ceiling is below the claimed target.

### DW-59: [MEDIUM · `RV-EVIDENCE-KINDS` · the retained evidence bundle carries no logs, traces, metrics or state-store end-state] The manifest requires only `test-output` and `reports`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: src/Hexalith.ChatBot.Server/Audit/RecoveryValidationEvidenceManifest.cs
severity: medium
reason: **[MEDIUM · `RV-EVIDENCE-KINDS` · the retained evidence bundle carries no logs, traces, metrics or state-store end-state] The manifest requires only `test-output` and `reports`.** Task 2 and the "Evidence is retained and reviewable" obligation originally required links to raw CI artifacts, logs, traces, metrics, state-store end-state and test output; `RecoveryValidationEvidenceManifest` requires two kinds, and both workflows upload `TestResults` only. Requiring the other four did not make them exist — it guaranteed every manifest carried syntactically valid links to nothing, which `IsSafeArtifactLocator` (a syntax check) cannot detect. Round 5 narrowed the obligation to what the lane produces rather than leaving the documents asserting six. **Release-claim impact:** a reviewer investigating a failed or disputed run has the `.trx` and the retained reports/manifests only; there is no trace, metric or state-store end-state evidence to corroborate a verdict, so a disputed measurement cannot be independently reconstructed from the artifact. **Owner:** Architecture/DevOps with observability. **Closure evidence:** the lane emits OTLP traces and metrics plus a state-store end-state dump into `TestResults`, both workflows upload them, the manifest requires the full vocabulary again, and a test asserts each required locator resolves to a file the workflow actually produces. [`src/Hexalith.ChatBot.Server/Audit/RecoveryValidationEvidenceManifest.cs`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`]
status: open

### DW-60: [LOW] `TierThreeParallelValidationShouldRetainEveryNonCancellationSiblingFailure` is a flaky concurrency test.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: TrivialGovernedCommandAspireE2eTests.cs
severity: low
reason: **[LOW] `TierThreeParallelValidationShouldRetainEveryNonCancellationSiblingFailure` is a flaky concurrency test.** The test asserts `siblingFailures.InnerExceptions.Count == selectedPorts.Count - 1` (3), but `TopologyReadinessCoordinator.ValidateAsync` intermittently retains only 2 sibling failures: observed one failure followed by three passes on identical code with no intervening edit. The race is in the coordinator's sibling-exception aggregation when several validators throw concurrently after a shared `TaskCompletionSource` gate releases. Pre-existing and out of scope for Story 12.15 — `TrivialGovernedCommandAspireE2eTests.cs` is unmodified at baseline `1493ff8f`; it surfaced only because the Story 12.15 review ran the Integration suite repeatedly. Release-claim impact: a Tier-3 topology-readiness failure could under-report which resources failed, making a multi-resource startup failure look like a smaller one. Closure evidence: a deterministic reproduction (loop the test ≥50×) plus a fix that aggregates all non-cancellation sibling failures under a lock or `ConcurrentBag` before the coordinator throws. [`tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs:1007`]
status: open

### DW-61: [MEDIUM] The live-recovery evidence gate never asserts that a manifest's `ResidualIds` covers the scenario's required residual set.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: LiveRecoveryValidationEvidenceGate.cs:86-149
severity: medium
reason: **[MEDIUM] The live-recovery evidence gate never asserts that a manifest's `ResidualIds` covers the scenario's required residual set.** `LiveRecoveryValidationEvidenceGate.cs:86-149` validates manifest shape, freshness, provenance, coverage and verdict dimensions, but no branch checks residual coverage, and no test ties a manifest's residual ids to the `RV-EXT-M365` / `RV-DURABLE-WORM` / `RV-PROD-CONTROL` / `RV-PROVIDER-SCALE` entries in this ledger. Release-claim impact: a residual could be silently dropped from published evidence while the ADR and PRD still assert it open, so a narrowing of scope would not surface at the gate. Closure evidence: a gate branch requiring the per-job required residual set, plus a test binding that set to the ledger entry ids. Belongs with the chunk-2 gate-anchoring work accepted in review round 3. [`src/Hexalith.ChatBot.Server/Audit/LiveRecoveryValidationEvidenceGate.cs:86-149`]
status: open

### DW-62: [MEDIUM] The `RV-*` entries' "Release-claim impact: do not claim …" constraints have no enforcement point.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: _bmad-output/implementation-artifacts/deferred-work.md:7-10
severity: medium
reason: **[MEDIUM] The `RV-*` entries' "Release-claim impact: do not claim …" constraints have no enforcement point.** Each of the four Story 12.15 residuals above states prohibited claims (external-M365 disaster-recovery equivalence, durable audit-chain disaster recovery, Kubernetes/platform recovery automation, production performance/capacity), but nothing in the release workflow, the evidence gate, or the release-notes path checks that those claims are absent from shipped material. The constraint lives only as ledger prose. Release-claim impact: a prohibited disaster-recovery claim can ship with no gate catching it. Closure evidence: a named owner plus a defined checkpoint (release-notes review step or claim validator) where each open residual's prohibited-claim list is checked. Pre-existing: this ledger format predates Story 12.15. [`_bmad-output/implementation-artifacts/deferred-work.md:7-10`]
status: open

### DW-63: [HIGH · pre-existing production binding] The scheduled controls still read and emit only through process-local defaults.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-21)"), 2026-08-26
location: CommandGatewayServiceCollectionExtensions.cs:103,113,164-168,228-229
severity: high
reason: **[HIGH · pre-existing production binding] The scheduled controls still read and emit only through process-local defaults.** `CommandGatewayServiceCollectionExtensions.cs:103,113,164-168,228-229` binds the outbound trace, derived store, WORM store, and operator-alert sink to in-memory implementations. The hosted proof therefore demonstrates scheduler invocation but not durable production evidence, and its empty default stores can report zero breaches without exercising production tenant data. Story 12.16 explicitly owns the live Hexalith.Memories derived-store binding; the WORM/outbound-trace/alert production bindings need an equally explicit owner before “continuous production enforcement” or production release-gate claims are accepted.
status: open
decision: 2026-08-27 Build durable bindings — Assign owners and implement production bindings for outbound trace, derived store, WORM audit, and operator alerts with production-data scheduler evidence.
decision: 2026-08-26 Build durable bindings — Assign owners and implement production bindings for outbound trace, derived store, WORM audit, and operator alerts with production-data scheduler evidence.

### DW-64: [MEDIUM · pre-existing runtime scaling] Cadence ownership and status are process-local.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-21)"), 2026-08-26
location: PeriodicEnforcementRuntime.cs:181-193,321-323,690-718
severity: medium
reason: **[MEDIUM · pre-existing runtime scaling] Cadence ownership and status are process-local.** `PeriodicEnforcementRuntime.cs:181-193,321-323,690-718` keeps last-run state, cadence partitions, and non-overlap guards in memory. Restarts reset the evidence window and horizontally scaled replicas can execute the same global sweep and duplicate writes/alerts. The current story explicitly modeled the in-memory once-per-day guard, so distributed leasing/durable status belongs to the deployment/scale owner before the server runs with multiple replicas.
status: open
decision: 2026-08-28 Build distributed cadence — Introduce a durable fenced lease and cadence-status store and verify restart and multi-replica non-overlap.

### DW-65: [HIGH · owned by Story 12.16] The periodic correction-propagation SLO-deadline sweep (AC1 item 5) was never built.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
location: n/a
severity: high
reason: **[HIGH · owned by Story 12.16] The periodic correction-propagation SLO-deadline sweep (AC1 item 5) was never built.** Approved as a deviation by Jerome on 2026-07-31; recorded here because until now it existed only as prose in Story 12.14's Completion Notes, where no sweep or audit would find it. `ICorrectionPropagationCoordinator` exposes only `IsReady` and `StartAsync` — there is no sweep/scan method and no store seam enumerating in-flight or incomplete propagations, so a periodic scanner has nothing to iterate. SLO enforcement is currently live **inline and synchronously**: `DaprCorrectionPropagationCoordinator.StartAsync` sets `EstimatedCompletionAtUtc` via `CorrectionPropagationSlo.DeadlineFor`, and `InMemoryVectorReindexer` checks `IsBreached` within the same call. A *periodic* sweep only has in-flight work to catch once the reindex is asynchronous, which is why this is coupled to Story 12.16 (live Hexalith.Memories binding + the async-reindex question) rather than to 12.14. Story 9.6 defers both halves for the same reason and was deliberately not rewritten. **Do not close this until 12.16 either builds the sweep or records why an async reindex still does not need one.**
status: open

### DW-66: [MEDIUM · mitigated 2026-07-31; durable fix owed to `Hexalith.Builds`] `Hexalith.EventStore.Gateway` is missing from the shared package catalog.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
location: .github/workflows/ci.yml
severity: medium
reason: **[MEDIUM · mitigated 2026-07-31; durable fix owed to `Hexalith.Builds`] `Hexalith.EventStore.Gateway` is missing from the shared package catalog.** **Mitigation in place:** the workspace now builds the `references/` submodules from source (`UseHexalithProjectReferences=true`, set as a workflow-level `env:` in `.github/workflows/ci.yml` + `release.yml` and documented as a required local export in `README.md`), which sidesteps the package entirely. Verified: full solution 0 errors/0 warnings and 12 of the 13 test projects green (2,808 passed, 3 skipped), including `Hexalith.ChatBot.IntegrationTests`, which was previously unbuildable; UI E2E was deliberately not re-run (running it regenerates fixture PNGs). **Still owed:** add `PackageVersion Include="Hexalith.EventStore.Gateway"` to `references/Hexalith.Builds/Props/Directory.Packages.props` (it lists twelve sibling `Hexalith.EventStore.*` entries but not this one) and align `HexalithEventStoreVersion` — currently `3.78.0`, which predates Gateway's first release at `3.82.0` — with the checked-out EventStore submodule (`v3.86.0`). Until that lands, package-mode builds of this workspace are impossible and the source-reference switch is load-bearing rather than a preference. Owner: the `Hexalith.Builds` repository. Note the mitigation also changes what CI exercises (submodule source rather than published packages), which is worth confirming is intended.
status: done 2026-08-26
resolution: already resolved: references/Hexalith.Builds/Props/Directory.Packages.props:8,46 now pins EventStore 3.97.0 and catalogs Hexalith.EventStore.Gateway.

### DW-67: [RESOLVED 2026-07-31 — retained for history] The `Hexalith.Tenants` submodule bump in commit `30aa887` broke the super-repo build.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
location: references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj:22
reason: **[RESOLVED 2026-07-31 — retained for history] The `Hexalith.Tenants` submodule bump in commit `30aa887` broke the super-repo build.** `dotnet build Hexalith.ChatBot.slnx -c Debug` fails `NU1010` on `references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj:22`. The old pointer (`41e047e8`) had only an unconditional `ProjectReference` to `Hexalith.EventStore.Gateway`; the new pointer (`625061bd`) added a `PackageReference` fallback conditioned on `'$(HexalithEventStoreFromSource)' != 'true'`. In a super-repo build that submodule's `Directory.Build.props:56` defaults `UseHexalithProjectReferences` to `false`, so the fallback activates and Central Package Management finds no matching `PackageVersion` in the root `Directory.Packages.props` (the `Hexalith.EventStore.Gateway` project does exist on disk, so the ProjectReference branch would resolve if the property were set). Consequence: `Hexalith.ChatBot.IntegrationTests` cannot be rebuilt, so it was not re-verified after the 2026-07-31 code-review remediation. Options: add the missing `PackageVersion`, set `UseHexalithProjectReferences=true` for the super-repo build, or revert/advance the Tenants pointer. Deliberately not fixed during code review — it is a dependency/submodule change. Owner: workspace/dependency owner. See also the standing note that inner submodule `*.slnx` files are standalone-only.
status: done 2026-08-26
resolution: already resolved: .github/workflows/ci.yml:35 and .github/workflows/release.yml:23 set UseHexalithProjectReferences=true, selecting the existing Tenants project-reference branch.

### DW-68: [LOW · pre-existing clock assumption] `LastSucceededAtUtc` is not monotonic.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
location: PeriodicEnforcementRuntime.cs:285-297
severity: low
reason: **[LOW · pre-existing clock assumption] `LastSucceededAtUtc` is not monotonic.** `PeriodicEnforcementRuntime.cs:285-297` — `RecordM2SweepSucceeded` overwrites unconditionally with no monotonic guard. A backwards NTP correction that crosses a partition boundary re-runs the sweep and records an *earlier* success time; if the backward jump exceeds `M2SweepCadence + MissedCadenceAlertAfter`, the missed-cadence check then alerts on every tick until wall-clock catches up. The whole runtime reads wall-clock via `ISystemClock` with no monotonic source anywhere, so this belongs to the clock/scale owner rather than this story.
status: open

### DW-69: [MEDIUM · pre-existing test defect, not in this story's diff] `AiActionRiskClassifierTests.UnknownActionClassShouldFailClosedWithoutSerializingInvalidEnumValues` is flaky at ~1%.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
location: tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionRiskClassifierTests.cs:111
severity: medium
reason: **[MEDIUM · pre-existing test defect, not in this story's diff] `AiActionRiskClassifierTests.UnknownActionClassShouldFailClosedWithoutSerializingInvalidEnumValues` is flaky at ~1%.** `tests/Hexalith.ChatBot.Server.Tests/Governance/AiMediation/AiActionRiskClassifierTests.cs:111` asserts `serialized.ShouldNotContain("999")` over the entire serialized record, but `AiActionRiskClassifier.cs:74` stamps a live `ProducedAtUtc = DateTimeOffset.UtcNow` into that payload and its fractional seconds can legitimately contain `999`. The unknown enum value is already filtered correctly by `OrderKnownClasses` (`AiActionRiskClassifier.cs:102-107`) — the failing run's payload showed `"riskActionClasses":[]` — so the assertion tests the wrong thing. Measured 2 failures in 200 runs of the compiled runner; a full-suite run at `30aa887` returned 1 failed / 1,695 passed / 1,696 total, which contradicts the "all 13 projects green, 0 failed" completion claim as a *reproducible* gate. Fix by asserting against the enum-bearing fields (or a deterministic clock), not the whole payload. Owner: the AI-mediation risk-classification surface (Stories 4.3/4.8).
status: open

### DW-70: [MEDIUM · coupled to the distributed cadence-lease item above] The now-deterministic derived-store sentinel id makes the isolation probe fail-open under concurrent sweeps.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, second adversarial re-review of eaee04f..working tree)"), 2026-08-26
location: DerivedStoreIsolationProbeCoordinator.cs:243-244
severity: medium
reason: **[MEDIUM · coupled to the distributed cadence-lease item above] The now-deterministic derived-store sentinel id makes the isolation probe fail-open under concurrent sweeps.** `DerivedStoreIsolationProbeCoordinator.cs:243-244` — the round-1 patch removed the run correlation id from `SentinelResourceId` to make the probe idempotent (correctly: nightly activation of the old per-run id would have grown live derived state without bound). The consequence is that `iso-probe:{segment}:{ownerTenant}` is now a *shared mutable key* across runs. If two sweeps overlap on the same `(class, ownerTenant)` — two replicas, or a manual release-gate invocation racing the nightly sweep — run B's `finally` `InvalidateAsync` (`:126-128`) can delete run A's sentinel in the window between A's `PutAsync` (`:98-100`) and A's cross-tenant `GetAsync` (`:105-107`). A then observes `null`, `DerivedStoreIsolationVerifier.Verify` returns Isolated, and a genuinely leaky store reports **zero breaches** to the M2 stop-ship signal. `SweepAllTenantPairsAsync` is sequential, so a single process is safe today; the exposure begins exactly where the recorded "horizontally scaled replicas can execute the same global sweep" deferral does — but note that deferral describes duplicate *writes/alerts*, not a **masked breach**, which is strictly worse. Resolve together with distributed cadence leasing; do not close that item without covering this. Owner: the deployment/scale owner.
status: open
decision: 2026-08-28 Lease and fence cleanup — Serialize matching probes through the distributed cadence lease, fence cleanup to its generation, and add concurrent-sweep tests.

### DW-71: [LOW · pre-existing, outside the solution] `Hexalith.ChatBot.Aspire.Tests` fails on a stale hardcoded source path and nothing runs it.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, second adversarial re-review of eaee04f..working tree)"), 2026-08-26
location: src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs
severity: low
reason: **[LOW · pre-existing, outside the solution] `Hexalith.ChatBot.Aspire.Tests` fails on a stale hardcoded source path and nothing runs it.** `ChatBotAspireModuleTests.AspireModuleShouldWireDedicatedWorkflowStateStoreForChatBotSidecar` reads `src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs`, which does not exist — the file lives at `src/Hexalith.ChatBot.AppHost/Aspire/ChatBotAspireModule.cs` — so the test throws `FileNotFoundException` (1 failed / 2 passed). Neither the test nor the Aspire source is touched by Story 12.14, and the project is **not listed in `Hexalith.ChatBot.slnx`**, so neither CI nor any completion gate exercises it; the same is true of `Hexalith.ChatBot.ServiceDefaults.Tests` (5 tests, currently green). Surfaced only because the round-2 review ran every test project on disk rather than only the solution's 13. Fix is a one-line path correction, but the real question for the owner is whether these two orphaned projects should be added to the solution or deleted — an untracked test project that nothing runs is worse than no test project. Owner: the build/solution owner.
status: done 2026-08-26
resolution: already resolved: Commit 079c5a3 deleted the orphaned Hexalith.ChatBot.Aspire.Tests and Hexalith.ChatBot.ServiceDefaults.Tests projects and moved ChatBotAspireModule into AppHost.

### DW-72: [LOW · deliberate design, unbounded against a hanging store] Probe cleanup has no deadline.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, second adversarial re-review of eaee04f..working tree)"), 2026-08-26
location: DerivedStoreIsolationProbeCoordinator.cs:122-134
severity: low
reason: **[LOW · deliberate design, unbounded against a hanging store] Probe cleanup has no deadline.** `DerivedStoreIsolationProbeCoordinator.cs:122-134` — the `finally` issues up to four `InvalidateAsync` round-trips per probed pair with `CancellationToken.None`. Passing `None` is correct in intent (cleanup must survive cancellation, or a cancelled sweep leaves sentinels behind), but there is no timeout either. Against a store that hangs rather than fails fast, cancellation delivered mid-sweep blocks the `finally`, which blocks `SweepAllTenantPairsAsync`, `RunOnceAsync` and `ExecuteAsync` past the host `ShutdownTimeout` (30s default) into a forced kill. Same root cause as the open per-sweep-timeout patch in Story 12.14; fix by bounding the cleanup with its own timeout rather than by restoring the shutdown token.
status: open

### DW-73: [LOW · pre-existing, Story 13.4 scope] Null `facet.Health` NREs in the status-summary badge mapper.

origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21, adversarial re-review of commits e91503b+97d31a6)"), 2026-08-26
location: ChatBotConversationItemStatusSummary.razor:154
severity: low
reason: **[LOW · pre-existing, Story 13.4 scope] Null `facet.Health` NREs in the status-summary badge mapper.** `ChatBotConversationItemStatusSummary.razor:154` calls `health.ToUpperInvariant()` on `facet.Health`; a null `Health` throws `NullReferenceException` before the `BadgeColor` switch is evaluated. This is on the **unchanged** line 154 — NOT introduced by the reviewed diff (which only added the `"UNKNOWN" => BadgeColor.Warning` arm). Case is already normalized and empty string falls through to `_ => Subtle`, so only a genuine null trips it. Verify whether `facet.Health` can be null on any live surface; if so, coalesce to `"UNKNOWN"`/`string.Empty` before `ToUpperInvariant()`. Owner: Story 13.4 status-summary surface.
status: open

### DW-74: [LOW · verify in Story 13.9] Blocked-state reason heading may sit as an `<h2>` peer of the conversation-stream section title.

origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21, adversarial re-review of commits e91503b+97d31a6)"), 2026-08-26
location: ChatBotBlockedState.razor:17
severity: low
reason: **[LOW · verify in Story 13.9] Blocked-state reason heading may sit as an `<h2>` peer of the conversation-stream section title.** Commit `e91503b`/`9462de3` moved `ChatBotBlockedState`'s `FluentMessageBar Title` into an in-content `<h2 class="chatbot-section-title">` (`ChatBotBlockedState.razor:17`). In the empty-conversation path the blocked state renders inside `ChatBotConversationStream`'s section, whose own title is already an `<h2>` (`ChatBotConversationStream.razor:8`), producing two peer `<h2>`s where the blocked-reason heading is logically subordinate — a soft document-outline nuance (WCAG 1.3.1 best practice, not a hard failure). The other three usages render directly under the route `<h1>` (valid `h1→h2`), and the host still carries `aria-label`, so the accessible name is preserved. Story 13.9's live cross-surface real-render heading-order sweep should confirm axe/heading-order is clean on the project-conversation empty state; if flagged, make the blocked-state heading level context-aware. Owner: Story 13.9 real-render reverification.
status: open

### DW-75: [RESOLVED in this review — was: MEDIUM pre-existing test flake] Story 13.1's live-render acceptance test is flaky — NOT an app defect.

origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21)"), 2026-08-26
location: FrontComposerShell.razor:104
reason: **[RESOLVED in this review — was: MEDIUM pre-existing test flake] Story 13.1's live-render acceptance test is flaky — NOT an app defect.** **Resolution:** added a `BoundingBoxWhenReadyAsync` poll-until-non-null helper and routed the banner (`ShellHeaderBandBottomAsync`), route-heading, and settings-button box reads through it in `RealRenderCrossSurfaceE2ETests`; the previously-flaky test now passes 5/5 consecutively and the full class is 3/3. Build 0/0. Original diagnosis follows for the record: `RealRenderCrossSurfaceE2ETests.AllSixSurfaces_ComposeFrontComposerLayout_WithoutLegacyChrome_BelowShellBand` fails intermittently (~2 of 3 runs on the clean committed tree `9462de3`) with `bannerBox`/`settingsBox should not be null`. **Root-caused via live-DOM inspection: the shell banner and `[data-testid="fc-settings-button"]` DO render correctly** (DOM counts `banner=1`, `settings=1` after a hydration settle; `FcSettingsButton` is wired at `FrontComposerShell.razor:104` and `MainLayout` leaves `HeaderEnd` null so it auto-populates). The test reads `BoundingBoxAsync()` on shell-header elements (the banner in `ShellHeaderBandBottomAsync:327`, the settings button at ~:148) immediately after navigation, before the Fluent Blazor **web components hydrate and acquire a layout box**, so the box is intermittently `null`. This is Blazor-Server + Fluent web-component hydration-timing flakiness — pre-existing, not caused by the reviewed diff. It undermines the *reliability* of the story's live-route acceptance gate (a flaky gate passes non-deterministically), which matters for the Epic 13 "primary live route must execute successfully" rule. **Scoped fix (test-only, ChatBot side; no app/shell/submodule change):** `await <locator>.WaitForAsync(new() { State = WaitForSelectorState.Visible })` (or poll-until-non-null) before every shell-header `BoundingBoxAsync()` read — the banner in `ShellHeaderBandBottomAsync`, the settings button, and the route heading. Verified during review: a settings-visible wait alone fixed some runs but not all (the banner box still raced), so the fix must gate the banner read too. Owner: ChatBot UI E2E test.
status: done 2026-08-26
resolution: already resolved: tests/Hexalith.ChatBot.UI.E2E.Tests/RealRenderCrossSurfaceE2ETests.cs:139-150,441-450 routes heading, settings, and banner box reads through BoundingBoxWhenReadyAsync.

### DW-76: [RESOLVED — was: MEDIUM · verify-when-live-green] MessageBar aria-live / announcement contract now DOM-verified.

origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21)"), 2026-08-26
location: ChatBot UI E2E (optional seeded-terminal capture)
reason: **[RESOLVED — was: MEDIUM · verify-when-live-green] MessageBar aria-live / announcement contract now DOM-verified.** With the flake above fixed, the live route is green and the single-live-region contract was re-inspected directly on the live DOM (Story 13.1 verify-and-close, 2026-07-21). **Finding: not a defect.** The review's premise — that `FluentMessageBar` "owns its own intent-driven aria-live" that the raw `role`/`aria-live`/`aria-atomic` splat could duplicate or conflict with — does **not** hold in Fluent UI Blazor **v5.0.0-rc.3**: `FluentMessageBar` renders as a `<fluent-message-bar>` custom element whose shadow root is **slots-only** (`shadowRoot` = `<slot name="icon">`, `.content > <slot>`, `.actions`, `<slot name="dismiss">` — **0** `aria-live`, **0** `role="status"/"alert"`). So the host attributes `ChatBotStatusBanner`/`ChatBotBlockedState` splat ARE the single authoritative live-region declaration; there is no component-owned region to compete. **Verified live on two surfaces:** (1) non-announcing/dedup path — the always-present `/` project-workspace banner (`StateFamily=ObservedForOthersRejectionOrQueueUpdate` → InlineStatus/`NoLiveAnnouncement`) resolves to a single element (`data-chatbot-stable-id` count = 1) with **no** `role`, `aria-live="off"`, `aria-atomic="true"`, `data-chatbot-live-announced="false"`, 0 nested light-DOM regions, 0 shadow regions — proving the deterministic dedup outcome reaches the DOM verbatim; (2) announcing path — the `/compliance-audit-investigation` status bar renders `role="status"`/`aria-live="polite"` with 0 shadow-internal regions. **Durable gate added (converts the source-scan-only aria-live coverage into a direct live invariant per epics §3231):** `RealRenderCrossSurfaceE2ETests.StatusMessageBars_ExposeSingleAuthoritativeLiveRegion_WithoutInternalShadowRegion` (live DOM; 4/4 stable, `Skipped:0` = real browser path). **Residual (LOW):** the terminal→`alert`/assertive live *render* is still not directly observed — the metadata-only `FakeChatBotClient` seam renders no terminal/blocked banner by default; the `alert` value is produced by the identical host-attribute projection now proven live for status/off and passes through the same slots-only element, so a seeded blocked/error interaction is the only remaining direct observation. Owner: ChatBot UI E2E (optional seeded-terminal capture).
status: done 2026-08-26
resolution: already resolved: tests/Hexalith.ChatBot.UI.E2E.Tests/RealRenderCrossSurfaceE2ETests.cs:246-280 directly verifies the single authoritative live region on a real rendered route.

### DW-77: [MEDIUM · residual, Story 13.8 scope] Fixture-based forced-colors/visual E2E still assert `.chatbot-status__label`.

origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21)"), 2026-08-26
location: n/a
severity: medium
reason: **[MEDIUM · residual, Story 13.8 scope] Fixture-based forced-colors/visual E2E still assert `.chatbot-status__label`.** 30+ hand-authored fixtures in `GovernedOperationsVisualFoundationE2ETests` emit a class the live `ChatBotStatusBanner` (now `FluentMessageBar`) no longer renders. This review de-tautologized the two assertions commit `9462de3` directly weakened (browser-path redundant line + no-browser self-reference); fully retiring the fixture-theater and moving status-cue forced-colors verification to the live-render suite is Story 13.8 (live cross-surface reverification) scope. **Concrete hard failure now surfaced (Story 13.1 verify-and-close, 2026-07-21):** `GovernedOperationsVisualFoundationE2ETests.AssociationReviewShouldPreserveForcedColorsReducedMotionAndBlockedRedactionStates` (`:1243`) asserts `borderTopStyle === "solid"` on a candidate radio, but the CSS retirement deleted that `.chatbot-*` border rule so it now computes `none` — the fixture, not the live app, is stale. **Deterministic and pre-existing at committed HEAD `e91503b`** (reproduced with the Story 13.1 test change path-scoped-stashed: `Expected: solid / Actual: none`, same result), so it is NOT a Story 13.1 regression and does not gate 13.1. The full `Hexalith.ChatBot.UI.E2E.Tests` project is therefore **140 passed / 1 failed / 0 skipped**, the single failure being this Story-13.8-owned stale fixture. Story 13.8 must fix/retire this fixture assertion when it retires the theater.
status: open

### DW-78: [LOW] Retrospective doc conflates multi-commit "13.1 review" corrections.

origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21)"), 2026-08-26
location: 10-1-frontcomposer-shell-integration.md:15
severity: low
reason: **[LOW] Retrospective doc conflates multi-commit "13.1 review" corrections.** `10-1-frontcomposer-shell-integration.md:15` / `test-summary.md:31` attribute EventStore-registration removal (actually in commit `7e63edc`) and the account-affordance/banner-landmark (pre-existing `RealRenderCrossSurfaceE2ETests` / shell) to "the Story 13.1 review" as if in commit `9462de3`. Imprecise attribution in a doc whose stated purpose is to retract overclaims. Doc-only.
status: open

### DW-79: [LOW] Unrelated `references/Hexalith.Memories` gitlink bump bundled into UI-CSS commit `9462de3`

origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21)"), 2026-08-26
location: n/a
severity: low
reason: **[LOW] Unrelated `references/Hexalith.Memories` gitlink bump bundled into UI-CSS commit `9462de3`** (`07445b5`→`114e818`). Memories is referenced only by `.Server`, never `Hexalith.ChatBot.UI`, so it is not required for this UI story; it reduces commit atomicity and complicates bisect. Isolate dependency bumps in future commits.
status: open

### DW-80: [MEDIUM] Workspace-mode package-authority enforcement runs in no CI.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1e-centralize-nuget-package-reference-version-authority (2026-07-18)"), 2026-08-26
location: validate-consumer-package-authority.ps1
severity: medium
reason: **[MEDIUM] Workspace-mode package-authority enforcement runs in no CI.** `validate-consumer-package-authority.ps1` (workspace/consumer mode) and `validate-package-version-exceptions.ps1` (workspace mode) are the story's actual enforcement mechanisms, but Builds CI runs the consumer validator only against Builds itself and the exception validator only in schema mode, and ChatBot `ci.yml`/`release.yml` invoke neither. Until wired, the closed SDK/tool allowlist and consumer exclusivity are enforced only by convention plus ChatBot's architecture tests. Owned by Story 1.1f (reusable domain-module CI/CD alignment proposal 2026-07-18).
status: open

### DW-81: [LOW] Partial/standalone checkout rough edges in path-resolution conventions.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1e-centralize-nuget-package-reference-version-authority (2026-07-18)"), 2026-08-26
location: Hexalith.Timesheets.Server.csproj:12
severity: low
reason: **[LOW] Partial/standalone checkout rough edges in path-resolution conventions.** Timesheets `Hexalith.Timesheets.Server.csproj:12` references `$(HexalithEventStoreRoot)` with no empty-expansion guard (rooted-path project-not-found error when neither nested nor sibling checkout exists); the Timesheets `.gitmodules` path move (`Hexalith.Builds` → `references/Hexalith.Builds`) leaves stale directories/`.git/modules` config in existing clones until `git submodule sync` + re-init; Conversations `Directory.Build.props` sibling-Commons probe checks 1 of the 7 directories its nested branch requires; Builds README consumer-wrapper example uses an `Exists`-guarded import with no fail-closed fallback, unlike `Samples/Module.Directory.Packages.props` and the ChatBot wrapper. Pre-existing pattern conventions extended by this story, all fail-closed-with-confusing-errors rather than fail-open.
status: open

### DW-82: [LOW] Cancel/Stop governed handler validates metadata only, not the target generation.

origin: migrated from legacy ledger ("Deferred from: code review of story-10.6b (2026-06-21)"), 2026-08-26
location: AcceptedCommandDispatcher.cs:605
severity: low
reason: **[LOW] Cancel/Stop governed handler validates metadata only, not the target generation.** `CancelAiResponseGeneration` is dispatched to a fresh aggregate keyed by `CancellationId` (`AcceptedCommandDispatcher.cs:605`), so the handler (`GovernedOperationAggregate.cs:1648-1693`) only validates safe-metadata tokens, `ExpectedSourceVersion <= 0`, tenant, and `CancellationId` idempotency — it never confirms the target ResponseId/GenerationId exists, is still in-flight, or belongs to the same project/conversation/session, never compares `ExpectedSourceVersion` to real aggregate state, and the cancel write path is not project-authorized (absent from `ParticipantAuthorizationStage.CanReadProject`). Bounded by server-bound tenant isolation (no cross-tenant fabrication) and read-side project authorization (a fabricated "stopped" row is only observable to a user already authorized to read that project — integrity/state-poisoning, not confidentiality). A robust in-flight guard needs the server-side generation-session lifecycle that arrives with a real async streaming provider (M2+); consistent with the prior-pass deferral. Optional cheap sub-hardening that does NOT need the lifecycle: add `CancelAiResponseGeneration` (and `RecordProjectConversationMessage`) to the `CanReadProject` branch of `ParticipantAuthorizationStage` so the cancel write path is project-owner-authorized like the read side.
status: done 2026-08-26
resolution: already resolved: src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs:1675-1702 validates active generation identity, project, and source version; ParticipantAuthorizationStage.cs:521-522 authorizes both cancellation and conversation-message writes through project access.

### DW-83: The externally committed Hexalith.Builds gitlink is not reproducible from fetched remote refs.

origin: migrated from legacy ledger ("Deferred from: code review of story-10.6b (2026-06-21)"), 2026-08-26
location: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
source_spec: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
reason: The externally committed Hexalith.Builds gitlink is not reproducible from fetched remote refs. Evidence: Root points to `e4ae82df6cfcc6511a32fc2ce100070d7924f119`, while the submodule reports local `main` ahead 1/behind 4 and no fetched remote ref contains that commit, so a fresh checkout may be unable to obtain the recorded dependency revision.
status: done 2026-08-26
resolution: already resolved: Root commit f43bfbcb updates references/Hexalith.Builds to 5c3ff35c, and that exact gitlink is currently reachable from the submodule's origin/main.

### DW-84: Externally committed Story 13.9 visual artifacts do not reliably prove dark-mode or settled forced-colors rendering.

origin: migrated from legacy ledger ("Deferred from: code review of story-10.6b (2026-06-21)"), 2026-08-26
location: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
source_spec: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
reason: Externally committed Story 13.9 visual artifacts do not reliably prove dark-mode or settled forced-colors rendering. Evidence: Eighteen baseline-to-HEAD PNGs changed outside this workflow; inspected dark captures render Light/Clair, the French project-conversation dark/light files are byte-identical, and an English forced-colors dashboard capture records a loading state while the generator overwrites tracked PNGs without baseline comparison.
status: open

### DW-85: Streaming release-readiness evidence remains source-token and synthetic-fixture based rather than production progressive-render execution.

origin: migrated from legacy ledger ("Deferred from: code review of story-10.6b (2026-06-21)"), 2026-08-26
location: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
source_spec: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
reason: Streaming release-readiness evidence remains source-token and synthetic-fixture based rather than production progressive-render execution. Evidence: The pre-existing readiness pattern searches whole source files for markers, while the cited browser case uses hand-authored `SetContentAsync` HTML; production component wiring, successive response chunks, or Stop behavior can regress without those markers disappearing.
status: done 2026-08-27
resolution: already resolved: Commit 011261ef302aba9d3045f03d456562c089c6886d added tests/Hexalith.ChatBot.IntegrationTests/Story132ProductionBrowserAspireE2ETests.cs:92-335, which exercises the authenticated production UI, command/projection/SignalR path, active generation, Stop, terminal render, focus return, and live announcement.

### DW-86: [LOW · durable activity CT] Workflow activities pass `CancellationToken.None` into EventStore/audit/alert side effects.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6-hosted-dapr-workflow-production-binding-and-saga-readiness-validation (2026-08-09)"), 2026-08-26
location: Story 2.9 / correction-propagation runtime
severity: low
reason: **[LOW · durable activity CT]** Workflow activities pass `CancellationToken.None` into EventStore/audit/alert side effects. **Release-claim impact:** host cancellation cannot abort in-flight activity I/O; acceptable under typical Dapr durable-activity semantics if activities remain idempotent. **Owner:** Story 2.9 / correction-propagation runtime. **Closure evidence:** documented durable-activity cancellation policy, or activity-context token plumbing with idempotent compensation tests.
status: open

### DW-87: [MEDIUM · terminal failure ownership] Distinct correction-propagation workflow `Failed` status deferred; Story 2.9 keeps `Correction-delayed` for store/soft failures.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6-hosted-dapr-workflow-production-binding-and-saga-readiness-validation (2026-08-09)"), 2026-08-26
location: Story 2.10 retry/exhaustion
severity: medium
reason: **[MEDIUM · terminal failure ownership]** Distinct correction-propagation workflow `Failed` status deferred; Story 2.9 keeps `Correction-delayed` for store/soft failures. **Release-claim impact:** do not cite Story 2.9 for terminal-failure saga evidence. **Owner:** Story 2.10 retry/exhaustion. **Closure evidence:** terminal/exhaustion path with status + tests under 2.10 (or explicit hard-fail design change).
status: open

### DW-88: [LOW · pre-existing, self-disclosed] `StrictEnumContractResolver` doesn't cover an enum used as a dictionary KEY or a nested collection element (e.g. `List<List<TEnum>>`).

origin: migrated from legacy ledger ("Deferred from: code review of story-12.15 (2026-08-24, chunk A: Server/Audit + AppHost + Client generated)"), 2026-08-26
location: src/Hexalith.ChatBot.Client/Generated/StrictEnumContractResolver.cs:38-53
severity: low
reason: **[LOW · pre-existing, self-disclosed] `StrictEnumContractResolver` doesn't cover an enum used as a dictionary KEY or a nested collection element (e.g. `List<List<TEnum>>`).** Only dictionary VALUE and single-level `IEnumerable` element are handled. Already self-disclosed in the class's own comment ("Latent today... but nothing guarded the next regeneration") and confirmed inert — no property of either shape exists in the current generated client. Owner: ChatBot Client generated-serialization owner, if a future NSwag regeneration introduces such a shape. [`src/Hexalith.ChatBot.Client/Generated/StrictEnumContractResolver.cs:38-53`]
status: open

### DW-89: [MEDIUM · test-design] Generated-client freshness fixture is a tautology.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25)"), 2026-08-26
location: tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs:57-64
severity: medium
reason: **[MEDIUM · test-design] Generated-client freshness fixture is a tautology.** `GeneratedOutputHashShouldMatchCheckedInFreshnessFixture` (`tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs:57-64`) hashes the checked-in `Generated/HexalithChatBotClient.g.cs` and compares it to `tests/fixtures/hexalith-chatbot-generated-client.sha256`, never regenerating from `hexalith.chatbot.v1.yaml`; no NSwag regeneration step exists in either workflow. It therefore detects hand-edits to the generated file but can never detect drift between the OpenAPI contract and the committed client, and updating the recorded hash is indistinguishable from laundering real drift. Pre-existing test design; the fixture bump in the 12.15 delta is already disclosed as Epic 13 / package-authority scope. Release-claim impact: "generated client matches the contract" is unproven. Closure evidence: a test that regenerates from the YAML and diffs.
status: open

### DW-90: [LOW · observability] The 40-minute completion reserve refusal has no gate reason code.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25)"), 2026-08-26
location: .github/workflows/ci.yml:215-231
severity: low
reason: **[LOW · observability] The 40-minute completion reserve refusal has no gate reason code.** `.github/workflows/ci.yml:215-231` fails the job when `elapsed_seconds >= 2400`, measured from before checkout, so unrelated CI slowness (this repo has a documented history of 13+ minute restore hangs) surfaces as a red required check with only a shell message and no machine-readable reason. `review-reality.md` R7 already conceded this failure class has no stable code and marked it "Resolved by claim correction". Release-claim impact: a reserve refusal is indistinguishable from an evidence breach in automated triage. Closure evidence: a dedicated reason code emitted on the refusal path.
status: open

### DW-91: [LOW · latent] `recognizedLaneBindings[0]` is hardcoded.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25)"), 2026-08-26
location: StoryEvidenceValidator.cs:872
severity: low
reason: **[LOW · latent] `recognizedLaneBindings[0]` is hardcoded.** `StoryEvidenceValidator.cs:872` and the new production-declaration preflight both take `RequiredArray(trigger, "recognizedLaneBindings", …)[0]` and then require `binding["lane"] == laneName`. Every trigger in `story-evidence-policy.json` has exactly one binding today, so this passes; the new `trx`/`provenance` pinning inherits the same index. Release-claim impact: adding a second binding to any trigger would silently ignore it while rejecting its lane as unrecognised. Closure evidence: iterate all bindings and match by lane.
status: open

### DW-92: [MEDIUM · governance] `ModelContextProtocol` 1.4.1 → 2.2.0 major bump is unratified.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25)"), 2026-08-26
location: ScaffoldArchitectureTests.cs:273
severity: medium
reason: **[MEDIUM · governance] `ModelContextProtocol` 1.4.1 → 2.2.0 major bump is unratified.** Commit `f804229` accepted a major-version bump of the SDK the M1 MCP surface wraps by editing `ScaffoldArchitectureTests.cs:273` and the `architecture.md` / `epics.md` planning records — all three inside Story 12.15's File List — with no ADR entry and no decision-log record. The pins match the current `references/Hexalith.Builds` catalog, so nothing is drifting; what is missing is the decision. Disclosed as out-of-scope in Story 12.15 by the 2026-08-25 review. Release-claim impact: a major SDK bump under the MCP surface is unreviewed. Closure evidence: an ADR plus decision-log entry owned by the package-authority story.
status: open
decision: 2026-08-27 Ratify 2.2.0 — Review compatibility impact, add an accepted ADR and decision-log record, and execute MCP adapter conformance coverage.
decision: 2026-08-26 Ratify 2.2.0 — Review compatibility impact, add an accepted ADR and decision-log record, and execute MCP adapter conformance coverage.

### DW-93: [MEDIUM · api-contract] A permanently malformed command payload is reported as a retryable `503` and raises an operator alert on every attempt.

origin: migrated from legacy ledger ("Deferred from: hosted-lane review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25, second batch)"), 2026-08-26
location: ChatBot gateway/API
severity: medium
reason: **[MEDIUM · api-contract] A permanently malformed command payload is reported as a retryable `503` and raises an operator alert on every attempt.** `AcceptedCommandDispatcher.BuildPlanAsync` throws `InvalidOperationException` for an unparseable aggregate identity, and `CommandGateway.SubmitAsync` classifies it with genuine dispatch outages: the caller is told `retryable: true`, `clientAction: retry-later`, and each attempt queues a pre-commit replay intent and an `OperatorAlertKind.AuditUnavailable` alert. During one recovery run a single malformed field produced ~1,000 such alerts. Changing it means changing a shipped API contract (`400`/`422` semantics, the message catalog, OpenAPI and conformance tests), so it is out of Story 12.15's scope. **Owner:** ChatBot gateway/API. **Closure evidence:** a decided and documented status/reason for malformed payloads, with alerting no longer triggered per attempt.
status: open
decision: 2026-08-27 Client-error contract — Define 400 or 422 semantics with a catalogued reason, update OpenAPI and conformance tests, and suppress outage alerts for permanent defects.
decision: 2026-08-26 Client-error contract — Define 400 or 422 semantics with a catalogued reason, update OpenAPI and conformance tests, and suppress outage alerts for permanent defects.

### DW-94: [MEDIUM · reliability] The ChatBot UI host can still be made unstoppable by an unreachable OTLP collector.

origin: migrated from legacy ledger ("Deferred from: hosted-lane review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25, second batch)"), 2026-08-26
location: ChatBotUiHostDefaultsExtensions.cs:34-37
severity: medium
reason: **[MEDIUM · reliability] The ChatBot UI host can still be made unstoppable by an unreachable OTLP collector.** `ChatBotUiHostDefaultsExtensions.cs:34-37` registers `UseOtlpExporter()` with no bounded telemetry shutdown, and `chatbot-ui` is a composed resource in the same DCP application as the EventStore host that was fixed. Provider disposal joins the batch-exporter thread with no timeout while that thread is parked in the exporter's retry sleep, so `IHost.DisposeAsync` never returns and the resource cannot be stopped. **Constraint:** the fix that exists, `BoundedTelemetryShutdownService`, is `internal sealed` to `Hexalith.EventStore.ServiceDefaults`, so the UI host cannot adopt it without a visibility change in that repository — which is why this is recorded rather than copied. **Owner:** ChatBot UI hosting, with EventStore ServiceDefaults for the visibility decision. **Closure evidence:** the UI host stops cleanly with an unreachable OTLP endpoint, proven the same falsifiable way as the EventStore host (disable the bound, the dispose test fails).
status: open
decision: 2026-08-27 Expose platform API — Publish a reusable bounded-shutdown seam from EventStore ServiceDefaults, consume it in ChatBot UI, and add an unreachable-collector test.
decision: 2026-08-26 Expose platform API — Publish a reusable bounded-shutdown seam from EventStore ServiceDefaults, consume it in ChatBot UI, and add an unreachable-collector test.

### DW-95: [MEDIUM · evidence-integrity] The scheduled and release recovery lanes upload `TestResults` without the metadata-only TRX projection.

origin: migrated from legacy ledger ("Deferred from: hosted-lane review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25, second batch)"), 2026-08-26
location: ChatBot DevOps
severity: medium
reason: **[MEDIUM · evidence-integrity] The scheduled and release recovery lanes upload `TestResults` without the metadata-only TRX projection.** The completion lane runs `sanitize-recovery-trx` before uploading; `live-recovery-validation` and its release twin upload the raw directory under `--logger "console;verbosity=detailed"`. The two content sources that carried raw material have been fixed at source (the resource-log tail is now metadata-only, and the retained response body is ChatBot's own redacted ProblemDetails), so this is defence in depth rather than a known leak — but the asymmetry means a future addition to that console output reaches an artifact unsanitised. **Owner:** ChatBot DevOps. **Closure evidence:** both lanes project through the sanitizer before upload, or the console logger is removed from the uploaded path.
status: open

### DW-96: [LOW · observability] Three projection log event ids are duplicated within the EventStore projection subsystem.

origin: migrated from legacy ledger ("Deferred from: hosted-lane review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25, second batch)"), 2026-08-26
location: Hexalith.EventStore
severity: low
reason: **[LOW · observability] Three projection log event ids are duplicated within the EventStore projection subsystem.** `1120` and `1121` are shared by `ProjectionDiscoveryHostedService` and `ProjectionUpdateOrchestrator`, and `4660` twice within `ProjectionUpdateOrchestrator`. Anything filtering or alerting on event id conflates them. They predate this work and renumbering a shipped id changes what operators' filters match, so `ProjectionLogEventIdUniquenessTests` pins them explicitly and fails on any **new** collision instead. **Owner:** Hexalith.EventStore. **Closure evidence:** ids renumbered with an operator-facing note, and the pinned allowlist emptied.
status: open
decision: 2026-08-27 Renumber with migration — Assign unique ids, publish an operator-facing filter migration note, update tests, and empty the collision allowlist.
decision: 2026-08-26 Renumber with migration — Assign unique ids, publish an operator-facing filter migration note, update tests, and empty the collision allowlist.

### DW-97: [MEDIUM · diagnosis] B4 — the scoped-outage control probe's catch scope is wider than the request it classifies.

origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
location: ChatBot Tier-3 harness
severity: medium
reason: **[MEDIUM · diagnosis] B4 — the scoped-outage control probe's catch scope is wider than the request it classifies.** `IsChatBotControlAvailableAsync` wraps the whole method in `catch (HttpRequestException)` / `catch (TaskCanceledException)`, so `AcquireControlAsync` before the POST and the durable/projection waits *after* a successful `202` are inside it. **Not covered:** a genuine post-admission containment failure can still be attributed to the transport and reported as `client-timeout` with `IndependentControlUnobserved = true`, i.e. "never reached ChatBot" when it did. **Partial mitigation already in place:** the cause is now reported (`status-…` / `transport-…` / `client-timeout`), so the misattribution is visible in the failure message instead of silent — but the classification itself is still wrong in that case. **Fix:** narrow both catches to the control POST only. **Owner:** ChatBot Tier-3 harness.
status: open

### DW-98: [LOW · correctness] C7 — `observedAtLeastOnce` guards nothing it appears to guard.

origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
location: ChatBot Tier-3 harness
severity: low
reason: **[LOW · correctness] C7 — `observedAtLeastOnce` guards nothing it appears to guard.** In `RemainsGovernedOperationProjectionAbsentAsync` it is set unconditionally on the first loop iteration, so it is only ever false when the loop body never ran; in that case the method reports "present" from a window whose final read was null. **Not covered:** a zero-length absence window silently inverts the result. The variable is documented in place but its guard is unchanged. **Owner:** ChatBot Tier-3 harness.
status: open

### DW-99: [LOW · api-hygiene] C10 (second half) — `RecoveryResourceLogTail.Render(string? matching)` exposes a filter no call site uses.

origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
location: ChatBot Tier-3 harness
severity: low
reason: **[LOW · api-hygiene] C10 (second half) — `RecoveryResourceLogTail.Render(string? matching)` exposed a filter no call site used.** **Historical gap:** the tail was always rendered wholly unfiltered; the parameter was dead surface that invited a future caller to assume filtering happened. *(The first half — sharing the dispatch-unavailable type URI as one constant — was already done.)* **Owner:** ChatBot Tier-3 harness.
status: done 2026-08-26
resolution: already resolved: tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryResourceLogTail.cs:88 now exposes parameterless Render(); commit b35f3eb3 removed the unused matching filter.

### DW-100: [MEDIUM · test-design] D2 — three of four checkpoint-refusal reason codes are unasserted.

origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
location: Hexalith.EventStore
severity: medium
reason: **[MEDIUM · test-design] D2 — three of four checkpoint-refusal reason codes are unasserted.** `ProjectionCheckpointRefusalDiagnosticsTests` covers `post-cutover-writer-protocol` only. **Not covered:** `delivery-row-requires-fenced-completion`, `delivery-row-schema-regression`, `delivery-row-unsupported` — each needs a delivery row that `ProjectionDeliveryStateClassifier` classifies accordingly, which the current fixture cannot build. A draft covering them was written and **reverted rather than shipped half-working**; the gap is also recorded in the test's own remarks. Its `GetStateAsync<ProjectionDeliveryState?>` arrangement is additionally inert, because production reads through `GetStateAndETagAsync`, so that precondition comes from NSubstitute's default rather than the stub. **Owner:** Hexalith.EventStore.
status: open

### DW-101: [MEDIUM · test-design] D5 — the capture-filter fix is untested and its token check is approximate.

origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
location: ChatBot Tier-3 harness
severity: medium
reason: **[MEDIUM · test-design] D5 — the capture-filter fix is untested and its token check is approximate.** The `|| !cancellationToken.IsCancellationRequested` guard is copy-pasted byte-identically across the three capturing runners with no unit test. **Not covered:** it reads the caller token's state at catch time rather than identifying which token actually cancelled, so an internal deadline firing concurrently with caller cancellation is still dropped, and the comment claims a stronger guarantee than the code provides. **Fix:** compare token identity, and cover it. **Owner:** ChatBot Tier-3 harness.
status: open

### DW-102: [LOW · dead-code] D6 — `WaitForGovernedOperationAsync`'s absence path is unreachable.

origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
location: ChatBot Tier-3 harness
severity: low
reason: **[LOW · dead-code] D6 — `WaitForGovernedOperationAsync`'s absence path was unreachable.** After moving absence onto the projection channel, all three call sites passed `expectPresent: true`. **Historical gap:** the parameter, the `AbsenceConfirmationWindow` branch, the "absence" wording and the trailing `return false` were dead, and the retained comment documented a path that no longer existed — so a future reader could reasonably believe absence was still observed there. **Fix:** remove the parameter or keep one caller exercising it. **Owner:** ChatBot Tier-3 harness.
status: done 2026-08-26
resolution: already resolved: Commit 90c84cd7 removed the expectPresent/absence surface; tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs:1387 now has the presence-only IsGovernedOperationProjectionPresentAsync.

### DW-103: [MEDIUM · claim-accuracy] The mailbox admission probe's retained warm-up comment describes a barrier the code no longer provides.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk A — Tier-3 harness and live drivers (2026-08-25)"), 2026-08-26
location: LiveContinuityAspireE2eTests.cs:745-750
severity: medium
reason: **[MEDIUM · claim-accuracy] The mailbox admission probe's retained warm-up comment describes a barrier the code no longer provides.** `AssertMailboxTokenAdmissionAsync` returns on `503 + dispatch-unavailable` (`LiveContinuityAspireE2eTests.cs:745-750`), while the comment immediately above at `:733-736` still says that path "intentionally returns 503 until it is ready, matching the ordinary Tier-3 acceptance test's first-command retry discipline". `CommandGateway` emits that same problem type for `EventStoreGatewayException` and `HttpRequestException` as well as the probe's deliberately invalid `IntakeId` (`CommandGateway.cs:95-111`). **Not covered:** the admission assertion itself is sound — only the accepted branch can emit that type, so observing it does prove the bearer was admitted — but the downstream first-command warm-up barrier the comment describes is gone, and restoring it is a separate design change rather than a comment fix. **Owner:** ChatBot Tier-3 harness. **Closure evidence:** either a decided warm-up gate distinct from the admission proof, or the comment corrected to state that admission, not readiness, is what the probe establishes.
status: open

### DW-104: [MEDIUM · claim-accuracy] Claim B2's "every absence result" holds at one of three absence call sites.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk A — Tier-3 harness and live drivers (2026-08-25)"), 2026-08-26
location: AspireRecoverySandboxOperations.cs:458
severity: medium
reason: **[MEDIUM · claim-accuracy] Claim B2's "every absence result" holds at one of three absence call sites.** `AssertProjectionChannelReadsServerWritesAsync` precedes only the NFR59 absence probe (`AspireRecoverySandboxOperations.cs:458`). The cleanup fault-probe absence (`:605-611`) and the post-erase absence sweep including the control tenant (`:646-657`) have no positive control. **Not covered:** the post-erase check is structurally unable to have one — by then everything it could read has been erased — which is exactly the "erasure succeeded passes while observing nothing at all" case B2's rationale names. **Fix:** narrow the claim to the NFR59 absence result, or give the cleanup fault-probe site its own control. **Owner:** ChatBot Tier-3 harness. **Closure evidence:** the B2 claim states which absence results are control-backed, and the cleanup site either carries a control or records why it cannot.
status: open

### DW-105: [LOW · maintainability] Reusable association components hardcode DOM ids.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
location: ChatBotAssociationReviewActions.razor:4,7,10,47,68,71
severity: low
reason: **[LOW · maintainability] Reusable association components hardcode DOM ids.** `association-actions-title`, `association-decision-note`, `association-correction-title`, `association-correction-rationale` (`ChatBotAssociationReviewActions.razor:4,7,10,47,68,71`) and `association-comparison-title` (`ChatBotAssociationEvidenceComparison.razor:14`) are fixed strings on parameterised components. **Not covered:** harmless today because each renders once per page. **Owner:** ChatBot UI. **Closure evidence:** ids derived from a component-instance prefix, or a documented single-instance constraint.
status: open

### DW-106: [LOW · maintainability] Three verbatim copies of `CodeRow`/`TextRow` and two of `EvidenceText`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
location: AssociationReview.razor:209
severity: low
reason: **[LOW · maintainability] Three verbatim copies of `CodeRow`/`TextRow` and two of `EvidenceText`.** `AssociationReview.razor:209`, `ChatBotAssociationEvidenceComparison.razor:77,82`, `ChatBotAssociationReviewActions.razor:360,365`, `ChatBotAssociationCandidateRow.razor:57` — each carrying its own near-identical "Story 13.4" comment. **Owner:** ChatBot UI. **Closure evidence:** one shared render-fragment helper.
status: open

### DW-107: [MEDIUM · test-quality] The association CSS conformance assertions are not tied to association selectors.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
location: AssociationReviewComponentContractTests.cs:227-238
severity: medium
reason: **[MEDIUM · test-quality] The association CSS conformance assertions are not tied to association selectors.** `AssociationReviewComponentContractTests.cs:227-238` checks that `chatbot.tokens.css` contains `.chatbot-association-candidate` and, separately, that the file contains `max-width: 48rem`, `forced-colors`, and `prefers-reduced-motion` blocks — with nothing binding those blocks to any association rule. Every association class could lose its responsive and forced-colors handling with the test still green. The sibling `ProjectConversationCss...` fact repeats the identical three assertions against the identical file. **Owner:** ChatBot UI tests. **Closure evidence:** assertions scoped to the rule block containing the association selector, or replaced by a computed-style check on the live render.
status: open

### DW-108: [LOW · test-quality] `css.ShouldNotContain("#")` is both too strict and too weak.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
location: AssociationReviewComponentContractTests.cs:232
severity: low
reason: **[LOW · test-quality] `css.ShouldNotContain("#")` is both too strict and too weak.** It bans every `#` in the file — id selectors, `url(#…)` fragments, `#` inside comments — while the raw-colour check it stands in for misses `rgba(`, `hsla(`, `oklch(`, `color-mix(`, and named colours (`ShouldNotContain("rgb(")` does not match `rgba(`). `AssociationReviewComponentContractTests.cs:232`. **Owner:** ChatBot UI tests. **Closure evidence:** a colour-literal matcher covering the modern colour functions, with `#` restricted to hex-triplet position.
status: open

### DW-109: [LOW · test-quality] `AssociationReviewComponentContractTests` covers five unrelated components.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
location: Program.cs
severity: low
reason: **[LOW · test-quality] `AssociationReviewComponentContractTests` covers five unrelated components.** Four of its eight facts assert on `ProjectConversation`, `ChatBotEmailConversationItem`, `ChatBotApprovalConversationItem`, `ChatBotTaskIntentReviewPanel`, `ChatBotWhyProjectPanel`, `Program.cs`, and the `.csproj`, so a task-intent change fails a test class named for association review. **Owner:** ChatBot UI tests. **Closure evidence:** the unrelated facts moved to their own classes.
status: open

### DW-110: [MEDIUM · correctness] `ResolveEvidenceState` re-derives evidence state by substring keyword sniffing.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
location: AssociationReviewService.cs:296-313
severity: medium
reason: **[MEDIUM · correctness] `ResolveEvidenceState` re-derives evidence state by substring keyword sniffing.** `AssociationReviewService.cs:296-313` matches `unauthorized|restricted|suppressed|redacted|unavailable|expired|stale` case-insensitively against `EvidenceKind + EvidenceReference`, over data the server already labels structurally. **Not covered:** produces false positives (a legitimate "restricted-access" reference is suppressed) and false negatives (a genuinely restricted item without the magic words renders); its single test uses a reference literally named `suppressed-candidate-metadata`. **Owner:** ChatBot UI. **Closure evidence:** the safety net removed in favour of the server label, or its keyword set justified against the server's actual vocabulary.
status: open

### DW-111: [LOW · robustness] An empty or whitespace `AssociationId` renders the header only.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
location: AssociationReview.razor:152-159
severity: low
reason: **[LOW · robustness] An empty or whitespace `AssociationId` renders the header only.** `AssociationReview.razor:152-159` dispatches no load and produces no loading, error, or blocked state. **Owner:** ChatBot UI. **Closure evidence:** an `association-id-required` failure dispatch, or a routing constraint that makes the state unreachable.
status: open

### DW-112: [HIGH · test-integrity] The E2E fixtures for the approval surfaces assert markup the components no longer produce.

origin: migrated from legacy ledger ("Deferred from: code review of canonical story 13.4 — review risky AI actions without losing evidence or authority (2026-08-25)"), 2026-08-26
location: ProjectConversationE2ETests.cs
severity: high
reason: **[HIGH · test-integrity] The E2E fixtures for the approval surfaces assert markup the components no longer produce.** `ProjectConversationE2ETests.cs` carries 136 occurrences of `chatbot-definition-list` — the exact class `Story13DefinitionListMigrationTests` forbids in the components — plus `<time class="chatbot-code">` at `:5229` and `<header class="chatbot-approval-conversation-item__header">` at `:6402`, which the current `ChatBotApprovalConversationItem` renders as a `FluentStack` inside a `FluentCard` with no `<header>` element. `ApprovalQueuePriorityE2ETests.cs:29-31` reads `[data-priority-label]`, an attribute the component never emits (it renders `<span class="chatbot-priority-label">`), and expects `["Critical","High","Low"]` against a contract whose third row is `Medium`. The suites are green because 30 `SetContentAsync` calls and 0 `GotoAsync` mean they assert their own hand-written strings. **Not covered:** legacy Story 13.9 was supposed to replace these fixtures with real renders and only added a parallel real-render suite; no open story owns the retirement. **Owner:** ChatBot UI test architecture. **Closure evidence:** the approval fixtures are deleted or driven from the real components/contracts, so a component change breaks them.
status: open

### DW-113: [MEDIUM · localization] Display formatting for audit history is built in the service layer.

origin: migrated from legacy ledger ("Deferred from: code review of canonical story 13.4 — review risky AI actions without losing evidence or authority (2026-08-25)"), 2026-08-26
location: GovernedOperations.razor:223
severity: medium
reason: **[MEDIUM · localization] Display formatting for audit history is built in the service layer.** `GovernedOperationService.ToAuditHistoryLines` (`:62-69`) composes `"{Phase} · {Decision}/{Outcome} · audit:… · origin:… · correlation:…"`, including the `·` separator and the English sentence `"awaiting post-commit record"`, which `GovernedOperations.razor:223` renders into `<code>`. **Not covered:** the strings cannot be localized, restructured, or tested independently of their punctuation, and they bypass `ChatBotUiTextLocalizer` entirely. **Owner:** ChatBot UI. **Closure evidence:** audit history rendered from structured fields through the localizer.
status: open

### DW-114: [LOW · code hygiene] Five private forks of the same key-value row helpers.

origin: migrated from legacy ledger ("Deferred from: code review of canonical story 13.4 — review risky AI actions without losing evidence or authority (2026-08-25)"), 2026-08-26
location: GovernedOperations.razor:314-328
severity: low
reason: **[LOW · code hygiene] Five private forks of the same key-value row helpers.** Near-identical `CodeRow`/`TextRow`/`TimeRow`/`CodeRowIf` `RenderFragment` templates are duplicated in `GovernedOperations.razor:314-328`, `ChatBotApprovalConversationItem.razor:323-337`, `ChatBotAiActionPreviewSections.razor:186`, `ChatBotTaskIntentReviewPanel.razor:121` and `ChatBotWhyProjectPanel.razor:153-172`, each with a subtly different signature (`string` vs `string?`, `CodeRowIf` present or absent). **Not covered:** the legacy 13.4 migration's stated goal was a shared data-presentation pattern; what shipped will drift. **Owner:** ChatBot UI. **Closure evidence:** one shared row component consumed by all five surfaces.
status: open

### DW-115: [MEDIUM · user-safety] Server problem codes reach the user verbatim and mint unbounded announcement keys.

origin: migrated from legacy ledger ("Deferred from: code review of canonical story 13.4 — review risky AI actions without losing evidence or authority (2026-08-25)"), 2026-08-26
location: GovernedOperations.razor:57-58
severity: medium
reason: **[MEDIUM · user-safety] Server problem codes reach the user verbatim and mint unbounded announcement keys.** `GovernedOperationsEffects.SafeFailureCode` (`:51`) returns `problem.Result?.Code` unchanged; `GovernedOperations.razor:57-58` prints it through `SubmissionFailedBodyTemplate` and interpolates it into `AnnouncementKey`. **Not covered:** the code is never validated against the localized problem-details catalog, so an unknown or newly added server code surfaces as untranslated machine text — the same catalog-wiring asymmetry recorded for the rate-limit cells. **Owner:** ChatBot UI + problem-details catalog. **Closure evidence:** unknown codes fall back to a catalogued generic message, and announcement keys are drawn from a bounded set.
status: open

### DW-116: `.chatbot-conversation-shell` hand-rolled chrome still wraps the `FcPageLayout`/`FcPageHeader` composition. `chatbot.tokens.css:59-99` forces `display:grid` on `__body`, which is a `FluentStack Orientation="Horizontal"` supplying its own flex layout, so the `@media (min-width:900px)` `grid-template-columns` rule at :279-283 is fragile or inert; `Wrap` is left `false` so main and complementary cannot wrap at phone width; and the shell caps at `70rem` while content declares `FcPageLayoutMode.FullWidth`. Pre-existing — the Epic 13 application frame is Story 13.1 (`done`).

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
location: n/a
reason: `.chatbot-conversation-shell` hand-rolled chrome still wraps the `FcPageLayout`/`FcPageHeader` composition. `chatbot.tokens.css:59-99` forces `display:grid` on `__body`, which is a `FluentStack Orientation="Horizontal"` supplying its own flex layout, so the `@media (min-width:900px)` `grid-template-columns` rule at :279-283 is fragile or inert; `Wrap` is left `false` so main and complementary cannot wrap at phone width; and the shell caps at `70rem` while content declares `FcPageLayoutMode.FullWidth`. Pre-existing — the Epic 13 application frame is Story 13.1 (`done`).
status: open
decision: 2026-08-27 Adopt FrontComposer layout — Remove or reduce inner chrome, use FrontComposer and Fluent responsive primitives, and update cross-surface tests.
decision: 2026-08-26 Adopt FrontComposer layout — Remove or reduce inner chrome, use FrontComposer and Fluent responsive primitives, and update cross-surface tests.

### DW-117: Hard-coded element ids (`project-conversation-composer-input`, `-title`, `-error`, `project-conversation-stream-title`) make the conversation components single-instance-only; a second instance on one page yields duplicate ids and cross-wired focus targets.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
location: n/a
reason: Hard-coded element ids (`project-conversation-composer-input`, `-title`, `-error`, `project-conversation-stream-title`) make the conversation components single-instance-only; a second instance on one page yields duplicate ids and cross-wired focus targets.
status: open

### DW-118: `ProjectConversationService.SafeLocale` checks `IsNullOrWhiteSpace` before filtering, so an all-punctuation locale returns an empty string instead of the `"und"` fallback. Unreachable from `CultureInfo.CurrentUICulture.Name` today.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
location: n/a
reason: `ProjectConversationService.SafeLocale` checks `IsNullOrWhiteSpace` before filtering, so an all-punctuation locale returns an empty string instead of the `"und"` fallback. Unreachable from `CultureInfo.CurrentUICulture.Name` today.
status: open

### DW-119: `ProjectConversationService.WireToken` falls back to `value.ToString()` for an enum value outside the declared members, surfacing a raw numeric ordinal as a state or provenance token.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
location: n/a
reason: `ProjectConversationService.WireToken` falls back to `value.ToString()` for an enum value outside the declared members, surfacing a raw numeric ordinal as a state or provenance token.
status: open

### DW-120: The two hub callbacks in `ChatBotProjectConversationWorkspace` use `_ = InvokeAsync(...)` with no disposal guard and no exception observation; a signal arriving during teardown throws an unobserved `ObjectDisposedException`. The subscriber's `Reconnected += async _ =>` is async-void with no try/catch around `_onReconnected`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
location: n/a
reason: The two hub callbacks in `ChatBotProjectConversationWorkspace` use `_ = InvokeAsync(...)` with no disposal guard and no exception observation; a signal arriving during teardown throws an unobserved `ObjectDisposedException`. The subscriber's `Reconnected += async _ =>` is async-void with no try/catch around `_onReconnected`.
status: open

### DW-121: `ProjectConversationReducers.ReduceFailed` writes `SubmissionErrorCode` from a *load* error (conflating two different failures) and sets `StreamingErrorCode` in the same reducer that nulls `Conversation`, making that value unreachable because the streaming line only renders inside `@if (Conversation is { } ...)`.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
location: n/a
reason: `ProjectConversationReducers.ReduceFailed` writes `SubmissionErrorCode` from a *load* error (conflating two different failures) and sets `StreamingErrorCode` in the same reducer that nulls `Conversation`, making that value unreachable because the streaming line only renders inside `@if (Conversation is { } ...)`.
status: done 2026-08-27
resolution: already resolved: Commit 011261ef302aba9d3045f03d456562c089c6886d; src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationReducers.cs:209-218 preserves Conversation on load failure, with tests/Hexalith.ChatBot.UI.Tests/ProjectConversationStateTests.cs:27-61 proving the prior view survives.

### DW-122: [MEDIUM · missing-capability] The evidence drawer specified by EXPERIENCE.md does not exist on the association surface.

origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
location: ChatBotEvidenceChip.razor:105-113
severity: medium
reason: **[MEDIUM · missing-capability] The evidence drawer specified by EXPERIENCE.md does not exist on the association surface.** EXPERIENCE.md Component Patterns requires an evidence drawer that "expands source evidence without forcing users to read the full email thread" and redacts inaccessible details, and specifies that an evidence chip "click or keyboard activation opens the supporting evidence when permitted". No drawer or expansion exists anywhere on `/association-review/{id}`; `ChatBotEvidenceChip.ActivateAsync` (`ChatBotEvidenceChip.razor:105-113`) invokes an `OnActivate` callback that neither call site binds. **Deferred by decision 6 of the 2026-08-25 Story 13.3 review:** that review unnests the chips and drives `CanOpenEvidence` from `evidence.State` so the surface stops advertising an action it cannot perform; building the drawer is scoped to its own story. **Owner:** ChatBot UI. **Closure evidence:** a drawer that opens permitted evidence from a chip by pointer and keyboard, with redaction preserved for Redacted/Unauthorized/Unavailable states, verified on the live route.
status: open
decision: 2026-08-27 Build evidence drawer — Implement permitted evidence loading and drawer presentation with pointer and keyboard activation plus fail-closed redaction states.
decision: 2026-08-26 Build evidence drawer — Implement permitted evidence loading and drawer presentation with pointer and keyboard activation plus fail-closed redaction states.

### DW-123: [MEDIUM · reliability] Keycloak owner-marker liveness is pid-namespace-local and carries no host discriminator.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk B — Server and AppHost product surface (2026-08-25)"), 2026-08-26
location: src/Hexalith.ChatBot.AppHost/Program.cs:342-355
severity: medium
reason: **[MEDIUM · reliability] Keycloak owner-marker liveness is pid-namespace-local and carries no host discriminator.** `owner.marker` records only `{pid}:{startTime}` (`src/Hexalith.ChatBot.AppHost/Program.cs:342-355`), and `IsKeycloakRealmOwnerAlive` resolves it with `Process.GetProcessById` (`:420-430`). **Not covered:** on a host where the temp directory is shared across pid namespaces (a mounted `/tmp` in CI containers) or survives a reboot (Windows `%TEMP%`), a foreign pid can either alias a live local process — sparing an abandoned directory forever — or mismatch and cause a live directory to be deleted. **Why deferred:** the marker scheme is a strict improvement on the modification-time proxy it replaced, and adding a boot-id/machine-id discriminator is a separate design step rather than a fix to this change. **Owner:** ChatBot AppHost composition. **Closure evidence:** the marker records a namespace-independent owner token and the sweep treats any unrecognised token as live, with a test for each direction.
status: open

### DW-124: [MEDIUM · evidence-integrity] The per-lane freshness ceiling is keyed on a contract-author-controlled lane name with no allowlist.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk C — evidence gate tool, policy and CI (2026-08-25)"), 2026-08-26
location: ProvenanceAttestor.cs:110
severity: medium
reason: **[MEDIUM · evidence-integrity] The per-lane freshness ceiling is keyed on a contract-author-controlled lane name with no allowlist.** `ProvenanceAttestor.cs:110` and `StoryEvidenceValidator.cs:828` resolve the ceiling from `contract.results[].lane`, and the binding cross-checks that verify selector/trx/sources against the policy binding run only for `requiredClasses` (`StoryEvidenceValidator.cs:309-350`); `TrxEvidenceReader.ReadDefinition` (`:265`) applies no lane-name allowlist. **Not covered:** a story with `primaryPaths: []` that touches nothing recovery-related can name an ordinary lane `recovery-primary` and inherit the relaxed current-run window for arbitrary TRX content. **Why deferred:** this is the pre-existing lane-resolution shape rather than something the per-lane ceiling introduced, and the exposure depends on which direction the open ceiling decision takes. **Owner:** ChatBot story-evidence gate. **Closure evidence:** lane names carrying an override are allowlisted or bound-verified regardless of `requiredClasses`, with a test that a contract naming `recovery-primary` without the corresponding trigger is refused.
status: open

### DW-125: [LOW · robustness] `OutOfMemoryException` is caught in `when` filters in the sanitizer and the summarizer.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk C — evidence gate tool, policy and CI (2026-08-25)"), 2026-08-26
location: RecoveryTrxSanitizer.cs:57
severity: low
reason: **[LOW · robustness] `OutOfMemoryException` is caught in `when` filters in the sanitizer and the summarizer.** `RecoveryTrxSanitizer.cs:57` and `RecoveryAttemptSummarizer.cs:118`. **Not covered:** OOM is not reliably recoverable, the exception filter runs before unwinding, and the XML reader may be left mid-state; if the new size caps work the catch is unreachable, and if they do not it will not save the process. **Owner:** ChatBot story-evidence gate. **Closure evidence:** the size caps are the sole defence and the OOM filters are removed, or a documented rationale replaces them.
status: open

### DW-126: [HIGH · data-integrity] Two routed governance surfaces present hard-coded fixture data as live product data.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk C — evidence gate tool, policy and CI (2026-08-25)"), 2026-08-26
location: GovernedOperations.razor:254-262
severity: high
reason: **[HIGH · data-integrity] Two routed governance surfaces present hard-coded fixture data as live product data.** `GovernedOperations.razor:254-262` embeds six fake queue rows (`item:ambiguous-001`, `operations-admin`, …); `ChatBotApprovalQueuePriorityView.razor:127` binds the entire priority table to `ChatBotApprovalQueuePriorityContract.CreateDefault()`, a design-time fixture (`sha256:11aa`, `requester:requester-1`) whose batch action is hard-coded `DisabledWithReason` (`:88`) so no approval is ever possible there. `GovernedOperations.razor:86-90` additionally asserts `"age>0 risk:any confidence:any …"`, `"priority desc, item-ref asc"` and `"page-size:100"` as if they described the live query — the only real filter is `SelectedQueueFamily` and there is no pagination. The data layer behind it is not ready either: `OperationalDashboardService` returns `ChatBotHealthStatus.Unknown` with `depth: 0` placeholder views (`:51-54`). **Decision (2026-08-26):** split out of canonical 13.4, which makes only its own new AI Action Review route real; these surfaces are labelled in-product as sample data in the interim. **Owner:** ChatBot UI + operational query backend. **Closure evidence:** both surfaces bound to real queries, with the stated filter/sort/page metadata derived from the query actually issued.
status: open
decision: 2026-08-27 Keep open for backend
decision: 2026-08-26 Keep open for backend

### DW-127: [HIGH · build] The solution does not build at `HEAD`, so `Hexalith.ChatBot.Architecture.Tests` cannot be run at all.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15 patch application (2026-08-26)"), 2026-08-26
location: src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:198
severity: high
reason: **[HIGH · build] The solution does not build at `HEAD`, so `Hexalith.ChatBot.Architecture.Tests` cannot be run at all.** `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor:198` dispatches `PreviewAssociationDecisionAction`, a type defined nowhere in `src/` or in any `references/*` submodule — checked at both the pre- and post-bump gitlinks, so the four submodule moves in the Story 12.15 delta did not cause it. It was introduced by Story 13.3 (`0f3bfc8`) and is present at `476f1de`, i.e. it predates this delta. `Hexalith.ChatBot.Architecture.Tests` `ProjectReference`s `Hexalith.ChatBot.UI`, so the whole architecture suite is unbuildable. **Not covered:** the story's claimed `Architecture 78/0/0` verification is not reproducible at `HEAD`, and the two architecture guards changed by the 2026-08-26 patch application — the pairwise deadline ladder and the per-lane ceiling coverage — were verified by replicating their assertion logic against the real `ci.yml`, `release.yml` and `story-evidence-policy.json` rather than by executing them. That is derived evidence, not an executed test. **Owner:** Story 13.3 / ChatBot UI. **Closure evidence:** `dotnet build Hexalith.ChatBot.slnx --configuration Release` succeeds and `Hexalith.ChatBot.Architecture.Tests` runs green, at which point both guards should be executed and their counts recorded.
status: done 2026-08-26
resolution: already resolved: Commit 90c84cd7 removed the undefined PreviewAssociationDecisionAction dispatch; src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewActions.cs:20,26 and AssociationReview.razor:370,373 now use defined request/confirm actions.

### DW-128: [MEDIUM · test-design] `AddEventStoreAdmin`'s port-guard call site is pinned by a method-body source scan, not by execution.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15 patch application (2026-08-26)"), 2026-08-26
location: ChatBot AppHost composition
severity: medium
reason: **[MEDIUM · test-design] `AddEventStoreAdmin`'s port-guard call site is pinned by a method-body source scan, not by execution.** `AddEventStoreAdminBodyInvokesThePortGuard` slices the `AddEventStoreAdmin` method text and asserts the guard call appears inside it, which is strictly stronger than the previous whole-file substring and strictly weaker than composing the method. **Not covered:** invoking `AddEventStoreAdmin` for real needs a `HexalithChatBotResources` plus two `IResourceBuilder<ProjectResource>` instances — a full topology — so a refactor that keeps the call text but stops reaching it would still pass. **Owner:** ChatBot AppHost composition. **Closure evidence:** a test that composes `AddEventStoreAdmin` on a builder with colliding `Dapr:InternalGrpcPorts` and no preceding `AddHexalithChatBot`, asserting it throws.
status: open

### DW-129: No repository workflow lints the GitHub Actions files or the checked-in shell scripts, so workflow and script regressions are caught only when a run fails.
origin: spec-deferred 38290798d6e7
location: .github/workflows
source_spec: `spec-release-workflow-safety.md`
severity: low
reason: `.github/workflows/` contains no actionlint or shellcheck step; the spec's Verification section runs actionlint locally only, and shellcheck is unavailable in this environment. The publication guard and merge-ref boundaries are now safety-critical Bash with no static gate, and the existing `.github/scripts/install-dapr-cli.sh` is equally unguarded, so this predates the current change.
status: open

### DW-130: The required `build` job's ordinary test lanes have no zero-test guard, so a lane that discovers no tests exits 0 and still emits a checksummed TRX that reads as real evidence.
origin: spec-deferred 5bdedb5737f4
location: .github/workflows/ci.yml:130
source_spec: `spec-release-workflow-safety.md`
severity: medium
reason: `.github/workflows/ci.yml`'s `Test` step runs each of the 13 ordinary lanes as `dotnet test "$project" --no-build --configuration Release --logger trx ...` with no `RunConfiguration.TreatNoTestsAsError=true`. Reproduced in this repository: the same command form with a filter matching nothing prints "No test matches the given testcase filter" and exits 0, still writes the TRX, so `sha256sum` succeeds and `executed` still reaches 13. The `build` loop is unchanged by this story -- the gap predates it and was surfaced only because the new merge lane adopted the override. The merge lane is `pull_request`-only, so pushes to `main` and the whole release path remain unguarded.
status: open

### DW-131: Follow-up review still recommended for dw-release-workflow-safety after the damping cap was spent
origin: review-budget-followup
location: n/a
source_spec: `spec-release-workflow-safety.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260827-211223-44d7; this entry preserves the lingering recommendation for a deliberate later review.
status: done 2026-08-28
resolution: closed by human decision: The completed follow-up and separately tracked concrete residuals make the generic reminder redundant.
decision: 2026-08-28 Close review reminder — The completed follow-up and separately tracked concrete residuals make the generic reminder redundant.

### DW-132: A cancelled workflow token makes the coordinators' filtered catch rethrow the canonical evidence-write failure, so a deadline-killed live recovery run produces neither an unmeasurable report nor a ret
origin: spec-deferred cb692f129574
location: src/Hexalith.ChatBot.Server/Audit/ContinuityDrillCoordinator.cs:231
source_spec: `spec-retention-failure-marker.md`
severity: medium
reason: All three coordinators guard the retention fallback with `catch (Exception) when (cancellationToken is { IsCancellationRequested: false })`. The filter predates this story and is deliberately documented, and the marker is specified to be emitted only after both `RecordAsync` attempts fail -- so the current behaviour is spec-conformant. It nonetheless leaves the workflow-timeout path (the 265m SIGINT ladder in ci.yml / release.yml, i.e. the failure most likely to lose evidence) with no artifact-level reason at all: the gate reports plain `<job>:missing_evidence`, which is the exact reconstruction gap this story closed for the non-cancelled path.
status: open

### DW-133: Follow-up review still recommended for dw-retention-failure-marker after the damping cap was spent
origin: review-budget-followup
location: n/a
source_spec: `spec-retention-failure-marker.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260827-211223-44d7; this entry preserves the lingering recommendation for a deliberate later review.
status: done 2026-08-28
resolution: closed by human decision: The completed follow-up and separately tracked concrete residual make the generic reminder redundant.
decision: 2026-08-28 Close review reminder — The completed follow-up and separately tracked concrete residual make the generic reminder redundant.

### DW-134: RecoveryValidationTopologyContractTests.PrepareKeycloakRealmImportWritesTheRenderedRealmWithOwnerOnlyPermissionsAtAnUnpredictablePath fails in a full IntegrationTests run while passing in isolation.
origin: spec-deferred c71f5c5b5a39
location: tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs:377
source_spec: `spec-dw-52-loss-path-rpo-evidence.md`
severity: medium
reason: The test asserts that exactly one new `{temp}/hexalith-chatbot-keycloak-*` subdirectory appears, but a sibling test in the same class creates four such directories, so the delta assertion sees 2 in a whole-assembly run. Reproduced at baseline dc194c7 with every DW-52 change stashed (280 tests, same single failure), so it is pre-existing and untouched by this story; both files are absent from this diff.
status: done 2026-08-28
resolution: already resolved: tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs:367-379 identifies the generated directory by this process's owner marker, excluding sibling-test directories before asserting a single candidate.

### DW-135: Only the controlled-loss stage writes a failed attempt summary, so the gate's `latest_attempt_incomplete` branch stays unreachable when the projection-rebuild or scoped-outage stages throw.
origin: spec-deferred 2a66d370a301
location: tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:274
source_spec: `spec-dw-52-loss-path-rpo-evidence.md`
severity: low
reason: LiveContinuityAspireE2eTests wraps RunControlledLossAndRetainAsync in a catch that writes `LatestAttemptCompletedSuccessfully: false`; the stages that run after it still propagate without one, so a hosted failure there leaves no attempt summary for the gate to read. Pre-existing shape — no stage had such a summary before this story added one for the new stage.
status: open

### DW-136: Follow-up review still recommended for dw-loss-path-rpo-evidence after the damping cap was spent
origin: review-budget-followup
location: n/a
source_spec: `spec-dw-52-loss-path-rpo-evidence.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260827-211223-44d7; this entry preserves the lingering recommendation for a deliberate later review.
status: done 2026-08-28
resolution: closed by human decision: The completed follow-up and separately triaged concrete residuals make the generic reminder redundant.
decision: 2026-08-28 Close review reminder — The completed follow-up and separately triaged concrete residuals make the generic reminder redundant.
