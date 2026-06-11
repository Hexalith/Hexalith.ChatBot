# Test Automation Summary - Story 7.26 (Rate-limit outbound channel)

Workflow: `bmad-qa-generate-e2e-tests` · Framework: **xUnit v3** (.NET 10, compiled in-process runners) · Date: 2026-06-11

## Scope

Story 7.26 covers the outbound channel rate-limit policy mutation and send-seam enforcement. QA verified the existing API/behavior coverage against AC-10 and the workflow checklist, found two automation gaps, and closed both with test-only changes.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
  - Added `CommandGatewayApi_ShouldAcceptOutboundChannelRateLimitAsSinglePolicyAdminMutationThroughUiSpine`.
  - Covers HTTP `/api/v1/commands` submission of `SubmitOutboundChannelRateLimit`, single human policy-admin authority, pre/post audit envelopes, metadata-only response redaction, idempotency recording, budget/window refs, and no `outbound-channel-new-state:rate-limited` lifecycle-state ref.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/OutboundChannelRateLimitE2ETests.cs`
  - Added browser-backed UI fixture with deterministic static fallback.
  - Covers transient `outbound_channel_rate_limited` guidance, `retry-later` next action, `dependency-degraded` reason, finite capacity metrics (`budget`, observed count, throttled flag), fail-closed approved send with no external dispatch, under-budget/sibling/cross-tenant sends unaffected, draft/approval/decision actions remaining inspectable, prior artifacts visible, and metadata-only leakage checks.

## Existing Coverage Verified

- Authorization: policy-admin and tenant-admin allowed; non-policy human scopes, service clients, and AI actors denied.
- Bounds: out-of-range/undeclared values rejected at gateway and aggregate; enforcement safe-default fallback never raises the cap.
- Send seam: at-budget channel fails closed before `IOutboundMailboxSender.SendAsync`; under-budget sends proceed; Disabled/Quarantined reasons take precedence; sibling channels and other tenants remain isolated.
- Aggregate/gateway/audit: configure/no-op/reject flows, fail-closed pre-commit audit, metadata-only audit refs, and distinct `outbound_channel_rate_limited` send rejection are covered.
- Contracts: OpenAPI, generated client, checksum, message catalog, bounds, schema version, and safe-token serialization remain covered.

## Coverage

- API/behavior surfaces: covered by authorization, aggregate, dispatcher, gateway/audit, HTTP admission API, contract, client-generation, and catalog tests.
- UI/E2E surfaces: covered by the new outbound-channel rate-limit fixture.
- Critical error cases: unauthorized actor denial, invalid/out-of-bounds budget rejection, audit-unavailable fail closed, send-seam throttling before adapter dispatch, control-state precedence, and metadata leakage checks.
- Gaps closed in this run: **2/2** discovered gaps auto-applied.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - Build succeeded, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - Build succeeded, 0 warnings, 0 errors.
- [x] Focused server API test - Total 1, Failed 0.
- [x] Focused UI E2E class - Total 2, Failed 0.
- [x] Full `Hexalith.ChatBot.Server.Tests` compiled runner - Total 1604, Failed 0.
- [x] Full `Hexalith.ChatBot.UI.E2E.Tests` compiled runner - Total 104, Failed 0.

## Checklist Validation

- [x] API tests generated/updated where applicable.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, ASP.NET test host, and Playwright APIs.
- [x] Happy path covered: policy-admin HTTP mutation accepted, under-budget/sibling/cross-tenant sends proceed, and draft/approval workflows stay inspectable.
- [x] Critical error behavior covered: at-budget send is held with no external dispatch and stable `outbound_channel_rate_limited` guidance; metadata leakage checks included.
- [x] Tests use semantic accessible locators.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and browserless fallback remains deterministic.
- [x] Test summary created with coverage metrics and validation commands.
