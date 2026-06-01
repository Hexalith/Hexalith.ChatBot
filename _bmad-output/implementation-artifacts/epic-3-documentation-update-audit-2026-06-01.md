# Epic 3 Documentation Update Audit - Project Conversation Context, Files & Attachments

**Project:** Chatbot
**Epic:** 3 - Project Conversation Context, Files & Attachments
**Audit mode:** Autonomous retrospective follow-up, verified against story records and implementation files
**Date:** 2026-06-01

## Scope

This audit checked whether Epic 3 implementation learnings require updates to product, architecture, API, README, or configuration documentation.

## Verification Sources

- Sprint/story records: `_bmad-output/implementation-artifacts/3-*.md`
- Contract/API files: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationResponse.cs`, `src/Hexalith.ChatBot.Contracts/Queries/ProjectAiContextPackage.cs`, `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- Projection implementation: `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`, `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`, `src/Hexalith.ChatBot.Server/Program.cs`
- Attachment/context implementation: `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/AttachmentCaptureCoordinator.cs`, `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/AttachmentSafetyPolicy.cs`, `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/ProjectAiContextPackageAssembler.cs`
- UI implementation: `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/*`, `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`

## Updates Applied

### `README.md`

Status before audit: stale.

The README overview stopped at Epic 2 and did not mention Epic 3's S1 conversation projection, evidence panel, attachment capture/status work, or AI-context package manifest. It also did not warn readers that the implemented conversation path is currently ChatBot-owned rather than a `Hexalith.Conversations` adapter.

Update applied:

- Added Epic 3 capabilities to the project overview.
- Added a runtime note that project conversation context is a ChatBot-owned read projection in `chatbot-statestore`, exposed through the contract spine and consumed by S1.
- Added a guard against describing the path as a `Hexalith.Conversations` adapter before code exists.

### `_bmad-output/planning-artifacts/architecture.md`

Status before audit: stale in targeted sections.

The architecture document still described FR21-FR28 as conversation rendering via `Hexalith.Conversations` and mapped the feature to `Adapters/Conversations/`. The current implementation has no `Adapters/Conversations` directory. Epic 3 implemented a ChatBot-owned project-conversation projection store with in-memory and DAPR-backed implementations, contract query records, S1 UI state/service/component paths, and the AI-context package assembler.

Update applied:

- Reworded the FR21-FR28 capability statement to name the ChatBot-owned project-conversation projection and S1 UI.
- Reframed `Hexalith.Conversations` as a domain reference for possible later adapter patterns, not the current M0 implementation path.
- Expanded derived-store backing to include the S1 project-conversation read model in `chatbot-statestore`.
- Replaced `Adapters/Conversations/` in the FR mapping with `Projections/`, `Contracts/Queries/ProjectConversation*`, and UI `S1`.
- Replaced "Workflow sagas" in the internal flow with "coordinator/activity seams" to stay aligned with the current Dapr Workflow binding status.
- Updated the M0 happy path to say project conversation is materialized by ChatBot projections.

## No Update Required

### `_bmad-output/planning-artifacts/epics.md`

The Epic 3 and Epic 4 sections already describe the product-level requirements accurately, including the Story 3.14 rule that Epic 4 consumes only an authorized current context package manifest.

### `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`

The PRD remains a product requirement source rather than an implementation-claim document. Its FR21-FR34 and NFR9 language still matches the implemented direction.

### `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`

No manual documentation update was required. Story 3.14 regenerated the OpenAPI contract and generated client, updated the hash fixture, and review validation covered the project conversation and AI-context package schema.

## Carry-Forward

- Epic 4 story creation should treat Story 3.14's context package manifest as the only valid file-context input.
- If a future `Hexalith.Conversations` adapter is introduced, architecture should be updated again with the concrete adapter boundary and migration impact.
- Keep README and architecture updates tied to code evidence during retrospectives; broad PRD churn was not needed for this epic.
