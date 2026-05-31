# Test Automation Summary

**Story:** 2.7 - Association correction and supersession
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-05-31
**Framework:** xUnit v3 3.2.2, Shouldly 4.3.0, Microsoft.Playwright 1.60.0
**Run method:** `dotnet test` hit the sandbox VSTest socket limit; validation used builds plus compiled xUnit v3 executables.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - `AssociationCorrectionPreCommitAuditUnavailableShouldAbortAdmissionQueueReplayAndSkipDispatch` covers correction-specific audit-unavailable fail-closed behavior, coarse-idempotency admission abort, replay intent, operator alert, no dispatch, and metadata-only problem details.
- [x] Existing Story 2.7 gateway coverage in `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` covers correction idempotency replay, indefinite replay window, UI audit origin, safe conflict, and no target/rationale leakage.
- [x] Existing Story 2.7 dispatcher coverage in `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs` covers `CorrectEmailProjectAssociation` routing to the association aggregate with PascalCase, metadata-only EventStore payloads.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - `AssociationReviewShouldSubmitCorrectionThroughUiCommandSpineAndShowPartialStatus` covers target selection, rationale normalization, `CorrectEmailProjectAssociation` submission with `origin: ui`, projection refresh, downstream preview-only status, and unsafe text suppression.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - `AssociationReviewShouldKeepBlockedCorrectionReasonFocusableWithoutSubmitting` covers fail-closed blocked correction controls, focusable disabled reason, no command submission, and unauthorized detail suppression.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - `AssociationReviewShouldShowSafeCorrectionConflictWithoutLeakingPayload` covers idempotency conflict display without raw rationale, restricted addresses, raw provider payloads, or unauthorized project names.

## Coverage

- API/gateway paths: correction audit fail-closed path, admission abort, replay intent, operator alert, correction idempotency replay/conflict, safe problem details, UI surface origin attribution.
- UI features: correction submit workflow, target selection, rationale normalization, accepted/partial status, preview-only downstream impact, blocked reason accessibility, safe idempotency conflict.
- Critical error/safety cases: audit writer unavailable, projection-invalidation blocked reason, idempotency conflict, metadata-only diagnostics, raw evidence/rationale leakage suppression.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` - passed 198/198.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` - passed 26/26. Browser startup was unavailable in this sandbox, so the committed no-browser contract fallback path validated the same selectors/source contracts.
- [x] `git diff --check` - passed.
- [x] `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` and `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` were attempted first; VSTest aborted with sandbox `SocketException (13): Permission denied`.

## Checklist

- [x] API tests generated where applicable.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical error cases.
- [x] Tests use semantic roles, labels, and accessible names.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created in the configured output path.
- [x] Tests saved to the appropriate test projects.
- [x] Summary includes coverage metrics and validation commands.
