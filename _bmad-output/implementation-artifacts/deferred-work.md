# Deferred Work

### DW-1: [MEDIUM · PR integration] Exact-head story evidence does not test the GitHub pull-request merge result.

status: done 2026-08-27
resolution: resolved by sweep bundle dw-release-workflow-safety
resolution-undo: 62b87e5b2b0f5cd98de7e31b41ffb34f03070488e12da505081d09776a9959b0 2026-08-27 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-24, chunk E docs/ADRs/CI)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-2: [MEDIUM · release ordering] Per-commit live-recovery concurrency can release newer and older commits out of order.

status: done 2026-08-27
resolution: resolved by sweep bundle dw-release-workflow-safety
resolution-undo: 62b87e5b2b0f5cd98de7e31b41ffb34f03070488e12da505081d09776a9959b0 2026-08-27 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-24, chunk E docs/ADRs/CI)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-3: [LOW · code hygiene] Duplicated read-model-key helpers (`IntakeReadModelKeys`, `AttachmentIndexKeyFor`, `AreIntakeReadModelsAbsentAsync`, `RemainsIntakeReadModelsAbsentAsync`) independently added to both `AspireRecoverySandboxOperations.cs` and `AspireScopedOutageOperations.cs`.

status: done 2026-08-28
resolution: resolved by sweep bundle dw-shared-recovery-read-model-keys
resolution-undo: ebaf85466a0a206803a4214c894364d6aff422801162719b04c4820660feb084 2026-08-28 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-4: [LOW · design invariant] Graph scoped-outage lane hardcodes `RecoveryPhase` notification identity at every call site with no `CheckpointPhase` concept.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-5: [MEDIUM · cleanup integrity] Cleanup methods only null tracked note/intake refs when `complete=true`, leaving stale refs after a partial cleanup failure; no test exercises "cleanup fails, then the next scenario runs".

status: done 2026-10-23
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-6: [LOW · cleanup integrity] `_checkpointNoteRefs.Add(noteRef)` records a note before its durable-commit wait confirms — intentional per this diff's "preserve cleanup metadata before first mutation" fix, but cleanup's handling of a never-actually-committed ref isn't explicitly tested.

status: done 2026-10-23
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-7: [MEDIUM · verification strategy] Most of this diff's Aspire ops-harness rewrite (fault/observe/recover/cleanup logic in both `Aspire*Operations` classes) is reachable only through `LiveContinuityAspireE2eTests`, gated behind `HEXALITH_CHATBOT_TIER3=1` and not rerun in this pass — unlike the pieces extracted into static/internal helpers (e.g. `StopReachedDependencyBoundary`), which got real always-run unit tests.

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, independent /bmad-code-review pass)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-8: [MEDIUM · NFR41] Identity/Graph scope stamps still minted via post-hoc `/scope-observation`. Reconfirmed on ops-harness chunk.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
location: existing NFR41 / evidence-integrity residual
severity: medium
reason: **[MEDIUM · NFR41]** Identity/Graph scope stamps still minted via post-hoc `/scope-observation`. Reconfirmed on ops-harness chunk. **Release-claim impact:** NFR41 figures remain provisional (sandbox self-timing). **Owner:** existing NFR41 / evidence-integrity residual. **Closure evidence:** honest product monitoring clocks or explicit unmeasurable.
status: open
triage: 2026-11-24 Skipped. A Graph/Identity subset of DW-45. Product monitoring observations are unavailable on the scoped-outage lane.

### DW-9: [MEDIUM · RPO bound] Continuity `LastCommittedAtUtc` is harness `UtcNow` after projection wait, not an EventStore commit stamp. Entangled with green-path Zero-RPO residual.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-10: [LOW · contract tests] Ops-harness contract tests omit route/lane/composer guards.

status: done 2026-10-23
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-11: [LOW · topology path] `RecoverySandboxTopologyComposer` uses brittle relative `../../tests/...` from AppHost.

status: done 2026-10-23
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-12: [LOW · test identity] Recovery validator passwords duplicated in token provider and Identity probe.

status: done 2026-10-23
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk integration-tests/ops-harness)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-13: [HIGH · `RV-REBUILD-WORM`] Projection rebuild still identity-writes WORM/governed views and excludes them via `SourceDigestsOnly`.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
severity: high
archived: 2026-11-24

### DW-14: [HIGH · `RV-REBUILD-WORM`] Governed/WORM rebuild cannot diverge.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
severity: high
archived: 2026-11-24

### DW-15: [HIGH · `RV-MEASURABLE-CEILING`] E2E `RestorationTimeout = 3 minutes` vs A10/NFR56 `MaxRto` 4 hours.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
location: Architecture/DevOps
severity: high
reason: **[HIGH · `RV-MEASURABLE-CEILING`] E2E `RestorationTimeout = 3 minutes` vs A10/NFR56 `MaxRto` 4 hours.** Reconfirmed. **Release-claim impact:** do not cite this lane as confirming RTO ≤ 4 hr. **Owner:** Architecture/DevOps. **Closure evidence:** per existing entry.
status: open
triage: 2026-11-24 Skipped. Same full-window limitation and closure criteria as DW-58.

### DW-16: [MEDIUM · NFR41] Scoped-outage missing/degenerate monitoring → driver unmeasurable conversion still open. Ops reject bad stamps; driver path to audited unmeasurable remains.

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-17: [MEDIUM · RPO semantics] Green-path RPO hard-coded `TimeSpan.Zero` when no loss detected.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-18: [MEDIUM · dataset] ~~`DatasetVolume: 6` vs actual compared resources.~~ Closed 2026-08-04.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-19: [LOW · rebuild] `ReconstructCapturedEvent` placeholders outside structural digest tuple.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-20: [MEDIUM · gate producer] In-process gate evaluation remains smoke while gate trusts producer summary fields.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk live-drivers)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-21: [MEDIUM · evidence sink] Double-fail `RetainAsync` returns unmeasurable with no disk artifact.

status: done 2026-08-28
resolution: resolved by sweep bundle dw-retention-failure-marker
resolution-undo: b71f090ec811d8068dd7424e412d83c32baf6450975c0a62f208b21297d18f7d 2026-08-28 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk server)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-22: [LOW · options] `Enabled = false` skips environment/tenant/secret/path validation.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk server)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-23: [LOW · sanitization] `IsSafeArtifactLocator` rejects substrings `token`/`secret`/`password`.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-04, chunk server)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-24: [LOW · gate fixtures] Gate fixtures publish `rpo: 0` under `met` without asserting non-citability of constant-zero RPO.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 4+5+5b)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-25: [LOW · architecture methodology] `AppHostTopologyTests` StrictEnum/sidecar assertions are source-substring greps.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 4+5+5b)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-26: [LOW · ReplayTenantPolicy] Control-name hole `replay-test:tenant-alpha` → physical `tenant-alpha` remains untested in SweepVocabularyTests.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 4+5+5b)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-27: [MEDIUM · hygiene D12] ~~EventStore gitlink disclosure was stale.~~ Closed 2026-08-24.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 4+5+5b)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-28: [LOW · platform] Windows skips symlink-ancestry / immutable digest / executable-bit gate tests.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3cd)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-29: [LOW · hardening] Unbounded JSON/TRX/scope file reads and JSON depth.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3cd)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-30: [LOW · reason taxonomy] Broad reuse of `scope_digest_mismatch`/`status_mismatch` for I/O and CLI errors via subjects.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3cd)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-31: [LOW · test harness] `GateFixture.FindPolicy` parent-walk discovery.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3cd)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-32: [MEDIUM · story-evidence bash] Fail-closed bash paths and event bounds lack executable negative coverage.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3ab)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-33: [LOW · story-evidence policy] `aspire-dapr` pathPatterns are repo-wide `*Dapr*`/`*Aspire*` globs.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3ab)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-34: [LOW · architecture methodology] ADR prose and substring greps remain the Task 8 methodology.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3ab)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-35: [LOW · live-recovery checkout] Scheduled live-recovery jobs omit exact-head / producer-head sidecars.

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 3ab)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-36: [MEDIUM · gate producer summary] Gate still trusts producer-written `LatestAttemptCompletedSuccessfully` and `AlertsDeliveredByJob`.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 2)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-37: [LOW · AppHost cleanup] Temp realm cleanup is only on `AppDomain.ProcessExit`.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 2)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-38: [LOW · sandbox fixture] Committed Keycloak realm includes `recovery-validator` / `recovery-validator-pass`.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 2)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-39: [MEDIUM · `RV-PROVIDER-SCALE`] ~~`datasetVolume: 6` still overstates materialized rebuild inputs.~~ Closed 2026-08-04.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1d)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-40: [MEDIUM · `RV-EVIDENCE-KINDS`] Missing logs/traces/metrics/state-end-state evidence kinds remain a platform residual.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1d)"), 2026-08-26
location: Architecture/DevOps with observability
severity: medium
reason: **[MEDIUM · `RV-EVIDENCE-KINDS`] Missing logs/traces/metrics/state-end-state evidence kinds remain a platform residual.** Chunk 1d owes only stamping `RV-EVIDENCE-KINDS` on manifests (patch); producing the artifacts stays deferred. **Release-claim impact:** as on the existing `RV-EVIDENCE-KINDS` entry. **Owner:** Architecture/DevOps with observability. **Closure evidence:** per existing residual.
status: open
triage: 2026-11-24 Skipped. Required residual stamps already ship. The outstanding artifact collection is tracked in DW-59.

### DW-41: [LOW · Aspire/E2E options] `EventStoreDurableStateProbe` uses fixed 30s/250ms/10s bounds unbound from `LiveRecoveryValidationOptions.RestorationTimeout`.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1d)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-42: [LOW · continuity end-state] Actor-state probe only checks `events:1` + `MailboxMessageIntakeCaptured` suffix.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1d)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-43: [MEDIUM · `RV-PROVIDER-SCALE`] NFR59 unauthorized-mutation / cross-tenant flags remain structurally unreachable for the four sandbox deps.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1c)"), 2026-08-26
location: provider-integration / `RV-PROVIDER-SCALE`
severity: medium
reason: **[MEDIUM · `RV-PROVIDER-SCALE`] NFR59 unauthorized-mutation / cross-tenant flags remain structurally unreachable for the four sandbox deps.** Fault/commit pairs are mutually exclusive by stub design; foreign-tenant effect writes are blocked by `Authorized`. Already covered by chunk 1b decision 2. **Release-claim impact:** do not treat sandbox Contained NFR59 flags as product isolation proof. **Owner:** provider-integration / `RV-PROVIDER-SCALE`. **Closure evidence:** per existing residual.
status: open
triage: 2026-11-24 Skipped. Requires product-composed dependency faulting and real authorization/end-state measurements under DW-57.

### DW-44: [MEDIUM · `RV-PROVIDER-SCALE`] NFR58 `scope_escape` remains unreachable on the four-dep happy path.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1c)"), 2026-08-26
location: `RV-PROVIDER-SCALE`
severity: medium
reason: **[MEDIUM · `RV-PROVIDER-SCALE`] NFR58 `scope_escape` remains unreachable on the four-dep happy path.** Exercises emit the canonical signal; the monitor maps that signal to the ExpectedScope value. **Release-claim impact:** sandbox Contained must not be cited as product scope-containment proof. **Owner:** `RV-PROVIDER-SCALE`. **Closure evidence:** per existing residual.
status: open
triage: 2026-11-24 Skipped. Requires a product scope observation path under DW-57. The sandbox scope mapping is fixed by design.

### DW-45: [MEDIUM · chunk 1a/1b carry] NFR41 stamps are sandbox self-timing (control POST / `UtcNow` + 200 ms poll), not product monitoring evidence.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1c)"), 2026-08-26
location: Story 12.15 evidence-integrity (driver + monitor)
severity: medium
reason: **[MEDIUM · chunk 1a/1b carry] NFR41 stamps are sandbox self-timing (control POST / `UtcNow` + 200 ms poll), not product monitoring evidence.** Continues the open driver/monitor honesty residual. **Release-claim impact:** NFR41 figures remain provisional. **Owner:** Story 12.15 evidence-integrity (driver + monitor). **Closure evidence:** honest observation clocks or explicit unmeasurable when product monitoring is absent.
status: open
triage: 2026-11-24 Skipped. The scoped-outage lane has no independently published product-monitoring clocks. The current gate requires NFR41 measurements.

### DW-46: [LOW · Aspire continuity ops] Subscription simulator restore does not reset absolute processing counters.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1c)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-47: [MEDIUM · `RV-PROVIDER-SCALE` · decision 2] Four generic scoped deps remain sandbox-contract Contained.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1b)"), 2026-08-26
location: performance/capacity and provider-integration owners
severity: medium
reason: **[MEDIUM · `RV-PROVIDER-SCALE` · decision 2] Four generic scoped deps remain sandbox-contract Contained.** Administrator chose option 2 (2026-08-03): keep provisional-live sandbox evidence; claim narrowing recorded on the existing `RV-PROVIDER-SCALE` entry (see 2026-08-03 update). **Release-claim impact:** as on that entry. **Owner:** performance/capacity and provider-integration owners. **Closure evidence:** product-composed client paths for the four tokens under the same gate.
status: open
triage: 2026-11-24 Skipped. Requires product-composed provider fault seams under DW-57. The approved sandbox contract remains provisional.

### DW-48: [HIGH · `RV-MEASURABLE-CEILING`] Tier-3 E2E sets `RestorationTimeout = 3 minutes` while A10/NFR56 `MaxRto` remains 4 hours.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1b)"), 2026-08-26
location: Architecture/DevOps
severity: high
reason: **[HIGH · `RV-MEASURABLE-CEILING`] Tier-3 E2E sets `RestorationTimeout = 3 minutes` while A10/NFR56 `MaxRto` remains 4 hours.** Already tracked as `RV-MEASURABLE-CEILING`; reconfirmed in chunk 1b against `LiveContinuityAspireE2eTests`. A recovery between 3m and 4h becomes `unmeasurable`, never a measurable miss. **Release-claim impact:** do not cite this lane as confirming RTO ≤ 4 hr. **Owner:** Architecture/DevOps. **Closure evidence:** per existing `RV-MEASURABLE-CEILING` entry.
status: open
triage: 2026-11-24 Skipped. Duplicate of the full-window recovery limitation in DW-58.

### DW-49: [HIGH · `RV-REBUILD-WORM`] Live E2E still passes the same `SourceRecords` instance to seed and rebuild driver.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1b)"), 2026-08-26
severity: high
archived: 2026-11-24

### DW-50: [MEDIUM · workflows chunk / group 3] Independent `live-recovery-evidence-gate` job is skipped when validation fails.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-03, chunk 1b)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-51: [MEDIUM · chunk 1b/1c] Missing/degenerate scope-monitoring evidence cannot be fail-closed from the live scoped-outage driver alone.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-02, chunk 1a)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-52: [HIGH · governance + residual] No-loss continuity RPO stays `TimeSpan.Zero` by decision (2026-08-02 option 2).

status: done 2026-08-28
resolution: resolved by sweep bundle dw-loss-path-rpo-evidence
resolution-undo: 4be3e8d5ebe7beed69bd12b04996eb96a1f527879240e2ec3555232d1c620697 2026-08-28 7374617475733a206f70656e
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-02, chunk 1a)"), 2026-08-26
severity: high
archived: 2026-11-24

### DW-53: [HIGH · `RV-REBUILD-WORM` · WORM/governed rebuild fidelity] Projection rebuild claim-narrowing (2026-08-02 option 2) leaves WORM/governed resources off the proven equivalence path.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-02, chunk 1a)"), 2026-08-26
severity: high
archived: 2026-11-24

### DW-54: [HIGH · `RV-EXT-M365` · external M365/Graph fidelity] The live subscription and Graph scenarios use the topology-composed Worker/provider simulator, not Microsoft Graph.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: M365 integration / production validation owner
severity: high
reason: **[HIGH · `RV-EXT-M365` · external M365/Graph fidelity] The live subscription and Graph scenarios use the topology-composed Worker/provider simulator, not Microsoft Graph.** The Story 12.15 implementation exercises `GraphMailboxIntakeWorker`, expired-subscription recovery, independent DAPR read-model sentinels, and EventStore actor-state end-state assertions. Retained hosted evidence for this simulator-backed exercise now exists — see release run `33066358280` / evidence `01M11EYSDMP1ZF38B7KZA1A6FA` (2026-08-27) in `.decision-log.md` — but it does not, and cannot, prove production Graph behavior; this residual is about fidelity to the real Microsoft Graph service, not about whether a hosted run occurred. **Release-claim impact:** A10 remains provisional; no sandbox result may claim external-M365 disaster-recovery equivalence. **Owner:** M365 integration / production validation owner. **Closure evidence:** a retained production-shaped pre-production drill against a real Graph test tenant that exercises subscription expiry/renewal, webhook/reconciliation, throttling, permissions, and end-state assertions through the same fail-closed evidence gate.
status: open
triage: 2026-11-24 Skipped. Requires an approved Microsoft Graph test tenant, permissions and retained external drill evidence.
decision: 2026-08-27 Build Graph drill — Create an approved test-tenant drill covering subscription expiry, renewal, webhook reconciliation, throttling, permissions, and retained end-state evidence.
decision: 2026-08-26 Build Graph drill — Create an approved test-tenant drill covering subscription expiry, renewal, webhook reconciliation, throttling, permissions, and retained end-state evidence.

### DW-55: [HIGH · `RV-DURABLE-WORM` · durable audit-storage fidelity] Projection rebuild and audit-store fault validation still use `InMemoryWormAuditStore` or a test-hosted `IAuditWriter`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: durable WORM/audit infrastructure owner, coordinated with Story 12.16 where applicable
severity: high
reason: **[HIGH · `RV-DURABLE-WORM` · durable audit-storage fidelity] Projection rebuild and audit-store fault validation still use `InMemoryWormAuditStore` or a test-hosted `IAuditWriter`.** The repaired rebuild executes real persisted projection writes/read-back/cleanup, but it does not prove durable WORM/KMS/storage recovery or product audit composition. **Release-claim impact:** do not claim durable audit-chain disaster recovery or storage/KMS outage coverage. **Owner:** durable WORM/audit infrastructure owner, coordinated with Story 12.16 where applicable. **Closure evidence:** retained live evidence from the production binding showing append/read failure, fail-closed mutation behavior, recovery, chain verification, rebuild equivalence, and cleanup against durable storage.
status: open
triage: 2026-11-24 Skipped. Requires the approved durable WORM/KMS production binding and retained outage and restore evidence.
decision: 2026-08-27 Build durable binding — Bind an approved durable WORM and KMS store and retain failure, recovery, chain-verification, rebuild, and cleanup evidence.
decision: 2026-08-26 Build durable binding — Bind an approved durable WORM and KMS store and retain failure, recovery, chain-verification, rebuild, and cleanup evidence.

### DW-56: [HIGH · `RV-PROD-CONTROL` · production orchestration and replica coordination] The destructive scheduler runs isolated GitHub/Aspire test jobs, not a production AKS/multi-replica control plane.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: deployment/platform reliability owner
severity: high
reason: **[HIGH · `RV-PROD-CONTROL` · production orchestration and replica coordination] The destructive scheduler runs isolated GitHub/Aspire test jobs, not a production AKS/multi-replica control plane.** The scheduled lane is serialized within its own concurrency group; release validation is deliberately concurrent per commit so required checks are not cancelled. The run proves allowlisted Aspire resource commands and restoration in a single local topology. **Release-claim impact:** do not claim Kubernetes/platform recovery automation, distributed lease safety, replica coordination, or production resource-control authorization. **Owner:** deployment/platform reliability owner. **Closure evidence:** an approved pre-production platform drill with least-privilege resource authority, distributed lease/non-overlap proof, multi-replica behavior, bounded restoration, health/end-state verification, and retained gate evidence.
status: open
triage: 2026-11-24 Skipped. Requires an approved multi-replica pre-production platform and bounded control permissions.
decision: 2026-08-27 Build platform drill — Add an approved pre-production multi-replica drill with least-privilege controls, distributed non-overlap, restoration, health, and retained evidence.
decision: 2026-08-26 Build platform drill — Add an approved pre-production multi-replica drill with least-privilege controls, distributed non-overlap, restoration, health, and retained evidence.

### DW-57: [MEDIUM · `RV-PROVIDER-SCALE` · provider, product-composition, and workload scale] Local provider/component contract implementations and the configured six-record baseline do not represent product DI composition, production volume, latency, replicas, quotas, or regional failure modes.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: performance/capacity and provider-integration owners
severity: medium
reason: **[MEDIUM · `RV-PROVIDER-SCALE` · provider, product-composition, and workload scale] Local provider/component contract implementations and the configured six-record baseline do not represent product DI composition, production volume, latency, replicas, quotas, or regional failure modes.** Story 12.15 proves bounded contract behavior only. The baseline is **not** fully materialized: of its six categories, only `sourceRecords` and `wormAuditRecords` become real entities that are seeded, projected or compared; `governedCommands`, `approvals`, `policySnapshots` and `attachmentMetadata` are parsed into inert records that are never observed. Evidence no longer presents the configured corpus as exercised coverage: `configuredDatasetVolume` carries the six-record provenance, continuity and scoped-outage manifests publish `datasetVolume: 0`, and projection rebuild publishes the actual compared-resource count. Retained hosted evidence of this same bounded, non-production-shaped contract behavior now exists — see release run `33066358280` / evidence `01M11EYSDMP1ZF38B7KZA1A6FA` (2026-08-27) in `.decision-log.md` — but it is hosted proof of the same local-provider/sandbox-scale limitations described below, not a step toward production-shaped coverage; that gap is unaffected. **Update 2026-08-02:** the round-2 evidence-integrity remediation genuinely materialized `attachmentMetadata` — `attachment-processing` now runs through the real `AttachmentCaptureCoordinator` against a seeded projection candidate — though the candidate is exercise-seeded rather than sourced from the dataset file's own attachment record. `governedCommands`, `approvals`, and `policySnapshots` remain unmaterialized as described above. **Update 2026-08-03 (chunk 1b decision 2):** Contained for `ai-provider`, `command-execution`, `audit-store`, and `attachment-processing` is **sandbox-contract** evidence only — Aspire ops `fault`/`process`/`restore` the recovery-sandbox and trust that JSON (including NFR41 stamps and safety flags); ChatBot's real dependency clients are not the faulted seam. Independent control succeeding during `audit-store` is expected under this harness shape and must not be read as product audit fail-closed proof. **Update 2026-08-04:** the manifest/gate split closes the evidence-labeling defect only; it does not materialize the remaining categories or close the scale residual. **Release-claim impact:** local provider/rebuild timings cannot be advertised as production performance or capacity evidence; do not claim product DI composition, real AI-provider outage behavior, product command-execution path, or ChatBot audit-store fail-closed mutation from the Tier-3 sandbox Contained sweep for those four tokens. **Owner:** performance/capacity and provider-integration owners. **Closure evidence:** retained load-profile evidence at an approved production-shaped dataset and replica count, including product-composed provider paths (ChatBot client seams for the four tokens), quota/latency/failure modes, target measurements, isolation/end-state assertions, and the same manifest provenance.
status: open
triage: 2026-11-24 Skipped. Requires an approved production workload, replica and failure profile with retained product evidence.
decision: 2026-08-27 Build production-shaped lane — Exercise product-composed clients at an approved dataset, replica count, quota, latency, and regional failure profile with retained end-state evidence.
decision: 2026-08-26 Build production-shaped lane — Exercise product-composed clients at an approved dataset, replica count, quota, latency, and regional failure profile with retained end-state evidence.

### DW-58: [HIGH · `RV-MEASURABLE-CEILING` · the lane cannot reach the targets it is cited for] The Tier-3 restoration budget is 180 seconds against a 4-hour RTO (A10/NFR56) and a 4-hour rebuild target (NFR57).

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: src/Hexalith.ChatBot.Server/Audit/LiveRecoveryValidationOptions.cs
severity: high
reason: **[HIGH · `RV-MEASURABLE-CEILING` · the lane cannot reach the targets it is cited for] The Tier-3 restoration budget is 180 seconds against a 4-hour RTO (A10/NFR56) and a 4-hour rebuild target (NFR57).** `LiveRecoveryValidationOptions.Validate()` places no lower bound on `RestorationTimeout` relative to `MaxRto`, so any genuine recovery between 3 minutes and 4 hours converts to `unmeasurable`, never `missed`. Round 5 removed the companion `PerScenarioTimeout >= MaxRto` rule, which claimed to "permit measurement through the recovery target" while being unsatisfiable — nine serial scenarios at 4 hours each cannot fit a sub-`RunnerBudget` workflow — and replaced it with a serial fair-share bound; it deliberately did **not** add a `RestorationTimeout` floor, because any such number is invented (see the round-2 note that removed an earlier attempt). Each manifest publishes `MeasurableRecoveryCeilingSeconds` and the gate emits a non-blocking `{job}:{key}:target_exceeds_measurable_ceiling` claim limitation, so the limit is disclosed rather than hidden — but it is not enforced at ratification. A shorter budget is a deliberate trade: a 4-hour restoration window is impractical on every scheduled run. **Release-claim impact:** a pass from this lane is evidence for recovery within 180 seconds only; it must never be cited as confirming RTO ≤ 4 hr or rebuild ≤ 4 hr, and A10 ratification from it is bounded accordingly. **Owner:** Architecture/DevOps, with performance/capacity. **Closure evidence:** either a lane whose restoration budget reaches `RecoveryTargets.MaxRto` with retained evidence of a measurable miss and pass either side of the boundary, or a separately evidenced pre-production drill that measures the full window, plus a ratification rule that rejects a candidate artifact carrying the claim-limitation token for the target being ratified. [`src/Hexalith.ChatBot.Server/Audit/LiveRecoveryValidationOptions.cs`, `LiveRecoveryValidationEvidenceGate.cs`]
status: open
triage: 2026-11-24 Skipped. Requires a separately approved full-window lane and retained pass and measurable-miss evidence.
decision: 2026-08-27 Build full-window evidence — Add a separately scheduled full-window pre-production lane and reject ratification artifacts whose measurable ceiling is below the claimed target.
decision: 2026-08-26 Build full-window evidence — Add a separately scheduled full-window pre-production lane and reject ratification artifacts whose measurable ceiling is below the claimed target.

