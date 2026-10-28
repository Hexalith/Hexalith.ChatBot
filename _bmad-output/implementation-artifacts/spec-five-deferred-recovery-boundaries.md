---
title: 'Harden five deferred recovery-validation boundaries'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
baseline_commit: 'b4046c8041aad87505fbc4c0ebfc20c5cf0b9f33'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-22, DW-23, DW-41, DW-68 and DW-72 as a small recovery-validation hardening batch. Validate explicitly configured non-secret sandbox, tenant and evidence-path shape while disabled, keeping empty disabled defaults valid and secrets activation-only. Accept benign artifact names containing sensitive-word substrings while rejecting sensitive bounded path tokens and encoded equivalents; preserve artifact-only, query-free, fragment-free and credential-free locators. Bind the live durable-state probe's materialization and HTTP bounds to the configured restoration timeout. Keep scheduler success timestamps and their associated evidence coherent when wall-clock time moves backward. Bound isolation-probe cleanup independently of caller cancellation, including stores that ignore cancellation, and fail closed on cleanup deadline expiry. Preserve cancellation, strict durable-state observations, audit-before-alert behavior, existing production interfaces and dependency pins. Verify focused regressions, independently review, record five ledger dispositions, commit and push the task changes to main as requested.

</frozen-after-approval>

## Implementation Notes

Investigation found no unresolved user-visible choices or irreversible implementation operations. Five localized fixes reuse existing configuration, evidence, scheduler and probe seams and their test projects; no public contract, persistence format, dependency or submodule changes are needed. The user explicitly requested the batch and push. This uses the oneshot route.

Code map: `LiveRecoveryValidationOptions.Validate` returns before dormant-shape checks; retain the default-empty configuration and activation-only secret requirement. `RecoveryValidationEvidenceManifest.IsSafeArtifactLocator` rejects sensitive substrings throughout a URI. `InMemoryPeriodicEnforcementStatusStore` overwrites both overall and per-job success timestamps; retain the matching per-job correlation/count snapshot and latest-attempt completion status. `DerivedStoreIsolationProbeCoordinator` awaits cleanup with `CancellationToken.None`; use one independent total budget and bounded asynchronous waits. `EventStoreDurableStateProbe` has one-minute presence and ten-second HTTP defaults; construct the live instance after options and pass its restoration bound. Preserve existing handler injection and final strict reads. Extend the corresponding existing tests and add focused store/cleanup fixtures only as needed.

