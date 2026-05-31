# Epic 2 Documentation Update Audit

**Project:** Chatbot  
**Epic:** 2 - Email Intake & Project Association  
**Mode:** Autonomous YOLO documentation verification after retrospective  
**Retrospective:** `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-01.md`

## Verification Method

Candidate updates were proposed from Epic 2 story completion notes, senior review findings, sprint status, architecture/PRD/epic docs, README content, OpenAPI contract, and implementation code. Each candidate below was checked against current docs and code before editing.

Implementation evidence checked:

- `src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs`
- `src/Hexalith.ChatBot.AppHost/Program.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprAssociationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/DaprOperationStatusStore.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs`
- `src/Hexalith.ChatBot.Contracts/Serialization/JsonEnumMemberStringConverter.cs`
- Epic 2 story records in `_bmad-output/implementation-artifacts/2-*.md`

## Documentation Candidates

| Candidate doc | Proposed update | Verification result | Action |
|---|---|---|---|
| `README.md` | Update stale Epic 1-only overview and clarify correction propagation runtime status. | Needed. README still described email intake/association as later work, while Epic 2 code and story records show mailbox intake, association, correction, and retry/failure foundations are implemented. Code also has no `Dapr.Workflow` package/runtime binding. | Updated. |
| `_bmad-output/planning-artifacts/architecture.md` | Reconcile Dapr Workflow runtime wording with actual Epic 2 implementation. | Needed. The doc claimed hosted Dapr Workflow runtime/coordinator behavior in several places. Code has `DaprCorrectionPropagationCoordinator`, command writer/activity seams, deterministic workflow IDs, durable lifecycle events, and no `Dapr.Workflow`/`AddDaprWorkflow` runtime binding. | Updated. |
| `_bmad-output/planning-artifacts/epics.md` | Correct stale DAPR resource names and workflow-runtime wording. | Needed. The doc still named `chatbot-eventstore` and generic `pubsub`; code uses `statestore`, `chatbot-statestore`, and `chatbot-pubsub`. Story 2.8 wording also overstated hosted Dapr Workflow runtime. | Updated. |
| `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` | Update API schema for Epic 2 lifecycle, association routing, retry/failure fields, and stable enum tokens. | Not needed. OpenAPI already includes `AssociationRoutingStatus`, `Correcting`, `Correction-delayed`, `retryCount`, `maxAttempts`, `duplicateSafetyNote`, `ownerRole`, `failureReasonCode`, `fail-closed`, and `scorer-error`. Contract/client tests assert these shapes and tokens. | Discarded. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` | Change product requirements to match implementation. | Not needed. PRD content still describes product requirements and acceptance expectations. The implementation/runtime discrepancy belongs in architecture and planning docs, not by lowering PRD requirements. | Discarded. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` | Update confidence thresholds, retry idempotency, or M365 assumptions. | Not needed. Addendum values match implemented defaults and story behavior checked during Epic 2: `T_high`, `T_low`, retry idempotency key shape, scorer fail-closed behavior, and M365 provider-pass-through assumptions remain valid. | Discarded. |

## Applied Updates

### README

- Updated the project overview to include Epic 2 capabilities: Microsoft 365 mailbox intake, participant resolution, deterministic association scoring, S2 association review, decision/correction history, correction-propagation metadata, and duplicate/retry/failure status foundations.
- Added a runtime clarification that Epic 2 correction propagation currently runs through the server coordinator/activity seam and durable EventStore lifecycle events, not a hosted Dapr Workflow runtime binding.

### Architecture

- Replaced "DAPR Workflow runtime hosted" and "DAPR Workflow coordinates" claims with the actual Epic 2 implementation shape: DAPR-ready coordinator/activity seam, deterministic workflow identifiers, durable start/acknowledge/complete/delayed events, and pending hosted Dapr Workflow binding before production saga claims.
- Updated the module structure and internal decomposition to reflect the implemented coordinator seam and pending runtime binding.

### Epics

- Replaced stale DAPR resource naming with actual code-backed names: `statestore`, `chatbot-statestore`, `chatbot-pubsub`, `chatbot.events`, and `deadletter.chatbot.events`.
- Clarified local `accesscontrol.local.yaml` versus production deny-by-default `accesscontrol.yaml`.
- Updated Story 2.8 acceptance wording to reflect the implemented coordinator/activity seam while preserving the requirement that hosted Dapr Workflow binding is needed before production saga claims.

## Discarded Updates

- API documentation update discarded because OpenAPI and generated-client tests already match Epic 2 implementation.
- PRD update discarded because no product-requirement discrepancy was verified.
- Addendum update discarded because confidence, retry, fail-closed, and M365 assumption text still matches implementation and story evidence.

## Follow-Up Risks

- The code still uses `IAssociationCorrectionDependencyReadiness.IsWorkflowRuntimeReady`, which is a readiness gate name rather than evidence of a hosted Dapr Workflow runtime. Future implementation should either bind the actual runtime or rename the readiness flag if the chosen runtime remains in-process.
- Future Epic 3 stories should reference the corrected architecture wording so they do not assume Dapr Workflow SDK/runtime behavior already exists.
