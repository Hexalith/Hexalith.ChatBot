---
title: Product Requirements Document - Hexalith.ChatBot
created: "2026-05-10"
updated: "2026-05-28"
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-02b-vision
  - step-02c-executive-summary
  - step-03-success
  - step-04-journeys
  - step-05-domain
  - step-06-innovation
  - step-07-project-type
  - step-08-scoping
  - step-01b-continue
  - step-09-functional
  - step-10-nonfunctional
  - step-11-polish
  - step-12-complete
  - step-e-01-discovery
  - step-e-02-review
  - step-e-03-edit
inputDocuments:
  - "D:/Hexalith.ChatBot/_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md"
documentCounts:
  productBriefs: 1
  research: 0
  brainstorming: 0
  projectDocs: 0
  projectContext: 0
classification:
  projectType: saas_b2b
  domain: enterprise collaboration / AI project workspace
  complexity: medium
  projectContext: greenfield product on existing Hexalith platform
workflowType: 'prd'
releaseMode: single-release
status: final
completedAt: "2026-05-10T21:29:40.8096941+02:00"
lastEdited: "2026-05-28"
editHistory:
  - date: "2026-05-10"
    changes: "Addressed validation findings for NFR measurability, B2B SaaS entitlement/RBAC coverage, MVP scope deltas, CLI/MCP parity exception, and FR actor clarity."
  - date: "2026-05-28"
    changes: "Addressed PRD validation findings for minimum MVP slice, pilot success thresholds, open assumptions, glossary, lifecycle terminology, AI risk taxonomy, trace links, and FR acceptance guidance."
  - date: "2026-05-28"
    changes: "Addressed PRD validation findings for MVP sequencing, operation catalog completeness, lifecycle-state consistency, source-context traceability, and high-risk FR acceptance guidance."
  - date: "2026-05-28"
    changes: "Full pass on 52 findings from the 2026-05-28 validation report (prior grade: Poor). Resequenced single release into three increments (M0 vertical UI > M1 cross-surface parity + governance > M2 operations + recovery). CLI/MCP parity moved from M0 to M1. WCAG 2.2 AA scoped per-increment to enumerated surfaces. Created addendum.md (Confidence Thresholds, Risk Classifier, Command Allowlist v0/v1, Tenant Policy Schema, Shared Command Pipeline, Idempotency Keys, Replay Isolation, ID Evolution, Inbound Message Authenticity, Authority Class Mapping, Operating Baselines). Added A9a, A10, A11 with named owners. Added FR48a-d, FR55a, FR75a-g, FR81a, FR91a, FR95a, NFR9a, NFR13a, NFR15a, NFR17a, NFR42a, NFR49a, NFR50a. Decomposed noun-density FRs. Added Data Governance Surface and UI Surface Inventory. Resolved 'MVP must define' deflections inline. Added 5 [NOTE FOR PM] callouts at real tensions. v2 reviewer pass (rubric Fair, adversarial Shippable conditional on N1-N5) — N1-N5 fixed in tightening pass plus 5 reconciliation gaps from product brief. Polish applied via bmad-editorial-review-structure + bmad-editorial-review-prose (18 PRD edits + 9 addendum edits, no substance changes)."
---

# Product Requirements Document - Hexalith.ChatBot

**Author:** Jerome
**Date:** 2026-05-10

## Executive Summary

Enterprise project teams still coordinate critical external work through email, but email loses the connection between messages, files, decisions, approvals, and execution. Hexalith.ChatBot turns project email threads into structured, auditable workspaces where people and AI agents can act with clear project context, authorization, and traceability.

The MVP deliberately starts with email because external collaboration already happens there, especially with customers, suppliers, and partners who will not join another internal tool. It primarily serves project managers and delivery teams coordinating external project work by email. Platform operators and automation builders are secondary users who configure channels, identity, permissions, AI actions, and command surfaces.

The MVP proves one narrow but valuable loop: an authorized external participant sends a project email with attachments; the system associates it with the correct project using explicit identifiers, participant identity, mailbox routing rules, or human review when confidence is low; attachments are stored in governed folders; task intent is captured; risky work is routed for approval; and the resulting human or AI action is executed through Hexalith service commands with outcomes recorded as events and projected back into the workspace.

ChatBot does not own core project records, files, parties, identity, or event history (it does own substantial derived state — see §Data Governance Surface). It owns orchestration concerns: channel intake, project-context resolution, task-intent capture, approval routing, AI-action mediation, and cross-surface command exposure. Durable source-of-truth state remains in the appropriate Hexalith bounded contexts and is changed only through their commands and events.

The user-voice anchor from the product brief: *"I can collaborate simply with others on a project or subject, and the AI can help move the work forward without losing context."* The MVP increments are how that promise becomes shippable — M0 proves the loop for one team, M1 makes it reach across UI/CLI/MCP, M2 makes it operable in production.

### What Makes This Special

Hexalith.ChatBot treats AI as a governed project actor, not a disconnected assistant. An AI action is only useful when the system knows which project it belongs to, which participant requested it, which files it used, which authority approved it, which command executed it, and what outcome was recorded.

