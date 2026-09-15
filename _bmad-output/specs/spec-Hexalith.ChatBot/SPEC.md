---
id: SPEC-Hexalith.ChatBot
companions:
  - authority-and-contracts.md
  - planning-follow-up.md
  - ../../planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
  - ../../planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
  - ../../planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/source-manifest.md
  - ../../planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/qualification-evidence.md
  - ../../planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/reconcile-full-sibling-a13-2026-09-14.md
  - ../../planning-artifacts/architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md
  - ../../planning-artifacts/architecture.md
sources: []
---

> **Canonical reading set.** `SPEC.md` and every file listed under `companions:` must be read together under the roles in `authority-and-contracts.md`. The finalized PRD and approved addendum remain authoritative for product decisions; the final architecture spine remains authoritative for implementation consistency. This kernel summarizes those authorities and routes readers to them without redefining them.

The finalized planning artifacts establish planning consistency only. They do not establish implementation status or pilot, qualification, production, or release readiness.

# Hexalith.ChatBot

## Why

Enterprise project teams need to turn email-based external collaboration into authorized, traceable Project work without copying context into disconnected tools or granting AI unbounded access. Hexalith.ChatBot provides the governed orchestration layer for people, automation, and AI. It preserves each Hexalith bounded context's ownership and makes ambiguity, approval, failure, and evidence visible.

## Capabilities

- **CAP-1**
  - **intent:** Authorized teams can turn controlled project email into governed Project context.
  - **success:** Every admitted message reaches an authorized association, an explicit review disposition, or an explicit terminal disposition. The system preserves the applicable source identity, participant, attachment, task-intent, and audit outcomes and prevents unauthorized association candidates and cross-tenant disclosures.
- **CAP-2**
  - **intent:** Authorized people and governed AI can assist and act within one Project boundary.
  - **success:** Eligible read-only assistance remains scoped. Every determinately classified AI-proposed state-changing, outbound, file-exposing, task-creating or assigning, tool-invoking, or on-behalf effect becomes a frozen mandatory-approval proposal and executes only through its allowlisted governed command. A `classifier-indeterminate` result instead terminates without a proposal, domain or idempotency success, approval action, or effect; remediation uses a fresh immutably linked operation.
- **CAP-3**
  - **intent:** UI, CLI, MCP, service, AI, worker, and mailbox origins can use one governed operation model.
  - **success:** All state mutations from the seven origins use the shared command spine. UI, CLI, and MCP implement only the PRD's exact singular M1 parity set, with equivalent authorization, lifecycle, redaction, idempotency, retry, status, and audit outcomes.
- **CAP-4**
  - **intent:** Tenant owners and bounded administrators can bootstrap and govern ChatBot without a superuser or direct seed.
  - **success:** Two distinct principals who currently hold the Tenants `TenantOwner` role use stable commands to create the first ChatBot admin grant, the M0 policy snapshot, and four M0 service-client grants. The process enforces separation of duty, current owner authority, and fail-closed audit.
- **CAP-5**
  - **intent:** Reviewers can reconstruct governed mutations and sensitive attempts while protected data stays isolated and retention-governed.
  - **success:** Every committed mutation atomically includes its lifetime idempotency result, policy/approval references, and canonical envelope; sensitive attempts remain separate. Correction atomically freezes the complete impact manifest, enters exact state `CorrectionDelayed` on an SLO breach, and blocks all affected source and destination AI context until every item has its authorized outcome and the workflow reaches `Corrected`.
- **CAP-6**
  - **intent:** Operators can retry, replay, recover, and observe the product without confusing evidence purposes.
  - **success:** Retry behavior conforms to every row in the addendum's v1 registry. Replay demonstrates production invariance. Diagnostic, story-completion, and A10 operational evidence remain separate and independently validated.
- **CAP-7**
  - **intent:** Delivery governance can make only evidence-supported increment claims.
  - **success:** M0, M1, and M2 advance in strict order. M0 requires its complete PRD gate, current A5/A6/A13 records, and exact A9a first-use records; M1 revalidates them and requires A11-M1; M2 revalidates changed lower gates and requires A10 plus A11-M2.

## Constraints

