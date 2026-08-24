# Story-Evidence Integrity Gate

The TE-2 gate is the repository-owned preflight for a proposed story or technical-enabler completion. Policy v2 reconciles identity and status, an exact root/root-submodule diff or immutable HEAD snapshot, the independent lifecycle event paths, the story File List, machine results and provenance, triggered primary paths, and every mandatory task/acceptance mapping. A pass authorizes a reviewable status edit; it never edits status itself.

TE-2 and the Epic 13 action remain non-complete until repository branch protection requires the check named `story-evidence-integrity`.

## Local preflight

Build and run the gate tests first:

```bash
dotnet build tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release -warnaserror
dotnet test tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj --configuration Release --logger "trx;LogFileName=story-evidence-gate.trx" --results-directory TestResults/story-evidence-gate
```

Create provenance for the contract's `current-run` TRX lanes, then validate. Attestation never creates or rewrites a `retained` sidecar. A contract that declares `recovery-primary` must use `current-run`, the exact `recovery-primary/live-recovery-validation.trx` and `.provenance.json` paths, and the matching `file:` locator. The hosted completion workflow produces that destructive lane conditionally, while a local preflight must produce the same sanitized live TRX itself:

```bash
dotnet run --project tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release -- attest --contract _bmad-output/implementation-artifacts/evidence/<story-key>.json --base <full-base-sha> --head <full-head-sha> --results TestResults
dotnet run --project tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release -- validate --story _bmad-output/implementation-artifacts/<story>.md --contract _bmad-output/implementation-artifacts/evidence/<story-key>.json --target-status done --base <full-base-sha> --head <full-head-sha> --results TestResults --report _bmad-output/implementation-artifacts/evidence/reports/<story-key>.json
```

The base and head arguments must resolve to exact full Git revisions. A root contract may use `$BASE` and `$HEAD` as explicit CI-bound revision sources because a tracked contract cannot embed the identifier of the commit containing itself. Reports and provenance always contain the resolved full revisions.

## Contract grammar

`story-evidence-policy.json` is the versioned policy. Each prospective transition has exactly one `_bmad-output/implementation-artifacts/evidence/<story-key>.json` contract with:

- explicit `storyKey`, `storyTitle`, `storyPath`, `sprintStatusKey`, `recordKind`, `targetStatus`, and `persistedStatus`;
- an explicit `diff` or `snapshot-plus-transition` scope mode; root and optional root-declared-submodule repositories; their revision sources; exact owned paths; independent transition paths; and the canonical implementation digest;
- one or more TRX lanes with result/provenance paths, an exact artifact locator, source (`current-run` or `retained`), `class:<fully-qualified-class>` and/or `method:<fully-qualified-method>` selectors, and skip policy;
- triggered/declared primary-path classes and their recognized primary lane;
- one mapping for every mandatory task and acceptance criterion, bound to current paths and/or passing test names;
- exact pre-existing local disclosures for `diff`-mode local preflight only. Immutable CI and snapshot evaluation never use disclosures as a mixed-scope waiver.

Unknown fields, unsafe or unbounded metadata values, unsafe `storyKey` filenames, secret/token/payload-shaped fields, globs in File Lists, duplicate or normalization-colliding arrays, ambiguous active contract identities, undeclared gitlinks, deleted/missing File List paths, and non-TRX narrative results fail closed. Candidate discovery ignores unrelated malformed inactive contracts but rejects active duplicate story keys or normalized story paths. Fenced Markdown examples are excluded before identity, section, task, acceptance, ledger, and primary-claim parsing. Both TE specs and canonical BMAD product stories (`# Story ...`, `Status: ...`, numbered acceptance criteria, nested task checkboxes, and `### File List`) are parsed explicitly. Historical completed contracts cannot be edited outside the exact completion transition, `bootstrap` cannot regress from `false` to `true`, and terminal story/ledger/action records cannot regress. A normal product completion must transition from exact `review` values in both its base story and base sprint entry; a TE bootstrap may remain `in-progress`, `review`, or `in-review` while its planning records remain open/review.

