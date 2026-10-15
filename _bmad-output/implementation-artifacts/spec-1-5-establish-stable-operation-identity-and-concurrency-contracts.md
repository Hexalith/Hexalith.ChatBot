---
title: 'Establish Stable Operation Identity and Concurrency Contracts'
type: 'feature'
created: '2026-10-14'
status: 'in-review'
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

- [x] `src/Hexalith.ChatBot.Contracts/Identities/ChatBotOperationId.cs`, `Operations/CommandOperationIdentity.cs`, `Operations/CommandOperationConcurrency.cs` — add ULID identity, immutable metadata, optional decision-slot identity and validated expected revision; one type per file.
- [x] `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, `Queries/OperationStatus.cs`, `Enums/CommandExecutionState.cs`, `Enums/CommandProjectionState.cs` — declare canonical state, origin, resources, expected/committed revisions and transition metadata; retain legacy wire compatibility. Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` through NSwag and refresh the client hash fixture.
- [x] `src/Hexalith.ChatBot.Server/Operations/CommandOperationState.cs`, `CommandOperationTransition.cs`, `CommandOperationReducer.cs` — implement Received→Admitted→Committed/Rejected/Failed; retain terminal outcomes, enforce expected revision and semantic replay, capture actor/server-UTC/reason/correlation. ProjectionPending/Projected/ProjectionLagging are separate. Model retries as new predecessor-linked attempts, never reopening terminal state.
- [x] `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`, `Status/OperationStatusRecord.cs`, `Status/OperationStatusHttpResults.cs` — integrate identity/status mapping without claiming an accepted receipt is committed. Preserve historical replay and safe-message filtering; canonical live writes remain unavailable without accepted owner/protection evidence.
- [x] `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs`, `InMemoryOperationStatusStore.cs`, `ReadModelOperationStatusStore.cs` — apply the same policy within atomic updates: ignore stale revisions, allow equivalent duplicates, reject incompatible equal revisions and terminal rollback; preserve committed/identity fields and explicit legacy compatibility.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Operations/CommandOperationContractTests.cs`, `Gateway/Status/OperationStatusMonotonicityTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`, `tests/Hexalith.ChatBot.Client.Tests/CommandSubmissionTransportTests.cs` — cover the matrix, synthetic revision races/folded end-state, transition facts, shared TaskId, lifetime replay, complete status, legacy decoding and missing/forbidden parity. Retain durable fencing and trusted-authority regressions.

**Acceptance Criteria:**

- Given a new operation, when created/replayed, then original metadata stays stable and its operation ID cannot bind to different semantics.
- Given absent/malformed/stale concurrency input or competing semantics, when evaluated, then a typed safe rejection occurs before state changes and only the winning revision persists.
- Given a declared or illegal transition, when folded, then legal transitions capture actor/time/reason/correlation and illegal transitions preserve state; terminal retry creates a distinct linked attempt only for an eligible pre-effect failure.
- Given authorized status polling, when repeated or concurrent, then canonical state, safe reason, origin, attempts/ceiling, retry eligibility, partial metadata, prior outcome, terminal reason, correlation and safe actions remain coherent and retrieval does not wait for completion.
- Given missing A6/A13 acceptance, when live canonical execution is requested, then it fails closed; synthetic evidence does not establish native durability or owner execution.

## Implementation Notes

- Caller CommandId is the operation identity for new receipts. Stored receipts retain their exact historical OperationId, including the TaskId fallback when an old receipt omitted OperationId. Existing historical status collisions reject before legacy dispatch; this read is not an atomic owner reservation.
- Canonical requests explicitly carry `concurrency`; missing, null, noninteger, negative, duplicate or unknown guard members reject during parsing. Omitting the entire object retains legacy request compatibility. After trusted authorization, every canonical request fails closed before reservation/dispatch while A6/A13 remain open.
- The unregistered pure reducer folds immutable transition events, validates semantic replay and revision guards, retains terminal execution outcomes independently of projection, and creates distinct predecessor-linked retry attempts with safe prior-outcome metadata. It supplies synthetic evidence only and introduces no persistence path or runtime approval switch.
- Live legacy receipts keep canonical metadata absent; equivalent duplicate interleaving and initial status-write recovery preserve receipt identity without relying on unavailable durable metadata. Canonical metadata comes from the pure canonical fold only.
- Both status stores evaluate the same policy inside atomic replacement/SDK ETag retry, protect identity and terminal outcomes, reject incompatible equal revisions, ignore stale canonical revisions, and defensively copy mutable values. Legacy updates retain explicit timestamp-based compatibility; records are not implicitly upgraded.
- OpenAPI remains authoritative; NSwag regenerated the client and the hash fixture. Additive metadata is optional for legacy decoding. Public metadata files use the Contracts root namespace to retain the existing architecture guard against exported governance namespaces.

## Spec Change Log

- 2026-10-15: Implemented approved scope without changing frozen intent. Aligned new receipt references in audit/correction paths and current-runtime fixtures; preserved historical replay fixtures. The parent matrix audit strengthened cross-tenant parity to first prove the operation exists for an authorized caller.

## Review Triage Log

| ID | Verdict | Route | Evidence and required correction |
| --- | --- | --- | --- |
| B1 | medium | patch | Identity constructor parameters do not bind to OperationId/ResourceIds during System.Text.Json deserialization, blocking reuse of the public metadata contract. Correct the existing serializer binding and cover its round trip. |
| B2 | medium | patch | Concurrency constructor expectedRevision is nullable while its property is not; System.Text.Json rejects binding. Preserve validation while making the existing contract deserialize. |
| B3 | medium | patch | Initial canonical snapshots accept an unrelated committed revision; require the same expected-plus-one invariant as the reducer inside both stores. |
| B4 | medium | patch | Canonical execution and legacy lifecycle can contradict each other in accepted snapshots; validate the mapping used by FromCanonical. |
| B5 | medium | patch | Received-to-Admitted status updates can erase the retry predecessor outcome before terminal preservation applies; retain that immutable outcome across the attempt. |
| B6 | medium | patch | Canonical RetryCount is zero-based, but MaxAttempts is one-based; the current HTTP comparison permits a retry at the ceiling. |
| B7 | medium | patch | Canonical resource and transition actor strings accept values excluded by OpenAPI token bounds; validate these existing metadata fields before storing/exposing them. |
| B8 | medium | patch | The retry exhaustion helper uses the first operation timestamp, causing retry transitions to reject silently. Assert transition success and use monotonic attempt timestamps. |
| B9 | medium | patch | The in-memory race test invokes two synchronously completing writes sequentially. Synchronize actual competing workers before asserting the winner. |
| B10 | high | patch | Legacy receipt recovery lacks the original optional metadata added only in the final gateway status write. Keep canonical metadata absent on legacy receipts instead of introducing a new durable contract for the disabled canonical path. |
| E1 | high | patch | Replay can initialize a legacy status before the original final upsert; only the original carries new canonical metadata, triggering immutable-field conflict after dispatch. Same root cause as B10; legacy creation must remain metadata-compatible. |
| E2 | medium | patch | Predecessor ULIDs validate through a normalizing parser but retain their unnormalized spelling, so lowercase eligible retries fail ordinal comparison. |
| E3 | medium | patch | Same demonstrated expected/committed revision divergence as B3; both stores invoke the shared policy. |
| E4 | medium | patch | Same demonstrated canonical attempt-ceiling error as B6; legacy retry-count behavior must remain compatible. |
| V1 | medium | patch | Verified regression gap: correction dispatcher OperationId changes, while the real status sink lookup is not exercised through an accepted receipt. Extend the existing correction API test through that sink. |

All findings are caused or exposed by this change and have direct corrections without new public surface. Grouped patches: serializer binding (B1/B2); owner revision validation (B3/E3); canonical lifecycle compatibility (B4); predecessor outcome preservation (B5); retry ceiling (B6/E4); metadata bounds (B7); retry exhaustion evidence (B8); actual in-memory race (B9); legacy status metadata compatibility (B10/E1); predecessor normalization (E2); correction status bridge coverage (V1). No intent changes or deferred findings. All 15 findings were corrected and verified by the focused 63-test run and final full verification. Serializer helpers remain internal/private. The correction API regression now reports through the real status sink and polls by accepted OperationId.

## Design Notes

GetWorkflowOperationStatus uses the existing typed GetOperationStatus boundary. RetryCommandExecution is distinct from legacy RequestFailedWorkflowRetry; this story supplies its prerequisites. No migrations, deployments or external effects. Preserve historical OperationId values exactly.

## Verification

- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` — then execute Contracts, Client and Server individually through `.github/scripts/run-xunit-v4.sh`, retaining TRX. Run Architecture/Conformance if affected.
- Assert revisions, winning folded state, immutable fields and polling safety directly. Synthetic fixtures cannot prove native persisted end-state.
- Before proposing done, follow `docs/story-evidence-integrity.md` with exact scope, mappings and current-run provenance. An unavailable required native/primary lane leaves the story in review; never weaken the gate.

