---
title: 'Establish Stable Operation Identity and Concurrency Contracts'
type: 'feature'
created: '2026-10-14'
status: 'in-progress'
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

### Review Findings

Code review 2026-10-17 of commit `839aee3` (`4fdb63d..839aee3`). Four layers ran: Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor. Line numbers are for `839aee3`.

Outcome: 6 decisions resolved (5 became patches, 1 was deferred), 15 patches left as action items, 5 deferred, 20 rejected. Status moved from `review` to `in-progress` until the patches land. HEAD has since changed `CommandGateway.cs`, `ChatBotCommandAdmissionPipeline.cs` and three test files, so apply the patches against current HEAD. D1, D5 and D6 change OpenAPI and require NSwag regeneration and a refreshed client hash fixture.

- [ ] [Review][Patch] (Decided 2026-10-17: (a) non-retryable catalog code plus an OpenAPI note on `/api/v1/commands`) Canonical-request refusal tells clients to retry indefinitely — When `Concurrency` is not null, the request maps to the internal reason `canonical_execution_unavailable`, which `CreateDependencyUnavailable` turns into a 503 `dependency_degraded` with `retryable=true` and `clientAction=RetryLater`. The path stays closed by policy until A6/A13 are accepted, so a compliant client retries forever. The refusal is not documented on `/api/v1/commands`. Options: (a) keep fail-closed but add a non-retryable catalog code plus an OpenAPI response note; (b) keep it as transient unavailability and document that; (c) use a validation-style refusal. Medium. [src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs:184; src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs:360] (blind-hunter)
- [ ] [Review][Patch] (Decided 2026-10-17: (a) legacy field merge, so receipt-proven `committed` is never superseded by a replay-inferred `reconciling`; keep the max `DuplicateAttemptCount` and the `PriorOutcome`) Legacy status timestamp ordering can drop the original receipt's committed audit status — The legacy branch keeps whichever write has the later `LastUpdatedAt`. A duplicate that arrives during the original's final status write reads no status, so it initializes `Accepted(priorOutcome, auditReconciliationRequired: true, now)`. The original write then carries the older stamp, so it is discarded, whether or not it arrives first. `auditStatus` stays `reconciling` permanently because no writer ever sets it back to committed. Before this change the bad outcome needed the replay to land last; now it happens whenever the replay has the later stamp. The race test uses a constant clock and does not assert `AuditStatus`. Options: (a) a legacy field merge where receipt-proven `committed` is never superseded by a replay-inferred `reconciling`; (b) restore arrival-order last-writer-wins; (c) stop replay from initializing an absent status, which reopens the E1/B10 recovery path. Medium. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:35; src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs:355] (blind-hunter+verification-gap+edge-case-hunter)
- [ ] [Review][Patch] (Decided 2026-10-17: (a) move the read-only collision lookup into the admission pipeline before reservation and update the stage-order tests) Historical-collision 409 is returned after state has already changed — The new lookup in `CommandGateway.SubmitAsync` runs only after `AdmitAsync` returned Accepted. By then the pipeline has reserved coarse idempotency and durably written the `gate_passed` pre-commit audit envelope. The 409 then calls an abort whose failures are swallowed, and the 409 itself is not audited. AC2 requires rejection before state changes, while the Implementation Notes say only "before legacy dispatch". Options: (a) move the lookup into the pipeline before reservation and update the stage-order tests; (b) keep the placement and emit a compensating rejection audit; (c) accept it, since it needs an old TaskId reused as a new CommandId, and document that. Medium. [src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs:136-147] (acceptance-auditor)
- [x] [Review][Defer] (Decided 2026-10-17: (b) — deferred: successor uniqueness belongs to the owner aggregate (Stories 1.8/1.10); the pure reducer limit is documented in deferred-work) One failed attempt can be retried more than once — `Retry` records nothing on the predecessor, so distinct new ULIDs can each retry the same failed attempt and each gets `Attempt = previous.Attempt + 1`. `MaxAttempts` therefore limits chain depth, not total attempts. This conflicts with AC3 ("a distinct linked attempt") and the Race matrix row. Options: (a) a successor claim on the predecessor, such as the predecessor id used as a decision slot, so a second retry conflicts; (b) leave enforcement to the owner aggregate in stories 1.8/1.10 and document the reducer limit; (c) an attempt registry. Medium. [src/Hexalith.ChatBot.Server/Operations/CommandOperationReducer.cs:123] (blind-hunter+acceptance-auditor)
- [ ] [Review][Patch] (Decided 2026-10-17: (b) declare the legacy completion/audit fields compatibility-only for canonical records; derive canonical `retryEligible` from the fold plus an approved retry profile, which stays closed; add tests) Canonical-to-status mapping is incoherent outside the committed path — `FromCanonical` has these gaps:
  - Received and Admitted map to `completionStatus: accepted-projection-pending`.
  - Rejected and Failed show `auditStatus: reconciling` and `projectionState: ProjectionPending` forever.
  - The Received fact carries the reason `command_accepted`.
  - `NextRetryAt` and `PartialOutputCodes` are never set, so a retry-eligible fold always serializes `retryEligible: false` and `hasPartialOutputs: false`.
  - `ValidateCanonical` does not tie `ReasonCode`, `SafeNextActions` or `NextRetryAt` to the fold.
  - The tests reach `true` only by hand-patching the record.

  The legacy enums offer only accepted-projection-pending/completed/failed and committed/reconciling. Options: (a) extend the public completion and audit enums; (b) declare the legacy fields compatibility-only for canonical records, so consumers read `executionState`/`projectionState`, and derive `retryEligible` from the fold plus an approved retry profile (still closed); (c) defer to stories 1.8/1.10 with retry-profile approval. Medium. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs:71-108; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:66] (blind-hunter+acceptance-auditor)
