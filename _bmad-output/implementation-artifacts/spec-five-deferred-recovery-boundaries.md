---
title: 'Harden five deferred recovery-validation boundaries'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
baseline_commit: 'b4046c8041aad87505fbc4c0ebfc20c5cf0b9f33'
review_loop_iteration: 0
context: []
dependency_followup_baseline_commit: 'a402e00d3b35dc8772d7dbea0a46d53fadee2ad1'
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

## Dependency follow-up review (2026-10-28)

The user explicitly requested the supported dependency workflow, original Debug project validation and recovery-primary, with owner-first commits/pushes. The original production hardening is preserved. The previous `/tmp` harness results above are historical; the original-project results below supersede their build blockers.

The resumed cumulative review also examined the already committed hardening. These findings predate this dependency-only follow-up; they do not justify reverting or rewriting the preserved implementation. They remain review limitations rather than completion claims.

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Caller cancellation arriving during successful independent cleanup can return clean | medium / deferred | `ProbePairInternalAsync` does not recheck the caller token after its finally block. Pre-existing at the follow-up baseline; cancellation semantics need a separate production change. |
| A seed write that commits then fails leaves its nonce untracked | medium / deferred | `seeded.Add` follows `PutAsync`. Covered by the existing synthetic-sentinel lifecycle follow-up; carried limitation. |
| Overall monotonic success can mask later failed-pass alerts after rollback | medium / deferred | `CheckHealthAsync` still compares failure and success wall-clock timestamps. Pre-existing consumer interaction needs latest-attempt semantics and coordinator coverage. |
| A presence window plus closing read can exceed a restoration interval | medium / carried | The earlier triage explicitly retained an independently request-bounded strict closing read; some scenario call sites lack an enclosing restoration token. No hard operation-wide deadline claim is made here. |
| Regressed M2 completions can consume a cadence partition | medium / deferred | `RunM2SweepAsync` commits its partition before recording the status snapshot. Pre-existing coordinator interaction needs separate retry/cadence design. |
| Cancellation-ignoring deletions can outlive the wait budget | medium / carried | The existing sentinel-lifecycle follow-up covers abandoned cleanup and retained records; bounded waiting does not supervise the store's operation lifetime. |
| Encoded literal percent artifact names are refused | low / rejected | Rejecting all residual percent characters is the explicit conservative nested-encoding rule. No general arbitrary-filename acceptance is claimed. |
| Dormant absolute evidence paths can contain NUL | medium / deferred | `Path.IsPathFullyQualified` is not full filesystem-path syntax validation. Pre-existing shape-validation limitation needs a separate regression. |
| Later seed/read failure discards an already observed cross-tenant leak locator | medium / deferred | The outer generic catch reports Unknown even after an earlier observation. Pre-existing behavior; preserve strict fail-closed status while designing evidence retention separately. |
| Historical harness cannot be reproduced from tracked construction inputs | medium / resolved | The supported checked-in wrapper now builds and runs the original Server and Integration projects; their results replace the diagnostic harness for this follow-up. |
| Synchronously blocking invalidation can exceed cleanup budget | medium / deferred | `InvalidateAsync` is invoked before `AsTask().WaitAsync`; a synchronous block never reaches the bounded await. The review's Task.Run snippet was a proposed guard, not existing code. |
| Live options-to-probe bounds wiring had no assertion | medium / patched | The actual live `durableState` now asserts both bounds equal `options.RestorationTimeout`; standalone constructor tests remain covered by Integration. The failed live attempt did not reach these assertions. |
| New wrapper refusal branches needed executable verification | medium / patched | `SourceMappingWrapperTests` uses a fake producer and execution marker for eight refusals plus valid environment/child-exit forwarding. It is included in the existing CI unittest discovery file. |

The dependency diff received a fresh blind review and edge-case review with no actionable findings. The verification reviewer requested the wrapper regression coverage above.


## Supported dependency build workflow