### DW-59: [MEDIUM · `RV-EVIDENCE-KINDS` · the retained evidence bundle carries no logs, traces, metrics or state-store end-state] The manifest requires only `test-output` and `reports`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
location: src/Hexalith.ChatBot.Server/Audit/RecoveryValidationEvidenceManifest.cs
severity: medium
reason: **[MEDIUM · `RV-EVIDENCE-KINDS` · the retained evidence bundle carries no logs, traces, metrics or state-store end-state] The manifest requires only `test-output` and `reports`.** Task 2 and the "Evidence is retained and reviewable" obligation originally required links to raw CI artifacts, logs, traces, metrics, state-store end-state and test output; `RecoveryValidationEvidenceManifest` requires two kinds, and both workflows upload `TestResults` only. Requiring the other four did not make them exist — it guaranteed every manifest carried syntactically valid links to nothing, which `IsSafeArtifactLocator` (a syntax check) cannot detect. Round 5 narrowed the obligation to what the lane produces rather than leaving the documents asserting six. **Release-claim impact:** a reviewer investigating a failed or disputed run has the `.trx` and the retained reports/manifests only; there is no trace, metric or state-store end-state evidence to corroborate a verdict, so a disputed measurement cannot be independently reconstructed from the artifact. **Owner:** Architecture/DevOps with observability. **Closure evidence:** the lane emits OTLP traces and metrics plus a state-store end-state dump into `TestResults`, both workflows upload them, the manifest requires the full vocabulary again, and a test asserts each required locator resolves to a file the workflow actually produces. [`src/Hexalith.ChatBot.Server/Audit/RecoveryValidationEvidenceManifest.cs`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`]
status: open
triage: 2026-11-24 Skipped. Requires live collection of sanitized telemetry, state snapshots and verified hosted artifact retention.

### DW-60: [LOW] `TierThreeParallelValidationShouldRetainEveryNonCancellationSiblingFailure` is a flaky concurrency test.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-61: [MEDIUM] The live-recovery evidence gate never asserts that a manifest's `ResidualIds` covers the scenario's required residual set.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-62: [MEDIUM] The `RV-*` entries' "Release-claim impact: do not claim …" constraints have no enforcement point.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-01)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-63: [HIGH · pre-existing production binding] The scheduled controls still read and emit only through process-local defaults.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-21)"), 2026-08-26
location: CommandGatewayServiceCollectionExtensions.cs:103,113,164-168,228-229
severity: high
reason: **[HIGH · pre-existing production binding] The scheduled controls still read and emit only through process-local defaults.** `CommandGatewayServiceCollectionExtensions.cs:103,113,164-168,228-229` binds the outbound trace, derived store, WORM store, and operator-alert sink to in-memory implementations. The hosted proof therefore demonstrates scheduler invocation but not durable production evidence, and its empty default stores can report zero breaches without exercising production tenant data. Story 12.16 explicitly owns the live Hexalith.Memories derived-store binding; the WORM/outbound-trace/alert production bindings need an equally explicit owner before “continuous production enforcement” or production release-gate claims are accepted.
status: open
triage: 2026-11-24 Skipped. The scheduler remains dependent on process-local outbound trace, WORM and alert bindings. Production providers and tenant evidence are prerequisites.
decision: 2026-08-27 Build durable bindings — Assign owners and implement production bindings for outbound trace, derived store, WORM audit, and operator alerts with production-data scheduler evidence.
decision: 2026-08-26 Build durable bindings — Assign owners and implement production bindings for outbound trace, derived store, WORM audit, and operator alerts with production-data scheduler evidence.
resolution-note: Story 12.16 replaces only the Production derived-store and vector-reindex defaults with validated Hexalith.Memories service/client adapters. This entry stays open because the outbound-trace, WORM-audit, and operator-alert sinks remain process-local and the live Aspire acceptance lane was not executable without the configured mailbox client secret.

### DW-64: [MEDIUM · pre-existing runtime scaling] Cadence ownership and status are process-local.

origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-21)"), 2026-08-26
location: PeriodicEnforcementRuntime.cs:181-193,321-323,690-718
severity: medium
reason: **[MEDIUM · pre-existing runtime scaling] Cadence ownership and status are process-local.** `PeriodicEnforcementRuntime.cs:181-193,321-323,690-718` keeps last-run state, cadence partitions, and non-overlap guards in memory. Restarts reset the evidence window and horizontally scaled replicas can execute the same global sweep and duplicate writes/alerts. The current story explicitly modeled the in-memory once-per-day guard, so distributed leasing/durable status belongs to the deployment/scale owner before the server runs with multiple replicas.
status: open
triage: 2026-11-24 Skipped. Requires a shared cadence store and distributed fencing through audit and alert side effects plus replica validation.
decision: 2026-08-28 Build distributed cadence — Introduce a durable fenced lease and cadence-status store and verify restart and multi-replica non-overlap.
decision: 2026-08-28 Build distributed cadence — Introduce a durable fenced lease and cadence-status store and verify restart and multi-replica non-overlap.

### DW-65: [HIGH · owned by Story 12.16] The periodic correction-propagation SLO-deadline sweep (AC1 item 5) was never built.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
severity: high
archived: 2026-11-24

### DW-66: [MEDIUM · mitigated 2026-07-31; durable fix owed to `Hexalith.Builds`] `Hexalith.EventStore.Gateway` is missing from the shared package catalog.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-67: [RESOLVED 2026-07-31 — retained for history] The `Hexalith.Tenants` submodule bump in commit `30aa887` broke the super-repo build.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
archived: 2026-11-24

### DW-68: [LOW · pre-existing clock assumption] `LastSucceededAtUtc` is not monotonic.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-69: [MEDIUM · pre-existing test defect, not in this story's diff] `AiActionRiskClassifierTests.UnknownActionClassShouldFailClosedWithoutSerializingInvalidEnumValues` is flaky at ~1%.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, adversarial re-review of eaee04f..30aa887)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-70: [MEDIUM · coupled to the distributed cadence-lease item above] The now-deterministic derived-store sentinel id makes the isolation probe fail-open under concurrent sweeps.

status: done 2026-08-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, second adversarial re-review of eaee04f..working tree)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-71: [LOW · pre-existing, outside the solution] `Hexalith.ChatBot.Aspire.Tests` fails on a stale hardcoded source path and nothing runs it.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, second adversarial re-review of eaee04f..working tree)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-72: [LOW · deliberate design, unbounded against a hanging store] Probe cleanup has no deadline.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-14-wire-the-m2-audit-and-recovery-runtime-scheduler (2026-07-31, second adversarial re-review of eaee04f..working tree)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-73: [LOW · pre-existing, Story 13.4 scope] Null `facet.Health` NREs in the status-summary badge mapper.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21, adversarial re-review of commits e91503b+97d31a6)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-74: [LOW · verify in Story 13.9] Blocked-state reason heading may sit as an `<h2>` peer of the conversation-stream section title.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21, adversarial re-review of commits e91503b+97d31a6)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-75: [RESOLVED in this review — was: MEDIUM pre-existing test flake] Story 13.1's live-render acceptance test is flaky — NOT an app defect.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21)"), 2026-08-26
archived: 2026-11-24

### DW-76: [RESOLVED — was: MEDIUM · verify-when-live-green] MessageBar aria-live / announcement contract now DOM-verified.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21)"), 2026-08-26
archived: 2026-11-24

### DW-77: [MEDIUM · residual, Story 13.8 scope] Fixture-based forced-colors/visual E2E still assert `.chatbot-status__label`.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-78: [LOW] Retrospective doc conflates multi-commit "13.1 review" corrections.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 13-1-establish-one-working-fluent-frontcomposer-application-frame (2026-07-21)"), 2026-08-26
severity: low
archived: 2026-11-24

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
triage: 2026-11-24 Skipped. Workspace CI enforcement belongs to the separately tracked Hexalith.CiCd adoption and Builds ownership work.

### DW-81: [LOW] Partial/standalone checkout rough edges in path-resolution conventions.

origin: migrated from legacy ledger ("Deferred from: code review of 1-1e-centralize-nuget-package-reference-version-authority (2026-07-18)"), 2026-08-26
location: Hexalith.Timesheets.Server.csproj:12
severity: low
reason: **[LOW] Partial/standalone checkout rough edges in path-resolution conventions.** Timesheets `Hexalith.Timesheets.Server.csproj:12` references `$(HexalithEventStoreRoot)` with no empty-expansion guard (rooted-path project-not-found error when neither nested nor sibling checkout exists); the Timesheets `.gitmodules` path move (`Hexalith.Builds` → `references/Hexalith.Builds`) leaves stale directories/`.git/modules` config in existing clones until `git submodule sync` + re-init; Conversations `Directory.Build.props` sibling-Commons probe checks 1 of the 7 directories its nested branch requires; Builds README consumer-wrapper example uses an `Exists`-guarded import with no fail-closed fallback, unlike `Samples/Module.Directory.Packages.props` and the ChatBot wrapper. Pre-existing pattern conventions extended by this story, all fail-closed-with-confusing-errors rather than fail-open.
status: open

### DW-82: [LOW] Cancel/Stop governed handler validates metadata only, not the target generation.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of story-10.6b (2026-06-21)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-83: The externally committed Hexalith.Builds gitlink is not reproducible from fetched remote refs.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of story-10.6b (2026-06-21)"), 2026-08-26
source_spec: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
archived: 2026-11-24

### DW-84: Externally committed Story 13.9 visual artifacts do not reliably prove dark-mode or settled forced-colors rendering.

origin: migrated from legacy ledger ("Deferred from: code review of story-10.6b (2026-06-21)"), 2026-08-26
location: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
source_spec: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
reason: Externally committed Story 13.9 visual artifacts do not reliably prove dark-mode or settled forced-colors rendering. Evidence: Eighteen baseline-to-HEAD PNGs changed outside this workflow; inspected dark captures render Light/Clair, the French project-conversation dark/light files are byte-identical, and an English forced-colors dashboard capture records a loading state while the generator overwrites tracked PNGs without baseline comparison.
status: open
decision: 2026-08-28 Regenerate and review — Wait for settled dark and forced-colors state, compare against a baseline, regenerate affected PNGs, and present them for explicit acceptance.
decision: 2026-08-28 Regenerate and review — Wait for settled dark and forced-colors state, compare against a baseline, regenerate affected PNGs, and present them for explicit acceptance.

### DW-85: Streaming release-readiness evidence remains source-token and synthetic-fixture based rather than production progressive-render execution.

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of story-10.6b (2026-06-21)"), 2026-08-26
source_spec: _bmad-output/implementation-artifacts/spec-run-all-tests-and-fix-issues.md
archived: 2026-11-24

### DW-86: [LOW · durable activity CT] Workflow activities pass `CancellationToken.None` into EventStore/audit/alert side effects.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6-hosted-dapr-workflow-production-binding-and-saga-readiness-validation (2026-08-09)"), 2026-08-26
location: Story 2.9 / correction-propagation runtime
severity: low
reason: **[LOW · durable activity CT]** Workflow activities pass `CancellationToken.None` into EventStore/audit/alert side effects. **Release-claim impact:** host cancellation cannot abort in-flight activity I/O; acceptable under typical Dapr durable-activity semantics if activities remain idempotent. **Owner:** Story 2.9 / correction-propagation runtime. **Closure evidence:** documented durable-activity cancellation policy, or activity-context token plumbing with idempotent compensation tests.
status: open
decision: 2026-08-28 Document durable completion — Ratify CancellationToken.None for durable side effects, document replay and idempotency policy, and add shutdown and replay tests proving eventual completion is safe.
decision: 2026-08-28 Document durable completion — Ratify CancellationToken.None for durable side effects, document replay and idempotency policy, and add shutdown and replay tests proving eventual completion is safe.

### DW-87: [MEDIUM · terminal failure ownership] Distinct correction-propagation workflow `Failed` status deferred; Story 2.9 keeps `Correction-delayed` for store/soft failures.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 8-6-hosted-dapr-workflow-production-binding-and-saga-readiness-validation (2026-08-09)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-88: [LOW · pre-existing, self-disclosed] `StrictEnumContractResolver` doesn't cover an enum used as a dictionary KEY or a nested collection element (e.g. `List<List<TEnum>>`).

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-12.15 (2026-08-24, chunk A: Server/Audit + AppHost + Client generated)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-89: [MEDIUM · test-design] Generated-client freshness fixture is a tautology.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-90: [LOW · observability] The 40-minute completion reserve refusal has no gate reason code.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-91: [LOW · latent] `recognizedLaneBindings[0]` is hardcoded.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-92: [MEDIUM · governance] `ModelContextProtocol` 1.4.1 → 2.2.0 major bump is unratified.

origin: migrated from legacy ledger ("Deferred from: code review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25)"), 2026-08-26
location: ScaffoldArchitectureTests.cs:273
severity: medium
reason: **[MEDIUM · governance] `ModelContextProtocol` 1.4.1 → 2.2.0 major bump is unratified.** Commit `f804229` accepted a major-version bump of the SDK the M1 MCP surface wraps by editing `ScaffoldArchitectureTests.cs:273` and the `architecture.md` / `epics.md` planning records — all three inside Story 12.15's File List — with no ADR entry and no decision-log record. The pins match the current `references/Hexalith.Builds` catalog, so nothing is drifting; what is missing is the decision. Disclosed as out-of-scope in Story 12.15 by the 2026-08-25 review. Release-claim impact: a major SDK bump under the MCP surface is unreviewed. Closure evidence: an ADR plus decision-log entry owned by the package-authority story.
status: open
triage: 2026-11-24 Skipped. Requires package-owner compatibility ratification and the decision record for the retained MCP surface.
decision: 2026-08-27 Ratify 2.2.0 — Review compatibility impact, add an accepted ADR and decision-log record, and execute MCP adapter conformance coverage.
decision: 2026-08-26 Ratify 2.2.0 — Review compatibility impact, add an accepted ADR and decision-log record, and execute MCP adapter conformance coverage.

### DW-93: [MEDIUM · api-contract] A permanently malformed command payload is reported as a retryable `503` and raises an operator alert on every attempt.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: hosted-lane review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25, second batch)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-94: [MEDIUM · reliability] The ChatBot UI host can still be made unstoppable by an unreachable OTLP collector.

origin: migrated from legacy ledger ("Deferred from: hosted-lane review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25, second batch)"), 2026-08-26
location: ChatBotUiHostDefaultsExtensions.cs:34-37
severity: medium
reason: **[MEDIUM · reliability] The ChatBot UI host can still be made unstoppable by an unreachable OTLP collector.** `ChatBotUiHostDefaultsExtensions.cs:34-37` registers `UseOtlpExporter()` with no bounded telemetry shutdown, and `chatbot-ui` is a composed resource in the same DCP application as the EventStore host that was fixed. Provider disposal joins the batch-exporter thread with no timeout while that thread is parked in the exporter's retry sleep, so `IHost.DisposeAsync` never returns and the resource cannot be stopped. **Constraint:** the fix that exists, `BoundedTelemetryShutdownService`, is `internal sealed` to `Hexalith.EventStore.ServiceDefaults`, so the UI host cannot adopt it without a visibility change in that repository — which is why this is recorded rather than copied. **Owner:** ChatBot UI hosting, with EventStore ServiceDefaults for the visibility decision. **Closure evidence:** the UI host stops cleanly with an unreachable OTLP endpoint, proven the same falsifiable way as the EventStore host (disable the bound, the dispose test fails).
status: done 2026-11-25
resolution: The UI uses EventStore ServiceDefaults for telemetry and retains its catalog meter. The host lifecycle test stops and disposes the DI host while an exporter is blocked.
triage: 2026-11-24 Skipped. The bounded shutdown implementation remains internal to EventStore. The supported reusable platform API is required.
decision: 2026-08-27 Expose platform API — Publish a reusable bounded-shutdown seam from EventStore ServiceDefaults, consume it in ChatBot UI, and add an unreachable-collector test.
decision: 2026-08-26 Expose platform API — Publish a reusable bounded-shutdown seam from EventStore ServiceDefaults, consume it in ChatBot UI, and add an unreachable-collector test.

### DW-95: [MEDIUM · evidence-integrity] The scheduled and release recovery lanes upload `TestResults` without the metadata-only TRX projection.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: hosted-lane review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25, second batch)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-96: [LOW · observability] Three projection log event ids are duplicated within the EventStore projection subsystem.

origin: migrated from legacy ledger ("Deferred from: hosted-lane review of 12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10 (2026-08-25, second batch)"), 2026-08-26
location: Hexalith.EventStore
severity: low
reason: **[LOW · observability] Three projection log event ids are duplicated within the EventStore projection subsystem.** `1120` and `1121` are shared by `ProjectionDiscoveryHostedService` and `ProjectionUpdateOrchestrator`, and `4660` twice within `ProjectionUpdateOrchestrator`. Anything filtering or alerting on event id conflates them. They predate this work and renumbering a shipped id changes what operators' filters match, so `ProjectionLogEventIdUniquenessTests` pins them explicitly and fails on any **new** collision instead. **Owner:** Hexalith.EventStore. **Closure evidence:** ids renumbered with an operator-facing note, and the pinned allowlist emptied.
status: open
decision: 2026-08-27 Renumber with migration — Assign unique ids, publish an operator-facing filter migration note, update tests, and empty the collision allowlist.
decision: 2026-08-26 Renumber with migration — Assign unique ids, publish an operator-facing filter migration note, update tests, and empty the collision allowlist.

### DW-97: [MEDIUM · diagnosis] B4 — the scoped-outage control probe's catch scope is wider than the request it classifies.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-98: [LOW · correctness] C7 — `observedAtLeastOnce` guards nothing it appears to guard.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-99: [LOW · api-hygiene] C10 (second half) — `RecoveryResourceLogTail.Render(string? matching)` exposes a filter no call site uses.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-100: [MEDIUM · test-design] D2 — three of four checkpoint-refusal reason codes are unasserted.

origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
location: Hexalith.EventStore
severity: medium
reason: **[MEDIUM · test-design] D2 — three of four checkpoint-refusal reason codes are unasserted.** `ProjectionCheckpointRefusalDiagnosticsTests` covers `post-cutover-writer-protocol` only. **Not covered:** `delivery-row-requires-fenced-completion`, `delivery-row-schema-regression`, `delivery-row-unsupported` — each needs a delivery row that `ProjectionDeliveryStateClassifier` classifies accordingly, which the current fixture cannot build. A draft covering them was written and **reverted rather than shipped half-working**; the gap is also recorded in the test's own remarks. Its `GetStateAsync<ProjectionDeliveryState?>` arrangement is additionally inert, because production reads through `GetStateAndETagAsync`, so that precondition comes from NSubstitute's default rather than the stub. **Owner:** Hexalith.EventStore.
status: open
triage: 2026-11-24 Skipped. The classifier, checkpoint tracker and existing diagnostics tests are owned by Hexalith.EventStore.

### DW-101: [MEDIUM · test-design] D5 — the capture-filter fix is untested and its token check is approximate.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-102: [LOW · dead-code] D6 — `WaitForGovernedOperationAsync`'s absence path is unreachable.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: 12-15 adversarial batch, stood down by the coordinator (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-103: [MEDIUM · claim-accuracy] The mailbox admission probe's retained warm-up comment describes a barrier the code no longer provides.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk A — Tier-3 harness and live drivers (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-104: [MEDIUM · claim-accuracy] Claim B2's "every absence result" holds at one of three absence call sites.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk A — Tier-3 harness and live drivers (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-105: [LOW · maintainability] Reusable association components hardcode DOM ids.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-106: [LOW · maintainability] Three verbatim copies of `CodeRow`/`TextRow` and two of `EvidenceText`.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-107: [MEDIUM · test-quality] The association CSS conformance assertions are not tied to association selectors.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-108: [LOW · test-quality] `css.ShouldNotContain("#")` is both too strict and too weak.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-109: [LOW · test-quality] `AssociationReviewComponentContractTests` covers five unrelated components.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-110: [MEDIUM · correctness] `ResolveEvidenceState` re-derives evidence state by substring keyword sniffing.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-111: [LOW · robustness] An empty or whitespace `AssociationId` renders the header only.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.3 — Resolve ambiguous association from a safe live review surface (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-112: [HIGH · test-integrity] The E2E fixtures for the approval surfaces assert markup the components no longer produce.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of canonical story 13.4 — review risky AI actions without losing evidence or authority (2026-08-25)"), 2026-08-26
severity: high
archived: 2026-11-24

### DW-113: [MEDIUM · localization] Display formatting for audit history is built in the service layer.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of canonical story 13.4 — review risky AI actions without losing evidence or authority (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-114: [LOW · code hygiene] Five private forks of the same key-value row helpers.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of canonical story 13.4 — review risky AI actions without losing evidence or authority (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-115: [MEDIUM · user-safety] Server problem codes reach the user verbatim and mint unbounded announcement keys.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of canonical story 13.4 — review risky AI actions without losing evidence or authority (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-116: `.chatbot-conversation-shell` hand-rolled chrome still wraps the `FcPageLayout`/`FcPageHeader` composition. `chatbot.tokens.css:59-99` forces `display:grid` on `__body`, which is a `FluentStack Orientation="Horizontal"` supplying its own flex layout, so the `@media (min-width:900px)` `grid-template-columns` rule at :279-283 is fragile or inert; `Wrap` is left `false` so main and complementary cannot wrap at phone width; and the shell caps at `70rem` while content declares `FcPageLayoutMode.FullWidth`. Pre-existing — the Epic 13 application frame is Story 13.1 (`done`).

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
archived: 2026-11-24

### DW-117: Hard-coded element ids (`project-conversation-composer-input`, `-title`, `-error`, `project-conversation-stream-title`) make the conversation components single-instance-only; a second instance on one page yields duplicate ids and cross-wired focus targets.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
archived: 2026-11-24

### DW-118: `ProjectConversationService.SafeLocale` checks `IsNullOrWhiteSpace` before filtering, so an all-punctuation locale returns an empty string instead of the `"und"` fallback. Unreachable from `CultureInfo.CurrentUICulture.Name` today.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
archived: 2026-11-24

### DW-119: `ProjectConversationService.WireToken` falls back to `value.ToString()` for an enum value outside the declared members, surfacing a raw numeric ordinal as a state or provenance token.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
archived: 2026-11-24

### DW-120: The two hub callbacks in `ChatBotProjectConversationWorkspace` use `_ = InvokeAsync(...)` with no disposal guard and no exception observation; a signal arriving during teardown throws an unobserved `ObjectDisposedException`. The subscriber's `Reconnected += async _ =>` is async-void with no try/catch around `_onReconnected`.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
archived: 2026-11-24

### DW-121: `ProjectConversationReducers.ReduceFailed` writes `SubmissionErrorCode` from a *load* error (conflating two different failures) and sets `StreamingErrorCode` in the same reducer that nulls `Conversation`, making that value unreachable because the streaming line only renders inside `@if (Conversation is { } ...)`.

status: done 2026-08-27
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
archived: 2026-11-24

### DW-122: [MEDIUM · missing-capability] The evidence drawer specified by EXPERIENCE.md does not exist on the association surface.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of story-13.2 (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-123: [MEDIUM · reliability] Keycloak owner-marker liveness is pid-namespace-local and carries no host discriminator.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk B — Server and AppHost product surface (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-124: [MEDIUM · evidence-integrity] The per-lane freshness ceiling is keyed on a contract-author-controlled lane name with no allowlist.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk C — evidence gate tool, policy and CI (2026-08-25)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-125: [LOW · robustness] `OutOfMemoryException` is caught in `when` filters in the sanitizer and the summarizer.

status: done 2026-10-22
origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk C — evidence gate tool, policy and CI (2026-08-25)"), 2026-08-26
severity: low
archived: 2026-11-24

### DW-126: [HIGH · data-integrity] Two routed governance surfaces present hard-coded fixture data as live product data.

status: done 2026-11-24
origin: migrated from legacy ledger ("Deferred from: code review of 12-15 chunk C — evidence gate tool, policy and CI (2026-08-25)"), 2026-08-26
severity: high
archived: 2026-11-24

### DW-127: [HIGH · build] The solution does not build at `HEAD`, so `Hexalith.ChatBot.Architecture.Tests` cannot be run at all.

status: done 2026-08-26
origin: migrated from legacy ledger ("Deferred from: code review of 12-15 patch application (2026-08-26)"), 2026-08-26
severity: high
archived: 2026-11-24

### DW-128: [MEDIUM · test-design] `AddEventStoreAdmin`'s port-guard call site is pinned by a method-body source scan, not by execution.

status: done 2026-10-28
origin: migrated from legacy ledger ("Deferred from: code review of 12-15 patch application (2026-08-26)"), 2026-08-26
severity: medium
archived: 2026-11-24

### DW-129: No repository workflow lints the GitHub Actions files or the checked-in shell scripts, so workflow and script regressions are caught only when a run fails.
origin: spec-deferred 38290798d6e7
location: .github/workflows
source_spec: `spec-release-workflow-safety.md`
severity: low
reason: `.github/workflows/` contains no actionlint or shellcheck step; the spec's Verification section runs actionlint locally only, and shellcheck is unavailable in this environment. The publication guard and merge-ref boundaries are now safety-critical Bash with no static gate, and the existing `.github/scripts/install-dapr-cli.sh` is equally unguarded, so this predates the current change.
status: open

### DW-130: The required `build` job's ordinary test lanes have no zero-test guard, so a lane that discovers no tests exits 0 and still emits a checksummed TRX that reads as real evidence.

status: done 2026-10-22
origin: spec-deferred 5bdedb5737f4
source_spec: `spec-release-workflow-safety.md`
severity: medium
archived: 2026-11-24

### DW-131: Follow-up review still recommended for dw-release-workflow-safety after the damping cap was spent

status: done 2026-08-28
origin: review-budget-followup
source_spec: `spec-release-workflow-safety.md`
severity: low
archived: 2026-11-24

### DW-132: A cancelled workflow token makes the coordinators' filtered catch rethrow the canonical evidence-write failure, so a deadline-killed live recovery run produces neither an unmeasurable report nor a ret

status: done 2026-11-24
origin: spec-deferred cb692f129574
source_spec: `spec-retention-failure-marker.md`
severity: medium
archived: 2026-11-24

### DW-133: Follow-up review still recommended for dw-retention-failure-marker after the damping cap was spent

status: done 2026-08-28
origin: review-budget-followup
source_spec: `spec-retention-failure-marker.md`
severity: low
archived: 2026-11-24

### DW-134: RecoveryValidationTopologyContractTests.PrepareKeycloakRealmImportWritesTheRenderedRealmWithOwnerOnlyPermissionsAtAnUnpredictablePath fails in a full IntegrationTests run while passing in isolation.

status: done 2026-08-28
origin: spec-deferred c71f5c5b5a39
source_spec: `spec-dw-52-loss-path-rpo-evidence.md`
severity: medium
archived: 2026-11-24

### DW-135: Only the controlled-loss stage writes a failed attempt summary, so the gate's `latest_attempt_incomplete` branch stays unreachable when the projection-rebuild or scoped-outage stages throw.

status: done 2026-10-28
origin: spec-deferred 2a66d370a301
source_spec: `spec-dw-52-loss-path-rpo-evidence.md`
severity: low
archived: 2026-11-24

### DW-136: Follow-up review still recommended for dw-loss-path-rpo-evidence after the damping cap was spent

status: done 2026-08-28
origin: review-budget-followup
source_spec: `spec-dw-52-loss-path-rpo-evidence.md`
severity: low
archived: 2026-11-24

### DW-137: [HIGH · Story 12.16 blocker] ChatBot has no canonical Memories ingestion result from which to finalize the required correction binding.

status: done 2026-08-28
origin: `spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md`, 2026-08-28
source_spec: `spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md`
severity: high
archived: 2026-11-24

### DW-138: The harness copy of the attachment-index key shape is still a hand-copied twin of the production factory, so a production rename would silently make recovery absence checks vacuously true.

status: done 2026-10-28
origin: spec-deferred 5325ee2eab4f
source_spec: `spec-shared-recovery-read-model-keys.md`
severity: medium
archived: 2026-11-24

### DW-139: The "canonical" intake read-model set covers three keys while an intake also materializes per-intake items and participants entries, so cleanup and cross-tenant isolation checks cannot observe leaks t

status: done 2026-10-28
origin: spec-deferred 9e2be3ea29d2
source_spec: `spec-shared-recovery-read-model-keys.md`
severity: medium
archived: 2026-11-24

### DW-140: LiveProjectionRebuildDriverTests.RebuildUsesOnlyTheSelectedTenantImmutableSourceAndWormChain fails on a write-count assertion and already failed at this story's baseline commit.

status: done 2026-10-28
origin: spec-deferred 3257e0491634
source_spec: `spec-shared-recovery-read-model-keys.md`
severity: medium
archived: 2026-11-24

### DW-141: The integration test project has no clean build configuration in this workspace: the default Debug source-reference mode and the package mode each fail for different pre-existing dependency-drift reas

status: done 2026-10-28
origin: spec-deferred 77e48253b098
source_spec: `spec-shared-recovery-read-model-keys.md`
severity: high
archived: 2026-11-24

### DW-142: Follow-up review still recommended for dw-shared-recovery-read-model-keys after the damping cap was spent

status: done 2026-10-28
origin: review-budget-followup
source_spec: `spec-shared-recovery-read-model-keys.md`
severity: low
archived: 2026-11-24

## Deferred from: code review of 1-1-scaffold-the-buildable-hexalith-chatbot-module (2026-09-13)

Scope reviewed: Story 1.1c re-verification, commit range `8c3dd15~1..9567f43`.

- **Realm containing plaintext credentials written to a predictable, world-readable temp path** (`src/Hexalith.ChatBot.AppHost/Program.cs:193` at the reviewed commit) — already resolved after this range. HEAD uses `Directory.CreateTempSubdirectory`, `UnixCreateMode = UserRead|UserWrite`, an owner-marker file and a stale-directory sweep, guarded by `RecoveryValidationTopologyContractTests`. No action required; recorded so the regression window is traceable.
- **`keyPrefix` isolation is partly inert and asserted by counting substrings** (`src/Hexalith.ChatBot.AppHost/Aspire/ChatBotAspireModule.cs:209`) — the shared `statestore` keeping `keyPrefix: none` is defensible: it is one component shared by the eventstore and tenants actor hosts, so prefixing would not separate them. The AppHost is local-only (`IsPublishable=false`), so the blast radius is a developer's Redis. Unverified: whether `keyPrefix: name` has any effect on `chatbot-workflow-statestore`, where `actorStateStore: true` means DAPR composes actor keys as `appid||actortype||actorid||key` independently of the component key-prefix strategy. A Redis key dump from a live Tier-3 run would settle it. Both guards only count `.WithMetadata("keyPrefix", …)` occurrences in source text and cannot tell which component received which prefix.
- **Workflow hygiene gaps in the new topology-acceptance jobs** (`.github/workflows/ci.yml`, `.github/workflows/release.yml`) — mostly resolved after this range: HEAD pins `dapr init --runtime-version 1.18.0` (the reviewed commit used a bare `dapr init`, letting an upstream DAPR release change ACL/pubsub semantics inside a required gate) and carries `permissions: contents: read`. Remaining: the ~45-line job is duplicated verbatim between the two workflows with no `workflow_call` reuse.
- **`dotnet-version: 10.0.302` contradicts `global.json` 10.0.400** (`.github/workflows/ci.yml:29`) — pre-existing and repo-wide: all six `setup-dotnet` steps in `ci.yml` and every one in `release.yml` hardcode `10.0.302` rather than `global-json-file: global.json`, while `global.json` pins `10.0.400` with `rollForward: latestPatch`. The new topology jobs followed the existing convention rather than introducing the drift. Fixing it is a repo-wide change outside Story 1.1c.

## Deferred from: review of spec-1-1c-aspire-dapr-topology-review-remediation (2026-09-14)

- source_spec: `spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: Workflow string assertions cannot see step ORDER, so the new TRX counter guards would still pass if moved before their own `dotnet test` step or into another job.
  evidence: Real and verified, but a repo-wide pre-existing limitation of `ScaffoldArchitectureTests`' `ShouldContain` approach, not caused by this story. Settling it needs YAML parsing that asserts the guard step's index directly follows the step whose `run` contains its `LogFileName`, within the same job.
- source_spec: `spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: Waits outside the new shared topology budget (`StartupTimeout` 3 min, `SelectedResourceValidationTimeout` 5 min, the 6-minute M2 gate deadline) can still accumulate past the job's `timeout-minutes: 30`.
  evidence: Verified present in `TrivialGovernedCommandAspireE2eTests.cs:92,98,331`. Pre-existing and outside the recorded 2026-09-13 intent, which targeted the seven 5-minute resource-health waits specifically. A whole-test deadline would settle it.
  resolution: Resolved 2026-11-25. The required live and release tests share a 25-minute lane budget, including startup and the browser matrix. Linked cancellation propagates through nested work, and teardown is awaited for diagnostics. Deadline tests cover cancellation, cleanup, and refusing another test after the shared budget expires.
- source_spec: `spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: `accesscontrol.local.yaml` — the only access-control file the repo actually exercises — has no conformance guard that it stays policy-free and local-only.
  evidence: Its entire coverage is two `ShouldContain` strings in `ScaffoldTopologySmokeTests.cs:39-40`. Pre-existing; the production-file guards added by this story deliberately did not extend to it.
  resolution: Resolved 2026-11-24. The parsed local configuration stays policy-free. The app model selects it on the retained AppHost sidecars, and the declared release composition excludes the AppHost and its local configuration. The Dapr conformance and recovery topology test classes passed all 4 and 39 cases.
- source_spec: `spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: The widened per-resource endpoint assertion may fail on URL-activation timing rather than a real fault for `security`, `chatbot-ui` and `eventstore-admin-ui`.
  evidence: maybe-false, would be medium if true. Static evidence cannot establish whether `WaitForResourceHealthyAsync` guarantees a resolved active URL for those three at the moment health flips. A live Tier-3 run recording `ASPIRE_RESOURCE_EVIDENCE` for all seven resources settles it.
- source_spec: `spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: Two review patches need NEW test code and were not written because the solution cannot compile — a behavioral guard for the `KeycloakPersistent` stderr warning, and a test asserting the dead-letter drain's DAPR subscription metadata (`/dapr/subscribe` or `ITopicMetadata`).
  evidence: Both are verified-real medium findings routed to patch. Writing test code that cannot be compiled or run was judged worse than recording them, consistent with the same decision taken for the matrix row 8 gap. Close both in the pass that re-runs the blocked suites once `references/Hexalith.Folders` and the AppHost `WithEventStoreClientCredentials` break are resolved.
  resolution: Resolved 2026-11-25. The real AppHost testing builder emitted one grant-expiry warning with Keycloak persistence enabled and none with it disabled. Both cases passed, and the existing fan-out subscription discovery test covers the dead-letter metadata.

## Deferred from: review of spec-fix-generated-idempotency-helper-non-nullable-enum (2026-09-17)

- source_spec: `spec-fix-generated-idempotency-helper-non-nullable-enum.md`
  summary: Remaining Hexalith.Folders sample and adapter consumers still assign strings to the generated `PathMetadataPathPolicyClass` enum.
  evidence: `samples/Hexalith.Folders.Sample/FolderLifecycleSample.cs:172` still produces CS0029, and CLI/MCP inputs require a deliberate validated string-to-enum conversion rather than a literal substitution. This predates the scoped generator repair and should be completed with focused parsing and solution-build coverage once the nested dependency checkout is available.
- source_spec: `spec-fix-generated-idempotency-helper-non-nullable-enum.md`
  summary: Cross-version retries may derive a new idempotency key after the path-policy class migrates from the legacy free-form value to the closed enum.
  evidence: Maybe-false, medium if confirmed. Both `path_metadata` and `path_policy_class` participate in the helper hash, but the repository does not establish whether a deployed version accepted `governed-mailbox-attachment` operations that can be retried after this contract transition. A replay test across the previous and current client/helper versions plus deployment-history evidence would settle it.
- source_spec: `spec-fix-generated-idempotency-helper-non-nullable-enum.md`
  summary: Mailbox attachments classified `metadata_only` may remain unavailable to later AI-context content reads unless an authoritative policy stage reclassifies them.
  evidence: Maybe-false, medium if confirmed. `metadata_only` is the conservative valid class for arbitrary attachment bytes, while the Folders contract limits content reads to `content_allowed`; current static evidence does not show whether the server or post-scan workflow authoritatively reclassifies the stored path. An end-to-end capture, safe-scan, and authorized Folders content-read test would settle it.

- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: A committed EventStore command can lose its downstream correction or ingestion workflow scheduling obligation.
  evidence: Loop 15 BH2 verified that the canonical baseline already submits the domain command before scheduling the workflow, without durable reconciliation across scheduling failure or process termination. SDK commit proof restores the accepted receipt but cannot establish or repair that pre-existing scheduling obligation; a durable scheduling recovery must not resubmit the domain command.
- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: A fast workflow can publish completion before acceptance initializes its operation-status record and have the report discarded.
  evidence: Loop 15 BH3 verified the baseline status sink returns on a missing operation record, while the baseline dispatcher starts workflows before the gateway initializes status. Preserving early reports requires a status initialization and merge lifecycle that also avoids overwriting terminal progress.
  resolution: Resolved 2026-11-25. The dispatcher carries the accepted operation identity into durable correction work after SDK acknowledgement. Early workflow reports atomically initialize matching status, the scheduling acknowledgement cannot overwrite them, and the later gateway write merges audit status. The dispatcher, status sink and coordinator suite passed 182 cases.
- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: Duplicate replay can overwrite concurrent workflow completion or failure status through a non-atomic read and upsert.
  evidence: Loop 15 BH4 verified the same read/with/upsert sequence exists at the canonical baseline; workflow publication between the replay read and write can lose its terminal state and guidance. An atomic operation-status merge or conditional-update policy must resolve this pre-existing race.
  resolution: Resolved 2026-10-20 in Story 1.5: replay and workflow transforms now run against the latest snapshot inside both stores and every SDK conditional-write retry. `CommandGatewayTests.DuplicateReplayRacingTerminalWorkflowShouldRetainBothLatestFieldsAndExactCount` forces the workflow to lose the first write, advances the clock for the winning replay, then asserts persisted terminal state, reason, next actions, receipt/audit identity and exact count through both stores. Current-run evidence: `TestResults/story-1-5-deferrals/patch-gateway.trx`. Early publication and scheduling recovery remain separately open.

## Deferred from: code review of spec-1-2-publish-the-openapi-contract-spine-and-typed-client (2026-10-05)

- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: Genuinely uncertain external writes (conversation writer, mailbox send, sidecar outage after `MarkExternalEffectAttempted`) remain `RecoveryPending` with no terminal state, operator tool, or named manual recovery command; the dispatch failure still raises `OperatorAlertKind.AuditUnavailable`.
  evidence: Loop 16 finding 3 (medium). Retaining uncertain ownership is spec-mandated; `PossibleExternalWriteShouldRemainRecoveryPendingWithoutRedispatch` asserts 503/Dispatching 26 h later. The audit-unavailable alert classification at `CommandGateway.cs:181` is identical at baseline. `ChatBotProblemDetailsFactory.cs:81` ("no durable state was written") is now stale. An AD-4 named manual recovery command and a dispatch-specific alert kind belong to a Retry Profile story.
- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: After a durable receipt, a thrown post-commit audit write or operation-status upsert escapes `CommandGateway.SubmitAsync` as a raw 500 for a committed command.
  evidence: Loop 16 findings 12/45 (medium), pre-existing; carried in the spec triage log since Loop 7 BH8 (Loops 13 BH3, 14 BH8, 15 BH6) but never recorded in this ledger. `CommandGateway.cs:228-255` matches baseline apart from the `AcceptedAt` copy. Same-ID retries replay the stored receipt; fresh-ID `SubmitAsync` retries of generic commands re-dispatch.
  resolution: Resolved 2026-11-25. After the durable receipt, an audit exception queues reconciliation and a failed status write raises a metadata-only dependency alert. The caller still receives acceptance, and replay restores missing status from the same receipt without dispatching again. Nine focused gateway cases passed, covering caller cancellation and independent task cancellation at both dependencies.
### DW-143: Valid enum-keyed dictionary maps (e.g. `TenantPolicyValue.AiActionLowRiskAllowed`) pass request validation but cannot deserialize because `JsonEnumMemberStringConverter` lacks dictionary-key support, so authorization denies them with 403.

status: done 2026-11-24
origin: legacy ledger entry from Deferred from: code review of spec-1-2-publish-the-openapi-contract-spine-and-typed-client (2026-10-05)
source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
severity: medium
archived: 2026-11-24

- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: Post-commit idempotency-receipt repair (`DaprCoarseIdempotencyStore.ReconcileOutcomeFromAuditAsync` and `RecoverPendingOutcomeAsync`) conflicts with ARCHITECTURE-SPINE AD-2 ("post-commit repair … prohibited", "split idempotency truth"), and no ADR or interim exception records it.
  evidence: Loop 16 finding 23 (medium). The split receipt existed at baseline; spec tasks for durable recovery require the repair machinery. AD-2 notes terminal-idempotency co-commit remains owner-unaccepted under A13, so an explicit interim exception or ADR is the resolution path, not a code change in Story 1.2.
- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: Generic commands are now deduplicated on the caller `commandId`, but every production adapter (UI services, `ChatBotCliService`, `ChatBotMcpService`, `GraphMailboxIntakeWorker`) calls `SubmitAsync`, which mints a fresh ULID per call. CLI/MCP re-runs and deterministic `ComplianceAuditService` escalations therefore re-dispatch where the baseline 60 s body-hash key replayed them.
  evidence: Loop 16 findings 2/49 (medium). `CoarseIdempotencyComposer.cs:101-104` keys generic commands on (tenant, op, commandId) as the spec task requires; `SubmitWithCommandIdAsync` has no production caller. Governed-note/project-conversation bodies are unique per call, specialized commands keep domain dedupe, and POST transport retries are disabled. Reason (human decision 2026-10-05): commandId keying is spec-intended; migrating callers to stable IDs belongs to Stories 1.5/1.10.
- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: Coarse idempotency ownership state has no retention bound. Released tombstones and per-command identity/domain records stay in the Dapr state store permanently, and process-local `_pendingOutcomes`/`_unadmittedReservations` are not evicted on failure paths.
  evidence: Loop 16 findings 4/56/58 (medium). Saves pass `metadata: null` with no `ttlInSeconds` (`DaprCoarseIdempotencyStateClient.cs:30,33`); `TryDeleteAsync` has no caller; tombstones are spec-mandated to prevent delete/recreate ETag reuse. Roughly 2 KB per command ID remains in Redis. Reason (human decision 2026-10-05): a TTL horizon that is safe for ETag fencing (≥ lease + longest replay window + margin) needs its own retention/compaction design pass; the product is pre-release.

## Deferred from: build review of spec-1-2-publish-the-openapi-contract-spine-and-typed-client (2026-10-06)

- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: A typed EventStore domain rejection is answered as a retryable 503 `dispatch-unavailable` (with a `PreCommitOperationReplay` intent and an `AuditUnavailable` alert) instead of a terminal, non-retryable rejection problem, so callers retry a deterministic rejection indefinitely.
  evidence: Loop 17 BH1/EH4/VG-O2 (medium), pre-existing: baseline `CommandGateway.cs:95-111` mapped every `EventStoreGatewayException`, domain rejections included, to the same 503/intent/alert; Loop 16 added only the ownership release. EventStore's actor message-ID dedupe (`AggregateActor.cs:845-858` → `SubmitCommandHandler.ThrowDeterministicFailure`) replays the cached rejection on every same-ID retry. Back-pressure is not cached and should stay retryable. Needs a catalog-backed rejection code/problem type in the OpenAPI and the problem factory.
  triage: 2026-11-24 Skipped. A terminal rejection needs a new public problem type in the OpenAPI document and generated client, so it needs owner sign-off.
- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: Durable recovery ignores authoritative EventStore non-commit evidence; a retained `Dispatching` reservation whose command status is `Rejected` stays `RecoveryPending` (retryable 503) indefinitely, blocking the same caller ID and equivalent specialized requests.
  evidence: Loop 17 BH2/EH3 (medium): `DaprCoarseIdempotencyStore.RecoverPendingOutcomeAsync` acts only on `HasCommittedOutcomeEvidence`. Same defect class as the Loop 16 finding-3 entry (no terminal resolution for retained ownership; human decision 2026-10-05 routes it to a Retry Profile story). A safe automatic release needs a persisted writer-attempt flag on the reservation, because `ExternalEffectAttempted` is process-local and conversation/mailbox writes may precede the EventStore submission.
  triage: 2026-11-24 Skipped. The 2026-10-05 decision assigns this to the Retry Profile story, and it changes the durable reservation schema.

## Deferred from: code review of spec-1-2-publish-the-openapi-contract-spine-and-typed-client (2026-10-07, contract/client split)

- source_spec: `spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: Operation-status identifier `oneOf` alternatives overlap: `TaskId` and `CommandId` have identical ULID schemas, so every valid identifier matches both branches and fails strict JSON Schema validation; the example helper validates only the first branch.
  evidence: Contract/client split finding 7 (medium), pre-existing and repeatedly deferred in the spec from Loop 7 BH10 through Loop 15 EH3. `hexalith.chatbot.v1.yaml:1273` retains the identical alternatives, and `OpenApiContractSpineTests.cs:571` deliberately checks only the first. This entry carries the existing disposition into the ledger; it is not a newly introduced regression. Closure requires an unambiguous identifier schema and a check of the actual composition semantics.
  resolution: Resolved 2026-10-20 in Story 1.5: all four remaining overlapping operation-identifier compositions use their common `CommandId` ULID schema, preserving historical TaskId values. `OpenApiContractSpineTests.OperationIdentifiersShouldValidateTheirCommonUlidShapeAtEveryHistoricalLocation` covers valid/invalid tokens at all four locations; composition controls enforce zero/one/two actual matches. NSwag regenerated byte-identical C# and the SHA-256 fixture remains current. Current-run evidence: `TestResults/story-1-5-deferrals/patch-contracts.trx`.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: Reconcile persisted workflow completion, reason, and next actions after terminal status publication fails.
  evidence: Confirmation review BH3 (medium) verified that a terminal Dapr runtime yields no current schedule and GET clears NextRetryAt but leaves the stored retrying/pending projection. Baseline GET and the throwing status-sink upsert already left stale persisted status; a terminal publication/reconciliation policy must recover those facts without re-enabling stale retries.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-2-publish-the-openapi-contract-spine-and-typed-client.md`
  summary: Qualify SDK-confirmed receipt recovery through a real Redis/Dapr ChatBot process replacement after controlled receipt loss (medium, unverified).
  evidence: Confirmation review BH10 identified the absent fault-injected process-restart lane but demonstrated no current recovery defect. Serialized production-adapter/fresh-service controls and ordinary required live replay pass. Settle the remaining process-boundary question by retaining actual SDK confirmation, losing ChatBot receipt persistence, replacing the process, proving exact canonical recovery without another submission, and separately retaining pending behavior for missing historical confirmation; current fixture checks are not this independent qualification.

## Deferred from: code review of spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context (2026-10-07)

- source_spec: `spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: Owner authority evidence validation tolerates zero clock skew: `IsValidEvidence` requires `ObservedAt <= now` and, for current requests, `ObservedAt >= started` (same for `RevocationCheckedAt`), so a real provider stamping owner-clock times denies every request (owner ahead) or every mutation (owner behind).
  evidence: Edge-case-hunter EC5 at `src/Hexalith.ChatBot.Server/Authorization/ChatBotRequestAuthorizer.cs:270-277`; high if true, unverified because only the unavailable and synthetic providers exist and both stamp ChatBot's `ISystemClock`. Settle in A13 provider design by stating in the `IChatBotOwnerAuthorityProvider` contract which clock stamps evidence, or by adding a bounded skew allowance.
  triage: 2026-11-24 Skipped. Any clock-skew allowance widens the freshness and revocation windows, so the tolerated skew needs a product security decision.
- source_spec: `spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: Creation commands authorize against their new client-generated identifier (`NoteId`, `DraftId`, `IntakeId`, `*ChangeId`), so a real ChatBot owner mapping must either accept any not-yet-existing id (making the check meaningless) or reject every creation.
  evidence: Blind-hunter BH12 at `src/Hexalith.ChatBot.Server/Authorization/ChatBotAuthorityCatalog.cs` resource-property column; medium if true, unverified because owner-mapping semantics for `ChatBot`/`operation` requests are unspecified until A13. Settle by defining whether create rows need a parent/target scope separate from the new id.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: Specify owner authority for compliance escalation targets and machine grant delegation before production mappings can make those references authoritative.
  evidence: C-B3 is medium if true (unverified); EscalationTargetRef currently denotes an opaque project/group target, SendActorId is bound upstream, and outbound authority denies. Settle the target/delegation identity contract in A13 rather than inventing Parties mappings.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: Define owner-backed authority and runtime revocation behavior for tenant activity SignalR subscriptions.
  evidence: C-B8 is a pre-existing ownerless subscription path that broadcasts a tenant-only advisory nudge. Shared tenant binding now rejects ambiguity, but current owner membership/service-grant subscription semantics need a separate catalog and owner-mapping decision.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: Specify a bounded owner-client deadline and caller-cancellation behavior for accepted production authority mappings (medium if true, unverified).
  evidence: R-B9 identifies that ChatBotRequestAuthorizer awaits provider calls with caller cancellation only; the shipped unavailable provider completes synchronously, so no production hang is established. Settle the question in A13 by inspecting each accepted provider/client deadline and proving timeout returns metadata-only unavailable denial while caller cancellation still propagates.

## Deferred from: code review of spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context (2026-10-07 third review, ledgered 2026-10-08)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: Seven of eight Dapr subscription routes (AiOutcome, Approval, GovernedOperation, MailboxIntake, ParticipantResolution, TaskIntent, DeadLetter) accept unauthenticated HTTP with any tenant id; only the association route checks the app-channel token.
  evidence: Third review, pre-existing, at `src/Hexalith.ChatBot.Server/Gateway/ChatBotCompatibilityEndpointExtensions.cs:143`. MailboxIntake's `AssociationProjectionHandler` overload never reaches the invalidation coordinator, so R-B5's narrow scope holds for marker minting. Settle in the shared subscription-ingress follow-up.
  resolution: Resolved before 2026-11-24 by 64afc6a. Every projection route, the dead-letter drain and the subscribe handler require the EventStore sidecar channel. The fan-out tests from 3d63cf9 prove that missing and wrong app tokens return 401 on all eight projection routes.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: All seven projection routes subscribe to the same pubsub/topic without `Match` rules, so the Dapr .NET SDK may keep only the first-mapped route (GovernedOperation) as the topic's default route.
  evidence: Third review, pre-existing; high if true, not verified at runtime (`ChatBotCompatibilityEndpointExtensions.cs:142`). The SDK logs "A default subscription to topic … already exists". Settle by inspecting `/dapr/subscribe` and live delivery in a running sidecar topology.
  resolution: Resolved 2026-11-24. The host subscription discovery confirmed the collision: only the governed-operation route received the shared topic. One sidecar-guarded fan-out route now carries the single subscription with its dead-letter topic and runs every family's shared projection method in mapping order; the seven family routes stay mapped and guarded without subscriptions. Host tests cover discovery, per-family delivery and replay, untrusted ingress, non-object bodies, unbindable families and fault propagation. Server.Tests passed all 3755 cases and AppHostTopologyTests all 18. Live sidecar delivery was not exercised. Review follow-up 7c40c9c keeps later families running past association's token check, warns when a family cannot bind, and covers all seven families plus a CloudEvent-wrapped EventStore envelope. The routing collision is resolved. The review also found that EventStore publishes each event inside `payload` bytes, so the six families that read flattened fields still ignore live events; that is tracked as a separate entry below.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: Internal-producer admission markers expire after 5 minutes while workflow message ids are deterministic, so one late `/process` callback could leave a correction-propagation or invalidation command permanently rejected.
  evidence: Third review; medium if true, not verified (`src/Hexalith.ChatBot.Server/Gateway/DataProtectionChatBotAdmissionMarker.cs:18`). EventStore replays a cached rejection for a reused messageId; `AiExecutionCoordinator` shares the pattern. Settle by measuring submit→`/process` latency bounds and checking whether a retried submission with a fresh marker escapes the cached rejection.
  triage: 2026-11-24 Skipped. Needs a measured submit-to-`/process` latency bound and confirmation of how EventStore records a thrown admission failure.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: The internal-producer marker path has no proof through the real EventStore relay.
  evidence: Third review; medium if true, not verified. `TrustedAuthorityInternalProducerTests.cs:66` builds the `CommandEnvelope` by hand and calls `stage.EvaluateAsync` directly. Settle with a live dispatch→`/process` round trip once the Aspire runtime blocker clears, checking that extensions and the payload digest survive the relay.

## Deferred from: code review of spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context (2026-10-08 resolution pass)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: The recovery sandbox `ai-provider` exercise and the live recovery validation's ai-provider dependency cannot observe a provider fault, because stripped `requester_authority_class` labels route every `ExecuteLowRiskAIAssistance` to approval.
  evidence: Found while fixing the sandbox `CS7036` build break; a known blocker by Jerome's decision (2026-10-08). `DeterministicAiActionRiskClassifier` (`src/Hexalith.ChatBot.Server/Gateway/Stages/DeterministicAiActionRiskClassifier.cs:162`) reads the label from the binding-only authority principal, so the policy returns `risk_not_low_risk` and the dispatcher records `pending-approval`. A scratch harness running the real `RecoveryDependencyExerciseTests` passed command-execution, audit-store and attachment-processing and failed only ai-provider. Settle with owner-backed requester authority (A13) or a deliberately re-scoped exercise.
  status: done 2026-10-08
  resolution: re-scoped by Jerome's fourth-review decision (2026-10-08). The sandbox admission pipeline (`tests/Hexalith.ChatBot.RecoverySandbox/RecoveryDependencyExercise.cs`) now uses `Story132AcceptanceRiskClassifier`, which supplies fixed synthetic `project-contributor` requester authority for `ExecuteLowRiskAIAssistance` only and delegates every other command to the production classifier; production still fails closed on the stripped label. A scratch harness running the real `RecoveryDependencyExerciseTests` passes 4/4, and reverting to the production classifier fails only ai-provider. The live recovery validation's ai-provider dependency uses the same exercise but is not re-run until the Aspire runtime blocker clears.

- source_spec: `spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: Bound exhausted AI-execution recovery listing by tenant and page before loading the complete indexed work inventory.
  evidence: Workflow review W-B8; ReadModelAiExecutionWorkStore.ListExhaustedAsync/ListIndexedAsync scan index pages and tenant items before status/cursor/maximumCount filtering. The same global-scan shape exists at original baseline a8b421b6690ab9c0e66d7274ee5745b26a9e117c. Large inventories impose avoidable operator storage work; settle with tenant/page indexed access and storage-read-count tests without weakening authority expiry checks.
  triage: 2026-11-24 Skipped. Closing it needs a durable per-tenant exhausted index with lazy migration of the existing global pages. That storage-format change needs its own story.

## Deferred from: code review of spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context (2026-10-08 fifth review)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: The W-B1 HTTP fallback-target check asks the ChatBot owner for current `operation` authority on the caller-generated `CommandId` stream, which does not exist yet when one of the 17 allowlisted commands without a canonical dispatch target is admitted.
  evidence: Fifth review (acceptance-auditor) at `src/Hexalith.ChatBot.Server/Authorization/ChatBotRequestAuthorizer.cs:113-120`, with the dispatcher fallback at `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs:1052`. Medium if true, unverified. This is the same create-semantics question as the 2026-10-07 `NoteId`/`DraftId`/`IntakeId`/`*ChangeId` deferral, which does not name `CommandId` fallback targets. Settle it in A13 owner-mapping design by defining whether a fallback create needs parent/target scope separate from the new stream id. Production denies meanwhile through the unavailable owner provider.

## Deferred from: workflow independent review of spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context (2026-10-08, after fifth-review resolution)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context.md`
  summary: The request-context resolver binds any authenticated token whose `preferred_username` is `service-account-<id>` as service client `<id>`, with no client-credentials evidence beyond that username convention.
  evidence: Triage S-E3, pre-existing. Baseline `ClaimsAuthenticationStage` (a8b421b6690ab9c0e66d7274ee5745b26a9e117c, lines 72-76) used the same Keycloak convention, and `ChatBotRequestContextResolver.TryResolve` keeps it while adding conflict checks. Medium if true, unverified. To settle it, confirm whether the configured identity provider realm can issue a human user a `service-account-` prefixed `preferred_username` (or any non-client token carrying one). If it can, require client-credentials evidence (for example a matching `azp`/`client_id` and no human session markers) before machine classification. Machine grants still require owner evidence for that exact client, and production denies meanwhile through the unavailable owner provider.
  triage: 2026-11-24 Skipped. Needs verification of the production identity provider's username rules. Hardening the relay path also needs an EventStore relay contract that carries `azp`.

## Deferred from: Story 1.3 A13 reconciliation review (2026-10-11)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-3-bind-every-request-to-trusted-tenant-and-actor-context-2.md`
  summary: Long-lived tenant, party, or project streams that exceed EventStore's complete-read limits cannot provide current owner authority until a bounded source checkpoint or summary exists.
  evidence: `AuthoritativeEventStreamReader` has an existing 10,000-event and 16 MiB cap and returns `source-bound-exceeded` at the cap; the new owner queries use that reader and deny on this result. This preserves fail-closed behavior but can deny otherwise valid old streams. Resolve with a source-owned checkpoint or summary that retains contiguous provenance, exact request binding, and current revocation.

## Deferred from: Story 1.4 versioned safe outcomes review (2026-10-12)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
  summary: Failure conversation items can still expose sensitive token-shaped category, scope, dependency, or escalation metadata.
  evidence: `ChatBotFailureStateConversationItem.razor` displays these projected fields; `FailureStateProjectionTranslator.SafeOptionalToken` checks shape only. This display existed at Story 1.4 baseline. Audit the producer provenance and replace any sensitive field with a fixed safe catalog value.
  triage: 2026-11-24 Skipped. No production path writes these fields today, because `FailureStateProjectionHandler` has no route or registration. Category, scope and dependency also need a product allowlist.
  triage: 2026-11-25 Still blocked. The live fan-out fix covers the six existing producers. Failure-state projection still requires a producer and product rules for safe category, scope, dependency and escalation metadata.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
  summary: The gateway redaction stage copies correlation and task identifiers without validating their provenance.
  evidence: `CoarseUserFacingRedactionStage.Apply` copied both fields at Story 1.4 baseline. HTTP middleware validates incoming identifiers, but internal callers should be traced before deciding whether the stage needs its own validation.
  resolution: Resolved 2026-11-24. The gateway replaces an invalid or missing correlation ID and omits invalid optional task IDs. Valid IDs retain their ULID value in canonical wire form. SafeOutcomeProblemCompatibilityTests passed all 31 cases.
### DW-144: Operation status serialization throws when stored completion or audit enum text is unknown.

status: done 2026-11-24
origin: legacy ledger entry from Deferred from: Story 1.4 versioned safe outcomes review (2026-10-12)
source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
severity: medium
archived: 2026-11-24

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
  summary: Unverified medium risk: distinguish a fresh blocked workspace failure from repeated polling when the component uses a fixed stable identity.
  evidence: ChatBotBlockedState deduplicates by StableId, Reason and catalog code; fixed workspace callers supply no attempt identity. A live repeated-failure trace with distinct attempted operations would establish whether the second failure is incorrectly silent.
  triage: 2026-11-24 Skipped. Needs a UX decision on whether a user's repeated same-code retry should announce in the blocked workspace.

## Deferred from: code review of spec-1-4-return-versioned-safe-outcomes (2026-10-12)

- Unknown completion or audit text still throws before the generic-denial path (`OperationStatusHttpResults.cs:29`). Pre-existing parser. The fallback state still needs the schema and forward-compatibility decision already recorded above.
- Failure items still render category, scope, dependency, and escalation fields (`ChatBotFailureStateConversationItem.razor:45`). Pre-existing projected fields; same exposure as the earlier Story 1.4 deferral.
  triage: 2026-11-24 Skipped. Same latent exposure and allowlist decision as the 2026-10-12 failure-metadata entry above.
- Story commit `abcb637` advances the EventStore, FrontComposer, and Memories gitlinks. Maybe-false, medium if those pointer moves are unnecessary. Settle by building this story against the previous commits `1b7b8ce`, `c7491c7`, and `b16ab01`.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
  summary: Unverified medium risk: unconditional solution sibling entries may expand the Release package-mode build beyond the existing AppHost source closure.
  evidence: Compare exact Release project graphs at the story baseline and current revision with UseHexalithProjectReferences=false; the source inventory alone does not establish which additional projects would newly build. The existing AppHost already carries direct sibling resource references.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
  summary: CLI and MCP operation-status output omits the primary reason code and catalog headline/reason even when the status contains a known failure reason.
  evidence: The omission predates Story 1.4. A separate output-contract decision should define which existing status shapes gain catalog copy and how clients consume additive fields.

## Deferred from: code review of spec-1-4-return-versioned-safe-outcomes (2026-10-13)

### DW-145: Failure card can say retry is allowed when the action was withheld.

status: done 2026-11-24
origin: legacy ledger entry from Deferred from: code review of spec-1-4-return-versioned-safe-outcomes (2026-10-13)
severity: medium
archived: 2026-11-24

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
  summary: Reconcile known terminal failure codes with independently stored resolved statuses in failure conversation items.
  evidence: D-B3; the baseline already rendered Item.FailureStatus beside terminal_failure catalog copy. Define producer state/code compatibility before changing history rendering.
  resolution: Resolved 2026-11-25. Terminal events suppress contradictory resolution and retry metadata, including independently supplied summary facets. Four render cases cover terminal failure, exhausted retry and genuine reprocess history.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
  summary: Give distinct association-review attempts or route identities distinct announcement keys for repeated same-code failures.
  evidence: D-B5; the baseline already deduplicated by the fixed association-review-failed identity plus unchanged error code. Trace association navigation and retry focus behavior, then verify the chosen attempt identity in a mounted page.
  resolution: Resolved 2026-11-24. `AssociationReviewState.FailureActivation` now advances on every recorded load, decision and correction failure (stale loads excluded). The failure banner passes it as `AnnouncementActivation` under its fixed key, and the summary focus guard keys on code plus activation, so each same-code retry announces and takes focus once while re-renders and remounts stay quiet. The mounted-page test `SafeOutcomeInteractionTests.AssociationReviewRetryShouldAnnounceAndFocusEachSameCodeFailure` retries a same-code load failure with and without an intermediate loading render and fails without the activation or with the code-only focus guard; reducer tests pin the counter. UI.Tests passed 634/634, and SafeOutcomeInteractionTests (14), VersionedSafeOutcomeRenderTests (96), ProjectConversationWorkspaceLifecycleRenderTests (1) and RealRenderAssociationEvidenceE2ETests (5) passed.

## Deferred from: code review of spec-1-4-return-versioned-safe-outcomes (2026-10-14, src chunk)

- AI execution recovery returns a bare `404 {code:"safe-not-found"}` (`src/Hexalith.ChatBot.Server/Lifecycle/AiExecution/AiExecutionRecoveryEndpoints.cs:34`). Pre-existing: the response is not problem+json, has no catalog code and no catalog version, so this public failure path bypasses the versioned safe-outcome catalog.
  resolution: Resolved 2026-11-24. Both AI execution recovery routes now build their failures through `IChatBotProblemDetailsFactory` with the request correlation: unauthorized, blank, unknown and lapsed keys return the read routes' catalog-backed safe-not-found problem+json (403 `authorization_denied` with the catalog version, replacing the bare 404), and an undecodable cursor returns the catalog validation problem (400 `command_contract_invalid`). Store-call gating is unchanged, and no UI, Client, CLI or MCP caller uses these routes. Host tests compare each hidden outcome byte-for-byte with a read route's safe-not-found response and cover the bad cursor without store access. Server.Tests passed all 3756 cases.
- Suppressed status banners keep `role="alert"`/`"status"` with `aria-live="off"` (`src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor:106`). Maybe-false, medium if true: a remounted suppressed alert may still announce, which would defeat once-per-failure deduplication. Settle with NVDA and JAWS on Chrome against a remounted suppressed banner.
  resolution: Resolved 2026-11-24. `ChatBotStatusBanner` now derives `role` from the effective politeness, like `aria-live`: an announcing banner keeps `alert` or `status`, and a deduplicated one renders no role with `aria-live="off"`. The failure item's hidden announcement span follows the same rule. Chromium confirmed the premise: it still exposes an inserted `role="alert"` with `aria-live="off"` as an alert in its accessibility tree. The browser test `VersionedSafeOutcomeBrowserTests.RemountedSuppressedBannerShouldDropItsLiveRole` fails a governed note on the real UI host, records every banner render with a MutationObserver, remounts the banner twice in the same circuit, and checks the DOM and Chromium's accessibility tree (no alert role, no live region). It fails without the fix. NVDA and JAWS were not run; the Chromium accessibility tree stands in for them. UI.E2E.Tests passed 298/298 with 0 skipped, and UI.Tests passed 634/634.
- A failure item whose code or state changes under the same `ItemId` is never announced (`src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor:90`). Maybe-false, medium if true. Settle by checking whether failure-state projections update one conversation item in place or append a new item per transition.
  resolution: Verified 2026-11-24 as not a defect. Each failure transition is stored under a new item id (`failure:{operation}:{kind}:{version}`), so the stream announces it as a new item. Only a same-version redelivery with a different catalog code replaces an item in place, and that is not a state transition.


## Deferred from: Story 1.4 workflow review (2026-10-14)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
  summary: Preserve instance-specific backend retry permissions through code-only UI failure state.
  evidence: F-B4/F-E4; the baseline effects already discarded Retryable and ClientAction and enabled non-authorization load retries. Define the state fields and rendering intersection before changing this pre-existing behavior; a typed audit_unavailable with Retryable=false/ClientAction=None currently retains catalog retry guidance.
  triage: 2026-11-24 Skipped. Needs a UX decision on how the backend `Retryable` and `ClientAction` flags combine with the catalog action.
### DW-146: Define malformed typed-problem category handling at the UI failure state boundary.

status: done 2026-11-24
origin: legacy ledger entry from Deferred from: Story 1.4 workflow review (2026-10-14)
source_spec: `_bmad-output/implementation-artifacts/spec-1-4-return-versioned-safe-outcomes.md`
severity: medium
archived: 2026-11-24

## Deferred from: code review of 1-6-canonical-lifecycle-state-model-and-transition-enforcement (2026-10-15)

- source_spec: `_bmad-output/implementation-artifacts/1-6-canonical-lifecycle-state-model-and-transition-enforcement.md`
  summary: `CommandSubmissionLifecycleTransitionGuard` always validates the constant `Received->Proposed` edge, so the invalid-transition rejection path is reachable only via fake guards.
  evidence: Make the guard validate `current->requested` once a story persists per-item lifecycle state; do not accept a client-supplied current state.
  triage: 2026-11-24 Skipped. Validating the actual edge needs stored per-item lifecycle state, which no story provides yet.
- source_spec: `_bmad-output/implementation-artifacts/1-6-canonical-lifecycle-state-model-and-transition-enforcement.md`
  summary: `LifecycleReprocessFactory` has no production caller and emits no `supersedes`/`superseded_by` audit envelope (AC4 wiring).
  evidence: No `src` caller; wire it in the reprocess story (FR48d, Epic 6) together with the audit link emission.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Defensively copy collections in the pre-existing TenantAiPolicySnapshot provider evidence contract before activating a live policy provider.
  evidence: The primary-constructor record retained caller-owned AssistanceKinds and LowRiskAllowedByActionClass before Story 1.6; mutating them changes authorization values under the same reference. Story 1.6's separate synthetic snapshot copies nested collections, and its production AI snapshot provider remains unavailable.
  resolution: Resolved 2026-11-24. Snapshot construction copies provider collections into read-only data. Collection failures during provider resolution use the redacted unavailable path and caller cancellation still propagates. AiActionPolicyEvaluatorTests passed all 29 cases.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-15)

