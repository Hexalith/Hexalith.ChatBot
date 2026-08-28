---
title: 'Bind the live Hexalith.Memories derived-store backing'
type: 'feature'
created: '2026-08-28'
status: in-review
baseline_commit: '5b5d2e3c34d9e395994d7750ea5487adf7cc2ac3'
baseline_revision: '5b5d2e3c34d9e395994d7750ea5487adf7cc2ac3'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.Memories/_bmad-output/project-context.md'
warnings: [oversized]
deferred: [DW-137]
---

<intent-contract>

## Intent

**Problem:** Production ChatBot still resolves `IDerivedStore` and `IVectorReindexer` to process-local implementations, so the nightly isolation probe and correction path do not exercise Hexalith.Memories. The four `DerivedStoreClass` values are metadata-only governance/probe taxonomy and do not map one-to-one to Memories' syntactic, raw-semantic, natural-language-semantic, and graph records. Candidate-ranking/evidence/proposal snapshots are ChatBot-owned immutable derived state, not Memories records. Memories owns the real Redis-Vector/FalkorDB schema but exposes no supported diagnostic-probe store or source-versioned correction workflow.

**Approach:** Add two distinct Memories-owned tenant-scoped service/client capabilities: (1) metadata-only CRUD/enumeration for a dedicated diagnostic namespace whose four logical categories preserve the ChatBot `DerivedStoreClass` governance/probe taxonomy without creating or impersonating canonical Memories records; and (2) a start-or-rejoin plus status correction workflow over canonical Memories case/memory-unit identities. Case authority is explicit: ChatBot obtains the authorization-filtered Project Context through the supported Hexalith.Projects client and requires exactly one included memory reference for the initially associated Project and, separately, for a correction target Project; that reference's opaque `ReferenceId` is the sole authoritative prior or corrected Memories case id. A Project id is never a Memories case id. A ChatBot-owned Dapr ingestion-binding workflow—not an EventStore projection side effect or a new background service—starts or rejoins after an accepted association identifies `(tenantId, associationId, intakeId, associatedProjectId, sourceVersion)`. Its activities resolve the prior case from Projects, fetch the exact source message and every governed attachment through production content-source adapters, call Memories ingestion with deterministic source/idempotency identity per `(tenantId, intakeId, recordKind, ordinal)`, and durably poll a new additive `MemoriesClient.GetIngestionWorkflowStatusAsync` operation to terminal success. The validated terminal statuses are the sole source of the canonical `MemoryUnitId` results. Before transient ingestion payload cleanup, Memories promotes the exact raw source bytes/event JSON that drove each message or attachment derivation, together with the resolved provider/model/dimension and generation configuration, into a tenant-scoped durable source artifact governed by the same retention/deletion lifetime as that MemoryUnit. Only after every individual message/attachment ingestion succeeds does the ChatBot workflow call a separate finalize-binding operation with `(tenantId, associationId, intakeId, sourceVersion, priorCaseId, expectedAttachmentCount)` and the complete ordered entries `(recordKind, ordinal, memoryUnitId)`. The manifest contains exactly one `Message` entry at ordinal 0 and exactly `expectedAttachmentCount` unique `Attachment` entries at ordinals 1 through N in the captured provider order; Memories validates every entry and its durable source artifact, rejects count/order/identity/tenant/case failures, and atomically publishes exactly one binding. Correction preserves every MemoryUnit id: the existing durable ChatBot correction workflow resolves the correction target Project's exactly one included memory reference and carries its `ReferenceId` as a distinct pre-existing corrected case id, while Memories takes the prior case and ordered unit set from the binding, validates the corrected case in the same tenant, migrates case fields/edges/source-URI dedup mappings, and regenerates all derivatives from the durable artifacts. ChatBot production binds `IDerivedStore` to the diagnostic-probe client and `IVectorReindexer` to the separate correction-workflow client, durably awaits terminal correction status, and retains in-memory implementations only as explicit deterministic development/test defaults. Candidate-ranking correction remains ChatBot's existing supersede-and-re-evaluate-forward path.

## Boundaries & Constraints

**Always:** Keep Memories authoritative for its persisted `MemoryUnit` content, exact durable raw source artifacts plus resolved generation configuration, tenant-scoped finalized association/intake-to-case/memory-unit bindings, schema, tenant validation, embeddings, graph data, migrations, durable operation status/deadlines, and infrastructure clients. A durable source artifact MUST NOT use the transient 24-hour workflow-payload lifetime; it remains available for correction until the governed retention/deletion policy removes its MemoryUnit and binding, at which point deletion cascades to the artifact. Keep Projects authoritative for the authorization-filtered Project-to-Memories-case reference and require exactly one included memory reference whenever a prior or corrected case must be resolved. Keep ChatBot authoritative for governed association/intake/correction/project identity, durable ingestion-binding orchestration, the expected ordered message/attachment manifest supplied at binding finalization, and immutable candidate-ranking/evidence/proposal snapshots. The ingestion-binding workflow MUST durably start or rejoin each individual ingestion, poll supported status to terminal success, validate returned tenant/case/unit identity, persist enough safe workflow state to resume after restart, and call finalization exactly once only after the complete ordered set succeeds. Use tenant-first physical partitions and metadata-only diagnostics. Preserve fail-closed audit-before-alert probe behavior, source-version idempotency with per-MemoryUnit fences plus an intake-level convergence fence, cancellation, and the existing `ReplayTenantPolicy`/safe-token rules. New C# types use one file per type and public contracts are additive/versioned.

