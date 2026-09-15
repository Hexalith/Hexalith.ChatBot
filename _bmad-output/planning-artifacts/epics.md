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

### Story 2.2: Capture an Immutable Mailbox Source Record

As a project contributor using an authorized mailbox,
I want incoming email captured with its complete source identity and governance metadata,
So that it can enter collaboration workflows without losing provenance or being filed prematurely.

**Acceptance Criteria:**

**Given** an active governed mailbox source and its authorized ingestion client
**When** an M365/Exchange event is received through `CaptureMailboxEvent`
**Then** the immutable source record preserves tenant, mailbox identity, provider message/event identity, internet message ID, conversation/thread identity, sender, recipients, received/sent source times, server-UTC capture time, subject/body provenance, and attachment references
**And** no Project association or Project-visible artifact is inferred merely from receipt.

**Given** provider-supplied authenticity and sender headers
**When** the mailbox adapter parses the event
**Then** it retains DMARC, DKIM, and SPF verdicts plus `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender` discrepancies as source metadata and finite review reason codes
**And** it records external-sender and delegate/`principal_for` evidence without claiming ChatBot independently reverified provider verdicts.

**Given** tenant policy requires consent or lawful-basis metadata for an external participant, retained content, attachments, or later AI processing
**When** capture admission evaluates the message
**Then** the applicable basis, purpose, policy version, retention class, and evidence reference are recorded before the protected data persists
**And** missing required basis blocks the applicable persistence or processing without discarding safe intake diagnostics.

**Given** an A6-approved data-class contract for each persisted email field
**When** the source record is stored
**Then** encryption, tenant partitioning, retention, legal hold, export/delete, backup, redaction, and surviving-metadata handling follow that exact contract
**And** no plaintext message, participant, credential, or attachment data appears in logs, traces, errors, or support artifacts.

**Given** the same provider event or message is delivered more than once
**When** its canonical `tenant + mailbox + provider message/event identity` equivalence is evaluated
**Then** one source record and one authoritative capture outcome exist
**And** the duplicate returns the stored outcome and creates no duplicate Project message, attachment, task intent, approval, notification, or audit decision.

**Given** authorization, tenant binding, mailbox permission, policy, audit readiness, required provider identity, or data protection cannot be resolved
**When** capture is attempted
**Then** the event fails closed into its declared retryable, review, quarantined, or terminal intake state
**And** original provider identity and permitted recovery metadata remain available without exposing message content to an unauthorized actor.

**Given** a captured message that is rejected, deferred, failed, skipped, quarantined, or awaiting review later
**When** its source record is inspected by an authorized actor
**Then** the original permitted email context and provenance remain intact under retention/redaction policy
**And** derived workflow decisions never mutate the immutable source.

**Requirements:** FR1, FR2, FR10, FR20, FR64, ARCH-10, ARCH-18, ARCH-19, ARCH-20, ARCH-23, ARCH-41, NFR3, NFR4, NFR9a, NFR12, NFR13a, NFR14, NFR15a, NFR17, NFR19, NFR31, NFR36, NFR50, NFR52, NFR53, NFR55.

### Story 2.3: Separate Immutable Mail Sources from Rebuildable Projections

As an authorized workflow user,
I want mailbox source evidence kept separate from derived association views,
So that projections can be corrected or rebuilt without changing or re-ingesting the original email.

**Acceptance Criteria:**

**Given** a committed mailbox source event
**When** association processing requires a read model
**Then** an event-fed tenant-scoped projection is created with source identity, source version, provenance, derivation-contract version, schema version, redaction, retention, correlation, and current projection status
**And** the immutable email source remains authoritative and unchanged.

**Given** duplicate, delayed, or out-of-order source events
**When** the projection handler processes them
**Then** projection writes are idempotent and order-tolerant and use authoritative source version rather than arrival time
**And** a stale event cannot overwrite a newer derived state.

**Given** a projection record is first persisted
**When** native-store and API isolation tests execute
**Then** its trusted tenant partition, cache keys, cursor scope, topic subscription, and projection keys prevent cross-tenant access by construction
**And** absence or failure of that proof blocks the projection and its machine-surface use.

**Given** the projection is missing, stale, rebuilding, or unavailable
**When** an authorized query or downstream command requests association context
**Then** the response reports the safe current projection state, source version, correlation, and permitted next action
**And** it never fabricates current data, treats the projection as owner authority, or exposes another tenant's source.

**Given** an authorized projection-rebuild request
**When** rebuilding from immutable sources and canonical events
**Then** the resulting normalized projection matches a clean build for the same source/version set without mailbox re-ingestion
**And** the baseline dataset can be rebuilt within the declared four-hour recovery target or reports a measured failure.

**Given** a committed projection change
**When** an advisory SignalR notification is emitted
**Then** it carries only bounded tenant-grouped metadata compatible with the declared projection-change detail
**And** every UI consumer re-queries the authoritative typed read model rather than trusting notification content.

**Given** a source or projection failure
**When** diagnostics are emitted
**Then** metadata-only logs and traces preserve correlation, source/projection versions, state, and safe reason
**And** no raw email, participant, evidence, or restricted Project detail is logged.

**Requirements:** FR10, FR59, FR91, ARCH-17, ARCH-18, ARCH-19, ARCH-20, ARCH-26, NFR9a, NFR11, NFR17, NFR32, NFR34, NFR37, NFR38, NFR39, NFR50, NFR57.

### Story 2.4: Resolve Participants and Contain Unresolved Actors

As an authorized association reviewer,
I want email participants resolved to tenant-scoped Parties with unresolved cases clearly identified,
So that external contributions can be retained without granting unverified actors Project authority.

**Acceptance Criteria:**

**Given** an immutable captured email
**When** participant resolution runs
**Then** sender and recipients are resolved through a ChatBot-owned `IParticipantDirectory` adapter against current Parties authority
**And** ChatBot stores stable tenant-scoped Party IDs and source evidence references rather than copied owner PII.

**Given** an internal or external participant with a current unambiguous owner match
**When** `LinkOrResolveEmailParticipant` completes
**Then** the immutable resolution outcome records participant type, owner version, evidence, policy snapshot, actor, time, confidence/disposition, and correlation
**And** the outcome does not itself grant Project membership or broader action authority.

**Given** a participant cannot be resolved safely
**When** resolution completes
**Then** the participant enters a visible `NeedsReview`, `Deferred`, `Rejected`, or `Quarantined` disposition with safe evidence and next actions
**And** `MarkParticipantResolutionDisposition` and `ResumeParticipantResolutionReview` use expected revision and immutable predecessor/successor links.

**Given** an unresolved external sender whose retained email is allowed by tenant participation and lawful-basis policy
**When** the message enters association review
**Then** the sender can contribute email context without receiving MVP portal access
**And** the UI and records label the actor as unresolved/external without inferring an identity.

**Given** an unresolved, unauthorized, revoked, or tenant-mismatched actor
**When** it attempts to view Project candidates or files, create a task request, trigger a command, package AI context, or send communication
**Then** the action is denied before resource access or mutation
**And** the response and participant state reveal no forbidden Project or resource existence.

**Given** current Parties or tenant owner evidence is unavailable, stale beyond its allowed bound, malformed, or contradictory
**When** a trust-bearing resolution or authorization decision is requested
**Then** processing fails closed into a safe review/degraded state
**And** an event-fed local mirror may support redacted display only and cannot authorize the decision.

**Given** native/API isolation and negative authorization tests with internal, external, unresolved, and cross-tenant actors
**When** they execute
**Then** no participant record, candidate, evidence, Project detail, or action crosses tenant/Project authority
**And** resolution and denial records retain complete correlation and safe audit facts.

**Requirements:** FR13, FR14, FR15, FR16, FR17, ARCH-10, ARCH-11, ARCH-17, ARCH-18, ARCH-19, NFR1, NFR2, NFR6, NFR7, NFR11, NFR17, NFR32, NFR50, NFR55, UX-DR20, UX-DR44.

### Story 2.5: Generate Deterministic Authorized Association Candidates

As an authorized association user,
I want Project candidates ranked from deterministic explainable evidence,
So that association decisions do not depend on hidden inference or expose unauthorized Projects.

**Acceptance Criteria:**

**Given** a captured message with permitted association evidence
**When** the independently versioned `AssociationScorer` runs
**Then** it evaluates only product-declared deterministic signals including explicit Project identifier, mailbox-routing rule, and conversation/thread identifier
**And** it emits a finite normalized score, confidence band, ranked candidates, signal class/value, reason codes, evidence offsets/references, scorer version, and evaluation time.

**Given** potential Projects from the owner directory
**When** candidates are assembled
**Then** tenant, actor, Project, and resource authorization is applied before ranking or response construction
**And** unauthorized or cross-tenant Projects never appear in candidate rows, counts, evidence, confidence calculations, logs, telemetry, or errors.

**Given** equivalent source evidence, policy snapshot, scorer version, and authorized Project set
**When** scoring is repeated
**Then** candidate ordering, scores, bands, reasons, and normalized machine-readable output are identical
**And** deterministic evidence outranks and cannot be changed by any optional AI explanation.

**Given** the association, task-intent, and action-risk kernels
**When** architecture and artifact checks execute
**Then** each has an independent model/version, score space, fixtures, and runtime implementation
**And** no score, threshold, model artifact, or calibration output is shared implicitly among them.

**Given** an AI-provider outage or live AI remains disabled by A5
**When** association scoring runs
**Then** deterministic candidate generation and human review remain available without a live AI call
**And** the system does not infer A5 closure or degrade authorization safeguards.

**Given** malformed, non-finite, incomplete, conflicting, or unavailable scoring input
**When** the scorer evaluates it
**Then** it produces a typed safe failure or review result with no automatic association
**And** permitted original evidence remains linked for later resolution.

**Given** the approved MVP workload baseline
**When** candidate generation is measured
**Then** it completes within p95 ten seconds or returns a retrievable pending/review status with operation identity and safe next action
**And** evaluation records bind the exact consented/redacted/synthetic dataset, scorer version, thresholds, expected outcomes, and regression history.

**Requirements:** FR3, FR11, FR16, FR59, FR92, ARCH-11, ARCH-18, ARCH-30, NFR1, NFR2, NFR22, NFR25, NFR32, NFR34, NFR68, NFR70.

### Story 2.6: Govern Association Rules and Confidence Thresholds

As an authorized policy administrator,
I want association rules, evidence requirements, and confidence thresholds changed through bounded independent approval,
So that automatic association remains calibrated, explainable, and resistant to unilateral weakening.

**Acceptance Criteria:**

**Given** a tenant with no custom association threshold version
**When** the M0 association policy is resolved
**Then** `association.t-high` defaults to `0.90` and `association.t-low` defaults to `0.60`
**And** `T_low` affects ranking and presentation only and can never create an automatic-association disposition.

**Given** an `UpdateTenantPolicy` proposal for association rules, required evidence, `T_high`, or `T_low`
**When** schema validation runs
**Then** `T_high` is within `[0.80,1.00]`, `T_low` is within `[0.50,T_high)`, and every rule/evidence field belongs to the closed schema
**And** unknown, non-finite, contradictory, authority-expanding, or out-of-range values are rejected without a new policy version.

**Given** a security-sensitive association-policy proposal
**When** it is submitted and decided
**Then** a current authorized `policy-admin` initiates it and a distinct current authorized administrator independently approves it with justification, exact changed values, scope, expected revision, and expiry
**And** self-approval, service-client, worker, or AI-actor mutation is unavailable with a safe reason.

**Given** a proposal lowering either threshold or materially changing evidence/rules
**When** approval is evaluated
**Then** it references a versioned evaluation run demonstrating the required precision of at least 95 percent, recall of at least 90 percent across reviewed and auto-associated cases, and zero critical unauthorized-Project false positives
**And** missing, stale, mismatched, or failing calibration evidence blocks activation.

**Given** an approved policy change
**When** it activates
**Then** it creates a new immutable prospective policy snapshot containing initiator, distinct approver, justification, schema and scorer compatibility, calibration evidence, and canonical audit
**And** historical decisions continue to reference the policy and threshold versions originally used.

**Given** a stale revision, expired approval, owner-evidence failure, separation-of-duty conflict, audit unavailability, or concurrent policy activation
**When** the change is committed
**Then** no policy version is created or modified and a typed safe conflict/failure is returned
**And** the previously active valid policy remains effective.

**Given** a rollback request
**When** the prior values remain schema-valid and are independently approved
**Then** rollback creates a new immutable version referencing the superseded version
**And** no existing snapshot or association decision is destructively changed.

**Requirements:** FR9, FR61, ARCH-17, ARCH-18, ARCH-21, ARCH-30, ARCH-41, NFR13, NFR15a, NFR23, NFR35, NFR50, NFR68, UX-DR35, UX-DR36.

### Story 2.7: Route Association Outcomes Without Silent Contamination

As a project contributor,
I want uncertain or unsafe association outcomes routed to review,
So that email is never silently placed into the wrong Project.

**Acceptance Criteria:**

**Given** one authorized candidate with score at or above the active `T_high`, every required deterministic evidence item present and fresh, no conflicting signal, and a valid current policy snapshot
**When** `AssociateEmailToProject` evaluates the proposal at the expected revision
**Then** the message may transition from `Received` or `Proposed` to `Associated` for that candidate
**And** the event records scorer/evidence/policy versions, reason, actor/origin, operation identity, correlation, and canonical audit.

**Given** a score below `T_high`, multiple materially conflicting candidates, missing required evidence, stale evidence, unresolved authorization, or no safe candidate
**When** association disposition is determined
**Then** the message transitions to `NeedsReview` and is not attached to a Project
**And** `T_low` affects candidate ordering/presentation only and never changes that disposition to automatic association.

**Given** a scorer error, non-finite output, malformed result, unavailable authorization dependency, or unauthorized evidence
**When** disposition is determined
**Then** the message fails closed to `NeedsReview` with an empty public candidate list and a finite safe reason code
**And** suppressed candidates, raw scorer input, and restricted Project existence are absent from response and telemetry.

**Given** an association attempt using stale expected revision, stale policy, stale owner authority, or an invalid lifecycle transition
**When** the command reaches the commit boundary
**Then** no association or Project projection changes and the attempt receives a typed conflict, denied, or degraded outcome
**And** the rejected transition is separately audited with permitted actor, reason, state, and correlation facts.

**Given** a message in `NeedsReview`, `Rejected`, `Deferred`, `Failed`, `Skipped`, or a pending operation state
**When** an authorized actor retrieves it
**Then** the original permitted email context and source evidence remain linked and inspectable under retention/redaction policy
**And** the response identifies the safe next action without implying association completion.

**Given** association processing exceeds the candidate-generation budget
**When** the operation remains incomplete
**Then** it returns a retrievable pending/review status with stable operation identity, safe reason, owner/next action, and correlation
**And** later completion still validates the original or refreshed expected revision and current authority before commit.

**Given** automated disposition tests
**When** boundary, conflict, stale, unauthorized, no-candidate, and scorer-failure cases execute
**Then** only the exact eligible case auto-associates and every other case remains outside Project context
**And** persisted event/projection end state and canonical audit are asserted.

**Requirements:** FR3, FR4, FR8, FR10, FR11, FR16, FR57, FR61, FR68, ARCH-7, ARCH-17, ARCH-30, NFR2, NFR7, NFR13, NFR15, NFR17, NFR25, NFR32, NFR39, NFR50, NFR70.

### Story 2.8: Record Association Decisions, Evidence, and Human Rationale

As an authorized association reviewer,
I want each decision captured with its evidence and optional rationale,
So that the outcome is explainable, immutable, and safe to reconstruct.

**Acceptance Criteria:**

**Given** an association item in a state that permits human review
**When** an authorized reviewer submits `ConfirmEmailProjectAssociation`, `RejectEmailProjectAssociation`, `DeferEmailProjectAssociation`, `MarkEmailAssociationNeedsReview`, or `SkipEmailAssociation`
**Then** the command uses the item's stable decision slot, expected revision, current tenant/Project authority, immutable source, evidence, scorer, and policy versions
**And** it passes through the single CommandGateway and atomic canonical commit.

**Given** a reviewer confirms one visible candidate
**When** all authority and evidence remain current
**Then** the item transitions to `Associated` with the selected Project and records the decision consequence
**And** only the authorized selected candidate identifier—not suppressed alternatives—is included in the authoritative outcome.

**Given** a reviewer rejects all, defers, marks for review, or skips the item
**When** that decision is valid for the current state
**Then** the exact disposition, actor, time, reason, note reference, next-action state, and preserved original-email reference are committed
**And** the message is not attached to any Project unless a later authorized successor decision does so.

**Given** source evidence supporting a decision
**When** the decision commits
**Then** permitted evidence references preserve signal class/value, confidence/band, reason, candidate-visibility rule, source and scorer versions, policy snapshot, freshness, redaction, decision actor/time, and correlation
**And** retention and redaction boundaries remain attached to every reference.

**Given** evidence is stale, expired, unauthorized, changed, unavailable, or inconsistent with the selected candidate
**When** confirmation is attempted
**Then** confirmation is disabled or rejected with the finite safe reason and refresh/review action
**And** no stale decision, decision-slot outcome, or Project association is committed.

**Given** the reviewer supplies an optional note or resolution rationale
**When** the decision is recorded
**Then** the note is appended as a non-authoritative annotation linked to the canonical workflow envelope with author, time, redaction, retention, and correlation
**And** it cannot alter a source, decision, policy, evidence, audit fact, or repair a missing canonical envelope.

**Given** concurrent, repeated, or stale human decisions
**When** they target the same decision slot
**Then** first commit wins, equivalent retries return the prior outcome, and non-equivalent attempts receive a typed conflict
**And** the original and attempted outcomes remain reconstructable without in-place mutation.

**Requirements:** FR5, FR6, FR8, FR10, FR11, FR60, FR62, FR90, ARCH-13, ARCH-17, ARCH-18, NFR13, NFR13a, NFR15, NFR36, NFR48, NFR50, NFR51, NFR54, NFR70, UX-DR25.

### Story 2.9: Resolve Ambiguous Association in the Live Review Surface

As an authorized association reviewer,
I want to compare safe candidates and submit a clear association decision,
So that I can resolve ambiguity without exposing or contaminating another Project.

**Acceptance Criteria:**

**Given** an authorized reviewer opens an ambiguous association item
**When** the S2 route loads through the generated typed Client
**Then** it displays the permitted original message context, ranked safe candidates, confidence/band, deterministic reasons, decision consequences, source provenance, and current workflow state
**And** unauthorized candidates and evidence are absent from rows, counts, accessibility text, HTML, telemetry, and errors.

**Given** two or more safe candidates
**When** the candidate group renders
**Then** it is one named Fluent radiogroup with one Tab stop; arrow keys change selection and announce position/count without committing
**And** each option describes its safe Project identity, evidence, confidence, reason, and consequence without relying on color.

**Given** the Association Review layout
**When** primary and complementary information is arranged
**Then** the candidate group and persistent decision bar remain jointly visible outside the accordion while complementary evidence uses the shared Fluent accordion
**And** side-by-side comparison preserves labels and relationships at desktop and stacks safely at smaller widths.

**Given** a selected candidate
**When** the decision bar is announced or reviewed
**Then** its accessible description repeats the selected safe Project and exposes Confirm, Reject all, Defer, and Escalate/needs-review according to the canonical action availability
**And** the reviewer can add an optional non-authoritative note without obscuring the decision consequence.

**Given** the reviewer submits a permitted decision
**When** the command is admitted
**Then** the UI uses the completed Story 2.8 typed command with stable decision slot and expected revision and displays its operation identity and authoritative outcome
**And** it never applies an optimistic association before the admission/commit result.

**Given** no safe candidate, below-threshold evidence, deterministic conflict, scorer error, stale/expired evidence, validation failure, retry/quarantine state, or terminal state
**When** the route renders
**Then** it shows the corresponding safe state, responsible role or next action, and only actions that are enabled, focusable-disabled-with-reason, or not-applicable-hidden
**And** invalid confirmation focuses an error summary linked to the candidate/decision error while preserving selection and note.

**Given** evidence freshness changes while the reviewer is reading
**When** a reference becomes stale or expired
**Then** exactly one timestamped `fresh|stale|expired` indicator per reference updates and expiry disables the affected decision with `evidence-expired`
**And** the status is announced once without forced scroll or loss of focus.

**Given** live-route accessibility and visual-conformance validation
**When** S2 is exercised for loading, empty, every decision, unauthorized/redacted, degraded, conflict, retryable, terminal, keyboard, screen reader, zoom, phone/tablet, light/dark/forced-colors, reduced-motion, English, and French cases
**Then** WCAG 2.2 AA and Fluent/FrontComposer governance checks pass non-vacuously
**And** the user can identify the next action without reading raw audit logs.

**Requirements:** FR4, FR5, FR6, FR10, FR11, FR12, FR16, FR57, FR66, ARCH-15, ARCH-16, NFR1, NFR2, NFR24, NFR48, NFR60, NFR61, NFR62, NFR63, NFR70, UX-DR2, UX-DR3, UX-DR5, UX-DR11, UX-DR12, UX-DR13, UX-DR15, UX-DR22, UX-DR24, UX-DR25, UX-DR28, UX-DR29, UX-DR44, UX-DR47, UX-DR57, UX-DR65, UX-DR66, UX-DR69.

### Story 2.10: Correct and Supersede a Project Association

As an authorized Project owner,
I want to correct a wrong email association without erasing the original decision,
So that contaminated context is contained and the accountable history remains intact.

**Acceptance Criteria:**

**Given** a reversible existing association and current authority over the affected workflow and permitted target Project
**When** the owner submits `CorrectEmailProjectAssociation` with the selected target, rationale, evidence, stable operation ID, decision slot, and expected revision
**Then** the command is evaluated against current tenant/Project authority, policy, source, and evidence through CommandGateway
**And** stale, unauthorized, unsupported, or irreversible cases are denied before state change with a safe reason.

**Given** an eligible correction commits
**When** the association aggregate transitions
**Then** a new immutable correction decision supersedes the prior association, links predecessor/successor, records actor/time/rationale/evidence/policy/correlation, and enters `Correcting`
**And** the original source and association decision remain unchanged and reconstructable.

**Given** an item in `Correcting`
**When** any Project-context read, file exposure, AI context packaging, task action, outbound communication, or governed execution references the association
**Then** the system blocks authoritative use or marks the permitted view stale according to the declared contract
**And** AI use and boundary effects remain blocked until all required stores acknowledge the corrected source version.

**Given** concurrent corrections or a decision racing with another workflow transition
**When** they target the same expected revision or decision slot
**Then** first commit wins, equivalent duplicates return the stored outcome, and other attempts receive a typed conflict
**And** no branch produces two current associations or mutates the original decision.

**Given** tenant policy permits correction-derived association evidence in M1
**When** an accepted correction is added to the scorer's learned-signal dataset
**Then** it is versioned separately from M0 deterministic signals and carries source correction, policy, authorization, feature, weight/class, calibration, and redaction provenance
**And** users can inspect exactly how it influenced a later candidate score.

**Given** correction-derived evidence is disabled, stale, unauthorized, unexplained, uncalibrated, or conflicts with required deterministic evidence
**When** later scoring runs
**Then** that learned signal has no association influence and cannot enable silent automatic disposition
**And** the exclusion or conflict is represented by a typed explainable reason.

**Given** the live correction entry surface
**When** eligibility, rationale validation, stale revision, permission denial, accepted correction, or initial `Correcting` state is exercised
**Then** it preserves source evidence and form state, focuses linked errors, shows predecessor/successor identity and safe next action, and never implies propagation is complete
**And** English/French keyboard and screen-reader checks pass on the live route.

**Requirements:** FR7, FR8, FR60, FR63, FR90, FR96, ARCH-17, ARCH-18, ARCH-25, ARCH-30, NFR1, NFR13, NFR15, NFR16, NFR17, NFR47, NFR48, NFR50, NFR51, NFR60, NFR70, UX-DR34, UX-DR47, UX-DR59.

### Story 2.11: Coordinate Complete Correction Propagation

As a Project owner who corrected an association,
I want every affected derived store invalidated and acknowledged,
So that stale Project context cannot be treated as current.

**Acceptance Criteria:**

**Given** an association has entered `Correcting`
**When** its propagation plan is created
**Then** the aggregate records the corrected source version and the complete required-store set for the active increment, including candidate ranking, evidence snapshot, and operational queue projections
**And** each later derived store class must register its correction contract before architecture tests allow that store to persist association-derived data.

**Given** a required store receives a correction activity
**When** it invalidates and rebuilds its records
**Then** the work is tenant-scoped, idempotent, source-version guarded, and records acknowledged store, old/new source versions, time, result, operation, and correlation
**And** an older or duplicate activity cannot overwrite newer corrected data or produce a second acknowledgement outcome.

**Given** `AcknowledgeAssociationCorrectionStore` for one required store
**When** the acknowledgement commits at the expected revision
**Then** the aggregate updates acknowledged and remaining store sets while retaining `Correcting`
**And** only the final valid required acknowledgement transitions the item to `Corrected` and emits `AssociationCorrected`.

**Given** an item remains `Correcting`
**When** an authorized user views its status
**Then** the response shows predecessor/successor association, acknowledged and remaining stores, measured progress, current estimate, owner, next action, and safe correlation
**And** Project-context reads remain stale/blocked and AI context remains unavailable until completion.

**Given** propagation exceeds p95 ten minutes in M0/M1 or the applicable p95 sixty-minute M2 target
**When** acknowledgements remain outstanding
**Then** `MarkAssociationCorrectionDelayed` moves the item to `Correction-delayed`, identifies the responsible owner and safe next action, and triggers a P2 incident
**And** later valid acknowledgements can still complete the same immutable correction without clearing its incident history.

**Given** a store failure, unavailable dependency, invalid acknowledgement, stale revision, poison activity, or exhausted retry
**When** propagation cannot advance safely
**Then** the affected store remains outstanding, false completion is impossible, and the item exposes a retryable, quarantined, delayed, or terminal safe state
**And** unrelated tenant/store partitions continue where isolation permits.

**Given** propagation audit is reconstructed
**When** the correction history is queried
**Then** it connects the original association, successor correction, propagation plan, every activity/acknowledgement, delays/incidents, retries, and final outcome
**And** missing acknowledgement or audit evidence prevents completion authority.

**Requirements:** FR7, FR8, FR59, FR60, FR66, FR91a, ARCH-17, ARCH-18, ARCH-25, ARCH-27, NFR13, NFR15, NFR17, NFR17a, NFR18, NFR19, NFR34, NFR39, NFR41, NFR50, NFR51, NFR70, UX-DR34, UX-DR59.

### Story 2.12: Run Correction Propagation in the Supported Production Topology

As a Project owner awaiting a correction,
I want propagation executed by a durable hosted workflow,
So that the corrected association becomes trustworthy despite retries, restarts, or dependency failures.

**Acceptance Criteria:**

**Given** the completed correction coordinator/activity contracts
**When** the supported AppHost and container topology starts
**Then** the Dapr Workflow runtime is explicitly registered, health-checked, and wired to the tenant-partitioned `chatbot-workflow-statestore`, required pub/sub, EventStore writer, and ChatBot-owned store adapters
**And** no activity directly mutates Projects, Conversations, Folders, Memories, EventStore internals, or another owner's state.

**Given** an admitted correction
**When** the hosted workflow starts
**Then** its durable instance identity derives from tenant and correction operation, and it carries correction/source versions, required stores, authority, policy, retry profile, correlation, and current aggregate revision
**And** duplicate starts converge on the same authoritative workflow instance.

**Given** a workflow activity fails transiently
**When** retry policy applies
**Then** approved Retry Profile v1 or a stricter declared profile enforces typed retryability, ceiling, jittered backoff, lease expiry, and poison/dead-letter handling
**And** every repeat remains idempotent and cannot acknowledge an uncompleted store effect.

**Given** the worker or Dapr sidecar restarts during propagation
**When** the topology becomes healthy again
**Then** the workflow resumes from durable state without losing completed acknowledgements or repeating their effects
**And** user status retains workflow identity, retry count, last safe reason, remaining stores, estimate, and correlation.

**Given** workflow runtime, state store, pub/sub, audit, EventStore, or projection dependency failure
**When** admission or execution requires that dependency
**Then** failure is confined to the narrowest tenant/workflow/store scope possible, false success is impossible, and Project/AI context remains blocked
**And** unrelated partitions continue fairly without starvation from poison or delayed work.

**Given** the workflow exceeds its correction SLO or exhausts retry
**When** the operations-owned monitor invokes the declared transition
**Then** the aggregate records `Correction-delayed` or the appropriate terminal/recovery state through CommandGateway
**And** P2 escalation, owner, next action, and recovery command are visible without requiring Epic 11 dashboards.

**Given** production-path acceptance is evaluated
**When** a live `aspire run` test performs correction through success, restart, duplicate, transient failure, delay, and terminal failure scenarios
**Then** it verifies workflow, aggregate, projection, acknowledgement, audit, and state-store end states—not only mocks or HTTP status
**And** evidence names exact topology, dependency revisions, configuration, runner, time, and result while preserving all open release gates.

**Requirements:** FR66, FR80, FR91a, ARCH-24, ARCH-25, ARCH-27, ARCH-28, ARCH-33, ARCH-37, ARCH-39, NFR15a, NFR17, NFR17a, NFR18, NFR19, NFR20, NFR30, NFR34, NFR37, NFR39, NFR41, NFR58, NFR59, NFR66.

### Story 2.13: Recover Mailbox and Association Work Without Duplicate Effects

As an authorized reviewer or operator,
I want failed mailbox and association work exposed with safe family-specific recovery,
So that delivery faults can be resolved without duplicating or rewriting authoritative decisions.

**Acceptance Criteria:**

**Given** a mailbox intake, participant resolution, association, projection, or correction operation fails
**When** its state is recorded
**Then** it is classified as pending, review, retryable, failed, quarantined, skipped, or terminal with stable reason, retry count/ceiling, owner, next action, operation identity, and correlation
**And** failure never silently discards the immutable source or reports an uncommitted effect as complete.

**Given** retryable mailbox intake
**When** `RetryMailboxIntake` is submitted at the current revision
**Then** a linked immutable attempt follows the intake Retry Profile and converges on the existing source identity
**And** it cannot create a second email source or downstream artifact.

**Given** a failed or reviewable association
**When** `ReprocessEmailAssociation`, `ResumeEmailAssociationReview`, or the applicable participant-resolution successor is submitted
**Then** a new linked attempt uses current policy, owner authority, evidence freshness, scorer version, and expected revision
**And** the original attempt and any immutable human decision remain unchanged.

**Given** an explicit out-of-scope mailbox rule or an equivalent duplicate already has an authoritative outcome
**When** intake or association is evaluated
**Then** the item records `Skipped` with a finite reason and returns the prior outcome where applicable
**And** no duplicate Project message, attachment, task intent, approval, command, notification, outbound send, or audit decision can be produced.

**Given** a terminal `Rejected`, `Failed`, or `Skipped` workflow requires new evaluation
**When** authorized reprocessing is allowed
**Then** it creates the declared new workflow/attempt identity with predecessor and successor links
**And** the terminal record is never reopened, overwritten, or retried in place.

**Given** Graph revocation, expiry, throttling, partial access, delay/replay, subscription expiry, permission drift, dependency outage, lease expiry, or poison work
**When** the worker handles the condition
**Then** bounded backoff, ceiling, dead-letter/quarantine, manual recovery, and narrow-scope degradation follow the declared profile
**And** unrelated tenants, mailboxes, Projects, and workflow partitions remain serviceable where isolation permits.

**Given** a new artifact family later consumes the same mailbox source or association outcome
**When** architecture and duplicate-registry tests run
**Then** that family must declare stable identity, semantic equivalence, duplicate suppression, transition, retry/successor behavior, and end-state assertions before activation
**And** absence of the registration fails the non-vacuous test rather than relying on this story's current-family coverage.

**Given** authorized UI/API status for any retry or failure
**When** it is retrieved
**Then** the user sees terminality, stale/waiting/blocked state, safe next action, prior outcome, and retry eligibility without raw diagnostics or unauthorized Project detail
**And** the same operation can be reconstructed from canonical audit and immutable links.

**Requirements:** FR8, FR10, FR14, FR60, FR64, FR65, FR66, FR68, FR80, FR90, ARCH-17, ARCH-18, ARCH-27, NFR13, NFR13a, NFR14, NFR17, NFR18, NFR19, NFR20, NFR22, NFR30, NFR31, NFR39, NFR50, NFR51, NFR58, NFR70.

## Epic 3: Project Conversation Context, Files & Attachments

Contributors can see email-derived Project conversation context, participants, decisions, governed attachments, provenance, and current workflow status, and can prepare explicitly authorized context packages for later AI use.

### Story 3.1: Establish the Tenant-Isolated Project Conversation

As an authorized Project contributor,
I want a Project-scoped conversation view built from governed events,
So that I can follow collaboration context without seeing another tenant or Project.

**Acceptance Criteria:**

**Given** an authorized actor with current Project authority
**When** the actor requests the Project conversation through the typed Client
**Then** the read model returns only that tenant/Project's ordered permitted conversation entries with stable entry identity, event type, actor/origin, server-UTC time, provenance, source version, redaction, and current projection state
**And** the query applies authorization before projection, cache, cursor, or response access.

**Given** conversation records, cache entries, cursors, topics, and SignalR groups
**When** native-store/API isolation tests exercise two tenants and Projects with adversarial identifiers
**Then** physical keys and scopes derive only from trusted server context and prevent cross-tenant/Project access by construction
**And** application filtering alone is not accepted as isolation proof.

**Given** an unauthorized, revoked, stale-authority, malformed, or cross-tenant conversation request
**When** it is evaluated
**Then** it returns an existence-neutral safe outcome without Project name, participant, message, file, evidence, audit, or count leakage
**And** no projection data is read before authorization.

**Given** a conversation longer than one response page
**When** entries are queried
**Then** the server uses stable ordering and a tenant/Project-scoped opaque cursor with page size no greater than 100
**And** altered, expired, or cross-scope cursors fail safely without revealing the underlying key.

**Given** no selected Project, an empty authorized conversation, loading, stale projection, or degraded dependency
**When** the live S1 route renders
**Then** the single FrontComposer shell and Project context header display the exact safe state, current surface, owner/next action where applicable, and no fabricated conversation content
**And** general user upload, ungoverned execution, and unauthorized Project switching are absent.

**Given** a committed conversation-projection change
**When** a bounded tenant-grouped SignalR nudge arrives
**Then** the UI preserves selection, focus, scroll, and current reading position and offers a keyboard-reachable new-updates action when needed
**And** it re-queries the authoritative typed model rather than trusting notification payload content.

**Given** the approved baseline with healthy dependencies
**When** the conversation read is measured
**Then** it meets the default p95 of two seconds or returns a truthful pending/degraded state with operation identity and safe next action
**And** metadata-only telemetry records latency, projection lag, tenant-safe scope, and correlation.

**Requirements:** FR21, FR25, FR57, FR59, ARCH-18, ARCH-19, ARCH-26, ARCH-41, NFR1, NFR2, NFR9a, NFR11, NFR24, NFR27, NFR32, NFR34, NFR38, NFR39, NFR70, UX-DR2, UX-DR10, UX-DR16, UX-DR17, UX-DR18, UX-DR44, UX-DR46, UX-DR55, UX-DR56, UX-DR68, UX-DR69.

### Story 3.2: Render Associated Email as Authoritative Conversation Context

As an authorized Project contributor,
I want associated email shown as attributed Project conversation entries,
So that I can read its collaboration context while retaining source authority and provenance.

**Acceptance Criteria:**

**Given** an email has a current completed Project association
**When** the conversation projection consumes its committed source and association events
**Then** it creates one idempotent email-derived entry linked to immutable message and association identities, source/association versions, tenant/Project, correlation, retention, and redaction
**And** a duplicate or older event cannot create a second entry or overwrite newer association state.

**Given** an authorized user views that entry
**When** permitted message content is rendered
**Then** the entry shows source type, attributed sender/origin, recipients as authorized, subject/body context, source timestamp with relevant timezone, capture time, and current association state
**And** HTML or rich content is safely sanitized and cannot execute active content or reveal redacted fields through hidden markup.

**Given** the same conversation contains human, external, mailbox, worker, CLI, MCP, AI, or system entries
**When** entries are ordered and labelled
**Then** every entry has visible text/icon attribution and system decisions cannot resemble anonymous human chat
**And** stable IDs/codes use monospace only where appropriate while primary data uses Fluent text/data components.

**Given** source evidence for an email entry
**When** the user opens or navigates it
**Then** permitted authoritative evidence is expanded by default with source identity, redaction, timestamp, freshness, and provenance
**And** disclosure state, labels, and focus behavior are announced and keyboard operable.

**Given** source content or metadata is unauthorized, redacted, retained only for audit, stale, or unavailable
**When** the entry renders
**Then** the view shows the safe redacted/unavailable state without hidden source text, suppressed identities, or existence leaks
**And** copy, transcript, export, read-aloud, accessible names, and descriptions apply the same redaction as visible content.

**Given** the entry is loading, projection-pending, corrected, degraded, or terminal
**When** its live route state renders
**Then** the layout-matched busy/status treatment preserves the prior conversation and identifies state and safe next action without forced scroll
**And** no state is conveyed only by color, motion, or toast.

**Given** automated contract and browser tests for the associated-email concern of FR22
**When** they execute independently of participant, attachment, decision, approval, failure, and AI-outcome fixtures
**Then** the email concern passes its projection, authorization, redaction, keyboard, screen-reader, and visual-state assertions
**And** the test cannot pass solely because another FR22 concern rendered.

**Requirements:** FR21, FR22 (associated-email concern), FR25, FR27, ARCH-17, ARCH-18, NFR2, NFR3, NFR10, NFR32, NFR36, NFR60, NFR62, NFR64, UX-DR7, UX-DR8, UX-DR18, UX-DR20, UX-DR22, UX-DR25, UX-DR46, UX-DR55, UX-DR56, UX-DR67.

### Story 3.3: Show Authorized Participant and Origin Attribution

As an authorized Project contributor,
I want each conversation entry attributed to its permitted participant and origin,
So that I can distinguish who or what contributed without assuming unverified identity or authority.

**Acceptance Criteria:**

**Given** an entry linked to a resolved participant
**When** the conversation projection builds attribution
**Then** it references the stable Party ID, participant type, source role at the time, immutable surface/worker/mailbox origin, resolution version, provenance, and redaction state
**And** display labels come from an authorized owner-context read and are not copied into authoritative domain events.

**Given** an entry from a human, external party, mailbox, service client, worker, UI/API, CLI, MCP, AI actor, or system process
**When** attribution renders
**Then** a shared actor badge uses visible text plus icon to identify the permitted actor class and origin
**And** the presentation does not imply that an external sender, service client, AI actor, or display role has Project authority.

**Given** an unresolved participant
**When** the entry renders
**Then** it shows a safe unresolved/external state and permitted review status without guessing a name or matching identity
**And** actions requiring resolved authority remain unavailable with an associated safe explanation.

**Given** current participant display data is stale, unavailable, redacted, or no longer authorized
**When** the conversation is queried
**Then** the system uses a safe stable-ID/redacted label and freshness state without broadening access
**And** stale display metadata cannot authorize a trust-bearing action.

**Given** an actor may view the message but not one or more participant details
**When** the entry, copy, transcript, export, read-aloud, accessible name, or description is produced
**Then** the same field-level redaction is applied consistently
**And** HTML, telemetry, and hidden accessibility content contain no suppressed identity.

**Given** keyboard, screen-reader, locale, forced-colors, and responsive validation
**When** actor badges and unresolved states are exercised
**Then** role, name/state where authorized, origin, and unavailable reason are understandable without hover or color
**And** English and French expose equivalent safe meaning.

**Given** automated contract and browser tests for the participant concern of FR22
**When** they execute independently of email, attachment, decision, approval, failure, and AI-outcome fixtures
**Then** participant projection, authorization, redaction, attribution, and accessibility assertions pass
**And** the concern cannot pass because a generic message row exists without actor semantics.

**Requirements:** FR22 (participant concern), FR25, FR28, ARCH-10, ARCH-11, ARCH-17, ARCH-18, NFR1, NFR2, NFR6, NFR32, NFR38, NFR60, NFR62, UX-DR18, UX-DR20, UX-DR44, UX-DR65, UX-DR67.

### Story 3.4: Explain Why an Email Belongs to the Project

As an authorized Project contributor,
I want to inspect the evidence and decision that associated an email,
So that I can understand its placement and follow any correction history.

**Acceptance Criteria:**

**Given** an associated email entry and current authority to its permitted evidence
**When** the user opens `Why this project`
**Then** labelled facts show signal class, matched value, confidence and band, disposition, reason, candidate-visibility rule, scorer and policy versions, decision actor/time, evidence references, and correction links
**And** authoritative source evidence remains visually and structurally distinct from explanatory derived text.

**Given** evidence references in the explanation
**When** they render
**Then** each shows permitted source identity/content, redaction, timestamp, and exactly one `fresh|stale|expired` indicator
**And** an expired reference identifies its effect on available decisions and the safe refresh/review action.

**Given** an automatic association
**When** its explanation is inspected
**Then** the view identifies the required deterministic evidence, absence of conflict, active `T_high`, and exact scorer/policy versions used
**And** it does not imply AI inference or show correction-derived influence unless that versioned policy signal actually participated.

**Given** a human-confirmed or corrected association
**When** its explanation is inspected
**Then** the current decision links to immutable predecessor/successor decisions and permitted rationale without overwriting the original explanation
**And** `Correcting` or `Correction-delayed` state shows acknowledged/remaining propagation and continued AI-context block.

**Given** the viewer lacks authority for a candidate, participant field, source fragment, or audit fact
**When** the explanation is assembled
**Then** that detail is omitted or safely redacted before response construction and cannot be inferred from counts, reason text, links, HTML, or accessibility descriptions
**And** the remaining explanation stays coherent and existence-neutral.

**Given** disclosure, focus, and responsive behavior
**When** the explanation opens, closes, updates freshness, or follows a correction link
**Then** state is announced, focus returns correctly, visible order matches focus order, and no content is available only on hover
**And** the interaction passes live English/French keyboard, screen-reader, zoom, forced-colors, and reduced-motion checks.

**Requirements:** FR23, FR27, FR28, FR60, FR96, ARCH-17, ARCH-18, NFR2, NFR32, NFR48, NFR50, NFR51, NFR60, NFR62, NFR64, UX-DR22, UX-DR24, UX-DR25, UX-DR34, UX-DR67, UX-DR69.

### Story 3.5: Show Workflow Decisions, Status, Failures, and Review History

As an authorized Project contributor,
I want workflow outcomes and human review history visible in conversation context,
So that I can understand current state and the next safe action without reading raw audit logs.

**Acceptance Criteria:**

**Given** committed association, attachment, task-intent, approval, AI-action, command, failure, retry, or correction events permitted to the viewer
**When** the conversation projection consumes them
**Then** it creates typed attributed workflow entries with subject identity, family, state, actor/origin, transition, time, safe reason, policy/evidence references, operation/correlation, terminality, and next action
**And** unsupported producer families can be exercised through contract fixtures without fabricating production events.

**Given** an association or other human decision entry
**When** it renders
**Then** the decision concern independently shows actor, disposition, reason/rationale where permitted, decision time, authoritative slot, predecessor/successor, and affected subject
**And** it cannot be satisfied by merely rendering the source message or another FR22 concern.

**Given** an approval entry
**When** it renders
**Then** the approval concern independently distinguishes pending, approved, rejected, revision-requested, cancelled, expired, invalidated, and execution-linked outcomes with requester/reviewer authority as permitted
**And** a pending or rejected approval never resembles completed work.

**Given** a failure or retry entry
**When** it renders
**Then** the failure concern independently shows stable safe code, state, attempt count/ceiling, retry eligibility, owner, next action, terminality, and correlation
**And** raw exceptions, restricted payloads, and privileged diagnostic causes remain absent.

**Given** any email, attachment, approval, AI action, or command with human review
**When** its history is requested
**Then** the visible sequence preserves every permitted immutable review decision, note, supersession, correction, and retry relationship in server-UTC order
**And** no current-state projection overwrites or hides an earlier authoritative decision.

**Given** projection lag or an accepted long-running operation
**When** the user views conversation status
**Then** pending, partial, stale, waiting, blocked, retryable, terminal, and projection-pending states are distinguishable and include safe next action and operation identity
**And** the UI never reports `Done` from an HTTP acceptance or notification alone.

**Given** a user without Project/item/audit detail authority
**When** workflow entries are queried
**Then** per-item evidence, identities, reasons, and links are redacted or omitted while an authorized aggregate safe state may remain
**And** accessible descriptions, copy, export, and hidden markup use the same redaction.

**Given** independent FR22 concern tests
**When** associated-email, participant, attachment, decision, approval, failure, and AI-outcome fixtures are each removed in turn
**Then** the corresponding concern test fails while the other six can still pass
**And** this story's decision, approval, and failure concern tests each assert their own projection and rendered semantics.

**Requirements:** FR22 (decision, approval, and failure concerns), FR24, FR28, FR57, FR62, FR63, FR66, FR80, ARCH-17, ARCH-18, NFR2, NFR17, NFR32, NFR36, NFR38, NFR39, NFR40, NFR50, NFR51, NFR60, NFR62, NFR63, NFR70, UX-DR20, UX-DR37, UX-DR44, UX-DR45, UX-DR65, UX-DR67.

### Story 3.6: Distinguish Informational and Actionable Conversation Intent

As an authorized Project contributor,
I want conversation entries labelled by detected intent with inspectable evidence,
So that I can recognize possible requests without mistaking intent detection for permission or risk approval.

**Acceptance Criteria:**

**Given** a permitted source message
**When** the independently versioned `TaskIntentDetector` evaluates it
**Then** it returns `informational`, `request-information`, `request-action`, `request-decision`, or a typed indeterminate/failure state with confidence, evidence offsets/excerpts, detector version, evaluation time, and source identity/version
**And** offsets are validated against the exact immutable source and cannot reference redacted or out-of-range content.

**Given** the association scorer, task-intent detector, and action-risk classifier artifacts
**When** dependency/version checks execute
**Then** the detector has its own categorical contract, fixtures, calibration, and runtime artifact
**And** it shares no score, threshold, model artifact, or implied disposition with association or action risk.

**Given** an actionable or indeterminate intent marker
**When** it is projected into conversation context
**Then** the marker records provenance, tenant/Project, source/evidence versions, confidence, state, redaction, retention, and correlation
**And** no Project mutation, task creation, proposal, approval, command execution, or external effect occurs from detection alone.

**Given** the user views a classified entry
**When** its message-classification badge renders
**Then** visible text plus icon distinguishes informational from actionable and exposes the specific request kind, confidence/state, and a keyboard-accessible evidence-review action
**And** it never labels the item low-risk, approved, executable, or completed.

**Given** the user activates evidence review
**When** the review content opens
**Then** it shows the full permitted source, highlighted offsets/excerpts, detector version, confidence, evaluation time/state, and safe guidance for later governed disposition
**And** the action is read-only, returns focus correctly, and cannot invoke a future proposal command.

**Given** low confidence, conflicting evidence, source-version mismatch, malformed offsets, detector failure, or unavailable AI provider
**When** classification is requested
**Then** the item displays an indeterminate/review or typed safe failure state without actionable automation
**And** informational conversation, association review, and other non-AI workflows remain available.

**Given** contract, deterministic-fixture, authorization, and browser tests
**When** each intent category and error state is exercised in English/French, keyboard, screen-reader, forced-colors, and reduced-motion modes
**Then** category, evidence, confidence, and review semantics are independently verifiable
**And** no restricted source content appears in markup, accessible descriptions, logs, or exports.

**Requirements:** FR26, FR27, ARCH-18, ARCH-30, NFR2, NFR8, NFR10, NFR22, NFR32, NFR60, NFR62, NFR64, NFR68, UX-DR20, UX-DR21, UX-DR22, UX-DR25, UX-DR44, UX-DR56, UX-DR67.

### Story 3.7: Distinguish AI Summaries and Outcomes from Source Evidence

As an authorized Project contributor,
I want generated summaries and AI outcomes visibly separated from authoritative evidence,
So that I can use them as assistance without confusing them with the source record.

**Acceptance Criteria:**

**Given** an authorized committed AI summary or outcome record, or a non-production conformance fixture
**When** the conversation projection consumes it
**Then** it creates a typed AI entry with model/provider identifier, model version, generation time, source IDs/versions, evidence provenance, policy/redaction/retention versions, completion state, actor/origin, operation, and correlation
**And** A5-blocked or fixture content is never represented as a live production invocation.

**Given** an AI summary and its permitted source evidence
**When** they render
**Then** authoritative source evidence is expanded by default and generated content is collapsed by default under a visible `AI summary` label
**And** model/version/time/source-ID provenance precedes generated content and the two use structurally distinct semantic regions.

**Given** an AI proposal, allowed response, denied request, failed request, or completed execution outcome
**When** the AI-outcome concern of FR22 renders
**Then** it identifies the exact outcome state, origin, source request, authority status, operation, safe reason, and next action without resembling anonymous chat or completed work when pending
**And** each state is testable without another FR22 concern being present.

**Given** generated content is partial, stale, superseded, failed, redacted, or missing one or more sources
**When** it is displayed
**Then** that condition is explicit, partial content is not treated as a committed Project message, and unavailable source authority is not replaced by the summary
**And** copy/export/transcript/read-aloud output retains the AI label, provenance, state, and applicable redaction.

**Given** the viewer lacks authority for a source, model detail, Project context, or generated field
**When** the response is assembled
**Then** the field is omitted or safely redacted before rendering and no hidden text, count, citation, or error confirms it
**And** remaining generated content is withheld if its safe interpretation depends on the suppressed source.

**Given** keyboard, screen-reader, zoom, forced-colors, reduced-motion, responsive, English, and French validation
**When** the summary disclosure and AI outcomes are exercised
**Then** labels, provenance, disclosure state, completion state, source precedence, focus, and safe next actions remain understandable without color or animation
**And** the renderer uses Fluent/FrontComposer components with no decorative assistant persona.

**Given** independent FR22 concern tests
**When** the AI-outcome fixture is removed or mislabeled
**Then** the AI-outcome test fails while email, participant, attachment, decision, approval, and failure concern tests remain independently evaluable
**And** a generic message card cannot satisfy AI provenance or source-separation assertions.

**Requirements:** FR22 (AI-outcome concern), FR27, FR28, ARCH-18, ARCH-30, ARCH-39, NFR2, NFR9, NFR10, NFR22, NFR32, NFR60, NFR62, NFR64, NFR68, UX-DR6, UX-DR9, UX-DR18, UX-DR20, UX-DR22, UX-DR23, UX-DR25, UX-DR44, UX-DR56, UX-DR67, UX-DR69.

### Story 3.8: Capture Email Attachments into Governed Quarantine

As an authorized Project contributor,
I want referenced email attachments captured without immediate exposure,
So that their provenance and safety can be established before Project storage or use.

**Acceptance Criteria:**

**Given** an immutable captured email with provider attachment references and an authorized mailbox-ingestion client
**When** `CaptureEmailAttachment` processes one reference
**Then** it creates one tenant-scoped capture aggregate with stable attachment/source identity, immutable attempt ID, message/mailbox provenance, provider metadata, content hash where safely available, declared name/type/size, policy, retention/redaction, operation, and correlation
**And** it transitions to `PendingScan` without creating a Project-folder file or AI context item.

**Given** attachment bytes are retrieved from the provider
**When** they enter ChatBot custody
**Then** they are encrypted and written only to the declared tenant-partitioned quarantine/staging store with no executable serving path
**And** logs, traces, errors, projections, and support artifacts contain no bytes, secrets, or protected content.

**Given** the same provider attachment is delivered or requested repeatedly
**When** canonical source identity and content metadata are evaluated
**Then** equivalent capture returns the existing attempt/outcome and creates no duplicate staged content or downstream artifact
**And** a conflicting payload under the same provider identity becomes a typed review/quarantine failure rather than overwriting content.

**Given** missing attachment data, declared type/size outside the closed limits, provider access failure, malformed metadata, or source-version mismatch
**When** capture admission runs
**Then** the attachment enters the applicable pending, unavailable, rejected, failed, or retryable state with a safe reason and next action
**And** no unvalidated content is exposed to Project or AI consumers.

**Given** the actor can view the email but lacks attachment authority
**When** attachment metadata or content is requested
**Then** access is denied before staging-store or projection access with no filename, size, type, hash, status detail, or content leak
**And** the security-sensitive attempt is recorded through the authorized audit path.

**Given** a new attachment record class and quarantine store
**When** A6/data-class and native/API isolation gates are evaluated
**Then** protection, retention, hold, export/delete, backup, erasure, encryption, and physical tenant-partition proof pass before tenant-material persistence
**And** missing proof blocks activation and all machine-surface use.

**Given** the MVP UI and public contracts
**When** attachment capture capabilities are inspected
**Then** they expose no general user upload or scheduled/file-addition automation
**And** only authorized email-derived attachment references can initiate this capture family.

**Requirements:** FR29, FR32, FR34, FR55a, FR64, ARCH-18, ARCH-19, ARCH-20, NFR3, NFR4, NFR9a, NFR13a, NFR14, NFR15a, NFR21, NFR52, NFR53, UX-DR27, UX-DR68.

### Story 3.9: Scan and Quarantine Unsafe Attachments

As an authorized Project contributor,
I want captured attachments safety-checked before storage or exposure,
So that unsafe or unsupported content cannot enter Project context.

**Acceptance Criteria:**

**Given** an attachment in `PendingScan`
**When** the ChatBot-owned scanner adapter submits it to the approved scanning dependency
**Then** only the tenant-scoped quarantined bytes and required metadata are supplied under least privilege
**And** the aggregate performs no I/O and trusts only the adapter's versioned typed result through CommandGateway.

**Given** a clean scan with allowed actual type and size
**When** `MarkEmailAttachmentOutcome` commits the result
**Then** the attachment records scanner/signature version, actual type/size, scan time, source version, policy, evidence, operation, and correlation as eligible for governed Project-folder storage
**And** clean status alone grants neither Project content access nor AI-context eligibility.

**Given** malware, unsafe active content, a blocked type, excessive size, declared/actual mismatch, corrupted content, or prohibited archive behavior
**When** scanning completes
**Then** the attachment transitions to terminal `Unsafe` or `Rejected` with a stable safe reason and quarantine/retention disposition
**And** bytes and sensitive scanner details never reach Project storage, conversation content, AI packaging, public errors, or telemetry.

**Given** scanner timeout, dependency outage, unavailable signature set, malformed scanner response, or transient retrieval failure
**When** the scan cannot be trusted
**Then** the attachment remains unavailable or transitions to typed `Failed`/retryable state without exposure
**And** status includes retry eligibility, attempt count/ceiling, owner, and safe next action.

**Given** a duplicate, delayed, or out-of-order scan result
**When** its attachment attempt and expected revision are validated
**Then** equivalent outcomes replay the prior result and stale/conflicting results are rejected before mutation
**And** `Unsafe` cannot be overwritten by a later stale clean response.

**Given** attachment bytes or metadata are requested before a clean current result and Project authorization
**When** any UI, API, CLI, MCP, worker, or AI actor attempts access
**Then** the request fails closed before store access and returns an existence-neutral safe response
**And** negative tests prove no cache, cursor, URL, log, trace, or accessible description leaks content or metadata.

**Given** scanner unavailability affects one tenant or attachment partition
**When** processing degrades
**Then** degradation remains at the narrowest scope possible and unrelated tenant/mailbox work continues
**And** the affected item retains visible recoverable state rather than silent loss or false completion.

**Requirements:** FR31, FR32, FR34, FR55, FR68, ARCH-10, ARCH-17, ARCH-18, NFR2, NFR3, NFR4, NFR7, NFR13, NFR15, NFR17, NFR18, NFR21, NFR39, NFR41, NFR58, NFR59, NFR70.

### Story 3.10: Store a Clean Attachment in the Governed Project Folder

As an authorized Project contributor,
I want a clean email attachment stored through the governed Folders boundary,
So that it becomes a Project file without bypassing owner authority or duplicating content.

**Acceptance Criteria:**

**Given** a current clean attachment result, completed Project association, permitted target folder, and authorized actor
**When** `StoreEmailAttachmentInProjectFolder` is admitted
**Then** CommandGateway revalidates tenant/Project/file authority, association/correction state, scan result/version, file metadata/redaction, retention policy, owner command mapping, expected revisions, idempotency, and audit readiness
**And** stale, correcting, unauthorized, unsafe, expired, or unmapped input fails before any owner call or exposure.

**Given** an A13-accepted Folders command mapping
**When** the storage choreography begins
**Then** ChatBot atomically records orchestration intent and an idempotent activity submits only the owner-accepted command with the same authority, identity, evidence, policy, operation, and correlation tuple
**And** ChatBot never writes a Folders store directly, invents a producer contract, or dual-writes owner state.

**Given** Folders accepts and commits the file
**When** its authoritative event/revision is reconciled
**Then** the attachment transitions to `Stored` with opaque tenant-scoped owner file/folder IDs, committed owner revision, scan and source provenance, retention/redaction, and canonical outcome
**And** no copied owner PII or unauthorized content metadata enters ChatBot events.

**Given** the owner effect committed but its response, event delivery, or ChatBot projection was interrupted
**When** recovery runs
**Then** the committed owner revision is reconciled as the same operation and is never retried as uncommitted work
**And** projection/status remains pending or retryable until the authoritative result is observed.

**Given** an equivalent repeated storage request
**When** identity and owner revision are checked
**Then** it returns the stored outcome without creating a duplicate file, attachment projection, conversation event, or audit decision
**And** a different payload or target under the same operation identity returns a typed conflict.

**Given** the attachment projection begins referencing association-derived Project context
**When** architecture and correction-registry tests run
**Then** it registers its source-version-guarded invalidation/rebuild contract with Story 2.11 propagation
**And** a correction blocks or marks the file context stale until the attachment store acknowledges the new association version.

**Given** Folders, audit, policy, authorization, or workflow dependency is unavailable
**When** storage is attempted
**Then** the item remains safely pending, unavailable, failed, or retryable with owner and next action and no false `Stored` state
**And** unrelated tenant/folder partitions remain usable where isolation permits.

**Requirements:** FR30, FR31, FR32, FR34, FR55, FR59, FR64, FR68, FR91a, ARCH-10, ARCH-11, ARCH-13, ARCH-14, ARCH-17, ARCH-18, NFR1, NFR7, NFR13, NFR14, NFR15a, NFR17, NFR18, NFR21, NFR34, NFR50, NFR58, NFR70.

### Story 3.11: Inspect Attachment Status in Conversation Context

As an authorized Project contributor,
I want each email attachment's governed state visible in the conversation,
So that I know whether it is safe, stored, recoverable, or eligible for later AI context.

**Acceptance Criteria:**

**Given** an authorized conversation entry with attachment records
**When** the attachment concern is projected
**Then** each row links the immutable email/source identity to its current capture attempt, scan outcome, owner file/folder outcome, source version, operation, and correlation
**And** duplicate or out-of-order events cannot create duplicate rows or regress current state.

**Given** an attachment in captured, `PendingScan`, unavailable, rejected, `Unsafe`, failed, retryable, `Stored`, correcting, or terminal state
**When** its shared attachment row renders
**Then** labelled data shows permitted filename/type/size, capture/storage state, scan/quarantine, folder, duplicate/retry status, retention, freshness, and AI-context eligibility
**And** state uses visible text plus semantic icon/border and never relies on color alone.

**Given** a retryable failed attachment
**When** an authorized actor submits `RetryAttachmentCapture`
**Then** a single linked immutable successor attempt is created or the existing successor returned, with expected revision, retry profile, operation identity, and prior-outcome link
**And** unscanned content remains unavailable and no duplicate Project file is created.

**Given** the actor lacks attachment metadata, content, folder, retry, or AI-context authority
**When** the row and available actions are assembled
**Then** restricted fields/actions are redacted, focusable-disabled with safe reason, or not-applicable-hidden according to the canonical action contract
**And** download URLs, filenames, hashes, counts, hidden text, and error details cannot reveal the resource.

**Given** the attachment list is loading, empty, degraded, stale, corrected, or updated while the user reads
**When** the live route changes
**Then** the same layout node uses `aria-busy`, focus and scroll are preserved, new updates are announced through the bounded status mechanism, and safe owner/next action remains inline
**And** reduced motion removes shimmer and non-essential row movement.

**Given** desktop, tablet, and phone layouts
**When** attachment rows reflow
**Then** primary status and safe actions remain usable, dense metadata has a state-preserving detail/handoff path, and touch targets meet the declared minimum
**And** no general upload control appears.

**Given** independent FR22 attachment-concern contract and browser tests
**When** attachment projection/rendering is removed or incomplete
**Then** the attachment concern fails independently while email, participant, decision, approval, failure, and AI-outcome concerns remain separately evaluable
**And** tests cover keyboard, screen reader, zoom, forced colors, English/French, authorization, redaction, and every declared state.

**Requirements:** FR22 (attachment concern), FR24, FR28, FR31, FR32, FR34, FR65, ARCH-17, ARCH-18, NFR1, NFR2, NFR17, NFR18, NFR21, NFR24, NFR60, NFR62, NFR70, UX-DR3, UX-DR7, UX-DR10, UX-DR11, UX-DR12, UX-DR14, UX-DR15, UX-DR27, UX-DR37, UX-DR44, UX-DR46, UX-DR56, UX-DR66, UX-DR68.

### Story 3.12: Package Authorized Project Files for Governed AI Context

As an authorized Project contributor,
I want selected Project files packaged with exact scope, redaction, and provenance,
So that a later approved AI action can access only the context I was authorized to expose.

**Acceptance Criteria:**

**Given** a request would expose Project files to AI
**When** context packaging is requested
**Then** it is treated as a non-downgradable file-exposure effect and requires a current linked approved proposal through the existing CommandGateway approval stage
**And** the packager is an internal Server stage, not a new public command, surface bypass, or AI allowlist member.

**Given** a current approved proposal naming exact Project files
**When** the package is prepared
**Then** authorization, tenant/Project, file IDs and owner revisions, clean scan status, association/correction state, evidence freshness, redaction, retention, policy, approval, provider-reuse terms, operation, and audit readiness are revalidated
**And** missing, stale, expired, correcting, unauthorized, unsafe, or mismatched input denies packaging before content access.

**Given** all checks pass
**When** the sealed context package is created
**Then** it contains only authorized redacted content plus tenant, Project, stable file/source IDs and versions, evidence/provenance, policy/redaction/retention versions, approved purpose/effect, provider-reuse prohibition, expiry, integrity hash, operation, and correlation
**And** excluded fields and source content are neither copied nor inferable from package metadata.

**Given** a package is stored pending approved use
**When** native-store and API isolation tests execute
**Then** encryption, tenant/Project partitioning below the application, purpose-bound access, short expiry, and no cross-tenant cache/search/vector reuse are proven
**And** package content never appears in logs, traces, audit envelopes, support bundles, or test diagnostics.

**Given** the approved package is presented for user review
**When** permitted files, redactions, freshness, source authority, and intended effect are shown
**Then** the preview identifies exactly what will be exposed and what was removed without revealing forbidden data
**And** expired evidence disables the effect with `evidence-expired` and requires a refreshed proposal/decision.

**Given** the underlying association, file revision, scan result, authority, policy, approval, or redaction changes after package creation
**When** the package is considered for use
**Then** it is invalidated and cannot be refreshed in place or invoked
**And** a new proposal/approval/package lineage is required.

**Given** the package references association-derived content
**When** correction-registry checks execute
**Then** its store registers a source-version-guarded invalidation/acknowledgement contract with correction propagation
**And** AI access remains blocked until the correction completes and a new authorized package is approved.

**Given** A5 remains open or no approved AI execution exists
**When** package tests run
**Then** no live provider call, external egress, training, telemetry reuse, or boundary effect occurs
**And** deterministic safe adapters can verify packaging without implying live-AI or M0/M1 readiness.

**Requirements:** FR32, FR33, FR55a, FR57, FR60, FR68, FR91a, ARCH-10, ARCH-18, ARCH-19, ARCH-20, ARCH-30, ARCH-39, NFR1, NFR2, NFR3, NFR4, NFR8, NFR9, NFR9a, NFR10, NFR11, NFR16, NFR21, NFR48, NFR50, NFR52, NFR53, UX-DR22, UX-DR25, UX-DR32, UX-DR50, UX-DR51.

### Story 3.13: Complete the Live Project Conversation Experience

As an authorized Project contributor,
I want one coherent live workspace for conversation context and governed files,
So that I can understand sources, workflow state, and safe next actions across device and access needs.

**Acceptance Criteria:**

**Given** cold start, no selected Project, empty Project, or active Project context
**When** the live S1 route renders
**Then** the single FrontComposer shell preserves authorized Project context, page state, conversation stream, complementary evidence, selection, focus, and scroll relationships
**And** it uses `FcPageLayout`, `FcPageHeader`, Fluent controls, and the scoped style bundle with live computed-grid verification.

**Given** an active conversation fixture or implemented producer data
**When** all FR22 concerns render together
**Then** associated email, participants, attachments, decisions, approvals, failures, and AI outcomes each use their independently tested typed component and preserve source/authority/state relationships
**And** removing any one concern causes only its dedicated conformance assertion to fail rather than being masked by generic stream content.

**Given** informational/actionable classification, task-intent marker, generated summary/outcome, attachment, operation, proposal fixture, correction, AI outage, degradation, or unauthorized/redacted state
**When** it appears in the stream
**Then** its authority, provenance, freshness, completion, safe reason, and next action remain explicit and no future fixture is mistaken for a live implemented effect
**And** source evidence remains authoritative over summaries and system decisions never masquerade as chat participants.

**Given** two or more titled complementary sections
**When** the workspace lays them out
**Then** they use one Fluent accordion with the primary item expanded unless the content is the single primary stream/detail/workflow
**And** active work, complementary evidence, one dialog/sheet layer, and transient feedback remain visually distinct without stacked modals.

**Given** loading, validation, empty, unauthorized, degraded, retryable, correcting, terminal, or new-update state
**When** the route changes
**Then** layout-matched busy regions, inline safe status, linked error summary, focus preservation/return, and keyboard-reachable new-updates behavior apply
**And** valid selection, filters, drafts, reading position, and historical content are not silently discarded or force-scrolled.

**Given** desktop, tablet, and phone layouts at normal, 200-percent, and 400-percent zoom
**When** the workspace reflows
**Then** reading, evidence review, status, and safe actions remain available; dense administration/investigation uses a state-preserving handoff
**And** persistent chrome never obscures focused content and touch targets meet the declared floor.

**Given** the live acceptance matrix
**When** automated accessibility, keyboard-only, screen-reader, responsive, light/dark/forced-colors, reduced-motion, and English/French tests run
**Then** WCAG 2.2 AA, equal feature/state/action semantics, semantic color, focus, disclosure, localization, and redaction checks pass
**And** raw interactive controls, legacy tokens/components, hover-only critical actions, decorative assistant persona, color/motion/toast-only meaning, and infinite lists are absent.

**Given** the approved performance baseline
**When** conversation, attachment, status, and evidence reads are exercised
**Then** user-facing reads meet p95 two seconds or expose truthful retrievable pending/degraded state
**And** the evidence binds the live route, exact source/fixture provenance, implementation revision, runner, time, and result.

**Requirements:** FR21, FR22, FR23, FR24, FR25, FR26, FR27, FR28, FR31, FR34, ARCH-15, ARCH-16, ARCH-26, ARCH-33, ARCH-37, NFR2, NFR10, NFR24, NFR27, NFR32, NFR34, NFR39, NFR60, NFR61, NFR62, NFR63, NFR64, NFR65, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR6, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR20, UX-DR21, UX-DR22, UX-DR23, UX-DR24, UX-DR25, UX-DR27, UX-DR34, UX-DR37, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR55, UX-DR56, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.

## Epic 4: Governed AI Action Mediation

Users can detect and review actionable intent, request AI help, safely receive allowed read-only assistance, and approve or refuse every boundary-crossing action before exact-once governed execution.

### Story 4.1: Capture Complete Task Intent from Authorized Evidence

As an authorized Project contributor,
I want actionable conversation intent captured with its exact source evidence,
So that a reviewer can evaluate the request without losing context or treating detection as execution authority.

**Acceptance Criteria:**

**Given** a permitted source message classified as `request-information`, `request-action`, or `request-decision` by the qualified `TaskIntentDetector`
**When** `CaptureTaskIntent` is admitted
**Then** it creates one immutable task-intent aggregate containing tenant, Project, requester, source message/version, a factual summary of no more than 280 characters, action kind, exact evidence offsets/excerpts, detector version, confidence, detection time, state, policy/redaction/retention, operation, and correlation
**And** `actionable` is stored only as the grouping of those three labels, never as a fifth detector label or risk result.

**Given** the evidence offsets and summary
**When** the command is validated
**Then** every offset resolves inside the exact authorized immutable source version and the summary remains supported by those excerpts
**And** out-of-range, stale, redacted, mismatched, or unsupported evidence is rejected before persistence.

**Given** an informational classification
**When** task-intent capture is considered
**Then** no actionable task-intent aggregate or proposal is created
**And** the informational conversation marker remains available without invoking action-risk classification.

**Given** a missing, invalid, unqualified, failed, or non-contract detector artifact/output
**When** capture is requested
**Then** the item returns `detector-unavailable` and enters authorized review without calling `ActionRiskClassifier`
**And** no proposal, domain mutation beyond the safe attempt record, or idempotency success is written.

**Given** duplicate or concurrent capture for the same tenant/source/detector version and semantic intent
**When** the commands are processed
**Then** one authoritative task-intent record exists and equivalent requests return its outcome
**And** non-equivalent conflicts receive a typed response without overwriting the original.

**Given** task-intent data first persists
**When** native-store/API isolation and data-class checks execute
**Then** the record is physically tenant/Project partitioned and covered by approved protection, retention, redaction, export/delete, backup, and erasure controls
**And** unavailable proof blocks persistence and machine-surface use.

**Given** the versioned intent-evaluation partition
**When** M0 or M1 qualification is evaluated
**Then** measured informational/actionable precision and recall meet at least `80%/75%` for M0 and `90%/85%` for M1 against the exact deployed detector version
**And** failing or mismatched evidence blocks the applicable claim without affecting non-AI workflow availability.

**Requirements:** FR35, FR55a, FR59, FR60, FR68, FR90, ARCH-18, ARCH-19, ARCH-20, ARCH-30, NFR2, NFR9a, NFR13, NFR15a, NFR22, NFR32, NFR34, NFR50, NFR52, NFR68.

### Story 4.2: Record Terminal Task-Intent Dispositions

As an authorized task-intent reviewer,
I want to close non-actionable or already resolved intent with an accountable disposition,
So that it does not continue toward an AI proposal while its evaluation history remains visible.

**Acceptance Criteria:**

**Given** a captured task intent in a reviewable state
**When** an authorized reviewer submits `MarkTaskIntentDisposition`
**Then** the only terminal dispositions are `not-actionable`, `duplicate`, `already-handled`, and `out-of-scope`
**And** the command carries decision slot, expected revision, actor, tenant/Project, source/evidence versions, reason, optional note, policy, operation, and correlation.

**Given** a `duplicate` disposition
**When** it is validated
**Then** it identifies an authorized existing predecessor task-intent or governed outcome that represents the same request
**And** the predecessor link is retained without disclosing an unauthorized subject or merging the two immutable records.

**Given** any terminal disposition commits
**When** the task-intent state changes
**Then** the original detector output, full source reference, summary, action kind, evidence offsets, version, confidence, reviewer, time, rationale, and prior states remain unchanged and reconstructable
**And** no AI-action proposal, risk classification, command execution, task creation, or external effect occurs.

**Given** a reviewer lacks current Project/review authority or the evidence, source, policy, decision slot, or expected revision is stale
**When** disposition is submitted
**Then** the operation is denied or conflicts before mutation with an existence-neutral safe reason
**And** the task intent remains in its prior state.

**Given** concurrent or repeated dispositions
**When** they target the same decision slot
**Then** first commit wins, an equivalent repeat returns the stored outcome, and a non-equivalent repeat receives the recorded terminal result or typed conflict
**And** terminal state is never reopened or edited in place.

**Given** an authorized conversation or task-intent status view
**When** a terminal disposition is displayed
**Then** it shows disposition, reviewer/time, permitted rationale/evidence, predecessor where applicable, terminality, and safe next action
**And** optional annotations remain non-authoritative and cannot alter the decision or repair missing audit.

**Requirements:** FR36, FR38, FR60, FR62, FR90, ARCH-17, ARCH-18, NFR1, NFR2, NFR13, NFR15, NFR36, NFR50, NFR51, NFR70, UX-DR26, UX-DR44, UX-DR65.

### Story 4.3: Review Captured Task Intent and Source Evidence

As an authorized task-intent reviewer,
I want the complete captured intent and source evidence in one review surface,
So that I can close unsupported intent without acting from a summary alone.

**Acceptance Criteria:**

**Given** a captured task intent and current review authority
**When** the reviewer opens its live detail route
**Then** the typed view shows full permitted source message, ≤280-character summary, action kind, highlighted evidence offsets/excerpts, detector version, confidence, detection time/state, source version, policy/redaction, operation, and correlation
**And** authoritative source evidence is expanded and visually distinct from detector-derived interpretation.

**Given** the task intent remains reviewable
**When** available actions render
**Then** `not-actionable`, `duplicate`, `already-handled`, and `out-of-scope` dispositions use the completed Story 4.2 command contract
**And** proposal conversion is not rendered or advertised until its own command path is implemented.

**Given** the reviewer selects a terminal disposition
**When** required rationale, duplicate predecessor, authority, evidence freshness, or revision validation fails
**Then** a linked error summary receives focus, valid input and selection are preserved, and the exact safe unavailable reason is reachable
**And** no optimistic terminal state is shown before the authoritative result.

**Given** a valid terminal disposition is submitted
**When** CommandGateway returns its outcome
**Then** the route displays immutable disposition, reviewer/time, note/evidence links, terminality, operation status, and safe next action
**And** it does not imply risk classification, approval, proposal, or execution occurred.

**Given** source/evidence is stale, expired, corrected, redacted, unavailable, or outside current Project authority
**When** the route renders or a decision is submitted
**Then** affected actions are focusable-disabled with safe reasons or omitted when not applicable, and no restricted content is returned
**And** evidence expiry is announced once and requires refresh before a dependent decision.

**Given** live-route acceptance
**When** loading, empty, unauthorized/redacted, detector-unavailable, validation, conflict, degraded, terminal, keyboard, screen-reader, zoom, responsive, light/dark/forced-colors, reduced-motion, English, and French cases run
**Then** WCAG 2.2 AA, focus, source precedence, safe messaging, and equal-locale behavior pass
**And** raw controls, tooltip-only reasons, hidden source leakage, and color/motion-only meaning are absent.

**Requirements:** FR35, FR36, FR38, FR57, FR60, ARCH-15, NFR1, NFR2, NFR24, NFR48, NFR60, NFR61, NFR62, NFR63, NFR64, NFR70, UX-DR3, UX-DR11, UX-DR12, UX-DR13, UX-DR15, UX-DR22, UX-DR25, UX-DR26, UX-DR44, UX-DR47, UX-DR65, UX-DR67.

### Story 4.4: Classify Action Risk with an Independent Categorical Kernel

As an authorized user requesting AI assistance,
I want the requested effect classified by a deterministic governed contract,
So that low-risk eligibility and mandatory approval are decided consistently rather than inferred from task intent.

**Acceptance Criteria:**

**Given** an authorized supported action request and a valid deployed `ActionRiskClassifier`
**When** classification runs
**Then** the versioned categorical kernel reads the pinned AI allowlist entry, effect surface, tenant policy snapshot, requester authority, and Project/file/recipient/tool scopes
**And** its only classifier outputs are `low-risk` or `approval-required`, with classifier version and complete labelled input tuple.

**Given** the request modifies state, exposes files, sends externally, creates or assigns tasks, invokes an external tool, or acts on behalf of a participant
**When** the effect surface is classified
**Then** the result is structurally `approval-required`
**And** tenant policy, requester role, explanation, prior approval, or model output has no path to downgrade any of the six effect classes.

**Given** a product-declared read-only/no-external-effect subtype that the tenant has explicitly enabled
**When** its authority, scope, policy, context, and allowlist metadata are complete and current
**Then** it may classify `low-risk`
**And** catalog membership or a generic read label alone does not make a subtype eligible.

**Given** valid classifier machinery but missing tags, unknown effect surface, or undeclared authority class
**When** classification runs
**Then** the deterministic class is `approval-required`
**And** the indeterminate input is retained for review without performing an effect.

**Given** a missing, invalid, unqualified, failed, or non-contract classifier artifact/output
**When** classification is requested
**Then** it returns `classifier-unavailable`, writes no proposal/domain/idempotency success, and records the required auditable attempt
**And** it cannot silently substitute `approval-required`, `low-risk`, or an AI-generated classification.

**Given** authorization, evidence, audit, or product support fails before classification
**When** the request is evaluated
**Then** the user disposition is respectively safe `denied` or `unsupported` and the classifier is not invoked
**And** disposition remains distinct from any internal classifier result.

**Given** an optional M1 AI explanation or a reviewer disagreement
**When** it is recorded
**Then** the explanation cannot change the deterministic class, and the disagreement chain records classifier version/input, original class, reviewer/product decision, and resolution
**And** normal approval authorizes one proposal but never reclassifies a mandatory effect.

**Given** the exact deployed classifier and versioned evaluation partition
**When** quality is measured
**Then** evaluation misclassification is no greater than one percent and sampled-production reviewer disagreement is no greater than two percent
**And** those measurements remain separate from audit completeness and fail the applicable qualification when exceeded.

**Requirements:** FR39, FR40, FR41, FR46, ARCH-18, ARCH-30, ARCH-31, NFR7, NFR8, NFR15a, NFR16, NFR22, NFR32, NFR47, NFR50, NFR68, UX-DR30, UX-DR50.

### Story 4.5: Convert Reviewed Task Intent into a Governed Proposal

As an authorized task-intent reviewer,
I want a captured request converted into an immutable AI-action proposal,
So that any possible effect can be reviewed without losing its source or executing prematurely.

**Acceptance Criteria:**

**Given** a captured task intent remains reviewable and its source/evidence is current
**When** an authorized reviewer submits `ProposeAIAction`
**Then** the command links the proposal to the exact task-intent aggregate, immutable source/version, detector output, reviewer, stable operation ID, expected revision, current authority, and policy
**And** a terminally disposed, stale, corrected, unauthorized, or already converted intent cannot create another proposal.

**Given** the proposed action request
**When** pre-classification disposition and `ActionRiskClassifier` run
**Then** authorization/evidence/audit/allowlist failure yields `denied`, unsupported product operation yields `unsupported`, and supported input records the exact `low-risk|approval-required` classifier result/version/input tuple
**And** task-intent confidence or label is never reused as action risk.

**Given** a supported proposal is created
**When** its immutable record commits
**Then** it freezes requester/origin, tenant/Project, source intent/request, target product command and allowlist version, effect classes, resources/files and proposed redaction manifest, recipients/sender/delegation where applicable, tools, classifier tuple, policy snapshot, reversibility, expected post-state/events, required authority, approval requirement, proposal revision, expiry, operation, and correlation
**And** unavailable fields are explicitly not applicable rather than silently omitted.

**Given** one or more of the six mandatory boundary effects
**When** the proposal is constructed
**Then** its approval requirement is immutable and cannot be downgraded by tenant policy, reviewer, client, AI actor, or later editing
**And** the proposal enters `AwaitingApproval` without invoking a provider, exposing file content, mutating Project state, creating a task, invoking a tool, acting on behalf, or sending externally.

**Given** a product-declared low-risk request
**When** proposal conversion is attempted
**Then** the user is directed to the separate `ExecuteLowRiskAssistance` path unless an indeterminate or boundary effect requires `AwaitingApproval`
**And** no low-risk record can conceal a boundary-crossing effect.

**Given** repeated or concurrent conversion attempts
**When** they use the task intent's authoritative conversion slot
**Then** one immutable proposal is created, equivalent requests return it, and non-equivalent attempts receive a typed conflict
**And** the original task intent remains linked and unchanged.

**Given** the task-intent review surface after this story
**When** conversion is currently permitted
**Then** it exposes a governed Convert action with source/effect summary and consequence, uses the typed Client, and shows admitted/approval-required/denied/unsupported/failure before implying AI work
**And** validation failure preserves review context and focuses the linked error summary.

**Requirements:** FR36, FR37, FR39, FR41, FR45, FR46, FR55, FR60, FR90, ARCH-12, ARCH-17, ARCH-18, ARCH-31, NFR1, NFR13, NFR15a, NFR16, NFR47, NFR48, NFR50, NFR70, UX-DR26, UX-DR30, UX-DR31, UX-DR32, UX-DR47, UX-DR50.

### Story 4.6: Execute Eligible Read-Only Assistance Without External Effect

As an authorized Project contributor,
I want eligible read-only assistance returned through the governed pipeline,
So that I can receive useful help without creating an unreviewed Project or external effect.

**Acceptance Criteria:**

**Given** the exact immutable allowlist version contains `ChatBot.ExecuteLowRiskAssistance`, the tenant has enabled its declared subtype, and the classifier returns `low-risk`
**When** an authorized requester submits the command with stable operation ID, canonical request hash, Project scope, explicit safe context, policy, classifier, and expected revision
**Then** CommandGateway revalidates every input and may invoke only the configured governed AI-provider adapter for that subtype
**And** generic catalog membership, freeform tool requests, or unlisted subtypes remain ineligible.

**Given** A5 lacks the accepted provider contract, negative evidence, or current qualification for the exact candidate
**When** live assistance is requested
**Then** the command returns the safe provider-disabled/unavailable result without an external call or success record
**And** synthetic adapter tests cannot claim live-AI, onboarding, pilot, or M1 readiness.

**Given** an eligible request and A5-approved provider
**When** the context package is assembled and invoked
**Then** it is tenant/Project scoped, purpose-bound, policy-redacted, retention-governed, provider-reuse restricted, and records source/evidence/model/policy/redaction versions before invocation
**And** unauthorized sources, files, secrets, credentials, or hidden prompt content are excluded.

**Given** the provider returns a successful read-only response
**When** the result commits
**Then** it is attributed with AI actor, model/version, source/evidence provenance, completion state, operation, policy, redaction, canonical audit, and correlation
**And** it may persist only its request/result bookkeeping and never a Project message, file, task, outbound communication, tool effect, acted-on-behalf result, or AI-action proposal.

**Given** processing discovers state mutation, file exposure, external communication, task creation/assignment, tool invocation, acting on behalf, unknown effect, or indeterminate authority
**When** the request is evaluated before or during invocation
**Then** no effect or partial result is committed and the outcome is `approval-required`
**And** the user must create a separate immutable proposal through `ProposeAIAction`; this command cannot materialize one implicitly.

**Given** repeated equivalent requests or a transport retry
**When** the stable operation ID and canonical request hash are checked
**Then** the stored response/outcome is returned without another provider call
**And** a conflicting request returns `idempotency-conflict` without exposing prior content.

**Given** provider timeout, throttling, unsafe output, malformed response, policy drift, redaction failure, or audit unavailability
**When** assistance cannot complete safely
**Then** the operation exposes a typed failed/retryable/denied state with no committed partial Project content
**And** association review, human decisions, retry, and audit queries continue without live AI calls.

**Requirements:** FR40, FR43, FR44, FR46, FR55, FR59, FR68, FR80, FR90, ARCH-10, ARCH-18, ARCH-30, ARCH-31, ARCH-39, NFR4, NFR7, NFR8, NFR9, NFR10, NFR13, NFR16, NFR22, NFR32, NFR50, NFR68, UX-DR23, UX-DR30, UX-DR50.

### Story 4.7: Establish Mandatory Approval for Every Boundary Effect

As an authorized human reviewer,
I want every boundary-crossing AI proposal routed into an immutable approval subject,
So that no risky effect can execute from policy, classification, or automation alone.

**Acceptance Criteria:**

**Given** an immutable proposal containing state mutation, file exposure, external communication, task creation/assignment, external-tool invocation, or acting on behalf
**When** approval routing evaluates it
**Then** exactly one approval subject and authoritative decision slot are created in `AwaitingApproval`
**And** the proposal's mandatory-approval flag cannot be changed, bypassed, grouped away, or downgraded by tenant policy, surface, service client, AI actor, or reviewer.

**Given** the approval subject
**When** its review contract is assembled
**Then** it includes requester/origin, tenant/Project, source request/task intent, target command and immutable allowlist version, files/resources and redaction/freshness, recipients, sender/delegation, tools, classifier input/version/result, policy snapshot, reversibility, expected post-state/events, proposal and operation identities, expiry, required reviewer authority, and current state
**And** non-applicable fields are explicitly identified rather than omitted ambiguously.

**Given** evidence references, authority, policy, allowlist, or proposal content may age
**When** approval availability is queried
**Then** each required reference has one timestamped freshness state and the contract returns `enabled`, `disabled-with-reason`, or `not-applicable-hidden` for every decision
**And** expired evidence disables approval with `evidence-expired` and a safe refresh/revision action.

**Given** an actor is considered as reviewer
**When** eligibility is determined
**Then** current tenant, Project, resource, command, effect, and any separation-of-duty authority are checked against owner evidence
**And** UI roles, global-admin labels, stale mirrors, machine identities, and AI actors cannot grant human approval authority.

**Given** a proposal is `AwaitingApproval`
**When** any executor, worker, UI/API, CLI, MCP, or AI actor attempts the proposed effect
**Then** execution is denied before context/file access or owner command dispatch
**And** the proposal remains visibly pending and cannot resemble completed work.

**Given** proposal fields need to change
**When** a reviewer or requester attempts an in-place edit
**Then** the original proposal remains immutable and the contract requires a revision successor with new identity, expected revision, reclassification, policy/evidence snapshot, and approval slot
**And** the original approval subject cannot authorize the changed effect.

**Given** concurrent routing, duplicate proposal delivery, or approval-record persistence failure
**When** the subject is created
**Then** stable proposal identity produces one decision slot and one current approval subject or no committed state
**And** canonical audit/idempotency/policy references commit atomically with the record.

**Requirements:** FR41, FR42, FR44, FR45, FR55, FR61, FR68, FR90, ARCH-13, ARCH-17, ARCH-18, ARCH-31, NFR1, NFR13, NFR15a, NFR16, NFR47, NFR48, NFR50, NFR70, UX-DR30, UX-DR31, UX-DR32, UX-DR33, UX-DR50, UX-DR52.

### Story 4.8: Decide an AI Action Proposal Without Changing Its Scope

As an authorized human reviewer,
I want to approve, reject, request revision, or cancel a frozen AI-action proposal,
So that my decision is precise, current, and cannot authorize a different effect.

**Acceptance Criteria:**

**Given** an `AwaitingApproval` proposal and an eligible current reviewer
**When** `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, or permitted `CancelAIAction` is submitted
**Then** the command carries proposal and decision-slot identity, expected revision, reviewer authority, exact frozen context hash, evidence/policy/allowlist versions, rationale where required, operation, and correlation
**And** it passes through CommandGateway and the atomic canonical boundary.

**Given** the reviewer chooses Approve
**When** authority, tenant/Project, resources/files/redaction/freshness, recipients/sender/delegation, tool scope, classifier tuple, policy, allowlist, proposal content, expiry, and expected revision remain valid
**Then** the proposal transitions once to `Approved` for exactly its frozen effect and records reviewer/time and execution eligibility
**And** approval does not execute the effect, alter its classification, or authorize a changed command/resource/input.

**Given** the reviewer chooses Reject
**When** the decision commits
**Then** the proposal transitions once to `Rejected` with safe reason/rationale and no execution eligibility
**And** the source intent, proposal, evidence, classifier result, and decision remain immutable and inspectable.

**Given** the reviewer requests revision
**When** the decision commits
**Then** the current proposal becomes terminal `RevisionRequested` and records the requested changes
**And** any revised content requires a new linked proposal, reclassification, context/evidence/policy snapshot, revision, and decision slot.

**Given** cancellation is requested by an actor authorized for the current state
**When** it wins the proposal revision race
**Then** the proposal becomes terminal `Cancelled` with no execution eligibility
**And** cancellation after execution has committed returns the stored executed outcome and never reverses or conceals the effect.

**Given** stale/expired evidence, revoked authority, policy or allowlist drift, changed resources, proposal expiry, audit unavailability, or invalid state
**When** any decision is attempted
**Then** it is disabled or denied with the specific safe reason and required next action and no decision/effect commits
**And** unavailable approval remains focusable with its explanation in UI contracts.

**Given** concurrent or repeated decisions
**When** they target the one decision slot
**Then** first commit wins, equivalent retries return the stored decision, and later non-equivalent attempts receive current terminal outcome or typed conflict
**And** exactly one authoritative human decision and canonical audit chain exist.

**Requirements:** FR42, FR44, FR45, FR46, FR55, FR61, FR68, FR90, ARCH-13, ARCH-17, NFR1, NFR13, NFR15, NFR15a, NFR16, NFR36, NFR47, NFR48, NFR50, NFR51, NFR70, UX-DR32, UX-DR33, UX-DR50, UX-DR51.

### Story 4.9: Review and Preview the Frozen AI Action

As an authorized human reviewer,
I want one complete proposal and effect preview before deciding,
So that I understand the authority, data, recipients, command, and expected outcome I am authorizing.

**Acceptance Criteria:**

**Given** an authorized reviewer opens an AI-action proposal
**When** the live S3 route loads through the generated typed Client
**Then** a visibly pending proposal panel programmatically links the source request/task intent, Project, context manifest, action disposition, classifier details, and approval record
**And** pending, approved-awaiting-execution, rejected, revision-requested, cancelled, expired, invalidated, and unavailable states cannot resemble completed work.

**Given** complete review authority
**When** approval context renders
**Then** it shows requester/origin, Project, target command and allowlist version, files/resources and redaction/freshness, recipients, sender/delegation, tools, classifier version/input/result, policy snapshot, reversibility, proposal/operation identity, expected post-state, canonical audit events, and reviewer authority
**And** fields not applicable to the proposal are explicitly labelled as such.

**Given** outbound communication, file access, command execution, or an AI-generated change is proposed
**When** the preview is displayed
**Then** the exact frozen content/effect, affected resources, authority, redactions, freshness, and expected outcome are visible before approval
**And** preview performs no file exposure beyond the reviewer's current authority and no external or owner effect.

**Given** action classification is shown
**When** the reviewer inspects it
**Then** the prominent user disposition is `allowed-read-only`, `approval-required`, `denied`, or `unsupported`, with the subordinate internal `low-risk|approval-required` classifier output/version/input labelled separately where applicable
**And** task-intent confidence and action risk are never conflated.

**Given** Approve, Reject, Request revision, or Cancel is available
**When** controls render
**Then** they use the completed Story 4.8 typed commands; unavailable approval remains focusable with a programmatically associated safe explanation
**And** submission preserves context/focus, prevents pending duplicate submit, and shows the authoritative operation outcome without optimistic execution.

**Given** evidence expires or authority, policy, allowlist, file, recipient, sender, tool, or proposal revision drifts while the route is open
**When** the state refreshes or a decision is submitted
**Then** the affected action is disabled/denied with the exact safe reason and a fresh proposal/decision path
**And** one freshness indicator per evidence reference is announced once without forced scroll.

**Given** live S3 acceptance
**When** loading, complete review, insufficient authority, drift, every decision, denied, unsupported, degraded, conflict, terminal, keyboard, screen-reader, zoom, responsive, light/dark/forced-colors, reduced-motion, English, and French cases run
**Then** WCAG 2.2 AA, source/effect linkage, focus, redaction, and equal-locale checks pass
**And** no raw controls, hidden source text, stacked modal, tooltip-only reason, or color/motion-only status exists.

**Requirements:** FR42, FR44, FR45, FR46, ARCH-15, ARCH-16, NFR1, NFR2, NFR24, NFR47, NFR48, NFR60, NFR61, NFR62, NFR63, NFR70, UX-DR3, UX-DR7, UX-DR11, UX-DR12, UX-DR13, UX-DR15, UX-DR22, UX-DR25, UX-DR30, UX-DR31, UX-DR32, UX-DR33, UX-DR44, UX-DR47, UX-DR48, UX-DR50, UX-DR51, UX-DR58, UX-DR65, UX-DR67, UX-DR69.

### Story 4.10: Execute the Exact Approved Allowlisted Command Once

As an authorized requester with a human-approved proposal,
I want the exact frozen command executed through its owning service once,
So that the approved outcome reaches the Project conversation without scope drift or duplicate effect.

**Acceptance Criteria:**

**Given** an immutable current `Approved` proposal targeting `Project.AppendConversationMessage`
**When** `ExecuteApprovedProjectCommand` reaches execution admission
**Then** it revalidates requester and reviewer authority, tenant/Project/conversation, frozen content, files/redaction/freshness, recipients/sender/delegation if present, command and allowlist version, classifier/effects, policy, proposal revision, idempotency, and audit readiness immediately before dispatch
**And** any drift blocks execution and requires a new proposal/decision rather than editing the approved record.

**Given** A13 has not accepted the exact Conversations `AppendMessageCommand` v1 / `MessageAppended` producer mapping with compatible authority, expected-revision/guard, lifetime idempotency, atomic audit, retention, ACL, and fencing evidence
**When** execution is attempted
**Then** the request returns the safe A13-blocked outcome with no owner call or message effect
**And** the legacy `Project.` product prefix is never treated as Project ownership or permission to invent a target.

**Given** the exact A13 mapping is accepted and all checks pass
**When** execution begins
**Then** ChatBot atomically records orchestration intent and the narrowly delegated `ai-action-mediator-client` submits exactly one owner command carrying trusted tenant, authorized requester Party, correlation/causation, stable operation/message identity, governed-AI author, approved frozen text, bounded provenance, and accepted concurrency fields
**And** the client grant is limited to that requester, Project, proposal, approval, and command and expires on use, revocation, or five minutes.

**Given** Conversations commits `MessageAppended`
**When** the owner outcome is reconciled
**Then** the committed owner event/revision is authoritative and the proposal/execution status records one linked completed effect and canonical audit outcome
**And** the conversation projection displays the attributed approved AI message without ChatBot dual-writing owner state.

**Given** the owner effect committed but response or event delivery was interrupted
**When** retry/reconciliation occurs
**Then** the stored owner identity/revision is recovered as the same logical operation and the message is not submitted again as uncommitted work
**And** pending status remains truthful until the authoritative result is observed.

**Given** the allowlist contains any unknown member or the request targets outbound send, identity/role/policy/allowlist mutation, grants, administration, destructive file work, task effect, external tool, or unrestricted downstream command
**When** AI execution is requested
**Then** execution is denied as unsupported/not allowlisted without target resolution or effect
**And** tenant policy cannot add the member or override the denial.

**Given** success, duplicate, owner conflict, authorization denial, timeout/uncertainty, audit failure, and malformed owner response tests
**When** the live supported topology executes them
**Then** normalized result translation, event sequence, state-store end state, owner revision, canonical audit, and exactly-once effect are asserted
**And** no mock-only or metadata-preparation result can satisfy execution acceptance.

**Requirements:** FR41, FR43, FR44, FR45, FR46, FR55, FR59, FR68, FR80, FR81a, FR90, ARCH-10, ARCH-13, ARCH-14, ARCH-17, ARCH-31, ARCH-39, NFR1, NFR7, NFR13, NFR15a, NFR16, NFR19, NFR32, NFR34, NFR50, NFR70, UX-DR32, UX-DR51.

### Story 4.11: Inspect and Recover AI Mediation Outcomes Safely

As an authorized requester or reviewer,
I want to inspect proposals, decisions, executions, refusals, and failures as one linked history,
So that I can understand the outcome and retry only work that is safe to repeat.

**Acceptance Criteria:**

**Given** an authorized AI-action subject
**When** its history is queried
**Then** the typed result links source intent/request, classifier input/version/result, proposal versions, approval decisions, execution attempts, owner outcomes, denials/refusals, failures/retries, model/source provenance, operation identities, and canonical audit/correlation
**And** immutable records remain ordered and attributable without in-place repair.

**Given** an unsafe, unauthorized, policy-blocked, approval-missing, non-allowlisted, unsupported, malformed, or dependency-unavailable request
**When** mediation refuses it
**Then** the user receives the versioned existence-neutral disposition and one safe retry/escalate/dismiss/request-access action
**And** no target command, protected resource, external effect, or raw diagnostic detail is exposed.

**Given** an approved execution failed before the owner effect committed and the failure is explicitly retryable
**When** an authorized actor submits `RetryApprovedAIActionExecution`
**Then** a new immutable attempt links to the original proposal/approval/execution, uses current expected revision and Retry Profile, and revalidates all authority/policy/evidence/allowlist/effect/audit facts
**And** repeating the retry command returns the same successor attempt rather than creating multiple retries.

**Given** the owner effect committed or its commit status is uncertain
**When** retry is requested
**Then** the system reconciles the owner outcome and never repeats the request as uncommitted work
**And** committed results return the stored outcome while uncertainty remains pending with owner and safe next action.

**Given** AI provider failure or outage
**When** proposal/approval/retry/audit operations that need no live AI are used
**Then** they remain available with existing frozen content and evidence as authorized
**And** no live call is fabricated, no safety gate is weakened, and only the affected AI scope degrades.

**Given** execution or retry reaches an invalid lifecycle state, stale decision, exhausted ceiling, poison work, or terminal failure
**When** the operation is handled
**Then** the transition rejects before mutation or enters its declared terminal/quarantine state with reason, attempts, owner, next action, and correlation
**And** unrelated tenant/Project work continues where isolation permits.

**Given** the S3 route receives an advisory status update
**When** it refreshes the authoritative record
**Then** pending, approved, executing, completed, denied, unsupported, failed, retryable, cancelled, invalidated, and terminal outcomes display inline with attribution and safe recovery
**And** no toast, color, motion, or partial content alone communicates final outcome.

**Requirements:** FR44, FR46, FR55, FR59, FR65, FR66, FR68, FR80, FR90, ARCH-14, ARCH-17, ARCH-26, NFR2, NFR13, NFR15, NFR17, NFR18, NFR19, NFR22, NFR32, NFR34, NFR39, NFR40, NFR41, NFR50, NFR51, NFR58, NFR70, UX-DR31, UX-DR37, UX-DR44, UX-DR45, UX-DR65.

### Story 4.12: Invalidate AI Work When Association or Authority Changes

As an authorized requester or reviewer,
I want pending AI work invalidated when its Project context or authority changes,
So that stale approval cannot execute against corrected or unauthorized evidence.

**Acceptance Criteria:**

**Given** AI task-intent, proposal, approval, context-package, or pending-execution records reference association-derived context
**When** their stores are introduced
**Then** each registers its source-version-guarded invalidation/rebuild and acknowledgement contract with the Story 2.11 correction registry
**And** missing registration fails architecture tests before the store can activate.

**Given** an association enters `Correcting` or `Correction-delayed`
**When** any referenced AI request, context package, proposal, approval, or execution is queried or used
**Then** current Project-context use and execution are blocked immediately and the item shows correcting state, source-version mismatch, owner, and next action
**And** no cached projection, prior approval, or service-client grant can override the block.

**Given** an unexecuted proposal or approval consumed the old association
**When** its correction activity runs
**Then** it becomes immutable `Invalidated`, records the correction/predecessor link and affected evidence, and acknowledges only after its derived data and delegated grants are revoked or rebuilt
**And** a new action requires fresh detection/review as applicable, classification, proposal, policy/evidence, approval, and operation identity.

**Given** an owner effect already committed before correction
**When** correction propagation evaluates it
**Then** the effect remains in immutable history and is never falsely erased or retried
**And** audit and conversation context link the effect to the later correction and any separately governed compensating/superseding action.

**Given** authority, policy, allowlist, evidence freshness, file, recipient, sender, tool, or owner contract changes without association correction
**When** an approved action is about to execute
**Then** immediate revalidation blocks drift and records the precise safe invalidation/failure reason
**And** the old approval cannot authorize a refreshed or altered effect.

**Given** all affected AI stores complete invalidation/rebuild for the corrected source version
**When** they acknowledge propagation
**Then** completion follows the existing correction aggregate's required-store contract and AI context remains blocked until the final acknowledgement
**And** delayed handling preserves the p95 target, P2 escalation, progress, and incident history.

**Given** live S3 and conversation views
**When** invalidation, correction progress, delayed propagation, completed correction, or a committed pre-correction effect renders
**Then** predecessor/successor, stale/current state, affected proposal/execution, acknowledged/remaining stores, owner, and safe re-proposal action are visible as authorized
**And** the UI never implies that correction reverses an irreversible or externally committed effect.

**Requirements:** FR7, FR44, FR46, FR60, FR63, FR91a, ARCH-17, ARCH-18, ARCH-25, NFR13, NFR16, NFR17, NFR17a, NFR34, NFR47, NFR48, NFR50, NFR51, NFR70, UX-DR25, UX-DR31, UX-DR32, UX-DR34, UX-DR51, UX-DR59.

### Story 4.13: Complete the Live Governed AI Mediation Journey

As an authorized Project contributor or reviewer,
I want one coherent live journey from detected intent through safe outcome,
So that I can understand and control AI assistance without bypassing authority or approval.

**Acceptance Criteria:**

**Given** captured actionable intent
**When** the user follows the live task-intent and S3 routes
**Then** source review, terminal dispositions, governed conversion, action disposition/classification, proposal preview, human decisions, execution status, history, retry, and correction invalidation use their completed typed contracts
**And** each transition displays authoritative operation/state before enabling its next action.

**Given** an eligible read-only/no-external-effect subtype
**When** A5, authority, context, policy, classifier, and allowlist prerequisites pass
**Then** the user receives an attributed result with model/source provenance and completion state through `ExecuteLowRiskAssistance`
**And** any discovered boundary effect returns `approval-required` without an effect or implicit proposal.

**Given** each of state mutation, file exposure, external communication, task creation/assignment, external-tool invocation, and acting on behalf
**When** its end-to-end fixture is submitted
**Then** it creates an immutable proposal and cannot execute without one current authorized human approval for the exact frozen effect
**And** automated tests prove no tenant policy, actor, classifier explanation, surface, or retry downgrades the requirement.

**Given** an approved exact allowlisted command and an accepted owner mapping
**When** execution succeeds, duplicates, conflicts, becomes uncertain, fails, or is retried
**Then** the route shows the owner-authoritative state, exact-once outcome, operation/attempt lineage, safe reason, audit result, and next action
**And** only `Project.AppendConversationMessage` and `ChatBot.ExecuteLowRiskAssistance` can be AI-invocable in M1, with their distinct risk and execution contracts.

**Given** denied, unsupported, classifier-unavailable, provider-outage, approval-missing, evidence-expired, authority/policy/allowlist drift, correcting, invalidated, cancelled, failed, retryable, or terminal state
**When** the journey renders
**Then** the user sees the versioned disposition, state, owner, and one safe action without raw diagnostics or protected-resource confirmation
**And** human review, existing-proposal decisions, retry, and audit remain available during AI-provider outage where no live call is required.

**Given** source evidence, AI summary/result, proposal, approval, and owner outcome appear together
**When** they render
**Then** source remains expanded and authoritative, generated content stays labelled/provenance-first, pending work cannot resemble completion, and all actors/origins are attributed
**And** copy/export/read-aloud/accessibility output preserves the same redaction and authority distinctions.

**Given** live acceptance across task-intent and S3 surfaces
**When** success, loading, empty, validation, insufficient authority, every decision, drift, execution progress/outcome, denial, unsupported, outage, degraded, conflict, retry, correction, keyboard, screen-reader, zoom, responsive, light/dark/forced-colors, reduced-motion, English, and French cases run
**Then** WCAG 2.2 AA, focus, safe messaging, Fluent/FrontComposer, redaction, and equal-locale checks pass non-vacuously
**And** no raw controls, freeform ungoverned execution, stacked modals, hidden source leakage, or color/motion/toast-only meaning exists.

**Given** increment qualification evidence
**When** the journey is assessed for M0 or M1
**Then** it binds exact runtime, source, provider, detector/classifier, policy, allowlist, owner mapping, dataset, topology, test, runner, time, and independent-verification provenance
**And** open A5, A6, or A13 evidence blocks the claim even when synthetic end-to-end tests pass.

**Requirements:** FR35, FR36, FR37, FR38, FR39, FR40, FR41, FR42, FR43, FR44, FR45, FR46, ARCH-15, ARCH-31, ARCH-33, ARCH-37, ARCH-39, NFR1, NFR2, NFR7, NFR8, NFR9, NFR10, NFR13, NFR15a, NFR16, NFR17, NFR22, NFR24, NFR32, NFR34, NFR39, NFR40, NFR47, NFR48, NFR50, NFR60, NFR61, NFR62, NFR63, NFR65, NFR68, NFR70, UX-DR3, UX-DR6, UX-DR7, UX-DR11, UX-DR12, UX-DR13, UX-DR15, UX-DR20, UX-DR22, UX-DR23, UX-DR25, UX-DR26, UX-DR30, UX-DR31, UX-DR32, UX-DR33, UX-DR37, UX-DR44, UX-DR45, UX-DR47, UX-DR48, UX-DR50, UX-DR51, UX-DR54, UX-DR58, UX-DR65, UX-DR67, UX-DR68, UX-DR69.

## Epic 5: Cross-Surface Parity — CLI & MCP

Developers and AI/automation clients can perform the singular governed workflow parity set through CLI and MCP with the same authorization, transitions, redaction, idempotency, status, and audit outcomes as UI/API.

### Story 5.1: Pin the Singular M1 Parity Contract

As a product and platform owner,
I want one machine-readable versioned parity manifest,
So that UI, CLI, and MCP implement exactly the agreed governed operations and outcomes.

**Acceptance Criteria:**

**Given** the M1 parity manifest
**When** its operation families are inspected
**Then** it contains exactly intake-status inspection, candidate review, confirm/reject/defer/correct association, attachment storage/status inspection, task-intent capture/status, AI-action approval decisions, approved-command execution, retry, operation status, and audit lookup
**And** ingestion and attachment storage may remain event-driven while their status and governed decisions are represented equivalently.

**Given** each parity row
**When** its contract is validated
**Then** it maps to the canonical OpenAPI/typed Client operation and declares allowed actor/session types, immutable origin, authorization/redaction, normalized input, states/transitions, safe reasons, idempotency/conflicts, audit result, and applicable UI/CLI/MCP exposure
**And** no row invents a public command, owner target, or lifecycle state.

**Given** the public operation catalog, per-surface exposure policy, MCP tags, AI allowlist, and owner executable mappings
**When** synchronization checks run
**Then** they remain distinct deny-by-default artifacts linked by explicit versioned mappings
**And** catalog or MCP exposure never adds an AI-invocable allowlist member or implies an owner implementation exists.

**Given** an operation is added, removed, renamed, or semantically changed in one surface
**When** the manifest and generated-contract checks execute
**Then** the build fails until an explicit compatible version/change decision and all required surface mappings/tests are present
**And** silent reduction or expansion of the M1 exit set is rejected.

**Given** the manifest includes a human decision or approved execution
**When** its MCP actor policy is inspected
**Then** only a current human-delegated MCP session with recorded user presence and `actorType=human` is eligible
**And** AI, tool, mediator, execution, and generic service principals receive a structural denial.

**Given** M0 release evidence or documentation
**When** parity claims are scanned
**Then** CLI and MCP are identified as M1 scope and thin test shims cannot satisfy production parity
**And** no M1 exit or governed machine-surface adoption is claimed until every manifest row passes production-adapter conformance.

**Given** the parity-manifest test
**When** one required row, field, surface mapping, negative actor case, or outcome assertion is removed
**Then** the test fails non-vacuously and identifies the missing contract
**And** an empty manifest or zero executed cases cannot pass.

**Requirements:** FR82, FR83, FR84, FR85, FR86, ARCH-15, ARCH-31, ARCH-32, ARCH-39, NFR32, NFR33, NFR65, NFR70, UX-DR53, UX-DR61, UX-DR70.

### Story 5.2: Authenticate and Tenant-Bind the CLI User

As an authorized CLI user,
I want a tenant-scoped delegated session using the same typed service boundary as the UI,
So that command-line access cannot bypass current human authority or governance.

**Acceptance Criteria:**

**Given** a user starts CLI authentication
**When** the OAuth device flow completes
**Then** the `cli-automation-client` session binds the exact Keycloak subject, source user, tenant, delegated scopes, grant evidence, issue/expiry times, and immutable `CLI` origin
**And** no password, token, device code, refresh secret, or protected claim appears in command output, shell history guidance, logs, traces, audit, or support artifacts.

**Given** a valid CLI session
**When** an operation is submitted
**Then** the CLI uses only the generated typed Client and current server-side tenant, membership, Project/resource, action, and policy authorization
**And** it cannot access databases, actors, queues, mailbox/index/tenant stores, projections, or internal gateway stages directly.

**Given** a delegated CLI credential
**When** its lifetime is evaluated
**Then** it expires within seven days and requires refresh through the governed flow
**And** logout, revocation, user/tenant deactivation, permission change, or explicit grant revocation is recognized within the declared staleness limits.

**Given** missing, expired, revoked, malformed, over-scoped, under-scoped, cross-tenant, or case-mismatched credential/owner evidence
**When** the CLI calls any operation
**Then** the request fails closed with the versioned existence-neutral safe code and nonzero documented exit status
**And** no protected resource, prior output, or mutation is returned.

**Given** a CLI user has a human product role
**When** a parity operation is authorized
**Then** the current human and owner-side resource authority is evaluated for that operation
**And** the machine client itself never inherits or substitutes for the human role.

**Given** a CLI request reaches CommandGateway
**When** its command record and audit are inspected
**Then** the canonical semantic tuple matches the equivalent UI/API intent and the adapter contributes only immutable `CLI` origin and delegated-session evidence
**And** origin cannot be changed by downstream stages or user flags.

**Requirements:** FR82, FR84, FR85, FR86, ARCH-8, ARCH-12, ARCH-15, ARCH-22, ARCH-32, NFR1, NFR2, NFR4, NFR5, NFR6, NFR7, NFR10, NFR32, NFR34, NFR67, UX-DR53, UX-DR61.

### Story 5.3: Inspect the Full Parity Set from the CLI

As an authorized CLI user,
I want machine-readable and human-readable inspection of governed workflow state,
So that I can investigate and prepare decisions without a UI-only dependency.

**Acceptance Criteria:**

**Given** a valid tenant-bound CLI session
**When** the user invokes intake status, candidate/evidence review, attachment storage/status, task-intent status, AI proposal/approval status, operation status, or audit lookup
**Then** the CLI calls the corresponding canonical typed query such as `GetEmailAssociationStatus`, `ListProjectAssociationCandidates`, `GetProjectAssociationEvidence`, `GetAttachmentStorageStatus`, `GetTaskIntentStatus`, `GetAIActionProposal`, `GetApprovalStatus`, `GetWorkflowOperationStatus`, or `GetAuditHistory`
**And** no CLI-specific read model, authorization, state, or reason mapping is introduced.

**Given** an ambiguous association query
**When** candidates are returned
**Then** CLI output preserves the same safe ordered candidates, evidence snippets, confidence values/bands, reason codes, visibility rules, freshness, and current revision as UI/API
**And** suppressed candidates cannot be inferred from totals, ordering gaps, verbose output, or errors.

**Given** `--output json` or the equivalent machine-readable mode
**When** any parity query succeeds or fails
**Then** output follows the canonical camelCase schema with stable codes/IDs/states, UTC timestamps, opaque cursors, redaction, operation/correlation, and no decorative prose
**And** human-readable mode preserves the same facts and safe outcome without becoming a second contract.

**Given** a list result larger than one page
**When** the user continues with the returned cursor
**Then** the CLI passes the opaque tenant/query-scoped cursor unchanged and preserves stable server ordering
**And** forged, expired, or cross-scope cursors fail safely.

**Given** the user lacks tenant, Project, file, approval, command, or audit detail authority
**When** a query runs
**Then** output is existence-neutral and applies the same server-side redaction as UI/API
**And** verbose/debug mode, exit messages, logs, or serialization cannot reveal the restricted detail.

**Given** a query is long-running or the result is not ready
**When** five seconds p95 is reached or a connection would exceed 30 seconds
**Then** the CLI returns operation identity/current state, retry count, partial-output metadata, safe next action, terminal reason, and correlation for later polling
**And** it does not hold the connection or falsely report completion.

**Given** success, empty, unauthorized, degraded, retryable, terminal, pagination, and redaction fixtures for each read family
**When** CLI integration tests run
**Then** normalized contract output and server-side state match the UI/API arm
**And** every manifest read row executes non-vacuously.

**Requirements:** FR82, FR84, FR85, FR86, ARCH-15, ARCH-16, ARCH-32, ARCH-41, NFR1, NFR2, NFR10, NFR26, NFR27, NFR32, NFR34, NFR38, NFR39, NFR40, NFR67, UX-DR37, UX-DR53, UX-DR61, UX-DR65.

### Story 5.4: Perform Governed Parity Decisions from the CLI

As an authorized CLI user,
I want to submit the parity-set decisions and recoveries with explicit review context,
So that command-line operations have the same governed effect as UI/API actions.

**Acceptance Criteria:**

**Given** a valid CLI session and current authorized item state
**When** the user confirms, rejects, defers, or corrects association; captures task intent; approves, rejects, requests revision, or cancels an AI proposal; executes an approved command; or invokes a declared retry
**Then** the CLI constructs only the corresponding canonical typed command with actor, tenant, target, stable operation ID, expected revision, decision slot where applicable, source attribution, policy/approval references, and correlation
**And** every mutation enters the single CommandGateway through the generated Client.

**Given** a human decision or boundary effect
**When** the CLI prepares submission
**Then** human-readable mode shows the same safe target, evidence/freshness, authority/effects, command/allowlist, consequences, and expected outcome required by the canonical review contract
**And** non-interactive mode requires explicit complete inputs and never assumes confirmation from a missing prompt.

**Given** an association decision
**When** it commits through CLI
**Then** candidate identity/order/evidence revision and normalized disposition match the equivalent UI/API input and the resulting event/state/audit are equivalent
**And** the CLI cannot submit a suppressed candidate or bypass stale-evidence validation.

**Given** an AI approval decision or approved execution
**When** it is submitted
**Then** current human authority and frozen proposal/effect context are revalidated and all six mandatory effects remain approval-required
**And** the CLI client identity, automation mode, or flags cannot self-approve, weaken policy, or expand the AI allowlist.

**Given** an attachment storage step remains event-driven
**When** CLI parity is evaluated
**Then** the CLI exposes its canonical status and governed retry/decision where supported without inventing a manual storage command
**And** an owner action appears only if the parity manifest maps it to an existing public contract.

**Given** repeated, conflicting, stale, unauthorized, invalid-state, denied, unsupported, degraded, retryable, or terminal submission
**When** CommandGateway responds
**Then** CLI output and exit status preserve the canonical operation, state, safe reason, redaction, idempotency/prior outcome, audit result, and next action
**And** a transport retry cannot duplicate a decision or owner effect.

**Given** CLI integration tests for every mutation row in the parity manifest
**When** success, rejection, retry, conflict, revocation, and audit-unavailable cases execute
**Then** normalized command records, event sequences, state-store/owner end state, immutable `CLI` origin, and canonical audit are asserted
**And** a shim, mock-only result, or zero-case test cannot satisfy production CLI parity.

**Requirements:** FR82, FR84, FR85, FR86, ARCH-12, ARCH-13, ARCH-15, ARCH-31, ARCH-32, NFR1, NFR7, NFR13, NFR15, NFR15a, NFR16, NFR18, NFR32, NFR34, NFR50, NFR67, NFR70, UX-DR32, UX-DR33, UX-DR53, UX-DR61.

### Story 5.5: Separate Human-Delegated and Tool-Scoped MCP Identity

As an authorized MCP participant,
I want the server to distinguish current human delegation from AI/tool identity,
So that machine access cannot impersonate a person or make human decisions.

**Acceptance Criteria:**

**Given** a human begins an MCP-delegated session
**When** authentication and presence validation complete
**Then** `mcp-human-delegated-client` binds exact source user, `actorType=human`, current user-presence evidence, tenant, scopes, OAuth/delegation evidence, issue/expiry, and immutable `MCP` origin
**And** the session lasts no more than one hour and terminates on logout, revocation, lost presence, or authority loss.

**Given** an AI or tool obtains MCP access
**When** `mcp-tool-client` is issued
**Then** it binds the exact tenant, AI/tool actor, tool-scoped grant, allowed query/proposal/eligible-low-risk set, issue/expiry, and immutable origin
**And** it lasts no more than 24 hours, requires refresh, and never carries `actorType=human` or a human-decision scope.

**Given** either MCP identity calls the service
**When** request context is constructed
**Then** current server-side tenant, owner, Project/resource, action, session, presence, delegation, and exposure-policy authority is evaluated before any data access or command handling
**And** claims, IDs, prompt text, or MCP tool metadata cannot broaden that authority.

**Given** expired, revoked, malformed, over-scoped, under-scoped, cross-tenant, case-mismatched, presence-lost, or contradictory evidence
**When** an MCP operation is attempted
**Then** the call fails closed with an existence-neutral structured safe result and no protected data/effect
**And** revocation/presence tests meet the declared staleness bound.

**Given** an MCP credential or delegated token
**When** logs, traces, audit, tool results, protocol errors, support artifacts, or test output are inspected
**Then** secrets and bearer material are absent while safe actor/session/scope/expiry/correlation metadata remains
**And** no debug mode reveals the credential or restricted claims.

**Given** an MCP command reaches CommandGateway
**When** its canonical record is inspected
**Then** semantic command fields match an equivalent authorized UI/API intent and the adapter adds only immutable `MCP` origin plus actor/delegation/presence evidence
**And** downstream stages cannot mutate origin or convert a tool actor into a human actor.

**Requirements:** FR83, FR84, FR85, FR86, ARCH-11, ARCH-12, ARCH-15, ARCH-22, ARCH-32, NFR1, NFR2, NFR4, NFR5, NFR6, NFR7, NFR10, NFR32, NFR34, NFR67, UX-DR53, UX-DR61.

### Story 5.6: Expose the Restricted MCP Tool Surface

As an authorized MCP tool or AI client,
I want only the actor-visible query, proposal, and eligible low-risk capabilities,
So that tool use cannot cross into human decisions or privileged administration.

**Acceptance Criteria:**

**Given** a valid `mcp-tool-client` session
**When** the server publishes its MCP tools
**Then** exposure is limited to the closed versioned actor-visible status/evidence queries plus `ProposeAIAction` and eligible `ExecuteLowRiskAssistance`
**And** each tool delegates solely to the generated typed Client and its canonical server operation.

**Given** association confirmation/rejection/deferral/correction, `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, approved execution, outbound communication, policy, permission/grant, safety-control, queue-item, retention, or other administrative mutation
**When** MCP tool exposure and runtime authorization are evaluated
**Then** the capability is structurally absent or denied for AI/tool/service principals
**And** prompt content, tenant policy, MCP tags, catalog membership, or delegated requester identity cannot enable it.

**Given** an actor-visible query
**When** the tool calls it
**Then** authorization, ordering, evidence/freshness, state/reason, redaction, cursor, operation, and audit outcome are equivalent to UI/API for that actor and resource
**And** tool descriptions, schemas, errors, counts, and results reveal no suppressed candidates or restricted detail.

**Given** `ProposeAIAction` is exposed
**When** the tool submits a request
**Then** it can create only an immutable reviewable proposal under the originating human/Project authority and cannot approve or execute it
**And** the audit chain retains the MCP tool/AI actor, source requester/delegation, tenant/Project, operation, and correlation.

**Given** `ExecuteLowRiskAssistance` is exposed
**When** the request is classified
**Then** only a product-declared tenant-enabled read-only/no-external-effect subtype can execute and any boundary or indeterminate effect returns `approval-required`
**And** MCP exposure does not add or modify an AI allowlist member.

**Given** an expired/revoked grant, unauthorized resource, unsupported tool, schema mismatch, unsafe payload, audit failure, or provider outage
**When** a tool call runs
**Then** it returns the canonical structured safe outcome with no effect or restricted detail
**And** transport/protocol error data contains no credentials, PII, prompts, source content, or raw exceptions.

**Given** MCP tool integration and negative exposure tests
**When** every allowed and forbidden row is exercised
**Then** normalized commands/results, immutable origin, event/state/audit outcomes, and structural denials match the parity manifest
**And** an empty tool list, shim, mock-only response, or omitted forbidden-case test cannot pass.

**Requirements:** FR83, FR84, FR85, FR86, ARCH-8, ARCH-12, ARCH-15, ARCH-31, ARCH-32, NFR1, NFR2, NFR4, NFR7, NFR8, NFR10, NFR16, NFR32, NFR34, NFR50, NFR67, NFR70, UX-DR30, UX-DR50, UX-DR53, UX-DR61.

### Story 5.7: Perform Human-Delegated Parity Decisions through MCP

As a currently present authorized human using an MCP client,
I want my permitted parity-set decisions submitted with explicit human delegation,
So that MCP can assist my workflow without turning an AI or tool into the decision maker.

**Acceptance Criteria:**

**Given** a valid `mcp-human-delegated-client` session with current user presence and `actorType=human`
**When** its tool/resource surface is listed
**Then** it exposes the delegating human's authorized M1 parity queries and mutations according to the pinned manifest
**And** exposure remains bounded by current tenant, Project/resource, role, action, delegation, and surface policy.

**Given** a candidate review or other read operation
**When** the human-delegated MCP client invokes it
**Then** safe ordered data, evidence/freshness, state/reason, redaction, operation, cursor, and audit outcome match the equivalent UI/API actor request
**And** presentation differences do not alter semantics or reveal suppressed data.

**Given** an association decision/correction, AI approval/rejection/revision/cancellation, approved execution, or other human-decision mutation in the parity set
**When** it is submitted
**Then** current user presence and `actorType=human` are revalidated at that submission, and the canonical command records the delegating human as actor and `MCP` as immutable origin
**And** complete expected revision, decision slot, frozen review context, operation identity, delegation evidence, and correlation are required.

**Given** user presence is lost, session expired/revoked, the human logs out, or current authority changes
**When** a human decision is attempted
**Then** it is structurally denied before mutation with the safe presence/authority reason
**And** a tool, AI, mediator, execution, or generic service principal cannot continue or resubmit it as the human.

**Given** the same proposal originated through MCP or will be executed through an AI/service client
**When** approval authority is evaluated
**Then** only the current human actor may decide under their own authority
**And** origin, proposal ownership, automation, or execution responsibility grants no self-approval path.

**Given** repeated, conflicting, stale, unauthorized, invalid-state, denied, unsupported, audit-unavailable, or transport-ambiguous submission
**When** the server responds
**Then** the structured outcome preserves canonical state, safe reason, redaction, idempotency/prior result, operation, audit, and next action
**And** no decision or owner effect is duplicated.

**Given** human-delegated MCP integration tests for every eligible parity row
**When** success, lost-presence, wrong-actor-type, revocation, conflict, retry, and denial cases execute
**Then** normalized command, event sequence, state-store/owner end state, human actor, immutable `MCP` origin, and canonical audit match the manifest
**And** production parity cannot pass using the restricted tool client or a test shim.

**Requirements:** FR83, FR84, FR85, FR86, ARCH-12, ARCH-13, ARCH-15, ARCH-22, ARCH-32, NFR1, NFR2, NFR6, NFR7, NFR13, NFR15a, NFR16, NFR32, NFR34, NFR50, NFR67, NFR70, UX-DR32, UX-DR33, UX-DR53, UX-DR61.

### Story 5.8: Preserve Immutable Origin on Canonically Normalized Commands

As a security and audit reviewer,
I want equivalent surface inputs normalized identically while retaining truthful origin,
So that behavior is comparable without losing who or what submitted the operation.

**Acceptance Criteria:**

**Given** equivalent semantic intent from UI/API, CLI, MCP, worker, mailbox event, service client, or AI actor
**When** its adapter constructs the typed command
**Then** actor, tenant, target/resource IDs, operation, expected revision, decision slot, source/evidence, policy/approval, normalized payload, and correlation follow the one canonical normalization contract
**And** key ordering, whitespace, Unicode normalization, identifier casing rules, time, and JSON serialization cannot produce surface-specific semantics.

**Given** an adapter boundary
**When** a command is created
**Then** the adapter attaches exactly one immutable declared origin plus its required session/event/delegation evidence
**And** user payload, command flags, prompts, forwarded headers, or later gateway stages cannot spoof, omit, or rewrite origin.

**Given** two equivalent commands from different surfaces
**When** the parity oracle compares them
**Then** their semantic tuples and canonical request hashes match while their separately asserted origin fields have the expected values
**And** the oracle does not byte-compare origin-bearing envelopes or discard origin to force a pass.

**Given** a command is authorized, denied, conflicts, retries, or commits
**When** events, idempotency, status, audit, logs, and traces are inspected
**Then** immutable origin propagates from adapter boundary through the canonical outcome with correlation
**And** redaction removes protected session/credential data without erasing actor class or safe origin attribution.

**Given** an adapter attempts to call an aggregate/store directly or replicate authentication, tenant binding, authorization, risk, approval, idempotency, lifecycle, or audit logic
**When** architecture tests run
**Then** the build fails and identifies the forbidden dependency/stage
**And** UI, CLI, and MCP production adapters remain typed-Client-only shells.

**Given** a contract or normalization version changes
**When** compatibility checks run
**Then** additive-compatible changes preserve prior normalized meaning and breaking changes require explicit version, migration, and synchronized parity rows
**And** mixed or unknown versions fail closed instead of normalizing approximately.

**Requirements:** FR84, FR85, FR86, ARCH-8, ARCH-12, ARCH-15, ARCH-16, ARCH-32, ARCH-41, NFR4, NFR10, NFR13a, NFR32, NFR33, NFR34, NFR50, NFR70, UX-DR20, UX-DR53, UX-DR61.

### Story 5.9: Prove Differential Conformance Across Production Surfaces

As a release reviewer,
I want every parity operation exercised through its production adapters,
So that equivalent inputs are proven to reach equivalent governed outcomes.

**Acceptance Criteria:**

**Given** the pinned parity manifest
**When** the differential suite enumerates cases
**Then** every required read, decision, execution, retry, status, and audit row has positive and applicable unauthorized, redacted, stale, conflict, duplicate, retry, degraded, terminal, and audit-unavailable cases
**And** each case declares its eligible UI/API, CLI, human-delegated MCP, and restricted MCP arms plus expected structural denials.

**Given** equivalent authorized semantic input through eligible production adapters
**When** a case executes in the supported topology
**Then** normalized commands/queries, authorization result, ordered data, transitions, safe reason, redaction, idempotency/prior outcome, event sequence, state-store/owner end state, operation status, and canonical audit are equivalent
**And** only the expected immutable origin and session/delegation evidence differ.

**Given** ambiguous association candidate review
**When** it runs across eligible surfaces
**Then** safe candidate order, evidence snippets, confidence/bands, reason codes, visibility rules, freshness, and current revision match exactly
**And** unauthorized candidates are absent from every result and diagnostic channel.

**Given** a human decision through MCP
**When** current presence and `actorType=human` are valid or invalid
**Then** the human-delegated arm matches UI/CLI behavior when valid and receives the specified structural denial when invalid
**And** restricted tool/AI/service arms are denied regardless of prompt or policy.

**Given** an approved execution or retry produces an owner effect
**When** transport failure, duplicate delivery, or concurrency is injected
**Then** each surface converges on the same single owner-authoritative outcome and audit chain
**And** no adapter repeats a committed effect or uses a surface-local recovery path.

**Given** thin CLI/MCP test shims from M0
**When** production conformance runs
**Then** shims are excluded or removed and each required arm proves the actual shipping adapter and generated Client path
**And** mock-only, HTTP-status-only, empty, skipped, or zero-assertion cases fail the suite.

**Given** any manifest row, production arm, expected denial, normalization assertion, event/state end-state assertion, redaction check, or audit assertion is missing or divergent
**When** M1 parity validation runs
**Then** the parity gate fails with the exact row/surface/outcome difference
**And** partial success cannot support an M1 exit or machine-surface adoption claim.

**Given** all parity cases pass
**When** release evidence is assembled
**Then** it binds exact manifest/OpenAPI/client, UI/CLI/MCP/runtime revisions, topology, identities, policy/allowlist, fixtures, commands, runner, time, and results
**And** open A5, A6, or A13 still blocks M1 readiness independently of parity success.

**Requirements:** FR82, FR83, FR84, FR85, FR86, ARCH-15, ARCH-24, ARCH-31, ARCH-32, ARCH-33, ARCH-37, ARCH-39, NFR1, NFR2, NFR7, NFR11, NFR13, NFR15a, NFR16, NFR32, NFR34, NFR50, NFR65, NFR67, NFR70, UX-DR53, UX-DR61, UX-DR70.

### Story 5.10: Confirm Live Cross-Surface Attribution and Adoption

As an authorized workflow user or pilot reviewer,
I want to see how an operation was normalized and attributed across surfaces,
So that I can trust machine-surface use and recover safely from parity failures.

**Acceptance Criteria:**

**Given** an authorized parity-set operation from UI/API, CLI, or MCP
**When** the S7 Cross-surface Attribution route loads
**Then** it shows current/stale parity-contract version, normalized operation and target, user disposition, lifecycle state, pending/prior/conflict outcome, immutable actor/origin, safe redaction, idempotency, audit result, and correlation
**And** source user, delegation, or presence evidence appears only to viewers authorized for that detail.

**Given** equivalent operations from different surfaces
**When** their records are compared
**Then** the view presents equivalent semantic command/state/outcome fields and clearly identifies the expected origin difference
**And** presentation differences never hide an authorization, redaction, transition, reason, idempotency, or audit divergence.

**Given** adapter parity failure, stale manifest/client, lost MCP presence, expired CLI/MCP credential, structural tool denial, conflict, duplicate, retryable failure, or unsupported operation
**When** S7 or the originating machine surface reports it
**Then** the exact safe state, responsible surface/owner, and CLI/MCP recovery guidance are shown without raw diagnostics or restricted data
**And** retry guidance uses the canonical operation/status path rather than a surface-local bypass.

**Given** a pilot workflow uses CLI or MCP for one parity-set operation
**When** governed machine-surface adoption is evaluated
**Then** evidence proves the production adapter, current human/tool identity as applicable, typed Client, CommandGateway, authorization, approval, idempotency, owner effect, status, and canonical audit path
**And** the workflow did not use a shim, direct store, broad credential, mock owner, or skipped parity assertion.

**Given** no real eligible pilot workflow exists or any parity/gate evidence is missing, stale, partial, mismatched, or historical
**When** SM15 or M1 exit is assessed
**Then** adoption remains unproven and the route/report identifies the missing support rather than inferring readiness
**And** open A5, A6, or A13 blocks the M1 claim even if S7 and conformance tests otherwise pass.

**Given** live S7 acceptance
**When** current, stale, success, pending, prior, conflict, denied, structural denial, adapter failure, redacted, degraded, retryable, terminal, keyboard, screen-reader, zoom, responsive, light/dark/forced-colors, reduced-motion, English, and French cases run
**Then** WCAG 2.2 AA, focus, safe messaging, redaction, origin attribution, and equal-locale behavior pass
**And** CLI/MCP outputs remain outside WCAG scope while preserving equivalent safe semantics.

**Given** final Epic 5 release validation
**When** the parity manifest and production adapters are inspected
**Then** every singular M1 parity row is exposed exactly as authorized and the Fluent/FrontComposer live route tests are non-vacuous
**And** the final offender, missing-row, skipped-arm, and unexplained-divergence lists are empty.

**Requirements:** FR82, FR83, FR84, FR85, FR86, ARCH-15, ARCH-32, ARCH-37, ARCH-39, NFR2, NFR10, NFR26, NFR32, NFR34, NFR39, NFR40, NFR50, NFR60, NFR62, NFR65, NFR67, NFR70, UX-DR11, UX-DR12, UX-DR15, UX-DR20, UX-DR37, UX-DR44, UX-DR53, UX-DR61, UX-DR65, UX-DR67, UX-DR70.

## Epic 6: Outbound Communication & Inbound Authenticity

Authorized users can understand inbound authenticity and safely draft and send exact approved Project communication under current sender, mailbox, recipient, file, and Project authority.

### Story 6.1: Preserve Provider Authenticity and Sender Evidence

As an authorized mailbox reviewer,
I want provider authenticity and sender evidence retained with each inbound message,
So that anomalies are reviewable without ChatBot inventing trust or identity.

**Acceptance Criteria:**

**Given** an inbound M365/Exchange message
**When** the mailbox adapter captures its immutable source record
**Then** it records provider-supplied DMARC, DKIM, and SPF verdicts and the relevant parsed values from `Authentication-Results`, `Received`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender`
**And** each field retains provider/source provenance, observation time, parser/schema version, redaction, retention, and correlation.

**Given** header identities or provider verdicts disagree
**When** authenticity evidence is normalized
**Then** every discrepancy is retained as intake metadata with a finite safe reason code and exact evidence references
**And** the adapter never upgrades, recomputes, or represents a provider verdict as ChatBot verification.

**Given** M365 expresses delegated send or on-behalf-of semantics
**When** sender evidence is recorded
**Then** the delegate is preserved as sender authority and the principal/mailbox owner is separately recorded as `principal_for`
**And** sender, principal, token subject, mailbox, and delegation evidence are never collapsed into one inferred identity.

**Given** the sender cannot be resolved to a current internal tenant Party
**When** the source record is created
**Then** it carries `external_sender=true` and a safe unresolved/external participant state
**And** the marker does not grant Project access, file access, task/command authority, or outbound authority.

**Given** required verdicts/headers are missing, malformed, duplicated, contradictory, or unsupported
**When** parsing completes
**Then** the record carries the typed unknown/anomalous evidence state and review reason without broadening trust
**And** raw header injection, control characters, active content, PII, or restricted values cannot enter logs, UI markup, audit detail for unauthorized actors, or safe error text.

**Given** a duplicate or replayed mailbox event
**When** authenticity evidence is processed
**Then** the same immutable source version and outcome are reused and no competing sender/authenticity record is created
**And** a materially changed provider source creates a versioned conflict/review record rather than mutating the original.

**Given** contract and adversarial parser tests
**When** pass/fail/none/unknown verdicts, aligned/misaligned identities, delegated sends, external senders, malformed headers, and cross-tenant cases execute
**Then** normalized evidence and reason codes are deterministic and tenant-isolated
**And** no test asserts that ChatBot cryptographically reverified the provider result.

**Requirements:** FR48a, FR48b, FR48c, FR48d, FR55, FR57, FR60, ARCH-18, ARCH-23, NFR2, NFR3, NFR4, NFR10, NFR11, NFR31, NFR32, NFR50, NFR52, UX-DR20, UX-DR41, UX-DR44, UX-DR67.

### Story 6.2: Enforce Strict or Paranoid Inbound Authenticity

As an authorized mailbox reviewer,
I want authenticity anomalies routed according to a closed fail-safe policy,
So that suspicious inbound messages cannot silently enter Project association.

**Acceptance Criteria:**

**Given** a mailbox source with `mailbox.authenticity-strictness=strict`
**When** current provider/header/sender evidence is complete and non-anomalous
**Then** `CaptureMailboxEvent` records `AuthenticityAccepted` and may start the separate association workflow in `Received`
**And** acceptance retains the exact evidence/policy versions and does not imply Project association or participant authority.

**Given** strict mode and unresolved or anomalous authenticity/sender evidence
**When** intake disposition is determined
**Then** the message enters `AuthenticityReviewRequired` with stable safe reason, mailbox-scoped evidence, owner, and next action
**And** it exposes no candidate Project and cannot enter association before an authorized resolution.

**Given** `mailbox.authenticity-strictness=paranoid` and any unresolved or anomalous evidence
**When** intake disposition is determined
**Then** the message enters terminal `AuthenticityBlocked`
**And** no reviewer, service client, AI actor, or automatic retry can reopen it in place or start association.

**Given** an authorized `mailbox-admin` with current mailbox scope and fresh evidence
**When** `ResolveInboundAuthenticityReview` is submitted for a strict-mode review item
**Then** it can transition once to `AuthenticityAccepted` or terminal `AuthenticityRejected` with expected revision, decision slot, rationale, evidence/policy, actor/time, operation, correlation, and atomic audit
**And** mailbox-admin scope grants neither message-content read beyond the review contract nor Project association-decision authority.

**Given** an `AuthenticityBlocked` or `AuthenticityRejected` record and materially changed provider evidence or policy
**When** `ReprocessInboundAuthenticity` is authorized
**Then** one new immutable intake successor is created with predecessor link, new operation/revision/evidence/policy, and current strict/paranoid evaluation
**And** the terminal predecessor remains unchanged and candidate Project data remains unavailable before new acceptance.

**Given** an unknown, unset, permissive, malformed, stale, or contradictory authenticity-policy value
**When** intake is evaluated
**Then** the policy is rejected or resolves to its safe closed default without accepting anomalous input
**And** MVP exposes no permissive mode or override path.

**Given** the authenticity workflow and association workflow
**When** lifecycle contract tests run
**Then** `AuthenticityAccepted|AuthenticityReviewRequired|AuthenticityBlocked|AuthenticityRejected` remain distinct from association `Received|NeedsReview|Failed|Skipped` and participant `Quarantined`
**And** invalid cross-family transitions reject before mutation and are safely audited.

**Given** the authorized authenticity review component
**When** verdicts, header discrepancies, external/delegated sender evidence, review/blocked/rejected states, and available actions render
**Then** provider provenance and ChatBot disposition are clearly separated, redaction is mailbox-scoped, and next action is accessible without raw headers
**And** keyboard, screen-reader, forced-colors, reduced-motion, English, and French tests pass.

**Requirements:** FR48a, FR48b, FR48c, FR48d, FR57, FR68, FR87, FR88, FR89, ARCH-17, ARCH-18, ARCH-23, NFR1, NFR2, NFR7, NFR13, NFR15, NFR31, NFR39, NFR60, NFR62, NFR70, UX-DR25, UX-DR41, UX-DR44, UX-DR65.

### Story 6.3: Resolve the Five Outbound Sender-Authority Classes

As an authorized Project contributor,
I want outbound sender authority resolved from current mailbox and Project evidence,
So that drafts and sends use only an identity the provider and ChatBot both permit.

**Acceptance Criteria:**

**Given** an outbound authority request
**When** it is resolved
**Then** the only possible classes are `draft-only`, `authenticated-user send`, `shared-mailbox send`, `send-on-behalf`, and `approved service-send`
**And** the result records token subject/client, OAuth mode/scope, target mailbox, mailbox ACL/delegation, current membership where applicable, asserted sender, send-as/on-behalf semantics, tenant, Project/outbound scope, evidence time/freshness, policy version, and revalidation state.

**Given** `draft-only`
**When** the requester has current Project and outbound-draft scope
**Then** a draft may identify requester, Project, and draft ID without provider send authority
**And** the class cannot leave ChatBot or be represented as send eligibility.

**Given** `authenticated-user send`
**When** a delegated token has `Mail.Send`, the target is the token subject's own mailbox, no delegation is asserted, and current Project/outbound scope exists
**Then** the class is eligible with the complete evidence tuple
**And** any subject/mailbox or scope mismatch denies it.

**Given** `shared-mailbox send`
**When** delegated permission, current membership, required explicit send-as grant, and Project/outbound scope all exist
**Then** the class is eligible with mailbox, membership/grant, requester, and recipient evidence
**And** membership or API permission alone is insufficient.

**Given** `send-on-behalf`
**When** delegated permission, current on-behalf delegation, enabled policy class, and Project/outbound scope all match
**Then** the delegate is sender authority and the mailbox principal is preserved as `principal_for`
**And** requester/delegate mismatch returns `delegation-mismatch` without substituting another identity.

**Given** `approved service-send`
**When** an application client has target-mailbox access restriction, current explicit service mailbox/outbound grant, originating requester, and a linked fresh human approval
**Then** the class may be eligible for exactly that draft/proposal/send
**And** missing approval returns `approval-missing` and no service client can send or approve on its own.

**Given** `outbound.authority-enabled` policy
**When** a class is evaluated
**Then** policy may enable only a class already eligible under the fixed mapping and cannot redefine evidence, authority, or sender identity
**And** provider permission with disabled ChatBot class returns `policy-blocked` while lapsed shared membership returns `membership-revoked`.

**Given** complete positive and negative fixtures for all five classes
**When** authority tests execute
**Then** missing, stale, malformed, revoked, mismatched, cross-tenant, external-sender, and conflicting evidence fails closed deterministically
**And** the rendered/audited result distinguishes sender, requester, principal, mailbox, class, freshness, and revalidation without leaking credentials.

**Requirements:** FR47, FR48, FR48c, FR49, FR50, FR57, FR68, ARCH-11, ARCH-18, ARCH-23, NFR1, NFR2, NFR4, NFR5, NFR6, NFR7, NFR16, NFR32, NFR48, NFR50, NFR70, UX-DR32, UX-DR41, UX-DR44.

### Story 6.4: Create an Immutable Outbound Project Email Draft

As an authorized Project contributor,
I want to create an outbound email draft within my Project and sender scope,
So that proposed communication can be reviewed without leaving the Project boundary.

**Acceptance Criteria:**

**Given** a requester with current Project and outbound-draft authority
**When** `CreateOutboundProjectEmailDraft` is submitted
**Then** it validates tenant/Project, intended mailbox/sender authority class, recipients, source context/files, redaction, retention, policy, stable operation ID, and expected revision through CommandGateway
**And** draft-only authority is sufficient to create a draft but never to send it.

**Given** valid draft input
**When** the draft commits
**Then** it records immutable draft/version identity, requester/origin, tenant/Project, intended sender/mailbox/authority class, normalized To/Cc/Bcc recipients, subject/body, source message/task/proposal and file references, redaction/freshness, policy, creation time, operation, and correlation
**And** it transitions to `Drafted` with atomic canonical audit and no provider call.

**Given** quoted email, attachment text, Project context, AI output, or user content is included
**When** draft content is assembled
**Then** all content is treated as untrusted data, authorized and redacted field by field, and cannot redefine system/tool policy, sender authority, recipients, approval, or commands
**And** hidden instructions, active content, secrets, and unauthorized source text are excluded.

**Given** content, subject, recipient, source file, sender/mailbox, authority class, or Project context changes after draft creation
**When** the user saves the change
**Then** a new immutable linked draft version with a new expected review hash is created
**And** no prior approval or send eligibility carries to the changed version.

**Given** a prior draft's send outcome is `SendOutcomeUnknown`, `Reconciling`, or `Unresolved`
**When** a new draft for the same logical communication is requested
**Then** creation is blocked until authorized reconciliation reaches audited `NotSent`
**And** the safe status identifies reconciliation as the next action without suggesting retry.

**Given** repeated equivalent create requests
**When** the stable operation ID and canonical draft hash are checked
**Then** one draft version exists and the stored outcome is returned
**And** a different payload under that ID returns `idempotency-conflict` without exposing prior content.

**Given** authorization, sender evidence, recipient validation, source/file authority, redaction, policy, revision, or audit readiness fails
**When** draft creation is attempted
**Then** no draft or send/proposal state is committed and the user receives the safe outcome and next action
**And** denied attempts expose no restricted Project, file, recipient, or mailbox detail.

**Requirements:** FR47, FR48, FR50, FR55, FR57, FR60, FR68, FR90, ARCH-12, ARCH-17, ARCH-18, ARCH-23, NFR1, NFR2, NFR4, NFR13, NFR15a, NFR16, NFR47, NFR50, NFR52, NFR70.

### Story 6.5: Freeze Outbound Content in a Mandatory Approval Proposal

As an authorized outbound requester,
I want the exact draft and sender context frozen for human approval,
So that no communication can leave the Project with changed content, recipients, or authority.

**Acceptance Criteria:**

**Given** a current immutable outbound draft version
**When** its linked approval proposal is created through the governed proposal contract
**Then** the proposal is structurally `approval-required` because outbound communication is a non-downgradable boundary effect
**And** tenant policy, action classification, sender class, requester role, AI involvement, or client origin cannot make it low risk.

**Given** the outbound proposal commits
**When** its frozen context is inspected
**Then** it includes requester/origin, tenant/Project, draft/version/hash, exact subject/body, To/Cc/Bcc recipients, sender/mailbox/authority class and complete evidence tuple, delegate/`principal_for` where applicable, source/files/redaction/freshness, policy, reversibility, approval expiry, expected provider effect/post-state/audit events, proposal/operation identity, and correlation
**And** changed or non-applicable values cannot be hidden by omission.

**Given** content, recipients, sender authority, mailbox, files, Project/source context, policy, or redaction changes after proposal creation
**When** the proposal is reviewed or used
**Then** it is invalidated and requires a new draft/proposal/decision chain
**And** the original proposal and any decision remain immutable and cannot authorize the changed send.

**Given** evidence references in the proposal
**When** freshness is evaluated
**Then** each reference exposes one timestamped `fresh|stale|expired` state and expired evidence disables approval with `evidence-expired`
**And** the safe next action creates a refreshed draft/proposal rather than editing the old one.

**Given** an outbound proposal is pending
**When** any user, CLI/MCP client, service client, AI actor, mailbox worker, or provider adapter attempts send
**Then** no communication leaves ChatBot and the attempt returns `approval-missing` or the more specific safe denial
**And** the pending proposal cannot resemble a sent or executing message.

**Given** more than one outbound proposal is selected
**When** grouped approval is considered
**Then** one-click batching is unavailable because external/irreversible effects require separate decisions and audit
**And** grouping for presentation never merges frozen security fields, decision slots, or outcomes.

**Given** duplicate or concurrent proposal creation
**When** stable draft/proposal identity and expected revision are checked
**Then** one approval subject and decision slot exist or no state commits
**And** atomic audit, policy, idempotency, and proposal references remain complete.

**Requirements:** FR45, FR49, FR50, FR55, FR61, FR68, FR90, ARCH-13, ARCH-17, ARCH-18, ARCH-23, NFR13, NFR15a, NFR16, NFR47, NFR48, NFR50, NFR70, UX-DR31, UX-DR32, UX-DR50, UX-DR52, UX-DR58.

### Story 6.6: Decide the Frozen Outbound Proposal

As an authorized human outbound reviewer,
I want to approve, reject, request revision, or cancel the exact frozen draft,
So that my decision cannot be reused for different communication or sender authority.

**Acceptance Criteria:**

**Given** a current outbound proposal in `AwaitingApproval`
**When** an eligible human submits the shared Approve, Reject, Request revision, or Cancel proposal decision
**Then** current human presence, `actorType=human`, tenant/Project, outbound review authority, exact draft/context hash, sender-authority evidence, recipients, files/redaction/freshness, policy, expiry, decision slot, and expected revision are validated
**And** no new outbound-specific bypass or competing approval command is introduced.

**Given** Approve is selected and every frozen field remains valid
**When** the decision commits
**Then** the proposal becomes `Approved` with reviewer, approved/expires times, authority, draft/hash, sender tuple, recipients, policy/evidence versions, operation, and canonical audit
**And** approval does not call the provider, mark the draft sent, or authorize any changed field.

**Given** Reject, Request revision, or Cancel is selected
**When** the decision commits
**Then** the proposal enters the corresponding immutable terminal state with rationale and no send eligibility
**And** revision requires a new draft/proposal/decision chain while cancellation cannot erase a previously committed provider effect.

**Given** the requester is an AI/tool/service principal or a machine client attempts to decide
**When** approval authority is evaluated
**Then** the human decision is structurally denied even when the principal originated, mediated, or will execute the send
**And** an approved service-send still requires the distinct prior human decision in its audit chain.

**Given** membership/delegation revoked, sender/recipient/file/Project drift, evidence expired, policy changed, proposal expired, audit unavailable, or state/revision stale
**When** a decision is attempted
**Then** no decision or send eligibility commits and the exact safe unavailable reason and refresh/revision action are returned
**And** the prior valid state remains authoritative.

**Given** concurrent or repeated decisions
**When** they target the authoritative decision slot
**Then** first commit wins, equivalent retry returns the stored decision, and non-equivalent attempts return current terminal result or typed conflict
**And** exactly one decision and canonical audit outcome exist.

**Requirements:** FR48, FR49, FR50, FR55, FR68, FR90, ARCH-13, ARCH-17, NFR1, NFR6, NFR13, NFR15, NFR15a, NFR16, NFR36, NFR48, NFR50, NFR70, UX-DR32, UX-DR33, UX-DR51, UX-DR58.

### Story 6.7: Review Outbound Communication on the Live S6 Surface

As an authorized outbound reviewer,
I want to inspect the exact communication and authority evidence before deciding,
So that I understand what will leave the Project and under whose identity.

**Acceptance Criteria:**

**Given** an authorized reviewer opens a pending outbound proposal
**When** S6 loads through the generated typed Client
**Then** it displays exact frozen subject/body, To/Cc/Bcc recipients, requester/origin, tenant/Project, sender/mailbox/authority class, delegate/`principal_for`, current membership/delegation/provider evidence, source/files/redaction/freshness, policy, approval lifetime, reversibility, expected provider effect/post-state/audit events, proposal/draft/operation identities, and correlation
**And** it performs no provider call or file/content exposure beyond the reviewer's current authority.

**Given** inbound authenticity or outbound authority evidence is displayed
**When** the reviewer inspects it
**Then** provider-supplied verdicts, header discrepancies, external-sender state, delegate/principal, authority class, and revalidation status are labelled without claiming ChatBot re-verification
**And** sender authority, requester, mailbox owner, and service client remain distinct.

**Given** the reviewer can Approve, Reject, Request revision, or Cancel
**When** controls render or submit
**Then** they use the completed Story 6.6 decision contract; unavailable approval remains focusable with the exact safe reason
**And** pending duplicate submission is prevented in presentation while server idempotency remains authoritative.

**Given** the proposal is approved
**When** its state renders
**Then** S6 shows `Approved` and awaiting-send eligibility with approved/expires times and frozen hash
**And** it does not show `Sending` or `Sent` until an authoritative send outcome exists.

**Given** insufficient authority, self-approval attempt, evidence expiry, membership/delegation drift, changed content/recipient/file/sender/policy, stale revision, conflict, rejection, revision request, cancellation, degradation, or terminal state
**When** the route renders
**Then** the exact safe state, owner, and new-draft/review/escalation action are shown and valid user context is preserved
**And** no restricted recipient, mailbox, Project, source, file, or provider detail leaks.

**Given** multiple outbound proposals
**When** the reviewer navigates them
**Then** each retains a separate frozen security context, decision slot, action, and audit outcome and one-click grouped approval is absent
**And** list/group presentation cannot conceal differences in content, authority, recipients, or files.

**Given** live S6 acceptance
**When** loading, full review, insufficient authority, all decisions, drift, approved-awaiting-send, denied, degraded, conflict, terminal, keyboard, screen-reader, zoom, responsive, light/dark/forced-colors, reduced-motion, English, and French cases run
**Then** WCAG 2.2 AA, focus, preview, source/authority, redaction, and equal-locale checks pass
**And** raw controls, stacked modals, tooltip-only explanations, hidden source content, or color/motion-only meaning are absent.

**Requirements:** FR45, FR48, FR48a, FR48b, FR48c, FR48d, FR49, FR50, FR57, ARCH-15, NFR1, NFR2, NFR24, NFR48, NFR60, NFR61, NFR62, NFR63, NFR70, UX-DR3, UX-DR11, UX-DR12, UX-DR13, UX-DR15, UX-DR22, UX-DR25, UX-DR31, UX-DR32, UX-DR33, UX-DR41, UX-DR44, UX-DR47, UX-DR48, UX-DR51, UX-DR52, UX-DR58, UX-DR65, UX-DR67, UX-DR69.

### Story 6.8: Send the Exact Approved Project Email Once

As an authorized outbound requester with a fresh human approval,
I want the exact approved draft sent under current sender authority,
So that communication leaves the Project once and only as reviewed.

**Acceptance Criteria:**

**Given** a current immutable `Approved` outbound proposal
**When** `SendApprovedProjectEmail` reaches execution admission
**Then** it revalidates requester/reviewer, human approval lifetime, tenant/Project, exact draft/hash, recipients, source files/redaction/freshness, mailbox/sender/authority tuple, current membership/delegation/application restriction, policy, proposal/draft revisions, idempotency, rate/control admission, provider readiness, and audit readiness
**And** any drift blocks send and requires a new draft/proposal/decision rather than changing the approved record.

**Given** all checks pass for one of the five sender-authority classes
**When** send begins
**Then** the aggregate atomically records `OutboundSendStarted`/`Sending`, frozen effect identity, provider request identity, operation, approval, policy, authority evidence, and canonical audit before provider dispatch
**And** the ChatBot-owned mailbox adapter sends exactly the frozen content/recipients/attachments under the resolved identity and no other.

**Given** `approved service-send`
**When** the service client dispatches
**Then** its exact target-mailbox restriction, current grant, originating requester, fresh human approval, tenant/Project, draft, and one-send operation are enforced
**And** the credential cannot approve, change, broaden, or send another draft and expires on use/revocation or the declared short lifetime.

**Given** the provider confirms successful send with authoritative message/effect identity
**When** the outcome commits
**Then** the send transitions once to terminal `Sent` and records provider reference, sender authority, recipients, approved content hash, requester/reviewer, time, operation, and canonical audit outcome
**And** later equivalent submissions return the stored `Sent` result without another provider call.

**Given** the provider definitively rejects before accepting the message
**When** the typed result is translated
**Then** the send enters `Failed` or audited `NotSent` according to the lifecycle contract with safe reason, owner, and permitted new-draft/recovery action
**And** no state claims an external send occurred.

**Given** timeout, connection loss, malformed response, or any result where provider acceptance cannot be proven or disproven
**When** dispatch returns
**Then** the send enters `SendOutcomeUnknown` with provider request identity and reconciliation-required status
**And** retry, new draft, or another send is forbidden until authorized reconciliation proves `NotSent`.

**Given** audit, authority, policy, approval, redaction, attachment, control, or provider readiness fails before dispatch
**When** send is attempted
**Then** no external call occurs and the typed safe denial/failure is separately audited
**And** raw message, recipient, mailbox, credential, or provider details do not leak.

**Given** supported-topology tests for each authority class
**When** success, duplicate, pre-dispatch rejection, revoked membership/delegation, approval expiry, changed content/recipient/file, provider ambiguity, and audit failure execute
**Then** provider ledger, domain events, state-store, operation status, and canonical audit prove at most one external send
**And** mock-only status or a provider call without ledger reconciliation cannot satisfy acceptance.

**Requirements:** FR47, FR48, FR48c, FR49, FR50, FR55, FR59, FR68, FR80, FR90, ARCH-10, ARCH-13, ARCH-14, ARCH-17, ARCH-23, NFR1, NFR4, NFR5, NFR7, NFR13, NFR14, NFR15a, NFR16, NFR19, NFR32, NFR34, NFR50, NFR70, UX-DR32, UX-DR51, UX-DR58.

### Story 6.9: Reconcile an Unknown Outbound Send Outcome

As an authorized mailbox operator,
I want an uncertain provider outcome reconciled from authoritative evidence,
So that the system neither resends a delivered message nor abandons a provably unsent one.

**Acceptance Criteria:**

**Given** a send in `SendOutcomeUnknown`
**When** the mailbox worker or currently authorized `mailbox-admin` submits `ReconcileOutboundSendOutcome`
**Then** it validates tenant/mailbox scope, provider request/message identity, draft/proposal/send operation, expected revision, current provider access, evidence time/freshness, policy, actor, and audit readiness
**And** it transitions to `Reconciling` without dispatching another message.

**Given** authoritative provider evidence proves the exact approved message was delivered
**When** reconciliation commits
**Then** the send transitions once to terminal `Sent` with provider identity, delivery evidence reference, sender/recipient/content hash, reconciler/time, operation, and canonical audit
**And** every retry or new-draft attempt returns/points to the stored sent outcome and cannot send again.

**Given** authoritative provider evidence proves the provider did not accept or deliver the message
**When** reconciliation commits
**Then** the send transitions once to audited `NotSent` with reason/evidence and safe new-draft action
**And** the original draft/send attempt remains immutable and cannot be retried; changed or repeated communication requires a new draft/proposal/approval.

**Given** provider evidence remains missing, contradictory, stale, inaccessible, or inconclusive
**When** reconciliation cannot determine outcome
**Then** state remains `Reconciling` until the four-hour deadline and then transitions to `Unresolved` with P2 escalation, owner, and safe investigation action
**And** send retry and new-draft creation for the logical communication remain forbidden.

**Given** duplicate, delayed, or concurrent reconciliation evidence
**When** it targets the same expected revision
**Then** equivalent evidence returns the stored outcome and stale/conflicting evidence cannot regress or overwrite `Sent`, `NotSent`, or `Unresolved`
**And** one authoritative reconciliation history and operation lineage remain.

**Given** the reconciler lacks mailbox scope or attempts to access Project/draft content beyond their authority
**When** evidence/status is assembled
**Then** the operation uses only content-free provider/send identities necessary for reconciliation and redacts protected Project/message fields
**And** mailbox-admin authority grants no Project workflow mutation other than this bounded reconciliation transition.

**Given** live status during unknown/reconciling/unresolved outcomes
**When** an authorized requester or reviewer views S6 or operation status
**Then** state, elapsed/deadline, evidence freshness, owner, prohibition on retry, P2 escalation, and next action are explicit
**And** the UI never reports `Failed` as safe-to-resend without audited `NotSent`.

**Requirements:** FR47, FR48, FR50, FR55, FR57, FR59, FR68, FR80, FR90, ARCH-17, ARCH-23, NFR1, NFR2, NFR13, NFR15, NFR17, NFR18, NFR31, NFR39, NFR41, NFR50, NFR51, NFR70, UX-DR37, UX-DR41, UX-DR44, UX-DR58, UX-DR65.

### Story 6.10: Reconstruct and Qualify the Complete Communication History

As an authorized requester, reviewer, or compliance investigator,
I want the complete inbound-authenticity and outbound-send history reconstructed,
So that I can prove what was received, proposed, approved, sent, refused, or reconciled.

**Acceptance Criteria:**

**Given** an authorized outbound communication record
**When** its history is queried
**Then** it links inbound authenticity where relevant, immutable draft versions, source/files/redaction, frozen proposal, human decision, sender-authority evidence, provider dispatch, operation attempts, unknown/reconciliation states, terminal outcome, actors/origins, policy, and canonical audit/correlation
**And** proposed content, approved content, and sent content/hash remain distinguishable and exact.

**Given** a reviewer may inspect the communication but lacks one or more Project, source, file, recipient, mailbox, provider, or audit-detail permissions
**When** history is assembled
**Then** each field/link is authorized and redacted before response construction and the remaining state stays coherent and existence-neutral
**And** copy, export, transcript, accessible descriptions, logs, and support artifacts apply the same redaction.

**Given** `Drafted`, `AwaitingApproval`, `Approved`, `Sending`, `Sent`, `Failed`, `SendOutcomeUnknown`, `Reconciling`, `NotSent`, `Unresolved`, rejected, revision-requested, cancelled, expired, or invalidated state
**When** S6 and operation history render
**Then** authoritative state, actor, time, freshness, terminality, retry/new-draft prohibition, owner, P2 escalation, and safe next action are explicit
**And** approval, HTTP acceptance, projection completion, or ambiguous provider response never appears as `Sent`.

**Given** qualification fixtures for `draft-only`, `authenticated-user send`, `shared-mailbox send`, `send-on-behalf`, and `approved service-send`
**When** success and every declared authority/evidence failure execute
**Then** sender/requester/principal/service-client identity, membership/delegation/application restriction, policy, approval, recipients, content hash, provider ledger, event/state, and audit outcomes match the fixed mapping
**And** M365 permission without matching ChatBot authority always fails closed.

**Given** duplicate, concurrent, approval-expired, membership-revoked, delegation-mismatched, policy-blocked, approval-missing, audit-unavailable, provider-rejected, ambiguous, reconciled-sent, reconciled-not-sent, and unresolved cases
**When** integration tests execute
**Then** at most one provider effect occurs, no immutable record is overwritten, and every safe result and next action matches the lifecycle
**And** tests inspect provider/external ledger plus persisted domain/audit end state.

**Given** live S6 acceptance after send and reconciliation support
**When** all lifecycle, authorization/redaction, validation, degraded, keyboard, screen-reader, zoom, responsive, light/dark/forced-colors, reduced-motion, English, and French states run
**Then** WCAG 2.2 AA, focus, authority/effect/freshness context, safe messages, and equal-locale behavior pass non-vacuously
**And** raw controls, hidden source text, stacked modals, grouped external approval, and color/motion/toast-only meaning are absent.

**Given** M1 outbound readiness is assessed
**When** evidence is assembled
**Then** it binds exact mailbox/provider adapter, identity/authority contracts, policy, drafts/proposals/approvals, runtime/topology, tests, external ledger, source revisions, runner, time, and independent verification
**And** open A5, A6, A13, or applicable M1 qualification evidence blocks the claim without being replaced by local or historical results.

**Requirements:** FR47, FR48, FR48a, FR48b, FR48c, FR48d, FR49, FR50, ARCH-23, ARCH-33, ARCH-37, ARCH-39, NFR1, NFR2, NFR4, NFR10, NFR13, NFR14, NFR15a, NFR16, NFR31, NFR32, NFR34, NFR49, NFR50, NFR51, NFR60, NFR61, NFR62, NFR63, NFR65, NFR67, NFR70, UX-DR11, UX-DR12, UX-DR15, UX-DR32, UX-DR37, UX-DR41, UX-DR44, UX-DR45, UX-DR51, UX-DR58, UX-DR65, UX-DR67, UX-DR69, UX-DR70.

## Epic 7: Tenant Policy & Bounded Administration

Tenant, policy, mailbox, and compliance administrators can configure only their authorized scopes through immutable, independently approved, auditable governance versions without becoming Project superusers.

### Story 7.1: Govern the Closed ChatBot Admin-Role Hierarchy

As a current tenant owner,
I want ChatBot admin roles granted, changed, or revoked through independent governed approval,
So that administrative scope is explicit and never becomes implicit Project superuser access.

**Acceptance Criteria:**

**Given** the closed admin-role catalog
**When** roles and scopes are inspected
**Then** `tenant-admin` is exactly the union of the proper `mailbox-admin`, `policy-admin`, `compliance-admin`, and `operations-admin` subsets defined by policy
**And** no custom role, wildcard scope, global-admin inheritance, or UI label can add authority.

**Given** an admin grant mutation
**When** it is submitted
**Then** only `GrantChatBotAdminRole`, `ChangeChatBotAdminRole`, or `RevokeChatBotAdminRole` may mutate it
**And** direct state/database seeding, claims-only grants, generic policy writes, or alternate bootstrap paths are rejected.

**Given** one of the three commands
**When** admission evaluates it
**Then** a current Tenants `TenantOwner` initiates, a distinct current authorized `TenantOwner` independently approves, the role/scope is closed and exact, and expected revision, justification, expiry where applicable, owner evidence, operation, and audit readiness are valid
**And** service clients, workers, AI/tool actors, or the same owner cannot initiate-and-approve.

**Given** a valid grant or change commits
**When** the new role becomes active
**Then** it creates a new immutable version linked to predecessor with exact principal, tenant, role, scopes, initiator, approver, owner evidence, policy, effective/expiry time, operation, and canonical audit
**And** it grants no Project, mailbox-content, per-item evidence, file, association-decision, or owner-context authority outside the explicit scope.

**Given** a valid revoke commits
**When** the grant becomes `Revoked`
**Then** new administrative access is denied within revocation bounds and in-flight sensitive mutations revalidate current authority
**And** the revoked grant and its history remain immutable and reconstructable.

**Given** stale/unavailable/malformed owner evidence, self-approval, closed-scope violation, stale revision, conflicting reuse, expired decision, audit unavailability, or unsupported role
**When** mutation is attempted
**Then** no grant version changes and the safe outcome identifies the permitted remediation
**And** the prior valid grant state remains authoritative.

**Given** M0 bootstrap versus M1 role management
**When** surface exposure is validated
**Then** M0 permits only required provisioning automation under these same commands/approvals and M1 may expose the bounded editor
**And** neither path can bypass open A13 owner mapping or claim tenant/Project authority from local metadata alone.

**Requirements:** FR75a, FR75g, FR90, ARCH-11, ARCH-13, ARCH-17, ARCH-21, ARCH-39, NFR1, NFR2, NFR6, NFR7, NFR13, NFR15a, NFR35, NFR50, NFR67, NFR70, UX-DR35, UX-DR36, UX-DR44.

### Story 7.2: Separate Aggregate Admin Visibility from Project Detail

As a bounded tenant administrator,
I want aggregate operational visibility without implicit Project content access,
So that I can understand tenant health while Project confidentiality remains enforced.

**Acceptance Criteria:**

**Given** a current `tenant-admin` or applicable scoped admin without Project authority
**When** tenant-wide operational summaries are queried
**Then** the response may contain aggregate queue depth/age/owner role, health/status enums, freshness, and aggregate metrics permitted by that scope
**And** it contains no Project name/ID mapping, item/message/participant identity, candidate/evidence content, filename, proposal content, detailed audit reason, or per-item link.

**Given** the same admin requests per-item detail
**When** authorization runs
**Then** current tenant membership plus exact Project/resource authority is required before projection, cache, cursor, audit, or owner-context access
**And** admin role, aggregate visibility, global-admin status, debug mode, or possession of an opaque item ID cannot satisfy it.

**Given** an admin also has current Project authority
**When** per-item detail is requested
**Then** the response is limited to the intersection of admin and Project/resource scopes and applies field-level redaction
**And** loss or revocation of Project authority removes detail access within the required staleness bound while aggregate scope may remain.

**Given** aggregate counts or groups are small enough to risk re-identification
**When** aggregation policy is applied
**Then** the result suppresses, combines, or reports unavailable according to the closed privacy threshold without leaking through totals or deltas
**And** policy/version/freshness are visible to the authorized admin.

**Given** pagination, filtering, sorting, export, copy, accessible descriptions, telemetry, or support output for aggregate admin data
**When** it is produced
**Then** opaque tenant-scoped cursors and the same redaction/aggregation rules apply in every channel
**And** a filter cannot be used as an existence oracle for a Project or item.

**Given** cross-tenant, no-Project-authority, revoked-authority, small-group, malformed-cursor, and debug-mode tests
**When** native/API and live surface checks run
**Then** aggregate views remain useful while per-item Project identity/evidence/file/audit detail leakage is zero
**And** every qualifying admin read is attributed and audited according to its scope.

**Requirements:** FR75b, FR75g, ARCH-11, ARCH-19, ARCH-41, NFR1, NFR2, NFR6, NFR9a, NFR10, NFR11, NFR27, NFR38, NFR45, NFR50, NFR67, UX-DR35, UX-DR38, UX-DR39, UX-DR44, UX-DR49, UX-DR67.

### Story 7.3: Operate Only Mailbox, Client, or Opaque Queue Partitions

As a bounded operations administrator,
I want to pause or resume broad work partitions without seeing Project detail,
So that I can contain degradation without becoming a workflow-item operator.

**Acceptance Criteria:**

**Given** a current admin with the applicable mailbox/client/operations scope but no Project authority
**When** `PauseMailboxSource`, `ResumeMailboxSource`, `PauseQueuePartition`, or the declared client-work-source partition operation is submitted
**Then** the target is identified only by tenant-scoped mailbox/client/opaque partition identity and current version
**And** the command contains no Project name, message, participant, evidence, file, proposal, or item content.

**Given** a valid pause command
**When** it commits through CommandGateway
**Then** new work admission/claim for that exact source or partition stops according to the closed pause contract while in-flight work follows its safe lease/lifecycle rule
**And** unrelated tenant/mailbox/client/partition work continues where isolation permits.

**Given** a valid resume command
**When** current authority, remediation evidence, expected revision, policy, dependencies, and audit readiness pass
**Then** only the exact paused source/partition resumes and its immutable pause/resume history remains
**And** resume cannot release a separate disabled/quarantined/rate-limited safety control owned by Epic 9.

**Given** the admin requests investigation without Project authority
**When** the request is recorded
**Then** it uses a content-free opaque identifier, scope, safe symptom/reason, time, requester, operation, and correlation
**And** any detailed evidence retrieval is separately authorized to a reviewer with current Project scope.

**Given** the same admin attempts to retry, requeue, quarantine, dismiss, decide, correct, approve, or otherwise mutate an individual Project workflow item
**When** authorization runs
**Then** the operation is denied unless current Project authority and the original requester's authority/revision/policy/audit requirements are independently satisfied
**And** admin role alone cannot elevate the per-item action.

**Given** an emergency per-item override is requested
**When** MVP contracts are evaluated
**Then** no such command or bypass exists and the result identifies the normal authorized escalation path
**And** a future override would require an explicit versioned contract, independent approval, reason, bounded scope, and expiry.

**Given** every pause, resume, investigation, denial, and qualifying status read
**When** audit is reconstructed
**Then** it records admin identity, exact scope, affected opaque sources/partitions, prior/new state, policy, time, reason, operation, and correlation without Project content
**And** audit unavailability returns redacted `AuditUnavailable` and commits no operation.

**Requirements:** FR75c, FR75g, FR80, ARCH-17, ARCH-27, ARCH-28, NFR1, NFR2, NFR7, NFR13, NFR15a, NFR20, NFR30, NFR38, NFR39, NFR50, NFR70, UX-DR35, UX-DR38, UX-DR44, UX-DR64.

### Story 7.4: Enforce the Closed Versioned Tenant Policy Schema

As an authorized policy administrator,
I want tenant policy changes constrained by a closed versioned schema,
So that configuration cannot create new authority or weaken mandatory safety invariants.

**Acceptance Criteria:**

**Given** the Tenant Policy Schema
**When** a row is inspected
**Then** it declares stable key, value type/range/enumeration, safe default or explicit no-default block, initiating role, required independent co-approver, security sensitivity, increment, dependencies, and cross-knob invariants
**And** unknown keys, wildcard structures, freeform executable values, or tenant-defined authority/command classes are impossible.

**Given** an `UpdateTenantPolicy` request
**When** schema and authority validation run
**Then** the initiating admin may change only rows naming their current role and every security-sensitive row requires its named distinct current authorized co-approver, justification, expected revision, and evidence
**And** service clients, workers, AI/tool actors, UI labels, or `tenant-admin` shorthand cannot bypass row-specific requirements.

**Given** a policy value is unset
**When** it is resolved
**Then** the exact schema safe default applies only where declared
**And** A6-dependent protection/retention or A5-dependent live-AI rows with no approved default block the applicable persistence/invocation.

**Given** proposed values are unknown, malformed, out of range, mutually inconsistent, authority-expanding, allowlist-expanding, approval-weakening, isolation-weakening, or retention-invalid
**When** the snapshot is validated
**Then** the complete change is rejected atomically and the prior valid policy remains active
**And** no partial row update or permissive fallback occurs.

**Given** a valid approved policy change
**When** it commits
**Then** a new immutable prospective snapshot records exact changed values, schema version, initiator, co-approver, justification, dependency evidence, effective time, predecessor, operation, and canonical audit
**And** historical decisions and commands retain their original snapshot references.

**Given** rollback is requested
**When** prior values remain valid under the current schema and receive required approval
**Then** a new immutable snapshot reintroduces those values prospectively
**And** neither prior snapshots nor historical outcomes are modified.

**Given** concurrent changes, stale revision, expired approval, owner-evidence failure, audit unavailability, or schema-version mismatch
**When** activation is attempted
**Then** no snapshot changes and the safe conflict/failure plus remediation is returned
**And** configuration caches invalidate within the declared bounds without temporarily broadening access.

**Requirements:** FR52, FR61, FR75a, FR75d, FR75g, ARCH-17, ARCH-18, ARCH-21, ARCH-31, ARCH-39, NFR1, NFR6, NFR7, NFR13, NFR15a, NFR23, NFR35, NFR50, NFR70, UX-DR35, UX-DR36, UX-DR44.

### Story 7.5: Configure Low-Risk Eligibility and Approval Routing Safely

As an authorized policy administrator,
I want to enable declared low-risk subtypes and route approvals within closed bounds,
So that tenant workflow can adapt without changing product risk or authority rules.

**Acceptance Criteria:**

**Given** a product-declared read-only/no-external-effect subtype in the immutable AI catalog
**When** an authorized policy admin proposes enabling or disabling it
**Then** `UpdateTenantPolicy` validates the exact subtype/catalog/classifier compatibility, tenant/Project applicability, schema row, current authority, required independent approval, expected revision, justification, and qualification evidence
**And** a tenant cannot create a subtype, edit its effect metadata, or make a non-eligible operation low risk.

**Given** state mutation, file exposure, outbound communication, task creation/assignment, external-tool invocation, or acting on behalf
**When** any low-risk or approval-routing policy is validated
**Then** the effect remains structurally mandatory-approval
**And** no routing target, timeout, fallback, role, aggregation, or tenant setting can downgrade, auto-approve, or skip the human decision.

**Given** an approval-routing change
**When** it is proposed
**Then** it selects only closed reviewer roles/scopes, escalation owners, age thresholds, and notification routes allowed by schema
**And** routing conveys work but never grants reviewer authority, Project access, self-approval, or execution scope.

**Given** the requester, originating AI/tool/service principal, mediator, executor, or an actor lacking current effect/Project authority appears in a route
**When** eligibility is resolved
**Then** that actor remains unable to approve regardless of routing
**And** the item exposes a safe no-eligible-reviewer/escalation state rather than weakening approval.

**Given** a valid approved configuration commits
**When** later classification or approval routing runs
**Then** it references the exact immutable policy/catalog/classifier/routing versions and records the selected route without changing the action classification
**And** already created proposals retain their frozen versions unless explicitly invalidated by policy drift.

**Given** unknown subtype, allowlist expansion, effect mismatch, missing qualification, stale revision, expired approval, audit failure, or invalid route
**When** configuration is attempted
**Then** no policy version activates and the prior valid configuration remains
**And** the safe outcome names the responsible role or remediation without exposing restricted users/Projects.

**Given** schema and integration tests
**When** every declared low-risk subtype, disabled state, six mandatory effects, reviewer route, self-approval, no-reviewer, drift, and invalid setting is exercised
**Then** only the eligible safe cases execute without approval and every boundary case remains reviewable or denied
**And** tenant configuration never changes the AI allowlist membership.

**Requirements:** FR40, FR41, FR52, FR61, FR75d, FR75g, ARCH-18, ARCH-30, ARCH-31, NFR1, NFR7, NFR8, NFR13, NFR15a, NFR16, NFR35, NFR46, NFR47, NFR50, NFR70, UX-DR30, UX-DR32, UX-DR35, UX-DR36, UX-DR50, UX-DR52.

### Story 7.6: Configure Mailbox Patterns and Provider Connections Within Scope

As an authorized mailbox administrator,
I want to configure monitored mailbox patterns, routing, and provider connections,
So that ingestion uses explicit least-privilege sources without granting me message or Project access.

**Acceptance Criteria:**

**Given** a current `mailbox-admin` for one tenant/mailbox scope
**When** `ConfigureMailboxSource` is submitted
**Then** it may define only schema-authorized mailbox/alias/folder patterns, routing rules, event/subscription settings, and secret-manager credential references within that scope
**And** it cannot include raw credentials, arbitrary executable routing, wildcard tenant access, Project content, association decisions, or outbound authority.

**Given** a provider credential connection
**When** it is validated
**Then** least-privilege OAuth/application mode, mailbox target/restriction, required permissions, tenant, expiry/rotation/revocation, and provider evidence are checked without returning the secret
**And** over-scoped, expired, revoked, wrong-tenant, permission-drifted, or inaccessible credentials fail closed.

**Given** a mailbox pattern or routing change
**When** schema validation runs
**Then** overlaps, ambiguous tenant ownership, unsafe catch-all patterns, unsupported folders/events, conflicting routes, and authority expansion are rejected atomically
**And** the prior valid version remains active.

**Given** a proposed authenticity-policy change
**When** a mailbox admin initiates it
**Then** it follows the exact Tenant Policy Schema row and requires the named independent current `policy-admin` approval and justification
**And** no mailbox-only command or provider setting can introduce a permissive mode or bypass `strict|paranoid` behavior.

**Given** a valid configuration activates
**When** mailbox workers resolve it
**Then** a new immutable version records tenant/mailbox scope, normalized patterns/routes, provider connection reference/status, policy, initiator/approver as applicable, effective time, predecessor, expected revision, operation, and canonical audit
**And** workers use only that version and exact service-client grant.

**Given** a mailbox admin queries configuration
**When** the response is assembled
**Then** it shows patterns, routes, provider permission/connection health, freshness, degraded state, and safe next action but no message content, participant evidence, Project candidates, or association decisions
**And** copy/export/logs/accessibility content preserve credential and data redaction.

**Given** authorization, provider validation, policy approval, expected revision, or audit readiness fails
**When** configuration is attempted
**Then** no version or credential binding changes and the safe failure is recorded
**And** unrelated mailbox scopes remain active where isolation permits.

**Requirements:** FR18, FR19, FR51, FR53, FR61, FR75e, FR75g, ARCH-10, ARCH-17, ARCH-22, ARCH-23, NFR1, NFR2, NFR4, NFR5, NFR6, NFR7, NFR13, NFR15a, NFR31, NFR35, NFR50, NFR70, UX-DR35, UX-DR41, UX-DR44, UX-DR60, UX-DR67.

### Story 7.7: Inspect Mailbox Permission and Processing Health

As an authorized mailbox administrator,
I want current mailbox permission and processing health without message access,
So that I can repair integration degradation within my scope.

**Acceptance Criteria:**

**Given** a current mailbox admin requests one authorized mailbox source
**When** its status is queried through the typed Client
**Then** the result shows configured pattern/version, connection and permission status, OAuth/application mode, required-versus-observed scopes, application-access restriction where applicable, credential expiry/rotation/revocation state, subscription expiry, permission drift, routing/worker state, freshness, health enum, affected scope, owner, and safe next action
**And** it contains no email content, sender/recipient PII, Project candidate/detail, attachment metadata, or association decision.

**Given** provider permission is revoked, expired, throttled, partial, delayed, subscription-expired, or drifted
**When** health is evaluated
**Then** the exact mailbox scope reports `degraded|failed|unknown` with stable safe reason and remediation while new processing fails closed as required
**And** unrelated mailboxes and tenants remain healthy where isolation permits.

**Given** status evidence is stale or the provider cannot be queried
**When** the view loads
**Then** it shows the last observed time, `stale|expired|unknown` state, affected dependency, responsible owner, and safe refresh/escalation action
**And** it never infers healthy from missing evidence.

**Given** the viewer lacks the exact mailbox/admin scope
**When** status is requested
**Then** the response is existence-neutral and reveals neither mailbox configuration nor health
**And** aggregate tenant health remains separately governed by aggregate visibility rules.

**Given** configuration/permission status is copied, exported, logged, announced, or placed in a support artifact
**When** redaction runs
**Then** secret values, tokens, mailbox content, protected identities, and provider raw errors are excluded while safe state/reason/correlation remains
**And** no debug mode bypasses the rule.

**Given** a qualifying mailbox-admin status read
**When** audit is available
**Then** admin identity, scope used, mailbox/source identifiers, fields/classes accessed, time, result, and correlation are recorded
**And** audit unavailability returns redacted `AuditUnavailable` and no protected status detail.

**Given** live component acceptance
**When** healthy, degraded, failed, unknown, stale, revoked, permission-drift, subscription-expiry, unauthorized, loading, keyboard, screen-reader, forced-colors, reduced-motion, English, and French states run
**Then** state/freshness/owner/next action remain understandable without color, tooltip, or raw diagnostics
**And** focus and redaction behavior pass non-vacuously.

**Requirements:** FR53, FR67, FR75e, FR75g, ARCH-23, ARCH-28, NFR1, NFR2, NFR4, NFR10, NFR31, NFR37, NFR38, NFR39, NFR40, NFR41, NFR42, NFR43, NFR50, NFR60, NFR62, NFR70, UX-DR35, UX-DR39, UX-DR41, UX-DR44, UX-DR62, UX-DR65, UX-DR67.

### Story 7.8: Bound Compliance Administration and Retention Changes

As an authorized compliance administrator,
I want redacted tenant audit and governed investigation/retention initiation,
So that I can fulfill compliance duties without operating Project workflows or directly changing owner data.

**Acceptance Criteria:**

**Given** a current `compliance-admin` without Project authority
**When** tenant audit or aggregate compliance status is queried
**Then** the response is tenant-scoped and redacts Project names/IDs, participants, message/file content, evidence detail, proposal content, and precise workflow reasons according to policy
**And** current Project authority is required separately for unredacted per-item evidence.

**Given** a compliance admin triggers an investigation
**When** the request commits
**Then** it records tenant scope, content-free subject/filter identifiers, lawful purpose, requester, policy, evidence window, operation, and correlation and routes it to the authorized investigation workflow
**And** it grants no ability to retry, requeue, quarantine, dismiss, approve, correct, or otherwise mutate an individual Project item.

**Given** a retention-window change within the closed A6/NFR49a policy bounds
**When** a compliance admin proposes it
**Then** the exact schema row requires a distinct current authorized admin/TenantOwner co-approval, justification, affected data classes/scope, legal-hold precedence, expected revision, and current A6 evidence
**And** missing/invalid bounds, self-approval, stale evidence, or authority expansion blocks the change.

**Given** a valid approved retention change
**When** it activates
**Then** a new immutable prospective policy version is created and owner contexts remain responsible for retention/export/delete/hold enforcement and acknowledgements
**And** ChatBot neither rewrites historical audit nor directly mutates Projects, Conversations, Folders, Parties, EventStore, backups, or owner stores.

**Given** a hold, surviving-metadata rule, backup propagation requirement, or tamper-evidence constraint conflicts with the proposed retention change
**When** policy validation runs
**Then** the operation is blocked with the exact safe reason and responsible owner/action
**And** compliance scope cannot override legal hold, append-only audit, isolation, or fail-closed invariants.

**Given** a compliance admin attempts a Project workflow action or unredacted detail query without Project authority
**When** authorization runs
**Then** it is denied before data access/mutation with no resource-existence leak
**And** the separate auditable-attempt path records the bounded denial metadata.

**Given** every compliance read, investigation, proposal, approval, denial, and policy activation
**When** audit is reconstructed
**Then** it includes admin identity, scope used, affected classes/items at the permitted granularity, time, decision, policy, approvals, redaction, outcome, and correlation
**And** audit unavailability returns no protected result and commits no change.

**Requirements:** FR58, FR75b, FR75d, FR75f, FR75g, ARCH-10, ARCH-11, ARCH-17, ARCH-20, ARCH-21, ARCH-39, NFR1, NFR2, NFR7, NFR12, NFR15a, NFR35, NFR49, NFR49a, NFR52, NFR53, NFR54, NFR55, NFR67, NFR70, UX-DR35, UX-DR36, UX-DR42, UX-DR44.

### Story 7.9: Audit Every Administrative Operation and Qualifying Read

As a security and compliance reviewer,
I want every administrative use attributable to its exact scope,
So that elevated access cannot operate through an unaudited path.

**Acceptance Criteria:**

**Given** the closed admin command/query catalog
**When** audit-obligation enumeration runs
**Then** every role grant/change/revoke, policy/mailbox/service configuration, partition operation, compliance action, rejected change, denial, restricted read, and operational-dashboard read above the aggregation threshold is registered
**And** an unregistered admin route, direct store access, debug endpoint, or skipped category fails the build.

**Given** an administrative mutation
**When** it commits
**Then** domain event, idempotency, policy/approval references, and canonical envelope atomically include admin identity/type, current role/scope used, tenant, affected resources/items at permitted granularity, command, decision/reason, expected revision, source evidence, redaction, prior/new state, time, operation, correlation, and outcome
**And** completeness is 100 percent with no skip-audit branch.

**Given** a qualifying admin read or rejected/non-mutating attempt
**When** it is authorized, denied, or fails
**Then** the separate auditable-attempt record includes identity, role/scope, query/action, requested/returned field classes, affected count/scope, aggregation/redaction decision, time, reason, correlation, and outcome
**And** it is not misrepresented as a domain mutation or replaced by telemetry.

**Given** audit readiness is unavailable
**When** an admin mutation, protected read, dashboard read, or rejected change is attempted
**Then** the operation returns redacted `AuditUnavailable`, returns no protected data, writes no domain/idempotency state, and raises the operations/security readiness signal
**And** `tenant-admin`, local development, support, or emergency labels provide no bypass.

**Given** audit or dashboard data is viewed by an admin
**When** its own access record is written
**Then** recursion is bounded by the declared audit-access event contract while preserving who accessed which scope and when
**And** failure to record that access withholds the protected result rather than looping or silently succeeding.

**Given** the admin-audit completeness suite
**When** it executes every catalog row and representative success/denial/conflict/failure/read outcome
**Then** 100 percent of mutations and sampled sensitive attempts contain every required field and pass tenant/redaction checks
**And** empty enumerations, telemetry-only evidence, or missing route coverage fail non-vacuously.

**Given** hash-link/checkpoint evidence remains A6/A13-gated
**When** admin audit is described or released
**Then** canonical completeness can be claimed only at its proven level and tamper-evidence/onboarding claims remain blocked until the owning gates close
**And** operational projection completeness never repairs a missing canonical record.

**Requirements:** FR55, FR57, FR75g, ARCH-13, ARCH-16, ARCH-17, ARCH-28, ARCH-37, ARCH-39, NFR2, NFR4, NFR7, NFR10, NFR15a, NFR35, NFR38, NFR49, NFR49a, NFR50, NFR50a, NFR51, NFR65, NFR70.

### Story 7.10: Administer Tenant Policy and Mailboxes on the Live S5 Surface

As an authorized bounded administrator,
I want one clear surface for only my role, policy, mailbox, and permission scopes,
So that I can configure the tenant without being presented as a Project superuser.

**Acceptance Criteria:**

**Given** a current `tenant-admin`, `policy-admin`, `mailbox-admin`, `compliance-admin`, or `operations-admin`
**When** S5 loads through the generated typed Client
**Then** bounded-scope badges and labelled data grids distinguish union/subset role, aggregate see-only, partition-operate, mailbox, policy, compliance, and per-Project powers
**And** only commands, rows, fields, and detail currently authorized for that actor are exposed.

**Given** role grant/change/revoke, policy update, mailbox configuration, or permission change is initiated
**When** its form and review flow render
**Then** exact changed values, scope, proposer, required distinct approver, justification, policy/schema version, expected revision, expiry/conflict, dependencies/evidence, and expected audit outcome are visible
**And** self-approval is focusable-disabled with its safe reason and no client-side role selection can broaden the closed schema.

**Given** a proposal is approved, rejected, cancelled, expired, conflicted, degraded, or terminally failed
**When** the route updates
**Then** it shows immutable proposal/decision lineage, active version, effective time, owner, and safe next action without optimistic activation
**And** rollback creates a new approved version rather than editing history.

**Given** a mailbox configuration or permission view
**When** it renders
**Then** patterns, routing, connection/permission health, freshness, owner, and remediation appear without credentials, mailbox content, participants, Project candidates, or association decisions
**And** authenticity-policy changes show the required independent policy-admin approval.

**Given** aggregate operational data and per-item data coexist
**When** the admin navigates
**Then** aggregate counts/health remain redacted at the declared threshold and per-item Project identity/evidence/file/audit detail requires current Project authority
**And** filters, URLs, counts, export/copy, markup, and accessible descriptions cannot elevate scope.

**Given** loading, empty, validation, unauthorized/redacted, stale authority, proposal, distinct approval, conflict, expiry, rejection, cancellation, active version, degraded provider permission, rollback, audit-unavailable, or terminal state
**When** S5 renders
**Then** state, owner, unavailable reason, and safe next action remain inline and valid form/filter context is preserved
**And** no raw diagnostic or protected detail appears.

**Given** live S5 acceptance
**When** role-specific, keyboard, screen-reader, focus-return, zoom, responsive, light/dark/forced-colors, reduced-motion, English, and French scenarios run
**Then** WCAG 2.2 AA, Fluent/FrontComposer, scope, separation-of-duty, redaction, and equal-locale checks pass
**And** raw controls, stacked modals, hover-only critical actions, infinite lists, tooltip-only reasons, and color/motion/toast-only meaning are absent.

**Requirements:** FR51, FR52, FR53, FR75a, FR75b, FR75c, FR75d, FR75e, FR75f, FR75g, ARCH-15, NFR1, NFR2, NFR24, NFR27, NFR35, NFR38, NFR39, NFR40, NFR60, NFR61, NFR62, NFR63, NFR70, UX-DR2, UX-DR3, UX-DR5, UX-DR7, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR15, UX-DR35, UX-DR36, UX-DR38, UX-DR41, UX-DR44, UX-DR46, UX-DR47, UX-DR48, UX-DR49, UX-DR60, UX-DR64, UX-DR65, UX-DR66, UX-DR67, UX-DR69.

### Story 7.11: Govern Service-Client Grants and Revocation

As an authorized tenant administrator,
I want service clients granted exact expiring permissions through policy and audit,
So that machine identities can perform only their declared integration work and be revoked safely.

**Acceptance Criteria:**

**Given** the closed service-client class catalog
**When** M0 and M1 classes are inspected
**Then** M0 contains only `mailbox-ingestion-client`, `audit-projection-client`, `background-retry-client`, and `ai-action-mediator-client`, while M1 adds only `cli-automation-client`, `mcp-human-delegated-client`/tool client as separately defined, and `ai-action-execution-client`
**And** every class has exact tenant/resource/actor scope, command/query set, delegation requirements, maximum expiry, and revocation semantics.

**Given** `GrantServiceClientPermission`
**When** admission evaluates it
**Then** an authorized admin, current owner authority, approved policy, exact client subject/class/scope/operations/resources, expiry, delegation evidence, expected revision, justification, operation, and audit readiness are required
**And** the first M0 grant additionally requires two distinct current Tenants owners and any gate-required Security approval.

**Given** a valid grant commits
**When** it becomes `Active`
**Then** a new immutable version records tenant, principal, class, exact permissions/resources, originating user/Project/proposal where applicable, issue/expiry, initiator/approver, policy, credential reference, operation, and canonical audit
**And** the service client inherits no human, tenant-admin, Project, approval, or wildcard authority.

**Given** `RevokeServiceClientPermission`, expiry, user-presence loss, or one-use completion applies
**When** the grant lifecycle transitions
**Then** the current grant becomes immutable `Revoked`, `Expired`, or consumed and new admission is denied within the required bound
**And** it can never reactivate; later access requires a new linked grant version and applicable approvals.

**Given** a service client attempts an operation outside exact tenant, resource, actor, command/query, delegation, approval, or lifetime scope
**When** authorization runs
**Then** it receives a structural existence-neutral denial with no protected result/effect
**And** no human role, broad OAuth/API permission, shared credential, or policy label broadens the grant.

**Given** S5 service-client administration
**When** active, expiring, expired, revoked, consumed, degraded, conflict, unauthorized, or audit-unavailable states render
**Then** the grid shows safe principal/class/scope/operations/resources, expiry, policy, approvers, state, last use, owner, and next action without secret values
**And** grant/revoke controls follow the typed command, distinct approval, focus, and redaction requirements.

**Given** grant/revoke/expiry and negative-scope tests for every class
**When** they execute
**Then** current authorization, exact permissions, immutable lifecycle, revocation latency, operation outcome, and canonical audit are asserted
**And** unknown classes, excess permissions, invalid expiry, self-approval, stale revision, audit failure, and direct seeding all fail closed.

**Requirements:** FR19, FR53, FR75a, FR75d, FR75g, ARCH-17, ARCH-21, ARCH-22, NFR1, NFR2, NFR4, NFR5, NFR6, NFR7, NFR13, NFR15a, NFR35, NFR50, NFR67, NFR70, UX-DR35, UX-DR36, UX-DR44, UX-DR60, UX-DR67.

### Story 7.12: Prove the Bounded Administration Safety Matrix

As a security and release reviewer,
I want every administrative role and boundary proven through end-to-end tests,
So that no configuration or operational path turns an administrator into a hidden Project superuser.

**Acceptance Criteria:**

**Given** `tenant-admin`, `mailbox-admin`, `policy-admin`, `compliance-admin`, `operations-admin`, TenantOwner without ChatBot grant, global admin, Project roles, service clients, workers, AI/tool actors, CLI, and MCP identities
**When** the authorization matrix is generated from the closed contracts
**Then** every role/scope has explicit permitted resources/actions and explicit forbidden operations
**And** an unclassified actor/action/resource row fails the test rather than inheriting a permissive default.

**Given** positive administrative cases
**When** role grants, policy changes, low-risk/routing configuration, mailbox configuration/health, service-client grants, aggregate reads, partition operations, and compliance initiation execute
**Then** each uses current owner authority, exact scope, required distinct approval, expected revision, immutable version, CommandGateway, and canonical audit
**And** persisted end state matches only the requested bounded change.

**Given** forbidden cases
**When** admins attempt per-Project detail/mutation without Project authority, mailbox-content read, association decision, approval self-dealing, custom policy/role/client class, AI allowlist expansion, six-effect downgrade, direct owner-store mutation, workflow-item operation, or debug bypass
**Then** each fails before protected read or mutation with an existence-neutral safe result
**And** no surface, machine credential, claim, or aggregate view changes the denial.

**Given** cross-tenant identifiers, caches, cursors, filters, queues, role grants, policy versions, mailbox/client resources, and audit queries
**When** native/API isolation cases execute
**Then** leakage and cross-tenant mutation are zero and partitioning derives from trusted context
**And** tenant-admin union scope remains confined to its own tenant and does not grant Project content.

**Given** revocation, owner-evidence staleness, user-presence loss, permission drift, approval expiry, concurrent change, audit outage, or degraded dependency
**When** every affected admin command/query is exercised
**Then** sensitive mutations and protected reads fail within the declared staleness bounds, prior valid state remains authoritative, and safe owner/next action is visible
**And** unrelated scopes continue where isolation permits.

**Given** live S5 and component conformance
**When** every admin role, loading/empty, proposal/approval, active/revoked/expired, conflict, validation, unauthorized/redacted, degraded, rollback, audit-unavailable, keyboard, screen-reader, zoom, responsive, theme, motion, and locale state runs
**Then** WCAG 2.2 AA, Fluent/FrontComposer, bounded-scope, focus, redaction, and equal-locale tests pass non-vacuously
**And** final raw-control, legacy-layout, hidden-data, missing-role-row, and unauthorized-action lists are empty.

**Given** admin release evidence
**When** M1 readiness is assessed
**Then** evidence binds exact RBAC/owner mapping, policy schema, service-client catalog, runtime, source revisions, topology, tests, runner, time, results, and independent approvals
**And** open A5/A6/A13, stale sibling contracts, or a material owner/RBAC drift blocks the claim and triggers the required architect source re-check.

**Requirements:** FR51, FR52, FR53, FR75a, FR75b, FR75c, FR75d, FR75e, FR75f, FR75g, ARCH-11, ARCH-12, ARCH-13, ARCH-19, ARCH-21, ARCH-22, ARCH-23, ARCH-32, ARCH-33, ARCH-37, ARCH-39, ARCH-40, NFR1, NFR2, NFR4, NFR6, NFR7, NFR9a, NFR11, NFR13, NFR15a, NFR16, NFR32, NFR35, NFR38, NFR50, NFR60, NFR61, NFR62, NFR65, NFR67, NFR70, UX-DR11, UX-DR12, UX-DR15, UX-DR35, UX-DR36, UX-DR44, UX-DR60, UX-DR65, UX-DR67, UX-DR70.

## Epic 8: Review Operations, Notifications & Escalation

Reviewers can find, prioritize, claim, and resolve work queues and receive bounded, actionable notifications and escalation guidance without leaking Project detail or creating approval fatigue.

### Story 8.1: Build Persistent Tenant-Safe Review Queues

As an authorized reviewer or operator,
I want persistent queues for work needing human attention,
So that review and recovery items remain visible even when telemetry or a worker is unavailable.

**Acceptance Criteria:**

**Given** ambiguous association, unresolved participant, pending approval, failed ingestion/attachment, or retryable operation events
**When** the queue projection consumes them
**Then** it creates or updates one tenant-scoped item in the correct declared queue family with stable item/subject identity, Project/mailbox reference as authorized, state, reason, age/entered time, owner role, assignee, risk/confidence, freshness, retry count, terminality, next action, source version, operation, and correlation
**And** queue truth is persisted from canonical events rather than inferred solely from logs, metrics, alerts, or in-memory workers.

**Given** duplicate, delayed, retried, corrected, superseded, or out-of-order source events
**When** queue handlers process them
**Then** item projection is idempotent, source-version ordered, and preserves immutable subject history
**And** an older event cannot reopen terminal work, lose assignment, or overwrite a newer next action.

**Given** a subject no longer requires review because it was resolved, cancelled, invalidated, exhausted, or reached a terminal state
**When** its committed event arrives
**Then** the active queue item closes with exact terminal outcome and time while remaining reconstructable
**And** it does not disappear without an attributable transition.

**Given** queue records, caches, cursors, topics, dead letters, and SignalR groups
**When** first-store and recurring native/API isolation tests execute
**Then** tenant partition derives only from trusted server context and cross-tenant reads/writes are impossible
**And** per-item Project identity/evidence remains unavailable without current Project authority.

**Given** an admin with aggregate queue scope but no Project authority
**When** queue summaries are requested
**Then** only redacted aggregate depth, oldest age, owner role, freshness, and safe family/state counts above privacy thresholds are returned
**And** item identity, Project, evidence, files, precise reasons, and links remain hidden.

**Given** queue projection is stale, rebuilding, unavailable, or behind its source
**When** status is queried
**Then** freshness/source version, affected scope, responsible owner, and safe next action are explicit
**And** the system never reports an empty or healthy queue from missing evidence.

**Given** metadata-only queue telemetry
**When** depth, oldest age, retry, failure, and projection lag are emitted
**Then** it contains tenant-safe scope and correlation without item payload/evidence
**And** operational dashboards consume but do not activate or replace the persistent queue behavior.

**Requirements:** FR67, FR69, FR71, FR79, FR80, ARCH-17, ARCH-18, ARCH-19, ARCH-27, ARCH-28, NFR2, NFR9a, NFR11, NFR17, NFR20, NFR28, NFR30, NFR37, NFR38, NFR39, NFR41, NFR50, NFR70, UX-DR35, UX-DR38, UX-DR44, UX-DR64.

### Story 8.2: Filter, Sort, and Prioritize Review Queues Server-Side

As an authorized reviewer,
I want stable server-side queue filtering and prioritization,
So that I can find the most important permitted work without loading or leaking the full queue.

**Acceptance Criteria:**

**Given** a queue query
**When** filters are validated
**Then** the canonical dimensions include age, risk, confidence, Project, mailbox, failure state, reviewer/assignee, and next action plus the declared queue family/state
**And** unknown dimensions, unbounded values, client expressions, or unauthorized scope filters are rejected safely.

**Given** an authorized per-item reviewer
**When** filtering, sorting, and prioritization execute
**Then** tenant/Project/item authorization and field redaction occur before candidate rows, counts, filter facets, or ordering are computed
**And** unauthorized items cannot be inferred from counts, gaps, cursor behavior, timing, or errors.

**Given** an approval queue
**When** default priority is calculated
**Then** it follows the declared risk × affected authority × age ordering with a stable item-identity tie-breaker and freshness/blocked safety rules
**And** priority affects presentation only and never grants authority or auto-decides work.

**Given** another queue family
**When** default ordering applies
**Then** its versioned declared priority uses safe age/state/risk/confidence/next-action inputs and a stable tie-breaker
**And** users can select only supported sorts within their authorized field set.

**Given** more results than one page
**When** the query returns
**Then** page size defaults to and never exceeds 100, and it supplies a tenant/query/filter/sort/version-scoped opaque cursor with stable ordering
**And** forged, expired, changed-query, or cross-tenant cursors fail without leaking keys or items.

**Given** active filters
**When** results render
**Then** the response includes an authorized active-filter summary/count and preserves selected item/focus across refresh where the item remains visible
**And** small-screen reflow retains labelled filters and a state-preserving detail path.

**Given** the approved baseline with healthy dependencies
**When** representative queue queries run
**Then** user-facing reads meet p95 two seconds or return a truthful retrievable pending/degraded state
**And** telemetry records safe query family, latency, row count band, freshness, and correlation without filter values that reveal protected data.

**Requirements:** FR69, FR78, FR79, ARCH-15, ARCH-19, ARCH-41, NFR1, NFR2, NFR10, NFR20, NFR24, NFR27, NFR30, NFR32, NFR38, NFR39, NFR70, UX-DR38, UX-DR44, UX-DR49, UX-DR52, UX-DR64, UX-DR67.

### Story 8.3: Resolve Safe Review Actions and Disabled Reasons

As an authorized reviewer,
I want each queue action shown with its true availability and safe reason,
So that I know what I can do next without trial-and-error or hidden authority changes.

**Acceptance Criteria:**

**Given** a queue item and current actor context
**When** action availability is queried
**Then** the server evaluates tenant/Project/resource authority, item family/state/revision, assignment, evidence freshness, policy, approval, retries, dependencies, controls, terminality, and audit readiness
**And** each declared action resolves to exactly `enabled`, `disabled-with-reason`, or `not-applicable-hidden`.

**Given** an action is enabled
**When** its contract is returned
**Then** it identifies the canonical command/query, consequence, required input, current revision/decision slot, and confirmation level
**And** availability is advisory and the command revalidates all conditions at submission.

**Given** an action applies but is currently unavailable
**When** it resolves to `disabled-with-reason`
**Then** it uses a finite versioned safe code and guidance naming an authorized responsible role or available action
**And** the disabled control remains focusable or has a reachable associated explanation without tooltip-only content.

**Given** an action does not apply to the item family/state or would reveal a forbidden capability/resource
**When** availability is assembled
**Then** it is `not-applicable-hidden`
**And** absence, layout gaps, counts, accessibility text, or API payloads do not disclose suppressed Project/action details.

**Given** authorization, evidence freshness, assignment, state, policy, control, dependency, or revision changes after rendering
**When** the action is submitted
**Then** CommandGateway returns the current safe enabled/disabled/conflict outcome and no stale action commits
**And** the UI preserves context, focuses the linked error/status, and refreshes next guidance.

**Given** ambiguous association, unresolved participant, approval, failed intake/attachment, and retryable-operation fixtures
**When** every state/role combination is evaluated
**Then** the action matrix is complete, deterministic, and contains no unknown or raw-error reason
**And** removing a state/action/role row fails non-vacuous contract tests.

**Requirements:** FR69, FR71, FR76, FR79, FR80, ARCH-16, ARCH-17, NFR1, NFR2, NFR7, NFR15, NFR17, NFR32, NFR39, NFR40, NFR62, NFR63, NFR70, UX-DR33, UX-DR37, UX-DR38, UX-DR44, UX-DR47, UX-DR65.

### Story 8.4: Claim and Assign Review Items Without Granting Authority

As an authorized reviewer or coordinator,
I want to claim or assign review work safely,
So that responsibility is clear without changing who is allowed to inspect or decide the item.

**Acceptance Criteria:**

**Given** an unclaimed review item and an actor with current claim authority plus Project/detail access where required
**When** `ClaimQueueItem` is submitted with stable operation ID and expected revision
**Then** the item records the actor as assignee/owner at server-UTC time with bounded lease or ownership semantics, source state, policy, operation, and correlation
**And** claiming grants no new Project, evidence, file, approval, decision, retry, or command authority.

**Given** an authorized coordinator assigns an item to an eligible reviewer
**When** `AssignQueueItem` is submitted
**Then** current assigner authority, target-reviewer eligibility/presence where applicable, tenant/Project scope, item state, expected revision, and policy are validated
**And** an ineligible, cross-tenant, unauthorized, inactive, or machine target is rejected without confirming protected identity/item detail.

**Given** two reviewers claim or coordinators assign the same current revision concurrently
**When** commands commit
**Then** first commit wins and the others receive a typed assignment conflict with safe refreshed owner/next action
**And** exactly one current assignment and canonical audit outcome exist.

**Given** an equivalent repeated claim or assignment
**When** stable operation identity is evaluated
**Then** the stored outcome is returned without a duplicate transition or notification
**And** a different target/payload under that ID returns `idempotency-conflict`.

**Given** assignment expires, the reviewer loses authority, Project/item state changes, the item resolves, or a safety control blocks work
**When** assignment is evaluated
**Then** it becomes stale/released/closed according to the declared lifecycle and cannot authorize a decision
**And** the queue shows current owner/state and safe reclaim/reassign/escalation action.

**Given** an assigned reviewer submits the underlying workflow decision
**When** CommandGateway authorizes it
**Then** current workflow-specific authority, evidence, policy, revision, and decision slot are revalidated independently of assignment
**And** assignment alone never permits a forbidden decision.

**Given** assignment history is queried
**When** authorized users inspect it
**Then** claim/assign/conflict/expiry/reassignment/closure events are attributable and linked to the item without overwriting prior responsibility
**And** admins lacking Project authority see only permitted opaque/aggregate assignment state.

**Requirements:** FR69, FR70, FR71, FR75b, FR75c, FR75g, FR90, ARCH-17, NFR1, NFR2, NFR6, NFR13, NFR15, NFR20, NFR39, NFR50, NFR70, UX-DR35, UX-DR38, UX-DR44, UX-DR64.

### Story 8.5: Show the Next Human Action for Each Review Item

As an authorized reviewer,
I want each queue item to explain its current state and next required action,
So that I can move work forward without interpreting raw audit or internal workflow data.

**Acceptance Criteria:**

**Given** an authorized reviewer lists or opens a queue item
**When** the queue row/detail is assembled
**Then** it shows family, safe subject identity, state, age, owner/assignee, risk/confidence as applicable, evidence freshness, retry count/ceiling, terminality, operation/correlation, and current next human action
**And** every field comes from the authoritative workflow/projection contract rather than UI inference.

**Given** an ambiguous email, unresolved participant, pending approval, failed ingestion/attachment, retryable operation, or delayed correction/send outcome
**When** next action is resolved
**Then** guidance names the canonical responsible role or available review/retry/refresh/escalation/investigation action for that exact state
**And** it never suggests an action prohibited by authority, evidence, lifecycle, control, or terminality.

**Given** the actor activates an enabled next action
**When** navigation occurs
**Then** state-preserving routing opens the owning association, participant, approval, attachment, retry, correction, outbound, or investigation surface with the safe item/operation context
**And** the queue does not replicate the workflow's command or authorization logic.

**Given** the actor lacks Project/item detail authority but has aggregate queue scope
**When** row/detail renders
**Then** only opaque family/state/age/owner-role/freshness and content-free investigation guidance appear
**And** Project, participant, message, file, proposal, evidence, precise reason, and item action remain hidden.

**Given** state, assignment, freshness, authority, or retry eligibility changes while the item is open
**When** the view refreshes
**Then** selection/focus/filter context is preserved, changed status is announced once, and current next action replaces stale guidance
**And** any attempted stale action is revalidated by CommandGateway.

**Given** loading, stale, unavailable, blocked, waiting, retryable, terminal, unauthorized/redacted, and no-action states
**When** shared queue row/detail components render
**Then** each has a stable safe code, owner where applicable, and one safe guidance path without color/tooltip/toast-only meaning
**And** keyboard, screen-reader, responsive, forced-colors, reduced-motion, English, and French tests pass.

**Requirements:** FR69, FR71, FR76, FR79, FR80, ARCH-15, ARCH-16, NFR1, NFR2, NFR17, NFR24, NFR38, NFR39, NFR40, NFR60, NFR62, NFR63, NFR70, UX-DR10, UX-DR12, UX-DR15, UX-DR25, UX-DR37, UX-DR38, UX-DR44, UX-DR45, UX-DR46, UX-DR64, UX-DR65.

### Story 8.6: Detect Stale, Waiting, Blocked, and Escalation-Needed Work

As an authorized reviewer or operator,
I want queue items classified by delay and blockage with accountable escalation guidance,
So that aging work is visible before it becomes silently abandoned.

**Acceptance Criteria:**

**Given** a versioned operating baseline and workflow-family state model
**When** the operations-owned evaluator examines queue age, evidence freshness, dependencies, assignment, retry state, deadlines, and terminality
**Then** it deterministically derives current `stale`, `waiting`, `blocked`, and `escalation-needed` indicators with observed time, threshold/baseline version, affected scope, owner, and safe next action
**And** missing or unqualified evidence yields `unmeasurable|unsupported` rather than healthy or ready.

**Given** an item waits on another actor, approval, owner service, retry time, correction acknowledgement, send reconciliation, or external dependency
**When** waiting status is reported
**Then** it identifies the safe responsible role/dependency and permitted next action without exposing a restricted person, Project, or evidence item
**And** waiting does not reset item age or hide its escalation threshold.

**Given** evidence becomes stale/expired or an operation exceeds its family-specific age/SLO threshold
**When** the evaluator runs
**Then** the queue item records the attributable status transition and freshness/age facts while preserving the underlying authoritative workflow state
**And** any dependent decision becomes unavailable according to its completed action contract.

**Given** escalation criteria are met
**When** the item enters `escalation-needed`
**Then** it identifies severity, accountable route/role, safe content-free notification payload, deadline, operation/correlation, and next action
**And** it does not auto-approve, retry, reassign, release a control, or mutate the underlying Project workflow.

**Given** the blocking condition clears
**When** fresh authoritative state is observed
**Then** derived queue indicators update idempotently from the newer source version and preserve prior escalation history
**And** an older evaluator result cannot reintroduce a cleared state.

**Given** evaluator, clock, baseline, dependency, or audit readiness fails
**When** status cannot be trusted
**Then** the affected scope reports degraded/unmeasurable with owner and safe recovery action
**And** unrelated tenant/queue partitions continue where isolation permits.

**Given** deterministic time/freshness tests
**When** boundary times, server UTC, source timezone presentation, business-day approval ages, stale evidence, delayed correction, send reconciliation deadline, dependency recovery, and out-of-order evaluations run
**Then** state and escalation outcomes match the versioned rules exactly
**And** raw timestamps are converted to tenant-local only at presentation.

**Requirements:** FR71, FR72, FR79, FR80, ARCH-17, ARCH-27, ARCH-28, NFR17, NFR17a, NFR23, NFR36, NFR37, NFR39, NFR41, NFR42, NFR43, NFR46, NFR48, NFR50, NFR70, UX-DR25, UX-DR37, UX-DR38, UX-DR44, UX-DR62, UX-DR64.

### Story 8.7: Configure Bounded Notification and Escalation Routing

As an authorized tenant or policy administrator,
I want review and escalation notifications routed through closed policy,
So that responsible users are informed without receiving unauthorized content or authority.

**Acceptance Criteria:**

**Given** `ConfigureNotificationRouting`
**When** a routing proposal is validated
**Then** it may select only schema-declared event families, authorized recipient roles/scopes, supported channels, severity/age/escalation rules, digest behavior, and rate ceilings within the tenant
**And** arbitrary addresses, executable templates, wildcard recipients, cross-tenant routes, or Project-detail expansion are rejected.

**Given** routing for review, approval, failure, degradation, quarantine, retry exhaustion, stale work, or escalation
**When** its recipient scope is evaluated
**Then** the route identifies an authorized role/queue and resolves actual recipients at dispatch using current authority
**And** configuration does not grant Project/item access, reviewer authority, assignment, approval, or command permission.

**Given** a security-sensitive routing or escalation change
**When** policy requires independent approval
**Then** the schema-named initiator and distinct authorized co-approver, exact values, justification, expected revision, evidence, and expiry are required
**And** service clients, workers, AI/tool actors, or self-approval cannot activate it.

**Given** a valid approved route commits
**When** it becomes active
**Then** it creates a new immutable prospective version with tenant, event/recipient/channel scope, thresholds, digest/ceiling rules, initiator/approver, policy/schema, effective time, predecessor, operation, and canonical audit
**And** existing notification records retain the route version originally applied.

**Given** a route is changed, disabled, or rolled back
**When** the update is approved
**Then** a new immutable version takes effect prospectively and pending work is re-evaluated under the declared rule
**And** historical notifications, acknowledgements, and audit are not rewritten.

**Given** unknown event/channel/role, unsafe template, invalid ceiling, missing authorization, stale revision, expired approval, audit failure, or conflicting update
**When** configuration is attempted
**Then** no route version changes and the prior valid route remains
**And** the safe result identifies the responsible remediation without revealing protected recipients or items.

**Given** route configuration is previewed in S5
**When** authorized admins inspect it
**Then** event family, authorized role/scope, channel, severity/threshold, ceiling/digest, approval, version, state, and next action are visible without item content
**And** keyboard, screen-reader, responsive, English/French, and redaction checks pass.

**Requirements:** FR72, FR73, FR75d, FR75g, ARCH-17, ARCH-18, ARCH-21, ARCH-28, NFR1, NFR2, NFR13, NFR15a, NFR23, NFR35, NFR46, NFR50, NFR70, UX-DR35, UX-DR36, UX-DR44, UX-DR52, UX-DR60, UX-DR65.

### Story 8.8: Dispatch Actionable Notifications Without Approval Fatigue

As an authorized reviewer,
I want bounded, deduplicated notifications for work requiring attention,
So that I can respond promptly without notification overload or leaked Project context.

**Acceptance Criteria:**

**Given** a queue transition covered by the active route
**When** `DispatchWorkflowNotification` is admitted
**Then** it resolves current authorized recipients at dispatch, applies tenant/Project/item redaction, and constructs bounded content containing safe family/state, age/severity, owner role, permitted next action/link, route version, operation, and correlation
**And** it excludes message/file/proposal content, candidate/evidence details, secrets, raw reasons, and unauthorized Project identity.

**Given** repeated equivalent source transitions or delivery callbacks
**When** notification identity is evaluated
**Then** one logical notification per recipient/channel/subject/transition/route window is created and stored outcome is reused
**And** a retry cannot duplicate the user-visible notice or underlying workflow action.

**Given** push-notification volume for one reviewer
**When** the rolling limits are evaluated
**Then** no more than eight push notices per hour and 30 per day are delivered and excess work moves to a digest without disappearing from queues
**And** urgent escalation follows only the separately declared severity route and remains audited.

**Given** a reviewer has more than 25 open applicable items
**When** queue pressure is evaluated
**Then** the reviewer and accountable owner receive the safe overload alert/digest according to route policy
**And** the system does not auto-approve, hide, or reassign work solely because of volume.

**Given** approval grouping is considered
**When** items are compared
**Then** grouping is permitted only for identical frozen security fields with separate decisions/audit and excludes irreversible, external, file, tool, and on-behalf one-click batching
**And** grouping presentation never merges proposal identity, evidence freshness, authority, or outcome.

**Given** sampled reviewer behavior
**When** rubber-stamp monitoring exceeds the declared 15-percent threshold
**Then** a tenant-safe alert and review signal are emitted with classifier/route versions and sample provenance
**And** monitoring never blocks a valid decision or exposes reviewer/item detail to unauthorized actors.

**Given** channel failure, expired recipient authority, revoked route, throttling, transient provider failure, or exhausted delivery retry
**When** dispatch cannot complete
**Then** notification enters its declared failed/retryable/digest/terminal state and `RetryWorkflowNotification` creates one linked attempt when allowed
**And** the underlying review item remains visible and no workflow decision is lost or repeated.

**Given** notification delivery and audit are inspected
**When** success, suppression, digest, retry, failure, and escalation occur
**Then** route/recipient scope, redaction, channel, transition, attempts, outcome, operation, and canonical audit are attributable
**And** raw delivery/provider errors or recipient secrets do not leak.

**Requirements:** FR72, FR73, FR76, FR79, FR80, FR90, ARCH-17, ARCH-28, NFR1, NFR2, NFR4, NFR10, NFR13, NFR14, NFR17, NFR18, NFR20, NFR39, NFR40, NFR46, NFR50, NFR70, UX-DR38, UX-DR44, UX-DR45, UX-DR52, UX-DR64, UX-DR65, UX-DR67.

### Story 8.9: Operate Review Queues on the Live S10 Surface

As an authorized reviewer or bounded operations administrator,
I want a responsive queue workspace showing only my permitted work and actions,
So that I can coordinate resolution without leaking or elevating Project scope.

**Acceptance Criteria:**

**Given** an authorized reviewer opens S10
**When** the route loads through the generated typed Client
**Then** the compact Fluent filter toolbar exposes server-supported filters, active summary/count, sort, pagination, and refresh, and dense labelled rows show permitted state, age, owner/assignee, risk/confidence, freshness, next action, retry count, and terminality
**And** page size remains no greater than 100 with opaque cursors and stable focus/selection on refresh.

**Given** a reviewer has current Project/item authority
**When** actions render
**Then** Claim, Assign, and navigation to the owning workflow decision/retry surface use the completed availability and command contracts
**And** S10 does not replicate association, participant, approval, attachment, retry, correction, outbound, or investigation logic.

**Given** an admin has aggregate/partition scope but no Project authority
**When** S10 renders
**Then** rows remain aggregate or opaque and expose only permitted pause/resume/content-free investigation navigation from the bounded admin contract
**And** retry, requeue, quarantine, dismiss, decide, evidence/file detail, and Project identity remain unavailable.

**Given** claim/assignment conflict, stale revision, authority loss, evidence expiry, safety control, blocked dependency, waiting actor, retry exhaustion, stale queue, or escalation threshold
**When** state refreshes or an action is submitted
**Then** inline status shows the canonical safe reason, owner, and next action, preserves filters/selection, and focuses linked errors where needed
**And** no optimistic assignment or decision is shown before the authoritative outcome.

**Given** notification delivery is sent, suppressed, digested, failed, retried, or escalated
**When** the item/status is viewed
**Then** the queue remains authoritative and displays permitted notification/escalation state without hiding work
**And** a toast or delivery failure never substitutes for persistent state.

**Given** desktop, tablet, or phone layout
**When** queues and detail reflow
**Then** reading, filters, primary safe actions, claim/assignment, and state-preserving detail/handoff remain usable and touch targets meet the declared floor
**And** no infinite list, forced scroll, hover-only critical action, stacked modal, or compact-only destructive/approval action appears.

**Given** live S10 acceptance
**When** every queue family, loading/empty, filtered/no-result, unauthorized/redacted, aggregate/opaque, claim/assign/conflict, stale/waiting/blocked/escalation, degraded, retryable, terminal, notification, keyboard, screen-reader, zoom, responsive, theme, motion, and English/French state runs
**Then** WCAG 2.2 AA, Fluent/FrontComposer, focus, redaction, safe messages, and equal-locale checks pass non-vacuously
**And** raw controls, hidden Project data, tooltip/color/motion-only meaning, and client-side filtering are absent.

**Requirements:** FR69, FR70, FR71, FR72, FR76, FR78, FR79, ARCH-15, NFR1, NFR2, NFR24, NFR27, NFR39, NFR40, NFR46, NFR60, NFR61, NFR62, NFR63, NFR70, UX-DR2, UX-DR3, UX-DR5, UX-DR7, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR35, UX-DR38, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR49, UX-DR52, UX-DR64, UX-DR65, UX-DR66, UX-DR67, UX-DR69.

### Story 8.10: Qualify Review Operations and Notification Safety End to End

As a release and operations reviewer,
I want queue and notification behavior proven under realistic load and failure,
So that human work remains visible, bounded, and recoverable without leaking or bypassing governance.

**Acceptance Criteria:**

**Given** ambiguous-association, unresolved-participant, pending-approval, failed-ingestion/attachment, retryable-operation, correction, and outbound fixtures
**When** end-to-end queue scenarios run
**Then** source event, persistent queue item, authorized row/detail, action availability, claim/assignment, owning-workflow outcome, closure, notification, and canonical audit remain linked and correct
**And** no item silently disappears, duplicates, reopens terminal state, or reports completion from telemetry alone.

**Given** every declared age, risk, confidence, Project, mailbox, failure, reviewer, next-action, family, and state filter/sort
**When** multi-page datasets execute
**Then** server authorization/redaction, stable ordering, pagination ≤100, opaque cursor scope, active-filter summary, and p95 two-second behavior are verified
**And** unauthorized items cannot be inferred from counts, timing, facets, gaps, or errors.

**Given** concurrent claims/assignments and stale/revoked authority
**When** operations race
**Then** first commit wins, losing actors receive safe current status, assignment grants no decision authority, and the underlying command revalidates current workflow scope
**And** event/state/audit end state contains one authoritative ownership transition per revision.

**Given** notification volume, duplicate transitions, >25 open items, grouping candidates, delivery failures, and sampled reviewer decisions
**When** fatigue controls execute
**Then** hourly/daily ceilings, digest overflow, overload alert, safe grouping exclusions, retry deduplication, and rubber-stamp monitoring behave exactly as declared
**And** no required approval is batched away, auto-decided, or hidden.

**Given** queue projection, worker, audit, notification provider, identity/authorization, mailbox, AI, or owner dependency failure
**When** resilience tests run
**Then** affected scope reports stale/degraded/blocked/unmeasurable with owner and next action, unrelated partitions continue fairly, and no protected data or unaudited mutation occurs
**And** recovery consumes authoritative source versions without duplicating work.

**Given** aggregate admin, per-Project reviewer, unauthorized actor, service client, and cross-tenant identities
**When** S10/API/native-store tests execute
**Then** each sees and performs exactly its bounded actions with zero Project-detail or tenant leakage
**And** admin aggregate/partition scope never becomes individual workflow-item mutation.

**Given** runtime safety controls are disabled, quarantined, or rate-limited in Epic 9 fixtures
**When** Epic 8 behavior is inspected
**Then** queues faithfully display/control availability from authoritative safety state but do not create, release, or enforce that state independently
**And** missing Epic 9 evidence is reported rather than simulated as active control.

**Given** release evidence
**When** Epic 8 acceptance is assessed
**Then** it binds exact queue/notification contracts, policies, sources, runtime/topology, load/failure fixtures, browser/API/store tests, runner, time, and result
**And** open release gates or incomplete live-route evidence block the applicable increment claim.

**Requirements:** FR69, FR70, FR71, FR72, FR73, FR76, FR78, FR79, ARCH-17, ARCH-19, ARCH-27, ARCH-28, ARCH-33, ARCH-37, ARCH-39, NFR1, NFR2, NFR9a, NFR11, NFR13, NFR14, NFR15a, NFR17, NFR18, NFR20, NFR24, NFR27, NFR30, NFR37, NFR39, NFR40, NFR41, NFR46, NFR50, NFR58, NFR59, NFR60, NFR65, NFR66, NFR67, NFR70, UX-DR11, UX-DR12, UX-DR15, UX-DR35, UX-DR38, UX-DR44, UX-DR49, UX-DR52, UX-DR64, UX-DR65, UX-DR67, UX-DR70.

## Epic 9: Runtime Governance Control Plane

Authorized administrators can actually disable, quarantine, and rate-limit mailbox sources, service clients, AI actors, and command capabilities, then safely release controls after independent approval and remediation.

### Story 9.1: Establish the Durable Closed Safety-Control View

As an authorized operations or security administrator,
I want one durable versioned view of runtime safety controls,
So that every gateway and worker evaluates the same authoritative subject state.

**Acceptance Criteria:**

**Given** the closed safety-control grammar
**When** its subject and mode catalog is inspected
**Then** subjects are exactly mailbox source, service client, AI actor, and command capability, and controlled modes are exactly `disabled`, `quarantined`, and `rate-limited` with `Active` as the released state
**And** outbound is represented through its command capability and cannot become a fifth subject class.

**Given** a safety-control version
**When** it is persisted and projected
**Then** it records trusted tenant, stable subject type/ID, prior/new mode, numeric bound/window where applicable, version, effective time, initiator/independent approver, evidence/policy, reason, expiry/review time, operation, and correlation
**And** each control or release is an immutable successor rather than an in-place update.

**Given** gateway and worker admission
**When** control state is requested
**Then** both consume the same durable versioned tenant/subject view and expose its current mode/version/freshness/bound without surface-local interpretation
**And** no configuration file, cache default, role claim, or queue flag can override it.

**Given** the control view or required version/freshness is missing, stale, malformed, conflicting, or unavailable
**When** the affected subject attempts new admission or work
**Then** the exact scope fails closed according to the safer-state rule with a versioned safe outcome
**And** the system never assumes `Active` from absence or uncertainty.

**Given** existing committed domain or external effects
**When** a control state changes
**Then** their immutable outcomes remain unchanged while queued/investigation evidence is preserved
**And** the control affects only new admission/claim/invocation as declared rather than rewriting history.

**Given** control records, caches, cursors, topics, and queue partitions
**When** native/API isolation tests execute
**Then** physical tenant/subject partitioning derives only from trusted context and cross-tenant control reads/writes are impossible
**And** control authority grants no Project data, item detail, or workflow-item operation access.

**Given** a duplicate, delayed, or out-of-order control event
**When** the admission view projects it
**Then** source version and predecessor ordering preserve the newest authoritative state and equivalent events are idempotent
**And** an older release cannot clear a newer safer control.

**Requirements:** FR74, FR75, FR55a, FR61, FR68, ARCH-17, ARCH-18, ARCH-19, ARCH-20, ARCH-27, NFR1, NFR2, NFR7, NFR9a, NFR13, NFR15, NFR17, NFR29, NFR30, NFR50, NFR70, UX-DR35, UX-DR37, UX-DR44.

### Story 9.2: Apply and Release Controls Through Independent Approval

As an authorized safety administrator,
I want controls applied and released with subject-specific independent approval,
So that emergency containment and recovery cannot be performed unilaterally or ambiguously.

**Acceptance Criteria:**

**Given** an `Active` mailbox source, service client, AI actor, or command capability
**When** `ApplySafetyControl` is proposed
**Then** the command uses the closed subject/mode discriminator, stable subject ID, exact disabled/quarantined/rate-limited bound, current subject revision, evidence, reason, policy, initiator, required independent approver, operation, and audit readiness
**And** unknown subjects/modes, wildcard scope, missing numeric bound, or Project-item target are rejected.

**Given** a mailbox-source control
**When** approval is evaluated
**Then** current `mailbox-admin` initiator plus independent `policy-admin` and current M365 authority/unsafe-activity evidence are required
**And** neither role alone nor Project authority can apply it.

**Given** a service-client or AI-actor control
**When** approval is evaluated
**Then** a current `policy-admin` initiator plus independent `tenant-admin` are required with respectively credential/scope evidence or provider/identity and A5/A13 evidence
**And** the controlled client/actor, its requester, or a service/AI principal cannot approve.

**Given** a command-capability control
**When** approval is evaluated
**Then** a current `policy-admin` initiator plus the required Security approver and current qualification/allowlist/owner-contract/incident evidence are required
**And** outbound is controlled only through its command capability.

**Given** all apply checks pass
**When** the command commits
**Then** a new immutable control version transitions the exact subject to `Disabled`, `Quarantined`, or `RateLimited`, records the bound if applicable, and atomically commits canonical audit/idempotency/policy/approval references
**And** duplicate submission returns the stored version while a conflicting payload/revision fails.

**Given** a controlled subject
**When** `ReleaseSafetyControl` is proposed
**Then** the same subject-specific roles independently approve current authority, exact subject/revision, remediation and release evidence, policy, bound removal, and safe dependencies
**And** mailbox release requires current M365/unsafe-activity evidence, service release credential rotation/revocation and scope validation, AI release provider/identity plus valid A5/A13, and command release qualification/allowlist/owner/incident evidence.

**Given** release succeeds
**When** it commits
**Then** a new immutable successor becomes `Active` and preserves the complete control/remediation/approval history
**And** it releases only the exact subject and does not restore a different/older control or bypass another admission policy.

**Given** missing/stale/malformed/conflicting approval or evidence, self-approval, stale revision, audit failure, or dependency uncertainty
**When** apply or release is attempted
**Then** no new version commits and the prior safer control remains authoritative
**And** the user receives the exact safe reason, responsible role, and remediation action without Project-data access.

**Requirements:** FR74, FR75, FR55, FR61, FR68, FR75a, FR75c, FR75d, FR75e, FR75g, FR90, ARCH-13, ARCH-17, ARCH-27, NFR1, NFR2, NFR7, NFR13, NFR15a, NFR29, NFR35, NFR50, NFR70, UX-DR35, UX-DR36, UX-DR44.

### Story 9.3: Enforce Current Controls at CommandGateway Admission

As a security administrator,
I want active safety controls enforced before any governed command proceeds,
So that disabled, quarantined, or over-limit subjects cannot reach protected data or effects.

**Acceptance Criteria:**

**Given** any UI/API, CLI, MCP, service-client, worker, mailbox-event, or AI-origin command/query
**When** CommandGateway evaluates runtime safety
**Then** it resolves the current tenant/subject control version for every applicable mailbox source, service client, AI actor, and command capability before handler/data/effect access
**And** the stage is shared and cannot be omitted, reordered, or replicated by a surface adapter.

**Given** an exact subject is `Disabled`
**When** new authentication, intake, invocation, command admission, or applicable query is attempted
**Then** it is denied with the versioned safe control code, current control version/freshness, owner, and permitted next action
**And** no domain/idempotency success, provider/owner call, queue claim, or protected response occurs.

**Given** an exact subject is `Quarantined`
**When** admission is attempted
**Then** it is denied and the content-free investigation/queue reference is preserved or opened idempotently according to policy
**And** quarantine grants no admin access to Project/item evidence or ability to operate the item.

**Given** an exact subject is `RateLimited`
**When** an operation enters the declared window
**Then** the trusted server-side counter/budget applies the recorded numeric bound atomically at the correct tenant/subject/operation scope
**And** excess receives safe retry-after/status guidance without a partial handler or external effect.

**Given** multiple applicable controls
**When** they are evaluated
**Then** the most restrictive valid result wins and each relevant version/bound is recorded in the admission outcome
**And** a permissive control cannot cancel a safer subject or policy restriction.

**Given** control view, cache, version, freshness, counter store, or required authority is missing, stale, malformed, conflicting, or unavailable
**When** admission runs
**Then** the affected scope fails closed and the auditable-attempt path records the safe failure
**And** local defaults, stale `Active`, debug mode, admin role, or surface fallback cannot continue.

**Given** the control changes while a command is awaiting approval or execution
**When** the effect is revalidated immediately before commit/dispatch
**Then** the current safer control blocks it and invalidates eligibility as required
**And** prior approval, earlier admission, or cached policy cannot override the current version.

**Given** admission-control tests for every origin and subject/mode
**When** positive, denied, over-limit, stale-view, unavailable-counter, concurrent-bound, and release cases run
**Then** command/event/state/external-ledger/audit assertions prove no bypass or excess effect
**And** telemetry reports tenant-safe mode/bound/rejection metrics without becoming enforcement authority.

**Requirements:** FR74, FR75, FR68, FR81a, ARCH-12, ARCH-13, ARCH-27, ARCH-32, NFR1, NFR2, NFR7, NFR13, NFR15a, NFR16, NFR19, NFR29, NFR30, NFR32, NFR50, NFR58, NFR59, NFR67, NFR70, UX-DR37, UX-DR44, UX-DR50, UX-DR51.

### Story 9.4: Enforce Controls in Workers with Fair Scheduling and Leases

As an operations administrator,
I want background work governed by the same controls and fair scheduling,
So that a disabled or noisy subject cannot bypass admission or starve unrelated work.

**Acceptance Criteria:**

**Given** a worker considers mailbox, retry, projection, notification, correction, AI, command, or outbound work
**When** it claims an item, renews a lease, or performs an effect
**Then** it resolves the same current durable tenant/subject control versions and applicable limits used by CommandGateway
**And** a queued item, prior admission, or worker identity cannot bypass a newly safer control.

**Given** a disabled or quarantined subject
**When** its work reaches scheduling
**Then** no new claim/effect occurs and the item remains safely blocked/quarantined with control version, owner, and next action
**And** investigation evidence is preserved without granting content access.

**Given** a rate-limited subject
**When** work is scheduled
**Then** the recorded tenant/subject messages, operations, requests/tokens, or executions bound is consumed atomically and excess waits with safe retry time/status
**And** parallel workers cannot exceed the bound through counter or lease races.

**Given** multiple tenants and work sources
**When** the scheduler selects work
**Then** it partitions fairly by tenant and then by work source while respecting priority, controls, quotas, breakers, and available capacity
**And** backlog in one tenant, mailbox, Project, client, AI actor, command, or surface does not starve unrelated scopes where isolation is possible.

**Given** a worker claims an item
**When** its bounded renewable lease expires, cannot renew, or worker restarts
**Then** another eligible worker may recover the item idempotently after expiry without repeating a committed effect
**And** stale lease owners cannot commit after losing the lease/control revision.

**Given** poison or repeatedly failing work
**When** retry ceiling is reached
**Then** it moves to the declared dead-letter/quarantine/review state with safe reason, owner, and recovery action
**And** the poisoned partition cannot consume unbounded leases or starve other partitions.

**Given** the control view, limiter, lease store, scheduler, or audit readiness is unavailable/stale
**When** affected work is considered
**Then** that scope fails closed and reports degraded/unmeasurable status while unrelated work continues where safe
**And** no in-memory permissive fallback executes the item.

**Given** concurrency, restart, noisy-neighbor, control-change, rate-bound, poison, dead-letter, and recovery tests
**When** they execute in the supported topology
**Then** persisted queue/state/lease/limit/event/audit and external-ledger end states prove fairness, no bypass, and no repeated effect
**And** later dashboards observe but are not required to activate this behavior.

**Requirements:** FR74, FR75, FR68, ARCH-24, ARCH-27, ARCH-28, ARCH-33, NFR7, NFR13, NFR14, NFR15a, NFR17, NFR18, NFR19, NFR20, NFR29, NFR30, NFR37, NFR41, NFR50, NFR58, NFR59, NFR66, NFR70.

### Story 9.5: Enforce All Mailbox-Source Control Modes and Release

As an authorized mailbox safety administrator,
I want a mailbox source disabled, quarantined, or rate-limited and later released safely,
So that unsafe intake can be contained without affecting unrelated sources.

**Acceptance Criteria:**

**Given** a mailbox-source control proposal
**When** it is approved
**Then** a current `mailbox-admin` initiator and independent `policy-admin` approve the exact tenant/mailbox source, mode/bound, reason, evidence, and revision
**And** current M365 authority and unsafe-activity evidence are required for apply/release decisions.

**Given** the mailbox source is `Disabled`
**When** new provider events arrive
**Then** intake admission is denied before source-content retrieval/persistence with the safe control status
**And** already committed sources and review evidence remain unchanged and authorized status stays visible.

**Given** the mailbox source is `Quarantined`
**When** new provider events arrive
**Then** intake is denied and one content-free mailbox investigation is opened/preserved with control/evidence references
**And** no message content, Project candidate, attachment, or participant workflow is created.

**Given** the mailbox source is `RateLimited` with a messages/time-window bound
**When** events arrive concurrently
**Then** the trusted limiter admits no more than the recorded bound and leaves excess work safely pending/retry-after without retrieving or processing it prematurely
**And** no parallel worker or subscription replay exceeds the bound.

**Given** current M365 authority and unsafe-activity/remediation evidence are complete and both required roles approve
**When** `ReleaseSafetyControl` commits
**Then** the exact mailbox source becomes `Active` through a new immutable version and queued intake resumes under current policy/permissions
**And** another source, older control, or provider permission is not released or broadened.

**Given** provider permission drift, stale/unknown control, missing approval/evidence, audit failure, or unavailable limiter
**When** apply, intake, or release is attempted
**Then** the prior safer state remains and only the affected mailbox scope fails closed
**And** unrelated tenants/mailboxes continue where isolation permits.

**Given** disabled, quarantined, rate-limited, concurrent-bound, provider-replay, stale-view, release-success, and release-failure integration cases
**When** they execute
**Then** provider retrieval, intake records, queues, investigation, limiter, control versions, and canonical audit prove each cell and release rule
**And** all four mailbox scenarios are counted independently in the 12-cell qualification matrix.

**Requirements:** FR74 (mailbox-source cells/release), FR75, FR53, FR68, ARCH-23, ARCH-27, NFR7, NFR13, NFR14, NFR19, NFR20, NFR29, NFR30, NFR31, NFR41, NFR50, NFR58, NFR59, NFR70, UX-DR35, UX-DR37, UX-DR41, UX-DR44.

### Story 9.6: Enforce All Service-Client Control Modes and Release

As an authorized service-client safety administrator,
I want a machine client disabled, quarantined, or rate-limited and later released after credential remediation,
So that a compromised or noisy client cannot continue using its grant.

**Acceptance Criteria:**

**Given** a service-client control proposal
**When** it is approved
**Then** a current `policy-admin` initiator and independent `tenant-admin` approve the exact tenant/client subject, mode/bound, reason, credential/scope evidence, and revision
**And** the controlled client, its originating user, an AI actor, or a service principal cannot approve.

**Given** the service client is `Disabled`
**When** it authenticates or submits any operation
**Then** authentication/admission is denied with the safe control state before protected data or handler access
**And** no human role, cached token, delegated grant, or broader provider scope overrides the denial.

**Given** the service client is `Quarantined`
**When** the control activates
**Then** active use/delegation is revoked within the required bound, new admission is denied, and one content-free credential investigation is opened/preserved
**And** existing committed outcomes and evidence remain immutable while no new effect executes.

**Given** the service client is `RateLimited` with an operations/time-window bound
**When** concurrent calls occur
**Then** the trusted limiter admits no more than the exact recorded bound at tenant/client scope and returns safe retry-after/status for excess
**And** parallel instances, refreshed tokens, or multiple surfaces cannot evade the counter.

**Given** credential rotation/revocation evidence, exact grant/scope validation, current owner authority, and both required approvals are complete
**When** `ReleaseSafetyControl` commits
**Then** the exact client becomes `Active` through a new immutable control version and only its current separately governed grant can operate
**And** release neither restores expired/revoked grants nor broadens commands, resources, delegation, or lifetime.

**Given** credential evidence, scope, control view, limiter, revocation, approval, or audit readiness is stale/missing/conflicting/unavailable
**When** apply, client use, or release is attempted
**Then** the prior safer state remains and the exact client scope fails closed
**And** unrelated clients and tenants remain usable where isolation permits.

**Given** disabled, quarantined, active-session revocation, rate-limited concurrency, token refresh/evasion, stale-view, release-success, and release-failure integration cases
**When** they execute for every declared service-client class
**Then** authentication, grant use, operation/effect, investigation, limiter, control versions, and canonical audit prove each cell/release
**And** all four service-client scenarios are counted independently in the 12-cell qualification matrix.

**Requirements:** FR74 (service-client cells/release), FR75, FR19, FR53, FR68, ARCH-22, ARCH-27, NFR1, NFR4, NFR5, NFR6, NFR7, NFR13, NFR19, NFR29, NFR30, NFR41, NFR50, NFR58, NFR59, NFR67, NFR70, UX-DR35, UX-DR37, UX-DR44.

### Story 9.7: Enforce All AI-Actor Control Modes and Release

As an authorized AI safety administrator,
I want an AI actor disabled, quarantined, or rate-limited and released only with current gate evidence,
So that model/tool use can be contained without stopping human-governed workflows.

**Acceptance Criteria:**

**Given** an AI-actor control proposal
**When** it is approved
**Then** a current `policy-admin` initiator and independent `tenant-admin` approve the exact tenant/AI actor, mode/bound, reason, provider/identity evidence, A5/A13 state, and revision
**And** the AI actor, originating requester, tool/MCP client, mediator, executor, or service principal cannot approve.

**Given** the AI actor is `Disabled`
**When** model or tool invocation is requested
**Then** invocation is denied before context packaging/provider/tool access with the safe control state
**And** human association review, existing-proposal decisions, retry, correction, and authorized audit remain available without live AI.

**Given** the AI actor is `Quarantined`
**When** requests or pending proposals reference it
**Then** new invocation/execution is denied while immutable proposals, approvals, context metadata, and outcomes are preserved for authorized investigation
**And** quarantining neither approves, cancels, edits, nor deletes the proposals.

**Given** the AI actor is `RateLimited` with requests/tokens/time-window bounds
**When** concurrent invocations occur
**Then** trusted counters enforce both applicable bounds at tenant/actor scope before provider call and return safe retry-after/status for excess
**And** retries, alternate surfaces, tool calls, or provider sessions cannot evade or double-consume limits incorrectly.

**Given** current provider/identity evidence, valid A5/A13 gates, exact actor/grant/allowlist/policy scope, remediation, and both required approvals are complete
**When** `ReleaseSafetyControl` commits
**Then** the exact actor becomes `Active` through a new immutable control version
**And** release does not authorize a provider, command, context, Project, file, tool, or effect outside its existing governed grants.

**Given** A5/A13 remains open or stale, identity/provider/allowlist evidence is missing, control/limiter is unavailable, or approval/audit fails
**When** apply, invocation, or release is attempted
**Then** the prior safer state remains and model/tool invocation stays blocked
**And** synthetic tests or existing proposals cannot substitute for gate evidence.

**Given** disabled, quarantined, preserved-proposal, rate-limited request/token, concurrency, non-AI availability, stale-view, release-success, and release-failure integration cases
**When** they execute with safe provider/tool adapters
**Then** invocation ledger, proposals, limiter, state, control versions, and canonical audit prove each cell/release without external effect
**And** all four AI-actor scenarios are counted independently in the 12-cell qualification matrix.

**Requirements:** FR74 (AI-actor cells/release), FR75, FR40, FR41, FR46, FR68, ARCH-27, ARCH-30, ARCH-31, ARCH-39, NFR7, NFR8, NFR9, NFR13, NFR16, NFR22, NFR29, NFR30, NFR41, NFR50, NFR58, NFR59, NFR70, UX-DR30, UX-DR35, UX-DR37, UX-DR44, UX-DR50.

### Story 9.8: Enforce All Command-Capability Control Modes and Release

As an authorized command safety administrator,
I want a command capability disabled, quarantined, or rate-limited and released only after Security-approved remediation,
So that unsafe execution can be stopped across every surface and worker.

**Acceptance Criteria:**

**Given** a command-capability control proposal
**When** it is approved
**Then** a current `policy-admin` initiator and required Security approver approve the exact tenant/product command/version, mode/bound, reason, qualification, allowlist, owner-contract, incident, and revision evidence
**And** outbound is targeted through `SendApprovedProjectEmail` or its exact command capability rather than a separate subject class.

**Given** the command capability is `Disabled`
**When** any UI/API, CLI, MCP, service client, AI actor, or worker submits it
**Then** CommandGateway denies admission before handler, owner/provider call, state mutation, or protected response
**And** prior approval, admin role, surface exposure, catalog/allowlist membership, or retry cannot override the control.

**Given** the command capability is `Quarantined`
**When** admission is attempted
**Then** it is denied and one content-free command investigation is opened/preserved with exact control/command/incident references
**And** pending proposals, approvals, and committed history remain immutable without execution.

**Given** the command capability is `RateLimited` with executions/time-window bound
**When** concurrent calls occur across origins and workers
**Then** one trusted tenant/command-version counter admits no more than the recorded bound before execution and excess receives safe retry-after/status
**And** retries, aliases, alternate surfaces, owner mappings, or multiple instances cannot evade the limit.

**Given** current qualification, immutable allowlist/catalog/exposure versions, accepted owner contract, closed incident/remediation evidence, and both required approvals are complete
**When** `ReleaseSafetyControl` commits
**Then** the exact command/version becomes `Active` through a new immutable control version
**And** release neither adds allowlist/catalog membership nor activates another version, target, surface, or tenant.

**Given** qualification/owner/allowlist/incident evidence is missing/stale/mismatched, control/limiter is unavailable, or approval/audit fails
**When** apply, command use, or release is attempted
**Then** the prior safer state remains and the capability fails closed everywhere
**And** local tests, cached approval, or historical evidence cannot support release.

**Given** disabled, quarantined, preserved-proposal, rate-limited cross-surface concurrency, outbound, stale-view, release-success, and release-failure integration cases
**When** they execute
**Then** normalized command, handler/owner/provider ledger, limiter, event/state, investigation, control versions, and canonical audit prove each cell/release
**And** all four command-capability scenarios are counted independently in the 12-cell qualification matrix.

**Requirements:** FR74 (command-capability cells/release), FR75, FR43, FR46, FR52, FR61, FR68, ARCH-27, ARCH-31, ARCH-32, ARCH-39, NFR7, NFR13, NFR15a, NFR16, NFR19, NFR29, NFR30, NFR35, NFR41, NFR50, NFR58, NFR59, NFR67, NFR70, UX-DR30, UX-DR35, UX-DR37, UX-DR44, UX-DR50, UX-DR51.

### Story 9.9: Govern Tenant Limits, Quotas, and Circuit Breakers

As an authorized policy or operations administrator,
I want bounded tenant workload limits and circuit breakers,
So that excessive or failing activity cannot exhaust shared capacity or cascade across scopes.

**Acceptance Criteria:**

**Given** `UpdateOperationalLimits`
**When** a proposal is validated
**Then** it may configure only closed schema rows for mailbox processing, AI requests/tokens, command execution, outbound communication, UI/API, CLI, and MCP rates/quotas/breaker thresholds at declared tenant/resource/source scope
**And** every numeric unit, window, capacity, threshold, reset/recovery rule, initiator, co-approver, expected revision, justification, and baseline evidence is explicit.

**Given** a security-sensitive or capacity-expanding limit change
**When** approval is evaluated
**Then** the schema-named current admin and required independent approver validate tenant capacity/baseline, fairness, safety controls, and downstream provider/owner constraints
**And** service clients, AI/tool actors, self-approval, unbounded values, wildcard resources, or local config overrides cannot activate it.

**Given** a valid approved change
**When** it commits
**Then** a new immutable prospective limit version records exact scopes/bounds/breakers, evidence, approvers, effective time, predecessor, operation, and canonical audit
**And** gateway/workers atomically consume the same versioned counters/budgets.

**Given** an applicable quota or rate bound is exhausted
**When** new work arrives
**Then** the exact scope returns safe rate-limited/pending status with retry-after, current usage band, owner, and next action while no excess effect begins
**And** unrelated tenant/resource/source budgets remain available.

**Given** a dependency failure/error rate crosses a configured circuit threshold
**When** the periodic evaluator runs
**Then** the breaker opens for the narrowest declared scope, admission/workers fail or defer safely, and recovery follows its versioned half-open/probe/close rule
**And** missing evidence never closes the breaker or reports the dependency healthy.

**Given** multiple policy limits and safety controls apply
**When** admission/scheduling occurs
**Then** the most restrictive current valid result wins and every contributing version/bound is recorded
**And** a higher quota or closed breaker cannot release a disabled/quarantined subject or weaken approval/authorization.

**Given** one tenant/mailbox/Project/client/AI actor/command/surface creates backlog or saturates a provider
**When** load and failure tests run
**Then** fair tenant-then-source scheduling, isolated counters, bounded leases, and breakers prevent degradation of unrelated scopes where technically possible
**And** telemetry measures saturation, rejection/defer rate, queue age, and freshness without becoming control authority.

**Given** limit/counter/breaker state is stale, malformed, unavailable, or conflicting
**When** work is evaluated
**Then** the affected scope fails closed with a safe degraded status and prior safer state remains
**And** no in-memory unlimited fallback or unmetered retry path executes.

**Requirements:** FR75, FR61, FR68, FR90, ARCH-17, ARCH-27, ARCH-28, NFR7, NFR13, NFR15a, NFR19, NFR20, NFR23, NFR28, NFR29, NFR30, NFR35, NFR41, NFR50, NFR58, NFR59, NFR66, NFR70, UX-DR35, UX-DR37, UX-DR39, UX-DR44.

### Story 9.10: Operate Runtime Safety Controls on the Live Admin Surface

As an authorized safety administrator,
I want to inspect, apply, and release only my permitted runtime controls,
So that containment and recovery remain clear, independently approved, and bounded.

**Acceptance Criteria:**

**Given** an authorized admin opens runtime controls in the shared S5/S10 admin experience
**When** the route loads through the generated typed Client
**Then** a labelled Fluent data grid presents exactly mailbox source, service client, AI actor, and command capability with current `Active|Disabled|Quarantined|RateLimited` version, bound/window, freshness, initiator/approver, effective time, reason, evidence/remediation state, affected aggregate scope, and safe next action
**And** outbound appears under command capability and no fifth subject class renders.

**Given** the actor selects Apply control
**When** mode and subject fields render
**Then** only role-authorized subjects and `disabled|quarantined|rate-limited` modes are available, rate limit requires exact numeric unit/window, and the form shows required independent approver/evidence and operational consequence
**And** wildcard, Project-item, custom mode, raw command target, or unauthorized subject input is impossible.

**Given** the actor selects Release
**When** release review renders
**Then** it shows the immutable active control, exact subject/version, subject-specific remediation/evidence checklist, required roles, current gates, prior incident/investigation, expected post-state, and audit outcome
**And** release remains focusable-disabled with the exact safe reason while any prerequisite is missing or stale.

**Given** apply or release is proposed, approved, conflicts, expires, is rejected, cancelled, fails audit, or commits
**When** state refreshes
**Then** proposer/distinct approver, exact changes, decision lineage, active safer state, operation/correlation, owner, and next action remain inline without optimistic activation
**And** valid filters/selection/form context and focus are preserved.

**Given** the admin lacks Project authority
**When** control scope and impact render
**Then** only tenant/mailbox/client/actor/command or opaque aggregate identifiers/counts permitted by admin scope appear
**And** Project names, item evidence, files, messages, proposals, precise reasons, and workflow-item actions remain redacted/unavailable.

**Given** controlled operations appear in queues
**When** the admin follows a link
**Then** S10 shows safe blocked/control state and content-free investigation or partition actions only
**And** the control surface cannot retry, requeue, decide, quarantine, dismiss, or mutate an individual Project workflow item.

**Given** live route acceptance
**When** every subject/mode, apply/release, role/co-approver, missing/stale evidence, conflict, unauthorized/redacted, stale-view, rate-bound, degraded, loading/empty, keyboard, screen-reader, zoom, responsive, theme, motion, and English/French state runs
**Then** WCAG 2.2 AA, Fluent/FrontComposer, focus, bounded scope, safe messaging, and equal-locale checks pass
**And** raw controls, hidden detail, stacked modals, tooltip-only reasons, color/motion-only meaning, and client-side enforcement are absent.

**Requirements:** FR74, FR75, FR75b, FR75c, FR75d, FR75e, FR75g, ARCH-15, ARCH-27, ARCH-28, NFR1, NFR2, NFR24, NFR27, NFR29, NFR30, NFR38, NFR39, NFR40, NFR60, NFR61, NFR62, NFR63, NFR70, UX-DR2, UX-DR3, UX-DR7, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR15, UX-DR35, UX-DR36, UX-DR37, UX-DR38, UX-DR44, UX-DR46, UX-DR47, UX-DR48, UX-DR49, UX-DR60, UX-DR64, UX-DR65, UX-DR66, UX-DR67, UX-DR69.

### Story 9.11: Qualify Every Safety-Control Cell and Release Flow

As a security and release reviewer,
I want all safety-control cells and recoveries proven through the supported topology,
So that no subject or mode remains an inert administrative record.

**Acceptance Criteria:**

**Given** the closed matrix of four subjects by three controlled modes
**When** qualification enumerates scenarios
**Then** exactly 12 independent apply/enforcement cases exist—disabled, quarantined, and rate-limited for mailbox source, service client, AI actor, and command capability—plus one release case for each subject
**And** missing, duplicate, skipped, empty, or fifth-subject rows fail the matrix.

**Given** each of the 12 controlled cases
**When** it executes through applicable UI/API, CLI, MCP, service-client, worker, mailbox, and AI origins
**Then** the exact subject-specific initiator/co-approver, transition/event, bound, gateway/worker enforcement, safe response, queue/investigation state, and canonical audit are asserted
**And** domain/external end-state proves no denied or excess effect occurred.

**Given** each of the four release cases
**When** current authority, subject revision, remediation, role-specific evidence, approval, policy, dependency, and audit readiness are valid or invalid
**Then** valid release activates only the exact subject through an immutable successor and invalid release preserves the prior safer control
**And** no release broadens grants, Project access, allowlist/catalog membership, owner mapping, or another limit/control.

**Given** concurrency, duplicate application, stale view/cache, limit-store outage, lease loss, worker restart, poison work, counter race, approval conflict/expiry, audit outage, or out-of-order event
**When** resilience cases run
**Then** first valid commit wins, the most restrictive state remains authoritative, limits are not exceeded, committed effects are not repeated, and recovery is idempotent
**And** unrelated tenant/subject partitions continue fairly where isolation permits.

**Given** cross-tenant and no-Project-authority identities
**When** control/status/investigation paths are exercised at API/native-store/live UI levels
**Then** control data and mutation remain tenant-isolated and Project detail leakage is zero
**And** safety-admin authority never permits individual Project workflow operations.

**Given** per-tenant rates, quotas, and breakers for mailbox, AI, commands, outbound, UI/API, CLI, and MCP
**When** baseline/noisy-neighbor/dependency-failure tests run
**Then** exact bounds, fair scheduling, narrow breaker scope, safe recovery, saturation/queue metrics, and non-interference are proven
**And** telemetry remains observational rather than enforcement authority.

**Given** live admin and queue acceptance
**When** all subject/mode/release, role, evidence, disabled/quarantined/rate-limited, stale/unknown, conflict, denied, degraded, loading, keyboard, screen-reader, responsive, theme, motion, and locale states run
**Then** WCAG 2.2 AA, Fluent/FrontComposer, safe actions, redaction, scope, and equal-locale checks pass non-vacuously
**And** final missing-cell, bypass, raw-control, hidden-data, and unexplained-outcome lists are empty.

**Given** control-plane release evidence
**When** M1 readiness is assessed
**Then** it binds exact matrix/schema/policy, control and limit versions, identities/approvals, runtime/topology, provider/owner dependencies, store/external ledgers, tests, runner, time, results, and independent verification
**And** open A5/A6/A13 or stale qualification evidence blocks the claim without being replaced by metadata or historical tests.

**Requirements:** FR74, FR75, ARCH-17, ARCH-19, ARCH-24, ARCH-27, ARCH-28, ARCH-32, ARCH-33, ARCH-37, ARCH-39, NFR1, NFR2, NFR7, NFR9a, NFR11, NFR13, NFR14, NFR15a, NFR16, NFR19, NFR20, NFR29, NFR30, NFR32, NFR34, NFR37, NFR39, NFR40, NFR41, NFR50, NFR58, NFR59, NFR60, NFR65, NFR66, NFR67, NFR70, UX-DR11, UX-DR12, UX-DR15, UX-DR35, UX-DR37, UX-DR38, UX-DR44, UX-DR64, UX-DR65, UX-DR67, UX-DR70.

## Epic 10: Command Allowlist & Lifecycle Governance

Policy and security administrators can govern the immutable command allowlist and verify that each workflow family uses its own valid lifecycle, conflict, successor, and retry semantics.

### Story 10.1: Publish the Canonical Workflow-Family Lifecycle Catalog

As a workflow and contract owner,
I want one versioned catalog of family-specific states and transitions,
So that surfaces and workers cannot substitute generic or conflicting lifecycle semantics.

**Acceptance Criteria:**

**Given** the canonical lifecycle catalog
**When** its required families are inspected
**Then** it contains explicit contracts for intake/authenticity/association, participant resolution, attachments, approvals, AI actions, commands, and audit projection
**And** outbound, notification, administration, correction, retention/recovery, and safety-control extensions reference their own already-declared family contracts rather than collapsing into one generic state machine.

**Given** each family contract
**When** it is validated
**Then** it declares stable version, state strings, initial/current/terminal states, permitted command/event transitions, actor/authority, required inputs, expected revision/decision slot, retry or successor semantics, user response, safe reason, audit facts, and increment availability
**And** health enums, queue indicators, and UI display labels remain distinct from authoritative workflow states.

**Given** states or transitions already implemented by earlier epics
**When** source and catalog synchronization checks run
**Then** runtime contracts, OpenAPI/generated Client, events, projections, UI/CLI/MCP values, audit, fixtures, and documentation use the exact canonical strings/versions
**And** this epic validates/completes the catalog without deferring repair of an earlier incomplete workflow.

**Given** a family needs a new state, transition, retry, or terminal outcome
**When** contract evolution is proposed
**Then** an explicit compatible version or migration/upcast plan, owner, authority, events, surface behavior, audit, fixtures, and deprecation handling are required
**And** a local enum or string addition cannot silently alter the catalog.

**Given** an unknown family/state/command/event, duplicate terminal meaning, cross-family transition, or missing retry/successor rule
**When** lifecycle validation runs
**Then** the build fails with the exact offending row
**And** no runtime path maps it approximately or defaults it to success/failure.

**Given** the catalog conformance test
**When** a required family, state, transition, terminal marker, actor, event, conflict, retry, response, redaction, or audit field is removed
**Then** it fails non-vacuously
**And** empty reflection sets, skipped projects, or zero-case matrices cannot pass.

**Requirements:** FR87, FR88, FR89, ARCH-7, ARCH-15, ARCH-17, ARCH-18, ARCH-37, ARCH-41, NFR13, NFR13a, NFR15, NFR18, NFR32, NFR33, NFR36, NFR39, NFR50, NFR70, UX-DR37, UX-DR44, UX-DR53.

### Story 10.2: Validate Every Transition Against Its Family Model

As a workflow owner,
I want inbound and outbound transitions checked against the exact family contract,
So that commands, events, workers, and surfaces cannot move an item into an illegal state.

**Acceptance Criteria:**

**Given** a typed command/event and current aggregate/workflow state
**When** transition validation runs
**Then** it resolves the exact lifecycle family/version and checks source state, target state, command/event pairing, actor/authority, required evidence/policy/approval/control, expected revision/decision slot, terminality, and entry/exit invariants before mutation
**And** no string comparison, UI state, queue indicator, or adapter mapping substitutes for the family contract.

**Given** a valid transition
**When** the aggregate handles it
**Then** pure `Handle`/`Apply` produces only the catalog-declared event/state and canonical transition/audit facts
**And** persistence occurs before publication/projection and no external I/O runs inside the aggregate.

**Given** equivalent commands through UI/API, CLI, MCP, worker, mailbox, service-client, or AI origin
**When** transition validation executes
**Then** the same semantic input and authoritative state yield the same accepted/rejected transition and safe reason
**And** origin affects attribution/authority where declared but cannot select a different lifecycle implementation.

**Given** a terminal state
**When** a retry, reprocess, revision, supersession, reconciliation, appeal, or compensation is valid
**Then** the catalog-declared successor command creates/links a new immutable attempt/workflow or returns the existing successor
**And** the terminal record is never reopened or edited in place.

**Given** concurrent commands at the same expected revision or decision slot
**When** validation and atomic commit race
**Then** first commit wins, losing commands re-evaluate against current state and return prior outcome or typed conflict
**And** no validation-to-commit gap permits two authoritative transitions.

**Given** a cross-context owner effect
**When** its committed event/revision is reconciled
**Then** the local coordinator advances only through the declared owner-event transition with matching identity/authority/source version
**And** a synchronous response or uncommitted intent cannot advance the state alone.

**Given** every permitted lifecycle row
**When** positive transition tests run
**Then** the declared current/next state, event, audit, user response, idempotency, and projection end state are asserted
**And** missing or unreachable permitted rows fail coverage rather than being ignored.

**Requirements:** FR87, FR88, ARCH-7, ARCH-12, ARCH-13, ARCH-14, ARCH-17, NFR1, NFR13, NFR15, NFR15a, NFR18, NFR19, NFR32, NFR34, NFR50, NFR70, UX-DR37, UX-DR53.

### Story 10.3: Reject and Audit Invalid Transitions Consistently

As a security and workflow reviewer,
I want invalid transition attempts rejected and attributable,
So that lifecycle violations cannot mutate state or disappear from investigation.

**Acceptance Criteria:**

**Given** a command/event that is not permitted from the authoritative current state or family version
**When** transition validation runs
**Then** it rejects deterministically before event/domain/idempotency success, publication, projection, notification, owner/provider call, or external effect
**And** the prior state/revision remains unchanged.

**Given** an invalid transition is rejected
**When** the auditable-attempt record is written
**Then** it captures permitted tenant, actor/type/origin, command/event, resource, family/version, current/requested state, reason, expected/current revision, policy/approval/control references, redaction, time, operation, and correlation
**And** it remains distinct from a committed mutation envelope.

**Given** the actor lacks authority for the resource or current state
**When** the invalid result is returned
**Then** the response uses the versioned existence-neutral safe code and permitted next action without confirming the resource, state, Project, evidence, or owner detail
**And** privileged transition diagnostics remain separately authorized.

**Given** attempt-audit storage is unavailable
**When** an invalid or unauthorized transition is attempted
**Then** the transition remains rejected, no protected state/data is returned, and the audit-readiness incident signal is raised
**And** telemetry cannot substitute for the missing record or permit continuation.

**Given** duplicate/repeated invalid attempts
**When** they are recorded
**Then** each required security attempt remains attributable according to the bounded attempt/dedup contract without creating a domain outcome
**And** repeated input never becomes valid merely through retry or elapsed time.

**Given** the family transition matrix
**When** invalid source/target, terminal reopen, wrong command/event, stale revision, wrong decision slot, missing prerequisite, cross-family, owner-response-only, and unknown-state cases run
**Then** every rejected combination returns its declared safe reason and produces no state/effect
**And** persisted end state and attempt-audit completeness are asserted rather than only an error status.

**Requirements:** FR89, FR55, FR57, FR68, ARCH-16, ARCH-17, NFR1, NFR2, NFR7, NFR13, NFR15, NFR15a, NFR32, NFR39, NFR40, NFR50, NFR51, NFR70, UX-DR37, UX-DR44, UX-DR65.

### Story 10.4: Separate Catalog, Surface, MCP, AI, and Owner Mappings

As a security and product owner,
I want command membership and executability represented by distinct deny-by-default artifacts,
So that exposure on one surface cannot silently authorize AI or invent an owner operation.

**Acceptance Criteria:**

**Given** the command-governance artifacts
**When** they are inspected
**Then** the complete public ChatBot operation catalog, UI/API/CLI/MCP exposure policy, MCP tags, AI-invocable allowlist, and owner executable target mappings each have their own immutable version, schema, owner, approval, provenance, and compatibility state
**And** their relationships are explicit mapping rows rather than shared names or inferred prefixes.

**Given** an operation exists in the public catalog
**When** surface or AI access is evaluated
**Then** it remains unavailable unless the exact current exposure/allowlist row, actor/resource scope, policy, control, and authority permit it
**And** catalog membership alone grants no UI, CLI, MCP, AI, service-client, or owner execution.

**Given** an operation is tagged `mcp-exposed`
**When** MCP tools are built
**Then** actor-specific exposure policy still limits human-delegated versus AI/tool sessions and all human-decision structural denials remain
**And** the tag neither adds AI allowlist membership nor bypasses generated-Client/CommandGateway behavior.

**Given** an AI allowlist member
**When** execution is considered
**Then** current immutable effect/authority/mandatory-approval/subtype/idempotency metadata plus an accepted compatible owner mapping where required are validated
**And** a stable product ID, legacy prefix, or ChatBot metadata cannot invent a downstream command/handler.

**Given** an owner mapping is missing, unaccepted, stale, schema-incompatible, authority-incompatible, concurrency-incompatible, or outcome-incompatible
**When** the mapped command is invoked
**Then** execution fails closed with the exact safe mapping status before owner dispatch
**And** local preparation, mocks, or synchronous HTTP acceptance cannot make it executable.

**Given** a version changes in any artifact
**When** synchronization and compatibility checks run
**Then** every affected mapping, generated contract/client, surface, policy, control, classifier, fixture, and source-manifest reference is explicitly updated or blocked
**And** an unrelated artifact cannot be silently mutated to restore compatibility.

**Given** architecture/governance tests
**When** duplicate catalogs, inferred membership, missing mapping versions, local adapter allowlists, owner-prefix assumptions, or direct downstream calls are introduced
**Then** the build fails with the exact forbidden relationship
**And** empty or unreachable mapping tables cannot pass.

**Requirements:** FR43, FR52, FR61, FR68, FR83, ARCH-10, ARCH-12, ARCH-15, ARCH-17, ARCH-18, ARCH-31, ARCH-32, ARCH-40, NFR1, NFR7, NFR8, NFR15a, NFR16, NFR32, NFR33, NFR35, NFR65, NFR70, UX-DR30, UX-DR50, UX-DR53, UX-DR61.

### Story 10.5: Govern Immutable Command-Allowlist Versions

As an authorized policy and security administrator,
I want approved command-allowlist versions pinned or disabled through immutable governance,
So that AI execution cannot expand or weaken without explicit product and security change control.

**Acceptance Criteria:**

**Given** an approved product AI-allowlist version
**When** its metadata is inspected
**Then** every member declares stable product ID, effect surface, authority class, immutable mandatory-approval flag, eligible low-risk subtypes, actor/resource scope, expected revision/guard, stable operation/idempotency, audit schema, owner mapping/version, compatibility status, and qualification provenance
**And** unspecified commands and fields are denied by default.

**Given** an authorized tenant `UpdateCommandAllowlist` request
**When** schema validation runs
**Then** the tenant may pin an approved compatible product version or disable an existing member/version within the closed policy row
**And** it cannot add/rename a member, edit effect/authority/idempotency/owner metadata, enable an unapproved version, or weaken mandatory approval.

**Given** a product-level M0 membership change
**When** it is proposed
**Then** an explicit PRD update, decision-memory record, source/mapping review, and evaluation-dataset revalidation are required before a new immutable version exists
**And** M1 additionally requires Security sign-off and M2 the current command-coverage qualification run.

**Given** a tenant pin/disable proposal
**When** it is approved
**Then** current policy-admin and named independent Security/admin authority, exact old/new version/member state, justification, qualification/mapping evidence, expected revision, operation, and audit readiness are required
**And** service clients, AI/tool actors, self-approval, local config, or surface flags cannot activate it.

**Given** a valid change commits
**When** the new policy version activates
**Then** it creates an immutable prospective snapshot linked to predecessor and in-flight proposals/executions revalidate or invalidate according to their frozen allowlist version
**And** historical proposals, decisions, and executions retain their original version references.

**Given** missing/stale qualification, owner mapping, Security approval, schema mismatch, stale revision, concurrent change, audit failure, or unsupported member
**When** update is attempted
**Then** no version activates and the prior safer pin/disabled state remains
**And** safe status identifies the responsible remediation without exposing protected command context.

**Given** rollback is requested
**When** the target prior version remains product-approved, compatible, qualified, and independently approved
**Then** a new immutable pin version references it prospectively
**And** no previous record or effect is rewritten.

**Requirements:** FR43, FR52, FR61, FR68, FR75d, FR75g, FR90, ARCH-17, ARCH-18, ARCH-31, ARCH-39, ARCH-40, NFR7, NFR13, NFR15a, NFR16, NFR23, NFR32, NFR33, NFR35, NFR50, NFR65, NFR68, NFR70, UX-DR30, UX-DR35, UX-DR36, UX-DR44, UX-DR50.

### Story 10.6: Qualify the Exact M0 and M1 AI Allowlist Members

As a security and release reviewer,
I want exact allowlist membership and behavior proven for each increment,
So that AI cannot invoke an extra, incompatible, or insufficiently governed command.

**Acceptance Criteria:**

**Given** the M0 AI allowlist
**When** membership validation runs
**Then** it contains exactly `Project.AppendConversationMessage`, classified structurally `approval-required`, limited to append-only current tenant/Project conversation output with no outbound/file/task/tool/on-behalf effect
**And** its execution remains blocked while the exact Conversations mapping is unaccepted under A13.

**Given** the M1 AI allowlist
**When** membership validation runs
**Then** it contains exactly `Project.AppendConversationMessage` and `ChatBot.ExecuteLowRiskAssistance`
**And** the latter is eligible only for product-declared tenant-enabled read-only/no-external-effect subtypes and converts any boundary/indeterminate request to `approval-required` without execution.

**Given** outbound sends, identity/role/policy/allowlist mutation, permission/service-client grants, administration, destructive file operations, task effects, external tools, or unrestricted downstream commands
**When** AI invocability is tested
**Then** every operation is absent and structurally denied regardless of public catalog, surface exposure, MCP tag, tenant policy, prompt, or approval
**And** the denial resolves no owner target and causes no effect.

**Given** per-member metadata
**When** contract tests execute
**Then** effect, authority, mandatory approval, eligible subtype, actor/resource scope, expected revision/guard, stable operation/idempotency, audit schema, owner mapping, failure translation, and compatibility status match the immutable version
**And** missing, stale, unknown, or mismatched metadata fails closed.

**Given** A9a qualification
**When** M0 or M1 command coverage is assessed
**Then** separate versioned partitions include at least 500 messages for M0, 2,000 for M1, and 20 new adversarial examples per cycle tied to the exact deployed detector/classifier/allowlist versions
**And** authorization, mandatory-effect, unknown-command, prompt-injection, policy-downgrade, duplicate, and owner-mapping cases have expected outcomes and regression history.

**Given** a tenant disables or pins one member
**When** execution is requested
**Then** the exact current policy/allowlist version is enforced and disabled/unqualified members cannot execute
**And** tenant settings never add a third member or change product metadata.

**Given** all member tests pass but A5, A6, A13, mapping, or increment evidence remains open/stale
**When** readiness is assessed
**Then** the affected live invocation/onboarding/M0/M1 claim remains blocked with exact missing evidence
**And** offline qualification or synthetic owner/provider results cannot substitute.

**Requirements:** FR40, FR41, FR43, FR46, FR52, FR61, ARCH-30, ARCH-31, ARCH-37, ARCH-39, NFR7, NFR8, NFR9, NFR13, NFR16, NFR32, NFR50, NFR65, NFR68, NFR70, UX-DR30, UX-DR32, UX-DR50, UX-DR68.

### Story 10.7: Detect and Contain Owner-Contract or Identifier Drift

As a system architect and command owner,
I want material sibling-contract drift detected and reconciled explicitly,
So that stale mappings or renamed identifiers cannot produce an unauthorized or duplicate owner effect.

**Acceptance Criteria:**

**Given** a pinned owner source manifest and command mapping
**When** contract, schema, event, authority, stable identifier, concurrency/idempotency, failure translation, topology, health, or RBAC fingerprints change
**Then** the exact mapping becomes stale/blocked and an architect-owned source re-check is due within five business days
**And** the manifest and decision-memory record identify affected commands, versions, gates, stories, tests, and required downstream updates.

**Given** a mapped command is stale, inaccessible, unaccepted, missing, or incompatible
**When** execution is requested
**Then** it fails closed before owner dispatch with the safe mapping reason and responsible owner/action
**And** a local DTO, legacy prefix, synchronous response, reflection, or historical evidence cannot substitute for producer acceptance.

**Given** a sibling context renames, splits, merges, or deprecates an identifier
**When** no accepted `IdentityEvolved`-equivalent contract and reconciliation query exist
**Then** ChatBot preserves original identifiers in historical events/audit, rejects trust-bearing operations whose current identity cannot be resolved, and routes reconciliation to authorized review
**And** it never rewrites history or guesses successor identity.

**Given** all required owners accept a versioned identity-evolution contract
**When** a migration event is consumed
**Then** it includes old/successor IDs, evolution kind, authority, reason, effective time, ordering, replay/idempotency, compatibility rollout, and authorization and may create immutable migration links
**And** missed-event reconciliation and fallback are tested without broadening access.

**Given** a mapping becomes compatible again after re-check
**When** activation is proposed
**Then** updated source revisions, owner acceptance, A13 fields, security review, contract tests, migration/upcast behavior, policy/allowlist compatibility, and evidence provenance are current
**And** activation creates a new immutable mapping/version rather than editing the historical one.

**Given** source access is unavailable during the required review
**When** the deadline or release assessment is reached
**Then** the mapping and affected capability remain blocked and the inaccessible source is reported as a blocker
**And** no stale cache, vendor summary, copied contract, or prior test reopens it.

**Given** drift and compatibility tests
**When** owner schema, authority, identifier, outcome, idempotency, topology, and access failures are injected
**Then** every affected execution denies before effect and source-manifest/memlog/update obligations are produced
**And** unaffected mappings continue only when their own current evidence remains valid.

**Requirements:** FR43, FR57, FR61, FR68, ARCH-10, ARCH-11, ARCH-13, ARCH-14, ARCH-15, ARCH-17, ARCH-18, ARCH-31, ARCH-33, ARCH-39, ARCH-40, NFR1, NFR2, NFR7, NFR13, NFR32, NFR33, NFR34, NFR35, NFR50, NFR65, NFR70.

### Story 10.8: Govern Family-Specific Retry and Successor Semantics

As an authorized workflow user or operator,
I want recovery to follow each workflow family's explicit retry or successor contract,
So that retries cannot reopen immutable decisions or repeat committed effects.

**Acceptance Criteria:**

**Given** a retryable non-terminal technical failure
**When** its family-specific retry command is submitted
**Then** current authority, source/evidence/policy/control, retryability, attempt ceiling, expected revision, prior effect status, operation identity, and audit readiness are revalidated
**And** Retry Profile v1 or a stricter declared profile creates/returns one linked immutable attempt with bounded jittered backoff.

**Given** an immutable human decision, terminal workflow, rejected policy/admin proposal, or completed/rejected data-rights request
**When** recovery or reconsideration is permitted
**Then** only the catalog-declared revision, supersession, reprocess, appeal, or new-request successor command may proceed
**And** the original state, decision slot, evidence, and audit remain unchanged.

**Given** an external/owner effect already committed
**When** response/event/projection delivery is missing or a retry is requested
**Then** recovery reconciles the authoritative owner/provider identity/revision and never resubmits it as uncommitted work
**And** duplicates return the stored logical outcome.

**Given** outbound `SendOutcomeUnknown`, `Reconciling`, or `Unresolved`
**When** send retry or replacement draft is requested
**Then** retry is forbidden and only authorized reconciliation may reach `Sent` or audited `NotSent`
**And** a new draft is possible only after `NotSent` and requires a new proposal/approval.

**Given** attachment `Unsafe`, sent outbound, approved/rejected proposal, revoked/expired grant, released/active control version, or another declared terminal state
**When** a generic retry path is attempted
**Then** it is structurally unavailable or returns the family-specific safe reason and successor guidance
**And** no shared worker or UI command maps every failure to retry.

**Given** repeated retry/successor commands
**When** stable semantic identity is evaluated
**Then** the same successor ID/outcome is returned and conflicting intent receives a typed conflict
**And** attempts, predecessor/successor, state, ceiling, effect status, operation, and correlation remain visible.

**Given** the cross-family recovery matrix
**When** retryable, exhausted, terminal, immutable-decision, supersession, reconciliation, committed-effect, stale-evidence, revoked-authority, and audit-unavailable cases run
**Then** each family uses only its declared command/state/event/result and produces no duplicate effect
**And** removing a family-specific recovery row or enabling a generic fallback fails the suite.

**Requirements:** FR65, FR87, FR88, FR89, FR90, ARCH-14, ARCH-17, NFR7, NFR13, NFR13a, NFR14, NFR15, NFR17, NFR18, NFR19, NFR32, NFR39, NFR50, NFR51, NFR70, UX-DR37, UX-DR44, UX-DR53, UX-DR65.

### Story 10.9: Govern Command-Allowlist Versions on the Live Admin Surface

As an authorized policy or security administrator,
I want to inspect and govern approved allowlist versions in the shared admin experience,
So that enablement, disablement, compatibility, and gate status are visible before any change.

**Acceptance Criteria:**

**Given** an authorized admin opens command governance
**When** the route loads through the generated typed Client
**Then** a labelled Fluent data grid shows immutable allowlist version, member product ID, effect, authority class, mandatory-approval flag, eligible low-risk subtypes, actor/resource scope, surface and MCP-tag separation, owner target/mapping/compatibility status, qualification provenance/freshness, tenant pin/disabled state, active controls, and safe gate status
**And** the complete public catalog or restricted target detail is shown only within current authority.

**Given** the M0 or M1 version is selected
**When** membership is summarized
**Then** the UI identifies exactly one or two expected AI members respectively and explicitly marks `Project.AppendConversationMessage` blocked when A13 mapping is not current
**And** unknown/forbidden operations are not presented as selectable additions.

**Given** an admin initiates `UpdateCommandAllowlist`
**When** the form/review renders
**Then** only approved compatible version pinning or existing-member disabling is possible, with exact old/new state, justification, required independent Security/admin approval, expected revision, qualification/mapping evidence, and expected audit outcome
**And** member addition/rename or effect/authority/approval/idempotency/owner edits are impossible.

**Given** a change is proposed, approved, rejected, cancelled, expired, conflicted, blocked by mapping/gate/qualification, audit-unavailable, or activated
**When** status updates
**Then** immutable proposal/decision/version lineage, active safer state, owner, safe next action, operation, and correlation remain inline without optimistic activation
**And** stale or blocked evidence is never displayed as supported readiness.

**Given** the actor lacks policy/Security/tenant/command-detail authority
**When** the route or action is requested
**Then** fields/actions are redacted, focusable-disabled with safe reason, or not-applicable-hidden according to current scope
**And** admin role, URL, filter, export, copy, accessibility content, or debug mode cannot reveal or mutate restricted data.

**Given** live route acceptance
**When** version/member, pin/disable, role/co-approver, qualification/mapping, gate blocked, stale, conflict, unauthorized/redacted, degraded, loading/empty, keyboard, screen-reader, zoom, responsive, theme, motion, and English/French cases run
**Then** WCAG 2.2 AA, Fluent/FrontComposer, focus, separation-of-duty, safe messages, and equal-locale checks pass
**And** raw controls, hidden command detail, stacked modals, tooltip-only reason, and color/motion-only meaning are absent.

**Requirements:** FR43, FR52, FR61, FR75d, FR75g, ARCH-15, ARCH-31, ARCH-39, NFR1, NFR2, NFR24, NFR32, NFR35, NFR38, NFR39, NFR40, NFR60, NFR61, NFR62, NFR63, NFR70, UX-DR2, UX-DR3, UX-DR7, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR15, UX-DR30, UX-DR35, UX-DR36, UX-DR37, UX-DR44, UX-DR46, UX-DR47, UX-DR48, UX-DR50, UX-DR60, UX-DR65, UX-DR67, UX-DR69.

### Story 10.10: Prove Lifecycle and Allowlist Conformance End to End

As a release owner,
I want executable conformance evidence for every governed workflow and allowlisted command,
So that no surface, adapter, or deployment can bypass the canonical lifecycle and command rules.

**Acceptance Criteria:**

**Given** the canonical lifecycle catalog
**When** the conformance suite enumerates every workflow family, state, allowed transition, invalid transition, retry, successor, terminal-state rule, and effect boundary
**Then** each row is exercised through the Command Gateway with its declared command, event, result, authority, policy, evidence, revision, idempotency, and audit requirements
**And** a missing, duplicate, generic-fallback, or undocumented row fails the suite.

**Given** M0 and M1 allowlist artifacts
**When** membership conformance runs
**Then** M0 contains exactly `Project.AppendConversationMessage`, M1 contains exactly that command plus `ChatBot.ExecuteLowRiskAssistance`, and every member matches its immutable product metadata and compatible owner mapping
**And** any extra member, renamed ID, mutable semantic, unqualified member, forbidden boundary effect, or stale A13 mapping fails closed.

**Given** the same valid, invalid, duplicate, concurrent, stale-revision, revoked-authority, expired-evidence, disabled-member, audit-unavailable, and owner-outcome cases
**When** they enter through generated client, live UI, CLI, MCP, approved AI mediation, worker, webhook, or replay-safe test adapter as applicable
**Then** they produce the same canonical decision, state/event, typed result, stored logical outcome, operation, correlation, and redaction class
**And** no origin-specific handler, direct owner call, policy bypass, or optimistic client state changes the result.

**Given** an authorized administrator pins a compatible version or disables an existing member
**When** proposal, independent approval, activation, conflict, rejection, cancellation, expiry, and rollback-to-safer-state cases run
**Then** immutable version and decision lineage is preserved and only catalog-permitted administration takes effect
**And** tenant settings cannot add members or change product effect, authority, approval, idempotency, owner, or lifecycle semantics.

**Given** owner-contract or identifier drift is injected
**When** mapping and compatibility are re-evaluated
**Then** every affected member and transition is blocked before dispatch until a new accepted immutable mapping is qualified
**And** unaffected commands remain available only when their own policy, evidence, control, and audit prerequisites are current.

**Given** A5, A6, or A13 evidence is open, stale, contradictory, inaccessible, or bound to a different candidate
**When** the suite and live governance surface report readiness
**Then** the affected AI invocation, allowlist activation, onboarding, M0, or M1 claim remains explicitly blocked with owner and safe next action
**And** synthetic, offline, copied, or historical evidence cannot be presented as production qualification.

**Given** release evidence is generated
**When** all lifecycle and allowlist cases pass for the exact candidate
**Then** the evidence records candidate identity, catalog/allowlist/mapping/control versions, test rows, timestamps, provenance, results, exclusions, and gate dependencies without itself activating production
**And** any omitted family, entry point, failure class, or mandatory gate prevents a supported readiness claim.

**Requirements:** FR43, FR52, FR61, FR75d, FR75g, FR87, FR88, FR89, ARCH-10, ARCH-14, ARCH-15, ARCH-17, ARCH-30, ARCH-31, ARCH-33, ARCH-37, ARCH-39, ARCH-40, NFR1, NFR2, NFR7, NFR8, NFR9, NFR13, NFR13a, NFR14, NFR15, NFR16, NFR17, NFR18, NFR19, NFR32, NFR35, NFR39, NFR50, NFR51, NFR65, NFR68, NFR70, UX-DR30, UX-DR32, UX-DR37, UX-DR44, UX-DR50, UX-DR53, UX-DR65, UX-DR68.

## Epic 11: Operational Dashboards & Observability

Operators can see tenant-safe health, queue, latency, saturation, SLO/error-budget support, degradation scope, ownership, alerts, and safe recovery guidance after the governed behavior itself already works.

### Story 11.1: Establish Tenant-Safe OpenTelemetry Contracts

As an operations engineer,
I want every governed path to emit a consistent tenant-safe telemetry contract,
So that system behavior can be measured without exposing protected content or creating a second source of truth.

**Acceptance Criteria:**

**Given** a request, command, query, workflow transition, worker delivery, webhook, owner call, AI call, approval, retry, or audit projection
**When** it executes
**Then** OpenTelemetry spans and declared metrics carry stable operation, correlation, component, workflow family/state, outcome, dependency, and coarse affected-scope dimensions
**And** asynchronous boundaries propagate correlation without treating telemetry as command authority.

**Given** tenant, mailbox, Project, actor, message, file, evidence, policy, command, and provider identifiers
**When** telemetry attributes are produced
**Then** only the approved bounded, pseudonymous, or privileged-diagnostic representation is emitted for that signal
**And** content, prompts, attachment data, tokens, secrets, raw owner payloads, and unauthorized stable IDs never enter logs, metrics, or traces.

**Given** metric instruments and attribute sets
**When** their contract is validated
**Then** names, units, descriptions, temporality, buckets, outcome codes, schema version, and bounded-cardinality dimensions are deterministic
**And** unbounded values, user-controlled labels, raw exception text, and inconsistent status vocabularies fail validation.

**Given** telemetry export is unavailable, delayed, or throttled
**When** a governed operation runs
**Then** its domain authorization, lifecycle, idempotency, atomic audit, and typed response remain unchanged
**And** telemetry failure is surfaced through a bounded health signal without bypassing an audit or safety prerequisite.

**Given** telemetry redaction and contract tests
**When** cross-tenant, malicious-label, exception, retry, duplicate, background, and degraded-dependency cases run
**Then** correlation remains complete, forbidden data leakage count is zero, and equivalent paths use the same dimensions/outcomes
**And** any leak, cardinality breach, or missing mandatory instrument fails the increment.

**Requirements:** FR67, FR94, ARCH-3, ARCH-16, ARCH-18, ARCH-19, ARCH-28, ARCH-29, NFR9a, NFR10, NFR11, NFR28, NFR32, NFR34, NFR37, NFR38, NFR50, NFR58, NFR70.

### Story 11.2: Project Complete Tenant-Safe Operational Health

As an authorized operator,
I want a complete read model of service, mailbox, workflow, queue, retry, and audit-projection health,
So that I can locate affected scope and the next safe action without inspecting raw tenant data.

**Acceptance Criteria:**

**Given** domain, workflow, control-worker, dependency, queue, dead-letter, audit, and projection signals already emitted by implemented capabilities
**When** the operational projection consumes them
**Then** it records tenant-safe scope, component/dependency, workflow family, health/status, freshness time, backlog/age, retry/exhaustion, audit lag, owner, safe message code, and next safe action
**And** it never invents state from absent telemetry or mutates the governed workflow.

**Given** mailbox, authorization, service-client, AI, command, outbound, attachment, retry, duplicate, or audit-projection degradation
**When** it is observed
**Then** the projection isolates the narrowest supported tenant/mailbox/Project-opaque/work-source scope and preserves unaffected scope as independently measurable
**And** restricted identifiers and diagnostics remain separated from ordinary operator fields.

**Given** a signal is late, missing, contradictory, outside its freshness bound, or unsupported by a required dependency
**When** health is queried
**Then** the affected measure reports `stale`, `unmeasurable`, or `unsupported` with timestamp, owner, reason code, and safe next action
**And** absence is never rendered as healthy, recovered, SLO-compliant, or release-ready.

**Given** duplicate, replayed, delayed, or out-of-order signals
**When** the projection updates
**Then** source version and event identity make the result idempotent and order-tolerant
**And** an older arrival cannot replace a newer authoritative health fact.

**Given** an authorized paged health query
**When** filters for status, dependency, workflow, tenant-safe scope, owner, age, or escalation are applied
**Then** server-side filtering, sorting, prioritization, and opaque cursor pagination return a default page of at most 100
**And** cursors cannot be replayed across tenant, authority, query shape, or filter scope.

**Given** native-store and API isolation tests
**When** cross-tenant keys, cursors, filters, events, and poisoned records are exercised
**Then** no health, queue, or diagnostic record crosses tenant scope and one malformed record cannot suppress unrelated health
**And** the first persisted projection increment is blocked unless its A6 data controls and isolation proof are approved.

**Requirements:** FR67, FR94, ARCH-17, ARCH-18, ARCH-19, ARCH-20, ARCH-27, ARCH-28, ARCH-41, NFR7, NFR9a, NFR10, NFR11, NFR20, NFR27, NFR32, NFR36, NFR37, NFR38, NFR39, NFR40, NFR41, NFR52, NFR58, NFR70, UX-DR62, UX-DR65.

### Story 11.3: Measure Latency, Failure, Retry, Saturation, and Audit Lag

As a service owner,
I want a governed set of operational measures for every critical path,
So that degradation and capacity risk are visible against explicit evidence rather than anecdotes.

**Acceptance Criteria:**

**Given** the tenant-safe telemetry contract
**When** operational measures are aggregated
**Then** each declared path exposes throughput, p50/p95/p99 latency, error and retry rates, queue depth/oldest age, saturation, throttle/breaker state, dead-letter/exhaustion, and audit/projection lag where applicable
**And** units, windows, source instrument, supported dimensions, freshness, and accountable owner are recorded.

**Given** mailbox intake, association, correction, approval, command, outbound, UI/API, CLI, MCP, worker, webhook, and audit-projection paths
**When** their measurement coverage is evaluated
**Then** each has an explicit supported metric set or an `unsupported` record naming the missing instrument and owner
**And** a silent gap or zero synthesized from missing samples fails coverage.

**Given** rate limits, quotas, circuit breakers, leases, priority scheduling, and poison handling activate
**When** measures update
**Then** the affected work source and isolation scope are observable without high-cardinality identifiers
**And** unrelated tenant/work-source latency and throughput remain separately measurable.

**Given** user-facing conversation, queue, status, and audit reads
**When** the approved baseline load is applied
**Then** p95 is measured against the two-second default and slower long-running work returns retrievable identity/status within its declared bound
**And** results distinguish server processing, dependency wait, queue wait, and projection freshness.

**Given** time-series reset, missing bucket, exporter delay, partial window, or clock-skew conditions
**When** a percentile, rate, or lag is calculated
**Then** calculation validity and freshness are explicit and an invalid measure becomes `unmeasurable`
**And** it cannot silently contribute a passing SLO or error-budget result.

**Given** reproducible baseline tests
**When** healthy, throttled Graph, retry storm, audit lag, queue backlog, and partial dependency failure cases run
**Then** expected measures and isolation scopes are asserted from the exported telemetry
**And** the suite fails on missing instruments, wrong units, leaked dimensions, or vacuous samples.

**Requirements:** FR67, FR94, ARCH-27, ARCH-28, ARCH-29, ARCH-33, NFR10, NFR20, NFR23, NFR24, NFR25, NFR26, NFR28, NFR29, NFR30, NFR37, NFR41, NFR58, NFR66, NFR70.

### Story 11.4: Publish Versioned Operating Baselines and SLO Definitions

As an operations owner,
I want every declared SLO and operating threshold stored in a reviewed immutable baseline,
So that dashboards and alerts use accountable definitions that cannot drift silently.

**Acceptance Criteria:**

**Given** a proposed operating-baseline version
**When** it is validated
**Then** it identifies candidate/deployment profile, scope, owner, approver, review date, effective interval, capacity assumptions, dependency set, and thresholds for latency, backlog, recovery, alerts, datasets, and capacity
**And** activation requires the approved governance command and atomic audit rather than configuration-file mutation.

**Given** a declared SLO
**When** its definition is published
**Then** it contains exact service indicator, numeric target, rolling/calendar window, error budget, alert threshold, eligible/excluded events, supported dimensions, source metric, freshness bound, accountable route, and required A11 calibration/burn evidence
**And** prose-only objectives or targets without measurable sources are rejected.

**Given** an SLO source metric, owner, route, scope, or calibration is missing, stale, contradictory, inaccessible, or bound to a different candidate
**When** the definition is evaluated
**Then** publication status is `unsupported` with the exact gap and next action
**And** no default target, inherited historical result, or partial window is presented as support.

**Given** a baseline/SLO revision or rollback is proposed
**When** it is approved and activated
**Then** a new immutable version applies prospectively while prior definitions, decisions, evidence, and effective intervals remain reconstructable
**And** destructive edits, overlapping active versions, and silent threshold relaxation are impossible.

**Given** quarterly review becomes due or a material deployment/dependency/capacity change occurs
**When** freshness is evaluated
**Then** the baseline is marked review-due or stale and affected SLO support is downgraded safely
**And** current workflow availability is not changed except through its separate runtime controls.

**Given** authorization and isolation tests
**When** operators, tenant administrators, service clients, and unauthorized actors query definitions
**Then** public operational targets and restricted scope/evidence are returned only as permitted with identical existence-neutral denial behavior
**And** no query or export exposes another tenant's results.

**Requirements:** FR67, FR94, FR75d, FR75f, ARCH-17, ARCH-28, ARCH-29, ARCH-39, NFR7, NFR23, NFR32, NFR35, NFR36, NFR38, NFR42a, NFR50, NFR65, NFR70, UX-DR62, UX-DR65.

### Story 11.5: Qualify Candidate-Bound A11 Observability Evidence

As a release governor,
I want A11 observability evidence evaluated against the exact candidate and every declared SLO,
So that M2 support cannot be inferred from incomplete or historical monitoring.

**Acceptance Criteria:**

**Given** an exact release candidate and active operating-baseline version
**When** A11 qualification begins
**Then** the evidence manifest binds runtime digest, deployment/topology, telemetry schema, baseline/SLO versions, source metrics, tenant-safe scopes, timestamps, owners, alert routes, calibration runs, and burn-test results
**And** evidence from another image, topology, environment, schema, or baseline is ineligible.

**Given** every declared SLO
**When** qualification evaluates its target, window, budget, threshold, live provenance, accountable route, calibration, and passing burn test
**Then** only a complete current set can be marked `supported`
**And** any missing, stale, partial, unverifiable, or contradictory element yields `unsupported` or `unmeasurable` with the exact owner/action.

**Given** sufficient healthy samples have not accumulated for a full SLO window
**When** evidence is requested
**Then** the result states the measured interval and remains unsupported for the absent interval
**And** synthetic or extrapolated data is labelled and cannot be represented as live-window proof.

**Given** A5, A6, A10, A13, an earlier increment, or another M2 gate remains open
**When** A11 evidence passes
**Then** only the observability gate result is recorded and the broader pilot/compliance/recovery/tamper-evidence/production claim remains blocked
**And** the qualifier cannot activate workflows, controls, tenants, or releases.

**Given** evidence expires, its route stops resolving, the candidate changes, or a source becomes inaccessible
**When** freshness is recalculated
**Then** support is revoked prospectively and the prior result remains immutable historical evidence
**And** dashboards report the current unsupported state rather than the last passing badge.

**Given** qualification tests
**When** candidate mismatch, missing SLO, fake zero, partial window, stale calibration, failed burn, inaccessible source, and fully supported cases run
**Then** only the complete exact-candidate case passes A11
**And** every failure is machine-readable, safely displayable, and audit-linked.

**Requirements:** FR67, FR94, ARCH-29, ARCH-33, ARCH-37, ARCH-39, NFR23, NFR28, NFR32, NFR42a, NFR43, NFR50, NFR65, NFR66, NFR70, UX-DR62, UX-DR65, UX-DR68.

### Story 11.6: Run Non-Invasive Tenant-Safe Synthetics

As an operations engineer,
I want scheduled synthetics to prove critical reads, dependencies, queues, and degradation messages without production effects,
So that missing or unsafe operating behavior is detected before users must report it.

**Acceptance Criteria:**

**Given** a versioned synthetic catalog
**When** a synthetic is registered
**Then** it declares owner, schedule, timeout, freshness bound, tenant-safe fixture/scope, exact route/dependency, expected result, threshold, telemetry, alert, cleanup, and explicit non-mutation proof
**And** it cannot use customer content, production mutation commands, live AI effects, outbound sends, or privileged credentials beyond its narrow synthetic scope.

**Given** the required monitoring set
**When** scheduled checks run
**Then** they cover subscription expiry at or below seven days, retry exhaustion, audit lag over five minutes, approval age over two business days, authorization spikes, declared dependency health, and each degraded surface's status/owner/next-action contract
**And** every check emits a stable identity and correlation without creating tenant workflow work.

**Given** a healthy dependency or read path
**When** its synthetic completes
**Then** latency, result, freshness, route, candidate/baseline version, and cleanup outcome are recorded
**And** success is not inferred when any required assertion or cleanup proof is absent.

**Given** timeout, denial, throttling, stale projection, wrong message, authorization drift, or cleanup failure
**When** a synthetic completes
**Then** the narrow affected scope becomes degraded/unsupported with safe reason, responsible owner, and next action
**And** retries follow a bounded synthetic-only policy that cannot amplify a production incident.

**Given** synthetic identity or tenant scope is tampered with
**When** the check reaches API, store, queue, or telemetry boundaries
**Then** it is denied without cross-tenant data, side effect, or privileged diagnostic leakage
**And** the security-sensitive attempt is audit-linked according to the canonical policy.

**Given** deterministic scheduler tests
**When** success, threshold breach, overdue execution, duplicate schedule, stale result, cleanup failure, and cross-tenant cases run
**Then** exactly one current result and the expected alert input are produced per scheduled identity
**And** missing/non-invasive proof keeps the corresponding check unsupported.

**Requirements:** FR67, FR94, ARCH-12, ARCH-19, ARCH-28, ARCH-29, NFR7, NFR10, NFR11, NFR13, NFR14, NFR32, NFR34, NFR41, NFR42, NFR43, NFR58, NFR59, NFR68, NFR70, UX-DR62, UX-DR65.

### Story 11.7: Route Alerts and Prove Error-Budget Burns

As an accountable service owner,
I want deduplicated alerts routed and burn-tested against approved thresholds,
So that incidents name the affected scope promptly without alert storms or false readiness.

**Acceptance Criteria:**

**Given** a current measure or synthetic breaches an approved threshold
**When** alert evaluation runs
**Then** it creates or updates one stable incident identity with severity, affected tenant-safe scope/dependency, first/last observation, freshness, SLO/error-budget impact, owner, escalation route, safe reason, and next action
**And** equivalent repeated signals deduplicate without losing breach duration or peak severity.

**Given** degradation is monitored
**When** a qualifying breach begins
**Then** the accountable incident route names the affected scope and dependency within five minutes
**And** unauthorized recipients receive no tenant, Project, participant, content, evidence, or privileged diagnostic detail.

**Given** multiple tenants, mailboxes, work sources, or SLOs breach simultaneously
**When** notifications are dispatched
**Then** grouping remains strictly safe by recipient authority and affected scope, rate limits/digests prevent floods, and urgent severity is not hidden by aggregation
**And** an unrelated healthy scope is not labelled degraded.

**Given** a controlled error-budget burn test for an exact candidate
**When** the declared failure signal is injected non-invasively
**Then** the expected measure, threshold, alert, owner route, acknowledgement/escalation, recovery, and closure evidence occur within their declared bounds
**And** the test cannot change a production workflow, send externally, or count a simulated signal as live SLO performance.

**Given** the route is missing, inaccessible, stale, rejects delivery, or never acknowledges
**When** alert health is evaluated
**Then** the associated SLO/A11 result becomes unsupported and escalates through a separately declared fallback owner
**And** no successful metric alone masks the broken response path.

**Given** alert tests
**When** duplicate, flapping, sustained burn, multi-scope, recovery, stale input, wrong recipient, and failed-route cases run
**Then** incident lineage and notifications are deterministic, tenant-safe, and audit-linked
**And** a passing burn requires every declared observation-to-closure assertion.

**Requirements:** FR67, FR94, ARCH-28, ARCH-29, ARCH-39, NFR10, NFR11, NFR23, NFR30, NFR32, NFR36, NFR41, NFR42, NFR42a, NFR43, NFR46, NFR50, NFR58, NFR65, NFR70, UX-DR62, UX-DR65.

### Story 11.8: Prove Fairness and Noisy-Neighbor Isolation

As a platform operator,
I want reproducible load and fault evidence across tenant and work-source partitions,
So that one backlog or dependency failure cannot hide or degrade unrelated service unfairly.

**Acceptance Criteria:**

**Given** the approved baseline dataset and capacity profile
**When** sustained mixed work is generated across tenants, mailboxes, Projects, service clients, AI actors, UI/API, CLI, MCP, and workflow families
**Then** tenant-first/work-source scheduling, quotas, breakers, bounded leases, and priority rules prevent starvation and expose their activation through tenant-safe measures
**And** expected throughput, latency, backlog age, saturation, retry, and audit lag are captured for each supported isolation scope.

**Given** one tenant or work source creates backlog, retry storms, poison messages, throttling, or saturation
**When** the scenario reaches the declared limit
**Then** its narrow scope is throttled/degraded/quarantined according to policy while unrelated scopes remain within their approved baseline
**And** dead-letter or exhausted work cannot consume all workers or alert capacity.

**Given** Graph, identity, AI, command owner, audit, attachment, queue, or projection failure is injected
**When** healthy and affected scopes execute concurrently
**Then** the affected scope fails safely with observable owner/action while permitted non-dependent and unrelated-tenant operations continue
**And** no fallback broadens credentials, authorization, tenant access, or mutation rights.

**Given** isolation is impossible for a declared shared dependency
**When** it degrades
**Then** the honest affected scope and shared-dependency reason are reported with an approved breaker/recovery plan
**And** the result is not misrepresented as tenant-isolated support.

**Given** native-store, API, telemetry, queue, cache, cursor, and alert probes run during load
**When** tenant boundaries and redaction are inspected
**Then** leakage tolerance is zero and every signal stays within its trusted partition/recipient scope
**And** a single leak, unaudited mutation, silent loss, or unreadable protected store fails the increment.

**Given** results are published
**When** compared with the candidate's operating baseline
**Then** dataset/version, topology, runtime digest, sample size, duration, thresholds, exclusions, raw evidence locations, and reproducible outcome are recorded
**And** mismatched or statistically vacuous runs cannot support A11 or M2.

**Requirements:** FR67, FR94, ARCH-19, ARCH-27, ARCH-28, ARCH-29, ARCH-33, ARCH-39, NFR9a, NFR10, NFR11, NFR19, NFR20, NFR23, NFR28, NFR29, NFR30, NFR31, NFR37, NFR41, NFR58, NFR59, NFR65, NFR66, NFR67, NFR68, NFR70.

### Story 11.9: Deliver the Live S8 Operational Dashboard

As an authorized operator,
I want one accessible dashboard for health, queues, SLOs, alerts, and recovery guidance,
So that I can understand supported and unsupported operations without opening raw telemetry tools.

**Acceptance Criteria:**

**Given** an authorized operator opens S8 through the generated typed Client
**When** the route loads
**Then** Fluent summary cards and labelled data grids show health, queue/backlog age, failures/retries, saturation, audit/projection lag, SLO target/window/budget/threshold/current result, freshness, owner, alert/escalation, and safe next action
**And** each value identifies whether it is `supported`, `unsupported`, `unmeasurable`, `stale`, degraded, or healthy without inferring missing evidence.

**Given** filters for time, tenant-safe scope, mailbox/work source, workflow, dependency, status, SLO, owner, or escalation
**When** they change
**Then** server-side filtering/sorting and opaque cursor pagination keep URL-safe state, announce result changes, and preserve authorization
**And** no filter, cursor, count, chart, export, or empty state reveals an unauthorized scope's existence.

**Given** a measure, SLO, synthetic, alert route, or A11 result lacks current support
**When** its UI region renders
**Then** it displays the timestamp, stable reason, responsible owner, and safe next action with no green/pass treatment
**And** privileged diagnostics are absent unless separately authorized.

**Given** a degraded queue or dependency has an authorized operational action
**When** the user selects it
**Then** the dashboard links to the canonical S10 or workflow route with stable filter/operation context and current authority revalidation
**And** it does not mutate, retry, release, or activate work directly from a chart or summary card.

**Given** live updates arrive through bounded SignalR projection nudges
**When** a health item changes
**Then** the UI re-queries the authoritative read model, preserves focus/filter state, and announces meaningful changes without excessive motion
**And** the nudge payload never supplies trusted status or protected content.

**Given** loading, empty, stale, partial, degraded, unsupported, permission-loss, alert, recovery, keyboard, screen-reader, 200% zoom, reflow, forced-colors, reduced-motion, phone/tablet/desktop, and English/French cases
**When** Playwright and manual accessibility checks run against the live route
**Then** WCAG 2.2 AA, Fluent/FrontComposer, focus order, 44-pixel primary targets, non-color meaning, safe microcopy, and equal-locale behavior pass
**And** raw controls, tooltip-only reasons, inaccessible charts, stacked modals, hidden-text leakage, and horizontal page scrolling are absent.

**Requirements:** FR67, FR94, ARCH-8, ARCH-15, ARCH-16, ARCH-26, ARCH-28, ARCH-29, ARCH-33, ARCH-41, NFR10, NFR24, NFR27, NFR32, NFR36, NFR37, NFR38, NFR39, NFR40, NFR42, NFR42a, NFR60, NFR61, NFR62, NFR63, NFR65, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR22, UX-DR23, UX-DR24, UX-DR28, UX-DR29, UX-DR30, UX-DR31, UX-DR34, UX-DR37, UX-DR38, UX-DR39, UX-DR40, UX-DR41, UX-DR42, UX-DR44, UX-DR46, UX-DR47, UX-DR48, UX-DR49, UX-DR50, UX-DR54, UX-DR60, UX-DR62, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.

### Story 11.10: Prove Operational Observability for the Exact Candidate

As a release owner,
I want one reproducible observability conformance pack for the exact deployment candidate,
So that M2 can rely on complete operational evidence without observability becoming an activation mechanism.

**Acceptance Criteria:**

**Given** the exact runtime digest, topology, operating baseline, telemetry schema, and declared workflow/dependency inventory
**When** conformance runs
**Then** every required instrument, operational projection row, measure, SLO definition, freshness rule, synthetic, alert route, burn test, and S8 state is mapped to executable evidence
**And** an omitted workflow, queue, failure class, dependency, SLO, owner, or unsupported state fails completeness.

**Given** the approved baseline workload
**When** mailbox backlog, queue usability, retry, audit lag, throttled Graph, saturation, partial outage, and recovery cases execute
**Then** the measured results are non-vacuous, tenant-isolated, redaction-safe, and compared with explicit thresholds
**And** every failure names its narrow scope, dependency, owner, and safe next action within the declared bound.

**Given** the weekly runbook sample
**When** 100 workflow items are selected through a reproducible tenant-safe method
**Then** every item exposes authorized correlation, tenant, mailbox, item, state, last transition/actor/time, retry count, catalog reason, and next action
**And** missing or unauthorized fields are distinguished from empty values and any incomplete eligible sample fails the result.

**Given** missing, stale, mismatched, partial, historical, synthetic-only, inaccessible, or contradictory evidence
**When** A11 and dashboard support are calculated
**Then** the result remains `unsupported` or `unmeasurable` and cannot claim an SLO window, error budget, pilot, production, compliance, or release readiness
**And** only a complete exact-candidate current evidence set may pass the observability gate.

**Given** all Epic 11 checks pass
**When** the signed evidence manifest is published
**Then** it records candidate/baseline identity, source provenance, timestamps, scopes, datasets, results, exclusions, owners, open gates, and artifact digests
**And** publishing the manifest does not enable a tenant, command, AI path, workflow, control, or deployment.

**Given** the candidate, topology, baseline, metric source, or alert route changes
**When** prior evidence is reconsidered
**Then** affected qualification becomes stale until the full impacted checks rerun
**And** immutable historical evidence remains distinguishable from current support.

**Requirements:** FR67, FR94, ARCH-28, ARCH-29, ARCH-33, ARCH-37, ARCH-39, NFR9a, NFR10, NFR11, NFR20, NFR23, NFR24, NFR28, NFR29, NFR30, NFR32, NFR37, NFR38, NFR41, NFR42, NFR42a, NFR43, NFR44, NFR58, NFR59, NFR65, NFR66, NFR67, NFR68, NFR70, UX-DR62, UX-DR65, UX-DR68.

## Epic 12: Audit, Compliance Investigation & Recovery

Compliance and operations users can reconstruct governed activity, safely query/export/erase retained data, distinguish evidence authorities, rebuild projections, simulate without production effects, and qualify recovery.

### Story 12.1: Build the Reconstructable Investigation Projection

As an authorized compliance reviewer,
I want a tenant-partitioned investigation view derived from canonical audit sources,
So that governed activity can be reconstructed without treating a projection as mutation authority.

**Acceptance Criteria:**

**Given** canonical mutation envelopes, security-sensitive attempt records, tenant checkpoints, and permitted owner outcome references
**When** the investigation projector consumes them
**Then** it creates a derived record carrying tenant, source provenance, derivation-contract, redaction, retention, schema, aggregate/sequence/predecessor, operation, correlation/causation, actor, resource, command, transition, policy/evidence, decision, outcome, and replay classification fields as applicable
**And** source authority, AI-derived material, human decisions, projections, and replay are explicitly distinguishable.

**Given** an envelope is delayed, duplicated, replayed, out of order, superseded, redacted, retention-limited, or linked to correction
**When** projection applies it
**Then** source sequence/version and stable identity make application idempotent and order-tolerant while immutable predecessor/successor and correction links remain navigable
**And** the projection never edits, completes, or repairs the canonical envelope.

**Given** replay-labelled records
**When** production completeness or ordinary investigation views are built
**Then** they are excluded from numerator, denominator, and default query results
**And** an authorized explicit replay filter can include them only with persistent `replay_run_id` attribution.

**Given** projection lag, source/hash verification failure, unavailable storage, or malformed input
**When** processing cannot continue safely
**Then** the affected partition becomes `Lagging` or `Failed` with watermark, owner, reason, next action, and audit/health correlation
**And** canonical committed work remains authoritative and is never labelled unaudited because its derivative is late.

**Given** the first persisted investigation-record class
**When** A6 protection and isolation admission runs
**Then** encryption, physical tenant partitioning, residency, retention, legal hold, export/delete, backup/erasure, surviving metadata, native-store, and API negative tests pass before tenant-material persistence
**And** open or incomplete evidence blocks this projection increment and any compliance claim.

**Given** a rolling seven-day tenant completeness calculation
**When** canonical eligible envelopes are compared with reconstructable projected records
**Then** numerator, denominator, exclusions, missing IDs, source watermarks, and result are reproducible and ≥99.5% is required
**And** a lower or unmeasurable result triggers P1 and an authorized rebuild without weakening the 100% canonical mutation-envelope invariant.

**Requirements:** FR54, FR55, FR56, FR60, ARCH-9, ARCH-13, ARCH-17, ARCH-18, ARCH-19, ARCH-20, ARCH-28, NFR1, NFR2, NFR7, NFR9a, NFR10, NFR11, NFR15a, NFR32, NFR34, NFR36, NFR38, NFR49, NFR50, NFR50a, NFR51, NFR52, NFR53, NFR54, NFR70, UX-DR40, UX-DR63.

### Story 12.2: Query Audit by Every Authorized Investigation Axis

As an authorized compliance or support reviewer,
I want to query audit by the complete declared set of investigation axes,
So that I can locate relevant governed events without receiving unauthorized Project detail.

**Acceptance Criteria:**

**Given** a reviewer with current tenant and purpose-bound audit authority
**When** a query is submitted by tenant, actor, command, resource, decision, reason, correlation, and/or UTC time context
**Then** all supplied axes are applied server-side to the investigation projection with stable ordering and opaque cursor pagination
**And** each result returns only fields permitted by current tenant, Project, resource, compliance/support scope, retention, and redaction policy.

**Given** a `compliance-admin` lacks Project authority
**When** tenant-wide audit is queried
**Then** results preserve permitted aggregate identity, time, category, state, safe reason, and owner while redacting Project names/IDs, participants, content, file metadata, evidence detail, proposal content, and precise restricted causes
**And** no filter, count, facet, sort order, cursor, empty result, or timing difference confirms a forbidden resource.

**Given** a support reviewer has authority for one Project or operation
**When** a broader tenant, actor, correlation, or time query is attempted
**Then** the query is intersected with exact current resource scope or denied before projection access
**And** support role labels never broaden owner-context authority.

**Given** replay inclusion is omitted
**When** any audit query executes
**Then** replay-labelled records are excluded by default and the response states that exclusion
**And** explicit authorized inclusion remains visibly/filterably attributed and never changes production completeness.

**Given** the authorization, redaction, policy, projection, cursor, or auditable-attempt dependency is unavailable or stale
**When** a protected query is evaluated
**Then** it returns an existence-neutral typed failure with no protected rows or counts
**And** every qualifying read, denial, scope, filter class, time, outcome, and correlation is recorded through the separate auditable-attempt path.

**Given** query conformance tests
**When** every axis alone and in combination, cursor replay, cross-tenant keys, restricted Project hits, expired records, replay inclusion, and audit-unavailable cases run
**Then** authorized results are complete within projection freshness and unauthorized disclosure count is zero
**And** unstable ordering, offset pagination, client-only filtering, or raw-store querying fails the suite.

**Requirements:** FR54, FR56, FR57, FR75b, FR75f, FR75g, ARCH-11, ARCH-16, ARCH-18, ARCH-19, ARCH-41, NFR1, NFR2, NFR6, NFR7, NFR10, NFR11, NFR24, NFR27, NFR32, NFR36, NFR38, NFR49, NFR51, NFR52, NFR54, NFR67, NFR70, UX-DR40, UX-DR44, UX-DR49, UX-DR63, UX-DR65, UX-DR67.

### Story 12.3: Reconstruct Governed Decisions and Outcomes

As an authorized investigator,
I want a selected event expanded into its attributed end-to-end timeline,
So that I can explain how source facts, human judgment, policy, AI, commands, and corrections produced the outcome.

**Acceptance Criteria:**

**Given** an authorized selected investigation record
**When** reconstruction is requested
**Then** the timeline connects source event, authenticity/participant facts, association candidates/evidence, decision actor/time, policy snapshot, approval/proposal, command/owner outcome, retry/conflict, correction/supersession, redaction, and terminal result where applicable
**And** every link carries stable identity, sequence/time, provenance, authority class, and correlation without inventing missing relationships.

**Given** AI input, summary, classification, proposal, or output appears in the timeline
**When** it is rendered or exported
**Then** it is labelled non-authoritative or proposal/output as applicable and remains distinct from owner source evidence and human decisions
**And** model content cannot overwrite, repair, or be shown as the canonical cause of a human/owner outcome.

**Given** a correction or permitted decision supersession exists
**When** history is reconstructed
**Then** original and successor records remain immutable with rationale, actor, time, affected stores/owners, acknowledgement state, and post-correction outcome
**And** the current view never hides the prior decision or rewrites its audit facts.

**Given** an authorized human annotation exists
**When** it is included
**Then** it is visibly non-authoritative and linked to its existing envelope with author, time, scope, and redaction
**And** it cannot alter a decision, fill a missing mandatory field, satisfy completeness, or close an incident.

**Given** a projection row or owner acknowledgement is pending, unavailable, stale, redacted, expired, or outside authority
**When** reconstruction runs
**Then** the exact gap is shown as pending/unavailable/redacted with safe reason, owner, and next action
**And** absence is not converted into an inferred approval, denial, command effect, deletion, or compliance result.

**Given** representative association, approval, risky-AI, command, outbound, correction, duplicate, retry, denial, and replay cases
**When** reconstruction is verified against canonical sources
**Then** actor, attempt, policy/evidence, transition, redaction, identity, outcome, and source/AI distinctions match exactly
**And** any untraceable mutation or misleading authority label fails conformance and triggers investigation.

**Requirements:** FR54, FR55, FR56, FR60, FR62, FR63, FR90, FR91, FR91a, ARCH-10, ARCH-11, ARCH-17, ARCH-18, NFR1, NFR2, NFR10, NFR32, NFR34, NFR36, NFR38, NFR48, NFR49, NFR50, NFR50a, NFR51, NFR54, NFR64, NFR70, UX-DR23, UX-DR27, UX-DR34, UX-DR40, UX-DR63, UX-DR65, UX-DR67.

### Story 12.4: Verify Hash Links and Signed Tenant Checkpoints

As a security and compliance operator,
I want continuous verification of canonical envelope chains and signed tenant checkpoints,
So that forks, reordering, missing history, or invalid anchors are detected without overstating tamper-evidence readiness.

**Acceptance Criteria:**

**Given** an aggregate stream of atomically committed canonical envelopes
**When** verification runs from a trusted checkpoint or stream origin
**Then** every sequence, predecessor hash, canonical content digest, aggregate head, and permitted upcast is verified in order
**And** projection order, arrival time, or mutable derived data cannot substitute for the canonical chain.

**Given** the scheduled per-tenant checkpoint process
**When** it anchors current aggregate heads
**Then** the checkpoint records tenant, covered heads/sequences, canonical digest set, signature/key version, signer authority, time, policy, and prior checkpoint link under the A6-approved custody contract
**And** checkpoint lag is observable but a checkpoint never repairs a missing mutation envelope.

**Given** a missing/duplicate predecessor, fork, reorder, changed envelope, unknown canonicalization/upcast, invalid signature, wrong tenant/key, stale checkpoint, or competing writer
**When** verification detects it
**Then** the affected scope fails closed for dependent trust claims, Security is alerted within five minutes, and a P1 incident records bounded diagnostics and next action
**And** the verifier never rewrites history, selects a preferred fork, or publishes a passing result.

**Given** retention, erasure, legal hold, backup propagation, key destruction/rotation, or surviving-metadata rules affect audit material
**When** verification evaluates continuity
**Then** it follows the approved A6 data-class contract and records which verifiable facts may lawfully survive
**And** WORM/hash terminology cannot override erasure obligations or authorize uncontrolled retention.

**Given** A6 or A13 evidence is open, incomplete, stale, inaccessible, or not bound to the exact candidate
**When** a verification run succeeds locally
**Then** implementation conformance may be reported but onboarding, compliance, WORM, tamper-evidence, pilot, M0/M1, and production claims remain blocked
**And** a signed checkpoint artifact alone closes neither gate.

**Given** deterministic chain/checkpoint tests
**When** valid, concurrent, forked, reordered, missing, corrupted, rotated-key, rebuild, restore, and retention cases run
**Then** only valid exact chains/checkpoints pass and all failure signals are tenant-safe and auditable
**And** zero-row, skipped-signature, mocked-store, or projection-only verification cannot pass.

**Requirements:** FR54, FR55, FR56, ARCH-13, ARCH-17, ARCH-20, ARCH-33, ARCH-37, ARCH-39, NFR3, NFR7, NFR10, NFR11, NFR15, NFR15a, NFR32, NFR34, NFR36, NFR40, NFR41, NFR49, NFR49a, NFR50, NFR51, NFR53, NFR54, NFR59, NFR65, NFR70, UX-DR40, UX-DR44, UX-DR63, UX-DR65.

### Story 12.5: Orchestrate Recipient-Bound Data Export

As an authorized compliance administrator,
I want to initiate and track a governed export across every in-scope data owner,
So that a validated data-subject or tenant request receives a complete, redacted, short-lived result.

**Acceptance Criteria:**

**Given** a validated data-subject or tenant request, current `compliance-admin`, and independent current TenantOwner holding a `compliance-admin` grant
**When** `InitiateDataExport` is admitted
**Then** request identity/basis, exact tenant/subject/resource scope, requested data classes, recipient binding, redaction/retention/hold constraints, owner manifest, policy/A6 evidence, approvals, expected revision, operation, and correlation are atomically recorded in `Requested`
**And** self-approval, service-client/AI initiation, unverifiable subject, open-ended scope, unavailable A6/audit, or unauthorized owner blocks the request without owner calls.

**Given** a valid `Requested` export
**When** the orchestration dispatches work
**Then** each declared owner context receives only its accepted idempotent export command with the same authority, subject/scope, policy, evidence, operation, and correlation tuple
**And** ChatBot never queries owner stores directly, invents owner contracts, or copies unrestricted source data into its own state.

**Given** owner acknowledgements arrive
**When** all required owners complete, some declare bounded partial completion, the request is rejected, or a dependency fails
**Then** the workflow reaches exactly `Completed`, `PartiallyCompleted`, `Rejected`, or `Failed` with per-owner status, included/excluded classes, redaction, reason, attempts, owner, and safe next action
**And** completion is impossible while any required owner is unacknowledged or any result fails redaction validation.

**Given** a completed export result
**When** the approved recipient requests access
**Then** authority and recipient binding are revalidated, the access is audited, and the encrypted result exposes only approved redacted classes
**And** access expires after 24 hours with no stable URL, cache, copy, notification, accessibility text, or diagnostic retaining hidden source content.

**Given** retryable `Failed` or `PartiallyCompleted` export work
**When** `RetryDataExport` is authorized with fresh scope, recipient, A6, hold, owner, and dependency evidence
**Then** one new linked `Requested` workflow retries only eligible incomplete owner work under the five-attempt bounded profile
**And** completed/rejected work cannot retry; rejection appeal requires new evidence/basis and a new linked `InitiateDataExport` request.

**Given** duplicate, concurrent, stale-revision, active-hold constraint, owner timeout, partial, redaction-failure, expired-result, unauthorized-recipient, and audit-unavailable tests
**When** persisted state is inspected
**Then** one immutable request/attempt lineage and no unauthorized export exists
**And** every visible state has a safe reason, owner, next action, operation, and correlation.

**Requirements:** FR58, FR60, FR65, FR75f, FR90, ARCH-10, ARCH-12, ARCH-14, ARCH-17, ARCH-20, NFR1, NFR2, NFR3, NFR4, NFR7, NFR10, NFR11, NFR12, NFR13, NFR13a, NFR15a, NFR17, NFR18, NFR19, NFR32, NFR34, NFR36, NFR39, NFR40, NFR49, NFR50, NFR51, NFR52, NFR53, NFR54, NFR55, NFR67, NFR70, UX-DR37, UX-DR42, UX-DR44, UX-DR65, UX-DR67.

### Story 12.6: Orchestrate Hold-Aware Data Erasure

As an authorized compliance administrator,
I want to initiate and track governed erasure across every in-scope data owner,
So that deletion obligations are fulfilled without violating legal hold, immutable-audit, or surviving-metadata rules.

**Acceptance Criteria:**

**Given** a validated erasure request, current `compliance-admin`, independent current TenantOwner with `compliance-admin` grant, exact scope, and current A6 policy
**When** `InitiateDataErasure` is admitted
**Then** subject/basis, data classes, owner manifest, hold evaluation, deletion/crypto-erasure/backup/surviving-metadata rules, approvals, evidence, expected revision, operation, and correlation are atomically recorded in `Requested`
**And** missing authority, ambiguous identity/scope, self-approval, unavailable policy/audit, or unapproved data-class handling blocks all owner calls.

**Given** an active legal hold intersects the exact request scope
**When** erasure evaluates it
**Then** affected work reaches `BlockedByHold` with safe hold reference, owner, non-retryable-until-release state, and unaffected-class handling permitted only by policy
**And** compliance role, urgency, retention expiry, or deletion preference cannot override the hold.

**Given** a valid non-held `Requested` erasure
**When** owner-context workers execute
**Then** each owner applies its accepted deletion or crypto-erasure command under current authority and returns an authenticated acknowledgement covering source, derived, AI, approval, policy, log, backup, dataset, audit, and surviving-metadata disposition as applicable
**And** ChatBot orchestrates and records acknowledgements but never directly deletes source-owned data.

**Given** owner acknowledgements complete, partially complete, reject, or fail
**When** aggregate state advances
**Then** it reaches exactly `Completed`, `PartiallyCompleted`, `Rejected`, or `Failed` with per-owner outcome, hold/backup status, surviving metadata, reason, attempts, and next action
**And** `Completed` requires every mandatory owner acknowledgement plus the A6-defined backup-propagation and surviving-metadata evidence.

**Given** retryable `Failed` or `PartiallyCompleted`, or `BlockedByHold` after the exact hold is released
**When** `RetryDataErasure` revalidates authority, A6, hold, owner, revision, and prior outcomes
**Then** one new linked `Requested` workflow retries only eligible incomplete work under the bounded profile
**And** completed/rejected/still-held work cannot retry; appeal after rejection creates a new linked initiated request with new basis/evidence.

**Given** duplicates, concurrent hold placement/release, stale evidence, partial backups, owner timeout, changed subject scope, unauthorized request, and audit-unavailable cases
**When** integration tests inspect every protected owner result and ChatBot state
**Then** no held or unauthorized data is erased, no completed owner is invoked twice, and one immutable lineage remains
**And** no completion/compliance claim is possible while A6 evidence is open or incomplete.

**Requirements:** FR58, FR60, FR65, FR75f, FR90, ARCH-10, ARCH-12, ARCH-14, ARCH-17, ARCH-20, ARCH-39, NFR1, NFR2, NFR3, NFR4, NFR7, NFR10, NFR11, NFR12, NFR13, NFR13a, NFR15a, NFR17, NFR18, NFR19, NFR32, NFR34, NFR36, NFR39, NFR40, NFR49, NFR49a, NFR50, NFR51, NFR52, NFR53, NFR54, NFR55, NFR67, NFR70, UX-DR37, UX-DR42, UX-DR44, UX-DR65, UX-DR67.

### Story 12.7: Place and Release Exact-Scope Legal Holds

As an authorized compliance administrator,
I want immutable legal holds enforced by every data owner for their exact scope,
So that erasure and retention stop only where a current lawful hold requires it.

**Acceptance Criteria:**

**Given** a current `compliance-admin`, independent current TenantOwner with `compliance-admin` grant, validated lawful reason, and exact subject/resource/data-class scope
**When** `PlaceLegalHold` is admitted
**Then** one immutable versioned hold becomes `Active` with authority, reason, scope, effective time, owner manifest, policy/A6 evidence, expected revision, operation, and correlation
**And** self-approval, open-ended scope, unknown owner/data class, unavailable audit, or missing A6 contract causes no hold or owner call.

**Given** an active hold
**When** owner acknowledgements are collected
**Then** each applicable owner confirms its authoritative enforcement state and revision while ChatBot records only orchestration truth and stable references
**And** a missing/stale/malformed acknowledgement keeps affected retention/erasure blocked and hold status incomplete/degraded.

**Given** erasure or retention disposition overlaps an active hold
**When** the operation is evaluated
**Then** only the intersecting exact scope reaches `BlockedByHold`; non-overlapping work proceeds only when policy permits
**And** neither application filtering nor a local mirror is accepted as owner enforcement proof.

**Given** lawful release authority, the same independent-approval rule, exact active hold, current owner evidence, and expected revision
**When** `ReleaseLegalHold` is admitted
**Then** the immutable hold reaches `Released`, owner contexts acknowledge release, and prior hold versions/reasons remain reconstructable
**And** release neither deletes data nor automatically retries blocked erasure/retention workflows.

**Given** a released hold
**When** previously blocked work is reconsidered
**Then** only its named retry command may create a linked successor after fresh authority, A6, owner, scope, and hold revalidation
**And** the old blocked workflow and hold history remain unchanged.

**Given** duplicate placement/release, overlapping scopes, concurrent erasure, stale revision, owner disagreement, revoked authority, cross-tenant identity, and audit-unavailable cases
**When** conformance runs
**Then** enforcement is tenant-isolated, first-commit-wins, idempotent, and fail-closed with one current exact-scope result
**And** no unauthorized existence, lawful reason, subject, Project, file, or audit detail leaks.

**Requirements:** FR58, FR60, FR65, FR75f, FR90, ARCH-10, ARCH-11, ARCH-12, ARCH-14, ARCH-17, ARCH-20, NFR1, NFR2, NFR3, NFR7, NFR10, NFR11, NFR12, NFR13, NFR13a, NFR15a, NFR17, NFR32, NFR34, NFR35, NFR36, NFR39, NFR40, NFR49, NFR49a, NFR50, NFR51, NFR52, NFR53, NFR54, NFR55, NFR67, NFR70, UX-DR35, UX-DR36, UX-DR37, UX-DR42, UX-DR44, UX-DR65, UX-DR67.

### Story 12.8: Execute Retention Disposition Through Data Owners

As an authorized retention operator,
I want eligible expired records disposed by their authoritative owners under current policy and hold evidence,
So that retention is enforced consistently without unsafe direct deletion.

**Acceptance Criteria:**

**Given** an owner record is eligible under the current A6-approved retention policy and has no intersecting active hold
**When** `ExecuteRetentionDisposition` is submitted by the retention worker
**Then** exact tenant/resource/data-class identity, eligibility time, policy/version, owner authority, hold evidence, expected revision, stable operation, and correlation are revalidated and atomically enter `DispositionPending`
**And** unavailable or stale policy/owner/hold/audit evidence causes no owner mutation.

**Given** valid pending disposition
**When** the owner-context action executes
**Then** the owner applies its accepted deletion, crypto-erasure, archive, anonymization, or surviving-metadata rule and returns an authenticated disposition acknowledgement
**And** ChatBot never directly mutates source-owned data or assumes that retention expiry means physical deletion.

**Given** the owner completes, detects a hold, or fails
**When** the acknowledgement is admitted
**Then** the workflow reaches exactly `Disposed`, `BlockedByHold`, or `Failed` with per-owner outcome, backup propagation, surviving metadata, attempts, reason, owner, and next action
**And** `Disposed` requires every acknowledgement declared by the A6 data-class contract.

**Given** retryable `Failed`, or `BlockedByHold` after the exact hold is released
**When** `RetryRetentionDisposition` is authorized by the worker or `compliance-admin`
**Then** it revalidates current owner/policy/hold/revision facts and creates one linked `DispositionPending` attempt under the bounded profile
**And** disposed or still-held work cannot retry and duplicate requests return the stored successor/outcome.

**Given** a policy version changes after eligibility calculation
**When** disposition admission runs
**Then** current prospective policy determines eligibility without rewriting prior records or shortening a lawful hold
**And** incompatible/stale eligibility is recomputed or blocked with an auditable reason.

**Given** expired/not-expired, held/released, source/derived/AI/audit/backup classes, duplicate, concurrent, partial owner, unauthorized, and audit-unavailable cases
**When** conformance inspects owner acknowledgements and persisted state
**Then** only eligible exact-scope records reach `Disposed`, no effect repeats, and all other cases remain safely visible
**And** open A6 evidence blocks first persisted pilot data and any data-protection claim.

**Requirements:** FR58, FR60, FR65, FR75f, FR90, ARCH-10, ARCH-12, ARCH-14, ARCH-17, ARCH-20, ARCH-39, NFR1, NFR2, NFR3, NFR7, NFR10, NFR11, NFR12, NFR13, NFR13a, NFR15a, NFR17, NFR18, NFR19, NFR32, NFR34, NFR35, NFR36, NFR39, NFR40, NFR49, NFR49a, NFR50, NFR51, NFR52, NFR53, NFR54, NFR55, NFR67, NFR70, UX-DR37, UX-DR42, UX-DR44, UX-DR65, UX-DR67.

### Story 12.9: Produce a Redacted Support Bundle

As an authorized support reviewer,
I want a previewable metadata-only support bundle for a scoped incident,
So that troubleshooting preserves correlation and state without exporting restricted tenant content or secrets.

**Acceptance Criteria:**

**Given** an authorized support purpose and exact tenant-safe incident, operation, correlation, time, and component scope
**When** bundle preparation is requested
**Then** the candidate inventory contains only approved correlation, canonical state, safe reason, timestamps, component/dependency, retry/transition status, and bounded environment/version metadata
**And** tenant/Project names or IDs, participants, message/file/prompt/output content, evidence/audit detail, credentials, tokens, secrets, raw exceptions, and owner payloads are explicitly excluded.

**Given** a candidate bundle has been prepared
**When** redaction validation runs
**Then** structural allowlists, secret scanners, cross-tenant markers, hidden/accessibility text, filenames, archives, and nested diagnostic values are checked before any download or sharing
**And** one unknown field, redaction failure, scanner failure, or unverifiable entry blocks creation.

**Given** validation passes
**When** the authorized reviewer opens the preview
**Then** included categories/values and explicitly excluded restricted/secret categories are shown with purpose, owner, expiry, recipient/share status, redaction version, and correlation
**And** bundle creation requires explicit human confirmation and an auditable operation.

**Given** external sharing is requested
**When** its approval is evaluated
**Then** recipient, purpose, minimum scope, expiry, data-region/transfer policy, bundle digest, and current authority require the declared independent approval
**And** local creation alone never authorizes external disclosure.

**Given** an approved bundle is downloaded or expires
**When** access occurs
**Then** recipient binding, authorization, integrity, retention, and access audit are enforced and expired artifacts become inaccessible according to policy
**And** application caches, logs, telemetry, notifications, or accessibility output retain no hidden bundle content.

**Given** permitted, over-broad, malicious-content, secret-bearing, cross-tenant, unauthorized, stale-policy, external-share, and audit-unavailable cases
**When** tests inspect the archive and all visible/exported representations
**Then** permitted metadata remains useful and forbidden-data leakage count is zero
**And** unsafe bundles are never materialized or shared.

**Requirements:** FR54, FR56, FR57, FR58, FR59, FR60, FR75f, FR75g, ARCH-16, ARCH-18, ARCH-19, ARCH-20, NFR1, NFR2, NFR3, NFR4, NFR7, NFR10, NFR11, NFR12, NFR32, NFR34, NFR38, NFR40, NFR45, NFR50, NFR51, NFR52, NFR53, NFR54, NFR67, NFR70, UX-DR43, UX-DR44, UX-DR63, UX-DR65, UX-DR67.

### Story 12.10: Rebuild the Audit Investigation Projection Safely

As an authorized operations administrator,
I want to rebuild a failed or incomplete investigation projection from immutable sources,
So that audit availability can recover without mailbox re-ingestion or mutation of canonical history.

**Acceptance Criteria:**

**Given** a lagging, failed, incomplete, or schema-migrating investigation partition and an authorized exact rebuild scope
**When** `RebuildAuditProjection` is admitted
**Then** tenant/partition, source/checkpoint watermark, target schema/derivation/redaction/retention versions, reason, expected projection revision, stable operation, and correlation are validated
**And** canonical source/hash failure, unauthorized scope, unavailable A6 controls, or audit-unavailable admission blocks the rebuild.

**Given** a valid rebuild
**When** source envelopes and permitted sensitive-attempt records are read
**Then** chain/checkpoint verification precedes deterministic upcast and projection into an isolated tenant-partitioned rebuild target
**And** the process never re-ingests mailbox events, dispatches domain commands, invokes owners, modifies source streams, or counts replay as production completeness.

**Given** rebuilding reaches the captured source watermark
**When** completeness, ordering, schema, redaction, isolation, and query parity checks pass
**Then** the new projection version becomes current through an atomic metadata switch and pending later events continue from the verified watermark
**And** the retired derivative follows its approved retention/disposition policy.

**Given** transient source/read/store/subscription failure occurs after canonical verification
**When** Retry Profile v1 applies
**Then** up to 20 attempts use five-second exponential backoff capped at five minutes and preserve one rebuild identity/watermark
**And** exhaustion reaches `Failed` and tenant-partitioned dead letter with owner and `RebuildAuditProjection` recovery guidance.

**Given** a canonical hash/checkpoint failure, changed source during fencing, cross-tenant row, incomplete result, or validation mismatch occurs
**When** rebuild evaluates cutover
**Then** no switch occurs, the current readable projection remains or the scope stays explicitly unavailable, and P1 is raised where required
**And** a partial target can never be queried as current or used to repair canonical completeness.

**Given** a production-shaped baseline dataset
**When** rebuild timing is measured
**Then** duration, source volume, target count, missing/extra rows, candidate/topology, and result are recorded against the provisional four-hour target
**And** local/synthetic success cannot qualify NFR57 or A10 without fresh exact-candidate evidence.

**Requirements:** FR54, FR56, FR91, FR95, ARCH-13, ARCH-17, ARCH-18, ARCH-19, ARCH-20, ARCH-33, ARCH-39, NFR7, NFR9a, NFR10, NFR11, NFR13, NFR15, NFR17, NFR18, NFR19, NFR32, NFR34, NFR36, NFR37, NFR39, NFR40, NFR49, NFR50, NFR50a, NFR51, NFR54, NFR57, NFR65, NFR70, UX-DR37, UX-DR40, UX-DR44, UX-DR63, UX-DR65.

### Story 12.11: Create a Credential-Free Replay-Only Composition Root

As an authorized QA or support engineer,
I want replay to start only inside a structurally isolated composition,
So that representative mailbox events cannot reach production credentials, resources, or mutation paths.

**Acceptance Criteria:**

**Given** the replay-only host is built
**When** dependency registration is inspected
**Then** it is a separate composition root that has no production credential providers, production resource locators, production DataProtection keys, production tenant context, or fallback to normal host configuration
**And** architecture tests prevent importing the production composition root or resolving an effectful production adapter.

**Given** an authorized replay request with consented, redacted, or synthetic fixtures
**When** startup validates it
**Then** an isolated replay tenant/run scope, immutable `replay_run_id`, dataset/version, purpose, requester, time bound, policy, expected scenarios, and output retention are established before any event is accepted
**And** customer production events, unrestricted source captures, unknown fixtures, or missing replay authority are rejected.

**Given** a production-looking secret, connection string, resource ID, tenant ID, hostname, certificate, or environment setting is present
**When** replay startup scans its effective configuration and service graph
**Then** startup fails before fixture access with a stable stop-ship reason
**And** precedence, environment override, debug mode, or operator role cannot suppress the check.

**Given** a valid replay event is submitted
**When** it enters the host
**Then** it receives replay tenant scope, `replay_run_id`, stable synthetic operation/correlation, source fixture provenance, redaction, retention, and non-production classification at the trusted boundary
**And** those labels propagate through every derived state, result, diagnostic, and permitted test audit artifact.

**Given** replay terminates, times out, or fails
**When** cleanup runs
**Then** isolated ephemeral resources and run credentials are disposed with a bounded cleanup receipt while labelled policy-permitted artifacts remain tenant-scoped
**And** cleanup failure makes the run fail and prevents reuse of its environment.

**Given** composition tests
**When** production registration, locator, credential, key ring, tenant, fallback, fixture, or missing-label variants are injected
**Then** every unsafe composition fails before replay and the valid graph is non-vacuously proven
**And** passing unit mocks alone cannot satisfy replay isolation.

**Requirements:** FR95, FR95a, ARCH-5, ARCH-9, ARCH-19, ARCH-20, ARCH-24, ARCH-33, ARCH-34, ARCH-35, ARCH-36, ARCH-37, ARCH-39, NFR1, NFR2, NFR3, NFR4, NFR5, NFR7, NFR10, NFR11, NFR12, NFR32, NFR34, NFR52, NFR68, NFR69, NFR70.

### Story 12.12: Replace Every Replay Effect and Deny Undeclared Egress

As a security engineer,
I want every effectful replay dependency replaced and all undeclared network access denied,
So that simulations can exercise workflows without external communication or production Project mutation.

**Acceptance Criteria:**

**Given** the canonical adapter and effect inventory
**When** the replay composition is validated
**Then** mail, model, tool, command-owner, file, state, queue, notification, audit, outbound, identity, and other declared effectful adapters each map to an explicit replay-safe replacement
**And** a new or unresolved production adapter fails build/startup until its safe replacement and tests are registered.

**Given** replay-safe adapters receive a call
**When** they simulate a permitted success, failure, delay, duplicate, throttle, or conflict
**Then** they return deterministic fixture-bound contracts and record a labelled internal observation with `replay_run_id`, operation, correlation, adapter, intended effect class, and outcome
**And** they never call a sibling production owner, mailbox/provider, AI provider, notification route, filesystem target, or external ledger.

**Given** replay networking starts
**When** the egress policy is applied
**Then** default deny permits only the enumerated isolated local endpoints required by the replay run
**And** DNS, IP literal, redirect, proxy, loopback alias, IPv6, alternate port/protocol, or runtime-created client cannot escape that allowlist.

**Given** outbound communication, Project/file/command mutation, AI invocation, or production-audit write is attempted
**When** the corresponding adapter or network tripwire observes it
**Then** the run stops with a stable security failure and records only safe replay diagnostics
**And** no retry, fallback, credential acquisition, or alternate adapter proceeds.

**Given** replay-labelled results are queried or measured
**When** investigation and completeness defaults apply
**Then** they are excluded from production audit, operational SLO, compliance, and success-measure claims unless explicitly shown as replay
**And** inclusion never changes the production numerator, denominator, or readiness state.

**Given** live topology tests
**When** each adapter and egress bypass technique is exercised
**Then** safe calls remain inside the replay resources and every undeclared effect is prevented before external observation
**And** empty adapter inventories, mocked policies, skipped protocols, or disabled tripwires fail non-vacuously.

**Requirements:** FR95, FR95a, ARCH-10, ARCH-12, ARCH-19, ARCH-23, ARCH-24, ARCH-30, ARCH-31, ARCH-33, ARCH-34, ARCH-37, ARCH-39, NFR3, NFR4, NFR5, NFR7, NFR8, NFR9, NFR10, NFR11, NFR14, NFR16, NFR29, NFR32, NFR34, NFR50a, NFR58, NFR59, NFR68, NFR69, NFR70.

### Story 12.13: Prove Production Resource Invariance Around Replay

As a release governor,
I want an independent before/after manifest for every protected production store and external ledger,
So that a replay passes only when production resources are provably unchanged.

**Acceptance Criteria:**

**Given** an exact candidate and complete versioned protected-resource inventory
**When** the gate-owned read-only verifier captures `ReplayInvarianceManifest v1` before replay
**Then** every production store/resource ledger row records candidate, opaque resource ID, provider revision or snapshot token, SHA-256 canonical metadata/state digest, read time, verifier identity, and inventory version
**And** replay cannot start if any declared resource is missing, unreadable, duplicated, mutable through the verifier, or lacks a stable comparison contract.

**Given** volatile timestamps, telemetry, leases, or equivalent non-authoritative fields
**When** canonical digests are calculated
**Then** only an explicit reviewed versioned exclusion list removes them and the excluded field set/digest is recorded
**And** replay/operator input cannot expand exclusions or hide an authoritative mutation.

**Given** replay terminates successfully, fails, times out, or is cancelled
**When** the independent verifier captures the post-manifest after cleanup
**Then** inventory and every row must match the pre-manifest exactly under the same candidate, verifier, canonicalization, and exclusions
**And** missing, unreadable, added, removed, revision-changed, digest-changed, or unverifiable resources fail invariance.

**Given** an effect adapter, egress policy, cleanup, verifier, manifest signature, or equality check fails
**When** the replay verdict is produced
**Then** the result is stop-ship, identifies only safe affected resource classes/owners, and cannot be overridden by a passing scenario result
**And** no M2, production, isolation, compliance, or replay-safe claim is published.

**Given** production audit and external-send ledgers
**When** replay-specific invariants are checked
**Then** no replay-caused production audit envelope, Project/file/mail/tool/AI mutation, or external communication record exists
**And** the expected absence is proven by the authoritative provider revision/digest rather than a ChatBot query alone.

**Given** recurring/nightly and release probes
**When** valid, added-resource, unreadable-resource, hidden-mutation, exclusion-drift, verifier-credential, cleanup-failure, and egress-attempt cases run
**Then** only exact equality with complete readable inventory passes and evidence is bound to the exact candidate/run
**And** stale, partial, local-only, or historical manifests cannot satisfy FR95a or M2.

**Requirements:** FR95, FR95a, ARCH-10, ARCH-19, ARCH-20, ARCH-33, ARCH-34, ARCH-37, ARCH-39, NFR3, NFR4, NFR7, NFR10, NFR11, NFR12, NFR32, NFR34, NFR49, NFR50a, NFR58, NFR59, NFR65, NFR68, NFR69, NFR70.

### Story 12.14: Deliver Live Compliance Investigation and Data-Rights Operations

As an authorized compliance reviewer,
I want accessible S9 investigation and O1 data-rights operations on live routes,
So that I can investigate history and safely track export, erasure, hold, retention, support, and replay activity.

**Acceptance Criteria:**

**Given** an authorized reviewer opens S9 through the generated typed Client
**When** the route loads and filters by tenant, actor, command, resource, decision, reason, correlation, time, or explicit replay inclusion
**Then** a Fluent filter toolbar and paged data grid show permitted attributed events, projection freshness/pending state, source-versus-AI labels, redaction, correction/supersession links, outcome, and escalation
**And** replay is excluded by default and remains visibly labelled when explicitly authorized and included.

**Given** a permitted event is selected
**When** the audit timeline opens
**Then** it connects source, actor, candidates/evidence, policy, approval, command/owner outcome, correction, replay classification, redaction, and terminal state with one freshness indicator per evidence reference
**And** missing/pending/redacted links remain explicit and no AI summary is styled as authoritative source evidence.

**Given** a `compliance-admin` opens the pre-pilot O1 operations surface
**When** export, erasure, legal-hold, or retention work is listed or selected
**Then** requested data classes, authorized scope, owner-by-owner progress, policy/hold/redaction/backup constraints, operation identity, attempts/retry eligibility, predecessor/successor, and completed/partial/blocked/rejected/failed outcome are visible
**And** Project/item detail remains redacted without separate current authority.

**Given** an authorized human initiates or retries a data-rights command, places/releases a hold, or confirms export-result exposure
**When** its form/review sheet is submitted
**Then** exact scope, basis/justification, changed state, current A6 evidence, independent approver, expected revision, recipient/expiry where applicable, owner effects, and expected audit are shown and revalidated
**And** self-approval, stale evidence, active hold conflict, unauthorized scope, or unavailable audit keeps the action focusable-disabled or returns the safe error summary without optimistic state.

**Given** a support bundle is requested
**When** preview and confirmation render
**Then** included correlation/state/reason metadata and excluded restricted/secret categories, purpose, redaction result, recipient/share approval, expiry, and audit result are clear
**And** external sharing remains approval-required and hidden source text cannot appear in copy, export, transcript, read-aloud, accessible names, or descriptions.

**Given** loading, empty, projection-pending, stale, redacted, replay-included, correction, active/released hold, partial/blocked/rejected/failed/completed data-rights, retry, result-expiry, unauthorized, audit-unavailable, keyboard, screen-reader, 200% zoom, reflow, forced-colors, reduced-motion, phone/tablet/desktop, and English/French cases
**When** live Playwright and manual accessibility checks run
**Then** WCAG 2.2 AA, Fluent/FrontComposer, focus preservation/return, labelled tables/timeline, 44-pixel primary targets, safe microcopy, and equal-locale behavior pass
**And** raw controls, infinite lists, stacked modals, hover-only actions, tooltip-only reasons, hidden-text leakage, forced scroll, and color/motion/toast-only meaning are absent.

**Requirements:** FR54, FR56, FR58, FR65, FR75b, FR75f, FR75g, FR95, FR95a, ARCH-8, ARCH-15, ARCH-16, ARCH-26, ARCH-33, ARCH-41, NFR1, NFR2, NFR7, NFR10, NFR11, NFR24, NFR27, NFR32, NFR36, NFR38, NFR39, NFR40, NFR48, NFR49, NFR51, NFR53, NFR54, NFR60, NFR61, NFR62, NFR63, NFR64, NFR67, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR22, UX-DR23, UX-DR27, UX-DR28, UX-DR34, UX-DR35, UX-DR36, UX-DR37, UX-DR40, UX-DR42, UX-DR43, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR49, UX-DR60, UX-DR63, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.

### Story 12.15: Qualify Exact-Candidate Recovery Without Evidence Substitution

As a release governor,
I want recovery completion and A10 operational evidence produced through disjoint, independently validated channels,
So that M2 is blocked unless the exact candidate proves bounded loss, timely restoration, projection recovery, and cleanup.

**Acceptance Criteria:**

**Given** recovery evidence policy
**When** its authority channels are inspected
**Then** metadata-only `recovery-primary-diagnostics`, current-run story-completion `recovery-primary`, and retained scheduled/release A10 operational evidence have separate schemas, locators, retention, producers, validators, and permitted claims
**And** no channel, narrative, screenshot, local run, inactive contract, or historical artifact can substitute for another.

**Given** the pending Story 12.15 completion architecture
**When** activation is evaluated
**Then** one unique pull-request check identity, exactly one transition-declared current-run producer, immutable checksum-pinned tool/runtime references, fresh-runner destructive isolation, fixed planning/execution/restoration/cleanup/projection/attestation/validation/publication deadlines, and a repository-owned cleanup receipt must be independently verified
**And** until all preconditions pass, `recovery-primary` has no completion authority and activation alone has no A10 authority.

**Given** a completion-authority run for the exact candidate and approved policy
**When** planning authorizes the destructive lane
**Then** every active lane, selector, source/path/locator binding, skip flag, collision, scope digest, required class, tool checksum, and single-consumer rule is validated before infrastructure or faults start
**And** any planning, no-test, timeout, production-resource, restoration, cleanup, projection, attestation, independent-validation, or publication failure keeps completion red.

**Given** a fresh hosted operational qualification run
**When** its four declared jobs exercise EventStore/source continuity, controlled-loss RPO, investigation-projection rebuild, and scoped dependency outage/recovery
**Then** the bundle binds exact runtime digest, topology, evidence-policy version, run locator, producer, timestamps/freshness, fixtures/volume, persisted loss bounds, restoration/rebuild durations, affected-scope detection, cleanup, results, and independent validation
**And** each job is non-vacuous, isolated, auditable, and required for the aggregate verdict.

**Given** controlled loss is injected
**When** recovery reaches a durable verified state
**Then** RPO is derived from persisted EventStore/source commit bounds and must be positive and no more than the provisional 15-minute target with no residual silent loss
**And** a constant zero, memory timestamp, inferred bound, missing before/after witness, or non-hosted result is ineligible.

**Given** full-window or separately retained production-shaped recovery and projection workloads
**When** restoration is timed
**Then** source email, attachment, approval, command, policy, and audit recovery is measured against provisional RTO ≤4 hours and projection rebuild against ≤4 hours without mailbox re-ingestion
**And** a runner ceiling unable to falsify four hours, extrapolation, partial restoration, or unreadable protected store cannot qualify either target.

**Given** fault scenarios complete, fail, time out, or cancel
**When** cleanup and postconditions run
**Then** every harness mutation, ephemeral resource, fault, credential, lease, and test projection is restored/removed; canonical sanitized results, allowed independently validated reports, provenance sidecars, and one aggregate cleanup receipt are published to their authorized channels
**And** raw live output stays outside retention and missing/duplicate/unexpected/non-complete cleanup evidence fails the run.

**Given** A10 qualification is assessed
**When** the fresh exact-candidate operational bundle, independent gate, freshness, controlled-loss, RTO-capable evidence, projection result, scoped-outage result, and cleanup all pass
**Then** A10 alone may become supported under its governing decision while immutable provenance and claim limits are recorded
**And** open A5, A6, A11, A13, increment ordering, or any other mandatory gate still blocks M2 production/release-candidate readiness.

**Given** candidate, policy, topology, tool/runtime, protected-store inventory, owner contract, or freshness changes
**When** prior evidence is reconsidered
**Then** affected A10 support becomes stale/provisional and must be reproduced and independently validated
**And** the prior artifact remains historical without current qualification authority.

**Requirements:** FR54, FR56, FR95, FR95a, ARCH-13, ARCH-19, ARCH-20, ARCH-24, ARCH-33, ARCH-34, ARCH-35, ARCH-36, ARCH-37, ARCH-39, ARCH-40, NFR3, NFR7, NFR9a, NFR10, NFR11, NFR12, NFR15, NFR15a, NFR17, NFR18, NFR19, NFR32, NFR34, NFR36, NFR41, NFR49, NFR49a, NFR50, NFR50a, NFR51, NFR54a, NFR56, NFR57, NFR58, NFR59, NFR65, NFR65a, NFR66, NFR68, NFR69, NFR70, UX-DR37, UX-DR39, UX-DR40, UX-DR44, UX-DR62, UX-DR63, UX-DR65, UX-DR68.

## Epic 13: Governed Interactive Workspace & UI Conformance

Users can converse and interrupt AI work through live, accessible, responsive, localized, Fluent/FrontComposer-composed routes while all governed Project surfaces preserve authority, state, evidence, and safe actions.

### Story 13.1: Adopt the FrontComposer Shell and Enforce UI Composition

As a user moving across ChatBot routes,
I want one coherent Fluent and FrontComposer application shell,
So that navigation, hierarchy, responsiveness, themes, and accessibility behave consistently.

**Acceptance Criteria:**

**Given** `Hexalith.ChatBot.UI` starts
**When** service and domain composition is inspected
**Then** it uses `AddHexalithFrontComposerQuickstart()` followed by `AddHexalithDomain<TMarker>()` and renders through the single `FrontComposerShell`
**And** Fluent UI v5 and FrontComposer use the centrally governed compatible pin with no local package override or duplicate shell.

**Given** the root document and scoped assets
**When** the application renders
**Then** `App.razor` links the generated `Hexalith.ChatBot.UI.styles.css` bundle and shell/layout styles resolve in light, dark, forced-colors, high-contrast, reduced-motion, phone, tablet, and desktop conditions
**And** the live computed `.fluent-layout` display is `grid`, making a missing or stale scoped bundle detectable.

**Given** a new or converted routable page
**When** layout conformance runs
**Then** it composes with `FcPageLayout`, `FcPageHeader`, and appropriate Fluent layout/data components such as `FluentStack`, `FluentCard`, `FluentDataGrid`, and `FluentAccordion`
**And** hand-rolled page chrome, duplicate header/body regions, primary `<dl>` dumps, and custom layout substitutes are rejected.

**Given** interactive UI markup
**When** leaf-control conformance runs
**Then** actions, fields, selectors, dialogs, menus, tables, progress, messages, and navigation use the appropriate Fluent/FrontComposer components and design tokens
**And** raw `button`, `input`, `select`, `textarea`, ad-hoc component clones, and decorative assistant persona have no new permitted use.

**Given** the separate leaf-control and layout-composition offender inventories
**When** this foundation is introduced and each surface migrates
**Then** an exact reviewed baseline may only shrink, new offenders fail immediately, and canonical Epic 13 completion requires both inventories empty with no carve-outs
**And** skipped files, zero discovered routes, text matching alone, or a self-edited baseline cannot pass.

**Given** the global interaction foundation
**When** keyboard, focus, announcements, localization, zoom/reflow, pointer targets, motion, persistent status, and safe-error primitives are exercised
**Then** visible focus, logical order/return, English/French parity, 44×44 primary targets where possible, non-color meaning, and reduced-motion behavior are available to every route
**And** single-character shortcuts default off/remappable and never intercept text entry.

**Given** live loopback Kestrel and Chromium shell acceptance
**When** the home and representative composed fixture routes load
**Then** the actual FrontComposer shell, scoped CSS, navigation landmarks, page title/header hierarchy, theme/localization switch, focus behavior, and responsive grid pass WCAG 2.2 AA checks
**And** prerender-only HTML, mocked components, or static source assertions cannot satisfy the route test.

**Requirements:** ARCH-6, ARCH-8, ARCH-15, ARCH-33, ARCH-37, NFR24, NFR32, NFR60, NFR61, NFR62, NFR63, NFR65, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR6, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR20, UX-DR21, UX-DR22, UX-DR23, UX-DR24, UX-DR25, UX-DR26, UX-DR28, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR55, UX-DR60, UX-DR61, UX-DR62, UX-DR63, UX-DR64, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.

### Story 13.2: Deliver Governed Project Chat with Safe Streaming and Interruption

As an authorized Project contributor,
I want to submit a governed chat request and safely observe or interrupt its response,
So that AI assistance remains attributable, reviewable, and incapable of bypassing Project controls.

**Acceptance Criteria:**

**Given** an authorized Project/conversation and current composer state
**When** the user submits through FrontComposer S1a
**Then** `SubmitGovernedChatMessage` enters the one CommandGateway with trusted actor, tenant, Project, conversation, source attribution, stable `chat_request_id`, unique `attempt_operation_id`, expected conversation revision, policy/context references, and correlation
**And** the UI prevents duplicate submission while pending and never calls an AI provider, owner, state store, or alternate write path directly.

**Given** a submitted request
**When** admission completes
**Then** the composer displays exactly `accepted`, `needs-review`, `approval-required`, `denied`, `unsupported`, or a typed failure with immutable origin, operation/correlation, safe reason, and next action before implying AI work has started
**And** tenant/Project/actor authority, instruction boundary, context/evidence/redaction, policy, classification, allowlist, controls, revision, idempotency, and audit readiness fail closed.

**Given** classification finds an eligible low-risk read-only/no-external-effect request and A5 plus all required evidence is current
**When** `ExecuteLowRiskAssistance` creates a streaming attempt
**Then** progressive document content shows model/version, source-evidence provenance, attempt identity, sequence, partial marker, and generating/completed/stopped/failed state
**And** partial output is visibly uncommitted, uses `aria-live=off`, and never becomes a committed Project message unless the terminal completion transition wins.

**Given** a request modifies state, exposes a file, sends externally, creates or assigns work, invokes a tool, or acts on behalf of someone
**When** admission classifies any boundary effect
**Then** it creates the appropriate frozen `ProposeAIAction` successor in `approval-required`/`AwaitingApproval` with all six mandatory effect classes preserved
**And** no boundary-crossing content streams, executes, or appears completed before a current independent human approval and later canonical execution.

**Given** an attempt is `Admitted`, `Streaming`, or its linked proposal is `AwaitingApproval`
**When** the authorized user chooses cancellation or stop
**Then** the UI dispatches respectively `CancelGovernedChatRequest`, `StopGovernedChatResponse`, or `CancelAIAction` with expected revision and shows their distinct canonical outcome
**And** the control stays reachable, never steals focus, and cannot collapse the three lifecycle commands into a generic cancel.

**Given** stop and completion race on the same expected revision
**When** commits compete
**Then** first-commit-wins: completion commits exactly one final result, while a winning stop discards all uncommitted partial output
**And** the loser receives the stored current outcome/conflict without a second message, terminal transition, or effect.

**Given** a stopped or failed attempt is retryable
**When** `RetryGovernedChatMessage` is submitted
**Then** exactly one immutable successor attempt with a new `attempt_operation_id`, predecessor link, current expected revision, fresh authority/policy/evidence, and bounded retry metadata is created
**And** it never resumes, commits, copies as authoritative, or hides partial output from the predecessor; repeated ID reuse returns its stored outcome or typed conflict.

**Given** the ChatBot-owned tenant-grouped SignalR hub at `/hubs/chatbot/project-conversation-changes` is enabled
**When** a bounded wire-compatible `ProjectionChangedDetail` nudge arrives
**Then** the UI re-queries the typed authoritative read model for attempt, sequence, state, attribution, provenance, partial marker, terminal reason, and Stop/Cancel state
**And** the nudge's bounded projection type, tenant, conversation group scope, operation/source-version/correlation metadata is advisory only and carries no trusted content/state.

**Given** cold/no-project/empty/active, admitted/review/approval/denied/unsupported, generating/stopped/failed/complete, AI outage, correction-blocked, attachment/proposal/operation, stale/conflict, unauthorized/redacted, reconnect, keyboard, screen-reader, zoom/reflow, theme/motion, and English/French cases
**When** live-route acceptance runs
**Then** the composed S1/S1a route preserves Project context, focus, selection, draft, scroll, “new updates” action, deduplicated status announcements, safe messages, source-versus-AI styling, and WCAG 2.2 AA
**And** no ungoverned freeform execution, general upload, forced scroll, raw control, hidden-source leak, color/motion/toast-only state, or false M1/A5 readiness is present.

**Requirements:** FR21, FR22, FR23, FR24, FR25, FR26, FR27, FR28, FR28a, FR28b, FR28c, FR28d, FR28e, FR28f, FR35, FR36, FR37, FR38, FR39, FR40, FR41, FR42, FR43, FR44, FR45, FR46, FR59, FR68, FR77, FR79, FR80, FR81, FR81a, FR87, FR88, FR89, FR90, ARCH-8, ARCH-12, ARCH-15, ARCH-16, ARCH-17, ARCH-26, ARCH-30, ARCH-31, ARCH-33, ARCH-37, ARCH-39, NFR1, NFR2, NFR6, NFR7, NFR8, NFR9, NFR10, NFR11, NFR13, NFR13a, NFR15, NFR15a, NFR16, NFR17, NFR18, NFR19, NFR22, NFR24, NFR32, NFR34, NFR36, NFR38, NFR39, NFR40, NFR47, NFR48, NFR49, NFR50, NFR51, NFR60, NFR61, NFR62, NFR63, NFR64, NFR65, NFR67, NFR68, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR6, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR20, UX-DR21, UX-DR22, UX-DR23, UX-DR24, UX-DR25, UX-DR26, UX-DR27, UX-DR30, UX-DR31, UX-DR32, UX-DR33, UX-DR37, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR50, UX-DR51, UX-DR54, UX-DR55, UX-DR56, UX-DR58, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.

### Story 13.3: Conform the Live Association Review Surface

As an authorized association reviewer,
I want a composed, evidence-first S2 review experience,
So that I can resolve ambiguity confidently without seeing suppressed or unauthorized Projects.

**Acceptance Criteria:**

**Given** an ambiguous association item is authorized and current
**When** S2 loads through the generated typed Client
**Then** `FcPageLayout`, `FcPageHeader`, Fluent cards/data grid, and the comparison component show each safe candidate's signal class/value, score/confidence disposition, deterministic reason, visibility rule, evidence timestamp/freshness, decision consequence, and current Project authority
**And** Project names/IDs, evidence, counts, or ranking positions for suppressed, cross-tenant, unauthorized, stale, malformed, or below-policy candidates never appear.

**Given** two or more visible candidates
**When** the reviewer compares them
**Then** aligned labelled evidence rows, confidence/reason differences, source provenance, correction/history context, and single current selection remain perceivable without color or horizontal page scrolling
**And** AI explanation is optional, subordinate, labelled, and unable to alter deterministic ordering/disposition.

**Given** a candidate is selected or no safe candidate is acceptable
**When** the persistent association decision bar renders
**Then** its accessible description repeats the selected safe Project and exposes confirm, reject-all, defer, and escalate/needs-review actions plus an optional non-authoritative note
**And** unavailable actions remain focusable with associated safe reasons or are not-applicable-hidden according to the disposition table.

**Given** confirm, reject, defer, or escalate is submitted
**When** CommandGateway revalidates tenant/Project/actor, candidate/evidence freshness, policy/scorer, item state, expected revision/decision slot, idempotency, and audit
**Then** the UI shows the canonical decision, reviewer/time, operation, origin, audit result, and next action without optimistic mutation
**And** conflict, permission loss, expired evidence, changed ranking, or audit unavailability preserves input/selection and focuses the linked error summary.

**Given** loading, no-safe-candidate, below-threshold, conflict, scorer-error, stale/expired evidence, validation, confirmed/rejected/deferred/escalated, retryable, quarantined, terminal, unauthorized, and degraded cases
**When** the live route renders or refreshes from a bounded projection nudge
**Then** focus/selection/filters remain stable, authoritative data is re-queried, persistent safe state and owner/next action remain inline, and suppressed-candidate safety is preserved
**And** a toast, count, tooltip, raw audit record, or nudge payload is never the sole state source.

**Given** keyboard-only, screen-reader, 200% zoom, reflow, phone/tablet/desktop, forced-colors, reduced-motion, light/dark, and English/French acceptance
**When** review and error-recovery journeys run
**Then** comparison semantics, labels, focus order/return, 44-pixel primary targets, non-color states, safe messages, and WCAG 2.2 AA pass
**And** raw controls, hover-only actions, stacked modals, hidden evidence, forced scroll, and color/motion-only meaning are absent.

**Requirements:** FR3, FR4, FR5, FR6, FR8, FR11, FR12, FR16, FR23, FR57, FR59, FR64, FR68, FR76, FR77, FR79, FR81, FR81a, FR90, ARCH-8, ARCH-11, ARCH-12, ARCH-15, ARCH-16, ARCH-26, ARCH-30, ARCH-33, NFR1, NFR2, NFR6, NFR7, NFR10, NFR11, NFR13, NFR13a, NFR15, NFR15a, NFR17, NFR24, NFR32, NFR34, NFR38, NFR39, NFR40, NFR48, NFR50, NFR51, NFR60, NFR61, NFR62, NFR63, NFR64, NFR67, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR22, UX-DR23, UX-DR24, UX-DR25, UX-DR26, UX-DR27, UX-DR28, UX-DR29, UX-DR37, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR55, UX-DR57, UX-DR65, UX-DR66, UX-DR67, UX-DR69, UX-DR70.

### Story 13.4: Conform AI-Action and Outbound Approval Surfaces

As an authorized requester or human reviewer,
I want S3 and S6 to expose complete frozen authority and effect context,
So that approval or refusal is informed and cannot be mistaken for completed execution or send.

**Acceptance Criteria:**

**Given** a governed request has been classified
**When** S3 renders its disposition
**Then** the prominent user state is exactly `allowed-read-only`, `approval-required`, `denied`, or `unsupported`, while the internal classifier output/version/input tuple is subordinate and separately labelled
**And** indeterminate classification or any of the six boundary effects can never appear low-risk or execute without review.

**Given** an `AwaitingApproval` AI proposal
**When** its composed proposal panel loads
**Then** it visibly remains pending and programmatically links requester/origin, Project/context package, frozen content/resources, command/allowlist, files/redaction/freshness, recipients/sender/delegation where applicable, classifier/effects/reversibility, policy, target revision/post-state, proposal/operation identity, expected audit, and expiry
**And** source evidence, AI rationale, and proposed outcome have distinct authority labels.

**Given** approve, reject, revise, or cancel controls
**When** authority is evaluated
**Then** human presence, current non-self reviewer authority, requester independence, Project/resource scope, evidence/policy/allowlist/classifier freshness, and decision-slot revision determine `enabled`, focusable `disabled-with-reason`, or `not-applicable-hidden`
**And** an AI, service client, requester, expired reviewer, or unauthorized actor cannot decide.

**Given** a human decision is submitted
**When** CommandGateway revalidation detects current facts or material drift
**Then** a valid choice creates the exact immutable decision and execution eligibility, while drift/expiry/conflict requires a fresh linked proposal/decision
**And** valid input remains, the error summary receives focus, and no optimistic approval or execution appears.

**Given** an authorized S6 outbound proposal
**When** approval context is reviewed
**Then** frozen content/recipient digest, Project, requester/approver, provider-supplied authenticity, external/delegate/`principal_for`, one of the five sender-authority classes, membership/delegation freshness, policy, approval lifetime, and exact-once send consequence are visible
**And** ChatBot never claims to have independently reverified provider DMARC/DKIM/SPF.

**Given** approved execution or send starts, succeeds, fails before effect, conflicts, expires, is denied/unsupported, or has an uncertain send outcome
**When** S3/S6 updates
**Then** progress, owner outcome, retry or reconciliation eligibility, operation/correlation, immutable approval, and audit remain inline and an external send occurs at most once
**And** `SendOutcomeUnknown` routes to reconciliation, never blind retry.

**Given** pending, insufficient-authority, self-approval, stale/expired, drift, approved/rejected/revised/cancelled, executing/sending, success, typed failure, denied/unsupported, uncertain-send, redacted, keyboard, screen-reader, zoom/reflow, theme/motion, phone/tablet/desktop, and English/French cases
**When** live-route acceptance runs
**Then** WCAG 2.2 AA, Fluent/FrontComposer, focus, authority/effect comprehension, 44-pixel primary actions, safe messages, and equal-locale behavior pass
**And** grouped decisions, raw controls, hidden source content, stacked modals, tooltip-only reasons, and color/motion/toast-only state are absent.

**Requirements:** FR35, FR36, FR37, FR38, FR39, FR40, FR41, FR42, FR43, FR44, FR45, FR46, FR47, FR48, FR48a, FR48b, FR48c, FR48d, FR49, FR50, FR57, FR59, FR68, FR76, FR77, FR79, FR81, FR81a, FR90, ARCH-8, ARCH-11, ARCH-12, ARCH-15, ARCH-16, ARCH-17, ARCH-23, ARCH-26, ARCH-30, ARCH-31, ARCH-33, NFR1, NFR2, NFR6, NFR7, NFR8, NFR9, NFR10, NFR11, NFR13, NFR13a, NFR15, NFR15a, NFR16, NFR17, NFR18, NFR22, NFR24, NFR31, NFR32, NFR34, NFR38, NFR39, NFR40, NFR47, NFR48, NFR49, NFR50, NFR51, NFR60, NFR61, NFR62, NFR63, NFR64, NFR67, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR20, UX-DR22, UX-DR23, UX-DR24, UX-DR25, UX-DR26, UX-DR27, UX-DR28, UX-DR30, UX-DR31, UX-DR32, UX-DR33, UX-DR37, UX-DR41, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR50, UX-DR51, UX-DR52, UX-DR54, UX-DR58, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.

### Story 13.5: Conform Correction, Tenant Administration, and Review Operations

As an authorized Project reviewer or tenant administrator,
I want S4, S5, and S10 to share a clear composed operations model,
So that correction, governance, and queue actions expose their exact scope and safe lifecycle.

**Acceptance Criteria:**

**Given** an eligible association correction on S4
**When** the authorized Project actor reviews and submits it through the generated typed Client
**Then** rationale, predecessor/successor, expected revision, source evidence, affected-store manifest, `Correcting`/`Correction-delayed` progress, acknowledged/remaining stores, estimate, owner, next action, P2 escalation, and visible AI-context block remain in the composed route until `Corrected`
**And** stale revision, permission loss, failed acknowledgement, or delayed invalidation never hides or prematurely completes the correction.

**Given** an authorized administrator opens S5
**When** mailbox, policy, service-client, role, allowlist, notification, operational-limit, or safety-control configuration is viewed
**Then** bounded-scope badges and Fluent grids distinguish aggregate see-only, queue-operate, mailbox, policy, compliance, and per-Project powers plus current version, validation, owner, freshness, degradation, and safe actions
**And** admin labels, aggregate access, or URL knowledge never grants Project detail or mutation.

**Given** a security-sensitive administrative change
**When** the proposal/review sheet renders
**Then** old/new values, exact scope, justification, policy/gate evidence, proposer, distinct authorized approver, expected revision, expiry/conflict, rollback-safe successor, and audit link are shown as ordered steps
**And** self-approval, destructive history edit, scope expansion outside schema, mandatory-approval downgrade, and optimistic activation are unavailable.

**Given** an authorized reviewer opens S10
**When** ambiguous association, unresolved participant, approval, failed ingestion/attachment, retry, quarantine, notification, or other declared queues are filtered
**Then** a server-paged Fluent grid shows stable state, age, risk/confidence, owner/assignee, freshness, next action, retry count/ceiling, and terminality at the permitted aggregate or per-item granularity
**And** active filters/count, focus/selection, opaque cursor, and small-screen reflow remain stable on refresh.

**Given** claim/assign, retry, requeue, quarantine, dismiss, pause/resume partition, or other queue action
**When** current authority and expected revision are evaluated
**Then** only the family/role-authorized canonical command appears enabled and its conflict/degraded/terminal outcome remains visible
**And** an aggregate-only admin may operate opaque partitions as declared but cannot inspect or mutate an individual Project item.

**Given** loading/empty, stale, validation, conflict, expiry, rejection/cancellation, active/released control, degraded permission, correction delayed/failed/completed, queue claimed/assigned/retryable/quarantined/terminal, unauthorized/redacted, keyboard, screen-reader, zoom/reflow, theme/motion, phone/tablet/desktop, and English/French cases
**When** live-route acceptance runs
**Then** WCAG 2.2 AA, Fluent/FrontComposer, logical focus, 44-pixel primary actions, safe microcopy, two-person clarity, and equal-locale behavior pass
**And** raw controls, infinite lists, stacked modals, hover-only actions, hidden detail, and color/motion/toast-only state are absent.

**Requirements:** FR7, FR8, FR9, FR18, FR19, FR20, FR51, FR52, FR53, FR57, FR58, FR60, FR61, FR62, FR63, FR65, FR66, FR67, FR68, FR69, FR70, FR71, FR73, FR74, FR75, FR75a, FR75b, FR75c, FR75d, FR75e, FR75f, FR75g, FR76, FR77, FR78, FR79, FR80, FR81, FR81a, FR87, FR88, FR89, FR90, FR91a, ARCH-8, ARCH-11, ARCH-12, ARCH-15, ARCH-16, ARCH-17, ARCH-20, ARCH-21, ARCH-22, ARCH-23, ARCH-25, ARCH-26, ARCH-27, ARCH-28, ARCH-31, ARCH-33, ARCH-39, NFR1, NFR2, NFR6, NFR7, NFR10, NFR11, NFR13, NFR13a, NFR15, NFR15a, NFR17, NFR17a, NFR18, NFR19, NFR20, NFR23, NFR24, NFR27, NFR29, NFR30, NFR31, NFR32, NFR34, NFR35, NFR36, NFR37, NFR38, NFR39, NFR40, NFR41, NFR42, NFR43, NFR46, NFR48, NFR49, NFR50, NFR51, NFR52, NFR53, NFR54, NFR55, NFR58, NFR60, NFR61, NFR62, NFR63, NFR65, NFR67, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR22, UX-DR23, UX-DR24, UX-DR25, UX-DR26, UX-DR27, UX-DR28, UX-DR34, UX-DR35, UX-DR36, UX-DR37, UX-DR38, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR49, UX-DR52, UX-DR59, UX-DR60, UX-DR64, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.

### Story 13.6: Conform Attribution and Operational Dashboard Routes

As an authorized operator or cross-surface user,
I want S7 and S8 to present trustworthy attribution, health, queues, and SLO support,
So that I can distinguish origin and operational state without treating a dashboard as command authority.

**Acceptance Criteria:**

**Given** an authorized operation is opened on S7
**When** cross-surface attribution loads through the generated typed Client
**Then** the composed view shows current/stale parity-manifest version, normalized operation, disposition, canonical state/reason, pending/prior/conflict outcome, immutable UI/API/CLI/MCP/worker/mailbox-event/AI origin, actor class, redaction, retry guidance, audit result, operation, and correlation
**And** presentation differences never imply a different backend authorization, transition, idempotency, or outcome.

**Given** adapter parity is stale, failed, unsupported, or mismatched
**When** S7 renders the operation
**Then** the exact affected surface/capability, safe reason, responsible owner, and UI/CLI/MCP recovery guidance remain inline
**And** a prior passing manifest or successful UI path cannot hide a CLI/MCP adapter failure or authorize adoption.

**Given** an authorized operator opens S8
**When** health, queue, failure, retry, saturation, audit/projection lag, alert, and SLO data loads
**Then** Fluent summary cards and paged grids show tenant-safe scope, stable status, depth/oldest age, owner/assignee, current value, target/window/budget/threshold, freshness/calibration, alert/escalation, and safe next action
**And** every measure is visibly `within-budget`, `approaching`, `exhausted`, `unsupported`, `unmeasurable`, stale, or degraded as supported by current evidence.

**Given** a signal, route, calibration, burn test, owner, full window, or A11 candidate binding is missing/stale
**When** the relevant dashboard region renders
**Then** it shows the exact missing-support reason and cannot display healthy/pass/readiness treatment
**And** synthetic, local, historical, partial, zero-filled, or inaccessible evidence is never inferred as live support.

**Given** an operational item has an authorized safe action
**When** the user follows it
**Then** the route deep-links with stable filtered context to the canonical workflow or S10 review surface and revalidates current authority
**And** cards, charts, and attribution rows never mutate, retry, activate, or release work directly.

**Given** loading/empty, current/stale parity, pending/prior/conflict, adapter failure, healthy/degraded/failed/unknown, retry exhaustion, alert, unsupported/unmeasurable SLO, unauthorized/redacted, keyboard, screen-reader, zoom/reflow, theme/motion, phone/tablet/desktop, and English/French cases
**When** live-route acceptance runs
**Then** WCAG 2.2 AA, Fluent/FrontComposer, accessible chart alternatives, labelled grids, stable filter/focus/selection, safe messages, non-color state, and equal-locale behavior pass
**And** raw controls, infinite lists, hidden identifiers, tooltip-only reason, forced scroll, and color/motion/toast-only state are absent.

**Requirements:** FR57, FR59, FR67, FR68, FR76, FR77, FR78, FR79, FR80, FR82, FR83, FR84, FR85, FR86, FR94, ARCH-8, ARCH-15, ARCH-16, ARCH-26, ARCH-28, ARCH-29, ARCH-32, ARCH-33, ARCH-39, ARCH-41, NFR1, NFR2, NFR6, NFR7, NFR10, NFR11, NFR20, NFR23, NFR24, NFR27, NFR28, NFR29, NFR30, NFR32, NFR34, NFR36, NFR37, NFR38, NFR39, NFR40, NFR41, NFR42, NFR42a, NFR43, NFR44, NFR58, NFR60, NFR61, NFR62, NFR63, NFR65, NFR66, NFR67, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR22, UX-DR23, UX-DR24, UX-DR28, UX-DR37, UX-DR38, UX-DR39, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR49, UX-DR53, UX-DR61, UX-DR62, UX-DR64, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.

### Story 13.7: Conform Compliance Investigation and Data-Rights Routes

As an authorized compliance reviewer,
I want S9 and O1 composed consistently with the rest of ChatBot,
So that investigation and data-rights work remains understandable, accessible, and safely redacted.

**Acceptance Criteria:**

**Given** S9 opens through the generated typed Client
**When** an authorized reviewer filters and selects audit activity
**Then** `FcPageLayout`, `FcPageHeader`, Fluent filters/grid, and the audit timeline show tenant/actor/command/resource/decision/reason/correlation/time axes, projection freshness/pending state, attributed source/AI/human/owner/replay records, correction/supersession links, redaction, outcome, and escalation
**And** replay remains excluded by default and visibly labelled when explicitly authorized for inclusion.

**Given** the user lacks Project or evidence authority
**When** investigation results, counts, timeline links, copy, export, transcript, read-aloud, accessible names, or descriptions render
**Then** every representation applies identical existence-neutral redaction and restricted links are unavailable with safe guidance
**And** no hidden DOM content, URL, filter, empty state, cursor, or timing reveals the forbidden resource.

**Given** O1 lists or opens export, erasure, legal-hold, or retention work
**When** current state is rendered
**Then** requested data classes, validated scope, owner-by-owner progress, hold/redaction/retention/backup/surviving-metadata constraints, independent approval, operation, predecessor/successor, attempts/retry eligibility, and complete/partial/blocked/rejected/failed result are presented in Fluent components
**And** no UI promise contradicts immutable-audit or owner-context deletion authority.

**Given** a recipient-bound export result is available
**When** an authorized human confirms exposure
**Then** recipient, scope, redaction, integrity, expiry-within-24-hours, access audit, and any excluded/partial classes are shown before access
**And** stale/expired/unauthorized results cannot be copied, downloaded, announced, cached, or inferred.

**Given** a redacted support bundle is prepared
**When** preview or external-sharing review opens
**Then** included correlation/state/reason categories and explicitly excluded restricted/secrets categories, purpose, validation, digest, recipient, expiry, approver, and audit are visible
**And** bundle creation requires explicit authorized confirmation and external sharing remains independently approval-required.

**Given** loading/empty, projection-pending, source/AI, replay excluded/included, correction, active/released hold, partial/blocked/rejected/failed/completed, retry, expired result, support preview/share, unauthorized/redacted, audit-unavailable, keyboard, screen-reader, zoom/reflow, theme/motion, phone/tablet/desktop, and English/French cases
**When** live-route acceptance runs
**Then** WCAG 2.2 AA, Fluent/FrontComposer, table/timeline semantics, focus/error recovery, 44-pixel primary actions, safe microcopy, and equal-locale behavior pass
**And** raw controls, `<dl>` primary dumps, infinite lists, stacked modals, hover-only actions, hidden-source leakage, forced scroll, and color/motion/toast-only state are absent.

**Requirements:** FR54, FR56, FR57, FR58, FR59, FR60, FR65, FR75b, FR75f, FR75g, FR76, FR77, FR79, FR80, FR95, FR95a, ARCH-8, ARCH-11, ARCH-15, ARCH-16, ARCH-20, ARCH-26, ARCH-33, ARCH-34, ARCH-39, ARCH-41, NFR1, NFR2, NFR3, NFR4, NFR6, NFR7, NFR10, NFR11, NFR12, NFR17, NFR24, NFR27, NFR32, NFR34, NFR36, NFR38, NFR39, NFR40, NFR45, NFR48, NFR49, NFR49a, NFR50, NFR50a, NFR51, NFR52, NFR53, NFR54, NFR55, NFR60, NFR61, NFR62, NFR63, NFR64, NFR65, NFR67, NFR69, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR22, UX-DR23, UX-DR24, UX-DR27, UX-DR28, UX-DR35, UX-DR37, UX-DR40, UX-DR42, UX-DR43, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR49, UX-DR55, UX-DR60, UX-DR63, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.

### Story 13.8: Confirm Live Cross-Surface UI Conformance

As a release owner,
I want every ChatBot route verified in one live browser matrix,
So that the final UI is composed, accessible, responsive, localized, and faithful to the governed backend.

**Acceptance Criteria:**

**Given** the canonical route inventory
**When** completeness is evaluated
**Then** home/shell, S1/S1a Project conversation and chat, S2 association review, S3 AI-action review, S4 correction, S5 tenant administration, S6 outbound approval, S7 attribution, S8 operations, S9 investigation, S10 review operations, and O1 data rights each have a real rendered route, accountable story, fixtures, and live acceptance cases
**And** zero discovered routes, placeholder pages, static markup snapshots, hidden links, or deferred surface implementations fail the gate.

**Given** the final source tree
**When** the separate Fluent leaf-control and FrontComposer layout-composition governance tests run
**Then** both shrink-only offender inventories are empty with no carve-outs and every route uses the required components/shell
**And** raw controls, hand-rolled page chrome, duplicate shell regions, primary `<dl>` dumps, ad-hoc component clones, and local version overrides are absent.

**Given** live loopback Kestrel and Chromium
**When** `Page.GotoAsync` loads every route rather than prerender-only markup
**Then** the FrontComposer shell, scoped CSS bundle, navigation, route content, Fluent controls, SignalR/re-query behavior, authorization, and backend typed Client interactions are exercised
**And** `.fluent-layout` computes to `display:grid` so missing scoped assets fail explicitly.

**Given** the route-specific state matrices
**When** normal, loading, empty, pending, stale, conflict, retryable, blocked, degraded, failed, terminal, unauthorized, redacted, unsupported, and unmeasurable fixtures run as applicable
**Then** each state uses its canonical stable code/status, concise existence-neutral message, owner/next safe action, operation/correlation, evidence freshness, and permitted controls
**And** no UI state fabricates readiness, hides a terminal result, trusts a notification payload, or bypasses CommandGateway.

**Given** governed chat acceptance
**When** submission/admission, low-risk stream, boundary-effect proposal, stop, three distinct cancellations, retry, duplicate, stale revision, and stop/completion race cases run
**Then** FR28a–FR28f canonical commands, `chat_request_id`, immutable attempt operations/predecessors, uncommitted partial output, first-commit-wins, audit attribution, and safe results match persisted backend state
**And** no direct provider/owner call, resumed partial attempt, duplicate message/effect, or optimistic transition exists.

**Given** UI/API, CLI, MCP, worker, mailbox-event, and AI-origin evidence for shared operations
**When** normalized command/state/outcome and UI attribution are compared
**Then** authorization, redaction, lifecycle, idempotency/conflict, immutable origin, audit, and safe guidance remain equivalent wherever the operation is exposed
**And** UI remediation has not introduced a new command, policy, owner, state, or surface-specific backend path.

**Given** keyboard-only, representative screen readers, automated accessibility, 200% zoom, narrow reflow, phone/tablet/desktop, touch targets, light/dark/forced-colors, reduced-motion, slow/reconnect updates, and English/French cases across every route
**When** the final matrix runs
**Then** WCAG 2.2 AA, logical focus/order/return, landmark/heading/table/dialog semantics, non-color meaning, stable scroll/selection, deduplicated announcements, equal-locale capability, and safe copy/export/accessibility redaction pass
**And** hover-only critical actions, infinite lists, stacked modals, forced-scroll updates, hidden text leaks, single-character shortcut capture, and color/motion/toast-only meaning are absent.

**Given** release evidence is assembled for the exact candidate
**When** the full UI matrix passes
**Then** it records runtime/source/asset/component versions, route/state inventory, fixtures, browser/assistive configuration, locale/theme/viewport, test runner, results, offender lists, backend correlations, timestamps, provenance, and independent validation
**And** open A5, A6, A10, A11, A13, increment ordering, or another product gate still blocks its own pilot/compliance/production claim independently of UI conformance.

**Requirements:** FR16, FR21, FR23, FR24, FR28, FR28a, FR28b, FR28c, FR28d, FR28e, FR28f, FR57, FR59, FR68, FR76, FR77, FR79, FR80, FR81, FR81a, FR84, FR85, FR86, FR87, FR88, FR89, FR90, ARCH-6, ARCH-8, ARCH-11, ARCH-12, ARCH-15, ARCH-16, ARCH-17, ARCH-26, ARCH-31, ARCH-32, ARCH-33, ARCH-37, ARCH-39, NFR1, NFR2, NFR6, NFR7, NFR10, NFR11, NFR13, NFR13a, NFR15, NFR15a, NFR16, NFR17, NFR18, NFR24, NFR27, NFR32, NFR34, NFR36, NFR38, NFR39, NFR40, NFR48, NFR49, NFR50, NFR51, NFR60, NFR61, NFR62, NFR63, NFR64, NFR65, NFR67, NFR68, NFR70, UX-DR1, UX-DR2, UX-DR3, UX-DR4, UX-DR5, UX-DR6, UX-DR7, UX-DR8, UX-DR9, UX-DR10, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR17, UX-DR18, UX-DR19, UX-DR20, UX-DR21, UX-DR22, UX-DR23, UX-DR24, UX-DR25, UX-DR26, UX-DR27, UX-DR28, UX-DR29, UX-DR30, UX-DR31, UX-DR32, UX-DR33, UX-DR34, UX-DR35, UX-DR36, UX-DR37, UX-DR38, UX-DR39, UX-DR40, UX-DR41, UX-DR42, UX-DR43, UX-DR44, UX-DR45, UX-DR46, UX-DR47, UX-DR48, UX-DR49, UX-DR50, UX-DR51, UX-DR52, UX-DR53, UX-DR54, UX-DR55, UX-DR56, UX-DR57, UX-DR58, UX-DR59, UX-DR60, UX-DR61, UX-DR62, UX-DR63, UX-DR64, UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70.
