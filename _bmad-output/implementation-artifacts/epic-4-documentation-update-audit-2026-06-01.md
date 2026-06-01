# Epic 4 Documentation Update Audit

**Project:** Chatbot
**Epic:** 4 - Governed AI Action Mediation
**Date:** 2026-06-01
**Mode:** Autonomous verification against sprint status, story files, planning docs, and implementation code

## Candidate Documentation List

The retrospective identified the following documentation that might need updates:

- `README.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`

## Verification Results

### `README.md`

**Current-doc check:** The overview described Epics 1-3 but not Epic 4. Runtime notes covered Epic 2 correction propagation and Epic 3 S1 projection, but not the now-implemented AI mediation gateway stages or M0 approved-execution binding.

**Implementation evidence:**

- `CommandGatewayServiceCollectionExtensions` registers `DeterministicAiActionRiskClassifier`, `AiActionApprovalGate`, `DefaultAiActionPolicyEvaluator`, `DisabledAiAssistanceProvider`, `ApprovedAiActionCommandAllowlist`, and `MetadataOnlyConversationWriter`.
- Contracts and OpenAPI include `CaptureTaskIntent`, `MarkTaskIntentDisposition`, `ExecuteLowRiskAIAssistance`, `DecideAiActionApproval`, `ExecuteApprovedAIAction`, and `MarkAiActionProposalInvalidatedByCorrection`.
- Story 4.7 review fixed approved execution so the conversation writer prepares metadata-only append results before EventStore submission.

**Decision:** Update required and applied.

**Applied update:** Added Epic 4 overview and runtime note that approved M0 execution is constrained to `Project.AppendConversationMessage` through `ai-action-command-allowlist.m0`, with a metadata-only conversation writer until a durable sibling `Hexalith.Conversations` binding exists.

### `_bmad-output/planning-artifacts/architecture.md`

**Current-doc check:** The architecture still said M0 risk classification and approval gate were stubbed, and it described approved action execution as `Project.AppendConversationMessage` without clarifying the current metadata-only adapter behavior.

**Implementation evidence:**

- `CommandGatewayServiceCollectionExtensions` registers `DeterministicAiActionRiskClassifier` and `AiActionApprovalGate`, not only pass-through stubs.
- `ApprovedAiActionCommandAllowlist` exposes current version `ai-action-command-allowlist.m0` and allows only `Project.AppendConversationMessage`.
- `MetadataOnlyConversationWriter` returns metadata-only append results.
- `AcceptedCommandDispatcher` prepares approved AI action append metadata and submits the enriched payload through EventStore.

**Decision:** Update required and applied.

**Applied update:** Replaced stale stub wording, clarified Epic 4's concrete risk/approval stages, and documented the current metadata-only approved-execution binding.

### `_bmad-output/planning-artifacts/epics.md`

**Current-doc check:** Epic 4 describes the intended M0 AI mediation loop and Epic 5 describes cross-surface parity over the same command spine.

**Implementation evidence:**

- Epic 4 stories 4.1-4.9 are all marked `done` in sprint status.
- Implemented contracts, generated client, gateway stages, projection handlers, UI components, refusal catalog, and correction invalidation match the Epic 4 story intent.
- Epic 5 remains backlog and correctly depends on thin CLI/MCP adapters over the existing command spine.

**Decision:** No update required.

**Reason discarded:** The epic file is a requirement and planning artifact; it matches both the implemented Epic 4 behavior and the intended Epic 5 dependency direction.

### `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`

**Current-doc check:** The PRD defines FR35-FR46 task intent and AI mediation requirements, FR81a shared command pipeline, NFR15a fail-closed paths, NFR22 AI outage resilience, NFR40 message-catalog behavior, and NFR48 evidence freshness.

**Implementation evidence:**

- Epic 4 implements these requirements through typed contracts, deterministic risk classification, approval gate, low-risk policy evaluator, message catalog, preview, allowlist, and correction invalidation.
- No PRD text was found that incorrectly claims the current implementation has a completed production sibling-context conversation write binding.

**Decision:** No update required.

**Reason discarded:** The PRD remains product requirement source text. Implementation-specific clarification belongs in README and architecture, not in the PRD.

### `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`

**Current-doc check:** The addendum defines the risk classifier, M0/M1 allowlist, tenant policy schema, shared command pipeline, idempotency keys, and related governance rules. It states the M0 allowlist target is `Project.AppendConversationMessage`.

**Implementation evidence:**

- The OpenAPI/generated client and server command allowlist preserve `Project.AppendConversationMessage` and `ai-action-command-allowlist.m0`.
- The implementation's current metadata-only writer is a binding-level implementation detail, not a change to the allowlist target.

**Decision:** No update required.

**Reason discarded:** The addendum remains the intended governance specification. Architecture now records the current implementation binding nuance.

### `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`

**Current-doc check:** OpenAPI contains the Epic 4 command schemas and wire constants:

- `CaptureTaskIntent`
- `MarkTaskIntentDisposition`
- `ExecuteLowRiskAIAssistance`
- `ExecuteApprovedAIAction`
- `DecideAiActionApproval`
- `Project.AppendConversationMessage`
- `ai-action-command-allowlist.m0`
- low-risk assistance schema versions

**Implementation evidence:**

- Matching contract records exist under `src/Hexalith.ChatBot.Contracts/Commands` and `src/Hexalith.ChatBot.Contracts/Queries`.
- Generated client files include the same command and enum constants.
- Story validation repeatedly ran OpenAPI contract spine and generated-client hash tests.

**Decision:** No manual update required.

**Reason discarded:** The OpenAPI file already matches the implemented public contract surface.

## Final Documentation Actions

- Updated `README.md`.
- Updated `_bmad-output/planning-artifacts/architecture.md`.
- Did not update `epics.md`, PRD, PRD addendum, or OpenAPI because verification found no doc/code discrepancy requiring a manual change.

