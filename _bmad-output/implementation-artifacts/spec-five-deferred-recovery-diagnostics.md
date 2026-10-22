---
title: 'Resolve five deferred recovery-harness diagnostic items'
type: 'bugfix'
created: '2026-10-22'
status: 'in-review'
route: 'oneshot'
baseline_commit: '6b92c2970d8fd7fb6bc2d3ad26f864e80313e111'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close five deferred recovery-harness items: DW-60, DW-97, DW-98, DW-101 and DW-103. Keep control POST transport failures distinct from token-acquisition or post-admission failures; require an initial absence observation even for tiny positive windows; retain internal cancellation failures by comparing cancellation-token identity; correct the mailbox probe's readiness claim; and verify the existing concurrent-failure regression at least 50 times. Preserve metadata-only diagnostics, caller cancellation, recovery isolation and all release gates. Run focused checks and independent review, record exact closure evidence, commit and push only this task's changes to main as requested.

</frozen-after-approval>

## Implementation Notes

Investigation found no user-visible design choices or irreversible implementation operations. This is a small batch confined to the existing integration-test harness and its regression coverage; it adds no production API or dependency. The user explicitly authorized the batch and push.

Code map: `Recovery/AspireScopedOutageOperations.cs:IsChatBotControlAvailableAsync` catches too broadly; reuse `ScopedOutageOperationsTestSeam` and the existing deterministic HTTP/durable-state handlers. `Recovery/AspireRecoverySandboxOperations.cs:RemainsGovernedOperationProjectionAbsentAsync` can skip its loop; its constructor already refuses nonpositive windows. The three `Recovery/Capturing*` decorators share the cancellation-state filter. `Recovery/LiveContinuityAspireE2eTests.cs` retains a stale warm-up comment. `TrivialGovernedCommandAspireE2eTests.TierThreeParallelValidationShouldRetainEveryNonCancellationSiblingFailure` already uses the outer test token for its barrier; preserve its sequential aggregation and verify repeatedly.

Baseline `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2 after a successful Debug build (zero warnings/errors): missing `ChatBot:LiveRecoveryValidation:MailboxClientSecret`; AppHost exited 134. No topology resources started. Focused integration-project build and infrastructure-free tests will provide harness evidence; no live recovery qualification is claimed.


Implementation completed in the existing harness: initial/final absence observations, POST-only transport attribution with headers-only completion, exact cancellation-token capture, caller-only linked cancellation normalization with an independently retained deadline, timer shutdown on caller cancellation, corrected mailbox commentary and 64 concurrent-failure repetitions. New deterministic test seams remain internal to the test assembly. No production files, dependency versions or submodule pointers changed.

The default Debug integration build initially failed with duplicate Hexalith.Commons.UniqueIds assemblies (CS1704). Explicit whole-graph source mode resolved it: `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 --nologo` passed with zero warnings/errors. After the final test-only patches, the same command with `-p:BuildProjectReferences=false` reused those successfully built unchanged dependencies and passed with zero warnings/errors. Logs: `/tmp/chatbot-recovery-review-build.log` and `/tmp/chatbot-recovery-final-build.log`.

Final verification uses `.github/scripts/run-xunit-v4.sh` with `XUNIT_REQUIRE_ZERO_SKIPS=1` against `tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests`. The eight selected classes (RecoveryFailureCaptureTests, RecoveryAbsenceObservationTests, ScopedOutageControlProbeTests, AspireRecoveryCleanupStateTests, LiveContinuityAspireE2eContractTests, LiveContinuityDrillScenarioRunnerTests, LiveProjectionRebuildDriverTests, LiveScopedOutageInjectionDriverTests) passed 181 tests with no failures/skips; evidence: `TestResults/five-recovery-diagnostics-final.{ctrf.json,trx}`. The separately filtered `TierThreeParallelValidationShouldRetainEveryNonCancellationSiblingFailure` passed all 64 internal repetitions with no failures/skips; evidence: `TestResults/five-recovery-diagnostics-concurrency-final.{ctrf.json,trx}`. An initial combined class+method filter selected zero tests and was correctly refused by the boundary; it was replaced with separate class and method runs and contributes no passing evidence.

Repository completion limitation: `docs/story-evidence-integrity.md` and `story-evidence-policy.json` require the real bound recovery and Aspire/Dapr primary lanes for a terminal spec transition involving these paths. The startup blocker above prevents that evidence. This spec therefore stays in-review instead of claiming terminal story/release qualification; the five deferred ledger closures are backed by the focused infrastructure-free checks. No terminal product story, technical enabler or sprint status was changed.

## Review Triage Log

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| Linked scenario cancellation mistaken for independent failure | medium / patched | Added known-linked-token normalization at all three real driver boundaries and composed driver/decorator tests; unrelated and independently expired deadline tokens retain their original identity. |
| Buffered response-body failure erases an observed status | medium / patched | Command POST uses ResponseHeadersRead; failing-content regression retains observed status-503 without reading the body. |
| Original inner exception exposes private detail through ToString | medium / deferred | Verified identical inner-exception retention at the full baseline revision. Recorded a separate rendered-output boundary follow-up in deferred-work; wrapper message alone is not claimed as full redaction. |
| Identity branch lacks retained-bearer verification | low / patched | Test makes acquisition throw if called and asserts the checkpoint bearer reaches the command POST. |
| Final absence read errors and delay cancellation untested | medium / patched | Added separate first/final read failures for transport/cancellation and real caller cancellation during the delay. |
| Absence tests cover only a tiny window | medium / patched | Added sustained-window polling and intermediate persisted-presence tests. |
| Scenario deadline expires during successful independent cleanup | medium / patched | Caller cancellation disables the pending scenario timer; already-expired budgets stay canceled. Tests cover both states after waiting past the original timer deadline. |
