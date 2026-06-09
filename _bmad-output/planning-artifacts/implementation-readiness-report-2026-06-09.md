---
project: Chatbot
date: 2026-06-09
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
    - _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/m1-m2-surface-elaboration.md
    - _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md
missingDocuments: []
duplicateDocumentIssues: []
discoveryNotes:
  - PRD and UX artifacts are foldered but not sharded through index.md files.
  - Prior completed report for 2026-06-09 was reinitialized for this workflow run after user confirmation.
requirementExtraction:
  prdStatus: found
  functionalRequirementsExtracted: 111
  nonFunctionalRequirementsExtracted: 77
  additionalRequirementSources:
    - _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
epicCoverageValidation:
  prdFunctionalRequirements: 111
  claimedEpicCoverageEntries: 111
  coveredFunctionalRequirements: 111
  missingFunctionalRequirements: 0
  extraEpicClaims: 0
  coveragePercentage: 100.00
  traceabilityStatus: verified-against-fr-coverage-map
uxAlignment:
  uxStatus: found
  prdAlignment: aligned
  architectureAlignment: aligned-with-open-streaming-decision
  alignmentIssues: 1
  warnings: 2
epicQualityReview:
  criticalViolations: 2
  majorIssues: 5
  minorConcerns: 3
  epicCount: 10
  storyHeadingsDetected: 121
  assignableStoriesDetected: 119
  nonAssignablePlanningContainers: 2
finalAssessment:
  overallReadinessStatus: not-ready
  criticalIssuesRequiringAction: 2
  majorIssuesRequiringPlanningAttention: 5
  minorOrWarningFindings: 5
  totalIssueFindingsRecorded: 12
  assessor: Codex
---
# Implementation Readiness Assessment Report

**Date:** 2026-06-09
**Project:** Chatbot

## Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` (191,986 bytes, modified 2026-06-09 17:27)
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` (22,783 bytes, modified 2026-06-09 21:54)

**Sharded Documents:**
- None using the configured `*/index.md` sharded pattern.

**Foldered Documents:**
- Folder: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/`
  - `prd.md`
  - `addendum.md`
  - validation and review files present but not selected as primary assessment inputs

### Architecture Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/architecture.md` (69,746 bytes, modified 2026-06-09 17:27)

**Sharded Documents:**
- None.

### Epics & Stories Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/epics.md` (213,050 bytes, modified 2026-06-09 21:54)

**Sharded Documents:**
- None.

### UX Design Files Found

**Whole Documents:**
- None matching direct `*ux*.md`.

**Sharded Documents:**
- None using the configured `*/index.md` sharded pattern.

