---
title: 'Preserve recovery-validation outcomes across failure boundaries'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve five confirmed deferred follow-ups from the recovery-boundaries review: preserve caller cancellation arriving during isolation cleanup and bound synchronously blocking invalidation; detect the latest failed scheduler pass independently of monotonic success timestamps; reject invalid filesystem syntax in dormant recovery evidence paths; preserve a witnessed cross-tenant breach and its locator if later seed/read operations fail; and restore the existing summarize-recovery-attempt CLI grammar used by CI. Preserve independent shared cleanup deadlines, invocation-specific sentinel ownership, strict fail-closed outcomes, audit-before-alert ordering, existing success-clock monotonicity, metadata-only evidence and strict CLI option rejection. Verify meaningful regressions, independently review, record ledger dispositions, commit and push to main as requested. The separate M2 cadence retry and sentinel-retention lifecycle work remains deferred.

</frozen-after-approval>

## Implementation Notes

Investigation found no unresolved intent choices or irreversible implementation operations. This is a small internal bugfix batch reusing current probe, in-memory status and CLI seams, without dependency, submodule, persistence-schema or public HTTP contract changes. The user requested implementation and push; the oneshot route applies.

Code map: `Audit/DerivedStoreIsolationProbeCoordinator.cs` needs an independently scheduled invalidation invocation before its bounded wait, a post-cleanup caller check, and incremental retained breach evidence across the outer catch. Extend `DerivedStoreIsolationCleanupTests` and existing controlled-store fixture. `Operations/PeriodicEnforcement/InMemoryPeriodicEnforcementStatusStore.cs` and `PeriodicEnforcementRunStatus.cs` need a nullable latest terminal-pass outcome consumed by `PeriodicEnforcementCoordinator.CheckHealthAsync`; starts and overlaps must preserve that outcome. Extend status-store and coordinator regressions. `Audit/LiveRecoveryValidationOptions.cs` must combine absolute-path validation with platform invalid-path-character rejection, covering enabled and dormant overlays in `LiveRecoveryValidationOptionsTests`. `tools/Hexalith.ChatBot.StoryEvidenceGate/CommandArguments.cs` must admit only the options already consumed by `Program.SummarizeRecoveryAttempt`; execute the actual CLI in gate tests and retain malformed/missing-option refusals.

Validation uses the checked-in pinned source-mapping wrapper and original Debug projects. The baseline AppHost start and resource inspection occur before code changes. Focused Server and gate regressions precede the full Server suite. Recovery-primary is attempted separately where the environment permits; no local unit result qualifies A10 or replaces mandatory live evidence. Retain exact commands and results under `TestResults/five-recovery-followups` and a tracked evidence summary. Keep the spec in review if completion evidence remains blocked.


Implementation outcome: isolation invalidation is invoked on a worker and awaited against the existing shared independent deadline. Caller cancellation is checked inside cleanup's finally (including pending seed/read failures) and before verdict or audit. Partial probe observations are retained so a later store failure preserves Breach and its first locator. Latest scheduler completion outcome is stored under the existing lock independently of preserved wall-clock maxima; starts and overlaps do not erase it. Configured evidence directories reject platform invalid path characters, including NUL. This is lexical platform-character validation, not exhaustive filesystem/component legality or directory accessibility. The existing summary handler now has its strict command grammar entry.

Baseline revision: `07b8ecebe3484031a66d9aea8519bd00e7de9d29`. The pinned source-mapping wrapper started the real AppHost successfully; Aspire resource inspection reported all listed resources Running, then the exact task-owned AppHost stopped successfully before builds. Existing HXW009 and HXW011 preparation advisories remain; dependency guards and pins are unchanged.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Pending seed/read error could bypass caller cancellation during awaited cleanup | medium / patched | Caller check now runs in cleanup finally; two regressions assert the original caller token after seed/read failure. |
| Broader Windows directory component syntax is not covered by GetInvalidPathChars | medium / deferred, Windows runtime unverified | Current batch closes the reported NUL/platform-invalid-character hole. Full component legality needs platform-native tests and careful root/extended-path handling. No exhaustive filesystem-validity claim is made. |
| A new singleton failed episode inside the previous alert suppression window may be quiet | medium / deferred | Existing reason-keyed suppression is not reset by a successful pass. Latest terminal status is now accurate; alert episode reset is separate pre-existing work. |
| Synchronous-block test could time out before invocation starts and then wait for nonexistent cleanup | low / patched | Increased its entry scheduling margin and drain only after the callback actually entered; always release the blocker. |
| Later-failure tests did not verify cleanup end-state | medium / patched | Successful cases assert every owner partition contains no synthetic sentinel after cleanup. |
| CLI glue coverage omitted default-policy and failed-producer input states | medium / patched | Real Program.Main tests omit --policy and cover parsed, absent and malformed TRX with payload exclusion. |

The same independent reviewer rechecked all four corrections and reported no remaining findings in that scoped recheck. The two pre-existing follow-ups are recorded separately; the original M2 cadence and sentinel-retention work remains outside this batch.


## Final Verification

The original Debug Server, StoryEvidenceGate and Integration test projects built with zero warnings/errors. All source hashes remained unchanged during final testing; the Server DLL beside the Server and Integration test executables matches the source project's DLL exactly. Full commands, counters, source/binary/TRX checksums and local result paths are retained in `evidence/five-deferred-recovery-followups.json`.

| Final ordinary lane | Passed | Failed | Skipped | Result |
| --- | --- | --- | --- | --- |
| Server, serial full suite | 3534 | 0 | 0 | `TestResults/five-recovery-followups/server-reviewed.{ctrf.json,trx}` |
| StoryEvidenceGate, serial full suite | 313 | 0 | 0 | `TestResults/five-recovery-followups/gate-reviewed.{ctrf.json,trx}` |
| Integration, ordinary suite | 453 | 0 | 7 | `TestResults/five-recovery-followups/integration.{ctrf.json,trx}` |

Integration's seven skipped cases are its explicit retained-evidence, live recovery, four general Aspire/Dapr and production-browser opt-ins. They are unexecuted infrastructure evidence. The required live recovery method was invoked separately with `HEXALITH_CHATBOT_TIER3=1`, `HEXALITH_CHATBOT_TIER3_REQUIRED=1` and zero skips required.

That separate live attempt exited 1: one test executed, zero passed, one failed, zero skipped, 76.164 seconds. It reached the actual Aspire/Dapr topology but mailbox admission returned HTTP 403 `authorization_denied` before any recovery drill, matching the prior dependency-unblock run. The corrected actual CLI invocation then exited 0 and wrote the metadata-only failed-attempt summary with the one-failure counters. Exact commands and paths are in the evidence JSON; raw live diagnostics remain local under `TestResults/five-recovery-followups`. Aspire confirmed no AppHost running after disposal.

The five selected ledger entries now record their implementations and ordinary-suite evidence. The spec remains in-review because `docs/story-evidence-integrity.md` requires successful recovery-primary for a terminal transition on these paths. No completion TRX/provenance, product-story/sprint transition, A10 approval, dependency update or submodule change is claimed. Commitlint and the CRLF-aware whitespace check pass. The user authorized committing and pushing these task changes to main.
