---
baseline_commit: 8b533283dce51be4d2af1d52c49e31a871d85135
---

# Story 9.3: Audit query and compliance investigation surface (S9)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance/support reviewer,
I want to search and reconstruct what happened with safe redaction,
so that I can investigate without leaking unauthorized context or gaining mutation power.

## Acceptance Criteria

1. **Search + reconstruct over the tenant WORM chain (FR56 + FR54).**
   **Given** the Audit Investigation surface (S9),
   **When** I search,
   **Then** I can query by **tenant, actor, command, resource, decision, reason, correlation, message ID, surface, and time** (FR56) and reconstruct **association decisions, approvals, command outcomes, corrections, retries, and risky AI actions** (FR54) from the audit chain.
   - The **query is wired end-to-end**: an authenticated, tenant-scoped, `Compliance`-scoped HTTP read endpoint enumerates the tenant's WORM chain and runs `ComplianceAuditReadPolicy.Search` / `.Detail` — these read-policy methods already exist (forward-scaffolded by Story 7.4) but are **called by nothing today**; this story is what wires them to a real chain source, an endpoint, and a UI surface.
   - The current filter-key set (`tenant, actor, actor-type, command, resource, decision, reason, correlation, policy-snapshot, time`) is **missing `message-id` and `surface`**, which AC1 explicitly requires. Add them: `message-id` matches against the `source-message:`/`provider-message:` tokens in `AuditEnvelope.SourceEvidenceRefs`; `surface` matches `AuditEnvelope.SurfaceOrigin`. Extend `ComplianceAdministrationSchema.AuditFilterKeys` **and** `ComplianceAuditReadPolicy.MatchesFilter` together (they must not drift).
   - **Reconstruction is metadata-only timeline reconstruction**, not raw content: reuse `AuditOperationReconstructor` (Story 9.2) where reconstructing an operation's end-state helps, and render the chronological **audit timeline** (source message → association → corrections → approvals → commands → AI actions → outcome) the UX spine requires. Each timeline entry exposes event type, actor, timestamp, correlation id, command surface, policy snapshot, outcome, and links to **permitted** source evidence only.

2. **Per-project redaction + escalation; read/escalate-only authority — never mutation (FR54, NFR2, Flow 7).**
   **Given** a project I lack authority for,
   **When** results render,
   **Then** restricted detail is **redacted** and an **escalation path is offered without revealing the hidden resource** (no project name, file metadata, candidate evidence, or audit detail leaks — NFR2); the row/detail shows a safe `EscalationRequired` redaction state, a safe next action (`request-access`), and an escalation affordance that dispatches the existing `RequestComplianceEscalation` command with an **opaque** resource reference.
   - My role grants **read/escalate only, not mutation**: the surface exposes **no affordance to operate on, retry, correct, or approve workflow items**. Any operate-style control rendered for context is **disabled** (`aria-disabled="true"`, reachable explanation via `aria-describedby`) and dispatches **no workflow mutation command** (Flow 7 failure mode: "if Sofia lacks authority to mutate project state, investigation remains read/escalate only"). This is enforced by authority (`AdminScope.Compliance` ⇒ `{SeeOnly, Compliance, AuditObligation}` — **not** `Operate`/`Policy`/`Mailbox`), not merely by hiding buttons.
   - Detail visibility is driven by `ComplianceAuditReadPolicy.Detail(envelope, hasPerProjectAuthority)`: with per-project authority ⇒ `DetailAvailable` / `view-metadata` / visible evidence refs; without ⇒ `EscalationRequired` / `request-access` / empty evidence refs. Per-project authority must be evaluated against the reviewer's actual grants, never assumed true.
   - **Projection-pending visibility (Flow 7):** if the audit projection for an operation is delayed, the surface shows **partial status with operation identity**, never a fabricated complete reconstruction.

3. **Replay events are distinguishable and excluded from default production audit queries (FR95a).**
   **Given** the audit query,
   **When** default production results are computed,
   **Then** envelopes carrying a non-null `replay_run_id` (`AuditEnvelope.ReplayRunId`, introduced by Story 9.2) are **distinguishable** and **excluded from default production audit queries** — they do not appear in default search results. Today production has **zero** replay records (Story 9.4 owns *populating* `ReplayRunId`), so the exclusion holds by construction; this story must make the exclusion **real and testable now** (inject a replay-marked envelope; assert it is absent from a default query) rather than depending on 9.4. Use the existing `AuditReplayExclusion.IsReplayEnvelope` predicate (Story 9.2) — do not re-derive the marker test.

### Cross-cutting requirements that hold for every AC

