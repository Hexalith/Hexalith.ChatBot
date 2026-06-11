# Test Automation Summary

## Story

Story 7.9: Notification throttling and digest rollup.

## Generated Tests

### API / Behavioural Tests

- [x] Added `tests/Hexalith.ChatBot.Server.Tests/Notifications/NotificationThrottleDependencyInjectionTests.cs`.
- [x] Added `NotificationThrottleRuntimeSeamsResolveToSharedInMemoryDefaults` to prove `AddChatBotCommandGateway()` wires the throttle coordinator to the shared notification sink, delivery-history store, digest store, audit writer, and `ISystemClock`.
- [x] Added `RegisteredCoordinatorDeliversFirstPushAndRollsOverflowIntoDigest` to exercise the registered coordinator through the runtime DI seam: first push is delivered and counted, second push at the lowered ceiling rolls into digest, and both decisions emit audit evidence.

### E2E / UI Tests

- [x] N/A for Story 7.9: the story explicitly adds no UI surface and no public endpoint. Throttling/digest is a server-side delivery-pipeline concern, and the ceiling knob rides the existing `SubmitTenantPolicyChange` path.

## Coverage

- Public API endpoints: 0/0 new endpoints; no OpenAPI/generated-client drift expected for Story 7.9.
- UI surfaces: 0/0 new surfaces; no Playwright/bUnit E2E applicable.
- Server-side delivery pipeline: existing evaluator/coordinator/store/contract tests cover AC1-AC6 and AC9; the new DI tests cover the remaining integration seam where registrations can drift from the tested coordinator.
- Critical paths covered: both-window throttle decisions, exactly-at-ceiling behavior, server-measured window math, overflow-to-digest, metadata-redacted digest/audit entries, `(tenant x recipient)` isolation, closed bounded ceiling validation, fail-closed audit, metadata-only audit evidence, and runtime service registration.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - passed, 1585/1585.
- [x] `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - passed, 482/482.

## Checklist Validation

- [x] API/behavioural tests generated where applicable.
- [x] E2E/UI tests assessed; none applicable because Story 7.9 has no UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Microsoft.Extensions.DependencyInjection APIs.
- [x] Tests cover the happy path: registered coordinator delivers an immediate push under the ceiling.
- [x] Tests cover a critical error/overflow case: the next delivery at the ceiling rolls into digest instead of being dropped.
- [x] All generated tests run successfully with the in-process xUnit runner.
- [x] Proper locators: N/A, no UI test; typed service resolution and direct seam assertions are used.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent, using a fresh service provider per test.
- [x] Test summary created.
- [x] Tests saved to the existing server test directory.
- [x] Summary includes coverage metrics.

## Next Steps

- When the deferred durable/Dapr-state binding and runtime digest-send caller land, add integration tests for persisted pending digest state and scheduled digest send.
