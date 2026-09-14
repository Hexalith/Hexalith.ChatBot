---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
requirementsExtractionStatus: confirmed
epicDesignStatus: approved
epicCount: 13
functionalRequirementCount: 117
nonFunctionalRequirementCount: 79
architectureRequirementCount: 41
uxDesignRequirementCount: 70
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md"
  - "_bmad-output/planning-artifacts/architecture.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/implementation-conformance-addendum-2026-07-17.md"
---

# chatbot - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for chatbot, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

- FR1: The system can capture authorized mailbox events as project collaboration inputs.
- FR2: The system can preserve source email identity, thread identity, mailbox identity, sender, recipients, timestamps, and attachment references.
- FR3: The system can associate incoming email with an existing project using deterministic evidence.
- FR4: The system can detect ambiguous project association and route it to human review.
- FR5: Authorized users can review candidate projects with visible evidence, confidence state, reason codes, and the consequences of each available decision.
- FR6: Authorized users can choose a candidate project, reject all candidates, defer association, mark an item as needing review, and provide an optional decision note.
- FR7: Authorized users can correct a previously selected project association.
- FR8: The system can record association decisions, corrections, rejections, deferrals, retries, and skipped items.
- FR9: Tenant administrators can configure association rules, evidence requirements, and security-sensitive `T_high`/`T_low` thresholds within the closed policy schema, with authorization, independent approval where required, calibration guardrails, and audit.
- FR10: The system can preserve original email context when association is rejected, deferred, failed, skipped, or awaiting review.
- FR11: The system can expose deterministic association reasons and confidence inputs in machine-readable form for UI, CLI, MCP, audit, and test verification.
- FR12: Authorized users can compare candidate project evidence side by side when resolving ambiguous association.
- FR13: The system can resolve internal and external email participants to tenant-scoped parties.
- FR14: Authorized users can identify unresolved participants for review.
- FR15: External participants can contribute project context through email without requiring MVP external portal access.
- FR16: The system can enforce tenant and project authorization before exposing project candidates, files, conversations, approvals, commands, or audit details.
- FR17: The system can block unresolved or unauthorized actors from accessing project files, creating task requests, triggering commands, or sending outbound communication.
- FR18: Tenant administrators can configure governed mailbox participation rules.
- FR19: Authorized administrators can configure service-client access for CLI, MCP, background workers, mailbox events, and AI actors.
- FR20: The system can record consent or lawful-basis metadata where tenant policy requires it for external participants, retained email content, attachments, and AI processing.
- FR21: Authorized users can view email-derived messages as project conversation context.
- FR22: The system can represent associated email, participants, attachments, decisions, approvals, failures, and AI outcomes in project context; story authoring must decompose these seven independently testable concerns.
- FR23: Authorized users can inspect why an email belongs to a project, including signal class/value, confidence/disposition, reason, candidate-visibility rule, decision actor/time, evidence, and correction links.
- FR24: Authorized users can see association, attachment, task, approval, command, failure, retry, and next-action status for a project conversation.
- FR25: The system can keep project conversation context separate across tenants and projects.
- FR26: The system can distinguish informational context from actionable `request-information`, `request-action`, or `request-decision` intent and expose detector version, evidence offsets, confidence, and review actions without implying action risk.
- FR27: The system can distinguish AI-generated summaries from source evidence using a visible `AI summary` label, provenance, collapsed-by-default generated content, expanded-by-default sources, and non-color WCAG 2.2 AA treatment.
- FR28: The system can preserve visible human-review history for each email, attachment, approval, AI action, and command.
- FR28a: Authorized users can submit a Project-scoped message through the FrontComposer S1a composer; every submission enters CommandGateway with actor, tenant, Project, conversation, stable `operation_id`, expected revision, and source attribution.
- FR28b: The composer shows `accepted`, `needs-review`, `approval-required`, `denied`, `unsupported`, or typed failure before implying that AI work has started.
- FR28c: Eligible read-only/no-external-effect responses may stream with model/version, evidence provenance, and completion state; partial output is visibly partial and never a committed Project message.
- FR28d: Users can stop a `Streaming` generation, cancel an `Admitted` pre-stream request, or cancel an `AwaitingApproval` proposal through the distinct canonical commands; expected revision and first-commit-wins behavior determine the recorded outcome.
- FR28e: Every request that modifies state, exposes files, sends externally, creates/assigns tasks, invokes tools, or acts on behalf becomes a mandatory-approval proposal before any boundary-crossing output or effect.
- FR28f: Each logical chat submission has a stable `chat_request_id`; immutable attempts have distinct operation IDs, expected revision, and predecessor links; retry creates one linked attempt, never resumes partial output, and every race/failure is safely surfaced and audited.
- FR29: The system can capture attachments from associated project email.
- FR30: The system can store captured attachments in governed project folders.
- FR31: Authorized users can inspect attachment capture and storage status.
- FR32: The system can prevent unauthorized actors from viewing attachment metadata or content.
- FR33: The system can make authorized project files available as scoped AI context only through explicit authorization, policy checks, and auditable context packaging.
- FR34: The system can represent attachment states including captured, pending, unavailable, rejected, unsafe, failed, and retryable.
- FR35: The system can detect candidate task/action intent from authorized conversation actors and preserve the source evidence, tenant/project/requester fields, ≤280-character summary, action kind, evidence offsets, detector version, confidence, time, and state.
- FR36: Authorized users can review captured task intent, its full source message and data contract, and the available governed dispositions before action.
- FR37: Authorized users can convert captured task intent into an audited governed proposal linked to its source task-intent record.
- FR38: Authorized users can terminally mark captured task intent as not actionable, duplicate, already handled, or out of scope, retaining evaluation evidence and predecessor linkage for duplicates.
- FR39: The system can classify AI action requests through an independently versioned categorical `ActionRiskClassifier` that shares no score or runtime artifact with association or task-intent detection.
- FR40: The system can allow only product-declared read-only/no-external-effect subtypes when tenant policy and Project authorization permit them.
- FR41: The system must require approval for Project mutation, file exposure, external communication, task creation/assignment, tool invocation, and acting on behalf; tenant policy cannot downgrade these six effect classes.
- FR42: Authorized users can approve, reject, request revision, or cancel after reviewing command/allowlist version, resources/files and redaction, recipients, sender authority, classifier inputs/version, policy snapshot, expected post-state/audit events, and their own authority.
- FR43: The system can execute approved AI actions only through allowlisted governed commands.
- FR44: Authorized users can inspect AI action proposals, approvals, denials, executions, failures, and outcomes.
- FR45: Authorized users can preview outbound communication, file access, command execution, and AI-generated changes before approval or execution.
- FR46: The system can refuse or block unsafe AI, automation, command, or mailbox requests that exceed policy, Project authorization, sender authority, or approved command scope.
- FR47: Authorized users can create outbound project email drafts within approved Project and sender authority.
- FR48: The system can distinguish draft-only, authenticated-user send, shared-mailbox send, send-on-behalf, and approved service-send authority, resolving M365/ChatBot authority conflicts fail-closed.
- FR48a: Every inbound event records provider-supplied M365/Exchange DMARC, DKIM, and SPF verdicts.
- FR48b: The mailbox adapter parses required authenticity/sender headers and records discrepancies as intake metadata and review reason codes without broadening trust.
- FR48c: Delegated-send evidence records the delegate as sender authority and the principal as `principal_for`, applying the same identity rule outbound.
- FR48d: Unresolved external senders carry `external_sender=true`; only `strict` or `paranoid` authenticity modes are allowed, routing anomalies to review or block.
- FR49: The system must require authorized human approval before outbound Project communication leaves the Project boundary.
- FR50: The system can preserve proposed and approved content, recipients, sender authority, Project context, requester, approver, and decision outcome in approval records.
- FR51: Tenant administrators can configure mailbox integration settings and monitored mailbox patterns.
- FR52: Tenant administrators can enable product-declared low-risk read-only subtypes and configure approval routing but cannot downgrade FR41 effects or extend the AI allowlist.
- FR53: Tenant administrators can review mailbox permission status and degraded mailbox processing states.
- FR54: Compliance or support reviewers can investigate association and approval decisions, command outcomes, and risky AI actions.
- FR55: The system must atomically produce a canonical audit envelope for every durable mutation and separately record security-sensitive non-mutating attempts such as denials, restricted reads, and service-client failures.
- FR55a: Every tenant-material derived store must enforce isolation by construction and pass native-store/API negative tests in its first increment; no multi-tenant or machine-surface use is allowed before proof, and M2 adds vector/cache proofs plus recurring probes.
- FR56: Authorized users can query audit records by tenant, actor, command, resource, decision, reason, correlation, and time context.
- FR57: The system can hide unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details.
- FR58: Authorized administrators or reviewers can access operational support for tenant data retention, export, and deletion workflows.
- FR59: The system can propagate correlation context across mailbox intake, association, file handling, approval, AI mediation, command execution, audit, UI, CLI, and MCP.
- FR60: The system can preserve source evidence used for association, authorization, approval, rejection, refusal, correction, retry, and investigation with retention and redaction boundaries.
- FR61: The system can maintain immutable versioned policy snapshots used for association, authorization, approval, AI classification, and command execution decisions.
- FR62: Authorized users can append non-authoritative human notes or rationale linked to canonical workflow envelopes; annotations cannot alter decisions or repair missing audit completeness.
- FR63: Authorized users can supersede reversible human decisions where policy permits while preserving the original decision in audit history.
- FR64: The system can detect duplicate mailbox delivery and avoid duplicate project artifacts.
- FR65: The system exposes family-specific retry/recovery commands; approvals and other immutable decisions use named successors or supersession, never in-place retry.
- FR66: The system can surface terminal and non-terminal failure states to authorized users.
- FR67: The system can expose mailbox health and workflow queues with stable state, depth, oldest age, triage owner, freshness, and links to authorized item details.
- FR68: The system can fail closed when association, participant identity, tenant scope, authorization, audit writing, or required dependencies cannot be resolved.
- FR69: Authorized users can view and manage queues for ambiguous associations, unresolved participants, approvals, failed ingestion/attachments, and retryable operations.
- FR70: Authorized users can assign or claim review items that require human resolution.
- FR71: Authorized users can see the next required human action for an email, task intent, attachment, approval, or failed operation.
- FR72: The system can notify authorized users when review, approval, failure, degradation, quarantine, or retry states require attention.
- FR73: Tenant administrators can configure notification routing and escalation rules for unresolved review, approval, degraded, quarantine, and failure states.
- FR74: Authorized administrators can disable, quarantine, or rate-limit exactly mailbox sources, service clients, AI actors, and command capabilities through the closed safety-control matrix; story coverage must include all twelve subject/action cells and release flows.
- FR75: Authorized administrators can configure per-tenant rate limits, quotas, and circuit breakers for mailbox processing, AI mediation, command execution, and outbound communication.
- FR75a: `tenant-admin` is the union of finer mailbox, policy, compliance, and operations scopes; only the three named admin-role commands may mutate grants, requiring current and independent Tenants owners, closed scope, expected revision, and atomic audit; service clients/AI cannot participate.
- FR75b: Admins may see aggregate queue/health metrics tenant-wide, but per-item Project identity/evidence/file/audit detail requires current Project authority.
- FR75c: Admins without Project authority may pause/resume mailboxes, clients, or opaque queue partitions and request content-free investigation, but cannot mutate individual Project workflow items.
- FR75d: A `policy-admin` can initiate only schema-authorized policy knobs; every security-sensitive knob requires an independent authorized admin, separation of duty, justification, and canonical audit.
- FR75e: A `mailbox-admin` can configure patterns, routing, and provider credentials; authenticity changes need independent policy approval, and the role grants neither mailbox-content read nor association-decision authority.
- FR75f: A `compliance-admin` can read redacted tenant audit, trigger investigations, and initiate A6/NFR49a-bounded retention changes with independent approval, but cannot operate workflow items.
- FR75g: Every admin operation, including qualifying read-only dashboard access, is audited with identity, scope, affected items, and timestamp; no admin role bypasses fail-closed or audit invariants.
- FR76: Review actions visibly resolve to `enabled`, `disabled-with-reason`, or `not-applicable-hidden`, using finite safe reason codes and next guidance naming a responsible role or available action.
- FR77: Every refusal/blocked/degraded/failed/denied state uses a versioned safe message code, ≤80-character headline, existence-neutral reason, and safe retry/escalate/dismiss/request-access action.
- FR78: Authorized users can filter, sort, and prioritize operational queues by age, risk, confidence, Project, mailbox, failure state, reviewer, and next action.
- FR79: The system can show stale, waiting, blocked, and escalation-needed states for review queues and long-running operations.
- FR80: UI, CLI, and MCP users can retrieve long-running status with operation identity, state, retry count, partial outputs, safe next actions, terminal reason, and correlation.
- FR81: Authorized UI users can perform the core governed email-to-project workflow operations.
- FR81a: Every mutation from every origin passes through one command spine that authenticates, tenant-binds, authorizes, classifies risk, validates approval/identity/revision, constructs the canonical envelope, and atomically commits event, idempotency, policy/approval references, and audit—or commits nothing.
- FR82: Authorized CLI users can perform the singular M1 parity set for intake/candidates/association, attachments, task intent, AI approval/execution, retry, status, and audit.
- FR83: Authorized MCP clients can access the same M1 parity set subject to exposure policy and actor scope; MCP exposure does not extend the AI allowlist.
- FR84: UI, CLI, and MCP must return equivalent authorization outcomes and state transitions as a verification consequence of FR81a.
- FR85: The system can immutably identify UI/API, CLI, MCP, worker, mailbox-event, or AI origin from the adapter boundary through audit.
- FR86: Contract tests must prove equivalent inputs from every surface produce the same canonically normalized Command record; any divergence is an FR81a invariant violation.
- FR87: The system can define canonical family-specific lifecycle states for intake, participant resolution, attachments, approvals, AI actions, commands, and audit projection.
- FR88: The system can validate inbound and outbound workflow transitions against explicit state models.
- FR89: The system can reject invalid transitions and record the transition, actor, reason, and correlation context.
- FR90: The system can expose lifetime-stable operation IDs, one decision slot per human-decision subject, expected revisions, and stable resource identifiers across workflows.
- FR91: The system can separate immutable source records from derived Project projections and rebuild projections from sources when needed.
- FR91a: Correction must invalidate/rebuild every affected derived store, remain visibly `correcting` until all acknowledgements, block affected AI context, surface `correction-delayed` on SLO breach, and audit predecessor, correction, and store outcomes.
- FR92: Authorized product or QA users can maintain consented, redacted, or synthetic evaluation datasets with expected outcomes, redaction expectations, and regression history.
- FR93: The system can provide tenant-scoped fixtures/sandbox data for mailbox, association, authorization, attachment, approval, AI, command, and audit validation.
- FR94: The system can expose measurable ingestion, association, approval, command, retry-exhaustion, duplicate-suppression, and audit-lag outcomes through increment-appropriate operational surfaces.
- FR95: The system can simulate or replay representative mailbox events for authorized QA/support without external communication or production Project mutation.
- FR95a: Replay uses a credential-free replay-only composition, safe replacements for every effectful adapter, default-deny egress, `replay_run_id`, production-audit exclusion, and before/after production-store and external-ledger invariance; any failure blocks M2.
- FR96: Recorded corrections may inform future association only when policy permits, evidence remains explainable, and users can inspect its influence.

