---
title: 'Update .NET SDK references to 10.0.302'
type: 'chore'
created: '2026-07-16'
status: 'done'
review_loop_iteration: 1
baseline_commit: 'a547891e3f52c60be946af5e24b1fdcc62fa02af'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current SDK control surfaces and living guidance must move to .NET SDK `10.0.302`, but a literal repository-wide substitution also changed an unavailable `Microsoft.SourceLink.GitHub` package version and rewrote immutable evidence about historical runs.

**Approach:** Set active SDK pins, CI/action inputs, current assertions, and living guidance to `10.0.302`. Keep non-SDK dependency versions at published values and restore dated or captured evidence to the SDK version actually used, then verify each remaining older version is justified by its semantic role rather than accidental drift.

## Boundaries & Constraints

**Always:** Treat each root-declared submodule as its own repository and preserve formatting and line endings. Update `global.json`, setup-dotnet/SDK action inputs, current configuration tests, living prerequisites/development/architecture guidance, current project-context files, and source comments that describe the active toolchain. Keep `Microsoft.SourceLink.GitHub` at the newest version actually published for that package (`10.0.301` at review time). Restore captured command output, completed test/benchmark/conformance evidence, dated scan results, proof packets, test summaries, retrospectives, and completed story execution records to their original observed version. Correct associated dates when living guidance changes a release claim.

**Ask First:** Stop when a file mixes active normative guidance with immutable captured evidence and its intended ownership cannot be determined from surrounding structure, or if work would require entering a nested/non-root submodule, modifying binary content, or overwriting unrelated concurrent work.

**Never:** Invent a NuGet version from the SDK number; rewrite the claimed environment of a completed run without rerunning it; initialize or traverse nested submodules recursively; rewrite Git history; or independently commit, push, or update parent submodule pointers.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Active SDK control | Current pin/input/assertion uses an older .NET 10 SDK | Value becomes `10.0.302` | Fail if the SDK cannot resolve locally |
| Non-SDK package | `Microsoft.SourceLink.GitHub` dependency pin | Published package version remains `10.0.301` | Reject unavailable `10.0.302` |
| Immutable evidence | Dated/captured record of a run performed with an older SDK | Original observed SDK value is preserved | Do not relabel; rerun and append separate evidence if currency is required |
| Living release claim | Current guidance pairs `10.0.302` with an older SDK's release date | Version and date become internally consistent | Verify against official release metadata |
| Repository boundary | Root repository or root-declared submodule | Applicable current references are updated | Exclude nested submodule contents and Git metadata |

</frozen-after-approval>

## Code Map

- `global.json`, `.github/workflows/**`, and root-declared submodule equivalents -- executable SDK pins and setup inputs that must resolve to `10.0.302`.
- `references/Hexalith.Builds/Github/**` -- living shared-action defaults and examples that follow the SDK baseline.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- independent NuGet catalog; SourceLink must remain on its published package version.
- `_bmad-output/project-context.md`, top-level canonical `architecture.md`/`epics.md`, `README.md`, `CONTRIBUTING.md`, and current prerequisite/development/runbook docs -- living guidance that should describe the active SDK.
- `_bmad-output/implementation-artifacts/**`, dated reviews/research/proposals/memlogs, retrospectives, test summaries, proof packets, and `docs/**/*evidence*`/scan reports -- provenance-bearing records that retain observed historical versions.
- `tests/**` and `src/**` -- current pin assertions/comments stay updated; dated artifact generators retain the version tied to their recorded run date.
- `references/Hexalith.Memories/_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md` -- mixed file: current stack guidance and dated preflight evidence must be handled separately.

## Tasks & Acceptance

**Execution:**
- [x] Root and affected submodules' provenance-bearing files -- restore version tokens from each repository's pre-change baseline without reverting unrelated concurrent commits.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- restore `Microsoft.SourceLink.GitHub` to published `10.0.301` so consumers can restore.
- [x] Root and affected submodules' active configuration, living guidance, source comments, and current assertions -- retain/update `10.0.302` and correct coupled release dates, including root architecture's July 14, 2026 release claim.
- [x] Mixed records and dated artifact generators -- preserve historical run values while keeping separately stated current-stack guidance current.
- [x] All 14 repositories -- audit every remaining `10.0.300`/`10.0.301` occurrence by semantic role, verify SDK resolution, and inspect repository-local diffs without entering nested submodules.

**Acceptance Criteria:**
- Given an executable SDK pin, setup input, current pin assertion, or living prerequisite, when inspected after correction, then it uses `10.0.302` and changed `global.json` repositories resolve that SDK locally.
- Given a completed/datetime-bound test, benchmark, scan, proof packet, conformance artifact, retrospective, or execution record, when compared with its pre-change repository baseline, then its recorded SDK version is unchanged unless a new run with new provenance was actually appended.
- Given `Microsoft.SourceLink.GitHub`, when its central pin is inspected, then it remains `10.0.301` and no unavailable SDK-shaped package version is introduced.
- Given any remaining `10.0.300` or `10.0.301` tracked occurrence, when the semantic audit runs, then it belongs to immutable provenance, the SourceLink exception, or this workflow's explanation; no active SDK control surface remains stale.
- Given living guidance that states a release date for SDK `10.0.302`, when inspected, then it identifies July 14, 2026 rather than inheriting the prior SDK's May date.
- Given concurrent submodule commits and root pointer movement, when diffs are reviewed, then corrective edits preserve those commits and do not traverse or modify nested submodule contents.

