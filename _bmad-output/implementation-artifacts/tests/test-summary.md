# Test Automation Summary — Story 9.4 (Replay and simulation isolation)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer role:** QA automation (test generation only) · Jerome

**Framework detected:** .NET 10 / xUnit v3 + Shouldly + NetArchTest — the project's existing stack (no JS/Playwright present, none introduced).

**Mode:** Gap-fill against an already-implemented feature in `review` status. Audited every acceptance path against the
existing tests, found **three genuine coverage gaps**, and auto-applied tests for each. No source code was changed —
tests only.

## What this run did

Story 9.4 shipped with extensive coverage already (predicate, marker threading pre/post-commit, test-mode adapter, trace
store, tenant-aware selection, nightly probe, no-leak, boundary). This pass mapped existing tests against AC1–AC3 + the
story's own Task 5 checklist and added tests **only** where a real, achievable gap existed.

## Gaps discovered and filled

### 1. AC2 / Task 2 — marker on *every* command-path factory method (not just pre/post-commit)
Task 2 explicitly requires that **all** command-path envelopes of a replay run carry `ReplayRunId`
("pre-commit, post-commit, duplicate-suppression, rejection, escalation"). Existing tests asserted only pre/post-commit.
The duplicate-suppression and rejection paths (both funnel through the single `AuditEnvelopeFactory.Create` point) were
untested — a regression where a new/relocated factory bypasses `Create` would have slipped through silently.
- **Added** `ReplaySubmissionMarksEveryCommandPathFactoryEnvelope` — replay submission ⇒ `DuplicateMailboxIntakeSuppressed`
  + `RejectedLifecycleTransition` both carry the marker and are recognised by `AuditReplayExclusion.IsReplayEnvelope`.
- **Added** `ProductionSubmissionLeavesEveryCommandPathFactoryEnvelopeUnmarked` — production submission ⇒ both null by omission.
- File: `tests/Hexalith.ChatBot.Server.Tests/Audit/ReplayMarkerThreadingTests.cs`

### 2. DI-wiring guard (Task 3 + Task 4 — registration is real)
A `WormAuditChainDependencyInjectionTests` pattern existed for 9.1/9.2 but there was **no** replay equivalent. Without it
a registration regression could silently resolve a sender that bypasses `ReplayAwareOutboundMailboxSender`, defeating the
whole isolation model (the recurring Epic 7–9 wiring-drift defect).
- **Added** new file `tests/Hexalith.ChatBot.Server.Tests/Adapters/Mailbox/ReplayIsolationDependencyInjectionTests.cs`:
  - `OutboundMailboxSenderResolvesToTheReplayAwareSelector` — the dispatcher's `IOutboundMailboxSender` is the selector.
  - `OutboundTraceStoreResolvesToTheInMemoryDefault`.
  - `TestModeSenderAndIsolationProbeCoordinatorResolve`.

### 3. AC3 — verifier locator preference (documented-but-untested invariant)
`ReplayIsolationVerifier` documents that the outbound-trace assertion is checked first so its locator is preferred when
**both** invariants are violated. That ordering was unpinned.
- **Added** `VerifierPrefersTheTraceLocatorWhenBothInvariantsAreViolated` — both trace + chain replay-marked ⇒
  `TraceBreachReasonCode` with the `trace-send:` locator (not the chain hit).
- File: `tests/Hexalith.ChatBot.Server.Tests/Audit/ReplayIsolationProbeCoordinatorTests.cs`

## Generated Tests

| Layer | File | New tests |
|---|---|---|
| Marker threading (AC2) | `ReplayMarkerThreadingTests` | +2 |
| Nightly probe / verifier (AC3) | `ReplayIsolationProbeCoordinatorTests` | +1 |
| DI wiring (AC1/AC3) | `ReplayIsolationDependencyInjectionTests` (**new file**) | +3 |

## Coverage

| Acceptance criterion | Status |
|---|---|
| AC1 — test-tenant adapter intercept/record, no external send, no production mutation | Covered (predicate, selector unit + **DI**, test-mode sender, trace partitioning, dispatcher E2E) |
| AC2 — `replay_run_id` threaded, excluded from queries + completeness | Covered (**now incl. duplicate-suppression + rejection paths**, distinct-hash, real-record exclusions) |
| AC3 — nightly isolation probe, fail-closed, M2 gate | Covered (verifier clean/trace/chain/**locator-preference**, coordinator audit-then-deliver/Unknown/skip-test/counts) |
| Cross-cutting — no-leak, boundary (internal-to-Server) | Covered (`ReplayIsolationLeakTests`, `ReplayIsolationBoundaryFitnessTests`) |

## Test run results (full suites, after changes)

| Project | Before | After | Delta |
|---|---|---|---|
| `Hexalith.ChatBot.Server.Tests` | 1234 | **1240** | +6 |
| `Hexalith.ChatBot.Architecture.Tests` | 38 | **38** | 0 (boundary unchanged) |

All suites: **Passed — 0 failed, 0 skipped.**

## Documented limitation (not a regression, no production refactor made)

- `Program.ResolveReplayRunId` (the `X-Hexalith-Replay-Run-Id` boundary header → sanitized token) is a top-level
  `static` local function, not reachable from a test without refactoring production `Program.cs`. This workflow generates
  tests only, so it is left as-is. Its behavior is indirectly guarded: `AuditMetadata.SafeOptionalToken` (the sanitizer it
  delegates to) is exercised by `ReplayMarkerThreadingTests.AnUnsafeReplayRunIdIsDroppedToNullNotLeaked` and the
  trace-store no-leak tests. Promoting the resolver to an internal helper would make it directly unit-testable — flagged
  for the dev/review pass, out of scope here.

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API/behavioral tests generated · [x] E2E covered by existing dispatcher tests (no UI surface — initiation UI/CLI is a documented deferral)
- [x] Tests use standard framework APIs (xUnit/Shouldly) · [x] Happy path · [x] Critical error/negative cases (unmarked-by-omission, fail-closed locator)
- [x] All generated tests run successfully (1240 + 38, 0 failed) · [x] Semantic assertions, no hardcoded waits/sleeps · [x] Clear descriptions · [x] Tests independent (no order dependency)
- [x] Summary created · [x] Tests saved to appropriate directories · [x] Coverage metrics included

## Next steps

- Run the new tests in CI alongside the existing Epic 9 suites (no new test project — all land in existing csproj).
- When the periodic scheduler / replay-initiation surface is built (currently deferred), add an integration test that
  drives `SweepAllProductionTenantsAsync` on its cadence and asserts the M2 release-gate outcome.
