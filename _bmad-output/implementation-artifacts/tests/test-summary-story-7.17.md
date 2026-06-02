# Test Automation Summary — Story 7.17 (Rate-limit service client)

**Workflow:** bmad-qa-generate-e2e-tests · **Date:** 2026-06-02 · **Engineer:** QA automation (Jerome)
**Framework:** xUnit v3 + Shouldly + NSubstitute (.NET 10, repo-pinned). No UI/Playwright surface — this is a server/contract feature (S5 admin UI deferred per story), so "E2E" here is the gateway → aggregate → audit → contract pipeline exercised through in-process tests.

## Scope

Story 7.17 ships a single-actor, tenant-admin-authorized, schema-bounded per-service-client command rate limit, enforced as the **final admission gate** at `ServiceClientGrantValidator`. The dev story arrived in `review` with broad tests already in place. This run audited that coverage against the AC10 acceptance checklist and **auto-applied the discovered gaps**.

## Gaps discovered and auto-applied

| # | Gap (AC) | Why it mattered | Test added |
|---|----------|-----------------|------------|
| 1 | Trailing-window semantics (AC5) | Existing seam tests only seeded in-window timestamps. A cumulative-count regression (ignoring the rolling window) would have passed every test. | `ServiceClientGrantAuthorizationTests.StaleAdmittedCommandsOutsideTrailingWindowShouldNotCountAgainstBudget` — 5 admitted commands, only 2 in-window (incl. the exact -60m window edge proven *outside*), budget 3 ⇒ admitted. |
| 2 | Throttle count uses in-window only + tight boundary (AC5) | The deny path was tested with all-in-window commands; stale exclusion on the *deny* side and the exact `count == budget` boundary were unpinned. | `...TightBudgetBoundaryShouldAdmitBelowAndThrottleAtEffectiveCountUsingOnlyInWindowCommands` — budget 2, 2 in-window + 1 stale ⇒ throttled. |
| 3 | Budget = Minimum (0) defers all commands (AC2) | The bounds doc states a tenant may set 0 to defer all commands; 0 is in-bounds (not coerced to SafeDefaults). The lower boundary of the closed range had no enforcement test. | `...ZeroBudgetShouldDeferEveryCommandEvenWithNoRecentHistory` — budget 0, empty history ⇒ throttled; asserts `EffectiveBudget == 0`. |
| 4 | State-projection isolation (NFR30 / AC10 / NFR17) | Isolation was proven at the validator seam but not at the aggregate/state level — that configuring one client's budget never overwrites a sibling's nor mutates other committed entries. | `GovernedOperationAggregateTests.ServiceClientRateLimitsShouldKeepEachServiceClientBudgetIndependent` — two clients, independent budgets; re-tightening one leaves the other untouched. |

**Not-a-gap (closed without adding):** AC7 `DetailVisibility == MetadataOnly` is already asserted for **every** catalog entry (incl. the new `ServiceClientRateLimited`) by `MessageCatalogContractTests.CatalogEntriesShouldBeSafeAndSerializationTolerant`. A per-entry assertion would be redundant.

## AC10 coverage map (post-run)

- Single human tenant-admin applies it, no approver — ✅ `ServiceClientRateLimitAuthorizationTests`
- Non-tenant-admin scope denied; service/AI denied even with tenant-admin claims — ✅ same
- Out-of-bounds/undeclared budget rejected at **gateway** + **aggregate**; falls back to SafeDefaults at the seam (never raises) — ✅ gateway/aggregate/seam tests (+ gap #3 lower bound)
- At-budget denial with `service_client_rate_limited` as **final gate**, distinct from disabled/quarantined/revoked/expired/over-/under-scoped; under-budget & sibling unaffected (isolation) — ✅ seam tests (+ gaps #1, #2, #4)
- Rate-limit never masks a security denial — ✅ `SecurityDenialShouldKeepItsPreciseReason...`
- Audit envelope: actor/scope/subject/reason/old+new budget/window/policy-snapshot/timestamp, **no** `StateTransition`, no credential/OAuth/`@`/secret/project leakage — ✅ `CommandGatewayTests`
- Audit-unavailable ⇒ no durable rate-limit + no enforcement (fail closed) — ✅ `CommandGatewayTests`
- OpenAPI/client/checksum parity — ✅ `Client.Tests`

## Results (compiled xUnit v3 runners, `-parallel none`)

| Suite | Total | Failed | Note |
|-------|-------|--------|------|
| Contracts.Tests | 257 | 0 | |
| Server.Tests | 752 | 0 | **+4** vs the 748 dev baseline (the auto-applied gaps) |
| Client.Tests | 17 | 0 | OpenAPI/generated-client parity + checksum |
| Conformance.Tests | 75 | 0 | regression |
| Architecture.Tests | 37 | 0 | regression |

`dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → 0 warnings, 0 errors. No submodule/gitlink pointer drift.

## Files touched (tests only — extends files already in the story File List)

- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/ServiceClientGrantAuthorizationTests.cs` (+3 enforcement-seam tests)
- `tests/Hexalith.ChatBot.Server.Tests/Operations/GovernedOperationAggregateTests.cs` (+1 state-projection isolation test)

## Next steps

- Read-side durable projection (`IServiceClientRateLimitProvider`) + admitted-command history (`IServiceClientCommandHistory`) remain deferred (sanctioned, per 7.14–7.16). When implemented, add integration coverage that the projection observes `ServiceClientRateLimitConfigured` and the history increments only on successful admission.
- Service-client **query**-admission gateway wiring is deferred; when built, reuse the same `ServiceClientGrantValidator` seam tests for queries.
