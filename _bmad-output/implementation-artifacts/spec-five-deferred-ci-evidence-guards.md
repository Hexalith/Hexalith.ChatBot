---
title: 'Resolve five deferred CI evidence guard items'
type: 'bugfix'
created: '2026-10-22'
status: 'in-review'
route: 'dispatch'
baseline_commit: 'b16a185f079055f3022487abe05e0cd1e02d7c1b'
review_loop_iteration: 0
context: ['docs/story-evidence-integrity.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred entries concern confidence in CI evidence: Linux-specific gate coverage (DW-28), unexecuted shell refusal branches (DW-32), an uncoded completion reserve refusal (DW-90), first-binding assumptions (DW-91), and the ordinary lane zero-test boundary (DW-130).

**Approach:** Harden and exercise the CI evidence boundary, document its supported platform, and verify the existing zero-test fix before closing exactly these five entries. The user explicitly requests five deferred items and authorizes committing and pushing the finished changes on main.

## Boundaries & Constraints

**Always:** Preserve exact event revisions, the zero/empty push-base-only fallback, empty schedule/manual ranges, metadata-only output, same-repository retained downloads, strict policy pinning, all-lane attestation preflight, current freshness ceilings, and nine stable TE-2 reasons. Use a separate stable workflow refusal code for the reserve. Keep the current fixed 2400-second reserve threshold. Maintain approved bindings exactly; matching multiple bindings does not authorize new policy lanes. Stage only this task's files.

**Never:** Change dependencies, submodules, domain behavior, release qualifications, branch protection, or the policy's approved lane set. Do not initialize nested submodules or deploy. Do not count Linux tests as Windows evidence.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected behavior |
| --- | --- | --- |
| Event selection | PR, ordinary push, zero/empty-base push, schedule, manual | Exact full base/head; fallback only for zero/empty base; non-transition events use head..head |
| Event refusal | Missing nonzero push base, checkout mismatch, unsupported event | Nonzero exit; no successful outputs |
| Retained evidence | Safe same-repository locator, duplicates | Download exact named artifact once to isolated destination |
| Retained refusal | Unsafe locator, foreign repository, destination collision | Fail before unsafe download; no command injection |
| Reserve | Elapsed 2399 / 2400 seconds | Admit below threshold; refuse at threshold with retained machine-readable stable code |
| Binding selection | Two synthetic approved bindings in either order | Resolve by exact lane; refuse missing, duplicate, malformed, or cross-bound selection |
| Ordinary lane | Real xUnit filter matching no tests | Boundary refuses even if runner exits zero; no success checksum |

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml`: bounds and retained-artifact shell guards are inline in story-evidence-integrity; reserve runs before recovery production. Extract guards into repository-owned Bash, preserve env inputs and output names, and keep workflow connected to those scripts.
- `.github/scripts/run-merge-test-lanes.sh`, `run-xunit-v4.sh`: already require nonzero executed CTRF tests; use existing seam instead of another runner.
- `tools/Hexalith.ChatBot.StoryEvidenceGate/StoryEvidenceValidator.cs`: production declaration preflight around line 327 and primary-path validation around line 870 select binding index zero. `ValidatePinnedPolicy` intentionally permits exactly one approved binding today and must retain that rule.
- `PrimaryLaneBinding.cs`: reuse complete validation and add an internal exact-lane selector, rejecting ambiguous/missing/malformed binding entries. Use it in both consumers; binding metadata must follow its lane in primary-path validation.
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs`: add direct synthetic-policy tests for multiple bindings without loosening production pinning.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`: source assertions currently demand inline shell text; update only affected wiring assertions to read owning script and retain useful guard checks.
- `ReleaseWorkflowSafetyTests.cs`: contains real zero-test coverage but depends on a large architecture build graph. A Python unittest script suite may execute Bash with temporary Git repos and fake gh/date processes, avoiding network or domain dependencies. Wire it into CI and run locally.
- `docs/story-evidence-integrity.md`: record Linux-only enforcing CI (ubuntu-latest); Windows-specific skipped semantics are unsupported, not passing evidence. Describe shell checks and reserve diagnostics.

## Tasks & Acceptance

**Execution:**
- [x] `.github/scripts/resolve-story-evidence-bounds.sh`, `collect-story-evidence-artifacts.sh`, `check-story-evidence-reserve.sh` — extract executable guards and add a bounded JSON reserve report with code `completion_reserve_exhausted`; preserve threshold and failed exit semantics.
- [x] `.github/scripts/tests/test_story_evidence_guards.py`, `.github/workflows/ci.yml` — execute the matrix without external network, include a real zero-test runner refusal, and wire the tests into CI.
- [x] `PrimaryLaneBinding.cs`, `StoryEvidenceValidator.cs`, `StoryEvidenceGateTests.cs` — select exactly one binding by lane and test order independence and refusals while retaining pinned-policy mutation protection.
- [x] `ScaffoldArchitectureTests.cs`, `docs/story-evidence-integrity.md` — preserve wiring guards after extraction, document supported platform and workflow refusal code.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` — close only DW-28/32/90/91/130 after named verification; parent handles ledger and final evidence tracking.

**Acceptance Criteria:**
- Given the workflow calls the extracted guards, when executable tests run, then all matrix cases pass and failures have no successful output/download authority.
- Given multiple synthetic bindings, when a lane is chosen, then its own metadata is used independently of order and malformed selection fails closed.
- Given the existing ordinary runner, when it executes zero tests, then the wrapper rejects it and no successful checksum exists.
- Given verified changes, when finalized, then exactly five ledger entries close and the authorized main push contains only this work.

## Implementation Notes

User authorization covers the full batch and final main push. No human-only intent gaps or irreversible application changes were found. Parent is running the required Aspire baseline; this task changes CI scripts/gate tooling, not app topology. Implementation agent should complete code, docs, focused tests, and report exact results, leaving Git operations and ledger closure to parent. Preserve CRLF for C#/Markdown and LF for Bash/YAML/Python. Focused Debug build and direct native xUnit runner are appropriate. Do not broaden into dependency or unrelated architecture repairs.

## Spec Change Log

## Review Triage Log

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| BH-1: semantic validation of unselected bindings | low / rejected | All production entry points pin the single approved binding before reaching selection; selected binding metadata is subsequently validated against its result. Structural malformed unselected entries already refuse. Broader hypothetical policy semantics add guards without a reachable production defect. |
| BH-2: PR and push heads identical in fixture | medium / patch | Fixture could not detect the wrong event-head source. Use an available distinct push revision while checkout and PR head remain the proposed head. |
| BH-3: only one unique retained artifact succeeds | medium / patch | Parallel array indexing and per-run deduplication lacked a multi-element regression. Assert three exact artifact downloads across two runs, including a shared name. |
| BH-4: unsafe destinations lack earlier valid locator | medium / patch | Add earlier valid destination to collision/ancestry tests and verify all destinations preflight before any download. |
| BH-5: threshold-only refusal coverage | medium / patch | Equality-only threshold mutation would pass. Verify 2400, 2401, and 86400 seconds retain the same refusal code and no closeout authority. |
| BH-6: inherited Git fixture configuration | medium / patch | User signing, hooks or template configuration could make tests fail or execute unrelated programs. Isolate fixture Git configuration and explicitly disable signing/hooks/templates; exercise hostile ambient configuration. |
| BH-7: synthetic fixtures omit freshness overrides | low / rejected | Freshness resolution is unchanged; existing PerLaneCurrentRunCeilingShouldRaiseOnlyForDeclaringLanes and range/presence/disagreement cases exercise override behavior, while full binding freshness tests retain both acceptance and atomic refusal. Additional synthetic ceilings add little evidence for the changed selection consumers. |
| BH-8: observed LaneResult negatives absent | medium / patch | Valid declarations could hide lost observed-result guards. Add selector/source/class mismatch and zero-executed/failed/skipped cases against ValidatePrimaryPaths. |
| BH-9: synthetic merge omits shell suite | low / patch | Merge and exact-head jobs should exercise the same added guard suite. Invoke it using the synthetic merge's built Release runner. |

Edge-case hunter returned no findings. Verification-gap reviewer returned no findings after tracing the consumers, executable scripts, and passing test matrix.

## Verification

- `python3 -m unittest discover -s .github/scripts/tests -p 'test_story_evidence_guards.py' -v` — executable guard matrix.
- Debug build `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj`, then native runner via `.github/scripts/run-xunit-v4.sh` with zero skips, CTRF and TRX.
- Focused architecture build/run if available; record broad dependency blockers separately and use focused compilation where feasible.
- Whitespace: `git -c core.whitespace=cr-at-eol diff --check`; independent build workflow review; commitlint; fetch/divergence check; push main.


Verification completed: `dotnet build tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj --configuration Debug -m:1 --nologo` and the equivalent Architecture.Tests build both passed with zero warnings/errors. Native gate tests passed 299/299 with zero skips; four focused architecture wiring/zero-test checks passed with zero skips. CTRF/TRX and checksums are under `TestResults/five-deferred-ci/`. The Python executable suite passed 11/11, covering all shell matrix rows, real zero-test runner refusal, invalid clocks, download failure, and symlink/file destination ancestry. Bash syntax and CRLF-aware whitespace checks pass.

The baseline `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2 after a successful Debug build (zero warnings/errors), because the AppHost exits 134 when ChatBot:LiveRecoveryValidation:MailboxClientSecret is absent. No running AppHost was left. Formal terminal completion would require a current recovery-primary lane because this change edits ci.yml; the build record stays in-review pending that evidence. Exactly the five individually verified deferred entries are closed. No terminal story/technical-enabler transition or release qualification is claimed.

Final verification after review patches: the full native gate suite passes 305/305 with zero failures/skips (`TestResults/five-deferred-ci/final-gate.ctrf.json` and `.trx`); all 12 executable Python tests pass; the four focused architecture checks pass with zero failures/skips (`final-architecture.ctrf.json` and `.trx`). Both native wrappers validate the catalog-pinned runner and checksum evidence. Actionlint, Bash syntax, staged whitespace, and commitlint pass. All three independent review layers completed; seven findings were patched, two low-risk suggestions were rejected with evidence, and no new work was deferred. Remote fetch found main at the unchanged full baseline revision with zero local/remote divergence. The final commit and authorized push are the remaining repository operations.

## File List

- `.github/scripts/check-story-evidence-reserve.sh`
- `.github/scripts/collect-story-evidence-artifacts.sh`
- `.github/scripts/resolve-story-evidence-bounds.sh`
- `.github/scripts/tests/test_story_evidence_guards.py`
- `.github/workflows/ci.yml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-ci-evidence-guards.md`
- `docs/story-evidence-integrity.md`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/PrimaryLaneBinding.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/StoryEvidenceValidator.cs`