### Current-run validation (2026-10-15)

- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed with zero warnings/errors. Focused Server and Conformance project builds with the same configuration also passed.
- Each lane ran with `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5/<lane>.ctrf.json -result-trx TestResults/story-1-5/<lane>.trx`: Contracts 520, Client 94, Server 2687, Architecture 123, Conformance 151; zero skips. Results are local current-run evidence, not completion provenance.
- `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` stayed pending and was canceled. `aspire describe --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` reported no running host; `aspire ps --format Json` returned `[]`. Native persisted end-state and owner atomicity are not established. A6/A13 and retry-profile acceptance remain open.
- No done transition is proposed. Completion must pass the exact scope, mappings and provenance gate documented in `docs/story-evidence-integrity.md`; synthetic CAS/fold evidence does not replace any required native/primary lane.

### Matrix evidence

| Matrix row | Passing direct assertion coverage |
| --- | --- |
| Replay | `CommandOperationContractTests.LifetimeReplayShouldRetainOriginalOriginResourcesCreationCorrelationAndOutcome`; existing durable recovery/fencing regressions |
| Conflict | `CommandOperationContractTests.ChangedInputRevisionOrDecisionSlotShouldProduceTypedConflictsWithoutMutation`; `CommandOperationConcurrencyAdapterTests.MissingMalformedOrNegativeCanonicalGuardShouldRejectBeforeAdmission` |
| Race | `CommandOperationContractTests.SyntheticCompetingTransitionsShouldHaveOneWinnerAndOneUnchangedRevisionConflict`; `OperationStatusMonotonicityTests.SyntheticRevisionRaceShouldPersistExactlyOneWinningStatus` (in-memory and SDK fake) |
| Status | `OperationStatusMonotonicityTests.ReorderedAndEquivalentDuplicateUpdatesShouldPreserveCanonicalFoldAndOriginalMetadata`; `HigherRevisionShouldPreserveTerminalOutcomeAndCommittedFields`; `CanonicalStatusShouldExposeCompleteSafeFactsAndKeepProjectionPendingSeparateFromCommit` |
| Legacy/denial | `CommandSubmissionTransportTests.GetOperationStatusShouldAcceptNamedEnumWireValuesInsideEnumCollections`; `ServerBootstrapApiTests.OperationStatusEndpointShouldCollapseCrossTenantAndUnknownOperations`; `CommandGatewayTests.NewCommandIdShouldNotAliasHistoricalTaskBasedOperationBeforeDispatch` |

