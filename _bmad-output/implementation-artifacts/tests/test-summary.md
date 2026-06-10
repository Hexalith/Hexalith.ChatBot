# Test Automation Summary - Story 3.7

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-10
**Story:** `_bmad-output/implementation-artifacts/3-7-failure-retry-and-blocked-state-rendering.md`
**Framework:** xUnit v3 + Shouldly + Microsoft.Playwright.

## Generated Tests

### API Tests
- [x] Existing contract, OpenAPI, generated-client, server projection, conformance, UI service, localization, and static CSS coverage remains in place for Story 3.7 failure/retry/blocked rendering.
- [x] No new API test file was required in this workflow run; the discovered gap was in UI E2E coverage for Story 3.7 blocked-reason variants.

### E2E Tests
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - Added `ProjectConversationFailureBlockedReasonVariantsShouldRenderReachableMetadataOnlyExplanations`.
- [x] Added a focused Story 3.7 fixture scenario covering authorization denied, unresolved participant, stale/expired evidence, and correction delayed failure-state rows.
- [x] Preserved existing Story 3.7 populated-stream coverage for retry queued, retry accepted, retry exhausted, duplicate suppressed, terminal failure, policy blocked, audit unavailable, dependency degraded, projection retryable, reprocess-created, forced-colors, reduced-motion, phone layout, append-only item IDs, and metadata-only leakage guards.

## Coverage

- Failure/retry/blocked UI states: retry queued, retry accepted, retry exhausted, duplicate suppressed, dependency degraded, projection retryable, policy blocked, audit unavailable, terminal failure, reprocess-created, authorization denied, unresolved participant, stale/expired evidence, and correction delayed.
- Accessibility and UX: actor-led accessible names, semantic article locators, keyboard-focusable inline explanations, non-color status text, forced-colors mode, reduced-motion behavior, and mobile-width layout checks.
- Critical negative cases: no raw exception, stack trace, provider diagnostic, prompt, model output, command payload, policy body, audit envelope, hidden evidence value, restricted resource name, hidden participant display name, or raw provider diagnostic in rendered test surfaces.
- API endpoints: S1 project conversation read surface remains covered by existing contract/projection/conformance tests; no endpoint was added.

## Validation

- `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 24/24.

## Checklist Validation

- [x] API tests generated/validated if applicable.
- [x] E2E tests generated/validated because S1 is a UI stream.
- [x] Tests use standard test framework APIs.
- [x] Tests cover happy path failure/retry/blocked rendering.
- [x] Tests cover critical error cases: authorization denied, unresolved participant, stale/expired evidence, correction delayed, policy blocked, audit unavailable, dependency degraded, projection retryable, retry exhausted, terminal failure, duplicate suppression, and metadata-only leakage guards.
- [x] All generated tests run successfully.
- [x] Tests use semantic/accessibility locators in the browser path.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent and fixture-driven.
- [x] Test summary created at the workflow output path.
- [x] Tests saved to the existing UI E2E test project.
- [x] Summary includes coverage metrics.
