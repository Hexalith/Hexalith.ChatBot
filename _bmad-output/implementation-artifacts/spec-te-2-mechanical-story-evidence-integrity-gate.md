---
title: 'TE-2 Mechanical Story-Evidence Integrity Gate'
type: 'chore'
created: '2026-08-03'
status: 'in-review'
review_loop_iteration: 1
baseline_commit: 'e14afd950e728bf06a62b1c52ba13ac7ae724282'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story completion evidence is manually reconciled, allowing status, File Lists, scoped root/submodule changes, machine results, primary-path execution, and checked work to contradict each other.

**Approach:** Implement approved Technical Enabler TE-2 as a dependency-free .NET 10 fail-closed gate, versioned policy/contract, machine-result CI path, planning hold, self-validation evidence, and developer runbook. `targetStatus=done` means a completion proposal; for `recordKind=technicalEnabler`, the persisted terminal status is `complete`.

## Boundaries & Constraints

**Always:** Treat the approved proposal as authoritative; preserve 13 product epics/112 product stories and historical `done` records; use exact full Git revisions, deterministic SHA-256 scope digests, metadata-only reports, stable reason codes, read-only Git commands, root-declared submodules only, TRX plus checksum/provenance sidecars, and mandatory primary-path/task/AC evidence. Keep TE-2 and its sprint action open while the required protected check is absent.

**Ask First:** Any policy weakening, new dependency, mixed-scope waiver, external branch/ruleset mutation, or expansion into product/UX/runtime behavior.

**Never:** Initialize/update nested submodules; update dependencies; modify the dirty Conversations/Parties submodules or unrelated Story 12.15/deferred-work changes; infer story identity from numeric prefixes; accept narrative summaries, zero/all-skipped results, fallback-only evidence, stale/wrong-digest artifacts, broad bookkeeping exclusions, secrets/payloads, or automatic status mutation; commit or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Valid transition | Exact story/ledger scope, matching diff/File List, bound passing TRX, complete mappings | Exit 0 and deterministic JSON success report | Metadata summary only |
| Status/scope contradiction | Wrong story/sprint key, head, digest, File List, disclosure, or gitlink | Exit nonzero | Stable status/file/scope/gitlink reason |
| Invalid results | Missing/malformed/failed/zero/skipped/stale/wrong-SHA TRX or sidecar | Exit nonzero | Stable result/provenance reason |
| Primary fallback | Triggered browser/SignalR/hosting-assets/Aspire-Dapr/recovery class lacks primary execution | Exit nonzero | `primary_path_not_executed` |
| Incomplete evidence | Mandatory task/AC unchecked, stale, unmapped, or mapped to failed assertion | Exit nonzero | `checked_item_evidence_mismatch` |
| Bootstrap | TE-2 ledger in review with completion target and exact self-validation evidence | Gate passes prospectively but does not edit ledger | Remain incomplete until branch protection is active |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/{technical-enablers.md,epics.md,architecture.md,index.md}` -- approved TE-2 ledger, invariant, canonical inventory, and discovery edits.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- prospective hold/permanent rule; preserve wording and open action.
- `story-evidence-policy.json` and `_bmad-output/implementation-artifacts/evidence/te-2-mechanical-story-evidence-integrity-gate.json` -- strict grammar, triggers, schemas, bootstrap contract.
- `tools/Hexalith.ChatBot.StoryEvidenceGate/` -- CLI, story/enabler parsers, Git scope/digest, TRX/provenance, reconciliation, transition detection, JSON reporting; use `ProcessStartInfo.ArgumentList`.
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/` -- synthetic repositories/TRX/contracts and positive, negative, mutation, multi-repository, retained-evidence, bootstrap coverage.
- `Hexalith.ChatBot.slnx` and `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` -- project and governance integration.
- `.github/workflows/ci.yml` -- collision-safe machine results, full-history transition bounds, transition-scoped retained/current-primary collection, named `story-evidence-integrity` job/report upload; preserve non-recursive checkout. `.github/workflows/release.yml` is read-only retained-evidence context.
- `docs/story-evidence-integrity.md` and `README.md` -- exact CLI, grammar, reason codes, retained evidence, submodules, troubleshooting, external prerequisite.

## Tasks & Acceptance