- The finalized PRD and approved addendum are product authority. The final architecture spine is implementation-consistency authority; `architecture.md` supplies subordinate detail. A conflict is escalated to the owning authority and never resolved by this SPEC.
- AD-1 through AD-19 retain their exact identifiers, titles, and binding effect from the final architecture spine. Downstream work may reference them but must not renumber, reuse, weaken, or redefine them; an internal wording/count conflict is recorded for correction by the owning authority.
- The PRD Shared Workflow Contract is the sole lifecycle/transition authority, and its Command and Query Contracts section is the sole stable public operation-ID catalog. Every normative addendum section remains binding, including its independent classifiers, allowlist, policy, command pipeline, idempotency, replay, identity evolution, authenticity, authority, retry, operating-baseline, and recovery-qualification contracts.
- Every durable mutation uses the AD-2 command spine and its all-or-none event, lifetime-idempotency, policy/approval, and canonical-audit boundary. Post-commit publication, investigation views, checkpoints, and projections cannot repair or replace that boundary.
- `CommandGateway` alone selects exactly one profile from the closed admission map. Unknown, absent, or duplicate mappings reject; origins cannot select or alter profiles; and `ai-read-v1`, `ai-effect-v1`, and `projection-delivery-v1` run only their mandated stages.
- Security-sensitive non-mutating attempts use the separate durable attempt path. If it is unavailable, protected reads and queries return redacted `AuditUnavailable`, disclose no protected data, create no domain or idempotency state, raise the required incident signal, and remain incomplete; telemetry is not a substitute.
- The `CommandGateway`/auditable-attempt seam owns each `classifier-indeterminate` attempt and successor link. The attempt ledger is its only durable record, may prevent reuse of the original `operation_id`, never authorizes continuation or acts as domain idempotency state, and links remediation to a fresh `operation_id` by immutable predecessor reference.
- Current owner authorization, resource-scoped redaction, the closed Tenant Policy Schema, and the AD-6 command-created M0 bootstrap apply before protected access or mutation; missing, stale, malformed, unavailable, or conflicting evidence denies.
- Every row in the addendum's Retry Profile v1 table controls retryable and terminal reasons, automatic-retry maxima, exponential full-jitter backoff, exhaustion/dead-letter behavior, recovery owner, and manual command. The approved addendum currently contains 12 data rows; AD-4's reference to eleven is an explicit authority-maintenance follow-up, not permission to omit a row. A stricter profile still needs System Architect and Test Architect approval; broadening retry requires a new approved version.
- A9a, A11-M1, and A11-M2 remain independent. Exact A9a artifacts gate first use and later revalidation; A11-M1 blocks M1 until its mandatory measurement contract is current and evidenced; A11-M2 blocks M2 until every declared SLO and its baseline, recalibration, supported-request, catalog-drift, dashboard, route, and burn-evidence contract qualifies against the exact candidate.
- Recovery diagnostics, exact-candidate story-completion evidence, and retained A10 operational evidence use disjoint channels. The accepted Epic 12 contract remains `activation: pending`, and no current artifact closes A10.
- Cross-context ownership and choreography follow AD-5, AD-14, and AD-15: owners remain sovereign, ChatBot orchestrates by stable ID and durable events, and no distributed dual-write or local mirror may create owner authority.
- The ChatBot correction aggregate solely owns immutable manifest membership and lifecycle. The frozen manifest covers every ChatBot-derived store; every affected Conversations/Folders record and index; approved or executed AI actions; appended messages; task-intent conversions; sent mail; external/tool effects; file disclosures; and every required irreversible-effect disposition. Only the A13-mapped owner adapter or actor may acknowledge or dispose its item; coordinators and projections cannot self-acknowledge, add, replace, or omit one.
- A12 remains unresolved, without becoming an additional release gate in this SPEC: `IdentityEvolved` is proposed, not binding, until every producer accepts the versioned contract. Until then unresolved current identity fails closed to authorized review, original audit identifiers remain immutable, and no automatic cross-context migration may be assumed.
- Release Governance owns the immutable gate-record registry. Each candidate/increment has one immutable `gate_set_id` over every required record; consumers recompute current status at use; incomplete, mixed-candidate, expired, invalidated, or superseded sets fail closed; CI, release, and runtime enablement consume that same set; and drift requires a new set.
- M0 requires current A5/A6/A13 and exact A9a first-use records. M1 revalidates those records and additionally requires A11-M1. M2 revalidates changed A5/A6/A13/A9a/A11-M1 evidence and additionally requires A10 and A11-M2. The PRD increment table remains the complete release gate, including all evidence, outcomes, counter-metrics, disable conditions, sequencing, and permitted claims.
- Existing epics, stories, and sprint status are follow-up inputs wherever they drift from these authorities. Their wording and status are not silently reinterpreted; the concrete reconciliation ledger is `planning-follow-up.md`.

## Non-goals

- Redefining, abbreviating away, or resolving a conflict inside the PRD package or architecture spine; closing A5, A6, A9a, A10, A11-M1, A11-M2, A13, Epic 12 activation, or any increment gate through planning-document finality; or treating current code, story completion, historical evidence, interface compatibility, or a reviewed revision as readiness proof.
- Rewriting epics, stories, sprint tracking, code, CI, dependencies, or repository settings in this spec update.
- Broadening MVP into general email, task management, non-Project workspaces, unrestricted AI/commands, additional channels, arbitrary integrations, or commercial packaging.
- Introducing alternate lifecycle states, public operation IDs, policy knobs, retry behavior, audit topology, authority mappings, or cross-context owners.

## Success signal

`SPEC.md` and its companions enable a downstream planner, implementer, or reviewer to locate the exact product and architecture authorities; preserve every AD-1 through AD-19 decision and M0–M2 contract; detect delivery-artifact drift before acting; and reject any readiness claim whose complete PRD gate evidence is not current and approved.
