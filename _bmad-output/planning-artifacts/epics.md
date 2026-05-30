---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
status: complete
completedAt: "2026-05-29"
epicCount: 9
storyCount: 64
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md"
  - "_bmad-output/planning-artifacts/architecture.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md"
---

# Hexalith.ChatBot - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.ChatBot, decomposing the requirements from the PRD, UX Design, and Architecture into implementable stories.

Hexalith.ChatBot turns project email threads into structured, auditable workspaces where people and AI agents act with clear project context, authorization, and traceability. It is a B2B SaaS orchestration layer over existing Hexalith bounded contexts (Projects, Parties, Folders, Tenants, Conversations, EventStore, Memories, FrontComposer). The MVP is a single release delivered in three dependency-ordered increments: **M0** (vertical thesis path, UI-only) → **M1** (cross-surface parity + full governance) → **M2** (operations, recovery, continuity). Architecture and epics must preserve the M0 → M1 → M2 dependency order.

## Requirements Inventory

### Functional Requirements

> 96 base FRs plus sub-FRs (FR48a–d, FR55a, FR75a–g, FR81a, FR91a, FR95a). Increment tags: **[M0]** vertical loop · **[M1]** parity+governance · **[M2]** ops+recovery. Where the PRD attaches acceptance detail, a condensed `Accept/Contract/Decompose` note is preserved for story authoring; the PRD remains authoritative.

**Project Email Intake and Association (FR1–FR12)**

- FR1: The system can capture authorized mailbox events as project collaboration inputs. **[M0]**
- FR2: The system can preserve source email identity, thread identity, mailbox identity, sender, recipients, timestamps, and attachment references. **[M0]**
- FR3: The system can associate incoming email with an existing project using deterministic evidence. **[M0]**
- FR4: The system can detect ambiguous project association and route it to human review. **[M0]**
- FR5: Authorized users can review candidate projects with visible evidence, confidence state, reason codes, and the consequences of each available decision. **[M0]**
- FR6: Authorized users can choose a candidate project, reject all candidates, defer association, mark an item as needing review, and provide an optional decision note. **[M0]**
- FR7: Authorized users can correct a previously selected project association. **[M0]**
- FR8: The system can record association decisions, corrections, rejections, deferrals, retries, and skipped items. **[M0]**
- FR9: Tenant administrators can configure project association rules, evidence requirements, and the confidence thresholds `T_high` and `T_low`. Both knobs are security-sensitive (tenant-admin auth, audit event, schema-bounded range, not changeable by service clients/AI actors). Score domain, signals, safe defaults, calibration, guardrails per addendum §Confidence Thresholds. **[M0]**
- FR10: The system can preserve original email context when association is rejected, deferred, failed, skipped, or awaiting review. **[M0]**
- FR11: The system can expose deterministic association reasons and confidence inputs in machine-readable form for UI, CLI, MCP, audit, and test verification. **[M0]**
- FR12: Authorized users can compare candidate project evidence side by side when resolving ambiguous association. **[M0]**

**Participants, Identity, and Authorization (FR13–FR20)**

- FR13: The system can resolve internal and external email participants to tenant-scoped parties. **[M0]**
- FR14: Authorized users can identify unresolved participants for review. **[M0]**
- FR15: External participants can contribute project context through email without requiring MVP external portal access. **[M0]**
- FR16: The system can enforce tenant and project authorization before exposing project candidates, files, conversations, approvals, commands, or audit details. **[M0]**
- FR17: The system can block unresolved or unauthorized actors from accessing project files, creating task requests, triggering commands, or sending outbound communication. **[M0]**
- FR18: Tenant administrators can configure governed mailbox participation rules. **[M0/M1]**
- FR19: Authorized administrators can configure service-client access for CLI, MCP, background workers, mailbox events, and AI actors. **[M1]**
- FR20: The system can record consent or lawful-basis metadata where tenant policy requires it for external participants, retained email content, attachments, and AI processing.

**Project Conversation and Context (FR21–FR28)**

- FR21: Authorized users can view email-derived messages as project conversation context. **[M0]**
- FR22: The system can represent associated email, participants, attachments, decisions, approvals, failures, and AI outcomes in the project context. *Decompose:* seven first-class concerns → seven sub-stories (associated-email / participant / attachment / decision / approval / failure / AI-outcome rendering), each hosted on surface S1, acceptance-tested independently. **[M0]**
- FR23: Authorized users can inspect why an email belongs to a project, including source evidence, confidence signals, human decisions, and later corrections. *Accept:* "why" panel shows originating signal class, matched value, confidence score, threshold band (`auto`/`ambiguous`/`fail-closed`), decision actor, timestamp, and links to any superseding correction with its own evidence panel. **[M0]**
- FR24: Authorized users can see association, attachment, task, approval, command, failure, retry, and next-action status for a project conversation. **[M0]**
- FR25: The system can keep project conversation context separate across tenants and projects. **[M0]**
- FR26: The system can distinguish informational project context from actionable requests. *Accept:* every email carries a visible `informational`/`actionable` badge; `actionable` items surface detected intent (FR35) + next-action affordance; classification from the tag+heuristic kernel and reproducible. **[M0]**
- FR27: The system can distinguish system-generated summaries from source evidence. *Accept:* AI content visually distinct (typography + `AI summary` label), one-line provenance string (`Generated by <model+version> at <timestamp> from <source-evidence-IDs>`), collapsible to source; source evidence is default, AI summaries opt-in; non-color status (WCAG 2.2 AA). **[M0]**
- FR28: The system can preserve visible human-review history for each email, attachment, approval, AI action, and command. **[M0]**

**Files and Attachments (FR29–FR34)**

- FR29: The system can capture attachments from associated project email. **[M0]**
- FR30: The system can store captured attachments in governed project folders. **[M0]**
- FR31: Authorized users can inspect attachment capture and storage status. **[M0]**
- FR32: The system can prevent unauthorized actors from viewing attachment metadata or content. **[M0]**
- FR33: The system can make authorized project files available as scoped AI context only through explicit authorization, policy checks, and auditable context packaging. **[M0]**
- FR34: The system can represent attachment states including captured, pending, unavailable, rejected, unsafe, failed, and retryable. **[M0]**

**Task Intent and AI Action Mediation (FR35–FR46)**

> Risk classes (PRD table): Low-risk read-only (allow only per tenant policy + project authz); Approval-required (pause for human approval — six risky classes); Denied (refuse + audit); Unsupported (decline/route to manual). Mixed requests inherit the strictest applicable risk class.

- FR35: The system can detect candidate task or action intent from authorized project conversation actors and preserve source message evidence. *Contract:* task-intent record carries `tenant_id`, `project_id`, `source_message_id`, `requester_party_id`, `detected_intent_summary` (≤280), `detected_action_kind` enum, `source_evidence_offsets`, `kernel_version`, `confidence_score` [0,1], `detected_at`, `state`. Precision/recall targets ≥80%/≥75% by M0, ≥90%/≥85% by M1 (A9a). **[M0]**
- FR36: Authorized users can review captured task intent before governed action (displays the FR35 data contract + full source message + available transitions). **[M0]**
- FR37: Authorized users can convert captured task intent into a governed task or action request (creates the FR41 proposal record, links to source, itself audited). **[M0]**
- FR38: Authorized users can mark captured task intent as not actionable, duplicate, already handled, or out of scope (each terminal; record preserved for A9a; duplicate links predecessor). **[M0]**
- FR39: The system can classify AI action requests by risk. **[M0]**
- FR40: The system can allow low-risk AI assistance when tenant policy and project authorization permit it. **[M0]**
- FR41: The system can require approval before AI actions that modify project state, expose files, send external communication, create or assign tasks, invoke tools, or act on behalf of a participant. *Note:* default approval-required for the six risky classes; tenant admins may downgrade `low-risk-allowed` per class; tuning rule guards NFR46 approval fatigue (rubber-stamp >15% rolling-7d triggers revisit). **[M0]**
- FR42: Authorized users can approve or reject proposed AI actions after reviewing the action summary, affected resources, recipients, sender authority, risk classification, and expected outcome. *Accept:* approval surface shows command name (current allowlist version), input files (tappable evidence refs w/ redaction state), recipients, sender-authority class, risk classification + producing input tuple, policy snapshot ID, expected post-state, decisions `approve`/`reject`/`request-revision`/`cancel`; `approve` disabled-with-reason when user lacks authority. **[M0]**
- FR43: The system can execute approved AI actions only through allowlisted governed commands (M0 allowlist = `Project.AppendConversationMessage` only, per addendum §Command Allowlist v0). **[M0]**
- FR44: Authorized users can inspect AI action proposals, approvals, denials, executions, failures, and outcomes. **[M0]**
- FR45: Authorized users can preview outbound communication, file access, command execution, and AI-generated changes before approval or execution. **[M0]**
- FR46: The system can refuse or block unsafe AI, automation, command, or mailbox requests that exceed tenant policy, project authorization, sender authority, or approved command scope. **[M0]**

**Outbound Communication (FR47–FR50)**

- FR47: Authorized users can create outbound project email drafts within approved project and sender authority. **[M1]**
- FR48: The system can distinguish draft-only, authenticated-user send, shared-mailbox send, send-on-behalf, and approved service-send authority. M365→ChatBot authority-class mapping per addendum §Inbound Message Authenticity; conflict (M365 grants send-on-behalf but ChatBot grants none) → fail closed. **[M1]**
- FR48a: Inbound provider authenticity passthrough — record M365/Exchange DMARC/DKIM/SPF verdicts as-supplied; ChatBot does not re-verify. **[M1]**
- FR48b: Inbound header inspection — parse `Received`/`Authentication-Results`/`From`/`Reply-To`/`Sender`/`X-Original-Sender`; record `From`/`Sender`/`Reply-To` disagreements as intake metadata (feeds risk classifier; surfaces to reviewer; does not block ingestion). **[M1]**
- FR48c: On-behalf-of disambiguation — recorded sender authority is the delegate's identity; principal preserved as `principal_for`; outbound follows the same rule. **[M1]**
- FR48d: External-sender posture — senders with no resolved tenant party flagged `external_sender = true`; `mailbox.authenticity-strictness` knob (`permissive`/`strict`/`paranoid`) controls auto-associate / NeedsReview / fail-closed. **[M1]**
- FR49: The system can require approval before outbound project communication leaves the project boundary. **[M1]**
- FR50: The system can preserve proposed content, approved content, recipients, sender authority, project context, requester, approver, and decision outcome in approval records. **[M1]**

**Admin, Governance, and Audit (FR51–FR63)**

- FR51: Tenant administrators can configure mailbox integration settings and monitored mailbox patterns. **[M0/M1]**
- FR52: Tenant administrators can configure AI action policy for low-risk and approval-required actions. **[M1]**
- FR53: Tenant administrators can review mailbox permission status and degraded mailbox processing states. **[M1]**
- FR54: Compliance or support reviewers can investigate association decisions, approval decisions, command outcomes, and risky AI actions. **[M1/M2]**
- FR55: The system can produce audit records for security-sensitive association, participant, file, approval, command, AI, retry, and duplicate-handling events. **[M0]**
- FR55a: Cross-tenant isolation in derived stores — vector indexes, embedding stores, prompt-context caches, candidate-ranking caches enforce tenant isolation by construction (per-tenant partition or row-level scoping verified at every read); cross-tenant queries impossible at the store-access layer; periodic isolation probe (NFR59). **[M2]**
- FR56: Authorized users can query audit records by tenant, actor, command, resource, decision, reason, correlation, and time context. **[M1/M2]**
- FR57: The system can hide unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details. **[M0]**
- FR58: Authorized administrators or reviewers can access operational support for tenant data retention, export, and deletion workflows (may be partly manual in M0/M1; dashboards in M2). **[M1/M2]**
- FR59: The system can propagate correlation context across mailbox intake, project association, file handling, approval, AI mediation, command execution, audit, UI, CLI, and MCP. **[M0]**
- FR60: The system can preserve source evidence used for association, authorization, approval, rejection, refusal, correction, retry, and audit investigation with retention boundaries and redaction behavior. **[M0]**
- FR61: The system can maintain versioned policy snapshots used for association, authorization, approval, AI action classification, and command execution decisions. **[M0]**
- FR62: Authorized users can add human notes or resolution rationale to association, participant, approval, retry, quarantine, and correction decisions. **[M0/M1]**
- FR63: Authorized users can supersede reversible human decisions where policy permits while preserving the original decision in audit history. **[M0/M1]**

**Tenant-Admin Permission Model (FR75a–FR75g, M1)**

- FR75a: `tenant-admin` holds the union of FR75b–FR75g scopes; finer roles (`mailbox-admin`, `policy-admin`, `compliance-admin`, `operations-admin`) hold proper subsets. Admin assignment is security-sensitive (audit event; not by service clients/AI actors). **[M1]**
- FR75b: See-only scopes — admins read queue summaries/health/aggregate metrics across tenant projects without per-project membership; per-item detail requires per-project authority. **[M1]**
- FR75c: Operate scopes — queue-level operations (retry/requeue/quarantine/dismiss) on see-only items, recorded with admin identity/items/queue/reason; cannot mutate project-level records. **[M1]**
- FR75d: Policy scope (`policy-admin`) — mutate Tenant Policy Schema knobs; security-sensitive knobs require a second admin approval (two-person rule) + documented justification in audit. **[M1]**
- FR75e: Mailbox scope (`mailbox-admin`) — configure mailbox patterns/routing rules/provider-credential connections; cannot read content or decide associations. **[M1]**
- FR75f: Compliance scope (`compliance-admin`) — read audit across tenant (per-project redaction NFR2), trigger investigations, configure retention within NFR49a bounds; cannot operate on workflow items. **[M1]**
- FR75g: Audit obligation on every admin action (incl. read-only dashboard access above an aggregation threshold) — admin identity/scope/items/timestamp; no skip-audit path; tenant-admin does not bypass NFR15a/NFR50a. **[M1]**

**Reliability, Failure Handling, and Operations (FR64–FR80)**

- FR64: The system can detect duplicate mailbox delivery and avoid duplicate project artifacts. **[M0]**
- FR65: The system can retry failed mailbox, attachment, association, approval, command, and projection work where retry is valid. **[M0]**
- FR66: The system can surface terminal and non-terminal failure states to authorized users. **[M0]**
- FR67: The system can expose mailbox health, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, and audit projection status. *Accept:* each queue/health view renders name, current depth/status enum (`healthy`/`degraded`/`failed`/`unknown`, not count-derived), oldest item age, owner role, link to per-item detail; refresh within NFR6 staleness; freshness timestamp per NFR48. **[M0 minimal / M2 dashboards]**
- FR68: The system can fail closed when project association, participant identity, tenant scope, authorization, audit writing, or required dependencies cannot be resolved. **[M0]**
- FR69: Authorized users can view and manage queues for ambiguous associations, unresolved participants, pending approvals, failed ingestion, failed attachment handling, and retryable operations. **[M0/M1]**
- FR70: Authorized users can assign or claim review items that require human resolution. **[M1]**
- FR71: Authorized users can see the next required human action for an email, task intent, attachment, approval, or failed operation. **[M0]**
- FR72: The system can notify authorized users when review, approval, failure, degraded mailbox, quarantine, or retry states require attention. **[M1]**
- FR73: Tenant administrators can configure notification routing and escalation rules for unresolved review, approval, degraded, quarantine, and failure states. **[M1]**
- FR74: Authorized administrators can disable, quarantine, or rate-limit mailbox sources, service clients, AI actors, or command capabilities producing unsafe/invalid/excessive/policy-violating activity. *Decompose:* five subject classes × three actions → per-(subject × action) stories; disable & quarantine are security-sensitive (FR75d two-person rule), rate-limit is standard. **[M1]**
- FR75: Authorized administrators can configure per-tenant rate limits, quotas, and circuit breakers for mailbox processing, AI mediation, command execution, and outbound communication. **[M1]**
- FR76: The system can present review items with clear available actions, disabled-action reasons, and next-step guidance based on item state and user authorization. *Accept:* every affordance is `enabled`/`disabled-with-reason`/`not-applicable-hidden`; disabled reasons from a finite set (`insufficient-authority`/`state-not-permitted`/`dependency-degraded`/`awaiting-other-actor`/`policy-blocked`), not raw error text; next-step points to a responsible role/action, never "contact support". **[M0]**
- FR77: The system can explain refusal, blocked, degraded mailbox, failed attachment, failed command, and authorization-denied states in user-safe language without exposing restricted evidence. *Accept:* messages drawn from a versioned message catalog — stable code, user-safe headline ≤80 chars, one-sentence reason naming no unauthorized projects/files/parties/audit detail (NFR2), safe next-action affordance; restricted detail stays in audit only. **[M0]**
- FR78: Authorized users can filter, sort, and prioritize operational queues by age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action. **[M1]**
- FR79: The system can show stale, waiting, blocked, and escalation-needed states for review queues and long-running operations. **[M0/M1]**
- FR80: UI, CLI, and MCP users can retrieve long-running operation status including operation identity, current state, retry count, partial outputs, safe next actions, terminal reason, and correlation context. **[M0 UI / M1 CLI+MCP]**

