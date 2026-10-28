---
title: 'Resolve five deferred runtime and validation guards'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
baseline_commit: '9a032f439bb7499fd458fc7894f9fc2088d74c1c'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve five deferred maintenance items: DW-69's flaky serialized-enum assertion; rejected regressed M2 completions consuming their retry cadence; scheduler failure alerts remaining suppressed across a successful intervening pass; caller-relative release package directories changing meaning after the script changes directory; and sprint/technical-enabler ledgers allocating excessive line arrays before applying the 65,536-line ceiling. Preserve fail-closed status, ordinary repeated-failure throttling, exact package inventories, parser duplicate/fence behavior and stable refusal subjects. Verify and independently review the changes, record closure evidence, and commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

Investigation confirms five bounded root-owned corrections with no unresolved intent gaps or irreversible application actions. The requested five-item batch uses the oneshot route. No dependencies, gitlinks, public wire contracts, domain persistence, or product-story status changes are needed.

Code map: PeriodicEnforcementCoordinator.RunM2SweepAsync currently commits its private partition before InMemoryPeriodicEnforcementStatusStore rejects a regressed completion; make acceptance explicit at the internal status-store seam and only commit accepted completions. A rejected completion must remain failed and retryable after backoff, while preserving newer evidence and breach alerting. RunOnceAsync must re-arm only the pass-failure alert on successful completion, including success without an intervening health check. Use the existing coordinator/status tests and deterministic clocks.

pack-release-packages.sh must resolve its output directory from the caller's directory before cleanup and repository cd, including dash-prefixed paths. Exercise the real script from a temporary external directory with stub package production and validate cleanup, declared artifacts and arguments.

SprintLedgerReader and TechnicalEnablerLedgerReader currently split byte-bounded text without the existing line ceiling. Reuse a shared bounded text-line reader for local and FromText paths before parsing; preserve Markdown fenced-example exclusion and exact status/duplicate handling. Extend original gate tests with exact-limit and first-excess cases for LF, CRLF and final unterminated lines.

AiActionRiskClassifierTests must inspect serialized risk fields rather than search unrelated timestamps for digits. Include a deterministic timestamp containing 999 to demonstrate the original false failure while still asserting the invalid enum is absent.

Validation: required Aspire baseline start/inspection; Debug source-mapped Server build and original native xUnit suite; gate build and native xUnit suite; executable release script regressions; Bash syntax, whitespace and commitlint. Record exact commands/results and material blockers without promoting product-story, sprint, recovery qualification or release claims.


The exact source-mapped Aspire baseline started and was inspected: 26 Running/Healthy, 17 Waiting and one Running/Unhealthy resource. The task-owned AppHost stopped successfully. Existing HXW009/HXW011 source-mapping advisories remain; no live recovery qualification is claimed.

Implemented the internal boolean acceptance seam and delayed cadence commit; refused completions retain newer status, count as job failures, retain breach alerts and remain eligible after retry backoff. Pass alerts re-arm only after actual recovery, requiring enabled M2 latest attempts to have completed; backoff-only ticks preserve repeated-failure suppression. Packaging resolves literal caller paths once with CDPATH neutralized, covering the special '-' directory as well as spaces and dash prefixes. Its executable regression runs in both ordinary CI paths. Both local and Git-text ledger parsers now use the bounded shared line reader, retaining fenced-example exclusion. Enum serialization assertions cover both action arrays with a fixed timestamp whose fraction contains 999.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Backoff-only ticks clear pass-failure throttle | medium / patched | Recovery now requires completed enabled M2 attempts; same-instance fail/backoff/retry test preserves suppression through one hour, while successful intervening sweeps re-arm it. |
| Enum assertion omits nested input action classes | medium / patched | Both riskActionClasses and inputTuple.actionClasses are asserted empty; unrelated timestamp digits are permitted. |
| Packaging test absent from CI | medium / patched | Both ordinary exact-head and synthetic-merge jobs invoke the executable packaging test. |
| CDPATH and '-' alter output path normalization | medium / patched | Relative destinations become absolute literal paths before mkdir/cd; CDPATH is neutralized; real-script tests set CDPATH/OLDPWD and include a '-' directory. |
| Rejected completions omit job failure counters | medium / patched | Rejection records the evaluator failure, with counter increments on each rejected retry. |
| Rejected completion with unalerted breach lacks coverage | medium / patched | Regression theory uses a real tampered WORM chain with unavailable audit writer, checks both failure and unalerted-breach alerts, newer snapshot retention and retry. |
| Empty output directory becomes caller directory during normalization | medium / patched | Explicit refusal runs before mkdir/cleanup; negative real-script test preserves existing caller package and symbol files byte-for-byte. |

Independent correction recheck reports no remaining concrete defect. No review finding was deferred.


## Verification

Final Debug source-reference Server build passes with zero warnings/errors. Full Server suite: 3,540/3,540; focused scheduler/status/risk classes: 48/48. Debug evidence-gate build passes with zero warnings/errors; full gate suite: 344/344. Native xUnit runs have zero failures or skips. Executable script suites pass packaging 2/2 (five path subcases plus empty-input preservation), workflow guards 14/14, and recovery-result projection 6/6. Bash syntax, actionlint, CRLF-aware whitespace and commitlint pass. Exact commands, result checksums and changed-source hashes are retained in validation/five-deferred-runtime-guards.json.

All five scoped maintenance items are implemented, independently reviewed and verified. This maintenance spec remains in-review under docs/story-evidence-integrity.md because no formal completion contract or primary-path preflight is proposed. Product stories, sprint state, recovery qualification and release claims are unchanged. No dependency or submodule changes are included.
