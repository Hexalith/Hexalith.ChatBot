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

### Review Findings — round 9 (2026-10-24, delta code review)

Code review round 9 (bmad-code-review, full mode). The reviewed delta is `68cab6e..bfc6740` limited to the 72 File List paths: 34 files, +1,087/−399, 2,481 diff lines, no line-ending-only changes. It covers `dd640b6`, which is titled `fix: story 1.6` but holds the Story 1.5 actionable-deferral resume and the R8B2/R8E1 patch, plus six Story 1.7 commits that touched Story 1.5 files. Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and the frozen intent are unchanged. Four layers ran and none failed: Blind Hunter (15 findings), Edge Case Hunter (12), Verification Gap (none) and Acceptance Auditor (6). Of the 33 raw findings, four share one root cause and became one decision; the other 29 are rejected below, several carrying earlier Story 1.5 or Story 1.7 dispositions.

- [x] [Review][Patch] R9-D1: skip stale same-instance workflow reports that would overwrite newer Failed or Retrying progress (medium) [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusWorkflowStatusSink.cs:44] — resolved from decision 2026-10-24 (option 1, narrow ordering guard). In the atomic callback, for the same `WorkflowInstanceId`, return null when the stored workflow is `Failed` and the report is `Retrying` or `Delayed`, or when both are `Retrying` and the report's retry count is lower than the stored `WorkflowRetryCount`. Keep `Started` after `Failed` as the supported restart. Add both-store contention regressions for an older `Retrying`/`Delayed` losing to `Failed` and `Retrying(1)` losing to `Retrying(3)`, asserting unchanged persisted terminal/retry fields. Finding: acceptance-auditor+blind-hunter+edge-case-hunter (R9A3, R9B1, R9E8, R9E9). `OperationStatusWorkflowStatusSink` captures `now` before `UpdateAsync` and stamps `LastUpdatedAt = max(now, existing.LastUpdatedAt)` (`OperationStatusWorkflowStatusSink.cs:68`). The legacy policy keeps the later record and gives ties to the incoming one (`OperationStatusUpdatePolicy.cs:45`), so a report that loses CAS and retries against a newer snapshot always wins. Before `dd640b6` the sink wrote `LastUpdatedAt = now` and the same policy discarded the older report. The R8B2/R8E1 guard (`OperationStatusWorkflowStatusSink.cs:44-49`) covers only a same-instance `Completed`. Verified outcomes: an older `Started`/`Retrying` landing over a newer `Failed` clears `TerminalReasonCode`, returns `CompletionStatus` to `AcceptedProjectionPending`, restores `NextRetryAt` and retry-later guidance (public `retryEligible` true again); an older `Retrying(1)` landing over `Retrying(3)` moves `WorkflowRetryCount` and `RetryCount` backwards. `DaprCorrectionPropagationCoordinator` schedules first and reports `Started(0)` afterwards (`DaprCorrectionPropagationCoordinator.cs:45-57`), so a running workflow's own reports can precede it. Unverified member R9E8: an older instance's report after a newer instance completes; instance IDs are deterministic per tenant, association, correction and source version (`DaprCorrectionPropagationCoordinator.cs:76`), and whether one operation can carry two instances is unsettled. A plain timestamp skip would break `DuplicateReplayRacingTerminalWorkflowShouldRetainBothLatestFieldsAndExactCount`, and a same-instance `Started` after `Failed` is the supported restart (`OperationStatusWorkflowStatusSinkTests.cs:379-386`), so the ordering rule needs a choice. Live reach is blocked today by Story 1.7's unconditional atomic refusal (no new correction operations are admitted); it becomes live with Story 1.8.
- [x] [Review][Defer] R9-D1 residual: stale same-instance `Started`/`Delayed` reports, the coordinator's post-schedule `Started(0)` and cross-instance reports (R9E8) still order by stamp [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusWorkflowStatusSink.cs:68] — deferred: a stale `Started` after `Failed` is indistinguishable from the supported restart, and `Started`/`Delayed`/`Completed` always report retry count 0, so closing these needs a per-instance report sequence (new persisted field and workflow/activity inputs). Live-unreachable while Story 1.7 refuses every admission; belongs with the R8B1/R8B3 workflow/receipt ordering follow-ups before Story 1.8 admits correction operations.

Applied 2026-10-24 by the user's "apply every patch". `OperationStatusWorkflowStatusSink` now returns null inside the atomic callback for the same instance when the stored workflow is `Completed`, when it is `Failed` and the report is `Retrying` or `Delayed`, or when both are `Retrying` and the report's count is lower; `Started` after `Failed` remains the restart path. The runner's report order makes both new rules safe: `Failed` is reported only immediately before the workflow throws (`CorrectionPropagationWorkflowRunner.cs`), case-resolution `Retrying` counts only rise, and store-phase `Retrying(0)` always follows `Started(0)`. New both-store contention tests `OlderRetryingOrDelayedReportLosingAtomicWriteShouldPreserveFailedWorkflow` (4 cases) and `LowerRetryingReportLosingAtomicWriteShouldNotRewindRetryProgress` (2 cases) force the older report to lose its first write. Run against the unpatched sink, all six fail on the defect (`WorkflowStatus` `retrying`/`delayed` instead of `failed`; `WorkflowRetryCount` 1 instead of 3); with the patch they pass. Evidence under `TestResults/story-1-5-r9/`: Debug Server.Tests build with `-p:HexalithBuildsRoot=references/Hexalith.Builds`, 0 warnings and 0 errors; `focused-classes` 100/100 (sink and monotonicity classes, including the R8B2 and failed-restart cases); `focused-gateway` 12/12 (duplicate-suppression, replay-versus-workflow and replay-authority races); `mutation.ctrf.json` 0/6 on the unpatched sink; full `server-full` lane 3,257 passed, 7 failed, 0 skipped. The 7 failures are all `FoldersFolderStoreTests`, thrown by `expectedFileVersion` validation in the Folders client source at the recorded submodule pointer (`references/Hexalith.Folders/src/Hexalith.Folders.Client/Convenience/FileUpload.cs:192`); they share no code with the sink and are not caused by this patch. Story 1.5 stays spec `in-review` / sprint `review`: the R9-D1 residual and earlier explicit deferrals, native owner/protection evidence and the completion evidence contract remain open.

Rejected:

- R9A1 `false` — Removing the explicit `Concurrency is not null` refusal does not admit canonical requests. The only production `ValidateAtomicParticipationAsync` refuses every profile (`ChatBotAdmissionEvidenceValidator.cs:372-378`), and Story 1.7's universal order deliberately moved concurrency into operation/profile validation against accepted owner proof (`:85-105`, `:220-241`). Opening that gate is the A6/A13 acceptance itself, owned by Story 1.8 (D8/D9).
- R9A2 `low` — Historical replay is unreachable behind the atomic gate. This carries the Story 1.7 round-1 rejection (a read-only replay path before the gate is new design); the remaining fix, reconciling this spec's Implementation Notes and matrix evidence, edits the spec under review.
- R9A4 `low` — A stale expected revision returns 400 `command_contract_invalid`, and the live path never reads the decision slot. Unreachable until owner evidence exists; carries the Story 1.7 round-1/CR5-E13 contract-decision rejection.
- R9A5 `low` — `RevalidateMarkedCommitAsync` rebuilds the request without `Concurrency`. Masked while atomic participation refuses and markers never reach `/process`; covered by the Story 1.7 D8 deferral to Story 1.8.
- R9A6 `low` — `dd640b6` carries Story 1.5 work under a `fix: story 1.6` title although this spec's records say the resume was left uncommitted. The commit is on `origin/main`; the only fixes are rewriting pushed history or editing the spec under review.
- R9B2 `low` — The SDK store's 3-attempt retry budget differs from the in-memory unbounded CAS loop, but the pre-delta `UpsertAsync` already used the same `ReadModelWritePolicy` budget. Exhaustion needs three or more concurrent writers on one operation status, replay is live-unreachable behind the atomic gate, and handling it adds branches.
- R9B3 `false` — Dropping `CorrelationId` from `ReadModelWriteContext` changes nothing: `ReadModelWritePolicy` reads it only inside `if (logger is not null)`, and this store has never passed a logger.
- R9B4 `false` — Carries CR3-B1/CR4-E1: the pipeline's classification block returns `classifier-indeterminate` for recordless, indeterminate or undeclared-subtype input before the approval gate runs, so the gate's `Blocked(classifier-indeterminate)` never surfaces as 403.
- R9B5 `false` — Recording nothing for a 422 follows Story 1.7's frozen matrix (no durable idempotency on classifier failure); successor/link derivation is the Story 1.7 D5 deferral.
- R9B6 `false` — Carries the Story 1.7 round-2 rejection: the UI and catalog copy give the same remediation.
- R9B7 `low` — The inline 422, the generic 409 and the client fixture's free text are cosmetic; carries the Story 1.7 round-1 rejection.
- R9B8 `false` — Carries the Story 1.7 round-1 rejection: the description already says every mutation and bookkeeping write fails closed with 503, and 202 remains the declared success contract.
- R9B9 `low` — `RevalidateMarkedCommitAsync` coverage, its unguarded parse and its missing grant, evidence and envelope inputs are masked while atomic participation refuses. The parse was rejected `false` in Story 1.7 round 1; coherence is the D8 deferral.
- R9B10 `false` — The rewritten 422/409 bodies come only from catalog constants plus correlation and task IDs, through `CreateAdmissionFailure` and the redaction stage. No request content can reach them, and the factory test pins both producers.
- R9B11 `low` — The production classifier is deterministic and in-process, and qualification failures return indeterminate instead of throwing. Terminal mapping and no-audit are carried Story 1.7 decisions; operator logging would add new plumbing.
- R9B12 `false` — Carries the Story 1.7 round-2 rejection: no `State` consumer distinguishes denied from blocked.
- R9B13 `false` — Carries CR5-E20: the pending-approval branches stay live for historical replay.
- R9B14 `false` — Carries Story 1.7 D2 (deliberate classification exemption) and the CR4-A1 fitness pin on `RequiresClassification`.
- R9B15 `false` — `DelayedInitialOperationStatusStore` injects only the first write, which is the gateway's initial `UpsertAsync`; its test waits on `FirstWriteEntered` before replaying, so moving replay to `UpdateAsync` masks nothing. Synthetic defaults are labeled synthetic-only (Story 1.7 round 1), and the test-name nit is cosmetic.
- R9E1 `low` — A malformed decision payload throwing in the approval gate is pre-existing: the pre-delta pipeline (`68cab6e`) already called `approvalGate.EvaluateAsync` with the same `ReadDecisionCommand`. Typed adapters never send one, and guarding it adds branches.
- R9E2 `false` — For `ExecuteLowRiskAIAssistance` the gate returns only `Blocked`, `RoutedToApproval` or `AllowedLowRiskExecution` (`AiActionApprovalGate.cs:66-119`), so the dispatcher never meets an implicit `Approved`.
- R9E3 `false` — Carries the Story 1.7 round-1 rejection of the unguarded `JsonDocument.Parse`.
- R9E4 `low` — Same defect as R9A5 (D8).
- R9E5 `low` — Revalidation skips `SetAdmissionEnvelope`, but the production atomic validator never reads the envelope; masked and covered by D8/D9.
- R9E6 `low` — The hard-coded `Received→Admitted` envelope is the Story 1.7 D9 placeholder-envelope deferral to Story 1.8.
- R9E7 `low` — Same as R9B2.
- R9E10 `low` — Carries Story 1.7 round-1/CR3-E8: the retry-later context-package mismatch code is unreachable and a contract decision.
- R9E11 `false` — Same as R9A1.
- R9E12 `low` — Same as R9A6.

### Review Findings — round 10 (2026-10-30, delta code review)

Code review round 10 (bmad-code-review, full mode). The reviewed delta is `git diff --ignore-cr-at-eol bfc6740..HEAD`, limited to the 72 non-tracking File List paths: 6 files, +131/−35, 246 diff lines. It covers two commits. `b92e509` is the round-9 R9-D1 patch, which no review had examined yet. `9a032f4` contributes its overlapping `GovernedOperationService`, resx and service-test hunks; that commit belongs to `spec-five-deferred-ui-presentation-lifecycle.md`. `9a032f4` also converted both `SharedResource` files and `GovernedOperationService.cs` from LF to CRLF; that whole-file churn is excluded. Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and the frozen intent are unchanged. Four layers ran and none failed: Blind Hunter (11 findings, R10B1–B11), Edge Case Hunter (8, R10E1–E8), Verification Gap (one gap plus one other finding, R10V1–V2) and Acceptance Auditor (8, R10A1–A8). Of the 29 raw findings, ten share one pre-existing root cause and became one deferral. Five became four patches, one carries an existing ledger entry, and 13 are rejected below.

- [x] [Review][Patch] R10-P1: narrow the sink comment to the ordering the guard enforces (low) [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusWorkflowStatusSink.cs:44] — The comment says a report that lost the atomic write "must not reopen a failure or rewind retry progress". The guard only skips three same-instance cases: `Completed` stored, `Retrying`/`Delayed` over `Failed`, and a lower `Retrying` count over `Retrying`. A stale `Started` still reopens `Failed`, and `Retrying` over a stored `Started` still rewinds counts (R10-W1). State the enforced rules and name the deferred residual. Finding: edge-case-hunter (R10E8).
- [x] [Review][Patch] R10-P2: pin that an equal-count same-instance `Retrying` report still applies (low) [tests/Hexalith.ChatBot.Server.Tests/Lifecycle/OperationStatusWorkflowStatusSinkTests.cs:502] — Add a theory over both stores that sends two sequential same-instance `Retrying(0)` reports due at T1, then T2. This is the pending-store poll after a swallowed `Started` publication (`CorrectionPropagationWorkflowRunner.cs:47`, `:137-160`). Assert persisted `NextRetryAt == T2`, an advanced `LastUpdatedAt` and `retryEligible` at T2 through `OperationStatusHttpResults.ToJsonElement`. Today, a `<` → `<=` mutation at sink line 50 fails only the `CallbackCount`/`ConditionalConflicts` interaction counts. Finding: verification-gap (R10V1).
- [x] [Review][Patch] R10-P3: correct the stale redaction comment in the audit-history service test (low) [tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs:93] — The comment still says the history is "rendered as a redacted, metadata-only line". The service now maps structured `OperationAuditHistoryModel` metadata, and the negative privacy assertions moved to `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedAuditPresentationRenderTests.cs`. Describe the structured mapping and name where redaction is proven. Finding: blind-hunter+acceptance-auditor (R10B5, R10A5).
- [x] [Review][Patch] R10-P4: align the French audit origin label with the established term (low) [src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx:1126] — `AuditHistory_Origin_Label` translates "Surface origin" as "Surface d’origine". The existing `SurfaceOrigin_Label`, used by the conversation review history and decision items, says "Origine de surface". Use the established wording and update the `fr` row at `GovernedAuditPresentationRenderTests.cs:33`. Finding: blind-hunter (R10B8).
- [x] [Review][Defer] R10-W1: stale or reordered same-instance workflow reports outside the R9-D1 guard still rewind or reopen operation status (medium) [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusWorkflowStatusSink.cs:46] — deferred: pre-existing since `dd640b6`; `b92e509` only adds skips. Findings: blind-hunter+edge-case-hunter+acceptance-auditor (R10B1–B4, R10E1–E5, R10A1).
  - Reopened terminal states: a stale `Started(n)` after `Failed(5)` reopens the failure. It clears `TerminalReasonCode`, returns `CompletionStatus` to `AcceptedProjectionPending` and replaces the escalate guidance. A stale `Retrying` or `Started` after the instance's final `Delayed` permanently replaces the escalation reason and next action.
  - Counts moved backwards: a stale `Retrying(m)` over a stored `Started(k)` with k > m, a stale equal-count store-phase `Retrying(0)`, and the coordinator's post-schedule `Started(0)` over `Retrying(n)` all move `WorkflowRetryCount`, `RetryCount` or `WorkflowStatus` backwards.
  - Not public: `NextRetryAt` rewinds never reach the public `retryEligible`. `OperationStatusQueryHandler.cs:43-54` clears `NextRetryAt` unless the workflow runtime's authoritative progress matches status, count and due time.
  - Reach: the workflow awaits every report activity (`CorrectionPropagationWorkflowRunner.cs:139-146`, `:155`), so reports from one run cannot overlap. Staleness needs the coordinator's out-of-workflow `Started(0)` (`DaprCorrectionPropagationCoordinator.cs:45-57`) or an at-least-once duplicate activity execution. Live-unreachable while Story 1.7 refuses every admission.
  - Round-9 Delayed cases: these tests replay a cross-run sequence, since `Failed` throws before any `Delayed` in one run, and they pass a `retryDueAt` that the sink ignores for `Delayed`. Harmless defensive coverage.
  - Correction to the round-9 residual's premise: the runner's post-timer `Started` carries the case-resolution retry count 1–4 (`CorrectionPropagationWorkflowRunner.cs:143-146`). Only the coordinator, the Start activity and store-phase polling send `Started(0)`. A count rule could therefore skip a stale case-resolution `Started(n≥1)` after `Failed` without a sequence field. Combined with the round-9 `Retrying`-over-`Failed` skip, though, it would also drop a restarted run's reports until its first `Started(0)` lands (rejected R10E6/R10A2). The rule choice therefore belongs with the per-instance ordering follow-up before Story 1.8 admits correction operations.
- [x] [Review][Defer] R10-W2: the UI E2E no-browser fallback still asserts the duplicate-suppression literal from before `dd640b6` (medium) [tests/Hexalith.ChatBot.UI.E2E.Tests/DuplicateRetryFailureStatesE2ETests.cs:352] — deferred: pre-existing outside this delta and already in the ledger (Story 1.6 round-29 entry, owner Story 1.5 / UI E2E). Re-verified at HEAD: `ChatBotCommandAdmissionPipeline.cs:469` unions `duplicate_suppressed` with prior codes, and the literal `PartialOutputCodes = ["duplicate_suppressed"]` occurs nowhere in the pipeline. The source-scan assertion therefore fails whenever the browser fallback runs. It is a one-line test correction that keeps the production union. Finding: acceptance-auditor (R10A8). No new ledger entry.