**Cross-Surface Command Parity (FR81–FR86)**

- FR81: Authorized UI users can perform the core governed email-to-project workflow operations. **[M0]**
- FR81a: Shared command pipeline (architectural invariant) — every state-mutating operation, from any surface (UI/CLI/MCP/service client/AI actor/background worker), passes through a single command-handling pipeline applying, in order: authentication, tenant-scope binding, authorization, risk classification, approval gate, idempotency check, command execution, audit emission, projection update. Surface adapters translate input into a typed Command record and cannot replicate any stage. Parity follows by construction; architecture review must reject adapter designs that bypass stages. **[M0 spine / M1 surfaces]**
- FR82: Authorized CLI users can inspect, associate, reject, defer, correct, retry, approve, execute, check status, and query audit for the same governed workflow (CLI adapter over the FR81a pipeline). **[M1]**
- FR83: Authorized MCP clients can access the same governed workflow operations for AI-agent/automation use (MCP adapter over the FR81a pipeline). **[M1]**
- FR84: The system can return equivalent authorization outcomes and state transitions across UI, CLI, and MCP (verification of FR81a; divergence = defect against FR81a). **[M1]**
- FR85: The system can identify whether an action originated from UI/API, CLI, MCP, background worker, mailbox event, or AI actor; origin attached at the adapter boundary, travels with the Command record into audit, immutable downstream. **[M0/M1]**
- FR86: Contract tests must verify the FR81a invariant for each surface — equivalent input → same Command record after canonical normalization; test failure is an invariant violation, not a tolerance threshold. **[M0 shims / M1 full]**

**Workflow State, Contracts, and Testability (FR87–FR96)**

- FR87: The system can define canonical lifecycle states for email intake items, participant resolution, attachment handling, approvals, AI actions, command executions, and audit projection. **[M0]**
- FR88: The system can validate inbound and outbound workflow state transitions against an explicit state model. **[M0]**
- FR89: The system can reject invalid state transitions and record the rejected transition, actor, reason, and correlation context. **[M0]**
- FR90: The system can expose idempotency keys and stable resource identifiers for mailbox events, email messages, attachments, approvals, commands, retries, outbound communication, and audit records. **[M0 keys / M2 full contract]**
- FR91: The system can separate immutable source records from derived project projections and rebuild derived projections from source records when needed. **[M0]**
- FR91a: Correction propagation contract — on correction (FR7) every derived store referencing the original association is invalidated and rebuilt (candidate ranking, evidence snapshot, AI proposals that consumed misassigned context, vector index entries [M2], queue projections); user-facing state during reindex is `correcting` (progress + ETA); item stays `correcting` until all derived stores acknowledge; AI actions cannot use corrected context until invalidation completes; audit records predecessor, correction, per-store invalidation outcome. **[M0/M1]**
- FR92: Authorized product or QA users can maintain internal evaluation datasets derived from consented, redacted, or synthetic project examples with expected outcomes, redaction expectations, and regression result history. **[M0/M1]**
- FR93: The system can provide tenant-scoped test fixtures or sandbox data for validating mailbox intake, association, authorization, attachment handling, approval, AI mediation, command execution, and audit behavior. **[M0/M1]**
- FR94: The system can expose measurable operational outcomes for ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag (FR67 queues in M0/M1; OpenTelemetry metrics to tenant dashboard in M2 per NFR42a). **[M0/M2]**
- FR95: The system can simulate or replay representative mailbox events for authorized QA or support investigation without sending external communication or mutating production project state. **[M2]**
- FR95a: Replay isolation contract — replay/simulation runs against a dedicated test tenant; test-tenant outbound adapter intercepts and records instead of sending; replay events carry `replay_run_id` in audit; production audit queries exclude replay; NFR50a excludes replay from numerator/denominator; nightly probe asserts no replay record in any production tenant's outbound-trace store and gates M2 release. **[M2]**
- FR96: The system can make recorded correction decisions available as future association evidence only when tenant policy permits, the evidence remains explainable, and users can inspect why it influenced a match. **[M1/M2]**

### NonFunctional Requirements

> 70 base NFRs plus sub-NFRs (NFR9a, NFR13a, NFR15a, NFR17a, NFR42a, NFR49a, NFR50a). NFR17a is catalogued under Reliability though it appears near FR91a in the PRD.

**Security and Privacy (NFR1–NFR12)**

- NFR1: All command and query operations must enforce tenant, actor, role, project, and resource authorization before returning data or mutating state.
- NFR2: Unauthorized users/CLI/MCP/AI actors/service clients/mailbox events must receive redacted failure responses revealing no restricted project names, file metadata, candidate evidence, audit details, or tenant data.
- NFR3: Email content, attachments, AI prompts/outputs, audit records, tokens, policy snapshots, logs, traces, backups, and evaluation datasets must be encrypted in transit and at rest; release validation verifies TLS, encrypted storage per data class, and no plaintext export of protected content.
- NFR4: Secrets and all credential classes (mailbox/service-client/CLI/MCP/AI-tool/AI-provider) must not be exposed in logs, traces, CLI output, MCP responses, audit payloads, support bundles, or diagnostics.
- NFR5: M365/Exchange permissions and all credential classes must follow least-privilege scope and support revocation without broad fallback access.
- NFR6: Authorization/policy/identity caches must have bounded staleness and revocation-sensitive invalidation; default max staleness 5 minutes for ordinary policy changes, 60 seconds for explicit revocation, verified by automated revocation tests.
- NFR7: Security-sensitive operations must fail closed when identity, tenant scope, authorization, audit readiness, policy evaluation, or required command validation is unavailable.
- NFR8: AI actors must operate only through explicitly authorized project scope, files, tools, commands, and policy-defined authority.
- NFR9: AI prompts/retrieved context/outputs/tool results/summaries must be tenant/project scoped, redacted per policy, logged per retention, and blocked from training/telemetry/reuse outside authorized boundaries unless configured; validation proves every AI context package contains tenant ID, project ID, source evidence refs, policy snapshot ID, redaction decision, retention class, and provider-reuse setting before invocation.
- NFR9a: Derived-store cross-tenant isolation — vector indexes/embedding stores/prompt-context caches/candidate-ranking caches partitioned per tenant at the store level; cross-tenant query through native API must fail at the storage layer; nightly synthetic cross-tenant probe; probe failure is stop-ship. **[M2]**
- NFR10: Logs/metrics/traces/support bundles/test artifacts must pass secret and sensitive-data redaction checks before export or external sharing.
- NFR11: Cross-tenant isolation testing has zero tolerance for unauthorized data exposure across candidates, evidence, files, summaries, prompts, CLI output, MCP payloads, logs, metrics, traces, and audit views.
- NFR12: Data residency/region boundaries must be defined for stored data classes before tenant onboarding when residency is specified; release validation maps each persisted class to an approved region or marks it not residency-constrained.

**Reliability and Data Integrity (NFR13–NFR22)**

- NFR13: Mailbox intake, attachment capture, association decisions, approvals, command execution, outbound, notifications, and audit projection must be idempotent per operation with stable key, replay window, conflict response, and same final observable state for repeated equivalent inputs.
- NFR13a: Per-operation idempotency contract — key composition, replay window, equivalence rule, conflict response per addendum §Idempotency Keys (eight operation classes); new classes must extend the table before shipping.
- NFR14: Duplicate mailbox delivery must not create duplicate project messages, attachments, task intents, approvals, commands, notifications, outbound emails, or audit decisions.
- NFR15: Invalid workflow state transitions must be rejected before mutation with deterministic error + audit event. If audit storage is unavailable, **every** state-mutating transition fails closed (not only security-sensitive ones).
- NFR15a: Fail-Closed Contract (invariant) — enforced by construction at every durable-state-writing path. Ten enumerated paths (M365 intake, deterministic association, ambiguous/user association, correction, AI proposal, approval, command execution, outbound send, tenant policy mutation, allowlist mutation) each with fail-closed conditions; no "audit unavailable → continue" branch; "audit writer down" returns typed `AuditUnavailable`, queues intent for replay (no state write), emits operator alert.
- NFR16: Risky AI actions, external sends, command execution, and project-file context packaging must not execute unless approval state, policy snapshot, actor authority, input-contract validation, and audit readiness are verified.
- NFR17: Partial failures must leave affected workflow items in visible, recoverable states (pending/retryable/failed/quarantined/needs review).
- NFR17a: Correction propagation latency — p95 ≤ 10 min (M0/M1, no vector index) and ≤ 60 min (M2, incl. vector reindex); items beyond SLO surface `correction-delayed` w/ owner role + next safe action; SLO breach = P2 incident. **[M0/M1; M2 vector SLO]**
- NFR18: Retry policy must specify retryable vs terminal errors, max attempts, backoff, jitter, dead-letter criteria, manual recovery, and operator-visible terminal reasons per workflow type.
- NFR19: Background workers/async processors must support at-least-once delivery safely via idempotency, concurrency control, lease/lock expiry, and poison-message handling.
- NFR20: Queue processing must prevent starvation across tenants/mailboxes/projects/item types while respecting priority, rate limits, and circuit breakers.
- NFR21: File/attachment processing must enforce malware/unsafe-content policy, size limits, type restrictions, scan status, quarantine behavior, and safe failure states before project or AI exposure.
- NFR22: Non-AI review/association/approval/retry/audit workflows must continue during AI provider outage when their non-AI dependencies are available; outage tests prove resolution/approval/retry/audit work without live AI.

**Performance and Scalability (NFR23–NFR30)**

- NFR23: Tenant/deployment operating baselines must be documented, versioned, reviewed at least quarterly, and used as the reference for latency, backlog, recovery, alerting, dataset size, and capacity; each baseline records owner, approval date, review date, accepted thresholds.
- NFR24: User-facing conversation/queue/status/audit lookups must meet default p95 ≤ 2 s under the MVP baseline unless stricter; measured by synthetic checks and production APM.
- NFR25: Ambiguous association candidate generation must complete within 10 s p95 under the MVP baseline, or return pending/manual-review status with retrievable operation identity and safe next actions.
- NFR26: CLI/MCP operations triggering long-running work must return operation identity + current status within 5 s p95 and must not hold the client connection longer than 30 s without a retrievable status response.
- NFR27: Queue views must support filtering, sorting, pagination, prioritization with default page size ≤ 100 and server-side filters for age, risk, confidence, project, mailbox, failure state, assigned reviewer, next action.
- NFR28: Operational latency metrics must include percentile distribution, error rate, retry rate, queue age, saturation indicators, and audit projection lag.
- NFR29: Tenant-level rate limits/quotas/circuit breakers must protect mailbox processing, AI mediation, command execution, outbound, UI/API, CLI, and MCP.
- NFR30: Backlogs in one tenant/mailbox/project/service-client/AI-actor/command-surface must not degrade unrelated tenants or workflow sources where isolation is technically possible.

**Integration and Interoperability (NFR31–NFR36)**

- NFR31: M365/Exchange integration must tolerate revoked permissions, expired tokens, throttling, backoff, partial access, duplicate events, delayed delivery, webhook replay, subscription expiry, and permission drift without silently broadening access.
- NFR32: UI/API, CLI, MCP, workers, webhook/event handlers, persisted events, audit records, projections, and replay fixtures must use contract-verifiable responses/events with stable identifiers, status codes, reason codes, state names, redaction semantics, correlation context, and equivalent authorization outcomes.
- NFR33: API/CLI/MCP/event/audit/projection/state-model contracts must support backward-compatible evolution or explicit versioning, deprecation policy, and migration paths for breaking changes.
- NFR34: Integration requests/events must carry correlation context across all surfaces, workers, and webhooks.
- NFR35: Configuration/policy changes must be auditable, versioned, rollback-capable for non-destructive settings, applied consistently to new work without silently changing completed decisions; destructive/authority-expanding changes require a new version rather than rollback overwrite.
- NFR36: Time-based behavior must use server-side UTC timestamps, preserve source timestamps/timezone context, and convert to tenant-local display only at presentation boundaries.

**Operability and Observability (NFR37–NFR48)**

- NFR37: Authorized operators must observe mailbox health, backlog, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, service-client failures, AI mediation failures, command failures, and audit projection lag.
- NFR38: User-visible status must be separated from privileged diagnostic detail and exposed according to authorization level.
- NFR39: The system must provide actionable status for degraded, stale, waiting, blocked, escalation-needed, failed, retryable, and terminal workflow states.
- NFR40: Degraded/blocked/failed/waiting states must use user-appropriate language with next-action guidance from the FR77 versioned message catalog. *Observable:* production telemetry counts uncategorized states (raw error text to a user); count must be 0 per release; nonzero blocks release.
- NFR41: Degraded dependencies must be isolated to the narrowest identified scope; incident status states affected scope + dependency within 5 minutes of detection when monitoring is available.
- NFR42: During degraded operation, every authorized user-facing surface displays current state enum (FR67), affected scope (NFR41), responsible owner role, and next safe action affordance (FR76); refresh within NFR6 staleness; synthetic checks assert all four elements render.
- NFR42a: SLOs published — ingestion/candidate-gen/ambiguous-resolution/command(per class)/audit-lag/retry-exhaustion/duplicate-suppression/mailbox-failure/approval-queue-age/AI-mediation latency published in the per-tenant operational view (M2) + addendum §Operating Baselines; each SLO has target, window, error budget, alert threshold; initial values per NFR24–27/NFR43; pilot calibration per A11. **[M2]**
- NFR43: Alerting/synthetic health checks must be non-invasive, tenant-safe, tied to documented thresholds; default MVP thresholds include subscription expiry within 7 days, retry exhaustion, audit projection lag > 5 min, approval items older than 2 business days, authorization-failure spikes above tenant baseline.
- NFR44: Runbook-ready diagnostics per workflow item must include correlation ID, tenant ID, mailbox ID, workflow item ID, current state, last transition (timestamp+actor+from-state), retry count, failure reason code (FR77 catalog), next safe action; weekly random sample of 100 items each renders complete diagnostic; missing field is a defect.
- NFR45: Support diagnostics shareable through redacted support bundles preserving correlation/state/reason context without exposing restricted tenant/project/participant/file/message/audit evidence.
- NFR46: Prevent approval fatigue with concrete measurable mechanisms — prioritization `(risk × authority-of-affected-party × time-in-queue)`; grouping by `(requester × command × project)` with one audit event per item; per-user notification ceiling ≤ 8/hr, ≤ 30/day + digest rollup; reviewer backlog alert at > 25 open items; observable median/p95 time-in-queue per risk class; fatigue present when > 15% of approvals in rolling 7d are rubber-stamp (< 5 s against `approval-required`).
- NFR47: Risky automation must distinguish reversible, supersedable, compensating, and irreversible actions before approval.
- NFR48: Every surfaced evidence reference carries a visible freshness indicator (snapshot timestamp + `fresh`/`stale`/`expired` from NFR6 window); reviewers cannot approve against `expired` evidence (approve disabled w/ reason `evidence-expired`); `stale` permitted but flagged; approval surface renders a per-evidence freshness chip (chip count = evidence-reference count).

**Auditability, Compliance, and Data Governance (NFR49–NFR55)**

- NFR49: Audit records must be tamper-evident, retention-governed, redaction-aware, reconstructable, and protected by restricted modification/deletion limited to authorized retention workflows.
- NFR49a: Tamper-evident mechanism — append-only WORM store with hash-chained envelopes (each carries predecessor hash in the tenant chain); deletion impossible at storage layer; redaction = appended redaction record (original preserved encrypted, redaction key in separate KMS); nightly per-tenant chain verification, broken chain alerts on-call security engineer within 5 min; GDPR erasure via projection tombstone + key-shred, never chain mutation. **[M2]**
- NFR50: Audit records must include tenant, actor, actor type, command, resource, decision, reason, correlation, timestamp, policy snapshot ref, source evidence refs, state-transition history, redaction decisions, idempotency key (where applicable), and resulting command/projection/outbound outcome; automated tests verify 100% required-field presence for security-sensitive events in the validation dataset.
- NFR50a: Audit completeness as production observable — fraction of state-mutating operations (NFR15a inventory) whose audit chain reconstructs the operation end-to-end from the chain alone; target ≥ 99.5% per rolling 7-day window per tenant; below triggers P1; reconstructability (not just field presence) is the test; replay excluded per FR95a. **[M2]**
- NFR51: Audit/diagnostic records must preserve enough context to reconstruct who acted, what was attempted, which policy applied, what evidence was used, what transitions occurred, what was redacted, and what outcome occurred.
- NFR52: The system must minimize retained email content, attachment content, prompts, outputs, diagnostics, and support bundles to data required for the authorized workflow, audit, and tenant retention policy.
- NFR53: Tenant data retention/export/deletion workflows must distinguish data classes (source email, metadata, attachments, derived projections, AI prompts/outputs, approvals, policy snapshots, logs, backups, evaluation datasets, audit records).
- NFR54: Audit evidence must respect retention boundaries and redaction rules so evidence preservation does not become uncontrolled data storage.
- NFR55: Where tenant policy/regulatory profile requires, the system must record consent or lawful-basis metadata for external participants, retained content, attachments, and AI processing.