Acceptance also runs `DeclaredEventsShouldFoldWithActorUtcReasonCorrelationAndSeparateProjection`, `IllegalTransitionShouldPreserveFold`, `TerminalAttemptShouldRemainClosedAndRetryShouldBeDistinctAndEligibleOnlyBeforeEffects`, `SeparateCommandsSharingTaskIdShouldHaveIndependentLifetimeOperationStatus`, and `CanonicalLiveRequestShouldFailClosedBeforeReservationOrDispatchDespiteSyntheticEvidence`. Status reads retain the nonblocking query handler.

### Review-fix validation

- Final solution Debug build passed with zero warnings/errors; all five lanes passed with zero skips: Contracts 520, Client 94, Server 2687, Architecture 123, Conformance 151 (3575 total). Logs: `/tmp/story-1-5-final-build.log` and `/tmp/story-1-5-final-<lane>-tests.log`; retained TRX/CTRF: `TestResults/story-1-5/`.
- Focused review-fix evidence is retained at `TestResults/story-1-5-review-fixes/`: 55 contract/status cases and 8 gateway/correction cases, zero failures/skips.
- Review regression methods include `ImmutableContractsShouldRoundTripWithoutDefaultingMissingOrNegativeRevision`, `EligibleRetryShouldNormalizeLowercasePredecessorIdentity`, `CanonicalSnapshotsShouldRejectUnrelatedCommitRevisionOverflowAndContradictoryLifecycle`, `PredecessorOutcomeShouldRemainImmutableFromReceivedThroughTerminalAttempt`, `DueCanonicalRetryShouldRespectOneBasedAttemptCeilingWhileLegacyCountRemainsCompatible`, `CanonicalStatusShouldRejectUnboundedOrMalformedMetadataBeforePersistingOrExposingIt`, `SyntheticRevisionRaceShouldPersistExactlyOneWinningStatus`, and the real correction API status-sink/poll assertion.
- Whitespace validation: `git -c core.whitespace=cr-at-eol diff --check` passed. The frozen intent remains byte-identical to the approved HEAD spec. No dependency, submodule, branch, deployment or remote changes were made. The build workflow records the verified implementation in a local Conventional Commit while the story remains in review.

