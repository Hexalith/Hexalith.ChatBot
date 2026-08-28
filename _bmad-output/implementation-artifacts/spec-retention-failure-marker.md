---
title: 'Emit retention-failure marker and classify total evidence-sink loss'
type: 'bugfix'
created: '2026-08-27'
status: 'done'
review_loop_iteration: 0
followup_review_recommended: true
baseline_revision: '9618701c221bc6d47bfc2811ab17fc82590b6ffe'
baseline_commit: '9618701c221bc6d47bfc2811ab17fc82590b6ffe'
context:
  - '{project-root}/docs/adrs/live-recovery-validation-drivers.md'
warnings: []
deferred:
  - summary: >-
      A cancelled workflow token makes the coordinators' filtered catch rethrow the canonical evidence-write
      failure, so a deadline-killed live recovery run produces neither an unmeasurable report nor a
      retention-failure marker.
    evidence: |-
      All three coordinators guard the retention fallback with
      `catch (Exception) when (cancellationToken is { IsCancellationRequested: false })`. The filter predates this
      story and is deliberately documented, and the marker is specified to be emitted only after both `RecordAsync`
      attempts fail -- so the current behaviour is spec-conformant. It nonetheless leaves the workflow-timeout path
      (the 265m SIGINT ladder in ci.yml / release.yml, i.e. the failure most likely to lose evidence) with no
      artifact-level reason at all: the gate reports plain `<job>:missing_evidence`, which is the exact
      reconstruction gap this story closed for the non-cancelled path.
    location: >-
      src/Hexalith.ChatBot.Server/Audit/ContinuityDrillCoordinator.cs:231
    severity: medium
---

<intent-contract>

## Intent

**Problem:** When both canonical and fallback recovery-evidence writes fail, all three coordinators return an `unmeasurable` report carrying `EvidenceRetentionFailedDeviation`, but the retained artifact contains no reason. The external gate consequently reports ordinary `missing_evidence`, so total sink loss cannot be reconstructed from artifacts alone.

**Approach:** After both evidence writes fail, emit one bounded metadata-only sentinel through a separate best-effort sink whose live implementation writes to a workflow-owned runner-temp path outside the evidence directory. Retain and load those sentinels with the workflow artifact, and make the gate classify matching total sink loss as `<job>:evidence_retention_failed` rather than `<job>:missing_evidence`.

## Boundaries & Constraints

**Always:** Keep canonical report verdicts and existing report-specific deviation tokens unchanged; emit a sentinel only after both `RecordAsync` attempts fail; use a separate injected sink and `CancellationToken.None` for the best-effort side channel; include only schema/kind, canonical run ULID, closed job/scenario tokens, UTC failure time, and stable reason code; validate path containment and marker bounds; keep audit-after-retention and audit-before-alert ordering; handle continuity, projection-rebuild, and scoped-outage identically; preserve ordinary `missing_evidence` when no valid marker exists.

**Block If:** The independent runner-temp path cannot be retained by every workflow invocation/upload that executes the live recovery test, or the gate cannot associate a marker with the same run and expected job without accepting caller-controlled free text.

**Never:** Edit the deferred-work ledger; put tenant identifiers, paths, payloads, exception types/messages, stack traces, secrets, or raw claims in the marker; treat the marker as successful evidence or relax stop-ship behavior; reuse the primary evidence directory; allow marker-write failure to mask the returned `unmeasurable` report.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Fallback succeeds | Canonical evidence write fails; fallback unmeasurable write succeeds | Existing unmeasurable manifest is authoritative; no sentinel | Gate evaluates the manifest normally |
| Total sink loss | Both evidence writes fail | Exactly one independent sentinel; coordinator returns/audits/alerts the unmeasurable report | Gate emits `<job>:evidence_retention_failed`, not `<job>:missing_evidence` |
| Ordinary absence | No manifest and no valid sentinel for a job | No inferred retention failure | Gate retains `<job>:missing_evidence` |
| Invalid side channel | Marker sink throws, or marker is malformed/mismatched | Coordinator still returns unmeasurable; gate rejects malformed marker | Stable fail-closed marker-validation reason; no diagnostic leakage |

