# Test Automation Summary

**Story:** 3.3 - Participant rendering in the conversation stream
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-01
**Framework:** xUnit v3, Shouldly, Microsoft.Playwright
**Run method:** `dotnet test` aborts in this sandbox at VSTest socket startup; validation used `dotnet build` plus the compiled xUnit v3 executable.

## Generated Tests

### API Tests

- [x] Existing Story 3.3 contract/API coverage is present in `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs` for additive participant DTO fields, stable participant wire tokens, generated-client availability, OpenAPI shape, and raw identity/provider/body field exclusion.
- [x] Existing Story 3.3 server projection coverage is present in `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` and `tests/Hexalith.ChatBot.Server.Tests/Projections/ParticipantResolutionProjectionTests.cs` for participant-before-association, association-before-participant, stale replay, correction state, tenant/project partitioning, and safe display fallback behavior.
- [x] Existing read-surface isolation coverage is present in `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs` for safe denial and metadata-only bodies.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - retained populated S1 stream coverage for mailbox, system decision, internal participant, external participant, unresolved participant, and restricted participant items with stable ordering and metadata-only body assertions.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - added `ProjectConversationParticipantItemsShouldExposeOrderedMetadataAndReachableUnavailableReasons` for participant-specific accessible-name discovery, evidence/status/actor/timestamp ordering, participant resolution/source participant metadata, allowed review actions, keyboard focusability, unresolved reason reachability, restricted Party ID suppression, and raw detail exclusion.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - extended `ProjectConversationPopulatedStreamShouldRespectMotionForcedColorsAndPhoneLayout` to assert participant item and unavailable-reason reduced-motion behavior, forced-colors support, and phone-width wrapping in addition to mailbox item behavior.
- [x] Existing loading, empty, and unauthorized/redacted E2E paths remain covered for persistent context, safe next action reachability, forced-colors denial, and unsafe text suppression.

## Coverage

- API endpoints: 1/1 Story 3.3 read endpoint covered (`GET /api/v1/projects/{projectId}/conversation`).
- UI states: 4/4 S1 E2E states covered: loading, populated stream, empty, unauthorized/redacted.
- Participant states: 4/4 populated participant display states covered: internal, external, unresolved, restricted.
- Participant metadata: participant type, status, resolution id, source participant id, Party ID when allowed, blocked reason, evidence reference/fingerprint, allowed review actions, source mailbox id, lifecycle state, safe next action, correlation id, actor label, timestamp, redaction, and confidence chip coverage.
- Accessibility/responsive modes: semantic roles and accessible names, keyboard focusability, reachable unavailable reasons, forced-colors, reduced-motion, and phone layout covered.
- Critical safety cases: metadata-only rendering, restricted Party ID suppression, no raw email address evidence, no provider display name, no unauthorized party name, no restricted party detail, no raw provider payload, no raw exception text, and no hidden diagnostic text.

## Validation

- [x] `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 6/6.
- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectConversationContractTests` - passed 3/3.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests` - passed 16/16.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ParticipantResolutionProjectionTests` - passed 4/4.
- [x] `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests` - passed as part of targeted UI validation.
- [x] Full compiled test executable sweep under `tests/*/bin/Debug/net10.0/` - passed with exit code 0.
- [x] `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests` - passed 1/1.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none -class Hexalith.ChatBot.Conformance.Tests.CrossTenantReadSurfaceIsolationTests` - passed 8/8.
- [x] `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-restore --filter "FullyQualifiedName~ProjectConversationE2ETests"` - attempted; aborted before executing tests due to sandbox VSTest `SocketException (13): Permission denied`.

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
