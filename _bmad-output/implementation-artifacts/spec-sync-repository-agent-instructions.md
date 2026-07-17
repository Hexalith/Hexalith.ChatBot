---
title: 'Synchronize repository agent instructions'
type: 'chore'
created: '2026-07-17'
status: 'done'
review_loop_iteration: 3
baseline_commit: 'd1c55982d27cea22556c4f59e75294ff9f4c65ba'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Root and submodule agent entry points are not location-independent: copied relative links break outside the superproject root, and repository line-ending policies prevent durable byte equality. The first universal rewrite also removed the mandatory `hexalith-llm-instructions.md` pre-work requirement, changing agent behavior.

**Approach:** Use a location-independent bootstrap rule in the superproject `CLAUDE.md`: read the local baseline when present, otherwise locate the enclosing superproject with Git and read its root-declared AI.Tools copy. Mirror its normalized text into the Codex, Claude, and GitHub Copilot entry points in the superproject and every root-declared submodule, respecting each repository's line-ending policy.

## Boundaries & Constraints

**Always:** Keep the universal policy location-independent; require every agent to locate, read, and follow `hexalith-llm-instructions.md` without initializing nested submodules; mirror its normalized text to all 42 in-scope entry points; preserve each repository's configured line endings and all unrelated work; perform changes within the repository that owns each file; snapshot the source hash and root-declared submodule list before editing; verify normalized textual equality afterward.

**Ask First:** Halt if the universal source or root `.gitmodules` declaration set changes during implementation, if an instruction target is already dirty or acquires newly observed user changes, or if a required entry-point parent directory cannot be created safely.

**Never:** Use relative Markdown links in the universal policy; initialize, inspect, or modify nested submodules; use recursive submodule commands; add repository-specific rules to the universal entry points; commit, push, or overwrite unrelated dirty work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Universal policy | Superproject `CLAUDE.md` contains a relative Markdown link | Replace it with link-free, location-independent guidance | Scan for local Markdown targets before mirroring |
| Mandatory baseline | Current repository lacks a root `hexalith-llm-instructions.md` | Resolve the enclosing superproject and read its root-declared AI.Tools copy | Stop and report a blocker if no permitted location exists |
| Drifted entry point | Any in-scope target differs after line-ending normalization | Replace it with the universal policy text in the repository's configured line ending | Compare normalized text |
| Missing entry point | An in-scope target is absent | Create it from the universal policy text | Fail if its parent cannot be created safely |
| Already synchronized | Entry point matches after line-ending normalization | Leave it unchanged | Report it as compliant |
| Unrelated dirty work | Repository contains changes outside instruction targets | Preserve those changes byte-for-byte | Stop if an intended target becomes dirty externally |

</frozen-after-approval>

## Code Map

- `CLAUDE.md` -- universal source policy in the superproject; rewritten to be location-independent.
- `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` -- mandatory baseline located by the universal policy before work begins.
- `AGENTS.md` and `.github/copilot-instructions.md` -- superproject Codex and GitHub Copilot entry points that mirror the universal source.
- `references/*/{AGENTS.md,CLAUDE.md,.github/copilot-instructions.md}` -- the 39 submodule entry points that mirror the universal source text.
- `.gitmodules` -- authoritative boundary containing the 13 eligible submodule paths.

## Tasks & Acceptance

**Execution:**
- [x] `CLAUDE.md` -- replace the relative-link wrapper with a link-free universal policy that preserves the mandatory baseline behavior.
- [x] `AGENTS.md`, `.github/copilot-instructions.md`, and `references/*/{AGENTS.md,CLAUDE.md,.github/copilot-instructions.md}` -- mirror the universal policy text to all 41 remaining in-scope entry points, preserving configured line endings.
- [x] `CLAUDE.md` and all mirrored entry points -- restore the mandatory, location-independent `hexalith-llm-instructions.md` bootstrap rule.
- [x] Root plus all 13 root-declared submodules -- snapshot source/submodule invariants and verify 42 normalized-text comparisons, link independence, line-ending conformance, and preservation of unrelated paths.

