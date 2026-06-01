# Test Automation Summary

**Story:** 3.7 - Failure, retry, and blocked-state rendering
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-01
**Framework:** xUnit v3, Shouldly, Microsoft.Playwright
**Run method:** compiled xUnit v3 executable, matching the story's sandbox fallback guidance.

## Generated Tests

### API Tests

- [x] Existing Story 3.7 contract/API coverage is present in `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`, and `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs` for additive failure-state DTO fields, stable wire tokens, generated-client availability, OpenAPI shape, and raw exception/diagnostic/payload field exclusion.
- [x] Existing Story 3.7 server/conformance coverage is present in `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` and related conformance tests for metadata-only failure-state projection, append-only IDs, duplicate/stale replay handling, out-of-order failure/retry events, and tenant/project partitioning.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - expanded populated S1 stream coverage for retry queued, retry accepted, retry exhausted, duplicate suppressed, terminal failure, policy blocked, audit unavailable, dependency degraded, projection retryable, and reprocess-created rows.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added failure-state metadata assertions for actor-leading accessible names, focusability, catalog code/version/detail visibility, retry counts, duplicate safety, blocked reasons, dependency degradation, audit unavailability, terminal rule, safe next action, and append-only item ordering.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - extended forced-colors, reduced-motion, phone-layout, and focusable unavailable-explanation assertions to failure-state rows.

## Coverage

- API endpoints: 1/1 Story 3.7 read endpoint covered (`GET /api/v1/projects/{projectId}/conversation`) by existing contract/server/conformance tests.
- UI states: 4/4 S1 E2E states covered: loading, populated stream, empty, unauthorized/redacted.
- Failure/retry/blocked rows: 10/10 required Story 3.7 E2E fixture states covered: retry queued, retry accepted, retry exhausted, duplicate suppressed, terminal failure, policy blocked, audit unavailable, dependency degraded, projection retryable, reprocess-created.
- Critical safety cases: metadata-only rendering, append-only failure-state item IDs, status text not color-only, reachable blocked/audit explanations, reduced-motion/forced-colors behavior, no raw exception, stack trace, provider diagnostic, prompt, model output, command payload, policy body, audit envelope, or hidden resource text.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 7/7.
- [x] Senior review validation (2026-06-01): targeted compiled xUnit v3 runners passed for `ProjectConversationContractTests` (3/3), `ClientGenerationTests` (14/14), `ProjectConversationProjectionTests` (32/32), `ChatBotLocalizationContractTests` + `ProjectConversationServiceTests` (12/12), and `ProjectConversationE2ETests` (7/7).
- [x] Senior review regression coverage added for expanded blocked-reason wire values, regenerated client enum values, EN/FR failure catalog mappings, and unsafe failure metadata token suppression.

## Checklist

- [x] API tests generated or already present where applicable.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases.
- [x] Tests use semantic roles, labels, accessible names, and stable metadata selectors.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created in the configured output path.
- [x] Tests saved to the appropriate test project.
- [x] Summary includes coverage metrics and validation commands.