**Recovery and Continuity (NFR56–NFR59)**

- NFR56: Source email records, attachment records, approval history, command history, policy snapshots, and audit records must meet default MVP recovery target RPO ≤ 15 min, RTO ≤ 4 hr unless stricter ([ASSUMPTION] per A10 until M2 drill).
- NFR57: Derived projections must be rebuildable from immutable source records and audit history within default 4 hr for the baseline validation dataset without mailbox re-ingestion.
- NFR58: Dependency outages must degrade only the affected tenant/mailbox/operation/service-client/command-surface/workflow-item when ownership and routing identify scope; outage tests prove no unrelated tenant/mailbox blocked for Graph/identity/AI/command/audit/attachment failures.
- NFR59: Resilience validation must prove degraded Graph access, expired subscriptions, AI provider outage, command execution failure, audit store unavailability, and partial attachment failure cause no cross-tenant leakage, unauthorized mutation, or silent data loss.

**Accessibility and Usability Quality (NFR60–NFR64)**

- NFR60: WCAG 2.2 AA conformance scoped per increment to UI surfaces existing in that increment; CLI/MCP out of WCAG scope. M0 surfaces (conform before M0): ambiguous association review, AI action approval, project conversation view. M1 surfaces: rejection/defer flows, approval-policy configuration UI, M1 admin operational view (FR75a–g). M2 surfaces: M2 operational dashboards. Per-increment validation includes automated checks + keyboard-only + screen-reader review before release.
- NFR61: Accessibility validation must include keyboard-only review flows, screen-reader labels, focus order, non-color status indicators, and error recovery for ambiguous association and approval workflows.
- NFR62: Status/failure/refusal/authorization messages must be understandable without exposing restricted evidence or relying only on color.
- NFR63: Users resolving ambiguous associations or approvals must identify the next available action without reading raw audit logs.
- NFR64: The UI must distinguish source evidence from AI-generated summaries so users make review decisions from authoritative context.

**Validation and Quality Gates (NFR65–NFR70)**

- NFR65: Production releases must meet documented quality gates covering tenant isolation, authorization, redaction, idempotency, state transitions, approval gates, duplicate suppression, and audit creation.
- NFR66: Performance validation must prove mailbox backlog processing, queue usability, retry behavior, audit projection lag, and throttled Microsoft Graph behavior against documented baselines.
- NFR67: Security validation must include negative authorization tests for UI/API, CLI, MCP, background workers, mailbox events, service clients, and AI actors.
- NFR68: Evaluation datasets/test fixtures must use consented, redacted, or synthetic examples with versioning, reproducibility, redaction verification, expected outcomes, and regression result history.
- NFR69: Replay/simulation must be isolated from production mutation, external sends, live AI tool execution, and live command execution; replay artifacts explicitly labeled and tenant-scoped.
- NFR70: Every externally visible operation must define expected state transition, audit event, user-visible response, redaction behavior, and retry/idempotency result.

### Additional Requirements

> Technical requirements from `architecture.md` (and the addendum) that shape epic/story creation. **The starter-template item is load-bearing for Epic 1, Story 1.**

**🟢 Starter Template / Module Scaffold (drives Epic 1, Story 1 — first implementation story):**

- **No external starter template.** External .NET/web starters (Clean Architecture, ABP, generic Blazor) are explicitly **rejected**. ChatBot is a **brownfield product on the fixed Hexalith platform**.
- **Selected starter: a NEW Hexalith module `Hexalith.ChatBot`, scaffolded by convention from the canonical sibling-module template** (no single CLI generator exists — scaffold by convention). Closest structural reference: **Hexalith.Folders** (most complete multi-surface sibling: REST+CLI+MCP+read-only Blazor UI+workers+OpenAPI Contract Spine). Closest domain reference: **Hexalith.Conversations** (email-derived conversation rendering, `IParticipantDirectory` adapter pattern).
- **Hexalith.EventStore added as a root-level git submodule** (`git submodule update --init`, never `--recursive`). Foundation for command/aggregate/projection/query/SignalR/CLI/MCP primitives.
- **Module project layout (.slnx, never .sln):** `Contracts` (low-dep; commands/events/rejections/queries/enums/identities + `openapi/` Contract Spine + `Messages/` catalog), `Client` (typed `IChatBotClient.SubmitAsync(IChatBotCommand)` + `Generated/`), `Server` (the modular monolith — only scanned assembly), `Aspire`, `AppHost` (DAPR topology), `ServiceDefaults` (OpenTelemetry/host), `Testing`. Surface adapters added per increment: `.UI` **[M0]**, `.Cli` + `.Mcp` **[M1]**, `.Workers` **[M0 intake/retry; M2 rebuild/replay]**. `tests/` mirror each project (xUnit v3) + dedicated `Architecture.Tests` (NetArchTest) and `Conformance.Tests`.
- **Root config:** `global.json` (SDK 10.0.300, rollForward latestPatch), `Directory.Build.props` (net10.0, nullable, warnings-as-errors, Allman braces — confirm/override to K&R), `Directory.Packages.props` (central package management, no inline versions), `Directory.Build.targets` (SDK-container opt-in), `.editorconfig`, `nuget.config`, `.gitmodules` (EventStore root-level only), `.github/workflows/` (ci.yml + release.yml semantic-release).
- **Aspire AppHost + DAPR components** (statestore, pubsub, deny-by-default `accesscontrol.yaml`); verify `aspire run` brings up the topology (ChatBot + DAPR sidecars + required siblings + Keycloak `WaitFor` healthy).
- **Adopt the Folders-style Contract Spine early** (OpenAPI 3.1 + NSwag-generated client + parity-oracle rows + idempotency helpers) as the single contract source UI/CLI/MCP adapters bind to (decision D7 — underpins FR81a parity-by-construction).

**Platform technology stack (pinned; do not upgrade casually):**

- .NET 10 / C# 14 (SDK 10.0.300, LTS, net10.0, nullable + warnings-as-errors, central package management).
- Hexalith.EventStore (CQRS/ES, `{tenant}:{domain}:{aggregateId}` identity → ChatBot uses `{tenant}:chatbot:{aggregateId}`; persist-then-publish; pure `Handle`/`Apply`; rejections-as-events; ULIDs not GUIDs; `system` platform tenant; EventStore owns the envelope; each service runs its own EventStore pipeline + AggregateActor 5-step sequence).
- DAPR 1.17.x (at-least-once pub/sub CloudEvents, actors via `IActorStateManager`, **DAPR Workflow** for FR91a correction propagation + multi-context sagas, deny-by-default ACLs). DAPR resources: AppId `chatbot`, state stores `chatbot-eventstore` + derived `chatbot-statestore`, topic `chatbot.events`, deadletter `deadletter.chatbot.events` (kebab-case convention-derived).
- .NET Aspire 13.3.x AppHost (K8s/AKS + Helm deploy in 13.3 — relevant to M2 ops).
- Blazor + Fluent UI v5 (RC, via Hexalith.FrontComposer — Roslyn source-gen, Fluxor, REST commands/queries + SignalR projection-nudge, contract-first annotations). ⚠️ Fluent UI v5 still RC — inherited pre-GA, pinned.
- CLI: System.CommandLine 2.0.x wrapping `Hexalith.ChatBot.Client` **[M1]**.
- MCP: ModelContextProtocol 1.3.x (`.Core` + `.AspNetCore`); tools translate to commands/queries, tenant-aware **[M1]**.
- AI context / vector store: Hexalith.Memories (Redis Vector / FalkorDB) for scoped AI context + vector indexes **[M2, NFR9a isolation]**.
- Testing: xUnit v3 3.2.x, Shouldly, NSubstitute, Testcontainers; three-tier (unit / DAPR integration / Aspire E2E); conformance + isolation + idempotency as release gates; Playwright + axe-core for UI E2E.

**Core architectural decisions (D1–D7) that bound stories:**

- **D1 — Sibling integration:** event-driven + DAPR Workflow saga. Writes to siblings (Projects/Parties/Folders/Conversations) go through *their* EventStore commands via ChatBot-owned adapter ports; ChatBot maintains derived state from their published events; multi-step cross-context ops are DAPR Workflow sagas with compensation. ChatBot stays an orchestrator, never a source of truth (avoid the "distended orchestrator").
- **D2 — M0 association model:** deterministic candidate generation + evidence + human confirm/correct (deterministic-only scorer in M0; learned signals in M1).
- **D3 — FR81a placement:** a `CommandGateway` admission layer in `Server` running `auth → tenant-bind → authorize → risk-classify → approval-gate → coarse-idempotency → pre-commit-audit`, then dispatching into EventStore's existing write path (`fine-idempotency → execute → publish → projection`) + post-commit audit. **NOT a second pipeline.** Governance interfaces (`IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore`) are `internal` to `.Server` (stage-replication = compile error).
- **D4 — Audit model:** two-phase — *pre-commit* fail-closed gate (intent/risk/approval, resolves NFR15a) + *post-commit* WORM hash-chain (NFR49a) that is fail-open-then-reconcile-from-event-log (resolves the NFR15a × NFR49a tension). Completeness = reconstructability, verified by a scheduled production assertion.
- **D5 — Internal decomposition:** modular monolith, one deployable service, hard event-mediated seams by derived-state lifecycle: **Association / Governance(mediation+approval) / Lifecycle(workflow) / Projections / Audit**. Cross-seam communication events-only; separate assemblies; extraction-ready. Seam test: *owns an aggregate with its own invariants, or just a folder?*
- **D6 — Derived-store modeling:** immutable decision snapshots (candidate rankings, evidence snapshots, AI proposals, approval records, policy snapshots — append-only, superseded-not-mutated; FR91a = supersede + re-evaluate-forward) vs live mirrors (membership/ACL/sibling-lifecycle — event-driven, version-stamped, order-tolerant, last-writer-wins by source version). **Rule: mirrors for display, live authorization for gates.**
- **D7 — Contract surface:** OpenAPI 3.1 Contract Spine, contract-first; RFC 9457 metadata-only problem responses.

**Cross-cutting architectural constraints:**

- Tenant isolation by construction at every layer (command, query, store, cache, vector index, projection, log, error body, pagination cursors); `tenantId` from Keycloak claims only, never request body; M0 single-tenant but tenant-partitioned by construction so M1's second tenant is additive.
- Fail-closed invariant enforced at one injectable audit-commit seam every state-writing path calls before persisting; only pre-commit paths fail closed.
- Idempotency at two altitudes: coarse request-dedup at the gateway, fine event-dedup at the aggregate — never conflate.
- Derived-state versioning & deterministic replay: event upcasting for evolving AI-proposal/projection shapes; projection schema version stamped in replay traces; *as-of* upstream resolution on rebuild (never re-query *current* Party/Folder data); consumer-driven (Pact-style) contract tests against the 7 sibling contexts.
- Evidence & confidence capture is a first-class invariant on every proposal/candidate: `confidenceScore` [0,1], `thresholdBand`, `evidenceRefs[]`, `kernelVersion`, `detectedAt`, post-action `correctionOutcome`.
- WORM-vs-GDPR-erasure resolved via crypto-shredding / redaction-by-key-destruction / projection tombstones over an immutable chain (redaction key in a separate KMS).
- Every derived record stamped with `tenantId`, `sourceProvenance`, `derivationKernelVersion`, `redactionState`, `retentionClass`, `schemaVersion`.

**Mechanical enforcement (tests are part of every change, not review-by-eyeball):**

- **NetArchTest:** no `*.Cli`/`*.Mcp`/`*.UI` type references governance interfaces; dependency-direction edges (Contracts ← Client ← Server; CLI/MCP/UI depend only on Client); aggregates/projections only in `.Server`.
- **Conformance tests:** real-aggregate vs in-memory event-sequence equality.
- **Differential-conformance harness:** same semantic intent across UI/CLI/MCP → identical event sequence + state-store end-state (incl. rejection + retry intents); exercised from **M0 via thin CLI/MCP test shims** so M1 parity debt surfaces early.
- **Cross-tenant isolation:** zero-leak negative tests across **9 actor types** (human user, tenant admin, project admin/owner, service client, CLI client, MCP client, background worker, M365 event, AI actor) incl. cursors + error bodies.
- **Tier 2/3 tests inspect state-store end-state**, never just HTTP/exit codes.

**Architecture implementation sequence (respects M0 → M1 → M2):** (1) module scaffold + EventStore submodule + Aspire AppHost; (2) Contract Spine skeleton + typed Client + `IChatBotCommand`; (3) CommandGateway with all 9 stage seams (risk/approval stubbed; tenant-partition, fail-closed gate, pre-commit audit, idempotency real) + NetArchTest + differential-conformance harness; (4) Association module (deterministic scorer, candidate generation, lifecycle) + S2 review UI; (5) WORM audit store + post-commit reconcile + completeness assertion; (6) governed AI mediation (classifier, proposal, approval gate, one allowlisted command) + S1/S3 UI; (7) event-driven projections/mirrors + correction propagation (Workflow + aggregate lifecycle).

**ADRs to author (own before the dependent work lands):** idempotency, schema-evolution/upcasting, audit-two-phase, gateway, saga; **WORM audit backing technology** (before M0 post-commit audit store); **audit↔execute transactionality spike** (before M0 closes); M365/Graph intake specifics (subscription model, least-privilege scopes, webhook/replay — pending A1 pilot grant); M1 detail (outbound sender-authority enforcement, Keycloak service-account flows, tenant policy schema editor S5, full differential harness); M2 detail (vector store-layer isolation, replay test-tenant mechanics, dashboards, SLO calibration + continuity drill).

**Open assumptions that gate scope (PRD §Open Assumptions A1–A11):** A1 (M365 first mailbox), A2 (one mailbox pattern first), A3 (CLI/MCP parity proof may start with associate/status/audit), A5 (AI provider telemetry/retention/region config before live AI), A6 (audit retention vs GDPR), A8 (fixed command allowlist sufficient for MVP), A9/A9a (evaluation dataset — ≥500 labeled by M0, ≥2000 by M1; A9a gate directional at M0, binding+CI-aware at M1), A10 (RPO/RTO pending M2 drill), A11 (pilot thresholds calibrate after baseline window). Open questions in the PRD must not be silently assumed closed by implementation.

### UX Design Requirements

> Extracted from `DESIGN.md` (visual identity) and `EXPERIENCE.md` (behavioral spec). Visual chain: Fluent UI v5 → Hexalith.FrontComposer → DESIGN.md → EXPERIENCE.md. Each UX-DR is scoped to be story-able with testable acceptance criteria. Surface increment scoping follows PRD §UI Surface Inventory (S1–S10) and NFR60.

**Design system & visual foundation**

- UX-DR1: Inherit the Fluent UI v5 → FrontComposer → DESIGN.md visual chain; **do not invent a custom chatbot design system**. The product reads as a quiet operational SaaS command workspace, not a playful assistant/marketing chatbot/consumer messaging app.
- UX-DR2: Implement the semantic color system using Fluent UI v5 tokens (via FrontComposer CSS custom properties) with consistent meaning across ALL surfaces incl. CLI doc snippets and MCP tool descriptions: **Neutral** (default workspace/panes/queues/audit), **Brand** (primary actions + selected nav only), **Information** (evidence, context, candidate rationale, non-terminal status), **Warning** (ambiguity, approval-required, stale evidence, degraded dependency, manual review), **Danger/Error** (blocked, unauthorized, failed, quarantined, rejected, terminal), **Success** (completed association, approved action, stored attachment, command success, completed projection). Do not vary meaning by surface.
- UX-DR3: Apply the design tokens from DESIGN.md — spacing scale (4/8/12/16/24 px; density-compact 8, comfortable 12, panel-gap 16, row-gap 8), radius (sm 4 / md 8 / lg 12), typography ramp (page-title, section-title, body, metadata, monospace for IDs/command names/state names/correlation IDs). Avoid oversized hero type in authenticated surfaces.
- UX-DR4: Meet contrast requirements — WCAG 2.2 AA 4.5:1 normal text / 3:1 non-text UI, in both light and dark themes; under Windows High Contrast / forced-colors, status meaning for evidence/risk/danger/success chips and banners must survive via icon, text label, or border — not background fill alone; satisfy WCAG 2.2 Focus Appearance for focus rings. Product wrappers must not override token pairs with raw CSS unless re-tested to the same ratios.

