# Epic 1 Documentation Update Audit - 2026-06-09

**Scope:** Documentation that might need updates based on Epic 1 implementation learnings.  
**Method:** Read the current doc content, compare against implementation code/contracts, and update only verified discrepancies.  
**Result:** No documentation updates were required. All proposed updates were discarded because the current docs match the implementation evidence checked below.

## Candidate Docs and Decisions

| Candidate doc | Why it was considered | Implementation evidence checked | Decision |
| --- | --- | --- | --- |
| `README.md` | Runtime setup and validation instructions can drift from AppHost and test-runner reality. | `src/Hexalith.ChatBot.AppHost/Program.cs`, `src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs`, `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml`, `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml`, `src/Hexalith.ChatBot.Client/IChatBotClient.cs`. | Discarded. README already documents compiled xUnit validation, root-only submodule initialization, `statestore`, `chatbot-statestore`, `chatbot-pubsub`, local `accesscontrol.local.yaml`, production `accesscontrol.yaml`, and thin client adapters. |
| `_bmad-output/planning-artifacts/architecture.md` | Architecture decisions changed during implementation around DAPR topology, local ACLs, gateway placement, and lifecycle scope. | `ChatBotAspireModule` constants, AppHost local ACL loading, OpenAPI lifecycle enum, `IChatBotClient.SubmitAsync(IChatBotCommand, ..., ChatBotSurfaceOrigin)`, and server-internal gateway stage names found in code. | Discarded. Architecture already reflects the current topology, local/production ACL split, `Skipped`, and the CommandGateway as an admission layer rather than a second pipeline. |
| `_bmad-output/planning-artifacts/epics.md` | Epic 1 story language can become stale after implementation discoveries. | Epic 1 story files, sprint status, `ChatBotAspireModule`, OpenAPI `LifecycleState`, and `RecordGovernedNote`. | Discarded. Epic 1 story text already names the implemented DAPR components, local/production ACL split, `IChatBotCommand`, `RecordGovernedNote` walking skeleton, and `Skipped` lifecycle state. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` | PRD scope previously risked treating `Skipped` as later scope. | `src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs`, generated client enum, and OpenAPI `LifecycleState`. | Discarded. PRD now states `Skipped` is part of the M0 command-spine contract and describes terminal skip semantics. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` | API/configuration details might need adjustment for command pipeline, idempotency, or surface origins. | OpenAPI command submission schema, `IChatBotClient`, `ChatBotSurfaceOrigin`, and `RecordGovernedNote`. | Discarded. No Epic 1 discrepancy found; addendum command-pipeline/idempotency content remains consistent with code. |
| `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` | API documentation can diverge from contracts/enums after implementation. | `LifecycleState.cs`, `ChatBotSurfaceOrigin.cs`, generated client enum, and command submission schema. | Discarded. OpenAPI includes `Skipped`, `Correcting`, `Correction-delayed`, and `SurfaceOrigin` values with boundary-origin description. |
| `docs/adrs/*.md` | Architecture decision records may need updates if Epic 1 altered core decisions. | ADR index candidates plus architecture decision sections for gateway, audit, DAPR, and host transition. | Discarded. No Epic 1-specific ADR discrepancy was verified. Existing ADRs focus later M2 audit/recovery topics or already align with current architecture. |

## Verified Implementation Facts

- `ChatBotAspireModule.ActorStateStoreComponentName` is `statestore`.
- `ChatBotAspireModule.StateStoreComponentName` is `chatbot-statestore`.
- `ChatBotAspireModule.PubSubComponentName` is `chatbot-pubsub`.
- AppHost resolves and loads `accesscontrol.local.yaml` for the local self-hosted topology.
- `accesscontrol.yaml` is deny-by-default and scoped to production mTLS/Sentry posture.
- `LifecycleState` includes `Skipped`, `Correcting`, and `Correction-delayed` in both contract code and OpenAPI.
- `ChatBotSurfaceOrigin` and OpenAPI `SurfaceOrigin` include `api`, `ui`, `cli`, `mcp`, `worker`, `mailbox`, and `ai`.
- `IChatBotClient.SubmitAsync(...)` accepts `IChatBotCommand` plus boundary surface origin.
- `RecordGovernedNote` remains the trivial metadata-only governed command for the Epic 1 walking skeleton.

## Updates Applied

None.

## Discarded Proposed Updates

- Do not add new DAPR topology notes: already present in README, architecture, epics, and AppHost comments.
- Do not change PRD lifecycle scope: already reconciled to include `Skipped` in M0.
- Do not regenerate or edit OpenAPI for Epic 1: current schema already matches contract code for checked Epic 1 surfaces.
- Do not edit ADRs for Epic 1: no verified ADR/code discrepancy was found.
