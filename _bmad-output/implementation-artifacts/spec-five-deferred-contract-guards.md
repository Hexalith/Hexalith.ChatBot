---
title: 'Resolve five deferred contract and recovery guards'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
baseline_commit: 'a26117d0c8cda489216f391280c406507d044170'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve five deferred maintenance items: DW-25's source-based enum/sidecar topology checks; DW-34's source/prose-based recovery authority guards; DW-46's missing subscription reconciliation coverage across restore cycles with cumulative counters; DW-88's permissive nested enum collections and enum dictionary keys; and DW-89's generated-client freshness verification. Replace fragile guards with executable behavior or compiled dependency checks, preserve recovery identity isolation and named enum wire values, independently review and verify the result, reconcile the deferred ledger, and commit and push to main as requested. Where an item already has the required implementation, verify it and record that evidence rather than duplicating it.

</frozen-after-approval>

## Implementation Notes

Investigation identifies bounded ChatBot-owned corrections without unresolved intent gaps, new public APIs, dependency changes, or irreversible application actions. The explicitly requested five-item batch uses the oneshot route. Product-story completion, recovery qualification and deployment are outside these maintenance closures.

DW-25: replace sidecar-specific source assertions in tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs with builder-model tests in tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs. Inspect real Dapr annotations, component references and absence of a UI sidecar using the existing non-started testing builder. Preserve unrelated config/security assertions. Existing Client.Tests/StrictEnumWirePolicyTests supplies executable enum checks.

DW-34: replace ProductionServer_ShouldNotReferenceAspireResourceLifecycleAuthority and FaultControlTypes_ShouldNotLeakIntoContractsDomainOrUi in tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs with compiled dependency rules using FitnessAssemblies/FitnessRule. Preserve the existing rule-machinery negative proof and add specific detection coverage where needed. Document the exact enforcement in docs/adrs/live-recovery-validation-drivers.md; do not claim all workflow/document checks can be replaced by IL.

DW-46: retain the simulator's cumulative telemetry. Extend tests around AspireRecoverySandboxOperations.ReconcileSubscriptionAsync to run two restore/reconciliation cycles, retaining prior counters and asserting fresh durable identities, actual recovered count and duplicate suppression. Reuse existing fake EventStore/read-model test seams; stale cumulative success must never stand in for current recovery.

DW-88: keep StrictStringEnumConverter semantics, including named case handling and nullable null acceptance. Extend StrictEnumContractResolver to handle nested enum collections at contract level and enum dictionary keys on read/write without accepting numeric key text or undefined values. Preserve existing NSwag property/item converter overrides and common dictionary interfaces, with focused regression tests. Put new C# types in dedicated files.

DW-89: ClientGenerationTests.IsolatedNswagGenerationShouldMatchCheckedInOutputByteForByte already copies the contract/config to an isolated directory and invokes restored NSwag. Run that test against the unchanged checked-in output and close the stale ledger entry with its source commit and current verification.

Validation uses repository Debug source mapping for AppHost, Architecture and Integration builds, individual native xUnit suites with zero-skip enforcement, Client tests including isolated generation, whitespace and commitlint. Record exact commands/results and runtime blockers in a local-maintenance verification JSON. Do not weaken existing package or runtime gates. Keep the maintenance spec in-review if the repository evidence gate does not authorize formal completion.

The baseline source-mapped AppHost Debug build succeeded with zero warnings/errors after serializing the project graph. The task-owned no-build Aspire run started, was inspected (44 resources: 26 Running/Healthy, 17 Waiting, one Starting), and stopped successfully. Initial unmapped startup lacked Conversations source mapping; an initial mapped parallel build hit a shared-output file lock. No dependency pins were changed to recover either condition.

Client Debug build passes with zero warnings/errors; the full native suite passes 122/122 without skips, including 27 recursive enum cases and isolated NSwag byte-for-byte regeneration. The generated file remains unchanged. Architecture Debug build passes; the full native suite passes 165/166, with all 27 recovery architecture/rule-machinery cases passing. The sole failure is the pre-existing unchanged FrontComposer Admin.Abstractions 3.128.2 override versus the shared catalog 3.128.3, already recorded by spec-five-deferred-validation-limits.md. This is retained as a broad-gate blocker, without changing dependency authority or claiming an entirely green architecture suite.

Final corrections bind effective state-store metadata to each named component and preserve Admin UI exposure/service dependencies. The replay regression retains one provider recovery phase across restores: first-cycle success precedes a missing-current replay or deliberately injected duplicate/cross-tenant writes. Durable breach evidence remains append-only while cleanup verifies derived projections and duplicate-probe absence. Strict enum-key diagnostics retain the original response path and line; default initialized/get-only dictionary population, direct enum values, alias keys and malformed/commented JSON are covered. A failing leading-comment regression exposed a converter entry-token defect, which was corrected before the final Client run.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Named component isolation metadata remains source-count based | medium / patched | Actual toolkit configuration callbacks are executed; each named store's keyPrefix, actor setting, Redis endpoint and type are asserted. |
| Enum-key errors lose the response path/line | low / patched | Key conversion wraps JSON errors with the original reader location; a nested invalid-key regression asserts exact path/line. |
| Direct enum values in enum-keyed dictionaries lack read coverage | low / patched | Direct nullable/non-nullable values reject integers, numeric text and empty text; named values and explicit null round-trip. |
| Existing dictionary population path lacks coverage | low / patched | Writable/get-only initialized dictionaries preserve their instance and prior entries, merge named values and refuse invalid keys. |
| Dictionary reader structural/comment handling lacks coverage | medium / patched | Eight malformed/nonobject cases fail within the JSON exception family; leading/interior/trailing comments are accepted. The leading-comment entry defect found by the test is fixed. |
| Dictionary keys lack casing/CLR alias regression | low / patched | Case and CLR aliases deserialize and serialize back to canonical wire names. |
| Hosting namespace guards lack an additional host-specific negative fixture | low / rejected | Existing compiled namespace failure machinery and four real recovery-type detection controls prove matching. No production assembly has a permitted hosting consumer; an extra fixture would introduce synthetic types/dependencies without a demonstrated guard defect. |
| Current-cycle cross-tenant leakage lacks a negative case | medium / patched | Both concrete operations report second-cycle leakage for the current durable identity while prior-cycle control identities remain absent. |
| Removed Admin source checks lack equivalent model coverage | medium / patched | Actual external Admin UI HTTP, exact wait targets and endpoint-reference annotations are asserted. |
| A second clean cycle assumes a new provider message despite fixed replay identity | medium / patched | Unrealistic second-cycle clean success was removed; the same recovery phase now exercises ordinary deduplicated missing-current behavior or deliberately injected forbidden writes. |

Independent correction recheck found no remaining concrete defect. No review finding was deferred.

## Verification

Final Client Debug build and full native suite pass 141/141, including 46 recursive enum cases and isolated NSwag regeneration. Source-mapped full Integration Debug build and final focused rebuild pass; three original recovery/topology classes pass 90/90. AppHost test build and suite pass 18/18. All these suites have zero failures or skips. Architecture Debug build passes and its full suite reports 165 passed, one pre-existing catalog-version failure, zero skips; the recovery architecture/machinery subset is 27/27. Exact commands, report hashes, source hashes and the broad-gate failure are retained in validation/five-deferred-contract-guards.json.

All five maintenance items are implemented or verified as already implemented, reviewed, and reconciled in the ledger. This maintenance spec stays in-review under the repository story-evidence integrity rules; no product-story, sprint, recovery qualification or release completion is proposed. No dependency or submodule changes are included.