### NonFunctional Requirements

- NFR1: Every command and query must authorize tenant, actor, role, Project, and resource before data return or mutation.
- NFR2: Unauthorized humans and machine actors must receive redacted responses that reveal no restricted Project, file, evidence, audit, or tenant data.
- NFR3: Email, attachments, AI data, audit, credentials, policy snapshots, telemetry, backups, and datasets must be encrypted in transit and at rest with validated tenant-appropriate separation and no plaintext diagnostic/export leakage.
- NFR4: Secrets and mailbox, service-client, CLI, MCP, AI-tool, and provider credentials must never appear in logs, traces, outputs, audit, support bundles, or UI diagnostics.
- NFR5: M365, service-client, CLI, MCP, and AI-tool credentials must be least-privilege and revocable without broader fallback access.
- NFR6: Authorization/policy/identity caches must cap ordinary staleness at five minutes and explicit-revocation staleness at 60 seconds, verified by automated revocation tests.
- NFR7: Security-sensitive operations must fail closed when identity, tenant, authorization, audit readiness, policy, or command validation is unavailable.
- NFR8: AI actors must operate only through explicitly authorized Project scope, files, tools, commands, and policy authority.
- NFR9: Every AI context package must be tenant/Project scoped, policy-redacted and retention-governed, prevent unauthorized training/telemetry/reuse, and include tenant, Project, evidence, policy, redaction, retention, and provider-reuse facts before invocation.
- NFR9a: Every derived store must be tenant-partitioned below the application and pass native-store/API negative isolation tests in its first increment; unproven stores block their increment and multi-tenant use, with M2 vector/cache proofs and nightly probes.
- NFR10: Logs, metrics, traces, support bundles, and test artifacts must pass secret and sensitive-data redaction checks before export or external sharing.
- NFR11: Cross-tenant isolation tests have zero tolerance for leakage through candidates, evidence, files, summaries, prompts, machine surfaces, telemetry, or audit.
- NFR12: Residency/region boundaries for each persisted data class must be defined before applicable tenant onboarding and validated against the approved deployment profile.
- NFR13: Repeated equivalent workflow inputs must converge using lifetime-stable operation IDs, one authoritative decision slot, and expected revisions or an A13-approved equivalent concurrency contract.
- NFR13a: Each operation class must define durable identity, semantic equivalence, and typed conflict behavior; time-window hashes may suppress only non-mutating proposals and can never permit a durable mutation/decision twice.
- NFR14: Duplicate mailbox delivery must not duplicate Project messages, attachments, task intents, approvals, commands, notifications, outbound sends, or audit decisions.
- NFR15: Invalid transitions must reject deterministically before mutation and be audited; unavailable audit storage makes every mutation fail closed.
- NFR15a: Every durable write atomically commits domain event, idempotency state, policy/approval references, and canonical audit envelope or commits nothing; all enumerated mutation paths return typed `AuditUnavailable` without authoritative state when the boundary is unavailable.
- NFR16: Risky AI actions, sends, command execution, and file-context packaging must not execute without valid approval, policy, authority, input validation, and audit readiness.
- NFR17: Partial failures must leave visible recoverable pending, retryable, failed, quarantined, or review states.
- NFR17a: Correction propagation must meet p95 ≤10 minutes in M0/M1 and ≤60 minutes in M2; breach produces `correction-delayed`, names owner/next action, keeps AI blocked, and triggers P2.
- NFR18: Every workflow must use approved Retry Profile v1 or a stricter profile, with tested retryability, ceilings, jittered backoff, exhaustion/dead-letter behavior, manual recovery, immutable links, and no repeated committed effect.
- NFR19: Background and async processing must safely support at-least-once delivery using idempotency, concurrency control, lease expiry, and poison-message handling.
- NFR20: Queue processing must prevent starvation across tenants, mailboxes, Projects, and workflow types while honoring priority, limits, and breakers.
- NFR21: File processing must enforce malware/unsafe-content rules, size/type limits, scan status, quarantine, and safe failure before Project or AI exposure.
- NFR22: When non-AI dependencies remain healthy, manual review/association, existing-proposal decisions, retry, and audit must continue during AI-provider outage and be proven without live AI calls.
- NFR23: Tenant/deployment operating baselines must be versioned, quarterly reviewed, and record owner, approval/review dates, and accepted thresholds for latency, backlog, recovery, alerts, datasets, and capacity.
- NFR24: User-facing conversation, queue, status, and audit reads must meet default p95 ≤2 seconds under the MVP baseline, measured by synthetic checks and production APM.
- NFR25: Candidate generation must meet p95 ≤10 seconds or return retrievable pending/review status with operation identity and safe next action.
- NFR26: Long-running CLI/MCP work must return identity/current status within p95 ≤5 seconds and must not hold the connection beyond 30 seconds without a retrievable status response.
- NFR27: Queue views require server-side filtering, sorting, prioritization, pagination, and default page size ≤100 for the declared filter dimensions.
- NFR28: Operational latency telemetry must include percentiles, error/retry rates, queue age, saturation, and audit projection lag.
- NFR29: Tenant-level rate limits, quotas, and circuit breakers must protect mailbox, AI, commands, outbound, UI/API, CLI, and MCP.
- NFR30: Backlog in one tenant, mailbox, Project, service client, AI actor, or surface must not degrade unrelated scopes where isolation is possible.
- NFR31: M365/Exchange integration must tolerate revocation, expiry, throttling/backoff, partial access, duplicates, delay/replay, subscription expiry, and permission drift without broadening access.
- NFR32: All surfaces, workers, handlers, persisted contracts, projections, and replay fixtures must use contract-verifiable stable IDs/codes/states, redaction, correlation, and equivalent authorization outcomes.
- NFR33: API, CLI, MCP, event, audit, projection, and state contracts must evolve compatibly or use explicit versioning, deprecation, and migration for breaking change.
- NFR34: Correlation context must traverse mailbox, file, association, approval, command, AI, audit, UI/API, CLI, MCP, worker, and webhook boundaries.
- NFR35: Configuration/policy changes must be audited, versioned, non-destructively rollback-capable, and applied prospectively; destructive or authority-expanding changes require new immutable versions.
- NFR36: Workflow, audit, retry, approval, retention, freshness, and SLA time uses server UTC, preserves relevant source timezone, and converts only at presentation.
- NFR37: Authorized operators must observe mailbox health/backlog and every declared workflow, authorization, service-client, AI, command, retry, duplicate, and audit-lag failure/queue.
- NFR38: User-visible status and privileged diagnostics must remain separated by authorization.
- NFR39: Degraded, stale, waiting, blocked, escalation-needed, failed, retryable, and terminal states must carry actionable status.
- NFR40: Every user-visible degraded/blocked/failed/waiting state must use the versioned safe message catalog; raw error leakage count must remain zero and any violation blocks release.
- NFR41: Degradation must be isolated to the narrowest identifiable scope, and monitored incidents must name affected scope/dependency within five minutes.
- NFR42: Each degraded surface must display state, affected scope, responsible owner, and next safe action within cache-staleness bounds, enforced by synthetic checks.
- NFR42a: The M2 tenant view and operating baseline must publish every declared SLO with target, window, error budget, alert threshold, and A11-calibrated evidence.
- NFR43: Non-invasive tenant-safe alerts/synthetics must cover declared dependencies and thresholds, including subscription expiry ≤7 days, retry exhaustion, audit lag >5 minutes, approval age >2 business days, and authorization spikes.
- NFR44: Weekly sampling of 100 workflow items must prove complete runbook diagnostics: correlation, tenant, mailbox, item, state, last transition/actor/time, retry count, catalog reason, and next action.
- NFR45: Redacted support bundles must preserve correlation/state/reason while excluding restricted tenant, Project, participant, file, message, and audit evidence.
- NFR46: Approval-fatigue controls must implement risk/authority/age prioritization, strictly safe per-item grouping, ≤8 push notices/hour and ≤30/day with digest overflow, >25-item reviewer alerts, and ≤15% rubber-stamp monitoring without bypassing approval.
- NFR47: Risky automation must distinguish reversible, supersedable, compensating, and irreversible actions before approval.
- NFR48: Every surfaced evidence reference must show timestamp and `fresh|stale|expired`; expired evidence disables approval with `evidence-expired`, and approval renders exactly one freshness indicator per evidence reference.
- NFR49: Audit must be tamper-evident, retention-governed, redaction-aware, reconstructable, and mutable/deletable only by authorized retention workflows.
- NFR49a: Canonical envelopes are append-only/hash-linked per aggregate stream with signed tenant checkpoints; fork/order/checkpoint failure alerts Security within five minutes and triggers P1, while A6/A13 block onboarding and data-protection/tamper-evidence claims until approved.
- NFR50: Canonical mutations and security-sensitive attempts must carry the complete declared actor/resource/decision/evidence/policy/transition/redaction/identity/outcome fields, verified for 100% of mutations and sampled sensitive attempts.
- NFR50a: Canonical envelope completeness for committed mutations is 100%; the separate M2 investigation projection must reconstruct ≥99.5% over rolling seven-day tenant windows or trigger P1/rebuild, excluding replay.
- NFR51: Audit/diagnostics must reconstruct who acted, what was attempted, policy/evidence/transitions/redaction applied, and outcome.
- NFR52: Retained email, attachment, AI, diagnostic, and support data must be minimized to authorized workflow, audit, and tenant-retention needs.
- NFR53: Retention/export/deletion must distinguish every declared source, derived, AI, approval, policy, log, backup, dataset, and audit data class.
- NFR54: Audit evidence must honor retention and redaction so evidence preservation does not become uncontrolled storage.
- NFR54a: Metadata-only recovery diagnostics, independently validated story-completion authority, and retained A10 operational evidence are disjoint channels and cannot substitute for one another.
- NFR55: Policy/regulatory profiles must record consent or lawful-basis metadata for external participants, retained content, attachments, and AI processing where required.
- NFR56: Source email, attachments, approvals, commands, policies, and audit target RPO ≤15 minutes/RTO ≤4 hours; targets remain A10-provisional and block M2 until fresh qualification.
- NFR57: Derived projections must rebuild from immutable sources/audit within four hours for the baseline dataset without mailbox re-ingestion.
- NFR58: Outages must degrade only identifiable affected scopes; tests must prove unrelated tenants/mailboxes remain usable across Graph, identity, AI, command, audit, and attachment failures.
- NFR59: Resilience tests must prove declared dependency failures cause no tenant leak, unauthorized/unaudited mutation, or silent loss, alongside first-store isolation and M2 recurring probes.
- NFR60: Every increment’s enumerated UI surfaces must meet WCAG 2.2 AA before release through automated, keyboard-only, and screen-reader validation; CLI/MCP are outside WCAG scope.
- NFR61: Accessibility validation must cover keyboard review, screen-reader labels, focus order, non-color states, and error recovery for association and approval.
- NFR62: Status/failure/refusal/authorization messages must be understandable, existence-neutral, and not color-only.
- NFR63: Association and approval users must identify their next action without raw audit logs.
- NFR64: UI must clearly distinguish authoritative source evidence from AI summaries.
- NFR65: Increment/release gates must prove isolation, authorization/redaction, idempotency, executable transitions, non-downgradable approval, authenticity, duplicate suppression, atomic audit, accessibility, evidence ownership, and disable conditions; only M2 may claim production/release-candidate readiness.
- NFR65a: Recovery completion accepts one exact-candidate current-run producer under approved policy and fails on any planning, execution, restoration, cleanup, projection, attestation, independent-validation, or publication gap; inactive/pre-activation and retained operational evidence cannot claim completion authority.
- NFR66: Performance validation must prove mailbox backlog, queue usability, retry, audit lag, and throttled Graph behavior against the approved baseline.
- NFR67: Security validation must include negative authorization for UI/API, CLI, MCP, workers, mailbox events, service clients, and AI actors.
- NFR68: Evaluation data/fixtures must be consented, redacted, or synthetic and versioned/reproducible with redaction checks, expected outcomes, and regression history across association, authorization, duplicate, retry, approval, refusal, and audit.
- NFR69: Replay/simulation must exclude production resources by construction, replace every effectful adapter, deny egress, label and tenant-scope artifacts, and prove production-store/external-ledger invariance.
- NFR70: Every externally visible operation must define transition, audit event, user response, redaction, and retry/idempotency outcome.

