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

## Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**
- None found at root matching `*prd*.md`

**Sharded / Packaged Documents:**
- Folder: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/`
  - `prd.md` - 191501 bytes, modified `2026-05-30 11:24`
  - `addendum.md` - 19497 bytes, modified `2026-05-30 11:24`
  - `prd-validation-report.md` - 24324 bytes, modified `2026-05-30 11:24`
  - `validation-report.md` - 20762 bytes, modified `2026-05-30 11:24`
  - Supporting review files present
  - No `index.md` found

### Architecture Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/architecture.md` - 64604 bytes, modified `2026-05-30 11:24`

**Sharded Documents:**
- None found

### Epics Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/epics.md` - 179500 bytes, modified `2026-05-30 13:58`

**Sharded Documents:**
- None found

### UX Files Found

**Whole Documents:**
- None found at root matching `*ux*.md`

**Sharded / Packaged Documents:**
- Folder: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/`
  - `DESIGN.md` - 15515 bytes, modified `2026-05-30 11:24`
  - `EXPERIENCE.md` - 36042 bytes, modified `2026-05-30 11:24`
  - `validation-report.md` - 11569 bytes, modified `2026-05-30 11:24`
  - Supporting review files present
  - No `index.md` found

### Issues Found

**Critical duplicate conflicts:** None found.

**Warnings:**
- PRD is packaged under a folder rather than a root `*prd*.md` file and has no `index.md`; selected canonical files are `prd.md` and `addendum.md`.
- UX is packaged under a folder rather than a root `*ux*.md` file and has no `index.md`; selected canonical files are `DESIGN.md` and `EXPERIENCE.md`.

### Documents Selected For Assessment

- PRD: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- PRD Addendum: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Epics: `_bmad-output/planning-artifacts/epics.md`
- UX Design: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- UX Experience: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`

## Step 2: PRD Analysis

### PRD Files Read

All files in `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/` were read:

- `.decision-log.md`
- `addendum.md`
- `prd-validation-report.md`
- `prd.md`
- `reconcile-product-brief.md`
- `review-adversarial-general.md`
- `review-adversarial-v2.md`
- `review-rubric-v2.md`
- `review-rubric.md`
- `validation-report.html`
- `validation-report.md`

### Functional Requirements

Total functional entries extracted: 111 entries, consisting of 96 primary FRs plus 15 lettered FR extensions.

FR1: The system can capture authorized mailbox events as project collaboration inputs.

FR2: The system can preserve source email identity, thread identity, mailbox identity, sender, recipients, timestamps, and attachment references.

FR3: The system can associate incoming email with an existing project using deterministic evidence.

FR4: The system can detect ambiguous project association and route it to human review.

FR5: Authorized users can review candidate projects with visible evidence, confidence state, reason codes, and the consequences of each available decision.

FR6: Authorized users can choose a candidate project, reject all candidates, defer association, mark an item as needing review, and provide an optional decision note.

FR7: Authorized users can correct a previously selected project association.

FR8: The system can record association decisions, corrections, rejections, deferrals, retries, and skipped items.

FR9: Tenant administrators can configure project association rules, evidence requirements, and the confidence thresholds `T_high` and `T_low`. The score domain, signals fed, safe defaults, calibration protocol, and guardrails on threshold changes are defined in `addendum.md` §Confidence Thresholds. Both knobs are security-sensitive per the Tenant Policy Schema.

FR10: The system can preserve original email context when association is rejected, deferred, failed, skipped, or awaiting review.

FR11: The system can expose deterministic association reasons and confidence inputs in machine-readable form for UI, CLI, MCP, audit, and test verification.

FR12: Authorized users can compare candidate project evidence side by side when resolving ambiguous association.

FR13: The system can resolve internal and external email participants to tenant-scoped parties.

FR14: Authorized users can identify unresolved participants for review.

FR15: External participants can contribute project context through email without requiring MVP external portal access.

FR16: The system can enforce tenant and project authorization before exposing project candidates, files, conversations, approvals, commands, or audit details.

FR17: The system can block unresolved or unauthorized actors from accessing project files, creating task requests, triggering commands, or sending outbound communication.

FR18: Tenant administrators can configure governed mailbox participation rules.

FR19: Authorized administrators can configure service-client access for CLI, MCP, background workers, mailbox events, and AI actors.

FR20: The system can record consent or lawful-basis metadata where tenant policy requires it for external participants, retained email content, attachments, and AI processing.

FR21: Authorized users can view email-derived messages as project conversation context.

FR22: The system can represent associated email, participants, attachments, decisions, approvals, failures, and AI outcomes in the project context.

FR23: Authorized users can inspect why an email belongs to a project, including source evidence, confidence signals, human decisions, and later corrections.

FR24: Authorized users can see association, attachment, task, approval, command, failure, retry, and next-action status for a project conversation.

FR25: The system can keep project conversation context separate across tenants and projects.

FR26: The system can distinguish informational project context from actionable requests.

FR27: The system can distinguish system-generated summaries from source evidence so users do not confuse AI interpretation with original email, attachment, or command facts.

FR28: The system can preserve visible human-review history for each email, attachment, approval, AI action, and command.

FR29: The system can capture attachments from associated project email.

FR30: The system can store captured attachments in governed project folders.

FR31: Authorized users can inspect attachment capture and storage status.

FR32: The system can prevent unauthorized actors from viewing attachment metadata or content.

FR33: The system can make authorized project files available as scoped AI context only through explicit authorization, policy checks, and auditable context packaging.

FR34: The system can represent attachment states including captured, pending, unavailable, rejected, unsafe, failed, and retryable.

FR35: The system can detect candidate task or action intent from authorized project conversation actors and preserve the source message evidence.

FR36: Authorized users can review captured task intent before governed action. The review surface displays the data contract from FR35 plus the source message in full and the available state transitions per FR37/FR38.

FR37: Authorized users can convert captured task intent into a governed task or action request. Conversion creates the proposal record per FR41 / `addendum.md` §Risk Classifier and links it to the source task-intent record. Conversion is itself an audited operation.

FR38: Authorized users can mark captured task intent as not actionable, duplicate, already handled, or out of scope. Each of these is a terminal state for the task-intent record; duplicate additionally links the predecessor task-intent ID.

FR39: The system can classify AI action requests by risk.

FR40: The system can allow low-risk AI assistance when tenant policy and project authorization permit it.

FR41: The system can require approval before AI actions that modify project state, expose files, send external communication, create or assign tasks, invoke tools, or act on behalf of a participant.

FR42: Authorized users can approve or reject proposed AI actions after reviewing the action summary, affected project resources, external recipients, sender authority, risk classification, and expected outcome.

FR43: The system can execute approved AI actions only through allowlisted governed commands.

FR44: Authorized users can inspect AI action proposals, approvals, denials, executions, failures, and outcomes.

FR45: Authorized users can preview outbound communication, file access, command execution, and AI-generated changes before approval or execution.

FR46: The system can refuse or block unsafe AI, automation, command, or mailbox requests that exceed tenant policy, project authorization, sender authority, or approved command scope.

FR47: Authorized users can create outbound project email drafts within approved project and sender authority.

FR48: The system can distinguish draft-only, authenticated-user send, shared-mailbox send, send-on-behalf, and approved service-send authority. The mapping rule from M365 / Exchange permission models to ChatBot sender-authority classes is defined in `addendum.md` §Inbound Message Authenticity; conflicts fail closed.

FR48a: Inbound provider authenticity passthrough. Every inbound message intake event records the M365 / Exchange DMARC, DKIM, and SPF verdicts as supplied by the provider; ChatBot does not re-verify.

FR48b: Inbound header inspection. The mailbox adapter parses `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender` headers and records disagreements as intake metadata.

FR48c: On-behalf-of disambiguation. When a delegated-send relationship is expressed by the provider, the recorded sender authority is the delegate's identity, with the principal preserved as `principal_for`; outbound actions follow the same rule.

FR48d: External-sender posture. Messages from senders with no resolved tenant party are flagged `external_sender = true`; tenant policy `mailbox.authenticity-strictness` controls whether external-sender messages auto-associate, route to NeedsReview, or fail closed.

FR49: The system can require approval before outbound project communication leaves the project boundary.

FR50: The system can preserve proposed content, approved content, recipients, sender authority, project context, requester, approver, and decision outcome in approval records.

FR51: Tenant administrators can configure mailbox integration settings and monitored mailbox patterns.

FR52: Tenant administrators can configure AI action policy for low-risk and approval-required actions.

FR53: Tenant administrators can review mailbox permission status and degraded mailbox processing states.

FR54: Compliance or support reviewers can investigate association decisions, approval decisions, command outcomes, and risky AI actions.

FR55: The system can produce audit records for security-sensitive association, participant, file, approval, command, AI, retry, and duplicate-handling events.

FR55a: Cross-tenant isolation in derived stores. Vector indexes, embedding stores, prompt-context caches, candidate-ranking caches, and other derived stores holding tenant-derived material must enforce tenant isolation by construction; cross-tenant queries must fail below the application layer.

