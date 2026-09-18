---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/source-manifest.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/qualification-evidence.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/reconcile-full-sibling-a13-2026-09-14.md"
  - "_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/implementation-conformance-addendum-2026-07-17.md"
  - "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-17.md"
  - "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md"
  - "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-20.md"
  - "_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md"
  - "_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-15.md"
  - "references/Hexalith.EventStore/_bmad-output/project-context.md"
  - "references/Hexalith.Conversations/_bmad-output/project-context.md"
  - "references/Hexalith.Projects/_bmad-output/project-context.md"
  - "references/Hexalith.Folders/_bmad-output/project-context.md"
  - "references/Hexalith.Parties/_bmad-output/project-context.md"
  - "references/Hexalith.Tenants/_bmad-output/project-context.md"
  - "references/Hexalith.FrontComposer/_bmad-output/project-context.md"
  - "references/Hexalith.Memories/_bmad-output/project-context.md"
  - "references/Hexalith.Commons/_bmad-output/project-context.md"
workflowType: 'architecture'
project_name: 'Hexalith.ChatBot'
user_name: 'Jerome'
date: '2026-05-28'
updated: '2026-09-15'
lastStep: 8
status: 'final'
completedAt: '2026-05-28'
implementationReadinessRebaselinedAt: '2026-07-17'
packageVersionAuthorityCorrectedAt: '2026-07-18'
independentValidationCorrectedAt: '2026-07-20'
productAuthorityReconciledAt: '2026-09-14'
planningBaselineReconciledAt: '2026-09-15'
releaseReadiness: 'blocked-open-gates'
openReleaseGates: [A5, A6, A9a, A10, A11-M1, A11-M2, A13]
---

# Architecture Decision Document

_Reconciled on 2026-09-15 to the finalized PRD, its normative appendices, and the approved planning-baseline change. Architectural design completeness is not implementation, qualification, pilot, or production readiness._

## Normative Authority and Release-Gate Posture

The product authority is the finalized PRD plus `addendum.md`; the approved
`sprint-change-proposal-2026-09-15.md` governs this downstream correction without changing those sources.
`source-manifest.md` fixes the reviewed
brownfield revisions and consumed-contract hashes; it does not prove producer acceptance.
`qualification-evidence.md` owns mutable evidence state, and
`reconcile-full-sibling-a13-2026-09-14.md` is the single current A13 gate result across all nine contexts. If explanatory
architecture prose conflicts with those sources, the PRD package wins and this document must be rechecked.
The A13 result supersedes the initial five-gap extract only for gate status and incorporates the narrow H4/H12
verification. Neither the initial extract nor the narrow verification is separate closure authority. A8 governs
allowlist membership only, while A13 governs
executable producer, authority, audit, concurrency, and fencing acceptance. The source manifest's current baseline is
workspace revision `76f355a038c4abdb3b9fdb3fb836c25053a18fb0` with `material-gaps-recorded`; matching revisions,
hashes, typed transports, interfaces, or architecture decisions do not establish owner acceptance or execution.

The release posture is deliberately blocked:

| Gate | State | Release effect |
|---|---|---|
| A5 — live AI provider | **OPEN** | Live AI is disabled; M0/M1 onboarding is blocked until the candidate/provider contract and negative evidence are accepted. |
| A6 — data protection | **OPEN** | Pilot data/PII persistence, onboarding, and compliance claims are blocked until the data-class contract and independently witnessed owner-runtime evidence are accepted. |
| A9a — detector/classifier qualification | **OPEN** | No `approved-current` exact-artifact record exists; the affected TaskIntentDetector or ActionRiskClassifier remains disabled, and M0 first use plus M1/M2 revalidation are blocked. |
| A13 — owner execution, authority, audit, and fencing | **OPEN** | The indivisible exact-candidate owner bundle is unaccepted; Conversations append/assignment, onboarding, M0/M1, and tamper-evidence claims are blocked. |
| A11-M1 — mandatory M1 metric qualification | **OPEN** | No `approved-current` record freezes and evidences the mandatory M1 measurement contract; the governed cross-surface pilot claim is blocked. |
| A10 — recovery qualification | **OPEN / provisional** | No qualifying current hosted four-job controlled-loss/full-window evidence exists; M2 production and release-candidate claims are blocked. |
| A11-M2 — exact-candidate SLO qualification | **OPEN / unsupported** | Every SLO row is unsupported without its exact-candidate evidence bundle; M2 production and release-candidate claims are blocked. |

M0 requires current A5/A6/A13 approvals, with exact A9a M0 detector/classifier records gating first use. M1
revalidates A5/A6/A13 and exact deployed A9a records and additionally requires A11-M1. M2 revalidates
A5/A6/A13/A9a/A11-M1 against the changed exact candidate and additionally requires A10 and A11-M2.
These gate approvals are necessary but not sufficient because the PRD's sole increment table owns all release evidence,
disable conditions, sequencing, and permitted claims. The presence of code, interfaces, historical artifacts,
architecture reviews, or planning status does not close any gate.
Gate evidence identifies the exact candidate revision, contract/package versions, storage/provider profile,
responsible producer, test runner, time, result, independent verification, and expiry/reopen rule. A5 is approved by
Security + Architecture; A6 by Compliance/Data Protection + Architecture with Parties/EventStore owner evidence;
A13 by the System Architect and Conversations/Projects/Tenants/EventStore owners with Security validating authority
and fencing. Missing, expired, changed, mismatched, or partially accepted evidence leaves the gate open.

Release Governance owns one immutable gate-record registry conforming to the addendum Increment Gate Record Contract.
For each candidate/increment it publishes one immutable `gate_set_id` referencing every required record ID and binding
their exact candidate, dependencies, environment, expiry, and reopen data. Consumers recompute current status from those
immutable records and the current candidate/environment at use time. Publication and consumption are all-or-nothing: an
incomplete, mixed-candidate, expired, invalidated, or superseded set fails closed. CI/release decisions
and runtime artifact/store/surface enablement consume that same `gate_set_id`; local booleans, copied status, prose, or a
different record set cannot grant authority. Drift requires a new immutable set.

## Architecture at a Glance — Decision Map

**Critical design decisions — settled without closing qualification gates:**

- **D1 — Sibling integration and orchestration:** event-driven integration, with the minimum hosted Dapr Workflow
  correction binding owned by canonical Epic 2 before production correction claims.
- **D2 — M0 association-proposal model:** deterministic candidate generation with evidence and human confirm/correct.
- **D3 — FR81a placement:** every mutating origin enters one `CommandGateway`/EventStore command spine; adapters
  never replicate a stage.
- **D4 — Atomic mutation and audit ledger:** the domain event, lifetime idempotency result, applied policy/approval
  references, and hash-linked canonical envelope commit together through the A13-gated actor-dispatched write target,
  or none commit. The current EventStore baseline does not yet support the full unit.
- **D5 — Internal decomposition:** modular monolith with hard, event-mediated seams.
- **D6 — Derived-store modeling:** immutable decision snapshots use supersede-not-mutate; fresh live mirrors use
  version-stamped event-driven projections.
- **D7 — Contract surface:** OpenAPI 3.1 is the sole HTTP wire-contract source.
- **D8 — Host-layer reuse:** ChatBot is an EventStore domain module; its admission layer mounts at the platform
  pre-commit hook, while the AppHost remains only an ADR-scoped local-development umbrella.
- **D9 — Canonical workflows and recovery:** the PRD owns family transitions; Retry Profiles own retryability,
  maxima, backoff, exhaustion, recovery owner, and manual recovery commands.
- **D10 — Cross-context authority:** owner-context authority is current and explicit; local mirrors never grant
  trust-bearing authority.
- **D11 — M0 governance bootstrap:** stable commands and two distinct current Tenants owners create governance;
  direct seeding is prohibited.
- **D12 — Evidence-gated release:** A5/A6/A13 block M0; A9a gates detector/classifier first use and is revalidated at
  M1/M2; A11-M1 additionally blocks M1; A10 and A11-M2 additionally block M2, with changed lower gates revalidated.
- **D13 — Runtime control and work isolation:** one durable control/rate-limit view, fail-closed consumers,
  tenant-partitioned fair scheduling, and one operational owner govern workload execution.

Correction orchestration, A9a qualification, M365/Graph integration, checkpoint topology, and the deterministic
association kernel shape the implementation. Vector activation, replay-only composition, dashboards, and learned
association signals remain deferred implementation breadth, not deferred release gates.

## Project Context Analysis

### Requirements Overview

**Functional Requirements (117 identifiers: FR1–FR96 plus lettered extensions):** ChatBot orchestrates a governed email-to-project
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

**Non-Functional Requirements (79 identifiers: NFR1–NFR70 plus lettered extensions) shaping architecture:**

- **Security/privacy (NFR1–NFR12, NFR9a):** authorization at every boundary; redacted failure
  responses; encryption in transit/at rest; least-privilege M365 & service-client scopes; bounded
  auth-cache staleness (5 min normal / 60 s revocation); **derived-store tenant isolation by
  construction at the store layer**, not application filtering.
- **Reliability/integrity (NFR13–NFR22, NFR13a/15a/17a):** per-operation-class idempotency contract;
  one atomic fail-closed durability boundary across every enumerated mutation path; at-least-once worker delivery;
  AI-outage tolerance for non-AI workflows; correction-propagation SLO (p95 ≤ 10 min M0/M1, ≤ 60 min M2).
- **Performance/scalability (NFR23–NFR30):** p95 2 s UI reads; 10 s candidate generation; CLI/MCP
  long-running → operation-id within 5 s, no 30 s hold; per-tenant rate limits/quotas/circuit breakers;
  cross-tenant noisy-neighbor isolation.
- **Integration (NFR31–NFR36):** M365/Graph tolerance for throttle/revoke/replay; contract-verifiable
  responses with stable identifiers/codes; versioned contracts; correlation context everywhere;
  server-side UTC time.
- **Operability (NFR37–NFR48, NFR42a):** health/queue observability; A11-M1 metric and A11-M2 SLO qualification; message-catalog-driven
  user-safe states; approval-fatigue mechanisms (prioritization, grouping, rate ceiling, rubber-stamp
  observable); evidence-freshness chips.
- **Audit/compliance (NFR49–NFR55, NFR49a/50a):** `100%` of durable mutations atomically co-commit a
  hash-linked canonical envelope; the rebuildable investigation view has a separate ≥99.5% M2 availability
  target; GDPR
  retention classes; consent/lawful-basis metadata.
- **Recovery (NFR56–NFR59):** RPO ≤ 15 min / RTO ≤ 4 hr remain provisional per A10. The prior hosted
  bundle is historical, expired, predates the controlled-loss job, and could not demonstrate the four-hour recovery window;
  the accepted Epic 12 contract remains `activation: pending`. Projection rebuild from source ≤ 4 hr and scoped
  outage degradation also require fresh exact-candidate evidence.
- **Accessibility (NFR60–NFR64):** WCAG 2.2 AA scoped per-increment to enumerated surfaces; non-color
  status; keyboard/screen-reader for core flows; English + French.
- **Quality gates (NFR65–NFR70):** negative authorization tests across 9 actor types; isolated
  replay/simulation; every operation defines transition/audit/redaction/idempotency.

### Scale & Complexity

- **Primary domain:** distributed backend/service orchestration (.NET 10 + Dapr + Hexalith.EventStore)
  with a Blazor/FrontComposer web UI and CLI + MCP machine surfaces.
- **Complexity level:** High / enterprise (multi-tenant zero-tolerance isolation, GDPR, cross-surface
  parity, governed AI, M365 integration, event-sourced tamper-evident audit).
- **Estimated architectural components (provisional):** ChatBot service (governed command gateway +
  domain processors), Association context, Task-Intent/AI-Mediation context, Governance/Approval context,
  Lifecycle/Workflow context, Projection/Query layer (+ SignalR nudge), Audit/Replay layer, mailbox-ingestion
  adapter (M365/Graph), AI-provider adapter, CLI adapter, MCP server adapter, Blazor UI, background workers,
  + integration adapters to Projects/Parties/Folders/Tenants/Conversations/Memories/EventStore.