**Block If:** The authorization-filtered Project Context is unavailable/stale, does not match the governed Project, or contains zero or more than one included memory reference when resolving either the initially associated prior case or the correction target case. Block if a production message/attachment content adapter is unavailable, any expected payload is absent/unreadable/unauthorized, an ingestion cannot reach validated terminal success, its terminal status lacks a canonical `MemoryUnitId` or reports the wrong tenant/case, or the complete provider-ordered result set cannot be resumed after restart. The Memories-owned finalized ingestion binding must resolve exactly one correction tenant/association/intake to its prior case identity, retained durable source artifact/configuration, and complete ordered set of canonical message/attachment MemoryUnit ids; do not guess from `{associationId}:{priorProjectId}`, substitute any Project id for a case id, infer a unit id from provider/folder/source identifiers, or call digest replacement a vector reindex. Binding finalization fails atomically on an absent/unreadable artifact, duplicate or missing ordinal, count mismatch, unknown record kind, stale source version, or MemoryUnit whose tenant/prior case differs. Correction fails before scheduling when the binding is missing, ambiguous, stale, incomplete, or cross-tenant, or when the Projects-resolved corrected case is absent, not pre-existing, or not owned by the same tenant; failure leaves ingestion/binding/correction retryable as applicable. Block if Memories cannot regenerate every native derivative while preserving MemoryUnit ids, migrate case fields/edges/source-URI dedup mappings, remove prior-case artifacts, or prove Redis-Vector/FalkorDB readiness, durable per-unit and intake-level version ownership, the 60-minute terminal deadline, and live backend identity without secrets/payloads.

**Never:** Treat the four `DerivedStoreClass` values as Memories schema families or production data records; invent prompt-context/candidate-ranking stores in Memories merely to preserve their names; let diagnostic records escape the dedicated probe namespace; reference the non-packable Memories Server from ChatBot; copy/reflection-load `IndexSchemaDefinitions`; construct Redis/FalkorDB clients in ChatBot product code; write metadata-only hashes into canonical vector/memory-unit namespaces; treat `PriorProjectId` or `CorrectedProjectId` as a Memories case id; derive a case or unit id from association, project, provider, folder, attachment, or source-URI identity; run ingestion/finalization from a projection handler; use the ChatBot tenant/class ledger for correction version ownership in Production; silently fall back to in-memory in Production; weaken zero-coverage, cleanup, or breach release gates; acknowledge scheduled ingestion or correction work as complete; add a second background service; or edit `sprint-status.yaml`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Live diagnostic CRUD | Safe tenant/logical-class/resource identity and metadata-only probe entry | Memories writes, reads, enumerates, and idempotently deletes only the tenant-owned dedicated diagnostic partition; all four logical categories receive non-zero coverage without creating or mutating canonical Memories records | Invalid/unknown tenant, class, diagnostic schema, or backend readiness fails closed with a structured metadata-only error |
| Cross-tenant probe | Sentinel in an owner diagnostic partition, read through intruder scope | Intruder observes nothing; per-run fenced cleanup reaches terminal deletion for every sentinel; returned evidence is metadata-only and reports non-zero four-category coverage | Leakage, seed/read/delete failure, residual probe artifacts, backend failure, or zero coverage is an auditable stop-ship breach |
| ChatBot-owned candidate state | Corrected association/intake and immutable candidate/evidence/proposal snapshots | Existing tenant-partitioned EventStore/DAPR projections supersede and re-evaluate forward; no Memories record-kind mapping is fabricated | Missing tenant isolation or supersession evidence remains fail-closed and cannot be counted as Memories coverage |
| Project case resolution | Authorization-filtered Project Context for the initially associated Project or correction target Project | Supported Projects client returns the governed Project with exactly one included memory reference; its opaque `ReferenceId` is carried separately as the prior or corrected Memories case id | Unavailable/stale/mismatched context, zero or multiple included memory references, unauthorized/invalid reference, or absent/cross-tenant Memories case fails closed and remains retryable; Project id is never substituted |
| Ingestion completion and binding finalization | Accepted association plus exact message payload, captured-provider-ordered attachment payloads, tenant/association/intake/source version, Projects-resolved prior case, and expected attachment count | One durable ChatBot workflow starts/rejoins each Memories ingestion using deterministic per-source identity, polls supported ingestion status to validated terminal success, records the returned canonical ids as one message at ordinal 0 plus attachments at ordinals 1..N, and calls finalization exactly once; Memories then proves every unit and durable raw source/config artifact belong to the tenant and prior case before atomically publishing one complete binding | Missing content, nonterminal/failed/invalid ingestion status, restart/duplicate loss, duplicate/order/count mismatch, missing artifact/unit, stale version, unknown kind, cross-case, or cross-tenant entry causes no binding publication and remains retryable |
| Correction reindex | Tenant, correction/source version, association/intake, correction target Project, and its Projects-resolved pre-existing corrected case id | ChatBot carries Project and case as distinct identities; Memories resolves prior case and the ordered units exclusively from the finalized binding, preserves MemoryUnit ids, rebuilds linked syntactic/raw-semantic/natural-language-semantic/graph derivatives from exact durable source artifacts/configuration, migrates case fields/edges/source-URI dedup mappings, removes prior-case artifacts, advances every per-unit fence and then the intake convergence fence only after full convergence, and returns terminal evidence | Missing/incomplete linkage, unresolved/ambiguous Project memory reference, or invalid corrected case fails before scheduling; duplicate/older version whose intake fence is terminal is an idempotent no-op; partial/backend failure stays retryable and advances neither per-unit nor intake fence |
| Concurrent/restarted work | Duplicate delivery, ChatBot/Memories restart, or multiple replicas | Ingestion-binding starts/rejoins deterministic per-source ingestions and persists the ordered terminal results before one finalization; correction start-or-rejoin uses one deterministic intake operation key scoped by tenant, association, intake, correction, source version, and corrected case. Per-MemoryUnit fences prevent repeated effects and the intake fence becomes terminal only when all ordered units converge. The durable ChatBot workflows poll status with durable timers, while Memories owns durable ingestion/correction resumption and the correction's 60-minute terminal deadline | Stale ownership is recoverable; terminal failure is queryable; no duplicate ingestion, binding, correction effect, or cross-run probe deletion; ChatBot never reports completion for merely scheduled work, and there is no separately acknowledged in-flight correction set for a ChatBot sweep |
| Development mode | Live binding explicitly disabled outside Production | Existing in-memory seams remain deterministic | Production without valid live configuration fails startup |