**Foldered Documents:**
- Folder: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/`
  - `DESIGN.md` (15,574 bytes, modified 2026-06-05 12:13)
  - `EXPERIENCE.md` (36,101 bytes, modified 2026-06-05 12:13)
  - `m1-m2-surface-elaboration.md` (4,253 bytes, modified 2026-06-05 12:13)
  - `epic10-chat-surface-elaboration.md` (4,422 bytes, modified 2026-06-09 17:27)
  - validation and review files present but not selected as primary assessment inputs

### Issues Found

- No critical duplicate whole-vs-sharded document conflicts found.
- No required document category appears missing.

### Confirmed Documents For Assessment

- PRD: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- PRD addendum: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Epics and stories: `_bmad-output/planning-artifacts/epics.md`
- UX design:
  - `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
  - `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
  - `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/m1-m2-surface-elaboration.md`
  - `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md`

## Step 2: PRD Analysis

### Source Files Read

- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` — read completely.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` — read completely and treated as binding implementation context.

### Functional Requirements

Total FRs: 111

#### Functional Acceptance Guidance

### Functional Acceptance Guidance

Each FR group must be decomposed into acceptance scenarios before story implementation. At minimum, each scenario must state:

- The actor and command surface.
- Preconditions for tenant, project, party, mailbox, policy, and authorization state.
- Required input fields, source identifiers, idempotency key, and correlation context.
- Expected state transition using the canonical lifecycle model.
- Expected user-visible response and redaction behavior.
- Required audit event fields.
- Retry, duplicate, failure, and invalid-transition behavior where applicable.
- Cross-surface parity expectations for UI/API, CLI, and MCP where the operation is in the parity set.

Acceptance scenarios must cover at least one happy path, one authorization failure, one ambiguous or deferred state, one retry or idempotency case, one audit verification, and one redaction case for every FR group in the catalog that follows.

High-risk FR groups require explicit acceptance scenario matrices before implementation stories are considered ready:

| FR group | Minimum scenario coverage before story creation |
| --- | --- |
| FR1-FR12 - Project email intake and association | Deterministic association from the first controlled mailbox pattern, ambiguous candidate review, reject all, defer, correction, duplicate delivery, unauthorized project reference, cross-tenant signal suppression, source evidence display, and audit lookup. This depends on A1, A2, and A9. |
| FR39-FR46 - AI action mediation | Low-risk read-only assistance, approval-required outbound/file-exposing action, denied cross-tenant or unauthorized-file request, unsupported MVP action, mixed-risk request, approval rejection, approval revision, command failure, and audit reconstruction. This depends on A5 and A8. |
| FR55-FR63 - Audit and governance | Required audit field presence, redacted audit view for restricted actors, policy snapshot retrieval, source evidence retention, superseded human decision, support investigation, export/delete operational support, and audit-store unavailable fail-closed behavior. This depends on A6. |
| FR81-FR89 - Cross-surface parity and state model | UI/API, CLI, and MCP association/status/audit parity; invalid transition rejection; equivalent redaction and error codes; command-surface attribution; long-running status retrieval; and replay of the same scenario across at least one machine surface. This depends on A3. |

#### Functional Requirements Extracted

### Project Email Intake and Association

- FR1: The system can capture authorized mailbox events as project collaboration inputs.
- FR2: The system can preserve source email identity, thread identity, mailbox identity, sender, recipients, timestamps, and attachment references.
- FR3: The system can associate incoming email with an existing project using deterministic evidence.
- FR4: The system can detect ambiguous project association and route it to human review.
- FR5: Authorized users can review candidate projects with visible evidence, confidence state, reason codes, and the consequences of each available decision.
- FR6: Authorized users can choose a candidate project, reject all candidates, defer association, mark an item as needing review, and provide an optional decision note.
- FR7: Authorized users can correct a previously selected project association.
- FR8: The system can record association decisions, corrections, rejections, deferrals, retries, and skipped items.
- FR9: Tenant administrators can configure project association rules, evidence requirements, and the confidence thresholds `T_high` and `T_low`. The score domain, signals fed, safe defaults, calibration protocol, and guardrails on threshold changes are defined in `addendum.md` §Confidence Thresholds. Both knobs are security-sensitive (per the Tenant Policy Schema): changes require tenant-admin authorization, produce an audit event, are bounded by the schema's allowed range, and cannot be made by service clients or AI actors.
- FR10: The system can preserve original email context when association is rejected, deferred, failed, skipped, or awaiting review.
- FR11: The system can expose deterministic association reasons and confidence inputs in machine-readable form for UI, CLI, MCP, audit, and test verification.
- FR12: Authorized users can compare candidate project evidence side by side when resolving ambiguous association.

### Participants, Identity, and Authorization

- FR13: The system can resolve internal and external email participants to tenant-scoped parties.
- FR14: Authorized users can identify unresolved participants for review.
- FR15: External participants can contribute project context through email without requiring MVP external portal access.
- FR16: The system can enforce tenant and project authorization before exposing project candidates, files, conversations, approvals, commands, or audit details.
- FR17: The system can block unresolved or unauthorized actors from accessing project files, creating task requests, triggering commands, or sending outbound communication.
- FR18: Tenant administrators can configure governed mailbox participation rules.
- FR19: Authorized administrators can configure service-client access for CLI, MCP, background workers, mailbox events, and AI actors.
- FR20: The system can record consent or lawful-basis metadata where tenant policy requires it for external participants, retained email content, attachments, and AI processing.

### Project Conversation and Context

- FR21: Authorized users can view email-derived messages as project conversation context.
- FR22: The system can represent associated email, participants, attachments, decisions, approvals, failures, and AI outcomes in the project context.
  - **Decomposition guidance for story authoring:** FR22 has seven first-class concerns. For story authoring, decompose into seven sub-stories — one per concern (associated-email rendering, participant rendering, attachment rendering, decision rendering, approval rendering, failure rendering, AI-outcome rendering). Each sub-story inherits the §S1 surface from §UI Surface Inventory and is acceptance-tested independently.
- FR23: Authorized users can inspect why an email belongs to a project, including source evidence, confidence signals, human decisions, and later corrections.
  - **Accept when:** the "why" panel for any associated email displays, at minimum: the originating signal class (explicit identifier / mailbox routing rule / thread identifier / human selection / correction), the matched value (e.g., project alias text, routing rule name, thread root message ID), the confidence score, the threshold band (`auto` / `ambiguous` / `fail-closed`) the score fell into, the decision actor (system or named user), the decision timestamp, and links to any superseding correction with its own evidence panel.
- FR24: Authorized users can see association, attachment, task, approval, command, failure, retry, and next-action status for a project conversation.
- FR25: The system can keep project conversation context separate across tenants and projects.
- FR26: The system can distinguish informational project context from actionable requests.
  - **Accept when:** every email surfaced in the project conversation carries a visible classification badge `informational` or `actionable`; `actionable` items additionally surface the detected intent (per FR35) and the next-action affordance (review / capture / dismiss). The classification is derived from the same tag+heuristic kernel as the risk classifier and is reproducible for a given input.
- FR27: The system can distinguish system-generated summaries from source evidence so users do not confuse AI interpretation with original email, attachment, or command facts.
  - **Accept when:** AI-generated content is visually distinct (typographic treatment + label `AI summary`), is preceded by a one-line provenance string (`Generated by <model+version> at <timestamp> from <source-evidence-IDs>`), and can be collapsed to reveal the source evidence directly. Source evidence display is the default; AI summaries are opt-in to expand. WCAG 2.2 AA non-color status applies (the distinction does not rely on color alone).
- FR28: The system can preserve visible human-review history for each email, attachment, approval, AI action, and command.

### Files and Attachments

- FR29: The system can capture attachments from associated project email.
- FR30: The system can store captured attachments in governed project folders.
- FR31: Authorized users can inspect attachment capture and storage status.
- FR32: The system can prevent unauthorized actors from viewing attachment metadata or content.
- FR33: The system can make authorized project files available as scoped AI context only through explicit authorization, policy checks, and auditable context packaging.
- FR34: The system can represent attachment states including captured, pending, unavailable, rejected, unsafe, failed, and retryable.

### Task Intent and AI Action Mediation

Risk classification defaults:

| Risk class | Default outcome | Examples | Required controls |
| --- | --- | --- | --- |
| Low-risk read-only | Allow only when tenant policy and project authorization permit it. | Summarize already-associated project conversation, list visible status, explain candidate evidence already visible to the actor. | Project scope, actor authorization, policy snapshot, source evidence references, audit record. |
| Approval-required | Pause for authorized human approval before execution. | Draft or send external email, expose file content in generated output, create or assign a task, mutate project state, invoke an external tool, act on behalf of a participant. | Action preview, affected resources, recipients or destination, sender authority, approver identity, approval decision, command allowlist, audit record. |
| Denied | Refuse and audit when policy or authorization blocks the action. | Cross-tenant access, unauthorized files, unresolved project association, unresolved actor identity, unapproved sender authority, command outside allowlist. | Safe refusal message, redacted reason, policy or authorization reference, audit record when security-sensitive. |
| Unsupported | Decline or route to manual handling when the product does not support the action in MVP. | Full task lifecycle automation, autonomous project creation, broad document intelligence, arbitrary third-party workflow execution. | Clear unsupported-state response, optional task-intent capture, no project mutation unless separately approved. |

Mixed requests inherit the strictest applicable risk class. For example, a request that combines read-only summarization with outbound drafting is approval-required. A request that includes any denied operation is denied or split only when the denied portion can be safely separated and audited.

- FR35: The system can detect candidate task or action intent from authorized project conversation actors and preserve the source message evidence.
  - **Data contract.** A captured task-intent record includes, at minimum: `tenant_id`, `project_id`, `source_message_id`, `requester_party_id`, `detected_intent_summary` (≤ 280 chars), `detected_action_kind` (enum: `request-information` / `request-action` / `request-decision` / `inform-only`), `source_evidence_offsets` (the message offsets/substrings that produced the detection), `kernel_version`, `confidence_score` (in `[0.0, 1.0]`, same domain as `addendum.md` §Risk Classifier), `detected_at`, `state` (per FR36–FR38). Detection precision/recall targets are calibrated against the A9a evaluation dataset's `risky-ai-candidate` and `actionable` labels; the target is precision ≥ 80% and recall ≥ 75% by M0 release, ratcheting to ≥ 90% / ≥ 85% by M1 release. [ASSUMPTION A9a]
- FR36: Authorized users can review captured task intent before governed action. The review surface displays the data contract from FR35 plus the source message in full and the available state transitions per FR37/FR38.
- FR37: Authorized users can convert captured task intent into a governed task or action request. Conversion creates the proposal record per FR41 / `addendum.md` §Risk Classifier and links it to the source task-intent record. Conversion is itself an audited operation.
- FR38: Authorized users can mark captured task intent as not actionable, duplicate, already handled, or out of scope. Each of these is a terminal state for the task-intent record (the record is preserved for evaluation per A9a); duplicate additionally links the predecessor task-intent ID.
- FR39: The system can classify AI action requests by risk.
- FR40: The system can allow low-risk AI assistance when tenant policy and project authorization permit it.
- FR41: The system can require approval before AI actions that modify project state, expose files, send external communication, create or assign tasks, invoke tools, or act on behalf of a participant.

[NOTE FOR PM] FR41 + FR52 (tenant admins configure AI action policy) create a tension with NFR46 (prevent approval fatigue). The MVP default is approval-required for the six risky action classes above; tenant admins can downgrade `low-risk-allowed` per-tenant. This errs toward fatigue in the early pilot, on the assumption that AI action volume is low. If pilot data shows approval queue depth growing super-linearly with usage (NFR46 observable: rubber-stamp rate `> 15%` in a rolling 7-day window), the tuning move is to ratchet `tenant-policy.ai-action.low-risk-allowed` to `true` for the action classes whose review consistently approves without revision, not to add coarse-grained policy shortcuts. Revisit at the M1 → M2 increment boundary against pilot telemetry.
- FR42: Authorized users can approve or reject proposed AI actions after reviewing the action summary, affected project resources, external recipients, sender authority, risk classification, and expected outcome.
  - **Accept when:** the approval surface for any pending AI action displays, at minimum: the proposed command name (from the current allowlist version), the input files (each rendered as a tappable evidence reference with redaction state), the proposed outbound recipients if any, the sender authority class the action would use (per `addendum.md` §Inbound Message Authenticity), the risk classification with the input tuple that produced it (per `addendum.md` §Risk Classifier), the policy snapshot ID, the expected post-state (resource changes, side effects, audit events that will be emitted), and the approver's available decisions: `approve` / `reject` / `request-revision` / `cancel`. Approval requires the user to have authority for the action's risk class; the surface disables `approve` with a reason string when the user lacks authority.
- FR43: The system can execute approved AI actions only through allowlisted governed commands.
- FR44: Authorized users can inspect AI action proposals, approvals, denials, executions, failures, and outcomes.
- FR45: Authorized users can preview outbound communication, file access, command execution, and AI-generated changes before approval or execution.
- FR46: The system can refuse or block unsafe AI, automation, command, or mailbox requests that exceed tenant policy, project authorization, sender authority, or approved command scope.

### Outbound Communication

- FR47: Authorized users can create outbound project email drafts within approved project and sender authority.
- FR48: The system can distinguish draft-only, authenticated-user send, shared-mailbox send, send-on-behalf, and approved service-send authority. The mapping rule from M365 / Exchange permission models to ChatBot sender-authority classes is defined in `addendum.md` §Inbound Message Authenticity; the conflict case (M365 grants send-on-behalf but ChatBot grants no such authority) resolves to fail-closed (the action cannot be taken from ChatBot, even if the underlying mailbox would accept it).
- FR48a — **Inbound provider authenticity passthrough (M1).** Every inbound message intake event records the M365 / Exchange DMARC, DKIM, and SPF verdicts as-supplied by the provider. ChatBot does not re-verify; the provider is the source of truth.
- FR48b — **Inbound header inspection (M1).** The mailbox adapter parses `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender` headers and records disagreements between `From` / `Sender` / `Reply-To` as intake metadata. Disagreements do not block ingestion but feed the risk classifier and surface to the reviewer.
- FR48c — **On-behalf-of disambiguation (M1).** When a delegated-send relationship is expressed by the provider, the recorded sender authority is the delegate's identity, with the principal's identity preserved as `principal_for`. Outbound actions follow the same rule symmetrically.
- FR48d — **External-sender posture (M1).** Messages from senders with no resolved tenant party are flagged `external_sender = true`. The tenant policy `mailbox.authenticity-strictness` knob (`permissive` / `strict` / `paranoid`) controls whether external-sender messages auto-associate, route to NeedsReview, or fail closed.
- FR49: The system can require approval before outbound project communication leaves the project boundary.
- FR50: The system can preserve proposed content, approved content, recipients, sender authority, project context, requester, approver, and decision outcome in approval records.

### Admin, Governance, and Audit

- FR51: Tenant administrators can configure mailbox integration settings and monitored mailbox patterns.
- FR52: Tenant administrators can configure AI action policy for low-risk and approval-required actions.
- FR53: Tenant administrators can review mailbox permission status and degraded mailbox processing states.
- FR54: Compliance or support reviewers can investigate association decisions, approval decisions, command outcomes, and risky AI actions.
- FR55: The system can produce audit records for security-sensitive association, participant, file, approval, command, AI, retry, and duplicate-handling events.
- FR55a — **Cross-tenant isolation in derived stores (M2).** Vector indexes, embedding stores, prompt-context caches, candidate-ranking caches, and any other derived store that holds material derived from tenant data must enforce tenant isolation by construction (per-tenant store partitioning or row-level tenant scoping verified at every read). Cross-tenant queries are not possible at the store-access layer, not merely filtered at the application layer. Verification: a periodic isolation probe (per NFR59) attempts cross-tenant reads through the store-access layer and asserts they fail at the layer below the application.
- FR56: Authorized users can query audit records by tenant, actor, command, resource, decision, reason, correlation, and time context.
- FR57: The system can hide unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details.
- FR58: Authorized administrators or reviewers can access operational support for tenant data retention, export, and deletion workflows.
- FR59: The system can propagate correlation context across mailbox intake, project association, file handling, approval, AI mediation, command execution, audit, UI, CLI, and MCP.
- FR60: The system can preserve source evidence used for association, authorization, approval, rejection, refusal, correction, retry, and audit investigation with retention boundaries and redaction behavior.
- FR61: The system can maintain versioned policy snapshots used for association, authorization, approval, AI action classification, and command execution decisions.
- FR62: Authorized users can add human notes or resolution rationale to association, participant, approval, retry, quarantine, and correction decisions.
- FR63: Authorized users can supersede reversible human decisions where policy permits while preserving the original decision in audit history.

### Reliability, Failure Handling, and Operations

- FR64: The system can detect duplicate mailbox delivery and avoid duplicate project artifacts.
- FR65: The system can retry failed mailbox, attachment, association, approval, command, and projection work where retry is valid.
- FR66: The system can surface terminal and non-terminal failure states to authorized users.
- FR67: The system can expose mailbox health, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, and audit projection status.
  - **Accept when:** each surfaced queue/health view renders, at minimum: the queue/health name, current depth or status enum (per NFR43), oldest item age, owner role for triage, and a link to the per-item detail (which carries the FR23 / FR42-grade detail panels). Status enums are stable strings (`healthy` / `degraded` / `failed` / `unknown`), not derived from counts. The view refreshes within the bounded staleness in NFR6 and shows the freshness timestamp per NFR48.
- FR68: The system can fail closed when project association, participant identity, tenant scope, authorization, audit writing, or required dependencies cannot be resolved.
- FR69: Authorized users can view and manage queues for ambiguous associations, unresolved participants, pending approvals, failed ingestion, failed attachment handling, and retryable operations.
- FR70: Authorized users can assign or claim review items that require human resolution.
- FR71: Authorized users can see the next required human action for an email, task intent, attachment, approval, or failed operation.
- FR72: The system can notify authorized users when review, approval, failure, degraded mailbox, quarantine, or retry states require attention.
- FR73: Tenant administrators can configure notification routing and escalation rules for unresolved review, approval, degraded, quarantine, and failure states.
- FR74: Authorized administrators can disable, quarantine, or rate-limit mailbox sources, service clients, AI actors, or command capabilities producing unsafe, invalid, excessive, or policy-violating activity.
  - **Decomposition guidance for story authoring:** FR74 packs five subject classes (mailbox / service client / AI actor / command capability / outbound) × three actions (disable / quarantine / rate-limit). For story authoring, decompose into per-(subject × action) stories. Disable and quarantine are security-sensitive admin operations (per FR75d two-person rule); rate-limit is a standard policy mutation.
- FR75: Authorized administrators can configure per-tenant rate limits, quotas, and circuit breakers for mailbox processing, AI mediation, command execution, and outbound communication.

#### Tenant-Admin Permission Model (FR75a–FR75g, M1)

The tenant admin is not a superuser. Admin scope is bounded so the "no admin/debug bypass" promise (per NFR1, NFR2, NFR7) holds against the operational dashboards admins need.

- FR75a: A `tenant-admin` role holds the union of every admin scope in FR75b–FR75g; finer-grained admin roles (`mailbox-admin`, `policy-admin`, `compliance-admin`, `operations-admin`) hold proper subsets. Admin assignment itself is a security-sensitive operation that produces an audit event and cannot be performed by service clients or AI actors.
- FR75b — **See-only scopes:** admins can read operational queue summaries (depth, age, owner), health/status enums, and aggregate metrics (per FR67) across all tenant projects without holding per-project membership. Reading per-item detail (project name, evidence content, file metadata, audit reasons) requires per-project authority; admin role does not grant it.
- FR75c — **Operate scopes:** admins can perform queue-level operations (retry, requeue, quarantine, dismiss) on items they can see-only. The operation is recorded with the admin's identity, the affected items, the queue, and the reason. Admins cannot mutate project-level records (associations, files, approvals) through queue-level operations.
- FR75d — **Policy scope (`policy-admin`):** can mutate the Tenant Policy Schema knobs (per `addendum.md` §Tenant Policy Schema). Security-sensitive knobs additionally require a second admin approval (two-person rule) and a documented justification recorded in audit.
- FR75e — **Mailbox scope (`mailbox-admin`):** can configure mailbox patterns, routing rules, and provider-credential connections. Cannot read mailbox content; cannot decide associations.
- FR75f — **Compliance scope (`compliance-admin`):** can read audit records across the tenant (subject to per-project redaction per NFR2), trigger investigations, configure retention windows within NFR49a bounds. Cannot operate on workflow items.
- FR75g — **Audit obligation on every admin action:** every admin operation, including read-only access to operational dashboards above an aggregation threshold, produces an audit event with admin identity, scope used, items affected, and timestamp. No admin operation has a "skip audit" path. The `tenant-admin` role does not bypass NFR15a or NFR50a.
- FR76: The system can present review items with clear available actions, disabled-action reasons, and next-step guidance based on the item state and user authorization.
  - **Accept when:** every action affordance on a review item is in one of three visible states — `enabled` / `disabled-with-reason` / `not-applicable-hidden`. Disabled actions render a one-line reason from a finite set (`insufficient-authority` / `state-not-permitted` / `dependency-degraded` / `awaiting-other-actor` / `policy-blocked`); the reason is not derived from raw error text. Next-step guidance points to the responsible role or the action the user can take, never to "contact support."
- FR77: The system can explain refusal, blocked action, degraded mailbox, failed attachment, failed command, and authorization-denied states in user-safe language without exposing restricted evidence.
  - **Accept when:** every refusal / blocked / degraded / failed / denied state surfaces a message drawn from a versioned message catalog with: a stable message code, a user-safe headline ≤ 80 characters, a one-sentence reason that does not name unauthorized projects/files/parties/audit details (per NFR2), and a safe next-action affordance (retry / escalate / dismiss / request access). Restricted detail is preserved in the audit record but never in the user-facing surface.
- FR78: Authorized users can filter, sort, and prioritize operational queues by age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action.
- FR79: The system can show stale, waiting, blocked, and escalation-needed states for review queues and long-running operations.
- FR80: UI, CLI, and MCP users can retrieve long-running operation status including operation identity, current state, retry count, partial outputs, safe next actions, terminal reason, and correlation context.

### Cross-Surface Command Parity

- FR81: Authorized UI users can perform the core governed email-to-project workflow operations.
- FR81a — **Shared command pipeline (architectural invariant).** Every state-mutating operation, regardless of originating surface (UI, CLI, MCP, service client, AI actor, background worker), passes through one command spine. The ChatBot admission layer applies authentication, tenant-scope binding, authorization, risk classification, approval gate, coarse idempotency, and a pre-commit audit gate before dispatching to EventStore for fine idempotency, command execution, event publication, projection update, and post-commit audit emission. Surface adapters translate surface-specific input into a typed Command record and hand it to the pipeline; adapters cannot replicate any pipeline stage. Parity follows by construction. The architectural detail (adapter rules, invariant violations, what does and does not count as a parity violation) is in `addendum.md` §Shared Command Pipeline. Architecture review must reject adapter designs that bypass pipeline stages, regardless of stated rationale.
- FR82: Authorized CLI users can inspect, associate, reject, defer, correct, retry, approve, execute, check status, and query audit for the same governed workflow. (CLI adapter is the surface-specific translation layer over the FR81a pipeline.)
- FR83: Authorized MCP clients can access the same governed workflow operations for AI-agent and automation use. (MCP adapter is the surface-specific translation layer over the FR81a pipeline.)
- FR84: The system can return equivalent authorization outcomes and state transitions across UI, CLI, and MCP. **This is a verification of FR81a, not the enforcement mechanism**: if the pipeline invariant holds, equivalent outcomes follow by construction; if equivalent outcomes diverge across surfaces, the divergence is a defect against FR81a.
- FR85: The system can identify whether an action originated from UI/API, CLI, MCP, background worker, mailbox event, or AI actor. Origin is attached at the adapter boundary and travels with the Command record into the audit envelope; downstream pipeline stages cannot mutate origin.
- FR86: Contract tests must verify the FR81a invariant for each surface: given an equivalent input, each surface adapter must produce the same Command record (after canonical normalization). Test failure is an invariant violation, not a tolerance threshold. Contract-verifiable responses with stable error codes follow as a downstream consequence of FR81a; enforcement of parity is structural, not test-derived.

### Workflow State, Contracts, and Testability

- FR87: The system can define canonical lifecycle states for email intake items, participant resolution, attachment handling, approvals, AI actions, command executions, and audit projection.
- FR88: The system can validate inbound and outbound workflow state transitions against an explicit state model.
- FR89: The system can reject invalid state transitions and record the rejected transition, actor, reason, and correlation context.
- FR90: The system can expose idempotency keys and stable resource identifiers for mailbox events, email messages, attachments, approvals, commands, retries, outbound communication, and audit records.
- FR91: The system can separate immutable source records from derived project projections and rebuild derived projections from source records when needed.
- FR91a — **Correction propagation contract (M0/M1).** When a user corrects an association (per FR7), every derived store that referenced the original association must be invalidated and rebuilt: candidate ranking, evidence snapshot, AI action proposals that consumed the misassociated context, vector index entries derived from the misassociated material (M2), and operational queue projections. The user-facing state during reindex is `correcting` (visible on the corrected item with progress indicator and estimated completion). The corrected item remains in `correcting` state until all derived stores acknowledge invalidation; AI actions cannot use the corrected project context until invalidation completes. Audit records the predecessor association, the correction, and the per-store invalidation outcome.
- NFR17a — **Correction propagation latency.** Correction propagation completes within **p95 ≤ 10 minutes** for M0/M1 (no vector index dependency) and **p95 ≤ 60 minutes** for M2 (including vector reindex). Items still propagating beyond the SLO surface a `correction-delayed` state with the responsible owner role and the next safe action. Failure to propagate any derived store within the SLO is a P2 incident. [ASSUMPTION A11: M2 vector-reindex SLO is a starter value calibrated against pilot data volumes during M2.]
- FR92: Authorized product or QA users can maintain internal evaluation datasets derived from consented, redacted, or synthetic project examples with expected outcomes, redaction expectations, and regression result history.
- FR93: The system can provide tenant-scoped test fixtures or sandbox data for validating mailbox intake, association, authorization, attachment handling, approval, AI mediation, command execution, and audit behavior.
- FR94: The system can expose measurable operational outcomes for ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag. Exposure surface and SLO targets are defined in NFR42a (OpenTelemetry metrics published to the tenant operational dashboard in M2; intermediate exposure via the FR67 operational queues in M0/M1).
- FR95: The system can simulate or replay representative mailbox events for authorized QA or support investigation without sending external communication or mutating production project state.
- FR95a — **Replay isolation contract (M2).** Replay/simulation runs are architecturally separated from production: they execute against a dedicated test tenant, the outbound adapter for the test tenant intercepts every external action and records it instead of sending, and replay events carry a `replay_run_id` that is included in the audit envelope. Production audit queries default to excluding replay events; audit completeness measurement (NFR50a) excludes replay events from both numerator and denominator. A nightly automated probe asserts no replay run has ever produced a record in any production tenant's outbound-trace store; failure of the probe gates M2 release. Detailed mechanism in `addendum.md` §Replay Isolation.
- FR96: The system can make recorded correction decisions available as future association evidence only when tenant policy permits, the evidence remains explainable, and users can inspect why it influenced a match.

### Non-Functional Requirements

Total NFRs: 77

#### Non-Functional Requirements Extracted

## Non-Functional Requirements

### Security and Privacy

- NFR1: All command and query operations must enforce tenant, actor, role, project, and resource authorization before returning data or mutating state.
- NFR2: Unauthorized users, CLI clients, MCP clients, AI actors, service clients, and mailbox events must receive redacted failure responses that do not reveal restricted project names, file metadata, candidate evidence, audit details, or tenant data.
- NFR3: Email content, attachments, AI prompts, AI outputs, audit records, tokens, policy snapshots, logs, traces, backups, and evaluation datasets must be encrypted in transit and at rest using tenant-appropriate key management and separation controls; release validation must verify TLS for external transport, encrypted storage for each persisted data class, and no plaintext export of protected content in logs, traces, support bundles, or backups.
- NFR4: Secrets, mailbox credentials, service-client credentials, CLI credentials, MCP credentials, AI-tool credentials, and AI provider credentials must not be exposed in logs, traces, CLI output, MCP responses, audit payloads, support bundles, or user-facing diagnostics.
- NFR5: Microsoft 365 / Exchange permissions, service-client credentials, CLI credentials, MCP credentials, and AI-tool credentials must follow least-privilege scope and support revocation without broad fallback access.
- NFR6: Authorization, policy, and identity caches must have bounded staleness and revocation-sensitive invalidation for mailbox permissions, service clients, users, AI actors, and command scopes; the default MVP maximum staleness is 5 minutes for ordinary policy changes and 60 seconds for explicit revocation events, verified by automated revocation tests.
- NFR7: Security-sensitive operations must fail closed when identity, tenant scope, authorization, audit readiness, policy evaluation, or required command validation is unavailable.
- NFR8: AI actors must operate only through explicitly authorized project scope, files, tools, commands, and policy-defined authority.
- NFR9: AI prompts, retrieved context, generated outputs, tool results, and summaries must be tenant/project scoped, redacted where policy requires, logged according to retention policy, and blocked from training, telemetry, or reuse outside authorized boundaries unless explicitly configured; validation must prove every AI context package contains tenant ID, project ID, source evidence references, policy snapshot ID, redaction decision, retention class, and provider reuse setting before model or tool invocation.
- NFR9a — **Derived-store cross-tenant isolation (M2).** Vector indexes, embedding stores, prompt-context caches, and candidate-ranking caches must be partitioned per tenant at the store level (not the application level). The verification rule: a cross-tenant query through the store's native API must fail at the storage layer; an application bug that omits a tenant filter must not produce a cross-tenant read. Tests run nightly with synthetic cross-tenant probe inputs; failure of the probe is a stop-ship defect. See FR55a.
- NFR10: Logs, metrics, traces, support bundles, and test artifacts must pass secret and sensitive-data redaction checks before export or external sharing.
- NFR11: Cross-tenant isolation testing must have zero tolerance for unauthorized data exposure across project candidates, evidence, files, summaries, prompts, CLI output, MCP payloads, logs, metrics, traces, and audit views.
- NFR12: Data residency and region boundaries must be defined for stored email content, attachments, AI context, audit records, logs, backups, and evaluation datasets before tenant onboarding when a tenant or deployment profile specifies residency; release validation must verify that each persisted data class is mapped to an approved region or explicitly marked not residency-constrained.

### Reliability and Data Integrity

- NFR13: Mailbox intake, attachment capture, association decisions, approvals, command execution, outbound communication, notifications, and audit projection must be idempotent per operation with a stable idempotency key, replay window, conflict response, and the same final observable state for repeated equivalent inputs.
- NFR13a — **Per-operation idempotency contract.** The idempotency key composition, replay window, equivalence rule, and conflict response for each operation class are specified in `addendum.md` §Idempotency Keys (eight operation classes). NFR13 is the policy; the addendum table is the contract. New operation classes added to the system must extend the table before the operation can ship.
- NFR14: Duplicate mailbox delivery must not create duplicate project messages, attachments, task intents, approvals, commands, notifications, outbound emails, or audit decisions.
- NFR15: Invalid workflow state transitions must be rejected before mutation with deterministic error behavior and an audit event. If audit storage is unavailable, **every** state-mutating transition fails closed — not only security-sensitive ones, because behavioral classification of "security-sensitive" cannot be made before the operation runs, so a misclassified operation would silently mutate state. The enumerated code paths and their fail-closed contract are in NFR15a.
- NFR15a — **Fail-Closed Contract (invariant, not behavior).** Fail-closed is enforced by construction at every code path that can write durable state. The path inventory and the fail-closed condition per path:

  | Code path | State written | Fail-closed condition |
  |---|---|---|
  | M365 mailbox intake | message, attachment, intake audit | tenant scope unresolved · audit writer down · attachment scanner down (quarantine fallback only when audit is up) |
  | Association decision (deterministic) | association record, audit event | T_high/T_low scorer error · authorization check failure · audit writer down |
  | Association decision (ambiguous, user) | association record, audit event | user lacks project authority · candidate evidence stale beyond NFR48 freshness · audit writer down |
  | Correction | correction record, derived-context invalidation, audit event | corrector lacks project ownership · projection-invalidation queue down · audit writer down |
  | AI action proposal | proposal record, audit event | risk classifier indeterminate (per `addendum.md` §Risk Classifier) · evaluation dataset unavailable for the classifier kernel · audit writer down |
  | Approval decision | approval record, audit event | reviewer lacks authority for the action's risk class · audit writer down |
  | Command execution | command result, projection, audit event | command not in current allowlist version · idempotency check fails open · authorization check failure · audit writer down |
  | Outbound send (M1+) | outbound record, audit event | sender authority mismatch (per `addendum.md` §Inbound Message Authenticity) · outbound adapter not in approved mode · audit writer down |
  | Tenant policy mutation | policy snapshot, audit event | actor not tenant-admin · proposed value outside Tenant Policy Schema bounds · audit writer down |
  | Allowlist mutation (M1+) | allowlist version, audit event | actor not security engineer (or admin in production) · evaluation dataset gate not passed · audit writer down |

  No path on this list has an "audit unavailable → continue" branch. The fail-closed mode for "audit writer down" returns a typed `AuditUnavailable` error to the caller, queues the operation intent for replay (without writing state), and emits an operator alert. Replay only resumes when the audit writer is healthy.
- NFR16: Risky AI actions, external sends, command execution, and project-file context packaging must not execute unless approval state, policy snapshot, actor authority, input contract validation, and audit readiness are verified.
- NFR17: Partial failures must leave affected workflow items in visible, recoverable states such as pending, retryable, failed, quarantined, or needs review.
- NFR18: Retry policy must specify retryable versus terminal errors, maximum attempts, backoff, jitter, dead-letter criteria, manual recovery actions, and operator-visible terminal reasons per workflow type.
- NFR19: Background workers and async processors must support at-least-once delivery safely through idempotency, concurrency control, lease or lock expiry, and poison-message handling.
- NFR20: Queue processing must prevent starvation across tenants, mailboxes, projects, and workflow item types while respecting priority, rate limits, and circuit breakers.
- NFR21: File and attachment processing must enforce malware or unsafe-content policy, size limits, type restrictions, scan status, quarantine behavior, and safe failure states before project or AI exposure.
- NFR22: Non-AI review, association, approval, retry, and audit workflows must continue during AI provider outage when their required non-AI dependencies are available; outage tests must prove users can resolve associations, approve or reject existing proposals, retry mailbox work, and query audit status without live AI calls.

### Performance and Scalability

- NFR23: Tenant or deployment profile operating baselines must be documented, versioned, reviewed at least quarterly, and used as the reference for latency, backlog, recovery, alerting, validation dataset size, and capacity expectations; each baseline version must record owner, approval date, review date, and accepted default thresholds.
- NFR24: User-facing project conversation, queue, status, and audit lookups must meet a default p95 response target of 2 seconds under the MVP operating baseline unless the tenant or deployment profile defines a stricter target; the target must be measured by synthetic checks and production APM.
- NFR25: Ambiguous association candidate generation must complete within 10 seconds p95 under the MVP operating baseline, or return a pending/manual-review status with retrievable operation identity and safe next actions.
- NFR26: CLI and MCP operations that trigger long-running work must return an operation identity and current status within 5 seconds p95 and must not hold the client connection longer than 30 seconds without returning a retrievable status response.
- NFR27: Queue views must support filtering, sorting, pagination, and prioritization with a default page size no greater than 100 items and server-side filters for age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action.
- NFR28: Operational latency metrics must include percentile distribution, error rate, retry rate, queue age, saturation indicators, and audit projection lag.
- NFR29: Tenant-level rate limits, quotas, and circuit breakers must protect mailbox processing, AI mediation, command execution, outbound communication, UI/API, CLI, and MCP use.
- NFR30: Backlogs in one tenant, mailbox, project, service client, AI actor, or command surface must not degrade unrelated tenants or unrelated workflow sources where isolation is technically possible.

### Integration and Interoperability

- NFR31: Microsoft 365 / Exchange integration must tolerate revoked permissions, expired tokens, throttling, backoff, partial access, duplicate events, delayed delivery, webhook replay, subscription expiry, and permission drift without silently broadening access.
- NFR32: UI/API, CLI, MCP, workers, webhook/event handlers, persisted events, audit records, projections, and replay fixtures must use contract-verifiable responses and events with stable identifiers, status codes, reason codes, state names, redaction semantics, correlation context, and equivalent authorization outcomes.
- NFR33: API, CLI, MCP, event, audit, projection, and state-model contracts must support backward-compatible evolution or explicit versioning, deprecation policy, and migration paths for breaking changes.
- NFR34: Integration requests and events must carry correlation context across mailbox intake, file handling, association, approval, command execution, AI mediation, audit, UI/API, CLI, MCP, workers, and webhooks.
- NFR35: Configuration and policy changes must be auditable, versioned, rollback-capable for non-destructive settings, and applied consistently to new work without silently changing completed decision records; destructive or authority-expanding changes must require a new version rather than rollback overwrite.
- NFR36: Time-based behavior for workflow decisions, audit records, retries, approvals, retention, evidence freshness, and SLA calculations must use server-side UTC timestamps, preserve source timestamps and timezone context where relevant, and convert to tenant-local display only at presentation boundaries.

### Operability and Observability

- NFR37: Authorized operators must be able to observe mailbox health, backlog, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, service-client failures, AI mediation failures, command failures, and audit projection lag.
- NFR38: User-visible status must be separated from privileged diagnostic detail and exposed according to authorization level.
- NFR39: The system must provide actionable status for degraded, stale, waiting, blocked, escalation-needed, failed, retryable, and terminal workflow states.
- NFR40: Degraded, blocked, failed, and waiting states must be communicated in user-appropriate language with next-action guidance drawn from the versioned message catalog defined in FR77. Observable: every degraded/blocked/failed/waiting state surfaced to a user must resolve to a message-catalog entry; production telemetry counts uncategorized states (raw error text leaking to a user) and the count must be `0` per release; any nonzero count blocks release.
- NFR41: Degraded dependencies must be isolated to the narrowest identified scope among tenant, mailbox, project, operation, service client, workflow item, or command surface; incident status must state the affected scope and dependency within 5 minutes of detection when monitoring is available.
- NFR42: During degraded operation, every authorized user-facing surface displays, at minimum: the current state enum (per FR67), the affected scope (tenant / mailbox / project / operation, per NFR41), the responsible owner role for resolution, and the next safe action affordance (per FR76). The display refreshes within the bounded staleness in NFR6. Observable: synthetic checks assert that degraded-state surfaces render all four elements; missing any element fails the synthetic.
- NFR42a — **SLOs published.** SLOs for ingestion latency, candidate generation latency, ambiguous-resolution time, command latency (per command class), audit projection lag, retry exhaustion rate, duplicate suppression rate, mailbox failure rate, approval queue p95 age, and AI mediation latency must be published in the per-tenant operational view (M2) and in `addendum.md` §Operating Baselines (created during M2). Each SLO has: target, measurement window, error budget, and the alert threshold that consumes the budget. Initial values per NFR24–NFR27 and NFR43; pilot calibration updates them per A11.
- NFR43: Alerting and synthetic health checks must be non-invasive, tenant-safe, and tied to documented thresholds for mailbox subscriptions, Graph permissions, ingestion backlog, approval aging, retry exhaustion, duplicate spikes, authorization failure spikes, audit projection, command execution, and AI mediation; default MVP thresholds must include subscription expiry within 7 days, retry exhaustion, audit projection lag above 5 minutes, approval items older than 2 business days, and authorization failure spikes above the tenant baseline.
- NFR44: Runbook-ready diagnostics for any single workflow item must include, at minimum: correlation ID, tenant ID, mailbox ID, workflow item ID, current state, last transition (timestamp + actor + from-state), retry count, failure reason code (from the FR77 message catalog), and the next safe action affordance. Observable: a randomly sampled `100` workflow items per week must each render a complete diagnostic; any item missing a required field is a defect. "Runbook-ready" means an on-call engineer with no prior context can reach the correct next step from the diagnostic alone.
- NFR45: Support diagnostics must be shareable through redacted support bundles that preserve correlation, state, and reason context without exposing restricted tenant, project, participant, file, message, or audit evidence.
- NFR46: The system must prevent approval fatigue with concrete, measurable mechanisms:
  - **Prioritization:** the approval queue orders items by `(risk-class × authority-of-affected-party × time-in-queue)`, configurable through `tenant-policy.approval.priority-weights` (see `addendum.md` §Tenant Policy Schema).
  - **Grouping:** items are grouped for review by `(requester × command × project)` so a reviewer can approve or reject a batch with one action when the items share the same input shape; batch approval emits one audit event per item, not per batch.
  - **Suppression / rate ceiling:** a per-user notification rate ceiling of `≤ 8` push notifications per hour and `≤ 30` per day (starter values per A11), with the remainder rolled up into a single digest. Duplicate proposals within the idempotency replay window (per `addendum.md` §Idempotency Keys) suppress automatically.
  - **Backlog SLO:** if any individual reviewer has `> 25` open approval items, the system surfaces a load alert to the tenant admin per NFR43.
  - **Observable:** the production observable is the median and p95 time-in-queue per risk class, exposed through the M2 dashboards. Approval fatigue is considered present (and triggers the FR41 [NOTE FOR PM] revisit condition) when more than `15%` of approvals in a rolling 7-day window are rubber-stamp approvals — defined as approved within `< 5 seconds` of first surfaced, against `risk-class = approval-required`.
- NFR47: Risky automation must distinguish reversible, supersedable, compensating, and irreversible actions before approval.
- NFR48: Every surfaced evidence reference (association evidence, mailbox permissions, policy snapshots, AI context packages, audit projections) carries a visible freshness indicator: the snapshot timestamp and a state enum `fresh` / `stale` / `expired` derived from the bounded staleness window defined in NFR6 for that evidence class. Reviewers cannot approve an action against `expired` evidence; the approval surface disables `approve` with reason `evidence-expired`. `stale` is permitted but visually flagged. Observable: the AI action approval surface renders a per-evidence freshness chip; chip count must equal evidence-reference count on every approval render.

### Auditability, Compliance, and Data Governance

- NFR49: Audit records must be tamper-evident, retention-governed, redaction-aware, reconstructable, and protected by restricted modification/deletion controls limited to authorized retention workflows.
- NFR49a — **Tamper-evident mechanism (M2).** "Tamper-evident" is implemented as an append-only WORM store with hash-chained envelopes: each audit envelope carries a hash of its predecessor in the same tenant's chain; deletion is impossible at the storage layer; redaction is implemented as a redaction record appended to the chain (the original record is preserved encrypted with a redaction key held in a separate KMS). Chain verification runs nightly per tenant; broken chains alert the on-call security engineer within 5 minutes. Retention-governed deletion (per GDPR right-to-erasure) operates by tombstoning at the projection layer and key-shredding the redaction key, never by mutating the audit chain.
- NFR50: Audit records must include tenant, actor, actor type, command, resource, decision, reason, correlation, timestamp, policy snapshot reference, source evidence references, state-transition history, redaction decisions, idempotency key where applicable, and resulting command, projection, or outbound outcome; automated audit tests must verify required field presence for 100% of security-sensitive association, approval, command, retry, duplicate, and AI-action events in the validation dataset.
- NFR50a — **Audit completeness as production observable (M2).** "Audit completeness" in production is the fraction of state-mutating operations (per the NFR15a path inventory) whose audit chain reconstructs the operation end-to-end — i.e., for which every input, decision, resource reference, policy snapshot, and outcome can be recovered from the chain alone, without supplementary logs. Target: `≥ 99.5%` per rolling 7-day window per tenant; below `99.5%` triggers a P1 incident. Field presence (the NFR50 measurement) is necessary but not sufficient for completeness; reconstructability is the actual test. Replay events are excluded per FR95a.
- NFR51: Audit and diagnostic records must preserve enough context to reconstruct who acted, what was attempted, which policy applied, what evidence was used, what state transitions occurred, what was redacted, and what outcome occurred.
- NFR52: The system must minimize retained email content, attachment content, prompts, outputs, diagnostics, and support bundles to the data required for the authorized workflow, audit, and tenant retention policy.
- NFR53: Tenant data retention, export, and deletion workflows must distinguish data classes including source email, metadata, attachments, derived projections, AI prompts and outputs, approvals, policy snapshots, logs, backups, evaluation datasets, and audit records.
- NFR54: Audit evidence must respect retention boundaries and redaction rules so evidence preservation does not become uncontrolled data storage.
- NFR55: Where tenant policy or regulatory profile requires it, the system must record consent or lawful-basis metadata for external participants, retained content, attachments, and AI processing.

### Recovery and Continuity

- NFR56: Source email records, attachment records, approval history, command history, policy snapshots, and audit records must meet the default MVP recovery target of RPO <= 15 minutes and RTO <= 4 hours unless the tenant or deployment profile defines stricter targets.
- NFR57: Derived projections must be rebuildable from immutable source records and audit history within the default MVP recovery target of 4 hours for the baseline validation dataset without requiring mailbox re-ingestion.
- NFR58: Dependency outages must degrade only the affected tenant, mailbox, operation, service client, command surface, or workflow item when dependency ownership and routing can identify that scope; outage tests must prove no unrelated tenant or mailbox is blocked for Graph, identity, AI provider, command execution, audit store, and attachment-processing failures.
- NFR59: Resilience validation must prove degraded Graph access, expired subscriptions, AI provider outage, command execution failure, audit store unavailability, and partial attachment failure do not cause cross-tenant leakage, unauthorized state mutation, or silent data loss.

### Accessibility and Usability Quality

- NFR60: WCAG 2.2 AA conformance is scoped per increment to the UI surfaces that exist in that increment, so the accessibility bar scales with the surfaces a single frontend engineer can credibly cover. CLI and MCP are outside WCAG scope (no UI). In-scope surfaces by increment:
  - **M0 surfaces (must conform before M0 release):** ambiguous association review screen, AI action approval screen, project conversation view.
  - **M1 surfaces (must conform before M1 release):** rejection/defer flows once the full lifecycle lands, approval-policy configuration UI for tenant admins, the M1 portion of the admin operational view (FR75a–FR75g).
  - **M2 surfaces (must conform before M2 release):** the M2 operational dashboards (mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, audit lag).
  - Validation per increment must include automated checks plus keyboard-only and screen-reader review of each in-scope surface before that increment's release. Surfaces marked "post-MVP" or "vision" are not in NFR60 scope. If accessibility consultancy is engaged later, the bar can be widened — but the per-increment scoping enumerated here is the floor.
- NFR61: Accessibility validation must include keyboard-only review flows, screen-reader labels, focus order, non-color status indicators, and error recovery for ambiguous association and approval workflows.
- NFR62: Status, failure, refusal, and authorization messages must be understandable without exposing restricted evidence or relying only on color.
- NFR63: Users resolving ambiguous associations or approvals must be able to identify the next available action without reading raw audit logs.
- NFR64: The UI must distinguish source evidence from AI-generated summaries so users can make review decisions from authoritative context.

### Validation and Quality Gates

- NFR65: Production releases must meet documented quality gates covering tenant isolation, authorization, redaction, idempotency, state transitions, approval gates, duplicate suppression, and audit creation.
- NFR66: Performance validation must prove mailbox backlog processing, queue usability, retry behavior, audit projection lag, and throttled Microsoft Graph behavior against documented tenant or deployment baselines.
- NFR67: Security validation must include negative authorization tests for UI/API, CLI, MCP, background workers, mailbox events, service clients, and AI actors.
- NFR68: Evaluation datasets and test fixtures must use consented, redacted, or synthetic examples with versioning, reproducibility, redaction verification, expected outcomes, and regression result history for association, authorization, duplicate handling, retry, approval, refusal, and audit behavior.
- NFR69: Replay and simulation must be isolated from production mutation, external email sends, live AI tool execution, and live command execution; replay artifacts must be explicitly labeled and tenant-scoped.
- NFR70: Every externally visible operation must define expected state transition, audit event, user-visible response, redaction behavior, and retry/idempotency result.

### Additional Requirements

The PRD addendum is approved and binding implementation context. Additional requirements and constraints found there cover:

- Confidence Thresholds (T_high / T_low)
- Risk Classifier
- Command Allowlist v0 (M0)
- Command Allowlist v1 (M1)
- Tenant Policy Schema
- Shared Command Pipeline (architectural invariant for FR81a)
- Idempotency Keys (per operation class)
- Replay Isolation
- ID Evolution Contract
- Inbound Message Authenticity
- Authority class mapping (FR48 five-class taxonomy)
- Operating Baselines

Key binding constraints extracted from the addendum:

- Association confidence uses deterministic signals with `T_high = 0.90` and `T_low = 0.60` for M0; threshold changes are security-sensitive, audited, bounded, and blocked for service clients and AI actors.
- The M0 risk classifier is tag-and-heuristic; indeterminate results fail closed to approval-required review.
- M0 AI command allowlist contains exactly one command: `Project.AppendConversationMessage`.
- M1 allowlist requires per-command metadata for effect surface, authority class, default risk classification, override hook, and idempotency contract.
- Tenant Policy Schema is closed at the product level; tenants can only set declared knobs within type/range constraints.
- Every state-mutating operation must enter one shared command pipeline; adapters cannot perform authorization, risk classification, or audit writes independently.
- Idempotency keys are defined per operation class and must be extended before new operation classes ship.
- Replay/simulation runs must execute only against a dedicated test tenant, intercept outbound sends, and remain distinguishable in audit.
- Identity evolution from sibling contexts is handled through `IdentityEvolved` events and audit-time projection migration, not mutation of historical audit records.
- Inbound authenticity records M365/Exchange DMARC, DKIM, SPF, header discrepancy, delegated-send, and external-sender posture metadata.
- M2 operating baselines publish SLO targets, windows, budgets, thresholds, calibration source, and tenant scope.

### PRD Completeness Assessment

The PRD is highly detailed and traceability-ready. It provides an explicit glossary, journey-to-FR/NFR traceability table, acceptance guidance for high-risk FR groups, 111 formal FR entries, 77 formal NFR entries, and an approved addendum that closes important implementation contracts for thresholds, risk classification, command allowlists, policy schema, idempotency, replay isolation, identity evolution, inbound authenticity, and operating baselines.

The main readiness risk is not missing PRD detail; it is implementation planning drift. The PRD carries dense safety, parity, audit, and operations obligations that epics and stories must preserve exactly. The highest-risk obligations for downstream validation are FR81a shared command pipeline, NFR15a fail-closed path inventory, NFR50a audit completeness, FR75a-FR75g tenant-admin scope limits, FR48a-FR48d inbound authenticity, and NFR60 per-increment accessibility scope.

## Step 3: Epic Coverage Validation

### Source File Read

- `_bmad-output/planning-artifacts/epics.md` — read completely.

### Epic FR Coverage Extracted

The epics document has an explicit `FR Coverage Map`. The range entry `FR75a–FR75g` was expanded into individual FR identifiers for validation.

- FR1: Epic 2 — Capture authorized mailbox events as project inputs
- FR2: Epic 2 — Preserve source/thread/mailbox/sender/recipient/timestamp/attachment identity
- FR3: Epic 2 — Deterministic project association
- FR4: Epic 2 — Detect ambiguous association → human review
- FR5: Epic 2 — Review candidates with evidence, confidence, reason codes, consequences
- FR6: Epic 2 — Choose / reject-all / defer / needs-review + optional note
- FR7: Epic 2 — Correct a previously selected association
- FR8: Epic 2 — Record decisions, corrections, rejections, deferrals, retries, skips
- FR9: Epic 2 — Configure association rules + `T_high`/`T_low` (M0); full policy editor in Epic 7
- FR10: Epic 2 — Preserve original email context on reject/defer/fail/skip/review
- FR11: Epic 2 — Machine-readable association reasons + confidence inputs
- FR12: Epic 2 — Side-by-side candidate evidence comparison
- FR13: Epic 2 — Resolve internal/external participants to tenant-scoped parties
- FR14: Epic 2 — Identify unresolved participants for review
- FR15: Epic 2 — External email participation without portal auth
- FR16: Epic 1 — Authorization enforced at command/query boundary (gateway)
- FR17: Epic 2 — Block unresolved/unauthorized actor access
- FR18: Epic 7 — Configure governed mailbox participation rules
- FR19: Epic 5 — Configure service-client access (CLI/MCP/workers/mailbox/AI)
- FR20: Epic 9 — Record consent/lawful-basis metadata where policy requires
- FR21: Epic 3 — View email-derived messages as project conversation context
- FR22: Epic 3 — Represent email/parties/attachments/decisions/approvals/failures/AI outcomes (7 sub-stories)
- FR23: Epic 3 — "Why this project" evidence/provenance panel
- FR24: Epic 3 — Association/attachment/task/approval/command/failure/next-action status
- FR25: Epic 3 — Tenant/project conversation separation
- FR26: Epic 3 — Informational vs actionable classification badge
- FR27: Epic 3 — AI-summary vs source-evidence distinction
- FR28: Epic 3 — Visible human-review history per item
- FR29: Epic 3 — Capture attachments from associated email
- FR30: Epic 3 — Store attachments in governed project folders
- FR31: Epic 3 — Inspect attachment capture/storage status
- FR32: Epic 3 — Prevent unauthorized attachment metadata/content view
- FR33: Epic 3 — Scoped AI-context packaging (authz + policy + audit)
- FR34: Epic 3 — Attachment states (captured/pending/unavailable/rejected/unsafe/failed/retryable)
- FR35: Epic 4 — Detect candidate task/action intent + source evidence
- FR36: Epic 4 — Review captured task intent before action
- FR37: Epic 4 — Convert task intent into governed action request
- FR38: Epic 4 — Mark task intent not-actionable/duplicate/handled/out-of-scope
- FR39: Epic 4 — Classify AI action by risk
- FR40: Epic 4 — Allow low-risk AI assistance per tenant policy
- FR41: Epic 4 — Require approval for the six risky action classes
- FR42: Epic 4 — Approve/reject proposed AI actions after review
- FR43: Epic 4 — Execute approved actions only via allowlisted commands
- FR44: Epic 4 — Inspect AI proposals/approvals/denials/executions/outcomes
- FR45: Epic 4 — Preview outbound/file-access/command/AI changes before execution
- FR46: Epic 4 — Refuse/block unsafe AI/automation/command/mailbox requests
- FR47: Epic 6 — Create outbound project email drafts within authority
- FR48: Epic 6 — Distinguish five sender-authority classes
- FR48a: Epic 6 — Inbound DMARC/DKIM/SPF verdict passthrough
- FR48b: Epic 6 — Inbound header inspection + discrepancy recording
- FR48c: Epic 6 — On-behalf-of disambiguation
- FR48d: Epic 6 — External-sender posture flag + strictness knob
- FR49: Epic 6 — Require approval before outbound leaves the boundary
- FR50: Epic 6 — Preserve outbound approval-record fields
- FR51: Epic 7 — Configure mailbox integration + monitored patterns
- FR52: Epic 7 — Configure AI action policy (low-risk + approval-required)
- FR53: Epic 7 — Review mailbox permission + degraded states
- FR54: Epic 9 — Compliance/support investigate decisions/approvals/outcomes
- FR55: Epic 1 — Produce audit records for security-sensitive events (emission); WORM in Epic 9
- FR55a: Epic 9 — Cross-tenant isolation in derived stores
- FR56: Epic 9 — Query audit records by tenant/actor/command/resource/decision/reason/time
- FR57: Epic 1 — Hide unauthorized info (swappable redaction stage); applied across all surfaces
- FR58: Epic 9 — Retention/export/deletion operational support
- FR59: Epic 1 — Correlation propagation across all surfaces
- FR60: Epic 2 — Preserve source evidence with retention + redaction
- FR61: Epic 1 — Versioned policy snapshots
- FR62: Epic 2 — Human notes / resolution rationale
- FR63: Epic 2 — Supersede reversible human decisions (preserve original)
- FR64: Epic 2 — Detect duplicate mailbox delivery
- FR65: Epic 2 — Retry failed work where valid
- FR66: Epic 2 — Surface terminal/non-terminal failure states
- FR67: Epic 8 — Health/queue/dashboard exposure (M0 minimal in E1/E2)
- FR68: Epic 1 — Fail closed on unresolved context/dependency
- FR69: Epic 7 — View/manage operational queues
- FR70: Epic 7 — Assign/claim review items
- FR71: Epic 2 — Next required human action per item
- FR72: Epic 7 — Notify on review/approval/failure/degraded/quarantine/retry
- FR73: Epic 7 — Configure notification routing + escalation
- FR74: Epic 7 — Disable/quarantine/rate-limit sources (15 subject×action stories, shared control floor inlined per story)
- FR75: Epic 7 — Per-tenant rate limits/quotas/circuit breakers
- FR75a: Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation)
- FR75b: Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation)
- FR75c: Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation)
- FR75d: Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation)
- FR75e: Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation)
- FR75f: Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation)
- FR75g: Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation)
- FR76: Epic 2 — Review-item action affordances + disabled-action reasons
- FR77: Epic 1 — Versioned user-safe message catalog
- FR78: Epic 7 — Filter/sort/prioritize operational queues
- FR79: Epic 2 — Stale/waiting/blocked/escalation states
- FR80: Epic 1 — Long-running operation status (UI/M0); CLI/MCP exposure in Epic 5
- FR81: Epic 1 — UI core governed workflow operations
- FR81a: Epic 1 — Shared command pipeline architectural invariant
- FR82: Epic 5 — CLI workflow parity
- FR83: Epic 5 — MCP workflow parity
- FR84: Epic 5 — Equivalent authorization outcomes/state transitions across surfaces
- FR85: Epic 1 — Command-surface origin attribution (UI/M0); extended in Epic 5
- FR86: Epic 1 — Contract tests verify FR81a invariant (shims/M0); full harness in Epic 5
- FR87: Epic 1 — Canonical lifecycle states (full `Skipped` + matrix extended in Epic 7)
- FR88: Epic 1 — Validate workflow state transitions
- FR89: Epic 1 — Reject invalid transitions + record actor/reason/correlation
- FR90: Epic 1 — Idempotency keys + stable resource IDs (full per-class contract in Epic 9)
- FR91: Epic 2 — Separate source vs derived; rebuild projections
- FR91a: Epic 2 — Correction propagation contract
- FR92: Epic 1 — Evaluation datasets (test infrastructure); extended in Epic 9
- FR93: Epic 1 — Tenant-scoped test fixtures / sandbox data
- FR94: Epic 8 — Measurable operational outcome metrics
- FR95: Epic 9 — Replay/simulate mailbox events without external side effects
- FR95a: Epic 9 — Replay isolation contract (test tenant, audit distinguishability)
- FR96: Epic 2 — Corrections as future association evidence (M1)

Total FRs in epics after range expansion: 111

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --------- | --------------- | ------------- | ------ |
| FR1 | The system can capture authorized mailbox events as project collaboration inputs. | Epic 2 — Capture authorized mailbox events as project inputs | Covered |
| FR2 | The system can preserve source email identity, thread identity, mailbox identity, sender, recipients, timestamps, and attachment references. | Epic 2 — Preserve source/thread/mailbox/sender/recipient/timestamp/attachment identity | Covered |
| FR3 | The system can associate incoming email with an existing project using deterministic evidence. | Epic 2 — Deterministic project association | Covered |
| FR4 | The system can detect ambiguous project association and route it to human review. | Epic 2 — Detect ambiguous association → human review | Covered |
| FR5 | Authorized users can review candidate projects with visible evidence, confidence state, reason codes, and the consequences of each available decision. | Epic 2 — Review candidates with evidence, confidence, reason codes, consequences | Covered |
| FR6 | Authorized users can choose a candidate project, reject all candidates, defer association, mark an item as needing review, and provide an optional decision note. | Epic 2 — Choose / reject-all / defer / needs-review + optional note | Covered |
| FR7 | Authorized users can correct a previously selected project association. | Epic 2 — Correct a previously selected association | Covered |
| FR8 | The system can record association decisions, corrections, rejections, deferrals, retries, and skipped items. | Epic 2 — Record decisions, corrections, rejections, deferrals, retries, skips | Covered |
| FR9 | Tenant administrators can configure project association rules, evidence requirements, and the confidence thresholds `T_high` and `T_low`. The score domain, signals fed, safe defaults, calibration protocol, and guardrails on threshold changes are defined in `addendum.md` §Confidence Thresholds. Both knobs are security-sensitive (per the Tenant Policy Schema): changes require tenant-admin authorization, produce an audit event, are bounded by the schema's allowed range, and cannot be made by service clients or AI actors. | Epic 2 — Configure association rules + `T_high`/`T_low` (M0); full policy editor in Epic 7 | Covered |
| FR10 | The system can preserve original email context when association is rejected, deferred, failed, skipped, or awaiting review. | Epic 2 — Preserve original email context on reject/defer/fail/skip/review | Covered |
| FR11 | The system can expose deterministic association reasons and confidence inputs in machine-readable form for UI, CLI, MCP, audit, and test verification. | Epic 2 — Machine-readable association reasons + confidence inputs | Covered |
| FR12 | Authorized users can compare candidate project evidence side by side when resolving ambiguous association. | Epic 2 — Side-by-side candidate evidence comparison | Covered |
| FR13 | The system can resolve internal and external email participants to tenant-scoped parties. | Epic 2 — Resolve internal/external participants to tenant-scoped parties | Covered |
| FR14 | Authorized users can identify unresolved participants for review. | Epic 2 — Identify unresolved participants for review | Covered |
| FR15 | External participants can contribute project context through email without requiring MVP external portal access. | Epic 2 — External email participation without portal auth | Covered |
| FR16 | The system can enforce tenant and project authorization before exposing project candidates, files, conversations, approvals, commands, or audit details. | Epic 1 — Authorization enforced at command/query boundary (gateway) | Covered |
| FR17 | The system can block unresolved or unauthorized actors from accessing project files, creating task requests, triggering commands, or sending outbound communication. | Epic 2 — Block unresolved/unauthorized actor access | Covered |
| FR18 | Tenant administrators can configure governed mailbox participation rules. | Epic 7 — Configure governed mailbox participation rules | Covered |
| FR19 | Authorized administrators can configure service-client access for CLI, MCP, background workers, mailbox events, and AI actors. | Epic 5 — Configure service-client access (CLI/MCP/workers/mailbox/AI) | Covered |
| FR20 | The system can record consent or lawful-basis metadata where tenant policy requires it for external participants, retained email content, attachments, and AI processing. | Epic 9 — Record consent/lawful-basis metadata where policy requires | Covered |
| FR21 | Authorized users can view email-derived messages as project conversation context. | Epic 3 — View email-derived messages as project conversation context | Covered |
| FR22 | The system can represent associated email, participants, attachments, decisions, approvals, failures, and AI outcomes in the project context. | Epic 3 — Represent email/parties/attachments/decisions/approvals/failures/AI outcomes (7 sub-stories) | Covered |
| FR23 | Authorized users can inspect why an email belongs to a project, including source evidence, confidence signals, human decisions, and later corrections. | Epic 3 — "Why this project" evidence/provenance panel | Covered |
| FR24 | Authorized users can see association, attachment, task, approval, command, failure, retry, and next-action status for a project conversation. | Epic 3 — Association/attachment/task/approval/command/failure/next-action status | Covered |
| FR25 | The system can keep project conversation context separate across tenants and projects. | Epic 3 — Tenant/project conversation separation | Covered |
| FR26 | The system can distinguish informational project context from actionable requests. | Epic 3 — Informational vs actionable classification badge | Covered |
| FR27 | The system can distinguish system-generated summaries from source evidence so users do not confuse AI interpretation with original email, attachment, or command facts. | Epic 3 — AI-summary vs source-evidence distinction | Covered |
| FR28 | The system can preserve visible human-review history for each email, attachment, approval, AI action, and command. | Epic 3 — Visible human-review history per item | Covered |
| FR29 | The system can capture attachments from associated project email. | Epic 3 — Capture attachments from associated email | Covered |
| FR30 | The system can store captured attachments in governed project folders. | Epic 3 — Store attachments in governed project folders | Covered |
| FR31 | Authorized users can inspect attachment capture and storage status. | Epic 3 — Inspect attachment capture/storage status | Covered |
| FR32 | The system can prevent unauthorized actors from viewing attachment metadata or content. | Epic 3 — Prevent unauthorized attachment metadata/content view | Covered |
| FR33 | The system can make authorized project files available as scoped AI context only through explicit authorization, policy checks, and auditable context packaging. | Epic 3 — Scoped AI-context packaging (authz + policy + audit) | Covered |
| FR34 | The system can represent attachment states including captured, pending, unavailable, rejected, unsafe, failed, and retryable. | Epic 3 — Attachment states (captured/pending/unavailable/rejected/unsafe/failed/retryable) | Covered |
| FR35 | The system can detect candidate task or action intent from authorized project conversation actors and preserve the source message evidence. | Epic 4 — Detect candidate task/action intent + source evidence | Covered |
| FR36 | Authorized users can review captured task intent before governed action. The review surface displays the data contract from FR35 plus the source message in full and the available state transitions per FR37/FR38. | Epic 4 — Review captured task intent before action | Covered |
| FR37 | Authorized users can convert captured task intent into a governed task or action request. Conversion creates the proposal record per FR41 / `addendum.md` §Risk Classifier and links it to the source task-intent record. Conversion is itself an audited operation. | Epic 4 — Convert task intent into governed action request | Covered |
| FR38 | Authorized users can mark captured task intent as not actionable, duplicate, already handled, or out of scope. Each of these is a terminal state for the task-intent record (the record is preserved for evaluation per A9a); duplicate additionally links the predecessor task-intent ID. | Epic 4 — Mark task intent not-actionable/duplicate/handled/out-of-scope | Covered |
| FR39 | The system can classify AI action requests by risk. | Epic 4 — Classify AI action by risk | Covered |
| FR40 | The system can allow low-risk AI assistance when tenant policy and project authorization permit it. | Epic 4 — Allow low-risk AI assistance per tenant policy | Covered |
| FR41 | The system can require approval before AI actions that modify project state, expose files, send external communication, create or assign tasks, invoke tools, or act on behalf of a participant. | Epic 4 — Require approval for the six risky action classes | Covered |
| FR42 | Authorized users can approve or reject proposed AI actions after reviewing the action summary, affected project resources, external recipients, sender authority, risk classification, and expected outcome. | Epic 4 — Approve/reject proposed AI actions after review | Covered |
| FR43 | The system can execute approved AI actions only through allowlisted governed commands. | Epic 4 — Execute approved actions only via allowlisted commands | Covered |
| FR44 | Authorized users can inspect AI action proposals, approvals, denials, executions, failures, and outcomes. | Epic 4 — Inspect AI proposals/approvals/denials/executions/outcomes | Covered |
| FR45 | Authorized users can preview outbound communication, file access, command execution, and AI-generated changes before approval or execution. | Epic 4 — Preview outbound/file-access/command/AI changes before execution | Covered |
| FR46 | The system can refuse or block unsafe AI, automation, command, or mailbox requests that exceed tenant policy, project authorization, sender authority, or approved command scope. | Epic 4 — Refuse/block unsafe AI/automation/command/mailbox requests | Covered |
| FR47 | Authorized users can create outbound project email drafts within approved project and sender authority. | Epic 6 — Create outbound project email drafts within authority | Covered |
| FR48 | The system can distinguish draft-only, authenticated-user send, shared-mailbox send, send-on-behalf, and approved service-send authority. The mapping rule from M365 / Exchange permission models to ChatBot sender-authority classes is defined in `addendum.md` §Inbound Message Authenticity; the conflict case (M365 grants send-on-behalf but ChatBot grants no such authority) resolves to fail-closed (the action cannot be taken from ChatBot, even if the underlying mailbox would accept it). | Epic 6 — Distinguish five sender-authority classes | Covered |
| FR48a | **Inbound provider authenticity passthrough (M1).** Every inbound message intake event records the M365 / Exchange DMARC, DKIM, and SPF verdicts as-supplied by the provider. ChatBot does not re-verify; the provider is the source of truth. | Epic 6 — Inbound DMARC/DKIM/SPF verdict passthrough | Covered |
| FR48b | **Inbound header inspection (M1).** The mailbox adapter parses `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender` headers and records disagreements between `From` / `Sender` / `Reply-To` as intake metadata. Disagreements do not block ingestion but feed the risk classifier and surface to the reviewer. | Epic 6 — Inbound header inspection + discrepancy recording | Covered |
| FR48c | **On-behalf-of disambiguation (M1).** When a delegated-send relationship is expressed by the provider, the recorded sender authority is the delegate's identity, with the principal's identity preserved as `principal_for`. Outbound actions follow the same rule symmetrically. | Epic 6 — On-behalf-of disambiguation | Covered |
| FR48d | **External-sender posture (M1).** Messages from senders with no resolved tenant party are flagged `external_sender = true`. The tenant policy `mailbox.authenticity-strictness` knob (`permissive` / `strict` / `paranoid`) controls whether external-sender messages auto-associate, route to NeedsReview, or fail closed. | Epic 6 — External-sender posture flag + strictness knob | Covered |
| FR49 | The system can require approval before outbound project communication leaves the project boundary. | Epic 6 — Require approval before outbound leaves the boundary | Covered |
| FR50 | The system can preserve proposed content, approved content, recipients, sender authority, project context, requester, approver, and decision outcome in approval records. | Epic 6 — Preserve outbound approval-record fields | Covered |
| FR51 | Tenant administrators can configure mailbox integration settings and monitored mailbox patterns. | Epic 7 — Configure mailbox integration + monitored patterns | Covered |
| FR52 | Tenant administrators can configure AI action policy for low-risk and approval-required actions. | Epic 7 — Configure AI action policy (low-risk + approval-required) | Covered |
| FR53 | Tenant administrators can review mailbox permission status and degraded mailbox processing states. | Epic 7 — Review mailbox permission + degraded states | Covered |
| FR54 | Compliance or support reviewers can investigate association decisions, approval decisions, command outcomes, and risky AI actions. | Epic 9 — Compliance/support investigate decisions/approvals/outcomes | Covered |
| FR55 | The system can produce audit records for security-sensitive association, participant, file, approval, command, AI, retry, and duplicate-handling events. | Epic 1 — Produce audit records for security-sensitive events (emission); WORM in Epic 9 | Covered |
| FR55a | **Cross-tenant isolation in derived stores (M2).** Vector indexes, embedding stores, prompt-context caches, candidate-ranking caches, and any other derived store that holds material derived from tenant data must enforce tenant isolation by construction (per-tenant store partitioning or row-level tenant scoping verified at every read). Cross-tenant queries are not possible at the store-access layer, not merely filtered at the application layer. Verification: a periodic isolation probe (per NFR59) attempts cross-tenant reads through the store-access layer and asserts they fail at the layer below the application. | Epic 9 — Cross-tenant isolation in derived stores | Covered |
| FR56 | Authorized users can query audit records by tenant, actor, command, resource, decision, reason, correlation, and time context. | Epic 9 — Query audit records by tenant/actor/command/resource/decision/reason/time | Covered |
| FR57 | The system can hide unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details. | Epic 1 — Hide unauthorized info (swappable redaction stage); applied across all surfaces | Covered |
| FR58 | Authorized administrators or reviewers can access operational support for tenant data retention, export, and deletion workflows. | Epic 9 — Retention/export/deletion operational support | Covered |
| FR59 | The system can propagate correlation context across mailbox intake, project association, file handling, approval, AI mediation, command execution, audit, UI, CLI, and MCP. | Epic 1 — Correlation propagation across all surfaces | Covered |
| FR60 | The system can preserve source evidence used for association, authorization, approval, rejection, refusal, correction, retry, and audit investigation with retention boundaries and redaction behavior. | Epic 2 — Preserve source evidence with retention + redaction | Covered |
| FR61 | The system can maintain versioned policy snapshots used for association, authorization, approval, AI action classification, and command execution decisions. | Epic 1 — Versioned policy snapshots | Covered |
| FR62 | Authorized users can add human notes or resolution rationale to association, participant, approval, retry, quarantine, and correction decisions. | Epic 2 — Human notes / resolution rationale | Covered |
| FR63 | Authorized users can supersede reversible human decisions where policy permits while preserving the original decision in audit history. | Epic 2 — Supersede reversible human decisions (preserve original) | Covered |
| FR64 | The system can detect duplicate mailbox delivery and avoid duplicate project artifacts. | Epic 2 — Detect duplicate mailbox delivery | Covered |
| FR65 | The system can retry failed mailbox, attachment, association, approval, command, and projection work where retry is valid. | Epic 2 — Retry failed work where valid | Covered |
| FR66 | The system can surface terminal and non-terminal failure states to authorized users. | Epic 2 — Surface terminal/non-terminal failure states | Covered |
| FR67 | The system can expose mailbox health, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, and audit projection status. | Epic 8 — Health/queue/dashboard exposure (M0 minimal in E1/E2) | Covered |
| FR68 | The system can fail closed when project association, participant identity, tenant scope, authorization, audit writing, or required dependencies cannot be resolved. | Epic 1 — Fail closed on unresolved context/dependency | Covered |
| FR69 | Authorized users can view and manage queues for ambiguous associations, unresolved participants, pending approvals, failed ingestion, failed attachment handling, and retryable operations. | Epic 7 — View/manage operational queues | Covered |
| FR70 | Authorized users can assign or claim review items that require human resolution. | Epic 7 — Assign/claim review items | Covered |
| FR71 | Authorized users can see the next required human action for an email, task intent, attachment, approval, or failed operation. | Epic 2 — Next required human action per item | Covered |
| FR72 | The system can notify authorized users when review, approval, failure, degraded mailbox, quarantine, or retry states require attention. | Epic 7 — Notify on review/approval/failure/degraded/quarantine/retry | Covered |
| FR73 | Tenant administrators can configure notification routing and escalation rules for unresolved review, approval, degraded, quarantine, and failure states. | Epic 7 — Configure notification routing + escalation | Covered |
| FR74 | Authorized administrators can disable, quarantine, or rate-limit mailbox sources, service clients, AI actors, or command capabilities producing unsafe, invalid, excessive, or policy-violating activity. | Epic 7 — Disable/quarantine/rate-limit sources (15 subject×action stories, shared control floor inlined per story) | Covered |
| FR75 | Authorized administrators can configure per-tenant rate limits, quotas, and circuit breakers for mailbox processing, AI mediation, command execution, and outbound communication. | Epic 7 — Per-tenant rate limits/quotas/circuit breakers | Covered |
| FR75a | A `tenant-admin` role holds the union of every admin scope in FR75b–FR75g; finer-grained admin roles (`mailbox-admin`, `policy-admin`, `compliance-admin`, `operations-admin`) hold proper subsets. Admin assignment itself is a security-sensitive operation that produces an audit event and cannot be performed by service clients or AI actors. | Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation) | Covered |
| FR75b | **See-only scopes:** admins can read operational queue summaries (depth, age, owner), health/status enums, and aggregate metrics (per FR67) across all tenant projects without holding per-project membership. Reading per-item detail (project name, evidence content, file metadata, audit reasons) requires per-project authority; admin role does not grant it. | Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation) | Covered |
| FR75c | **Operate scopes:** admins can perform queue-level operations (retry, requeue, quarantine, dismiss) on items they can see-only. The operation is recorded with the admin's identity, the affected items, the queue, and the reason. Admins cannot mutate project-level records (associations, files, approvals) through queue-level operations. | Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation) | Covered |
| FR75d | **Policy scope (`policy-admin`):** can mutate the Tenant Policy Schema knobs (per `addendum.md` §Tenant Policy Schema). Security-sensitive knobs additionally require a second admin approval (two-person rule) and a documented justification recorded in audit. | Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation) | Covered |
| FR75e | **Mailbox scope (`mailbox-admin`):** can configure mailbox patterns, routing rules, and provider-credential connections. Cannot read mailbox content; cannot decide associations. | Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation) | Covered |
| FR75f | **Compliance scope (`compliance-admin`):** can read audit records across the tenant (subject to per-project redaction per NFR2), trigger investigations, configure retention windows within NFR49a bounds. Cannot operate on workflow items. | Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation) | Covered |
| FR75g | **Audit obligation on every admin action:** every admin operation, including read-only access to operational dashboards above an aggregation threshold, produces an audit event with admin identity, scope used, items affected, and timestamp. No admin operation has a "skip audit" path. The `tenant-admin` role does not bypass NFR15a or NFR50a. | Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation) | Covered |
| FR76 | The system can present review items with clear available actions, disabled-action reasons, and next-step guidance based on the item state and user authorization. | Epic 2 — Review-item action affordances + disabled-action reasons | Covered |
| FR77 | The system can explain refusal, blocked action, degraded mailbox, failed attachment, failed command, and authorization-denied states in user-safe language without exposing restricted evidence. | Epic 1 — Versioned user-safe message catalog | Covered |
| FR78 | Authorized users can filter, sort, and prioritize operational queues by age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action. | Epic 7 — Filter/sort/prioritize operational queues | Covered |
| FR79 | The system can show stale, waiting, blocked, and escalation-needed states for review queues and long-running operations. | Epic 2 — Stale/waiting/blocked/escalation states | Covered |
| FR80 | UI, CLI, and MCP users can retrieve long-running operation status including operation identity, current state, retry count, partial outputs, safe next actions, terminal reason, and correlation context. | Epic 1 — Long-running operation status (UI/M0); CLI/MCP exposure in Epic 5 | Covered |
| FR81 | Authorized UI users can perform the core governed email-to-project workflow operations. | Epic 1 — UI core governed workflow operations | Covered |
| FR81a | **Shared command pipeline (architectural invariant).** Every state-mutating operation, regardless of originating surface (UI, CLI, MCP, service client, AI actor, background worker), passes through one command spine. The ChatBot admission layer applies authentication, tenant-scope binding, authorization, risk classification, approval gate, coarse idempotency, and a pre-commit audit gate before dispatching to EventStore for fine idempotency, command execution, event publication, projection update, and post-commit audit emission. Surface adapters translate surface-specific input into a typed Command record and hand it to the pipeline; adapters cannot replicate any pipeline stage. Parity follows by construction. The architectural detail (adapter rules, invariant violations, what does and does not count as a parity violation) is in `addendum.md` §Shared Command Pipeline. Architecture review must reject adapter designs that bypass pipeline stages, regardless of stated rationale. | Epic 1 — Shared command pipeline architectural invariant | Covered |
| FR82 | Authorized CLI users can inspect, associate, reject, defer, correct, retry, approve, execute, check status, and query audit for the same governed workflow. (CLI adapter is the surface-specific translation layer over the FR81a pipeline.) | Epic 5 — CLI workflow parity | Covered |
| FR83 | Authorized MCP clients can access the same governed workflow operations for AI-agent and automation use. (MCP adapter is the surface-specific translation layer over the FR81a pipeline.) | Epic 5 — MCP workflow parity | Covered |
| FR84 | The system can return equivalent authorization outcomes and state transitions across UI, CLI, and MCP. **This is a verification of FR81a, not the enforcement mechanism**: if the pipeline invariant holds, equivalent outcomes follow by construction; if equivalent outcomes diverge across surfaces, the divergence is a defect against FR81a. | Epic 5 — Equivalent authorization outcomes/state transitions across surfaces | Covered |
| FR85 | The system can identify whether an action originated from UI/API, CLI, MCP, background worker, mailbox event, or AI actor. Origin is attached at the adapter boundary and travels with the Command record into the audit envelope; downstream pipeline stages cannot mutate origin. | Epic 1 — Command-surface origin attribution (UI/M0); extended in Epic 5 | Covered |
| FR86 | Contract tests must verify the FR81a invariant for each surface: given an equivalent input, each surface adapter must produce the same Command record (after canonical normalization). Test failure is an invariant violation, not a tolerance threshold. Contract-verifiable responses with stable error codes follow as a downstream consequence of FR81a; enforcement of parity is structural, not test-derived. | Epic 1 — Contract tests verify FR81a invariant (shims/M0); full harness in Epic 5 | Covered |
| FR87 | The system can define canonical lifecycle states for email intake items, participant resolution, attachment handling, approvals, AI actions, command executions, and audit projection. | Epic 1 — Canonical lifecycle states (full `Skipped` + matrix extended in Epic 7) | Covered |
| FR88 | The system can validate inbound and outbound workflow state transitions against an explicit state model. | Epic 1 — Validate workflow state transitions | Covered |
| FR89 | The system can reject invalid state transitions and record the rejected transition, actor, reason, and correlation context. | Epic 1 — Reject invalid transitions + record actor/reason/correlation | Covered |
| FR90 | The system can expose idempotency keys and stable resource identifiers for mailbox events, email messages, attachments, approvals, commands, retries, outbound communication, and audit records. | Epic 1 — Idempotency keys + stable resource IDs (full per-class contract in Epic 9) | Covered |
| FR91 | The system can separate immutable source records from derived project projections and rebuild derived projections from source records when needed. | Epic 2 — Separate source vs derived; rebuild projections | Covered |
| FR91a | **Correction propagation contract (M0/M1).** When a user corrects an association (per FR7), every derived store that referenced the original association must be invalidated and rebuilt: candidate ranking, evidence snapshot, AI action proposals that consumed the misassociated context, vector index entries derived from the misassociated material (M2), and operational queue projections. The user-facing state during reindex is `correcting` (visible on the corrected item with progress indicator and estimated completion). The corrected item remains in `correcting` state until all derived stores acknowledge invalidation; AI actions cannot use the corrected project context until invalidation completes. Audit records the predecessor association, the correction, and the per-store invalidation outcome. | Epic 2 — Correction propagation contract | Covered |
| FR92 | Authorized product or QA users can maintain internal evaluation datasets derived from consented, redacted, or synthetic project examples with expected outcomes, redaction expectations, and regression result history. | Epic 1 — Evaluation datasets (test infrastructure); extended in Epic 9 | Covered |
| FR93 | The system can provide tenant-scoped test fixtures or sandbox data for validating mailbox intake, association, authorization, attachment handling, approval, AI mediation, command execution, and audit behavior. | Epic 1 — Tenant-scoped test fixtures / sandbox data | Covered |
| FR94 | The system can expose measurable operational outcomes for ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag. Exposure surface and SLO targets are defined in NFR42a (OpenTelemetry metrics published to the tenant operational dashboard in M2; intermediate exposure via the FR67 operational queues in M0/M1). | Epic 8 — Measurable operational outcome metrics | Covered |
| FR95 | The system can simulate or replay representative mailbox events for authorized QA or support investigation without sending external communication or mutating production project state. | Epic 9 — Replay/simulate mailbox events without external side effects | Covered |
| FR95a | **Replay isolation contract (M2).** Replay/simulation runs are architecturally separated from production: they execute against a dedicated test tenant, the outbound adapter for the test tenant intercepts every external action and records it instead of sending, and replay events carry a `replay_run_id` that is included in the audit envelope. Production audit queries default to excluding replay events; audit completeness measurement (NFR50a) excludes replay events from both numerator and denominator. A nightly automated probe asserts no replay run has ever produced a record in any production tenant's outbound-trace store; failure of the probe gates M2 release. Detailed mechanism in `addendum.md` §Replay Isolation. | Epic 9 — Replay isolation contract (test tenant, audit distinguishability) | Covered |
| FR96 | The system can make recorded correction decisions available as future association evidence only when tenant policy permits, the evidence remains explainable, and users can inspect why it influenced a match. | Epic 2 — Corrections as future association evidence (M1) | Covered |

### Missing Requirements

- None. Every PRD FR identifier is claimed in the epics coverage map.

### FRs Claimed In Epics But Not Present In PRD

- None. No extra FR identifiers were claimed by epics outside the PRD FR set.

### Coverage Statistics

- Total PRD FRs: 111
- FRs covered in epics: 111
- Missing PRD FRs: 0
- Extra epic FR claims not in PRD: 0
- Coverage percentage: 100.00%

### Coverage Assessment

The epics document claims complete FR coverage. Coverage is not merely implied by story acceptance text; it is explicitly mapped in the `FR Coverage Map`, and the detailed story sections provide supporting implementation paths for the mapped FR groups. The only normalization required for validation was expanding the grouped `FR75a–FR75g` tenant-admin entry into seven individual PRD FR identifiers.

## Step 4: UX Alignment Assessment

### UX Document Status

Found. Selected UX inputs were read completely:

- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/m1-m2-surface-elaboration.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md`