### Technical Constraints & Dependencies

- **Fixed platform stack:** .NET 10 (repository SDK pin 10.0.400, net10.0, nullable + warnings-as-errors, package-reference
  versions owned solely by `references/Hexalith.Builds/Props/Directory.Packages.props`); Dapr (actors,
  at-least-once pub/sub, workflow, service invocation, deny-by-default ACLs);
  .NET Aspire orchestration; Hexalith.EventStore as the write-side foundation (CQRS/ES,
  `{tenant}:{domain}:{aggregateId}`, persist-then-publish, pure `Handle`/`Apply`, rejections-as-events,
  ULIDs not GUIDs, `system` platform tenant, EventStore owns its current command/event envelope; the proposed
  FR81a atomic canonical-audit contract is not in the pinned public contract and remains A13-blocked);
  Keycloak OIDC; Blazor + Fluent UI v5
  (RC-pinned) via Hexalith.FrontComposer (Roslyn source generators, Fluxor, REST commands/queries +
  SignalR projection-nudge, MCP descriptors).
- **Bounded-context dependencies (consume by stable ID, never duplicate authority):** The reviewed baseline includes
  these nine contexts: Projects, Conversations, Parties, Folders, Tenants, EventStore, FrontComposer, Memories, and
  Commons. Memories is optional post-MVP/M2 and cannot expand M0/M1 authority.
- **Module conventions inherited:** Contracts→Server dependency direction; CLI/MCP wrap the typed Client and
  never bypass the command pipeline or touch Dapr directly; tenant isolation physical (not just filtered) for
  indexes/caches/graphs; metadata-only logging (no payloads/PII/secrets); wrap sibling clients behind adapters
  (e.g., `IParticipantDirectory` over Parties); local event-fed mirrors for display only and current owner/gateway
  authorization for trust-bearing gates;
  contract-first FrontComposer annotations; additive, serialization-tolerant schema evolution (no V2 event types).
- **Checkout-root submodule policy:** "root-declared" is relative to the repository checkout being validated.
  In the ChatBot umbrella, initialize only entries declared by ChatBot's root `.gitmodules`, non-recursively;
  never initialize a submodule declared by any checkout below `references/`. For independent validation, use
  an isolated standalone checkout of the consumer at the exact gitlink commit pinned by ChatBot. That standalone
  consumer is the validation root, so only dependencies declared by its own root `.gitmodules` may be initialized,
  with explicit pathspecs and without `--recursive` or `--remote`; dependencies of those initialized checkouts
  remain uninitialized. Timesheets may therefore initialize its own root-declared `Hexalith.Works` checkout only
  in the standalone Timesheets validation checkout.

**Shared NuGet package-version authority (binding):**
`references/Hexalith.Builds/Props/Directory.Packages.props` is the sole catalog for package-reference versions.
Every .NET consumer root keeps a version-free `Directory.Packages.props` wrapper that imports this catalog.
Consumer repositories must not declare `PackageVersion Include`, `PackageVersion Update`, dependency-version
properties, `PackageReference Version`, nested `Version`, or `VersionOverride`. Architecture and CI validation
evaluate the imported catalog and reject missing imports, unresolved or duplicate catalog entries, and local
version workarounds. NuGet SDK resolver pins such as `Aspire.AppHost.Sdk/<version>` and versions in
`.config/dotnet-tools.json` cannot consume Central Package Management; they are explicit exceptions with
separate inventory and family-alignment gates.

Independent consumer completion evidence is produced from those standalone checkouts, never by filling nested
dependency checkouts inside the ChatBot umbrella. The validation method does not authorize a ChatBot `.gitmodules`
change, removal of dependency projects from a consumer `.slnx`, a consumer gitlink change, or reuse of evidence
captured at a commit other than the gitlink pinned by the recorded ChatBot baseline. After every standalone
consumer lane is green, the unchanged ChatBot umbrella is validated separately at that same baseline.

- **External constraints:** M365/Exchange Graph permission model (least-privilege, delegated/shared/send-on-behalf);
  GDPR/EU data protection.

### Cross-Cutting Concerns Identified

1. **Tenant isolation** — zero-tolerance; enforced at command, query, store, cache, vector index, projection,
   log, and error-body layers; `tenantId` from Keycloak claims only, never request body.
2. **Authorization at command/query boundary** — two-layer (API gate + domain), inside the gateway; redacted
   denials that don't confirm resource existence. *Rule to lock: mirrors for display, live authorization for gates.*
3. **Governed command spine (FR81a)** — every mutation from UI, CLI, MCP, service clients, AI actors, workers,
   and mailbox events enters `CommandGateway` and the required A13-gated EventStore write target. Admission performs
   authentication, tenant binding, the applicable owner-authority row, action-risk classification, approval validation,
   stable operation identity,
   expected-revision validation or an A13-approved owner guard, and canonical-envelope construction. Adapters may
   only translate to typed commands; they never reproduce a stage.
4. **Fail-closed atomicity (NFR15a)** — domain event, durable terminal idempotency result, applied policy/approval
   references, and canonical audit envelope all commit or none commit. `AuditUnavailable` writes no authoritative
   state. There is no post-commit repair route for a missing mutation envelope.
5. **Idempotency and concurrency (NFR13a)** — durable `operation_id` and `decision_slot_id` identities live for
   the governed record lifetime; expected revision controls races independently. A gateway cache may optimize
   admission but never substitutes for the atomically committed terminal idempotency record.
6. **Auditability & tamper-evidence (NFR49a/50a)** — canonical mutation envelopes are hash-linked inside the
   aggregate command stream and co-commit with the mutation. A signed per-tenant checkpoint anchors stream heads.
   Post-commit audit/investigation projections are rebuildable availability views only; they cannot authorize,
   complete, or repair a mutation.
7. **Redaction & data governance** — retention classes, redaction-aware audit, consistent redaction across
   UI/CLI/MCP/export; isolate redaction as a swappable policy stage (trim-safe to a coarse default).
8. **Observability & SLOs** — OpenTelemetry signal emission is mandatory. A11-M1 must freeze and evidence the M1
   measurement contract; A11-M2 rows remain `unsupported` until they have exact-candidate targets, budgets, signals,
   routes, and burn tests. Dashboards are later presentation, not evidence by themselves.
9. **Governed AI mediation** — scoped context packaging, risk classification, approval gates, allowlisted commands,
   refusal behavior, AI-outage resilience for non-AI workflows.
10. **Correlation & lifecycle-state consistency** — canonical state machine shared across surfaces; correlation
    propagated through every surface, worker, and projection.
11. **Derived-state versioning & deterministic replay** *(added — unanimous Party Mode finding)* — ChatBot owns
    derived state rebuilt by replaying events; a projection schema change (AI-proposal shapes will churn) must map
    old events → new schema (event upcasting), or replay produces state divergent from live, making evidence
    snapshots/approval records non-reproducible and undermining NFR49a. Includes: projection schema version stamped
    in replay traces, *as-of* upstream resolution (don't re-query *current* Party/Folder data during rebuild), and
    cross-context consumer-driven contract testing against the exact nine-context baseline in the source manifest.
12. **Evidence & confidence capture** *(added — product-thesis finding)* — the product exists for *reliable
    association*. Every association candidate and task-intent result structurally carries its confidence, evidence basis,
    and human-correction outcome because these data form the pilot's A11-M1 measurement and model-improvement loop.
    AI-action proposals carry the categorical ActionRiskClassifier class, version, and input tuple; no numeric confidence
    may soften or override that class. A fully governed, fully audited system can pass every other concern while still
    failing the product if it proposes the wrong project.
13. **WORM-vs-erasure tension (GDPR)** *(added)* — immutable canonical envelopes and signed checkpoints do not
    waive A6. Retention, legal-hold precedence, key granularity/custody, backup propagation, crypto-erasure, and
    surviving metadata require the approved A6 contract and runtime proof before persistence or pilot claims.
14. **Qualification authority** — planning and code reality can establish design fit, not release readiness.
    A5/A6/A13 gate M0; exact A9a records gate detector/classifier use and are revalidated later; A11-M1 additionally
    gates M1; A10 and A11-M2 additionally gate M2 after lower-gate revalidation. Missing evidence is blocking or
    `unsupported`, never inferred.

**Watch list (monitor and merge into the concerns above when applicable):** reversibility or undo as the
approval-fatigue antidote rather than more friction; AI cost and resource governance (B2B unit economics); and an
explicit ordering source (source version, not wall-clock time).

### Brownfield Ratification Note

The multi-perspective reconciliation ratified D1–D13 without granting release qualification. Unique qualifiers now
live with their owning decisions: lifecycle seams under D5, correction under D9, command-created M0 governance under
D11, runtime controls under D13, A9a under governed AI mediation, and the non-trimmable safety floor in the
implementation sequence. A5, A6, A9a, A10, A11-M1, A11-M2, and A13 remain evidence and approval gates, not design alternatives.

## Starter Template Evaluation

### Primary Technology Domain

Distributed **.NET service-oriented application** on the Hexalith platform: Dapr-based event-sourced
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

- **Foundation:** `Hexalith.EventStore` as a **root-declared git submodule under `references/Hexalith.EventStore`**
  (never recursive). It provides the command/aggregate/projection/query/SignalR/CLI/MCP primitives that ChatBot
  builds on.
- **Closest structural reference:** `Hexalith.Folders` — most complete recent multi-surface sibling
  (REST + CLI + MCP + read-only Blazor UI + background workers + an **OpenAPI Contract Spine** with
  generated client + idempotency helpers + parity-oracle tests). The `Hexalith.Folders` structure maps almost
  one-to-one to ChatBot's
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
  validators, CommandGateway, governance internals), `Testing`; surface adapters added per increment: `.UI` (M0),
  `.Cli` + `.Mcp` (M1), `.Workers`; `tests/` mirroring each project (xUnit v3). **Post-TE-1 (D8):** the
  standalone `Aspire` and `ServiceDefaults` projects are retired; `AppHost` remains only as an ADR-scoped
  local-development umbrella while platform composition lacks dedicated ChatBot resource support.
- Add EventStore as a **root-declared submodule under `references/Hexalith.EventStore`** (`git submodule update --init`, not `--recursive`).
- Root config: `global.json` (10.0.4xx selector: baseline `10.0.400`, `rollForward=latestPatch`, resolved
  `10.0.401` during the 2026-09-14 review), `Directory.Build.props` (nullable, warnings-as-errors),
  version-free `Directory.Packages.props` importing
  `references/Hexalith.Builds/Props/Directory.Packages.props`, `.editorconfig`, `nuget.config`.
- Wire Aspire AppHost + Dapr components: canonical EventStore actor/status store `statestore`, ChatBot derived
  state store `chatbot-statestore`, Redis pub/sub `chatbot-pubsub`, production deny-by-default
  `accesscontrol.yaml`, and local mTLS-off `accesscontrol.local.yaml`; verify `aspire run` brings up the topology.

## Technology Baseline and Qualification Boundaries

Repository pins were verified against repository state on 2026-09-14.

