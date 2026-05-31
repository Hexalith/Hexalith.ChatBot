# Test Automation Summary

**Story:** 2.8 - Correction propagation contract
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-01
**Framework:** xUnit v3 3.2.2, Shouldly 4.3.0, Microsoft.Playwright 1.60.0
**Run method:** `dotnet test` hit the sandbox VSTest socket limit; validation used builds plus compiled xUnit v3 executables.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Contracts.Tests/AssociationContractTests.cs` - `AssociationRoutingStatusShouldExposeCorrectionPropagationContractSafely` covers the public routing-status JSON contract for `Correcting`, propagation progress, stale corrected-context blocking, workflow id, required/completed store keys, safe next action, and metadata-only redaction.
- [x] Existing story 2.8 server tests cover propagation aggregate lifecycle, DAPR-style M0 fan-out/fan-in coordination, delayed alerting, projection progress merge, source-version ordering, and corrected-context readiness blocking.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - `AssociationReviewShouldSurfaceCorrectionPropagationProgressAndBlockCorrectedContextUse` covers `Correcting`, progress/ETA, responsible owner, safe wait action, disabled correction and AI action controls, focusable blocked reason, no submit, and unsafe text suppression.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - `AssociationReviewShouldSurfaceCorrectionDelayedEscalationWithoutStartingNewWorkflow` covers `Correction-delayed`, operations escalation, workflow instance continuity, status refresh without starting a new workflow, and unsafe text suppression.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs` - `AssociationReviewShouldShowCompletePropagationAndAllowPreparedContextActions` covers completed propagation, all-store acknowledgement, success status, and AI/command preparation becoming available.

## Coverage

- API contracts: association routing status propagation fields 1/1 public query surface covered.
- UI propagation states: `Correcting`, `Correction-delayed`, and `complete` covered.
- Critical error/safety cases: corrected-context blocked, delayed propagation escalation, no duplicate workflow start from status refresh, metadata-only display, raw payload/address/project/exception suppression.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests` - passed 29/29 via the in-process xUnit v3 runner.
- [x] `dotnet build tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests` - passed 81/81.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `git diff --check` - passed.
- [x] `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` and `dotnet test tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore -m:1 /nr:false` were attempted first; VSTest aborted with sandbox `SocketException (13): Permission denied`.

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
