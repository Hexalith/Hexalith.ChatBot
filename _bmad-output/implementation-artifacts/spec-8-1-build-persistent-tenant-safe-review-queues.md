---
title: Build persistent tenant-safe review queues
type: feature
created: '2026-11-22'
status: done
baseline_commit: 034dd5f8a687087ebcc62aa57d0598f566b321fb
route: dispatch
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-8-context.md'
---

<frozen-after-approval reason="human-owned intent">

## Intent

An authorized reviewer can find work that still needs attention after replay or restart. A committed decision closes the same item that the request opened. A tenant operator can distinguish an empty queue from an unavailable or incomplete projection.

## Boundaries & Constraints

Use the six existing queue families. Bind tenants and sequence numbers to trusted EventStore metadata. Apply current Project and resource authority before disclosing detail. Use committed events for item state, history and terminal outcomes. Keep worker observations and telemetry separate from durable facts. Use a governed per-tenant minimum of distinct subjects for aggregate-only counts. Until a current, approved minimum is available, counts are unavailable. Apply suppression to narrow buckets and any total, age or change that would reveal a suppressed bucket. The build includes the missing failure and recovery event lifecycles in the workflows that own ingestion, attachments and retries, as approved on 22 November. Filtering, claiming, notification routing and the S10 workspace have their own stories.

## I/O & Edge-Case Matrix

| Given | When | Then |
| --- | --- | --- |
| An approval request and its decision | A reader restarts and an older request arrives again | One logical approval remains closed. Its request, outcome and decision time remain reconstructable. |
| Concurrent or delayed deliveries | Two consumers process the same subject | The applicable source version wins within its stream. Duplicates preserve the original transition. |
| Two tenants or Projects reuse a business ID | An authorized reader opens one scope | Each subject keeps its own state. Inaccessible targets disclose no identifying data. |
| An operator holds aggregate access | A summary is requested | Only permitted depth, age, owner, freshness and family/state buckets are eligible. Small groups and derived totals cannot reveal an item. |
| A source or checkpoint is missing, behind or rebuilding | Someone requests queue health | Return the affected scope, known source version, responsible role and safe next action. A healthy empty result requires affirmative source coverage. |

</frozen-after-approval>

## Code Map

- In `src/Hexalith.ChatBot.Server/Projections/`, both project-conversation stores read approval event history. `ApprovalQueueItemBuilder` currently maps each request independently, which retains an open row after its decision.
- `ChatBotDomainProjectionHandler` consumes committed SDK envelopes. The association, participant, approval and AI-execution translators supply subject facts. Use stream-scoped versions. One participant resolution batch can emit multiple subjects.
- `Lifecycle/Attachments/AttachmentCaptureCoordinator.cs` and `Lifecycle/Workflows/IngestionBindingWorkflowRunner.cs` expose operational outcomes without complete committed queue lifecycles. `FailureStateProjectionHandler` has no runtime caller. Retry request acceptance alone cannot prove successful recovery.
- `Queries/ChatBotReadQueryHandler.cs` and `Authorization/ChatBotAuthorityCatalog.cs` enforce current read authority. `AdminQueueSummaryProjector.Create` currently includes item references. The audit checkpoint measures audit progress and cannot certify queue coverage.
- `tests/Hexalith.ChatBot.Server.Tests/Gateway/Status/AtomicOperationStatusReadModelStore.cs` exercises serialized EventStore read-model writes. Integration recovery fixtures provide separate native-store probes.

## Tasks & Acceptance

- [x] Establish the approval regression through both conversation stores and the persisted approval event path. Fold the latest state per tenant, Project and approval before selecting pending work. Preserve retained requests and decisions across restarts.
- [x] Extend the owning ingestion, attachment and retry workflows with trusted, idempotent EventStore transition commands/events. Record actionable failure, retry, exhaustion and confirmed recovery with stable identity and causal ordering. An accepted command or a local exception alone cannot establish a committed resolution. Reuse internal workflow admission and handle side-effect replay at the owning boundary.
- [x] Extend the named projection to verified source events for all six families. Materialize tenant-scoped subject history and an idempotent current state through the EventStore read-model SDK. Compare versions within the producing stream. Repair any interrupted indexing on redelivery. Handle explicit terminal and superseding facts.
- [x] Wire aggregate summary and authorized item reads through the existing authorization and query catalog. Enforce the governed disclosure policy, audit aggregate reads, and expose unavailable coverage with its reason. Feed authorized internal consumers and metadata-only telemetry from the same state.
- [x] Exercise restart, duplicate delivery, reversed order, concurrent writes, identical IDs across tenants and Projects, small-group suppression and unavailable sources. Verify persisted end-state through an independent native store read and authenticated API reads before marking the story complete.