</intent-contract>

## Code Map

- `src/Hexalith.ChatBot.Server/Audit/{ContinuityDrillCoordinator,ProjectionRebuildValidationCoordinator,ScopedOutageDegradationValidationCoordinator}.cs` -- duplicated `RetainAsync` double-failure seam; marker must run after the second failure and before audit/alert.
- `src/Hexalith.ChatBot.Server/Audit/IRecoveryValidationEvidenceSink.cs` -- primary/fallback evidence contract; keep unchanged to preserve independence.
- `src/Hexalith.ChatBot.Server/Audit/LiveRecoveryValidationEvidenceAttempt.cs` and `LiveRecoveryValidationEvidenceGate.cs` -- external decision input and per-job `missing_evidence` classification.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs` -- register a safe discarding product default for the new marker sink.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSink.cs` -- existing evidence-directory writer; do not use its directory for markers.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs` -- compose the file marker sink, retain attempt observations even under total sink loss, and pass markers to the in-process diagnostic gate.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveRecoveryEvidenceGateReplayTests.cs` -- discover markers before manifest/summary assumptions and supply them to the authoritative out-of-process gate.
- `.github/workflows/ci.yml` and `.github/workflows/release.yml` -- provide runner-temp marker roots for scheduled, release, and transition-declared recovery runs; copy them into always-uploaded `TestResults` artifacts.
- `tests/Hexalith.ChatBot.Server.Tests/Audit/RecoveryValidationEvidenceSinkCoordinatorTests.cs`, `LiveRecoveryValidationEvidenceGateTests.cs` -- three-coordinator call ordering and classification coverage.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSinkTests.cs`, `tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs` -- bounded file writer and every workflow invocation/upload contract.
- `docs/adrs/live-recovery-validation-drivers.md` -- document the independent side channel and its diagnostic-only, stop-ship semantics.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.ChatBot.Server/Audit/` -- add one marker record, separate sink contract/discarding default, gate input/validation, and all-three coordinator emission after double failure.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/` -- add the contained deterministic file sink; compose, retain, and replay markers without requiring a normal manifest first.
- [x] `.github/workflows/{ci,release}.yml` -- wire runner-temp marker directories into every live E2E invocation and always-stage them for upload.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Audit/`, `tests/Hexalith.ChatBot.IntegrationTests/Recovery/`, and `tests/Hexalith.ChatBot.Architecture.Tests/` -- add focused behavioral, serialization/path, replay, and workflow-contract tests.
- [x] `docs/adrs/live-recovery-validation-drivers.md` -- record marker vocabulary, privacy bounds, independence, and gate meaning.

**Acceptance Criteria:**
- Given either evidence write can still retain the fallback manifest, when recovery evidence is evaluated, then no sentinel is emitted and existing gate behavior is unchanged.
- Given both evidence writes fail in any of the three coordinators, when retention completes, then exactly one bounded marker is attempted through the independent sink before audit/alert and the returned report remains unmeasurable with its existing deviation token.
- Given a valid same-run marker is retained without a manifest, when the external gate evaluates that job, then it stop-ships with `<job>:evidence_retention_failed` and does not report `<job>:missing_evidence`.
- Given no valid same-run marker exists, when expected evidence is absent, then the gate reports ordinary `<job>:missing_evidence`; malformed, future/non-UTC, unknown-job, or run-mismatched markers fail closed without being accepted as retention-failure proof.
- Given any CI/release path invokes the live recovery E2E, when the producer fails or completes, then its independent marker directory is available to the test and included by the always-run artifact staging/upload path.

## Spec Change Log

- 2026-08-27: Implemented the independent retention-failure marker, gate classification, live artifact wiring,
  tests, and ADR documentation; all execution tasks are complete.

## Review Triage Log

### 2026-08-28 — Automated review pass

- `intent_gap`: 0
- `bad_spec`: 0
- `patch`: 12 (`high`: 6, `medium`: 6, `low`: 0)
- `defer`: 0
- `reject`: 4

Addressed findings:

- `[high] [patch]` Retained the attempt summary in the independent runner-temp tree before parsing canonical evidence, so total evidence-directory loss remains replayable.
- `[high] [patch]` Made every valid same-run/job marker a stop-ship `evidence_retention_failed` signal, including partial or contradictory manifest sets.
- `[high] [patch]` Rejected marker JSON with unmapped fields to preserve the closed, metadata-only schema boundary.
- `[high] [patch]` Bounded the best-effort marker sink with a one-second timeout so a hung side channel cannot block audit and alert publication.
- `[high] [patch]` Moved workflow marker/summary staging ahead of failure-prone evidence processing while preserving always-run upload behavior.
- `[high] [patch]` Added a workflow-shaped file-artifact-to-replay test for all three recovery job tokens.
- `[medium] [patch]` Counted marker-backed jobs in required-alert accounting.
- `[medium] [patch]` Restricted replay discovery to the designated `retention-failures` artifact subtree.
- `[medium] [patch]` Added a pre-deserialization marker size bound.
- `[medium] [patch]` Converted marker file races and I/O failures into stable invalid-marker results while preserving cancellation.
- `[medium] [patch]` Changed marker replacement to a temporary sibling plus atomic move to avoid truncated retained files.
- `[medium] [patch]` Made timestamp acquisition best-effort and clamped regressing clocks to the report start time.

Rejected findings:

- `[reject]` Per-dataset marker filenames were not adopted because the sentinel deliberately proves job-level total sink loss and accepts no caller-controlled dataset text.
- `[reject]` A product-runtime persistent marker reader/sink was not added because the intent explicitly assigns persistence to the workflow-owned independent path.
- `[reject]` Symlink-hardening beyond lexical containment was not added for a fresh, workflow-owned runner-temp directory with no untrusted co-tenant writer.
- `[reject]` A live Aspire fault-injection variant was not added because the non-Docker workflow-shaped boundary test exercises the same serialized artifact and replay contract deterministically; the environment-gated replay remains available for infrastructure validation.

### 2026-08-28 — Review pass

- intent_gap: 0
- bad_spec: 0
- patch: 8 (high 2, medium 5, low 1)
- defer: 1 (high 0, medium 1, low 0)
- reject: 17
- addressed_findings:
  - `[high]` `[patch]` Guarded the out-of-process replay gate's `retention-failures` enumeration with
    `Directory.Exists`; an artifact whose producer died before writing a summary leaves that staged root empty,
    `actions/upload-artifact` drops it, and the gate raised `DirectoryNotFoundException` instead of its own reason
    code. Also corrected the assertion message, which named the parent artifact root rather than the directory
    actually searched.
  - `[high]` `[patch]` Made the workflow guard observe the staging step's condition and root: it now asserts each
    staging block carries `if: always()`, that its `RETENTION_FAILURE_ROOT` equals the producer's
    `HEXALITH_CHATBOT_RECOVERY_RETENTION_FAILURE_DIR`, and that the copy is non-blocking. Mutation-checked by
    flipping `release.yml`'s finalize step to `if: success()`, which previously stayed green.
  - `[medium]` `[patch]` Made marker staging non-blocking (`|| true`) in all three workflow blocks: staging was moved
    ahead of `jq`/`mv`/`cp` finalization, so under `set -e` a copy failure newly aborted the always-run attempt
    envelope that previously always ran.
  - `[medium]` `[patch]` Replaced the live producer's ad-hoc marker deserialize loop with the same bounded loader the
    out-of-process gate uses (new `LoadRetentionFailureMarkersFromDirectoryAsync` overload), so identical retained
    bytes cannot be accepted in-process and rejected after upload, and a corrupt marker becomes an invalid candidate
    instead of an exception.
  - `[medium]` `[patch]` Covered the gate's unreadable-marker guard: `RetentionFailureMarkers = [null]` and a null
    list now assert `retention_failure_marker_invalid`. The loader materializes `null` for every retained-but-
    unreadable marker file, and no test evaluated one.
  - `[medium]` `[patch]` Covered the closed per-job scenario vocabulary, which no test exercised: cross-job and
    free-text `Scenario` values, plus `Kind`, `ReasonCode`, before-start and after-completion instants, and the
    duplicate-key branch. Every invalid case now carries its own failure message.
  - `[medium]` `[patch]` Added a composed coordinator to real-file-sink to loader to gate test, so the load-bearing
    assumption that the coordinator's correlation id is the canonical run ULID the gate matches on cannot break
    silently into ordinary `missing_evidence`.
  - `[low]` `[patch]` Stopped temp-file cleanup exceptions from replacing the real marker-write failure; corrected
    the now-stale `LiveRecoveryValidationAttemptSummary` docs (it lives in the independent root, not beside the
    manifests); and documented the required `HEXALITH_CHATBOT_RECOVERY_RETENTION_FAILURE_DIR` variable and the
    one-second best-effort bound in the ADR.

