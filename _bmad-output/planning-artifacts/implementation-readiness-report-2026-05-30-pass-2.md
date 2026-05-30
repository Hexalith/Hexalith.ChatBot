---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
includedDocuments:
  prd:
    - _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
    - _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
  architecture:
    - _bmad-output/planning-artifacts/architecture.md
  epics:
    - _bmad-output/planning-artifacts/epics.md
  ux:
    - _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md
    - _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-30
**Project:** Chatbot
**Run:** pass-2 (re-validation after the 2026-05-30 sprint change proposals; supersedes the 14:40 report which predates the 15:03 `epics.md` revision)

## Step 1: Document Discovery

### PRD Files Found

**Sharded:** `prds/prd-Hexalith.ChatBot-2026-05-28/`
- `prd.md` — primary PRD
- `addendum.md` — PRD addendum
- _(supporting, not assessed: validation-report.md, prd-validation-report.md, reconcile-product-brief.md, review-adversarial-general.md, review-adversarial-v2.md, review-rubric.md, review-rubric-v2.md, .decision-log.md, validation-report.html)_

**Whole document at root:** none — no duplicate conflict.

### Architecture Files Found

**Whole:** `architecture.md` (64 KB, modified 11:24)
**Sharded:** none — no duplicate conflict.

### Epics & Stories Files Found

**Whole:** `epics.md` (194 KB, modified 15:03 — current post-sprint-change version)
**Sharded:** none — no duplicate conflict.

### UX Design Files Found

**Sharded:** `ux-designs/ux-Hexalith.ChatBot-2026-05-28/`
- `DESIGN.md` — UI/visual design spec
- `EXPERIENCE.md` — experience/interaction spec
- _(supporting, not assessed: review-accessibility.md, review-rubric.md, validation-report.md, .decision-log.md, validation-report.html)_

**Whole document at root:** none — no duplicate conflict.

### Issues Identified

- **Duplicates:** None. No document type exists as both whole and sharded.
- **Missing required documents:** None. PRD, Architecture, Epics, and UX are all present.
- **Output-file conflict (resolved):** The default output path `implementation-readiness-report-2026-05-30.md` already held a complete report generated at 14:40. Because `epics.md` was modified at 15:03 and `sprint-change-proposal-2026-05-30-pass-2.md` was created at 15:05 (both after that report), the prior report is stale. Resolution: this re-validation is written to `implementation-readiness-report-2026-05-30-pass-2.md`, preserving the 14:40 report.

### Confirmed Document Set for Assessment

