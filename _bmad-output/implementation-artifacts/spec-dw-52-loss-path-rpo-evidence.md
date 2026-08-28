---
title: 'DW-52 retained loss-path RPO evidence'
type: 'feature'
created: '2026-08-28'
status: 'done'
baseline_revision: 'a675d3cd944c4cfedee49f81f1da12a87f1696b7'
baseline_commit: 'a675d3cd944c4cfedee49f81f1da12a87f1696b7'
review_loop_iteration: 0
followup_review_recommended: true
context: []
warnings: [oversized]
deferred:
  - summary: >-
      RecoveryValidationTopologyContractTests.PrepareKeycloakRealmImportWritesTheRenderedRealmWithOwnerOnlyPermissionsAtAnUnpredictablePath
      fails in a full IntegrationTests run while passing in isolation.
    evidence: |-
      The test asserts that exactly one new `{temp}/hexalith-chatbot-keycloak-*` subdirectory appears, but a sibling
      test in the same class creates four such directories, so the delta assertion sees 2 in a whole-assembly run.
      Reproduced at baseline dc194c7 with every DW-52 change stashed (280 tests, same single failure), so it is
      pre-existing and untouched by this story; both files are absent from this diff.
    location: >-
      tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs:377
    severity: medium
  - summary: >-
      Only the controlled-loss stage writes a failed attempt summary, so the gate's `latest_attempt_incomplete`
      branch stays unreachable when the projection-rebuild or scoped-outage stages throw.
    evidence: |-
      LiveContinuityAspireE2eTests wraps RunControlledLossAndRetainAsync in a catch that writes
      `LatestAttemptCompletedSuccessfully: false`; the stages that run after it still propagate without one, so a
      hosted failure there leaves no attempt summary for the gate to read. Pre-existing shape — no stage had such a
      summary before this story added one for the new stage.
    location: >-
      tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:274
    severity: low
---

<intent-contract>

## Intent

**Problem:** The hosted recovery lane publishes `MeasuredRpo = 0s` only because its two continuity scenarios retain every committed operation. That green no-loss result proves safety but does not exercise or substantiate A10's RPO <= 15 minutes.

**Approach:** Add a separately identified, retained controlled-loss drill whose RPO is derived exclusively from authoritative EventStore commit timestamps around a deliberately rejected subscription notification and whose independent gate requires a positive measurement no greater than `RecoveryTargets.MaxRpo`. Keep ordinary continuity scenarios, unexpected-loss stop-ship semantics, and the 15-minute runtime target unchanged.

## Boundaries & Constraints

**Always:** Read both commit bounds from persisted EventStore actor envelopes; prove the pre-fault operation durable, the fault-window candidate absent, and the post-restore retained operation durable; preserve tenant isolation, metadata-only artifacts, cleanup, exact repository/run binding, freshness, and independent replay; retain the loss report/manifest through the existing CI/release artifact; treat only positive RPO at or below the canonical 900-second target as qualifying loss-path evidence.

**Block If:** The sandbox cannot expose a safe stable candidate identity before the injected dependency rejection without changing production worker behavior, or the durable actor envelope cannot supply validated UTC timestamp/sequence bounds.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; cite `0s`, unit tests, local diagnostics, or an unpublished artifact as A10 proof; delete or tamper with append-only EventStore state; add the drill to `ContinuityDrillScenarios.All`; weaken unexpected committed-data-loss, cleanup, tenant-isolation, or release stop-ship behavior; change `RecoveryTargets.MaxRpo`/`MaxRto`; claim overall A10 ratification while the four-hour RTO residual remains open.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Qualifying loss path | Durable pre-fault commit, rejected fault-window candidate, durable post-restore commit; `0 < rpo <= 900s` | Metadata-only retained report/manifest records durable bounds and independent gate passes the RPO channel | No error expected |
| Vacuous or invalid bounds | Zero, missing, malformed, non-UTC, mismatched, or reversed persisted bounds | Artifact is ineligible; ordinary no-loss manifests cannot substitute | Fail closed with stable loss-path/bound reason |
| Target miss | Valid positive durable-bound RPO `> 900s` | Evidence remains inspectable but release gate reports target deviation | Block release without relabeling as structural loss |
| Residual or cross-tenant loss | Retained commit missing, unauthorized mutation, isolation/cleanup failure | Existing safety invariant remains failed | Structural/cleanup stop-ship; never accepted as controlled loss |

