---
title: "Current sibling contract reconciliation"
status: complete
created: "2026-09-14"
reviewScope: "Adversarial H4/H12 and source-lineage H2"
---

# Current Sibling Contract Reconciliation — 2026-09-14

## Snapshot and verdict

The inspected sibling worktrees were clean at these exact checked-out revisions:

| Repository | Current revision | Version identity |
| --- | --- | --- |
| Hexalith.Conversations | `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | No containing tag; public compatibility metadata declares command/event schema `1` and Contracts/Client package `1.0.0`. |
| Hexalith.EventStore | `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | `v3.104.0-2-g7579b858`; the new payload-protection v2 contracts are post-`v3.104.0` source and are not established here as a released package. |
| Hexalith.Parties | `14d249fde316b0002aec84351d7a7cdf953d1d30` | `v1.1.1-61-g14d249fd`; default EventStore consumption is released package `3.104.0`. |

**Overall:** the source revisions are now correctly pinned, but the product assumptions are not executable without producer/architecture work. Conversations exposes a v1 append DTO, client method, and opt-in HTTP route, but no production append handler or aggregate command in the checked-out repository. EventStore can atomically persist multiple events in one aggregate-actor batch, which is compatible with the revised per-aggregate audit-chain topology, but it does not atomically commit that batch with the durable terminal idempotency record, has no generic canonical-audit or signed-checkpoint contract, and provides no cross-aggregate transaction. The new payload-protection/erasure types are source-only contract seams and are not wired into the current event persister.

## H4 — Conversation append mapping

### What exists

- `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs` defines `Hexalith.Conversations.Contracts.Commands.AppendMessageCommand` with `Metadata`, `ConversationId`, `MessageId`, `AuthorPartyId`, `Text`, optional `ProviderCorrelation`, and optional `CallerMetadata`.
- `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs` defines schema, tenant, actor Party, correlation, optional causation, and optional idempotency key. `ConversationCommandSchemaValidation.ValidateMetadata` requires the key at runtime and caps it at 200 characters.
- `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Versioning/SchemaVersion.cs` sets `SchemaVersion.Current` to `1`; `ContractCompatibilityMetadata.Current` declares `Hexalith.Conversations.Contracts` and `.Client` as `1.0.0`.
- `IConversationClient.AppendMessageAsync` and `ConversationClient.AppendMessageAsync` send the DTO to `POST api/v1/conversations/{conversationId}/messages` and project command metadata into the tenant, actor, correlation, causation, and idempotency headers.
- `ConversationCommandApi` exposes that route only through an injected `IConversationCommandApiHandler.AppendMessageAsync`.
- `MessageAppended` is the public v1 result event shape. Publication is at least once and may reorder; its deduplication identity is `(tenantId, conversationId, eventId, schemaVersion)`.

### What does not exist

- There is no symbol named `Project.AppendConversationMessage` in the Conversations repository.
- There is no production implementation of `IConversationCommandApiHandler` in `src`; the only implementations are test fakes.
- `ConversationAggregate` has no `Handle` overload for append, and the domain `Commands` directory contains no append-message command. The public DTO/client/route therefore does not establish an executable end-to-end producer contract by itself.
- Neither `AppendMessageCommand` nor `ConversationCommandMetadata` carries an expected conversation revision. The accepted idempotency ADR deliberately excludes expected revisions and sequence numbers from the fingerprint.
- `CallerMetadata` is explicitly provenance-only and cannot carry tenant authority, authorization truth, governance truth, raw prompts, message text, protected content, or approval truth. It cannot be used as a hidden carrier for the PRD's approval/policy/audit contract.
- `IdempotentConversationCommandExecutor` defaults retention to 24 hours. The available in-memory store returns retryable uncertainty for an expired completed record but may replace an expired pending reservation. Neither behavior proves the PRD's governed-record-lifetime outcome retention and duplicate barrier.

### Required mapping decision

The stable product ID may map to Conversations v1 only after a producer-owned executable adapter is accepted. The minimum field mapping is:

