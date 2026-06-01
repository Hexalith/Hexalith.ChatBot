---
baseline_commit: 2eae6dae091e604562164ed37555de07cfca671f
---

# Story 3.14: Scoped AI-context packaging from authorized files

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As the system,
I want authorized project files represented through an explicit, auditable AI-context package manifest,
so that AI-context eligibility can be inspected before Epic 4 consumes it — without invoking any model or tool.

## Acceptance Criteria

1. Given an authorized project file set, when an AI-context eligibility package is produced in Epic 3, then the package manifest can be inspected by an authorized reviewer/approver through the project conversation query contract **without invoking any model, embedding, or external tool**, and a file is included only when explicit authorization, tenant policy checks, and auditable context packaging all pass — files that fail any gate are never silently present in the included set. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.14; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR33; _bmad-output/planning-artifacts/architecture.md#Governed-AI-Mediation]
2. Given the context package is assembled, when it is materialized, then it carries — before any model or tool invocation — tenant ID, project ID, source evidence references, a policy snapshot ID, a redaction decision, a retention class, a provider-reuse setting, and per-excluded-file reasons. Validation must prove every materialized package contains all of these fields. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR9; _bmad-output/planning-artifacts/architecture.md#Format-Patterns; #Communication-Patterns]
3. Given an attachment is still pending scan, scan-unavailable/retryable, unsafe/rejected/failed, redacted, or otherwise AI-context-ineligible per Story 3.13, when the package is assembled, then that file is excluded from the included set and recorded in the excluded set with a stable, user-safe reason code until tenant policy and the safety gate permit it; it can move into the included set only after scan success, policy allows it, project/file authorization passes, and audit/correlation metadata are available. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR21; #FR33; _bmad-output/implementation-artifacts/3-13-attachment-status-states-and-authorization.md#Current-State-To-Preserve]
4. Given an unauthorized actor, stale/revoked authority, a cross-tenant or cross-project context, or a project with no eligible files, when the package is requested, then access is prevented with a redacted denial (or an empty, non-confirming package) that does not confirm the existence of restricted project, folder, file, evidence, audit, tenant, or attachment data, and never exposes raw file content, folder/file paths, scanner findings, provider payloads, or raw exception text. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR32; #NFR2; #NFR6; #NFR11; Hexalith.Folders/_bmad-output/project-context.md#Critical-Dont-Miss-Rules]
5. Given attachment safety, storage, authorization, policy, or projection events arrive out of order or more than once, when the package is (re)assembled, then assembly is idempotent, source-version tolerant, tenant/project scoped, and order-tolerant; it must not include a file whose latest authorized safety/authorization state is ineligible, and must not regress a file out of the included set on a stale event once a newer authorized event has admitted it (and vice-versa for terminal exclusion). [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR13; #NFR14; #NFR15; _bmad-output/planning-artifacts/architecture.md#Communication-Patterns]
6. Contract, server assembler/projection, authorization, policy-snapshot, and focused tests prove: manifest inspectability without model/tool invocation, presence of all NFR9 fields, included-vs-excluded partitioning with stable reasons, pending/ineligible exclusion (NFR21), redacted denial / empty non-confirming package for unauthorized or cross-tenant callers, idempotent order-tolerant assembly, and absence of any raw content/scanner/provider/path leakage. [Source: _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR9]

## Tasks / Subtasks

- [x] Add the AI-context package manifest contract (metadata-only) (AC: 1, 2, 3, 6)
  - [x] Add a metadata-only manifest record under `src/Hexalith.ChatBot.Contracts/Queries/`, for example `ProjectAiContextPackage`, plus a per-file entry record (for example `ProjectAiContextPackageFile`) and a per-exclusion record (for example `ProjectAiContextPackageExclusion`). Do not overload `ProjectConversationItem`; this is a project-scoped derived contract, not a conversation row.
  - [x] The manifest record MUST carry, as first-class required fields: `TenantId`, `ProjectId`, `PolicySnapshotId`, `RedactionDecision`, `RetentionClass`, `ProviderReuseSetting`, `PackageId`, `PackageVersion`, `SchemaVersion`, `SourceVersion`, `CorrelationId`, an `IncludedFiles` list, an `ExcludedFiles` list, and `SourceEvidenceReferences`. These satisfy NFR9 and the `[ChatBot] Derived-record shape` rule (`tenantId`, `sourceProvenance`, `derivationKernelVersion`, `redactionState`, `retentionClass`, `schemaVersion`).
  - [x] Each `IncludedFiles` entry exposes only stable metadata references already published by Story 3.12/3.13 (for example `FolderId`, `FileId`, `SourceProviderAttachmentId`, redaction state, retention class, source-evidence reference). It MUST NOT carry display name, content type, byte content, base64, local path, provider payload, or scanner detail.
  - [x] Each `ExcludedFiles` entry carries a stable reason code from a fixed vocabulary (for example `pending-scan`, `unsafe`, `rejected`, `failed`, `unavailable`, `retryable`, `redacted`, `policy-denied`, `unauthorized`, `not-yet-eligible`) plus a metadata-only file reference token. Reason codes must be enum-like stable strings, never localized text or raw scanner reasons.
  - [x] Reuse the existing AI-context placeholder fields where they already exist (`AiContextPackageId`, `AiContextPackageVersion`, `AiContextRedactionState`, `AiAuthorizedContextReferences`, `AiExcludedContextReasons` on `ProjectConversationItem`) for cross-referencing; do not duplicate or rename them. If OpenAPI/generated client drift is needed, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` and regenerate the client rather than hand-editing `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`.
- [x] Add a server-side context-package assembler that never invokes a model or tool (AC: 1, 2, 3, 5, 6)
  - [x] Add a server-owned assembler under `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/` (or a sibling `.../AiContext/` folder if cleaner), for example `IProjectAiContextPackageAssembler` plus a default implementation. It reads only already-projected attachment safety/storage/authorization state for one tenant+project — it must perform NO embedding, NO model call, NO Folders content read, and NO external tool call.
  - [x] Include a file in the package only when its latest authorized state satisfies ALL of: scan success (`AttachmentScanStatus` captured/clean), AI-context eligibility is eligible (not `not-eligible`/`pending`), storage reference present and authorized, tenant policy permits AI context for the class, and audit/correlation metadata available. Anything else goes to `ExcludedFiles` with the matching stable reason. Mirror the Story 3.13 eligibility gate exactly — do not re-derive a weaker rule.
  - [x] Stamp the manifest with the policy snapshot id, redaction decision, retention class, and provider-reuse setting resolved from tenant policy/projection context at assembly time. Resolve provider-reuse from the tenant-policy knob (default to the safe/non-reuse value when unset or invalid); do not invent values when a source is unavailable — fail closed to exclusion or an empty package rather than fabricating a snapshot id (apply the Story 3.11 anti-fabrication learning).
  - [x] Make assembly idempotent, source-version stamped, last-writer-wins by source version, and order-tolerant. A stale safety/authorization event must not flip a file's membership; a newer authorized event must. Do not regress terminal exclusion (`unsafe`/`rejected`/`failed`) on a stale clean event.
  - [x] Keep assembly pure/deterministic over projected state so it can be recomputed on read or maintained as a derived projection; if a projection store entry is added, extend `IProjectConversationProjectionStore`, `InMemoryProjectConversationProjectionStore`, and `DaprProjectConversationProjectionStore` with an idempotent upsert mirroring the attachment safety-outcome pattern, keeping the package source version independent of storage/scan source versions.
  - [x] Satisfy FR33 "auditable" packaging **without** introducing a command/mutation: auditability here means the manifest is a durable, inspectable, correlation-stamped derived record carrying the policy snapshot id, source evidence references, redaction decision, and retention class (the audit-envelope metadata fields). Do not emit a new WORM audit command for package read/derive — the package is consumed under the gateway/audit posture when Epic 4 invokes a model, not when Epic 3 assembles the manifest.
- [x] Add authorization and redaction for package access (AC: 1, 4, 6)
  - [x] Reuse the existing tenant/project authority from gateway/projection context and the Story 3.13 `AttachmentAuthorization` boundary. Do not trust route, query, payload, provider, or UI-supplied tenant/project/folder/file ids as authority.
  - [x] An unauthorized actor, stale/revoked authority, foreign tenant, or foreign project must receive a redacted denial or an empty, non-confirming package. Unauthorized and nonexistent projects must be indistinguishable at the caller-visible boundary. A project with zero eligible files returns a well-formed empty package (no included files, NFR9 fields still present), not an error that confirms project existence to unauthorized callers.
  - [x] Per-file references in the manifest are subject to the same redaction as Story 3.13: when a file's attachment metadata is redacted/unauthorized, it must not appear in `IncludedFiles`, and its `ExcludedFiles` entry must use the `redacted`/`unauthorized` reason without leaking folder/file ids or names.
- [x] Expose the manifest through the query contract (AC: 1, 2, 4, 6)
  - [x] Surface the manifest through the existing project conversation query path. Prefer extending the `GET /api/v1/projects/{projectId}/conversation` (`GetProjectConversation`) response with an optional `aiContextPackage` field, OR add a dedicated read endpoint such as `GET /api/v1/projects/{projectId}/ai-context-package` (`GetProjectAiContextPackage`) if the conversation response is the wrong granularity. Choose one; do not add a command/mutation surface — this story only reads/derives.
  - [x] The read path must be metadata-only, ETag-able like the existing conversation read, and must surface `stale|rebuilding|unavailable` projection states rather than pretending freshness. It must never trigger model/tool invocation as a side effect of being read.
  - [x] If a new endpoint or response field is added, update the OpenAPI spine and regenerate the typed client; add the operation to the differential-conformance expectations so all three surfaces stay in parity.
- [x] Add user-safe messaging and localization for exclusion/denial (AC: 3, 4, 6)
  - [x] Add any required stable reason codes/message-catalog entries through `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs` and `ChatBotMessageCodes.cs` (reuse `AssociationAiContextBlocked` / add analogous `*_ai_context_*` codes as needed). User-visible exclusion reasons must come from the versioned catalog, never raw scanner/policy/exception text.
  - [x] If any exclusion reason or package state is surfaced in S1 UI, add EN/FR strings through `ChatBotUiTextKey`, `ChatBotUiTextLocalizer`, `SharedResource.resx`, and `SharedResource.fr.resx`. Keep this story's UI footprint minimal — the package is primarily an inspectable contract for Epic 4; do not build a new dashboard or approval surface (that is Epic 4 S3).
- [x] Add focused tests and validation coverage (AC: all)
  - [x] Assembler tests: a fully authorized clean file is included; a pending-scan file is excluded with `pending-scan`; unsafe/rejected/failed/unavailable/retryable files are excluded with the matching reason; a policy-denied class is excluded with `policy-denied`; a redacted/unauthorized file is excluded with `redacted`/`unauthorized`; an empty project yields a well-formed empty package.
  - [x] NFR9 completeness test: every materialized package (including the empty package) carries tenant id, project id, source evidence references, policy snapshot id, redaction decision, retention class, provider-reuse setting, and excluded-file reasons.
  - [x] No-invocation test: assembling and reading the package performs zero model/embedding/tool/Folders-content calls (assert via test doubles that those ports are never invoked).
  - [x] Idempotency/order tests: duplicate safety/authorization events, stale event after newer admission, stale clean after terminal exclusion, out-of-order delivery, and same provider attachment id with different ordinals all converge to the correct membership without flapping.
  - [x] Authorization/isolation tests: authorized reviewer, unauthorized actor, stale/revoked authority, foreign tenant, foreign project, and nonexistent project produce indistinguishable redacted/empty results; cross-tenant queries are impossible.
  - [x] Contract/OpenAPI/generated-client + differential-conformance tests if the contract or endpoint changes; otherwise regression tests proving existing fields carry the manifest and no raw content/scanner/provider/path fields were added.
  - [x] Leakage tests: assert absence of raw attachment bytes, base64, provider payload/source context, Graph tokens, local file paths, folder/file names under redaction, scanner detail, malware family text, tenant ids in denial bodies, credentials, secrets, and raw exception text in the manifest, denial bodies, logs, and fixtures.

## Dev Notes

### Scope Boundaries

- This story owns the **explicit, auditable AI-context package manifest** that aggregates already-eligible project files into an inspectable contract, plus the server assembler, authorization/redaction, exclusion-reason vocabulary, and read exposure. It is the Epic 3 producer that Epic 4 will consume.
- This story must **NOT** invoke any model, embedding, or external tool; must not read file content; must not implement file download/preview/browsing, user uploads, document intelligence, vector/embedding indexing, AI action proposal/approval/execution, tenant policy administration UI, operational dashboards, or CLI/MCP parity surfaces. Epic 4 (Stories 4.1–4.9) consumes this package and owns mediation/approval/execution.
- Re-derive **no weaker** eligibility rule than Story 3.13. A file is package-eligible only if Story 3.13's `AttachmentAiContextEligibility` says it is eligible AND authorization/policy/audit metadata are present. When in doubt, exclude (fail closed).
- Do not move Folders authority into ChatBot. ChatBot orchestrates and projects safe derived metadata; Hexalith.Folders still owns folder/file aggregates, ACLs, storage metadata, and content access.
- No package upgrades or framework changes. Use repo-pinned .NET SDK `10.0.300`, `net10.0`, central package management, Dapr 1.17.x, Aspire 13.3.x, xUnit v3, Shouldly, NSubstitute.

### Existing Code To Reuse

- Per-item AI-context fields (already present, do not duplicate/rename): `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs` — `AttachmentAiContextEligibility` (line 59), `AiContextPackageId`/`AiContextPackageVersion`/`AiContextRedactionState` (lines 175-177), `AiAuthorizedContextReferences`/`AiExcludedContextReasons` (lines 178-179), and the derived-record fields `RedactionState`/`RetentionClass`/`SchemaVersion`/`SourceVersion`/`CorrelationId` (lines 29-33).
- Attachment eligibility/safety/authorization (Story 3.12/3.13): `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/AttachmentSafetyPolicy.cs`, `AttachmentAuthorization.cs`, `AttachmentCaptureCoordinator.cs`, and `src/Hexalith.ChatBot.Server/Projections/ProjectConversationAttachmentSetView.cs` (`AiContextEligibility`, `RedactionState`, `RetentionClass`, `WithStorageOutcome`/`WithSafetyOutcome` membership rules), `ProjectConversationAttachmentStorageView.cs`, `ProjectConversationItemView.cs`.
- Projection stores: `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`, `InMemoryProjectConversationProjectionStore.cs`, `DaprProjectConversationProjectionStore.cs` (idempotent, source-version-stamped upsert pattern).
- Query/read path: `GET /api/v1/projects/{projectId}/conversation` (`GetProjectConversation`) in `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` (line ~181), `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationResponse.cs`, `ProjectConversationCursorPage.cs`.
- Folders adapter boundary: `src/Hexalith.ChatBot.Server/Adapters/Folders/IFolderStore.cs`, `FoldersFolderStore.cs`, `UnavailableFolderStore.cs`, `AttachmentStorageIdentity.cs`.
- Message catalog/denial: `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotMessageCodes.cs` (existing `AssociationAiContextBlocked` = `association_ai_context_blocked`), `src/Hexalith.ChatBot.Server/Gateway/ChatBotProblemDetailsFactory.cs`.

### Current State To Preserve

- Story 3.13 added `AttachmentAiContextEligibility` gating: a file becomes AI-context eligible only after scan success, tenant policy allows it, project/file authorization passes, and audit/correlation metadata are available. `ProjectConversationAttachmentSetView.WithStorageOutcome`/safety-outcome already prevent stale failures from erasing captured `FolderId`/`FileId` and hide stored references once a safety gate blocks an item — the package must respect those hidden references (never reach around the view to read raw stored ids).
- `ProjectConversationItemView.BuildAttachmentFacet` already computes scan-driven health and `AttachmentAllowedActions` (excludes AI-context actions when ineligible). Drive package membership from the same computed eligibility, not from raw enum strings re-read elsewhere.
- The contract already reserves `AiContextPackageId`/`AiAuthorizedContextReferences`/`AiExcludedContextReasons` on AI-outcome items for Epic 4 cross-reference. Populate/align the package id with these rather than introducing a competing id scheme.
- Existing worktree has unrelated modified root submodule pointers `Hexalith.EventStore` and `Hexalith.Tenants` plus `_bmad-output/story-automator/orchestration-2-20260531-161212.md`; do not revert, stage, or commit those as part of this story.

### Architecture Guardrails

- Dependency direction is `Contracts <- Client <- UI/Server`. The manifest record lives in `.Contracts`; the assembler, authorization, policy-snapshot resolution, and projection live in `.Server` only (the single scanned assembly) — never in `.UI`, `.Client`, CLI, MCP, or generated client code.
- This story is **read/derive only**. Do not create a command or mutate state outside the gateway. The package is a derived projection/read; no `IChatBotCommand`, no aggregate `Handle` change. Reads surface `stale|rebuilding|unavailable` rather than pretending freshness.
- Derived-record shape is mandatory: the manifest carries `tenantId`, `sourceProvenance`, `derivationKernelVersion`/`schemaVersion`, `redactionState`/redaction decision, `retentionClass`. JSON camelCase, `System.Text.Json` shared options, `DateTimeOffset` UTC.
- Tenant/project authority comes from authenticated claims and server-side projection/gateway context, never route/body/query/provider values. Unknown, foreign, unauthorized, stale, malformed, or degraded contexts collapse to redacted denial / empty non-confirming package. Cross-tenant queries must be impossible at the store-access layer.
- Dapr pub/sub and worker delivery are at-least-once and unordered → all projection/handler updates idempotent, source-version stamped, order-tolerant, last-writer-wins by source version.
- Use exact attachment status wire values (lowercase enum-member strings): `captured`, `pending`, `unavailable`, `rejected`, `unsafe`, `failed`, `retryable`. Exclusion reason codes are likewise stable strings, never localized text.
- User-safe text comes from versioned message/localization catalogs. Raw error text, scanner detail, unauthorized file/folder names, tenant ids, provider payloads, or file paths in user-visible output are release-blocking defects (NFR40).
- Root submodule policy applies: initialize only root `.gitmodules` entries; never use recursive submodule commands.

### Previous Story Intelligence

- Story 3.13 (done, commit `2eae6da`) senior review fixed: unsafe-handling must be resolved through the coordinator path (not just normalized in the policy); avoid double scanner execution; terminal safety outcomes need an explicit supersession flag; stored folder/file ids must be hidden once a safety gate blocks an item. Apply the same mindset: package membership must be driven by the gated/computed eligibility view, terminal exclusion must not be undone by stale clean events, and hidden references must stay hidden.
- Story 3.12 fixed captured-reference preservation (a later failure cannot erase stored `FolderId`/`FileId`) and required the coordinator to be invoked from the normal projection flow. The package assembler should likewise read from the normal projection state, not a side channel.
- Story 3.11 fixed provenance fabrication: never fabricate policy snapshot ids, retention class, redaction decision, or provider-reuse settings from unavailable data. Fail closed to exclusion / empty package instead of inventing values.
- Story 3.10 established that pending projection must not display as done: a pending-scan or not-yet-eligible file must be excluded (not optimistically included) and recorded as excluded with a safe reason.
- Validation sandbox gotcha: `dotnet test` via VSTest fails here with `SocketException (13): Permission denied`. Prefer compiled in-process xUnit v3 runners (run the built test `.dll` directly with `-parallel none -class <FQN>` / `-method <FQN>`).

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - Targeted compiled xUnit v3 runners for Server (assembler/projection/authorization), Contracts/Client if the contract or OpenAPI changes, Conformance/differential-conformance and isolation, and focused UI/E2E only if a UI surface is touched.
- Prove the no-invocation invariant with test doubles for any model/embedding/tool/Folders-content port, asserting zero calls during assemble + read.
- Include replay/order tests: duplicate safety/auth events, stale-after-newer admission, stale-clean-after-terminal-exclusion, out-of-order delivery, and same provider attachment id with different ordinals.
- Include NFR9 completeness assertions on every materialized package, including the empty-project package.
- Include redaction/isolation tests proving unauthorized and nonexistent projects produce indistinguishable caller-visible results, and leakage assertions for raw bytes/base64/provider payload/Graph tokens/paths/folder-file names/scanner detail/tenant ids/secrets/raw exceptions.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md` (Epic 3 header + Story 3.14, with forward reference from Epic 4 Story 4.4 confirming the package is the consumed contract).
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md` (Governed AI Mediation, Implementation Patterns: naming/structure/format/communication, derived-record shape, audit envelope, NFR9a isolation deferral note).
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` (FR33, NFR9, NFR8, NFR21, FR32, NFR2/NFR6/NFR11/NFR13/NFR14/NFR15) and `addendum.md` (tenant policy schema, policy-snapshot semantics).
- Inspected source to confirm reuse points: `ProjectConversationItem.cs` already reserves AI-context package fields; `ProjectConversationAttachmentSetView.cs` already computes `AiContextEligibility`; projection stores already expose the idempotent source-version-stamped upsert pattern; `GetProjectConversation` is the existing project-scoped read.
- Latest-technology research not required: this story is constrained to repo-pinned versions and local generated clients; no external package upgrades.

### References

- `_bmad-output/planning-artifacts/epics.md` — Epic 3 header and Story 3.14; Epic 4 Story 4.4 forward reference (`uses only the authorized context package (NFR8/NFR9)`).
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` — FR33 (scoped AI context only through explicit authorization, policy checks, auditable packaging), NFR9 (required package fields before model/tool invocation), NFR8, NFR21, FR32, NFR2/NFR6/NFR11/NFR13/NFR14/NFR15.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` — tenant policy schema and policy-snapshot semantics.
- `_bmad-output/planning-artifacts/architecture.md` — Governed AI Mediation, Implementation Patterns & Consistency Rules (naming/structure/format/communication/process), derived-record shape, audit envelope, project structure & boundaries.
- `_bmad-output/implementation-artifacts/3-13-attachment-status-states-and-authorization.md` — attachment AI-context eligibility gate, authorization/redaction, idempotency, senior-review learnings.
- `_bmad-output/implementation-artifacts/3-12-attachment-capture-and-governed-folder-storage.md` — storage coordinator/projection baseline and captured-reference preservation.
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs` — existing AI-context package fields and derived-record fields.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationAttachmentSetView.cs` — computed `AiContextEligibility`/redaction/retention and membership-preservation rules.
- `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/AttachmentSafetyPolicy.cs`, `AttachmentAuthorization.cs`, `AttachmentCaptureCoordinator.cs` — safety/authorization gates to reuse.
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs` + in-memory/Dapr stores — idempotent source-version-stamped upsert pattern.
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` — `GetProjectConversation` read path and contract spine.
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotMessageCodes.cs` — versioned message catalog and `AssociationAiContextBlocked` code.
- `Hexalith.Folders/_bmad-output/project-context.md` — Folders metadata-only and authorization/redaction invariants.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story workflow executed in a single Claude session under user orchestration constraint `max parallel = 1`: no subagents, no nested workflows, no parallel fan-out.
- Sprint status read fully; `3-14-scoped-ai-context-packaging-from-authorized-files` was `backlog` and `epic-3` was already `in-progress` (no epic status change needed).
- Artifact discovery loaded whole `epics.md` and `architecture.md`; nested PRD/addendum artifacts read manually for FR33/NFR9/NFR21/FR32 detail.
- Previous story intelligence loaded from Story 3.13 (done) and recent commit `2eae6da feat(story-3.13): enforce attachment safety states`.
- Checklist validation applied during story creation; no user input requested per `#YOLO`.
- Implemented in dev-story workflow on 2026-06-01.
- Dev-story run read sprint status fully; 3.14 started as `ready-for-dev`, was marked `in-progress`, and was moved to `review` after validation.
- Red phase confirmed with failing compiler errors for missing `ProjectAiContextPackage`, `ProjectAiContextPackageAssemblyRequest`, and `DefaultProjectAiContextPackageAssembler`.
- Fixed one assembler idempotency test setup issue where the second call omitted the association policy snapshot carrier.
- Fixed architecture gate violation by avoiding a hard-coded legacy lifecycle literal in production source.
- Fixed conformance leakage by changing the caller-visible package tenant field to a stable non-raw tenant reference.
- Fixed OpenAPI safety scan by removing restricted wording from the new package schema description.
- Validation used compiled in-process xUnit v3 runners because story notes warn `dotnet test`/VSTest can fail in this sandbox.
- Senior developer review executed on 2026-06-01 using `.agents/skills/bmad-story-automator-review`.
- Review finding fixed: package policy snapshot and retention metadata now resolve from the newest projected state instead of item-id ordering.
- Review finding fixed: clean attachments without source evidence metadata remain excluded as `not-yet-eligible` instead of being admitted with an item-id fallback.

### Completion Notes List

- Added metadata-only `ProjectAiContextPackage`, included-file, and exclusion records with NFR9 fields, source provenance, derivation kernel version, stable package identity, and stable exclusion reason codes.
- Extended `ProjectConversationResponse` with optional `AiContextPackage` and updated the OpenAPI spine plus regenerated checked-in NSwag client output and freshness hash.
- Added deterministic server assembler registered in DI. It reads already-projected conversation attachment metadata, never invokes model/embedding/tool/Folders-content ports, includes only clean eligible authorized attachments, and excludes pending, unsafe, rejected, failed, unavailable, retryable, redacted, unauthorized, policy-denied, and not-yet-eligible files with stable reasons.
- Added full-project projection reads for package assembly in both in-memory and Dapr stores so package membership is not limited by conversation pagination.
- Reused existing project conversation read authorization/denial boundaries and added non-raw tenant-scope references to avoid tenant sentinel leakage in authorized bodies.
- Added versioned message catalog support for package-unavailable messaging without adding S1 UI surface strings.
- Added focused assembler, contract, client freshness, architecture, conformance, and leakage coverage; full compiled regression suite passes.

### File List

- `_bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`
- `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCodes.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectAiContextPackage.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationResponse.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Attachments/ProjectAiContextPackageAssembler.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/MessageCatalogContractTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectAiContextPackageContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/ProjectAiContextPackageAssemblerTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Change Log

- 2026-06-01: Added scoped AI-context package manifest contract, deterministic server assembler, project conversation read exposure, OpenAPI/generated client updates, message catalog entry, focused tests, and full validation coverage.
- 2026-06-01: Senior developer review fixed latest-policy metadata selection and source-evidence fail-closed admission; status moved to done.

## Senior Developer Review (AI)

### Review Checklist

- [x] Story file loaded from `_bmad-output/implementation-artifacts/3-14-scoped-ai-context-packaging-from-authorized-files.md`.
- [x] Story Status verified as reviewable (`review`) before review and updated to `done` after fixes.
- [x] Epic and Story IDs resolved as 3.14.
- [x] Story context and planning references reviewed from story Discovery Results and References.
- [x] Architecture/standards docs loaded from `_bmad-output/planning-artifacts/architecture.md` and `Hexalith.Folders/_bmad-output/project-context.md`.
- [x] Tech stack detected: .NET SDK 10.0.300, `net10.0`, Dapr/Aspire, OpenAPI/NSwag, xUnit v3, Shouldly.
- [x] External doc search not required; story is constrained to repo-pinned local contracts and generated client artifacts.
- [x] Acceptance Criteria cross-checked against implementation.
- [x] File List reviewed and corrected for changed source/test/story artifacts.
- [x] Tests identified and mapped to ACs; targeted gaps fixed.
- [x] Code quality and security review performed on changed source files.
- [x] Outcome: Approve after automatic fixes.

### Findings Fixed

- **HIGH**: `ProjectAiContextPackageAssembler` selected `PolicySnapshotId` and package `RetentionClass` from the first nonblank item after item-id ordering, so an older projected item could stamp a stale policy snapshot on a newer package. Fixed by resolving latest nonblank metadata by `SourceVersion` and `CorrelationId`.
- **HIGH**: A fully clean attachment with missing source-evidence metadata could be included because the assembler fell back to the attachment item id as evidence. Fixed by requiring real source evidence metadata before inclusion and excluding the file as `not-yet-eligible` when unavailable.
- **MEDIUM**: Story File List omitted changed review/test artifacts. Fixed by adding the story test summary and API test file entries.

### Validation

- [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- [x] `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -parallel none -class Hexalith.ChatBot.Server.Tests.Lifecycle.ProjectAiContextPackageAssemblerTests`
- [x] `dotnet tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests.dll -parallel none -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldExposeAiContextPackageManifestMetadataOnly -method Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests.ProjectConversationEndpointShouldOmitAiContextPackageFromRedactedDenials`
- [x] `dotnet tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests.dll -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectAiContextPackageContractTests -class Hexalith.ChatBot.Contracts.Tests.MessageCatalogContractTests`
- [x] `dotnet tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests.dll -parallel none -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests`
- [x] `dotnet tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests.dll -parallel none -class Hexalith.ChatBot.Conformance.Tests.ContractSpineOracleTests`