</intent-contract>

## Code Map

- `references/Hexalith.Memories/src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs:21` -- internal schema authority; expose supported behavior through new contracts/endpoints, not this type.
- `references/Hexalith.Memories/src/Hexalith.Memories.Server/Activities/Indexing/{IndexSemanticActivity,IndexGraphActivity,TenantIdGuard}.cs` -- reuse real vector/graph write and tenant-validation paths.
- `references/Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/` -- distinct additive contracts for four-category metadata-only diagnostic CRUD/enumeration and for the canonical MemoryUnit correction start/status workflow; do not expose canonical derived-record CRUD.
- `references/Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` -- supported remote client boundary; add cancellable methods and structured errors.
- `references/Hexalith.Memories/src/Hexalith.Memories.Server/Endpoints/` -- authenticated tenant-scoped correction start/status and diagnostic CRUD/enumeration endpoints; server resolves the ingestion binding and persisted source material, confines probe records to the dedicated namespace, and owns cleanup plus durable workflow/version state.
- `references/Hexalith.Memories/src/Hexalith.Memories.Aspire/{HexalithMemoriesServerExtensions,HexalithMemoriesSearchIndexServerResources}.cs` -- compose and expose complete Redis-Vector/FalkorDB/server resources without leaking connections to ChatBot.
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/IDerivedStore.cs:29` -- metadata-only four-class seam becomes the production diagnostic-probe adapter contract; it remains governance/probe vocabulary and is not the canonical Memories schema.
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/{IVectorReindexer,IVectorReindexLedger,InMemoryVectorReindexer}.cs` -- preserve in-memory defaults; production `IVectorReindexer` separately adapts the Memories correction workflow and never uses the tenant/class ledger for durable version ownership.
- `references/Hexalith.Projects/src/Hexalith.Projects.Contracts/Models/ProjectMemoryReference.cs` and the supported Projects client `GetProjectContextAsync` surface -- authoritative tenant-filtered mapping from each governed Project to exactly one included Memories case reference; use `ReferenceId`, never Project id, for prior/corrected case authority.
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/` plus the supported message/attachment content-source adapters -- add one ChatBot-owned Dapr ingestion-binding workflow, separate from projections and periodic/background services. Start/rejoin it after accepted association, resolve the prior case through Projects, fetch the exact message and captured-provider-ordered attachments, start/rejoin each Memories ingest using deterministic identity, durably poll terminal ingestion status for canonical unit ids, and finalize only the complete ordered binding. Memories promotes each exact raw ingestion payload/configuration to a governed durable artifact before transient cleanup.
- `references/Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` -- add cancellable ingestion-status retrieval returning the existing safe `IngestionWorkflowStatus`; ChatBot must not infer completion or unit identity from workflow/source identifiers.
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/{CorrectionPropagationRequest,CorrectionPropagationActivityRequest,CorrectionPropagationRunStoreActivity}.cs` -- resolve the correction target Project's exactly one included memory reference and carry `CorrectedCaseId` separately from `CorrectedProjectId` through the durable correction workflow; Memories resolves the prior case and canonical linked MemoryUnit refs exclusively from its finalized binding; accept the M2 activity set.
- `src/Hexalith.ChatBot.Server/Audit/DerivedStoreIsolationProbeCoordinator.cs:96` -- use the Memories-backed `IDerivedStore` diagnostic adapter in Production, fence every probe run, await deletion, and make cleanup failure and zero four-category coverage fail closed.
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs:131` and `src/Hexalith.ChatBot.Server/Program.cs:66` -- default `TryAdd` registrations and explicit validated production replacement.
- `src/Hexalith.ChatBot.AppHost/Program.cs:27` -- add full Memories topology and pass only the server endpoint/configuration to ChatBot.
- `src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/{M2SweepJobs,PeriodicEnforcementRuntime}.cs` -- keep the existing nightly probe scheduling; correction completion remains inside the existing durable correction workflow, whose durable polling/deadline path leaves no separately acknowledged in-flight work for DW-65 to sweep.
- `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs:275` -- existing two-tenant M2 release path; extend with backend identity, non-zero coverage, cleanup, correction, and persisted end-state assertions.
- `_bmad-output/implementation-artifacts/deferred-work.md:546` -- reconcile DW-63/DW-65/DW-70 truthfully; leave unrelated process-local WORM/alert/outbound items open.
- `docs/adrs/{derived-store-cross-tenant-isolation,correction-driven-vector-reindexing}.md` -- retire only deferrals proven by live evidence and record the synchronous-vs-durable-job decision.

## Tasks & Acceptance

**Execution:**
- `references/Hexalith.Memories/src/Hexalith.Memories.Contracts/V1/DerivedStores/` -- add additive tenant-scoped metadata-only diagnostic CRUD/enumeration, atomic finalize-binding, and correction start/status contracts with logical probe class, ordered record kind/ordinal/MemoryUnit identity, source version, prior/corrected case authority, deterministic intake operation identity, and safe evidence; expose no canonical derived-record CRUD.
- `references/Hexalith.Memories/src/Hexalith.Memories.Server/{Endpoints,Activities/Indexing,Infrastructure,Workflows}/` -- implement authenticated tenant-first diagnostic storage in a dedicated noncanonical namespace with fenced terminal cleanup; promote exact raw source/config artifacts before transient cleanup and retain them for the governed MemoryUnit lifetime; atomically finalize the ordered ingestion binding; implement start-or-rejoin correction that preserves MemoryUnit ids, resolves prior case and ordered units from the binding, validates the caller's pre-existing corrected case, migrates case fields/edges/source-URI dedup mappings, regenerates syntactic/raw-semantic/NL-semantic/graph state, removes prior-case artifacts, enforces the 60-minute deadline, and commits all per-unit fences followed by the intake convergence fence only after convergence.
- `references/Hexalith.Memories/src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` and its tests -- expose cancellable supported methods and verify wire/error compatibility.
- `references/Hexalith.Memories/src/Hexalith.Memories.Aspire/` and its topology tests -- expose/compose every required live resource while keeping infrastructure connections inside Memories.
- Projects/ChatBot/Memories ingestion integration -- through the supported Projects client require exactly one included memory reference for the initially associated Project and use only its `ReferenceId` as `PriorCaseId`. Add a ChatBot-owned Dapr ingestion-binding workflow that starts/rejoins after accepted association, fetches exact message and attachment content through production adapters, starts/rejoins Memories ingestion with deterministic identity per `(tenantId, intakeId, recordKind, ordinal)`, durably polls the supported ingestion-status client method to validated terminal success, persists the returned canonical unit ids in message-then-provider-attachment order, and only then calls the separate finalize-binding contract. Finalize with governed `(associationId, intakeId)`, source version, prior case, expected attachment count, and exactly one message at ordinal 0 plus attachments at ordinals 1..N; publish atomically only after Memories proves every unit and durable source/config artifact is present, same-tenant, same-case, correctly ordered, and complete.
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/` -- add a production Memories-backed diagnostic `IDerivedStore` adapter and a separate Memories correction adapter that sends governed identity, starts or rejoins the deterministic operation, and uses durable workflow timers/status polling until terminal; preserve in-memory defaults only for explicit local/test mode.
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/` and aggregate/activity-catalog tests -- resolve the correction target Project through the supported Projects client, require exactly one included memory reference, and propagate its `ReferenceId` as `CorrectedCaseId` separately from `CorrectedProjectId`; never pass a Project id or synthetic `{associationId}:{priorProjectId}` identity as a Memories case. Memories resolves prior case and canonical refs from its finalized binding. Admit the required M2 activity set and report correction completion only for terminal success/idempotent no-op; terminal failure follows the existing fail-closed delay/audit/alert path.
- `src/Hexalith.ChatBot.Server/Audit/DerivedStoreIsolationProbeCoordinator.cs` and focused tests -- exercise all four logical categories through the Memories-backed diagnostic adapter in Production, require unique fenced run identity, await terminal cleanup, and classify cleanup failure/residuals/zero coverage as stop-ship; never present diagnostic records as canonical Memories derivatives.
- `src/Hexalith.ChatBot.Server/{Gateway/CommandGatewayServiceCollectionExtensions.cs,Program.cs}` -- validate configuration; select live adapters in Production with no fallback and retain explicit local/test defaults.
- `src/Hexalith.ChatBot.AppHost/{Program.cs,DaprComponents/}` plus AppHost/architecture tests -- compose Memories Redis Stack, FalkorDB, server, Dapr components, references, readiness, and safe configuration.
- `src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/` -- retain the existing probe scheduler and record/test that the durable correction workflow owns status polling, the M2 deadline, and failure alerting across restarts; because the correction is never completed at schedule time, no independently acknowledged work remains for a DW-65 periodic sweep.
- `tests/Hexalith.ChatBot.{Server,Architecture,Conformance,AppHost,Integration}Tests/` -- cover both DI modes; zero/multiple/stale/unavailable/cross-tenant Project memory references; Project-id/case-id separation; missing/unauthorized message or attachment content; complete message-plus-provider-ordered-attachment ingestion/status/binding; no finalization before all terminal-success unit ids are present; restart/duplicate recovery and exactly-once finalization; missing-binding fail-closed behavior; four-category diagnostic cross-tenant denial; proof that diagnostic CRUD never touches canonical namespaces; terminal cleanup; per-source-version fencing; correction terminal status/deadline; no-leak responses; topology; ChatBot-owned candidate supersession; and retained state-store/backend end-state.
- `Directory.Build.props`, affected `.csproj` files, and `Hexalith.ChatBot.slnx` -- add conditional source/package references without inline versions; keep Release/package consumers valid.
- `docs/adrs/{derived-store-cross-tenant-isolation,correction-driven-vector-reindexing}.md`, `README.md`, `_bmad-output/implementation-artifacts/{deferred-work.md,spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md}` -- record evidence, residuals, commits, and exact file list without touching the orchestrator-owned status file.

**Acceptance Criteria:**
- Given a production-configured ChatBot topology, when the host starts, then `IDerivedStore` resolves to the Memories-backed metadata-only diagnostic adapter and `IVectorReindexer` resolves to the distinct Memories correction-workflow adapter, the in-memory store/ledger/reindexer cannot be selected in Production, Redis-Vector/FalkorDB/server resources are healthy, and missing/invalid live configuration stops startup rather than selecting memory.
- Given two authenticated test tenants, when all four logical probe categories are seeded, read, enumerated, and deleted through the live diagnostic binding, then the foreign tenant observes no identifier or payload, records remain confined to the Memories-owned diagnostic namespace, canonical memory-unit/vector/graph records are unchanged, cleanup is terminal with no residual artifacts, and release evidence reports non-zero four-category coverage. Candidate-ranking/evidence/proposal isolation is asserted separately against ChatBot's tenant-partitioned immutable projection store and is never reported as Memories-native coverage.
- Given an accepted association whose associated Project Context has exactly one included memory reference, when the durable ChatBot ingestion-binding workflow starts or rejoins, then it uses that reference's `ReferenceId`—never the Project id—as `PriorCaseId`, fetches the exact source message and every governed attachment through production adapters, starts/rejoins each Memories ingest using deterministic source identity, durably polls supported ingestion status to validated terminal success, and treats those statuses as the sole source of canonical `MemoryUnitId` results. It finalizes exactly once only after collecting one message at ordinal 0 and every unique attachment at ordinals 1..N in captured provider order; Memories then atomically records exactly one `(tenant, association, intake, sourceVersion, priorCase)` binding only after proving every MemoryUnit and its exact durable raw source/config artifact is same-tenant and same-case. Unavailable/stale/mismatched Project Context, zero/multiple included memory references, missing/unauthorized content, nonterminal/failed/invalid ingestion status, duplicate/order/count mismatch, missing artifact/unit, stale version, restart, cross-case, or cross-tenant input publishes no incomplete binding and remains retryable.
- Given that finalized binding and a correction target Project whose authorization-filtered Context has exactly one included memory reference resolving a pre-existing same-tenant corrected case, when the durable correction path runs twice and across ChatBot/Memories restarts, then ChatBot carries `CorrectedProjectId` and that reference's `ReferenceId` as distinct identities, every existing MemoryUnit id is preserved, all linked syntactic/raw-semantic/NL-semantic/graph derivatives and source-URI dedup mappings belong to the corrected case and reflect the exact retained source plus newest configuration/version exactly once, prior-case fields/edges/artifacts are absent, older/duplicate work is a terminal no-op, every per-unit fence and then the intake convergence fence advance only after full convergence, and persisted end-state—not only HTTP status—proves completion. Missing, ambiguous, stale, incomplete, or cross-tenant binding; unavailable/stale/mismatched Project Context; zero/multiple included memory references; or an absent/cross-tenant corrected case fails before the operation starts and remains retryable.
- Given the nightly Story 12.14 probe, when the Memories diagnostic store leaks, is unavailable, cleanup fails, or four-category coverage is zero, then the existing metadata-only audit-before-alert path produces a stop-ship verdict without cross-tenant confirmation.
- Given the Memories operation is started or rejoined asynchronously, when ChatBot correction propagation runs, then Memories durably owns restart recovery, queryable status, and the 60-minute terminal timeout/failure transition while the existing durable ChatBot correction workflow polls with durable timers and does not mark the vector activity complete until terminal success/idempotent no-op; therefore DW-65 records that no separately acknowledged in-flight correction exists for an independent ChatBot periodic sweep.

## Spec Change Log

- 2026-08-28 -- Human resolution: chose the four ChatBot classes as metadata-only governance/probe categories in a dedicated Memories-owned diagnostic namespace, plus a separate Memories-owned durable correction workflow awaited to terminal completion. Chose full-fidelity Memories-owned correction: exact raw source/config artifacts survive transient cleanup for the governed MemoryUnit lifetime; a separate atomic finalization binds the complete ordered message/attachment set to the prior case; correction takes a same-tenant pre-existing corrected case from ChatBot, preserves MemoryUnit ids, and commits per-unit fences followed by the intake convergence fence.
- 2026-08-28 -- Human escalation resolution: chose Projects-authoritative case mapping plus a dedicated ChatBot Dapr ingestion-binding workflow. The initially associated and correction target Projects must each resolve through authorization-filtered Project Context to exactly one included memory `ReferenceId`; Project ids never substitute for Memories case ids. ChatBot durably fetches exact source content, starts/rejoins and polls every Memories ingestion to terminal canonical unit identity, then finalizes the complete message-at-0/attachments-at-1..N binding exactly once.

## Review Triage Log

- 2026-08-28 -- CRITICAL dev escalation resolved by human choice of option 1. The previously unnamed ingestion producer is one ChatBot-owned Dapr ingestion-binding workflow; Projects is the sole case-mapping authority through exactly one included memory `ReferenceId`; supported terminal Memories ingestion status is the sole unit-id authority. Re-drive must replace the attempted `CorrectedProjectId`-as-`CorrectedCaseId` wiring and must not attach ingestion/finalization to projection or folder-storage paths.

## Design Notes

The live boundary is service/client based and split by purpose. The `DerivedStoreClass` values remain metadata-only governance/probe categories and MUST NOT be mapped onto canonical Memories records. A production `IDerivedStore` adapter persists those diagnostic records only in a dedicated Memories-owned tenant partition and proves four-category cross-tenant isolation plus terminal deletion; this is live diagnostic-store evidence, not evidence that the four labels are Memories-native derivative types. Candidate-ranking/evidence/proposal state remains ChatBot-owned immutable derived state.

Real correction regeneration is a separate Memories-owned durable workflow. Before each successful ingestion deletes its transient payload, Memories promotes the exact raw source bytes/event JSON and resolved generation configuration to a tenant-scoped durable artifact with the governed MemoryUnit's retention/deletion lifetime. Once every message and attachment ingest returns, ChatBot submits a separate expected ordered manifest; Memories validates the tenant, prior case, record kind, ordinal, MemoryUnit, artifact, completeness, and source version before atomically publishing one association/intake binding. On correction ChatBot supplies governed tenant/association/intake/correction/project identity, source version, and a pre-existing corrected case id, but never Redis documents, vectors, or raw source content. Memories takes the prior case and ordered unit set exclusively from the binding, validates the corrected case is same-tenant, preserves the MemoryUnit ids, migrates case fields/edges/source-URI dedup mappings, and regenerates syntactic, raw-semantic, natural-language-semantic, and graph derivatives from the exact durable artifacts/configuration. A deterministic intake operation owns per-unit source-version fences and advances the intake convergence fence only after every unit and prior-case cleanup converge. This decision supersedes the direct four-class-to-`IndexSchemaDefinitions` mapping and metadata-digest rebuild assumptions in the Story 9.5/9.6 ADRs; implementation updates those ADRs accordingly.

Case mapping and ingestion completion have one authoritative route. ChatBot reads the authorization-filtered Project Context through the supported Projects client and accepts a Project as a Memories case authority only when it has exactly one included memory reference; the opaque `ReferenceId` is the case id. The initially associated Project supplies `PriorCaseId` to the ingestion-binding workflow, and the correction target Project separately supplies `CorrectedCaseId` to correction propagation. A Project id is never reused as either value. After accepted association, one ChatBot-owned Dapr workflow fetches source content through production message/attachment adapters, starts or rejoins deterministic per-source Memories ingestions, and durably polls the supported ingestion status until each terminal success returns its canonical `MemoryUnitId`. It persists the ordered safe results and invokes finalization exactly once only when the complete message-at-0 and captured-provider-order attachments-at-1..N manifest is available. Projection handlers may materialize metadata views but do not perform this orchestration.

The remote correction call is not one long HTTP request. ChatBot starts or rejoins a deterministic Memories workflow, then the existing durable correction workflow polls queryable status with durable timers until terminal success, idempotent no-op, or terminal retryable failure. Memories durably owns restart recovery and the 60-minute terminal timeout/failure transition; ChatBot maps that terminal evidence to its existing audit-before-alert path. Scheduled work is never acknowledged as completed, so DW-65 needs no separate ChatBot overdue-work sweep.

## Verification

**Commands:**
- `dotnet build references/Hexalith.Memories/Hexalith.Memories.slnx --configuration Release -m:1` -- expected: zero warnings/errors.
- Run affected Memories Contracts/Client/Server/Integration test projects individually -- expected: live Redis-Vector/FalkorDB isolation, regeneration, delete, restart, and error tests pass.
- `dotnet restore Hexalith.ChatBot.slnx -p:UseHexalithProjectReferences=true && dotnet build Hexalith.ChatBot.slnx --configuration Release --no-restore -m:1` -- expected: zero warnings/errors in source mode.
- Build Server/Architecture/Conformance/AppHost test projects, then run their xUnit v3 assemblies with `-parallel none` and focused `-class` filters -- expected: all selected tests pass with no skips.
- Run the exact live Integration test with `live-recovery.runsettings`, emit TRX, and assert executed=1/passed=1/notExecuted=0 -- expected: composed Memories resources, non-zero probe coverage, sentinel cleanup, correction result, and persisted live backend end-state all pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md` -- expected: every changed root gitlink is declared.

