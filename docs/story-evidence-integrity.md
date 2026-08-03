# Story-Evidence Integrity Gate

The TE-2 gate is the repository-owned preflight for a proposed story or technical-enabler completion. It reconciles identity and status, the exact root/root-submodule change scope, the story File List, machine results and provenance, triggered primary paths, and every mandatory task/acceptance mapping. A pass authorizes a reviewable status edit; it never edits status itself.

TE-2 and the Epic 13 action remain non-complete until repository branch protection requires the check named `story-evidence-integrity`.

## Local preflight

Build and run the gate tests first:

```bash
dotnet build tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release -warnaserror
dotnet test tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj --configuration Release --logger "trx;LogFileName=story-evidence-gate.trx" --results-directory TestResults/story-evidence-gate
```

Create provenance for the contract's `current-run` TRX lanes, then validate. Attestation never creates or rewrites a `retained` sidecar:

```bash
dotnet run --project tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release -- attest --contract _bmad-output/implementation-artifacts/evidence/<story-key>.json --base <full-base-sha> --head <full-head-sha> --results TestResults
dotnet run --project tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release -- validate --story _bmad-output/implementation-artifacts/<story>.md --contract _bmad-output/implementation-artifacts/evidence/<story-key>.json --target-status done --base <full-base-sha> --head <full-head-sha> --results TestResults --report _bmad-output/implementation-artifacts/evidence/reports/<story-key>.json
```

The base and head arguments must resolve to exact full Git revisions. A root contract may use `$BASE` and `$HEAD` as explicit CI-bound revision sources because a tracked contract cannot embed the identifier of the commit containing itself. Reports and provenance always contain the resolved full revisions.

## Contract grammar

`story-evidence-policy.json` is the versioned policy. Each prospective transition has exactly one `_bmad-output/implementation-artifacts/evidence/<story-key>.json` contract with:

- explicit `storyKey`, `storyTitle`, `storyPath`, `sprintStatusKey`, `recordKind`, `targetStatus`, and `persistedStatus`;
- explicit root and optional root-declared-submodule repositories, their revision sources, exact owned paths, and the canonical implementation digest;
- one or more TRX lanes with result/provenance paths, an exact artifact locator, source (`current-run` or `retained`), `class:<fully-qualified-class>` and/or `method:<fully-qualified-method>` selectors, and skip policy;
- triggered/declared primary-path classes and their recognized primary lane;
- one mapping for every mandatory task and acceptance criterion, bound to current paths and/or passing test names;
- exact pre-existing local disclosures for local preflight only. Immutable base/head CI diffs cannot use these disclosures as a mixed-scope waiver.

Unknown fields, unsafe or unbounded metadata values, secret/token/payload-shaped fields, globs in File Lists, duplicate or normalization-colliding arrays, ambiguous identities/ledger records, undeclared gitlinks, deleted/missing File List paths, and non-TRX narrative results fail closed. Fenced Markdown examples are excluded before identity, section, task, acceptance, ledger, and primary-claim parsing. Both TE specs and canonical BMAD product stories (`# Story ...`, `Status: ...`, numbered acceptance criteria, nested task checkboxes, and `### File List`) are parsed explicitly. A normal product completion must transition from exact `review` values in both its base story and base sprint entry; a TE bootstrap may remain `in-progress`, `review`, or `in-review` while its planning records remain open/review.

The policy fixes PR bounds to `github.event.pull_request.base.sha` and `github.event.pull_request.head.sha`, push bounds to `github.event.before` and `github.sha`, and permits only `git rev-parse HEAD^` when the push base is zero or unavailable. The checked-out full HEAD must equal the selected event head. Only the exact report directory and `implementationDigest` lifecycle field are excluded from canonical scope hashing.

The policy's `exceptions` collection is versioned and empty by default. Adding an exception is a policy change that requires explicit approval, expiry, stable allowed reasons, and dedicated parser/mutation tests; a missing environment is never a passing exception.

## Deterministic scope and submodules

The digest is SHA-256 over ordinally sorted `repository/path/mode/blob` tuples. Immutable scope reads bytes and modes from the exact Git tree; worktree scope reads tracked modes from the Git index. Regular-file line endings remain exact bytes, executable mode is significant, and a symbolic link is mode `120000` with its target text hashed without following the target. Only `scope.implementationDigest` in the exact story contract path is removed before hashing; the same field name in any other JSON location remains evidence. Generated reports live outside the owned File List.

Only repositories declared by the root `.gitmodules` are accepted, at any declared path depth. Every owned root gitlink requires an explicit inner repository scope. A changed submodule requires its superproject base gitlink to equal the declared submodule base, its committed-head/current gitlink to equal the declared submodule head, and its inner base/head diff to reconcile. The tool invokes only allowlisted read-only Git commands through `ProcessStartInfo.ArgumentList`; it never initializes, updates, or recursively discovers submodules.

## Machine results and retained evidence

