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
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs` assigns CommandId to new operation receipts; `Status/OperationStatusRecord.cs` preserves historical stored identity and carries optional canonical revision/transition metadata.
- `src/Hexalith.ChatBot.Server/Gateway/Status/ReadModelOperationStatusStore.cs` evaluates policy and update callbacks inside SDK conditional-write retries; its in-memory counterpart uses atomic compare-and-replace. Workflow reports and duplicate replay mutate the latest snapshot through those callbacks.
- `src/Hexalith.ChatBot.Server/Queries/OperationStatusQueryHandler.cs` provides nonblocking authorized reads; retain it.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.ChatBot.Contracts/Identities/ChatBotOperationId.cs`, `Operations/CommandOperationIdentity.cs`, `Operations/CommandOperationConcurrency.cs` — add ULID identity, immutable metadata, optional decision-slot identity and validated expected revision; one type per file.
- [x] `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, `Queries/OperationStatus.cs`, `Enums/CommandExecutionState.cs`, `Enums/CommandProjectionState.cs` — declare canonical state, origin, resources, expected/committed revisions and transition metadata; retain legacy wire compatibility. Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` through NSwag and refresh the client hash fixture.
- [x] `src/Hexalith.ChatBot.Server/Operations/CommandOperationState.cs`, `CommandOperationTransition.cs`, `CommandOperationReducer.cs` — implement Received→Admitted→Committed/Rejected/Failed; retain terminal outcomes, enforce expected revision and semantic replay, capture actor/server-UTC/reason/correlation. ProjectionPending/Projected/ProjectionLagging are separate. Model retries as new predecessor-linked attempts, never reopening terminal state.
- [x] `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`, `Status/OperationStatusRecord.cs`, `Status/OperationStatusHttpResults.cs` — integrate identity/status mapping without claiming an accepted receipt is committed. Preserve historical replay and safe-message filtering; canonical live writes remain unavailable without accepted owner/protection evidence.
- [x] `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs`, `InMemoryOperationStatusStore.cs`, `ReadModelOperationStatusStore.cs` — apply the same policy within atomic updates: ignore stale revisions, allow equivalent duplicates, reject incompatible equal revisions and terminal rollback; preserve committed/identity fields and explicit legacy compatibility.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Operations/CommandOperationContractTests.cs`, `Gateway/Status/OperationStatusMonotonicityTests.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`, `tests/Hexalith.ChatBot.Client.Tests/CommandSubmissionTransportTests.cs` — cover the matrix, synthetic revision races/folded end-state, transition facts, shared TaskId, lifetime replay, complete status, legacy decoding and missing/forbidden parity. Retain durable fencing and trusted-authority regressions.

**2026-10-20 resumed review work:**

- [x] `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` POST description — resolve Round18Build-BH-1 by qualifying canonical refusal as applying only after earlier admission checks, including authorization and the policy gate. Preserve the eight policy commands' retryable `dependency_degraded` precedence and the canonical refusal's non-retryable shape. Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` with NSwag and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- [x] `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs` — resolve Build15-VG-1 by adding a nonblank OperationId distinct from TaskId and CommandId to the existing text/JSON formatter theory; assert accepted identity precedence in both formats and retain the historical fallback rows.
- [x] `src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs` lookup catch and `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.OperationContracts.cs` — resolve Resume-BH-10 by rechecking the caller token before translating non-cancellation lookup failures. Cover a lookup which cancels the caller then throws IOException, asserting caller cancellation and zero reservation/audit/dispatch; retain active-caller IOException and dependency-owned cancellation coverage and successful retry.

This resume starts with the three Story 1.5 items carried by Story 1.6 reviews. The original review patches are checked off. Earlier explicit deferrals remain owned by their recorded follow-ups: strict oneOf compatibility, atomic workflow updates and duplicate counts, SDK serializer equivalence, accepted numeric equivalence, multi-event revision semantics, successor uniqueness and the separate historical lifecycle documentation are not silently resolved by this pass. Owner/protection acceptance and native evidence remain open. Status is reconciled to `in-progress` in both artifacts while these patches are open. After review, use spec `in-review` with sprint `review` consistently; never mark done without the existing evidence contract.

Shared-file preservation: apply only the caller-cancellation correction in `ChatBotCommandAdmissionPipeline.cs`; preserve Story 1.6's policy gate placement, authorization/audit/counter semantics and outward outcomes. Leave `CommandGateway.cs`, `CommandGatewayAdmissionApiE2ETests.cs` and `CommandGatewayTests.cs` unchanged unless a demonstrated Story 1.5 regression needs a direct correction. Keep Story 1.6 literal `review` in its spec and sprint tracking.

**2026-10-20 actionable deferral resume (after carried-finding commit):**

- [x] `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` — reconcile the original Story 1.2 contract/client finding 7 and Story 1.5 oneOf ledger entries against current code, then replace the four overlapping TaskId/CommandId identifier compositions (both operation path parameters, `OperationStatus.originalOperationId`, `OperationAuditHistory.operationId`) with their common unambiguous ULID schema reference. Preserve historical TaskId-valued identities. In `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`, remove the first-branch validation exemption and enforce actual oneOf cardinality; cover valid and invalid identifiers at all four locations and a deliberately overlapping composition. Regenerate the NSwag client and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- [x] `src/Hexalith.ChatBot.Server/Gateway/Status/IOperationStatusStore.cs`, `InMemoryOperationStatusStore.cs`, `ReadModelOperationStatusStore.cs`, `OperationStatusWorkflowStatusSink.cs` and `Gateway/ChatBotCommandAdmissionPipeline.cs` — reconcile Story 1.2 Loop 15 BH4, Story 1.5 workflow-sink and R3B9 duplicate-count ledger entries. Add an atomic update callback evaluated against the latest stored snapshot and again on every SDK conditional-write retry, using existing SDK seams. Convert workflow updates and replay duplicate increments to that callback; each successful duplicate suppression must increment exactly once despite contention. Preserve cancellation/current-authority checks, absent-record workflow behavior, historical identity, receipt/audit certainty, terminal workflow status/guidance and duplicate counts. Add deterministic concurrent gateway/store/sink regressions in `tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/` and `Gateway/CommandGatewayTests.OperationContracts.cs`, exercising both stores and real ETag conflicts with persisted final-state assertions. Do not broaden this into early workflow publication or durable scheduling recovery.
- [x] `tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/AtomicOperationStatusReadModelStore.cs`, `OperationStatusMonotonicityTests.cs` and focused supporting test types — reconcile the equal-revision SDK serializer ledger entry. Exercise canonical equal-revision duplicates, conflicting equal revisions and retained nested predecessor metadata through the production Dapr read-model adapter/serializer configuration, storing serialized bytes and reading fresh instances; object-reference fakes or an arbitrary serializer alone do not close this entry. Correct `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs` only if that production-seam evidence demonstrates an equivalence defect. Retain immutable identity, monotonic revision and defensive-copy behavior.
- [x] Update each original actionable ledger entry with its resolution and direct current-run evidence, preserving its historical text. Retain owner multi-event revision semantics, numeric equivalence and successor uniqueness as explicit unresolved owner decisions. Preserve all historical operation identities, canonicalizer behavior and lifetime fingerprints. No dependencies or submodule edits.

The user explicitly reopened these previously deferred corrections. During implementation Story 1.5 is spec/sprint `in-progress`; after implementation and review use spec `in-review` / sprint `review`. Story 1.6 remains literal `review` in both artifacts. Preserve its policy-gate placement, outcomes, authorization audit and counter behavior; its ADR/spec and shared `CommandGateway.cs`, `CommandGatewayAdmissionApiE2ETests.cs`, `CommandGatewayTests.cs` require no changes for this scope. Run focused affected checks and the existing required full verification, including Architecture/Conformance, and record their existing failures separately from the missing Aspire mailbox secret and native owner evidence. Neither story may be marked done without its evidence gate. The initial carried-finding commit is complete; this resumed build does not authorize another commit or push.

**Acceptance Criteria:**

- Given a new operation, when created/replayed, then original metadata stays stable and its operation ID cannot bind to different semantics.
- Given absent/malformed/stale concurrency input or competing semantics, when evaluated, then a typed safe rejection occurs before state changes and only the winning revision persists.
- Given a declared or illegal transition, when folded, then legal transitions capture actor/time/reason/correlation and illegal transitions preserve state; terminal retry creates a distinct linked attempt only for an eligible pre-effect failure.
- Given authorized status polling, when repeated or concurrent, then canonical state, safe reason, origin, attempts/ceiling, retry eligibility, partial metadata, prior outcome, terminal reason, correlation and safe actions remain coherent and retrieval does not wait for completion.
- Given missing A6/A13 acceptance, when live canonical execution is requested, then it fails closed; synthetic evidence does not establish native durability or owner execution.

## Implementation Notes

- Caller CommandId is the operation identity for new receipts. Stored receipts retain their exact historical OperationId, including the TaskId fallback when an old receipt omitted OperationId. Existing historical status collisions reject in admission before reservation, pre-commit audit and legacy dispatch; this read is not an atomic owner reservation.
- Canonical requests explicitly carry `concurrency`; missing, null, noninteger, negative, duplicate or unknown guard members reject during parsing. Omitting the entire object retains legacy request compatibility. After trusted authorization, every canonical request fails closed before reservation/dispatch while A6/A13 remain open.
- The unregistered pure reducer folds immutable transition events, validates semantic replay and revision guards, retains terminal execution outcomes independently of projection, and creates distinct predecessor-linked retry attempts with safe prior-outcome metadata. Equivalent replay of an existing predecessor-linked attempt returns its stored fold after guard validation; only creation of a new linked attempt requires Retry. It supplies synthetic evidence only and introduces no persistence path or runtime approval switch.
- Live legacy receipts keep canonical metadata absent; equivalent duplicate interleaving and initial status-write recovery preserve receipt identity without relying on unavailable durable metadata. Canonical metadata comes from the pure canonical fold only. Legacy updates merge receipt-proven committed audit status, the highest duplicate count and retained prior outcomes independently of timestamp ordering.
- Both status stores evaluate the same policy inside atomic replacement/SDK ETag retry, protect identity and terminal outcomes, reject incompatible equal revisions, ignore stale canonical revisions, and defensively copy mutable values. Legacy updates retain explicit timestamp-based compatibility; records are not implicitly upgraded.
- OpenAPI remains authoritative; NSwag regenerated the client and the hash fixture. Additive metadata is optional for legacy decoding. Canonical consumers use execution/projection state; legacy completion/audit summaries are compatibility fields. Transition actor identities remain server-side. Canonical public retry eligibility stays false while retry-profile approval is open, even when the pure fold is state-eligible; no caller approval switch or synthetic scheduling metadata is introduced. Public metadata files use the Contracts root namespace to retain the existing architecture guard against exported governance namespaces.

## Spec Change Log

- 2026-10-20: User reopened the existing strict identifier composition, atomic workflow/duplicate-count and production SDK serializer deferrals after the carried-finding commit. Added concrete tasks without changing frozen intent or baseline; retain owner revision, numeric-equivalence and successor-uniqueness decisions. Story 1.5 returns consistently to `in-progress` for implementation; Story 1.6 stays literal `review`.

- 2026-10-20: Reconciled spec `in-review` with sprint `in-progress` by reopening the three actionable Story 1.5 deferrals carried in Story 1.6 reviews (Round18Build-BH-1, Build15-VG-1, Resume-BH-10). Both tracked implementation as `in-progress`; after the three patches passed focused verification they moved consistently to spec `in-review` / sprint `review`. Earlier patches are complete; earlier explicit deferrals and missing native owner/protection evidence remain open. Preserve the frozen intent, original baseline and Story 1.6 policy-gate behavior.

- 2026-10-18: Resumed all three review layers at ca95a70b9237498568e87c5a95c9958f19717213, recorded eleven individual dispositions with no new patches, and refreshed the full local verification. The seven broader failures and missing runtime secret still prevent completion; frozen intent, original baseline and activation gates remain unchanged.