### Implementation Evidence (2026-08-28)

The supported diagnostic-store, ingestion-binding, and correction service/client surfaces, retained source artifact
promotion, Production derived-store adapter selection, durable ChatBot polling, Aspire resource composition, and fenced
probe cleanup are implemented in the working tree. The production mailbox-content authority required to feed the
ingestion-binding workflow is not present in the owned repositories, as detailed below. No commit or push was requested
or performed.

**Verified:**

- Memories Contracts, REST client, and CLI test projects build in Release with zero warnings/errors; 3 focused contract
  serialization tests and 3 focused REST-client tests pass with no skips.
- The Memories Server builds in Release with zero warnings/errors. A dedicated infrastructure-free derived-store test
  project builds with zero warnings/errors and passes 14 manifest, identity, atomic-publication, retry, and per-unit
  convergence-fence tests with no skips.
- ChatBot Server.Tests, AppHost.Tests, and Architecture.Tests build in Debug source-reference mode with zero
  warnings/errors. The focused runs pass 91 Server tests and 15 AppHost topology tests, while the complete Architecture
  suite passes 105 tests, all with no skips.
- The final focused ingestion/correction authority/content/status/DI matrix passes 50 tests with no skips, including unavailable
  message content, unauthorized attachment content, provider-ordinal translation, deterministic source identity,
  tenant/case/instance status mismatches, complete ordered finalization, retries, Project authority cardinality, and
  Production/Development adapter selection, and fail-closed Projects endpoint/token configuration.
