---
title: Hexalith.ChatBot Sibling Context Material-Change Re-check
status: complete
created: "2026-09-14"
sourceManifest: source-manifest.md
---

# Sibling Context Material-Change Re-check

## Verdict

The nine checked-out sibling repositories match the revisions pinned in `source-manifest.md`; no revision is inaccessible and no checkout is dirty. The product boundary is nevertheless **not contract-ready**. Five material gaps remain between the current sibling contracts and the updated `prd.md` / `addendum.md`: the only public conversation append command has a different name and no caller-supplied expected revision; Project-to-conversation ownership is stated inconsistently; ChatBot roles/admin scopes have no binding mapping to Tenants/EventStore authorization vocabulary; EventStore does not publish the atomic canonical audit-envelope/hash-chain contract claimed by ChatBot; and the currently pinned Parties/EventStore data-protection stack is not ready for real regulated personal data.

| Context | Pinned/current revision | Status |
| --- | --- | --- |
| Hexalith.Conversations | `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | **material gap** |
| Hexalith.Projects | `4f05a352edd67c4d5595913ee584539c1948dd58` | **material gap** |
| Hexalith.Folders | `b409b03f9c3e35bc65c80ed03422c540e6201b20` | **confirmed** |
| Hexalith.Parties | `14d249fde316b0002aec84351d7a7cdf953d1d30` | **material gap** |
| Hexalith.Tenants | `ff43dc941b01d4a68070f92dde0536f5ab1ef4df` | **material gap** |
| Hexalith.EventStore | `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | **material gap** |
| Hexalith.FrontComposer | `b0ad2fb69bcf5e7aadd7b388d25415d0fba876d5` | **confirmed** |
| Hexalith.Memories | `d99bc96371afbf55f8f37cd812c9e6cedba15b1d` | **confirmed** |
| Hexalith.Commons | `19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6` | **confirmed** |

## Per-context extract

### Hexalith.Conversations — material gap

- The public write is `AppendMessageCommand`, not `Project.AppendConversationMessage`. Its payload is `Metadata`, `ConversationId`, `MessageId`, `AuthorPartyId`, `Text`, optional `ProviderCorrelation`, and optional `CallerMetadata`; the resulting public event is `MessageAppended`. Evidence: `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs` and `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Events/MessageAppended.cs` at `596cee6f`.
- Neither that command nor `ConversationCommandMetadata` carries an expected conversation revision. The accepted idempotency ADR explicitly excludes expected revisions and sequence numbers from the Conversations equivalence fingerprint. Evidence: `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs` and `references/Hexalith.Conversations/docs/adrs/0001-idempotency-contract.md` at `596cee6f`.
- This conflicts with `addendum.md`'s v1 allowlist, which names `Project.AppendConversationMessage` and requires an expected conversation revision. Before architecture or stories consume the allowlist, one producer-owned adapter/rename and concurrency contract must be accepted, with an exact mapping to `AppendMessageCommand` and `MessageAppended`; otherwise the allowlisted operation is not callable as specified.
- Conversations' accepted ownership is unambiguous: it owns assignment, reassignment, and explicit clearing of the conversation's `ProjectId`; Projects must not maintain a second mutable conversation-membership list. Evidence: `references/Hexalith.Conversations/docs/adrs/0002-conversation-project-assignment-ownership.md` and `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/ReassignConversationProjectCommand.cs` at `596cee6f`.
- Preserve the typed, opaque identifiers (`ConversationId`, `MessageId`, `PartyId`, `ProjectId`, `TenantId`), tenant-first authorization, at-least-once publication, and aggregate-local rather than transport-global ordering. Evidence: `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Identifiers/ConversationId.cs` and `references/Hexalith.Conversations/docs/conversation-publication-events.md` at `596cee6f`.

### Hexalith.Projects — material gap

