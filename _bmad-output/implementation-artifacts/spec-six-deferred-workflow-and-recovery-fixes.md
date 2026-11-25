---
title: 'Complete six deferred workflow and recovery fixes'
type: 'bugfix'
created: '2026-11-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '3ecda5ae2ad4c646a6b5bd3ebddf9a48e55d3e5e'
---

<frozen-after-approval>

## Intent

Implement the six highest-priority verified ChatBot-owned fixes that can be completed with the available repository and environment. Prefer production behavior, then recovery correctness and qualification. Use the ledger at the baseline revision for item references.

1. L1127. Keep a correction workflow's early terminal status when the gateway has yet to write its acceptance view. Carry the prepared receipt identity only after the SDK accepts submission. Seed and update the status atomically, preserve existing receipt reconciliation, and cover old workflow inputs.
2. L2253. Reap the nightly producer's private Redis if the producer is killed. Isolate the job's temporary directory and restrict bounded cleanup to that job's labeled Redis containers after the producer stops. Exercise the actual shell hook.
3. L1097. Bound topology smoke execution and all of its nested waits by one linked deadline that lets the job retain failure diagnostics.
4. L2180. Observe independent topology resources concurrently within the shared readiness budget. Preserve per-resource diagnostics, readiness checks, and failure behavior.
5. L2296. Qualify the rebuild's isolated read/write set with executable coverage, and make the ADR describe the guarantees the tests establish.
6. L2077. Make the nightly script define the in-process deadline relative to the outer timeouts for every invocation. Test the effective deadline and reject invalid overrides.

Add tests where applicable, mark each ledger item resolved, and commit each one separately. Record examined blocked and obsolete candidates. Run the full solution build and each test project, resolve failures within the batch, and push to main.

</frozen-after-approval>

## Implementation Notes

- The correction request carries acceptance timing and operation class only after SDK acknowledgement. Activity reports initialize matching status atomically. The coordinator leaves status publication to the activities for these requests and retains the original scheduling-acknowledgement behavior for older requests.
- The nightly Redis cleanup requires the producer's unique temporary scope and a Redis-specific role label. A real Docker run killed the producer and verified cleanup while both neighboring test containers stayed running.
- The live and release projects share their 25-minute budget across test methods. Rebuild coverage asserts independently specified baseline and fresh partition keys, including a fresh conversation notification read.
- Nightly recovery validates 230 through 250 minutes before provisioning. The in-process timer begins before the private backend and topology setup, ahead of the outer 280-minute interrupt.

## Review Triage Log

- Fixed the overly broad Docker selector, early failure overwrite, and new synchronous status write risk.
- Fixed the nightly override range, late start of the in-process timer, separate per-test release budgets and missing browser-matrix budget.
- Kept topology teardown cooperative and awaited. The outer runner bounds process lifetime if an external disposable fails to return. The regression verifies that cooperative cleanup finishes before results are reported.
- Replaced self-derived rebuild expectations with independently constructed keys and observed store end state.