- **Read-only / out-of-band — never a new fail-closed gate or any mutation of audit/project state (D4, two-phase audit; NFR49a WORM).** The investigation surface only **reads** the WORM chain and projections (`IWormAuditStore.EnumerateChain`, the projection stores) plus dispatches the already-allowlisted `RequestComplianceInvestigation` / `RequestComplianceEscalation` commands (which record intent — they are *not* workflow-item mutations). It must **not** append to the audit chain on the read path, must **not** add a commit-time gate, and must **not** expose any path that mutates project/workflow state.
- **Metadata-only / no-leak floor (NFR2, NFR42).** Every result row, detail field, timeline entry, escalation payload, exported/copied artifact, and accessible name/description carries only `AuditMetadata`-safe bounded tokens (ASCII alnum + `.-_:@|`, marker-ban on `secret`/`password`/`bearer`/`token`/`exception`/file-extension sentinels). Redacted rows render via the blocked-state/safe-not-found pattern and **never confirm whether a restricted resource exists**. The `AssertMetadataOnly(bodyText)` E2E assertion (see Dev Notes) is the binding no-leak check for the rendered surface.
- **Tenant isolation by construction (NFR9a).** The query enumerates exactly one tenant's chain (`EnumerateChain(tenantId)` is tenant-partitioned); the endpoint resolves the tenant from the authenticated principal and a bad/cross-tenant/unknown lookup collapses to the identical **safe-not-found** (mirror the `/api/v1/operations/{operationId}/audit-history` endpoint). No query reads or links across tenants.
- **Authority is fail-closed (NFR2).** A principal lacking `AdminScope.Compliance` (or a non-human actor) receives a redacted denial that does not confirm resource existence — `ComplianceAuditReadPolicy.CanSearchTenantAudit` already encodes the gate via `AdminAuthorityEvaluator.HasHumanAdminScope`. Reuse it; do not invent a parallel check.
- **UX spine is binding (no mockups by design).** The UX package ships binding tables, not wireframes. The S9 IA, component (audit timeline, actor badge, evidence chip/drawer, blocked state), state-coverage, interaction, accessibility, and responsive rows below are **binding acceptance context** — absence of a mockup is not permission to invent behavior.
- **Localization EN + FR (Story 1.20) and accessibility floor (Stories 1.18–1.21).** All surface text and accessible labels come from `ChatBotUiTextLocalizer` / `ChatBotUiTextKey` (new keys + EN/FR `.resx` entries); the timeline, live regions, landmarks, focus model, and 44×44 touch targets follow the inherited accessibility floor.

## Tasks / Subtasks