| Product value | Conversations v1 target | Status |
| --- | --- | --- |
| Stable allowlist member `Project.AppendConversationMessage` | Versioned alias/wrapper for `AppendMessageCommand` | **Requires architecture work** — name, wrapper owner, and version are absent. |
| Authenticated tenant | `Metadata.TenantId` and owner-side tenant authorization | **Compatible** if derived from trusted context, never caller metadata. |
| Product `operation_id` | `Metadata.IdempotencyKey` | **Conditionally compatible** — required by validation, max 200 characters; retention/outcome semantics need alignment. |
| Product correlation/proposal/approval lineage | `Metadata.CorrelationId` and `Metadata.CausationId` | **Requires architecture work** — exact causal mapping and failure translation are not defined. |
| Target conversation | `ConversationId` | **Compatible** as an opaque owner-issued identifier. |
| Stable message identity | `MessageId` | **Compatible** as an opaque owner-issued identifier. |
| Human/AI attribution | `Metadata.ActorPartyId`, `AuthorPartyId`, bounded `CallerMetadata` | **Requires architecture work** — requester, executing AI actor, and displayed author must be distinguished without treating provenance as authority. |
| Approved output | `Text` | **Compatible** at shape level, subject to Conversations content policy. |
| Expected conversation revision | No v1 field | **Unsupported** — requires a new Conversations command/schema or an owner-accepted concurrency command. |
| Applied policy/approval references and canonical audit envelope | No append DTO/event fields; `CallerMetadata` is forbidden as authority | **Unsupported** for the FR81a atomic unit. The owning aggregate must emit the mutation and canonical audit event together, or the product must adopt another owner-approved atomic boundary. |
| Success/failure | `ConversationCommandAcceptedResult` or `ConversationErrorResult`; eventual public event `MessageAppended` | **Conditionally compatible** — the wrapper must define typed translation, including unknown/pending publication outcomes. |

**H4 disposition:** **requires architecture and Conversations producer work**. The schema-level mapping is identifiable, but the current sibling does not provide the executable, expected-revision-aware, atomically audited append promised by the product allowlist.

## H12 — Atomicity, outbox, and audit sequencing

### Compatible platform capability

- `DomainResult.Success(IReadOnlyList<IEventPayload>)` can return multiple ordinary events for one command.
- `IEventPersister.PersistEventsAsync` stages gapless events for one `AggregateIdentity` and intentionally does not save; `AggregateActor` performs one `StateManager.SaveStateAsync` for events, snapshot, the `EventsStored` checkpoint, and the actor-scoped commit witness.
- Actor dispatch serializes turns for one aggregate, and `EventMetadata.SequenceNumber` is gapless and authoritative within that aggregate. This is compatible with `addendum.md`'s revised rule that a mutation envelope and predecessor hash live in the same aggregate command stream.
- A ChatBot aggregate can therefore emit a domain mutation event and a canonical audit event in one `DomainResult` batch, provided its domain model owns the predecessor hash and both events are ordinary events in the same aggregate stream.

### Unsupported or conditional assumptions

- The terminal actor-local idempotency record is staged by `CompleteTerminalAsync` and saved after event publication, not in the earlier event-batch save. Trusted opaque-key admission is also finalized later by `SubmitCommandHandler.CompleteAdmissionWithRecoveryAsync`. Thus current EventStore does **not** implement FR81a's literal atomic unit of domain event + durable terminal idempotency outcome + policy/approval references + audit envelope.
- `IProjectionActivationOutbox` is a payload-free projection-activation outbox. `SubmitCommandHandler` calls `EnsureAsync` before routing; `DaprProjectionActivationOutbox` writes a separately sharded Dapr ledger with ETag retries. It is not an audit outbox and is not committed in the aggregate event transaction.
- No current public contract defines a generic canonical audit envelope, predecessor-hash field, per-aggregate hash verifier, WORM retention boundary, signed tenant checkpoint, or a transactional outbox committed with an aggregate stream.
- `EventMetadata.GlobalPosition` and `IGlobalPositionAllocator` do not close the gap. Allocation occurs in a separate global actor save before the aggregate event save, may leave unused positions if the later save fails, is not tenant-scoped, and does not select a predecessor hash. The event persister also has a no-op allocator fallback that writes `0`.
- EventStore explicitly documents that sequence is aggregate-local, not global. It also documents that the supported path currently lacks a storage-level append fence against adversarial direct actor-state writes in the observed Dapr/Redis profile.
- `CryptoShreddingAuditEvent` is a specialized protection-workflow/restore-admission record. It has no generic mutation command, policy/approval references, predecessor hash, or chain sequence and must not be presented as the ChatBot canonical audit contract.