FR56: Authorized users can query audit records by tenant, actor, command, resource, decision, reason, correlation, and time context.

FR57: The system can hide unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details.

FR58: Authorized administrators or reviewers can access operational support for tenant data retention, export, and deletion workflows.

FR59: The system can propagate correlation context across mailbox intake, project association, file handling, approval, AI mediation, command execution, audit, UI, CLI, and MCP.

FR60: The system can preserve source evidence used for association, authorization, approval, rejection, refusal, correction, retry, and audit investigation with retention boundaries and redaction behavior.

FR61: The system can maintain versioned policy snapshots used for association, authorization, approval, AI action classification, and command execution decisions.

FR62: Authorized users can add human notes or resolution rationale to association, participant, approval, retry, quarantine, and correction decisions.

FR63: Authorized users can supersede reversible human decisions where policy permits while preserving the original decision in audit history.

FR64: The system can detect duplicate mailbox delivery and avoid duplicate project artifacts.

FR65: The system can retry failed mailbox, attachment, association, approval, command, and projection work where retry is valid.

FR66: The system can surface terminal and non-terminal failure states to authorized users.

FR67: The system can expose mailbox health, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, and audit projection status.

FR68: The system can fail closed when project association, participant identity, tenant scope, authorization, audit writing, or required dependencies cannot be resolved.

FR69: Authorized users can view and manage queues for ambiguous associations, unresolved participants, pending approvals, failed ingestion, failed attachment handling, and retryable operations.

FR70: Authorized users can assign or claim review items that require human resolution.

FR71: Authorized users can see the next required human action for an email, task intent, attachment, approval, or failed operation.

FR72: The system can notify authorized users when review, approval, failure, degraded mailbox, quarantine, or retry states require attention.

FR73: Tenant administrators can configure notification routing and escalation rules for unresolved review, approval, degraded, quarantine, and failure states.

FR74: Authorized administrators can disable, quarantine, or rate-limit mailbox sources, service clients, AI actors, or command capabilities producing unsafe, invalid, excessive, or policy-violating activity.

FR75: Authorized administrators can configure per-tenant rate limits, quotas, and circuit breakers for mailbox processing, AI mediation, command execution, and outbound communication.

FR75a: A `tenant-admin` role holds the union of every admin scope in FR75b-FR75g; finer-grained admin roles hold proper subsets. Admin assignment is security-sensitive, audited, and cannot be performed by service clients or AI actors.

FR75b: See-only scopes. Admins can read operational queue summaries, health/status enums, and aggregate metrics across tenant projects without per-project membership; per-item detail requires per-project authority.

FR75c: Operate scopes. Admins can perform queue-level operations on items they can see-only; admins cannot mutate project-level records through queue-level operations.

FR75d: Policy scope. `policy-admin` can mutate Tenant Policy Schema knobs; security-sensitive knobs additionally require second-admin approval and documented audit justification.

FR75e: Mailbox scope. `mailbox-admin` can configure mailbox patterns, routing rules, and provider-credential connections, but cannot read mailbox content or decide associations.

FR75f: Compliance scope. `compliance-admin` can read audit records across the tenant subject to per-project redaction, trigger investigations, and configure retention windows within NFR49a bounds, but cannot operate on workflow items.

FR75g: Audit obligation on every admin action. Every admin operation, including read-only access to operational dashboards above an aggregation threshold, produces an audit event; `tenant-admin` does not bypass NFR15a or NFR50a.

FR76: The system can present review items with clear available actions, disabled-action reasons, and next-step guidance based on the item state and user authorization.

FR77: The system can explain refusal, blocked action, degraded mailbox, failed attachment, failed command, and authorization-denied states in user-safe language without exposing restricted evidence.

FR78: Authorized users can filter, sort, and prioritize operational queues by age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action.

FR79: The system can show stale, waiting, blocked, and escalation-needed states for review queues and long-running operations.

FR80: UI, CLI, and MCP users can retrieve long-running operation status including operation identity, current state, retry count, partial outputs, safe next actions, terminal reason, and correlation context.

FR81: Authorized UI users can perform the core governed email-to-project workflow operations.

FR81a: Shared command pipeline architectural invariant. Every state-mutating operation from UI, CLI, MCP, service client, AI actor, or background worker passes through a single command-handling pipeline applying authentication, tenant-scope binding, authorization, risk classification, approval gate, idempotency check, command execution, audit emission, and projection update in that order.

FR82: Authorized CLI users can inspect, associate, reject, defer, correct, retry, approve, execute, check status, and query audit for the same governed workflow.

FR83: Authorized MCP clients can access the same governed workflow operations for AI-agent and automation use.

FR84: The system can return equivalent authorization outcomes and state transitions across UI, CLI, and MCP. This verifies FR81a, rather than enforcing parity independently.

FR85: The system can identify whether an action originated from UI/API, CLI, MCP, background worker, mailbox event, or AI actor. Origin is attached at the adapter boundary and travels into the audit envelope.

FR86: Contract tests must verify the FR81a invariant for each surface: equivalent inputs must produce the same Command record after canonical normalization.

FR87: The system can define canonical lifecycle states for email intake items, participant resolution, attachment handling, approvals, AI actions, command executions, and audit projection.

FR88: The system can validate inbound and outbound workflow state transitions against an explicit state model.

FR89: The system can reject invalid state transitions and record the rejected transition, actor, reason, and correlation context.

FR90: The system can expose idempotency keys and stable resource identifiers for mailbox events, email messages, attachments, approvals, commands, retries, outbound communication, and audit records.

FR91: The system can separate immutable source records from derived project projections and rebuild derived projections from source records when needed.

FR91a: Correction propagation contract. When a user corrects an association, every derived store that referenced the original association must be invalidated and rebuilt; AI actions cannot use corrected project context until invalidation completes; audit records predecessor, correction, and per-store invalidation outcome.

FR92: Authorized product or QA users can maintain internal evaluation datasets derived from consented, redacted, or synthetic project examples with expected outcomes, redaction expectations, and regression result history.

FR93: The system can provide tenant-scoped test fixtures or sandbox data for validating mailbox intake, association, authorization, attachment handling, approval, AI mediation, command execution, and audit behavior.

FR94: The system can expose measurable operational outcomes for ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag.

FR95: The system can simulate or replay representative mailbox events for authorized QA or support investigation without sending external communication or mutating production project state.

FR95a: Replay isolation contract. Replay/simulation runs execute against a dedicated test tenant, intercept outbound adapters, record replay run IDs in audit, exclude replay from production audit queries and audit-completeness measurement, and run a nightly probe to ensure no replay produced production outbound-trace records.

FR96: The system can make recorded correction decisions available as future association evidence only when tenant policy permits, the evidence remains explainable, and users can inspect why it influenced a match.

### Non-Functional Requirements

Total non-functional entries extracted: 77 entries, consisting of 70 primary NFRs plus 7 lettered NFR extensions.

NFR1: All command and query operations must enforce tenant, actor, role, project, and resource authorization before returning data or mutating state.

NFR2: Unauthorized users, CLI clients, MCP clients, AI actors, service clients, and mailbox events must receive redacted failure responses that do not reveal restricted project names, file metadata, candidate evidence, audit details, or tenant data.

NFR3: Email content, attachments, AI prompts, AI outputs, audit records, tokens, policy snapshots, logs, traces, backups, and evaluation datasets must be encrypted in transit and at rest using tenant-appropriate key management and separation controls.

NFR4: Secrets, mailbox credentials, service-client credentials, CLI credentials, MCP credentials, AI-tool credentials, and AI provider credentials must not be exposed in logs, traces, CLI output, MCP responses, audit payloads, support bundles, or user-facing diagnostics.

NFR5: Microsoft 365 / Exchange permissions, service-client credentials, CLI credentials, MCP credentials, and AI-tool credentials must follow least-privilege scope and support revocation without broad fallback access.

NFR6: Authorization, policy, and identity caches must have bounded staleness and revocation-sensitive invalidation; default maximum staleness is 5 minutes for ordinary policy changes and 60 seconds for explicit revocation events, verified by automated revocation tests.

NFR7: Security-sensitive operations must fail closed when identity, tenant scope, authorization, audit readiness, policy evaluation, or required command validation is unavailable.

NFR8: AI actors must operate only through explicitly authorized project scope, files, tools, commands, and policy-defined authority.

NFR9: AI prompts, retrieved context, generated outputs, tool results, and summaries must be tenant/project scoped, redacted where policy requires, logged according to retention policy, and blocked from training, telemetry, or reuse outside authorized boundaries unless explicitly configured.

NFR9a: Derived-store cross-tenant isolation. Vector indexes, embedding stores, prompt-context caches, and candidate-ranking caches must be partitioned per tenant at the store level; native cross-tenant queries must fail at the storage layer and nightly synthetic probes are stop-ship.

NFR10: Logs, metrics, traces, support bundles, and test artifacts must pass secret and sensitive-data redaction checks before export or external sharing.

NFR11: Cross-tenant isolation testing must have zero tolerance for unauthorized data exposure across project candidates, evidence, files, summaries, prompts, CLI output, MCP payloads, logs, metrics, traces, and audit views.

