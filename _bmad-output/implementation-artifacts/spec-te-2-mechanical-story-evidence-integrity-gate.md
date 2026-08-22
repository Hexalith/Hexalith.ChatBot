---
title: 'TE-2 Mechanical Story-Evidence Integrity Gate'
type: 'chore'
created: '2026-08-22'
status: 'in-review'
review_loop_iteration: 2
baseline_commit: 'bef15d1caae4ea50a4d92f7de46e77c79315bcfb'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** TE-2 proves a fixed implementation diff, so its bootstrap can pass while a later lifecycle-only `review` to `complete` event cannot. Adjacent parser, process, provenance, and transition gaps can also weaken or stall the required check.

**Approach:** Adopt a policy/contract v2 `snapshot-plus-transition` mode: hash the exact committed HEAD state of every owned TE-2 path, bind fresh evidence to that snapshot, and separately validate the narrow lifecycle event semantically. Harden the existing gate without replacing its digest, report, reason-code, or checked-item architecture.

## Boundaries & Constraints

**Always:** Preserve 13 product epics, 112 product stories, historical terminal records, exact full Git revisions, deterministic mode-aware SHA-256 digests, metadata-only output, stable reason codes, current/retained provenance, and root-declared-submodule boundaries. Keep TE-2, its ledger record, and sprint action open until an administrator first makes `story-evidence-integrity` required; the later completion change must pass through that protection.

**Ask First:** Any policy weakening, new dependency, broader lifecycle mutation allowance, external branch/ruleset mutation, privileged GitHub integration, compatibility layer, or change to product/UX/runtime behavior.

**Never:** Use historical-range replay, a split activation enabler, an old report as primary completion proof, mixed-scope waivers, generalized mutation-policy syntax, Git-remote identity inference, GitHub administration tokens, automatic status mutation, nested-submodule operations, dependency updates, commits, or pushes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| V2 bootstrap | `bootstrap: true`, full clean HEAD snapshot, fresh passing TRX | Validate snapshot without requiring terminal lifecycle mutations; records remain open | Fail closed on scope, digest, result, or provenance mismatch |
| Delayed completion | Required check active; exact spec, contract, TE ledger, and sprint-action transition | Hash current HEAD snapshot, validate fresh evidence, and accept the four-path semantic delta | Reject missing, extra, or unauthorized mutations |
| Snapshot drift | Missing/changed mode, bytes, symlink, gitlink, index, worktree, or untracked state | Recompute exact snapshot; no disclosure waiver | `file_list_diff_mismatch`, `gitlink_scope_mismatch`, or `scope_digest_mismatch` |
| Invalid evidence | Spoofed selector, stale/future TRX, foreign locator, failed/skipped/malformed results | No provenance minted and no completion accepted | `machine_results_invalid` or `evidence_stale_or_unbound` |
| Inactive malformed contract | No related candidate transition | Ignore it; unrelated/no-transition evaluation succeeds | Active or changed malformed contract fails closed |

</frozen-after-approval>

## Code Map

