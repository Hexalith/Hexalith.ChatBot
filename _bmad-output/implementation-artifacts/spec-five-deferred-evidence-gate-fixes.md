---
title: 'Resolve five deferred evidence-gate reliability items'
type: 'bugfix'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
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
- [ ] `tools/Hexalith.ChatBot.StoryEvidenceGate/BoundedEvidenceInput.cs` — add internal bounded reading and apply it to mapped file/Git readers. Use 4 MiB JSON, 64 MiB TRX/source/text/output caps, explicit JSON depth 64 and XML character bounds. Enforce caps while reading growing streams.
- [ ] `CommandArguments.cs`, `docs/story-evidence-integrity.md` — static CLI subjects; document reason/subject mappings, remediation and limits; retain existing sufficient process/I/O/scope subjects.
- [ ] `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj`, `GateFixture.cs` — copy policy to output/publish; resolve directly from `AppContext.BaseDirectory`.
- [ ] `StoryEvidenceValidator.cs`, `ProvenanceAttestor.cs` — share internal complete binding checks for every freshness-overridden result, independent of triggered classes.
- [ ] `RecoveryTrxSanitizer.cs`, `RecoveryAttemptSummarizer.cs`, `Program.cs` — remove OOM catches; retain bounded malformed-input handling.
- [ ] `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/StoryEvidenceGateTests.cs` — matrix coverage, oversized file/Git inputs, depth, exact CLI diagnostics, invalid-override sidecar atomicity, valid recovery window and sanitizer/summary behavior.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md` — mark only DW-29/30/31/124/125 done after verification, with named evidence.

**Acceptance Criteria:**
- Given oversized inputs, when read, then allocation stays bounded and refusal contains only stable metadata.
- Given an override without its complete policy binding, when attesting or validating, then it fails without sidecar mutation; valid bindings retain their window.
- Given relocated output, when fixture tests execute, then repository ancestry is unnecessary.
- Given CLI/operational failures, when reported, then documented subjects distinguish categories without unknown caller input.
- Given five verified fixes, when pushing, then only this run's changes enter `main` and prior worktree changes remain intact.

## Implementation Notes

Baseline: `e6f36d4d6da448e0bdbf04c077ee6c2b3e4cee34`. User authorized preserving seven dirty root files and five dirty submodules. Focused Debug build passed; baseline suite passed 226 tests, zero failures/skips.

Baseline `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2: `TenantReadAuthorityResult.cs(12,5)` CS0246, `NativeSourceProof` missing. Separate from gate verification.

## Spec Change Log

## Review Triage Log

## Verification

- `dotnet build tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj --configuration Debug -m:1 --nologo` — zero warnings/errors.
- Execute built test runner through `.github/scripts/run-xunit-v4.sh` with `XUNIT_REQUIRE_ZERO_SKIPS=1`, retaining CTRF and TRX under `TestResults/deferred-five.*` — nonzero tests, zero failures/skips.
- Copy complete test output outside workspace; execute fixture tests there — policy loads.
- `git diff --check`, independent workflow review, commit-message validation, remote fetch/divergence inspection and authorized push to `main`.