**Information architecture — 9 surfaces** (build from IA/component/state/interaction tables; spine wins over future mockups)

- UX-DR5: **Project Workspace** (reached from app open / project switcher / deep link) — project-centered conversation, context, files, AI interaction, current work state. States: cold load, no project selected (picker/recents, no marketing hero), empty project conversation, active conversation, dependency degraded, unauthorized/redacted, project-switch success. *(PRD S1 region; M0.)*
- UX-DR6: **Conversation Detail** (S1) — multi-actor conversation stream (messages, parties, attachments, task intent, AI proposals, approvals, outcomes). System decisions labelled as system decisions, never anonymous chat. States incl. loading history, empty, streaming/update pending, attachment scan pending, AI proposal ready, command-accepted/projection-pending, correction applied, retryable failure, terminal failure, unauthorized/redacted. **[M0]**
- UX-DR7: **Association Review** (S2) — resolve ambiguous/failed email-to-project association with candidate evidence; no auto-attach when ambiguous. States incl. candidate loading, no authorized candidates, ambiguous candidates, candidate selected, validation error, confirm/reject/defer/escalate success, unauthorized candidate suppressed, retryable intake failure, quarantined/terminal failure. **[M0]**
- UX-DR8: **AI Action Review** (S3) — approve/reject/revise/cancel proposed risky AI actions before execution. States incl. proposal loading, ready, missing context, approval blocked by permission, decision successes, policy denied, execution pending/success, retryable/terminal execution failure. **[M0]**
- UX-DR9: **Files and Context** — governed folders, stored attachments, memory/index status, AI-context eligibility. States incl. folder/context loading, no files, file selected, upload/intake pending, scan pending, duplicate suppressed, memory/index pending, AI-context eligible, unauthorized/redacted file, storage retryable/terminal failure. **[M0]**
- UX-DR10: **Operational Queues** (S8/S10) — ambiguous associations, unresolved parties, pending approvals, failed ingestion, retryable work, quarantine; filter/sort/prioritize; no infinite scroll (pagination/virtualized, stable filters). States incl. queue loading, empty filtered, row selected, stale filters, retry queued, batch validation error, dependency degraded, unauthorized row redacted, terminal item, completed removed/archived. **[M1 queues / M2 dashboards]**
- UX-DR11: **Audit Investigation** (S9) — reconstruct association decisions, approval history, command execution, correction, retry, AI outcomes; search by message ID / correlation ID / project / requester / surface / actor / time / policy reason; redacted rows explain restriction + offer escalation without revealing hidden resource. **[M1/M2]**
- UX-DR12: **Tenant Configuration** (S5) — mailbox patterns, party resolution rules, confidence thresholds, approval policies, service clients, notifications; validation summary before fields; save-conflict explains policy/permission/stale-data cause; two-person-rule confirmation for security-sensitive knobs. **[M1]**
- UX-DR13: **Command Surface Reference** — explain UI/CLI/MCP parity, stable command names, status codes, reason codes, audit attribution; not a bypass affordance. **[M1]**

**Reusable components — behavioral specs (17)** (visual specs in DESIGN.md.Components)

- UX-DR14: **Project context header** — always shows authorized project identity, tenant context when relevant, current conversation/state, safe status.
- UX-DR15: **Conversation shell** — owns the two-part project-context ↔ active-conversation relationship; keeps workflow state visible while panels/evidence/approvals open.
- UX-DR16: **Conversation stream** — orders human/external-party/mailbox/AI/CLI-MCP/background/trigger/system events with actor attribution; system decisions not hidden as chat; chronological, grouped by day/source thread with accessible headings.
- UX-DR17: **Composer/action entry** — supports user messages and AI requests; a request implying risky action creates a proposal instead of executing.
- UX-DR18: **Actor badge** — distinguishes all **8 actor categories** (human user, external party, service client, AI actor, background worker, CLI, MCP, mailbox event) by accessible label + icon (not color); unresolved actors show unresolved state + safe actions; actor-type label precedes content in the accessible name.
- UX-DR19: **Evidence chip** — summarizes one evidence reason (project alias, sender match, thread ID, attachment metadata, prior correction, mailbox rule) with text + semantic status; click/keyboard opens supporting evidence when permitted; never color-only.
- UX-DR20: **Risk chip** — names the risk class in plain language (externally visible / file-exposing / project-mutating / tool-invoking / task-creating / participant-representing) and exposes the policy reason; never color-only.
- UX-DR21: **Attachment row** — storage status, scan status, folder link, duplicate/retry state, AI-context eligibility.
- UX-DR22: **Evidence drawer** — expands source evidence without forcing a full email-thread read; redacts inaccessible details; visually separate from the active decision (not a second conversation).
- UX-DR23: **AI proposal panel** — requester, project scope, input files, intended command, risk class, destination, policy reason, expected result; warning semantics until approved/rejected; programmatically related to source request/message.
- UX-DR24: **Approval panel + approval controls** — one review unit (proposed action + approve/reject/request-revision/cancel); remains pending until authorization path succeeds; disabled approval/association/correction controls remain focusable with `aria-disabled="true"` + announced reason OR an adjacent focusable "Why unavailable?" affordance (tooltip-only/default-non-focusable is insufficient); primary approval style only when all preconditions satisfied.
- UX-DR25: **Association candidate row** — candidate project, confidence band, evidence chips, unavailable/unauthorized suppression, actions (confirm, reject all, defer, escalate/manual review).
- UX-DR26: **Queue row** — state, age, risk, confidence, assignee, next required action, retry count, terminal/non-terminal status.
- UX-DR27: **Audit timeline** — chronological filterable reconstruction exposing event type, actor, timestamp, correlation ID, command surface, policy snapshot, outcome, links to permitted source evidence.
- UX-DR28: **Blocked state** — explains denial/unresolved association/quarantine/failed dependency/unsafe context with safe next action and redacted details; does not confirm resource existence.
- UX-DR29: **Status toast/banner** — transition feedback only; long-lived operational states live on the relevant surface, not in transient toasts.

**Voice, tone & microcopy**

- UX-DR30: Microcopy is factual, specific, and safe (e.g., "This message needs project review." not "We found a possible project!"; "Association blocked. You do not have access to this project." not "Project exists but permission denied."). Error/denial language must never reveal unauthorized project names, file metadata, candidate evidence, or sensitive audit details. Partial completion must say so ("Audit projection is pending. Command accepted." not "Done.").

**Interaction model**

- UX-DR31: Support primary interactions — select (project/conversation/queue item/candidate/file/approval/audit event); expand evidence inline or in side panel; confirm/reject/defer/correct/retry/quarantine/approve/request-revision/cancel/escalate; ask AI for help (risky → proposal); filter queues/audit by state/age/risk/confidence/project/mailbox/actor/reason/correlation/time; command palette/search where FrontComposer supports it.
- UX-DR32: Provide a Stop/Cancel control for streaming AI responses / proposal generation — always keyboard-reachable while streaming, in a stable focusable position (no inline appear/disappear that steals focus), announces "Response stopped" politely on activation, returns focus to composer or AI proposal panel.
- UX-DR33: Enforce banned/constrained interactions — no hidden auto-association when confidence is ambiguous; no AI execution of risky actions from a plain message send; no hover-only critical actions; no modal stacks beyond one active dialog/sheet; no infinite scroll for operational queues; no UI affordance suggesting CLI/MCP/admin authorization bypass.
- UX-DR34: Conform to WCAG 2.1.4 Character Key Shortcuts — single-character/modifier-free shortcuts disabled by default inside text-entry controls (composer, search, filters, config forms); globally remappable/disable-able from a "Keyboard shortcuts" preferences entry.

**State, feedback & live regions**

- UX-DR35: Implement the state-to-feedback matrix — skeletons match final layout with `aria-busy="true"` (cleared on swap-in; focus preserved/relocated to a labelled landing point; newly loaded history does not announce); one polite announcement for the current user's AI-proposal-ready / command-accepted (not repeated on poll or view re-entry); assertive announcement + reachable inline reason for the current user's rejected approval; observed-in-queue events for others get row-level inline status only (no live announcement); validation errors show an error summary before the panel with field-level `aria-invalid` + `aria-describedby`, focus to summary; dependency-degraded uses a scoped banner on the affected surface (not a global alarm unless the whole tenant/app is impacted); background updates while reading history use a non-interrupting "new updates" affordance (no forced scroll).

**Accessibility floor**

- UX-DR36: WCAG 2.2 AA for core UI workflows (conversation, association review, AI approval, queues, audit, tenant configuration); all action controls expose role/label/state/disabled-reason/keyboard operation; focus order follows visible reading/action order; status updates use appropriate live-region behavior without noisy repetition; evidence/risk chips carry text labels (not color alone); queue filtering, candidate selection, approval actions, and audit timeline navigation fully keyboard-operable; redacted/unauthorized states remain understandable to screen-reader users without leaking hidden content.
- UX-DR37: Keyboard & focus model — keyboard operation required for all workflows (advanced shortcuts are optional enhancements, not the only path); landmarks for nav / project context / main conversation-detail / complementary evidence-review panel / queue filters / status region, with **unique `aria-label`** when a role repeats (e.g., Evidence drawer + AI proposal panel both `complementary`); initial focus to surface heading or first actionable review item; dialogs/sheets trap focus and return it to the invoking control; Escape closes the topmost non-destructive popover/sheet/dialog and must not discard unsaved edits without explicit confirmation; submissions move focus to success status or error summary, rejected/blocked keep focus in the review panel with reason reachable.
- UX-DR38: Reduced motion — for `prefers-reduced-motion`, suppress shimmer skeletons, row movement animation, streaming-text animation, non-essential panel transitions; queue row insertion/reordering preserves focus and selection (status text, not movement, as the cue); progress uses non-motion text ("Scanning attachment", "Projection pending").
- UX-DR39: Off-surface affordances (export, copy-to-clipboard, download-transcript, "read aloud") apply the same redaction as the visual surface; exported artifact's accessible name/description contain no redacted source text; surface exposes a screen-reader-equivalent message that the export is redacted and full detail requires escalation.
- UX-DR40: Error-recovery patterns per flow — Association review (error summary names safe failure category, preserves candidate selection when valid, focuses summary, offers only still-valid actions); AI action review (risky actions require explicit confirmation copy; rejection/revision/cancel remain audit-visible); Queue retry (states duplicate-safety + retry count; failed retry returns focus to row status with next safe action visible); Correction (rationale where policy demands, previews affected attachments/derived AI context, reports success/partial/blocked without leaking unauthorized detail); Tenant configuration (validation summary before fields, field-level errors near controls, save conflicts explain policy/permission/stale cause).
- UX-DR41: Cognitive-load guardrails — one primary next action per workflow item (secondary/destructive grouped after); evidence/risk/status/actor/timestamp in consistent order across candidate rows, proposals, queues, audit; plain-language summaries precede raw IDs (IDs in metadata/expandable detail); filters show active-filter summary + result count; prefer one consolidated banner/panel per surface state over stacked alerts; dense tables reflow to labelled rows on small screens without dropping label/state/reason/safe-action.

**Responsive & platform**

- UX-DR42: Responsive web through Blazor/FrontComposer (not native mobile); CLI and MCP are separate command surfaces with equivalent backend state transitions, not visual breakpoints. Desktop/laptop = primary full-workflow surface (persistent nav + list/queue + detail + side panel coexist); tablet = nav collapses, conversation/detail may stack, association/approval stay complete; phone = reading, approve/defer/reject/confirm, status lookup, simple AI request.
- UX-DR43: Small-screen fallback — when a workflow is too dense for phone, keep read-only summary, status, safe approve/reject/defer/confirm, copy/share handoff link, and "open on larger screen" guidance; disable dense editing/admin-only controls with reachable explanation (no tooltip-only dependency); preserve draft/filter state when routing to a larger screen; screen-reader users hear the same limitation, remaining actions, and recovery path.
- UX-DR44: Touch targets — phone/tablet approval/association/filters/attachment/timeline/search/drawer-close/destructive controls use ≥ 44×44 CSS px where layout allows; compact dense-row controls meet WCAG 2.2 AA target size (≥ 24×24 CSS px or equivalent spacing); destructive and approval controls must not rely on compact-only sizing on phone/tablet; collapsed dense rows retain visible labels for project/actor/risk/state/confidence/time/next-action.

**Localization**

- UX-DR45: Support **English and French** UI (stakeholder discovery is French, project config outputs English); stable machine codes / status codes / reason codes / command names / correlation IDs remain untranslated; display labels and explanations are translated; dates/times/numbers/confidence bands/pluralization/actor labels use locale-aware formatting; avoid concatenated strings for accessible names/state descriptions; allow text expansion for French without truncating critical state/action words (collapse-first columns: raw IDs, secondary timestamps, low-priority metadata, repeated project/tenant context already in the header; must-keep/move-to-detail: actor, risk, state, confidence, next action, safe recovery reason).

**Key user flows** (drive end-to-end acceptance scenarios; map to PRD journeys)

- UX-DR46: Implement and test the 9 key flows from EXPERIENCE.md §Key Flows: (1) contributor asks AI for help → proposal → approve → executed outcome in conversation; (2) ambiguous association resolution (confirm/reject/defer/escalate, audited, no silent attach); (3) external party sends project context via ordinary email (ingest → party resolve → authz/association before exposure → fail closed on unresolved/unauthorized); (4) project owner repairs a wrong association (preserve original audit, relink/block attachments, invalidate derived AI context); (5) tenant admin configures governed collaboration (mailbox/party/thresholds/policy/audit visibility; degraded-mailbox safe recovery); (6) developer uses CLI parity (same candidates/evidence/status/redaction; partial-success on delayed projection; fail closed on stale creds/tenant switch); (7) compliance/support investigates a risky action (search + reconstruct; redacted rows offer escalation; read/escalate-only authority); (8) user reviews an AI action before boundary crossing (pause before send/exposure/mutation/tool/representation; approve/reject/revise/cancel; audited); (9) governed AI execution (scoped context; low-risk allowed, risky → proposal; refuse/clarify/route on unresolved association/missing context/authz failure; outcome through the same command/event/audit model). Inspiration: Claude Code / Codex (conversation-as-work-surface) and ChatGPT (familiar entry).

### FR Coverage Map

Every FR (and sub-FR) maps to exactly one primary epic below. Cross-cutting FRs note where an M1/M2 extension lives. NFRs are cross-cutting quality bars applied within the epics they constrain (security/isolation across all; reliability/idempotency in E1–E2; accessibility in E2–E3, E7, E8; audit/recovery in E1+E9; performance/observability in E8). Additional (Architecture) requirements seed E1 (scaffold + spine) and constrain all epics. UX-DRs map to surface stories: Project Workspace/S1/Files (UX-DR5, UX-DR6, UX-DR9) → E3; S2 (UX-DR7) → E2; S3 (UX-DR8) → E4; CLI/MCP/Command Surface Reference (UX-DR13) → E5; S5 (UX-DR12) → E7; Operational Queues (UX-DR10) → E7/E8; S8/S10 → E8; Audit Investigation S9 (UX-DR11) → E9. Cross-cutting UX-DRs are anchored in **Story 1.11** (design system, tokens, contrast, shared components, interaction primitives, responsive/touch — UX-DR1–4, 14–20, 28, 29, 31–34, 42–44) and **Story 1.12** (accessibility floor, live regions, reduced motion, redaction-safe export, localization EN+FR, error-recovery, cognitive-load — UX-DR35–41, 45), and inherited by every later surface story. The 9 key flows (UX-DR46) are realized end-to-end across E2–E4 (Flows 1–4), E5 (Flow 6), E6/E4 (Flow 8), E7 (Flow 5), and E9 (Flow 7), with the governed-AI-execution flow (Flow 9) spanning E4. Voice/tone microcopy (UX-DR30) is anchored in Story 1.7 (message catalog) and applied across surfaces. All 46 UX-DRs are covered by at least one story.

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
- FR74: Epic 7 — Disable/quarantine/rate-limit sources (15 subject×action sub-stories)
- FR75: Epic 7 — Per-tenant rate limits/quotas/circuit breakers
- FR75a–FR75g: Epic 7 — Tenant-admin permission model (bounded scopes, two-person rule, audit obligation)
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

## Epic List

9 epics across the 3 fixed increments (M0 → M1 → M2). Dependency flow is strictly forward — each epic is standalone and builds only on earlier epics; no epic requires a future epic. M0's four epics together deliver the complete vertical email-to-governed-action loop. The non-negotiable safety floor (tenant isolation, authorization, fail-closed, audit-of-the-command, idempotency, safe AI approval) is established in Epic 1 and inherited by all later epics; it is never trimmed.

### ▸ Increment M0 — Vertical Thesis Path (UI-only)