The policy fixes PR bounds to `github.event.pull_request.base.sha` and `github.event.pull_request.head.sha`, and push bounds to `github.event.before` and `github.sha`. It permits `git rev-parse HEAD^` only for a zero/empty push base; a non-zero base that is unavailable fails closed instead of silently narrowing the event to one commit. Schedule and manual-dispatch events use the empty exact range `github.sha..github.sha` because they do not propose a lifecycle transition. The checked-out full HEAD must equal the selected event head. Only the exact report directory is outside scope reporting, and only `scope.implementationDigest` in the active contract is masked during canonical hashing.

The policy's `exceptions` collection is versioned and empty by default. Adding an exception is a policy change that requires explicit approval, expiry, stable allowed reasons, and dedicated parser/mutation tests; a missing environment is never a passing exception.

## Deterministic scope and submodules

The digest is SHA-256 over ordinally sorted `repository/path/mode/blob` tuples. `diff` mode preserves the original exact-diff preflight and evaluates primary triggers against all scoped changes, including declared index/worktree changes. `snapshot-plus-transition` evaluates primary triggers only against the independent committed event, requires a clean index, worktree, and untracked set, requires every transition path to be root-owned, rejects every committed event path outside that repository's exact `includePaths` without a disclosure or report waiver, reads every included path from the exact HEAD tree whether or not that path changed in the event, and reports `eventPathCount` independently. Git-reported backslashes and normalization collisions fail closed rather than aliasing paths. Regular-file line endings remain exact bytes, executable mode is significant, and a symbolic link is mode `120000` with its target text hashed without following the target. Only `scope.implementationDigest` in the exact story contract path is masked; `bootstrap`, whitespace, and the same field name anywhere else remain digest evidence. Generated reports live outside the owned File List.

TE-2 bootstrap uses `snapshot-plus-transition` while its spec, ledger, and sprint action remain open. Delayed completion accepts exactly four event paths: the TE-2 spec (`in-review` to `complete` only), its contract (`bootstrap: true` to `false` plus the digest value only), the TE ledger (`review` to `complete` only), and the named sprint action (`open` to `done` only). Every other byte must remain unchanged, including the spec's frozen intent block. Missing, extra, or semantically broader lifecycle changes fail with a stable metadata-only reason.

Only repositories declared by the root `.gitmodules` are accepted, at any declared path depth. Every owned root gitlink requires an explicit inner repository scope. A changed submodule requires its superproject base gitlink to equal the declared submodule base, its committed-head/current gitlink to equal the declared submodule head, and its inner base/head diff to reconcile. The tool invokes only allowlisted read-only Git commands through `ProcessStartInfo.ArgumentList`; it never initializes, updates, or recursively discovers submodules.

## Machine results and retained evidence

Every lane must have a secure, canonical Microsoft TeamTest 2010 TRX: one namespaced `TestRun` root, one direct `Times`, `Results`, `TestDefinitions`, and `ResultSummary/Counters` structure, unique result `testId` values, and exactly one direct canonical `TestMethod` per `UnitTest`. Foreign-namespace local-name injection and duplicate structural elements fail as `machine_results_invalid`. Summary counters reconcile exactly with one `UnitTestResult` per reported total, with non-zero total/executed/passed counts, no failures, selectors resolved through each result's `testId` and canonical `TestMethod` class/name, and no skips unless that lane explicitly permits them. Display-name spoofing cannot satisfy a selector. A required primary lane never permits skips and must match the policy's exact lane, selector, source, and primary class. Policy path patterns use `**/` as zero or more directory segments; explicit claim triggers are recognized only outside Markdown fences. Current-run TRX start/finish times and provenance must be within the policy's current-run window and future-skew bound. Attestation validates the complete pinned policy, the contract version, safe non-aliasing TRX/provenance paths, and every current-run lane before creating or replacing any sidecar; one invalid lane leaves every existing sidecar byte-identical and creates none. The provenance sidecar binds the policy repository identity, exact base/head, implementation digest, TRX checksum, lane/source/selectors, UTC timestamp, and contract-declared artifact locator.

