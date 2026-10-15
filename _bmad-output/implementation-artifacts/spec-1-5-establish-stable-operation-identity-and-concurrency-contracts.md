---
title: 'Establish Stable Operation Identity and Concurrency Contracts'
type: 'feature'
created: '2026-10-14'
status: 'ready-for-dev'
baseline_commit: 5bb56567499c6df1c621184d3c3d35aa45521166
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-1-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Task IDs can alias operations; status writes permit identity drift and regressions. Legacy fixtures lack the canonical command lifecycle and revision contract.

**Approach:** Establish immutable identity, concurrency, replay, lifecycle and status contracts for command execution. Validate with synthetic evidence while live owner/persistence acceptance remains gated.

## Boundaries & Constraints

**Always:** Use caller-issued CommandId as the lifetime operation ULID for new canonical records, independently of TaskId. Preserve original origin, resources, creation UTC, correlation, class and equivalence version. Equivalence compares canonical command input and expected revision; origin is provenance, not semantic equivalence. Validate nonnegative expected revisions independently of replay. Use EventStore SDK seams, trusted authority, metadata-only catalog outcomes and OpenAPI authority.

**Never:** Rewrite historical stored identities, infer commit from SDK admission/projection, authorize mutation from a time-window hash, add another persistence path, or invent an owner-equivalent guard. A6/A13 and retry-profile approvals remain open; new canonical writes fail closed in live runtime. Tests cannot enable synthetic behavior for callers. Gateway admission/atomic owner commits and the RetryCommandExecution UI remain stories 1.7/1.8/1.10.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Behavior |
| --- | --- | --- |
| Replay | Same operation and semantics | Stored outcome and original metadata; no second effect |
| Conflict | Changed input, revision or occupied decision slot | Typed operation/revision/decision conflict; safe state/action only |
| Race | Competing writes at one revision | One winner; loser conflicts without mutation |
| Status | Reordered, duplicate or concurrent updates | Immutable identity and monotonic canonical revision/state |
| Legacy/denial | Historical receipt or unauthorized/missing ID | Preserve legacy identity; existence-neutral safe response |

</frozen-after-approval>

## Code Map

- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/CoarseCommandIdentityRecord.cs` and `CoarseIdempotencyComposer.cs` provide lifetime caller fences; preserve them.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` derives operation identity from TaskId; `Status/OperationStatusRecord.cs` lacks origin, canonical revision and transition facts.
- `src/Hexalith.ChatBot.Server/Gateway/Status/ReadModelOperationStatusStore.cs` uses SDK conditional writes but blindly replaces records; its in-memory counterpart does likewise.
- `src/Hexalith.ChatBot.Server/Queries/OperationStatusQueryHandler.cs` provides nonblocking authorized reads; retain it.

## Tasks & Acceptance

**Execution:**

- [ ] `src/Hexalith.ChatBot.Contracts/Identities/ChatBotOperationId.cs`, `Operations/CommandOperationIdentity.cs`, `Operations/CommandOperationConcurrency.cs` — add ULID identity, immutable metadata, optional decision-slot identity and validated expected revision; one type per file.
- [ ] `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, `Queries/OperationStatus.cs`, `Enums/CommandExecutionState.cs`, `Enums/CommandProjectionState.cs` — declare canonical state, origin, resources, expected/committed revisions and transition metadata; retain legacy wire compatibility. Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` through NSwag and refresh the client hash fixture.
- [ ] `src/Hexalith.ChatBot.Server/Operations/CommandOperationState.cs`, `CommandOperationTransition.cs`, `CommandOperationReducer.cs` — implement Received→Admitted→Committed/Rejected/Failed; retain terminal outcomes, enforce expected revision and semantic replay, capture actor/server-UTC/reason/correlation. ProjectionPending/Projected/ProjectionLagging are separate. Model retries as new predecessor-linked attempts, never reopening terminal state.
- [ ] `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`, `Status/OperationStatusRecord.cs`, `Status/OperationStatusHttpResults.cs` — integrate identity/status mapping without claiming an accepted receipt is committed. Preserve historical replay and safe-message filtering; canonical live writes remain unavailable without accepted owner/protection evidence.
- [ ] `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs`, `InMemoryOperationStatusStore.cs`, `ReadModelOperationStatusStore.cs` — apply the same policy within atomic updates: ignore stale revisions, allow equivalent duplicates, reject incompatible equal revisions and terminal rollback; preserve committed/identity fields and explicit legacy compatibility.
- [ ] `tests/Hexalith.ChatBot.Server.Tests/Operations/CommandOperationContractTests.cs`, `Gateway/Status/OperationStatusMonotonicityTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`, `tests/Hexalith.ChatBot.Client.Tests/CommandSubmissionTransportTests.cs` — cover the matrix, synthetic revision races/folded end-state, transition facts, shared TaskId, lifetime replay, complete status, legacy decoding and missing/forbidden parity. Retain durable fencing and trusted-authority regressions.

**Acceptance Criteria:**

- Given a new operation, when created/replayed, then original metadata stays stable and its operation ID cannot bind to different semantics.
- Given absent/malformed/stale concurrency input or competing semantics, when evaluated, then a typed safe rejection occurs before state changes and only the winning revision persists.
- Given a declared or illegal transition, when folded, then legal transitions capture actor/time/reason/correlation and illegal transitions preserve state; terminal retry creates a distinct linked attempt only for an eligible pre-effect failure.
- Given authorized status polling, when repeated or concurrent, then canonical state, safe reason, origin, attempts/ceiling, retry eligibility, partial metadata, prior outcome, terminal reason, correlation and safe actions remain coherent and retrieval does not wait for completion.
- Given missing A6/A13 acceptance, when live canonical execution is requested, then it fails closed; synthetic evidence does not establish native durability or owner execution.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

GetWorkflowOperationStatus uses the existing typed GetOperationStatus boundary. RetryCommandExecution is distinct from legacy RequestFailedWorkflowRetry; this story supplies its prerequisites. No migrations, deployments or external effects. Preserve historical OperationId values exactly.

## Verification

- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` — then execute Contracts, Client and Server individually through `.github/scripts/run-xunit-v4.sh`, retaining TRX. Run Architecture/Conformance if affected.
- Assert revisions, winning folded state, immutable fields and polling safety directly. Synthetic fixtures cannot prove native persisted end-state.
- Before proposing done, follow `docs/story-evidence-integrity.md` with exact scope, mappings and current-run provenance. An unavailable required native/primary lane leaves the story in review; never weaken the gate.