NFR12: Data residency and region boundaries must be defined for stored email content, attachments, AI context, audit records, logs, backups, and evaluation datasets before tenant onboarding when a tenant or deployment profile specifies residency.

NFR13: Mailbox intake, attachment capture, association decisions, approvals, command execution, outbound communication, notifications, and audit projection must be idempotent per operation with a stable idempotency key, replay window, conflict response, and equivalent final observable state for repeated equivalent inputs.

NFR13a: Per-operation idempotency contract. Key composition, replay window, equivalence rule, and conflict response for each operation class are specified in `addendum.md` §Idempotency Keys; new operation classes must extend the table before shipment.

NFR14: Duplicate mailbox delivery must not create duplicate project messages, attachments, task intents, approvals, commands, notifications, outbound emails, or audit decisions.

NFR15: Invalid workflow state transitions must be rejected before mutation with deterministic error behavior and an audit event; if audit storage is unavailable, every state-mutating transition fails closed.

NFR15a: Fail-Closed Contract. Fail-closed is enforced by construction at every durable-write path, including M365 intake, deterministic and ambiguous association, correction, AI action proposal, approval decision, command execution, outbound send, tenant policy mutation, and allowlist mutation. No path has an "audit unavailable -> continue" branch.

NFR16: Risky AI actions, external sends, command execution, and project-file context packaging must not execute unless approval state, policy snapshot, actor authority, input contract validation, and audit readiness are verified.

NFR17: Partial failures must leave affected workflow items in visible, recoverable states such as pending, retryable, failed, quarantined, or needs review.

NFR17a: Correction propagation latency. Correction propagation completes within p95 <= 10 minutes for M0/M1 and p95 <= 60 minutes for M2; SLO breaches surface `correction-delayed` and trigger a P2 incident.

NFR18: Retry policy must specify retryable versus terminal errors, maximum attempts, backoff, jitter, dead-letter criteria, manual recovery actions, and operator-visible terminal reasons per workflow type.

NFR19: Background workers and async processors must support at-least-once delivery safely through idempotency, concurrency control, lease or lock expiry, and poison-message handling.

NFR20: Queue processing must prevent starvation across tenants, mailboxes, projects, and workflow item types while respecting priority, rate limits, and circuit breakers.

NFR21: File and attachment processing must enforce malware or unsafe-content policy, size limits, type restrictions, scan status, quarantine behavior, and safe failure states before project or AI exposure.

NFR22: Non-AI review, association, approval, retry, and audit workflows must continue during AI provider outage when required non-AI dependencies are available.

NFR23: Tenant or deployment profile operating baselines must be documented, versioned, reviewed at least quarterly, and used as the reference for latency, backlog, recovery, alerting, validation dataset size, and capacity expectations.

NFR24: User-facing project conversation, queue, status, and audit lookups must meet a default p95 response target of 2 seconds under the MVP operating baseline unless a stricter target is defined.

NFR25: Ambiguous association candidate generation must complete within 10 seconds p95 under the MVP operating baseline, or return a pending/manual-review status with retrievable operation identity and safe next actions.

NFR26: CLI and MCP operations that trigger long-running work must return operation identity and current status within 5 seconds p95 and must not hold the client connection longer than 30 seconds without returning a retrievable status.

NFR27: Queue views must support filtering, sorting, pagination, and prioritization with a default page size no greater than 100 items and server-side filters for age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action.

NFR28: Operational latency metrics must include percentile distribution, error rate, retry rate, queue age, saturation indicators, and audit projection lag.

NFR29: Tenant-level rate limits, quotas, and circuit breakers must protect mailbox processing, AI mediation, command execution, outbound communication, UI/API, CLI, and MCP use.

NFR30: Backlogs in one tenant, mailbox, project, service client, AI actor, or command surface must not degrade unrelated tenants or unrelated workflow sources where isolation is technically possible.

NFR31: Microsoft 365 / Exchange integration must tolerate revoked permissions, expired tokens, throttling, backoff, partial access, duplicate events, delayed delivery, webhook replay, subscription expiry, and permission drift without silently broadening access.

NFR32: UI/API, CLI, MCP, workers, webhook/event handlers, persisted events, audit records, projections, and replay fixtures must use contract-verifiable responses and events with stable identifiers, status codes, reason codes, state names, redaction semantics, correlation context, and equivalent authorization outcomes.

NFR33: API, CLI, MCP, event, audit, projection, and state-model contracts must support backward-compatible evolution or explicit versioning, deprecation policy, and migration paths for breaking changes.

NFR34: Integration requests and events must carry correlation context across mailbox intake, file handling, association, approval, command execution, AI mediation, audit, UI/API, CLI, MCP, workers, and webhooks.

NFR35: Configuration and policy changes must be auditable, versioned, rollback-capable for non-destructive settings, and applied consistently to new work without silently changing completed decision records.

NFR36: Time-based behavior for workflow decisions, audit records, retries, approvals, retention, evidence freshness, and SLA calculations must use server-side UTC timestamps, preserve source timestamps and timezone context where relevant, and convert to tenant-local display only at presentation boundaries.

NFR37: Authorized operators must be able to observe mailbox health, backlog, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, service-client failures, AI mediation failures, command failures, and audit projection lag.

NFR38: User-visible status must be separated from privileged diagnostic detail and exposed according to authorization level.

NFR39: The system must provide actionable status for degraded, stale, waiting, blocked, escalation-needed, failed, retryable, and terminal workflow states.

NFR40: Degraded, blocked, failed, and waiting states must be communicated in user-appropriate language with next-action guidance drawn from the FR77 message catalog; uncategorized user-visible raw-error states must be `0` per release.

NFR41: Degraded dependencies must be isolated to the narrowest identified scope among tenant, mailbox, project, operation, service client, workflow item, or command surface; incident status must state affected scope and dependency within 5 minutes when monitoring is available.

NFR42: During degraded operation, every authorized user-facing surface displays current state enum, affected scope, responsible owner role, and next safe action; synthetic checks fail if any element is missing.

NFR42a: SLOs for ingestion latency, candidate generation latency, ambiguous-resolution time, command latency, audit projection lag, retry exhaustion rate, duplicate suppression rate, mailbox failure rate, approval queue p95 age, and AI mediation latency must be published in the per-tenant operational view and in `addendum.md` §Operating Baselines during M2.

NFR43: Alerting and synthetic health checks must be non-invasive, tenant-safe, and tied to documented thresholds including subscription expiry within 7 days, retry exhaustion, audit projection lag above 5 minutes, approval items older than 2 business days, and authorization failure spikes above tenant baseline.

NFR44: Runbook-ready diagnostics for any workflow item must include correlation ID, tenant ID, mailbox ID, workflow item ID, current state, last transition, retry count, failure reason code, and next safe action; 100 sampled items per week must each render complete diagnostics.

NFR45: Support diagnostics must be shareable through redacted support bundles preserving correlation, state, and reason context without exposing restricted tenant, project, participant, file, message, or audit evidence.

NFR46: The system must prevent approval fatigue with measurable prioritization, grouping, suppression/rate ceiling, backlog SLO, and observables. Queue ordering uses risk-class, authority-of-affected-party, and time-in-queue; items group by requester, command, and project; user push notifications are capped at <= 8/hour and <= 30/day; >25 open approval items per reviewer alerts the tenant admin; >15% rubber-stamp approvals over 7 days triggers the fatigue revisit condition.

NFR47: Risky automation must distinguish reversible, supersedable, compensating, and irreversible actions before approval.

NFR48: Every surfaced evidence reference carries snapshot timestamp and `fresh` / `stale` / `expired` state; reviewers cannot approve against expired evidence.

NFR49: Audit records must be tamper-evident, retention-governed, redaction-aware, reconstructable, and protected by restricted modification/deletion controls limited to authorized retention workflows.

NFR49a: Tamper-evident mechanism. Audit uses append-only WORM storage with hash-chained envelopes; redaction is appended as a record, original data is preserved encrypted under a separately managed redaction key, chains verify nightly, and broken chains alert within 5 minutes.

NFR50: Audit records must include tenant, actor, actor type, command, resource, decision, reason, correlation, timestamp, policy snapshot reference, source evidence references, state-transition history, redaction decisions, idempotency key where applicable, and resulting outcome; automated tests verify required fields for 100% of security-sensitive validation-dataset events.

NFR50a: Audit completeness as production observable. Audit completeness is the fraction of state-mutating operations whose audit chain reconstructs the operation end-to-end; target is >= 99.5% per rolling 7-day tenant window, below which a P1 incident is triggered.

NFR51: Audit and diagnostic records must preserve enough context to reconstruct who acted, what was attempted, which policy applied, what evidence was used, what state transitions occurred, what was redacted, and what outcome occurred.

NFR52: The system must minimize retained email content, attachment content, prompts, outputs, diagnostics, and support bundles to the data required for the authorized workflow, audit, and tenant retention policy.

