---
title: 'Update .NET SDK references to 10.0.302'
type: 'chore'
created: '2026-07-16'
status: 'in-review'
review_loop_iteration: 0
baseline_commit: '2ab241944b02d2cc98ee33a4d193cbf869c58483'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The root repository and several root-declared submodules still contain tracked text references to .NET SDK versions `10.0.300` or `10.0.301`, leaving SDK pins, CI inputs, tests, active documentation, and historical artifacts inconsistent with the requested `10.0.302` baseline.

**Approach:** Replace every exact tracked-text occurrence of `10.0.300` and `10.0.301` with `10.0.302` in the root repository and every initialized root-declared submodule, then prove with repository-local scans that neither old value remains.

## Boundaries & Constraints

**Always:** Cover all tracked text files, including hidden configuration, `global.json`, CI workflows, package-version declarations, source/tests, documentation, planning artifacts, implementation artifacts, and evidence files. Treat each root-declared submodule as its own repository, preserve its existing formatting and line endings, and leave already-current `10.0.302` references unchanged. The investigation baseline is 539 replacements across 304 files in nine repositories: root ChatBot, EventStore, Folders, Conversations, Projects, Parties, Memories, Builds, and Timesheets; Tenants, FrontComposer, AI.Tools, Commons, and PolymorphicSerializations currently have no old references.

**Ask First:** Stop if an occurrence can only be changed by entering a nested/non-root submodule, modifying binary content, or overwriting unrelated concurrent work discovered after the clean-tree baseline.

**Never:** Initialize or traverse nested submodules recursively; alter other version numbers; regenerate unrelated artifacts; rewrite Git history; commit, push, or update parent submodule pointers without a separate user request.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Old SDK patch | Exact text `10.0.300` or `10.0.301` | Text becomes `10.0.302` without surrounding edits | Report and stop if the file cannot be safely edited |
| Current SDK patch | Existing exact text `10.0.302` | Text remains unchanged | No action |
| Repository boundary | Root repository or root-declared submodule | Tracked text is included in the scan and replacement | Exclude nested submodule contents and Git metadata |

</frozen-after-approval>

## Code Map

- `global.json` and `references/*/global.json` -- authoritative SDK pins where old values remain.
- `.github/workflows/**` and `references/*/.github/workflows/**` -- setup-dotnet inputs and CI version assertions.
- `references/Hexalith.Builds/Github/**` and `references/Hexalith.Builds/Props/Directory.Packages.props` -- shared action defaults/examples and the SourceLink package pin.
- `_bmad-output/**`, `docs/**`, `README.md`, `CONTRIBUTING.md`, `fable_changes.md`, and equivalent paths inside root-declared submodules -- active and historical tracked version references explicitly included by the user.
- `src/**` and `tests/**` in the affected repositories -- comments, generated evidence fixtures, and assertions that encode the old SDK values.

## Tasks & Acceptance

**Execution:**
- [x] All affected tracked files in the root and root-declared submodules -- replace exact `10.0.300`/`10.0.301` tokens with `10.0.302` while preserving all other bytes and repository boundaries.
- [x] Each affected repository -- inspect diffs and verify occurrence/file totals so the mechanical edit introduces no unrelated changes.
- [x] Root and all root-declared submodules -- run final tracked-text scans and SDK-resolution checks for repositories with `global.json`.

**Acceptance Criteria:**
- Given the root repository and every initialized root-declared submodule, when tracked text is searched for exact `10.0.300` or `10.0.301` values, then the combined result contains zero matches.
- Given the 539 inventoried old-version occurrences, when the replacement is complete, then all 539 resolve to `10.0.302` and no surrounding content changes beyond the workflow spec/status bookkeeping.
- Given repositories with changed `global.json` SDK pins, when `dotnet --version` is run from those repository roots, then SDK resolution reports `10.0.302` successfully.
- Given repository diffs after the edit, when each owning repository is inspected, then only intended token replacements are present and no nested-submodule content or Git metadata has changed.

## Spec Change Log

## Verification

**Commands:**
- `git grep -n -I -E '10\.0\.(300|301)' -- . ':(exclude)references/*'` in the root and each root-declared submodule -- expected: no matches.
- `git diff --word-diff=porcelain` in each changed repository -- expected: removed tokens are only `10.0.300`/`10.0.301`, added replacement tokens are only `10.0.302`, aside from this workflow spec/status.
- `dotnet --version` in each repository whose `global.json` changed -- expected: `10.0.302`.

**Results:**
- PASS -- all 14 repository-local tracked-text scans returned zero old-version matches.
- PASS -- the baseline inventory contained 539 replacements in 304 working-tree files. Byte comparison against transformed `HEAD` content proved 537 replacements across 303 committed-file diffs. The remaining two replacements were in `references/Hexalith.Memories/_bmad-output/implementation-artifacts/27-1-access-telemetry-retention-ownership-decision.md`, an assume-unchanged working file whose committed content was already current; the replacement made its working content match `HEAD`.
- PASS -- root ChatBot, Folders, Conversations, Projects, Parties, and Timesheets each resolved `dotnet --version` to `10.0.302` after their `global.json` updates.
- BLOCKED (supplemental focused test) -- `dotnet test tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --configuration Release` could not restore because existing project-reference/package state lacks central `Fluxor.Blazor.Web` versions and a nested Commons Serialization checkout. The `--no-restore -m:1` fallback also reached pre-existing EventStore/Projects package-version errors and Conversations missing-reference/compiler errors before compiling the test project. The required textual, byte-level, and SDK-resolution checks passed independently.
