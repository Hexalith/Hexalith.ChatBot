# Test Automation Summary — Story 7.8 (Approval Queue Prioritization & Grouping)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-02 · **Engineer:** Jerome
**Framework:** .NET 10 / xUnit v3 / Shouldly (compiled in-process runner — sandbox `dotnet test`/VSTest `SocketException` workaround).
**Mode:** gap audit of the existing Story 7.8 test suite against AC1–AC9, with discovered gaps auto-applied.

## What this run did

Story 7.8 already shipped with focused tests. This run audited that coverage against the 9 acceptance
criteria, found the gaps below, and **auto-applied** them. No new test project or framework was introduced —
new tests extend the existing suites and reuse their patterns (no new abstractions/fixtures).

## Gaps discovered & closed

| # | Gap | AC | Test added |
|---|-----|----|-----------|
| 1 | Exactly-equal **priority score** was asserted equal, but the queue's deterministic **tie-break** (source-version desc → item-ref) was never exercised through the projector. | AC1/AC9 | `EqualScoreRowsShouldTieBreakBySourceVersionThenItemRefDeterministically` |
| 2 | Time-in-queue **upper clamp** (`MaxTimeInQueueSeconds` = 30 days) untested — only the future→0 lower clamp was. | AC1/AC9 | `VeryOldRequestShouldClampTimeInQueueToTheBoundedMaximum` |
| 3 | Explanation/group-key metadata-only invariant was checked for "no spaces"/"no secret", not for **raw requester/command/project plaintext** leakage. | AC5/NFR2 | `ExplanationAndGroupKeyShouldNotLeakRequesterCommandOrProjectPlaintext` |
| 4 | Batch fan-out tested only `Approve`/`Reject`; `RequestRevision`/`Cancel` unverified. | AC4 | `EveryDecisionKindShouldFanOutOneCommandPerItemCarryingThatDecision` (Theory ×4) |
| 5 | Each fanned command carrying its **own expected source version** (not a shared batch token) unverified. | AC4 | `EachFannedCommandShouldCarryItsOwnApprovalIdAndExpectedSourceVersion` |
| 6 | Extreme partial-authority (**all items denied**) edge untested. | AC6 | `BatchWithNoAuthorizedItemsShouldDenyEveryItemAndProduceNoCommands` |
| 7 | **Empty batch** edge untested. | AC6 | `EmptyBatchShouldBeAuthorizedWithNoOutcomesOrCommands` |

## Generated Tests

### Server Tests (modified)
- [x] `tests/Hexalith.ChatBot.Server.Tests/Projections/ApprovalPriorityScorerTests.cs` — gaps 1, 2, 3
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ApprovalBatchDecisionTests.cs` — gaps 4, 5, 6, 7

## Coverage (AC1–AC9 after this run)

- AC1 deterministic `(risk × authority × time)` order incl. equal-score tie-break + both time clamps — ✅ covered
- AC2 bounded `approval.priority-weights` knob, reject out-of-range/NaN/Inf/wrong-type, safe-default fallback — ✅ covered (pre-existing)
- AC3 grouping merges only on identical `(requester × command × project)`, never cross-tenant — ✅ covered (pre-existing)
- AC4 batch fan-out = one command/audit per item, all decision kinds, per-item source version — ✅ covered
- AC5 metadata-only / no requester-command-project plaintext leakage — ✅ covered
- AC6 partial-authority incl. all-denied & empty; non-human denied before state load — ✅ covered
- AC7 metadata-only audit refs, secret-bearing fields banned — ✅ covered (pre-existing)
- AC8 OpenAPI/client/checksum intentionally unchanged — ✅ Client schema-parity tests stay green
- AC9 acceptance umbrella — ✅ covered by the above

## Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **Build succeeded, 0 warnings, 0 errors**.
- `Hexalith.ChatBot.Server.Tests` (in-process runner, `-parallel none`) → **617 passed, 0 failed** (was 607; **+10** new gap-test cases).
- No submodule/gitlink drift introduced; OpenAPI/generated client/checksum untouched.

## Next Steps

- Run the full suite set in CI.
- A pure unit test cannot fully prove AC5's "redacted grouped item is indistinguishable from safe-not-found" at the
  authority-scoped **read endpoint** layer (the builder always emits the safe `requester:`/`command:`/`project:` refs and
  the projector strips via `SafeSummaryToken`); an integration test over the authority-scoped read path would close that
  end-to-end gap if desired.