### Additional Requirements

- ARCH-1 — Starter story: the first implementation story must scaffold `Hexalith.ChatBot` from the canonical sibling-module convention (no generator exists), using `.slnx` and `Contracts`, `Client`, `Server`, `Testing`, M0 `UI`/`Workers`, M1 `Cli`/`Mcp`, mirrored xUnit v3 tests, and the OpenAPI contract spine.
- ARCH-2 — The starter must add `Hexalith.EventStore` only as a root-declared, non-recursive submodule and establish `global.json`, warnings-as-errors/nullability, `.editorconfig`, `nuget.config`, and a version-free `Directory.Packages.props` importing the shared Builds catalog.
- ARCH-3 — ChatBot is an EventStore domain module hosted through `Hexalith.EventStore.DomainService`; the target host uses the SDK registration/middleware shape, domain query/projection handlers, read-model store/write policy, scoped cursor codec, platform telemetry, and platform health checks.
- ARCH-4 — The CommandGateway admission stages mount at the EventStore SDK pre-commit hook and must not become a second write pipeline; the platform hook is a technical prerequisite.
- ARCH-5 — Standalone production `.Aspire` and `.ServiceDefaults` projects are prohibited after host reuse. A thin `Hexalith.ChatBot.AppHost` is retained only as an ADR-scoped local-development umbrella while dedicated ChatBot resources are not modeled by the platform API.
- ARCH-6 — The implementation baseline is .NET 10/C# 14, Dapr application SDK 1.18.5, Aspire AppHost 13.5.3, Keycloak OIDC, Blazor/Fluxor, FrontComposer, and Fluent UI v5 at the centrally governed prerelease pin; package upgrades require dependency governance rather than local overrides.
- ARCH-7 — EventStore write code follows CQRS/event-sourcing conventions: pure `Handle`/`Apply`, persist-before-publish, rejection events for business failure, ULIDs rather than GUID validation, tenant/domain/aggregate identity, and backward-compatible event upcasting.
- ARCH-8 — Contracts depend inward toward Server; UI/CLI/MCP depend only on the typed Client; aggregates and governance internals live only in Server, and each C# file contains one documented type.
- ARCH-9 — The deployable is a modular monolith with hard Association, Governance/Mediation, Lifecycle/Workflow, Projection/Query, and Audit/Replay seams that communicate across seams through events rather than internal method calls.
- ARCH-10 — Every sibling/external dependency is wrapped behind a ChatBot-owned adapter; aggregates perform no I/O, only stable owner IDs—not copied owner PII—enter events, and ChatBot never invents producer contracts or dual-writes owner state.
- ARCH-11 — Current owner-context authority is required for trust-bearing decisions; local event-fed mirrors are display-only, missing/stale/malformed owner evidence denies, and ChatBot/global-admin labels never broaden tenant or Project authority.
- ARCH-12 — Every mutating origin constructs a typed `IChatBotCommand` and reaches one `CommandGateway` stage sequence; adapters may translate and attach immutable origin only, never authorize, classify, approve, deduplicate, or audit independently.
- ARCH-13 — The A13-gated atomic boundary co-commits domain events, lifetime terminal idempotency, policy/approval references, and the hash-linked canonical envelope through the supported actor-dispatched write path, with ACL, ETag/first-write fencing, fork/reorder/recovery tests, or commits nothing.
- ARCH-14 — Cross-context choreography records orchestration intent in ChatBot, submits owner-accepted commands idempotently with the same authority/revision/evidence/identity tuple, treats the owner event/revision as authoritative, and never retries an already committed owner effect as uncommitted work.
- ARCH-15 — OpenAPI 3.1 is the sole HTTP wire-contract source; a generated Client, idempotency helpers, exposure/allowlist mappings, and a parity oracle must remain synchronized and non-vacuously validated.
- ARCH-16 — API failures use metadata-only RFC 9457 responses backed by the versioned safe message catalog; raw exception text, payloads, PII, and secrets are prohibited from user responses and telemetry.
- ARCH-17 — Immutable candidate/evidence/proposal/approval/policy decisions are append-only and superseded rather than mutated; live mirrors are idempotent, order-tolerant, and last-writer-wins by source version rather than arrival time.
- ARCH-18 — Every derived record carries tenant, provenance, derivation-contract, redaction, retention, and schema versions; relevant detector/classifier/model versions and first-class evidence/confidence/correction fields are added by record type.
- ARCH-19 — Physical tenant isolation derives only from trusted server context and covers store keys, caches, cursors, topics, queues/dead letters, projections, search/vector collections, and prompt context; application filtering alone is insufficient.
- ARCH-20 — Before any record class first persists, it must have A6-approved protection/retention/hold/export/delete/backup/erasure controls and native/API isolation proof; M2 cannot retroactively legitimize M0/M1 persistence.
- ARCH-21 — M0 governance is command-created by two distinct current Tenants owners with all role-specific co-approvals; direct data seeding, claims-only pilot authorization, and broad bootstrap workarounds are prohibited.
- ARCH-22 — M0 exposes only the four enumerated service-client classes and their exact tenant/resource scopes, command/query sets, expiries, delegation evidence, and audit facts; machine identities never inherit human roles.
- ARCH-23 — The M365 adapter must retain provider authenticity verdicts/headers and narrow mailbox authority, degrade per affected mailbox, and implement the five fixed outbound authority classes with execution-time membership/delegation/policy revalidation.
- ARCH-24 — Local Aspire composition must wire the canonical EventStore `statestore`, tenant-partitioned `chatbot-statestore`, `chatbot-workflow-statestore`, `chatbot-pubsub`, Keycloak, Dapr sidecars, production deny-by-default ACLs, and local-only mTLS-off ACLs; topology validation uses a live `aspire run`.
- ARCH-25 — Correction propagation is a hosted Dapr Workflow coordinated through EventStore writer/activity seams; the aggregate owns lifecycle truth, every affected store acknowledges, reads block/mark stale during correction, and M2 vector reindexing is idempotent and source-version guarded.
- ARCH-26 — SignalR is advisory metadata-only projection/progress notification. The tenant-grouped ChatBot hub emits bounded `ProjectionChangedDetail`-compatible nudges and the UI always re-queries the typed authoritative read model; payloads never carry trusted content/state.
- ARCH-27 — Runtime safety controls and limits use one durable versioned admission view consumed by gateway and workers; unavailable/stale/unknown controls fail closed. Fair scheduling partitions by tenant then work source, uses bounded renewable leases, and prevents poison/dead-letter work from starving other partitions.
- ARCH-28 — One operations-owned control worker enforces controls, notifications, escalation, and tenant-safe health/freshness/retry/queue/audit status; missing live evidence is reported `unmeasurable` or `unsupported`, never inferred.
- ARCH-29 — OpenTelemetry emission is mandatory. M2 SLO publication remains unsupported until every metric has an exact-candidate numeric target, window, budget, live provenance, accountable route, calibration, and passing burn test.
- ARCH-30 — `TaskIntentDetector`, `AssociationScorer`, and `ActionRiskClassifier` are independent versioned kernels. M0 association/risk behavior is deterministic; optional LLM explanations cannot change classification, and non-AI workflows survive provider outage.
- ARCH-31 — AI catalog membership, per-surface exposure, MCP tags, product allowlist, and owner executable targets are separate deny-by-default artifacts. The two product allowlist IDs remain exact, and `Project.AppendConversationMessage` stays A13-blocked until Conversations accepts an executable mapping.
- ARCH-32 — NetArchTest must mechanically prevent dependency/stage/host-wiring regrowth; differential conformance must compare normalized commands, event sequences, and state-store end state across production UI/API, CLI, and MCP adapters, including rejection and retry.
- ARCH-33 — Testing is layered across pure aggregate/contract tests, fail-closed/idempotency/isolation integration tests, Dapr/Aspire topology tests, and live Playwright accessibility/render tests. Integration acceptance asserts persisted state-store end state, not only status codes or mock calls.
- ARCH-34 — Replay M2 uses a separate credential-free composition root and gate-owned before/after invariance manifests across every protected production store/resource ledger; any missing/unreadable/added/changed resource, egress, or verifier failure is stop-ship.
- ARCH-35 — Deployable images use .NET SDK container support; the M2 target is Aspire K8s/AKS plus Helm with exact runtime digest and shared DataProtection key-ring evidence, or an explicit single-replica guard.
- ARCH-36 — DataProtection admission/cursor keys use application name `Hexalith.ChatBot`; production config must supply `ChatBot:DataProtection:KeyRingPath` or explicitly set `ChatBot:DataProtection:SingleReplicaOnly=true`.
- ARCH-37 — CI/release must build Release with warnings as errors, central package authority, enabled audit, individually executed test projects, non-vacuous topology/browser gates, exact source/evidence provenance, story-evidence integrity, and checkout-root-only non-recursive submodule initialization.
- ARCH-38 — Release inventory includes Contracts, Client, and Testing packages plus `hexalith-chatbot-server` and `hexalith-chatbot-ui` containers; semantic-release and Conventional Commits govern releases.
- ARCH-39 — M0→M1→M2 dependency order is mandatory. A5/A6/A13 block M0/M1; M2 revalidates them and additionally requires A10/A11. Missing, stale, mismatched, partial, or historical evidence cannot support pilot, compliance, tamper-evidence, or production claims.
- ARCH-40 — A material sibling contract/authority/identifier/topology/RBAC change triggers an architect-owned source re-check within five business days, recorded in the manifest/memlog with required downstream updates; inaccessible sources are blockers.
- ARCH-41 — JSON uses camelCase through shared `System.Text.Json` options, time uses UTC `DateTimeOffset` with tenant-local conversion only in presentation, and list queries use opaque cursor pagination rather than offset/limit.