Architecture was also read completely for UX-support validation:

- `_bmad-output/planning-artifacts/architecture.md`

### UX ↔ PRD Alignment

Aligned.

- PRD user journeys map cleanly to the UX `Key Flows` 1-9.
- PRD surface inventory S1-S10 maps to UX information architecture and surface/state tables.
- PRD NFR60 WCAG 2.2 AA per-increment scope is reflected in UX accessibility floor and story guidance.
- PRD Epic 10 change for the governed interactive chat surface is reflected in `epic10-chat-surface-elaboration.md` with Project Workspace, governed composer, streaming, Stop/Cancel, accessibility, and localization details.
- The UX package intentionally remains spine-only with no mockups/wireframes; that is explicitly documented and is not itself a missing-doc issue.

### UX ↔ Architecture Alignment

Aligned with one open dependency.

- Architecture supports Blazor + Fluent UI v5 through Hexalith.FrontComposer and keeps UI/CLI/MCP adapters on the Client boundary.
- Architecture supports M0 surfaces S1/S2/S3, M1/M2 surfaces S4/S6/S7/S8/S9/S10, and Epic 10 FrontComposer Shell adoption.
- Architecture preserves the UX safety rule that the chat composer is a governed write surface on the CommandGateway spine, not a bypass or ungoverned textbox.
- Architecture supports accessibility and localization at the platform level and preserves SignalR projection-nudge semantics for read updates.