- The complete ChatBot Conformance suite passes 98 tests and the complete ChatBot Server suite passes 1,914 tests, both
  with no skips. The ingestion workflow runtime now carries only ChatBot-owned record/status contracts; all direct
  Memories client/contract mapping is confined to `src/Hexalith.ChatBot.Server/Adapters/Memories`. The UI-spine
  association-correction acceptance fixture verifies that admission persists and schedules the accepted command while
  leaving corrected-case resolution to the Projects-backed activity inside the durable workflow; exhausted activity
  retries transition to a durable timer and retry without starting propagation under an unresolved case.
- The complete `Hexalith.ChatBot.slnx` builds in Release source-reference mode with zero warnings/errors. The Server's
  Memories project references remove the outer graph-mode globals and explicitly build the two owned Memories surfaces
  with package dependencies, satisfying the Memories repository's Release policy rather than bypassing it.
- `git diff --check` passes in both the ChatBot repository and the Memories submodule.
- `aspire start --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --non-interactive --format Json`
  builds the AppHost in Debug with zero warnings/errors, then fails closed at startup because
  `ChatBot:LiveRecoveryValidation:MailboxClientSecret` is not configured. `aspire stop` confirms no AppHost remains
  running.

**Incomplete / blocked:**

- The durable ChatBot ingestion-binding boundary now resolves the prior case through Projects, calls
  `MemoriesClient.IngestAsync`, validates terminal ingestion status for canonical `MemoryUnitId` results, and finalizes
  the complete ordered manifest. It cannot run successfully in Production because no owned repository exposes a
  supported raw controlled-mailbox message/attachment content API from which production
  `IIngestionBindingMessageSource` / `IIngestionBindingAttachmentSource` adapters could be truthfully implemented:
  `src/Hexalith.ChatBot.Workers/Mailbox/IGraphMailboxMessageSource.cs:7-9` only returns the metadata-only
  `GraphMailboxMessage`; `GraphMailboxMessage.cs:3-18` has no body and `GraphMailboxAttachment.cs:3-7` has no
  bytes/download operation. `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs:3-10` explicitly
  says body content is out of scope, and `MailboxAttachmentReference.cs:3-5` explicitly records references without
  downloading/storing content. The only ChatBot implementations of `IMailboxMessageContentSource` and
  `IMailboxAttachmentContentSource` are `UnavailableMailboxMessageContentSource` and
  `UnavailableMailboxAttachmentContentSource`, registered at
  `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs:118,157`. The Workers project has no
  Microsoft Graph SDK/provider implementation (`src/Hexalith.ChatBot.Workers/Hexalith.ChatBot.Workers.csproj:1-9`).
  Folders only exposes metadata staging through
  `references/Hexalith.Folders/src/Hexalith.Folders/Aggregates/Folder/IWorkspaceFileContentStore.cs:3-7`; its current
  `MetadataDerivedSemanticIndexingContentMaterializer.cs:28-56` fabricates metadata-derived curated text. Conversations
  has governed conversation content but no provider-message or attachment-download surface, and Projects exposes
  Project Context/case authority, not mailbox payloads. Adapting any of those surfaces would invent content, cross an
  ownership boundary, or use read-projection side effects, all prohibited by this spec. The required owner action is to
  add an authenticated, tenant/project/mailbox-scoped provider API that returns the exact message body and exact
  attachment bytes by the already-captured opaque provider ids, with explicit
  unavailable/retryable/unauthorized outcomes; only then can the two production ingestion content adapters and their DI
  wiring be implemented.