### Architecture consequence

The revised per-aggregate chain avoids the impossible single per-tenant predecessor race, but H12 is only **partially compatible** with current EventStore. Architecture must choose and prove one of these owner-approved contracts:

1. Treat the atomically saved `EventsStored` command checkpoint as the durable idempotency commitment, define later terminal-result completion as recoverable enrichment, and revise FR81a's wording accordingly; or
2. Extend EventStore so the exact durable idempotency record and audit-bearing event batch commit together; or
3. Introduce a real transactional outbox with an owner-accepted atomicity guarantee.

In every option, ChatBot must define the canonical audit event schema, predecessor-hash derivation, aggregate-state update, signed tenant-head checkpoint, rebuild/fork verification, and concurrent-write acceptance tests. A13 must bind the selected storage provider and prove that only the supported actor path can write event keys and that competing first writes fail closed.

## Payload protection and erasure

Current revision `7579b858` adds source-level v1 contracts for the future `pdenc-v2` boundary:

- `IPersonalDataPolicy` (`ContractVersion = 1`) makes monotonic `Abstain`/`Protect` decisions over exact typed/serialized values.
- `ICanonicalPersonalDataPathPolicy` (`ContractVersion = 1`) selects complete canonical JSON Pointer sets when safe property correlation is unavailable.
- `IErasureStateProvider` (`ContractVersion = 1`) returns strongly observed erasure state, lifecycle epoch, bounded reason, and observation time.
- `IEventPayloadProtectionService` now has occurrence-aware event/snapshot protection and unprotection plus reservation completion calls using `PayloadProtectionCompletionContext` and `PayloadProtectionPersistenceOutcome`.
- `EventStorePayloadProtectionMetadata.CurrentMetadataVersion` remains `1`.

These contracts are **not an executable A6 closure**:

- They were introduced two commits after released tag `v3.104.0`; this review found no released package identity containing them.
- Production `EventPersister` still calls the legacy `ProtectEventPayloadAsync` overload. Production source has no use of `PayloadProtectionWriteResult`, completion lease/acquisition, completion outcome, personal-data policies, canonical-path policies, or erasure-state providers; usages outside Contracts are contract tests only.
- The new interfaces do not themselves provide the encryption engine, key backend/custody, erasure command/workflow, backup propagation, or completion evidence.

**Disposition:** **requires EventStore architecture/implementation and release work**. They are suitable contract seams to pin for future work, not evidence that ChatBot data is currently protected or erasable.

## Parties material-change check

The range from previously recorded `bfc15cc15b6556c5f97ae3d06bdefdd809d3b666` to current `14d249fde316b0002aec84351d7a7cdf953d1d30` contains no production `src/` changes and no public Parties command/event/identifier schema changes. It updates dependencies, SDK/runtime documentation, CI/fitness tests, and root gitlinks. Notable consumption facts are:

- .NET SDK moves to `10.0.401`, Dapr to `1.18.7`, and related test/tool packages advance.
- The default EventStore package moves from `3.103.0` to released `3.104.0`.
- The Parties source-mode receipt pins EventStore `dfc0ac557c43363159b55bffb4d40feceab1f787`, while the ChatBot workspace directly pins later EventStore `7579b858`.

**PRD impact:** **compatible; no product-domain contract change**. Refreshing the Parties revision in source lineage is sufficient for its public Party-identity role. The dependency move requires normal integration qualification, but Parties does not prove consumption of the post-tag payload-protection interfaces and cannot be cited to close A6 or FR81a.