### Alignment Issues

- **Open dependency: AI-response streaming transport.** UX requires progressive response rendering with an always keyboard-reachable Stop/Cancel control. Architecture explicitly marks the streaming transport decision open: extend SignalR projection-nudge or introduce a dedicated streaming channel. Story 10.6 is correctly blocked until an ADR records the transport while preserving the `never trust payload` and fail-closed posture.

### Warnings

- **Spine-only UX increases story acceptance burden.** The absence of mockups is explicitly approved, but implementation stories must import the UX IA, component, state, interaction, accessibility, responsive, localization, and redaction tables as binding acceptance context. Epics already state this, so the warning is about execution discipline rather than a document gap.
- **M1/M2 surface assignment gate must stay active.** Stories implementing S4, S6, S7, S8, S9, or S10 must include the specific surface row from `m1-m2-surface-elaboration.md` before sprint assignment.

### UX Alignment Assessment

UX readiness is acceptable for implementation planning. The UX package has enough structured behavior, state, accessibility, responsive, and localization detail to support story assignment, provided story authors keep the surface-specific gate artifacts binding. The one concrete architecture gap is already identified and blocked at Story 10.6: streaming transport must be resolved before implementation.

## Step 5: Epic Quality Review

### Review Scope

- `_bmad-output/planning-artifacts/epics.md` read completely.
- 10 epics reviewed.
- 121 story headings found.
- 119 assignable stories found after excluding 2 explicitly marked parent planning containers: Story 1.1 and Story 7.27.