### UX Design Requirements

- UX-DR1: Inherit visual behavior only through Microsoft Fluent UI v5 → Hexalith.FrontComposer → `DESIGN.md` → `EXPERIENCE.md`; do not define local palette, typography, radius, spacing, control, or semantic-role systems.
- UX-DR2: Every routable page must use the single FrontComposer shell with `FcPageLayout` and `FcPageHeader`; module-owned page chrome and legacy `.chatbot-page-header`, `.chatbot-page`, and `.chatbot-command-bar` patterns are prohibited.
- UX-DR3: Use FrontComposer/Fluent components for all equivalent controls and layout. Raw interactive `button`, `input`, `select`, and `textarea` elements, custom control clones, legacy v4/FAST tokens, and monospace `<dl>` primary-data dumps are prohibited.
- UX-DR4: Link and serve the scoped `Hexalith.ChatBot.UI.styles.css` bundle and verify on a live route that `.fluent-layout` computes to `display:grid`; source inspection or a static fixture cannot satisfy this check.
- UX-DR5: Group two or more sibling titled page sections in one `FluentAccordion`, primary item expanded, except a single primary grid/form/detail/workflow. On Association Review only, candidate group and decision bar remain jointly visible outside the accordion while complementary evidence is accordion content.
- UX-DR6: Keep the visual posture a quiet enterprise command workspace; source authority, human authority, intended effects, freshness, and audit outcomes stay close to the decision and the product must not resemble playful consumer chat.
- UX-DR7: Use inherited semantic color roles with visible text plus icon/border for every state. Functional text must meet 4.5:1, non-text UI/focus indicators 3:1, and all meaning must survive light, dark, and forced-colors modes.
- UX-DR8: Use inherited Fluent typography: `FcPageHeader` for page titles, compact section hierarchy, metadata for time/provenance/authority/policy/state/identity, and monospace only for stable IDs/codes.
- UX-DR9: Elevation and containment must separate active work, complementary evidence, one dialog/sheet layer, and transient feedback without turning generated output into an authority peer or detaching blocked state from its workflow.
- UX-DR10: Desktop supports persistent navigation and complementary panels; tablet stacks them; phone retains reading, governed request, status, safe confirm/reject/defer/approve/escalate actions and provides state-preserving handoff for dense administration/investigation.
- UX-DR11: Every surface must receive live-route acceptance for success, loading/empty, validation, unauthorized/redacted, degraded, retryable, terminal, keyboard/focus, responsive, light/dark/forced-colors, reduced-motion, English/French, and governed-command behavior.
- UX-DR12: WCAG 2.2 AA acceptance is per increment and combines automated, keyboard-only, and screen-reader checks; controls expose role, name, state, operation, and reachable unavailable reason, never tooltip-only explanation.
- UX-DR13: Focus follows visible order, is never obscured by persistent chrome at normal/200%/400% zoom, uses scroll margins, lands on a surface heading/first action, and returns correctly from dialogs/sheets without silently discarding work.
- UX-DR14: Reduced motion removes shimmer, row movement, streaming animation, and non-essential transitions; every progress condition retains textual status.
- UX-DR15: The page and language-of-parts metadata must follow locale; English/French have identical features, states, actions, disabled reasons, and accessible descriptions with locale-aware dates/numbers/plurals and no concatenated strings.
- UX-DR16: Implement `Project context header` from `FcPageHeader` to show only authorized Project/tenant context, current surface/state, and safe status; Project changes update context and announce once.
- UX-DR17: Implement `Conversation shell` from `FcPageLayout`, `FluentStack`, and `FluentGrid`, preserving the relationship among Project context, stream, composer, complementary panels, selection, focus, and scroll.
- UX-DR18: Implement `Conversation stream` from Fluent cards/text groups, ordering attributed human, external, mailbox, AI, surface, worker, and system events; system decisions must not masquerade as anonymous chat.
- UX-DR19: Implement `Composer/action entry` from `FluentTextArea`/`FluentButton`, visibly separating user message and AI request, using the shared command spine, showing optimism only after admission, and keeping Stop/Cancel stable and reachable.
- UX-DR20: Implement `Actor badge` as text+icon for human, external party, service client, AI, worker, CLI, MCP, and mailbox origins; unresolved actors show a safe unresolved state without inferred identity.
- UX-DR21: Implement `Message classification` as an accessible text+icon `informational|actionable` badge attached to its message, with actionable task-intent state and review/capture/dismiss affordances.
- UX-DR22: Implement `Source evidence` as expanded-by-default accordion/card content; every reference shows permitted source identity/content, redaction, timestamp, and freshness and remains authoritative over AI interpretation.
- UX-DR23: Implement `AI summary` as collapsed-by-default content visibly labelled `AI summary`, structurally distinct from evidence, with model/version/time/source-ID provenance preceding the content and keyboard-safe disclosure.
- UX-DR24: Implement `Why this project` as labelled facts for signal class, matched value, confidence/band, decision actor/time, and correction links, with announced disclosure state and focus return.
- UX-DR25: Implement one `Evidence freshness` chip per evidence reference, showing timestamp and `fresh|stale|expired`; expiry announces once and disables the affected decision with `evidence-expired` until refresh.
- UX-DR26: Implement `Task intent review` with the full source, ≤280-character summary, action kind, evidence offsets/excerpts, detector version, confidence, time/state, and governed convert/dismiss dispositions.
- UX-DR27: Implement `Attachment row` as a labelled data-grid row for capture/storage, scan/quarantine, folder, duplicate/retry, retention, and AI-context eligibility; MVP exposes no general upload affordance.
- UX-DR28: Implement `Association candidate group` as one named Fluent radiogroup/Tab stop; arrows change selection and announce position/count without committing, each option describes safe evidence/confidence, and unsafe candidates never render.
- UX-DR29: Implement `Association decision bar` as a persistent unit repeating the selected safe Project in its accessible description with confirm, reject-all, defer, and escalate; invalid confirm focuses the error summary.
- UX-DR30: Implement `Action classification` with prominent user disposition `allowed-read-only|approval-required|denied|unsupported` and subordinate labelled internal classifier output/version/input tuple; do not conflate disposition and classifier result.
- UX-DR31: Implement `AI proposal panel` as visibly pending, programmatically linking source request, Project/context package, classification, and approval details; it cannot resemble completed work.
- UX-DR32: Implement `Approval authority and effects` with requester/origin, Project, command/allowlist version, files/redaction/freshness, recipients, sender/delegation, classifier tuple, policy snapshot, reversibility, post-state, audit events, and proposal/operation identity.
- UX-DR33: Implement `Approval controls` for approve/reject/revise/cancel. Unavailable approval remains focusable with an associated safe explanation, and submission revalidates authority, policy, evidence, allowlist, and effects.
- UX-DR34: Implement `Correction progress` with predecessor/successor, `Correcting|Correction-delayed`, acknowledged/remaining stores, estimate, owner, next action, P2 escalation, and visible AI-context block until completion.
- UX-DR35: Implement `Bounded admin scope` with badges/data grids distinguishing aggregate see-only, queue-operate, mailbox, policy, compliance, and per-Project powers; aggregate visibility never grants detail or mutation.
- UX-DR36: Implement `Two-person approval` as ordered proposer/distinct-approver steps showing changed values, scope, justification, policy version, expiry/conflict, and audit link; self-approval is visibly unavailable with a reason.
- UX-DR37: Implement `Shared operation status` with operation/idempotency identity, canonical state/reason, origin, attempt count/ceiling, retry eligibility, partial output, prior outcome, correlation, and pending duplicate-submit prevention.
- UX-DR38: Implement `Queue row` as a dense labelled data-grid row with state, age, owner/assignee, risk/confidence, freshness, next action, retry count, and terminality; item detail remains redacted without Project authority.
- UX-DR39: Implement `Operational SLO dashboard` with metric, numeric target or missing-support reason, window, budget, alert threshold, calibration, tenant scope, freshness, owner, and `within-budget|approaching|exhausted|unsupported`; unsupported blocks the relevant M2 claim.
- UX-DR40: Implement `Audit timeline` as a filterable attributed sequence connecting source, actor, candidates/decisions, policy, approval, command, correction, replay, redaction, and outcome; replay is labelled and excluded by default from production-completeness views.
- UX-DR41: Implement `Inbound authenticity and sender authority` with provider-supplied DMARC/DKIM/SPF, header discrepancies, external-sender state, delegate/`principal_for`, authority class, membership/delegation evidence, and revalidation status without claiming ChatBot re-verification.
- UX-DR42: Implement `Retention and export request` with requested data classes, authorized scope, retention/hold/redaction limits, owner, progress, and completed/partial/blocked outcome; exposure requires explicit authorized human confirmation and must not promise deletion contrary to immutable-audit handling.
- UX-DR43: Implement `Redacted support bundle` with a preview of included correlation/state/reason data and explicitly excluded restricted/secrets data; creation requires redaction validation and authorized confirmation, and external sharing is approval-required.
- UX-DR44: Implement `Blocked state` as a persistent, existence-neutral message with stable safe code, short reason, owner where applicable, and one safe action; never expose suppressed candidates or confirm forbidden resources.
- UX-DR45: Implement `Status toast/banner` only for deduplicated transition feedback; persistent state stays inline and global banners are reserved for whole-application impact.
- UX-DR46: Implement `Busy region` as a layout-matched `FluentSkeleton` replacement with `aria-busy` on the same node, focus preservation/relanding, no historical-content announcement, and no shimmer under reduced motion.
- UX-DR47: Implement `Error summary` before its form/review unit, focus it on invalid submission, link it to field/decision errors, and preserve valid input, selection, draft, and filters.
- UX-DR48: Implement `Review dialog/sheet` as one modal layer using inherited containment, focus trap/return, and non-destructive Escape; unsaved edits require confirmation.
- UX-DR49: Implement `Queue filter bar` as a compact Fluent toolbar with server-side filters, pagination ≤100, active-filter summary/count, stable focus/selection on refresh, and labelled small-screen reflow.
- UX-DR50: Enforce the governed-action disposition table: eligible safe subtype → `allowed-read-only`; any boundary effect or otherwise-safe indeterminate classification → reviewable `approval-required`; authorization/evidence/audit/allowlist failure → existence-neutral `denied`; unsupported product operation → `unsupported` with no effect.
- UX-DR51: Immediately before approved execution/send, revalidate actor/reviewer authority, tenant/Project, files/redaction/freshness, recipients/sender/delegation, command/allowlist, policy, effects, proposal revision, idempotency, and audit; drift blocks and requires a fresh proposal/decision.
- UX-DR52: Approval queues prioritize risk × affected authority × age, group only items with identical frozen security fields and separate decisions/audit, exclude irreversible/external/file/tool/on-behalf one-click batching, alert at >25 open items, and digest excess notifications without hiding work.
- UX-DR53: UI/CLI/MCP parity presentation may differ, but every parity operation must display/return the same safe ordered candidates, normalized decision, authorization/redaction, lifecycle state, operation identity, retry/conflict semantics, immutable origin, and audit result.
- UX-DR54: Progressive AI output is document content with `aria-live=off`; a separate deduplicated status region announces only generating/complete/stopped/failure. Stop/Cancel never steals focus, partial output is uncommitted, and retry links a new immutable attempt.
- UX-DR55: New conversation/audit updates must not force scroll while a user reads history; provide a keyboard-reachable “new updates” action. Single-character shortcuts remain disabled/remappable and never intercept text entry.
- UX-DR56: S1 Project Workspace/Conversation must cover cold/no-project/empty/active states; classification, intent, generation, attachments, proposal/operation, correction, AI outage, degradation, and unauthorized/redacted states on live routes.
- UX-DR57: S2 Association Review must cover loading/no-safe-candidate/below-threshold/conflict/scorer-error review, selection, stale/expired evidence, validation, confirm/reject/defer/escalate, retry/quarantine/terminal, and suppressed-candidate safety.
- UX-DR58: S3 AI Action Review and S6 Outbound Approval must cover complete authority/effect/freshness context, insufficient authority, revalidation drift, every human decision, execution progress/outcome, denied/unsupported states, and exact-once send.
- UX-DR59: S4 Correction Surface must cover eligibility/rationale, stale revision, progress/acknowledgements, delayed/failed invalidation, permission denial, retry/escalation, completion, and continued AI block until safe.
- UX-DR60: S5 Tenant Administration must cover bounded scopes, loading/empty, policy/mailbox validation, proposal and distinct approval lifecycle, conflict/expiry/rejection/cancellation, active version, degraded permissions, rollback-safe non-destructive change, and terminal failure.
- UX-DR61: S7 Cross-surface Attribution must show current/stale parity version, normalized operation, disposition, pending/prior/conflict status, immutable origin, safe redaction, adapter-parity failure, and CLI/MCP recovery guidance.
- UX-DR62: S8 Operational Dashboards must present health/queue/SLO freshness, owner, alert/escalation and supported/unsupported state without using missing evidence as inferred readiness.
- UX-DR63: S9 Compliance Investigation must support authorized filters, selected-event reconstruction, projection pending, source/AI distinction, redaction, replay inclusion control, correction trace, retention/export/support status, terminal outcome, and escalation.
- UX-DR64: S10 Admin Queue Operations must preserve aggregate/opaque scope, stable filters, claim/assign and authorized retry/requeue/quarantine/dismiss semantics, conflict/degraded/terminal states, and no unauthorized Project-detail elevation.
- UX-DR65: Microcopy must be factual, specific, action-oriented, and existence-neutral; every blocked/degraded/failed/denied state uses a stable code, ≤80-character headline, one safe sentence, and a safe next action while precise causes remain in authorized audit.
- UX-DR66: Touch-primary actions target 44×44 CSS pixels where possible; compact controls meet at least 24×24 or equivalent spacing, and destructive/approval actions are never compact-only on phone/tablet.
- UX-DR67: Copy, export, transcript, read-aloud, retention result, support bundle, accessible names, and descriptions must apply the same redaction as the visible surface and contain no hidden source text.
- UX-DR68: MVP UI must not expose scheduled/file-addition automation, general user upload, ungoverned freeform execution, cross-tenant candidates, permissive authenticity, or a policy control that downgrades the six mandatory-approval effects.
- UX-DR69: Prohibit hover-only critical actions, infinite operational lists, stacked modals, decorative assistant persona, color/motion/toast-only meaning, and forced-scroll updates.
- UX-DR70: The separate Fluent-control and FrontComposer-layout governance tests must be non-vacuous, shrink-only, and finish with empty offender lists; each surface owns its first live acceptance, and the final cross-surface browser suite is regression confirmation only.

