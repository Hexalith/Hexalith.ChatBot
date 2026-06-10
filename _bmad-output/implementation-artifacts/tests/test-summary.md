# Test Automation Summary - Story 2.8

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-10
**Story:** `_bmad-output/implementation-artifacts/2-8-correction-propagation-contract.md`
**Framework:** xUnit v3 + Shouldly + Microsoft.Playwright, using the repository's compiled xUnit executable workaround when VSTest sockets are unavailable.

## Generated Tests

### API Tests

- [x] Existing Story 2.8 API coverage validated in `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`:
  - authorized UI-origin `CorrectEmailProjectAssociation` request passes the command gateway, records correction idempotency, emits pre/post audit envelopes, forwards metadata-only payload to EventStore, and starts correction propagation commands;
  - projection-invalidation dependency unavailable fails closed before durable correction or propagation state is written and returns the catalog-backed safe problem `association_correction_projection_unavailable`.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/CorrectionPropagationContractE2ETests.cs` - added correction propagation user-workflow E2E coverage:
  - submitting a correction records the UI-origin correction command, shows `Correcting`, displays M0 store progress, keeps a stable workflow instance id, moves focus to status, and blocks AI preparation while corrected context is stale;
  - acknowledging remaining M0 stores clears the workflow to `Corrected`, updates downstream impact to `complete`, and re-enables corrected-context AI preparation;
  - `Correction-delayed` surfaces the P2 incident signal, responsible owner, safe escalation action, and completes without creating a new workflow instance;
  - workflow/projection/audit/idempotency dependency failures keep correction submit disabled, show catalog-backed reasons, and record zero durable writes or workflow starts.

## Coverage

- API endpoints: 2/2 targeted Story 2.8 command-gateway flows covered by existing API E2E tests: accepted correction propagation start and projection-dependency fail-closed rejection.
- UI features: 3/3 targeted correction propagation E2E scenarios covered: correcting progress, delayed recovery, and dependency fail-closed blocking.
- Critical error cases: stale corrected context, delayed SLO breach, workflow unavailable, projection invalidation unavailable, audit unavailable, and idempotency store unavailable.

## Test Results

- `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -class Hexalith.ChatBot.UI.E2E.Tests.CorrectionPropagationContractE2ETests -parallel none -noLogo` - passed: 3 total, 0 errors, 0 failed, 0 skipped.
- `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore --filter CorrectionPropagationContractE2ETests -m:1 /nr:false` - build succeeded, then VSTest aborted before execution due to sandbox socket restriction: `System.Net.Sockets.SocketException (13): Permission denied`. Tests were executed through the compiled xUnit v3 executable.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- `git diff --check` - passed for tracked changes. The new untracked E2E test file was also checked with `git diff --check --no-index`; no whitespace diagnostics were reported.

## Checklist Validation

- [x] API tests generated/validated because Story 2.8 has API command-gateway behavior.
- [x] E2E tests generated because UI exists.
- [x] Tests use standard framework APIs: xUnit v3, Shouldly, and Playwright semantic locators.
- [x] Tests cover happy path: UI-origin correction propagation from `Correcting` to `Corrected`.
- [x] Tests cover critical error cases: delayed SLO breach, stale corrected context blocking, and fail-closed dependency unavailability.
- [x] All generated tests run successfully through the compiled xUnit v3 executable.
- [x] Tests use semantic/accessibility locators.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent and fixture-driven.
- [x] Test summary created at the workflow output path.
- [x] Tests saved to the existing UI E2E test project.
- [x] Summary includes coverage metrics.