### Epic Structure Validation

| Epic | User Value Focus | Independence / Dependency Check | Story Quality Summary | Result |
| --- | --- | --- | --- | --- |
| Epic 1: First Safe Governed Action & Command Spine | Partially user-value anchored through Story 1.9, but heavily technical/foundation. | Establishes the safety floor needed by later epics; no future epic needed for the first governed command, but Story 1.14 later shell deferral is closed only by Epic 10. | ACs are specific and testable; several stories are technical enablers rather than direct user stories. | Pass with major caution. |
| Epic 2: Email Intake & Project Association | Strong user value: receive, associate, review, correct project email. | Uses Epic 1 output. Story 2.8 explicitly defers production saga/runtime readiness to Story 8.6. | Strong BDD coverage and failure-state coverage. | Pass with dependency caution. |
| Epic 3: Project Conversation Context, Files & Attachments | Strong user value: usable governed project context and file handling. | Uses Epic 1/2 output; no forward dependency found. | Stories are decomposed well from FR22 and attachment concerns. | Pass. |
| Epic 4: Governed AI Action Mediation | Strong user value: safe AI help with proposal, approval, refusal, execution. | Uses Epic 1/3 output; no forward dependency found. | ACs cover classifier, approval, preview, refusal, and allowlist. | Pass. |
| Epic 5: Cross-Surface Parity — CLI & MCP | User value for developers, automation builders, and AI agents. | Uses Epic 1 command spine; no forward dependency found. | Thin-adapter constraint and parity tests are clear. | Pass. |
| Epic 6: Outbound Communication & Inbound Authenticity | Clear user/security value around governed outbound and inbound authenticity. | Uses Epic 4/5/7 policy foundations but no dependency on later epics found. | ACs are concrete and failure-aware. | Pass. |
| Epic 7: Tenant Administration & Governance Policy | Clear tenant-admin/operator value. | Contains a serious enforcement gap from architecture: control-state/rate-limit seams are inert until a durable projection/runtime trigger materializes, but no assignable story in the epic list owns that missing materialization. | Many stories are well-decomposed, especially FR74 subject/action matrix. | Critical defect. |
| Epic 8: Operational Dashboards & Observability | Clear operator value. | Uses prior workflow events and queues. Missing Epic 7 control-state materialization may need to land here or before it. | Stories are measurable and observability-oriented. | Pass with dependency caution. |
| Epic 9: Tamper-Evident Audit, Compliance Investigation & Recovery | Clear compliance/operator value. | Uses audit/event foundations; no forward dependency found. | Strong ACs for WORM, completeness, replay, retention, export, erasure, recovery. | Pass. |
| Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption | Mixed: strong user value for governed chat, but also technical shell migration. | Story 10.6 is explicitly blocked until the AI-response streaming transport ADR is accepted. Epic 10 is required for MVP readiness sign-off, so this is a release-readiness blocker. | Stories 10.1-10.3 are migration-focused; 10.4-10.7 are user/release-readiness focused. | Critical defect. |

