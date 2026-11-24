---
title: 'Retain recovery evidence after cancellation during a write'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

A recovery deadline can interrupt a canonical evidence write and leave the external gate without a retention diagnosis. After a failed primary write, continuity, projection rebuild, and scoped outage each get a bounded attempt to retain an unmeasurable report, followed by the existing independent marker if the fallback fails. Cancellation still stops domain execution and audit and alert delivery.

Given a completed measurement whose primary write cancels the caller, when each coordinator handles retention, then a successful fallback retains the unmeasurable report and an unsuccessful fallback emits exactly one marker.

</frozen-after-approval>

## Implementation Notes

The three coordinators share a two-second fallback budget. The existing marker schema, workflow staging and replay semantics handle both ordinary and cancelled retention failures.

## Review Triage Log

- Patched. The primary wait observes the caller deadline even when the sink remains pending.
- Patched. Replay reports invalid canonical artifacts alongside valid retention-failure markers and rejects the attempt.
- Patched. The coordinators observe cancellation before a scenario and before alert delivery.
- Patched. A regression leaves a fallback pending through its deadline and checks that the caller resumes.
- Covered by coordinator and replay regressions. The producer tests exercise cancellation with both fallback outcomes. The workflow-shaped replay checks an incomplete attempt and partial canonical output alongside retained markers. This scope does not claim a hosted drill.
- Preserved the replay error contract. A malformed canonical pair still raises its original validation error when replay finds no valid independent marker.
