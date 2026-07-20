---
title: 'Validate Timesheets from a standalone checkout'
type: 'validation'
created: '2026-07-19'
updated: '2026-07-20'
status: 'ready'
route: 'one-shot'
supersedes: 'ChatBot-umbrella initialization of Timesheets dependencies'
---

# Validate Timesheets from a standalone checkout

## Intent

**Problem:** The prior evidence initialized `Hexalith.Works` from `references/Hexalith.Timesheets` inside the ChatBot umbrella. Although Works is declared by Timesheets and was initialized without recursion, that location is not canonical independent-consumer evidence because Timesheets is a dependency checkout beneath ChatBot.

**Canonical approach:** Create an isolated standalone Timesheets checkout at the exact Timesheets gitlink pinned by the recorded ChatBot baseline. In that checkout, Timesheets is the repository root. Initialize only paths declared by Timesheets' root `.gitmodules`, using explicit pathspecs and no `--recursive` or `--remote`. This permits Timesheets' own root-declared `Hexalith.Works` checkout. Do not initialize any submodule declared by Works or by another initialized Timesheets dependency.

## Pinned baseline

- ChatBot observation baseline: `bd652e3c61ebfa0202f6a1fdb696759637a21bca`.
- Timesheets gitlink: `441f02509cfd43c888e2d4317a167b41657208b4`.
- Timesheets root-declared `Hexalith.Works` gitlink: `f2259daab922096113262fc9e0a5588182918e0a`.
- Timesheets root-declared `references/Hexalith.Builds` gitlink: `f0750ca703cc3ada6eb25050cb6b287e83ce3938`.

Before running, record the then-current ChatBot `HEAD` and verify that its Timesheets gitlink still equals the value above. A later documentation-only ChatBot commit may be used when the Timesheets gitlink is unchanged; any Timesheets gitlink change requires a new standalone checkout and new evidence.

## Required procedure

1. Clone `https://github.com/Hexalith/Hexalith.Timesheets.git` without recursing into submodules, outside the ChatBot worktree.
2. Detach the standalone checkout at `441f02509cfd43c888e2d4317a167b41657208b4`.
3. Record `git rev-parse HEAD`, the empty result from `git rev-parse --show-superproject-working-tree`, and a clean `git status --short`.
4. Read the standalone root `AGENTS.md`, repository build guidance, `.gitmodules`, solution, and test instructions.
5. Resolve the direct path list from that root `.gitmodules`; initialize required direct paths with explicit pathspecs and `git submodule update --init -- <path>...`. Never add `--recursive` or `--remote`. `Hexalith.Works` is an allowed direct path in this standalone checkout.
6. Record every initialized direct path and checked-out SHA. Verify `Hexalith.Works` is exactly `f2259daab922096113262fc9e0a5588182918e0a`, `references/Hexalith.Builds` is exactly `f0750ca703cc3ada6eb25050cb6b287e83ce3938`, and every dependency owned below those direct paths remains uninitialized.
7. Run Timesheets' documented canonical `.slnx` restore and serialized warning-as-error Release build, its package/consumer and resolved-graph validation, `Hexalith.Timesheets.Server.Tests`, and `Hexalith.Timesheets.Works.Tests` using the owning repository's documented runner conventions.
8. Record exact commands, exit codes, warnings/errors, passed/failed/skipped test totals, effective package values, and any blocker. A blocked or unrun required lane is not PASS evidence.
9. Finish with clean `git status --short` and `git diff --check`. Do not edit `.gitmodules`, remove dependency projects from `Hexalith.Timesheets.slnx`, change a gitlink, or create a source workaround for validation.

## Evidence acceptance

Timesheets is PASS only when identity/isolation, direct-root-dependency, canonical restore/build, focused tests, package-governance, resolved-graph, and clean-tree evidence are all green at the same pinned commit. The 2026-07-19 Works initialization performed inside the ChatBot umbrella remains historical diagnostic evidence and does not satisfy Story 1.1e's independent-validation gate.
