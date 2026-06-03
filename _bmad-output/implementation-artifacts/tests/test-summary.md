# Test Automation Summary — Story 9.3 (Audit query & compliance investigation surface, S9)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer role:** QA automation (test generation only) · Jerome

**Framework:** .NET 10 / xUnit v3 + Shouldly — the project's existing stack (no new framework introduced).
`WebApplicationFactory<Program>` for endpoint round-trips; stub `HttpMessageHandler` for client-transport coverage.

## What this run did

Story 9.3 already shipped (status `review`) a thorough suite — read-policy, endpoint, contract, UI-service, and
DOM-contract tests. This QA pass audited every acceptance path against the existing tests, found **four genuine
coverage gaps** on the story's headline deliverables, and auto-applied tests for each. No source code was changed —
tests only.

## Gaps discovered and filled

### 1. New FR56 filter dimensions through the HTTP endpoint round-trip (AC1)
`message-id` and `surface` were covered at the read-policy unit level and contract-validation level, but never through
the full endpoint path (JSON deserialize → schema validate → enumerate chain → `Search` → wire model).
- **Added** `SearchShouldHonorTheNewMessageIdAndSurfaceFiltersThroughTheEndpointRoundTrip` — seeds envelopes differing
  by `SurfaceOrigin` / `source-message:` / `provider-message:` tokens, POSTs each new filter, asserts correct narrowing.
- **Added** `SearchWithUnknownFilterKeyShouldCollapseToSafeNotFound` — an unknown filter key fails the schema gate and
  collapses to the identical safe-not-found (no leak).
- File: `tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditInvestigationEndpointTests.cs`

### 2. UI form → wire-query translation (AC1 / FR56)
`ComplianceAuditService.BuildQuery` was completely uncovered — the fake client ignored the query argument, so a
regression dropping `MessageId`/`Surface` forwarding (or the always-on `time` baseline that keeps the query
schema-valid) would have passed every test.
- **Added** `ServiceShouldTranslateEveryFr56DimensionOntoTheOutboundQueryWithATimeBaseline` and
  `ServiceShouldOmitBlankDimensionsAndStillKeepTheTimeBaseline` (also pins the `Limit <= 0 ⇒ 100` fallback).
- File: `tests/Hexalith.ChatBot.UI.Tests/ComplianceAuditSurfaceTests.cs` (fake client now captures the outbound query).

### 3. Hand-written transport safe-not-found collapse (deferral-2 seam, NFR2)
The hand-written `ComplianceAuditTransport` over the generated client had no test for its safe-not-found behavior — a
403/401/404 must surface as the empty `Denied`/`Restricted` view (never an exception or a leak), and a 200 must parse.
- **Added** new file `tests/Hexalith.ChatBot.Client.Tests/ComplianceAuditTransportTests.cs` — three tests: success
  parse + correlation header + FR56 filters survive serialization; search denials → `Denied`; detail success vs denial
  → parsed vs `Restricted`.

### 4. Remaining `MatchesFilter` arms in lock-step (AC1 / FR56)
The `actor-type`, `resource`, `reason`, and `policy-snapshot` arms were not individually exercised (only
actor/command/decision/correlation/surface/message-id/tenant were).
- **Added** `EveryFr56FilterDimensionShouldMatchItsEnvelopeFieldInLockStep` — match + mismatch per dimension.
- File: `tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditReadPolicyTests.cs`

## Generated Tests

| Layer | File | New tests |
|---|---|---|
| API / endpoint | `ComplianceAuditInvestigationEndpointTests` | +2 |
| Read policy | `ComplianceAuditReadPolicyTests` | +1 |
| UI service | `ComplianceAuditSurfaceTests` | +2 |
| Client transport | `ComplianceAuditTransportTests` (**new file**) | +3 |

## Test run results (full suites, after changes)

| Project | Before | After | Delta |
|---|---|---|---|
| `Hexalith.ChatBot.Server.Tests` | 1191 | **1194** | +3 |
| `Hexalith.ChatBot.UI.Tests` | 126 | **128** | +2 |
| `Hexalith.ChatBot.Client.Tests` | 20 | **23** | +3 |

All suites: **Passed — 0 failed, 0 skipped.**

## Coverage notes

- The three acceptance criteria keep their existing coverage; this pass hardens the **new** AC1 deliverables (the two
  filter dimensions) across all the layers they traverse: read policy → endpoint → client transport → UI service.
- **Not changed (deferral-1):** the Playwright `ComplianceAdministrationE2ETests` remains the fixture-based DOM
  contract documented in the story — a browser-hosted Blazor render harness is still absent from the repo. The real
  surface's DOM contract stays covered by the existing `ComplianceAuditSurfaceTests` composition assertions, now joined
  by the transport/service behavioral coverage above. No browser render harness was added in this pass.

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API tests generated · [x] Tests use standard framework APIs · [x] Happy path · [x] 1–2 critical error cases
- [x] All generated tests run successfully · [x] Proper/semantic locators (DOM-contract + role-based) · [x] Clear
  descriptions · [x] No hardcoded waits/sleeps · [x] Tests independent (no order dependency)
- [x] Summary created · [x] Tests saved to appropriate directories · [x] Coverage metrics included

## Next steps

- Run the new tests in CI alongside the existing suites (no new dependencies required).
- If/when a browser-hosted Blazor render harness lands, repoint the Playwright AuditInvestigation + PhoneFallback
  scenarios at the live component (deferral-1).