- The live Redis Stack/FalkorDB/Dapr/LLM acceptance lane was not run because the required mailbox client secret is an
  external operator-supplied value. Therefore live backend identity, persisted corrected end-state, restart behavior,
  and absence of canonical-namespace mutation by diagnostics remain unproven.
- The existing monolithic `Hexalith.Memories.Server.Tests` project remains blocked by 94 unrelated test compilation
  errors against the installed StackExchange.Redis API. Story 12.16 service coverage runs in the dedicated green
  `Hexalith.Memories.DerivedStores.Tests` project instead; the live integration assertions remain unproven.
- ChatBot Release/package mode resolves published Memories 2.21.3, which does not contain the new
  `V1.DerivedStores` contracts (`CS0234`/`CS0246`). The new Memories packages must be released and pinned before a
  package-mode Release build can pass. The source-reference Release mode now passes the Memories repository's
  Release-package policy by keeping Memories' own external dependencies in package mode. Publishing updated
  `Hexalith.Memories.Contracts` and `Hexalith.Memories.Client.Rest` packages and updating the authoritative Builds
  catalog beyond 2.21.3 is a package-owner/repository release action; adding a duplicate local package-version authority,
  fabricating an unpublished version, or checking binaries into ChatBot would not be a valid fix.
- The requested gitlink command cannot run because `scripts/validate-story-gitlinks.py` is absent from this repository
  (`python3: can't open file .../chatbot/scripts/validate-story-gitlinks.py: [Errno 2] No such file or directory`).