### Epic 1: Walking Skeleton & Governed Command Spine
Stand up a deployable `Hexalith.ChatBot` module where every state-mutating operation flows through one authenticated, tenant-isolated, fail-closed, audited command gateway — provable end-to-end through a single trivial governed command in the UI. This is the architecture-mandated walking skeleton: minimal surface, complete safety-floor spine, real from day one (tenant partitioning, fail-closed gate, pre-commit + post-commit audit emission, two-altitude idempotency, canonical lifecycle state model, the versioned user-safe message catalog, redaction stage, and mechanical parity/isolation enforcement). Includes the module scaffold (sibling-module template, EventStore root submodule, Aspire/DAPR topology, OpenAPI Contract Spine + typed Client + `IChatBotCommand`) as the first story.
**FRs covered:** FR16, FR55, FR57, FR59, FR61, FR68, FR77, FR80, FR81, FR81a, FR85, FR86, FR87, FR88, FR89, FR90, FR92, FR93.

### Epic 2: Email Intake & Project Association
Let an authorized user receive external project email and get it to the right project: auto-associated when deterministic evidence is strong, or reviewed against ranked candidates with visible evidence/confidence/reason codes when ambiguous — with confirm / reject-all / defer / needs-review / correct decisions, full lifecycle states, duplicate and retry safety, party resolution, source-evidence preservation, and correction propagation that invalidates derived context.
**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR7, FR8, FR9, FR10, FR11, FR12, FR13, FR14, FR15, FR17, FR60, FR62, FR63, FR64, FR65, FR66, FR71, FR76, FR79, FR91, FR91a, FR96.

### Epic 3: Project Conversation Context, Files & Attachments
Turn the associated email into usable, governed project context: render it as a project conversation (messages, parties, decisions, approvals, failures, AI outcomes), distinguish informational vs actionable items and AI summaries vs source evidence, expose a "why this project" provenance panel and human-review history, and capture attachments into governed folders with status, quarantine, and scoped AI-context eligibility.
**FRs covered:** FR21, FR22, FR23, FR24, FR25, FR26, FR27, FR28, FR29, FR30, FR31, FR32, FR33, FR34.

### Epic 4: Governed AI Action Mediation
Let a user ask AI for help on project work safely: detect task/action intent, classify risk via the tag+heuristic classifier, allow low-risk read-only assistance per policy, pause the six risky action classes for human approval with a full action preview, execute only the M0 allowlisted command (`Project.AppendConversationMessage`), refuse out-of-bounds requests, and record every proposal/approval/denial/execution/outcome — completing the M0 loop.
**FRs covered:** FR35, FR36, FR37, FR38, FR39, FR40, FR41, FR42, FR43, FR44, FR45, FR46.

### ▸ Increment M1 — Cross-Surface Parity & Full Governance

### Epic 5: Cross-Surface Parity — CLI & MCP
Let developers and AI agents run the full governed workflow through CLI and MCP with identical authorization, state transitions, redaction semantics, and audit attribution as the UI — service-client identities via Keycloak service accounts, the CLI/MCP adapters as thin translation layers over the shared command pipeline, and the differential-conformance harness fully wired across surfaces (parity by construction, verified not enforced by tests).
**FRs covered:** FR19, FR82, FR83, FR84 (extends FR80, FR85, FR86 across surfaces).

### Epic 6: Outbound Communication & Inbound Authenticity
Let authorized users draft and send governed project email with preserved sender authority (five authority classes), approval before anything leaves the project boundary, and full approval-record retention; and carry inbound provider authenticity posture (DMARC/DKIM/SPF passthrough, header inspection, on-behalf-of disambiguation, external-sender flag) into association and risk decisions.
**FRs covered:** FR47, FR48, FR48a, FR48b, FR48c, FR48d, FR49, FR50.

### Epic 7: Tenant Administration & Governance Policy
Let tenant administrators configure governed collaboration through bounded, audited admin scopes with no bypass: mailbox patterns, party resolution, confidence thresholds, AI action policy, approval-policy flexibility (per role/project/action-type/recipient/risk-class), notification routing/escalation, service-client grants, rate limits/quotas/circuit breakers, the versioned command allowlist (v1, two-person rule), the full lifecycle `Skipped` state, and operational-queue management (claim/assign/filter/sort).
**FRs covered:** FR18, FR51, FR52, FR53, FR69, FR70, FR72, FR73, FR74, FR75, FR75a, FR75b, FR75c, FR75d, FR75e, FR75f, FR75g, FR78.

### ▸ Increment M2 — Operations, Recovery, Continuity

### Epic 8: Operational Dashboards & Observability
Make the system operable in production: operational dashboards for mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, and audit lag; published SLOs with error budgets and alert thresholds; and measurable operational outcome metrics across all operation classes via OpenTelemetry.
**FRs covered:** FR67 (full dashboards S8/S10), FR94.

### Epic 9: Tamper-Evident Audit, Compliance Investigation & Recovery
Make audit defensible and recovery provable: tamper-evident append-only WORM hash-chained audit with reconstructability as a production observable; safe compliance investigation (search + reconstruct with per-project redaction and escalation, read/escalate-only authority); isolated replay/simulation against a dedicated test tenant; tenant data retention/export/deletion; consent/lawful-basis metadata; derived-store cross-tenant isolation by construction; and recovery/continuity targets (RPO/RTO, projection rebuild).
**FRs covered:** FR20, FR54, FR55a, FR56, FR58, FR95, FR95a (extends FR92 evaluation datasets).

---

## Epic 1: Walking Skeleton & Governed Command Spine

Stand up a deployable `Hexalith.ChatBot` module where every state-mutating operation flows through one authenticated, tenant-isolated, fail-closed, audited command gateway — provable end-to-end through a single trivial governed command in the UI. The architecture-mandated walking skeleton: minimal surface, complete safety-floor spine, real from day one. Establishes the safety floor inherited by every later epic.

### Story 1.1: Scaffold the buildable Hexalith.ChatBot module

As a platform engineer,
I want the `Hexalith.ChatBot` module scaffolded from the canonical sibling-module template with the EventStore submodule and Aspire/DAPR topology,
So that the team has a deployable, convention-correct foundation that builds, runs, and is ready for the command spine.

**Acceptance Criteria:**

**Given** the canonical sibling-module template (Hexalith.Folders as structural reference)
**When** the module is scaffolded
**Then** the `.slnx` solution contains `Contracts`, `Client`, `Server`, `Aspire`, `AppHost`, `ServiceDefaults`, `Testing` projects with strict `Contracts ← Client ← Server` dependency direction
**And** a `tests/` project mirrors each source project (xUnit v3), plus dedicated `Architecture.Tests` and `Conformance.Tests` projects.

**Given** the root configuration requirements
**When** the repository is initialized
**Then** `global.json` pins SDK 10.0.300 (rollForward latestPatch), `Directory.Build.props` sets `net10.0` + nullable + warnings-as-errors, `Directory.Packages.props` enables central package management with no inline package versions
**And** `.editorconfig`, `nuget.config`, `.gitmodules`, and `.github/workflows/` (ci + semantic-release) are present.

**Given** the root-level submodule policy
**When** EventStore is added
**Then** it is a root-level git submodule initialized with `git submodule update --init` (never `--recursive`)
**And** the build resolves EventStore types.

**Given** the Aspire AppHost
**When** `aspire run` is invoked
**Then** the topology brings up ChatBot + DAPR sidecars (statestore, pubsub, deny-by-default `accesscontrol.yaml`) and required siblings/Keycloak with `WaitFor` healthy
**And** DAPR resource names follow convention: AppId `chatbot`, state stores `chatbot-eventstore` + `chatbot-statestore`, topic `chatbot.events`, deadletter `deadletter.chatbot.events`.

**Given** `dotnet build Hexalith.ChatBot.slnx`
**When** run
**Then** the build succeeds under warnings-as-errors with no inline package versions.

### Story 1.2: Establish the OpenAPI Contract Spine, typed Client, and `IChatBotCommand`

As an adapter developer,
I want a single OpenAPI 3.1 Contract Spine with a generated typed client and an `IChatBotCommand` marker,
So that UI/CLI/MCP adapters bind to one contract source and cross-surface parity is structural.

**Acceptance Criteria:**

**Given** the Contract Spine decision (D7)
**When** the spine is created
**Then** `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` is the single contract source
**And** the typed client in `Client/Generated/` is NSwag-generated from it and never hand-edited.

**Given** the client surface
**When** an adapter submits a command
**Then** it constructs only a typed `IChatBotCommand` and calls `IChatBotClient.SubmitAsync(...)`.

**Given** shared contract types
**When** defined
**Then** `LifecycleState`, `RiskClass`, `ActorType`, `ThresholdBand` enums and ULID-based identity helpers exist in `Contracts` (identifiers parse via `Ulid.TryParse`, never `Guid.TryParse`).

**Given** an operation failure
**When** a problem response is returned
**Then** it is RFC 9457 metadata-only: `{ category, code, message, correlationId, taskId?, retryable, clientAction, details.visibility }`.

**And** a Contracts naming-convention test verifies commands are imperative (no suffix), events past-tense (no suffix), and rejections are `{Target}{Reason}Rejection : IRejectionEvent` with structured payloads only (IDs/enums/counts, never localized text).

### Story 1.3: CommandGateway admission spine with tenant binding and authorization

As a security engineer,
I want every state-mutating command to pass through a CommandGateway that authenticates, tenant-binds, and authorizes before any aggregate load,
So that no surface can reach domain state without enforced tenant isolation and authorization.

**Acceptance Criteria:**

**Given** the FR81a invariant
**When** the gateway is built
**Then** it runs stages in order `auth → tenant-bind → authorize` (risk-classify and approval-gate stubbed in this story) before dispatching to EventStore's write path, in `Server/Gateway/Stages/`.

**Given** a command from any surface
**When** `tenantId` is resolved
**Then** it is bound from authenticated Keycloak claims only — never from request body/CLI/MCP arguments
**And** a command carrying a cross-tenant identifier is rejected even when the principal holds valid credentials in another tenant (FR16, NFR1).

**Given** an unauthorized request
**When** authorization fails
**Then** the gateway returns a redacted denial that does not confirm whether the target resource exists (NFR2)
**And** an authorization-failure audit record is produced.

**Given** the adapter boundary
**When** an adapter is implemented
**Then** it constructs only `IChatBotCommand` and cannot replicate any gateway stage, because `IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore` are `internal` to `.Server`.

**And** a Tier 1/2 test proves a cross-tenant command fails closed with zero state mutation and an audit record.

### Story 1.4: Fail-closed audit-commit seam with pre- and post-commit audit emission

As a compliance owner,
I want every durable state write to pass through one fail-closed audit-commit seam that emits a pre-commit gate and a post-commit envelope,
So that no state mutates without an audit trail and the system fails closed when audit is unavailable.

**Acceptance Criteria:**

**Given** the two-phase audit model (D4)
**When** a state-writing path executes
**Then** it calls the single injectable audit-commit seam: pre-commit audit (intent/risk/approval) is a fail-closed gate and post-commit audit emits a hash-chainable envelope (full WORM chain deferred to Epic 9/M2).

**Given** the audit envelope
**When** emitted
**Then** it carries `tenantId, actorId, actorType, commandName, resourceId, decision, reasonCode, correlationId, timestamp, policySnapshotId, sourceEvidenceRefs[], idempotencyKey?, stateTransition, redactionDecision, outcome` (FR55, FR61, NFR50).

**Given** the audit writer is down
**When** a state write is attempted
**Then** the operation returns typed `AuditUnavailable`, writes no durable state, queues the intent for replay, and emits an operator alert (NFR15a)
**And** there is no "audit unavailable → continue" branch on any path.

**Given** a new state-writing path is added later
**When** the fail-closed test (parametrized from the path enumeration the code uses) runs
**Then** any path that skips the seam fails the test by omission (FR68)
**And** replay resumes only when the audit writer is healthy.

### Story 1.5: Two-altitude idempotency

As a reliability engineer,
I want coarse request-dedup at the gateway and fine event-dedup at the aggregate,
So that duplicate or replayed commands never double-apply and the two altitudes are never conflated.

**Acceptance Criteria:**

**Given** at-least-once delivery
**When** the same request arrives twice
**Then** the gateway's coarse request-dedup returns the prior outcome without re-dispatching, and the aggregate's fine event-dedup (EventStore idempotency cache) suppresses re-application (NFR13).

**Given** a command is admitted
**When** its idempotency key is composed
**Then** it follows the per-operation-class composition in addendum §Idempotency Keys with canonical-form normalization (key ordering, whitespace, NFC) before hashing (FR90, NFR13a).

**Given** a conflicting duplicate (same key, different payload)
**When** detected
**Then** the operation-class conflict response is returned (e.g., "already decided"), never a silent overwrite.

**And** a Tier 2 test asserts the state-store end-state is identical after repeated equivalent inputs.

### Story 1.6: Canonical lifecycle state model and transition enforcement

As a workflow owner,
I want a canonical lifecycle state machine with validated transitions,
So that workflow items move only through legal states and invalid transitions are rejected and audited.

**Acceptance Criteria:**

**Given** the exact state vocabulary
**When** states are defined
**Then** `Received | Proposed | Associated | Rejected | Deferred | NeedsReview | Failed | Skipped | Corrected` (+ sub-states `Correcting | Correction-delayed`) exist as stable strings used verbatim across UI/CLI/MCP/audit (FR87).

**Given** an inbound or outbound transition
**When** attempted
**Then** it is validated against the explicit state model (FR88).

**Given** an invalid transition
**When** attempted
**Then** it is rejected before mutation and recorded with the rejected transition, actor, reason, and correlation context (FR89, NFR15).

**Given** a terminal state (`Rejected`/`Failed`/`Skipped`)
**When** reprocessed
**Then** a new workflow instance with a new ID is created carrying `supersedes`/`superseded_by` audit links, and the original terminal record is preserved unchanged.

**And** health/status enums (`healthy`/`degraded`/`failed`/`unknown`) are stable strings, never derived from counts.

### Story 1.7: Versioned user-safe message catalog and redaction stage

As a UX and security owner,
I want a versioned message catalog and a swappable redaction stage,
So that every user-facing failure is safe, catalogued, and never leaks restricted detail.

**Acceptance Criteria:**

**Given** the message catalog (FR77)
**When** a refusal / blocked / degraded / failed / denied state surfaces
**Then** the message is drawn from `Contracts/Messages/` with a stable code, a user-safe headline ≤ 80 characters, a one-sentence reason naming no unauthorized project/file/party/audit detail (NFR2), and a safe next-action affordance.

**Given** any user-facing surface
**When** it renders
**Then** no raw exception text leaks; production telemetry counts uncategorized states and the count must be 0 per release (NFR40) — any nonzero count blocks release.

**Given** the redaction stage
**When** responses are produced across UI/CLI/MCP/export
**Then** redaction is applied consistently by a swappable policy stage that is trim-safe to a coarse default (FR57).

**And** disabled-action reasons are drawn from the finite set `insufficient-authority` / `state-not-permitted` / `dependency-degraded` / `awaiting-other-actor` / `policy-blocked` (FR76 support).

### Story 1.8: Correlation propagation and long-running operation status

As an operator,
I want correlation context on everything and a status query for long-running operations,
So that any action is traceable end-to-end and partial/eventual states are visible rather than falsely reported complete.

**Acceptance Criteria:**

**Given** any command, event, log, or OpenTelemetry activity
**When** produced
**Then** it carries `correlationId` propagated across mailbox intake → association → file handling → approval → AI mediation → command execution → audit → UI (FR59), and logs/traces are metadata-only (no payloads/PII/secrets).

**Given** a long-running operation
**When** its status is queried
**Then** the response includes operation identity, current state, retry count, partial outputs, safe next actions, terminal reason, and correlation context (FR80).

**Given** a command accepted but with a projection still pending
**When** the UI renders
**Then** it shows a partial-success state with operation identity and audit/projection status — never a false "Done".

**And** user-facing UTC timestamps convert to tenant-local only at the presentation boundary (NFR36).

### Story 1.9: First governed command end-to-end with surface-origin attribution

As a product owner,
I want one trivial allowlisted command to execute end-to-end through the full gateway from the UI, with surface origin attributed,
So that the walking skeleton is provably complete before feature work begins.

**Acceptance Criteria:**

**Given** a minimal UI core-operations shell (FR81)
**When** a user submits the one trivial governed command
**Then** it flows `auth → tenant-bind → authorize → coarse-idempotency → pre-commit-audit → [EventStore: execute → publish → projection] → post-commit-audit`, and the outcome is visible in the UI with audit history.

**Given** the adapter boundary
**When** the command is constructed
**Then** its origin (`UI/API`) is attached at the boundary, travels with the Command record into the audit envelope, and cannot be mutated by downstream pipeline stages (FR85).

**Given** the M0 surface scope
**When** the command executes
**Then** tenant partitioning, fail-closed behavior, and audit/idempotency are real (not stubbed), even though risk-classify and approval-gate remain stubbed.