- `story-evidence-policy.json`, `_bmad-output/implementation-artifacts/evidence/te-2-mechanical-story-evidence-integrity-gate.json` -- v2 grammar, repository/freshness pins, snapshot mode, and exact four transition paths.
- `tools/Hexalith.ChatBot.StoryEvidenceGate/{EvidenceJson,ScopeEvaluator,ScopeEvaluation,StoryEvidenceValidator}.cs` -- strict schema, full immutable HEAD snapshot, event-path reporting, digest masking, and reconciliation reuse.
- `tools/Hexalith.ChatBot.StoryEvidenceGate/LifecycleTransitionValidator.cs` -- new focused comparator for exact spec/contract/ledger/action mutations and frozen-block preservation.
- `tools/Hexalith.ChatBot.StoryEvidenceGate/{TransitionDetector,CommandArguments,Program}.cs` -- candidate-first activation, per-command option allowlists, and protected `ci` entry point.
- `tools/Hexalith.ChatBot.StoryEvidenceGate/{GitReader,TrxEvidenceReader,ProvenanceAttestor}.cs` -- bounded concurrent process draining, canonical TRX identities, freshness, and repository binding.
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/{GateFixture,StoryEvidenceGateTests}.cs` -- snapshot/delta, lifecycle mutation, process, TRX, contract activation, and executable CLI matrix.
- `.github/workflows/ci.yml`, `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`, `docs/story-evidence-integrity.md` -- retain producer-head/artifact sequencing and document that no-transition evaluation waits for but does not consume topology evidence.

## Tasks & Acceptance

**Execution:**
- [x] Policy/contract/scope -- bump mandatory grammar to v2; snapshot every owned path from exact clean HEAD, expose event-path count separately, strictly mask only `scope.implementationDigest`, and retain diff mode without compatibility scaffolding.
- [x] Lifecycle/transition/CLI -- enforce the exact four-path semantic transition, byte-identical frozen intent, candidate-first contract loading, and strict command options.
- [x] Git/TRX/provenance -- drain process streams concurrently under the timeout; resolve selectors through `testId` and `TestMethod`; require fresh current-run times and policy-bound retained repository identity.
- [x] Tests/docs/evidence -- cover every matrix failure, real `ci` no-transition/valid/invalid paths, refresh runbook and architecture pins, and produce v2 bootstrap evidence while leaving TE-2 open.

**Acceptance Criteria:**
- Given a clean v2 bootstrap HEAD, when the gate attests and validates fresh tests, then the complete owned snapshot passes without proposing terminal status.
- Given branch protection is active and only the four authorized lifecycle records change, when `ci` evaluates completion, then exact HEAD evidence passes; any frozen-intent, unrelated ledger/action, contract, or path mutation fails deterministically.
- Given no candidate transition, when an unchanged inactive contract is malformed, then detection returns no transition; changed or selected malformed evidence fails closed.
- Given spoofed TRX identity, stale/future results, foreign retained provenance, unknown CLI options, or blocked Git streams, when evaluated, then the gate fails within its bound using stable metadata-only reasons.

## Spec Change Log

- 2026-08-03: Implemented the approved TE-2 repository gate, self-validation contract, planning hold, machine-result CI path, and developer runbook. Frozen intent remained unchanged.
- 2026-08-03: Hardened the gate after audit: canonical BMAD story grammar, actual-result selectors, primary-lane class/skip binding, exact two-sided gitlinks, retained provenance immutability/source locators, policy-bound event revisions/exclusions, and normal technical-enabler completion detection. Frozen intent remained unchanged.
- 2026-08-03: Closed final immutable-scope and lifecycle edge cases: immutable validation rejects any index/worktree/untracked drift before hashing, and product completion rejects a sprint entry that was already `done` at the base revision. Frozen intent remained unchanged.
- 2026-08-03: Applied the adversarial review patch: exact raw producer heads and retained-artifact collection, collision-safe lanes, fence-aware/ambiguity-safe parsing, policy-bound primary claims/selectors/sources, exact Git tree/index/symlink digest semantics, explicit owned-gitlink scopes at any root-declared depth, internally reconciled TRX results, safe locator/path/metadata handling, and same-version policy mutation guards. Frozen intent remained unchanged.
- 2026-08-03: Corrected CI artifact scoping after final audit: exact-bound transition detection now precedes artifact collection, inactive retained contracts cannot block unrelated/no-transition runs, and topology success/download/head verification is required only for active contracts declaring the current-run Aspire/Dapr primary lane; the general producer remains mandatory. Frozen intent remained unchanged.
- 2026-08-03: Replaced synthetic policy-v1 primary selectors with exact shipped browser, SignalR, hosting/assets, Aspire/Dapr, and recovery test classes; fixture TRX names, architecture pins, and runbook producer mappings now prove the bound classes are executable. Frozen intent remained unchanged.
- 2026-08-22: Review exposed that diff membership made delayed activation impossible and found adjacent fail-closed gaps. Replanned around v2 HEAD snapshot plus semantic lifecycle delta; avoid historical replay and preserve exact hashing, mappings, stable reports/reasons, producer-head checks, and transition-declared artifacts.
- 2026-08-23: Hardened v2 after review: mode-aware primary triggers, root-owned transition paths, historical terminal-record protection, canonical namespace-strict TRX parsing, policy/contract/path preflight, multi-lane and multi-contract attestation atomicity, safe candidate identities, strict CLI allowlists, and fail-closed Git path normalization. Restored the workflow's ambient repository comparison while retaining policy-bound validation in code. Frozen intent remained unchanged.

## Design Notes

`snapshot-plus-transition` reads every `includePath` from the exact HEAD tree and computes the existing sorted repository/path/mode/object digest. `BASE..HEAD` remains a separate event set. Bootstrap validates the full snapshot without terminal mutations; completion requires spec `in-review` to `complete`, contract `bootstrap` true to false plus digest only, TE ledger `review` to `complete`, and sprint action `open` to `done`. All other bytes in those records, especially the frozen block, remain unchanged.

Branch protection is the deliberate administrative bootstrap boundary: enable the required check first, then submit completion. The gate does not acquire control-plane credentials to prove or mutate its own protection.

## Verification

**Commands:**
- `dotnet build tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release -warnaserror --no-restore` -- tool builds cleanly.
- `dotnet test tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj --configuration Release --no-restore --logger "trx;LogFileName=story-evidence-gate.trx" --results-directory TestResults/story-evidence-gate -m:1 /nr:false` -- full focused matrix passes with no skipped tests.
- `dotnet run --project tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release --no-build -- ci ...` -- v2 bootstrap self-validation passes and emits metadata-only reports.
- `actionlint .github/workflows/ci.yml .github/workflows/release.yml` and `git diff --check` -- workflows and scoped changes are clean.

## File List

- `.github/workflows/ci.yml`
- `Hexalith.ChatBot.slnx`
- `README.md`
- `_bmad-output/implementation-artifacts/evidence/te-2-mechanical-story-evidence-integrity-gate.json`
- `_bmad-output/implementation-artifacts/spec-te-2-mechanical-story-evidence-integrity-gate.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/index.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md`
- `_bmad-output/planning-artifacts/technical-enablers.md`
- `docs/story-evidence-integrity.md`
- `story-evidence-policy.json`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/GateFixture.cs`
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj`
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/ChangedPath.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/CommandArguments.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/EvidenceJson.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/GateIssue.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/GateOptions.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/GateReason.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/GateReport.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/GateValidationException.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/GitCommandResult.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/GitReader.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/JsonReportWriter.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/LaneResult.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/LifecycleTransitionValidator.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/MarkdownStoryReader.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/PrimaryPathVerdict.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/Program.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/ProvenanceAttestor.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/RepositoryScope.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/ScopeEvaluation.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/ScopeEvaluator.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/SprintLedgerReader.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/StoryEvidenceValidator.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/StoryRecord.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/TechnicalEnablerLedgerReader.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/TransitionDetector.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/TransitionRecord.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/TrxEvidenceReader.cs`

