---
title: 'Resolve five deferred UI presentation and lifecycle defects'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
baseline_commit: '43cd45ed7c91e9dcf3ee6c00d6d22c40e2d7daa5'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve five related deferred UI defects: DW-74 blocked-state heading hierarchy, DW-113 service-formatted audit history, DW-114 duplicated metadata-row rendering, DW-117 hard-coded composer/stream IDs, and DW-120 unobserved conversation subscription callbacks during teardown. Preserve localized English/French presentation, metadata-only audit reads, Fluent UI/FrontComposer composition, blank/redacted-field handling, authoritative typed re-query and recovery, stable per-instance accessibility references, and safe focus return. Verify the changes, independently review them, record precise closure evidence, and commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

Investigation found five bounded UI corrections without unresolved human-only intent choices or irreversible application changes. Use the oneshot route. No dependency, submodule, persistence, deployment or public transport changes are needed.

Code map: GovernedOperationService and OperationOutcome currently flatten audit entries into English strings; retain typed metadata and render localized labels/empty-history guidance at the view. Existing GovernedOperationServiceTests and ChatBotLocalizationContractTests must assert structured fields; VersionedSafeOutcomeRenderTests supplies actual page rendering. Generalize AssociationReviewRendering's existing Fluent row pattern into one shared component/renderer used by the five named surfaces, preserving optional values and culture-aware time markup. ChatBotBlockedState should accept a contextual heading level; the stream's empty state is subordinate to its H2. Give ChatBotGovernedComposer and ChatBotConversationStream stable instance IDs, and pass the composer's input identity into the workspace's focus-return target. Update real browser selectors and add repeated-instance/rerender tests. Make subscription callbacks awaitable end to end, guard disposed/stale subscriptions and queued workspace dispatch, and contain advisory callback failures; exercise callbacks, reconnect and disposal with the existing fake hub factory.

The source-mapped Aspire baseline started and was inspected: 26 Running/Healthy resources, 17 Waiting, and security Running/Unhealthy. The exact AppHost stopped successfully before edits. Existing HXW009/HXW011 mapping advisories are retained without pin changes. This baseline does not establish live-route readiness.

Verify Debug source-reference UI, actual component-rendering and relevant architecture lanes with the repository wrapper and native runner; retain exact commands/results and hashes. Do not advance product-story, sprint or release qualification without the required evidence preflight. Scoped rendering/lifecycle verification is distinct from live primary/browser acceptance.


Implementation retains structured metadata-only audit history and localized empty/reconciliation states. One shared Fluent row component now serves the five target surfaces and existing association helpers, preserving optional fields and UTC machine timestamps. Composer/stream titles and descriptions use stable unique instance IDs; DOM IDs have an alphabetic prefix for CSS selector compatibility, while initial announcement identity remains stable across remounts. Workspace subscription callbacks are awaitable, check disposal before queued dispatch, and rebind after an in-flight scope change. Subscriber startup is serialized, events check connection ownership, and cleanup cannot dispose a replacement connection. Current-run tests cover asynchronous callback failures, startup/disposal races, reconnect, replacement and actual mounted-workspace dispatch.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Connection published after disposal during factory creation leaks | medium / patched | Check lifetime after publication and dispose the owned connection; deterministic factory/disposal regression asserts no start and one dispose. |
| Overlapping startup joins/disposes replacement connection | medium / patched | Serialize subscription changes, bind join and failure cleanup to the initiating connection, atomically detach ownership; delayed success/failure startup cases preserve replacement. |
| Workspace skips changed project/scope while startup pending | medium / patched | Active subscription pass loops until its captured project, tenant and scope match current state; actual mounted-workspace delayed-start cases cover same/new tenant. |
| Random initial composer announcement key repeats retained outcomes after remount | medium / patched | Restore initial outcome announcement identity independently of DOM identity; real failure/receipt remount regressions share dedup state. |
| Two populated streams with identical item IDs duplicate child metadata IDs | medium / deferred | Existing item components derive IDs from ItemId; the current product mounts one stream and separately scopes attachment copies. This is broader than the ledger's hard-coded stream title/composer IDs and requires coordinated item-ID propagation. Recorded separately without claiming full repeated-feed isolation. |
| Empty committed audit history is described as pending | medium / patched | Distinguish Reconciling from Committed using the audit read's own typed status; actual en/fr page cases cover both. |
| Multi-composer browser stop/focus test missing | low / rejected | Existing real route browser test executes verified stop and asserts focus inside the actual Fluent textarea; repeated-instance render tests independently prove target/reference isolation. No production route mounts multiple composers; adding a new interactive host would exceed a simple correction for this test-strength suggestion. |
| Structured audit migration drops negative privacy assertions | medium / patched | Service-to-page en/fr tests seed omitted transport fields with private markers and assert they never reach the mapped model or rendered page. |
| Timestamp test bypasses caller formatter wiring | low / patched | Actual approval/why-panel renders cover en/fr display formatting, invariant datetime and optional timestamp suppression. |
| Workspace lifecycle test invokes private dispatch without subscription wiring | medium / patched | Additional real mounted-workspace tests deliver fake-hub changed/reconnected notifications through the actual subscriber and assert typed actions and disposal behavior. |


Review correction recheck found one remaining queued-recovery race: recovery could capture old tenant/callbacks before waiting for serialized admission. Verdict: medium, patched. Recovery now reads the current tenant/callbacks only after acquiring the subscription semaphore. Deterministic queued-recovery tests place a newer tenant subscription or same-tenant callback rebind ahead of recovery and assert that neither is rolled back.


Final independent correction recheck found no remaining concrete defect in the recovery change. One broader pre-existing populated-stream item-ID follow-up is recorded; no release or product-story claim is advanced.

## Verification

Debug source-reference builds of UI.Tests, UI.E2E.Tests, Architecture.Tests and IntegrationTests pass with zero warnings/errors. Full UI: 606/610 pass with the same four pre-existing Playwright catalog assertions (expected 1.64.0, evaluated 1.64.1), zero skips. The disclosed focused UI run excludes exactly those four methods and passes 606/606. Actual rendering: 111/111; Chromium live routes and localized failure/visual modes: 19/19; relevant architecture: 16/16. All passing focused lanes have zero failures/skips. Commitlint and the CRLF-aware whitespace check pass. Exact commands, source/result checksums and broad-failure details are retained in validation/five-deferred-ui-presentation-lifecycle.json.

All five scoped maintenance items are implemented, reviewed and verified. This spec remains in-review under docs/story-evidence-integrity.md because the full UI gate retains the disclosed catalog failures and no formal completion contract/preflight is claimed. Product stories, sprint state, A10 and release gates are unchanged. Generated browser screenshots are archived under ignored TestResults; existing tracked screenshots are preserved.