</intent-contract>

## Code Map

- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/EventStoreDurableStateProbe.cs` -- authoritative `:events:1` reader; extend its boolean probe to return validated event sequence/timestamp observations.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs` -- current subscription checkpoint/fault/restore path; add separate controlled-loss operations without weakening ordinary continuity.
- `tests/Hexalith.ChatBot.RecoverySandbox/Program.cs` and `RecoveryNotificationIdentity.cs` -- closed authenticated simulator route/phase vocabulary; expose only safe candidate identity and observed timestamps.
- `src/Hexalith.ChatBot.Server/Audit/LiveRecoveryValidationEvidenceGate.cs` -- canonical target, assertion, completeness, freshness, commit-binding, and release decision authority.
- `src/Hexalith.ChatBot.Server/Audit/RecoveryValidationEvidenceManifest.cs` -- metadata-only manifest contract and validation.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSink.cs` -- run-scoped artifact producer already uploaded by CI/release workflows.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs` -- hosted producer orchestration and pre-replay non-vacuity assertions.
- `.github/workflows/ci.yml` and `.github/workflows/release.yml` -- read-only unless the existing whole-directory upload/invocation cannot retain and replay the added channel.
- `docs/adrs/live-recovery-validation-drivers.md`, `docs/adrs/continuity-drill-and-rpo-rto-validation.md`, `_bmad-output/planning-artifacts/architecture.md` -- normative separation of no-loss safety from retained RPO proof.
- `_bmad-output/implementation-artifacts/12-15-stand-up-live-recovery-continuity-fault-injection-drivers-and-recalibrate-a10.md` -- historical story record; append DW-52 mechanism/results without rewriting prior evidence.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/EventStoreDurableStateProbe.cs`, new `DurableCommitObservation.cs`, and `EventStoreDurableStateProbeTests.cs` -- return and test fail-closed persisted UTC sequence/timestamp/identity bounds, including missing, malformed, mismatched, and reversed cases.
- [x] `tests/Hexalith.ChatBot.RecoverySandbox/RecoveryNotificationIdentity.cs`, new `CapturingRecoveryChatBotClient.cs`, and `Program.cs` -- add closed loss/post-recovery phases and expose only safe ULID/timestamp metadata before dependency rejection; pin the route contract in `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eContractTests.cs`.
- [x] New `tests/Hexalith.ChatBot.IntegrationTests/Recovery/IControlledLossPathOperations.cs`, `AspireControlledLossPathOperations.cs`, and `LiveControlledLossPathRunner.cs` -- witness pre-fault durability, rejected candidate absence, restoration, post-restore durability, isolation, and cleanup; cover zero/reversed bounds, cancellation, target boundary/miss, and residual loss in new `LiveControlledLossPathRunnerTests.cs`.
- [x] New `src/Hexalith.ChatBot.Server/Audit/ControlledLossPathReport.cs`, `ControlledLossPathEvaluator.cs`, and `ControlledLossPathVerdicts.cs`, plus `IRecoveryValidationEvidenceSink.cs`, `LiveRecoveryValidationJobs.cs`, `RecoveryValidationEvidenceManifest.cs`, and `LiveRecoveryValidationEvidenceGate.cs` -- define the distinct evidence job and validate positive RPO, canonical target, durable-bound assertions, and metadata-only fields without changing `ContinuityDrillScenarios` or its evaluator.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSink.cs`, `LiveContinuityAspireE2eTests.cs`, and `LiveRecoveryEvidenceGateReplayTests.cs` -- retain the new report/manifest pair, assert it is non-vacuous before aggregation, and replay it out of process with exact commit/freshness/duplicate/corruption failure coverage.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Audit/LiveRecoveryValidationEvidenceGateTests.cs`, `RecoveryValidationEvidenceManifestTests.cs`, `RecoveryValidationEvidenceSinkCoordinatorTests.cs`, `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSinkTests.cs`, and `tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs` -- update closed job/count fixtures and add anti-zero, <=900, >900, missing-bound, structural-loss, producer, replay, and topology-authority tests.
- [x] `docs/adrs/live-recovery-validation-drivers.md`, `docs/adrs/continuity-drill-and-rpo-rto-validation.md`, `_bmad-output/planning-artifacts/architecture.md`, PRD/addendum/decision log/NFR assessment, `RecoveryTargets.cs` XML docs, and Story 12.15 completion notes -- distinguish implemented mechanism from genuine hosted evidence; cite a loss artifact only if actually produced and keep overall A10 provisional while RTO remains unproven.

**Acceptance Criteria:**
- Given either ordinary hosted continuity scenario retains all committed data, when evidence is published, then its `MeasuredRpo` remains `0s` and the RPO proof channel rejects it as non-exercising.
- Given the controlled fault-window candidate is rejected and surrounding operations have validated durable EventStore bounds, when the retained loss-path artifact is gated, then its positive measured RPO is compared to exactly 900 seconds and the durable retained/lost identities are independently evidenced.
- Given a valid loss-path RPO at or below 15 minutes, when independent retained replay runs for the exact commit inside the freshness window, then the RPO channel passes without weakening ordinary unexpected-loss stop-ships.
- Given missing/inverted/fabricated bounds, zero RPO, residual committed-data loss, cleanup failure, cross-tenant leakage, stale evidence, or commit mismatch, when the gate runs, then it fails closed with a stable reason and cannot support A10.
- Given only local or unit evidence exists, when governance docs are updated, then they describe the mechanism as ready but do not claim hosted RPO proof or overall A10 ratification.

## Spec Change Log

- 2026-08-28: Implemented the controlled-loss runner, authoritative EventStore bounds, retained fourth-job
  evidence/gating, independent replay failures, closed sandbox authority, regression coverage, and honest
  mechanism-only governance updates. Local Release verification is green; no hosted loss artifact was produced.
- 2026-08-28: Hardened first-event probing, lane/phase ownership, active-fault authorization, same-clock and
  requested-tenant bounds, cross-tenant safety, failure retention, gate-only alert accounting, and missed-reason
  consistency after review. Focused and full Release verification remain green; hosted proof remains outstanding.

- 2026-08-28: Closed the executing-coverage gaps around the seam that creates the loss and the producer side of the
  fourth job's evidence, declared the drill's external-M365 residual, and finished the decision log's record of the
  three-job replay break. Focused and full Release verification remain green; hosted proof remains outstanding.

## Review Triage Log

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 12: (high 5, medium 6, low 1)
- defer: 0
- reject: 5: (high 1, medium 4, low 0)
- addressed_findings:
  - `[medium]` `[patch]` Corrected first-event durable-bound reads so aggregates with later metadata sequences remain valid; added advanced-metadata coverage.
  - `[medium]` `[patch]` Required the controlled loss phase to observe an active subscription fault before bypassing the ordinary source failure.
  - `[high]` `[patch]` Added a dedicated controlled-loss sandbox lane and rejected controlled phases from ordinary continuity/graph authority.
  - `[high]` `[patch]` Bound both authoritative commit observations to the requested tenant before report projection.
  - `[medium]` `[patch]` Removed cross-process timestamp ordering and retained only same-authoritative-clock RPO ordering plus UTC/identity/sequence validation.
  - `[medium]` `[patch]` Extended the controlled-loss deadline so a hosted run can retain and gate a measured RPO above 900 seconds instead of timing out at the target.
  - `[high]` `[patch]` Added the fourth-job retention-failure marker around runner/sink failures.
  - `[high]` `[patch]` Moved controlled-loss success assertions after attempt-summary retention so failed evidence remains replayable.
  - `[medium]` `[patch]` Classified controlled loss as a gate-only alert channel while preserving release blocking for target and structural failures.
  - `[medium]` `[patch]` Combined cleanup failures with the primary injection/restoration diagnostic instead of replacing it.
  - `[low]` `[patch]` Added a distinct target-missed reason code and enforced verdict/reason consistency.
  - `[high]` `[patch]` Added post-recovery cross-tenant aggregate/read-model absence and sentinel-preservation checks, plus corresponding cleanup and tests.

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 6: (high 1, medium 2, low 3)
- defer: 2: (high 0, medium 1, low 1)
- reject: 17: (high 1, medium 6, low 10)
- addressed_findings:
  - `[high]` `[patch]` The previous pass's tenant binding compared the persisted envelope against the logical
    `replay-test:` label while the topology writes durable state under the physical name
    `ReplayTenantPolicy.StorageTenantFor` derives, so every hosted controlled-loss run would have thrown
    "belongs to another tenant" on the pre-fault bound. `RequireRequestedTenant` now resolves that same
    single-source derivation; the fake operations publish the physical tenant, and two tests pin both directions
    (derived name accepted, logical label rejected).
  - `[medium]` `[patch]` `rpo_measurement_mismatch` — the gate's recompute of RPO from the manifest's own persisted
    bounds, the only thing tying the graded number to EventStore — had no test anywhere in the repo. Added a case
    that publishes 700s against 20s of durable bounds and asserts the stop-ship.
  - `[medium]` `[patch]` Adding `controlled-loss-path` to `LiveRecoveryValidationJobs.All` is a compatibility break:
    the 2026-08-27 three-job bundle the ADR/addendum/PRD/`RecoveryTargets` re-cite can no longer replay clean
    (`controlled-loss-path:missing_evidence`), while the addendum still claimed it passed "through the shipped
    evidence gate" and was citable through 2026-09-04. Corrected all four documents and pinned the break with a
    gate test over a three-job attempt.
  - `[low]` `[patch]` The out-of-process replay fixture used an invented action vocabulary
    (`fault:`/`restore:subscription-notification-rejection`) that the producer never emits; aligned it to
    `FileRecoveryValidationEvidenceSink`'s actual `reject:subscription-notification` /
    `restore:graph-subscription`.
  - `[low]` `[patch]` `_controlledFaultLeftStateUnchanged` survived `CleanupSubscriptionScenarioAsync` alongside the
    refs it is derived from, so a second controlled-loss run on the same operations instance would publish the
    previous run's isolation fact; it is now reset with them.
  - `[low]` `[patch]` The `PerScenarioTimeout` comment justified 20 minutes as "greater than MaxRpo", which was
    equally true of the previous 25; recorded the actual driver (ten serial scenarios plus topology margin inside
    the smallest configured workflow window) and cross-referenced the contract test that encodes it.

## Design Notes

Use a distinct job/report contract rather than a third ordinary continuity scenario. Existing continuity manifests require `data-loss-absent`, and their coordinator treats any committed loss as `missed`; special-casing expected loss there would corrupt the safety signal. The controlled drill instead drops one known notification during the closed subscription fault, proves that candidate was never durably committed, and measures the recovery-point gap between authoritative durable commits before and after restoration.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: build succeeds; run focused gate/evaluator/manifest classes directly with `-class`.
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: build succeeds; focused durable-probe/loss/sink/replay classes pass.
- `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: loss channel remains closed and sandbox-only.
- `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: solution builds with warnings as errors.
- `git diff --check` -- expected: no whitespace errors; deferred-work ledger unchanged.

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 7: (high 1, medium 4, low 2)
- defer: 0
- reject: 20: (high 0, medium 8, low 12)
- addressed_findings:
  - `[high]` `[patch]` `CapturingRecoveryChatBotClient` — the decorator whose 503 *is* the controlled loss — had no
    executing test anywhere: only `LiveRecoveryValidationArchitectureTests`' source-text match on its class name, and
    a Tier-3 fact that skips without `HEXALITH_CHATBOT_TIER3`. Inverting `rejectSubmission`, or capturing the
    candidate identity after the throw, left every suite green while turning the drill back into a no-loss run.
    Added `RecoverySandboxContractTests.ControlledLossDecoratorCapturesTheCandidateAndRejectsOnlyTheLossPhase`,
    which drives the real `GraphMailboxIntakeWorker` over the real `ControlledGraphMailboxMessageSource` and pins
    both phases: the loss phase yields `chatbot_submission_recoverable` with nothing reaching the inner client, the
    non-loss phase submits, and either way the captured `CandidateRef` is a canonical ULID with a UTC observation.
  - `[medium]` `[patch]` The controlled-loss manifest omitted `RV-EXT-M365`. `ResidualIds` keyed only continuity's
    `M365SubscriptionFailure` and scoped-outage `Graph`, so the drill — which faults and restores that same composed
    subscription boundary, and whose ADR row explicitly retains the residual — fell through to the default arm. The
    one artifact that will carry the RPO claim was under-declaring its own boundary limitation. Added the arm and
    pinned it.
  - `[medium]` `[patch]` `AspireRecoverySandboxOperations.EvaluateControlledLossSafety` collapses twelve observations
    into the five facts the gate turns into `structural_breach`; eleven of the twelve conjuncts were unasserted (the
    single existing case flipped one input and checked one output). Dropping `candidateReadModelsAbsent` or
    `sentinelsUnchangedDuringFault` would have published `candidate-absent: true` on a run where the rejected
    candidate had materialized. Added a one-input-at-a-time theory over every conjunct plus a dedicated case for the
    shared `sentinelsUnchangedAfterRecovery` input.
  - `[medium]` `[patch]` No test tied the sink's hand-listed assertion dictionary to the gate that grades it: the
    replay fixture builds its `Assertions` from `RequiredAssertionsFor` and the gate tests hand-build manifests, so a
    renamed or dropped producer key would have surfaced first on a hosted run. Added
    `RecordedControlledLossManifestSatisfiesTheGateItWillBeReplayedThrough`, which records through
    `FileRecoveryValidationEvidenceSink` and asserts the written manifest satisfies every required assertion.
  - `[medium]` `[patch]` The failure path that tells the gate a hosted attempt was incomplete lived inline in the
    skipped Tier-3 fact and was pinned only by source-text matches. Extracted `RunControlledLossStageAsync` and added
    contract tests asserting the summary lands in the retention-failure root (never the evidence directory) with
    `LatestAttemptCompletedSuccessfully: false` and the four-job alert map, and that a successful stage leaves none.
  - `[low]` `[patch]` `RejectControlledLossCandidateAsync` read `candidateObservedAtUtc` eagerly with
    `GetDateTimeOffset()`. When the sandbox never reached the submission boundary the capture is JSON `null`, so a
    raw "element is not a string" error replaced the method's own "candidate was not safely identified" diagnostic
    in the hosted log. Now read defensively and routed into the intended failure.
  - `[low]` `[patch]` The `.decision-log.md` entry — the document the PRD's A10 row points readers to — recorded only
    that the 2026-08-27 bundle "predates this fourth channel", while the ADR, addendum and PRD all record the
    stronger fact that it now returns `controlled-loss-path:missing_evidence` and can no longer replay clean. Added
    that sentence so the four re-cited documents agree.

## Auto Run Result

Status: done
Blocking condition: none

**Summary:** Follow-up review pass over the retained `controlled-loss-path` channel. Four review layers ran against
the full diff since `a675d3cd944c4cfedee49f81f1da12a87f1696b7`. The pass found no intent gap and no spec defect: the
mechanism, its gating, and its honest mechanism-only governance framing are unchanged in substance. What it did find
was that the channel's most load-bearing behaviours were verified by source text rather than by execution — above all
the decorator that actually creates the loss. Seven findings were patched, none deferred, twenty rejected.

**Files changed in this pass:**
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoverySandboxContractTests.cs` -- drive the real intake worker
  across `CapturingRecoveryChatBotClient` for both the loss and non-loss phases.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSink.cs` -- declare `RV-EXT-M365`
  on the controlled-loss manifest.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/FileRecoveryValidationEvidenceSinkTests.cs` -- assert a recorded
  controlled-loss manifest satisfies the gate's required assertions and carries the residual.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveControlledLossPathRunnerTests.cs` -- one-input-at-a-time
  coverage of the safety-fact aggregation.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs` -- extract
  `RunControlledLossStageAsync` so the incomplete-attempt summary's destination and flag are executable.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eContractTests.cs` -- contract tests for
  that stage's failure and success paths.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs` -- defensive read of the
  captured candidate observation instant.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/.decision-log.md` -- record the three-job
  replay break the other re-cited documents already state.

`_bmad-output/implementation-artifacts/deferred-work.md` and `.github/workflows/{ci,release}.yml` were not changed.

**Review findings:** 7 patches applied (high 1, medium 4, low 2); 0 deferred; 20 rejected. The rejections included
several substantial claims that did not survive verification against the code: that the gate's
`durable_bounds_invalid` branch is dead (it is reached at the replay surface, which is the gate's actual job, because
`ValidateControlledLossMeasurement` runs before `manifest.Validate()`); that the failure path fabricates alert counts
for stages that never ran (zero is literally what those un-constructed coordinators delivered); that the sandbox's
shared `RecordProcessing` counters corrupt other lanes (only the per-response `submitted` boolean is ever consumed);
and that the sentinel null-forgiving reads can throw (`InjectSubscriptionFaultAsync` seeds them before the reject
step, matching the pre-existing pattern). Two others were re-raised from the previous pass without new evidence and
rejected again for the same reasons: cross-clock ordering of `RejectedAtUtc` against the EventStore bounds, which the
previous pass deliberately removed and pinned with tests, and the probe's widened envelope predicate.

**Follow-up review recommendation:** `true` -- a high-severity patch was applied. Patched severity counts: high 1,
medium 4, low 2; weighted medium/low score `3 x 4 + 2 = 14`.

**Verification performed:**
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/... -c Release`: succeeded, 0 warnings / 0 errors.
- `dotnet build tests/Hexalith.ChatBot.Server.Tests/... -c Release`: succeeded, 0 warnings / 0 errors.
- `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/... -c Release`: succeeded, 0 warnings / 0 errors.
- `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`:
  succeeded, 0 warnings / 0 errors.