| Type | Document(s) |
|------|-------------|
| PRD | `prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, `addendum.md` |
| Architecture | `architecture.md` |
| Epics & Stories | `epics.md` (15:03 version) |
| UX Design | `ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`, `EXPERIENCE.md` |

_Document discovery complete and confirmed by Jerome._

## Step 2: PRD Analysis

Source: `prd.md` (1,482 lines) + `addendum.md` (167 lines), both read in full. The PRD is a single-release MVP delivered in three dependency-ordered increments **M0 → M1 → M2**. Requirements are tagged FR1–FR96 (plus lettered sub-requirements) and NFR1–NFR70 (plus lettered sub-requirements).

### Functional Requirements

**Project Email Intake and Association (FR1–FR12)**
- FR1: Capture authorized mailbox events as project collaboration inputs.
- FR2: Preserve source email identity, thread identity, mailbox identity, sender, recipients, timestamps, attachment references.
- FR3: Associate incoming email with an existing project using deterministic evidence.
- FR4: Detect ambiguous project association and route it to human review.
- FR5: Authorized users review candidate projects with visible evidence, confidence state, reason codes, and decision consequences.
- FR6: Authorized users choose a candidate, reject all, defer, mark needs-review, add optional decision note.
- FR7: Authorized users correct a previously selected association.
- FR8: Record association decisions, corrections, rejections, deferrals, retries, and skipped items.
- FR9: Tenant admins configure association rules, evidence requirements, and thresholds `T_high`/`T_low` (security-sensitive; addendum §Confidence Thresholds).
- FR10: Preserve original email context when association is rejected/deferred/failed/skipped/awaiting review.
- FR11: Expose deterministic association reasons and confidence inputs in machine-readable form for UI/CLI/MCP/audit/test.
- FR12: Authorized users compare candidate project evidence side by side.

**Participants, Identity, and Authorization (FR13–FR20)**
- FR13: Resolve internal/external email participants to tenant-scoped parties.
- FR14: Authorized users identify unresolved participants for review.
- FR15: External participants contribute context via email without MVP portal access.
- FR16: Enforce tenant+project authorization before exposing candidates, files, conversations, approvals, commands, audit.
- FR17: Block unresolved/unauthorized actors from files, task requests, commands, outbound communication.
- FR18: Tenant admins configure governed mailbox participation rules.
- FR19: Authorized admins configure service-client access for CLI, MCP, workers, mailbox events, AI actors.
- FR20: Record consent/lawful-basis metadata where tenant policy requires.

**Project Conversation and Context (FR21–FR28)**
- FR21: View email-derived messages as project conversation context.
- FR22: Represent associated email, participants, attachments, decisions, approvals, failures, AI outcomes (decompose into 7 sub-stories for authoring).
- FR23: Inspect why an email belongs to a project (+ explicit "why-panel" accept-when criteria).
- FR24: See association/attachment/task/approval/command/failure/retry/next-action status.
- FR25: Keep project conversation context separate across tenants and projects.
- FR26: Distinguish informational vs actionable requests (+ classification-badge accept-when).
- FR27: Distinguish system summaries from source evidence (+ AI-provenance accept-when).
- FR28: Preserve visible human-review history per email/attachment/approval/AI action/command.

**Files and Attachments (FR29–FR34)**
- FR29: Capture attachments from associated project email.
- FR30: Store captured attachments in governed project folders.
- FR31: Inspect attachment capture/storage status.
- FR32: Prevent unauthorized actors from viewing attachment metadata/content.
- FR33: Make authorized files available as scoped AI context only through explicit authorization, policy checks, auditable packaging.
- FR34: Represent attachment states: captured, pending, unavailable, rejected, unsafe, failed, retryable.

**Task Intent and AI Action Mediation (FR35–FR46)** — risk classes: low-risk / approval-required / denied / unsupported; mixed requests inherit strictest.
- FR35: Detect candidate task/action intent from authorized actors + preserve source evidence (+ data-contract fields; precision/recall targets A9a).
- FR36: Review captured task intent before governed action.
- FR37: Convert captured task intent into a governed task/action request (audited).
- FR38: Mark task intent not-actionable/duplicate/already-handled/out-of-scope (terminal states).
- FR39: Classify AI action requests by risk (addendum §Risk Classifier).
- FR40: Allow low-risk AI assistance when tenant policy + project authorization permit.
- FR41: Require approval before AI actions that modify state / expose files / send external / create-assign tasks / invoke tools / act on behalf.
- FR42: Approve or reject proposed AI actions after review (+ detailed approval-surface accept-when).
- FR43: Execute approved AI actions only through allowlisted governed commands.
- FR44: Inspect AI action proposals, approvals, denials, executions, failures, outcomes.
- FR45: Preview outbound communication, file access, command execution, AI changes before approval/execution.
- FR46: Refuse/block unsafe AI/automation/command/mailbox requests exceeding policy/authorization/authority/allowlist.

**Outbound Communication (FR47–FR50, incl. FR48a–FR48d)**
- FR47: Create outbound project email drafts within approved project + sender authority.
- FR48: Distinguish draft-only / authenticated-user / shared-mailbox / send-on-behalf / approved-service-send authority (addendum §Authority class mapping; conflict → fail closed).
- FR48a (M1): Record provider DMARC/DKIM/SPF verdicts as-supplied (no re-verify).
- FR48b (M1): Header inspection (Received/Authentication-Results/From/Reply-To/Sender/X-Original-Sender), record disagreements.
- FR48c (M1): On-behalf-of disambiguation; record delegate identity + `principal_for`.
- FR48d (M1): External-sender posture flag; `mailbox.authenticity-strictness` knob controls behavior.
- FR49: Require approval before outbound leaves the project boundary.
- FR50: Preserve proposed/approved content, recipients, sender authority, project context, requester, approver, decision in approval records.

**Admin, Governance, and Audit (FR51–FR63, incl. FR55a)**
- FR51: Configure mailbox integration settings + monitored patterns.
- FR52: Configure AI action policy for low-risk and approval-required actions.
- FR53: Review mailbox permission status + degraded processing states.
- FR54: Compliance/support investigate association/approval/command/risky-AI decisions.
- FR55: Produce audit records for security-sensitive events.
- FR55a (M2): Cross-tenant isolation in derived stores (vector/embedding/prompt-context/candidate caches) enforced by construction; isolation probe per NFR59.
- FR56: Query audit records by tenant/actor/command/resource/decision/reason/correlation/time.
- FR57: Hide unauthorized project names, evidence, file metadata, audit, CLI output, MCP payloads, error details.
- FR58: Operational support for retention/export/deletion workflows.
- FR59: Propagate correlation context across the full pipeline.
- FR60: Preserve source evidence with retention boundaries + redaction.
- FR61: Maintain versioned policy snapshots.
- FR62: Add human notes/resolution rationale to decisions.
- FR63: Supersede reversible human decisions where policy permits, preserving original in audit.

**Reliability, Failure Handling, and Operations (FR64–FR80, incl. FR75a–FR75g)**
- FR64: Detect duplicate mailbox delivery, avoid duplicate artifacts.
- FR65: Retry failed mailbox/attachment/association/approval/command/projection work where valid.
- FR66: Surface terminal and non-terminal failure states.
- FR67: Expose mailbox health + all operational queues (+ queue-view accept-when; stable status enums).
- FR68: Fail closed when association/identity/tenant scope/authorization/audit/dependencies unresolved.
- FR69: View+manage queues for ambiguous/unresolved/pending/failed/retryable items.
- FR70: Assign or claim review items.
- FR71: See next required human action per item.
- FR72: Notify users when review/approval/failure/degraded/quarantine/retry states need attention.
- FR73: Configure notification routing + escalation rules.
- FR74: Disable/quarantine/rate-limit mailbox sources/service clients/AI actors/command capabilities (decompose per subject×action).
- FR75: Configure per-tenant rate limits/quotas/circuit breakers.
- FR75a (M1): `tenant-admin` = union of admin scopes; finer roles hold subsets; admin assignment is security-sensitive+audited.
- FR75b: See-only scopes (queue summaries/health/metrics) without per-project membership; per-item detail needs per-project authority.
- FR75c: Operate scopes (retry/requeue/quarantine/dismiss) on see-only items; recorded; cannot mutate project records.
- FR75d: Policy scope (`policy-admin`); security-sensitive knobs require two-person rule + justification.
- FR75e: Mailbox scope (`mailbox-admin`); cannot read content or decide associations.
- FR75f: Compliance scope (`compliance-admin`); read audit subject to redaction; configure retention within NFR49a; cannot operate on items.
- FR75g: Audit obligation on every admin action; no skip-audit path; no NFR15a/NFR50a bypass.
- FR76: Present review items with clear actions, disabled-reasons, next-step guidance (+ accept-when, finite reason set).
- FR77: Explain refusal/blocked/degraded/failed/denied states in user-safe language (+ versioned message-catalog accept-when).
- FR78: Filter/sort/prioritize queues by age/risk/confidence/project/mailbox/failure/reviewer/next-action.
- FR79: Show stale/waiting/blocked/escalation-needed states.
- FR80: Retrieve long-running operation status (identity, state, retry count, partial outputs, next actions, terminal reason, correlation) across UI/CLI/MCP.

**Cross-Surface Command Parity (FR81–FR86, incl. FR81a)**
- FR81: UI users perform core governed email-to-project operations.
- FR81a: **Shared command pipeline architectural invariant** — every state-mutating op (any surface) passes through one pipeline: auth → tenant-scope → authorization → risk classification → approval gate → idempotency → execution → audit → projection. Adapters cannot replicate stages. Parity by construction (addendum §Shared Command Pipeline).
- FR82: CLI parity for inspect/associate/reject/defer/correct/retry/approve/execute/status/audit.
- FR83: MCP parity for governed AI-agent/automation access.
- FR84: Equivalent authorization outcomes + state transitions across surfaces (verification of FR81a).
- FR85: Identify action origin (UI/API/CLI/MCP/worker/mailbox-event/AI actor); attached at adapter, immutable downstream.
- FR86: Contract tests verify the FR81a invariant (same Command record per surface after normalization).

**Workflow State, Contracts, and Testability (FR87–FR96, incl. FR91a, FR95a)**
- FR87: Define canonical lifecycle states for all workflow object types.
- FR88: Validate inbound/outbound state transitions against an explicit state model.
- FR89: Reject invalid transitions; record rejected transition/actor/reason/correlation.
- FR90: Expose idempotency keys + stable resource identifiers (addendum §Idempotency Keys).
- FR91: Separate immutable source records from derived projections; rebuild projections when needed.
- FR91a (M0/M1): Correction propagation contract — invalidate+rebuild all derived stores on correction; `correcting` state blocks AI use until done. (NFR17a: p95 ≤ 10 min M0/M1, ≤ 60 min M2; breach → `correction-delayed`, P2 incident.)
- FR92: Maintain internal evaluation datasets (consented/redacted/synthetic) with expected outcomes + regression history.
- FR93: Provide tenant-scoped test fixtures/sandbox data.
- FR94: Expose measurable operational outcomes (latencies, retry exhaustion, duplicate suppression, audit lag) (surface per NFR42a).
- FR95: Simulate/replay representative mailbox events without external sends or production mutation.
- FR95a (M2): Replay isolation contract — dedicated test tenant, intercepted outbound, `replay_run_id`, nightly probe gates M2 release.
- FR96: Make recorded corrections available as future association evidence only when policy permits + explainable + inspectable.

**Total Functional Requirements: 96 base (FR1–FR96) + 15 lettered sub-requirements (FR48a-d, FR55a, FR75a-g, FR81a, FR91a, FR95a) = 111 FR-tagged items.**

### Non-Functional Requirements

**Security and Privacy (NFR1–NFR12, incl. NFR9a)**
- NFR1: Enforce tenant/actor/role/project/resource authorization before returning data or mutating state.
- NFR2: Redacted failure responses to unauthorized actors (no restricted names/metadata/evidence/audit/tenant data).
- NFR3: Encrypt all sensitive data classes in transit + at rest; release validation verifies TLS + encrypted storage + no plaintext export.
- NFR4: No secrets/credentials in logs/traces/CLI/MCP/audit/support bundles/diagnostics.
- NFR5: Least-privilege scope + revocation for M365/service/CLI/MCP/AI-tool credentials.
- NFR6: Bounded staleness + revocation-sensitive invalidation for auth/policy/identity caches (default 5 min; 60 s for revocation).
- NFR7: Fail closed when identity/tenant scope/authorization/audit readiness/policy/validation unavailable.
- NFR8: AI actors operate only through authorized scope/files/tools/commands/policy authority.
- NFR9: AI prompts/context/outputs/results tenant+project scoped, redacted, retention-logged, blocked from training/telemetry/reuse; validate context-package fields before invocation.
- NFR9a (M2): Derived-store cross-tenant isolation at store level; nightly cross-tenant probe is stop-ship. (See FR55a.)
- NFR10: Logs/metrics/traces/support bundles/test artifacts pass secret + sensitive-data redaction before export.
- NFR11: Cross-tenant isolation testing — zero tolerance for unauthorized exposure across all surfaces.
- NFR12: Define data residency/region boundaries per persisted data class before onboarding when residency specified.

**Reliability and Data Integrity (NFR13–NFR22, incl. NFR13a, NFR15a)**
- NFR13: Idempotent per-operation with stable key, replay window, conflict response, same final observable state.
- NFR13a: Per-operation idempotency contract — 8 operation classes in addendum §Idempotency Keys; new classes extend the table before shipping.
- NFR14: Duplicate delivery must not create duplicate messages/attachments/intents/approvals/commands/notifications/outbound/audit.
- NFR15: Reject invalid transitions before mutation with deterministic error + audit; if audit unavailable, **every** state-mutating transition fails closed.
- NFR15a: **Fail-Closed Contract** — enumerated 10-path inventory; "audit writer down" returns typed `AuditUnavailable`, queues intent without writing state, alerts. No "audit unavailable → continue" branch.
- NFR16: Risky AI/external sends/command execution/file-context packaging blocked unless approval+policy snapshot+authority+input validation+audit readiness verified.
- NFR17: Partial failures leave items in visible recoverable states (pending/retryable/failed/quarantined/needs-review).
- NFR18: Retry policy specifies retryable-vs-terminal, max attempts, backoff, jitter, dead-letter, manual recovery, terminal reasons.
- NFR19: At-least-once workers safe via idempotency, concurrency control, lease/lock expiry, poison-message handling.
- NFR20: Queue processing prevents starvation across tenants/mailboxes/projects/item types.
- NFR21: File processing enforces malware/unsafe-content policy, size/type limits, scan status, quarantine, safe failures before exposure.
- NFR22: Non-AI workflows continue during AI provider outage (outage tests prove association/approval/retry/audit work without live AI).

**Performance and Scalability (NFR23–NFR30)**
- NFR23: Documented, versioned, quarterly-reviewed operating baselines (owner, approval/review dates, thresholds).
- NFR24: User-facing lookups p95 ≤ 2 s under MVP baseline (synthetic + APM measured).
- NFR25: Ambiguous candidate generation p95 ≤ 10 s, else pending/manual-review with retrievable identity.
- NFR26: CLI/MCP long-running ops return identity+status within 5 s p95; no client hold > 30 s without retrievable status.
- NFR27: Queue views support filter/sort/pagination/prioritization (default page ≤ 100; server-side filters).
- NFR28: Latency metrics include percentiles, error rate, retry rate, queue age, saturation, audit projection lag.
- NFR29: Tenant-level rate limits/quotas/circuit breakers across all surfaces.
- NFR30: Per-tenant/source backlogs must not degrade unrelated tenants/workflow sources where isolation is possible.

**Integration and Interoperability (NFR31–NFR36)**
- NFR31: M365/Exchange integration tolerates revoked permissions/expired tokens/throttling/backoff/partial access/duplicate events/delayed delivery/webhook replay/subscription expiry/permission drift without broadening access.
- NFR32: Contract-verifiable responses+events with stable IDs/codes/reason codes/state names/redaction/correlation/equivalent authorization.
- NFR33: Backward-compatible evolution or explicit versioning + deprecation + migration paths.
- NFR34: Correlation context across the full pipeline + webhooks.
- NFR35: Config/policy changes auditable/versioned/rollback-capable (non-destructive); destructive/authority-expanding changes require new version.
- NFR36: Server-side UTC timestamps; preserve source timestamps/timezone; convert to local only at presentation.

**Operability and Observability (NFR37–NFR48, incl. NFR42a)**
- NFR37: Operators observe mailbox health, backlog, all queues, failures, audit projection lag.
- NFR38: User-visible status separated from privileged diagnostic detail by authorization.
- NFR39: Actionable status for degraded/stale/waiting/blocked/escalation/failed/retryable/terminal states.
- NFR40: Degraded/blocked/failed/waiting communicated via versioned message catalog (FR77); uncategorized-state count must be 0 per release (blocks release).
- NFR41: Isolate degraded dependencies to narrowest scope; state affected scope+dependency within 5 min of detection.
- NFR42: Degraded-state surfaces render state enum + affected scope + owner role + next safe action; synthetic-verified.
- NFR42a (M2): SLOs published per-tenant + addendum §Operating Baselines; each has target/window/error-budget/alert threshold.
- NFR43: Non-invasive tenant-safe alerting + synthetic checks tied to documented thresholds (defaults: subscription expiry 7 days, retry exhaustion, audit lag > 5 min, approvals > 2 business days, auth failure spikes).
- NFR44: Runbook-ready single-item diagnostics (enumerated min fields); 100 sampled items/week each render complete diagnostic.
- NFR45: Redacted shareable support bundles preserving correlation/state/reason without restricted evidence.
- NFR46: Prevent approval fatigue with measurable mechanisms (prioritization, grouping, suppression/rate ceiling ≤8/hr ≤30/day, backlog SLO >25, rubber-stamp observable >15% triggers FR41 revisit).
- NFR47: Distinguish reversible/supersedable/compensating/irreversible actions before approval.
- NFR48: Every surfaced evidence reference carries freshness chip (fresh/stale/expired); cannot approve against `expired`.

**Auditability, Compliance, and Data Governance (NFR49–NFR55, incl. NFR49a, NFR50a)**
- NFR49: Tamper-evident, retention-governed, redaction-aware, reconstructable audit with restricted modify/delete controls.
- NFR49a (M2): Append-only WORM store, hash-chained envelopes, nightly chain verification, erasure via tombstone+key-shred (never chain mutation).
- NFR50: Audit records include full enumerated field set; automated tests verify 100% field presence for security-sensitive events.
- NFR50a (M2): Audit completeness as production observable — ≥ 99.5% reconstructable per rolling 7-day window per tenant; below → P1; replay excluded.
- NFR51: Preserve enough context to reconstruct who/what/policy/evidence/transitions/redaction/outcome.
- NFR52: Minimize retained content to authorized-workflow/audit/retention need.
- NFR53: Retention/export/deletion workflows distinguish all data classes.
- NFR54: Audit evidence respects retention boundaries + redaction (not uncontrolled storage).
- NFR55: Record consent/lawful-basis metadata where required.

**Recovery and Continuity (NFR56–NFR59)**
- NFR56: RPO ≤ 15 min, RTO ≤ 4 hr default for source records (A10 starter target).
- NFR57: Derived projections rebuildable from source within 4 hr for baseline dataset without mailbox re-ingestion.
- NFR58: Dependency outages degrade only affected scope; outage tests prove no unrelated tenant/mailbox blocked.
- NFR59: Resilience validation proves degraded dependencies cause no cross-tenant leakage/unauthorized mutation/silent loss.

**Accessibility and Usability Quality (NFR60–NFR64)**
- NFR60: WCAG 2.2 AA scoped per increment to enumerated UI surfaces (M0: ambiguous-association review, AI-action approval, project-conversation view; M1/M2 add their surfaces). CLI/MCP out of scope.
- NFR61: Accessibility validation includes keyboard-only/screen-reader/focus-order/non-color status/error recovery for association+approval.
- NFR62: Status/failure/refusal/authorization messages understandable without restricted evidence or color-only.
- NFR63: Users identify next available action without reading raw audit logs.
- NFR64: UI distinguishes source evidence from AI-generated summaries.

**Validation and Quality Gates (NFR65–NFR70)**
- NFR65: Documented quality gates (isolation, authorization, redaction, idempotency, transitions, approval gates, duplicate suppression, audit creation).
- NFR66: Performance validation (backlog, queue usability, retry, audit lag, throttled Graph) against baselines.
- NFR67: Security validation — negative authorization tests for every actor type.
- NFR68: Evaluation datasets/fixtures consented/redacted/synthetic, versioned, reproducible, redaction-verified.
- NFR69: Replay/simulation isolated from production mutation/external sends/live AI/live commands; labeled + tenant-scoped.
- NFR70: Every externally visible operation defines expected transition, audit event, response, redaction, retry/idempotency result.

**Total Non-Functional Requirements: 70 base (NFR1–NFR70) + 7 lettered sub-requirements (NFR9a, NFR13a, NFR15a, NFR17a, NFR42a, NFR49a, NFR50a) = 77 NFR-tagged items.**

### Additional Requirements (constraints, contracts, assumptions)

These are binding requirements not numbered as FR/NFR but carried in the PRD body and addendum, and must be traced into epics:

- **Command contracts (26 commands):** CaptureMailboxEvent, ProposeEmailProjectAssociation, AssociateEmailToProject, ConfirmEmailProjectAssociation, RejectEmailProjectAssociation, DeferEmailProjectAssociation, MarkEmailAssociationNeedsReview, CorrectEmailProjectAssociation, ReprocessEmailAssociation, QuarantineEmailAssociation, LinkOrResolveEmailParticipant, CaptureEmailAttachment, StoreEmailAttachmentInProjectFolder, CaptureTaskIntent, MarkTaskIntentDisposition, ProposeAIAction, ApproveAIAction, RejectAIAction, RequestAIActionRevision, CancelAIAction, ExecuteApprovedProjectCommand, CreateOutboundProjectEmailDraft, SendApprovedProjectEmail, RecordWorkflowAuditDecision, RetryWorkflowOperation, GrantServiceClientPermission, RevokeServiceClientPermission.
- **Query contracts (14 queries):** GetEmailAssociationStatus, ListProjectAssociationCandidates, GetProjectAssociationEvidence, ListUnresolvedOrDeferredMessages, GetAttachmentStorageStatus, GetTaskIntentStatus, GetAIActionProposal, GetApprovalStatus, GetWorkflowOperationStatus, GetAuditHistory, GetMailboxIngestionHealth, ListOperationalQueues, GetServiceClientPermissions, GetProjectAccessForActor. (No admin/debug bypass.)
- **Association lifecycle states (canonical):** Received → Proposed → Associated | Rejected | Deferred | NeedsReview | Failed | Skipped; Associated → Corrected (with Correcting / Correction-delayed transient sub-states). Rejected/Failed/Skipped terminal; reprocess creates new workflow instance with audit linkage.
- **RBAC matrix:** 10 actor types (TenantAdmin, ProjectAdmin/Owner, ProjectMember/Contributor, MailboxOwner, Auditor/Compliance, ServiceClient, CLI Client, MCP Client, AI Actor, Background Worker) with allowed resources/actions/explicit blocks.
- **Service-client classes (6):** mailbox-ingestion-client (M0), audit-projection-client (M0), background-retry-client (M0), cli-automation-client (M1), mcp-tool-client (M1), ai-action-execution-client (M1) — each with scope, command set, credential expiry.
- **Data Governance Surface:** 13 ChatBot-owned derived record classes with retention class, redaction sensitivity, isolation surface, owner increment.
- **UI Surface Inventory (handoff to UX):** S1–S10 surfaces mapped to increments (M0: S1–S3; M1: S4–S7; M2: S8–S10).
- **Integration list (MVP):** Hexalith.Projects, Parties, Folders, Tenants, EventStore, FrontComposer, M365/Exchange, Keycloak, Aspire, CLI (M1+), MCP server (M1+).
- **Addendum binding contracts:** Confidence Thresholds (T_high=0.90/T_low=0.60 M0 defaults), Risk Classifier (tag-and-heuristic M0), Command Allowlist v0 (single command `Project.AppendConversationMessage`), Command Allowlist v1 (M1), Tenant Policy Schema (closed knob set, M0/M1/M2 tiers), Shared Command Pipeline invariant, Idempotency Keys (8 operation classes), Replay Isolation, ID Evolution Contract, Inbound Message Authenticity + Authority Class Mapping (FR48 5-class taxonomy), Operating Baselines (deferred to M2).
- **Open assumptions A1–A11:** explicit, with owners and revisit conditions — A1 (M365 first mailbox), A2 (single mailbox pattern first), A3 (CLI/MCP parity scope), A4 (operating baselines), A5 (AI provider config), A6 (audit retention vs GDPR), A7 (no external portal), A8 (fixed command allowlist), A9/A9a (evaluation dataset: ≥500 labeled by M0, ≥2000 by M1), A10 (RPO/RTO starter targets), A11 (pilot adoption thresholds). **These are flagged "must not be silently assumed closed by implementation."**
- **Material-change re-check protocol:** System Architect owns re-check within 5 business days of any material sibling-context contract change; outcome logged to `.decision-log.md`.

### PRD Completeness Assessment

**Strengths (unusually high requirement maturity):**
- Requirements are densely cross-referenced (Traceability Overview maps journeys → FRs → NFRs); high-risk FR groups carry explicit acceptance-scenario minimums (FR1-12, FR39-46, FR55-63, FR81-89).
- Fail-closed, idempotency, audit, and authorization invariants are specified as contracts (NFR15a path inventory, addendum idempotency table) rather than aspirations.
- Increment sequencing (M0/M1/M2) is explicit with per-increment scope, a non-negotiable safety floor, and dependency ordering — directly testable against epic structure in later steps.
- The PRD already passed two validation cycles (2026-05-28) and an adversarial review; it is `status: final`.

**Watch-items to carry into Steps 3–5 (epic coverage / quality):**
1. **High FR sub-requirement density** — FR22, FR74 carry explicit decomposition guidance ("decompose into N sub-stories"). Step 5 must verify epics honor that decomposition rather than collapsing it (relevant to Jerome's fine-grained-story preference).
2. **Increment tagging** — many FR/NFR sub-items are explicitly M0/M1/M2 scoped. Step 3 must confirm epics preserve the M0 → M1 → M2 dependency order and don't pull M1/M2 governance into M0 or vice versa.
3. **Assumptions A1–A11 are open** — these must surface as risks/owners in epics, not be silently treated as closed.
4. **Addendum is binding, not optional** — the 11 addendum contracts are referenced by FRs/NFRs as the actual contract surface. Epic coverage must reach the addendum-defined behavior (e.g., 8 idempotency operation classes, 5 sender-authority classes, single M0 allowlisted command), not just the FR headline.
5. **`epics.md` changed at 15:03 (post the 14:40 report)** — the pass-2 sprint change is the specific reason this re-validation exists; Step 3 coverage must be run against the current epics, with attention to whatever the two sprint-change proposals altered.

_PRD analysis complete. FR/NFR/additional-requirement extraction recorded. Proceeding to Step 3: Epic Coverage Validation._

## Step 3: Epic Coverage Validation

Source: `epics.md` (2,630 lines, current 15:03 version), read in full for the Requirements Inventory, Additional Requirements, UX Design Requirements, FR Coverage Map, and the Epic List with per-epic "FRs covered" declarations. The epics document carries its own embedded **FR Coverage Map** (every FR → exactly one primary epic) plus per-epic FR declarations, which I cross-validated against the PRD's 111 FR-tagged items.

### Coverage Matrix (by epic / increment)

| Epic | Increment | Primary FRs covered | Count |
|------|-----------|---------------------|-------|
| **E1** First Safe Governed Action & Command Spine | M0 | FR16, FR55, FR57, FR59, FR61, FR68, FR77, FR80, FR81, FR81a, FR85, FR86, FR87, FR88, FR89, FR90, FR92, FR93 | 18 |
| **E2** Email Intake & Project Association | M0 | FR1–FR15 (less FR16), FR17, FR60, FR62, FR63, FR64, FR65, FR66, FR71, FR76, FR79, FR91, FR91a, FR96 | 28 |
| **E3** Project Conversation Context, Files & Attachments | M0 | FR21–FR34 | 14 |
| **E4** Governed AI Action Mediation | M0 | FR35–FR46 | 12 |
| **E5** Cross-Surface Parity — CLI & MCP | M1 | FR19, FR82, FR83, FR84 (+ extends FR80/FR85/FR86) | 4 |
| **E6** Outbound Communication & Inbound Authenticity | M1 | FR47, FR48, FR48a, FR48b, FR48c, FR48d, FR49, FR50 | 8 |
| **E7** Tenant Administration & Governance Policy | M1 | FR18, FR51, FR52, FR53, FR69, FR70, FR72, FR73, FR74, FR75, FR75a–FR75g, FR78 | 18 |
| **E8** Operational Dashboards & Observability | M2 | FR67 (full), FR94 | 2 |
| **E9** Tamper-Evident Audit, Compliance Investigation & Recovery | M2 | FR20, FR54, FR55a, FR56, FR58, FR95, FR95a (+ extends FR92) | 7 |
| | | **Primary-assignment total** | **111** |

**Multi-increment FRs (primary epic + noted extension)** — these are correctly modelled as one primary home plus a downstream extension, not as gaps:
- FR9 → E2 (M0 thresholds) + E7 (full policy editor)
- FR55 → E1 (audit emission) + E9 (WORM persistence)
- FR67 → E8 primary (full dashboards) with M0-minimal surfacing noted in E1/E2
- FR80 → E1 (UI/M0) + E5 (CLI/MCP exposure)
- FR85 → E1 (origin attribution) + E5 (extended across surfaces)
- FR86 → E1 (M0 shims) + E5 (full differential harness)
- FR87 → E1 (canonical states) + E7 (full `Skipped` + transition matrix)
- FR90 → E1 (idempotency keys) + E9 (full per-class contract)
- FR92 → E1 (test infra) + E9 (extended evaluation datasets)

### Missing Requirements

**None.** All 96 base FRs and all 15 lettered sub-FRs (FR48a–d, FR55a, FR75a–g, FR81a, FR91a, FR95a) have a traceable primary-epic home. No FR is unassigned.

**Reverse check (FRs in epics but not in PRD):** None. The epics' Requirements Inventory mirrors the PRD FR catalog exactly (same numbering, same sub-FRs, same increment tags). No invented or orphaned FRs.

**Consistency check (FR Coverage Map vs per-epic declarations):** Consistent. Every FR's primary-epic assignment in the Coverage Map matches that epic's declared "FRs covered" list; the union of per-epic primary FRs equals 111 with no double-counting of primary ownership.

### Beyond-FR coverage observed (informational; feeds Steps 4–5)

- **NFRs (77 items):** intentionally modelled as cross-cutting quality bars rather than 1:1 epic rows — security/isolation across all epics; reliability/idempotency in E1–E2; accessibility in E2–E3/E7/E8; audit/recovery in E1+E9; performance/observability in E8. This is a defensible pattern but means **NFR traceability is not per-NFR explicit** — Step 5 (epic quality) should confirm each NFR has at least one acceptance hook in the stories that claim it, since some NFRs (e.g., NFR15a fail-closed contract, NFR49a WORM, NFR50a completeness, NFR9a derived-store isolation) are release-gate invariants.
- **Architecture/Additional requirements:** the epics carry an explicit Additional Requirements inventory (scaffold/starter decision, pinned stack, decisions D1–D7, mechanical-enforcement tests) seeding E1 and constraining all epics — strong traceability from `architecture.md`.
- **UX-DRs (46 items):** the epics map all 46 UX design requirements to surface stories, with cross-cutting visual/accessibility UX-DRs anchored in Stories 1.14–1.21. Detailed UX alignment is the subject of **Step 4**.

### Coverage Statistics

- **Total PRD FR-tagged items:** 111 (96 base FR1–FR96 + 15 lettered sub-FRs)
- **FRs with a primary epic home:** 111
- **FR coverage: 100%**
- **Unassigned FRs:** 0
- **Orphaned FRs (in epics, not in PRD):** 0
- **Map ↔ epic-declaration inconsistencies:** 0
- **Epics:** 9, across the three fixed increments (M0: E1–E4 · M1: E5–E7 · M2: E8–E9), strictly forward dependency flow.

**Pass-2 note:** the 15:03 `epics.md` revision (the reason this re-validation exists) left FR coverage complete and internally consistent — the sprint change did not introduce coverage gaps or orphan any requirement. Whether the sprint change altered *story-level* scope/quality is assessed in Step 5.

_Epic coverage validation complete: 100% FR coverage, zero gaps, zero orphans. Proceeding to Step 4: UX Alignment._

## Step 4: UX Alignment Assessment

### UX Document Status

**Found.** Two `status: final` UX documents (both read in full), forming a deliberate two-part spec:
- `DESIGN.md` (239 lines) — visual identity: Fluent UI v5 token mapping, semantic color system, typography/spacing/radius tokens, 17 component visual specs.
- `EXPERIENCE.md` (354 lines) — behavioral spine: information architecture (9 surfaces), voice/tone, component behavior, state patterns (per-surface state coverage + state-to-feedback matrix), interaction primitives, accessibility floor, responsive model, and 9 key flows.

Both cite the PRD as their source (`sources:` frontmatter references `prd.md` + `prd-validation-report.md`). **Note:** the UX package is intentionally **spine-only — no mockups/wireframes by design** (stated explicitly in `EXPERIENCE.md` §Visual reference decision).

### UX ↔ PRD Alignment

**Strong and explicit.**
- **Surfaces ↔ PRD UI Surface Inventory:** the 9 UX surfaces map onto the PRD's S1–S10 inventory — Conversation Detail (S1), Association Review (S2), AI Action Review (S3), Tenant Configuration (S5), Operational Queues (S8/S10), Audit Investigation (S9), plus Project Workspace / Files and Context / Command Surface Reference as UX decompositions.
- **Key flows ↔ PRD journeys:** the 9 UX key flows map 1:1 to the PRD's 8 user journeys + System Journey (Flow 1↔UJ1 … Flow 8↔UJ8, Flow 9↔System Journey "Governed AI execution"). Same actor names (Amira, Marc, Elena, Priya, Nora, Leo, Sofia, Ari).
- **Safety semantics carried through:** UX voice/tone, blocked-state behavior, and redaction ("Association blocked. You do not have access to this project.") directly implement PRD FR57/FR77/NFR2/NFR62 (no resource-existence leaks, user-safe message catalog).
- **Banned interactions match PRD invariants:** no hidden auto-association on ambiguity (FR4/FR68/Journey 2), no AI execution of risky actions from a plain send (FR41/FR46), no UI affordance suggesting authorization bypass (CLI/MCP parity boundary).
- **Accessibility matches NFR60–64:** WCAG 2.2 AA scoped to core workflows, non-color status, keyboard operability, reduced motion, redaction-safe export.

### UX ↔ Architecture Alignment

**Strong and explicit** — `architecture.md` directly accounts for the UX:
- **Same front-end stack:** architecture names Blazor + Fluent UI v5 (RC, via Hexalith.FrontComposer), Fluxor state, REST commands/queries + SignalR projection-nudge (arch lines 111–113, 265, 362–363) — exactly the visual/behavioral chain the UX docs assume.
- **Surface → module homes mapped:** `.UI` adapter [M0] hosts S1 conversation, S2 association, S3 approval (arch lines 630, 674–680); S5/S8–S10 admin/audit surfaces mapped to `Projections/` + UI.
- **Live-update model supports the UX state matrix:** the architecture's "SignalR nudge → re-query, never trust payload" pattern (arch 358, 509, 692, 702) backs the UX "command accepted / projection pending" partial-success states and the "background update while reading history" non-interrupting affordance.
- **Performance backs UX responsiveness:** p95 ≤ 2 s UI reads (NFR24, arch 73).
- **EN + FR localization** is acknowledged at the architecture layer (arch 367).
- **Microcopy reuse:** the architecture quotes the UX denial string verbatim (arch 567), evidence the layers were authored against each other.

### Alignment Issues / Warnings

1. **🟡 EN+FR localization is UX-originated, not a PRD FR/NFR.** French/English UI support comes from the UX layer (`EXPERIENCE.md` §Product-Specific Concerns + UX-DR45: "stakeholder discovery is French, project config outputs English"). The PRD does not enumerate localization as an FR/NFR. It **is** carried into the epics (UX-DR45 → Stories 1.18–1.21) and acknowledged in architecture — so it is covered downstream — but it is a real implementation cost (locale-aware formatting, French text-expansion handling, untranslated-machine-code discipline) that entered via UX rather than as a conscious PRD commitment. **Recommend:** product owner explicitly ratifies localization scope at PRD level so it is not silently de-scoped under pressure (or silently assumed).
2. **🟡 Spine-only UX (no mockups) is a delivery risk, not a gap.** The decision is deliberate and the mitigation is in place (binding IA/component/state/interaction/accessibility tables; epics' cross-cutting guidance makes the tables binding acceptance context — "absence of mockups is not permission to invent behavior"). But a single frontend engineer implementing 10 surfaces from prose tables with no visual reference carries interpretation risk. Carry into Step 6 readiness as a watch-item; the per-surface binding tables are the control.
3. **🟢 PRD surfaces S4 (correction), S6 (outbound approval), S7 (cross-surface attribution) are folded into other UX surfaces** rather than authored as standalone UX surfaces. This is consistent with the epics' explicit "Later-surface elaboration before increment sprint planning" note (S4→Epics 2/3, S6→Epic 6, S7→Epics 1/5). Tracked, not a gap.
4. **🟢 "ChatBot" naming/positioning** — M0 has no native chat surface (project-conversation view + review/approval surfaces only). UX, PRD ([NOTE FOR PM]), and epics all carry this consciously. Aligned and managed; a pilot-communication item, not a spec gap.
5. **🟢 Shared, acknowledged risk:** Fluent UI v5 is still RC. Both `DESIGN.md` and `architecture.md` (lines 265, 729) flag it as a pinned pre-GA dependency. Acknowledged in both layers — not a misalignment.

**Verdict:** UX documentation exists, is final, and is unusually well-aligned with both PRD and architecture (shared vocabulary, explicit source citations, surface→module mapping, microcopy reuse). No blocking misalignment. Two yellow watch-items (localization provenance, spine-only delivery risk) carry into Step 6.

_UX alignment assessment complete. Proceeding to Step 5: Epic Quality Review._

## Step 5: Epic Quality Review

All 9 epics / 107 stories read in full and validated against `create-epics-and-stories` best practices. This is a pass-2 re-validation: the predecessor 14:40 report raised **11 attention items (5 Major / 3 Minor / 3 UX)**; the `sprint-change-proposal-2026-05-30-pass-2.md` applied fixes to `epics.md` (15:03). I verified each fix landed.

### Best-Practices Compliance Checklist (per epic)

| Epic | User value | Independent (no fwd-dep) | Story sizing | Clear ACs | Entity timing | FR traceability |
|------|:---:|:---:|:---:|:---:|:---:|:---:|
| E1 Command Spine (M0) | ⚠️→✅* | ✅ | ✅ | ✅ | ✅ | ✅ |
| E2 Intake & Association (M0) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| E3 Conversation/Files (M0) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| E4 AI Mediation (M0) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| E5 CLI/MCP Parity (M1) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| E6 Outbound/Authenticity (M1) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| E7 Tenant Admin/Governance (M1) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| E8 Dashboards/Observability (M2) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| E9 Audit/Compliance/Recovery (M2) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

*E1 is a foundation/walking-skeleton epic — see Pass-2 verification below; it is correctly handled via the value-anchor invariant rather than left as a bare technical milestone.

### Special Implementation Checks

- **Starter template (architecture specifies one):** ✅ `architecture.md` mandates a Hexalith-module scaffold (sibling-template + EventStore root submodule + Aspire/DAPR topology + OpenAPI Contract Spine). **Epic 1 Story 1.1 IS that setup story** ("Scaffold the buildable Hexalith.ChatBot module"), covering `.slnx` projects, root config, submodule init (`--init`, never `--recursive` — matches repo policy), Aspire `aspire run` health, and build-green. Satisfies the requirement, with an optional-split planning note.
- **Brownfield indicators:** ✅ New module on the existing Hexalith platform. Integration points are explicit (EventStore submodule; sibling adapters `IParticipantDirectory`/`IFolderStore`; Keycloak; M365/Graph). CI/CD scaffolded in Story 1.1 (`.github/workflows/` ci + semantic-release).
- **Entity/store-creation timing:** ✅ Event-sourced (EventStore) — no upfront monolithic schema. Story 1.1 scaffolds the module skeleton; each later story introduces only the aggregates/projections/derived stores it needs (e.g., derived stores per Data Governance Surface owner-increment). No "create all tables in Story 1" anti-pattern.

### Pass-2 Fix Verification (all 11 items)

| Item | Severity | Fix applied | Verified in `epics.md` |
|------|----------|-------------|------------------------|
| MAJOR-5 | 🟠 | Epic 1 value-anchor invariant + per-story anchors 1.1–1.8 | ✅ Invariant in E1 header; 8 `Anchor:` lines present; Story 1.9 is the value proof |
| MAJOR-2 | 🟠 | Split Story 7.6 → 6 stories (7.6–7.11) | ✅ 7.6 routing / 7.7 escalation / 7.8 prioritization+grouping / 7.9 throttling+digest / 7.10 backlog alert / 7.11 rubber-stamp observable |
| MAJOR-1 | 🟠 | Keep 15 control stories (7.12–7.26), **inline** the security floor into each | ✅ Each of 7.12–7.26 carries audit-floor AC ("…no skip-audit path (FR74, FR75g)"); disable/quarantine carry FR75d two-person-rule AC; rate-limit carry FR75/NFR30 bound. No shared-floor dependency remains |
| MAJOR-4 | 🟠 | Split Story 8.2 → 8.2 telemetry / 8.3 SLO+budgets / 8.4 alerts; old 8.3 → 8.5 | ✅ Epic 8 = 5 sprint-ready stories; no multi-concern 8.2 |
| MAJOR-3 | 🟠 | Strengthen 9.7–9.13 with negative-path + evidence ACs | ✅ Each carries fail-closed-when-unauthorized + audit + partial-failure/retry + evidence-artifact ACs (e.g., 9.9 erasure proof artifact; 9.11 drill report; 9.13 per-assertion evidence) |
| MINOR-1 | 🟡 | Beneficiary on "As the system" stories | ✅ 5 `Beneficiary:` lines (2.3, 2.4, 3.14, 4.1, 4.7) |
| MINOR-2 | 🟡 | Story 1.1 optional-split planning note | ✅ Present (1.1a scaffold / 1.1b topology+CI) |
| MINOR-3 | 🟡 | Outcome-framed titles guidance (identifiers kept) | ✅ Captured as binding planning guidance |
| UX-1/2/3 | 🟡 | Cross-cutting acceptance & planning guidance subsection | ✅ Present at end of Epic List (spine-only binding tables; S4/S6/S7/S8–S10/S9 elaboration; naming; outcome titles) |

Also verified structurally: `storyCount: 107` matches 107 story headers (E1=21, E2=9, E3=14, E4=9, E5=4, E6=5, E7=27, E8=5, E9=13); Epic 7 = 7.1–7.27 and Epic 8 = 8.1–8.5 sequential with **no gaps or duplicates**.

### Findings by Severity

#### 🔴 Critical Violations
**None.** No technical-milestone epics without user value; no forward dependencies; no epic-sized stories that cannot be completed.

#### 🟠 Major Issues
**None outstanding.** All 5 Major issues from the 14:40 report were resolved by the pass-2 sprint change and verified above. Notably:
- **Forward-dependency check:** clean. Dependency flow is strictly forward (M0: E1–E4 → M1: E5–E7 → M2: E8–E9). Story-level cross-references are all backward (2.8→1.6, 4.9→3.14, 5.4→E1 shims, 6.2→6.1, 9.6→2.8/9.5). No story depends on a later story.
- **Shared-floor dependency eliminated:** the previous "15 control stories silently inherit a floor in 7.6" defect is gone — the floor is inlined per control story, so each is independently sprintable with its security ACs intact.

#### 🟡 Minor Concerns
1. **AC repetition across the fine-grained story families (intentional, with a maintenance cost).** The 15 Epic 7 control stories (7.12–7.26) and the 7 Epic 3 rendering stories (3.2–3.8) carry near-identical ACs differing by one subject/concern line. This is a *deliberate, documented* choice — Epic 7 Option A was explicitly selected by Jerome over consolidation, and the FR22/FR74 decomposition is PRD-mandated and matches the [[bmad-story-granularity-preference]] (fine-grained, independently-sprintable stories). **Not a defect.** The only residual risk is drift: a change to the inlined audit/two-person-rule floor must be propagated across 15 stories by hand. **Recommendation:** at implementation, back the floor with a single shared acceptance-test fixture / parametrized conformance test (the architecture's NetArchTest + conformance harness is the natural home) so the 15 stories stay in lock-step without manual sync.
2. **NFR traceability is cross-cutting, not per-NFR-per-story.** Release-gate NFRs are well covered by dedicated stories/ACs (NFR15a→1.4, NFR11→1.12, NFR49a→9.1, NFR50a→9.2, NFR9a→9.5, NFR46→7.8–7.11, NFR17a→2.8/9.6). But a few platform/infra and quality-gate NFRs are only implied by cross-cutting constraints rather than cited in a story AC — notably **NFR3** (encryption in transit/at rest), **NFR12** (data residency), and the **NFR65–NFR70** validation-gate cluster. These are largely Hexalith/Aspire-platform-inherited, but they are release-validation obligations. **Recommendation:** at sprint planning, attach an explicit test/validation owner to NFR3, NFR12, and NFR65–NFR70 (a one-line addition to E1's gate stories or E9) so no release-gate NFR lacks a named verification.
3. **Technical-leaning epic identifiers** ("Command Spine", "CLI & MCP") retained for stable cross-referencing — MINOR-3 consciously kept these with the mitigation that *story* titles stay outcome-framed. Documented decision, not a defect.

### Remediation Guidance Summary

No blocking remediation required. Two light, non-blocking recommendations carry into Step 6:
- **R1 (Minor):** Back the Epic 7 inlined control-floor and Epic 3 rendering-floor with a shared parametrized acceptance fixture to prevent AC drift across the fine-grained story families.
- **R2 (Minor):** Assign explicit verification owners to the cross-cutting release-gate NFRs not individually story-traced (NFR3, NFR12, NFR65–NFR70) during sprint planning.

**Verdict:** Epic/story quality is **strong and sprint-ready**. Structure (user-valued epics, forward-only dependencies, BDD ACs with negative paths, starter-template Story 1.1, FR traceability) is sound; all 11 prior attention items are resolved; only two minor, non-blocking hygiene recommendations remain.

_Epic quality review complete. Proceeding to Step 6: Final Assessment._

## Summary and Recommendations

### Overall Readiness Status

## ✅ READY for Phase 4 implementation (pass-2)

This re-validation was run because `epics.md` was revised at 15:03 (after the 14:40 report) and a second sprint-change proposal (`-pass-2`) landed at 15:05. The 14:40 report rated the package **NEEDS WORK** on the strength of **11 attention items (5 Major / 3 Minor / 3 UX)**. The pass-2 sprint change applied targeted fixes to `epics.md`, and this assessment **verifies all 11 are resolved** with no regressions to FR coverage. The planning package — PRD, UX, Architecture, Epics, and Stories — is internally consistent, fully traceable, and sprint-ready. M0 (Epic 1) can begin.

### What the re-validation confirmed

| Dimension | Result |
|-----------|--------|
| Document inventory | Complete — PRD + addendum, architecture, epics, 2 UX docs; no duplicates, no missing required docs |
| FR coverage | **111/111 (100%)** — every base FR + lettered sub-FR has exactly one primary epic; map ↔ epic declarations consistent; 0 orphans |
| PRD maturity | `status: final`; two validation cycles + adversarial review; contracts (fail-closed, idempotency, audit, authority-mapping) specified, not aspirational |
| UX alignment | Final, spine-based; 1:1 journey↔flow mapping; surfaces↔S1–S10; architecture adopts the same FrontComposer/Blazor/Fluent-v5 + SignalR stack and reuses UX microcopy |
| Epic/story quality | 9 epics / **107 stories**; user-valued; forward-only dependency flow (M0→M1→M2); BDD ACs with negative paths; starter-template Story 1.1; FR-traced |
| Prior attention items | **11/11 resolved** (5 Major, 3 Minor, 3 UX) and verified landed in the 15:03 `epics.md` |

### Critical Issues Requiring Immediate Action

**None.** No critical or major blockers remain. No technical-milestone epics, no forward dependencies, no uncovered FRs, no unresolved document conflicts.

### Non-blocking recommendations (fold into sprint planning — not gates)

1. **R1 — Prevent AC drift across the fine-grained story families.** The 15 Epic 7 control stories (7.12–7.26) and 7 Epic 3 rendering stories (3.2–3.8) carry a deliberately inlined shared floor (audit / two-person-rule / rate-limit-bounds; WCAG/actor-attribution). Back that floor with a single parametrized acceptance-test fixture / conformance check so the 15+7 stories stay in lock-step without manual sync. (This finest-grained backlog is the intended trade-off of Option A and matches the fine-grained-story preference — keep it, just guard it mechanically.)
2. **R2 — Name verification owners for the cross-cutting release-gate NFRs not individually story-traced** — specifically **NFR3** (encryption in transit/at rest), **NFR12** (data residency), and the **NFR65–NFR70** validation-gate cluster. They're largely Hexalith/Aspire-platform-inherited, but each is a release obligation; a one-line owner on E1's gate stories or E9 closes the loop.
3. **R3 — Ratify EN+FR localization at PRD level.** French/English support entered via UX (UX-DR45) and is carried in Story 1.20, but is not a PRD FR/NFR. Confirm the product owner accepts the localization cost so it isn't silently assumed or silently cut.

### Watch-items to carry into delivery (awareness, already mitigated)

- **Spine-only UX (no mockups)** — deliberate; mitigated by binding IA/component/state/interaction/accessibility tables. One frontend engineer building 10 surfaces from tables carries interpretation risk; the binding-tables rule is the control.
- **Open assumptions A1–A11 must not be silently closed** — several (A1 M365 grant, A9/A9a evaluation-dataset cardinality, A10 RPO/RTO drill, A11 pilot thresholds) gate real scope and have named owners + revisit conditions. Keep them open and tracked.
- **Fluent UI v5 is still RC** — pinned pre-GA in both DESIGN.md and architecture; do not upgrade casually.
- **Increment dependency order is fixed (M0 → M1 → M2)** — M1 must not start before M0 is stable in pilot; M2 not before M1's parity invariant is in production.

### Recommended Next Steps

1. **Proceed to M0 sprint planning** — run `bmad-sprint-planning` for Chatbot to generate `sprint-status.yaml` from the corrected 107-story epics (no existing sprint-status to migrate). Begin Epic 1 with **Story 1.1 (module scaffold)** — the starter-template setup story and the spine that unblocks the Story 1.9 value proof.
2. **Fold R1/R2/R3 into the first sprint's definition-of-ready** rather than treating them as pre-implementation blockers.
3. **Author the load-bearing ADRs before their dependent work** (per the epics' ADR list): idempotency, schema-evolution/upcasting, audit-two-phase, gateway, saga, and the **WORM audit backing technology** + **audit↔execute transactionality spike** before M0 closes.
4. **Keep assumptions A1–A11 on the risk register** with their named owners; confirm A1 (M365 pilot grant) and A9a (evaluation-dataset ≥500 labeled by M0) early, since both gate M0 acceptance.

### Final Note

This re-validation examined the full planning package across 6 steps and found **0 critical and 0 major outstanding issues** — the 11 attention items from the 14:40 report are all resolved in the 15:03 `epics.md`. Three minor, non-blocking recommendations (R1–R3) remain and belong in sprint planning, not in a pre-implementation gate. The package is **READY** for Phase 4; M0/Epic 1 can begin. These findings can be used to refine sprint-planning inputs, or you may proceed as-is.

---

**Assessment:** Implementation Readiness (pass-2 re-validation)
**Date:** 2026-05-30
**Assessor:** Implementation Readiness PM (BMAD `bmad-check-implementation-readiness`)
**For:** Jerome
**Supersedes:** `implementation-readiness-report-2026-05-30.md` (14:40) — stale relative to the 15:03 `epics.md`
**Scope:** PRD (`prd.md` + `addendum.md`), `architecture.md`, `epics.md` (15:03 / 107 stories), UX (`DESIGN.md` + `EXPERIENCE.md`)