- 2026-10-17: Resumed review at eb6530d34f00d51920a42f88a3dffec3090715e3, retained the original baseline and prior dispositions, and added five focused reducer regression groups. Re-ran the required verification and identified the AppHost's missing recovery-mailbox secret as a concrete native-runtime blocker. Frozen intent and completion/activation gates remain unchanged.

- 2026-10-17: Resumed the three-layer review at 050866e685edadfd7ff529bd781f2daf1de83fb4, corrected lifetime replay of stored predecessor-linked attempts, and implemented nine accepted review corrections with schema, transition, snapshot and historical audit-recovery evidence. Recorded one separate Story 1.6 documentation deferral; preserved frozen intent and all completion/activation gates.

- 2026-10-17: Resumed the saved review, rejected duplicate root concurrency guards, corrected the canonical client fixture, and added replay/retry identity-binding regressions. Recorded every review disposition and three separate deferrals; preserved frozen intent and all owner/protection/completion gates.

- 2026-10-17: Applied the 15 accepted review patches, including the non-retryable canonical refusal, pre-reservation collision lookup, legacy field merge, private transition actors, accepted OperationId adapters, predecessor validation and focused regression evidence. Preserved all explicit deferrals and frozen intent.

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

### Build review triage (2026-10-17)

Three independent layers reviewed the preserved baseline diff. Each finding was classified before grouping; runtime reachability was checked against the reducer factories, status mapping, gateway cancellation boundary, and HTTP parser. Earlier Story 1.6/BMAD changes are historical context. No new intent decision or completion transition is required.

| ID | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| BH1 | low | rejected | Carried: reusable constructor/reducer slot bounds remain looser than HTTP. The prior review rejected this exact slot-check claim; the live adapter validates the published bounds and the reducer is unregistered. |
| BH2 | medium | patch | The reusable guard JSON reader silently ignores unknown members and duplicate slots, violating the closed schema for callers deserializing the published contract. Reject these inputs in the existing converter with direct regression coverage. |
| BH3 | medium | patch | A whitespace slot reaches constructor validation and escapes deserialization as ArgumentException, so consumers catching JsonException miss malformed JSON. Translate constructor validation to JsonException and retain its validation. |
| BH4 | low | patch | A public identity constructor accepts operationClass values outside the existing 80-character lowercase token schema; FromCanonical can then emit invalid contract metadata. Add the existing field's bounds to constructor and status validation. |
| BH5 | low | rejected | Malformed predecessor CommandId/CorrelationId require a manually forged internal snapshot. FromCanonical supplies validated predecessor identity/correlation from Retry. Extra guards on this disabled runtime path add complexity without an everyday caller reaching the defect. |
| BH6 | low | rejected | Invalid OriginalOperationId likewise requires forging the internal record; FromCanonical sets it to normalized predecessor/current identity. Further lineage guards add complexity to a closed runtime path. |
| BH7 | low | rejected | Negative counters/zero ceilings are forged snapshots. Receive validates the ceiling, Retry respects it, and FromCanonical derives the attempt count; no current canonical writer supplies these values. Extra guard branches are unnecessary for this run. |
| BH8 | low | rejected | Impossible initial revisions require forging snapshots rather than the reducer fold; actual creation/transition/projection factories assign revisions. Duplicating full fold validation in an inactive snapshot boundary adds complexity without a current caller. |
| BH9 | low | rejected | The chronology probe forges CreatedAt/AcceptedAt independently of the fold. Receive uses identity creation and Apply enforces monotonic time; FromCanonical preserves those values. Additional initial-snapshot guards have no everyday caller. |
| BH10 | low | rejected | Committed plus PreEffectFailure requires a forged snapshot; Apply rejects it before FromCanonical. Adding another guard on the inactive canonical boundary is unnecessary for this run. |
| BH11 | medium | patch | Confirmed: the relocated catch(Exception) swallows caller-token cancellation, while the outer gateway intentionally propagates OperationCanceledException. Preserve caller cancellation and retain dependency-owned cancellation as a safe 503. |
| BH12 | false | rejected | All actual reducer creation and replacement paths supply Array.AsReadOnly over fresh arrays; callers cannot mutate a reducer-produced transition list. The probe constructs a different internal state directly and does not demonstrate the alleged mutation path. |
| EC1 | low | patch | Confirmed by call tracing and the reviewer probe: Apply accepts lowercase correlation spelling, but status validation requires normalized spelling. Normalize valid transition correlations inside the existing fold boundary and prove a successful fold maps to valid status. |
| EC2 | low | rejected | Carried: same constructor/reducer slot-bounds claim as the prior rejected row and BH1; the live adapter already enforces schema bounds. |
| VG1 | medium | patch | Verified against the gateway's cancellation filter and new lookup catch: same caller-cancellation regression as BH11. The existing dependency-OCE test has an uncancelled caller token and does not cover this case. |

Patch groups: guard JSON strictness/exception consistency (BH2/BH3); operation-class metadata validation (BH4); caller cancellation (BH11/VG1); normalized fold correlation (EC1). All explicit prior deferrals remain unchanged.

### Resumed build review triage (2026-10-17)

Resumed from `in-review` at `8adcde63b2103a98930dc590db269c5be8a53498`, retaining baseline `5bb56567499c6df1c621184d3c3d35aa45521166`. All three independent layers completed before classification: Blind Hunter returned ten findings, Edge Case Hunter returned none, and Verification Gap returned one. The full baseline diff also contains previously committed Story 1.6 and BMAD work; it is context, not permission to rewrite that work.

| ID | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| R3B1 | low | rejected | `ChatBotOperationId` loses its value if directly JSON-deserialized, but every current use is a local `TryParse` result; serialized identity/status members are strings. Its private-constructor shape matches the existing command/correlation helpers. Supporting a new serialization boundary adds validation/converter complexity without an everyday caller. |
| R3B2 | low | rejected | The synthetic identity constructor permits command names excluded by the HTTP schema. Live requests already pass the adapter's length/pattern/suffix checks, and the reducer has no runtime registration. Another guard on this inactive internal-consumer path adds complexity without a current caller encountering the defect. |
| R3B3 | medium | patch | Confirmed: `TryGetProperty` chooses the final root `concurrency` member, so an earlier malformed/null guard can be hidden by a valid final guard. Reject repeated root guards in the existing adapter and cover both orderings plus equivalent duplicates. No public surface is added. |
| R3B4 | low | rejected | CLI status formatting omits canonical metadata, but live canonical writes are unconditionally closed and legacy receipts intentionally carry no canonical metadata. Extending the output contract adds fields/formatting branches for a state no current runtime writer produces; preserve this prerequisite boundary for owner activation. |
| R3B5 | low | rejected | The same inactive canonical-status boundary applies to MCP: its present writer supplies legacy status. Adding canonical fields to the structured output introduces public surface and filtering branches without an everyday reachable canonical record. |
| R3B6 | maybe-false | defer | Unverified medium: raw numeric spellings hash differently in the unchanged canonicalizer; no accepted v1 rule or bound typed command establishes whether `1`, `1.0`, and `1e0` must alias. Confirm owner command numeric equivalence and golden vectors before canonical owner activation; do not silently change existing lifetime fingerprints. |
| R3B7 | low | rejected | Carried: the earlier review explicitly rejected the immediate-predecessor meaning of `OriginalOperationId` for third attempts. The mapping is unchanged, canonical retry remains inactive, and changing lineage propagation adds state/branches. |
| R3B8 | medium | patch | The client canonical-status fixture supplies a predecessor without its matching prior outcome and omits the terminal reason retained by the server fold. Correct the existing fixture to a producible retry snapshot and directly assert retained predecessor metadata and terminal facts. |
| R3B9 | medium | defer | Confirmed pre-existing lost increment: the preserved baseline already reads a status and writes `DuplicateAttemptCount + 1`, so two duplicate callers can write the same next count. The new maximum merge preserves monotonicity but does not create that race. Atomic increment semantics belong to a separate store/caller change. |
| R3B10 | low | defer | The Story 1.6 ADR tells clients to wait for acceptance while its existing gateway dependency outcome is retryable. This documentation/contract discrepancy comes from the separately committed policy work (`a5548fe3dd185b2cc754ac730d7f2cc88b6f2c1d` for the ADR), not Story 1.5; reconcile it with that story's owner without changing the approved policy behavior here. |
| R3V1 | medium | patch | Verified regression gap: replay/retry fixtures do not vary tenant, actor, resource identities or operation class. Add parameterized conflict assertions that retain the original failed fold for each binding. These directly verify the already implemented immutable-identity checks. |

Surviving patch groups are root-guard uniqueness (R3B3), producible client fixture (R3B8), and immutable-binding verification (R3V1). The three deferred findings are recorded separately; rejected findings are not patched or deferred. Frozen intent, prior review decisions, A6/A13, retry-profile approval and the native/primary completion gate remain unchanged.

### Fourth build review triage (2026-10-17)

Resumed at `050866e685edadfd7ff529bd781f2daf1de83fb4`, preserving baseline `5bb56567499c6df1c621184d3c3d35aa45521166`. All three independent reviewers completed before triage. Each finding is classified separately below; earlier decisions are carried only where the location and claim are unchanged.

| ID | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| R4B1 | low | rejected | Carried: the earlier converter/constructor slot-schema mismatch was explicitly rejected. Null slots still deserialize in the reusable contract while the live adapter rejects them; another guard on the unregistered synthetic boundary adds complexity without an everyday caller. |
| R4B2 | low | patch | Existing Contracts tests already pin required revision, minimum, format and slot bounds, and adapter tests exercise malformed guards; however object closure and integer/string types are not pinned. Complete those existing schema assertions so these new guard rules cannot drift silently. |
| R4B3 | low | patch | The published canonical state arrays are pinned only to literals, whereas the status mapper resolves Contracts enum names into generated enums. Compare the actual Contracts enum names to the schema in the existing test, preventing a renamed member from reaching a failing mapper. |
| R4B4 | medium | patch | Every projection fixture uses the commit reason again, so losing the retained terminal reason would pass. Add distinct known commit/projection reasons and assert retained terminal fields independently of last-transition facts. |
| R4B5 | medium | patch | Existing illegal-transition tests cover state edges and actor bounds, but do not reject backdated/non-UTC direct events, unknown/generic reasons, invalid correlations and undefined enum facts. Add unchanged-fold assertions through Apply for these declared validations. |
| R4B6 | medium | patch | Projection tests cover committed progress and rollback only. Add Received, Admitted, Rejected and Failed cases proving Project cannot advance projection or change an uncommitted/unsuccessful fold. |
| R4B7 | medium | patch | The defensive-copy test mutates only ResourceIds. Extend it to SafeNextActions and PartialOutputCodes at both input and read boundaries through both stores, so removal of those copies cannot silently alter public metadata. |
| R4B8 | medium | patch | Predecessor tests reject replacement via Upsert but never mutate input/read generated PriorOutcome objects. Add direct mutation evidence for retained predecessor metadata and nested legacy outcomes through both stores. |
| R4B9 | low | rejected | The SDK fixture intentionally models one key per instance; every current consumer creates a separate instance for its one-operation scenario. Key-aware partitioning would add fixture state for a use no existing test makes. These synthetic tests make no native tenant-isolation claim. |
| R4B10 | low | defer | The lifecycle relationship-name documentation mismatch belongs to separately committed Story 1.6, whose factory changed to superseded_by/supersedes before this resume. Record it separately without rewriting that story's artifact. |
| R4E1 | false | rejected | Carried: the earlier review rejected the same null SafeNextActions snapshot claim. It is a required positional persisted field and all existing writers supply it; the finding again supplies no historical writer that omitted it. |
| R4E2 | medium | patch | Confirmed: Receive rejects every predecessor-linked identity before comparing an existing equivalent fold; Retry has no current-state argument. Move the new-attempt predecessor rejection after stored-fold replay, retaining guard validation and rejecting predecessor creation outside Retry. Cover every retry lifecycle state plus conflicting/malformed replay. |
| R4V1 | medium | patch | Verified gap: both retained audit recovery tests generate only command-based receipts, so tightening either reconciliation path to CommandId equality would go undetected. Extend existing fixtures with historical TaskId-based preparations/intents/envelopes and assert exact reconstructed receipt metadata and one dispatch. |