- [ ] [Review][Patch] (Decided 2026-10-17: (a) drop `actorId` from the public `CommandOperationTransition` and keep it server-side; regenerate the client and refresh the hash fixture) `lastTransition.actorId` is published on operation status without a redaction marker — The actor token allows `@`, and the tests use `actor@example.test`, so actor identities that may be PII reach any authorized reader of the tenant's status. The existing `TaskIntentTransitionAuditSummary` exposes `actorId` with a `redactionState`, but the new schema has none. This is not live yet because canonical records are synthetic, but the contract is published. Options: (a) drop `actorId` from the public transition and keep it server-side; (b) add `redactionState` and redact non-service actors; (c) keep it as-is for tenant-scoped readers. Medium. [src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml:637; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:125] (acceptance-auditor)
- [ ] [Review][Patch] CLI, MCP and UI still report TaskId as the operation id. Use `response.OperationId`, with the historical fallback only when it is empty. Update `ChatBotCliCommandTests.cs:266`, which pins `"op-1"`, and the UI tests. Add CLI and MCP assertions with OperationId ≠ TaskId. High. [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:27; src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:44; src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs:31; src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs:35] (all four layers)
- [ ] [Review][Patch] `Receive` accepts an identity that already carries `PredecessorOperationId`. That creates a linked attempt with `Attempt = 1` and no `PriorOutcome`, bypassing `Retry` eligibility. `ValidateCanonical` also accepts a predecessor without `PriorOutcome`. Reject these outside `Retry` and require `PriorOutcome` exactly when a predecessor is set. Medium. [src/Hexalith.ChatBot.Server/Operations/CommandOperationReducer.cs:51; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:128] (blind-hunter+acceptance-auditor)
- [ ] [Review][Patch] The legacy branch of `OperationStatusUpdatePolicy` has no tests, so replacing line 35 with `return incoming;` passes every test. Add older-after-newer and identity-drift rows for both stores, and an advancing-clock duplicate-during-initial-write variant that asserts `AuditStatus` (its outcome depends on the timestamp-ordering decision). Medium. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:22-35] (verification-gap)
- [ ] [Review][Patch] The failure path of the pre-dispatch status lookup is untested. Add tests where `TryGetAsync` throws or is cancelled, asserting 503, zero dispatches, and a released reservation that allows a later retry. Medium. [src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs:136-158] (verification-gap)
- [ ] [Review][Patch] The canonical-refusal path is untested for precedence and over HTTP. Cover a denied caller sending `concurrency`, asserting the authorization denial, audit and counter. Cover an HTTP POST with a `concurrency` body, asserting 503 with no reservation, audit or dispatch. Medium. [src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs:184-189] (verification-gap+blind-hunter)
- [ ] [Review][Patch] Status-store metadata validation is incomplete:
  - Legacy records (`CanonicalRevision` null) may carry `Origin`, `ResourceIds`, `CreatedAt`, `EquivalenceVersion` or `PredecessorOperationId`, and `ResourceIds` is then exposed without validation.
  - Canonical `PredecessorOperationId` is not validated.
  - `OperationId` and `CorrelationId` are parse-checked but not checked for normalized equality.

  Low. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:106-112; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:128-144; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:32-34] (acceptance-auditor+edge-case-hunter)
