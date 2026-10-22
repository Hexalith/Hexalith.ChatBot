---
title: 'Resolve five deferred evidence-gate reliability items'
type: 'bugfix'
created: '2026-10-22'
status: 'complete'
route: 'dispatch'
baseline_commit: 'fdeb341ee2b31dc54f3a5a0551de9e303544c457'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five open items weaken the story-evidence gate: unbounded reads (DW-29), ambiguous failure subjects (DW-30), repository-dependent fixture discovery (DW-31), freshness overrides granted by lane name alone (DW-124), and unreliable out-of-memory recovery (DW-125).

**Approach:** Resolve these five items, verify refusal and acceptance paths, record ledger closure evidence, then commit and push this run's changes to `main`.

## Boundaries & Constraints

**Always:** Preserve nine stable reason codes, exact-byte scope hashing, current freshness ceilings, valid bound recovery evidence, metadata-only reports, and all-lanes preflight before sidecar writes. Preserve unrelated dirty files and submodules; stage only this run's changes. Validate the commit message.

**Never:** Weaken release gates, change domain behavior or dependencies, initialize nested submodules, or claim recovery qualification from unit tests.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected behavior |
|----------|-------|-------------------|
| Oversized input | JSON, TRX, source, Git output exceeds cap | Bounded read refuses with stable metadata; evidence remains unchanged |
| Deep JSON | Depth exceeds 64 | Deterministic parsing refusal |
| Valid input | Within limits | Existing hashes and acceptance preserved |
| Override spoof | Lane name without full binding | Attestation and validation refuse; sidecars unchanged |
| Bound override | Matching primary declaration/class/selector/source/paths | Existing extended window applies |
| Relocated tests | Output outside repository | Fixture loads copied policy asset |
| CLI or operational failure | Invalid option, missing input, Git/scope failure | Documented bounded subject; unknown caller input absent |

</frozen-after-approval>

## Code Map

Paths below are relative to `tools/Hexalith.ChatBot.StoryEvidenceGate/` unless prefixed otherwise.

- `EvidenceJson.cs`: strict loaders and age resolution; depth currently defaults to 64.
- `TrxEvidenceReader.cs`: ordinary TRX lacks the sanitizer's 64 MiB cap.
- `GitReader.cs`, `ScopeEvaluator.cs`: unbounded text/blob/worktree reads; preserve exact bytes and process timeout cleanup.
- `TransitionDetector.cs`, `MarkdownStoryReader.cs`, `SprintLedgerReader.cs`: local metadata reads and contract discovery.
- `StoryEvidenceValidator.cs`, `ProvenanceAttestor.cs`: primary binding checked only for required classes; attestation trusts override lane names.
- `RecoveryTrxSanitizer.cs`, `RecoveryAttemptSummarizer.cs`: existing 64 MiB byte/character caps and explicit OOM catch filters.
- `CommandArguments.cs`, `Program.cs`, `GateIssue.cs`: existing reason/subject reporting.
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/`: fixture, project asset configuration, behavioral tests. Invalid contract mutations need `refreshEvidence: false` to avoid automatic attestation.
- `docs/story-evidence-integrity.md`: gate invariants and reason vocabulary.

## Tasks & Acceptance

**Execution:**
- [x] `tools/Hexalith.ChatBot.StoryEvidenceGate/BoundedEvidenceInput.cs` — add internal bounded reading and apply it to mapped file/Git readers. Use 4 MiB JSON, 64 MiB TRX/source/text/output caps, explicit JSON depth 64 and XML character bounds. Enforce caps while reading growing streams.
- [x] `CommandArguments.cs`, `docs/story-evidence-integrity.md` — static CLI subjects; document reason/subject mappings, remediation and limits; retain existing sufficient process/I/O/scope subjects.
- [x] `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj`, `GateFixture.cs` — copy policy to output/publish; resolve directly from `AppContext.BaseDirectory`.
- [x] `StoryEvidenceValidator.cs`, `ProvenanceAttestor.cs` — share internal complete binding checks for every freshness-overridden result, independent of triggered classes.
- [x] `RecoveryTrxSanitizer.cs`, `RecoveryAttemptSummarizer.cs`, `Program.cs` — remove OOM catches; retain bounded malformed-input handling.
- [x] `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs` — matrix coverage, oversized file/Git inputs, depth, exact CLI diagnostics, invalid-override sidecar atomicity, valid recovery window and sanitizer/summary behavior.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` — mark only DW-29/30/31/124/125 done after verification, with named evidence.