Patch groups remain direct corrections to the reducer and existing verification boundaries; they add no public API or runtime activation. The prior implementation agent is unavailable in this resumed session, so patches are applied inline. Frozen intent, A6/A13, retry-profile approval and native/primary completion obligations remain unchanged.

### Fifth build review triage (2026-10-17)

Resumed at `eb6530d34f00d51920a42f88a3dffec3090715e3`, retaining baseline `5bb56567499c6df1c621184d3c3d35aa45521166`. All three independent layers completed before classification: Blind Hunter returned ten findings, Edge Case Hunter returned two, and Verification Gap found no gaps. Each finding is classified separately; unchanged earlier claims retain their recorded dispositions.

| ID | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| R5B1 | low | rejected | The pre-reservation collision read can block historical command A's replay if another historical command already used A as its TaskId/OperationId, even though A's receipt names a different operation. This requires a cross-kind historical ULID alias; ordinary lifetime replay remains covered and succeeds. Preserving this rare alias would require a new read-only receipt lookup across idempotency implementations before the no-reservation gate, adding behavior beyond a direct correction for an everyday defect. |
| R5B2 | low | rejected | Carried: the earlier review rejected the same single catalog reason for different typed reducer conflicts. Conflict already distinguishes the outcomes, the reducer has no live consumer, and differentiated safe-message mapping adds branches without an everyday caller. |
| R5B3 | low | rejected | The pure unregistered reducer accepts any known non-generic safe reason, including CommandAccepted on a failed fact. No current runtime transition writer consumes this path. A new state-to-reason taxonomy would add contract decisions and validation branches for an inactive consumer; existing known-code and retained-terminal-reason checks remain enforced. |
| R5B4 | low | rejected | Canonical fields are independently optional to preserve legacy wire decoding. The actual FromCanonical writer supplies coherent fields and the store validates them; the reviewer identifies schema-permitted fabricated responses rather than a malformed response from a current writer. Adding conditional schema branches and generated-client handling adds complexity without an everyday defect. |
| R5B5 | low | rejected | Existing OpenAPI examples intentionally demonstrate producible legacy responses; canonical client and status fixtures already exercise complete synthetic metadata. Adding a second public example, regeneration and example-validation machinery is optional documentation expansion, beyond a direct correction to an incorrect example. |
| R5B6 | medium | patch | Verified test gap: Project's missing/negative and mismatched revision branches have no direct committed-fold assertions. Add focused cases asserting the typed conflict and exact unchanged execution, projection, committed revision and fold. |
| R5B7 | medium | patch | Verified test gap: existing eligible-retry rejection rows vary tenant/actor/resources/class but not command type/input, missing or changed predecessor, reused operation identity or backdated creation. Add independently varied rejection cases against an eligible predecessor, with a successful control and unchanged-fold assertions. |
| R5B8 | medium | patch | Verified test gap: decision-slot conflict tests do not exercise a foreign-tenant occupant. Add a same-tenant conflict control and foreign-tenant success, retaining the occupant unchanged. This protects the existing tenant comparison without claiming native store isolation. |
| R5B9 | medium | patch | Verified test gap: direct transition fixtures do not set PreEffectFailure on otherwise valid non-failure events. Cover admission, commit and both projection targets; establish the unmodified event is legal, then assert rejection and unchanged state for its flagged counterpart. |
| R5B10 | medium | patch | Verified test gap: direct-event replay only repeats the latest fact. Replay admission and commit after projection completes, then alter each historical fact at its old revision; assert exact replay retains the complete current fold and the altered fact conflicts without losing progress. |
| R5E1 | false | rejected | Carried: null SafeNextActions was already rejected in the earlier and fourth reviews. It is a required positional field and every historical/current writer supplies it; the reviewer again constructs an omitted/null persisted value without identifying a writer that produced it. |
| R5E2 | low | rejected | Carried: the earlier BH5 row rejected the same forged predecessor-correlation snapshot. Retry constructs prior identity/correlation from a validated CommandOperationIdentity and FromCanonical retains them; another guard on a manually fabricated inactive snapshot adds complexity without an everyday caller. |

The five surviving findings have independent test gaps and remain separate patch groups. The previous implementation agent is unavailable in this resumed session, so the focused test additions are applied inline. No new public surface, persistence path, runtime activation or deferral is introduced. Frozen intent, prior deferrals, A6/A13, retry-profile approval and native/primary completion obligations remain unchanged.

### Sixth build review triage (2026-10-18)

Resumed at `ca95a70b9237498568e87c5a95c9958f19717213`, preserving baseline `5bb56567499c6df1c621184d3c3d35aa45521166`. All three independent layers completed before triage. Blind Hunter returned ten findings (its 934,552-byte diff gives the capped floor of ten), Edge Case Hunter returned one, and Verification Gap reported no gaps. Each finding is classified separately below. The baseline diff includes separately committed Story 1.6/BMAD changes; those remain context.

| ID | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| R6B1 | low | rejected | Extending the public positional Contracts record changes its binary constructor/Deconstruct signatures. Repository consumers use the generated wire DTO; no caller constructs or deconstructs this Contracts record. Supporting hypothetical precompiled consumers requires new overloads and parameter forwarding, beyond a direct correction for an everyday caller. Existing legacy wire decoding remains tested. |
| R6B2 | low | rejected | Generated record equality compares the defensively copied ResourceIds collection by reference. The actual reducer deliberately compares immutable bindings and resource elements rather than record equality, and no current consumer relies on structural identity equality. Adding equality/hash overrides or new public guidance adds behavior for an unused comparison boundary. |
| R6B3 | low | rejected | Carried: R3B2 rejected the same CommandType constructor/schema mismatch. The live adapter enforces command-name bounds and the reducer remains unregistered; another guard adds complexity without a current runtime caller encountering the defect. |
| R6B4 | low | rejected | Carried: BH1/EC2/R4B1 and the earlier rejected list cover the same reusable decision-slot bounds mismatch. The live adapter validates the published token bounds; adding another constructor/converter guard on the inactive synthetic boundary remains rejected. |
| R6B5 | low | rejected | OperationIdFor's TaskId-null fallback is byte-identical to the preserved baseline. A different result from the surface formatters requires an invalid historical receipt with both missing OperationId and empty/whitespace TaskId; no producer or retained fixture demonstrating that shape was identified. Another fallback branch would change historical key selection for a fabricated receipt rather than correct an everyday reachable case. |
| R6B6 | low | rejected | Carried: R5B4 rejected the same independently optional canonical wire fields. Optional fields preserve legacy decoding; FromCanonical and the stores enforce coherent actual snapshots. Conditional schema alternatives and generated-client handling add complexity for fabricated responses. |
| R6B7 | low | rejected | Carried: R5B3 rejected the same known-catalog-but-incoherent terminal reason claim. The pure reducer has no runtime transition writer; a state-to-reason taxonomy adds contract decisions and branches to an inactive consumer. Existing known-code and retained-terminal-reason validation remains enforced. |
| R6B8 | low | rejected | Carried: R4B9 rejected the same one-slot SDK fixture limitation. Each current scenario creates a separate fixture for its single key, and these tests make no native tenant/operation partition-isolation claim. A keyed fixture registry adds unused test state. |
| R6B9 | maybe-false | defer | Carried: the existing explicit serializer-round-trip deferral covers this exact claim. Production-serializer round trips must settle persisted equal-revision equivalence before canonical persistence activation. Do not append or implement the same deferred item again. |
| R6B10 | false | rejected | The artifact explicitly treats full-suite failures as a halt and leaves completion blocked; it never accepts focused successes as sufficient. The previous eight-failure result is correctly historical. Current verification below has seven broader failures and a passing full Server lane. No new code defect or incorrect completion claim is established by this finding. |
| R6E1 | false | rejected | Carried: the original rejected finding, R4E1 and R5E1 cover the identical omitted/null SafeNextActions claim. It has always been a required positional persisted field and every identified writer supplies it. The reviewer again supplies no historical producer of an omitted/null value, so the proposed malformed persisted state does not establish the alleged reachable regression. |

No patch or new deferral survives this review. Runtime code and tests remain unchanged. Frozen intent, prior dispositions/deferrals, A6/A13, retry-profile approval and native/primary completion requirements remain unchanged.

### Seventh build review triage (2026-10-20)

Resumed at `eeb738cd35597155c7abb53eac258f9e4f67fc82`, preserving baseline `5bb56567499c6df1c621184d3c3d35aa45521166`. All three independent context-free layers completed before classification: Blind Hunter returned ten findings, Edge Case Hunter returned one, and Verification Gap found no gaps. The reviewer diff is 962,343 bytes, excluding this claims file; the complete baseline audit and current-run diff are retained under `/tmp/story-1-5-oct20-build-3c7_lrdw/`. The broad historical baseline includes independently committed Story 1.6, release and BMAD work. Every finding repeats an explicit earlier disposition or ledger entry; the carried entries retain their ownership without duplicate deferrals or source changes.

| ID | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| R7B1 | medium | defer | Carried from Story 1.6 R22Build-BH-1/EH-2 and the release-maintenance ledger: `pack-release-packages.sh:28` still passes a prerelease SemVer into FileVersion. The reviewer independently reproduced CS7035 with warnings as errors. The helper is unchanged by this resume; numeric file-version handling remains release maintenance. |
| R7B2 | medium | defer | Carried from Story 1.6 R22Build-BH-2/EH-1/VG-2: `release.yml:300-305` still performs an exact-tag lookup when semantic-release returns success without a release. This unchanged release issue already has its own ledger entry. |
| R7B3 | medium | defer | Carried from Story 1.6 R22Build-BH-7/EH-4: the publication verifier's curl still has no transfer/connection timeout, so the outer deadline does not bound a stalled request. Existing release-maintenance ownership stands. |
| R7B4 | low | defer | Carried from Story 1.6 R22Build-BH-6: the unchanged pack helper resolves its relative output directory on both sides of `cd`; its current caller starts at the root. A single resolved output path remains the recorded release-maintenance correction. |
| R7B5 | medium | defer | Carried from the explicit Story 1.5 successor-uniqueness deferral: the pure reducer does not record a successor on the predecessor and can construct distinct equal-depth retries. Owner enforcement remains due with Stories 1.8/1.10 before live retry activation; no synthetic reducer test grants it. |
| R7B6 | maybe-false | defer | Carried, unverified medium: the expected-plus-one commit invariant is explicit in both reducer and store. Accepted EventStore multi-event owner revision semantics are still required to settle its suitability when Story 1.8 wires the owner commit. |
| R7B7 | maybe-false | defer | Carried, unverified medium: the SDK fake retains object references and does not prove equivalence against a production-serializer round trip. The original review deferral requires that evidence before canonical writes go live; no actual divergent stored value was demonstrated. |
| R7B8 | maybe-false | defer | Carried from R3B6, unverified medium: the unchanged canonicalizer preserves numeric spellings. Accepted typed-command numeric equivalence and golden vectors remain required before owner activation; historical lifetime fingerprints are preserved. |
| R7B9 | low | defer | Carried from the explicit pre-existing oneOf compatibility deferral: TaskId and CommandId have identical ULID schemas, so their remaining strict oneOf branches cannot match exactly one alternative. Current schema/legacy compatibility ownership is unchanged; this pass changes POST documentation only. |
| R7B10 | medium | defer | Carried from Story 1.6 round 8: `AiActionApprovalGate:97-98` forwards only the snapshot ID from the resolved tenant/ID/version tuple. The production provider remains unavailable. Carrying the tuple into approval results and events stays with Story 1.6's accepted live-persistence scope; its policy behavior is preserved. |
| R7E1 | medium | defer | Carried: the same no-release exact-tag failure as R7B2 and the existing R22Build-BH-2/EH-1/VG-2 ledger entry. It shares that root cause and release-maintenance follow-up. |

All eleven raw findings retain individual rows. Grouping combines only R7B2/R7E1's same exact-tag defect; the other nine remain separate. There are no new patches, intent gaps, spec loopbacks or additional ledger entries. The three requested deferrals are resolved in their original ledger evidence, preserving the earlier text and review history. Frozen intent, A6/A13, retry-profile approval and native/primary obligations remain unchanged.

