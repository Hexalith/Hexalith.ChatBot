# Test Automation Summary - Story 3.9

**Workflow:** `bmad-qa-generate-e2e-tests`  
**Date:** 2026-06-10  
**Story:** `_bmad-output/implementation-artifacts/3-9-why-this-project-evidence-and-provenance-panel.md`  
**Framework:** xUnit v3 + Shouldly + Microsoft.Playwright.

## Generated Tests

### API Tests

- [x] Existing contract/OpenAPI/generated-client tests validate the additive association routing-status fields, signal-class wire tokens, generated client availability, and raw evidence/body/provider/policy/audit field exclusions.
- [x] Existing server/conformance tests validate metadata-only routing-status enrichment, raw note/rationale exclusion, safe denial, and cross-tenant read-surface isolation.

### E2E Tests

- [x] `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` now validates all Story 3.9 required originating signal classes in the why-this-project panel fixture: explicit project identifier, mailbox routing rule, conversation thread identifier, human selection, and correction.
- [x] Existing browser coverage opens the panel from email and decision rows, follows the superseding correction link, focuses redacted evidence and close controls, verifies forced-colors/reduced-motion/mobile layout, and asserts metadata-only rendering.

## Coverage

- API/read surface: association routing-status query and generated client covered for metadata-only provenance fields.
- UI features: why panel open affordance from email and decision rows, redacted evidence explanation, superseding correction navigation, corrected-context metadata, and close/focus behavior.
- Signal classes: 5/5 required classes covered in E2E fixture assertions.
- Critical negative cases: no raw email body/subject/html, provider payload/source context, raw decision note, raw correction rationale, hidden project/file/participant names, raw policy body, audit envelope, prompt/output/tool payloads, local paths, secrets, or tokens in rendered test surfaces.

## Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-build -m:1 /nr:false --filter "FullyQualifiedName~ProjectConversationWhyProjectPanelShouldOpenFromEmailAndDecisionRowsAndRemainMetadataOnly"` - blocked by VSTest socket permission error in this sandbox.
- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -method Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests.ProjectConversationWhyProjectPanelShouldOpenFromEmailAndDecisionRowsAndRemainMetadataOnly` - passed 1/1.
- `DiffEngine_Disabled=true tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 24/24.

## Checklist Validation

- [x] API tests generated/validated if applicable.
- [x] E2E tests generated/validated because S1 is a UI stream.
- [x] Tests use standard test framework APIs.
- [x] Tests cover the happy path panel open/render flow.
- [x] Tests cover critical error/restriction cases: redacted evidence, unavailable details, correction-delayed/corrected context, safe denial through existing API/conformance coverage, and metadata-only leakage guards.
- [x] All generated/validated tests run successfully through the compiled xUnit runner fallback.
- [x] Tests use semantic/accessibility locators in the browser path.
- [x] Tests have clear descriptions.
- [x] No hardcoded waits or sleeps were added.
- [x] Tests are independent and fixture-driven.
- [x] Test summary created at the workflow output path.
- [x] Tests saved to the existing UI E2E test project.
- [x] Summary includes coverage metrics.