Baseline: `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2. AppHost restore/build failed with two Conversations errors: `Conversations requires the Platform tool-selected source mapping; validation does not resolve dependency mappings.` No topology started. Exact output: `/tmp/chatbot-five-recovery-baseline.log`. Focused project builds and infrastructure-free tests will be attempted separately; a passing unit lane does not qualify live recovery or release readiness.


Implementation: dormant overlays now validate environment, replay tenant and supplied evidence paths/locators before the disabled return. Artifact locators decode escapes and reject bounded sensitive tokens, controls and nested encodings while accepting benign substrings. The live probe takes restoration options, bounds response-body reads and clamps poll delays to the remaining scheduling window; the strict closing read stays independently request-bounded, with the live caller's cancellation supplying the hard restoration cap. Scheduler success timestamps cannot rewind; regressed M2 completions retain the coherent success snapshot but remain ineligible for release. Isolation cleanup has one independent five-second total budget, bounds noncooperative awaits and reports both timeout and fast deletion failures as incomplete. Invocation-specific bounded synthetic sentinel identities prevent an abandoned delete from removing a later probe's evidence, even if callers reuse a correlation id. Normal cleanup still deletes the owned synthetic entries.

Normal build limitations: source-mode Server tests fail CS1704 duplicate UniqueIds and an uninitialized nested Conversations Builds import. Reusing dependency outputs through `BuildProjectReferences=false` still fails duplicate Memories/EventStore references or native Folders fixture references. Genuine tool-generated source mappings were attempted without changing dependency pins: the Conversations-owner map refuses ChatBot's root with HXW006, and the temporary root diagnostic map refuses the direct excluded Commons package/source overlap with HXW005. These gates remain unchanged. Commands and full results are retained in `/tmp/chatbot-five-recovery-*-build.log` and the two `/tmp/chatbot-five-recovery-*-source-mapping-20261028` preparation directories. The observed concurrent parent commit advanced only Builds before implementation; no user changes or submodule pointers are included in this task.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Regressed M2 completion could release older clean evidence after clock catch-up | medium / patched | Regressed completions now mark the latest attempt incomplete; gate regressions cover breach and zero-coverage attempts. |
| Late noncooperative deletion could erase the next probe's sentinel | medium / patched | Per-invocation synthetic nonces fence deletion; an overlapping-store regression reuses the same correlation and still observes the later breach. |
| Small presence windows oversleep the fixed poll interval | medium / patched | Poll delays clamp to remaining scheduling time. The documented strict closing observation retains its own request bound; the enclosing live cancellation remains the hard cap. |
| Response-body deadline lacked regression coverage | medium / patched | Immediate-header/cancellation-only-body fixture verifies the configured token reaches body reading. |
| Total cleanup budget was tested only on the first delete | medium / patched | Individually shorter successive invalidations exceed one shared deadline and cannot receive fresh per-delete budgets. |
| Caller cancellation and cleanup deadline were untested together | medium / patched | Combined cancellation/hanging-delete regression retains the original caller token after independent cleanup expires. |
| Cleanup timeout might overwrite an observed breach | medium / patched | Leaky-store timeout regression preserves the breach reason and offender locator. |
| Nonce-scoped orphan retention after failed deletion is unbounded across runs | medium / deferred | Fast cleanup failures now fail closed too; bounded retention/reaping or quarantine requires a separate lifecycle policy. Fencing stays, and no bounded-storage claim is made. |


Final verification: all 893 current Server source files compiled as the real `Hexalith.ChatBot.Server` assembly against prebuilt unchanged dependency DLLs, using the Web SDK and original logging generator: zero warnings/errors, no warning suppressions and no source shadows. The focused harness then compiled against that new assembly and executed all 101 tests with zero failures/skips, including five real gateway DI tests. The seven classes cover 28 manifest, 23 durable-state probe, 16 existing isolation, 15 options, ten cleanup, five DI and four scheduler store/gate cases. Source checksums were stable before compilation and after execution, and the copied Server assembly matched SHA-256 `af26e048924cf94a731f05a4559be1375f70554dd46a8712bf57be3be53b5b60`. This is diagnostic source validation, not the original module graph or package/live qualification. Full commands, results, source/dependency/binary checksums, CTRF and TRX are under `/tmp/chatbot-five-recovery-assembly-harness-20261028`; the committed summary is `evidence/five-deferred-recovery-boundaries.json`.

Exact passing commands:

- `dotnet build /tmp/chatbot-five-recovery-full-server-20261028/DiagnosticServer.csproj --configuration Debug -m:1 --nologo` — exit 0, zero warnings/errors.
- `dotnet build /tmp/chatbot-five-recovery-assembly-harness-20261028/FocusedAssemblyRecovery.csproj --configuration Debug -m:1 --nologo` — exit 0, zero warnings/errors.
- `XUNIT_REQUIRE_ZERO_SKIPS=1 .github/scripts/run-xunit-v4.sh /tmp/chatbot-five-recovery-assembly-harness-20261028/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-five-recovery-assembly-harness-20261028/final-focused.ctrf.json -parallel none -result-trx /tmp/chatbot-five-recovery-assembly-harness-20261028/final-focused.trx` — exit 0, 101 passed, zero failed/skipped; catalog-pinned runner 4.0.2.

Exact original test-build blocker: `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 --nologo` — exit 1, CS1704 duplicate UniqueIds plus missing nested Conversations Builds import. Its log is `/tmp/chatbot-five-recovery-server-baseline-build.log`. The separate original production build with `-p:BuildProjectReferences=false` exits 1 on duplicate Memories/EventStore assemblies; see `/tmp/chatbot-five-recovery-production-build.log`.

Completion disposition: DW-22, DW-23, DW-41, DW-68 and DW-72 are closed with the focused evidence above. The independent blind reviewer verified all seven original findings patched and the fast-cleanup failure regression; the synthetic-orphan lifecycle limitation has one explicit followup ledger entry. `docs/story-evidence-integrity.md` requires the actual recovery-primary lane for a terminal spec transition involving the touched recovery paths. Because the original graph prevents Aspire startup, this spec stays in-review. No terminal product-story, release qualification, sprint status, dependency or submodule pointer is changed. `git -c core.whitespace=trailing-space,space-before-tab,cr-at-eol diff --cached --check` passes, interpreting the required CRLF endings correctly.