NFR53: Tenant data retention, export, and deletion workflows must distinguish source email, metadata, attachments, derived projections, AI prompts and outputs, approvals, policy snapshots, logs, backups, evaluation datasets, and audit records.

NFR54: Audit evidence must respect retention boundaries and redaction rules so evidence preservation does not become uncontrolled data storage.

NFR55: Where tenant policy or regulatory profile requires it, the system must record consent or lawful-basis metadata for external participants, retained content, attachments, and AI processing.

NFR56: Source email records, attachment records, approval history, command history, policy snapshots, and audit records must meet default MVP recovery target RPO <= 15 minutes and RTO <= 4 hours unless stricter targets apply.

NFR57: Derived projections must be rebuildable from immutable source records and audit history within the default MVP recovery target of 4 hours for the baseline validation dataset without requiring mailbox re-ingestion.

NFR58: Dependency outages must degrade only the affected tenant, mailbox, operation, service client, command surface, or workflow item when dependency ownership and routing can identify that scope.

NFR59: Resilience validation must prove degraded Graph access, expired subscriptions, AI provider outage, command execution failure, audit store unavailability, and partial attachment failure do not cause cross-tenant leakage, unauthorized state mutation, or silent data loss.

NFR60: WCAG 2.2 AA conformance is scoped per increment to existing UI surfaces. M0: ambiguous association review, AI action approval, project conversation view. M1: rejection/defer flows, approval-policy configuration UI, M1 admin operational view. M2: operational dashboards. Validation includes automated checks plus keyboard-only and screen-reader review.

NFR61: Accessibility validation must include keyboard-only review flows, screen-reader labels, focus order, non-color status indicators, and error recovery for ambiguous association and approval workflows.

NFR62: Status, failure, refusal, and authorization messages must be understandable without exposing restricted evidence or relying only on color.

NFR63: Users resolving ambiguous associations or approvals must be able to identify the next available action without reading raw audit logs.

NFR64: The UI must distinguish source evidence from AI-generated summaries so users can make review decisions from authoritative context.

NFR65: Production releases must meet documented quality gates covering tenant isolation, authorization, redaction, idempotency, state transitions, approval gates, duplicate suppression, and audit creation.

NFR66: Performance validation must prove mailbox backlog processing, queue usability, retry behavior, audit projection lag, and throttled Microsoft Graph behavior against documented tenant or deployment baselines.

NFR67: Security validation must include negative authorization tests for UI/API, CLI, MCP, background workers, mailbox events, service clients, and AI actors.

NFR68: Evaluation datasets and test fixtures must use consented, redacted, or synthetic examples with versioning, reproducibility, redaction verification, expected outcomes, and regression result history for association, authorization, duplicate handling, retry, approval, refusal, and audit behavior.

NFR69: Replay and simulation must be isolated from production mutation, external email sends, live AI tool execution, and live command execution; replay artifacts must be explicitly labeled and tenant-scoped.

NFR70: Every externally visible operation must define expected state transition, audit event, user-visible response, redaction behavior, and retry/idempotency result.

### Additional Requirements

- MVP is delivered as one release window with dependency-ordered increments: M0 vertical thesis path, M1 cross-surface parity and full governance, M2 operations/recovery/continuity.
- M0 is UI-only and covers one controlled Microsoft 365 / Exchange mailbox pattern, deterministic association, ambiguous association review, governed attachment capture, one approved AI action path, Keycloak-backed identity, fail-closed behavior, and M0 audit events.
- M1 adds CLI/MCP parity, service-client authorization, outbound draft/send, the full lifecycle matrix including `Skipped`, tenant-admin permission model, command allowlist v1, risk-classifier calibration, and inbound authenticity controls.
- M2 adds operational dashboards, continuity drill evidence, replay isolation, full idempotency contract, tamper-evident audit chain, audit-completeness observable, and derived-store cross-tenant isolation probes.
- The addendum is binding for Confidence Thresholds, Risk Classifier, Command Allowlist v0/v1, Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys, Replay Isolation, ID Evolution Contract, Inbound Message Authenticity, Authority Class Mapping, and Operating Baselines.
- Open assumptions A1-A11 are requirements constraints with named owners and revisit triggers, especially A9a evaluation dataset, A10 recovery objectives, and A11 pilot adoption thresholds.
- ChatBot owns durable derived records listed in the Data Governance Surface, including association records, candidate rankings, evidence snapshots, AI action proposals, approval records, projections, policy snapshots, lifecycle state, workflow instance maps, vector/embedding/prompt-context stores, replay traces, and operational queue projections.
- Core command and query contracts are enumerated in the PRD and must preserve actor identity, tenant scope, correlation ID, idempotency, target resource IDs, expected result codes, audit metadata, and shared authorization behavior.
- UI Surface Inventory defines S1-S10 across M0/M1/M2 and hands per-surface detail to UX.
- Functional Acceptance Guidance requires acceptance scenarios for each FR group and names high-risk matrices for FR1-FR12, FR39-FR46, FR55-FR63, and FR81-FR89.

### PRD Completeness Assessment

The PRD is complete enough for architecture and epic coverage validation: it is marked final, contains a full FR/NFR catalog, includes addendum-backed contracts for previously weak areas, and has traceability from journeys to FR/NFR groups. The strongest readiness evidence is the M0/M1/M2 sequencing, fail-closed NFR15a contract, FR81a shared pipeline invariant, A9a dataset ownership, Tenant Policy Schema, command allowlists, idempotency table, and UI Surface Inventory.

Residual risks to carry into coverage validation: several requirements depend on downstream architecture to preserve the addendum contracts; some acceptance-scenario matrices are explicit only for the highest-risk FR groups; and the support artifacts still record editing/cross-reference risks that were mostly repaired in the final PRD but should be checked against architecture and epics.

