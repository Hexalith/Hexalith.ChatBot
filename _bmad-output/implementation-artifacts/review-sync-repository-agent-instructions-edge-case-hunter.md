# Edge Case Hunter Review Prompt

Invoke the `bmad-review-edge-case-hunter` skill on this diff:

<diff>
===== ROOT TRACKED DIFF =====
diff --git a/_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md b/_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md
index c49c8d4..2ce7e9e 100644
--- a/_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md
+++ b/_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-17.md
@@ -1,6 +1,9 @@
 ---
 stepsCompleted:
   - step-01-document-discovery
+  - step-02-prd-analysis
+  - step-03-epic-coverage-validation
+  - step-04-ux-alignment
 inputDocuments:
   - _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
   - _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
@@ -47,3 +50,449 @@ inputDocuments:
 - No required planning-document category is missing.
 - The PRD and UX folders have no standard `index.md`; the primary documents above were explicitly confirmed.
 - Decision logs, validation reports, reconciliation reports, rubric reviews, and accessibility reviews remain supporting evidence rather than authoritative assessment inputs.
+
+## PRD Analysis
+
+### Functional Requirements
+
+- FR1: The system can capture authorized mailbox events as project collaboration inputs.
+- FR2: The system can preserve source email identity, thread identity, mailbox identity, sender, recipients, timestamps, and attachment references.
+- FR3: The system can associate incoming email with an existing project using deterministic evidence.
+- FR4: The system can detect ambiguous project association and route it to human review.
+- FR5: Authorized users can review candidate projects with visible evidence, confidence state, reason codes, and the consequences of each available decision.
+- FR6: Authorized users can choose a candidate project, reject all candidates, defer association, mark an item as needing review, and provide an optional decision note.
+- FR7: Authorized users can correct a previously selected project association.
+- FR8: The system can record association decisions, corrections, rejections, deferrals, retries, and skipped items.
+- FR9: Tenant administrators can configure project association rules, evidence requirements, and the confidence thresholds `T_high` and `T_low`. The score domain, signals, defaults, calibration protocol, and change guardrails are defined by the addendum. Both knobs are security-sensitive: changes require tenant-admin authorization and audit, must remain schema-bounded, and cannot be made by service clients or AI actors.
+- FR10: The system can preserve original email context when association is rejected, deferred, failed, skipped, or awaiting review.
+- FR11: The system can expose deterministic association reasons and confidence inputs in machine-readable form for UI, CLI, MCP, audit, and test verification.
+- FR12: Authorized users can compare candidate project evidence side by side when resolving ambiguous association.
+- FR13: The system can resolve internal and external email participants to tenant-scoped parties.
+- FR14: Authorized users can identify unresolved participants for review.
+- FR15: External participants can contribute project context through email without requiring MVP external portal access.
+- FR16: The system can enforce tenant and project authorization before exposing project candidates, files, conversations, approvals, commands, or audit details.
+- FR17: The system can block unresolved or unauthorized actors from accessing project files, creating task requests, triggering commands, or sending outbound communication.
+- FR18: Tenant administrators can configure governed mailbox participation rules.
+- FR19: Authorized administrators can configure service-client access for CLI, MCP, background workers, mailbox events, and AI actors.
+- FR20: The system can record consent or lawful-basis metadata where tenant policy requires it for external participants, retained email content, attachments, and AI processing.
+- FR21: Authorized users can view email-derived messages as project conversation context.
+- FR22: The system can represent associated email, participants, attachments, decisions, approvals, failures, and AI outcomes in the project context. Story decomposition must treat all seven as independently accepted concerns on the S1 surface.
+- FR23: Authorized users can inspect why an email belongs to a project, including source evidence, confidence signals, human decisions, and later corrections. The evidence panel must show signal class, matched value, confidence score, threshold band, decision actor and timestamp, and superseding-correction links.
+- FR24: Authorized users can see association, attachment, task, approval, command, failure, retry, and next-action status for a project conversation.
+- FR25: The system can keep project conversation context separate across tenants and projects.
+- FR26: The system can distinguish informational project context from actionable requests. Each email must carry an `informational` or `actionable` badge; actionable items also expose detected intent and the review/capture/dismiss next action from the reproducible tag-and-heuristic kernel.
+- FR27: The system can distinguish system-generated summaries from source evidence so users do not confuse AI interpretation with original email, attachment, or command facts. AI content must be visually and textually labeled, carry model/version/timestamp/source provenance, permit source-evidence reveal, default to source evidence, and not rely on color alone.
+- FR28: The system can preserve visible human-review history for each email, attachment, approval, AI action, and command.
+- FR29: The system can capture attachments from associated project email.
+- FR30: The system can store captured attachments in governed project folders.
+- FR31: Authorized users can inspect attachment capture and storage status.
+- FR32: The system can prevent unauthorized actors from viewing attachment metadata or content.
+- FR33: The system can make authorized project files available as scoped AI context only through explicit authorization, policy checks, and auditable context packaging.
+- FR34: The system can represent attachment states including captured, pending, unavailable, rejected, unsafe, failed, and retryable.
+- FR35: The system can detect candidate task or action intent from authorized project conversation actors and preserve the source message evidence. The task-intent contract includes tenant/project/source/requester identity, a ≤280-character summary, a four-value action-kind enum, source evidence offsets, kernel version, confidence, detection time, and state. Precision/recall targets are ≥80%/≥75% for M0 and ≥90%/≥85% for M1 against A9a.
+- FR36: Authorized users can review captured task intent before governed action, including the FR35 contract, full source message, and FR37/FR38 transitions.
+- FR37: Authorized users can convert captured task intent into a governed task or action request linked to an FR41 proposal; conversion is audited.
+- FR38: Authorized users can mark captured task intent as not actionable, duplicate, already handled, or out of scope. Each is terminal and preserved for evaluation; duplicate links its predecessor.
+- FR39: The system can classify AI action requests by risk.
+- FR40: The system can allow low-risk AI assistance when tenant policy and project authorization permit it.
+- FR41: The system can require approval before AI actions that modify project state, expose files, send external communication, create or assign tasks, invoke tools, or act on behalf of a participant.
+- FR42: Authorized users can approve or reject proposed AI actions after reviewing the action summary, affected project resources, external recipients, sender authority, risk classification, and expected outcome. The surface must expose the allowlisted command, redaction-aware input-file evidence, recipients, sender-authority class, classifier input tuple, policy snapshot, expected post-state/audit emissions, and approve/reject/request-revision/cancel choices; unauthorized approval is disabled with a reason.
+- FR43: The system can execute approved AI actions only through allowlisted governed commands.
+- FR44: Authorized users can inspect AI action proposals, approvals, denials, executions, failures, and outcomes.
+- FR45: Authorized users can preview outbound communication, file access, command execution, and AI-generated changes before approval or execution.
+- FR46: The system can refuse or block unsafe AI, automation, command, or mailbox requests that exceed tenant policy, project authorization, sender authority, or approved command scope.
+- FR47: Authorized users can create outbound project email drafts within approved project and sender authority.
+- FR48: The system can distinguish draft-only, authenticated-user send, shared-mailbox send, send-on-behalf, and approved service-send authority. M365 permission does not override ChatBot authority; conflicts fail closed.
+- FR48a: Every inbound message intake event records provider-supplied M365/Exchange DMARC, DKIM, and SPF verdicts; ChatBot does not re-verify them.
+- FR48b: The mailbox adapter parses `Received`, `Authentication-Results`, `From`, `Reply-To`, `Sender`, and `X-Original-Sender`, records disagreements as intake metadata, and feeds them to risk classification and reviewer display without blocking adapter ingestion.
+- FR48c: Provider-expressed delegated send records the delegate as sender authority and the principal as `principal_for`; outbound behavior is symmetric.
+- FR48d: Unresolved tenant-party senders are flagged `external_sender = true`; `mailbox.authenticity-strictness` (`permissive`, `strict`, `paranoid`) controls auto-association, NeedsReview, or fail-closed behavior.
+- FR49: The system can require approval before outbound project communication leaves the project boundary.
+- FR50: The system can preserve proposed content, approved content, recipients, sender authority, project context, requester, approver, and decision outcome in approval records.
+- FR51: Tenant administrators can configure mailbox integration settings and monitored mailbox patterns.
+- FR52: Tenant administrators can configure AI action policy for low-risk and approval-required actions.
+- FR53: Tenant administrators can review mailbox permission status and degraded mailbox processing states.
+- FR54: Compliance or support reviewers can investigate association decisions, approval decisions, command outcomes, and risky AI actions.
+- FR55: The system can produce audit records for security-sensitive association, participant, file, approval, command, AI, retry, and duplicate-handling events.
+- FR55a: Vector indexes, embedding stores, prompt-context caches, candidate-ranking caches, and any derived store holding tenant-derived material must enforce tenant isolation by construction. Cross-tenant native/store-layer reads must fail below the application, verified by periodic isolation probes per NFR59.
+- FR56: Authorized users can query audit records by tenant, actor, command, resource, decision, reason, correlation, and time context.
+- FR57: The system can hide unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details.
+- FR58: Authorized administrators or reviewers can access operational support for tenant data retention, export, and deletion workflows.
+- FR59: The system can propagate correlation context across mailbox intake, project association, file handling, approval, AI mediation, command execution, audit, UI, CLI, and MCP.
+- FR60: The system can preserve source evidence used for association, authorization, approval, rejection, refusal, correction, retry, and audit investigation with retention boundaries and redaction behavior.
+- FR61: The system can maintain versioned policy snapshots used for association, authorization, approval, AI action classification, and command execution decisions.
+- FR62: Authorized users can add human notes or resolution rationale to association, participant, approval, retry, quarantine, and correction decisions.
+- FR63: Authorized users can supersede reversible human decisions where policy permits while preserving the original decision in audit history.
+- FR64: The system can detect duplicate mailbox delivery and avoid duplicate project artifacts.
+- FR65: The system can retry failed mailbox, attachment, association, approval, command, and projection work where retry is valid.
+- FR66: The system can surface terminal and non-terminal failure states to authorized users.
+- FR67: The system can expose mailbox health, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, and audit projection status. Every health/queue view must show its name, depth or stable `healthy`/`degraded`/`failed`/`unknown` status, oldest-item age, triage owner, detail link, and freshness timestamp within NFR6 staleness.
+- FR68: The system can fail closed when project association, participant identity, tenant scope, authorization, audit writing, or required dependencies cannot be resolved.
+- FR69: Authorized users can view and manage queues for ambiguous associations, unresolved participants, pending approvals, failed ingestion, failed attachment handling, and retryable operations.
+- FR70: Authorized users can assign or claim review items that require human resolution.
+- FR71: Authorized users can see the next required human action for an email, task intent, attachment, approval, or failed operation.
+- FR72: The system can notify authorized users when review, approval, failure, degraded mailbox, quarantine, or retry states require attention.
+- FR73: Tenant administrators can configure notification routing and escalation rules for unresolved review, approval, degraded, quarantine, and failure states.
+- FR74: Authorized administrators can disable, quarantine, or rate-limit mailbox sources, service clients, AI actors, or command capabilities producing unsafe, invalid, excessive, or policy-violating activity. Story decomposition covers each subject/action pair; disable and quarantine require FR75d two-person approval, while rate limiting is a standard policy mutation.
+- FR75: Authorized administrators can configure per-tenant rate limits, quotas, and circuit breakers for mailbox processing, AI mediation, command execution, and outbound communication.
+- FR75a: `tenant-admin` holds the union of FR75b–FR75g scopes; `mailbox-admin`, `policy-admin`, `compliance-admin`, and `operations-admin` hold proper subsets. Admin assignment is audited, security-sensitive, and unavailable to service clients or AI actors.
+- FR75b: Admins may see tenant-wide queue summaries, health/status enums, and aggregate metrics without project membership; per-item project names, evidence, files, and audit reasons still require project authority.
+- FR75c: Admins may retry, requeue, quarantine, or dismiss queue-level items they can see, with actor/items/queue/reason audit; these operations cannot mutate project-level associations, files, or approvals.
+- FR75d: `policy-admin` may mutate schema-declared tenant-policy knobs; security-sensitive changes require a second administrator and audited justification.
+- FR75e: `mailbox-admin` may configure mailbox patterns, routing rules, and provider credentials but cannot read mailbox content or decide associations.
+- FR75f: `compliance-admin` may read tenant audit records subject to project redaction, trigger investigations, and configure retention within NFR49a bounds, but cannot operate workflow items.
+- FR75g: Every admin operation, including above-threshold read-only dashboard access, produces an audit event containing identity, scope, affected items, and timestamp; no admin scope bypasses NFR15a or NFR50a.
+- FR76: The system can present review items with clear available actions, disabled-action reasons, and next-step guidance based on item state and authorization. Each action is visibly `enabled`, `disabled-with-reason`, or `not-applicable-hidden`; disabled reasons come from the finite safe catalog and guidance identifies a role or available action rather than generic support.
+- FR77: The system can explain refusal, blocked action, degraded mailbox, failed attachment, failed command, and authorization-denied states in user-safe language without exposing restricted evidence. Messages come from a versioned catalog with a stable code, ≤80-character headline, safe one-sentence reason, and retry/escalate/dismiss/request-access action; restricted detail remains audit-only.
+- FR78: Authorized users can filter, sort, and prioritize operational queues by age, risk, confidence, project, mailbox, failure state, assigned reviewer, and next action.
+- FR79: The system can show stale, waiting, blocked, and escalation-needed states for review queues and long-running operations.
+- FR80: UI, CLI, and MCP users can retrieve long-running operation status including operation identity, current state, retry count, partial outputs, safe next actions, terminal reason, and correlation context.
+- FR81: Authorized UI users can perform the core governed email-to-project workflow operations.
+- FR81a: Every state-mutating operation from UI, CLI, MCP, service client, AI actor, or background worker passes through one command spine. The admission layer applies authentication, tenant binding, authorization, risk classification, approval, coarse idempotency, and pre-commit audit before EventStore fine idempotency, execution, publication, projection, and post-commit audit. Surface adapters only translate to typed Commands and cannot replicate or bypass stages; architecture review must reject violations.
+- FR82: Authorized CLI users can inspect, associate, reject, defer, correct, retry, approve, execute, check status, and query audit for the same governed workflow through the FR81a pipeline.
+- FR83: Authorized MCP clients can access the same governed workflow operations for AI-agent and automation use through the FR81a pipeline.
+- FR84: The system can return equivalent authorization outcomes and state transitions across UI, CLI, and MCP; divergence is a defect against FR81a, not tolerated surface variation.
+- FR85: The system can identify whether an action originated from UI/API, CLI, MCP, background worker, mailbox event, or AI actor. Origin is immutable after adapter attachment and travels with the Command into audit.
+- FR86: Contract tests must verify that equivalent surface inputs produce the same canonically normalized Command record. Failure is an invariant violation; parity enforcement remains structural.
+- FR87: The system can define canonical lifecycle states for email intake items, participant resolution, attachment handling, approvals, AI actions, command executions, and audit projection.
+- FR88: The system can validate inbound and outbound workflow state transitions against an explicit state model.
+- FR89: The system can reject invalid state transitions and record the rejected transition, actor, reason, and correlation context.
+- FR90: The system can expose idempotency keys and stable resource identifiers for mailbox events, email messages, attachments, approvals, commands, retries, outbound communication, and audit records.
+- FR91: The system can separate immutable source records from derived project projections and rebuild derived projections from source records when needed.
+- FR91a: Correcting an association invalidates and rebuilds every dependent candidate ranking, evidence snapshot, consumed AI proposal, M2 vector entry, and queue projection. The item remains visibly `correcting` until all stores acknowledge invalidation; AI cannot use corrected context meanwhile. Audit preserves the predecessor, correction, and per-store result.
+- FR92: Authorized product or QA users can maintain internal evaluation datasets from consented, redacted, or synthetic examples with expected outcomes, redaction expectations, and regression history.
+- FR93: The system can provide tenant-scoped test fixtures or sandbox data for mailbox intake, association, authorization, attachments, approval, AI mediation, command execution, and audit validation.
+- FR94: The system can expose measurable outcomes for ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag through NFR42a M2 telemetry and FR67 intermediate views.
+- FR95: The system can simulate or replay representative mailbox events for authorized QA or support investigation without external communication or production project mutation.
+- FR95a: Replay/simulation runs execute in a dedicated test tenant with every external action intercepted and recorded rather than sent. Audit envelopes carry `replay_run_id`; production queries and NFR50a exclude replay by default. A nightly probe verifies that replay never writes any production tenant outbound trace, and failures gate M2.
+- FR96: The system can make recorded correction decisions available as future association evidence only when tenant policy permits, the evidence remains explainable, and users can inspect its influence.
+
+**Total functional requirements: 111** (FR1–FR96 plus FR48a–FR48d, FR55a, FR75a–FR75g, FR81a, FR91a, and FR95a).
+
+### Non-Functional Requirements
+
+- NFR1: All command and query operations must enforce tenant, actor, role, project, and resource authorization before returning data or mutating state.
+- NFR2: Unauthorized human and machine actors must receive redacted failures that reveal no restricted project name, file metadata, candidate evidence, audit detail, or tenant data.
+- NFR3: Email content, attachments, AI prompts/outputs, audit records, tokens, policy snapshots, logs, traces, backups, and evaluation datasets must be encrypted in transit and at rest with tenant-appropriate separation. Release validation verifies TLS, encrypted persistence for each data class, and no plaintext protected content in exports.
+- NFR4: Secrets and mailbox, service-client, CLI, MCP, AI-tool, and provider credentials must not appear in logs, traces, client output, audit payloads, support bundles, or user diagnostics.
+- NFR5: M365/Exchange permissions and all machine/client credentials must be least-privilege and revocable without broader fallback access.
+- NFR6: Authorization, policy, and identity caches must have bounded staleness and revocation-sensitive invalidation: default maximum five minutes for ordinary changes and 60 seconds for explicit revocation, verified automatically.
+- NFR7: Security-sensitive operations must fail closed when identity, tenant scope, authorization, audit readiness, policy evaluation, or required validation is unavailable.
+- NFR8: AI actors must operate only through explicitly authorized project scope, files, tools, commands, and policy authority.
+- NFR9: AI prompts, context, outputs, tool results, and summaries must remain tenant/project scoped, policy-redacted and retained, and blocked from unauthorized training, telemetry, or reuse. Every AI context package must contain tenant/project IDs, evidence references, policy snapshot, redaction decision, retention class, and provider-reuse setting before invocation.
+- NFR9a: M2 vector indexes, embedding stores, prompt-context caches, and candidate-ranking caches must be tenant-partitioned at the storage layer. Native cross-tenant queries must fail even if the application omits a filter; nightly synthetic probes are stop-ship gates.
+- NFR10: Logs, metrics, traces, support bundles, and test artifacts must pass secret and sensitive-data redaction checks before export or sharing.
+- NFR11: Cross-tenant isolation tests have zero tolerance for exposure through candidates, evidence, files, summaries, prompts, CLI/MCP, telemetry, or audit.
+- NFR12: Region and residency boundaries for every persisted data class must be defined before onboarding a tenant profile that specifies residency and verified as mapped or explicitly unconstrained.
+- NFR13: Mailbox intake, attachment capture, association, approval, command execution, outbound communication, notifications, and audit projection must be idempotent per operation with stable key, replay window, conflict behavior, and identical observable state for equivalent inputs.
+- NFR13a: The addendum's eight-operation idempotency table is binding for key composition, replay window, equivalence, and conflict response. Every new operation class must extend it before shipping.
+- NFR14: Duplicate delivery must not duplicate project messages, attachments, task intents, approvals, commands, notifications, outbound email, or audit decisions.
+- NFR15: Invalid transitions must be rejected before mutation with deterministic error and audit. If audit storage is unavailable, every state mutation fails closed.
+- NFR15a: Fail-closed behavior is structural for every durable-write path. The binding path inventory is:
+  - M365 intake fails closed on unresolved tenant or unavailable audit; unavailable scanning permits only audited quarantine.
+  - Deterministic association fails closed on scorer error, authorization failure, or unavailable audit.
+  - Human ambiguous association fails closed on insufficient project authority, evidence staler than NFR48, or unavailable audit.
+  - Correction fails closed on insufficient ownership, unavailable invalidation queue, or unavailable audit.
+  - AI proposal fails closed on indeterminate classifier, unavailable classifier evaluation data, or unavailable audit.
+  - Approval fails closed on insufficient risk-class authority or unavailable audit.
+  - Command execution fails closed on absent allowlist entry, idempotency failing open, authorization failure, or unavailable audit.
+  - M1 outbound send fails closed on sender-authority mismatch, unapproved adapter mode, or unavailable audit.
+  - Tenant-policy mutation fails closed on non-admin actor, out-of-schema value, or unavailable audit.
+  - M1 allowlist mutation fails closed on insufficient security/admin authority, failed evaluation gate, or unavailable audit.
+  - Audit unavailability returns typed `AuditUnavailable`, queues intent without durable business state, and alerts operators; replay resumes only after audit health returns.
+- NFR16: Risky AI actions, external sends, command execution, and project-file packaging must not execute without verified approval, policy snapshot, actor authority, input validation, and audit readiness.
+- NFR17: Partial failures must remain visible and recoverable as pending, retryable, failed, quarantined, or needs review.
+- NFR17a: Correction propagation must complete within p95 ≤10 minutes in M0/M1 and p95 ≤60 minutes in M2 including vector reindex. Breaches show `correction-delayed`, owner, and next safe action and constitute a P2 incident; the M2 value is A11-calibrated.
+- NFR18: Each workflow retry policy must define retryable/terminal errors, attempts, backoff, jitter, dead-letter criteria, recovery actions, and visible terminal reasons.
+- NFR19: Workers and asynchronous processors must safely support at-least-once delivery through idempotency, concurrency control, expiring leases/locks, and poison handling.
+- NFR20: Queue processing must prevent starvation across tenants, mailboxes, projects, and item types while respecting priorities, limits, and circuit breakers.
+- NFR21: File processing must enforce malware/unsafe-content policy, sizes, types, scan status, quarantine, and safe failure before project or AI exposure.
+- NFR22: Non-AI association, review, approval, retry, and audit workflows must remain usable during AI-provider outage when their own dependencies are available; outage tests must prove it.
+- NFR23: Tenant/deployment operating baselines must be versioned, owned, approved, reviewed at least quarterly, and define latency, backlog, recovery, alert, dataset-size, and capacity thresholds.
+- NFR24: User-facing conversation, queue, status, and audit lookups target p95 ≤2 seconds under the MVP baseline, measured by synthetic checks and production APM unless a stricter profile applies.
+- NFR25: Candidate generation targets p95 ≤10 seconds or returns pending/manual-review with retrievable identity and safe next actions.
+- NFR26: Long-running CLI/MCP calls return operation identity and status within p95 ≤5 seconds and do not hold a connection beyond 30 seconds without a retrievable status response.
+- NFR27: Queue views provide server-side filtering, sorting, pagination, and prioritization with default page size ≤100 across all enumerated dimensions.
+- NFR28: Operational latency telemetry includes percentile distribution, errors, retries, queue age, saturation, and audit lag.
+- NFR29: Tenant-level limits, quotas, and circuit breakers protect mailbox, AI, commands, outbound, UI/API, CLI, and MCP.
+- NFR30: Backlog in one tenant, source, actor, or surface must not degrade unrelated tenants or workflow sources where isolation is feasible.
+- NFR31: M365/Exchange integration must tolerate revocation, expiry, throttling/backoff, partial access, duplicates, delays, webhook replay, subscription expiry, and permission drift without broadening access.
+- NFR32: UI/API, CLI, MCP, workers, handlers, persisted events, audit, projections, and replay fixtures must use contract-verifiable identifiers, codes, states, redaction, correlation, and authorization outcomes.
+- NFR33: API, client, event, audit, projection, and state contracts require backward-compatible evolution or explicit versions, deprecation, and breaking-change migration.
+- NFR34: Integration requests/events carry correlation across intake, file handling, association, approval, command, AI, audit, UI/API, CLI, MCP, workers, and webhooks.
+- NFR35: Configuration/policy changes must be audited, versioned, consistently applied to new work, and rollback-capable only for non-destructive settings; destructive or authority-expanding changes require a new version.
+- NFR36: Workflow, audit, retry, approval, retention, freshness, and SLA time uses server-side UTC while preserving source timezone context and converting only at presentation.
+- NFR37: Authorized operators must observe mailbox health/backlog, unresolved and ambiguous queues, approvals, retries, duplicate suppression, authorization/service-client/AI/command failures, and audit projection lag.
+- NFR38: User-visible status must remain separate from privileged diagnostic detail and follow authorization.
+- NFR39: The system must give actionable status for degraded, stale, waiting, blocked, escalation-needed, failed, retryable, and terminal states.
+- NFR40: Degraded/blocked/failed/waiting states use FR77 catalog guidance. Production telemetry must report zero uncategorized raw-error states per release; any nonzero count blocks release.
+- NFR41: Dependency degradation must be isolated to the narrowest identifiable tenant/mailbox/project/operation/client/item/surface scope; monitored incidents identify scope and dependency within five minutes.
+- NFR42: Every authorized degraded-state surface displays the stable state, affected scope, responsible owner role, and safe next action within NFR6 freshness. Synthetic checks require all four.
+- NFR42a: M2 per-tenant operations and the addendum publish SLOs for ingestion, candidates, ambiguous resolution, command classes, audit lag, retries, duplicates, mailbox failure, approval age, and AI latency. Each SLO defines target, window, error budget, and alert threshold, initially from NFR24–NFR27/NFR43 and calibrated by A11.
+- NFR43: Tenant-safe, non-invasive alerting and synthetic checks use documented thresholds. Defaults include subscription expiry within seven days, retry exhaustion, audit lag above five minutes, approval age above two business days, and authorization spikes over tenant baseline.
+- NFR44: Runbook-ready item diagnostics include correlation, tenant, mailbox, item ID, state, last timestamped actor transition, retry count, FR77 reason code, and next safe action. Weekly samples of 100 items must be complete.
+- NFR45: Redacted support bundles preserve correlation, state, and reason without restricted tenant/project/party/file/message/audit evidence.
+- NFR46: Approval-fatigue prevention is measurable and includes:
+  - Priority ordering by risk class × affected-party authority × queue time, configured through tenant-policy weights.
+  - Grouping by requester × command × project, with eligible batch decisions still emitting one audit event per item.
+  - Push-notification ceilings of ≤8/hour and ≤30/day per user, excess rolled into one digest, and duplicate proposals suppressed during their replay window.
+  - Tenant-admin load alerts when one reviewer exceeds 25 open approvals.
+  - Median/p95 queue time per risk class in M2; fatigue triggers when >15% of rolling seven-day approval-required decisions occur within five seconds of first display.
+- NFR47: Risky automation must distinguish reversible, supersedable, compensating, and irreversible actions before approval.
+- NFR48: Every surfaced evidence reference carries its timestamp and `fresh`/`stale`/`expired` state from NFR6. Expired evidence disables approval with `evidence-expired`; stale evidence is visibly flagged, and approval chip count equals evidence count.
+- NFR49: Audit records must be tamper-evident, retention-governed, redaction-aware, reconstructable, and modifiable/deletable only by authorized retention workflows.
+- NFR49a: M2 audit uses an append-only WORM store and per-tenant hash-chained envelopes. Storage-layer deletion is impossible; redaction appends a record and separately encrypts the original with a shreddable KMS key. Nightly chain verification alerts security within five minutes; GDPR erasure tombstones projections and shreds keys without mutating the chain.
+- NFR50: Audit includes tenant, actor/type, command, resource, decision/reason, correlation/time, policy snapshot, evidence, transition history, redaction, idempotency, and outcome. Automated tests require 100% field presence for security-sensitive association, approval, command, retry, duplicate, and AI events in the dataset.
+- NFR50a: M2 production audit completeness is the share of NFR15a mutations reconstructable end-to-end from the chain alone. The per-tenant rolling-seven-day target is ≥99.5%; lower is P1. Replay is excluded per FR95a.
+- NFR51: Audit/diagnostics must reconstruct actor, attempt, policy, evidence, transitions, redactions, and outcome.
+- NFR52: Retention must minimize email/attachment content, prompts/outputs, diagnostics, and support bundles to authorized workflow, audit, and policy needs.
+- NFR53: Retention/export/deletion distinguishes source email, metadata, attachments, projections, AI content, approvals, policy snapshots, logs, backups, datasets, and audit.
+- NFR54: Audit evidence must respect retention and redaction so preservation does not become uncontrolled storage.
+- NFR55: Policy/regulatory profiles may require consent or lawful-basis metadata for external parties, retained content, attachments, and AI processing.
+- NFR56: Source email/attachment, approval/command history, policy snapshots, and audit target RPO ≤15 minutes and RTO ≤4 hours unless stricter profile values apply.
+- NFR57: Projections must rebuild from immutable source/audit within the default four-hour recovery target for the baseline dataset without mailbox re-ingestion.
+- NFR58: Dependency outages must degrade only the narrowest identifiable scope; tests prove unrelated tenants/mailboxes remain available across Graph, identity, AI, commands, audit, and attachment failures.
+- NFR59: Resilience validation must prove Graph degradation, subscription expiry, AI outage, command failure, audit outage, and partial attachment failure cause no cross-tenant leak, unauthorized mutation, or silent loss.
+- NFR60: WCAG 2.2 AA applies per increment to existing UI, excluding CLI/MCP:
+  - M0: ambiguous-association review, AI-action approval, and project conversation.
+  - M1: rejection/defer lifecycle flows, approval-policy configuration, and FR75a–FR75g admin view.
+  - M2: mailbox, failed-association, approval, duplicate, AI-outcome, and audit-lag dashboards.
+  - Each release requires automated checks plus keyboard-only and screen-reader review for every in-scope surface.
+- NFR61: Accessibility validation covers keyboard flows, screen-reader labels, focus order, non-color status, and error recovery for association and approval.
+- NFR62: Status, failure, refusal, and authorization messages must be understandable, evidence-safe, and not color-only.
+- NFR63: Association/approval users must identify the next action without reading raw audit logs.
+- NFR64: UI must distinguish source evidence from AI summaries so decisions use authoritative context.
+- NFR65: Production releases must gate tenant isolation, authorization, redaction, idempotency, transitions, approvals, duplicate suppression, and audit creation.
+- NFR66: Performance validation must prove mailbox backlog processing, queue usability, retry behavior, audit lag, and throttled Graph behavior against documented baselines.
+- NFR67: Security validation includes negative authorization for UI/API, CLI, MCP, workers, mailbox events, service clients, and AI actors.
+- NFR68: Datasets/fixtures must be consented, redacted, or synthetic and carry versioning, reproducibility, redaction verification, expected outcomes, and regression history across all named workflow risks.
+- NFR69: Replay/simulation is isolated from production mutation, external sends, live AI tool execution, and live commands; artifacts are labeled and tenant-scoped.
+- NFR70: Every externally visible operation defines its state transition, audit event, response, redaction, and retry/idempotency result.
+
+**Total non-functional requirements: 77** (NFR1–NFR70 plus NFR9a, NFR13a, NFR15a, NFR17a, NFR42a, NFR49a, and NFR50a).
+
+### Additional Requirements
+
+#### Delivery and scope constraints
+
+- The MVP is one release delivered in strict dependency order M0 → M1 → M2. M0 proves the UI vertical loop, M1 adds cross-surface parity and full governance, and M2 adds operations, recovery, continuity, tamper evidence, replay isolation, and derived-store isolation.
+- No increment may trade away tenant isolation, authorization, fail-closed state mutation, per-operation idempotency, audit completeness, or governed AI approval. Scope reduction must first remove polish, advanced inference, advanced policy flexibility, document-intelligence breadth, and dashboard breadth.
+- M0 uses one controlled M365/Exchange mailbox pattern, three deterministic association signals, one UI surface path, and exactly one AI command. M1 introduces CLI/MCP/service-client actors, outbound draft/send, full lifecycle and policy/admin scope. M2 closes production-operability and continuity requirements.
+- Source-of-truth ownership remains with Projects, Parties, Folders, Tenants, EventStore, and mail integration. ChatBot owns orchestration plus the explicitly enumerated, tenant-partitioned derived governance records.
+- CLI and MCP are governed clients over public application/service APIs and may not access databases, queues, indexes, mail stores, or tenant storage directly.
+- Required integrations are Projects, Parties, Folders, Tenants, EventStore, FrontComposer, Microsoft 365/Exchange, Keycloak, Aspire, CLI, and MCP. Integration contracts are versioned and correlated end to end.
+
+#### Binding addendum contracts
+
+- Association scoring uses `[0.0, 1.0]`; M0 defaults are `T_high = 0.90` and `T_low = 0.60`. `≥T_high` permits automatic association only with deterministic evidence; `[T_low,T_high)` requires review; `<T_low` fails to NeedsReview. M0 guardrails prohibit `T_high < 0.80` or `T_low < 0.50` without a documented evaluation run.
+- The M0 risk classifier is deterministic tag-and-heuristic over command, policy classification, effect surface, and requester authority. Indeterminate classification falls back to approval-required. M1 may add an explanatory LLM layer but not delegate classification to it.
+- The exact M0 AI allowlist is `Project.AppendConversationMessage`, approval-required and bounded to in-tenant/in-project append-only behavior. M1 introduces a versioned command catalog with effect, authority, default-risk, policy-override, and idempotency metadata.
+- Tenant policy is a closed, versioned product schema. M0 includes association thresholds, unsafe-attachment behavior, a per-action-class low-risk map defaulting false, and mailbox routing rules. M1/M2 add approval/admin/authenticity, allowlist, dashboard, replay, retention, and idempotency controls.
+- The shared command pipeline order is binding: authentication → tenant binding → authorization → risk classification → approval → coarse idempotency → pre-commit audit → EventStore fine idempotency/execution → event publication → projection → post-commit audit. Adapters only translate inputs to typed Commands.
+- Idempotency has eight operation-class contracts: message intake, association, approval, command execution, outbound send, AI proposal, correction, and retry. Each defines key composition, replay window, canonical equivalence, and conflict response.
+- Replay uses a dedicated test tenant and intercepting outbound adapter, labels every audit envelope with `replay_run_id`, excludes replay from production queries/completeness, and is guarded by a nightly no-production-write probe.
+- Sibling identifier rename/split/merge/deprecation uses `IdentityEvolved` → immutable `ProjectionIdentityMigration`; original audit IDs are never rewritten and queries return original plus successor identities.
+- Inbound authenticity preserves provider DMARC/DKIM/SPF results and parses security-relevant headers. Sender authority has five fixed classes, and every M365-versus-ChatBot authority conflict fails closed.
+- The operating baseline catalog includes stable SLO names, targets, windows, budgets, alerts, calibration sources, and tenant scopes. Several M2 values remain explicitly `calibration-pending` until A11 pilot evidence exists.
+
+#### Assumptions requiring closure or continued validation
+
+- A1–A2: The pilot can use M365/Exchange and one controlled mailbox pattern before advanced mailbox cases.
+- A3: MVP parity is required, though the first machine-surface proof may begin with association, status, and audit.
+- A4: Default NFR operating baselines remain provisional until tenant-specific approval.
+- A5–A6: AI-provider handling can satisfy tenant policy, and audit reconstructability can coexist with GDPR retention/deletion.
+- A7–A8: External users do not need portal access in MVP, and a fixed AI command allowlist is sufficient.
+- A9/A9a: The Test Architect can maintain a representative labeled dataset (≥500 M0, ≥2,000 M1) with the required taxonomy, refresh cadence, and adversarial additions.
+- A10: RPO ≤15 minutes and RTO ≤4 hours remain starter targets until an M2 continuity drill proves or revises them.
+- A11: Pilot adoption, latency, correction, notification, and SLO starter values require 2–4 weeks of tenant baseline evidence and per-increment recalibration.
+
+### PRD Completeness Assessment
+
+The PRD is unusually comprehensive and strongly test-oriented: it provides a numbered catalog, measurable thresholds, actor and surface boundaries, lifecycle semantics, audit fields, failure behavior, decomposition guidance, acceptance details, increment ownership, and a binding addendum for architecture-sensitive contracts. The extraction found a continuous base sequence of FR1–FR96 and NFR1–NFR70, with all letter-suffixed requirements accounted for.
+
+Its principal readiness risk is not missing requirement volume but unresolved calibration and breadth. A4, A9/A9a, A10, and A11 leave operating baselines, dataset representativeness, recovery targets, and several production SLO/error-budget values provisional. A1–A8 also remain explicit decision checkpoints. These assumptions are clearly owned and bounded, but implementation readiness depends on epics and stories preserving their validation/revisit work rather than treating starter values as permanently resolved.
+
+The catalog's size—111 FRs and 77 NFRs—creates decomposition and traceability risk. Several requirements carry mandatory sub-story or scenario matrices, and the safety floor spans every increment. The next validation step must therefore prove explicit epic/story coverage of each requirement and binding addendum invariant, not merely broad thematic alignment.
+
+## Epic Coverage Validation
+
+The epics document contains an explicit FR Coverage Map. The requirement identifiers below link to the complete PRD text in the preceding section; the requirement column is a compact traceability label rather than a replacement for that authoritative text.
+
+### Coverage Matrix
+
+| FR | PRD requirement | Epic coverage | Status |
+| --- | --- | --- | --- |
+| FR1 | Capture authorized mailbox events | Epic 2 | ✓ Covered |
+| FR2 | Preserve email/thread/mailbox/party/time/attachment identity | Epic 2 | ✓ Covered |
+| FR3 | Deterministic project association | Epic 2 | ✓ Covered |
+| FR4 | Ambiguous association to review | Epic 2 | ✓ Covered |
+| FR5 | Candidate evidence, confidence, reasons, consequences | Epic 2 | ✓ Covered |
+| FR6 | Choose/reject/defer/review/note decisions | Epic 2 | ✓ Covered |
+| FR7 | Correct association | Epic 2 | ✓ Covered |
+| FR8 | Record decisions/corrections/retries/skips | Epic 2 | ✓ Covered |
+| FR9 | Configure rules and security-sensitive thresholds | Epic 2; Epic 7 editor extension | ✓ Covered |
+| FR10 | Preserve context in non-associated states | Epic 2 | ✓ Covered |
+| FR11 | Machine-readable reasons and confidence inputs | Epic 2 | ✓ Covered |
+| FR12 | Side-by-side evidence comparison | Epic 2 | ✓ Covered |
+| FR13 | Resolve participants to tenant-scoped parties | Epic 2 | ✓ Covered |
+| FR14 | Review unresolved participants | Epic 2 | ✓ Covered |
+| FR15 | External participation by email without portal | Epic 2 | ✓ Covered |
+| FR16 | Boundary authorization before exposure/mutation | Epic 1 | ✓ Covered |
+| FR17 | Block unresolved/unauthorized actors | Epic 2 | ✓ Covered |
+| FR18 | Governed mailbox participation rules | Epic 7 | ✓ Covered |
+| FR19 | Service-client access configuration | Epic 5 | ✓ Covered |
+| FR20 | Consent/lawful-basis metadata | Epic 9 | ✓ Covered |
+| FR21 | Email-derived project conversation | Epic 3 | ✓ Covered |
+| FR22 | Seven project-context concern types | Epic 3 | ✓ Covered |
+| FR23 | Why-this-project evidence/provenance | Epic 3 | ✓ Covered |
+| FR24 | Conversation item statuses and next action | Epic 3 | ✓ Covered |
+| FR25 | Tenant/project conversation separation | Epic 3 | ✓ Covered |
+| FR26 | Informational/actionable distinction | Epic 3 | ✓ Covered |
+| FR27 | AI summary/source-evidence distinction | Epic 3 | ✓ Covered |
+| FR28 | Visible human-review history | Epic 3 | ✓ Covered |
+| FR29 | Capture attachments | Epic 3 | ✓ Covered |
+| FR30 | Governed folder storage | Epic 3 | ✓ Covered |
+| FR31 | Attachment capture/storage status | Epic 3 | ✓ Covered |
+| FR32 | Attachment access control | Epic 3 | ✓ Covered |
+| FR33 | Authorized scoped AI context | Epic 3 | ✓ Covered |
+| FR34 | Attachment lifecycle states | Epic 3 | ✓ Covered |
+| FR35 | Task/action intent detection and contract | Epic 4 | ✓ Covered |
+| FR36 | Review captured task intent | Epic 4 | ✓ Covered |
+| FR37 | Convert intent to governed action | Epic 4 | ✓ Covered |
+| FR38 | Terminal task-intent dispositions | Epic 4 | ✓ Covered |
+| FR39 | AI risk classification | Epic 4 | ✓ Covered |
+| FR40 | Policy-authorized low-risk AI | Epic 4 | ✓ Covered |
+| FR41 | Approval for six risky classes | Epic 4 | ✓ Covered |
+| FR42 | Full AI-action approval review | Epic 4 | ✓ Covered |
+| FR43 | Allowlisted command execution | Epic 4 | ✓ Covered |
+| FR44 | Inspect AI-action lifecycle | Epic 4 | ✓ Covered |
+| FR45 | Preview boundary-crossing actions | Epic 4 | ✓ Covered |
+| FR46 | Refuse/block unsafe requests | Epic 4 | ✓ Covered |
+| FR47 | Authorized outbound drafts | Epic 6 | ✓ Covered |
+| FR48 | Five sender-authority classes | Epic 6 | ✓ Covered |
+| FR48a | Provider authenticity verdict passthrough | Epic 6 | ✓ Covered |
+| FR48b | Security-relevant header inspection | Epic 6 | ✓ Covered |
+| FR48c | On-behalf-of disambiguation | Epic 6 | ✓ Covered |
+| FR48d | External-sender posture and strictness | Epic 6 | ✓ Covered |
+| FR49 | Approval before outbound send | Epic 6 | ✓ Covered |
+| FR50 | Preserve outbound approval-record fields | Epic 6 | ✓ Covered |
+| FR51 | Mailbox integration and monitored patterns | Epic 7 | ✓ Covered |
+| FR52 | AI-action policy configuration | Epic 7 | ✓ Covered |
+| FR53 | Mailbox permission/degraded-state review | Epic 7 | ✓ Covered |
+| FR54 | Compliance/support investigation | Epic 9 | ✓ Covered |
+| FR55 | Security-sensitive audit records | Epic 1; Epic 9 WORM extension | ✓ Covered |
+| FR55a | Derived-store isolation by construction | Epic 9 | ✓ Covered |
+| FR56 | Multi-dimensional audit queries | Epic 9 | ✓ Covered |
+| FR57 | Hide unauthorized information | Epic 1; inherited across surfaces | ✓ Covered |
+| FR58 | Retention/export/deletion support | Epic 9 | ✓ Covered |
+| FR59 | End-to-end correlation | Epic 1 | ✓ Covered |
+| FR60 | Retained/redacted source evidence | Epic 2 | ✓ Covered |
+| FR61 | Versioned policy snapshots | Epic 1 | ✓ Covered |
+| FR62 | Human rationale/notes | Epic 2 | ✓ Covered |
+| FR63 | Supersede reversible decisions | Epic 2 | ✓ Covered |
+| FR64 | Duplicate delivery suppression | Epic 2 | ✓ Covered |
+| FR65 | Valid retries | Epic 2 | ✓ Covered |
+| FR66 | Terminal/non-terminal failure states | Epic 2 | ✓ Covered |
+| FR67 | Health, queues, dashboards | Epic 8; M0 minimum in Epics 1–2 | ✓ Covered |
+| FR68 | Fail closed on unresolved context/dependency | Epic 1 | ✓ Covered |
+| FR69 | Operational queue management | Epic 7 | ✓ Covered |
+| FR70 | Assign/claim review items | Epic 7 | ✓ Covered |
+| FR71 | Next required human action | Epic 2 | ✓ Covered |
+| FR72 | Attention notifications | Epic 7 | ✓ Covered |
+| FR73 | Notification routing/escalation | Epic 7 | ✓ Covered |
+| FR74 | Disable/quarantine/rate-limit controls | Epic 7; Epic 8 runtime activation | ✓ Covered |
+| FR75 | Tenant limits/quotas/circuit breakers | Epic 7; Epic 8 runtime activation | ✓ Covered |
+| FR75a | Admin role hierarchy and assignment | Epic 7 | ✓ Covered |
+| FR75b | See-only admin scopes | Epic 7 | ✓ Covered |
+| FR75c | Queue-level operate scopes | Epic 7 | ✓ Covered |
+| FR75d | Two-person policy administration | Epic 7 | ✓ Covered |
+| FR75e | Mailbox-admin boundary | Epic 7 | ✓ Covered |
+| FR75f | Compliance-admin boundary | Epic 7 | ✓ Covered |
+| FR75g | Audit every admin action | Epic 7 | ✓ Covered |
+| FR76 | Review affordances and safe disabled reasons | Epic 2 | ✓ Covered |
+| FR77 | Versioned safe-message catalog | Epic 1 | ✓ Covered |
+| FR78 | Queue filtering/sorting/prioritization | Epic 7 | ✓ Covered |
+| FR79 | Stale/waiting/blocked/escalation states | Epic 2 | ✓ Covered |
+| FR80 | Long-running operation status | Epic 1; Epic 5 machine-surface extension | ✓ Covered |
+| FR81 | UI governed workflow operations | Epic 1 | ✓ Covered |
+| FR81a | One shared command pipeline | Epic 1; preserved by Epics 5, 10, 11 | ✓ Covered |
+| FR82 | CLI parity | Epic 5 | ✓ Covered |
+| FR83 | MCP parity | Epic 5 | ✓ Covered |
+| FR84 | Equivalent cross-surface outcomes | Epic 5 | ✓ Covered |
+| FR85 | Immutable surface-origin attribution | Epic 1; Epic 5 extension | ✓ Covered |
+| FR86 | Command-record conformance tests | Epic 1; Epic 5 full harness | ✓ Covered |
+| FR87 | Canonical lifecycle states | Epic 1; Epic 7 completion | ✓ Covered |
+| FR88 | Explicit transition validation | Epic 1 | ✓ Covered |
+| FR89 | Reject and audit invalid transitions | Epic 1 | ✓ Covered |
+| FR90 | Idempotency keys/stable resource IDs | Epic 1; Epic 9 full contract | ✓ Covered |
+| FR91 | Source/projection separation and rebuild | Epic 2 | ✓ Covered |
+| FR91a | Correction propagation | Epic 2 | ✓ Covered |
+| FR92 | Evaluation datasets | Epic 1; Epic 9 extension | ✓ Covered |
+| FR93 | Tenant-scoped fixtures/sandbox | Epic 1 | ✓ Covered |
+| FR94 | Operational metrics | Epic 8 | ✓ Covered |
+| FR95 | Safe replay/simulation | Epic 9 | ✓ Covered |
+| FR95a | Replay isolation | Epic 9 | ✓ Covered |
+| FR96 | Corrections as future evidence | Epic 2 | ✓ Covered |
+
+### Missing Requirements
+
+No PRD functional-requirement identifier is missing from the epic coverage map. No epic-only FR identifier was found that is absent from the PRD.
+
+The coverage claim includes cross-epic activation or extension for FR55, FR57, FR67, FR74, FR75, FR80, FR81a, FR85–FR87, FR90, FR92, and the Epic 10/11 preservation work. Identifier coverage is therefore complete, while implementation sequencing and acceptance quality remain subjects for later readiness steps.
+
+### Coverage Statistics
+
+- Total PRD FRs: 111
+- FRs covered in epics: 111
+- Missing FRs: 0
+- Extra epic-only FRs: 0
+- Coverage: 100.0%
+
+## UX Alignment Assessment
+
+### UX Document Status
+
+**Found.** The selected UX package contains two final core specifications and two approved elaborations:
+
+- `DESIGN.md` defines the Fluent UI v5 / FrontComposer visual inheritance, semantic status treatment, product components, and responsive layout posture.
+- `EXPERIENCE.md` defines the information architecture, component behavior, state matrix, interaction model, WCAG 2.2 AA floor, English/French localization, responsive fallbacks, and nine end-to-end journeys.
+- `m1-m2-surface-elaboration.md` closes assignment-readiness detail for PRD surfaces S4 and S6-S10.
+- `epic10-chat-surface-elaboration.md` closes the Project Workspace, governed composer, progressive-response, and Stop/Cancel interaction detail introduced by Epic 10.
+
+The deliberate absence of mockups or wireframes is not treated as a missing artifact: `EXPERIENCE.md` explicitly declares the UX handoff spine-only and names the IA, components, states, interactions, accessibility rules, and journeys as the binding implementation inputs.
+
+### UX ↔ PRD Alignment
+
+The UX package is substantively aligned with the PRD and addendum:
+
+- All PRD user-facing surface contracts S1-S10 have a UX home. S1-S3 are represented by Project Workspace / Conversation Detail, Association Review, and AI Action Review; S4 and S6-S10 are explicitly mapped in the M1/M2 elaboration; S5 is Tenant Configuration; the Epic 10 elaboration supplies the governed write/composer behavior for the interactive workspace.
+- The nine UX journeys cover the principal PRD actors and use cases: project contributor, external party, project owner, tenant admin, developer/automation user, compliance/support reviewer, and governed AI actor.
+- The UX state model preserves the PRD's safety invariants: ambiguous association requires an explicit decision, risky AI work becomes a proposal, unauthorized resources are redacted, retry and partial-success states remain visible, correction preserves audit history, and UI/CLI/MCP do not imply an authorization bypass.
+- Addendum contracts are carried into the UX elaborations, including sender-authority display, tenant-policy/two-person-rule behavior, stable operation status and idempotent retry messaging, replay isolation, risk-reason display, evidence freshness, and operational SLO language.
+- Accessibility and localization match PRD NFR60-NFR64: WCAG 2.2 AA, keyboard and screen-reader operation, non-color status, reduced motion, responsive fallbacks, and English/French parity.
+
+No UX requirement was found that contradicts the PRD or introduces an unapproved product capability.
+
+### UX ↔ Architecture Alignment
+
+The architecture supports the documented UX contracts:
+
+- Blazor, FrontComposer, Fluent UI v5, Fluxor, typed REST queries/commands, and SignalR projection nudges provide the declared UI foundation and update model.
+- The ChatBot-owned read projections, explicit `stale|rebuilding|unavailable` states, operation identities, and metadata-only SignalR nudge/re-query pattern support the UX loading, partial-success, retry, degraded-dependency, and progressive-response states while preserving the "never trust payload" boundary.
+- `CommandGateway` admission and Client-only surface adapters support the UX rule that the composer is governed, risky requests become proposals, and UI/CLI/MCP share the same authorization, approval, idempotency, audit, and lifecycle behavior.
+- FrontComposer shell adoption plus the Epic 12 Fluent-only and Epic 13 layout-composition rules support the visual-inheritance contract and mechanically prohibit a parallel raw-HTML/custom-token design system.
+- Projection-backed UI reads and the PRD's p95 2-second target are architecturally compatible; tenant-scoped SignalR nudges trigger typed re-queries rather than rendering untrusted event payloads.
+- The architecture carries the WCAG 2.2 AA and English/French requirements, identifies Playwright/axe-core coverage that grows by increment, and preserves responsive FrontComposer composition.
+
+No unsupported UX component, interaction, or surface was identified, and no architecture change is required before implementation on UX grounds.
+
+### Alignment Issues
+
+1. **Non-blocking source-of-truth drift — later UI conformance decisions are architecture-only.** The core UX files were last updated on 2026-06-05, while the architecture incorporates binding Epic 12 (2026-06-19) and Epic 13 (2026-06-22) rules: no raw form controls, no legacy/custom primitive tokens, mandatory `FcPageLayout` / `FcPageHeader` composition, required Fluent layout/data components, and separate conformance guards. These rules are consistent with the UX inheritance principle, but they are not stated in the UX package itself. An implementer reading only the declared visual chain can miss the precise mechanical acceptance boundary. Backport these constraints into `DESIGN.md` or add a dated UX implementation addendum that explicitly points to the architecture rules and their conformance tests.
+
+2. **Non-blocking traceability defect — stale architecture requirement counts and one incorrect NFR citation.** The architecture labels the requirements as "96 FRs" and "70 NFRs" although the authoritative PRD catalog contains 111 functional and 77 non-functional identifiers after lettered extensions. Its Epic 12 frontend section also attributes accessibility affordances to `NFR6`, while the same architecture correctly maps accessibility to NFR60-NFR64 elsewhere. The underlying design covers the extensions, so this is not a capability gap, but the counts and citation should be corrected to prevent misleading UX acceptance traceability.
+
+### Warnings
+
+- **No blocking UX warning.** The UX artifacts are sufficient for implementation readiness when consumed together with the architecture and the two selected UX elaborations.
+- The UX folder has no standard `index.md`; maintain an explicit manifest or index so later elaborations and binding conformance decisions cannot be omitted during handoff.
diff --git a/references/Hexalith.Builds b/references/Hexalith.Builds
--- a/references/Hexalith.Builds
+++ b/references/Hexalith.Builds
@@ -1 +1 @@
-Subproject commit 13bd3993a1b42ca83b06aaae0492e838ae3385aa
+Subproject commit 13bd3993a1b42ca83b06aaae0492e838ae3385aa-dirty
diff --git a/references/Hexalith.Commons b/references/Hexalith.Commons
--- a/references/Hexalith.Commons
+++ b/references/Hexalith.Commons
@@ -1 +1 @@
-Subproject commit 48feced64053171b6dd1ab5c862976323c0f25e8
+Subproject commit 48feced64053171b6dd1ab5c862976323c0f25e8-dirty
diff --git a/references/Hexalith.Conversations b/references/Hexalith.Conversations
--- a/references/Hexalith.Conversations
+++ b/references/Hexalith.Conversations
@@ -1 +1 @@
-Subproject commit f52c1a5fb93f5765bb0c75c81421f76575a5ddf0
+Subproject commit f52c1a5fb93f5765bb0c75c81421f76575a5ddf0-dirty
diff --git a/references/Hexalith.EventStore b/references/Hexalith.EventStore
index 1b3000f..11ba1e7 160000
--- a/references/Hexalith.EventStore
+++ b/references/Hexalith.EventStore
@@ -1 +1 @@
-Subproject commit 1b3000f91bc7fd1a9bf2c1b3b7f552de7cd2d931
+Subproject commit 11ba1e73269c52d65fecdbe39462459eeec788b4-dirty
diff --git a/references/Hexalith.Folders b/references/Hexalith.Folders
--- a/references/Hexalith.Folders
+++ b/references/Hexalith.Folders
@@ -1 +1 @@
-Subproject commit 18f52adf6208cbb87894cd33447c9ec3b088be36
+Subproject commit 18f52adf6208cbb87894cd33447c9ec3b088be36-dirty
diff --git a/references/Hexalith.FrontComposer b/references/Hexalith.FrontComposer
--- a/references/Hexalith.FrontComposer
+++ b/references/Hexalith.FrontComposer
@@ -1 +1 @@
-Subproject commit 6861ca1bb3284f5cb5873daebdf2a7f3febed609
+Subproject commit 6861ca1bb3284f5cb5873daebdf2a7f3febed609-dirty
diff --git a/references/Hexalith.Memories b/references/Hexalith.Memories
--- a/references/Hexalith.Memories
+++ b/references/Hexalith.Memories
@@ -1 +1 @@
-Subproject commit 9f3c72003069a2f6eed5351c5dc3f18947484959
+Subproject commit 9f3c72003069a2f6eed5351c5dc3f18947484959-dirty
diff --git a/references/Hexalith.Parties b/references/Hexalith.Parties
--- a/references/Hexalith.Parties
+++ b/references/Hexalith.Parties
@@ -1 +1 @@
-Subproject commit 87903f2cdeabc4ced40659d8b1ca92dc5130c35e
+Subproject commit 87903f2cdeabc4ced40659d8b1ca92dc5130c35e-dirty
diff --git a/references/Hexalith.PolymorphicSerializations b/references/Hexalith.PolymorphicSerializations
--- a/references/Hexalith.PolymorphicSerializations
+++ b/references/Hexalith.PolymorphicSerializations
@@ -1 +1 @@
-Subproject commit 89c8409785aad2b8bcfbbae079b52adf0ad14441
+Subproject commit 89c8409785aad2b8bcfbbae079b52adf0ad14441-dirty
diff --git a/references/Hexalith.Timesheets b/references/Hexalith.Timesheets
--- a/references/Hexalith.Timesheets
+++ b/references/Hexalith.Timesheets
@@ -1 +1 @@
-Subproject commit 5e02d55688cd182f9c8a57bfb8acb98a82b32652
+Subproject commit 5e02d55688cd182f9c8a57bfb8acb98a82b32652-dirty