- The gate for `SubmitTenantPolicyChange`, `ApproveTenantPolicyChange`, `SetAssociationConfidenceThresholds`, `SubmitEscalationPolicyChange`, `SubmitNotificationRoutingChange`, `SubmitMailboxConfigurationChange`, `SubmitRetentionConfigurationChange` and `SubmitDataClassInventoryChange` is enforced only in the gateway pipeline and SDK admission stage; it is not repeated in `GovernedOperationAggregate` or `AcceptedCommandDispatcher`. Existing handlers remain, including `SubmitNotificationRoutingChange`, which emits `NotificationRoutingSnapshotActivated`. Unverified medium. Settle by tracing every dispatch path for all eight commands to determine whether any reaches the aggregate without an admission stage.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-16)

- Audit-enabled builds fail with NU1903 on `Microsoft.AspNetCore.SignalR.Common` (GHSA-334f-vvp4-7g6p); Story 1.6 evidence passes only with `-p:NuGetAudit=false` and no tracked item exists. Pre-existing dependency advisory; track a package bump or documented suppression.
  resolution: Resolved before 2026-11-24 by e466cbb. Central transitive pinning selects `Microsoft.AspNetCore.SignalR.Common` 11.0.0 from the shared catalog. The 2026-11-24 Release solution build ran with `NuGetAudit` enabled and reported no NU1903.