- Projects owns `ProjectId`, Project lifecycle/setup, and metadata-only links. Conversation assignment is completed through Conversations before Projects records `ProjectResolutionConfirmed`; there is no Projects append-message command and no Projects-owned mutable conversation-membership event. Evidence: `references/Hexalith.Projects/docs/event-catalog.md` and `references/Hexalith.Projects/src/Hexalith.Projects.Contracts/Events/ProjectResolutionConfirmed.cs` at `4f05a352`.
- `prd.md` says Projects owns “membership, and the boundary that relates a Project to conversation IDs.” Read literally, that contradicts the Conversations ownership ADR and can create dual-write membership. The PRD must distinguish **Project access/membership** (Projects-owned) from **conversation-to-Project assignment** (Conversations-owned; Projects may orchestrate through its ACL).
- The Projects identifier ADR says sibling identifiers remain opaque strings and specifically claims Conversations exposes no Contracts value object; the pinned Conversations revision now exposes `ConversationId`. Projects deliberately remains string-based until it adopts an owner type, so ChatBot must treat serialized sibling IDs as opaque and must not copy the stale “no type exists” rationale into new architecture. Evidence: `references/Hexalith.Projects/docs/adr/identifier-boundary.md` at `4f05a352` and `references/Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Identifiers/ConversationId.cs` at `596cee6f`.
- Project-context reads use an explicit allowlist and safe-denial matrix. Stale state is surfaced for read-only assembly, while a future trust-bearing operation must fail closed. ChatBot may consume a stale context only under its own bounded-freshness policy and may not treat it as current authorization. Evidence: `references/Hexalith.Projects/docs/context-assembly-decision-matrix.md` at `4f05a352`.

### Hexalith.Folders — confirmed

- The consumed boundary is contract-backed: `CreateFolder`, `GetEffectivePermissions`, `ListFolderFiles`, and `GetFolderFileMetadata` are present in the v1 OpenAPI contract. Folder, workspace, operation, and related identifiers are opaque tenant-scoped references and never tenant authority. Evidence: `references/Hexalith.Folders/src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml` at `b409b03f`.
- File/context queries authorize before observation in the fixed order tenant access → folder ACL → path policy → sensitivity classification → bounds → query execution. Hidden, missing, excluded, and unauthorized targets collapse to safe-denial shapes. Evidence: the same OpenAPI contract and `references/Hexalith.Folders/docs/adrs/0005-layered-authorization-and-oidc.md` at `b409b03f`.
- This confirms the PRD's Folders ownership and authorized-file-context constraints. Preserve metadata-only discovery, owner-side authorization, opaque identifiers, and the rule that ChatBot never infers access from possession of a folder/file ID.

### Hexalith.Parties — material gap

- Parties is the correct owner of person/organization records and is fronted by the EventStore gateway. `PartyCreated` carries domain details in the event payload while the Party identity comes from the EventStore envelope; `PartyMerged` exists only as a v2 forward-compatibility placeholder with `SurvivorPartyId` and `MergedPartyId`. Evidence: `references/Hexalith.Parties/src/Hexalith.Parties.Contracts/Events/PartyCreated.cs`, `references/Hexalith.Parties/src/Hexalith.Parties.Contracts/Events/PartyMerged.cs`, and `references/Hexalith.Parties/docs/api-contracts.md` at `14d249fd`.
- The pinned Parties product overview explicitly says the default key backend is development-only and that regulated EU personal data must not be stored in its MVP; production KMS/secret-store custody is still required. ChatBot M0 expects real external sender/party resolution, so A6 must include an executable Parties production-key/custody gate, not only approval of a data-class document. Evidence: `references/Hexalith.Parties/docs/project-overview.md` at `14d249fd`.
- Parties' local Tenants projection is eventually consistent and can briefly accept a just-disabled tenant/user unless the separate gateway authorization integration is enabled. ChatBot's revocation bounds therefore require deployment proof of the EventStore/Tenants authority path, not reliance on the local projection alone. Evidence: `references/Hexalith.Parties/docs/tenant-access-projection.md` and `references/Hexalith.Parties/docs/api-contracts.md` at `14d249fd`.
- Preserve Parties as identity/personal-data owner and do not treat `PartyMerged` as acceptance of the proposed generic `IdentityEvolved` contract.

### Hexalith.Tenants — material gap