| Concern | Decision | Verified status |
|---|---|---|
| Language & runtime | `net10.0`; `global.json` baseline `10.0.400` with `rollForward=latestPatch`, resolved `10.0.401`; effective C# 14 under repository `LangVersion=latest` | Feature-band selector, not an exact SDK/language pin; checked-in CI/release still requests incompatible `10.0.302`, so no uniform/hermetic SDK or evidence claim |
| Persistence / write model | Hexalith.EventStore (CQRS/ES, `{tenant}:{domain}:{aggregateId}`, persist-then-publish, pure Handle/Apply, rejections-as-events, ULIDs, `system` platform tenant) | Foundation submodule |
| Messaging / orchestration | Dapr application .NET SDK `1.18.5`; current general CI/release CLI/runtime `1.18.0/1.18.0` plus checked-in CLI SHA-256 | Current general wiring only; non-qualifying for the accepted recovery-primary target |
| Epic 12 `recovery-primary` target | Dapr CLI/runtime `1.18.2/1.18.4`; CLI archive SHA-256 `ccfff008fd16f50096a9192ad56697ac7052e3add6fa0a07789d87b4c4df8c40` | `activation: pending`; current recovery jobs still use mismatched `1.18.0/1.18.0` and cannot produce completion authority |
| Hosting / composition | .NET **Aspire 13.5.3** AppHost | Declared by the current AppHost SDK and shared catalog |
| Hosting integrations | `Aspire.Hosting.Keycloak 13.5.3-preview.1.26425.3`; `CommunityToolkit.Aspire.Hosting.Dapr 13.5.0-preview.1.260825-0345` | Deliberate prerelease pins; repository Dapr 1.18 pairing requires live topology evidence and is not upstream-certified by package presence |
| Published runtime base | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` | Floating 10.0 patch; exact image digest belongs in candidate evidence |
| UI | Blazor + prerelease **Fluent UI v5 `5.0.0-rc.5-26219.1`** via FrontComposer | Shared-catalog prerelease pin; upgrade only through dependency governance |
| CLI surface (M1) | System.CommandLine `2.0.11` wrapping `Hexalith.ChatBot.Client` | Shared-catalog pin |
| MCP surface (M1) | **ModelContextProtocol 2.2.0**; the implemented ChatBot MCP adapter uses stdio server transport, wraps `Hexalith.ChatBot.Client`, and translates tools to commands/queries without local governance | Pinned in the shared Builds catalog and evaluated through the consumer wrapper; architecture tests assert the evaluated pin and adapter boundary |
| AI context / vector store | Hexalith.Memories (Redis Vector / FalkorDB) for scoped AI context + vector indexes (M2, NFR9a isolation) | Existing module |
| Testing | xUnit v3 `4.0.0`, Shouldly, NSubstitute, Testcontainers; unit / Dapr integration / Aspire E2E; conformance + isolation + idempotency as release gates | Shared-catalog pin; four UI contract assertions still expect `3.2.2`, so their passing status is not inferred |
| Code organization | Fixed module boundaries; strict Contracts→Server direction; CLI/MCP/UI depend only on Client; governance interfaces `internal` in Server (mechanical FR81a parity guarantee, NetArchTest-verifiable) | Platform convention |
| Solution / release | `.slnx` format; Conventional Commits + semantic-release | Platform convention |

**Note:** The checked-in OpenAPI 3.1 Contract Spine remains the sole HTTP wire-contract source; the typed Client is
generated from it through `Client/nswag.json`, and generation/parity-oracle checks are mandatory. PRD operation IDs,
surface exposure, MCP tags, AI allowlist, and owner mappings remain separate deny-by-default artifacts.

The Dapr application SDK, CLI, sidecar runtime, .NET SDK, and container digest are independent version planes.
Topology/recovery evidence records all resolved identities; none is implied by `net10.0` or a NuGet package. Current
upstream patches reviewed on 2026-09-14 (`Dapr.Client 1.18.7`, runtime `1.18.4`, System.CommandLine `2.0.12`,
xUnit `4.0.1`) do not authorize upgrades. `NuGetAudit=false` means this review is not a vulnerability disposition.
Even an independently activated Epic 12 completion lane cannot satisfy A10 without the separate fresh exact-candidate
controlled-loss and full-window/production-shaped operational evidence.

## Core Architectural Decisions

### Decision Rationale and Qualification Boundary

The decision map above resolves the two architectural forks: integrations are event-driven with a narrowly owned
workflow coordinator, and M0 uses deterministic association proposals with human confirmation or correction. The
selected target is `[ADOPTED]` architecture, not proof that an owner context implements or accepts the contract.
Post-commit projections and checkpoints cannot substitute for D4 canonical-audit durability.

Runtime-control activation precedes observability hardening. Association scoring remains deterministic in M0, and
correction coordination remains subordinate to aggregate lifecycle truth. Existing code, story status, compatible
interfaces, or platform packages are evidence inputs only and do not waive the release gates.

### Data Architecture

- **Write model (platform):** Hexalith.EventStore CQRS/ES — persist-then-publish, pure `Handle`/`Apply`,
  rejections-as-events, ULIDs, `{tenant}:{domain}:{aggregateId}`. ChatBot is a new EventStore domain
  service; its aggregates and projections are auto-discovered from `Hexalith.ChatBot.Server`.
- **ChatBot owns derived state, split by mutability (D6):**
  - **Immutable decision snapshots** — candidate rankings, evidence snapshots, AI-action proposals,
    approval records, policy snapshots. Append-only, **superseded not mutated**. FR91a correction =
    *supersede + re-evaluate-forward*: open proposals re-evaluate against the corrected association;
    closed/approved proposals remain immutable history. Immutability here is the audit defense.
  - **Live mirrors** — membership, ACL/authorization state, sibling lifecycle status surfaced in the UI.
    **Event-driven projections** off siblings' published events, keyed on `{tenant}:{domain}:{aggregateId}`,
    **idempotent + order-tolerant** (version-stamped, last-writer-wins by *source version*, not arrival
    order). **Rule: mirrors for display, live authorization for gates.**
- **Derived-store backing:** ChatBot-owned Dapr state store (Redis), tenant-partitioned, via EventStore
  projections. Association routing, operation status, and the S1 project-conversation read model all
  use this ChatBot-owned store in the live topology. Vector/embedding/prompt-context remains planned via
  **Hexalith.Memories** (Redis Vector / FalkorDB) with store-layer tenant isolation (NFR9a) — M2.
- **Physical isolation convention:** physical partition/namespace identity derives only from trusted server tenant
  context. Tenant-qualified addressing applies to aggregate/state keys (`tenant:domain:aggregateId`, with encoded
  segments), caches, opaque cursors, topics/subscriptions, queues/dead letters, search/vector collections, prompt-
  context records, and operational projections. Owner contexts keep sovereign isolation. Use store-native partition/
  ACL enforcement wherever supported; an application predicate is never the sole control. A provider unable to pass
  native-store and API negative-isolation proof fails its first persistence/exposure gate.
- **First-store gate:** each ChatBot-owned record class carries tenant, source provenance,
  `derivationContractVersion`, redaction state, retention class, and schema version; classifier/model versions are
  additional fields only where applicable. Before its first persistence, the class requires A6-approved retention,
  legal hold, export/delete, backup propagation, erasure, and surviving-metadata controls plus native-store and API
  negative isolation proof. M2 cannot retroactively legitimize M0/M1 storage. M2 adds vector/embedding/prompt-cache
  proofs and recurring production probes.
- **Association scorer:** `AssociationScorer` is independently versioned and uses only authorized explicit
  Project IDs, mailbox-routing rules, and conversation/thread identifiers in M0. It emits a finite `[0,1]` score,
  visible candidates, evidence references, version, and one typed reason. Defaults are `T_high=0.90` and
  `T_low=0.60`; `T_low` ranks review only. Only a conflict-free result at or above `T_high` with required evidence
  may auto-associate. Every other automatic outcome enters `NeedsReview`; errors/non-finite values expose no
  candidates. `Deferred` and `Rejected` are authorized human decisions only.
- **Idempotency and concurrency:** the addendum's per-operation identities and semantic equivalence rules are
  binding. The durable identity, canonical equivalence, logical outcome, and conflict disposition co-commit with
  the event and canonical audit for the governed record lifetime. Expected revision is independent; gateway
  deduplication is only an optimization. Retry attempts use stable child identities under the original operation.
- **Derived-state versioning & deterministic replay (cross-cutting #11):** event upcasting for evolving
  AI-proposal/projection shapes; projection schema version stamped in replay traces; *as-of* upstream
  resolution on rebuild (never re-query *current* Party/Folder data); consumer-driven contract tests
  against the exact nine-context baseline in `source-manifest.md`.
- **Identifier evolution:** `IdentityEvolved` is proposed, not accepted. Until each producer accepts versioned
  schema, authority, ordering, replay/idempotency, rollout, reconciliation, and fallback, ChatBot preserves the
  original audit IDs, rejects operations whose current identity cannot be resolved, and routes authorized review.
  Any accepted migration adds immutable links and never rewrites history; acceptance, versions, and rollout evidence
  are recorded in the source manifest and memlog before the binding is restored.

### Authentication & Security

- **Identity (platform):** Keycloak OIDC; `tenantId` from authenticated claims or trusted service-client context,
  never untrusted API/CLI/MCP/request-body values;
  cross-tenant identifiers rejected even with valid credentials in another tenant.
- **Authorization:** two-layer — API gate (claims/tenant/RBAC) + domain authorization inside the
  CommandGateway, before any aggregate load. Redacted denials that don't confirm resource existence.
- **Owner authority:** ChatBot roles do not create Tenants or Project authority. Every command/query resolves
  the applicable closed owner-authority row rather than indiscriminately requiring every owner: current Tenants
  `TenantOwner` plus an explicit ChatBot grant for admin operations; current Projects resource grant for Project
  operations; explicit audit/compliance grant plus Project authority for unredacted item evidence; and exact
  case-sensitive Keycloak/EventStore claims, originating-resource authority, and operation scope for machine actors.
  Current
  owner/gateway evidence governs revocation-sensitive mutations; claims-only local fallback and mirror-based
  authorization are forbidden for pilot. Missing, stale, unavailable, malformed, or case-mismatched owner evidence
  denies, and the most restrictive result wins. Ordinary identity/policy cache staleness is at most five minutes and
  explicit revocation at most 60 seconds. Global-admin labels and tenant-wide operational visibility never confer
  Project mutation or unredacted evidence access. Every tenant-admin dashboard read uses the auditable-attempt path;
  there is no adapter-specific aggregation threshold. Every accepted admin mutation co-commits its canonical envelope.
  A13 remains open until owners accept the complete mapping.
- **Tenant isolation:** by construction at every layer, including derived stores, caches, vector indexes,
  projections, logs, error bodies, pagination cursors. M0 is single-tenant but **tenant-partitioned by
  construction** so M1's second tenant is additive, not a rewrite.
- **Fail-closed invariant (NFR15a, D4):** every durable write reaches the single atomic commit seam. Missing
  tenant/authority/policy/approval/concurrency/idempotency/audit durability returns the normative typed failure and
  writes no authoritative state. Post-commit projection lag is a separate availability state.
- **Closed tenant policy:** `addendum.md` is the sole policy catalog. Unknown knobs and unsafe combinations reject
  atomically and leave the prior snapshot active. An unset/invalid row takes only its declared safe default; a row
  with no pre-approval default blocks its named operation. Row-specific mutators/approvers, separation of duty, schema
  versions, migrations, and audit are binding. Accepted policy versions co-commit canonical envelopes; rejected attempts
  write no policy/domain/idempotency state and use the auditable-attempt path. Schema drift, dependencies, and unsafe
  combinations are tested at every increment gate. `safety.controls` changes only through
  `ApplySafetyControl`/`ReleaseSafetyControl`; service clients and AI actors cannot mutate policy. A6-dependent
  region/retention/AI-context rows have no pre-approval default and block applicable persistence or invocation.
- **Auditable-attempt availability:** denials, restricted reads, service-client failures, every tenant-admin dashboard
  read, rejected policy changes, and classifier failures require the durable auditable-attempt path. If unavailable,
  the command/query returns redacted `AuditUnavailable`, returns no protected data, writes no domain/idempotency state,
  raises the Operations/Security audit-readiness incident signal, and remains incomplete; telemetry cannot substitute.
- **M0 governance bootstrap:** two distinct current Tenants `TenantOwner` principals use
  `GrantChatBotAdminRole`, `UpdateTenantPolicy`, and `GrantServiceClientPermission` under the target A13-pending
  mapping. Tenant ownership does not itself confer a ChatBot grant: first admin, each policy row, and service grants
  retain their distinct current initiator/independent approver, separation-of-duty, and named Security/A5/A6/schema/
  owner conditions. A rejected, stale, unavailable, malformed, or scope-conflicting approval creates no version.
  Direct database/state seeding is forbidden; absence of an accepted bootstrap path blocks M0 rather than permitting
  a workaround. M0 admin is provisioning automation only; the broad role/policy editor begins at M1.
- **M0 service clients:** bootstrap permits only `mailbox-ingestion-client` (one tenant/mailbox pattern, 90-day
  auto-rotated credential), `audit-projection-client` (tenant-scoped event-read/projection-write, 90 days),
  `background-retry-client` (tenant-scoped retry/status only, 30 days), and `ai-action-mediator-client` (one
  requester/Project/proposal/approval/allowlisted command, expires on use/revocation/5 minutes). Machine identities
  never inherit UI roles, and every grant/action carries its exact principal, tenant, scope, expiry, and audit facts.
- **Mailbox authenticity and outbound authority:** the M0 adapter records provider DMARC/DKIM/SPF verdicts,
  discrepancies in `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender`,
  delegated sender/principal evidence, and `external_sender`. The closed policy is
  `strict|paranoid`; MVP has no permissive mode. M1 outbound authority is exactly `draft-only`,
  `authenticated-user send`, `shared-mailbox send`, `send-on-behalf`, or `approved service-send`; membership,
  delegation, policy, Project authority, and approval are revalidated at execution. Typed mismatch outcomes are
  `policy-blocked`, `delegation-mismatch`, `membership-revoked`, and `approval-missing`; no service send occurs
  without its linked, current `Approved` proposal.
- **Redaction:** a **swappable policy stage** (trim-safe to a coarse default), applied consistently across
  UI/CLI/MCP/export.

### API & Communication Patterns

- **FR81a CommandGateway (D3) — the keystone:** every state-mutating UI, CLI, MCP, service-client, AI-actor,
  worker, and mailbox operation enters one `CommandGateway`/EventStore spine. Authentication, tenant binding, the
  applicable authorization row, stable operation identity, expected revision or an A13-approved owner guard, and
  canonical-envelope construction are universal. The gateway—not an adapter or handler—selects exactly one profile from
  the closed addendum command-to-profile map; unknown, absent, or duplicate mappings reject. Only profile-mandated stages
  run: `ai-read-v1` classifies without proposal approval, `ai-effect-v1` requires determinate risk plus human-only
  approval, and `projection-delivery-v1` performs neither AI risk classification nor AI proposal approval. It is not a
  second EventStore pipeline; no origin may select, switch, omit, reorder, or duplicate a stage. The owning aggregate then
  atomically commits its event, terminal idempotency record, policy/approval references, and canonical envelope before
  publish/project; a pre-commit failure writes none of those authoritative records.
- **Parity by construction:** surface adapters (UI/CLI/MCP) depend only on `Hexalith.ChatBot.Client` and
  construct only a typed `IChatBotCommand`; equivalent input produces the same canonical semantic command payload and
  identity tuple. Origin is attached immutably at the adapter boundary as the sole surface-specific envelope field;
  the conformance oracle compares the semantic tuple and separately asserts expected origin, never byte-compares
  origin-bearing envelopes. Authorization/state/reason/redaction/idempotency/audit
  outcomes are equivalent. CLI/MCP cannot access databases, actors, queues, mailbox/index/tenant stores, or projections
  directly. `IRiskClassifier`/`IApprovalGate`/`IAuditWriter` are `internal`
  to `.Server` (stage-replication = compile error). Enforced by a **NetArchTest** + a **differential-conformance
  harness** over surface-agnostic semantic intents (event-sequence + state-store end-state equivalence across
  UI/CLI/MCP; include rejection + retry intents). M0 started with thin CLI/MCP test shims; Epic 5 replaces that
  proof with production CLI and MCP adapter-backed conformance arms plus the UI/API client seam.
- **Sibling integration & orchestration (D1):** ChatBot invokes only owner-accepted public commands and consumes
  published owner events; it never invents a producer target or dual-writes owner state. Multi-context work uses
  coordinator/activity seams. The current Conversations append is a mapped target, not an executable guarantee,
  and remains blocked by A13.
- **Durable cross-context choreography:** a ChatBot aggregate atomically records orchestration intent/status and
  dispatch eligibility. An idempotent worker submits the accepted owner command with the same `operation_id`, actor
  authority, expected owner revision, policy/approval/evidence references, origin, and correlation. The owner aggregate
  alone commits its effect and canonical envelope. The persisted owner event and committed revision are authoritative;
  a synchronous response advances only when the accepted mapping proves it represents that same committed effect.
  Response and event normalize to one coordinator-command identity derived from owner context, aggregate/effect
  identity, and committed owner revision; duplicates replay the stored outcome, and each A13 mapping names its authority
  signal and identity formula. A committed owner effect is reconciled through delivery/projection recovery and never retried as an
  uncommitted ChatBot effect. No distributed dual-write is permitted, and every mapping remains A13-gated.
- **Contract Spine (D7):** OpenAPI 3.1 spec is the single contract source → generated client + parity-oracle
  rows + idempotency helpers (Folders pattern). The PRD §Command and Query Contracts is the sole stable public-ID
  catalog; ChatBot entry commands, surface exposure, MCP tags, AI allowlist members, and owner executable targets
  are separate mappings. Every mutation carries actor, tenant, correlation, stable operation identity, target IDs,
  expected revision/accepted owner guard, result codes, policy/approval references, and canonical audit metadata.
  Queries apply the same tenant/role filters with no admin/debug bypass. Problem responses are metadata-only
  (RFC 9457). Contract evolution is additive/backward-compatible by default; a breaking API/event/state change needs
  an explicit version, deprecation window, compatibility handling, and migration.
- **Atomic audit (D4):** the required A13-gated actor-dispatched aggregate target assigns predecessor/sequence and co-commits
  the canonical hash-linked mutation envelope. Signed per-tenant checkpoints anchor aggregate-stream heads.
  Audit/investigation projections, notifications, and UI views are post-commit consumers and may lag; none repairs
  a missing envelope. `IProjectionActivationOutbox` is projection-only and cannot implement FR81a. The current
  EventStore can conditionally co-commit an aggregate-local event batch but not the full terminal-idempotency/audit
  unit. Before M0, A13 requires owner acceptance, supported-write-path ACLs, provider ETag/first-write fencing, and
  concurrent-write, duplicate-predecessor, fork, reorder, checkpoint-rebuild, and recovery tests.
- **Surfaces:** EventStore command/query + REST; CLI (M1); MCP server (M1, shared-catalog-pinned
  ModelContextProtocol 2.2.0 with stdio transport in the current implementation);
  SignalR projection-nudge (re-query on nudge, never trust payload).
- **Long-running response boundary:** return operation identity and current status within five seconds p95. Never hold
  a request beyond 30 seconds without a retrievable status containing retry count, partial-output marker, terminal
  reason, next safe action, and correlation.

### Canonical Lifecycle, Identity, and Retry

- The PRD `Shared Workflow Contract` is the sole state/transition authority. The association family uses
  `Received`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Correcting`,
  `CorrectionDelayed`, and `Corrected` exactly as defined there. Other workflow-specific states—participant,
  attachment, task intent, AI action, chat, command, projection, governance, data-subject, and notification—remain
  in their own closed families and must not be collapsed into the association enum.