- Denial for all eight policy-snapshot commands listed in the 2026-10-15 entry is not repeated in `GovernedOperationAggregate` / `AcceptedCommandDispatcher` (raised again in round 3). Unverified medium; trace dispatch paths for every listed command as described in that entry.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Before enabling a live AI policy provider, verify provider-owned collection failures produce redacted unavailable decisions.
  evidence: The pre-existing TenantAiPolicySnapshot contract exposes provider-owned collections and baseline enumeration/lookup already occurred without failure translation. Current providers use ordinary arrays/maps and production remains unavailable; a reachable provider whose enumeration or lookup throws would settle this unverified medium concern.
  resolution: Resolved 2026-11-24. Snapshot construction copies provider collections into read-only data. Collection failures during provider resolution use the redacted unavailable path and caller cancellation still propagates. AiActionPolicyEvaluatorTests passed all 29 cases.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-16, round 4)

- Gate precedes idempotency lookup: retries of policy commands accepted before the gate now return 503 `dependency_degraded`. Unverified medium; settle by checking for any environment/event stream holding pre-gate accepted policy commands.
- Audit-envelope coverage (PreCommit audit failure, replay intent, SourceEvidenceRefs) for notification/escalation/tenant-policy/mailbox commands was removed with gateway denial; aggregate-level tests missing. Unverified medium; resolves together with the aggregate-bypass entries above.

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Verify that supported admission-marker issuers cannot sign qualified policy command aliases that bypass the simple-name policy gate (unverified medium).
  evidence: EventStore dispatch normalizes namespace/assembly-qualified CommandType strings, while TenantPolicyPersistenceGate matches simple names. A valid marker for an alias takes the SDK marker fast path, but current dispatcher plans and exact-match admission issue only simple names and marker validation binds the exact CommandType. A supported producer of a qualified signed envelope, or an explicit qualified-issuer contract test before activation, would settle reachability.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-16, round 8)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: SDK `/process` rejection with `PolicySnapshotUnavailable` may expose a different outward shape than the HTTP gateway's 503 `dependency_degraded` (verified 2026-10-18 in round 21: it does; low, documented in the ADR).
  evidence: only `CommandGateway` maps the reason to a dependency-unavailable problem; `ChatBotDomainServiceAdmissionStage` returns `Rejected(PolicySnapshotUnavailable)`. Settle by checking how EventStore surfaces an SDK Rejected reason to the caller. Settled 2026-10-18 (round 21): `DomainServiceRequestRouter.cs:149` wraps the admission rejection in `DomainResult.Rejection`; `AggregateActor` persists and publishes the metadata-only `ChatBotDomainServiceAdmissionRejected` event on the target stream (D3) and returns that cached terminal rejection to a same-message-ID retry, so SDK producers get no retryable outcome. `docs/adrs/isolated-policy-snapshots.md` records this. In the live topology `/process` sees a gated command only through the marker path, i.e. pre-gate marked dispatches (round-4/14 stranded entry).
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Carry the exact resolved tenant/policy-version tuple from `AiActionPolicyDecision` into `ChatBotApprovalResult` and the low-risk AI events when snapshots become live.
  evidence: `AiActionApprovalGate.cs:97-98` passes only `PolicySnapshotId`; events are persisted state, so the change belongs with accepted A6 evidence in Story 1.6's open live-persistence scope. ADR wording qualified in round 8.

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-10-17)

- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: OpenAPI `oneOf: [TaskId, CommandId]` over identical ULID schemas can never satisfy strict `oneOf` validation (pre-existing).
  evidence: Still present on the `/api/v1/operations/{operationId}` and audit-history path parameters, on `OperationStatus.originalOperationId` and on `OperationAuditHistory.operationId` (`hexalith.chatbot.v1.yaml:97,1393,1487` at `839aee3`). Story 1.5 replaced only `OperationStatus.operationId` with `$ref: CommandId`.
  resolution: Resolved 2026-10-20 with the original Story 1.2 contract/client finding 7 above. Both path parameters, `OperationStatus.originalOperationId` and `OperationAuditHistory.operationId` now reference the common ULID schema. Strict zero/one/two-match controls and all four identifier locations pass in `TestResults/story-1-5-deferrals/patch-contracts.trx`; historical identities are unchanged.
- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: `OperationStatusWorkflowStatusSink` writes a record built from an earlier non-atomic `TryGetAsync`, so concurrent replay or workflow field changes can be lost (pre-existing).
  evidence: `OperationStatusWorkflowStatusSink.cs:25-69`. Before Story 1.5 the blind store replace lost them; now the legacy timestamp policy decides instead. Fix with an atomic read-modify-write through the store's update callback.
  resolution: Resolved 2026-10-20: the sink now uses the atomic callback and recomputes from each latest snapshot, retaining duplicate counts and avoiding regressed update timestamps. Null means no write, preserving absent-operation behavior. The same-instance completion guard also preserves completed progress against older workflow reports while allowing failed restarts; its 37 focused cases pass in `TestResults/story-1-5-deferrals/workflow-review.trx`. Both-store sink/status regressions pass in `TestResults/story-1-5-deferrals/patch-status.trx`; deterministic workflow/replay ETag conflict and terminal-guidance evidence is in `TestResults/story-1-5-deferrals/patch-gateway.trx`.
- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: The canonical commit invariant `CommittedRevision == ExpectedRevision + 1` assumes single-event owner commits (unverified medium).
  evidence: Enforced in `CommandOperationReducer.Apply` (`commitValid`) and `OperationStatusUpdatePolicy.ValidateCanonical` (`validCommit`). Settle by confirming EventStore owner revision semantics for commands that emit several events when Story 1.8 wires the owner commit.
- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Equal-revision canonical equivalence is decided by comparing `JsonSerializer.Serialize(current)` with `JsonSerializer.Serialize(incoming)`, but the synthetic SDK store never JSON round-trips (unverified medium).
  evidence: `AtomicOperationStatusReadModelStore.cs:53` keeps the object reference, so the "SDK fake" race and duplicate rows never compare against a store-deserialized `current`. Settle with a round-trip through the DAPR read-model serializer before canonical writes go live.
  resolution: Resolved 2026-10-20: the SDK fixture now runs the production `DaprReadModelStore` with the unkeyed production Dapr builder serializer over conditional serialized byte state, returning fresh instances. `OperationStatusMonotonicityTests.ProductionDaprSerializerShouldPreserveCanonicalEqualRevisionAndNestedPredecessorMetadata` verifies equivalent equal-revision writes, conflicting writes, persisted nested predecessor wire values and defensive copies. All SDK monotonicity/identity/race rows use that seam. Current-run evidence: `TestResults/story-1-5-deferrals/patch-status.trx`. No equivalence defect was observed; the existing comparison policy is retained. This is synthetic transport evidence, not native durability.
- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: The pure reducer cannot stop one failed attempt being retried more than once. `CommandOperationReducer.Retry` records nothing on the predecessor, so several distinct retry ULIDs can each succeed, and `MaxAttempts` limits chain depth rather than total attempts.
  evidence: `CommandOperationReducer.cs:123` at `839aee3`. Deferred by decision on 2026-10-17: successor uniqueness belongs to the owner aggregate. Enforce it when Story 1.8 (atomic owner commit) and Story 1.10 (RetryCommandExecution UI) wire live retries.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Unverified medium - establish accepted numeric equivalence for canonical operation v1 before owner activation.
  evidence: R3B6; the unchanged canonicalizer preserves numeric token spellings, so 1, 1.0 and 1e0 hash differently. Settle with an accepted owner command numeric rule and golden equivalence vectors; never rewrite existing lifetime fingerprints implicitly.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Preserve every concurrent duplicate suppression with an atomic count update.
  evidence: R3B9; the original baseline admission pipeline already reads count N and writes N+1 outside the store transaction. Concurrent callers can lose an increment; the new max merge prevents regression but cannot reconstruct two increments. A focused concurrent gateway/store test should bind the future fix.
  resolution: Resolved 2026-10-20: each duplicate replay computes its increment inside the atomic callback. `CommandGatewayTests.ConcurrentDuplicateSuppressionsShouldIncrementExactlyOnceEachAndRetainTerminalWorkflowGuidance` forces contention in both stores, asserts three callback evaluations for two successful suppressions (one real SDK ETag conflict), and checks persisted count exactly two with terminal workflow guidance, partial outputs and receipt-proven audit certainty retained. `ReplayAtomicRetryShouldRecheckCallerCancellationAndCurrentAuthorityBeforeMutation` verifies retry guards. Current-run evidence: `TestResults/story-1-5-deferrals/patch-gateway.trx`.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Reconcile the Story 1.6 policy-persistence ADR retry guidance with its gateway dependency outcome.
  evidence: R3B10; resolved 2026-10-18 by the approved Story 1.6 round-14 decision. docs/adrs/isolated-policy-snapshots.md now documents retryable HTTP 503 dependency_degraded until A6/native acceptance; SyntheticHttpPolicyTransportsMustReturnSafeDependencyDenial asserts Retryable=true and ClientAction=RetryLater across every gated transport (TestResults/story-1-6/round-14-focused.trx).

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Align the historical Story 1.6 lifecycle artifact with the superseded_by and supersedes relationship names.
  evidence: R4B10; the separately committed lifecycle factory now emits superseded_by/supersedes while 1-6-canonical-lifecycle-state-model-and-transition-enforcement.md still claims superseded_by_workflow/supersedes_workflow in its task and AC4 evidence.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-17, round 14)

### DW-147: `TenantPolicySchema.Validate` rejects `t-low >= t-high` only when `t-high` appears earlier in the change set, so `[t-low=0.95, t-high=0.90]` validates (pre-existing, medium).

status: done 2026-11-24
origin: legacy ledger entry from Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-17, round 14)
source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
severity: medium
archived: 2026-11-24

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Association scoring uses the caller-supplied `ThresholdPolicy` or falls back to `AssociationThresholdPolicySnapshot.DefaultM0`, and decisions record `command.ThresholdPolicy.PolicyVersion` without checking it against the tenant's threshold version (pre-existing, medium).
  evidence: `AssociationScoringOrchestrator.cs:44`; `GovernedOperationAggregate.cs:2760/2792/2821`. Conflicts with "no default policy" and exact decision references; owned by Story 2.6 (association rules and confidence thresholds).
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Legacy policy handlers lack replay/conflict/append-only semantics and must be replaced before live policy persistence (pre-existing, medium).
  evidence: `GovernedOperationAggregate.cs:149-157` derives the next version from caller `SourceVersion + 1`, reads `DateTimeOffset.UtcNow` inside `Handle`, and returns `NoOp` on proposed-ID reuse; `GovernedOperationState.cs:548` overwrites snapshots by ID. Kept for historical replay per the Story 1.6 Code Map and unreachable while the gate denies.
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Pending `ApproveTenantPolicyChange` items created before the gate have no reject/withdraw path and stay pending (unverified medium; extends the round-4 "gate precedes idempotency lookup" entry).
  evidence: The gate denies approvals unconditionally and no reject/withdraw/expire command exists. Settled by confirming no environment holds pre-gate pending or accepted policy changes; nothing is deployed while A6/A13 remain open.
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Blocked AI outcomes, including `policy-snapshot-unavailable` from the fail-closed evaluator, record no auditable attempt and are relabeled `command_not_allowlisted` (medium; deferred by decision 2026-10-17).
  evidence: `ChatBotCommandAdmissionPipeline.cs:179-185` returns `Rejected(CommandNotAllowlisted)` without `DenyAsync`; this branch was already unaudited for `insufficient-authority` and `risk_classification_unavailable`. Only acceptance mode reaches the policy case. Reason: auditable sensitive non-mutating attempts belong to Story 1.9; keep the outward 403 `refusal_blocked_action`.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Verify BMAD Loop dependency-version metadata against the installed method version before future setup or upgrade (unverified medium; pre-existing tooling concern).
  evidence: `.agents/skills/bmod-bmad-loop/bmod.toml` requires `bmad` and `bmad-build-auto` 6.13.0, while `_bmad/_config/manifest.yaml` records method 6.12.4. Both states predate this run at 459d5c5e04905743fdc28e1f20fd14bf893cf972. Establish whether the setup/dependency resolver enforces those declarations or treats them as dormant metadata; resolve with the tooling owner without changing this story's agent context or dependency versions.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-18, round 15)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: The round-14 gating of `SubmitRetentionConfigurationChange` and `SubmitDataClassInventoryChange` also removed their NFR35 audit-ref coverage. This extends the round-4 removed-coverage entry, which names only notification, escalation, tenant-policy and mailbox (medium).
  evidence: The removed assertions covered `inventory-snapshot:`, the inventory old/new fingerprint refs, the per-class `data-class:`/`retention-class:`/`owner-role:`/`deletion-behavior:`/`export-eligibility:`/`redaction-sensitivity:` refs, and the retention pre-commit replay intent. The retention and data-class branches of `AuditEnvelopeFactory.cs:3064/3108` now have no assertions. Restore them before the gate lifts, as direct `AuditEnvelopeFactory.PreCommit` tests independent of the gate or as aggregate-level tests.
  resolution: Resolved 2026-11-24. Direct pre-commit envelope tests verify the retention snapshot, fingerprints and windows, and the inventory snapshot, fingerprints and classification dimensions under NFR35. The retention outage test also verifies the pre-commit replay intent.
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Before activating a live AI policy provider, require new decisions to resolve the tenant's current effective snapshot so a caller cannot select a superseded, more permissive policy (unverified medium/high).
  evidence: `AiActionApprovalGate.cs:86` passes the caller's `command.PolicySnapshotId` as `RequestedPolicySnapshotId` (pre-existing since the baseline). `DefaultAiActionPolicyEvaluator` requires only an exact echo, and `TenantPolicySnapshotResolver` resolves any exact historical tuple. Not reachable today because the production provider returns null. Settle at live-provider design: treat a superseded snapshot as stale for new decisions, keep exact historical resolution for audit and replay only, and record the obligation in `docs/adrs/isolated-policy-snapshots.md`.
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: The `"unavailable"` snapshot sentinel defeats the approved-execution policy guard (pre-existing, unverified medium).
  evidence: `GovernedOperationAggregate.cs:2465` rejects `ExecuteApprovedAIAction` with `policy-snapshot-unavailable` only when both the command and approval-request snapshot IDs are blank. Approval requests store `"unavailable"`: the proposal fallback at `:3933` has done so since the baseline, and every early-routed low-risk approval has done so since the Story 1.6 round-4 fail-closed decision. So the guard never fires. At the baseline the early-route ID was an unresolved caller value, which also passed. Settle by deciding whether human-approved execution requires a resolved policy snapshot; if it does, treat the sentinel as missing in the guard.

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Add regression coverage for CLI text output choosing a nonblank accepted OperationId over different TaskId and CommandId values (pre-existing Story 1.5 verification gap).
  evidence: Build15-VG-1; ChatBotCliOutputFormatter.cs:47 uses the correct accepted identity, but AcceptedCommandJsonOutputReportsPartialSuccessWorkflowMetadata checks the distinct operation only in JSON and AcceptedFormatterShouldPreserveHistoricalFallbackOnlyWithoutOperationId has only absent/blank-ID rows. Replacing the text identity with the historical task/command fallback would pass those tests. Add a distinct-ID row to the existing shared text/JSON formatter theory in Story 1.5; the formatter and tests were unchanged at this run's initial HEAD 16ba59f50fdbe87c55de117c13405b82fa664f93. Resolved 2026-10-20 in Story 1.5: the shared formatter theory now includes accepted-operation distinct from historical-task and caller-command, asserting precedence in text and JSON alongside the three historical fallback rows. All four rows passed in TestResults/story-1-5-oct20-final/cli.trx.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-18, round 16)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Authorization-failure audit facts, including the eight policy-gate denials, are held only in process and never reach the WORM chain (pre-existing, medium).
  evidence: `ChainedAuditWriter.cs:20` delegates `RecordAuthorizationFailureAsync` to the inner `InMemoryAuditWriter`, which appends to an in-process list; nothing in `src` reads `AuthorizationFailures`. Before the Story 1.6 gate, retention and data-class changes produced WORM-chained NFR35 records; a denied attempt now leaves no durable trace. Durable auditable sensitive attempts belong to Story 1.9.
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Before activating a live AI policy provider, decide whether transient provider faults keep a retryable dependency outcome instead of a non-retryable 403 (unverified medium).
  evidence: `DefaultAiActionPolicyEvaluator.cs:48` catches provider exceptions and returns `Unavailable()` (`Blocked`); `ChatBotCommandAdmissionPipeline.cs:179-185` maps `Blocked` to `Rejected(command_not_allowlisted)`, so the caller sees 403 `refusal_blocked_action`. At the baseline the exception escaped to `CommandGateway.cs:80-82`, giving 503 retry-later. No registered provider throws today (production returns null; acceptance and recovery providers are pure). A provider that can fail transiently would settle reachability.

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Before activating a live AI policy provider, settle whether optional per-action maps require the complete closed schema or represent sparse restrictions (unverified medium).
  evidence: Build16-BH-4; DefaultAiActionPolicyEvaluator.LowRiskAllowedForEveryActionClass has permitted a partial map covering only requested classes since the preserved baseline. TenantAiPolicySnapshot documents optional per-action restrictions, while TenantPolicySchema requires all six keys for stored tenant-policy values. Registered fixtures omit the map and production returns null. Define the provider mapping contract at live-provider design; if it must mirror the closed schema, reject incomplete or unknown-key maps and verify unavailable/redacted outcomes before activation.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-18, round 17)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Canonical audit envelopes carry the constant `chatbot.gateway.policy-snapshot.v1` instead of the decision's exact tenant/snapshot/version tuple (pre-existing, medium).
  evidence: `AuditEnvelopeFactory.cs:18` (`NoPayloadPolicySnapshotId`) is stamped on every PreCommit/PostCommit envelope, so no tenant snapshot reference reaches the WORM chain that epic AC4 historical inspection relies on. Unchanged by Story 1.6; the round-8 entry covers only `ChatBotApprovalResult` and the low-risk AI events. No live policy decision exists while the production provider is unavailable. Carry the exact tuple into envelopes in Story 1.6's open live-persistence scope, alongside the round-8 entry.
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Per-action-class policy tokens never match: the approval gate sends enum names, the evaluator parses kebab tokens (pre-existing, low, fail-closed).
  evidence: `AiActionApprovalGate.cs:88` builds `RiskActionClasses` with `value.ToString()` (`ModifiesState`); `DefaultAiActionPolicyEvaluator.TryParseActionClass` (`:105`) accepts only `modifies-state` and similar, so any non-empty list routes to approval. Unreachable today because `AiActionRiskClassifier` yields `LowRisk` only with zero action classes. Settle with the round-16 per-action-map entry at live-provider design: use the shared token mapper, or remove the map.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Preserve caller cancellation when the Story 1.5 pre-reservation status lookup fails with a non-cancellation exception after its caller token is cancelled (pre-existing, medium; Story 1.5 owner).
  evidence: Resume-BH-10; `ChatBotCommandAdmissionPipeline.cs:210-213` catches IOException and other non-cancellation exceptions regardless of the caller token state, then returns `idempotency_outcome_unavailable` without a caller-token recheck. The OperationCanceledException filter already preserves direct caller cancellation but does not cover this race. Git blame attributes the lookup to Story 1.5 commit `8adcde63b2103a98930dc590db269c5be8a53498`; Story 1.6 does not modify it. Add `cancellationToken.ThrowIfCancellationRequested()` in that catch and cover a cancelling/throwing lookup while retaining the active-caller dependency-denial case. Resolved 2026-10-20 in Story 1.5: the lookup catch rechecks caller cancellation before translation. The new caller-cancel-then-IOException regression failed before the fix and passes afterward; direct caller cancellation, active-caller IOException, dependency-owned cancellation, zero reservation/audit/dispatch and later retry are covered in TestResults/story-1-5-oct20-final/server.trx.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Qualify Story 1.5's canonical-concurrency OpenAPI refusal description to preserve earlier policy and authorization denial precedence, and refresh the generated client documentation.
  evidence: Low; src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml lines 37-39 were introduced by Story 1.5 commit 8adcde63b2103a98930dc590db269c5be8a53498 and promise non-retryable canonical_execution_unavailable for concurrency requests, while the existing earlier policy gate returns retryable dependency_degraded for its eight commands. The 16 passing SyntheticHttpPolicyTransportsMustReturnSafeDependencyDenial rows pin that behavior; this Story 1.6 run changes neither description nor runtime ordering. Resolved 2026-10-20 in Story 1.5 (Round18Build-BH-1): the POST description now qualifies canonical refusal after earlier admission/authorization/policy checks and documents retryable dependency_degraded for the eight policy commands. NSwag regenerated the client remarks and the hash fixture was refreshed. Current Server policy admission/HTTP rows and Client generation/freshness tests pass in TestResults/story-1-5-oct20-final/{server,client}.trx; policy-gate behavior is unchanged.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-18, round 19)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Unresolved-policy AI refusals carry problem `Type` `https://hexalith.dev/errors/chatbot/command-not-allowlisted` and category `Authorization_denied` for an allowlisted, authorized command (pre-existing, low).
  evidence: `ChatBotCommandAdmissionPipeline.cs:179-185` maps every approval-gate `Blocked` result to `CommandNotAllowlisted`, and `ChatBotProblemDetailsFactory.cs:191-205` stamps that type; the mapping predates the Story 1.6 baseline, which only made missing/stale/invalid policy `Blocked`. Outward Code `refusal_blocked_action` and 403 are the decided shape; a distinct type URI is a public OpenAPI change. No UI/CLI/MCP consumer keys on the type today.

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Add evaluated MSBuild regression coverage for dependency-mode defaults in independent build maintenance (pre-existing, medium).
  evidence: Round19Build-VG-2 pre-verifies that removing the empty-Configuration arm in Directory.Build.props restores ordinary local package selection while architecture XML checks and explicitly overridden CI builds still pass. The default-source fix was independently committed as 69eec6b779500eaf22f8720f67170c921c57e1ae and is untouched by this policy-snapshot run. Evaluate a consuming project's selected property and project/package reference items with overrides absent, explicit Debug, Release and CI to pin the intended selection behavior.
  resolution: Resolved 2026-11-24. Seven real Server project evaluations passed. The tests cover default, Debug, Release, CI and explicit mode overrides, and verify the resulting EventStore reference family against the shared package catalog.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-18, round 20)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Two association-threshold lineages coexist: direct `SetAssociationConfidenceThresholds` (free-form string `PolicyVersion`, read by scoring) and the tenant-policy `association.t-high`/`association.t-low` knobs (versioned by the immutable snapshot model, never read by scoring). Decide which is authoritative before the policy gate lifts (pre-existing, medium).
  evidence: `GovernedOperationAggregate.cs:2859` emits `AssociationConfidenceThresholdsChanged`, and only `GovernedOperationState.cs:973` sets the scoring thresholds; `TenantPolicySnapshotActivated` is stored by ID at `GovernedOperationState.cs:545` and changes no threshold; the knobs appear only in `TenantPolicySchema` defaults and validation. Both writers are gated today. Owned by Story 2.6 (association rules and confidence thresholds), alongside the 2026-10-17 caller-supplied `ThresholdPolicy` entry.
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Before activating a live AI policy provider, define how `TenantAiPolicySnapshot` is built from a tenant policy snapshot: the schema has no knob for its scalar `LowRiskAllowed`, `EffectSurface` or `AssistanceKinds` (pre-existing, medium).
  evidence: `TenantAiPolicySnapshot.cs:17` needs those fields (they predate the Story 1.6 baseline); `TenantPolicyKnobIds.cs:10` defines only the `ai-action.low-risk-allowed` map. Without a mapping or new knobs, the retained tenant/ID/version tuple would name a snapshot that cannot answer the evaluator's surface and kind checks. Settle together with the round-16 per-action-map entry.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-18, round 21)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Retention and data-class inventory audit envelopes map to no NFR15a inventory path, so the completeness measurer will count every admitted change as a gap once the policy gate lifts (pre-existing, medium).
  evidence: `AuditEnvelopeFactory.cs:3064` and `:3108` build envelopes for `SubmitRetentionConfigurationChange` (Story 7.4) and `SubmitDataClassInventoryChange` (Story 9.7); `ChatBotAuditPathMap.cs:67-73` classifies neither; `AuditOperationReconstructor.cs:47-51` returns `UnmappedPathReasonCode`, which `AuditCompletenessMeasurer.cs:139` counts. Both commands are denied by the Story 1.6 gate today, so no envelope is produced. Choose their inventory path code before the gate lifts; the gate's structural membership already covers both.
  resolution: Resolved 2026-11-24. Both compliance snapshot commands now reconstruct under the existing tenant-policy-mutation inventory path. A regression theory covers both command types.