## Design Notes

The marker is diagnostic proof of sink loss, never recovery evidence. Use a deterministic sanitized `{job}-{scenario}.retention-failure.json` filename under a workflow-owned directory outside `LiveRecoveryValidationOptions.EvidenceDirectory`; overwrite retries for the same scenario. A marker-sink failure is swallowed only after the existing double failure so the coordinator's fail-closed report still reaches audit/alert and callers.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug --filter "FullyQualifiedName~RecoveryValidationEvidenceSinkCoordinatorTests|FullyQualifiedName~LiveRecoveryValidationEvidenceGateTests"` -- expected: all marker/order/gate cases pass.
- `dotnet test tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug --filter "FullyQualifiedName~FileRecoveryValidationEvidence|FullyQualifiedName~LiveRecoveryEvidenceGateReplay"` -- expected: writer and replay cases pass without live topology.
- `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --configuration Debug --filter "FullyQualifiedName~LiveRecoveryValidationArchitectureTests"` -- expected: all workflow invocations retain the marker path.
- `dotnet build Hexalith.ChatBot.slnx --configuration Debug` -- expected: zero warnings and errors.

**Results:**
- Server marker/order/gate tests: 41 passed, 0 failed, 0 skipped.
- Integration file/replay tests: 13 passed, 0 failed, 1 environment-gated authoritative replay skipped.
- Workflow architecture tests: 17 passed, 0 failed, 0 skipped.
- Full architecture suite: 104 passed, 0 failed, 0 skipped.
- Debug solution build: succeeded with 0 warnings and 0 errors.
- `git diff --check`: passed.

### 2026-08-28 — Review pass

- intent_gap: 0
- bad_spec: 0
- patch: 5 (high 0, medium 3, low 2)
- defer: 0
- reject: 25
- addressed_findings:
  - `[medium]` `[patch]` The workflow-owned marker root is never reset, while its filenames are deterministic per
    job+scenario rather than per run. A root that survives between runs (a local Tier-3 re-run, which the ADR itself
    instructs to set the variable, or any runner whose temp is not wiped per job) carried a previous run's marker into
    the next attempt, where it failed `marker.RunId == attempt.RunId` and turned a healthy run into
    `retention_failure_marker_invalid` both in process and after upload. The producer now empties the root before the
    topology starts, rather than filtering foreign markers at load — the gate must keep treating a run-mismatched
    marker as fail-closed proof of tampering.
  - `[medium]` `[patch]` Folded into the same move: `RetentionFailureDirectory` was resolved only after Aspire
    startup, health waits, and token acquisition, so a lane that simply forgot
    `HEXALITH_CHATBOT_RECOVERY_RETENTION_FAILURE_DIR` burned the full provisioning budget before reporting it. The
    absolute-path and containment checks now run immediately after `RequireTier3Runtime()`.
  - `[medium]` `[patch]` `SinkTimeout` did not actually bound the sink. `WaitAsync` bounds only the awaited portion of
    a task, so a sink blocking synchronously — a hung filesystem inside `Directory.CreateDirectory`, `Serialize`, or
    `File.Move` — never yielded a task to bound and held audit-then-alert open past the documented one second. The
    attempt is now offloaded before the bound is applied, making the ADR's guarantee real.
  - `[medium]` `[patch]` The workflow guard's `block.Contains("if: always()")` check did not prove what its own comment
    claimed: `if: always() && steps.recovery.outcome == 'success'` satisfied it while skipping staging on exactly the
    failed producer run whose marker the gate needs. The guard now reads the step's condition line, requires it to
    start with `always()`, and rejects any `.outcome` / `.result` / `success()` / `failure()` conjunct. Mutation-checked
    by applying that exact mutation to `release.yml`, which previously stayed green and now fails with the condition
    quoted in the message.
  - `[low]` `[patch]` `liveInvocations` was compared against three counts derived from the same file, so a workflow
    that stopped invoking the live test entirely satisfied all of them with zero — the guard vanished at the moment the
    env var and staging were dropped wholesale. It is now asserted greater than zero.
  - `[low]` `[patch]` The gate built its stop-ship token from a bare `evidence_retention_failed` literal beside
    `RecoveryValidationEvidenceRetentionFailureMarker.EvidenceRetentionFailedReasonCode`, which holds the same text;
    it now composes the token from the constant.