Applied 2026-10-30 by the user's "apply every patch". R10-P1 rewrites the sink comment to name the enforced skips and the deferred residual. R10-P2 adds `EqualCountRetryingReportShouldAdvanceRetryDueTime` for the in-memory and SDK stores. R10-P3 rewrites the service-test comment. R10-P4 sets the French `AuditHistory_Origin_Label` to "Origine de surface" and updates the render-test row. Evidence is under `TestResults/story-1-5-r10/`. Builds ran through `python3 .github/scripts/with-source-mapping.py dotnet build <project> --configuration Debug -m:1`; Server.Tests, UI.Tests and UI.E2E.Tests each built with 0 warnings and 0 errors. `server-sink` passed 45/45: the 43 existing sink cases plus both new ones. `mutation-equal-count` passed 0/2 with the guard mutated from `<` to `<=`: persisted `NextRetryAt` stays at T1 (10:00:30) instead of T2 (10:01:00). The mutation was reverted, and the restored build re-ran 45/45. `ui-e2e-audit-render` passed 15/15 (`GovernedAuditPresentationRenderTests`, en/fr labels). `ui-full` passed 606/610. The four failures are the pre-existing `PackagePinsShouldMatchApprovedSharedCatalog*` assertions (`Microsoft.Playwright` evaluated 1.64.1, expected 1.64.0); `spec-five-deferred-ui-presentation-lifecycle.md` discloses them, and they are unrelated to these edits. Story 1.5 stays spec `in-review` / sprint `review`. R10-W1, R10-W2, the earlier explicit deferrals, native owner/protection evidence and the completion evidence contract remain open.

Rejected:

- R10B6 `low` — The page can show two audit statuses, but both come from the same `OperationStatusRecord.AuditStatus`: `OperationAuditHistoryQueryHandler.cs:48` passes `record.AuditStatus`. They differ only when the record changes between the status read and the audit-history read, and the removed history line already rendered `audit:{auditHistory.AuditStatus}`. A single audit-status source is a presentation design change, and governed audit history is live-unreachable while Story 1.7 refuses admissions.
- R10A4 `low` — Same as R10B6.
- R10B7 `low` — Audit entries reuse `ReviewHistory_Decision_Label` ("Review decision"). The wording is cosmetic. The fix adds a localization key across both resx files, `ChatBotUiTextKey`, the component and the exact-string render tests, for a section that is live-unreachable today.
- R10B9 `low` — Phase and origin enums render as C# member names (`PostCommit`, `Ui`). This predates the delta: the removed formatter interpolated the same enums, and the render tests pin the names. Mapping them to wire names adds code for a live-unreachable section.
- R10B10 `false` — The audit-history handler loads the record by the requested `OperationId` and returns that record's id with envelopes keyed by its `CommandId` (`OperationAuditHistoryQueryHandler.cs:37-48`). A foreign operation's history would need a server defect.
- R10B11 `false` — The round-10 scope was chosen deliberately as the File List paths. The coupled `9a032f4` files belong to `spec-five-deferred-ui-presentation-lifecycle.md`, and listing them would edit this spec's File List.
- R10E6 `low` — The round-9 `Failed` rule now skips a restarted run's first `Retrying(n)` when it lands before the coordinator's post-schedule `Started(0)`. That needs at least three activity round trips to beat one sink write, and the run's next `Started(n)` restores progress within one 30-second timer. The restart path is live-unreachable, and a fix needs the per-instance ordering owned by R10-W1.
- R10A2 `low` — Same as R10E6.
- R10E7 `low` — The round-9 residual's claim that "`Started`/`Delayed`/`Completed` always report retry count 0" is wrong for the runner's post-timer `Started`. Correcting the round-9 text edits the spec under review; the corrected fact is recorded with R10-W1.
- R10A3 `low` — No sink test sends a report from a different instance after `Failed`, `Completed` or a higher `Retrying`. The instance scope matters only if one operation can carry two instances, which is the unverified R9E8 owned by the residual. A test now would pin unsettled semantics.
- R10A6 `low` — The round-9 residual cites sink line 68 for the stamp, which is now line 72. Fixing it edits the spec under review; R10-W1 cites current lines.
- R10A7 `false` — An informational scope note, not a defect. The auditor itself confirms that the UI hunk keeps audit and status reads keyed by the accepted OperationId and metadata-only.
- R10V2 `low` — Static visual-foundation fixtures (`GovernedOperationsVisualFoundationE2ETests.cs:1933`, `:1999`, `:2600`) and their assertions (`:658`, `:665-666`) still show the removed line format. They never render the live page, `GovernedAuditPresentationRenderTests` covers the real presentation, and rewriting hand-written fixtures exceeds a direct correction.

### Review Findings — round 11 (2026-10-31, round-10 patch review)

Code review round 11 (bmad-code-review, full mode). The reviewed diff is the uncommitted round-10 patches, `git diff --ignore-cr-at-eol HEAD -- src tests`: 5 files, +42/−5, 101 diff lines, no line-ending-only changes. Nothing on the File List was committed after round 10 (`bfc6740..HEAD` still covers the same 6 files), so these patches were the only unreviewed code; the uncommitted spec and ledger record edits were excluded. Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and the frozen intent are unchanged. Four layers ran and none failed: Blind Hunter (8 findings, R11B1–B8), Edge Case Hunter (4, R11E1–E4), Verification Gap (no gaps) and Acceptance Auditor (4, R11A1–A4). Of the 16 raw findings, one became a decision, six became two patches, four share the R10-W1 root cause and became one carried deferral, and five are rejected below.