### Eighth build review triage (2026-10-20, actionable deferrals)

All three independent context-free layers completed before classification: Blind Hunter returned ten findings, Edge Case Hunter three, and Verification Gap two other findings with no verification gaps. The reviewed cumulative diff was 1,013,625 bytes, retained at `/tmp/story-1-5-deferrals-review-ll8pvwht/review-input.diff`; the final patched cumulative diff is `review.diff` in that directory; `baseline-full.diff` preserves the complete original-baseline audit including this claims file, and `resume.diff` preserves this run's changes. Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and resumed HEAD `68cab6e21f5a24c44d9e99c52dc1e6b4af834673` are unchanged. Historical Story 1.6/release/BMAD work is context, not authority to rewrite it.

| ID | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| R8B1 | medium | defer | Verified pre-existing: replay may initialize a missing status, workflow then completes, and the original gateway's later/equal-stamped `Accepted` upsert clears workflow fields and partial outputs. The gateway upsert and legacy merge are unchanged by this resume; the original blind-replacement baseline also lost these fields. Record a distinct initial-receipt merge/lifecycle follow-up without reopening early-publication or scheduling recovery here. |
| R8B2 | medium | patch | Verified: the sink's new max timestamp can let an older retrying/started report overwrite a newer completion after losing CAS, restoring pending reason and retry guidance. Preserve completion for the same workflow instance; retain the explicit existing failed-to-started restart behavior. Add a deterministic both-store workflow/workflow contention regression. |
| R8B3 | medium | defer | Carried from Story 1.2 Loop 15 BH3 and its ledger: the sink deliberately skips absent operations, while dispatch can publish before initial status creation. The existing early-publication lifecycle/reconciliation follow-up remains open; this pass preserves absent-record behavior. |
| R8B4 | low | rejected | Carried from BH1/EC2/R4B1: reusable constructor slot bounds differ from HTTP, but the current adapter validates the published bounds and the synthetic reducer is unregistered. Additional validation on that inactive boundary remains rejected; no new consumer or reachable defect was demonstrated. |
| R8B5 | medium | defer | Carried from R7B1 and Story 1.6 R22Build-BH-1/EH-2: prerelease FileVersion handling remains release maintenance. The pack helper is unchanged. |
| R8B6 | medium | defer | Carried from R7B2/R7E1 and Story 1.6 R22Build-BH-2/EH-1/VG-2: semantic-release can succeed without tagging, then exact-tag verification fails. Existing release ownership stands. |
| R8B7 | medium | defer | Carried from R7B3 and Story 1.6 R22Build-BH-7/EH-4: curl has no per-request timeout. The unchanged publication helper retains its existing deadline follow-up. |
| R8B8 | low | defer | Carried from R7B4 and Story 1.6 R22Build-BH-6: relative pack output paths resolve on different sides of `cd`. Existing release-maintenance ownership stands. |
| R8B9 | maybe-false | defer | Carried, unverified medium, from Story 1.6 R22Build-BH-8 and the artifact-content ledger: package availability plus skip-duplicate does not establish artifact identity. No conflicting artifact was shown; the recorded signing-aware conflicting-content fixture is still required. |
| R8B10 | medium | defer | Carried from the Story 1.4 ledger: unknown stored completion/audit strings throw before the generic-denial fallback. Parsing is unchanged; schema migration/forward-compatibility fallback decisions remain with that existing follow-up. |
| R8E1 | medium | patch | Same verified completion regression as R8B2, observed at the atomic sink callback. Preserve same-instance completed progress and directly assert unchanged persisted retry eligibility/guidance after a losing older report. |
| R8E2 | medium | defer | Carried: same no-release exact-tag failure as R8B6 and the original release-maintenance ledger. |
| R8E3 | low | rejected | Carried: same inactive constructor slot-bounds claim and prior rejection as R8B4. The HTTP adapter still enforces the actual live boundary. |
| R8V1 | medium | defer | Carried: same no-release exact-tag defect as R8B6/R8E2 and Story 1.6 R22Build-BH-2/EH-1/VG-2. This is an other finding, not a missing test claim. |
| R8V2 | medium | defer | Carried from Story 1.6 R22Build-VG-1: release timeout 45 versus Architecture's expected 30 remains an unchanged, separately failing broader check. No budget/guard decision is taken here. |

All fifteen findings retain individual rows. Only R8B2/R8E1 share the new direct patch; R8B6/R8E2/R8V1 share the carried exact-tag defect and R8B4/R8E3 share the prior rejection. R8B1 is the only new deferral. No intent change, spec loopback, owner approval, activation or completion is inferred.

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

- [x] [Review][Patch] (Decided 2026-10-17: (a) non-retryable catalog code plus an OpenAPI note on `/api/v1/commands`) Canonical-request refusal tells clients to retry indefinitely — When `Concurrency` is not null, the request maps to the internal reason `canonical_execution_unavailable`, which `CreateDependencyUnavailable` turns into a 503 `dependency_degraded` with `retryable=true` and `clientAction=RetryLater`. The path stays closed by policy until A6/A13 are accepted, so a compliant client retries forever. The refusal is not documented on `/api/v1/commands`. Options: (a) keep fail-closed but add a non-retryable catalog code plus an OpenAPI response note; (b) keep it as transient unavailability and document that; (c) use a validation-style refusal. Medium. [src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs:184; src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs:360] (blind-hunter)
- [x] [Review][Patch] (Decided 2026-10-17: (a) legacy field merge, so receipt-proven `committed` is never superseded by a replay-inferred `reconciling`; keep the max `DuplicateAttemptCount` and the `PriorOutcome`) Legacy status timestamp ordering can drop the original receipt's committed audit status — The legacy branch keeps whichever write has the later `LastUpdatedAt`. A duplicate that arrives during the original's final status write reads no status, so it initializes `Accepted(priorOutcome, auditReconciliationRequired: true, now)`. The original write then carries the older stamp, so it is discarded, whether or not it arrives first. `auditStatus` stays `reconciling` permanently because no writer ever sets it back to committed. Before this change the bad outcome needed the replay to land last; now it happens whenever the replay has the later stamp. The race test uses a constant clock and does not assert `AuditStatus`. Options: (a) a legacy field merge where receipt-proven `committed` is never superseded by a replay-inferred `reconciling`; (b) restore arrival-order last-writer-wins; (c) stop replay from initializing an absent status, which reopens the E1/B10 recovery path. Medium. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:35; src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs:355] (blind-hunter+verification-gap+edge-case-hunter)
- [x] [Review][Patch] (Decided 2026-10-17: (a) move the read-only collision lookup into the admission pipeline before reservation and update the stage-order tests) Historical-collision 409 is returned after state has already changed — The new lookup in `CommandGateway.SubmitAsync` runs only after `AdmitAsync` returned Accepted. By then the pipeline has reserved coarse idempotency and durably written the `gate_passed` pre-commit audit envelope. The 409 then calls an abort whose failures are swallowed, and the 409 itself is not audited. AC2 requires rejection before state changes, while the Implementation Notes say only "before legacy dispatch". Options: (a) move the lookup into the pipeline before reservation and update the stage-order tests; (b) keep the placement and emit a compensating rejection audit; (c) accept it, since it needs an old TaskId reused as a new CommandId, and document that. Medium. [src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs:136-147] (acceptance-auditor)
- [x] [Review][Defer] (Decided 2026-10-17: (b) — deferred: successor uniqueness belongs to the owner aggregate (Stories 1.8/1.10); the pure reducer limit is documented in deferred-work) One failed attempt can be retried more than once — `Retry` records nothing on the predecessor, so distinct new ULIDs can each retry the same failed attempt and each gets `Attempt = previous.Attempt + 1`. `MaxAttempts` therefore limits chain depth, not total attempts. This conflicts with AC3 ("a distinct linked attempt") and the Race matrix row. Options: (a) a successor claim on the predecessor, such as the predecessor id used as a decision slot, so a second retry conflicts; (b) leave enforcement to the owner aggregate in stories 1.8/1.10 and document the reducer limit; (c) an attempt registry. Medium. [src/Hexalith.ChatBot.Server/Operations/CommandOperationReducer.cs:123] (blind-hunter+acceptance-auditor)
- [x] [Review][Patch] (Decided 2026-10-17: (b) declare the legacy completion/audit fields compatibility-only for canonical records; derive canonical `retryEligible` from the fold plus an approved retry profile, which stays closed; add tests) Canonical-to-status mapping is incoherent outside the committed path — `FromCanonical` has these gaps:
  - Received and Admitted map to `completionStatus: accepted-projection-pending`.
  - Rejected and Failed show `auditStatus: reconciling` and `projectionState: ProjectionPending` forever.
  - The Received fact carries the reason `command_accepted`.
  - `NextRetryAt` and `PartialOutputCodes` are never set, so a retry-eligible fold always serializes `retryEligible: false` and `hasPartialOutputs: false`.
  - `ValidateCanonical` does not tie `ReasonCode`, `SafeNextActions` or `NextRetryAt` to the fold.
  - The tests reach `true` only by hand-patching the record.

  The legacy enums offer only accepted-projection-pending/completed/failed and committed/reconciling. Options: (a) extend the public completion and audit enums; (b) declare the legacy fields compatibility-only for canonical records, so consumers read `executionState`/`projectionState`, and derive `retryEligible` from the fold plus an approved retry profile (still closed); (c) defer to stories 1.8/1.10 with retry-profile approval. Medium. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs:71-108; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:66] (blind-hunter+acceptance-auditor)
- [x] [Review][Patch] (Decided 2026-10-17: (a) drop `actorId` from the public `CommandOperationTransition` and keep it server-side; regenerate the client and refresh the hash fixture) `lastTransition.actorId` is published on operation status without a redaction marker — The actor token allows `@`, and the tests use `actor@example.test`, so actor identities that may be PII reach any authorized reader of the tenant's status. The existing `TaskIntentTransitionAuditSummary` exposes `actorId` with a `redactionState`, but the new schema has none. This is not live yet because canonical records are synthetic, but the contract is published. Options: (a) drop `actorId` from the public transition and keep it server-side; (b) add `redactionState` and redact non-service actors; (c) keep it as-is for tenant-scoped readers. Medium. [src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml:637; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:125] (acceptance-auditor)
- [x] [Review][Patch] CLI, MCP and UI still report TaskId as the operation id. Use `response.OperationId`, with the historical fallback only when it is empty. Update `ChatBotCliCommandTests.cs:266`, which pins `"op-1"`, and the UI tests. Add CLI and MCP assertions with OperationId ≠ TaskId. High. [src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:27; src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs:44; src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs:31; src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs:35] (all four layers)
- [x] [Review][Patch] `Receive` accepts an identity that already carries `PredecessorOperationId`. That creates a linked attempt with `Attempt = 1` and no `PriorOutcome`, bypassing `Retry` eligibility. `ValidateCanonical` also accepts a predecessor without `PriorOutcome`. Reject these outside `Retry` and require `PriorOutcome` exactly when a predecessor is set. Medium. [src/Hexalith.ChatBot.Server/Operations/CommandOperationReducer.cs:51; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:128] (blind-hunter+acceptance-auditor)
- [x] [Review][Patch] The legacy branch of `OperationStatusUpdatePolicy` has no tests, so replacing line 35 with `return incoming;` passes every test. Add older-after-newer and identity-drift rows for both stores, and an advancing-clock duplicate-during-initial-write variant that asserts `AuditStatus` (its outcome depends on the timestamp-ordering decision). Medium. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:22-35] (verification-gap)
- [x] [Review][Patch] The failure path of the pre-dispatch status lookup is untested. Add tests where `TryGetAsync` throws or is cancelled, asserting 503, zero dispatches, and a released reservation that allows a later retry. Medium. [src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs:136-158] (verification-gap)
- [x] [Review][Patch] The canonical-refusal path is untested for precedence and over HTTP. Cover a denied caller sending `concurrency`, asserting the authorization denial, audit and counter. Cover an HTTP POST with a `concurrency` body, asserting 503 with no reservation, audit or dispatch. Medium. [src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs:184-189] (verification-gap+blind-hunter)
- [x] [Review][Patch] Status-store metadata validation is incomplete:
  - Legacy records (`CanonicalRevision` null) may carry `Origin`, `ResourceIds`, `CreatedAt`, `EquivalenceVersion` or `PredecessorOperationId`, and `ResourceIds` is then exposed without validation.
  - Canonical `PredecessorOperationId` is not validated.
  - `OperationId` and `CorrelationId` are parse-checked but not checked for normalized equality.

  Low. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:106-112; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:128-144; src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusHttpResults.cs:32-34] (acceptance-auditor+edge-case-hunter)
