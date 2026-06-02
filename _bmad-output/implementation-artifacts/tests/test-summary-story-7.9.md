# Test Automation Summary — Story 7.9 (Notification throttling and digest rollup)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-02 · **Engineer:** Jerome
**Framework:** .NET 10 / xUnit v3 / Shouldly / NSubstitute (compiled in-process runner — sandbox `dotnet test`/VSTest `SocketException` workaround). No new framework introduced.
**Mode:** gap audit of the existing Story 7.9 test suite against AC1–AC9, with discovered gaps auto-applied.
**Story:** `_bmad-output/implementation-artifacts/7-9-notification-throttling-and-digest-rollup.md`

Story 7.9 is a **server-side delivery-pipeline** feature with **no public HTTP endpoint and no UI surface** (per the
story's decided variances). "E2E/API tests" therefore means compiled in-process coverage of the throttle evaluator, the
throttle/digest coordinator (the full evaluate → audit → side-effect path), the metadata-only stores, and the closed
Tenant Policy ceiling knob. The story already shipped tests; this run closed coverage gaps and auto-applied them.

## Gaps Discovered and Auto-Applied

| # | Gap | AC | Test added |
|---|-----|----|-----------|
| A | Audit envelope omits `notification-item:` for a **redacted** delivery (and carries it for `ItemContext`) — the digest side was tested, the audit side was not | 3, 6 | `RedactedDeliveryEnvelopeOmitsItemRefIndistinguishableFromNotFound`, `ItemContextDeliveryEnvelopeCarriesTheSafeItemRef` |
| B | Per-recipient isolation **within the same tenant** (existing isolation test was cross-tenant only) | 4 | `PerRecipientThrottlingIsIsolatedWithinTheSameTenant` |
| C | Store key length-prefix **collision defense** for ambiguous `(tenant × recipient)` pairs (`ab|cd` vs `abc|d` vs `a|bcd`) | 4 | `HistoryStoreKeyIsCollisionSafeAcrossAmbiguousTenantRecipientPairs`, `DigestStoreKeyIsCollisionSafeAcrossAmbiguousTenantRecipientPairs` |
| D | `digest-rolled-up-count` audit snapshot **increments** across successive overflows (:1, :2, :3) | 2, 6 | `SuccessiveOverflowsAccumulateAndRolledUpCountSnapshotIncrements` |
| E | `DrainPendingDigest` builds the digest, is **destructive**, and is **isolated per pair** | 2 | `DrainPendingDigestReturnsAllEntriesThenLeavesThePairEmpty`, `DrainPendingDigestIsIsolatedPerPair` |

## Generated Tests

### API / behavioural tests (coordinator + stores)
- [x] `tests/Hexalith.ChatBot.Server.Tests/Notifications/NotificationThrottleCoordinatorTests.cs` — +4 tests (gaps A×2, B, D)
- [x] `tests/Hexalith.ChatBot.Server.Tests/Notifications/NotificationDeliveryStoreTests.cs` — new file, 4 tests (gaps C×2, E×2)

### E2E / UI tests
- N/A — Story 7.9 adds no UI surface and no public endpoint (ceiling tuned via the existing `SubmitTenantPolicyChange` path; throttle/digest is a server-side delivery-pipeline concern). No Playwright/bUnit E2E applicable.

## Coverage (acceptance criteria → tests)

| AC | Coverage |
|----|----------|
| 1 — both windows ≤8/hr ∧ ≤30/day, exactly-at-ceiling deterministic | Pre-existing evaluator tests (8th delivers / 9th throttles; daily-throttle-under-hourly; purity) |
| 2 — server-measured UTC, client time ignored | Pre-existing (window-edge 3600s/86400s, future-timestamp rejection) + **D** (rolled-up-count snapshot) |
| 3 — overflow → digest, identity preserved, redacted = not-found, no restricted detail | Pre-existing (digest entry, redaction, serialized no-leak) + **A** (audit-side item-ref redaction) |
| 4 — `(tenant × recipient)` isolation | Pre-existing (cross-tenant) + **B** (same-tenant per-recipient) + **C** (key collision defense) |
| 5 — closed bounded ceiling knob; above-max/wrong-type/undeclared rejected; safe defaults | Pre-existing contract tests (no gap) |
| 6 — fail-closed audit, no counter advance, metadata-only envelope | Pre-existing (audit-unavailable, one envelope/decision, secret bans) + **A** + **D** |
| 7 — read/observable surfaces (EN/FR, stable tokens) | N/A — no new visible text this story |
| 8 — OpenAPI/client drift only if public contracts change | N/A — no public surface; client parity confirmed by regression |
| 9 — acceptance coverage proves all of the above | Satisfied by the matrix above |

## Validation Results

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **Build succeeded, 0 warnings, 0 errors** (warnings-as-errors clean).
- `Hexalith.ChatBot.Server.Tests -parallel none` → **642 passed, 0 failed** (was 634; +8 new).
- `Hexalith.ChatBot.Contracts.Tests -parallel none` → **231 passed, 0 failed** (unchanged — AC5 already fully covered).
- Stray submodule gitlink drift reset non-recursively before build (`git submodule update -- Hexalith.EventStore Hexalith.FrontComposer Hexalith.Tenants`); no submodule pointer bumped.

## Checklist (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API/behavioural tests generated · [N/A] E2E/UI (no UI surface) · [x] standard framework APIs · [x] happy path · [x] critical error cases (fail-closed audit, redaction, collision)
- [x] All generated tests run successfully · [x] proper locators (direct typed seam calls — no UI) · [x] clear descriptions · [x] no hardcoded waits/sleeps (deterministic injected `FixedClock`) · [x] independent tests (fresh harness/store per test)
- [x] Summary created · [x] tests saved to `tests/Hexalith.ChatBot.Server.Tests/Notifications/` · [x] coverage metrics included

## Next Steps

- Run the new tests in CI alongside the existing Server/Contracts suites.
- When the durable/Dapr-state binding and the runtime digest-send caller land (deferred this story), add integration
  tests for persistence and the periodic digest-send across the rolling window.