### Critical Violations

#### CR-1: Required Epic 10 Story Is Blocked By An Unresolved Architecture Decision

- **Where:** `epics.md` Story 10.6, `architecture.md` Frontend Architecture, `epic10-chat-surface-elaboration.md` Open dependency.
- **Violation:** A required MVP readiness epic contains an explicitly blocked story: Story 10.6 cannot be assigned until the AI-response streaming transport ADR is accepted.
- **Impact:** Epic 10 is required before MVP readiness sign-off, but the plan still contains an unresolved architecture decision on a required story path. This violates implementation readiness and creates a forward dependency on unplanned decision work.
- **Recommendation:** Add an explicit assignable ADR/story before Story 10.6, or split Story 10.6 into `10.6a streaming transport ADR` and `10.6b streaming UX implementation`. Do not mark Epic 10 assignment-ready until the ADR exists and the chosen transport preserves `never trust payload` and fail-closed semantics.

#### CR-2: Epic 7 Claims Disable/Quarantine/Rate-Limit Value But Enforcement Materialization Is Deferred Outside The Epic Plan

- **Where:** `architecture.md` notes that control-state and rate-limit enforcement seams read from `AlwaysActive...` / `AlwaysUnlimited...` provider defaults and remain inert until a durable read-side projection plus periodic runtime trigger materializes; that work is described as deferred beyond Epic 8 retro action items. `epics.md` maps FR74/FR75 to Epic 7 stories 7.12-7.26.
- **Violation:** The epic/story plan claims user-facing governance control coverage, but the architecture context says the enforcement path remains inert until missing work lands. That is a hidden forward dependency and makes stories 7.12-7.26 risk becoming UI/audit shells rather than effective controls.
- **Impact:** FR74 and FR75 may appear covered while actual disable/quarantine/rate-limit enforcement is not implementation-ready. Tenant-admin safety controls could be accepted without affecting runtime behavior.
- **Recommendation:** Add explicit assignable story coverage for durable control-state projection, runtime enforcement providers, periodic trigger/materialization, and tests proving disabled/quarantined/rate-limited subjects are actually blocked or throttled. Place it before 7.12-7.26 or as a prerequisite Epic 8 story, and update the FR coverage map.