- [ ] [Review][Patch] `PrepareDispatchAsync` now accepts `OperationId == TaskId` for a brand-new Reserved preparation. Restore strict equality with `CommandId` there and keep the historical tolerance in the reconcile paths only. Low. [src/Hexalith.ChatBot.Server/Gateway/Idempotency/DaprCoarseIdempotencyStore.cs:257] (acceptance-auditor)
- [ ] [Review][Patch] The concurrency guard's token bounds are untested and not tied to OpenAPI. Add adapter rows for a slot with a space, a `/`, 241 characters, a number and null. Add a drift assertion against the YAML `pattern` and `maxLength`. Low. [src/Hexalith.ChatBot.Server/Gateway/CommandSubmissionContractAdapter.cs:148-162] (verification-gap+blind-hunter)
- [ ] [Review][Patch] `CommandOperationConcurrencyJsonConverter.Write` throws `ArgumentNullException` for a null value, which STJ passes when `HandleNull` is true. Write a JSON null instead. Low. [src/Hexalith.ChatBot.Contracts/Serialization/CommandOperationConcurrencyJsonConverter.cs:50] (blind-hunter+edge-case-hunter)
- [ ] [Review][Patch] `CommandOperationReducer.Project` lacks the `ArgumentNullException.ThrowIfNull(current)` guard that the other entry points have. Low. [src/Hexalith.ChatBot.Server/Operations/CommandOperationReducer.cs:75] (blind-hunter+edge-case-hunter)
- [x] [Review][Defer] OpenAPI `oneOf: [TaskId, CommandId]` over identical schemas can never satisfy strict `oneOf` validation. It remains on the operation path parameters, `originalOperationId` and `OperationAuditHistory.operationId`; this change fixed only `OperationStatus.operationId`. [src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml:97] — deferred: pre-existing contract ambiguity.
- [x] [Review][Defer] `OperationStatusWorkflowStatusSink` builds its write from an earlier `TryGetAsync`, so concurrent replay or workflow fields can be lost. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusWorkflowStatusSink.cs:25-69] — deferred: pre-existing; the old store's blind replace lost them too.
- [x] [Review][Defer] The reducer and the store require `CommittedRevision == ExpectedRevision + 1`, which assumes single-event owner commits. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:125-127] — deferred: maybe-false, unverified medium. Settle by confirming EventStore owner revision semantics for multi-event commands when Story 1.8 wires the owner commit.
- [x] [Review][Defer] The synthetic SDK store keeps the object reference and never JSON round-trips, so equal-revision equivalence (compared by `JsonSerializer.Serialize`) is unproven against a store-deserialized `current`. [tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/AtomicOperationStatusReadModelStore.cs:53] — deferred: maybe-false, unverified medium. Settle with a round-trip through the DAPR read-model serializer before canonical writes go live.

#### Rejected

- False: "the pre-dispatch status read wrongly adds a dependency." The read is spec-mandated ("historical status collisions reject before legacy dispatch"). The fail-closed 503 matches the gateway's posture, and coarse-idempotency conflicts are not audited either.
- False: "a stored record with null `SafeNextActions` makes `Snapshot` throw." The field has been a positional member since the record was introduced (99007f6), and every writer sets it.
- False: "equivalent canonical duplicates with different offsets conflict." Canonical records are UTC by construction: the identity normalizes `CreatedAt` and validation requires zero offsets. The serialization cost is negligible at status-write rates.
- False: "Received→Rejected cannot be folded." The spec declares Received→Admitted→Committed/Rejected/Failed only.
- False: "a terminal occupant permanently blocks a decision slot." The reducer does not decide occupancy; the caller supplies `occupiedDecision`.
- False: "legacy statuses wrongly emit `hasPartialOutputs`." It is a derived flag reflecting `PartialOutputCodes`. The "canonical metadata absent" note refers to stored identity fields.
- False: "a changed revision or slot on replay should be a revision or decision conflict." Equivalence includes the expected revision, so reusing an operation id with different semantics is an `OperationConflict`.
- Spec decision: live receipts keep origin, resources and creation time absent (Implementation Notes, B10/E1). Changing that edits the approved spec.
- Spec-dictated, low: the `Contracts/Operations` folder vs namespace mismatch (task paths plus a documented note; the types are DTOs, so the guard's intent holds), and the extended `Contracts.Queries.OperationStatus` record (named in the task list).
- Low, not worth fixing:
  - Post-dispatch `OperationStatusUpdateConflictException`, and a replay merging into a new command's status. Both need an old TaskId reused as a new CommandId in a race, and the fix adds catch and guard branches.
  - The Contracts concurrency converter, constructor and reducer slot checks are looser than the schema. The type is used only by the unregistered reducer, and the live adapter enforces the bounds.
  - One `ReasonCode` for every conflict kind. There is no consumer yet, and per-kind codes add catalog surface.
  - Duplicate type names bridged by `Enum.Parse`. The members are identical today.
  - `Snapshot` hand-copies the receipt DTOs. It covers every current property.
  - Missing OpenAPI descriptions and `preEffectFailure`. Adding them requires a client regeneration.
  - The duplicated authority-lapse check. It is a second check around the new async read.
  - `OriginalOperationId` holds only the immediate predecessor. This matters only from a third attempt.
  - Resource-id ordering on replay. The ids are server-derived per command.
  - The generated DTO defaults `ExpectedRevision` to 0. Canonical requests fail closed today.

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