**Exact ChatBot repository file list:**

- `Directory.Build.props`
- `Hexalith.ChatBot.slnx`
- `README.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-12-16-bind-the-live-hexalith-memories-derived-store-backing.md`
- `docs/adrs/correction-driven-vector-reindexing.md`
- `docs/adrs/derived-store-cross-tenant-isolation.md`
- `references/Hexalith.Memories` (dirty submodule working tree; gitlink not moved)
- `src/Hexalith.ChatBot.AppHost/DaprComponents/llm.memories.yaml`
- `src/Hexalith.ChatBot.AppHost/DaprComponents/secrets.memories.json`
- `src/Hexalith.ChatBot.AppHost/DaprComponents/secretstore.memories.yaml`
- `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`
- `src/Hexalith.ChatBot.AppHost/Program.cs`
- `src/Hexalith.ChatBot.Server/Audit/DerivedStoreIsolationProbeCoordinator.cs`
- `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationActivityRequest.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationActivityResult.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationRunStoreActivity.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflow.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/CorrectionPropagationWorkflowRunner.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/ICorrectionPropagationWorkflowSteps.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/VectorReindexCorrectionPropagationStoreActivity.cs`
- `src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/PeriodicEnforcementRuntime.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/IDerivedStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/IVectorReindexer.cs`
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/InMemoryVectorReindexer.cs`
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/MemoriesDerivedStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/MemoriesDerivedStoreServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/MemoriesVectorReindexer.cs`
- `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DerivedStoreIsolationBoundaryFitnessTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Audit/DerivedStoreIsolationProbeCoordinatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/CorrectionPropagationCoordinatorTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/Workflows/IngestionBindingActivitiesTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/Workflows/IngestionBindingWorkflowRunnerTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/Workflows/VectorReindexCorrectionPropagationStoreActivityTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStores/MemoriesDerivedStoreAdapterTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/DerivedStores/MemoriesDerivedStoreDependencyInjectionTests.cs`

