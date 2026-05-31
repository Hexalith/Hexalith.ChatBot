# Epic 1 Documentation Update Audit

**Scope:** Post-retrospective documentation verification against Epic 1 implementation evidence.  
**Method:** Read candidate docs, compare against source code/tests/story records, update only verified discrepancies, and discard candidates where docs already matched code.

## Candidate List and Decisions

| Candidate document | Why checked | Implementation evidence | Decision |
|---|---|---|---|
| `README.md` | Root README had placeholder/typo content and no setup/runtime instructions. | `src/Hexalith.ChatBot.AppHost/Program.cs`, `src/Hexalith.ChatBot.Aspire/ChatBotAspireModule.cs`, story records documenting compiled xUnit runner usage. | Updated. |
| `_bmad-output/planning-artifacts/architecture.md` | Architecture still used shorthand DAPR topology language and the obsolete `chatbot-eventstore` store name. | `ChatBotAspireModule.ActorStateStoreComponentName == "statestore"`, `StateStoreComponentName == "chatbot-statestore"`, `PubSubComponentName == "chatbot-pubsub"`, AppHost local ACL selection. | Updated. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` | PRD M0/M1 scope bullets still placed `Skipped` in M1, while Epic 1 implemented it. FR81a also used a simplified pipeline order that missed the pre-commit and post-commit audit split. | `src/Hexalith.ChatBot.Contracts/Enums/LifecycleState.cs`, `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/LifecycleStateModelTests.cs`, `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` and Story 1.4/1.9 records. | Updated. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` | Shared Command Pipeline addendum still described audit as a single post-command stage. | Architecture D4 and implemented gateway flow split pre-commit audit gate from post-commit audit emission. | Updated. |
| `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` | API docs could have diverged on lifecycle states and surface-origin fields. | OpenAPI already includes `Skipped`, `Correcting`, `Correction-delayed`, `origin`, and `surfaceOrigin`; client generation tests cover synchronization. | No update needed. |
| `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml` and `accesscontrol.local.yaml` | Config docs could have missed local-vs-production security posture. | Both files already document production mTLS deny-by-default versus local mTLS-off default-allow behavior. | No update needed. |
| Story files under `_bmad-output/implementation-artifacts/1-*.md` | Story records could need correction after retrospective. | Current story statuses are `done`; known review/history contradictions are retained as historical record with superseding notes. | No update needed. |

## Verified Updates Applied

- Replaced README placeholder text with module purpose, root-only submodule initialization, restore/build commands, compiled xUnit runner guidance, and Aspire/DAPR runtime notes.
- Reconciled architecture topology names with implementation: `statestore`, `chatbot-statestore`, `chatbot-pubsub`, `accesscontrol.yaml`, and `accesscontrol.local.yaml`.
- Reconciled PRD M0/M1 lifecycle scope: `Skipped` is now M0; M1 keeps full transition-matrix expansion.
- Reconciled FR81a and the addendum pipeline order with the implemented two-phase audit model.

## Discarded Proposed Updates

- No OpenAPI update was made because the checked-in contract already matches the implemented lifecycle and surface-origin contract.
- No DAPR config-file update was made because the production and local ACL files already carry the required security distinction.
- No story-file rewrite was made because retained historical review notes are useful audit trail, and current status plus superseding sections already capture the final state.