Every lane must have a parseable TRX whose summary counters reconcile exactly with one `UnitTestResult` per reported total, with non-zero total/executed/passed counts, no failures, selectors matched by actual passing result names (including parameterized method display names), and no skips unless that lane explicitly permits them. A required primary lane never permits skips and must match the policy's exact lane, selector, source, and primary class. Policy path patterns use `**/` as zero or more directory segments; explicit claim triggers are recognized only outside Markdown fences. Its provenance sidecar binds the exact base/head, implementation digest, TRX checksum, lane/source/selectors, UTC timestamp, and contract-declared artifact locator.

Policy v1 binds the logical primary lanes to shipped test classes exactly:

| Primary class | Logical lane | Required class selector |
|---|---|---|
| `browser` | `browser-primary` | `class:Hexalith.ChatBot.UI.E2E.Tests.RealRenderCrossSurfaceE2ETests` |
| `signalr` | `signalr-primary` | `class:Hexalith.ChatBot.Server.Tests.Projections.ChatBotProjectConversationHubE2ETests` |
| `hosting-assets` | `hosting-assets-primary` | `class:Hexalith.ChatBot.UI.E2E.Tests.FrontComposerShellIntegrationE2ETests` |
| `aspire-dapr` | `aspire-dapr-primary` | `class:Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests` |
| `recovery` | `recovery-primary` | `class:Hexalith.ChatBot.IntegrationTests.Recovery.LiveContinuityAspireE2eTests` |

The CI and release topology producers write `topology-acceptance.trx` from the bound Aspire/Dapr class. Scheduled and release live-recovery producers write `live-recovery-validation.trx` from the bound recovery class, so an active exact-run retained locator can satisfy `recovery-primary` without changing the logical lane name.

Retained scheduled/release evidence is accepted only when its immutable downloaded sidecar binds to the same exact implementation digest, full revisions, source, safe exact-run GitHub Actions locator, and checksum and is within `maximumEvidenceAgeHours`. Current-run locators must exactly equal `file:<declared-trx>`. Result paths reject traversal and symbolic links in the results root, its ancestry, or the declared path. After resolving exact bounds, CI detects the proposed transitions and downloads only retained artifacts declared by those transition contracts into collision-checked `retained/<run>/<artifact>/` directories; inactive contracts cannot block an unrelated or no-transition run. A missing artifact required by an active transition fails closed. A diagnostic fallback, unrelated aggregate suite, zero/all-skipped run, stale artifact, or screenshot without a direct invariant assertion cannot satisfy a triggered browser, SignalR, hosting/assets, Aspire/Dapr, or recovery primary path.

## Stable reason codes

| Reason | Meaning |
| --- | --- |
| `status_mismatch` | Story path/key/title, sprint key, record kind, target, persisted status, or transition bounds disagree. |
| `file_list_diff_mismatch` | File List and exact owned changed paths differ, contain duplicates, or name a missing path. |
| `gitlink_scope_mismatch` | A submodule is nested/not root-declared or its root gitlink and inner scope disagree. |
| `scope_digest_mismatch` | A revision, disclosure, path scope, or canonical digest is missing or contradictory. |
| `machine_results_invalid` | TRX is missing/malformed/failed/zero/all-skipped or contains a forbidden skip. |
| `evidence_stale_or_unbound` | Provenance is stale or has the wrong full revision, digest, checksum, lane, or selectors. |
| `primary_path_not_executed` | A triggered/declared primary class lacks a successful recognized primary lane. |
| `checked_item_evidence_mismatch` | Mandatory work is unchecked/unmapped, names a stale path, or cites a non-passing assertion. |
| `evidence_payload_forbidden` | Contract/provenance grammar contains an unknown, secret-shaped, or payload-shaped field. |

## CI behavior

The build job rejects colliding project/lane names, emits collision-safe per-project TRX plus checksum sidecars, records its exact producer HEAD, and uploads them on success or failure. The topology producer independently checks out and records the same raw event head. The `story-evidence-integrity` job runs with `always()` and read-only Actions access, always requires and verifies the general build producer, checks out the exact event head with full history and no recursive submodules, reruns the gate's own tests, resolves the policy-bound event base/head, and detects story, technical-enabler ledger, and technical-enabler action completion transitions. It requires, downloads, and verifies the topology producer only when an active transition contract declares the current-run `aspire-dapr-primary` lane, and collects retained locators only from active transition contracts without overwriting destinations. It then attests current-run lanes only, validates every matching contract, writes a metadata-only job summary, and uploads metadata reports/sidecars even on failure. With no transition it does not depend on topology or inactive retained artifacts and publishes a successful `no-transition` report after the self-tests pass.

If a required environment or retained primary artifact is unavailable, leave the story in `review`/`in-progress`; do not convert the primary obligation to a warning. If a path is owned by another local story, disclose it exactly for local diagnosis and split the change before an immutable completion proposal.

## Troubleshooting

Start with the first stable reason in the JSON report. Re-run `attest` after any owned source, test, contract, or workflow change because the digest and TRX checksum are exact. Do not hand-edit provenance or reports. For submodule failures, confirm the root gitlink and explicit inner base/head both changed and the submodule is declared directly by the root `.gitmodules`. For result failures, inspect the TRX at its locator rather than copying its payload into the report.