Given a committed event for each family, when consumed through its production entry point, then the stored record carries a stable subject, entered time, state, reason, ownership, assignment, risk/confidence, retry and terminal facts, next action, version, operation and correlation as supplied by that source. Given a completed transition, when replayed, then its exact outcome and time remain attributable. Given metadata-only telemetry, when emitted, then payload and evidence remain undisclosed.

## Verification

Run the individual Server and Integration test projects with the .NET 11 SDK. Exercise SDK projection delivery, a restarted reader, CAS redelivery, two authorized tenants and the native store. Record local test-double evidence separately from independent Dapr state and API checks.

## Implementation Notes

The baseline package-mode Server test project built cleanly on .NET 11. A new regression failed in both store implementations before the read fix. Folding by Tenant, Project and approval before selecting pending rows preserves the terminal decision on redelivery. Stable queue identity is distinct from the versioned conversation event identity. The owning Server and Workers test projects use Release package mode in this workspace. The SDK executable is at `/workspace/.hexalith-works-tools/dotnet/dotnet`. Source compilation is also documented in the repository README.

A native Dapr 1.19.2 / Redis test ran the request, decision and delayed redelivery in three separate processes. The restarted approval reader returned zero open items, two history events and zero events for the other tenant. An independent Redis read confirmed the approved outcome, decision time and source version 9. This probe exercised the approval handler and read model.

The named SDK projection also received the approval request, decision and delayed request in separate processes using the native sidecar. It retained one closed subject with two transitions. A separate Kestrel run read that persisted history through the SDK workload route and the human GET route. Short-lived signed bearer tokens went through the production bearer validator. The owner-authority source was a test fixture. Project revocation and a second tenant denied detail, and the aggregate operator received no counts without a policy.

Native Dapr and Redis probes applied the ingestion and attachment aggregate events in reverse order across two Projects sharing an identical business ID. New readers observed all four subjects closed at version three. Each retained a separate retry history. Direct reads of Redis found eight subject records. The recurring native regression in the Integration project uses `HEXALITH_CHATBOT_TEST_DAPR_GRPC_ENDPOINT` to select an isolated sidecar. Both cases passed locally.

The solution compiled in Release with the current sibling worktrees and the Platform revision pinned by Chatbot. Source-scoped HTTP tests returned the observed head, owner and replay action when a checkpoint was missing. Missing source evidence returned an unavailable status. The checked-in unit and native suites exercised duplicate and concurrent deliveries, interrupted indexing, two-tenant isolation, shared IDs across Projects, and authorized bucket suppression. Without an authoritative tenant-wide stream inventory the coverage field continues to report partial scope. The policy activation gate still requires its existing A6 evidence.

The final Release solution build succeeded, and all 3,669 Server tests passed with zero skips in the serial run. One route-catalog check had failed in an earlier run concurrent with a build. A separate source-mapped Debug build used Chatbot’s pinned references and passed all 21 native Folders attachment tests. The contract (531), generated client (141), worker (32), CLI (61) and conformance (151) suites passed. The dedicated native CI run passed both cases with zero skips. The recovery sandbox confirmed the bytes it stored and passed the attachment replay check.

The full pinned integration run had 19 failures out of 555 cases, with two existing gated skips. All 19 failures reproduced on the unchanged Chatbot baseline. The UI suite retained its streaming-disposal failure. The architecture suite reported the existing Folders package-version conflict. Tests of external owner handoffs used controlled owner adapters. No deployed Folders or Memories credentials were used.

## Spec Change Log

On 22 November the requester approved expanding the producer work and selected a governed per-tenant disclosure minimum. Counts remain unavailable until the tenant approves a value.

## Review Triage Log