**Execution:**
- [x] Planning/ledger files -- apply the approved 13/112-preserving hold, TE-2 record, architecture invariant, and index links without closing TE-2/action.
- [x] Policy/contract/tool -- implement strict schema parsing, status/File List/diff/gitlink/digest/TRX/provenance/primary-path/task-AC reconciliation, transition detection, and metadata-only stable reports.
- [x] Tests/solution/architecture guard -- add projects and prove every proposal failure class, mutations, dirty/immutable scopes, root gitlink+submodule, current/retained evidence, and TE-2 bootstrap.
- [x] CI/docs -- collect per-lane TRX+sidecars, run/upload the named gate for transitions or no-transition self-tests, and document the shipped command.

**Acceptance Criteria:**
- Given the proposal matrix, when fixtures mutate each invariant, then the intended stable reason is produced and all valid root-only, multi-repository, current-run, retained exact-digest, primary-path, and bootstrap cases pass.
- Given Release warnings-as-errors and CI workflow validation, when narrow then broad checks run, then tool/tests/solution/architecture gates pass and machine-derived counts/results are recorded.
- Given GitHub reports `main` unprotected, when repository acceptance passes, then TE-2/action remain non-complete and the exact external prerequisite is reported.

## Spec Change Log

- 2026-08-03: Implemented the approved TE-2 repository gate, self-validation contract, planning hold, machine-result CI path, and developer runbook. Frozen intent remained unchanged.
- 2026-08-03: Hardened the gate after audit: canonical BMAD story grammar, actual-result selectors, primary-lane class/skip binding, exact two-sided gitlinks, retained provenance immutability/source locators, policy-bound event revisions/exclusions, and normal technical-enabler completion detection. Frozen intent remained unchanged.
- 2026-08-03: Closed final immutable-scope and lifecycle edge cases: immutable validation rejects any index/worktree/untracked drift before hashing, and product completion rejects a sprint entry that was already `done` at the base revision. Frozen intent remained unchanged.
- 2026-08-03: Applied the adversarial review patch: exact raw producer heads and retained-artifact collection, collision-safe lanes, fence-aware/ambiguity-safe parsing, policy-bound primary claims/selectors/sources, exact Git tree/index/symlink digest semantics, explicit owned-gitlink scopes at any root-declared depth, internally reconciled TRX results, safe locator/path/metadata handling, and same-version policy mutation guards. Frozen intent remained unchanged.
- 2026-08-03: Corrected CI artifact scoping after final audit: exact-bound transition detection now precedes artifact collection, inactive retained contracts cannot block unrelated/no-transition runs, and topology success/download/head verification is required only for active contracts declaring the current-run Aspire/Dapr primary lane; the general producer remains mandatory. Frozen intent remained unchanged.
- 2026-08-03: Replaced synthetic policy-v1 primary selectors with exact shipped browser, SignalR, hosting/assets, Aspire/Dapr, and recovery test classes; fixture TRX names, architecture pins, and runbook producer mappings now prove the bound classes are executable. Frozen intent remained unchanged.

## Design Notes

Contracts use an exact conventional path plus explicit story path/key/title and sprint key; no numeric inference. Canonical digests sort repository/path/mode/blob tuples, preserve immutable Git-tree bytes/modes and worktree index modes, hash symlink target text as mode `120000`, and include two-sided submodule gitlink tuples while excluding only the policy-approved report path and the exact contract's `scope.implementationDigest`. TRX sidecars bind base/head, digest, TRX checksum, lane/source/selectors, timestamp, and the contract-declared artifact locator; attestation writes current-run lanes only.

Root contract revisions use explicit `$BASE`/`$HEAD` sources because a tracked contract cannot embed the identifier of the commit that contains itself; reports and provenance persist the resolved full revisions. An immutable scope requires its index, worktree, and non-ignored untracked set to match resolved head before exact tree objects are read. Exact pre-existing local disclosures are accepted only for a worktree preflight and never waive mixed immutable base/head scope.

## Verification

**Commands:**
- `dotnet build tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release -warnaserror --no-restore` and the equivalent test-project build -- both TE-2 projects clean.
- `dotnet test tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj --configuration Release --no-restore --logger "trx;LogFileName=story-evidence-gate.trx" --results-directory TestResults/story-evidence-gate -m:1 /nr:false` -- matrix green with machine counts.
- `dotnet run --project tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj --configuration Release --no-build -- attest|validate ...` -- exact-digest prospective TE-2 self-validation green.
- `UseHexalithProjectReferences=true dotnet build Hexalith.ChatBot.slnx --configuration Release --no-restore -warnaserror -m:1 /nr:false` and CI-compatible per-project tests -- broad repository acceptance attempted and blockers recorded below.
- `actionlint .github/workflows/ci.yml .github/workflows/release.yml`, the exact 44-path `git diff --check`, and broad `git diff --check` -- workflow/scoped checks green; broad blocker recorded below.