## Deferred from: Story 1.6 round-22 build workflow review (2026-10-19; historical release maintenance)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Fix prerelease FileVersion handling in release packaging (pre-existing, medium).
  evidence: R22Build-BH-1/EH-2: .github/scripts/pack-release-packages.sh:28 assigns a full prerelease version to FileVersion. An independent SDK 10.0.402 warnings-as-errors compiler reproduction for 1.2.3-alpha.1 fails CS7035. Use the numeric file-version core and cover alpha/beta packaging; this release helper is unchanged by the Story 1.6 documentation pass.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Skip NuGet verification when semantic-release publishes no release (pre-existing, medium).
  evidence: R22Build-BH-2/EH-1/VG-2: release.yml:301 checks successful SHA eligibility rather than an actual release result. Cached semantic-release source returns without tagging on no relevant changes and its CLI exits zero; the subsequent exact-tag lookup fails on an untagged docs/chore HEAD. Bind verification to an actual published version.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Provide recovery for partially published tagged NuGet releases (pre-existing, medium).
  evidence: R22Build-BH-3: semantic-release tags/pushes before publishCmd, publish-release-packages.sh pushes sequentially, and GitHub assets follow successful publication. A mid-push failure can strand an incomplete tagged version; retain the prepared artifact set and define an explicit retry for that version.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Validate the NuGet publishing credential before release preparation and tag creation (pre-existing, medium).
  evidence: R22Build-BH-4: .releaserc.json defines prepareCmd/publishCmd but no NuGet verifyConditionsCmd. NUGET_API_KEY is checked only inside publish-release-packages.sh, after semantic-release has created its tag; missing credentials advance release state before failing.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Resolve a packaging output directory consistently before cleanup and repository-directory switching (pre-existing, low).
  evidence: R22Build-BH-6: pack-release-packages.sh:19-25 cleans the caller-relative output directory, changes to repository_root, and passes the unchanged relative path to dotnet pack; its final inventory still examines the caller-relative directory. An invocation outside the root can clean/inspect one directory and write another. The current semantic-release caller starts at the root.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Bound each NuGet verification HTTP request by the remaining deadline (pre-existing, medium).
  evidence: R22Build-BH-7/EH-4: verify-nuget-publication.sh:26-29 supplies neither a connection nor a transfer timeout to curl. Its 600-second guard runs only after curl returns failure, so a stalled request can exceed the advertised verification budget until the workflow timeout.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Determine whether duplicate NuGet versions can hide conflicting prepared versus published package contents (pre-existing, unverified medium).
  evidence: R22Build-BH-8: publish-release-packages.sh uses --skip-duplicate, while verify-nuget-publication.sh discards downloads and tests only HTTP success. No actual conflicting artifact was shown. Settle using a fixture with the same ID/version but conflicting contents and a signing-aware comparison before treating an availability check as artifact-content acceptance.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Add publication-free stable/prerelease package artifact coverage to build maintenance (pre-existing, medium).
  evidence: R22Build-BH-10: CI invokes restore/build/test but neither dotnet pack nor the packaging helpers, and test-source searches find no helper coverage. The reproduced FileVersion prerelease failure escapes ordinary builds. Check the intended package IDs, versions, dependencies and symbol artifacts without publishing.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Reconcile the release job timeout with its existing Architecture guard (pre-existing, medium).
  evidence: R22Build-VG-1: release.yml:269 says timeout-minutes: 45, but LiveRecoveryValidationArchitectureTests.cs:633 requires 30. A fresh Debug build passed, then the exact native test ReleasePublication_ShouldRequireFreshMainHeadImmediatelyBeforeSemanticRelease failed 0/1 passed with zero skips. Current failed CTRF/TRX/logs are TestResults/story-1-6/round-22-release-guard.*; do not weaken the guard without deciding the intended release budget.


## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-20, round 25)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Addendum to the round-15 `"unavailable"` sentinel entry: the approved-AI dispatch marks an external effect attempted before the aggregate's policy guard runs, so fixing only the guard is not enough (pre-existing, unverified medium).
  evidence: `AcceptedCommandDispatcher.cs:757` (unchanged since baseline `38ca946:757`) substitutes the literal `"unavailable"` for a missing `ExecuteApprovedAIAction.PolicySnapshotId`, then calls `MarkExternalEffectAttempted` and `PrepareAppendConversationMessageAsync` before `GovernedOperationAggregate.cs:2465` can refuse. If human-approved execution must require a resolved policy snapshot, the dispatcher has to treat the sentinel as missing before the external effect, not only the aggregate guard. Settle together with the existing sentinel entry.

## Deferred from: Story 1.5 actionable-deferral build review (2026-10-20)

- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Preserve workflow progress and duplicate partial-output facts when a delayed original acceptance receipt arrives after replay initialized the operation status.
  evidence: R8B1 (medium), pre-existing at the original blind-replacement baseline. A replay-created status can receive a completed workflow report before the original gateway finishes its post-commit audit; `CommandGateway` then upserts a fresh `Accepted` record. Equal/later LastUpdatedAt selects that receipt in the unchanged legacy policy, which preserves audit/count/prior outcome but clears workflow completion/guidance and partial outputs. This is separate from early reports discarded before any record exists (Story 1.2 Loop 15 BH3) and from the now-atomic replay/sink mutations. Resolve with a receipt-initialization/field-merge lifecycle decision and a deterministic replay-created → workflow-completed → original-receipt final-state regression through both stores, retaining receipt-proven audit certainty and historical identity.
  resolution: Resolved 2026-11-25. Receipt publication merges audit proof atomically while preserving workflow completion and duplicate output from replay. The regression passed against both operation-status stores.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-20, round 27)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Ungated commands accept a caller-supplied `PolicySnapshotId` that is only token-checked and record it as decision provenance (pre-existing, medium).
  evidence: About 50 Contracts commands carry `PolicySnapshotId`, including rate limits, disable/quarantine requests and approvals, outbound drafts and sends, admin-queue operations, consent, export and deletion. `ParticipantAuthorizationStage` checks only the token shape (e.g. `IsValidTenantExportRequest` `:2008`, `IsValidDeletionErasureRequest` `:2025`, also `InventorySnapshotId`). The value is stored in domain events (`AiActorDisabled.cs:18`, `CommandCapabilityRateLimitConfigured.cs:23`) and in `policy-snapshot:`/`inventory-snapshot:` audit refs (`AuditEnvelopeFactory.cs:3181,3225`). No policy snapshot can be persisted or resolved, so these references point at nothing. Epic AC3/AC6 need them resolved against the tenant's history; this belongs to Story 1.6's open live-persistence scope, alongside the round-14 association-threshold entry.
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: The compliance UI hard-codes a default policy reference and a constant correlation ID on every escalation and investigation command (pre-existing, medium).
  evidence: `ComplianceAuditService.cs:74-75` and `:96-97` (unchanged since baseline `38ca946`) send `PolicySnapshotId: "policy-snapshot-compliance-v1"` and `CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW"`. This contradicts "no default policy" once policy references are resolved. Owned by the compliance-audit surface (Stories 9.x).
- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Policy approval records self-reported requester and approver refs instead of the trusted principal (pre-existing, medium; unreachable while the Story 1.6 gate denies `ApproveTenantPolicyChange`).
  evidence: `GovernedOperationAggregate.cs:219` enforces separation of duties on trusted IDs (`pending.RequesterActorId` vs `envelope.UserId`), but `TenantPolicySnapshotActivated` (`:233-235`) stores the payload's `RequesterRef`/`ApproverRef`. `ParticipantAuthorizationStage.cs:721` only checks that the two strings differ. Bind both refs to the trusted principal when the legacy policy handlers are replaced (round-14 legacy-handler entry). The disable/quarantine approval family stores the same payload refs and is outside Story 1.6.

## Deferred from: code review of spec-1-7-admit-commands-through-the-single-commandgateway (2026-10-21)

- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Marked-envelope revalidation will be incoherent once commit participation exists (medium; masked while atomic participation always refuses, and Story 1.8 owns the commit-participation contract).
  evidence: `RevalidateMarkedCommitAsync` (`ChatBotCommandAdmissionPipeline.cs:61`) runs only the caller-input `ValidateOperationAsync` and the atomic check. Three internal producers mint markers and call `SubmitCommandAsync` directly: `AiExecutionCoordinator`, `EventStoreCorrectionPropagationCommandWriter` and `AiActionProposalInvalidationCoordinator`. Their commands skip authorization, classification and profile stages, including the ai-effect `MarkAiActionProposalInvalidatedByCorrection`. Gateway-dispatched envelopes re-run operation validation, which rejects the `riskClassification` that `AcceptedCommandDispatcher` adds to the payload, so every admitted AI proposal or assistance would fail at `/process`. Story 1.8 needs a commit-participation check for server envelopes that skips caller-selector rules for server-owned fields, and must route the producers through the gateway or give them an equivalent profile check.
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: The atomic boundary is checked against a placeholder envelope, and the legacy non-atomic flow still runs after the gate (medium; masked while atomic participation always refuses, and Story 1.8 owns the commit-participation contract).
  evidence: `ChatBotCommandAdmissionPipeline.cs:261` builds a `Received→Admitted` envelope before lifecycle validation. After `ValidateAtomicParticipationAsync`, the pre-1.7 reservation, lifecycle validation, second `PreCommit` envelope, separate audit write and dispatch still run, which duplicates envelope construction and reorders AC2's universal stages. Story 1.8 must have atomic participation consume the lifecycle-validated envelope and replace the post-gate flow.
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Restore the Story 13.2 live M0 message→ask→approve→execute→stop assertions once commit participation exists (medium; the review re-baselined the live test to the fail-closed 503 contract).
  evidence: `Story132ProductionBrowserAspireE2ETests` (run by ci.yml:469 and release.yml:76) required 202 for `ExecuteLowRiskAIAssistance` and an approval-required proposal from the UI ask. Story 1.7 production admission refuses every mutation, so the review re-baselined it to assert a safe refusal. The original flow returns with Story 1.8.
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: AI decision commands take approver authority from a claim the production principal never carries (pre-existing, medium; Story 1.10 completes the first governed UI action).
  evidence: `AiActionApprovalGate.cs:42` and `:56` read `requester_authority_class` for `DecideAiActionApproval` and `DecideOutboundApproval`. `ChatBotAuthorityPrincipal` emits only binding, tenant-role and project-owner claims, so Approve, Reject and RequestRevision always return `insufficient-authority`. The review only removed the structural `classifier-indeterminate` block. Approver authority must come from current owner evidence, not token labels.
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Map or deny the ten legacy-unmapped concrete commands through a PRD/addendum decision before Story 1.8 lets anything commit (medium).
  evidence: `DecideOutboundApproval`, `MarkAiActionProposalInvalidatedByCorrection`, `RecordGovernedNote`, `RecordProjectConversationMessage`, `RequestComplianceEscalation`, `RequestComplianceInvestigation`, `RequestOutboundSendApproval`, `SubmitConsentLawfulBasisRecord`, `SubmitDataClassInventoryChange` and `SubmitEscalationPolicyChange` have implementer-chosen profiles in `ChatBotCommandAdmissionCatalog.ConcreteDefinitions`, but no stable product command reaches them. The addendum says the pipeline "denies … unmapped commands". The review locked them in an explicit fitness list instead of denying them, which would have rewritten about 400 test references. The same decision covers the doubtful existing rows `ExecuteRetentionDisposition→SubmitRetentionConfigurationChange`, `UpdateTenantPolicy→SetAssociationConfidenceThresholds` and `ProposeEmailProjectAssociation→ScoreMailboxMessageAssociation`.
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Outbound sends need structured approval evidence before admission can accept them (medium; the review made admission fail closed without it).
  evidence: Only `ExecuteApprovedAIAction` gets the structured `ValidateApproval` check (approver, presence, lifetime, scope/drift, independence). The dispatcher sends the external email before the aggregate re-validates the approval. The outbound owner-source story must add structured outbound approval evidence that reuses `GovernedOperationAggregate.ValidateOutboundSendApproval` plus the independence, presence and ≤10-minute external-effect lifetime checks. Nothing produces this evidence yet.

## Deferred from: code review of spec-1-7-admit-commands-through-the-single-commandgateway (2026-10-21, round 2)

- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Marked-envelope revalidation now refuses every internal system producer outright; this extends the D8 deferral (medium; masked while atomic participation always refuses, and Story 1.8 owns the commit-participation contract).
  evidence: `ChatBotDomainServiceAdmissionStage.cs:73-81` resolves the signed issuer through `ResolveCommand` (only `service-account-*` relays with actor null or `service`) or `ResolveBoundGrantIssuer` (only `actorType=human`). So `actorType=system` envelopes from `AiExecutionCoordinator`, `EventStoreCorrectionPropagationCommandWriter` and `AiActionProposalInvalidationCoordinator` always get `authentication_denied` before `RevalidateMarkedCommitAsync`, where the baseline accepted any valid marker. The correction-propagation command types (for example `StartMailboxAssociationCorrectionPropagation`) are absent from `ChatBotCommandAdmissionCatalog`, so revalidation would also refuse them as command-not-allowlisted. The dispatcher-enriched `riskClassification` rejection and the skipped profile and concurrency checks are already in D8. `TrustedAuthorityInternalProducerTests.InternalWorkflowProducerMarkerProvesIntegrityWithoutAdmittingMutation` only asserts `IsAccepted == false`; Story 1.8 should pin the rejection reason when it gives internal producers a commit-participation path.
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Classified AI commands need a wire `policySnapshotId` that the UI never sends, so UI Ask AI becomes a terminal `classifier-indeterminate` once owner evidence exists (medium; deferred by the user to Story 1.6 live persistence: no policy source exists yet, and production refuses with 503 before classification).
  evidence: `MatchesClassificationRequest` rejects a blank `policySnapshotId` (`ChatBotAdmissionEvidenceValidator.cs:172`), and `ValidateProfileAsync` keeps exact wire-policy equality for classified commands (`:251-252`). `SubmitAskAiAsync` passes `PolicySnapshotId: null` (`ProjectConversationService.cs:249`), and CLI/MCP `ExecuteApprovedAIAction` never set it. `DeterministicAiActionRiskClassifier` copies the payload value into the record, so binding to the owner policy must change the classifier and the validator together. Round 1's rejection ("the UI always supplies it") was factually wrong.
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: The non-self approval check compares the approver with the payload-supplied `RequesterId`, so self-approval would pass (medium; deferred by the user to Story 1.10 with the round-1 D2 follow-up: approver authority from owner evidence is already deferred there, decisions are refused today, and nothing produces approval evidence).
  evidence: `ChatBotAdmissionEvidenceValidator.cs:302` checks `Decision.DecisionActorId == Request.RequesterId`. `AiActionApprovalRequested.RequesterId` comes from the `ProposeAIAction` payload (the UI always sends `"ui-requester"`, `ProjectConversationService.cs:239`). The authenticated proposer (`envelope.UserId`, stored on the proposal record) is not carried into the approval request, and `Handle(DecideAiActionApproval)` never checked non-self. Story 1.10's owner approval evidence must bind independence to trusted principals.
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: No surface can execute a UI-originated approved action, because the owner now binds `SourceConversationItemId` and CLI/MCP cannot supply it (medium; deferred by the user to Story 1.8: nothing can execute before commit participation, and CLI/MCP are obsolete migration sources).
  evidence: Round-2 patch R2-B7 added `SourceConversationItemId` equality to `GovernedOperationAggregate.ValidateApprovedExecutionApproval` (`:4036`), which `Handle(ExecuteApprovedAIAction)` also uses. UI proposals always set it (`ProjectConversationService.cs:252`). The only `ExecuteApprovedAIAction` producers, `ChatBotCliService.cs:316` and `ChatBotMcpService.cs:147`, have no such argument, so execution returns `ApprovedCommandScopeExceeded`. The Story 1.8 or McpCli execution surface must carry `sourceConversationItemId` (update `SurfaceArms` parity and the MCP tool schema).
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Successor commands are chosen by adapters instead of being derived from committed state (medium; deferred by the user to Story 1.8: derivation needs committed predecessor state, and enforcing it now would break UI Ask AI with no producer to replace it).
  evidence: The addendum's transition rules require the pipeline to derive the next command and profile from committed state and to deny transitions that skip the distinct successor command. `ChatBotCommandAdmissionCatalog` has no predecessor or successor table. The UI submits `ProposeAIAction` directly (`ProjectConversationService.SubmitAskAiAsync`), and `ExecuteLowRiskAIAssistance` is accepted as an entry command. Only caller `successorCommand`/`nextCommand` fields are rejected (`ChatBotAdmissionEvidenceValidator.cs:53-54`).

- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Repair the existing Story 13.2 live browser fixture's current owner-authority prerequisites before claiming a passing production-client refusal matrix (high; pre-existing at the Story 1.7 baseline).
  evidence: `Story132ProductionBrowserAspireE2ETests` initializes the realm and recovery protocol but no current ChatBot operation/query grant, Party identity or Project authority. `AddStory132AcceptanceFixture` replaces only policy/provider/classifier services. The authoritative authorizer, acceptance fixture and recovery provisioner are unchanged from baseline `cd8314401488c7ba3453cec25bdf305e34e0d146`, whose positive-submit browser test already omitted the same owner prerequisites. Fresh state denies before the asserted 503 and before an authorized conversation can enable the composer. Closure requires owner-supported fixture provisioning and an executed fresh-state browser result; the unavailable atomic participation must not be bypassed, authority must not be fabricated and a 403 must not be represented as a passed 503 boundary.
  triage: 2026-11-24 Skipped. The live browser fixture still needs current owner grants, and another session holds uncommitted edits to `Story132ProductionBrowserAspireE2ETests.cs`.

## Deferred from: code review of spec-1-7-admit-commands-through-the-single-commandgateway (2026-10-21, round 3)

- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Marked-envelope revalidation also rejects the dispatcher's own `executionRecord`, not only `riskClassification`; this extends the D8 deferral (medium; masked while atomic participation always refuses, and Story 1.8 owns the commit-participation contract).
  evidence: The round-2 patch made `ValidateOperationAsync` reject any non-null `executionRecord` (`ChatBotAdmissionEvidenceValidator.cs:55-56`), and `RevalidateMarkedCommitAsync` (`ChatBotCommandAdmissionPipeline.cs:72`) runs that caller-input check on signed payloads. `AcceptedCommandDispatcher` signs `ExecuteApprovedAIAction` with `ExecutionRecord = record` (`:794`) and `ProposeAIAction`/`ExecuteLowRiskAIAssistance` with `RiskClassification` (`:682`, `:704`). So every gateway-dispatched AI command would self-reject at `/process` with `command_contract_invalid`. Story 1.8 must exempt both server-enriched fields on marked commits, not only `riskClassification`.

## Deferred from: code review of spec-1-7-admit-commands-through-the-single-commandgateway (2026-10-21, round 4)

- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: An approval for the append-only M0 conversation write may last 30 minutes as a "reversible internal write" (unverified; medium if true; CR4-A6, maybe-false). Unreachable today because no owner source produces approval evidence; Story 1.10, which owns approval evidence, should settle it.
  evidence: `ValidateApproval` allows 30 minutes whenever the only effect is `ModifiesState` (`src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotAdmissionEvidenceValidator.cs:321-322`). The addendum's Approval Freshness section caps reversible internal writes at 30 minutes and irreversible effects at 10 (`addendum.md:88`). `Project.AppendConversationMessage` is "append-only" (`addendum.md:112`), and no per-command reversibility metadata exists. A product answer settles it: is an appended AI conversation message reversible? If not, add reversibility to the command metadata and use the 10-minute bound.

## Deferred from: code review of spec-1-7-admit-commands-through-the-single-commandgateway (2026-10-21, round 5)

- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: The approval requester-authority drift check reads the executor's authority, not the original requester's current authority (medium; unreachable while no owner source produces approval evidence; CR5-A1, deferred to Story 1.10 with the round-2 D3 deferral).
  evidence: `ValidateApproval` (`src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotAdmissionEvidenceValidator.cs:309`) compares `approval.Request.RiskInputTuple` with the current classification's tuple. `BuildApprovedExecutionInput` (`src/Hexalith.ChatBot.Server/Gateway/Stages/DeterministicAiActionRiskClassifier.cs:135`) builds that tuple from the principal submitting `ExecuteApprovedAIAction`, and nothing ties that principal to `Request.RequesterId`, which is payload-supplied (D3). So a different executor with the same authority class passes, a legitimate executor with a different class is refused, and the addendum's Approval Freshness rule (requester/approver authority drift invalidates immediately) never reads the original requester's current authority. Binding the executor to the requester is not in the addendum (CR3-E10). Story 1.10 must read the original requester's current authority from owner evidence for the drift check. Closure evidence: a drift row where the requester's current authority changes while the executor's does not.
- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: The release-only M2 stop-ship gate stays red until Story 1.8 delivers commit participation, so no ChatBot release can pass until then (low; by design under the round-2 D1 decision; CR5-B6).
  evidence: `.github/workflows/release.yml:85` runs `TrivialGovernedCommandAspireE2eTests.CommittedGovernedStateShouldPassM2StopShipSweepsInRealDaprTopology`, which needs committed two-tenant governed state, and `semantic-release` depends on that job. The exit condition was recorded only in a YAML comment and test remarks, and Story 1.8's acceptance criteria (`epics.md:1040`) do not mention it. Story 1.8 must make the test pass in release.yml rather than delete, skip or weaken it. Closure evidence: a green release `topology-acceptance` job with the M2 step executed and passed.

## Deferred from: code review of spec-1-7-admit-commands-through-the-single-commandgateway (2026-10-22, round 6)

- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Governance and data-rights commands must supply approval proofs that cannot exist yet; settle per-command stage applicability and two owner/PRD divergences before Story 1.8 lets anything commit, alongside the D3 PRD decision (medium; fail-closed and unreachable while no owner source exists and atomic participation always refuses; CR6-D1, deferred by the user after a deep-dive).
  evidence: `ChatBotCommandAdmissionCatalog.RequiredStages` demands `separation-of-duty` and `independent-approval` for every governance-v1 command and `tenantowner-compliance-approval` for every data-rights-v1 command (`src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotCommandAdmissionCatalog.cs:306-307`); `StageExemptions` (`:234`) covers only the D4 lifecycle commands, and `ValidateProfileAsync` refuses any missing stage (`ChatBotAdmissionEvidenceValidator.cs:235`). Three groups need different answers. (a) Two-person initiators (the ten `Submit*Disable/Quarantine` commands and `SubmitTenantPolicyChange`) record a pending approval with no effect, and `Approve*` enforces approver ≠ requester (`GovernedOperationAggregate.cs:435-495`, `:139-175`). Exempt `independent-approval` for them only if the owner-source stage-reference contract defines a reference as a completed approval; if a pending approval counts, no exemption is needed. (b) The five `Submit*RateLimit` commands activate single-actor (`GovernedOperationAggregate.cs:1162`, pre-existing at baseline `cd83144`), but the PRD safety-control matrix requires an initiator plus an independent approver for every cell, rate limits included (`prd.md:1435-1438`). The owner must add the approval step, or the PRD must change. (c) Data-rights `Submit*` requests get only a gateway compliance-scope check (`ParticipantAuthorizationStage.cs:462-478`), and no TenantOwner/compliance approval command exists, although `addendum.md:191` requires independent approval. Do not satisfy (b) or (c) with admission exemptions: that would remove PRD-mandated stages. Closure evidence: an addendum table of each governance and data-rights command's initiator, approver or single-step role, with `StageExemptions` and the owner flows matching it.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Restore package-only CI/CD dependency validation after qualifying the required EventStore APIs in packages.
  evidence: R4-B1 (medium, verified): independent CI commit 0954093bb0e69775291298c7076a4eb1375e2b34 sets HexalithEventStoreFromSource=true in ci.yml, release.yml and build-release-configuration.sh; Parties and Tenants select EventStore ProjectReference on that flag even with UseHexalithProjectReferences=false. This predates the resumed Story 1.7 patches and conflicts with the mandatory package-only CI/CD baseline; do not remove the override until the required packaged API is qualified and the packaged Release graph passes.


## Deferred from: five evidence-gate fixes review (2026-10-22)

- source_spec: `spec-five-deferred-evidence-gate-fixes.md`
  summary: Add structural XML budgets or streaming projection to prevent DOM amplification inside the 64 MiB TRX byte/character limit (medium; pre-existing; BH-2).
  evidence: TrxEvidenceReader.ValidateTrx, RecoveryTrxSanitizer.Sanitize and RecoveryAttemptSummarizer.TryLoad all materialize XDocument before validating canonical structure, both at baseline fdeb341ee2b31dc54f3a5a0551de9e303544c457 and after the bounded-read fix. Millions of small elements fit inside the byte cap and retain a much larger DOM. Closure evidence: node/depth or streaming limits with adversarial TRX tests that refuse before large DOM construction.
  resolution: Implemented 2026-10-28 in spec-five-deferred-validation-limits.md: all three TRX consumers preflight the same bounded bytes before DOM construction, enforcing depth 64 and 1,000,000 combined reader nodes/attributes. Regressions exercise every consumer, exact boundaries and sidecar/output preservation; see validation/five-deferred-validation-limits.json. Individual start-tag buffering remains a separate parser limitation.
- source_spec: `spec-five-deferred-evidence-gate-fixes.md`
  summary: Bound aggregate discovery allocation and retain only identity metadata for inactive evidence contracts (medium; pre-existing; BH-3).
  evidence: TransitionDetector.FindSingleContract stores every full parsed candidate in its candidates list before filtering, as it did at baseline fdeb341ee2b31dc54f3a5a0551de9e303544c457. Many individually compliant contracts can exceed a reasonable aggregate memory budget. Closure evidence: identity-only candidate records plus a documented discovery budget and multi-candidate negative coverage preserving active duplicate detection.
  resolution: Implemented 2026-10-28 in spec-five-deferred-validation-limits.md: each active scan admits at most 4,096 files and 64 MiB combined bounded input bytes, retaining only detached identity strings. Regressions cover exact limits, excess bytes/files, malformed-byte and oversized-file accounting, retained identity shape, and existing active duplicate detection. See validation/five-deferred-validation-limits.json.


## Deferred from: five recovery-harness diagnostics review (2026-10-22)

- source_spec: `spec-five-deferred-recovery-diagnostics.md`
  summary: Captured recovery failures retain original inner exceptions whose rendered diagnostic text can escape the metadata-only wrapper message (medium; pre-existing).
  evidence: At baseline 6b92c2970d8fd7fb6bc2d3ad26f864e80313e111, all three Capturing* decorators retain the original exception as InnerException; LiveContinuityAspireE2eTests later aggregates those failures, so Exception.ToString can include provider messages or stack traces in raw TRX. The new capture tests verify wrapper-message redaction and exact cause retention, not full rendered-output sanitization. Closure requires an explicit diagnostic retention/redaction boundary with injected sensitive inner-message tests, coordinated with the still-open scheduled/release raw-TRX sanitization work (DW-95). This batch preserves the existing diagnostic contract and does not claim full exception rendering is sanitized.
  triage: 2026-11-24 Skipped. `RecoveryFailureCaptureTests` pins the retained cause. Keeping or redacting it needs a decision, and the raw upload is owned by CiCd (DW-95).


## Deferred from: five recovery-harness contract checks (2026-10-23)

- source_spec: `spec-five-deferred-recovery-contracts.md`
  summary: The existing realm service-grant test still expects claims-only authority after the resolver switched to owner evidence (medium, pre-existing).
  evidence: RecoveryValidationTopologyContractTests.RenderedRealmServiceClientGrantExpiryIsAcceptedByTheClaimsGrantResolver fails with service_client_grant_missing in TestResults/five-recovery-contracts.{ctrf.json,trx}. At baseline e97e7a8831768aee1ca1cb7cfc9ec5c64b7dfe68, the test already constructs ClaimsServiceClientGrantResolver without its optional authorizer, which deterministically denies; no owner RequestContext is established either. Neither resolver nor this existing test was changed by the batch. Repair the fixture with supported owner authority rather than restoring claim-derived authority.
  resolution: Already repaired by d718220801f7855dc4c7b6c0d140f3785e95d6e9; verified 2026-10-28 in the original Debug RecoveryValidationTopologyContractTests lane, 30 passed, zero failed/skipped. The current fixture proves rendered future canonical UTC expiries and explicitly denies claims-only authorization; production owner-authority requirements are preserved. See spec-five-deferred-validation-limits.md.

- source_spec: `spec-five-deferred-recovery-contracts.md`
  summary: The existing mailbox realm assertion rejects unrelated runtime-generated workload secrets (low, pre-existing).
  evidence: RecoveryValidationTopologyContractTests.RealmAddsClosedRecoveryMailboxServiceIdentityWithoutACommittedSecret rejects any RandomNumberGenerator substring in AppHost Program.cs, which already generates five non-mailbox workload secrets at baseline e97e7a8831768aee1ca1cb7cfc9ec5c64b7dfe68. This assertion and AppHost Program.cs were untouched. TestResults/five-recovery-contracts.{ctrf.json,trx} records the failure; scope a future assertion to the mailbox secret configuration boundary.
  resolution: Already repaired by d718220801f7855dc4c7b6c0d140f3785e95d6e9; verified 2026-10-28 by the 30/30 zero-skip original topology fixture lane. The mailbox assertion permits unrelated runtime secret generation, and rendered-client checks verify the configured mailbox secret without printing it. See spec-five-deferred-validation-limits.md.


## Deferred from: code review of spec-1-7-admit-commands-through-the-single-commandgateway (2026-10-23, round 7)

- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: `RunSettingsIsAcceptedByTheTestPlatform` never passes `live-recovery.runsettings`, so it proves only that the direct runner executes one probe; retire the probe and file or pass the settings (low; pre-existing since `bb64e24`; CR7-W1).
  evidence: The argument vector in `tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs:676` holds only `run-xunit-v4.sh`, the runner beside the test assembly, a CTRF path and `-method ...RunSettingsPlatformAcceptanceProbe`, with no `--settings`. No workflow, script or project file references `live-recovery.runsettings`, so the remarks ("runs the real runner against the real file") are stale. The Story 1.7 blocker fix (`fdeb341`) changed only the runner path to `AppContext.BaseDirectory`, which still resolves to `bin/Release/net10.0` in the Release lane. The choice depends on whether the file still has a local use.
  resolution: Implemented 2026-10-28 in spec-five-deferred-validation-limits.md: removed the unused settings file, obsolete XML check and both misleading acceptance/probe methods after confirming no workflow/script/project consumers. All 16 remaining LiveRecoveryValidationArchitectureTests pass with zero skips; the 14-test Python evidence-guard suite includes a real native-runner zero-test refusal with no checksum.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Normalize release-package output paths before cleanup so relative destinations keep the same meaning after the script changes directory.
  evidence: Cb-root-B1; pack-release-packages.sh creates and deletes packages relative to the caller, then changes to repository_root before packing and verification. An outside-directory invocation with artifacts can exit successfully with an empty caller destination and packages written under the repository. Normalize the path once, including dash-prefixed inputs, and test invocation from another directory.
  resolution: Resolved 2026-10-28 in spec-five-deferred-runtime-guards.md: normalize literal caller-relative directories before cleanup and repository cd, neutralize CDPATH, and reject empty destinations before mutation. Real-script regressions cover relative, absolute, dash-prefixed, space-containing and single-dash directories plus empty-input preservation. Both ordinary CI paths run the tests.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: Apply the 65,536-line input limit before sprint and technical-enabler ledger readers allocate line arrays.
  evidence: Cb-root-E1; SprintLedgerReader splits byte-bounded text and TechnicalEnablerLedgerReader uses VisibleLines(string), bypassing BoundedEvidenceInput.MaximumTextLines. A reviewer probe accepted one-million-line inputs of about 1 MiB, allocating approximately 23.5 MiB and 39.9 MiB. Route file readers through a bounded line reader, retain stable sprint-input-limit and ledger-input-limit refusals, and test newline-heavy inputs below the byte ceiling.
  resolution: Resolved 2026-10-28 in spec-five-deferred-runtime-guards.md: local and Git-text readers share bounded line admission with stable sprint-input-limit/ledger-input-limit refusals and unchanged fenced-example handling. Exact/excess LF, CRLF and unterminated inputs pass; full gate suite is 344/344 without skips.