### FR Coverage Map

FR1: Epic 2 — Capture authorized mailbox events.

FR2: Epic 2 — Preserve complete source-message identity and attachment references.

FR3: Epic 2 — Associate email using deterministic evidence.

FR4: Epic 2 — Route ambiguous association to human review.

FR5: Epic 2 — Review safe candidates with evidence, confidence, reasons, and consequences.

FR6: Epic 2 — Confirm, reject, defer, or mark association for review.

FR7: Epic 2 — Correct a prior Project association.

FR8: Epic 2 — Record association, correction, rejection, defer, retry, and skip decisions.

FR9: Epic 2 — Configure and govern association rules and thresholds needed by the complete association outcome; Epic 7 supplies the bounded policy-administration surface.

FR10: Epic 2 — Retain original email context in every unresolved or terminal association outcome.

FR11: Epic 2 — Expose machine-readable deterministic reasons and confidence inputs.

FR12: Epic 2 — Compare candidate evidence side by side.

FR13: Epic 2 — Resolve email participants to tenant-scoped Parties.

FR14: Epic 2 — Surface unresolved participants for review.

FR15: Epic 2 — Accept external-party email context without an MVP portal.

FR16: Epic 2 — Authorize before exposing Project workflow resources.

FR17: Epic 2 — Block unresolved/unauthorized actors from Project effects.

FR18: Epic 2 — Apply governed mailbox participation rules; Epic 7 supplies their bounded administration.

FR19: Epic 2 — Apply scoped service-client access needed by intake; Epic 7 supplies the full grant administration.

FR20: Epic 2 — Record consent/lawful-basis metadata before applicable intake and processing.

FR21: Epic 3 — View email-derived Project conversation messages.

FR22: Epic 3 — Render associated email, participants, attachments, decisions, approvals, failures, and AI outcomes.

FR23: Epic 3 — Inspect Project-association evidence and correction provenance.

FR24: Epic 3 — View current workflow statuses and next actions in conversation context.

FR25: Epic 3 — Keep conversation context isolated by tenant and Project.

FR26: Epic 3 — Distinguish informational and actionable intent without implying action risk.

FR27: Epic 3 — Distinguish source evidence from AI summaries.

FR28: Epic 3 — Preserve visible human-review history.

FR28a: Epic 13 — Submit governed Project-scoped chat through CommandGateway.

FR28b: Epic 13 — Show admission disposition before AI work begins.

FR28c: Epic 13 — Stream safe responses with provenance and uncommitted partial-state treatment.

FR28d: Epic 13 — Support distinct stop and cancellation lifecycles with expected revision.

FR28e: Epic 13 — Convert boundary-crossing chat requests into mandatory-approval proposals.

FR28f: Epic 13 — Provide immutable chat attempts, exact retry/race behavior, safe failures, and audit.

FR29: Epic 3 — Capture mailbox attachments.

FR30: Epic 3 — Store attachments in governed Project folders.

FR31: Epic 3 — Inspect attachment capture/storage status.

FR32: Epic 3 — Prevent unauthorized attachment metadata/content access.

FR33: Epic 3 — Build explicitly authorized, auditable AI context packages.

FR34: Epic 3 — Represent the full attachment state family.

FR35: Epic 4 — Detect versioned task/action intent with complete source evidence.

FR36: Epic 4 — Review task intent before governed action.

FR37: Epic 4 — Convert intent into a linked governed proposal.

FR38: Epic 4 — Record terminal non-actionable/duplicate/handled/out-of-scope dispositions.

FR39: Epic 4 — Classify requests with an independent versioned risk classifier.

FR40: Epic 4 — Allow only declared authorized low-risk assistance.

FR41: Epic 4 — Enforce non-downgradable approval for all six boundary effects.

FR42: Epic 4 — Review complete proposal authority/effect context and decide safely.

FR43: Epic 4 — Execute only current allowlisted commands; Epic 10 governs later allowlist versions.

FR44: Epic 4 — Inspect every AI proposal/decision/execution outcome.

FR45: Epic 4 — Preview outbound, file, command, and AI changes before effect.

FR46: Epic 4 — Safely refuse requests outside policy, authority, or command scope.

FR47: Epic 6 — Create an authorized outbound Project-email draft.

FR48: Epic 6 — Enforce the five sender-authority classes.

FR48a: Epic 6 — Record provider-supplied DMARC/DKIM/SPF verdicts.

FR48b: Epic 6 — Inspect authenticity headers and record discrepancies safely.

FR48c: Epic 6 — Preserve delegate and principal authority evidence.

FR48d: Epic 6 — Apply strict/paranoid external-sender posture.

FR49: Epic 6 — Require human approval before outbound boundary crossing.

FR50: Epic 6 — Preserve frozen proposal/approval/send evidence.

FR51: Epic 7 — Configure mailbox integration and monitored patterns.

FR52: Epic 7 — Govern low-risk subtype enablement and approval routing without allowlist/approval weakening; Epic 10 governs the command catalog itself.

FR53: Epic 7 — Review mailbox permission and degradation status.

FR54: Epic 12 — Investigate association, approval, command, and risky-AI history.

FR55: Epic 1 — Atomically audit every durable mutation and separately audit sensitive attempts; Epic 12 adds investigation/retention hardening.

FR55a: Epic 1 — Enforce first-use store isolation as a cross-product foundation.

FR56: Epic 12 — Query authorized audit by all declared axes.

FR57: Epic 1 — Apply cross-surface, existence-neutral redaction.

FR58: Epic 12 — Operate tenant retention, export, and deletion workflows.

FR59: Epic 1 — Propagate correlation through every boundary.

FR60: Epic 2 — Preserve association/correction source evidence with retention and redaction; Epic 12 consumes it for investigation.

FR61: Epic 1 — Preserve immutable policy snapshots with governed decisions; Epics 7 and 10 administer their versions.

FR62: Epic 2 — Append non-authoritative workflow rationale without altering canonical decisions.

FR63: Epic 2 — Supersede reversible association decisions while preserving history.

FR64: Epic 2 — Suppress duplicate mailbox delivery and Project artifacts.

FR65: Epic 2 — Recover intake/association/correction through exact family retry/successor rules.

FR66: Epic 2 — Surface terminal and non-terminal intake/association failure states.

FR67: Epic 11 — Expose complete health, queue, failure, and audit-projection status; earlier epics expose the minimum status required to operate their own workflows.

FR68: Epic 1 — Establish universal fail-closed behavior when governed prerequisites are unresolved.

FR69: Epic 8 — View and manage human-work queues.

FR70: Epic 8 — Claim or assign review items safely.

FR71: Epic 8 — Show the responsible next human action.

FR72: Epic 8 — Notify authorized users about actionable workflow states.

FR73: Epic 8 — Configure bounded notification routing and escalation.

FR74: Epic 9 — Enforce disable/quarantine/rate-limit for the four closed subject classes.

FR75: Epic 9 — Enforce per-tenant rate limits, quotas, and circuit breakers.

FR75a: Epic 7 — Implement the closed admin-role hierarchy and command-only grants.

FR75b: Epic 7 — Separate aggregate see-only scope from per-Project detail.

FR75c: Epic 7 — Bound partition operations and require Project authority for item mutation.

FR75d: Epic 7 — Enforce policy-admin row scope and two-person approval.

FR75e: Epic 7 — Enforce mailbox-admin configuration-only authority.

FR75f: Epic 7 — Enforce redacted compliance scope and independent retention approval.

FR75g: Epic 7 — Audit every administrative action without bypass.

FR76: Epic 8 — Present safe enabled/disabled/hidden review actions and guidance.

FR77: Epic 1 — Establish the versioned existence-neutral message catalog used by every epic.

FR78: Epic 8 — Filter, sort, and prioritize queues safely.

FR79: Epic 8 — Expose stale, waiting, blocked, and escalation states.

FR80: Epic 1 — Establish retrievable long-running operation status; Epic 5 exposes it through CLI/MCP.

FR81: Epic 1 — Deliver the first governed UI operation.

FR81a: Epic 1 — Establish the one atomic shared command spine.

FR82: Epic 5 — Deliver CLI parity for the singular M1 set.

FR83: Epic 5 — Deliver governed MCP parity without allowlist expansion.

FR84: Epic 5 — Prove equivalent cross-surface authorization and transitions.

FR85: Epic 5 — Preserve immutable origin for every production surface/actor.

FR86: Epic 5 — Verify canonically normalized adapter parity through contracts.

FR87: Epic 10 — Complete the family-specific lifecycle catalog; earlier epics deliver complete subsets for their own workflows.

FR88: Epic 10 — Validate every governed transition against its family model.

FR89: Epic 10 — Reject and audit invalid transitions consistently.

FR90: Epic 1 — Establish stable operation/decision/resource identities and concurrency guards.

FR91: Epic 2 — Separate immutable sources from rebuildable association projections.

FR91a: Epic 2 — Propagate correction through every affected store and production workflow.

FR92: Epic 1 — Establish versioned consented/redacted/synthetic evaluation datasets; later qualification consumes them.

FR93: Epic 1 — Establish tenant-scoped sandbox fixtures for governed workflow validation.

FR94: Epic 11 — Publish measurable operational outcomes through supported observability surfaces.

FR95: Epic 12 — Simulate/replay safely without production effect.

FR95a: Epic 12 — Prove credential, adapter, egress, audit, and production-store replay isolation.

FR96: Epic 2 — Reuse correction evidence only when policy allows and influence remains explainable.

## Epic List

### Epic 1: First Safe Governed Action & Command Spine

A user can complete one tenant-bound, policy-aware, fail-closed, idempotent, and atomically audited action through a runnable foundation that safely enables every later workflow.

**FRs covered:** FR55, FR55a, FR57, FR59, FR61, FR68, FR77, FR80, FR81, FR81a, FR90, FR92, FR93.

**Implementation notes:** Begin with the architecture-mandated canonical sibling-module scaffold and runnable local topology. Establish the OpenAPI contract spine, typed Client, shared CommandGateway, canonical audit/idempotency boundary, safe message catalog, tenant isolation, operation status, and initial qualification fixtures. Technical Enabler TE-1 remains outside the product-epic count.

### Epic 2: Email Intake, Association & Production Correction

Contributors can ingest authorized project email, resolve participants and Project association from safe evidence, handle ambiguity and duplicates, correct mistakes, and recover correction propagation in production.