### Major Issues

#### MA-1: Epic 1 Is A Technical Foundation Epic Disguised As A User-Value Epic

- **Where:** Epic 1 stories 1.1a-1.8 and 1.10-1.13.
- **Issue:** Epic 1 is mostly scaffold, contract spine, gateway, audit seam, idempotency, lifecycle, message catalog, tests, and fixtures. Story 1.9 provides the user-observable value anchor, but most stories are technical enablers.
- **Impact:** This is acceptable only because this is a greenfield Hexalith module and the starter-template requirement makes setup unavoidable. Without strict sequencing to Story 1.9, the epic can drift into platform construction without user value.
- **Recommendation:** Keep the value-anchor invariant mandatory: every foundation story must unblock or protect Story 1.9. Sprint planning should reject any Epic 1 story that cannot name that link.

#### MA-2: Epic 10 Combines Product Capability With Technical Migration

- **Where:** Epic 10 title and stories 10.1-10.3.
- **Issue:** The epic combines governed interactive chat with FrontComposer Shell adoption and surface migrations. Stories 10.1-10.3 are primarily technical migration/recomposition stories.
- **Impact:** This weakens epic user-value clarity and can obscure whether chat/product readiness or shell migration is the actual acceptance driver.
- **Recommendation:** Keep Epic 10 only if release-readiness closure is the explicit value proposition. Otherwise split into a shell-migration epic and a governed-chat epic, or add user-facing acceptance outcomes to 10.1-10.3 beyond build/snapshot migration.

