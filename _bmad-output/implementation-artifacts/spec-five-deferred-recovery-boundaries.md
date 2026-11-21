---
title: 'Harden five deferred recovery-validation boundaries'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
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

## Review Triage Log (2026-11-21 resumed review)

Resumed `in-review` against current `main` (`c323546`). The staged diff is every superproject change since `baseline_commit` `b4046c8041aad87505fbc4c0ebfc20c5cf0b9f33` (106 later commits). Findings about later shared-CI migration, nightly recovery split, and platform upgrades are not caused by the five-boundary hardening.

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Legacy `ci.yml`/`release.yml` still invoke deleted `run-xunit-v4.sh`, `install-dapr-cli.sh`, `build-release-configuration.sh`, `run-merge-test-lanes.sh`, and `project-recovery-results.sh` | medium / deferred | Those scripts are absent from `.github/scripts/`; the workflows still call them. Caused by later shared-CI migration (`bf0f44f`), not this spec's recovery-boundary sources. Already tracked as R21-D1 / 2026-11-21 CiCd adoption. |
| Workflow `-method` filters still name `Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests` (and `E2E` casing) | medium / deferred | The `[Fact]` now lives in `tests/Hexalith.ChatBot.Recovery.Tests/LiveContinuityAspireE2eTests.cs`. Same later Fact move as R21-W1. |
| Nightly recovery hook is not enabled (no ChatBot `.github/hexalith-ci.json`, no evidence upload, in-process deadline only on the composite) | medium / deferred | `.github/actions/nightly/run-recovery.sh` exists; README keeps the hook disabled until the 330-minute allowance. Same later enablement work as R21-W1/W2/W3. |
| Specs stay `in-review`; sprint-status omits this oneshot; no `validation/five-deferred-recovery-boundaries.json` | false / rejected | Status `in-review` is this spec's recorded completion state after live mailbox HTTP 403. Adding sprint-status or a validation JSON would edit process artifacts, not the five-boundary product code. |
| Tracked evidence JSON cites deleted harness paths, `net10.0`, and disagrees on source SHA-256 after SDK 11 | low / rejected | `evidence/five-deferred-recovery-boundaries.json` labels itself historical diagnostic evidence. Later SDK/source drift is expected; rewriting historical evidence is not a direct product correction. |
| `tools/Hexalith.ChatBot.StoryEvidenceGate` is deleted while legacy workflows still build and run it | medium / deferred | No remaining StoryEvidenceGate project; `ci.yml`/`release.yml` still name the csproj and `net11.0` test runner. Same shared-CI producer retirement as R21-D1. |
| ADR still documents both the nightly `Recovery.Tests` sweep and the retired TE-2 / Dapr 1.18.0 / `RecoveryTrxSanitizer` path | medium / deferred | `docs/adrs/live-recovery-validation-drivers.md` line 147 vs 153. Documentation lag of the same later migration; the 2026-11-21 CiCd adoption entry already asks to update the ADR scheduler bullet at delivery. |
| `release.yml` `bypass_stop_ship_gates` can publish when topology or live-recovery jobs fail | low / rejected | Dispatch-only, default false; the workflow comments that push publication still requires green gates. Changing that override is a later release-policy decision, not a five-boundary defect. |
| `IsSafeArtifactLocator` rejects hyphenated names such as `artifact:reports/access-token/value` | false / rejected | Hyphen is a token bound; `RecoveryValidationEvidenceManifestTests.ArtifactLocatorRejectsSensitiveTokensAndUnsafeUriShape` requires that locator to fail. Matches the frozen intent to reject sensitive bounded path tokens while accepting substrings (`tokenization`, `passwordless`). |
| Residual `%` after one unescape still refuses percent-bearing artifact names | carried low / rejected | carried The nested-encoding rule is unchanged at `RecoveryValidationEvidenceManifest.cs:399-403`. No general arbitrary-filename acceptance is claimed. |
| Isolation cleanup: `Task.Run` + `WaitAsync` abandons cancellation-ignoring `InvalidateAsync` when the five-second budget expires | carried medium / deferred | carried Bounded waiting still does not supervise the store operation lifetime (`DerivedStoreIsolationProbeCoordinator.cs:134-143`). Same sentinel-lifecycle limitation as the original deferred row. |
| `seeded.Add` still runs only after `PutAsync` succeeds | carried medium / deferred | carried `ProbePairInternalAsync` still records the nonce after the put (`:104-107`). Same synthetic-sentinel lifecycle follow-up. |
| Cleanup timeout during a later seed/read exception overwrites a witnessed leak | false / rejected | When the caller is not cancelled, the outer catch at `:167-177` re-verifies `observableThroughIntruder` and keeps the leak. The follow-up already patched this. |
| Nonce-scoped orphans have no reaper after failed deletion | carried medium / deferred | carried Fast cleanup still fails closed; fencing stays; no bounded-storage claim is made. Same original deferred lifecycle policy. |
| `EventStoreDurableStateProbe` copies `RestorationTimeout` onto both presence and HTTP timeout | false / rejected | That bind is DW-41. The options constructor sets `_presenceWindow` and `_client.Timeout` from `RestorationWindow(options)` (`EventStoreDurableStateProbe.cs:34-38`). |
| A presence window plus an independently request-bounded closing read can exceed a restoration interval | carried medium / carried | carried `ReadActorStateAsync` still uses `CancelAfter(RequestTimeout)` linked to the caller token (`:354-355`). Some scenario call sites still lack an enclosing restoration token. No hard operation-wide deadline is claimed. |
| Live constructor assertions sit after mailbox admission, so the recorded 403 runs never executed them | carried medium / patched | carried `LiveRecoverySweep.cs:255-258` still asserts both bounds equal `options.RestorationTimeout`. The live 403 is the documented remaining completion blocker, not a missing assertion. |
| Dormant `EvidenceDirectory` can contain NUL | false / rejected | `Validate` rejects `Path.GetInvalidPathChars()` for enabled and dormant absolute paths (`LiveRecoveryValidationOptions.cs:143-145`). NUL is in that set. Broader Windows component legality remains on the follow-ups spec, not this row. |
| `LastPassSucceeded` is an init-only extra property, so other stores drop it | false / rejected | `IPeriodicEnforcementStatusStore` is internal. The only implementer is `InMemoryPeriodicEnforcementStatusStore`, which sets the property on every `Read()`. No second implementer exists to diverge. |
| `_lastSchedulerAlertByReason` is not reset by a successful pass | false / rejected | A recovered pass with successful M2 completions removes `periodic_enforcement_pass_failed` (`PeriodicEnforcementCoordinator.cs:246`). Fail-success-fail re-arm was resolved in the runtime-guards follow-up. |
| Story Completion criterion 4 has no passing live citation | false / rejected | The spec already records the live 403 and stays `in-review` for that reason. Not a new product defect. |
| `require-runtime.py` hard-codes `tests/.../net11.0/.playwright/node/linux-x64/node` and requires `GITHUB_ENV` | medium / deferred | `.github/actions/after-unit-tests/require-runtime.py:14-24,40`. Later CI hook; non-linux or missing `GITHUB_ENV` fails before fail-closed live tests. Not part of the five-boundary sources. |
| M2 release-gate `FixedTimeEquals` throws on length mismatch and returns 500 | false / rejected | `ChatBotM2ReleaseGateAuthenticationHandler.cs:36` calls `FixedTimeEquals` once. Unequal lengths return false; the handler returns `AuthenticateResult.Fail`, not an untyped 500. |
| Attachment index extra keys make recovery cleanup erase the index and leave set rows | false / rejected | `GetIntakeReadModelKeysAsync` does not iterate `AttachmentKeys` or throw `foreign attachment`. Canonical set and index keys are already in `IntakeReadModelKeysFor` roots (`:23-30`). |
| Inventory `GetAsync` hangs ignoring cancellation because the inventory deadline is missing | false / rejected | There is no `inventoryDeadline` / `inventoryBudget` in this store. The cited snippet is not in the current method. |
| Caller cancel after a witnessed leak discards the breach as `OperationCanceledException` | false / rejected | After independent cleanup, `:154` and `:181` rethrow the caller token. `SweepAllTenantPairsAsync` does not catch it, so a cancelled probe cannot look like a successful zero-breach sweep. |
| Mailbox bearer payload that is not Base64 JSON fails with an unclassified parse exception | medium / deferred | `RecoveryMailboxOwnerProvisioner.AssertMailboxTokenIdentity` (`:148-154`) has no catch around `FromBase64String` / `JsonDocument.Parse` / `GetProperty`. Later live harness, not this spec's production boundary. |
| EventStore status or admin query exceeding the 30s `HttpClient` timeout aborts the two-minute command wait | false / rejected | `SubmitChatBotGrantAsync` catches `TaskCanceledException` when the caller token is live and continues until `CommandBudget` (`:204-210`). |
| `with-source-mapping.py` crashes with a traceback on invalid JSON | false / rejected | No `JSONDecodeError` handler exists in that script; the cited branch is not present. |
| `resolve-published-builds-tooling.sh` exports a bogus root when `_ChatBotPublishedToolRoot` is empty | false / rejected | An empty root fails the assembly `[[ -f ]]` check and exits 1 (`resolve-published-builds-tooling.sh:7-9`). Nothing is printed on that path. |
| `RecordM2SweepSucceeded` changed from `void` to `bool` | false / rejected | Internal store, single implementer, updated with the clock-regression snapshot. Not a public production contract break. |
| Aspire SDK 13.5.3→17, Dapr 1.19.1, `net10.0`→`net11.0` pin moves | false / rejected | Those pins moved in later main commits. The five-boundary sources do not change them. |
| Live `RestorationTimeout` probe wiring is only asserted in a Fact no enabled lane runs | medium / deferred | Pre-verified. The live construction assertions exist (`LiveRecoverySweep.cs:255-258`); `EventStoreDurableStateProbeTests` covers the options constructor. The Fact now lives only in ungated `Recovery.Tests` (R21-W1). Same later lane split, not a missing bind. |
| `RetainedLiveRecoveryEvidenceShouldPassTheReleaseGateOutOfProcess` was deleted while CI still selects it | medium / deferred | Pre-verified. The method is gone from `LiveRecoveryEvidenceGateReplayTests`; `ci.yml:628` and `release.yml:280` still name it. Same later producer/consumer split as the deleted scripts. |


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
