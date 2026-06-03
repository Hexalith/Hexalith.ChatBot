# Test Automation Summary — Story 9.12 (Projection rebuild validation)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **By:** Jerome (QA automation)
**Framework:** xUnit v3 + Shouldly (the project's existing .NET test stack — no new framework introduced).

## Scope

Story 9.12 is a **server-internal validation-harness** (a pure evaluator + an injectable coordinator + fail-closed
audit-then-deliver), not a UI feature and not an HTTP/API surface — there is no UI to drive and no endpoint to exercise,
so this run generates **unit/component automated tests** (the project's E2E analog for `.Server/Audit/` machinery),
mirroring the Story 9.11 QA additions. The story's primary tests (evaluator, coordinator, leakage scan) already shipped
green; this run **closed the discovered direct-coverage gaps** the story's own Project Structure Notes flagged
("QA-gap tests: envelope-factory metadata-only refs, deferred-driver throws, token-set/`Contains`").

## Discovered gaps → tests applied (auto-applied)

| # | Gap (untested unit before this run) | Test added |
|---|---|---|
| 1 | `ProjectionRebuildVerdicts` closed set + null-safe `Contains` + legacy-literal avoidance pinned only indirectly | `ProjectionRebuildVerdictsTests.cs` (8 cases) |
| 2 | `AuditEnvelopeFactory.ProjectionRebuildValidationFailed` envelope shape + bounded metadata-only refs only smoke-serialized by the leakage scan | `AuditEnvelopeFactoryProjectionRebuildTests.cs` (2 cases) |
| 3 | `DeferredProjectionRebuildDriver.RebuildAsync` inert throw never asserted directly | `DeferredProjectionRebuildDriverTests.cs` (1 case) |
| 4 | `ProjectionResourceDigest.Create` sanitization boundary (unsafe/content-bearing/null tokens) never exercised | `ProjectionResourceDigestTests.cs` (9 cases) |
| 5 | `ProjectionRebuildReport.Unmeasurable` factory fields + `IsBreach`/`IsDivergent` folds only exercised indirectly via the coordinator | `ProjectionRebuildReportTests.cs` (3 cases) |
| 6 | Evaluator empty-snapshot edge case (empty→empty is equivalent) | added 1 case to existing `ProjectionRebuildEquivalenceEvaluatorTests.cs` |

## Generated / extended tests

### Unit / component tests (`tests/Hexalith.ChatBot.Server.Tests/Audit/`)
- [x] `ProjectionRebuildVerdictsTests.cs` — closed set is exactly equivalent/divergent/unmeasurable; literals avoid the legacy-lifecycle tokens; `Contains` recognizes only known tokens; null-safe.
- [x] `AuditEnvelopeFactoryProjectionRebuildTests.cs` — divergent+over-target envelope pins the fixed command/decision/state-transition/outcome tokens, pre-commit phase, metadata-only redaction, Worker origin, null replay-run id, integer-second duration (`16200`), boolean flags, resources-compared/schema-version refs, one ref per deviation, the safe first-diverging locator, and the space-free / banned-marker-free ref invariant; unmeasurable envelope carries the incomplete deviation, zero-second duration, and **no** first-diverging ref.
- [x] `DeferredProjectionRebuildDriverTests.cs` — the inert default throws `NotSupportedException("…M2-deferred")` (the throw the coordinator's fail-safe catch maps to `unmeasurable`).
- [x] `ProjectionResourceDigestTests.cs` — `Create` keeps safe tokens verbatim and reduces unsafe/content-bearing/null tokens (spaces, `secret`, `.json`, embedded body+password) to the `redacted-ref` fallback for both fields.
- [x] `ProjectionRebuildReportTests.cs` — `Unmeasurable` factory field-by-field; `IsBreach`/`IsDivergent` folds across equivalent-within-target, deterministic-but-slow, and unmeasurable.
- [x] `ProjectionRebuildEquivalenceEvaluatorTests.cs` (extended) — two empty snapshots with matching schema are equivalent with a null first-diverging locator.

### Pre-existing Story 9.12 tests (verified green, unchanged)
- `ProjectionRebuildEquivalenceEvaluatorTests.cs`, `ProjectionRebuildValidationCoordinatorTests.cs` (Server), `ProjectionRebuildLeakageScanTests.cs` (Conformance).

## Results

| Suite | Result |
|---|---|
| Story 9.12 Server tests (evaluator + coordinator + new QA-gap fixtures) | **40 / 40 passed** |
| Conformance — `ProjectionRebuildLeakageScan` | **1 / 1 passed** |
| Architecture.Tests (boundary + scaffold guards; **no** new allowlist entry) | **39 / 39 passed** |
| Full `Hexalith.ChatBot.Server.Tests` (regression) | **1447 / 1447 passed** (was 1423; +24 new) |

- Grep confirmed **no** new `4` / `TimeSpan.FromHours(4)` literal introduced for the rebuild target — `RecoveryTargets.MaxRto` reused throughout (source and tests).
- No hardcoded waits/sleeps; all tests use a `FixedClock` and a deterministic scripted/fake driver; tests are independent (no order dependency).

## Coverage

- Public surface of the Story 9.12 deliverables now has **direct** unit coverage: verdict vocabulary, structural-digest sanitization, the pure evaluator (incl. empty-snapshot edge), the report + `Unmeasurable` factory + breach folds, the coordinator (all four paths + sweep + audit-then-deliver + test-tenant guard), the inert deferred driver, the breach-envelope factory refs, and the cross-tenant no-leak floor.
- **Deferred (by design, documented seams):** the live `IProjectionRebuildDriver` rebuild runtime and the periodic scheduler / release-gate wiring — exercised only via the inert default + scripted fakes (inert-control-floor, consistent with Stories 9.4/9.11).

## Next Steps

- Run in CI alongside the existing Epic 9 suites.
- When the live `IProjectionRebuildDriver` runtime lands (M2), add an integration test that drives a real test-tenant rebuild from immutable source records + WORM history against the deployed Aspire/AKS environment.
