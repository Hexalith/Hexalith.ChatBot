# Test Automation Summary — Story 8.4 (Tenant-safe alert wiring)

**Date:** 2026-06-03
**Workflow:** bmad-qa-generate-e2e-tests
**Framework:** xUnit v3 + Shouldly + NSubstitute (in-process compiled runner, `-parallel none`)
**Scope:** server-side .NET feature (no UI) → automated unit/behavioral tests over the five NFR43 alert
evaluators, the shared payload, the coordinator, and the previously-untested integration seams.

## Coverage Assessment

The dev work (status `review`) already shipped thorough, passing tests for the five evaluators, the payload
validator, the in-memory counter, and the coordinator. QA review found the **evaluators and coordinator well
covered** but **three integration/wiring seams untested**. All three gaps were auto-applied.

| AC | Area | Pre-existing coverage | Gap found & filled |
|---|---|---|---|
| AC1 | audit-projection-lag evaluator | ✅ fires Degraded/Failed, suppresses Healthy/Unknown | — |
| AC2 | retry-exhaustion **evaluator** | ✅ fires/suppresses | — |
| AC2 | retry-exhaustion **hook** (`ChatBotMetrics.RecordRetryExhausted` → `IRetryExhaustionAlertSource.Signal`) | ❌ none | ✅ **added** (signal after counter; exception-isolated + gap-counted; blank-tenant no-op) |
| AC3 | approval-queue age | ✅ boundary, aggregate, terminal, non-family | — |
| AC4 | subscription expiry | ✅ per-mailbox, suppression, safe scope | — |
| AC5 | auth-failure spike **evaluator + counter** | ✅ baseline, sliding window, determinism | — |
| AC5 | auth-failure **gateway wiring** (`CommandGateway` → `IAuthorizationFailureCounter.Record`) | ❌ none | ✅ **added** (denial feeds counter with bound tenant only — NFR2) |
| AC6 | payload safe-token validator | ✅ marker-ban, high-cardinality, blank, all 5 kinds | — |
| AC7 | pre-commit audit envelope (`AuditEnvelopeFactory.OperationalAlertFired`) | ⚠️ indirect (coordinator test) | ✅ **added** direct test (fixed tokens, ref list, space→`\|` scope folding, no restricted content) |
| AC8 | non-invasive | ✅ pure evaluators + isolation (reinforced by AC2 hook test) | — |
| AC9 | coordinator + recipients | ✅ 5 fire, audit-unavailable fail-closed, no-signals, non-human principal | — |

## Generated Tests

### New file
- [x] `tests/Hexalith.ChatBot.Server.Tests/Audit/AuditEnvelopeFactoryOperationalAlertTests.cs` — direct AC7
  coverage of the metadata-only pre-commit envelope: fixed `CommandName`/`Decision`/`StateTransition`/phase/
  redaction-stage tokens, the bounded safe ref list, the `tenant:{ref} mailbox:{ref}` → `tenant:{ref}|mailbox:{ref}`
  scope folding (no space-bearing ref), and no restricted content in any ref (NFR2/NFR42). (3 facts)

### Extended files
- [x] `tests/Hexalith.ChatBot.Server.Tests/Observability/ChatBotMetricsTests.cs` — AC2 retry-exhaustion hook:
  source is signalled with the tenant after the OTel counter; a throwing source is swallowed and gap-counted with
  reason `retry-alert-signal-threw`; blank tenant does not signal. (3 facts + 2 nested fakes)
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` — AC5 gateway wiring: an authorization
  denial feeds the failure counter exactly once with the bound tenant only (never actor/command/reason). `Gateway(…)`
  helper extended with an optional `IAuthorizationFailureCounter`. (1 fact + 1 nested recording counter)

## Results

```
dotnet build tests/Hexalith.ChatBot.Server.Tests/...csproj -m:1 /nr:false   → Build succeeded, 0 Warning(s), 0 Error(s)
Hexalith.ChatBot.Server.Tests -parallel none                                → Total: 1066, Failed: 0, Skipped: 0
```

- Server.Tests: **1066 passing** (was 1059 → **+7** new tests). 0 failures, 0 warnings (warnings-as-errors clean).
- Architecture/Conformance/Contracts/UI suites: unaffected — changes are test-project-only; `.Server` source is
  untouched, so prior green results (37/75/298/120) hold.

## Coverage

- Story 8.4 acceptance criteria (AC1–AC9): **fully covered** — all five alert kinds have positive + negative tests,
  the validator rejects unsafe tokens, the coordinator audits-before-delivers and fails closed, and the three
  previously-untested wiring seams (retry hook, gateway auth-counter, audit-envelope factory) now have direct tests.

## Next Steps

- Run tests in CI (already part of the standard gate).
- No further gaps identified for Story 8.4. The runtime scheduler that periodically invokes the coordinator is
  out of scope (deferred, consistent with `ReviewerBacklogAlertCoordinator`) and is not testable until added.
