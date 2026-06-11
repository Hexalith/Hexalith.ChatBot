# Test Automation Summary

Story: 8.7a - Durable control-state/rate-limit projection and enforcement-seam activation
Date: 2026-06-11

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Projections/GovernedOperationProjectionTests.cs` - Added endpoint-level coverage proving a published control event is accepted through `/chatbot/events/governed-operations`, projected into the durable control-state store, and remains idempotent on replay.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Projections/GovernedOperationProjectionTests.cs` - Added provider coverage for service-client, AI-actor, command-capability, and outbound-channel control/rate-limit seams.

### E2E Tests

- [x] Existing API E2E and browser-fixture suites remain the applicable workflow coverage for story 8.7a; no new UI route is required by the story.
- [x] New API/projection tests cover the runtime event-ingestion path behind the enforcement seams instead of adding a UI-only fixture.

## Coverage

- Control-event projection: all governed subject classes translate to `(tenantId, subjectClass, subjectRef)` projection notifications.
- Endpoint behavior: control events return `200 OK`, materialize the projected state, and replay as an idempotent success.
- Runtime providers: projected disabled/quarantined state maps across service-client, AI-actor, command-capability, and outbound-channel seams.
- Freshness: revocation-sensitive state fails closed after 60 seconds; ordinary policy state remains accepted at 5 minutes and fails closed after the 5-minute bound.
- Rate limits: all runtime rate-limit providers return projected budgets and out-of-bounds budgets resolve to safe defaults, never a raised cap.
- Isolation: admitted histories stay tenant-partitioned and subject-class-partitioned even when subject refs match.

## Validation

- [x] `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore --filter FullyQualifiedName~GovernedOperationProjectionTests -m:1 /nr:false` - Compiled successfully; VSTest execution aborted because the sandbox denied socket creation.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.GovernedOperationProjectionTests` - Total 29, Errors 0, Failed 0, Skipped 0.
- [x] `git diff --check` - Passed.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E/API workflow tests generated for the projection subscriber path.
- [x] Tests use standard xUnit v3, WebApplicationFactory, and Shouldly APIs.
- [x] Tests cover happy path control-event projection.
- [x] Tests cover critical error/fail-closed cases for stale projection state and out-of-bounds budgets.
- [x] Tests use semantic HTTP endpoint coverage where applicable and no hardcoded waits.
- [x] Tests have clear behavior descriptions.
- [x] Tests are independent and have no order dependency.
- [x] Test summary created with coverage metrics.