## Step 3: Epic Coverage Validation

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR1 | The system can capture authorized mailbox events as project collaboration inputs. | Story 2.1: Microsoft 365 mailbox intake and source-identity capture | Covered |
| FR2 | The system can preserve source email identity, thread identity, mailbox identity, sender, recipients, timestamps, and attachment references. | Story 2.1: Microsoft 365 mailbox intake and source-identity capture | Covered |
| FR3 | The system can associate incoming email with an existing project using deterministic evidence. | Story 2.3: Deterministic association scorer and candidate generation | Covered |
| FR4 | The system can detect ambiguous project association and route it to human review. | Story 2.4: Ambiguous-association detection and fail-closed routing | Covered |
| FR5 | Authorized users can review candidate projects with visible evidence, confidence state, reason codes, and the consequences of each available decision. | Story 2.5: Ambiguous association review surface (S2) | Covered |
| FR6 | Authorized users can choose a candidate project, reject all candidates, defer association, mark an item as needing review, and provide an optional decision note. | Story 2.5: Ambiguous association review surface (S2) | Covered |
| FR7 | Authorized users can correct a previously selected project association. | Story 2.7: Association correction and supersession<br>Story 2.8: Correction propagation contract | Covered |
| FR8 | The system can record association decisions, corrections, rejections, deferrals, retries, and skipped items. | Story 2.6: Association decision recording, evidence preservation, and notes | Covered |
| FR9 | Tenant administrators can configure project association rules, evidence requirements, and the confidence thresholds `T_high` and `T_low`. The score domain, signals fed, safe defaults, calibration protocol, and guardrails on threshold changes are defined in `addendum.md` §Confidence Thresholds. Both knobs are security-sensitive (per the Tenant Policy Schema): changes require tenant-admin authorization, produce an audit event, are bounded by the schema's allowed range, and cannot be made by service clients or AI actors. | Story 2.3: Deterministic association scorer and candidate generation | Covered |
| FR10 | The system can preserve original email context when association is rejected, deferred, failed, skipped, or awaiting review. | Story 2.4: Ambiguous-association detection and fail-closed routing | Covered |
| FR11 | The system can expose deterministic association reasons and confidence inputs in machine-readable form for UI, CLI, MCP, audit, and test verification. | Story 2.3: Deterministic association scorer and candidate generation | Covered |
| FR12 | Authorized users can compare candidate project evidence side by side when resolving ambiguous association. | Story 2.5: Ambiguous association review surface (S2) | Covered |
| FR13 | The system can resolve internal and external email participants to tenant-scoped parties. | Story 2.2: Participant resolution and unresolved/unauthorized handling | Covered |
| FR14 | Authorized users can identify unresolved participants for review. | Story 2.2: Participant resolution and unresolved/unauthorized handling<br>Story 3.3: Participant rendering in the conversation stream | Covered |
| FR15 | External participants can contribute project context through email without requiring MVP external portal access. | Story 2.2: Participant resolution and unresolved/unauthorized handling | Covered |
| FR16 | The system can enforce tenant and project authorization before exposing project candidates, files, conversations, approvals, commands, or audit details. | Story 1.3: CommandGateway admission spine with tenant binding and authorization | Covered |
| FR17 | The system can block unresolved or unauthorized actors from accessing project files, creating task requests, triggering commands, or sending outbound communication. | Story 2.2: Participant resolution and unresolved/unauthorized handling | Covered |
| FR18 | Tenant administrators can configure governed mailbox participation rules. | Story 7.3: Mailbox-admin scope and mailbox configuration | Covered |
| FR19 | Authorized administrators can configure service-client access for CLI, MCP, background workers, mailbox events, and AI actors. | Story 5.1: Service-client identities and scoped grants | Covered |
| FR20 | The system can record consent or lawful-basis metadata where tenant policy requires it for external participants, retained email content, attachments, and AI processing. | Story 9.10: Consent and lawful-basis metadata | Covered |
| FR21 | Authorized users can view email-derived messages as project conversation context. | Story 3.1: Render email-derived project conversation (S1) | Covered |
| FR22 | The system can represent associated email, participants, attachments, decisions, approvals, failures, and AI outcomes in the project context. | Story 3.2: Associated-email rendering in the conversation stream<br>Story 3.3: Participant rendering in the conversation stream<br>Story 3.4: Attachment rendering in the conversation stream<br>Story 3.5: Association and correction decision rendering<br>Story 3.6: Approval event rendering<br>Story 3.7: Failure, retry, and blocked-state rendering<br>Story 3.8: AI outcome rendering | Covered |
| FR23 | Authorized users can inspect why an email belongs to a project, including source evidence, confidence signals, human decisions, and later corrections. | Story 3.9: "Why this project" evidence and provenance panel | Covered |
| FR24 | Authorized users can see association, attachment, task, approval, command, failure, retry, and next-action status for a project conversation. | Story 3.10: Conversation item status and next action | Covered |
| FR25 | The system can keep project conversation context separate across tenants and projects. | Story 3.1: Render email-derived project conversation (S1) | Covered |
| FR26 | The system can distinguish informational project context from actionable requests. | Story 3.11: Informational/actionable classification, AI-summary distinction, and review history | Covered |
| FR27 | The system can distinguish system-generated summaries from source evidence so users do not confuse AI interpretation with original email, attachment, or command facts. | Story 3.8: AI outcome rendering<br>Story 3.11: Informational/actionable classification, AI-summary distinction, and review history | Covered |
| FR28 | The system can preserve visible human-review history for each email, attachment, approval, AI action, and command. | Story 3.11: Informational/actionable classification, AI-summary distinction, and review history | Covered |
| FR29 | The system can capture attachments from associated project email. | Story 3.12: Attachment capture and governed-folder storage | Covered |
| FR30 | The system can store captured attachments in governed project folders. | Story 3.12: Attachment capture and governed-folder storage | Covered |
| FR31 | Authorized users can inspect attachment capture and storage status. | Story 3.13: Attachment status, states, and authorization | Covered |
| FR32 | The system can prevent unauthorized actors from viewing attachment metadata or content. | Story 3.4: Attachment rendering in the conversation stream<br>Story 3.13: Attachment status, states, and authorization | Covered |
| FR33 | The system can make authorized project files available as scoped AI context only through explicit authorization, policy checks, and auditable context packaging. | Story 3.14: Scoped AI-context packaging from authorized files | Covered |
| FR34 | The system can represent attachment states including captured, pending, unavailable, rejected, unsafe, failed, and retryable. | Story 3.13: Attachment status, states, and authorization | Covered |
| FR35 | The system can detect candidate task or action intent from authorized project conversation actors and preserve the source message evidence. | Story 3.11: Informational/actionable classification, AI-summary distinction, and review history<br>Story 4.1: Task-intent detection and data contract<br>Story 4.2: Task-intent review, conversion, and disposition | Covered |
| FR36 | Authorized users can review captured task intent before governed action. The review surface displays the data contract from FR35 plus the source message in full and the available state transitions per FR37/FR38. | Story 4.2: Task-intent review, conversion, and disposition | Covered |
| FR37 | Authorized users can convert captured task intent into a governed task or action request. Conversion creates the proposal record per FR41 / `addendum.md` §Risk Classifier and links it to the source task-intent record. Conversion is itself an audited operation. | Story 4.2: Task-intent review, conversion, and disposition | Covered |
| FR38 | Authorized users can mark captured task intent as not actionable, duplicate, already handled, or out of scope. Each of these is a terminal state for the task-intent record (the record is preserved for evaluation per A9a); duplicate additionally links the predecessor task-intent ID. | Story 4.2: Task-intent review, conversion, and disposition | Covered |
| FR39 | The system can classify AI action requests by risk. | Story 4.3: AI action risk classification | Covered |
| FR40 | The system can allow low-risk AI assistance when tenant policy and project authorization permit it. | Story 4.4: Low-risk AI assistance execution | Covered |
| FR41 | The system can require approval before AI actions that modify project state, expose files, send external communication, create or assign tasks, invoke tools, or act on behalf of a participant. | Story 4.2: Task-intent review, conversion, and disposition<br>Story 4.5: Approval gate and AI action approval surface (S3)<br>Story 7.6: Notifications, escalation, and approval-fatigue controls | Covered |
| FR42 | Authorized users can approve or reject proposed AI actions after reviewing the action summary, affected project resources, external recipients, sender authority, risk classification, and expected outcome. | Story 4.5: Approval gate and AI action approval surface (S3) | Covered |
| FR43 | The system can execute approved AI actions only through allowlisted governed commands. | Story 4.7: Allowlisted command execution | Covered |
| FR44 | Authorized users can inspect AI action proposals, approvals, denials, executions, failures, and outcomes. | Story 4.6: AI action preview and inspection | Covered |
| FR45 | Authorized users can preview outbound communication, file access, command execution, and AI-generated changes before approval or execution. | Story 4.6: AI action preview and inspection | Covered |
| FR46 | The system can refuse or block unsafe AI, automation, command, or mailbox requests that exceed tenant policy, project authorization, sender authority, or approved command scope. | Story 4.8: Refusal and safe-block behavior | Covered |
| FR47 | Authorized users can create outbound project email drafts within approved project and sender authority. | Story 6.2: Outbound draft creation within authority | Covered |
| FR48 | The system can distinguish draft-only, authenticated-user send, shared-mailbox send, send-on-behalf, and approved service-send authority. The mapping rule from M365 / Exchange permission models to ChatBot sender-authority classes is defined in `addendum.md` §Inbound Message Authenticity; the conflict case (M365 grants send-on-behalf but ChatBot grants no such authority) resolves to fail-closed (the action cannot be taken from ChatBot, even if the underlying mailbox would accept it). | Story 6.1: Sender-authority classes and M365 mapping | Covered |
| FR48a | **Inbound provider authenticity passthrough (M1).** Every inbound message intake event records the M365 / Exchange DMARC, DKIM, and SPF verdicts as-supplied by the provider. ChatBot does not re-verify; the provider is the source of truth. | Story 6.4: Inbound authenticity passthrough and header inspection | Covered |
| FR48b | **Inbound header inspection (M1).** The mailbox adapter parses `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender` headers and records disagreements between `From` / `Sender` / `Reply-To` as intake metadata. Disagreements do not block ingestion but feed the risk classifier and surface to the reviewer. | Story 6.4: Inbound authenticity passthrough and header inspection | Covered |
| FR48c | **On-behalf-of disambiguation (M1).** When a delegated-send relationship is expressed by the provider, the recorded sender authority is the delegate's identity, with the principal's identity preserved as `principal_for`. Outbound actions follow the same rule symmetrically. | Story 6.5: On-behalf-of disambiguation and external-sender posture | Covered |
| FR48d | **External-sender posture (M1).** Messages from senders with no resolved tenant party are flagged `external_sender = true`. The tenant policy `mailbox.authenticity-strictness` knob (`permissive` / `strict` / `paranoid`) controls whether external-sender messages auto-associate, route to NeedsReview, or fail closed. | Story 6.5: On-behalf-of disambiguation and external-sender posture | Covered |
| FR49 | The system can require approval before outbound project communication leaves the project boundary. | Story 6.3: Outbound approval gate and approval record | Covered |
| FR50 | The system can preserve proposed content, approved content, recipients, sender authority, project context, requester, approver, and decision outcome in approval records. | Story 6.3: Outbound approval gate and approval record | Covered |
| FR51 | Tenant administrators can configure mailbox integration settings and monitored mailbox patterns. | Story 7.3: Mailbox-admin scope and mailbox configuration | Covered |
| FR52 | Tenant administrators can configure AI action policy for low-risk and approval-required actions. | Story 7.2: Policy-admin scope, Tenant Policy Schema editor, and AI action policy | Covered |
| FR53 | Tenant administrators can review mailbox permission status and degraded mailbox processing states. | Story 7.3: Mailbox-admin scope and mailbox configuration | Covered |
| FR54 | Compliance or support reviewers can investigate association decisions, approval decisions, command outcomes, and risky AI actions. | Story 9.3: Audit query and compliance investigation surface (S9) | Covered |
| FR55 | The system can produce audit records for security-sensitive association, participant, file, approval, command, AI, retry, and duplicate-handling events. | Story 1.4: Fail-closed audit-commit seam with pre- and post-commit audit emission | Covered |
| FR55a | **Cross-tenant isolation in derived stores (M2).** Vector indexes, embedding stores, prompt-context caches, candidate-ranking caches, and any other derived store that holds material derived from tenant data must enforce tenant isolation by construction (per-tenant store partitioning or row-level tenant scoping verified at every read). Cross-tenant queries are not possible at the store-access layer, not merely filtered at the application layer. Verification: a periodic isolation probe (per NFR59) attempts cross-tenant reads through the store-access layer and asserts they fail at the layer below the application. | Story 9.5: Derived-store cross-tenant isolation | Covered |
| FR56 | Authorized users can query audit records by tenant, actor, command, resource, decision, reason, correlation, and time context. | Story 9.3: Audit query and compliance investigation surface (S9) | Covered |
| FR57 | The system can hide unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details. | Story 1.7: Versioned user-safe message catalog and redaction stage<br>Story 2.5: Ambiguous association review surface (S2) | Covered |
| FR58 | Authorized administrators or reviewers can access operational support for tenant data retention, export, and deletion workflows. | Story 9.8: Tenant export workflow | Covered |
| FR59 | The system can propagate correlation context across mailbox intake, project association, file handling, approval, AI mediation, command execution, audit, UI, CLI, and MCP. | Story 1.8: Correlation propagation and long-running operation status | Covered |
| FR60 | The system can preserve source evidence used for association, authorization, approval, rejection, refusal, correction, retry, and audit investigation with retention boundaries and redaction behavior. | Story 2.6: Association decision recording, evidence preservation, and notes | Covered |
| FR61 | The system can maintain versioned policy snapshots used for association, authorization, approval, AI action classification, and command execution decisions. | Story 1.4: Fail-closed audit-commit seam with pre- and post-commit audit emission | Covered |
| FR62 | Authorized users can add human notes or resolution rationale to association, participant, approval, retry, quarantine, and correction decisions. | Story 2.6: Association decision recording, evidence preservation, and notes | Covered |
| FR63 | Authorized users can supersede reversible human decisions where policy permits while preserving the original decision in audit history. | Story 2.7: Association correction and supersession<br>Story 3.5: Association and correction decision rendering | Covered |
| FR64 | The system can detect duplicate mailbox delivery and avoid duplicate project artifacts. | Story 2.9: Duplicate detection, retry, and failure states | Covered |
| FR65 | The system can retry failed mailbox, attachment, association, approval, command, and projection work where retry is valid. | Story 2.9: Duplicate detection, retry, and failure states | Covered |
| FR66 | The system can surface terminal and non-terminal failure states to authorized users. | Story 2.9: Duplicate detection, retry, and failure states | Covered |
| FR67 | The system can expose mailbox health, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, and audit projection status. | Story 8.1: Operational dashboards (S8/S10) | Covered |
| FR68 | The system can fail closed when project association, participant identity, tenant scope, authorization, audit writing, or required dependencies cannot be resolved. | Story 1.4: Fail-closed audit-commit seam with pre- and post-commit audit emission | Covered |
| FR69 | Authorized users can view and manage queues for ambiguous associations, unresolved participants, pending approvals, failed ingestion, failed attachment handling, and retryable operations. | Story 7.5: Operational queue management | Covered |
| FR70 | Authorized users can assign or claim review items that require human resolution. | Story 7.5: Operational queue management | Covered |
| FR71 | Authorized users can see the next required human action for an email, task intent, attachment, approval, or failed operation. | Story 2.9: Duplicate detection, retry, and failure states | Covered |
| FR72 | The system can notify authorized users when review, approval, failure, degraded mailbox, quarantine, or retry states require attention. | Story 7.6: Notifications, escalation, and approval-fatigue controls | Covered |
| FR73 | Tenant administrators can configure notification routing and escalation rules for unresolved review, approval, degraded, quarantine, and failure states. | Story 7.6: Notifications, escalation, and approval-fatigue controls | Covered |
| FR74 | Authorized administrators can disable, quarantine, or rate-limit mailbox sources, service clients, AI actors, or command capabilities producing unsafe, invalid, excessive, or policy-violating activity. | Story 7.6: Notifications, escalation, and approval-fatigue controls | Covered |
| FR75 | Authorized administrators can configure per-tenant rate limits, quotas, and circuit breakers for mailbox processing, AI mediation, command execution, and outbound communication. | Story 7.6: Notifications, escalation, and approval-fatigue controls | Covered |
| FR75a | A `tenant-admin` role holds the union of every admin scope in FR75b–FR75g; finer-grained admin roles (`mailbox-admin`, `policy-admin`, `compliance-admin`, `operations-admin`) hold proper subsets. Admin assignment itself is a security-sensitive operation that produces an audit event and cannot be performed by service clients or AI actors. | Story 7.1: Tenant-admin permission model and bounded scopes | Covered |
| FR75b | **See-only scopes:** admins can read operational queue summaries (depth, age, owner), health/status enums, and aggregate metrics (per FR67) across all tenant projects without holding per-project membership. Reading per-item detail (project name, evidence content, file metadata, audit reasons) requires per-project authority; admin role does not grant it. | Story 7.1: Tenant-admin permission model and bounded scopes | Covered |
| FR75c | **Operate scopes:** admins can perform queue-level operations (retry, requeue, quarantine, dismiss) on items they can see-only. The operation is recorded with the admin's identity, the affected items, the queue, and the reason. Admins cannot mutate project-level records (associations, files, approvals) through queue-level operations. | Story 7.1: Tenant-admin permission model and bounded scopes | Covered |
| FR75d | **Policy scope (`policy-admin`):** can mutate the Tenant Policy Schema knobs (per `addendum.md` §Tenant Policy Schema). Security-sensitive knobs additionally require a second admin approval (two-person rule) and a documented justification recorded in audit. | Story 7.2: Policy-admin scope, Tenant Policy Schema editor, and AI action policy<br>Story 7.6: Notifications, escalation, and approval-fatigue controls | Covered |
| FR75e | **Mailbox scope (`mailbox-admin`):** can configure mailbox patterns, routing rules, and provider-credential connections. Cannot read mailbox content; cannot decide associations. | Story 7.3: Mailbox-admin scope and mailbox configuration | Covered |
| FR75f | **Compliance scope (`compliance-admin`):** can read audit records across the tenant (subject to per-project redaction per NFR2), trigger investigations, configure retention windows within NFR49a bounds. Cannot operate on workflow items. | Story 7.4: Compliance-admin scope | Covered |
| FR75g | **Audit obligation on every admin action:** every admin operation, including read-only access to operational dashboards above an aggregation threshold, produces an audit event with admin identity, scope used, items affected, and timestamp. No admin operation has a "skip audit" path. The `tenant-admin` role does not bypass NFR15a or NFR50a. | Story 7.1: Tenant-admin permission model and bounded scopes<br>Story 7.6: Notifications, escalation, and approval-fatigue controls | Covered |
| FR76 | The system can present review items with clear available actions, disabled-action reasons, and next-step guidance based on the item state and user authorization. | Story 1.7: Versioned user-safe message catalog and redaction stage<br>Story 2.5: Ambiguous association review surface (S2) | Covered |
| FR77 | The system can explain refusal, blocked action, degraded mailbox, failed attachment, failed command, and authorization-denied states in user-safe language without exposing restricted evidence. | Story 1.7: Versioned user-safe message catalog and redaction stage<br>Story 3.7: Failure, retry, and blocked-state rendering<br>Story 4.8: Refusal and safe-block behavior<br>Story 8.3: Degraded-state operability and runbook diagnostics | Covered |
| FR78 | Authorized users can filter, sort, and prioritize operational queues by age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action. | Story 7.5: Operational queue management | Covered |
| FR79 | The system can show stale, waiting, blocked, and escalation-needed states for review queues and long-running operations. | Story 2.5: Ambiguous association review surface (S2) | Covered |
| FR80 | UI, CLI, and MCP users can retrieve long-running operation status including operation identity, current state, retry count, partial outputs, safe next actions, terminal reason, and correlation context. | Story 1.8: Correlation propagation and long-running operation status<br>Story 5.2: CLI adapter and workflow parity | Covered |
| FR81 | Authorized UI users can perform the core governed email-to-project workflow operations. | Story 1.9: First governed command end-to-end with surface-origin attribution | Covered |
| FR81a | **Shared command pipeline (architectural invariant).** Every state-mutating operation, regardless of originating surface (UI, CLI, MCP, service client, AI actor, background worker), passes through a single command-handling pipeline that applies authentication, tenant-scope binding, authorization, risk classification, approval gate, idempotency check, command execution, audit emission, and projection update in that order. Surface adapters translate surface-specific input into a typed Command record and hand it to the pipeline; adapters cannot replicate any pipeline stage. Parity follows by construction. The architectural detail (adapter rules, invariant violations, what does and does not count as a parity violation) is in `addendum.md` §Shared Command Pipeline. Architecture review must reject adapter designs that bypass pipeline stages, regardless of stated rationale. | Story 1.3: CommandGateway admission spine with tenant binding and authorization<br>Story 1.10: Architecture dependency fitness tests<br>Story 5.4: Cross-surface equivalence verification | Covered |
| FR82 | Authorized CLI users can inspect, associate, reject, defer, correct, retry, approve, execute, check status, and query audit for the same governed workflow. (CLI adapter is the surface-specific translation layer over the FR81a pipeline.) | Story 5.2: CLI adapter and workflow parity | Covered |
| FR83 | Authorized MCP clients can access the same governed workflow operations for AI-agent and automation use. (MCP adapter is the surface-specific translation layer over the FR81a pipeline.) | Story 5.3: MCP adapter and governed tool surface | Covered |
| FR84 | The system can return equivalent authorization outcomes and state transitions across UI, CLI, and MCP. **This is a verification of FR81a, not the enforcement mechanism**: if the pipeline invariant holds, equivalent outcomes follow by construction; if equivalent outcomes diverge across surfaces, the divergence is a defect against FR81a. | Story 5.4: Cross-surface equivalence verification | Covered |
| FR85 | The system can identify whether an action originated from UI/API, CLI, MCP, background worker, mailbox event, or AI actor. Origin is attached at the adapter boundary and travels with the Command record into the audit envelope; downstream pipeline stages cannot mutate origin. | Story 1.9: First governed command end-to-end with surface-origin attribution<br>Story 5.4: Cross-surface equivalence verification | Covered |
| FR86 | Contract tests must verify the FR81a invariant for each surface: given an equivalent input, each surface adapter must produce the same Command record (after canonical normalization). Test failure is an invariant violation, not a tolerance threshold. Contract-verifiable responses with stable error codes follow as a downstream consequence of FR81a; enforcement of parity is structural, not test-derived. | Story 1.10: Architecture dependency fitness tests<br>Story 1.11: Differential-conformance harness<br>Story 5.4: Cross-surface equivalence verification | Covered |
| FR87 | The system can define canonical lifecycle states for email intake items, participant resolution, attachment handling, approvals, AI actions, command executions, and audit projection. | Story 1.6: Canonical lifecycle state model and transition enforcement<br>Story 7.22: Command allowlist v1 and full lifecycle completion | Covered |
| FR88 | The system can validate inbound and outbound workflow state transitions against an explicit state model. | Story 1.6: Canonical lifecycle state model and transition enforcement | Covered |
| FR89 | The system can reject invalid state transitions and record the rejected transition, actor, reason, and correlation context. | Story 1.6: Canonical lifecycle state model and transition enforcement<br>Story 7.22: Command allowlist v1 and full lifecycle completion | Covered |
| FR90 | The system can expose idempotency keys and stable resource identifiers for mailbox events, email messages, attachments, approvals, commands, retries, outbound communication, and audit records. | Story 1.5: Two-altitude idempotency | Covered |
| FR91 | The system can separate immutable source records from derived project projections and rebuild derived projections from source records when needed. | Story 2.8: Correction propagation contract | Covered |
| FR91a | **Correction propagation contract (M0/M1).** When a user corrects an association (per FR7), every derived store that referenced the original association must be invalidated and rebuilt: candidate ranking, evidence snapshot, AI action proposals that consumed the misassociated context, vector index entries derived from the misassociated material (M2), and operational queue projections. The user-facing state during reindex is `correcting` (visible on the corrected item with progress indicator and estimated completion). The corrected item remains in `correcting` state until all derived stores acknowledge invalidation; AI actions cannot use the corrected project context until invalidation completes. Audit records the predecessor association, the correction, and the per-store invalidation outcome. | Story 2.8: Correction propagation contract | Covered |
| FR92 | Authorized product or QA users can maintain internal evaluation datasets derived from consented, redacted, or synthetic project examples with expected outcomes, redaction expectations, and regression result history. | Story 1.13: Tenant-scoped fixture and evaluation scaffold | Covered |
| FR93 | The system can provide tenant-scoped test fixtures or sandbox data for validating mailbox intake, association, authorization, attachment handling, approval, AI mediation, command execution, and audit behavior. | Story 1.13: Tenant-scoped fixture and evaluation scaffold | Covered |
| FR94 | The system can expose measurable operational outcomes for ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag. Exposure surface and SLO targets are defined in NFR42a (OpenTelemetry metrics published to the tenant operational dashboard in M2; intermediate exposure via the FR67 operational queues in M0/M1). | Story 8.2: SLOs, metrics, and alerting | Covered |
| FR95 | The system can simulate or replay representative mailbox events for authorized QA or support investigation without sending external communication or mutating production project state. | Story 9.4: Replay and simulation isolation | Covered |
| FR95a | **Replay isolation contract (M2).** Replay/simulation runs are architecturally separated from production: they execute against a dedicated test tenant, the outbound adapter for the test tenant intercepts every external action and records it instead of sending, and replay events carry a `replay_run_id` that is included in the audit envelope. Production audit queries default to excluding replay events; audit completeness measurement (NFR50a) excludes replay events from both numerator and denominator. A nightly automated probe asserts no replay run has ever produced a record in any production tenant's outbound-trace store; failure of the probe gates M2 release. Detailed mechanism in `addendum.md` §Replay Isolation. | Story 9.2: Audit completeness as a production observable<br>Story 9.3: Audit query and compliance investigation surface (S9)<br>Story 9.4: Replay and simulation isolation | Covered |
| FR96 | The system can make recorded correction decisions available as future association evidence only when tenant policy permits, the evidence remains explainable, and users can inspect why it influenced a match. | Story 2.7: Association correction and supersession | Covered |