**And** a Tier 3 Aspire E2E test exercises the command end-to-end and inspects state-store end-state (not just an HTTP 202).

### Story 1.10: Mechanical parity and isolation enforcement harnesses

As an architecture owner,
I want NetArchTest, a differential-conformance harness, and a cross-tenant isolation harness wired from day one,
So that parity-by-construction and zero cross-tenant leakage are enforced mechanically, not by review.

**Acceptance Criteria:**

**Given** the dependency rules
**When** NetArchTest runs
**Then** no `*.Cli`/`*.Mcp`/`*.UI` type references `IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore`, dependency-direction edges hold (`Contracts ← Client ← Server`; CLI/MCP/UI → Client only), and aggregates/projections exist only in `.Server`.

**Given** the differential-conformance harness
**When** the same semantic intent is submitted through UI and thin CLI/MCP test shims
**Then** identical event sequence + state-store end-state result, including rejection and retry intents (FR86).

**Given** the 9 actor types (human user, tenant admin, project admin/owner, service client, CLI client, MCP client, background worker, M365 event, AI actor)
**When** cross-tenant negative tests run
**Then** every actor fails closed with zero leakage across candidates, evidence, files, pagination cursors, and error bodies (NFR11, NFR67).

**Given** downstream calibration needs
**When** test infrastructure is provided
**Then** a tenant-scoped fixture/sandbox harness and an evaluation-dataset partition scaffold exist (FR92, FR93)
**And** Tier 2/3 tests inspect state-store end-state, never only HTTP/exit codes.

### Story 1.11: UX foundation — design system, shared components, and interaction primitives

As a frontend engineer,
I want the Fluent UI v5 / FrontComposer design system, semantic tokens, shared components, and interaction primitives established once,
So that every M0+ surface inherits a consistent, governed UX rather than reinventing it.

**Acceptance Criteria:**

**Given** the visual inheritance chain
**When** the UI foundation is built
**Then** it inherits Fluent UI v5 → FrontComposer → DESIGN.md with no custom design system, applying the semantic color system (neutral / brand / info / warning / danger / success) with consistent meaning across all surfaces (UX-DR1, UX-DR2).

**Given** design tokens
**When** components render
**Then** spacing/radius/typography tokens from DESIGN.md are used, and contrast meets WCAG 2.2 AA (4.5:1 text / 3:1 non-text) in light and dark, with status surviving forced-colors via icon/text/border, not fill (UX-DR3, UX-DR4).

**Given** the cross-cutting component library
**When** built
**Then** project context header, conversation shell, actor badge (8 actor types by label + icon, not color), evidence chip, risk chip, blocked state, and status toast/banner exist as reusable behavioral components (UX-DR14–UX-DR20, UX-DR28, UX-DR29); feature-specific components (candidate row, AI proposal/approval panels, queue row, audit timeline) are delivered in their feature stories.

**Given** interaction primitives
**When** implemented
**Then** banned interactions are enforced (no hidden auto-association, no AI risky execution from a plain send, no hover-only critical actions, no modal stacks beyond one, no infinite scroll for queues, no bypass affordance) (UX-DR33); a keyboard-reachable Stop/Cancel exists for streaming AI with a polite "Response stopped" announcement (UX-DR32); single-character/modifier-free shortcuts are disabled in text inputs and remappable (WCAG 2.1.4, UX-DR34).

**And** responsive behavior (desktop primary; tablet/phone triage), the small-screen fallback pattern, and touch-target sizes (≥ 44×44, dense ≥ 24×24) are established (UX-DR42–UX-DR44).

### Story 1.12: Accessibility floor and English/French localization

As an accessibility and localization owner,
I want a WCAG 2.2 AA behavioral floor and EN/FR localization established,
So that every governed surface is operable, understandable, and bilingual by inheritance.

**Acceptance Criteria:**

**Given** the accessibility floor
**When** any core surface is built
**Then** keyboard operation is required for all workflows; landmarks carry unique `aria-label`s when a role repeats; focus order follows visible order; dialogs trap and restore focus; disabled actions expose a reachable reason (`aria-disabled` + announced, not tooltip-only) (UX-DR36, UX-DR37).

**Given** live-region behavior
**When** state changes
**Then** the state-to-feedback matrix is applied: `aria-busy` on load; one polite announcement for the user's own proposal/command-accepted; assertive + reachable reason for the user's rejected approval; observed-for-others = inline status only; validation error summary with `aria-invalid`/`aria-describedby` (UX-DR35).

**Given** reduced motion and off-surface affordances
**When** applicable
**Then** `prefers-reduced-motion` suppresses non-essential animation (status text, not movement) (UX-DR38), and export/copy/download/read-aloud apply the same redaction as the visual surface with a screen-reader-equivalent redaction message (UX-DR39).

**Given** localization
**When** UI text renders
**Then** English and French are supported; machine codes/status/reason/command names/correlation IDs stay untranslated; dates/times/numbers/confidence bands/actor labels use locale-aware formatting; French text expansion is handled without truncating critical state/action words (UX-DR45).

**And** error-recovery patterns (association / AI-review / queue-retry / correction / tenant-config) and cognitive-load guardrails (one primary action, consistent evidence/risk/status/actor/time ordering, plain-language before IDs, active-filter summary) are applied across surfaces (UX-DR40, UX-DR41).

---

## Epic 2: Email Intake & Project Association

Let an authorized user receive external project email and get it to the right project — auto-associated when deterministic evidence is strong, or reviewed against ranked candidates with evidence when ambiguous — with confirm/reject/defer/correct decisions, full lifecycle, duplicate and retry safety, party resolution, evidence preservation, and correction propagation.

### Story 2.1: Microsoft 365 mailbox intake and source-identity capture

As a project team,
I want one controlled M365/Exchange mailbox pattern ingested idempotently with full source identity preserved,
So that project email becomes a governed, traceable, duplicate-safe collaboration input.

**Acceptance Criteria:**

**Given** one configured controlled mailbox pattern for a tenant
**When** a mailbox event arrives
**Then** the worker captures it as a project collaboration input (FR1) and preserves source email identity, internet message ID, conversation/thread identity, mailbox identity, sender, recipients, timestamps, and attachment references (FR2).

**Given** the message-intake operation class
**When** the same provider message is delivered twice
**Then** the `tenant_id + mailbox_id + provider_message_id` idempotency key suppresses the duplicate and audits the suppression (NFR13a), creating no duplicate intake record.

**Given** an intake where tenant scope is unresolved or the audit writer is down
**When** processing runs
**Then** the path fails closed per NFR15a (no durable state, intent queued), and the item is visibly recoverable (NFR17).

**And** message timestamps are stored as server-side UTC, preserving the source timestamp/timezone context (NFR36).

### Story 2.2: Participant resolution and unresolved/unauthorized handling

As a reviewer,
I want email senders and recipients resolved to tenant-scoped parties (and unresolved ones flagged),
So that external participants contribute by email while authorization is enforced before any context is exposed.

**Acceptance Criteria:**

**Given** an ingested message
**When** participants are resolved
**Then** internal and external participants are resolved to tenant-scoped parties through an `IParticipantDirectory` adapter over Hexalith.Parties (FR13), storing only stable `PartyId` references — never upstream PII.

**Given** a participant that cannot be resolved
**When** resolution completes
**Then** the participant is shown in an unresolved state with safe identity evidence and actions (link / create-pending / reject / quarantine) (FR14), and an external party may still contribute project context by email without portal access (FR15).

**Given** an unresolved or unauthorized actor
**When** it attempts to access files, create task requests, trigger commands, or send outbound communication
**Then** the action is blocked and fails closed (FR17), with a redacted reason that does not confirm resource existence (NFR2).

### Story 2.3: Deterministic association scorer and candidate generation

As the system,
I want a deterministic-signals scorer that produces a confidence score and ranked authorized candidates,
So that strong deterministic matches auto-associate and everything else gets evidence-backed candidates.

**Acceptance Criteria:**

**Given** the M0 deterministic signals (explicit project identifier, mailbox routing rule, conversation/thread identifier)
**When** a message is scored
**Then** the Association scorer produces a `[0.0,1.0]` confidence and ranked candidate projects filtered by tenant + authorization (unauthorized projects never appear as candidates) (FR3).

**Given** a score `≥ T_high` with required deterministic evidence present
**When** association runs
**Then** the message auto-associates to the single matched project; deterministic signals outrank any AI inference.

**Given** a tenant administrator
**When** they set `association.t-high` / `association.t-low`
**Then** the change is security-sensitive (tenant-admin auth, audit event, schema-bounded range, not by service clients/AI actors) (FR9, addendum §Confidence Thresholds).

**And** association reasons and confidence inputs are exposed in machine-readable form for UI/CLI/MCP/audit/test (FR11).

### Story 2.4: Ambiguous-association detection and fail-closed routing

As the system,
I want ambiguous or low-confidence messages routed to human review instead of being silently filed,
So that the workspace is never contaminated by an uncertain association.

**Acceptance Criteria:**

**Given** a score in `[T_low, T_high)`
**When** association runs
**Then** the message produces an ambiguous-association decision routed to UI review (`NeedsReview`/candidate list), never auto-attached (FR4).

**Given** a score `< T_low`, conflicting required signals, or a scorer error/non-finite value
**When** association runs
**Then** the message fails closed to `NeedsReview` (empty candidate list on scorer error, audited) and original email context is preserved (FR10, addendum §Confidence Thresholds).

**Given** an item in `Rejected`/`Deferred`/`NeedsReview`/`Failed`/`Skipped`
**When** the user views it
**Then** the original email context remains intact and inspectable (FR10).

### Story 2.5: Ambiguous association review surface (S2)

As an authorized reviewer,
I want a candidate-review surface with evidence, confidence, and clear decisions,
So that I can resolve ambiguity from captured evidence without re-reading the full thread.

**Acceptance Criteria:**

**Given** an ambiguous item
**When** I open Association Review (S2)
**Then** I see ranked candidate rows with evidence chips, confidence band, reason codes, and the consequence of each decision (FR5), and I can compare candidate evidence side by side (FR12).

**Given** a candidate item
**When** I act
**Then** I can choose a candidate, reject all, defer, mark needs-review, and add an optional decision note (FR6); each action affordance is `enabled` / `disabled-with-reason` / `not-applicable-hidden` with a finite reason set (FR76, UX-DR24/UX-DR25).

**Given** a long-running or blocked item
**When** rendered
**Then** stale / waiting / blocked / escalation-needed states are shown with next-action guidance (FR79), and the surface conforms to WCAG 2.2 AA (NFR60, UX-DR36).

**And** unauthorized candidate projects are suppressed from the list, evidence, and any error text (FR57, NFR2).

### Story 2.6: Association decision recording, evidence preservation, and notes

As a compliance owner,
I want every association decision recorded as an event with preserved evidence and optional human rationale,
So that decisions are reconstructable and explainable later.

**Acceptance Criteria:**

**Given** any association decision
**When** it is made
**Then** the decision, correction, rejection, deferral, retry, or skip is recorded as a domain event with actor, tenant, timestamp, signal/rule, and confidence state (FR8).