## Proposed consumed-contract manifest

Add a distinct table like the following under `source-manifest.md`; repository revision, exact path, contract/schema version, file hash, compatibility outcome, reviewer/date, and unresolved work should all be retained. Hashes below are SHA-256 of the clean current checkout.

| Consumed assumption | Repository revision | Exact source path and symbol | Contract/version | SHA-256 | Result | Unresolved work |
| --- | --- | --- | --- | --- | --- | --- |
| Conversation append request | Conversations `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs` — `AppendMessageCommand` | schema `1`; declared Contracts package `1.0.0` | `e31d0ab2e2c437a73ae9bdbb946d1f69e3d4ac5840e2d49042d3a5229ee84acd` | **Requires architecture work** | Stable alias/version and complete field/authority mapping. |
| Conversation command metadata | Conversations `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs` — `ConversationCommandMetadata` | schema `1` | `b51ddda06892513c03fbd51a51b08d2528dcc0fcf2ef49308ca0dbf921a09735` | **Requires architecture work** | `operation_id` retention, actor/author/AI attribution, causation mapping; no expected revision. |
| Conversation append client | Conversations `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | `references/Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs` — `AppendMessageAsync` | declared Client package `1.0.0` | `a8334c5fc9c2447c12465e54c229cbe0987491f4dfb77bbcb99e8c15f2613b42` | **Compatible at transport shape** | Does not prove producer execution. |
| Conversation append HTTP/handler boundary | Conversations `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | `references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs` — route and `IConversationCommandApiHandler.AppendMessageAsync` | v1 route | `1c23daa1f1fe4c1dbf45bf647c8c2b35ee41ecb958da3a8ff795d2e1b4c30b0c` | **Unsupported as executable producer** | No production handler or aggregate append command in current `src`. |
| Conversation append result event | Conversations `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/MessageAppended.cs` — `MessageAppended` | schema `1` | `dacb9529569d23ec4c91a95fdc506f4a6775af30097da22378c8b8dff981b046` | **Compatible at event shape** | No expected revision or approval/policy/audit fields. |
| Conversations idempotency semantics | Conversations `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | `references/Hexalith.Conversations/docs/adrs/0001-idempotency-contract.md`; `src/Hexalith.Conversations/Idempotency/ConversationCommandFingerprint.cs`; `src/Hexalith.Conversations.Server/CommandHandlers/IdempotentConversationCommandExecutor.cs`; `src/Hexalith.Conversations/Idempotency/InMemoryConversationIdempotencyStore.cs` | accepted ADR; schema-scoped key | `32658f14e717ed4993a0be92c3ae13e385b8382c93853867d93d650daab4e440`; `f4e0fb1f90da5988deba872c2adaff9f9af2435718658561a11731351fbffd0c`; `9e45ff8acbb4e582d98528006c78f82d3b2ea1d6e7be610599a3775f646ef67b`; `70d61244d4d920f88c4bc6197111805c1a9c8b37312ec6e5e7f188638d6f0ac6` | **Requires architecture work** | Expected revision excluded; default 24-hour retention and expiry behavior do not establish product-lifetime retention. |
| One-aggregate event-batch durability | EventStore `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/IEventPersister.cs` — `PersistEventsAsync`; `src/Hexalith.EventStore.Server/Actors/AggregateActor.cs` — event-batch `SaveStateAsync` | current source; event metadata `1` | `0965e71e28d3c7ac520aa49aa7b45fa6f91509a38cbd0c7df7572ca4dd59532b`; `fa06c2fb35e981ba3586498ea95707cdfe61d60d9e7086649e33ee3e7d377e35` | **Conditionally compatible** | Domain + audit event can co-commit in one aggregate; terminal idempotency does not. |
| Multi-event domain result | EventStore `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs` — `DomainResult.Success` | current source | `a30fdf89cb8e4aacfd1ae7f1b6916c9455fe9a12a34fbdadfdb7901842c71881` | **Compatible** | Canonical audit event schema remains ChatBot/owner architecture work. |
| Projection activation outbox | EventStore `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/IProjectionActivationOutbox.cs` — `IProjectionActivationOutbox` | current source | `5bd994dd362d7305ca0cf88365f6ae240417b4c9b367e8fb9419c9982147421d` | **Incompatible as FR81a outbox** | Projection-only, separately persisted, not atomically committed with stream. |
| Aggregate-local ordering | EventStore `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/EventMetadata.cs` — `SequenceNumber`, `GlobalPosition`; `docs/concepts/event-envelope.md` | metadata version `1` | `98d4600ad58c02cea37b993347cbb57ce226d47fb134989572d2d25fdd7fb6ba`; `0bd4ecfc76b32b49bcf9a03ff993ad35e9679e42e1b972bebf7125a756c83e5f` | **Compatible for per-aggregate chain only** | Hash schema, verifier, signed tenant-head checkpoint, and A13 fencing proof absent. |
| Personal-data policy | EventStore `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IPersonalDataPolicy.cs` — `IPersonalDataPolicy`; `ICanonicalPersonalDataPathPolicy.cs` — `ICanonicalPersonalDataPathPolicy` | both contract version `1`; post-`v3.104.0` source | `117a4ee269e9eb5e296497bcd571da3350a1cbba633f3466280ec4c7d77b959d`; `780087c13e62151a075baa7146f9a8ef7a4cd43ca3c34631a6b92bda49c951fb` | **Requires EventStore work** | No runtime policy engine/persister integration or released package. |
| Erasure-state observation | EventStore `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IErasureStateProvider.cs` — `IErasureStateProvider` | contract version `1`; post-`v3.104.0` source | `f0e4a5de400af8fd0b5189877640535b2b97b06340c52df6253aae99d6077a64` | **Requires EventStore work** | Observation seam only; no erasure workflow, backend, backup propagation, or proof. |
| Payload-protection persistence completion | EventStore `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs` — occurrence-aware overloads, `AcquirePayloadProtectionCompletionLeaseAsync`, `CompletePayloadProtectionAsync` | contract surface for `pdenc-v2`; metadata version `1`; post-`v3.104.0` source | `bf642ba897581dae1870c524e10ee8c97824e4c4c1675ddcd0cbfe14f8f6781e` | **Requires EventStore work** | Current persister uses legacy overload; completion path has no production integration. |
| Specialized crypto-shredding audit | EventStore `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/CryptoShreddingAuditEvent.cs` — `CryptoShreddingAuditEvent` | current source | `8d2bba84d8f2fe5213efac8be711edc2d283e442a3b99222f533a4deaf0295f3` | **Unsupported as canonical product audit** | Specialized scope; no predecessor hash or generic mutation contract. |
| Parties public-domain compatibility | Parties `14d249fde316b0002aec84351d7a7cdf953d1d30` | `references/Hexalith.Parties/docs/architecture.md`; no changed `src/` file in reviewed range | default EventStore package `3.104.0` | `73d6a20f6796503794a8b95811cc406135e0f25ba363d0d872f34eae1f137be9` | **Compatible** | Runtime/dependency qualification only; cannot prove post-tag EventStore security contracts. |

Review date for every proposed row: `2026-09-14`. Reviewer: current-sibling targeted evidence pass. Any source change to these paths, their repository revision, schema/package version, or owning authorization semantics must reopen the material-change check.

## Recommended closure order

1. Obtain a Conversations-owned append implementation and versioned product-ID mapping, including expected revision, actor/author/AI attribution, idempotency retention, typed errors, and an audit-bearing event batch.
2. Decide how FR81a's durable idempotency clause maps to EventStore's two-stage event/terminal completion model; do not call the current projection outbox transactional audit infrastructure.
3. Specify and implement the canonical audit event, per-aggregate predecessor hash, signed tenant-head checkpoint, and A13 provider/write-fence evidence.
4. Pin a released EventStore package containing the selected payload-protection contracts, then prove the engine, key custody, completion, erasure, backup, and Parties integration before closing A6.
5. Copy the proposed rows into the canonical source manifest only after the owners accept the mappings; repository revision alone is lineage, not compatibility approval.
