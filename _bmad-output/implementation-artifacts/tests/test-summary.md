# Test Automation Summary

Story: 7.20 - Rate-limit AI actor
Date: 2026-06-11

## Generated Tests

### API Tests
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` - Added API-level coverage that a single human `policy-admin` submits `SubmitAiActorRateLimit` through `/api/v1/commands`, dispatches once, writes pre/post audit envelopes, records command-execution idempotency, and keeps the response metadata-only.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` - Added API-level coverage that an over-budget AI actor is denied before dispatch with `ai_actor_rate_limited`, no audit envelopes/idempotency side effects, retry-later guidance, and redacted metadata-only problem details.

### E2E Tests
- [x] `CommandGatewayAdmissionApiE2ETests` now covers story 7.20 through the in-memory `WebApplicationFactory<Program>` command-admission pipeline, including authentication claims, tenant binding, authorization, rate-limit provider/history seams, problem mapping, redaction, and dispatch suppression.

## Coverage

- API endpoints: `/api/v1/commands` story 7.20 paths covered for accepted admin mutation and rate-limited AI-actor denial.
- UI features: no browser UI surface was applicable for this story; the existing command-admission API E2E harness is the relevant user-facing workflow test layer.
- Critical acceptance paths added: 2/2 discovered E2E gaps closed.
- Existing lower-level coverage retained: authorization, aggregate, audit, contract, catalog, generated-client parity, and final-gate service-client grant tests were already present.

## Fixes Applied From Test Findings

- [x] `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs` now maps `ChatBotAuthorizationReasonCodes.AiActorRateLimited` to `ChatBotMessageCodes.AiActorRateLimited`, treats it as a known authorization reason, and marks the API problem as retryable so the catalog-backed `retry-later` response is surfaced.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -method "Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayAdmissionApiE2ETests.CommandGatewayApi_ShouldAcceptAiActorRateLimitAsSinglePolicyAdminMutationThroughUiSpine" -method "Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayAdmissionApiE2ETests.CommandGatewayApi_ShouldReturnTypedRedactedRetryLaterResponseForRateLimitedAiActor"` - 2 total, 0 failed.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none -class "Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayAdmissionApiE2ETests"` - 44 total, 0 failed.

## Notes

- `dotnet test ...` via VSTest could not run in this sandbox because the runner opens a TCP listener and hit `System.Net.Sockets.SocketException (13): Permission denied`; the xUnit v3 in-process runner was used for execution after a successful build.