Policy v2 binds the logical primary lanes to shipped test classes exactly:

| Primary class | Logical lane | Required class selector |
|---|---|---|
| `browser` | `browser-primary` | `class:Hexalith.ChatBot.UI.E2E.Tests.RealRenderCrossSurfaceE2ETests` |
| `signalr` | `signalr-primary` | `class:Hexalith.ChatBot.Server.Tests.Projections.ChatBotProjectConversationHubE2ETests` |
| `hosting-assets` | `hosting-assets-primary` | `class:Hexalith.ChatBot.UI.E2E.Tests.FrontComposerShellIntegrationE2ETests` |
| `aspire-dapr` | `aspire-dapr-primary` | `class:Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests` |
| `recovery` | `recovery-primary` | `class:Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests` |

`aspire-dapr` path triggers are intentionally narrow: `src/Hexalith.ChatBot.AppHost/**` and `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` only (not repo-wide `*Dapr*`/`*Aspire*` name globs).

The CI and release topology producers write `topology-acceptance.trx` from the bound Aspire/Dapr class. `recovery-primary` has one completion lifecycle: at most one active transition contract declares it as `current-run` with the policy-pinned paths and locator. Before DAPR starts, the repository-owned `plan` command validates pinned policy, strict contract/status/lifecycle shape, scope digest, File List, checked mapping declarations, safe collision-free paths, the exact recovery selector/source/path binding, and single-consumer cardinality. `story-evidence-integrity` then conditionally runs the bound live class. Its contract bytes contain no future workflow-run or artifact identifier, and the sidecar is minted only after the exact implementation digest and sanitized TRX checksum both exist.

The live test writes its raw TRX under runner-temporary staging that no artifact upload includes. After DAPR cleanup, `RecoveryTrxSanitizer` projects exactly one passing bound test into a deterministic payload-free TRX containing only test identity, times, counters, IDs, and outcome. Unexpected class/method, counters, outcome, or timestamp structure fails before attestation. Live-class operational reports/manifests created during this completion invocation remain unpublished diagnostics and are not A10 evidence.

Scheduled and release live-recovery producers still write `live-recovery-validation.trx` plus recovery reports/manifests for the independent operational/A10 evidence gate. Those retained artifacts are not TE-2 `recovery-primary` completion evidence. They intentionally carry no TE-2 sidecar, and changing a completion contract to `retained` fails `primary_path_not_executed` even if a manually constructed sidecar would otherwise match.

Retained evidence on lanes whose policy binding permits it is accepted only when its immutable downloaded sidecar and locator bind to the policy's `repositoryIdentity`, the same exact implementation digest, full revisions, source, checksum, and the `maximumRetainedEvidenceAgeHours` window. The gate never infers retained identity from a Git remote or ambient repository variable. Current-run locators must exactly equal `file:<declared-trx>`. Result paths reject traversal and symbolic links in the results root, its ancestry, or the declared path. After resolving exact bounds, candidate detection loads only changed or selected contracts, then CI downloads only retained artifacts declared by those active contracts into collision-checked `retained/<run>/<artifact>/` directories. An unchanged malformed inactive contract cannot block an unrelated or no-transition run; a changed or selected malformed contract fails closed. A diagnostic fallback, unrelated aggregate suite, zero/all-skipped run, stale artifact, or screenshot without a direct invariant assertion cannot satisfy a triggered browser, SignalR, hosting/assets, Aspire/Dapr, or recovery primary path.

## Stable reason codes