**Acceptance Criteria:**
- Given the superproject and every path declared by root `.gitmodules`, when parity is checked after normalizing CRLF/LF, then all 42 Codex, Claude, and GitHub Copilot entry points match the universal source text.
- Given any entry point's location differs from the superproject root, when its policy is read, then no relative Markdown link is required to locate mandatory instructions.
- Given an agent starts work from the superproject, AI.Tools, or another checked-out root submodule, when it follows the universal policy, then it can locate and read the mandatory `hexalith-llm-instructions.md` without initializing a nested submodule.
- Given repositories with distinct `.gitattributes` settings, when the change is checked, then each entry point uses its repository's configured line ending while retaining the same normalized text.
- Given unrelated or concurrent work across the superproject and submodules, when before/after hashes and diffs are reviewed, then the instruction synchronization has not overwritten those paths.
- Given nested submodules under any root-declared submodule, when the change is complete, then none were initialized or modified.

## Spec Change Log

- User correction: the universal rewrite removed the mandatory Hexalith LLM baseline. Replaced the broken relative link with a Git-resolved location-independent bootstrap rule, avoiding behavior loss and nested-submodule initialization.
- Edge-review remediation: require the fallback workspace to declare the AI.Tools submodule in its root `.gitmodules`, preventing a similarly named undeclared directory from supplying the baseline.

## Verification

**Commands:**
- Capture a SHA-256 of `CLAUDE.md`, root `.gitmodules` paths, and pre-existing non-target dirty-path hashes before editing; repeat afterward -- expected: source and declaration snapshots are stable, unrelated hashes unchanged.
- Normalize CRLF/LF to LF and compare the universal source to `AGENTS.md`, `.github/copilot-instructions.md`, and all 39 submodule entry points -- expected: 42 matches.
- Scan universal policy Markdown links and inspect each target with `git check-attr eol` -- expected: no relative Markdown links and each target conforms to its repository's line-ending policy.
- Run `git -c core.whitespace=cr-at-eol diff --check` against instruction paths in every changed repository -- expected: no whitespace errors.

## Initial Suggested Review Order

**Universal baseline**

- Defines location-independent policy and normalized synchronization contract.
  [`CLAUDE.md:1`](../../CLAUDE.md#L1)

- Shows the Codex entry point mirrors the same baseline.
  [`AGENTS.md:1`](../../AGENTS.md#L1)

**Repository-specific preservation**

- Relocates Build-only workflow, architecture, coding, and testing rules.
  [`DEVELOPMENT.md:1`](../../references/Hexalith.Builds/DEVELOPMENT.md#L1)

- Makes the relocated guidance discoverable from repository documentation.
  [`README.md:61`](../../references/Hexalith.Builds/README.md#L61)

**Scope boundary**

- Declares the thirteen eligible submodules for universal-policy mirroring.
  [`.gitmodules:1`](../../.gitmodules#L1)

## Suggested Review Order

**Mandatory baseline bootstrap**

- Restores required baseline loading without fragile relative links.
  [`CLAUDE.md:9`](../../CLAUDE.md#L9)

- Establishes the trusted root-declared AI.Tools lookup boundary.
  [`CLAUDE.md:19`](../../CLAUDE.md#L19)

**Exact entry-point mirroring**

- Shows Codex receives the same normalized universal policy.
  [`AGENTS.md:1`](../../AGENTS.md#L1)

- Confirms the baseline source used by the bootstrap exists.
  [`hexalith-llm-instructions.md:1`](../../references/Hexalith.AI.Tools/hexalith-llm-instructions.md#L1)

**Repository-specific preservation**

- Keeps Build-only workflow and coding rules outside universal entry points.
  [`DEVELOPMENT.md:1`](../../references/Hexalith.Builds/DEVELOPMENT.md#L1)

- Keeps the fallback constrained to a declared root submodule.
  [`.gitmodules:1`](../../.gitmodules#L1)