- [x] [Review][Patch] R11-D1: move the French "Surface origin" wording to the idiomatic "Surface d’origine" (low) [src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx:384] — resolved from decision 2026-10-31 (the recommended option: move all three values). R10-P4 aligned `AuditHistory_Origin_Label` with the established "Origine de surface", but that term is a word-for-word calque of the English; idiomatic French puts the qualifier after the noun, as in "pays d’origine". Set `SurfaceOrigin_Label` (`:384`, conversation review history and decision items) to "Surface d'origine", `AuditHistory_Origin_Label` (`:1126`) to "Surface d’origine" and the `GovernedOperations_Intro` fragment (`:161`) to "La surface d'origine", keeping each block's apostrophe style, and restore the `fr` row at `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedAuditPresentationRenderTests.cs:33`. No other test pins these strings. Finding: blind-hunter (R11B7).
- [x] [Review][Patch] R11-P1: name the deferred sink residual concretely instead of promising a sequence fix (low) [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusWorkflowStatusSink.cs:44] — The R10-P1 comment keeps "failed workflows may still intentionally restart through Started" and ends with "Other stale orderings stay deferred until reports carry a per-instance sequence". A stale `Started` is not intentional but still reopens `Failed`, and R10-W1 records that a count rule could close parts of the residual without a sequence field and leaves the rule choice open (an equal-count due-time rule is a second candidate, R11-W1). State that `Started` after `Failed` still applies as the supported restart, so a stale `Started` can reopen `Failed`, and that other stale reports can still reopen `Failed` or `Delayed` or rewind counts and due times and are tracked as deferred work (the repository's comment idiom, `PeriodicEnforcementCoordinator.cs:353`, `:372`). Findings: edge-case-hunter+acceptance-auditor+blind-hunter (R11E4, R11A2, R11B5).
- [x] [Review][Patch] R11-P2: limit the audit-history service-test comment to what the render test proves (low) [tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs:95] — The R10-P3 comment says `GovernedAuditPresentationRenderTests` proves that no command payload, tenant/resource names, secrets, paths or raw exception text reach the rendered page. Its only privacy test, `GovernedAuditShouldWithholdOmittedTransportFieldsThroughServiceAndPage` (`GovernedAuditPresentationRenderTests.cs:96-145`), seeds sentinels into the history operation id and the omitted transport fields (`ResourceId`, `ReasonCode`, `StateTransition`, `RecordedAt`) and asserts they reach neither the serialized service model nor the page. It has no tenant, exception or command-payload sentinel, and the pass-through `Decision`, `Outcome` and `CorrelationId` are tested only with safe values. Name the method and list what it proves. Findings: blind-hunter+edge-case-hunter+acceptance-auditor (R11B1, R11E3, R11A1).
- [x] [Review][Defer] R11-W1: stale same-instance workflow reports outside the R9-D1 guard (carried R10-W1, medium) [src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusWorkflowStatusSink.cs:47] — deferred: pre-existing since `dd640b6`; the reviewed diff changes only the sink comment. Members: a stale `Retrying` after the instance's final `Delayed` reopens it (R11E2), a stale equal-count `Retrying` rewinds `NextRetryAt` (R11E1, R11B4), and the frozen Status row stays unmet until R10-W1 closes (R11A3). Two facts amend the R10-W1 ledger entry.
  - Equal-count rule without a sequence: `WaitForRetryAsync` derives each due time from a history-recorded `ReadUtcAsync` plus `RemoteStatusPollDelay` (`CorrectionPropagationWorkflowRunner.cs:135`), so successive same-instance `Retrying` reports carry later due times and a duplicate activity execution repeats the same one. Skipping an equal-count `Retrying` whose `retryDueAt` is earlier than the stored `NextRetryAt` is a candidate rule for the ordering follow-up; no test covers the reverse (T2 then stale T1) order today.
  - Public effect: R10-W1's "Not public" bullet holds only against early eligibility. A rewound `NextRetryAt` no longer matches the runtime's progress, so `OperationStatusQueryHandler.cs:43-54` drops it and `retryEligible` stays false until the post-timer `Started` report, a false negative bounded by one 30-second poll window.

Applied 2026-10-31 for R11-D1 only, by the user's "apply recommended". `SharedResource.fr.resx` now reads "La surface d'origine" (`:161`), "Surface d'origine" (`:384`) and "Surface d’origine" (`:1126`); `AuditHistory_Origin_Label` and the render-test `fr` row are back to their `HEAD` values, so the round-10 R10-P4 hunks drop out of the working-tree diff. The intro still agrees with its suffix ("La surface d'origine ui est déclarée …"). Evidence is under `TestResults/story-1-5-r11/`. UI.E2E.Tests and UI.Tests built through `python3 .github/scripts/with-source-mapping.py dotnet build <project> --configuration Debug -m:1` with 0 warnings and 0 errors. `ui-e2e-audit-render` passed 15/15 (en/fr labels). `ui-full` passed 606/610; the four failures are the same `PackagePinsShouldMatchApprovedSharedCatalog*` assertions as round 10 (`Microsoft.Playwright` evaluated 1.64.1, expected 1.64.0). `ui-e2e-full` passed 288/289. The one failure, `SafeOutcomeInteractionTests.WorkspaceRetryShouldDispatchFreshReload`, also fails when its class runs alone (12/13): it mounts `ChatBotProjectConversationWorkspace` with only `ProjectId`, so the blank-title guard that FrontComposer `adc0dbe2` (2026-10-23) added to `FcPageHeader` throws, and ChatBot picked that guard up with the gitlink bump in `3e1f6e1` (2026-10-28). It is pre-existing and unrelated to these edits.

Applied 2026-10-31 for R11-P1 and R11-P2, by the user's "apply every patch". R11-P1 rewrites the sink comment (`OperationStatusWorkflowStatusSink.cs:44-47`): `Started` after `Failed` stays the supported restart, so a stale `Started` can also reopen `Failed`, and the other stale reports that can still reopen `Failed` or `Delayed` or rewind counts and due times are tracked as deferred work. R11-P2 rewrites the service-test comment (`GovernedOperationServiceTests.cs:93-97`) to name `GovernedAuditShouldWithholdOmittedTransportFieldsThroughServiceAndPage` and the sentinels it seeds. Both are comment-only, and each file keeps its line endings (sink LF, service test CRLF). Server.Tests and UI.Tests built through the source-mapping wrapper with 0 warnings and 0 errors; `server-sink` passed 45/45 and `ui-governed-service` passed 9/9 (`TestResults/story-1-5-r11/`). Story 1.5 stays spec `in-review` / sprint `review`: R11-W1 (carried R10-W1), R10-W2, the earlier explicit deferrals, native owner/protection evidence and the completion evidence contract remain open.

Rejected:

- R11B2 `false` — The first poll's write is unconditional because the seeded `Accepted` record has no `WorkflowInstanceId`, so the second poll always meets a stored same-instance `Retrying(0)`. The round-10 `mutation-equal-count` run (guard `<` → `<=`) failed 0/2 with `NextRetryAt` stuck at T1, which proves the test reaches the equal-count branch. A dropped first write would be a separate sink defect, not a blind spot of this test.
- R11B3 `false` — The test pins no new contract. The catalog declares `association_correction_propagation_pending` with next action `retry-later` (`ChatBotMessageCatalog.cs:323-328`), the sink copies that action, and `OperationStatusHttpResults.cs:66-69` derives eligibility mechanically from it. R10-P2 asked for exactly these assertions, and the live query path still drops `NextRetryAt` unless runtime progress matches.
- R11B6 `low` — Separate `AuditHistory_Origin_Label` and `SurfaceOrigin_Label` keys follow the per-context localization keys of the `9a032f4` component, owned by `spec-five-deferred-ui-presentation-lifecycle.md`. Their values now match in both cultures, and retiring one key touches `ChatBotUiTextKey`, both resx files and `ChatBotOperationAuditHistory.razor:23` for a drift nobody meets in everyday use.
- R11B8 `low` — The mixed apostrophes in `SharedResource.fr.resx` (15 lines with the curly form, 269 with the straight one) predate this diff, which removed one curly apostrophe. The curly form is correct French typography and reads the same to users; normalizing means rewriting many values and the render tests that pin them.
- R11A4 `false` — No unrun lane can hide a regression from this diff. The only production change is a comment; the one changed resx value is pinned only by `GovernedAuditPresentationRenderTests`, which ran 15/15; the two UI.E2E resx scanners (`OperationalDashboardsDegradedSurfaceE2ETests.cs:75`, `OperationalDashboardsPublishedSlosE2ETests.cs:97`) read only dashboard keys; and Contracts, Client and Architecture inputs are unchanged. Completion evidence remains the already-open evidence-contract item.

### Review Findings — round 12 (2026-11-01, delta code review)

Code review round 12 (bmad-code-review, full mode). The user confirmed the recommended delta since round 11: `git diff --submodule=short --ignore-cr-at-eol a9b9aece1dd93d47eafa3929beb0a318933ed9d2 48a1dfe2e3d5992b5cddbc717915cb0ede4af99c`, restricted to this story's implementation/test paths. Only `CommandGatewayAdmissionApiE2ETests.cs` and `ServerBootstrapApiTests.cs` changed: 2 files, +56/−35, 226 diff lines. These hunks come from `e89a3ac`, the EventStore 3.129 HTTP-host adaptation. This is not a re-review of the full original-baseline implementation. Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and frozen intent remain unchanged. Loaded context: this spec, `epic-1-context.md`, and `references/Hexalith.AI.Tools/hexalith-state-instructions.md`; authentication and lifecycle architecture were consulted during verification.

All four independent layers completed: Blind Hunter returned five findings; Edge Case Hunter returned none; Verification Gap reported no verification gaps; Acceptance Auditor reported no acceptance or constraint violations. The final Blind Hunter finding contained two independent claims, which were verified separately. Parent triage: **0 decision-needed, 3 patch, 1 defer, 2 rejected**. No review layer failed. Verification below is static; the current affected tests could not be rebuilt or executed.

- [x] [Review][Patch] **R12-P1 (medium): authenticate the case-distinct proposal-metadata regression so validation is exercised** [tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs:1472]. Finding: blind-hunter B2. `TypedFacadeShouldPreserveCaseDistinctProposalMetadataKeysAtValidationBoundary` still creates an anonymous factory and expects `401 authentication_denied`. The host now rejects before reading the body, as this delta explicitly establishes. A malformed serialized proposal therefore satisfies the existing assertion without reaching the validation it claims to test. Use the existing authenticated factory and assert the typed outcome after successful contract validation; an authentication denial must fail this regression. Retain the case-distinct dictionary input and the malformed-member validation controls. The test's source predates this delta, but its loss of validation coverage accompanies the host-boundary migration this fixture change is adapting.
- [x] [Review][Patch] **R12-P2 (low): reconcile the boundary-order test name and its live-probe explanations** [tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs:1049]. Finding: blind-hunter B4. The test still says `ShouldValidateContractBeforeAuthentication` while its new assertions and XML summary require authentication first. `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:948` and `:1111` also explicitly claim that contract-invalid requests return 400 before bearer authentication and cite this bootstrap test as their evidence. Rename the test and directly correct those explanations to authentication → contract validation → admission. Preserve the live probes' existing rule that 400/422 do not prove admission and neither 503 proves owner execution. No runtime behavior or new test branch is needed.
- [x] [Review][Patch] **R12-P3 (low): describe periodic-enforcement health as authenticated** [tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs:969]. Finding: blind-hunter B5, comment claim. The newly authenticated health test retains the statement `This endpoint is anonymous`. The production route requires `ChatBotApplicationBoundaryPolicy`; change only that description while preserving every metadata-redaction assertion. The separate claim that restoring anonymous access would escape this test is rejected below.
- [x] [Review][Defer] **R12-W1 (medium): retain the disclosed unauthenticated-command audit regression for the HTTP-host integration follow-up** [tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs:2559] — deferred: blind-hunter B1 identifies a wider host-boundary regression already acknowledged in `e89a3ac`'s commit message. With `tenantId: null`, the gateway fixture installs no synthetic principal; `ChatBotApplicationBoundaryResultHandler` returns 401 before the endpoint and never calls `IAuditWriter`. The existing test nevertheless requires one authorization-failure audit fact, including the body-derived command type, at lines 2576–2580. Those expectations cannot pass through the current handler. The root cause is the production authentication/result-handler change outside the approved two-file diff, rather than the added authenticated fixture wiring. The host follow-up must reconcile metadata-only denial auditing and its tests without reading an unauthenticated command body or merely deleting the audit requirement. Closure requires a current passing denial-audit regression and verification of the intended audit-failure behavior. No native or completion evidence is claimed. **Amended and resolved 2026-11-02 (round 15, R15-D1/R15-P3):** the user ratified a bounded denial-metadata read in place of the "without reading an unauthenticated command body" condition. The boundary reads at most 16 KiB of an `application/json` body within 250 ms and records only a canonical allowlisted `commandType`, otherwise `unknown_command`; body identity, resource and origin fields are never used. `e48e87e` restored the metadata-only boundary denial audit, R15-P1 restored the authorization-failure counter feed, and R15-P4 answers the typed dependency-unavailable 503 when the audit path fails. Evidence is in the round-15 section.

Validation attempted:

1. `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1` — exit **2** at source preparation, `HXW004`: the existing Timesheets checkout is `e540a536c8a51df1f283f537d376aa5f0ba6b45f`, while the root's staged gitlink is `4181f242bc879a2c547a5a7390692f8c7dfde6b9`. The wrapper restored the already pinned module tool 4.39.0, then refused the mismatched source graph. Log: `/tmp/chatbot-story-1-5-review-build-js78Ig.log`. The user's checkout and index were preserved.
2. Focused fallback: `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false` — exit **1**, **256 errors, 0 warnings**. The first failure is missing `Hexalith.Commons` in EventStore.Contracts; the graph also reports `MSB4019` for the absent nested `references/Hexalith.Conversations/references/Hexalith.Builds/Props/Directory.Packages.props` and unresolved Tenants contract types. Log: `/tmp/chatbot-story-1-5-review-fallback-fk8QyU.log`. No nested submodule was initialized and no dependency or gate was changed.

No current affected tests ran. The existing Server test executable predates this host change and was not presented as current evidence. Review artifacts only were changed; implementation patches await the user's workflow choice. Story 1.5 remains spec `in-review` / sprint `review`, with its existing owner/protection and completion-evidence gates open. Story 1.6 is unchanged.

Applied 2026-11-01 by the user's option 1, "Apply every patch". R12-P1 now uses `AuthenticatedFactory("tenant-alpha")` and requires HTTP 403, `ChatBotProblemTypes.CommandNotAllowlisted`, and `ChatBotMessageCodes.RefusalBlockedAction`, retaining the catalog schema assertion and both `source`/`Source` dictionary keys. The existing intended command, `AppendConversationMessage`, is unsupported by `AiActionCommandMetadataProvider`, so the classifier's rejected record reaches that specific refusal after authentication and contract validation. A 401, contract-invalid 400, or generic authorization refusal cannot satisfy the new assertions. R12-P2 renames the bootstrap test to `CommandEndpointShouldAuthenticateBeforeContractValidationAndValidateBeforeAdmission` and corrects both live mailbox-probe explanations to authentication → contract validation → admission, retaining the limits on admission/owner-execution claims. R12-P3 corrects only the periodic-enforcement health comment; its redaction assertions are unchanged. No runtime implementation or deferred finding was patched.

Post-patch validation:

- Required runtime baseline: `aspire ps --format Json --non-interactive` returned `[]`. `python3 .github/scripts/with-source-mapping.py aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited **2** at source preparation with the same Timesheets `HXW004` mismatch, before Aspire could start; log `/tmp/chatbot-story-1-5-r12-runtime-hTbfAH.log`. A final `aspire ps --format Json --non-interactive` again returned `[]`; no session needed stopping.
- `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1` exited **2**, still blocked by Timesheets `HXW004`; log `/tmp/chatbot-story-1-5-r12-patch-build-Rf9vbg.log`.
- `dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false` exited **1**, with the same **256 errors, 0 warnings**, beginning with missing `Hexalith.Commons` in EventStore.Contracts and also including the absent nested Conversations Builds import; log `/tmp/chatbot-story-1-5-r12-patch-fallback-fHzZ9O.log`.
- CRLF-aware `git diff --check` passed. Static inspection confirmed the exact refusal mapping, the removal of the obsolete boundary-order test name/explanations, unchanged production code and deferred entries, and preservation of the frozen intent and original baseline. No current test execution is claimed; old binaries were not used as passing evidence. No dependency, submodule checkout, staged entry, or shared instruction file changed.

Code-review actions: **3 patches applied, 0 decision-needed, 1 deferred, 2 rejected**. Per the review workflow's status rule, Story 1.5 is now spec/sprint `in-progress`: R12-W1 and the earlier unresolved medium findings remain, and current test/native completion evidence is unavailable. This status change does not reopen the frozen intent or imply native owner/protection acceptance. Story 1.6 remains literal `review` in its spec and sprint tracking.

Rejected:

- **R12-B3 (`low`)** — The boundary-order helper does send an invalid bearer rather than omitting the header. Both cases lack an authenticated principal; the production authorization boundary is unconditional and runs before body parsing. Headerless anonymous command denial is already covered by `CommandEndpointShouldRejectUnauthenticatedSubmissionsWithSafeProblemDetails`. No differing body-dependent behavior was demonstrated; adding another malformed-body credential matrix would add test branches for a negligible, undemonstrated distinction.
- **R12-B5a (`false`)** — The claim that restoring anonymous access to periodic-enforcement health would pass the revised test is disproved by `EventStoreDomainServiceEndpointInventory.Validate`: non-probe anonymous routes or routes without an explicit named policy are startup violations. `EventStoreDomainServiceSecurityStartupValidator.StartingAsync` throws on those violations before `CreateClient` can succeed. Its incorrect anonymous comment is handled separately as R12-P3.

### Source-recovery build review (2026-11-01)

Resumed the build workflow against preserved baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and unchanged implementation HEAD `2ac8c4b5bc87cc253c92ca60e806a86ab0948e99`. All three context-free review layers completed against `/tmp/story-1-5-unblock-baseline-i__dbizt.diff` (536 files, 6,829,185 bytes). That original-baseline diff includes separately committed UI, native Folders, contract and release work; this continuation changed no production or test source. Every finding is classified separately below. Carried dispositions retain their existing owner and are not duplicated in the ledger.

| ID | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| U1-B1 | medium | defer | Carried R12-W1: the HTTP authentication boundary returns before gateway denial auditing. The current full Server lane reproduces the expected-one/observed-zero audit failure. Retain its existing host-integration follow-up without reading an unauthenticated body or weakening the audit assertion. Resolved 2026-11-02 by `e48e87e` and round 15 (R15-P1, R15-P3, R15-P4); the bounded read was ratified as R15-D1. |
| U1-B2 | medium | defer | Carried R10-W1/R11-W1: stale same-instance Started reports can still reopen Failed or replace later retry progress. The supported restart and ordering decision remain unchanged; do not reopen the accepted narrow guard or duplicate its ledger entry. |
| U1-B3 | medium | defer | Carried R8B1: the original final receipt can overwrite workflow completion on a replay-created record. Its existing receipt-initialization/field-merge lifecycle decision remains open. |
| U1-B4 | low | rejected | Carried BH1/EC2/R4B1/R8B4: constructor slot bounds remain intentionally looser than the live adapter. The adapter enforces the published grammar and the pure reducer is unregistered; no new consumer justifies reopening that decision. |
| U1-B5 | maybe-false | defer | Acquisition validates lease metadata, but MaintainAsync replaces renewed/validated metadata directly. A malformed response could create an immediate observation loop; no current owner response producing those malformed values was demonstrated. Record unverified medium under native Folders qualification: settle with owner response invariants and a controlled malformed-renewal transport case. This native adapter is separately committed work, not an operation-contract or source-recovery change. |
| U1-B6 | low | rejected | Release is already bounded to five seconds, failure retains owner state for supported recovery, and the storage attempt has a safe failure outcome. Adding a cleanup diagnostic protocol, logger plumbing and new cases is optional observability expansion; the finding shows no additional everyday effect beyond the already recorded owner lease-recovery problem. |
| U1-B7 | medium | defer | The workspace loads before subscribing, while the successful initial JoinTenant path does not trigger a fresh typed read. A projection change in that window can remain unseen until another signal/action. This same initial-join behavior exists at the original baseline; record a separate UI subscription follow-up, preserving typed reads as authority. |
| U1-B8 | low | rejected | A blank tenant intentionally leaves the subscriber unchanged until component teardown or a replacement subscription. Workspace disposal closes it; unloaded, cross-project and stale-scope callbacks are rejected by ProjectConversationEffects. The demonstrated residual is retaining an advisory connection, not exposing stale tenant data; adding a new clear-subscription contract is more than a direct correction for the shown impact. |
| U1-B9 | low | rejected | Dictionary replacement matches the existing Json.NET dictionary behavior; no current producer of duplicate response members or promised duplicate-key policy was identified. A new key-alias/duplicate rejection policy and converter branches are not justified by this hypothetical response alone. |
| U1-B10 | medium | defer | Carried R7B3/R8B7 and Story 1.6 R22Build-BH-7/EH-4: curl has no request timeout within the publication verifier's outer deadline. Existing release-maintenance ownership and ledger entry stand. |
| U1-B11 | low | rejected | Metadata rows retain label-before-value DOM order within the same FluentStack, including code/time values. No broken reading-order or assistive-technology interaction was demonstrated. Introducing new IDs/roles and relationship machinery for these noninteractive text pairs is optional semantic enhancement, not a direct correction to the operation contract. |
| U1-E1 | false | rejected | Carried R4E1/R5E1/R6E1: SafeNextActions has always been a required positional persisted field and every identified writer supplies it. No historical producer of the fabricated omitted/null record was identified; retain the existing rejection. |
| U1-E2 | low | rejected | Carried BH1/EC2/R8E3, the same constructor/reducer slot-grammar claim as U1-B4. Current malformed-concurrency acceptance is exercised at the guarded live adapter; the inactive reusable-boundary decision remains unchanged. |
| U1-E3 | medium | defer | StrictStringEnumConverter delegates nonempty names to Json.NET, whose comma-separated parsing can map Admitted, Committed to Rejected. The reviewer reproduced this through the actual helper. The same delegation exists in the original baseline; retain a separate client-contract follow-up for non-flags compound names, without changing accepted aliases or generated-client bytes here. |
| U1-V1 | medium | defer | Pre-verified test gap: SourceMappingWrapperTests observes Debug, hash, cwd and exit code, but none of the five child MSBuild import variables. The reviewer removed all import keys in a temporary wrapper copy and both existing tests still passed. The real wrapper correctly forwards the full environment and current mapped builds pass; extend the existing child-observation test in the separately committed build-tooling follow-up. |

Disposition: **15 findings, 0 patches, 0 intent/spec loopbacks**. Seven findings carry earlier dispositions; four new follow-ups are recorded separately in deferred-work (native lease-response qualification, initial subscription refresh, compound enum names, and child import-variable assertions). The other four new findings are rejected above. No production or test source was changed after verification, so no broader test repetition was needed. Existing owner/workflow deferrals, frozen intent, original baseline and completion gates are preserved.

### Review Findings — round 13 (2026-11-01, R12-patch review)

Code review round 13 (bmad-code-review, full mode). The user chose the recommended scope: only the round-12 patches, `git diff --ignore-cr-at-eol 48a1dfe2e3d5992b5cddbc717915cb0ede4af99c 86f658f5eb46fffbe75f52f814e199a39edb9086` restricted to this story's File List code paths — `LiveContinuityAspireE2eTests.cs` and `ServerBootstrapApiTests.cs`, 2 files, +17/−15, 85 diff lines. Later commits through `0a55921` change no File List code path. This is not a re-review of the original-baseline implementation; baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and frozen intent are unchanged. Loaded context: this spec, `epic-1-context.md` and `references/Hexalith.AI.Tools/hexalith-state-instructions.md`.

All four independent layers completed: Blind Hunter returned nine findings (B1–B9), Edge Case Hunter five (E1–E5), Verification Gap no verification gaps plus one other finding (V1), and Acceptance Auditor two low findings plus one informational note (A1–A3). Parent triage of 18 raw findings: **0 decision-needed, 3 patch, 1 defer, 8 rejected**. No layer failed. Verification is static; the current full Server result `TestResults/story-1-5-resumed-implementation/server.trx` already records the three changed R12 tests passing at this HEAD.

- [x] [Review][Patch] **R13-P1 (low): finish the R12-P2 boundary-order sweep** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eContractTests.cs:179]. Findings B1, B2, E5, V1, A1, A2. Two explanations still state the retired order: the `MailboxAdmissionProbeBodyShouldPassTheProductionRequestContract` summary (`LiveContinuityAspireE2eContractTests.cs:179-181`, "validated BEFORE authentication: a contract-invalid body answers the validation 400 whatever the bearer") and the `MailboxAdmissionProbeBody` summary (`LiveContinuityAspireE2eTests.cs:1095-1096`, "passes the OpenAPI boundary and reaches authentication"). Both contradict the renamed in-process test, whose anonymous loop requires 401 for the contract-invalid body. Correct both to authentication → contract validation → admission, retaining the rule that a 400 never proves admission. The contract-test file joins the File List. Comment-only.
- [x] [Review][Patch] **R13-P2 (low): restore the reason in the rewritten mailbox-probe remark and re-wrap it** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:949]. Finding B7. "The former incomplete mailbox source could not establish admission" no longer says why, so "The probe therefore sends…" has no stated premise, and line 951 breaks mid-sentence at 72 columns. State that the incomplete body answers the contract 400 after authentication but before admission, so it proves authentication only, then re-wrap the paragraph. Comment-only.
- [x] [Review][Patch] **R13-P3 (low): give periodic-enforcement health its actual redaction reason** [tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs:969]. Finding B6. R12-P3's "requires authentication … so it must disclose nothing" names authentication as the reason for redaction. Production (`ChatBotCompatibilityEndpointExtensions.cs:55-59`) states the actual reason: the externally published route is readable by any authenticated caller. "token-gated M2 release-gate endpoint" no longer distinguishes the two endpoints now that both are gated; the M2 endpoint uses its shared-secret scheme (`ChatBotM2ReleaseGateAuthentication`). Line 971 is 128 columns in a block wrapped at 120. Reword and re-wrap only; every redaction assertion is unchanged.
- [x] [Review][Defer] **R13-W1 (low): Story 1.2's completion evidence still names the renamed bootstrap test** [_bmad-output/implementation-artifacts/evidence/1-2-publish-the-openapi-contract-spine-and-typed-client.json:656] — deferred: findings E4, A3. Task-43 lists `CommandEndpointShouldValidateContractBeforeAuthenticationAndAuthenticateBeforeAdmission`, which R12-P2 renamed; no other reference remains. `TransitionDetector` re-validates evidence mappings only on a transition to done, and Story 1.2 is already done, so CI is unaffected; a later re-validation of Story 1.2 would report a missing assertion. The fix edits another completed story's evidence contract, outside this story's boundary.

Applied 2026-11-01 by the user's option 1, "Apply every patch". R13-P1 corrects the `MailboxAdmissionProbeBodyShouldPassTheProductionRequestContract` summary (contract validated after authentication and before admission; an authenticated contract-invalid 400 proves authentication, never admission) and the `MailboxAdmissionProbeBody` summary (an authenticated request passes OpenAPI validation and reaches admission). R13-P2 restores the reason in the mailbox-probe remark (the former incomplete body answered the contract 400 after authentication but before admission, so it proved only authentication) and re-wraps the paragraph. R13-P3 states that any authenticated caller can read the externally published periodic-enforcement health endpoint and that the M2 release-gate endpoint is protected by its own shared-secret scheme; every redaction assertion is unchanged. All three are comment-only. `LiveContinuityAspireE2eContractTests.cs` joins the File List. R13-W1 and the rejected findings were not touched.

Post-patch validation:

- `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1` and the same command for `tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj` each exited **0** with **0 warnings, 0 errors**, so the edited XML documentation compiles.
- `tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests -class '*LiveContinuityAspireE2eContractTests'` exited **0**: **52 passed**, 0 failed, 0 skipped.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests` with `-method` filters for `TypedFacadeShouldPreserveCaseDistinctProposalMetadataKeysAtValidationBoundary`, `CommandEndpointShouldAuthenticateBeforeContractValidationAndValidateBeforeAdmission` and `PeriodicEnforcementHealthEndpointShouldExposeSchedulerStatus` exited **0**: **3 passed**, 0 failed, 0 skipped.
- `git diff --check` passed for the three test files; their line endings are unchanged (the mixed-EOL live test file keeps its 316 CRLF lines), and no added line exceeds 120 columns. No full lane was re-run; the 65 current Server failures recorded above are unaffected by these comment-only edits.

Code-review actions: **3 patches applied, 0 decision-needed, 1 deferred, 8 rejected**. Story 1.5 stays spec `in-review` / sprint `review`, the status `0a55921` restored after round 12, rather than the generic workflow's `in-progress`. Completion remains gated by the explicit owner/workflow deferrals, the 65 current Server failures, A6/A13 owner/protection evidence, retry-profile approval and the completion evidence contract. Story 1.6 is unchanged.

Rejected:

- **R13-B3 (`false`)** — The renamed test cannot distinguish validate-before-admission from the reverse order. The order is pinned in the same class: `CommandEndpointShouldRejectInvalidContractBeforeAdmissionWithVersionedSafeProblem` (`ServerBootstrapApiTests.cs:1382`) sends authenticated contract-invalid bodies whose commands admission would refuse and requires 400 with zero dispatch and zero audit, so a reorder fails it. `LiveContinuityAspireE2eTests.cs:952` cites the class, not the single test.
- **R13-B4/E2 (`low`)** — The facade test never asserts that both `source`/`Source` keys were sent. This predates the delta (`8636ee9`, Story 1.2). Server-side ordinal duplicate detection (`CommandSubmissionContractAdapter.cs:270`) remains pinned, because a case-insensitive comparer would produce the contract 400. No producer of client-side key loss was shown; a capturing handler is new test infrastructure.
- **R13-B5/E1 (`low`)** — The 403 does not pin which admission branch refused, and the outcome depends on the intended-command alias. Every `CommandNotAllowlisted` branch runs after contract validation, so the test's purpose holds. The actual branch is `ChatBotAdmissionEvidenceValidator` operation validation (`:62-69`): `AiActionCommandMetadataProvider.TryGet("AppendConversationMessage")` is null because the supported name is `Project.AppendConversationMessage`, and this check precedes classification. The test comment ("admission refuses its unsupported intended command") is accurate; R12's applied note attributing the refusal to "the classifier's rejected record" names the wrong stage. E1's audit-empty guard assumes the classifier path. Accepting the alias would fail this test loudly.
- **R13-B8 (`low`)** — The in-process and live comments name different 503 types. The in-process summary (`ServerBootstrapApiTests.cs:1040-1046`, outside this delta) describes its own fixture, and the edited cross-reference claims only the order. The duplicated in-process probe body predates this delta; the live body is separately pinned by `MailboxAdmissionProbeBodyShouldPassTheProductionRequestContract`.
- **R13-B9 (`low`)** — The live run never sends the incomplete body with a forged bearer. The deployed host runs the pipeline the in-process test pins; adding a live probe branch is the credential/body matrix expansion already rejected as R12-B3.
- **R13-E3 (`false`)** — Anonymous access to periodic-enforcement health is untested. `EventStoreDomainServiceEndpointInventory` permits anonymous metadata only on the exact health, alive and ready probes, so an anonymous mapping of this route is a startup violation and `CreateClient` fails (R12-B5a).

### Review Findings — round 14 (2026-11-01, R13-patch review)

Code review round 14 (bmad-code-review, full mode). The user chose the recommended scope: only the round-13 patches, `git diff --ignore-cr-at-eol 0a55921 bd14dfe -- src tests` — `LiveContinuityAspireE2eContractTests.cs`, `LiveContinuityAspireE2eTests.cs` and `ServerBootstrapApiTests.cs`, 3 files, +13/−10, 63 diff lines, comment-only. Nothing on the File List changed after `bd14dfe`. This is not a re-review of the original-baseline implementation; baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and frozen intent are unchanged. Loaded context: this spec, `epic-1-context.md` and `references/Hexalith.AI.Tools/hexalith-state-instructions.md`.

All four independent layers completed: Blind Hunter returned eight findings (B1–B8), Edge Case Hunter two (E1–E2), Verification Gap no verification gaps, and Acceptance Auditor no acceptance violations plus one low finding and two nits (A1–A3). Parent triage of 13 raw findings: **0 decision-needed, 6 patch, 0 defer, 4 rejected**. No layer failed. Every surviving finding is a low comment defect; no assertion or code line is affected. Verification is static.

- [x] [Review][Patch] **R14-P1 (low): R13-P2's history of the retired probe body is wrong** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:950]. Finding B1. "The former probe body (an incomplete mailbox source) therefore proved only authentication" applies today's order to a body retired in `833c673`, while contract validation still preceded authentication: that commit's remark says the body "passed with any token for exactly that reason", so its 400 proved nothing. The order flipped only in `e89a3ac`. State the present rule (a contract-invalid body proves at most authentication) and, if the history stays, that the retired body proved nothing under the former validate-first order. Comment-only.
  - Applied 2026-11-03: the remark now says a contract-invalid body proves at most authentication, and that the retired body proved nothing because contract validation then preceded authentication. Comment-only; IntegrationTests build 0 warnings/0 errors, `LiveContinuityAspireE2eContractTests` 58/58.
- [x] [Review][Patch] **R14-P2 (low): the remark's "so" supports only half of its conclusion** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:948]. Finding A2. In "Authentication precedes OpenAPI request validation, so a contract-invalid body answers the versioned 400 after authentication but before admission", the premise yields "after authentication" but not "before admission". State that validation precedes admission. Comment-only; one rewrite of the paragraph covers R14-P1 and R14-P2.
  - Applied 2026-11-03: the same rewrite states that validation precedes admission. Comment-only; validated with R14-P1.
- [x] [Review][Patch] **R14-P3 (low): the probe-body summary names a stop point the live lane never reaches** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:1097]. Findings B2, E1, A1. "An admitted request fails in dispatch planning" omits that live admission refuses the probe first: `ValidateAtomicParticipationAsync` (`ChatBotCommandAdmissionPipeline.cs:262`) returns `CanonicalExecutionUnavailable` before dispatch. This file's own remarks (`:942-944`) and the call-site comment ("IntakeId is not a ULID if dispatch is ever reached") both say so. Name the canonical-execution 503 as the expected live outcome and the non-ULID intake as the backstop. The wording is pre-existing; R13-P1 re-wrapped it while aligning this summary. B2's companion claim about line 951 ("invalid only after admission") is false: the non-ULID intake is the body's only invalidity, and it is checked only after admission.
  - Applied 2026-11-03: the `MailboxAdmissionProbeBody` summary names the live canonical-execution `503` before dispatch and makes the non-ULID intake the backstop. Comment-only; validated with R14-P1.