## Suggested Review Order

**Protected entry point**

- Batch preflight keeps multi-contract CI fail-closed while still reporting every candidate.
  [`Program.cs:115`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/Program.cs#L115)

- Candidate-first detection protects terminal history without loading unrelated malformed contracts.
  [`TransitionDetector.cs:20`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/TransitionDetector.cs#L20)

**Snapshot and lifecycle contract**

- Mode-aware evaluation separates exact HEAD ownership from the narrow committed event.
  [`ScopeEvaluator.cs:20`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/ScopeEvaluator.cs#L20)

- Four semantic replacements enforce the delayed TE-2 completion boundary byte-for-byte.
  [`LifecycleTransitionValidator.cs:21`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/LifecycleTransitionValidator.cs#L21)

- The v2 contract declares 45 owned paths and exactly four transition paths.
  [`te-2-mechanical-story-evidence-integrity-gate.json:13`](evidence/te-2-mechanical-story-evidence-integrity-gate.json#L13)

**Evidence trust boundary**

- Attestation validates every lane and destination before any provenance write begins.
  [`ProvenanceAttestor.cs:52`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/ProvenanceAttestor.cs#L52)

- Canonical TeamTest structure prevents namespace, identity, counter, and freshness spoofing.
  [`TrxEvidenceReader.cs:119`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/TrxEvidenceReader.cs#L119)

- Safe story keys prevent contract and report paths from escaping their roots.
  [`EvidenceJson.cs:107`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/EvidenceJson.cs#L107)

**Policy and verification**

- Pinned policy validation also runs before no-transition success or attestation.
  [`StoryEvidenceValidator.cs:151`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/StoryEvidenceValidator.cs#L151)

- Lifecycle tests prove exact completion and reject every broader contract mutation.
  [`StoryEvidenceGateTests.cs:92`](../../tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs#L92)

- Multi-contract tests prove invalid later evidence cannot partially mint provenance.
  [`StoryEvidenceGateTests.cs:2225`](../../tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs#L2225)

- Architecture pins keep the policy, workflow, and gate integration mechanically present.
  [`ScaffoldArchitectureTests.cs:763`](../../tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs#L763)

- The runbook explains operational invariants and the external branch-protection prerequisite.
  [`story-evidence-integrity.md:44`](../../docs/story-evidence-integrity.md#L44)