- IntegrationTests whole assembly: 299 total, 293 passed, 6 environment-gated skips, 0 failed (17 new tests). The
  `RecoveryValidationTopologyContractTests` interference recorded as `deferred` in the previous pass did not
  reproduce in this run; it is order-dependent and remains recorded rather than re-observed.
- Server.Tests 1870/1870. Architecture.Tests 104/104.
- `git diff --check`: clean apart from the trailing blank line this section replaces; deferred-work ledger unchanged.

**Residual risks:** Unchanged and still dominant -- no hosted Tier-3 controlled-loss run has been produced, so the
channel's RPO evidence and A10 overall remain provisional, and the four-hour RTO residual is untouched. This pass
does not change what the hosted run would measure; it changes what fails locally when the mechanism regresses. The
seam that creates the loss now executes in the normal suite, so an inverted rejection or a lost candidate capture is
caught before a hosted lane; the producer's manifest is now checked against the gate that grades it. What remains
unexercised outside a hosted run is the Aspire sandbox operations layer itself -- `WitnessControlledLossCommitAsync`,
`RejectControlledLossCandidateAsync`, `ReadControlledLossSafetyAsync` -- and the authoritative EventStore envelope
shape the durable probe reads, which is still pinned only against a test-authored JSON document. A separate honest
limitation worth carrying into any future citation: the measured RPO is the span between two commits the drill itself
creates around its own inject/reject/restore choreography, so its magnitude reflects the injected outage duration
rather than an independent bound on production recovery-point exposure.
