---
title: Hexalith.ChatBot PRD Source Manifest
status: current
created: "2026-09-14"
updated: "2026-09-14"
workspaceRevision: 76f355a038c4abdb3b9fdb3fb836c25053a18fb0
recheckStatus: material-gaps-recorded
recheckArtifact: reconcile-full-sibling-a13-2026-09-14.md
---

# Source Manifest

This manifest makes the brownfield evidence used by the 2026-09-14 PRD update reproducible. Repository-relative paths are authoritative; hashes identify local review inputs that were not committed at update time.

## Direct product inputs

| Source | Revision or SHA-256 | Role |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md` | commit `eceaa02ac63769eba580b8f29b8b23e048003a16`; SHA-256 `d890417042c0329f168306936ae9b1004f2aa40c22f3e1d2845579db9b1ffd63` | Original product intent and user-voice anchor |
| `_bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md` | reviewed working-tree snapshot over commit `bab0218c14f5d5c4bc9513014535c5f22e84cde7`; SHA-256 `e7a031bec0be1af92d981d86d06342e227ddb109f50a0b19f9800db3c50a02e7` | Accepted recovery evidence-integrity contract; `activation: pending`. A later hash at this path triggers re-check and is not silently consumed. |
| `orient-extract.md` | SHA-256 `e347e94774e82596bbc5a2a2773191632da705190d9bba0c105aa00a76ce8228` | 2026-09-13 PRD orientation extract |
| `review-adversarial-general.md` | SHA-256 `cac9f929eff9d4a800cd1e9163379b4d14e0de5075d28510634ef0e58bf67a18` | 2026-09-13 adversarial findings |
| `review-rubric.md` | SHA-256 `b569edf82e2c2d4c8120d84cd2dce3fe013e1b00c22e2e1c2e6885975fd85e1a` | 2026-09-13 rubric findings |
| `validation-report.md` | SHA-256 `1aa8295437bcc700cdcb67b10459175b85b9cdedbc1bfac471e35ef3542f7dae` | 2026-09-13 synthesized validation input |
| `.memlog.md` | append-only; initialized 2026-09-13 | Canonical decision and override memory |

## Sibling bounded-context revisions inspected

| Context | Revision | PRD dependency role |
| --- | --- | --- |
| Hexalith.Conversations | `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | Conversation identity and messages |
| Hexalith.Projects | `4f05a352edd67c4d5595913ee584539c1948dd58` | Project identity, membership, and project boundary |
| Hexalith.Folders | `b409b03f9c3e35bc65c80ed03422c540e6201b20` | Governed file and folder records |
| Hexalith.Parties | `14d249fde316b0002aec84351d7a7cdf953d1d30` | Party identity and external participants |
| Hexalith.Tenants | `ff43dc941b01d4a68070f92dde0536f5ab1ef4df` | Tenant identity and tenant policy boundary |
| Hexalith.EventStore | `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | Command/event durability and canonical audit envelope, payload protection, and erasure boundary |
| Hexalith.FrontComposer | `b0ad2fb69bcf5e7aadd7b388d25415d0fba876d5` | Governed UI shell and interactive chat surface |
| Hexalith.Memories | `d99bc96371afbf55f8f37cd812c9e6cedba15b1d` | Optional AI memory dependency |
| Hexalith.Commons | `19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6` | Shared contracts and primitives |

These revisions identify the checked-out dependency baseline; they do not by themselves prove acceptance of every proposed cross-context contract. The 2026-09-14 material-change re-check found five contract gaps and no inaccessible sibling checkout at inspection time. A later concurrent working-tree change in Hexalith.Folders affects generated client files, not the reviewed OpenAPI contract; the pinned revision and consumed-file hash remain authoritative for this update. `IdentityEvolved` remains unaccepted until the producer contracts named in the addendum are approved.

## Consumed contract baseline

The System Architect reviewed these exact contract files on 2026-09-14. SHA-256 values bind the reviewed content at the sibling revision above; status is a compatibility result, not producer acceptance.

| Context | Exact consumed contract / symbol | SHA-256 | Compatibility result |
| --- | --- | --- | --- |
| Conversations | `src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs` — `AppendMessageCommand` | `e31d0ab2e2c437a73ae9bdbb946d1f69e3d4ac5840e2d49042d3a5229ee84acd` | **Blocked A13:** exact v1 field mapping is specified in the addendum; no caller expected revision. |
| Conversations | `src/Hexalith.Conversations.Contracts/Commands/ConversationCommandMetadata.cs` — `ConversationCommandMetadata` | `b51ddda06892513c03fbd51a51b08d2528dcc0fcf2ef49308ca0dbf921a09735` | **Blocked A13:** idempotency key exists; owner retention/concurrency does not yet meet ChatBot's lifetime guarantee. |
| Conversations | `src/Hexalith.Conversations.Client/IConversationClient.cs` — `AppendMessageAsync` | `a8334c5fc9c2447c12465e54c229cbe0987491f4dfb77bbcb99e8c15f2613b42` | Compatible transport shape only; does not prove producer execution. |
| Conversations | `src/Hexalith.Conversations.Server/Api/ConversationCommandApi.cs` — v1 route and `IConversationCommandApiHandler.AppendMessageAsync` | `1c23daa1f1fe4c1dbf45bf647c8c2b35ee41ecb958da3a8ff795d2e1b4c30b0c` | **Blocked A13:** no production handler or aggregate append implementation exists in the current source. |
| Conversations | `src/Hexalith.Conversations.Contracts/Events/MessageAppended.cs` — `MessageAppended` | `dacb9529569d23ec4c91a95fdc506f4a6775af30097da22378c8b8dff981b046` | Compatible success-event target once A13 mapping is accepted. |
| Conversations | `docs/adrs/0001-idempotency-contract.md` | `32658f14e717ed4993a0be92c3ae13e385b8382c93853867d93d650daab4e440` | **Blocked A13:** current documented default is 24 hours and excludes expected revisions. |
| Conversations | `docs/adrs/0002-conversation-project-assignment-ownership.md` | `b300c3b0764d225628d51ea1488da4864bf7d9a2d2c4e907a264cab12634c0ec` | Compatible; PRD assigns conversation-to-Project assignment solely to Conversations. |
| Projects | `src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml` — `ListProjects`, `GetProject`, `GetProjectContext` authorization-filtered reads | `cf83ea0c7c13ca6df1b37ecba796fe124229b438f1200afc3fbeb42cc01b1b2e` | **Blocked A13 for authority-token acceptance:** current routes provide server-authorized, tenant-scoped metadata/context and safe denial; exact ChatBot role-to-Projects resource-grant mapping still requires Projects owner acceptance and contract tests. |
| Folders | `src/Hexalith.Folders.Contracts/openapi/hexalith.folders.v1.yaml` — `CreateFolder`, `GetEffectivePermissions`, `ListFolderFiles`, `GetFolderFileMetadata` | `a953a7d3bf6e3b1f99542ba8d20720047c1aa316d7a80d6725a28e96f834c420` | Compatible with owner-side authorization and opaque tenant-scoped IDs. |
| Parties | `docs/project-overview.md` — production key-custody constraint | `0bc1e9f8e1c632d24fda520ad6416be419dbcc7074d1b307958148546c3cf740` | **Blocked A6:** development key backend is not suitable evidence for regulated PII. |
| Parties | `docs/tenant-access-projection.md` — authorization freshness | `b754368f7497d29c040ce9ee9f4882e042e4a5c42cd68ee0667f68afd4fadaaf` | **Blocked A13:** trust-bearing operations require current gateway authorization, not local projection alone. |
| Tenants | `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs` — closed tenant-role vocabulary | `78dbc4d45629e0dca93c01de9ac9ca0960371fa3a50a6fa4facb12829876291f` | **Blocked A13:** PRD supplies a deny-by-default application-role mapping pending owner acceptance. |
| Tenants | `docs/production-auth-claim-contract.md` — EventStore claim vocabulary | `2dd987eee795c149203a0f69157664b70e1c857974a66ced3d040a7c4816c874` | Compatible target; exact role/permission mapping remains A13. |
| EventStore | `src/Hexalith.EventStore.Contracts/Commands/SubmitCommandRequest.cs` and `CommandEnvelope.cs` — current command boundary | `6a24294242d0096c4736267d308309be89c0a1a30ca64a3a32cf3026eda56cf7`; `4acdb00e6bb86c7e58c9a4658538688c635acffa6ac940edb53ed524a7901d7f` | **Blocked A13:** current envelopes do not publish ChatBot's atomic canonical-audit/hash-chain contract or caller expected revision. |
| EventStore | `src/Hexalith.EventStore.Server/Events/IEventPersister.cs` and `src/Hexalith.EventStore.Server/Actors/AggregateActor.cs` — one-aggregate event-batch durability | `0965e71e28d3c7ac520aa49aa7b45fa6f91509a38cbd0c7df7572ca4dd59532b`; `fa06c2fb35e981ba3586498ea95707cdfe61d60d9e7086649e33ee3e7d377e35` | Conditionally compatible: domain plus audit events can co-commit in one aggregate; terminal idempotency does not co-commit. |
| EventStore | `src/Hexalith.EventStore.Server/Projections/IProjectionActivationOutbox.cs` — projection activation outbox | `5bd994dd362d7305ca0cf88365f6ae240417b4c9b367e8fb9419c9982147421d` | Incompatible as FR81a audit outbox; it is projection-only and separately persisted. |
| EventStore | `docs/guides/security-model.md` — actor-backed gateway authorization | `56b08ddeeaf9a556ed50e29562d283b32b19973d97c94cbd1a064b67f7a0035e` | Compatible required pilot topology; claims-only local fallback is excluded. |
| EventStore | `src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs` — payload-protection interface | `bf642ba897581dae1870c524e10ee8c97824e4c4c1675ddcd0cbfe14f8f6781e` | **Blocked A6:** interface exists, but engine/backend/production evidence and erasure completion do not. |
| EventStore | `src/Hexalith.EventStore.Contracts/Security/IPersonalDataPolicy.cs`, `ICanonicalPersonalDataPathPolicy.cs`, and `IErasureStateProvider.cs` — version 1 protection/erasure seams | `117a4ee269e9eb5e296497bcd571da3350a1cbba633f3466280ec4c7d77b959d`; `780087c13e62151a075baa7146f9a8ef7a4cd43ca3c34631a6b92bda49c951fb`; `f0e4a5de400af8fd0b5189877640535b2b97b06340c52df6253aae99d6077a64` | **Blocked A6:** post-tag source contracts are unwired and unreleased; no production engine, erasure workflow, backup propagation, or proof. |
| FrontComposer | `src/Hexalith.FrontComposer.Contracts/Communication/ProjectionChangedDetail.cs` — scoped progress transport | `c17ee17b210ff76d8647afd3ff0ef3a2b4bd3d8cfd231487fbdb1038eaccd343` | Compatible with ChatBot-owned governed-composer lifecycle and authorization. |
| Commons | `src/libraries/Hexalith.Commons.TenantAccess/TenantAccessEvaluator.cs` — tenant access evaluator | `0ce89e385db66952b366900f8cbd787c42eee03e81597236029fa94e2c6cc16b` | Compatible shared mechanism; ChatBot retains the closed role/permission vocabulary. |

The single current nine-context/A13 gate result is `reconcile-full-sibling-a13-2026-09-14.md`. It supersedes the initial five-gap extract for gate status and incorporates the narrower H4/H12 implementation verification. A8 governs allowlist membership; executable append, authority, audit, and fencing acceptance belong to A13. A6, A13, or A12 cannot be closed by repository revision alone.

## Direct architecture re-check

The current working-tree Epic 12 architecture was rechecked after its 2026-09-14 material update. The PRD now consumes its adopted AD-6 through AD-9 decisions, explicit `activation: pending` boundary, unique pull-request check identity, cleanup receipt, disjoint diagnostic/completion/A10 evidence channels, fixed closeout deadlines, immutable tool references, and isolated destructive-run contract. These changes strengthen NFR54a/NFR65a and A10 but do not close A10: activation evidence and a separate fresh operational controlled-loss/RTO bundle remain required.

## Re-check rule

The System Architect opens a re-check within five business days when a listed context changes a consumed command/event schema, authorization or identifier semantics, integration topology, or referenced RBAC rule. The outcome is appended to `.memlog.md` and updates this manifest. Missing source artifacts or inaccessible revisions are recorded as blockers rather than inferred.