===== ROOT UNTRACKED SPEC DIFF =====
diff --git a/_bmad-output/implementation-artifacts/spec-sync-repository-agent-instructions.md b/_bmad-output/implementation-artifacts/spec-sync-repository-agent-instructions.md
new file mode 100644
index 0000000..3a74aef
--- /dev/null
+++ b/_bmad-output/implementation-artifacts/spec-sync-repository-agent-instructions.md
@@ -0,0 +1,73 @@
+---
+title: 'Synchronize repository agent instructions'
+type: 'chore'
+created: '2026-07-17'
+status: 'in-review'
+review_loop_iteration: 0
+baseline_commit: '1b529f42594b03ba73f9d870e667ad76a8020e29'
+context:
+  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
+  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
+---
+
+<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">
+
+## Intent
+
+**Problem:** Root agent entry points have drifted across the umbrella repository and its root-declared submodules, so Codex, Claude, and GitHub Copilot can receive different instructions in the same repository.
+
+**Approach:** Treat each repository's root `CLAUDE.md` as its canonical source and make `AGENTS.md` plus `.github/copilot-instructions.md` byte-for-byte identical to it. Apply this only to the root repository and the 13 `references/...` submodules declared by the root `.gitmodules`.
+
+## Boundaries & Constraints
+
+**Always:** Preserve each repository's `CLAUDE.md`; create a missing Codex or Copilot entry point when needed; preserve all unrelated pre-existing changes; perform changes within the repository that owns each file; verify exact byte equality rather than normalized textual similarity.
+
+**Ask First:** Halt if a declared repository lacks a readable `CLAUDE.md`, if an instruction target contains newly observed user changes, or if satisfying equality would require changing the canonical `CLAUDE.md`.
+
+**Never:** Initialize, inspect, or modify nested submodules; use recursive submodule commands; edit instruction files outside the root repository and its root-declared submodules; commit, push, or overwrite unrelated dirty work.
+
+## I/O & Edge-Case Matrix
+
+| Scenario | Input / State | Expected Output / Behavior | Error Handling |
+|----------|--------------|---------------------------|----------------|
+| Drifted entry point | `AGENTS.md` or Copilot instructions differ from `CLAUDE.md` | Replace the target with the exact canonical bytes | Verify with `cmp` |
+| Missing entry point | Codex or Copilot instruction file is absent | Create it with the exact canonical bytes | Fail if its parent cannot be created safely |
+| Already synchronized | Both targets match `CLAUDE.md` | Leave the repository unchanged | Report it as compliant |
+| Unrelated dirty work | Repository contains changes outside instruction targets | Preserve those changes byte-for-byte | Stop if an intended target becomes dirty externally |
+
+</frozen-after-approval>
+
+## Code Map
+
+- `CLAUDE.md` and `references/*/CLAUDE.md` -- canonical per-repository instruction sources; read-only for this change.
+- `AGENTS.md` and `references/*/AGENTS.md` -- Codex entry points that must mirror the canonical file.
+- `.github/copilot-instructions.md` and `references/*/.github/copilot-instructions.md` -- GitHub Copilot entry points that must mirror the canonical file.
+- `.gitmodules` -- authoritative boundary containing the 13 eligible submodule paths.
+
+## Tasks & Acceptance
+
+**Execution:**
+- [x] `references/{Hexalith.EventStore,Hexalith.FrontComposer,Hexalith.Folders,Hexalith.Conversations,Hexalith.Parties,Hexalith.Memories}/.github/copilot-instructions.md` -- replace drifted Copilot content with the owning repository's `CLAUDE.md` bytes.
+- [x] `references/{Hexalith.Commons,Hexalith.Builds,Hexalith.Timesheets}/AGENTS.md` -- replace drifted Codex content with the owning repository's `CLAUDE.md` bytes.
+- [x] `references/{Hexalith.Commons,Hexalith.Builds,Hexalith.Timesheets,Hexalith.PolymorphicSerializations}/.github/copilot-instructions.md` -- create or replace Copilot content from the owning repository's `CLAUDE.md` bytes.
+- [x] Root plus all root-declared submodules -- verify both target entry points exist and compare byte-for-byte equal to `CLAUDE.md`; confirm unrelated dirty paths were not overwritten by this change.
+
+**Acceptance Criteria:**
+- Given the root repository and every path declared by root `.gitmodules`, when instruction parity is checked, then `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md` exist and have identical bytes within each repository.
+- Given repositories that were already compliant, when the change is complete, then they have no instruction-file diff.
+- Given the approved pre-existing changes in the root, `Hexalith.Builds`, and `Hexalith.Timesheets`, when diffs are reviewed, then the instruction synchronization does not overwrite those paths and any concurrent user edits remain intact.
+- Given nested submodules under any root-declared submodule, when the change is complete, then none were initialized or modified.
+
+## Spec Change Log
+
+## Design Notes
+
+- `Hexalith.Commons` and `Hexalith.PolymorphicSerializations` contained dangling Copilot symlinks. They were replaced with regular files so the entry points are readable and byte-identical to each repository's `CLAUDE.md`.
+- Canonical CRLF line endings were retained where present; whitespace verification therefore uses Git's `cr-at-eol` handling.
+
+## Verification
+
+**Commands:**
+- Enumerate `.gitmodules` paths and run `test -f` plus `cmp -s` for each repository's three entry points -- expected: all 14 repositories pass.
+- Run `git -c core.whitespace=cr-at-eol diff --check` against instruction paths in every changed repository -- expected: no whitespace errors after treating canonical CRLF endings as line terminators.
+- Review `git status --short` and instruction-only diffs in every changed repository -- expected: only intended instruction paths are new/modified in addition to the approved pre-existing work.