- [x] [Review][Patch] **R14-P4 (low): "before any write" is false for the dispatch-planning path** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:1097]. Finding B3. By the time `AcceptedCommandDispatcher.BuildPlanAsync` rejects the non-ULID intake (`AcceptedCommandDispatcher.cs:205-207`), several writes have already happened. Admission has reserved the idempotency record (`CommandGateway.cs:113`), and the gateway has written `PrepareDispatchAsync` (`:138`). The failure branch then queues a `PreCommitOperationReplay` intent and raises an `AuditUnavailable` alert (`:200-207`). The in-process copy of the body says "before any external write" (`ServerBootstrapApiTests.cs:3719-3721`). Align to it. This is the same sentence as R14-P3, so one rewrite fixes both.
  - Applied 2026-11-03: the same rewrite says "before any external write", matching the in-process copy in `ServerBootstrapApiTests`. Comment-only; validated with R14-P1.
- [x] [Review][Patch] **R14-P5 (low): the R13-P1 re-wrap leaves an orphan line** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:1098]. Findings B8, A3. `/// any write.` (18 columns) stands alone in a summary otherwise wrapped near 117–120 columns. Re-wrap with the R14-P3/P4 rewrite.
  - Applied 2026-11-03: the rewrite wraps at 113–119 columns with no orphan line; only paragraph-final lines are shorter. Comment-only; validated with R14-P1.
- [x] [Review][Patch] **R14-P6 (low): the contract-test summary still makes both mailbox probes depend on the body** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eContractTests.cs:179]. Finding E2. "The live mailbox-bearer probes are only meaningful when their body passes the OpenAPI request contract" held under the retired validate-first order, when the forged-bearer probe needed a contract-valid body to reach 401 (`833c673`). Authentication now precedes validation, so the forged-bearer 401 no longer depends on the body (`AssertInvalidMailboxBearerIsRejectedBeforeAdmissionAsync` summary). Narrow the sentence to the authenticated admission probe. This is retired-order residue that R13-P1's sweep missed. Comment-only.
  - Applied 2026-11-03: the summary is narrowed to the authenticated admission probe and adds that the forged-bearer `401` precedes validation and does not depend on the body. Comment-only; contract tests 58/58.

On 2026-11-02 the user chose option 2, "Leave as action items". R14-P1 through R14-P6 stay unchecked above; no test file was edited. Code-review actions: **0 decision-needed, 6 patches left as action items, 0 deferred, 4 rejected**. Story 1.5 stays spec `in-review` / sprint `review`, as in round 13, rather than the generic workflow's `in-progress`. The open items are comment-only and do not touch the review base. Completion remains gated by the explicit owner/workflow deferrals, the 65 current Server failures, A6/A13 owner/protection evidence, retry-profile approval and the completion evidence contract.

Rejected:

- **R14-B4 (`false`)** — "A 400 is not only a pre-admission signal." The edited comments speak only of contract-invalid bodies. They never claim that admission cannot itself answer 400 (the `ChatBotAdmissionEvidenceValidator` revision and metadata checks), and "a 400 never proves admission" still holds. The live proof requires the canonical-execution or dispatch-unavailable 503 (`ProvesMailboxAdmission`), so an admission-stage 400 would fail the probe loudly. The in-process order test requires 503 for the valid body.
- **R14-B5 (`false`)** — The stated order skips the boundary authorization stage. "After authentication and before admission" is still a true partial order: authentication → `ChatBotApplicationBoundaryPolicy` (EventStore's human policy) → contract validation → admission. Neither comment claims that nothing runs in between. The live remarks (`:940-942`) already list authorization among what the problem type proves.
- **R14-B6 (`false`)** — "'Any authenticated caller' is imprecise." The wording matches the production rationale (`ChatBotCompatibilityEndpointExtensions.cs:55-59`). Limiting readers to human bearers does not change the redaction requirement, because the endpoint is externally published and not scoped to a tenant. The comment explains the assertions. It does not claim the test reads the endpoint as every possible caller.
- **R14-B7 (`false`)** — "The redaction assertions under the rewritten comment cannot fail." The endpoint returns `PeriodicEnforcementHealthResponse.From(coordinator.Status)` (`ChatBotCompatibilityEndpointExtensions.cs:63`), an eight-member liveness record with no M2 sweep, stop-ship or evaluator-failure member. `PeriodicEnforcementCoordinatorTests.cs:752-760` seeds a WORM sweep with seven breaches plus a WORM evaluator failure count. It then asserts that the serialized liveness response carries no breach, stop-ship or coverage data, no job name and no count. The API test's assertions are also outside this comment-only delta.

### Review Findings — round 15 (2026-11-02, delta code review)

Code review round 15 (bmad-code-review, full mode). The user chose the recommended scope: `git diff --ignore-cr-at-eol bd14dfe 18f8301` restricted to this story's 74 File List code paths. Two test files changed: `CommandGatewayAdmissionApiE2ETests.cs` (from `e48e87e`, "fix(authentication): preserve denial audits and authenticated hub access") and `ServerBootstrapApiTests.cs` (from `18e9fce`, "fix(gateway): retain typed terminal refusals under SDK catalog guards"). The delta is +122/−3, 190 diff lines, all test code. The round-14 action items R14-P1 through R14-P6 are still unapplied, and their files have not changed. This round does not re-review the original-baseline implementation. Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and the frozen intent are unchanged. Loaded context: this spec, `epic-1-context.md` and `references/Hexalith.AI.Tools/hexalith-state-instructions.md`.

All four independent layers completed, and none failed:

| Layer | Findings |
| --- | --- |
| Blind Hunter | 13 (B1–B13) |
| Edge Case Hunter | 10 (E1–E10) |
| Verification Gap | none |
| Acceptance Auditor | 5 (A1–A5) |

Parent triage of the 28 raw findings: **2 decision-needed, 2 patch, 0 defer, 24 rejected**. Both decisions resolved to patches, giving **4 patches** in total.

The Verification Gap layer ran current evidence at `18f8301` after `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1` (0 warnings, 0 errors):

- The full Server lane passed **3643/3643**: 0 failed, 0 skipped, 0 not run.
- The three changed gateway tests passed 9/9, including all seven theory rows, twice. Four concurrent copies of the two new tests also passed 8/8 each.
- `ServerBootstrapApiTests` passed 106/106, three times. This includes all 17 cases that use the two changed factories.

Decision items:

- [x] [Review][Decision] **R15-D1 (medium): the boundary denial audit reads the unauthenticated command body, which R12-W1's closure forbids** — Finding A1.
  - R12-W1 (round 12 in this spec, plus its `deferred-work.md` reason) and U1-B1 require the follow-up to reconcile denial auditing "without reading an unauthenticated command body".
  - `e48e87e` does read it. `ChatBotApplicationBoundaryResultHandler.ReadDeniedCommandNameAsync` (`:95-158`) reads at most 16 KiB of an `application/json` body within 250 ms. It records `commandType` only when that is a canonical allowlisted name; otherwise it records `unknown_command`.
  - Two tests pin the body-derived `RecordGovernedNote`: the anonymous test (`CommandGatewayAdmissionApiE2ETests.cs:2585`) and the theory's challenged `normal` rows (`:2655`).
  - Before `e89a3ac`, the gateway model-bound the whole unauthenticated body and recorded `SafeCommandName` with no allowlist. The bounded read is therefore tighter than the behaviour R12-W1 lost.
  - **Option 1:** ratify the bounded, allowlist-only read. Amend R12-W1's closure wording and record its resolution with this round's evidence.
  - **Option 2:** enforce the original wording. Challenged denials record `unknown_command` without touching the body. `:2585`, `:2655` and the handler unit rows that expect `RecordGovernedNote` under a challenge change to match.
  - **Resolved 2026-11-02:** the user chose option 1, ratifying the bounded, allowlist-only read. This becomes R15-P3.
- [x] [Review][Decision] **R15-D2 (medium): an audit outage at the boundary turns a typed 401/403 into an untyped 500** — Finding A3.
  - `ChatBotApplicationBoundaryResultHandler.cs:72-81` lets a `RecordAuthorizationFailureAsync` exception propagate. `ChatBotApplicationBoundaryResultHandlerTests.AuditBackendFailureMustPropagateWithoutRetryOrEndpointEffects` pins that behaviour.
  - ChatBot.Server registers no exception handler (`Program.cs:143-152`), and neither do EventStore's DomainService or ServiceDefaults. The host therefore answers an empty 500 in Production and the developer exception page in Development.
  - Before `e89a3ac`, the same audit failure inside `ChatBotCommandAdmissionPipeline.DenyAsync` was caught by `CommandGateway.SubmitAsync` (`CommandGateway.cs:79-86`), which returned the typed dependency-unavailable 503 (catalog code `dependency_degraded`, retryable).
  - R12-W1's closure asks for the "expected behavior when its audit path is unavailable" through the host-boundary lane. No HTTP case covers it.
  - **Option 1:** map the boundary audit failure to the gateway's typed dependency-unavailable 503 (`dependency_degraded`) with correlation headers. Invert the unit test and add an HTTP case.
  - **Option 2:** keep propagation as the intended fail-closed behaviour and record it in R12-W1's closure.
  - **Option 3:** defer to a host-boundary follow-up.
  - **Resolved 2026-11-02:** the user chose option 1, the typed dependency-unavailable 503. This becomes R15-P4.

Patch items:

- [x] [Review][Patch] **R15-P1 (medium): boundary denials no longer feed the authorization-failure spike counter** [src/Hexalith.ChatBot.Server/Authentication/ChatBotApplicationBoundaryResultHandler.cs:72]. Finding A2.
  - `IAuthorizationFailureCounter` feeds the NFR43 authorization-failure spike alert. Its contract (`IAuthorizationFailureCounter.cs:3-11`) requires it to be fed "from the same code path that emits `ChatBotAuthorizationFailureAuditFact`", including the literal `unavailable` tenant.
  - Before `e89a3ac`, anonymous submissions reached `ChatBotCommandAdmissionPipeline.DenyAsync` (`:577-603`), which records the audit fact and then calls `authorizationFailureCounter?.Record`.
  - `e48e87e` restored the boundary audit fact but not the counter. As a result, floods of unauthenticated or forbidden-workload requests never reach spike alerting.
  - **Fix:** after the audit write, record the tenant value the fact carries. Assert one counter reading in the anonymous test and the theory, using a recording counter registered by the test, because the hosted alert coordinator drains the production counter.
  - The handler is outside the File List; it joins the File List if the patch is applied.
- [x] [Review][Patch] **R15-P2 (medium): the story's completion blockers still cite 65 Server failures that no longer reproduce** [_bmad-output/implementation-artifacts/spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md:1065]. Finding A4.
  - The failure groups under "Resumed implementation verification after source preparation recovery (2026-11-01)" (`:1031-1037`), the closeout status note under "Source-object recovery and parent closeout (2026-11-01)" (`:1065`) and R12-W1 list "65 current Server failures (62 SDK HTTP/result-body cases, two hub authentication cases and R12-W1)" as a completion blocker.
  - At `18f8301` the full Server lane passes 3643/3643. `18e9fce` adopted the finite route-catalog receiver, and `e48e87e` restored the boundary audit and hub authentication.
  - **Fix:** record this round's evidence against that blocker, and keep the earlier failing run as historical evidence. Record R12-W1's resolution status according to R15-D1 and R15-D2.
  - Records only; the intent does not change.
- [x] [Review][Patch] **R15-P3 (medium, from R15-D1): ratify the bounded denial-metadata read in R12-W1's closure and record its resolution** [_bmad-output/implementation-artifacts/deferred-work.md:2136].
  - Amend R12-W1's "without reading an unauthenticated command body" condition, both in this spec's round-12 entry and in the ledger reason, to the ratified rule: the boundary may read at most 16 KiB of an `application/json` body within 250 ms, and it records only a canonical allowlisted `commandType`. Anything else records `unknown_command`, and no body identity, resource or origin field is ever used.
  - Record the resolution in `e48e87e`, with evidence: the passing anonymous test, all seven theory rows, the handler unit class and the full Server lane (3643/3643).
  - R12-W1's audit-unavailable condition closes with R15-P4.
  - Records only.
- [x] [Review][Patch] **R15-P4 (medium, from R15-D2): return the typed dependency-unavailable 503 when the boundary denial audit fails** [src/Hexalith.ChatBot.Server/Authentication/ChatBotApplicationBoundaryResultHandler.cs:72].
  - Catch a non-cancellation exception from `RecordAuthorizationFailureAsync` and answer `problemDetailsFactory.CreateDependencyUnavailable(correlation.CorrelationId, correlation.TaskId)` with the correlation headers already set. This matches `CommandGateway.SubmitAsync` (`CommandGateway.cs:79-86`).
  - Cancellation still propagates.
  - Invert `AuditBackendFailureMustPropagateWithoutRetryOrEndpointEffects` so it expects the 503 and exactly one attempted fact, and add an HTTP case through the real host.
  - The handler joins the File List.

Applied 2026-11-02 by the user's option 1, "Apply every patch", after both decisions resolved to option 1:

- **R15-P1 and R15-P4** (`ChatBotApplicationBoundaryResultHandler.cs`): a denied `POST /api/v1/commands` records the denial fact, then feeds `IAuthorizationFailureCounter` with the fact's tenant value when a counter is registered. A non-cancellation audit failure answers `CreateDependencyUnavailable`, the gateway's typed and retryable 503 (catalog code `dependency_degraded`), with the correlation headers already set. Nothing is counted in that case, and cancellation still propagates.
- **Tests:**
  - New test double `BoundaryAuthorizationFailureCounter`, which never drains.
  - The anonymous test and the theory each assert one counter entry carrying the fact's tenant.
  - The new `CommandGatewayApi_ShouldAnswerTypedDependencyUnavailableWhenBoundaryDenialAuditFails` drives an audit failure through the real host; the gateway's private audit double gains `AuthorizationFailureException`.
  - The unit test is inverted to `AuditBackendFailureMustAnswerTypedDependencyUnavailableWithoutRetryOrEndpointEffects`, and `RecordedDenialMustFeedTheFailureCounterWithTheFactTenant` is added.
- **R15-P2 and R15-P3** (records only):
  - The source-recovery closeout note now carries a superseded marker with current evidence.
  - The round-12 R12-W1 entry, the U1-B1 row and the ledger `resolution:` record the ratified bounded read and the resolution.
  - The handler, its unit class and the new test double join the File List.

Post-patch validation (evidence under `TestResults/story-1-5-r15/`, which is gitignored):

- `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1` exited **0** with 0 warnings and 0 errors.
- The handler unit class (`-class Hexalith.ChatBot.Server.Tests.Authentication.ChatBotApplicationBoundaryResultHandlerTests`) passed **19/19**.
- The four gateway boundary tests (`-method` filters) passed **10/10**, including all seven theory rows.
- The first run failed both new tests on a wrong expectation of mine: I expected `audit_unavailable`, but the concrete factory's dependency-unavailable code is `dependency_degraded`. I corrected the tests; the production call did not change.
- The full Server lane passed **3645/3645**, with 0 failed and 0 skipped: the 3643 tests from before the patches plus the two new tests.
- The Conformance lane passed **151/151**.
- The Architecture lane passed **166/167**. The one failure, `ScaffoldArchitectureTests.RootDeclaredSiblingPackageWrappersShouldDelegateVersionsToSharedBuildsCatalog`, is shared-catalog drift: `references/Hexalith.Projects/Directory.Packages.props` pins `Hexalith.EventStore.Admin.Abstractions` at 3.132.0 while the catalog says 3.132.2. This round touches neither file, and both submodules are at their recorded commits.
- Line endings are unchanged. The handler, its unit class, the new double, this spec and `deferred-work.md` are all-CRLF; `CommandGatewayAdmissionApiE2ETests.cs` stays LF.

Code-review actions: **2 decisions resolved (both option 1), 4 patches applied, 0 deferred, 24 rejected**.

Story 1.5 stays spec `in-review` / sprint `review`, not the generic workflow's `done`. Completion remains gated by:

- the explicit owner/workflow deferrals;
- the unapplied R14-P1 through R14-P6 comment items;
- A6/A13 owner/protection evidence;
- retry-profile approval;
- the completion evidence contract.

Rejected:

- **R15-B1 (`false`)** — Claim: the signed-human test overclaims owner authority. Its comment names the actual cause, the fixture's missing owner persona ("No owner persona or successful authority grant is supplied"). It claims only that the real bearer handler and human policy pass the credential to the real authentication stage, and the proxy's single `AuthenticateAsync` call pins that. It never claims that `CurrentChatBotOwnerAuthorityProvider` ran.
- **R15-B2 (`false`)** — Claim: the test cannot prove the tenant came from the credential. Payload-derived tenant binding is pinned by the neighbouring `CommandGatewayApi_ShouldRejectCrossTenantTargetBeforeDispatchAndRecordMetadataOnlyAuditFact`. Giving this test a different body tenant would turn it into that test.
- **R15-B3/E9 (`false`)** — Claim: the signed-human test lacks origin, correlation, category and visibility assertions. It denies through `DenyAsync`, whose response shape the same class already pins: `SurfaceOrigin` at `:1076`, `:1466`, `:1646` and the request-origin theory at `:2290`, and category and visibility in the cross-tenant test.
- **R15-B4 (`false`)** — Claim: the 403 boundary category is never asserted. `CreateAuthorizationProblem` derives the category from the status alone (`ChatBotProblemDetailsFactory.cs:82-84`). The theory asserts the 403 status and the code, and 17 `authorization_denied` category assertions in the class pin that ternary.
- **R15-B5/E5 (`false`)** — Claim: the 403 body is never checked for the credential tenant. That tenant is the caller's own, and `CreateAuthorizationProblem(reason, correlation, task)` has no tenant parameter, so the tenant has no way into the body.
- **R15-B6 (`false`)** — Claim: the minted JWTs are not leak sentinels. Neither the positional `ChatBotAuthorizationFailureAuditFact` nor the authorization problem has a member that could carry a bearer token.
- **R15-B7 (`false`)** — Claim: the replacement correlation id is never validated. The boundary's fallback is `ChatBotCorrelationId.New()` (`ChatBotCorrelationHttpContextExtensions.cs:79-82`), which `ServerBootstrapApiTests` pins. The task id is `null` by construction when the header does not parse, and the sentinel scan already rejects the unsafe header value.
- **R15-B8 (`false`)** — Claim: channel, non-human, issuer, audience and expiry rows are missing. The audit branch depends only on challenge versus forbid (`ChatBotApplicationBoundaryResultHandler.cs:46-48`, `:66`), and the theory covers both. Per-credential outcomes belong to EventStore's human policy and to `ChatBotSignedBearerFixtureTests`, which covers signature, issuer and audience.
- **R15-B9/E3 (`low`)** — Claim: the content-type, lone `CommandType` and non-string-value branches are untested. Every recordable name must pass the canonical allowlist, which the handler's unit rows pin (`AliceSmith`, `recordGovernedNote`). Losing any of those guards could therefore record only a canonical command name. The cases belong in the unit class, outside this delta.
- **R15-B10 (`false`)** — Claim: the theory compares the wire code with the reason-code constant. A divergent catalog mapping would make it fail loudly, not pass silently. The handler's reason choice is pinned by `fact.ReasonCode`, and the anonymous test and the handler's unit tests use the same constant.
- **R15-B11 (`low`)** — Claim: the 401 responses carry no `WWW-Authenticate` header. ChatBot's typed 401 has never sent it, including the gateway path before `e89a3ac`, and no ChatBot surface reads it. Adding it would change production code and the published contract, outside this test-only delta.
- **R15-B12 (`false`)** — Claim: the factory comment is stale. "Bypass only its named route policies" is still accurate: the finite receiver bypasses nothing, and `ConfigureFiniteReceiver` documents its own purpose (`SdkRouteFixtureAuthorization.cs:21`).
- **R15-B13 (`false`)** — Claim: the two helpers call `ConfigureFiniteReceiver` in a different order.
  - Callers pass only `IServiceCollection` callbacks.
  - Only `ConfigureFiniteReceiver` registers `HttpRouteCatalogFixtureFiles` or the route-catalog settings.

  The order therefore has no observable effect. The parent factory that is never disposed also never builds a host.
- **R15-E1 (`false`)** — Claim: the duplicate row cannot show which key wins. A caller can name any canonical command with a single key, so winning a duplicate gains nothing, and both values pass the same allowlist.
- **R15-E2 (`false`)** — Claim: chunked bodies, the size boundary and the read deadline are untested. `OversizedDeniedBodyMustStayBoundedWithOrWithoutContentLength(false)` streams a body with no length and stops at 16 KiB + 1, and `StalledMetadataReadMustExpireWithOneSafeFactAndTypedDenial` pins the deadline.
- **R15-E4 (`false`)** — Claim: the forbid branch is tested only with `normal` metadata. That branch takes identity only from `context.User`, with a constant API origin (`:66-69`). The body read returns only an allowlisted name, so no metadata variant can affect identity.
- **R15-E6 (`low`)** — Claim: a boundary denial issues a new correlation id where the endpoint would fall back to the body's `commandId`. Only callers that omit `X-Correlation-Id` see this. Aligning the two would mean deriving correlation from the unauthenticated body, which extends R15-D1.
- **R15-E7 (`low`)** — Claim: `RecordCount` cannot detect a reserve-then-abort sequence. The theory's rows never reach the endpoint. The final-state idiom already appears at 22 sites in this class before the delta, and a call-recording proxy would be new test infrastructure.
- **R15-E8 (`low`)** — Claim: the signed-human 403 cannot tell which pipeline branch denied. The test's subject is that the request passes the boundary into the real authentication stage, which the proxy call pins. Telling the branches apart would need a positive-control grant or an authorizer probe.
- **R15-E10 (`false`)** — Claim: the commit's "Use actual ordinary SDK authority" misleads readers of the factories. The factory comment (`ServerBootstrapApiTests.cs:3220-3222`) states that the named route policies are bypassed, and the commit's phrase describes `ChatBotTerminalRefusalTests` (`:35`).
- **R15-A5 (`false`)** — Claim: the test violates the shared-file preservation rule. That rule governs Story 1.5's own resumed edits. `e48e87e` is an independent `fix(authentication)` commit that this File List delta only brings into view. The part of the claim about telling denial branches apart is R15-E8.

### Review Findings — round 16 (2026-11-02, R15-patch review)

Code review round 16 (bmad-code-review, full mode). The user chose the recommended scope: only the round-15 patch commit `9152a9e` ("fix(authentication): count boundary denials and type audit outages"), as `git diff --ignore-cr-at-eol 18f8301 HEAD` restricted to this story's 77 File List code paths. That range equals `9152a9e`. It covers 4 files, +160/−24 and 287 diff lines:

- `ChatBotApplicationBoundaryResultHandler.cs`
- `BoundaryAuthorizationFailureCounter.cs` (new)
- `ChatBotApplicationBoundaryResultHandlerTests.cs`
- `CommandGatewayAdmissionApiE2ETests.cs`

Nothing else on the File List changed after `3eee82f`. The later commits touch only submodule pins, `.config/dotnet-tools.json` and `MemoriesProbeCommandGateway.cs`. R14-P1 through R14-P6 are still unapplied, and their files are unchanged. This round does not re-review the original-baseline implementation. Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and the frozen intent are unchanged. Loaded context: this spec, `epic-1-context.md` and `references/Hexalith.AI.Tools/hexalith-state-instructions.md`.

All four independent layers completed, and none failed:

| Layer | Findings |
| --- | --- |
| Blind Hunter | 9 (B1–B9) |
| Edge Case Hunter | 9 (E1–E9) |
| Verification Gap | 2 gaps (V1–V2) |
| Acceptance Auditor | no acceptance violations, plus 5 low findings (A1–A5) |

Parent triage of the 25 raw findings: **0 decision-needed, 4 patch, 0 defer, 13 rejected**. Twelve raw findings fold into the four patches; B7's rejected part is listed separately.

Every patch is low severity. In the shipped composition, denial facts go through `ChainedAuditWriter` to `InMemoryAuditWriter.RecordAuthorizationFailureAsync` (`InMemoryAuditWriter.cs:55-60`). That writer cannot fail for a non-null fact and ignores cancellation, so the R15-P4 503 branch is latent in production until a durable denial-fact writer exists.

Verified fact: `OperationalAlertWiringCoordinator.CollectAlerts` (`:121-123`) evaluates only readings whose tenant equals a tenant enumerated by `PeriodicEnforcementCoordinator.EnumerateTenantsAsync`. Unauthenticated boundary denials feed the literal `unavailable` bucket, which never raises an NFR43 alert. R15-P1's rationale ("floods of unauthenticated or forbidden-workload requests never reach spike alerting") is therefore restored only for forbidden credentials that carry a tenant. This matches NFR43's "authorization spikes over tenant baseline" wording and the counter contract. It is recorded here only (see R16-B1).

- [x] [Review][Patch] **R16-P1 (low): the new audit catch misclassifies failures** [src/Hexalith.ChatBot.Server/Authentication/ChatBotApplicationBoundaryResultHandler.cs:87]. Findings B2, B4, E1, E5, E9, A2.
  - The filter `when (exception is not OperationCanceledException)` lets an `OperationCanceledException` raised by the audit store itself escape as the untyped 500 that R15-D2 removed. An example is a client timeout while `RequestAborted` is live.
  - A caller abort that surfaces as an `IOException` gets a 503 written to an aborted response.
  - `GetRequiredService<IAuditWriter>()` sits inside the `try`. A resolution failure is a host misconfiguration, but it is reported as a retryable `dependency_degraded` 503.
  - **Fix:** resolve the writer before the `try`. Use this story's own pipeline pattern (`ChatBotCommandAdmissionPipeline.cs:210-213`, `:279-283`): `catch (Exception exception) when (exception is not OperationCanceledException || !context.RequestAborted.IsCancellationRequested)`, starting with `context.RequestAborted.ThrowIfCancellationRequested()`.
  - Request cancellation still propagates, as R15-P4 requires.
  - Applied 2026-11-03: `IAuditWriter` is resolved at the top of the command-denial branch, before the body read and the `try`; the catch uses `when (exception is not OperationCanceledException || !context.RequestAborted.IsCancellationRequested)` and starts with `context.RequestAborted.ThrowIfCancellationRequested()`. Server.Tests build 0 warnings/0 errors; full Server lane 3649/3649 (3645 + 4 new rows); a mutant with HEAD's catch fails the transport-abort, store-cancellation and missing-writer rows.
- [x] [Review][Patch] **R16-P2 (low): no test throws cancellation from inside the audit write** [tests/Hexalith.ChatBot.Server.Tests/Authentication/ChatBotApplicationBoundaryResultHandlerTests.cs:145]. Findings V2, A1.
  - `CancelledDenialMustPropagateCancellationWithoutAuditOrEndpointEffects` and `RequestAbortedDuringMetadataReadMustPropagateWithoutAudit` both stop in `ReadDeniedCommandNameAsync` before the audit call. Both assert `audit.Facts.ShouldBeEmpty()`.
  - Dropping the `when` filter, so that a request abort during the audit write answers 503, would therefore pass every test.
  - **Fix:** add unit rows where the audit write runs and then throws `OperationCanceledException`.
    - With `RequestAborted` cancelled during the write: the call must throw, the status must not be 503, the response body must be empty and the counter must stay empty.
    - With a live request (this pins R16-P1): the call must answer the typed 503.
  - Applied 2026-11-03: `BoundaryAuditWriter` (joins the File List) gained a `duringWrite` hook. New rows: `RequestAbortedDuringAuditWriteMustPropagateWithoutResponseOrCounter` (cancellation and transport; throws, no 503, empty body, empty counter), a live-request `store-cancellation` row on `AuditBackendFailureMustAnswerTypedDependencyUnavailableWithoutRetryOrEndpointEffects` (typed 503), and `MissingAuditWriterMustFailAsHostMisconfigurationRatherThanTypedOutage`. Handler class 23/23; a mutant without the `when` filter fails the cancellation row.
- [x] [Review][Patch] **R16-P3 (low): the counter feed is proven only through a double that discards the timestamp** [tests/Hexalith.ChatBot.Server.Tests/Authentication/ChatBotApplicationBoundaryResultHandlerTests.cs:181]. Findings V1, B6.
  - `BoundaryAuthorizationFailureCounter.Record` keeps only the tenant.
  - Passing `default` or a stale instant instead of the server clock would pass every test. In production, `InMemoryAuthorizationFailureCounter` would then prune the entry at read time, or throw from `MinValue.AddSeconds(-window)` after the fact was written.
  - The double's summary says a hosted alert reader could otherwise "hide one", and R15-P1 used a recording counter "because the hosted alert coordinator drains the production counter". The production counter is a sliding window, though: `ReadAndReset` keeps in-window events (`InMemoryAuthorizationFailureCounter.cs:50-69`).
  - **Fix:** make `RecordedDenialMustFeedTheFailureCounterWithTheFactTenant` use the real `InMemoryAuthorizationFailureCounter` with a fixed `ISystemClock`. Assert one reading: `unavailable`, `FailureCount` 1, and `WindowStartUtc` equal to the fixed instant.
  - Correct the double's summary to describe what it does: it records each fed tenant in order and reports no readings. Test-only.
  - Applied 2026-11-03: `RecordedDenialMustFeedTheFailureCounterWithTheFactTenant` feeds the real `InMemoryAuthorizationFailureCounter` through `WormAuditTestData.FixedClock` and asserts the single reading `(unavailable, 1, fixed instant)`; the double's summary now says it records fed tenants in order and reports no readings. Mutants: a `default` timestamp throws, wall-clock time fails the reading.
- [x] [Review][Patch] **R16-P4 (low): the HTTP 503 case never checks the task id** [tests/Hexalith.ChatBot.Server.Tests/Gateway/CommandGatewayAdmissionApiE2ETests.cs:2711]. Findings A3 and B7 (task-id part).
  - `CommandSubmissionRequest` sends `X-Hexalith-Task-Id: 01ARZ3NDEKTSV4RRFFQ69G5FAX`, but the 503 case asserts only the correlation id.
  - The sibling 401 test asserts both the task-id header and the body `taskId` (`:2604`, `:2606`).
  - The 503 body `taskId` comes only from `CreateDependencyUnavailable(correlation.CorrelationId, correlation.TaskId)`. Dropping that argument would therefore pass, and the unit context sends no task id at all.
  - **Fix:** assert the `X-Hexalith-Task-Id` header and the body `taskId` against `fact.TaskId`. Test-only.
  - Applied 2026-11-03: the 503 case asserts `fact.TaskId`, the `X-Hexalith-Task-Id` header and the body `taskId`; a mutant passing a null task id to `CreateDependencyUnavailable` fails at the body assert. `CommandGatewayAdmissionApiE2ETests` 62/62.

On 2026-11-02 the user chose option 2, "Leave as action items". R16-P1 through R16-P4 stay unchecked above; no source or test file was edited. Code-review actions: **0 decision-needed, 4 patches left as action items, 0 deferred, 13 rejected**. Story 1.5 stays spec `in-review` / sprint `review`, not the generic workflow's `in-progress` or `done`. Completion remains gated by:

- the explicit owner/workflow deferrals;
- R14-P1 through R14-P6 and R16-P1 through R16-P4;
- A6/A13 owner/protection evidence;
- retry-profile approval;
- the completion evidence contract.

Rejected:

- **R16-B1 (`low`), first part** — Claim: unauthenticated denials still never reach the spike alert. This is true, as recorded above, but it is pre-existing and consistent with NFR43's tenant-baseline scope: the counter contract explicitly feeds `unavailable`, and the Story 8.4 coordinator evaluates only enumerated tenants. A platform-scope anonymous-spike alert would be a new design, outside this delta.
- **R16-B1 (`low`), second part** — Claim: `unavailable` floods drive an O(n) `RemoveAll` under the counter's shared lock. This is real (`InMemoryAuthorizationFailureCounter.cs:71-75`), but it is the pre-existing Story 8.4 counter, which every gateway denial also feeds. The cost per request is bounded by the 600 s window, and it matters only under a sustained high-rate anonymous flood. A fix (prefix pruning or a capped bucket) is more than a direct correction.
- **R16-B3/E7/A4 (`low`)** — Claim: a boundary audit outage is now silent, with no log, metric or alert, where propagation used to reach the host's unhandled-exception log. This is real, but the branch is unreachable in the shipped composition (`InMemoryAuditWriter` cannot fail). `CommandGateway.SubmitAsync:83-86` swallows the same outage just as silently. Adding a logger dependency and a `LoggerMessage` is more than a direct correction. Revisit when a durable denial-fact writer lands.
- **R16-E4 (`low`)** — Claim: the counter is skipped while audit is down. This is deliberate in R15-P4 ("Nothing is counted in that case"). It matches `ChatBotCommandAdmissionPipeline.DenyAsync`, where an audit throw precedes `authorizationFailureCounter?.Record`, and the contract feeds the counter from the path that emits the fact. The branch is latent in production.
- **R16-B5/E2 (`false`)** — Claim: an exception from the counter or clock resolution after a successful audit becomes a bare 500. `InMemoryAuthorizationFailureCounter.Record` has no throwing path: it returns early on a blank tenant, and the tenant is never null. The shipped composition registers `ISystemClock` and the counter together (`CommandGatewayServiceCollectionExtensions.cs:297`, `:315`). `DenyAsync` uses the same optional-counter, required-clock shape.
- **R16-B7 (`false`), all but the task id** — Claim: the 503 tests omit `type`, `status`, `clientAction`, visibility, content type, the unit `category` and the absence of `tenant-alpha`. Both paths build the 503 with the same `CreateDependencyUnavailable` and `CoarseUserFacingRedactionStage`. The gateway tests pin its type, status and `RetryLater` client action (`CommandGatewayTests.cs:1400`, `:5140`), the HTTP case asserts status and `category`, and the factory takes no tenant parameter, so `tenant-alpha` cannot enter the body. The task-id part is R16-P4.
- **R16-B8 (`false`)** — Claim: the forbid path is untested for audit failure and counter feeding. The catch and the counter feed do not branch on challenge versus forbid. The HTTP theory's `workload` forbid row already asserts `failureCounter.Tenants.ShouldBe([fact.TenantId])` with `credential-tenant` (`:2682`), and the 503 factory has no tenant parameter, so it cannot echo `credential-tenant`.
- **R16-B9 (`false`)** — Claim: a retryable 503 tells unauthenticated callers to retry an invalid credential, and the CLI or MCP may loop. The typed, retryable 503 is the user's R15-D2 option 1, which also matches the gateway path before `e89a3ac`. No ChatBot client retries automatically: the CLI and MCP formatters only render `Retryable` (`ChatBotCliOutputFormatter.cs:315`, `ChatBotMcpResultFormatter.cs:104`).
- **R16-E3 (`low`)** — Claim: a send-and-abort burst never reaches the counter. This is true, but an aborted caller receives no 401 or 403, so an aborted probe learns nothing that a counted probe would. No fact is written by design (`RequestAbortedDuringMetadataReadMustPropagateWithoutAudit`), and the counter follows the fact. The gateway before `e89a3ac` also recorded neither when a request was aborted during model binding.
- **R16-E6 (`low`)** — Claim: boundary denials on GET, health, hub and recovery routes do not feed the counter. Only the admission pipeline and this handler have ever fed it, as a source search confirms. Read routes never emitted a `ChatBotAuthorizationFailureAuditFact` or fed the counter, so this is pre-existing scope, not a regression. Extending fact emission to other routes is more than a direct correction.
- **R16-E8 (`false`)** — Claim: resolving the problem-details factory before the audit means a factory failure leaves the denial unaudited. The factory is a registered singleton that both outcomes need, and its resolution had to move above the `try` so that the catch can use it. No reachable failure has been shown.
- **R16-A5 (`false`)** — Claim: the handler summary and the `IAuthorizationFailureCounter` summary are stale. The handler still turns a refused boundary policy into typed, metadata-only problem details, and its summary never listed the audit side effect either. The counter's "fed … from the same code path that emits `ChatBotAuthorizationFailureAuditFact`" stays true, because the boundary emits that fact, and the file still lives beside the gateway authorization seam. No harm has been named.

### Review Findings — round 17 (2026-11-03, delta + callee code review)

Code review round 17 (bmad-code-review, full mode). The user chose "Delta + callee": `git diff --ignore-cr-at-eol 9152a9e..HEAD` restricted to this story's 77 File List code paths, plus the two Recovery helpers that the new call runs. Only `19ae0ce` ("test(recovery): seed current mailbox owner grants") touched the File List: two lines at `LiveContinuityAspireE2eTests.cs:215-216` add a `RecoveryMailboxOwnerProvisioner.SeedAsync` call before the mailbox admission probe. `94a91b5` ("fix(test): use EventStore 4 platform scopes for owner setup") amended the new helper. The diff covers 3 files, +248 lines and 276 diff lines:

- `LiveContinuityAspireE2eTests.cs` (File List)
- `RecoveryMailboxOwnerProvisioner.cs` (new, not on the File List)
- `RecoveryAccessTokenProvider.cs` (adds `AcquireEventStoreValidatorAsync`, not on the File List)

R14-P1 through R14-P6 and R16-P1 through R16-P4 are still unapplied; the working tree was clean. Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and the frozen intent are unchanged. Loaded context: this spec, `epic-1-context.md` and `references/Hexalith.AI.Tools/hexalith-state-instructions.md`.

All four independent layers completed, and none failed:

| Layer | Findings |
| --- | --- |
| Blind Hunter | 10 (B1–B10) |
| Edge Case Hunter | 11 (E1–E11) |
| Verification Gap | 2 gaps (V1–V2), plus 2 other findings (V3–V4) |
| Acceptance Auditor | 8 (A1–A8) |

Parent triage of the 33 raw findings: **1 decision-needed, 8 patch, 4 defer, 9 rejected**. The remaining raw findings fold into those entries.

Verified facts:

- Production `ChatBotAdmissionEvidenceValidator.ValidateAtomicParticipationAsync` (`:372-378`) returns `CanonicalExecutionUnavailable` for every command. Its only registration is `CommandGatewayServiceCollectionExtensions.cs:79`, and `ChatBotCommandAdmissionPipeline.cs:262-265` applies it after authorization.
- The required topology lane pins a real-topology `GrantChatBotAuthority` refusal as 503 `canonical-execution-unavailable` with `retryable: false` (`TrivialGovernedCommandAspireE2eTests.cs:597-598`). While A6/A13 stay open, no ChatBot authority grant can be issued through the gateway.
- Release run `41540260256` (`e5f1664`, 2026-11-02): "required live recovery validation sweep" failed inside `SeedAsync` (`RecoveryMailboxOwnerProvisioner.cs:38`) at its first owner command. `BootstrapGlobalAdmin` answered 503 "Catalog authority cannot safely admit this operation." That is EventStore's `RouteCatalogUnavailableExceptionHandler`, whose exceptions come from route-catalog configuration mismatches, not warm-up. The seed has never completed in a hosted lane.
- `ChatBotServiceGrants:LifetimeDays` defaults to 90 (`Program.cs:406`), and CI sets 90 (`ci.yml:262`, `:402`, `:488`; `release.yml:47`, `:139`). `GovernedOperationAggregate.Handle(GrantChatBotAuthority)` rejects `ExpiresAt > now + 30 days` (`:54`), and rejects any reused grant id as `authority_issuance_reused` (`:63-66`) whatever its expiry.
- No server code reads `chatbot:service-client-grant-id` or `chatbot:service-client-grant-expiry`. Only the `ClaimsServiceClientGrantResolver` constants and test fixtures reference them.
- Local runs use the persistent `dapr init` Redis (`ChatBotAspireModule.cs:208-232`). The realm hardcodes the recovery mailbox client's grant id `01ARZ3NDEKTSV4RRFFQ69G5FB2`.
- `PartyAggregate.Handle(CreateParty)` returns `NoOp` for an existing party (`PartyAggregate.cs:722-723`), so repeating `CreateParty` is harmless.
- EventStore status is keyed by message id, with bounded correlation compatibility (`CommandStatusController.cs:62-152`). ChatBot forwards `CommandId` as the EventStore `MessageId` (`AcceptedCommandDispatcher.cs:96`), so both polling styles in the new helper resolve.
- `CommandStatus` has `TimedOut` (7) and no `Failed` value.

- [x] [Review][Decision] **R17-D1 (high): the owner seed cannot succeed in the production composition, so the live recovery lane cannot pass** — Findings V3 and B3 (retry part).
  - `SeedAsync` needs 202 for two `GrantChatBotAuthority` submissions through ChatBot's `/api/v1/commands`. The gateway refuses every authorized command with a non-retryable 503 `canonical-execution-unavailable` (see the facts above).
  - `SubmitChatBotGrantAsync` treats that 503 as retryable. It retries every 500 ms for the 2-minute budget (about 240 POSTs), then throws "grant was not accepted: status 503".
  - The probe this feeds, `AssertMailboxTokenAdmissionAsync`, proves authorization only by observing that post-authorization 503. Authorization needs a current ChatBot operation grant on the probe's intake, and that grant can only be issued through the same fail-closed gateway. Without the seed, the probe gets a non-transient 403. While A6/A13 stay open, the live mailbox proof is circular.
  - Second blocker: in the hosted release lane the seed fails even earlier, at `BootstrapGlobalAdmin`, with EventStore's route-catalog 503. That is an environment problem outside this diff.
  - Blast radius: the `release.yml` jobs "required live recovery validation sweep" and "required independent live recovery evidence gate", the scheduled CI sweep, and the story-evidence recovery step.
  - Options:
    1. Keep the seed. On the non-retryable grant 503, fail fast with a message that names the A6/A13 blocker. Apply R17-P1 through R17-P8, and defer the live-lane prerequisite.
    2. Revert `19ae0ce` and `94a91b5`, and defer the same prerequisite. The probe keeps failing at its 403.
    3. Re-scope the probe's proof to accept the typed 403 `authorization-denied` while A6/A13 are open. This weakens the proof and is not recommended.
  - **Resolved 2026-11-03:** the user chose option 1. The fail-fast became R17-P9 (applied with R17-P1 through R17-P8), and the live-lane prerequisite became R17-W5 (deferred).
- [x] [Review][Patch] **R17-P1 (medium): the service grant copies the mailbox token's grant claims** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:61] — B1, B2 (grant-id part), E1, E10 (grant-id part), E11, A1, A4 (grant-id part), A5 (root-cause part).
  - Copying the token's expiry of about 90 days hits the aggregate's 30-day cap: `invalid_authority_grant`.
  - Copying the realm's static grant id makes every rerun against the same state store fail permanently with `authority_issuance_reused`.
  - **Fix:**
    - Mint the grant id (`ChatBotCommandId.New()`) and an expiry of at most 30 days that outlives the 250-minute lane, for example `UtcNow.AddDays(1)`. Use them for both `ServiceClientGrant` and `GrantChatBotAuthority`.
    - Keep `ReadMailboxGrantMetadata`'s client, tenant and command claim checks, but stop building owner state from the grant claims.
- [x] [Review][Patch] **R17-P2 (low): the seeded grant target repeats the probe body's literals** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:24] — B7, V2, A6.
  - **Fix:** expose the probe's intake id once as an internal constant that both `MailboxAdmissionProbeBody` and the provisioner use. Add a `LiveContinuityAspireE2eContractTests` row asserting that the seeded resource and operation equal the probe body's `intakeId` and `commandType`.
- [x] [Review][Patch] **R17-P3 (low): the recovery-validator credentials are hardcoded a second time** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryAccessTokenProvider.cs:60] — B8, A7.
  - **Fix:** build on `RecoveryValidatorIdentity.CreateTokenRequestFields()` and override only `client_id`. Add the missing `<param>` and `<returns>` docs.