**FRs covered:** FR1–FR20, FR60, FR62–FR66, FR91, FR91a, FR96.

**Implementation notes:** Deliver one controlled M365/Exchange mailbox path, deterministic scoring, complete family lifecycle/retry semantics, safe candidate review, source evidence, participant resolution, projection rebuild, and hosted Dapr Workflow correction coordination without depending on later observability work.

### Epic 3: Project Conversation Context, Files & Attachments

Contributors can see email-derived Project conversation context, participants, decisions, governed attachments, provenance, and current workflow status, and can prepare explicitly authorized context packages for later AI use.

**FRs covered:** FR21–FR28, FR29–FR34.

**Implementation notes:** Build the ChatBot-owned conversation projection and attachment/Folders adapter end to end. Decompose FR22 into seven independently testable rendering outcomes while consolidating shared projection/component work to avoid repeated file churn.

### Epic 4: Governed AI Action Mediation

Users can detect and review actionable intent, request AI help, safely receive allowed read-only assistance, and approve or refuse every boundary-crossing action before exact-once governed execution.

**FRs covered:** FR35–FR46.

**Implementation notes:** Keep task-intent, association, and risk kernels independently versioned; enforce the six non-downgradable approval effects; package scoped context; qualify the exact allowlist target; and cover proposal invalidation after corrected association.

### Epic 5: Cross-Surface Parity — CLI & MCP

Developers and AI/automation clients can perform the singular governed workflow parity set through CLI and MCP with the same authorization, transitions, redaction, idempotency, status, and audit outcomes as UI/API.

**FRs covered:** FR82–FR86.

**Implementation notes:** Production CLI and MCP adapters wrap only the generated typed Client. Differential conformance compares normalized commands, event sequences, and state-store end state; it replaces M0 shims and treats any adapter-stage bypass as an invariant violation.

### Epic 6: Outbound Communication & Inbound Authenticity

Authorized users can understand inbound authenticity and safely draft and send exact approved Project communication under current sender, mailbox, recipient, file, and Project authority.

**FRs covered:** FR47, FR48, FR48a–FR48d, FR49, FR50.

**Implementation notes:** M0 delivers the provider verdict/header/delegation/external-sender floor; M1 adds all five outbound authority classes, frozen content, execution-time revalidation, mandatory approval, exact-once send, and typed fail-closed mismatch behavior.

### Epic 7: Tenant Policy & Bounded Administration

Tenant, policy, mailbox, and compliance administrators can configure only their authorized scopes through immutable, independently approved, auditable governance versions without becoming Project superusers.

**FRs covered:** FR51–FR53, FR75a–FR75g.

**Implementation notes:** Implement the closed Tenant Policy Schema, command-created governance/bootstrap, bounded aggregate versus per-item visibility, distinct-admin approval, mailbox scope, service-client grants, and no admin/debug bypass.

### Epic 8: Review Operations, Notifications & Escalation

Reviewers can find, prioritize, claim, and resolve work queues and receive bounded, actionable notifications and escalation guidance without leaking Project detail or creating approval fatigue.

**FRs covered:** FR69–FR73, FR76, FR78, FR79.

**Implementation notes:** Deliver persistent queue state, safe actions/disabled reasons, stable filters/pagination, assignment conflicts, owner/next-action guidance, notification routing/ceilings/digests, and safe grouping while leaving actual runtime subject control to Epic 9.

### Epic 9: Runtime Governance Control Plane

Authorized administrators can actually disable, quarantine, and rate-limit mailbox sources, service clients, AI actors, and command capabilities, then safely release controls after independent approval and remediation.

**FRs covered:** FR74, FR75.

**Implementation notes:** Consolidate the durable control projection, fail-closed admission/worker enforcement, fair scheduling, bounded leases, periodic evaluator, recovery, audit, redaction, and all twelve subject/action cells so no control remains inert pending a future epic.

### Epic 10: Command Allowlist & Lifecycle Governance

Policy and security administrators can govern the immutable command allowlist and verify that each workflow family uses its own valid lifecycle, conflict, successor, and retry semantics.

**FRs covered:** FR87–FR89; governance extension of FR43, FR52, and FR61.

**Implementation notes:** Keep product catalog, surface exposure, MCP tags, AI allowlist, and owner mappings distinct and deny-by-default. Earlier epics deliver complete lifecycle subsets; this epic completes administrative allowlist and cross-family governance rather than repairing them retroactively.

### Epic 11: Operational Dashboards & Observability

Operators can see tenant-safe health, queue, latency, saturation, SLO/error-budget support, degradation scope, ownership, alerts, and safe recovery guidance after the governed behavior itself already works.

**FRs covered:** FR67, FR94.

**Implementation notes:** Deliver OpenTelemetry-backed M2 operational views, freshness and `unsupported` states, candidate-bound A11 evidence, non-invasive synthetics, alert routing/burn tests, and cross-tenant/noisy-neighbor validation without becoming the activation mechanism for earlier workflows.

### Epic 12: Audit, Compliance Investigation & Recovery

Compliance and operations users can reconstruct governed activity, safely query/export/erase retained data, distinguish evidence authorities, rebuild projections, simulate without production effects, and qualify recovery.

**FRs covered:** FR54, FR56, FR58, FR95, FR95a.

**Implementation notes:** Extend the already-atomic canonical audit into authorized investigation, A6-governed data-subject workflows, WORM/checkpoint verification, replay-only composition/invariance, and A10 recovery qualification. Diagnostic, story-completion, and retained operational evidence remain disjoint.

### Epic 13: Governed Interactive Workspace & UI Conformance

Users can converse and interrupt AI work through live, accessible, responsive, localized, Fluent/FrontComposer-composed routes while all governed Project surfaces preserve authority, state, evidence, and safe actions.

**FRs covered:** FR28a–FR28f; UX realization and regression protection for the affected surface requirements owned by Epics 2–12.

**Implementation notes:** Consolidate the former overlapping UI remediation chain into surface-owned outcomes: shell/foundation, Project conversation/composer/streaming, association review, AI/outbound review, tenant/review administration, dashboards, compliance investigation, and live cross-surface confirmation. Earlier epics retain their functional surfaces; this epic adds governed chat and closes the unified visual/accessibility contract without becoming their deferred implementation dependency.

### Technical Enabler TE-1: DomainService SDK Host Adoption

Tracked outside the 13 product epics because it is a platform/host refactor rather than a standalone user outcome. It provides the EventStore pre-commit admission hook, migrates ChatBot to DomainService SDK seams, retires module-owned production hosting boilerplate, retains only the ADR-scoped local AppHost shim, and proves unchanged product behavior.

## Epic 1: First Safe Governed Action & Command Spine

A user can complete one tenant-bound, policy-aware, fail-closed, idempotent, and atomically audited action through a runnable foundation that safely enables every later workflow.

### Story 1.1: Scaffold the Runnable Canonical Module Foundation

As a product delivery team,
I want a runnable ChatBot module following the canonical sibling-module convention,
So that governed capabilities can be implemented and verified on a consistent foundation.

**Acceptance Criteria:**

**Given** a clean checkout with only root-declared dependencies initialized
**When** the solution structure is inspected
**Then** `Hexalith.ChatBot.slnx` includes `Contracts`, `Client`, `Server`, `Testing`, `UI`, `Workers`, `Cli`, `Mcp`, and the ADR-scoped `AppHost` projects
**And** mirrored xUnit v3 test projects exist for contracts, server, architecture, conformance, integration, and browser acceptance.

**Given** the repository build configuration
**When** package and compiler settings are inspected
**Then** .NET 10, C# 14, nullable analysis, and warnings-as-errors are enabled
**And** `.editorconfig`, `global.json`, `nuget.config`, and a version-free `Directory.Packages.props` importing the shared Builds catalog are present.

**Given** the EventStore dependency
**When** `.gitmodules` and initialized modules are inspected
**Then** `Hexalith.EventStore` is declared only as a root submodule
**And** no nested or recursive submodule initialization is required.

**Given** the project dependency graph
**When** architecture tests execute
**Then** `Contracts` remains infrastructure-independent, `Client` depends on contracts, surfaces depend only on the typed client, and governance internals remain inside `Server`
**And** the tests reject forbidden dependency directions and multiple documented types in one C# file.

**Given** the local-development AppHost
**When** `aspire run` starts successfully
**Then** it wires the canonical EventStore `statestore`, tenant-partitioned ChatBot state stores, workflow state store, pub/sub, Keycloak, and required Dapr sidecars
**And** topology validation proves the declared resource names and health endpoints are live.

**Given** production host boundaries
**When** deployable projects are inspected
**Then** no standalone production Aspire or ServiceDefaults project exists
**And** the retained AppHost is explicitly limited to local development.

**Given** the solution and all test projects
**When** each project is restored and built in Release configuration
**Then** compilation succeeds with warnings treated as errors
**And** every test project can be invoked independently without a vacuous pass.

**Given** release metadata
**When** the package and container inventory is inspected
**Then** it identifies Contracts, Client, and Testing packages plus Server and UI container targets
**And** it records A5, A6, and A13 as open blockers without claiming M0/M1, pilot, compliance, or production readiness.

**Requirements:** ARCH-1, ARCH-2, ARCH-5, ARCH-6, ARCH-8, ARCH-24, ARCH-33, ARCH-37, ARCH-38, ARCH-39.

### Story 1.2: Publish the OpenAPI Contract Spine and Typed Client

As an authorized surface developer,
I want one generated client derived from the canonical HTTP contract,
So that every product surface starts from the same stable command, status, and failure semantics.

**Acceptance Criteria:**

**Given** the ChatBot HTTP boundary
**When** its public contract is inspected
**Then** OpenAPI 3.1 is the sole wire-contract source for command submission and operation-status retrieval
**And** hand-authored competing request or response models are rejected by architecture tests.

**Given** the canonical contract
**When** client generation runs
**Then** it produces a compiling typed Client with command submission, status retrieval, cancellation-token, correlation, and idempotency support
**And** the generated Client contains no Server or infrastructure dependency.

**Given** any serialized public request or response
**When** contract round-trip tests execute
**Then** JSON uses the shared camelCase options, time uses UTC `DateTimeOffset`, and stable identifiers use their declared ULID/resource formats
**And** list contracts use opaque cursor pagination rather than offset/limit.

**Given** a command-submission response
**When** admission succeeds, remains pending, or fails safely
**Then** the contract can express operation identity, canonical state, safe reason code, correlation, retry eligibility, and any prior stored outcome
**And** no raw exception, payload, PII, secret, or restricted-resource detail is part of the contract.

**Given** a non-success API outcome
**When** it is represented on the wire
**Then** it uses a metadata-only RFC 9457 problem contract with a versioned safe message code
**And** its schema remains existence-neutral for unauthorized resources.

**Given** a committed OpenAPI or generated-client change
**When** the contract synchronization check runs
**Then** generated output, exposure metadata, and client signatures match the canonical specification
**And** stale or manually modified generated artifacts fail the check.

**Given** the conformance test project
**When** the initial parity oracle executes against representative contract fixtures
**Then** it performs non-vacuous serialization and normalization assertions
**And** it does not claim runtime adapter parity before those adapters are implemented.

**Requirements:** FR80 (contract foundation), FR90 (contract foundation), ARCH-15, ARCH-16, ARCH-32, ARCH-33, ARCH-41, NFR32, NFR33, NFR36, NFR70.

### Story 1.3: Bind Every Request to Trusted Tenant and Actor Context

As an authorized tenant user or machine client,
I want every request evaluated against trusted current authority,
So that I can access only the tenant, Project, role, and resource scope granted to me.

**Acceptance Criteria:**

**Given** an authenticated request
**When** ChatBot creates its request context
**Then** tenant, actor type, actor identity, role, Project, and resource authority derive from trusted server and current owner evidence
**And** caller-supplied tenant, role, Project, or resource claims cannot broaden that scope.

**Given** valid current owner evidence and sufficient authority
**When** the actor requests an allowed resource or operation
**Then** authorization succeeds only for the bound tenant and permitted Project/resource
**And** the normalized authorization outcome is available to the typed boundary without copying owner PII into domain records.

**Given** missing, stale, malformed, unavailable, or contradictory identity or owner evidence
**When** a command or query is authorized
**Then** the request fails closed with a typed safe outcome
**And** no restricted data or mutation is returned or performed.

