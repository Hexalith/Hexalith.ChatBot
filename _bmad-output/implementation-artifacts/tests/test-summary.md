# Test Automation Summary

Story: 7.26 - Rate-limit outbound channel
Date: 2026-06-11

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs` - Added HTTP admission API coverage for `SubmitOutboundChannelRateLimit` as a single policy-admin mutation through the UI spine.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/OutboundChannelRateLimitE2ETests.cs` - Added browser-backed E2E coverage for transient outbound-channel rate-limit guidance, held approved sends, unaffected under-budget/sibling/other-tenant sends, inspectable draft/approval actions, prior artifact visibility, finite capacity metrics, and metadata-only safe guidance.

## Coverage

- API/behavior surfaces: existing tests cover authorization, aggregate configure/reject/no-op behavior, bounded fallback, dispatcher send-seam enforcement, audit fail-closed behavior, OpenAPI/client parity, generated-client checksum, and catalog guidance. This run added the missing HTTP admission API path.
- E2E/UI surfaces: this run added the missing outbound-channel rate-limit UI fixture.
- Critical error cases: invalid budget rejection, unauthorized actor denial, audit-unavailable fail closed, at-budget send rejection before adapter dispatch, control-state precedence, and metadata leakage checks are covered.
- Gaps closed in this run: 2 discovered gaps auto-applied.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - Build succeeded, 0 warnings, 0 errors.
- [x] `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - Total 1604, Failed 0.
- [x] `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - Total 104, Failed 0.

## Checklist Validation

- [x] API tests generated/updated where applicable.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, ASP.NET test host, and Playwright APIs.
- [x] Happy path covered.
- [x] Critical error behavior covered.
- [x] Tests use semantic accessible locators.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent.
- [x] Test summary created with coverage metrics and validation commands.