- The normative family row—not the verb in a command name—decides whether recovery is an in-place transition,
  immutable version successor, linked workflow successor, linked attempt, or stored-outcome replay. Terminal and
  reopen rules remain family-specific. Invalid transitions deterministically reject and use the auditable-attempt path.
  Replaying an operation returns its recorded outcome; semantic drift returns the typed identity/revision conflict.
- Retry Profile `v1` in `addendum.md` is the executable registry. Automatic-retry counts exclude the initial
  attempt; backoff is exponential with full jitter. A deployment may reduce retries or make a reason terminal,
  but the baseline and every stricter tenant/deployment profile still require System Architect and Test Architect
  approval before first gate use. Raising a maximum or making a terminal reason retryable requires a new version and
  fresh approval; architecture/addendum finality is not qualification. Each retry audit records the profile version;
  retry state and canonical envelope co-commit; dead-letter routing is projection only and never authorizes another
  attempt. Every exhausted item exposes typed reason, attempt count, next safe action, owner, and predecessor/successor IDs.
- Approval, policy, admin-role, service-client, safety-control, and queue decisions use zero automatic retries.
  Transport uncertainty resubmits the same `operation_id` only to retrieve the logical outcome. Unknown external
  effects, committed effects, stale approval/revision, and terminal reasons are never blindly retried.

### Frontend Architecture

- **Stack (platform):** Blazor + Fluent UI v5 (RC, via FrontComposer); Fluxor state; contract-first
  FrontComposer annotations; REST commands/queries + SignalR nudge.
- **M0 surfaces (NFR60 scope):** S1 project conversation view, S2 ambiguous association review, S3 AI action
  approval. The **conversation view is a read projection**. A future chat surface writes through the same
  CommandGateway, making chat a new surface on the spine rather than a new subsystem.
- **Governed chat surface (canonical Epic 13; originally delivered through legacy Epic 10):** the interactive composer is now in
  scope as that governed write surface. Every message is **admitted through CommandGateway**; a risky request
  becomes an Epic 4 AI-action proposal (approval-required), never a direct execution. This is **not an ungoverned
  free-form text box**. An indeterminate request instead returns `classifier-indeterminate`, exposes no proposal or
  approval action, and produces no effect. It preserves the original rule: no ungoverned write path. The composer reuses the M0
  allowlisted `Project.AppendConversationMessage`.
- **FrontComposer Shell adoption (canonical Story 13.1; legacy Story 10.1):** `Hexalith.ChatBot.UI` composes through the
  `FrontComposerShell` (`AddHexalithFrontComposerQuickstart()` → `AddHexalithDomain<TMarker>()`), consuming the
  `Hexalith.FrontComposer` submodule read-only. Story 1.14 shipped a temporary token-alias bridge "until the shell
  wrapper lands." This shell adoption closes that deferral. Fluent UI v5 is pinned identically in both repositories
  (`5.0.0-rc.5-26219.1`), so adoption does not require a local version override.
- **ChatBot UI Fluent-only conformance (canonical Epic 13; legacy Epic 12):** mirroring
  FrontComposer's project-wide rule, every `Hexalith.ChatBot.UI` `.razor` page/component must use FrontComposer
  or Fluent UI v5 components (Microsoft Fluent V2) — **never raw `<button>/<input>/<select>/<textarea>`** (raw
  `<a>` nav links allowed). Fluent v5 does not upgrade raw controls, so they render unstyled and lack the NFR60–NFR64
  accessibility affordances. Hand-authored CSS must not recreate primitives a Fluent component provides
  (button styling, heading type-ramp, foreground role) nor use legacy v4/FAST tokens (`--type-ramp-*`,
  `--neutral-*`, `--accent-*`, `--palette-*`, `--design-unit`); custom CSS is permitted only for layout the
  design system does not own. **Enforced by `ChatBotFluentConformanceTests`** (Governance trait), mirroring
  FrontComposer `FluentConformanceTests` and Tenants.UI `DomainUiFluentConformanceTests`. The guard's offender
  allowlist may only shrink and must remain **empty** at canonical Epic 13 completion; **documented carve-outs: none.**
  Background: Epic 10 adopted the shell correctly but its ACs under-specified component-level conformance, so
  interior surfaces stayed raw HTML over a 1,323-line `chatbot.tokens.css` custom design system — retired in
  Story 12.8.