### Missing Requirements

None. Every PRD FR and FR extension is referenced by at least one epic/story section.

### Coverage Statistics

- Total PRD FRs: 111
- FRs covered in epics: 111
- Coverage percentage: 100.0%
- FRs in epics but not in PRD: 0

### Coverage Assessment

The epics document provides full FR traceability. Coverage is not limited to the requirements inventory: all 111 PRD FR entries are also referenced in the epic/story body, and no extra FR identifiers appear in the epics that are absent from the PRD. The next validation risk is story quality and UX/architecture alignment, not missing FR coverage.

## Step 4: UX Alignment Assessment

### UX Document Status

Found. The UX package is present at `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/` and includes:

- `DESIGN.md` - final visual identity and component semantics
- `EXPERIENCE.md` - final information architecture, behavior, states, interactions, accessibility, responsive rules, and key flows
- `validation-report.md`, `review-accessibility.md`, and `review-rubric.md` as supporting review evidence

### UX to PRD Alignment

Aligned. The UX explicitly derives from the PRD and product brief, and it preserves the PRD's surface inventory and journey model:

- PRD S1-S10 map cleanly to UX surfaces: Project Workspace / Conversation Detail, Association Review, AI Action Review, Files and Context, Operational Queues, Audit Investigation, Tenant Configuration, and Command Surface Reference.
- UX key flows cover the PRD's user journeys and system journey: contributor AI help, ambiguous association, external-party email, correction, tenant admin configuration, CLI parity, compliance investigation, AI approval, and governed AI execution.
- UX accessibility and interaction requirements align with NFR60-NFR64: WCAG 2.2 AA, keyboard operation, live-region discipline, reduced motion, non-color status meaning, redaction-safe export, English/French localization, and responsive small-screen fallback.
- UX state patterns preserve PRD lifecycle concerns: ambiguous association, correcting state, command accepted/projection pending, retryable/terminal failures, unauthorized redaction, dependency degraded, audit projection pending, and AI proposal ready.

