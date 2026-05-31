# Test Automation Summary

**Story:** 2.6 - Association decision recording, evidence preservation, and notes
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-05-31
**Framework:** xUnit v3 3.2.2, Shouldly 4.3.0, Microsoft.Playwright 1.60.0
**Run method:** `dotnet test` hit the sandbox VSTest socket limit; validation used serial builds plus compiled xUnit v3 executables.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - `AssociationDecisionShouldUseTwentyFourHourActorScopedIdempotencyAndUiAuditOrigin` covers association-decision idempotency class, 24-hour replay window, duplicate suppression, UI surface origin, evidence-reference audit facts, and note redaction from audit.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs` - `AssociationDecisionPreCommitAuditUnavailableShouldAbortAdmissionQueueReplayAndSkipDispatch` covers audit-unavailable fail-closed behavior, admission abort, replay intent, operator alert, and metadata-only problem details.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/Stages/AcceptedCommandDispatcherTests.cs` - `DispatchShouldRouteAssociationDecisionToAssociationAggregateWithPascalCaseMetadataOnlyPayload` covers command routing to the association aggregate, PascalCase EventStore payloads, UI provenance extensions, and no raw mailbox payload leakage.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - `AssociationReviewShouldSubmitDecisionThroughUiCommandSpineAndRefreshStatus` covers candidate selection, bounded note normalization, `AssociateEmailToProject` submission with `origin: ui`, projection refresh, audit reconciling feedback, already-decided disabled reason, and unsafe text suppression.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - `AssociationReviewShouldShowSafeIdempotencyConflictWithoutLeakingDecisionPayload` covers safe idempotency-conflict display without raw notes, restricted addresses, raw provider payloads, or unauthorized project names.

## Coverage

- API/gateway paths: association decision idempotency, audit fail-closed path, EventStore dispatch routing, evidence-reference audit metadata, UI surface origin attribution.
- UI features: S2 decision submit workflow, note normalization, accepted/projection-pending feedback, audit-reconciling feedback, already-decided disabled reason, safe idempotency conflict.
- Critical error/safety cases: duplicate decision replay, audit writer unavailable, idempotency conflict, metadata-only problem/details, raw evidence and note leakage suppression.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -class Hexalith.ChatBot.Server.Tests.Gateway.CommandGatewayTests -class Hexalith.ChatBot.Server.Tests.Gateway.Stages.AcceptedCommandDispatcherTests -parallel none -noLogo` - passed 50/50.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -class Hexalith.ChatBot.UI.E2E.Tests.GovernedOperationsVisualFoundationE2ETests -parallel none -noLogo` - passed 23/23.
- [x] `git diff --check` - passed.
- [x] `dotnet test ...` attempted first; VSTest aborted with sandbox `SocketException (13): Permission denied`, so compiled xUnit v3 executables were used per project guidance.

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