## Deferred from: code review of spec-1-7-admit-commands-through-the-single-commandgateway (2026-10-23, round 8)

- source_spec: `spec-1-7-admit-commands-through-the-single-commandgateway.md`
  summary: The addendum's ai-read-v1 row omits the Project authority that the production catalog and the frozen Story 1.7 matrix require; record it as a versioned PRD/addendum decision so profile derivation from the addendum keeps the stage (low; CR8-W1).
  evidence: `src/Hexalith.ChatBot.Server/Gateway/Admission/ChatBotCommandAdmissionCatalog.cs:304` gives ai-read-v1 `project-authority`, following the Story 1.7 I/O matrix row "current policy/context/Project authority". `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:186` lists only origin/instruction-boundary validation, AI risk classification and current policy/context-package scope, and `addendum.md:213` makes any profile change a versioned PRD/addendum decision. The divergence is recorded only in the `AdmissionProfileFitnessTests` stage-pin comment (CR7-Cc-P3).

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-10-24, round 9)

- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Order stale same-instance `Started`/`Delayed` workflow reports, the coordinator's post-schedule `Started(0)` and cross-instance reports against newer operation-status progress with a per-instance report sequence.
  evidence: R9-D1 residual. `OperationStatusWorkflowStatusSink.cs:68` stamps `max(now, existing.LastUpdatedAt)` inside the atomic callback, so a report that loses CAS ties and wins under the legacy policy (`OperationStatusUpdatePolicy.cs:45`). The round-9 narrow guard skips only `Retrying`/`Delayed` over `Failed` and lower-count `Retrying` over `Retrying`; a stale `Started` after `Failed` is indistinguishable from the supported restart (`OperationStatusWorkflowStatusSinkTests.cs:379-386`), `Started`/`Delayed`/`Completed` always report retry count 0, and `DaprCorrectionPropagationCoordinator.cs:45-57` reports `Started(0)` after scheduling. Whether one operation can carry two instances (`DaprCorrectionPropagationCoordinator.cs:76`) is unverified. Live-unreachable while Story 1.7 refuses every admission; resolve with the R8B1/R8B3 workflow/receipt ordering follow-ups before Story 1.8 admits correction operations.

## Deferred from: code review of spec-1-6-persist-the-first-isolated-policy-snapshot (2026-10-25, round 29)

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Repair the obsolete duplicate-suppression source assertion used when browser startup is unavailable (medium; Story 1.5 / UI E2E test owner).
  evidence: Round29-12, verification-gap Other findings, reproduced by the parent. `DuplicateRetryFailureStatesE2ETests.cs:352` requires `PartialOutputCodes = ["duplicate_suppressed"]`, but `ChatBotCommandAdmissionPipeline.cs:469` now unions the marker with earlier codes. Blame attributes that production change to Story 1.5 commit `dd640b6c`, independent of Story 1.6. `CHROME_EXECUTABLE_PATH=/bin/false dotnet tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests.dll -method '*DuplicateMailboxDelivery_SuppressesDuplicateAndKeepsOriginalArtifacts'` exits 1, one failed test at line 352; the focused UI E2E build passes with zero warnings/errors. Update the assertion to tolerate preserved prior codes, retaining the production union. Logs are in `/tmp/bmad-review-1-6-7oahj5xh/ui-fallback.log`.
  resolution: Resolved 2026-10-31 (Story 1.5 R10-W2 follow-up, test-only): `DuplicateRetryFailureStatesE2ETests.cs:352` now pins the production union form `PartialOutputCodes = [.. (replayStatus.PartialOutputCodes ?? []).Union(["duplicate_suppressed"], StringComparer.Ordinal)]` from `ChatBotCommandAdmissionPipeline.cs:469`; production code is unchanged. With `CHROME_EXECUTABLE_PATH=/nonexistent/chrome` the fallback runs without launching a browser: the method failed at line 352 before the change and passed after it, and the class passed 4/4. On the default path the class also passed 4/4 with three real Chrome launches under `DEBUG=pw:browser`, one per browser test. Full UI.E2E passed 289/289 with zero skips. Evidence is under `TestResults/story-1-5-r12/`.

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Carried existing BH-5 / Current-BH-2 obligation to defensively copy provider-owned AI policy lists and maps before enabling a live provider (medium; no new scope).
  evidence: Round29-6 reconfirms that `TenantAiPolicySnapshot` still retains caller-owned collections under read-only interfaces. Mutation can change permissions under the same policy identity. This predates the Story 1.6 baseline; the separate synthetic snapshot model copies collections, and production still uses the unavailable provider. This review references the existing obligation rather than creating another implementation task.

- source_spec: `spec-1-6-persist-the-first-isolated-policy-snapshot.md`
  summary: Carried existing R4-BH-2 / Current-BH-3 obligation to establish provider-collection failure containment before live activation (unverified medium; no new scope).
  evidence: Round29-5 reconfirms enumeration and lookup outside `DefaultAiActionPolicyEvaluator`'s provider-call catch. Current providers return ordinary arrays/maps and production returns null. A supported provider whose collection access throws would settle reachability and require a redacted unavailable decision. This review references the existing verification obligation rather than creating another implementation task.


- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-platform-owner-service-access.md`
  summary: Supply owner-observed file version and lock-fence evidence in the ChatBot Folders attachment adapter before calling the current Folders upload API.
  evidence: The final owner-service repair full Server lane ran 3303 tests with 3296 passing and seven unchanged FoldersFolderStoreTests failures. The unchanged adapter calls FileUpload.BuildInlineFileMutation without expectedFileVersion/expectedLockFence, which the unchanged current Folders SDK rejects before transport. Exact CTRF/TRX/log evidence is /tmp/chatbot-owner-final-full-server.*; no assertion or SDK guard was weakened.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-platform-owner-service-access.md`
  summary: Reconcile legacy Platform composition test awaits with the umbrella repository's CA2007 requirement without suppressing the analyzer gate.
  evidence: The broad Platform.Composition.Tests Debug build reports 36 CA2007 errors in unchanged legacy test helpers under the ancestor editor configuration (/tmp/chatbot-owner-platform-tests-build.log). New owner-service tests run in a focused test project and pass; the broad blocker remains explicit and unrelated legacy test files are unchanged.

## Deferred from: code review of spec-1-6-platform-owner-service-access (2026-10-25, round 2)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-platform-owner-service-access.md`
  summary: Give the Projects owner client a dedicated audience so its token no longer authenticates at EventStore and EventStore Admin.
  evidence: Projects shares the `hexalith-eventstore` audience (`src/Hexalith.ChatBot.AppHost/Program.cs:267`, `:296`, `:319`). Round 2 closed the tenant-scoped reach by omitting `eventstore:tenant` from owner tokens. The token still authenticates at EventStore, so anything gated only on authentication is reachable. A dedicated audience needs a Projects-owned change: `ProjectsAuthenticationServiceCollectionExtensions.cs:90` validates a single `ValidAudience`, and `EventStoreGatewayTokenForwardingHandler` forwards caller bearers to the EventStore command gateway, which requires `hexalith-eventstore`.


## Folders attachment owner-evidence follow-up (2026-10-25)

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Provide owner-authorized absent-path and original replay preconditions for native ChatBot attachment Add/replay.
  evidence: `AuthoritativeWorkspaceFileContextSource.QueryAsync` returns the same safe 404 for absent and forbidden direct paths, while `WorkspaceContentAuthority.ValidateIngressAsync` requires expectedFileVersion=0 for fresh Add and original version/fence for replay. Visible metadata supplies only a current positive version. The repaired ChatBot adapter forwards observations unchanged, preserves owner conflict rejection, and never interprets 404/empty metadata as version zero. Stubbed AcceptedCommand tests verify adapter behavior only; native Add/replay remains blocked on a supported owner API.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Serialize the existing owner lock fence through the Folders workspace-lock HTTP response.
  evidence: `WorkspaceLockStatusQueryHandler` populates `Lease.LockFence`, but `FoldersDomainServiceEndpoints.ToHttpResult` and `LockLeaseMetadataResponse` omit it. The generated SDK consequently observes zero. A generated-client HTTP fixture reproduces the omitted field and requires a retryable evidence-unavailable result with no upload. ChatBot does not fabricate a fence or acquire/release another task's lock.

Validation update for the earlier seven-FoldersFolderStoreTests entry: the ChatBot adapter now reads and validates supported owner metadata/lock observations before building an upload and preserves owner admission. Debug source build passes with zero warnings/errors; focused Folders tests pass 66/66 and the subsequent full Server project passes 3,421/3,421, both with zero skips. Evidence: `/tmp/chatbot-folders-focused.{ctrf.json,trx,log}` and `/tmp/chatbot-folders-full-server.{ctrf.json,trx,log}`. The exception/test regression is repaired; the two native owner gaps above remain open, and Story 1.6 remains review pending accepted A6 protection and native-isolation evidence.


## Deferred from: resumed Folders owner-evidence review (2026-10-25)

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Publish and qualify the Folders SDK observation APIs through the shared package catalog before package-mode release.
  evidence: R2-B1 (high): the catalog pins Hexalith.Folders.Client 1.3.1; a minimal compile against that package fails CS1061 for LockLeaseMetadata.LockFence and FileUploadDescriptor.ExpectedFileVersion/ExpectedLockFence. Exact command/output: /tmp/chatbot-folders-review-gsvpxxxd/package-compatibility.log. This repair explicitly validates Debug source references; coordinate the owner publication/catalog update and prove a package-only Release build separately.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Supply owner-issued absence and original operation preconditions for native attachment creation and accepted replay recovery.
  evidence: R2-B2/R2-B4 (high): WorkspaceContentAuthority requires zero for fresh absent-target Add and the original fence/version preconditions for accepted replay. Current positive metadata and an active current lease establish neither; accepted replay can legitimately outlive that lease. Keep the adapter fail-closed until supported owner evidence proves both paths without inventing preconditions.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Include the owner lock fence in the Folders workspace-lock HTTP contract.
  evidence: R2-B3 (high): FoldersDomainServiceEndpoints creates LockLeaseMetadataResponse without LockFence, leaving the generated client's value zero. The existing real-client HTTP fixture requires safe rejection. Closing this requires the owner response change and a successful native contract/persisted-state test.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Establish an owner-managed lease lifecycle for each attachment task before storage observation and submission.
  evidence: R2-B5 (high): AttachmentCaptureCoordinator obtains content then calls IFolderStore without acquiring/renewing the deterministic attachment task's workspace lease. The adapter requires that task as the current holder and intentionally only reads evidence. Demonstrate consecutive attachments and lease expiration/recovery through supported owner APIs without borrowing another task's fence.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Distinguish owner concurrency conflicts from actual duplicate admission in attachment upload outcomes.
  evidence: R2-B6 (medium): WorkspaceContentAuthority rejects invalid version/fence preconditions, FoldersDomainServiceEndpoints maps that to concurrency_conflict, and the preexisting ChatBot upload-409 branch reports duplicate-pending for every 409. Add typed safe-problem classification and regressions preserving genuine duplicate replay behavior; observation 409 already stays not-evaluated.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Align attachment operation IDs and filename paths with Folders canonical request validation while preserving existing replay identities.
  evidence: R2-P1 follow-up (high native capability blocker): AttachmentStorageIdentity emits colon-bearing operation IDs and retains spaces/non-ASCII filename characters; Oq2RequestValidation accepts only opaque alphanumeric/underscore/hyphen IDs and ASCII path tokens. Real generated-client probes reject ordinary invoice.pdf at mutation serialization and sales report.pdf/résumé.pdf at metadata serialization. This repair safely maps those exceptions; an owner-compatible identity/path migration with replay evidence is still needed.


## Deferred from: third Folders owner-evidence review (2026-10-25)

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Bind stored attachment references to the owner-allocated file incarnation after authoritative publication.
  evidence: R3-B9 (high, preexisting): WorkspaceFileMutationService allocates Add's OwnerFileId with UniqueIdHelper.GenerateSortableUniqueStringId, while AttachmentStorageIdentity.FileIdFor hashes the operation ID. AcceptedCommand does not establish that those identifiers match, and preflight metadata can describe a different file incarnation. Resolve through owner-confirmed publication evidence with read/scan and replay tests; do not substitute an existing path's ID or silently change stable identities in this observation-only repair.

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Verify whether permanent protocol/capability statuses can reach the Folders adapter's generic retry mapping.
  evidence: R3-B7 (medium if confirmed, unverified): the fallback maps otherwise unclassified HexalithFoldersApiException statuses to retryable. No current owner endpoint emits the cited 405/415/501 codes for the generated client's fixed methods and media types. A real transport/deployment response proving one of those statuses on this path would settle reachability and justify a terminal classification regression; no speculative branch was added.


## Deferred from: fourth Folders owner-evidence review (2026-10-26)

- source_spec: `_bmad-output/implementation-artifacts/spec-fix-folders-attachment-owner-evidence.md`
  summary: Qualify attachment upload and replay with native owner persisted-state tests when the existing owner contract blockers are resolved.
  evidence: R4-B9 (medium, preexisting acceptance gap): FoldersFolderStoreTests configures accepted/replayed results through RecordingFoldersClient, while real generated-client fixtures currently cover failure paths. Those tests deliberately prove forwarding and safe failure handling only. After owner-authorized absence/replay evidence, fence transport, lease lifecycle, identity/path compatibility and owner publication binding are available, assert the persisted file identity and bytes and response-loss replay without duplicate publication. This is the acceptance test for the existing owner follow-ups, not a claim of native success or Story 1.6 acceptance.


## Native inline Folders implementation follow-up (2026-10-26)

- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: The approved native implementation supplies the earlier owner absence/replay, lease lifecycle, fence transport, canonical identity/path, typed conflict and confirmed file-incarnation gaps in source mode.
  evidence: Additive native v1 HTTP/generated client, real FolderDomainProcessor, CAS-persisted source events and real EventStore SDK bytes/ledger now prove fresh Add, consecutive attachments, retained legacy identity, response-loss/expiry replay, exact owner file identity and duplicate-admission race forwarding. The final source-mode Folders lane passes 117/117; owner evidence/lease lane passes 23/23; SDK content admission/sealing lane passes 22/22; canonical legacy-key golden passes 1/1, all with zero skips. Actual lock projection folding and both existing generated lock/transition fence decoders are covered. Evidence directory: `/tmp/chatbot-native-implementation-RB2Akn`. Historical deferred entries above retain their original evidence; these are source implementation dispositions subject to the new spec's independent review.

- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: Coordinate owner SDK publication/shared catalog qualification and actual native backend/isolation acceptance separately.
  evidence: No package is published and no dependency pin is changed. The shared catalog still pins Folders.Client 1.3.1, which predates both the frozen repair observation fields and this new native companion. Persisted test-store tests and the healthy Aspire baseline do not qualify production Dapr CAS/native storage isolation. Story 1.6/sprint remain in review, and A6 protection/native-isolation acceptance remain outstanding.

Validation update: full unfiltered Server passes 3,472/3,472 with zero skips using `-parallel none` after an initial parallel-only global-meter assertion failure (3,471 passed/1 failed). Both CTRF/TRX/logs are retained. Package-only Release qualification exits 1 with seven errors because unchanged Folders.Client 1.3.1 lacks the native companion types; exact command/result is `commands.json` and `package-only-release.log` in the implementation evidence root. Package publication/catalog and production backend/isolation remain open.


- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: Qualify native SDK publication and package-only Release after the supported owner artifacts are available in the shared catalog.
  evidence: Review B2 confirms the exact package-only Server Release command exits 1 with seven missing-native-type errors against unchanged Folders.Client 1.3.1; `commands.json` and `package-only-release.log` under `/tmp/chatbot-native-implementation-RB2Akn` retain the command/result. Package publication and dependency changes are explicitly excluded from this implementation.

- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: Retain full C7 revocation and expired-to-stale runtime qualification as a separate owner acceptance item.
  evidence: Review B10 confirms approved C7 documentation leaves revocation handling, authority propagation and exact expired/stale status boundaries reference-pending. Native operations independently reauthorize and fail closed; profile validation and controlled renewal tests do not prove production upstream revocation timestamps or stale-query propagation.


Final native follow-up qualification (2026-10-26): independent review corrections are complete in source mode. Parent checks pass SDK 28/28, owner 64/64, existing fence decoders 2/2, trusted gateway admission/catalog 8/8, decoder/canonical-key 5/5, focused ChatBot Folders 121/121 and full unfiltered Server 3,476/3,476 (`-parallel none`), all zero skips. Exact commands, CTRF/TRX, blocker output and final source/evidence checksums: `/tmp/chatbot-native-review-u5g9_rps`. The separate package-only Release attempt still exits 1 with seven missing-native-type errors against unchanged Client 1.3.1. SDK publication/catalog, full C7 propagation/stale qualification, actual backend/isolation and Story 1.6 A6 remain open; prior entries/evidence above remain historical snapshots.


## Deferred from: code review of spec-1-6-platform-owner-service-access (2026-10-27, round 4)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-platform-owner-service-access.md`
  summary: Run the owner-service Server and Integration tests in CI once the Folders package-mode break is fixed.
  evidence: CI run 40277543114 at `f75d1de` fails the `build` job at Build and story-evidence-integrity at "Build Release configuration". The cause is the already-recorded R2-B1 Folders.Client 1.3.1 gap: the 36 CS errors are all in `FoldersFolderStore.cs:60-102`. So the lanes holding `OwnerServiceAuthenticationTests`, `MemoriesDerivedStoreDependencyInjectionTests` and `OwnerServiceTopologyTests` never execute; the only run of these tests is the local Debug receipt. No separate fix: it settles when the Folders observation APIs ship in the shared catalog.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-6-platform-owner-service-access.md`
  summary: Re-verify the AppHost build and the owner topology after the Platform gitlink advance to `b117945` (maybe-false, unverified medium).
  evidence: `25a269f` and `85e69f7` moved Platform from `5338a24` (last owner verification 2026-10-25 16:55 UTC) to `b117945`. That brings in Story 2.5 Composition sources the AppHost compiles: `AgentsHostComposition.cs`, `AgentsDeploymentProfile.cs`, `AgentsIngressDenialContext.cs` and `CoordinatedProtectionAcceptance.cs`. Settle with a clean-worktree AppHost Debug build plus the AppHost (19) and `OwnerServiceTopologyTests` lanes at `f75d1de`.

## Deferred from: code review of spec-native-folders-inline-attachments (2026-10-27)

- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: Package-mode builds cannot compile the native path in ChatBot or Folders (carried B2, new Folders scope).
  evidence: `dotnet build src/Hexalith.ChatBot.Server -c Release -p:UseHexalithProjectReferences=false` fails with 7 errors (CS0234/CS0246 on `Hexalith.Folders.Client.Native`). In package mode `Hexalith.Folders.Server` also uses `ContentTaskSealRequest`, `ContentUploadRequest.LeaseFence` and `TrySealTaskForCompletionAsync`/`TryQuiesceTaskLeaseAsync`, which the pinned `Hexalith.EventStore.DomainService` 3.127.0 (and cached 3.128.2) lack, so the Folders Release/`UseNuGetDeps=true` workflows should go red too (inferred from package symbols, not built). Settles when EventStore DomainService, then Folders.Client, publish and the pins advance.

- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: Move the native stub-handler tests to a file compiled in both build modes once the native SDK package exists.
  evidence: `NativeRuntimeMapsSafeTypedFailures`, `MalformedConflictProblemCannotEscapeNativeFailureMapping`, `NativeMalformedResponseAndTransportLossFailSafely` and `LeaseObservationUsesEarlierRenewalAndPreservesShortExpiry` sit in the source-only `FoldersNativeAttachmentTests.cs` (`Hexalith.ChatBot.Server.Tests.csproj:7-9` removes it in package mode), so CI never compiles them. They need `Hexalith.Folders.Client.Native`, so the move only helps after the pin advance above.

- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: Native storage handles only inline attachments up to 256 KiB and reports larger ones as retryable `attachment_content_streaming_required`.
  evidence: Pre-existing mapping: the legacy adapter returns the same retryable reason (`FoldersFolderStore.cs:27-34`); the spec scopes native Add to inline content (E4 rejects streamed bodies). Most real email attachments exceed 256 KiB. Settles with a native streamed Add or a coordinator retry ceiling for that reason.

- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: The native target path keys only on provider message, ordinal and name, while the operation id also keys on intake, mailbox and attachment.
  evidence: Pre-existing identity semantics (legacy `PathFor`/`OperationIdFor` diverge the same way; the spec preserves path semantics). A message re-ingested under a new intake gets non-retryable `folders_target_conflict` from the absence check instead of duplicate suppression (`AttachmentStorageIdentity.cs:71`). Settle with an identity decision when re-ingestion is designed.

- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: Concurrent stores of one request may report a terminal idempotency conflict for a stored attachment (maybe-false, unverified medium).
  evidence: Both stores share the live same-task lease (`WorkspaceNativeEndpoints.cs:83-94`). If one publishes between the other's accepted lookup and its legacy-path absence query, the owner matches the native operation id with a different path digest and throws `idempotency_conflict` (`WorkspaceOperationEvidence.cs:63-66`), mapped terminal by `FoldersFolderStore.Native.cs:109`. `AttachmentCaptureCoordinator` handles one intake sequentially; settle by showing whether two `CaptureAsync` runs for one intake can overlap.

- source_spec: `_bmad-output/implementation-artifacts/spec-native-folders-inline-attachments.md`
  summary: Prerequisite before configuring `ChatBot:Folders` anywhere: an abandoned native lease blocks every later attachment in its association workspace (resolved from review decision D1, option 1).
  evidence: Only the holder's own task can clear an expired native lease, and only when clean (`FolderDomainProcessor.Native.cs:80-97`); others get retryable `native_contention`. A holder whose intake is never re-driven after a failed completion/release or a crash, or one with an uploaded-but-unpublished body after expiry (`AbandonUploadAsync` rejects task-bound uploads, `DaprAuthoritativeContentStore.cs:314`), blocks the workspace indefinitely. Capture re-runs only on association events for the same intake (`AssociationProjectionHandler`, `ReadModelProjectConversationProjectionStore.cs:189`), so contention losers also stay `Retryable`. Settles with (1) the owner's C7-deferred lock takeover and staged-work release (`c7-lock-authorization-timing.md:76-80`, Architecture+Security approval; spec Task 3 and the C6 native paragraph currently forbid takeover) and (2) a ChatBot re-drive for `Retryable` attachments independent of intake events.
  reason: Takeover/staged-work release is C7-deferred owner work needing Architecture+Security approval, and ChatBot has no timed re-drive for Retryable attachments; keep ChatBot:Folders unconfigured until both exist.


## Deferred from: review of spec-five-deferred-recovery-boundaries (2026-10-28)

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-recovery-boundaries.md`
  summary: Bound retention or quarantine of invocation-scoped isolation-probe sentinels when cleanup cannot complete.
  evidence: Independent review verified that a timed-out or failed deletion can leave up to four synthetic entries for a pair invocation, and later runs do not revisit those nonce-scoped identities; MemoriesDerivedStore also retains its known-ID entries. Nonce fencing is required to prevent late noncooperative deletion from masking a later probe's breach. Fast cleanup failures and timeouts now fail closed, and cleanup waiting is bounded; accumulated synthetic storage is not claimed bounded. A separate lifecycle policy must provide safe bounded reaping, retention or quarantine without deleting an active probe's evidence.


- source_spec: `spec-five-deferred-recovery-boundaries.md`
  summary: Reconcile caller cancellation and synchronous blocking with isolation-probe cleanup deadlines.
  evidence: The cumulative follow-up review found no post-cleanup caller check and an InvalidateAsync call that can block before returning its ValueTask; both predate the dependency-only follow-up.
  resolution: Implemented 2026-10-28: invalidation invocation runs independently inside the shared cleanup budget; caller cancellation is checked in cleanup finally, including pending seed/read failures, and before verdict/audit. Original Server suite: 3534 passed, zero failed/skipped. See spec-five-deferred-recovery-followups.md and evidence/five-deferred-recovery-followups.json; required live completion remains separately gated.
- source_spec: `spec-five-deferred-recovery-boundaries.md`
  summary: Track latest scheduler pass outcome independently of monotonic wall-clock success timestamps.
  evidence: CheckHealthAsync compares LastFailedAtUtc to the preserved success maximum, which can mask a failed pass during clock catch-up.
  resolution: Implemented 2026-10-28: a lock-protected nullable latest terminal-pass outcome drives health independently of monotonic success timestamps; starts and overlaps preserve it. Regressions cover equal/rollback timestamps, failed completion and recovery. Original Server suite: 3534 passed, zero failed/skipped. See spec-five-deferred-recovery-followups.md and its evidence summary.
- source_spec: `spec-five-deferred-recovery-boundaries.md`
  summary: Preserve retry eligibility when an M2 completion is rejected as regressed.
  evidence: RunM2SweepAsync commits the completion cadence partition before the status store rejects the regressed snapshot.
  resolution: Resolved 2026-10-28 in spec-five-deferred-runtime-guards.md: internal status-store acceptance controls partition commitment; refused completions preserve newer evidence, increment failure counts, retain unalerted-breach signals and retry after backoff. Same-coordinator rejection/catch-up cases pass; full Server suite is 3,540/3,540 without skips.
- source_spec: `spec-five-deferred-recovery-boundaries.md`
  summary: Reject invalid filesystem path syntax in dormant recovery evidence directories.
  evidence: Path.IsPathFullyQualified accepts absolute strings containing NUL; the existing dormant-shape check does not validate invalid path characters.
  resolution: Implemented 2026-10-28 for the reported NUL/platform-invalid-character hole: enabled and dormant absolute paths reject Path.GetInvalidPathChars while empty disabled defaults remain valid. Original Server suite: 3534 passed, zero failed/skipped. Full Windows component legality remains a separately recorded unverified follow-up; see spec-five-deferred-recovery-followups.md and its evidence summary.
- source_spec: `spec-five-deferred-recovery-boundaries.md`
  summary: Preserve a witnessed isolation breach and locator when a later seed or read fails.
  evidence: ProbePairInternalAsync's outer exception fallback discards earlier cross-tenant observations and reports generic Unknown; strict fail-closed status already remains intact.
  resolution: Implemented 2026-10-28: earlier observations survive subsequent seed/read failure and cleanup failure, retaining Breach/reason/first locator in result, audit and alert. Successful cleanup asserts empty owner partitions. Original Server suite: 3534 passed, zero failed/skipped. See spec-five-deferred-recovery-followups.md and its evidence summary.

- source_spec: `spec-five-deferred-recovery-boundaries.md`
  summary: Restore the recovery-attempt summary CLI grammar entry.
  evidence: Program.Main implements summarize-recovery-attempt and CI invokes it, but CommandArguments.AllowedOptions omits it; the exact supported invocation exits 2 with status_mismatch/cli-command.
  resolution: Implemented 2026-10-28: the existing command now admits only its handler options; real Program.Main tests cover default policy, parsed/absent/malformed TRX, optional outcome and malformed/duplicate/missing/unknown options. Full gate suite: 313 passed, zero failed/skipped. See spec-five-deferred-recovery-followups.md and evidence/five-deferred-recovery-followups.json.


## Deferred from: review of spec-five-deferred-recovery-followups (2026-10-28)

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-recovery-followups.md`
  summary: Qualify broader Windows directory-component syntax in recovery evidence configuration beyond platform invalid path characters.
  evidence: The existing absolute-path check now rejects Path.GetInvalidPathChars, including the confirmed NUL defect. Windows wildcard/other component-invalid names need platform-native tests and root/extended-path compatibility before claiming exhaustive filesystem syntax validation; Windows runtime behavior was not executed in this Linux run. This pre-existing wider limitation does not justify rejecting legal Linux names.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-recovery-followups.md`
  summary: Re-arm the singleton scheduler pass-failure alert after a successful intervening pass.
  evidence: CheckHealthAsync now observes the latest terminal pass independently of clocks. The pre-existing reason-keyed _lastSchedulerAlertByReason stamp still suppresses fail-success-fail episodes within M2MissedCadenceAlertResuppressAfter. Qualify that sequence on one coordinator and decide when success should reset the failure-episode throttle; ordinary repeated-failure suppression must remain.
  resolution: Resolved 2026-10-28 in spec-five-deferred-runtime-guards.md: successful recovered passes re-arm the whole-pass failure episode even without an intervening health check; unresolved M2 attempts during backoff preserve suppression. Same-instance fail-success-fail and persistent-failure/backoff/expiry cases pass. Full Server suite is 3,540/3,540 without skips.


## Deferred from: review of spec-five-deferred-validation-limits (2026-10-28)

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-validation-limits.md`
  summary: Ignore unrelated inactive identity JSON containing duplicate object members without aborting active discovery.
  evidence: Pre-existing lazy JsonNode dictionary construction throws ArgumentException on candidate storyKey access when an unrelated property is duplicated; the same field access exists at baseline c4e3cbc0b8abf417fa697f991b16726c3a6d74d7. A direct SDK reproduction returned ArgumentException. The current catch handles JsonException only. Settle with local duplicate-member refusal and an active-completion regression; no full-contract grammar relaxation is implied.
  resolution: Resolved 2026-10-28: Inactive duplicate-member JSON is locally contained during identity extraction; active changed/selected duplicate-member contracts remain refused in four regression cases. Full gate suite passes 331/331 without skips. See spec-five-deferred-evidence-completeness.md and validation/five-deferred-evidence-completeness.json.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-validation-limits.md`
  summary: Qualify parser allocation for a concentrated XML start tag before structural TRX preflight can observe its attributes (medium if confirmed, unverified).
  evidence: XmlReader.Read buffers a start tag before AttributeCount becomes available, including in the previous direct DOM loaders. The new preflight bounds subsequent DOM construction and dispersed attributes, not streaming parser internals. Existing 64 MiB byte/character ceilings remain. A single-element distinct-attribute stress case with retained allocation measurements would settle whether a tighter parser or lexical admission limit is needed.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-validation-limits.md`
  summary: Reuse one bounded identity-discovery inventory within a multi-transition Detect invocation.
  evidence: Story, technical-ledger and sprint completion lookup paths each call FindSingleContract, both at baseline c4e3cbc0b8abf417fa697f991b16726c3a6d74d7 and now. Retained memory is bounded per scan, but a batch repeats file reads and JSON parsing. Cache lazily within one Detect invocation and qualify multi-transition read counts while preserving duplicate and inactive-input behavior; the current documentation explicitly promises per-scan budgets only.
  resolution: Resolved 2026-10-28: One lazy bounded inventory now serves every story, technical-ledger, sprint-story and sprint-action lookup in a Detect invocation. A real multi-transition Git fixture asserts one read per file, no reads for no-transition and fresh ambiguity detection on a later invocation. Existing discovery-budget and active-duplicate cases remain passing. See spec-five-deferred-evidence-completeness.md and validation/five-deferred-evidence-completeness.json.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-validation-limits.md`
  summary: Reconcile the existing FrontComposer EventStore Admin package overrides with the current shared Builds catalog.
  evidence: The original full Architecture Debug suite reports 156 passed, one failed, zero skipped. RootDeclaredSiblingPackageWrappersShouldDelegateVersionsToSharedBuildsCatalog rejects unchanged FrontComposer Directory.Packages.props selecting Hexalith.EventStore.Admin.Abstractions 3.128.2 against shared catalog 3.128.3. FrontComposer remains 5f665c4d38afb2255a7dde4620330e09824a40a1; Builds remains 81ab049208678bd6616828824729c0ddac02f7d1. Exact command and result are retained in validation/five-deferred-validation-limits.json; no dependency or submodule change is included.
  resolution: Resolved before 2026-11-24 by FrontComposer c092e8f9, recorded at gitlink 781a48f (868f27e). FrontComposer's `Directory.Packages.props` only imports the shared catalog, and `RootDeclaredSiblingPackageWrappersShouldDelegateVersionsToSharedBuildsCatalog` passed on 2026-11-24.


- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-ui-presentation-lifecycle.md`
  summary: Repeated populated conversation streams sharing the same ItemId values still duplicate child accessibility IDs.
  evidence: Blind review confirmed participant/attachment/approval reason IDs and failure announcements derive from ItemId in existing item components. The current product mounts one stream and separately scopes attachment copies; DW-117 fixes hard-coded composer and stream-title IDs only. Propagate stream instance scope through item status/evidence/history/action components and verify two actual populated streams with identical items before claiming repeated-feed isolation.
  resolution: Resolved 2026-11-24. `ChatBotConversationStream` now has an `IdScope` that defaults to its instance id and passes it to every item component. The participant, attachment, decision, approval, failure, AI-outcome and email items append it to each id they build from `ItemId` or `AssociationId` (evidence-chip and unavailable reasons, the approve reason, the failure announcement and its safe action), following the attachment `IdScope` pattern. Standalone items keep unscoped ids, and the failure announcement data attribute and deduplication key stay unscoped. The status, classification and review-history children build no ids. `ConversationAccessibilityRenderingTests.RepeatedPopulatedStreamsShouldScopeItemIdsAndResolveReferencesPerStream` renders two streams holding the same seven item kinds, with the default and an explicit scope, and checks that every id is unique, carries its stream's scope and survives a rerender, and that every aria reference resolves within its own stream; before the change it found 10 duplicated ids. UI.Tests passed 636/636 and UI.E2E.Tests 298/298 with 0 skipped.

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-10-30, round 10)

- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Order stale or reordered same-instance workflow reports outside the round-9 R9-D1 guard: a stale `Started(n)` after `Failed`, any stale report after the final `Delayed`, a stale `Retrying` over a stored `Started(k)`, equal-count store-phase `Retrying(0)`, and the coordinator's post-schedule `Started(0)` (medium; amends the round-9 residual entry).
  evidence: R10-W1 (R10B1–B4, R10E1–E5, R10A1). `OperationStatusWorkflowStatusSink.cs:46-50` skips only `Completed`, `Retrying`/`Delayed` over `Failed`, and a lower `Retrying` over `Retrying`; `:72` still stamps `max(now, existing.LastUpdatedAt)`. A stale `Started` after `Failed` clears `TerminalReasonCode` and returns `CompletionStatus` to `AcceptedProjectionPending`. A stale report after `Delayed` permanently replaces the escalation reason and next action. Count rewinds move `WorkflowRetryCount`/`RetryCount` backwards, but `OperationStatusQueryHandler.cs:43-54` hides any stale `NextRetryAt` from public `retryEligible`. The workflow awaits every report activity (`CorrectionPropagationWorkflowRunner.cs:139-146`, `:155`), so staleness needs the coordinator's out-of-workflow `Started(0)` (`DaprCorrectionPropagationCoordinator.cs:45-57`) or an at-least-once duplicate activity execution. Corrects the round-9 premise: the runner's post-timer `Started` carries the case-resolution retry count 1–4 (`:143-146`), so a count rule could skip a stale `Started(n≥1)` after `Failed`. Combined with the round-9 `Retrying`-over-`Failed` skip, it would also drop a restarted run's reports until its first `Started(0)` lands (rejected R10E6/R10A2); choose the rule with the per-instance ordering follow-up. Live-unreachable while Story 1.7 refuses every admission; resolve before Story 1.8 admits correction operations.

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-10-31, round 11)

- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Carried R10-W1 stale same-instance workflow-report ordering (medium; amends the round-10 entry above with two facts, no new defect).
  evidence: R11-W1 (R11E1, R11E2, R11B4, R11A3). (1) The equal-count `Retrying` member can close without a sequence field: `WaitForRetryAsync` derives each due time from a history-recorded `ReadUtcAsync` plus `RemoteStatusPollDelay` (`CorrectionPropagationWorkflowRunner.cs:135`), so successive same-instance `Retrying` reports carry later due times and a duplicate activity execution repeats the same one; skipping an equal-count `Retrying` whose `retryDueAt` is earlier than the stored `NextRetryAt` is a candidate rule, and no test covers the reverse (T2 then stale T1) order. (2) The round-10 claim that `OperationStatusQueryHandler.cs:43-54` hides any stale `NextRetryAt` from public `retryEligible` holds only against early eligibility: a rewound `NextRetryAt` no longer matches runtime progress, so the handler drops it and `retryEligible` stays false until the post-timer `Started` report, a false negative bounded by one 30-second poll window. Live-unreachable while Story 1.7 refuses every admission; resolve with R10-W1 before Story 1.8 admits correction operations.

## Story 1.5 UI E2E follow-up (2026-10-31)

- source_spec: `spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Supply the route's heading and page title when `SafeOutcomeInteractionTests.WorkspaceRetryShouldDispatchFreshReload` mounts `ChatBotProjectConversationWorkspace` (medium; Story 1.5 / UI E2E test owner).
  evidence: The test mounted the workspace with only `ProjectId`, so `HeadingText` stayed empty and `PageTitle` null. FrontComposer `adc0dbe2` (2026-10-23) made `FcPageHeader.OnParametersSet` (`FcPageHeader.razor.cs:145-152`) throw `InvalidOperationException: FcPageHeader requires a non-blank PageTitle or Heading so the browser document has a title.` when both are blank. ChatBot first pinned a FrontComposer commit containing the guard in `9718ef6` (2026-10-25); the later bump `3e1f6e1` (2026-10-28) only moved an already-guarded pin. The failure is deterministic: the round-11 full UI.E2E run was 288/289 (`TestResults/story-1-5-r11/ui-e2e-full.log`), and the class alone was 12/13 at `a9b9aec`. The real routes always pass both values (`ProjectConversation.razor:5-10`, `ProjectWorkspace.razor:7-13`), so the product is unaffected. CI never reported it because the `build` job stops at the Architecture lane on the FrontComposer EventStore Admin catalog mismatch (2026-10-28 entry above) before UI.E2E runs.
  resolution: Resolved 2026-10-31 (test-only): the test now passes `HeadingText` and `PageTitle` as "Project conversation", the English values of the route's `ProjectConversation_Title` and `PageTitle_ProjectConversation` resources and the literal the sibling render hosts use (`WorkspaceLifecycleRenderHost.cs:19`, `VersionedSafeOutcomeRenderTests.cs:1114`). The `FcPageHeader` guard and FrontComposer are unchanged. `SafeOutcomeInteractionTests` passed 13/13, and full UI.E2E passed 289/289 with zero skips. Evidence is under `TestResults/story-1-5-r12/`.


## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-11-01, round 12)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R12-W1 — Reconcile unauthenticated-command denial auditing after the EventStore 3.129 HTTP-host adaptation (medium).
  evidence: `CommandGatewayAdmissionApiE2ETests.cs:2559` expects one authorization-failure audit fact, including a body-derived command type, but the unauthenticated request is now rejected before its handler. `ChatBotApplicationBoundaryResultHandler.cs:26-64` returns typed 401 without calling IAuditWriter; `e89a3ac` already discloses this failing audit expectation. The reviewed delta contains only two test files, so the production result-handler/audit integration is outside that scope. This is static verification: source mapping is blocked by Timesheets checkout/gitlink drift and the focused fallback build has dependency-wiring errors; no current test run is claimed.
  reason: Carry the already disclosed host-integration regression to that follow-up. Reconcile metadata-only denial auditing and the expected failure response without reading an unauthenticated body or merely removing the audit requirement.
  closure: Current passing unauthenticated-denial audit regression, expected behavior when its audit path is unavailable, and the host-boundary integration lane.
  resolution: Resolved 2026-11-02 (Story 1.5 code review round 15). `e48e87e` restored the metadata-only denial audit at the HTTP boundary (`ChatBotApplicationBoundaryResultHandler`). The user ratified its bounded read (R15-D1) in place of the "without reading an unauthenticated command body" condition: at most 16 KiB of an `application/json` body within 250 ms, recording only a canonical allowlisted `commandType`, otherwise `unknown_command`. Round-15 patches restore the NFR43 authorization-failure counter feed (R15-P1) and answer the gateway's typed dependency-unavailable 503 (`dependency_degraded`, retryable) when the audit path fails (R15-P4). Closure evidence: `CommandGatewayApi_ShouldRejectUnauthenticatedSubmissionBeforeDispatch`, the seven-row boundary-denial theory, `CommandGatewayApi_ShouldAnswerTypedDependencyUnavailableWhenBoundaryDenialAuditFails`, the boundary handler unit class (19/19) and the full Server lane (3645/3645), under `TestResults/story-1-5-r15/`.

## Deferred from: Story 1.5 source-recovery build review (2026-11-01)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: U1-B5 — Qualify renewed/validated native Folders lease metadata before replacing the current lease (unverified medium; native adapter owner).
  evidence: FoldersFolderStore.Native.cs validates acquisition fields at lines 50-53, but MaintainAsync replaces renewal/validation responses directly at lines 89-91. A zero observation interval or inconsistent identity/fence/time could cause repeated immediate requests or loss of the current lease. No current owner response producing those malformed values was shown. Settle with the supported owner's response invariants and a controlled malformed-renewal transport case; this separately committed native adapter was unchanged by Story 1.5 source recovery.
  resolution: Resolved 2026-11-25. Renew and validate accept only the existing ownership fence and operation-appropriate time window. A malformed response cancels pending work and cleanup uses the last valid lease. The source-mapped native HTTP suite passed all 29 cases, including eight altered responses from the real owner endpoint. The focused review covers a healthy validation during upload, cleanup after malformed acquire timing, and rejection of renewal before the current boundary.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: U1-B7 — Re-query authoritative project-conversation state after the initial successful tenant subscription (medium; UI subscription owner).
  evidence: ChatBotProjectConversationWorkspace reads the conversation before OnAfterRenderAsync joins the tenant group, and ProjectConversationStreamingSubscriber.EnsureSubscribedCoreAsync returns after the initial JoinTenantAsync without invoking a refresh callback. A projection change between the initial read and group join is not replayed and remains unseen until another signal or user action. The same initial-join behavior exists at the preserved original baseline. Verify with a deterministic change between read and join followed by the typed current-state refresh, preserving tenant/project/scope guards.
  resolution: Resolved 2026-11-24. Every successful join refreshes authoritative state through the mounted workspace's current tenant, project, and scope. Failed or disposed subscriptions do not refresh. The subscriber and workspace rendering test classes passed all 19 cases.

### DW-148: U1-E3 — Reject compound comma-separated names for non-flags client enum contracts (medium; client contract owner).

status: done 2026-11-24
origin: legacy ledger entry from Deferred from: Story 1.5 source-recovery build review (2026-11-01)
source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
severity: medium
archived: 2026-11-24

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: U1-V1 — Assert every prepared MSBuild import variable in the source-wrapper child process test (medium; build tooling verification owner).
  evidence: .github/scripts/tests/test_story_evidence_guards.py:370-392 observes only Configuration, mapping hash, cwd and exit status. The reviewer ran both SourceMappingWrapperTests successfully against a temporary wrapper that removed all five IMPORTS variables before launching the child, proving the current tests miss loss of the prepared source graph. The production wrapper correctly forwards those imports and the current mapped Debug build passes. Extend the existing child-observation assertion; do not weaken producer validation or source/package duplicate guards.
  resolution: Resolved 2026-11-24. A standalone wrapper regression checks the exact five MSBuild import paths in a child process, together with the mapping hash, Debug configuration, inherited environment and child exit code.

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-11-01, round 13)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R13-W1 — Update Story 1.2's completion evidence after the R12-P2 bootstrap-test rename (low; evidence-contract owner).
  evidence: `_bmad-output/implementation-artifacts/evidence/1-2-publish-the-openapi-contract-spine-and-typed-client.json:656` (task-43) still lists `ServerBootstrapApiTests.CommandEndpointShouldValidateContractBeforeAuthenticationAndAuthenticateBeforeAdmission`; `86f658f` renamed it to `CommandEndpointShouldAuthenticateBeforeContractValidationAndValidateBeforeAdmission`. No other reference remains. `TransitionDetector` re-validates mappings only on a transition to done and Story 1.2 is already done, so CI is unaffected; a later re-validation of Story 1.2 would report the missing assertion.
  reason: The correction edits another completed story's evidence contract, outside Story 1.5's boundary.
  closure: Task-43 names the current test whenever Story 1.2 evidence is next regenerated or re-validated, with that assertion passing.

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-11-03, round 17)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R17-W1 — No normal-lane check exercises `RecoveryMailboxOwnerProvisioner` (low).
  evidence: `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs` is referenced only by `LiveContinuityAspireE2eTests.cs:215`, which runs only in the gated live recovery lanes.
  reason: The test only means something once a seeding route exists. The production gateway refuses every grant while A6/A13 are open (R17-D1).
  closure: A contract test proves that the built grants pass `GovernedOperationAggregate.Handle(GrantChatBotAuthority)` validation.
  resolution: Resolved 2026-11-25. The ordinary integration lane builds the exact live probe grants and verifies their bounded scope and acceptance by the ChatBot authority aggregate.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R17-W2 — The drill's own mailbox intakes have no per-intake ChatBot operation authority (pre-existing).
  evidence: `src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs:176` mints `MailboxMessageIntakeId.New()`. The authorizer requires ChatBot `operation` evidence on each `IntakeId` (`ChatBotAuthorityCatalog.cs:26`). The seed covers only the probe intake.
  reason: Pre-existing. These intakes would also hit `canonical-execution-unavailable`, so they are blocked on A6/A13.
  closure: Live drill intakes are authorized through an accepted owner path once canonical execution opens.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R17-W3 — There are three near-copies of the live owner-seeding helpers, and the copies have drifted (low).
  evidence: `RecoveryMailboxOwnerProvisioner.SubmitOwnerAsync` and `WaitForGlobalAdministratorAsync` mirror `TrivialGovernedCommandAspireE2eTests.SubmitOwnerCommandAndWaitAsync` and `WaitForCurrentGlobalAdministratorAsync`. `94a91b5` had to fix the copied `system` tenant, and `PolicyRefusalPreflightTopology.cs:515` still uses it.
  reason: Fixing it means a refactor across three live harnesses, outside this delta.
  closure: One shared internal test helper owns owner-command submission and waiting.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R17-W4 (`maybe-false`, unverified medium) — Both seeded grants' principal may not match the mailbox token subject.
  evidence: `ChatBotRequestAuthorizer.Request` keys the operation and service-grant owner requests by `context.SubjectId` (the token `sub`). The seed uses `RecoveryValidationTopology.MailboxClientId` for both. The realm defines no fixed service-account user for `recovery-validation-mailbox-client`.
  reason: This cannot be settled without a live token.
  closure: Decode a live recovery mailbox token. If `sub` differs from the client id, key both grants by `sub`.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R17-W5 — The live recovery lane cannot pass until canonical execution opens (from R17-D1 option 1).
  evidence: `LiveContinuityAspireE2eTests.AssertMailboxTokenAdmissionAsync` proves authorization only through the post-authorization 503. That needs a current ChatBot grant, and `ChatBotAdmissionEvidenceValidator.ValidateAtomicParticipationAsync` (`:372-378`) refuses every command, grants included. The seed now fails fast and names this (R17-P9). Separately, hosted release run `41540260256` fails earlier, at `BootstrapGlobalAdmin`, with EventStore's route-catalog 503.
  reason: A6/A13 owner-atomic acceptance and the retry-profile approvals are open release gates outside Story 1.5.
  closure: Once canonical execution is accepted, the live sweep seeds both grants, the probe observes its admission proof, and the route-catalog 503 is resolved in the hosted topology.

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-11-03, round 18)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R18-W1 — The gateway admission path keeps the old cancellation filter for the same denial-audit write that the boundary handler now classifies (low, latent).
  evidence: `CommandGateway.SubmitAsync` (`src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs:83`) wraps `AdmitAsync` in `when (exception is not OperationCanceledException)`, and the pipeline's `DenyAsync` audit writes have no inner catch. A store-raised cancellation on a live request therefore escapes as an untyped 500, and an `IOException` raised under a request abort writes a 503 to the aborted response. For the same route and writer, `ChatBotApplicationBoundaryResultHandler` (`8ea98fb`, R16-P1) answers the typed 503 and propagates the abort. No test injects a denial-audit failure on the gateway path.
  reason: Pre-existing and outside the round-18 delta. It is latent because `InMemoryAuditWriter.RecordAuthorizationFailureAsync` cannot fail and ignores cancellation.
  closure: When a durable denial-fact writer lands, `CommandGateway.cs:83` uses `when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)` plus `ThrowIfCancellationRequested()`, and a `CommandGatewayTests` row whose denial-audit double raises its own cancellation on a live token asserts the typed `DependencyDegraded` result.
  resolution: Resolved 2026-11-24. Admission denial-audit faults yield the typed dependency problem while caller abort propagates. Four cases exercise dependency cancellation, I/O errors and caller cancellation with zero dispatch.

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-11-04, round 20)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R20-W1 — Three gateway lapsed-authority denial-audit sites escape as untyped host failures (low, latent).
  evidence: `CommandGateway.SubmitAsync` calls `admission.RejectAuthorityLapseAsync` at `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs:130`, `:162` and `:174` outside any `try`. `ChatBotCommandAdmissionPipeline.DenyAsync` (`:577-595`) awaits the audit write with no catch, and the endpoint (`ChatBotCompatibilityEndpointExtensions.cs:110-120`) has none, so a denial-audit failure there becomes an untyped 500. The late denial audit at `ChatBotCommandAdmissionPipeline.cs:343` is caught at `:409` and answers `dependency_degraded` through `CommandGateway.Denied` (`:349-351`); round 19 recorded it as `idempotency_outcome_unavailable`.
  reason: Pre-existing and outside the round-20 delta. It is latent because `InMemoryAuditWriter.RecordAuthorizationFailureAsync` cannot fail and ignores cancellation.
  closure: Together with R18-W1, when a durable denial-fact writer lands, classify the three call sites like `:83` (typed dependency-unavailable on an outage, propagation on a request abort), and add a `CommandGatewayTests` row per site whose denial-audit double fails.
  resolution: Resolved 2026-11-24. All three pre-effect lapsed-authority sites preserve ownership cleanup and return a bounded dependency outcome on a writer fault. Tests cover each site with an active and an aborted caller.

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-11-21, round 21)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R21-W1 — No lane runs the moved destructive recovery sweep (medium).
  evidence: `bf0f44f` made `LiveContinuityAspireE2eTests` the `internal` class `LiveRecoverySweep` and moved its `[Fact]` to `tests/Hexalith.ChatBot.Recovery.Tests/LiveContinuityAspireE2eTests.cs`. Only `.github/actions/nightly/run-recovery.sh` runs that project, and no `.github/hexalith-ci.json` enables `nightly`. The legacy scheduled selectors (`.github/workflows/ci.yml:537`, `release.yml:188`) name the old IntegrationTests class, and their jobs call scripts that `bf0f44f` deleted.
  reason: `README.md` deliberately keeps the hook disabled until the shared 330-minute job allowance is released. For Story 1.5, the sweep's mailbox admission probe was already unpassable while A6/A13 are open (R17-W5).
  closure: Enable `nightly` in `hexalith-ci.json`, and retire the legacy selectors at the same time.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R21-W2 — The replacement nightly hook never uploads the evidence artifact that the recovery manifests name (medium, latent).
  evidence: `LiveRecoverySweep.EvidenceArtifactLocator()` (`tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveRecoverySweep.cs:740-743`) stamps `artifact:live-recovery-validation-evidence` unless `HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT` is set. `.github/actions/nightly/action.yml` sets no locator, and nothing uploads `TestResults/live-recovery/<runId>` or the retention-failure markers under `$RUNNER_TEMP`. The central `references/Hexalith.Builds/.github/workflows/release.yml:165-166` runs the hook without uploading anything afterwards. The locator's own summary records the same defect as a past fix.
  reason: Outside Story 1.5 (shared-CI migration files), and latent until the hook is enabled.
  closure: When the hook is enabled, upload both directories under the locator's name, or set the variable to the uploaded name, and pin that binding with a guard.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R21-W3 — The recovery in-process deadline is set only in the nightly action's environment (low).
  evidence: The comment in `.github/actions/nightly/run-recovery.sh:18` relies on "the 250-minute in-process deadline", but only `action.yml:17` sets `HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES` to `250`. Without it, `RecoveryWorkflowTimeout` defaults to 300 minutes (`LiveRecoverySweep.cs:796`), which is after the 280-minute SIGINT and equal to the 300-minute TERM bound. The guards that pinned the order (in-process deadline, then SIGINT, then the step limit) were deleted with `LiveRecoveryValidationArchitectureTests` in `bf0f44f`.
  reason: Outside Story 1.5. The action is the script's only caller, and it is not enabled.
  closure: When the hook is enabled, export the default in the script or guard both files.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R21-W4 — The shared CI would classify `Hexalith.ChatBot.Recovery.Tests` and `Hexalith.ChatBot.Release.Tests` as unit projects (low).
  evidence: `classify_projects` (`references/Hexalith.Builds/Github/lib/ci_pipeline.py:315-323`) puts every project in the unit stage unless its name contains `E2E` or ends with an integration suffix. The unit stage runs before `after-unit-tests` exports the Tier-3 flags. Both projects link `tests/required-live.xunit.runner.json` (`failSkips: true`), so the sweep's Tier-3 skip becomes a `FAIL_SKIP` failure (reproduced locally by two review layers). A solution-wide `dotnet test` fails the same way.
  reason: `.github/hexalith-ci.json` does not exist yet.
  closure: When the file is written, list both projects under `tests.exclude`.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R21-D1 — The shared-CI migration retired Story 1.5's completion-evidence producer without retiring its consumers (medium).
  evidence: `bf0f44f` deleted `tools/Hexalith.ChatBot.StoryEvidenceGate`, `story-evidence-policy.json` and the runner scripts, and moved the sweep to `Hexalith.ChatBot.Recovery.Tests.LiveContinuityAspireE2eTests`. Every job in the legacy `.github/workflows/ci.yml` and `release.yml` still calls a deleted script; on `8f846fd` every required job fails at its first build step. `docs/story-evidence-integrity.md:67` and `docs/adrs/live-recovery-validation-drivers.md:144` still bind `recovery-primary` to the old IntegrationTests class. Story 1.5 has no evidence contract yet, and none can be attested or validated.
  reason: The shared-CI migration owns the producer; 1.5 stays blocked by A6/A13 regardless.
  closure: The shared CI supplies a completion-evidence producer, or a correct-course proposal formally retires the TE-2 gate and states how stories reach `done` without it. Retire the legacy workflows and the stale doc bindings in the same change.
  resolution: Resolved 2026-11-21 by `sprint-change-proposal-2026-11-21.md`: TE-2 is retired, and stories complete under the Story Completion policy (`docs/story-evidence-integrity.md`). The doc and ADR bindings are updated. Retiring the legacy workflows moved to the Hexalith.CiCd adoption entry below, because CiCd owns `.github/workflows/**`.

## Deferred from: code review of spec-1-5-establish-stable-operation-identity-and-concurrency-contracts (2026-11-21, round 22)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R22-W1 — BL0016 breaks every Release and source-mapped Debug solution build (high).
  evidence: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor:153` calls `JSRuntime.InvokeVoidAsync("HexalithChatBot.focusElementById", …)` outside a try/catch. `dotnet build src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj --configuration Release -m:1` exits 1 at `437f0fb` with `error BL0016` (also in `TestResults/story-1-5-r21-p1/release-build.log` and `solution-build.log`). The file is unchanged since `e209f2f`. Both the legacy `ci.yml:117` and the shared `references/Hexalith.Builds/Github/lib/ci_pipeline.py:335` build the whole solution, so the UI, UI.Tests, Architecture and every CI test lane are blocked. Round 21 rejected this as R21-V5 on a result that was not a fresh compile.
  reason: Pre-existing and outside Story 1.5; the streaming stop control belongs to stories 1.16 and 10.6b.
  closure: Guard the call as the analyzer requires, then rerun the Release UI build without incremental reuse.
  resolution: Resolved 2026-11-21: the focus-return `InvokeVoidAsync` in `ChatBotStreamingStopControl.razor` is wrapped in try/catch (`JSDisconnectedException`, `JSException`, `TaskCanceledException`). A `--no-incremental` Release UI build fails with BL0016 at `:153` on the old file and builds 0/0 on the patched one. The Release package-mode and source-mapped Debug solution builds both finish with 0 errors.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R22-W2 — No automated lane runs the Server.Tests rows, including R21-P1's abort-guard theory (medium).
  evidence: The legacy build job fails on BL0016 (R22-W1) and then calls the deleted `.github/scripts/run-merge-test-lanes.sh` (`ci.yml:119`). `release.yml` has no Server.Tests lane, and no `.github/hexalith-ci.json` adopts the shared pipeline. `ChatBotApplicationBoundaryResultHandlerTests.cs:134` and every other Server.Tests row are protected only by local runs.
  reason: Amends R21-D1; the lane outage belongs to the shared-CI migration and to R22-W1.
  closure: A solution-building CI lane runs `tests/Hexalith.ChatBot.Server.Tests` on push.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R22-W3 — Three hub cases fail in the Release Server lane and reopen R15-P2's hub-authentication closure (medium).
  evidence: `tests/Hexalith.ChatBot.Server.Tests/bin/Release/net11.0/Hexalith.ChatBot.Server.Tests` at `437f0fb`: 3,629 passed and 3 failed (`TestResults/story-1-5-r21-p1/server-release.trx`). `ChatBotProjectConversationHubE2ETests` same-tenant join (`:133`), cross-tenant join (`:84`) and AI-progress subscriber (`:40`) each fail with `HubException: Failed to invoke 'JoinTenant' because user is unauthorized`. R15-P2 closed the hub-authentication blocker at 3643/3643 in Debug source mode, and the hub and its tests are unchanged since `e48e87e`.
  reason: Pre-existing, because the round-22 diff does not touch the hub, and the hub belongs to story 10.6b. The cause (package-mode or dependency drift) was not reproduced.
  closure: Find the drift, restore the three cases, and record a green Release Server lane.
  resolution: Resolved 2026-11-21. The cause was ASP.NET Core 11, not package drift. `DefaultHubDispatcher` now passes all of a hub method's attributes to `AuthorizationPolicy.CombineAsync(provider, metadata)`, which returns the host fallback policy when none of them is authorization data. The async `JoinTenant` carries `AsyncStateMachineAttribute`, so it received the EventStore SDK's deny-unclassified fallback (hub-class attributes guard only the connection). `JoinTenant` and `LeaveTenant` now name `ChatBotApplicationBoundaryPolicy`. `ChatBotProjectConversationHubE2ETests` went from 7/10 to 10/10, and the Release Server lane passes 3632/3632 with zero skips.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R22-W4 — The source-mapped Debug build cannot compile Server.Tests or RecoverySandbox (medium).
  evidence: `python3 .github/scripts/with-source-mapping.py dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1` at `437f0fb` (`TestResults/story-1-5-r21-p1/solution-build.log`) fails `Hexalith.ChatBot.Server.Tests` with CS0234/CS0246 at `Projections/ChatBotProjectConversationHubE2ETests.cs:14`, `:17` and `:219`, because the SignalR client types are missing in source mode, and with CS8765 at `Adapters/Projects/ProjectsMemoriesCaseResolverTests.cs:114` and `ProjectsProjectDirectoryTests.cs:244`. `Hexalith.ChatBot.RecoverySandbox` fails the static-web-assets check ("scoped css bundles do not have matching endpoints").
  reason: Pre-existing and outside the round-22 diff, which R21-P1 verified through an out-of-tree harness.
  closure: Reference the SignalR client in source mode, align the Projects test overrides with the regenerated client, fix the sandbox endpoint mapping, and rerun the Debug Server lane from the solution build.
  resolution: Resolved 2026-11-21. Server.Tests now references `Microsoft.AspNetCore.SignalR.Client` directly; package mode had supplied it only through the Projects 1.2.4 and FrontComposer packages. The two Projects test fakes override with `string?` to match the source-mode `ClientCompatibility` shim. RecoverySandbox sets `EnableHotReloadInRuntimeConfigDevFile=false`, because the SDK 11.0.100 Debug hot-reload browser-tools assets of the referenced ChatBot.Server collide with its own at `_framework/`. The source-mapped Debug solution build finishes with 0 errors. The Debug Server lane passed 3652/3653; its one failure is the `ChatBotMetricsTests` flake recorded below.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: R22-W5 — An open ledger entry still closes against a deleted guard (low).
  evidence: The spec-1-6 entry "Reconcile the release job timeout with its existing Architecture guard" (this file, around line 1725) cites `LiveRecoveryValidationArchitectureTests.cs:633`. `bf0f44f` cut that file to 266 lines and deleted the guard.
  reason: Amends R21-D1's closure; the entry belongs to spec-1-6.
  closure: When R21-D1 closes, retire or re-scope the ledger entries bound to the deleted guards and the legacy workflows.

## Deferred from: story 1.5 R22 build-blocker follow-up (2026-11-21)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: `ChatBotMetricsTests.AllOperationalInstrumentsPlusGapCounterAreRegisteredOnTheChatBotMeter` is flaky when run with other test classes (low).
  evidence: The full Debug Server lane failed it once. It captured 20 instrument names, two complete sets, because another test class's host kept a second `Hexalith.ChatBot` meter alive while it ran. The class passes 3/3 in isolation and passed in both Release lanes. `MetricCapture` filters instruments by meter name only (`ChatBotMetricsTests.cs:396`).
  reason: Pre-existing and outside the build-blocker fixes; the metrics seam belongs to story 8.2.
  closure: Capture by the test's own meter instance instead of the shared meter name, or run every class that creates the ChatBot meter in one non-parallel collection.
  resolution: Resolved before 2026-11-24 by ca2e7e7. `MetricCapture` and the throwing listener select instruments by reference to the ChatBot meter. Server.Tests passed in full on 2026-11-24.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
  summary: Shared-catalog drift fails the UI.Tests and Architecture pin guards (medium).
  evidence: `4aa212b` made the root wrapper import `$(HexalithBuildsRoot)/Props/Directory.Packages.props`, and `tests/PackageCatalogTestHelper.cs` read the unexpanded text, so 12 UI.Tests/Architecture rows failed before any version was compared. BL0016 (R22-W1) had kept both lanes from compiling. The helper now substitutes the MSBuild-evaluated `HexalithBuildsRoot`, and the defining-project check still requires every `PackageVersion` to come from `references/Hexalith.Builds`. With that fix, real drift remains: `AssertUiFoundationPins` expects Fluent UI Components/Icons 5.0.1 and Playwright 1.64.1, while the catalog has 5.1.0 and 1.65.0 (UI.Tests 609/613). `references/Hexalith.Folders/Directory.Packages.props` pins `CommunityToolkit.Aspire.Hosting.Dapr` 17.0.0-beta.1088, while the catalog has 17.0.0-preview.1.261120-0454 (Architecture 118/119).
  reason: Updating the UI floor pins is an approval decision, and the Folders wrapper belongs to the Folders module.
  closure: Approve and update the UI foundation pins, or hold the catalog at the approved versions; Folders realigns its wrapper with the shared catalog.
  resolution: Partly resolved 2026-11-21: by the user's decision, `AssertUiFoundationPins` now expects Fluent UI Components/Icons 5.1.0 and Playwright 1.65.0 (UI.Tests 613/613). The Folders wrapper drift stays open (Architecture 118/119).

## Deferred from: correct course sprint-change-proposal-2026-11-21 (2026-11-21)

- source_spec: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-21.md`
  summary: Adopt Hexalith.CiCd so that a Release solution build and Server.Tests run on every push and the legacy workflows retire (medium).
  evidence: ci run `44897519125` on `bd61641` builds the solution (0 errors, 7 CS0618 warnings), then fails at the deleted `.github/scripts/run-merge-test-lanes.sh` (`ci.yml:119`). `story-evidence-integrity` and every release job (run `44897518989`) fail at the deleted `build-release-configuration.sh`. No `.github/hexalith-ci.json` exists, and CiCd owns `.github/workflows/**`.
  reason: Hexalith.CiCd owns the registration and the generated callers. ChatBot drafts the request (proposal §4.9) and lands the product preparation first.
  closure: CiCd delivers `setup/Hexalith.ChatBot.yml` and generated callers that replace `ci.yml` and `release.yml`, and the first push run passes Server.Tests (closes R22-W2). Before the request, ChatBot realigns the Folders wrapper, fixes the `ChatBotMetricsTests` flake, closes R21-W2 and R21-W3, proves the Release build under the shared `-warnaserror` flags (CS0618 in `references/Hexalith.Platform` and in `RecoverySandboxTopologyComposer.cs:55`), and passes `python3 references/Hexalith.Builds/check.py` in a standalone clone. At delivery, also update the scheduler bullet in the Consequences section of `docs/adrs/live-recovery-validation-drivers.md`.