- The producer's exact tenant membership vocabulary is `Unknown`, `TenantOwner`, `TenantContributor`, and `TenantReader`; global administrators are a distinct `system:global-administrators:global-administrators` aggregate/domain. Events include `UserAddedToTenant(TenantId, UserId, Role)` and `UserRoleChanged(TenantId, UserId, OldRole, NewRole)`. Evidence: `references/Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`, `references/Hexalith.Tenants/src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`, and `references/Hexalith.Tenants/docs/event-contract-reference.md` at `ff43dc94`.
- ChatBot defines `TenantAdmin`, `ProjectAdmin`, `ProjectOwner`, `ProjectMember`, `Contributor`, `MailboxOwner`, `Auditor`, and scoped `policy-admin` / `mailbox-admin` / `operations-admin` / `compliance-admin`, but neither PRD artifact binds those roles/scopes to Tenants roles, global-admin status, EventStore permission tokens, or Project resource grants. This is an authorization-contract gap: every matrix row needs a closed, deny-by-default mapping and conflict rule before implementation.
- Tenants command/query traffic uses the platform tenant `system`, domain `tenants`, stable `sub`, and exact `eventstore:tenant`, `eventstore:domain`, and `eventstore:permission` claims; identifier comparisons are ordinal and case-sensitive. Evidence: `references/Hexalith.Tenants/docs/production-auth-claim-contract.md` at `ff43dc94`.
- Preserve Tenants as tenant-lifecycle/membership/configuration truth. ChatBot may own its closed policy schema and admin scopes, but it must not imply that arbitrary Tenants configuration values or UI labels grant those scopes without owner-side validation.

### Hexalith.EventStore — material gap

- EventStore provides durable command/event envelopes, stable `MessageId`, tenant/domain/aggregate identity, correlation/causation, aggregate-local sequence, command status, and server-side optimistic-concurrency retries. The public command request/envelope has no caller-supplied expected aggregate revision. Evidence: `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/SubmitCommandRequest.cs`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/EventMetadata.cs`, and `references/Hexalith.EventStore/docs/reference/command-api.md` at `7579b858`.
- Gateway authorization is the confirmed topology: authenticate; resolve immutable tenant/subject/domain/message/aggregate context; validate tenant lifecycle/membership; validate RBAC; only then invoke handlers, projections, caches, replay, or reads. Claims-only validators are local/dev fallbacks; production uses configured actor-backed Tenants validators and fails closed on stale/unavailable/malformed/ambiguous responses. Evidence: `references/Hexalith.EventStore/docs/guides/security-model.md` at `7579b858`.
- No public/current EventStore artifact defines the ChatBot-claimed “canonical audit envelope,” an atomic commit of that envelope plus domain event and idempotency record, or a per-tenant hash-linked/WORM audit chain. EventStore has structured authorization logs and crypto-shredding audit events, but those are not the product-wide canonical mutation-audit contract in `addendum.md` §Shared Command Pipeline and NFR49a. That ownership/atomicity must be accepted by EventStore or reassigned before stories depend on it.
- The pinned revision adds approved public `pdenc-v2` policy, canonical-path, occurrence, erasure-state, snapshot, write-result, and completion contracts. It explicitly does **not** add the engine, backend, Server persistence integration, production enablement, erasure completion, or G5 closure; Story 8.3 and successors remain gated. Evidence: `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IPersonalDataPolicy.cs`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IErasureStateProvider.cs`, and `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/8-2-payload-protection-contracts-and-golden-vectors.md` at `7579b858`.
- The PRD correctly keeps A6 blocking, but A6 must not be closed from these contracts alone. It needs the production engine/backend, key custody, backup/restore behavior, surviving-metadata decision, Parties adapter, and runtime evidence.

### Hexalith.FrontComposer — confirmed

