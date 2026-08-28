---
title: 'Harden pull-request integration and release ordering'
type: 'chore'
created: '2026-08-27'
status: 'done'
baseline_revision: '735645ac9f87d0e6062c92c77860f41b8915c9a9'
baseline_commit: '735645ac9f87d0e6062c92c77860f41b8915c9a9'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
warnings: [oversized]
deferred:
  - summary: >-
      No repository workflow lints the GitHub Actions files or the checked-in shell scripts, so
      workflow and script regressions are caught only when a run fails.
    evidence: |-
      `.github/workflows/` contains no actionlint or shellcheck step; the spec's Verification section
      runs actionlint locally only, and shellcheck is unavailable in this environment. The publication
      guard and merge-ref boundaries are now safety-critical Bash with no static gate, and the existing
      `.github/scripts/install-dapr-cli.sh` is equally unguarded, so this predates the current change.
    location: >-
      .github/workflows
    severity: low
  - summary: >-
      The required `build` job's ordinary test lanes have no zero-test guard, so a lane that discovers no
      tests exits 0 and still emits a checksummed TRX that reads as real evidence.
    evidence: |-
      `.github/workflows/ci.yml`'s `Test` step runs each of the 13 ordinary lanes as
      `dotnet test "$project" --no-build --configuration Release --logger trx ...` with no
      `RunConfiguration.TreatNoTestsAsError=true`. Reproduced in this repository: the same command form with a
      filter matching nothing prints "No test matches the given testcase filter" and exits 0, still writes the
      TRX, so `sha256sum` succeeds and `executed` still reaches 13. The `build` loop is unchanged by this
      story -- the gap predates it and was surfaced only because the new merge lane adopted the override. The
      merge lane is `pull_request`-only, so pushes to `main` and the whole release path remain unguarded.
    location: >-
      .github/workflows/ci.yml:130
    severity: medium
---

<intent-contract>

## Intent

**Problem:** Exact-head CI evidence does not exercise GitHub's synthetic pull-request merge, while independently completing per-commit recovery gates can let an older `main` workflow reach semantic-release after a newer commit. This permits merge-only failures and out-of-order publication attempts without weakening any validated commit's verdict.

**Approach:** Add a PR-only, non-provenance merge-ref build/test lane alongside the unchanged exact-head evidence graph. Immediately before semantic-release on `main`, fetch the current remote head and publish only for an exact match; classify an included older SHA as superseded and fail closed on divergence or lookup failure.

## Boundaries & Constraints

**Always:** Preserve the exact PR-head checkout, bounds, artifacts, and dependencies of `build`, `topology-acceptance`, and `story-evidence-integrity`; preserve per-commit, non-cancelling recovery validation and an authoritative verdict for every SHA; initialize only root-declared submodules; grant merge-ref validation read-only contents access; place the release guard immediately before publication.

**Block If:** The GitHub pull-request event cannot bind the synthetic merge commit to both advertised parent SHAs, or semantic-release cannot be skipped without marking an ancestor run failed.

**Never:** Add the merge-ref job to the story-evidence provenance graph or reuse its artifact names; serialize/cancel validation runs by branch; treat a divergent or missing remote `main` as superseded; edit the deferred-work ledger; change prerelease behavior for `next`, `alpha`, or `beta`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| PR merge validation | Pull-request event whose synthetic merge has the advertised base and head parents | Restore, Release build, and ordinary test lanes execute on `github.sha` without producing exact-head evidence | Any checkout, parent, build, or test mismatch fails the isolated job |
| Non-PR CI event | Push, schedule, or dispatch | Merge-ref job is skipped; existing jobs retain their behavior | No merge-ref claim is emitted |
| Current main release | Validated SHA equals freshly fetched `origin/main` | Semantic-release runs | Fetch or commit resolution failure blocks publication |
| Superseded main release | Validated SHA is a strict ancestor of freshly fetched `origin/main` | Job succeeds with an explicit superseded verdict; semantic-release is skipped | No release side effect occurs |
| Diverged main release | Validated SHA is not equal to or an ancestor of current `origin/main` | Publication is refused | Guard fails closed |