**Given** an actor without authority to a tenant, Project, or resource
**When** the actor submits an otherwise valid request
**Then** the response is existence-neutral and redacts Project names, evidence, files, audit details, and tenant details
**And** equivalent missing and forbidden resources cannot be distinguished from the public response.

**Given** a human, service client, worker, mailbox event, CLI client, MCP client, or AI actor
**When** its authorization context is constructed
**Then** its immutable origin and actor class remain distinct
**And** machine identities never inherit a human role or global administrative bypass.

**Given** cached authorization, policy, or identity evidence
**When** ordinary freshness or explicit revocation limits are exceeded
**Then** ordinary cache age never exceeds five minutes and revocation recognition never exceeds 60 seconds
**And** automated revocation tests prove access is denied within those limits.

**Given** the negative authorization test matrix
**When** it exercises UI/API, CLI, MCP, worker, mailbox-event, service-client, and AI origins across two tenants
**Then** unauthorized reads and mutations are denied with equivalent normalized outcomes
**And** no candidate, evidence, file, summary, prompt, telemetry, or audit detail crosses the tenant boundary.

**Requirements:** FR57, FR68, ARCH-10, ARCH-11, ARCH-12, ARCH-19, NFR1, NFR2, NFR6, NFR7, NFR8, NFR11, NFR38, NFR67.

### Story 1.4: Return Versioned Safe Outcomes

As a user whose request cannot proceed normally,
I want a clear, safe explanation and next action,
So that I can recover or seek help without restricted information being exposed.

**Acceptance Criteria:**

**Given** the shared safe-message catalog
**When** a catalog entry is defined
**Then** it has a stable code, catalog version, headline of no more than 80 characters, one safe explanatory sentence, terminality, and supported next actions
**And** any named owner or responsible role reveals no unauthorized resource existence.

**Given** a refused, blocked, degraded, failed, denied, waiting, retryable, or unsupported outcome
**When** it crosses a public boundary
**Then** it resolves through the versioned catalog rather than raw exception text
**And** the response supplies only safe retry, escalate, dismiss, or request-access actions applicable to that outcome.

**Given** an unknown, malformed, or untranslated message code
**When** the outcome is rendered
**Then** the system uses a deny-safe generic catalog entry
**And** it neither exposes internal diagnostics nor invents a successful or retryable state.

**Given** the English and French catalogs
**When** catalog completeness tests run
**Then** both locales contain equivalent codes, states, actions, disabled reasons, and accessible descriptions
**And** locale-aware output uses complete messages rather than concatenated fragments.

**Given** a safe outcome displayed in the UI
**When** it is rendered in light, dark, forced-colors, reduced-motion, keyboard-only, or screen-reader use
**Then** state is conveyed through visible text plus semantic icon or border rather than color or motion alone
**And** the safe next action and any unavailable reason are keyboard reachable and programmatically associated.

**Given** persistent workflow state and transition feedback
**When** a user-visible status changes
**Then** persistent state remains inline and deduplicated toast/banner feedback is used only for the transition
**And** a global banner is reserved for whole-application impact.

**Given** automated response, telemetry, localization, and rendered-output scans
**When** representative internal exceptions, secrets, identifiers, and restricted data are injected
**Then** the raw leakage count is zero
**And** precise causes remain available only through a separately authorized diagnostic path.

**Requirements:** FR57, FR77, ARCH-16, NFR2, NFR4, NFR10, NFR38, NFR39, NFR40, NFR62, UX-DR7, UX-DR15, UX-DR44, UX-DR45, UX-DR62, UX-DR65, UX-DR67.

### Story 1.5: Establish Stable Operation Identity and Concurrency Contracts

As an authorized user submitting governed work,
I want a durable operation identity and deterministic conflict semantics,
So that I can safely retrieve the outcome without causing the logical action twice.

**Acceptance Criteria:**

**Given** a new externally visible operation
**When** its contract is created
**Then** it receives a lifetime-stable operation ID, stable resource identifiers, immutable origin, creation time, correlation context, operation class, and semantic-equivalence definition
**And** none of those identities is reused for a different logical operation.

**Given** a command targeting versioned state
**When** it is submitted
**Then** it carries the expected revision or the specifically approved equivalent concurrency guard
**And** missing or malformed concurrency input is rejected before mutation with a typed safe outcome.

**Given** two non-equivalent operations competing for the same revision or decision slot
**When** they are processed concurrently
**Then** first commit wins and the other operation receives a deterministic typed conflict
**And** the conflict response identifies only the safe current operation state and permitted next action.

**Given** repeated semantically equivalent submissions with the same stable operation identity
**When** the authoritative outcome already exists
**Then** the stored outcome is returned rather than executing the logical action again
**And** a time-window hash alone can never authorize a repeated durable mutation or human decision.

**Given** an authorized actor and an operation ID
**When** operation status is requested
**Then** the response includes canonical state, safe reason, origin, attempt count and ceiling, retry eligibility, partial-output metadata, prior outcome, terminal reason, correlation, and safe next actions
**And** unauthorized actors receive the same existence-neutral redaction rules as other resources.

**Given** an operation transitions through the initial command-family lifecycle
**When** a transition is attempted
**Then** only declared transitions are accepted and the state, actor, server-UTC time, reason, and correlation are captured
**And** invalid transitions reject before state change.

**Given** a pending or terminal operation
**When** its status is polled repeatedly
**Then** identity and committed fields remain stable and the latest state is monotonic according to the declared lifecycle
**And** status retrieval does not hold a connection open waiting for completion.

**Requirements:** FR80, FR90, ARCH-7, ARCH-17, ARCH-41, NFR13, NFR13a, NFR15, NFR19, NFR26, NFR32, NFR34, NFR36, NFR70, UX-DR37.

### Story 1.6: Persist the First Isolated Policy Snapshot

As an authorized tenant user,
I want my governed action evaluated against an immutable tenant policy snapshot,
So that the decision is reproducible and cannot be influenced by another tenant's policy.

**Acceptance Criteria:**

**Given** A6 has no accepted data-class protection contract and independently witnessed runtime evidence
**When** tenant-material policy persistence or onboarding is requested
**Then** persistence fails closed with the appropriate safe catalog code
**And** development validation is limited to explicitly labelled synthetic fixtures without implying pilot or compliance readiness.

**Given** an A6-approved policy-snapshot data-class contract
**When** a policy snapshot is persisted
**Then** it records tenant, schema version, policy version, effective time, authorizing actor, provenance, redaction, retention, hold, export/delete, backup, encryption, and rollback relationships required by that contract
**And** security-sensitive changes create a new immutable version rather than altering an existing snapshot.

**Given** a request evaluated under policy
**When** the policy is resolved
**Then** the exact immutable policy snapshot ID and version used by the decision are returned to the command pipeline
**And** missing, stale, malformed, unauthorized, or ambiguous policy fails closed.

**Given** policy changes after an operation has been evaluated
**When** historical evidence is inspected
**Then** the original decision continues to reference its original immutable snapshot
**And** rollback or replacement applies prospectively through a new version.

**Given** the first tenant-material derived store introduced by ChatBot
**When** native-store isolation tests run with two tenants and adversarial keys, cursors, cache entries, and direct API calls
**Then** physical partitioning derives only from trusted server tenant context and cross-tenant reads and writes are impossible
**And** application-level filters are not accepted as the isolation mechanism.

**Given** the same store is unavailable or its isolation proof is absent, stale, or failing
**When** a policy-backed command, machine surface, or multi-tenant use is attempted
**Then** that increment remains disabled and the request fails closed
**And** no later M2 proof can retroactively legitimize the unproven persistence.

**Given** persisted policy snapshots, diagnostics, backups, and test artifacts
**When** security scans and data-handling checks execute
**Then** required encryption and separation are applied and plaintext secrets or protected policy values are absent from exported artifacts
**And** the evidence identifies the exact store, configuration, test run, and policy contract used.

**Requirements:** FR55a, FR61, FR68, ARCH-17, ARCH-18, ARCH-19, ARCH-20, ARCH-36, ARCH-39, NFR3, NFR4, NFR7, NFR9a, NFR11, NFR12, NFR35, NFR52, NFR53, NFR55, NFR59.

### Story 1.7: Admit Commands Through the Single CommandGateway

As an authorized user submitting a state-changing request,
I want one consistent admission decision before any effect occurs,
So that unsafe or incomplete commands fail closed regardless of their origin.

**Acceptance Criteria:**

**Given** a typed `IChatBotCommand` from any surface or machine origin
**When** it enters the Server boundary
**Then** it can reach mutation handling only through the single `CommandGateway`
**And** adapters can translate input and attach immutable origin but cannot authorize, classify, approve, deduplicate, or audit independently.

**Given** a command entering the gateway
**When** admission executes
**Then** the stages run in the canonical order: authenticate, tenant-bind, apply authorization, classify action risk, validate approval, validate stable operation identity, validate expected revision or approved owner guard, construct the canonical envelope, and request atomic commit
**And** no origin can omit, reorder, or duplicate a stage.

**Given** a command whose action classification is read-only and has no external effect
**When** current tenant policy and Project authorization explicitly permit its product-declared subtype
**Then** admission can return `accepted` without inventing mutation authority
**And** an unknown or indeterminate subtype is never silently treated as low risk.

**Given** a command that mutates Project state, exposes files, sends externally, creates or assigns tasks, invokes tools, or acts on behalf
**When** approval validation runs
**Then** a valid current approval is mandatory and tenant policy cannot downgrade the requirement
**And** missing, stale, mismatched, or incomplete approval returns a typed non-mutating outcome.

**Given** any required identity, tenant, authorization, policy, approval, operation, revision, audit, or dependency input is unavailable or invalid
**When** its admission stage runs
**Then** processing stops with the appropriate safe catalog code
**And** no later stage, domain event, idempotency record, projection, notification, or external effect occurs.

**Given** the A13 atomic target is not accepted and available
**When** an otherwise valid mutating command reaches the commit stage
**Then** the command returns the declared A13-blocked or audit-unavailable result and writes nothing
**And** command preparation or metadata generation is never reported as owner execution.

**Given** architecture and gateway-stage tests
**When** a second write pipeline, public internal stage, direct aggregate dispatch, or adapter-stage replica is introduced
**Then** the build fails non-vacuously
**And** the supported EventStore pre-commit hook is the only permitted mounting point for the gateway admission stages.

**Requirements:** FR61, FR68, FR81a, ARCH-3, ARCH-4, ARCH-8, ARCH-12, ARCH-16, ARCH-30, ARCH-31, ARCH-32, NFR1, NFR7, NFR8, NFR15, NFR16, NFR32, NFR65, NFR70, UX-DR50.

### Story 1.8: Commit a Governed Mutation and Canonical Audit Atomically

As a compliance owner,
I want each durable mutation committed with its complete canonical evidence in one atomic boundary,
So that authoritative state can never exist without its audit, idempotency, policy, and approval record.

**Acceptance Criteria:**

**Given** A13 does not identify one accepted owner-dispatched write target with supported ACL, concurrency, fencing, and recovery semantics
**When** any mutating command reaches the gateway commit stage
**Then** the operation fails closed and no authoritative record is written
**And** a local metadata preparation path, projection outbox, or test fixture cannot claim owner execution or close A13.

**Given** the exact A13 candidate has been accepted by its required owners and Security
**When** an admitted mutation commits
**Then** the owner-dispatched write atomically persists the domain event, lifetime-terminal idempotency outcome, policy and approval references, and canonical audit envelope
**And** any failure of one element causes all elements to remain uncommitted.

**Given** a canonical mutation envelope
**When** completeness validation executes
**Then** it contains the required tenant, actor, actor type, immutable origin, command, resource, decision, reason, correlation, server-UTC time, policy, approval, source evidence, identity, transition, redaction, predecessor, sequence, idempotency, and outcome facts
**And** completeness is 100 percent for committed mutations.

**Given** concurrent or repeated writes to the same aggregate stream
**When** provider ETag or first-write fencing is evaluated
**Then** exactly one valid predecessor and sequence commits and competing writes receive their stored or typed conflict outcomes
**And** duplicate predecessors, forks, and reorder attempts cannot create a second authoritative history.

**Given** the event, idempotency, policy-reference, approval-reference, or audit storage is unavailable before commit
**When** the mutation is attempted
**Then** the caller receives typed `AuditUnavailable` or the more specific safe failure and no authoritative state changes
**And** no branch permits an unaudited continuation or a later projection to repair the missing envelope.