- `FrontComposerShell` is a framework-owned 12-parameter shell for Header, Navigation, Content, Footer, skip links, theme, settings, account, palette, and accessibility. Domain/host text remains host-owned. Evidence: `references/Hexalith.FrontComposer/docs/reference/components/front-composer-shell.md` at `b0ad2fb6`.
- FrontComposer policy metadata and presentation fail closed, but tenant isolation and authorization remain host-owned; the host injects tenant/user/message/correlation identities rather than accepting them from operator/agent input. Evidence: `references/Hexalith.FrontComposer/docs/skills/frontcomposer/security/tenant-and-policy-boundaries.md` at `b0ad2fb6`.
- The source includes metadata-rich `ProjectionChangedDetail`, scoped SignalR join/leave, and scoped reconnect/rejoin support required by the ChatBot streaming UI handoff. The accepted proposal still records package publish/pin coordination as an open handoff. Evidence: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Communication/ProjectionChangedDetail.cs`, `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`, and `references/Hexalith.FrontComposer/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-20-ai-response-progress-transport.md` at `b0ad2fb6`.
- This supports the PRD's shell choice, provided ChatBot remains owner of the governed composer, approval UI, domain state, routes, authorization lifecycle, and live data/dispatch. Do not state that FrontComposer itself supplies or authorizes the ChatBot product experience.

### Hexalith.Memories — confirmed

- Memories is safe to retain as optional. Its cross-module event intake is Dapr pub/sub → Memories sidecar → `POST /events/ingest` → workflow, with CloudEvents identity and at-least-once deduplication. Its own current AppHost does not prove a real EventStore-originating full-stack path. Evidence: `references/Hexalith.Memories/docs/dev/eventstore-integration.md` at `d99bc963`.
- MCP tenant arguments are checked exactly against authenticated tenant claims before downstream calls. Evidence: `references/Hexalith.Memories/src/Hexalith.Memories.Mcp/Authentication/TenantClaimAuthorizationFilter.cs` at `d99bc963`.
- `MemoryUnitId` is opaque, not guaranteed ULID or time-sortable, and is stable only while the permanent `(tenantId, caseId, sourceUri)` dedup record survives. Consumers resolve through the source-URI lookup rather than retaining unbounded historical IDs. Evidence: `references/Hexalith.Memories/docs/dev/memory-unit-id-stability.md` at `d99bc963`.
- These constraints agree with optional use and with A12 remaining unaccepted. If architecture selects Memories for ChatBot's derived stores, full EventStore-originating integration and tenant-isolation evidence become an explicit prerequisite; otherwise ChatBot remains owner of those stores as the PRD states.

### Hexalith.Commons — confirmed

- Commons supplies a domain-neutral tenant-access evaluator that reconciles all tenant-bearing inputs, checks projection health before state, compares tenant/principal identifiers ordinally, fails closed on stale/gap/rollback/poison/unavailable state, and delegates the closed role/permission vocabulary to the consuming module. Evidence: `references/Hexalith.Commons/src/libraries/Hexalith.Commons.TenantAccess/TenantAccessEvaluator.cs` and `references/Hexalith.Commons/src/libraries/Hexalith.Commons.TenantAccess/TenantAccessDenialKind.cs` at `19d7d4d6`.
- Commons supplies ULID generation as a helper, not a universal cross-context identifier contract. Memories explicitly uses non-ULID opaque IDs, while Projects and Folders treat sibling IDs as opaque wire values. Evidence: `references/Hexalith.Commons/src/libraries/Hexalith.Commons.UniqueIds/README.md` at `19d7d4d6`.
- Preserve Commons as shared mechanics only. Domain owners retain identifier lifecycle, role mapping, and authorization meaning.

## Cross-context constraints to preserve

1. `IdentityEvolved` is absent from all nine pinned repositories. The PRD/addendum are correct to label it proposed and non-binding. No automatic identity migration may depend on it until each producer accepts a versioned event plus reconciliation query and fallback.
2. Cross-context identifiers are opaque, case-sensitive owner values. Do not infer ULID/GUID shape, creation time, ordering, tenant authority, or permission from an identifier.
3. Event publication and local authorization projections are eventually consistent. Trust-bearing ChatBot work requires current owner/gateway authorization or a bounded, explicitly accepted freshness rule; no local projection silently overrides the authoritative context.
4. FrontComposer owns reusable shell behavior; ChatBot owns its product workflows and domain UX. EventStore owns the existing gateway/envelopes, but its ownership of the proposed canonical audit envelope remains unresolved.
5. Architecture/stories must not close A6, A8, or A12 by citing a checked-out revision alone. A6 needs executable production data-protection evidence; A8 needs an accepted callable command/schema/concurrency mapping; A12 needs producer acceptance.
