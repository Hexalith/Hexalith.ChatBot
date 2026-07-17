---
title: 'Synchronize repository agent instructions'
type: 'chore'
created: '2026-07-17'
status: 'in-review'
review_loop_iteration: 2
baseline_commit: '65afdff4e99dba099adbccf45e299fc5713df2c6'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Agent entry points have drifted across the umbrella repository's root-declared submodules, so Codex, Claude, and GitHub Copilot do not receive the same superproject policy everywhere.

**Approach:** Treat the superproject's `./CLAUDE.md` as the single canonical source and make every root-declared submodule's `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md` byte-for-byte identical to that one file. The superproject's three entry points are already identical and remain the source baseline.

## Boundaries & Constraints

**Always:** Copy the exact superproject `CLAUDE.md` bytes, including its links and CRLF endings, into all three entry points of every root-declared submodule; create a missing entry point when needed; preserve all unrelated work; perform changes within the repository that owns each file; verify exact byte equality rather than normalized textual similarity.

**Ask First:** Halt if the superproject `CLAUDE.md` changes during implementation, if an instruction target acquires newly observed user changes, or if the root `.gitmodules` declaration set changes.

**Never:** Initialize, inspect, or modify nested submodules; use recursive submodule commands; edit instruction files outside the root repository and its root-declared submodules; commit, push, or overwrite unrelated dirty work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Drifted entry point | Any submodule target differs from superproject `CLAUDE.md` | Replace the target with the superproject's exact canonical bytes | Verify with `cmp` against the superproject file |
| Missing entry point | A submodule target is absent | Create it with the superproject's exact canonical bytes | Fail if its parent cannot be created safely |
| Already synchronized | All three submodule targets match superproject `CLAUDE.md` | Leave that submodule unchanged | Report it as compliant |
| Unrelated dirty work | Repository contains changes outside instruction targets | Preserve those changes byte-for-byte | Stop if an intended target becomes dirty externally |

</frozen-after-approval>

## Code Map

- `CLAUDE.md` -- sole canonical instruction source in the superproject; read-only for this change.
- `references/*/AGENTS.md` -- submodule Codex entry points that must mirror the superproject source.
- `references/*/CLAUDE.md` -- submodule Claude entry points that must mirror the superproject source.
- `references/*/.github/copilot-instructions.md` -- submodule GitHub Copilot entry points that must mirror the superproject source.
- `.gitmodules` -- authoritative boundary containing the 13 eligible submodule paths.

## Tasks & Acceptance

**Execution:**
- [x] `references/{Hexalith.EventStore,Hexalith.Tenants,Hexalith.FrontComposer,Hexalith.Folders,Hexalith.Conversations,Hexalith.Parties,Hexalith.AI.Tools,Hexalith.Memories,Hexalith.Commons,Hexalith.Builds,Hexalith.Timesheets,Hexalith.PolymorphicSerializations}/{AGENTS.md,CLAUDE.md,.github/copilot-instructions.md}` -- replace all 36 non-compliant targets with the superproject `CLAUDE.md` bytes.
- [x] `references/Hexalith.Projects/{AGENTS.md,CLAUDE.md,.github/copilot-instructions.md}` -- confirm the already-compliant submodule remains unchanged.
- [x] All 13 root-declared submodules -- verify all three target files exist and compare byte-for-byte equal to the superproject `CLAUDE.md`; confirm unrelated dirty paths were not overwritten.

**Acceptance Criteria:**
- Given every submodule path declared by root `.gitmodules`, when instruction parity is checked, then its `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md` exist and each has bytes identical to the superproject `CLAUDE.md`.
- Given `Hexalith.Projects` is already compliant, when the change is complete, then it has no instruction-file diff.
- Given unrelated or concurrent work across the superproject and submodules, when diffs are reviewed, then the instruction synchronization does not overwrite those paths.
- Given nested submodules under any root-declared submodule, when the change is complete, then none were initialized or modified.

## Spec Change Log

## Verification

**Commands:**
- Enumerate the 13 root `.gitmodules` paths and run `test -f` plus `cmp -s` from superproject `CLAUDE.md` to each of the three submodule entry points -- expected: all 39 comparisons pass.
- Run `git -c core.whitespace=cr-at-eol diff --check` against instruction paths in every changed submodule -- expected: no whitespace errors after treating the canonical CRLF endings as line terminators.
- Review `git status --short` and instruction-only diffs in every changed repository -- expected: only intended instruction paths are new/modified in addition to the approved pre-existing work.