- **ChatBot UI FrontComposer layout composition (canonical Epic 13; legacy Epic 13):** beyond leaf-control conformance, every `Hexalith.ChatBot.UI` routable page must compose through FrontComposer `FcPageLayout` + `FcPageHeader` and Fluent layout/data components (`FluentDataGrid`, `FluentStack`, `FluentCard`, `FluentAccordion`) — **never** hand-rolled page chrome (`.chatbot-page-header`/`.chatbot-page`/`.chatbot-command-bar`) rendered inside the shell `@Body` (which collides with `FrontComposerShell`'s own header region), and **never** `<dl>` monospace data dumps for primary content. Reference pattern: `Hexalith.Tenants.UI` (`MyTenantsPage`/`TenantAuditPage`, guarded by `DomainUiFluentConformanceTests`). Enforced by the sibling `ChatBotLayoutCompositionConformanceTests` guard (Governance trait, mirroring Tenants.UI `DomainUiFluentConformanceTests`; the leaf-control `ChatBotFluentConformanceTests` stays a separate guard); both offender allowlists are shrink-only and must remain **empty**. `App.razor` must link the scoped `Hexalith.ChatBot.UI.styles.css` bundle (mirroring `Hexalith.Tenants.UI`) so the Fluent/FrontComposer `FluentLayout` CSS grid actually loads. Every canonical surface story owns its real rendered route; canonical Story 13.8 repeats the live loopback Kestrel + Chromium (`Page.GotoAsync`) checks as regression confirmation and asserts `.fluent-layout` resolves to `display:grid` so a missing scoped bundle fails the gate. Binding UX detail: [`implementation-conformance-addendum-2026-07-17.md`](ux-designs/ux-Hexalith.ChatBot-2026-05-28/implementation-conformance-addendum-2026-07-17.md).
- **AI-response streaming transport (accepted ADR, canonical Story 13.2; legacy Story 10.6a/10.6b):** the current spine carries SignalR
  projection-nudge only (re-query on nudge, never trust payload). UX-DR32 requires progressive AI response rendering
  with an always-reachable Stop/Cancel. The accepted ADR
  [`docs/adrs/ai-response-streaming-transport.md`](../../docs/adrs/ai-response-streaming-transport.md) extends the
  SignalR projection-nudge model with metadata-only AI response progress nudges and rejects a dedicated streaming
  channel as the default. Canonical Story 13.2 owns the implemented ChatBot-owned,
  tenant-grouped, metadata-only SignalR hub at `/hubs/chatbot/project-conversation-changes`, enabled by
  `ChatBot:ProjectionChangeNotifications:Enabled=true`. The hub sends only an advisory tenant-scoped change signal;
  it uses FrontComposer's wire-compatible `ProjectionChangedDetail` shape (`projectionType`, tenant, conversation
  `groupScope`, bounded operation/source-version/correlation metadata). The UI re-queries the typed project-conversation
  read model for authoritative attempt, sequence, state, attribution, provenance, partial marker, terminal reason, and
  Stop/Cancel state. Partial output is visibly partial and never a committed Project message; stop/cancel/completion use
  expected revision and first-commit-wins, and retry creates one immutable linked attempt rather than resuming output.
  This pivot was chosen after the EventStore projection relay proved unsuitable for the live ChatBot topology: the
  EventStore relay is signal-only and the ChatBot Dapr topology uses `chatbot-pubsub`, not the relay's `pubsub`
  component. The decision must not weaken the "never trust payload" or fail-closed posture.
- **Accessibility:** WCAG 2.2 AA per-increment to enumerated surfaces; non-color status; EN + FR.

### Infrastructure & Deployment

- **Composition (platform/local shim):** .NET Aspire 13.5.3 local AppHost shim; Dapr components (`statestore` for EventStore
  actor/status/archive/checkpoint state, `chatbot-statestore` for ChatBot read models and coarse idempotency,
  `chatbot-pubsub` for Redis pub/sub, plus the ChatBot workflow state store for hosted saga coordination);
  production deny-by-default `accesscontrol.yaml`; local mTLS-off `accesscontrol.local.yaml`; canonical Story 2.9 binds
  correction propagation to hosted Dapr Workflow in the live topology while preserving EventStore as lifecycle truth.
- **Runtime controls and workload fairness (D13):** one durable, versioned current control/rate-limit projection is the
  admission source for service clients, AI actors, command capabilities, mailbox sources, and outbound channels; gateway
  and workers consume the same view. Stale/unavailable/malformed/unknown control fails closed, with no production
  `AlwaysActive`/`AlwaysUnlimited` fallback. Work partitions by tenant then mailbox/Project/workflow and uses weighted
  deficit round robin plus policy quotas/circuit breakers. Bounded renewable leases return expired work safely; poison
  items enter the tenant-partitioned Retry Profile dead letter without starving other partitions. One
  `OperationsControlWorker`, owned by `operations-admin`, performs periodic enforcement/notification/escalation and
  publishes tenant-safe dependency health, control freshness, retry/dead-letter, queue-age, and audit-lag state. Missing
  live sources report `unmeasurable|unsupported`; numeric thresholds remain qualified by the applicable A11-M1 or
  A11-M2 record, and protected state remains A6-qualified.
- **Canonical audit topology:** hash-linked envelopes live in the same aggregate command stream and atomic batch as
  the mutation/idempotency/policy/approval facts. A separate signed per-tenant checkpoint anchors aggregate heads;
  investigation projections or archival stores remain rebuildable derivatives. Key custody, retention, erasure,
  backup propagation, and surviving metadata are target concerns under open A6, not proven implementation choices.
- **Correction propagation (FR91a):** the aggregate owns the exact `Correcting | CorrectionDelayed | Corrected`
  lifecycle (`Apply(AssociationCorrectionStarted/AssociationCorrectionDelayed/AssociationCorrected)`). Hosted Dapr
  Workflow coordinates start, acknowledge, complete, delay, and vector-reindex activities through existing EventStore
  writer/activity seams. Correction start atomically freezes the complete impact manifest: every ChatBot-derived store;
  every affected Conversations/Folders record and index; approved/executed AI actions; appended messages; task-intent
  conversions; sent mail; external/tool effects; file disclosures; and every required irreversible-effect disposition.
  Each item records an authenticated owner repair/rebuild acknowledgement or explicit `contained`,
  `compensation-required`, or `cannot-repair` disposition. Reads block every affected source/destination AI context until
  all items complete and the workflow reaches `Corrected`. `ReindexVectors(tenantId, correctionId, sourceVersion)` remains
  an M2 activity and must be idempotent and version-guarded. Propagation p95 is `<=10 minutes` in M0/M1 and `<=60 minutes`
  in M2; a missed SLO transitions to `CorrectionDelayed`, exposes owner/next safe action, and triggers P2. `Proposed` is
  not an association state.
- **Deploy / recovery:** SDK-container images; Aspire K8s/AKS + Helm is M2 target scope. A10 remains provisional:
  the accepted Epic 12 contract is `activation: pending`; its diagnostic and completion artifacts have no A10
  authority. Provisional targets are RPO `<=15 minutes` and RTO `<=4 hours` for source email records, attachments,
  approvals, commands, policy snapshots, and audit records, plus projection rebuild `<=4 hours` from immutable sources
  without mailbox re-ingestion. The 2026-08-27 hosted bundle expired on 2026-09-04 under the current eight-day policy,
  predates the controlled-loss job, lacks its RPO evidence, and has only a 180-second ceiling. M2 requires a separate
  fresh hosted four-job controlled-loss bundle bound to exact candidate, evidence-policy version, locator, producer,
  timestamps/freshness, persisted loss bounds, RTO duration, cleanup, independent validation, and stable failure reason,
  plus an RTO-capable full-window or retained production-shaped drill.
- **Replay isolation:** M2 uses a replay-only composition root with no production credentials/locators, replaces
  mail/model/tool/command/file/state/queue/outbound adapters, denies undeclared egress, stamps `replay_run_id`,
  and excludes replay from production audit completeness. A separate gate-owned read-only verifier captures a
  `ReplayInvarianceManifest v1` before replay and after termination; the candidate enumerates every protected production
  store/resource ledger, and each row binds candidate, opaque resource ID, provider revision/snapshot token, and SHA-256
  canonical metadata/state digest. Volatile timestamp/telemetry/lease fields form an explicit versioned exclusion list.
  Replay starts only after a complete pre-manifest and passes only on exact inventory/row equality; missing, unreadable,
  added, or changed resources fail. A composition, egress, verifier, or invariance failure is a stop-ship condition.
- **Observability and A11-M1:** OpenTelemetry emission is always on. Before M1 exit, one independently machine-readable
  record freezes and evidences the definitions, denominators, supported-request mix, provisional targets, minimum
  samples/windows, evidence sources, owners, and pass/fail rules for SM8, SM16, SM-C3, and SM-C5, and records SM12 and
  SM15 in the same bundle. Missing, stale, partial, mismatched, historical, unverifiable, or failed evidence blocks M1.
- **Observability and A11-M2:** The addendum's metric targets are normative planning values and drift-tested against the
  code catalog, but are not supported/publishable SLOs until each exact M2 candidate has stable metric name, numeric
  target/unit, window, error budget, alert threshold, timestamped calibration source, tenant scope, live
  signal/provenance, accountable route/receiver, and passing burn-test result/date/immutable locator. The qualification
  table pairs one-to-one by metric name. Missing, stale, partial, failing, unverifiable, historical, or mismatched data
  yields `unsupported`; dashboards expose `within-budget|approaching|exhausted|unsupported`. A11-M2 also requires the
  2–4 week pilot baseline, SM8-SM14/SM16 recalibration, and the frozen SM-C5 supported-request mix; all current SLO rows
  remain unsupported with no candidate. A11-M1 and A11-M2 are independent machine-readable decisions; neither
  substitutes for the other.

### Host-Layer Reuse (D8 — Technical Enabler TE-1)

- **Decision:** ChatBot is an EventStore **domain module** hosted on the `Hexalith.EventStore.DomainService` SDK. The
  target state uses an approximately two-line host: `AddEventStoreDomainService()` plus admission-chain registration,
  followed by `UseEventStoreDomainService()`. Use `IDomainQueryHandler` for queries, `IDomainProjectionHandler` for
  projections, `IReadModelStore` + `ReadModelWritePolicy` for read models, and
  `IQueryCursorCodec`/`QueryCursorScope` for cursors. Use
  `AddEventStoreDomainTelemetry`/`AddEventStoreDomainStateStoreHealthCheck` for telemetry and health. Compose the module
  from the platform AppHost by using `AddEventStoreDomainModule(...)`, as `tenants` and `sample` are composed today.
- **FR81a preserved:** the CommandGateway admission layer mounts as the SDK's **pre-commit admission hook** (EventStore platform prerequisite TE-1.2) — same stage order, same `internal` governance interfaces, same "NOT a second pipeline" invariant, now enforced at the platform seam.
- **Implementation state after TE-1:** `Program.cs` uses the SDK host shape (`AddEventStoreDomainService(...)`, admission-stage registration, `UseEventStoreDomainService()`), public compatibility routes live outside `Program.cs`, custom `/process` plumbing is removed, queries/projections/read models/cursors/telemetry/health use SDK contracts, and standalone ChatBot `.Aspire`/`.ServiceDefaults` projects are retired.
- **Retained exception:** `src/Hexalith.ChatBot.AppHost` remains as a thin local-development umbrella for EventStore, Tenants, ChatBot Server, ChatBot UI, Keycloak, and Dapr sidecars. Its internal Dapr wiring preserves `chatbot-statestore`, `chatbot-workflow-statestore`, and `chatbot-pubsub` because the current `AddEventStoreDomainModule(...)` API does not yet model those dedicated resources. This is not a production domain-hosting bypass.
- **Deployment boundary:** the DataProtection-backed admission marker and query cursor key ring use `SetApplicationName("Hexalith.ChatBot")`; production deployments must configure `ChatBot:DataProtection:KeyRingPath` or explicitly set `ChatBot:DataProtection:SingleReplicaOnly=true`.
- **Mechanical enforcement:** NetArchTest anti-regrowth rules (no inline query mapping in the Server host, no per-domain telemetry/health classes, no hand-rolled host wiring beyond SDK calls + admission registration) are owned by TE-1.5.
- **ADR:** accepted at [`docs/adrs/domainservice-sdk-host-adoption.md`](../../docs/adrs/domainservice-sdk-host-adoption.md). It governs TE-1.2–TE-1.7 and records the selected exception boundary: the retained local-development umbrella AppHost required for dedicated ChatBot Dapr resources, never a production domain-hosting bypass. Tracking: [`technical-enablers.md`](technical-enablers.md).

