# Test Automation Summary

**Story:** 2.1 - Microsoft 365 mailbox intake and source-identity capture
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-05-31
**Framework:** xUnit v3 3.2.2 and Shouldly 4.3.0
**Run method:** compiled xUnit v3 executables, matching the existing sandbox validation path.

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - mailbox-intake gateway/API path admits the allowlisted command, uses the `message-intake` idempotency class, suppresses duplicate provider delivery, aborts on pre-commit audit outage, normalizes mailbox/provider IDs with NFC before hashing, and fails closed before durable-state work when tenant context is missing.
- [x] Existing server API tests continue to cover user-safe 401/403/409/503 problem details, audit redaction, operation status, audit history, and command submission behavior.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs` - deterministic fake-Graph worker lane covers created notification submission, duplicate notification source identity, retryable Graph degradation, revoked credential recovery, foreign mailbox fail-closed behavior, non-UTC timestamp conversion to UTC with source timezone preserved, and opaque provider state non-forwarding.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/M365MailboxEventActorIsolationTests.cs` - cross-tenant mailbox event actor negative path remains covered.
- [x] Tier-3 Aspire E2E binary was run; Docker/DAPR-gated live tests self-skipped as designed in this environment.

### Contract And Architecture Tests
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/MailboxIntakeContractTests.cs` - mailbox intake contract field requirements, camelCase JSON, UTC timestamp serialization, and ULID-only identity.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests` - worker/adapter dependency boundaries prevent Server gateway, audit, idempotency, DAPR, and EventStore processor bypasses.

## Coverage

- API/gateway paths: submit, duplicate replay, audit unavailable, missing tenant, redacted problem response, operation status, audit history.
- Worker paths: created notification, duplicate notification, throttled/subscription-expired/token-expired/partial-access retryable fetches, revoked permission, foreign mailbox, timestamp normalization, opaque provider state handling.
- Contract paths: command schema, source identity schema, JSON casing, UTC timestamp serialization, ULID validation.
- UI features: not applicable for Story 2.1; no mailbox UI was introduced by the story.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` - passed 69/69.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` - passed 120/120.
- [x] `tests/Hexalith.ChatBot.Workers.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Workers.Tests` - passed 11/11.
- [x] `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` - passed 33/33.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests` - passed 53/53.
- [x] `tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests` - passed 2/2 runnable, 2 skipped by Tier-3 Docker/DAPR opt-in guard.

## Checklist

- [x] API tests generated where applicable.
- [x] E2E/worker-flow tests generated where UI is not applicable.
- [x] Tests use standard xUnit v3 and Shouldly APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases.
- [x] Tests use clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created in the configured output path.
- [x] Coverage metrics and validation commands recorded.