**Acceptance Criteria:**
- Given oversized inputs, when read, then allocation stays bounded and refusal contains only stable metadata.
- Given an override without its complete policy binding, when attesting or validating, then it fails without sidecar mutation; valid bindings retain their window.
- Given relocated output, when fixture tests execute, then repository ancestry is unnecessary.
- Given CLI/operational failures, when reported, then documented subjects distinguish categories without unknown caller input.
- Given five verified fixes, when pushing, then only this run's changes enter `main` and prior worktree changes remain intact.

## Implementation Notes

Baseline: `e6f36d4d6da448e0bdbf04c077ee6c2b3e4cee34`. User authorized preserving seven dirty root files and five dirty submodules. Focused Debug build passed; baseline suite passed 226 tests, zero failures/skips.

Baseline `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2: `TenantReadAuthorityResult.cs(12,5)` CS0246, `NativeSourceProof` missing. Separate from gate verification.


Implementation completed and audited against the unified diff. Added bounded file/pipe input and shared primary binding helpers, fixed static CLI subjects, copied the policy asset, and removed OOM catches. No submodule content or pointers changed.

Matrix audit: oversized inputs are covered by JsonReadersShouldRefuseOversizedFiles, OversizedTrxShouldLeaveSidecarAtomic, OversizedWorktreeSourceShouldRefuseBeforeHashing, OversizedGitSourceShouldRefuseBoundedly, OversizedGitStderrShouldKillTheProducerPromptly and BoundedReadsShouldRefuseGrowingStreams; depth by JsonReadersShouldRejectDepthBeyond64; valid bytes by BoundedReadAtCeilingShouldPreserveExactBytes and existing bootstrap/scope tests; spoofing and sidecar preservation by FreshnessOverrideShouldRequireCompleteBindingBeforeAnySidecarWrite and InvalidLaterFreshnessOverrideShouldLeaveAllSidecarsUntouched; valid overrides by FullyBoundPrimaryLaneShouldRetainExtendedFreshness; CLI/operational failures by CliFailureShouldUseExactStaticSubject and OperationalFailureShouldRetainExactMetadataSubject. Full suite: 271 passed, zero failures/skips, in TestResults/deferred-five-final.*. Parent strengthened the depth test to use an object root; all four cases passed in TestResults/deferred-five-depth.* after a clean Debug build.

Relocation: copied complete output to /tmp/story-evidence-relocated-uhp1n4et; seven bootstrap/snapshot/primary fixture cases passed in TestResults/deferred-five-relocated.*. Debug publish to /tmp/story-evidence-publish-4wBogQS4 succeeded and its policy hash matches the repository policy. Whitespace verification uses `git -c core.whitespace=cr-at-eol diff --check` to respect required CRLF. Only the five authorized ledger entries were closed. Commit validation and the authorized push remain finalization steps after review.


Review patches are verified: the metadata line-array cap, committed JSON blob consumers, explicit Windows skip, shared sanitizer limit, duplicate declaration, and active discovery regression all pass. The parent Debug build and full suite passed 276 cases with zero failures/skips in TestResults/deferred-five-verified.*; current copied output passed seven cases in TestResults/deferred-five-relocated-verified.*. All three independent review layers are complete; two older memory-budget risks are deferred in the ledger.

Repository finalization uses TE-3 and the existing technical-enabler grammar: terminal status is `complete`, with its matching completed retrospective action and evidence contract. This follows the repository-owned gate vocabulary in place of the build workflow's generic `done` spelling. Product story statuses and release qualification remain unchanged. The full implementation diff, including source files, is bound by the completion contract and current-run gate self-tests. The implementation baseline is the frontmatter revision; the earlier e6f36d4 notes describe investigation before another session committed the existing work.

## Spec Change Log

## Review Triage Log

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| BH-1: line-array amplification | medium / patch | ReadFileLines materializes both a list and array for up to 64 MiB of newline-only input; .gitmodules previously streamed. Add a small internal line-count cap before retaining excess lines, with a refusal regression. |
| BH-2: XML DOM amplification | medium / defer | The three XDocument.Load calls predate this change and can materialize millions of small XML elements inside the byte/character limit. File caps improve this without solving the older structural-budget problem; record separate follow-up. |
| BH-3: aggregate discovery allocation | medium / defer | FindSingleContract retained complete parsed candidate objects before this change. Per-file caps do not bound the number of compliant inactive candidates; record separate identity-only/budget follow-up. |
| BH-4: format-specific blob consumer coverage | medium / patch | New JSON-specific BlobBytes calls in ScopeEvaluator and LifecycleTransitionValidator lack consumer-level limit regressions; existing Show coverage does not exercise them. Add focused committed-blob cases. |
| BH-5: asynchronous/file acceptance boundary matrix | low / rejected | Existing valid fixture/file consumers and exact-byte synchronous boundary tests pass; no acceptance defect was found. Additional large/empty helper matrix would add substantial test setup for rarely encountered boundaries. |
| BH-6: Windows pass-through | medium / patch | New stderr test returns successfully on Windows without exercising the producer. Replace the successful return with an explicit platform skip. Linux zero-skip evidence remains accurate. |
| BH-7: direct producer/descendant identity assertions | low / rejected | The current stderr producer keeps pipes open during sleep; prompt successful drainage already catches loss of ordinary tree cleanup. No surviving producer was demonstrated; additional PID tracking and platform-specific introspection are disproportionate. |
| BH-8: duplicate recovery byte ceiling | low / patch | MaximumTrxBytes duplicates the new shared input limit. A direct constant alias removes future drift without changing current behavior. |
| BH-9: duplicate primary declaration coverage | medium / patch | PrimaryLaneBinding requires exactly one matching declaration; add the missing duplicate-declaration mutation and verify both consumers refuse atomically. |
| BH-10: baseline explanation in spec | low / rejected | Frontmatter records the canonical implementation baseline; earlier notes reflect initial investigation before another session committed the dirty work. This finding only edits the build spec, which the review workflow excludes. |
| VG-1: oversized inactive contract during active discovery | medium / patch | Pre-verified coverage gap: current inactive-malformed test has no active completion, while oversized tests only invoke strict loaders/Show. Add an active completion plus unchanged oversized inactive contract and assert the expected transition. |

Edge-case hunter returned no findings after tracing changed paths and deletions.


## Verification

- `dotnet build tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj --configuration Debug -m:1 --nologo` — zero warnings/errors.
- Execute built test runner through `.github/scripts/run-xunit-v4.sh` with `XUNIT_REQUIRE_ZERO_SKIPS=1`, retaining CTRF and TRX under `TestResults/deferred-five.*` — nonzero tests, zero failures/skips.
- Copy complete test output outside workspace; execute fixture tests there — policy loads.
- `git diff --check`, independent workflow review, commit-message validation, remote fetch/divergence inspection and authorized push to `main`.

## File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/evidence/five-deferred-evidence-gate-fixes.json`
- `_bmad-output/implementation-artifacts/spec-five-deferred-evidence-gate-fixes.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/technical-enablers.md`
- `docs/story-evidence-integrity.md`
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/GateFixture.cs`
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/GrowingEvidenceStream.cs`
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj`
- `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/BoundedEvidenceInput.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/CommandArguments.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/EvidenceJson.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/GitReader.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/LifecycleTransitionValidator.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/MarkdownStoryReader.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/PrimaryLaneBinding.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/Program.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/ProvenanceAttestor.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/RecoveryAttemptSummarizer.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/RecoveryTrxSanitizer.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/ScopeEvaluator.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/SprintLedgerReader.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/StoryEvidenceValidator.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/TechnicalEnablerLedgerReader.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/TransitionDetector.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/TrxEvidenceReader.cs`
