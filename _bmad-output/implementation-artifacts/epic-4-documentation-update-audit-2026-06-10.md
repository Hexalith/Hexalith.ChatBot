# Epic 4 Documentation Update Audit

**Project:** Chatbot
**Epic:** 4 - Governed AI Action Mediation
**Date:** 2026-06-10
**Mode:** Autonomous verification against sprint status, story files, planning docs, and implementation code

## Candidate Documentation List

The retrospective identified the following documentation that might need updates:

- `README.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- Configuration/runtime notes under `README.md` and `CommandGatewayServiceCollectionExtensions`

## Verification Results

### `README.md`

**Current-doc check:** README already describes Epic 4 task-intent capture/review, deterministic risk classification, low-risk routing, S3 approval, preview/inspection, M0 allowlisted execution, refusal cataloging, and corrected-context invalidation. Runtime notes explicitly say `DeterministicAiActionRiskClassifier` and `AiActionApprovalGate` are registered gateway stages, approved M0 execution is constrained by `ai-action-command-allowlist.m0` to `Project.AppendConversationMessage`, and the current conversation writer is metadata-only.

**Implementation evidence:**

- `CommandGatewayServiceCollectionExtensions` registers `IRiskClassifier` as `DeterministicAiActionRiskClassifier` and `IApprovalGate` as `AiActionApprovalGate`.
- It also registers `DefaultAiActionPolicyEvaluator`, `DisabledAiAssistanceProvider`, `ApprovedAiActionCommandAllowlist`, and `MetadataOnlyConversationWriter`.
- `ApprovedAiActionCommandAllowlist.CurrentVersion` resolves to `ai-action-command-allowlist.m0`; the M0 member set contains `Project.AppendConversationMessage`.
- `MetadataOnlyConversationWriter` returns a metadata-only append result and does not bind to a durable sibling conversation write.

**Decision:** No update required.

**Reason discarded:** Current README matches the implementation evidence.

### `_bmad-output/planning-artifacts/architecture.md`

**Current-doc check:** Architecture now says Epic 4 replaced original risk/approval stubs with `DeterministicAiActionRiskClassifier` and `AiActionApprovalGate`, documents the command-gateway order, clarifies the governed chat surface must use the command spine, and records that M0 approved execution prepares metadata-only append results rather than claiming a durable sibling `Hexalith.Conversations` binding.

**Implementation evidence:**

- `Program.cs` maps task-intent, approval, and AI-outcome projection endpoints.
- `CommandGatewayServiceCollectionExtensions` registers task-intent, approval, and AI-outcome projection handlers.
- `ApprovedAiActionOutcomeProjectionTranslator` and related tests cover approved execution, refusal, and correction invalidation projection paths.
- `MetadataOnlyConversationWriter` confirms the current binding level.

**Decision:** No update required.

**Reason discarded:** The architecture document already reflects the current implementation and avoids the stale stub/binding claims fixed by the prior audit.

### `_bmad-output/planning-artifacts/epics.md`

**Current-doc check:** Epic 4 still describes the intended governed AI action mediation loop, and Epic 5 still describes thin CLI/MCP adapters over the shared command pipeline.

**Implementation evidence:**

- Sprint status marks all Epic 4 stories done.
- Story records 4.1-4.9 show completed task intent, risk classification, low-risk execution, approval, preview, approved execution, refusal, and correction invalidation.
- Current code implements the command-spine and projection paths named by the epic.

**Decision:** No update required.

**Reason discarded:** This file remains a planning and requirement source that matches implemented Epic 4 behavior.

### `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`

**Current-doc check:** The PRD defines FR35-FR46, FR81a, NFR15/NFR16, NFR22, NFR40, NFR48, and FR91a. It does not claim that the current implementation has a durable sibling `Hexalith.Conversations` write binding.

**Implementation evidence:**

- Epic 4 code implements the relevant requirements through typed contracts, deterministic classification, approval gating, context-package checks, refusal cataloging, endpoint-projected lifecycle rows, and correction invalidation.
- The product requirement that M0 executes `Project.AppendConversationMessage` remains valid as a target even though the current binding is metadata-only.

**Decision:** No update required.

**Reason discarded:** The PRD is still accurate requirement source text; implementation binding details belong in README and architecture, which are current.

### `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`

**Current-doc check:** The addendum defines risk classifier rules, command allowlist v0/v1 governance, tenant policy schema, shared command pipeline, and idempotency keys. It names `Project.AppendConversationMessage` as the M0 allowlist target.

**Implementation evidence:**

- Contract/OpenAPI/client and `ApprovedAiActionCommandAllowlist` preserve `Project.AppendConversationMessage` and `ai-action-command-allowlist.m0`.
- The implementation's metadata-only writer is a current binding detail, not a change to the governed allowlist target.

**Decision:** No update required.

**Reason discarded:** Governance specification and implementation agree at the allowlist/contract level.

### `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`

**Current-doc check:** OpenAPI contains Epic 4 command schemas and constants including `CaptureTaskIntent`, `ProposeAIAction`, `MarkTaskIntentDisposition`, `ExecuteLowRiskAIAssistance`, `ExecuteApprovedAIAction`, `DecideAiActionApproval`, `Project.AppendConversationMessage`, and `ai-action-command-allowlist.m0`.

**Implementation evidence:**

- Matching command records exist under `src/Hexalith.ChatBot.Contracts/Commands`.
- Generated client output contains the same command records and enum constants.
- Story re-reviews recorded contract/OpenAPI/generated-client validation.

**Decision:** No manual update required.

**Reason discarded:** OpenAPI and generated client match the implemented public contract surface.

### Configuration and Runtime Documentation

**Current-doc check:** README documents the relevant DAPR component names and warns that Epic 4 approved execution is metadata-only at the conversation writer boundary. It does not claim a live durable `Hexalith.Conversations` write binding.

**Implementation evidence:**

- `Program.cs` maps projection endpoints using `ChatBot:Projection:PubSubName` and `ChatBot:Projection:Topic`, defaulting to `chatbot-pubsub` and `chatbot.events`.
- `CommandGatewayServiceCollectionExtensions` uses the DAPR sidecar endpoint and registers projection handlers.
- The runtime registrations match the README's component naming and Epic 4 notes.

**Decision:** No update required.

**Reason discarded:** Runtime/config documentation matches current code.

## Final Documentation Actions

- Created this audit document.
- Did not update README, architecture, epics, PRD, PRD addendum, OpenAPI, or configuration docs because verification found no current doc/code discrepancy.