### UX to Architecture Alignment

Aligned. Architecture names the UX inputs, selects the required platform, and gives implementation homes to the surface set:

- Architecture uses Blazor + Fluent UI v5 through Hexalith.FrontComposer, matching `DESIGN.md` and `EXPERIENCE.md`.
- The architecture includes `Hexalith.ChatBot.UI` with M0 S1 conversation, S2 association review, S3 AI approval; M1 S4-S7; and M2 S8-S10.
- Projection/query plus SignalR re-query supports the UX requirement for live status without treating SignalR payloads as source-of-truth data.
- The Contract Spine, typed client, CommandGateway, and UI/CLI/MCP adapter boundary support the UX Command Surface Reference and parity behavior.
- Playwright E2E with axe-core is planned for M0 S1/S2/S3 and grows per increment, supporting the UX accessibility floor.

### Alignment Issues

No blocking UX alignment gaps found.

### Warnings

- UX is a spine-only handoff with no mockups or wireframes. This is intentional in `EXPERIENCE.md`, but story authors must treat the IA, component, state, interaction, accessibility, and responsive tables as binding acceptance context.
- Architecture gives detailed M0 UI homes and broad M1/M2 UI increment markers, but it does not detail every later surface at the same level as S1-S3. Carry this into M1/M2 story elaboration for S4 correction, S6 outbound approval, S7 attribution, S8/S10 operations, and S9 compliance.
- The PRD notes a naming/positioning risk: "ChatBot" may imply a native chat surface, while M0 is a project conversation view plus review/approval surfaces. UX and architecture align on no fake chat surface for M0, but pilot communication should keep this expectation explicit.

## Step 5: Epic Quality Review