**Recorded results (2026-08-03):** focused gate matrix 109 total / 109 executed / 109 passed / 0 failed / 0 skipped; TE-2 prospective self-validation passed with 44 File List paths, 44 scoped diff paths, and 7/7 checked-item mappings. The three TE-2/submodule CI architecture guards passed 3/3, `actionlint` passed, and the exact 44-path diff check is clean. Across the previously recorded 14 CI test projects, machine output remains 3,135 total / 3,125 passed / 5 failed / 5 skipped: unrelated dirty recovery-sandbox work causes 1/74 architecture failures, and existing shared-catalog drift (`bunit` 2.8.6 versus expected 2.8.4-preview) causes 4/227 UI-test failures; all other projects pass, including browser E2E at 141/141. The serialized solution Release build succeeds without warnings-as-errors with 26 MSB3277 warnings, while the required `-warnaserror` build fails on existing Microsoft.IdentityModel 8.19.2/8.22.0 reference conflicts; the targeted architecture run still passes its 3/3 selected guards while emitting those existing dependency-conflict diagnostics. Broad `git diff --check` fails on the unrelated Story 12.15 whitespace finding at `tests/Hexalith.ChatBot.RecoverySandbox/Program.cs:284`. GitHub reports `main` unprotected, the branch-protection endpoint returns 404, and the repository has no rulesets. These repository/external blockers keep the TE-2 spec `in-review`, its ledger entry in `review`, TE-2.5 in progress, and the sprint action open.

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

**Policy and gate entry points**

- Versioned primary-path and provenance rules define the fail-closed contract.
  [`story-evidence-policy.json:64`](../../story-evidence-policy.json#L64)

- CI orchestration detects exact completion transitions before attesting or validating evidence.
  [`Program.cs:111`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/Program.cs#L111)

- Reconciliation binds identity, policy, status, scope, results, primary paths, and checked work.
  [`StoryEvidenceValidator.cs:126`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/StoryEvidenceValidator.cs#L126)

**Exact scope and machine provenance**

- Canonical hashing preserves Git tree modes, symlinks, gitlinks, and contract-only lifecycle exclusions.
  [`ScopeEvaluator.cs:20`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/ScopeEvaluator.cs#L20)

- TRX parsing rejects inconsistent counters, nonpassing results, unsafe paths, and unbound provenance.
  [`TrxEvidenceReader.cs:24`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/TrxEvidenceReader.cs#L24)

- Transition detection reconciles product stories, enabler records, sprint keys, and open actions.
  [`TransitionDetector.cs:15`](../../tools/Hexalith.ChatBot.StoryEvidenceGate/TransitionDetector.cs#L15)

**CI enforcement and lifecycle**

- The uniquely named job binds raw producer heads and transition-declared artifacts only.
  [`ci.yml:96`](../../.github/workflows/ci.yml#L96)

- TE-2 remains review-only until self-validation and protected-check activation both hold.
  [`technical-enablers.md:34`](../planning-artifacts/technical-enablers.md#L34)

- The product plan keeps 13 epics and 112 stories under prospective enforcement.
  [`epics.md:622`](../planning-artifacts/epics.md#L622)

- Architecture makes evidence integrity a metadata-only gated state transition.
  [`architecture.md:641`](../planning-artifacts/architecture.md#L641)

**Evidence, documentation, and verification**

- The bootstrap contract declares the exact 44-path scope and seven checked mappings.
  [`te-2 evidence:1`](evidence/te-2-mechanical-story-evidence-integrity-gate.json#L1)

- The runbook documents contract grammar, retained artifacts, commands, and troubleshooting.
  [`story-evidence-integrity.md:1`](../../docs/story-evidence-integrity.md#L1)

- Mutation fixtures exercise positive, negative, multi-repository, retained, and bootstrap behavior.
  [`StoryEvidenceGateTests.cs:12`](../../tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs#L12)

- Architecture assertions freeze CI naming, raw-head binding, artifact sequencing, and non-recursion.
  [`ScaffoldArchitectureTests.cs:763`](../../tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs#L763)

- Solution integration keeps the dependency-free tool and its tests in normal builds.
  [`Hexalith.ChatBot.slnx:28`](../../Hexalith.ChatBot.slnx#L28)