### Internal Decomposition (modular monolith — D5)

ChatBot is one deployable service with hard internal seams organized by derived-state lifecycle, separate assemblies,
and event-only communication across seams (extraction-ready if M2 scale demands):
- **Association** — candidate generation, deterministic scoring, evidence snapshots, association lifecycle.
- **Governance/Mediation** — risk classifier (tag+heuristic, no AI dependency), six risky action classes,
  AI-action proposals, approval records, command allowlist.
- **Lifecycle/Workflow** — workflow-instance maps, lifecycle state machine, coordinator/activity seams, and future
  Dapr Workflow runtime binding.
- **Projection/Query** — projections, queue projections, SignalR nudge, FrontComposer read models.
- **Audit/Replay** — WORM hash chain, replay traces (near-platform concern).
Seam test: *owns an aggregate with its own invariants, or just a folder?*

### Governed AI Mediation

- **Task intent:** `TaskIntentDetector` is versioned independently from association and risk. It returns exactly
  `informational|request-information|request-action|request-decision` plus confidence, evidence offsets, and version;
  `actionable` is a derived grouping. Missing/invalid/unqualified artifacts return `detector-unavailable`, require
  authorized review, and do not invoke the risk classifier. Its separate A9a partitions must reach precision/recall
  `>=80%/75%` at M0 and `>=90%/85%` at M1.
- **Risk classifier:** `ActionRiskClassifier` is a versioned categorical tag/heuristic contract with
  only two successful determinate classes, `low-risk|approval-required`; `denied|unsupported` occur before classification. State mutation, file disclosure,
  outbound send, task creation/assignment, external-tool invocation, and acting on behalf of a participant are
  structurally non-downgradable. Only product-declared read-only/no-external-effect subtypes can be low risk.
  A missing, invalid, unqualified, failed, or non-contract artifact/output, missing tag, unknown effect surface, or
  undeclared authority class returns the typed product result `classifier-indeterminate`. It creates no proposal,
  durable domain state, durable idempotency state, approval action, or effect; only a separately typed redacted, non-mutating
  auditable attempt with safe remediation/escalation is retained. `classifier-unavailable` may appear only as a
  non-canonical availability reason attached to that result. Remediation creates a new linked operation that must classify
  determinately; the former attempt cannot be resumed, reinterpreted, or approved. The CommandGateway/auditable-attempt
  seam owns the attempt and successor link: the attempt records original `operation_id`, correlation, classifier
  version/input tuple, redacted reason, and remediation; the successor uses a fresh `operation_id` and immutable
  predecessor reference. The attempt ledger is the only durable record and can deny reuse of the original identity but
  never authorize continuation or serve as domain idempotency state. Optional M1 explanations cannot alter
  the result. Each reviewer disagreement or reclassification records the
  classifier version, input tuple, original class, reviewer or product decision, and resolution; quality limits are
  `<=1%` evaluation misclassification and `<=2%` sampled-production disagreement, independent of audit completeness.
- **Execution:** approved actions execute only through allowlisted EventStore commands (M0 allowlist =
  `Project.AppendConversationMessage`). This is a stable product ID mapping to Conversations
  `AppendMessageCommand` v1 / `MessageAppended`, not a Project-owned or currently executable guarantee.
  Conversations owns the append and conversation-to-Project assignment. A13 blocks the mapping until producer,
  authority, atomic-audit, concurrency, and lifetime duplicate evidence is owner-accepted and contract-tested.
- **Allowlists:** the public operation catalog, per-surface exposure, MCP tags, AI allowlist, and owner targets are
  distinct deny-by-default artifacts. M0 AI membership is exactly `Project.AppendConversationMessage`; M1 adds
  exactly `ChatBot.ExecuteLowRiskAssistance`. Outbound sends, identity/role/policy/allowlist mutation, grants,
  administration, destructive file operations, and unrestricted downstream commands are never AI-invocable.
  Versions are immutable; tenants may pin or disable only and cannot add members or weaken approval. M0 membership
  change requires PRD + memlog + dataset revalidation; M1 requires Security sign-off; M2 additionally requires the A9a
  command-coverage run.
- **A9a qualification:** separate versioned partitions cover association, task intent, and action risk, with at
  least 500 messages at M0, 2,000 at M1, and 20 new adversarial examples per cycle. Association release evidence
  targets 95% precision, 90% recall, and zero critical unauthorized false positives. A9a does not close A11-M1 or A11-M2.

### Implementation Sequence and Integration Flow

**Remediation and qualification sequence (architecture-level; respects M0→M1→M2):**
1. Keep the Contract Spine and typed Client aligned to the complete PRD operation/query catalog and family state models.
2. Establish the one supported atomic EventStore write path and A13 ownership for event + terminal idempotency +
   policy/approval references + canonical hash-linked envelope; prove ACL/fencing/fork/reorder/rebuild behavior.
3. Implement command-created M0 governance bootstrap with real deterministic classification, approval, policy,
   service-client, and admin-role controls; no pilot-eligible stubs or direct seeding.
4. Close A6 before applicable record persistence and A5 before live AI invocation; preserve non-AI workflows during
   provider outage.
5. Accept and prove the Conversations append/assignment and owner-authority mappings under A13, then qualify the
   M0 vertical loop and first-store isolation without inferring readiness from local metadata preparation.
6. Extend the singular parity set and full M1 governance only after M0 passes; run exact lifecycle/retry/authorization/
   audit conformance across all surface origins.
7. Close A11-M1 with the frozen, evidenced M1 measurement bundle before M1 exit. After M1 passes, activate and
   independently verify the recovery-evidence architecture, then close A10 and A11-M2 with fresh exact-candidate
   operational evidence before any M2 production/release-candidate claim.

**Cross-component dependencies:** the CommandGateway is the spine everything routes through; the Contract
Spine constrains all three surfaces; event-driven projections depend on sibling event contracts (Pact tests);
correction propagation spans Association + Lifecycle/Workflow + Projection + Audit; the **safety floor**
(tenant isolation, authorization, fail-closed gate, audit-of-the-command, the gateway spine) must not ride
inside any trimmable stage (redaction depth, approval-policy richness, dashboards) — a dependency map must
prove this.

**Integration flow:** adapters submit typed commands through the gateway to the A13-gated EventStore actor target;
the atomic event/idempotency/policy/audit commit then feeds publications, projections, coordinator activities, and
metadata-only SignalR nudges followed by authorized re-query. Keycloak supplies OIDC identity; M365/Exchange Graph,
AI providers, sibling contexts, and the M2 Memories capability remain behind ChatBot-owned adapters.

**Target M0 vertical path (gate-blocked):** mailbox intake → deterministic association evidence → S2 human confirm →
gateway command → Folders attachment reference → project-conversation projection → governed S3 AI-action approval →
`Project.AppendConversationMessage` mapping → A13-approved Conversations execution → atomic commit → projection →
SignalR nudge → authorized UI re-query. Metadata-only command preparation is not owner execution; A5, A6, and A13
must close, and the exact A9a M0 detector/classifier artifacts must be `approved-current` before their first use, before
this path supports the M0 permitted claim.

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
(`Ulid.TryParse`, never `Guid`); EventStore identity `{tenant}:chatbot:{aggregateId}`; Dapr app ID `chatbot`,
EventStore actor/status store `statestore`, ChatBot derived state store `chatbot-statestore`, pub/sub component
`chatbot-pubsub`, topic `chatbot.events`, deadletter `deadletter.chatbot.events`; kebab-case for
convention-derived resource names.

**[ChatBot] Lifecycle vocabulary:** state enums are family-specific and come from the PRD Shared Workflow
Contract. Association uses exactly `Received | Associated | Rejected | Deferred | NeedsReview | Failed | Skipped |
Correcting | CorrectionDelayed | Corrected`; `Proposed` is not an association state. Other families retain their own closed enums;
builders must not create a universal workflow enum or synonyms. Health and evidence states are separate vocabularies.

### Structure Patterns

**[inherited] Module boundaries & dependency direction:** Contracts (low-dep) ← Client ← Server; CLI/MCP/UI
depend **only** on Client; Aspire/AppHost/ServiceDefaults at edges; Testing references Server+Contracts.
Tests in `tests/Hexalith.ChatBot.{Area}.Tests` mirroring source; never inline or locally override package
versions. The consumer `Directory.Packages.props` is a version-free wrapper over the shared Builds catalog.

**[ChatBot] Module-internal seams (D5):** source organized by derived-state lifecycle module —
`Association/`, `Governance/` (mediation+approval), `Lifecycle/` (workflow), `Projections/`, `Audit/` — not
broad type buckets. Cross-seam communication is events-only; no cross-module method calls into another seam's
internals. Governance interfaces (`IRiskClassifier`, `IApprovalGate`, `IAuditWriter`, `IIdempotencyStore`)
are `internal` to `.Server`.

**[ChatBot] Sibling integration:** every sibling client is wrapped behind a ChatBot-owned adapter
(`IProjectDirectory` over Projects, `IParticipantDirectory` over Parties, `IFolderStore` over Folders,
`IConversationWriter` over Conversations). **Never call a sibling client from aggregate `Handle` logic.**
Store stable IDs (`ProjectId`/`PartyId`/`FolderId`/`ConversationId`) in events — **never upstream PII**.

### Format Patterns

**[ChatBot] Problem/error responses (Folders pattern, RFC 9457, metadata-only):** `{ category, code, message,
correlationId, taskId?, retryable, clientAction, details.visibility }`. User-safe text is drawn from a **versioned
message catalog** (FR77): stable code + headline ≤80 chars + one-sentence reason that names no unauthorized
project/file/party/audit detail. **Raw error text leaking to a user = release-blocking defect (NFR40).**

**[ChatBot] Classifier result contract:** `classifier-indeterminate` is the canonical product result for every
indeterminate risk-classifier case. Its response carries no proposal/approval identity, durable domain state, or durable
idempotency state; it may carry a redacted safe reason such as `classifier-unavailable`, remediation guidance, and the predecessor
operation identity needed to submit a new linked operation. The ChatBot CommandGateway/auditable-attempt seam owns the
original attempt identity and successor link. The original operation is terminal and never approvable; a successor uses a
fresh `operation_id` plus an immutable predecessor reference and is independently classified.

**[ChatBot] Derived-record shape (every derived class):** carries `tenantId`, `sourceProvenance`,
`derivationContractVersion`, `redactionState`, `retentionClass`, `schemaVersion`; scorer/detector/classifier or
model version is added where applicable. Decision snapshots are
append-only + superseded (never mutated); live mirrors are version-stamped projections.