The local tool manifest pins `hexalith.builds.module.cli` 4.38.1, the released preparation command already selected by Conversations. `module/build.module-manifest.v2.json` is build-only and makes ChatBot the active owner. `.github/scripts/with-source-mapping.py` restores and verifies that exact pin, invokes `prepare-build --mode source` into a fresh ignored workspace, validates the returned Debug environment, and passes the producer's five imports/hash to the ordinary command and descendants without shell evaluation. It retains imports for detached child builds.

The duplicate excluded UniqueIds package was removed from the source-mode Server tests. Server no longer forces its two Memories edges into package mode. AppHost and Integration stop forwarding reserved Commons-root metadata inside a mapping; their nonempty-root standalone paths remain. Folders keeps its old Memories fallback outside mappings; Projects keeps standalone Version/PackageVersion isolation outside mappings; EventStore keeps its standalone source-version forwarding outside mappings. No producer mapping, duplicate/source/configuration guard, warning policy, library package pin or nested checkout was weakened.

Owner commits were pushed to `main` before parent gitlinks were staged:

- Folders: `56550414c58db9b2d46a1a8b45fc77a3156cbee0`.
- Projects: `dd2b871bac16fbba62703a14ab32d9ba9b579c8f`.
- EventStore: `50221cf7dbe425becc333f6757ba37b93a22ab4e`.

Folders and EventStore remote main had advanced. Their fixes were rebased without conflicts onto `9141b99b0bf232feb76d9c0aa9f3e401fc64acc5` and `717a06e16c3afd81b904eaf4de646b74b13262d2` respectively, preserving remote work. The final original-project builds and tests use those pushed owner heads. Nested submodules were neither initialized nor updated.

