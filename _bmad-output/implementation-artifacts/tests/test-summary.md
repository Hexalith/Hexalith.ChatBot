# Test Automation Summary — Story 8.2 (Operational telemetry emission)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer role:** QA automation (test generation only) · Jerome

## Context

Story 8.2 is a backend **OpenTelemetry metric-emission seam** (.NET 10, `System.Diagnostics.Metrics`)
in `.Server`/`.ServiceDefaults`. There is **no UI and no public HTTP endpoint** (AC7/AC8 explicitly forbid
one), so there are no browser/E2E flows to drive — the appropriate automated coverage is BCL
`MeterListener`-based instrument tests plus instrumentation-point seam tests, which is the framework the
project already uses (xUnit v3 + Shouldly + NSubstitute). This run **closed coverage gaps** in that existing
suite rather than introducing a new framework. The feature was already implemented (status `review`) with a
substantial test set; the QA pass mapped every acceptance criterion to a concrete test and auto-applied the
discovered gaps.

## Method / Framework

- xUnit v3 (`v3.2.2`), Shouldly, NSubstitute — compiled in-process runners (VSTest avoided per the story note:
  `SocketException (13): Permission denied` in this sandbox).
- Metrics observed deterministically via `System.Diagnostics.Metrics.MeterListener` (no exporter required).

## Gaps Discovered and Auto-Applied

| # | Gap (acceptance criterion) | Test added |
| - | -------------------------- | ---------- |
| A | Gap-detection meta-counter dimension ban was unasserted — nothing proved `chatbot.telemetry.emission_failures` carries **only** `operation-class`+`reason` and never leaks a `tenant` tag (AC4/AC6). | `ChatBotMetricsTests.GapDetectionMetaCounterCarriesOnlyOperationClassAndReasonAndNeverLeaksTenant` |
| B | The audit-projection-lag **observable gauge** was excluded from the dimension-name ban — only tag *values* were checked, not that the key set is exactly `{tenant, operation-class}` (AC4/AC9). | `ChatBotMetricsTests.AuditProjectionLagGaugeMeasurementCarriesOnlyTenantAndOperationClassDimensions` |
| C | The finite `ChatBotOperationClasses` taxonomy (`All`/`IsKnown`, the closed operation-class set) had no test locking the seven stable tokens (AC3). | `ChatBotOperationClassesTests` (4 facts/theories, 13 cases) |
| D | Approval latency's `finally`-path guarantee — that latency is still recorded when the core decision **throws** while the exception propagates unchanged — was unverified (only the happy Approved path was covered) (AC2/AC5). | `AiActionApprovalGateMetricsTests.EvaluateAsyncShouldRecordApprovalLatencyEvenWhenTheDecisionThrows` |

## Generated / Modified Tests

### Metrics-seam unit tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/Observability/ChatBotMetricsTests.cs` — +2 dimension-ban tests (Gaps A, B)
- [x] `tests/Hexalith.ChatBot.Server.Tests/Observability/ChatBotOperationClassesTests.cs` — **new**, closed-taxonomy lock (Gap C)

### Instrumentation-point tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AiActionApprovalGateMetricsTests.cs` — +1 exception-path latency test (Gap D)

## Coverage vs Acceptance Criteria

| AC | Covered by | Status |
| -- | ---------- | ------ |
| AC1 seven instruments registered on the ChatBot meter | `AllSevenOperationalInstrumentsPlusGapCounterAreRegisteredOnTheChatBotMeter` | ✅ pre-existing |
| AC2 latency histograms + counters + gauge | latency theory, counter test, gauge test, **+ Gap D exception-path** | ✅ strengthened |
| AC3 bounded tenant + finite operation-class | dimension test, **+ Gap C taxonomy lock** | ✅ strengthened |
| AC4 no restricted/secret dimension (push + meta + gauge) | push-instrument ban, **+ Gap A meta-counter ban, + Gap B gauge ban** | ✅ strengthened |
| AC5 non-blocking emission | forced-failure swallow test, **+ Gap D exception still propagates with latency recorded** | ✅ strengthened |
| AC6 gap-detection meta-counter | emit-threw / tenant-unavailable / lag-source-threw tests, **+ Gap A ban** | ✅ strengthened |
| AC7 meter wired via `AddMeter` in ServiceDefaults | `ChatBotMeterNameShouldBeStableAndWiredIntoTheMetricsPipeline` | ✅ pre-existing |
| AC8 read-only audit-lag gauge, coarse value only, fail-safe no-data | gauge reflect / no-data / source-throw tests, **+ Gap B key ban** | ✅ strengthened |
| AC9 acceptance roll-up | full `ChatBotMetricsTests` suite + instrumentation-point tests | ✅ |

## Test Run Results (compiled in-process runners, `-parallel none`)

| Suite | Total | Failed |
| --- | --- | --- |
| Server.Tests | **986** (was 970; +16 cases from the four gaps) | 0 |
| Server.Tests (namespace `…Observability`) | 27 | 0 |
| ServiceDefaults.Tests | 4 | 0 |

- Build: `dotnet build Hexalith.ChatBot.slnx --no-restore` → **0 Warning(s), 0 Error(s)** (warnings-as-errors clean).
- No source under test was modified — tests only. No new framework, no public surface added.

## Residual (intentionally not added — justified)

- **Multi-tenant gauge mix** (trustworthy readings + fail-safe no-data rows in one collection): the production
  default `UnavailableAuditProjectionLagSource` reports nothing, so a real per-tenant checkpoint feed does not
  exist yet. Single-reading + no-data + source-throw cases already pin the fail-safe doctrine; a mixed-row test
  is best added when the real source replaces the unavailable default (a sanctioned follow-up swap).
- **Throwing-`IChatBotMetrics`-at-a-seam** tests: the non-blocking guarantee lives inside `ChatBotMetrics.SafeEmit`
  (covered by Gap A + the forced-failure test). Seams intentionally do **not** re-wrap the call, so a throwing
  *implementation* would be a contract violation, not a behaviour to assert — out of scope.

## Next Steps

- Run the suites in CI (already green locally).
- When a real per-tenant audit-checkpoint feed replaces `UnavailableAuditProjectionLagSource`, add the
  multi-tenant mixed-reading gauge test described above.