### Overall Quality Assessment

The epic set is structurally coherent and traceable, with 9 epics and 100 stories across the fixed M0 → M1 → M2 sequence. No epic requires a future epic to function, and the starter-template requirement is satisfied by Story 1.1. The main readiness risks are not FR coverage gaps; they are story-quality risks where some stories are too broad, some high-risk stories are too thin, and several stories rely on inherited/shared acceptance criteria that could be missed during implementation.

### Epic Structure Validation

| Epic | User Value Focus | Independence | Quality Result |
| --- | --- | --- | --- |
| Epic 1: First Safe Governed Action & Command Spine | Borderline but acceptable. It is foundation-heavy, but it is anchored to a first user-observable governed action in Story 1.9. | Independent. It creates the safety floor used later. | Needs discipline: keep all technical stories tied to the first governed action proof. |
| Epic 2: Email Intake & Project Association | Strong user value: governed email reaches the right project or review queue. | Depends only on Epic 1 safety floor. | Good. |
| Epic 3: Project Conversation Context, Files & Attachments | Strong user value: associated email becomes usable governed project context. | Depends on Epic 2 association output, not future epics. | Good. |
| Epic 4: Governed AI Action Mediation | Strong user value: safe AI help with review and approval. | Depends on Epics 1-3. | Good. |
| Epic 5: Cross-Surface Parity — CLI & MCP | Strong value for developers/automation, though more technical. | Depends on the shared pipeline established earlier. | Good. |
| Epic 6: Outbound Communication & Inbound Authenticity | Strong user/security value. | Depends on prior governance and approval model. | Good. |
| Epic 7: Tenant Administration & Governance Policy | Strong tenant-admin/operator value. | Depends on earlier governance model. | Major story-readiness risk due to breadth and inherited criteria. |
| Epic 8: Operational Dashboards & Observability | Strong operator value. | M2 extension over earlier operational state. | One story is explicitly too broad pending split. |
| Epic 9: Tamper-Evident Audit, Compliance Investigation & Recovery | Strong compliance/recovery value. | M2 extension over earlier audit/event foundations. | Several high-risk stories are under-specified. |

### Dependency Analysis

No critical forward dependency found.

- Story 1.4 references the full WORM chain being deferred to Epic 9/M2. This is acceptable only because Story 1.4 still delivers a hash-chainable post-commit envelope and a fail-closed pre-commit audit gate in M0; it must not require Epic 9 to make M0 state writes safe.
- Story 3.14 says AI-context eligibility is inspected before Epic 4 consumes it. This is a valid backward dependency for Epic 4, not a forward dependency for Epic 3.
- No database/entity creation violation found. The plan follows EventStore/Dapr module setup and creates contract/state concerns when first needed rather than creating all domain state up front.

### Critical Violations

None found.

### Major Issues

1. **Epic 7 stories 7.7-7.21 rely on a shared acceptance floor from Story 7.6.**
   - Example: Story 7.10 "Disable service client" has one local AC, while its audit, scope, two-person-rule, and service-client/AI-actor prohibitions live in Story 7.6's shared floor.
   - Why it matters: individual stories can be pulled into a sprint without the shared floor, causing security/audit requirements to be missed.
   - Recommendation: inline the shared acceptance floor into each 7.7-7.21 story, or convert the matrix of subject/action controls into a single explicit scenario table with one implementation story per coherent control surface.

2. **Story 7.6 is too broad for implementation as written.**
   - It combines notification routing, escalation rules, approval queue prioritization, grouping, rate ceilings, digest rollup, reviewer backlog SLO, rubber-stamp-rate observability, and the shared acceptance floor for 15 later stories.
   - Recommendation: split into notification routing, escalation policy, approval queue prioritization/grouping, notification throttling/digest, reviewer backlog alerting, and rubber-stamp observable stories.

3. **Several high-risk M2 compliance/recovery stories have only one acceptance criterion.**
   - Examples: Story 9.7 data-class inventory, 9.8 tenant export, 9.9 deletion/erasure, 9.10 consent/lawful-basis metadata, 9.11 continuity drill, 9.12 projection rebuild, 9.13 scoped outage degradation.
   - Why it matters: these are compliance, recovery, export, erasure, and outage stories; one happy-path AC is not enough.
   - Recommendation: add negative-path and evidence ACs for authorization failure, redaction behavior, audit record creation, partial failure, retry/terminal behavior, tenant isolation, and observable proof artifacts.

4. **Story 8.2 is explicitly not sprint-ready.**
   - The story itself says to split into telemetry emission, SLO publication, and tenant-safe alert wiring before sprint assignment.
   - Recommendation: perform that split before M2 sprint planning; do not hand Story 8.2 to implementation unchanged.

5. **Epic 1 is acceptable only as a foundation exception, not as a model for later epics.**
   - Story 1.1 through 1.8 are strongly technical; the epic avoids being a pure technical milestone only because Story 1.9 proves a first governed command end-to-end in the UI.
   - Recommendation: keep Story 1.9 as the epic's value anchor and make every preceding foundation story demonstrate how it unblocks or protects that first governed action.

### Minor Concerns

- Several stories use "As the system" as the actor, such as Stories 2.3, 2.4, 3.14, 4.1, and 4.7. This is acceptable for internal automated behavior, but the beneficiary should remain explicit in the ACs.
- Story 1.1 is a large setup story covering solution scaffold, root config, submodule, Aspire topology, Dapr naming, CI, and build. This meets the starter-template requirement, but it should be timeboxed or split if estimation exceeds a single story.
- Epic 1 and Epic 5 titles include technical language ("Command Spine", "CLI & MCP"). The value statements are clear enough, but story titles should continue emphasizing user/operator/security outcomes during sprint planning.

### Best Practices Checklist

| Check | Result |
| --- | --- |
| Epics deliver user value | Pass with caveat for Epic 1 foundation exception |
| Epic independence | Pass |
| No forward dependencies | Pass |
| Stories appropriately sized | Partial; Stories 7.6, 8.2, and 1.1 need attention |
| Acceptance criteria are testable | Mostly pass; thin M2 compliance/recovery stories need more criteria |
| Technical setup handled correctly | Pass; Story 1.1 satisfies starter-template setup |
| Traceability maintained | Pass; all 111 FR entries covered |

## Summary and Recommendations

### Overall Readiness Status

**NEEDS WORK before full Phase 4 implementation.**

M0 can proceed only with controls: Epic 1 must remain anchored to the first governed UI command, and Story 1.1 should be split if estimation shows it cannot be completed as one story. Full implementation across M1/M2 should not proceed until the Epic 7, Epic 8, and Epic 9 story-quality issues are fixed.

### Critical Issues Requiring Immediate Action

No critical blockers were found:

- No missing PRD, architecture, epic, or UX artifact.
- No duplicate whole/sharded document conflict.
- No missing FR coverage in epics.
- No broken forward dependency where an epic requires a future epic to function.

### Issues Requiring Attention

This assessment identified **11 attention items** across **3 categories**:

- **Epic/story quality major issues: 5**
  - Story 7.6 is too broad.
  - Stories 7.7-7.21 depend on inherited/shared acceptance criteria.
  - M2 compliance/recovery stories 9.7-9.13 are too thin for their risk level.
  - Story 8.2 is explicitly not sprint-ready and must be split.
  - Epic 1 is acceptable only as a foundation exception anchored to Story 1.9.
- **Epic/story quality minor concerns: 3**
  - Several stories use "As the system" and should clarify the beneficiary.
  - Story 1.1 may exceed one implementable story.
  - Some epic/story titles lean technical and should keep user/operator/security outcomes explicit.
- **UX alignment warnings: 3**
  - UX is spine-only with no mockups; the tables must be treated as binding story context.
  - Later UI surfaces S4/S6/S7/S8-S10/S9 need more detailed story elaboration before their increments.
  - "ChatBot" naming may imply a native chat surface even though M0 intentionally avoids one.

### Recommended Next Steps

1. **Fix Epic 7 before M1 sprint planning.** Split Story 7.6 and inline or explicitly attach the shared acceptance floor to each Story 7.7-7.21 item.
2. **Split Story 8.2 before M2 planning.** Separate telemetry emission, SLO publication, and tenant-safe alert wiring.
3. **Strengthen Stories 9.7-9.13.** Add authorization, redaction, audit, failure, retry/terminal, tenant-isolation, and evidence-output criteria.
4. **Preserve the Epic 1 value anchor.** Story 1.9 must remain the proof that the foundation delivers a first safe governed action; do not allow Epic 1 to drift into platform work with no user-observable outcome.
5. **Use the UX spine as acceptance input.** Surface stories must import the UX IA/state/accessibility/responsive requirements, not treat the lack of mockups as permission to invent behavior.

### Final Note

The planning set is strong on traceability: 111 of 111 PRD FR entries are covered by epic/story sections, and PRD, UX, architecture, and epics are aligned at the surface and architecture level. The remaining work is story hardening, not product-definition repair.

**Assessment date:** 2026-05-30  
**Assessor:** Codex using `bmad-check-implementation-readiness`