Source preparation retains two existing advisory findings: HXW011 (the direct Platform commit is untagged) and HXW009 (workspace Builds differs from Platform's pinned catalog). Source mode permits these; package qualification is not claimed. The generated mapping still refuses a Release invocation with HXW006. Independent standalone MSBuild item evaluations verified all three owners retain their prior metadata when no mapping is supplied; results are in `TestResults/recovery-unblock/dependency-checks.json`.


## Original-project validation (final pushed dependency heads)

All commands ran from the ChatBot root on .NET SDK 10.0.402 in Debug. No diagnostic/shadow project, `BuildProjectReferences=false`, warning suppression, audit override or nested Builds import was used.

| Exact command | Result | Retained local output |
| --- | --- | --- |
| `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1 --nologo` | Exit 0; 0 warnings/errors; final build 42.06s | `/tmp/chatbot-recovery-unblock-server-build-final-head.log` |
| `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug -m:1 --nologo` | Exit 0; 0 warnings/errors; final build 94.53s | `/tmp/chatbot-recovery-unblock-integration-build-final.log` |
| `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests TestResults/recovery-unblock/server-final.ctrf.json -parallel none -result-trx TestResults/recovery-unblock/server-final.trx` | Exit 0; 3,520 passed, 0 failed/skipped; native runner 4.0.2 | `/tmp/chatbot-recovery-unblock-server-tests-final.log` |
| `python3 .github/scripts/with-source-mapping.py bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests TestResults/recovery-unblock/integration-final.ctrf.json -parallel none -result-trx TestResults/recovery-unblock/integration-final.trx` | Exit 0; 453 passed, 0 failed, 7 opt-in skips; native runner 4.0.2 | `/tmp/chatbot-recovery-unblock-integration-tests-final.log` |
| `python3 .github/scripts/tests/test_story_evidence_guards.py SourceMappingWrapperTests -v` | Exit 0; 2 tests passed, including 8 refusal subcases | `/tmp/chatbot-recovery-unblock-wrapper-tests.log` |

Integration's seven ordinary-lane skips are the retained-evidence replay, the required live-recovery test, four `TrivialGovernedCommandAspireE2eTests` cases and the production-browser matrix. These are unexecuted in that ordinary invocation, not passing infrastructure evidence. The required live method runs separately with Tier-3 required and zero skips.

The cumulative original hardening and dependency follow-up were independently reviewed. New dependency review findings are resolved; six separate pre-existing follow-ups are recorded in the ledger, with carried sentinel-lifecycle and final-read limitations retained. Status stays `in-review` until the required live evidence passes.


## Required recovery-primary result and remaining blockers

Exact final invocation, after both final original test assemblies completed:

```bash
HEXALITH_CHATBOT_TIER3=1 HEXALITH_CHATBOT_TIER3_REQUIRED=1 HEXALITH_CHATBOT_RECOVERY_EVIDENCE_ARTIFACT=completion-recovery-evidence HEXALITH_CHATBOT_RECOVERY_RETENTION_FAILURE_DIR=/home/administrator/projects/hexalith/chatbot/TestResults/recovery-unblock/retention-failures-final HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES=250 ChatBotServiceGrants__LifetimeDays=90 XUNIT_REQUIRE_ZERO_SKIPS=1 python3 .github/scripts/with-source-mapping.py timeout --signal=INT --kill-after=15m 265m bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests TestResults/recovery-unblock/raw-recovery-results-final/live-recovery-validation.ctrf.json -method Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests.LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate -result-trx TestResults/recovery-unblock/raw-recovery-results-final/live-recovery-validation.trx
```

Exit 1: exactly one test executed, 0 passed, 1 failed, 0 skipped, 71.670s. The earlier attempt also executed exactly one test and failed at the same boundary (72.915s). Both started the real Aspire/Dapr topology. The final run used the three published dependency heads above; it reached real mailbox bearer acquisition and readiness but `AssertMailboxTokenAdmissionAsync` received HTTP 403 `authorization_denied` from `/api/v1/commands`, before any continuity, rebuild or scoped-outage drill. Neither run reached the newly added live-bound assertions. This is an authorization/provisioning blocker, not the former Conversations or assembly-duplication build failure. Do not accept 403 as admission proof or relax the guard.

Final raw diagnostics: `/tmp/chatbot-recovery-unblock-primary-final.log`; raw CTRF/TRX: `TestResults/recovery-unblock/raw-recovery-results-final/live-recovery-validation.{ctrf.json,trx}`. `aspire describe --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json` confirmed no AppHost running after disposal. Existing shared Dapr containers were not uninstalled. Local CLI/runtime observations were Dapr 1.18.2/1.18.7; this is not the hosted pinned-runtime qualification.

Additional exact evidence-tool results:

- `dotnet build tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Debug -m:1 --nologo` — exit 0; 0 warnings/errors.
- `dotnet tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll summarize-recovery-attempt --input TestResults/recovery-unblock/raw-recovery-results-final/live-recovery-validation.trx --outcome failure --output TestResults/recovery-unblock/recovery-attempt-summary-final.json` — exit 2, `status_mismatch` / `cli-command`. `Program.Main` has the handler and CI calls it, but `CommandArguments.AllowedOptions` omits the command. Recorded as a separate existing tooling blocker; output: `/tmp/chatbot-recovery-unblock-attempt-summary-final.json`.
- `dotnet tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Debug/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll sanitize-recovery-trx --input TestResults/recovery-unblock/raw-recovery-results-final/live-recovery-validation.trx --output TestResults/recovery-unblock/recovery-primary/live-recovery-validation.trx` — exit 2, `machine_results_invalid` / `recovery-sanitize`, correctly refusing a failed test. Output: `/tmp/chatbot-recovery-unblock-sanitize-final.json`. No canonical recovery-primary TRX or provenance was minted.

The spec remains `in-review`; no terminal transition, release qualification or A10 approval is claimed. The committed `evidence/five-deferred-recovery-boundaries.json` now includes the reproducible supported commands, exact final counters, owner revisions, TRX checksums and remaining blockers alongside the historical diagnostic evidence. Commitlint and whitespace validation pass; each owning repository was committed and pushed before staging its parent gitlink.