- [x] [Review][Patch] `PrepareDispatchAsync` now accepts `OperationId == TaskId` for a brand-new Reserved preparation. Restore strict equality with `CommandId` there and keep the historical tolerance in the reconcile paths only. Low. [src/Hexalith.ChatBot.Server/Gateway/Idempotency/DaprCoarseIdempotencyStore.cs:257] (acceptance-auditor)
- [x] [Review][Patch] The concurrency guard's token bounds are untested and not tied to OpenAPI. Add adapter rows for a slot with a space, a `/`, 241 characters, a number and null. Add a drift assertion against the YAML `pattern` and `maxLength`. Low. [src/Hexalith.ChatBot.Server/Gateway/CommandSubmissionContractAdapter.cs:148-162] (verification-gap+blind-hunter)
- [x] [Review][Patch] `CommandOperationConcurrencyJsonConverter.Write` throws `ArgumentNullException` for a null value, which STJ passes when `HandleNull` is true. Write a JSON null instead. Low. [src/Hexalith.ChatBot.Contracts/Serialization/CommandOperationConcurrencyJsonConverter.cs:50] (blind-hunter+edge-case-hunter)
- [x] [Review][Patch] `CommandOperationReducer.Project` lacks the `ArgumentNullException.ThrowIfNull(current)` guard that the other entry points have. Low. [src/Hexalith.ChatBot.Server/Operations/CommandOperationReducer.cs:75] (blind-hunter+edge-case-hunter)
- [x] [Review][Defer] OpenAPI `oneOf: [TaskId, CommandId]` over identical schemas can never satisfy strict `oneOf` validation. It remains on the operation path parameters, `originalOperationId` and `OperationAuditHistory.operationId`; this change fixed only `OperationStatus.operationId`. [src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml:97] — deferred: pre-existing contract ambiguity. Resolution 2026-10-20: reopened and corrected in the actionable deferral resume; all four sites now reference the shared ULID schema and actual zero/one/two branch cardinality is covered by `patch-contracts.trx` below.
- [x] [Review][Defer] `OperationStatusWorkflowStatusSink` builds its write from an earlier `TryGetAsync`, so concurrent replay or workflow fields can be lost. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusWorkflowStatusSink.cs:25-69] — deferred: pre-existing; the old store's blind replace lost them too. Resolution 2026-10-20: reopened and corrected with the atomic store callback; both-store terminal interleaving, absence and historical identity evidence is retained in `patch-status.trx` and `patch-gateway.trx` below.
- [x] [Review][Defer] The reducer and the store require `CommittedRevision == ExpectedRevision + 1`, which assumes single-event owner commits. [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdatePolicy.cs:125-127] — deferred: maybe-false, unverified medium. Settle by confirming EventStore owner revision semantics for multi-event commands when Story 1.8 wires the owner commit.
- [x] [Review][Defer] The synthetic SDK store keeps the object reference and never JSON round-trips, so equal-revision equivalence (compared by `JsonSerializer.Serialize`) is unproven against a store-deserialized `current`. [tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/AtomicOperationStatusReadModelStore.cs:53] — deferred: maybe-false, unverified medium. Settle with a round-trip through the DAPR read-model serializer before canonical writes go live. Resolution 2026-10-20: reopened and settled through the production Dapr read-model adapter/default serializer over conditional bytes; equal-revision duplicates/conflicts and nested predecessor metadata pass in `patch-status.trx` below. No equivalence defect was demonstrated.

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

### Current review-patch validation (2026-10-17)

This pass implements all **15** accepted `[Review][Patch]` rows above. The five explicit deferrals remain deferred. Frozen intent, A6/A13, retry-profile approval, and the native/primary completion gate remain unchanged. No completion transition is proposed.

| Accepted patch | Current direct evidence |
| --- | --- |
| 1. Non-retryable canonical refusal | `MessageCatalogContractTests.CanonicalExecutionRefusalShouldBeTerminalBlockedAndNeverSuggestRetry`; `CommandGatewayTests.CanonicalLiveRequestShouldFailClosedBeforeReservationOrDispatchDespiteSyntheticEvidence`; `CommandGatewayAdmissionApiE2ETests.CanonicalConcurrencyHttpRequestShouldFailClosedBeforeReservationAuditOrDispatch` assert catalog code, 503, `retryable=false`, and `clientAction=none`. OpenAPI documents POST refusal; English/French resources cover the new entry. |
| 2. Legacy receipt/audit merge | `OperationStatusMonotonicityTests.LegacyReceiptCommitShouldSurviveLaterReplayTimestampInEitherArrivalOrder` covers both stores/orderings and preserves committed audit certainty, duplicate count, and prior outcome. |
| 3. Collision lookup before reservation | `CommandGatewayTests.NewCommandIdShouldNotAliasHistoricalTaskBasedOperationBeforeDispatch` asserts no reservation, audit envelope or dispatch; `GatewayShouldExecuteAdmissionStagesBeforeDispatch` pins the earlier lookup. |
| 4. Canonical status compatibility and retry approval | `CanonicalStatusShouldUseFoldedExecutionAndProjectionAsAuthority` covers all execution states. `CanonicalRetryShouldRemainClosedUntilProfileApprovalWhileLegacySchedulingRemainsCompatible` proves synthetic fold eligibility cannot grant public retry permission. Canonical scheduling metadata is absent; injected scheduling/reason/action drift rejects. |
| 5. Private transition actor | `CanonicalStatusShouldExposeCompleteSafeFactsAndKeepProjectionPendingSeparateFromCommit` retains the actor internally and asserts public omission. OpenAPI/client tests reject an exported `actorId`; NSwag output and hash fixture were refreshed. |
| 6. Accepted identity across surfaces | CLI/MCP accepted-command tests use OperationId different from TaskId; `GovernedOperationServiceTests.SubmitGovernedNoteShouldReadOutcomeBackThroughOperationStatusKeyedByAcceptedOperationId` pins UI status/audit reads. All three surfaces cover historical fallback. |
| 7. Linked-attempt eligibility | `ReceiveShouldRejectPredecessorIdentityOutsideEligibleRetry`; status validation requires predecessor and prior outcome together; existing eligible retry and predecessor-outcome tests still pass. |
| 8. Legacy monotonicity evidence | `LegacyOlderWritesShouldPreserveNewerStatusAndRejectIdentityDrift` covers both stores. The initial-write replay test now includes advancing-clock rows and directly asserts audit status. |
| 9. Collision-read throw/cancellation | `CollisionLookupFailureShouldReturnUnavailableWithoutReservationAndAllowLaterRetry` asserts 503, zero effects/reservations, and a later successful retry. The relocated read needs no reservation cleanup. |
| 10. Canonical refusal precedence/HTTP | `CanonicalGuardShouldPreserveAuthorizationDenialAuditAndCounterPrecedence` and the canonical HTTP test cover the denial audit/counter and allowed-call refusal without reservation/audit/dispatch. |
| 11. Metadata validation | `StatusMetadataShouldRejectImplicitLegacyUpgradeAndUnnormalizedCanonicalIdentities` covers both stores, malformed/unnormalized predecessors, normalized operation/correlation identity, absent legacy metadata, and safe HTTP suppression. |
| 12. Strict new dispatch preparation | `NewDurableDispatchPreparationShouldRequireCallerCommandOperationIdentity` rejects TaskId preparation while retaining Reserved state. Current preparation fixtures use CommandId; explicit historical task-based receipt fixtures remain unchanged in meaning. |
| 13. Guard token bounds | `CanonicalDecisionSlotShouldRejectTokensOutsidePublishedBounds` covers spaces, slash, 241 characters, number and null; `OpenApiContractSpineTests` pins the schema pattern and maximum length. |
| 14. Nullable guard serialization | `ImmutableContractsShouldRoundTripWithoutDefaultingMissingOrNegativeRevision` now asserts JSON null serialization under general/web defaults. |
| 15. Projection null guard | `ProjectShouldRejectNullStateBeforeExaminingRevision` asserts the same argument guard as the other reducer entry points. |

Build and current-run results:

- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed, zero warnings/errors; log `/tmp/story-1-5-current-build.log`. After the final fixture corrections, `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` also passed, zero warnings/errors; log `/tmp/story-1-5-current-server-build.log`.
- Each full lane ran with `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5-current/<lane>.ctrf.json -result-trx TestResults/story-1-5-current/<lane>.trx`. Full passing lanes: Contracts **521**, Client **94**, Server **2854**, Cli **60**, Mcp **70**; zero failures/skips. Logs are `/tmp/story-1-5-current-<lane>-tests.log`.
- Focused runs retained positive CTRF/TRX at `TestResults/story-1-5-current/{server-focused,architecture-focused,conformance-focused,ui-focused}.{ctrf.json,trx}`: Server **642**, Architecture **13**, Conformance **22**, UI **10**, all zero failures/skips. Server selected `CommandGatewayTests`, `CommandOperationContractTests`, `OperationStatusMonotonicityTests`, `CommandOperationConcurrencyAdapterTests`, and `CommandGatewayAdmissionApiE2ETests`. Conformance selected contract drift, success parity and contract-spine oracle classes. UI selected governed-operation methods and both locale catalog cases. Architecture selected dependency direction, trusted authority, all three adapter boundaries, competing-wire-DTO and UTC guards.
- All 16 methods named in the Matrix evidence and following acceptance paragraph resolve to passing current-run Server/Client test cases, including cross-tenant/missing parity, actual status races, immutable replay, legal/illegal transitions and terminal retry eligibility. The complete Server run retains durable fencing, trusted-authority and real correction status-sink regressions.

The required broader lanes were run without exclusions and remain **failing** on unchanged paths:

```bash
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests TestResults/story-1-5-current/architecture.ctrf.json -result-trx TestResults/story-1-5-current/architecture.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests TestResults/story-1-5-current/conformance.ctrf.json -result-trx TestResults/story-1-5-current/conformance.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests TestResults/story-1-5-current/ui.ctrf.json -result-trx TestResults/story-1-5-current/ui.trx
```

| Broader lane | Exact failing test(s) and result |
| --- | --- |
| Architecture: 121 passed, 2 failed, 0 skipped; exit 1 | `ScaffoldArchitectureTests.RootDeclaredSiblingPackageWrappersShouldDelegateVersionsToSharedBuildsCatalog`: ten local version declarations in the EventStore, Parties and Works wrappers. `LiveRecoveryValidationArchitectureTests.RunSettingsIsAcceptedByTheTestPlatform`: its hard-coded Release child probe uses stale xUnit `4.0.1`, rejected by shared catalog `4.0.2` even though the probe test itself passed. |
| Conformance: 150 passed, 1 failed, 0 skipped; exit 1 | `DaprAccessControlConformanceTests.EventStorePolicyMustGrantEveryMappedSdkPostRoute`: the unchanged production ACL lacks `/project/rebuild/reclaim-guarded/v1`, `/project/rebuild/reclaim-recovery/v1`, `/project/source-guard/applied-receipt/v1`, `/project/source-guard/owner-proof/v1`, and `/project/source-guard/reconcile-applied-receipt/v1` from the current SDK route surface. |
| UI: 495 passed, 4 failed, 0 skipped; exit 1 | `ChatBotAccessibilityFocusContractTests.PackagePinsShouldMatchApprovedSharedCatalogForAccessibilityFloor`, `ChatBotLiveRegionReducedMotionContractTests.PackagePinsShouldMatchApprovedSharedCatalogForLiveRegionAndMotionFoundation`, `ChatBotLocalizationContractTests.PackagePinsShouldMatchApprovedSharedCatalogForLocalizationFoundation`, and `ChatBotResponsiveTouchContractTests.PackagePinsShouldMatchApprovedSharedCatalog`: shared Fluent `5.0.1` differs from the existing asserted `5.0.0`. |