#### MA-3: Story 2.8 Depends On Story 8.6 For Production Saga Claims

- **Where:** Story 2.8 ownership note and Story 8.6.
- **Issue:** Story 2.8 owns correction propagation semantics but explicitly does not claim production saga readiness until Story 8.6 binds hosted Dapr Workflow and validates production runtime behavior.
- **Impact:** The story is completable, but its user-value statement can be overread as production-safe correction propagation. That creates risk of premature acceptance.
- **Recommendation:** Make the acceptance boundary explicit in story status and sprint plans: Story 2.8 is a coordinator/activity seam and aggregate lifecycle story; production saga readiness is incomplete until Story 8.6 passes.

#### MA-4: ADR Prerequisites Are Documented But Not All Are Represented As Assignable Work

- **Where:** `epics.md` architecture requirements notes list ADRs/spikes required before dependent work: WORM backing technology, audit/execute transactionality, M365/Graph specifics, saga, schema evolution/upcasting, etc.
- **Issue:** Several decision prerequisites are called out as required before dependent work, but the epic plan does not consistently represent them as assignable stories or acceptance prerequisites.
- **Impact:** Teams may discover blockers mid-story instead of planning them as implementation work.
- **Recommendation:** Add explicit ADR/spike stories or acceptance criteria in the owning epics, especially before audit-store implementation, M365 intake, and production saga orchestration.

#### MA-5: Spine-Only UX Requires Strong Story-Level Import Discipline

- **Where:** Cross-cutting acceptance/planning guidance and UX documents.
- **Issue:** UX intentionally ships as a contract spine, not mockups. This is acceptable, but every surface story must import UX tables as binding context.
- **Impact:** If story authors omit those anchors, the story may pass functionally while failing accessibility, redaction, responsive, or localization requirements.
- **Recommendation:** Add a standard story checklist item: every UI/surface story must cite the relevant UX-DR, surface state table, accessibility floor, and M1/M2 surface gate row when applicable.

### Minor Concerns

#### MI-1: Parent Planning Containers Still Include Historical Acceptance Context

- **Where:** Story 1.1 and Story 7.27.
- **Concern:** Both are explicitly non-assignable parent planning containers, but they retain historical acceptance context.
- **Impact:** Story automation or human sprint planning could accidentally treat the parent as assignable despite the warning.
- **Recommendation:** Keep these parent headings out of sprint-status candidate lists and rely only on child stories 1.1a-1.1d and 7.27a-7.27b.

#### MI-2: Several Story Titles Are Technical Even When Acceptance Is Good

- **Examples:** Story 1.5 `Two-altitude idempotency`, Story 1.10 `Architecture dependency fitness tests`, Story 1.11 `Differential-conformance harness`, Story 8.2 `Operational telemetry emission`.
- **Concern:** The bodies explain value, but the titles read as technical tasks.
- **Recommendation:** In sprint story files, preserve the user/operator/security outcome in the title or subtitle.

#### MI-3: Inline Given/When/Then Formatting In Epic 10 Is Less Consistent

- **Where:** Stories 10.1-10.5 use compact inline Given/When/Then formatting.
- **Concern:** The criteria are testable, but less scannable than the rest of the epic file.
- **Recommendation:** Expand Epic 10 ACs to the same multi-line Given/When/Then format before story creation.

### Best Practices Compliance Checklist

| Check | Status | Notes |
| --- | --- | --- |
| Epics deliver user value | Mostly pass | Epic 1 and Epic 10 need explicit value framing discipline. |
| Epic independence | Partial | No Epic N depends on Epic N+1 for basic sequence, but Story 2.8 → 8.6 and Story 10.6 ADR blocker need explicit handling. |
| Stories appropriately sized | Mostly pass | 119 assignable stories, with two parent containers properly marked non-assignable. |
| No forward dependencies | Fail | Story 10.6 is blocked by ADR; Epic 7 enforcement materialization is missing/deferred. |
| Database/entity creation timing | Pass | No evidence of all-tables-up-front planning; state and infrastructure are introduced by capability slices. |
| Clear acceptance criteria | Mostly pass | Strong BDD coverage overall; Epic 10 formatting should be normalized. |
| Traceability to FRs maintained | Pass | Step 3 verified 111/111 FR coverage. |

### Quality Assessment Summary

The plan is unusually strong on traceability, safety-floor discipline, and acceptance specificity. It is not ready to start implementation without corrections because two defects are structural: a required story is blocked by an unresolved ADR, and governance control enforcement appears claimed without an assignable story for the durable enforcement materialization. Resolve those before treating the full plan as implementation-ready.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY** for full implementation.

The planning package is strong in document completeness, formal PRD extraction, FR-to-epic traceability, UX contract coverage, and testable acceptance criteria. The readiness blocker is not volume or clarity; it is structural implementation risk. Two critical defects must be resolved before the plan should be treated as implementation-ready.

### Critical Issues Requiring Immediate Action

1. **Close the Epic 7 governance enforcement gap.** Add assignable story coverage for durable control-state projection, runtime enforcement providers, periodic trigger/materialization, and tests proving disabled, quarantined, and rate-limited subjects are actually blocked or throttled. Update the FR coverage map for FR74/FR75 after the story is added.
2. **Resolve the Story 10.6 streaming transport blocker.** Complete an ADR or split Story 10.6 into an ADR/transport foundation story plus the user-facing progressive render and stop/cancel interaction story. Do not assign Story 10.6 until the architecture decision is accepted.

### Recommended Next Steps

1. Add or revise stories for the Epic 7 control-state and rate-limit materialization path, then re-run the FR coverage and epic quality checks for FR74 and FR75.
2. Add a streaming transport ADR/story dependency for Epic 10 and update Story 10.6 so its acceptance criteria are no longer blocked by an unresolved architecture decision.
3. Clarify production-readiness boundaries for Story 2.8 and Story 8.6 so correction propagation is not accepted as production saga readiness before hosted Dapr Workflow validation passes.
4. Represent required ADRs and spikes as assignable work or explicit story prerequisites before audit-store, M365 intake, saga, schema evolution, and streaming implementation stories.
5. Normalize Epic 10 acceptance criteria formatting and require every UI/surface story to cite the relevant UX decision record, surface state table, accessibility floor, and M1/M2 gate row where applicable.

### Final Note

This assessment recorded **12 issue findings**: 2 critical violations, 5 major issues, 3 minor concerns, and 2 UX warnings. Document discovery, PRD extraction, UX presence, and FR coverage were sufficient; the 111 PRD functional requirements were all accounted for in the epic coverage map. Address the critical issues before proceeding to full implementation. Narrow preparatory work can proceed only if it does not depend on the unresolved governance-enforcement and streaming-transport decisions.

**Assessment Date:** 2026-06-09  
**Assessor:** Codex using `bmad-check-implementation-readiness`