</intent-contract>

## Code Map

- `.github/workflows/ci.yml:72` (`:38` before this change) -- `build` is the reusable restore/build/per-project-test shape, but its exact-head checkout and `machine-test-results` provenance must remain unchanged.
- `.github/workflows/ci.yml:150` and `.github/workflows/ci.yml:446` (`:116` and `:412` before this change) -- exact-head completion and topology jobs; their semantics and evidence graph are read-only boundaries.
- `.github/workflows/release.yml:96` -- per-commit recovery concurrency preserves each SHA's verdict and must not become branch-keyed.
- `.github/workflows/release.yml:262` -- `semantic-release` job published immediately after full-history checkout and Node setup; the main-head decision is inserted here.
- `.github/scripts/install-dapr-cli.sh` -- precedent for checked-in, fail-closed workflow shell logic; keep new scripts narrow and executable under Bash.
- `.releaserc.json` -- documents `main`, `next`, `alpha`, and `beta` as release branches; do not apply a literal main comparison to prerelease runs.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs:752` -- workflow contract tests and block/step extraction helpers for isolated PR integration assertions.
- `tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs:347` -- release concurrency and evidence-gate architecture assertions; anchor main-head publication ordering here.
- `tests/Hexalith.ChatBot.Architecture.Tests/ReleaseWorkflowSafetyTests.cs` -- add executable scenario coverage for the new shell boundaries; keep its single test class self-contained.
- `docs/story-evidence-integrity.md` and `docs/adrs/live-recovery-validation-drivers.md` -- existing exact-head and per-commit design records that must distinguish validation verdicts from publication eligibility.

## Tasks & Acceptance

**Execution:**
- [x] `.github/scripts/verify-pull-request-merge.sh` and `.github/scripts/run-merge-test-lanes.sh` -- extract fail-closed merge-parent binding and ordinary-lane execution so their success, mismatch, zero-lane, and failing-test behavior can be executed locally.
- [x] `.github/workflows/ci.yml` -- add a uniquely named, explicitly bounded PR-only merge-ref job that checks out `github.sha`, calls the tested scripts, restores/builds in Release, has no `needs`, and uploads no provenance artifacts.
- [x] `.github/scripts/guard-main-publication.sh` -- implement exact-current, strict-ancestor superseded, prerelease pass-through, and all other fail-closed decisions with explicit outputs.
- [x] `.github/workflows/release.yml` -- call the tested guard immediately before semantic-release, condition publication on its output, preserve per-commit validation and configured prerelease behavior, and add no branch-wide concurrency.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/ReleaseWorkflowSafetyTests.cs` -- execute the scripts against temporary Git graphs and stub test commands, covering valid/mismatched merge parents, successful/failing/empty test lanes, current/ancestor/divergent/missing `main`, and prerelease pass-through; assert exit codes and outputs.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` and `tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs` -- pin workflow wiring, PR-only/no-needs isolation, nonrecursive submodules, no provenance artifacts, conditional release, and unchanged per-commit recovery concurrency without substituting for behavioral tests.
- [x] `docs/story-evidence-integrity.md` and `docs/adrs/live-recovery-validation-drivers.md` -- record the isolation boundary and the distinction between preserved validation verdicts and newest-head publication without extending the earlier provenance-only sign-off.

**Acceptance Criteria:**
- Given a pull request whose head passes exact-head evidence, when GitHub constructs its merge ref, then an independent required check builds and tests that exact synthetic merge without changing any exact-head producer or artifact.
- Given multiple `main` SHAs completing recovery validation out of order, when each reaches publication, then every validation verdict remains successful or failed on its own evidence while only the freshly fetched exact `main` head may invoke semantic-release.
- Given an older validated SHA included in current `main`, when its publication guard runs, then it records superseded and exits successfully without publishing.
- Given divergent history or an unavailable remote `main`, when the publication guard runs, then it fails closed before semantic-release.

## Spec Change Log

### 2026-08-27 — Executable workflow-boundary verification
- Trigger: review found that source-substring assertions could stay green while merge test execution or release-guard control flow was broken.
- Amended: required narrow checked-in scripts plus executable temporary-repository and stub-command scenario tests for every matrix branch; retained architecture tests only for Actions wiring and provenance isolation.
- Avoids: a current `main` silently falling through to superseded, a divergent/fetch failure publishing, or a merge-only failing lane being bypassed while textual tests pass.
- KEEP: the isolated PR-only job, exact ordered parent binding, unchanged exact-head evidence graph, per-SHA non-cancelling recovery verdicts, exact/ancestor/divergent release classification, prerelease preservation, and fail-closed remote handling.

## Review Triage Log

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 2: (high 2, medium 0, low 0)
- patch: 5: (high 0, medium 1, low 4)
- defer: 0
- reject: 11: (high 0, medium 3, low 8)
- addressed_findings:
  - `[high]` `[bad_spec]` Replace source-only merge-lane assertions with executable parent-binding and test-failure propagation scenarios while preserving the isolated workflow contract.
  - `[high]` `[bad_spec]` Replace source-only publication assertions with executable current, ancestor, divergent, missing-ref, fetch-failure, and prerelease scenarios over temporary Git graphs.

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 11: (high 1, medium 4, low 6)
- defer: 0
- reject: 10: (high 0, medium 5, low 5)
- addressed_findings:
  - `[medium]` `[patch]` Disable persisted checkout credentials in the untrusted synthetic-merge job and pin the setting in its architecture contract.
  - `[high]` `[patch]` Make project discovery failures propagate instead of allowing process substitution to hide a partial merge-test suite.
  - `[medium]` `[patch]` Require every discovered xUnit v3 lane to execute at least one test.
  - `[low]` `[patch]` Assert every expected project path and required `dotnet test` argument exactly once in the successful-lane scenario.
  - `[low]` `[patch]` Add executable rejection coverage for invalid synthetic-merge parent cardinality.
  - `[medium]` `[patch]` Add executable rejection coverage for a checkout that differs from the validated release SHA.
  - `[medium]` `[patch]` Add executable rejection coverage for nonempty partial test discovery.
  - `[low]` `[patch]` Bound child-process waits and terminate hung process trees in workflow-safety tests.
  - `[low]` `[patch]` Strengthen publication-guard adjacency assertions to reject any intervening workflow step.
  - `[low]` `[patch]` Assert no workflow job depends on the isolated merge-ref job.
  - `[low]` `[patch]` Bound the semantic-release job with an explicit timeout.

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 8: (high 1, medium 1, low 6)
- defer: 1: (high 0, medium 0, low 1)
- reject: 16: (high 0, medium 4, low 12)
- addressed_findings:
  - `[high]` `[patch]` Replaced the inert `--minimum-expected-tests` Microsoft.Testing.Platform switch, which this VSTest-bridge toolchain accepts and ignores, with `RunConfiguration.TreatNoTestsAsError=true`, and proved the substitution against the real runner instead of a stub argv string.
  - `[medium]` `[patch]` Removed the publication guard's reliance on undocumented duplicate-key resolution in `GITHUB_OUTPUT` by writing the decision exactly once through an exit trap, and asserted the single-write cardinality in every guard scenario.
  - `[low]` `[patch]` Stopped `readonly name="$(command)"` from swallowing failed SHA resolutions in the merge verifier and the publication guard, so a malformed or unknown revision stops at its own diagnostic.
  - `[low]` `[patch]` Added executable coverage for the unsupported-release-branch deny path and for a malformed advertised pull-request SHA.
  - `[low]` `[patch]` Isolated the scenario processes from developer Git configuration (global config, hooks, signing, credential prompts) so the boundaries, not the workstation, decide the result.
  - `[low]` `[patch]` Bounded the merge-ref and semantic-release workflow-block extractions by the next top-level job key so an inserted job cannot satisfy the isolation and adjacency assertions.
  - `[low]` `[patch]` Recorded that the synthetic-merge check is enforcing only once branch protection lists it as a required status check, and why the run-settings override is the form that fails a zero-test lane.
  - `[low]` `[patch]` Refreshed the stale Code Map line anchors displaced by the new job.

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 8: (high 0, medium 1, low 7)
- defer: 1: (high 0, medium 1, low 0)
- reject: 25: (high 0, medium 6, low 19)
- addressed_findings:
  - `[medium]` `[patch]` Bound the publication guard's hardcoded channel set to the configuration it stands in front of: a new executable scenario reads `.releaserc.json` and the release workflow's push triggers and drives the guard once per configured channel, so a channel added to configuration can no longer become silently unreleasable.
  - `[low]` `[patch]` Stopped `readonly discovery_file="$(mktemp)"` from swallowing a failed `mktemp` in the merge lane runner -- the same idiom the sibling script already documents and avoids -- so allocation failure reports itself instead of masquerading as project-discovery failure.
  - `[low]` `[patch]` Captured `git show --format=%P` into a variable before splitting in the merge verifier, so an unreadable commit stops on its own diagnostic instead of being misreported as "found 0 parents".
  - `[low]` `[patch]` Corrected the lane-count refusal message, which said "refusing a partial run" even when more lanes were discovered than expected -- the likelier direction, since it fires whenever a test project is added.
  - `[low]` `[patch]` Scoped the non-cancellation assertion to each per-commit concurrency block and pinned their cardinality; the previous whole-file `cancel-in-progress: false` search stayed green if one of the two lanes flipped to cancelling.
  - `[low]` `[patch]` Stopped scenario cleanup from discarding the scenario's verdict: the 18 `finally` deletes now tolerate `IOException`/`UnauthorizedAccessException`, and a missing step-output file reports the guard's own failure instead of throwing `FileNotFoundException`.
  - `[low]` `[patch]` Pinned `DOTNET_CLI_UI_LANGUAGE` for scenario child processes so the zero-test-lane proof, which asserts on the runner's English diagnostic, does not depend on the machine locale.
  - `[low]` `[patch]` Named the implementing scripts, the deliberate lane-set duplication with the `build` job, and the `MERGE_TEST_*` test seams in `docs/story-evidence-integrity.md`, so a maintainer meeting a red merge check has a path from symptom to file.

## Design Notes

The ancestry distinction is security-relevant: equality means publish, strict ancestry means safely superseded because current `main` includes the validated tree, and every other relationship is an error. Semantic-release's own remote check remains defense in depth for a race after the explicit guard.

## Verification

**Commands:**
- `actionlint .github/workflows/ci.yml .github/workflows/release.yml` -- expected: both workflows are valid.
- `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --configuration Release -m:1` -- expected: architecture project builds cleanly.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Release/net10.0/Hexalith.ChatBot.Architecture.Tests -class Hexalith.ChatBot.Architecture.Tests.ReleaseWorkflowSafetyTests` -- expected: every executable matrix scenario passes with zero skips.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Release/net10.0/Hexalith.ChatBot.Architecture.Tests` with focused `-method` arguments for the two workflow-wiring contracts -- expected: both focused assertions pass.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Summary: Added an isolated PR synthetic-merge build/test check and a fail-closed latest-`main` publication guard while preserving exact-head provenance and per-SHA recovery verdicts. This follow-up pass closed the one real coupling the earlier passes left unbound -- the guard's hardcoded release-channel set versus the configuration it gates -- and removed the remaining places where a boundary's own diagnostic, rather than the boundary, would decide what a failure looked like.

Files changed:
- `.github/workflows/ci.yml` -- adds the bounded, read-only PR merge-ref job with no provenance dependency or artifact output.
- `.github/workflows/release.yml` -- invokes the tested publication guard immediately before the conditioned semantic-release step.
- `.github/scripts/verify-pull-request-merge.sh` -- binds the checked-out synthetic merge to exactly the advertised base and head parents; unresolvable revisions and unreadable parent lists now stop on their own diagnostics.
- `.github/scripts/run-merge-test-lanes.sh` -- discovers the complete ordinary lane set, fails on discovery/allocation/count gaps with an accurate message in both drift directions, and runs every lane in Release under `RunConfiguration.TreatNoTestsAsError=true`.
- `.github/scripts/guard-main-publication.sh` -- classifies current, superseded, prerelease, and blocked publication outcomes, publishing the decision exactly once through an exit trap.
- `tests/Hexalith.ChatBot.Architecture.Tests/ReleaseWorkflowSafetyTests.cs` -- executes 22 isolated Git, stub-command, and real-runner safety scenarios under Git- and locale-isolated child processes, including one driven from the live release configuration.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` -- pins merge-ref workflow isolation and least privilege, bounded by the next top-level job key.
- `tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs` -- pins release-guard adjacency, conditional publication, and per-block per-SHA non-cancellation.
- `docs/story-evidence-integrity.md` -- documents merge-ref compatibility as non-provenance validation, the external required-status-check prerequisite, the zero-test lane override, and the implementing scripts and their deliberate duplication of the `build` lane set.
- `docs/adrs/live-recovery-validation-drivers.md` -- documents verdict preservation and newest-head publication ordering.
- `_bmad-output/implementation-artifacts/spec-release-workflow-safety.md` -- records plan, review repair, verification, and completion evidence.