Unchanged-path evidence: the following read-only command returned exit 0 with no diff. No dependency, ACL, release-output or unrelated test changes were made to mask these failures:

```bash
git diff --exit-code HEAD -- Directory.Packages.props src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs tests/Hexalith.ChatBot.Conformance.Tests/DaprAccessControlConformanceTests.cs tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs tests/Hexalith.ChatBot.UI.Tests/ChatBotLiveRegionReducedMotionContractTests.cs tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs references/Hexalith.Builds references/Hexalith.EventStore references/Hexalith.Parties references/Hexalith.Works
```

Runtime evidence remains unavailable: `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` remained pending after “Starting Aspire AppHost in the background...”; exact-target `aspire describe --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json` reported no running host. Exact-target stop also reported not running; the pending CLI was canceled with Ctrl-C (exit 0, “Stopping Aspire”), and final describe again found no ChatBot host. The unrelated FrontComposer host was left untouched. Synthetic evidence does not establish native persisted end-state, owner atomicity or acceptance of any open gate. Local result files are current-run evidence, not completion provenance. Whitespace validation uses `git -c core.whitespace=cr-at-eol diff --check`; the frozen intent remains byte-identical to HEAD.

### Parent post-review verification (2026-10-17)

- Fixed review groups BH2/BH3, BH4, BH11/VG1 and EC1. The converter now rejects unknown/duplicate members with consistent JsonException failures; operation classes satisfy the published token bounds; caller cancellation propagates; transition correlation IDs normalize before folding/replay. The malformed-class HTTP fallback suppresses the rejected value.
- Focused patch evidence: 79 Server cases passed with zero failures/skips at `TestResults/story-1-5-round2/server-focused.{ctrf.json,trx}`.
- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed with zero warnings/errors. Log: `/tmp/story-1-5-post-review-build.log`.
- Full passing lanes after all code changes: Contracts 521, Client 94, Server 2862, Cli 60, Mcp 70: **3607 passed**, zero failures/skips. Each used `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5-post-review/<lane>.ctrf.json -result-trx TestResults/story-1-5-post-review/<lane>.trx`. Logs: `/tmp/story-1-5-post-review-<lane>-tests.log`.
- Parent TRX audit verified all 16 matrix/acceptance methods and all five new review-regression methods above resolve to passing cases in these final results.
- The same seven broader failures remain: Architecture 121 passed/2 failed, Conformance 150 passed/1 failed, UI 495 passed/4 failed; all zero skips, each command exited 1. Failure names, causes and unchanged-path evidence are recorded in the preceding table. The final exact commands are:

```bash
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests TestResults/story-1-5-post-review/architecture.ctrf.json -result-trx TestResults/story-1-5-post-review/architecture.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests TestResults/story-1-5-post-review/conformance.ctrf.json -result-trx TestResults/story-1-5-post-review/conformance.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests TestResults/story-1-5-post-review/ui.ctrf.json -result-trx TestResults/story-1-5-post-review/ui.trx
```

The build workflow halted at step-04 because required broader verification still fails. The spec remains in-review, sprint tracking remains in-progress, and no commit, completion transition, or completion attestation is proposed. The approved frozen intent and original baseline_commit are preserved. A6/A13, retry-profile approval, native owner/persistence acceptance and the explicit deferrals remain open.

### Resumed review-patch verification (2026-10-17)

All three accepted patch groups (R3B3, R3B8, R3V1) are implemented. Root guard uniqueness rejects null/malformed-first, null/malformed-last, differing and identical duplicate objects. The client fixture now represents the second attempt of a committed, projection-lagging operation with its matching prior failed outcome, consistent attempt ceiling, terminal reason and empty partial-output marker. Replay/retry tests independently vary tenant, actor, resources and operation class and prove the original fold remains unchanged.

- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed after all code changes: zero warnings/errors. Log: `/tmp/story-1-5-resume-final-build.log`.
- Focused native runner evidence retained under `TestResults/story-1-5-resume/patch-{server,client}.{ctrf.json,trx}`: Server **10** cases and Client **1**, all passed with zero failures/skips. Server selected `-method '*DuplicateRootCanonicalGuardsShouldRejectBeforeAdmission' -method '*ReplayAndRetryShouldRejectChangedIdentityBindings'`; Client selected `-method '*CanonicalStatusShouldDecodeAllSafeFactsWithoutConfusingProjectionLagWithCompletion'`.
- Final full lanes each used `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5-resume-final/<lane>.ctrf.json -result-trx TestResults/story-1-5-resume-final/<lane>.trx`. Logs: `/tmp/story-1-5-resume-final-<lane>-tests.log`.

| Final lane | Passed | Failed | Skipped | Exit |
| --- | ---: | ---: | ---: | ---: |
| Contracts | 521 | 0 | 0 | 0 |
| Client | 94 | 0 | 0 | 0 |
| Server | 2872 | 0 | 0 | 0 |
| Cli | 60 | 0 | 0 | 0 |
| Mcp | 70 | 0 | 0 | 0 |
| Architecture | 121 | 2 | 0 | 1 |
| Conformance | 150 | 1 | 0 | 1 |
| UI | 495 | 4 | 0 | 1 |

Total: **4383 passed, 7 failed, 0 skipped**. Contracts/Client/Server/Cli/Mcp account for **3617** passing cases. A parent TRX audit resolved all 16 Matrix evidence/acceptance methods and all three patch methods to passing final-run cases. No test exclusions or gate changes were introduced.

The broader failure names exactly match the pre-patch run and the previously recorded table: sibling package-version authorities, the stale Release xUnit child probe, five missing production ACL routes, and four Fluent 5.0.0 assertions against the shared 5.0.1 catalog. These exact final commands each exited 1:

```bash
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests TestResults/story-1-5-resume-final/architecture.ctrf.json -result-trx TestResults/story-1-5-resume-final/architecture.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests TestResults/story-1-5-resume-final/conformance.ctrf.json -result-trx TestResults/story-1-5-resume-final/conformance.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests TestResults/story-1-5-resume-final/ui.ctrf.json -result-trx TestResults/story-1-5-resume-final/ui.trx
```

The earlier exact `git diff --exit-code HEAD -- ...` unchanged-path command was rerun with the same paths and returned exit 0 with no output. No package, ACL, unrelated test or submodule changes were made. `git -c core.whitespace=cr-at-eol diff --check` passed, and the frozen intent is byte-identical to HEAD.

The current runtime attempt also remains blocked: `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` stayed at "Starting Aspire AppHost in the background...". Exact-target `aspire describe --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json` found no running host. Exact-target stop also found none; Ctrl-C canceled the pending start with exit 0, and final describe again found no host. No other AppHost was stopped.

The build workflow halts at step-04 because broader required verification remains failing. The spec remains `in-review`, sprint tracking remains `in-progress`, and no commit, completion transition or evidence attestation is proposed. A6/A13, retry-profile approval, accepted native owner/persistence evidence and all prior explicit deferrals remain open. These local results do not establish native durability or authorize canonical runtime execution.

### Fourth review-patch verification (2026-10-17)

All nine accepted corrections (R4B2-R4B8, R4E2, R4V1) are implemented. New linked-attempt creation still requires Retry, while equivalent stored attempts replay without reopening or changing the retained outcome. The replay regression covers Received, Admitted, Committed, Rejected and Failed, plus malformed guards, changed revision/input and rejected new predecessor creation. Historical recovery fixtures cover both retained intents and post-commit envelopes, command/task-based identities and both accepted timestamp spellings. They assert exact reconstructed receipt metadata in identity/domain/receipt state and the replay response, with one dispatch.

- The pre-fix focused run executed 22 Server cases: 17 passed and the five new retry-state replay cases failed with RetryNotEligible. Evidence: `TestResults/story-1-5-r4/red-server.{ctrf.json,trx}`. The final full run passes all 22, demonstrating the correction rather than merely exercising it.
- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed after all code changes with zero warnings/errors. Log: `/tmp/story-1-5-r4-final-build.log`.
- Each full lane used `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5-r4/final-<lane>.ctrf.json -result-trx TestResults/story-1-5-r4/final-<lane>.trx`. Logs: `/tmp/story-1-5-r4-final-<lane>-tests.log`.

| Final lane | Passed | Failed | Skipped | Exit |
| --- | ---: | ---: | ---: | ---: |
| Contracts | 521 | 0 | 0 | 0 |
| Client | 94 | 0 | 0 | 0 |
| Server | 2889 | 0 | 0 | 0 |
| Cli | 60 | 0 | 0 | 0 |
| Mcp | 70 | 0 | 0 | 0 |
| Architecture | 121 | 2 | 0 | 1 |
| Conformance | 150 | 1 | 0 | 1 |
| UI | 495 | 4 | 0 | 1 |

Total: **4400 passed, 7 failed, 0 skipped**. The five passing full lanes account for **3634** cases. The parent TRX audit resolved all 16 Matrix/acceptance methods and all nine patch methods to passing final cases; its diagnostic output is `TestResults/story-1-5-r4/verification-audit.json`. It is a local result audit, not completion attestation or native durability evidence. The remaining patch methods verify distinct commit/projection reasons, malformed direct facts, projection before commit, mutable arrays and nested prior outcomes, and schema/Contracts enum consistency.

The same seven broader failures remain on unchanged paths, with the exact names and causes in the preceding broader-lane table: ten sibling package-version declarations, a stale Release xUnit 4.0.1 probe against catalog 4.0.2, five missing production ACL routes, and four Fluent 5.0.0 assertions against catalog 5.0.1. These commands each exited 1:

```bash
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests TestResults/story-1-5-r4/final-architecture.ctrf.json -result-trx TestResults/story-1-5-r4/final-architecture.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests TestResults/story-1-5-r4/final-conformance.ctrf.json -result-trx TestResults/story-1-5-r4/final-conformance.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests TestResults/story-1-5-r4/final-ui.ctrf.json -result-trx TestResults/story-1-5-r4/final-ui.trx
```

The earlier exact `git diff --exit-code HEAD -- ...` unchanged-path command returned exit 0 with no output again. No dependency, ACL, unrelated-test or submodule changes were made to hide failures. Frozen intent is byte-identical to HEAD and `git -c core.whitespace=cr-at-eol diff --check` passes.

The current runtime attempt also remains blocked: `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` stayed at “Starting Aspire AppHost in the background...”. Exact-target `aspire describe --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json` found no running host. Exact-target stop also found none; Ctrl-C canceled the pending start with exit 0, and final describe again found no host. The separate FrontComposer host was left untouched.

The build workflow halts at step-04 because required verification still fails and accepted native owner/persistence evidence remains unavailable. The spec remains in-review and sprint tracking remains in-progress; no commit, completion transition or completion attestation is proposed. A6/A13, retry-profile approval and all prior explicit deferrals remain open. These results do not authorize canonical runtime execution.

### Fifth review-patch verification (2026-10-17)

The five accepted regression groups R5B6-R5B10 add **18** cases in `CommandOperationContractTests.cs`; runtime code is unchanged. They cover missing/negative/mismatched projection revisions, independently changed retry semantics and chronology, a foreign-tenant decision-slot occupant, illegal pre-effect flags on otherwise legal events, and exact/altered older-event replay after projection completes. The focused run passes all 18 with zero failures/skips at `TestResults/story-1-5-r5/patch-server.{ctrf.json,trx}`.

- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed with zero warnings/errors; log `/tmp/story-1-5-r5-patch-build.log`.
- Final `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed with zero warnings/errors; log `/tmp/story-1-5-r5-final-build.log`.
- Each full lane used `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5-r5/final-<lane>.ctrf.json -result-trx TestResults/story-1-5-r5/final-<lane>.trx`. No exclusions or skip-policy changes were introduced.

| Final lane | Passed | Failed | Skipped | Exit |
| --- | ---: | ---: | ---: | ---: |
| Contracts | 521 | 0 | 0 | 0 |
| Client | 94 | 0 | 0 | 0 |
| Server | 2906 | 1 | 0 | 1 |
| Cli | 60 | 0 | 0 | 0 |
| Mcp | 70 | 0 | 0 | 0 |
| Architecture | 121 | 2 | 0 | 1 |
| Conformance | 150 | 1 | 0 | 1 |
| UI | 495 | 4 | 0 | 1 |

Total: **4417 passed, 8 failed, 0 skipped**. All 16 Matrix/acceptance methods and all five new regression methods resolve to passing final-run cases. Their parent audit is retained at `TestResults/story-1-5-r5/verification-audit.json`, which also records the failing full-lane summaries and explicitly is not completion evidence.

The seven Architecture/Conformance/UI failures exactly match the pre-patch run and the earlier broader-lane table: ten sibling package-version declarations; the stale Release xUnit 4.0.1 child probe against catalog 4.0.2; five missing production ACL routes; and four Fluent 5.0.0 assertions against catalog 5.0.1. An additional Server failure occurred twice in full-suite runs: `AiExecutionCoordinatorTests.OwningReplicaCancellationProjectionShouldNotRunProviderCancellationCallbacksInline` times out at line 262 waiting one second for its callback. The pre-patch full Server run passed all 2889 cases. The failing method then passed alone (**1** case), and its entire class passed alone (**32** cases), with zero skips. Retained probe outputs are `server-timeout-probe` and `server-lifecycle-probe` under `TestResults/story-1-5-r5/`. The full Server rerun still failed the same test with **2906 passed, 1 failed, 0 skipped**; its results remain separate at `final-server-rerun.{ctrf.json,trx}`. Focused success does not replace this full-suite failure, and no timeout or assertion was weakened.

Exact failing final commands, each exit 1:

```bash
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests TestResults/story-1-5-r5/final-server-rerun.ctrf.json -result-trx TestResults/story-1-5-r5/final-server-rerun.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests TestResults/story-1-5-r5/final-architecture.ctrf.json -result-trx TestResults/story-1-5-r5/final-architecture.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests TestResults/story-1-5-r5/final-conformance.ctrf.json -result-trx TestResults/story-1-5-r5/final-conformance.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests TestResults/story-1-5-r5/final-ui.ctrf.json -result-trx TestResults/story-1-5-r5/final-ui.trx
```

The earlier exact unchanged-path command returned exit 0 with no diff. `git diff --exit-code HEAD -- tests/Hexalith.ChatBot.Server.Tests/Lifecycle/AiExecutionCoordinatorTests.cs src/Hexalith.ChatBot.Server/Lifecycle/AiExecution/AiExecutionCoordinator.cs` also returned exit 0. No dependency, ACL, unrelated test, timeout, submodule or runtime-code changes were made to mask these failures.

The current native-runtime attempt reached a concrete startup blocker: `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited **2**, reporting AppHost exit **134** because no recovery mailbox client secret was configured. The required key is `ChatBot:LiveRecoveryValidation:MailboxClientSecret` (or the existing legacy AppHost key `LiveRecoveryValidation:MailboxClientSecret`). Startup diagnostics are retained at `/home/administrator/.aspire/logs/cli_20261017T164731_401986ef.log`. Exact-target describe and stop both found no running ChatBot host; other AppHosts were left untouched. This does not establish native persisted end-state, accepted owner execution, A6/A13 acceptance or retry-profile approval.

The build workflow halts at step-04 because required full verification remains failing and native/primary evidence is unavailable. The spec remains `in-review`, sprint tracking remains `in-progress`, and no commit, completion transition or completion attestation is proposed. Frozen intent, the original baseline, all earlier explicit deferrals and runtime activation gates remain unchanged.

### Sixth review verification (2026-10-18)

The current checkout required no additional code or test patches after the three-layer review. Verification ran against `ca95a70b9237498568e87c5a95c9958f19717213`; only this spec's review/evidence narrative changes afterward. The frozen intent and original baseline remain unchanged.

- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed with zero warnings/errors. Log: `/tmp/story-1-5-r6-baseline-build.log`.
- Each lane ran separately with `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5-r6/baseline-<lane>.ctrf.json -result-trx TestResults/story-1-5-r6/baseline-<lane>.trx`. Logs: `/tmp/story-1-5-r6-baseline-<lane>-tests.log`. No tests were excluded and no gate was weakened.

| Current lane | Passed | Failed | Skipped | Exit |
| --- | ---: | ---: | ---: | ---: |
| Contracts | 521 | 0 | 0 | 0 |
| Client | 94 | 0 | 0 | 0 |
| Server | 2929 | 0 | 0 | 0 |
| Cli | 60 | 0 | 0 | 0 |
| Mcp | 70 | 0 | 0 | 0 |
| Architecture | 121 | 2 | 0 | 1 |
| Conformance | 150 | 1 | 0 | 1 |
| UI | 495 | 4 | 0 | 1 |

Total: **4440 passed, 7 failed, 0 skipped**. The five passing lanes contain **3674** cases. All 16 methods named in the Matrix evidence and acceptance paragraph resolve to passing current-run TRX cases; `TestResults/story-1-5-r6/verification-audit.json` retains that local audit and the lane summaries. It is not completion provenance or native persistence evidence. The previously observed Server callback timeout did not recur; the full current Server lane passes, including that method. This does not erase the earlier failed runs.

The seven failures match the earlier broader-lane table: ten sibling package-version declarations; a stale Release xUnit 4.0.1 child probe against catalog 4.0.2; five missing production ACL routes; and four Fluent 5.0.0 assertions against catalog 5.0.1. The exact failing commands each returned exit 1:

```bash
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests TestResults/story-1-5-r6/baseline-architecture.ctrf.json -result-trx TestResults/story-1-5-r6/baseline-architecture.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests TestResults/story-1-5-r6/baseline-conformance.ctrf.json -result-trx TestResults/story-1-5-r6/baseline-conformance.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests TestResults/story-1-5-r6/baseline-ui.ctrf.json -result-trx TestResults/story-1-5-r6/baseline-ui.trx
```

`aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` returned exit **2** after building successfully. Startup threw InvalidOperationException because `ChatBot:LiveRecoveryValidation:MailboxClientSecret` (or legacy `LiveRecoveryValidation:MailboxClientSecret`) is unconfigured. Exact-target describe before startup and exact-target stop afterward both reported no running ChatBot host. No other AppHost was stopped, no credentials were fabricated, and native owner/persisted end-state evidence remains unavailable.

The earlier exact unchanged-path check was rerun successfully, including the unrelated Server lifecycle/coordinator paths. No source, test, dependency, ACL or submodule changes were made. `git -c core.whitespace=cr-at-eol diff --check` passes and the frozen block is byte-identical to HEAD.

The workflow halts at step-04 because required full verification remains failing and native/primary evidence is unavailable. The spec remains `in-review`, sprint tracking remains `in-progress`; no commit, completion transition or completion attestation is proposed. A6/A13, retry-profile approval and every prior explicit deferral remain open.

### Seventh review verification (2026-10-20)

The resumed patch resolves Round18Build-BH-1, Build15-VG-1 and Resume-BH-10. OpenAPI now qualifies the canonical refusal after earlier admission/authorization/policy checks and explicitly retains the policy commands' retryable HTTP 503 `dependency_degraded`. The standard NSwag build target regenerated only the four SubmitCommand XML remarks; the generated-client SHA-256 fixture is current. The shared CLI formatter theory now proves a nonblank accepted OperationId wins over distinct TaskId/CommandId in text and JSON, alongside the three historical fallback rows. The lookup catch adds `cancellationToken.ThrowIfCancellationRequested()` before dependency-error translation; no policy gate or denial mapping changed.

- Red/green evidence: `CallerCancellationDuringFailedCollisionLookupShouldPropagateWithoutReservationAuditOrDispatch` failed before the source correction (no OperationCanceledException), then passed after it. Retained outputs: `TestResults/story-1-5-oct20-impl/red-server.{ctrf.json,trx}` and the passing `patch-server` result. The regression asserts the caller token and zero reservation, audit and dispatch. Existing active-caller IOException, dependency-owned cancellation, direct caller cancellation and successful-retry rows pass.
- Focused results under `TestResults/story-1-5-oct20-impl/`: Server cancellation/canonical precedence **6**, policy admission/HTTP **32**, CLI identity/fallback **4**, Client generation/freshness **27**, all passed with zero skips. The policy rows cover all eight gated commands with and without concurrency. Client generation includes isolated NSwag regeneration and hash validation. Two initial filter probes (`green-server`, `policy-server`) executed zero cases and correctly exited 1; neither is passing evidence.
- `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed with zero warnings/errors. Log: `/tmp/story-1-5-oct20-final-build.log`.
- Each full lane ran with `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5-oct20-final/<lane>.ctrf.json -result-trx TestResults/story-1-5-oct20-final/<lane>.trx`, using lowercase result names. Exact commands, exits, failure names and logs are retained in `TestResults/story-1-5-oct20-final/lane-results.json`.

| Current full lane | Passed | Failed | Skipped | Exit |
| --- | ---: | ---: | ---: | ---: |
| Contracts | 521 | 0 | 0 | 0 |
| Client | 94 | 0 | 0 | 0 |
| Server | 2931 | 0 | 0 | 0 |
| Cli | 61 | 0 | 0 | 0 |
| Architecture | 121 | 2 | 0 | 1 |
| Conformance | 150 | 1 | 0 | 1 |

Total **3878 passed, 3 failed, 0 skipped**. All 16 Matrix/acceptance methods and eight resumed-patch/control methods resolve to current passing TRX cases via test definitions, recorded in `TestResults/story-1-5-oct20-final/verification-audit.json`. This is a local result audit, not completion provenance. Unaffected UI/MCP lanes were not rerun; earlier disclosed UI package-pin failures are not claimed resolved.

Required broader checks remain failing on unchanged paths. These exact commands each exited 1:

```bash
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests TestResults/story-1-5-oct20-final/architecture.ctrf.json -result-trx TestResults/story-1-5-oct20-final/architecture.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests TestResults/story-1-5-oct20-final/conformance.ctrf.json -result-trx TestResults/story-1-5-oct20-final/conformance.trx
```

- `LiveRecoveryValidationArchitectureTests.ReleasePublication_ShouldRequireFreshMainHeadImmediatelyBeforeSemanticRelease`: the existing release job has `timeout-minutes: 45`, but the guard expects 30 (already recorded by Story 1.6 R22Build-VG-1).
- `ScaffoldArchitectureTests.RootDeclaredSiblingPackageWrappersShouldDelegateVersionsToSharedBuildsCatalog`: ten local version declarations remain in the EventStore/Parties/Works wrappers.
- `DaprAccessControlConformanceTests.EventStorePolicyMustGrantEveryMappedSdkPostRoute`: the existing production ACL lacks `/project/rebuild/reclaim-guarded/v1`, `/project/rebuild/reclaim-recovery/v1`, `/project/source-guard/applied-receipt/v1`, `/project/source-guard/owner-proof/v1`, and `/project/source-guard/reconcile-applied-receipt/v1`.

`git diff --exit-code HEAD -- .github/workflows/release.yml tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs tests/Hexalith.ChatBot.Conformance.Tests/DaprAccessControlConformanceTests.cs src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml references/Hexalith.EventStore/Directory.Packages.props references/Hexalith.Parties/Directory.Packages.props references/Hexalith.Works/Directory.Packages.props` returned exit 0 with no diff. No assertions, dependencies, submodules, ACLs or release gates were changed to mask these failures.

The required Aspire baseline was attempted before code changes with `aspire start --non-interactive --apphost /home/administrator/projects/hexalith/chatbot/src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`. Build passed, but startup returned exit **2**, AppHost exit **134**, because `ChatBot:LiveRecoveryValidation:MailboxClientSecret` (or legacy `LiveRecoveryValidation:MailboxClientSecret`) is unconfigured. Diagnostics: `/home/administrator/.aspire/logs/cli_20261020T105333_6ec9078b.log`. Exact-target `aspire describe --apphost /home/administrator/projects/hexalith/chatbot/src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json --non-interactive` reported no running ChatBot host. Native owner/persisted-end-state acceptance remains unavailable.

The frozen block is byte-identical to HEAD; `git -c core.whitespace=cr-at-eol diff --check` passes. Story 1.6's ADR, spec, `CommandGateway.cs`, `CommandGatewayAdmissionApiE2ETests.cs` and `CommandGatewayTests.cs` are unchanged. The only shared-source change is the caller-token check after the policy gate. Story 1.6 remains literal `review` in both its spec and sprint tracking.

Story 1.5 is consistently spec `in-review` / sprint `review` after the requested patches and review. Required broad verification, native owner/protection evidence, retry-profile approval and the original explicit deferrals still prevent a done transition. No commit, push, completion attestation or runtime activation is proposed by this Story 1.5 pass.

### Actionable deferral implementation verification (2026-10-20)

All four reopened tasks are implemented. The original Story 1.2 contract/client finding 7 and Loop 15 BH4 ledger entries, plus the Story 1.5 oneOf, workflow-sink, SDK serializer and R3B9 entries, retain their historical text and now include direct resolution evidence. Owner multi-event revision semantics, accepted numeric equivalence and successor uniqueness remain unresolved owner decisions; lifetime fingerprints and historical operation identities are unchanged.

- Both operation path parameters, `OperationStatus.originalOperationId` and `OperationAuditHistory.operationId` now reference the existing common `CommandId` ULID wire schema. Historical TaskId-valued identifiers remain valid. The example helper enforces exactly one matching branch; regression controls cover zero, one and two matches, as well as valid/invalid identifiers at every corrected location. NSwag ran through its normal build target and produced byte-identical C#; `tests/fixtures/hexalith-chatbot-generated-client.sha256` remains the correct checksum.
- `IOperationStatusStore.UpdateAsync` evaluates a pure callback against a defensive copy of the latest snapshot and reevaluates it after every atomic conflict. A null result skips the write; the SDK adapter exits the existing SDK transform before conditional save, preserving absent-workflow behavior. Both stores validate the addressed identity and recheck cancellation before saving. Replay checks current authority and caller cancellation inside every callback attempt. Workflow/replay stamps never regress relative to the latest snapshot; duplicate suppression preserves workflow terminal guidance, partial-output facts, original receipt identity and receipt-proven audit certainty.
- The synthetic SDK fixture now executes the production `DaprReadModelStore` with the default serializer configuration used by the production unkeyed Dapr client. Conditional state saves store serialized bytes and reads return fresh instances. Equal canonical revisions, incompatible duplicates and retained nested predecessor metadata pass through that seam. The original equivalence comparison remains unchanged because the production-seam checks show no defect. This is synthetic conditional transport evidence, not native-store durability or owner execution evidence.
- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` and the corresponding Contracts project build passed with zero warnings/errors. Logs: `/tmp/story-1-5-deferrals-server-build.log` and `/tmp/story-1-5-deferrals-contracts-build.log`.
- Focused built-runner results at `TestResults/story-1-5-deferrals/patch-{contracts,status,gateway}.{ctrf.json,trx}`: Contracts **34**, status/workflow **89**, gateway contention/recovery **16**, all passed with zero failures/skips. Each uses `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh <built-lane-runner> <ctrf-path> -result-trx <trx-path>` with class/method selectors recorded in its local log at `/tmp/story-1-5-deferrals-patch-<name>.log`. An initial combined class/method filter selected zero cases and correctly exited 1 (`patch-server`); it is not passing evidence.
- `ConcurrentDuplicateSuppressionsShouldIncrementExactlyOnceEachAndRetainTerminalWorkflowGuidance` checks count exactly two and retained terminal facts after contention in both stores. `DuplicateReplayRacingTerminalWorkflowShouldRetainBothLatestFieldsAndExactCount` forces the workflow's first write to lose against a later-stamped replay, then checks the persisted combined end-state. Each asserts three callback evaluations for two writes and, on the SDK path, one real ETag conflict. `ReplayAtomicRetryShouldRecheckCallerCancellationAndCurrentAuthorityBeforeMutation` rejects a cancelled or expired-authority replay on callback reevaluation without adding its status mutation. Existing initial-status recovery, terminal monotonicity and defensive-copy regressions pass through the same production adapter seam.