| Reviewer and finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind 1, upgrade loses pending approvals | High | The periodic input switches immediately to the new family index when Dapr is configured. Retained approval events can exist with no entries in that index. Patch the upgrade path. |
| Blind 2, permanent Folders failure offers retry | High | The writer classifies storage failures by attempt number. Candidate reads exclude the owner's unavailable state. Patch the classification and targeted retry. |
| Blind 3, accepted upload loses confirmation opportunity | High | A successful legacy upload has no content confirmation, yet the persisted storage outcome can remove the candidate. Patch reconciliation. |
| Blind 4, finalization retry revisits earlier source failures | Medium | A finalize-only retry loops over every source from generation zero. Deterministic operation IDs protect against duplicate creation but still revisit prior failure state. Patch phase resumption. |
| Blind 5, unmatched intent changes the current row | High | The join applies retry-requested before testing the owner's current head and operation class. Patch ordering. |
| Blind 6, a regressed stream can pass coverage | High | Checkpoints validate the observed prefix. Previously projected rows ahead of that prefix were unchecked. Patch source-scoped verification. |
| Blind 7, summary loses the recovery action | Medium | A failed check clears subjects but leaves the generic tenant-inventory reason. Patch with a coarse unavailable response that preserves suppression. |
| Blind 8, stale Project rows remain actionable | High | The handler returns rows already collected when the subsequent source check detects lag. Patch disclosure on stale coverage. |
| Blind 9, empty participant actions imply Link | Medium | The default enum value from FirstOrDefault produces a concrete action. Patch the empty-list case. |
| Blind 10, reopened item age includes closed time | Medium | The minimum over all earlier opening facts can precede a later closure. Patch the current open interval. |
| Blind 11, history versions are ambiguous across producers | Medium | History sorts by stream ID, while the serialized entries omit the stream. Patch authorized history ordering and provenance. |
| Blind 12, stale proposal poisons subsequent reads | High | Admission did not check the current snapshot on submission. Replay returned immediately at a stale pending proposal. Patch admission and replay. |
| Blind 13, native test bypasses the owner execution path | Medium | The test materializes aggregate events locally and sends them to the real projection. It covers native storage but cannot establish the retry-owner handoff. Add owner handoff evidence. |
| Edge 1, certified empty stream requires native proof | Medium | The installed SDK omits NativeProof unless its reader explicitly requests it. Patch the queue reader. |
| Edge 2, whole-tenant emptiness cannot be certified | Medium | The existing EventStore inventory is best effort. An unknown tenant-wide result is appropriate without authoritative enumeration. Preserve that limit and improve the scoped and aggregate explanations. |
| Edge 3, stale or mismatched retry masks owner state | High | The join writes a current transition before checking the failure. Patch with Blind 5. |
| Edge 4, nonretryable Folders failure remains retryable | High | The owner retry disposition is available on the storage response. Patch with Blind 2. |
| Edge 5, missing capture candidate reports successful rejoin | High | The owner activity discards the capture coordinator's result. Patch the exact target check. |
| Edge 6, retry may cross generations | High | The coordinator uses the greater of candidate and request versions. That can advance the generation of a targeted retry. Patch the target precondition. |
| Edge 7, invalid manifests bypass exhaustion | Medium | The manifest-validation catch records failure outside the shared attempt-limit path. Patch the ceiling. |
| Edge 8, missing allowed actions invent Link | Medium | The empty array produces the enum default. Patch with Blind 9. |
| Edge 9, reopened work inherits old age | Medium | A previous opening fact controls the age of the new incident. Patch with Blind 10. |
| Edge 10, stale snapshot proposal can poison replay | High | Old proposals can be written before the reader detects the mismatch. Patch with Blind 12. |
| Edge 11, row precondition collides across streams | Medium | Current-version comparison alone cannot distinguish two sources at the same sequence number. Patch the authorized revision check. |
| Edge 12, periodic worker loses Project group | Medium | The old approval builder produced GroupProjectRef for the escalation rule. The new subject conversion omitted it. Patch with the upgrade path. |
| Verification 1, commit gate lacks a concrete-writer regression | High | Capture tests inject a recording writer and native tests build the fact directly. Exercise a receipt followed by absent and then matching stream evidence. |
| Verification 2, rejoin after retry-start lacks an execution test | High | The join test checks the instance ID and manually applies a recovery. Exercise the activity against unchanged and superseding owner heads. |
| Verification 3, recurring CI skips native cases | Medium | The native test requires its explicit sidecar endpoint and the ordinary merge lane permits skips. Run the dedicated native checks with a disposable sidecar. |
| Verification 4, unavailable attachment offers a dead-end retry | High | The coordinator writes retryable after the owning Folders operation refused retry. Patch with Blind 2 and Edge 5. |
| Verification 5, incompatible operation class creates pending work | High | The queue transition precedes the operation-class check. Patch with Blind 5. |

### Review resolution

- The upgraded reader merges retained approvals with the named queue, including their Project. A legacy pending request remains visible immediately after upgrade, and a later committed decision closes it. A new incident receives its own entry time. Participant records with no permitted action offer escalation.
- The retry join validates the event, owner version, generation and operation class before changing an item. An unconfirmed Folders upload stays available for reconciliation. Permanent refusal closes the retry path. Finalization retries reuse confirmed ingestion results, and invalid manifests reach the same attempt limit as other failures.
- Scoped reads verify the authoritative prefix against the persisted checkpoints and subjects. A regressed stream or incomplete projection suppresses stale rows. Multi-stream detail includes the producing stream for each transition and checks the opaque row revision. Suppressed or unverified aggregate data returns a coarse recovery action.
- Stale policy proposals are rejected. Tests cover rotation, rejection and contradictory activation. With no approved minimum, the API and queue metrics withhold counts. Tenant-wide completeness still requires an authoritative inventory, and policy activation still depends on the existing A6 gate.
- The merge runner now starts an isolated Dapr and Redis deployment and requires both native cases to pass. They exercise concrete commit confirmation and owner rejoin after restart with controlled owner adapters. A signed HTTP test also read the persisted approval history through the generated client.
