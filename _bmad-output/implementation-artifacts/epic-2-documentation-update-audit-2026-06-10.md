# Epic 2 Documentation Update Audit Rerun

**Project:** Chatbot  
**Epic:** 2 - Email Intake & Project Association  
**Mode:** Autonomous YOLO documentation verification after retrospective rerun  
**Retrospective:** `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-10.md`

## Verification Method

Candidate documentation updates were synthesized from Epic 2 story records, senior review findings, the prior Epic 2 retrospective, current sprint status, planning docs, README content, OpenAPI schema, and implementation code. Each candidate was checked against current code before deciding whether to edit or discard.

Implementation evidence checked:

- `README.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/DaprCorrectionPropagationCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprAssociationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/DaprOperationStatusStore.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/RequestFailedWorkflowRetry.cs`
- `Directory.Packages.props`

## Candidate Decisions

| Candidate doc | Proposed update | Verification result | Action |
|---|---|---|---|
| `README.md` | Ensure overview names Epic 2 capabilities and does not overclaim hosted Dapr Workflow runtime. | Current README names mailbox intake, participant resolution, deterministic association scoring, S2 review, decision/correction history, correction-propagation metadata, and retry/failure foundations. It explicitly says correction propagation runs through the server coordinator/activity seam and durable EventStore lifecycle events, not hosted Dapr Workflow runtime behavior. | Discarded. |
| `_bmad-output/planning-artifacts/architecture.md` | Reconcile correction propagation wording with implementation. | Current architecture says Epic 2 implements a DAPR-ready coordinator/activity seam with deterministic workflow identifiers and durable lifecycle events, while hosted Dapr Workflow runtime binding remains pending. Code has `DaprCorrectionPropagationCoordinator` but no `Dapr.Workflow` or `AddDaprWorkflow` runtime binding. | Discarded. |
| `_bmad-output/planning-artifacts/epics.md` | Clarify Story 2.8 ownership and production saga readiness. | Current Story 2.8 ownership note states it owns the contract, aggregate lifecycle, coordinator/activity seam, acknowledgements, visible states, and readiness; it separately assigns hosted Dapr Workflow production runtime binding and saga-readiness validation to later work. | Discarded. |
| `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` | Update Epic 2 API schema for association routing, correction lifecycle, retry/failure fields, and stable tokens. | Current OpenAPI contains `AssociationRoutingStatus`, lifecycle tokens `Correcting` and `Correction-delayed`, and generated-client references for association status. Code also contains `RequestFailedWorkflowRetry` and generated client coverage. | Discarded. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` | Lower product requirements to match current hosted workflow/runtime implementation. | Not appropriate. The PRD states product requirements and SLO/state expectations. The implementation-status distinction belongs in architecture and epic planning docs, which already carry it. | Discarded. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` | Update retry idempotency or threshold policy assumptions. | Addendum still matches implementation intent: retry idempotency is based on tenant, failed event, and retry actor; association threshold behavior and fail-closed semantics remain valid. | Discarded. |
| `Directory.Packages.props` or configuration docs | Add/update Dapr Workflow package documentation. | No code reference to `Dapr.Workflow` or hosted workflow registration exists. Current docs correctly avoid claiming hosted workflow runtime binding. No package/doc change is warranted. | Discarded. |

## Verified Code-Doc Alignment

- README and architecture both describe the correction-propagation implementation as a coordinator/activity seam with durable EventStore lifecycle events.
- Code confirms `DaprCorrectionPropagationCoordinator` is an in-process coordinator seam, not a hosted Dapr Workflow runtime binding.
- Code confirms `DaprAssociationProjectionStore` and `DaprOperationStatusStore` back user-visible association routing/progress and operation status through the DAPR ChatBot state store.
- Code and OpenAPI confirm lifecycle/status visibility for `Correcting` and `Correction-delayed`.
- Code confirms `RequestFailedWorkflowRetry` exists as the retry command and is exposed beyond server internals.

## Applied Updates

No documentation updates were applied. Every proposed update was discarded after verification because current docs already matched the implementation evidence.

## Follow-Up Risks

- Future docs should continue distinguishing coordinator/activity seams from hosted runtime bindings.
- Epic 3 stories should cite the current Epic 2 contracts and readiness semantics instead of re-describing association state from product prose alone.
- If hosted Dapr Workflow runtime binding is later implemented, README, architecture, and epics should be updated in the same change.