**Given** evidence used for a decision
**When** recorded
**Then** source evidence is preserved with retention boundaries and redaction behavior (FR60), carrying `confidenceScore`, `thresholdBand`, `evidenceRefs[]`, `kernelVersion`, `detectedAt` (architecture cross-cutting #12).

**Given** a reviewer
**When** resolving an item
**Then** they can add human notes or resolution rationale to association/participant/retry/quarantine/correction decisions (FR62).

### Story 2.7: Association correction and supersession

As a project owner,
I want to correct a wrong association while preserving the original decision in history,
So that contaminated context is repaired accountably without erasing the record.

**Acceptance Criteria:**

**Given** an existing association
**When** an authorized owner corrects it
**Then** the new association is recorded and the prior one is superseded (not mutated), preserving the original decision and downstream impact in audit history (FR7, FR63).

**Given** a corrector lacking project ownership or with the projection-invalidation queue down
**When** correction is attempted
**Then** the operation fails closed per NFR15a with a safe, redacted reason.

**Given** tenant policy permits it (M1)
**When** a correction is recorded
**Then** it becomes available as future association evidence only while remaining explainable and inspectable as to why it influenced a match (FR96).

### Story 2.8: Correction propagation contract

As a project owner,
I want a correction to invalidate and rebuild every derived store that used the wrong association,
So that AI never acts on stale, misassigned project context.

**Acceptance Criteria:**

**Given** a correction (FR7)
**When** it is recorded
**Then** every derived store referencing the original association (candidate ranking, evidence snapshot, AI-action proposals that consumed the misassigned context, queue projections; vector entries in M2) is invalidated and rebuilt; the aggregate owns the `correcting`/`current` lifecycle and a DAPR Workflow coordinates invalidation (FR91, FR91a).

**Given** an item in `Correcting`
**When** an AI action is requested against it
**Then** the action is blocked until all derived stores acknowledge invalidation; the item shows progress and estimated completion.

**Given** invalidation exceeds the SLO (p95 ≤ 10 min for M0/M1)
**When** propagation is still running
**Then** the item surfaces `Correction-delayed` with the responsible owner role and next safe action, and a P2 incident is raised (NFR17a).

**And** audit records the predecessor association, the correction, and the per-store invalidation outcome.

### Story 2.9: Duplicate detection, retry, and failure states

As a reviewer,
I want duplicate deliveries suppressed and failed work retried or surfaced clearly,
So that messy mailbox conditions never corrupt project state or hide work.

**Acceptance Criteria:**

**Given** duplicate mailbox delivery
**When** detected
**Then** no duplicate project messages, attachments, task intents, approvals, commands, notifications, or audit decisions are created (FR64, NFR14); duplicate suppression records retry metadata.

**Given** a failed mailbox/attachment/association/approval/command/projection operation where retry is valid
**When** retry runs
**Then** it is idempotent (no duplicate artifacts) and exposes retry count + duplicate-safety note (FR65).

**Given** a terminal or non-terminal failure
**When** it occurs
**Then** the item is surfaced in a visible, recoverable state with the next required human action (FR66, FR71), and terminal states follow the new-workflow-instance reprocess rule from Story 1.6.

---

## Epic 3: Project Conversation Context, Files & Attachments

Turn the associated email into usable, governed project context: render it as a project conversation with parties, decisions, approvals, AI outcomes; distinguish informational vs actionable and AI-summary vs source evidence; expose a "why this project" panel and review history; and capture attachments into governed folders with status and scoped AI-context eligibility.

### Story 3.1: Render email-derived project conversation (S1)

As an authorized contributor,
I want associated email rendered as a project conversation kept separate per tenant and project,
So that I work from project context without opening my mailbox.

**Acceptance Criteria:**

**Given** an associated message
**When** I open the Project Workspace / Conversation Detail (S1)
**Then** email-derived messages render as ordered project conversation context (FR21) within a conversation shell that keeps project context and workflow state visible (UX-DR6/UX-DR15/UX-DR16).

**Given** multiple tenants and projects
**When** conversations render
**Then** conversation context is kept strictly separate across tenants and projects (FR25); no cross-tenant content appears in content, cursors, or error bodies (NFR11).

**And** system decisions are labelled as system decisions in the stream, not shown as anonymous chat messages (UX-DR16).

### Story 3.2: Conversation item rendering across the seven concerns

As an authorized contributor,
I want associated email, participants, attachments, decisions, approvals, failures, and AI outcomes represented in project context,
So that the full project picture is visible in one place.

**Acceptance Criteria:** *(FR22 — the seven first-class concerns; each is acceptance-tested independently and may be implemented as seven sub-stories)*

**Given** a project conversation
**When** it renders
**Then** it represents (1) associated email, (2) participants, (3) attachments, (4) decisions, (5) approvals, (6) failures, and (7) AI outcomes, each with actor attribution and the actor-type label preceding content in the accessible name (FR22, UX-DR18).

**Given** each concern
**When** acceptance-tested
**Then** every concern renders independently on surface S1 with consistent evidence/risk/status/actor/timestamp ordering (UX-DR41).

**And** the rendering conforms to WCAG 2.2 AA, with non-color status and reduced-motion behavior (NFR60/NFR61, UX-DR38).

### Story 3.3: "Why this project" evidence and provenance panel

As an authorized contributor,
I want to inspect why an email belongs to a project,
So that I can trust the association and see corrections.

**Acceptance Criteria:**

**Given** an associated email
**When** I open the "why" panel
**Then** it displays the originating signal class (explicit identifier / mailbox routing rule / thread identifier / human selection / correction), the matched value, the confidence score, the threshold band (`auto`/`ambiguous`/`fail-closed`), the decision actor, the decision timestamp, and links to any superseding correction with its own evidence panel (FR23).

**Given** restricted evidence
**When** the panel renders for a user lacking authority
**Then** inaccessible details are redacted consistently and the evidence drawer remains understandable to screen-reader users without leaking hidden content (UX-DR22/UX-DR39).

### Story 3.4: Conversation item status and next action

As an authorized contributor,
I want to see association, attachment, task, approval, command, failure, retry, and next-action status,
So that I always know the state of a project conversation item.

**Acceptance Criteria:**

**Given** a project conversation item
**When** it renders
**Then** it shows association, attachment, task, approval, command, failure, retry, and next-action status (FR24) using stable status enums (`healthy`/`degraded`/`failed`/`unknown`), never count-derived.

**Given** a command accepted with projection pending
**When** rendered
**Then** it shows partial-success with operation identity and audit/projection status, not a false "Done" (UX-DR30/UX-DR35).

### Story 3.5: Informational/actionable classification, AI-summary distinction, and review history

As an authorized contributor,
I want actionable items flagged, AI summaries visibly distinct from source evidence, and review history visible,
So that I never confuse AI interpretation with original facts and can see what humans decided.

**Acceptance Criteria:**

**Given** any email in the conversation
**When** it renders
**Then** it carries a visible `informational`/`actionable` badge; actionable items surface detected intent (FR35) and the next-action affordance; classification is reproducible from the tag+heuristic kernel (FR26).

**Given** AI-generated content
**When** it renders
**Then** it is visually distinct (typographic treatment + `AI summary` label, non-color), preceded by a provenance string `Generated by <model+version> at <timestamp> from <source-evidence-IDs>`, collapsible to source evidence; source evidence is the default view (FR27, NFR64).

**And** visible human-review history is preserved for each email, attachment, approval, AI action, and command (FR28).

### Story 3.6: Attachment capture and governed-folder storage

As an authorized contributor,
I want attachments captured from associated email and stored in governed project folders,
So that files live under project governance, not in mailboxes.

**Acceptance Criteria:**

**Given** an associated email with attachments
**When** intake runs
**Then** attachments are captured (FR29) and stored into the project's governed Hexalith.Folders structure via an `IFolderStore` adapter (FR30), storing stable `FileId`/`FolderId` references.

**Given** duplicate attachment delivery
**When** capture runs
**Then** storage is idempotent and duplicates are suppressed (NFR14) without creating duplicate folder entries.

### Story 3.7: Attachment status, states, and authorization

As an authorized contributor,
I want attachment status and safe handling of unsafe files, with unauthorized access blocked,
So that files are governed and unsafe content cannot reach project or AI surfaces.

**Acceptance Criteria:**

**Given** a captured attachment
**When** I inspect it
**Then** I see capture and storage status (FR31) and one of the states captured / pending / unavailable / rejected / unsafe / failed / retryable (FR34).

**Given** unsafe-content/malware policy
**When** an attachment is scanned
**Then** size limits, type restrictions, scan status, and quarantine behavior are enforced before any project or AI exposure (NFR21), per the tenant `attachments.unsafe-handling` knob.

**Given** an unauthorized actor
**When** they attempt to view attachment metadata or content
**Then** access is prevented with a redacted denial (FR32, NFR2).

### Story 3.8: Scoped AI-context packaging from authorized files

As the system,
I want authorized project files exposed to AI only through an explicit, auditable context package,
So that AI never receives unauthorized or unscoped file content.

**Acceptance Criteria:**

**Given** an AI action needing file context
**When** the context package is built
**Then** files are included only through explicit authorization, policy checks, and auditable context packaging (FR33).

**Given** the context package
**When** assembled
**Then** it contains tenant ID, project ID, source evidence references, policy snapshot ID, redaction decision, retention class, and provider-reuse setting before model/tool invocation (NFR9).

**And** an attachment still pending scan or AI-context-ineligible is excluded from the package until policy permits (NFR21).

---

## Epic 4: Governed AI Action Mediation

Let a user ask AI for help on project work safely: detect intent, classify risk, allow low-risk read-only assistance per policy, pause risky actions for approval with a full preview, execute only the allowlisted command, refuse out-of-bounds requests, and record every outcome — completing the M0 loop.

### Story 4.1: Task-intent detection and data contract

As the system,
I want candidate task/action intent detected from authorized conversation actors with source evidence,
So that actionable requests are captured for governed review.

**Acceptance Criteria:**

**Given** an authorized project conversation actor
**When** a message implies a task or action
**Then** a task-intent record is captured preserving source message evidence (FR35) with `tenant_id`, `project_id`, `source_message_id`, `requester_party_id`, `detected_intent_summary` (≤280), `detected_action_kind` enum, `source_evidence_offsets`, `kernel_version`, `confidence_score` `[0,1]`, `detected_at`, and `state`.

**Given** the A9a evaluation dataset
**When** detection quality is measured
**Then** precision/recall meet ≥80%/≥75% by M0 release (ratcheting to ≥90%/≥85% by M1) (FR35, A9a).

### Story 4.2: Task-intent review, conversion, and disposition

As an authorized reviewer,
I want to review captured intent and either convert it to a governed action or close it,
So that only intended work proceeds.

**Acceptance Criteria:**

**Given** a captured task-intent record
**When** I review it
**Then** the surface displays the FR35 data contract plus the full source message and available transitions (FR36).

**Given** an actionable intent
**When** I convert it
**Then** conversion creates the FR41 proposal record linked to the source task-intent record and is itself an audited operation (FR37).

**Given** a non-actionable intent
**When** I disposition it
**Then** I can mark it not-actionable / duplicate / already-handled / out-of-scope as a terminal state; the record is preserved for A9a, and duplicate links the predecessor task-intent ID (FR38).

### Story 4.3: AI action risk classification

As a security owner,
I want AI action requests classified by risk with a fail-closed default,
So that risky work is never executed without approval.

**Acceptance Criteria:**

**Given** a proposed AI action
**When** classified
**Then** the tag+heuristic classifier reads the proposed command, its tenant-policy classification, the action's effect surface, and the requester's authority class, and outputs `low-risk` or `approval-required` (FR39); it has no AI-service dependency, so it survives AI outage (NFR22).

**Given** an indeterminate classification (missing tags / unknown effect surface / undeclared authority)
**When** classification completes
**Then** the action is treated as `approval-required` (fail-closed) (addendum §Risk Classifier).

**Given** a mixed request
**When** classified
**Then** it inherits the strictest applicable risk class.

**And** a reviewer disagreeing with the classification produces a reviewer-disagreement audit record feeding A9a calibration.

### Story 4.4: Low-risk AI assistance execution

As an authorized contributor,
I want low-risk read-only AI assistance to run when policy allows,
So that I get help without unnecessary approval friction.

**Acceptance Criteria:**

**Given** a `low-risk` action
**When** tenant policy `ai-action.low-risk-allowed` permits the class and the actor is authorized to the project
**Then** the assistance executes within scoped project context (FR40), using only the authorized context package (NFR8/NFR9).

**Given** `low-risk-allowed` is `false` for the class (the safe default)
**When** the action is requested
**Then** it is routed to approval instead of executing.

### Story 4.5: Approval gate and AI action approval surface (S3)

As an authorized approver,
I want risky AI actions paused for review with a complete preview,
So that nothing risky executes until I approve it.

**Acceptance Criteria:**

**Given** an action in any of the six risky classes (modifies-state / exposes-files / sends-external / creates-tasks / invokes-tools / acts-on-behalf)
**When** proposed
**Then** it requires approval before execution (FR41).

**Given** the AI Action Review surface (S3)
**When** I open a pending action
**Then** it displays the command name (current allowlist version), input files as tappable evidence references with redaction state, proposed recipients, sender-authority class, risk classification with the producing input tuple, the policy snapshot ID, the expected post-state, and decisions `approve`/`reject`/`request-revision`/`cancel` (FR42).

**Given** I lack authority for the action's risk class
**When** the surface renders
**Then** `approve` is disabled with a reason string, and the disabled control remains focusable with an announced reason (FR42, UX-DR24).

**And** the approval surface renders a per-evidence freshness chip and disables `approve` against `expired` evidence with reason `evidence-expired` (NFR48).

### Story 4.6: AI action preview and inspection

As an authorized user,
I want to preview and later inspect the full lifecycle of an AI action,
So that I can make a safe decision and reconstruct what happened.

**Acceptance Criteria:**

**Given** a proposed action
**When** I preview it
**Then** I can preview outbound communication, file access, command execution, and AI-generated changes before approval or execution (FR45).

**Given** any AI action
**When** I inspect it
**Then** I can view its proposals, approvals, denials, executions, failures, and outcomes (FR44) with audit history available.

### Story 4.7: Allowlisted command execution

As the system,
I want approved AI actions executed only through allowlisted governed commands,
So that AI can never invoke an un-allowlisted command.

**Acceptance Criteria:**

**Given** an approved AI action
**When** it executes
**Then** it runs only through a command in the current allowlist version — in M0, exactly `Project.AppendConversationMessage` — via the CommandGateway → EventStore path (FR43, addendum §Command Allowlist v0).

**Given** a command not in the current allowlist version
**When** execution is attempted
**Then** it fails closed with a redacted rejection (NFR15a), and the attempt is audited.

**And** the executed outcome is recorded as an event and projected back into the project conversation.

### Story 4.8: Refusal and safe-block behavior

As a security owner,
I want unsafe AI/automation/command/mailbox requests refused with a safe, audited message,
So that boundary-crossing attempts are blocked and traceable.

**Acceptance Criteria:**

**Given** a request exceeding tenant policy, project authorization, sender authority, or approved command scope
**When** evaluated
**Then** it is refused or blocked (FR46) with a safe refusal message from the message catalog (FR77) and an audited denial when security-sensitive.

**Given** an unresolved association or missing required context
**When** an AI action is requested
**Then** the AI refuses the project-specific action or asks for association resolution / additional files instead of fabricating (System Journey, Flow 9).

---

## Epic 5: Cross-Surface Parity — CLI & MCP

Let developers and AI agents run the full governed workflow through CLI and MCP with identical authorization, state transitions, redaction, and audit attribution as the UI — service-client identities via Keycloak, thin adapters over the shared command pipeline, and a full differential-conformance harness (parity by construction, verified not enforced by tests).

### Story 5.1: Service-client identities and scoped grants

As an authorized administrator,
I want least-privilege service-client identities with scoped, expiring grants,
So that CLI/MCP/worker/mailbox/AI actors operate without inheriting human roles.

**Acceptance Criteria:**

**Given** the Service Client Permissions model
**When** an administrator configures service-client access (CLI/MCP/workers/mailbox events/AI actors)
**Then** each client gets a dedicated Keycloak service-account identity with least-privilege scopes, an authorized command/query set, and a credential expiry per the addendum table (FR19); service-client authorization never inherits UI roles.

**Given** a delegated flow (e.g., `cli-automation-client` acting for a user)
**When** it executes
**Then** source user, tenant, scope, expiry, and OAuth grant evidence are recorded in audit.

**Given** an expired / revoked / over-scoped / under-scoped credential
**When** used
**Then** the operation fails closed and is covered by an acceptance test (FR19, NFR5).

### Story 5.2: CLI adapter and workflow parity

As a developer/automation builder,
I want a CLI that performs the governed workflow over the same backend,
So that I can script operations without bypassing governance.

**Acceptance Criteria:**

**Given** the CLI (System.CommandLine wrapping `Hexalith.ChatBot.Client`)
**When** I run commands
**Then** I can inspect, associate, reject, defer, correct, retry, approve, execute, check status, and query audit for the governed workflow (FR82), with the same ordered candidates, evidence fields, status/reason codes, and redaction semantics as the UI.

**Given** the CLI adapter
**When** it submits work
**Then** it constructs only `IChatBotCommand` and calls the gateway — no DAPR, no data-plane access, no gateway-stage replication (NetArchTest-enforced).

**Given** a delayed audit projection
**When** a CLI command succeeds
**Then** the CLI returns a clear partial-success state with operation identity and status, not a false full-reconciliation claim (FR80, Flow 6).

**And** stale credentials, a tenant switch, or a revoked service-client scope fail closed without revealing restricted project existence (NFR2).

### Story 5.3: MCP adapter and governed tool surface

As an AI agent/automation tool,
I want governed MCP tools over the same workflow,
So that machine actors operate through the same authorized command model.

**Acceptance Criteria:**

**Given** the MCP server (ModelContextProtocol .AspNetCore wrapping `Hexalith.ChatBot.Client`)
**When** an AI/automation client invokes a tool
**Then** it accesses the same governed workflow operations (FR83), restricted to commands tagged `mcp-exposed`, tenant-aware and scope-bound.

**Given** an unknown or unauthorized tool/argument
**When** invoked
**Then** it is rejected at the contract boundary (with a suggestion) and never exposes restricted evidence, cross-tenant data, or unapproved actions (FR83, NFR2).

**Given** the MCP adapter
**When** it submits work
**Then** it constructs only `IChatBotCommand` and replicates no gateway stage (NetArchTest-enforced).

### Story 5.4: Cross-surface equivalence verification

As an architecture owner,
I want equivalent outcomes across UI/CLI/MCP verified by a full differential-conformance harness,
So that parity is provable and any divergence is a defect.

**Acceptance Criteria:**

**Given** an equivalent semantic intent
**When** submitted through UI, CLI, and MCP
**Then** the system returns equivalent authorization outcomes and state transitions (FR84), and the full differential-conformance harness asserts identical event sequence + state-store end-state (incl. rejection and retry intents) — replacing the M0 shims (FR86 extension).

**Given** any action from any surface
**When** executed
**Then** its origin (UI/API, CLI, MCP, background worker, mailbox event, AI actor) is attributed at the adapter boundary and travels immutably into the audit envelope (FR85 extension).

**And** if equivalent outcomes diverge across surfaces, the divergence is treated as a defect against FR81a, not a tolerance threshold.

---

## Epic 6: Outbound Communication & Inbound Authenticity

Let authorized users draft and send governed project email with preserved sender authority and approval, and carry inbound provider authenticity (DMARC/DKIM/SPF, headers, on-behalf-of, external-sender) into association and risk decisions.

### Story 6.1: Outbound draft creation within authority

As an authorized contributor,
I want to create outbound project email drafts within my project and sender authority,
So that responses originate from governed project context.

**Acceptance Criteria:**

**Given** an authorized contributor with outbound-draft scope
**When** they create an outbound draft
**Then** the draft is created within the approved project and sender authority (FR47), as a `draft-only` action that does not leave ChatBot.

**Given** a contributor lacking project authority or outbound-draft scope
**When** draft creation is attempted
**Then** it fails closed with a redacted reason (NFR2).

### Story 6.2: Sender-authority classes and M365 mapping

As a security engineer,
I want the five outbound sender-authority classes distinguished and mapped to M365 posture,
So that outbound authority is explicit and conflicts fail closed.

**Acceptance Criteria:**

**Given** an outbound action
**When** sender authority is determined
**Then** the system distinguishes draft-only / authenticated-user send / shared-mailbox send / send-on-behalf / approved service-send (FR48), mapping each to its M365 posture and ChatBot-side authorization requirement per addendum §Inbound Message Authenticity.

**Given** M365 grants send-on-behalf but tenant policy disallows it (or requester ≠ delegate, or lapsed shared-mailbox membership, or missing paired approval)
**When** the action is attempted
**Then** it fails closed with the specific reason (`policy-blocked` / `delegation-mismatch` / `membership-revoked` / `approval-missing`) (FR48 conflict rules).

### Story 6.3: Outbound approval gate and approval record

As an authorized approver,
I want outbound communication paused for approval with full record retention,
So that nothing leaves the project boundary without an audited decision.

**Acceptance Criteria:**

**Given** an outbound send
**When** requested
**Then** approval is required before the message leaves the project boundary (FR49), surfaced on the outbound approval surface (S6).

**Given** an outbound approval
**When** recorded
**Then** the record preserves proposed content, approved content, recipients, sender authority, project context, requester, approver, and decision outcome (FR50).

**Given** the outbound-send operation class
**When** a draft send is retried
**Then** the `tenant_id + outbound_draft_id + send_actor` idempotency key rejects a second send with "already sent" (NFR13a) — drafts are single-shot.

### Story 6.4: Inbound authenticity passthrough and header inspection

As a reviewer,
I want inbound provider authenticity verdicts and header discrepancies recorded,
So that authenticity signals inform association and risk without blocking ingestion.

**Acceptance Criteria:**

**Given** an inbound message
**When** intake runs
**Then** the M365/Exchange DMARC/DKIM/SPF verdicts are recorded as-supplied in the intake audit event; ChatBot does not re-verify (FR48a).

**Given** the message headers
**When** parsed
**Then** `Received`/`Authentication-Results`/`From`/`Reply-To`/`Sender`/`X-Original-Sender` are inspected and `From`/`Sender`/`Reply-To` disagreements recorded as intake metadata that feeds the risk classifier and surfaces to the reviewer — without blocking ingestion (FR48b).

### Story 6.5: On-behalf-of disambiguation and external-sender posture

As a security engineer,
I want delegated-send disambiguated and external senders flagged with a strictness knob,
So that authority and external posture drive safe association decisions.

**Acceptance Criteria:**

**Given** a delegated-send relationship from the provider
**When** recorded
**Then** the sender authority is the delegate's identity, with the principal preserved as `principal_for`; outbound actions follow the same rule symmetrically (FR48c).

**Given** a sender with no resolved tenant party
**When** ingested
**Then** the message is flagged `external_sender = true`, and the tenant `mailbox.authenticity-strictness` knob (`permissive`/`strict`/`paranoid`) controls whether it auto-associates, routes to `NeedsReview`, or fails closed (FR48d).

---

## Epic 7: Tenant Administration & Governance Policy

Let tenant administrators configure governed collaboration through bounded, audited admin scopes with no bypass: permission model, policy schema, mailbox config, approval policy, queues, notifications, rate limits, the versioned allowlist, and full-lifecycle completion.

### Story 7.1: Tenant-admin permission model and bounded scopes

As a security owner,
I want a bounded tenant-admin model with see-only vs operate scopes and an audit obligation,
So that admins get the dashboards they need without a bypass to authorization or audit.

**Acceptance Criteria:**

**Given** the admin roles
**When** assigned
**Then** `tenant-admin` holds the union of FR75b–FR75g scopes and finer roles (`mailbox-admin`/`policy-admin`/`compliance-admin`/`operations-admin`) hold proper subsets; admin assignment is security-sensitive (audit event, not by service clients/AI actors) (FR75a).

**Given** see-only scope
**When** an admin views operational queues
**Then** they read queue summaries/health/aggregate metrics across tenant projects without per-project membership, but per-item detail (project name, evidence content, file metadata, audit reasons) requires per-project authority (FR75b).

**Given** operate scope
**When** an admin performs queue-level operations (retry/requeue/quarantine/dismiss)
**Then** the operation is recorded with admin identity, affected items, queue, and reason; admins cannot mutate project-level records (associations/files/approvals) through queue operations (FR75c).

**And** every admin operation — including read-only dashboard access above an aggregation threshold — produces an audit event with admin identity, scope used, items affected, and timestamp; no admin operation has a skip-audit path, and `tenant-admin` does not bypass NFR15a/NFR50a (FR75g).

### Story 7.2: Policy-admin scope, Tenant Policy Schema editor, and AI action policy

As a policy administrator,
I want to configure tenant policy knobs (including AI action policy) within a closed, versioned schema with a two-person rule on sensitive changes,
So that governance behavior is tunable but never unsafe.

**Acceptance Criteria:**

**Given** the closed, versioned Tenant Policy Schema
**When** a policy-admin edits knobs (S5)
**Then** they may only set values within declared types/ranges (tenants cannot define new knobs); each change records actor, old/new value, and timestamp (FR75d).

**Given** a security-sensitive knob (e.g., thresholds, `low-risk-allowed`, allowlist pin)
**When** changed
**Then** it requires a second admin approval (two-person rule) and a documented justification recorded in audit (FR75d).

**Given** AI action policy
**When** configured
**Then** the admin sets `ai-action.low-risk-allowed` per action class (default `false`, opt-in per class) and approval-required behavior (FR52), surfaced through the schema editor.

### Story 7.3: Mailbox-admin scope and mailbox configuration

As a mailbox administrator,
I want to configure monitored mailbox patterns and routing rules and review mailbox health,
So that governed mailbox participation is set up safely without reading content.

**Acceptance Criteria:**

**Given** mailbox-admin scope
**When** configuring
**Then** the admin can configure mailbox patterns, routing rules, and provider-credential connections (FR75e, FR51) and governed mailbox participation rules (FR18), but cannot read mailbox content or decide associations.

**Given** monitored mailboxes
**When** reviewed
**Then** the admin sees mailbox permission status and degraded mailbox processing states scoped to the affected mailbox only, with safe recovery steps and no tenant-wide fallback access (FR53, NFR31).

### Story 7.4: Compliance-admin scope

As a compliance administrator,
I want tenant-wide audit read access and retention configuration without workflow-mutation power,
So that compliance can oversee without operating on project items.

**Acceptance Criteria:**

**Given** compliance-admin scope
**When** exercised
**Then** the admin can read audit records across the tenant (subject to per-project redaction per NFR2), trigger investigations, and configure retention windows within NFR49a bounds (FR75f); they cannot operate on workflow items.

**Given** a project the compliance-admin lacks authority for
**When** audit detail is requested
**Then** restricted detail is redacted and an escalation path is offered without revealing the hidden resource (NFR2).

### Story 7.5: Operational queue management

As an authorized operator,
I want to view, claim/assign, and prioritize operational queues,
So that review work is triaged efficiently across the tenant.

**Acceptance Criteria:**

**Given** the operational queues
**When** I view them
**Then** I can view and manage ambiguous-association, unresolved-participant, pending-approval, failed-ingestion, failed-attachment, and retryable queues (FR69), each row showing state/age/risk/confidence/assignee/next-action/retry-count/terminal status.

**Given** a review item needing human resolution
**When** I act
**Then** I can assign or claim it (FR70), and filter/sort/prioritize queues by age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action (FR78) with server-side filters and pagination (no infinite scroll; default page ≤ 100) (NFR27, UX-DR33).

### Story 7.6: Notifications, escalation, and approval-fatigue controls

As a tenant administrator,
I want notifications and escalation configured with built-in approval-fatigue protection,
So that reviewers are alerted to what matters without being overwhelmed.

**Acceptance Criteria:**

**Given** review/approval/failure/degraded/quarantine/retry states
**When** they require attention
**Then** authorized users are notified (FR72), and the admin can configure notification routing and escalation rules (FR73).

**Given** the approval queue
**When** it operates
**Then** it applies prioritization `(risk × authority-of-affected-party × time-in-queue)`, grouping by `(requester × command × project)` with one audit event per item, a per-user notification ceiling (≤ 8/hr, ≤ 30/day) with digest rollup, and a reviewer-backlog alert above 25 open items (NFR46).

**And** rubber-stamp rate (> 15% approved within < 5 s against `approval-required`, rolling 7 days) is observable and triggers the FR41 tuning revisit (NFR46).

### Story 7.7: Source disable / quarantine / rate-limit controls

As an authorized administrator,
I want to disable, quarantine, or rate-limit misbehaving sources and set per-tenant limits,
So that unsafe or excessive activity can be contained.

**Acceptance Criteria:** *(FR74 — five subject classes × three actions = 15 sub-stories; decompose per (subject × action))*

**Given** a mailbox source / service client / AI actor / command capability / outbound channel producing unsafe/invalid/excessive/policy-violating activity
**When** an admin acts
**Then** they can disable, quarantine, or rate-limit it (FR74); disable and quarantine are security-sensitive (two-person rule per FR75d), and rate-limit is a standard policy mutation.

**Given** per-tenant protection
**When** configured
**Then** the admin sets rate limits, quotas, and circuit breakers for mailbox processing, AI mediation, command execution, and outbound communication (FR75), and a backlog in one source does not degrade unrelated tenants/sources where isolation is possible (NFR30).

### Story 7.8: Command allowlist v1 and full lifecycle completion

As a security engineer,
I want the versioned command allowlist v1 under change control and the full lifecycle state matrix completed,
So that M1 governance breadth lands without weakening the M0 safety floor.

**Acceptance Criteria:**

**Given** the command allowlist
**When** v1 is established
**Then** it is the full catalog minus `disallowed-for-AI` knobs, versioned with per-command metadata (effect surface, authority class, default risk, idempotency contract); a version increment is required to add/remove a command or change its default risk; changes require security-engineer sign-off and are recorded in `.decision-log.md` (addendum §Command Allowlist v1).

**Given** the lifecycle state machine
**When** M1 completes it
**Then** the `Skipped` terminal state (duplicate suppression / out-of-scope mailbox rule) and the full state-transition matrix are in place (extends FR87–FR89), with isolation extended to CLI/MCP/service-client actor types.

---

## Epic 8: Operational Dashboards & Observability

Make the system operable in production: dashboards for mailbox processing, failed associations, approval queues, duplicate handling, AI outcomes, and audit lag; published SLOs with error budgets and alerting; and measurable operational outcomes across all operation classes.

### Story 8.1: Operational dashboards (S8/S10)

As a tenant administrator/operator,
I want operational dashboards across the workflow,
So that I can see processing health and act on problems before they spread.

**Acceptance Criteria:**

**Given** the M2 operational dashboards (S8/S10)
**When** rendered
**Then** they expose mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, and audit projection lag (FR67).

**Given** each queue/health view
**When** it renders
**Then** it shows the queue/health name, current depth or status enum (`healthy`/`degraded`/`failed`/`unknown`, not count-derived), oldest item age, owner role for triage, and a link to per-item detail; it refreshes within the NFR6 staleness bound and shows the freshness timestamp (FR67, NFR48).

**And** the M2 dashboard surfaces conform to WCAG 2.2 AA (NFR60).

### Story 8.2: SLOs, metrics, and alerting

As an operator,
I want published SLOs, per-class metrics, and tied alerting,
So that error budgets are visible and threshold breaches page the right owner.

**Acceptance Criteria:**

**Given** operational outcomes
**When** measured
**Then** OpenTelemetry metrics expose ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag (FR94), published to the per-tenant operational view.

**Given** each SLO
**When** published (addendum §Operating Baselines, created at M2)
**Then** it carries target, measurement window, error budget, and the alert threshold that consumes the budget (NFR42a); initial values per NFR24–NFR27/NFR43, pilot-calibrated per A11.

**Given** the default alert thresholds
**When** breached
**Then** alerts fire for subscription expiry within 7 days, retry exhaustion, audit projection lag > 5 min, approval items older than 2 business days, and authorization-failure spikes above the tenant baseline (NFR43); alerting is non-invasive and tenant-safe.

### Story 8.3: Degraded-state operability and runbook diagnostics

As an on-call engineer,
I want degraded states scoped and every workflow item runbook-diagnosable,
So that I can reach the correct next step from the diagnostic alone.

**Acceptance Criteria:**

**Given** a degraded dependency
**When** detected with monitoring available
**Then** its impact is isolated to the narrowest scope (tenant/mailbox/project/operation/service-client/command-surface) and incident status states the affected scope + dependency within 5 minutes (NFR41).

**Given** a degraded user-facing surface
**When** rendered
**Then** it shows the current state enum, the affected scope, the responsible owner role, and the next safe action affordance, refreshing within NFR6 staleness (NFR42).

**Given** any single workflow item
**When** diagnosed
**Then** runbook-ready diagnostics include correlation ID, tenant ID, mailbox ID, workflow item ID, current state, last transition (timestamp+actor+from-state), retry count, failure reason code (FR77 catalog), and next safe action; a weekly random sample of 100 items each renders a complete diagnostic (NFR44).

---

## Epic 9: Tamper-Evident Audit, Compliance Investigation & Recovery

Make audit defensible and recovery provable: tamper-evident WORM audit with reconstructability as a production observable; safe compliance investigation; isolated replay; retention/export/deletion + consent; derived-store cross-tenant isolation; and recovery/continuity targets.

### Story 9.1: Tamper-evident WORM audit chain

As a compliance owner,
I want an append-only, hash-chained audit store with GDPR-safe redaction,
So that audit history is tamper-evident yet honors erasure.

**Acceptance Criteria:**

**Given** the WORM audit store
**When** an audit envelope is written
**Then** it is appended to a per-tenant hash-chain (each envelope carries its predecessor's hash) in a store where deletion is impossible at the storage layer (NFR49a).

**Given** the nightly chain verification
**When** it runs per tenant
**Then** a broken chain alerts the on-call security engineer within 5 minutes (NFR49a).

**Given** a GDPR right-to-erasure request
**When** processed
**Then** redaction is an appended redaction record (original preserved encrypted, redaction key in a separate KMS) and erasure operates by projection tombstone + key-shred — never by mutating the audit chain (NFR49a, architecture cross-cutting #13).

### Story 9.2: Audit completeness as a production observable

As a compliance owner,
I want audit completeness measured as reconstructability in production,
So that "complete audit" is proven, not assumed.

**Acceptance Criteria:**

**Given** the set of state-mutating operations (NFR15a path inventory)
**When** completeness is measured
**Then** it is the fraction whose audit chain reconstructs the operation end-to-end from the chain alone (every input, decision, resource reference, policy snapshot, and outcome), not merely field presence (NFR50a).

**Given** the rolling 7-day window per tenant
**When** completeness drops below 99.5%
**Then** a P1 incident is triggered (NFR50a); a scheduled production assertion rebuilds state from the log and diffs the projection.

**And** replay events are excluded from numerator and denominator (FR95a).

### Story 9.3: Audit query and compliance investigation surface (S9)

As a compliance/support reviewer,
I want to search and reconstruct what happened with safe redaction,
So that I can investigate without leaking unauthorized context or gaining mutation power.

**Acceptance Criteria:**

**Given** the Audit Investigation surface (S9)
**When** I search
**Then** I can query by tenant, actor, command, resource, decision, reason, correlation, message ID, surface, and time (FR56) and reconstruct association decisions, approvals, command outcomes, corrections, retries, and risky AI actions (FR54).

**Given** a project I lack authority for
**When** results render
**Then** restricted detail is redacted and an escalation path is offered without revealing the hidden resource; my role grants read/escalate only, not mutation (FR54, NFR2, Flow 7).

**And** replay events are distinguishable (`replay_run_id`) and excluded from default production audit queries (FR95a).

### Story 9.4: Replay and simulation isolation

As a QA/support engineer,
I want to replay representative mailbox events in full isolation,
So that investigation and testing never touch production or send external email.

**Acceptance Criteria:**

**Given** a replay/simulation run
**When** executed
**Then** it runs against a dedicated test tenant whose outbound adapter intercepts every external action and records the would-have-sent envelope instead of sending, mutating no production project state (FR95, FR95a).

**Given** replay events
**When** audited
**Then** they carry `replay_run_id`, are excluded from production audit queries by default, and are excluded from NFR50a completeness measurement (FR95a).

**Given** the nightly isolation probe
**When** it runs
**Then** it asserts no replay run has ever produced a record in any production tenant's outbound-trace store, and probe failure gates the M2 release (FR95a, addendum §Replay Isolation).

### Story 9.5: Derived-store cross-tenant isolation

As a security owner,
I want vector/embedding/cache derived stores isolated per tenant at the store layer,
So that an application bug cannot produce a cross-tenant read.

**Acceptance Criteria:**

**Given** vector indexes, embedding stores, prompt-context caches, and candidate-ranking caches (Hexalith.Memories)
**When** built
**Then** they are partitioned per tenant at the store level (not application filtering); a cross-tenant query through the store's native API fails at the storage layer (FR55a, NFR9a).

**Given** the nightly synthetic cross-tenant probe
**When** it runs
**Then** it attempts cross-tenant reads through the store-access layer and asserts failure below the application layer; probe failure is a stop-ship defect (FR55a, NFR9a).

### Story 9.6: Data retention, export, deletion, and consent

As a compliance administrator,
I want retention/export/deletion workflows by data class plus consent metadata,
So that GDPR obligations are met and retained data is minimized.

**Acceptance Criteria:**

**Given** the data classes (source email, metadata, attachments, derived projections, AI prompts/outputs, approvals, policy snapshots, logs, backups, evaluation datasets, audit records)
**When** retention/export/deletion runs
**Then** workflows distinguish each class (NFR53) and provide operational support for tenant data retention, export, and deletion (FR58); retained content is minimized to what the workflow/audit/retention policy requires (NFR52).

**Given** tenant policy or regulatory profile requires it
**When** external participants, retained content, attachments, or AI processing are recorded
**Then** consent or lawful-basis metadata is recorded (FR20, NFR55).

### Story 9.7: Recovery and continuity

As an operations owner,
I want verified recovery targets and scoped outage degradation,
So that the system is provably operable and isolated under failure.

**Acceptance Criteria:**

**Given** source records, attachments, approvals, command history, policy snapshots, and audit records
**When** the M2 continuity drill runs
**Then** recovery meets RPO ≤ 15 min and RTO ≤ 4 hr (drill executes recovery from a simulated EventStore outage and a simulated M365 subscription failure), recalibrating the [ASSUMPTION] targets per A10 (NFR56).

**Given** derived projections
**When** rebuilt
**Then** they rebuild from immutable source records + audit history within 4 hr for the baseline validation dataset, without mailbox re-ingestion (NFR57).

**Given** a dependency outage (Graph / identity / AI provider / command execution / audit store / attachment processing)
**When** it occurs
**Then** only the affected tenant/mailbox/operation/service-client/command-surface/workflow-item degrades, and resilience validation proves no cross-tenant leakage, unauthorized state mutation, or silent data loss (NFR58, NFR59).