## File List

- `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/CommandExecutionState.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/CommandProjectionState.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/ChatBotOperationId.cs`
- `src/Hexalith.ChatBot.Contracts/Operations/CommandOperationConcurrency.cs`
- `src/Hexalith.ChatBot.Contracts/Operations/CommandOperationIdentity.cs`
- `src/Hexalith.ChatBot.Contracts/Operations/CommandOperationTransitionMetadata.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/OperationPriorOutcome.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs`
- `src/Hexalith.ChatBot.Contracts/Serialization/CommandOperationConcurrencyJsonConverter.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandSubmissionContractAdapter.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/DaprCoarseIdempotencyStore.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Stages/AcceptedCommandDispatcher.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/InMemoryOperationStatusStore.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusKey.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdateConflictException.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/ReadModelOperationStatusStore.cs`
- `src/Hexalith.ChatBot.Server/Operations/CommandOperationConflict.cs`
- `src/Hexalith.ChatBot.Server/Operations/CommandOperationMetadataValidation.cs`
- `src/Hexalith.ChatBot.Server/Operations/CommandOperationPriorOutcome.cs`
- `src/Hexalith.ChatBot.Server/Operations/CommandOperationReducer.cs`
- `src/Hexalith.ChatBot.Server/Operations/CommandOperationResult.cs`
- `src/Hexalith.ChatBot.Server/Operations/CommandOperationState.cs`
- `src/Hexalith.ChatBot.Server/Operations/CommandOperationTransition.cs`
- `tests/Hexalith.ChatBot.Client.Tests/CommandSubmissionTransportTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CommandSubmissionRequestContractDriftTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/SuccessIntentParityTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.AuthorityExpiry.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.DurableRecovery.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.OperationContracts.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandOperationConcurrencyAdapterTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/DelayedInitialOperationStatusStore.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/AtomicOperationStatusReadModelStore.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/OperationStatusMonotonicityTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/CorrectionPropagationProgressReadTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Operations/CommandOperationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
- `tests/fixtures/story-1-2-contract-spine-oracle.json`