===== SUBMODULE TRACKED DIFF: references/Hexalith.EventStore =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
index 5366a5a7..7c120a52 100644
--- a/.github/copilot-instructions.md
+++ b/.github/copilot-instructions.md
@@ -1,32 +1,22 @@
 # AI assistant instructions

 Before working in this repository, read
-[`hexalith-llm-instructions.md`](../references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
+[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
 (in the `references/Hexalith.AI.Tools` submodule) and follow it.

-## Commit Messages
-
-When generating a commit message, follow the repository's
-`@commitlint/config-conventional` contract directly:
-
-- Format the header as `<type>[optional scope][!]: <description>`.
-- Start the description with a lowercase letter and omit a trailing period.
-  Use imperative mood as a repository authoring convention.
-- Keep the entire header at 100 characters or fewer; prefer a concise header
-  near 50 characters.
-- Choose the type by release impact: `feat` for a minor release, `fix` or
-  `perf` for a patch release, and `docs`, `test`, `refactor`, `build`, `ci`,
-  `chore`, `revert`, or `style` for changes that do not release product
-  behavior.
-- Use `!` or a `BREAKING CHANGE:` footer for a major release.
-
-Commitlint mechanically enforces the header format, allowed types, description
-case, trailing punctuation, and length. Imperative mood and choosing the type
-that accurately reflects release impact remain author and reviewer
-responsibilities.
-
 ## Git Submodules

 - Initialize root-declared submodules only, using the `references/...` paths declared in the root `.gitmodules` file.
 - Avoid recursive submodule commands unless they are explicitly scoped so that nested submodules are not initialized.
 - If nested submodules are initialized accidentally, deinitialize them before continuing.
+
+## Release Package Inventory
+
+Release packaging is manifest-driven by [`tools/release-packages.json`](./tools/release-packages.json).
+The manifest currently contains 14 packages: `Hexalith.EventStore.Contracts`,
+`Hexalith.EventStore.Client`, `Hexalith.EventStore.Server`, `Hexalith.EventStore.SignalR`,
+`Hexalith.EventStore.Testing`, `Hexalith.EventStore.Testing.Integration`,
+`Hexalith.EventStore.Aspire`, `Hexalith.EventStore.ServiceDefaults`,
+`Hexalith.EventStore.DomainService`, `Hexalith.EventStore.RestApi.Generators`,
+`Hexalith.EventStore.Gateway`, `Hexalith.EventStore.Admin.Abstractions`,
+`Hexalith.EventStore.Admin.Cli`, and `Hexalith.EventStore.Admin.Server`.

===== SUBMODULE TRACKED DIFF: references/Hexalith.FrontComposer =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
index f7abf210..074ba059 100644
--- a/.github/copilot-instructions.md
+++ b/.github/copilot-instructions.md
@@ -1,14 +1,11 @@
 # AI assistant instructions

 Before working in this repository, read
-[`hexalith-llm-instructions.md`](../references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
+[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
 (in the `references/Hexalith.AI.Tools` submodule) and follow it.

 ## Git Submodules

-IMPORTANT! Only initialize and update submodules declared in the root repository `.gitmodules` file.
-
 - Initialize root-declared submodules only, using the `references/...` paths declared in the root `.gitmodules` file.
-- Do not initialize, update, or recurse into nested submodules inside those root-declared submodules.
 - Avoid recursive submodule commands unless they are explicitly scoped so that nested submodules are not initialized.
 - If nested submodules are initialized accidentally, deinitialize them before continuing.

===== SUBMODULE TRACKED DIFF: references/Hexalith.Folders =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
index 8436e09..a3a68a6 100644
--- a/.github/copilot-instructions.md
+++ b/.github/copilot-instructions.md
@@ -1,11 +1,13 @@
 # AI assistant instructions

 Before working in this repository, read
-[`hexalith-llm-instructions.md`](../references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
+[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
 (in the `references/Hexalith.AI.Tools` submodule) and follow it.

 ## Git Submodules

+- Initialize the root-declared submodules with:
+  `git submodule update --init references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.Commons references/Hexalith.EventStore references/Hexalith.FrontComposer references/Hexalith.Memories references/Hexalith.PolymorphicSerializations references/Hexalith.Tenants`.
 - Initialize root-declared submodules only, using the `references/...` paths declared in the root `.gitmodules` file.
-- Avoid recursive submodule commands unless they are explicitly scoped so that nested submodules are not initialized.
+- Do not run recursive submodule initialization unless it is explicitly scoped so that nested submodules are not initialized.
 - If nested submodules are initialized accidentally, deinitialize them before continuing.

===== SUBMODULE TRACKED DIFF: references/Hexalith.Conversations =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
index 8436e09..074ba05 100644
--- a/.github/copilot-instructions.md
+++ b/.github/copilot-instructions.md
@@ -1,7 +1,7 @@
 # AI assistant instructions

 Before working in this repository, read
-[`hexalith-llm-instructions.md`](../references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
+[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
 (in the `references/Hexalith.AI.Tools` submodule) and follow it.

 ## Git Submodules

===== SUBMODULE TRACKED DIFF: references/Hexalith.Parties =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
index 69d4039..175ee9f 100644
--- a/.github/copilot-instructions.md
+++ b/.github/copilot-instructions.md
@@ -1,7 +1,7 @@
 # AI assistant instructions

 Before working in this repository, read
-[`hexalith-llm-instructions.md`](../references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
+[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
 (in the `references/Hexalith.AI.Tools` submodule) and follow it.

 ## Git Submodules

===== SUBMODULE TRACKED DIFF: references/Hexalith.Memories =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
index cf530098..074ba059 100644
--- a/.github/copilot-instructions.md
+++ b/.github/copilot-instructions.md
@@ -1,7 +1,7 @@
 # AI assistant instructions

 Before working in this repository, read
-[`hexalith-llm-instructions.md`](../references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
+[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
 (in the `references/Hexalith.AI.Tools` submodule) and follow it.

 ## Git Submodules
@@ -9,4 +9,3 @@ Before working in this repository, read
 - Initialize root-declared submodules only, using the `references/...` paths declared in the root `.gitmodules` file.
 - Avoid recursive submodule commands unless they are explicitly scoped so that nested submodules are not initialized.
 - If nested submodules are initialized accidentally, deinitialize them before continuing.
-

===== SUBMODULE TRACKED DIFF: references/Hexalith.Commons =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
deleted file mode 120000
index 8436e09..0000000
--- a/.github/copilot-instructions.md
+++ /dev/null
@@ -1,11 +0,0 @@
-# AI assistant instructions
-
-Before working in this repository, read
-[`hexalith-llm-instructions.md`](../references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
-(in the `references/Hexalith.AI.Tools` submodule) and follow it.
-
-## Git Submodules
-
-- Initialize root-declared submodules only, using the `references/...` paths declared in the root `.gitmodules` file.
-- Avoid recursive submodule commands unless they are explicitly scoped so that nested submodules are not initialized.
-- If nested submodules are initialized accidentally, deinitialize them before continuing.
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
new file mode 100644
index 0000000..b7c27e9
--- /dev/null
+++ b/.github/copilot-instructions.md
@@ -0,0 +1,11 @@
+# AI assistant instructions
+
+Before working in this repository, read
+[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
+(in the `references/Hexalith.AI.Tools` submodule) and follow it.
+
+## Git Submodules
+
+- Initialize root-declared submodules only, using the `references/...` paths declared in the root `.gitmodules` file.
+- Avoid recursive submodule commands unless they are explicitly scoped so that nested submodules are not initialized.
+- If nested submodules are initialized accidentally, deinitialize them before continuing.
diff --git a/AGENTS.md b/AGENTS.md
index 074ba05..b7c27e9 100644
--- a/AGENTS.md
+++ b/AGENTS.md
@@ -1,11 +1,11 @@
-# AI assistant instructions
-
-Before working in this repository, read
-[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
-(in the `references/Hexalith.AI.Tools` submodule) and follow it.
-
-## Git Submodules
-
-- Initialize root-declared submodules only, using the `references/...` paths declared in the root `.gitmodules` file.
-- Avoid recursive submodule commands unless they are explicitly scoped so that nested submodules are not initialized.
-- If nested submodules are initialized accidentally, deinitialize them before continuing.
+# AI assistant instructions
+
+Before working in this repository, read
+[`hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
+(in the `references/Hexalith.AI.Tools` submodule) and follow it.
+
+## Git Submodules
+
+- Initialize root-declared submodules only, using the `references/...` paths declared in the root `.gitmodules` file.
+- Avoid recursive submodule commands unless they are explicitly scoped so that nested submodules are not initialized.
+- If nested submodules are initialized accidentally, deinitialize them before continuing.

===== SUBMODULE TRACKED DIFF: references/Hexalith.Builds =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
index d948a86..2066fba 100644
--- a/.github/copilot-instructions.md
+++ b/.github/copilot-instructions.md
@@ -1,7 +1,280 @@
-Use primary constructors for classes and records when possible.
-Do not duplicate properties defined in the class or record primary constructor.
-Use recommended XML tags for commenting public, protected and internal objects in C# code.
-Use latest C# and Blazor language version.
-Comments of record the record properties defined in the primary constructor must be defined in the record comment with param tags.
-For unit tests use XUnit and Shouldly
-For GitHub workflow or action references to Hexalith.Builds actions or reusable workflows, always use the latest main branch reference: Hexalith/Hexalith.Builds/<action-path>@main. Do not pin Hexalith.Builds actions or reusable workflows to release tags or commit SHAs. This is an intentional exception to the third-party action SHA-pinning rule.
+# CLAUDE.md - AI Assistant Instructions for Hexalith Projects
+
+This file provides guidance for AI assistants (Claude, Copilot, Cursor, etc.) working with Hexalith .NET applications built using Domain-Driven Design (DDD) architecture.
+
+## AI assistant instructions
+
+When adding or updating GitHub workflow or action references to Hexalith.Builds
+actions or reusable workflows, always use the latest main branch reference:
+`Hexalith/Hexalith.Builds/<action-path>@main`. Do not pin Hexalith.Builds
+actions or reusable workflows to release tags or commit SHAs. This is an
+intentional exception to the third-party action SHA-pinning rule.
+
+## Technology Stack
+
+- **.NET 10+** - Latest .NET framework
+- **C# 14+** - Latest C# language features
+- **DAPR 1.18+** - Distributed Application Runtime for microservices
+- **Microsoft Fluent UI Blazor** - UI component library for Blazor applications
+- **XUnit + Shouldly** - Unit testing framework and assertion library
+
+## Hexalith Ecosystem
+
+The Hexalith ecosystem consists of multiple interconnected repositories:
+
+| Repository | Description |
+|------------|-------------|
+| [Hexalith](https://github.com/Hexalith/Hexalith) | Core framework and shared components |
+| [Hexalith.Domains](https://github.com/Hexalith/Hexalith.Domains) | Domain models and business logic |
+| [Hexalith.PolymorphicSerializations](https://github.com/Hexalith/Hexalith.PolymorphicSerializations) | Polymorphic JSON serialization support |
+| [Hexalith.IdentityStores](https://github.com/Hexalith/Hexalith.IdentityStores) | Identity and authentication stores |
+| [Hexalith.Builds](https://github.com/Hexalith/Hexalith.Builds) | Build configurations and CI/CD templates |
+| [HexalithApp](https://github.com/Hexalith/HexalithApp) | Main application templates |
+| [Hexalith.NetAspire](https://github.com/Hexalith/Hexalith.NetAspire) | .NET Aspire integration |
+| [Hexalith.Security](https://github.com/Hexalith/Hexalith.Security) | Security and authorization components |
+
+## Commit Message Guidelines
+
+**All commit messages MUST follow the [Angular Conventional Commits](https://github.com/angular/angular/blob/main/contributing-docs/commit-message-guidelines.md) specification** for semantic-release automated version management and package publishing.
+
+### Commit Message Format
+
+```text
+<type>(<scope>): <short description>
+
+<optional body>
+
+<optional footer>
+```
+
+### Types
+
+| Type | Description | Version Bump |
+|------|-------------|--------------|
+| `feat` | New feature | Minor |
+| `fix` | Bug fix | Patch |
+| `docs` | Documentation only | None |
+| `style` | Code style (formatting, whitespace) | None |
+| `refactor` | Code refactoring (no feature/fix) | None |
+| `perf` | Performance improvements | Patch |
+| `test` | Adding or modifying tests | None |
+| `build` | Build system or dependencies | None |
+| `ci` | CI/CD configuration | None |
+| `chore` | Miscellaneous maintenance | None |
+
+### Rules
+
+1. Use imperative mood in short description (e.g., "add" not "added")
+2. Start description with lowercase (unless proper noun)
+3. Omit the period at end of short description
+4. Keep short description under 50 characters
+5. Wrap body at 72 characters
+6. Use `BREAKING CHANGE:` in footer for breaking changes (triggers major version)
+
+### Examples
+
+```text
+feat(auth): add user authentication endpoint
+
+Implement JWT-based authentication with refresh token support.
+Includes validation middleware and token generation service.
+
+Closes #123
+```
+
+```text
+fix(orders): correct tax calculation for international orders
+
+BREAKING CHANGE: tax calculation now requires country code parameter
+```
+
+```text
+refactor(domain): simplify aggregate root base class
+```
+
+## Domain-Driven Design Architecture
+
+### Project Structure
+
+Hexalith modules follow a **vertical slice architecture** with separate NuGet packages per layer. Each module (e.g., `Hexalith.Documents`) is organized as follows:
+
+```text
+
+{ModuleName}/
+├── AspireHost/                         # .NET Aspire orchestration host
+├── HexalithApp/                        # Application templates (submodule)
+├── references/Hexalith.Builds/         # Build configuration (submodule)
+├── src/
+│   ├── examples/
+│   │   └── Hexalith.{Module}.Example/                       # Example implementation
+│   ├── libraries/                                           # NuGet package libraries
+│   │   ├── Domain/                                          # Domain layer packages
+│   │   │   ├── Hexalith.{Module}/                           # Aggregate roots, entities, state
+│   │   │   ├── Hexalith.{Module}.Abstractions/              # Domain interfaces, value objects
+│   │   │   └── Hexalith.{Module}.Events/                    # Domain events
+│   │   ├── Application/                                     # Application layer packages
+│   │   │   ├── Hexalith.{Module}.Commands/                  # CQRS command definitions
+│   │   │   ├── Hexalith.{Module}.Requests/                  # Queries & view models
+│   │   │   ├── Hexalith.{Module}.Application/               # Command & query handlers
+│   │   │   ├── Hexalith.{Module}.Application.Abstractions/  # Application interfaces
+│   │   │   └── Hexalith.{Module}.Projections/               # Read model projections
+│   │   ├── Infrastructure/                                  # Infrastructure layer packages
+│   │   │   ├── Hexalith.{Module}.Servers/                   # Shared server utilities
+│   │   │   ├── Hexalith.{Module}.ApiServer/                 # REST API controllers
+│   │   │   ├── Hexalith.{Module}.WebServer/                 # Web server implementation
+│   │   │   └── Hexalith.{Module}.WebApp/                    # Blazor web application
+│   │   └── Presentation/                                    # Presentation layer packages
+│   │       ├── Hexalith.{Module}.UI.Components/             # Reusable Blazor components
+│   │       ├── Hexalith.{Module}.UI.Pages/                  # Blazor page components
+│   │       └── Hexalith.{Module}.Localizations/             # language resources
+│   └── servers/                        # Docker/deployment projects
+└── test/
+    └── Hexalith.{Module}.Tests/        # Unit & integration tests
+```
+
+### Layer Organization by Package
+
+Each layer is a separate NuGet package with clear responsibilities:
+
+| Package | Layer | Contents |
+|---------|-------|----------|
+| `Hexalith.{Module}` | Domain | Aggregate roots, entities, value objects, state |
+| `Hexalith.{Module}.Abstractions` | Domain | Domain interfaces, shared value objects |
+| `Hexalith.{Module}.Events` | Domain | Domain events |
+| `Hexalith.{Module}.Commands` | Application | Command definitions, validators |
+| `Hexalith.{Module}.Requests` | Application | Query definitions, view models |
+| `Hexalith.{Module}.Application` | Application | Command & query handlers, services |
+| `Hexalith.{Module}.Projections` | Application | Event projections, read model handlers |
+| `Hexalith.{Module}.Servers` | Infrastructure | Shared server utilities |
+| `Hexalith.{Module}.ApiServer` | Infrastructure | REST API controllers, modules |
+| `Hexalith.{Module}.WebServer` | Infrastructure | Web server implementation |
+| `Hexalith.{Module}.WebApp` | Infrastructure | Blazor web application |
+| `Hexalith.{Module}.UI.Components` | Presentation | Reusable Blazor component library |
+| `Hexalith.{Module}.UI.Pages` | Presentation | Page-level Blazor components |
+| `Hexalith.{Module}.Localizations` | Domain | i18n resources |
+
+### Package Dependency Flow
+
+```text
+Presentation (UI.Components, UI.Pages, Localizations)
+    ↓
+Infrastructure (Servers, ApiServer, WebServer, WebApp)
+    ↓
+Application (Commands, Requests, Handlers, Projections)
+    ↓
+Domain (Aggregates, Events)
+    ↓
+Abstractions (value objects & interfaces)
+```
+
+## C# Coding Standards
+
+### Primary Constructors
+
+Use primary constructors for classes and records when possible:
+
+### XML Documentation
+
+Use XML documentation for all public, protected, and internal members:
+
+### Record Properties Documentation
+
+For records with primary constructors, document properties using `<param>` tags:
+
+```csharp
+/// <summary>
+/// Represents a customer in the system.
+/// </summary>
+/// <param name="Id">The unique customer identifier.</param>
+/// <param name="Email">The customer's email address.</param>
+/// <param name="Name">The customer's full name.</param>
+/// <param name="CreatedAt">When the customer was created.</param>
+public sealed record Customer(
+    string Id,
+    string Email,
+    string Name,
+    DateTimeOffset CreatedAt);
+```
+
+### Naming Conventions
+
+| Element | Convention | Example |
+|---------|------------|---------|
+| Interfaces | Prefix with `I` | `IOrderRepository` |
+| Async methods | Suffix with `Async` | `GetOrderAsync` |
+| Event handlers | Suffix with `Handler` | `OrderPlacedHandler` |
+| Commands | Imperative verb | `PlaceOrder`, `CancelOrder` |
+| Events | Past tense | `OrderPlaced`, `OrderCancelled` |
+| Value objects | Noun | `Money`, `Address`, `Email` |
+| Aggregates | Domain noun | `Order`, `Customer`, `Product` |
+
+### Error Handling
+
+- Use `ArgumentException.ThrowIfNullOrWhiteSpace()` for string validation
+- Use `ArgumentNullException.ThrowIfNull()` for null checks
+- Create domain-specific exceptions for business rule violations
+- Use Result pattern for expected failures
+
+```csharp
+public sealed class InsufficientStockException(
+    string productId,
+    int requested,
+    int available)
+    : DomainException($"Product {productId}: requested {requested}, available {available}")
+{
+    public string ProductId { get; } = productId;
+    public int Requested { get; } = requested;
+    public int Available { get; } = available;
+}
+```
+
+### Logging with LoggerMessageAttribute
+
+Use `LoggerMessageAttribute` for high-performance source-generated logging. This approach provides compile-time checking and avoids boxing allocations.
+
+**Rules:**
+
+- Always use `static partial` methods with `LoggerMessageAttribute`
+- Pass `ILogger` as the first parameter
+- For exceptions, pass `Exception` as the second parameter (before other parameters)
+- Use structured logging with named placeholders (e.g., `{OrderId}`, `{CustomerId}`)
+- The class must be declared as `partial`
+
+## Testing Standards
+
+Unit Tests use XUnit and Shouldly and test methods are written using Pascal Case naming.
+
+### Test Organization
+
+```text
+test/
+└── Hexalith.{Module}.Tests/    # All tests for the module
+    ├── {Aggregate}/            # Tests organized by aggregate
+    │   ├── {Command}Tests.cs   # Command tests
+    │   ├── {Event}Tests.cs     # Command tests
+    │   ├── {Query}Tests.cs     # Query tests
+    │   └── {Aggregate}Tests.cs # Aggregate tests
+    └── ...
+```
+
+## Build Configuration
+
+This project uses centralized build configuration from `Hexalith.Builds`:
+
+- `Hexalith.Build.props` - Common build properties
+- `Hexalith.Package.props` - NuGet package properties
+- `Directory.Packages.props` - Centralized package versions
+
+## Start the application
+
+```bash
+cd AspireHost
+dotnet run
+```
+
+## Additional Resources
+
+- [Hexalith Documentation](https://github.com/Hexalith/Hexalith)
+- [DAPR Documentation](https://docs.dapr.io/)
+- [Fluent UI Blazor](https://www.fluentui-blazor.net/)
+- [Commit Guidelines](https://github.com/angular/angular/blob/main/contributing-docs/commit-message-guidelines.md)
diff --git a/AGENTS.md b/AGENTS.md
index bfb4e98..2066fba 100644
--- a/AGENTS.md
+++ b/AGENTS.md
@@ -4,14 +4,6 @@ This file provides guidance for AI assistants (Claude, Copilot, Cursor, etc.) wo

 ## AI assistant instructions

-Before working in this repository, read the shared Hexalith LLM instructions —
-[`hexalith-llm-instructions.md`](https://github.com/Hexalith/Hexalith.AI.Tools/blob/main/hexalith-llm-instructions.md)
-— and follow it.
-
-Before working on any module user interface or UX, also read
-[`Hexalith.AI.Tools/hexalith-ux-instructions.md`](https://github.com/Hexalith/Hexalith.AI.Tools/blob/main/hexalith-ux-instructions.md)
-and follow it.
-
 When adding or updating GitHub workflow or action references to Hexalith.Builds
 actions or reusable workflows, always use the latest main branch reference:
 `Hexalith/Hexalith.Builds/<action-path>@main`. Do not pin Hexalith.Builds
diff --git a/Github/initialize-dotnet/README.md b/Github/initialize-dotnet/README.md
index 19a1f74..50b3e37 100644
--- a/Github/initialize-dotnet/README.md
+++ b/Github/initialize-dotnet/README.md
@@ -9,7 +9,7 @@ declared by a `global.json` file, or install an explicit SDK version when no
 | Input | Description | Required | Default |
 |-------|-------------|----------|---------|
 | `global-json-file` | Path to a `global.json` file that pins the SDK version. Takes precedence over `dotnet-version` when set. | No | `''` |
-| `dotnet-version` | SDK version passed to `actions/setup-dotnet` when `global-json-file` is empty. | No | `10.0.300` |
+| `dotnet-version` | SDK version passed to `actions/setup-dotnet` when `global-json-file` is empty. | No | `10.0.302` |
 | `aspire` | Install the Aspire workload when set to any non-empty value. | No | `''` |

 ## Steps
diff --git a/Github/initialize-dotnet/action.yml b/Github/initialize-dotnet/action.yml
index d6c68f0..77e046f 100644
--- a/Github/initialize-dotnet/action.yml
+++ b/Github/initialize-dotnet/action.yml
@@ -8,7 +8,7 @@ inputs:
   dotnet-version:
     description: 'Explicit .NET SDK version to install when global-json-file is not provided'
     required: false
-    default: '10.0.300'
+    default: '10.0.302'
   aspire:
     description: 'Whether to install Aspire workload'
     required: false
diff --git a/Github/publish-container-to-registry/README.md b/Github/publish-container-to-registry/README.md
index db46b94..99eeb4a 100644
--- a/Github/publish-container-to-registry/README.md
+++ b/Github/publish-container-to-registry/README.md
@@ -88,7 +88,7 @@ jobs:
       - name: Initialize .NET
         uses: Hexalith/Hexalith.Builds/Github/initialize-dotnet@main
         with:
-          dotnet-version: '10.0.300'
+          dotnet-version: '10.0.302'

       - name: Publish application containers
         uses: Hexalith/Hexalith.Builds/Github/publish-container-to-registry@main
diff --git a/Github/unit-tests/README.md b/Github/unit-tests/README.md
index b4ba8eb..39993db 100644
--- a/Github/unit-tests/README.md
+++ b/Github/unit-tests/README.md
@@ -36,7 +36,7 @@ jobs:
       - name: Setup .NET
         uses: actions/setup-dotnet@main
         with:
-          dotnet-version: 10.0.300
+          dotnet-version: 10.0.302

       - name: Run unit tests for Core project
         uses: ./Github/unit-tests
diff --git a/Props/Directory.Packages.props b/Props/Directory.Packages.props
index 15dda70..89cb55c 100644
--- a/Props/Directory.Packages.props
+++ b/Props/Directory.Packages.props
@@ -206,7 +206,7 @@
     <PackageVersion Include="Microsoft.OpenApi" Version="2.9.0" />
     <PackageVersion Include="Microsoft.Playwright" Version="1.61.0" />
     <PackageVersion Include="Microsoft.SemanticKernel" Version="1.77.0" />
-    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="10.0.300" />
+    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="10.0.301" />
     <PackageVersion Include="Microsoft.TeamsFx" Version="3.0.0" />
     <PackageVersion Include="Microsoft.TypeScript.MSBuild" Version="6.0.3" />
     <PackageVersion Include="Microsoft.VisualStudio.Threading.Analyzers" Version="18.7.23" />

===== SUBMODULE TRACKED DIFF: references/Hexalith.Timesheets =====
diff --git a/AGENTS.md b/AGENTS.md
index b74f50b..0dffa4e 100644
--- a/AGENTS.md
+++ b/AGENTS.md
@@ -1,4 +1,4 @@
-# Hexalith.Timesheets Agent Instructions
+# Hexalith.Timesheets Claude Instructions

 ## Shared Hexalith LLM Instructions

diff --git a/_bmad-output/planning-artifacts/architecture.md b/_bmad-output/planning-artifacts/architecture.md
index d1186b9..420207b 100644
--- a/_bmad-output/planning-artifacts/architecture.md
+++ b/_bmad-output/planning-artifacts/architecture.md
@@ -149,7 +149,7 @@ This is not a generic web application starter problem. The right foundation is a

 Current starter/tooling checks performed during this step:

-- Local SDK: `dotnet --version` returned `10.0.301`.
+- Local SDK observed during this architecture step: `dotnet --version` returned `10.0.301`; the current repository pin is `10.0.302`.
 - Local templates include `aspire-apphost`, `aspire-servicedefaults`, `aspire-starter`, `webapi`, `blazor`, `classlib`, `xunit`, and solution templates.
 - `dotnet new sln --name Hexalith.Timesheets` on the local .NET 10 SDK creates `Hexalith.Timesheets.slnx`.
 - NuGet lists `Aspire.ProjectTemplates` `13.4.5` as the current package version on 2026-06-18.
@@ -334,7 +334,7 @@ Timesheets domain state changes will persist through `Hexalith.EventStore`. The

 **Version notes:**

-- .NET SDK: local target is .NET 10, current local SDK `10.0.301`.
+- .NET SDK: local target is .NET 10, current local SDK `10.0.302`.
 - Dapr SDK packages: target latest verified package line `1.18.4` for Timesheets-owned direct pins, subject to scaffold compatibility validation. Current Timesheets root package files do not directly pin Dapr SDK packages; Dapr arrives through sibling EventStore project references, and the submodule-owned `Hexalith.Builds` package props still keep base `Dapr` at `1.17.9` while Dapr ASP.NET Core/Actors/Workflow pins are `1.18.4`.
 - Aspire templates/packages: current `Aspire.ProjectTemplates` checked as `13.4.5`.

diff --git a/docs/launch-readiness.md b/docs/launch-readiness.md
index 34343d0..196ac13 100644
--- a/docs/launch-readiness.md
+++ b/docs/launch-readiness.md
@@ -17,7 +17,7 @@ Final package-currency verdict: **CONCERNS / WAIVED**. Direct Timesheets package
 | Vulnerable and deprecated packages | PASS | `dotnet list Hexalith.Timesheets.slnx package --vulnerable --include-transitive` and `dotnet list Hexalith.Timesheets.slnx package --deprecated` reported no vulnerable or deprecated packages from the current sources. | No compatibility, security, or deterministic-build reason was found for adding an explicit transitive pin. |
 | Root npm applicability | not applicable | The Timesheets root has no `package.json`, `package-lock.json`, `npm-shrinkwrap.json`, `pnpm-lock.yaml`, or `yarn.lock`. | Manifests under `Hexalith.*` are sibling-submodule owned and excluded from this Timesheets root audit. |
 | Transitive drift | reviewed, no pin | Solution-level `dotnet list Hexalith.Timesheets.slnx package --outdated --include-transitive` and the AppHost project-level variant still fail with `error: Sequence contains no matching element`. Project-level audits succeeded for the remaining Timesheets projects. Drift sources are `Google.Protobuf` via sibling `Hexalith.EventStore.Client` -> `Dapr.Client` `1.18.4`; `Polly.*` and `System.Threading.RateLimiting` via direct `Microsoft.Extensions.Http.Resilience` `10.7.0`; `DiffEngine`, `EmptyFiles`, `System.CodeDom`, and `System.Management` via `Shouldly`; `Microsoft.Testing.*`, `Microsoft.ApplicationInsights`, and `Microsoft.Bcl.AsyncInterfaces` via `xunit.v3`; `Newtonsoft.Json` via `Microsoft.NET.Test.Sdk`; and `Castle.Core` / `System.Diagnostics.EventLog` via `NSubstitute`. | The drift is patch/test-stack/platform-transitive only, with no vulnerability/deprecation finding. Adding root transitive pins would promote package dependencies without a documented compatibility, security, or deterministic-build need. |
-| Platform and submodule alignment | waived | Root Timesheets pins .NET SDK `10.0.301`, Aspire packages/AppHost SDK `13.4.6`, and `CommunityToolkit.Aspire.Hosting.Dapr` `13.4.0-preview.1.260602-0230`; it does not directly pin Dapr SDK or Fluent UI packages today. `Hexalith.Builds/Props/Directory.Packages.props` remains submodule-owned and pins base `Dapr` `1.17.9`, Dapr ASP.NET Core/Actors/Workflow `1.18.4`, Aspire Hosting `13.4.6`, and `Microsoft.FluentUI.Components` `4.11.6`. | Owner: Platform / Hexalith.Builds. Risk: architecture and launch-readiness claims can overstate actual package pins. Revisit condition: the platform reconciles `Hexalith.Builds` package policy or a Timesheets-owned UI/package story adds direct Fluent UI/Dapr pins. |
+| Platform and submodule alignment | waived | Root Timesheets pins .NET SDK `10.0.302`, Aspire packages/AppHost SDK `13.4.6`, and `CommunityToolkit.Aspire.Hosting.Dapr` `13.4.0-preview.1.260602-0230`; it does not directly pin Dapr SDK or Fluent UI packages today. `Hexalith.Builds/Props/Directory.Packages.props` remains submodule-owned and pins base `Dapr` `1.17.9`, Dapr ASP.NET Core/Actors/Workflow `1.18.4`, Aspire Hosting `13.4.6`, and `Microsoft.FluentUI.Components` `4.11.6`. | Owner: Platform / Hexalith.Builds. Risk: architecture and launch-readiness claims can overstate actual package pins. Revisit condition: the platform reconciles `Hexalith.Builds` package policy or a Timesheets-owned UI/package story adds direct Fluent UI/Dapr pins. |

 ## Launch-Scope Classification

diff --git a/global.json b/global.json
index 1f2c64d..5f5c8e8 100644
--- a/global.json
+++ b/global.json
@@ -1,6 +1,6 @@
 {
   "sdk": {
-    "version": "10.0.301",
+    "version": "10.0.302",
     "rollForward": "latestPatch"
   }
 }

===== SUBMODULE TRACKED DIFF: references/Hexalith.PolymorphicSerializations =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
deleted file mode 120000
index f9a753f..0000000
--- a/.github/copilot-instructions.md
+++ /dev/null
@@ -1 +0,0 @@
-Hexalith.Builds/.github/copilot-instructions.md
\ No newline at end of file
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
new file mode 100644
index 0000000..596e227
--- /dev/null
+++ b/.github/copilot-instructions.md
@@ -0,0 +1,7 @@
+# AI Instructions
+
+Please read and follow the instructions in [references/Hexalith.Builds/CLAUDE.md](./references/Hexalith.Builds/CLAUDE.md) for coding standards, build commands, and project conventions.
+
+Also read and follow the shared Hexalith LLM instructions in [hexalith-llm-instructions.md](https://github.com/Hexalith/Hexalith.AI.Tools/blob/main/hexalith-llm-instructions.md).
+
+Before working on any module user interface or UX, also read [Hexalith.AI.Tools/hexalith-ux-instructions.md](https://github.com/Hexalith/Hexalith.AI.Tools/blob/main/hexalith-ux-instructions.md) and follow it.

===== SUBMODULE UNTRACKED DIFF: references/Hexalith.Timesheets =====
diff --git a/.github/copilot-instructions.md b/.github/copilot-instructions.md
new file mode 100644
index 0000000..0dffa4e
--- /dev/null
+++ b/.github/copilot-instructions.md
@@ -0,0 +1,16 @@
+# Hexalith.Timesheets Claude Instructions
+
+## Shared Hexalith LLM Instructions
+
+Before starting any work in this repository, read and follow
+[`Hexalith.AI.Tools\hexalith-llm-instructions.md`](./Hexalith.AI.Tools/hexalith-llm-instructions.md).
+
+## Git Submodules
+
+- Never initialize or update nested submodules recursively unless the user
+  explicitly asks for nested submodules.
+- For repositories with submodules, initialize/update only root-level submodules
+  by default.
+- Avoid `git submodule update --init --recursive` and similar recursive
+  submodule commands unless nested submodule initialization is explicitly
+  requested.

</diff>
