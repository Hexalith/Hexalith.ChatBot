# Test Automation Summary - Story 4.1

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-10
**Story:** `_bmad-output/implementation-artifacts/4-1-task-intent-detection-and-data-contract.md`
**Framework:** xUnit v3 + Shouldly + ASP.NET Core `WebApplicationFactory`; existing UI E2E uses Microsoft.Playwright with static fallback assertions.

## Generated Tests

### API Tests

- [x] Added `ProjectConversationEndpointShouldOmitDetectedIntentWhenTaskIntentCaptureFailsClosed` in `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`.
- [x] The test exercises the real `GET /api/v1/projects/{projectId}/conversation` endpoint with an authenticated project-scoped principal and in-memory projection store.
- [x] It covers the Story 4.1 fail-closed path where a redacted, non-actionable source item exposes safe classification metadata but no `detectedIntent` contract.
- [x] Existing API E2E coverage for `ProjectConversationEndpointShouldExposeCapturedTaskIntentMetadataOnly` continues to prove the happy path for captured task-intent metadata, ordered source evidence IDs, safe next action, message code, and metadata-only leakage protections.

### E2E Tests

- [x] Existing browser-backed UI E2E coverage in `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` renders actionable detected-intent metadata using semantic locators, forced-colors/reduced-motion checks, and metadata-only leakage assertions.
- [x] The added server API E2E gap closes the critical error path at the user-visible query contract before UI rendering.

## Coverage

- API endpoint coverage: captured task-intent happy path plus fail-closed redacted/non-actionable source behavior through the project conversation query contract.
- UI coverage: existing classification E2E verifies detected-intent display, no browser-side action buttons, source-evidence-first rendering, AI-summary opt-in behavior, and redacted classification explanation.
- Critical leakage coverage: tests assert no raw mail body, provider payload, prompt text, tool arguments, safe offset tokens, or task-intent identifiers leak on the wrong path.

## Validation

- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -parallel none -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldExposeCapturedTaskIntentMetadataOnly -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldOmitDetectedIntentWhenTaskIntentCaptureFailsClosed` - passed 2/2.

## Checklist Validation

- [x] API tests generated where applicable.
- [x] E2E/UI coverage exists for the Story 4.1 rendered query-contract surface.
- [x] Tests use standard xUnit v3, Shouldly, ASP.NET Core test-host APIs, and existing UI E2E Playwright patterns.
- [x] Tests cover happy path captured task-intent projection.
- [x] Tests cover a critical error case: redacted/non-actionable fail-closed source with no detected-intent exposure.
- [x] All generated tests run successfully.
- [x] Tests use endpoint-level user-visible contract assertions and semantic UI assertions.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent and fixture-driven.
- [x] Test summary created at the workflow output path.
- [x] Tests saved to existing test directories.
- [x] Summary includes coverage metrics.