- [x] [Review][Patch] **R17-P4 (low): failures dropped the reference helper's diagnostics** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:140] — B4.
  - **Fix:** include the status body in the terminal-state exceptions and in the status `ShouldBe(OK)` message. Name the last status and reason in `WaitForGlobalAdministratorAsync` and in the timeout exceptions.
- [x] [Review][Patch] **R17-P5 (low): the terminal-state checks look for `Failed`, which does not exist, instead of `TimedOut`** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:138] — E4, E5.
  - A `TimedOut` command is polled for the full 2-minute budget and then reported as a misleading `TimeoutException`.
  - **Fix:** replace `Failed` with `TimedOut` in both loops.
- [x] [Review][Patch] **R17-P6 (low): the 15-minute startup deadline and its comment ignore the seed** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:171] — B5, E9, A3.
  - The comment says the deadline is "sized to the bounded work that actually runs under it". The seed adds token loops of 3 minutes each and owner-command and grant loops of 2 minutes each.
  - **Fix:** resize the deadline or give the seed its own linked budget, and update the comment.
- [x] [Review][Patch] **R17-P7 (low): the submit loops abort on warm-up transport failures** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:107] — B3 (transient part), E8.
  - After the health waits, the seed is now the first caller of the command path. The probe and the reference helper tolerate `HttpRequestException` and per-request timeouts during that window; the seed does not.
  - **Fix:** catch both with `when (!cancellationToken.IsCancellationRequested)`, within the existing budgets.
- [x] [Review][Patch] **R17-P8 (low): the class summary overstates what is seeded** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:19] — B6 (summary part), V4 (summary part).
  - The class seeds evidence only for the admission probe's fixed intake, not for the drill's own mailbox intakes. **Fix:** say so in the summary.
- [x] [Review][Patch] **R17-P9 (from R17-D1): fail fast on the canonical-execution refusal** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:187]
  - `SubmitChatBotGrantAsync` now stops on a 503 whose problem type is `canonical-execution-unavailable` and throws a message that names the A6/A13 gate, instead of retrying for 2 minutes.
  - The predicate `IsCanonicalExecutionRefusal` (`:125`) is pinned in the normal lane: other 503s, other statuses and non-problem bodies keep the bounded retry.
  - The class remarks name the blocker.
- [x] [Review][Defer] R17-W1: no normal-lane check exercises the provisioner [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:29] — deferred. Sources V1 (filed as defer), B9 and A2. A check only means something once a seeding route exists; then add a contract test that the built grants pass `GovernedOperationAggregate` validation.
- [x] [Review][Defer] R17-W2: the drill's own mailbox intakes still have no per-intake ChatBot operation authority [src/Hexalith.ChatBot.Workers/Mailbox/GraphMailboxIntakeWorker.cs:176] — deferred: pre-existing, since no grants existed before this diff. Sources B6 and V4. Those intakes would also hit `canonical-execution-unavailable`, so they are blocked on A6/A13.
- [x] [Review][Defer] R17-W3: this is the third near-copy of the owner-seeding helpers [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:154] — deferred. Source B10. Fixing it means a refactor across `TrivialGovernedCommandAspireE2eTests`, `PolicyRefusalPreflightTopology` and the provisioner. The drift is real: `94a91b5` had to fix the copied `system` tenant, and `PolicyRefusalPreflightTopology.cs:515` still uses that pre-`94a91b5` form.
- [x] [Review][Defer] R17-W4 (`maybe-false`, unverified medium): both seeded grants' principal may not match the token subject [tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryMailboxOwnerProvisioner.cs:53] — deferred. Source E2. `ChatBotRequestAuthorizer.Request` keys both the operation and the service-grant requests by `context.SubjectId`, which is the token `sub` (the service grant at `ResolveServiceGrantResolutionCoreAsync`), while the seed uses the client id for both. The realm defines no fixed service-account user for `recovery-validation-mailbox-client`. To settle it, decode a live recovery mailbox token and check its `sub`.
- [x] [Review][Defer] R17-W5: the live recovery lane cannot pass until canonical execution opens [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:215] — deferred from R17-D1 option 1. The mailbox admission proof needs a ChatBot grant, and ChatBot issues grants only through the gateway that stays fail-closed while A6/A13 are open. A second, separate environment blocker: hosted release run `41540260256` fails earlier, at `BootstrapGlobalAdmin`, with EventStore's route-catalog 503.

Code-review actions: **1 decision-needed resolved (option 1), 9 patches applied (R17-P1 through R17-P9), 5 deferred, 9 rejected**. Story 1.5 stays spec `in-review` / sprint `review`, not the generic workflow's `in-progress` or `done`.

Patch validation (2026-11-03):

- `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug -m:1` succeeded with 0 warnings and 0 errors.
- `-class …LiveContinuityAspireE2eContractTests`: 58/58 passed. That includes the 6 new rows, `MailboxOwnerSeedShouldTargetTheExactAdmissionProbeIntake` and `MailboxOwnerSeedFailsFastOnlyOnTheCanonicalExecutionRefusal` (5 rows).
- `-namespace Hexalith.ChatBot.IntegrationTests.Recovery`: 421 total, 0 failed, 2 skipped. The skips are the live Tier-3 tests, which need `HEXALITH_CHATBOT_TIER3=1`.
- The live lane itself was not run. Per R17-W5 it cannot pass yet, and the fail-fast is proven only by the normal-lane predicate.

Changed files:

- `RecoveryMailboxOwnerProvisioner.cs`: R17-P1, P2, P4, P5, P7, P8 and P9, plus the `SeedBudget` timeout that names the step it reached.
- `RecoveryAccessTokenProvider.cs`: R17-P3.
- `LiveContinuityAspireE2eTests.cs`: the shared `MailboxAdmissionProbeIntakeId` for R17-P2, and the startup deadline of 15 minutes plus `SeedBudget`, with its comment, for R17-P6. Byte-level edits keep the file's mixed line endings.
- `LiveContinuityAspireE2eContractTests.cs`: R17-P2 and R17-P9 rows.

Rejected:

- **R17-B3/E3 (`false`)** — Claim: a 503 retry reuses the command id after EventStore has definitively refused it. The 503 comes from ChatBot's admission pipeline before dispatch, so EventStore never receives the message and has no cached rejection to replay. The real defect, retrying a non-retryable 503, is R17-D1.
- **R17-A1, the 202 part (`false`)** — Claim: ChatBot answers 202 and EventStore ends the command `Rejected`. ChatBot answers 503 before dispatch, as the facts above show. The 30-day cap itself is real and is covered by R17-P1.
- **R17-B2/E10/A4, the `CreateParty` part (`false`)** — Claim: a repeated `CreateParty` fails a rerun. The Party aggregate returns `NoOp` for an existing party.
- **R17-B2/E10/A4, the `authority_grant_active` part (`low`)** — True, but it occurs only on a rerun within the 1-hour lease against the same Redis, and only after the A6/A13 gate opens. A fix would need to parse the rejection reason, which is more than a direct correction.
- **R17-B2, the cleanup part (`low`)** — Claim: seeded grants, membership and the Party are never cleaned up. With R17-P1's fresh ids, reruns no longer collide on the service grant, and the state that accumulates in the dev store is harmless. Adding cleanup would be new machinery.
- **R17-E6 (`low`)** — Claim: `WaitForCommandAsync` does not retry a transient 5xx status read. These reads follow a 202 on a warm command path, and the reference helper is just as strict. Fixing it would add branches.
- **R17-E7 (`low`)** — Claim: `SubmitOwnerAsync` fails on its first non-OK status read. EventStore writes the status record before it answers 202, and the required topology helper relies on that too. Fixing it would add branches.
- **R17-A8 (procedural)** — Claim: the callee files are missing from the File List. The fix would edit the spec under review. The callee files also come from non-story commits, the same rationale as R15-A5.
- **R17-A5, the security framing (`false`)** — Claim: owner evidence built from an unverified JWT contradicts the probe's remark. The server still validates the bearer; the test only reads its own Keycloak-issued token. The old probe was criticised for building a principal locally. The root-cause part of A5 is folded into R17-P1.

### Review Findings — round 18 (2026-11-03, R14/R16-patch review)

Code review round 18 (bmad-code-review, full mode). The user confirmed the recommended scope: only the action-item commit `8ea98fb` ("fix(authentication): propagate only request aborts from boundary audit"), which applied R14-P1 through R14-P6 and R16-P1 through R16-P4. The range is `git diff --ignore-cr-at-eol b92d2c0 8ea98fb -- src tests`: 7 files, +101/−28, 275 diff lines. Its spec check-offs are records and were not reviewed as code:

- `ChatBotApplicationBoundaryResultHandler.cs`
- `ChatBotApplicationBoundaryResultHandlerTests.cs`
- `BoundaryAuditWriter.cs`
- `BoundaryAuthorizationFailureCounter.cs`
- `CommandGatewayAdmissionApiE2ETests.cs`
- `LiveContinuityAspireE2eTests.cs`
- `LiveContinuityAspireE2eContractTests.cs`

`8ea98fb` is the only commit after `b92d2c0`, and `HEAD` equals `origin/main`. This round does not re-review the original-baseline implementation. Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and the frozen intent are unchanged. Loaded context: this spec, `epic-1-context.md` and `references/Hexalith.AI.Tools/hexalith-state-instructions.md`.