Review findings: 8 patches applied (high 0, medium 1, low 7); 1 item deferred (medium); 25 items rejected as intent-authorized behavior, loud fail-closed paths, pre-existing conventions, or non-actionable noise. Follow-up review recommendation: `true`; no patched finding was high severity, and the patch score is `10` (`3 x 1 medium + 7 low`).

Verification performed:
- `actionlint .github/workflows/ci.yml .github/workflows/release.yml` -- passed.
- `bash -n` for all three scripts -- passed.
- Release build of `Hexalith.ChatBot.Architecture.Tests.csproj` with `-m:1` -- passed with 0 warnings and 0 errors.
- `ReleaseWorkflowSafetyTests` -- 22/22 passed, 0 skipped.
- Focused merge-ref and publication workflow contracts -- 1/1 passed each, 0 skipped.
- Full architecture suite -- 104/104 passed, 0 skipped.
- Mutation probes on both assertions added this pass: flipping one `cancel-in-progress` to `true` fails `ReleasePublication_ShouldRequireFreshMainHeadImmediatelyBeforeSemanticRelease`; adding a `1.x` channel to `.releaserc.json` fails `EveryConfiguredReleaseChannelShouldRemainPublishable` with `Unsupported release branch 1.x`. Both files were restored and re-verified clean.
- Toolchain probe behind the earlier high-severity patch, re-confirmed with the pinned CLI locale: `dotnet test <arch tests dll> --filter <no match>` exits 0, while the same run with `-- RunConfiguration.TreatNoTestsAsError=true` exits 1.
- `git diff --check` -- passed.

Residual risks: hosted GitHub event/ref behavior and semantic-release side effects still cannot be reproduced locally; the synthetic-merge parent premise (that `github.sha` is the merge of the advertised parents, in base-then-head order) and the runner's step-output parsing remain axioms exercised only by a live run, and a base-branch push between merge-ref regeneration and event delivery would fail the check closed rather than distinguishably. The merge check blocks nothing until branch protection lists `pull-request synthetic merge build and test` as required; this run did not change remote settings. The ordinary-lane count and exclusion list remain duplicated between the `build` job and the merge lane script -- drift fails both loudly, and the duplication is now documented. A validated SHA that is a strict *descendant* of the fetched `origin/main` (a stale remote read or a rollback) is refused as divergence, which is what the intent's matrix specifies. `next` keeps its prerelease pass-through and therefore no stale-head guard, as the intent requires. `shellcheck` was unavailable, so Bash validation used `bash -n`, executable scenarios, and workflow lint; the absent CI lint gate and the `build` job's own zero-test blindness are recorded as deferred items.
