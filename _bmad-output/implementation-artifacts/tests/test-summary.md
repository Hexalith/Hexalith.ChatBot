# Test Automation Summary

**Story:** 3.11 - Informational/actionable classification, AI-summary distinction, and review history
**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-01
**Framework:** xUnit v3, Shouldly, Microsoft.Playwright
**Run method:** compiled xUnit v3 executables plus solution build, matching the repository's sandbox-safe validation pattern.

## Generated Tests

### API Tests

- [x] `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs` - contract serialization coverage for classification, detected intent, AI summary provenance, review history, additive compatibility, nullable behavior, and leakage negatives.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs` - OpenAPI coverage for exact informational/actionable and detected-action wire values.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/SharedContractTypeTests.cs` - shared enum-member wire-name coverage.
- [x] `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs` - generated-client parity and checksum coverage.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` - server projection coverage for informational/actionable mapping, detected intent, safe unavailable classification, AI provenance, append-only review history, and replay/order tolerance.
- [x] `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs` - API response coverage for S1 conversation classification, status, review-history, redaction, and metadata-only leakage behavior.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests` - tenant isolation and safe-collapse coverage for unauthorized, foreign, malformed, and unsafe contexts.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - Story 3.11 E2E coverage for visible informational/actionable badges, deterministic metadata, detected intent, action kind, and safe next action.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - source-evidence-default and opt-in collapsible `AI summary` coverage with provenance string and non-leakage assertions.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` - append-only review-history coverage with chronological ordering, unique accessible names, `aria-live="off"`, redaction states, forced-colors, reduced-motion, and phone-width layout.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs` - UI service/model mapping coverage for generated-client classification, detected intent, AI summary provenance, and review history.
- [x] `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs` - EN/FR label and component-localization contract coverage.

## Coverage

- API endpoints: S1 project conversation read path, contract/OpenAPI/generated-client spine, server projection mapping, safe redaction/unavailable states, review-history metadata, and conformance tenant-isolation paths are covered.
- UI features: classification badges, actionable detected intent, AI-summary/source-evidence distinction, provenance, append-only review history, localization, mobile layout, forced colors, reduced motion, keyboard/screen-reader accessible names, and negative metadata-only assertions are covered.
- Critical safety cases: raw email body/subject/html, provider payload/source context, raw decision notes, raw correction rationale, unauthorized project/file/participant names, hidden evidence values, raw policy/audit envelope, command payloads, prompts, outputs, tool payloads, local paths, tokens, tenant ids, and secrets remain excluded from API/UI/test fixture output.

## Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed with 0 warnings and 0 errors.
- [x] `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none` - passed 89/89.
- [x] `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none` - passed 15/15.
- [x] `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests -class Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests` - passed 69/69.
- [x] `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none` - passed 58/58.
- [x] `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests` - passed 14/14.
- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 12/12.

## Checklist

- [x] API tests generated if applicable.
- [x] E2E tests generated for the UI surface.
- [x] Tests use standard xUnit v3, Shouldly, and Playwright APIs.
- [x] Tests cover happy path.
- [x] Tests cover critical redaction, unavailable, tenant-isolation, replay/order, and leakage cases.
- [x] Tests use semantic roles, labels, accessible names, and stable metadata selectors.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps.
- [x] Tests are independent and order-free.
- [x] Test summary created in the configured output path.
- [x] Tests saved to the appropriate test projects.
- [x] Summary includes coverage metrics and validation commands.
