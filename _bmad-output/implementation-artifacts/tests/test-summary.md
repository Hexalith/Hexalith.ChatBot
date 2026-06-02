# Test Automation Summary - Story 7.6

**Story:** 7.6 - Notification routing and delivery
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-02
**Framework:** xUnit v3 in-process runners + Shouldly (existing project framework).
Compiled runners used per the story sandbox note (`dotnet test`/VSTest can hit `SocketException (13)`).

## Approach

Story 7.6 shipped to `review` with acceptance coverage already in place across contracts,
authorization, gateway/audit, resolver, projection, and UI. This QA pass audited the existing
suite against the 9 acceptance criteria and the architecture guardrails, then **auto-applied 4
discovered gaps** — behaviours present in the implementation but unverified by any test.

## Discovered Gaps Applied

| # | Gap | AC / Guardrail | Test added |
|---|-----|----------------|-----------|
| 1 | `raised-at` normalized to UTC on the delivery (`ToUniversalTime()`) was never asserted on a non-UTC offset | AC1 ("UTC raised-at" metadata field) | `RaisedAtShouldBeNormalizedToUtcOnTheDelivery` |
| 2 | Delivery `TenantRef` provenance (must come from event binding, never recipient/correlation id) was unasserted | AC2 + arch guardrail "tenant id from authenticated binding" | `TenantRefShouldComeFromTheEventBindingNeverRecipientOrCorrelation` |
| 3 | The metadata-only delivery seam (`INotificationSink` / `InMemoryNotificationSink`) had **zero** coverage — deliveries were resolved but never exercised through the sink | AC1 ("delivered"), AC6 (delivery record stays metadata-only) | `ResolvedDeliveriesShouldFlowThroughTheMetadataOnlySinkWithoutLeakage` |
| 4 | The `NotificationRoutingSchema.MaxEntries` (64) bound was untested | AC5 (schema-bounded rejection) | `RoutingMapShouldRejectMoreEntriesThanTheSchemaBound` |

## Generated Tests

### Server resolver / delivery seam
- [x] `tests/Hexalith.ChatBot.Server.Tests/Notifications/NotificationRoutingResolverTests.cs`
  - UTC normalization of raised-at (AC1)
  - Tenant-ref provenance from event binding only (AC2 + guardrail)
  - End-to-end flow through `InMemoryNotificationSink` with no content/address/secret leakage (AC1, AC6)

### Contracts
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/NotificationRoutingContractTests.cs`
  - Routing map rejects more than `MaxEntries` entries (AC5)

## AC -> Coverage Map (post-pass)

| AC | Covered by |
|----|-----------|
| 1 - six state classes route, metadata-safe fields, UTC raised-at | resolver `AllSixStateClasses...`, `RaisedAtShouldBeNormalizedToUtc...`; contract metadata-only serialization; sink flow test |
| 2 - recipient scoped to per-item authority | resolver `ItemSpecificContext...`, `Aggregate...`, tenant-provenance test |
| 3 - unauthorized recipient redacted, no existence leakage | resolver `ItemSpecificContext...` (redacted form), sink leakage assertions |
| 4 - closed typed map, governed spine, actor/old/new/reason/timestamp | contract + gateway audit-ref tests |
| 5 - schema-bounded validation, undeclared rejected | contract `RejectUndeclaredValues`, `RejectDuplicate...`, `RejectMoreEntriesThanTheSchemaBound` |
| 6 - fail-closed audit + metadata-only audit refs | `CommandGatewayTests` notification-routing fail-closed + audit-ref tests |
| 7 - non-human / unauthorized denied before state load | `NotificationRoutingAuthorizationTests` allow/deny matrix |
| 8 - OpenAPI/client drift only if public | no public surface added; Client.Tests checksum parity unchanged |
| 9 - aggregate acceptance coverage | all of the above |

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, **179 tests** (was 178; +1).
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, **568 tests** (was 565; +3).
- 4 new tests, 0 failures, 0 regressions.

## Checklist Validation

- [x] API/acceptance tests generated (gap-driven).
- [x] Tests use standard xUnit v3 + Shouldly APIs.
- [x] Tests cover happy path + critical edge cases (UTC normalization, tenant provenance, leakage, bound overflow).
- [x] All generated tests run successfully.
- [x] Tests use semantic assertions; no hardcoded waits or sleeps.
- [x] Tests are independent (no order dependency; isolated state per test).
- [x] Test summary created with coverage metrics.

## Next Steps

- Run the full suite set in CI (Contracts, Server, UI, Conformance, Architecture, Client).
- Story 7.9 will add throttle/digest rollup on the per-event sink - extend these sink tests then.
