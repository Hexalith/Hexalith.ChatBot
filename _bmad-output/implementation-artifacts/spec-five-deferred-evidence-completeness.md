---
title: 'Resolve five deferred recovery evidence completeness items'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
baseline_commit: '4697c67b6c1d72aed07a27f121c9b0be34e80bce'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five related deferred evidence-maintenance items: DW-61 required residual coverage, DW-95 metadata-only scheduled/release recovery artifacts, DW-135 incomplete-attempt retention across coordinator failures, and the two TransitionDetector follow-ups from spec-five-deferred-validation-limits concerning duplicate-member inactive JSON and one discovery inventory per Detect invocation. Preserve strict selected-contract validation, active duplicate detection, discovery budgets, metadata-only output, truthful producer observations, original exceptions, bounded failure retention and existing release gates. Record scoped verification and close precisely these entries, independently review, commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

Investigation found no human-only intent gaps or irreversible application changes. These localized evidence-path corrections use the oneshot route. No dependency, submodule, public API, domain persistence or deployment configuration changes are required. Larger producer receipt contracts (DW-20/DW-36) and release-claim policy decisions (DW-62) remain outside this batch.

Code map: TransitionDetector.cs should lazily capture one bounded identity inventory per invocation and contain malformed duplicate-member candidate JSON without relaxing strict selected-contract loading. Test real multi-transition Detect invocations, inventory read counts, inactive malformed isolation and active identity ambiguity. LiveRecoveryValidationEvidenceGate.cs should own required residual sets, reused by FileRecoveryValidationEvidenceSink; explicit independently stated expected sets and omission tests prevent circular verification. The live test needs an outer coordinator failure-retention boundary preserving the independent summary root, bounded cancellation-independent writes and delivered alert observations. Extend its always-run filesystem contract tests for stage exceptions and retention failures. Both operational recovery workflows should stage raw CTRF/TRX outside uploaded TestResults, always retain the existing safe attempt projection, and sanitize successful TRX before upload. Preserve marker staging order and build the tool explicitly. Update relevant workflow tests and docs.

The required source-mapped Aspire baseline started successfully, inspection found 44 Running/Healthy resources, and the exact AppHost stopped successfully before edits. Existing HXW009/HXW011 mapping advisories were observed without changing pins or guards.

Verification will build Debug source references and execute original gate, focused Server/Integration/Architecture lanes and executable workflow guards. Retain exact commands, counters and source/result hashes in a tracked validation summary. These checks do not qualify live recovery, A10, package-only Release or product-story completion.

Implemented the five scoped corrections. The gate now owns required residual mappings shared by the evidence sink. Discovery builds one lazy, bounded inventory per Detect invocation, including story, technical-ledger, sprint-story and sprint-action selection, and ignores malformed inactive duplicate-member JSON. The outer live-attempt boundary retains incomplete summaries after any coordinator/setup/assertion failure using delivered alert snapshots and independent retention cancellation. Operational workflows build the evidence tool first, stage raw CTRF/TRX outside uploads and always project safe attempt metadata plus sanitized successful TRX. Existing marker/envelope retention and independent evidence gates are preserved.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Earlier passing TRX survives a later failed projection in the same root | medium / patched | Script clears only its two owned outputs; executable repeated-root regression proves failure retains no stale canonical TRX. |
| Best-effort summarizer exit code can hide absent summary output | medium / patched | Projection requires a fresh nonempty summary; blocked-output regression fails explicitly. |
| Safe-shaped arbitrary test identity reaches retained metadata | high / patched | Summary pins method/class to the actual recovery test and outcome to known tokens; attribute adversaries verify redaction. |
| Replacement boundary tests omit controlled-loss marker composition | medium / patched | Real marker sink and RunControlledLossAndRetainAsync execute inside the outer boundary; persisted marker and incomplete summary retain the same run and original exception. |
| Independent summary deadline lacks deterministic exhaustion coverage | low / patched | Zero budget cancels before any summary mutation, preserving primary exception and retention cancellation. |
| Late failure after a successful summary lacks coverage | medium / patched | Final-validation failure replaces prior success with incomplete state and observed delivered alerts. |
| Cache regression omits technical-ledger and sprint-action paths | medium / patched | One batch executes all four lookup paths with exact per-file counts and later-invocation ambiguity detection. |
| Active duplicate-member cases are absent | medium / patched | Four changed/selected identity/unrelated-member cases remain refused. |
| Workflow checks do not bind always-run projection, ordering or raw CTRF path | medium / patched | Extracted producer/projection/upload steps assert always(), order and runner-temporary raw CTRF placement. |
| Sink tests omit complete emitted residual-set matrix | medium / patched | All ten persisted scenario manifests are compared with independently stated expected sets. |

Independent correction recheck found no concrete remaining defects. No new work is deferred.

## Verification

Final gate tests pass 331/331 without skips. Full Server tests pass 3,535/3,535 without skips. Full Integration tests pass 484/491 with zero failures and the same seven opt-in live/retained/browser skips. Focused Architecture passes 21/21; full Architecture retains the separately recorded pre-existing FrontComposer catalog mismatch. Executable workflow guards pass 14/14, projection scenarios pass 6/6, actionlint and Bash syntax checks pass. Builds use Debug source references and preserve dependency pins and submodule contents.

The implementation batch is complete within its focused verification scope. This spec remains in-review under docs/story-evidence-integrity.md because terminal completion would require the exact current-run live recovery primary lane, which was not executed. No story, sprint, A10, production recovery or release qualification is advanced. The broad Architecture mismatch is not hidden or weakened. Exact commands, counters and source/result hashes are retained in validation/five-deferred-evidence-completeness.json.