All four independent layers completed, and none failed:

| Layer | Findings |
| --- | --- |
| Blind Hunter | 10 (B1–B10) |
| Edge Case Hunter | 3 (E1–E3) |
| Verification Gap | 1 gap (V1), plus 1 other finding (V2) |
| Acceptance Auditor | no acceptance violations; 3 low findings and 1 nit (A1–A4) |

Parent triage of the 19 raw findings: **0 decision-needed, 4 patch, 1 defer, 11 rejected**. The other raw findings fold into those entries. Every surviving entry is low. The Acceptance Auditor confirmed that all ten action items are implemented as instructed, and that line endings, the File List and the status are intact.

Verified facts:

- `CommandGateway.SubmitAsync` (`:83`) still wraps `AdmitAsync` in `catch (Exception exception) when (exception is not OperationCanceledException)`. The pipeline's `DenyAsync` audit writes (`ChatBotCommandAdmissionPipeline.cs`) have no inner catch. The POST `/api/v1/commands` handler has no catch either, and ChatBot.Server registers no exception handler.
- `Program.cs:28` and `:73` register the boundary policy and the command gateway unconditionally. `CommandGatewayServiceCollectionExtensions.cs:236` is the only `IAuditWriter` registration: a singleton `ChainedAuditWriter` over in-memory stores, whose construction does no I/O.
- The live lane runs `RecoveryMailboxOwnerProvisioner.SeedAsync` before the admission probe (`LiveContinuityAspireE2eTests.cs:217-219`). While A6/A13 are open, the seed fails fast on the canonical-execution refusal, so the probe is never sent.

- [x] [Review][Patch] **R18-P1 (low): the catch comment says a request abort propagates the store's exception, but a transport failure is replaced** [src/Hexalith.ChatBot.Server/Authentication/ChatBotApplicationBoundaryResultHandler.cs:93]. Findings B1, E3, V2 and A1.
  - The comment says "A request abort propagates whatever the audit store raised for it (cancellation or a transport failure)".
  - For an `IOException` on an aborted request, the filter enters the catch. `context.RequestAborted.ThrowIfCancellationRequested()` then throws a fresh `OperationCanceledException`, and the `IOException` is dropped, not even kept as an inner exception.
  - The `transport` row of `RequestAbortedDuringAuditWriteMustPropagateWithoutResponseOrCounter` asserts `OperationCanceledException`, so the code is right and the comment is wrong. Rethrowing a fresh cancellation also matches the pipeline pattern at `ChatBotCommandAdmissionPipeline.cs:210-212` and `:279-281`.
  - **Fix:** say that a request abort propagates as the request's cancellation, whatever the audit store raised for it. Comment-only; one rewrite of the paragraph covers R18-P2.
  - Applied 2026-11-03: the catch comment now says that a request abort propagates as the request's cancellation, whatever the audit store raised for it. Comment-only; Server.Tests build 0 warnings/0 errors, handler class 23/23.
- [x] [Review][Patch] **R18-P2 (low): the gateway-parity claim now covers a case the gateway does not handle** [src/Hexalith.ChatBot.Server/Authentication/ChatBotApplicationBoundaryResultHandler.cs:95]. Findings B4 and V1 (the comment part).
  - The rewritten paragraph says "Any other failure, including the store's own cancellation, … Like the gateway's admission path, answer the typed, retryable dependency-unavailable outcome".
  - The gateway's filter (`CommandGateway.cs:83`) lets a store-raised cancellation on a live request escape as an untyped 500. Parity therefore holds only for audit failures that are not cancellations.
  - **Fix:** limit the comparison, for example "as the gateway's admission path does for a non-cancellation audit failure". Comment-only. The gateway's own behaviour is R18-W1.
  - Applied 2026-11-03: the same rewrite says "as the gateway's admission path does for a non-cancellation audit failure". Comment-only; validated with R18-P1.
- [x] [Review][Patch] **R18-P3 (low): the probe-body summary states a live outcome the lane cannot currently reach** [tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs:1109]. Finding A3.
  - The summary says "Live admission currently refuses it with the canonical-execution `503` before dispatch."
  - That 503 is applied after authorization (`ChatBotCommandAdmissionPipeline.cs:262-265`), and authorization needs the mailbox operation grant. While A6/A13 stay open, the gateway refuses every grant (R17-D1), so the seed fails fast and the probe is never sent (R17-P9, R17-W5).
  - R14-P3's instruction to name the 503 as the expected live outcome predates that finding.
  - **Fix:** make the sentence conditional, for example "Once its owner grants are seeded, live admission refuses it with the canonical-execution `503` before dispatch". It can also point to `RecoveryMailboxOwnerProvisioner` for the current seed refusal. Comment-only.
  - Applied 2026-11-03: the `MailboxAdmissionProbeBody` summary now says "Once its owner grants are seeded". It adds that while A6/A13 stay open the gateway refuses those grants too, so the seed fails fast and the probe is not sent, with a `cref` to `RecoveryMailboxOwnerProvisioner`. Comment-only. The target lines are LF and were byte-edited; the file keeps its 316 CRLF lines. IntegrationTests build 0 warnings/0 errors, `LiveContinuityAspireE2eContractTests` 58/58.
- [x] [Review][Patch] **R18-P4 (low): the store-cancellation row runs on a token that can never be cancelled, so its new liveness assertion is vacuous** [tests/Hexalith.ChatBot.Server.Tests/Authentication/ChatBotApplicationBoundaryResultHandlerTests.cs:212]. Finding B2 (the unit part).
  - `Context()` never sets `RequestAborted`, so the theory runs on `CancellationToken.None`, and `context.RequestAborted.IsCancellationRequested.ShouldBeFalse()` is always true.
  - Kestrel's `RequestAborted` is always cancellable. The row does not model the live request the R16-P1 rule is about: a store cancellation while the request is live is an outage, not an abort.
  - **Fix:** bind `context.RequestAborted` to a `CancellationTokenSource` that is never cancelled, for both rows of `AuditBackendFailureMustAnswerTypedDependencyUnavailableWithoutRetryOrEndpointEffects`. Test-only.
  - Applied 2026-11-03: both rows bind `RequestAborted` to a live, never-cancelled token and assert `CanBeCanceled` before `IsCancellationRequested`. Handler class 23/23. A throwaway mutant whose filter tests `!context.RequestAborted.CanBeCanceled` passes under the old `CancellationToken.None` setup. It fails the `store-cancellation` row here, and the handler was restored and rebuilt afterwards.
- [x] [Review][Defer] **R18-W1 (low): the gateway admission path keeps the old cancellation filter for the same denial-audit write** [src/Hexalith.ChatBot.Server/Gateway/CommandGateway.cs:83] — deferred: pre-existing and outside this delta, and latent in production. Findings V1 and B4 (the code part).
  - On POST `/api/v1/commands`, a denial raised in admission audits through `DenyAsync` with no inner catch. If the audit store raises its own `OperationCanceledException` while the request is live, it escapes `SubmitAsync` as an untyped 500. An `IOException` raised on an aborted request writes a 503 to the aborted response.
  - The boundary handler now answers the first case with the typed 503 and propagates the second, so one route and one writer have two outage contracts.
  - No test injects a denial-audit failure on the gateway path.
  - It is latent because `InMemoryAuditWriter.RecordAuthorizationFailureAsync` (`:55-63`) cannot fail and ignores cancellation.
  - **Closure:** when a durable denial-fact writer lands, give `CommandGateway.cs:83` the same `when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)` filter, with `ThrowIfCancellationRequested()`. Add a `CommandGatewayTests` row whose denial-audit double raises its own cancellation on a live token.

On 2026-11-03 the user chose option 1, "Apply every patch". Code-review actions: **0 decision-needed, 4 patches applied (R18-P1 through R18-P4), 1 deferred, 11 rejected**. Story 1.5 stays spec `in-review` / sprint `review`, not the generic workflow's `in-progress` or `done`. Current completion gates:

- the explicit owner/workflow deferrals;
- A6/A13 owner/protection evidence;
- retry-profile approval;
- the completion evidence contract.

This supersedes the round-14 and round-16 closing lists, which still name the applied R14/R16 items and the retired "65 current Server failures".

Patch validation (2026-11-03):

- `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1` succeeded with 0 warnings and 0 errors. The same build of `Hexalith.ChatBot.IntegrationTests.csproj` also succeeded with 0 warnings and 0 errors.
- `-class …Authentication.ChatBotApplicationBoundaryResultHandlerTests`: 23/23 passed.
- `-class …Recovery.LiveContinuityAspireE2eContractTests`: 58/58 passed.
- The full Server lane was not rerun. The production change is comment-only, and only the handler test class changed.

Changed files: `ChatBotApplicationBoundaryResultHandler.cs` (R18-P1, R18-P2), `LiveContinuityAspireE2eTests.cs` (R18-P3) and `ChatBotApplicationBoundaryResultHandlerTests.cs` (R18-P4). All of them are already on the File List.

Rejected:

- **R18-B3 (`false`)** — Claim: resolving the counter and clock after the durable write can turn a recorded denial into an untyped 500. `AddChatBotCommandGateway` registers `ISystemClock` and the counter together (`CommandGatewayServiceCollectionExtensions.cs:297`, `:315`; `Program.cs:73`). `InMemoryAuthorizationFailureCounter.Record` has no throwing path for a real clock instant. The test comment's "throw" describes a mutant that feeds a `default` stamp. This is the same as R16-B5/E2.
- **R18-B5 (`false`)** — Claim: a missing writer surfaces only on the first denied POST. The shipped host always registers both the boundary policy and the only `IAuditWriter` (`Program.cs:28`, `:73`), so it cannot reach that state. The new test pins only that a broken composition fails loudly. Constructor injection would be more than a direct correction.
- **R18-E1 (`false`)** — Claim: an outage raised while the writer is resolved escapes as an untyped 500. The writer is a singleton `ChainedAuditWriter` built by primary constructors over in-memory stores (`CommandGatewayServiceCollectionExtensions.cs:227-236`). Resolution does no I/O and so has no outage path.
- **R18-B6/E2 (`low`)** — Claim: an audit outage, on the 503 path or under an abort, leaves no log, inner exception or alert. This is true but latent, because `InMemoryAuditWriter` cannot fail. It is the same as R16-B3/E7/A4: adding a logger dependency is more than a direct correction. Revisit with R18-W1's durable-writer trigger.
- **R18-B7 (`false`)** — Claim: neighbouring summaries keep the stop point that R14-P3 fixed. The `MailboxAdmissionProbeIntakeId` summary speaks of an *admitted* probe, which does fail in dispatch planning. `ServerBootstrapApiTests.CommandEndpointShouldAuthenticateBeforeContractValidationAndValidateBeforeAdmission` describes its in-process `AuthenticatedFactory` composition, where it asserts `DispatchUnavailable`. That does not contradict the live predicate, which runs against the production validator.
- **R18-B8 (`low`)** — Claim: "before any external write" hides the backstop's residue (idempotency reservation, prepared dispatch, replay intent and alert). The backstop cannot be reached live while `ValidateAtomicParticipationAsync` refuses every command and the seed fails fast. The wording is R14-P4's deliberate alignment with the in-process copy.
- **R18-B9 (`false`)** — Claim: `StatusCode.ShouldNotBe(503)` lets another status pass, and the cancellation row should pin the store's own exception instance.
  - Every typed result path writes a JSON problem body, which `Body.Length.ShouldBe(0)` forbids.
  - `DefaultHttpContext.Response.HasStarted` is always false, so asserting it would pin nothing.
  - The exception instance is not part of the contract: the pipeline pattern also rethrows a fresh cancellation.
- **R18-B10 (`low`)** — Claim: the theories pick their exceptions with two-way ternaries on magic strings, and `Services` ignores `clock` when no counter is given. No row or caller is affected today. A throwing `switch` or an independent clock registration would add branches for a hypothetical typo.
- **R18-B2, the HTTP-row part (`low`)** — Claim: the HTTP-level 503 test needs a store-cancellation row. The handler alone decides between 500 and 503, and the unit row pins that decision (a cancellation that escapes fails `DenyAsync`). The branch is latent in production.
- **R18-A2 (procedural)** — Claim: the round-14 and round-16 closing paragraphs still say the items "stay unchecked" and list them as completion gates. Fixing them would edit the spec's history. This round's record restates the current gates instead.
- **R18-A4 (`low`)** — Claim: R16-P2's recorded mutant ("without the `when` filter") does not fail against the shipped catch. This is true: because the catch starts with `ThrowIfCancellationRequested()`, the filter's abort clause only preserves the original exception instance. The recorded mutant ran against the pre-patch catch. The R16-P2 behaviour, no 503 for an abort during the write, is still pinned, because the pre-patch catch fails the `transport` row. Correcting the record would edit the spec's history.

### Review Findings — round 19 (2026-11-03, R18-patch review)

Code review round 19 (bmad-code-review, full mode). The user confirmed the recommended scope: only the round-18 patch commit `3d8293a` ("test(authentication): apply story 1.5 review round 18 patches"). The range is `git diff --ignore-cr-at-eol 8ea98fb 3d8293a -- src tests`: 3 files, +14/−9, 65 diff lines. Nothing on `src` or `tests` changed after `3d8293a`; `HEAD` (`9b2d3ab`) only moves 13 submodule pointers. The commit's spec and ledger records were not reviewed as code:

- `ChatBotApplicationBoundaryResultHandler.cs` (R18-P1, R18-P2)
- `LiveContinuityAspireE2eTests.cs` (R18-P3)
- `ChatBotApplicationBoundaryResultHandlerTests.cs` (R18-P4)

Baseline `5bb56567499c6df1c621184d3c3d35aa45521166` and the frozen intent are unchanged. Loaded context: this spec, `epic-1-context.md` and `references/Hexalith.AI.Tools/hexalith-state-instructions.md`.

All four independent layers completed, and none failed:

| Layer | Findings |
| --- | --- |
| Blind Hunter | 8 (B1–B8) |
| Edge Case Hunter | 3 (E1–E3) |
| Verification Gap | none |
| Acceptance Auditor | no acceptance violations; 2 nits (A1, A2) |

Parent triage of the 13 raw findings: **0 decision-needed, 3 patch, 0 defer, 10 rejected**. Every surviving entry is low. The Acceptance Auditor confirmed that R18-P1 through R18-P4 are applied as recorded, that the only `src` change is a comment, and that line endings, the File List and the status are intact.

Verified facts:

- The gateway's admission path does not answer every non-cancellation audit failure the same way. The outcome depends on which write fails and where:
  - Denial audits raised before the outcome block (`ChatBotCommandAdmissionPipeline.cs:86-318`, outside the classifier and status-lookup `try` blocks) escape to `CommandGateway.SubmitAsync:83` and answer `CreateDependencyUnavailable` (`dependency_degraded`).
  - A precommit audit failure (`:350`, `:380`) or the lapsed-authority denial audit at `:404` is caught at `:409` as `Rejected(AuditUnavailable)`, which `CommandGateway.Denied` (`:326-328`) answers with `CreateAuditUnavailable` (`audit_unavailable`).
  - The lapsed-authority denial audit at `:343` is caught at `:409` as `idempotency_outcome_unavailable`.
- Two rows pin the metadata-read deadline filter (`ChatBotApplicationBoundaryResultHandler.cs:152`, `catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)`):
  - `StalledMetadataReadMustExpireWithOneSafeFactAndTypedDenial` runs on `CancellationToken.None`.
  - `RequestAbortedDuringMetadataReadMustPropagateWithoutAudit` runs on a cancelled token.
  - A `!cancellationToken.CanBeCanceled` mutant passes both.
- `ValidateAtomicParticipationAsync` (`ChatBotAdmissionEvidenceValidator.cs:372-379`) still refuses every profile from a single method body.
- No `src` code compares the token of an `OperationCanceledException` by identity, and no `src` comment cites a review-ledger ID.

- [x] [Review][Patch] **R19-P1 (low): the narrowed gateway-parity clause still claims a parity the gateway does not have** [src/Hexalith.ChatBot.Server/Authentication/ChatBotApplicationBoundaryResultHandler.cs:96]. Finding E3.
  - The comment says the handler answers dependency-unavailable "as the gateway's admission path does for a non-cancellation audit failure".
  - Only the gateway's early denial audits answer dependency-unavailable. A precommit audit failure, or the lapsed-authority denial audit inside the outcome block, answers `audit_unavailable` or `idempotency_outcome_unavailable` (see the verified facts).
  - A reader closing R18-W1 would take the clause as gateway-wide parity.
  - **Fix:** delete the comparison, so the sentence ends at "answer the typed, retryable dependency-unavailable outcome". The gateway side stays tracked as R18-W1. Comment-only.
  - Applied 2026-11-03: the comparison is deleted, and the paragraph now ends "answer the typed, retryable dependency-unavailable outcome. The denied request has no effect." Comment-only.
- [x] [Review][Patch] **R19-P2 (low): the store-cancellation row does not model the client timeout its comment names** [tests/Hexalith.ChatBot.Server.Tests/Authentication/ChatBotApplicationBoundaryResultHandlerTests.cs:205]. Finding B6.
  - The row throws `new OperationCanceledException("restricted-audit-backend")`, whose token is `CancellationToken.None`. An `HttpClient` timeout instead raises a `TaskCanceledException` with an inner `TimeoutException`, carrying its own cancelled token.
  - A filter mutant that classifies by the exception's own token (`oce.CancellationToken.IsCancellationRequested`) passes every current row. In production that mutant would turn a store timeout on a live request into an untyped 500, which breaks the R16-P1 rule.
  - **Fix:** in that row, throw `new TaskCanceledException("restricted-audit-backend", new TimeoutException(), new CancellationToken(canceled: true))`. Test-only. The row still models a store-raised cancellation on a live request.
  - Applied 2026-11-03: the row throws that `TaskCanceledException`, and its comment now names the store's own, already cancelled token. A throwaway mutant whose audit filter classifies by the exception's own token (`exception is not OperationCanceledException oce || !oce.CancellationToken.IsCancellationRequested`) passes all 23 rows at `3d8293a` and fails this row.
