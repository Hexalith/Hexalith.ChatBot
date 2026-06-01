---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md"
  - "_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md"
  - "Hexalith.EventStore/_bmad-output/project-context.md"
  - "Hexalith.Conversations/_bmad-output/project-context.md"
  - "Hexalith.Projects/_bmad-output/project-context.md"
  - "Hexalith.Folders/_bmad-output/project-context.md"
  - "Hexalith.Parties/_bmad-output/project-context.md"
  - "Hexalith.Tenants/_bmad-output/project-context.md"
  - "Hexalith.FrontComposer/_bmad-output/project-context.md"
  - "Hexalith.Memories/_bmad-output/project-context.md"
  - "Hexalith.Commons/_bmad-output/project-context.md"
workflowType: 'architecture'
project_name: 'Hexalith.ChatBot'
user_name: 'Jerome'
date: '2026-05-28'
lastStep: 8
status: 'complete'
completedAt: '2026-05-28'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements (96 FRs):** ChatBot orchestrates a governed email-to-project
collaboration loop over existing Hexalith bounded contexts. By capability area, with
architectural implications:

- **Email intake & association (FR1–FR12):** mailbox-event capture, source-identity
  preservation, deterministic association, ambiguity → human review, candidate evidence,
  correction. Implies an **Association** context owning the lifecycle state machine + a
  deterministic candidate-scoring kernel (deterministic signals outrank AI inference).
- **Participants, identity, authorization (FR13–FR20):** party resolution via Hexalith.Parties
  (behind an adapter), authorization at command/query boundary, external-party email
  participation without portal auth.
- **Project conversation & context (FR21–FR28):** email-derived conversation rendering through a
  ChatBot-owned project-conversation projection and S1 UI surface, with evidence/provenance,
  informational-vs-actionable classification, and AI-summary-vs-source-evidence distinction.
- **Files & attachments (FR29–FR34):** capture into Hexalith.Folders, scan/quarantine, scoped
  AI-context packaging under explicit authorization.
- **Task intent & AI mediation (FR35–FR46):** task-intent kernel, tag+heuristic risk
  classifier (no AI dependency), approval gates for six risky action classes, allowlisted-command
  execution only.
- **Outbound communication (FR47–FR50, FR48a–d):** governed draft-and-send, five sender-authority
  classes, inbound authenticity (DMARC/DKIM/SPF passthrough).
- **Admin, governance, audit (FR51–FR63, FR75a–g):** tenant policy schema, bounded tenant-admin
  scopes (no superuser), audit production, redaction, derived-store cross-tenant isolation.
- **Reliability & operations (FR64–FR80):** duplicate detection, retry, fail-closed, operational
  queues, notification routing, long-running status.
- **Cross-surface parity & state model (FR81–FR96):** the FR81a shared command pipeline, CLI/MCP
  adapters, canonical lifecycle state machine, idempotency keys, correction propagation (FR91a),
  replay isolation (FR95a).

**Non-Functional Requirements (70 NFRs) shaping architecture:**

- **Security/privacy (NFR1–NFR12, NFR9a):** authorization at every boundary; redacted failure
  responses; encryption in transit/at rest; least-privilege M365 & service-client scopes; bounded
  auth-cache staleness (5 min normal / 60 s revocation); **derived-store tenant isolation by
  construction at the store layer**, not application filtering.
- **Reliability/integrity (NFR13–NFR22, NFR13a/15a/17a):** per-operation-class idempotency contract
  (8 classes); fail-closed invariant across 10 enumerated code paths; at-least-once worker delivery;
  AI-outage tolerance for non-AI workflows; correction-propagation SLO (p95 ≤ 10 min M0/M1, ≤ 60 min M2).
- **Performance/scalability (NFR23–NFR30):** p95 2 s UI reads; 10 s candidate generation; CLI/MCP
  long-running → operation-id within 5 s, no 30 s hold; per-tenant rate limits/quotas/circuit breakers;
  cross-tenant noisy-neighbor isolation.
- **Integration (NFR31–NFR36):** M365/Graph tolerance for throttle/revoke/replay; contract-verifiable
  responses with stable identifiers/codes; versioned contracts; correlation context everywhere;
  server-side UTC time.
- **Operability (NFR37–NFR48, NFR42a):** health/queue observability; published SLOs; message-catalog-driven
  user-safe states; approval-fatigue mechanisms (prioritization, grouping, rate ceiling, rubber-stamp
  observable); evidence-freshness chips.
- **Audit/compliance (NFR49–NFR55, NFR49a/50a):** tamper-evident append-only WORM hash-chained audit;
  ≥99.5% audit-completeness production observable (reconstructability, not just field presence); GDPR
  retention classes; consent/lawful-basis metadata.
- **Recovery (NFR56–NFR59):** RPO ≤ 15 min / RTO ≤ 4 hr (assumption pending M2 drill); projection rebuild
  from source ≤ 4 hr; scoped outage degradation.
- **Accessibility (NFR60–NFR64):** WCAG 2.2 AA scoped per-increment to enumerated surfaces; non-color
  status; keyboard/screen-reader for core flows; English + French.
- **Quality gates (NFR65–NFR70):** negative authorization tests across 9 actor types; isolated
  replay/simulation; every operation defines transition/audit/redaction/idempotency.

### Scale & Complexity

- **Primary domain:** distributed backend/service orchestration (.NET 10 + DAPR + Hexalith.EventStore)
  with a Blazor/FrontComposer web UI and CLI + MCP machine surfaces.
- **Complexity level:** High / enterprise (multi-tenant zero-tolerance isolation, GDPR, cross-surface
  parity, governed AI, M365 integration, event-sourced tamper-evident audit).
- **Estimated architectural components (provisional):** ChatBot service (governed command gateway +
  domain processors), Association context, Task-Intent/AI-Mediation context, Governance/Approval context,
  Lifecycle/Workflow context, Projection/Query layer (+ SignalR nudge), Audit/Replay layer, mailbox-ingestion
  adapter (M365/Graph), AI-provider adapter, CLI adapter, MCP server adapter, Blazor UI, background workers,
  + integration adapters to Projects/Parties/Folders/Tenants/Conversations/Memories/EventStore.

### Technical Constraints & Dependencies

- **Fixed platform stack:** .NET 10 (SDK 10.0.300, net10.0, nullable + warnings-as-errors, central package
  management); DAPR (actors, at-least-once pub/sub, workflow, service invocation, deny-by-default ACLs);
  .NET Aspire orchestration; Hexalith.EventStore as the write-side foundation (CQRS/ES,
  `{tenant}:{domain}:{aggregateId}`, persist-then-publish, pure `Handle`/`Apply`, rejections-as-events,
  ULIDs not GUIDs, `system` platform tenant, EventStore owns the envelope; **each service already runs its
  own EventStore command pipeline + AggregateActor 5-step sequence**); Keycloak OIDC; Blazor + Fluent UI v5
  (RC-pinned) via Hexalith.FrontComposer (Roslyn source generators, Fluxor, REST commands/queries +
  SignalR projection-nudge, MCP descriptors).
- **Bounded-context dependencies (consume by stable ID, never duplicate authority):** Hexalith.Projects,
  Parties, Folders, Tenants, Conversations, EventStore, Memories (Redis Vector / FalkorDB for AI
  context/vector indexes), Commons.
- **Module conventions inherited:** Contracts→Server dependency direction; CLI/MCP wrap the typed Client and
  never bypass the command pipeline or touch DAPR directly; tenant isolation physical (not just filtered) for
  indexes/caches/graphs; metadata-only logging (no payloads/PII/secrets); wrap sibling clients behind adapters
  (e.g., `IParticipantDirectory` over Parties); local event-fed tenant-access projection that fails closed;
  contract-first FrontComposer annotations; additive, serialization-tolerant schema evolution (no V2 event types).
- **Submodule policy:** root-level submodules only; never recursive init.
- **External constraints:** M365/Exchange Graph permission model (least-privilege, delegated/shared/send-on-behalf);
  GDPR/EU data protection.