| Reason | Meaning |
| --- | --- |
| `status_mismatch` | Story path/key/title, sprint key, record kind, target, persisted status, or transition bounds disagree. |
| `file_list_diff_mismatch` | File List and exact owned diff/snapshot paths differ, contain duplicates, or name a missing path. |
| `gitlink_scope_mismatch` | A submodule is nested/not root-declared or its root gitlink and inner scope disagree. |
| `scope_digest_mismatch` | A revision, disclosure, path scope, or canonical digest is missing or contradictory. |
| `machine_results_invalid` | TRX is missing/malformed/failed/zero/all-skipped or contains a forbidden skip. |
| `evidence_stale_or_unbound` | Provenance is stale or has the wrong full revision, digest, checksum, lane, or selectors. |
| `primary_path_not_executed` | A triggered/declared primary class lacks a successful recognized primary lane. |
| `checked_item_evidence_mismatch` | Mandatory work is unchecked/unmapped, names a stale path, or cites a non-passing assertion. |
| `evidence_payload_forbidden` | Contract/provenance grammar contains an unknown, secret-shaped, or payload-shaped field. |

## CI behavior

The build job rejects colliding project/lane names, emits collision-safe per-project TRX plus checksum sidecars, records its exact producer HEAD, and uploads them on success or failure. The topology producer independently checks out and records the same raw event head. The `story-evidence-integrity` job runs with `always()`, a 360-minute outer budget, and read-only contents/Actions access. It waits for both producers, always requires and verifies the general producer, checks out the exact event head with full history and no recursive submodules, reruns the gate's own tests, resolves the policy-bound event base/head, and detects candidates before consuming contracts.

The job requires, downloads, and verifies topology evidence only when the validated production plan declares current-run `aspire-dapr-primary`. That exact-head topology producer uses the same checksum-pinned Dapr CLI/runtime 1.18.0 pair and current artifact-action major as the consuming completion path. When the plan declares the single current-run `recovery-primary`, the completion job initializes runtime 1.18.0, runs the bound live class into raw staging, stops DAPR, and sanitizes the canonical completion TRX. A failed, skipped, zero-test, timed-out, cleanup-failed, or projection-failed recovery producer prevents attestation and fails the required check. With no declared recovery lane, none of those destructive steps run. Retained locators come from the validated plan and download without overwriting destinations.

The 360-minute job owns its whole deadline and fixes closeout at job start plus 330 minutes. It refuses DAPR initialization at or after 40 minutes elapsed and caps initialization at 10 minutes. Immediately before production it derives the remaining time to fixed closeout, sends interrupt no later than 15 minutes before that deadline and no later than the 265-minute in-process deadline, and caps the live step at 280 minutes. The final 30 minutes are thereby allocated only to DAPR cleanup, projection, attestation, validation, and upload.

The job then invokes the strict `ci` entry point, which validates the full pinned policy even for `no-transition`, preflights every active contract and all cross-contract result-path collisions before any provenance write, then attests and validates every matching contract. A contract-specific attestation failure produces its metadata-only report while the remaining records are still evaluated and the summary is still written. Whatever metadata reports, sidecars, and sanitized recovery TRX were safely produced are uploaded by the always-running retention step; unchecked raw TRX and completion-run operational side-products are outside every upload path. The archive is retained for 30 days and is immutable only until GitHub deletion or expiry; its post-upload artifact ID/digest is not a TE-2 attestation input. With no transition the job has waited for topology but does not consume or require its evidence and publishes a successful `no-transition` report after the self-tests pass.

If a required environment or retained primary artifact is unavailable, leave the story in `review`/`in-progress`; do not convert the primary obligation to a warning. If a path is owned by another local story, disclose it exactly for local diagnosis and split the change before an immutable completion proposal.

## Troubleshooting

Start with the first stable reason in the JSON report. Re-run `attest` after any owned source, test, contract, or workflow change because the digest and TRX checksum are exact. Do not hand-edit provenance or reports. For submodule failures, confirm the root gitlink and explicit inner base/head both changed and the submodule is declared directly by the root `.gitmodules`. For result failures, inspect the TRX at its locator rather than copying its payload into the report.