- [x] [Review][Patch] **R19-P3 (low): the stalled-read row still runs on a token that can never be cancelled** [tests/Hexalith.ChatBot.Server.Tests/Authentication/ChatBotApplicationBoundaryResultHandlerTests.cs:45]. Finding B4.
  - R18-P4 bound a live token only for the audit-outage rows. `StalledMetadataReadMustExpireWithOneSafeFactAndTypedDenial` still runs on `CancellationToken.None`, so its `IsCancellationRequested.ShouldBeFalse()` is vacuous.
  - A `!cancellationToken.CanBeCanceled` deadline-filter mutant survives every row (see the verified facts). Kestrel's token is always cancellable, so in production that mutant would turn every stalled read into an untyped failure instead of the ratified bounded deadline (R15-D1).
  - **Fix:** as R18-P4 did, bind `context.RequestAborted` in that row to a live `CancellationTokenSource` that is never cancelled, and assert `CanBeCanceled`. Test-only.
  - Applied 2026-11-03: the row binds `RequestAborted` to a live, never-cancelled token and asserts `CanBeCanceled` before `IsCancellationRequested`. A throwaway `!cancellationToken.CanBeCanceled` deadline-filter mutant passes all 23 rows at `3d8293a` and fails this row.

On 2026-11-03 the user chose option 1, "Apply every patch". Code-review actions: **0 decision-needed, 3 patches applied (R19-P1 through R19-P3), 0 deferred, 10 rejected**. Story 1.5 stays spec `in-review` / sprint `review`, not the generic workflow's `done`. The completion gates restated in round 18 are unchanged:

- the explicit owner/workflow deferrals;
- A6/A13 owner/protection evidence;
- retry-profile approval;
- the completion evidence contract.

Patch validation (2026-11-03):

- `python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1` succeeded with 0 warnings and 0 errors.
- `-class …Authentication.ChatBotApplicationBoundaryResultHandlerTests`: 23/23 passed.
- Sensitivity check: both throwaway mutants were applied to the handler together.
  - The patched class failed exactly the two target rows: the `store-cancellation` row and `StalledMetadataReadMustExpireWithOneSafeFactAndTypedDenial`.
  - The `3d8293a` class passed 23/23 against the same mutants.
  - The patched handler and tests were then restored byte for byte and rebuilt, and the class passed 23/23 again.
- The full Server lane was not rerun: the production change is comment-only, and only the handler test class changed. IntegrationTests were not rebuilt, because no file in that project changed.

Changed files: `ChatBotApplicationBoundaryResultHandler.cs` (R19-P1) and `ChatBotApplicationBoundaryResultHandlerTests.cs` (R19-P2, R19-P3). Both are already on the File List, and both are still fully CRLF (188 and 323 lines).

Rejected:

- **R19-B1/E2 (`false`)** — Claim: "a request abort propagates as the request's cancellation" is wrong when the store raises a cancellation on an aborted request, because the store's own exception then propagates with its own token. What propagates is still a cancellation caused by the request abort, which is what the sentence says. The claimed harm needs a caller that compares token identity. No `src` code does that, and ASP.NET Core classifies aborts by the request's state.
- **R19-B2/A1 (`maybe-false`, would be `low`)** — Claim: "Once its owner grants are seeded, live admission refuses it with the canonical-execution `503`" names a precondition that rules out its own outcome, because the gate that refuses the grants is the same one that would produce the 503. Whether that holds depends on how A6/A13 open: the gate could be lifted for every profile at once or one profile at a time, or a seeding route outside the gateway could appear (R17-W1). The live predicate accepts both outcomes, and the summary's backstop sentence covers admission past the refusal. Revisit with R17-W5.
- **R19-B3/A2 (`false`)** — Claim: the `AssertMailboxTokenAdmissionAsync` remarks (`LiveContinuityAspireE2eTests.cs:946`) still state the outcome that R18-P3 made conditional. The remarks describe the response that a probe reaching admission gets, which matches production today: `ValidateAtomicParticipationAsync` refuses every profile. They do not claim that the lane sends the probe.
- **R19-B5 (`false`)** — Claim: the new `CanBeCanceled` assertion checks the arrange step, not the handler. That is its purpose: it fails if a later edit drops the live-token binding the row depends on. It has no other effect.
- **R19-B7 (`low`)** — Claim: the parity sentence should point to deferred R18-W1. No `src` comment cites a review-ledger ID; the ledger entry is where it is tracked. R19-P1 deletes the sentence anyway.
- **R19-B8 (`low`)** — Claim: R18-P3 adds LF lines to a file that `.editorconfig` requires to be CRLF. The mixed endings predate the change (316 CRLF lines out of 1,161), and R18-P3 kept the edited block's existing endings. No build or format gate enforces line endings. Normalising the whole file would rewrite about 845 lines, which is not a direct correction to this delta.
- **R19-E1 (`low`)** — Claim: an audit outage on the 503 path leaves no log or alert. Same as R16-B3/E7/A4 and R18-B6/E2: latent, because `InMemoryAuditWriter` cannot fail, and adding a logger dependency is more than a direct correction. Revisit with R18-W1's durable-writer trigger.

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

### Current implementation verification (2026-11-01)

Verified canonical HEAD `2ac8c4b5bc87cc253c92ca60e806a86ab0948e99` on `main` from a clean working tree. Loaded this complete spec and both frontmatter context files before inspection. All execution tasks and accepted patches through R12-P3 are already present; no production or test-source changes were needed. This pass adds only this verification record and ignored local results. Original baseline `5bb56567499c6df1c621184d3c3d35aa45521166`, frozen intent, explicit deferrals, spec/sprint `in-progress` and Story 1.6's literal `review` are preserved.

Required build/runtime commands:

```bash
python3 .github/scripts/with-source-mapping.py aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj
python3 .github/scripts/with-source-mapping.py dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false
python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1
```

All three exited **2** during source preparation with `HXW009`: the pinned Builds file `Props/Directory.Packages.props` at `74c1bc5ca68bc9856a5fa7d06323f60a0b48b604` cannot be read from the local `references/Hexalith.Builds` checkout. Logs, in command order: `/tmp/story-1-5-implementation-runtime.log`, `/tmp/story-1-5-implementation-solution-build.log`, `/tmp/story-1-5-implementation-server-build.log`. Aspire did not start. `aspire ps --format Json --non-interactive` returned `[]`; subsequent `aspire describe --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json --non-interactive` reported no running ChatBot host. The historical mailbox-secret startup failure was not reached this run.

The focused fallback ladder retained the source-mapping guard:

```bash
dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1 /nr:false
dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -p:HexalithBuildsRoot=/home/administrator/projects/hexalith/chatbot/references/Hexalith.Builds -m:1 /nr:false
```

Both exited **1**, each with one error and zero warnings. The first reports `MSB4019` for the absent nested Conversations Builds import; the second reaches Conversations' explicit refusal, `Conversations requires the Platform tool-selected source mapping; validation does not resolve dependency mappings.` Logs: `/tmp/story-1-5-implementation-server-fallback.log` and `/tmp/story-1-5-implementation-server-root-builds-fallback.log`. No nested submodule was initialized, no dependency or mapping guard was changed, and stale Server binaries were not executed as current evidence.

Independent Contracts and Client fallbacks built and ran current source:

```bash
dotnet build tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1 /nr:false
dotnet build tests/Hexalith.ChatBot.Client.Tests/Hexalith.ChatBot.Client.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -m:1 /nr:false
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests TestResults/story-1-5-implementation/contracts.ctrf.json -result-trx TestResults/story-1-5-implementation/contracts.trx
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests TestResults/story-1-5-implementation/client.ctrf.json -result-trx TestResults/story-1-5-implementation/client.trx
```

Both builds exited **0**, zero warnings/errors. Full lanes exited **0**: Contracts **531 passed**, Client **141 passed**, **672 total**, zero failures/skips. Build/test logs: `/tmp/story-1-5-implementation-contracts-build.log`, `/tmp/story-1-5-implementation-client-build.log`, `/tmp/story-1-5-implementation-contracts-tests.log`, `/tmp/story-1-5-implementation-client-tests.log`. Current CTRF/TRX results are at the exact paths above. These independent fallback results do not establish the verified full source graph.

Current Server, full matrix/acceptance, Architecture/Conformance and native persisted-end-state evidence could not be produced. Earlier results and failures remain historical disclosures, not current passes. Source preparation is the immediate verification blocker; A6/A13 owner/protection acceptance, retry-profile approval, existing explicit deferrals and the completion evidence contract remain separate open gates. The story stays `in-progress`; no done transition, attestation, runtime activation, commit or push is proposed.

### Resumed implementation verification after source preparation recovery (2026-11-01)

Rechecked the complete spec and its two frontmatter context files against unchanged implementation HEAD `2ac8c4b5bc87cc253c92ca60e806a86ab0948e99`. All execution tasks and accepted patches through R12-P3 are present; no production or test-source correction was necessary. This continuation appends verification evidence only. Earlier failed preparation/fallback attempts above remain historical; the recovered source preparation now permits the required mapped build and current test execution. The original baseline, frozen intent, explicit deferrals and Story 1.6 are preserved. Story status is left for the parent workflow to reconcile; no done transition or runtime approval is inferred.

Required solution command:

```bash
python3 .github/scripts/with-source-mapping.py dotnet build Hexalith.ChatBot.slnx --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false
```

Exit **0**, **0 warnings/errors**, elapsed **2m17s**; log `/tmp/story-1-5-resumed-solution-build.log`. Each full lane then ran separately with `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.<Lane>.Tests/bin/Debug/net10.0/Hexalith.ChatBot.<Lane>.Tests TestResults/story-1-5-resumed-implementation/<lane>.ctrf.json -result-trx TestResults/story-1-5-resumed-implementation/<lane>.trx`, using lowercase result names. Exact argument arrays, environment, exit codes, counts, failure names/messages and log/result paths are retained in `TestResults/story-1-5-resumed-implementation/lane-results.json`.

| Current full lane | Passed | Failed | Skipped | Exit |
| --- | ---: | ---: | ---: | ---: |
| Contracts | 531 | 0 | 0 | 0 |
| Client | 141 | 0 | 0 | 0 |
| Server | 3477 | 65 | 0 | 1 |
| Cli | 61 | 0 | 0 | 0 |
| Architecture | 167 | 0 | 0 | 0 |
| Conformance | 151 | 0 | 0 | 0 |

Total **4528 passed, 65 failed, 0 skipped**. The earlier Architecture/Conformance failures did not recur in this current run. The complete failed Server lane remains required failing evidence; focused success does not replace it. `server-failure-groups.json` in the same results directory retains every exact failed case name, file/line, message and trace, grouped as follows:

- **62 SDK HTTP-host integration failures:** `ServerBootstrapApiTests` (17), `CommandGatewayTests` (39), `TrustedAuthorityReReviewTests` (1) and `TrustedAuthorityReviewTests` (5). Current `/process` and `/project` responses are HTTP 503 where the fixtures require HTTP 200 result envelopes (or 404 for the unknown projection); the malformed-command-payload fixture receives an empty non-JSON error body. Representative exact failures are `ServerBootstrapApiTests.ProcessEndpointShouldRejectUnauthenticatedTransportWithValidEnvelope` (through `ReadDomainServiceResultAsync`, line 3499), `ProjectEndpointShouldReturnNotFoundForUnknownDomain`, `ProcessEndpointShouldRejectMalformedCommandPayloadWithoutEchoingPayload`, and `CommandGatewayTests.SyntheticSdkPolicyWritesMustDenyEvenAnAuthenticSignedMarker`. These observations require HTTP-host/fixture reconciliation; no expectation or runtime gate was changed.
- **2 hub authentication failures:** `ChatBotProjectConversationHubE2ETests.AiResponseProgressProjectionShouldBroadcastTenantScopedChangeSignalToHubSubscribers` and `UnauthenticatedJoinShouldFailClosedWhenJwtAuthenticationIsConfigured` receive HTTP 401 during their configured connection setup.
- **1 existing R12-W1 failure:** `CommandGatewayAdmissionApiE2ETests.CommandGatewayApi_ShouldRejectUnauthenticatedSubmissionBeforeDispatch` expects one authorization-failure audit fact and observes zero. This is current execution evidence for the already recorded HTTP-boundary denial-audit deferral; it is not silently resolved or duplicated.

The documented direct-runner fallback separately passed **181/181**, zero skips, exit **0**:

```bash
XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests TestResults/story-1-5-resumed-implementation/server-core.ctrf.json -result-trx TestResults/story-1-5-resumed-implementation/server-core.trx -class '*CommandOperationContractTests' -class '*CommandOperationConcurrencyAdapterTests' -class '*OperationStatusMonotonicityTests' -class '*OperationStatusWorkflowStatusSinkTests'
```

The focused result and exact command are retained in `focused-results.json`; log `/tmp/story-1-5-resumed-server-core-tests.log`. Both changed R12 bootstrap regression methods pass in the full Server result. The parent's independent `parent-matrix-audit.json` resolves all **16** methods named by the Matrix evidence/acceptance section to **38 passing current TRX cases**, including immutable replay, competing revisions, terminal retry, historical identities, missing/forbidden parity and fail-closed canonical requests. This is a local audit, not completion provenance or native-store evidence.

Runtime baseline command `python3 .github/scripts/with-source-mapping.py aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited **0** after a successful mapped Debug build. `aspire wait chatbot-bhvxwdeb --timeout 45 --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive` exited **0**, reporting healthy after 16 seconds; ChatBot and its UI were healthy in the subsequent exact-target describe. The same snapshot records `eventstore-pjwuuxpb`, `eventstore-admin-yntkgsqd` and `tenants-hvcjafmf` as **Finished**, each with exit **134**. Their causes are absent from the retained CLI diagnostics, so full-topology health is not claimed. Sanitized state is retained at `/tmp/story-1-5-resumed-runtime-resource-summary.json`; startup/wait logs are `/tmp/story-1-5-resumed-runtime.log` and `/tmp/story-1-5-resumed-runtime-wait.log`, with CLI diagnostics `/home/administrator/.aspire/logs/cli_20261101T170757722_detach-child_ea88738e83394fcf94e036eaf9d466a9.log`. No secret was fabricated, and the earlier mailbox-secret startup failure did not recur.

Exact-target `aspire stop --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive` exited **0** before the solution build; final exact-target describe confirmed no running ChatBot host. Stop log: `/tmp/story-1-5-resumed-runtime-stop.log`. No topology was restarted during verification. Native persisted-end-state/owner execution evidence, A6/A13 acceptance, retry-profile approval and the existing explicit owner/workflow deferrals remain open, separately from the 65 current Server failures.

`git diff --exit-code HEAD -- src tests .github Directory.Build.props Directory.Build.targets Directory.Packages.props global.json references` returned **0**; generated client bytes still match the existing SHA-256 fixture. CRLF-aware `git diff --check` passed. No dependency, submodule checkout, nested initialization, branch, index, commit or push changed in this implementation continuation.

### Source-object recovery and parent closeout (2026-11-01)

HXW009 was caused by an absent local Git object, not a missing working-tree file. `git cat-file -e 74c1bc5ca68bc9856a5fa7d06323f60a0b48b604^{commit}` in the root-declared Builds submodule initially exited 128. Fetching that exact revision from origin also exited 128 (`upload-pack: not our ref`); `git fetch --no-recurse-submodules --tags origin` succeeded but still did not provide it. An existing local Builds object store contained the exact commit and catalog, so this command recovered its reachable objects without moving any checkout:

```bash
git -C references/Hexalith.Builds fetch --no-recurse-submodules /home/administrator/projects/hexalith/platform/references/Hexalith.Builds 74c1bc5ca68bc9856a5fa7d06323f60a0b48b604
```

Exit **0**. The recovered catalog blob is `5a8b113853766e1deda3f4112bc1924001133a0d`. No nested submodule was initialized, updated or used as a new workspace dependency. The source store was read only. `python3 .github/scripts/with-source-mapping.py python3 -c 'print("Source preparation succeeded")'` then exited **0**, selecting mapping hash `278da9e99e3150d7181a4972f5a729fd1220d5fca564b0fb4f5602c9ef627bb8`. The producer still reports advisory HXW011 (no local Platform release tag) and HXW009 (the root Builds checkout differs from Platform's catalog pin); these do not prevent preparation. No guard, tool pin or dependency pin was changed. Exact operations and preservation checks are retained in `TestResults/story-1-5-resumed-implementation/dependency-object-recovery.json` and `/tmp/story-1-5-unblock-source-preparation-after-builds.log`.

A bounded second runtime inspection used the current built outputs and the same verified mapping: `python3 .github/scripts/with-source-mapping.py aspire start --no-build --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited **0** and the exact ChatBot health wait exited **0**. Console logs identify the three exit-134 causes omitted from the first CLI diagnostics: EventStore cannot bind `http://127.0.0.1:8080`, EventStore admin cannot bind `http://127.0.0.1:8090`, and Tenants cannot bind `http://127.0.0.1:61448`, each reporting address already in use. The same host was stopped successfully; final exact-target describe confirms none running. Other port owners were not stopped. Sanitized exact commands, exits and causes are retained in `TestResults/story-1-5-resumed-implementation/runtime-blockers.json`; detailed local console logs are under `/tmp/story-1-5-unblock-runtime-diagnosis-*.logs.json`. This supplies diagnostics, not native owner/persisted-end-state evidence.

Parent verification confirms the full solution build and five passing lanes, all 65 disclosed Server failures, the 181-case focused pass and the independent 16-method/38-case matrix pass above. No failed test was suppressed or relabeled. Root HEAD, index gitlinks, every root-declared submodule HEAD/branch/working-tree status and every nested-submodule initialization state match the initial snapshot. The incoming user's verification section and the frozen block are preserved byte for byte. Only this story record, the append-only deferred ledger and Story 1.5's sprint stage were updated; local machine results remain ignored.

Story 1.5 is spec `in-review` / sprint `review` following the completed review. Story 1.6 remains literal `review` in both artifacts. Completion remains blocked by the **65 current Server failures** (62 SDK HTTP/result-body cases, two hub authentication cases and R12-W1), the **three runtime port conflicts**, missing accepted **A6/A13 owner/protection evidence**, **retry-profile approval**, existing explicit owner/workflow decisions, and the **Story 1.5 completion contract/current bound primary evidence** (no Story 1.5 evidence contract or new attestation is present). The earlier Architecture/Conformance and missing-mailbox-secret failures did not recur. The generic build workflow's done/commit instructions are superseded by the user's explicit no-commit/no-push request and preserved completion gates: no done transition, completion attestation, runtime activation, commit or push is made. **Superseded 2026-11-02 (round 15, R15-P2):** the 65-failure blocker no longer reproduces. The full Server lane passed 3643/3643 at `18f8301` and 3645/3645 after the round-15 patches; the failing run above remains historical evidence. The other completion gates listed here stand; the three runtime port conflicts were not re-verified this round.

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
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eContractTests.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs`
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
- `src/Hexalith.ChatBot.Server/Authentication/ChatBotApplicationBoundaryResultHandler.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Authentication/BoundaryAuditWriter.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Authentication/BoundaryAuthorizationFailureCounter.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Authentication/ChatBotApplicationBoundaryResultHandlerTests.cs`