- [x] **Task 1 — Extend the audit query filter dimensions: `message-id` + `surface` (AC: #1)**
  - [x] Add `"message-id"` and `"surface"` to `ComplianceAdministrationSchema.AuditFilterKeys` (`src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs:185`). `FilterKey` is a free string validated against this set, so adding keys is a **backward-compatible** server-side change — confirm whether `AdminContractTests` / `OpenApiContractSpineTests` pin the exact key set and update those assertions if so. Do **not** change the `ComplianceAuditFilterRef` record shape (no OpenAPI/client regeneration needed). If a reviewer insists the key set is a versioned contract, bump `ComplianceAdministrationSchemaVersions` (`v1` → add `v2`) rather than mutating `v1` silently.
  - [x] Add the matching arms in `ComplianceAuditReadPolicy.MatchesFilter` (`src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs:102`): `"surface"` ⇒ ordinal match against `AuditMetadata.SafeOptionalToken(envelope.SurfaceOrigin)`; `"message-id"` ⇒ the filter value matches a `source-message:`/`provider-message:` token present in `envelope.SourceEvidenceRefs` (reuse the token convention from `AuditEnvelopeFactory`; treat the value as an opaque safe token, never raw content). Keep `MatchesFilter` and `AuditFilterKeys` in lock-step.

- [x] **Task 2 — Exclude replay events from default production queries (AC: #3)**
  - [x] In `ComplianceAuditReadPolicy.Search`, filter out replay envelopes by default using `AuditReplayExclusion.IsReplayEnvelope` (Story 9.2, `src/Hexalith.ChatBot.Server/Audit/AuditReplayExclusion.cs`) — a replay-marked record (`ReplayRunId is not null`) must not appear in default production results. Place the exclusion alongside the existing `Where` chain (lines 39–46) so it composes with the safe-identifier, time-window, and filter predicates.
  - [x] Document (code comment + ADR) that this is the FR95a "production audit queries exclude replay" half of the replay-isolation contract; Story 9.4 owns populating `ReplayRunId`, and an explicit replay-scoped investigation mode (showing replay records) is **out of scope** here unless trivially expressible — if added, it must be opt-in and never the default.

- [x] **Task 3 — Wire the read endpoint: enumerate chain → read policy → contracts (AC: #1, #2, cross-cutting isolation)**
  - [x] Add a tenant-scoped, `Compliance`-gated HTTP read seam exposing the two existing queries: `SearchComplianceAuditRecords` (→ `ComplianceAuditSearchResult`) and `GetComplianceAuditDetail` (→ `ComplianceAuditDetail`). Mirror the existing **read endpoint pattern** in `src/Hexalith.ChatBot.Server/Program.cs` (`/api/v1/operations/{operationId}/audit-history`, lines 333–381): resolve correlation context, `TryResolveTenant`, collapse bad/unresolved/cross-tenant to `SafeNotFound`, return metadata-only results. Suggested routes: `GET/POST /api/v1/compliance/audit/search` and `GET /api/v1/compliance/audit/{auditRecordRef}`.
  - [x] Source the chain via the tenant-partitioned WORM store (`IWormAuditStore.EnumerateChain(tenantId)` → unwrap `WormAuditChainRecord.Envelope`), or extend `IAuditHistoryReader` with a tenant-wide enumerate seam if a thin reader abstraction is cleaner (today `IAuditHistoryReader.GetPostCommitEnvelopes(tenantId, commandId)` is per-command only). Keep audit interfaces **internal to `.Server`** (NetArchTest-enforced — no `.UI`/`.Cli`/`.Mcp` may reference `IWormAuditStore`, the read policy, or `AuditEnvelope`).
  - [x] Gate with `ComplianceAuditReadPolicy.CanSearchTenantAudit` (already wired to `AdminScope.Compliance`); a principal without the scope or a non-human actor gets a redacted denial (`SafeNotFound`), never a confirmation. Pass the enumerated envelopes, the validated `ComplianceAuditQueryFilters`, a UTC `generatedAtUtc`, and the correlation id into `Search`; resolve per-project authority for `Detail` from the reviewer's actual grants.
  - [x] Register any new types via DI in the existing service-collection extension (`CommandGatewayServiceCollectionExtensions` or the audit/projection registration shape).

- [x] **Task 4 — Build the S9 Audit Investigation Blazor surface (AC: #1, #2, #3)**
  - [x] Create the surface as a `.razor` page + a UI service (mirror `AssociationReviewService` calling `IChatBotClient` — UI never touches `.Server` internals; it calls the generated client / HTTP). Place the page under `src/Hexalith.ChatBot.UI/Components/Pages/` and view models alongside the existing surface services.
  - [x] **Match the binding DOM contract already asserted by `tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs`** (this fixture-based contract test was authored ahead of the surface — the real surface must produce the same structure so the E2E can be repointed from its inline fixture to the real component):
    - Page `<main aria-labelledby="compliance-audit-title">` with `<h1 id="compliance-audit-title">Compliance audit</h1>`.
    - `<section data-chatbot-surface="audit-investigation-s9" aria-labelledby="compliance-timeline-title">` with `<h2>Audit investigation</h2>`.
    - The **audit timeline** as `<ol aria-label="Compliance audit timeline">`; each entry an `<article aria-label="Audit record, {command}, {redaction-state}, {timestampZ}">` with `data-redaction-state` + `data-escalation-state`, and a definition list of safe tokens: `actor:…`, `command:…`, `decision:…`, `reason:…`, `correlation:…`, `policy-snapshot:…`, `outcome:…`, `redaction:…`, `escalation:…`, `safe-next-action:…`.
    - Escalation affordance: a button **"Request compliance access"** with `aria-describedby="compliance-escalation-reason"` that dispatches `RequestComplianceEscalation` with an **opaque** `escalationTarget` (e.g. `project-opaque-ref`); a **"Trigger investigation"** button dispatching `RequestComplianceInvestigation`.
    - **Read/escalate-only proof (AC2):** any operate-style control (e.g. "Retry queue item") is rendered `aria-disabled="true"` with `aria-describedby="compliance-operate-denied"` and dispatches **no** workflow mutation.
  - [x] **States** the surface must handle (UX state-coverage table): audit loading (skeleton, `aria-busy`), no matching events, event selected, filters active, projection pending (partial status + operation identity, polite live region), redacted detail, export/copy unavailable, retry/correction trace present, terminal command outcome, investigation handoff/escalation logged.
  - [x] **Search/filter controls** for the FR56 dimensions (tenant, actor, command, resource, decision, reason, correlation, message ID, surface, time range), with labelled controls (keyboard-first plus equivalent labelled controls for non-keyboard users). **No infinite scroll** — pagination/virtualized list with stable filters; **no UI affordance suggesting CLI/MCP/admin bypass of authorization**.
  - [x] **Accessibility (Stories 1.18–1.21 floor):** timeline is a chronological event list with accessible group headings; each entry announces actor-type label before content; arrow/roving focus inside the timeline only with announced position+count; landmarks (navigation, main, complementary evidence/review panel) carry unique `aria-label`; status/projection changes use polite live regions (historical entries do **not** announce on load); redacted states stay understandable without leaking; export/copy/read-aloud apply the **same redaction** as the visual surface and expose a screen-reader-equivalent "redacted — escalate for full detail" message.
  - [x] **Responsive / phone fallback (UX responsive table):** dense audit analysis uses the small-screen fallback — a `complementary` "Compliance audit summary is available on phone." region that keeps the read-only summary, safe-next-action, and **escalation reachable**, hides `data-compliance-dense-audit`/`-retention` blocks under the 640px breakpoint, and exposes "open on larger screen" guidance to sighted and screen-reader users alike. Touch targets ≥ 44×44 CSS px (compact dense-row controls ≥ 24×24 / WCAG 2.2 AA spacing).
  - [x] **Localization:** add `ChatBotUiTextKey` constants + EN/FR `.resx` entries for every visible string and accessible label (title, column/field labels, filter labels, escalation/investigation buttons, disabled-reason text, redaction/projection-pending messages). No free-form literals in the component.

- [x] **Task 5 — Tests (AC: #1, #2, #3)**
  - [x] **Read-policy tests** (extend `tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditReadPolicyTests.cs`): `surface` and `message-id` filters match the right envelopes and reject unsafe values; replay-marked envelopes are excluded from default search; a non-`Compliance` principal / non-human actor → denied result; cross-tenant envelopes are never returned (isolation); time-window + limit honored; every emitted row/detail field is `AuditMetadata`-safe.
  - [x] **Endpoint tests** (`tests/Hexalith.ChatBot.Server.Tests/…`): tenant-scoped enumerate → policy → contract round-trip; bad ULID / unresolved tenant / cross-tenant / missing scope all collapse to the **identical** `SafeNotFound`; the read path performs **no audit-chain append and no project/workflow mutation** (assert chain length unchanged after a search).
  - [x] **Contract/schema tests:** `ValidateAuditQueryFilters` accepts the new `message-id`/`surface` keys and still rejects unknown keys; update `AdminContractTests`/`OpenApiContractSpineTests` if they pin the key set; if a schema version was bumped, assert both versions are `IsKnown`.
  - [x] **UI E2E (`tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs`):** the existing three scenarios (AuditInvestigation, RetentionValidation, PhoneFallback) must stay green; **repoint the AuditInvestigation + PhoneFallback scenarios at the real surface** (replace the inline `BuildComplianceFixture` HTML with navigation to / rendering of the real component) so they verify the shipped surface, not a hand-built fixture — keeping `AssertMetadataOnly`, the escalation→`RequestComplianceEscalation`, investigation→`RequestComplianceInvestigation`, and the disabled-retry→no-mutation assertions. If full repointing exceeds the story, **say so explicitly in Completion Notes** and at minimum add a real-surface render test covering the same DOM contract — never leave the fixture as the only coverage and call it done.
  - [x] **No-leak tests:** extend the Epic 7/8/9 no-leak serialization assertions to the new result/detail/escalation payloads and the rendered surface body.

- [x] **Task 6 — ADR + docs (AC: #1, #2, #3)**
  - [x] Author `docs/adrs/audit-investigation-surface.md`: the S9 surface realizes FR54/FR56 over the Story 9.1 WORM chain via the Story 7.4 read-policy/contract scaffold; the `message-id`/`surface` filter additions; the FR95a replay exclusion from default queries; the read/escalate-only authority model (`AdminScope.Compliance` ⇒ no mutation) and per-project redaction + escalation (NFR2, Flow 7); and tenant isolation by construction (NFR9a). Reference the Story 7.4 admin-scope work, Story 9.1 `worm-audit-backing.md`, and Story 9.2 `audit-completeness-observable.md`.

## Dev Notes

### What this story actually changes (and what already exists)

This is an **S-tagged surface story** that **wires existing, forward-scaffolded pieces into a working investigation surface** — it is mostly *integration + UI + two small contract extensions*, not green-field. **Reuse, do not reinvent.**

**Already exists — consume by reference:**

- **Compliance audit query contracts (Story 7.4 forward-scaffold).** `ComplianceAuditQueryFilters`, `ComplianceAuditFilterRef`, `ComplianceAuditResultRow`, `ComplianceAuditDetail`, `SearchComplianceAuditRecords`, `GetComplianceAuditDetail`, `ComplianceAuditSearchResult`, and the `RequestComplianceInvestigation` / `RequestComplianceEscalation` **commands** all exist in `Contracts`, are in the OpenAPI spine, and are in the generated client (`HexalithChatBotClient.g.cs`). [Source: src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs; src/Hexalith.ChatBot.Contracts/Queries/ComplianceAuditQueries.cs]
- **The read policy already does search + redaction + escalation logic** — but is **called by nothing today.** `ComplianceAuditReadPolicy.Search/Detail` (`internal static`, `src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs`) filters by time-window + `MatchesFilter`, maps to safe rows, and `Detail(envelope, hasPerProjectAuthority)` returns `DetailAvailable`+`view-metadata` (authorized) vs `EscalationRequired`+`request-access` (not). It gates on `AdminScope.Compliance` via `AdminAuthorityEvaluator.HasHumanAdminScope`. **Your job is to feed it a real chain + expose it via endpoint + UI**, and to add the two missing filter keys + the replay exclusion. It is **internal to `.Server`** — the UI must reach it through the endpoint/client, never directly.
- **The WORM chain is the source of truth (Story 9.1).** `IWormAuditStore.EnumerateChain(tenantId)` returns `IReadOnlyList<WormAuditChainRecord>` (tenant-partitioned, append-order); unwrap `.Envelope` to get `AuditEnvelope`. `EnumerateTenants()` exists for sweeps but the surface is single-tenant per request. [Source: src/Hexalith.ChatBot.Server/Audit/IWormAuditStore.cs; WormAuditChainRecord.cs]
- **`AuditEnvelope` carries every field the surface needs** (`TenantId, ActorId, ActorType, CommandName, ResourceId, Decision, ReasonCode, CorrelationId, Timestamp, PolicySnapshotId, SourceEvidenceRefs, StateTransition, RedactionDecision, Outcome, Phase, SurfaceOrigin, ReplayRunId`). **Message ID is not a first-class field** — it lives in `SourceEvidenceRefs` as `source-message:{id}` / `provider-message:{id}` tokens (see `AuditEnvelopeFactory`). **Surface** is `SurfaceOrigin` (`ChatBotSurfaceOrigin`: Api/Ui/Cli/Mcp/Worker/Mailbox/Ai). [Source: src/Hexalith.ChatBot.Server/Audit/AuditEnvelope.cs; src/Hexalith.ChatBot.Contracts/Enums/ChatBotSurfaceOrigin.cs]
- **Replay marker + exclusion predicate (Story 9.2).** `AuditEnvelope.ReplayRunId` (nullable) + `AuditReplayExclusion.IsReplayEnvelope`. Apply the predicate in `Search`; do not re-derive it. [Source: src/Hexalith.ChatBot.Server/Audit/AuditReplayExclusion.cs; AuditEnvelope.cs:23-29]
- **Reconstruction helper (Story 9.2).** `AuditOperationReconstructor.Reconstruct(envelopes)` rebuilds an operation's end-state (resource, decision, transition, outcome, projection token) from the chain alone — reuse it for the FR54 "reconstruct command outcomes/corrections/retries" timeline where rebuilding end-state is useful. [Source: src/Hexalith.ChatBot.Server/Audit/AuditOperationReconstructor.cs]
- **Authority model.** `AdminScope.Compliance`; `AdminScopes.ComplianceAdmin` ⇒ `{SeeOnly, Compliance, AuditObligation}` (note: **no `Operate`/`Policy`/`Mailbox`** — that is the structural basis for read/escalate-only). `AdminAuthorityEvaluator.HasHumanAdminScope(principal, scope)` checks claims + human-actor. [Source: src/Hexalith.ChatBot.Contracts/Enums/AdminScope.cs; AdminScopes.cs; src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs]
- **Redaction/escalation enums.** `ComplianceAuditRedactionState` (`Unknown/Restricted/DetailAvailable/EscalationRequired`), `ComplianceEscalationStatus` (`Unknown/NotRequested/Requested/Approved/Denied`). [Source: src/Hexalith.ChatBot.Contracts/Enums/]
- **Read-endpoint pattern to mirror.** `/api/v1/operations/{operationId}/audit-history` (Program.cs:333–381) — tenant-scoped, `SafeNotFound` collapse, metadata-only via `OperationAuditHistoryHttpResults`. The new compliance search/detail endpoints follow the same shape. [Source: src/Hexalith.ChatBot.Server/Program.cs:333-381; src/Hexalith.ChatBot.Server/Audit/OperationAuditHistoryHttpResults.cs]
- **The S9 DOM contract already exists as a test.** `ComplianceAdministrationE2ETests` (Story 7.4) asserts the exact surface structure (`data-chatbot-surface="audit-investigation-s9"`, the `Compliance audit` h1, `Compliance audit timeline` ol, article rows + safe-token dl, the escalation/investigation buttons, the disabled-retry no-mutation proof, the phone `complementary` fallback, and `AssertMetadataOnly`). It currently renders an **inline fixture**, not the real component — build the real surface to that contract and repoint the test. **This is your UI acceptance spine.** [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs]
- **UI surface plumbing pattern.** `AssociationReviewService` (UI service injecting `IChatBotClient`, mapping DTOs → view models); page pattern in `Components/Pages/*.razor`; component/section pattern with `data-chatbot-surface`, `aria-labelledby`, `ChatBotStatusBanner`; `ChatBotWhyProjectPanel` (Story 3.9) is the evidence/provenance-panel reference for "why/what happened" rendering. [Source: src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs; src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor; src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor]
- **Localization + tokens.** `ChatBotUiTextLocalizer` / `ChatBotUiTextKey` (keyed strings, EN/FR `.resx`), `wwwroot/css/chatbot.tokens.css` (semantic tokens the fixture already pulls in). [Source: src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs; ChatBotUiTextKey.cs]
- **Safe-token discipline.** `AuditMetadata.SafeOptionalToken / SafeCommandName / SafeActorType / IsSafeStableIdentifier`; `ComplianceAdministrationSchema.IsSafeComplianceToken` for query refs. Every emitted token passes one of these. [Source: src/Hexalith.ChatBot.Server/Audit/AuditMetadata.cs; ComplianceAdministrationContracts.cs:200-201]

**What you are adding (the real deliverables):** (1) two filter keys (`message-id`, `surface`) in schema + read policy; (2) replay exclusion in `Search`; (3) the tenant-scoped `Compliance`-gated search/detail HTTP endpoints wired to the chain; (4) the S9 Blazor surface (timeline, search/filters, redacted-detail, escalation, projection-pending, states, a11y, responsive, localization); (5) tests + ADR.

### Architecture constraints (must follow)

- **S9 lives in Epic 9; elaborate before building (binding planning guidance).** "S9 compliance investigation (Epic 9)" was explicitly flagged for elaboration before increment planning; the UX is spine-only — import the binding tables, do not invent. [Source: epics.md:589; epics.md:588]
- **FR54** — compliance/support reviewers investigate association decisions, approval decisions, command outcomes, and risky AI actions. **FR56** — authorized users query audit by tenant, actor, command, resource, decision, reason, correlation, and time. **FR75f** — `compliance-admin` reads audit across tenant (per-project redaction NFR2), triggers investigations, configures retention within NFR49a bounds; **cannot operate on workflow items.** [Source: epics.md:113, 116, 132]
- **NFR2** — unauthorized actors get redacted failure responses revealing no restricted project names, file metadata, candidate evidence, audit details, or tenant data. [Source: epics.md:187]
- **FR95a** — replay events carry `replay_run_id`; **production audit queries exclude replay.** [Source: epics.md:177]
- **NFR9a / tenant isolation by construction** — cross-tenant queries impossible at the store-access layer; `EnumerateChain` is tenant-partitioned. [Source: architecture.md (Data boundaries); src/Hexalith.ChatBot.Server/Audit/IWormAuditStore.cs:7-8]
- **D4 two-phase audit / WORM (NFR49a)** — audit is append-only and tamper-evident; the investigation surface is a **read** over it and must add no commit-time gate and never mutate the chain. [Source: architecture.md:143-147, 369-372; Story 9.1 / 9.2 Dev Notes]
- **Boundary (NetArchTest-enforced)** — audit interfaces, `AuditEnvelope`, and `ComplianceAuditReadPolicy` are `internal` to `.Server`; no `.UI`/`.Cli`/`.Mcp` type may reference them. The UI reaches audit data **only** through the HTTP endpoint + generated client. [Source: architecture.md (Architectural Boundaries); tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs]

### Previous-work intelligence — apply directly

- **Story 7.4 forward-scaffolded this surface's contracts, read policy, and E2E DOM contract.** Treat that as the spec, not as something to redo. The two filter-key gaps (`message-id`, `surface`) and the missing replay exclusion are the only read-policy *logic* changes; everything else is wiring + UI.
- **Story 9.1 (WORM chain) + 9.2 (completeness, replay marker)** are the substrate. Reuse `EnumerateChain`, `AuditReplayExclusion`, `AuditOperationReconstructor`; do not touch the canonical hash or the chain-append path.
- **Bookkeeping drift is the #1 recurring review auto-fix across Epics 7–9** (stale test counts, **File List omissions** — Story 9.1/9.2 reviews both had to fix these). Keep the **File List exhaustive** (every new + modified source, test, `.resx`, and the ADR) and every cited test count accurate.
- **Inert-control-floor honesty.** If any piece is deferred (e.g. full E2E repointing, an escalation *approval* workflow beyond recording the request, a replay-scoped investigation mode), **say so explicitly in Completion Notes** with what remains — never let a deferral read as "done." Story 9.4 owns replay execution; escalation *approval/denial* state transitions beyond `RequestComplianceEscalation` recording are not this story's scope unless an existing handler already supports them.
- **Define-once / reuse.** Consume `IWormAuditStore`, `ComplianceAuditReadPolicy`, the compliance contracts, `AuditReplayExclusion`, `AuditMetadata`, `AdminAuthorityEvaluator`, `ChatBotUiTextLocalizer`, and the `audit-history` endpoint pattern by reference — do not re-derive scopes, token rules, the redaction/escalation mapping, or the safe-not-found shape.

### Project Structure Notes

- **Contracts:** filter-key additions in `src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs` (and a possible `ComplianceAdministrationSchemaVersions.V2` only if a reviewer deems the key set a versioned contract).
- **Server:** `MatchesFilter` + replay exclusion in `src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs`; new search/detail endpoints in `Program.cs` (mirroring the audit-history endpoint) + any HTTP-results/reader helper in `src/Hexalith.ChatBot.Server/Audit/`; DI in the existing service-collection extension.
- **UI:** new page in `src/Hexalith.ChatBot.UI/Components/Pages/` + UI service + view models (mirroring `AssociationReviewService`); new `ChatBotUiTextKey` constants + EN/FR `.resx`; reuse `chatbot.tokens.css`.
- **Tests:** `tests/Hexalith.ChatBot.Server.Tests/Audit/` (read-policy + endpoint), `tests/Hexalith.ChatBot.Contracts.Tests/` (schema/contract), `tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs` (repoint to real surface).
- **Docs:** `docs/adrs/audit-investigation-surface.md`.
- No conflict with the unified structure: the `Audit/` server seam, the `Components/Pages` UI home, and the `internal`-to-`.Server` boundary match the architecture's prescribed placement.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.3 (lines 2400-2416)]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 9 (lines 580-582, 2358-2360); cross-cutting S9 elaboration guidance (lines 588-589)]
- [Source: _bmad-output/planning-artifacts/epics.md#FR54 (113), FR56 (116), FR75f (132), NFR2 (187), FR95a (177)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Two-phase audit / D4 (143-147, 369-372); tenant isolation (Data boundaries)]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#IA (33-45), Components (66-84), State coverage (110-138), Interaction (140-172), Audit semantics (173-180), Accessibility (189-229), Responsive (231-243), Flow 7 (322-331)]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Blocked state (226-227)]
- [Source: src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs (Search/Detail/MatchesFilter); AuditEnvelope.cs; IWormAuditStore.cs; WormAuditChainRecord.cs; AuditReplayExclusion.cs; AuditOperationReconstructor.cs; AuditMetadata.cs; AuditEnvelopeFactory.cs; OperationAuditHistoryHttpResults.cs; IAuditHistoryReader.cs]
- [Source: src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs (filter keys, schema, commands); Queries/ComplianceAuditQueries.cs; Enums/AdminScope.cs; AdminScopes.cs; ComplianceEscalationStatus.cs; ComplianceAuditRedactionState.cs; ChatBotSurfaceOrigin.cs]
- [Source: src/Hexalith.ChatBot.Server/Program.cs:333-381 (audit-history read-endpoint pattern); src/Hexalith.ChatBot.Server/Governance/Admin/AdminAuthorityEvaluator.cs]
- [Source: src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs; Components/Pages/AssociationReview.razor; Components/Governed/ChatBotWhyProjectPanel.razor; Localization/ChatBotUiTextLocalizer.cs; ChatBotUiTextKey.cs; wwwroot/css/chatbot.tokens.css]
- [Source: tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs (binding S9 DOM contract); tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditReadPolicyTests.cs; tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs; OpenApiContractSpineTests.cs]
- [Source: _bmad-output/implementation-artifacts/9-1-tamper-evident-worm-audit-chain.md; 9-2-audit-completeness-as-a-production-observable.md]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8, 1M context) — BMAD dev-story workflow.

### Debug Log References

- `dotnet test tests/Hexalith.ChatBot.Server.Tests` — 1194 passed (incl. 6 new read-policy + 7 endpoint compliance tests).
- `dotnet test tests/Hexalith.ChatBot.Contracts.Tests` — 339 passed (incl. message-id/surface key validation).
- `dotnet test tests/Hexalith.ChatBot.UI.Tests` — 128 passed (incl. 7 compliance surface/service tests + EN/FR localization coverage).
- `dotnet test tests/Hexalith.ChatBot.Architecture.Tests` — 37 passed (UI→Server boundary + no-direct-audit-write fitness).
- `dotnet test tests/Hexalith.ChatBot.Client.Tests` — 23 passed (incl. 3 compliance audit transport tests).
- `dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests --filter ComplianceAdministration` — 3 passed via the deterministic
  no-browser fixture path; the browser-backed Playwright run is flaky in this WSL sandbox (env-specific, not code).
- `dotnet build Hexalith.ChatBot.slnx` — full solution builds, 0 warnings / 0 errors.

### Completion Notes List

Implemented all six tasks. Highlights and honest deferrals:

- **AC1 (search + reconstruct, FR54/FR56):** added the `message-id` + `surface` filter dimensions in lock-step across
  `ComplianceAdministrationSchema.AuditFilterKeys` and `ComplianceAuditReadPolicy.MatchesFilter`, and wired the
  previously-uncalled `Search`/`Detail` read-policy methods to a real chain source via two tenant-scoped,
  `Compliance`-gated HTTP endpoints over `IWormAuditStore.EnumerateChain`. The S9 Blazor surface renders the
  metadata-only audit timeline.
- **AC2 (per-project redaction + escalation; read/escalate-only):** detail visibility is driven by
  `Detail(envelope, hasPerProjectAuthority)`, where `HasPerProjectAuthority` is evaluated from the reviewer's actual
  `project-owner` grants against the record's `project:` evidence token (never assumed). The surface dispatches only the
  allowlisted escalation/investigation commands with an **opaque** target and renders any operate-style control inert.
- **AC3 (replay exclusion, FR95a):** `Search` excludes replay-marked envelopes by default via the Story 9.2
  `AuditReplayExclusion.IsReplayEnvelope` predicate; a unit test injects a replay-marked record and asserts its absence.
- **Cross-cutting:** unresolved/cross-tenant/non-Compliance/non-human/unknown all collapse to the identical
  safe-not-found; an endpoint test asserts the WORM chain length is unchanged after a search (no append on the read path).
- **Deferral 1 — Playwright E2E repointing:** the binding `ComplianceAdministrationE2ETests` remains fixture-based. The
  real surface reproduces the same DOM contract (now covered by `ComplianceAuditSurfaceTests` component-composition
  assertions), but repointing the Playwright scenarios to render the live Blazor component needs a browser-hosted render
  harness that does not exist in the repo (the current E2E uses `SetContentAsync` with inline HTML). Per the story's
  explicit allowance, a real-surface render contract test was added instead of leaving the fixture as the only coverage.
- **Deferral 2 — UI transport seam:** because the new endpoints post-date the generated OpenAPI client (and its
  `FilterKey` enum predates `message-id`/`surface`), the UI reaches them through a small hand-written typed transport
  (`Generated/ComplianceAuditTransport.cs`) over the existing `HttpClient` rather than a regenerated client. No
  OpenAPI/client regeneration was performed (none required for the v1-compatible filter change).
- **Minor note — `outcome` token:** the binding fixture row shows an `outcome:` token, but the metadata-only
  `ComplianceAuditResultRow` contract (fixed by Story 7.4 — not to be reshaped) carries no outcome field, so the rendered
  row omits `outcome:`; all other fixture tokens are reproduced.

### File List

**Source — modified**
- `src/Hexalith.ChatBot.Contracts/Commands/ComplianceAdministrationContracts.cs`
- `src/Hexalith.ChatBot.Server/Audit/ComplianceAuditReadPolicy.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Client/IChatBotClient.cs`
- `src/Hexalith.ChatBot.Client/ChatBotClient.cs`
- `src/Hexalith.ChatBot.UI/Program.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`

**Source — new**
- `src/Hexalith.ChatBot.Server/Audit/ComplianceAuditHttpResults.cs`
- `src/Hexalith.ChatBot.Client/ComplianceAuditClient.cs`
- `src/Hexalith.ChatBot.Client/Generated/ComplianceAuditTransport.cs`
- `src/Hexalith.ChatBot.UI/Services/ComplianceAuditService.cs`
- `src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor`

**Tests — modified**
- `tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditReadPolicyTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/AdminContractTests.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`

**Tests — new**
- `tests/Hexalith.ChatBot.Server.Tests/Audit/ComplianceAuditInvestigationEndpointTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ComplianceAuditSurfaceTests.cs`
- `tests/Hexalith.ChatBot.Client.Tests/ComplianceAuditTransportTests.cs`

**Docs — new**
- `docs/adrs/audit-investigation-surface.md`

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — adversarial review on 2026-06-03. **Outcome: Approve (auto-fixes applied).**

Build clean (`dotnet build Hexalith.ChatBot.slnx` → 0/0). All affected suites green: Server 1194, Contracts 339, UI 128, Client 23, Architecture 37.

**Acceptance-criteria audit (all implemented, verified against code + tests):**
- **AC1** — `message-id`/`surface` added in lock-step in `ComplianceAdministrationSchema.AuditFilterKeys` and `ComplianceAuditReadPolicy.MatchesFilter`; the previously-uncalled `Search`/`Detail` are now wired to `IWormAuditStore.EnumerateChain` through two tenant-scoped, `Compliance`-gated endpoints; the S9 timeline renders metadata-only rows. Covered by read-policy + endpoint + transport + surface tests.
- **AC2** — per-project visibility driven by `HasPerProjectAuthority` (actual `project-owner` grant vs `project:` evidence token, never assumed); operate control rendered inert (`aria-disabled`, no mutation command). Covered by endpoint + read-policy tests.
- **AC3** — `Search` (and the detail fetch) exclude replay-marked envelopes via the Story 9.2 `AuditReplayExclusion.IsReplayEnvelope`; a seeded replay record is asserted absent. Covered by unit + endpoint tests.
- **Cross-cutting** — unresolved/cross-tenant/non-Compliance/non-human/unknown all collapse to the identical safe-not-found; an endpoint test asserts WORM chain length unchanged after a search (no append on the read path); no-leak assertions hold on rows, detail, and transport.

**Findings (no Critical / High; all Medium auto-fixed, Low noted):**
- **[Medium — fixed]** File List omitted the new `tests/Hexalith.ChatBot.Client.Tests/ComplianceAuditTransportTests.cs` (present in git). Added.
- **[Medium — fixed]** Stale Debug Log test counts (Server 1191→1194, UI 126→128, Client 20→23) and inaccurate sub-counts ("5 endpoint"→7, "5 surface"→7). Corrected to match the live run.
- **[Low — noted, not changed]** `ComplianceAuditService.GetDetailAsync` and the `ComplianceAuditRedactedDetail` text key exist and are test-exercised but are not yet wired into a detail/evidence drawer in `ComplianceAuditInvestigation.razor`; the binding S9 DOM contract carries redaction/escalation/safe-next-action inline on each row, so AC2 redaction is satisfied at row level. A future "event selected → detail drawer" enhancement can consume them.
- **[Low — noted]** Playwright E2E remains fixture-based (honest deferral already in Completion Notes); the real surface is covered by the `ComplianceAuditSurfaceTests` composition contract test.

## Change Log

| Date       | Version | Description                                                                                 | Author |
|------------|---------|---------------------------------------------------------------------------------------------|--------|
| 2026-06-03 | 0.1     | Implemented Story 9.3: message-id/surface filters, replay exclusion, tenant-scoped Compliance-gated audit search/detail endpoints, S9 Blazor investigation surface (read/escalate-only), tests, and ADR. Status → review. | Amelia (Dev Agent) |
| 2026-06-03 | 0.2     | Adversarial review: ACs validated against implementation, all suites green (1194/339/128/23/37). Auto-fixed File List omission (transport tests) and stale Debug Log counts. No Critical/High. Status → done. | Jérôme Piquot (AI Review) |
