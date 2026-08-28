---
title: 'Share recovery read-model keys and absence polling'
type: 'refactor'
created: '2026-08-28'
status: 'done'
baseline_revision: '53c41666742c8bf359783e24997581467f5afae7'
baseline_commit: '53c41666742c8bf359783e24997581467f5afae7'
review_loop_iteration: 0
followup_review_recommended: true
context: []
warnings: []
deferred:
  - summary: >-
      The harness copy of the attachment-index key shape is still a hand-copied twin of the production
      factory, so a production rename would silently make recovery absence checks vacuously true.
    evidence: |-
      RecoveryIntakeReadModelProbe.KeysFor builds the third key as the literal
      `{tenantId}:project-conversation:{intakeId}:attachments`, while production writes it from the private
      `AttachmentIndexKeyFor` in ReadModelProjectConversationProjectionStore. The first two keys are genuinely
      tethered (they call the public KeyFor factories); the third is compared only against a restatement of
      itself in KeysPreserveCanonicalShapesAndOrder. Editing the production shape breaks no test, and the
      harness would then erase and assert absence of a key that can never exist. This story reduced the number
      of harness copies from two to one but could not close the gap: the intent lists production as read-only
      evidence and forbids changing production read-model keys or public contracts.
    location: >-
      tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryIntakeReadModelProbe.cs:48
    severity: medium
  - summary: >-
      The "canonical" intake read-model set covers three keys while an intake also materializes per-intake
      items and participants entries, so cleanup and cross-tenant isolation checks cannot observe leaks there.
    evidence: |-
      Both operations classes document EraseIntakeReadModelsAsync as erasing "every read model an intake
      materializes", but ReadModelProjectConversationProjectionStore also persists
      `{tenant}:project-conversation:{intake}:items` and `...:participants` plus the item and participant views
      they index. Those are outside the trio, so they survive cleanup and the absence probes are blind to them.
      This predates the change, but the change names the trio canonical and pins it with a new test.
    location: >-
      tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryIntakeReadModelProbe.cs:43
    severity: medium
  - summary: >-
      LiveProjectionRebuildDriverTests.RebuildUsesOnlyTheSelectedTenantImmutableSourceAndWormChain fails on
      a write-count assertion and already failed at this story's baseline commit.
    evidence: |-
      Shouldly.ShouldAssertException: readModels.Writes should be 4 but was 6. Reproduced on the patched tree,
      on HEAD, and on a tree restored to baseline 53c41666742c8bf359783e24997581467f5afae7 with the two new
      files removed, so it is unrelated to this refactor and to its patches. The counter involved is Writes,
      which this story never touched.
    location: >-
      tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriverTests.cs:63
    severity: medium
  - summary: >-
      The integration test project has no clean build configuration in this workspace: the default Debug
      source-reference mode and the package mode each fail for different pre-existing dependency-drift reasons.
    evidence: |-
      `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj
      --configuration Debug -m:1` fails with CS1704 ("An assembly with the same simple name
      'Hexalith.Commons.UniqueIds' has already been imported") in
      references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts. The super-repo sets
      UseHexalithProjectReferences=true for Debug, which turns on HexalithCommonsFromSource inside the
      EventStore submodule, so a source-built UniqueIds 3.97 and the transitively resolved package 2.30 land in
      the same compile closure. Forcing package mode with -p:UseHexalithProjectReferences=false instead fails
      with CS0234/CS0246 because the published Hexalith.Memories.Contracts package lacks the
      Hexalith.Memories.Contracts.V1.DerivedStores namespace that src/Hexalith.ChatBot.Server consumes.
      Only -p:HexalithCommonsFromSource=false builds clean. Neither failure is in any file this story touched.
    location: >-
      references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj
    severity: high
---

<intent-contract>

## Intent

**Problem:** `AspireRecoverySandboxOperations` and `AspireScopedOutageOperations` independently define the same intake read-model key vocabulary and sustained-absence polling. That duplication makes a future projection-key change easy to apply incompletely across recovery scenarios.

**Approach:** Extract one integration-harness helper that owns the canonical intake read-model key set plus one-shot and sustained absence checks, then make both operations classes use it for cleanup and verification while preserving the existing key strings, order, final boundary read, and 500 ms polling cadence.

## Boundaries & Constraints

**Always:** Keep the helper inside `Hexalith.ChatBot.IntegrationTests.Recovery`; preserve `ProjectConversationSourceEmailView.KeyFor`, `ProjectConversationAttachmentSetView.KeyFor`, and the exact `{tenantId}:project-conversation:{intakeId}:attachments` index shape; keep cancellation propagation, `ConfigureAwait(false)`, ordinal behavior, and the final read after the polling window; keep one C# type per file.

**Block If:** The production projection store proves a different attachment-index shape is authoritative, or sharing the helper requires changing recovery evidence semantics or poll timing.

**Never:** Change production read-model keys, recovery windows, durable-state probes, public contracts, package dependencies, or the deferred-work ledger; do not broaden this refactor into unrelated recovery-harness cleanup.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Canonical key enumeration | Valid tenant and intake identifiers | Returns the source-email, attachment-set, and attachment-index keys in the existing order and exact shapes | Existing key factories validate blank identifiers |
| One-shot absence | No canonical key is present | Returns `true` after checking all three keys | Storage/cancellation failures propagate |
| Presence detected | Any canonical key is present | Returns `false` immediately | No failure is reinterpreted as absence |
| Sustained absence | All keys remain absent for the requested window | Polls at 500 ms and performs a final boundary read before returning `true` | Cancellation propagates; late presence returns `false` |

</intent-contract>

## Code Map

- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs:1295-1344` -- first duplicated key and absence implementation; call sites also erase the three keys separately and run sustained isolation checks.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs:683-772` -- second duplicated implementation; retains a separate single-key absence poll that is outside this extraction.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationSourceEmailView.cs:31` -- authoritative source-email key factory; read-only evidence.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationAttachmentSetView.cs:14` -- authoritative attachment-set key factory; read-only evidence.
- `src/Hexalith.ChatBot.Server/Projections/ReadModelProjectConversationProjectionStore.cs:943` -- authoritative attachment-index string shape; read-only evidence.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/InMemoryRecoveryReadModelStore.cs` -- existing ETag-aware test store reusable for focused helper tests.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/EventStoreDurableStateProbeTests.cs` -- nearby polling-boundary test conventions and Shouldly/xUnit v3 style.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryIntakeReadModelProbe.cs` -- add the single canonical key enumerator and one-shot/sustained absence methods with a production 500 ms cadence and a narrow internal test cadence seam.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs` -- instantiate and use the helper for key enumeration and absence polling; remove the four duplicated local helpers.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs` -- make the same substitution while retaining its unrelated `RemainsReadModelKeyAbsentAsync` behavior.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryIntakeReadModelProbeTests.cs` -- cover the I/O matrix with exact key assertions and focused absence/cancellation behavior.

**Acceptance Criteria:**
- Given either Aspire recovery operations class, when intake read models are erased or checked for absence, then the shared helper supplies the same three keys and no local duplicate key/absence helper remains.
- Given the shared helper's default construction, when sustained absence is observed, then polling uses the existing 500 ms interval and performs a closing read after the window.
- Given a present canonical key, storage failure, or caller cancellation, when absence is evaluated, then presence returns `false` and failures/cancellation propagate rather than becoming a successful absence claim.
- Given the completed refactor, when the focused integration test project is built and its helper tests run, then warnings-as-errors remain clean and all focused tests pass.

## Spec Change Log

## Review Triage Log

### 2026-08-28 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 2: (high 0, medium 2, low 0)
- defer: 0
- reject: 16: (high 0, medium 4, low 12)
- addressed_findings:
  - `[medium]` `[patch]` Added deterministic coverage proving an intermediate poll rejects presence even when that key is removed before the closing read.
  - `[medium]` `[patch]` Added an Nth-read failure seam and coverage proving a closing storage-read failure propagates instead of becoming absence.

### 2026-08-28 - Follow-up review pass
- intent_gap: 0
- bad_spec: 0
- patch: 4: (high 0, medium 2, low 2)
- defer: 4: (high 1, medium 3, low 0)
- reject: 13: (high 0, medium 3, low 10)
- addressed_findings:
  - `[medium]` `[patch]` The fake store's read seam checked `Volatile.Read(ref _reads) + 1` against
    `FailOnReadNumber` and then incremented separately, so the ordinal was non-atomic and a failing read never
    advanced the counter - contradicting the documented "Nth read attempt" contract by making every subsequent
    read fail too. Split into `BeginRead` (atomic attempt counter, counted whether or not the attempt fails)
    and `CompleteRead`.
  - `[medium]` `[patch]` `Reads` was incremented before the dictionary lookup, so
    `SpinWait.SpinUntil(() => store.Reads >= 4)` in
    SustainedAbsenceReturnsFalseForIntermediatePresenceRemovedBeforeClosingRead was not a happens-after gate:
    the test's erase could land between the increment and the probe's observation, letting the probe miss the
    injected presence and fail a correct implementation. `CompleteRead` now runs after storage is observed, so
    the counter is a valid gate.
  - `[low]` `[patch]` `RejectReads` and `FailOnReadNumber` threw the identical message
    ("Injected read-model read failure."), so a red test could not tell which seam fired. They now throw
    distinct, self-identifying messages.
  - `[low]` `[patch]` The `Reads` doc comment still said "persisted reads" after the seam began counting etag
    probes as well; it and the `FailOnReadNumber` doc now describe the actual contract.

### 2026-08-28 - Follow-up review pass 2
- intent_gap: 0
- bad_spec: 0
- patch: 3: (high 0, medium 1, low 2)
- defer: 0
- reject: 27: (high 0, medium 6, low 21)
- addressed_findings:
  - `[medium]` `[patch]` The two sustained-absence tests raced the probe's polling loop on wall-clock time: each
    needed its `SaveAsync` to land inside a 100 ms / 50 ms `Task.Delay`, and the
    `SpinWait.SpinUntil(() => store.Reads >= 4)` gate did not establish happens-after ordering between the save
    and read 4 - if read 4 won the race, the subsequent erase made the probe return `true` and failed a correct
    implementation. Replaced the wall-clock coupling with an `OnReadAttempt` seam on the fake store that injects
    presence from inside the read path at an exact attempt number, so both tests are now deterministic. Boundary
    vs. intermediate position is now expressed by the window (`TimeSpan.Zero` closes after the first sweep, so
    attempt 4 is the closing read; 200 ms cannot, so attempt 4 is an in-loop poll) rather than by timing.
  - `[low]` `[patch]` `BeginRead` threw for `RejectReads` before incrementing `_readAttempts`, so the
    `FailOnReadNumber` doc's "attempts are counted whether or not they succeed" was false whenever both seams
    were set. The attempt is now counted first, and `RejectReads` documents that it preserves the numbering.
  - `[low]` `[patch]` No test pinned `FailOnReadNumber`'s single-shot contract - the only user threw out of the
    probe on the injected failure and never read again, so reverting last pass's atomic-attempt fix would still
    have passed green. Added `InjectedReadFailureFailsOnlyTheTargetedAttempt`, which asserts the failure lands on
    exactly one attempt and that later reads observe storage normally.

  Not re-raised: the attachment-index key copy, the canonical-trio breadth, the baseline-failing
  `LiveProjectionRebuildDriverTests` case, and the workspace build drift are already carried as open ledger
  entries; nothing was added or changed there. Notable rejects: the "dead `System.Diagnostics` using / dead
  `PollInterval` constant" and "missing file copyright header" findings were checked against the tree and are
  false (both constants and `Stopwatch` remain used at 9 other sites per class; no file in this repo carries a
  header). Re-coupling the probe to each class's `PollInterval`, sharing the 5-attempt erase retry loop,
  validating `window`, and removing the closing boundary read are all excluded by the intent.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug -m:1` -- expected: build succeeds with zero warnings and errors.
- `dotnet run --project tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug --no-build -- -class Hexalith.ChatBot.IntegrationTests.Recovery.RecoveryIntakeReadModelProbeTests` -- expected: all focused xUnit v3 tests pass.
- `rg -n "IntakeReadModelKeys|AttachmentIndexKeyFor|AreIntakeReadModelsAbsentAsync|RemainsIntakeReadModelsAbsentAsync" tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs` -- expected: no matches.

## Auto Run Result

Status: done
Blocking condition: none

### Implemented change

`RecoveryIntakeReadModelProbe` is the single owner of the canonical intake read-model key vocabulary and of the
one-shot and sustained absence checks for the recovery harness. `AspireRecoverySandboxOperations` and
`AspireScopedOutageOperations` both construct it and delegate erase-key enumeration (1 site each), one-shot
absence (1 site each) and sustained absence (5 + 1 sites); all four duplicated local helpers are gone from both
classes. Key strings, key order, short-circuit-on-presence, `ConfigureAwait(false)`, cancellation propagation,
the 500 ms cadence and the closing read after the polling window are preserved verbatim.

This second follow-up pass changed no production or harness behavior. It made the probe's two sustained-absence
tests deterministic, closed a documented-contract inconsistency in the fake store's read seam, and pinned that
seam's single-shot guarantee with a test.

### Files changed

- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryIntakeReadModelProbe.cs` - shared probe: static
  `KeysFor` enumerator, `AreAbsentAsync`, `RemainsAbsentAsync`, 500 ms production cadence with an internal
  cadence seam for deterministic tests. Unchanged this pass.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryIntakeReadModelProbeTests.cs` - focused suite, now
  16 cases. The two sustained-absence presence tests no longer race the polling loop; added
  `InjectedReadFailureFailsOnlyTheTargetedAttempt`.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/InMemoryRecoveryReadModelStore.cs` - read seam: the attempt
  counter now advances before the `RejectReads` throw, and a new `OnReadAttempt` hook lets a test mutate storage
  at an exact read attempt from inside the read path.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireRecoverySandboxOperations.cs` - delegates to the probe;
  duplicated key/absence helpers removed. Unchanged this pass.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/AspireScopedOutageOperations.cs` - same substitution, with
  its unrelated single-key `RemainsReadModelKeyAbsentAsync` left intact. Unchanged this pass.

### Review findings breakdown

- Patches applied this pass: 3 (medium 1, low 2) - all in the harness test seam, detailed in the triage log.
- Items deferred this pass: 0. The four defer-worthy findings this pass surfaced are the same ones already
  carried as open ledger entries from the previous pass; they were not re-raised, re-scoped, or duplicated.
- Items rejected this pass: 27 (medium 6, low 21) - proposals the intent excludes (sharing the erase retry loop,
  tethering production key factories, re-coupling `PollInterval`), behavior the intent told this refactor to
  preserve verbatim (the poll-interval window overshoot, the unvalidated `window`, the closing boundary read),
  two findings that were checked and are factually false (dead usings/constants; missing copyright headers), and
  test-hygiene cosmetics.

### Follow-up review recommendation

Patched findings this pass: high 0, medium 1, low 2. Score = 3 x 1 + 1 x 2 = 5, which is >= 5, so
`followup_review_recommended` is `true`.

### Verification performed

- `dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj
  --configuration Debug -m:1 -p:HexalithCommonsFromSource=false` -- Build succeeded, 0 Warning(s), 0 Error(s).
  The `-p:HexalithCommonsFromSource=false` flag is required by the pre-existing workspace dependency drift
  already carried in the ledger; the spec's unflagged form of this command still fails for that same reason,
  in files this story does not touch.
- `dotnet run --project ... -- -class ...RecoveryIntakeReadModelProbeTests` -- Total: 16, Failed: 0, in 0.126s.
  Re-run 5 times consecutively: 16/16 passing every time, with run times of 0.118-0.127s (previously the suite
  spent ~350 ms in real `Task.Delay` waits), confirming the timing coupling is gone rather than merely widened.
- `rg -n "IntakeReadModelKeys|AttachmentIndexKeyFor|AreIntakeReadModelsAbsentAsync|RemainsIntakeReadModelsAbsentAsync"`
  over both operations classes -- no matches, as expected.
- `LiveProjectionRebuildDriverTests`, the only other consumer of the fake store, was run to confirm this pass
  did not move it: 9 total, 1 failed, the same pre-existing baseline failure at line 63 recorded in the ledger.

### Residual risks

- The harness attachment-index key is still a hand copy of a private production helper, and the canonical set is
  asserted to be exactly three keys. Both are carried as open ledger entries; a production key rename would
  still make the corresponding absence check vacuous without breaking a test.
- Every line this refactor changed inside the two Aspire operations classes is exercised only by the Tier-3
  live lane (`HEXALITH_CHATBOT_TIER3=1` plus Docker and DAPR). The call-site repointing is guarded by the
  compiler and by the new probe unit tests, not by an executed live run in this session.
- `OnReadAttempt` adds another seam to a shared test fixture. It is inert unless a test assigns it, and the
  fixture's only other consumer asserts `Writes`, which this pass did not touch.