## Deferred from: review of spec-five-deferred-recovery-boundaries (2026-11-21)

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-recovery-boundaries.md`
  summary: Legacy CI and release workflows still consume deleted recovery scripts, test identities, and the StoryEvidenceGate producer.
  evidence: Current `ci.yml` and `release.yml` still call missing `.github/scripts/run-xunit-v4.sh`, `build-release-configuration.sh`, and `project-recovery-results.sh`; they still `-method` the old IntegrationTests live Fact and `RetainedLiveRecoveryEvidenceShouldPassTheReleaseGateOutOfProcess`; they still build `tools/Hexalith.ChatBot.StoryEvidenceGate`. The live Fact now lives only in ungated `Hexalith.ChatBot.Recovery.Tests`. The ADR still documents the retired TE-2 / Dapr 1.18.0 sanitizer path beside the nightly hook. This is later shared-CI migration, not the five-boundary hardening.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-recovery-boundaries.md`
  summary: The after-unit-tests runtime hook only resolves Playwright's linux-x64 node and requires GITHUB_ENV.
  evidence: `.github/actions/after-unit-tests/require-runtime.py` hard-codes `tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net11.0/.playwright/node/linux-x64/node` and opens `os.environ["GITHUB_ENV"]`. A non-linux-x64 runner or a local invocation without that variable fails before fail-closed live tests can be enabled.
  resolution: Resolved 2026-11-24. The runtime hook resolves the packaged Node executable for the host and emits shell-quoted local settings when GITHUB_ENV is unset. Tests cover the supported paths and both output modes. This also resolves the duplicate nightly-runtime-hook entry.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-recovery-boundaries.md`
  summary: Recovery mailbox token identity parsing has no bounded JWT error path.
  evidence: `RecoveryMailboxOwnerProvisioner.AssertMailboxTokenIdentity` parses the bearer payload with `Convert.FromBase64String` and `JsonDocument.Parse` and reads required claims with `GetProperty` and no catch. A non-Base64 or non-JSON payload fails as `FormatException`, `JsonException`, or `KeyNotFoundException` instead of the mailbox-token error used for a non-JWT shape. This is later live-harness code, not the five-boundary production change.
  resolution: Resolved 2026-11-24. Malformed encodings, invalid payloads, missing or mistyped claims, and mismatched identities produce fixed diagnostics without bearer contents. The accepted identity and all 67 live continuity contract cases passed.

## Retired: spec-chatbot-ci-cd-alignment (2026-11-21)

- source_spec: `_bmad-output/implementation-artifacts/spec-chatbot-ci-cd-alignment.md` (deleted; git history keeps the text)
  summary: The 2026-09-18 draft CI/CD alignment spec is retired as superseded by `sprint-change-proposal-2026-11-21.md` option C1 and Hexalith.CiCd story 19 (`spec-19-migrate-chatbot.md` in the CiCd repository).
  evidence: Its frozen intent edits CiCd-owned `.github/workflows/**` and `.github/dependabot.yml`, adds workflow-YAML architecture tests, keeps story-evidence gates in module CI, and adds a module-owned manual release, all of which the current CI/CD ownership policy forbids.

## Deferred from: review of spec-1-1c-aspire-dapr-topology-review-remediation (2026-11-21)

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: The required topology-acceptance job still filters a Fact that now lives only on Live.IntegrationTests, so the live Running/403 assertions never execute.
  evidence: `ci.yml:433` and `release.yml:72` pass `-method Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2ETests.GovernedCommandsShouldFailClosedWithoutAtomicParticipationInRealDaprTopology` to the IntegrationTests runner. That Fact is only on `tests/Hexalith.ChatBot.Live.IntegrationTests/RequiredTopologyTests.cs:10`. `run-xunit-v4.sh` fails the lane on zero tests, but the live contract is still not run. Caused by the later Live.IntegrationTests split; Hexalith.CiCd owns the workflow files.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: Architecture tests no longer pin topology-acceptance `-method` strings, TRX paths, or guard-step names.
  evidence: `ScaffoldArchitectureTests.TopologySmokeShouldPreserveRefusalOrderAndCommittedStateProof` only reads helper method bodies in `TrivialGovernedCommandAspireE2eTests.cs`. Hexalith.CiCd forbids workflow-YAML architecture tests; a stale CI filter cannot be caught here.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: Twelve required topology resources share a 10-minute readiness budget originally sized for seven waits.
  evidence: `RequiredTopologyResources` in `TrivialGovernedCommandAspireE2eTests.cs:82-96` now includes parties, projects, Memories, `memories-vectors`, and `memories-graphs`. `TopologyReadinessBudget` remains 10 minutes. Later topology expansion, not this story.
  resolution: Resolved 2026-11-25. All required resource waits now start concurrently and share the original monotonic readiness budget. Terminal failures cancel and join sibling observations while healthy resources retain their evidence. The all-resource barrier regression and existing cancellation and multi-failure tests pass.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: Selecting any external owner key can leave local Memories without JWT wiring.
  evidence: `Program.cs:163-181` treats any Projects/Memories owner key as `externalOwners`, then optionally keeps the local Memories endpoint when `ChatBot:Memories:Endpoint` is unset, while `:277-285` skips Memories JWT in that mode. Later owner-composition, not this story.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: `Dapr:SpineAppIdSuffix` does not suffix parties or projects app ids.
  evidence: `ChatBotAspireModule.ResolveSpineAppId` (`:127-134`) only suffixes `AppId` and `EventStoreServiceName`. Isolated topologies can collide on unsuffixed parties/projects sidecars. Later suffix work, not this story.
  triage: 2026-11-24 Skipped. Parties and Projects call EventStore through a literal `eventstore` app id, so suffix isolation first needs configurable EventStore app ids in those repositories.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: Unique Dapr internal gRPC port validation cannot see Memories' default port 50002.
  evidence: `SidecarAppIds` (`ChatBotAspireModule.cs:77-86`) omits Memories; that sidecar is attached in `Program.cs`. A configured `Dapr:InternalGrpcPorts` value of 50002 can collide. Later Memories composition, not this story.
  resolution: Resolved 2026-11-24. `ChatBotAspireModule.ValidateUniqueInternalGrpcPorts` now also takes reserved ports, and `GetFixedDaprPorts` lists the fixed daprd HTTP and gRPC ports of a sidecar composed outside the module. Right after composing Memories, `Program.cs` reserves the ports read from the Memories sidecar options (3502 and 50002 by default), so a `Dapr:InternalGrpcPorts` entry that reuses one fails closed at composition and names both owners instead of losing a bind race when the sidecars start. `RecoveryValidationTopologyContractTests.AppHostRejectsADaprInternalGrpcPortBoundByTheMemoriesSidecarAsync` composes the AppHost with chatbot on 50002 and eventstore-admin on 3502; both cases failed (no exception) without the `Program.cs` reservation. `ValidateUniqueInternalGrpcPortsRejectsAPortReservedByAnExternallyComposedSidecar` covers the guard and the port listing directly. The class passed 42/42 and AppHost.Tests 18/18.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: Isolated Dapr HTTP port reservations still cover only four sidecars while parties and projects also run sidecars.
  evidence: `IsolatedDaprHttpResourceNames` (`TrivialGovernedCommandAspireE2eTests.cs:74-80`) is eventstore, tenants, chatbot, eventstore-admin. `RequiredDaprSidecarResources` also lists parties, projects, Memories, and eventstore-admin-ui. Later sidecar expansion, not this story.
  triage: 2026-11-24 Skipped. Parties, Projects and Memories already take dynamic app ports. The remaining fixed Memories daprd ports are guarded by the 2026-11-24 internal gRPC port resolution.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: A Dapr workflow outage clears `NextRetryAt` on an operation-status read.
  evidence: `DaprCorrectionPropagationWorkflowRuntime.ReadProgressAsync` returns null on non-cancel faults (`:29-32`). `OperationStatusQueryHandler.cs:53-58` then writes `record with { NextRetryAt = null }`. Later correction-propagation, not this story.
  triage: 2026-11-24 Skipped. Hiding an unverified retry time is the current fail-closed behavior, pinned by `CorrectionPropagationProgressReadTests`. Changing it is a product decision.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: CI and release topology jobs invoke `.github/scripts/install-dapr-cli.sh`, which is not in the tree.
  evidence: `ci.yml:423` and `release.yml:62` run that script; `.github/scripts/` has other untracked helpers but not `install-dapr-cli.sh`. Later CiCd migration, not this story.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-1-1c-aspire-dapr-topology-review-remediation.md`
  summary: CI and release workflows still build and run deleted `Hexalith.ChatBot.StoryEvidenceGate` projects.
  evidence: `ci.yml` (e.g. `:73-78`, `:200-207`) and `release.yml:168` name `tools/Hexalith.ChatBot.StoryEvidenceGate` and `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests`; those projects are gone. Later deletion; Hexalith.CiCd owns the workflows.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/1-1e-centralize-nuget-package-reference-version-authority.md`
  summary: Consumer-local package versions are back in six root-declared submodules, so Hexalith.Builds is no longer the only package-version authority.
  evidence: At ChatBot `843f886a4064493dbdb116e16ae8959dd8442999` the root wrapper is still version-free, but these `Directory.Packages.props` wrappers declare versions locally again: Folders `73ca79b4` (4 `PackageVersion Update` rows, plus `HexalithAspireHostingDaprVersion` 17.0.0-beta.1088), Memories `e413124b` (18 rows, including a `Hexalith.EventStore.*` floor override via `MemoriesEventStoreMinimumVersion` and two `Hexalith.Memories.*` rows the catalog lacks; Dapr hosting 17.0.0-beta.1078), Parties `d8d599e6` (16 rows, plus local Commons/Tenants/FrontComposer/Memories version defaults), Timesheets `271442d0` (16 rows; Dapr hosting 17.0.0-beta.1052), Works `93322fa5` (3 conditional `Microsoft.Build*` rows) and McpCli `e79b7f8b` (14 rows; Dapr hosting 17.0.0-beta.1078). Most rows restate the catalog value (for example `Aspire.Hosting` 17.0.0), but `CommunityToolkit.Aspire.Hosting.Dapr` differs from the catalog's 17.0.0-preview.1.261120-0454 in four repositories. Recent history points to Dependabot bumps and local version-floor fixes (Memories `9677a0c5`, Folders `c00df5d8`, 2026-11-19/20). No CI runs workspace-mode authority validation (DW-80), and Hexalith.CiCd owns Dependabot. Fixes belong in each owning repository. Story 1.1e was closed as superseded on 2026-11-21 instead of being resumed.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: GitHub workflows still call deleted StoryEvidenceGate projects, release scripts, and python suites.
  evidence: `ci.yml` and `release.yml` invoke missing `install-dapr-cli.sh`, `verify-pull-request-merge.sh`, `guard-main-publication.sh`, `verify-nuget-publication.sh`, `project-recovery-results.sh`, StoryEvidenceGate binaries, and absent `test_*.py` files; `.releaserc.json` is gone. Later CiCd migration, not this package-mode story.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: The nightly runtime hook hard-codes linux-x64 Playwright node and requires `GITHUB_ENV`.
  evidence: `.github/actions/after-unit-tests/require-runtime.py:24` and `:40`. Nightly/CiCd hook, not this story.
  resolution: Resolved 2026-11-24 by the shared after-unit-tests runtime hook fix. Both entries refer to the same defect.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: `with-source-mapping.py` raises if `hexalith-module --version` fails after restore.
  evidence: `.github/scripts/with-source-mapping.py:31-32` uses `check=True` instead of returning the tool exit code. Composition wrapper, not this story.
  resolution: Resolved 2026-11-24. The wrapper forwards the version command's stdout, stderr, and exit status before building. A controlled tool shim restored successfully, exited 37 from the version command, and confirmed that preparation and the build were never invoked.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: An empty association-id load dispatches failure without the generation guard used for other load failures.
  evidence: `AssociationReviewEffects.HandleLoadAsync` returns after `AssociationReviewFailedAction("association-id-required")` without `DispatchLoadFailure`. UI review effects, not this story.
  resolution: Resolved 2026-11-24. Blank loads share the effect's generation guard and carry the originating load into the reducer so a queued failure cannot replace a newer route. The current blank route still reports the required ID. The association-review effects and state test classes passed all 79 cases.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: The bound Memories probe helper throws if JSON omits `command` instead of returning 403.
  evidence: `MemoriesProbeCommandGateway.cs:358` calls `GetProperty("command")` before bound-proof 403 handling. Test double, later Memories probe work.
  resolution: Resolved 2026-11-24. Malformed or incomplete command envelopes return 403 before the fixture forwards a request. Loopback HTTP regressions cover missing fields and invalid JSON shapes.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: ChatBot silences Aspire 17 `AddProject` deprecations for every project.
  evidence: `Directory.Build.props:29` adds `CS0618` and `ASPIRE012` to `WarningsNotAsErrors`. .NET 11/Aspire 17 follow-on, not package-mode pairing.
  resolution: Resolved 2026-11-25. Removed the repo-wide CS0618 and ASPIRE012 exceptions. The retained recovery topology call has a local diagnostic suppression for its legacy resource type.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: Release dispatch can bypass stop-ship gates and still mint a nuget.org credential.
  evidence: `release.yml` `bypass_stop_ship_gates` plus OIDC before `guard-main-publication.sh`. Parent/CiCd release ownership.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: Live catalog Redis containers can leak when the recovery timeout SIGKILLs the process.
  evidence: `LiveCatalogFixture` starts `redis:7.4.2` and cleans up only in `DisposeAsync`. Live topology, not this story.
  resolution: Resolved 2026-11-25. The nightly hook gives its producer a private temp scope and reaps only matching fixture-Redis containers after it exits. The hook preserves producer status and records cleanup status. In a real Docker probe after SIGKILL, Redis was removed and both a same-scope non-Redis container and another run’s Redis remained running.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: The recovery-validator sandbox user still carries a static password and bootstrap admin grants.
  evidence: `src/Hexalith.ChatBot.AppHost/KeycloakRealms/hexalith-realm.json` user `recovery-validator` includes `global-administrators:bootstrap`. Realm/recovery work, not this story.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: Ordinary tests never pin the mapped M2 release-gate secret (401 without/wrong header, 200/503 with it).
  evidence: `ChatBotM2ReleaseGateAuthenticationHandler` compares the secret, but `M2ReleaseGateEndpointShouldNotBeMappedWithoutAToken` only covers the unmapped-without-token case. Later release-gate work.
  resolution: Resolved 2026-11-24. The HTTP bootstrap tests prove absent and incorrect secrets return 401 without resolving the gate dependencies. The configured secret reaches the stop-ship logic and returns 503 with its typed verdict. The unmapped case remains covered. ServerBootstrapApiTests passed all 107 cases.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: Sibling ChatBot health routes require the boundary policy, but ordinary tests pin anonymous 401 only for `/health/chatbot`, and the live workflow probe still GETs `/health/chatbot/workflows` with no credentials.
  evidence: `ChatBotCompatibilityEndpointExtensions.cs:43-64` vs `ServerBootstrapApiTests` `/health/chatbot` only; `TrivialGovernedCommandAspireE2eTests.cs:1104-1107` expects 200. Later health-boundary change.
  resolution: Resolved 2026-11-24. An anonymous-denial theory in ServerBootstrapApiTests covers `/health/chatbot`, `/health/chatbot/workflows` and `/health/chatbot/periodic-enforcement`. Each route must be mapped once for GET with the ChatBot boundary policy and answer an anonymous caller with 401 and `authentication_denied`, so the SDK fallback denial cannot mask a missing route. The authenticated health check is kept. The live workflow probe already sends a tenant-bound bearer since 065ded0. A moved workflows route failed only its row, and an unpoliced route stopped the host at the EventStore endpoint inventory. ServerBootstrapApiTests passed all 110 cases.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: Nightly recovery/release hooks no longer fail closed when they discover zero tests.
  evidence: `live-recovery.runsettings` (`TreatNoTestsAsError`) was deleted; `.github/actions/nightly/run-recovery.sh` and `run-release.sh` call `dotnet test` without that switch. Nightly CiCd safety net, not this story.
  resolution: Resolved 2026-11-24. The nightly release hook requires at least one test through Microsoft.Testing.Platform. A controlled run made the application succeed with zero tests and confirmed that the hook exited 9. The existing CTRF recovery guard independently rejected a zero-test run.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: Unverified: empty SDK terminal-refusal ids may throw from `CommandEnvelope` instead of a typed refusal.
  evidence: Would be settled by a `DomainServiceTerminalRefusalContext` with blank `MessageId`, `UserId`, or `CorrelationId` reaching `RefuseUnsupportedAsync`.
  triage: 2026-11-24 Skipped. ChatBot cannot receive blank ids here: the SDK's `CommandEnvelope` constructor rejects them before admission. Any typed refusal belongs to Hexalith.EventStore.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: Unverified: live catalog setup may throw if a Dapr sidecar YAML has no `spec` object.
  evidence: Would be settled by a sidecar `Config` whose deserialized JSON lacks `spec`, hitting `LiveCatalogFixture.cs:120`.
  resolution: Resolved 2026-11-24. Live catalog setup initializes an omitted spec, applies its isolated mTLS and discovery configuration, and refuses invalid spec shapes with a stable diagnostic. Regression tests cover the configuration variants.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-synchronized-package-release-ci.md`
  summary: Unverified: blank tenant or intake ids may inventory colliding read-model keys.
  evidence: Would be settled by a caller of `GetIntakeReadModelKeysAsync` with whitespace `tenantId` or `intakeId`.
  resolution: Verified 2026-11-24 as not reachable. `GetIntakeReadModelKeysAsync` builds keys through `ProjectConversationSourceEmailView.KeyFor`, which has rejected blank tenant and intake ids before any store read since f1262bd.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-recovery-cleanup-state-isolation.md`
  summary: Legacy `ci.yml` still selects the deleted IntegrationTests live-recovery Fact after the sweep moved to Recovery.Tests.
  evidence: `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs` is gone; the Fact is `tests/Hexalith.ChatBot.Recovery.Tests/LiveContinuityAspireE2eTests.cs`. `.github/workflows/ci.yml:280` still passes `-method Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests.LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate`, and `.github/scripts/run-xunit-v4.sh` fails closed when `passed + failed == 0`. Caused by later shared-CI migration (`bf0f44f`), not this spec. Hexalith.CiCd owns the workflow.


- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-independent-worm-rebuild.md`
  summary: Architecture tests no longer prevent Graph/mailbox re-ingestion, Party/Folder current-state reads, or baseline-partition writes the live-recovery ADR still says they prevent.
  evidence: `docs/adrs/live-recovery-validation-drivers.md` still claims those scans; `LiveRecoveryValidationArchitectureTests.ProjectionRebuildTopology_ShouldKeepIndependentSeedAndProductionReplayPaths` only string-matches seed/rebuild datasets and the production replay types. Later architecture-test rewrite, not this story.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-independent-worm-rebuild.md`
  summary: Workflow attempt-envelope, deadline, runsettings, and live-method architecture pins were removed after this story.
  evidence: Those facts are absent from `tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs` on HEAD; the deletion is in the later architecture-test rewrite, not the independent WORM rebuild.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-independent-worm-rebuild.md`
  summary: Legacy CI still selects the deleted IntegrationTests live-recovery Fact after the sweep moved to Recovery.Tests.
  evidence: Hosted producers in `.github/workflows/ci.yml` and `release.yml` still name `Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests`; the Fact is `tests/Hexalith.ChatBot.Recovery.Tests/LiveContinuityAspireE2eTests.cs`. Later `bf0f44f`; Hexalith.CiCd owns workflows.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-independent-worm-rebuild.md`
  summary: The PRD addendum NFR57 row no longer states the independently proven sandbox rebuild path this story recorded.
  evidence: Spec-commit addendum retired `RV-REBUILD-WORM` for the sandbox path; HEAD `addendum.md:404` only says there is no current production-shaped full-window proof. Later addendum rewrite (approval 2026-09-14), not this story.
  resolution: Resolved 2026-11-24. The NFR57 row records the independent sandbox rebuild and links the ADR that retires its rebuild residual. The row retains the full-window, freshness and production qualification limits.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md`
  summary: Production ingestion-binding cannot fetch exact mailbox message or attachment bytes because no owned repository exposes that content API.
  evidence: ChatBot registers only `UnavailableMailboxMessageContentSource` and `UnavailableMailboxAttachmentContentSource`; Workers `GraphMailboxMessage` is metadata-only and has no Graph SDK download. Spec Implementation Evidence already records the required owner API.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md`
  summary: Unverified: local Memories conversation.echo may prevent real embedding/NL-semantic regeneration even when Redis Stack and FalkorDB are up.
  evidence: Would be settled by tracing `GenerateChunkEmbeddingsActivity` / NL description against `src/Hexalith.ChatBot.AppHost/DaprComponents/llm.memories.yaml`.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md`
  summary: ChatBot mailbox ingest uses Memories `SourceType.File`, so correction skips Event-only natural-language-semantic rebuild.
  evidence: `MemoriesClient.IngestCoreAsync` hard-codes `SourceType.File`; `ApplyDerivedStoreCorrectionActivity` runs NL-semantic only when `artifact.SourceType == SourceType.Event`. Native Memories File ingest has the same gate.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md`
  summary: ChatBot package-mode Release still resolves published Memories 2.21.3, which has no `V1.DerivedStores` contracts, and the Memories gitlink was not moved.
  evidence: Implementation Evidence records CS0234/CS0246 in package mode and that publishing `Hexalith.Memories.Contracts` / `Client.Rest` plus updating the Builds catalog is a package-owner action.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-builds-catalog-consumer-authority-completeness.md`
  summary: Hexalith.Builds `docs/dotnet-11-migration.md:34` still says CommunityToolkit.Aspire.Hosting.Dapr "is 17.0.0-beta.1052", in present tense, but the catalog selects 17.0.0-preview.1.261120-0454.
  evidence: The sentence sits under "Retained compatibility" and was already stale at Builds 27fe4d62, after the catalog moved through beta.1078 to the preview build. The Builds catalog consumer-authority change edited only the next two lines of that bullet. Fix it by putting the sentence in past tense or by naming the current selection.

- source_spec: `_bmad-output/implementation-artifacts/spec-deferred-work-priority-batch.md`
  summary: Verify that the nightly release hook executes both required release checks.
  evidence: The minimum discovery count rejects an empty run, but one surviving test can satisfy it if discovery loses the M2 or origin-parity check. The two required identities are declared in `tests/Hexalith.ChatBot.Release.Tests/GovernedStateReleaseGateTests.cs`.
  resolution: Resolved 2026-11-24. The nightly hook requires both named release checks to pass exactly once in a fresh TRX. Controlled runs covered success, missing, skipped, renamed and duplicate results, and runner failure.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-deferred-work-priority-batch-2.md`
  summary: Six projection families (mailbox-intake, association, participant-resolution, ai-outcome, task-intent and approval) read flattened domain fields, but EventStore publishes each event inside the envelope's `payload` bytes, so live deliveries bind with null domain data and are ignored.
  evidence: High, pre-existing, exposed by the L1208 fan-out. `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPublisher.cs:199-229` publishes `EventEnvelope` (metadata plus `Payload`). Only the governed-operation and governed-control translators decode `payload`. The other `Published*Event` records expect top-level `record`, `request`, `intakeId` and similar, and their fan-out tests post pre-flattened records. Failure-state projections also have no route or registration. Settle by decoding each family's payload by `eventTypeName` and adding EventStore-envelope rows to `ChatBotProjectionFanOutEndpointsTests`.
  resolution: Resolved 2026-11-25. Fan-out adapts concrete EventStore payload bytes for all six families using trusted envelope metadata and shared SDK payload mapping. The fan-out and review-queue projection suites passed 34 cases, covering both envelope shapes, compatibility, replay, foreign events, authorization and write-fault redelivery. The review regression covers concrete task disposition, envelope correlation, retry after an association write fault and SDK ownership of workflow side effects.

- source_spec: none
  summary: Association correction propagation events omit evidence required by the projection translator.
  evidence: The producer records `MailboxAssociationCorrectionPropagationStarted.cs`, `MailboxAssociationCorrectionStoreInvalidated.cs`, `MailboxAssociationCorrectionPropagationCompleted.cs`, and `MailboxAssociationCorrectionPropagationDelayed.cs` omit `ThresholdPolicyVersion` and `DerivationKernelVersion`. `AssociationProjectionTranslator.cs` requires both before translating the event. Resolve with an ordered merge against the existing association's evidence and tests for out-of-order replay.
  resolution: Resolved 2026-11-25. Concrete propagation events inherit the matching correction evidence. The tests cover all four payloads, reordered subscription delivery, separate correction generations and historical SDK queue replay.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-deferred-work-priority-batch-2.md`
  summary: A decision or correction failure that completes after the reviewer moved to another association may still be applied, announced and focused on the new association's page.
  evidence: Maybe-false, medium if true. `AssociationReviewReducers.ReduceSubmitFailed` and `ReduceCorrectionFailed` do not compare the originating association, and the 2026-11-24 activation counter now makes such a failure announce. Settle with a reducer test that dispatches a stale failure after navigation.
  resolution: Resolved 2026-11-25. Decision and correction results carry the exact originating load. The active review discards stale success, failure and validation results after navigation, including a return to the original ID. The effects and reducer suites passed all 97 cases. The 16-case review run also checks that both current validation rejections still apply inline.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-deferred-work-priority-batch-2.md`
  summary: Returning to the association review page with an already-announced failure moves focus to the error summary again, although the banner stays silent.
  evidence: Low, pre-existing. `_announcedFailure`, formerly `_announcedErrorCode`, belongs to the component instance and starts empty on every remount. Fix by seeding it from the current state on first render or keeping it in circuit-scoped state.
  resolution: Resolved 2026-11-25. The circuit remembers which failure activation took focus. Unmounting and remounting in that scope retains focus for the old failure, and the next activation receives focus once. The interactive and static rendering suites passed all 111 cases.

- source_spec: `/home/administrator/projects/hexalith/chatbot/_bmad-output/implementation-artifacts/spec-deferred-work-priority-batch-2.md`
  summary: A validation code shown after an association-review failure can send focus to the removed failure-summary element.
  evidence: Unverified medium, pre-existing. The focus guard reads `ValidationErrorCode ?? ErrorCode` and focuses `_errorSummary`, which only renders while `ErrorCode` is set. The validation banner in `ChatBotAssociationReviewActions.razor` has no focus target of its own. Settle with a browser test that fails, then validates on the same page.
  resolution: Resolved 2026-11-25. The page-level focus target is limited to the mounted failure summary. The interactive renderer verified that a subsequent validation rejection renders inline without moving focus. Both focused interaction tests passed.
