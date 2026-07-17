---
title: 'Synchronize repository agent instructions'
type: 'chore'
created: '2026-07-17'
status: 'in-review'
review_loop_iteration: 0
baseline_commit: '1b529f42594b03ba73f9d870e667ad76a8020e29'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Root agent entry points have drifted across the umbrella repository and its root-declared submodules, so Codex, Claude, and GitHub Copilot can receive different instructions in the same repository.

**Approach:** Treat each repository's root `CLAUDE.md` as its canonical source and make `AGENTS.md` plus `.github/copilot-instructions.md` byte-for-byte identical to it. Apply this only to the root repository and the 13 `references/...` submodules declared by the root `.gitmodules`.

## Boundaries & Constraints

**Always:** Preserve each repository's `CLAUDE.md`; create a missing Codex or Copilot entry point when needed; preserve all unrelated pre-existing changes; perform changes within the repository that owns each file; verify exact byte equality rather than normalized textual similarity.

**Ask First:** Halt if a declared repository lacks a readable `CLAUDE.md`, if an instruction target contains newly observed user changes, or if satisfying equality would require changing the canonical `CLAUDE.md`.

**Never:** Initialize, inspect, or modify nested submodules; use recursive submodule commands; edit instruction files outside the root repository and its root-declared submodules; commit, push, or overwrite unrelated dirty work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Drifted entry point | `AGENTS.md` or Copilot instructions differ from `CLAUDE.md` | Replace the target with the exact canonical bytes | Verify with `cmp` |
| Missing entry point | Codex or Copilot instruction file is absent | Create it with the exact canonical bytes | Fail if its parent cannot be created safely |
| Already synchronized | Both targets match `CLAUDE.md` | Leave the repository unchanged | Report it as compliant |
| Unrelated dirty work | Repository contains changes outside instruction targets | Preserve those changes byte-for-byte | Stop if an intended target becomes dirty externally |

</frozen-after-approval>

## Code Map

- `CLAUDE.md` and `references/*/CLAUDE.md` -- canonical per-repository instruction sources; read-only for this change.
- `AGENTS.md` and `references/*/AGENTS.md` -- Codex entry points that must mirror the canonical file.
- `.github/copilot-instructions.md` and `references/*/.github/copilot-instructions.md` -- GitHub Copilot entry points that must mirror the canonical file.
- `.gitmodules` -- authoritative boundary containing the 13 eligible submodule paths.

## Tasks & Acceptance

**Execution:**
- [x] `references/{Hexalith.EventStore,Hexalith.FrontComposer,Hexalith.Folders,Hexalith.Conversations,Hexalith.Parties,Hexalith.Memories}/.github/copilot-instructions.md` -- replace drifted Copilot content with the owning repository's `CLAUDE.md` bytes.
- [x] `references/{Hexalith.Commons,Hexalith.Builds,Hexalith.Timesheets}/AGENTS.md` -- replace drifted Codex content with the owning repository's `CLAUDE.md` bytes.
- [x] `references/{Hexalith.Commons,Hexalith.Builds,Hexalith.Timesheets,Hexalith.PolymorphicSerializations}/.github/copilot-instructions.md` -- create or replace Copilot content from the owning repository's `CLAUDE.md` bytes.
- [x] Root plus all root-declared submodules -- verify both target entry points exist and compare byte-for-byte equal to `CLAUDE.md`; confirm unrelated dirty paths were not overwritten by this change.

**Acceptance Criteria:**
- Given the root repository and every path declared by root `.gitmodules`, when instruction parity is checked, then `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md` exist and have identical bytes within each repository.
- Given repositories that were already compliant, when the change is complete, then they have no instruction-file diff.
- Given the approved pre-existing changes in the root, `Hexalith.Builds`, and `Hexalith.Timesheets`, when diffs are reviewed, then the instruction synchronization does not overwrite those paths and any concurrent user edits remain intact.
- Given nested submodules under any root-declared submodule, when the change is complete, then none were initialized or modified.

## Spec Change Log

## Design Notes

- `Hexalith.Commons` and `Hexalith.PolymorphicSerializations` contained dangling Copilot symlinks. They were replaced with regular files so the entry points are readable and byte-identical to each repository's `CLAUDE.md`.
- Canonical CRLF line endings were retained where present; whitespace verification therefore uses Git's `cr-at-eol` handling.

## Verification

**Commands:**
- Enumerate `.gitmodules` paths and run `test -f` plus `cmp -s` for each repository's three entry points -- expected: all 14 repositories pass.
- Run `git -c core.whitespace=cr-at-eol diff --check` against instruction paths in every changed repository -- expected: no whitespace errors after treating canonical CRLF endings as line terminators.
- Review `git status --short` and instruction-only diffs in every changed repository -- expected: only intended instruction paths are new/modified in addition to the approved pre-existing work.