## Auto Run Result

**Outcome:** Done. Total recovery-evidence sink loss still produces a bounded, metadata-only marker through an
independent workflow-owned path, and replay classifies the affected job as `evidence_retention_failed` while preserving
ordinary `missing_evidence`. This follow-up review pass closed a run-isolation defect in the marker root and made two
guarantees the change already documented actually hold.

**Changed areas (this pass):**

- `tests/.../LiveContinuityAspireE2eTests.cs` — the marker root is resolved and emptied before the topology starts, so
  a reused root cannot carry a foreign run's marker into this attempt and the missing-variable check fails fast.
- `src/.../RecoveryValidationEvidenceRetentionFailureRecorder.cs` — the best-effort attempt is offloaded before the
  one-second bound is applied, so a synchronously blocking sink can no longer delay audit and alert.
- `tests/.../LiveRecoveryValidationArchitectureTests.cs` — the staging guard now reads the step's condition instead of
  substring-matching `if: always()`, rejects success-dependent conjuncts, and refuses to run vacuously at zero
  invocations.
- `src/.../LiveRecoveryValidationEvidenceGate.cs` — the stop-ship token is composed from the marker's reason-code
  constant rather than a duplicated literal.

**Review findings:** four lenses produced 5 accepted patches (0 high, 3 medium, 2 low), 0 deferred items, and 25
rejected findings. No intent gaps and no specification defects.

**Follow-up review recommended:** `true` — no high-severity patched findings, but the weighted score is
`3 x 3 medium + 1 x 2 low = 11`, at or above the threshold of 5.

**Verification:**

- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -m:1` — succeeded, 0 warnings, 0 errors. (The parallel
  build first emitted a transient MSB3026 file-copy race on `Hexalith.ChatBot.Client.dll`, which did not recur.)
- Server marker/order/gate tests: 43 passed, 0 failed, 0 skipped.
- Full `Hexalith.ChatBot.Server.Tests`: 1858 passed, 0 failed, 0 skipped.
- Integration file/replay tests: 14 passed, 0 failed, 1 environment-gated authoritative replay skipped.
- Full architecture suite: 104 passed, 0 failed, 0 skipped.
- Mutation check: setting `release.yml`'s finalize step to `if: always() && steps.recovery.outcome == 'success'` now
  fails the workflow guard with "must not make retention staging depend on '.outcome'"; reverted and re-verified green.
- `git diff --check`: passed.

**Residual risks:**

- The authoritative out-of-process replay test remains environment-gated and was skipped locally; the composed and
  workflow-shaped integration tests exercise the producer-to-replay boundary without that external topology.
- A cancelled workflow token still bypasses the retention fallback entirely, so a deadline-killed run yields plain
  `missing_evidence`. Spec-conformant and already recorded as a deferred item.
- The live marker sink and the artifact loader remain test-project code, consistent with the existing evidence sink;
  deployed product DI resolves the discarding no-op by design. The bounded-timeout offload therefore hardens a path
  that only the live lane exercises today.
- The workflow contract is still verified by reading the YAML text rather than by executing it: the guard now proves
  the staging step's condition and root, but not that `actions/upload-artifact` actually transferred the staged
  subtree on a real runner.