### Cross-Cutting Concerns Identified

1. **Tenant isolation** — zero-tolerance; enforced at command, query, store, cache, vector index, projection,
   log, and error-body layers; `tenantId` from Keycloak claims only, never request body.
2. **Authorization at command/query boundary** — two-layer (API gate + domain), inside the gateway; redacted
   denials that don't confirm resource existence. *Rule to lock: mirrors for display, live authorization for gates.*
3. **Governed command admission gateway (FR81a)** — re-scoped from "a pipeline" to a **component + enforcement
   discipline**: a `CommandGateway` admission layer (auth → tenant-bind → authorize → risk-classify →
   approval-gate → coarse idempotency → pre-commit audit) that sits *in front of* EventStore's existing
   per-context write pipeline (which owns fine idempotency → execute → publish → projection). Adapters
   (UI/CLI/MCP) may construct only a typed `IChatBotCommand` and hand it to the gateway; governance interfaces
   stay `internal` so stage-replication is a compile error, backed by an architecture test (NetArchTest).
   Parity is enforced by construction + a differential-conformance harness, not by aspiration.
4. **Fail-closed invariant (NFR15a)** — enforced at one injectable audit-commit seam every state-writing path
   calls before persisting; **only pre-commit paths fail closed** (see #6).
5. **Idempotency (NFR13a)** — **two altitudes**: coarse request-dedup at the gateway, fine event-dedup at the
   aggregate (the existing idempotency cache). Never conflate them. At-least-once DAPR delivery tolerance.
6. **Auditability & tamper-evidence (NFR49a/50a)** — **two-phase, resolving the NFR15a × NFR49a tension**:
   *pre-commit* audit (intent/risk/approval) is a fail-closed gateway gate; *post-commit* WORM hash-chain audit
   is fail-open-then-reconcile (the event log is the source of truth, the chain is rebuilt from it on recovery —
   you cannot block-the-commit AND derive-the-chain on the same write). Completeness = reconstructability,
   verified by a scheduled production assertion that rebuilds state from the log and diffs the projection.
7. **Redaction & data governance** — retention classes, redaction-aware audit, consistent redaction across
   UI/CLI/MCP/export; isolate redaction as a swappable policy stage (trim-safe to a coarse default).
8. **Observability & SLOs** — OpenTelemetry; per-class latency/queue/lag metrics; published SLOs. *Emit structured
   signal always; visualize later (dashboards are trim-able, emission is not).*
9. **Governed AI mediation** — scoped context packaging, risk classification, approval gates, allowlisted commands,
   refusal behavior, AI-outage resilience for non-AI workflows.
10. **Correlation & lifecycle-state consistency** — canonical state machine shared across surfaces; correlation
    propagated through every surface, worker, and projection.
11. **Derived-state versioning & deterministic replay** *(added — unanimous Party Mode finding)* — ChatBot owns
    derived state rebuilt by replaying events; a projection schema change (AI-proposal shapes will churn) must map
    old events → new schema (event upcasting), or replay produces state divergent from live, making evidence
    snapshots/approval records non-reproducible and undermining NFR49a. Includes: projection schema version stamped
    in replay traces, *as-of* upstream resolution (don't re-query *current* Party/Folder data during rebuild), and
    cross-context consumer-driven contract testing (Pact-style) against the 7 sibling contexts.
12. **Evidence & confidence capture** *(added — product-thesis finding)* — the product exists for *reliable
    association*; every AI proposal must structurally carry its confidence, evidence basis, and human-correction
    outcome as a first-class invariant, because that data IS the pilot's success measurement (A11 evidence-resolution)
    and the model-improvement loop. A fully-governed, fully-audited system that proposes the wrong project passes
    every other concern green while the product fails.
13. **WORM-vs-erasure tension (GDPR)** *(added)* — tamper-evidence says "never mutate the log"; GDPR right-to-erasure
    says "erase this person's data." The resolution (crypto-shredding / redaction-by-key-destruction / projection
    tombstones over an immutable chain) is an architecture decision, not a policy footnote.

**Watch-list (monitor; may fold into the above):** reversibility/undo as the approval-fatigue antidote (vs more
friction); AI cost/resource governance (B2B unit economics); explicit ordering-source (source version, not wall clock).

### Architectural Findings to Carry into Decisions (from Party Mode)

- **Modular monolith with hard, event-mediated seams**, not premature service sharding — the real risk is a
  *distended orchestrator* (shadow source of truth), not a distributed monolith. Candidate seams by derived-state
  lifecycle: Association / Governance-Mediation / Lifecycle-Workflow / Projection-Query / Audit-Replay. Seam test:
  *owns an aggregate with its own invariants, or just a folder?*
- **FR81a = `CommandGateway` admission layer over EventStore's existing pipeline** (not a second pipeline), enforced
  by Client-only adapter surface + NetArchTest + differential-conformance harness over surface-agnostic semantic
  intents (event-sequence + state-store end-state equivalence; include rejection and retry intents).
- **Two-phase audit** (pre-commit fail-closed gate vs post-commit reconcile-from-event-log) — resolve before M0 closes.
- **Derived-store split:** decision snapshots immutable (FR91a = *supersede + re-evaluate-forward*; open proposals
  re-evaluate, closed/approved proposals are immutable history); live mirrors fresh (event-driven, version-stamped,
  order-tolerant projections).
- **Correction propagation (FR91a):** implemented in Epic 2 as a DAPR-ready coordinator/activity seam with
  deterministic workflow identifiers and durable lifecycle events; the aggregate owns the `correcting`/`current`
  lifecycle. Hosted Dapr Workflow runtime binding remains a follow-up before production saga orchestration claims.
  `ReindexVectors(tenantId, correctionId, sourceVersion)` stays an M2 activity and must be idempotent +
  version-guarded.
- **M0 is a walking skeleton:** minimal *surface* (one tenant, one mailbox, one allowlisted command, UI-only) but a
  *complete spine* — all gateway stage seams present and typed; tenant partitioning, fail-closed, and
  audit/idempotency **real** from day one (retrofitting them touches every path). Epic 4 replaces the original
  risk/approval stubs with the registered `DeterministicAiActionRiskClassifier` and `AiActionApprovalGate`
  stages for governed AI mediation.
- **A9a gate semantics by milestone:** *directional* at M0 (n≈100 positives gives ±~6pt CI — can't distinguish 88%
  from 92%), *binding & CI-aware* at M1 (require lower confidence bound to clear). Budget inter-annotator-agreement /
  label-quality work + a frozen held-out partition + dataset versioning.
- **Safety floor vs trimmable richness:** the architect's deliverable is a dependency map proving no safety-floor
  invariant (isolation, authorization, fail-closed gate, audit-of-the-command, the spine) rides inside a trimmable
  stage (redaction depth, approval-policy richness, dashboards).

### Open Architecture Questions (resolve in the Decisions step)

1. **ChatBot → sibling-context contract: event-driven (publish an intent, the sibling decides) or invocation-driven
   (call the sibling's command)?** This single choice determines whether ChatBot is an orchestrator or a puppeteer
   and cascades into nearly every later decision.
2. **M0's purpose: prove the *governed loop* or the *association heuristic*? Concretely — does M0 include AI-*proposed*
   association with a human confirm/correct gesture, or human-only filing?** If human-only, the "reliable association"
   thesis is untested and A11's 70% evidence-resolution target is unmeasurable.

## Starter Template Evaluation

### Primary Technology Domain

Distributed **.NET service-oriented application** on the Hexalith platform: DAPR-based event-sourced
backend (Hexalith.EventStore) + Blazor/Fluent UI web surface (Hexalith.FrontComposer) + CLI and MCP
machine surfaces, composed and run via .NET Aspire. This is a **brownfield product on a fixed,
opinionated platform**, not a greenfield free choice of stack.

### Starter Options Considered

1. **External .NET / web starter templates** (Clean Architecture template, ABP, generic Blazor
   templates) — **Rejected.** They reintroduce a parallel persistence/messaging/UI stack, contradicting
   the mandate to consume Hexalith bounded contexts by ID and route all writes through EventStore.
   They make architectural decisions Hexalith has already made differently and authoritatively.
2. **Hexalith sibling-module scaffold (the established module template)** — **Selected.** Every sibling
   module follows one canonical shape; ChatBot must be a new module of the same shape so it inherits
   tenant isolation, the command/event pipeline, DI conventions, testing tiers, and FrontComposer UI
   generation by construction.

### Selected Starter: New Hexalith module `Hexalith.ChatBot`, scaffolded from the canonical sibling-module template

- **Foundation:** `Hexalith.EventStore` as a **root-level git submodule** (never recursive). Provides the
  command/aggregate/projection/query/SignalR/CLI/MCP primitives ChatBot builds on.
- **Closest structural reference:** `Hexalith.Folders` — most complete recent multi-surface sibling
  (REST + CLI + MCP + read-only Blazor UI + background workers + an **OpenAPI Contract Spine** with
  generated client + idempotency helpers + parity-oracle tests). Maps almost 1:1 onto ChatBot's
  cross-surface parity requirement (FR81a).
- **Closest domain reference:** `Hexalith.Conversations` — reference implementation for conversation
  adapter patterns ChatBot may adopt later (`IParticipantDirectory` over Parties, local event-fed
  tenant-access projection, store-stable-IDs-not-PII). The current M0 S1 implementation is a
  ChatBot-owned read projection and UI state model, not a `Hexalith.Conversations` adapter.
- **Recommended pattern to adopt from Folders:** a **Contract Spine** (OpenAPI 3.1 + generated client +
  parity oracle) as the single contract source UI/CLI/MCP adapters bind to — directly reinforces the
  FR81a "parity by construction" + differential-conformance findings.

**Initialization (first implementation story — no single CLI generator exists; scaffold by convention):**

- Create module solution + project layout matching the sibling-module shape (`.slnx`, not `.sln`):
  `Contracts` (commands/events/rejections/queries/enums/identities — low-dep), `Client` (typed client;
  exposes `IChatBotCommand` submission; CLI/MCP/UI bind here), `Server` (aggregates, projections,
  validators, CommandGateway, governance internals), `Aspire`, `AppHost` (DAPR topology), `ServiceDefaults`
  (OpenTelemetry/host config), `Testing`; surface adapters added per increment: `.UI` (M0), `.Cli` + `.Mcp`
  (M1), `.Workers`; `tests/` mirroring each project (xUnit v3).
- Add EventStore as a **root-level submodule** (`git submodule update --init`, not `--recursive`).
- Root config: `global.json` (SDK 10.0.300), `Directory.Build.props` (nullable, warnings-as-errors),
  `Directory.Packages.props` (central package management), `.editorconfig`, `nuget.config`.
- Wire Aspire AppHost + DAPR components: canonical EventStore actor/status store `statestore`, ChatBot derived
  state store `chatbot-statestore`, Redis pub/sub `chatbot-pubsub`, production deny-by-default
  `accesscontrol.yaml`, and local mTLS-off `accesscontrol.local.yaml`; verify `aspire run` brings up the topology.

**Architectural Decisions Provided by the Platform "Starter" (versions web-verified May 2026):**

| Concern | Decision | Verified status |
|---|---|---|
| Language & runtime | C# 14 / `net10.0`, SDK `10.0.300` (LTS), nullable, warnings-as-errors, central package mgmt | GA, released 2026-05-12; matches all siblings |
| Persistence / write model | Hexalith.EventStore (CQRS/ES, `{tenant}:{domain}:{aggregateId}`, persist-then-publish, pure Handle/Apply, rejections-as-events, ULIDs, `system` platform tenant) | Foundation submodule |
| Messaging / orchestration | DAPR 1.17.x — at-least-once pub/sub (CloudEvents), actors via `IActorStateManager`, deny-by-default ACLs; Epic 2 implements a DAPR-ready correction-propagation coordinator seam, with hosted Dapr Workflow binding still pending | Matches sibling pins |
| Hosting / composition | .NET **Aspire 13.3.x** AppHost (K8s/AKS + Helm deploy in 13.3 — relevant to M2 ops) | Latest 13.3 (2026-05-07); EventStore/Tenants/Folders on 13.3.x |
| UI | Blazor + **Fluent UI v5 (RC, via FrontComposer)** — Roslyn source-gen, Fluxor, REST + SignalR projection-nudge, contract-first | ⚠️ Still RC May 2026 — inherited pre-GA dependency, pinned, do not upgrade casually |
| CLI surface (M1) | System.CommandLine 2.0.x wrapping `Hexalith.ChatBot.Client` | Per Folders pin; verify at scaffold |
| MCP surface (M1) | **ModelContextProtocol 1.3.x** (`.Core` + `.AspNetCore`); tools translate to commands/queries, tenant-aware | GA (v1.x), latest 1.3.0 (2026-05-08) — de-risks M1 |
| AI context / vector store | Hexalith.Memories (Redis Vector / FalkorDB) for scoped AI context + vector indexes (M2, NFR9a isolation) | Existing module |
| Testing | xUnit **v3** 3.2.x, Shouldly, NSubstitute, Testcontainers; three-tier (unit / DAPR integration / Aspire E2E); conformance + isolation + idempotency as release gates | Greenfield module → v3 |
| Code organization | Fixed module boundaries; strict Contracts→Server direction; CLI/MCP/UI depend only on Client; governance interfaces `internal` in Server (mechanical FR81a parity guarantee, NetArchTest-verifiable) | Platform convention |
| Solution / release | `.slnx` format; Conventional Commits + semantic-release | Platform convention |

**Note:** Module scaffolding should be the **first implementation story**. Adopting the Folders-style
Contract Spine should be decided early — it underpins cross-surface parity (FR81a).

## Core Architectural Decisions

### Decision Priority Analysis

**Critical decisions (block implementation) — now made:**
- **D1 — Sibling integration & orchestration:** event-driven, with Dapr Workflow saga binding planned before production cross-context orchestration claims (resolves open question #1).
- **D2 — M0 association-proposal model:** deterministic candidate generation + evidence + human confirm/correct (resolves open question #2; confirms PRD M0 scope).
- **D3 — FR81a placement:** a `CommandGateway` admission layer in front of EventStore's existing per-context pipeline (not a second pipeline).
- **D4 — Audit model:** two-phase — pre-commit fail-closed gate + post-commit WORM reconciled-from-event-log.
- **D5 — Internal decomposition:** modular monolith with hard, event-mediated seams.
- **D6 — Derived-store modeling:** immutable decision snapshots (supersede-not-mutate) vs. fresh live mirrors (event-driven projections).
- **D7 — Contract surface:** OpenAPI 3.1 Contract Spine, contract-first.

**Important decisions (shape architecture):** correction-propagation orchestration (coordinator/activity seam now, hosted Dapr Workflow binding pending; aggregate owns lifecycle); association scorer placement (Association module, deterministic-only in M0); WORM audit backing; M365/Graph adapter boundary; A9a gate semantics by milestone.

**Deferred (post-M0, mostly M2):** vector/embedding cross-tenant store isolation (NFR9a); replay/simulation test-tenant isolation (FR95a); operational dashboards; learned/AI candidate ranking (M1); outbound send + inbound authenticity (M1); CLI/MCP adapters (M1).

### Data Architecture

- **Write model (platform):** Hexalith.EventStore CQRS/ES — persist-then-publish, pure `Handle`/`Apply`,
  rejections-as-events, ULIDs, `{tenant}:{domain}:{aggregateId}`. ChatBot is a new EventStore domain
  service; its aggregates/projections auto-discovered from `Hexalith.ChatBot.Server`.
- **ChatBot owns derived state, split by mutability (D6):**
  - **Immutable decision snapshots** — candidate rankings, evidence snapshots, AI-action proposals,
    approval records, policy snapshots. Append-only, **superseded not mutated**. FR91a correction =
    *supersede + re-evaluate-forward*: open proposals re-evaluate against the corrected association;
    closed/approved proposals remain immutable history. Immutability here is the audit defense.
  - **Live mirrors** — membership, ACL/authorization state, sibling lifecycle status surfaced in the UI.
    **Event-driven projections** off siblings' published events, keyed on `{tenant}:{domain}:{aggregateId}`,
    **idempotent + order-tolerant** (version-stamped, last-writer-wins by *source version*, not arrival
    order). **Rule: mirrors for display, live authorization for gates.**
- **Derived-store backing:** ChatBot-owned DAPR state store (Redis), tenant-partitioned, via EventStore
  projections. Association routing, operation status, and the S1 project-conversation read model all
  use this ChatBot-owned store in the live topology. Vector/embedding/prompt-context remains planned via
  **Hexalith.Memories** (Redis Vector / FalkorDB) with store-layer tenant isolation (NFR9a) — M2.
- **Association scorer:** deterministic-signals kernel (explicit project ID / mailbox routing rule /
  thread ID) in the **Association** module, producing `[0,1]` confidence vs `T_high`/`T_low`. Deterministic
  only in M0; learned signals enter M1 (addendum §Confidence Thresholds / §Risk Classifier).
- **Idempotency — two altitudes:** coarse request-dedup at the CommandGateway + fine event-dedup at the
  aggregate (EventStore idempotency cache); per-operation-class keys per addendum §Idempotency Keys.
- **Derived-state versioning & deterministic replay (cross-cutting #11):** event upcasting for evolving
  AI-proposal/projection shapes; projection schema version stamped in replay traces; *as-of* upstream
  resolution on rebuild (never re-query *current* Party/Folder data); consumer-driven contract tests
  (Pact-style) against the 7 sibling contexts.

### Authentication & Security

- **Identity (platform):** Keycloak OIDC; `tenantId` from authenticated claims only, never request body;
  cross-tenant identifiers rejected even with valid credentials in another tenant.
- **Authorization:** two-layer — API gate (claims/tenant/RBAC) + domain authorization inside the
  CommandGateway, before any aggregate load. Redacted denials that don't confirm resource existence.
- **Tenant isolation:** by construction at every layer incl. derived stores, caches, vector indexes,
  projections, logs, error bodies, pagination cursors. M0 is single-tenant but **tenant-partitioned by
  construction** so M1's second tenant is additive, not a rewrite.
- **Fail-closed invariant (NFR15a, D4):** enforced at a **single injectable audit-commit seam** every
  state-writing path calls before persisting; new state-writing paths fail by omission if they skip it
  (test parametrized from the same path enumeration the code uses). Only **pre-commit** paths fail closed.
- **Redaction:** a **swappable policy stage** (trim-safe to a coarse default), applied consistently across
  UI/CLI/MCP/export.

### API & Communication Patterns

- **FR81a CommandGateway (D3) — the keystone:** a `CommandGateway` admission layer in `Hexalith.ChatBot.Server`
  runs `auth → tenant-bind → authorize → risk-classify → approval-gate → coarse-idempotency → pre-commit-audit`,
  then dispatches into EventStore's existing write path (`fine-idempotency → execute → publish → projection`)
  and emits post-commit audit. **It is NOT a second command pipeline.**
- **Parity by construction:** surface adapters (UI/CLI/MCP) depend only on `Hexalith.ChatBot.Client` and
  construct only a typed `IChatBotCommand`; `IRiskClassifier`/`IApprovalGate`/`IAuditWriter` are `internal`
  to `.Server` (stage-replication = compile error). Enforced by a **NetArchTest** + a **differential-conformance
  harness** over surface-agnostic semantic intents (event-sequence + state-store end-state equivalence across
  UI/CLI/MCP; include rejection + retry intents) — exercised from **M0 via thin CLI/MCP test shims**, not
  shipped, so M1 parity debt surfaces early.
- **Sibling integration & orchestration (D1):** writes to siblings go through their EventStore commands
  (they own the aggregates); ChatBot maintains derived state from siblings' published events; multi-step
  cross-context operations use coordinator/activity seams now and bind to Dapr Workflow before production saga
  claims; synchronous invocation reserved for trivial single in-tenant writes.
- **Contract Spine (D7):** OpenAPI 3.1 spec is the single contract source → generated client + parity-oracle
  rows + idempotency helpers (Folders pattern). Problem responses metadata-only (RFC 9457).
- **Two-phase audit (D4):** *pre-commit* audit (intent/risk/approval) = fail-closed gateway gate; *post-commit*
  WORM hash-chain audit (NFR49a) = **fail-open-then-reconcile** (event log is source of truth; chain rebuilt
  from it on recovery — cannot block-the-commit AND derive-the-chain on the same write). Completeness (NFR50a)
  = reconstructability, verified by a scheduled production assertion that rebuilds state and diffs the projection.
- **Surfaces:** EventStore command/query + REST; CLI (M1); MCP server (M1, ModelContextProtocol 1.3.x);
  SignalR projection-nudge (re-query on nudge, never trust payload).

### Frontend Architecture

- **Stack (platform):** Blazor + Fluent UI v5 (RC, via FrontComposer); Fluxor state; contract-first
  FrontComposer annotations; REST commands/queries + SignalR nudge.
- **M0 surfaces (NFR60 scope):** S1 project conversation view, S2 ambiguous association review, S3 AI action
  approval. The **conversation view is a read projection a future chat surface can write into via the same
  CommandGateway** — chat becomes a new surface on the spine, not a new subsystem (no fake chat textbox).
- **Accessibility:** WCAG 2.2 AA per-increment to enumerated surfaces; non-color status; EN + FR.

### Infrastructure & Deployment

- **Composition (platform):** .NET Aspire 13.3.x AppHost; DAPR components (`statestore` for EventStore
  actor/status/archive/checkpoint state, `chatbot-statestore` for ChatBot read models and coarse idempotency,
  `chatbot-pubsub` for Redis pub/sub); production deny-by-default `accesscontrol.yaml`; local mTLS-off
  `accesscontrol.local.yaml`; Epic 2 hosts the correction-propagation coordinator seam in-process, with hosted
  Dapr Workflow runtime binding still pending for saga orchestration.
- **WORM audit backing:** append-only store with hash-chained envelopes per tenant; redaction via
  key-destruction with the redaction key in a **separate KMS** (resolves WORM-vs-GDPR-erasure, cross-cutting
  #13); nightly chain verification.
- **Correction propagation (FR91a):** the aggregate owns the `correcting`/`current` lifecycle
  (`Apply(CorrectionStarted)`/`Apply(CorrectionCompleted)`). Epic 2 implements a DAPR-ready coordinator/activity
  seam that emits durable start/acknowledge/complete/delayed events through EventStore; hosted Dapr Workflow
  runtime binding remains pending. Reads during correction check the aggregate flag and block or serve
  `stale=true`; `ReindexVectors(tenantId, correctionId, sourceVersion)` remains an M2 activity and must be
  idempotent + version-guarded.
- **Deploy / recovery:** SDK-container images; Aspire 13.3 K8s/AKS + Helm (M2); RPO ≤ 15 min / RTO ≤ 4 hr
  pending M2 drill; replay/simulation against an isolated test tenant (FR95a, M2).
- **Observability:** OpenTelemetry; structured emission always-on (dashboards trim-able, emission is not);
  published SLOs (M2).

### Internal Decomposition (modular monolith — D5)

One deployable ChatBot service, hard internal seams by derived-state lifecycle, separate assemblies,
events-only across seams (extraction-ready if M2 scale demands):
- **Association** — candidate generation, deterministic scoring, evidence snapshots, association lifecycle.
- **Governance/Mediation** — risk classifier (tag+heuristic, no AI dependency), six risky action classes,
  AI-action proposals, approval records, command allowlist.
- **Lifecycle/Workflow** — workflow-instance maps, lifecycle state machine, coordinator/activity seams, and future
  Dapr Workflow runtime binding.
- **Projection/Query** — projections, queue projections, SignalR nudge, FrontComposer read models.
- **Audit/Replay** — WORM hash chain, replay traces (near-platform concern).
Seam test: *owns an aggregate with its own invariants, or just a folder?*

### Governed AI Mediation

- **Risk classifier:** tag+heuristic (no AI-service dependency → approval gate survives AI outage, NFR22);
  six risky action classes; fail-closed to approval-required on indeterminate.
- **Execution:** approved actions execute only through allowlisted EventStore commands (M0 allowlist =
  `Project.AppendConversationMessage`). The current M0 ChatBot adapter prepares metadata-only append results
  before EventStore submission; it is not yet a durable sibling `Hexalith.Conversations` write binding.
- **A9a gate semantics by milestone:** *directional* gate at M0 (n≈100 positives → ±~6pt CI), *binding +
  CI-aware* at M1 (require lower confidence bound to clear). Budget inter-annotator-agreement / label-quality,
  a frozen held-out partition, and dataset versioning.

### Decision Impact Analysis

**Implementation sequence (architecture-level; respects M0→M1→M2):**
1. Module scaffold + EventStore submodule + Aspire AppHost (first story).
2. Contract Spine skeleton + typed Client + `IChatBotCommand`.
3. CommandGateway with all 9 stage seams (risk/approval stubbed; tenant-partition, fail-closed gate,
   pre-commit audit, idempotency **real**) + NetArchTest + differential-conformance harness.
4. Association module (deterministic scorer, candidate generation, lifecycle) + S2 review UI.
5. WORM audit store + post-commit reconcile + completeness assertion.
6. Governed AI mediation (classifier, proposal, approval gate, one allowlisted command) + S1/S3 UI.
7. Event-driven projections/mirrors + correction propagation (Workflow + aggregate lifecycle).

**Cross-component dependencies:** the CommandGateway is the spine everything routes through; the Contract
Spine constrains all three surfaces; event-driven projections depend on sibling event contracts (Pact tests);
correction propagation spans Association + Lifecycle/Workflow + Projection + Audit; the **safety floor**
(tenant isolation, authorization, fail-closed gate, audit-of-the-command, the gateway spine) must not ride
inside any trim-able stage (redaction depth, approval-policy richness, dashboards) — a dependency map must
prove this.

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined

**Critical conflict points identified:** ~18 areas where AI agents could diverge. The Hexalith platform
already pins most *generic* conventions (recorded below as **[inherited]**); this section concentrates on
the **[ChatBot]**-specific patterns the platform does not pin — the CommandGateway contract, lifecycle-state
vocabulary, derived-record shape, audit envelope, evidence/confidence capture, and parity enforcement.

### Naming Patterns

**[inherited] C# / files:** file-scoped namespaces matching folder path under `Hexalith.ChatBot.*`; one
type per file; `I`-prefixed interfaces; `_camelCase` private fields; `Async` suffix; PascalCase types/members;
**Allman braces** (greenfield default, matching EventStore/Parties — confirm or override to K&R).

**[inherited] EventStore domain naming (reflection-discovered — names are load-bearing):**
- Commands: imperative, no suffix → `AssociateEmailToProject`, `ProposeAIAction`, `ApproveAIAction`.
- Events: past tense, no suffix → `EmailAssociatedToProject`, `AIActionProposed`, `AIActionApproved`.
- Rejections: `{Target}{Reason}Rejection` implementing `IRejectionEvent`, **structured payload only (IDs/enums/
  counts, never English/localized text)** → `EmailAssociationUnauthorizedRejection`, `AIActionNotInAllowlistRejection`.
- Aggregates/projections/state live in `Hexalith.ChatBot.Server` **only** (the only scanned assembly).

**[ChatBot] Identifiers & resources:** ULIDs for `messageId`/`correlationId`/`aggregateId`/`causationId`
(`Ulid.TryParse`, never `Guid`); EventStore identity `{tenant}:chatbot:{aggregateId}`; DAPR AppId `chatbot`,
EventStore actor/status store `statestore`, ChatBot derived state store `chatbot-statestore`, pub/sub component
`chatbot-pubsub`, topic `chatbot.events`, deadletter `deadletter.chatbot.events`; kebab-case for
convention-derived resource names.

**[ChatBot] Lifecycle-state vocabulary (exact strings — shared across UI/CLI/MCP/audit):**
`Received | Proposed | Associated | Rejected | Deferred | NeedsReview | Failed | Skipped | Corrected`
+ sub-states `Correcting | Correction-delayed`. Status enums are stable strings (`healthy|degraded|failed|
unknown`), never derived from counts. Agents must use these names verbatim — no synonyms.

### Structure Patterns

**[inherited] Module boundaries & dependency direction:** Contracts (low-dep) ← Client ← Server; CLI/MCP/UI
depend **only** on Client; Aspire/AppHost/ServiceDefaults at edges; Testing references Server+Contracts.
Tests in `tests/Hexalith.ChatBot.{Area}.Tests` mirroring source; never inline package versions (central
`Directory.Packages.props`).

**[ChatBot] Module-internal seams (D5):** source organized by derived-state lifecycle module —
`Association/`, `Governance/` (mediation+approval), `Lifecycle/` (workflow), `Projections/`, `Audit/` — not
broad type buckets. Cross-seam communication is events-only; no cross-module method calls into another seam's
internals. Governance interfaces (`IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, `IIdempotencyStore`)
are `internal` to `.Server`.

**[ChatBot] Sibling integration:** every sibling client wrapped behind a ChatBot-owned adapter
(`IProjectDirectory` over Projects, `IParticipantDirectory` over Parties, `IFolderStore` over Folders,
`IConversationWriter` over Conversations). **Never call a sibling client from aggregate `Handle` logic.**
Store stable IDs (`ProjectId`/`PartyId`/`FolderId`/`ConversationId`) in events — **never upstream PII**.

### Format Patterns

**[ChatBot] Problem/error responses (Folders pattern, RFC 9457, metadata-only):** `{ category, code, message,
correlationId, taskId?, retryable, clientAction, details.visibility }`. User-safe text drawn from a **versioned
message catalog** (FR77): stable code + headline ≤80 chars + one-sentence reason that names no unauthorized
project/file/party/audit detail. **Raw error text leaking to a user = release-blocking defect (NFR40).**

**[ChatBot] Derived-record shape (every derived class):** carries `tenantId`, `sourceProvenance`,
`derivationKernelVersion`, `redactionState`, `retentionClass`, `schemaVersion`. Decision snapshots are
append-only + superseded (never mutated); live mirrors are version-stamped projections.

**[ChatBot] Evidence & confidence capture (cross-cutting #12 — every proposal/candidate):** `confidenceScore`
∈ `[0,1]`, `thresholdBand` (`auto|ambiguous|fail-closed`), `evidenceRefs[]` (typed signal class + matched
value), `kernelVersion`, `detectedAt`, and (after human action) `correctionOutcome`. A *first-class* shape,
not analytics bolted on later.

**[inherited] Data formats:** JSON camelCase; `System.Text.Json` only (shared options factory, never inline
`new JsonSerializerOptions()`); `DateTimeOffset` UTC server-side, `{Action}At` naming, tenant-local only at
presentation; cursor pagination `{ items, cursor, hasMore }` (never offset/limit); ETag/`If-None-Match`→304.

### Communication Patterns

**[ChatBot] Audit envelope (minimum fields, both phases):** `tenantId, actorId, actorType, commandName,
resourceId, decision, reasonCode, correlationId, timestamp, policySnapshotId, sourceEvidenceRefs[],
idempotencyKey?, stateTransition, redactionDecision, outcome`. **Pre-commit** audit = fail-closed gateway gate;
**post-commit** WORM entry = hash-chained envelope (predecessor hash), fail-open-then-reconcile-from-event-log.

**[ChatBot] Correlation propagation:** `correlationId` on every command/event/log/OTel activity, propagated
across mailbox intake → association → file handling → approval → AI mediation → command execution → audit →
UI/CLI/MCP/workers/sibling calls. Logs/traces are **metadata-only** (envelope metadata, never payloads/PII/secrets).

**[inherited] Events & projections:** persist-then-publish; never publish before persistence; DAPR pub/sub is
at-least-once + unordered → **all projection/event handlers idempotent + order-tolerant** (version-stamped,
last-writer-wins by source version); SignalR nudges trigger re-query, never trusted as data; projection reads
surface `stale|rebuilding|unavailable` rather than pretending freshness.

**[ChatBot] Idempotency keys (two altitudes):** coarse request-dedup key at the CommandGateway + fine
event-dedup at the aggregate; per-operation-class composition per addendum §Idempotency Keys; canonical-form
normalization (key ordering, whitespace, NFC) before hashing.

### Process Patterns

**[ChatBot] CommandGateway flow (the spine — every state mutation, every surface):**
`auth → tenant-bind → authorize → risk-classify → approval-gate → coarse-idempotency → pre-commit-audit →
[EventStore: fine-idempotency → execute → publish → projection] → post-commit-audit`. **Surface adapters
translate input into a typed `IChatBotCommand` and call `IChatBotClient.SubmitAsync` — they MUST NOT replicate
any stage.** No path mutates state outside the gateway.

**[ChatBot] Fail-closed (NFR15a):** all 10 state-writing paths route through one injectable audit-commit seam;
on `tenantScope unresolved | authz failure | audit writer down | classifier indeterminate | command not in
allowlist`, return a typed rejection and **write no durable state** (queue intent for replay, never partial write).

**[ChatBot] Lifecycle transitions:** validated against an explicit state model; invalid transitions rejected +
audited (rejected transition, actor, reason, correlation); terminal states (`Rejected`/`Failed`/`Skipped`)
never move back — reprocess creates a **new workflow instance** with `supersedes`/`superseded_by` audit links.

**[ChatBot] Correction propagation (FR91a):** aggregate owns `correcting`/`current` lifecycle via
`Apply(CorrectionStarted/Completed)`; Epic 2's coordinator/activity seam coordinates required M0 derived-store
invalidation and writes durable propagation events, while hosted Dapr Workflow runtime binding remains pending.
Reads during correction check the flag and block or serve `stale=true`; AI actions cannot use corrected context
until invalidation completes.

**[inherited] Domain correctness:** never throw for business-rule violations (return
`DomainResult.Rejection([...])` — exceptions bypass the idempotency cache); aggregate `Handle` is pure
(no I/O/DAPR/await/authz); authorization/orchestration outside aggregate logic; backward-compatible
deserialization for every event ever produced (no `V2` types — additive + upcasting).

### Enforcement Guidelines

**All AI agents MUST:**
- Route every state mutation through the CommandGateway; adapters construct only `IChatBotCommand`.
- Use the exact lifecycle-state strings and reason codes from the message catalog.
- Stamp every derived record with tenant/provenance/kernel-version/redaction/retention/schema-version.
- Keep `tenantId` from authenticated claims; fail closed on unresolved tenant/authz/audit.
- Write tests in the same change: Tier 1 pure aggregate/Handle; cross-tenant isolation negative tests;
  fail-closed parametrized from the path enumeration; idempotency replay/conflict.

**Pattern enforcement (mechanical, not review-by-eyeball):**
- **NetArchTest**: no `*.Cli`/`*.Mcp`/`*.UI` type references `IRiskClassifier|IApprovalGate|IAuditWriter|
  IIdempotencyStore`; dependency-direction edges; aggregates only in `.Server`.
- **Conformance tests**: real-aggregate vs in-memory event-sequence equality.
- **Differential-conformance harness**: same semantic intent across UI/CLI/MCP → identical event sequence +
  state-store end-state (incl. rejection + retry intents); exercised from M0 via thin shims.
- **Cross-tenant isolation**: zero-leak negative tests across 9 actor types incl. cursors + error bodies.
- **Tier 2/3 inspect state-store end-state**, never just HTTP/exit codes.

### Pattern Examples

**Good:**
- `MarcConfirmsAssociation` (UI) and the CLI `chatbot associate` both build `AssociateEmailToProject` and
  call `IChatBotClient.SubmitAsync` → identical `EmailAssociatedToProject` event.
- Unauthorized association attempt → `EmailAssociationUnauthorizedRejection` (structured) → message-catalog
  code `assoc-unauthorized` → UI shows "Association blocked. You do not have access to this project."

**Anti-patterns (reject in review):**
- A CLI adapter calling `IRiskClassifier` directly (replicates a gateway stage — compile error by design).
- An aggregate `Handle` calling `IParticipantDirectory` (sibling call inside pure domain logic).
- Logging a command/event payload or party PII; leaking raw exception text to a user surface.
- Mutating a closed approval record on correction (must supersede); a projection handler assuming ordered/
  unique delivery; `Guid.TryParse` on a ULID identifier.

## Project Structure & Boundaries

### Complete Project Directory Structure

Increment markers: **[M0]** vertical loop · **[M1]** parity+governance · **[M2]** ops+recovery.

```
Hexalith.ChatBot/                              # umbrella module repo root
├── Hexalith.ChatBot.slnx                       # .slnx only (never .sln)
├── global.json                                 # SDK 10.0.300, rollForward latestPatch
├── Directory.Build.props                       # net10.0, nullable, warnings-as-errors, Allman
├── Directory.Packages.props                    # central package management (no inline versions)
├── Directory.Build.targets                     # SDK-container opt-in
├── .editorconfig  .gitignore  nuget.config  README.md  CHANGELOG.md
├── .gitmodules                                 # Hexalith.EventStore (root-level only)
├── .github/workflows/                          # ci.yml, release.yml (semantic-release)
├── Hexalith.EventStore/                        # [M0] root-level git submodule — foundation
├── docs/
│   ├── adrs/                                    # idempotency, schema-evolution, audit-two-phase, gateway, saga
│   ├── contract/                                # Contract Spine + parity-oracle docs
│   └── exit-criteria/                           # per-increment evidence (M0/M1/M2 safety-floor proofs)
├── src/
│   ├── Hexalith.ChatBot.Contracts/             # [M0] low-dep: no infra
│   │   ├── openapi/hexalith.chatbot.v1.yaml     # Contract Spine — SINGLE contract source (D7)
│   │   ├── Commands/                            # AssociateEmailToProject, ProposeAIAction, ApproveAIAction, …
│   │   ├── Events/  └─ Rejections/              # past-tense events; {Target}{Reason}Rejection (structured)
│   │   ├── Queries/                             # GetEmailAssociationStatus, ListProjectAssociationCandidates, …
│   │   ├── Enums/                               # LifecycleState, RiskClass, ActorType, ThresholdBand
│   │   ├── Identities/                          # typed ULID identity helpers; IChatBotCommand marker
│   │   └── Messages/                            # versioned message catalog (codes + headlines, FR77)
│   ├── Hexalith.ChatBot.Client/                # [M0] typed client; IChatBotClient.SubmitAsync(IChatBotCommand)
│   │   ├── Registration/                        # AddHexalithChatBot(...) DI extensions
│   │   └── Generated/                           # NSwag-generated from spine (never hand-edit)
│   ├── Hexalith.ChatBot.Server/                # [M0] the modular monolith (ONLY scanned assembly)
│   │   ├── Gateway/                             # [M0] CommandGateway (the spine, D3)
│   │   │   └── Stages/                          #   Auth, TenantBind, Authorize, RiskClassify, ApprovalGate,
│   │   │                                        #   Idempotency(coarse), AuditPre  — internal interfaces
│   │   ├── Association/                         # [M0] seam: Aggregates/, Scoring/ (deterministic kernel,
│   │   │                                        #   T_high/T_low), Evidence/, Validators/
│   │   ├── Governance/                          # [M0] seam: RiskClassifier/ (tag+heuristic), Approval/,
│   │   │                                        #   AiMediation/, Allowlist/, Aggregates/  ; Outbound/ [M1]
│   │   ├── Lifecycle/                           # [M0] seam: StateModel/ (transitions), Workflows/ (Epic 2
│   │   │                                        #   coordinator/activity seam; hosted Dapr Workflow binding pending)
│   │   ├── Projections/                         # [M0] seam: read models, queue projections, live mirrors
│   │   ├── Audit/                               # [M0] seam: pre/post-commit, WORM hash-chain, replay traces [M2]
│   │   ├── Adapters/                            # ports over siblings + external providers
│   │   │   ├── Projects/  Parties/  Folders/  Conversations/   # [M0] IProjectDirectory, IParticipantDirectory…
│   │   │   ├── Mailbox/                         # [M0] M365/Graph ingestion port (one mailbox pattern)
│   │   │   └── AiProvider/                      # [M0] scoped-context AI port
│   │   └── Registration/
│   ├── Hexalith.ChatBot.Aspire/                # [M0] Aspire hosting extensions
│   ├── Hexalith.ChatBot.AppHost/              # [M0] Aspire AppHost — DAPR topology (statestore,
│   │                                            #   chatbot-statestore, chatbot-pubsub, accesscontrol.yaml,
│   │                                            #   accesscontrol.local.yaml), sibling app refs
│   ├── Hexalith.ChatBot.ServiceDefaults/      # [M0] OpenTelemetry, health, shared host config
│   ├── Hexalith.ChatBot.UI/                   # [M0] Blazor + FrontComposer: S1 conversation, S2 association
│   │                                            #   review, S3 AI approval; [M1] S4–S7; [M2] S8–S10
│   ├── Hexalith.ChatBot.Cli/                  # [M1] System.CommandLine, wraps Client (no DAPR, no stages)
│   ├── Hexalith.ChatBot.Mcp/                  # [M1] ModelContextProtocol .AspNetCore, wraps Client
│   ├── Hexalith.ChatBot.Workers/             # [M0] mailbox-ingestion + retry; [M2] projection rebuild, replay
│   └── Hexalith.ChatBot.Testing/             # [M0] fakes/builders, InMemoryChatBotService, command helpers
└── tests/
    ├── Hexalith.ChatBot.Contracts.Tests/      # [M0] Tier1: naming, serialization round-trip, message-catalog
    ├── Hexalith.ChatBot.Server.Tests/         # [M0] Tier1/2: aggregates, gateway stages, fail-closed table,
    │                                            #   isolation, idempotency (state-store end-state asserts)
    ├── Hexalith.ChatBot.Architecture.Tests/   # [M0] NetArchTest: dep-direction, adapter-cannot-replicate-stage
    ├── Hexalith.ChatBot.Conformance.Tests/    # [M0] differential-conformance harness + parity oracle (shims)
    ├── Hexalith.ChatBot.IntegrationTests/     # [M0] Tier3: Aspire E2E, cross-tenant isolation (9 actors)
    ├── fixtures/                               # A9a evaluation partition, redaction/leakage corpus, oracle rows
    └── e2e/                                    # [M0] Playwright — S1/S2/S3 (axe-core a11y); grows per increment
```

### Architectural Boundaries

**API boundaries:** external = REST commands/queries (EventStore `CommandsController` + ChatBot query
controllers) on `/api/v1/...`; internal EventStore invocation on `/process` (domain processor) + `/project`.
CLI/MCP are governed clients over the same Contract Spine — **never direct data-plane access** (no DB, queue,
mailbox store, index). Every external write enters via the **CommandGateway**.

**Component boundaries (modular-monolith seams):** Association ↔ Governance ↔ Lifecycle ↔ Projections ↔ Audit
communicate **events-only** across seams; no cross-seam reach into internals. Governance stage interfaces
(`IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore`) are `internal` to `.Server`
(NetArchTest-enforced). UI/CLI/MCP ↔ Server only through `IChatBotClient`.

**Service boundaries (sibling contexts):** writes to Projects/Parties/Folders/Conversations go through their
EventStore commands via ChatBot-owned adapter ports; ChatBot consumes their published events to build derived
state. Multi-context operations use coordinator/activity seams now and bind to Dapr Workflow before production
saga claims. Keycloak = identity boundary; M365/Graph = mailbox boundary (degraded per-mailbox, no tenant-wide
fallback); AI provider = mediation boundary (non-AI workflows survive its outage).

**Data boundaries:** EventStore = write-side source of truth for ChatBot aggregates; derived state in
ChatBot-owned tenant-partitioned `chatbot-statestore` (Redis) via projections; vector/embedding in
Hexalith.Memories [M2]; WORM audit chain in a dedicated append-only store with redaction keys in a **separate
KMS**. Cross-tenant queries impossible at the store-access layer (NFR9a).

### Requirements → Structure Mapping

| FR group | Lives in |
|---|---|
| FR1–FR12 Email intake & association | `Server/Association/` + `Adapters/Mailbox/` + UI `S2` |
| FR13–FR20 Participants/identity/authz | `Adapters/Parties/` + `Gateway/Stages/{Authorize,TenantBind}` |
| FR21–FR28 Conversation & context | `Projections/` + `Contracts/Queries/ProjectConversation*` + UI `S1` |
| FR29–FR34 Files & attachments | `Adapters/Folders/` + `Server/Association/` (attachment lifecycle) |
| FR35–FR46 Task intent & AI mediation | `Server/Governance/{AiMediation,RiskClassifier,Approval,Allowlist}` + UI `S3` |
| FR47–FR50, FR48a–d Outbound + authenticity | `Server/Governance/Outbound/` + `Adapters/Mailbox/` **[M1]** |
| FR51–FR63, FR75a–g Admin/governance/audit | `Server/Audit/` + `Projections/` (queues) + UI `S5/S8–S10` |
| FR64–FR80 Reliability/ops/queues | `Workers/` + `Projections/` + `Lifecycle/StateModel/` |
| FR81–FR96 Parity, state model, replay | `Gateway/` + `Contracts/openapi/` + `Conformance.Tests/` + `Lifecycle/` |

**Cross-cutting locations:** tenant isolation → `Gateway/Stages/TenantBind` + every store key; fail-closed →
`Gateway/Stages/AuditPre` (single seam); audit → `Server/Audit/`; correlation → `ServiceDefaults` + envelope;
redaction → swappable stage in `Gateway` + `Contracts/Messages/`; evidence/confidence → `Association/Evidence/`
+ `Governance/AiMediation/`; derived-state versioning → `Projections/` (schema-versioned) + `docs/adrs/`.

### Integration Points

**Internal:** surface adapter → `IChatBotClient` → CommandGateway → EventStore write path → events → DAPR
pub/sub → ChatBot projections + coordinator/activity seams → SignalR nudge → UI re-query.

**External:** Keycloak (OIDC tokens, claims→tenant); M365/Exchange Graph (mailbox subscription/intake [M0],
draft/send [M1]); AI provider (scoped-context mediation); sibling Hexalith services (commands + events);
Hexalith.Memories (vector/graph [M2]).

**Data flow (M0 happy path):** mailbox event → `Workers` intake → `Association` deterministic scoring →
candidates+evidence projection → UI `S2` human confirm → `AssociateEmailToProject` via Gateway → attachment
stored via `Adapters/Folders` → project conversation materialized by ChatBot projections → AI action proposed
(`Governance`) → UI `S3` approval → `Project.AppendConversationMessage` prepared through the M0 metadata-only
conversation writer and submitted through EventStore → audit (pre+post) →
projection → SignalR nudge → UI.

### File Organization Patterns

- **Configuration:** root-level `global.json`/`Directory.Build.props`/`Directory.Packages.props`/`.editorconfig`;
  DAPR components under `AppHost`; Contract Spine under `Contracts/openapi/`.
- **Source:** by seam (lifecycle module), not type bucket; one type per file; `.g.cs`/`Generated/` never hand-edited.
- **Tests:** mirror source boundaries; dedicated `Architecture` + `Conformance` projects; shared `fixtures/`
  (no per-project corpus forks); `e2e/` Playwright with `data-testid`/role selectors.
- **Assets/docs:** ADRs + contract + exit-criteria docs under `docs/`.

### Development Workflow Integration

- **Dev server:** `aspire run` brings up ChatBot + DAPR sidecar, EventStore + Tenants sidecars, the UI surface
  without a DAPR sidecar, and Keycloak with the tenant-claim realm import. Local self-hosted DAPR runs mTLS-off
  and therefore loads `accesscontrol.local.yaml`; production keeps deny-by-default `accesscontrol.yaml` under
  mTLS/Sentry. AppHost edits require Aspire restart.
- **Build:** `dotnet build Hexalith.ChatBot.slnx`; central package versions; warnings-as-errors gate.
- **Deploy:** SDK-container images per packable host; Aspire 13.3 K8s/AKS + Helm publish target [M2]; semantic-
  release on merge to main.

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:** All technology choices are platform-native and version-verified current (May 2026):
.NET 10.0.300, Aspire 13.3.x, DAPR 1.17.x, MCP SDK 1.3.x (GA), xUnit v3. No contradictory decisions remain —
notably the apparent **NFR15a (fail-closed incl. "audit down") × NFR49a (WORM hash-chain) contradiction is
resolved** by the two-phase audit model (pre-commit fail-closed gate vs post-commit reconcile-from-event-log).
Two coherence caveats, both owned: **Fluent UI v5 is still RC** (inherited pre-GA, pinned, do-not-upgrade);
sibling **Aspire versions span 13.1–13.3** (topology/integration note; ChatBot targets 13.3.x to match
EventStore/Tenants).

**Pattern Consistency:** Patterns support the decisions by construction — the CommandGateway + Client-only
adapter surface + NetArchTest operationalize FR81a "parity by construction"; the differential-conformance
harness verifies it; the two-altitude idempotency + single audit-commit seam realize NFR13a/NFR15a;
supersede-not-mutate + version-stamped projections realize FR91a/#11. Naming/communication/process patterns
align with the EventStore foundation (rejections-as-events, persist-then-publish, ULIDs, metadata-only logging).

**Structure Alignment:** The modular-monolith seams (Association/Governance/Lifecycle/Projections/Audit) map
1:1 to the decisions and the FR→structure table; the Contract Spine sits in `Contracts/openapi/`; boundaries
(API/component/service/data) are explicit and enforced (events-only across seams, governance interfaces
`internal`, CLI/MCP/UI → Client only).

### Requirements Coverage Validation

**Functional Requirements Coverage ✅:** All FR groups (FR1–FR96) map to a concrete home (see FR→Structure
table). The two parked open questions are resolved (D1 event-driven+saga; D2 deterministic candidates +
human confirm/correct). M0 covers the full vertical loop for one tenant/mailbox/command; FR47–50/48a–d
(outbound+authenticity), CLI/MCP parity (FR82–83), and full lifecycle land in M1; replay/idempotency-contract/
dashboards (FR95a/FR67 expanded) in M2 — per the fixed increment order, not as omissions.

**Non-Functional Requirements Coverage ✅ (with M2-deferred detail):** Security/isolation (NFR1–12, 9a) —
gateway authz + tenant-partition by construction + derived-store isolation. Reliability (NFR13–22, 15a) —
fail-closed seam, two-phase audit, idempotency, AI-outage tolerance (tag+heuristic classifier has no AI
dependency). Audit (NFR49–55, 49a/50a) — WORM hash chain + reconstructability assertion. Performance
(NFR23–30) — architecturally supported (per-tenant rate limits/circuit breakers, projection reads, noisy-
neighbor isolation); **specific SLO budgets calibrate at M2 per A11** (framed, not yet numeric). Recovery
(NFR56–59) — **RPO/RTO targets pending the M2 continuity drill (A10)**. Accessibility (NFR60–64) — WCAG 2.2
AA per-increment to enumerated surfaces.

### Implementation Readiness Validation ✅

**Decision Completeness:** All M0-critical decisions documented with verified versions; M1/M2 decisions framed
with clear deferral markers. **Structure Completeness:** complete tree with per-file increment markers; all
boundaries and integration points specified. **Pattern Completeness:** ~18 conflict points addressed with
mechanical enforcement (NetArchTest, conformance, differential harness, isolation tests) and good/anti-pattern
examples.

### Gap Analysis Results

**Critical Gaps (block M0):** none. M0 is buildable as scoped (scaffold → Contract Spine → CommandGateway with
real audit/idempotency/tenant-partition + deterministic risk classification and approval gate for AI mediation →
deterministic Association → one allowlisted command → S1/S3/S2 UI).

**Important Gaps (detail before M1/M2 — own with ADRs):**
1. **WORM audit backing technology** not yet named (pattern is clear: append-only + hash-chain + separate-KMS
   redaction keys). Needs an ADR before the M0 post-commit audit store is built.
2. **M365 / Graph intake specifics** (subscription model, least-privilege permission scopes, webhook/replay
   handling) — adapter boundary defined; concrete scopes pending A1 / pilot-tenant grant.
3. **Audit↔execute transactionality spike** — confirm commit-boundary semantics before M0 closes.
4. **M1 detail:** outbound sender-authority mapping enforcement, Keycloak service-account flows, tenant policy
   schema editor (S5), differential-conformance harness fully wired across CLI/MCP.
5. **M2 detail:** vector/embedding store-layer isolation (NFR9a), replay test-tenant mechanics (FR95a),
   operational dashboards (S8–S10), SLO calibration + continuity drill.

**External dependencies (architecture relies on, not architecture-owned):** A9a evaluation dataset +
label-quality / inter-annotator-agreement protocol (Test Architect); pilot-tenant M365 permission grant (A1);
A11 baseline measurement for SLO/threshold calibration.

**Nice-to-Have Gaps:** reversibility/undo pattern (approval-fatigue antidote); AI cost/resource governance
model; explicit ordering-source documentation in the correlation/lifecycle ADR.

### Architecture Completeness Checklist

**Requirements Analysis**
- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped (13 — incl. the 3 Party-Mode additions)

**Architectural Decisions**
- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed (architecturally; numeric SLO calibration deferred to M2/A11)

**Implementation Patterns**
- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**Project Structure**
- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION (M0 scope) — all 16 checklist items confirmed, no Critical Gaps
open. M1/M2 carry intentional deferred detail (Important Gaps above), to be elaborated via ADRs before each
increment begins, per the fixed M0→M1→M2 order.

**Confidence Level:** High for M0; Medium for M1/M2 (by-design deferred detail, not unknown risk).

**Key Strengths:**
- Parity-by-construction is mechanically enforceable (CommandGateway + `internal` governance interfaces +
  NetArchTest + differential harness), not aspirational.
- The hardest contradiction (fail-closed × WORM) is resolved before implementation, not discovered during it.
- Orchestration-not-ownership is preserved (sibling commands for writes, events for derived state, saga for
  multi-context) — ChatBot stays an orchestrator, avoiding the "distended orchestrator" failure mode.
- M0 is a true walking skeleton: minimal surface, complete safety-floor spine — M1/M2 are additive, not rewrites.

**Areas for Future Enhancement:** learned/AI candidate ranking (M1); vector-store isolation + replay (M2);
operational dashboards + SLO calibration (M2); reversibility/undo; AI cost governance.

### Implementation Handoff

**AI Agent Guidelines:**
- Route every state mutation through the CommandGateway; adapters construct only `IChatBotCommand`.
- Honor the safety floor (tenant isolation, authorization, fail-closed gate, audit-of-the-command, the gateway
  spine) — never let it ride inside a trim-able stage.
- Use exact lifecycle-state strings and message-catalog reason codes; stamp every derived record with
  tenant/provenance/kernel-version/redaction/retention/schema-version.
- Write the matching tests in the same change (Tier 1 aggregate, isolation negatives, fail-closed table,
  idempotency); inspect state-store end-state in Tier 2/3.

**First Implementation Priority:** scaffold the `Hexalith.ChatBot` module (the canonical sibling-module shape +
EventStore root submodule + Aspire AppHost), then the Contract Spine + `IChatBotClient`, then the CommandGateway
with all nine stage seams (risk/approval stubbed; tenant-partition, fail-closed gate, pre-commit audit,
idempotency real). Open ADRs for the WORM backing and the audit↔execute transactionality spike before the
audit store lands.