**Given** a successful atomic commit
**When** publication and projection processing occur
**Then** they consume only the committed authoritative outcome and may visibly remain pending or retryable
**And** projection success is never part of, or a substitute for, the atomic mutation boundary.

**Given** the atomic-boundary conformance fixture
**When** success, injected partial failure, concurrency, duplicate predecessor, fork, reorder, checkpoint rebuild, and recovery cases run
**Then** assertions inspect the persisted state-store and event-stream end state rather than only an HTTP response or mock call
**And** the fixture command is explicitly non-production, cannot enter the public operation catalog or AI allowlist, and cannot support a pilot or tamper-evidence claim.

**Requirements:** FR55, FR61, FR68, FR81a, ARCH-4, ARCH-7, ARCH-13, ARCH-15, ARCH-17, ARCH-37, ARCH-39, NFR13, NFR15, NFR15a, NFR19, NFR49, NFR49a, NFR50, NFR65, NFR70.

### Story 1.9: Handle Duplicate, Conflicting, and Sensitive Non-Mutating Attempts

As an authorized user or security reviewer,
I want repeated operations and sensitive failures resolved deterministically and recorded safely,
So that retries cannot duplicate effects and attempted boundary violations remain accountable.

**Acceptance Criteria:**

**Given** an already committed operation and an equivalent request carrying the same lifetime-stable operation ID
**When** the request is submitted again from an authorized equivalent context
**Then** the gateway returns the stored canonical outcome without dispatching, publishing, projecting, notifying, or invoking an external effect again
**And** the returned response retains the original committed identity, revision, and correlation relationships.

**Given** an existing operation ID and a request with a non-equivalent normalized payload, tenant, target, authority, policy, approval, or expected revision
**When** idempotency validation runs
**Then** the request receives typed `idempotency-conflict` without changing either operation
**And** the response does not disclose the conflicting operation to an unauthorized actor.

**Given** two human decisions competing for one authoritative decision slot
**When** they arrive concurrently
**Then** first commit wins and the losing request returns the recorded current or terminal outcome
**And** neither decision is overwritten, retried in place, or represented as a second authoritative decision.

**Given** an at-least-once delivery or a retry after an ambiguous transport response
**When** the original operation outcome is queried or resubmitted
**Then** committed work replays the stored outcome and uncommitted work follows its family-specific retry contract
**And** no repeated committed domain or external effect occurs.

**Given** a denial, restricted read, service-client authentication/authorization failure, invalid transition, or other declared security-sensitive non-mutating attempt
**When** it is handled
**Then** a separate canonical attempt record captures the authorized metadata required for actor, scope, reason, correlation, policy, redaction, and outcome
**And** it is not misrepresented as a committed domain mutation.

**Given** security-attempt recording is unavailable
**When** a restricted read or unauthorized mutation is attempted
**Then** access and mutation remain denied with an existence-neutral safe response
**And** unavailable recording never broadens access, returns protected content, or causes an untracked domain mutation.

**Given** conformance tests for equivalent duplicates, conflicting duplicates, concurrent decisions, transport retries, denials, restricted reads, and service-client failures
**When** they execute
**Then** event-stream and state-store assertions prove exactly-once authoritative outcomes and the required separate attempt records
**And** redaction scans prove no restricted payload, PII, or secret entered public responses or diagnostic exports.

**Requirements:** FR55, FR57, FR68, FR80, FR90, ARCH-13, ARCH-16, ARCH-17, NFR2, NFR4, NFR10, NFR13, NFR13a, NFR15, NFR15a, NFR19, NFR32, NFR50, NFR51.

### Story 1.10: Complete the First Governed UI Action

As an authorized UI user,
I want to inspect a failed command and request its governed retry,
So that I can take a safe recovery action and follow its authoritative outcome.

**Acceptance Criteria:**

**Given** an authenticated user opens the operation route with an operation ID
**When** the UI calls `GetWorkflowOperationStatus` through the generated typed Client
**Then** it renders the authorized operation identity, canonical state/reason, immutable origin, attempts and ceiling, retry eligibility, partial-output status, prior outcome, terminal reason, correlation, and safe next action
**And** it never reads a store, actor, queue, projection, or internal gateway service directly.

**Given** a failed operation is explicitly retryable and the user has current authority
**When** the user activates Retry and confirms the displayed target and consequence
**Then** the UI constructs the catalog-defined `RetryCommandExecution` with a new stable operation ID, predecessor link, expected revision, actor, tenant, target, correlation, and source attribution
**And** submission enters the single CommandGateway through the typed Client.

**Given** the retry request is submitted
**When** admission returns
**Then** the UI shows `accepted`, `needs-review`, `approval-required`, `denied`, `unsupported`, or typed failure before implying that retry work has begun
**And** pending duplicate submission is disabled without relying on client-side suppression for correctness.

**Given** the prior operation is committed, terminally non-retryable, exhausted, stale, unauthorized, or no longer eligible
**When** Retry is requested
**Then** no original effect is repeated and the UI displays the existence-neutral catalog outcome and safe next action
**And** valid page state and focus are preserved.

**Given** an admitted retry remains asynchronous
**When** status changes or an advisory SignalR nudge arrives
**Then** the UI re-queries the authoritative typed status and shows pending, partial, completed, failed, or terminal state without falsely reporting success
**And** the notification payload itself is never trusted as authoritative content or state.

**Given** the live route is rendered
**When** its success, loading, empty, validation, unauthorized/redacted, degraded, retryable, and terminal states are exercised
**Then** it uses the single FrontComposer shell, `FcPageLayout`, `FcPageHeader`, and Fluent controls without raw `button`, `input`, `select`, or `textarea` elements
**And** the scoped styles are loaded and `.fluent-layout` computes to `display:grid` on the live route.

**Given** keyboard-only, screen-reader, 200/400-percent zoom, phone, tablet, light/dark/forced-colors, and reduced-motion validation
**When** the operation route and retry interaction are tested in English and French
**Then** focus order and return are correct, state and errors are not color/motion-only, touch targets meet the declared floor, and both locales expose identical capabilities and safe reasons
**And** the enumerated live-route checks pass without a static-fixture substitute.

**Given** a baseline performance run with healthy dependencies
**When** the operation route retrieves user-facing status
**Then** the read meets the default p95 of two seconds or displays a retrievable pending/degraded state
**And** correlation traverses UI, typed Client, API, gateway, EventStore, audit, publication, projection, and status retrieval.

**Requirements:** FR59, FR80, FR81, FR81a, FR90, ARCH-12, ARCH-15, ARCH-16, ARCH-26, NFR18, NFR24, NFR32, NFR34, NFR39, NFR60, NFR62, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR7, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR37, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR65, UX-DR66, UX-DR69.

### Story 1.11: Create Reproducible Safety Evaluation Assets

As a quality and security reviewer,
I want versioned evaluation datasets and isolated tenant sandboxes,
So that governed behavior can be reproduced without exposing production data or resources.

**Acceptance Criteria:**

**Given** an evaluation dataset or fixture version
**When** it is admitted to the repository or evaluation store
**Then** every record is explicitly consented, verified redacted, or synthetic and carries provenance, schema version, expected outcome, redaction result, stable fixture identity, and integrity hash
**And** data with missing authority, provenance, or redaction evidence is rejected.

**Given** the initial evaluation corpus
**When** its coverage manifest is inspected
**Then** it includes reproducible positive and adversarial cases for tenant authorization, redaction, duplicate submission, conflicting idempotency, expected revision, retry, approval-required classification, refusal, audit unavailability, and canonical-envelope completeness
**And** cases for later association and AI capabilities are contract fixtures only and do not claim those capabilities are implemented.

**Given** a tenant-scoped sandbox run
**When** its topology and credentials are resolved
**Then** stores, keys, topics, queues, caches, actors, identities, and test artifacts are isolated from other sandbox tenants and every production resource
**And** the run uses only dedicated test credentials and deterministic safe adapter replacements.

**Given** a sandbox request from a human, service client, worker, mailbox event, CLI shim, MCP shim, or AI fixture actor
**When** it crosses its permitted tenant or resource boundary
**Then** the expected result is an existence-neutral denial with no state change or protected-data leakage
**And** native-store and API assertions verify the end state rather than relying only on response codes.

**Given** an evaluation run with the same dataset, configuration, seed, contract versions, and implementation revision
**When** it is repeated
**Then** deterministic cases produce the same normalized commands, state transitions, outcomes, and audit facts
**And** any intentional expected-outcome change requires a new dataset version and recorded regression rationale.

**Given** logs, traces, screenshots, browser output, support artifacts, and failure diagnostics produced by evaluation
**When** redaction validation runs
**Then** secrets and protected tenant, Project, participant, file, message, policy, and audit evidence are absent
**And** failed redaction invalidates the run and prevents external sharing.

**Given** an increment evidence manifest
**When** evaluation results are referenced
**Then** it records exact dataset, fixture, contract, implementation, runner, time, result, and independent-verification provenance
**And** synthetic preparation, stale results, or incomplete evidence cannot close A5, A6, A13, or support pilot, compliance, tamper-evidence, or production claims.

**Requirements:** FR92, FR93, ARCH-19, ARCH-33, ARCH-37, ARCH-39, NFR3, NFR4, NFR9a, NFR10, NFR11, NFR32, NFR50, NFR59, NFR65, NFR68.

## Epic 2: Email Intake, Association & Production Correction

Contributors can ingest authorized project email, resolve participants and Project association from safe evidence, handle ambiguity and duplicates, correct mistakes, and recover correction propagation in production.

### Story 2.1: Provision a Governed Mailbox Source and Ingestion Client

As an authorized tenant provisioning administrator,
I want a narrowly scoped mailbox source and ingestion client established through governed commands,
So that mailbox processing starts only with explicit tenant, mailbox, and machine authority.

**Acceptance Criteria:**

**Given** a tenant with two distinct current Tenants owners and no ChatBot mailbox configuration
**When** the required `GrantChatBotAdminRole`, `ConfigureMailboxSource`, `UpdateTenantPolicy`, and `GrantServiceClientPermission` operations are proposed and independently approved as applicable
**Then** each operation passes through CommandGateway with current owner evidence, exact scope, expected revision, separation of duty, and atomic canonical audit
**And** direct database/state seeding, claims-only authorization, self-approval, or broad bootstrap workarounds are rejected.

**Given** an approved controlled mailbox source
**When** its configuration becomes active
**Then** it identifies exactly one tenant, authorized M365/Exchange mailbox pattern, monitored folders/events, closed `strict|paranoid` participation mode, routing constraints, and immutable policy version
**And** it grants no mailbox-content read, Project access, association-decision, or broader tenant authority to the configuring administrator.

**Given** an approved `mailbox-ingestion-client` grant
**When** the machine identity authenticates
**Then** it is limited to one tenant and configured mailbox pattern with only `MailboxEvent.Receive`, `Message.Capture`, and `Attachment.Capture`
**And** it never inherits human roles or permission to query Project content, decide association, invoke AI, or send communication.

**Given** a mailbox-ingestion credential
**When** its lifecycle is evaluated
**Then** it expires within 90 days, is auto-rotated through the governed credential path, and can be revoked without fallback access
**And** expired, revoked, over-scoped, under-scoped, tenant-mismatched, or case-mismatched credentials fail closed.

**Given** current mailbox membership, delegation, provider permission, or tenant lifecycle evidence is missing, stale, malformed, or unavailable
**When** source activation or intake authorization is evaluated
**Then** the affected mailbox source remains inactive or degraded with a safe reason and owner/next action
**And** unrelated mailbox and tenant scopes are not broadened or disabled.

**Given** A6 or A13 evidence required for the exact persistence/bootstrap candidate is unaccepted
**When** production activation or tenant-material capture is requested
**Then** activation is blocked and no tenant-material email record is persisted
**And** configuration metadata or synthetic tests cannot claim M0, onboarding, pilot, or compliance readiness.

**Given** grant, revoke, rotation, activation, denial, or service-client failure
**When** the operation completes or fails
**Then** its canonical record includes principal, tenant, exact mailbox scope, permission set, expiry, approvers, policy, reason, correlation, and outcome
**And** audit unavailability returns redacted `AuditUnavailable` with no grant, configuration, or protected-data return.

**Requirements:** FR18, FR19, FR55, FR61, FR68, ARCH-11, ARCH-12, ARCH-13, ARCH-20, ARCH-21, ARCH-22, ARCH-23, ARCH-39, NFR1, NFR4, NFR5, NFR6, NFR7, NFR15a, NFR31, NFR35, NFR50.