## Spec Change Log

- Iteration 1: adversarial review found that literal substitution created an unpublished `Microsoft.SourceLink.GitHub` pin and falsified dated evidence. Human approved narrowing the intent to current SDK references while preserving published dependency pins and historical provenance. The spec now prevents restore failure and misleading audit/test records. KEEP: `10.0.302` SDK pins, CI inputs, current assertions, living guidance, root-declared-only submodule scope, byte-preserving edits, and local SDK-resolution verification.

## Design Notes

Classification is semantic, not purely path-based. A canonical architecture or project-context file is living guidance; a timestamped command transcript or signed-off test summary is provenance. In a mixed file, classify each occurrence from its surrounding statement rather than restoring or updating the entire file blindly.

## Verification

**Commands:**
- Repository-local `git grep -n -I -E '10\.0\.(300|301)' -- . ':(exclude)references/*'` plus semantic allowlist comparison -- expected: every match is an approved provenance/package/workflow exception.
- Baseline-to-working-tree token comparison for provenance files -- expected: recorded version tokens equal their repository's pre-change content while unrelated concurrent changes remain intact.
- `dotnet --version` in each repository with a changed `global.json` -- expected: `10.0.302`.
- Official NuGet version-index check for `Microsoft.SourceLink.GitHub` -- expected: configured `10.0.301` exists and `10.0.302` is not configured.
- `git diff --check` and repository-local word/byte diff inspection -- expected: only classified corrections/current-reference changes, with any inherited whitespace warning documented rather than silently normalized.

## Results

- Restored 249 provenance-bearing files exactly to their repository pre-change baselines and restored the dated SDK observation in the mixed Memories record without reverting concurrent commits or parent pointers.
- Kept active SDK pins, CI/action inputs, current assertions, and living guidance on `10.0.302`; corrected the root release date to July 14, 2026 and clarified mixed historical/current statements that a literal substitution made contradictory.
- Set `Microsoft.SourceLink.GitHub` to published version `10.0.301`; no `10.0.302` SourceLink pin remains.
- Audited the root plus all 13 root-declared submodules. Remaining `10.0.300`/`10.0.301` occurrences are provenance, explicitly labeled historical observations in living documents, the SourceLink exception, or this workflow explanation.
- `dotnet --version` resolved `10.0.302` in the root, EventStore, Folders, Conversations, Projects, Parties, and Timesheets repositories.
- Effective baseline-relative diff checks passed except for CRLF lines reported by Git as trailing whitespace in Projects `global.json` and the Builds SourceLink catalog. The corrective working-tree diff additionally reports restored CRLF lines in three Projects historical artifacts. All of those line endings predate this correction and were preserved. The focused root architecture-test restore remains blocked by pre-existing missing central package versions/project references and unrelated Conversations compilation errors.
- Fresh Blind Hunter and Edge Case Hunter reviews completed with no remaining findings.

## Suggested Review Order

**Intent and semantic boundary**

- Start with the approved distinction between active SDK controls and immutable evidence.
  [`spec-update-dotnet-sdk-to-10-0-302.md:16`](spec-update-dotnet-sdk-to-10-0-302.md#L16)

**Current SDK baseline**

- The root executable pin establishes SDK 10.0.302 for the repository.
  [`global.json:3`](../../global.json#L3)

- Shared setup-dotnet consumers inherit the same stable SDK default.
  [`action.yml:11`](../../references/Hexalith.Builds/Github/initialize-dotnet/action.yml#L11)

- Living architecture records the verified July release date.
  [`architecture.md:279`](../planning-artifacts/architecture.md#L279)

**Independent dependency versioning**

- SourceLink stays on its published package version instead of mirroring the SDK.
  [`Directory.Packages.props:209`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L209)

**Historical provenance**

- Timesheets separates its observed SDK from the current repository pin.
  [`architecture.md:152`](../../references/Hexalith.Timesheets/_bmad-output/planning-artifacts/architecture.md#L152)

- Memories keeps current guidance and dated preflight evidence distinct.
  [`27-1-access-telemetry-retention-ownership-decision.md:153`](../../references/Hexalith.Memories/_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md#L153)

- The dated Memories preflight retains the SDK actually observed.
  [`27-1-access-telemetry-retention-ownership-decision.md:293`](../../references/Hexalith.Memories/_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md#L293)

- Conversations labels its captured preview SDK while naming the current stable pin.
  [`architecture.md:382`](../../references/Hexalith.Conversations/_bmad-output/planning-artifacts/architecture.md#L382)

**Assertions and verification**

- Root architecture coverage asserts the active global.json value.
  [`ScaffoldArchitectureTests.cs:648`](../../tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs#L648)

- Conversations smoke coverage asserts its repository-local pin.
  [`ScaffoldSmokeTest.cs:49`](../../references/Hexalith.Conversations/tests/Hexalith.Conversations.IntegrationTests/ScaffoldSmokeTest.cs#L49)

- Verification results summarize provenance, resolution, diff, and review evidence.
  [`spec-update-dotnet-sdk-to-10-0-302.md:86`](spec-update-dotnet-sdk-to-10-0-302.md#L86)