**[ChatBot] Evidence & confidence capture (cross-cutting #12 — association/task-intent only):** Association candidates
and task-intent results carry `confidenceScore` ∈ `[0,1]`, `thresholdBand` (`auto|ambiguous|fail-closed`),
`evidenceRefs[]` (typed signal class + matched value), `kernelVersion`, `detectedAt`, and (after human action)
`correctionOutcome`. ActionRiskClassifier results are categorical and carry class, classifier version, and input tuple;
they never use a numeric confidence value to authorize, downgrade, or recover a result.

**[inherited] Data formats:** JSON camelCase; `System.Text.Json` only (shared options factory, never inline
`new JsonSerializerOptions()`); `DateTimeOffset` UTC server-side, `{Action}At` naming, tenant-local only at
presentation; cursor pagination `{ items, cursor, hasMore }` (never offset/limit); ETag/`If-None-Match`→304.

### Communication and Process Enforcement

The detailed rules remain authoritative under D3, D4, and D9. Builders apply these mechanics:

| Decision | Implementation-facing requirement |
|---|---|
| D3 — one command spine | Every UI, CLI, MCP, service-client, AI-actor, worker, and mailbox mutation builds a typed `IChatBotCommand` and calls `IChatBotClient.SubmitAsync`; adapters never duplicate a gateway stage. |
| D3 — classifier containment | Only determinate `low-risk` or `approval-required` results may continue. `classifier-indeterminate` terminates before proposal, durable domain state, durable idempotency state, approval action, or effect; remediation submits a new linked operation. |
| D4 — atomic durability | Construct the canonical envelope with tenant, actor, command, stable operation/decision identity, revision, origin, transition, policy, evidence, redaction, outcome, and chain fields. Co-commit it with the event, lifetime terminal idempotency result, and policy/approval references. |
| D4 — auditable attempts | Security-sensitive non-mutating denials, restricted reads, service-client failures, and every tenant-admin dashboard read use the separately measured auditable-attempt path. It never repairs missing mutation audit. |
| D9 — lifecycle and retry | Validate the exact family row and Retry Profile v1. The row selects in-place change, immutable successor, linked workflow/attempt, or stored-outcome replay; projections and dead letters cannot authorize command re-execution. |

Propagate `correlationId` through commands, events, metadata-only logs/traces, workers, and sibling calls. Normalize
idempotency input before hashing; treat expected revision as a separate guard. Persist before publish. Because Dapr
pub/sub is at-least-once and unordered, projection handlers are idempotent, order-tolerant, and source-versioned;
SignalR remains an advisory re-query nudge. Unresolved tenant or current-owner authority, invalid policy, approval,
classifier, identity, or revision, an unsupported owner contract, or unavailable canonical-audit durability fails
closed with the normative typed result and no authoritative domain or idempotency write.

**[ChatBot] Correction propagation (FR91a):** the ChatBot correction aggregate solely owns immutable manifest membership
and `Correcting | CorrectionDelayed | Corrected` lifecycle; owner contexts retain sole authority over their records and
effects. Each frozen item is stably identified by correction ID, owner context, owner resource/effect ID, source version or
effect digest, and required outcome. Each A13 mapping names the only owner adapter/actor authorized to submit the item's
acknowledgement or disposition; the coordinator and projections cannot self-acknowledge, add, replace, or omit an item.
The aggregate applies lifecycle changes via
`Apply(AssociationCorrectionStarted/AssociationCorrectionDelayed/AssociationCorrected)`. The coordinator records an
authenticated `AssociationCorrectionImpactAcknowledged` for every frozen manifest item, including ChatBot stores,
affected Conversations/Folders records and indexes, actions, appended messages, task-intent conversions, sent mail,
external/tool effects, file disclosures, and irreversible-effect dispositions. Reads block all affected
source/destination AI context until every item completes and the workflow reaches `Corrected`. Runtime implementation
status is evidence input, not an architecture readiness claim.

**[inherited] Domain correctness:** never throw for business-rule violations (return
`DomainResult.Rejection([...])` — exceptions bypass the idempotency cache); aggregate `Handle` is pure
(no I/O, Dapr, `await`, or authorization); authorization and orchestration stay outside aggregate logic; backward-compatible
deserialization for every event ever produced (no `V2` types — additive + upcasting).

### Enforcement Guidelines

**All AI agents must:**
- Route every state mutation through the CommandGateway; adapters construct only `IChatBotCommand`.
- Use the exact family-specific lifecycle strings, stable operation IDs, decision slots, and reason codes.
- Stamp every derived record with tenant/provenance/derivation-contract/redaction/retention/schema versions.
- Keep `tenantId` from authenticated claims; fail closed on unresolved tenant/authz/audit.
- Never seed M0 governance state; use the bootstrap commands and two distinct current Tenants owners.
- Atomically persist the event, terminal lifetime outcome, policy/approval references, and canonical audit envelope.
- Apply exact family lifecycle, concurrency, successor, and Retry Profile v1 semantics.
- Treat `Project.AppendConversationMessage` as an A13-blocked Conversations mapping, not an executable Project command.
- Keep diagnostic, story-completion, and A10 operational recovery evidence in disjoint authority channels.
- Write tests in the same change: Tier 1 pure aggregate/Handle; cross-tenant isolation negative tests;
  fail-closed parametrized from the path enumeration; idempotency replay/conflict.

**Pattern enforcement (mechanical, not review-by-eyeball):**
- **NetArchTest**: no `*.Cli`/`*.Mcp`/`*.UI` type references `IRiskClassifier|IApprovalGate|IAuditWriter|
  IIdempotencyStore`; dependency-direction edges; aggregates only in `.Server`.
- **Conformance tests**: real-aggregate vs in-memory event-sequence equality.
- **Differential-conformance harness**: same semantic intent across UI/CLI/MCP → identical event sequence +
  state-store end-state (including rejection and retry intents). M0 shim coverage has been superseded by Epic 5
  production-adapter arms for UI/API, CLI, and MCP.
- **Cross-tenant isolation**: zero-leak negative tests across 9 actor types, including cursors and error bodies.
- **Tier 2/3 inspect state-store end-state**, never just HTTP/exit codes.

### Pattern Examples

**Good:**
- `MarcConfirmsAssociation` (UI) and the CLI `chatbot associate` both build
  `ConfirmEmailProjectAssociation` and call `IChatBotClient.SubmitAsync` → identical `AssociationConfirmed` event.
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
├── global.json                                 # repository SDK pin 10.0.400, rollForward latestPatch
├── Directory.Build.props                       # net10.0, nullable, warnings-as-errors, Allman
├── Directory.Packages.props                    # version-free wrapper over the shared Builds catalog
├── Directory.Build.targets                     # SDK-container opt-in
├── .editorconfig  .gitignore  nuget.config  README.md  CHANGELOG.md
├── .gitmodules                                 # root-declared Hexalith submodules under references/ only
├── .github/workflows/                          # ci.yml, release.yml (semantic-release)
├── references/
│   ├── Hexalith.Builds/                        # sole package-version catalog and shared build policy
│   └── Hexalith.EventStore/                    # [M0] root-declared git submodule — foundation
├── docs/
│   ├── adrs/                                    # idempotency, schema-evolution, atomic-audit, gateway, saga
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
│   │   │   └── Stages/                          #   Auth, TenantBind, Authorize, Risk/Approval, OperationIdentity,
│   │   │                                        #   RevisionGuard, CanonicalEnvelope — internal interfaces
│   │   ├── Association/                         # [M0] seam: Aggregates/, Scoring/ (deterministic kernel,
│   │   │                                        #   T_high/T_low), Evidence/, Validators/
│   │   ├── Governance/                          # [M0] seam: RiskClassifier/ (tag+heuristic), Approval/,
│   │   │                                        #   AiMediation/, Allowlist/, Aggregates/  ; Outbound/ [M1]
│   │   ├── Lifecycle/                           # [M0] family state models + coordinator/activity seams;
│   │   │                                        #   runtime status is evidence, never inferred from architecture
│   │   ├── Projections/                         # [M0] seam: read models, queue projections, live mirrors
│   │   ├── Audit/                               # [M0] domain audit facts + investigation projection;
│   │   │                                        # canonical envelope/atomic stream boundary is A13-platform-owned
│   │   ├── Adapters/                            # ports over siblings + external providers
│   │   │   ├── Projects/  Parties/  Folders/  Conversations/   # [M0] IProjectDirectory, IParticipantDirectory…
│   │   │   ├── Mailbox/                         # [M0] M365/Graph ingestion port (one mailbox pattern)
│   │   │   └── AiProvider/                      # [M0] scoped-context AI port
│   │   └── Registration/
│   ├── Hexalith.ChatBot.AppHost/              # [M0] ADR-scoped local-dev Aspire umbrella — DAPR topology (statestore,
│   │                                            #   chatbot-statestore, chatbot-pubsub, accesscontrol.yaml,
│   │                                            #   accesscontrol.local.yaml), sibling app refs
│   ├── Hexalith.ChatBot.UI/                   # [M0] Blazor + FrontComposer: S1 conversation, S2 association
│   │                                            #   review, S3 AI approval; [M1] S4–S7; [M2] S8–S10
│   ├── Hexalith.ChatBot.Cli/                  # [M1] System.CommandLine, wraps Client (no DAPR, no stages)
│   ├── Hexalith.ChatBot.Mcp/                  # [M1] ModelContextProtocol stdio server, wraps Client
│   ├── Hexalith.ChatBot.Workers/             # [M0] mailbox-ingestion + retry; [M2] projection rebuild, replay
│   └── Hexalith.ChatBot.Testing/             # [M0] fakes/builders, InMemoryChatBotService, command helpers
└── tests/
    ├── Hexalith.ChatBot.Contracts.Tests/      # [M0] Tier1: naming, serialization round-trip, message-catalog
    ├── Hexalith.ChatBot.Server.Tests/         # [M0] Tier1/2: aggregates, gateway stages, fail-closed table,
    │                                            #   isolation, idempotency (state-store end-state asserts)
    ├── Hexalith.ChatBot.Architecture.Tests/   # [M0] NetArchTest: dep-direction, adapter-cannot-replicate-stage
    ├── Hexalith.ChatBot.Conformance.Tests/    # [M0/M1] differential-conformance harness + parity oracle;
    │                                            #   Epic 5 drives UI/API, CLI, and MCP production adapter arms
    ├── Hexalith.ChatBot.IntegrationTests/     # [M0] Tier3: Aspire E2E, cross-tenant isolation (9 actors)
    ├── fixtures/                               # A9a evaluation partition, redaction/leakage corpus, oracle rows
    └── e2e/                                    # [M0] Playwright — S1/S2/S3 (axe-core a11y); grows per increment
```

### Architectural Boundaries

**API boundaries:** REST, UI, CLI, MCP, service SDKs, AI mediation, workers, and mailbox handlers are adapters over
the same typed command/query contracts. No adapter uses direct DB, queue, mailbox-store, index, actor-state, or
projection writes. Every mutation enters the **CommandGateway** and required A13-gated EventStore actor target; routes and host
plumbing are code-owned seed, not alternate authority.

**Component boundaries (modular-monolith seams):** Association ↔ Governance ↔ Lifecycle ↔ Projections ↔ Audit
communicate **events-only** across seams; no cross-seam reach into internals. Governance stage interfaces
(`IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore`) are `internal` to `.Server`
(NetArchTest-enforced). UI/CLI/MCP ↔ Server only through `IChatBotClient`.

**Service boundaries (sibling contexts):** ChatBot uses only owner-accepted public contracts through local ports and
consumes owner events to build derived state. Multi-context operations use coordinator/activity seams. Keycloak is
the identity boundary; M365/Graph is the mailbox boundary (degraded per mailbox, no tenant-wide fallback); the AI
provider is a mediation boundary and non-AI workflows survive its outage. Revision/hash compatibility does not
equal producer acceptance or close A13.

**Data boundaries:** EventStore is the write-side source of truth for ChatBot aggregates and the proposed target owner
of the atomic canonical mutation-envelope contract, pending A13 acceptance or an approved transactional alternative.
ChatBot derived state lives in a tenant-
partitioned store via projections; vector/embedding uses optional Memories at M2. Investigation/audit views may use
separate stores but are never canonical mutation evidence. Every first-use store proves below-application isolation;
A6 owns protection, retention, erasure, backup, and key-custody qualification.

### Cross-Context Ownership Contract

| Context | Sole source-of-truth ownership | ChatBot boundary / gate |
|---|---|---|
| ChatBot | Association decisions, AI-mediated workflows, assistant interactions, policy/approval snapshots, lifecycle, queues, and other PRD-enumerated derived records | Orchestrates owners; never absorbs their source records |
| Projects | Project identity, lifecycle, membership/access, resource authorization | Current owner grant required; a ChatBot role or Project ID is not authority |
| Conversations | Conversation identity, messages, append/history, conversation-to-Project assignment/reassignment | `Project.AppendConversationMessage` is a legacy product ID mapping to Conversations; executable path is A13-blocked |
| Parties | Internal/external Party identity resolution | Store stable Party IDs; current owner evidence for trust-bearing use; A6 covers regulated PII runtime proof |
| Folders | Governed folders, attachments, file access control, file metadata | Use opaque tenant-scoped IDs and owner authorization; A6 covers runtime protection proof |
| Tenants | Tenant facts, boundaries, membership, tenant-policy and authorization context | ChatBot owns its application role/policy snapshots; owner mapping is A13-blocked |
| EventStore | Current aggregate-local command/event-batch durability | Proposed atomic audit/idempotency/policy co-commit owner (or approved transactional alternative), supported path, ACLs, and fencing require A13 acceptance |
| FrontComposer | Governed UI shell and progress transport contract | ChatBot owns composer lifecycle and authorization; nudges are advisory and require re-query |
| Memories | Optional AI memory/vector/graph capability | Post-MVP/M2 only; never expands M0/M1 authority and requires first-store isolation |
| Commons | Shared tenant-access evaluation mechanism and primitives | ChatBot retains closed application permissions and current owner checks |

`IdentityEvolved` remains an unaccepted external proposal. The System Architect opens a source re-check within five
business days when any consumed command/event schema, authorization or identifier semantic, integration topology,
or referenced RBAC rule changes. The outcome is appended to the architecture memlog and refreshes the manifest;
missing/inaccessible sources are blockers, and later working-tree content is never silently consumed.

### Requirements → Structure Mapping

| FR group | Lives in |
|---|---|
| FR1–FR12 Email intake & association | `Server/Association/` + `Adapters/Mailbox/` + UI `S2` |
| FR13–FR20 Participants/identity/authz | `Adapters/Parties/` + `Gateway/Stages/{Authorize,TenantBind}` |
| FR21–FR28 Conversation & context | `Projections/` + `Contracts/Queries/ProjectConversation*` + UI `S1` |
| FR29–FR34 Files & attachments | `Adapters/Folders/` + `Server/Association/` (attachment lifecycle) |
| FR35–FR46 Task intent & AI mediation | `Server/Governance/{AiMediation,RiskClassifier,Approval,Allowlist}` + UI `S3` |
| FR48a–FR48d Inbound authenticity | `Adapters/Mailbox/` + intake admission/evidence **[M0]** |
| FR47–FR50 Outbound authority + draft/send | `Server/Governance/Outbound/` + `Adapters/Mailbox/` **[M1]** |
| FR51–FR63, FR75a–g Admin/governance/audit | `Server/Audit/` + `Projections/` (queues) + UI `S5/S8–S10` |
| FR64–FR80 Reliability/ops/queues | `Workers/` + `Projections/` + `Lifecycle/StateModel/` |
| FR81–FR96 Parity, state model, replay | `Gateway/` + `Contracts/openapi/` + `Conformance.Tests/` + `Lifecycle/` |

**Cross-cutting locations:** tenant isolation → `Gateway/Stages/TenantBind` + every store key; fail-closed →
`Gateway/Stages/AuditPre` (single seam); audit → `Server/Audit/`; correlation → Server middleware + envelope;
redaction → swappable stage in `Gateway` + `Contracts/Messages/`; evidence/confidence → `Association/Evidence/`
+ `Governance/AiMediation/`; derived-state versioning → `Projections/` (schema-versioned) + `docs/adrs/`.

### File Organization Patterns

- **Configuration:** root-level `global.json`/`Directory.Build.props`/version-free
  `Directory.Packages.props`/`.editorconfig`; the package catalog is
  `references/Hexalith.Builds/Props/Directory.Packages.props`; Dapr components under the local AppHost shim;
  Contract Spine under `Contracts/openapi/`.
- **Source:** by seam (lifecycle module), not type bucket; one type per file; `.g.cs`/`Generated/` never hand-edited.
- **Tests:** mirror source boundaries; dedicated `Architecture` + `Conformance` projects; shared `fixtures/`
  (no per-project corpus forks); `e2e/` Playwright with `data-testid`/role selectors.
- **Assets/docs:** ADRs + contract + exit-criteria docs under `docs/`.

### Development Workflow Integration

- **Dev server:** `aspire run` brings up ChatBot + Dapr sidecar, EventStore + Tenants sidecars, the UI surface
  without a Dapr sidecar, and Keycloak with the tenant-claim realm import. Local self-hosted Dapr runs mTLS-off
  and therefore loads `accesscontrol.local.yaml`; production keeps deny-by-default `accesscontrol.yaml` under
  mTLS/Sentry. AppHost edits require Aspire restart.
- **Build:** `dotnet build Hexalith.ChatBot.slnx`; shared Builds-owned package versions, exclusive-authority
  validation, and warnings-as-errors gate.
- **Deploy:** SDK-produced Server/UI runtime images per packable host; Aspire 13.5.3 K8s/AKS + Helm publish target
  [M2], with resolved ASP.NET runtime image digest and shared DataProtection key ring (or explicit single-replica guard)
  bound in candidate evidence; `semantic-release` on merge to `main`.

## Delivery and Evidence Integrity

- **Shared ownership:** every Hexalith domain module uses the reusable `Hexalith.Builds` domain CI and release
  workflows. Module callers contain only triggers, least-privilege permissions, concurrency, explicit secret
  mappings, and module-specific inputs; they do not duplicate standard build, test, or release mechanics.
- **Dependency modes:** local development uses root-declared project references where available. CI and release
  use published NuGet dependencies across repository boundaries, build in Release with warnings as errors and
  enabled NuGet auditing, and execute test projects individually.
- **Non-vacuous gates:** required Aspire/Dapr topology and browser tiers must execute their named tests. Missing,
  zero-test, self-skipped, or all-skipped evidence fails the lane; uploaded results do not substitute for passing
  execution.
- **Story status:** CI detects each story or sprint-ledger transition to `done` and requires a matching TE-2 evidence
  contract plus a passing `story-evidence-integrity` report. The File List and scoped change sets must reconcile,
  result provenance must match the tested implementation digest, mandatory tests must be non-vacuous, required
  primary paths must be executed, and every checked task or acceptance item must have current evidence. Root-declared
  submodule changes require both the submodule diff and superproject gitlink; nested submodules are never initialized.
- **Completion authority:** the versioned policy fixes PR and push base/head selection, verifies the exact event head,
  and makes a failed result-producing job fail the always-running gate. It admits current-digest machine results or an
  approved retained exact-digest artifact within policy age; narrative summaries, screenshots, diagnostic fallbacks,
  and unrelated aggregate suites cannot replace required primary-path evidence.
- **Recovery diagnostics:** `recovery-primary-diagnostics` is metadata-only and has no completion or A10 authority.
- **Recovery completion:** `recovery-primary` admits exactly one transition-declared current-run producer bound to the
  exact candidate and policy. A side-effect-free planner validates scope before the isolated destructive lane. The
  accepted tool target is Dapr CLI/runtime `1.18.2/1.18.4`, with the CLI archive SHA-256 fixed by the policy. Raw live
  TRX stays outside retention; the authority channel contains only the aggregate cleanup receipt, canonical sanitized
  result, allowed independently validated reports, and provenance sidecars. Planning, production, timeout/no-test,
  restoration, cleanup, projection, attestation, validation, or publication failure fails completion.
- **A10 operational evidence:** scheduled/release controlled-loss and full-window bundles use a disjoint
  retention/freshness channel. They can inform A10 but cannot satisfy story completion.
- **Activation:** the Epic 12 architecture remains `activation: pending` until its unique pull-request check identity,
  cleanup receipt, closeout deadlines, immutable tools, and fresh-runner isolation are independently verified.
- **Release provenance:** reusable domain-module releases run only after successful push-triggered CI on `main`,
  check out and assert the triggering `workflow_run.head_sha`, do not repeat CI tests, and record the tested source
  SHA with released artifacts. The ChatBot umbrella is an explicit exception: its repository-owned release workflow
  triggers on push/manual dispatch and reruns the required Aspire/Dapr and live-recovery gates for that exact
  `github.sha` before `semantic-release`; it must not be described as consuming `workflow_run.head_sha`.
- **Security boundary:** reusable workflow callers use non-cancelling release concurrency, job-scoped write
  permissions, explicit named secrets, checkout-root-only non-recursive submodule initialization, CodeQL,
  dependency review, commitlint, and Dependabot. The checkout root is the workflow's repository; a ChatBot
  umbrella workflow cannot initialize submodules owned by a dependency checkout. Third-party actions are
  full-SHA pinned; the policy-owned
  `Hexalith.Builds/...@main` references are the documented exception.
- **ChatBot release inventory:** package and consumer validation covers `Hexalith.ChatBot.Contracts`,
  `Hexalith.ChatBot.Client`, and `Hexalith.ChatBot.Testing`; container validation and publication covers
  `hexalith-chatbot-server` and `hexalith-chatbot-ui`.

## Architecture Validation Results

### Coherence Validation — Design Contract Reconciled

The command, lifecycle, ownership, authorization, audit, isolation, and recovery decisions now agree with the
finalized PRD package. The old post-commit canonical-audit model is retired: D4 requires atomic mutation,
lifetime idempotency, policy/approval references, and hash-linked audit. Post-commit projections and checkpoints
have separate availability roles. Repository pins were verified against repository state at this revision; pin currency does not
imply release qualification.

Parity remains structural: every adapter origin builds the same typed command and reaches the same gateway and
required A13-gated owner path. Family-specific lifecycle matrices, immutable successors, stable identities, exact retry
profiles, current owner authorization, and first-store isolation prevent independently built units from choosing
incompatible semantics.

### Requirements Coverage Validation

All 117 FR and 79 NFR identifiers map to the capability and physical locations above. The PRD remains sole authority
for the operation/query catalog, workflow matrices, policy schema, retry registry, increments, and gate evidence.
Canonical audit completeness remains a `100%` mutation invariant, not a current qualification claim or error budget;
A6/A13 still block tamper-evidence claims, A11-M1 remains open, every A11-M2 SLO row remains `unsupported`, and A10
targets remain provisional.

### Release-Gate Validation — BLOCKED

Architecture-document completeness is final, but implementation and release readiness are not established. The
opening gate table remains the scan anchor: A5, A6, A9a, A10, A11-M1, A11-M2, and A13 are all open. The PRD's increment table owns
the complete evidence and permitted claims. The current nine-context reconciliation, compatible interfaces, local
tests, historical artifacts, and this review cannot close a gate.

### Critical Gaps and Required Evidence

A13 needs the indivisible owner-approved atomic-write, authority, Conversations, ACL, concurrency, fencing, and
recovery bundle on one candidate. A6 needs the approved data-class contract and independently witnessed production
protection/erasure evidence; A5 needs the candidate-bound provider contract and negative tests. A9a needs independently
approved exact-artifact detector/classifier evidence. A11-M1 needs the frozen M1 measurement contract and qualifying
bundle. A10 needs independent activation plus fresh controlled-loss and RTO-capable evidence. A11-M2 needs every numeric
SLO row, live provenance, calibration, route, burn test, baseline, and exact-candidate binding. Partial evidence closes
none of them.

### Architecture Completeness Checklist

- [x] Final PRD and normative appendices reconciled.
- [x] Shared command and atomic audit boundary defined.
- [x] Family lifecycle, stable identity, concurrency, and retry ownership fixed.
- [x] Authorization, M0 bootstrap, tenant policy, and cross-context ownership fixed.
- [x] Deployment, replay, observability, and recovery evidence envelopes fixed.
- [x] A5, A6, A9a, A10, A11-M1, A11-M2, and A13 preserved as open release gates.
- [ ] Implementation/release readiness — intentionally not asserted; requires the gate evidence above.

### Start Here

Use the enforcement checklist above. First establish the A13 atomic write, authority, and Conversations boundary for
one exact candidate while live AI and pilot persistence remain disabled behind A5/A6. Only then can the M0 vertical
loop and exact A9a first-use records be qualified. A11-M1 then blocks M1 exit; A10 and A11-M2 remain separate M2 gates
after lower-gate revalidation.