The required pre-edit Aspire baseline built successfully but startup exited **2**, AppHost exit **134**, due to the missing `ChatBot:LiveRecoveryValidation:MailboxClientSecret` (legacy key also absent). Logs: `/tmp/story-1-5-deferrals-aspire-start.log` and `/home/administrator/.aspire/logs/cli_20261020T113120_2f860853.log`. This remains separate from broad-lane failures and from open A6/A13, retry-profile and native owner/persistence acceptance. No native completion or runtime activation is claimed. Final full verification and review are recorded separately; Story 1.5 remains `in-progress` during this implementation handoff.

### Actionable deferral parent verification (2026-10-20)

After the R8B2/R8E1 review correction, the required `dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` passed with zero warnings/errors. Log: `/tmp/story-1-5-deferrals-final-build.log`.

Every full lane ran individually with `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5-deferrals-final/<lane>.ctrf.json -result-trx TestResults/story-1-5-deferrals-final/<lane>.trx`, with lowercase result names. Exact commands, exits, failure names and logs are retained in `TestResults/story-1-5-deferrals-final/lane-results.json`.

| Full lane | Passed | Failed | Skipped | Exit |
| --- | ---: | ---: | ---: | ---: |
| Contracts | 531 | 0 | 0 | 0 |
| Client | 94 | 0 | 0 | 0 |
| Server | 2954 | 0 | 0 | 0 |
| Cli | 61 | 0 | 0 | 0 |
| Architecture | 121 | 2 | 0 | 1 |
| Conformance | 150 | 1 | 0 | 1 |

Total **3911 passed, 3 failed, 0 skipped**. `TestResults/story-1-5-deferrals-final/verification-audit.json` resolves all 16 matrix/acceptance methods, eight carried-patch/policy controls and twelve actionable-deferral/review-control methods to current passing TRX results through their test definitions. This is a local audit, not completion provenance or native durability evidence. Unaffected UI/MCP lanes were not rerun; their older disclosures are not claimed resolved.

The two Architecture failures and one Conformance failure remain separate, unchanged broader blockers. The exact Architecture and Conformance commands in `lane-results.json` each exited 1:

- `LiveRecoveryValidationArchitectureTests.ReleasePublication_ShouldRequireFreshMainHeadImmediatelyBeforeSemanticRelease`: release-job timeout is still 45 minutes while the assertion expects 30.
- `ScaffoldArchitectureTests.RootDeclaredSiblingPackageWrappersShouldDelegateVersionsToSharedBuildsCatalog`: ten local version declarations remain in EventStore/Parties/Works wrappers.
- `DaprAccessControlConformanceTests.EventStorePolicyMustGrantEveryMappedSdkPostRoute`: the production ACL still lacks `/project/rebuild/reclaim-guarded/v1`, `/project/rebuild/reclaim-recovery/v1`, `/project/source-guard/applied-receipt/v1`, `/project/source-guard/owner-proof/v1` and `/project/source-guard/reconcile-applied-receipt/v1`.

`git diff --exit-code HEAD -- .github/workflows/release.yml tests/Hexalith.ChatBot.Architecture.Tests/LiveRecoveryValidationArchitectureTests.cs tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs tests/Hexalith.ChatBot.Conformance.Tests/DaprAccessControlConformanceTests.cs src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml references/Hexalith.EventStore/Directory.Packages.props references/Hexalith.Parties/Directory.Packages.props references/Hexalith.Works/Directory.Packages.props` returned exit 0. No gates or unrelated dependencies were changed.

Separately, the pre-edit Aspire command above exited 2 after a successful build because the mailbox secret is missing (AppHost exit 134); exact-target `aspire describe --apphost /home/administrator/projects/hexalith/chatbot/src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json --non-interactive` confirms no running ChatBot host. Native persisted-end-state evidence remains unavailable.

Frozen intent is byte-identical to HEAD. Story 1.6's spec/ADR, `CommandGateway.cs`, `CommandGatewayAdmissionApiE2ETests.cs` and `CommandGatewayTests.cs` are byte-identical to HEAD. Its policy-gate placement/outcomes and authorization audit/counter controls pass in the full Server lane. The identity canonicalizer/composer and owner reducer are unchanged; numeric equivalence, multi-event revision semantics and successor uniqueness retain their original owner deferrals.

Story 1.5 remains spec `in-review` / sprint `review` after independent review. Story 1.6 stays literal `review` in both artifacts. Required broad verification, native owner/protection evidence, retry-profile approval and the completion evidence contract remain open. No done transition, completion attestation, runtime activation or additional commit/push is proposed.

R8B2/R8E1 are resolved by a same-instance completion guard inside the atomic workflow callback. A stored completion skips subsequent reports for that instance; failed workflows retain their supported restart path. `OlderWorkflowReportLosingAtomicWriteShouldPreserveCompletedProgressAndDisableRetry` forces started/retrying reports to lose their first write to a later completion through both stores, then asserts completed state, catalog reason/actions, cleared schedule/failure metadata and public retry ineligibility. It observes three callback evaluations and one SDK ETag conflict. `RetryActivityShouldPublishDueTimeAndEligibilityUntilAttemptLimit` now covers both stores, including failed-to-started recovery. The focused sink class passed **37** cases with zero failures/skips at `TestResults/story-1-5-deferrals/workflow-review.{ctrf.json,trx}`; build/test logs are `/tmp/story-1-5-deferrals-workflow-review-build.log` and `/tmp/story-1-5-deferrals-workflow-review-tests.log`. All four new race cases and both restart controls pass again in the final full Server lane.

The pre-review full run (3906 passed, 3 failed, zero skips) and its original logs are retained under `TestResults/story-1-5-deferrals-pre-review/`; the final paths above contain the post-review rerun. The fifteen review findings are individually triaged above. There are no open review patches or spec loopbacks. The new, pre-existing original-receipt/workflow merge race R8B1 is recorded separately in deferred-work; it is not covered by the three resolved deferrals. No other carried ledger entry was duplicated.

The build workflow remains at step-04 because required broader verification still fails and native owner evidence is unavailable. Both stories remain at their review stage; neither has a done/evidence-gate attestation. The completed initial carried-finding commit remains the only new commit/push in this task; resumed source, tests and records are left uncommitted for review.

## File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Cli/ChatBotCliOutputFormatter.cs`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/CommandExecutionState.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/CommandProjectionState.cs`
- `src/Hexalith.ChatBot.Contracts/Identities/ChatBotOperationId.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalogEntry.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
- `src/Hexalith.ChatBot.Contracts/Operations/CommandOperationConcurrency.cs`
- `src/Hexalith.ChatBot.Contracts/Operations/CommandOperationIdentity.cs`
- `src/Hexalith.ChatBot.Contracts/Operations/CommandOperationTransitionMetadata.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/OperationPriorOutcome.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs`
- `src/Hexalith.ChatBot.Contracts/Serialization/CommandOperationConcurrencyJsonConverter.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Mcp/ChatBotMcpResultFormatter.cs`
- `src/Hexalith.ChatBot.Server/Audit/AuditEnvelopeFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotCommandAdmissionPipeline.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemTypes.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandSubmissionContractAdapter.cs`
- `src/Hexalith.ChatBot.Server/Gateway/IChatBotProblemDetailsFactory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Idempotency/DaprCoarseIdempotencyStore.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Redaction/CoarseUserFacingRedactionStage.cs`
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
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs`
- `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`
- `tests/Hexalith.ChatBot.Client.Tests/CommandSubmissionTransportTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CommandSubmissionRequestContractDriftTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/SuccessIntentParityTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.AuthorityExpiry.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayTests.DispatchFencing.cs`
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
- `tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
- `tests/fixtures/story-1-2-contract-spine-oracle.json`
- `src/Hexalith.ChatBot.Server/Gateway/Status/IOperationStatusStore.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusUpdateSkippedException.cs`
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusWorkflowStatusSink.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/ConditionalDaprStateClient.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/RacingOperationStatusStore.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/OperationStatusWorkflowStatusSinkTests.cs`