The key product bet is not that AI can summarize an email. It is that an email can become authorized project work: linked to the right project, backed by the right files, approved by the right participant, executed through the right command, and recorded for review. (This is the product team's working thesis pending pilot validation per the Measurable Outcomes section, not an external market claim — see §Market Context & Competitive Landscape for the explicit disclaimer.)

The core insight is that enterprise AI needs durable operating boundaries. Tenant, party, project, conversation, folder, approval, command, event, and projection boundaries make AI automation governable enough for real collaboration with customers, partners, suppliers, and internal teams.

### Key Product Risks to Validate Early

The product will fail if mailbox-to-project association is unreliable, because misplaced messages or attachments can corrupt trust in the workspace. The MVP should not depend on fully automatic project detection. Ambiguous messages must fail closed into review with suggested matches and evidence.

The product will also fail if external participant authorization is treated as a UI concern. Authorization must be enforced at command and query boundaries before external participants, AI agents, or automation clients can access files, create task requests, trigger commands, or send outbound messages.

The product also risks failure if governance feels like extra administration rather than a natural continuation of email-based work. The MVP must minimize manual classification, approval overhead, and context re-entry.

Early validation must measure project association accuracy, authorization enforcement, approval completion rate, command execution consistency, and audit completeness. Risky actions include actions that modify project state, send outbound communication, expose file contents, create or assign tasks, invoke external tools, or operate on behalf of a participant.

## Project Classification

Hexalith.ChatBot is classified as a B2B SaaS product in the enterprise collaboration and AI project workspace domain. Its complexity is medium: it is not a regulated vertical product, but it has significant requirements for multi-tenant identity, external collaboration, mailbox ingestion, project association, file governance, approval workflow, auditability, and cross-surface command parity.

The PRD context is a greenfield product definition on an existing Hexalith platform. The product is new, but it depends on existing Hexalith services and infrastructure, including Hexalith.Conversations, Hexalith.Projects, Hexalith.Folders, Hexalith.Parties, Hexalith.Tenants, Hexalith.EventStore, Keycloak, Aspire, and Hexalith.FrontComposer.

Source context for downstream architecture and story creation is the current Hexalith module planning context available in the repository on 2026-05-28, including the project-context artifacts for Hexalith.Conversations, Hexalith.Projects, Hexalith.Folders, Hexalith.Parties, Hexalith.Tenants, Hexalith.EventStore, Hexalith.FrontComposer, Hexalith.Memories, and Hexalith.Commons where applicable.

**Material-change re-check protocol.** A "material change" to a sibling bounded-context artifact is one that changes any of: the command/event contract surface ChatBot depends on (command names, event names, schemas, error codes), the authorization model (role names, scope semantics, policy claims), the identifier model (party-ID format, project-ID lifecycle, conversation-ID semantics), the integration topology (replacing or removing one of the named integrations in §Integration List), or any RBAC matrix entry that ChatBot's FRs reference. Editorial revisions, internal refactoring, and documentation-only changes are not material.

The **System Architect** owns the trigger for re-check; the Architect monitors the sibling repositories' release notes and PRD updates and opens a re-check task within 5 business days of any material change. The re-check produces either (a) a confirmation that ChatBot integration assumptions still hold, or (b) a list of required PRD/architecture/story updates with owners and a target completion date. The re-check outcome is logged to `.decision-log.md` regardless of result.

## Success Criteria

### User Success

Users succeed when project email stops being a disconnected communication channel and becomes reliable project context. A project manager or delivery team member can receive an external email, understand which project it belongs to, review any uncertain association, store attachments in the correct governed folder, and convert the request into trackable human or AI work without manually reconstructing the thread.

The primary user success signal is accurate email-to-project association. When deterministic evidence is sufficient, the system associates the message with the correct project automatically. When confidence is low or multiple projects could match, the system presents a ranked candidate list with supporting evidence such as project alias, conversation identifier, sender or recipient party match, mailbox routing rule, subject reference, prior conversation linkage, attachment metadata, referenced document, or prior user correction. The user chooses the correct project, rejects all candidates, or defers the decision. Users should not need to open multiple systems, search mailbox history, or manually compare project records to decide where an email belongs.

Users also succeed when AI-assisted work remains understandable and controlled. Low-risk read-only assistance can be allowed by tenant policy, while risky AI actions require explicit review. Before an AI action modifies project state, sends external communication, exposes file contents, creates or assigns tasks, invokes external tools, or acts on behalf of a participant, the user can review the proposed action, approve or reject it, and see the recorded outcome afterward.

### Business Success

The business succeeds when Hexalith.ChatBot proves that email-first external collaboration can become governed project execution with less coordination overhead and higher trust. Success means project teams can turn inbound and outbound project email into reliable, governed project context without manually forwarding, copying, or re-explaining the thread across tools.

The strongest business validation is repeated use of the mailbox-to-project workflow by project managers and delivery teams. Business success is demonstrated when at least one pilot organization uses mailbox-to-project association as a recurring project workflow, not a one-time import or demo path. Reduced coordination overhead is evidenced by fewer manual project updates sourced from email, fewer duplicate project conversations, faster routing of emails requiring action, lower reassignment rates, and shorter time from email receipt to available project context.

The MVP must also validate that governance does not feel like extra administration. Users should be able to resolve ambiguous project association from the evidence already captured by the system, without re-reading the full email thread or re-entering context manually.

The MVP must validate that cross-surface command parity has business value. Core email-to-project workflow operations must be available through chatbot UI, CLI, and MCP so human users, automation scripts, and AI agents operate through the same governed command model.

Success criteria must be evaluated across contributor usability, delivery-lead visibility, tenant-admin control, compliance auditability, and developer automation parity.

### Technical Success

Technical success requires email ingestion, project association, participant authorization, file handling, task capture, approval routing, command execution, and audit projection to work as one controlled path. The MVP must fail closed when project association, participant identity, tenant scope, or authorization cannot be resolved. Fail-closed outcomes must be visible to an authorized user with enough evidence to resolve or dismiss the item.

Mailbox-to-project association must support deterministic routing signals such as project-specific mailbox aliases, conversation identifiers, explicit project references, participant identity, and mailbox routing rules. Deterministic signals must take precedence over AI-generated inference. AI may rank candidates or summarize evidence, but it must not override fail-closed association rules.

Association confidence must be configurable and auditable. `T_high` and `T_low` are tenant or deployment policy thresholds calibrated against the evaluation dataset and reviewed through audit evidence. Emails above `T_high` are eligible for automatic association when required deterministic evidence is present. Emails between `T_low` and `T_high` require user choice from a candidate project list. Emails below `T_low` are deferred or rejected. No email may be silently associated when required deterministic signals conflict.

Authorization must be enforced at command and query boundaries across UI, CLI, and MCP. External participants, AI agents, and automation clients must resolve to scoped parties before accessing files, creating task requests, triggering commands, or sending outbound communication. Unauthorized projects must never appear as candidates, evidence, logs visible to the user, CLI output, or MCP response payloads.

Mailbox ingestion must tolerate duplicates, retries, and partial failures. Message intake, attachment storage, and task creation must use idempotency, duplicate detection, retry handling, and visible failure states so repeated delivery does not create conflicting project records.

The system must preserve auditability. Every auto-association, user-selected association, rejection, defer, retry, duplicate suppression, risky AI approval, and executed AI action must produce an audit record containing actor, tenant, timestamp, source message ID, decision, evidence summary, command surface, requester, approver when applicable, command, project, input files, and result.

### Measurable Outcomes

Primary MVP outcomes:

- Correct email-to-project association rate, including automatic deterministic matches and user-resolved ambiguous matches.
- Automatic association reassignment rate.
- Ambiguous association resolution rate.
- Median time to resolve ambiguous association.
- Percentage of ambiguous associations resolved using presented evidence without manual context re-entry.
- Percentage of unresolved or unauthorized emails routed to visible review or failure states.

Association quality outcomes (anchored to A9a evaluation dataset, owned by the **Test Architect**):

- For the A9a evaluation dataset, deterministic email-to-project association achieves at least `95% precision` and `90% recall` for non-ambiguous messages. These targets are calibration targets, not contractual commitments to tenants; calibration runs occur before each pilot phase, before M0 release, before M1 release, and quarterly thereafter. [ASSUMPTION A9a]
- The A9a evaluation dataset produces `0` critical false-positive associations involving unauthorized projects, where "critical false-positive" is defined in `addendum.md` §Confidence Thresholds as auto-association of a message into a project the sender is not authorized to read. [ASSUMPTION A9a]
- Deterministic project matches are attached automatically only when required evidence is present.
- Ambiguous messages produce suggested project candidates with supporting evidence, confidence scores, and explicit user choice.
- Each candidate project includes evidence signals when available, such as sender or domain match, project keyword match, thread history, attachment metadata, referenced ticket or document, or prior user correction.
- Users can choose a candidate project, reject all candidates, or defer the decision without losing the original email context.
- User-selected associations are recorded as correction events and available for future association evaluation.

Control and audit outcomes:

- No ambiguous message is attached to a project without user confirmation.
- No unresolved sender can access project files, create task requests, trigger commands, or send outbound project communication.
- No cross-tenant project, participant, file, approval, command, or projection access is permitted.
- Risky AI actions require approval before execution.
- Low-risk AI assistance executes only within tenant policy and authorized project scope.
- Attachments from associated emails are stored in the selected project's governed folder structure.
- Repeated processing of the same email or message ID is idempotent: no duplicate project artifacts, no duplicate audit decisions except retry metadata, and identical final status unless source data changed.
- Failed ingestion and association attempts are countable by reason.
- Audit completeness is measured across association, override, approval, command, attachment, retry, duplicate suppression, and AI action events.

Cross-surface parity outcomes:

- MVP parity means UI, CLI, and MCP expose the same core governed operations for the email-to-project workflow. Interaction design, batching, and presentation may differ by surface.
- The MVP parity set includes ingest/status, project candidate review, project association decision, attachment storage/status, task request capture, approval decision, retry, audit lookup, and status lookup.
- For ambiguous emails, UI, CLI, and MCP responses return the same ordered candidate list, evidence snippets, confidence scores, and rejection or defer reasons.
- For every core MVP operation, UI, CLI, and MCP share the same backend behavior and produce equivalent state transitions.
- Automated parity tests verify create, associate, choose candidate, reject, defer, retry, status, and audit lookup.

Validation outcomes:

- MVP QA includes a labeled corpus of representative emails covering deterministic matches, ambiguous matches, no-match cases, unauthorized project references, cross-tenant references, duplicates, retries, attachment references, and risky AI approval paths.
- MVP validation is achieved when pilot users can process a representative mailbox sample containing clear matches, ambiguous matches, duplicates, attachments, external parties, and unauthorized cases, with correct project association decisions, complete evidence display, and complete audit records across UI, CLI, and MCP.

Pilot adoption outcomes (numeric targets are starter values per A11; final targets fix after a 2–4 week baseline measurement at the pilot tenant — see A11):

- At least one pilot tenant uses the workflow for `4` consecutive weeks with at least `2` monitored project mailbox patterns and at least `5` active projects represented in the evaluation sample. [ASSUMPTION A11]
- At least `70%` of ambiguous association decisions in the pilot are resolved from presented evidence without manual context re-entry. The `70%` is a directional target chosen against the pilot baseline; it ratchets up across increments (M0 → M1 → M2) as evidence quality improves. [ASSUMPTION A11]
- Median time from email receipt to available governed project context is reduced by at least `40%` compared with the pilot tenant's current manual email-to-project update process, measured against the A11 baseline. [ASSUMPTION A11]
- Manual project updates sourced from email are reduced by at least `30%` for pilot projects included in the monitored workflow. [ASSUMPTION A11]
- Pilot users complete at least `10` governed AI action reviews in M0 and at least `30` across M0 + M1, with approval, rejection, refusal, and audit outcomes visible across UI (M0) and at least one machine surface (M1).

AI-action and attachment-handling outcomes (replacing the product-brief metrics that no longer fit the email-only MVP shape):

- **AI-action-execution success rate** (replaces the brief's "task completion rate"): of governed AI actions that pass approval and execute through an allowlisted command, `≥ 95%` complete successfully (no command-execution failure, no rollback) per A11 baseline window. The brief's "task completion rate" framing assumed full task-lifecycle ownership, which is out of MVP per A8; AI-action-execution is the MVP equivalent. [ASSUMPTION A11]
- **Attachment auto-handling rate** (replaces the brief's "document management automation rate"): of attachments captured from associated project email, `≥ 90%` are auto-stored into the correct governed project folder with classification metadata, without manual user intervention; the remainder route to UI review with a reason from the FR77 catalog. The brief's "document management automation" framing assumed broader document intelligence, which is out of MVP per the §Out-of-scope list; attachment auto-handling is the MVP equivalent. [ASSUMPTION A11]

The **Test Architect** owns the measurement of the association quality outcomes, and the **Product Lead** owns the measurement of the pilot adoption outcomes. Both report results into the §Validation outcomes review before each increment release.

## Product Scope

### MVP - Minimum Viable Product

The MVP proves the full email-to-project collaboration loop:

- Receive project email through controlled mailbox patterns.
- Send project email only through approved outbound communication flows.
- Identify sender and recipients as parties within the correct tenant context.
- Associate email with a project using deterministic signals when possible.
- Present candidate projects with evidence for user selection when association is ambiguous.
- Allow the user to select a candidate project, reject all candidates, or defer association.
- Store email attachments in governed project folders.
- Represent email activity as project conversation context.
- Capture task intent from conversation actors.
- Classify AI actions by risk.
- Allow low-risk AI assistance according to tenant policy and project authorization.
- Require approval for state-changing, externally visible, file-exposing, task-creating, tool-invoking, or participant-representing AI actions.
- Execute approved actions through Hexalith service commands.
- Record outcomes as auditable events and projections.
- Handle duplicate mailbox delivery, retries, and partial ingestion failures without corrupting project state.
- Expose core email-to-project project, conversation, file, task, approval, audit, and status operations through chatbot UI, CLI, and MCP.

MVP scope is intentionally limited to governed email-to-project context creation and approved project collaboration commands. Task intent is limited to detecting candidate action requests from associated project email and surfacing them for governed review; it does not include full task lifecycle management unless explicitly approved through service commands. Conversation context is limited to email-derived project context, association evidence, attachments, decisions, detected risks, and approved actions. Service-command execution is limited to a fixed allowlist of project collaboration commands with authorization, approval where required, and audit logging across UI, CLI, and MCP.

Generic email support is not a separate MVP channel. Non-Microsoft 365 / Exchange mailbox sources may be included only when they satisfy the same controlled mailbox contract: stable message identity, tenant-scoped mailbox authority, attachment capture, sender/recipient identity evidence, idempotent delivery handling, audit metadata, and fail-closed authorization behavior. This depends on A1.

CLI and MCP parity is intentional MVP scope for this product, even though many B2B SaaS PRDs treat CLI surfaces as non-goals. Hexalith.ChatBot requires CLI and MCP because automation builders and AI agents must use the same governed command model as human users. Within the MVP, CLI/MCP parity is delivered in Increment M1 rather than M0 so the first release validates the email-to-project vertical through one surface before parity is extended; this preserves the parity bet while keeping M0 buildable by the named team. This depends on A3.

The cost of keeping CLI and MCP in the MVP is reduced breadth elsewhere: no additional collaboration channels, no full task lifecycle, no broad document intelligence, no commercial packaging, no arbitrary integrations, and only the core governed email-to-project operation set. UI-only is insufficient for the MVP thesis because automation builders and AI agents are first-class actors; if they cannot inspect, resolve, approve, execute, and audit through the same command model, the product has not proven governed human-and-machine collaboration. The parity bet is validated when, by the end of M1, pilot automation or AI-agent workflows use CLI or MCP for at least one association/status/audit path without bypassing authorization, approval, or audit behavior.

[NOTE FOR PM] Deferring CLI/MCP from M0 to M1 weakens the parity thesis between increments. Pilots that engage during M0 will see UI-only behavior and may form expectations that CLI/MCP later violates (e.g., terminology, error surfaces, approval flows). Mitigation: M1 ships before any pilot exit, and M0 release notes flag CLI/MCP as "coming in M1, parity-by-construction."

#### Minimum Release Slice — Three Increments

The MVP is delivered in three increments within the single release window. Each increment is releasable to the pilot cohort on its own; M0 proves the thesis, M1 proves parity and governance breadth, M2 proves operational readiness. The named team (one product lead, one architect, backend/service engineers, one frontend engineer, one CLI/MCP engineer, one security/identity engineer, one QA/test architect, DevOps) sizes the per-increment scope. If any increment slips, the rule is to shorten breadth inside the increment, never to break the dependency order.

The three increments share a common non-negotiable: no increment is shippable if tenant isolation, authorization, fail-closed behavior (as contracted in NFR15a — see Reliability NFRs), idempotency (per the per-operation-class table in `addendum.md`), audit completeness (per NFR50a), or safe AI approval behavior is removed. If resources are constrained, trim dashboards, advanced mailbox inference, advanced approval-policy flexibility, document-intelligence breadth, and UI polish before trimming any increment's safety floor.

##### Increment M0 — Vertical Thesis Path (UI-only)

M0 proves one complete email-to-governed-action loop end-to-end, in the UI, for one tenant, with humans driving every decision point.

- One controlled Microsoft 365 / Exchange mailbox pattern for one configured tenant, with stable message identity, conversation/thread identifiers, attachment metadata, and delivery/retry state.
- Deterministic association using explicit project identifier, mailbox routing rule, or conversation/thread identifier (the three signals listed; learned/inferred matching is out of scope for M0).
- Ambiguous association review in the UI: candidate projects with ranked evidence; user can confirm, reject, defer, or correct a prior association.
- Governed attachment capture into Hexalith.Folders with metadata, status, and quarantine of unsafe attachments.
- One task-intent and AI-action path: detect a candidate action from associated project email, package authorized project context, classify risk via the heuristic + tag classifier (see `addendum.md` §Risk Classifier), route approval-required work to a human reviewer, and execute through one allowlisted command (see M0 allowlist in `addendum.md` §Command Allowlist v0).
- Keycloak-backed identity and tenant scope; fail-closed authorization enforced at the command/query boundary per NFR15a.
- M0 lifecycle states: Received, Proposed, Associated, NeedsReview, Deferred, Rejected, Failed, Skipped, Corrected. The full state-transition matrix expands in M1, but `Skipped` is part of the M0 command-spine contract because duplicate suppression and out-of-scope mailbox rules need a terminal safe state.
- Required audit events for M0 ops: message intake, candidate generation, association decision, attachment handling, AI action proposal, approval decision, command execution result, retry/duplicate suppression, correction.
- Negative isolation tests across the M0 actor set: human user, tenant admin, project owner, background worker, M365 event, AI actor.
- Dependency failure handling for M0 dependencies: M365, Keycloak, Hexalith.Projects/Folders/Parties, EventStore, attachment scanner, AI service.
- WCAG 2.2 AA conformance applies only to the M0 UI surfaces enumerated in NFR60: ambiguous association review, AI action approval, and the project conversation view that hosts them. Later increments inherit the bar as their surfaces are added.

M0 is not shippable without every item listed above for Increment M0. Outbound communication, CLI, MCP, multi-tenant rollout, and operational dashboards are explicitly out of scope for M0.

##### Increment M1 — Cross-Surface Parity & Full Governance

M1 extends the M0 loop across surfaces and completes the governance model that the parity bet depends on.

- CLI operation parity for inspect, associate, reject, defer, correct, retry, approve, execute, status, and audit lookup.
- MCP operation parity for governed AI-agent/tool access to the same authorized command model.
- Cross-surface parity enforced by a single shared command pipeline at the architectural layer (see FR81a and `addendum.md` §Shared Command Pipeline). Contract tests verify the invariant; they do not enforce it.
- Service-client permissions and Keycloak service-account flows.
- One outbound draft-and-send path that preserves sender authority (per FR48 + `addendum.md` §Inbound Message Authenticity → "Authority class mapping"), recipients, approved content, and audit history.
- Full state-transition matrix expansion per §Association Lifecycle and States.
- Per-tenant, per-role, per-project, per-action-type, per-recipient, per-risk-class approval policies — surfaced through the Tenant Policy Schema in `addendum.md` §Tenant Policy Schema.
- Tenant-admin permission model as its own FR group (FR75a–FR75g): what admins can see, what they can operate on, the audit obligations attached to admin actions, and the absence of any bypass to authorization or audit.
- Versioned command allowlist artifact under change control (see `addendum.md` §Command Allowlist v1).
- Risk-classifier mechanism named, calibrated against the evaluation dataset (see A9a), with a stated misclassification fallback and audit chain when classification disagrees with reviewer action.
- Negative isolation tests across the remaining actor types: CLI client, MCP client, service client.
- Inbound-message-authenticity checks (DMARC/DKIM/SPF, header inspection, on-behalf-of disambiguation, external-sender posture) — see FR48a–FR48d.

##### Increment M2 — Operations, Recovery, Continuity

M2 makes the system operable in production by tenant administrators and Hexalith ops, with recovery and continuity guarantees.

- Operational dashboards for mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, and audit lag.
- Recovery: RPO ≤ 15 min and RTO ≤ 4 hr verified through a documented continuity drill; both targets carry an [ASSUMPTION] tag until the drill calibrates them (see A10).
- Replay/simulation contract: replay events run against a separate test tenant with outbound adapters intercepted; replay records are distinguishable from production records in audit (see FR95a and `addendum.md` §Replay Isolation).
- Idempotency contract per operation class, defined in `addendum.md` §Idempotency Keys (key composition, replay window, equivalence rule, conflict response).
- Tamper-evident audit chain implemented as an append-only WORM store with hash-chained envelopes (see NFR49a).
- Audit completeness as a production observable: the fraction of state-mutating operations whose audit chain reconstructs the operation end-to-end ≥ 99.5% per rolling 7-day window (see NFR50a).
- Cross-tenant isolation tests for vector indexes, embedding stores, and prompt-context caches (see FR55a / NFR9a).
- WCAG 2.2 AA conformance extended to M1 surfaces with UI (admin dashboards, approval-policy configuration). CLI and MCP are outside WCAG scope.
- Performance and operability instrumentation across all operation classes; SLOs published per NFR42a.

This sequencing depends on A2, A3, A8, A9, and A10. Architecture and epics must preserve the increment dependency order (M0 → M1 → M2); within an increment, internal ordering is an architecture concern.

Out of scope for MVP:

- Autonomous project creation from email.
- General email client replacement.
- Full task lifecycle management.
- Full document intelligence over attachments.
- Broad knowledge management.
- Unrestricted command execution or automation.
- Cross-tenant association suggestions.
- Scheduled-time, file-addition, and broad event-triggered automation outside associated project email.

### Growth Features (Post-MVP)

Growth scope includes broader channel and automation capabilities after the controlled email path is proven:

- Teams, WhatsApp, and additional messenger channels.
- More advanced mailbox interpretation for forwarded threads, aliases, shared mailboxes, and complex conversation histories.
- Improved automatic project matching based on learned patterns and historical context.
- Richer approval policies by tenant, role, project, action type, recipient, and risk class.
- The three automation triggers deferred from MVP (the product brief named four; only conversation-triggered automation lands in MVP): **scheduled-time triggers** (e.g., daily project summary, weekly approval-aging digest), **file-addition triggers** (e.g., new attachment in a governed project folder triggers classification + summary), and **explicit user-instruction triggers** (e.g., "from now on, when supplier X emails, draft a reply and route to me for approval"). These were originally in MVP scope per the product brief; the email-only MVP delays them so the team can prove the governed-AI-action loop on one trigger source first.
- More advanced task orchestration across scheduled, file-triggered, and conversation-triggered workflows (composed from the trigger types above).
- Expanded operational dashboards for mailbox processing, failed associations, approval queues, duplicate handling, and AI action outcomes.
- Deeper document intelligence for classification, extraction, summarization, and comparison.

### Vision (Future)

The long-term vision is a governed AI-native project collaboration layer for the Hexalith ecosystem. Email is the first wedge, but the broader product becomes a multi-channel workspace where internal users, external participants, automation, and AI agents collaborate around durable project context.

In the vision state, Hexalith.ChatBot provides reusable project-aware AI workers, multi-channel conversation capture, governed task execution, audit-ready action history, document intelligence, reusable MCP tools, and consistent command access across human and machine surfaces. The product becomes the safe operating boundary where enterprise collaboration and agentic automation meet.

[NOTE FOR PM] The "ChatBot" name reflects the vision-state interactive surface. In M0 the user-visible interaction model was the project conversation view + ambiguous association review + AI action approval, with no conversational chat surface. **Updated 2026-06-09 (`sprint-change-proposal-2026-06-09.md`, approved):** the interactive chat surface is now pulled into scope as a **governed write surface**, delivered in **Epic 10** (Interactive Chat Surface & FrontComposer Shell Adoption). This is option (b) above made first-class rather than "thin": a composer that wraps the existing governed affordances — every message is admitted through CommandGateway and a risky request becomes an AI-action proposal (approval-required), so the safety floor is unchanged and no ungoverned/freeform textbox is introduced. This resolves the long-standing naming-vs-scope finding ("make the chat surface a first-class MVP concern"). Positioning during the MVP window may still lead with "Governed AI Project Workspace"; the chat surface and the workspace are the same surface. Epic 10 also adopts the Hexalith.FrontComposer Shell as the mandated UI composition layer (closing the Story 1.14 deferred shell swap).

## User Journeys

### Journey 1: Business Contributor Requests AI Help From a Project Conversation

Amira is a business contributor helping move a customer delivery project forward. The project conversation contains internal discussion, messages from external parties represented through Hexalith.Parties, and email-derived updates with attached documents. A new project email enters the workspace, Hexalith.ChatBot associates it with the project using deterministic evidence, and the evidence is visible before Amira asks the AI for help.

Before Hexalith.ChatBot, Amira would have copied the thread into a separate AI tool, downloaded attachments, searched the project folder for the latest documents, and manually checked whether the response was safe to send. She would have moved faster, but with weak traceability and a meaningful risk of using stale or unauthorized context.

In Hexalith.ChatBot, Amira opens the project conversation and sees the message associated with the project. The external sender is resolved as a party, the attachments are linked to governed project folders, and the system shows why this email belongs in this project. Amira expects the AI to understand the project without re-explaining the thread, but she also needs to know whether the AI is using approved context or guessing.

Amira asks the AI to compare the attached document with current project folder content and draft a response. The AI does not act as an unbounded assistant. It creates a proposed project action with visible project scope, requester identity, input files, intended command, expected output, and risk classification. Before approval, Amira sees a plain-language summary of what will happen and why approval is required. Because the action may expose file contents and produce outbound communication, the system routes it for approval instead of executing silently.

The value moment is controlled acceleration. Amira can review the proposed AI action inside the same project conversation, approve it, reject it, or request changes. After approval, the action executes through the governed command model, and the result is recorded back into the project conversation with audit history.

If the AI lacks sufficient context, the system asks for additional files or clarification instead of fabricating an answer. If approval is rejected, the rejection reason remains visible in the conversation. If command execution fails, Amira sees a clear failure state and retry path without losing the original request.

This journey reveals requirements for conversation-context display, party resolution, attachment linking, AI task intent capture, risk classification, approval routing, evidence display, failure recovery, audit history, and user-friendly review of proposed AI actions.

### Journey 2: Business Contributor Resolves an Ambiguous Project Association

Marc is an authorized project contributor responsible for resolving ambiguous project communication for projects he can access. A message arrives from a known external party, but that party participates in multiple active projects. The subject line references a shared initiative, and the attachments could plausibly belong to more than one workspace.

Hexalith.ChatBot does not attach the message automatically. It presents candidate project rows with confidence state and evidence: sender or recipient party match, thread references, project alias or identifier, subject/body signals, attachment names or metadata, prior associations, conversation participants, prior corrections, and reason labels. Marc can confirm a candidate, choose a different project, reject all candidates, defer the decision, or escalate/manual review.

The critical moment is trust. Marc does not need to search mailbox history or compare project records manually. The system gives him enough evidence to choose, records the decision as an auditable association event, and preserves the original email context.

If no candidate is viable, the message remains in a visible unresolved state instead of disappearing or contaminating a project workspace. If multiple candidates are equally likely, the system names the uncertainty instead of hiding it. If Marc chooses the wrong project and later corrects it, the correction is recorded with actor, time, previous association, new association, and reason. That correction can inform future association evaluation, but it must not automatically authorize unsafe future matches.

This journey reveals requirements for fail-closed association behavior, candidate ranking, evidence snippets, confidence bands, user selection, reject/defer states, reassignment/correction events, unresolved queues, future evidence capture, and parity of the same association decision across UI, CLI, and MCP.

### Journey 3: External Party Sends Project Context Into Hexalith

Elena is an external party represented in Hexalith.Parties. She may be a customer, supplier, partner, or any organization/contact participating in a project. In the MVP, she does not authenticate into Hexalith.ChatBot, does not receive a tenant account, and does not need to learn a new collaboration tool.

Elena sends an email with a decision, request, and supporting attachment to the project mailbox or controlled project email pattern. From her perspective, the workflow still feels like normal external collaboration. She expects the receiving team to understand the context, use the right documents, and respond responsibly.

Hexalith.ChatBot resolves Elena as a party, checks tenant and project scope, evaluates project association signals, and stores the message and attachment only if the project context is safe enough. Inbound permissions are derived from project email collaboration rules and scoped party relationships. If association is ambiguous, an internal authorized user chooses the correct project. If Elena is not authorized for the project, if her identity cannot be resolved, or if sender spoofing/mismatch is detected, the system fails closed and does not expose candidate projects, files, or internal context.

The value moment is invisible governance. Elena can keep using email, while the internal team gains structured project context, governed file handling, and auditable follow-up. If her identity is unresolved, the internal user sees an unverified external party state with evidence and can link to an existing party, create/link a pending party, reject, or quarantine according to policy.

This journey reveals requirements for party resolution, external identity mapping, mailbox routing, project association, authorization checks before context exposure, safe failure behavior, unresolved-party handling, quarantine, and email continuity for external collaborators.

### Journey 4: Project Owner Corrects a Wrong Association

Priya is the project owner for a sensitive delivery project. She notices that an email-derived conversation item has been associated with her project, but the message actually belongs to another workspace. The mistake matters because the message has already generated candidate task intent and may have influenced AI context preparation.

Priya opens the association details and sees the original evidence, actor, timestamp, confidence state, candidate projects shown, and any downstream artifacts created from the association. She corrects the association or marks it as misfiled. The system keeps the prior association audit-visible, updates project conversation linkage, moves or relinks attachments according to project ownership rules, refreshes or invalidates AI context indexes, and emits a correction event to downstream consumers.

The value moment is accountable repair. Priya can cleanly detach contaminated context without erasing history, and the system prevents AI from using the prior derived context unless it is explicitly reviewed under the corrected association.

This journey reveals requirements for association correction, derived-context tracking, attachment relinking, AI context invalidation, correction events, audit history, and project-owner authority boundaries.

### Journey 5: Tenant Admin Configures Governed Email Collaboration

Nora is a tenant admin responsible for making external collaboration safe. She configures controlled mailbox patterns, party resolution rules, project association signals, tenant policies for low-risk AI assistance, MVP approval rules for externally visible or project-mutating output, and audit visibility rules.

Her concern is not only whether the workflow works when everything is clean. She needs confidence that the system behaves safely when senders are unresolved, messages are duplicated, attachments are renamed, projects share similar names, an external party participates in multiple projects, or a dependency is unavailable.

Nora reviews operational views for unresolved parties, ambiguous project matches, duplicate message suppression, rejected associations, approval queues, failed command executions, and accuracy/correction metrics. She verifies that unauthorized projects never appear as candidates, evidence, CLI output, MCP payloads, or audit details to users who cannot access them.

The value moment is operational confidence. Nora sees that mailbox ingestion, party resolution, authorization, approval, and audit records are tenant-scoped and policy-driven. When a message cannot be safely resolved, it enters a visible review or failure state instead of being silently attached, discarded, or executed.

This journey reveals requirements for mailbox configuration, tenant-scoped policy management, party resolution rules, approval policy configuration, authorization enforcement, operational dashboards, duplicate/retry handling, audit access, dependency failure handling, and failure-state review.

### Journey 6: Developer Uses CLI To Inspect and Resolve Project Email Workflow

Leo is a developer and automation builder supporting project operations. He uses the CLI to inspect unmatched or ambiguous project emails, view candidate projects and evidence, resolve an association, check attachment storage status, trigger an approved command, and verify audit output.

Leo does not expect the CLI to mimic the chatbot UI. He expects operation parity: the same command model, authorization rules, candidate list, evidence fields, resolve/reject/defer/correct actions, permission failures, state transitions, and audit results as the UI and MCP.

He runs a CLI command to list unresolved associations, inspects the candidate project evidence, selects the correct project, confirms attachment status, and checks the resulting audit record. When a retry is needed, the CLI exposes retry status without creating duplicate conversations, files, or task requests. If a CLI command succeeds but an audit projection is delayed, the CLI returns a clear partial-success state rather than implying the system is fully reconciled.

The value moment is repeatability. Leo can script governed operational tasks without bypassing authorization, approval, and audit requirements. The CLI becomes a trusted automation surface because it shares the same backend behavior as the user-facing experience.

This journey reveals requirements for CLI command structure, authentication, command/query authorization, candidate list output, association decision commands, attachment/status inspection, approval/status commands, audit lookup, idempotent retries, partial-success states, and parity tests across UI, CLI, and MCP.

### Journey 7: Compliance or Support Reviewer Investigates a Risky Action

Sofia is reviewing a reported concern: an AI-assisted response may have used the wrong project context. She needs to reconstruct what happened without relying on screenshots or informal explanations.

She opens the audit history and sees the source message ID, tenant, project, requester, party identities, candidate project evidence, selected association, rejected alternatives, input files, prior decisions that influenced the action, approval policy, approval decision, command surface, model/agent identity, executed command, timestamp, output, destination, and result. If the action was rejected, deferred, retried, corrected, or duplicate-suppressed, that state is visible as well.

The value moment is reconstructability. Sofia can determine who initiated the action, which project context was used, which emails, attachments, parties, and prior decisions influenced it, what candidates or alternatives were rejected, what policy applied, what model or agent acted, what output was produced, and where it went.

If Sofia lacks permission for a project, the system does not expose project names, candidate evidence, files, or sensitive audit details. Investigation access is powerful, but still tenant- and role-scoped. Sofia can investigate and escalate, but she does not necessarily have authority to mutate project association or project state.

This journey reveals requirements for audit completeness, searchable audit records, approval history, association evidence retention, correction history, duplicate/retry traceability, command-surface attribution, model/agent attribution, output destination tracking, and safe visibility rules for compliance/support users.

### Journey 8: User Reviews an AI Action Before It Leaves the Project Boundary

Amira asks the AI to prepare a response that may include project file content and be sent to an external party. The AI response is ready, but the system pauses before anything leaves the project boundary.

Amira sees the context used, the files referenced, the proposed action, the recipient or destination, the policy rule that triggered review, and the expected command. She can approve, reject, request revision, or cancel. The interface names the risk in plain language: the action is externally visible, file-exposing, project-mutating, tool-invoking, or participant-representing.

If Amira asks the AI to do something outside project boundary or policy, the AI refuses or routes for approval. No external email, project mutation, file exposure, or tool invocation occurs until the required authorization path succeeds. The denial or approval decision is audited with reason and policy rule.

This journey reveals requirements for AI action preview, policy-trigger explanation, approval/reject/revise/cancel actions, safe refusal, policy-denied audit records, and externally visible action controls.

### System Journey: Governed AI Execution

A project-aware AI agent receives a request from a conversation actor or command surface. The request asks it to analyze project files, summarize context, draft a response, classify an incoming request, or prepare a command for execution.

The AI agent does not receive an unbounded workspace. It receives project scope, requester identity, authorized input files, tenant policy, permitted action types, current approval requirement, and evidence source traceability. It can perform low-risk read-only assistance if policy allows, but it must create a proposed action for risky operations.

The value moment is enforceable agency. The AI can help move work forward, but it cannot silently cross tenant boundaries, access unauthorized files, send outbound communication, mutate project state, or invoke external tools without the required authorization and approval path.

If association is unresolved, the AI refuses project-specific action or asks for association resolution. If required context is missing, the AI asks for clarification or additional files. If authorization fails, the action is denied and audited. If approval is required, the AI waits for a human decision. If execution succeeds or fails, the result is recorded through the same command/event model as human and CLI actions.

This journey reveals requirements for AI action scoping, tool/command allowlists, policy-aware execution, approval gating, context sufficiency checks, authorization failure handling, unresolved-association refusal, audit logging, and consistent event projection.

### Shared Workflow Contract

The journeys share a common association lifecycle across UI, CLI, and MCP:

`Received -> Proposed -> Associated | Rejected | Deferred | NeedsReview | Failed | Skipped`

`Associated -> Corrected` is a superseding correction path that preserves the original association and downstream impact in audit history.

Canonical state definitions:

| State | Meaning | Terminal |
| --- | --- | --- |
| `Received` | The mailbox event or replayed message has been captured with tenant, mailbox, and source message identity. | No |
| `Proposed` | Candidate project association evidence has been generated but no authoritative association decision has been made. | No |
| `Associated` | An automatic or human-confirmed association links the email-derived context to one project. | No |
| `Rejected` | The reviewer rejected all candidate associations for the message. | **Yes (terminal).** Reprocessing does not move a `Rejected` item back to `Proposed`; it creates a new workflow instance with a new ID, carrying an audit link `superseded_by_workflow` ← old → `supersedes_workflow` → new. The original `Rejected` record is preserved unchanged. |
| `Deferred` | The reviewer chose not to decide yet; the item remains visible with next action and owner. | No |
| `NeedsReview` | The item requires human review because evidence, party identity, authorization, dependency status, or policy context is incomplete. | No |
| `Failed` | Processing reached a terminal failure that requires manual intervention or explicit reprocessing. | **Yes (terminal).** Reprocessing follows the same rule as `Rejected`: a new workflow instance is created with a new ID; the original `Failed` record is preserved. |
| `Skipped` | The item is intentionally not processed, such as duplicate suppression or out-of-scope mailbox rule. | Yes |
| `Corrected` | A previous association was superseded while preserving the original decision and downstream impact in audit history. | No |
| `Correcting` (sub-state of `Corrected`) | The correction has been recorded but derived-store invalidation is still in progress per FR91a / NFR17a. The corrected item remains in `Correcting` until all derived stores acknowledge invalidation. AI actions cannot use the corrected project context until this sub-state clears. | No (transient) |
| `Correction-delayed` (sub-state of `Correcting`) | Derived-store invalidation has exceeded the NFR17a SLO. Surfaces the responsible owner role and the next safe action; triggers a P2 incident. Clears to `Corrected` when invalidation completes. | No (transient, incident-flagged) |

Valid transitions must be defined in the state model for FR87-FR89. Deferred items are not skipped; they remain active review items. Skipped items are terminal unless an authorized reprocess command creates a new workflow instance or superseding decision.

Core queries:

- Show unresolved or deferred messages.
- Show candidate projects.
- Show evidence for each candidate.
- Show prior correction history.
- Show attachment/security status.
- Show audit history for association decisions and downstream actions.

Core commands:

- Associate email with a project.
- Reject candidate association.
- Defer association.
- Correct a previous association.
- Escalate/manual review.
- Retry or quarantine failed workflow.
- Approve, reject, revise, or cancel a proposed AI action.

Core events:

- Association candidate generated.
- Association selected.
- Association rejected.
- Association deferred.
- Association corrected.
- Party unresolved or linked.
- Attachment linked, blocked, moved, or relinked.
- Retry attempted.
- Duplicate suppressed.
- AI action proposed.
- Approval accepted, rejected, revised, or canceled.
- Command execution succeeded or failed.

Context ownership:

- Hexalith.ChatBot owns AI-mediated collaboration workflows and user-facing assistant interactions, plus the derived stores enumerated in §Data Governance Surface.
- Hexalith.Projects owns project identity, membership, and project conversation boundaries.
- Hexalith.Parties owns external participant identity resolution.
- Email ingestion owns message capture, headers, attachments, and delivery state.
- Audit/compliance owns immutable event records and investigation views.
- ChatBot references these source-of-truth contexts by ID; it does not duplicate their authority. Where ChatBot derives its own state from sibling sources (associations, candidate rankings, evidence snapshots, AI action proposals, approval records, projections, policy snapshots, lifecycle), the derived state is governed under §Data Governance Surface.

#### Data Governance Surface (Hexalith.ChatBot first-class durable records)

ChatBot does not own the source records (project, party, message, file), but it owns substantial derived state that is security-sensitive, retention-governed, and tenant-scoped. Architecture and operations treat the following records as ChatBot-owned, with full audit, retention, redaction, and isolation obligations:

| Record class | Source | Retention class | Redaction sensitivity | Isolation surface | Owner increment |
|---|---|---|---|---|---|
| Association record | derived from mailbox event + project ID | GDPR-aligned, default 7 years, configurable per `tenant-policy.audit.retention` | high (carries project name + sender identity) | per-tenant store partition | M0 |
| Candidate ranking | derived from scorer kernel + evidence | short-term (60 days default) | medium (carries evidence references) | per-tenant store partition | M0 |
| Evidence snapshot | extracted from message + provider headers | bound to associated message retention | high (carries message content fragments) | per-tenant store partition | M0 |
| AI action proposal | derived from task-intent kernel | bound to audit retention | high (carries proposal text, file references) | per-tenant store partition | M0 |
| Approval record | derived from reviewer decision | audit retention | high (carries reviewer identity + reason) | per-tenant store partition | M0 |
| Projection | derived from event stream | rebuilt on demand from EventStore | inherits source class | per-tenant store partition | M0 |
| Policy snapshot | derived from Tenant Policy Schema state | audit retention | medium (no PII; carries policy values) | per-tenant store partition | M1 |
| Lifecycle state | derived from workflow events | bound to workflow retention | low | per-tenant store partition | M0 |
| Workflow instance map (predecessor → successor) | derived from terminal-state reprocessing | audit retention | low | per-tenant store partition | M0 |
| Vector index / embedding store / prompt-context cache | derived from project files + conversations | configurable per `tenant-policy.ai-context.retention` | high (derived material can leak source content) | per-tenant store partition enforced at the store layer per FR55a / NFR9a | M2 |
| Outbound trace store (test-tenant only) | derived from replay/simulation per FR95a | bounded by test-tenant retention policy | medium | test-tenant only; nightly probe asserts no production presence | M2 |
| Approval queue / failed-association queue / unresolved-party queue (operational views) | projections | rebuilt on demand | inherits source class | per-tenant store partition | M1 |

Each record class carries tenant ID, source provenance, derivation kernel version, redaction state, and retention class in its row. Cross-tenant queries are not possible at the store-access layer (FR55a / NFR9a). Deletion follows the NFR49a retention path; mutation is restricted to authorized retention workflows.

### UI Surface Inventory (handoff to UX)

The eight journeys plus the System Journey imply distinct UI surfaces. The PRD does not author the inventory in detail — that work belongs to `bmad-ux`, which will produce a per-surface UX specification. The PRD enumerates the surfaces here to give UX an unambiguous starting list and to pin the per-increment NFR60 scoping.

**M0 UI surfaces (required for M0 release):**
- **S1 — Project conversation view** (UJ1, UJ3): renders email-derived messages, parties, attachments, decisions, approvals, AI outcomes; hosts the `informational` / `actionable` classification badges from FR26 and the AI-summary distinction from FR27.
- **S2 — Ambiguous association review** (UJ2, UJ3): candidate-list, evidence panel (FR23), confirm / reject / defer / correct affordances.
- **S3 — AI action approval** (UJ1, UJ8): proposed-action surface per FR42 acceptance bullets — command, input files, recipients, sender authority, risk classification, policy snapshot, expected outcome, approve / reject / request-revision / cancel.

**M1 UI surfaces:**
- **S4 — Correction surface** (UJ4): correction affordance with `correcting` state from FR91a, predecessor display.
- **S5 — Tenant admin configuration** (UJ5): Tenant Policy Schema editor (per `addendum.md` §Tenant Policy Schema), mailbox configuration, approval-policy configuration, allowlist version pin, two-person-rule confirmation for security-sensitive knobs.
- **S6 — Outbound approval** (UJ8 outbound extension): pre-send approval surface for outbound communication.
- **S7 — Cross-surface attribution view** (UJ6): which surface originated each action (FR85).

**M2 UI surfaces:**
- **S8 — Operational dashboards** (UJ5 expanded, UJ7): mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, audit projection lag, SLO/error-budget status from NFR42a.
- **S9 — Compliance investigation** (UJ7): audit search, evidence reconstruction, replay event distinction from FR95a, per-project redaction.
- **S10 — Admin queue operations** (FR75c): retry / requeue / quarantine / dismiss affordances at queue level.

CLI and MCP surfaces (M1) are not UI; they are addressed by §CLI and MCP Parity Boundary and do not participate in NFR60.

### Journey Requirements Summary

The journeys reveal these capability areas:

- Project conversation context built from email-derived messages, parties, attachments, task intent, decisions, approvals, failures, and AI outcomes.
- Party resolution through Hexalith.Parties for internal and external participants.
- External party participation that preserves normal email collaboration while enforcing tenant, project, and party boundaries.
- Email-to-project association with deterministic signals, confidence bands, candidate evidence, and fail-closed ambiguity handling.
- User-controlled association decisions: select candidate, reject all, defer, escalate, or correct a previous association.
- Correction decisions that can inform future evidence while never bypassing authorization, tenant scope, or fail-closed rules.
- AI action mediation with low-risk tenant policy and MVP confirmation gates for externally visible or project-mutating actions.
- AI agent scoping with explicit project, requester, input file, policy, tool, command, authorization, evidence, and audit boundaries.
- Governed attachment storage in project folders with duplicate, retry, scan, block, move, and relink protection.
- Tenant-scoped authorization enforced at command and query boundaries.
- Audit records for association, correction, rejection, defer, retry, duplicate suppression, party resolution, approval, command execution, AI action requests, and AI outcomes.
- CLI operation parity for the email-to-project workflow, including inspect, associate, reject, defer, correct, retry, approve, execute, status, and audit lookup.
- Admin controls for mailbox patterns, party resolution, confidence thresholds, approval policy, audit visibility, and failure review.
- Compliance/support investigation views that preserve traceability without leaking unauthorized project context.
- Security behavior that prevents unauthorized users from seeing project names, candidate projects, candidate evidence, files, CLI output, MCP payloads, or sensitive audit details.
- Recovery paths for ambiguous association, insufficient AI context, rejected approval, command failure, duplicate messages, unresolved parties, unauthorized parties, corrected associations, blocked attachments, denied AI actions, and degraded dependencies.

## Domain-Specific Requirements

### Compliance & Regulatory

Hexalith.ChatBot must support GDPR/EU data protection expectations for email-derived project context, attachments, external party records, AI action history, and audit records. The product must define what personal data is captured from email and project conversations, why it is processed, how long it is retained, who can access it, and how tenant administrators can respond to deletion, export, and correction requests where legally applicable.

External participants represented through Hexalith.Parties may include names, email addresses, organizations, domains, message metadata, attachments, and project participation history. These records must be tenant-scoped and access-controlled. The system must avoid exposing external-party details, candidate projects, project names, files, evidence snippets, or audit details to users who are not authorized for the relevant tenant and project.

Audit records must be complete enough to reconstruct governed decisions, but they must not become an unrestricted secondary data store. Audit views must enforce the same tenant, role, and project visibility rules as operational workflows. Retention and deletion behavior must be explicit for message content, attachments, association evidence, AI prompts/outputs, approval decisions, and immutable event records.

### Technical Constraints

Security must be enforced at command and query boundaries, not only in the UI. UI, CLI, and MCP must share the same authorization model, tenant scope, party/project resolution rules, and audit requirements. All project association decisions, AI actions, approval decisions, file access, outbound communication, and administrative operations must be attributable to an authenticated user, automation client, external party record, or governed AI actor.

Tenant isolation is non-negotiable. Project candidates, evidence snippets, party records, attachments, command outputs, CLI responses, MCP payloads, and audit views must never leak cross-tenant or unauthorized project information. Ambiguous association must fail closed when tenant, project, party, or authorization scope cannot be established.

AI actions must operate inside explicit project boundaries. The system must track requester, project scope, input files, proposed command, policy decision, approval state, execution result, and output destination. Risky actions must require review before modifying project state, exposing file content, sending outbound communication, invoking external tools, or acting on behalf of a participant.

Email ingestion must treat mailbox delivery as unreliable. The system must support idempotency, duplicate suppression, retry tracking, partial failure states, attachment security checks, and reconciliation between raw message state and projected project context. Audit-write failure must fail closed for risky actions.

### Microsoft 365 / Exchange Integration Requirements

The MVP must support controlled Microsoft 365 / Exchange-style mailbox integration patterns for project email collaboration. Integration design must respect tenant boundaries, mailbox ownership, delegated access, shared mailbox patterns where applicable, and enterprise identity provider expectations.

Mailbox ingestion must preserve message identifiers, sender and recipient addresses, timestamps, thread/conversation identifiers, headers needed for correlation, attachment metadata, and delivery/retry state. These fields are required for project association evidence, duplicate suppression, audit reconstruction, and troubleshooting.

The system must support secure authorization for mailbox access and avoid broad mailbox permissions where narrower delegated or application permissions can satisfy the workflow. Mailbox configuration should be tenant-admin controlled and auditable, including which mailboxes are monitored, which project patterns or aliases are allowed, and which users or services can process inbound and outbound project email.

Outbound project email must be governed. Sending or drafting external responses from project context must respect approval policy, sender authority, participant permissions, audit requirements, and Microsoft 365 mailbox constraints. The product must record who requested the outbound action, which mailbox or identity sent it, what content was approved, and which project context or files influenced it.

### Integration Requirements

Hexalith.ChatBot must integrate with existing Hexalith bounded contexts rather than duplicating their authority. Hexalith.Projects owns project identity and project boundaries. Hexalith.Parties owns internal and external participant records. Hexalith.Folders owns governed project folders and files. Hexalith.Tenants owns tenant facts and authorization context. Hexalith.EventStore supports command/event flow, audit-friendly outcomes, and projections.

The product must use stable identifiers across these contexts so email-derived decisions, attachment links, AI actions, approval records, CLI operations, MCP calls, and audit views can be correlated without copying source-of-truth records into ChatBot-owned storage.

CLI and MCP integration must expose the same governed operations as the UI for the email-to-project workflow. These surfaces must not provide bypass paths around UI-enforced permissions, approvals, tenant isolation, or audit recording.

### Risk Mitigations

The highest domain risk is incorrect or unsafe project association. Mitigation requires deterministic evidence, confidence bands, candidate project review, user correction, fail-closed behavior, and audit records for association decisions and corrections.

The second major risk is unauthorized context exposure. Mitigation requires command/query authorization, tenant-scoped candidate generation, safe audit visibility, attachment access checks, and suppression of unauthorized project names or evidence in UI, CLI, MCP, logs, and error messages.

The third major risk is unsafe AI execution. Mitigation requires project-scoped AI context, action risk classification, approval gates for risky actions, tool/command allowlists, output destination tracking, and auditable refusal or denial when policy is not satisfied.

The fourth major risk is mailbox integration unreliability. Mitigation requires idempotency, duplicate detection, retry handling, source message tracking, attachment scan states, visible failure queues, and reconciliation between mailbox events and project projections.

The fifth major risk is GDPR-sensitive data sprawl. Mitigation requires explicit data classification, retention policy, access controls, audit visibility rules, and defined handling for personal data in messages, attachments, parties, prompts, outputs, and audit records.

## Innovation & Novel Patterns

### Detected Innovation Areas

The core innovation in Hexalith.ChatBot is treating AI as a governed project actor rather than a disconnected assistant. The AI does not receive arbitrary copied context or operate as a side-channel tool. It acts inside explicit tenant, party, project, conversation, folder, approval, command, and audit boundaries.

This pattern changes the role of AI in project collaboration. Instead of only summarizing or drafting from user-provided prompts, the AI can participate in project work through governed actions: analyzing authorized files, preparing responses, classifying requests, proposing task actions, and executing approved commands. Every action remains attributable to a requester, project, input context, policy decision, approval state, command surface, execution result, and audit trail.

The second innovation is the operating model around ambiguity. The system does not rely on hidden AI confidence to silently attach email or execute work. When project association is uncertain, it exposes candidate projects and evidence to an authorized user, records the decision, and uses that decision as governed project context. AI assistance becomes safer because the project boundary is explicit before AI action begins.

### Market Context & Competitive Landscape

This PRD does not make external competitive claims without dedicated market research. The relevant product thesis is internal and technical: enterprise AI collaboration becomes credible when AI actions are constrained by the same boundaries that govern people, files, projects, and commands.

Hexalith.ChatBot should therefore be evaluated against its own operating promise: an AI action is useful only when the system can prove which project it belongs to, which participant requested it, which files it used, which policy allowed it, which approval applied, which command executed it, and what result was recorded.

### Validation Approach

Innovation validation should focus on technical proof rather than broad market claims. The MVP should prove that a governed AI actor can operate safely and usefully inside the email-to-project workflow.

Validation requires end-to-end scenarios where:

- Email-derived project context is associated with the correct project or routed to user resolution when ambiguous.
- The AI refuses or pauses when project association, party identity, authorization, or required context is unresolved.
- The AI receives only authorized project scope, files, commands, policies, and action types.
- Low-risk read-only assistance executes only within tenant policy and authorized project scope.
- Risky AI actions produce a proposed action with requester, project, files, command, output destination, and risk reason.
- Risky AI actions require approval before project mutation, outbound communication, file exposure, external tool invocation, or participant-representing behavior.
- Approved AI actions execute through Hexalith service commands rather than bypassing bounded contexts.
- AI outcomes are recorded as auditable events and projected back into the project conversation.
- UI, CLI, and MCP expose equivalent state transitions and audit records for the governed AI action lifecycle.

The technical validation bar is not that the AI always produces perfect output. The validation bar is that the AI operates only inside authorized project boundaries, fails closed when governance context is incomplete, and leaves a complete decision and execution trail.

### Risk Mitigation

Innovation-specific risks (false confidence, approval fatigue, authority confusion, cross-surface inconsistency, over-expansion) are addressed in the operational §Risk Mitigation Strategy under Project Scoping rather than restated here. The mitigations are: visible evidence + unresolved states + refusal behavior (false confidence); the FR81a + FR41 + NFR46 stack with the FR41 [NOTE FOR PM] tuning rule (approval fatigue); explicit requester / approver / command / audit attribution per NFR50 (authority confusion); the FR81a shared-pipeline invariant (cross-surface inconsistency); and the three-increment M0/M1/M2 sequencing with non-negotiable safety floors (over-expansion).

## B2B Governance and Tenant Requirements

### Project-Type Overview

Hexalith.ChatBot is a B2B SaaS collaboration product operating in a strict multi-tenant enterprise environment. The product must support project-centered collaboration between internal users, external parties, automation clients, and governed AI actors while preserving tenant isolation, project boundaries, authorization, and auditability.

The SaaS-specific concern is governed collaboration across organizational boundaries: tenant-scoped mailboxes, parties, projects, files, approvals, commands, AI actions, and audit records must remain consistently enforced across UI, CLI, and MCP surfaces.

Subscription tiers, billing, usage-based metering, self-service tenant provisioning, marketplace packaging, trials, plan limits, and commercial entitlement enforcement are out of scope for this PRD. Tenant access is administratively provisioned for MVP purposes.

### Entitlement and Packaging Boundary

The MVP has one administratively provisioned product entitlement: access to the governed email-to-project workflow for an approved tenant. Tenant administrators may enable or disable mailbox sources, service clients, AI actors, command capabilities, and operational limits, but these controls are safety and governance controls, not commercial plan gates.

MVP behavior must not vary by subscription tier. Authorization, audit retention required for the workflow, project association, attachment handling, approval gates, CLI access, MCP access, and failure handling must be determined by tenant policy and role permissions, not billing package.

Future commercial packaging may introduce plan limits for monitored mailboxes, project count, retained audit duration, AI action volume, advanced approval policies, automation breadth, and operational analytics. Those packaging decisions must not weaken tenant isolation, authorization, audit completeness, or fail-closed behavior.

### Technical Architecture Considerations

Hexalith.ChatBot must operate as an orchestration layer over existing Hexalith bounded contexts. It must not become the source of truth for projects, parties, files, tenants, identity, or event history. It references those domains through stable identifiers and executes state changes through governed service commands.

Email-to-project association logic is owned by a Project Association context. Mailbox ingestion, CLI, MCP, and AI actors may submit candidate signals or association requests, but they must not independently assign authoritative project links.

The technical architecture must support strict tenant isolation, command/query authorization at every surface, a shared operation contract across UI/CLI/MCP, deterministic and auditable project association before AI consumes project context, fail-closed behavior when required context is unresolved, event-backed traceability, and a clear separation between low-risk read-only assistance and risky actions requiring approval.

### Context Ownership

This section is the **canonical** ownership listing for the PRD. The §Context Ownership block inside §Shared Workflow Contract and the brief framing in the §Executive Summary point here rather than repeat. ChatBot's first-class durable records are detailed in §Data Governance Surface.

- Hexalith.ChatBot owns AI-mediated collaboration workflows, user-facing assistant interactions, and the derived records enumerated in §Data Governance Surface (associations, candidate rankings, evidence snapshots, AI action proposals, approval records, policy snapshots, projections, lifecycle, vector indexes, replay traces, queue projections).
- Hexalith.Projects owns project identity, membership, and project conversation boundaries.
- Hexalith.Parties owns internal and external participant identity resolution.
- Hexalith.Folders owns governed project folders, attachment storage, file access control, and file metadata.
- Hexalith.Tenants owns tenant facts, tenant boundaries, tenant policies, and authorization context.
- Hexalith.EventStore supports command/event flow, event-backed traceability, projections, retries, duplicate suppression records, and audit-friendly outcomes.
- Mail integration owns message capture, headers, attachments, delivery state, and Microsoft 365 / Exchange synchronization concerns.
- Other contexts consume decisions through published contracts rather than duplicating decision logic.

### Tenant Model

Hexalith.ChatBot must enforce strict tenant isolation. No tenant may see another tenant’s project names, candidate projects, parties, files, email metadata, association evidence, command outputs, CLI responses, MCP payloads, cache entries, vector/index artifacts, integration tokens, background jobs, or audit records.

`tenantId` must be resolved from authenticated Keycloak claims or trusted service-client context, not from untrusted CLI, MCP, API, or request-body values. Cross-tenant identifiers in requests must be rejected even if the authenticated principal has valid credentials in another tenant. A command or query with a mailbox, project, message, or resource outside the actor’s tenant must fail closed with an auditable authorization failure.

Cross-tenant candidate suggestions are explicitly prohibited. If an email, party, or project signal could match another tenant, the system must suppress that information and fail closed into tenant-appropriate review or rejection.

### Permission Model

The MVP role-based and policy-based permission model around the email-to-project workflow is defined in §RBAC Matrix and §Service Client Permissions, and operationalized through FR75a–FR75g for tenant-admin scopes.

Core human roles:

- **TenantAdmin:** configures mailbox patterns, party resolution rules, project association policies, confidence thresholds, approval policies, identity integration, service-client permissions, and audit visibility.
- **ProjectAdmin / ProjectOwner:** owns project-level accountability; can review newly associated external conversations, approve sensitive AI use, correct misfiled associations, and manage project-specific collaboration boundaries.
- **ProjectMember / Contributor:** requests AI help, views authorized project conversation context, reviews proposed AI actions, and can approve/cancel actions when granted permission.
- **MailboxOwner:** grants or manages controlled mailbox access according to tenant policy.
- **Auditor / Compliance Reviewer:** investigates audit history and failure states within authorized tenant/project scope; may escalate issues but does not automatically gain mutation authority.

Machine and automation actors:

- **ServiceClient:** receives explicit scoped grants only and cannot inherit human user permissions or broad tenant admin rights.
- **AI Actor:** operates only inside explicitly provided project scope, authorized input files, tenant policy, command allowlists, and approval requirements.

Permissions must be enforced at command and query boundaries. A user or client must not receive unauthorized project candidates, evidence snippets, files, audit details, or command outputs. Permission failures must not reveal whether a target resource exists.

### RBAC Matrix

The MVP permission model uses role-based defaults plus tenant policy constraints. This matrix defines the minimum allowed action/resource boundaries; deployments may restrict permissions further.

| Actor | Allowed Resources | Allowed Actions | Explicitly Blocked |
| --- | --- | --- | --- |
| TenantAdmin | Tenant mailbox configuration, tenant policy, service clients, audit visibility, operational queues | Configure mailbox patterns, confidence thresholds, approval policies, service-client grants, notification routing, and tenant-level limits | Access to project content outside granted tenant scope; bypassing project authorization |
| ProjectAdmin / ProjectOwner | Authorized projects, associated conversations, project files, approvals, correction history | Review associations, approve risky AI actions, correct associations, manage project collaboration boundaries, inspect project audit history | Cross-tenant access; mailbox permission changes outside tenant policy |
| ProjectMember / Contributor | Authorized project conversations, allowed files, assigned review/approval items | Request AI help, review proposed actions, approve or cancel actions when policy grants authority, inspect visible status | Approving actions without delegated authority; viewing restricted evidence |
| MailboxOwner | Tenant-authorized mailboxes and mailbox permission status | Grant or manage controlled mailbox access according to tenant policy | Project association overrides unless also granted project authority |
| Auditor / Compliance Reviewer | Authorized audit records, failure states, redacted support context | Investigate association decisions, approvals, command outcomes, risky AI actions, and failure states | Mutating project state or broadening access by audit role alone |
| ServiceClient | Explicitly granted tenant/project/service scopes | Execute granted command/query operations with correlation, expiry, and audit metadata | Inheriting human roles; operating outside granted scope; silent privilege escalation |
| CLI Client | Same backend resources as the authenticated actor or service client | Inspect, associate, reject, defer, retry, approve, execute, status, and audit operations where authorized | Bypassing UI/API authorization, audit logging, validation, or tenant filters |
| MCP Client | Same backend resources as the authorized AI or automation actor | Access governed workflow tools where policy and scope permit | Tool calls that expose restricted evidence, cross tenant data, or unapproved actions |
| AI Actor | Explicitly packaged project context, authorized files, allowlisted commands | Produce proposals, perform low-risk assistance, execute approved actions through governed commands | Acting as a privileged system user; using unapproved files, tools, commands, or recipients |
| Background Worker | Assigned workflow items, mailbox events, retries, projections | Process idempotent work, retries, duplicate suppression, projections, and notifications | Mutating state without command validation, tenant scope, idempotency, and audit behavior |

### Service Client Permissions

Service clients are not users. They require dedicated identities with least-privilege scopes per integration and operation. Service-client authorization must not inherit UI roles except through explicit delegated flows where the source user, tenant, scope, and expiry are recorded.

Service-client classes and their enumerated scopes:

| Service-client class | Increment | Scope | Authorized command/query set | Credential expiry |
|---|---|---|---|---|
| `mailbox-ingestion-client` | M0 | per-tenant; one configured mailbox pattern | `MailboxEvent.Receive`, `Message.Capture`, `Attachment.Capture` | 90 days, auto-rotated |
| `audit-projection-client` | M0 | per-tenant; read source events, write projection state | `Event.Read`, `Projection.Append`, `Projection.Rebuild` (admin-triggered only) | 90 days, auto-rotated |
| `background-retry-client` | M0 | per-tenant; retry-only access to failed operations | `Operation.Retry`, `Operation.Status.Read` | 30 days, auto-rotated |
| `cli-automation-client` | M1 | per-tenant; configured per CLI installation; user-delegated via OAuth device flow | the MVP parity set (per §Cross-surface parity outcomes) | 7 days, refresh required |
| `mcp-tool-client` | M1 | per-tenant + per-AI-actor; MCP-tool-scoped grants | the MVP parity set, restricted to commands tagged `mcp-exposed` | 24 hours, refresh required |
| `ai-action-execution-client` | M1 | per-tenant + per-AI-actor; bound to a specific approved AI-action proposal | exactly one command from the current allowlist version, exactly once | bound to approval record; expires on use or 5 minutes |

Service-client authorization never inherits UI roles. Delegated flows (e.g., `cli-automation-client` acting on behalf of a user) record source user, tenant, scope, expiry, and the OAuth grant evidence in audit. Every service-client action carries tenant, client identity, operation, resource type, triggering integration event where applicable, result, and audit metadata in its audit envelope per NFR50.

Expired, revoked, over-scoped, and under-scoped service-client credentials must fail closed and be covered by acceptance tests.

### Command and Query Contracts

MVP command contracts include the canonical governed workflow operations listed in this section. Architecture may split or compose them, but UI/API, CLI, and MCP must expose equivalent authorization outcomes, state transitions, audit behavior, and redaction semantics for the parity set.

- `CaptureMailboxEvent`
- `ProposeEmailProjectAssociation`
- `AssociateEmailToProject`
- `ConfirmEmailProjectAssociation`
- `RejectEmailProjectAssociation`
- `DeferEmailProjectAssociation`
- `MarkEmailAssociationNeedsReview`
- `CorrectEmailProjectAssociation`
- `ReprocessEmailAssociation`
- `QuarantineEmailAssociation`
- `LinkOrResolveEmailParticipant`
- `CaptureEmailAttachment`
- `StoreEmailAttachmentInProjectFolder`
- `CaptureTaskIntent`
- `MarkTaskIntentDisposition`
- `ProposeAIAction`
- `ApproveAIAction`
- `RejectAIAction`
- `RequestAIActionRevision`
- `CancelAIAction`
- `ExecuteApprovedProjectCommand`
- `CreateOutboundProjectEmailDraft`
- `SendApprovedProjectEmail`
- `RecordWorkflowAuditDecision`
- `RetryWorkflowOperation`
- `GrantServiceClientPermission`
- `RevokeServiceClientPermission`

Each command must include actor identity, tenant scope, correlation ID, idempotency key, target resource IDs, expected result codes, and audit metadata.

MVP query contracts include:

- `GetEmailAssociationStatus`
- `ListProjectAssociationCandidates`
- `GetProjectAssociationEvidence`
- `ListUnresolvedOrDeferredMessages`
- `GetAttachmentStorageStatus`
- `GetTaskIntentStatus`
- `GetAIActionProposal`
- `GetApprovalStatus`
- `GetWorkflowOperationStatus`
- `GetAuditHistory`
- `GetMailboxIngestionHealth`
- `ListOperationalQueues`
- `GetServiceClientPermissions`
- `GetProjectAccessForActor`

Queries must apply the same tenant and role filters as commands. There is no admin/debug bypass in MVP.

### Association Lifecycle and States

Email-to-project association must be idempotent by tenant, mailbox identity, and message identity. Reprocessing the same message must not create duplicate project links, conversations, files, task requests, or audit decisions except retry metadata.

Required association states (canonical ordering, matching §Shared Workflow Contract):

- `Received`
- `Proposed`
- `Associated`
- `Rejected`
- `Deferred`
- `NeedsReview`
- `Failed`
- `Skipped`
- `Corrected`

The §Shared Workflow Contract table is canonical: it defines terminal-state semantics, the reprocessing rule (terminal states are terminal; reprocess creates a new workflow instance with audit linkage), and the M0-versus-M1 increment scoping of each state. The state list above appears here only as a frame for the Association Lifecycle section.

Each association must have a confidence state such as automatic, suggested, rejected, or manually confirmed. Automatic association may occur only when deterministic rules meet a configured confidence threshold. Ambiguous emails must not be auto-associated, and users must be able to inspect why an association was suggested or rejected.

### Trust Boundaries

Hexalith.ChatBot must define explicit trust boundaries between authenticated human users, external parties, service clients, AI actors, mailbox integrations, CLI clients, MCP clients, and Hexalith bounded contexts. A request crossing any boundary must carry tenant scope, caller identity, authorization context, command/query intent, and audit metadata.

CLI and MCP are first-class MVP surfaces for the email-to-project workflow. They are clients over the same command/query surface, not privileged backdoors. They must use public application/service APIs only and must not connect directly to databases, message queues, internal indexes, mailbox stores, or tenant-scoped storage.

AI actors must be treated as governed service actors with delegated authority, not as privileged system users. An AI actor can only use the project scope, files, commands, and tools granted by policy and by the requester’s authorized context.

### CLI and MCP Parity Boundary

CLI and MCP must expose only the same governed MVP operations available through approved application services. They must not bypass tenant isolation, role checks, audit logging, association confidence rules, validation, or human-review requirements.

Parity means equivalent authorization and outcomes, not identical UX or full UI feature equivalence. CLI/MCP error responses may be adapted for the surface but must preserve equivalent security semantics. Audit records must identify whether an action came from UI/API, CLI, MCP, background worker, mailbox event, or AI actor.

This CLI/MCP surface is an intentional B2B SaaS exception because the product serves automation builders and AI agents as first-class users. CLI and MCP must remain governed clients of the same backend contracts, not separate administrative interfaces.

### Microsoft 365 / Exchange Permission Constraints

Microsoft 365 / Exchange integration must distinguish mailbox read, attachment read, draft creation, send-as/send-on-behalf, shared mailbox access, delegated access, and application permissions. The MVP should use the narrowest permission model that supports controlled project mailbox workflows.

Microsoft 365 / Exchange permissions are external constraints, not internal authority. If Microsoft Graph or Exchange grants mailbox access but Hexalith role rules deny project access, association is blocked.

The system must operate only within granted mailbox permissions. Missing, revoked, partial, throttled, expired, or delayed Microsoft 365 permissions must result in degraded mailbox processing for the affected mailbox only, with visible operational status and no fallback to broader tenant-wide access.

Outbound email authority must be explicit. The system must record whether an outbound message was drafted only, sent by an authenticated user, sent through a shared mailbox, or sent by an approved service flow. Risky outbound actions must require approval and preserve the approved content, sender authority, recipients, project context, and source command surface.

Message identity must be stable enough for idempotency and audit reconstruction. The integration must retain source message ID, internet message ID where available, conversation/thread identifiers, mailbox identity, delivery timestamp, sender/recipient metadata, and attachment identifiers.

### Integration List

MVP integrations are limited to the systems needed to prove the governed email-to-project collaboration loop:

- Hexalith.Projects
- Hexalith.Parties
- Hexalith.Folders
- Hexalith.Tenants
- Hexalith.EventStore
- Hexalith.FrontComposer (UI composition for the M0/M1/M2 surfaces enumerated in NFR60)
- Microsoft 365 / Exchange mailboxes
- Keycloak
- Aspire (development and runtime topology composition — see §Aspire entry in Glossary)
- CLI (M1+)
- MCP server (M1+)

Non-MVP integrations include Teams, WhatsApp, additional messenger channels, broad document intelligence providers, workflow builders, arbitrary third-party integrations, and advanced operational consoles.

### Integration Contracts

MVP integrations must use explicit versioned contracts for commands, events, API payloads, permission claims, and failure states. Breaking contract changes require compatibility handling or coordinated deployment across dependent Hexalith services, CLI, and MCP clients.

All integration requests must include correlation IDs so actions can be traced across API, worker, Microsoft 365 event handling, CLI, MCP, AI mediation, command execution, and audit projection.

### Dependency Failure Handling

Failures in Microsoft 365 / Exchange, Keycloak, Hexalith services, Aspire composition, CLI, or MCP integrations must be isolated to the affected operation, tenant, mailbox, or client session. The system must not silently create project associations when required dependencies are unavailable; it may queue, retry, mark pending review, quarantine, or surface degraded status according to operation type.

Expected failure outcomes include mailbox unavailable, Graph throttled, token expired, ambiguous project match, no candidate above confidence threshold, project deleted during association, tenant mismatch, duplicate email event, stale confirmation/version conflict, project index stale, attachment scanning unavailable, audit write failure, and CLI/MCP timeout. Each state needs user-visible status, retry behavior, audit behavior, and terminal/non-terminal classification.

If Keycloak or identity resolution is unavailable, command/query operations fail closed rather than falling back to broad access. If EventStore or audit writing is unavailable, **every** state-mutating operation fails closed per NFR15a — not only risky commands and AI actions. If AI services are unavailable, deterministic project association (which does not depend on AI), audit, authorization, and manual resolution workflows remain usable; the M0 risk classifier is tag-and-heuristic and does not depend on AI services either (see `addendum.md` §Risk Classifier), so the approval gate survives AI outage.

### Performance & Operability Considerations

The MVP must be operable under realistic mailbox and project volumes without turning ambiguous association into a manual bottleneck. The system should measure ingestion latency, candidate generation latency, ambiguous-resolution time, command execution latency, audit projection lag, retry volume, duplicate suppression rate, failed mailbox processing rate, and dead-letter rate.

CLI and MCP operations must return clear status for long-running or eventually consistent work. If audit projections, attachment scans, or command outcomes are delayed, surfaces should return pending or partial-success states rather than stale success claims.

Operational views should expose API health, mailbox integration health, background worker health, database health, Keycloak connectivity, mailbox backlog, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, service-client failures, and audit projection lag.

### Compliance Requirements

The B2B governance implementation must support enterprise expectations without overclaiming certification status. The MVP must include practical controls for GDPR/EU data protection, tenant isolation, least privilege, auditability, identity integration, and Microsoft 365 / Exchange mailbox governance.

Compliance requirements include GDPR-aware handling of email content, external party records, message metadata, attachments, AI prompts/outputs, approvals, and audit records; tenant-admin visibility into captured data and retention; command/query authorization across UI/CLI/MCP; audit records for association and AI workflows; secure mailbox access; explicit outbound authority; and suppression of unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details.

Storage locations for tenant project metadata, email metadata, audit records, and derived AI outputs are defined per data class in §Data Governance Surface (Hexalith.ChatBot owns the derived stores; sibling bounded contexts own the source records). Region pinning and residency constraints follow NFR12 and the tenant policy `data.residency` knob. Export/delete workflows are operationally supported through FR58 (admin/reviewer-triggered operational support) and may be partially manual in M0/M1; M2 adds the operational dashboards that surface request status (per FR67 + FR75f compliance-admin scope).

### Audit Requirements

The system must record who or what associated an email to a project, when, using which rule/signal, and whether the association was automatic, suggested, corrected, rejected, deferred, or overridden.

Minimum audit fields include `tenantId`, `actorId`, `actorType`, `commandName`, `resourceId`, `decision`, `reasonCode`, `correlationId`, and `timestamp`.

Security-sensitive operations must produce audit records. Audit completeness must be measurable and failed authorization attempts and service-client failures must be queryable for incident review.

### Security and Isolation Acceptance Test Matrix

The MVP must include acceptance tests across these actor types:

- Human user
- Tenant admin
- Project admin / owner
- Service client
- CLI client
- MCP client
- Background worker
- Microsoft 365 / Exchange event
- AI actor

Each actor type must be tested for authentication, tenant context, authorization, audit production, failure behavior, and data exposure. Negative tests must cover cross-tenant project IDs, stale or missing tenant context, cached CLI credentials after tenant switch, MCP tool arguments attempting to bypass validation, revoked service-client credentials, missing service-client scopes, disabled users, malformed mailbox events, duplicate events, and role changes.

Tenant isolation monitoring has zero tolerance for cross-tenant access events. Security-sensitive operations must produce audit records.

### Future Packaging Signals

Subscription tiers are out of scope for this PRD and must not influence MVP behavior. MVP must not enforce subscription-tier limits or billing gates, and tenant isolation and permissions must not depend on subscription tier.

Future packaging may consider tenant count, monitored mailbox count, project count, email volume, AI action volume, audit retention duration, advanced approval policies, channel integrations, automation limits, and operational analytics.

### B2B SaaS Non-Goals

The MVP is not a general-purpose email client, CRM, helpdesk, workflow builder, enterprise search product, unrestricted AI automation platform, full project management suite, public API marketplace, broad enterprise admin suite, or commercial billing system.

The MVP does not include subscription tier enforcement, arbitrary third-party integrations, cross-tenant project discovery, implicit service-client elevation, broad customizable permission models, or autonomous association confirmation without human or configured policy approval.

### Implementation Considerations

The MVP should be implemented as a narrow vertical slice through the governed email-to-project workflow. It should avoid building a broad collaboration platform before proving project association, authorization, approval, audit, and cross-surface parity.

Implementation should prioritize stable identifiers across Hexalith bounded contexts, shared command/query contracts for UI/CLI/MCP, idempotent mailbox ingestion, explicit association lifecycle states, evidence-first candidate generation, tenant-scoped authorization filters before candidate or audit projection, allowlisted service commands, auditable AI action proposals and outcomes, contract tests, parity tests, and test fixtures for clear matches, ambiguous matches, no-match cases, unauthorized references, cross-tenant references, duplicate delivery, retries, attachment states, degraded dependencies, and risky AI approvals.

## Project Scoping

### Strategy & Philosophy

**Approach:** Single-release MVP delivered in three sequential increments (M0 → M1 → M2). Technical and trust proofs lead; breadth is sequenced behind them.

Hexalith.ChatBot's MVP is scoped as one coherent release window containing three dependency-ordered increments rather than a single big-bang ship or a multi-release roadmap. Each increment is releasable to the pilot cohort; the increments together prove the complete governed email-to-project loop end-to-end across UI, CLI, and MCP. The detailed per-increment scope lives in §Minimum Release Slice — Three Increments; this section summarizes the strategic intent and the team load.

The MVP is a technical proof, user workflow proof, and platform proof distributed across the three increments:

- **M0 — Technical proof of the vertical loop:** email-to-project association, tenant isolation, authorization at the command boundary, fail-closed behavior, governed AI action with approval, and audit completeness work end-to-end in the UI under realistic failure conditions.
- **M1 — Platform proof of cross-surface parity:** UI, CLI, and MCP operate through a single shared command pipeline that makes parity structural, not test-enforced. Full lifecycle, full approval policy, admin governance, and outbound communication land here.
- **M2 — Operations proof:** dashboards, recovery (RPO/RTO), tamper-evident audit chain, audit-completeness as a production observable, idempotency contract, replay isolation, and cross-tenant cache/vector isolation make the system operable in production.

**Resource Requirements:** The named team is product ownership, a system architect, backend/service engineers (plural — single point of failure if reduced), one frontend engineer, one CLI/MCP engineer, one security/identity engineer, a QA/test architect, and DevOps support. This team is sized for the three-increment delivery, not for a single big-bang ship of the original 25-item must-have list. The accessibility bar (WCAG 2.2 AA) is scoped per-increment to the surfaces that exist in that increment — the frontend engineer is not expected to deliver full WCAG conformance against unbuilt surfaces.

[NOTE FOR PM] The original PRD (pre-2026-05-28 update) treated the must-have list as a single shippable release. The adversarial review correctly identified that as a 6–12 person-year backlog packaged as one release. The three-increment sequencing reconciles team to scope without dropping the parity bet or the trust foundation. If the team grows materially (e.g., a second frontend engineer or a second backend pair), increment durations compress but the dependency order does not change — M1 still depends on M0's vertical loop being stable in pilot, and M2 still depends on M1's parity invariant being in production.

### Complete Feature Set

**Core User Journeys Supported:**

The single release supports all documented MVP journeys:

- Business contributor requests AI help from a project conversation.
- Business contributor resolves ambiguous project association.
- External party sends project context into Hexalith through email.
- Project owner corrects a wrong association.
- Tenant admin configures governed email collaboration.
- Developer uses CLI to inspect and resolve the project email workflow.
- Compliance or support reviewer investigates a risky action.
- User reviews an AI action before it leaves the project boundary.
- Governed AI execution operates inside explicit project, tenant, party, file, command, approval, and audit boundaries.

**Must-Have Capabilities (grouped by increment):**

The single release must include the full governed email-to-project collaboration loop. The 25 must-have capabilities are now grouped by their target increment so per-increment scope and team load are visible. The dependency order is M0 → M1 → M2.

**M0 must-haves — Vertical Thesis Path (UI-only):**

- Controlled Microsoft 365 / Exchange mailbox ingestion for one configured tenant.
- Tenant-scoped mailbox configuration and one monitored mailbox pattern.
- Stable message identity, conversation/thread identifiers, attachment metadata, and delivery/retry state.
- Party resolution through Hexalith.Parties for internal and external participants present in the M0 mailbox.
- Tenant isolation across the M0 actor set: human users, tenant admins, project owners, background workers, M365 events, AI actors (CLI, MCP, service clients deferred to M1).
- Email-to-project association using deterministic signals (explicit project identifier, mailbox routing rule, conversation/thread identifier) with configurable confidence thresholds T_high / T_low whose contract is defined in `addendum.md` §Confidence Thresholds.
- Candidate project generation with evidence and confidence state.
- User association decisions: confirm, choose different project, reject all candidates, defer, needs review, and correct previous association.
- M0 lifecycle states: Received, Proposed, Associated, NeedsReview, Deferred, Rejected, Failed, Skipped, Corrected.
- Governed attachment capture into Hexalith.Folders with security/status handling and quarantine of unsafe attachments.
- Project conversation context built from email-derived messages, parties, attachments, decisions, approvals, failures, and AI outcomes.
- AI action mediation with explicit project scope, requester, input files, command intent, risk classification, policy decision, approval state, and audit trail — restricted to one allowlisted command in M0.
- Low-risk AI assistance according to tenant policy and authorized project scope.
- Approval/confirmation for externally visible, project-mutating, file-exposing, tool-invoking, task-creating, or participant-representing AI actions.
- The M0 subset of the command allowlist (see `addendum.md` §Command Allowlist v0).
- Audit records for the M0 audit-event set named in §Increment M0.
- UI support for the M0 surfaces enumerated in NFR60: ambiguous association review, AI action approval, project conversation view.
- Keycloak-backed identity for the M0 actor set.
- Aspire-composed development/runtime topology (single-tenant pilot footprint).
- Dependency failure handling for M0 dependencies: M365, Keycloak, Hexalith.Projects/Folders/Parties, EventStore, attachment scanner, AI service.
- Performance and operability baselines for M0 operations: ingestion latency, candidate generation latency, ambiguous-resolution time, command latency for the one allowlisted command, audit projection lag (M0 events only).
- Security and isolation acceptance tests for the M0 actor set.

**M1 must-haves — Cross-Surface Parity & Full Governance:**

- CLI operation parity for inspect, associate, reject, defer, correct, retry, approve, execute, status, and audit lookup.
- MCP operation parity for governed AI-agent/tool access.
- Shared command pipeline (architectural invariant) that makes UI/CLI/MCP parity structural — see FR81a.
- Service-client authorization via Keycloak service accounts.
- Outbound communication: one outbound draft-and-send path preserving sender authority (FR48), recipients, approved content, audit history.
- Full state-transition matrix expansion.
- Approval policy flexibility per tenant, role, project, action type, recipient, and risk class — surfaced through the Tenant Policy Schema (see `addendum.md` §Tenant Policy Schema).
- Tenant-admin permission model (FR75a–FR75g): admin scopes, audit obligations, no bypass.
- Versioned command allowlist v1 under change control (see `addendum.md` §Command Allowlist v1).
- Risk-classifier mechanism named, calibrated against A9a evaluation dataset, with misclassification fallback and reviewer-disagreement audit chain.
- Inbound-message-authenticity controls: provider-supplied DMARC/DKIM/SPF passthrough without ChatBot re-verification, header inspection, on-behalf-of disambiguation, external-sender posture (FR48a–FR48d).
- Tenant isolation extended to CLI client, MCP client, service client actor types.

**M2 must-haves — Operations, Recovery, Continuity:**

- Operational dashboards for mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, audit lag.
- Recovery objectives verified through a continuity drill (RPO ≤ 15 min, RTO ≤ 4 hr — [ASSUMPTION] per A10 until drill calibrates).
- Replay/simulation contract with separate test tenant, outbound-adapter interception, and audit distinguishability (FR95a).
- Idempotency contract per operation class (see `addendum.md` §Idempotency Keys).
- Tamper-evident audit chain via append-only WORM store with hash-chained envelopes (NFR49a).
- Audit completeness as a production observable (NFR50a, ≥ 99.5% per rolling 7-day window).
- Cross-tenant isolation tests for vector indexes, embedding stores, prompt-context caches (FR55a / NFR9a).
- WCAG 2.2 AA extended to M1 surfaces with UI: admin dashboards and approval-policy configuration.
- Full performance and operability instrumentation across all operation classes; SLOs published per NFR42a (initial values from NFR24–NFR27 and NFR43; pilot calibration per A11).

**Nice-to-Have Capabilities:**

These may be included only if they do not weaken the must-have release goal:

- Richer operational dashboards beyond essential failure/status views.
- Advanced mailbox inference for forwarded threads, aliases, shared mailbox edge cases, and complex conversation histories.
- Enhanced AI prompt/user experience polish beyond the required governed action preview and refusal behavior.
- Expanded document intelligence over attachments beyond capture, metadata, status, and authorized AI use.
- Advanced approval policies beyond MVP confirmation gates for risky actions.
- Advanced correction analytics and learning beyond recording correction events for evaluation.
- Broader administrative reporting beyond tenant, mailbox, association, approval, and audit essentials.

Explicitly out of scope for this release:

- Teams, WhatsApp, and additional messenger channels.
- General email client replacement.
- Full task lifecycle management.
- Autonomous project creation from email.
- Full document intelligence over attachments.
- Broad knowledge management.
- Unrestricted command execution or automation.
- Cross-tenant project discovery or candidate suggestions.
- Subscription tiers, billing, usage metering, entitlement enforcement, trials, or commercial packaging.
- Arbitrary third-party integrations.
- Broad customizable permission models.
- Autonomous association confirmation without human or configured policy approval.

### Risk Mitigation Strategy

**Technical Risks:**

The highest technical risk is incorrect project association. The release mitigates this with deterministic evidence, configurable confidence thresholds, candidate review, fail-closed behavior, correction workflow, seeded evaluation datasets, and zero tolerance for critical false-positive associations involving unauthorized projects.

The second technical risk is tenant or permission leakage across UI, CLI, MCP, service clients, workers, indexes, and audit projections. The release mitigates this with Keycloak-derived tenant scope, command/query authorization, no direct data-plane access for CLI/MCP, tenant-scoped candidates/evidence, negative isolation tests, and auditable authorization failures.

The third technical risk is unreliable mailbox integration. The release mitigates this with stable message identity, idempotency keys, duplicate suppression, retry states, degraded mailbox health, M365 permission handling, and failure queues that do not contaminate project state.

The fourth technical risk is unsafe AI action. The release mitigates this with scoped AI context, allowlisted commands/tools, risk classification, low-risk policy limits, approval/confirmation for risky actions, refusal behavior, and audit records for proposed, denied, approved, executed, and failed AI actions.

**Market Risks:**

The core market risk is that users may perceive the governed workflow as extra administration rather than a continuation of email-based work. The release mitigates this by making the primary workflow evidence-driven and low-friction: users should resolve ambiguity from captured evidence without re-reading full threads or manually comparing project records.

The second market risk is that external participants will not adopt a new tool. The release mitigates this by preserving email as the external collaboration channel and representing external participants as Hexalith.Parties without requiring external authentication in the MVP.

The third market risk is that UI/CLI/MCP parity may matter less than expected. The release treats parity as a validation goal by constraining it to core governed operations and measuring whether developers and automation clients use CLI/MCP without bypassing governance.

**Resource Risks:**

The single release is split into three increments (M0 → M1 → M2) to keep per-increment scope sized to the named team. The dependency order is fixed: M1 must not start before M0 is in pilot, and M2 must not start before M1's parity invariant is in production. If resources become constrained, the scope should not cut tenant isolation, association correctness, authorization, auditability, idempotency, or fail-closed behavior — those are product safety foundations shared across all three increments.

Resource contingency should reduce polish and breadth within an increment before reducing trust controls. The first candidates to trim are advanced dashboards (M2), advanced mailbox inference (post-MVP), advanced approval-policy flexibility (within M1), rich document intelligence (post-MVP), analytics polish, and non-essential UI refinements. The MVP is not shippable until M2 closes; M0 alone proves the thesis to the pilot cohort but does not constitute MVP completion.

[NOTE FOR PM] If team growth changes the picture (e.g., a second frontend engineer joins, or backend headcount doubles), increment durations compress but the dependency order does not change. If team shrinks (e.g., the security engineer rotates off mid-M1), the recovery path is to extend M1 duration, not to compress trust controls — and to log the schedule slip rather than absorb it silently.

The following functional requirements convert the validated scope, journeys, governance boundaries, and risk controls into the capability contract for UX, architecture, epics, and delivery.

## Functional Requirements

### Glossary

| Term | Definition |
| --- | --- |
| Actor | A human user, external party record, service client, background worker, mailbox event processor, CLI client, MCP client, or AI actor that initiates or participates in a workflow. |
| AI actor | A governed service actor that can use only explicitly provided project scope, authorized files, allowlisted tools or commands, and tenant policy. It is not a privileged system user. |
| Allowlisted command | A Hexalith service command present in the current versioned command allowlist (see `addendum.md` §Command Allowlist v0 / v1). Only allowlisted commands can be invoked by AI actors; commands removed from the allowlist cannot be invoked by AI actors even when the underlying service still accepts them. |
| Aspire | .NET Aspire — the application composition framework Hexalith uses for development and runtime topology (service discovery, resource declaration, telemetry wiring, local-dev container orchestration). Aspire is a build/deploy concern, not a runtime dependency of individual user requests. |
| Approval | A human or policy decision that permits, rejects, revises, cancels, or denies a proposed risky action before execution. |
| Approval-required | A risk classification (per `addendum.md` §Risk Classifier) for actions that cannot execute without human approval: actions that modify project state, send external communication, expose file content, create or assign tasks, invoke external tools, or act on behalf of a participant. Distinct from `low-risk` (allowed if tenant policy permits) and `denied` (refused regardless of approval). |
| Association | The decision that links email-derived context to one project or explicitly rejects, defers, fails, skips, or corrects that link. |
| Candidate project | A project the system may suggest for association after applying tenant, authorization, and evidence filters. Unauthorized projects must not appear as candidates. |
| Command surface | The client or origin through which an operation is requested, including UI/API, CLI, MCP, background worker, mailbox event, or AI actor. |
| Context package | The bounded project, requester, file, policy, evidence, command, and redaction context made available to an AI actor or governed command. Synonym: **Scoped AI context.** The PRD uses "Context package" in the FR/NFR catalog and "Scoped AI context" in narrative prose; the two terms refer to the same artifact. |
| Evaluation dataset | The labeled corpus of mailbox events maintained by the Test Architect (see A9 / A9a) used to calibrate `T_high`, `T_low`, the risk classifier, and the precision/recall targets in §Measurable Outcomes. Cardinality, label taxonomy, and refresh cadence in A9a. |
| Evidence | The auditable signals used to justify association, authorization, approval, refusal, correction, or investigation. |
| External party | A customer, supplier, partner, organization, or contact represented through Hexalith.Parties and participating through email or another governed channel. |
| Party | A tenant-scoped participant identity owned by Hexalith.Parties. A party may represent an internal or external participant. |
| Projection | A derived read model or project view rebuilt from source records, commands, events, and audit history. |
| Fail closed | A code path that, on encountering an error or unmet precondition that would otherwise let it proceed without enforcing a safety control, returns a typed error and writes no durable state — instead of falling back to permissive behavior. See NFR15a for the enumerated paths and conditions. |
| Idempotency key | A stable identifier composed from operation-class-specific inputs that lets the system detect and suppress duplicate operations. Per-operation key composition, replay window, equivalence rule, and conflict response are in `addendum.md` §Idempotency Keys. |
| Low-risk | A risk classification (per `addendum.md` §Risk Classifier) for read-only AI actions that may execute without approval when tenant policy `ai-action.low-risk-allowed = true` and the actor is authorized to the project. Distinct from `approval-required` and `denied`. |
| MCP | Model Context Protocol — the protocol surface through which AI agents and automation tools invoke governed Hexalith operations. MCP clients are first-class actors in the FR81a shared command pipeline. |
| MVP parity set | The subset of governed operations exposed at full parity across UI, CLI, and MCP. Enumerated in §Cross-surface parity outcomes; verified by FR86 contract tests against the FR81a invariant. |
| Operating baseline | A tenant- or deployment-specific configuration of NFR performance, capacity, and reliability targets. Default MVP values are in NFR24–NFR27 and NFR43; per-tenant overrides land via the Tenant Policy Schema in M1. |
| Policy snapshot | An immutable, versioned capture of the tenant policy state at the moment a decision was made (authorization, association, approval, AI action, command execution). The snapshot ID travels in the audit envelope so decisions are reconstructable even after policy mutates. |
| Risky AI action | An AI-mediated action classified `approval-required` (synonym for that classification at the user-facing layer). See `addendum.md` §Risk Classifier for the classification rules. |
| Service client | A non-human client identity with explicit scoped grants, expiry, correlation, and audit metadata. It does not inherit human UI roles. |
| Source record | Immutable or source-of-truth input such as mailbox message identity, attachment record, command record, event, approval, or audit entry. |
| Tenant policy | Configuration values for a single tenant, structured by the Tenant Policy Schema in `addendum.md` §Tenant Policy Schema. Tenant policy is the universal configuration surface for behavior knobs; tenants cannot define new knobs, only set values within the schema. |

### Traceability Overview

| Journey | Primary FRs | Primary NFRs | Validation focus |
| --- | --- | --- | --- |
| UJ1 - Business contributor requests AI help | FR21-FR28, FR33, FR35-FR46 | NFR1-NFR11, NFR16, NFR49-NFR55, NFR60-NFR64 | Project context visibility, scoped AI package, approval, audit trail |
| UJ2 - Business contributor resolves ambiguous association | FR3-FR12, FR64-FR69, FR76-FR80 | NFR13-NFR18, NFR23-NFR30, NFR37-NFR48 | Candidate evidence, confirm/reject/defer, fail-closed ambiguity |
| UJ3 - External party sends project context | FR1-FR4, FR13-FR20, FR29-FR34 | NFR1-NFR12, NFR31-NFR36, NFR49-NFR55 | Party resolution, tenant scope, authorization before exposure |
| UJ4 - Project owner corrects wrong association | FR7-FR8, FR23-FR28, FR60-FR63, FR87-FR96 | NFR13-NFR22, NFR49-NFR59 | Correction, derived-context invalidation, audit reconstructability |
| UJ5 - Tenant admin configures governed collaboration | FR9, FR18-FR20, FR51-FR53, FR67-FR75 | NFR23-NFR48, NFR65-NFR70 | Policy configuration, mailbox health, operational queues |
| UJ6 - Developer uses CLI | FR80-FR86, FR90-FR95 | NFR24-NFR36, NFR67-NFR70 | CLI parity, status, idempotency, redaction, audit lookup |
| UJ7 - Compliance/support investigates risky action | FR54-FR63, FR85-FR86, FR90-FR91 | NFR49-NFR59 | Reconstructable audit records with safe visibility |
| UJ8 - User reviews AI action before boundary crossing | FR39-FR50 | NFR16, NFR46-NFR48, NFR60-NFR64 | Risk explanation, approval decision, refusal and outbound control |
| System journey - Governed AI execution | FR33, FR39-FR46, FR81-FR89 | NFR1-NFR22, NFR31-NFR36, NFR49-NFR55 | Policy-aware AI actor, allowlisted command execution, failure behavior |

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

## Open Assumptions and Decisions

These assumptions are explicit so UX, architecture, and story creation can confirm or replace them before implementation locks in downstream commitments.

| ID | Assumption or decision | Owner | Revisit condition |
| --- | --- | --- | --- |
| A1 | The MVP pilot can use Microsoft 365 / Exchange as the first controlled mailbox integration without requiring another mailbox provider. | Product / Architecture | Revisit if the first pilot tenant cannot grant acceptable M365 permissions or primarily uses another mailbox provider. |
| A2 | The minimum release slice may use one controlled mailbox pattern before supporting multiple aliases, shared mailbox edge cases, forwarded-thread inference, or complex mailbox histories. | Product / Architecture | Revisit after the deterministic and ambiguous association paths pass pilot validation. |
| A3 | CLI or MCP parity is required in MVP, but the first parity proof may cover association, status, and audit before covering every UI operation. | Product / Developer Experience | Revisit if pilot automation users do not exercise the machine surface or if parity threatens the minimum release slice. |
| A4 | Default tenant/deployment operating baselines in NFR23-NFR30 are acceptable until a tenant-specific baseline is approved. | Architecture / Operations | Revisit during tenant onboarding, capacity planning, or quarterly baseline review. |
| A5 | AI provider telemetry, training reuse, retention, and region behavior can be configured to satisfy tenant policy before live AI invocation. | Security / Architecture | Revisit before selecting or enabling an AI provider for tenant data. |
| A6 | Audit retention and deletion behavior can preserve reconstructability while satisfying GDPR/EU data protection obligations. | Compliance / Architecture | Revisit before pilot onboarding and whenever retention policy conflicts with deletion/export requests. |
| A7 | External participants do not need authenticated portal access in MVP; party resolution and email governance are sufficient for the first workflow. | Product | Revisit if pilot users need external review, approval, or correction actions from outside email. |
| A8 | A fixed allowlist of project collaboration commands is sufficient for MVP AI action execution. | Product / Architecture | Revisit when task-intent capture produces repeated unsupported-but-valid project work requests. |
| A9 | The evaluation dataset can be built from consented, redacted, or synthetic examples that represent realistic mailbox ambiguity and authorization failures. | Test Architect (single named owner; Product Lead consults on sampling representativeness) | Revisit if dataset results fail to predict pilot behavior or miss important edge cases. |
| A9a | The evaluation dataset cardinality is at least `500` labeled messages by M0 release and at least `2000` by M1 release, drawn from the pilot tenant's consented mailbox sample plus synthetic adversarial examples. Label taxonomy: `deterministic-match`, `ambiguous-match`, `no-match`, `unauthorized-project`, `cross-tenant-reference`, `duplicate`, `attachment-only`, `risky-ai-candidate`, `inbound-authenticity-anomaly`. Refresh cadence: monthly during pilot, quarterly afterward. Adversarial-example protocol: each calibration cycle adds at least `20` new adversarial examples drawn from production reviewer disagreements (see `addendum.md` §Risk Classifier). | Test Architect | Revisit if recall drops below 90% on production sampling for two consecutive months. |
| A10 | RPO ≤ 15 min and RTO ≤ 4 hr are starter targets until a documented M2 continuity drill calibrates them against the Aspire-composed topology and pilot data volumes. The drill must execute end-to-end recovery from a simulated EventStore outage and a simulated M365 subscription failure. | Architecture / DevOps | Revisit after the M2 continuity drill; re-state the targets as commitments if drill confirms, or revise downward with documented rationale. |
| A11 | Pilot adoption thresholds (4 weeks, 2 mailbox patterns, 5 projects, 70% evidence-resolution, 40% time reduction, 30% manual-update reduction, 10/30 AI reviews) are starter values calibrated against a 2–4 week baseline measurement window at the pilot tenant before M0 release. Final targets are documented after baseline measurement and may differ from the starter values. | Product Lead | Revisit immediately after the baseline measurement window closes; thereafter, every increment release. |

The following non-functional requirements define the quality bar for the same capability contract: how reliably, securely, observably, and accessibly the product must operate under enterprise conditions.

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