**Exact Memories submodule file list:**

- `Hexalith.Memories.slnx`
- `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/DerivedStoreBinding.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/DerivedStoreBindingEntry.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/DerivedStoreCorrectionState.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/DerivedStoreCorrectionStatus.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/DerivedStoreRecordKind.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/DiagnosticStoreClass.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/DiagnosticStoreDeleteResult.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/DiagnosticStoreEntry.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/FinalizeDerivedStoreBindingRequest.cs`
- `src/Hexalith.Memories.Contracts/V1/DerivedStores/StartDerivedStoreCorrectionRequest.cs`
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`
- `src/Hexalith.Memories.Contracts/V1/MemoriesRoutes.cs`
- `src/Hexalith.Memories.Server/Activities/Cases/DeleteMemoryUnitProjectionActivity.cs`
- `src/Hexalith.Memories.Server/Activities/Ingestion/ReleaseDedupKeyIfOwnedActivity.cs`
- `src/Hexalith.Memories.Server/DerivedStores/ApplyDerivedStoreCorrectionActivity.cs`
- `src/Hexalith.Memories.Server/DerivedStores/DerivedStoreCorrectionStartResult.cs`
- `src/Hexalith.Memories.Server/DerivedStores/DerivedStoreCorrectionWorkflow.cs`
- `src/Hexalith.Memories.Server/DerivedStores/DerivedStoreCorrectionWorkflowInput.cs`
- `src/Hexalith.Memories.Server/DerivedStores/DerivedStoreStateException.cs`
- `src/Hexalith.Memories.Server/DerivedStores/DurableDerivedStoreSourceArtifact.cs`
- `src/Hexalith.Memories.Server/DerivedStores/PromoteDerivedStoreSourceArtifactActivity.cs`
- `src/Hexalith.Memories.Server/DerivedStores/PromoteDerivedStoreSourceArtifactInput.cs`
- `src/Hexalith.Memories.Server/DerivedStores/RedisDerivedStoreService.cs`
- `src/Hexalith.Memories.Server/Endpoints/DerivedStoreEndpoints.cs`
- `src/Hexalith.Memories.Server/Export/TenantExportService.cs`
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`
- `src/Hexalith.Memories.Server/Hosting/MemoriesServerServiceCollectionExtensions.cs`
- `src/Hexalith.Memories.Server/Program.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantIsolationVerifier.cs`
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`
- `tests/Hexalith.Memories.Cli.Tests/ClientRest/MemoriesClientDerivedStoreTests.cs`
- `tests/Hexalith.Memories.Contracts.Tests/V1/DerivedStoreContractsSerializationTests.cs`
- `tests/Hexalith.Memories.DerivedStores.Tests/Hexalith.Memories.DerivedStores.Tests.csproj`
- `tests/Hexalith.Memories.Server.Tests/DerivedStores/RedisDerivedStoreServiceTests.cs`
